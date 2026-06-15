# Gemini Raw: BodySlide and Outfit Studio (2026-06-15)

Query: GitHub repo ousnius/BodySlide-and-Outfit-Studio — 描述、README、目錄結構、技術架構

---

### 1. About

An outfit and body customization tool, a mesh editor and a conversion tool for Bethesda games.
License: GPL-3.0 | Language: C++ (~97%) | 支援：Skyrim/SSE, Fallout 4, Starfield

### 2. README 核心

- BodySlide：滑桿式身體/服裝外觀調整 + Batch Build（一鍵套用所有服裝）
- Outfit Studio：.nif 網格編輯器、skinning/weight transfer、格式轉換
- 建構依賴：wxWidgets, nifly, OpenGL/GLI/SOIL2, TinyXML-2, nlohmann/json, fkYAML, Autodesk FBX SDK
- 跨平台：Windows (VS 2022 + CMake)、Linux (CMake native)

### 3. 目錄結構

```
.github/            CI workflows
lang/               多語言 (.po/.mo)
lib/
  FSEngine/         BSA/BA2 直讀（無需解壓）
  fkYAML/
  nifly/            NIF 格式核心（子模組）
  lz4/, tinyxml2/, json/
res/xrc/            wxWidgets XML UI 定義
src/
  common/           共用底層
  program/          BodySlide.cpp, OutfitStudio.cpp
CMakeLists.txt
```

### 4. 最新 Release

v5.8.1 (2026-06)：Favorites 管理、Output Path 易達、Lazy linear diff lists（大網格速度提升）、移除 exe 名稱 x64 後綴

### 5. 技術架構

- **語言**：C++ (C++17+)
- **GUI**：wxWidgets（跨平台）
- **3D 渲染**：OpenGL 即時預覽
- **輸入**：.nif, .obj, .fbx, .xml (Slider 定義), .osp (工程檔)
- **輸出**：修改後 .nif, .osd (差分數據), .tri (變形數據)
- **BSA/BA2 直讀**：自研 FSEngine

---

⚠️ 以上為 Gemini 輸出，版本號需人工確認
