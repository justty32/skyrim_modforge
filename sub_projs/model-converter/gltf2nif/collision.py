"""Collision: hulls JSON -> per-hull convex data ready for bhkConvexVerticesShape.

Input JSON (metres, DarkSouls native Y-up, same handedness as glTF):
    {"hulls": [ {"vertices": [[x,y,z], ...]}, ... ]}

bhk shapes live in Havok *metres* (Skyrim units / 69.99), and DarkSouls is already
metres, so hull vertices get the axis swap ONLY (Y-up -> Z-up), never the *70 render
scale. Each hull yields Vector4 vertices (w=0) and Vector4 half-space planes
(nx,ny,nz, d) computed from the convex hull faces.
"""

from __future__ import annotations

import json

import numpy as np

from ._binwriter import GltfError
from .geometry import convex_hull_planes, gltf_to_skyrim_dir


class Hull:
    __slots__ = ("vertices", "planes")

    def __init__(self, vertices: np.ndarray, planes: list[tuple[np.ndarray, float]]):
        self.vertices = vertices                    # Nx3 float (Havok metres, Z-up)
        self.planes = planes                        # list of (unit normal Z-up, d)


def load_hulls(path: str) -> list[Hull]:
    """Parse hulls JSON, axis-swap to Z-up (no scale), compute half-spaces per hull."""
    with open(path, "r", encoding="utf-8") as fh:
        doc = json.load(fh)
    raw = doc.get("hulls")
    if not isinstance(raw, list):
        raise GltfError("hulls JSON: missing 'hulls' list")
    hulls: list[Hull] = []
    for h in raw:
        pts = h.get("vertices") if isinstance(h, dict) else None
        if not pts:
            continue
        # Axis swap only (metres in, metres out): reuse the direction transform since it
        # is the pure (x,y,z)->(x,-z,y) rotation with no unit scaling.
        verts = np.array([gltf_to_skyrim_dir(float(x), float(y), float(z)) for x, y, z in pts],
                         dtype=np.float64)
        planes = convex_hull_planes(verts)
        if len(verts) >= 4 and planes:
            hulls.append(Hull(verts, planes))
    if not hulls:
        raise GltfError("hulls JSON: no usable convex hulls (need >=4 non-coplanar verts each)")
    return hulls
