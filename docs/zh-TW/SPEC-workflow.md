<!-- CLI 工作流程 -->
# ModForge 規格說明 — 工作流程

← [目錄](SPEC-index.md)

## 工作流程

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # 先行檢查
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # 僅建置插件
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + 編譯後的腳本 -> MO2 資料夾
```
`package` 輸出 `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`。

## Voice：TTS / voice cloning / FUZ

```bash
dotnet run --project src/ModForge.Cli -- voicediag  myspec.json out.esp
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp --plan
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp
```

`voicediag` 與 `voicelines --plan` 會離線列出每個 INFO 對應的 speaker、voiceType、template 與輸出路徑，不需要 TTS。實際生成時，`MODFORGE_TTS_BIN` 指向本機 `voicegen.py` wrapper；`voiceTemplates[].engine` 目前可用 `f5`，`fish-s2` 會再轉呼 `MODFORGE_FISH_SPEECH_BIN`。`MODFORGE_XWMAENCODE` 指向 CK/DirectX 的 `xWMAEncode.exe`，在 Wine 下需要 Windows path；缺 xWMA 時降級為 loose `.wav`。`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA` 用於 lip；缺則 no-lip/static mouth。

Voice files 是 loose assets，不嵌入 ESP/ESM。最穩流程是先 `package` 到最終 mod folder，再對該資料夾中的 plugin 跑 `voicelines`；或 `build` + `voicelines` 到 staging dir，再 `package --assets <stagingDir>`。輸出路徑形如 `Sound/Voice/<plugin>/<voiceType>/<quest>_<topic>_<infoFormId>_<response>.fuz`。

**自然語言 → 規格：** 向 AI 代理人（Claude Code）描述需求；代理人根據本文件 / `../examples/spec.schema.json`（依 `for_agent.md`）輸出規格，執行 `validate`（自動修正問題），再執行 `build`/`package`。此代理人驅動循環**即是** NL→規格層——工具本身不含 LLM API（原本規劃的 `describe` 指令已取消），因此無需設定任何 API 金鑰或提供商。

## 語音（TTS 語音克隆 → .fuz）

選用的 build 後管線，為建好 plugin 中的每一句對話合成配音音訊（+ 對嘴）。僅使用外部工具
——不內含任何捆綁元件。

**Spec 欄位**

- `voiceTemplates[]` — 具名的克隆配方，由 NPC 參照：
  - `id` — 唯一的模板名稱。
  - `engine` — `f5` | `chatterbox` | `gptsovits` | `xtts`。**僅 `f5` 有實作**；其餘可通過
    validate 但尚無後端。
  - `referenceWav` + `referenceText` — zero-shot 參考片段與其逐字稿（路徑相對於 spec 檔；
    f5 需要逐字稿）。
  - `modelPath` — 選用的微調模型目錄（相對於 spec）。
  - `rvcModel` — 選用的 RVC 模型，用於音色穩定。
  - `seed` — 確定性輸出。
  - `speed` / `exaggeration` / `language` — 生成調校；三者連同其餘參數全數傳給 TTS process。
- `npcs[].voiceTemplate` — ref → 某 `voiceTemplates` id；把該 NPC 的台詞導向克隆引擎。與
  `npcs[].voiceType`（遊戲內 VTYP 記錄 ref）不同——你仍需要一個 voiceType，它決定輸出資料夾
  （見下）。
- `voiceLine`（全域，選用）— 輸出設定：`format`（`fuz` | `wav` | `xwm`，預設 `fuz`）與
  `skipLip`（true = 略過 .lip 生成，嘴形靜止）。

**環境變數**

| 變數 | 工具 | 用途 |
|-----|------|-----|
| `MODFORGE_TTS_BIN` | TTS wrapper 腳本/執行檔（如 f5 venv wrapper）| 必要——缺則 `voicelines` 直接報錯 |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe`（走 Wine）| WAV → xwm 編碼（`format: xwm`/`fuz`）|
| `MODFORGE_FACEFX` | `FaceFXWrapper.exe`（走 Wine）| .lip 對嘴生成 |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | FaceFXWrapper 所需 |

**工作流程**

```bash
dotnet run --project src/ModForge.Cli -- build      myspec.json out.esp   # 1. build（所有 dialogue/banter/scene INFO 取得 EditorID 供檔名用）
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp   # 2. 走訪 INFO，合成 WAV → xwm → .fuz 放在 esp 旁
dotnet run --project src/ModForge.Cli -- package    myspec.json OutModDir # 3. 照常打包（Sound/ 樹隨 mod 一起走）

# 輔助：從原版 archive 擷取參考片段
dotnet run --project src/ModForge.Cli -- extract-voices "<path>/Skyrim - Voices_en0.bsa" FemaleYoungEager refclips/
```

**檔案佈局** — `Sound/Voice/<plugin>/<voiceType>/<quest10>_<topic15>_<formid8>_<n>.fuz`
（CK 命名格式：quest EditorID 前 10 字、topic EditorID 前 15 字、8 位 hex INFO FormID、
1-based response 序號）。引擎依**說話者的 voiceType** 查檔，所以每個 voiceType 生成一份檔案
即可服務該 voiceType 下所有說那句台詞的 NPC。重跑時已存在的檔會被略過（尚無 hash cache
——刪除即可重生）。

**失敗行為**

- 某 INFO 的說話者無法從其條件解出（理解 GetIsID、alias 或 faction 條件）→ 以**大聲警告**
  略過，絕不靜默。
- xwm 編碼在 `format: fuz` 下失敗 → 該句改寫成 **loose `.wav`** 並附警告，而非把裸 WAV 包進
  .fuz（引擎是否接受 WAV-in-fuz 未經驗證）。
- 缺 FaceFX 環境變數 → 無 .lip（效果同 `skipLip`）；字幕仍可運作（一旦有真正的 .fuz 檔，
  就不需要 Fuz Ro D'oh）。

## 尚未涵蓋（可在 `ModForge.Core` 的 `Generator.Build` + 規格類別中擴充）
世界放置現已涵蓋新建室內場景、原版室內場景，**以及室外/世界空間場景**（透過 `worldspace` + 世界座標），ModForge 現在也能**建立**新的世界空間（WRLD）+ 地區（REGN）——見 [SPEC-world](SPEC-world.md)（僅限記錄層；地形/LOD/導航網格仍在 CK 端）。Refs（spec 內部或 `<master>:0xFORMID`）與 `find` 指令是參照外部 form 的基礎積木。其餘缺口為長尾記錄類型/欄位及 CK 端的地形/LOD/導航網格創作——記錄端的模式相同：新增一個規格類別 + 在 `Build` 中新增一個迴圈。
