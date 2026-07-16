# 從 CLI 驅動 ModForge（JSON spec → plugin）

**預設**工作流程：你撰寫一份 JSON **spec**，CLI 產出一個有效的 `.esp`/`.esl`
（＋編譯好的 `.pex`）。spec 就是契約——你絕不手寫 plugin 位元組或 FormID。

← 索引：[for_agent.md](for_agent.md) · spec 欄位：[SPEC-index.md](spec/SPEC-index.md) · 改用程式碼計算 spec：[for_agent_lib.md](for_agent_lib.md)
· 擬真 NPC：[lifelike/](lifelike/README.md) · 引擎機制：[engine-internals.md](engine-internals.md)

## 你的工作，濃縮成一個迴圈

```
request (NL) ──▶ write spec.json (per SPEC-index.md) ──▶ validate ──(fix on errors)──▶ build|package ──▶ dump (verify) ──▶ report honestly
                                                  ▲___________________________|
```

一份完整的 spec 範例是 `../examples/sample_spec.json`。

## 指令

```bash
cd /home/lorkhan/repo/moddings/skyrim/projects/ModForge
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q        # build once (and after any code change)
R="dotnet run --project src/ModForge.Cli --no-build --"       # then drive it fast

$R validate <spec.json>                      # ALWAYS run first; exits non-zero + lists problems
$R build    <spec.json> <out.esp>            # spec -> plugin (records, dialogue, FormLinks, VMAD)
$R package  <spec.json> <outModDir>          # build + compile each script `source` -> MO2-ready folder
$R dump     <plugin.esp>                     # read back: records, names, npc race/class/outfit/factions, weapon/armor stats, effects, cells/placements, keywords, scripts, dialogue, objectives, masters
$R find     <plugin.esp> <query> [type]      # search a master (e.g. Skyrim.esm) -> "Skyrim.esm:0xFORMID  Type  EditorID"  (query 可為 0xFORMID 反查：「這個 FormID 是什麼記錄？」)
$R compile  <script.psc> <outDir>            # .psc -> .pex via the CK PapyrusCompiler under Wine
$R extract  <plugin.esp> <strings.json>      # pull translatable strings -> JSON (source/target)
$R apply    <plugin.esp> <strings.json> <out.esp>     # write targets back (Latin scripts / inline)
$R applyloc <plugin.esp> <strings.json> <outModDir>   # CJK: Localized UTF-8 <plugin>_chinese.STRINGS
$R gen      <out.esp>                         # demo plugin (sanity check the toolchain)
$R smtree   <Skyrim.esm>                      # list Story Manager event roots (find an event root FormID)
$R navdiag  <plugin.esp>                      # 列出 plugin 內每張 NAVM，並把每張 override 的 NVNM 與 master 逐位元組比對（IDENTICAL / DIFF）。用了 navmeshOverrides[] 就跑這個
```

不帶參數執行 `$R` 會列出完整指令表——大多數記錄族都有對應的 `*diag` 探針（`questdiag`／`packagediag`／`landdiag`／`navdiag`…），把單一記錄的欄位印出來，方便拿你生的跟同類 vanilla 記錄比對。

`--no-build` 需要先做過一次 `dotnet build`；不確定時就拿掉它（較慢）。

## 引用原版 forms（race/class/outfit/keywords/factions）

某些 spec 欄位是 **refs**——它們接受一個 spec 內的 `editorId`，或一個外部原版 form
`"<master>:0xFORMID"`（例如 `"Skyrim.esm:0x013746"` = NordRace）。master 會自動加入。
要找到原版 FormID，請搜尋遊戲 master：

```bash
SKYRIM_ESM="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R find "$SKYRIM_ESM" nordrace Race        # -> Skyrim.esm:0x013746  Race  NordRace
$R find "$SKYRIM_ESM" blacksmith Class     # -> Skyrim.esm:0x013257  Class VendorBlacksmith
$R find "$SKYRIM_ESM" armorclothing Keyword
$R find "$SKYRIM_ESM" restorehealth MagicEffect  # -> Skyrim.esm:0x03EB15  AlchRestoreHealth (for a potion `effects`)
$R find "$SKYRIM_ESM" banneredmare Cell    # -> Skyrim.esm:0x01605E  WhiterunBanneredMare (interior `placement` cell)
$R find "$SKYRIM_ESM" tamriel Worldspace   # -> Skyrim.esm:0x00003C  Tamriel (exterior `placement` worldspace)
$R find "$SKYRIM_ESM" 0x000D4B52           # 反查：0xFORMID -> 記錄型別（如確認 placement base 是 STAT 非 REFR）
```
一律執行 `find` 來取得真正的 FormID——**絕不用猜的**。搜尋是依 EditorID
（具描述性，例如 `NordRace`）；本地化顯示名稱無法在 headless 下解析。一個站立的
NPC 至少需要 `race` ＋ `class` 才能像真正的 actor 一樣行動；`outfit` 替它穿上衣物。

