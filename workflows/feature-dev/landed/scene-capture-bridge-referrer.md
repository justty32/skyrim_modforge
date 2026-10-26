# 已落地 — scene-capture-bridge referrer 原語 `references[]`

← [landed index](README.md)｜採集橋：[scene-capture-bridge.md](scene-capture-bridge.md)（含 🔴 部署事故，先讀）· [editor](scene-capture-bridge-editor.md) · [referrer](scene-capture-bridge-referrer.md) · [flags-deps](scene-capture-bridge-flags-deps.md)

## scene-capture-bridge — referrer 原語 `references[]`：價值主張 IN-GAME PASS · 2026-07-12

`sc ref`／`sc refc`（遊戲內指一個既有物件＋自由 label）＋ `references[]`（消費端）閉環後測的不是「指得到、標得住」（那部分 DLL 端仍待驗，見 [wait_todo](../../../wait_todo/ingame-tests.md)），而是**這個原語本身值不值錢**：`examples/referrer-chair-anchor.json` → `ModForgeReferrerChair.zip`（vanilla `WhiterunBreezehome` override）。

**端到端結論（那條鏈）**：遊戲內指一個既有物件 → 給它 label → spec 的 `sitTarget.target` 用那個 label → build 成 `PackageTargetSpecificReference` → **NPC 真的跟那個特定物件互動**（不是引擎隨便挑一張家具）。使用者實機回報「坐在 808」：Chairwarden Sofia 走過了誘餌椅 `MFRef_DecoyChair`（REFR 尾碼 `807`——更近、一模一樣的 base/角度、就擋在她北上的路上，但**沒有**被 `references[]` 命名），一路走到北牆邊坐上 `MFRef_SofiaChair`（`808`，380 單位外，**被命名的那張**）。

**過程插曲本身就是證據**：第一版把命名的椅子擺在 `y=400`（太貼北牆），她**走過去了但卡在牆與椅子之間坐不下**——這個「卡住」恰恰證明她**非那張不可**：若 label 沒接上，她會坐旁邊那張誘餌，或乾脆站著不動；她選擇卡在死角也要坐那張特定的椅子，正是 specific-reference targeting 生效的訊號。往南挪到 `y=330` 後就順利坐上（commit `287f755`）。

**🔴 slot-kind 的教訓（整輪最值錢的部分）**：label 給你一個名字，但能不能鎖定「就是那一個」，取決於你把它放進**哪個槽**——不是所有 ref 欄位都一樣：
- **SingleRef target 槽**（`sitTarget.target`／`follow.target`／`escort.target`／`activate.target`／`useMagic.target`／`patrol.start`）→ build 成 `PackageTargetSpecificReference`＝**就是那一個 ref**，沒有第二種解讀。
- **location 槽**（`sandbox.location`／`sleep.location`／`travel.place`／`escort.destination`／`eat.location`／`useMagic.location`）→ build 成 `LocationTarget`＋radius＝**一塊區域**，引擎在半徑內隨便挑家具/床/食物——`sandbox.location: "sofia's chair"` 不是「坐那張椅子」，是「在那張椅子附近閒晃」，她完全可能坐到**別張**椅子。
- 最壞的地方在於：走 location 槽**build 綠、dump 乾淨、零警告**——不是 build 失敗，是**靜默地做了作者沒要的事**，只有進遊戲才看得出來。
- **已加護欄**：`src/ModForge.Core/PackageRefSlots.cs`（12 個 SingleRef/Location 槽的分類表，唯一真相來源；反射 anti-rot 測試確保新 template 新增 ref 槽時**必須**分類，漏分類就編不過）＋ `BuildReferences`（`Generator.Build.References.cs`）在 `references[]` 的 label 落進 Location 槽時印一行 INFO 提示。同一輪也結案了一整個「eager 解析、跑在 `BuildPlacements`/`BuildReferences` 之前」的 bug 家族（五處欄位，含共用 `BuildCondition` 的 `param`/`reference`），全部改走 deferred wire（`DeferCondition`/`DeferTarget`/`DeferLocation`），並在 `BuildCondition` 內加 `refsIndexed` 旗標防第六隻靜默重演；`ConditionPlacedRefTests`/`PackageRefSlotsTests` 等回歸測試釘住（C# 1021 測綠）。細節見 [phases](../../plans/scene-capture-bridge/phases.md)「pointer/referrer 原語」與後續五節。
- **`area:<ref>` 前綴 opt-out（2026-07-29 加）**：location 槽寫 `"sandbox.location": "area:sofia's chair"` ＝作者明示「我就是要一塊區域」→ 護欄那行 INFO 不印（意圖已回答）。helper `HasAreaPrefix`/`StripAreaPrefix`（`Generator.Build.Packages.AliasRefs.cs`）；解析端 `MakeLocationSlot`（全 location 槽唯一 chokepoint）先剝前綴再解 label，validate 端 6 個 location 槽走 `PkgLoc` 同步剝。**無前綴＝byte-identical**（舊 spec 完全不受影響）；前綴只在 location 槽合法，落 SingleRef 槽留給它報 unresolved（正確）。護欄的 INFO 文字也改成提示 `area:` 這條靜音路。`ReferenceSlotKindTests` 加三條釘住。

