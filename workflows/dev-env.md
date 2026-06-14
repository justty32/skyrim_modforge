# dev-env — 開發環境（跨機）

← [INDEX](../INDEX.md)｜外部工具/env 清單見 [tooling.md](tooling/README.md)

ModForge 在**兩種機器**上開發，能做的事不同：

| 機器 | 有什麼 | 能做 | 不能做 |
|------|--------|------|--------|
| **Manjaro（主力機）** | Skyrim.esm + Wine + CK 工具（PapyrusCompiler / xWMAEncode / LipGenerator）+ TTS + MO2/Proton | 全部：build / 全測試 / Papyrus 編譯 / 出貨 / 語音 / **實機測試** | — |
| **離線機 / fresh clone（無 Skyrim/Wine）** | 只有 .NET SDK（無 Skyrim.esm、無 Wine/CK、無遊戲）| 改 code/docs · `build` 出 `.esp`（純 Mutagen）· **離線測試** | 實機測試、`bootstrap-pex`、`ship*`、語音（都需 Wine/CK/Skyrim/TTS）|

> 設計上**離線可開發**：`.pex` 是條件式 EmbeddedResource（缺檔仍可 build，只 runtime warn），需 Skyrim.esm 的測試標了 `Category=RequiresSkyrim` 可排除。所以這類離線機 clone 下來，跳過 `bootstrap-pex`、跑離線測試即可。

## 測試

```bash
# 全部（需 Manjaro：含 RequiresSkyrim）
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj      # net10.0

# 離線（任何機器；排除需本機 Skyrim.esm 的）
scripts/test-offline.sh        # = dotnet test --filter "Category!=RequiresSkyrim"
```

需本機 `Skyrim.esm` 的測試已標 `Category=RequiresSkyrim`；一般離線迴歸請排除該 category。

## 前置步驟（**僅 Manjaro**，fresh clone 後、`dotnet build` 前做一次）

```bash
scripts/bootstrap-pex.sh
```

把 `assets/papyrus/*.psc` 編成 `.pex`（被 CLI embed 為 EmbeddedResource；`.pex` 不進 repo，任何 `.psc` 改動同樣重跑、新 `.pex` 留本機不 commit）。需 Wine + CK PapyrusCompiler（native 走 `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS`）。**離線機沒有 Wine/CK → 跳過**（build 仍可，runtime 才 warn 缺 `.pex`）。

## 出貨腳本（**僅 Manjaro**）

`package`→FLAT zip→`MODFORGE_SHIP_DIR`（預設 `~/skyrim_mods/mine/`，自動防 stale-file）：

- 一般 mod：`scripts/ship.sh <spec> [zipName] [--clean-prefix]`
- 語音 mod：`scripts/ship-voice.sh <spec> ...`（package→voicelines→voicediag→zip，需 `MODFORGE_TTS_BIN`；要有聲還需 `MODFORGE_XWMAENCODE`＝SSE `Tools/Audio/xwmaencode.exe`，否則吐無聲 loose wav）
  - **已知陷阱**：含對話 `setGlobal`/`sayOnce` 的 spec，package 與 ship-voice 的 TIF 內聯自動編譯會 spurious fail（zip 出 0 個 `.pex`）→ 對話照播但 sayOnce 失效（選項重複）。修法：對 package 產出的 `Scripts/Source/TIF_*.psc` 逐一 `dotnet run --project src/ModForge.Cli -- compile <psc> <stage>/Scripts`（單獨 compile 必成），再 `cd <stage> && zip <既有zip> Scripts/*.pex` 補進去（語音不用重做）。
  - lip 嘴型：設 `MODFORGE_LIPGEN`＝CK `LipGenerator.exe`；但它在 wine 下會 crash/重試把配音拖到極慢，量大時可先**不設＝跳過**（嘴不動），之後再統一補 lip。

## 通用

- Commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題）。
- 外部工具 / `MODFORGE_*` env var / 資料依賴（含缺檔降級行為）完整清單見 [tooling.md](tooling/README.md)。
