# 07 — Skinned Characters (deferred design)

← [README](README.md) · prev: [06-standalone-runbook.md](06-standalone-runbook.md)

The static spine ([02]–[06]) is runnable today. Skinned characters/armor are **designed here but deferred** — you chose static-first. This chapter names the walls and the path through them so that when you do start, the shape is already decided. The headline: **this is where you reboot to Windows for PyNifly.**

---

## 1. Why skinned is a different animal

A static is geometry + material + simple collision. A skinned mesh adds three hard things:
1. A **Skyrim skeleton** with exact bone names (`NPC Spine [Spn1]`, `NPC L UpperArm [LUpr]`, …).
2. **Skin weights** — ≤4 bones per vertex, normalized.
3. A **`BSDismemberSkinInstance`** with body-part partitions (the flags that let the game hide/sever parts).

NifTools addon does statics, not skinned-SSE well. **PyNifly** does all three correctly (skin, partitions, `_0`/`_1` weight nifs, full `BSTriShape`) — and it's **Windows-only**. Your dual-boot makes this a clean reboot, not a fragile Wine fight ([01] §2).

---

## 2. The pipeline (Blender, then reboot for export)

Most of the work is in Blender (native Manjaro); only the final nif export needs Windows.

```
[Manjaro / Blender]
  1. Import source mesh + its skeleton ([02] / game extractor)
  2. Retarget source skeleton → Skyrim skeleton   ← WALL 1 (per-source bone map)
  3. Transfer weights to the Skyrim skeleton       (Outfit Studio Copy-Bone-Weights)
  4. Clamp ≤4 weights/vertex, normalize
  5. Save .blend
[REBOOT → Windows / Blender + PyNifly]
  6. Open .blend, build BSDismemberSkinInstance partitions
  7. PyNifly export skinned SSE .nif (skin + partitions + weights)
  8. copy .nif back to the Manjaro build tree
[Manjaro]
  9. textures → .dds ([03]), paths into the build tree ([04] §4-§5)
 10. handoff to animation .hkx                       ← WALL 2 (Havok — separate pipeline)
```

---

## 3. Wall 1 — skeleton retargeting (the human-judgment step)

Source skeletons (Unity humanoid for Genshin, UE skeletal for WuWa, FromSoft rigs) differ from Skyrim's in bone names, count, orientation, and rest pose.

- **Build a source→Skyrim bone-name map once per source skeleton.** Genshin's `Bip001 Spine` → Skyrim `NPC Spine [Spn1]`, etc. This map is *human work* the first time; applying it is batchable — the "write the mapping once, reuse" philosophy (IDEAS §13/§14).
- **Tools:** Blender Rigify/retarget addons, or manual constraint-based retarget. For armor that just needs to fit a body, you often skip full retarget and go straight to weight-transfer (§4) from a reference body.
- **Output:** the mesh posed on / parented to the Skyrim skeleton.

This is *semi*-automatable: the per-rig map is manual, applying it is scriptable. ModForge can store the map per `sourceType` and apply it in `convert.py`, but authoring it is offline human work.

---

## 4. Wall 1b — weight transfer (mostly mechanical)

**Outfit Studio → Shape → Copy Bone Weights** from a reference body (CBBE/UNP/vanilla) onto your mesh. This is the standard armor-refit workflow — largely point-and-click, and it sets default `BSDismember` partitions on import. Outfit Studio has a "Building on Linux" path (native) or runs under Wine ([01] §3); either works since this is pre-export.

After transfer: clamp to ≤4 weights/vertex (Blender weight-tools or Outfit Studio), normalize, sanity-check no vertex has zero total weight (→ explodes in-game).

---

## 5. Wall 2 — animation (`.hkx`, out of scope here)

A rigged mesh still needs `.hkx` to move (idle, walk, attack). That's a **separate pipeline** — see the survey [`../05-animation-pipeline.md`](../05-animation-pipeline.md) (OAR/serde-hkx/Pandora). This chapter stops at "mesh skinned to the Skyrim skeleton with valid partitions, exported via PyNifly." If you reuse a vanilla skeleton + race, the character animates with existing Skyrim animations — so you may not need new `.hkx` at all for a first character.

---

## 6. ModForge integration (when this lands)

The `modelSource.backend: pynifly` branch ([05] §2) is a **deliberate manual hand-off**, not automated:
- `importmesh` detects `backend: pynifly`, does the Blender-side prep it can, and writes a **manifest** (`MODFORGE_PYNIFLY_MANIFEST`) listing what to export Windows-side.
- You reboot, run a `pynifly_export.py` over the manifest, copy results back.
- `package` then bundles the skinned nif like any other mesh.

A fully-automated skinned path would need PyNifly callable headless on Linux — which it isn't. The dual-boot + manifest is the honest design: automate everything up to the Windows-only seam, make the seam a documented one-command reboot step.

NPC wiring (race, head parts, `WNAM`/skin) reuses the existing `NpcSpec` machinery — the skinned nif is just the body/armor mesh the NPC or armor record points at via its `Model` field.

---

## 7. What "done" looks like (when you tackle it)

- A source→Skyrim bone map for one source skeleton, stored and reusable.
- One character/armor mesh: retargeted, weight-transferred (≤4/vertex), `BSDismember` partitions, **PyNifly-exported skinned SSE nif**, rendering in-game on a vanilla skeleton (animating with existing Skyrim anims).
- The `pynifly` manifest hand-off documented as a build step.

Then — and only if a custom-animated character is wanted — cross into the `.hkx` pipeline (survey [05]).

---

### Sources
[PyNifly (GH BadDogSkyrim — skinned/partitions/weights, Windows-only)](https://github.com/BadDogSkyrim/PyNifly) · [Outfit Studio — Copy Bone Weights + Building on Linux (GH ousnius wiki)](https://github.com/ousnius/BodySlide-and-Outfit-Studio/wiki/Copying-bone-weights) · [Beyond Skyrim — Rigging in Outfit Studio](https://wiki.beyondskyrim.org/wiki/Arcane_University:Rigging_in_Outfit_Studio) · `BSDismemberSkinInstance` / 80-bone-partition cap (Beyond Skyrim NIF Data Format). Animation handoff: survey [`../05-animation-pipeline.md`](../05-animation-pipeline.md).
