# JContainers SE — 完整 API 深挖

版本：API 4 / Feature 2（SE 版）  
原始碼來源：`~/skyrim_mods/unzip/JContainers SE/Data/scripts/source/*.psc`

---

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

### 2.5 JIntMap — Int key 關聯容器

與 JMap / JFormMap 相同介面，key 型別為 `Int`。適合用整數（如 FormID、index、timestamp）作 key 的場景。

### 2.6 JDB — 全域資料庫

JDB 是 process-level 全域 JMap，跨 mod、跨腳本共享，不綁 save game。

| Function | 說明 |
|---|---|
| `solveFlt / solveInt / solveStr / solveObj / solveForm(String path, T default)` | 從 JDB 依 path 讀取（path 格式：`.modKey.subkey`） |
| `solveFltSetter / solveIntSetter / …(String path, T value, Bool createMissingKeys=false)` | 依 path 寫入 |
| `setObj(String key, Int object)` | 把一個容器關聯到頂層 key |
| `hasPath(String path) → Bool` | path 是否可解析 |
| `allKeys() → Int` | 所有頂層 key → JArray |
| `allValues() → Int` | 所有頂層 value → JArray |
| `writeToFile(String path)` | 把整個 JDB 寫出為 JSON |
| `root() → Int` | 取 JDB 底層的 JMap handle（已 retained，可直接用） |

**JDB 用法慣例**：
```papyrus
; 初始化（通常在 mod 啟動時）
int myData = JMap.object()
JDB.setObj("mymod", myData)  ; 頂層 key 用 mod name

; 任何地方讀寫
JDB.solveIntSetter(".mymod.playerLevel", akActor.GetLevel(), true)
int lv = JDB.solveInt(".mymod.playerLevel", 0)
```

### 2.7 JFormDB — 以 Form 為 key 的全域 DB

JFormDB 是 JDB 的 Form-keyed 版本，內部是 `{storageName → {Form → JMap}}` 三層結構。

| Function | 說明 |
|---|---|
| `setEntry(String storageName, Form fKey, Int entry)` | 設定 Form 關聯的容器（entry=0 刪除） |
| `makeEntry(String storageName, Form fKey) → Int` | 取得或建立 Form 的 JMap entry |
| `findEntry(String storageName, Form fKey) → Int` | 尋找，不存在回 0 |
| `solveFlt / solveInt / … (Form fKey, String path, T default)` | 依 Form key + path 讀取 |
| `solveFltSetter / … (Form fKey, String path, T value, Bool createMissingKeys=false)` | 依 Form key + path 寫入 |
| `hasPath(Form fKey, String path) → Bool` | path 是否可解析 |
| `getInt / getFlt / … (Form fKey, String key)` | JMap-like 直接存取 |
| `setInt / setFlt / … (Form fKey, String key, T value)` | JMap-like 直接存取 |

### 2.8 JAtomic — 原子操作

對容器內路徑的值做原子算術，回傳前一個值（fetch-and-modify 語意）。適合多腳本共存時的計數器、lock 等。

| Function | 說明 |
|---|---|
| `fetchAddInt / fetchAddFlt(object, path, value, initialValue=0, createMissingKeys=false)` | x += v |
| `fetchMultInt / fetchMultFlt` | x *= v |
| `fetchModInt` | x %= v |
| `fetchDivInt / fetchDivFlt` | x /= v |
| `fetchAndInt / fetchXorInt / fetchOrInt` | 位元運算 |
| `exchangeInt / exchangeFlt / exchangeStr / exchangeForm / exchangeObj` | 原子交換，回舊值 |
| `compareExchangeInt / …` | CAS（compare-and-swap）語意 |

### 2.9 JString — 字串工具

| Function | 說明 |
|---|---|
| `JString.wrap(String sourceText, Int charactersPerLine=60) → Int` | 把長字串按行長包裝，回傳 JArray（每行一個元素） |

### 2.10 JContainers — 版本與工具

| Function | 說明 |
|---|---|
| `APIVersion() → Int` | 目前應回傳 4 |
| `featureVersion() → Int` | 目前應回傳 2 |
| `isInstalled() → Bool` | 確認正確安裝（驗證版本） |
| `fileExistsAtPath(String path) → Bool` | 絕對路徑下的檔案是否存在 |
| `contentsOfDirectoryAtPath(String dirPath, String extension="") → String[]` | 列出目錄內容 |
| `removeFileAtPath(String path)` | 刪除檔案或目錄 |
| `userDirectory() → String` | 回傳 `My Games/Skyrim Special Edition/JCUser/` 路徑 |

### 2.11 JLua — Lua 整合（實驗性）

| Function | 說明 |
|---|---|
| `evalLuaFlt / evalLuaInt / evalLuaStr / evalLuaObj / evalLuaForm(code, transport, default)` | 執行 Lua 程式碼，transport 是可傳入的容器 |
| `setStr / setFlt / setInt / setForm / setObj(key, value, transport=0)` | 設定 Lua 全域變數 |

---

## 三、ModForge 生成 Papyrus script 的可利用模式

### Pattern 1：外部 JSON 讀取設定

