# 自己動手做一棵 Skyrim 自訂技能樹（Custom Skills Framework 實作指南）

> 這是一份**動手教學**：跟著走完，你會得到一棵能在遊戲裡「按 ESC → Skills」就看得到、可以練等、可以花點數點 perk 的自訂技能樹。
> 技術原理（CSF 架構、兩代格式斷層、欄位語意的逐項拆解）請看姊妹文件 [`custom-skills-framework.md`](custom-skills-framework.md)；本文**不重複**那些深水區內容，只在需要時連過去。
> 慣例：散文用繁體中文，所有 JSON key / record type / API 名稱 / FormId 保留 English。
> 範例全部改編自本機解壓的 **Constellations - Additional Player Skills**（Nexus 117352），那是 CSF 作者 Exit-9B 親自掛保證的現代 JSON 範本。

---

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

## 4. Step 2 — 在 esp 裡建記錄

這一步全在 SSEEdit/CK 裡做。下面逐種記錄說明要填什麼。

### 4.1 PERK records（每個節點一個）

技能樹的每個節點 = 一個普通 PERK record，**和原版 perk 完全一樣**——這正是 ModForge 既有 perk 能力能直接重用的原因。

兩類常見 perk：
- **entry-point perk**（「Modify Skill Use」「Mod Attack Damage」之類）：透過 perk entry-point 改變遊戲行為。
- **ability perk**（掛一個 abilities/spell 的被動 MGEF）：給玩家一個常駐被動效果。

每個 PERK 要填：
- **EDID**（Editor ID）：用你自己的前綴，例如 `BL_Mastery`、`BL_Tracking01`。
- **FULL**（Name）/ **DESC**（Description）：這是 perk 在選單裡顯示的名字與說明。**這層的在地化走 esp**（見 Step 6），與技能名/述（JSON 那層）是兩套獨立通道。
- **Perk Sections / entry points 或 abilities**：perk 的實際效果。
- **多階 perk**：若一個節點要有 4 階，建 4 個 PERK record，用 `Next Perk` 串起來，node 只填第一階。

> **地雷（必看）**：entry-point perk 在載入時如果 `PerkConditionTabCount` 那個 byte 是 0 會直接 CTD。請設成 vanilla canonical 值。這是 ModForge 既有筆記 [perk-conditiontabcount-ctd](記憶) 記錄過的坑——做 entry-point perk 時務必比對。

### 4.2 三個 GLOB（GlobalVariable）——每棵技能一組

CSF 用 global variable 存技能的狀態。**實證最小只需三個 per skill**（Constellations 連 `showMenu`/`showLevelup`/`perkPoints`/`color`/`debugReload` 都沒做，照常運作）：

| GLOB | 命名慣例 | 型別 | 存什麼 |
|------|----------|------|--------|
| level | `Skill<X>Level`（如 `SkillBeastLoreLevel`） | Short/Float | 目前技能等級 |
| ratio | `Skill<X>Ratio` | Float | 距下一級的進度（0–1） |
| legendary | `Skill<X>Legendary` | Short | 歸零成 legendary 的次數 |

每個 GLOB：
- 一定要給 **editor id**（console 指令與 init script 都靠它引用）。
- level 初值建議 0，由 init script（Step 5）設成 `iAVDSkillStart`（=15）。

> 可選的額外 GLOB（只有走「獨立選單群組」或要自訂點數池時才需要）：`showMenu`（值 0，外部設 1 開選單）、`showLevelup`（值 0，設 1 顯示升級訊息）、`perkPoints`（自訂點數池，不設則用玩家標準 perk point）、`color`（技能名 RGB）、`debugReload`。MVP 不用做這些。

### 4.3 升級掛鉤 KYWD（Keyword）

三個慣例命名 keyword，後綴是技能 id（`CustomSkill<Type>_<Id>`）：

| KYWD（EDID） | 貼在哪 | 作用 |
|--------------|--------|------|
| `CustomSkillAdvance_BeastLore` | perk 的「Modify Skill Use」entry-point 條件上 | 改變升級速度（取代原版 `EPModSkillUsage_IsAdvanceSkill`），配 `EPModSkillUsage_AdvanceObjectHasKeyword` 條件 |
| `CustomSkillBook_BeastLore` | 一本 BOOK 上 | 首次閱讀即推進此技能（像原版技能書） |
| `CustomSkillWorkbench_BeastLore`（可選） | 一個 constructible workbench 上 | 在該處製作即給此技能 XP |

