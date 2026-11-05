# CODE_MAP.dialogue-quests — 對話樹・Banter

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Dialogue 對話樹
→ **說明文件**：[SPEC-dialogue.md § dialogue](../../../../docs/spec/SPEC-dialogue.md#dialogue)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `DialogueSpec`, `DialogueInfoSpec`；**`hello`（bool）= 這行是 NPC 自動招呼(Misc/Hello)而非玩家選單 topic** |
| Build P1 | `Generator.Build.Dialogue.Hello.cs` | **招呼專屬檔**（2026-08-27 由 `Generator.Build.Dialogue.cs` 拆出）。`BuildConditionedHello` 只建 INFO 並暫存於 (speaker\|quest)，`BuildNpcHellos` 才真正發 Hello topic（含 `MakeHello` local ＋ 只有 `greeting` 沒有 `dialogue[]` 的 NPC 的自建 host quest）。**`hello:true` 招呼：同一 (NPC,quest) 的所有招呼合進 **一個** Hello topic 的多條 INFO(conditioned 在前、plain `greeting` 墊底)——**鐵律:招呼靠 INFO 順序選取(第一個條件符合的勝出),不是多個獨立 Hello topic 靠 priority 競爭(那樣引擎只取一個 topic、其他無視);vanilla 237/297 Hello topic 多 INFO**(in-game 2026-06-07 確認身份招呼)|
| Build P1 | `Generator.Build.Dialogue.cs` `DialogueInfoFlags` | DialogueSpec → INFO (ENAM) flags 合併（`goodbye`/`sayOnce`/`walkAway`/`random`/`invisibleContinue`/`forceSubtitle`）；hello 與 player-topic 兩個建立點共用 |
| Build P1 | `Generator.Build.Dialogue.cs` | **M組 `variants[]` INFO 批次**：一個 dialogue entry 在**同一 topic** 下掛多條 sibling INFO（各帶 `Random` flag→引擎在條件符合的 sibling 隨機選；共用 `AddSpeakerGate` local）；`DialogueVariantId(ed,i)`=`<ed>_v<i>`（record EditorID + `dialogResponsesByEd` key，pass1/pass2 一致）；parent INFO 僅在 `responses` 非空或無 variants 時建（純批次 header→不發 parent）；variant 自有 emotion/emotionValue 缺省繼承 parent |
| Build P1 | `Generator.Build.Dialogue.cs` `BuildDialogue` | **拆檔後只是編排器**（2026-08-27）：逐筆分派到 `BuildConditionedHello`／`BuildPlayerTopic`，再依序 `BuildDialogueViews`（DLVW）、`BuildNpcHellos`。**呼叫順序即 FormID 順序，不可重排**（golden-hash 197/197 把關）|
| Build P1+P2 | `Generator.Build.Dialogue.cs` | **對話樹**：`topLevel:false` → branch 非 TopLevel（sub-topic，只在被 link 時出現）；topic 註冊進 `dialogTopicsByEd`（topic/INFO 同 editorId，formKeyByEd 會撞，需專屬 map）。pass-2 `WireDialogueLinks`：`linkTo`→INFO `LinkTo`(ENAM，指 target dialogue 的 **topic** 或 vanilla ref)、`previousDialog`→INFO `PreviousDialog`(PNAM，指 target **INFO**)|
| Build P2 | `Generator.Build.Conditions.Wire.cs` `WireDialogueConditions` | INFO CTDA 條件接線；**M組 `useConditionTemplates`：把 `ModSpec.ConditionTemplates`（`ConditionTemplateSpec` name+conditions）依名展開到 INFO（inline conditions 之後、同 BuildCondition 路徑、alias-aware）**；**M組 `variants`：`ApplyShared` local（inline conditions + templates + identity）套到 parent INFO **與每條 variant INFO**，variant 再接自有 `conditions`（用 `DialogueVariantId` 查 INFO）；setStage 自動閘僅 parent** |
| Validate | `Generator.Validate.Quests.cs` | speaker NPC ref、quest ref、condition function；**conditionTemplate name 唯一/非空 + 每條 CheckCondition；dialogue useConditionTemplates 引用須存在** |
| Diag | `Diagnostics.Dialogue.cs` | topic / INFO / condition / result-script dump |

> **Voice 註**：dialogue / banter / scene 的 INFO 現在全部補上 EditorID（語音管線 `voicelines` 的 CK 檔名需要 quest/topic EditorID，INFO EditorID 供追蹤）——管線本體見 [CODE_MAP.infra.md](../CODE_MAP.infra.md)「語音克隆」段。

---

## Banter 隨機台詞
→ **說明文件**：[SPEC-dialogue.md § banter](../../../../docs/spec/SPEC-dialogue.md#banter--proactive-unprompted-npc-lines)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Dialogue.cs` | `BanterSpec`（含 emotion / chance）|
| Build P1 | `Generator.Build.Banter.cs` | 建 ambient banter topic + random INFO |
| Build P2 | `Generator.Build.Conditions.cs` | banter condition 接線 |
