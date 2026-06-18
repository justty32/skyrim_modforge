# API：JIntMap / JDB / JFormDB / JAtomic / 其餘

← [jcontainers](jcontainers.md)

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

