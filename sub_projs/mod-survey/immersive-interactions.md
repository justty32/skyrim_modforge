# Mod Survey — Immersive Interactions (Nexus 47670, v1.72)

> ModForge 取向逆向：把這個 mod 拆成「ModForge 可生成的資料層」vs「必須靠外部 framework/原生支援」。
> 檔案：`Immersive Interactions-47670-1-72-1690553183.zip`（解壓後已分析，temp 已清）。

## 一、這個 mod 做什麼 + 怎麼運作

Immersive Interactions（內部 EditorID 前綴 `AR_`，原名 "Animations Reborn"）讓玩家**啟動（Activate）世界物件時播放情境動畫**：開門、撿地上物、開鎖、搜屍、開箱、祈禱、向衛兵/小孩/將領/Jarl 敬禮、摸狗、擠牛奶、解謎機關（拉桿/按鈕/柱子/鏈條）、撿柴、滅火、讀書讀信、用毒咳嗽、坐下等待等。

**機制總結（重點）：完全沒有 SKSE .dll，純資料 + Papyrus + 動畫框架。** 不是 hardcoded 在原生外掛裡，而是三層協作：

1. **觸發層 = Perk Entry Point「Add Activate Choice」**。一顆 `AR_AnimPerk` (Perk 0x000802，OnInit/OnPlayerLoadGame 時 `AddPerk` 給玩家)，掛 **33 個 entry-point effect**：29 個 `PerkEntryPointAddActivateChoice` + 4 個 `PerkEntryPointSetText`(SetActivateLabel)。每個 AddActivateChoice 帶 **conditions**（用 `GetIsID` / keyword / FormList 判斷 crosshair 目標是門/箱/屍/狗/衛兵…）＋一段 **perk fragment**（`AnimimationsReborn_Fragments.psc`，Extends Perk）。fragment 只做一件事：呼叫 quest script 對應函式，例如 `Activate.fOpen(akActor, akTargetRef)`、`Activate.fpetdog(...)`、`Activate.fpuzzle(...)`。
   - 注意 master 列含 **`Dynamic Activation Key.esp`（DAK）**。DAK（自身是 SKSE 外掛）提供「長按 Activate 走 perk choice、短按走原版」的分流；本 mod 把它當依賴，而非自帶 dll。`OnControlUp` 裡也用 `RegisterForControl("Activate")` + `HoldTime` 自己判長按（撿柴/滅火/喝蜜酒那條走 `FindClosestReferenceOfAnyTypeInList` 找附近 static）。

2. **邏輯層 = `AR_QuestScript.psc`（Extends Quest）**。所有 `fXxx()` 函式的家。流程模式高度一致：`IsPlayerIn3rd()`(強制第三人稱+鎖控制) → 設動畫選擇器 global → `PlayIdle`/`SendAnimationEvent` 播動畫 → `utility.wait` → `akTargetRef.Activate(akActor)` 真正執行原版啟動 → 收尾 `Returnto1st()`。用 `bool busy` 當互斥鎖、`bisDoingFavor` 防跟隨者 favor 衝突。目標分類**全靠 FormList.HasForm()**（`Interact_Levers`/`Interact_Buttons`/`Interact_Pillars`/`Interact_Chains`/`Interact_Bars`/`Interact_Puzzle`…各一個 FormList）。
   - `AR_Ref_AliasScript.psc`（Extends ReferenceAlias，掛玩家別名）跑被動 event：`OnObjectEquipped`（吃食材咳嗽、讀書動畫）、`OnItemRemoved`（用毒）、`OnControlDown`（按 Wait 改坐姿+調 timescale）。

3. **動畫選擇層 = FNIS + DAR（Dynamic Animation Replacer）**。動畫本身用 **FNIS** 註冊自訂 idle/offset anim（`FNIS_ImmersiveInteractions_List.txt`：`ofa`/`o`/`b`/`s`/`+` 行定義 `AO_OpenDoor`/`AO_PickUp`/`AO_IdleLockPick`/`AO_IdleTake`…動畫事件名 + .hkx）。而「同一個動作要播哪一種變體」靠 **DAR**：7 個 `_CustomConditions/19931..19937` 資料夾，每個一個 `_conditions.txt`，條件就是
   ```
   ValueEqualTo("ImmersiveInteractions.esp"|0x0000AA13, N)
   ```
   亦即讀 GlobalShort **`AR_DogUp` (0x0000AA13)** 的整數值（1..7）。Papyrus 在播動畫**前**用 `AR_DogUp.SetValue(N)`、播完 `SetValue(0)`，DAR 即時換上該層資料夾裡的 `.hkx`（不同高度/姿勢的撿取、不同搜尋動畫等）。**Global 變數當 DAR 的執行期選擇器**就是這個 mod 的核心巧思。

