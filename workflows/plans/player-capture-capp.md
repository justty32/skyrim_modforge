# Player capture — `sc capp <label>`（去 PROTEUS 化）

**狀態：✅ 已落地，待實機**（2026-07-12；DLL crc `f8afc170`，co-save SCCP v8，C# 923 測綠）。實機步驟見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)「`sc capp` 直接吸玩家」。

## 落地摘要（2026-07-12）

| 計畫項 | 落地 |
|---|---|
| `sc capp [Label]` / `sc capc [Label]` | `Console.cpp`：label 取**未 `Lower()` 的 raw2**（`Trim()`，去空白與引號）。usage/helpString 更新 |
| 玩家＝一般 actor | `Captures::CapturePlayer` ＝ `CaptureRef(PlayerCharacter::GetSingleton(), "player", label)`——chargen 本來就在 base TESNPC（`Skyrim.esm:0x000007`），`ReadNpc` 原封不動就讀得到，**無 PROTEUS 中介** |
| 玩家 perks | `ReadNpc`：`actor->As<PlayerCharacter>()` → `GetPlayerRuntimeData().addedPerks`（玩家 base 的 perk array 是空的）；一般 NPC 照舊走 `npc->perks` |
| 顯式數值（**所有 actor**） | `AsActorValueOwner()->GetBaseActorValue()`：kHealth/kMagicka/kStamina ＋ AV 6..23 的 18 技能（＝Mutagen `Skill` enum 序，index 即映射） |
| label → editorId | `SceneExporter::AppendCaptures`：`editorId = "MFCap_" + sanitize(label)`（非 alnum → `_`），item/npc 兩段都吐 |
| co-save | `kVerCaps = 8`：entry 追加 `label`；NpcPayload 追加 H/M/S ＋ skills。v≤7 舊存檔照舊讀（欄位缺省 0） |
| C# 消費 | `CapturedNpcSpec`／`NpcSpec` 加 `Health/Magicka/Stamina/Skills`；`BuildNpcs` 寫 `PlayerSkills`（DNAM）；`ExpandCapturedNpcs` **優先序＝顯式數值 ＞ class autocalc**（有顯式值 → `AutoCalcStats=false`，class 仍帶）；兩處 validator 收邊界（skills 0\|18・0–255、H/M/S 0–65535）；`BuildNpcs` 對「顯式值 ＋ autoCalc 同開」`Warn` |

**未做（等實機結果再決定）**：玩家 base voiceType 若為空 → 分身啞巴（先照實輸出，不 fallback）；玩家物品欄全吸不過濾。

---

## 原始計畫（2026-07-11 拍板）

## 動機

PROTEUS 能複製玩家臉，只因引擎把 chargen 資料寫在玩家的 TESNPC base（0x14 的 base 0x7）上——DLL 直接讀同一處即可，**不需要 PROTEUS 中介**。直接吸玩家還順帶解掉 PROTEUS 路線的三個已知缺陷：

| PROTEUS 路線缺陷 | 直接吸玩家 |
|---|---|
| clone 自報 level 1、數值 50/50/50 | 真等級 + 真 H/M/S + 18 技能（顯式 DNAM，見下） |
| 不寫 tintLayers | 玩家 base 的 tintLayers 直接讀（RaceMenu 寫入處；實吸驗證，見風險） |
| base/defaultOutfit 指向 esp 上的空殼 runtime 模板 → 裸體 | 無中介記錄；worn armour → 現有 OTFT 鑄造路線 |

## DLL 側（SCCP → v8）

1. **Console.cpp**：新增 `sc capp [label]`（吸玩家）；`sc capc [label]` 加選用 label。⚠️ label 要用**未 `Lower()` 的 raw2**（現有解析會把參數全轉小寫，label 需保留大小寫）。usage/helpString 更新。
2. **Captures.h/.cpp**：
   - `Entry` 加 `std::string label`；`CapturePlayer(label)` = `CaptureRef(RE::PlayerCharacter::GetSingleton() 的 ref, "player")` + label 落到 entry。`CaptureConsoleRef(label)` 同。
   - **玩家 perks**：玩家的 base TESNPC perk array 是空的——perk 在 `RE::PlayerCharacter` runtime data 的 `addedPerks`（`BSTArray<PerkRankData*>`，vcpkg header PlayerCharacter.h L817/L937 已驗）。ReadNpc 裡：actor 是 player → 走 addedPerks，否則照舊走 `npc->perks`。
   - **顯式數值（所有 actor 一律捕，不只玩家）**：`AsActorValueOwner()->GetBaseActorValue()` 取 kHealth/kMagicka/kStamina + 18 技能（AV 6..23，順序 = Mutagen `Skill` enum：OneHanded…Enchanting）。NpcData 加 `health/magicka/stamina` float + `skills` vector（18 或空）。
3. **SceneExporter.cpp**：capturedNpcs 條目加 `editorId`（= `"MFCap_" + sanitize(label)`，label 空則不輸出）、`health`、`magicka`、`stamina`、`skills[18]`。
4. **CoSave.cpp**：`kVerCaps = 8`；append label + hms + skills；load 以 `version <= 7` gate 舊格式。

## ModForge 消費側

5. **Spec.CapturedNpcs.cs**：`Health/Magicka/Stamina`（float，0 = 未知）+ `Skills List<int>`。`EditorId` 已存在——DLL 直接餵，現有「顯式 editorId 優先」邏輯即為 label 識別機制。
6. **Spec.Actors.cs NpcSpec**：加 `Health/Magicka/Stamina int`（0 = 不設）+ `Skills List<int>`（0 或 18 個）。
7. **Generator.Build.Actors.cs**：有顯式數值 → 寫 DNAM（Mutagen `Npc.PlayerSkills`：`Health/Magicka/Stamina` ushort + `SkillValues` dict——**欄位形狀實作時用 reflection 驗**，非 autocalc NPC 引擎直接讀 DNAM）。
8. **Generator.CapturedNpcs.cs**：優先序改為 **顯式數值 > class autocalc**——`AutoCalcStats = 有 class 且無顯式數值`；class/level 照舊帶（class 仍供 AI/訓練語意）。
9. **Validators**（captured + 手寫 NpcSpec 兩處）：skills 數量 0|18、每值 0–255（DNAM 是 byte）；H/M/S ≥0 且 ≤65535（DNAM ushort）。
10. **Tests**：DLL-shaped json 帶 editorId/stats/skills end-to-end；「有 class + 有顯式數值 → autocalc OFF、DNAM 落值」；「有 class 無數值 → autocalc ON」（不破既有 in-game 已驗路線）；validation 邊界。

## 風險／實吸待驗

- 玩家 **tintLayers** 是否真在 base TESNPC 上（RaceMenu 寫該處，vanilla chargen 待驗）——空了也只是回到 PROTEUS 同等水準，不劣化。
- 玩家 base **voiceType 可能為空** → 分身啞巴；先照實輸出，之後再考慮 fallback（如 MaleEvenToned）。
- 玩家**物品欄很大**（含任務物品、金幣、鑰匙）→ 先全吸（行為與 NPC 一致），嫌吵再加過濾。
- NPC 捕獲也改帶顯式數值後，舊 capture json（無 stats 欄位）走原 class-autocalc 路——欄位缺省 0 即自然向後相容。

## 量級估計

DLL ~150 行 + C# ~120 行 + tests；一個離峰時段可完。
