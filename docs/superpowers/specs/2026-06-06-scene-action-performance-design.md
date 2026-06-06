# Scene action performances — movement + timer beats (IDEAS §1b, first slice)

**Date:** 2026-06-06
**Status:** Design — autonomous implementation (user delegated all decisions, "方向隨你")
**Source idea:** `docs/IDEAS.md` §1b (NPC 劇情演出 / Scene-driven performance)

## Goal

Let a scene do more than talk: an actor can **walk to a spot** (or run any AI package — sandbox,
patrol, …) and the scene can **pause for N seconds** between beats. This turns an SM-/presence-
triggered quest's scene from "two heads talking" into a visible little performance:
*NPC walks to the altar → waits → speaks → walks back.*

This is the **first slice** of §1b. In scope: **Package** scene actions (movement via the existing
Travel template; any spec-defined PACK) and **Timer** scene actions. Out of scope (documented as
future): sit/use-furniture (needs a new UseItemAt PACK template), and standalone PlayIdle/animation.

## Decoded vanilla truth (Skyrim.esm, via new `scnscan` + `packagediag`)

A non-Dialog scene beat is just another `SceneAction` on the Scene, distinguished by `Type`:

```
# dunTolvaldsCaveCrownScene 0x0F6788 — three ghosts walk to the crown
Type=Package  ActorID=3  Index=1  StartPhase=0  EndPhase=0  Packages=[0F6790:Skyrim.esm]
# 0F6790 dunTolvaldsCaveGhostCrownDefender01 = a TRAVEL package (template 0x016FAA):
#   Data[0] PackageDataLocation radius=256 target=LocationTarget(...)   ← the destination
#   Data[2] PackageDataBool (ride horse)   Data[4] PackageDataBool (prefer path)

# BardSongsInstrumentalLute02 0x1069C2 — bard travels to the bard-spot, plays, with timed beats
Type=Package  ActorID=16  Index=1  StartPhase=1  EndPhase=1  Packages=[0FB904,02AC7B]
Type=Timer    ActorID=16  Index=3  StartPhase=3  EndPhase=3  TimerSeconds=33
#   0FB904 BardSongsBardSpotTravel = Travel (LocationKeyword target);
#   02AC7B DefaultStayAtCurrentLocation = shared vanilla "stay put" package (chained).

# MQ306EsbernSit 0x0F1C94 — SIT package (FUTURE work, not this slice):
#   Data[16] PackageDataTarget SingleRef → SpecificReference(chair)   Data[3] float  Data[4] bool
```

Confirmed Mutagen API (`SceneAction`, Mutagen 0.53.1):
- `Type : TypeEnum` — `Dialog | Package | Timer`.
- Common: `ActorID:int?` (the alias index that performs it), `Index:uint?` (1-based, scene-wide),
  `StartPhase`/`EndPhase:uint?` (the phase-index window the action spans), `Name:string`.
- Package: `Packages : IList<IFormLink<IPackageGetter>>` — one or more PACK FormKeys, run in order.
- Timer: `TimerSeconds : float?`.
- Dialog (already built): `Topic`, `Emotion`, `EmotionValue`, `HeadtrackActorID`, `LoopingMin/Max`, `Flags`.

**Key reuse insight:** a vanilla scene Package action *just references a PACK FormKey*, and ModForge
already has a full PACK builder (Travel/Sandbox/Patrol/Follow/Escort/UseMagic/Sleep) with location
targeting + validation. So the slice is: **let a scene beat reference a package the author already
defines in `spec.packages[]`.** Zero new package plumbing; movement = a Travel package to a marker.

## Spec change (additive — existing scenes untouched)

`SceneSpec` gains an optional `actions[]`; `ScenePhaseSpec.lines` may now be empty (a pure "beat"
phase that exists only as a window for actions).

```jsonc
"scenes": [{
  "editorId": "MF_AltarRite", "questEditorId": "MF_RiteQuest",
  "actors": [ {"aliasId":0,"npc":"MF_Priest"}, {"aliasId":1,"npc":"MF_Acolyte"} ],
  "phases": [
    {},                                   // phase 0: no lines — a beat for the walk
    {"speaker":0, "lines":["Approach the altar."]},
    {"speaker":1, "lines":["As you say."]}
  ],
  "actions": [
    {"actor":1, "package":"MF_WalkToAltar", "startPhase":0, "endPhase":0},  // Package beat
    {"actor":1, "timerSeconds":2.0,         "startPhase":0, "endPhase":0}   // Timer beat
  ]
}]
```

