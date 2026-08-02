# darksouls-port — 已移出

**2026-08-02 起本資料夾只是導引。** 內容全在：

```
projects/darksouls-port/          ← 本機：~/repo/moddings/skyrim/projects/darksouls-port
```

→ [新位置的 README](../../../darksouls-port/README.md)

## 是什麼

本機 Dark Souls Remastered 地圖移植成 Skyrim worldspace（首目標 m18_01_00_00 北方不死院）。自帶 C# `DsExtractor`（MSB/FLVER/TPF → JSON/glTF/DDS，SoulsFormats）+ Python 工具 + venv。**移植資產僅本機、不發佈。**

## 留在 ModForge 的

生成端在 ModForge（spec → worldspace esp）；反向模型轉換靠 [model-converter](../model-converter/README.md) 的 `gltf2nif`。

---

**未帶 commit 歷史**（使用者決定）——舊歷史查 `git log -- sub_projs/darksouls-port`。
