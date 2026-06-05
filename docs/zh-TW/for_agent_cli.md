# 從 CLI 驅動 ModForge（JSON 規格 → 插件）

**預設**工作流程：你撰寫一份 JSON **規格**，CLI 產生一個合法的 `.esp`/`.esl`（+ 已編譯的 `.pex`）。規格就是合約——你不需要手寫插件位元組或 FormID。

← 索引：[for_agent.md](for_agent.md) · 規格欄位：[SPEC-index.md](SPEC-index.md) · 改用程式碼計算規格：[for_agent_lib.md](for_agent_lib.md)
· 擬真 NPC：[lifelike/](lifelike/README.md) · 引擎機制：[engine-internals.md](engine-internals.md)

## 你的工作，一個迴圈搞定

```
請求（自然語言）──▶ 撰寫 spec.json（依 SPEC-index.md）──▶ 驗證 ──（有錯則修正）──▶ build|package ──▶ dump（驗證）──▶ 如實回報
                                                  ▲___________________________|
```

完整的規格範例請見 `../examples/sample_spec.json`。

## 指令

```bash
cd /home/lorkhan/repo/ModForge
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q        # 建置一次（以及任何程式碼異動後）
R="dotnet run --project src/ModForge.Cli --no-build --"       # 之後快速驅動

$R validate <spec.json>                      # 務必先執行；非零退出 + 列出問題
$R build    <spec.json> <out.esp>            # 規格 -> 插件（records、dialogue、FormLinks、VMAD）
$R package  <spec.json> <outModDir>          # build + 編譯每個腳本的 `source` -> MO2 就緒資料夾
$R dump     <plugin.esp>                     # 回讀：records、名稱、npc 種族/職業/服裝/派系、武器/盔甲數值、效果、場景/放置、關鍵字、腳本、對話、目標、主檔
$R find     <plugin.esp> <query> [type]      # 搜尋主檔（例如 Skyrim.esm）-> "Skyrim.esm:0xFORMID  Type  EditorID"
$R compile  <script.psc> <outDir>            # .psc -> .pex，透過 Wine 底下的 CK PapyrusCompiler
$R extract  <plugin.esp> <strings.json>      # 擷取可翻譯字串 -> JSON（source/target）
$R apply    <plugin.esp> <strings.json> <out.esp>     # 寫回譯文（拉丁文字 / 內嵌）
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK：Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # 示範插件（驗證工具鏈是否正常）
$R smtree   <Skyrim.esm>                      # 列出 Story Manager 事件根（查詢事件根 FormID）
```

`--no-build` 需要先執行過 `dotnet build`；若不確定，可省略（速度較慢）。

## 引用原版表單（種族/職業/服裝/關鍵字/派系）

部分規格欄位為 **refs** — 接受規格內的 `editorId`，或外部原版表單 `"<master>:0xFORMID"`（例如 `"Skyrim.esm:0x013746"` = NordRace）。主檔會自動加入。若要查找原版 FormID，請搜尋遊戲主檔：

```bash
SKYRIM_ESM="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R find "$SKYRIM_ESM" nordrace Race        # -> Skyrim.esm:0x013746  Race  NordRace
$R find "$SKYRIM_ESM" blacksmith Class     # -> Skyrim.esm:0x013257  Class VendorBlacksmith
$R find "$SKYRIM_ESM" armorclothing Keyword
$R find "$SKYRIM_ESM" restorehealth MagicEffect  # -> Skyrim.esm:0x03EB15  AlchRestoreHealth（用於藥水的 `effects`）
$R find "$SKYRIM_ESM" banneredmare Cell    # -> Skyrim.esm:0x01605E  WhiterunBanneredMare（室內 `placement` 場景）
$R find "$SKYRIM_ESM" tamriel Worldspace   # -> Skyrim.esm:0x00003C  Tamriel（室外 `placement` 世界空間）
```
務必執行 `find` 取得真實 FormID — **絕不要猜測**。搜尋依據為 EditorID（描述性名稱，例如 `NordRace`）；本地化顯示名稱在無頭模式下無法解析。一個靜態 NPC 至少需要 `race` + `class` 才能表現得像真正的角色；`outfit` 則為其穿上衣物。

## 產生內容的工作流程

