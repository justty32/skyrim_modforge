"""Hand-rolled Skyrim static-mesh NIF reader -> list[Mesh] (glTF space).

Scope (MVP): NIF version 20.2.0.7, User Version 12 only (Skyrim LE bs=83 / SSE
bs>=100). Extracts triangle geometry from:
  - LE: NiTriShape / NiTriStrips -> NiTriShapeData / NiTriStripsData
  - SSE: BSTriShape / BSDynamicTriShape / BSSubIndexTriShape (BSVertexData)
Node transforms (NiNode-derived) are composed so multi-part shapes sit correctly;
geometry is converted Skyrim(Z-up) -> glTF(Y-up). Skinned/animated shapes are
rejected (SkinnedNifError -> CLI exit 3).

Robustness: Block Sizes (present since 20.2.0.5) give every block's start offset,
so after parsing each block we seek to the known next boundary — a parse drift in
one block cannot desync the rest.

Layouts taken verbatim from niftools/nifxml nif.xml (see _nifxml_ref.xml is the
downloaded reference; not committed). SSE vertex decode uses the BSVertexDesc
offset table + Full_Precision flag (the self-describing method), which is more
robust than sequential field parsing. Real vanilla-NIF byte validation is deferred
to the main machine (no game assets offline).
"""

from __future__ import annotations

import struct

import numpy as np

from .geometry import Mesh, skyrim_to_gltf_dir, skyrim_to_gltf_point

SKYRIM_VERSION = 0x14020007
SKYRIM_USER_VERSION = 12

# NiNode-derived grouping types we know carry [transform][children] in NiNode layout.
NODE_TYPES = {
    "NiNode", "BSFadeNode", "BSLeafAnimNode", "BSOrderedNode", "BSMultiBoundNode",
    "BSValueNode", "NiBillboardNode", "NiSwitchNode", "BSTreeNode", "BSBlastNode",
    "BSDamageStage", "BSMasterParticleSystem", "BSDebrisNode", "BSRangeNode",
}
LE_SHAPE_TYPES = {"NiTriShape", "NiTriStrips"}
SSE_SHAPE_TYPES = {"BSTriShape", "BSDynamicTriShape", "BSSubIndexTriShape", "BSMeshLODTriShape", "BSLODTriShape"}
LE_DATA_TYPES = {"NiTriShapeData", "NiTriStripsData"}


class NifError(Exception):
    """Unparseable / unsupported NIF -> CLI exit 2."""


class SkinnedNifError(Exception):
    """Contains skin/animation; MVP static backend rejects -> CLI exit 3."""


class _Reader:
    __slots__ = ("d", "p")

    def __init__(self, data: bytes):
        self.d = data
        self.p = 0

    def seek(self, pos: int) -> None:
        self.p = pos

    def take(self, n: int) -> bytes:
        if self.p + n > len(self.d):
            raise NifError("unexpected end of file")
        b = self.d[self.p:self.p + n]
        self.p += n
        return b

    def u8(self) -> int:
        return self.take(1)[0]

    def u16(self) -> int:
        return struct.unpack_from("<H", self.take(2))[0]

    def u32(self) -> int:
        return struct.unpack_from("<I", self.take(4))[0]

    def i32(self) -> int:
        return struct.unpack_from("<i", self.take(4))[0]

    def u64(self) -> int:
        return struct.unpack_from("<Q", self.take(8))[0]

    def f32(self) -> float:
        return struct.unpack_from("<f", self.take(4))[0]

    def vec3(self) -> tuple[float, float, float]:
        return struct.unpack_from("<3f", self.take(12))

    def mat33(self) -> np.ndarray:
        # nif.xml: stored column-major m11,m21,m31, m12,m22,m32, m13,m23,m33.
        m = struct.unpack_from("<9f", self.take(36))
        return np.array(
            [[m[0], m[3], m[6]], [m[1], m[4], m[7]], [m[2], m[5], m[8]]],
            dtype=np.float64,
        )

    def line(self) -> str:
        start = self.p
        while self.p < len(self.d) and self.d[self.p] != 0x0A:
            self.p += 1
        s = self.d[start:self.p]
        self.p += 1  # consume newline
        return s.decode("latin-1")

    def sized_string(self) -> str:
        n = self.u32()
        return self.take(n).decode("latin-1")

    def export_string(self) -> str:
        n = self.u8()
        return self.take(n).rstrip(b"\x00").decode("latin-1")


def _local_matrix(trans, rot: np.ndarray, scale: float) -> np.ndarray:
    m = np.eye(4, dtype=np.float64)
    m[:3, :3] = rot * scale
    m[:3, 3] = trans
    return m


