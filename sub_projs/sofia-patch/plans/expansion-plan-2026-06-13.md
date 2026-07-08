# Sofia 擴充計畫（2026-06-13）— 對照 ModForge 可實現性

← 參考：[`follower-decode-2026-06-13.md`](../reference/follower-decode-2026-06-13.md)（Sofia 架構解碼）、[`sofia-personality.md`](../reference/sofia-personality.md)（性格/寫作 brief）、[`../SPEC-dialogue-quests.md`](../../../docs/spec/SPEC-dialogue-quests.md)、[`../SPEC-packages.md`](../../../docs/spec/SPEC-packages.md)、[`../SPEC-world.md`](../../../docs/spec/SPEC-world.md)、[`../SPEC-workflow.md`](../../../docs/spec/SPEC-workflow.md)

## 1. 目標

把「擴充 Sofia」當成一份 **ModForge spec → 生成的 `.esp` plugin** 來做，而不是手改 CK。擴充的內容就是 Sofia 賴以成立的那些 pattern 的**規模化**：更多在場偵測 banter（看到 NPC/地點就吐槽）、更深的互動（送禮/喝醉/吟唱式 set-piece scene）、好感度/聲望狀態追蹤、任務後感想、多隨從互評、玩家裝備/技能驅動的 idle 閒聊、客製語音。Sofia 解碼結論已確認：**它沒用到任何 ModForge 做不出的機制**——它是「scene-banter 在場偵測 + GLOB 狀態 + 小型 controller quest + 對話 condition」的組合放大。因此本計畫多數功能落在 ✅，少數落在 🟡（需小幅擴充 CTDA 函數白名單或便利欄位），只有「MCM 即時調參」「真‧formlist 分類表」屬 🔴。所有可實現性判斷都對應一個我實際在 spec doc / builder 程式碼裏看到的功能。

> 鐵律沿用（解碼備忘 §踩坑）：scene actor 必須是同 quest 的 alias；在場偵測閘用 `autoStart.gateGlobal` **不要**用 scene-level `conditions`（controller 強制 `Scene.Start()` 繞過 begin-conditions）；dense 事件（Hello/ActorDialogue）上的對話必須有 conditions 才不劫持原版；state-varying 招呼是「一個 Hello topic 多條 INFO（順序定優先）」不是多 topic 競 priority。

---

## 2. 優先功能清單

可實現性圖例：✅ 能（列出用到的精確 spec 功能）／🟡 需小幅擴充（點名要加的小東西）／🔴 缺口（描述要做的新功能）。

### F1 — 看到具名 NPC 就吐槽（仿 `JJSofia{Nazeem,…}Comment`）
(a) 玩家帶 Sofia 靠近 Nazeem/Carlotta 等具名 NPC，Sofia 自動講一段針對對方的台詞（2-phase 對白可讓對方回嘴）。
(b) **✅ 能**。`SceneSpec` + `autoStart`（在場偵測：`triggerDistance` / `requireLineOfSight` / `cooldownSeconds` / `pollSeconds`），actor 用 `UniqueActor`-bound 的兩個 alias（Sofia + 目標 NPC），host quest `startGameEnabled:true`。複用的 `MFSceneBanterController` 已落地。`brawlOnEnd` 還能做「吵到打起來」。每多一個目標 NPC = 複製一份這個 block（Sofia 就是這樣 N 份）。
(c)
```jsonc
{ "editorId": "MFSofiaX_NazeemComment", "questEditorId": "MFSofiaX_CommentHost",
  "autoStart": { "triggerDistance": 1024, "cooldownSeconds": 600, "pollSeconds": 5 },
  "actors": [ {"aliasId":0,"npc":"MFSofia","name":"Sofia"},
              {"aliasId":1,"npc":"Skyrim.esm:0x00019DFD","name":"Nazeem"} ],   // Nazeem ref
  "phases": [ {"speaker":0,"emotion":"Disgust","lines":["Oh look. The Cloud District's finest."]},
              {"speaker":1,"emotion":"Anger","lines":["Do you get to the Cloud District very often?"]} ] }
```

