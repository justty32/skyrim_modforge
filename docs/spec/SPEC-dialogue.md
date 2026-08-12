# ModForge spec — classes, dialogue, banter & scenes

← [index](SPEC-index.md) · quests & Story Manager → [SPEC-quests](SPEC-quests.md) · identity system → [SPEC-identities](SPEC-identities.md)

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

**INFO (ENAM) behaviour flags** (all default false): `sayOnce` (speak at most once ever — VIGILANT's
most-used INFO flag, for one-shot story beats), `walkAway` (NPC walks off after the line), `random`
(engine random-picks among sibling INFOs whose conditions pass, for line variety), `invisibleContinue`
(continue to the next INFO in the chain without closing the menu), `forceSubtitle` (always show the
subtitle even when subtitles are off). They apply to both player topics and `hello` greetings.

**Dialogue trees (branching conversations).** By default every `dialogue` entry is a **top-level**
player option, all shown the moment you talk to the NPC. To build a *tree* — pick a topic, the NPC
answers, then *new* options appear — use:
- **`linkTo`** (ENAM, a list): after this line plays, surface these dialogue topics as the next
  choices. Each entry is another `dialogue`'s `editorId` (resolved to its TOPIC) or a vanilla
  `<master>:0xFORMID` topic. VIGILANT's #1 tree technique.
