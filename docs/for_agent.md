# FOR_AGENT — operating ModForge as an AI agent

You (an AI agent) drive ModForge to turn a content request into a Skyrim plugin, and to translate
plugin text. ModForge is the deterministic half; **you are the NL→spec half.** You never hand-write
plugin bytes or FormIDs — you emit a **spec** and the tool emits a valid `.esp`/`.esl`.

## Two ways to drive ModForge

| Path | Use when | Guide |
|---|---|---|
| **CLI + JSON** (default) | describe a mod → write a JSON spec → run the CLI. Reviewable, diffable, no compile step. | **[for_agent_cli.md](for_agent_cli.md)** |
| **Library** (`ModForge.Core`) | the spec must be *computed* — loops/conditionals, data pulled from elsewhere, embedding generation in a bigger program, or reacting to build warnings in code. | **[for_agent_lib.md](for_agent_lib.md)** |

Default to CLI + JSON; reach for the library only when the spec must be computed, not authored.
Both paths produce the same plugins and share the same field reference and limits below.

- **Spec field reference (both paths):** [SPEC-index.md](spec/SPEC-index.md) · complete example: `../examples/sample_spec.json`
- **Making NPCs feel alive** (sandbox / daily life / combat / spell use): start with
  [lifelike/](lifelike/README.md) — recipe + two-systems insight + vanilla FormID reference + gotchas.
- **Vanilla/DLC FormID lookup cache:** start with
  [`../reference/INDEX-skyrim-masters-local.md`](reference/INDEX-skyrim-masters-local.md) before
  crawling the repo. It maps the local `Skyrim.esm`/DLC extraction outputs, cached `find/*.txt`
  probes, `run-status.tsv`, `skyrim-smtree.txt`, and exact CLI commands for new lookups.
- **The engine mechanics behind the generator:** [engine-internals.md](engine-internals.md).
- **Bringing your OWN meshes / textures / sounds / animations** (custom-content mods): the
  external-resource contract — what ModForge references + bundles vs what you author elsewhere —
  is **[external_assets.md](external_assets.md)**.
