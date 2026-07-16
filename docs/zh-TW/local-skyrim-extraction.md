# 本機 Skyrim Master 抽取

本 repo 在 `src/ModForge.Cli` 已有 master 檢視指令：`find` 依 EditorID/name 搜尋
記錄，`smtree` 列出 Story Manager event 根。`scripts/extract-skyrim-masters.sh` 裡的本機抽取
包裝程式會對 Manjaro 上的 Steam Proton Skyrim Special Edition master 跑這些既有指令，並寫出小而
可重現的參考產物。

預設 Data 路徑：

```bash
$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data
```

預期的 master：

```text
Skyrim.esm
Dawnguard.esm
HearthFires.esm
Dragonborn.esm
```

執行：

```bash
cd /home/lorkhan/repo/moddings/skyrim/projects/ModForge
scripts/extract-skyrim-masters.sh
```

需要時覆寫路徑：

```bash
MODFORGE_SKYRIM_DATA="/path/to/Skyrim Special Edition/Data" \
MODFORGE_REFERENCE_OUT="/tmp/modforge-skyrim-reference" \
scripts/extract-skyrim-masters.sh
```

輸出預設放到 `reference/skyrim-masters-local/`。`reference/` 已被 gitignore，所以
龐大或屬於本機的抽取資料不會進入版本控制。產生的檔案有：

- `manifest.json`：輸入路徑、大小，以及輸出佈局。
- `run-status.tsv`：每個 CLI 探測及其是否成功。
- `find/*.txt`：對 Skyrim.esm 與 DLC master 中常見記錄類型抽樣的 FormID 參考搜尋。
- `skyrim-smtree.txt`：來自 Skyrim.esm 的 Story Manager event 根。
- `logs/*.log` 與 `logs/*.err`：失敗或吵雜探測的 build 輸出與 stderr。

導覽地圖：

```text
reference/INDEX-skyrim-masters-local.md
```

當 agent 需要原版或 DLC FormID 時，從這裡開始。它的組織方式比照 ModForge
`CODE_MAP` 檔案：輸出資料夾、Data 資料夾地圖、語音位置、記錄家族查詢表、
快取查詢命名、狀態格式、直接 CLI 查詢指令，以及在不盲目列出目錄的前提下擴充快取的規則。

2026-06-12 Manjaro 執行的分拆輸出：

- `reference/skyrim-esm-local/`：僅 `Skyrim.esm`。
- `reference/skyrim-dlc-local/`：僅 `Dawnguard.esm`、`HearthFires.esm` 與 `Dragonborn.esm`。
- `reference/skyrim-masters-local/`：合併的全 master 執行。

若只想跑選定的 master，把 `MODFORGE_SKYRIM_MASTERS` 設為以空白分隔的清單：

```bash
MODFORGE_SKYRIM_MASTERS="Skyrim.esm" \
MODFORGE_REFERENCE_OUT="/home/lorkhan/repo/moddings/skyrim/projects/ModForge/reference/skyrim-esm-local" \
scripts/extract-skyrim-masters.sh
```

這是一個參考產生工作流，不是完整的 master dump。若要取得特定記錄，請用
`find/*.txt` 裡的 FormID 搭配既有的診斷指令，例如：

```bash
R="dotnet run --project src/ModForge.Cli --no-build --"
SK="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/Skyrim.esm"
$R weatherdiag "$SK" 0x10E1F2
```
