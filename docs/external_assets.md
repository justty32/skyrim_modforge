# External assets — bringing your own meshes / textures / sounds / animations

← index: [for_agent.md](for_agent.md) · spec fields: [SPEC-index.md](SPEC-index.md) · CLI: [for_agent_cli.md](for_agent_cli.md)

By default ModForge gives a record a 3D appearance by **cloning a vanilla record** (a weapon's
`template` reuses IronSword's `.nif`). The **external-resource pipeline** lets you instead bring your
**own** assets — a custom mesh, textures, a sound, an animation — and have ModForge (1) **wire the
Data-relative path into the record** and (2) **bundle the files** next to the `.esp` so the packaged
mod is self-contained / MO2-ready.

## What ModForge does vs. what you must author

ModForge **references and bundles** assets. It does **NOT** author them. Be honest about the split:

| Asset | ModForge does | You must author (DCC tool / Creation Kit) |
|---|---|---|
| `.nif` mesh | writes the path into the record's `MODL`, copies the file | model the mesh (Blender + Nif tools / 3ds Max), set up collision, materials |
| `.dds` texture | bundles the file (the `.nif` names its textures internally) | paint the texture, generate mipmaps, point the `.nif`'s `BSShaderTextureSet` at it |
| `.wav` / `.xwm` sound | emits a Sound Descriptor (SNDR) pointing at the file, links records to it, bundles the file | record/produce the audio; `.xwm` is the compressed in-game format (xWMAEncode) |
| `.hkx` animation | bundles the file (if placed under a recognised folder) | author the animation + a behaviour graph (CK / havok tools); wiring anims into behaviour is **out of scope** |

ModForge cannot validate asset **content** — a path that points at a broken `.nif` builds and bundles
fine but crashes in-game. The tool guarantees the **wiring and the bundle**, not the bytes.

## Data-relative path rules (this is the #1 gotcha)

Skyrim loads loose files from the game's `Data/` folder. Two different rooting conventions:

- **Model paths (`model` field)** are rooted at **`Data\Meshes\`**. So the engine path
  `Data\Meshes\MyMod\bell.nif` is written as **`MyMod\bell.nif`** — do **NOT** include the
  `Meshes\` prefix (ModForge's `validate` rejects a `model` that starts with `Meshes\`).
  (Confirmed from vanilla: IronSword's model is `Weapons\Iron\LongSword.nif`, i.e. on disk at
  `Data\Meshes\Weapons\Iron\LongSword.nif`.)
- **Sound file paths (`sounds[].files`)** are rooted at **`Data\`** and live under **`Sound\`**,
  e.g. `Sound\fx\mymod\bell.wav` (on disk at `Data\Sound\fx\mymod\bell.wav`). Include the
  `Sound\` segment.

Use **backslashes** (`\`) by Bethesda convention — ModForge also accepts `/` and the engine
normalizes. Paths must be **relative** (no `C:\…`, no leading `\`, no drive letter). Pick a unique
sub-folder named after your mod (`MyMod\…`, `Sound\fx\mymod\…`) so you never collide with vanilla or
another mod.

## Where you put the files: the assets source directory

`package` copies a **source asset directory** into the output mod folder. Point at it with the spec's
`assets` field (relative to the spec file, or absolute) **or** the CLI `--assets <dir>` override
(which wins). The source must contain the engine-standard sub-folders; ModForge bundles only these,
case-insensitively, preserving structure:

```
Meshes/  Textures/  Sounds/ (or Sound/)  Music/  Seq/
```

Anything outside those folders (a `README.txt`, a `Docs/` dir) is **ignored**. So lay out the source
exactly as it should appear under `Data/`:

```
my_assets/
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
  Meshes/actors/mymod/anims/idle.hkx        # .hkx ride along under Meshes\…
```

> Animations (`.hkx`) live **under `Meshes\`** in Skyrim (e.g.
> `Meshes\actors\character\animations\…`), so they are bundled as part of the `Meshes/` tree. Wiring
> a new animation into an actor's **behaviour graph** is a Creation-Kit / havok task ModForge does
> not perform — it only carries the file.

After `package`, the output folder is a drop-in MO2/Vortex mod:

```
OutModDir/
  MyMod.esp
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
```

## Spec fields

### `model` — a custom mesh on a record

Set `model` (a Data-relative `.nif` path, no `Meshes\` prefix) on **statics, activators, furniture,
miscItems, weapons**. When set, ModForge writes it into the record's model subrecord instead of (or
in addition to) cloning a template:

```jsonc
"statics":    [ { "editorId": "MFMonument", "model": "MyMod\\monument.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Forged Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Forged Bell", "model": "MyMod\\bell.nif" } ],
"miscItems":  [ { "editorId": "MFRelic", "name": "Forged Relic", "value": 250, "model": "MyMod\\relic.nif" } ]
```

- **`model` + `template` together** on a `miscItem`: ModForge warns and **`model` wins** (your mesh
  overrides the cloned template's).
- **A `weapon` with `model` but no `template`** likely **CRASHES on equip** — a weapon also needs
  1st-person model / animation type / equip data that only a `template` clone supplies. Pair a weapon
  `model` **with** a `template` of the same weapon type (the `model` then overrides just the
  world/3rd-person mesh). ModForge warns when a weapon has `model` and no `template`.
- Statics/activators/furniture are pure-mesh records — `model` alone is the normal, correct case.

### `sounds` — custom Sound Descriptors (SNDR)

A `sounds` entry emits a **Sound Descriptor** pointing at your `.wav`/`.xwm`. Records reference it by
`editorId`:

```jsonc
"sounds": [
  { "editorId": "MFBellChimeSD",
    "files": [ "Sound\\fx\\mymod\\bell_chime_01.wav" ],   // one or more; Data-relative under Sound\
    "category": "",            // ref → SNCT; empty -> Skyrim.esm:0x0172A1 AudioCategorySFX
    "outputModel": "",         // ref → SOPM; empty -> Skyrim.esm:0x0B4058 (vanilla SFX output)
    "priority": 128,
    "staticAttenuation": 5.0 } // dB attenuation
],
"activators": [ { "editorId": "MFBell", "name": "Bell", "model": "MyMod\\bell.nif",
                  "activationSound": "MFBellChimeSD" } ]
```

Sound-link fields that take a SNDR *ref* (an in-spec `sounds` editorId **or** a vanilla
`<master>:0xFORMID`):

| record | fields |
|---|---|
| `activators` | `activationSound`, `loopingSound` |
| `miscItems` | `pickUpSound`, `putDownSound` |
| `weapons` | `pickUpSound`, `putDownSound` |

`category`/`outputModel` default to the vanilla SFX category + output model so the sound is actually
audible without further tuning. This SNDR primitive is general by design — it is also the foundation
the planned voice/TTS pipeline (`.fuz` voice lines) will build on.

## Workflow

```bash
# 1) author your assets into a source dir laid out like Data/ (Meshes/Textures/Sound/…)
# 2) write the spec: model paths + sounds + (optionally) an `assets` dir
dotnet run --project src/ModForge.Cli -- validate examples/custom_asset_spec.json   # path-shape + refs
dotnet run --project src/ModForge.Cli -- package  examples/custom_asset_spec.json OutModDir
#    (or: package … OutModDir --assets /path/to/my_assets   to override the spec's `assets`)
dotnet run --project src/ModForge.Cli -- dump OutModDir/MFCustomAssets.esp           # verify wiring
find OutModDir -type f                                                                # verify bundle
```

`validate` checks: model is a `.nif`, not prefixed with `Meshes\`, relative; a sound has at least one
`.wav`/`.xwm` file; sound/category/output-model refs resolve. `dump` prints each record's `model:` path
and `activationSound`/`pickUpSound -> …` links and the SNDR's `soundFile=` paths.

A complete worked example is **`../examples/custom_asset_spec.json`** (with a placeholder asset tree
at `../examples/assets/customasset/` — stub bytes only; replace with real authored content).

## Limits — be honest

ModForge writes **structurally valid** records and **copies the files you give it**. It does **not**:

- author or validate mesh/texture/sound/animation **content** (a bad `.nif` still crashes in-game);
- wire animations into an actor **behaviour graph** (a CK/havok task);
- generate `.dds` from a `.nif`'s texture references (you supply both; the `.nif` names its textures).

Confirming a custom asset actually renders/plays needs a Proton/Skyrim launch — see
[for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim).
