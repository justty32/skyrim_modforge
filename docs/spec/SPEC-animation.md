# ModForge spec — action-system asset/config (OAR / BDI / PIE)

← [index](SPEC-index.md)

Three top-level blocks generate **loose files** (no `.esp` record) for the modern animation/
combat stack. `package` writes them next to the plugin in the MO2-ready folder. The `.hkx`
animations themselves are **user-supplied** — ModForge writes the config tree and places the
clips you provide; it never authors Havok. (Survey + real-file analysis:
`../../analysis/mod-survey/action-system/`.)

> **Out of scope:** building/retargeting `.hkx` (Blender→hkx wall), behavior-graph patching
> (Pandora), SCAR AI annotations. Those are pipelines outside the record/config layer.

---

## `animationReplacers` — OAR (Open Animation Replacer)

A condition-based runtime animation replacement mod. Each entry is one **replacer-mod** (a root
`config.json`) with named **submods** (each its own `config.json` + the clips it ships). Emitted to
`Meshes/actors/character/animations/OpenAnimationReplacer/<mod>/...`.

```jsonc
{
  "animationReplacers": [
    {
      "mod": "Sofia Katana",            // replacer-mod folder + config name
      "author": "ModForge",
      "description": "katana moveset for a follower",
      "submods": [
        {
          "name": "Attack - Sword & Shield",
          "priority": 100008,            // higher wins; required (> 0)
          "replaces": "actors/character/animations/...", // vanilla/MCO anim path being replaced
          "hkx": ["anims/ss_atk.hkx"],   // user-supplied clips (rel. to `assets` or spec dir)
          "npcMoveset": {                // sugar (expands into `conditions`) — see below
            "rightWeapon": "sword", "leftWeapon": "shield", "playerOnly": false
          }
        }
      ]
    }
  ]
}
```

**Submod fields:** `name`, `description`, `priority` (>0), `replaces`, `hkx[]`, legacy
`variants[]` (emitted under `_variants_<anim>/1.hkx,2.hkx…`), `replacementAnimations[]`,
`conditions[]`, `npcMoveset`, `functionsOnActivate[]`, `functionsOnDeactivate[]`,
`functionsOnTrigger[]`, `replaceVanillaPath` (plain tier-a replacer: drop the clip at the vanilla
path, no config/conditions). Existing `variants: ["source.hkx"]` remains valid unchanged.

### `conditions[]` — OAR condition shape

OAR uses its **own** condition names (not Skyrim CTDA function names). Containers `AND`/`OR` nest
children in `conditions[]`. Empty `conditions: []` = applies to everyone. Form refs are
`"Plugin.esp|0xFormID"`.

| `condition` | fields |
|---|---|
| `IsActorBase` | `form` (+ `negated`) — e.g. exclude the player with `negated:true`, `form:"Skyrim.esm|0x000007"` |
| `IsEquippedType` | `type` (weapon enum), `leftHand` |
| `IsRace` | `form` |
| `IsFemale` | (none) |
| `Random` | `randomMin`, `randomMax`, `comparison`, `value` |
| `CompareValues` | `graphVariable`, `graphVariableType` (Int/Float/Bool), `comparison`, `value` |
| `AND` / `OR` | `conditions[]` |
| `PRESET` (2.2.0+) | `preset` — a `conditionPresets[].name` from the replacer-mod root |

**`type` weapon enum:** `fist`=0 `sword`=1 `dagger`=2 `waraxe`=3 `mace`=4 `greatsword`=5
`battleaxe`/`warhammer`=6 `bow`=7 `staff`=8 `crossbow`=9 `shield`=11 `torch`=12.

### `npcMoveset` sugar

The recurring NPC-moveset recipe — expands to one `AND` of `IsEquippedType(right)` +
`IsEquippedType(left)` + (when `playerOnly:false`) `IsActorBase ¬player` + (optional) `IsRace`,
plus a sibling `Random` when `randomPick` is set:

| field | effect |
|---|---|
| `rightWeapon` / `leftWeapon` | weapon-type names → two `IsEquippedType` |
| `playerOnly` | `false` → adds `IsActorBase ¬player(Skyrim.esm|0x7)` (NPC-only moveset) |
| `race` | optional `IsRace "Plugin.esp|0xFormID"` |
| `randomPick` | optional `Random{0..1} < randomPick` (combo variety) |

### OAR 2.2+ root `conditionPresets[]`

