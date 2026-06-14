# 05 — ModForge Integration Design

← [README](README.md) · prev: [04-nif-and-collision.md](04-nif-and-collision.md) · next: [06-standalone-runbook.md](06-standalone-runbook.md)

How the verified manual pipeline ([06]) folds into the generator. Design, not code — but it names concrete files, spec fields, and the existing conventions to copy. Grounded in the current `src` layout (`workflows/common/code-map/CODE_MAP.infra.md`) and the real `Spec.Items.cs` / `Assets.cs`.

**Copy these existing conventions exactly** (don't invent new ones):
- **Asset layer parallel to the record layer.** ModForge writes records + folder structure and **shells out** to external tools (Blender headless, ck-cmd, Compressonator) — it does **NOT** author nif/dds bytes. `Assets.cs` already says "does NOT author meshes." This is the xLODGen/Papyrus posture.
- **Shell-out with env-var fallback:** `Papyrus.cs` is the template — `null → MODFORGE_* env → default`, drives an exe under Wine *or* native. The mesh/texture tools copy this shape.
- **Asset copy + MO2 assembly:** `Assets.cs` copies `Meshes/Textures/Sounds` trees; `Package.cs` flattens the MO2 folder. Mesh output is just more `Meshes/…` + `Textures/…` for them to pick up.
- **Two-pass build:** `Generator.Build.cs` = pass 1 (records) → pass 2 (link). Mesh conversion is **independent of records** (it's pure asset work), so it's a **separate CLI step**, not a record builder — like `compile` is separate from `build`.

---

## 1. Spec design (additive — no breaking change)

Additive optional fields only (CLAUDE.md: new optional fields are safe; existing examples unaffected). After adding, update `examples/spec.schema.json` + `sample_spec.json`.

**`modelSource`** — an optional sibling block on any record that already has a `Model` field (`StaticSpec`, `FurnitureSpec`, `ActivatorSpec`, `MiscSpec`, weapon/armor `Model`). It says "produce the `.nif` that `Model` points at, from this source":
```jsonc
"statics": [{
  "editorId": "MyCrate",
  "model": "Meshes/Mine/crate.nif",        // existing field — the Data-relative target
  "modelSource": {                           // NEW optional block
    "file": "model-work/src/crate.fbx",     // source mesh (local, never committed)
    "sourceType": "gltf",                    // gltf | fbx | obj  (carries the [02] transform rule)
    "collision": "convex",                   // convex | box | none
    "materialProfile": "truepbr",            // truepbr | legacy  ([03])
    "backend": "niftools",                   // niftools | ckcmd | pynifly  ([01] §4)
    "textures": {                             // source → slot mapping ([03])
      "diffuse": "model-work/src/crate_d.png",
      "normal":  "model-work/src/crate_n.png",
      "rmaos":   "model-work/src/crate_orm.png"
    }
  }
}]
```
Absence of `modelSource` = today's behaviour (the `.nif` is user-supplied and rides the copy-trees). Presence = the build produces it.

> Field hygiene (CLAUDE.md): later removing/renaming a field needs `grep -r "field" examples/` + same-commit update of all hits. Adding is free.

---

## 2. New CLI step `importmesh` (parallel to `compile` / `package`)

Lives in `Program.Build.cs` alongside `build`/`validate`/`package`/`compile`. Unlike voice (`voicelines` needs the built esp for FormIDs), mesh conversion needs **only the spec** — so it can run *before or after* `build`. Pipeline per `modelSource`:

```
importmesh <spec.json>
  1. Read every record carrying a modelSource block
  2. Cache check: (source mtime + opts hash) unchanged & target nif exists → skip
  3. backend == niftools/ckcmd:
       shell out blender --background --python convert.py -- <args>   (native)
         · import file, apply per-sourceType transform ([02])
         · map materials → BSLighting/True-PBR; shell Compressonator for each .dds ([03])
         · generate convex/box bhk collision ([04] §3)
         · write Data-relative texture paths into BSShaderTextureSet
         · export NiTriShape .nif
     backend == pynifly (skinned):
       NOT run here — emit a manifest line "reboot to Windows, run pynifly_export.py" ([07])
  4. Place .nif + .dds into the Meshes/ … Textures/ tree package already bundles
```
Steps 1–4 are automatable for statics; the pynifly branch is a deliberate manual hand-off (dual-boot). Then `package` sweeps the output into the zip.

**Why a separate step:** conversion is slow, has heavy optional external deps (Blender, Wine tools), and is pure asset work. Keeping it out of `build` keeps `build` fast and dependency-free; `importmesh` is opt-in — same reasoning as `compile` vs `build`.

**Full build order:** `importmesh` → `build` → `package` (or `build` → `importmesh` → `package`; order-independent because meshes don't depend on FormIDs). Record in `SPEC-workflow.md`.

---

## 3. New core file `Mesh.cs` + `convert.py`

- **`Mesh.cs`** (Core) — shell-out orchestration, mirrors `Papyrus.cs`: a `MeshOptions` class with `null → MODFORGE_* → default` for each backend exe; "tool missing → warn, skip" (never hard-fail). Resolves backend per `modelSource.backend`. Keep ≤300 lines (CLAUDE.md).
- **`convert.py`** (repo-shipped, embedded or shipped beside the CLI like the `.pex` resources) — the headless Blender script doing import/transform/material/collision/export. Versioned with the repo; `Mesh.cs` invokes it via `blender --background --python convert.py -- <json-args>`.
- *(No C# nif writer.)* Unlike the voice plan's native `WriteFuz` (fuz is tiny + verified), nif is a large opaque format — we **orchestrate Blender/ck-cmd**, never self-author. (`nifly` C++ lib is a fallback only if in-process authoring is ever wanted — not recommended.)

A small **pure, testable** helper *can* live in Core: `MeshPath.Validate(spec)` — confirms every `Model`/texture path is well-formed and resolves to a packaged location (feeds the `meshdiag` of §6). That's record/string logic, unit-testable without Blender, like `Generator.SceneFragments.cs`.

---

## 4. Tool config (env vars, conditional)

Mirrors `Papyrus.cs`/`PapyrusOptions`:

| Env var | Points at | Missing → |
|---------|-----------|-----------|
| `MODFORGE_BLENDER` | `blender` binary | skip mesh conversion, warn (user-supplied nif still works) |
| `MODFORGE_COMPRESSONATOR` | `compressonatorcli` | skip dds compression, warn (or pass-through PNG — invalid, so warn loudly) |
| `MODFORGE_TEXCONV` | `texconv.exe` (Wine) | alternate to Compressonator; unset = use Compressonator |
| `MODFORGE_CKCMD` | `ck-cmd` (may carry `wine ` prefix) | only needed if `backend: ckcmd` |
| `MODFORGE_PYNIFLY_MANIFEST` | path to write the skinned hand-off list | only for `backend: pynifly` |

Each missing tool degrades gracefully to the next-lower capability with a warning, never hard-fails — the existing conditional-embed / conditional-tool stance.

---

## 5. Package + build-pipeline wiring

- `Assets.cs` already copies `Meshes`/`Textures` trees — confirm the glob covers `Meshes/<sub>/` and `Textures/<sub>/` where `importmesh` writes (it should; same trees as user-supplied assets).
- `Package.cs` flat MO2 assembly already handles `Meshes/`+`Textures/`; converted assets ride along. **No `.seq` interaction.**
- `StaticSpec.AlternateTextures` (already exists) lets one nif be reused with swapped texture sets per placement — useful for variant props without re-converting.

---

## 6. Maintenance-chain landing (when implemented, not now)

Per CLAUDE.md Workflow 1, on landing (this is research):
- **Code:** `Spec.Items.cs` (+ a `ModelSourceSpec` record), `Mesh.cs`, `convert.py`, `Program.Build.cs`, `examples/spec.schema.json` + `sample_spec.json`.
- **CODE_MAP:** add `Mesh.cs` to `CODE_MAP.infra.md`; `importmesh` into the CLI table; `modelSource` cross-ref into `CODE_MAP.world.md` (static/placement) and `CODE_MAP.items-magic.md` (weapon/armor models). Add a Tests row (`MeshPathTests`).
- **Docs:** `modelSource` field into `SPEC-world.md` / `SPEC-items-magic.md` (or a new `SPEC-assets.md` if it grows); `importmesh` into `for_agent_cli.md` + `SPEC-workflow.md`.
- New diag **`meshdiag <esp>`** (parallel to `lightdiag`/`identitydiag`) — verifies `Model`/texture path resolution from the built esp without the game. High value given the invisible-on-wrong-path failure mode.

---

## 7. What "done" looks like

`modforge importmesh spec.json` reads each `modelSource`, shells `convert.py` to produce a `NiTriShape` `.nif` + `.dds` at the deterministic Data-relative paths, `package` bundles them — with `MODFORGE_BLENDER` set, degrading gracefully (Compressonator/ck-cmd/pynifly) when other tools are unset. The runbook ([06]) is the spec for what this step automates; the skinned `pynifly` backend ([07]) is a documented manual hand-off, not yet automated.

---

### Sources
Internal conventions from `workflows/common/code-map/CODE_MAP.infra.md`, `src/ModForge.Core/Papyrus.cs`, `src/ModForge.Core/Assets.cs`, `src/ModForge.Core/Spec.Items.cs`, `src/ModForge.Cli/ModForge.Cli.csproj`. Engine/format facts: [01]–[04].
