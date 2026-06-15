# PapyrusUtil — 完整 API 深挖

版本：4.6 AE/SE（2024-01-18）  
原始碼來源：`~/skyrim_mods/unzip/PapyrusUtil/Scripts/Source/*.psc`

---

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

### 2.4 ActorUtil — Actor Package Override

| Function | 簽名 | 說明 |
|---|---|---|
| `AddPackageOverride` | `(Actor targetActor, Package targetPackage, int priority=30, int flags=0)` | 加入 package，priority 0-100（100 最高），進 save |
| `RemovePackageOverride` | `(Actor targetActor, Package targetPackage) → bool` | 移除指定 package override |
| `CountPackageOverride` | `(Actor targetActor) → int` | 計算現有 override 數量（包含條件未滿足的） |
| `ClearPackageOverride` | `(Actor targetActor) → int` | 清除此 Actor 所有 override（包含其他 mod 的！） |
| `RemoveAllPackageOverride` | `(Package targetPackage) → int` | 從所有 Actor 移除此 package |

> **警告**：`ClearPackageOverride` 會清掉所有 mod 設的 override，使用需謹慎。

### 2.5 MiscUtil — 雜項 utility

| Function | 簽名 | 說明 |
|---|---|---|
| `ScanCellNPCs` | `(ObjectReference CenterOn, float radius=0.0, Keyword HasKeyword=none, bool IgnoreDead=true) → Actor[]` | 掃描 Cell 內活著的 Actor，radius=0 掃整個 cell |
| `ScanCellNPCsByFaction` | `(Faction FindFaction, ObjectReference CenterOn, float radius=0.0, int minRank=0, int maxRank=127, bool IgnoreDead=true) → Actor[]` | 依 faction 過濾 |
| `ScanCellObjects` | `(int formType, ObjectReference CenterOn, float radius=0.0, Keyword HasKeyword=none) → ObjectReference[]` | 掃描特定 form type 的物件 |
| `FilesInFolder` | `(string directory, string extension="*") → string[]` | 列出目錄中的檔案 |
| `FoldersInFolder` | `(string directory) → string[]` | 列出目錄中的子目錄 |
| `FileExists` | `(string fileName) → bool` | 檔案是否存在 |
| `ReadFromFile` | `(string fileName) → string` | 讀取檔案為字串（勿讀大檔） |
| `WriteToFile` | `(string fileName, string text, bool append=true, bool timestamp=false) → bool` | 寫字串到檔案 |
| `PrintConsole` | `(string text)` | 輸出到 console |
| `GetRaceEditorID` | `(Race raceForm) → string` | 取得種族 EditorID |
| `GetActorRaceEditorID` | `(Actor actorRef) → string` | 取得 Actor 的種族 EditorID |
| `ToggleFreeCamera` | `(bool stopTime=false)` | 開/關自由鏡頭（TFC） |
| `SetFreeCameraSpeed` | `(float speed)` | 設定自由鏡頭速度 |
| `SetFreeCameraState` | `(bool enable, float speed=10.0)` | 設定自由鏡頭狀態 |

### 2.6 PapyrusUtil — 陣列操作 utility

| 分類 | Function（以 int 為例，同理 float/string/Form/Actor/ObjRef/Alias） | 說明 |
|---|---|---|
| 建立 | `ActorArray(int size, Actor filler=none)` | 建立指定大小的 Actor 陣列 |
| 建立 | `ObjRefArray(int size, ObjectReference filler=none)` | 建立 ObjRef 陣列 |
| Resize | `ResizeActorArray(Actor[], int toSize, Actor filler=none)` | 調整陣列大小 |
| Push | `PushInt(int[], int push) → int[]` | 尾端加一個值，回新陣列 |
| Remove | `RemoveInt(int[], int ToRemove) → int[]` | 移除所有符合的值 |
| RemoveDupe | `RemoveDupeInt(int[]) → int[]` | 去重 |
| Diff | `GetDiffInt(int[], int[], bool CompareBoth=false, bool IncludeDupes=false) → int[]` | 取差集 |
| Intersect | `GetMatchingInt(int[], int[]) → int[]` | 取交集 |
| Count | `CountInt(int[], int EqualTo) → int` | 計算符合值的數量 |
| Merge | `MergeIntArray(int[], int[], bool RemoveDupes=false) → int[]` | 合併兩個陣列 |
| Slice | `SliceIntArray(int[], int StartIndex, int EndIndex=-1) → int[]` | 切片 |
| Sort | `SortIntArray(int[], bool descending=false)` | 就地排序 |
| String | `StringSplit(string, string Delimiter=",") → string[]` | 分割字串（自動 trim 空白） |
| String | `StringJoin(string[], string Delimiter=",") → string` | 合併字串 |
| Math | `ClampInt(int value, int min, int max) → int` | 夾緊數值 |
| Math | `WrapInt(int value, int end, int start=0) → int` | 環繞（適合陣列 index 繞圈） |
| Math | `SignInt(bool doSign, int value) → int` | 有/無號轉換 |
| Math | `AddIntValues(int[]) → int` | 陣列加總 |

