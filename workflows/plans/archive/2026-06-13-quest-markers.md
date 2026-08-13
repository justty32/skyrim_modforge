# Quest markers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the three Skyrim marker mechanisms a spec currently can't produce — the dynamic objective compass/map arrow (QSTA), a static XMarker anchor an objective can point at, and a permanent world-map marker (XMRK).

**Architecture:** Three thin layers over existing machinery. **A** adds `ObjectiveSpec.targets[]` → a pass-2 `WireObjectiveTargets()` that fills each QOBJ's `QuestObjectiveTarget` (AliasID + flag + CTDA) after aliases are built. **B** adds two `PlacementSpec.kind` values (`xmarker`/`xmarkerHeading`) that default the base form and force persistence. **C** adds a top-level `mapMarkers[]` → `BuildMapMarkers()` emitting a `PlacedObject` on the MapMarker static with an XMRK subrecord. All marker targets reuse existing alias fills; **B**/**C** refs are persistent so a `forced:` alias can point at them.

**Tech Stack:** C#/.NET 10, Mutagen.Bethesda.Skyrim 0.49, xUnit. Build is in-memory and offline (external base refs like `Skyrim.esm:0x3B` resolve to a FormKey without loading the master).

**Verified facts (probed against Mutagen 0.49 + the local Skyrim.esm this session):**
- `QuestObjective.Targets` is `ExtendedList<QuestObjectiveTarget>`; `QuestObjectiveTarget` = `int AliasID`, `Quest.TargetFlag Flags` (`CompassMarkerIgnoresLocks = 1`), `ExtendedList<Condition> Conditions`.
- Alias `ID` == sequential index; built `quest.Aliases` carry `.ID` + `.Name`.
- `PlacedObject.MapMarker` is a `MapMarker { TranslatedString Name; MapMarker.MarkerType Type; MapMarker.Flag Flags }`. `MarkerType`: None=0,City=1,Town=2,Settlement=3,Cave=4,Camp=5,Fort=6,NordicRuins=7,DwemerRuin=8,Shipwreck=9,Grove=10,Landmark=11,DragonLair=12,Farm=13,WoodMill=14,Mine=15,ImperialCamp=16,StormcloakCamp=17,Doomstone=18,WheatMill=19,Smelter=20,Stable=21,ImperialTower=22,Clearing=23,Pass=24,Altar=25,Rock=26,Lighthouse=27,OrcStronghold=28,GiantCamp=29,Shack=30,NordicTower=31,NordicDwelling=32,Docks=33,Shrine=34,…(castle/capitol per hold)…,RavenRock=54,…. `Flag`: Visible=1, CanTravelTo=2, ShowAllIsHidden=4.
- Base formids (STAT, confirmed): XMarker `Skyrim.esm:0x0000003B`, XMarkerHeading `Skyrim.esm:0x00000034`, MapMarker `Skyrim.esm:0x00000010`.
- Build invocation in tests: `Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod`.

---

### Task 1: A — objective targets spec + QSTA wiring

**Files:**
- Modify: `src/ModForge.Core/Spec/Spec.Dialogue.cs` (ObjectiveSpec + new ObjectiveTargetSpec)
- Create: `src/ModForge.Core/Build/Generator.Build.ObjectiveTargets.cs`
- Modify: `src/ModForge.Core/Build/Generator.Build.cs` (call the new pass)
- Test: `tests/ModForge.Core.Tests/Build/ObjectiveTargetTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class ObjectiveTargetTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Objective_target_emits_QSTA_with_alias_index_flag_and_condition()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFQM_Q", Name = "Q", Type = "SideQuest",
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
            Aliases =
            {
                new QuestAliasSpec { Name = "Bystander", Fill = "findMatching:any" },
                new QuestAliasSpec { Name = "Goal", Fill = "forced:Skyrim.esm:0x000014" },
            },
            Objectives =
            {
                new ObjectiveSpec
                {
                    Index = 10, Text = "Reach the goal", ShowStage = 10,
                    Targets =
                    {
                        new ObjectiveTargetSpec
                        {
                            Alias = "Goal", CompassIgnoresLocks = true,
                            Conditions = { new ConditionSpec { Function = "GetDistance", Comparison = "<=", Value = 500, Param = "Skyrim.esm:0x000014" } },
                        },
                    },
                },
            },
        });

        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFQM_Q");
        var goalId = q.Aliases.Single(a => a.Name == "Goal").ID;       // sequential index (1 here)
        var obj = q.Objectives.Single(o => o.Index == 10);
        var t = Assert.Single(obj.Targets);
        Assert.Equal((int)goalId, t.AliasID);
        Assert.True(t.Flags.HasFlag(Quest.TargetFlag.CompassMarkerIgnoresLocks));
        Assert.Single(t.Conditions);
    }
}
```

