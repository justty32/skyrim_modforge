# Script Event 入口 — 可行性探針 + 設計（RESEARCH SPIKE）

> 日期：2026-06-04 · 狀態：探針完成，**結論：可行，建議列為下一步**（待使用者核可才實作）
> 前置：階段二 storyEvent 管線已實機 PASS（`docs/superpowers/specs/2026-06-04-story-manager-spec-pipeline-design.md`、[[story-manager-kill-recipe]]）
> 範圍：本文件只是 spike 的決策／解碼成果，**不含實作**。不要把任何 throwaway 工具或 .pex 併入 repo。

## 為什麼要 Script Event 入口

今天 ModForge 的 `storyEvent` 只能掛在引擎**自己會發**的事件上（目前只接了 KillActor）。
要量產自訂劇情，需要一個**自訂入口**：ModForge 自己的內容能主動發出帶任意 ref payload 的 story event。
Skyrim SM 內建一個泛用 **Script Event** SMEN，監聽 Papyrus 的 `kmyKeyword.SendStoryEvent(...)`。
這就是「量產內容的最終通用入口」。

唯一障礙：這需要 plugin 內附一個**已編譯的 Papyrus 腳本（.pex）**，而 ModForge 目前完全不做 Papyrus。

---

## 1. 解碼成果：原版 Script Event 記錄結構（純解碼，零 Papyrus）

全部用 Mutagen overlay 從 `Skyrim.esm` 解出。**這是本 spike 最有價值、且今天就能照做的交付物。**

### 1.1 SMEN 根

```
ScriptEvent 事件根  = Skyrim.esm:0x01379A
  Type      = ScriptEvent
  Flags     = 0   Conditions = 0   MaxConcurrentQuests = 0
  Parent    = Skyrim.esm:0x00005B  (SM 總根 "Root" SMBN，與其它事件根並列)
```

（對照：KillActor 根 = `0x013010`，見 [[story-manager-kill-recipe]]。`smtree` 列出 24 個事件根。）

### 1.2 樹形：根 → 中介分支 → 子分支 → quest node → quest

以原版 World Encounter（道路偶遇）系列為解碼樣本。完整鏈：

```
SMBN Root 0x00005B
 └─ SMEN ScriptEvent 0x01379A            (Type=ScriptEvent，無條件 = 泛用監聽)
     └─ SMBN WEQuestNode 0x0896A8        (flags=0；3 條件，含 OR 的 keyword 過濾 → WEStart 0x04A600 或 WERoadStart 0x1027A6)
         └─ SMBN WERoadQuests 0x1027A7   (flags=Random；1 條件 = keyword 過濾 → WERoadStart 0x1027A6)
             └─ SMQN WERoadQuestNode 0x102D65 (PreviousSibling 串其它 quest node)
                 └─ QUEST WERoad12 0x10BF8E 等一票 WE* quest
```

注意：原版在 ScriptEvent 根下還疊了一層**中介分支**做 keyword 分流。
ModForge 不必照抄這層——一層分支即可（沿用 storyEvent 管線「一事件根→一條共用分支→多 quest node 串 PreviousSibling」鐵律）。

### 1.3 Quest 記錄形狀（被 script event 啟動的 quest）

```
Quest.Event = "SCPT"               (RecordType；KillActor 是 "KILL")
Quest.Flags : 清掉 StartGameEnabled（與 KillActor 路徑相同）
Quest.EventConditions : 放 radiant 閘門（時間/等級/GetQuestCompleted…）。
```

別名（alias）從事件 payload 取 ref——**和 KillActor 完全同一機制**（`FindMatchingRefFromEvent`），只是 slot 名不同：

```
ALIAS  FromEvent="SCPT"  EventData = 52 31 00 00  ascii "R1"  → akRef1  (第一個 ObjectReference)
ALIAS  FromEvent="SCPT"  EventData = 4C 31 00 00  ascii "L1"  → akLoc   (Location)
（依 SendStoryEvent 簽章，還有 R2 = 52 32 = akRef2，理論上可用）
```

`SendStoryEvent` 原版簽章（`Keyword.psc`）：
```
Function SendStoryEvent(Location akLoc=None, ObjectReference akRef1=None, ObjectReference akRef2=None, \
                        int aiValue1=0, int aiValue2=0) native
```
→ payload 對應：`akLoc`=L1、`akRef1`=R1、`akRef2`=R2。與已解碼的 alias slot 完全吻合。

