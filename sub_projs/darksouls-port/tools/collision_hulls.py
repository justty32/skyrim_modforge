#!/usr/bin/env python3
"""collision_hulls.py -- DS(R) collision hkx -> triangle mesh + convex hulls (route A).

Stage 2 of the darksouls-port collision pipeline. The C# `DsExtractor hkx-extract`
unpacks the hkxbhd/hkxbdt container into per-piece `.hkx` tagfiles; this tool turns
ONE such piece into:

  <name>.collision.json  -- raw triangle mesh (vertices + faces), DS metres, Y-up.
  <name>.hulls.json      -- convex hulls, the interface agreed with gltf2nif:
        {"hulls": [ {"vertices": [[x,y,z], ...]}, ... ]}
     Coordinates stay in DS native metres, Y-up. DO NOT rescale / axis-swap here;
     that is the NIF writer's job.

Why Python (not the C# extractor): DSR collision hkx are Havok 2015 TAG0 tagfiles
holding a CustomParamStorageExtendedMeshShape (uncompressed triangle storage). No
C# SoulsFormats/HKX2/HKLib NuGet parses DSR 2015 tagfiles; soulstruct-havok
(Grimrukh) does, and V-HACD (vhacdx) lives in Python anyway.

Decomposition methods (`--method`):
  components (default) -- Route A refined for FromSoft collision, which is authored
      as many small near-planar patches. Weld coincident verts, split into connected
      components; each near-planar/small component becomes ONE convex hull (a thin
      slab = exact coverage, no bridged concavity); genuinely non-planar components
      are V-HACD'd. Keeps hull count low with full surface coverage.
  vhacd -- Plain whole-mesh V-HACD. NOTE: DS collision are thin OPEN shells (near
      zero enclosed volume) so V-HACD saturates at maxConvexHulls (natural count
      100+); capping loses coverage. Kept for comparison / genuinely solid pieces.

Setup (offline-reproducible, see extractor/README.md "hkx-extract / hulls"):
    python3 -m venv venv && . venv/bin/activate
    git clone https://github.com/Grimrukh/soulstruct
    git clone https://github.com/Grimrukh/soulstruct-havok
    pip install -e ./soulstruct && pip install --no-deps -e ./soulstruct-havok
    pip install numpy scipy colorama networkx vhacdx trimesh

Usage:
    python collision_hulls.py <piece.hkx> <outdir> [options]
      --method {components,vhacd}  (default components)
      --name NAME          output stem (default: input filename stem)
      --max-hulls N        cap; components method warns if exceeded (default 63)
      --planar-thresh M    max off-plane deviation (m) to treat a component as one
                           slab; above it the component is V-HACD'd (default 0.6)
      --resolution N       V-HACD voxel resolution (default 100000)
      --vol-error F        V-HACD minimumVolumePercentErrorAllowed (default 5.0)
      --max-verts N        V-HACD/hull maxNumVerticesPerCH (default 64; hard <256)
      --weld-digits N      vertex-weld decimal places (default 4 => 0.1mm)
      --no-mesh-json       skip writing the raw <name>.collision.json
"""
from __future__ import annotations

import argparse
import json
import sys
import warnings
from pathlib import Path

import numpy as np

warnings.filterwarnings("ignore", category=RuntimeWarning)  # trimesh vol on flat hulls


def load_collision_mesh(hkx_path: Path):
    """Return (vertices Nx3 float64, faces Mx3 int, submesh summary list)."""
    from soulstruct.havok.core import HKX
    from soulstruct.havok.fromsoft.shared.map_collision import MapCollisionModel

    hkx = HKX.from_path(str(hkx_path))
    if str(hkx.hk_version) != "20150100":
        print(f"WARNING: unexpected Havok version {hkx.hk_version} "
              f"(validated on DSR 2015 '20150100')", file=sys.stderr)
    model = MapCollisionModel.from_hkx(hkx)

    verts_parts, faces_parts, offset = [], [], 0
    summary = []
    for m in model.meshes:
        v = np.asarray(m.vertices3D, dtype=np.float64)
        f = np.asarray(m.face_vertex_indices, dtype=np.int64)
        verts_parts.append(v)
        faces_parts.append(f + offset)
        offset += v.shape[0]
        summary.append((int(m.vertex_count), int(m.face_count), int(m.material_index)))
    return np.concatenate(verts_parts), np.concatenate(faces_parts), summary