### F2 — 對泛型角色吐槽（仿 `JJSofiaGuardComment`，16 變體）
(a) 對「任一衛兵 / 任一商人」這種**沒有具名**的目標吐槽，多條變體隨機挑。
(b) **🟡 需小幅擴充**。在場偵測 controller 與 scene 鏈都 ✅，但 `autoStart` actor 目前只支援 `UniqueActor`-bound 具名 NPC（doc 明寫 *Out of scope: dynamic "scan current teammates" fill*）。要對「最近的一個衛兵」吐槽，需把 SM 已有的 **`findMatching:closest`（`HasKeyword` 過濾 loaded-area ref）** alias fill 接進 `autoStart` 的 actor 槽。小幅擴充 = 讓 scene actor 接受 `findMatching` fill。替代降級方案（✅）：用 `banter`（IDLE topic、多條 Random INFO）配 `WornHasKeyword`/faction condition，但那是 Sofia 自己講、無對方回嘴。
(c)
```jsonc
"actors": [ {"aliasId":0,"npc":"MFSofia"},
  {"aliasId":1,"fill":"findMatching:closest",                      // 🟡 新：scene actor 用 findMatching
   "conditions":[{"function":"GetInFaction","param":"Skyrim.esm:0x00028848","comparison":"==","value":1}]} ] // 衛兵 faction
```

### F3 — 海量條件式 idle 閒聊（仿 `JJSofiaIdleDialogue`，1 topic / 247 INFO）
(a) Sofia 旅途中自言自語的旁白，依**時間/室內外/玩家受傷**等分歧出不同台詞。
(b) **✅ 能**。`banter` entry（同一 speaker+quest 收斂成一個 IDLE ambient topic，多條 Random INFO，引擎隨機挑 conditions 通過者）+ `conditions`（`GetCurrentTime` 夜晚、`IsInInterior`、`GetActorValuePercent` 受傷、`GetInFaction CurrentFollowerFaction==1` 隨從限定）。觸發要 speaker 帶 `AllowIdleChatter` interrupt flag 的 package。這正是 Sofia 247 條 idle 的生成法。
(c)
```jsonc
"banter": [
  { "questEditorId":"MFSofiaX_IdleHost", "speakerNpcEditorId":"MFSofia",
    "responses":["The cold gets into your bones out here."],
    "conditions":[ {"function":"GetCurrentTime","comparison":">=","value":20},
                   {"function":"IsInInterior","comparison":"==","value":0} ] } ]
```

### F4 — 玩家穿著驅動評論（仿 `JJPlayerOutfitType` + outfit formlist）
(a) 玩家穿重甲/暴露裝/法師袍/犯罪裝時，Sofia 評論你的穿著。
(b) **✅ 能（關鍵字版）／🔴 缺口（formlist 分類版）**。ModForge CTDA 白名單**已含 `WornHasKeyword`**（builder `WornHasKeywordConditionData` 確認），所以「穿了帶 `ArmorHeavy`/`ClothingBody` 關鍵字的東西」這種 condition 可直接生 — 涵蓋大宗情境。但 Sofia 的細分（`JJBadOutfits`(13)/`JJPlayerRevealingOutfits`(6) 等具名 armor 清單）靠的是 **`GetIsInList`（讀 FormList 成員）**，而 builder 的 condition 白名單**沒有 `GetIsInList`、也沒有 FORM-List(FLST) builder**。要逐件指定「這 13 件算醜」就需 🔴：① 新增 `formLists[]`（FLST record builder）② condition 白名單加 `GetIsInList`。多數擴充用 `WornHasKeyword` 已足，formlist 是 nice-to-have。
(c)
```jsonc
// ✅ 立即可做（關鍵字）：
{ "function":"WornHasKeyword","param":"Skyrim.esm:0x06BBD2","comparison":"==","value":1 }  // 重甲關鍵字
// 🔴 需新增 formLists[] + GetIsInList 才能做的細分：
{ "function":"GetIsInList","param":"MFSofiaX_RevealingOutfits","comparison":"==","value":1 }
```

