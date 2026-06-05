# ModForge spec — dialogue, quests & scripts

← [index](SPEC-index.md)

### classes (CLAS)
An NPC's "profession" — set an npc's `class` ref to one. It drives the actor's attribute
distribution and favoured skills (and, for a trainer NPC, what it `teaches`).
```jsonc
{ "editorId": "MF_Battlemage", "name": "ModForge Battlemage",
  "teaches": "Destruction",        // a Skill the class can train (trainers); optional
  "maxTrainingLevel": 50,
  "healthWeight": 30, "magickaWeight": 50, "staminaWeight": 20,   // attribute split (~sum 100)
  "skillWeights": { "Destruction": 100, "Restoration": 75, "OneHanded": 50 } }  // Skill -> 0–255 favour
```
Skill names: `OneHanded`, `TwoHanded`, `Archery`, `Block`, `Smithing`, `HeavyArmor`, `LightArmor`,
`Pickpocket`, `Lockpicking`, `Sneak`, `Alchemy`, `Speech`, `Alteration`, `Conjuration`,
`Destruction`, `Illusion`, `Restoration`, `Enchanting`. A class only drives an NPC's actual
attribute/skill values when that npc has **`level` > 0 and `autoCalcStats: true`** — otherwise the
engine uses flat defaults (a bare NPC reads 50/50/50 regardless of class). To see it: spawn a
magicka-heavy and a health-heavy NPC (both `autoCalcStats` at the same level) and compare
`getav magicka`/`getav health`.

### dialogue
A `dialogue` entry is a player topic shown under a quest's branch, optionally limited
to one speaker NPC (a `GetIsID` condition). `questEditorId` must name a quest in this
spec; `speakerNpcEditorId`, if set, must name an npc. `prompt` is the player's line;
`responses` are the NPC's spoken lines.

From one `dialogue` entry the build emits the **whole vanilla chain** so the topic
actually surfaces in-game (confirmed It.23, SSE 1.6.1170):
- the **Topic** (`Custom`, `SNAM='CUST'` — a null subtype crashes on load) + **Branch**
  (`TopLevel`, Player) + an **INFO** carrying the responses. Each INFO gets `ENAM`
  (flags) + `CNAM` (favor level) — **an INFO without `ENAM` is treated as invalid and
  its topic is silently dropped from the menu**;
- a **DialogView (DLVW)** per quest tying its branches to the quest (without it the
  quest's player dialogue is never served);
- a **Hello** info (`Misc`/`Hello`/`SNAM='HELO'`) per speaking NPC so the NPC is
  *conversable* at all — set the line with `npc.greeting`.

**Result fragment (do something when the line is picked).** A dialogue choice can only
*act* (take gold, join the follower system, set a stage) through a Papyrus fragment — JSON
holds static data, never control flow. Set `resultScript` (the fragment's Scriptname, which
must `Extends TopicInfo` and define `Function Fragment_0(ObjectReference akSpeakerRef)`),
`resultScriptSource` (the `.psc`, compiled by `package`), and `resultProperties` (bind its
`Auto` properties — same shape as a `scripts[]` entry's properties: `int`/`float`/`bool`/
`string`/`object`). The build attaches the INFO's `OnBegin` fragment VMAD (fires when the player
selects the line; use `OnEnd` only for effects that must follow the full voiced response). Set `goodbye: true`
to close the menu after the line (vanilla recruit/dismiss lines all do). See
`examples/follower_paid_spec.json` + `MFHirePaidRecruit.psc` for a paid-follower recruit.

> **Three runtime requirements (not record bugs):** (1) the dialogue only registers on a
> **game LOAD** — test with a genuine new game, or `save`+`load` after the quest starts;
> a main-menu `coc` or mid-session `startquest` leaves the NPC mute even with a perfect
> plugin. (2) Place the speaker at a real in-room coordinate — a no-package NPC at cell
> origin **(0,0,0)** lands off-navmesh and can't be reached. (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** (or bundle silent `.fuz`) and enable subtitles. See `lifelike/gotchas.md`.

