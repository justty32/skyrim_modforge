# 外部資源 — 帶入您自己的網格 / 貼圖 / 音效 / 動畫

← 索引：[for_agent.md](for_agent.md) · 規格欄位：[SPEC-index.md](SPEC-index.md) · CLI：[for_agent_cli.md](for_agent_cli.md)

ModForge 預設透過**複製原版記錄**（武器的 `template` 重用 IronSword 的 `.nif`）來給記錄 3D 外觀。**外部資源管線**讓您改為帶入**自己的**資源 — 自訂網格、貼圖、音效、動畫 — 並讓 ModForge (1) **將 Data 相對路徑寫入記錄**，以及 (2) **將檔案打包**到 `.esp` 旁邊，使打包後的模組可獨立運作 / 相容 MO2。

## ModForge 負責的 vs. 您必須製作的

ModForge **參考並打包**資源，**不會**製作它們。請誠實面對這個分工：

| 資源 | ModForge 負責 | 您必須製作（DCC 工具 / Creation Kit） |
|---|---|---|
| `.nif` 網格 | 將路徑寫入記錄的 `MODL`，複製檔案 | 建模（Blender + Nif tools / 3ds Max），設定碰撞、材質 |
| `.dds` 貼圖 | 打包檔案（`.nif` 在內部指定其貼圖） | 繪製貼圖、產生 mipmap、讓 `.nif` 的 `BSShaderTextureSet` 指向它 |
| `.wav` / `.xwm` 音效 | 發出指向檔案的 Sound Descriptor（SNDR），連結記錄，打包檔案 | 錄製/製作音訊；`.xwm` 是遊戲內壓縮格式（xWMAEncode） |
| `.hkx` 動畫 | 打包檔案（若放在已識別的資料夾下） | 製作動畫 + 行為圖（CK / havok 工具）；將動畫接入行為是**不在範圍內**的 |

ModForge 無法驗證資源**內容** — 指向壞掉 `.nif` 的路徑可以建置並打包，但遊戲中會當機。工具保證**接線和打包**，不保證位元組的正確性。

## Data 相對路徑規則（這是第一大陷阱）

Skyrim 從遊戲的 `Data/` 資料夾載入散落的檔案。兩種不同的根目錄慣例：

- **模型路徑（`model` 欄位）**以 **`Data\Meshes\`** 為根目錄。所以引擎路徑 `Data\Meshes\MyMod\bell.nif` 應寫成 **`MyMod\bell.nif`** — **不要**加 `Meshes\` 前綴（ModForge 的 `validate` 會拒絕以 `Meshes\` 開頭的 `model`）。
  （已從原版確認：IronSword 的模型是 `Weapons\Iron\LongSword.nif`，即磁碟上位於 `Data\Meshes\Weapons\Iron\LongSword.nif`。）
- **音效檔案路徑（`sounds[].files`）**以 **`Data\`** 為根目錄，並放在 **`Sound\`** 下，例如 `Sound\fx\mymod\bell.wav`（磁碟上是 `Data\Sound\fx\mymod\bell.wav`）。請包含 `Sound\` 區段。

依 Bethesda 慣例使用**反斜線**（`\`） — ModForge 也接受 `/`，引擎會正規化。路徑必須是**相對路徑**（無 `C:\…`，無前導 `\`，無磁碟機代號）。請選擇以您的模組命名的唯一子資料夾（`MyMod\…`、`Sound\fx\mymod\…`），以免與原版或其他模組衝突。

## 放置檔案的位置：資源來源目錄

`package` 會將一個**來源資源目錄**複製到輸出模組資料夾。透過規格的 `assets` 欄位（相對於規格檔案，或絕對路徑）**或** CLI 的 `--assets <dir>` 覆寫（優先使用）來指定。來源必須包含引擎標準子資料夾；ModForge 僅打包這些（不區分大小寫，保留結構）：

```
Meshes/  Textures/  Sounds/ (or Sound/)  Music/  Seq/
```

這些資料夾之外的任何內容（`README.txt`、`Docs/` 目錄）都會**被忽略**。所以請按照 `Data/` 下應有的結構來排列來源：

```
my_assets/
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
  Meshes/actors/mymod/anims/idle.hkx        # .hkx ride along under Meshes\…