### F5 — 玩家技能驅動對話（仿鏡像 18 技能 GLOB）
(a) Sofia 看你練哪一系（破壞法術高 → 評論你愛放火）給不同台詞。
(b) **✅ 能**。CTDA 白名單含 **`GetBaseActorValue`**（doc 明寫「base (un-buffed) AV — perks gate on this e.g. Destruction>=30」），condition `runOn:Reference` + `reference: player`、`actorValue:"Destruction"` 即可直接讀玩家技能，**不需** Sofia 的 18 個鏡像 GLOB（那是 Papyrus-only 限制下的 workaround；ModForge 可直接 CTDA）。
(c)
```jsonc
{ "function":"GetBaseActorValue","actorValue":"Destruction","comparison":">=","value":50,
  "runOn":"Reference","reference":"Skyrim.esm:0x000014" }
```

### F6 — 好感度／聲望 GLOB 系統（仿 `JJSofiaRelationship`）
(a) 一個計數器隨玩家行為加減，台詞/互動依好感度分歧；低好感冷淡、高好感親暱。
(b) **✅ 能**。`globals[]`（GLOB builder：`MFSofiaX_Affinity` short）記錄；對話/banter/scene `conditions` 用 **`GetGlobalValue`** 開閘。加分由 **dialogue result fragment** 改 GLOB：spec 已有 `setGlobal` result fragment（見 git log「setGlobal result fragment to mutate a GlobalVariable」）讓選某對話 +1，**純 record、無 per-mod script**。降階里程碑也能用 `gateGlobal`（scene 重播 token）。
(c)
```jsonc
"globals": [ {"editorId":"MFSofiaX_Affinity","type":"short","value":0} ],
"dialogue": [ {"questEditorId":"MFSofiaX_Talk","speakerNpcEditorId":"MFSofia",
  "prompt":"You did well back there.","responses":["...thanks. I mean it."],
  "setGlobal":{"global":"MFSofiaX_Affinity","value":1,"mode":"add"} } ],   // +1 好感
"banter": [ {"questEditorId":"MFSofiaX_Idle","speakerNpcEditorId":"MFSofia",
  "responses":["I'm glad it's you I'm travelling with."],
  "conditions":[{"function":"GetGlobalValue","param":"MFSofiaX_Affinity","comparison":">=","value":5}] } ]
```

### F7 — 任務後感想（仿 condition on `GetStage`）
(a) 玩家做完某 vanilla / 自訂 quest 後，Sofia 講一段對該事件的感想。
(b) **✅ 能**。`banter` 或 `dialogue` `conditions` 用 **`GetStage`**（`param` 指向那個 quest）。任何 vanilla quest 的 FormID 都能當 `param`。
(c)
```jsonc
{ "questEditorId":"MFSofiaX_Idle","speakerNpcEditorId":"MFSofia",
  "responses":["You really put down that dragon at the Western Watchtower. I'm impressed."],
  "conditions":[{"function":"GetStage","param":"Skyrim.esm:0x0004E50C","comparison":">=","value":160}] }  // MQ104
```

