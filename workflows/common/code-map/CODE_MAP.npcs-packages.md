# CODE_MAP — NPC・派系・職業・AI 套件・戰鬥風格・天氣

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：NPCs、factions、relationships、classes、combat styles、AI packages（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、outfits、weather/climate。

## Lifelike Docs（NPC 深度指南）
→ **說明文件**：[lifelike/README.md](../../../docs/lifelike/README.md)（入口）

這組文件是 NPC / package 功能的深度知識庫，獨立於 SPEC-*.md 之外。修改 NPC 或 AI package 相關功能時，需同步評估這裡是否需要更新。

| 檔案 | 內容 |
|-----|-----|
| `docs/lifelike/README.md` | 入口索引 |
| `docs/lifelike/cookbook-index.md` | Cookbook 目錄 |
| `docs/lifelike/cookbook-npc-basics.md` | NPC 基礎食譜 |
| `docs/lifelike/cookbook-followers.md` | 隨從食譜 |
| `docs/lifelike/cookbook-social-quest.md` | 社交 / 任務食譜 |
| `docs/lifelike/cookbook-magic.md` | 魔法 NPC 食譜 |
| `docs/lifelike/cookbook-world-items.md` | 世界物件食譜 |
| `docs/lifelike/cookbook-advanced.md` | 進階食譜 |
| `docs/lifelike/formid-reference.md` | 原版 FormID 速查 |
| `docs/lifelike/gotchas.md` | 常見陷阱 |
| `docs/lifelike/cheatsheets.md` | 速查表 |

（zh-TW 對應版本在 `docs/zh-TW/lifelike/`，最低優先級，有明確需要才更新。）

