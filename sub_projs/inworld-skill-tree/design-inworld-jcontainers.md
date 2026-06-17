# 設計：方案 A+ — in-world 3D 樹（Campfire 引擎）+ JContainers per-NPC 狀態

← [sub_proj README](README.md)

**這是什麼**：把 idea #20 的[方案 A（純效果成長）](README.md#方案-a純效果不開-ui最簡單現在就能做)、**Campfire/Frostfall 的 in-world 3D 技能樹**（[替代路線](README.md#替代路線待主力機查證見-wait_user)）、與 **JContainers per-NPC 狀態**三者綜合成一條可實作路線。使用者選定方向（2026-06-17），**玩家與 NPC 通用**。

**玩家版 vs NPC 版（兩種複雜度）**：
- **玩家版 = Campfire 原生模式，零橋接**：玩家對營火開樹、點星、全域 GLOB 記 rank——這正是 Frostfall Endurance 樹的現成做法。只有一個玩家、全域 GLOB 不衝突，**不需 JContainers**。是 baseline，現成可生成。
- **NPC 版 = 玩家版 + per-NPC 狀態橋接**：多個 NPC 共用同一棵樹 base，全域 GLOB 會互相污染 → 需 JContainers `JFormDB` 存每個 NPC 的狀態 + 開樹時用 session GLOB 借位（§三）。這是本檔的新設計增量。

**為何這個組合而非方案 B/C/D**：
- **繞開方案 C 的 CSF Scaleform UI**：用 Campfire 的世界內 3D 星樹當 UI，玩家端依賴只有 `Campfire.esm`，不需 CSF 的 native dll + Scaleform JSON + UTF-16 翻譯檔。產物全是 ESP record + 薄 Papyrus，最貼合 ModForge「JSON spec → 技能樹」。
- **繞開方案 B 的 GLOB 海（381 records）**：用 JContainers `JFormDB` 以 NPC Form 為 key 存巢狀狀態，取代「每 NPC × 每節點一個 GLOB」。
- **繞開方案 D 的 SKSE native 層**：不需 Proteus 那種 Actor 控制轉移。

> **信心標註**：Campfire 機制來自 mod-survey 對 `Campfire.bsa` 的原始碼逆向（[campfire.md](../mod-survey/findings/campfire.md)，高信心）；JContainers API 來自原始碼（[jcontainers.md](../mod-survey/findings/jcontainers.md)）；Frostfall 是活範例（[frostfall.md](../mod-survey/findings/frostfall.md)）。**但「對 NPC 而非營火開樹」「per-NPC 狀態橋接」是本檔的新設計推論——標 ⚠️ 者為待驗 unknown，未經主力機 / 原始碼確認前不可當事實。**

---

## 一、三者各自的角色

| 層 | 由誰提供 | 職責 |
|---|---|---|
| **perk 效果層** | 方案 A（`Actor.AddPerk`） | NPC 實際獲得 perk 的傷害/被動效果；與星視覺解耦 |
| **UI 層（3D 星樹）** | Campfire Skill System | 玩家準心對星 `OnActivate` 點 perk；整棵樹 spawn 在世界座標、轉向面對玩家、走遠 480u 自毀 |
| **持久狀態層** | JContainers `JFormDB` | 每個 NPC 一份巢狀技能狀態，存檔安全、可 `writeToFile` 匯出 |
| **成長來源** | 方案 A | 任務進度 / 好感度 / 戰鬥 XP 累積 → 寫進 JFormDB |

**關鍵解耦**（沿用 Campfire 原生設計）：星點視覺狀態 ← GLOB；perk 效果 ← ability/MGEF。星只是 GLOB 的「可點介面」。本設計在此之上再加一層：**GLOB ↔ JFormDB 的 per-NPC 橋接**。

---

## 二、資料模型（JFormDB）

```
JFormDB storageName = "ModForgeNpcSkills"
  NPC_Form (JFormMap key)
    └─ <skillId>                       ; 一個 NPC 可有多棵樹
         ├─ level        : int
         ├─ ratio        : float       ; 0–1 升級進度
         ├─ perkPoints   : int         ; 未花點數
         └─ nodes                      ; JMap: nodeId → rank
              ├─ "Adaptation"  : 2
              ├─ "Windbreaker" : 0
              └─ ...
```

存取（Papyrus，pattern 來自 jcontainers.md §三）：
```papyrus
; 讀某 NPC 某節點 rank
int rank = JFormDB.solveInt(npc, ".ModForgeNpcSkills.Endurance.nodes.Adaptation", 0)
; 寫
JFormDB.solveIntSetter(npc, ".ModForgeNpcSkills.Endurance.nodes.Adaptation", 2, true)
```

優點：① 無 128 陣列上限、可巢狀；② Form-as-key 不需字串拼 FormID；③ `JDB.writeToFile` 可把全 NPC 技能狀態匯出 JSON（debug / 跨存檔遷移 / 外部編輯）。

---

## 三、開樹：觸發載體（不綁營火）+ 對 NPC 橋接

