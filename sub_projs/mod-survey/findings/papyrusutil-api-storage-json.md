# API：StorageUtil（存取/列表）+ JsonUtil

← [papyrusutil](papyrusutil.md)

## 二、完整 API 表

### 2.1 StorageUtil — 儲存 / 讀取 scalar 值

| Function | 簽名 | 說明 |
|---|---|---|
| `SetIntValue` | `(Form ObjKey, string KeyName, int value) → int` | 在 Form 或 global（none）存 int |
| `SetFloatValue` | `(Form ObjKey, string KeyName, float value) → float` | 同上，float |
| `SetStringValue` | `(Form ObjKey, string KeyName, string value) → string` | 同上，string |
| `SetFormValue` | `(Form ObjKey, string KeyName, Form value) → Form` | 同上，Form |
| `GetIntValue` | `(Form ObjKey, string KeyName, int missing=0) → int` | 讀取，找不到回 missing |
| `GetFloatValue` | `(Form, string, float missing=0.0) → float` | 同上 |
| `GetStringValue` | `(Form, string, string missing="") → string` | 同上 |
| `GetFormValue` | `(Form, string, Form missing=none) → Form` | 同上 |
| `HasIntValue` | `(Form, string) → bool` | 是否已設定 |
| `HasFloatValue` | `(Form, string) → bool` | 同上 |
| `HasStringValue` | `(Form, string) → bool` | 同上 |
| `HasFormValue` | `(Form, string) → bool` | 同上 |
| `UnsetIntValue` | `(Form, string) → bool` | 刪除，成功回 true |
| `UnsetFloatValue` | `(Form, string) → bool` | 同上 |
| `UnsetStringValue` | `(Form, string) → bool` | 同上 |
| `UnsetFormValue` | `(Form, string) → bool` | 同上 |
| `PluckIntValue` | `(Form, string, int missing=0) → int` | 讀後刪（原子 get-and-remove） |
| `PluckFloatValue` | `(Form, string, float missing=0.0) → float` | 同上 |
| `PluckStringValue` | `(Form, string, string missing="") → string` | 同上 |
| `PluckFormValue` | `(Form, string, Form missing=none) → Form` | 同上 |
| `AdjustIntValue` | `(Form, string, int amount) → int` | 原子 +=，key 不存在時初始化為 amount |
| `AdjustFloatValue` | `(Form, string, float amount) → float` | 同上 |

### 2.2 StorageUtil — 列表操作

所有列表函數都有 int / float / string / Form 四個型別版本，下表以「XList」代表四種。

| Function | 簽名（簡） | 說明 |
|---|---|---|
| `XListAdd` | `(Form, string, X value, bool allowDuplicate=true) → int` | 尾端加入，回新 index |
| `XListGet` | `(Form, string, int index) → X` | 依 index 讀取 |
| `XListSet` | `(Form, string, int index, X value) → X` | 依 index 覆寫，回舊值 |
| `XListInsert` | `(Form, string, int index, X value) → bool` | 在 index 插入 |
| `XListRemove` | `(Form, string, X value, bool allInstances=false) → int` | 依值刪除，回刪除數量 |
| `XListRemoveAt` | `(Form, string, int index) → bool` | 依 index 刪除 |
| `XListPluck` | `(Form, string, int index, X missing) → X` | 讀後刪 |
| `XListShift` | `(Form, string) → X` | 取出並刪除第一個（queue 語意） |
| `XListPop` | `(Form, string) → X` | 取出並刪除最後一個（stack 語意） |
| `XListAdjust` | `(Form, string, int index, X amount) → X` | 依 index 原子 +=（int/float 才有） |
| `XListCount` | `(Form, string) → int` | 列表長度 |
| `XListCountValue` | `(Form, string, X value, bool exclude=false) → int` | 計算特定值的出現次數 |
| `XListFind` | `(Form, string, X value) → int` | 線性搜尋，回 index 或 -1 |
| `XListHas` | `(Form, string, X value) → bool` | 是否包含值 |
| `XListClear` | `(Form, string) → int` | 清空，回清除前長度 |
| `XListSort` | `(Form, string) → void` | 升序排序 |
| `XListSlice` | `(Form, string, X[] slice, int startIndex=0) → void` | 複製 list 到 Papyrus 陣列 |
| `XListResize` | `(Form, string, int toLength, X filler) → int` | 調整長度（最多 500） |
| `XListCopy` | `(Form, string, X[] copy) → bool` | 從 Papyrus 陣列複製進 list |
| `XListToArray` | `(Form, string) → X[]` | 整個 list 轉成 Papyrus 陣列 |
| `XListRandom` | `(Form, string) → X` | 隨機取一個值（mt19937） |
| `FormListFilterByTypes` | `(Form, string, int[] FormTypeIDs, bool ReturnMatching=true) → Form[]` | 依 form type 過濾列表 |
| `FormListFilterByType` | `(Form, string, int FormTypeID, bool ReturnMatching=true) → Form[]` | 單 type 版便利包裝 |

