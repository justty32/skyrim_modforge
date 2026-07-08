# Step 1：總覽 + 前置需求 + 規劃技能樹

← [custom-skill-tree-guide](README.md)

## 1. 總覽：要做出一棵技能樹，需要哪些拼圖

一棵「現代（JSON 格式）」自訂技能樹由**四到六塊**拼起來。先看全貌：

| # | 零件 | 放哪 | 做什麼 | 必要性 |
|---|------|------|--------|--------|
| A | **一個 `.esp`** | `Data/MySkills.esp` | 裝技能樹要用到的所有記錄：PERK（節點）、GLOB（等級/進度/legendary）、KYWD（升級掛鉤），可選 BOOK / MGEF | **必要** |
| B | **技能設定檔 `<X>.json`** | `Data/SKSE/Plugins/CustomSkills/<X>.json` | 描述這棵樹：技能名/述、指向 esp 裡哪幾個 GLOB、升級曲線、每個 perk 節點的座標與連線 | **必要** |
| C | **`SKILLS.json`**（可選之一） | `Data/SKSE/Plugins/CustomSkills/SKILLS.json` | 把你的技能**塞進原版技能選單那一頁**（最像原生）。不用它就得另開獨立選單 | 二選一 |
| D | **init alias script** | esp 裡的 quest + 一支 `.psc`/`.pex` | 玩家首次安裝時把技能等級設成起始值、授予基礎被動 perk、做版本 gate | **建議** |
| E | **訓練 TIF fragment**（可選） | 對話 TopicInfo 的一行 fragment | 把某個 NPC 變成你的技能訓練師（花錢練等） | 可選 |
| F | **Fortify-技能 MGEF + `ActorValueData/*.toml`**（可選） | esp 裡的 MGEF + 一份 toml | 讓「強化 X 技能」附魔/藥水對自訂技能生效。**這條非要一個自寫的 native SKSE plugin 不可** | 進階加值 |

**心智模型**：`perk 的效果由 esp 決定，技能的「外觀與進度」由 CSF 設定檔（JSON）決定。** CSF 框架（`CustomSkills.dll`）只提供「選單外殼 + XP/升級引擎」——它不發明新的 perk 格式，你的 perk 就是普通的 PERK record。

最小可行產物（MVP）= **A（N 個 PERK + 3 個 GLOB + 升級 KYWD）+ B（`<X>.json`）+ C（`SKILLS.json`）+ D（init script）**。E、F 是錦上添花。

---

## 2. 前置需求

### 玩家端（執行時依賴）
- **Custom Skills Framework**（Nexus 41780）這個 SKSE plugin 必裝。它提供 `CustomSkills.dll`（runtime）與 `CustomSkills.psc`/`.pex`（Papyrus API）。
- 對應版本的 SKSE64、Address Library。

### 兩代格式：選哪一個
CSF 有**兩種完全不同的設定格式**（細節見 survey §1）：

| 世代 | 後端 | 設定檔 | 建議 |
|------|------|--------|------|
| 舊（v1.x） | NetScriptFramework | `Data/NetScriptFramework/Plugins/CustomSkill.<Id>.config.txt`（INI 風格） | **不要用**，只在維護 VIGILANT/GLENMORIL 那代舊 mod 時才碰 |
| **新（v2.x / v3.x）** | 原生 `CustomSkills.dll` | `Data/SKSE/Plugins/CustomSkills/<X>.json` | **用這個**。本指南全程走 v3 |

**強烈建議鎖定 v3**：v3 的 `CustomSkills.psc` 多了 `AdvanceSkill` / `IncrementSkill` / `ShowTrainingMenu` / `GetSkillLevel` 等便利函式（v2 只有 3 個函式），讓你**不必自寫管理 quest**。本指南假設 API version 3。

### 作者端（製作時用得到）
- **SSEEdit / xEdit** 或 **Creation Kit**：用來在 esp 裡建 PERK / GLOB / KYWD / MGEF 記錄。
- 一個文字編輯器寫 JSON（注意 `SKILLS.json` 用了 `$ref` 與 jsonc 註解風格，但出貨檔請寫成合法 JSON——範例裡的 `//` 註解是教學用，實檔要拿掉）。
- Translations 檔需存成 **UTF-16 LE + BOM、tab 分隔**。
- 若做 D（init script）/ E（訓練 TIF）：Papyrus compiler。

---

## 3. Step 1 — 規劃技能樹

動手建記錄前，先在紙上把樹畫出來。要決定的東西：

1. **技能數**：一個 mod 可以有多棵樹（Constellations 有 3 棵：HandtoHand / Athletics / Sorcery）。每棵樹是一個獨立的 `<Skill>.json`。
2. **每棵樹的 perk 節點數**：CSF 的 `nodes` 陣列**上限 127 個**。Constellations 每棵 9 個，很夠用。
3. **每個節點對應哪個 perk**：一個節點 = 一個 PERK record。**多階 perk（rank 2/3/4…）只在 node 填第一階的 FormId**，後面幾階靠 esp 裡 PERK 的 Next Perk 鏈接。
4. **節點之間的連線（links）**：哪個節點要先點才能點下一個。入口節點通常叫 `Mastery`。
5. **grid 座標 `x`/`y`**：浮點自由佈局。**注意方向反直覺：`x` 正方向朝左、`y` 正方向朝上**（survey §2.1）。渲染器自己連線，你只給座標。現代 JSON **沒有** `GridX`/`GridY`（那是舊 INI 的東西）。

### 範例小樹（本指南全程用它）

假設我們做一棵叫 **「Beast Lore（野獸學識）」** 的技能（id = `BeastLore`），5 個節點：

```
                 [Mastery]   (x=0.0, y=0.0)  入口
                  /      \
                 /        \
         [Tracking]      [Resilience]
        (x=-1.2,y=1.0)   (x=1.4,y=1.0)
            |                 |
       [Predator]        [ThickHide]
       (x=-1.8,y=2.5)    (x=2.0,y=2.5)
```

- `Mastery` → links `[ Tracking, Resilience ]`
- `Tracking` → links `[ Predator ]`
- `Resilience` → links `[ ThickHide ]`
- `Predator`、`ThickHide` 是末端，無 links。

對照真實案例：Constellations 的 `HandToHand.json` 入口 `Mastery`（x=0.4,y=0.0）分出 `UnarmedSpeed`（x=-0.3,y=0.8，往左上）與 `DamageAttack`（x=2.0,y=0.5，往右下方視覺上是左），各自再往下分支到末端。座標就是這樣憑視覺擺，框架不檢查重疊。

---