- **Voice generation for dialogue:** start with [SPEC-workflow.md § Voice](spec/SPEC-workflow.md#voice-tts-voice-cloning--fuz)
  and [CODE_MAP.infra.md § 語音克隆](../workflows/common/code-map/CODE_MAP.infra.md#語音克隆tts--fuz). Always run
  `voicediag` or `voicelines --plan` before spending time on TTS.

## Limits — be honest, do not over-claim

ModForge writes **structurally valid** records. That is NOT the same as **in-game functional**:

- **NPCs can now be functional actors** — set `race` + `class` (+ `outfit`) via vanilla refs
  and the NPC behaves like a real actor. Give an NPC carried gear with `items` (a list of
  `{ item: <ref>, count: N }` — vanilla or in-spec weapon/armor/potion/gold); a weapon they carry is
  auto-equipped (so it's how you arm an NPC — give it real `damage`, or template a vanilla weapon and
  the clone's damage carries through, else a 0-damage weapon is rated below fists and never drawn), and
  the items drop as loot **on death**. Note: an `essential` NPC never dies, so their gear can't be
  looted (and a *living* NPC's worn/equipped items only show via pickpocket, needing perks to take) —
  use `protected` if you want the player to be able to kill and loot them.
- **Placement works for interior cells AND the open world (exterior):** `placements` put an
  NPC/object into (a) a new in-spec interior cell (`cell` = its editorId; reach with
  `coc <editorId>`), (b) a **vanilla interior cell** (`cell` = `"Skyrim.esm:0xFORMID"`, e.g.
  `0x01605E` = Bannered Mare — `find <Skyrim.esm> <name> Cell`), or (c) the **exterior/open
  world** (`worldspace` = `"Skyrim.esm:0x00003C"` = Tamriel — `find <Skyrim.esm> <name>
  Worldspace`; `position` is then WORLD coords, and the exterior cell at floor(x/4096),
  floor(y/4096) is found + overridden). All vanilla placement overrides the cell/worldspace to
  *add* your ref (vanilla contents untouched) and reads the game `Data` folder — set
  `MODFORGE_SKYRIM_DATA` if not at the Steam default. (An ungenerated exterior grid gets a brand-
  new cell — structural only, not in-game verified; placing near existing locations is the safe path.)
- **Items/spells now carry gameplay stats:** weapons take `damage`/`speed`/`reach`, armor
  takes `armorType` + biped `slots`, **spells/potions take `effects`** (a MagicEffect *ref* +
  magnitude/area/duration), and spells take `spellType`/`castType`/`targetType`/`baseCost`. A
  potion with one effect is fully functional; a spell wants an effect + the cast fields. The
  `effects[].magicEffect` *ref* can be a vanilla MGEF **or** an in-spec `magicEffects` entry — author
  a custom MGEF (`archetype`/`actorValue`/`magicSkill`/`resistValue`/`flags`/…) for a bespoke effect.
- **Leveled lists + containers:** `leveledItems`/`leveledNpcs` (weighted level-gated entries,
  each a *ref*) and `containers` (item *refs* + counts) — loot tables, merchant chests, etc.
- **Crafting:** `recipes` (COBJ) make an item (`createdObject` *ref*) craftable at a `workbench`
  keyword (defaults to the forge) by consuming `components` (item *refs* + counts).
- **Classes:** `classes` (CLAS) define an npc "profession" — `healthWeight`/`magickaWeight`/
  `staminaWeight` + `skillWeights` (Skill→0-255) + `teaches`; an npc's `class` ref can point at one.
- **CombatStyles (CSTY) + NPC.spells:** `combatStyles[]` define HOW an NPC fights — the six
  `equipMult*` fields are the AI's per-weapon-class preference scores (push `equipMultMagic` high
  for a mage NPC; vanilla csVampireMagic uses 8.1). An npc's `combatStyle` ref points at one.
  Combined with `npcs[].spells` (array of SPEL refs, populates the AI's spell list) the engine
  picks one of the listed spells to cast based on the CombatStyle preferences. Use
  `cstydiag <Skyrim.esm> <0xFORMID>` to inspect any vanilla CSTY's numeric values.
- **AI Packages (PACK):** `packages` give NPCs decision-layer behaviour ("sandbox at a spot",
  "travel to the inn", etc.). Skyrim PACKs use a vanilla **procedure template** (`template` *ref*,
  e.g. `Skyrim.esm:0x01C254` = Sandbox) that defines the data-input schema; the package fills those
  inputs. Supported templates: Sandbox / Travel / UseMagic / Patrol / Follow / Escort (see
  [lifelike/formid-reference.md](lifelike/formid-reference.md)). The `interruptFlags` array
  (`HellosToPlayer`, `AllowIdleChatter`, `WorldInteractions`, …) is what separates a silent statue
  from a lifelike NPC. Assign packages to an actor via `npcs[].packages`. Use
  `packagediag <Skyrim.esm> <0xFORMID>` to dump a template's slot schema or to inspect any package.
- **More record types** (same spec→build→dump pattern): `ingredients` (alchemy, take `effects`),
  `ammunitions` (`damage`), `scrolls` (`effects` + cast fields), `soulGems` (`maximumCapacity`),
  `keys`, `keywords` (define your own → reference it from any record's `keywords`), `outfits`
  (item *refs*; an npc `outfit` can point at an in-spec outfit), and `statics`/`activators`
  (a `model` .nif path — reference a vanilla mesh — as placement bases).
- **External/vanilla forms CAN be referenced** (race/class/outfit/keywords/factions/
  magicEffect/placement base+cell+worldspace/leveled+container entries, via `"<master>:0xFORMID"`).
- **Dialogue** records are valid, but a line actually appearing in conversation can need
  quest-flag/branch tuning. Voice is optional post-build asset generation: set `voiceType` and
  `voiceTemplate`, build the plugin, run `voicediag`, then `voicelines`. The resulting files live in
  `Sound/Voice/<plugin>/<voiceType>/`; they are not embedded in the plugin. Without generated voice
  files, assume subtitle-only/Fuz Ro D-oh behavior.
- **Story Manager (SM) event-driven quests:** add `storyEvent` + `aliases` to a quest and the
  build wires SMBN→SMQN automatically under the vanilla event root. Supported events:
  `KillActor`, `ChangeLocation`, `CastMagic`, `AddItem`, `Assault`, `CraftItem`,
  `PlayerRemoveItem`, `Arrest`, `IncreaseLevel`, `ScriptEvent`. Alias fills:
  `fromEvent:<slot>`, `uniqueActor:<ref>`, `forced:<ref>`, `createObject:<ref>@<targetAlias>`
  (spawn a new ref to `<ref>` at another alias — in-game confirmed 2026-06-05),
  `findMatching:closest`|`findMatching:any` (fill with the nearest/first existing ref in the loaded
  area matching the alias's `conditions`, e.g. nearest NPC). An alias can also carry a Papyrus
  **alias script** (`script`/`scriptSource`/`scriptProperties`, extends `ReferenceAlias`) that
  travels with whatever ref fills it — e.g. `OnActivate` on a `createObject`-spawned ref to chain a
  story event (in-game confirmed 2026-06-05). **In-game confirmed (2026-06-04)**
  on all variant patterns including ESL plugins. See SPEC-dialogue-quests.md → "Story Manager
  quests".
- **Journal progression on an SM quest:** mark a stage `startUpStage:true` and the engine auto-runs
  it the instant the quest starts, so an SM-triggered quest shows its opening log entry / displays its
  first objective with no external `SetStage`. Complete/close it later from a player action with an
  alias `script` whose `OnActivate` calls `GetOwningQuest().SetStage(N)` (reusable
  `examples/MFSE_AdvanceStage.psc`). Full arc demo: `examples/story-manager-queststage.json` (cast →
  objective shows + chest spawns → open chest → objective completes + quest closes). In-game confirmed
  2026-06-05.
- **Aliases on an ordinary quest (no `storyEvent`):** the `aliases[]` block also works on a normal
  StartGameEnabled quest — `forced`/`uniqueActor`/`createObject`/`findMatching` fills + an alias
  `script` all apply (only `fromEvent` is invalid without an event). They fill on quest start (= game
  load). So a plain quest can force an NPC/ref into an alias, spawn an object at it, and carry an
  `OnActivate` alias script with no SM event. Demo `examples/quest-alias-standalone.json`. In-game
  confirmed 2026-06-05.
- **Script Event (custom story trigger):** a `ScriptEvent` quest declares a `keyword`; a
  Papyrus caller fires `MFStoryEventDispatch.Fire(kw, ref1, ref2, loc)` and the engine routes
  it to matching SM quests. The dispatcher `.pex` is embedded in the CLI and copied into
  `Scripts/` by `package` automatically — no per-mod Papyrus compile needed. **In-game
  confirmed (2026-06-04).**
- **Reusable trigger library** — the one-line `Fire()` call wired to four real in-game entry
  points, all in-game confirmed (2026-06-05): magic effect (cast a spell), potion (drink),
  activator (`OnActivate`, pull a lever — model must be a real vanilla NIF), dialogue line
  (`TopicInfo` `Fragment_0`, NPC gives a quest), and an **alias script** (`ReferenceAlias`
  `OnActivate`, activate a ref a quest alias holds — including one spawned at runtime by
  `createObject`). Examples: `story-manager-{magic,potion,activator,dialogue,alias}trigger.json`.
- You cannot confirm anything works **in-game** from here — that needs a Proton/Skyrim launch.
  Say "generated and structurally verified (dump)", not "works in-game", unless a human tested it.

When a request needs something in the Limits list, say so plainly and offer what IS possible.
