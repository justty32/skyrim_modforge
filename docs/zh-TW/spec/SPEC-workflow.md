# ModForge spec — 工作流與缺口

← [index](SPEC-index.md)

## 工作流

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` 會佈置出 `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`。

**NL → spec：** 把你想要的東西描述給 AI agent（Claude Code）；agent 會產出一份符合本文件／
`../examples/spec.schema.json` 的 spec（依 `for_agent.md`），執行 `validate`
（遇到問題時自我修正），然後 `build`／`package`。這個由 agent 驅動的迴圈**就是**
NL→spec 層——並沒有工具內建的 LLM API（曾經規劃的 `describe` 命令已捨棄），
所以沒有 API key／供應商需要設定。

## `requires[]`——宣告這個 plugin 需要哪些 mod（build 會強制檢查）

只要 spec 裡任何地方寫了 `"PROTEUS.esp:0x08073D"`，就會讓 **PROTEUS.esp 成為輸出的 master**，而
**Skyrim 對「缺 master」的 plugin 會靜默拒絕載入**——沒有錯誤、沒有 log，記錄在遊戲裡就是不存在。
`build` 一定會*回報*它連結到的 masters（並寫出 `<plugin>.requires.txt`）。`requires[]` 更進一步：
它是作者**宣告**的內容，兩者不一致時 build 會**失敗**。

```json
{
  "pluginName": "MyMod.esp",
  "requires": [
    "XPMSE.esp",
    { "plugin": "PROTEUS.esp", "version": "3.4+", "reason": "the captured player's spells",
      "url": "https://www.nexusmods.com/skyrimspecialedition/mods/62934" },
    { "name": "PapyrusUtil SE", "reason": "storageWrites (SKSE plugin — has no .esp)" }
  ]
}
```

| Field | Meaning |
|-------|---------|
| `plugin` | build 預期會連結到的 master（`.esp`／`.esm`／`.esl`）。**雙向都會檢查。** 清單裡的純字串就是它的簡寫形式。 |
| `name` | **沒有自己 plugin** 的需求（SKSE DLL、loose-file 框架）。它永遠不會是 master，所以**純文件、永不檢查**——但仍會寫進旁檔，也就是玩家讀到的需求清單。 |
| `version` | **純文件——ModForge 無法驗證。** Skyrim plugin 不帶 mod 版本（見下）。只印給人看，不強制檢查。 |
| `reason` | 為什麼需要這個 mod。`--sync-requires` 會自動用拉進這個 master 的 spec 欄位填入。 |
| `url` | 去哪裡取得——會寫進旁檔。 |

**兩個檢查：**

- **有 link 但沒宣告 → 報錯，什麼都不寫出。** 這正是這個功能要抓的漂移：plugin 剛剛多了一個
  沒人簽收的安裝必要條件。訊息會指名確切的 spec 欄位（`capturedNpcs[0].spells[2] = PROTEUS.esp:0x08073D`），
  你可以刪掉那行，也可以把這個 master 補進宣告。
- **宣告了但從沒 link → 警告。** 過期／複製貼上留下的行。（如果 mod 是*執行期*需要、但沒有任何
  記錄引用它，它就不是 master：改用 `name` 宣告。）

**天生向後相容：** 一份**沒有 `requires` 段落**的 spec 完全不檢查——寫這個段落才算選擇加入。
`"requires": []` 也是一種選擇加入：它宣告「這個 mod 只用 vanilla」，所以之後任何 mod ref 都會讓 build 失敗。

**`build --sync-requires`** 會把 build 實際連結到的 masters 寫回 spec 的 `requires[]`（若原本沒有這個
段落就建立它，刪掉過期的項目，保留你寫的 `reason`／`version`／`url`，永不動到 `name` 項目）：

```bash
dotnet run --project src/ModForge.Cli -- build myspec.json out.esp --sync-requires
```
一次 capture（`sc cap`／`sc capp`）會為每個 mod 給的 spell/perk/item 拉進一個相依，靠手動維護
這份清單會讓這個約定不值得存在。同步而非靜默處理的重點在於：相依集合會變成**spec diff 裡的一行**——
你的 mod 需要什麼有了變動，就會像其他變更一樣出現在 `git diff` 裡。

**為什麼沒有版本*檢查*：** `.esp` 沒有 mod 版本。它的 `TES4`／`HEDR`「version」是檔案**格式**版本
（1.70/1.71——PROTEUS 3.4 跟一份兩筆記錄的測試 plugin 完全一樣）；`CNAM`／`SNAM`（作者／描述）是
自由文字，通常是 `DEFAULT` 或空白。真正的版本資訊只存在於 **mod manager** 的中繼資料裡
（MO2 `meta.ini` 的 `version=`，來自 Nexus），不在 plugin 裡，build 也看不到。所以 `version` 是
給人看的標籤，並在 `<plugin>.requires.txt` 裡被標成如此；ModForge 不會假裝驗證它。

## Voice（TTS 聲音克隆 → .fuz）

選用的後置 build 流程，會為已 build plugin 內的每一句對話合成配音音訊（外加唇形同步）。
只用外部工具——不綁帶任何東西。
**遊戲內已於 2026-06-13 確認**，使用真正的 F5-TTS（克隆的聲音在自訂 NPC 上播放）。真實模型的
設定筆記（Blackwell→torch cu128、當 `ref_text=""` 時 F5 會自動轉錄 ref、xWMAEncode 可直接吃 F5 的
24 kHz PCM、空的自訂 cell 會讓 NPC 掉落 → 改用原版 interior）放在 CLAUDE.md。

**Spec 欄位**

- `voiceTemplates[]` — 具名的克隆配方，由 NPC 引用：
  - `id` — 唯一的 template 名稱。
  - `engine` — `f5` | `fish-s2` | `chatterbox` | `gptsovits` | `xtts`。`f5` 由
    隨附的本機 `voicegen.py` 處理；`fish-s2` 透過 `MODFORGE_FISH_SPEECH_BIN`（一個寫出 WAV 的本機
    Fish Speech wrapper）路由。其餘名稱保留，直到對應的 wrapper 存在為止。
  - `referenceWav` + `referenceText` — zero-shot 的參考片段及其轉錄文字
    （路徑相對於 spec 檔；f5 需要轉錄文字）。
  - `modelPath` — 選用的微調模型目錄（相對於 spec）。
  - `rvcModel` — 選用的 RVC 模型，用於音色穩定化。
  - `seed` — 決定性輸出。
  - `speed` / `exaggeration` / `language` — 生成調校；三者連同其餘設定都會
    一併傳遞給 TTS 程序。
- `npcs[].voiceTemplate` — ref → 某個 `voiceTemplates` id；把該 NPC 的台詞路由到
  克隆引擎。與 `npcs[].voiceType` 不同，後者是遊戲內的 VTYP record ref
  （你仍然需要一個 voiceType——它決定輸出資料夾，見下文）。
- `voiceSpeakers[]` — 配音台詞，其說話者為**外部** NPC（來自另一份 master，已 build 的
  plugin 無法解析——例如像 Sofia 這樣既有的隨從，透過手動的
  `GetIsID(<master>:0xFORMID)` 條件閘控）。每筆 `{ speaker, voiceType, template }` 將該
  NPC ref → 其 `voiceType`（資料夾名稱，例如 `JJSofiaVoiceType`）→ 某個 `voiceTemplates` id 綁定起來。少了
  它，說話者就無法解析（mod-only cache），該句也就沒有配音。**從隨從自己的 BSA 抽取一段克隆 ref**，
  使用 `extract-voices <Follower.bsa> <VoiceType> <outDir> <Follower.esp>`
  （選用的第 4 個參數會讓 BSA voice 路徑以該 plugin 為鍵，而非 Skyrim.esm）。這就是讓一個
  既有的全配音隨從用自己的聲音評論新內容的做法。
- `voiceLine`（全域，選用）— 輸出設定：`format`（`fuz` | `wav` | `xwm`，
  預設 `fuz`）與 `skipLip`（true = 跳過 .lip 生成，嘴形靜止）。

**環境變數**

| Var | Tool | Needed for |
|-----|------|-----------|
| `MODFORGE_TTS_BIN` | TTS wrapper 腳本／執行檔（例如 f5 venv wrapper） | 必要——少了它 `voicelines` 會報錯 |
| `MODFORGE_FISH_SPEECH_BIN` | Fish Speech S2 wrapper 腳本／執行檔 | 僅當某 template 使用 `engine: "fish-s2"` 時才需要 |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe`（在 Wine 下執行） | WAV → xwm 編碼（`format: xwm`／`fuz`） |
| `MODFORGE_LIPGEN` | CK 官方 `LipGenerator.exe`（在 Wine 下執行） | **首選**的 .lip 唇形同步生成；隨 Creation Kit 提供於 `Tools/LipGen/LipGenerator/`，並會自動在自己的 exe 旁找到 `FonixData.cdf`（不需另設 cdf 變數） |
| `MODFORGE_FACEFX` | 社群的 `FaceFXWrapper.exe`（在 Wine 下執行） | 當 `MODFORGE_LIPGEN` 未設定時，作為 .lip 生成的後備 |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | 僅 `MODFORGE_FACEFX` 後備路徑才需要 |

