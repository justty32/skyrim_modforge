# CODE_MAP.npcs-packages — NPC・Captured NPC・職業・派系・戰鬥風格

← [CODE_MAP.npcs-packages](../CODE_MAP.npcs-packages.md)

## NPCs
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（race+class+outfit 最低要求）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `NpcSpec`（race/class/faction/spells/combatStyle/outfit/packages/perks/**unique/essential/protected**/**items**/**顯式數值欄**（health/magicka/stamina/skills[18]→DNAM，autoCalcStats 的替代路線）/**外貌配方欄**（female/weight/height/bodyTint/hairColor/faceTexture/headParts/tintLayers/faceMorphs/faceParts）…）, `NpcItemSpec`（item ref + count）, `TintLayerSpec` |
| Build P1 | `Generator.Build.Actors.cs` | 建 NPC record（level/class/faction/combat-style/spell/perk 組裝；unique/essential/protected/female → `NpcConfiguration.Flag`；**外貌 record-local 半邊**：weight/height/QNAM(TextureLighting)/NAM9 faceMorph（18-slot 引擎陣列→具名欄映射，已離線鎖定＋測試釘死）/NAMA faceParts/tintLayers）；**外貌 ref 半邊在 `WireNpcs` pass 2**（HCLF hairColor→CLFM、FTST faceTexture→`HeadTexture` property、PNAM headParts）；**inventory items 在 `WireNpcs` pass 2 填（forward-ref-safe，建 `ContainerEntry`/`ContainerItem`；武器自動裝備、死亡掉落）**；**顯式數值（DNAM）＝非 autocalc 路線**：`health/magicka/stamina`（ushort）＋`skills`（18 byte，順序＝Mutagen `Skill` enum＝引擎 AV 6..23＝DLL 匯出序）寫進 `PlayerSkills`；非 autocalc actor 的引擎就是讀這裡。**兩條路不可同開**（autocalc 載入時重算會覆蓋掉寫死的值）→ `BuildNpcs` 偵測到就 `Warn`。**鐵律：`autoCalcStats` 必須配 `class`——autoCalc 靠 class 算 H/M/S,無 class → ~0 血 → essential NPC 永久倒地(看似死,要 `resurrect`);`BuildNpcs` 偵測到 autoCalc 無 class 會 `Warn`（in-game 踩過 2026-06-07）**；**注意：ModForge 只寫 TESNPC「配方」，不烘 FaceGeom .nif/facetint .dds——自訂臉在烘焙里程碑前會灰/暗臉（身形/髮色/膚色/身份正確）** |
| Validate | `Generator.Validate.Npcs.cs` | faction/class/outfit/voice/race ref；外貌欄（hairColor/faceTexture/headParts ref、faceMorphs=0\|18、faceParts=0\|4、weight 0-100、tint 色域）；**顯式數值欄（skills=0\|18 且每值 0–255、H/M/S 0–65535）**；package template/slot integrity |
| Diag | `Diagnostics.Records.Npc.cs` | NPC class/race/faction/outfit/voice/combat-style/package/perk 詳細 dump |
| Diag | `Diagnostics.Records.cs` | 跨類型 record 詳細欄位（含 NPC）|
| Tests | `NpcTests.cs` | NPC config flag（essential/protected）|

---

## Captured NPCs（capturedNpcs[] 遊戲內「演員滴管」消費，Idea #24）
→ **計畫/設計**：[plans/captured-npcs-consumption.md](../../../plans/captured-npcs-consumption.md)（Phase 1=TESNPC 配方；Phase 2=FaceGeom 烘焙未做）；姊妹＝[CODE_MAP.items-magic.md § Captured items](../CODE_MAP.items-magic.md)

scene-capture-bridge DLL `sc cap`（準星）／`sc capc`（console 選取）／**`sc capp`（直接吸玩家）**匯出的 `capturedNpcs[]` → macro 展開成既有 `NpcSpec`（身份＋臉/身配方＋顯式數值）＋擷取地點的 ACHR `PlacementSpec`。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.CapturedNpcs.cs` | `CapturedNpcSpec`（逐欄對齊 DLL json；含 class/level/combatStyle/voiceType/spells/inventory＋**顯式 health/magicka/stamina/skills[18]**）＋`CapturedNpcItemSpec`（{item,count,worn,name,enchantment?}——**實例附魔列**）＋`CapturedHairColorSpec`（只消費 id）＋`CapturedNpcPerkSpec`（rank advisory）＋`CapturedActiveEffectSpec`（整組 advisory）|
| Expand P0 | `Generator.CapturedNpcs.cs` `ExpandCapturedNpcs`（`ExpandMacros` 尾端）| 每筆→`NpcSpec`（身份+外貌配方+class/level；**數值優先序＝顯式 H/M/S/skills ＞ class autocalc**——有顯式值就寫 DNAM 且 autoCalcStats 關（autocalc 會在載入時覆寫掉），沒有才走舊的「有 class 才開 autoCalc」（無 class 的 0 血陷阱）；perk 只取 ref；**equippedArmor→鑄 in-spec OTFT**（引擎只穿 outfit 護甲、inventory 護甲不穿——實機「靴子放口袋」證實）＋**inventory 列**：worn→outfit、其餘→Items（數量保留）；**帶實例附魔的列先鑄 WEAP/ARMO 模板複製＋引用或新鑄 ENCH**（`ResolveOrMintEnchant` 重用，editorId `<ed>_Inv<j>`，實例顯示名帶入）；class/level/combatStyle/voiceType/spells 直通；legacy `equipped`→護甲、`equippedWeapons`→Items。PROTEUS 空殼 defaultOutfit 被取代）＋有 cell/worldspace anchor 才生 `PlacementSpec`（Kind=npc、Persistent）。editorId 預設 `MFCapNpc_<name>_<i>`；**DLL 給了顯式 editorId（`sc capp <label>` → `MFCap_<label>`）則以它為準**＝label 就是身份 |
| Validate | `Generator.Validate.SceneNpcRoles.cs` `ValidateCapturedNpcs` | race 必填、身份/配方 ref 格式、cell⊕worldspace 互斥、faceMorphs=0\|18、faceParts=0\|4、**skills=0\|18 且每值 0–255（DNAM byte）**、**H/M/S 0–65535（DNAM ushort）**、weight/色域/tint value 範圍 |
| Tests | `CapturedNpcsTests.cs` | validate＋expand＋**faceMorph index↔具名欄映射鎖定測試**（18 相異值讀回）＋**顯式數值 vs autocalc 優先序＋skills index↔`Skill` enum 映射鎖定**＋DLL-shaped json 端到端（含 `sc capp` 形狀）；vanilla refs resolve 1 RequiresSkyrim |

---

## Classes（職業）
→ **說明文件**：[SPEC-dialogue.md § classes](../../../../docs/spec/SPEC-dialogue.md#classes-clas)

（源碼見 [CODE_MAP.dialogue-quests.md § Classes](../dialogue-quests/quests.md#classes職業-clas)）

---

## Factions 派系
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `FactionSpec`, `RelationshipSpec` |
| Build P1 | `Generator.Build.Classes.cs` | `BuildRelationships`, `WireRelationships`, `WireOutfits` |
| Validate | `Generator.Validate.Npcs.cs` | faction ref |
| Diag | `Diagnostics.Factions.cs` | faction members / vendor config / crime data / relationship dump |

---

## Combat Styles 戰鬥風格（CSTY）
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（combatStyle + spells 搭配說明）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `CombatStyleSpec`（equipMult* 六欄 AI 武器偏好分數）|
| Build P1 | `Generator.Build.Actors.cs` | 建 CombatStyle record + 接到 NPC |
| Validate | `Generator.Validate.Npcs.cs` | combatStyle ref |
| Diag | `Diagnostics.Records.cs` | CombatStyle 欄位 dump |
