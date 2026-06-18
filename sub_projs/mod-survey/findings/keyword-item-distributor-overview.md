# 這個 mod 做什麼 + 怎麼運作

← [keyword-item-distributor](keyword-item-distributor.md)

## 一、這個 mod 做什麼 + 怎麼運作

KID 是一個 SKSE DLL plugin（`po3_KeywordItemDistributor.dll`）。它在**遊戲啟動時**讀取 Data 資料夾下所有以 `_KID.ini` 結尾的設定檔，按設定把指定的 **Keyword（KYWD）掛到各種 item/object record 上**，完全不需要修改任何 ESP/ESM。

### 運作流程

1. 遊戲啟動，SKSE 載入所有 plugin DLL。
2. KID 掃描 `Data/*.ini`（含子資料夾），篩出 `_KID.ini` 結尾的檔案。
3. 每行 `Keyword = ...` 依設定的 type/filter/traits/chance 批次套用。
4. Keyword 物件如果 KID 找不到對應的 FormID/EditorID，**可以動態建立一個新的 KYWD**（僅限此特殊模式）。
5. 套用結果寫進 `po3_KeywordItemDistributor.log`（`My Games/Skyrim Special Edition/SKSE/`）供除錯。

### 與 ESP 模式的差異

KID 的修改發生在**記憶體層（runtime）**，不寫入任何 ESP。多個 mod 可以各自帶一份 `_KID.ini`，互不衝突，不需要相容補丁。

---

