# 第 3 幕 支線任務 - 斯坦達爾指南

狀態：基於源代碼的切片。未發現對話或場景記錄；僅包含任務結構。

來源策略：
- FormID、EditorID、目標已透過 CLI questdiag 從 ESM 提取。
- 提取的 `quests.md` 連結用於目標參考。
- 沒有該任務擁有的對話主題（已透過 infodiag 確認）。
- 沒有場景記錄（已透過 scenediag 確認）。

## 任務記錄

[`43CBAE zzzCOGuide "斯坦達爾指南"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:360)

CLI：
- `questdiag Vigilant.esm 0x43CBAE`
- `infodiag Vigilant.esm 0x43CBAE` — 結果：未發現對話主題
- `scenediag Vigilant.esm 0x43CBAE` — 結果：不是場景記錄

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x43CBAE`
- EditorID: `zzzCOGuide`
- 名稱: `斯坦達爾指南`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `Misc` (雜項)
- 過濾器: `CO\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 1 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 22 | 無 | 空 |
| 24 | 無 | 空 |
| 30 | 無 | 空 |
| 35 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 60 | 無 | 空 |
| 70 | CompleteQuest | 空 |
| 999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 文本 |
|---:|---|---|
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:361) | 打破顫慄詛咒 |
| 20 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:362) | 打破墮落詛咒 |
| 22 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:363) | 前往朱利亞斯的房間 |
| 24 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:364) | 前往地下室 |
| 30 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:365) | 打破泡沫詛咒 |
| 35 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:366) | 獲得巴托洛房間的鑰匙 |
| 40 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:367) | 打破鎖鏈詛咒 |
| 50 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:368) | 打破嫉妒詛咒 |
| 60 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:369) | 釋放朱利亞斯 |

目標目標：
- 9 個目標，每個目標指向 1 個標靶。
- 所有標靶都帶有 `CompassMarkerIgnoresLocks` (羅盤標記忽略鎖定) 標記。
- CLI 未轉儲標靶引用；如果放置引用很重要，則需要更深入的 QUST 標靶檢查。

## 對話記錄

任務 `0x43CBAE` 沒有擁有的對話主題。推論：
- 這是一個 **純目標任務** — 沒有 NPC 驅動的對話。
- 目標可能是由環境行為觸發的（與受詛咒的物品互動、導航到特定地點、擊敗敵人、營救 NPC）。
- 階段進展沒有明確記錄（`CompleteQuest` 出現在階段 70 和 999，但日誌條目為空）。

假設：
- (推論) 階段 70 或 999 標誌著任務完成，可能是當玩家滿足所有主要目標時，透過程式化方式（例如透過腳本效果或任務別名更新）觸發的。

## 場景記錄

未發現以 `0x43CBAE` 為主機任務的場景記錄。該任務不包含場景暫存。

## 第 3 幕背景

zzzCOGuide 是 **第 3 幕（宅邸篇章）中的支線任務**。第 3 幕的主線任務是 [`065932 zzzCOMq01 "湮滅之子"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276)，涉及：
- 調查貴族宅邸（目標 30）
- 解決案件（與宅邸的詛咒有關）
- 擊敗朱利亞斯（目標 60）
- 逃離宅邸（目標 70）

「斯坦達爾指南」任務有多個重疊的地點/NPC 引用：
- 「前往朱利亞斯的房間」（目標 22） — 對應於 zzzCOMq01 的主要反派地點
- 「釋放朱利亞斯」（目標 60） — 直接引用了主線任務中的朱利亞斯
- 打破詛咒的目標暗示該任務涉及打破五個截然不同的詛咒：
  - 顫慄、墮落、泡沫、鎖鏈、嫉妒

這表明斯坦達爾指南是一個 **可選的謎題/挑戰任務**，玩家在宅邸中時可以承接，涉及打破物品或人身上的超自然詛咒作為支線目標。

## 目標翻譯筆記

- 五個詛咒目標中的 「Breack」 都是 「Break」(打破) 的拼寫錯誤（保留源代碼中的拼寫錯誤）。
- 詛咒名稱暗示了象徵性/情感性的主題：顫慄 (恐懼)、墮落 (惡習)、泡沫 (瘋狂？)、鎖鏈 (奴役/僕從)、嫉妒 (貪婪)。
- 「Go To Basement」使用了美式大寫風格（`To` 而非 `to`）。

## 相關記錄

第 3 幕主線任務：
- [`065932 zzzCOMq01 "湮滅之子"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276)

第 3 幕其他支線任務：
- [`324E7E zzzCOSubQ01 "繼任者"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:32)
- [`444115 zzzCOqOwl "織者之針 2"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:321)

通用對話：
- [`065EF0 zzzCOGenericDialogue "CO 通用對話"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:355)

## 重建筆記

基於源代碼：
- 該任務由 Vigilant.esm 中的 FormID `0x43CBAE` 與 EditorID `zzzCOGuide` 代表。
- 它包含 9 個目標，與宅邸中的 9 個羅盤標靶相關聯。
- 它沒有對話分支，也沒有場景記錄，這表明它是由純粹的環境機制（物品互動、NPC 遭遇、地點發現）驅動的，而不是 NPC 對話樹或分階段場景。
- 階段 0, 1, 10–60 似乎是中間進度；階段 70 和 999 均帶有 `CompleteQuest` 標記。

公開驗證：
- 透過單元/引用檢查，定位羅盤標靶所引用的實際受詛咒物品/NPC（顫慄詛咒、墮落詛咒等）。
- 確定階段 70 或 999 哪一個是實際的完成觸發器（兩者都標記為 `CompleteQuest`；可能 70 是預期的，999 是保險）。
- 檢查是否有任何任務別名或腳本驅動邏輯（未在 QUST 記錄中公開）限制了目標 10 → 20 → 22 → 24 → 30 → 35 → 40 → 50 → 60 → 70 的進度。
- 驗證「斯坦達爾指南」是可選的（玩家可以完全跳過並完成 zzzCOMq01）還是推進第 3 幕所必需的。
- 如果需要更完整的敘事重建背景，請檢查第 3 幕地點單元中名為朱利亞斯、巴托洛或相關角色的 NPC。