> 當 `format: fuz` 且 `skipLip` 為 false 時，唇形同步會自動執行。把 `MODFORGE_LIPGEN` 指向
> CK 的 `LipGenerator.exe`，`voicelines` 就會把一個真正的 `.lip` 打包進每個 `.fuz`，讓 NPC 嘴巴會動——**遊戲內已於
> 2026-06-13 確認**（NPC 的嘴會隨音節動作）。若未設定任何唇形工具，`voicelines` 會印出
> 一次性警告，`.fuz` 出貨時不帶唇形資料（嘴形靜止）——字幕仍然有效。
>
> **資料夾名稱陷阱：** `voicelines` 會寫到 `Sound/Voice/<PluginName>/…`，所以要在 *packaged* 後的
> plugin 上執行（先 package，再對 packaged esp 跑 voicelines）——否則 voice 資料夾不會對上出貨的
> plugin 名稱，引擎也就找不到音訊。

**工作流**

```bash
dotnet run --project src/ModForge.Cli -- build      myspec.json out.esp   # 1. build (all dialogue/banter/scene INFOs get EditorIDs for the filenames)
dotnet run --project src/ModForge.Cli -- voicediag myspec.json out.esp    # 2. check speaker/template/path without TTS
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp   # 2. walk INFOs, synth WAV → xwm → .fuz next to the esp
dotnet run --project src/ModForge.Cli -- package    myspec.json OutModDir # 3. package as usual (the Sound/ tree travels with the mod)

# helper: harvest reference clips from a vanilla archive
dotnet run --project src/ModForge.Cli -- extract-voices "<path>/Skyrim - Voices_en0.bsa" FemaleYoungEager refclips/

# helper: harvest reference clips AND tag each with its source INFO emotion → annotation manifest
dotnet run --project src/ModForge.Cli -- voice-annotate <esm> <voiceType> <VoicesBSA> <outDir>
```