def _planar_deviation(points: np.ndarray) -> float:
    """Max perpendicular distance (m) of points from their best-fit plane."""
    if len(points) < 3:
        return 0.0
    ctr = points.mean(0)
    _, _, vt = np.linalg.svd(points - ctr, full_matrices=False)
    return float(np.abs((points - ctr) @ vt[2]).max())


def _vhacd(vertices, faces, max_hulls, resolution, vol_error, max_verts):
    import vhacdx
    return vhacdx.compute_vhacd(
        np.ascontiguousarray(vertices, dtype=np.float64),
        np.ascontiguousarray(faces, dtype=np.uint32),
        maxConvexHulls=max_hulls, resolution=resolution,
        minimumVolumePercentErrorAllowed=vol_error,
        maxNumVerticesPerCH=max_verts, shrinkWrap=True, fillMode="flood",
    )


def decompose_components(mesh, args):
    """Weld + per-connected-component: planar/tiny -> 1 convex hull; else V-HACD."""
    import trimesh
    mesh = mesh.copy()
    mesh.merge_vertices(digits_vertex=args.weld_digits)
    comps = [c for c in mesh.split(only_watertight=False) if len(c.faces) > 0]

    hull_verts = []  # list of Kx3 arrays
    n_planar = n_split = 0
    for c in comps:
        if len(c.faces) < 6 or _planar_deviation(c.vertices) <= args.planar_thresh:
            try:
                hv = np.asarray(c.convex_hull.vertices, dtype=np.float64)
            except Exception:
                # Degenerate component (collinear / <4 pts): pass raw verts through;
                # the NIF writer can thicken a sliver. Negligible collision area.
                hv = np.unique(np.asarray(c.vertices, dtype=np.float64), axis=0)
            hull_verts.append(hv)
            n_planar += 1
        else:
            # Non-planar patch: subdivide with a small cap so total stays bounded.
            for hv, _ in _vhacd(c.vertices, c.faces, 4, args.resolution,
                                args.vol_error, args.max_verts):
                hull_verts.append(np.asarray(hv, dtype=np.float64))
            n_split += 1
    print(f"components     : {len(comps)} (planar/tiny={n_planar}, vhacd-split={n_split})")
    return hull_verts


def decompose_vhacd(mesh, args):
    hulls = _vhacd(mesh.vertices, mesh.faces, min(args.max_hulls, 64),
                   args.resolution, args.vol_error, args.max_verts)
    return [np.asarray(hv, dtype=np.float64) for hv, _ in hulls]


def _hull_equations(hv):
    """scipy ConvexHull face half-spaces (n_faces x 4: [a,b,c,d], a*x+..+d<=0 inside).
    QJ joggles so coplanar/thin hulls don't blow up Qhull. Returns None on failure."""
    from scipy.spatial import ConvexHull, QhullError
    try:
        return ConvexHull(hv, qhull_options="QJ").equations
    except (QhullError, ValueError):
        return None


