# 遊戲內場景匯出 — referrer 形狀：頂層 `references[]`（定案形狀與槽位語意）

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

### ✅ 拍板（2026-07-12）：referrer 的形狀＝頂層 `references[]`（ModForge 消費端已落地）

**問題**（backlog「📌 pointer/referrer 原語」）：marker 標的是**空座標**（「這裡放東西」）；referrer 標的是**一個已存在 ref 的身份**（vanilla 椅子，或玩家自己 `sc pl` 擺的椅子）＋自由標籤，好讓下游 spec 拿標籤去引用它（例：給 Sofia 的 sandbox package 一個「sofia 的椅子」錨點）。

**定案形狀**（`Spec.References.cs` / `Generator.Build.References.cs` / `Generator.Validate.References.cs`，2026-07-12 C# 端全鏈落地）：

```json
"references": [
  { "ref": "MFRef_SofiaChair",          // (乙) 檔內 placements[] 的 editorId
    "label": "sofia's chair",           // 必填、全 spec 唯一
    "base": "Skyrim.esm:0x0B9C04",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": { "x": 18700, "y": -12700, "z": -4590 },
    "rotation": { "x": 0, "y": 0, "z": 180 },
    "note": "she should always come back to this one" },

  { "ref": "Skyrim.esm:0x0D1991",       // (甲) 外部既有 vanilla ref
    "label": "skulvar's hoe",
    "base": "Skyrim.esm:0x02F2F4",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": { "x": 19265.9, "y": -12816.5, "z": -4603.4 },
    "anchor": "replace" }               // persistent 逃生門（見下）
]
```

**核心語意＝`label` 是一個「可解析的名字」**：build 把它註冊進 pass-2 的 editorId→FormKey 表（`formKeyByEd`），所以 **spec 裡任何一個 ref 欄位都能直接寫這個 label** —— package 的 `sitTarget.target` / `travel.place`、quest alias 的 `forced:`、`linkedRefs.target`、`enableParent.ref`、objective target、script Form property。消費站點**零改動**。ModForge **不生成**該 ref（唯一例外＝下面的 anchor）。三兄弟並列：`removals[]` 擦掉既有、`overrides[]` 移動既有、`references[]` **命名**既有。

> 🔴 **契約層必須講明白：label 給你一個名字，但「能不能鎖定就是那一個 ref」取決於你把 label 放進哪個槽。**（2026-07-12，做實機 demo 時發現）
>
> **完整槽位表**（程式碼真相＝`src/ModForge.Core/PackageRefSlots.cs`，附反腐化測試；下表是它的鏡像）：
>
> | 槽的種類 | 全部的槽 | build 出來是 | 語意 |
> |---|---|---|---|
> | **SingleRef target** | `patrol.start`、`follow.target`、`escort.target`、`sitTarget.target`、`activate.target`、`useMagic.target` | `PackageTargetSpecificReference(FormKey)` | **就是那一個 ref**，引擎不會挑別的 |
> | **location** | `sandbox.location`、`sleep.location`、`travel.place`、`escort.destination`、`eat.location`、`useMagic.location` | `LocationTarget(FormKey)` ＋ radius | 錨定**那個 ref 所在的區域**；引擎在 radius 內**自己挑**家具/床/食物 |
>
> 所以 `sandbox.location: "sofia's chair"` **不等於**「坐那張椅子」，而是「在那張椅子附近晃」——她很可能坐**旁邊另一張**椅子，而且**不會有任何警告或錯誤**（build 綠、dump 漂亮、實機是隨機的）。這是最壞的一種缺陷：看起來對。**「她必須用那個物件」一律走 SingleRef 槽。** referrer 的**價值證明**（`examples/referrer-chair-anchor.json`，含對照組誘餌椅）就是靠 `sitTarget` 才立得住。
>
> **✅ 護欄（2026-07-12）**：`references[]` 的 label 掉進 location 槽時，build 印一行 **info**（`  i …`，走 `BuildResult.Notes`，**不是 warning**——「在那附近晃」是合法意圖，只有作者知道要哪個），內容含槽名、radius、以及「要鎖定就改用這些 SingleRef 槽」。**只在 label 來自 `references[]` 時印**：location 槽裡放 vanilla FormID 或檔內 placement editorId＝一般的區域用法，不吵。零 esp 位元變動（`examples/referrer-chair-anchor.json` / `scene-references.json` 產出 md5 不變）。程式：`Generator.Build.References.cs` `NoteLabelsUsedAsAreaAnchors`；測試：`ReferenceSlotKindTests.cs`。