- [ ] **Step 2: Run it — expect FAIL**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~ObjectiveTargetTests"`
Expected: compile error / FAIL — `ObjectiveSpec.Targets` and `ObjectiveTargetSpec` don't exist.

- [ ] **Step 3: Add the spec types** in `src/ModForge.Core/Spec/Spec.Dialogue.cs`

Add to `ObjectiveSpec` (after `CompleteStage`):
```csharp
    // QSTA targets: the alias(es) the compass/map arrow points at. The marker follows whatever the
    // alias is filled with at runtime — an actor (mark a person) or a location/ref (mark a place).
    // Several targets = several QSTA (vanilla "any of X/Y/Z"). Resolved by WireObjectiveTargets once
    // the quest's aliases exist.
    public List<ObjectiveTargetSpec> Targets { get; set; } = new();
```

Add a new class right after `ObjectiveSpec`:
```csharp
// One QSTA on an objective. `alias` is an alias NAME on the SAME quest (resolved to its alias index).
// `compassIgnoresLocks` sets the QSTA flag so the compass marker shows through locked doors.
// `conditions` are per-target CTDA gates (the marker only shows while they pass), built via the
// shared BuildCondition().
public sealed class ObjectiveTargetSpec
{
    public string Alias { get; set; } = "";
    public bool CompassIgnoresLocks { get; set; }
    public List<ConditionSpec> Conditions { get; set; } = new();
}
```

- [ ] **Step 4: Add the build pass** — create `src/ModForge.Core/Build/Generator.Build.ObjectiveTargets.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: objective QSTA targets. Run after aliases are built (BuildStoryManager /
        // BuildStandaloneQuestAliases) so a name→alias-index map exists on the built quest. Each
        // ObjectiveTargetSpec becomes a QuestObjectiveTarget (AliasID + flag + CTDA) on its QOBJ. ---
        public void WireObjectiveTargets()
        {
            foreach (var qs in spec.Quests)
            {
                if (qs.Objectives.All(o => o.Targets.Count == 0)) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest))
                    continue;
                var idByName = quest.Aliases.ToDictionary(a => a.Name ?? "", a => (int)a.ID, StringComparer.OrdinalIgnoreCase);

                foreach (var o in qs.Objectives.Where(o => o.Targets.Count > 0))
                {
                    var obj = quest.Objectives.FirstOrDefault(x => x.Index == o.Index);
                    if (obj is null) { Warn($"  ! quest '{qs.EditorId}' objective {o.Index} has targets but no built QOBJ — skipped"); continue; }
                    foreach (var ts in o.Targets)
                    {
                        if (!idByName.TryGetValue(ts.Alias, out var aliasId))
                        { Warn($"  ! quest '{qs.EditorId}' objective {o.Index} target alias '{ts.Alias}' not found — skipped"); continue; }
                        var t = new QuestObjectiveTarget
                        {
                            AliasID = aliasId,
                            Flags = ts.CompassIgnoresLocks ? Quest.TargetFlag.CompassMarkerIgnoresLocks : 0,
                        };
                        foreach (var c in ts.Conditions)
                            if (BuildCondition(c, $"quest '{qs.EditorId}' objective {o.Index} target") is { } cond)
                                t.Conditions.Add(cond);
                        obj.Targets.Add(t);
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 5: Call the pass** in `src/ModForge.Core/Build/Generator.Build.cs` — add right after the `WireQuestStages();` line (~121):
```csharp
        ctx.WireObjectiveTargets();                // QOBJ QSTA targets (alias index + flag + CTDA) — after aliases exist