### F8 — 多隨從互評（Sofia ↔ 另一隨從）
(a) 同時帶 Sofia + 另一個自訂/vanilla 隨從時，他倆彼此點名吐槽。
(b) **✅ 能**。一個 `autoStart` scene、兩個 actor 都是同 host quest 的 `UniqueActor` alias（Sofia + 對方隨從），phase 互相點名。隨從都跟著玩家 → 在場偵測天然成立。
(c)
```jsonc
{ "editorId":"MFSofiaX_LydiaBanter","questEditorId":"MFSofiaX_CommentHost",
  "autoStart": {"triggerDistance":1024,"cooldownSeconds":900},
  "actors":[ {"aliasId":0,"npc":"MFSofia"},{"aliasId":1,"npc":"Skyrim.esm:0x000A2C8E"} ],   // Lydia
  "phases":[ {"speaker":0,"lines":["So you're a housecarl. Bet that pays well."]},
             {"speaker":1,"lines":["It is an honour to serve. Not that you'd understand."]} ] }
```

### F9 — 新 set-piece 演出 scene（仿 `BardSongs` / `WeddingScene` 多-phase）
(a) Sofia 唱一首歌 / 跳一段 / 在營火邊演一段，配走位+停頓+動畫。
(b) **✅ 能**。`SceneSpec` 多 phase + `actions[]`：`package`（Travel 走位到 marker）/ `timerSeconds`（停頓 beat）/ `idle`（PlayIdle 動畫，`SceneAdapter` phase fragment，2026-06-07 in-game 確認）。phase 可空（beat phase）。`headtrackActor`/`headtrackPlayer`/`faceTarget` per-phase 凝視控制。
(c)
```jsonc
{ "editorId":"MFSofiaX_Campfire","questEditorId":"MFSofiaX_PerformHost",
  "actors":[ {"aliasId":0,"npc":"MFSofia"} ],
  "phases":[ {}, {"speaker":0,"lines":["A song, then. For the road."]},
             {"speaker":0,"lines":["...The Dragonborn comes."]} ],
  "actions":[ {"actor":0,"startPhase":0,"package":"MFSofiaX_WalkToFire","timerSeconds":3},
              {"actor":0,"startPhase":1,"idle":"Skyrim.esm:0x0F11EE","timerSeconds":4} ] }  // 動作 idle
```

### F10 — 真‧journal mini-quest（仿 `JJSofiaWeddingCeremony` / `TrackingMarker`）
(a) Sofia 給一條支線（賞金/帶路/找回走失隨從），含日誌 stage、目標、地圖 marker。
(b) **✅ 能**。`stages[]`（`startUpStage` + 推進、`completeQuest`）+ `objectives[]`（`showStage`/`completeStage`）+ **`objectives[].targets[]`（QSTA 地圖 marker，2026-06-13 剛做）**；用 `aliases[]`（`uniqueActor` / `forced` / `xmarker` 錨點 + `forced` alias）填 marker 目標。`package` 自動生 stage/objective fragment。「找回走失隨從」= 一個 `xmarker` placement + `forced` alias + objective target 指它。
(c)
```jsonc
"quests":[{ "editorId":"MFSofiaX_Bounty","startGameEnabled":false,
  "stages":[ {"index":10,"logEntry":"Sofia wants me to clear a bandit camp.","startUpStage":true},
             {"index":20,"logEntry":"Camp cleared.","completeQuest":true} ],
  "objectives":[{ "index":10,"text":"Clear the bandit camp","showStage":10,"completeStage":20,
                  "targets":[{"alias":"CampMarker"}] }],
  "aliases":[{ "name":"CampMarker","fill":"forced:MFSofiaX_CampXMarker" }] }]
```

