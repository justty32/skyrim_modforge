# 外部資源 — 帶入你自己的網格／貼圖／音效／動畫

← index: [for_agent.md](for_agent.md) · spec fields: [SPEC-index.md](spec/SPEC-index.md) · CLI: [for_agent_cli.md](for_agent_cli.md)

預設情況下，ModForge 透過**複製一筆原版記錄**來賦予記錄 3D 外觀（武器的
`template` 會重用 IronSword 的 `.nif`）。**外部資源管線**讓你改為帶入你
**自己的**資源——自訂網格、貼圖、音效、動畫——並讓 ModForge（1）**把 Data 相對
路徑寫入記錄**，以及（2）把檔案**打包**在 `.esp` 旁邊，使打包後的
mod 自我完備／可直接給 MO2 使用。

## ModForge 做什麼 vs. 你必須自行製作什麼

ModForge **參照並打包**資源。它**不會**製作資源。請誠實看待這個分工：

| Asset | ModForge does | You must author (DCC tool / Creation Kit) |
|---|---|---|
| `.nif` mesh | 把路徑寫入記錄的 `MODL`、複製檔案 | 製作網格（Blender + Nif tools / 3ds Max）、設定 collision、materials |
| `.dds` texture | 打包檔案（`.nif` 在內部指定自己的貼圖） | 繪製貼圖、產生 mipmaps、讓 `.nif` 的 `BSShaderTextureSet` 指向它 |
| `.wav` / `.xwm` sound | 產出指向檔案的 Sound Descriptor（SNDR）、把記錄連到它、打包檔案 | 錄製／製作音訊；`.xwm` 是壓縮後的遊戲內格式（xWMAEncode） |
| `.hkx` animation | 打包檔案（若放在可辨識的資料夾下） | 製作動畫＋behaviour graph（CK / havok tools）；把動畫接進 behaviour 是**範圍之外** |

ModForge 無法驗證資源**內容**——一條指向壞掉 `.nif` 的路徑照樣能 build 並打包
得好好的，卻會在遊戲內崩潰。本工具保證的是**接線與打包**，而不是位元組本身。

## Data 相對路徑規則（這是頭號陷阱）

Skyrim 會從遊戲的 `Data/` 資料夾載入 loose files。有兩種不同的起點慣例：

- **Model 路徑（`model` 欄位）** 以 **`Data\Meshes\`** 為起點。所以引擎路徑
  `Data\Meshes\MyMod\bell.nif` 寫作 **`MyMod\bell.nif`**——**不要**包含
  `Meshes\` 前綴（ModForge 的 `validate` 會拒絕以 `Meshes\` 開頭的 `model`）。
  （已從原版確認：IronSword 的 model 是 `Weapons\Iron\LongSword.nif`，即位於磁碟上的
  `Data\Meshes\Weapons\Iron\LongSword.nif`。）
- **Sound 檔案路徑（`sounds[].files`）** 以 **`Data\`** 為起點，並位於 **`Sound\`** 之下，
  例如 `Sound\fx\mymod\bell.wav`（磁碟上位於 `Data\Sound\fx\mymod\bell.wav`）。要包含
  `Sound\` 區段。

依 Bethesda 慣例使用**反斜線**（`\`）——ModForge 也接受 `/`，引擎會
正規化。路徑必須是**相對**的（不可有 `C:\…`、不可有開頭的 `\`、不可有磁碟機代號）。挑一個
以你的 mod 命名的獨特子資料夾（`MyMod\…`、`Sound\fx\mymod\…`），這樣你就絕不會與原版或
另一個 mod 衝突。

## 檔案放哪裡：資源來源目錄

`package` 會把一個**來源資源目錄**複製到輸出的 mod 資料夾。用 spec 的
`assets` 欄位（相對於 spec 檔案，或絕對路徑）**或** CLI 的 `--assets <dir>` 覆寫
（後者勝出）指向它。來源必須包含引擎標準的子資料夾；ModForge 只打包這些，
不分大小寫，並保留結構：

```
Meshes/  Textures/  Sounds/ (or Sound/)  Music/  Seq/
```

這些資料夾以外的任何東西（一個 `README.txt`、一個 `Docs/` 目錄）都會被**忽略**。所以請把來源
完全照它應該出現在 `Data/` 之下的樣子來擺放：

```
my_assets/
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
  Meshes/actors/mymod/anims/idle.hkx        # .hkx ride along under Meshes\…
