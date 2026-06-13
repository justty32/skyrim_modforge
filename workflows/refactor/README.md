# 重構整理（refactor）— 工作流入口

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

behavior-preserving 的拆分 / 模塊化。這是本工作流的 **入口**。拆法（膨脹即拆 / 雜亂即分類 / 平鋪太多即包夾、四級成長軌跡）見元工作流 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md)；碰原始碼時的**程式碼慣例 + CODE_MAP 維護鏈**見 [common/conventions](../common/conventions.md)。

## 流程

維護鏈中**一次只動一個面向**，做完 commit 再看下一個：

```
Step 1  程式碼重構（behavior-preserving 拆分）→ 立即更新 CODE_MAP 與相關文檔 → 跑測試確認行為不變 → commit
Step 2  （視需要）CODE_MAP 臃腫 → 單獨重構 → 同步其連結到的文檔段落 → commit
Step 3  （視需要）文檔臃腫 → 單獨重構 → 同步 CODE_MAP 指向它們的連結 → commit
Step 4  （視需要）examples/assets → 單獨處理 → commit
```

**禁止**同一 session 內同時重構超過一個面向；每個 Step 完成前不啟動下一個，確保任意時間點維護鏈一致。

## 內容

| 檔案 | 內容 |
|------|------|
| [session-log.md](session-log.md) | 本工作流 open / in-flight 重構項（hub 在 repo 根 [SESSION-LOG](../../SESSION-LOG.md)）|

> **archive**：過時的重構筆記/計畫封存進 `refactor/archive/`（學 [specs/](../specs/README.md)）。本入口檔若膨脹，照結構整理原則拆。