一句話：**Perk-AddActivateChoice（條件分流）→ perk fragment → Quest script（鎖+播動畫+延後 Activate）→ Global 寫值 → DAR 依 Global 換 FNIS 動畫變體。** 完全資料/腳本驅動，零原生程式碼（DAK/FNIS/DAR 是外部依賴）。

## 二、關鍵檔案與模式

| 檔案 | 角色 |
|---|---|
| `ImmersiveInteractions.esp` | 95 records：**43 GlobalShort**（`AR_*` 全是 MCM 開關 + 1 個 DAR 選擇器 `AR_DogUp`）、**35 FormList**（目標分類）、**1 Perk** `AR_AnimPerk`（33 entry-points）、**1 Quest** `AR_Quest`、3 SoundMarker、3 SoundDescriptor、2 MagicEffect + 1 Ingestible（`AR_Energized` buff）、4 Furniture override（Lever，加/改 keyword 讓拉桿可分類）、1 DialogTopic+1 INFO。master 含 DAK.esp。 |
| `AnimimationsReborn_Fragments.psc/.pex` | Perk fragment 派發器：~29 個 `Fragment_N`，每個一行 `Activate.fXxx(akActor, akTargetRef)`。 |
| `AR_QuestScript.psc/.pex` | 全部互動邏輯（`fOpen`/`fTake`/`fHarvest`/`fpuzzle`/`fSearch`/`fSearchNPC`/`fPray`/`fpetdog`/`fwavehorse`/`fsalute*`/`ffriend`…）+ `OnControlUp` 撿柴滅火 + `IsPlayerIn3rd`/`Returnto1st` helper。 |
| `AR_Ref_AliasScript.psc/.pex` | 玩家別名被動 event（裝備/移除/Wait 鍵）。 |
| `AR_MCMScript.pex` | SkyUI MCM 設定面板（讀寫那 43 個 global）。 |
| `meshes/.../ImmersiveInteractions/FNIS_..._List.txt` | FNIS 自訂動畫定義 → 產生 behavior（`FNIS_ImmersiveInteractions_Behavior.hkx`）。 |
| `meshes/.../DynamicAnimationReplacer/_CustomConditions/1993N/_conditions.txt` + `.hkx` | DAR 條件層：`ValueEqualTo(...esp\|0x0000AA13, N)`，依 `AR_DogUp` 換動畫。 |
| `sound/fx/*.wav`, `Interface/MCM/*.dds` | 音效 + MCM 圖。 |

**用到的動畫事件名（FNIS / SendAnimationEvent）**：`AO_OpenDoor`、`AO_PickUp`、`AO_PickupLow`、`AO_IdleLockPick`、`AO_IdleTake`、`AO_IdleKnock`、`AO_Kneel(Enter/Exit)`、`AO_Cut`、`AO_Tan`、`AO_NoteStart/During/Exit`、加上大量原版 idle（`idlepickup_ground`、`idlegreybeardwordteach`、`idleSearchingChest/Table`、`idlewave`、`idlesalute`…）以 `PlayIdle(Idle property)` 播放。
**用到的 keyword**：`VendorItemIngredient`、`VendorItemPoison`、`VendorItemSpellTome`、`Armor*`/`Clothing*`（armor 換裝動畫分類）。

## 三、對 ModForge 的參考價值（可生成 / 需新支援 / 純參考）

