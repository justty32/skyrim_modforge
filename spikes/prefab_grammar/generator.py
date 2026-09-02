"""Deterministic prefab grammar layout generation.

The contract defaults remain rooms=6, hall_length=2, and max_blocks=30.  They
produce at least 12 blocks with the spike fixture library while leaving enough
room for the required hall/room cadence.
"""
from __future__ import annotations

import random
from collections import deque
from dataclasses import dataclass
from typing import Optional, Sequence, Tuple

from prefab_grammar.geometry import Aabb, add, any_overlap, canon, canon_vec, norm_angle, rotate_xy, sub
from prefab_grammar.schema import GENERATOR_ID, LAYOUT_SCHEMA_VERSION, Prefab, validate_layout


class GenerationError(RuntimeError):
    """The prefab library cannot satisfy a required generation step."""


@dataclass(frozen=True)
class GeneratorOptions:
    seed: int
    rooms: int = 6
    hall_length: int = 2
    max_blocks: int = 30


@dataclass(frozen=True)
class _Socket:
    block: int
    connector: str
    type: str
    position: Tuple[float, float, float]
    facing: float


def _world_connector(connector: object, yaw: float, origin: Sequence[float]) -> dict:
    return {
        "id": connector.id,
        "role": connector.role,
        "type": connector.type,
        "position": list(add(rotate_xy(connector.position, yaw), origin)),
        "facing": norm_angle(connector.facing + yaw),
    }


def _world_placement(placement: object, yaw: float, origin: Sequence[float]) -> dict:
    return {
        "base": placement.base,
        "editorId": placement.editor_id,
        "position": list(add(rotate_xy(placement.position, yaw), origin)),
        "rotation": [
            norm_angle(placement.rotation[0]),
            norm_angle(placement.rotation[1]),
            norm_angle(placement.rotation[2] + yaw),
        ],
        "scale": canon(placement.scale),
    }


def _make_block(prefab: Prefab, index: int, yaw: float, origin: Sequence[float]) -> Tuple[dict, Aabb]:
    world_box = prefab.bbox.transformed(yaw, origin)
    block = {
        "index": index,
        "prefabId": prefab.id,
        "kind": prefab.kind,
        "yaw": norm_angle(yaw),
        "origin": list(canon_vec(origin)),
        "boundingBox": {
            "center": list(world_box.center),
            "size": list(world_box.size),
        },
        "connectors": [
            _world_connector(connector, yaw, origin)
            for connector in sorted(prefab.connectors, key=lambda item: item.id)
        ],
        "placements": [
            _world_placement(placement, yaw, origin) for placement in prefab.placements
        ],
    }
    return block, world_box


def _socket_for(block: dict, connector_id: str) -> _Socket:
    connector = next(
        item for item in block["connectors"] if item["id"] == connector_id
    )
    return _Socket(
        block=block["index"],
        connector=connector["id"],
        type=connector["type"],
        position=tuple(connector["position"]),
        facing=connector["facing"],
    )


def _connection(socket: _Socket, block: dict, entrance_id: str) -> dict:
    return {
        "a": {"block": socket.block, "connector": socket.connector},
        "b": {"block": block["index"], "connector": entrance_id},
        "position": list(socket.position),
        "type": socket.type,
    }


def generate_layout(prefabs: Sequence[Prefab], options: GeneratorOptions) -> dict:
    """Generate one deterministic layout using only the options seed for randomness."""
    rng = random.Random(options.seed)
    library = tuple(sorted(prefabs, key=lambda prefab: prefab.id))
    candidates = [prefab for prefab in library if prefab.kind == "entrance"]
    if not candidates:
        raise GenerationError("no entrance prefab in library")

    entrance = rng.choice(candidates)
    entrance_block, entrance_box = _make_block(entrance, 0, 0.0, (0.0, 0.0, 0.0))
    blocks = [entrance_block]
    placed_boxes = [entrance_box]
    connections = []
    frontier = deque(
        _socket_for(entrance_block, connector.id)
        for connector in sorted(entrance.exit_connectors(), key=lambda item: item.id)
    )
    hall_budget = options.hall_length
    rooms_placed = 0
    side = deque()

    def try_place(socket: _Socket, wanted: str) -> Optional[Tuple[Prefab, dict, Aabb]]:
        place_candidates = [
            (prefab, prefab.entrance_connector())
            for prefab in library
            if prefab.kind == wanted and prefab.entrance_connector().type == socket.type
        ]
        place_candidates.sort(key=lambda item: item[0].id)
        rng.shuffle(place_candidates)
        for prefab, connector in place_candidates:
            yaw = norm_angle(socket.facing + 180.0 - connector.facing)
            origin = sub(socket.position, rotate_xy(connector.position, yaw))
            block, box = _make_block(prefab, len(blocks), yaw, origin)
            if not any_overlap(box, placed_boxes):
                return prefab, block, box
        return None

    while (
        frontier
        and len(blocks) < options.max_blocks
        and (rooms_placed < options.rooms or hall_budget > 0)
    ):
        socket = frontier.popleft()
        wanted = "hall" if hall_budget > 0 else "room"
        placed = try_place(socket, wanted)
        if placed is None and wanted == "room":
            placed = try_place(socket, "hall")
        if placed is None:
            side.append(socket)
            continue

        prefab, block, box = placed
        blocks.append(block)
        placed_boxes.append(box)
        entrance_connector = prefab.entrance_connector()
        connections.append(_connection(socket, block, entrance_connector.id))
        new_exits = sorted(prefab.exit_connectors(), key=lambda item: item.id)
        main = rng.randrange(len(new_exits)) if len(new_exits) > 1 else 0
        for index, connector in enumerate(new_exits):
            new_socket = _socket_for(block, connector.id)
            if index == main:
                frontier.append(new_socket)
            else:
                side.append(new_socket)

        if prefab.kind == "hall":
            hall_budget -= 1
        else:
            rooms_placed += 1
            hall_budget = options.hall_length

    side.extend(frontier)
    frontier.clear()
    open_connectors = []
    while side:
        socket = side.popleft()
        placed = try_place(socket, "cap")
        if placed is None:
            open_connectors.append(
                {
                    "block": socket.block,
                    "connector": socket.connector,
                    "position": list(socket.position),
                    "facing": norm_angle(socket.facing),
                    "type": socket.type,
                }
            )
            continue
        prefab, block, box = placed
        blocks.append(block)
        placed_boxes.append(box)
        connections.append(_connection(socket, block, prefab.entrance_connector().id))

    result = {
        "schemaVersion": LAYOUT_SCHEMA_VERSION,
        "seed": options.seed,
        "generator": GENERATOR_ID,
        "options": {
            "rooms": options.rooms,
            "hallLength": options.hall_length,
            "maxBlocks": options.max_blocks,
        },
        "blocks": blocks,
        "connections": connections,
        "openConnectors": open_connectors,
        "stats": {
            "blockCount": len(blocks),
            "connectionCount": len(connections),
            "openCount": len(open_connectors),
        },
    }
    validate_layout(result)
    return result