```papyrus
; 從外部 JSON 讀入 follower 設定表
int config = JValue.readFromFile("Data/SKSE/Plugins/MyMod/config.json")
string voice = JMap.getStr(config, "voiceType", "Female")
float weight = JMap.getFlt(config, "commentaryWeight", 1.0)
```

### Pattern 2：per-Form 複雜狀態（JFormDB）

```papyrus
; 在 NPC Form 上掛複雜結構（關係 + 情緒 + 任務進度）
JFormDB.solveIntSetter(targetNPC, ".mymod.relationship", 2, true)
JFormDB.solveFloatSetter(targetNPC, ".mymod.lastTalkTime", Utility.GetCurrentRealTime(), true)

; 讀取
int rel = JFormDB.solveInt(targetNPC, ".mymod.relationship", 0)
```

### Pattern 3：動態 topic pool（JArray + JDB）

```papyrus
; 建立 commentary pool 並存入 JDB
int pool = JArray.objectWithStrings(new string[3])
JArray.addStr(pool, "看到了嗎？")
JArray.addStr(pool, "有趣的地方。")
JDB.setObj("mymod_commentaryPool", pool)

; 隨機取一句
int pool = JDB.solveObj(".mymod_commentaryPool", 0)
int idx = Utility.RandomInt(0, JArray.count(pool) - 1)
string line = JArray.getStr(pool, idx, "")
```

### Pattern 4：Pool 管理臨時物件

```papyrus
; 批次建立臨時物件時用 pool 防止 GC
int tempMap = JValue.addToPool(JMap.object(), "mymod_tempSession")
int tempArr = JValue.addToPool(JArray.object(), "mymod_tempSession")
; 使用完畢
JValue.cleanPool("mymod_tempSession")
```

### Pattern 5：跨存檔的 Form 索引表（JFormDB + writeToFile）

```papyrus
; 把整個 FormDB 的 storageName 存到磁碟
JDB.writeToFile("Data/SKSE/Plugins/MyMod/persistent.json")
; 遊戲啟動時讀回
int loaded = JValue.readFromFile("Data/SKSE/Plugins/MyMod/persistent.json")
JDB.setObj("mymod", loaded)
```

---

## 四、對 ModForge 的參考價值

| 功能 | 評估 | 說明 |
|---|---|---|
| `JValue.readFromFile / writeToFile` | **可直接生成** | 外部 JSON 讀寫 pattern 固定，適合 config 系統 |
| `JMap.object / getInt / setInt / nextKey` | **可直接生成** | string-keyed KV + 迭代 pattern 固定 |
| `JArray.object / addStr / getStr / count` | **可直接生成** | 動態列表 pattern 固定 |
| `JFormDB.solveIntSetter / solveInt` | **可直接生成**（推斷） | per-Form 嵌套狀態是最常用 pattern |
| `JDB.setObj / solveInt` | **可直接生成**（推斷） | 全域設定/狀態，跨 mod 共享 |
| `JValue.retain / release / addToPool / cleanPool` | **需警告生成** | 生命週期管理是最容易出錯的部分；生成時需確保成對 |
| `JAtomic.fetchAddInt` | **純參考** | 多腳本並發計數器；Papyrus 本身單執行緒，此功能偏工具層 |
| `JLua.evalLua*` | **純參考** | 實驗性，不建議生產環境使用 |
| `JFormMap` 作為臨時 Form→value map | **可直接生成**（推斷） | 用於 scene 內批次 Form 處理 |
| `JArray.unique / sort` | **可直接生成** | 去重 set 操作 pattern 固定 |
| `JString.wrap` | **純參考** | 文字排版；Papyrus 顯示系統少用 |

### 與 PapyrusUtil 的選擇建議

| 場景 | 建議 |
|---|---|
| 簡單 per-NPC int/float/string 值 | **PapyrusUtil.StorageUtil**（更輕量，自動隨 save 管理） |
| 跨存檔設定 / 外部可編輯 config | **PapyrusUtil.JsonUtil**（路徑 API 夠用） |
| Actor package 臨時 override | **PapyrusUtil.ActorUtil**（專用 API） |
| Cell 掃描 NPC/物件 | **PapyrusUtil.MiscUtil**（唯一選擇） |
| 複雜嵌套資料（如 follower 多欄位記憶） | **JContainers.JFormDB**（path 語法比多個 StorageUtil key 乾淨） |
| 動態 topic pool / 大型陣列 | **JContainers.JArray**（無 128 上限） |
| Form 作為 map key（如 Form→狀態表） | **JContainers.JFormMap / JFormDB** |
| 跨 mod 共享結構化資料 | **JContainers.JDB**（全域，有命名空間） |
| 外部 JSON schema 雙向讀寫 | **JContainers.JValue.readFromFile/writeToFile**（比 JsonUtil 更完整支援 nested JSON） |

**總結**：PapyrusUtil 是「簡單 + 自動管理」，JContainers 是「強大 + 需手動管理生命週期」。選 JContainers 的唯一理由是需要 nested 結構、超大陣列、Form-as-key，或是要讀寫 JSON 對結構有嚴格要求。

> ⚠️ 以上「可生成 / 純參考」是本 survey 推斷，**未查 ModForge src/**，可能有誤判。確認需另做 code pass。