def _read_header(r: _Reader) -> dict:
    header_string = r.line()
    version = r.u32()
    if version != SKYRIM_VERSION:
        raise NifError(f"unsupported NIF version 0x{version:08X} (need Skyrim 0x14020007)")
    endian = r.u8()
    if endian != 1:
        raise NifError("big-endian NIF not supported")
    user_version = r.u32()
    if user_version != SKYRIM_USER_VERSION:
        raise NifError(f"unsupported User Version {user_version} (need 12)")
    num_blocks = r.u32()

    # BSStreamHeader (Bethesda). Always present for Skyrim user version 12.
    bs_version = r.u32()
    r.export_string()  # Author
    if bs_version > 130:
        r.u32()  # Unknown Int
    if bs_version < 131:
        r.export_string()  # Process Script
    r.export_string()  # Export Script
    if bs_version >= 103:
        r.export_string()  # Max Filepath

    num_block_types = r.u16()
    block_types = [r.sized_string() for _ in range(num_block_types)]
    block_type_index = [r.u16() for _ in range(num_blocks)]
    block_sizes = [r.u32() for _ in range(num_blocks)]  # since 20.2.0.5
    num_strings = r.u32()
    r.u32()  # Max String Length
    strings = [r.sized_string() for _ in range(num_strings)]
    num_groups = r.u32()
    for _ in range(num_groups):
        r.u32()

    types_per_block = [block_types[idx] for idx in block_type_index]
    offsets = []
    cur = r.p
    for sz in block_sizes:
        offsets.append(cur)
        cur += sz
    return {
        "header_string": header_string,
        "bs_version": bs_version,
        "num_blocks": num_blocks,
        "types": types_per_block,
        "block_sizes": block_sizes,
        "offsets": offsets,
        "strings": strings,
    }


def _skip_niobjectnet(r: _Reader) -> None:
    r.u32()  # Name (string index)
    num_extra = r.u32()
    for _ in range(num_extra):
        r.i32()  # Extra Data refs
    r.i32()  # Controller ref


def _read_niavobject(r: _Reader):
    """Read NiObjectNET + NiAVObject prefix; return (trans, rot, scale)."""
    _skip_niobjectnet(r)
    r.u32()  # Flags (uint for BSVER > 26)
    trans = r.vec3()
    rot = r.mat33()
    scale = r.f32()
    r.i32()  # Collision Object ref
    return trans, rot, scale


def _read_ninode(r: _Reader) -> dict:
    trans, rot, scale = _read_niavobject(r)
    num_children = r.u32()
    children = [r.i32() for _ in range(num_children)]
    return {"kind": "node", "local": _local_matrix(trans, rot, scale), "children": children}


def _read_le_shape(r: _Reader) -> dict:
    trans, rot, scale = _read_niavobject(r)
    data_ref = r.i32()       # NiGeometry.Data
    skin_ref = r.i32()       # NiGeometry.Skin Instance
    return {
        "kind": "le_shape",
        "local": _local_matrix(trans, rot, scale),
        "data_ref": data_ref,
        "skinned": skin_ref != -1,
    }


def _read_nitrishapedata(r: _Reader) -> dict:
    r.i32()                       # Group ID
    num_vertices = r.u16()
    r.u8()                        # Keep Flags
    r.u8()                        # Compress Flags
    has_vertices = r.u8()
    verts = []
    if has_vertices:
        for _ in range(num_vertices):
            verts.append(r.vec3())
    bs_data_flags = r.u16()       # BS Data Flags (BS202)
    r.u32()                       # Material CRC
    has_normals = r.u8()
    normals = []
    if has_normals:
        for _ in range(num_vertices):
            normals.append(r.vec3())
        if bs_data_flags & 0x1000:  # has tangents
            for _ in range(num_vertices * 2):  # tangents + bitangents
                r.vec3()
    r.vec3(); r.f32()             # Bounding Sphere (NiBound)
    has_vertex_colors = r.u8()
    if has_vertex_colors:
        for _ in range(num_vertices):
            r.take(16)            # Color4
    num_uv_sets = bs_data_flags & 0x1
    uvs = []
    for s in range(num_uv_sets):
        for v in range(num_vertices):
            uv = struct.unpack_from("<2f", r.take(8))
            if s == 0:
                uvs.append(uv)
    r.u16()                       # Consistency Flags
    r.i32()                       # Additional Data ref
    num_triangles = r.u16()       # NiTriBasedGeomData
    r.u32()                       # Num Triangle Points
    has_triangles = r.u8()
    tris = []
    if has_triangles:
        for _ in range(num_triangles):
            tris.append(struct.unpack_from("<3H", r.take(6)))
    return {
        "kind": "data",
        "verts": verts,
        "normals": normals,
        "uvs": uvs,
        "tris": tris,
    }