### F11 — 喝醉/送禮等狀態互動模組（仿 `JJSofiaDrunk` / `JJSofiaGiveGift`）
(a) 給 Sofia 送禮（她回謝、好感+1）；或一段喝醉對飲 → 鬥毆 scene。
(b) **✅ 能**。送禮 = dialogue 選項 + result fragment（`rewardItem` 反向：用 `setGlobal` 加好感 + 既有 fragment；給禮物進她背包則用 NPC `Items`/script）。喝醉鬥毆 = `autoStart` scene + `brawlOnEnd:true`（actor 標 `essential` 非致命）。狀態用 FACT/GLOB gate（仿 `SofiaBrawlFaction`，用 `identities[]` 的 faction 或裸 GLOB）。
(c)
```jsonc
{ "editorId":"MFSofiaX_DrinkBrawl","questEditorId":"MFSofiaX_DrunkHost",
  "autoStart":{"triggerDistance":512,"cooldownSeconds":3600,"brawlOnEnd":true,
               "gateGlobal":"MFSofiaX_DrunkArmed"},
  "actors":[ {"aliasId":0,"npc":"MFSofia"},{"aliasId":1,"npc":"MFSofiaX_Drinker"} ],
  "phases":[ {"speaker":0,"lines":["Bet I can drink you under the table!"]},
             {"speaker":1,"lines":["You're on!"]} ] }
```

### F12 — 客製語音（仿 `JJSofiaVoiceType`，克隆 Sofia 嗓音）
(a) 給所有新台詞配上克隆的 Sofia 嗓音（含嘴型 lip）。
(b) **✅ 能**。`voiceTemplates[]`（`engine:"f5"` 零樣本克隆，`referenceWav`+`referenceText`）+ `npcs[].voiceTemplate`；CLI `voicelines`（先 `package` 再對 packaged esp 跑，folder 名才對）。**2026-06-13 in-game 確認**真 F5-TTS 克隆嗓音 + 官方 CK `LipGenerator` 嘴型。一個 distinct `voiceType` 出一份檔。
(c)
```jsonc
"voiceTemplates":[{ "id":"SofiaClone","engine":"f5",
  "referenceWav":"refs/sofia_ref.wav","referenceText":"Well, well, well.","seed":1234 }],
"npcs":[{ "editorId":"MFSofia","voiceTemplate":"SofiaClone","voiceType":"Skyrim.esm:0x0002F7C3" }]
```

### F13 — 隨從狀態 faction（跟隨中／已解散／鬥毆）
(a) 對話/package 依 Sofia 當前是「跟隨中」還是「已解散」分歧（仿 3 個 `SofiaFollowerFaction` 等）。
(b) **✅ 能**。`identities[]` 的 faction 機制（裸 editorId 自動建 FACT）或 vanilla `CurrentFollowerFaction`；condition 用 `GetInFaction` 開閘。`evaluateSpeakerPackages:true` 讓 setStage-gated 的 follow/escort package 立刻生效。Sofia 自己就是「把 vanilla 隨從系統包一層」，ModForge 的 `follower_vanilla_spec.json`（`DialogueFollowerScript.SetFollower`）正是同套路。
(c)
```jsonc
{ "questEditorId":"MFSofiaX_Talk","speakerNpcEditorId":"MFSofia",
  "prompt":"Wait here.","responses":["Fine. Don't take all day."],
  "conditions":[{"function":"GetInFaction","param":"Skyrim.esm:0x0005C84D","comparison":"==","value":1}] }  // CurrentFollowerFaction
```

### F14 — 戰鬥風格依玩家/情境切換（仿 `SofiaCombatClass`/6 CSTY）
(a) Sofia 依玩家偏好或處境切換戰鬥職業（弓/法/近戰）。
(b) **🟡 需小幅擴充**。`combatStyles[]`（CSTY builder）+ `classes[]`（CLAS）✅；但 Sofia 的「runtime 動態切換」靠 MCM index + Papyrus 換 package/CSTY。ModForge 可生**多個** Follow/Sandbox package 並用 `conditions`（`GetBaseActorValue` 玩家技能 / `GetGlobalValue`）讓引擎依條件選不同 package（list order = priority，首個符合 schedule+conditions 勝），達到**靜態條件切換**。真‧玩家可調的即時切換需 🟡 一個 `setGlobal` 對話選項（已有）翻一個「戰鬥模式」GLOB + package conditions 讀它——可組裝，但要多寫幾個 package entry。
(c)
```jsonc
"globals":[{"editorId":"MFSofiaX_CombatMode","type":"short","value":0}],
"packages":[
  {"editorId":"MFSofiaX_FollowMelee","template":"Skyrim.esm:0x019B2C","follow":{...},
   "conditions":[{"function":"GetGlobalValue","param":"MFSofiaX_CombatMode","comparison":"==","value":0}]},
  {"editorId":"MFSofiaX_FollowArcher","template":"Skyrim.esm:0x019B2C","follow":{...},
   "conditions":[{"function":"GetGlobalValue","param":"MFSofiaX_CombatMode","comparison":"==","value":1}]} ]
```

