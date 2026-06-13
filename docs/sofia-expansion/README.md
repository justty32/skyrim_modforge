# Sofia 擴充專案（docs/sofia-expansion/）

**專案目標**：用 ModForge（JSON spec → 生成 `.esp`）做一個 **Sofia 風格的隨從擴充**——不手改 CK，而是把 Sofia 賴以成立的那些 pattern（在場偵測 banter、GLOB 狀態、小型 controller quest、條件分歧對話、克隆語音）規模化成更多吐槽、更深互動、好感度系統、新演出 scene 與 mini-quest。

解碼總結論：**Sofia 沒用到任何 ModForge 做不出的機制**，它是已落地能力的規模化組合，ModForge 直接夠用。

## 檔案索引

| 檔案 | 說明 |
|------|------|
| [`sofia-personality.md`](sofia-personality.md) | **性格分析 / 寫作 brief**（本專案中心）——Sofia 的原型、幽默機制、說話癖、不安全感、情緒光譜，附大量原文台詞範例 + 「寫新台詞 checklist」。要生成「聽起來像 Sofia」的新對話先讀這份。 |
| [`follower-decode-2026-06-13.md`](follower-decode-2026-06-13.md) | **結構+內容解碼**——`SofiaFollower.esp` 的記錄普查（30 quest / 28 scene / 1135 INFO / 57 GLOB）、五個可複用架構 pattern、quest/scene/formlist 內容索引、對 ModForge 的施工法。 |
| [`expansion-plan-2026-06-13.md`](expansion-plan-2026-06-13.md) | **擴充計畫 + 可行性對照**——F1–F16 十六個具體功能，逐個標 ✅/🟡/🔴 並給 spec 範例與降級方案；含建議實作順序與缺口彙總表。 |
| [`vigilant-support-plan-2026-06-13.md`](vigilant-support-plan-2026-06-13.md) | **Sofia × VIGILANT 支援計劃**——讓 Sofia 對 VIGILANT 進度有「可對談反應」：任務/scene 狀態更新後在她身上**浮現談論選項**（玩家主動找她聊），用 quest-state condition + 對話樹組裝，**刻意不用 scene/自動插話**。本 session 對話樹+跨任務閘的綜合應用，無新功能缺口。 |

外部參考（repo 主 spec 文檔）：[`../SPEC-dialogue-quests.md`](../SPEC-dialogue-quests.md)、[`../SPEC-packages.md`](../SPEC-packages.md)、[`../SPEC-world.md`](../SPEC-world.md)、[`../SPEC-workflow.md`](../SPEC-workflow.md)。

**相關工具（cell 逆向）**：[`../sleeping-giant-inn-reverse-2026-06-13.md`](../sleeping-giant-inn-reverse-2026-06-13.md) — 用新 CLI 子指令 `cellrefs <esp> <0xFORMID>` 把 vanilla interior cell（範例 RiverwoodSleepingGiantInn `0x0133C6`，480 ref）逆向成 `placements[]` JSON（`examples/sleeping_giant_inn.json`）。旋轉 esm radian→ModForge degree、cell-override 寫法、scale 缺欄等坑都記在那。要把 Sofia 演出搬進某個 vanilla 室內、或重佈置一個既有 cell 時用得上。

## 雜七雜八 / misc data

**原始 mod 在磁碟上的位置**
- esp：`~/skyrim_mods/unzip/Sofia Follower v.2/Data/SofiaFollower.esp`（v2.51，**635 KB**，2017-07-06；Mutagen overlay 解碼安全，**只抽 esp 不碰 BSA**）
- BSA（記憶體大、解碼一律不碰）：`SofiaFollower.bsa`（**78 MB**，meshes/scripts/sound/voice）+ `SofiaFollower - Textures.bsa`（**34 MB**）
- Masters：`Skyrim.esm` + `Update.esm`；作者前綴 tag `JJ`。

