# API：JValue / JArray / JMap / JFormMap

← [jcontainers](jcontainers.md)

## 二、完整 API 表

### 2.1 JValue — 生命週期管理與共用操作

JValue 提供所有容器型別共用的介面，所有容器都繼承這些功能。

**生命週期管理**：

| Function | 簽名 | 說明 |
|---|---|---|
| `retain` | `(Int object, String tag="") → Int` | 保留物件，防止 GC。回傳 object 本身 |
| `release` | `(Int object) → Int` | 釋放，回傳 0（可用 `obj = JValue.release(obj)` 置零） |
| `releaseAndRetain` | `(Int previousObject, Int newObject, String tag="") → Int` | 原子 release + retain，用於替換持有的物件 |
| `releaseObjectsWithTag` | `(String tag)` | 釋放所有帶此 tag 的物件 |
| `zeroLifetime` | `(Int object) → Int` | 縮短臨時物件的存活時間，讓 GC 盡早回收 |
| `addToPool` | `(Int object, String poolName) → Int` | 加入具名 pool，pool 持有 → 物件不被 GC |
| `cleanPool` | `(String poolName)` | 清空 pool，pool 持有的物件被釋放 |

**複製**：

| Function | 說明 |
|---|---|
| `shallowCopy(Int object) → Int` | 淺複製（子物件不複製） |
| `deepCopy(Int object) → Int` | 深複製（遞迴複製所有子物件） |

**型別檢查**：

| Function | 說明 |
|---|---|
| `isExists(Int object) → Bool` | 是否為有效物件（非 null） |
| `isArray / isMap / isFormMap / isIntegerMap(Int) → Bool` | 型別判斷 |
| `empty(Int object) → Bool` | 是否為空容器 |
| `count(Int object) → Int` | 元素數量 |
| `clear(Int object)` | 清空容器 |

**JSON 序列化**：

| Function | 說明 |
|---|---|
| `readFromFile(String filePath) → Int` | 讀入 JSON 檔，回傳容器 handle |
| `readFromDirectory(String dirPath, String extension="") → Int` | 掃描目錄，回傳 `{filename: container}` 的 JMap |
| `objectFromPrototype(String prototype) → Int` | 從 JSON 字串建立容器 |
| `writeToFile(Int object, String filePath)` | 把容器寫出為 JSON 檔案 |

**Path 解析（類 XPath/JSON Pointer）**：

| Function | 簽名 | 說明 |
|---|---|---|
| `hasPath` | `(Int object, String path) → Bool` | 路徑是否可解析 |
| `solvedValueType` | `(Int object, String path) → Int` | 路徑指向的值型別（0=無,1=none,2=int,3=float,4=form,5=object,6=string） |
| `solveFlt / solveInt / solveStr / solveObj / solveForm` | `(Int object, String path, T default) → T` | 依路徑取值 |
| `solveFltSetter / solveIntSetter / …` | `(Int object, String path, T value, Bool createMissingKeys=false) → Bool` | 依路徑設值，可自動建立缺失的 key |

**Lua 整合（實驗性）**：

| Function | 說明 |
|---|---|
| `evalLuaFlt / evalLuaInt / evalLuaStr / evalLuaObj / evalLuaForm` | 對容器執行 Lua 程式碼，取回結果 |

### 2.2 JArray — 有序集合

key 是 index（Int），支援負數 index（從尾端算）。

