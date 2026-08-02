# model-converter — 已移出為獨立 repo

**2026-08-02 起本資料夾只是導引。** `nif2gltf/`、`gltf2nif/`、測試與 CLI 契約全在同層的獨立 repo：

```
../../../model-converter/                  ← 本機：~/repo/moddings/skyrim/projects/model-converter
```

→ [該 repo 的 README](../../../model-converter/README.md)｜[PROTOCOL.md](../../../model-converter/PROTOCOL.md)（CLI 契約）｜[gltf2nif/README.md](../../../model-converter/gltf2nif/README.md)

**未帶 commit 歷史**（使用者決定）——舊歷史查 ModForge 的 `git log -- sub_projs/model-converter`。

## 與 ModForge 的關係（不變）

**黑盒 exec，不整合**。掛勾＝環境變數 `MODFORGE_NIF2GLTF_BIN`，指向一支在自己 venv 內跑的 wrapper；呼叫方只給 args、只收 glTF。

兩個消費者：

- [godot-worldspace-editor](../godot-worldspace-editor/README.md)（同層 repo）——`nif→glTF` 當 Godot 裡的視覺代理。
- [darksouls-port](../../../darksouls-port/plan.md)（同層 repo）——`gltf2nif` 反向，FLVER→glTF→NIF 資產管線。

正向（外部 FBX/OBJ/glTF → `.nif`）的 deep-dive 真相**留在 ModForge**：[workflows/idea/asset-pipelines/model-porting/](../../workflows/idea/asset-pipelines/model-porting/README.md)。