Constellations 做了 `CustomSkillAdvance_*` 與 `CustomSkillBook_*`，**沒做** `CustomSkillWorkbench_*`（那三棵技能不走製作台路線）。你按需取捨。

### 4.4 可選：Fortify-技能 MGEF（進階）

若要支援「強化野獸學識」這種附魔/藥水，得做一組 fortify-skill MGEF + 一份 `ActorValueData/<Mod>_AVG.toml`，把自訂技能**借用一個閒置的原版 ActorValue 槽**當載體（原版引擎只認固定 AV 列舉，自訂技能沒有自己的 AV）。

**這條需要一個你自寫的 native SKSE plugin（像 Constellations 的 `Constellations.dll`）來讀那份 toml——純 esp 做不到。** 預設別做，當作未來擴充。細節見 survey §6.3。

---

## 5. Step 3 — 寫 `<X>.json`（逐欄帶寫）

這是核心。我們照 Constellations 的 `HandToHand.json` 逐行改編成 `BeastLore.json`。建一個檔：

`Data/SKSE/Plugins/CustomSkills/MySkills/BeastLore.json`

（子資料夾 `MySkills/` 是慣例，避免和別的 mod 撞檔名。）

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/skill.json",
  "id": "BeastLore",                          // 技能唯一 ID；Papyrus / 訓練選單 / console 都用它引用
  "name": "$BeastLore_Name",                  // localizedString：$-key → Translations 檔（Step 6）
  "description": "$BeastLore_Description",
  "level":      "MySkills.esp|800",           // form：指向 4.2 的 level GLOB（"Plugin.esp|FormId"）
  "ratio":      "MySkills.esp|801",           // form：ratio GLOB
  "legendary":  "MySkills.esp|802",           // form：legendary GLOB
  "experienceFormula": {                      // 升級曲線——五個調校鈕
    "useMult":      0.8,                       // 每次 AdvanceSkill 得到的 XP = useMult * 量 + useOffset
    "useOffset":    27.0,
    "improveMult":  2.0,                       // 升一級所需 XP 隨等級成長的係數
    "improveOffset": 0.0,
    "enableXPPerRank": true                    // true = per-rank 累積制（原版式）
  },
  "nodes": [                                  // perk 樹節點；陣列第一個是入口，最多 127 個
    {
      "id": "Mastery",                         // 給 links 引用的節點名
      "perk": "MySkills.esp|D65",              // form：本節點的 PERK（多階只填第一階）
      "x": 0.0,                                // 佈局座標（x 正向朝左、y 正向朝上）
      "y": 0.0,
      "links": [ "Tracking", "Resilience" ]    // 連到的子節點（用 id 字串，或 1-based 索引）
    },
    { "id": "Tracking",   "perk": "MySkills.esp|D66", "x": -1.2, "y": 1.0, "links": [ "Predator" ] },
    { "id": "Resilience", "perk": "MySkills.esp|D67", "x":  1.4, "y": 1.0, "links": [ "ThickHide" ] },
    { "id": "Predator",   "perk": "MySkills.esp|D68", "x": -1.8, "y": 2.5 },   // 末端：無 links
    { "id": "ThickHide",  "perk": "MySkills.esp|D69", "x":  2.0, "y": 2.5 }
  ]
}
```

**逐欄要點**：

- **`version` 不在這裡**。`version: 1` 是 root（`CustomSkill.json` / `SKILLS.json`）的欄位，不是 skill 物件的欄位。
- **`id`**：給 Papyrus / 訓練選單 / console 引用。提醒：Constellations 的 JSON 寫 `"HandtoHand"`（小寫 t）但訓練 TIF 卻呼叫 `"HandToHand"`——疑似容錯/筆誤仍可運作，但**你自己務必前後一致**，別賭它。
- **`name`/`description`**：以 `$` 開頭即翻譯 key（推薦）；不以 `$` 開頭視為直接字面值（deprecated）。
- **`level`/`ratio`/`legendary`**：值是 `form` 字串 `"PluginName.es[lmp]|FormId"`，指向你在 4.2 建的三個 GLOB。FormId 可 3–8 位 hex、可選 `0x` 前綴（範例 `D65`/`800` 都合法）。**這是 load-order 無關的**——CSF runtime 用 plugin 名 + 本地 FormId 查表，不受載入順序索引影響。這也是它與 ModForge FormId 配置流程天然契合的原因。
- **`experienceFormula`**：五個旋鈕就是「練多快 / 升多貴」。三棵真實技能各不同（H2H `useMult:0.8 useOffset:27`；Athletics `useMult:7.0 improveOffset:120`；Sorcery `useMult:1.8`），證明這組參數就是調曲線用的。先抄一組能動的再微調。
- **`nodes`**：第一個元素是入口（即使技能沒有 perk，**第一個 node 仍必填**）。每個 node：`id`（可選，給 links 用）、`perk`（必填 form）、`x`/`y`（必填浮點）、`links`（可選）。

---

## 6. Step 4 — 掛進選單

兩條路，二選一。

### 路線 A（推薦）：用 `SKILLS.json` 住進原版技能頁

`SKILLS.json` 是 CSF **唯一被特殊對待的檔名**：它直接取代/擴充原版技能選單那一頁，玩家按 ESC → Skills 就看得到，無感接軌。

`Data/SKSE/Plugins/CustomSkills/SKILLS.json`：

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/CustomSkill.json",
  "version": 1,                               // root 欄位：schema 版本常數
  "skydome": {                                // 背景星圖；可重用 vanilla 預設免自製
    "model": "DLC01/Interface/INTVampirePerkSkydome.nif",
    "cameraRightPoint": 2                      // 1=vanilla skydome 視角、2=beast skydome 視角
  },
  "skills": [
    "Enchanting", "Smithing", "HeavyArmor", "Block",
    "TwoHanded", "OneHanded", "Marksman",
    "LightArmor", "Sneak", "Lockpicking", "Pickpocket", "Speechcraft",
    "Alchemy", "Illusion", "Conjuration", "Destruction", "Restoration", "Alteration",
    { "$ref": "MySkills/BeastLore.json" }      // ← 自訂技能用 $ref 內嵌，放你想要的位置
  ]
}
```