```csharp
// SceneSpec:
public List<SceneActionSpec> Actions { get; set; } = new();

// One non-dialog scene beat. Exactly one of (package, timerSeconds>0):
//  * package  → a Package action: the actor runs the named PACK (a spec packages[] entry or external
//    ref) during the phase window. Movement = a Travel package whose destination is a placed marker.
//  * timerSeconds>0 → a Timer action: the phase window waits this many seconds (no actor behaviour).
public sealed class SceneActionSpec
{
    public int Actor { get; set; }                 // aliasId (from actors[]) that performs the action
    public string Package { get; set; } = "";      // ref → a PACK (spec packages[] editorId or <master>:0xID)
    public float TimerSeconds { get; set; }         // >0 → a Timer action instead of a Package action
    public int StartPhase { get; set; }             // first phase index (into phases[]) the action spans
    public int EndPhase { get; set; } = -1;         // last phase index; -1 = same as StartPhase
}
```

`ScenePhaseSpec.lines` empty ⇒ the phase still emits a `ScenePhase` (so action windows can target it)
but no Dialog action / SCEN topic. `speaker`/`emotion` are ignored for a lineless beat phase.

## Build (`Generator.Build.Scene.cs`)

1. The existing per-phase loop already adds one `ScenePhase` per `phases[]` entry. Change: when
   `ph.Lines.Count == 0`, add the empty `ScenePhase` but **skip** the topic + Dialog action (it's a
   pure beat). Dialog actions keep their current 1-based `Index` counter.
2. After the dialog loop, emit one SceneAction per `s.Actions`, continuing the same `actionIndex`:
   - Timer (when `TimerSeconds > 0`): `Type=Timer`, `ActorID`, `Index`, `StartPhase`, `EndPhase`,
     `TimerSeconds`.
   - Package (else): `Type=Package`, `ActorID`, `Index`, `StartPhase`, `EndPhase`; the `Packages[]`
     FormKey is a forward ref → **deferred to `WireScenes` (pass 2)** via a new `sceneActionWires`
     list (mirrors how `sceneAliasWires` defers the actor UniqueActor binding). `formKeyByEd` already
     contains every PACK by the time `WireScenes` runs (`BuildFormKeyTable` → `WireScenes`).
3. `scene.LastActionIndex = actionIndex - 1` (now counts dialog + non-dialog actions).
4. `EndPhase` default of -1 in the spec → resolved to `StartPhase` at build time.

`WireScenes`: for each `(sceneEd, action, packageRef)` wire, `Resolve` the package editorId/ref and
`action.Packages.Add(formKey)`.

## Validate (`Generator.Validate.Quests.cs`)

- Relax the "phase N has no lines" error: a lineless phase is allowed **iff** at least one action
  spans it; otherwise still an error ("phase N has no lines and no action covers it").
- Per action: `actor` must be a scene actor alias; `startPhase` in `[0, phases.Count)`; `endPhase` in
  `[startPhase, phases.Count)`; exactly one of (`package` non-empty, `timerSeconds > 0`); if
  `package` set, `CheckRef(package)`.

## Example + in-game test

`examples/scene-action-performance.json`: reuse the proven presence-banter shape (two essential
unique NPCs in the Sleeping Giant Inn + a StartGameEnabled host quest), add a Travel package
`MF_WalkToMarker` → an XMarker placement near them, a beat phase that runs it + a short timer, then
the dialogue. `coc RiverwoodSleepingGiantInn`, stand near both → actor walks to the marker, pauses,
then they talk. Packaged zip to `~/skyrim_mods/` for the user to run (I can't launch the game).

This is also the first in-game confirmation of non-Dialog scene actions in ModForge.

## Tests (`SceneTests.cs`)

- Package action: actions[] with a package → scene gets a `Type=Package` SceneAction whose `Packages`
  contains the PACK FormKey, correct `ActorID`/`StartPhase`/`EndPhase`, `Index` continuing past the
  dialog actions; `LastActionIndex` updated.
- Timer action: `timerSeconds` → `Type=Timer` action with `TimerSeconds` set, no `Packages`.
- Beat phase: a lineless phase emits a `ScenePhase` but no Dialog action / no SCEN topic for it.
- Regression: a scene with no `actions[]` and all phases lined → byte-identical behaviour to today.
- Validate: action with both package+timer → problem; out-of-range phase → problem; unknown actor →
  problem; lineless phase with no covering action → problem.

## Future work (decoded, deferred)

- **Sit / use furniture** (`MQ306EsbernSit` shape): register a UseItemAt PACK template (Data[16]
  `PackageDataTarget` SingleRef → a placed FURN ref, Data[3] float, Data[4] bool) so a scene actor
  can sit/use a workbench/altar. Needs the template FormKey decoded + a `furniture`-targeting PACK
  spec, then the same Package scene action references it.
- **PlayIdle / animation event names** (DAR/OAR for custom) — likely a Papyrus `PlayIdle` on an alias
  script beat rather than a SceneAction; revisit after sit lands.
- LipFile / camera (Camera Shot) — cosmetic, last.
