# godot-worldspace-editor — 已移出為獨立 repo

**2026-08-02 起本資料夾只是導引。** 程式碼與設計文檔全在同層的獨立 repo：

```
../../../godot-worldspace-editor/          ← 本機：~/repo/moddings/skyrim/projects/godot-worldspace-editor
```

→ [該 repo 的 README](../../../godot-worldspace-editor/README.md)｜[design/](../../../godot-worldspace-editor/design/README.md)｜[godot/](../../../godot-worldspace-editor/godot/)

**未帶 commit 歷史**（使用者決定）——舊歷史查 ModForge 的 `git log -- sub_projs/godot-worldspace-editor`。

## 與 ModForge 的關係（不變）

靠**協議**對接、不整合。前端輸出 → 後端入口：

| 前端輸出 | ModForge 入口 |
|---|---|
| heightmap PNG | spec `heightmap` → `Heightmap.cs` → `Vhgt.Encode` + `Vnml.Compute` |
| splatmap PNG | spec `baseTexture`（BTXT）/ `textureLayers[].splatmap` → `Splatmap.cs` → `Vtxt.cs` |
| `placements.json` | spec `godotPlacements` → `GodotPlacements.cs` → 合流 `placements[]` |

契約真相在 [SPEC-worldspaces](../../docs/spec/SPEC-worldspaces.md) + [CODE_MAP.world](../../workflows/common/code-map/CODE_MAP.world.md)，**留在 ModForge**。

⚠️ 該 repo 執行期會 shell out 回 ModForge CLI（`nifexport`／`texexport`／`texpath`）與 `../model-converter` 的 venv，預設路徑假設**三個 repo 同層**；不同層時用它的 `godot/texconfig.json` 覆寫。