---

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/lifelike_npc_spec.json` | 擬真 NPC（race + class + outfit + packages）|
| `examples/npc-inventory.json` | NPC 庫存（攜帶武器/金幣/藥水；武器自動裝備、可被劫/loot）|
| `examples/follower_hireable_spec.json` | 可招募隨從（hireable 模式）|
| `examples/follower_paid_spec.json` | 付費隨從 |
| `examples/follower_vanilla_spec.json` | vanilla 風格隨從 |
| `examples/class_spec.json` | 自訂職業（CLAS）|
| `examples/combat_spec.json` | 戰鬥風格（CSTY）|
| `examples/package_spec.json` | Sandbox AI package |
| `examples/package2_spec.json` | Travel package |
| `examples/package3_spec.json` | UseMagic package |
| `examples/package4_spec.json` | 其他 package 變體 |
| `examples/escort_spec.json` | Escort package |
| `examples/radiant_package_spec.json` | **#2 radiant 演出 package：`alias:`/`aliasLoc:` target/location 指向 ownerQuest 的 alias（travel→aliasLoc:Dungeon、escort target→alias:VIP）** |
| `examples/follow_spec.json` | Follow package |
| `examples/patrol_spec.json` | Patrol package |
| `examples/scene-sit-performance.json` | SitTarget package（NPC 走到家具並坐下；scene Package action 驅動的演出 beat）|
| `examples/package-activate.json` | Activate package（NPC 走到 ref 並活化〔lever/door/activator〕）|
| `examples/package-eat.json` | Eat package（location sandbox-variant：NPC 找食物+椅子坐下吃）|
| `examples/usemagic_spec.json` | UseMagic package（施法 AI）|
| `examples/weather_spec.json` | 自訂天氣 + 氣候 |
| `examples/scripts/MFFollowerFollow.psc` | 隨從 Follow 行為腳本 |
| `examples/scripts/MFFollowerTrade.psc` | 隨從交易腳本 |
| `examples/scripts/MFFollowerWait.psc` | 隨從待命腳本 |
| `examples/scripts/MFHireFollowerSetup.psc` | 招募設置腳本 |
| `examples/scripts/MFHirePaidDismiss.psc` | 付費解散腳本 |
| `examples/scripts/MFHirePaidRecruit.psc` | 付費招募腳本 |
| `examples/scripts/MFHireVanillaRecruit.psc` | vanilla 招募腳本 |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `PackageTests.cs` | AI package 資料槽填充（所有 template 變體）|
| `PackageAliasTargetTests.cs` | **#2 alias target/location：Travel/Sleep/Activate/Escort 的 alias:/aliasLoc: → PackageTargetAlias / LocationFallback(AliasForReference\|AliasForLocation) + index；validate（無 ownerQuest / 未知 alias / external ownerQuest）** |
| `RelationshipAndEslTests.cs` | faction relationship build + ESL flag 行為 + **masterless 防呆**（`PluginIo.Write` 對零外部 ref 的 esp 補 Skyrim.esm master；有 ref 不重複）|
| `WeatherClimateTests.cs` | weather scalar fields + climate build |

---

---

## NPCs
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（race+class+outfit 最低要求）

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
→ **計畫/設計**：[plans/captured-npcs-consumption.md](../../plans/captured-npcs-consumption.md)（Phase 1=TESNPC 配方；Phase 2=FaceGeom 烘焙未做）；姊妹＝[CODE_MAP.items-magic.md § Captured items](CODE_MAP.items-magic.md)

scene-capture-bridge DLL `sc cap`（準星）／`sc capc`（console 選取）／**`sc capp`（直接吸玩家）**匯出的 `capturedNpcs[]` → macro 展開成既有 `NpcSpec`（身份＋臉/身配方＋顯式數值）＋擷取地點的 ACHR `PlacementSpec`。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.CapturedNpcs.cs` | `CapturedNpcSpec`（逐欄對齊 DLL json；含 class/level/combatStyle/voiceType/spells/inventory＋**顯式 health/magicka/stamina/skills[18]**）＋`CapturedNpcItemSpec`（{item,count,worn,name,enchantment?}——**實例附魔列**）＋`CapturedHairColorSpec`（只消費 id）＋`CapturedNpcPerkSpec`（rank advisory）＋`CapturedActiveEffectSpec`（整組 advisory）|
| Expand P0 | `Generator.CapturedNpcs.cs` `ExpandCapturedNpcs`（`ExpandMacros` 尾端）| 每筆→`NpcSpec`（身份+外貌配方+class/level；**數值優先序＝顯式 H/M/S/skills ＞ class autocalc**——有顯式值就寫 DNAM 且 autoCalcStats 關（autocalc 會在載入時覆寫掉），沒有才走舊的「有 class 才開 autoCalc」（無 class 的 0 血陷阱）；perk 只取 ref；**equippedArmor→鑄 in-spec OTFT**（引擎只穿 outfit 護甲、inventory 護甲不穿——實機「靴子放口袋」證實）＋**inventory 列**：worn→outfit、其餘→Items（數量保留）；**帶實例附魔的列先鑄 WEAP/ARMO 模板複製＋引用或新鑄 ENCH**（`ResolveOrMintEnchant` 重用，editorId `<ed>_Inv<j>`，實例顯示名帶入）；class/level/combatStyle/voiceType/spells 直通；legacy `equipped`→護甲、`equippedWeapons`→Items。PROTEUS 空殼 defaultOutfit 被取代）＋有 cell/worldspace anchor 才生 `PlacementSpec`（Kind=npc、Persistent）。editorId 預設 `MFCapNpc_<name>_<i>`；**DLL 給了顯式 editorId（`sc capp <label>` → `MFCap_<label>`）則以它為準**＝label 就是身份 |
| Validate | `Generator.Validate.SceneNpcRoles.cs` `ValidateCapturedNpcs` | race 必填、身份/配方 ref 格式、cell⊕worldspace 互斥、faceMorphs=0\|18、faceParts=0\|4、**skills=0\|18 且每值 0–255（DNAM byte）**、**H/M/S 0–65535（DNAM ushort）**、weight/色域/tint value 範圍 |
| Tests | `CapturedNpcsTests.cs` | validate＋expand＋**faceMorph index↔具名欄映射鎖定測試**（18 相異值讀回）＋**顯式數值 vs autocalc 優先序＋skills index↔`Skill` enum 映射鎖定**＋DLL-shaped json 端到端（含 `sc capp` 形狀）；vanilla refs resolve 1 RequiresSkyrim |

---

