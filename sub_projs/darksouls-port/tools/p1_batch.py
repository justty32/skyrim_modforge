#!/usr/bin/env python3
"""p1_batch — P1「空殼院」batch driver.

Turns the extracted m18_01_00_00 assets into a packageable asset dir + spec:

  1. render NIFs : extracted/m*/<stem>.gltf --(gltf2nif)--> out/p1_assets/Meshes/DsPort/<stem>.nif
  2. collision   : extracted/collision/h*.hulls.json chunked to <=57 hulls
                   (57 = the exact bhkListShape size in-game confirmed by P0),
                   each chunk on a tiny-triangle "carrier" NIF
                   --> out/p1_assets/Meshes/DsPort/col/<hstem>_c<i>.nif
  3. textures    : union of every piece's referenced DDS
                   --> out/p1_assets/Textures/DsPort/m18/<name>.dds
  4. spec        : p1/ds_port_p1_spec.json — DSPortWorld (SmallWorld, flat LAND
                   safety net) + one STAT/placement per NIF, all at ANCHOR.

All MSB map pieces are baked at (0,200,0) identity, so every placement shares
ONE anchor; the +200m Y is common and simply ignored (FLVER-local == relative).

Coordinate map (DS Y-up metres -> Skyrim Z-up units, same as gltf2nif):
    world = ANCHOR + (x*70.03, -z*70.03, y*70.03)

ANCHOR is chosen so the MSB player start (-13.75, -15.25, -44.5) lands exactly
at the centre of cell (0,0) => in-game entry is:
    cow DSPortWorld 0 0        (drops you on the LAND safety net at z~4000)
    player.setpos z 19935      (lifts you to the asylum start-room floor)

Run with the model-converter venv NOT required — this script shells out to it:
    python3 tools/p1_batch.py [--skip-nifs]
"""
import json, os, subprocess, sys, base64, struct, shutil, glob

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                       # sub_projs/darksouls-port
MC = os.path.join(os.path.dirname(ROOT), 'model-converter')
MC_PY = os.path.join(MC, '.venv', 'bin', 'python')
EXTR = os.path.join(ROOT, 'extracted')
ASSETS = os.path.join(ROOT, 'out', 'p1_assets')
SPEC_OUT = os.path.join(ROOT, 'p1', 'ds_port_p1_spec.json')

SCALE = 70.03
# Excluded from render: m0160 (unreferenced 1-tri), m9999 (flat black occluder),
# m9000/m9100 (±1.5-2.9 km skybox backdrops) and m5201 (±550 m far terrain) —
# the huge backdrops are a P2 topic (vert budget / cull behaviour unproven).
RENDER_EXCLUDE = {'m0160B1A18', 'm9000B1A18', 'm9100B1A18', 'm9999B1A18', 'm5201B1A18'}
MAX_HULLS_PER_NIF = 57            # P0 in-game confirmed bhkListShape size
TEXPREFIX = r'textures\dsport\m18'

# Player start (MSB Player part, piece-local) mapped to the centre of cell (0,0):
# M(start) = (-962.9, +3116.3, -1068.0)  =>  ANCHOR = (2048,2048,21000) - M(start)_xy
ANCHOR = (3010.9, -1068.3, 21000.0)
START_Z = 19935                    # asylum start-room floor ~19928.5 + margin

def run_gltf2nif(gltf, nif, collision=None):
    cmd = [MC_PY, '-m', 'gltf2nif', gltf, nif, '--texprefix', TEXPREFIX]
    if collision:
        cmd += ['--collision', collision]
    r = subprocess.run(cmd, cwd=MC, capture_output=True, text=True)
    if r.returncode != 0:
        print(f"FAIL gltf2nif {os.path.basename(nif)}\n{r.stdout}\n{r.stderr}")
    return r.returncode == 0

def carrier_gltf(path, centroid):
    """Minimal 1mm triangle at `centroid` (DS metres Y-up) — invisible render
    payload whose only job is to carry a bhk collision tree."""
    cx, cy, cz = centroid
    verts = [(cx, cy, cz), (cx + 0.001, cy, cz), (cx, cy + 0.001, cz)]
    buf = b''.join(struct.pack('<3f', *v) for v in verts)
    mn = [min(v[i] for v in verts) for i in range(3)]
    mx = [max(v[i] for v in verts) for i in range(3)]
    g = {
        "asset": {"version": "2.0"},
        "scene": 0, "scenes": [{"nodes": [0]}], "nodes": [{"mesh": 0}],
        "meshes": [{"primitives": [{"attributes": {"POSITION": 0}, "material": 0}]}],
        "materials": [{"name": "m19_B_wall_07"}],
        "accessors": [{"bufferView": 0, "componentType": 5126, "count": 3,
                       "type": "VEC3", "min": mn, "max": mx}],
        "bufferViews": [{"buffer": 0, "byteOffset": 0, "byteLength": len(buf)}],
        "buffers": [{"byteLength": len(buf),
                     "uri": "data:application/octet-stream;base64," + base64.b64encode(buf).decode()}],
    }
    with open(path, 'w') as f:
        json.dump(g, f)