def _read_nitristripsdata(r: _Reader) -> dict:
    r.i32()                       # Group ID
    num_vertices = r.u16()
    r.u8(); r.u8()                # Keep / Compress Flags
    has_vertices = r.u8()
    verts = []
    if has_vertices:
        for _ in range(num_vertices):
            verts.append(r.vec3())
    bs_data_flags = r.u16()
    r.u32()                       # Material CRC
    has_normals = r.u8()
    normals = []
    if has_normals:
        for _ in range(num_vertices):
            normals.append(r.vec3())
        if bs_data_flags & 0x1000:
            for _ in range(num_vertices * 2):
                r.vec3()
    r.vec3(); r.f32()             # Bounding Sphere
    has_vertex_colors = r.u8()
    if has_vertex_colors:
        for _ in range(num_vertices):
            r.take(16)
    num_uv_sets = bs_data_flags & 0x1
    uvs = []
    for s in range(num_uv_sets):
        for v in range(num_vertices):
            uv = struct.unpack_from("<2f", r.take(8))
            if s == 0:
                uvs.append(uv)
    r.u16()                       # Consistency Flags
    r.i32()                       # Additional Data ref
    r.u16()                       # Num Triangles (NiTriBasedGeomData)
    num_strips = r.u16()
    strip_lengths = [r.u16() for _ in range(num_strips)]
    has_points = r.u8()
    tris = []
    if has_points:
        for length in strip_lengths:
            strip = [r.u16() for _ in range(length)]
            tris.extend(_destrip(strip))
    return {"kind": "data", "verts": verts, "normals": normals, "uvs": uvs, "tris": tris}


def _destrip(strip: list[int]) -> list[tuple[int, int, int]]:
    out = []
    for i in range(len(strip) - 2):
        a, b, c = strip[i], strip[i + 1], strip[i + 2]
        if a == b or b == c or a == c:
            continue  # degenerate (strip restart)
        if i % 2 == 0:
            out.append((a, b, c))
        else:
            out.append((a, c, b))
    return out


def _decode_half3(buf: bytes, off: int) -> tuple[float, float, float]:
    return tuple(np.frombuffer(buf, dtype="<f2", count=3, offset=off).astype(np.float64))


def _decode_float3(buf: bytes, off: int) -> tuple[float, float, float]:
    return struct.unpack_from("<3f", buf, off)


def _decode_half2(buf: bytes, off: int) -> tuple[float, float]:
    return tuple(np.frombuffer(buf, dtype="<f2", count=2, offset=off).astype(np.float64))


def _read_bstrishape(r: _Reader, bs_version: int) -> dict:
    """SSE BSTriShape via BSVertexDesc offset table (self-describing decode)."""
    trans, rot, scale = _read_niavobject(r)
    r.vec3(); r.f32()             # Bounding Sphere
    if bs_version >= 155:         # F76 Bound Min Max
        for _ in range(6):
            r.f32()
    skin_ref = r.i32()
    r.i32()                       # Shader Property
    r.i32()                       # Alpha Property
    vertex_desc = r.u64()
    if bs_version >= 130:         # FO4+: uint triangle count
        num_triangles = r.u32()
    else:                         # SSE: ushort
        num_triangles = r.u16()
    num_vertices = r.u16()
    data_size = r.u32()

    stride = (vertex_desc & 0xF) * 4
    attributes = (vertex_desc >> 44) & 0xFFF
    has_vertex = attributes & 0x1
    has_uv = attributes & 0x2
    has_normal = attributes & 0x8
    skinned = bool(attributes & 0x40) or skin_ref != -1
    full_precision = attributes & 0x400
    uv_off = ((vertex_desc >> 8) & 0xF) * 4
    nrm_off = ((vertex_desc >> 16) & 0xF) * 4

    verts: list = []
    normals: list = []
    uvs: list = []
    tris: list = []
    if data_size > 0 and stride > 0:
        vbytes = r.take(stride * num_vertices)
        for v in range(num_vertices):
            base = v * stride
            if has_vertex:
                if full_precision:
                    verts.append(_decode_float3(vbytes, base))
                else:
                    verts.append(_decode_half3(vbytes, base))
            if has_uv:
                uvs.append(_decode_half2(vbytes, base + uv_off))
            if has_normal:
                nx = vbytes[base + nrm_off] / 255.0 * 2.0 - 1.0
                ny = vbytes[base + nrm_off + 1] / 255.0 * 2.0 - 1.0
                nz = vbytes[base + nrm_off + 2] / 255.0 * 2.0 - 1.0
                normals.append((nx, ny, nz))
        for _ in range(num_triangles):
            tris.append(struct.unpack_from("<3H", r.take(6)))

    return {
        "kind": "sse_shape",
        "local": _local_matrix(trans, rot, scale),
        "skinned": skinned,
        "verts": verts,
        "normals": normals if len(normals) == len(verts) else [],
        "uvs": uvs if len(uvs) == len(verts) else [],
        "tris": tris,
    }


