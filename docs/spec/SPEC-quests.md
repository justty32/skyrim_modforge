# ModForge spec — quest stages, Story Manager & scripts

← [index](SPEC-index.md) · dialogue/banter/scenes & CTDA conditions → [SPEC-dialogue](SPEC-dialogue.md)

### Quest stages, log entries & objective wiring
A quest's `stages[]` are integer milestones the quest can be **set to** (10, 20, 30…). Each stage
optionally writes a **journal log entry** + a quest-state flag. Objectives display/complete as stages
are set; a `dialogue` line can advance a stage when picked.

```jsonc
"quests": [{
  "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
  "startGameEnabled": true, "priority": 60,
  "stages": [
    { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
    { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank.",
      "conditions": [ { "function": "GetStage", "comparison": "GreaterThanOrEqualTo",
                        "value": 10, "param": "MF_ErrandQuest" } ] },   // optional CTDA gate on the log entry
    { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true }   // closes the quest
  ],
  "objectives": [
    { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
    { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 }
  ]
}]
```
- **`stages[]`** — `index` (unique, **ascending**), `logEntry` (journal text; omit for a silent
  milestone), `completeQuest` / `failQuest` (QuestLogEntry flag that closes / fails the quest at this
  stage — at most one), `conditions` (optional CTDA gate on the log entry, built with the shared
  **ConditionSpec**: `function`, `comparison` (`==`/`>=`/… or `EqualTo`/…, default `>=`), `value`,
  `param` (ref → the function's form parameter, e.g. the quest for `GetStage`)).
- **`stages[].startUpStage`** — marks the stage the engine **auto-runs `SetStage` to the instant the
  quest starts** (vanilla QSDT "Start Up Stage" flag). How a **Story-Manager-triggered** quest shows its
  opening log entry / first objective with **no external `SetStage`** — without it an SM quest sits
  silently at stage 0. At most one per quest. **IN-GAME CONFIRMED 2026-06-05.**
- **`stages[].instanceGlobals[]`** — bind GLOBs to **this quest instance** when the stage runs
  (gather/count **radiant** quests). The stage fragment calls `UpdateCurrentInstanceGlobal(<global>)` so
  objective text `<Global=MF_ItemCount>/<Global=MF_ItemTotal>` shows **per-instance** counts (one
  template, many copies with different counts — Missives' trick). Each entry: `{ "global":
  "<GLOB editorId>", "randomMin": N, "randomMax": M }` (seed `SetValue(Utility.RandomInt(N,M))`), or
  `{ "global": "…", "value": V }` (seed `SetValue(V)`), or `{ "global": "…" }` (bind only). Declare the
  GLOB in `globals[]`; ModForge ships `<quest>_Stages.psc` for `package` to compile. Put it on the
  `startUpStage` to roll the target on start. Demo `examples/gather_quest_spec.json`. (Pickup script is yours.)
- **`objectives[].showStage` / `.completeStage`** — link an objective to stages: `SetObjectiveDisplayed`
  at `showStage`, `SetObjectiveCompleted` at `completeStage`. `-1` (default) = "not stage-linked".
- **`objectives[].targets[]`** — the compass/map **markers** for an objective (QSTA). Each target is
  `{ "alias": "<aliasName>", "compassIgnoresLocks": false, "conditions": [...] }`. The marker arrow
  follows whatever the **alias is filled with** at runtime: fill the alias with an actor to mark a
  **person**, or with a location/ref (a door, an `kind:"xmarker"` anchor, or a `mapMarkers[]` entry)
  to mark a **place**. Several targets = several markers (vanilla "kill any of X/Y/Z"). `alias` must
  name an alias on the **same quest**. `compassIgnoresLocks` shows the compass marker through locked
  doors; `conditions` are per-target CTDA (the marker only shows while they pass). The objective must
  be **displayed** (via `showStage` or a script) for its marker to appear. To mark a fixed spot with
  no NPC, place an `kind:"xmarker"` anchor and bind it with a `forced:<editorId>` alias. See
  `examples/quest-markers.json`.
- **`dialogue[].setStage`** — picking that topic advances the host quest to this stage. To advance a
  stage from a NON-dialogue action (e.g. activating a runtime-spawned ref), attach an **alias script**
  (`alias[].script`) whose `OnActivate` calls `GetOwningQuest().SetStage(N)` — the reusable
  `examples/MFSE_AdvanceStage.psc` does exactly this. End-to-end journal-progression demo (start-up
  stage shows the objective on SM start → alias `OnActivate` completes it + closes the quest):
  `examples/story-manager-queststage.json`.

**What's record-only vs. what needs Papyrus:** stages, log entries, the `completeQuest`/`failQuest`
flags and log-entry conditions are **pure record data** — they build, `dump`/`questdiag` cleanly,
and the engine reads them directly. But *displaying* an objective on stage-set and *advancing* a
stage from a dialogue line require **Papyrus fragments**. The `package` command handles this
end-to-end (**no CK needed, IN-GAME CONFIRMED It.36 2026-06-02**):

1. Generates `Scripts/Source/<quest>_Stages.psc` — one `Fragment_Stage_XXXX_Item00000()` function
   per stage that shows/completes objectives (CK-standard naming; engine calls it when `SetStage()` fires).
2. Generates `Scripts/Source/TIF_<dialogue>.psc` — `extends TopicInfo Hidden`, with an explicit
   `Quest Property OwningQuest Auto` bound to the quest FormKey; `Fragment_0` calls
   `OwningQuest.SetStage(N)`. Uses `OnBegin` (fires when the player selects the line).
   **Do not use `GetOwningQuest()` — it returns None for StartGameEnabled quests on game-load.**
3. Compiles both `.psc` → `.pex` with the Linux-native `papyrus-compiler` (falls back to Wine/CK).
4. Attaches the VMAD to the QUST (`QuestScriptFragment.Unknown2=1` required — the enable flag; 0
   skips the fragment even when `SetStage()` fires) and to the INFO (`DialogResponsesAdapter`, `OnBegin`).
5. Auto-adds a `GetStage(quest) < setStage` condition on every `setStage` dialogue line so the NPC
   won't repeat it after the player has already picked it.

Inspect any quest with `questdiag <plugin> <0xFORMID>`. Dialogue still only registers on a game
**LOAD** (see the gotcha above). Worked example: `examples/quest_stages_spec.json`.

**Other generated result actions** (the same TIF fragment can combine several — no per-mod script):

- **`hello: true`** — emit the line as the NPC's auto-spoken **greeting** (`Misc`/`Hello`), not a player
  menu option. Combine with `identity`/`primaryIdentity`/`conditions` to greet differently by state; the
  engine plays the highest-priority matching Hello, else the NPC's plain `greeting`. (State-varying
  greetings go in ONE Hello topic as multiple ordered INFOs — `hello:true` lines for the same speaker+quest
  are merged automatically; specific conditioned lines first, plain fallback last.) `prompt` is ignored.
- **`setPrimaryIdentity: "<id>"|"auto"`** — override the player's primary identity (see [SPEC-identities](SPEC-identities.md)).
- **`openBarter: true`** — open the trade menu with the speaking vendor NPC (`Actor.ShowBarterMenu()`).
- **`rewardItem` (a ref) + `rewardCount`** — give the player that item/gold (`Game.GetPlayer().AddItem`).
- **`evaluateSpeakerPackages: true`** — re-evaluate the speaker's AI packages now, so a package newly
  enabled by this line's `setStage` (e.g. a Follow PACK gated on `GetStage==N`) activates immediately.

**Escort/follow quest pattern** (pure record + the actions above): a quest with stages 10/20 + an
objective; a Follow PACK (`template` `0x019B2C`, target = player) with `conditions:
[{ function: "GetStage", value: 10, param: "<quest>" }]`; the NPC carrying `[followPkg, standSandbox]`;
an `identity`-gated "I'll escort you" line (`setStage: 10`, `evaluateSpeakerPackages: true`) and a
"we've arrived" line (`conditions: GetStage==10`, `setStage: 20`, `rewardItem`). See
`examples/identity-paladin.json` (the Adventurer-gated Wary Traveler escort).

#### `persist` / `syncPerks` — JContainers JFormDB per-Form state (Idea #20 skill tree, Phase 0)

A `persist` block writes **nested per-Form state** to a [JContainers](https://www.nexusmods.com/skyrimspecialedition/mods/16495)
`JFormDB` storage, and a `syncPerks` block applies perks from that stored state — the persistence layer
of the in-world skill tree (an NPC "grows" perks from saved skill ranks, no Campfire UI). Both can hang
on **two hosts**:

- a **dialogue line** (`dialogue[].persist` / `.syncPerks`) — runs in the line's TIF result fragment when
  the line is **picked**;
- a **quest stage** (`quest.stages[].persist` / `.syncPerks`) — runs in the stage fragment when the quest
  **reaches** that stage (bank state on a milestone, not a dialogue choice).

In both, the writes appear before the perk sync so a sync sees what was just stored.

- **`persist`** — `{ storage, key?, set: [...] }`. `storage` is the JFormDB storageName (the namespace
  bucket; becomes the first path component). `key` is the Form the state hangs on — see **Key** below.
  Each `set` entry is `{ path, <value>, delta? }`:
  - `path` — subpath under the storage, e.g. `".Endurance.nodes.Adaptation"` (the emitted path is
    `".<storage><path>"`).
  - exactly one value: `int` / `float` / `str` (→ `solveIntSetter`/`solveFltSetter`/`solveStrSetter`)
    or `form` (a ref → `solveFormSetter`, bound as a VMAD property).
  - `delta: true` (int/float only) — **add** to the current stored value (read-add-write) instead of
    replacing, for counters like accumulating XP/ratio.
- **`syncPerks`** — `{ storage, key?, nodes: [{ path, perk, minRank? }] }`. For each node, reads the
  stored rank (`solveInt`) and **AddPerk** when `rank >= minRank` (default 1), else **RemovePerk**, on
  the key actor. Idempotent — safe to run every time.

**Key** — three forms:
- `"speaker"` — the dialogue NPC (`akSpeakerRef`). **Dialogue lines only** (a quest stage has no speaker;
  validation rejects `"speaker"` on a stage). This is the default, so a stage `persist` must set `key`.
- `"player"` — `Game.GetPlayer()`.
- **any other value** — an arbitrary ref (an in-spec editorId or `<master>:0xFORMID`), bound as a Form
  property and used as the JFormDB key (e.g. key all state on a specific NPC base form / a stone that
  represents an NPC). For `syncPerks`, the ref should be an actor reference at runtime — `AddPerk` is
  guarded by `If (key as Actor)`, so a non-actor key simply no-ops.

Property names are namespaced per stage (`S0010_PF_0`, `S0010_SyncPerk_0`, …) so several stages in one
quest script never collide; a dialogue TIF uses the bare names (`PF_0`, `SyncPerk_0`, `PKey`, `SKey`).

**Lifecycle**: only the root-DB path API (`JFormDB.solveXxxSetter`/`solveInt`) is generated. JContainers
owns those roots and persists them with the save, so there is **no** `JValue.object()`/`retain()`/`release()`
handle to balance — the retain/release footgun is avoided by construction (resolves design unknown U5).

**Runtime/build needs**: JContainers SE must be installed in-game; compiling the generated `TIF_*.psc` /
`<quest>_Stages.psc` needs JContainers' own `.psc` on the Papyrus header path (`MODFORGE_PAPYRUS_BASE`) —
a main-machine step (see WAIT_USER). Worked example: `examples/npc_skill_persist_spec.json` (a trainer NPC).

### Story Manager quests — event-driven start

A quest can be **launched automatically by the Story Manager (SM)** in response to an
in-game event instead of starting on game load or via `SetObjectiveDisplayed`. Add a
`storyEvent` block to the quest and the build wires everything automatically (SMBN→SMQN
under the correct vanilla event root, `StartGameEnabled` cleared).

**In-game confirmed (2026-06-04)** on all five variant patterns (victim, killer, forced,
condition, ESL).

```jsonc
// minimal — triggers on any actor kill, Victim alias = the killed actor
{
  "editorId": "MFSM_Avenge", "name": "Avenge the Fallen",
  "stages": [ { "index": 10 } ],
  "storyEvent": { "event": "KillActor" },
  "aliases": [ { "name": "Victim", "fill": "fromEvent:victim" } ]
}
```

#### `storyEvent` fields

| Field | Type | Notes |
|---|---|---|
| `event` | string | Event name — see table below. **Required.** |
| `keyword` | string | `editorId` of a keyword in this spec. **Required for `ScriptEvent` only.** Multiple quests can share the same keyword (same filter branch). |
| `conditions` | ConditionSpec[] | Extra CTDA conditions on the SM branch (same shape as `dialogue[].conditions`). Gates whether SM tries to start this quest. |
| `locationFilter` | string[] | **Location-aware encounter sugar (#5).** LocType keyword refs (`LocTypeBanditCamp`, `LocTypeDungeon`, …). The build appends one `GetKeywordDataForCurrentLocation` event-condition per keyword, **OR'd** together — so the quest fires only when the player's new location has ANY listed LocType. Best with `event: "ChangeLocation"`. Pure CTDA (offline-verifiable). |
| `cooldownHours` | float | **Anti-spam cooldown (#6, EE_WITimeout pattern).** Min in-game hours between firings. The build creates a `<quest>_LastFired` float GLOB + attaches the reusable `MFEncounterCooldown` quest script (`OnInit` `Stop()`s the quest if it re-fired within the window). 0 = none. ⚠ runtime check needed (prebuilt `.pex` compiles on the main machine; see `WAIT_USER.md`). Hold detection = a `findMatchingLocation` location alias + a `LocAliasHasKeyword` condition (new fns also: `GetKeywordDataForCurrentLocation`, `LocationHasKeyword`). |

#### Supported events

| `event` | Triggered by | Slots available for `fill` |
|---|---|---|
| `KillActor` | Any actor killed | `victim`, `killer`, `location` |
| `ChangeLocation` | Actor enters a new location | `oldLocation`, `newLocation` |
| `CastMagic` | Spell cast | `caster`, `target`, `location` |
| `AddItem` | Item added to inventory | `owner`, `location` |
| `Assault` | Actor assaulted | `victim`, `attacker`, `location` |
| `CraftItem` | Player crafts an item at a station | `workbench` |
| `PlayerRemoveItem` | Item leaves player inventory (sold/dropped/given) | `owner`, `item` |
| `Arrest` | A guard arrests an actor | `guard`, `criminal` |
| `IncreaseLevel` | Player levels up | *(none — gate via `storyEvent.conditions`, e.g. `GetLevel`)* |
| `ScriptEvent` | Papyrus `SendStoryEvent` via the dispatcher | `ref1`, `ref2`, `location` |

#### `aliases` — dynamic alias fill

Each entry in `aliases` fills one quest alias when the quest starts. If any **required** alias
cannot be filled the quest silently does not start.

```jsonc
"aliases": [
  { "name": "Victim",    "fill": "fromEvent:victim" },        // slot from the event payload
  { "name": "Killer",    "fill": "fromEvent:killer" },
  { "name": "NewLoc",    "fill": "fromEvent:newLocation" },   // Location slot → alias Type=Location auto-set
  { "name": "TheBoss",   "fill": "uniqueActor:Skyrim.esm:0x01414D" },  // specific NPC (Ulfric)
  { "name": "TriggerRef","fill": "forced:Skyrim.esm:0x000014" },        // forced ref (player)
  { "name": "Spawned",   "fill": "createObject:Skyrim.esm:0x0010FE05@Caster" },  // spawn a wolf AT the Caster alias
  { "name": "Nearby",    "fill": "findMatching:closest",               // nearest ref in the loaded area…
    "conditions": [ { "function": "HasKeyword", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x013794" } ] },  // …matching these gates (nearest NPC)
  { "name": "Hatch",     "fill": "createObject:Skyrim.esm:0x0BCD2D@Caster",     // spawn a chest, then…
    "script": "MFSE_AliasActivate", "scriptSource": "MFSE_AliasActivate.psc",   // …OnActivate on the spawned ref
    "scriptProperties": [ { "name": "TheKW", "type": "object", "objectEditorId": "MFSE_AliasKW" } ] }
]
```

| `fill` prefix | Alias kind | Notes |
|---|---|---|
| `fromEvent:<slot>` | `FindMatchingRefFromEvent` | Slot names from the event table above. Location slots (`newLocation`, `oldLocation`, `location`) automatically set `QuestAlias.Type = Location`. |
| `uniqueActor:<ref>` | `UniqueActor` | Pinned to a specific NPC by ref; `AllowReserved` forced on. |
| `forced:<ref>` | `ForcedReference` | Static ref (e.g. the player `Skyrim.esm:0x000014`). |
| `createObject:<ref>@<targetAlias>` | `CreateReferenceToObject` | On quest start, **spawns a new reference** to `<ref>` (any placeable base — NPC/container/static/item) AT the ref held by `<targetAlias>` (`Create=At`, `Level=Easy`). `<targetAlias>` must be another **ref-type** alias in the same quest (not a Location), and cannot be itself. E.g. cast a spell → spawn a guardian at the caster. In-game confirmed (2026-06-05). |
| `findMatching:closest` / `findMatching:any` | `QuestAlias.Flag.MatchingRefInLoadedArea` (+ `MatchingRefClosest` for `closest`) | Fills with an **already-existing reference in the loaded area** matching this alias's `conditions` — `closest` picks the nearest match, `any` the first. The match filter is a CTDA list (same `ConditionSpec` shape) wired onto `QuestAlias.Conditions` (e.g. `HasKeyword ActorTypeNPC` = nearest NPC; `GetIsID <base>` = nearest of a base). **At least one condition is required.** This is the loaded-area "Find Matching Reference" mechanism decoded from vanilla `MQGreybeardCall` Bystander aliases — **not** `FindMatchingRefNearAlias` (that only finds editor-linked-ref children). Whether the alias fills depends on a matching ref actually being in the loaded area at runtime. |
| `findMatchingLocation:<locTypeKeyword>[@<parentLocationAlias>]` | `QuestAlias.Type = Location` + `LocationAliasReference` | **Radiant LocationAlias (#7).** Fills a **Location**-type alias by "Find Matching Location" — pick a location whose **LocType keyword** matches (`<locTypeKeyword>` = an in-spec KYWD editorId or `Plugin.esm:0xID`), optionally narrowed to a child location **within** `@<parentLocationAlias>` (another Location alias in this quest). Emits `Location = {Keyword=<locType>, AliasID=<parent index>}`. The Missives radiant variety core: a Hold location, then a Dungeon/Inn location within it. |
| `findInLocationAlias:<locationAlias>[#<refTypeLCRT>]` | `QuestAlias.Type = Reference` + `LocationAliasReference` | **Radiant find-ref-in-location (#8).** Fills a **Reference**-type alias by "Find Matching Reference" scoped to the location held by `<locationAlias>` (a Location alias in this quest) — narrowed by an optional **RefType** LCRT (`#<refTypeLCRT>`, e.g. a dungeon `BossContainer`) and/or this alias's `conditions`. Emits `Location = {AliasID=<location index>, RefType=<LCRT>}`. **A refType and/or at least one condition is required.** Missives' Alias_target/Alias_chest (the boss/loot inside the dungeon). Uses `LocationAliasReference` (NOT `FindMatchingRefNearAlias`, which is verified linked-ref-child-only). |

**Extra alias options:**

| Field | Default | Notes |
|---|---|---|
| `allowReserved` | `false` | Set `true` if the target NPC may be reserved by another quest (`ReservesLocationOrReference`). Without this, the alias fails to fill and the quest doesn't start. `uniqueActor` forces it on. |
| `script` / `scriptSource` / `scriptProperties` | — | Attach a Papyrus **alias script** to this alias (a `ReferenceAlias`-extending script stored on the quest's `QuestAdapter.Aliases` VMAD, bound to the alias ID). It travels with **whatever ref fills the alias** — including a `createObject`-spawned or `findMatching`-matched ref that no base-object script could reach. Classic use is `Event OnActivate(ObjectReference akActionRef)`: activating the aliased ref runs it (e.g. call `MFStoryEventDispatch.Fire(...)` to chain a story event). `script` = the Scriptname, `scriptSource` = the `.psc` for `package` to compile, `scriptProperties` bind its Auto properties (same shape as a dialogue `resultProperties`). You supply the compiled `.pex`. In-game confirmed (2026-06-05); reusable helper `examples/MFSE_AliasActivate.psc`. |

**Aliases on an ordinary quest (no `storyEvent`):** the same `aliases[]` block works on a normal
**StartGameEnabled** quest — `forced`/`uniqueActor`/`createObject`/`findMatching` fills + an alias
`script` all apply (only `fromEvent` is invalid — no event to pull from; validator flags it). Aliases
fill when the quest starts (= game load). In-game confirmed (2026-06-05); demo
`examples/quest-alias-standalone.json` (forced player → `createObject` chest → `OnActivate` advances).

**Radiant chain (`findMatchingLocation` + `findInLocationAlias`):** these compose into the Missives
variety pattern — `Hold` (`findMatchingLocation:<holdLocType>`) → `Dungeon`
(`findMatchingLocation:<dungeonLocType>@Hold`) → `BossChest`
(`findInLocationAlias:Dungeon#<bossLCRT>`). Demo `examples/radiant_alias_spec.json`. ⚠ the
`LocationAliasReference` *shape* is reflection-verified but the CK *semantics* + example LocType/LCRT
FormIDs need a main-machine xEdit byte-compare (`gamedata find` for real IDs). See `WAIT_USER.md`.

#### `spawn` — dynamic near-player spawn (F組 #3)
A quest's `spawn` block: on quest start, place `count` copies of `form` (ActorBase / LeveledNpc) at a
random `minDistance`..`maxDistance` offset around the player, then (`snapToNavmesh`, default true) toggle
`EnableAI` so each snaps to the nearest navmesh point — a legal walkable spawn with **no pre-placed
markers** (the EE NavmeshTester trick), via the reusable `MFDynamicSpawn` quest script. Pair with
`ChangeLocation` + `locationFilter` + `cooldownHours` for a rate-limited location-aware encounter
(`examples/location_encounter_spec.json`). ⚠ runtime (`PlaceAtMe`+`EnableAI` snap) needs an in-game check; `.pex` compiles on the main machine. See `WAIT_USER.md`.

#### SM iron laws (engine behaviour, not bugs)

- **One event → one quest starts** — the engine tries quest nodes in order and starts the first one
  whose conditions pass. A second unconditional quest on the same event never starts. Use conditions.
- **`SimpleActor` critters don't fire `KillActor`** — killing chickens, rabbits, etc. produces
  no SM event. Target proper actors (bandits, wolves, NPCs).
- **Any required alias that fails to fill → quest doesn't start, silently.** Make aliases
  optional only if the quest can function without them.
- **ESL plugins work fine with SM records.** No need to use an ESP just for SM content.

#### ScriptEvent — sending your own story events

`ScriptEvent` lets Papyrus code trigger SM quests without relying on a vanilla event.
The build embeds the shared dispatcher (`MFStoryEventDispatch.pex`) into the packaged mod
automatically — you don't compile anything per-quest.

```jsonc
// 1. declare the keyword that identifies your event channel
"keywords": [ { "editorId": "MY_StoryKW" } ],

// 2. the quest that responds to it
"quests": [{
  "editorId": "MY_QuestOnFire",
  "storyEvent": { "event": "ScriptEvent", "keyword": "MY_StoryKW" },
  "aliases": [ { "name": "Target", "fill": "fromEvent:ref1" } ]
}]
```

From Papyrus (any script), fire the event:
```papyrus
; MFStoryEventDispatch is the embedded global script
MFStoryEventDispatch.Fire(MY_StoryKW, akRef1, akRef2, akLocation)
```

The dispatcher calls `MY_StoryKW.SendStoryEvent(...)` which the engine routes to every
matching SM quest node. One dispatcher `.pex` serves all mods — `package` copies it into
`Scripts/` automatically when any ScriptEvent quest is present.

**Wiring `Fire()` to a real trigger.** Any Papyrus context can call the dispatcher. A reusable
pattern is a magic-effect script: attach it to a custom MGEF, set a keyword property, and casting
the spell fires the story event with the caster as `ref1`:

```papyrus
Scriptname MFSE_SpellTrigger extends ActiveMagicEffect
Keyword Property TheKW Auto
Event OnEffectStart(Actor akTarget, Actor akCaster)
    MFStoryEventDispatch.Fire(TheKW, akCaster, akTarget)
EndEvent
```

`package` compiles such a script beside the embedded dispatcher source automatically, so `Fire()`
resolves without any local Papyrus setup. The same shape works from dialogue fragments, alias
scripts, or activators — anywhere you can run one line of Papyrus.

The same one-line `Fire()` call works from any entry point — a small reusable trigger library:

| Entry point | Script (`extends …`) | Event | Example |
|-------------|----------------------|-------|---------|
| Magic effect (spell) | `ActiveMagicEffect` | `OnEffectStart` | `story-manager-magictrigger.json` (in-game ✓) |
| Magic effect (potion) | `ActiveMagicEffect` | `OnEffectStart` | `story-manager-potiontrigger.json` (same script, drink to fire; in-game ✓) |
| Activator | `ObjectReference` | `OnActivate` | `story-manager-activatortrigger.json` (pull a lever; in-game ✓) |
| Dialogue line | `TopicInfo` | `Fragment_0` | `story-manager-dialoguetrigger.json` (NPC gives a quest; in-game ✓) |

All four in-game verified 2026-06-05. Note for the activator: the `model` must be a NIF path that
actually exists in the load order — a wrong path spawns an invisible object with no error.

The first three attach one script to a record (MGEF / ACTI) with a `Keyword` property, set via the
spec's `scripts[]`. The dialogue trigger wires the script as a line's `resultScript` +
`resultScriptSource` + a `TheKW` `resultProperty`. `package` compiles all of them beside the
embedded dispatcher source automatically.

See also `examples/story-manager-scriptevent.json` + `examples/MFSE_TestTrigger.psc` (OnInit test).

### scripts — Papyrus attachment
```jsonc
{
  "targetEditorId": "MF_Q1",          // record to attach to (any editorId in the spec)
  "scriptName": "MFDemoQuestScript",  // must match the .pex/.psc Scriptname
  "source": "scripts/MFDemoQuestScript.psc",  // optional: .psc path (rel. to this spec);
                                              //  `package` compiles it via Wine
  "properties": [
    { "name": "GreetingCount", "type": "int",    "int": 3 },
    { "name": "PlayerRef",     "type": "object", "objectEditorId": "MF_Smith" }
  ]
}
```
- Property `type` ∈ `int | float | bool | string | object`. Set the matching value
  field: `int` / `float` / `bool` / `str`, or `objectEditorId` (for `object`, resolved
  to a FormLink). Properties are flagged *Edited* so the game reads them.
- Attaching works on any record that supports scripts (Quest, Npc, Activator,
  MagicEffect, Weapon, Armor, MiscItem, Book, Ingestible, …). The script `Name` must
  match the compiled `.pex`.

