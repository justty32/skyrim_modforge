"""Offline contract test for the quest-node extraction format."""
import json
from pathlib import Path
import unittest
from jsonschema import Draft202012Validator

ROOT = Path(__file__).resolve().parents[1]
SCHEMA = json.loads((ROOT / "schemas/quest-node.schema.json").read_text(encoding="utf-8"))
FIXTURES = ROOT / "fixtures/quest-nodes"


class QuestNodeSchemaTests(unittest.TestCase):
    def test_all_fixtures_validate(self):
        validator = Draft202012Validator(SCHEMA)
        nodes = [json.loads(p.read_text(encoding="utf-8")) for p in sorted(FIXTURES.glob("*.json"))]
        self.assertGreaterEqual(len(nodes), 3)
        for node in nodes:
            self.assertEqual([], list(validator.iter_errors(node)), node["questId"])
        keys = {f"{n['questId']}@{n['stage']}" for n in nodes}
        self.assertTrue({"MQ101@10", "MQ101@20", "MQ101@25", "MQ101@30"} <= keys)
        for node in nodes:
            for edge in node.get("previous", []) + node.get("next", []):
                self.assertIn(edge["node"], keys, edge)

    def test_invalid_cases_are_rejected(self):
        validator = Draft202012Validator(SCHEMA)
        valid = json.loads((FIXTURES / "mq101-intro.json").read_text(encoding="utf-8"))
        cases = [
            {**valid, "stage": -1},
            {k: v for k, v in valid.items() if k != "questId"},
            {**valid, "reactionTags": []},
            {**valid, "plugin": "Skyrim.txt"},
            {**valid, "next": [{"node": "not-a-node-key"}]},
            {**valid, "unexpected": True},
        ]
        for case in cases:
            self.assertTrue(list(validator.iter_errors(case)), case)


if __name__ == "__main__":
    unittest.main()
