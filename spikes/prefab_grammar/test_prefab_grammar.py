"""Deterministic contract tests for the prefab grammar spike."""
import hashlib
import json
import pathlib
import subprocess
import sys
import unittest

SPIKES = pathlib.Path(__file__).resolve().parents[1]      # .../ModForge/spikes
if str(SPIKES) not in sys.path:
    sys.path.insert(0, str(SPIKES))

from prefab_grammar import geometry, schema            # noqa: E402
from prefab_grammar.generator import GeneratorOptions, generate_layout  # noqa: E402

PREFABS = SPIKES / "prefab_grammar" / "data" / "prefabs"


class PrefabGrammarContractTests(unittest.TestCase):
    @staticmethod
    def _layout(seed):
        prefabs = schema.load_prefab_library(PREFABS)
        return generate_layout(prefabs, GeneratorOptions(seed=seed))

    @staticmethod
    def _hash(layout):
        text = schema.dump_layout(layout)
        return hashlib.sha256(text.encode("utf-8")).hexdigest()

    def test_same_seed_is_byte_identical(self):
        first_hash = self._hash(self._layout(1337))
        second_hash = self._hash(self._layout(1337))
        print(f"SAME_SEED_SHA256 = {first_hash}")
        self.assertEqual(first_hash, second_hash)

    def test_different_seeds_differ(self):
        first = self._layout(1337)
        second = self._layout(2024)
        first_hash = self._hash(first)
        second_hash = self._hash(second)
        print(f"SEED_1337_SHA256 = {first_hash}")
        print(f"SEED_2024_SHA256 = {second_hash}")
        self.assertNotEqual(first_hash, second_hash)
        schema.validate_layout(first)
        schema.validate_layout(second)

    def test_bounding_boxes_do_not_overlap(self):
        layout = self._layout(1337)
        boxes = [
            geometry.Aabb.of(block["boundingBox"]["center"], block["boundingBox"]["size"])
            for block in layout["blocks"]
        ]
        for first_index in range(len(boxes)):
            for second_index in range(first_index + 1, len(boxes)):
                self.assertFalse(
                    boxes[first_index].overlaps(boxes[second_index]),
                    f"bounding boxes overlap: indexes {first_index} and {second_index}",
                )
        self.assertGreaterEqual(layout["stats"]["blockCount"], 12)

    def test_connectors_pair_bidirectionally(self):
        layout = self._layout(1337)
        connector_by_endpoint = {
            (block["index"], connector["id"]): connector
            for block in layout["blocks"]
            for connector in block["connectors"]
        }
        all_endpoints = set(connector_by_endpoint)
        seen_endpoints = []

        for connection in layout["connections"]:
            a_endpoint = (connection["a"]["block"], connection["a"]["connector"])
            b_endpoint = (connection["b"]["block"], connection["b"]["connector"])
            self.assertIn(a_endpoint, connector_by_endpoint)
            self.assertIn(b_endpoint, connector_by_endpoint)
            a_connector = connector_by_endpoint[a_endpoint]
            b_connector = connector_by_endpoint[b_endpoint]
            for axis in range(3):
                self.assertAlmostEqual(
                    a_connector["position"][axis],
                    b_connector["position"][axis],
                    places=6,
                )
            facing_delta = abs(
                ((a_connector["facing"] - b_connector["facing"]) % 360) - 180
            )
            self.assertLess(facing_delta, 1e-6)
            self.assertEqual(a_connector["type"], b_connector["type"])
            seen_endpoints.extend((a_endpoint, b_endpoint))

        for opening in layout["openConnectors"]:
            endpoint = (opening["block"], opening["connector"])
            self.assertIn(endpoint, connector_by_endpoint)
            seen_endpoints.append(endpoint)

        self.assertEqual(set(seen_endpoints), all_endpoints)
        for endpoint in all_endpoints:
            self.assertEqual(
                seen_endpoints.count(endpoint),
                1,
                f"connector endpoint must appear exactly once: {endpoint}",
            )

    def test_invalid_documents_are_rejected(self):
        bad_version = {
            "schemaVersion": 2,
            "id": "bad_version",
            "kind": "entrance",
            "boundingBox": {"center": [0, 0, 0], "size": [1, 1, 1]},
            "connectors": [
                {"id": "out", "role": "exit", "type": "hall2", "position": [0, 0, 0], "facing": 0}
            ],
            "placements": [
                {"base": "Skyrim.esm:0x000001", "position": [0, 0, 0]}
            ],
        }
        with self.assertRaises(schema.SchemaError):
            schema.parse_prefab(bad_version)

        bad_kind = {
            "schemaVersion": 1,
            "id": "bad_kind",
            "kind": "tunnel",
            "boundingBox": {"center": [0, 0, 0], "size": [1, 1, 1]},
            "connectors": [],
            "placements": [
                {"base": "Skyrim.esm:0x000001", "position": [0, 0, 0]}
            ],
        }
        with self.assertRaises(schema.SchemaError):
            schema.parse_prefab(bad_kind)

        bad_facing = {
            "schemaVersion": 1,
            "id": "bad_facing",
            "kind": "entrance",
            "boundingBox": {"center": [0, 0, 0], "size": [1, 1, 1]},
            "connectors": [
                {"id": "out", "role": "exit", "type": "hall2", "position": [0, 0, 0], "facing": 45}
            ],
            "placements": [
                {"base": "Skyrim.esm:0x000001", "position": [0, 0, 0]}
            ],
        }
        with self.assertRaises(schema.SchemaError):
            schema.parse_prefab(bad_facing)

        bad_size = {
            "schemaVersion": 1,
            "id": "bad_size",
            "kind": "entrance",
            "boundingBox": {"center": [0, 0, 0], "size": [1, 0, 1]},
            "connectors": [
                {"id": "out", "role": "exit", "type": "hall2", "position": [0, 0, 0], "facing": 0}
            ],
            "placements": [
                {"base": "Skyrim.esm:0x000001", "position": [0, 0, 0]}
            ],
        }
        with self.assertRaises(schema.SchemaError):
            schema.parse_prefab(bad_size)

        bad_cap = {
            "schemaVersion": 1,
            "id": "bad_cap",
            "kind": "cap",
            "boundingBox": {"center": [0, 0, 0], "size": [1, 1, 1]},
            "connectors": [
                {"id": "in", "role": "entrance", "type": "hall2", "position": [0, 0, 0], "facing": 180},
                {"id": "out", "role": "exit", "type": "hall2", "position": [1, 0, 0], "facing": 0},
            ],
            "placements": [
                {"base": "Skyrim.esm:0x000001", "position": [0, 0, 0]}
            ],
        }
        with self.assertRaises(schema.SchemaError):
            schema.parse_prefab(bad_cap)

        bad_layout = {
            "schemaVersion": 1,
            "seed": 1,
            "blocks": [
                {
                    "index": 0,
                    "prefabId": "only_block",
                    "kind": "entrance",
                    "yaw": 0,
                    "origin": [0, 0, 0],
                    "boundingBox": {"center": [0, 0, 0], "size": [1, 1, 1]},
                    "connectors": [],
                    "placements": [],
                }
            ],
            "connections": [],
            "openConnectors": [],
            "stats": {"blockCount": 2, "connectionCount": 0, "openCount": 0},
        }
        with self.assertRaises(schema.SchemaError):
            schema.validate_layout(bad_layout)

    def test_cli_runs_and_is_deterministic(self):
        command = [sys.executable, "-m", "prefab_grammar.cli", "--seed", "1337"]
        first = subprocess.run(
            command, cwd=str(SPIKES), capture_output=True, text=True
        )
        second = subprocess.run(
            command, cwd=str(SPIKES), capture_output=True, text=True
        )
        self.assertEqual(first.returncode, 0, first.stderr)
        self.assertEqual(second.returncode, 0, second.stderr)
        self.assertEqual(first.stdout, second.stdout)
        layout = json.loads(first.stdout)
        schema.validate_layout(layout)


if __name__ == "__main__":
    unittest.main()