要點：
- **`skills[]` 把原版技能字串列舉與自訂技能 `{ "$ref": "…" }` 混排**，**陣列順序 = 選單裡的排列順序**。把 `$ref` 插在語意相近的原版技能旁邊（Constellations 就把 HandToHand 接在 Block 後、Sorcery 壓軸）。
- 原版技能名（`Alchemy`、`Destruction`、`OneHanded`、`Marksman`、`VampirePerks`、`WerewolfPerks` … 共 20 個）是字串列舉，直接列。**只列你要顯示的**——上面省略某個原版技能，那頁就不顯示它。
- `$ref` 讓每棵樹各自存成乾淨的 `MySkills/<Skill>.json`，`SKILLS.json` 只做組裝。
- `skydome` 可指你自己的 `.nif`，或像上面重用 vanilla 的 `INTVampirePerkSkydome.nif`（免自製）。

### 路線 B：獨立選單群組

若檔名**不是** `SKILLS.json`（例如就叫 `BeastLore.json` 放在 `CustomSkills/` 根、自帶 `version`/`skills`），它就是一個**獨立選單群組**，原版技能頁看不到，必須靠 `CustomSkills.OpenCustomSkillMenu("BeastLore")` 或 console 才開得起來。VIGILANT/GLENMORIL 走這條（自帶 `showMenu` GLOB + 觸發腳本）。MVP 建議走路線 A。

---

## 7. Step 5 — XP / 升級 / 訓練接線

三件事：**XP 怎麼進、等級怎麼初始化、訓練怎麼接**。v3 API 把這些都包好了，不必再自寫 VIGILANT 那種 `zVCSF*` kill-actor 管理腳本。

### 7.1 Papyrus API（`CustomSkills.psc`，v3）

全部 `global native`：

```papyrus
int  GetAPIVersion()                                            ; 目前回傳 3
void OpenCustomSkillMenu(string asSkillId)                      ; 開某技能/群組選單
void ShowTrainingMenu(string asSkillId, int aiMaxLevel, Actor akTrainer)
void AdvanceSkill(string asSkillId, float afMagnitude)          ; 依使用量推進技能
void IncrementSkill(string asSkillId)                           ; +1 級
void IncrementSkillBy(string asSkillId, int aiCount)
string GetSkillName(string asSkillId)
int  GetSkillLevel(string asSkillId)
void ShowSkillIncreaseMessage(string asSkillId, int aiSkillLevel)
void DebugReload()                                             ; debug only
```