def main():
    skip_nifs = '--skip-nifs' in sys.argv
    mesh_dir = os.path.join(ASSETS, 'Meshes', 'DsPort')
    col_dir = os.path.join(mesh_dir, 'col')
    tex_dir = os.path.join(ASSETS, 'Textures', 'DsPort', 'm18')
    for d in (mesh_dir, col_dir, tex_dir):
        os.makedirs(d, exist_ok=True)

    # ---- 1. render NIFs -----------------------------------------------------
    render = []
    for gltf in sorted(glob.glob(os.path.join(EXTR, 'm*', 'm*.gltf'))):
        stem = os.path.basename(gltf)[:-5]
        if stem in RENDER_EXCLUDE:
            continue
        render.append(stem)
        nif = os.path.join(mesh_dir, stem + '.nif')
        if not skip_nifs and not run_gltf2nif(gltf, nif):
            sys.exit(f"render nif failed: {stem}")
    print(f"render NIFs: {len(render)}")

    # ---- 2. collision carriers ---------------------------------------------
    scratch = os.path.join(ROOT, 'out', 'p1_carriers')
    os.makedirs(scratch, exist_ok=True)
    carriers = []
    for hj in sorted(glob.glob(os.path.join(EXTR, 'collision', 'h*.hulls.json'))):
        hstem = os.path.basename(hj)[:-len('.hulls.json')]
        hulls = json.load(open(hj))['hulls']
        chunks = [hulls[i:i + MAX_HULLS_PER_NIF] for i in range(0, len(hulls), MAX_HULLS_PER_NIF)]
        for ci, chunk in enumerate(chunks):
            name = f"{hstem}_c{ci}"
            cj = os.path.join(scratch, name + '.hulls.json')
            json.dump({"hulls": chunk}, open(cj, 'w'))
            vs = [v for h in chunk for v in h['vertices']]
            centroid = tuple(sum(v[i] for v in vs) / len(vs) for i in range(3))
            cg = os.path.join(scratch, name + '.gltf')
            carrier_gltf(cg, centroid)
            nif = os.path.join(col_dir, name + '.nif')
            if not skip_nifs and not run_gltf2nif(cg, nif, collision=cj):
                sys.exit(f"carrier nif failed: {name}")
            carriers.append(name)
    print(f"carrier NIFs: {len(carriers)}")

    # ---- 3. textures ---------------------------------------------------------
    pool = {f.lower(): f for f in os.listdir(os.path.join(EXTR, 'textures_pool'))}
    stems = set()
    for tj in glob.glob(os.path.join(EXTR, 'm*', '*.textures.json')):
        base = os.path.basename(tj).replace('.textures.json', '')
        if base in RENDER_EXCLUDE:
            continue
        stems.update(json.load(open(tj)))
    ntex = 0
    for s in sorted(stems):
        f = pool.get((s + '.dds').lower())
        if f:
            shutil.copy2(os.path.join(EXTR, 'textures_pool', f), os.path.join(tex_dir, f))
            ntex += 1
    print(f"textures: {ntex}/{len(stems)}")

    # ---- 4. spec --------------------------------------------------------------
    ax, ay, az = ANCHOR
    statics, placements = [], []
    for stem in render:
        ed = f"DSP_{stem}"
        statics.append({"editorId": ed, "model": f"DsPort\\{stem}.nif"})
        placements.append({"base": ed, "worldspace": "DSPortWorld",
                           "position": {"x": ax, "y": ay, "z": az}})
    for name in carriers:
        ed = f"DSPC_{name}"
        statics.append({"editorId": ed, "model": f"DsPort\\col\\{name}.nif"})
        placements.append({"base": ed, "worldspace": "DSPortWorld",
                           "position": {"x": ax, "y": ay, "z": az}})

    cells = [{"x": x, "y": y, "height": 4000}
             for x in range(-3, 5) for y in range(-2, 5)]

    spec = {
        "pluginName": "DSPortP1.esp",
        "esl": False,
        "_what": ("P1 'empty shell asylum': all Undead Asylum map pieces "
                  f"({len(render)} render NIFs) + full DS collision as {len(carriers)} "
                  "carrier NIFs (<=57 hulls each, P0-confirmed scale) in a custom "
                  "SmallWorld worldspace over a flat LAND safety net at z=4000. "
                  "Excluded: m9000/m9100/m5201 giant backdrops, m9999 black occluder (P2)."),
        "_coords": ("All MSB pieces are baked at one origin -> every placement sits at "
                    f"ANCHOR ({ax},{ay},{az}); world = ANCHOR + (x*70.03, -z*70.03, y*70.03) "
                    "of DS-local metres. MSB player start maps to the centre of cell (0,0). "
                    f"IN-GAME: cow DSPortWorld 0 0  then  player.setpos z {START_Z} "
                    "(start-room floor ~19928; LAND net is 16 km below the asylum — "
                    "cow drops you on the net, setpos lifts you into the start room)."),
        "worldspaces": [{
            "editorId": "DSPortWorld",
            "name": "Northern Undead Asylum",
            "climate": "Skyrim.esm:0x000812",
            "water": "Skyrim.esm:0x000018",
            "flags": ["SmallWorld", "CannotFastTravel"],
            "defaultLandHeight": 4000,
            "defaultWaterHeight": -14000,
            "cells": cells,
        }],
        "statics": statics,
        "placements": placements,
    }
    os.makedirs(os.path.dirname(SPEC_OUT), exist_ok=True)
    json.dump(spec, open(SPEC_OUT, 'w'), indent=1)
    print(f"spec: {SPEC_OUT}  (statics={len(statics)} placements={len(placements)} cells={len(cells)})")

if __name__ == '__main__':
    main()
