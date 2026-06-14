# Mod Survey — Extended Encounters (v1.6.7)

對象：`Extended Encounters.esp`（Nexus 44810）。本文從 ModForge（JSON spec → `.esp` 生成器）角度拆解它怎麼運作、哪些機制可生成 / 需新支援 / 純參考。

> 記錄方法：只用 ModForge lazy CLI verb（streamed/overlay-safe）跑這顆 esp，沒有整顆載入 Skyrim.esm。

---

## 1. 這個 mod 做什麼 + 怎麼運作

Extended Encounters 往 Skyrim 既有的隨機遭遇池裡塞了 ~140 個新遭遇（路邊/荒野/地點/危險地點），讓世界「動起來」而不增加存檔 bloat。它**完全不靠 cell 裡預擺的東西**，而是靠 **Story Manager (SM) 事件 → quest → ReferenceAlias → AI package**，並在 quest 跑起來的瞬間用 Papyrus `MoveTo` 把遭遇「組裝」到玩家附近的合法 navmesh 上。

record census（`dump`）很能說明性質：

| 記錄類型 | 數量 | 角色 |
|---|---|---|
| `Quest` | 330 | 每個遭遇 = 一個 SM-managed quest |
| `Package` | 490 | 每個遭遇的 NPC 行為（travel / sandbox / scavenge / orbit…） |
| `GlobalShort` | 357 | MCM 開關 + 各遭遇的 toggle/權重 |
| `StoryManagerQuestNode` | 30 | 把這些 quest 掛到 SM 樹的葉節點 |
| `StoryManagerBranchNode` | 3 | `EE_ScriptEvent` / `EE_ChangeLocation` / `EE_LocationInteraction` |
| `LeveledNpc` | 8 | 「random enemy / random daedra」對手 |
| `Npc` | 20 | 少量自製 NPC（Scholar / Bard / Scavenger / Prisoner…），其餘全用 vanilla actor |
| `Outfit` `LeveledItem` `Faction` `Message` `SoundDescriptor` `Activator` | 少量 | 服裝隨機化、旅商貨物、音樂、MCM 訊息 |

`scnscan` = **0 個 scene（含非對話 action）**、`gamedata` 報 **dialogue_lines=0**。也就是說：這顆 mod **不用 SCEN、不用對話**。表演靠 package + 少量 Papyrus，不是 ModForge 既有的 scene/dialogue 路線。

### 觸發 → 選擇 → 生成 → 收場 的流程

1. **觸發（SM 事件）**：`smtree <esp>` 回 `(0 event roots)` — 因為 event root 在 Skyrim.esm 裡。EE 是 **additive** 把自己的 branch/quest node 接到 vanilla 的 SM event root（同 memory `story-manager-kill-recipe`：SMBN additive-parent vanilla root）。三條 branch：
   - `EE_ScriptEvent`（0x0008BD）— 由 Papyrus `SendStoryEvent` 主動丟事件（dynamic WE / situation encounters）。
   - `EE_ChangeLocation`（0x0008C6）— 玩家換 location 時 vanilla 的 Change Location event。
   - `EE_LocationInteraction`（0x000A08）— sleep/wait/fast-travel 進危險地點。
2. **選擇（SM quest node + 條件）**：30 個 `StoryManagerQuestNode` 依**地點類型**分流，EditorID 本身就是路由表：
   - `EE_Road` / `EE_Wilderness` / `EE_Dragon` — 路邊與荒野。
   - `EE_WI_LocType*`（WI = waiting/idle，Change-Location 觸發）：`Town/Inn/Store/Temple/Cemetary/Castle/BanditCamp/ForswornCamp/WarlockLair/VampireLair/MilitaryFort/City/House/Settlement…`。
   - `EE_LI_LocType*`（LI = location interaction，危險地點觸發）：`Inn/BanditCamp/ForswornCamp/WarlockLair/MilitaryFort/OrcStronghold/VampireLair/Settlement/Town/City`。
   每個 node 底下掛一票候選 quest，SM 用 quest 的條件 + `GlobalShort` 權重隨機挑一個跑。`questdiag 0x000809`（EE_WE006 Faendal hunting）顯示：`event=SCPT`、`filter="Wilderness Encounters\"`、stages `0(StartUp)/10/100/255(ShutDown)`、**0 objectives、log 全空** — 純骨架 quest，不是任務日誌型 quest。