### F15 — 地點驅動評論（仿「進到某 location 就吐槽」）
(a) 玩家帶 Sofia 進入特定地點（Whiterun / 某地城）時 Sofia 評論該地。
(b) **🟡 需小幅擴充**。可用既有的 `ChangeLocation` Story Manager 事件（`newLocation` slot）啟一個 comment quest，但要 condition 在「哪個 location」上分歧需 **`GetInCurrentLocation`/`GetIsCurrentLocation`** CTDA——builder 白名單**沒有這兩個 location 函數**。小幅擴充 = condition 白名單加 location 系函數。降級方案（✅）：用 `autoStart` scene 的 actor 綁該地點的一個具名 vanilla NPC（等於 F1：靠近該地標誌人物即觸發），不需新 CTDA。
(c)
```jsonc
// 🟡 需 location CTDA：
{ "function":"GetInCurrentLocation","param":"Skyrim.esm:0x000164BC","comparison":"==","value":1 }  // WhiterunLocation
// ✅ 降級（綁地點代表 NPC，等同 F1）：actors=[Sofia, 該城衛兵/店主], autoStart 在場偵測
```

### F16 — MCM 可調參（評論頻率 / 追上距離）（仿 `SofiaCommentFrequency` 等）
(a) 玩家用 MCM 滑桿即時調「吐槽頻率」「Sofia 追上你的距離」。
(b) **🔴 缺口**。MCM 是 SkyUI 的 SKSE-only menu + Papyrus property 寫回 GLOB，ModForge 完全不生 MCM（無 SKSE 依賴、無 quest-config-menu script builder）。**設計取捨**：cooldown/triggerDistance 在 spec 裏是**作者**寫死的常數（`autoStart.cooldownSeconds`/`triggerDistance`），不是玩家 runtime 可調。要玩家可調需新增「MCM config quest + SKSE menu script」builder——大工程且引入 SKSE 依賴，與 ModForge「純 Papyrus、免 SKSE」基調衝突。建議**不做**，作者層提供合理預設即可。
(c)（不適用 — 無 spec shape；屬刻意不支援。）

---

## 3. 建議實作順序（✅-only 高價值優先）

按「先做純 ✅、高內容產出、複用已落地 controller」排序：

1. **F12 客製語音** — 先把克隆嗓音 pipeline 跑通（2026-06-13 已 in-game 確認），之後每批新台詞都能配音；先行可避免後面所有對話都得補配音。
2. **F1 看到具名 NPC 吐槽** — 純複製 `JJSofia<X>Comment` pattern，N 份 autoStart scene，內容產出最高、機制最成熟（`MFSceneBanterController` 已落地）。Sofia 28 scene 裏 8 個是這類。
3. **F3 條件式 idle 閒聊** — `banter` + `conditions`（時間/室內外/受傷/隨從限定），對應 Sofia 最大宗的 247 條 idle，純 ✅、無新機制。

接著（皆 ✅）：**F6 好感度 GLOB**（用 `setGlobal` 串起 F1/F3/F7 的條件分歧，是把零散吐槽變「有記憶的同伴」的黏合層）→ **F7 任務後感想**（`GetStage` condition，零成本加情境深度）→ **F5 技能驅動對話**（`GetBaseActorValue`，免 Sofia 的 18 GLOB workaround）→ **F8 多隨從互評** → **F9 set-piece 演出** → **F10 mini-quest** → **F11 喝醉/送禮** → **F13 隨從狀態 faction**。

