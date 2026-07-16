# 第 2 幕 支線任務指南 - 斯坦達爾指南

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋翻譯問題或特定條件時出現簡短的原始片段。
- CLI 診斷提供確定的階段/目標/條件數據。

## 任務記錄

[`43B81F zzzBMGuide "斯坦達爾指南"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:83)

CLI：
- `questdiag Vigilant.esm 0x43B81F`
- `infodiag Vigilant.esm 0x43B81F`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x43B81F`
- EditorID: `zzzBMGuide`
- 名稱: `斯坦達爾指南`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `Misc` (雜項)
- 過濾器: `BM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | CompleteQuest | 空 |
| 999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 任務文本 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:84) | 追蹤血跡 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:85) | 追蹤古老之血 |

目標標靶：
- 每個目標在 ESM 中各有 1 個標靶。
- 標靶標記：兩個標靶均帶有 `CompassMarkerIgnoresLocks` (羅盤標記忽略鎖定)。
- 標靶條件：無。
- （推論：標靶可能是放置在風盔城地牢中的標記引用；如果空間分期重要，則需要更深入的轉儲。）

## 無對話或場景

與第 2 幕主線任務 (`zzzBMMq01–03`) 不同，此任務：
- 沒有自定義對話主題（已透過 `infodiag 0x43B81F` 確認：「沒有具有該 FormID 的 DialogTopic，也沒有該 FormID 任務擁有的主題」）。
- 沒有場景記錄（已透過 `scenediag 0x43B81F` 確認：「0x43B81F 不是場景」）。

這是一個 **導航和追踪任務** — 純粹為了在風盔城地下調查期間提供目標標記和羅盤引導。

## 重建筆記

基於源代碼：
- 該雜項任務由 [`43B81F zzzBMGuide`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:83) 代表，名稱為 `"斯坦達爾指南"`。
- 它包含 2 個目標（階段 0 和階段 10），對應兩個主要的調查分支：
  - 目標 0：「追蹤血跡」（初始調查階段）
  - 目標 10：「追蹤古老之血」（升級調查階段，可能與 `zzzBMMq02` 中的吸血鬼發現有關）
- 它在階段 20 完成（明確的 `CompleteQuest` 標記），並在階段 999 設有啟動關閉階段。
- 所有對話和互動都發生在第 2 幕主線任務 (`zzzBMMq01`, `zzzBMMq02`, `zzzBMMq03`) 中。

任務流程推論：
- 階段 0–10：任務激活；玩家跟隨由目標 0 和 10 放置的羅盤標記。
- 階段 20：任務完成（兩個調查分支都在主線任務中得到解決）。
- 階段 999：關閉（清理）。

公開驗證：
- 如果空間佈局重要，請檢查目標 0 和 10 的確切標靶位置（單元/引用）。
- 階段進展是由 `zzzBMMq01`、`zzzBMMq02` 還是父任務管理器腳本驅動。
- 此任務是與第 2 幕主線任務並行運行，還是作為父任務/包裝任務。
