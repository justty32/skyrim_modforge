# CODE_MAP.npcs-packages — AI Packages（PACK）

← [CODE_MAP.npcs-packages](../CODE_MAP.npcs-packages.md)

## AI Packages（PACK）
→ **說明文件**：[SPEC-packages.md § packages](../../../../docs/spec/SPEC-packages.md#packages--ai-packages-what-an-npc-does) · [engine-internals.md § AI Packages](../../../../docs/engine-internals.md#ai-packages-are-template-driven)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Packages.cs` | `PackageSpec`, `PackageScheduleSpec`, `SandboxSpec`, `SleepSpec` |
| Spec | `Spec.Packages.Templates.cs` | `TravelSpec`, `UseMagicSpec`, `PatrolSpec`, `FollowSpec`, `EscortSpec`, `SitTargetSpec`, `ActivateSpec`, `EatSpec` |
| Data | `PackageTemplates.cs` | vanilla PACK procedure-template FormKey 登錄（含 `SitTarget`=0x0A9277、`Activate`=0x019B2D、`Eat`=0x019714）|
| Data | `PackageRefSlots.cs` | 🔴 **槽位種類表（ONE source of truth）**：package 每一個吃字串的欄位分成 `SingleRef`（`patrol.start`/`follow.target`/`escort.target`/`sitTarget.target`/`activate.target`/`useMagic.target` → `PackageTargetSpecificReference`＝**就那一個 ref**）、`Location`（`sandbox.location`/`sleep.location`/`travel.place`/`escort.destination`/`eat.location`/`useMagic.location` → `LocationTarget`+radius＝**一塊區域**，引擎在 radius 內自己挑家具/床/食物）、`NotAPlacedRef`（base form / enum 名）。`BuildReferences` 的 area-anchor 護欄靠它；docs/spec/SPEC-packages.md 與 specs 設計檔的表都是它的鏡像。**反腐化**：`ReferenceSlotKindTests` reflection 掃 `PackageSpec` 全部 string 欄位，漏分類就紅——新 template 加 `target`/`location` 欄位時**必須同 commit 補這張表**。🔴 **12 個 SingleRef/Location 槽一律走 deferred wire**（見下列 `Generator.Build.Packages.cs`）|
| Build P2 | `Generator.Build.Packages.cs` | 資料槽填充 dispatcher（sandbox/sleep/travel/usemagic/patrol/follow/escort/**sittarget/activate/eat**）。🔴 **鐵律**：`BuildPackageData` 跑在 `BuildPlacements`／`BuildReferences` **之前**，ref 表這時只有 base record——所以**任何吃 placed-ref 的槽（`PackageRefSlots` 的 SingleRef/Location 全部）都不准在這裡解析**，只能丟進 `deferredTargetWires`／`deferredLocationWires`（`DeferTarget(...)`／`deferredLocationWires.Add(...)`），由 `WireDeferredTargets`／`WireDeferredLocations` 在 placements＋labels 都在了之後填。急著解析＝檔內 placement editorId 與 `references[]` label 永遠解不到、無聲掉回 NearSelf/Self（2026-07 `eat.location`／`useMagic.location`／`useMagic.target` 真的踩過）。base form 槽（template/combatStyle/ownerQuest/`useMagic.spell`）不受此限 |
| Build P2 | `Generator.Build.Packages.Advanced.cs` | 複雜套件槽：Escort/Patrol/Follow/**SitTarget/Activate/Eat**（SitTarget slot16 SingleRef→家具走位+坐；Activate slot0 SingleRef→物件走位+活化〔lever/door〕；Eat 為 location sandbox-variant，固定 food/chair 搜尋）。UseMagic slot4 先寫 `PackageTargetSelf`（self-cast 預設＋解不到時的 fallback），ref 走 `DeferTarget(selfOnUnresolved: true)` |
| Build P2 | `Generator.Build.Packages.AliasRefs.cs` | **C組 #2 radiant alias 解析**：`TryParseAliasRef`（`alias:`/`aliasLoc:` 共用，Build+Validate）+ `TryResolveAliasIndex`（對 package 的 in-spec `ownerQuest` 找 alias index）。`MakeLocationSlot`（`Generator.BuildContext.Utilities.cs`）alias→`LocationFallback{AliasForReference\|AliasForLocation, Data=idx}`；`WireDeferredTargets`（`Generator.Build.PlacementRefs.cs`）alias→`PackageTargetAlias{Alias=idx}`。⚠ AliasFor* 選擇 + PackageTargetAlias byte 待主力機 xEdit 比對真 radiant package |
| Build P2 | `Generator.Build.Conditions.Wire.cs` `WirePackageConditions` | package condition 接線（`BuildCondition` 組裝在 `Generator.Build.Conditions.cs`，dispatch 在 `Generator.Build.Conditions.Functions.cs`）|
| Validate | `Generator.Validate.Npcs.cs` | package template/slot integrity、AI-data enum、**alias-capable slot（`PkgSlotRef`）：`alias:`/`aliasLoc:` 需 in-spec ownerQuest + alias 存在** |

### npcPatches（override 既有 NPC 的 AI 排程）
→ **說明文件**：[SPEC-packages.md § npcPatches](../../../../docs/spec/SPEC-packages.md#npcpatches--override-an-existing-npcs-ai-schedule)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.NpcPatch.cs` | `NpcPatchSpec`（overrideOf / packages / mode / **factions**）|
| Build P1 | `Generator.Build.NpcPatches.cs` `BuildNpcPatches` | `TryResolveTemplate<INpcGetter>` 解 vanilla NPC → `new Npc(fk)+DeepCopyIn` 整筆 override（name/stats/faction 帶上；名字靠 MasterCache 提供的 STRINGS 解出）→ `mod.Npcs.Add` |
| Build P2 | `Generator.Build.NpcPatches.cs` `WireNpcPatchPackages` / **`WireNpcPatchFactions`** | package refs → `r.Packages` replace/prepend/append；**factions refs → ADD `RankPlacement`(rank 0) 到 `r.Factions`（additive、去重）**——給既有 NPC 加 membership（如 vendor FACT + JobMerchantFaction，見 CODE_MAP.world Idea #24 §D）|
| Infra | `Generator.BuildContext.Utilities.cs` `MasterCache`/`ProvisionStrings` | **本地化解法**：從 `Skyrim - Interface.bsa` lazy 抽 `<master>_english.{strings,ilstrings,dlstrings}` 到 temp `Strings/`（檔名照 ModKey 大小寫，Linux case-sensitive），overlay 用 `StringsReadParameters{English,StringsFolderOverride,BsaFolderOverride}` 開（BSA-free 夾 → 不觸發讀 load-order 的 archive scan）→ headless 解得 vanilla NPC 名 |
| Validate | `Generator.Validate.Npcs.cs` `ValidateNpcPatches` | overrideOf 須 external ref、packages 非空、mode ∈ replace/prepend/append |
