# dev-env — 開發環境（跨機）

← [INDEX](../INDEX.md)｜外部工具/env 清單見 [tooling.md](tooling/README.md)

ModForge 在**兩種機器**上開發，能做的事不同：

| 機器 | 有什麼 | 能做 | 不能做 |
|------|--------|------|--------|
| **Manjaro（主力機）** | Skyrim.esm + Wine + CK 工具（PapyrusCompiler / xWMAEncode / LipGenerator）+ TTS + MO2/Proton | 全部：build / 全測試 / Papyrus 編譯 / 出貨 / 語音 / **實機測試** | — |
| **離線機 / fresh clone＝Windows 11 + PowerShell**（無 Skyrim/Wine）| 只有 .NET SDK（無 Skyrim.esm、無 Wine/CK、無遊戲）| 改 code/docs · `build` 出 `.esp`（純 Mutagen）· **離線測試** | 實機測試、`bootstrap-pex`、`ship*`、語音（都需 Wine/CK/Skyrim/TTS）|

> **離線機＝Windows 注意**：`scripts/*.sh` 需經 bash（Git Bash / WSL）才能跑；或直接用原生跨平台的 `dotnet test --filter ...` 指令（見 [testing.md](testing.md)）。commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題）。

> 設計上**離線可開發**：`.pex` 是條件式 EmbeddedResource（缺檔仍可 build，只 runtime warn），需 Skyrim.esm 的測試標了 `Category=RequiresSkyrim` 可排除。所以這類離線機 clone 下來，跳過 `bootstrap-pex`、跑離線測試即可。

## 測試

離線迴歸 `scripts/test-offline.sh`（= `dotnet test --filter "Category!=RequiresSkyrim"`）；含 `Skyrim.esm` 的全測試僅 Manjaro。指令、Category 語意、`MODFORGE_SKYRIM_DATA` 完整說明見 **[testing.md](testing.md)**（權威）。

## 前置步驟（**僅 Manjaro**，fresh clone 後、`dotnet build` 前做一次）

```bash
scripts/bootstrap-pex.sh
```

把 `assets/papyrus/*.psc` 編成 `.pex`（被 CLI embed 為 EmbeddedResource；`.pex` 不進 repo，任何 `.psc` 改動同樣重跑、新 `.pex` 留本機不 commit）。需 Wine + CK PapyrusCompiler（native 走 `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS`）。**離線機沒有 Wine/CK → 跳過**（build 仍可，runtime 才 warn 缺 `.pex`）。

## 出貨腳本（**僅 Manjaro**）

`package`→FLAT zip→`MODFORGE_SHIP_DIR`（預設 `~/skyrim_mods/mine/`，自動防 stale-file）：

- 一般 mod：`scripts/ship.sh <spec> [zipName] [--clean-prefix]`
- 語音 mod：`scripts/ship-voice.sh <spec> ...`（package→voicelines→voicediag→zip，需 `MODFORGE_TTS_BIN`；要有聲還需 `MODFORGE_XWMAENCODE`＝SSE `Tools/Audio/xwmaencode.exe`，否則吐無聲 loose wav）
  - **已知陷阱**（TIF 內聯編譯 spurious fail 完整修法、LipGenerator wine crash）→ 見 [feature-dev/gotchas](feature-dev/gotchas.md)「Voice / ship-voice」。

## 實機狀態查詢（**僅 Manjaro**）

遊戲跑著的時候，`scripts/skylink/skylink-bridge.sh up` 把 [SkyLink](skylink/README.md) 的 MCP server 接上，agent 就能直接查執行中遊戲的 load order / cell / quest stage / FormID，不必靠人轉述。實機**體感**（動畫、對嘴、崩不崩）仍只有人能判。

## 通用

- Commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題，見上「離線機＝Windows 注意」）。
- 外部工具 / `MODFORGE_*` env var / 資料依賴（含缺檔降級行為）完整清單見 [tooling.md](tooling/README.md)。