**用 CLI 探 esp 的正路**（不要自己寫 loader，記憶體鐵律：**永不載 Skyrim.esm 250 MB、不 `.ToList()` 整個 record group**）
- 已有 Release build：`dotnet src/ModForge.Cli/bin/Release/net10.0/ModForge.Cli.dll <cmd> <esp> ...`
- `find <esp> <query> [type]` → editorId/name → FormID（例 `find <esp> JJSofiaDialogue quest` = `0x001D8B`）
- `infodiag <esp> <0xFORMID> [substr]` → dump 一個 quest 所有 topic 的 INFO 回應 + 完整 CTDA（`sofia-personality.md` 的台詞就是這樣抽的，主樞紐 `JJSofiaDialogue` = `0x001D8B`）
- `scenediag <esp> <0xFORMID>` → SCEN 的 host quest / actor alias / phase / action

**可行性總帳（出自 expansion-plan）**：16 功能 → **✅ 能 11**（F1, F3, F5, F6, F7, F8, F9, F10, F11, F12, F13，含 F4 的關鍵字版）／**🟡 需小幅擴充 3**（F2 泛型目標吐槽要 scene actor `findMatching`、F14 戰鬥即時切換、F15 地點評論要 location CTDA）／**🔴 缺口 2**（F4 formlist 細分穿著＝缺 `formLists[]` builder + `GetIsInList`、F16 MCM 即時調參＝缺 SKSE menu builder，建議不做）。

**本專案會吃到的 ModForge 已落地能力**（全部 in-game 確認）
- **scene-banter 在場偵測**：`MFSceneBanterController` + `SceneSpec.autoStart`（triggerDistance / requireLineOfSight / cooldownSeconds / pollSeconds / brawlOnEnd）→ F1/F2/F8/F11 看到 X 就吐槽 / 多隨從互評 / 喝醉鬥毆。
- **多 phase scene 演出**：phase actions `package`（走位）/ `timerSeconds`（停頓 beat）/ `idle`（PlayIdle 動畫，SceneAdapter fragment）+ per-phase headtrack/face → F9 set-piece（唱歌/營火）。
- **GLOB 狀態 + `setGlobal` result fragment**：`globals[]` + 對話/banter/scene `conditions` 用 `GetGlobalValue` 開閘 → F6 好感度系統（這也是身份系統「③ 聲望/行為追蹤」該怎麼做的現成藍圖）。
- **條件分歧對話**：`banter`（一個 IDLE topic 多條 Random INFO）+ CTDA（`GetCurrentTime`/`IsInInterior`/`GetBaseActorValue`/`WornHasKeyword`/`GetStage`/`GetInFaction`）→ F3 海量 idle / F5 技能驅動 / F7 任務後感想 / F4 穿著評論（關鍵字版）。
- **mini-quest**：`stages[]`（startUpStage + 推進）+ `objectives[].targets[]`（QSTA 地圖 marker，2026-06-13 落地）+ `aliases[]`（uniqueActor / forced / xmarker 錨點）→ F10 賞金/帶路/找回走失隨從。
- **隨從機制**：vanilla `CurrentFollowerFaction` / `identities[]` faction + `GetInFaction` gate；`follower_vanilla_spec.json`（`DialogueFollowerScript.SetFollower`）= Sofia「包一層 vanilla 隨從系統」的同套路 → F13。
- **語音管線**：`voiceTemplates[]`（`engine:"f5"` 零樣本克隆 + `referenceWav`/`referenceText`）+ `npcs[].voiceTemplate`；CLI `voicelines`（先 `package` 再對 packaged esp 跑）+ `voicediag`；官方 CK `LipGenerator.exe` 出嘴型 → F12 克隆 Sofia 嗓音。**踩坑**：voice 是 loose asset（`Sound/Voice/<plugin>/<voiceType>/...fuz`），不嵌 esp；ship 流程＝先 package 到最終夾再對該夾 plugin 跑 voicelines，folder 名才對。

**寫作鐵律提醒**（沿用解碼 §踩坑，做 scene/dialogue 時別忘）
- scene actor 必須是同 host quest 的 alias；在場偵測閘用 `autoStart.gateGlobal`，**不要**用 scene-level `conditions`（controller 強制 `Scene.Start()` 繞過 begin-conditions）。
- dense 事件（Hello / ActorDialogue）上的對話必須有 conditions 才不劫持原版。
- state-varying 招呼＝「一個 Hello topic 多條 INFO（順序定優先）」，不是多 topic 競 priority。
