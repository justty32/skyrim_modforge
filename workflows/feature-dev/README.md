# 功能開發（feature-dev）— 工作流入口

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

新增 / 修改 ModForge 功能的工作流。這是本工作流的 **入口**：先讀本檔，再往下深入。always-on 鐵律見 [CLAUDE.md](../../CLAUDE.md)；要整理結構時參考 [DEV-GUIDE](../../DEV-GUIDE.md)（被動）；**程式碼慣例 + CODE_MAP 維護鏈**見 [common/conventions](../common/conventions.md)。

## 流程

```
修改程式碼（增量）
  → 跑離線測試（scripts/test-offline.sh = Category!=RequiresSkyrim）綠燈
  → 交使用者實機測試 → 回報問題 → 修程式碼 → 重複
  → 全數通過後：補齊 CODE_MAP → 補文檔 → commit
```

- **離線測試是你（Claude）自己跑**的把關（鐵律：改完跑測試）；指令/分類見 [testing](../testing.md)、跨機差異見 [dev-env](../dev-env.md)。
- **實機測試一律由使用者做**——Claude 起不了 Skyrim/MO2/Proton（見 [dev-env](../dev-env.md) 機器能力表）。所以先靠離線測試 + 結構驗證把握到極限，再交付；需使用者驗證的記到 [WAIT_USER](../../WAIT_USER.md)。
- 測試迭代期間，CODE_MAP / 文檔可暫時落後。
- 跨 session 時在 [session-log.md](session-log.md) 補一行 `[功能名] 文檔/CODE_MAP 待同步`，下個 session 不會誤判已同步。
- **commit 前**：CODE_MAP + 文檔必須對齊（HTML 不要求，examples/assets 視情況）。

## 內容

| 檔案 | 內容 |
|------|------|
| [landed.md](landed/README.md) | **已落地功能目錄**（時間序；功能在哪、實作細節指標）|
| [gotchas.md](gotchas.md) | 開發踩坑（含**外部工具內部開發聯動**：Papyrus 編譯 / Wine shell-out）|
| [session-log.md](session-log.md) | 本工作流 open / in-flight 進度（hub 在 repo 根 [SESSION-LOG](../../SESSION-LOG.md)）|

> **archive**：過時/被取代的功能開發文檔封存進 `feature-dev/archive/`（學 [specs/](../specs/README.md) 的封存做法，保留歷史、不污染現役）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。