```

- [ ] **Step 6: Run the test — expect PASS**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~ObjectiveTargetTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Spec/Spec.Dialogue.cs src/ModForge.Core/Build/Generator.Build.ObjectiveTargets.cs src/ModForge.Core/Build/Generator.Build.cs tests/ModForge.Core.Tests/Build/ObjectiveTargetTests.cs
git commit -m "feat(quest): objective QSTA targets (alias marker + compass flag + CTDA)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: A — objective target validation

**Files:**
- Modify: `src/ModForge.Core/Validate/Generator.Validate.Quests.cs`
- Test: `tests/ModForge.Core.Tests/Build/ObjectiveTargetTests.cs` (add a case)

- [ ] **Step 1: Write the failing test** (append to the class)

```csharp
    [Fact]
    public void Objective_target_naming_missing_alias_is_a_validate_problem()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFQM_Bad", Name = "Bad", Type = "SideQuest",
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
            Aliases = { new QuestAliasSpec { Name = "Real", Fill = "findMatching:any" } },
            Objectives =
            {
                new ObjectiveSpec { Index = 10, Text = "x", ShowStage = 10,
                    Targets = { new ObjectiveTargetSpec { Alias = "Ghost" } } },
            },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MFQM_Bad") && p.Contains("Ghost"));
    }
```

- [ ] **Step 2: Run it — expect FAIL** (no such problem reported)

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~ObjectiveTargetTests"`
Expected: FAIL on the new case.

- [ ] **Step 3: Add validation** in `Generator.Validate.Quests.cs`, inside the per-quest objective loop (where `q.Objectives` is already iterated — alongside the existing showStage/completeStage checks). Build the alias-name set once per quest:

```csharp
                var aliasNames = new HashSet<string>(q.Aliases.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                foreach (var o in q.Objectives)
                {
                    // ... existing duplicate-index / showStage / completeStage checks ...
                    foreach (var t in o.Targets)
                        if (!string.IsNullOrEmpty(t.Alias) && !aliasNames.Contains(t.Alias))
                            Problems.Add($"quest '{q.EditorId}' objective {o.Index} target alias '{t.Alias}' is not an alias on this quest");
                }
```

(Place the `aliasNames` set just before the `foreach (var o in q.Objectives)` loop; merge the target check into that existing loop rather than adding a second loop.)

- [ ] **Step 4: Run the test — expect PASS.** Run the same filter; both cases green.

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Validate/Generator.Validate.Quests.cs tests/ModForge.Core.Tests/Build/ObjectiveTargetTests.cs
git commit -m "feat(quest): validate objective target alias exists on the quest" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: B — xmarker / xmarkerHeading placement kind

**Files:**
- Modify: `src/ModForge.Core/Build/Generator.Build.Placements.cs`
- Modify: `src/ModForge.Core/Spec/Spec.World.cs` (doc the new `kind` values on `PlacementSpec.Kind`)
- Test: `tests/ModForge.Core.Tests/Build/XMarkerKindTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class XMarkerKindTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Xmarker_kind_defaults_base_and_forces_persistent()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "MeetSpot", Kind = "xmarker", Cell = "Room",
            Position = new Vec3 { X = 10, Y = 20, Z = 30 },
        });
        var mod = Build(spec);
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var anchor = cell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "MeetSpot");
        Assert.Equal(0x3Bu, anchor.Base.FormKey.ID);            // XMarker
        Assert.Equal("Skyrim.esm", anchor.Base.FormKey.ModKey.FileName);
    }
}
```

(If the cell-traversal helper differs in this codebase, match the pattern used in the existing placement/worldspace tests — `WorldspaceRegionTests.cs` / `PackageTests.cs` show how built cells are reached.)

- [ ] **Step 2: Run it — expect FAIL** (base resolves empty → placement skipped; no anchor).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~XMarkerKindTests"`

- [ ] **Step 3: Implement** in `Generator.Build.Placements.cs`.

(3a) Just before the `if (!TryResolveRef(pl.Base, formKeyByEd, out var baseFk))` line, default the base for marker kinds:
```csharp
                var baseRef = pl.Base;
                bool isXMarker = pl.Kind.Equals("xmarker", StringComparison.OrdinalIgnoreCase);
                bool isXMarkerHeading = pl.Kind.Equals("xmarkerHeading", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(baseRef) && isXMarker) baseRef = "Skyrim.esm:0x0000003B";
                else if (string.IsNullOrWhiteSpace(baseRef) && isXMarkerHeading) baseRef = "Skyrim.esm:0x00000034";
```
Then change the resolve call to use `baseRef`:
```csharp
                if (!TryResolveRef(baseRef, formKeyByEd, out var baseFk))
                { Warn($"  ! placement: base '{baseRef}' unresolved — skipped"); continue; }
```

