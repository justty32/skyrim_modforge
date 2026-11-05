# 05 — ModForge Integration Design — §1–3 spec、CLI、核心檔

← [入口](../05-modforge-integration.md)

## 1. Spec design (additive — no breaking change)

Additive optional fields only (CLAUDE.md: new optional fields are safe; existing examples unaffected). After adding, update `examples/spec.schema.json` + `sample_spec.json`.

**`modelSource`** — an optional sibling block on any record that already has a `Model` field (`StaticSpec`, `FurnitureSpec`, `ActivatorSpec`, `MiscSpec`, weapon/armor `Model`). It says "produce the `.nif` that `Model` points at, from this source":
```jsonc
"statics": [{
  "editorId": "MyCrate",
  "model": "Mine/crate.nif",               // existing field — relative to Meshes (not Data)
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