額外 extension scripts（v3）讓任意 script/alias/magic-effect 監聽事件：
- `CustomSkills_FormExt.psc`：`RegisterForCustomSkillIncrease(Form)` + `Event OnCustomSkillIncrease(string asSkillId)`；`RegisterForCustomSkillBookRead(...)` + `Event OnCustomSkillBookRead(...)`。

### 7.2 init alias script（仿 `CNS_InitScript`）

在 esp 裡建一個 quest（start game enabled），加一個指向 PlayerRef 的 ReferenceAlias，掛上這支 script。它在**首次安裝**時把 level GLOB 設成 `iAVDSkillStart`（=15）、授予基礎 auto-perk，並用版本號 gate（避免每次載入重設）：

```papyrus
Scriptname BL_InitScript extends ReferenceAlias

Perk Property BL_BeastLore_AutoPerk Auto       ; 安裝即給的基礎被動

GlobalVariable Property SkillBeastLoreLevel Auto

int Property CurrentVersion = 1 AutoReadOnly
int KnownVersion = 0

Event OnInit()
    RegisterForSingleUpdate(1.0)
EndEvent

Event OnPlayerLoadGame()
    if KnownVersion != CurrentVersion
        DoUpdate()
    endif
EndEvent

Event OnUpdate()
    DoUpdate()
EndEvent

Function DoUpdate()
    Actor actorRef = self.GetActorReference()
    if KnownVersion < 1
        DoNewInstall()
        actorRef.AddPerk(BL_BeastLore_AutoPerk)
    endif
    KnownVersion = CurrentVersion
EndFunction

Function DoNewInstall()
    int startingLevel = Game.GetGameSettingInt("iAVDSkillStart")   ; = 15
    SkillBeastLoreLevel.SetValue(startingLevel)
EndFunction
```

這就是 `CNS_InitScript` 的精簡版（原檔處理三棵樹 + 四個 auto-perk，結構一模一樣）。`CurrentVersion`/`KnownVersion` 是安裝/升級 gate：日後改版只要 bump `CurrentVersion`，`OnPlayerLoadGame` 就會跑一次 `DoUpdate`。

> 一個慣例做法：再建一個 `extends Quest` 的「ModObjects」純屬性容器 quest（仿 `CNS_ModObjects`），把樹裡的具名 perk 接出來給其他腳本/條件引用——非必要，但讓 form 管理乾淨。

### 7.3 訓練師（一行 TIF fragment）

把現有 NPC 重新利用成訓練師最省事：在那個 NPC 的對話 TopicInfo 上掛一個 fragment，body 只要一行（這就是 `CNS_TIF__TrainingEnthir` 的全部 code）：

```papyrus
;BEGIN CODE
CustomSkills.ShowTrainingMenu("BeastLore", 90, akSpeaker)
;END CODE
```

`ShowTrainingMenu(skillId, maxLevel, trainer)`：第二參數是該訓練師的等級上限（**50/75/90 對應 adept/expert/master**）。CK 對話編輯器會自動生成 fragment 的外殼（`Function Fragment_0(ObjectReference akSpeakerRef)` + `Actor akSpeaker = akSpeakerRef as Actor`），你只填中間那一行。

### 7.4 XP 三路怎麼進

- **用量**：`CustomSkillAdvance_BeastLore` keyword 掛在 perk 的 Modify-Skill-Use entry-point，玩家用相關動作就漲。
- **訓練**：上面的 `ShowTrainingMenu`（花錢）。
- **任意腳本**：直接 `CustomSkills.AdvanceSkill("BeastLore", mag)` 或 `IncrementSkill("BeastLore")`。
- **附魔/藥水（Fortify 技能）**：**非要 SKSE 不可**——得走 4.4 的 fortify MGEF + `ActorValueData/*.toml` + 自寫 native plugin。純 esp/JSON 做不到。

CSF runtime 用 `ratio`/`legendary` GLOB 與 `experienceFormula` 自己算等級與 perk 點數；perk 授予走原版 perk-tree 花點數 UI；perk 效果全是 esp 裡的普通 PERK record。