(3b) In the `bool persistent = ...` expression, OR in the marker kinds (a quest-target anchor must persist):
```csharp
                bool persistent = pl.Persistent
                    || isXMarker || isXMarkerHeading
                    || pl.LinkedRefs.Count > 0
                    // ... rest unchanged ...
```

- [ ] **Step 4: Doc the kind** — extend the `Kind` comment on `PlacementSpec` in `Spec.World.cs` to mention `"xmarker"` / `"xmarkerHeading"` (auto base + forced persistent; bind with a `forced:<editorId>` alias to use as an objective target).

- [ ] **Step 5: Run the test — expect PASS.**

- [ ] **Step 6: Commit**

```bash
git add src/ModForge.Core/Build/Generator.Build.Placements.cs src/ModForge.Core/Spec/Spec.World.cs tests/ModForge.Core.Tests/Build/XMarkerKindTests.cs
git commit -m "feat(world): xmarker/xmarkerHeading placement kind (auto base + persistent anchor)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: C — mapMarkers spec + build

**Files:**
- Create: `src/ModForge.Core/Spec/Spec.MapMarkers.cs` (MapMarkerSpec) — or add to `Spec.World.cs` if it stays under 300 lines
- Modify: `src/ModForge.Core/Spec/Spec.cs` (ModSpec.MapMarkers list)
- Create: `src/ModForge.Core/Build/Generator.Build.MapMarkers.cs`
- Modify: `src/ModForge.Core/Build/Generator.Build.cs` (call the pass)
- Test: `tests/ModForge.Core.Tests/Build/MapMarkerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class MapMarkerTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void MapMarker_emits_placed_object_on_mapmarker_base_with_xmrk()
    {
        var spec = new ModSpec();
        spec.MapMarkers.Add(new MapMarkerSpec
        {
            EditorId = "MF_HiddenCamp", Name = "Hidden Camp",
            Worldspace = "Skyrim.esm:0x00003C",       // Tamriel
            Position = new Vec3 { X = 1000, Y = 2000, Z = 0 },
            Type = "Camp", Flags = { "Visible", "CanTravelTo" },
        });
        var mod = Build(spec);
        var marker = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(r => r.EditorID == "MF_HiddenCamp");
        Assert.Equal(0x10u, marker.Base.FormKey.ID);                 // MapMarker static
        Assert.NotNull(marker.MapMarker);
        Assert.Equal(MapMarker.MarkerType.Camp, marker.MapMarker!.Type);
        Assert.True(marker.MapMarker.Flags.HasFlag(MapMarker.Flag.Visible));
        Assert.True(marker.MapMarker.Flags.HasFlag(MapMarker.Flag.CanTravelTo));
    }
}
```

(This test reaches the exterior cell via worldspace resolution — `Skyrim.esm:0x00003C` is Tamriel; the build resolves it through the same `ExteriorCell` path placements use. If that path needs the master loaded and CI is offline, mark this single assertion `[Trait("Category","RequiresSkyrim")]` and add an offline companion that asserts only the MapMarker subrecord shape on a placement into an in-spec interior cell instead.)

- [ ] **Step 2: Run it — expect FAIL** (no `MapMarkers` / `MapMarkerSpec`).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~MapMarkerTests"`

- [ ] **Step 3: Add the spec type** — create `src/ModForge.Core/Spec/Spec.MapMarkers.cs`:

```csharp
using System.Collections.Generic;

namespace ModForge;

// A permanent world-map marker (XMRK on a REFR whose base is the vanilla MapMarker static). Independent
// of any quest, but — being a persistent named REFR — it can be a `forced:<editorId>` alias target, so
// it can double as an objective target. `type` is a MapMarker.MarkerType name (City/Town/Cave/Camp/…,
// None if empty); `flags` are MapMarker.Flag names (Visible | CanTravelTo | ShowAllIsHidden). Empty
// flags = the marker stays hidden until the player discovers it.
public sealed class MapMarkerSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Worldspace { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public string Type { get; set; } = "";
    public List<string> Flags { get; set; } = new();
}
```