### 1.4 ★關鍵：keyword 怎麼綁到監聽器（這是本題核心事實）

keyword **不是**綁在 SMEN 根、也不是 Quest 上有 Keyword 欄位（Quest 沒有 Keyword property）。
而是用一條**條件**放在分支（或 quest node / event condition）上，形狀：

```
Condition (ConditionFloat)
  CompareOperator = EqualTo
  ComparisonValue = 1
  Data = GetEventDataConditionData {
            Function   = GetIsID          // 對 event 的 keyword member 做 GetIsID
            Member     = Keyword
            Record     = <你的 KYWD FormKey>   // 原版範例：WERoadStart 0x1027A6
            RunOnType  = Subject
         }
```

白話＝CK 條件 `GetEventData Keyword GetIsID <Keyword> == 1`。
引擎收到 `K.SendStoryEvent(...)`，把 K 當成 event 的 Keyword member；這條條件就是「只有當發來的 keyword == 我的 keyword 時才跑」。

**Mutagen 0.53.1 原生暴露 `GetEventDataConditionData`**（`Function=GetIsID`, `Member=Keyword`, `Record=FormLink`）。
→ 不需要任何手刻 binary。keyword 本身就是普通 KYWD 記錄，ModForge 早就會建。

> 一句話總結記錄形狀：在 ScriptEvent 根（0x01379A）下掛一條分支，分支條件＝
> `GetEventData/GetIsID Member=Keyword Record=<自建KYWD> ==1`；分支下掛 quest node 串多個
> `Event="SCPT"` 的 quest，alias 用 `FindMatchingRefFromEvent{FromEvent="SCPT", EventData="R1"/"L1"/"R2"}` 取 payload。
> 真正觸發要靠一支 Papyrus 腳本呼叫 `<該KYWD>.SendStoryEvent(loc, ref1, ref2)`。

**這整個 ESP 端（KYWD + SMBN + 條件 + Quest + alias）今天就能用現有 Mutagen API 100% 建出，零 Papyrus。**

---

## 2. Papyrus-on-Linux 可行性裁決

需求：plugin 內要附一支會呼叫 `SendStoryEvent` 的 .pex。ModForge 跑在 Linux（Manjaro），無 Creation Kit GUI。

### 探針實測（本 spike 已驗）

- **官方 `PapyrusCompiler.exe` 是 .NET 程式**（依賴 Antlr3.Runtime.dll / PCompiler.dll / StringTemplate.dll），
  位於 `…/Skyrim Special Edition 1946180/Papyrus Compiler/`。
- 本機**已裝 mono**（`/usr/bin/mono`）。
- 官方完整 Papyrus source set 在 CK 安裝的 `…/1946180/Data/Scripts.zip`（14,301 個 `.psc`，含 `Keyword.psc`、`Location.psc`…）。
  （注意：`…/Skyrim Special Edition/Data/Scripts/Source/` 只有 64 個 loose .psc——不完整，會缺 `LocationRefType` 等型別而編譯失敗；必須用 Scripts.zip 的完整 set。）

**實測結果（已驗，工具已刪）：**
用 mono 跑官方 `PapyrusCompiler.exe`，`-i` 指向解開的完整 source set，成功把一支
trivial dispatcher `.psc` 編成 **615-byte 的 `MFDispatch.pex`**：
- magic header `FA 57 C0 DE`（合法 Papyrus pex magic）。
- 內含字串 `FireEvent` / `akKeyword` / `SendStoryEvent` / `Keyword` / `ObjectReference` / `Location`。
- 編譯器輸出 `Compilation succeeded. 1 succeeded, 0 failed.`

> **裁決：custom .pex generation 在這台 Linux 機器上 FEASIBLE。**
> 最佳路徑＝**mono + 官方 PapyrusCompiler.exe + Scripts.zip 完整 source set**。
> 不需要 Caprica，不需要 wine（compiler 是純 .NET；只有 `PapyrusAssembler.exe` 是 native PE，但編譯流程用不到它）。

### 選項評比

