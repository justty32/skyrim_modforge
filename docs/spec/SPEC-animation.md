# ModForge spec — action-system asset/config (OAR / BDI / PIE)

← [index](SPEC-index.md)

Three top-level blocks generate **loose files** (no `.esp` record) for the modern animation/
combat stack. `package` writes them next to the plugin in the MO2-ready folder. The `.hkx`
animations themselves are **user-supplied** — ModForge writes the config tree and places the
clips you provide; it never authors Havok. (Survey + real-file analysis:
`sub_projs/mod-survey/action-system/`.)

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

**Submod fields:** `name`, `description`, `priority` (>0), `replaces`, `hkx[]`, `variants[]`
(emitted under `_variants_<anim>/1.hkx,2.hkx…`), `conditions[]`, `npcMoveset`, `replaceVanillaPath`
(plain tier-a replacer: drop the clip at the vanilla path, no config/conditions).

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