Put a reusable condition block on the replacer-mod, then use `PRESET` in any submod (or supported
`CONDITION` function). ModForge emits the OAR author-config key **`conditionPresets`** and each
reference as **`{ "condition":"PRESET", "requiredVersion":"2.2.0", "Preset":"<name>" }`**. Names must be nonempty and unique;
every `PRESET` must refer to a name in the same replacer-mod.

```jsonc
"conditionPresets": [{
  "name": "PlayerOnly",
  "conditions": [{ "condition": "IsActorBase", "form": "Skyrim.esm|0x000007" }]
}],
"submods": [{
  "conditions": [{ "condition": "PRESET", "preset": "PlayerOnly" }]
}]
```

### OAR 2.2+ `replacementAnimations[]` — variant metadata

The original `variants[]` lists source `.hkx` files and still only controls placement. Add an
optional `replacementAnimations[]` item when OAR must persist variant weights, sequential mode,
or play-once flags. It emits a submod `replacementAnimDatas[]` item using OAR's exact keys:
`projectName`, `path`, numeric `variantMode` (random=0, sequential=1), numeric
`variantStateScope` (local=1, submod=2, replacerMod=4, reference=8), and `variants[]` metadata.

```jsonc
"variants": ["anims/idle_a.hkx", "anims/idle_b.hkx"],
"replacementAnimations": [{
  "projectName": "DefaultMale",
  "path": "Data\\Meshes\\actors\\character\\animations\\male\\mt_idle.hkx",
  "variantMode": "random",
  "variantStateScope": "submod",
  "variants": [
    { "filename": "1.hkx", "weight": 2 },
    { "filename": "2.hkx", "weight": 1 }
  ]
}]
```

`filename` must refer to a generated numeric file from that submod's `variants[]`; duplicate names,
non-finite/non-positive weights, and bad filename references fail validation. `path` is intentionally
explicit: OAR matches it to its own discovered runtime replacement path, and ModForge cannot safely
invent a portable value from a build-machine location. Copy it from OAR Author-mode config for the
target behavior project.

### Submod functions and multifunctions (OAR 3.0+)

`functionsOnActivate[]`, `functionsOnDeactivate[]`, and `functionsOnTrigger[]` emit OAR's three
function-set keys. The supported, source-verified minimal built-in surface is `PlaySound` plus the
recursive multifunctions `CONDITION`, `RANDOM`, and `ONE`. `CONDITION` contains `conditions[]` and
`functions[]`; `RANDOM`/`ONE` contain `functions[]`, and `RANDOM.weights[]` is optional but, when
present, has one positive finite value per child. Trigger-set functions additionally require
`triggers[]` (`event`, optional `payload`).

```jsonc
"functionsOnTrigger": [{
  "function": "CONDITION",
  "triggers": [{ "event": "OAR", "payload": "sound1" }],
  "conditions": [{ "condition": "PRESET", "preset": "PlayerOnly" }],
  "functions": [{ "function": "PlaySound", "soundForm": "Skyrim.esm|0x0003C8" }]
}]
```

External-plugin functions and OAR built-ins outside this typed subset are intentionally not accepted:
their argument contracts need a separate verified spec rather than a raw JSON passthrough.

---

## `behaviorData` — BDI (Behavior Data Injector)

Inject graph variables/events into a behavior project **with no behavior patch**. Emitted to
`SKSE/Plugins/BehaviorDataInjector/<file>.json` as a flat array.

```jsonc
{
  "behaviorData": [
    { "file": "Sofia_BDI", "entries": [
      { "projectPath": "Actors", "type": "kInt",   "name": "MF_Combat", "value": 0 },
      { "projectPath": "Actors", "type": "kEvent", "name": "MF_OnVow" }   // kEvent: no value
    ]}
  ]
}
```

`type` ∈ `kInt | kBool | kFloat | kEvent`. Injected variables can be read by OAR `CompareValues`
conditions (the "animation drives state" chain).

---

## `payloadMacros` — PIE (Payload Interpreter) macro table

Named macros → payload commands. Emitted to `SKSE/PayloadInterpreter/Config/<file>.ini`.

```jsonc
{
  "payloadMacros": [
    { "file": "SofiaAxe", "section": "Intensify", "macros": [
      { "name": "enableIframe", "command": "@SETGHOST|1" }   // → "$enableIframe = @SETGHOST|1"
    ]}
  ]
}
```

---

## Player-side prerequisites (ModForge marks, does not generate)

OAR + Address Library + Animation Queue Fix; BDI / PIE DLLs; movesets need **Pandora** (or
Nemesis) run once to build the behavior baseline. The `.hkx` clips are author-supplied; provide
them via `assets` (the bundled source dir) or paths relative to the spec.