3. **生成（alias + MoveTo）**：quest 跑起來時——
   - quest 帶一組 **ReferenceAlias**：`WETrigger`、`WEScene1..4`、`WESceneCenter`（隱形 XMarker）外加 `NavmeshTester`（`EE_NavmeshTester` actor, 0x000AA9）以及遭遇演員 alias。
   - QF fragment（`EE_QF_EE_DynamicWE_010465F2`）的 spawn 演算法是本 mod 的核心 trick：把 `NavmeshTester` `MoveTo` 到玩家周圍隨機 (-6000..6000) 偏移、`while` 迴圈確保距離 >4000、再 `EnableAI(False)/EnableAI()` 讓它**吸附到最近的 navmesh**，然後把所有 scene marker `MoveTo(NavmeshTester)`，最後 `Delete()` tester。→ 用一隻拋棄式 actor 找到「玩家附近的合法可走點」，無需預擺 cell。
   - 演員透過 alias 的 `Fill`（Unique actor / `LeveledNpc` list / vanilla actor）填入，然後綁 AI package 走過去。
4. **演出（AI package）**：490 個 package 全部是 **vanilla PackageTemplate 的薄包裝**，data input 指向 alias-reference。例：
   - `EE_WE001OrbitPlayerNoCombat`（0x000800）：`PackageTemplate → 015B84:Skyrim.esm`、flags `OncePerDay,IgnoreCombat`、4 個 data（`PackageDataLocation` target=`AliasForReference data=28` + 三個距離 float 3000/8000/4000）→ 兩隻龍繞玩家飛。
   - `EE_WE004TravelBackAndForth`（0x000806）：`PackageTemplate → 09BD86:Skyrim.esm`，兩個 `PackageDataLocation`（`AliasForReference data=9 / data=10`）讓 NPC 在兩個 alias 之間來回。
   - PF fragment（`EE_PF_EE_WE096Scavenge`）：`Corpse.GetReference().RemoveAllItems(akTransferTo=Scavenger)` — 拾荒者走到屍體把東西搬走。
5. **收場（cleanup）**：玩家走遠 / location 變了，alias 腳本停 quest，把演員 `Delete()`、marker `MoveToMyEditorLocation()` 歸位（`EE_WIPlayerScript.OnLocationChange`、`EE_DynamicWEStarterScript` 用 `RegisterForSingleUpdate(RandomFloat(6h..24h))` 排程下一次）。零殘留 = 不 bloat 存檔。

危險地點分支另有 `EE_SituationEncountersScript`（ReferenceAlias）：玩家 sleep/wait/fast-travel 時讀 `PlayerLocation.HasKeyword(LocTypeBanditCamp/...)` 算出 `LocationType`（1..11 危險、99 安全/已清），再 `GenerateEncounter()` 用 `LvlBandit*/LvlVampire/...` ActorBase 在玩家附近生敵人。

---

## 2. 關鍵 record 與模式

- **SM 三段路由**：`StoryManagerBranchNode`（事件種類）→ `StoryManagerQuestNode`（依 location-type keyword 分流的葉）→ 一票候選 `Quest`。Node 本身是 additive override，接到 Skyrim.esm 的 vanilla event root。**EditorID 命名即文件**（`EE_WI_LocTypeBanditCamp`）。
- **骨架 quest**：`type=None`、`event=SCPT`、有 SM filter、stage 只有 StartUp/中間/ShutDown、**沒有 objective/log**。它存在的唯一目的是「掛 alias + 跑 QF fragment 組裝遭遇」。
- **ReferenceAlias = 組裝插槽**：隱形 XMarker alias（trigger / 多個 scene point / center）+ NavmeshTester + 演員 alias。`Fill` 來源 = Unique NPC / `LeveledNpc` list / vanilla actor。
- **navmesh-tester spawn 演算法**（純 Papyrus，本 mod 招牌）：拋棄式 actor `MoveTo` 隨機偏移 → `EnableAI` 吸附 navmesh → marker 跟著它 → delete。比 ModForge 既有「預擺 placement + refpos」更動態。
- **package = vanilla template 薄包裝**：`PackageTemplate → Skyrim.esm:0x…`，data input 全指 `AliasForReference`。沒有自製 procedure tree（`ProcedureTree: 0 branch(es)`）。
- **LeveledNpc 當「random enemy」**：`EE_WE003 Uthgerd fighting a random enemy` 的對手是 `Lvl*` list，靠 LVLN 的 chanceNone/權重做隨機。
- **GlobalShort 海**（357 個）：MCM 開關 + 每遭遇的 enable/權重，Papyrus 在丟 story event 前先 gate。

---

## 3. 對 ModForge 的參考價值

### 可生成（ModForge 已有對應支援）