### banter — proactive (unprompted) NPC lines
A `banter` entry is a line the NPC says **on its own**, with no player menu — the vanilla
follower-comment pattern (`HirelingIdles`). Shape: `editorId` (optional), `questEditorId`,
`speakerNpcEditorId`, `responses` (the spoken line(s) — one comment), `emotion`/`emotionValue`,
`conditions` (situational gates). All banter entries sharing a (speaker, quest) collapse into
**one ambient topic** — Category=Misc, SNAM=`IDLE`, no branch — with one **Random**-flagged INFO
per entry; the engine random-picks one whose `conditions` currently pass and plays it. **Trigger
requirement:** the speaker must have **idle chatter enabled** — an AI package carrying the
`AllowIdleChatter` interrupt flag (a `Sandbox` package, or the vanilla follow package). Make it
situational with `conditions` (e.g. `GetCurrentTime` for night, `IsInInterior`, `GetActorValuePercent`
for "I'm hurt", and `GetInFaction CurrentFollowerFaction==1` for follower-only). This is the
*unprompted* counterpart to a `dialogue` line the player asks for. NOTE: ambient/idle only — true
combat shouts use a different subtype (Taunt/Attack), not yet supported. See `examples/follower_vanilla_spec.json`.

### scenes — two NPCs talking to EACH OTHER (SCEN)
A `scene` is a scripted conversation between NPCs (not the player) — the vanilla **Scene** record.
A scene is **hosted by a quest**, its participants are that quest's **aliases** (not direct NPC refs),
and it plays an ordered list of **phases**, one spoken line per phase.
```jsonc
{ "editorId": "MF_TavernArgument",
  "questEditorId": "MF_SceneQuest",     // a StartGameEnabled quest in this spec (the scene runs while it does)
  "beginOnQuestStart": true,            // play the moment the host quest starts (= on game load); default true
  "stopQuestOnEnd": false,              // stop the host quest when the scene finishes (vanilla one-shots set true)
  "actors": [                            // each actor = an alias INDEX + the NPC that fills it
    { "aliasId": 0, "npc": "MF_Borin", "name": "Borin" },
    { "aliasId": 1, "npc": "MF_Hilda", "name": "Hilda" } ],
  "phases": [                            // played in order; `speaker` is one of the actors' aliasId
    { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
    { "speaker": 1, "emotion": "Disgust", "lines": [ "Owe you? That swill wasn't worth a clipped septim." ] },
    { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
    { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] }
```
From this one entry the build emits the **whole vanilla chain** (mirrors `scenediag` on
`dunIronbindBeemJaMourningScene`):
- one **QuestAlias** per actor on the host quest, each `UniqueActor`-bound to the named NPC (so the
  alias fills with that specific actor);
- the **Scene (SCEN)**: its `SceneActors` reference the **alias indices** (not NPC FormKeys); its
  `Phases` are the ordered beats; one **Dialog `SceneAction`** per phase ties (speaking alias, phase)
  → the line's topic, with the *other* actor as the headtrack target so they face each other;
- one **Scene-subtype DialogTopic** (Category=Scene, SNAM=`SCEN`) + **INFO** per phase, carrying the
  spoken `lines` + `emotion`.