| 選項 | 裁決 |
|---|---|
| **mono + 官方 PapyrusCompiler.exe** | ✅ **推薦**。已實測成功產出合法 .pex。零授權疑慮（官方 toolchain，本機已有）。缺點：依賴 CK 安裝 + Scripts.zip 在場（建置機需有）。 |
| **Caprica（開源跨平台編譯器）** | 可行但**非必要**。本機未裝、未在 PATH；要 build/裝。既然官方 compiler 已用 mono 跑通，Caprica 只是備援（若未來想去掉 CK 依賴 / 進 CI 容器，再評估）。 |
| **預編一支泛用 dispatcher，shipping bytes** | ✅ **推薦做法**（見 §3）。一支泛用腳本編一次、所有 mod 共用 byte。把 Papyrus 編譯從每次 build 移到一次性產物。 |
| **PapyrusUtil 既有 .pex** | ❌ 不適用。它只給 StorageUtil/JsonUtil/MiscUtil 等；**沒有** SendStoryEvent 包裝（SendStoryEvent 是 `Keyword` script 的 native method，不在 PapyrusUtil 裡）。確認過 zip 內容。 |
| **從 C# 直接吐 .pex bytes** | ❌ 不值得。pex 格式雖固定（`FA57C0DE` + string table + 函式表 + bytecode），但手寫 emitter 工程量大、易錯、無 upside——已有現成 compiler 能跑。**不做**。 |

---

## 3. 單一泛用 dispatcher 能否服務所有 generated mod？

**能。Yes。** 一支泛用 dispatcher 編譯一次、所有 mod 共用同一份 .pex bytes。
keyword 與 ref payload 都是**參數**——不必每個 mod 重編。

機制：dispatcher 掛在某個 ModForge 控制的 alias / quest / magic effect 上（依觸發場景），
在需要時呼叫 `傳入的Keyword.SendStoryEvent(loc, ref1, ref2)`。
每個 mod 自己的內容只要拿到自己的 KYWD（普通 ESP 記錄，ModForge 已會建）丟給 dispatcher 即可。
因為 keyword 是執行期參數，**同一份 byte 能服務無限多 mod / 無限多 story event。**

### Dispatcher .psc 草稿（設計用，**不是** shipping 產物）

最小可行版——掛在 quest 上、由別處（如 alias OnInit、別的 fragment、或 RegisterForSingleUpdate）呼叫：

```papyrus
Scriptname MFStoryEventDispatch extends Quest
{ ModForge 泛用 story-event 派發器。編一次、所有產生的 mod 共用同一份 .pex。
  keyword 與 ref 都是參數，故同一 byte 服務任意 mod / 任意事件。}

; 發一個 script story event。akKeyword 決定哪條 SM 分支會接（靠分支的
; GetEventData/GetIsID Keyword==akKeyword 條件）。akRef1→R1、akRef2→R2、akLoc→L1。
Function Fire(Keyword akKeyword, ObjectReference akRef1 = None, \
              ObjectReference akRef2 = None, Location akLoc = None) Global
    akKeyword.SendStoryEvent(akLoc, akRef1, akRef2)
EndFunction

; 同步版：回傳是否真的啟動了某個 quest（除錯/條件鏈用）。
bool Function FireAndWait(Keyword akKeyword, ObjectReference akRef1 = None, \
                          ObjectReference akRef2 = None, Location akLoc = None) Global
    return akKeyword.SendStoryEventAndWait(akLoc, akRef1, akRef2)
EndFunction
```

（探針實際編過的是同義的 instance-method 版，已成功產出合法 .pex；上面改成 `Global` 讓任何
fragment 都能 `MFStoryEventDispatch.Fire(kMyKeyword, …)` 呼叫，更適合「通用入口」。
最終是 Global 還是掛在 alias，等實作階段決定觸發場景時定。）

---

## 4. 提議的 spec 設計（與既有 storyEvent 管線對齊）

維持「意圖導向、藏掉 SMEN/SMBN 機制」的既有風格。

### 4.1 事件表新增 ScriptEvent 定義

`StoryManagerEvents.cs` 加一筆（純資料，沿用既有 `StoryEventDef` 形狀）：

```csharp
["ScriptEvent"] = new StoryEventDef(
    new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x01379A),  // ScriptEvent 根
    new RecordType("SCPT"),
    new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase) {
        ["ref1"] = new byte[]{ 0x52,0x31,0x00,0x00 },  // "R1" akRef1
        ["ref2"] = new byte[]{ 0x52,0x32,0x00,0x00 },  // "R2" akRef2
        ["loc"]  = new byte[]{ 0x4C,0x31,0x00,0x00 },  // "L1" akLoc
    }),
```

### 4.2 spec schema：擴充 storyEvent，加 `keyword`

對使用者只多一個概念：「我要自己發這個事件，且綁一個 keyword」。