```

> 動畫（`.hkx`）在 Skyrim 中位於 **`Meshes\` 之下**（例如
> `Meshes\actors\character\animations\…`），因此會作為 `Meshes/` 樹的一部分被打包。把
> 新動畫接進角色的 **behaviour graph** 是一項 Creation-Kit / havok 工作，ModForge 並
> 不執行——它只攜帶檔案。

`package` 之後，輸出資料夾就是一個可直接放入的 MO2/Vortex mod：

```
OutModDir/
  MyMod.esp
  Meshes/MyMod/bell.nif
  Textures/MyMod/bell.dds
  Sound/fx/mymod/bell_chime_01.wav
```

## Spec 欄位

### `model` — 記錄上的自訂網格

在 **statics、activators、furniture、miscItems、weapons** 上設定 `model`（一條
Data 相對的 `.nif` 路徑，無 `Meshes\` 前綴）。設定後，ModForge 會把它寫入記錄的 model
subrecord，而不是（或除了）複製 template：

```jsonc
"statics":    [ { "editorId": "MFMonument", "model": "MyMod\\monument.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Forged Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Forged Bell", "model": "MyMod\\bell.nif" } ],
"miscItems":  [ { "editorId": "MFRelic", "name": "Forged Relic", "value": 250, "model": "MyMod\\relic.nif" } ]
```

- 在 `miscItem` 上**同時有 `model` + `template`**：ModForge 會警告，且 **`model` 勝出**（你的網格
  會覆蓋複製 template 的網格）。
- **有 `model` 但沒有 `template` 的 `weapon`** 很可能會在**裝備時崩潰**——武器還需要
  1st-person model／animation type／equip data，而這些只有 `template` 複製才能提供。請把武器
  `model` 與一個相同武器類型的 `template` **搭配**（`model` 接著只會覆蓋
  world／3rd-person 網格）。當武器有 `model` 卻沒有 `template` 時，ModForge 會警告。
- Statics/activators/furniture 是純網格記錄——只給 `model` 是正常、正確的做法。

### `sounds` — 自訂 Sound Descriptor（SNDR）

一筆 `sounds` 條目會產出一個指向你的 `.wav`/`.xwm` 的 **Sound Descriptor**。記錄透過
`editorId` 來參照它：

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

接受 SNDR *ref*（一個 in-spec 的 `sounds` editorId **或**一個原版
`<master>:0xFORMID`）的音效連結欄位：

| record | fields |
|---|---|
| `activators` | `activationSound`, `loopingSound` |
| `miscItems` | `pickUpSound`, `putDownSound` |
| `weapons` | `pickUpSound`, `putDownSound` |

`category`/`outputModel` 預設為原版 SFX category＋output model，因此音效不需進一步調校
就確實聽得到。這個 SNDR 基礎元件刻意設計得很通用——它也是規劃中的
voice/TTS 管線（`.fuz` voice lines）將要建立其上的基礎。

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

`validate` 會檢查：model 是 `.nif`、未以 `Meshes\` 為前綴、是相對路徑；一個 sound 至少有一個
`.wav`/`.xwm` 檔案；sound/category/output-model 的 ref 都能解析。`dump` 會印出每筆記錄的 `model:` 路徑、
`activationSound`/`pickUpSound -> …` 連結，以及 SNDR 的 `soundFile=` 路徑。

一個完整的實作範例是 **`../examples/custom_asset_spec.json`**（搭配一棵 placeholder 資源樹
位於 `../examples/assets/customasset/`——僅為佔位位元組；請替換為真正製作的內容）。

## 限制 — 誠實以對

ModForge 寫出**結構上有效**的記錄，並**複製你給它的檔案**。它**不會**：

- 製作或驗證 mesh/texture/sound/animation 的**內容**（壞掉的 `.nif` 在遊戲內仍會崩潰）；
- 把動畫接進角色的 **behaviour graph**（一項 CK/havok 工作）；
- 從 `.nif` 的貼圖參照產生 `.dds`（兩者都由你提供；`.nif` 會指定自己的貼圖）。

要確認一個自訂資源真的能算繪／播放，需要進行一次 Proton/Skyrim 啟動——見
[for_agent.md → Limits](for_agent.md#limits--be-honest-do-not-over-claim)。
