# ModForge — External Tooling & Environment Dependencies

← [INDEX](../../INDEX.md)｜開發環境（跨機）見 [dev-env.md](../dev-env.md)

ModForge is a Skyrim mod **generator**: record-building (the ESP/ESM itself) is pure Mutagen and needs no external tools. External binaries and data files only come into play for **Papyrus compilation**, the **voice pipeline**, and **vanilla-master resolution**. Everything here is optional in the sense that a missing tool degrades or skips one feature — the core build never hard-crashes on a missing dependency (the one exception is a spec `$env` with no default, which is a deliberate hard error).

Conventions in the tables:
- **Required?** — required *for the feature that reads it*, not for ModForge as a whole.
- **Missing → behavior** — what actually happens when the tool/var is absent. ModForge prefers *warn-and-degrade* over throwing.

## 內容

| 檔 | 涵蓋 |
|----|------|
| [env-vars.md](env-vars.md) | `MODFORGE_*` 環境變數（指向什麼 / 是否必須 / 缺失行為 / 讀取點）|
| [binaries.md](binaries.md) | 外部 binary（wine / PapyrusCompiler / TTS / xWMAEncode / LipGen / ffmpeg…）+ gotchas |
| [data-assets.md](data-assets.md) | 執行期讀的外部資料/資產（Skyrim.esm / STRINGS BSA / Papyrus headers / 語音 BSA…）|

## Fresh-clone prerequisite

Before the first `dotnet build`, the six dispatcher/controller `.psc` must be compiled to `.pex` (they're embedded as conditional `EmbeddedResource` but `.pex` is gitignored). See [dev-env.md](../dev-env.md)「前置步驟」for the exact `compile` commands. Missing `.pex` only *warns* at build time — the relevant Fire()-routed trigger / identity feature won't work at runtime until the `.pex` exists locally.