1. 閱讀 `SPEC-index.md` 了解確切欄位。撰寫 `spec.json`（camelCase；屬性名稱不區分大小寫匹配）。對於種族/職業/服裝/關鍵字/原版派系，請先用 `find` 取得 FormID，並使用 `"<master>:0xFORMID"` 的引用格式。
2. 執行 `validate spec.json`。**若回報問題，請修正規格並重新驗證** — 不要建置無效的規格。此步驟會檢查：空白/重複的 `editorId`、對話→未知任務/NPC、腳本→未知目標、物件屬性→未知記錄、錯誤的屬性型別。
3. 執行 `package spec.json OutDir`（或只需插件時用 `build spec.json out.esp`）。
4. 執行 `dump OutDir/<pluginName>` 並**確認輸出符合需求**（名稱、派系成員、附加腳本 + 屬性數量、對話提示、任務目標）。
5. 回報你所產生的內容，並如實說明哪些僅為結構性（見「限制」章節）。

## 翻譯工作流程

1. 執行 `extract some.esp strings.json` → 每個條目包含 `source` + 空白 `target`。
2. 填入每個 `target` 的譯文（編輯 JSON）。
3. **中文（或任何 CJK）：** 執行 `applyloc some.esp strings.json OutDir` → 產生 `OutDir/<plugin>.esp` + `OutDir/Strings/<plugin>_chinese.STRINGS`（UTF-8，小寫後綴——已對照官方 CHS 模組驗證）。**拉丁文字：** 使用 `apply some.esp strings.json out.esp`（內嵌）。
4. 驗證：對結果執行 `dump`，或解碼 `_chinese.STRINGS`（UTF-8）以確認文字內容。

## 環境需求

- **.NET 8 或 10 SDK**（`dotnet`）。NuGet 會在首次建置時還原 `Mutagen.Bethesda.Skyrim`。
- **遊戲 `Data` 資料夾** — 僅在放置於**原版**場景時需要（工具會讀取主檔以覆寫場景）。預設為 Steam 路徑；可用 `MODFORGE_SKYRIM_DATA` 覆寫。
- **Papyrus**（`compile`，以及 `package` 中腳本含有 `source` 時）：需要 `wine` + Creation Kit 的 `PapyrusCompiler.exe` + 原版基礎腳本原始碼。預設假設本機 CK Steam 安裝路徑及 `~/.cache/modforge/papyrus/Source/Scripts`。可用環境變數覆寫：
  - `MODFORGE_PAPYRUS_COMPILER` = `PapyrusCompiler.exe` 的路徑
  - `MODFORGE_PAPYRUS_BASE` = 存放基礎 `.psc` + `TESV_Papyrus_Flags.flg` 的目錄
  - 一次性設定：`unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`（約 14k 個 `.psc`）。若腳本使用 SKSE 函式，請將 SKSE `.psc` 加入該目錄。

## 常見陷阱（這些問題真的會讓你踩坑）

- **`editorId`** 是你在規格中的引用鍵——不可為空，且在整份規格中必須唯一。它不是 FormID；FormID 與主檔由 Mutagen 指派。記錄之間以 `editorId` 互相引用。
- `dialogue` 需要真實的 `questEditorId`（規格中存在的任務）；若設定了 `speakerNpcEditorId`，其對象必須是 NPC。`script` 的 `targetEditorId` 必須存在；`object` 屬性的 `objectEditorId` 也必須存在。`validate` 會強制檢查所有這些規則。
- `script` 的 `scriptName` 必須與編譯後 `.pex` 的 `Scriptname` 相同，且 `.psc` 檔名也必須與 `Scriptname` 一致。
- **ESL**（`esl: true`，預設）：最多 2048 筆新記錄（FormID 0x800–0xFFF）；超出時寫入時會有明確錯誤。
- **CJK**：只有 `applyloc` 才能產生遊戲可讀的中文（Localized UTF-8）。直接內嵌的字串會將中文轉為 `?`（引擎使用 cp1252 編碼）。CJK 文字請勿使用 `apply`/`build`。
- Papyrus 編譯器即使失敗也會回傳退出碼 0；本工具已自動抓取 stdout——若你自行呼叫 `wine PapyrusCompiler.exe`，請做同樣處理，並確認 `.pex` 是否存在。

## 限制 — 如實回報，切勿誇大

ModForge 寫出的是**結構上合法**的記錄，這與**遊戲中可正常運作**並不相同，且你無法從此處確認遊戲內行為（那需要實際啟動 Proton/Skyrim）。關於哪些功能可運作與哪些不可運作的完整說明——以及如實回報的規則——請見索引：**[for_agent.md → 限制](for_agent.md#限制--請如實說明不要過度宣稱)**。回報結果前請先閱讀。
