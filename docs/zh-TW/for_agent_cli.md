# 從 CLI 驅動 ModForge（JSON 規格 → 插件）

**預設**工作流程：你撰寫一份 JSON **規格**，CLI 產生一個合法的 `.esp`/`.esl`（+ 已編譯的 `.pex`）。規格就是合約——你永遠不需要手寫插件位元組或 FormID。

← 索引：[for_agent.md](for_agent.md) · 規格欄位：[SPEC-index.md](SPEC-index.md) · 改用程式碼計算規格：[for_agent_lib.md](for_agent_lib.md)
· 擬真 NPC：[lifelike/](lifelike/README.md) · 引擎機制：[engine-internals.md](engine-internals.md)

## 你的工作，一個迴圈搞定

```
request (NL) ──▶ write spec.json (per SPEC-index.md) ──▶ validate ──(fix on errors)──▶ build|package ──▶ dump (verify) ──▶ report honestly
                                                  ▲___________________________|
```

完整的規格範例請見 `../examples/sample_spec.json`。

## 指令

```bash
cd /home/lorkhan/repo/ModForge
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q        # build once (and after any code change)
R="dotnet run --project src/ModForge.Cli --no-build --"       # then drive it fast

$R validate <spec.json>                      # ALWAYS run first; exits non-zero + lists problems
$R build    <spec.json> <out.esp>            # spec -> plugin (records, dialogue, FormLinks, VMAD)
$R package  <spec.json> <outModDir>          # build + compile each script `source` -> MO2-ready folder
$R voicediag <spec.json> <built.esp>         # offline: INFO -> speaker -> voiceType -> template -> expected voice path
$R voicelines <spec.json> <built.esp> --plan # same plan without generating TTS
$R voicelines <spec.json> <built.esp>        # generate Sound/Voice/<plugin>/<voiceType>/*.fuz|wav
$R dump     <plugin.esp>                     # read back: records, names, npc race/class/outfit/factions, weapon/armor stats, effects, cells/placements, keywords, scripts, dialogue, objectives, masters
$R find     <plugin.esp> <query> [type]      # search a master (e.g. Skyrim.esm) -> "Skyrim.esm:0xFORMID  Type  EditorID"
$R compile  <script.psc> <outDir>            # .psc -> .pex via the CK PapyrusCompiler under Wine
$R extract  <plugin.esp> <strings.json>      # pull translatable strings -> JSON (source/target)
$R apply    <plugin.esp> <strings.json> <out.esp>     # write targets back (Latin scripts / inline)
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK: Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # demo plugin (sanity check the toolchain)
$R smtree   <Skyrim.esm>                      # list Story Manager event roots (find an event root FormID)
```

`--no-build` 需要先執行過 `dotnet build`；若不確定，可省略（速度較慢）。

## 引用原版表單（race/class/outfit/keywords/factions）

部分規格欄位為 **refs** — 它們接受規格內的 `editorId`，或外部原版表單 `"<master>:0xFORMID"`（例如 `"Skyrim.esm:0x013746"` = NordRace）。主檔會自動加入。若要查找原版 FormID，請搜尋遊戲主檔：

```bash
SKYRIM_ESM="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R find "$SKYRIM_ESM" nordrace Race        # -> Skyrim.esm:0x013746  Race  NordRace
$R find "$SKYRIM_ESM" blacksmith Class     # -> Skyrim.esm:0x013257  Class VendorBlacksmith
$R find "$SKYRIM_ESM" armorclothing Keyword
$R find "$SKYRIM_ESM" restorehealth MagicEffect  # -> Skyrim.esm:0x03EB15  AlchRestoreHealth (for a potion `effects`)
$R find "$SKYRIM_ESM" banneredmare Cell    # -> Skyrim.esm:0x01605E  WhiterunBanneredMare (interior `placement` cell)
$R find "$SKYRIM_ESM" tamriel Worldspace   # -> Skyrim.esm:0x00003C  Tamriel (exterior `placement` worldspace)
```
務必執行 `find` 取得真實 FormID — **絕不要猜測**。搜尋依據為 EditorID（描述性名稱，例如 `NordRace`）；本地化顯示名稱在無頭模式下無法解析。一個站立的 NPC 至少需要 `race` + `class` 才能表現得像真正的角色；`outfit` 則為其穿上衣物。

## 產生內容的工作流程

1. 閱讀 `SPEC-index.md` 了解確切欄位。撰寫 `spec.json`（camelCase；屬性名稱不區分大小寫匹配）。對於 race/class/outfit/keywords/原版 factions，請先用 `find` 取得 FormID，並使用 `"<master>:0xFORMID"` 的引用格式。
2. 執行 `validate spec.json`。**若回報問題，請修正規格並重新驗證** — 不要建置無效的規格。它會捕捉：空白/重複的 `editorId`、dialogue→未知的 quest/npc、script→未知的目標、object-property→未知的記錄、錯誤的屬性型別、**未知的規格欄位**（拼字保護 — 遞迴檢查每個 JSON key 是否符合 C# 規格型別；略過 `_*` / `//*` 注釋 key）。
3. 執行 `package spec.json OutDir`（或只需插件時用 `build spec.json out.esp`）。
4. 執行 `dump OutDir/<pluginName>` 並**確認輸出符合需求**（名稱、派系成員、附加腳本 + 屬性數量、對話提示、任務目標）。
5. 回報你所產生的內容，並如實說明哪些僅為結構性（見「限制」章節）。