```

> 動畫（`.hkx`）在 Skyrim 中放在 **`Meshes\`** 下（例如 `Meshes\actors\character\animations\…`），因此它們作為 `Meshes/` 樹的一部分被打包。將新動畫接入角色的**行為圖**是 Creation Kit / havok 的工作，ModForge 不執行 — 它只攜帶檔案。

`package` 後，輸出資料夾是一個可直接拖入 MO2/Vortex 的模組：

```
OutModDir/
  MyMod.esp
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
```

## 規格欄位

### `model` — 記錄上的自訂網格

在 **statics、activators、furniture、miscItems、weapons** 上設定 `model`（Data 相對 `.nif` 路徑，無 `Meshes\` 前綴）。設定後，ModForge 會將它寫入記錄的模型子記錄，而不是（或同時）複製模板：

```jsonc
"statics":    [ { "editorId": "MFMonument", "model": "MyMod\\monument.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Forged Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Forged Bell", "model": "MyMod\\bell.nif" } ],
"miscItems":  [ { "editorId": "MFRelic", "name": "Forged Relic", "value": 250, "model": "MyMod\\relic.nif" } ]
```

- **`model` + `template` 同時用於 `miscItem`**：ModForge 會警告，且 **`model` 優先**（您的網格會覆蓋複製模板的網格）。
- **有 `model` 但沒有 `template` 的 `weapon`** 很可能會**在裝備時當機** — 武器還需要第一人稱模型 / 動畫類型 / 裝備資料，這些只有 `template` 複製才能提供。請將武器 `model` **搭配**相同武器類型的 `template`（`model` 只覆寫世界/第三人稱網格）。ModForge 在武器有 `model` 但無 `template` 時會警告。
- Statics/activators/furniture 是純網格記錄 — 僅用 `model` 是正常、正確的做法。

### `sounds` — 自訂 Sound Descriptors（SNDR）

一個 `sounds` 條目會發出一個指向您的 `.wav`/`.xwm` 的 **Sound Descriptor**。記錄透過 `editorId` 參考它：

```jsonc
"sounds": [
  { "editorId": "MFBellChimeSD",
    "files": [ "Sound\\fx\\mymod\\bell_chime_01.wav" ],   // one or more; Data-relative under Sound\
    "category": "",            // ref → SNCT; empty -> Skyrim.esm:0x0172A1 AudioCategorySFX
    "outputModel": "",         // ref → SOPM; empty -> Skyrim.esm:0x0B4058 (vanilla SFX output)
    "priority": 128,
    "staticAttenuation": 5.0 } // dB attenuation
],
"activators": [ { "editorId": "MFBell", "name": "Bell", "model": "MyMod\\bell.nif",
                  "activationSound": "MFBellChimeSD" } ]
```

接受 SNDR *ref*（規格內的 `sounds` editorId **或**原版 `<master>:0xFORMID`）的音效連結欄位：

| 記錄 | 欄位 |
|---|---|
| `activators` | `activationSound`、`loopingSound` |
| `miscItems` | `pickUpSound`、`putDownSound` |
| `weapons` | `pickUpSound`、`putDownSound` |

`category`/`outputModel` 預設為原版 SFX 類別 + 輸出模型，因此音效無需進一步調整即可實際發聲。這個 SNDR 原語在設計上是通用的 — 它也是規劃中的語音/TTS 管線（`.fuz` 語音行）將以之為基礎的根基。

## 工作流程

```bash
# 1) author your assets into a source dir laid out like Data/ (Meshes/Textures/Sound/…)
# 2) write the spec: model paths + sounds + (optionally) an `assets` dir
dotnet run --project src/ModForge.Cli -- validate examples/custom_asset_spec.json   # path-shape + refs
dotnet run --project src/ModForge.Cli -- package  examples/custom_asset_spec.json OutModDir
#    (or: package … OutModDir --assets /path/to/my_assets   to override the spec's `assets`)
dotnet run --project src/ModForge.Cli -- dump OutModDir/MFCustomAssets.esp           # verify wiring
find OutModDir -type f                                                                # verify bundle
```

`validate` 檢查：model 是 `.nif`、沒有 `Meshes\` 前綴、是相對路徑；音效至少有一個 `.wav`/`.xwm` 檔案；音效/類別/輸出模型 ref 可解析。`dump` 會印出每筆記錄的 `model:` 路徑、`activationSound`/`pickUpSound -> …` 連結以及 SNDR 的 `soundFile=` 路徑。

完整工作範例：**`../examples/custom_asset_spec.json`**（含 `../examples/assets/customasset/` 的佔位資源樹 — 僅存根位元組；請替換為真正製作的內容）。

## 限制 — 請誠實說明

ModForge 寫出**結構有效**的記錄並**複製您給它的檔案**，但**不會**：

- 製作或驗證網格/貼圖/音效/動畫**內容**（壞掉的 `.nif` 在遊戲中仍會當機）；
- 將動畫接入角色的**行為圖**（這是 CK/havok 的工作）；
- 從 `.nif` 的貼圖參考產生 `.dds`（您需要同時提供兩者；`.nif` 在內部指定其貼圖）。

確認自訂資源能實際渲染/播放需要 Proton/Skyrim 啟動 — 詳見 [for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim)。