```jsonc
"quests": [{
  "editorId": "MFSE_BanditRaid",
  "storyEvent": {                       // 沿用既有塊；event 改成 "ScriptEvent"
    "event": "ScriptEvent",
    "keyword": "MFSE_BanditRaidKW",     // ★新欄位：自建 KYWD 的 editorId（ModForge 自動建/查）
    "conditions": []                    // 可選 radiant 閘門
  },
  "aliases": [
    { "name": "Target", "fill": "fromEvent:ref1" },   // R1
    { "name": "Place",  "fill": "fromEvent:loc"  }     // L1
  ]
}]
```

build 端（`Generator.Build.StoryManager.cs` pass 2）對 `event=="ScriptEvent"` 多做兩件事：
1. 在共用分支上加 keyword 過濾條件＝`GetEventDataConditionData{ GetIsID, Member=Keyword, Record=<該KYWD> } == 1`
   （Mutagen 原生型別，見 §1.4）。同一 keyword 共用一條分支；不同 keyword → 不同分支（互斥分流，貼合原版）。
2. 確保 `MFSE_BanditRaidKW` 這個 KYWD 記錄存在（spec 另宣告，或 build 自動建）。

其餘（清 StartGameEnabled、alias FindMatchingRefFromEvent、PreviousSibling 串接）與 KillActor 路徑**完全共用既有程式碼**。

### 4.3 dispatcher / .pex 怎麼進 package

- dispatcher `.pex` 是**一次性產物**：離線（或建置機上有 CK 時）用 mono+PapyrusCompiler 編一次，
  把 byte 當靜態 asset 收進 repo（如 `assets/papyrus/MFStoryEventDispatch.pex`）。
- package 步驟把它放到 `Data/Scripts/MFStoryEventDispatch.pex`（與既有 .seq / navmesh asset 打包路徑同一機制）。
- 觸發點（誰呼叫 `Fire(...)`）是**實作階段**要決定的設計問題：可能掛在某個 always-on quest 的 alias script、
  或由其他 ModForge 已支援的機制（dialogue fragment、magic effect…）呼叫。最小驗證用一個手動觸發即可。

---

## 5. 今天可建 vs. 卡在 Papyrus

| 部分 | 狀態 |
|---|---|
| ScriptEvent 根、SMBN 分支、keyword 過濾條件、Quest(`Event="SCPT"`)、alias(R1/L1/R2) | ✅ **今天就能建**（純 Mutagen，記錄形狀已完整解碼，§1） |
| KYWD 記錄 | ✅ 今天就能建（ModForge 已會建 keyword） |
| dispatcher `.pex`（呼叫 SendStoryEvent） | ⚠️ 需編譯，但**已驗可行**：mono + 官方 compiler + Scripts.zip（§2）。一次性產物。 |
| .pex 進 package | ✅ 既有 asset 打包機制可用 |
| 觸發點接線（誰呼叫 dispatcher） | 🔜 實作階段設計（非阻塞；最小版手動觸發即可） |

**唯一「新」依賴**＝建置機要有 mono + CK 的 Scripts.zip 才能（一次性）產出 dispatcher .pex。
產出後那份 byte 就固定下來，後續所有 mod build 不再碰 Papyrus。

---

## 6. 建議

**值得，建議列為下一步。** 理由：
- 記錄形狀已 100% 解碼，ESP 端與既有 storyEvent 管線高度共用（只多一條 keyword 條件 + 一個事件表項）。
- Papyrus 障礙已實測破解（mono 編譯成功產出合法 pex），且是**一次性**成本，不污染每次 build。
- 這是「量產自訂劇情」的最終通用入口，解鎖價值最高。

**最小、能實機端到端驗證的第一步：**
1. 離線用 mono+compiler 編出 `MFStoryEventDispatch.pex`（Global `Fire`），存成 repo asset。
2. 手寫一個最小 spec：一個 KYWD + 一個 `Event="ScriptEvent"` 的模板 quest（alias 取 R1），
   build 出帶 keyword 條件分支的 ESP，package 時帶上那份 .pex。
3. 實機：用主控台 / 一個 always-on alias 的 OnInit 呼叫 `MFStoryEventDispatch.Fire(kMyKW, akSomeRef)`，
   驗證 SM 啟動模板 quest 且 alias 填上傳入的 ref。
   （成功＝證明「自訂入口 + 任意 payload」整條鏈通；之後就是量產接線。）

> 與既有實機驗證鐵律一致：先結構驗證（build 綠 + dump 比對原版 WE 形狀），再 package→zip→MO2 實機。