def _world_matrix(idx: int, parent: dict, local: dict) -> np.ndarray:
    chain = []
    cur = idx
    seen = set()
    while cur is not None and cur not in seen:
        seen.add(cur)
        chain.append(cur)
        cur = parent.get(cur)
    m = np.eye(4, dtype=np.float64)
    for i in reversed(chain):
        if i in local:
            m = m @ local[i]
    return m


def _assemble_mesh(name: str, world: np.ndarray, verts, normals, uvs, tris) -> Mesh:
    rot_scale = world[:3, :3]
    out_pos = []
    for (x, y, z) in verts:
        wx, wy, wz = (world @ np.array([x, y, z, 1.0]))[:3]
        out_pos.append(skyrim_to_gltf_point(float(wx), float(wy), float(wz)))
    out_nrm = []
    for (x, y, z) in normals:
        nx, ny, nz = rot_scale @ np.array([x, y, z], dtype=np.float64)
        n = np.array([nx, ny, nz])
        ln = np.linalg.norm(n)
        if ln > 1e-9:
            n = n / ln
        out_nrm.append(skyrim_to_gltf_dir(float(n[0]), float(n[1]), float(n[2])))
    return Mesh(name=name, positions=out_pos, normals=out_nrm, uvs=list(uvs), triangles=list(tris))


def read_nif(data: bytes) -> list[Mesh]:
    """Parse NIF bytes -> list[Mesh] in glTF space. Raises NifError / SkinnedNifError."""
    r = _Reader(data)
    hdr = _read_header(r)
    types = hdr["types"]
    offsets = hdr["offsets"]
    strings = hdr["strings"]
    bs_version = hdr["bs_version"]

    blocks: dict[int, dict] = {}
    for i in range(hdr["num_blocks"]):
        r.seek(offsets[i])
        t = types[i]
        try:
            if t in NODE_TYPES:
                blocks[i] = _read_ninode(r)
            elif t in LE_SHAPE_TYPES:
                blocks[i] = _read_le_shape(r)
            elif t == "NiTriShapeData":
                blocks[i] = _read_nitrishapedata(r)
            elif t == "NiTriStripsData":
                blocks[i] = _read_nitristripsdata(r)
            elif t in SSE_SHAPE_TYPES:
                blocks[i] = _read_bstrishape(r, bs_version)
        except NifError:
            # Defensive: a malformed sub-block is skipped, not fatal (boundary seek recovers).
            blocks.pop(i, None)

    # Build node hierarchy (child -> parent) and per-block local matrices.
    parent: dict[int, int] = {}
    local: dict[int, np.ndarray] = {}
    for i, b in blocks.items():
        if "local" in b:
            local[i] = b["local"]
        if b.get("kind") == "node":
            for c in b["children"]:
                if c >= 0:
                    parent[c] = i

    meshes: list[Mesh] = []
    skinned_seen = False
    for i, b in blocks.items():
        kind = b.get("kind")
        if kind == "le_shape":
            if b["skinned"]:
                skinned_seen = True
                continue
            data = blocks.get(b["data_ref"])
            if not data or data.get("kind") != "data" or not data["verts"]:
                continue
            world = _world_matrix(i, parent, local)
            meshes.append(_assemble_mesh(
                f"shape_{i}", world, data["verts"], data["normals"], data["uvs"], data["tris"]))
        elif kind == "sse_shape":
            if b["skinned"]:
                skinned_seen = True
                continue
            if not b["verts"]:
                continue
            world = _world_matrix(i, parent, local)
            meshes.append(_assemble_mesh(
                f"shape_{i}", world, b["verts"], b["normals"], b["uvs"], b["tris"]))

    if not meshes:
        if skinned_seen:
            raise SkinnedNifError("NIF contains only skinned/animated geometry (static backend)")
        raise NifError("no static triangle geometry found")
    return meshes