Add to `ModSpec` in `Spec.cs`:
```csharp
    public List<MapMarkerSpec> MapMarkers { get; set; } = new();
```

- [ ] **Step 4: Add the build pass** — create `src/ModForge.Core/Build/Generator.Build.MapMarkers.cs`:

```csharp
using System;
using System.Linq;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- world-map markers (XMRK). Each → a persistent PlacedObject on the MapMarker static,
        // carrying a MapMarker subrecord (name + type + flags). Registered in formKeyByEd so a
        // `forced:` alias / linked ref can target it (lets a map marker double as a quest target). ---
        public void BuildMapMarkers()
        {
            const string MapMarkerBase = "Skyrim.esm:0x00000010";
            foreach (var mm in spec.MapMarkers)
            {
                if (string.IsNullOrWhiteSpace(mm.Worldspace))
                { Warn($"  ! mapMarker '{mm.EditorId}': no worldspace — skipped"); continue; }
                int cx = PosToGrid(mm.Position.X), cy = PosToGrid(mm.Position.Y);
                var cell = ExteriorCell(mm.Worldspace, cx, cy);
                if (cell is null) { Warn($"  ! mapMarker '{mm.EditorId}': worldspace '{mm.Worldspace}' unresolved — skipped"); continue; }
                if (!TryResolveRef(MapMarkerBase, formKeyByEd, out var baseFk)) continue;

                var marker = new MapMarker();
                if (!string.IsNullOrEmpty(mm.Name)) marker.Name = mm.Name;
                if (!string.IsNullOrWhiteSpace(mm.Type) && Enum.TryParse<MapMarker.MarkerType>(mm.Type, true, out var mt))
                    marker.Type = mt;
                foreach (var f in mm.Flags)
                    if (Enum.TryParse<MapMarker.Flag>(f, true, out var fl)) marker.Flags |= fl;

                var rec = new PlacedObject(mod)
                {
                    Placement = new Placement
                    {
                        Position = new Noggog.P3Float(mm.Position.X, mm.Position.Y, mm.Position.Z),
                        Rotation = new Noggog.P3Float(0, 0, 0),
                    },
                    MapMarker = marker,
                };
                rec.Base.SetTo(baseFk);
                if (!string.IsNullOrWhiteSpace(mm.EditorId))
                {
                    rec.EditorID = mm.EditorId;
                    formKeyByEd[mm.EditorId] = rec.FormKey;
                    recordsByEd[mm.EditorId] = rec;
                }
                cell.Persistent.Add(rec);     // map markers persist
            }
        }
    }
}
```

(Confirm the exact names `PosToGrid` / `ExteriorCell` / `recordsByEd` against `Generator.Build.Placements.cs` and `Generator.BuildContext.cs`; reuse them verbatim. If `MapMarker.Name` setter wants a `TranslatedString`, assign the string directly — there is an implicit conversion in Mutagen.)

- [ ] **Step 5: Call the pass** in `Generator.Build.cs` — add right after `ctx.BuildPlacements();`:
```csharp
        ctx.BuildMapMarkers();                     // world-map markers (XMRK PlacedObject on MapMarker static)
```

- [ ] **Step 6: Run the test — expect PASS.** (If offline CI lacks the master, see the Step-1 note and split the assertion.)

- [ ] **Step 7: Commit**

```bash
git add src/ModForge.Core/Spec/Spec.MapMarkers.cs src/ModForge.Core/Spec/Spec.cs src/ModForge.Core/Build/Generator.Build.MapMarkers.cs src/ModForge.Core/Build/Generator.Build.cs tests/ModForge.Core.Tests/Build/MapMarkerTests.cs
git commit -m "feat(world): mapMarkers[] — world-map markers (XMRK) on the MapMarker static" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: C — mapMarker validation

**Files:**
- Modify: `src/ModForge.Core/Validate/Generator.Validate.World.cs` (or `Generator.Validate.World.More.cs` — whichever holds placement/worldspace checks)
- Test: `tests/ModForge.Core.Tests/Build/MapMarkerTests.cs` (add a case)

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public void MapMarker_bad_type_and_flag_are_validate_problems()
    {
        var spec = new ModSpec();
        spec.MapMarkers.Add(new MapMarkerSpec
        {
            EditorId = "MF_Bad", Worldspace = "Skyrim.esm:0x00003C",
            Type = "Nonsense", Flags = { "Glowing" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("Nonsense"));
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("Glowing"));
    }
```