### 3.1 觸發載體抽象（開樹入口）

Campfire 把開樹綁死在營火 ACTI 的 OnActivate。本設計把「開樹入口」**抽象成任意觸發載體**——營火只是其中一種：

| 載體 | 機制 | target 解析 | 適用 |
|---|---|---|---|
| **自訂 activator**（石頭/樹/祭壇/營火…）| `OnActivate` | 載體綁定的 actor（玩家，或這顆石頭代表的某 NPC）| 場景化「修練處」 |
| **法術 SPEL**（瞄準施放）| script-effect MGEF | `Game.GetCurrentCrosshairRef()` ／被擊中的 actor | **最通用**：施在誰、開誰的樹 |
| **物品 MISC / 書**（使用）| `OnEquip` / 讀書 | 玩家，或彈選單選 target | 道具感 |

**法術路線最優雅**：它把玩家版/NPC 版統一成同一入口——一個「管理技能」法術，**自施 → target＝玩家 → 開玩家樹（全域 GLOB）；瞄準 NPC → target＝該 NPC → 開 NPC 樹（JFormDB 橋接）**。`target 是不是玩家`是唯一分歧點。

不論哪種載體，職責只有**決定兩件事**：① **target actor**（樹屬於誰、狀態存哪）② **spawn 位置**（樹在哪展開，通常 target 面前）。決定後一律進 3.2 統一流程。

### 3.2 對 NPC 開樹流程（target ≠ 玩家）

target 解析為某 NPC 後，橋接靠一組**全域 session GLOB**（僅描述「當前正在管理的那一個 NPC」），把 Campfire 的全域單例假設與 per-NPC 持久狀態縫起來：

```
觸發載體（§3.1）解析出 target ＝ NPC X
  │
  1. LoadNpcStateToGlobs(npc, skillId)
  │     JFormDB[npc][skillId].nodes → 灌進 Campfire node 的 required_perk_rank_global（session GLOB）
  │     JFormDB[npc][skillId].perkPoints → 灌進 perkPoint GLOB
  │
  2. ShowPerkTreeForActor(npc)              ⚠️ U1：見下方 unknowns
  │     spawn controller 在 NPC 位置（取代營火）→ Campfire 讀 session GLOB 重建星亮/暗
  │
  3. 玩家點星（Campfire 原生 OnActivate → IncreasePerkRank）
  │     寫回 session GLOB（Campfire 原生行為，不需我們改）
  │
  4. 關樹（走遠 480u 自毀 / Exit bug）→ OnPerkTreeClosed
        SaveGlobsToNpcState(npc, skillId): session GLOB → JFormDB[npc]
        SyncPerks(npc): 對每個 rank>0 的 node → Actor.AddPerk；rank 歸 0 → RemovePerk
```

**為何 session GLOB 只需一組**：因為「同一時間只會管理一個 NPC」（玩家一次只對一個 NPC 開樹）。Campfire 用的那組全域 GLOB 在開樹瞬間被當前 NPC「借用」，關樹存回 JFormDB 即釋放。這正是方案 B「代理選單（轉移模型）」的精神，但 GLOB 數量從 `381 = 127 NPC × 3` 降到 `每棵樹的節點數 × 一組`（與 NPC 數無關）。

**玩家版（baseline，零橋接）**：玩家用就是 Campfire 原生流程——透過載體（自施法術／活化物／真營火）開樹、點星寫**全域 GLOB**、效果直接套在玩家身上。沒有「借位/存回」步驟（全域 GLOB 就是玩家的真相，只有一個玩家不會衝突），也**不需 JContainers**。換言之 §二/§三 的 JFormDB + session GLOB 橋接是 NPC 專屬增量；玩家版砍掉這層即可，兩版共用同一套 ACTI/node/line/controller record 與 perk 效果層。**先做玩家版驗證 in-world 樹本身，再加 NPC 橋接**是最穩的路徑。

---

## 四、純效果成長（方案 A 本體，可獨立先做）

不論 UI 層做不做，效果層現在就能做：

