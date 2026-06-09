# 05 — ModForge 整合設計

← [README](README.md) · 上一份：[04-fuz-and-filenames.md](04-fuz-and-filenames.md) · 下一份：[06-standalone-runbook.md](06-standalone-runbook.md)

驗證過的手跑 pipeline（[06]）如何折進產生器。這是設計、非程式碼 —— 但它點名具體檔案、spec 欄位、與必須照抄的既有慣例。以當前 `src` layout 為基礎（讀自 `docs/CODE_MAP.infra.md`）。

**精確照抄這些既有慣例**（別發明新模式）：
- **帶 env-var fallback 的 shell-out：** `Papyrus.cs` 是範本 —— `PapyrusOptions` 欄位 `null → MODFORGE_* env → default`，在 Wine *或* native 下驅動 exe。語音工具照抄此形狀。
- **條件式 EmbeddedResource：** 六個 `MF*.psc/.pex` 條件式 embed 進 `ModForge.Cli.csproj`（缺檔仍 build；runtime warn）。任何 embedded helper（如合成-lip 資料表）照辦。
- **資產複製 + MO2 組裝：** `Assets.cs` 複製 `Meshes/Textures/Sounds` 樹；`Package.cs` 組裝扁平 MO2 資料夾。語音輸出只是更多 `Sound/Voice/...` 讓它們撿。
- **兩段 build：** `Generator.Build.cs` = pass 1（建 records）→ pass 2（link）。語音生成在 records 存在*之後*跑（需要最終 INFO FormID），所以是**post-build step**，不是 record builder。

---

## 1. Spec 設計（新欄位）

僅 additive —— 無 breaking change（依 CLAUDE.md：新 optional 欄位安全；既有 example 不受影響）。加完後更新 `examples/spec.schema.json` 與 `sample_spec.json`。

**`VoiceTemplate`** —— 具名語音 recipe（`ModSpec` 上的頂層清單，如 `Spec.cs` 其他 record family）：
```jsonc
"voiceTemplates": [{
  "id": "MaleNordCloned",
  "engine": "f5",                         // f5 | chatterbox | gptsovits
  "referenceWav": "refs/MaleNord_ref.wav",// 零訓練 ref（f5/chatterbox）
  "referenceText": "transcript ...",      // f5 需要
  "modelPath": null,                       // gptsovits 微調目錄（取代 ref）
  "rvcModel": null,                        // 選用 RVC 重上色後處理
  "language": "en",
  "seed": 12345,
  "exaggeration": 0.5                       // chatterbox 情緒旋鈕（選用）
}]
```
這對映記憶 [[voice-gen-interface-future]] 的延後計劃。

**`NpcSpec.voiceTemplate`** —— 把某 NPC 的行路由到一個 template。NPC 已有 `VoiceType`（ref → VTYP；`Spec.Actors.cs` 第 16 行），供**路徑段**；`voiceTemplate` 供**引擎/語音**。或全域 `voiceType → voiceTemplate` map 讓 vanilla 配音 NPC 自動路由。

**`voiceLine`（選用，per-INFO 或 per-build）：** `{ skipLip: bool, format: "wav"|"xwm"|"fuz" }`。INFO 行已帶**文字** —— 無需為字加新欄位。

> 欄位衛生：之後刪/改名欄位需 `grep -r "field" examples/` 並在同一 commit 更新所有命中（CLAUDE.md 規則）。新增免費。

---

## 2. 新 CLI step `voicelines`（與 `compile`/`package` 平行）

住在 `Program.Build.cs`，與 `build`/`validate`/`package`/`compile` 並列。在 `build` **之後**跑（需要最終 INFO FormID）。每個 emit 的 INFO response 的 pipeline：

```
voicelines <spec.json> <built.esp>
  1. 走訪 INFO records（Mutagen 讀建好的 esp，如 Diagnostics 那樣）
  2. 每個 response：解析 text + NPC voiceType + voiceTemplate
  3. 算 CK-相符檔名 + 路徑          ← 決定性，ModForge 超能力（[04]）
  4. 快取檢查：(text+template+seed) 未變且檔在 → 跳過
  5. shell-out 到 voicegen（MODFORGE_TTS_BIN）→ line_raw.wav
  6. 正規化（mono/16-bit；lip 則 16k 副本）→ native 或 ffmpeg
  7.（選）lip：FaceFXWrapper/Runalip 走 Wine，或 native 合成（[03]）
  8.（選）xwm：xWMAEncode 走 Wine（[03]）
  9. native 打包 .fuz（Generator.Build.Voice.cs）或 出鬆散 wav（[04]）
 10. 寫到 Data/Sound/Voice/<plugin>/<voicetype>/<name>.<ext>
```
步驟 1、3–6、8–10 全可自動化；7 是 contingent 的 lip 步驟。`package` 接著把輸出掃進 zip。

