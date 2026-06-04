# Script Event 入口 實作計畫

> 設計來源：`docs/superpowers/specs/2026-06-04-script-event-entry-spike.md`（記錄形狀已 100% 解碼）。
> 目標：讓 ModForge 內容能**自己發**帶任意 ref payload 的 story event（自訂入口），SM 接到後啟動模板 quest。
> 與既有 storyEvent 管線高度共用；只多「一個事件表項 + keyword 過濾分支 + 一份通用 dispatcher .pex」。

**現況盤點（已確認存在，直接複用）**
- `KeywordSpec`/`BuildKeywords()`（pass 1）— KYWD 已會建。
- `Papyrus.CompileBest`（native `~/tools/papyrus-compiler` 已在）+ Package 已會編譯/打包 .pex。
- 既有 script attach（VMAD、quest fragment、typed property）。
- `GetEventDataConditionData{ Function=GetIsID, Member=Keyword, Record=IFormLink, RunOnType=Subject }` Mutagen 0.53.1 原生。
- SM 分支建構鐵律：一事件根→共用分支→quest node 串 PreviousSibling（[[story-manager-kill-recipe]]）。

## Task 1 — 事件表加 ScriptEvent（純資料）
`StoryManagerEvents.cs`：`Defs["ScriptEvent"]` root `0x01379A`、code `SCPT`、slots ref1=R1、ref2=R2、loc=L1。
測試：`TryGet("ScriptEvent")` 回傳正確 code/slots。

## Task 2 — spec 加 keyword 欄位
`Spec.StoryManager.cs`：`QuestStoryEventSpec.Keyword`(string，KYWD editorId)。

## Task 3 — build：keyword 過濾分支（per root+keyword）
`Generator.Build.StoryManager.cs`：分支 key 從 `root` 改成 `root|keyword`（非 ScriptEvent keyword 空→等同原行為）。
ScriptEvent 首次建分支時加條件：`ConditionFloat{ CompareOperator=EqualTo, ComparisonValue=1, Data=GetEventDataConditionData{ Function=GetIsID, Member=Keyword, Record=<kwFormKey>, RunOnType=Subject } }`。
分支 EditorID 含 keyword 以區分。kwFormKey 由 `formKeyByEd[keyword]`（pass 2，KYWD 已在表）。
測試：ScriptEvent quest → 分支帶 1 條件、Data 是 GetEventDataConditionData、Record=該 KYWD、Member=Keyword、Function=GetIsID；同 keyword 共用分支、不同 keyword 不同分支。

## Task 4 — validate：ScriptEvent 必須有已宣告的 keyword
`Generator.Validate.StoryManager.cs`：event==ScriptEvent → keyword 非空且存在於 spec.Keywords，否則 error。非 ScriptEvent 帶 keyword → 忽略（不報錯）。
測試：缺 keyword/未宣告 → Problems；正常 → 無。

## Task 5 — 通用 dispatcher .pex（一次性產物，進 repo）
`assets/papyrus/MFStoryEventDispatch.psc`（Global `Fire(Keyword, ObjectReference akRef1, akRef2, Location)` → `akKeyword.SendStoryEvent(akLoc, akRef1, akRef2)`）。
用 `Papyrus.CompileBest` 編一次 → `assets/papyrus/MFStoryEventDispatch.pex`，兩者都 commit。

## Task 6 — package：有 ScriptEvent quest 時帶上 dispatcher .pex
`Package.cs`：spec 有任一 `storyEvent.event=="ScriptEvent"` 的 quest → 把 `assets/papyrus/MFStoryEventDispatch.pex` 複製進 `Scripts/`。

## Task 7 — 範例 + 實機測試包
`examples/story-manager-scriptevent.json`：一個 KYWD + 一個 `Event=ScriptEvent` 模板 quest（alias ref1）+ 一個 start-game 觸發 quest 掛測試腳本 `MFSE_TestTrigger`（OnInit 呼叫 `MFStoryEventDispatch.Fire(kw, Game.GetPlayer())`）。
package→zip→`~/skyrim_mods`。實機：載入→`sqv <模板quest>` 應 running、alias ref1=玩家(0x14)。
