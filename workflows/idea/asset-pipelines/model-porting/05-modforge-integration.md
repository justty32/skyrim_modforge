# 05 — ModForge Integration Design

← [README](README.md) · prev: [04-nif-and-collision.md](04-nif-and-collision.md) · next: [06-standalone-runbook.md](06-standalone-runbook.md)

> **現況更新（2026-11-02）：**[單 StaticSpec 完整包](../../../../../model-converter/MODFORGE-PACKAGE.md)已用獨立 opt-in CLI 接好既有 converter／ModForge package／Mutagen catalog 真讀回與失敗保護；本頁通用 `modelSource`／`importmesh` 仍未實作。靜態轉換器已改成
> 獨立 [`model-converter`](../../../../../model-converter/README.md) 的黑盒 CLI，並且已由它自行
> 產生 NIF/DDS；下文的 `modelSource`、`importmesh`、`Mesh.cs` 通用設計
> 都還沒有落地。擴展時應保留這些產品意圖，但實作改為呼叫現有 CLI；
> 舊的 Blender/NifTools/Compressonator backend 細節只留作歷史參考。

How the verified manual pipeline ([06]) folds into the generator. Design, not code — but it names concrete files, spec fields, and the existing conventions to copy. Grounded in the current `src` layout (`workflows/common/code-map/CODE_MAP.infra.md`) and the real `Spec.Items.cs` / `Assets.cs`.

**Copy these existing conventions exactly** (don't invent new ones):
- **Asset layer parallel to the record layer.** ModForge writes records + folder structure and **shells out** to external tools (Blender headless, ck-cmd, Compressonator) — it does **NOT** author nif/dds bytes. `Assets.cs` already says "does NOT author meshes." This is the xLODGen/Papyrus posture.
- **Shell-out with env-var fallback:** `Papyrus.cs` is the template — `null → MODFORGE_* env → default`, drives an exe under Wine *or* native. The mesh/texture tools copy this shape.
- **Asset copy + MO2 assembly:** `Assets.cs` copies `Meshes/Textures/Sounds` trees; `Package.cs` flattens the MO2 folder. Mesh output is just more `Meshes/…` + `Textures/…` for them to pick up.
- **Two-pass build:** `Generator.Build.cs` = pass 1 (records) → pass 2 (link). Mesh conversion is **independent of records** (it's pure asset work), so it's a **separate CLI step**, not a record builder — like `compile` is separate from `build`.

## 內容

內容按主題分 2 檔放在 [05-modforge-integration/](05-modforge-integration/)，本檔是入口。

| 檔案 | 內容 |
|------|------|
| [spec-cli-core.md](05-modforge-integration/spec-cli-core.md) | §1 spec 設計、§2 新 CLI `importmesh`、§3 `Mesh.cs` + `convert.py` |
| [wiring-done.md](05-modforge-integration/wiring-done.md) | §4 工具設定、§5 package／build 接線、§6 維護鏈落地、§7 完成定義 |