def coverage(mesh, hull_verts, eps=0.05):
    """Fraction of mesh vertices within eps (m) of the union of hulls (0 => inside)."""
    pts = mesh.vertices
    best = np.full(len(pts), np.inf)
    for hv in hull_verts:
        if len(hv) < 4:
            continue
        eqs = _hull_equations(hv)
        if eqs is None:
            continue
        # outside distance ~= max over faces of (n.p + d), clamped at 0.
        d = np.maximum((pts @ eqs[:, :3].T + eqs[:, 3]).max(axis=1), 0.0)
        best = np.minimum(best, d)
    return float((best <= eps).mean())


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("hkx", type=Path)
    ap.add_argument("outdir", type=Path)
    ap.add_argument("--method", choices=["components", "vhacd"], default="components")
    ap.add_argument("--name")
    ap.add_argument("--max-hulls", type=int, default=63)
    ap.add_argument("--planar-thresh", type=float, default=1.5)
    ap.add_argument("--resolution", type=int, default=100_000)
    ap.add_argument("--vol-error", type=float, default=5.0)
    ap.add_argument("--max-verts", type=int, default=64)
    ap.add_argument("--weld-digits", type=int, default=4)
    ap.add_argument("--no-mesh-json", action="store_true")
    args = ap.parse_args()

    import trimesh

    name = args.name or args.hkx.name
    for ext in (".hkx.dcx", ".hkx", ".dcx"):
        if name.endswith(ext):
            name = name[: -len(ext)]
            break
    args.outdir.mkdir(parents=True, exist_ok=True)

    vertices, faces, submeshes = load_collision_mesh(args.hkx)
    mesh = trimesh.Trimesh(vertices, faces, process=False)
    bbox = mesh.extents
    print(f"input hkx      : {args.hkx}")
    print(f"submeshes      : {len(submeshes)}  "
          + "  ".join(f"[v{v} f{f} mat{m}]" for v, f, m in submeshes))
    print(f"merged mesh    : verts={vertices.shape[0]} faces={faces.shape[0]}")
    print(f"bbox (metres)  : {bbox.round(3).tolist()}")

    if not args.no_mesh_json:
        mj = args.outdir / f"{name}.collision.json"
        mj.write_text(json.dumps({
            "source": args.hkx.name, "space": "DS-native metres, Y-up",
            "vertexCount": int(vertices.shape[0]), "faceCount": int(faces.shape[0]),
            "submeshes": [{"vertices": v, "faces": f, "material": m}
                          for v, f, m in submeshes],
            "vertices": np.round(vertices, 6).tolist(),
            "faces": faces.astype(int).tolist(),
        }))
        print(f"wrote mesh     : {mj}")

    print(f"method         : {args.method}  (resolution={args.resolution} "
          f"volError={args.vol_error} maxVerts={args.max_verts})")
    if args.method == "components":
        hull_verts = decompose_components(mesh, args)
    else:
        hull_verts = decompose_vhacd(mesh, args)

    # --- validate: every point of a hull must be an extreme point of its own
    # convex hull (=> the hull is genuinely convex, no interior verts). ---
    from scipy.spatial import ConvexHull
    vert_counts = [len(hv) for hv in hull_verts]
    non_convex = degenerate = 0
    for hv in hull_verts:
        if len(hv) < 4:
            degenerate += 1
            continue
        eqs = _hull_equations(hv)
        if eqs is None:  # Qhull can't build => coplanar/degenerate
            degenerate += 1
            continue
        try:
            extreme = len(ConvexHull(hv).vertices)  # strict (no QJ)
            if extreme < len(hv) - 1:               # interior points present
                non_convex += 1
        except Exception:
            degenerate += 1
    cov = coverage(mesh, hull_verts)

    out_hulls = [{"vertices": np.round(hv, 6).tolist()} for hv in hull_verts]
    hj = args.outdir / f"{name}.hulls.json"
    hj.write_text(json.dumps({"hulls": out_hulls}))

    print(f"hulls          : {len(out_hulls)}")
    print(f"hull verts     : min={min(vert_counts)} max={max(vert_counts)} "
          f"mean={sum(vert_counts)/len(vert_counts):.1f}")
    print(f"coverage       : {cov*100:.1f}% of mesh verts within 5cm of a hull")
    print(f"convexity      : {len(out_hulls)-non_convex}/{len(out_hulls)} convex, "
          f"{degenerate} near-degenerate (thin/flat)")
    print(f"wrote hulls    : {hj}")

    ok = True
    if len(out_hulls) > args.max_hulls:
        print(f"!! hull count {len(out_hulls)} > {args.max_hulls} -- raise "
              f"--planar-thresh (components) or --vol-error", file=sys.stderr); ok = False
    if max(vert_counts) >= 256:
        print(f"!! a hull has {max(vert_counts)} verts (>=256)", file=sys.stderr); ok = False
    if non_convex:
        print(f"!! {non_convex} hull(s) not convex", file=sys.stderr); ok = False
    return 0 if ok else 3


if __name__ == "__main__":
    raise SystemExit(main())
