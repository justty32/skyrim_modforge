"""Exact 90-degree-quantised geometry for the prefab grammar spike.

Every rotation in this spike is a multiple of 90 degrees about Z. That keeps the
rotation matrix entries in {-1, 0, 1}, so rotating a coordinate is an exact float
operation (sign flips and axis swaps only) with no trigonometric drift. Byte
identical output for a fixed seed depends on that property.

Coordinate conventions follow ModForge, not Mundusform:
  * Skyrim game units, right-handed, +Z up.
  * Angles in DEGREES about Z (see ModForge PlacementSpec.Rotation / NavCutSpec.RotationZ).
  * A bounding box is CENTRE + FULL size (w, d, h), exactly like NavCutSpec.Position/Size.
    Mundusform instead used a min-corner plus width/height 2D rect; we do not copy that.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Sequence, Tuple

Vec3 = Tuple[float, float, float]

# Face-to-face contact must not read as an overlap, so the AABB test is strict
# and shrinks each box by this much before comparing.
EPSILON = 1e-6

QUANTISED_YAWS = (0.0, 90.0, 180.0, 270.0)


def canon(value: float) -> float:
    """Canonicalise a float for output: round to 6 dp and kill negative zero."""
    result = round(float(value) + 0.0, 6)
    return result + 0.0 if result != 0.0 else 0.0


def canon_vec(vec: Sequence[float]) -> Vec3:
    if len(vec) != 3:
        raise ValueError(f"expected 3 components, got {len(vec)}")
    return (canon(vec[0]), canon(vec[1]), canon(vec[2]))


def norm_angle(degrees: float) -> float:
    """Normalise an angle to [0, 360) and canonicalise it."""
    return canon(float(degrees) % 360.0)


def quantise_yaw(degrees: float) -> float:
    """Normalise to [0, 360) and require a multiple of 90."""
    angle = norm_angle(degrees)
    if angle not in QUANTISED_YAWS:
        raise ValueError(f"yaw must be a multiple of 90 degrees, got {degrees!r}")
    return angle


def rotate_xy(point: Sequence[float], yaw: float) -> Vec3:
    """Rotate a point about the local origin by `yaw` degrees around +Z.

    Exact: only multiples of 90 are accepted, so this is sign flips and swaps.
    """
    angle = quantise_yaw(yaw)
    x, y, z = float(point[0]), float(point[1]), float(point[2])
    if angle == 0.0:
        rx, ry = x, y
    elif angle == 90.0:
        rx, ry = -y, x
    elif angle == 180.0:
        rx, ry = -x, -y
    else:  # 270.0
        rx, ry = y, -x
    return (canon(rx), canon(ry), canon(z))


def rotate_size(size: Sequence[float], yaw: float) -> Vec3:
    """A yaw of 90 or 270 swaps the box's width and depth; height never changes."""
    angle = quantise_yaw(yaw)
    w, d, h = float(size[0]), float(size[1]), float(size[2])
    if angle in (90.0, 270.0):
        w, d = d, w
    return (canon(w), canon(d), canon(h))


def add(a: Sequence[float], b: Sequence[float]) -> Vec3:
    return (canon(a[0] + b[0]), canon(a[1] + b[1]), canon(a[2] + b[2]))


def sub(a: Sequence[float], b: Sequence[float]) -> Vec3:
    return (canon(a[0] - b[0]), canon(a[1] - b[1]), canon(a[2] - b[2]))


@dataclass(frozen=True)
class Aabb:
    """Axis-aligned box, CENTRE + FULL size -- the ModForge NavCutSpec convention."""

    center: Vec3
    size: Vec3

    @staticmethod
    def of(center: Sequence[float], size: Sequence[float]) -> "Aabb":
        c = canon_vec(center)
        s = canon_vec(size)
        if min(s) <= 0.0:
            raise ValueError(f"bounding box size must be positive on all axes, got {s}")
        return Aabb(c, s)

    def min_corner(self) -> Vec3:
        return (
            canon(self.center[0] - self.size[0] / 2.0),
            canon(self.center[1] - self.size[1] / 2.0),
            canon(self.center[2] - self.size[2] / 2.0),
        )

    def max_corner(self) -> Vec3:
        return (
            canon(self.center[0] + self.size[0] / 2.0),
            canon(self.center[1] + self.size[1] / 2.0),
            canon(self.center[2] + self.size[2] / 2.0),
        )

    def transformed(self, yaw: float, offset: Sequence[float]) -> "Aabb":
        """Rotate about the local origin, then translate."""
        return Aabb(add(rotate_xy(self.center, yaw), offset), rotate_size(self.size, yaw))

    def overlaps(self, other: "Aabb") -> bool:
        """True iff the two boxes share interior volume on ALL THREE axes.

        Touching faces are not an overlap -- adjacent corridor segments must be
        allowed to share a wall plane. Mundusform only tested X/Y, so it could not
        stack floors; this spike tests Z as well.
        """
        a_min, a_max = self.min_corner(), self.max_corner()
        b_min, b_max = other.min_corner(), other.max_corner()
        for axis in range(3):
            if a_min[axis] >= b_max[axis] - EPSILON:
                return False
            if b_min[axis] >= a_max[axis] - EPSILON:
                return False
        return True


def any_overlap(box: Aabb, placed: Sequence[Aabb]) -> bool:
    return any(box.overlaps(other) for other in placed)
