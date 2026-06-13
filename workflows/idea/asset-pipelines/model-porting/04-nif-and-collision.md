# 04 — NIF Export + Collision (the format target)

← [README](README.md) · prev: [03-materials-textures.md](03-materials-textures.md) · next: [05-modforge-integration.md](05-modforge-integration.md)

This is the model-porting analogue of the voice plan's "`.fuz` + filename" chapter: the precise target format, plus the one thing ModForge computes deterministically that nobody else can (correct Data-relative paths). For a **static prop** this is fully native Manjaro.

---

## 1. The `.nif` target (just enough to be safe)

NIF = a node-graph binary scene: `NiNode` transforms with leaf geometry + property + collision blocks. For a static prop you need exactly:

```
NiNode (root)
 ├─ BSTriShape / NiTriShape        ← the geometry
 │    ├─ BSLightingShaderProperty  ← material ([03])
 │    │    └─ BSShaderTextureSet   ← Data-relative texture paths ([03] §1)
 │    └─ (vertex data, UVs, normals)
 └─ bhkCollisionObject             ← collision ([§3])
      └─ bhkRigidBody → bhkShape
```

**The Linux escape hatch (verified, load-bearing for this whole plan):**
- SSE's native geometry block is **`BSTriShape`** (packed/half-precision). LE's is **`NiTriShape`**.
- **LE-form `NiTriShape` nifs load fine in SSE** and, being lower-precision, are often *recommended* for SSE statics. So a NifTools-addon export — which emits `NiTriShape` — is a **valid SSE asset without conversion.**
- **"SSE-optimization"** (`NiTriShape → BSTriShape` via SSE NIF Optimizer / Cathedral Assets Optimizer) is an *optimization*, **skippable** for statics. If you ever want it, it's a Wine one-shot at the end.

This is why the static path is native: NifTools writes `NiTriShape`, and that's a shippable SSE static.

---

## 2. Exporting the static nif (NifTools addon, native)

In Blender (GUI first time, headless later via [05]):
```
File → Export → NetImmerse/Gamebryo (.nif)
  Game: Skyrim Special Edition   (emits NiTriShape — valid in SSE)
  Apply Scaling: as calibrated in [02]
```
NifTools addon confirmed (2026): **basic unweighted Skyrim SE export** + convex/box collision. It does **not** do skinned SSE well — that's the PyNifly/Windows path ([07]) — and does **not** do MOPP concave collision (§3).

**ck-cmd alternate (Wine):** `ck-cmd importfbx crate.fbx -e out/` → nif with materials/vertex-colors, "95% game-ready", **LE-form** (loads in SSE). Use if NifTools material mapping fights a particular source ([01] §3). Same `NiTriShape`-is-valid logic applies.

---

## 3. Collision (the static prop's only likely wall)

A static with no collision = the player walks through / falls through it. Tiers:

| Collision | How | Native? | When |
|-----------|-----|---------|------|
| **Box** | bounding box → `bhkBoxShape` | ✅ NifTools | blocky props (crates, pillars) |
| **Convex hull** | hull of the mesh → `bhkConvexVerticesShape` | ✅ NifTools (confirmed: name a `CollisionPolyhedron` mesh, export) | most props — convex approximation is fine |
| **Concave (MOPP)** | `bhkMoppBvTreeShape` | ❌ **wall** — NifTools can't author MOPP | concave/hollow geometry where the player must enter |

**Native recipe (convex/box):**
1. In Blender, duplicate the mesh, make a **convex hull** (`Mesh → Convex Hull` in edit mode, or a low-poly box), name it per NifTools' collision convention.
2. Set the `bhk` layer / material (e.g. stone, metal) — affects sound + physics.
3. Export — NifTools writes the `bhkCollisionObject → bhkRigidBody → bhkConvexVerticesShape`.

**MOPP workaround (no Havok SDK):** decompose concave geometry into **several convex pieces** (each a `bhkConvexVerticesShape`), union under one `bhkRigidBody` / `bhkListShape`. Or accept a box for props you don't enter. True concave MOPP needs Havok tooling — out of scope for the static MVP; revisit only if a specific asset demands it.

---

## 4. Data-relative paths — ModForge's determinism lever

This is the model-porting equivalent of the voice plan's filename rule: the place where a wrong string = silent failure, and the place ModForge uniquely controls.

- The nif bakes **Data-relative texture paths** into `BSShaderTextureSet` (slot 0 = `Textures\Mine\crate.dds`, etc.).
- The plugin's `StaticSpec.Model` (or `Furniture`/`Activator`) bakes a **Data-relative mesh path** (`Meshes\Mine\crate.nif`).
- **Both must match where `package` actually places the files.** A typo, wrong case, or wrong sub-folder = invisible object / untextured, **no error log** ([[vanilla-nif-paths-must-be-verified]]; the `Model` field in `Spec.MagicFx.cs` already carries the "wrong = invisible" warning).

**Why ModForge gets this right for free:** ModForge *is* the generator — it decides the mesh path in the spec, decides where `package` copies the `.nif`/`.dds`, and can write the texture-set paths into the nif during the export script. So all three (spec path, on-disk path, in-nif path) come from one source of truth. The standalone manual flow ([06]) has to keep them in sync by hand; the integrated flow ([05]) makes them one computed value — the same "ModForge owns the identifiers" superpower as the voice filename rule.

**Verification without launching the game** (per `ingame-test-workflow`): a `meshdiag`/`modeldiag` step (parallel to `lightdiag`/`identitydiag`) reads the built esp's `STAT`/`FURN`/`ACTI` records, resolves each `Model` path, and checks the file exists at the packaged location + the nif's texture paths resolve. Catches the dominant failure mode structurally.

---

## 5. Packaging into the build tree

`Assets.cs` already copies `Meshes/`, `Textures/`, `Sounds/…` trees into the MO2 layout, and `package` bundles them — **the nif/dds ride along unchanged**, exactly like the voice files rode the `Sound/Voice/...` copy. Output tree:
```
<zip root>/
  MyMod.esp
  Meshes/Mine/crate.nif
  Textures/Mine/crate.dds
  Textures/Mine/crate_n.dds
```
No `.seq` interaction (static ≠ quest). Per [[mo2-reinstall-reverts-manual-pex]], always rebuild into the zip — never hand-drop into the live MO2 folder.

---

## 6. What "done" looks like

- A `NiTriShape` `.nif` that opens clean in NifSkope: geometry, `BSLightingShaderProperty` + texture paths, and a `bhkConvexVerticesShape`/`bhkBoxShape`.
- Texture paths in the nif **match** the packaged `.dds` locations.
- (Stretch) a `meshdiag` that verifies path resolution from the built esp without the game.

→ [05](05-modforge-integration.md) turns this manual export into an `importmesh` CLI step.

---

### Sources
[Blender NifTools addon — Collision Objects (convex-hull → `bhkConvexVerticesShape`, no MOPP, basic SSE export)](https://blender-niftools-addon.readthedocs.io/en/latest/user/features/collisions/collision_objects.html) · [ck-cmd (GH aerisarn — fbx→nif, LE-form)](https://github.com/aerisarn/ck-cmd) · SSE NIF Optimizer (Nexus #4089) · Beyond Skyrim NIF Data Format. Internal: `StaticSpec`/`FurnitureSpec`/`ActivatorSpec` (`Spec.Items.cs`), `Assets.cs` copy-trees, `Spec.MagicFx.cs` ("wrong = invisible").
