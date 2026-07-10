# ModForge — 外部工具與環境依賴

← [INDEX](../../INDEX.md)｜開發環境（跨機）見 [dev-env.md](../dev-env.md)

ModForge 是 Skyrim mod **生成器**：record 建構（ESP/ESM 本體）純用 Mutagen，不需任何外部工具。外部 binary 與資料檔只在 **Papyrus 編譯**、**語音管線**、**vanilla-master 解析** 三處派上用場。這裡列的一切都是「可選」的——缺工具只會讓某個功能降級或跳過，核心 build 絕不因缺依賴硬當（唯一例外：spec 的 `$env` 無 default 時是刻意的 hard error）。

表格中的欄位慣例：
- **Required?** —— 指對*讀取它的那個功能*而言必須，不是對 ModForge 整體。
- **Missing → behavior** —— 工具/變數缺席時實際發生什麼。ModForge 偏好 *warn-and-degrade* 而非丟例外。

## 內容

| 檔 | 涵蓋 |
|----|------|
| [env-vars.md](env-vars.md) | `MODFORGE_*` 環境變數（指向什麼 / 是否必須 / 缺失行為 / 讀取點）|
| [binaries.md](binaries.md) | 外部 binary（wine / PapyrusCompiler / TTS / xWMAEncode / LipGen / ffmpeg…）+ gotchas |
| [data-assets.md](data-assets.md) | 執行期讀的外部資料/資產（Skyrim.esm / STRINGS BSA / Papyrus headers / 語音 BSA…）|
| [skylink.md](skylink.md) | SkyLink AI 實機狀態查詢橋（Manjaro 專屬；agent 直接查執行中遊戲的 load order / cell / quest stage）|

## Fresh-clone 前置

第一次 `dotnet build` 前，六個 dispatcher/controller `.psc` 必須先編成 `.pex`（它們以條件式 `EmbeddedResource` embed，但 `.pex` 被 gitignore）。實際的 `compile` 指令見 [dev-env.md](../dev-env.md)「前置步驟」。缺 `.pex` 在 build 時只會 *warn*——相關的 Fire()-routed trigger / identity 功能要等本機有了 `.pex` 才會在 runtime 生效。
