# CODE_MAP.world — 遊戲內場景匯出（Validate・Example・Tests）

← [CODE_MAP.world](../CODE_MAP.world.md)

## 遊戲內場景匯出 · NPC 角色 macro（續：Validate・Example・Tests）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Validate | `Generator.Validate.Npcs.cs` | package location 槽（sandbox/sleep/travel/escort.destination/useMagic.location/eat）走 `PkgLoc` local fn＝`PkgSlotRef(StripAreaPrefix(...))`，先剝 `area:` 前綴再驗；SingleRef 槽維持裸 `PkgSlotRef`（`area:` 在那留給它報 unresolved）|
| Validate | `Generator.Validate.References.cs` | `RegisterReferenceLabels`（`Validate()` 開頭呼叫，把 label 加進 `Ids` → 其他 CheckRef 認得）＋`ValidateReferences`：ref/label 必填、label 唯一且不可長得像外部 ref、非外部 ref 必須是 placements[] editorId、anchor∈{none,marker,replace}、anchor 不可用在檔內 ref、anchor 需 cell/worldspace、`replace` 需 base、cell/worldspace 互斥、scale>0 |
| **core 前置修** | `Generator.Build.Dialogue.Hello.cs`（2026-08-27 前在 `Generator.Build.Dialogue.cs`）| **外部 speaker Hello 支援**：conditioned-hello 建構 + `AddSpeakerGate` 兩處，in-spec `npcsByEd` 失敗時 fallback `TryResolveRef` 解析 `<plugin>:0xID` → GetIsID FormKey；`MakeHello` 改吃 `FormKey`（非 `INpcGetter`）並在 hello 材質化迴圈解析外部 speaker（否則外部 NPC 的 Hello topic 永不生成）。回歸測全綠 |
| Validate | `Generator.Validate.SceneNpcRoles.cs` | `ValidateNpcRoles`：npc 必填、role 必填且須已知（`KnownRoles`={blacksmith}）；`ValidateRemovals`：removals 須 well-formed 外部 ref；`ValidateOverrides`：同 ref 形狀規則＋與 removals 撞名＝矛盾（警告，build 讓 removal 贏）|
| Example | `examples/scene-export-blacksmith.scene.json` | M0 契約 fixture：farmhouse(0x00084A)+Carlotta(0x013B99 as 替身)+town marker+campfire hazard+blacksmith role |
| Example | **`examples/referrer-chair-anchor.json`** | **referrer 權威範例／實機價值證明**（zip `ModForgeReferrerChair`）：vanilla WhiterunBreezehome（0x0165A8，**要 navmesh ⇒ 必 vanilla cell**）擺**兩張同 base 的 `CommonChair01F`**，`references[]` 只命名其中一張 → Sofia 的 **SitTarget** package slot-16 SingleRef 指該 label。**對照組**＝未命名的誘餌椅（更近、擋路）⇒ 「坐了椅子」≠「坐了命名的那張」。命名的 `0x808` flag=0x400/Persistent、誘餌 `0x807` flag=0/Temporary |
| Example | `examples/scene-references.json` | **(甲) 外部路徑 ＋ `anchor` 逃生門** fixture：Skulvar 的鋤頭(0x0D1991) → temporary 警告；乙路徑那半（椅子 0x0B9C04 → label `"sofia's chair"`）**已從 `sandbox.location` 改為 `sitTarget.target`**——🔴 location 槽只錨定**區域**（`LocationTarget`＋radius，引擎在 radius 內自己挑家具），要鎖定**就是那一個 ref** 必須用 **SingleRef target 槽**（`PackageTargetSpecificReference`）。舊寫法 build 全綠卻證明不了任何事 |
| Tests | `SceneNpcRolesTests.cs` | validate（缺 npc／未知 role）+ expand（host quest/greeting/package/patch、idempotent、未知 role emit 空、無 npcRoles no-op）+ build（Hello INFO GetIsID gate 到外部 NPC FormKey）|
| Tests | `ReferencesTests.cs` | 乙路徑（強制 persistent 0x400、label 解析成 package Location target、不多生記錄、無 references[] 時椅子仍是 temporary＝行為不變）+ validate 9 條 + 甲路徑（無 link cache 仍綁 label；RequiresSkyrim：temporary 警告、`anchor:"marker"` 生 XMarkerHeading、`anchor:"replace"` 生自家複製品且原件 disable+深埋）|
| Tests | `ReferenceSlotKindTests.cs` | **area-anchor 護欄**：label→location 槽＝一行 info（含槽名/radius/AREA/該改用哪個 SingleRef 槽，前綴 `  i `，**Warnings 為空**）；六個 location 槽全會印、六個 SingleRef 槽全不印；**negative**：label 在 SingleRef 槽／location 槽放 vanilla FormID／location 槽放檔內 placement editorId／沒有 `references[]` ⇒ 完全不吵；產物不變（記錄數/槽 payload 照舊）；**反腐化**：reflection 掃 `PackageSpec` 全 string 欄位須在 `PackageRefSlots` 分類過 + 表的 accessor 真的讀到對應欄位；**`area:` opt-out** 三條：前綴讓護欄閉嘴、前綴仍解到與裸 label 同一 FormKey（產物不變）、前綴套在 vanilla FormID 也照解 |