## 引用某個 MOD——它就變成 master（安裝必要條件）

同一套語法吃任何 plugin：`"PROTEUS.esp:0x08073D"`。這會讓 **PROTEUS.esp 成為你 plugin 的
master**——而 **Skyrim 對「缺 master」的 plugin 是靜默不載**：沒有錯誤、沒有 log，記錄在遊戲裡
就是不存在。這不是 bug，ModForge 也不會過濾（`sc capp` 的玩家分身若把 mod 給的法術全丟掉，
那就不再是「你」了），但你必須知道它發生了。所以 `build` **會告訴你**，而且會講是哪個 spec 欄位
把它拉進來的：

```
7 non-vanilla master(s) — the plugin will NOT load for anyone missing them (Skyrim drops it silently):
  ImGladYoureHere.esp  (3 link(s))
      ← capturedNpcs[0].spells[10] = ImGladYoureHere.esp:0x18D2A1
      … +6 more
  PROTEUS.esp  (1 link(s))
      ← capturedNpcs[0].spells[17] = PROTEUS.esp:0x08073D
wrote MFCapHatak.requires.txt (the install requirements, with the spec field behind each one)
```

- **原版 masters**（`Skyrim.esm`、`Update.esm`、`Dawnguard.esm`、`HearthFires.esm`、
  `Dragonborn.esm`）永遠不列——每個安裝都有。
- **Creation Club**（`ccBGSSSE001-Fish.esm`、`_ResourcePack.esl`…）**要列**：它是按帳號購買的，
  沒買的玩家一樣卡死。
- **`<plugin>.requires.txt`** 寫在 .esp 旁邊（沒有非原版依賴時會刪掉舊檔）。請把它跟 plugin 放一起：
  這是「這份 build 依賴誰」唯一的記錄。每個 master 底下列出所有「指到它的 form」的 spec 欄位——
  想拔掉某個依賴，就把那個 master 底下的欄位**全部**刪掉再 build。
- `package` 印同一份摘要但**不寫**旁檔（它的輸出資料夾就是要出貨的 mod）。

.esp 本身一個 byte 都不變——這純粹是可見性。若 plugin 必須可攜，就別引用 mod 的內容：
手寫 spec、只用原版 forms。

### `requires[]`——宣告它們，build 會強制檢查

只回報還擋不住**漂移**：mod 被移除、capture 重新做一次、某行被刪掉——plugin 的 master 清單
就悄悄變了，這正是上面那種靜默不載的失敗。所以 spec 可以**宣告**自己需要什麼，兩者不一致時
`build` 會拒絕寫出 plugin：

```json
"requires": [
  "XPMSE.esp",
  { "plugin": "PROTEUS.esp", "version": "3.4+", "reason": "the captured player's spells" },
  { "name": "PapyrusUtil SE", "reason": "storageWrites (SKSE plugin — has no .esp)" }
]
```

- **有 link 但沒宣告 → 報錯，.esp 完全不寫出**（訊息會指名是哪個 spec 欄位，你可以刪掉那行，
  或把該 master 補進宣告）；
- **宣告了但從沒 link → 警告**（過期的行；只在執行期需要、沒有自己 plugin 的 mod 該歸到 `name`，
  那是純文件、永不檢查）；
- **完全沒有 `requires` 段落 → 不檢查任何東西**（每一份舊 spec 都不受影響——寫這個段落才算選擇
  加入）。`"requires": []` 也是一種選擇加入：代表*只用 vanilla*，所以任何 mod ref 都會讓 build 失敗。

**`build spec.json out.esp --sync-requires`** 把實際的 master 集合寫回 spec 的 `requires[]`（保留
你寫的 `reason`／`version`／`url`，刪掉過期的項目，若原本沒有這個段落就建立它）。在 capture 之後
用它——它把相依關係的變動變成 spec diff 裡一行可審閱的紀錄。

⚠️ **沒有版本檢查，也不可能有。** `.esp` 本身不帶 mod 版本：`TES4`／`HEDR` 的「version」是檔案
*格式*版本（PROTEUS 3.4 跟一份兩筆記錄的測試 plugin 都是 1.70/1.71），`CNAM`／`SNAM` 是自由文字
（通常是 `DEFAULT`／空白）。只有 mod *manager* 才知道版本（MO2 的 `meta.ini`，來自 Nexus）。
`requires[]` 裡的 `version` 只是**給人看的標籤**，會印在 `<plugin>.requires.txt` 裡並標記為未驗證。

## 產生內容工作流程

1. 閱讀 `SPEC-index.md` 取得確切欄位。撰寫 `spec.json`（camelCase；屬性名稱以
   不分大小寫的方式比對）。對於 race/class/outfit/keywords/原版 factions，先用 `find`
   取得 FormID，再使用 `"<master>:0xFORMID"` ref 形式。
