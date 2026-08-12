import json
from pathlib import Path
import unittest

from jsonschema import Draft202012Validator


ROOT = Path(__file__).resolve().parents[1]
SCHEMA = json.loads((ROOT / "schemas/scene-catalog.schema.json").read_text(encoding="utf-8"))


def valid_catalog():
    return {
        "schemaVersion": 1,
        "sources": [{
            "plugin": "Example.esp",
            "sourcePath": "C:/mods/Example.esp",
            "sha256": "a" * 64,
            "localized": False,
            "recordCount": 1,
            "loadOrderIndex": 0,
        }],
        "records": [{
            "formKey": "Example.esp:0x000800",
            "plugin": "Example.esp",
            "recordType": "Static",
            "editorId": "ExampleRock",
            "name": None,
            "modelPath": "Landscape/Rocks/ExampleRock.nif",
            "sourcePlugin": "Example.esp",
            "sourcePath": "C:/mods/Example.esp",
        }],
    }


class SceneCatalogSchemaTests(unittest.TestCase):
    def test_scene_catalog_contract_accepts_v1_document(self):
        Draft202012Validator(SCHEMA).validate(valid_catalog())

    def test_scene_catalog_contract_rejects_missing_root_fields(self):
        validator = Draft202012Validator(SCHEMA)
        for field in ["schemaVersion", "sources", "records"]:
            document = valid_catalog()
            del document[field]
            self.assertTrue(list(validator.iter_errors(document)), field)


if __name__ == "__main__":
    unittest.main()