**Prefix 計數 / 清除（跨物件）**：

| Function | 說明 |
|---|---|
| `CountIntValuePrefix(string PrefixKey)` | 計算所有物件上 key 前綴符合的 int value 數量 |
| `CountAllPrefix(string PrefixKey)` | 所有型別全部計 |
| `ClearIntValuePrefix(string PrefixKey)` | 清除所有物件上 key 前綴符合的 int value |
| `ClearAllPrefix(string PrefixKey)` | 所有型別全部清 |
| `CountObjXxxPrefix(Form, string)` | 限定物件的版本 |
| `ClearObjXxxPrefix(Form, string)` | 限定物件的版本 |

### 2.3 JsonUtil — 外部 JSON 檔案讀寫

基礎路徑：`data/skse/plugins/StorageUtilData/`。路徑中 `../` 表示向上一層。

| Function | 說明 |
|---|---|
| `Load(string FileName) → bool` | 手動載入（通常不需要，自動） |
| `Save(string FileName, bool minify=false) → bool` | 手動儲存 |
| `Unload(string FileName, bool saveChanges=true, bool minify=false) → bool` | 卸載並選擇是否存檔 |
| `IsPendingSave(string FileName) → bool` | 是否有未儲存的修改 |
| `IsGood(string FileName) → bool` | 檔案是否載入成功無錯誤 |
| `GetErrors(string FileName) → string` | 取得 JSON 解析錯誤訊息 |
| `JsonInFolder(string folderPath) → string[]` | 列出目錄中所有 .json 檔名 |
| `JsonExists(string FileName) → bool` | 檔案是否存在 |

scalar 讀寫（與 StorageUtil 相同模式，但第一個參數是 `string FileName`）：  
`SetIntValue / GetIntValue / HasIntValue / UnsetIntValue / AdjustIntValue`  
（同理 Float / String / Form 四種型別）

列表操作（同 StorageUtil 列表模式，第一個參數改為 `string FileName`）：  
`IntListAdd / IntListGet / IntListSet / IntListRemove / IntListRemoveAt / IntListInsertAt / IntListClear / IntListCount / IntListFind / IntListHas / IntListSlice / IntListResize / IntListCopy / IntListToArray / IntListRandom / IntListAdjust / IntListCountValue`  
（同理 Float / String / Form 四種型別）

**Path API（實驗性 JSON 路徑解析）**：

| Function | 說明 |
|---|---|
| `SetPathIntValue(FileName, Path, value)` | 依 `.key.subkey[idx]` 路徑設值 |
| `GetPathIntValue(FileName, Path, missing=0) → int` | 依路徑讀值 |
| `GetPathBoolValue(FileName, Path, missing=false) → bool` | 便利包裝（底層是 int） |
| `SetRawPathValue(FileName, Path, RawJSON) → bool` | 直接寫入 raw JSON 字串 |
| `PathIntElements(FileName, Path, invalidType=0) → int[]` | 把路徑指向的陣列全部取出 |
| `FindPathIntElement(FileName, Path, toFind) → int` | 在陣列中搜尋 |
| `PathCount(FileName, Path) → int` | 路徑下的元素數量 |
| `PathMembers(FileName, Path) → string[]` | 路徑下的 key 名稱列表 |
| `CanResolvePath / IsPathString / IsPathNumber / IsPathForm / IsPathBool / IsPathArray / IsPathObject` | 路徑型別檢查 |
| `SetPathIntArray(FileName, Path, int[] arr, bool append=false)` | 把 Papyrus 陣列寫入路徑 |
| `ClearPath(FileName, Path)` | 清除路徑下的值 |
| `ClearPathIndex(FileName, Path, int Index)` | 清除路徑下特定 index |
| `ClearAll(FileName)` | 清空整個 JSON 檔案 |