- **掛 SM 事件的骨架 quest**：`QuestStoryEventSpec`（`Spec.StoryManager.cs`，`event` + `conditions` + `keyword`）已能把一顆 quest 接到 vanilla SM event root，這正是 EE 每顆遭遇 quest 的型態。✅
- **AI package（vanilla template 包裝）**：`Spec.Packages.cs` / `Spec.Packages.Templates.cs` 已支援 `Template` + Travel/Sandbox/Patrol/Follow/Escort/Eat/UseItemAt 等 data input（指向 placed ref 或 in-spec placement）。EE 的 Orbit/Travel/Sandbox/Scavenge 大多落在這套裡。✅（cf. memory `scene-playidle-recipe`/package 系列）
- **LeveledNpc / LeveledItem**：`LeveledNpcSpec`/`LeveledItemSpec`（`Spec.Items.cs`）已支援 chanceNone + 權重 entries；spawn 也能吃 LVLN 當 base（`Spec.World.cs`）。EE 的「random enemy」可直接表達。✅
- **ReferenceAlias + fill + 條件 + alias 腳本**：`QuestAliasSpec`（`Fill`、`Conditions`、`Script`/`ScriptSource`/`ScriptProperties`）已能描述 EE 的演員/marker alias 與其腳本。✅
- **Outfit / Faction / Message / Activator / 自製 NPC**：全是 ModForge 既有 record 路線。✅

### 需新支援（缺口）

- **獨立 SM branch/quest-node 樹**：ModForge 目前是「quest 自己宣告 storyEvent」掛到 vanilla root；EE 卻自建 `StoryManagerBranchNode` + 30 個 `StoryManagerQuestNode`，**依 location-type keyword 分流、底下掛一票候選 quest 做加權隨機選擇**。要生成這種「多候選 + 條件路由」的 SM 子樹，需要新的 spec（branch/node 結構 + 候選 quest 清單 + 各自條件/權重）。⚠️ 需確認 `Generator.Build.StoryManager.cs` 目前能生到哪一層（branch node? 多 quest 候選?）— 這是最大缺口。
- **navmesh-tester 動態 spawn helper**：EE 的「拋棄式 actor `MoveTo` 隨機偏移 → `EnableAI` 吸 navmesh → marker 跟隨 → delete」是一段可重用的 Papyrus 樣板。ModForge 目前偏向預擺 placement（cf. memory `programmatic-navmesh`、`refpos`）；若要做「玩家附近隨機生成」這類遭遇，值得把這段做成可生成的 alias-script 樣板（搭配隱形 marker alias 自動生成）。⚠️
- **「一個 quest = 多 alias marker（trigger/scene1..4/center）」的成組 alias 樣板**：可生成，但目前要手寫每個 alias；值得一個 high-level「encounter scaffold」糖衣自動鋪 marker alias + cleanup 腳本。⚠️

### 純參考（設計觀念，不必生成）

- **「marker + MoveTo + delete，不預擺 cell」的零-bloat 哲學**：解釋了為何隨機遭遇不該往 cell 塞 placement。
- **EditorID 即路由表的命名法**（`EE_WI_LocType*` / `EE_LI_LocType*`）：對 AI-agent 友善的 spec 命名值得借鏡。
- **GlobalShort 開關 + MCM gate 模式**：每 feature 一個 global toggle，Papyrus 丟 story event 前先檢查。
- **「骨架 quest（無 objective/log）純當 SM 容器」**：提醒 ModForge 的 SM quest 不一定要有任務日誌。

### 相關既有筆記

- memory `story-manager-kill-recipe`：SMBN additive-parent vanilla root — EE 的三條 branch 用同一招。
- memory `programmatic-navmesh` / `refpos`：與 EE 的 navmesh-tester 動態定位互補（一個是預擺，一個是 runtime 找點）。
- package 系列（`dispatcher-magic-trigger`、`scene-playidle-recipe`）：EE 證明大量遭遇可以**只靠 package 不靠 scene/dialogue** 跑起來。
- memory `sm-quest-journal-progression`：EE 反例 — 它的 SM quest 刻意**不**進日誌（無 startUpStage objective）。

---

### TL;DR

機制 = **vanilla SM event root → EE 自建 branch/quest-node 樹（依 location keyword 路由 + 加權隨機選候選 quest）→ 骨架 quest 跑 QF fragment：用拋棄式 NavmeshTester actor `MoveTo` 玩家附近吸 navmesh 找合法點 → 把隱形 marker alias 移過去、依 alias fill 演員（含 LeveledNpc）→ 綁 vanilla-template AI package 演出 → 走遠即 delete 歸零**。對 ModForge：SM 事件掛載 / package / LeveledNpc / alias 都已可生成；最大缺口是「多候選 + keyword 路由的獨立 SM branch/quest-node 子樹生成」與「navmesh-tester 動態 spawn Papyrus 樣板」，前者建議當下一個功能優先評估。