- **`topLevel: false`** on the *target* entries: marks them as **sub-topics** that only appear when
  something `linkTo`s them (otherwise they'd also show in the initial menu). Default `true`.
- **`previousDialog`** (PNAM): chain this INFO after another (its value is a `dialogue` `editorId`,
  resolved to that INFO) — for ordering responses within a flow.

```jsonc
{ "editorId": "AskAboutCave", "questEditorId": "Q", "speakerNpcEditorId": "Hideko",
  "prompt": "What's in the cave?", "responses": ["Bandits. And worse."],
  "linkTo": ["AskHowMany", "AskReward"] },            // → two follow-up options appear
{ "editorId": "AskHowMany", "questEditorId": "Q", "speakerNpcEditorId": "Hideko",
  "prompt": "How many bandits?", "responses": ["A dozen, maybe."],
  "topLevel": false }                                  // a sub-topic — only via the linkTo above
```

> **Three runtime requirements (not record bugs):** (1) the dialogue only registers on a
> **game LOAD** — test with a genuine new game, or `save`+`load` after the quest starts;
> a main-menu `coc` or mid-session `startquest` leaves the NPC mute even with a perfect
> plugin. (2) Place the speaker at a real in-room coordinate — a no-package NPC at cell
> origin **(0,0,0)** lands off-navmesh and can't be reached. (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** (or bundle silent `.fuz`) and enable subtitles. See `lifelike/gotchas.md`.

**INFO array batch (`variants`).** To generate **many sibling lines under one topic** — ambient
commentary that reacts to travel / location / time / weather / player state — declare them all in **one**
`dialogue` entry's `variants` array instead of repeating the topic, speaker, and gate for each. Each
`variants[]` entry becomes its own INFO with the **`random`** flag (the engine random-picks among the
siblings whose conditions currently pass), and **shares** the parent entry's speaker gate, `conditions`,
`useConditionTemplates`, and `identity` — plus its own extra `conditions` and `responses`. This is the
generator for FCO-style 265-line commentary on one shared gate. A `variants[]` entry is
`{ responses, conditions?, emotion?, emotionValue?, sayOnce? }` (`emotion`/`emotionValue` inherit the
parent when unset). When `variants` is set and the parent `responses` is **empty**, no parent INFO is
emitted (the entry is a pure batch header); a non-empty parent `responses` plays as one more sibling.
Variants are line-variety only — result fragments / `setStage` / `linkTo` stay on the parent entry, and
`variants` is not supported on a `hello` line. Pair with `conditionTemplates` to share the gate set across
*several* batches.

```jsonc
{ "editorId": "LydiaTravelBanter", "questEditorId": "Q", "speakerNpcEditorId": "Lydia",
  "prompt": "", "useConditionTemplates": ["Following"],     // shared gate: only while following
  "variants": [
    { "responses": ["Lovely day for it."], "conditions": [{ "function": "GetCurrentTime", "comparison": "<", "value": 18 }] },
    { "responses": ["Getting dark. We should make camp."], "conditions": [{ "function": "GetCurrentTime", "comparison": ">=", "value": 18 }] },
    { "responses": ["I used to dream of adventure. Be careful what you wish for."], "sayOnce": true }
  ] }
```

**Acting on a pick — `persist` / `syncPerks` / `storageWrites`.** A dialogue line can bank state when
picked: `persist`/`syncPerks` (JContainers JFormDB nested per-Form state, Idea #20) and `storageWrites`
(PapyrusUtil StorageUtil flat per-Form KV — follower memory, cooldowns, flags). Both emit into the line's
TIF result fragment. Same shapes as on a quest stage — see [persist/syncPerks](SPEC-quests.md#persist--syncperks--jcontainers-jformdb-per-form-state-idea-20-skill-tree-phase-0)
and [storageWrites](SPEC-quests.md#storagewrites--papyrusutil-storageutil-per-form-kv-j-group) in SPEC-quests
(on a dialogue line `target`/`key` may be `"speaker"`).

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
  - **per-phase gaze override** (optional): a phase may set `headtrackActor` (an actor `aliasId` to look
    at; `-1` = look at no one; default = the other actor), `headtrackPlayer: true` (turn to face the
    PLAYER — mutually exclusive with a non-default `headtrackActor`), and `faceTarget` (default true).
    Use it for a beat where an NPC turns to address you directly. Omit all three = unchanged behaviour.
  - **conditions** (optional CTDA gates, shared `ConditionSpec`, wired in pass 2): scene-level
    `conditions` (the whole scene only STARTS if all pass) and per-phase `startConditions` (the phase
    only plays if all pass) / `completionConditions` (the phase ends once all pass). A scene with no
    conditions is byte-identical to before. See `examples/scene-conditions.json`.
    - **IMPORTANT — scene-level `conditions` only gate ENGINE-started scenes** (`beginOnQuestStart`, or
      a non-forced engine start). An **`autoStart` (presence-gated) scene is force-started by the
      controller script (`Scene.Start()`), which BYPASSES scene begin-conditions** — so scene-level
      `conditions` do NOTHING on an autoStart scene. To gate a presence-triggered scene, use
      `autoStart.gateGlobal` (a GLOB the controller checks before starting) instead. Per-phase
      `startConditions`/`completionConditions` ARE still evaluated during playback either way.
    - **`completionConditions` advance a phase.** The standard "advance once the spoken line finishes"
      gate is **`IsSceneActionComplete`** — on a scene condition the `scene` defaults to the owning scene,
      so you only give `sceneActionIndex` (the action's index in the built SCEN; find it with `scenediag`).
      You can also gate on player position to pace a beat — `GetDistance` (≤ N units), `GetInCell`,
      `GetInCurrentLoc`, `GetInWorldspace`. (⚠️ phase-advance behaviour is **offline-built but not yet
      in-game-verified** — pending in-game tests are tracked in the repo-root `WAIT_USER.md`.)
- one **Scene-subtype DialogTopic** (Category=Scene, SNAM=`SCEN`) + **INFO** per phase, carrying the
  spoken `lines` + `emotion`.

> **Runtime requirements (not record bugs):** (1) the two NPCs must be **placed near each other** —
> add a `placements[]` entry per NPC into the **same cell** (they have to be co-located to converse).
> (2) Like all quest dialogue, a scene only loads on a **game LOAD** — test a new game, or `save`+`load`
> after the host quest starts (the build auto-writes the `.seq` entry). (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** and enable subtitles. **Status: structural only** — `build`/`validate`/`dump`
> verified against the vanilla scene shape; **not yet in-game confirmed.** See `examples/scene_spec.json`
> and `lifelike/cookbook-advanced.md`.

#### autoStart — presence-gated repeating Scene (隨從在場偵測 + 互動 Scene)
Instead of playing once on game-load (`beginOnQuestStart`), a scene can play itself **whenever the
player is co-present with both actors**, re-firing on a cooldown — the usable form of "follower
banter" (followers stay near the player, so it fires while travelling). Add an `autoStart` block:
```jsonc
{ "editorId": "MF_TravelBanter", "questEditorId": "MF_BanterQuest",   // host quest MUST be StartGameEnabled
  "autoStart": {
    "triggerDistance": 1024.0,        // max distance (units) from the player to EACH actor; default 2048
    "requireLineOfSight": false,      // also require the player HasLOS both actors; default false
    "cooldownSeconds": 15.0,          // min REAL seconds between plays (timescale-independent); default 60
    "pollSeconds": 4.0,               // RegisterForSingleUpdate poll interval; default 5
    "brawlOnEnd": true },             // when the dialogue finishes, the two actors fight each other; default false
  "actors": [ /* ≥2, UniqueActor-bound as above */ ],
  "phases": [ /* … */ ] }
```
When `autoStart` is present the build **clears** the scene's `beginOnQuestStart` and attaches the
reusable **`MFSceneBanterController`** (extends Quest) to the host quest, wiring it to this scene + the
first two actor alias indices + the tuning. The controller polls (chained `RegisterForSingleUpdate`)
and calls `Scene.Start()` when both actors are loaded, within range, not dead/in-combat, (optional LOS),
and the cooldown elapsed. With **`brawlOnEnd`** it detects the scene finishing and makes the two actors
fight (`StartCombat` both ways) — they come to blows after the argument; mark the actors **`essential`**
(NpcSpec flag) for a non-lethal brawl. `package` ships `MFSceneBanterController.pex` into `Scripts/`
automatically. See `examples/scene-presence-banter.json`. **Out of scope (later):** dynamic "scan
current teammates" fill (this slice uses named, `UniqueActor`-bound actors).

##### Replay policy — controlling when/how often it re-fires
By default the presence gate re-fires every time the player is co-present and `cooldownSeconds` has
elapsed (an endless loop). Add any of these to `autoStart` to control replay (all **AND**-ed onto the
cooldown):
```jsonc
"autoStart": {
  "triggerDistance": 1024.0, "cooldownSeconds": 15.0, "pollSeconds": 4.0,
  "playOnce": true,                 // play AT MOST ONCE ever; the controller stops polling afterwards
  "playHour": 12.0,                 // only fire within +/- playHourTolerance of this in-game hour (0..24,
  "playHourTolerance": 2.0,         //   circular); -1 (default) = any time. e.g. 12 +/- 2 = 10:00..14:00
  "gateGlobal": "MF_BanterDone"     // a ref → a GLOB used as a re-arm TOKEN (see globals)
}
```
- **`playOnce`** — the simplest "don't loop": after the single play the controller unregisters its poll
  (save-bloat hygiene). Best for a one-shot encounter.
- **`playHour` / `playHourTolerance`** — a time-of-day window (the controller reads the in-game hour).
  Independent of the real-time cooldown — use for "only at noon", "only at night", etc.
- **`gateGlobal`** — the general mechanism: the scene plays only while the global `== 0`, and the
  controller `SetValue(1)`s it immediately after. It then stays off until some **OTHER** generated
  content `SetValue(0)`s it (a dialogue result script, a quest stage fragment, an alias script, another
  event). This is "play once **until something re-arms it**". Build the GLOB in
  [`globals`](SPEC-items.md#globals-glob--shared-flags--counters--constants); resetting it to 0 is
  authored separately (Papyrus). See `examples/scene-replay-policy.json`.

> Changing `MFSceneBanterController.psc` requires recompiling its `.pex` (native
> `~/tools/papyrus-compiler` with `MODFORGE_PAPYRUS_HEADERS` pointing at the source cache, or Wine+CK).

#### actions — non-dialog performance beats (NPC 劇情演出)
A scene can do more than talk. Add an `actions[]` list and the scene becomes a little performance —
*walk to a spot → wait → talk*. Each action is a vanilla non-Dialog `SceneAction` (decoded from
`dunTolvaldsCaveCrownScene` / the `BardSongs*` scenes via `scnscan`) that runs over a **window of
phase indices**. A phase referenced only by an action may have **empty `lines`** — a pure *beat phase*.
```jsonc
{ "editorId": "MF_AltarRite", "questEditorId": "MF_RiteQuest",
  "actors": [ {"aliasId":0,"npc":"MF_Priest"}, {"aliasId":1,"npc":"MF_Acolyte"} ],
  "phases": [
    {},                                              // phase 0: a BEAT (no lines) — window for the walk
    {"speaker":0, "lines":["Approach the altar."]},  // phase 1: spoken
    {"speaker":1, "lines":["As you say."]} ],
  "actions": [
    {"actor":0, "package":"MF_WalkToAltar", "startPhase":0, "endPhase":0},  // PACKAGE: actor runs a PACK
    {"actor":0, "timerSeconds":2.0,         "startPhase":0, "endPhase":0} ] // TIMER: pace the beat 2s
}
```
Each action sets one primary kind (fragment-backed actions may also set `timerSeconds`):
- **`idle`** — a ref to an `IDLE` (IdleAnimation) record (`<master>:0xFORMID`; discover with
  `find <master> <keyword> idle`). The actor **plays that idle animation** when `startPhase` begins
  (kneel / pray / gesture…), then returns to AI naturally. The animation runs via a `SceneAdapter`
  per-phase **OnStart fragment** on the SCEN — an `SF_<scene>.Fragment_<phase>` that calls
  `<alias>.GetActorRef().PlayIdle(<idle>)` (decoded from vanilla `SF_BardSongsBallad01Scene`). The
  fragment is compiled + attached by **`package`** (a pure `build`/`validate` attaches no VMAD). Two
  gotchas, both handled for you: (1) the engine only **runs** a phase that has a `SceneAction`, so an
  idle action also emits a **Timer** (every vanilla fragment phase carries one) — that Timer makes the
  phase fire its fragment AND **holds the pose**; set `timerSeconds` to control the hold (default 2s).
  (2) The actor must be **standing** — a seated/sandboxing NPC ignores `PlayIdle`, so give him a
  package that keeps him in place (a Sandbox with `allowSitting:false`), like vanilla's
  package-controlled scene actors. The idle's `<master>` must be a real IDLE — a wrong FormID plays
  nothing (no error), so verify it.
- **`setStage`** — a restricted phase-begin fragment that calls `Quest.SetStage(stage)`. Shape:
  `{"quest":"MF_TargetQuest","stage":40}`; omit `quest` to target the scene's host quest. ModForge
  binds the target as a `Quest` property rather than embedding a FormID in Papyrus. Like `idle`, it
  auto-emits a Timer so the phase actually runs; set `timerSeconds` only when the phase should remain
  open longer. Multiple fragment-backed actions on the same phase share one `Fragment_<phase>()`.
  In-spec targets must be quests and must declare that stage. For an external `<plugin>:0xFORMID`
  quest, record type and stage existence are an author contract because the master may be unavailable
  offline. A failed fragment compile makes `package` fail before writing the ESP, so it cannot silently
  ship a Timer-only scene. This is intentionally limited to `SetStage`; arbitrary Papyrus bodies remain out of scope.
- **`package`** — a ref to an AI package (a `packages[]` entry in this spec, or an external
  `<master>:0xFORMID`). The actor runs that PACK across the phase window. **Movement** = a **Travel**
  package whose destination is a placed marker; **ambient activity** = a **Sandbox** package; etc.
  (anything `packages[]` can build). The build emits a `Type=Package` SceneAction whose `Packages`
  holds the resolved PACK FormKey (resolved in pass 2, like the actor aliases).
- **`timerSeconds`** (> 0) — a `Type=Timer` SceneAction: the scene waits this many seconds over the
  window (vanilla bard scenes pace beats this way). Pair a Timer with a movement Package on the same
  beat phase so the phase reliably advances after the walk (the engine advances when the window's
  actions complete).

**PlayIdle composition** (idle = animation + its own hold Timer; put fragments on phase ≥1, never 0):
```jsonc
"phases": [ {}, {"speaker":0,"lines":["By the Eight, I pledge my blade."]}, {"speaker":0,"lines":["It is done."]} ],
"actions": [
  {"actor":0, "startPhase":0, "timerSeconds":1.5},                                 // a standing beat (no fragment on phase 0)
  {"actor":0, "startPhase":1, "idle":"Skyrim.esm:0x0F11EE", "timerSeconds":4.0},   // IdleBlessingKneelEnter — kneel + pray (hold 4s)
  {"actor":0, "startPhase":2, "idle":"Skyrim.esm:0x0F11EF", "timerSeconds":2.0} ]  // IdleBlessingKneelExit — rise
```

`startPhase`/`endPhase` are indices into `phases[]`; `endPhase` -1 = `startPhase`. Validation: actor
must be a scene actor, the phase window must be in range, a beat (lineless) phase must be covered by an
action. See `examples/scene-action-performance.json` (Borin walks across the Sleeping Giant Inn to the
vanilla `RiverwoodInnCenterMarker`, waits 8s, then the two argue) and `examples/scene-playidle.json`
(a supplicant kneels → murmurs a prayer → rises), plus `examples/scene-setstage.json` (a phase advances
another quest). **Out of scope (later):** arbitrary scene fragments and CAMS camera shots; sit / use-furniture (needs a
UseItemAt PACK template — `MQ306EsbernSit` shape decoded; available as the `sittarget` PACK template)
and idle **event-name** (string) variants rather than IDLE-record refs.

### conditions — CTDA gates (on a `dialogue` INFO, a `banter` INFO, or a `package`)
A condition is **static gate data**, so it lives in the spec (logic still belongs in Papyrus). Both
`dialogue[].conditions` and `packages[].conditions` take the same shape:
```jsonc
{ "function": "GetItemCount",          // form-arg: HasPerk | GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    //   GetQuestCompleted(quest) | GetDistance(ref; value=units) | GetIsCurrentPackage(pack) | GetIsVoiceType(VTYP/list)
  //                                    //   GetQuestRunning(quest) | GetInCell(cell) | GetInWorldspace(wrld) | GetEquipped(item/list) | GetDeadCount(npc base) | GetInCurrentLoc(location)
  //                                    //   GetKeywordDataForCurrentLocation(LocType kw) | LocationHasKeyword(LocType kw) | LocAliasHasKeyword(alias=<locAlias>, param=LocType kw) — location-aware encounters
  //                                    // two-param: GetStageDone(param=quest, stage=N) — 1 if that exact stage was set
  //                                    //   IsSceneActionComplete(scene=<owning by default>, sceneActionIndex=N) — scene phase "advance when action N done"
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // script-property: GetVMQuestVariable(param=quest, variableName=Prop) | GetVMScriptVariable(param=object, variableName=Prop) — read a Papyrus property in a condition
  //                                    // alias-arg: GetIsAliasRef (use "alias", NOT "param" — names an alias on the OWNING quest)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99) | TemperIsEnchanted (recipe temper guard)
  //                                    //   GetSitting (sit-state; ==3 sitting, ==4 sleeping) | GetGold (run-on actor's gold) | GetMapMarkerVisible (runOn=Reference to a map marker)
  "comparison": ">=",                  // == != > >= < <=
  "value": 500,
  "param": "Skyrim.esm:0x00000F",      // the function's form arg (faction/item/global/quest/npc) as a ref
  "actorValue": "",                    // for GetActorValue/GetActorValuePercent instead of param — e.g. "Health", "WaitingForPlayer"
  "alias": "",                         // for GetIsAliasRef instead of param — an alias NAME on the owning quest (resolved to its index)
  "variableName": "",                  // for GetVMQuestVariable/GetVMScriptVariable — the Papyrus property name read off the attached script
  "runOn": "Reference",                // whose value: Subject (default) | Reference | Target | CombatTarget | ...
  "reference": "Skyrim.esm:0x000014",  // the ref read when runOn=Reference (here, the player)
  "or": false }                        // OR with the NEXT condition (default AND)
```
A `dialogue` INFO already carries an auto `GetIsID` speaker gate; these are appended. Typical follower
uses: hide a paid recruit line unless `GetItemCount Gold >= 500` (on the player) **and**
`GetInFaction CurrentFollowerFaction == 0`; gate a Follow package on `GetInFaction
CurrentFollowerFaction == 1` so it only runs after recruitment. See `examples/follower_paid_spec.json`.

**What `param` / `reference` may name.** Both are **arbitrary refs**, and both are resolved *after*
placements and `references[]` exist — so besides a base record (faction/item/global/quest/NPC base) and
a vanilla `<master>:0xFORMID`, either may name a **placed ref**: an **in-spec `placements[]` editorId**
or a **`references[]` label**. That is what makes a world-anchored gate expressible without Papyrus —
`{ "function": "GetDistance", "param": "the chair", "comparison": "<=", "value": 512 }` (the player is
near *that* object), or `{ "function": "GetMapMarkerVisible", "runOn": "Reference", "reference":
"my marker" }`. This holds **everywhere the shared condition shape appears**: `dialogue` (inline +
`conditionTemplates` + variants), `banter`, `packages`, `perks` (perk-level and effect-level),
`quests[].storyEvent.conditions`, quest `aliases[].conditions` (the `findMatching*` match filter),
`scenes[].conditions` and `phases[].startConditions` / `completionConditions`, `stages[].conditions`,
`objectives[].targets[].conditions`, and recipe conditions. A ref that resolves to none of the above
warns and the condition is **dropped** (the gate then tests nothing — treat the warning as an error).

**Shared condition templates (`conditionTemplates` + `dialogue[].useConditionTemplates`)** — when many
INFOs share the same gate set (ambient commentary: a location/state/time block repeated across hundreds
of lines), define it once and reference it by name. A top-level `conditionTemplates: [{ "name": "X",
"conditions": [...] }]` declares a named block; a line's `"useConditionTemplates": ["X", "Y"]` appends
those blocks' conditions to its INFO — **after** the line's inline `conditions`, in listed order, through
the same builder (so `GetIsAliasRef` etc. resolve against the owning quest). Templates nest no further
(a template is a flat condition list). `validate` flags a reference to an undefined template and a
duplicate template name.

**`GetIsAliasRef`** gates on **which quest alias the run-on actor fills** (the most-used VIGILANT
dialogue technique — gate a line by role, e.g. "the Victim alias", not a hardcoded NPC FormID). Give
the alias by **`alias`** (its name on the **owning quest**), not `param`; build resolves it to the
alias index. Valid only where there's an owning quest: `dialogue` / `banter` / `scene` (scene-level &
per-phase) / quest `stages[].conditions` / `objectives[].targets[].conditions`. On a `package` /
`perk` / recipe condition (no owning quest) it's dropped with a warning.

**`GetVMQuestVariable` / `GetVMScriptVariable`** read a **Papyrus script property in a condition** — the
way to gate a line on another mod's runtime state without writing Papyrus. `GetVMQuestVariable` reads a
property off a **quest's** attached script (`param` = the quest); `GetVMScriptVariable` reads one off an
**object's** attached script (`param` = the object/ref). `variableName` is the property name string, and
`value`/`comparison` test it (e.g. Inigo-The-Hunters `PlayerInDialogue` bark-suppression: `{ "function":
"GetVMQuestVariable", "param": "ITH.esp:0x…", "variableName": "PlayerInDialogue", "comparison": "==",
"value": 0 }`). ⚠️ The exact `variableName` string the engine expects (bare property name vs. a backing
`::Prop_var` form) **depends on the target script and needs xEdit/in-game verification** — ModForge emits
whatever string you give verbatim into the CTDA.

