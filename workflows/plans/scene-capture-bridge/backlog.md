# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就搬進 [phases.md](phases.md)（已落地實作記錄，標日期/DLL crc），從這裡刪除。

---

## ❌ 已否決（別再做第三次）

- **遊戲內 rebind（面板抓鍵改動作鍵）——放棄，改走 `.ini`**（使用者 2026-07-12 實機後拍板：「這太麻煩了，先隱藏掉這個功能吧，我們之後把他擺進 .ini 設定」）。**為什麼不再試**：面板（SKSE Menu Framework）**不暫停遊戲**，所以「抓玩家想綁的那顆鍵」永遠在跟**玩家手上還按著的鍵**搶同一條輸入串流——人剛用滑鼠點完 `Rebind`，手多半還在 WASD 上。**兩次嘗試、兩種設計都在實機失敗**：① P5（2026-07-11）armed 後來者不拒 → 綁成 W；② 重作（`ddf6324`，2026-07-12）加了保留鍵黑名單＋按下再放開才 commit → **使用者實機回報仍失敗**。現況＝**`SceneCaptureBridge.ini`**（SKSE 資料夾、缺檔自動生成、寫鍵名不寫 scancode、面板一顆 `reload keys from ini`）——檔案沒有那條賽道可輸：沒有 armed 狀態、沒有 input sink、沒有時序。實作與完整驗屍見 [phases.md](phases.md) 該兩節；抓鍵狀態機已從 `Modes`／`plugin.cpp` 移除（要看舊碼去 git `ddf6324`）。

---

## 待做（分檔）

| 檔案 | 內容 |
|------|------|
| [three-lines-a.md](backlog/three-lines-a.md) | 🗺️ 2026-08-20 使用者拍板三條線全開（建議順序）；A. 施法叫出選單 ＋ 物品欄式預覽 ＋ 最愛 |
| [three-lines-bc.md](backlog/three-lines-bc.md) | B. `sc nav` 遊戲內畫導航網格；C. `sc ed <xx>` 編輯既有物件狀態屬性 |
| [open-directions.md](backlog/open-directions.md) | 仍未做：使用者 2026-07-14 提的兩個新方向 |