2. 執行 `validate spec.json`。**若它回報問題，請修正 spec 並重新驗證**——別
   build 一份無效的 spec。它會抓出：空白／重複的 `editorId`、dialogue→未知的 quest/npc、
   script→未知的 target、object-property→未知的 record、錯誤的 property type、**未知的 spec
   欄位**（拼字防護——遞迴檢查每個 JSON key 是否符合 C# spec type；略過
   `_*` / `//*` 註解 key）。
3. 執行 `package spec.json OutDir`（或只需 plugin 時用 `build spec.json out.esp`）。
4. 執行 `dump OutDir/<pluginName>` 並**確認輸出符合 request**（名稱、faction
   成員資格、附加的 scripts ＋ property 數量、dialogue 提示、quest objectives）。
5. 回報你產出了什麼，並如實說明哪些僅為結構性（見「限制」章節）。

## 翻譯工作流程

1. 執行 `extract some.esp strings.json` → 每個條目都有 `source` ＋空白的 `target`。
2. 把每個 `target` 填上你的翻譯（編輯該 JSON）。
3. **中文（或任何 CJK）：** 執行 `applyloc some.esp strings.json OutDir` → 產生 `OutDir/<plugin>.esp`
   ＋ `OutDir/Strings/<plugin>_chinese.STRINGS`（UTF-8，小寫後綴——已對照
   官方 CHS 模組驗證）。**拉丁文字：** 使用 `apply some.esp strings.json out.esp`（內嵌）。
4. 驗證：對結果執行 `dump`，或解碼 `_chinese.STRINGS`（UTF-8）以確認文字內容。

## 環境前置需求

- **.NET 8 或 10 SDK**（`dotnet`）。NuGet 會在首次 build 時還原 `Mutagen.Bethesda.Skyrim`。
- **遊戲 `Data` 資料夾**——僅在放置進**原版** cell 時需要（它會讀取 master 以
  覆寫該 cell）。預設為 Steam 路徑；可用 `MODFORGE_SKYRIM_DATA` 覆寫。
- **Papyrus**（`compile`，以及 `package` 中腳本含有 `source` 時）：需要 `wine` ＋
  Creation Kit 的 `PapyrusCompiler.exe` ＋原版基礎腳本原始碼。預設假設本機 CK Steam
  安裝路徑及 `~/.cache/modforge/papyrus/Source/Scripts`。可用環境變數覆寫：
  - `MODFORGE_PAPYRUS_COMPILER` = `PapyrusCompiler.exe` 的路徑
  - `MODFORGE_PAPYRUS_BASE` = 存放基礎 `.psc` ＋ `TESV_Papyrus_Flags.flg` 的目錄
  - 一次性設定：`unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`
    （約 14k 個 `.psc`）。若腳本使用 SKSE 函式，請將 SKSE `.psc` 加入該目錄。

## 常見陷阱（這些問題真的會讓你踩坑）

- **`editorId`** 是你在 spec 中的引用 key——不可為空，且在整份 spec 中必須唯一。它
  不是 FormID；Mutagen 會指派 FormID/masters。記錄之間以 `editorId` 互相引用。
- `dialogue` 需要真實的 `questEditorId`（spec 中存在的某個 quest）；若設定了 `speakerNpcEditorId`，
  其對象必須是一個 npc。`script` 的 `targetEditorId` 必須存在；`object` 屬性的
  `objectEditorId` 也必須存在。`validate` 會強制檢查所有這些規則。
- `script` 的 `scriptName` 必須與編譯後 `.pex` 的 `Scriptname` 相同，且 `.psc`
  檔名也必須與該 `Scriptname` 一致。
- **ESL**（`esl: true`，預設）：≤ 2048 筆新記錄（FormID 0x800–0xFFF）；超出時於寫入時會有明確錯誤。
- **CJK**：只有 `applyloc` 才能產生遊戲可讀的中文（Localized UTF-8）。直接內嵌的
  字串會將中文轉為 `?`（引擎使用 cp1252 編碼）。CJK 文字請勿使用 `apply`/`build`。
- Papyrus 編譯器即使失敗也會回傳退出碼 0；本工具已自動抓取 stdout——
  若你曾自行呼叫 `wine PapyrusCompiler.exe`，請做同樣處理，並確認 `.pex` 是否存在。

## 限制——如實回報，切勿誇大

ModForge 寫出的是**結構上合法**的記錄，這與**遊戲中可正常運作**並不相同，
且你無法從此處確認遊戲內行為（那需要實際啟動 Proton/Skyrim）。關於哪些功能可運作
與哪些不可運作的完整說明——以及如實回報的規則——位於索引中：
**[for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim)**。回報結果前請
先閱讀。
