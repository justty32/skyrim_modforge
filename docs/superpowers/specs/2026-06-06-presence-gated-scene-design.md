# Presence-gated interaction Scene (隨從在場偵測 + 互動 Scene)

**Date:** 2026-06-06
**Status:** Design — approved for autonomous implementation (user delegated all decisions)
**Source idea:** `docs/IDEAS.md` §1a (隨從在場偵測方案 + Scene 觸發骨架)

## Goal

Let a spec author declare a two-NPC Scene that **plays itself whenever the player is co-present
with both actors** (and re-plays on a cooldown), instead of firing once on game-load. This is the
usable form of "follower banter": followers stay near the player, so the Scene fires repeatedly
while travelling.

Vertical slice: **named NPCs** (reuse the existing `UniqueActor`-bound Scene actors). Dynamic
"scan whoever is currently a teammate + `ForceRefTo`" is explicitly OUT of scope (a later layer).

## What already exists (reused, not rebuilt)

- `Generator.Build.Scene.cs` emits a complete two-NPC Scene from `SceneSpec`: a host quest, one
  `UniqueActor`-bound `QuestAlias` per actor, Scene record + actors, Phase + Dialog `SceneAction` +
  Scene-subtype `DialogTopic`/INFO per line. **Structural-only — not yet in-game confirmed.**
- `Scene.Flag.BeginOnQuestStart` (current default) auto-plays on quest start.
- `AttachScripts()` / `FillProperties()` already attach any script with int/float/bool/object
  properties to any record incl. a Quest (`QuestAdapter.Scripts`). Object props resolve via
  `formKeyByEd`, which `BuildFormKeyTable()` populates for **every** EditorID record incl. Scenes.
- Embed-a-`.pex`-in-the-CLI + ship-from-Package pattern (`MFStoryEventDispatch`, `Package.cs` §5b).

**Conclusion: almost no new plumbing.** The feature is a reusable controller `.psc` + a thin
build-side hook that attaches it when a new `autoStart` block is present + packaging + validate +
example.

## Spec change

Add an optional `autoStart` block to `SceneSpec` (new `SceneAutoStartSpec`). Additive/optional →
no breaking change; existing `scene_spec.json` keeps `beginOnQuestStart` behaviour untouched.

```json
"scenes": [{
  "editorId": "MF_TravelBanter",
  "questEditorId": "MF_BanterQuest",
  "autoStart": {
    "triggerDistance": 2048.0,   // max distance (units) from player to EACH actor; default 2048
    "requireLineOfSight": false, // also require player HasLOS both actors; default false
    "cooldownSeconds": 60.0,     // min real seconds between plays; default 60
    "pollSeconds": 5.0           // RegisterForSingleUpdate interval; default 5
  },
  "actors": [ {"aliasId":0,"npc":"..."}, {"aliasId":1,"npc":"..."} ],
  "phases": [ ... ]
}]
```

Semantics when `autoStart` is present:
- Build forces the Scene's `BeginOnQuestStart` **off** (the controller starts it, not quest-start).
- Build attaches the reusable controller script to the **host quest** with these properties:
  `BanterScene`(object→this Scene), `ActorAliasA`/`ActorAliasB`(int→first two actor aliasIds),
  `TriggerDistance`/`PollInterval`/`Cooldown`(float), `RequireLOS`(bool).
- Host quest must be `StartGameEnabled` (it is, in the example) so the controller's `OnInit` arms.

## Reusable controller script — `assets/papyrus/MFSceneBanterController.psc`

`extends Quest`. One prebuilt `.pex`, embedded in the CLI, serves every generated mod (mirrors the
dispatcher). Chained `RegisterForSingleUpdate` (NOT a persistent `OnUpdate` loop — save bloat).

