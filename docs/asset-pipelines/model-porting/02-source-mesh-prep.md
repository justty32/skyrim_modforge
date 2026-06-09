# 02 — Source Mesh Prep (import + calibrate, before NIF)

← [README](README.md) · prev: [01-toolchain-setup.md](01-toolchain-setup.md) · next: [03-materials-textures.md](03-materials-textures.md)

You chose **generic FBX/OBJ/glTF** as the MVP source, so the front door is Blender's built-in importers — native, no extraction step. This chapter is about getting the mesh into Blender *correctly oriented and scaled* and clean enough to export, because every downstream step assumes a sane Blender scene. Game-specific extraction is the appendix (§5), deferred until you pick a title.

---

## 1. Import (native, built-in)

```
File → Import → FBX (.fbx) / Wavefront (.obj) / glTF 2.0 (.glb/.gltf)
```
All three are core Blender importers — no addon, native Linux. glTF is the cleanest (PBR materials + correct unit metadata travel with the file); FBX is the most common from game rips; OBJ is geometry-only (no skeleton, no PBR — fine for statics, re-author materials in [03]).

**Pick one mesh** for the first run. A single static prop (a crate, a statue, a weapon model used as a static) is the MVP target — no skeleton.

---

## 2. The transform problem (the #1 silent-failure source after paths)

Skyrim's convention: **Z-up, −Y forward**, and "1 Blender unit = 1 Skyrim unit" *when export scaling is applied correctly*. Sources disagree on both axis and unit:

| Source convention | Up / Forward | Unit | Typical fix on import |
|---|---|---|---|
| **glTF 2.0** | Y-up | metres | rotate +90° X to Z-up; scale to Skyrim units |
| **FBX (Unity rip)** | Y-up | metres | same as glTF; Unity humanoid scale ≈ 1 m |
| **FBX (UE rip)** | Z-up | **centimetres** | axis often OK; **÷100 then ×Skyrim-unit** (the classic cm-vs-m trap) |
| **OBJ** | Y-up (usually) | unitless | rotate + scale, measured |

**Calibration procedure (do once per source convention, then reuse the constant):**
1. Import a vanilla Skyrim `.nif` of **known real-world size** (e.g. a door ≈ 200 units, a barrel) via NifTools addon into the same Blender scene — this is your **ruler**.
2. Import your source mesh next to it.
3. Scale + rotate your mesh until it matches the ruler's real proportions and sits Z-up/−Y-forward.
4. **Record the exact scale factor and rotation** for that source convention. Bake it into the per-source rule ([05] `modelSource.sourceType` carries this).
5. **Apply** the transform (`Ctrl+A → Rotation & Scale`) so it's baked into the geometry, not left on the object — exporters read applied transforms reliably.

> The community phrasing: "an object 128 'units' tall in Blender becomes 128 Skyrim units after export with Apply Scaling = FBX All; the mismatch is almost always centimetres vs metres." Don't trust a remembered magic constant — **measure against a vanilla ruler** the first time, then the constant is trustworthy for that source.

---

## 3. Mesh hygiene (cheap, prevents export errors)

Before export, in Blender:
- **Triangulate** (Skyrim geometry is triangles; `Ctrl+T` in edit mode, or a Triangulate modifier applied). NifTools/ck-cmd will triangulate, but doing it yourself makes the result predictable.
- **One object = one `NiTriShape`** by default; if a prop has multiple materials, either split per material or let the exporter emit multiple shapes (see [03] on material→shape mapping).
- **Apply transforms** (again — `Ctrl+A`), recalculate normals outside (`Shift+N`), remove doubles (`M → By Distance`).
- **UVs must exist** — without a UV map the textures in [03] have nowhere to land. Most rips carry UVs; OBJ/FBX usually do.
- **Origin** at a sensible point (object origin becomes the placement pivot in-game). For a floor-standing prop, origin at the base centre.

---

## 4. Where the mesh goes

Keep the working tree from [01]:
```
~/model-work/src/crate.fbx          # incoming
~/model-work/src/crate_diffuse.png  # incoming textures (→ .dds in [03])
~/model-work/out/crate.nif          # produced in [04]
~/model-work/out/textures/...dds    # produced in [03]
~/model-work/vanilla/Barrel01.nif   # the ruler from §2
```
Nothing here is committed — assets stay local per the legal rule (README).

---

## 5. Appendix — game-specific extraction (deferred; pick a title later)

You chose generic formats for the MVP, so this is reference only. All three converge on Blender, where §1 resumes. Detail lives in the survey [`../03-3d-model-import.md §6`](../03-3d-model-import.md).

- **Dark Souls / FromSoft → `soulstruct-blender`** (Blender 4.1–5.0, pure Python, **native Linux**). Imports FLVER directly: characters, objects, equipment, **map pieces (= statics)**, with armatures/weights. *Cleanest* — if you later want a real game source, start here.
- **Wuthering Waves (UE5) → FModel / CUE4Parse** (.NET, cross-platform, native Linux). Exports UE skeletal/static meshes → FBX/glTF + textures. Needs the game's `.usmap` + AES key.
- **Genshin (Unity) → 3DMigoto F8 frame-dump + GIMI** (DX11/Windows → run under Proton/Wine; Blender reconstruction native). *Least Linux-clean* — Genshin encrypts bundles, so AssetRipper can't read them directly.

When you pick one, this appendix becomes a real chapter; until then the generic importers (§1) are the path.

---

## 6. What "done" looks like

- A single source mesh imported, **Z-up / −Y-forward**, scaled to match a vanilla ruler, transforms **applied**, triangulated, UV'd.
- The per-source scale+rotation constant recorded (so the next mesh from the same source skips the calibration).

→ [03](03-materials-textures.md) handles its materials/textures, then [04](04-nif-and-collision.md) exports the nif.

---

### Sources
Blender built-in FBX/OBJ/glTF importers (native). Scale/orientation: [Beyond Skyrim Arcane University — Mesh Export to NIF](https://wiki.beyondskyrim.org/wiki/Arcane_University:Mesh_Export_to_NIF), [Getting Your Models into Skyrim (morroblivion)](https://morroblivion.com/files/modeltoskyrimguide.pdf). Extraction (appendix): [soulstruct-blender (GH Grimrukh)](https://github.com/Grimrukh/soulstruct-blender), [FModel](https://fmodel.app/), [GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer) — and the survey [`../03-3d-model-import.md`](../03-3d-model-import.md) §6.
