"""Prefab / layout schema for the prefab grammar spike (stdlib only, fail-closed).

This module is the CONTRACT the rest of the spike is written against: the prefab
JSON shape, the layout JSON shape, and the canonical serialisation that makes
"same seed -> byte-identical output" testable.

Prefab JSON (one file per prefab, under data/prefabs/):

    {
      "schemaVersion": 1,
      "id": "corridor_straight",
      "kind": "hall",                       # entrance | hall | room | cap
      "boundingBox": {"center": [0,0,128], "size": [512,256,256]},
      "connectors": [
        {"id": "in",  "role": "entrance", "type": "hall2", "position": [-256,0,0], "facing": 180},
        {"id": "out", "role": "exit",     "type": "hall2", "position": [ 256,0,0], "facing": 0}
      ],
      "placements": [
        {"base": "Skyrim.esm:0x0551C0", "editorId": "", "position": [0,0,0],
         "rotation": [0,0,0], "scale": 1.0}
      ]
    }

`facing` is the OUTWARD normal of the socket in degrees about +Z
(0 = +X, 90 = +Y, 180 = -X, 270 = -Y) and must be a multiple of 90.
Two connectors mate iff their `type` strings are equal, their world positions are
equal, and their world facings differ by exactly 180 degrees.

Kind rules (enforced here, not in the generator):
  entrance -> 0 connectors with role "entrance", >= 1 with role "exit"
  hall/room-> exactly 1 "entrance", >= 1 "exit"
  cap      -> exactly 1 "entrance", 0 "exit"
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping, Tuple

from prefab_grammar.geometry import Aabb, Vec3, canon, canon_vec, norm_angle, quantise_yaw

PREFAB_SCHEMA_VERSION = 1
LAYOUT_SCHEMA_VERSION = 1
GENERATOR_ID = "prefab_grammar/0.1"

PREFAB_KINDS = ("entrance", "hall", "room", "cap")
CONNECTOR_ROLES = ("entrance", "exit")


class SchemaError(ValueError):
    """A prefab or layout document violated the contract. Always fail closed."""


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise SchemaError(message)


def _field(obj: Any, key: str, where: str) -> Any:
    _require(isinstance(obj, dict), f"{where}: expected an object, got {type(obj).__name__}")
    _require(key in obj, f"{where}: missing required field '{key}'")
    return obj[key]


def _number(raw: Any, where: str) -> float:
    _require(
        isinstance(raw, (int, float)) and not isinstance(raw, bool),
        f"{where}: expected a number, got {raw!r}",
    )
    return float(raw)


def _vec3(raw: Any, where: str) -> Vec3:
    _require(
        isinstance(raw, (list, tuple)) and len(raw) == 3,
        f"{where}: expected a 3-element array, got {raw!r}",
    )
    for component in raw:
        _number(component, where)
    return canon_vec(raw)


@dataclass(frozen=True)
class Connector:
    id: str
    role: str
    type: str
    position: Vec3
    facing: float


@dataclass(frozen=True)
class PlacementDef:
    base: str
    editor_id: str
    position: Vec3
    rotation: Vec3
    scale: float


@dataclass(frozen=True)
class Prefab:
    id: str
    kind: str
    bbox: Aabb
    connectors: Tuple[Connector, ...]
    placements: Tuple[PlacementDef, ...]

    def entrance_connector(self) -> Connector:
        for connector in self.connectors:
            if connector.role == "entrance":
                return connector
        raise SchemaError(f"prefab '{self.id}': kind '{self.kind}' has no entrance connector")

    def exit_connectors(self) -> Tuple[Connector, ...]:
        return tuple(c for c in self.connectors if c.role == "exit")


def parse_connector(raw: Any, where: str) -> Connector:
    cid = _field(raw, "id", where)
    _require(isinstance(cid, str) and cid.strip() != "", f"{where}: 'id' must be a non-empty string")
    role = _field(raw, "role", where)
    _require(role in CONNECTOR_ROLES, f"{where}: 'role' must be one of {CONNECTOR_ROLES}, got {role!r}")
    ctype = _field(raw, "type", where)
    _require(isinstance(ctype, str) and ctype.strip() != "", f"{where}: 'type' must be a non-empty string")
    position = _vec3(_field(raw, "position", where), f"{where}.position")
    facing_raw = _number(_field(raw, "facing", where), f"{where}.facing")
    try:
        facing = quantise_yaw(facing_raw)
    except ValueError as exc:
        raise SchemaError(f"{where}: {exc}") from exc
    return Connector(cid, role, ctype, position, facing)


def parse_placement(raw: Any, where: str) -> PlacementDef:
    base = _field(raw, "base", where)
    _require(isinstance(base, str) and base.strip() != "", f"{where}: 'base' must be a non-empty string")
    _require(isinstance(raw, dict), f"{where}: expected an object")
    editor_id = raw.get("editorId", "")
    _require(isinstance(editor_id, str), f"{where}: 'editorId' must be a string")
    position = _vec3(_field(raw, "position", where), f"{where}.position")
    rotation = _vec3(raw.get("rotation", [0.0, 0.0, 0.0]), f"{where}.rotation")
    scale = _number(raw.get("scale", 1.0), f"{where}.scale")
    _require(scale > 0.0, f"{where}: 'scale' must be positive, got {scale!r}")
    rotation = (norm_angle(rotation[0]), norm_angle(rotation[1]), norm_angle(rotation[2]))
    return PlacementDef(base, editor_id, position, rotation, canon(scale))


def parse_prefab(raw: Any, *, source: str = "<prefab>") -> Prefab:
    """Validate one prefab document. Raises SchemaError on any violation."""
    version = _field(raw, "schemaVersion", source)
    _require(
        version == PREFAB_SCHEMA_VERSION,
        f"{source}: schemaVersion must be {PREFAB_SCHEMA_VERSION}, got {version!r}",
    )
    pid = _field(raw, "id", source)
    _require(isinstance(pid, str) and pid.strip() != "", f"{source}: 'id' must be a non-empty string")
    kind = _field(raw, "kind", source)
    _require(kind in PREFAB_KINDS, f"{source}: 'kind' must be one of {PREFAB_KINDS}, got {kind!r}")

    box_raw = _field(raw, "boundingBox", source)
    center = _vec3(_field(box_raw, "center", f"{source}.boundingBox"), f"{source}.boundingBox.center")
    size = _vec3(_field(box_raw, "size", f"{source}.boundingBox"), f"{source}.boundingBox.size")
    try:
        bbox = Aabb.of(center, size)
    except ValueError as exc:
        raise SchemaError(f"{source}.boundingBox: {exc}") from exc

    raw_connectors = _field(raw, "connectors", source)
    _require(isinstance(raw_connectors, list), f"{source}: 'connectors' must be an array")
    connectors = tuple(
        parse_connector(c, f"{source}.connectors[{i}]") for i, c in enumerate(raw_connectors)
    )
    ids = [c.id for c in connectors]
    _require(len(set(ids)) == len(ids), f"{source}: connector ids must be unique, got {ids}")

    entrances = [c for c in connectors if c.role == "entrance"]
    exits = [c for c in connectors if c.role == "exit"]
    if kind == "entrance":
        _require(not entrances, f"{source}: kind 'entrance' must not declare an entrance connector")
        _require(len(exits) >= 1, f"{source}: kind 'entrance' needs at least one exit connector")
    elif kind == "cap":
        _require(len(entrances) == 1, f"{source}: kind 'cap' needs exactly one entrance connector")
        _require(not exits, f"{source}: kind 'cap' must not declare exit connectors")
    else:
        _require(len(entrances) == 1, f"{source}: kind '{kind}' needs exactly one entrance connector")
        _require(len(exits) >= 1, f"{source}: kind '{kind}' needs at least one exit connector")

    raw_placements = _field(raw, "placements", source)
    _require(isinstance(raw_placements, list), f"{source}: 'placements' must be an array")
    _require(len(raw_placements) >= 1, f"{source}: 'placements' must not be empty")
    placements = tuple(
        parse_placement(p, f"{source}.placements[{i}]") for i, p in enumerate(raw_placements)
    )
    return Prefab(pid, kind, bbox, connectors, placements)


def load_prefab_file(path: Path) -> Prefab:
    path = Path(path)
    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SchemaError(f"{path.name}: not valid JSON: {exc}") from exc
    return parse_prefab(raw, source=path.name)


def load_prefab_library(directory: Path) -> Tuple[Prefab, ...]:
    """Load every *.json under `directory`, sorted by file name for determinism."""
    directory = Path(directory)
    _require(directory.is_dir(), f"prefab directory not found: {directory}")
    prefabs = tuple(load_prefab_file(p) for p in sorted(directory.glob("*.json")))
    _require(len(prefabs) > 0, f"no prefab json files found in {directory}")
    ids = [p.id for p in prefabs]
    _require(len(set(ids)) == len(ids), f"duplicate prefab ids in {directory}: {sorted(ids)}")
    return prefabs


def dump_layout(layout: Mapping[str, Any]) -> str:
    """Canonical serialisation: sorted keys, fixed indent, trailing newline.

    Two runs that produce equal dicts therefore produce byte-identical files.
    """
    return json.dumps(layout, indent=2, sort_keys=True, ensure_ascii=False) + "\n"


def validate_layout(layout: Any, *, source: str = "<layout>") -> None:
    """Structural check of a generated layout document. Raises SchemaError."""
    version = _field(layout, "schemaVersion", source)
    _require(
        version == LAYOUT_SCHEMA_VERSION,
        f"{source}: schemaVersion must be {LAYOUT_SCHEMA_VERSION}, got {version!r}",
    )
    seed = _field(layout, "seed", source)
    _require(isinstance(seed, int) and not isinstance(seed, bool), f"{source}: 'seed' must be an integer")
    blocks = _field(layout, "blocks", source)
    _require(isinstance(blocks, list) and len(blocks) > 0, f"{source}: 'blocks' must be a non-empty array")
    for index, block in enumerate(blocks):
        where = f"{source}.blocks[{index}]"
        _require(_field(block, "index", where) == index, f"{where}: 'index' must equal its array position")
        _require(isinstance(_field(block, "prefabId", where), str), f"{where}: 'prefabId' must be a string")
        _require(_field(block, "kind", where) in PREFAB_KINDS, f"{where}: bad 'kind'")
        try:
            quantise_yaw(_number(_field(block, "yaw", where), f"{where}.yaw"))
        except ValueError as exc:
            raise SchemaError(f"{where}.yaw: {exc}") from exc
        _vec3(_field(block, "origin", where), f"{where}.origin")
        box = _field(block, "boundingBox", where)
        _vec3(_field(box, "center", f"{where}.boundingBox"), f"{where}.boundingBox.center")
        _vec3(_field(box, "size", f"{where}.boundingBox"), f"{where}.boundingBox.size")
        raw_connectors = _field(block, "connectors", where)
        _require(isinstance(raw_connectors, list), f"{where}: 'connectors' must be an array")
        for j, connector in enumerate(raw_connectors):
            parse_connector(connector, f"{where}.connectors[{j}]")
        raw_placements = _field(block, "placements", where)
        _require(isinstance(raw_placements, list), f"{where}: 'placements' must be an array")
        for j, placement in enumerate(raw_placements):
            parse_placement(placement, f"{where}.placements[{j}]")

    connections = _field(layout, "connections", source)
    _require(isinstance(connections, list), f"{source}: 'connections' must be an array")
    for index, connection in enumerate(connections):
        where = f"{source}.connections[{index}]"
        for side in ("a", "b"):
            endpoint = _field(connection, side, where)
            block_index = _field(endpoint, "block", f"{where}.{side}")
            _require(
                isinstance(block_index, int) and not isinstance(block_index, bool)
                and 0 <= block_index < len(blocks),
                f"{where}.{side}: 'block' out of range",
            )
            _require(
                isinstance(_field(endpoint, "connector", f"{where}.{side}"), str),
                f"{where}.{side}: 'connector' must be a string",
            )
        _vec3(_field(connection, "position", where), f"{where}.position")
        _require(isinstance(_field(connection, "type", where), str), f"{where}: 'type' must be a string")

    openings = _field(layout, "openConnectors", source)
    _require(isinstance(openings, list), f"{source}: 'openConnectors' must be an array")
    for index, opening in enumerate(openings):
        where = f"{source}.openConnectors[{index}]"
        block_index = _field(opening, "block", where)
        _require(
            isinstance(block_index, int) and not isinstance(block_index, bool)
            and 0 <= block_index < len(blocks),
            f"{where}: 'block' out of range",
        )
        _require(isinstance(_field(opening, "connector", where), str), f"{where}: 'connector' must be a string")

    stats = _field(layout, "stats", source)
    for key in ("blockCount", "connectionCount", "openCount"):
        value = _field(stats, key, f"{source}.stats")
        _require(isinstance(value, int) and not isinstance(value, bool), f"{source}.stats.{key} must be an integer")
    _require(stats["blockCount"] == len(blocks), f"{source}.stats.blockCount disagrees with blocks[]")
    _require(stats["connectionCount"] == len(connections), f"{source}.stats.connectionCount disagrees")
    _require(stats["openCount"] == len(openings), f"{source}.stats.openCount disagrees")