```papyrus
Scriptname MFSceneBanterController extends Quest
Scene Property BanterScene Auto
Int   Property ActorAliasA = 0 Auto
Int   Property ActorAliasB = 1 Auto
Float Property TriggerDistance = 2048.0 Auto
Float Property PollInterval = 5.0 Auto
Float Property Cooldown = 60.0 Auto
Bool  Property RequireLOS = false Auto
float lastPlayed = 0.0

Event OnInit()
    RegisterForSingleUpdate(PollInterval)
EndEvent
Event OnUpdate()
    TryBanter()
    RegisterForSingleUpdate(PollInterval)   ; re-arm next poll
EndEvent

Function TryBanter()
    if BanterScene == None || BanterScene.IsPlaying()
        return
    endif
    float now = Utility.GetCurrentRealTime()
    if (now - lastPlayed) < Cooldown
        return
    endif
    Actor a = (GetAlias(ActorAliasA) as ReferenceAlias).GetReference() as Actor
    Actor b = (GetAlias(ActorAliasB) as ReferenceAlias).GetReference() as Actor
    if a == None || b == None || a.IsDead() || b.IsDead()
        return
    endif
    Actor player = Game.GetPlayer()
    if a.GetDistance(player) > TriggerDistance || b.GetDistance(player) > TriggerDistance
        return
    endif
    if a.IsInCombat() || b.IsInCombat() || player.IsInCombat()
        return
    endif
    if RequireLOS && (!player.HasLOS(a) || !player.HasLOS(b))
        return
    endif
    BanterScene.Start()
    lastPlayed = now
EndFunction
```

Design notes baked in:
- `GetCurrentRealTime()` (real seconds) for cooldown → independent of game timescale; `lastPlayed`
  is a member var (persisted in the save).
- `GetAlias(index) as ReferenceAlias` needs no new property type — int alias-index props work with
  existing `FillProperties` (a real `ReferenceAlias` property would need an alias-index on the
  object prop, which `MakeObjectProp` doesn't set).
- Combat/dead/LOS/cooldown gates; player-in-dialogue/menu check omitted (needs SKSE `UI`; the
  Scene actors' `DialoguePause` behaviour flag + cooldown are enough for the slice).
- `OnInit` arms on quest start. On a save where the mod was just added, the `StartGameEnabled`
  quest starts fresh → `OnInit` fires. (`.seq` is for dialogue topics, not relevant here.)

## C# changes (small)

- `Spec.Dialogue.cs`: `SceneAutoStartSpec` class + `SceneSpec.AutoStart` (nullable).
- `Generator.Build.Scene.cs`: in `BuildScenes`, when `s.AutoStart != null` → clear
  `BeginOnQuestStart`, and (deferred to `WireScenes`, after `formKeyByEd` is built so the Scene's
  own FormKey resolves) attach `MFSceneBanterController` to the host quest with the wired
  properties. Need the Scene record's editorId resolvable — it is (BuildFormKeyTable covers it).
- `Generator.Validate.Quests.cs`: when `autoStart` set — require host quest `StartGameEnabled`,
  require ≥2 actors, sane numeric ranges (warn if cooldown/poll ≤ 0, distance ≤ 0).
- `ModForge.Cli.csproj`: embed `MFSceneBanterController.pex` (+ `.psc`) as EmbeddedResource.
- `Package.cs`: §5c — ship `MFSceneBanterController.pex` into `Scripts/` when any scene has
  `autoStart` (mirror the dispatcher §5b block).
- Prebuild: compile `MFSceneBanterController.psc` once (same Wine+CK / native pipeline as the
  dispatcher); keep the `.pex` local (gitignored), document in CLAUDE.md prerequisite step.

## Example (end-to-end, in-game testable)

`examples/scene-presence-banter.json`: two placed unique NPCs in/near Riverwood + a
`StartGameEnabled` host quest + `autoStart` scene (short cooldown for testing, e.g. 15s,
triggerDistance ~1024). `coc` to them, stand near both → they banter; walk away + come back after
cooldown → they banter again. This is ALSO the first in-game confirmation of the base Scene record.

## Tests

- `SceneTests.cs`: autoStart present → Scene `BeginOnQuestStart` cleared; host quest carries a
  `MFSceneBanterController` ScriptEntry in `QuestAdapter.Scripts` with BanterScene object prop +
  ActorAliasA/B int props + float/bool config props. autoStart absent → unchanged (regression).
- Validate test: autoStart on a non-StartGameEnabled quest warns; <2 actors warns.

## Out of scope (later layers)

- Dynamic teammate scan + `ForceRefTo` alias fill (generic any-follower).
- Movement/animation/FURN Scene action types (IDEAS §1b).
- LOS between the two actors (vs player→actor); multi-line randomised banter sets; >2 actors.
- Player-in-menu/dialogue suppression (needs SKSE).