### 可生成（ModForge 資料層直接能產）
- **GlobalShort / GlobalVariable 批量**：`AR_*` 那 43 顆設定旗標純資料，ModForge 既有 global 支援即可生成。
- **FormList 批量**：35 個目標分類 FormList 是這個 mod 的「資料驅動」骨架，ModForge 能生成（HasForm 分類是很乾淨的、可被 spec 描述的模式）。
- **MagicEffect + Ingestible（buff 道具）**：`AR_Energized` / 食物 MGEF（PeakValueModifier）——既有 magic/MGEF 支援涵蓋。
- **SoundMarker / SoundDescriptor**：自訂音效記錄可生成。
- **Furniture override（加 keyword）**：4 個 vanilla Lever 的 additive override 只是補 keyword——屬 override-record 模式（參照 `worldspace-override-*` 筆記的 additive-carry 手法）。
- **Quest 殼 + ReferenceAlias（玩家別名）**：空 Quest + alias 容器可生成；**但掛在 alias 上的腳本**屬下一類。

### 需新支援（ModForge 目前缺、值得補的能力）
- **Perk Entry Point「Add Activate Choice」+ 對應的 Perk Fragment**：這是整個 mod 的觸發核心。查 `src/ModForge.Core/Generator.Build.Perks.EntryPoints.cs` 的 `EntryPointTabCount` 表，**沒有 `AddActivateChoice` 也沒有 `SetText`(SetActivateLabel)**——entry-point builder 目前只覆蓋戰鬥/數值類。要生成這類「自訂 Activate 選項 + fragment 派發」的互動 mod，ModForge 需：(1) 支援 AddActivateChoice / SetText entry type（含其 tab-count，呼應 `perk-conditiontabcount-ctd` 筆記：tab-count byte 不對會 load CTD）、(2) 能生成 Perk-fragment 黏合（Perk script + per-effect fragment index），目前 fragment 生成只在 SCEN/quest 路線（見 `scene-playidle-recipe`）。
- **Perk EntryPoint 上的 CTDA conditions（GetIsID/HasKeyword/條件分流）**：AddActivateChoice 之所以能分流，靠每個 effect 帶條件。需確認 ModForge perk builder 能把條件掛到 entry-point effect 層（非 perk 層）。
- **DAR `_conditions.txt` 生成器**：`_CustomConditions/<priority>/_conditions.txt` + `.hkx` 擺放是純文字 + 檔案佈局，ModForge **完全可以新增一個 packaging 步驟生成**（把「global 值 → 動畫資料夾」這個對照表 spec 化）。建議列為 OAR/animation 線的可生成項（OAR 的 `config.json` 更結構化、更該優先）。
- **FNIS list 生成**：`FNIS_..._List.txt`（`ofa`/`o`/`b`/`s`/`+` 語法）同理可由 spec 生成——但 FNIS 已被 Nemesis/Pandora 取代，新支援應瞄準 OAR。

### 純參考（必須靠外部 framework，ModForge 不生成）
- **Dynamic Activation Key (DAK)**：原生 SKSE 外掛，提供長按/短按 Activate 分流。ModForge 不可生成，只能在 spec 標為「外部 master 依賴」。
- **FNIS / Nemesis / Pandora behavior 引擎**：產 `*_Behavior.hkx` 需執行外部工具，非 esp 資料。
- **DAR / OAR runtime**：條件解析在外掛內，ModForge 只能生成它讀的「資料」（見上一類），引擎本身純參考。
- **`.hkx` 動畫檔**：手工製作/外部資產，ModForge 只搬運不生成。
- **SkyUI MCM**：`AR_MCMScript` 依賴 SkyUI；ModForge 可生成 global，但 MCM 面板本身靠 SkyUI framework。

### 與既有 ModForge 筆記的連結
- **`scene-playidle-recipe`**：本 mod 的 `PlayIdle(idleXxx)` / `Debug.SendAnimationEvent` + `OffsetStop` 收尾，和 scene PlayIdle 的「每 phase 一個 idle + 收尾」模式同源；DAR 的 global-選擇器則是 scene 之外的另一條 PlayIdle 變體控制法，可互相參照。
- **`perk-conditiontabcount-ctd`**：若 ModForge 要新增 AddActivateChoice entry-point，務必依此筆記補對 tab-count，否則 load CTD。
- **`dispatcher-magic-trigger`**：本 mod 的「perk fragment → Activate.fXxx() 中央 quest script」是另一種 dispatcher 模式（trigger 進、quest script 集中處理），與既有 dispatcher-psc 派發 Fire() 思路一致——quest-script-as-dispatcher 是可重用的生成樣板。
