# API：ActorUtil / MiscUtil / 陣列操作

← [papyrusutil](papyrusutil.md)

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

