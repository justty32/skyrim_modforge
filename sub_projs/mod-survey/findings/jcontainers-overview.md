# 這個工具做什麼 + 工作原理

← [jcontainers](jcontainers.md)

## 一、這個工具做什麼 + 工作原理

JContainers 是一個 SKSE64 外掛，提供「JSON-like 可序列化容器」給 Papyrus 腳本。它解決的核心問題：

1. **Papyrus 陣列上限 128**：JArray 不受限，可存任意數量。
2. **無 Nested 資料結構**：Papyrus 只有平坦陣列；JMap 可嵌套，做出 `{key: {subkey: value}}` 結構。
3. **Form 作為 key**：JFormMap 讓 Form 直接成為 map 的 key，取代 StorageUtil 的字串 key。
4. **外部 JSON 序列化**：容器可以直接讀/寫 JSON 檔案，與外部工具雙向互通。
5. **全域資料庫（JDB）**：提供 process-level 的全域 key-value 存儲，不綁 save，可跨 mod 共享。

**工作原理**：所有容器在 C++ 層管理，Papyrus 端透過 `Int`（物件 handle）參照它們。容器有**生命週期管理**機制（retain/release/pool），不同於 StorageUtil 的自動管理，JContainers 物件若無人持有則會被 GC 清除。這是最重要的使用注意事項。

**三種使用 namespace 方式**：
- `JArray.object()` / `JMap.getInt()` 等——標準 script 呼叫
- `JValue.readFromFile()` 等——共用基底功能
- `JContainers_DomainExample`——flat namespace 別名（`JArray_object()` 等），供跨腳本呼叫

---