## Classes（職業）
→ **說明文件**：[SPEC-dialogue.md § classes](../../../docs/spec/SPEC-dialogue.md#classes-clas)

（源碼見 [CODE_MAP.dialogue-quests.md § Classes](CODE_MAP.dialogue-quests.md#classes職業-clas)）

---

## Factions 派系
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `FactionSpec`, `RelationshipSpec` |
| Build P1 | `Generator.Build.Classes.cs` | `BuildRelationships`, `WireRelationships`, `WireOutfits` |
| Validate | `Generator.Validate.Npcs.cs` | faction ref |
| Diag | `Diagnostics.Factions.cs` | faction members / vendor config / crime data / relationship dump |

---

## Combat Styles 戰鬥風格（CSTY）
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（combatStyle + spells 搭配說明）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `CombatStyleSpec`（equipMult* 六欄 AI 武器偏好分數）|
| Build P1 | `Generator.Build.Actors.cs` | 建 CombatStyle record + 接到 NPC |
| Validate | `Generator.Validate.Npcs.cs` | combatStyle ref |
| Diag | `Diagnostics.Records.cs` | CombatStyle 欄位 dump |

---

## AI Packages（PACK）
→ **說明文件**：[SPEC-packages.md § packages](../../../docs/spec/SPEC-packages.md#packages--ai-packages-what-an-npc-does) · [engine-internals.md § AI Packages](../../../docs/engine-internals.md#ai-packages-are-template-driven)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Packages.cs` | `PackageSpec`, `PackageScheduleSpec`, `SandboxSpec`, `SleepSpec` |
| Spec | `Spec.Packages.Templates.cs` | `TravelSpec`, `UseMagicSpec`, `PatrolSpec`, `FollowSpec`, `EscortSpec`, `SitTargetSpec`, `ActivateSpec`, `EatSpec` |
| Data | `PackageTemplates.cs` | vanilla PACK procedure-template FormKey 登錄（含 `SitTarget`=0x0A9277、`Activate`=0x019B2D、`Eat`=0x019714）|
| Data | `PackageRefSlots.cs` | 🔴 **槽位種類表（ONE source of truth）**：package 每一個吃字串的欄位分成 `SingleRef`（`patrol.start`/`follow.target`/`escort.target`/`sitTarget.target`/`activate.target`/`useMagic.target` → `PackageTargetSpecificReference`＝**就那一個 ref**）、`Location`（`sandbox.location`/`sleep.location`/`travel.place`/`escort.destination`/`eat.location`/`useMagic.location` → `LocationTarget`+radius＝**一塊區域**，引擎在 radius 內自己挑家具/床/食物）、`NotAPlacedRef`（base form / enum 名）。`BuildReferences` 的 area-anchor 護欄靠它；docs/spec/SPEC-packages.md 與 specs 設計檔的表都是它的鏡像。**反腐化**：`ReferenceSlotKindTests` reflection 掃 `PackageSpec` 全部 string 欄位，漏分類就紅——新 template 加 `target`/`location` 欄位時**必須同 commit 補這張表**。🔴 **12 個 SingleRef/Location 槽一律走 deferred wire**（見下列 `Generator.Build.Packages.cs`）|
| Build P2 | `Generator.Build.Packages.cs` | 資料槽填充 dispatcher（sandbox/sleep/travel/usemagic/patrol/follow/escort/**sittarget/activate/eat**）。🔴 **鐵律**：`BuildPackageData` 跑在 `BuildPlacements`／`BuildReferences` **之前**，ref 表這時只有 base record——所以**任何吃 placed-ref 的槽（`PackageRefSlots` 的 SingleRef/Location 全部）都不准在這裡解析**，只能丟進 `deferredTargetWires`／`deferredLocationWires`（`DeferTarget(...)`／`deferredLocationWires.Add(...)`），由 `WireDeferredTargets`／`WireDeferredLocations` 在 placements＋labels 都在了之後填。急著解析＝檔內 placement editorId 與 `references[]` label 永遠解不到、無聲掉回 NearSelf/Self（2026-07 `eat.location`／`useMagic.location`／`useMagic.target` 真的踩過）。base form 槽（template/combatStyle/ownerQuest/`useMagic.spell`）不受此限 |
| Build P2 | `Generator.Build.Packages.Advanced.cs` | 複雜套件槽：Escort/Patrol/Follow/**SitTarget/Activate/Eat**（SitTarget slot16 SingleRef→家具走位+坐；Activate slot0 SingleRef→物件走位+活化〔lever/door〕；Eat 為 location sandbox-variant，固定 food/chair 搜尋）。UseMagic slot4 先寫 `PackageTargetSelf`（self-cast 預設＋解不到時的 fallback），ref 走 `DeferTarget(selfOnUnresolved: true)` |
| Build P2 | `Generator.Build.Packages.AliasRefs.cs` | **C組 #2 radiant alias 解析**：`TryParseAliasRef`（`alias:`/`aliasLoc:` 共用，Build+Validate）+ `TryResolveAliasIndex`（對 package 的 in-spec `ownerQuest` 找 alias index）。`MakeLocationSlot`（`Generator.BuildContext.Utilities.cs`）alias→`LocationFallback{AliasForReference\|AliasForLocation, Data=idx}`；`WireDeferredTargets`（`Generator.Build.PlacementRefs.cs`）alias→`PackageTargetAlias{Alias=idx}`。⚠ AliasFor* 選擇 + PackageTargetAlias byte 待主力機 xEdit 比對真 radiant package |
| Build P2 | `Generator.Build.Conditions.Wire.cs` `WirePackageConditions` | package condition 接線（`BuildCondition` dispatch 仍在 `Generator.Build.Conditions.cs`）|
| Validate | `Generator.Validate.Npcs.cs` | package template/slot integrity、AI-data enum、**alias-capable slot（`PkgSlotRef`）：`alias:`/`aliasLoc:` 需 in-spec ownerQuest + alias 存在** |

### npcPatches（override 既有 NPC 的 AI 排程）
→ **說明文件**：[SPEC-packages.md § npcPatches](../../../docs/spec/SPEC-packages.md#npcpatches--override-an-existing-npcs-ai-schedule)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.NpcPatch.cs` | `NpcPatchSpec`（overrideOf / packages / mode / **factions**）|
| Build P1 | `Generator.Build.NpcPatches.cs` `BuildNpcPatches` | `TryResolveTemplate<INpcGetter>` 解 vanilla NPC → `new Npc(fk)+DeepCopyIn` 整筆 override（name/stats/faction 帶上；名字靠 MasterCache 提供的 STRINGS 解出）→ `mod.Npcs.Add` |
| Build P2 | `Generator.Build.NpcPatches.cs` `WireNpcPatchPackages` / **`WireNpcPatchFactions`** | package refs → `r.Packages` replace/prepend/append；**factions refs → ADD `RankPlacement`(rank 0) 到 `r.Factions`（additive、去重）**——給既有 NPC 加 membership（如 vendor FACT + JobMerchantFaction，見 CODE_MAP.world Idea #24 §D）|
| Infra | `Generator.BuildContext.Utilities.cs` `MasterCache`/`ProvisionStrings` | **本地化解法**：從 `Skyrim - Interface.bsa` lazy 抽 `<master>_english.{strings,ilstrings,dlstrings}` 到 temp `Strings/`（檔名照 ModKey 大小寫，Linux case-sensitive），overlay 用 `StringsReadParameters{English,StringsFolderOverride,BsaFolderOverride}` 開（BSA-free 夾 → 不觸發讀 load-order 的 archive scan）→ headless 解得 vanilla NPC 名 |
| Validate | `Generator.Validate.Npcs.cs` `ValidateNpcPatches` | overrideOf 須 external ref、packages 非空、mode ∈ replace/prepend/append |

---

## Weather / Climate（天氣 WTHR / 氣候 CLMT）
→ **說明文件**：[SPEC-packages.md § weathers & climates](../../../docs/spec/SPEC-packages.md#weathers--climates--custom-skies-wthr--weather-cycles-clmt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Weather.cs` | `WeatherSpec`（含 `Template`：抄 vanilla 天氣繼承雲/天空）, `WeatherImageSpacesSpec`（per-ToD IMGS refs）, `ClimateSpec` |
| Build P1 | `Generator.Build.Weather.cs` | 建 weather scalar fields（colors/clouds/wind/fog）；`template` DeepCopy vanilla 天氣後只覆寫 spec 給的（empty clouds/null color 保留 template）|
| Build P1 | `Generator.Build.Climate.cs` | 建 climate scalar fields（timing/sun/moon/volatility）；weather entries pass 2 接；`WireWeatherLinks`：`WeatherSpec.ImageSpaces` → WTHR per-ToD `ImageSpaces`（`default` 填未設 ToD）|
| Validate | `Generator.Validate.Weather.cs` | color 範圍、cloud index、timing monotonicity、chance 總和 |
| Validate | `Generator.Validate.Lighting.cs` | `WeatherImageSpacesSpec` 中每個 IMGS ref 可解（cross-type with IMGS registry）|
| Diag | `Diagnostics.Weather.cs` | sky colors / cloud layers / precipitation / wind / fog / ImageSpaces per-ToD dump（`weatherdiag`）|
| Diag | `Diagnostics.Records.cs` | `imgsdiag`：列印 IMGS brightness/contrast/saturation/bloom/HDR/tint（室內 CELL 與室外 Weather 共用）|
| Tests | `LightingTests.cs` | weather `imageSpaces` per-ToD 接線（`default` 填空）+ `WeatherSpec.template` 雲繼承（與室內 LGTM/IMGS/CELL 同檔；另見 `WeatherClimateTests.cs` 管 weather scalar）|

室外 IMGS 調色範例：`examples/weather_bright.json`（IMGS `template` SkyrimClear Day + Weather `imageSpaces.default`，`weatherdiag` 確認四 ToD 全填同一 IMGS）。