> **Runtime requirements (not record bugs):** (1) the two NPCs must be **placed near each other** —
> add a `placements[]` entry per NPC into the **same cell** (they have to be co-located to converse).
> (2) Like all quest dialogue, a scene only loads on a **game LOAD** — test a new game, or `save`+`load`
> after the host quest starts (the build auto-writes the `.seq` entry). (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** and enable subtitles. **Status: structural only** — `build`/`validate`/`dump`
> verified against the vanilla scene shape; **not yet in-game confirmed.** See `examples/scene_spec.json`
> and `lifelike/cookbook-advanced.md`.

### conditions — CTDA gates (on a `dialogue` INFO, a `banter` INFO, or a `package`)
A condition is **static gate data**, so it lives in the spec (logic still belongs in Papyrus). Both
`dialogue[].conditions` and `packages[].conditions` take the same shape:
```jsonc
{ "function": "GetItemCount",          // form-arg: HasPerk | GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99) | TemperIsEnchanted (recipe temper guard)
  "comparison": ">=",                  // == != > >= < <=
  "value": 500,
  "param": "Skyrim.esm:0x00000F",      // the function's form arg (faction/item/global/quest/npc) as a ref
  "actorValue": "",                    // for GetActorValue/GetActorValuePercent instead of param — e.g. "Health", "WaitingForPlayer"
  "runOn": "Reference",                // whose value: Subject (default) | Reference | Target | CombatTarget | ...
  "reference": "Skyrim.esm:0x000014",  // the ref read when runOn=Reference (here, the player)
  "or": false }                        // OR with the NEXT condition (default AND)
```
A `dialogue` INFO already carries an auto `GetIsID` speaker gate; these are appended. Typical follower
uses: hide a paid recruit line unless `GetItemCount Gold >= 500` (on the player) **and**
`GetInFaction CurrentFollowerFaction == 0`; gate a Follow package on `GetInFaction
CurrentFollowerFaction == 1` so it only runs after recruitment. See `examples/follower_paid_spec.json`.

### Quest stages, log entries & objective wiring
A quest's `stages[]` are integer milestones the quest can be **set to** (10, 20, 30…). Each stage
optionally writes a **journal log entry** and can carry a quest-state flag. Objectives display and
complete as stages are set; a `dialogue` line can advance a stage when picked.

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
  milestone), `completeQuest` / `failQuest` (set the QuestLogEntry flag that closes / fails the quest
  when this stage is reached — at most one), `conditions` (optional CTDA gate on the log entry, built
  with the shared **ConditionSpec**: `function` (a `GetStage`/`GetIsID`/… name), `comparison`
  (`==`/`>=`/… or `EqualTo`/`GreaterThanOrEqualTo`/…, default `>=`), `value`, `param` (ref → the
  function's form parameter, e.g. the quest for `GetStage`)).
- **`stages[].startUpStage`** — marks the stage the engine **auto-runs `SetStage` to the instant the
  quest starts** (vanilla QSDT "Start Up Stage" flag). This is how a **Story-Manager-triggered** quest
  shows its opening log entry / displays its first objective with **no external `SetStage`** — without
  it an SM-started quest sits silently at stage 0. At most one per quest. (A `dialogue`/`startGameEnabled`
  quest usually doesn't need one; SM quests do.) **IN-GAME CONFIRMED 2026-06-05.**
- **`objectives[].showStage` / `.completeStage`** — link an objective to stages: it's
  `SetObjectiveDisplayed` at `showStage` and `SetObjectiveCompleted` at `completeStage`. `-1` (the
  default) means "not stage-linked".
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

**Extra alias options:**

| Field | Default | Notes |
|---|---|---|
| `allowReserved` | `false` | Set `true` if the target NPC may be reserved by another quest (`ReservesLocationOrReference`). Without this, the alias fails to fill and the quest doesn't start. `uniqueActor` forces it on. |
| `script` / `scriptSource` / `scriptProperties` | — | Attach a Papyrus **alias script** to this alias (a `ReferenceAlias`-extending script stored on the quest's `QuestAdapter.Aliases` VMAD, bound to the alias ID). It travels with **whatever ref fills the alias** — including a `createObject`-spawned or `findMatching`-matched ref that no base-object script could reach. Classic use is `Event OnActivate(ObjectReference akActionRef)`: activating the aliased ref runs it (e.g. call `MFStoryEventDispatch.Fire(...)` to chain a story event). `script` = the Scriptname, `scriptSource` = the `.psc` for `package` to compile, `scriptProperties` bind its Auto properties (same shape as a dialogue `resultProperties`). You supply the compiled `.pex`. In-game confirmed (2026-06-05); reusable helper `examples/MFSE_AliasActivate.psc`. |

**Aliases on an ordinary quest (no `storyEvent`):** the same `aliases[]` block works on a normal
**StartGameEnabled** quest — `forced` / `uniqueActor` / `createObject` / `findMatching` fills and an
alias `script` all apply (only `fromEvent` is invalid, since there's no event to pull a ref from — the
validator flags it). The aliases fill when the quest starts (= game load for a StartGameEnabled quest).
This lets a plain always-running quest force an NPC/ref into an alias, spawn an object at it, and carry
an `OnActivate` alias script — with no Story Manager event. In-game confirmed (2026-06-05); demo
`examples/quest-alias-standalone.json` (forced player → `createObject` chest at the player → open it →
alias `OnActivate` advances + closes the quest).

#### SM iron laws (engine behaviour, not bugs)

- **One event → one quest starts** — the engine tries quest nodes in order and starts the
  first one whose conditions pass. A second unconditional quest on the same event never starts
  in the same event firing. Use conditions to differentiate.
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
