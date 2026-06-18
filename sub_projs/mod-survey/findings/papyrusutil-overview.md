# 這個工具做什麼 + 工作原理

← [papyrusutil](papyrusutil.md)

## 一、這個工具做什麼 + 工作原理

PapyrusUtil 是一個 SKSE64 native 外掛，提供 Papyrus 本身缺少的底層能力：

1. **持久化 key-value 存儲**（StorageUtil）：在任意 Form 上（或 global namespace）存 int/float/string/Form 值與列表。資料綁在 save game 裡，Form 被刪除後自動清理。
2. **外部 JSON 讀寫**（JsonUtil）：資料不綁 save，存在 `data/skse/plugins/StorageUtilData/` 下的 JSON 檔案，可跨存檔存取、可編輯、可版本化。
3. **Actor Package Override**（ActorUtil）：對 Actor 動態疊加 Package，優先度 0-100，會進 save，比 AI Package stack 更靈活。
4. **Cell 掃描 + 雜項**（MiscUtil）：掃描 cell 內的 Actor/Object、檔案操作、console 輸出。
5. **陣列 utility**（PapyrusUtil）：動態陣列操作（push/remove/merge/slice/diff 等），繞過 Papyrus 128 元素上限問題（透過 StorageUtil 間接）。
6. **ObjectUtil**（已在 SSE 停用）：原本提供 animation event 替換，目前函數體為空，不可用。

工作原理：所有功能都是 native C++ 實作，Papyrus 腳本只是呼叫介面（`global native`）。StorageUtil 資料寫入 SKSE co-save（.skse 附掛），JsonUtil 資料寫入磁碟 JSON 檔案（遊戲存擋時自動落地）。

---

