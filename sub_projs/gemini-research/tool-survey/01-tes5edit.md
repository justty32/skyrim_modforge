# Gemini Raw: TES5Edit / xEdit (2026-06-15)

Query: GitHub repo TES5Edit/TES5Edit — 描述、README、目錄結構、release、技術架構

---

### 1. 倉庫描述 (About)

> **"xEdit by Elminster; Updated and maintained by Sharlikran, Zilav, and Hlp."**

### 2. README 核心內容

- xEdit 是針對 Bethesda "Creation Engine" 遊戲的高級圖形化插件編輯器和衝突檢測工具
- 用於查看、編輯和「清理」插件文件（.esp, .esm, .esl）
- **支援遊戲**：Oblivion, Skyrim (LE/SE/AE/VR), Fallout 3, New Vegas, FO4 (VR), FO76, Starfield
- **重命名機制**：工具本身 Game-agnostic，改名為 SSEEdit.exe / FO4Edit.exe / SFEdit.exe 即切換遊戲模式

### 3. 頂層目錄結構

- `xEdit.dpr` / `xEdit.dproj` — Delphi 專案主檔案
- `/Edit Scripts/` — `.pas` (Pascal) 腳本（QuickChange, LODGen, Assets browser, Copy as override…）
- `/External/` — 第三方依賴（jcl, jvcl, synapse, zlib, lz4, FastMM4）
- `/Launchers/` — `.bat` 批次啟動檔（QuickAutoClean, SkyrimSE, Fallout4, Starfield…）

### 4. 最新 Release

- **v4.1.5f**（2024-04-27）
  - 支援 Fallout 4 Next-Gen (NG) 更新
  - 支援 v7/v8 BA2 格式
  - 修復 Starfield / Skyrim VR record 解析錯誤
  - 改進 Steam VDF 解析（自動偵測安裝路徑）

### 5. 技術架構

**語言**：Delphi (Object Pascal)，VCL 視窗介面

**核心功能**：
1. **衝突解決**：Side-by-side 對比多 mod 同一 record，手動合併
2. **清理**：自動移除 ITM（與大師檔相同的 record）、恢復 UDR（已刪除引用）
3. **腳本自動化**：內建 Pascal 腳本引擎，批量處理 record
4. **格式轉換**：mod 轉 .esl 輕量格式

**架構亮點**：
- `wbInterface.pas`：所有物件（文件/record/子項）抽象為介面，懶加載（使用者展開才讀 binary）
- `wbDefinitions.pas`：每種 record 類型（WEAP, NPC_ 等）的 binary 佈局定義 → 遊戲更新只改定義，不動核心
- 腳本引擎：修改版 JvInterpreter，Pascal 腳本可直接呼叫 Delphi 內部類

---

⚠️ 以上為 Gemini 輸出，Nexus ID / URL 需人工驗證