---

## 三、ModForge 生成 Papyrus script 的可利用模式

以下 pattern 在 spec 層面可作為標準積木：

### Pattern 1：per-NPC 輕量記憶（StorageUtil）

```papyrus
; 記錄 NPC 上次對話時間（cooldown 防刷）
StorageUtil.SetFloatValue(akSpeaker, "mymod_lastGreet", Utility.GetCurrentRealTime())
float lastTime = StorageUtil.GetFloatValue(akSpeaker, "mymod_lastGreet", 0.0)
```

用途：follower 記憶、關係狀態、交互 cooldown、任務進度標記。

### Pattern 2：跨存檔外部設定（JsonUtil）

```papyrus
; 讀取外部 JSON 設定（可被玩家/其他 mod 在遊戲外編輯）
int cooldown = JsonUtil.GetIntValue("../MyMod/config", "greetCooldown", 300)
; 路徑 API：讀嵌套結構
float weight = JsonUtil.GetPathFloatValue("../MyMod/data", ".commentary.lydia.weight", 1.0)
```

### Pattern 3：動態 Package Override（ActorUtil）

```papyrus
; 讓 NPC 臨時站到指定位置
ActorUtil.AddPackageOverride(myFollower, myStandPackage, 50)
; 場景結束後清除
ActorUtil.RemovePackageOverride(myFollower, myStandPackage)
```

注意：priority 50 比普通 AI（30）高但不影響 combat（80+）。

### Pattern 4：Cell 掃描情境對話

```papyrus
; 附近有哪些 NPC 可以觸發情境 bark
Actor[] nearbyNPCs = MiscUtil.ScanCellNPCs(akSpeaker, 500.0)
if nearbyNPCs.Length > 2
  ; 觸發「人多」版本的 commentary
endif
```

### Pattern 5：FormList 型 cross-mod 協作（StorageUtil inter-mod）

```papyrus
; 其他 mod 把待處理的 Actor 加入我的 list
StorageUtil.FormListAdd(none, "mymod_processQueue", targetActor)
; 我的 mod 定期掃描並消費
int i = StorageUtil.FormListCount(none, "mymod_processQueue")
while i > 0
  i -= 1
  Actor a = StorageUtil.FormListGet(none, "mymod_processQueue", i) as Actor
  StorageUtil.FormListRemoveAt(none, "mymod_processQueue", i)
endwhile
```

### Pattern 6：prefix 批次清理

```papyrus
; 某 follower 離隊時，清除所有以 "mymod_follower_" 開頭的 key
StorageUtil.ClearAllObjPrefix(followerRef, "mymod_follower_")
```

---

## 四、對 ModForge 的參考價值

| 功能 | 評估 | 說明 |
|---|---|---|
| `StorageUtil` scalar 讀寫 | **可直接生成** | 在 QuestScript / FragmentScript 中做 per-form KV，pattern 極固定 |
| `StorageUtil` 列表操作 | **可直接生成** | ShiftList/PopList/FormListAdd 等 queue/set pattern 可模板化 |
| `JsonUtil` scalar 讀寫 | **可直接生成** | config 讀取 pattern 固定 |
| `JsonUtil` path API | **部分生成**（推斷） | 需要 spec 提供路徑表達式；嵌套結構若由 spec 定義則可生成 |
| `ActorUtil.AddPackageOverride` | **可直接生成** | priority/flags 參數固定，適合 follower action service pattern |
| `ActorUtil.ClearPackageOverride` | **需警告生成** | 易影響其他 mod，需在 spec 中標注風險 |
| `MiscUtil.ScanCellNPCs` | **可直接生成** | 情境觸發 pattern 固定 |
| `MiscUtil.WriteToFile` | **可直接生成** | debug log 或 export 用途 |
| `PapyrusUtil` 陣列操作 | **可直接生成**（推斷） | 大陣列操作；但要注意 Push 類頻繁呼叫有效能問題（原始碼有警告） |
| `PapyrusUtil.StringSplit/Join` | **可直接生成** | CSV-like inline 資料解析 |
| `StorageUtil` prefix 掃描/清除 | **純參考** | 較少見；自動生成需 spec 層明確觸發 |
| `ObjectUtil` animation replace | **不可用** | SSE 版已停用，函數體為空 |
| `StorageUtil.File*` 系列 | **已廢棄** | 都是 JsonUtil 的 proxy，應直接用 JsonUtil |

**對 ModForge 最高槓桿的生成點**：
- `StorageUtil.SetIntValue/GetIntValue` 搭配 Quest Script 的 per-NPC 狀態追蹤
- `JsonUtil.GetIntValue/GetPathFloatValue` 讀取外部 config（讓 mod 設定可在遊戲外調整）
- `ActorUtil.AddPackageOverride` + `RemovePackageOverride` 的 follower 動態行為包
- `MiscUtil.ScanCellNPCs` 驅動的情境 commentary 觸發

> ⚠️ 以上「可生成 / 純參考」是本 survey 推斷，**未查 ModForge src/**，可能有誤判。確認需另做 code pass。