### Voice lines workflow

Voice files 不是 plugin record，也不嵌入 ESP/ESM；Skyrim 讀取 loose path
`Sound/Voice/<plugin>/<voiceType>/<quest>_<topic>_<infoFormId>_<response>.fuz|wav`。

1. 在 spec 中設定 `voiceTemplates[]`，並讓 NPC 用 `voiceTemplate` 指向它；`voiceType` 決定資料夾名稱。
2. `build` 或 `package` 產出 plugin 後，先跑 `voicediag` 或 `voicelines --plan`。這不需要 TTS，能在花 GPU 時間前抓 speaker/template/path 問題。
3. 設定 `MODFORGE_TTS_BIN` 指向本機 `voicegen.py` wrapper。`engine: "f5"` 直接由 wrapper 生成；`engine: "fish-s2"` 會再轉呼 `MODFORGE_FISH_SPEECH_BIN`。`MODFORGE_XWMAENCODE` 指向 CK/DirectX 的 `xWMAEncode.exe`；`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA` 用於 lip。
4. 最穩的包裝順序是先 `package` 到最終 mod folder，再對該資料夾中的 plugin 跑 `voicelines`；或 `build` + `voicelines` 到 staging dir，再 `package --assets <stagingDir>`。

## 翻譯工作流程

1. 執行 `extract some.esp strings.json` → 每個條目包含 `source` + 空白 `target`。
2. 填入每個 `target` 的譯文（編輯 JSON）。
3. **中文（或任何 CJK）：** 執行 `applyloc some.esp strings.json OutDir` → 產生 `OutDir/<plugin>.esp` + `OutDir/Strings/<plugin>_chinese.STRINGS`（UTF-8，小寫後綴——已對照官方 CHS 模組驗證）。**拉丁文字：** 使用 `apply some.esp strings.json out.esp`（內嵌）。
4. 驗證：對結果執行 `dump`，或解碼 `_chinese.STRINGS`（UTF-8）以確認文字內容。

## 環境前置需求

- **.NET 8 或 10 SDK**（`dotnet`）。NuGet 會在首次建置時還原 `Mutagen.Bethesda.Skyrim`。
- **遊戲 `Data` 資料夾** — 僅在放置於**原版** cell 時需要（它會讀取主檔以覆寫該 cell）。預設為 Steam 路徑；可用 `MODFORGE_SKYRIM_DATA` 覆寫。
- **Papyrus**（`compile`，以及 `package` 中腳本含有 `source` 時）：需要 `wine` + Creation Kit 的 `PapyrusCompiler.exe` + 原版基礎腳本原始碼。預設假設本機 CK Steam 安裝路徑及 `~/.cache/modforge/papyrus/Source/Scripts`。可用環境變數覆寫：
  - `MODFORGE_PAPYRUS_COMPILER` = `PapyrusCompiler.exe` 的路徑
  - `MODFORGE_PAPYRUS_BASE` = 存放基礎 `.psc` + `TESV_Papyrus_Flags.flg` 的目錄
  - 一次性設定：`unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`（約 14k 個 `.psc`）。若腳本使用 SKSE 函式，請將 SKSE `.psc` 加入該目錄。

## 常見陷阱（這些問題真的會讓你踩坑）

- **`editorId`** 是你在規格中的引用鍵——不可為空，且在整份規格中必須唯一。它不是 FormID；Mutagen 會指派 FormID/主檔。記錄之間以 `editorId` 互相引用。
- `dialogue` 需要真實的 `questEditorId`（規格中存在的任務）；若設定了 `speakerNpcEditorId`，其對象必須是一個 npc。`script` 的 `targetEditorId` 必須存在；`object` 屬性的 `objectEditorId` 也必須存在。`validate` 會強制檢查所有這些規則。
- `script` 的 `scriptName` 必須與編譯後 `.pex` 的 `Scriptname` 相同，且 `.psc` 檔名也必須與 `Scriptname` 一致。
- **ESL**（`esl: true`，預設）：≤ 2048 筆新記錄（FormID 0x800–0xFFF）；超出時寫入時會有明確錯誤。
- **CJK**：只有 `applyloc` 才能產生遊戲可讀的中文（Localized UTF-8）。直接內嵌的字串會將中文轉為 `?`（引擎使用 cp1252 編碼）。CJK 文字請勿使用 `apply`/`build`。
- Papyrus 編譯器即使失敗也會回傳退出碼 0；本工具已自動抓取 stdout——若你曾自行呼叫 `wine PapyrusCompiler.exe`，請做同樣處理，並確認 `.pex` 是否存在。

## 限制 — 如實回報，切勿誇大

ModForge 寫出的是**結構上合法**的記錄，這與**遊戲中可正常運作**並不相同，且你無法從此處確認遊戲內行為（那需要實際啟動 Proton/Skyrim）。關於哪些功能可運作與哪些不可運作的完整說明——以及如實回報的規則——位於索引中：**[for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim)**。回報結果前請先閱讀。