**`voice-annotate`** — 類似 `extract-voices`，但對每個片段，它還會在 `<esm>` 中查找來源 INFO（那段
8-hex FormID 就在片段檔名裡），並寫出 `<outDir>/voice-annotations.json`：每個片段一筆，
含 `clip` / `text` / `emotion`（7 種 Skyrim 情緒：Neutral/Anger/Disgust/Fear/Sad/Happy/
Surprise）/ `intensity`（0–100）/ `infoFormId`，外加空白的 `override` / `intensityOverride` / `note`
欄位供你聽過之後填寫。`emotion`／`intensity` 直接來自 INFO 的
`Emotion`／`EmotionValue`（遊戲早已為每句台詞標好——免費且具權威的初步成果）；你
只需修正粗略標籤判斷錯的部分（例如標為 Neutral 但實際上帶諷刺——設定
`override`）。`<esm>` 對原版 voice type 來說是 `Skyrim.esm`，或對某 mod 的角色聲音來說是 mod
（`SofiaFollower.esp`、`Vigilant.esm`）。*（Phase B——`voiceTemplates[].referenceLibrary` 消化修正後的
manifest，為每句挑出情緒相符的參考片段——是另一個之後的獨立功能。）*

Fish S2 template 範例：

```json
{
  "voiceTemplates": [
    {
      "id": "SeranaFish",
      "engine": "fish-s2",
      "referenceWav": "refs/serana_ref.wav",
      "referenceText": "Keep your eyes open.",
      "modelPath": "models/fish-s2-pro",
      "language": "en",
      "seed": 1234
    }
  ]
}
```