- **perk 套用**：`SyncPerks(npc)` 依 JFormDB 的 node rank → `AddPerk`/`RemovePerk`。冪等，可在 NPC OnLoad / 升級時重跑。
- **成長來源**（寫進 JFormDB，玩家不必配點時的自動路線）：
  - 任務進度 gate（任務 stage → `solveIntSetter` 升 level + 自動點關鍵 node）
  - 好感度 gate（沿用 Sofia F6 GLOB 藍圖，見 [README 方案 A](README.md#方案-a純效果不開-ui最簡單現在就能做)）
  - 戰鬥 XP（NPC OnHit / 殺敵 event 累積 ratio）
- **適合先落地的 MVP**：純效果成長 + 任務 gate，**完全不碰 Campfire/in-world UI**——這條現在就能做，是 idea #20 原本的「方案 A」。in-world UI 是其上的玩家配點加值層。

---

## 五、待解 unknowns（阻擋 UI 層拍板，多需主力機 / 原始碼）

| # | 問題 | 為何重要 | 怎麼查 |
|---|------|---------|--------|
| **U1** | Campfire 能否**不靠營火、對任意 ref/位置** spawn 樹？三種觸發載體（活化物/法術/物品，§3.1）都歸結到「在 target actor 位置 spawn 一棵樹」這一核心能力；`ShowPerkTree()` 是 `CampCampfire` 的方法（綁營火本體）。 | 決定整條「對任意 target 開樹」可不可行（玩家版可用真營火繞過，NPC/法術/物件版必須解此題）。退路：開樹時在 target 腳下暫生隱形 dummy 營火 → ShowPerkTree → 取下（hacky 但或可行）。 | 讀 `CampCampfire.psc` + `_Camp_PlaceableObjectBase` 看 controller spawn 能否獨立呼叫；主力機 `Campfire.bsa` |
| **U2** | `IncreasePerkRank()` 寫的 `required_perk_rank_global` 是 node **base form 屬性**還是 instance？多 NPC 共用同一棵樹 base，session GLOB 是否真能隔離不互污染？ | 決定 session GLOB 橋接成不成立（本設計地基）。 | 原始碼確認 GLOB 是否全域單例；若是，橋接成立（開樹前灌、關樹後存）|
| **U3** | Campfire 的 **perkPoint 消費 / gate**（點數不足不能點、prerequisite 未點不能點下游）邏輯在哪？ | NPC 配點要不要受點數限制；Frostfall 用 `EndurancePerkPoints`，需逆向其 gate。 | 讀 Frostfall `_Frost_*` perk point 腳本 + Campfire node gate |
| **U4** | ModForge 能否生成這套 record：ACTI（node/line/controller）+ VMAD script 屬性互指（downstream node/line）+ PositionRef layout markers + register quest alias？ | 決定產線可不可生成；campfire.md §4 說「大致可，缺 PositionRef layout 模板」。 | code pass `src/`，確認 ACTI+VMAD 屬性互指 + cell ref layout 能力 |
| **U5** | JContainers `JFormDB` 的生成 pattern 是否在 ModForge script 生成能力內？（retain/release 生命週期需成對）| 持久層能否生成。 | code pass；jcontainers.md 標「可生成（推斷），未查 src/」|

---

## 六、ModForge 後端需新增（沿用 campfire.md §4 + JContainers pattern）

| 項目 | 說明 | 來源 |
|---|---|---|
| PerkNode / PerkLine / Controller **ACTI** + VMAD script 屬性互指 | 普通 ACTI 掛 `CampPerkNode` script + downstream 屬性；與 perk-conditiontabcount 同類路徑 | campfire.md §4 ✅可生成 |
| node rank **GLOB**（session，一組） | `required_perk_rank_global` + `_max`，**與 NPC 數無關** | campfire.md §4 ✅ |
| perk description **MESG** | 星的浮動說明文字 | campfire.md §4 ✅ |
| **PositionRef layout 模板** | 一組相對 marker（星擺位）+ ACTI 屬性互指拓樸 | campfire.md §4 ⚠️ 缺模板（唯一明確缺口）|
| register **quest** + `CampPerkSystemRegister` alias | 一行 `CampUtil.RegisterPerkTree` 把樹掛進系統 | campfire.md §3 ✅ |
| **觸發載體** record（§3.1）：SPEL + script-effect MGEF（瞄準法術）／自訂 ACTI（石頭/樹/祭壇）／MISC（物品） | 解析 target actor + spawn 位置 → 呼叫開樹；法術版最通用、統一玩家/NPC | ⚠️ 新增，視 U1 |
| JFormDB 存取 + GLOB↔JFormDB 橋接 **Papyrus** | LoadNpcStateToGlobs / SaveGlobsToNpcState / SyncPerks | jcontainers.md §三 ⚠️ U5 |
| 星/線/背板 **NIF** | 重用 Campfire 的（依賴 Campfire.esm 引其 form），免自製美術 | campfire.md §4 ✅ |

---

## 七、實作分期建議

1. **Phase 0（現在可做，零 unknown）**：方案 A 純效果成長 MVP——JFormDB 資料模型 + SyncPerks + 任務/好感度 gate。不碰 Campfire。先驗證「NPC 靠狀態自動長 perk」。
2. **Phase 1：玩家版 in-world 樹（繞過 U1/U2）**——照 Frostfall 模式掛一棵玩家樹到營火（Campfire 原生、全域 GLOB、零橋接）。驗證 in-world 星樹本體可生成可運作；只觸及 U4（generator）不觸及 U1/U2。
3. **Phase 2（待 U1/U2）**：NPC 版橋接——session GLOB + JFormDB + 對 NPC 開樹。需先在主力機釐清 U1（對任意 ref 開樹）/ U2（session GLOB 隔離）。
4. **Phase 3（待 U4 收尾）**：ModForge generator——把上述 record 從 spec 自動產出，補 PositionRef layout 模板。
