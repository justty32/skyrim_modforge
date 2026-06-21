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
  - 好感度 gate（沿用 Sofia F6 GLOB 藍圖，見 [README 方案 A](README.md#方案-a純效果不開-ui最簡單現在就能做)）✅ 已落地：`persist`/`syncPerks` 的 `gate: {global, atLeast?, atMost?}`
  - 戰鬥 XP（NPC OnHit / 殺敵 event 累積 ratio）
- **適合先落地的 MVP**：純效果成長 + 任務 gate，**完全不碰 Campfire/in-world UI**——這條現在就能做，是 idea #20 原本的「方案 A」。in-world UI 是其上的玩家配點加值層。

---

## 五、待解 unknowns（阻擋 UI 層拍板，多需主力機 / 原始碼）

| # | 問題 | 為何重要 | 怎麼查 |
|---|------|---------|--------|
| ~~**U1**~~ ✅ **已解（2026-06-21 原始碼）** | Campfire 能否不靠營火、對任意 ref/位置 spawn 樹？ | **答：能。放置引擎完全與營火無關**——`CampPerkNodeController extends _Camp_PlaceableObjectBase`，其 `PlaceObjects()`（`campperknodecontroller.psc:192`）+ `_Camp_ObjectPlacementThreadManager.PlaceObject()`（`:165`）全部相對 **`self`（被 spawn 的那個 ref）自身的 `CenterObject`/`OriginAng`** 佈局，鏈中沒有任何 campfire 參照。唯一的營火耦合在 `CampCampfire.ShowSkills`（`:967` gate「火要點燃」）與 `ShowPerkTree`（`:1000` `self.PlaceAtMe(nc)` 以營火為 anchor）。**消費端只要 `anyRef.PlaceAtMe(<CampPerkNodeController>)` 即在該 ref 位置長出整棵樹，零營火**。退路 dummy 營火**不需要**。⚠ 唯一代價：campfire-less spawn 的拆除（480u 自毀 / `OnCellDetach` failsafe 與營火無關、仍有效）要消費端自驅。 |
| ~~**U2**~~ ✅ **已解（2026-06-21 原始碼）** | rank GLOB 是 base-form 屬性還是 instance？session GLOB 橋接成不成立？ | **答：是每個 node 一個 `GlobalVariable` property（CK 指派），而 GLOB record 本身就是全域單例 + 存檔持久**（`campperknode.psc:4-7` `required_perk_rank_global`）。node 自身無 per-instance rank 狀態，`current_rank` 只是每次 spawn 從 GLOB 重讀的快取（`:64`），重開樹靠 GLOB 重建星亮/暗（`:64-67` `if current_rank>0 → PlayAnimation("OwnedWild")`）。**⇒ session GLOB 橋接完全成立（開樹前把 JFormDB 灌進 GLOB、關樹後存回），這正是 Campfire 自己的做法。** |
| ~~**U3**~~ ✅ **已解（2026-06-21 原始碼，Frostfall 免裝）** | perkPoint 消費 / gate 邏輯在哪？ | **答：整套 spend+gate 在 Campfire 自己的 `CampPerkNodeControllerBehavior.psc`，不在 Frostfall**。流程：星 `OnActivate`→`controller.NodeActivated(self)`（`campperknode.psc:46`）→ gate（`campperknodecontrollerbehavior.psc:25-60`）：可買 iff **起始 node 或下游(child) node 已買** 且 未滿 rank 且 `required_perk_points_available>0`；確認 Yes/No 後 `IncreasePerkRank()` + 點數池 `-1` + `SendEvent_CampfirePerkPurchased()`（`:117-124`）。公開 API = `CampUtil.RegisterPerkTree(controller, name)`（`camputil.psc:1287`）+ `GetPlacementSystem()`（`:131`）。**消費端只負責「賺點數」（增 `required_perk_points_available` GLOB）；spend/gate/視覺全是 Campfire 的。** ⚠ 修正 campfire.md 兩個舊錯：不是 `OnActivate→IncreasePerkRank` 直連（中間有 controller 確認選單）；gate 是**下游 child 已買**不是「parent perk rank」（Frostfall 樹根在底、`downstream_node` 指向原點）。 |
| ~~**U4**~~ ✅ **已解（2026-06-21 `src/` code-pass）** | ModForge 能否生成這套 record：ACTI（node/line/controller）+ VMAD script 屬性互指（downstream node/line）+ PositionRef layout markers + register quest alias？ | **答：能，全部落在現有能力域，無底層缺口。** 逐項：① **ACTI** = `ActivatorSpec`（name/model/keywords/sound/altTex，`Spec.Items.cs:125`）。② **script 屬性互指** = 頂層 `scripts:`（`ScriptAttachSpec.targetEditorId`）→ `AttachOneScript`/`FillProperties`/`MakeObjectProp`（`Generator.Build.Scripts.cs:26,231,263`）：`type:"object"` 屬性以 `ObjectEditorId` 經 `formKeyByEd` 解析**任意 in-spec record**——`downstream_node/line` 指另一個 node/line ACTI base 完全成立。③ **PositionRef markers** = `PlacementSpec`（任意 base 或 `kind:"xmarker"` helper，置於 cell 給定 `position` x/y/z + `rotation`，可命名 editorId + persistent；`Generator.Build.Placements.cs:53-66,171-183`）。④ **controller script 指向 PositionRef ref**：build 順序 `BuildActivators(62)` → `BuildPlacements(109)`（把 placement editorId 寫進 `formKeyByEd`）→ `AttachScripts(125)`，故 controller 的 object 屬性能解析到 placed marker ref（`Generator.Build.cs:62,109,125`）。⑤ **register quest alias** = `QuestAliasSpec.Script`/`ScriptSource`/`ScriptProperties`（PropertySpec，可 object 綁 controller ACTI base；`Spec.StoryManager.cs:40`）→ `CampPerkSystemRegister extends ReferenceAlias` + `required_node_controller`/`mod_name` 屬性照搬。⑥ **NIF** 重用 Campfire.esm form。<br>**唯一真缺（不阻擋，屬 Phase 3 generator build，非 primitive 缺口）**：沒有「技能樹 layout 模板」spec 糖——目前要手寫每個 PositionRef marker 的相對 x/y/z + 手接 12 槽 controller，能跑但極冗長易錯。一個高階 `skillTree:`（topology → 自動算相對座標 + 生 controller/node/line/marker/register-quest）是 generator 該長出的便利層，**設計上已無 unknown**。 |
| ~~**U5**~~ ✅ 已解（2026-06-18 離線實作） | JContainers `JFormDB` 的生成 pattern 是否在 ModForge script 生成能力內？（retain/release 生命週期需成對）| 持久層能否生成。 | **答：是，且 retain/release footgun 設計上繞開。** 已實作結構化 `persist`/`syncPerks` 對話欄位（`Spec.Persist.cs` + `Generator.JContainers.cs`），只生成 **root-DB path API**（`JFormDB.solveXxxSetter`/`solveInt`）——JContainers 自管、隨存檔持久，**沒有 `JValue.object()/retain()/release()` handle 要成對**。host=對話 TIF fragment（key=speaker/player）；form/perk 值走 VMAD object property。12 測綠、example `npc_skill_persist_spec.json`。⚠ 編 `.psc` 需 JContainers headers 進 `MODFORGE_PAPYRUS_BASE`（主力機，見 WAIT_USER）|

---

## 六、ModForge 後端需新增（沿用 campfire.md §4 + JContainers pattern）

| 項目 | 說明 | 來源 |
|---|---|---|
| PerkNode / PerkLine / Controller **ACTI** + VMAD script 屬性互指 | 普通 ACTI 掛 `CampPerkNode` script + downstream 屬性；與 perk-conditiontabcount 同類路徑 | campfire.md §4 ✅可生成 |
| node rank **GLOB**（session，一組） | `required_perk_rank_global` + `_max`，**與 NPC 數無關** | campfire.md §4 ✅ |
| perk description **MESG** | 星的浮動說明文字 | campfire.md §4 ✅ |
| **PositionRef layout 模板** | 一組相對 marker（星擺位）+ ACTI 屬性互指拓樸 | campfire.md §4 ⚠️ 缺模板 — **U4 已澄清**：底層 placement + 屬性互指能力齊全（`PlacementSpec` + `object` script-prop），缺的只是「topology → 自動算相對座標」的高階 spec 糖（Phase 3 generator build，非 primitive）|
| register **quest** + `CampPerkSystemRegister` alias | 一行 `CampUtil.RegisterPerkTree` 把樹掛進系統 | campfire.md §3 ✅ |
| **觸發載體** record（§3.1）：SPEL + script-effect MGEF（瞄準法術）／自訂 ACTI（石頭/樹/祭壇）／MISC（物品） | 解析 target actor + spawn 位置 → 呼叫開樹；法術版最通用、統一玩家/NPC | ⚠️ 新增，視 U1 |
| JFormDB 存取 + GLOB↔JFormDB 橋接 **Papyrus** | LoadNpcStateToGlobs / SaveGlobsToNpcState / SyncPerks | jcontainers.md §三 ⚠️ U5 |
| 星/線/背板 **NIF** | 重用 Campfire 的（依賴 Campfire.esm 引其 form），免自製美術 | campfire.md §4 ✅ |

---

## 七、實作分期建議

1. **Phase 0（現在可做，零 unknown）**：方案 A 純效果成長 MVP——JFormDB 資料模型 + SyncPerks + 任務/好感度 gate。不碰 Campfire。先驗證「NPC 靠狀態自動長 perk」。
   - **🟡 持久層 + SyncPerks 已落地（2026-06-18 離線）**：`persist`（巢狀 JFormDB 寫入，int/float/str/form + delta counter）+ `syncPerks`（依 stored rank AddPerk/RemovePerk）。解 U5。example `examples/npc_skill_persist_spec.json`。
     - ✅ **host=對話 TIF fragment**（key=speaker/player/ref）。
     - ✅ **host=quest stage fragment 已補（2026-06-18 離線）**：`quest.stages[].persist`/`.syncPerks` — 到達 stage 時觸發（emitter host-agnostic，prop 名以 `S<idx:D4>_` namespace 防多 stage 撞名；stage 無 akSpeakerRef → key 須 player/ref，validation 擋 speaker）。**這就是「任務 stage gate 觸發路徑」**。
     - ✅ **任意-ref key 已補（2026-06-18 離線）**：key 非 speaker/player 即視為 arbitrary ref → 綁 `PKey`/`SKey` Form property 當 JFormDB key（石頭代表某 NPC）。syncPerks 的 ref 須 runtime 是 actor ref（`If (key as Actor)` 守，非 actor no-op）。
     - ✅ **好感度 gate 已補（2026-06-19 離線）**：`persist`/`syncPerks` 可選 `gate: {global, atLeast?, atMost?}` — 綁一個 GLOB（relationship/reputation 計數，Sofia F6 藍圖），把整塊寫入/sync 包進 `If <GLOB>.GetValue() >= n`（atMost→band、皆無→`!= 0`）。GLOB 綁為 `PGate`/`SGate` property、validation 擋未解 GLOB + 反向 band。705 測綠，example `npc_skill_persist_spec.json` 加 `MFSkill_Bond` 對話線（affinity>=4 才 unlock）示範。
     - ✅ **.pex 已編 + 交付（2026-06-19 主力機）**：JContainers 12 `.psc` 併入 native headers cache + `MODFORGE_PAPYRUS_HEADERS` 指向它 → fragment 全編綠，FLAT zip 交付 `~/skyrim_mods/mine/`。memory `[[headless-jcontainers-papyrus-headers]]`。
     - ✅ **成長來源改「施法即觸發」（2026-06-19 離線）**：原 example 要找 Skill Trainer NPC 太難 → 重做成 **CastMagic SM quest**（玩家施任何法術 → SM 啟動 → `OnStory<Event>` handler 跑 persist+sync，keyed on player）。generator 新增 `StoryHandlerNeeded`：有 storyEvent 的 quest，其 stage persist 自動路由到 `OnStory` handler（SM quest 不跑 startUpStage fragment，沿用 [[dynamic-spawn-debugging]] 真因）。這就是「戰鬥/施法 XP gate」的觸發骨架——任何 SM 事件（KillActor/Assault/CastMagic…）都能掛 persist。707 測綠。
     - **剩**：實機驗（需 MO2 裝 JContainers SE；WAIT_USER）——施法長技能 + 好感度 gate 翻轉 + CastMagic 的 GetIsID Player 條件。
2. **Phase 1：玩家版 in-world 樹（繞過 U1/U2）**——照 Frostfall 模式掛一棵玩家樹到營火（Campfire 原生、全域 GLOB、零橋接）。驗證 in-world 星樹本體可生成可運作；只觸及 U4（generator）不觸及 U1/U2。
   - **🟡 零依賴 standalone 版已交付，待實機（2026-06-21 離線；使用者明確不想裝 Campfire/Frostfall）**：`examples/inworld_skill_tree_standalone_spec.json` + `examples/MFSkillNode.psc`（native-compiler 編綠）。**只依賴 Skyrim.esm**——放棄 Campfire 的營火 radial menu，改最直白的 in-world 樹：自訂房間擺 3 顆漂浮水晶節點（vanilla `WispCrystal01.nif`），各掛自寫 `MFSkillNode` 腳本（純 vanilla 型別），點擊→gate（前置節點+點數）→給自訂 Fortify ability。dump 驗證：masters 只剩 Skyrim.esm、gating 鏈正確、3 ability/MGEF + cell/floor 就位。交付 `~/skyrim_mods/mine/ModForgeSkillTree.zip`（FLAT），recipe 見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)。**啟示**：in-world 樹**不需要 Campfire**——一個自訂房間 + 可點 ACTI + 一支薄 OnActivate 腳本就成立，這對 Phase 3 generator 是更乾淨的目標（零外部 master）。Campfire 版（`inworld_skill_tree_spec.json`）留作 radial-menu 設計範本，不交付。
   - **（已封存）Campfire 依賴版**：`examples/inworld_skill_tree_spec.json` — 3 node 垂直鏈（Resolve→Vigor→Mastery），**無 generator、純用現有 primitives 手寫 spec** 驗證 pipeline。記錄全靠解碼 Frostfall 能跑的 Endurance 樹（`Frostfall.esp` controller `_Frost_PerkNodeController_Endurance` 064026）+ Campfire 1.11SE 六支 .psc 原始碼（從 `Campfire.bsa` 經 Mutagen Archive reader 抽出）。關鍵發現：① controller 本體 = 隱形 `MarkerX.nif`；② 三支樹腳本（`CampPerkNode`/`CampPerkNodeControllerBehavior`/`CampPerkSystemRegister`）**ship 編好的 .pex 在 Campfire.bsa，不用編**——ModForge 只 VMAD 引名 + 綁屬性（走 `scripts:` + `QuestAliasSpec.Script`）；③ PositionRef marker base = Campfire `_Camp_PerkNodePosRefDummy`(0x043811)/`_Camp_PerkLinePosRefDummy`(0x043832)；④ 確認 message `.Show(args)` 多餘參數會忽略 → 純文字描述安全（count=0）；⑤ register = StartGameEnabled quest + player-filled ReferenceAlias 跑 `CampPerkSystemRegister.OnInit`。**結構已全驗**（dump 輸出 esp：masters=Campfire+Skyrim、所有 object 屬性解析無 null、downstream 鏈正確、7 marker persistent、alias 填 player）。交付 `~/skyrim_mods/mine/ModForgeSkillTree.zip`（FLAT），實機 recipe + 三種結果判讀見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)。**這步同時就是 U4 能力的活證明**（手能拼出 = generator 也能生）。
3. **Phase 2（待 U1/U2）**：NPC 版橋接——session GLOB + JFormDB + 對 NPC 開樹。需先在主力機釐清 U1（對任意 ref 開樹）/ U2（session GLOB 隔離）。
4. **Phase 3（待 U4 收尾）**：ModForge generator——把上述 record 從 spec 自動產出，補 PositionRef layout 模板。