---

## 8. Step 6 — 在地化

三層**互相獨立**的在地化通道：

| 在地化什麼 | 在哪改 | 怎麼做 |
|------------|--------|--------|
| **技能名/述**（JSON 的 `name`/`description`） | `Data/Interface/Translations/<Plugin>_<LANG>.txt` | `$`-key 對應真文字；換語言檔即翻譯 |
| **perk 名/述**（PERK 的 FULL/DESC） | esp 的 STRINGS 或 inline 文字 | 出一份翻譯版 esp / STRINGS |
| **UI 字串** | 同 Translations 機制（與 MCM 同套） | — |

**Translations 檔格式**：`Data/Interface/Translations/MySkills_ENGLISH.txt`，**UTF-16 LE + BOM、tab 分隔、key↔value**，語言後綴 `ENGLISH`/`CHINESE`/…。**無 fallback 到 ENGLISH**——玩家須備妥對應語言檔。範例（抄 Constellations 的格式）：

```
$BeastLore_Name	Beast Lore
$BeastLore_Description	The study of beasts: how to track them, endure them, and outlast them.
```

翻譯 = 多放一份 `MySkills_CHINESE.txt`（同 key、換 value）。

> 注意一個分裂：**技能名/述（JSON 那層）翻譯靠換 Translations 檔；perk 名/述（esp 那層）翻譯靠換 esp**。兩套通道，VIGILANT 與 Constellations 都一致。一份完整中文化要兩個檔都換。

---

## 9. Step 7 — 測試（最小煙霧測試）

1. **檔案落位檢查**（在地化、JSON、esp 都到位）：
   - `Data/MySkills.esp` 啟用、排在 CSF 之後。
   - `Data/SKSE/Plugins/CustomSkills/SKILLS.json` 與 `MySkills/BeastLore.json` 存在、是合法 JSON（拿掉教學註解！）。
   - `Data/Interface/Translations/MySkills_ENGLISH.txt` 是 UTF-16 LE BOM。
2. **進遊戲、開新檔或乾淨存檔**，按 ESC → Skills，應該看到 Beast Lore 出現在你 `skills[]` 排的位置。技能名顯示正確 = Translations 接上了；顯示成 `$BeastLore_Name` = Translations 沒讀到（檢查編碼/檔名語言後綴）。
3. **console 驗證 GLOB**（GLOB 要有 editor id 才行）：
   - `getglobalvalue SkillBeastLoreLevel` 應為 15（init script 跑過了）。
   - `set SkillBeastLoreLevel to 50` 後重開選單，看等級變化、可點的 perk 數變化。
   - 若你做了 `showMenu` GLOB（路線 B）：`set BeastLoreShowMenu to 1` 直接開選單。
4. **點一個 perk**，看 esp 裡那個 PERK 的效果有沒有生效（entry-point 看數值、ability 看 active effects）。
5. **找訓練師對話**，確認 `ShowTrainingMenu` 跳出訓練界面。
6. **XP 推進**：`CustomSkills.IncrementSkill("BeastLore")` 或做相關動作，看 level 漲、升級訊息（若做了 `showLevelup`）。

> entry-point perk 一進選單就 CTD？回去看 4.1 的 `PerkConditionTabCount` 地雷。

---

## 10. 用 ModForge 生成

依 survey §5/§6.5 的 MVP 結論，把上面的工作分成「ModForge 現在能做」與「還缺的 generator」：

### 現在能生成（既有能力可重用）
- **PERK records**（Step 2.1）：技能樹全部節點 perk（含多階鏈）就是普通 PERK record，ModForge 既有 perk 支援直接適用（`PerkConditionTabCount` 地雷仍適用）。
- **GLOB records**（Step 2.2）：`level`/`ratio`/`legendary` 是簡單 GLOB，要給 editor id——ModForge 能產。
- **KYWD records**（Step 2.3）：`CustomSkillAdvance_<Id>` 等是普通 keyword record。