**為何獨立 step（而非塞進 `build`）：** 語音生成慢（每行 GPU）、需要最終 esp、且有沉重的選用外部依賴。獨立讓 `build` 保持快又無依賴；`voicelines` 是 opt-in。同理由如 `compile` 與 `build` 分開。

---

## 3. 新 core 檔 `Generator.Build.Voice.cs`

Native、無 Wine、可單元測試（如 `Generator.SceneFragments.cs` / `QuestFragments.cs` 這些純、免 Wine 的）：
- `WriteFuz(byte[] audio, byte[]? lip)` —— ~20 行 fuz writer（[04] §1）。
- `VoiceFileName(quest, infoFormId, responseIndex, topic)` —— 決定性檔名規則（[04] §2），附斷言重現抽出 vanilla 名的測試。
- （僅 Tier 2）`SyntheticLip(float[] envelope, double durationSec)` —— envelope → phoneme-keyframe `.lip`（[03] §2/§3）。

維持 ≤300 行（CLAUDE.md）。shell-out 編排（TTS/Wine）住在另一個 `Voice.cs`（Core），對映 `Papyrus.cs` —— options class 帶 `null → MODFORGE_* → default` fallback。

---

## 4. 工具設定（env vars、條件式）

照 `Papyrus.cs`/`PapyrusOptions`：

| Env var | 指向 | 缺則 → |
|---------|-----------|----------|
| `MODFORGE_TTS_BIN` | `voicegen.py` venv wrapper（[01]） | skip-with-warn（不生語音） |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe`（走 Wine） | 跳過 xwm，出 WAV |
| `MODFORGE_FACEFX` | `FaceFXWrapper.exe`（Wine） | 跳過 lip（Tier 0）或用合成（Tier 2） |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | 僅在 `MODFORGE_FACEFX` 設時需要 |

每個工具缺 = 帶 warning 優雅降到下一個更低 tier，絕不硬失敗。這是既有的條件式-embed/條件式-工具姿態。

---

## 5. Package + build-pipeline wiring

- `Assets.cs` 已複製 `Sounds` 樹 —— 確保 `Sound/Voice/<plugin>/<voicetype>/` 被涵蓋（應該落在既有 `Sound/...` 複製裡；驗證 glob 觸及 `Voice/`）。
- `Package.cs` 扁平 MO2 組裝已處理 `Sound/...`；語音檔搭便車。**無 `.seq` 互動**（語音 ≠ StartGameEnabled quest）。
- 完整 build 順序：`build` → `voicelines` → `package`。記進 `SPEC-workflow.md`。

---

## 6. 維護鏈落點（落地時）

依 CLAUDE.md Workflow 1，落地時（非現在 —— 這是研究）：
- **程式碼：** `Spec.cs`（+`Spec.Actors.cs`）、`Generator.Build.Voice.cs`、`Voice.cs`、`Program.Build.cs`、`examples/spec.schema.json` + `sample_spec.json`。
- **CODE_MAP：** 把 `Generator.Build.Voice.cs` / `Voice.cs` 列入 `CODE_MAP.infra.md`；`voicelines` 命令入 CLI 表；spec 欄位 cross-ref 進 `CODE_MAP.dialogue-quests.md`（INFO/voiceType 住那）。加 Tests 列（`VoiceFileNameTests`、`FuzWriterTests`）。
- **文檔：** `voiceLine`/`voiceTemplate` 欄位入 `SPEC-dialogue-quests.md`（若長大則開新 `SPEC-voice.md`）；`voicelines` 入 `for_agent_cli.md` 與 `SPEC-workflow.md`。
- 新 diag `voicediag <esp>`（與 `identitydiag` 平行）可不跑遊戲就對照 esp 的 INFO records 驗證 emit 的檔名/路徑 —— 鑑於無聲失敗風險，價值很高。

---

## 7.「完成」長什麼樣

`modforge voicelines spec.json built.esp` 走訪 INFO、透過 `voicegen.py` 每行生 WAV、寫到正確的決定性路徑，`package` 打包進去 —— 設了 `MODFORGE_TTS_BIN`、其餘（xwm/lip）未設時優雅降級。手冊（[06]）就是這個 step 所自動化內容的 spec。

---

### 來源
內部慣例讀自 `docs/CODE_MAP.infra.md`、`src/ModForge.Core/Papyrus.cs`、`src/ModForge.Core/Spec.Actors.cs`、`src/ModForge.Cli/ModForge.Cli.csproj`。引擎/格式事實：見 [01]–[04]。
