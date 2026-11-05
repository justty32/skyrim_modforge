# CODE_MAP.world — Living-world NPCs

← [CODE_MAP.world](../CODE_MAP.world.md)

## Living-world NPCs 活世界 NPC（Idea #23）
→ **說明文件**：[SPEC-world-macros.md § living-world NPCs](../../../../docs/spec/SPEC-world-macros.md#living-world-npcs-livingnpcs) · 設計 [sub_projs/living-adventurers/](../../../../sub_projs/living-adventurers/README.md)（idea #23 + design.md）

一小撮**具名持久 NPC 過自己的離場人生**（抽象幽靈模擬 + 就地實體化）的 **macro**（同 settlements pass-0），但**會掛 runtime 腳本**（兩個可重用 .pex）。每多一個既有 archetype 的 NPC = 一個 entry（純資料）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Controller .pex | `assets/papyrus/MFLivingWorldController.psc` | host quest，單一 roster 迴圈：`OnUpdateGameTime` 推進每個 alias 的抽象 sim、`OnUpdate` 跑每個 alias 的在場 poll（chained，不隨 N 線性膨脹）。props `SimIntervalHours`/`PollInterval`/`AliasCount` |
| Alias .pex | `assets/papyrus/MFLivingNpcAlias.psc` | per-NPC（extends ReferenceAlias）：`AdvanceSim`（deed global +1 + 輪替 anchorIdx）、`Presence`（玩家同 cell → `MoveTo` 進場 + `EvaluatePackage`；離開 → 回 HoldMarker）；`IsPlayerTeammate()` 時停止 sim/MoveTo 並交還 follower system。dismiss 後保留原 follower package 的可見步行，只有玩家已無法跟上且 grace 到期才 off-screen 回 hold、恢復納管。props `Archetype`/`HoldMarker`/`Anchors`/`DeedCount`/`ReclaimDelaySeconds`（30）/`ReclaimDistance`（8192） |
| Spec | `Spec.LivingNpc.cs` | `LivingNpcsSpec`（simIntervalHours/pollInterval/rumorSpeaker/npcs）+ `LivingNpcSpec`（ref/name/archetype/alignment/backstory/anchors/rumors/**interactions**）+ `LivingAnchorSpec`（cell/position/kind）|
| Spec | `Spec.cs` | `ModSpec.LivingNpcs`（nullable）+ `LivingNpcsExpanded` 守衛旗標 |
| Expand P0 | `Generator.LivingNpcs.cs` | **`ExpandLivingNpcs`**：共享 hold marker + sandbox package + host controller quest（掛 world-controller script）；每 NPC → deed GLOB + 每 anchor 一個 xmarker + Anchors FLST + alias（in-spec=place ACHR+`forced:`／external=`uniqueActor:`，掛 alias script + Archetype/HoldMarker/Anchors/DeedCount props，in-spec 補 sandbox package）+ 可選 rumor dialogue（gated on deed GLOB）。**P3**：`interactions` → per-NPC favor GLOB + 互動 dialogue（`setGlobal` favor，`LivingInteraction` fund/praise/parley copy，praise gate deed）；`alignment=hostile` 的 in-spec NPC 設 `Aggression=Aggressive`。`LivingArchetypeCode` 名→int |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandLivingNpcs(spec)`（在 `ExpandSettlements` 後）|
| Validate | `Generator.Validate.LivingNpcs.cs` | `ValidateLivingNpcs`：simInterval/poll>0、rumorSpeaker 須 in-spec/external、ref 必填且唯一、in-spec ref 須為 npcs[]、archetype 須已知、anchors≥1、anchor 須有 cell、有 rumors 無 speaker 警告、**alignment/interaction 種類須已知、external+hostile 警告** |
| Ship-gate | `Package.cs` + `ModForge.Cli.csproj` | `spec.LivingNpcs.Npcs.Count>0` → ship 兩個 .pex（EmbeddedResource，Exists 條件）|
| Example | `examples/living_npcs_spec.json` | Kjeld(adventurer，2 旅館輪替) + Falas(mageApprentice) + Bjorn rumor |
| Tests | `LivingNpcTests.cs` | macro 展開（controller quest/alias/archetype int/object props 解析/deed GLOB/anchor marker/FLST/persistent ref/external uniqueActor/rumor gated/no-speaker drop）+ validate |
| **core 前置修** | `Generator.Build.Scripts.cs` + `Generator.Build.Placements.cs` | ① alias scriptProperties object prop 改 deferred 解析（`deferredScriptObjectProps` + `WireDeferredScriptObjectProps`）→ 可指向 later-built placement；② `deferredForcedAliases.Ref` 併入 `deferredAnchorEds` → forced-alias ACHR 自動 persistent。回歸測 `AliasScriptObjectPropTests` |