### 還缺的 generator
- **`<X>.json` 產生器**：把「樹形規格」序列化成 skill JSON。關鍵契合點——`form` 字串是 `"Plugin.esp|FormId"`、load-order 無關，ModForge 只要知道自己產出的 plugin 檔名 + 各 record 本地 FormId 就能填，**與既有 FormId 配置流程天然契合**。
- **`SKILLS.json` 組裝器**：把原版技能字串與 `{ "$ref": ... }` 混排成 root。
- **Translations 檔**（`$`-key + UTF-16 LE BOM）。

### 未來 spec 欄位構想（proposal，非現況）

一個可能的 ModForge spec 片段長相（**僅為後續實作參考，非目前已支援**）：

```jsonc
// PROPOSAL — 尚未實作
{
  "customSkill": {
    "id": "BeastLore",
    "name": "$BeastLore_Name",
    "description": "$BeastLore_Description",
    "experienceFormula": { "useMult": 0.8, "useOffset": 27.0, "improveMult": 2.0 },
    "menu": "SKILLS",                          // "SKILLS" → 併入原版頁；或具名 → 獨立群組
    "insertAfter": "Block",                    // SKILLS.json 排序提示
    "skydome": "DLC01/Interface/INTVampirePerkSkydome.nif",
    "nodes": [
      { "id": "Mastery", "perk": "BL_Mastery", "x": 0.0, "y": 0.0, "links": ["Tracking","Resilience"] },
      { "id": "Tracking", "perk": "BL_Tracking01", "x": -1.2, "y": 1.0, "links": ["Predator"] }
      // perk 用 EDID 引用既有 spec 裡的 PERK；GLOB/KYWD 由 generator 自動帶出
    ]
  }
}
```

ModForge generator 拿到這份 spec 後：自動建 level/ratio/legendary 三個 GLOB + 升級 KYWD、把 `perk` 的 EDID 解析成 `"Plugin.esp|FormId"` 寫進 JSON、emit `SKILLS.json` 與 Translations。**一句話分工**：純 esp + JSON（+ 幾支薄 Papyrus）就能做出一棵接進原版技能頁的完整技能；只有「Fortify-技能附魔/藥水」那條需要額外的 native SKSE plugin（`ActorValueData` + fortify MGEF），屬框架外的進階加值，不在 MVP。

---

## 11. 常見地雷 / Checklist

**Do**
- 鎖定 **v3 JSON** 格式，用 `CustomSkills.psc` v3 API（省去自寫管理 quest）。
- 每棵技能至少 `level`/`ratio`/`legendary` 三個 GLOB，**都給 editor id**。
- `id` 在 JSON、訓練 TIF、console、Papyrus 呼叫處**前後完全一致**（別學 Constellations 的 `HandtoHand`/`HandToHand` 不一致賭容錯）。
- 出貨 JSON 是**合法 JSON**：拿掉所有 `//` 教學註解。
- Translations 存成 **UTF-16 LE + BOM、tab 分隔**，檔名語言後綴正確（`_ENGLISH`）。
- init script 用 `CurrentVersion`/`KnownVersion` gate，首裝才設 level=`iAVDSkillStart`。
- `nodes` 第一個是入口（必填），最多 127 個。
- 多階 perk 在 node 只填**第一階** FormId。
- skydome 可重用 vanilla `DLC01/Interface/INTVampirePerkSkydome.nif` 免自製。

**Don't**
- 別做 entry-point perk 卻把 `PerkConditionTabCount` 留成 0 → 一進選單 CTD。
- 別在 skill 物件裡放 `version`（那是 root 欄位）。
- 別忘了 `x` 正向朝左、`y` 正向朝上（座標反直覺）；別用 `GridX`/`GridY`（那是舊 INI）。
- 別把檔名取成 `SKILLS.json` 卻以為是獨立群組——`SKILLS.json` 會覆寫原版技能頁。
- 別期待 Translations 有 ENGLISH fallback——缺對應語言檔就顯示成 `$key`。
- 別以為純 esp 能做 Fortify-技能附魔——那條一定要 native SKSE plugin + `ActorValueData/*.toml`。
- 別在玩到一半的舊存檔裝技能後就斷定「沒生效」——init script 靠 `OnPlayerLoadGame`/`OnInit`，乾淨存檔或重載一次再判。

---

> 深水區（兩代格式斷層、舊 INI 對照、VIGILANT/GLENMORIL 案例、完整 schema 欄位表）見 [`custom-skills-framework.md`](custom-skills-framework.md)。本指南只負責「照著做就能跑」。