- [ ] **Step 2: Run it — expect FAIL.**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~MapMarkerTests"`

- [ ] **Step 3: Implement** — add a `ValidateMapMarkers()`-style block to the world validator (call it from the same place sibling world checks are invoked):

```csharp
            foreach (var mm in spec.MapMarkers)
            {
                if (string.IsNullOrWhiteSpace(mm.Worldspace))
                    Problems.Add($"mapMarker '{mm.EditorId}' has no worldspace");
                if (!string.IsNullOrWhiteSpace(mm.Type) && !Enum.TryParse<MapMarker.MarkerType>(mm.Type, true, out _))
                    Problems.Add($"mapMarker '{mm.EditorId}' has unknown type '{mm.Type}' (e.g. City/Town/Cave/Camp/Fort/Landmark)");
                foreach (var f in mm.Flags)
                    if (!Enum.TryParse<MapMarker.Flag>(f, true, out _))
                        Problems.Add($"mapMarker '{mm.EditorId}' has unknown flag '{f}' (Visible | CanTravelTo | ShowAllIsHidden)");
            }
```

(Add `using Mutagen.Bethesda.Skyrim;` to the validator file if not present.)

- [ ] **Step 4: Run the test — expect PASS** (both cases).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Validate/Generator.Validate.World.cs tests/ModForge.Core.Tests/Build/MapMarkerTests.cs
git commit -m "feat(world): validate mapMarker type + flags" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Worked example exercising all three

**Files:**
- Create: `examples/quest-markers.json`
- (verification only — no test file)

- [ ] **Step 1: Write the example** — a small spec that combines the three: a side quest whose objective points at an alias `forced:` to an in-spec `kind:"xmarker"` anchor, plus a `mapMarkers[]` entry, plus a second objective target pointing at the map marker (via another `forced:` alias) to prove C feeds A.

```jsonc
{
  "pluginName": "ModForgeQuestMarkers.esp",
  "esl": true,
  "cells": [ { "editorId": "MFQM_Cell", "name": "Marker Test Room" } ],
  "placements": [
    { "editorId": "MFQM_Anchor", "kind": "xmarker", "cell": "MFQM_Cell", "position": { "x": 0, "y": 0, "z": 0 } }
  ],
  "mapMarkers": [
    { "editorId": "MFQM_Camp", "name": "Test Camp", "worldspace": "Skyrim.esm:0x00003C",
      "position": { "x": 1000, "y": 2000, "z": 0 }, "type": "Camp", "flags": ["Visible", "CanTravelTo"] }
  ],
  "quests": [
    {
      "editorId": "MFQM_Quest", "name": "Marker Demo", "type": "SideQuest",
      "stages": [ { "index": 10, "startUpStage": true } ],
      "aliases": [
        { "name": "AnchorSpot", "fill": "forced:MFQM_Anchor" },
        { "name": "CampSpot",   "fill": "forced:MFQM_Camp" }
      ],
      "objectives": [
        { "index": 10, "text": "Go to the anchor", "showStage": 10,
          "targets": [ { "alias": "AnchorSpot", "compassIgnoresLocks": true } ] },
        { "index": 20, "text": "Then visit the camp",
          "targets": [ { "alias": "CampSpot" } ] }
      ]
    }
  ]
}
```

- [ ] **Step 2: Validate it**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/quest-markers.json`
Expected: `no problems`.

- [ ] **Step 3: Build it**

Run: `dotnet run --project src/ModForge.Cli -- build examples/quest-markers.json /tmp/mfqm.esp`
Expected: build succeeds; output mentions the quest, placement, and map marker. (Build needs `MODFORGE_SKYRIM_DATA` only if a step clones a master record — these don't; external refs resolve to FormKeys offline.)

- [ ] **Step 4: Commit**

```bash
git add examples/quest-markers.json
git commit -m "examples: quest-markers — objective QSTA + xmarker anchor + mapMarker (C feeds A)" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Maintenance chain — CODE_MAP + SPEC + schema

