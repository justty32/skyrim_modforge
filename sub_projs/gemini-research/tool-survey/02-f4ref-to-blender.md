# Gemini Raw: F4RefToBlender (2026-06-15)

Query: GitHub repo 6ooflames/F4RefToBlender — 描述、README、目錄結構、技術架構

---

### 1. About

> "Import Reference data from Gamebryo, NetImmerse, and Creation Kit into Blender using this and BadDogSkyrim's nifly implementation."

### 2. README

- 支援：Fallout 4、Skyrim CK
- 需求：PyNifly (BadDogSkyrim/PyNifly)
- 流程：BAE 解壓 mesh+texture → CK 匯出物件數據表（EditorID→模型路徑）→ CK 匯出 cell reference data → 貼進 Blender text editor 執行腳本
- Blender 執行時會 lock up，需外部 terminal 監控進度

### 3. 目錄結構

```
Source/
  importreference.py    # 核心腳本（唯一檔案）
LICENSE (GPL-3.0)
README.md
```

### 4. 技術架構

- **語言**：Python（Blender 腳本，非標準 Add-on）
- **依賴**：PyNifly（nifly 的 Python 封裝，讀 .nif 模型）
- **輸入**：
  1. CK 匯出的 reference data（EditorID + XYZ + 旋轉）
  2. CK 匯出的物件數據表（EditorID → 內部模型路徑）
  3. BAE 解壓的 .nif + 貼圖
- **輸出**：Blender 3D 場景（完整還原遊戲內佈局）
- **無 GUI**：直接改腳本內三個檔案路徑

---

⚠️ 以上為 Gemini 輸出，需人工驗證