**檔案佈局** — `Sound/Voice/<plugin>/<voiceType>/<quest10>_<topic15>_<formid8>_<n>.fuz`
（CK 命名規則：quest EditorID 的前 10 字元、topic EditorID 的前 15 字元、
8 位 hex INFO FormID、以 1 為起點的回應索引）。引擎是依**說話者的 voiceType**
查找檔案，所以每個 voiceType 生成一個檔案，就能服務該 voiceType 底下所有說那句話的
NPC。重跑時已存在的檔案會被跳過（尚無 hash
cache——刪掉才會重新生成）。

Voice 檔案是 Skyrim 的散裝資產，不是嵌進 plugin 裡的 record。打包選項：

- 先跑 `package`，再跑 `voicelines <spec> <OutModDir>/<plugin>`，讓檔案直接
  寫進最終的 mod 資料夾；或
- 在暫存目錄中跑 `build` + `voicelines`，再跑 `package <spec> <OutModDir> --assets <stagingDir>`，
  把生成的 `Sound/` 樹複製過去。

在 Linux/Wine 上，Windows 工具（`xWMAEncode.exe`、`FaceFXWrapper.exe`）需要 Windows 風格的路徑。
ModForge 會透過 `winepath -w` 轉換暫存路徑；若轉換／工具執行失敗，`format:fuz`
會降級成散裝的 `.wav`，而非寫出一個多半會無聲的 raw-PCM `.fuz`。

**失敗行為**

- 一個 INFO 若無法從它的條件解析出說話者（GetIsID、alias 或 faction
  條件都能理解）→ 跳過並發出**大聲的警告**，絕不靜默。
- 在 `format: fuz` 下 xwm 編碼失敗 → 該句改寫成**散裝 `.wav`** 並附上
  警告，而不是把裸 WAV 塞進 .fuz（引擎是否接受 WAV-in-fuz
  尚未驗證）。
- 未設定任何唇形工具（`MODFORGE_LIPGEN` 與 `MODFORGE_FACEFX` 都沒設）→ 無 .lip（效果同
  `skipLip`）外加一次性警告；字幕仍然有效（一旦有了真正的
  .fuz 檔，就不需要 Fuz Ro D'oh）。
- `engine: "fish-s2"` 但沒設 `MODFORGE_FISH_SPEECH_BIN` → wrapper 會以明確的錯誤結束。

## 尚未涵蓋（在 `ModForge.Core` `Generator.Build` + 一個 spec class 中擴充）
世界 placement 現在已涵蓋新的 interior cell、原版 interior cell，**以及 exterior／worldspace
cell**（透過 `worldspace` + 世界座標），而且 ModForge 現在也能**建立**新的 worldspace（WRLD）
+ region（REGN）——見 [SPEC-worldspaces](SPEC-worldspaces.md)（僅 record 層；terrain/LOD/navmesh 仍留在
CK 端）。Ref（spec 內或 `<master>:0xFORMID`）與 `find` 命令是處理外部對象的基本構件。
其餘的缺口是長尾的 record 型別／欄位，以及 CK 端的 terrain/LOD/
navmesh 製作——record 端的模式都一樣：加一個 spec class + 在 `Build` 裡加一個迴圈。
