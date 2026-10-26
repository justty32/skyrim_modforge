# Environment variables (`MODFORGE_*`)

← [tooling README](README.md)

ModForge 讀的 `MODFORGE_*` 環境變數（加上 spec `$env` 指名的任意變數）：指向什麼、是否必須、缺席時的行為、在哪裡讀取。

已抽到 [env-vars.json](env-vars.json)（15 列）。欄位：

- `Var`：變數名（`*(arbitrary)*` 一列＝spec `$env` 指名的任意變數）。
- `Points to`：指向的檔案／目錄／工具。
- `Required?`：對*讀取它的那個功能*而言是否必須（`optional`／`shell only`／功能條件）。
- `Missing → behavior`：缺席時實際發生什麼（fallback 路徑、ExitCode、降級）。
- `Read at`：讀取點（`檔名:行號`）。

統計：15 列；C# 端 11 個、Python 端 1 個（`MODFORGE_FISH_SPEECH_BIN`）、shell 腳本 2 個、spec `$env` 1 列。唯一刻意的硬錯誤是 `$env` 無值也無 `default`。