| Function | 簽名 | 說明 |
|---|---|---|
| `object` | `() → Int` | 建立空 JArray |
| `objectWithSize` | `(Int size) → Int` | 建立指定大小（填 None） |
| `objectWithInts / Strings / Floats / Booleans / Forms` | `(T[] values) → Int` | 從 Papyrus 陣列建立 |
| `subArray` | `(Int object, Int start, Int end) → Int` | 取子陣列 [start, end) |
| `addFromArray` | `(Int object, Int source, Int insertAtIndex=-1)` | 把另一個 JArray 的元素插入 |
| `addFromFormList` | `(Int object, FormList source, Int insertAtIndex=-1)` | 從 FormList 匯入 |
| `getInt / getFlt / getStr / getObj / getForm` | `(Int object, Int index, T default) → T` | 依 index 讀取 |
| `setInt / setFlt / setStr / setObj / setForm` | `(Int object, Int index, T value)` | 依 index 寫入 |
| `addInt / addFlt / addStr / addObj / addForm` | `(Int object, T value, Int addToIndex=-1)` | 插入值（-1 = 尾端） |
| `count` | `(Int object) → Int` | 元素數量 |
| `clear` | `(Int object)` | 清空 |
| `eraseIndex` | `(Int object, Int index)` | 刪除 index 處元素 |
| `eraseRange` | `(Int object, Int first, Int last)` | 刪除 [first, last] 區間 |
| `eraseInteger / eraseFloat / eraseString / eraseObject / eraseForm` | `(Int object, T value) → Int` | 刪除所有符合值，回刪除數量 |
| `findInt / findFlt / findStr / findObj / findForm` | `(Int object, T value, Int searchStartIndex=0) → Int` | 搜尋，回 index 或 -1 |
| `countInteger / countFloat / countString / countObject / countForm` | `(Int object, T value) → Int` | 計算出現次數 |
| `valueType` | `(Int object, Int index) → Int` | 取 index 處的值型別 |
| `swapItems` | `(Int object, Int index1, Int index2)` | 交換兩個 index |
| `sort` | `(Int object) → Int` | 升序排序，回 object 自身 |
| `unique` | `(Int object) → Int` | 去重後排序（Set 語意），回 object 自身 |
| `reverse` | `(Int object) → Int` | 反轉，回 object 自身 |
| `asIntArray / asFloatArray / asStringArray / asFormArray` | `(Int object) → T[]` | 轉成 Papyrus 陣列 |
| `writeToIntegerPArray / writeToFloatPArray / writeToFormPArray / writeToStringPArray` | 多參數 | 寫入既有 Papyrus 陣列（可指定範圍與方向） |

### 2.3 JMap — string key 關聯容器

| Function | 簽名 | 說明 |
|---|---|---|
| `object` | `() → Int` | 建立空 JMap |
| `getInt / getFlt / getStr / getObj / getForm` | `(Int object, String key, T default) → T` | 依 key 讀取 |
| `setInt / setFlt / setStr / setObj / setForm` | `(Int object, String key, T value)` | 依 key 寫入（key 不存在則建立） |
| `hasKey` | `(Int object, String key) → Bool` | 是否有此 key |
| `valueType` | `(Int object, String key) → Int` | key 對應值的型別 |
| `removeKey` | `(Int object, String key) → Bool` | 刪除 key-value pair |
| `count` | `(Int object) → Int` | pair 數量 |
| `clear` | `(Int object)` | 清空 |
| `addPairs` | `(Int object, Int source, Bool overrideDuplicates)` | 從另一個 map 合併 |
| `allKeys` | `(Int object) → Int` | 所有 key → JArray |
| `allKeysPArray` | `(Int object) → String[]` | 所有 key → Papyrus string[] |
| `allValues` | `(Int object) → Int` | 所有 value → JArray |
| `nextKey` | `(Int object, String previousKey="", String endKey="") → String` | 迭代用：取下一個 key |
| `getNthKey` | `(Int object, Int keyIndex) → String` | 取第 N 個 key（O(n/2) 複雜度） |

**JMap 迭代 pattern**：
```papyrus
string key = JMap.nextKey(myMap, previousKey="", endKey="")
while key != ""
  ; 處理 JMap.getInt(myMap, key) 等
  key = JMap.nextKey(myMap, key, endKey="")
endwhile
```

### 2.4 JFormMap — Form key 關聯容器

與 JMap 完全相同的介面，但 key 型別是 `Form`（不是 String）。

| Function | 說明 |
|---|---|
| `object() → Int` | 建立空 JFormMap |
| `getInt / getFlt / getStr / getObj / getForm (Int object, Form key, T default)` | 依 Form key 讀取 |
| `setInt / setFlt / setStr / setObj / setForm (Int object, Form key, T value)` | 依 Form key 寫入 |
| `hasKey(Int object, Form key) → Bool` | 是否有此 Form key |
| `removeKey(Int object, Form key) → Bool` | 刪除 |
| `allKeysPArray(Int object) → Form[]` | 所有 key → Form[] |
| `nextKey(Int object, Form previousKey=None, Form endKey=None) → Form` | 迭代 |
| `getNthKey(Int object, Int keyIndex) → Form` | 取第 N 個 key |