最後（含 🟡/🔴，視需要才碰）：**F4 細分穿著評論**（先上 `WornHasKeyword` ✅ 版，formlist 細分留後）→ **F2 泛型目標吐槽**（待 scene actor `findMatching`）→ **F14 戰鬥切換** → **F15 地點評論**（先上「綁代表 NPC」降級版）→ **F16 MCM**（建議不做）。

---

## 4. 缺口彙總（🟡/🔴 = 本擴充催生的 ModForge 新增項）

| # | 功能 | 等級 | 需要的 ModForge 新增 | 影響面 | 降級替代（免新增即可上） |
|---|------|------|----------------------|--------|---------------------------|
| F2 | 泛型目標吐槽（最近的衛兵） | 🟡 | `autoStart` scene actor 接受 **`findMatching:closest`** fill（目前僅 `UniqueActor`；SM alias 已有 findMatching，搬進 scene actor 槽） | `Spec.Scene.cs` / `Generator.Build.Scene.cs WireScenes` | 用 `banter`（Sofia 單向吐槽、無對方回嘴） |
| F4 | 細分穿著評論（具名 armor 清單） | 🔴 | ① **`formLists[]`（FLST record builder）** ② condition 白名單加 **`GetIsInList`** | 新 `Spec.FormList.cs` + `Generator.Build.Conditions.cs` | `WornHasKeyword`（已有，涵蓋重甲/法袍/暴露等關鍵字分類，大宗夠用） |
| F14 | 戰鬥風格即時切換 | 🟡 | 無新 record（用既有 `setGlobal` + 多 package + `GetGlobalValue` conditions 組裝）；便利性可加「package 模式群」糖衣 | 純 spec 組裝；可選 doc/example | 多寫幾個 conditioned package entry，現在就能做 |
| F15 | 地點驅動評論 | 🟡 | condition 白名單加 **`GetInCurrentLocation` / `GetIsCurrentLocation`**（location 系 CTDA） | `Generator.Build.Conditions.cs` + `Generator.Validate.Quests.cs` | 綁該地點代表 NPC 的 `autoStart` scene（= F1 機制，免新增） |
| F16 | MCM 即時調參 | 🔴 | MCM config quest + **SKSE SkyUI menu script builder**（引入 SKSE 依賴） | 與「純 Papyrus、免 SKSE」基調衝突 | **建議不做**；作者層在 spec 給合理 `cooldownSeconds`/`triggerDistance` 預設 |

**便利性小提案（非阻塞，源自解碼 §⑤）**：F4/F5 都在「讀玩家穿著/技能驅動分歧」。目前要手寫 `WornHasKeyword`/`GetBaseActorValue` CTDA；可考慮加一個 `playerWears`/`playerSkill` condition 捷徑（糖衣展開成既有 CTDA），降低作者心智負擔——但**不是缺口**，現有 CTDA 已能表達。

---

### 結語

15 個具體功能裏，**✅ 能 10**（F1, F3, F5, F6, F7, F8, F9, F10, F11, F12, F13 — 註：含 F4 的關鍵字版；嚴格計 11 個全 ✅ 條目）、**🟡 需小幅擴充 3**（F2, F14, F15）、**🔴 缺口 2**（F4 formlist 細分、F16 MCM）。結論呼應解碼備忘：**擴充 Sofia 用 ModForge 直接夠用**，主機制（在場偵測 banter / GLOB 狀態 / mini-quest / 條件分歧 / 克隆語音）全部已落地且多數 in-game 確認；🟡/🔴 只動到「細分穿著 formlist」「地點/泛型目標 CTDA」「MCM」三類邊緣便利，核心擴充不被它們擋住。