**Files:**
- Modify: `docs/CODE_MAP.dialogue-quests.md` (objective QSTA: new builder file + Tests row)
- Modify: `docs/CODE_MAP.world.md` (xmarker kind, mapMarkers builder + Tests rows)
- Modify: `docs/SPEC-dialogue-quests.md` (ObjectiveSpec.targets section)
- Modify: `docs/SPEC-world.md` (xmarker kind + mapMarkers section)
- Modify: `examples/spec.schema.json` (targets on objective, MapMarkerSpec, kind enum hint)

- [ ] **Step 1: CODE_MAP.dialogue-quests.md** — add a row for `Generator.Build.ObjectiveTargets.cs` (objective QSTA wiring, pass-2 after aliases) in the quest/dialogue build section, and add `ObjectiveTargetTests.cs` to the Tests column. Note the `ObjectiveTargetSpec` field on `Spec.Dialogue.cs`'s row.

- [ ] **Step 2: CODE_MAP.world.md** — note the `xmarker`/`xmarkerHeading` kind on the `Generator.Build.Placements.cs` row; add rows for `Spec.MapMarkers.cs`, `Generator.Build.MapMarkers.cs`, and the mapMarker validation in the world validator; add `MapMarkerTests.cs` + `XMarkerKindTests.cs` to the Tests column.

- [ ] **Step 3: SPEC-dialogue-quests.md** — under the quest/objective section, document `objectives[].targets[]`: `{ alias, compassIgnoresLocks, conditions[] }`, the alias-fill→marker semantics (actor = mark person, location/ref = mark place), and that the arrow follows the alias fill. Cross-reference the xmarker anchor and mapMarker as target sources.

- [ ] **Step 4: SPEC-world.md** — document the `kind:"xmarker"|"xmarkerHeading"` placement helper (auto base 0x3B/0x34, forced persistent, bind via `forced:` alias), and the new `mapMarkers[]` array (editorId/name/worldspace/position/type/flags + the MarkerType + Flag value lists). Note a map marker is itself a `forced:`-targetable ref.

- [ ] **Step 5: spec.schema.json** — add `targets` to the objective object, a `MapMarkerSpec` definition + `mapMarkers` array on the root, and extend the placement `kind` description to mention the marker kinds. (Manual; schema may lag but update here.)

- [ ] **Step 6: Full offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: all green (prior 465 + the new marker tests).

- [ ] **Step 7: Commit**

```bash
git add docs/CODE_MAP.dialogue-quests.md docs/CODE_MAP.world.md docs/SPEC-dialogue-quests.md docs/SPEC-world.md examples/spec.schema.json
git commit -m "docs(quest-markers): CODE_MAP + SPEC + schema for objective targets, xmarker kind, mapMarkers" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-review notes

- **Spec coverage:** A (objective→QSTA, Tasks 1–2), B (xmarker kind, Task 3), C (mapMarkers, Tasks 4–5) all covered; example (6) and docs (7) close the maintenance chain.
- **Type consistency:** `ObjectiveTargetSpec.Alias/CompassIgnoresLocks/Conditions`, `QuestObjectiveTarget.AliasID/Flags/Conditions`, `Quest.TargetFlag.CompassMarkerIgnoresLocks`, `MapMarker.MarkerType`/`MapMarker.Flag`, `MapMarkerSpec.EditorId/Name/Worldspace/Position/Type/Flags` — used identically across tasks. All verified against Mutagen 0.49 this session.
- **Offline risk:** the only master-dependent step is exterior-cell resolution for map markers (Task 4/6 use `Skyrim.esm:0x00003C` Tamriel). If headless CI lacks the master, split the MapMarker shape assertion into an interior-cell offline test and gate the worldspace one behind `RequiresSkyrim` (noted inline). Everything else is pure-FormKey offline.
- **Placeholder scan:** none — every code/step is concrete. The only "confirm against the file" notes are deliberate (helper method names `PosToGrid`/`ExteriorCell`/`recordsByEd`, cell-traversal in tests) since exact local signatures should be matched, not guessed.