**🧪 對照組實驗設計（可複用的方法論）**：兩個一模一樣的物件（同 base/角度/尺寸/navmesh）、唯一差別是其中一個出現在 `references[]` 裡；沒被命名的那個**擋在必經路上、更近**，故意讓「隨便找張椅子坐」也會得到一個看似合理的結果。判讀不靠肉眼「她坐了張椅子」（那對兩種假說都成立，等於沒驗），而是 console 點選她坐著的那張椅子讀 **RefID 尾碼**（`807`＝坐錯／對照組，`808`＝通過／被命名的那張）當硬證據。這正是「她坐了某張椅子」不能被誤讀成「通過」的原因——變因只有一個（label 有沒有命名），結果用一個無法唬弄的 ID 讀出來。

離線已驗過的底子（build 出的 REFR 帶 0x400＋落 cell 的 Persistent group；package slot 16 = `PackageTargetSpecificReference(0x808)`；928 測綠）——這次是這條鏈第一次在遊戲裡跑完整。

**DLL 端的標記→匯出也同日 IN-GAME 確認**（使用者遊戲內 `sc ref` 標了 5 筆 → `scene-export_Tamriel_x28y27_20260712-2243.json`）：**甲路徑 3 筆**（外部 vanilla ref → `references[].ref` ＝耐久 FormID）、**乙路徑 2 筆**（檔內目標 → `ref` ＝ editorId `MFRef_ref_4_4`／`MFRef_ref_5_5`，且**對應的 placement 真的被蓋上同一個 editorId**——「檔內相依」這個最難的一半成立：dynamic ref 沒有耐久 id，靠 `ObjectRefHandle` identity 在 `AppendPlacements` 當下蓋章，`AppendReferences` 只吐真的有出 placement 的那些）。同一份匯出也驗到 `sc pl py0` 的 `"noHavokSettle": true` 落在 7 筆 placement 上。**剩 open**：改 label 的路徑（大小寫保留／撞名擋下——這次用預設 label）、拒收三類、跨重開讀檔撿回檔內目標，見 [wait_todo](../../../wait_todo/ingame-tests.md)。

**那份匯出 2026-07-13 已被消費成 esp**（`ModForgeGoblets.zip`，待實機）：8 筆 SilverGoblet02 落在 vanilla `WinterholdCollegeExterior`（Tamriel 28,27），逐 byte 驗到 **6 筆 `0x20000000`（DontHavokSettle）＋ `MFRef_ref_5_5` 帶 `0x20000400`（凍結＋persistent）＋ `MFRef_ref_4_4` 只帶 `0x400`**——最後那顆是使用者開 `py0` **之前**擺的，等於匯出自帶**天然對照組**（它該掉、其餘該定住）。乙路徑的兩個檔內 label 確實被蓋章成 persistent（cell persistent=2/temporary=6）。WRLD override 形狀也複驗合規：EDID＋RNAM＋TNAM/UNAM、**無 OFST**（1.4MB 全是 vanilla Tamriel 的 RNAM large-ref 網格，非 bug）。
