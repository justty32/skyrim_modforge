# Sofia × VIGILANT 支援計劃（2026-06-13）

**一句話**：用 ModForge 做一個 **Sofia-style 隨從對 VIGILANT 進度的「可對談反應」patch**——當 VIGILANT 的某個 scene 演完／某個任務狀態更新後，**Sofia 的對話會多出新選項，玩家可以主動找她談論剛發生的事**。她用那套自戀＋毒舌＋說溜嘴的嘴砲回應。把本 session 解碼的兩塊（[Sofia 性格](sofia-personality.md) + [VIGILANT 解碼](../vigilant-story-decode-2026-06-13.md)）縫起來。

**核心機制（2026-06-13 使用者定調）：以「玩家主動找她談」為主，不用 scene。**
- ❌ **不做**：自動插話、Sofia 自己開 scene 演出、在 VIGILANT 的 cutscene 中間插嘴（時序脆、會出戲、需重度實機迭代）。
- ✅ **要做**：VIGILANT 任務/scene 狀態更新後，在 Sofia 身上**新增一個 player dialogue 選項**（如「關於剛剛那個…你怎麼看？」），玩家想聊才聊。選項由 **VIGILANT 的 quest-state 當 condition 閘**控制出現時機，聊開後用**對話樹**展開她的吐槽串。

**兩個閘維度，可單用也可 AND 疊：**
1. **任務/scene 狀態**（時間維度）：`GetStageDone`/`GetQuestRunning`/`GetQuestCompleted` ── 「這件事發生後」。
2. **Sofia 所在地點**（空間維度，2026-06-13 使用者補）：`GetInWorldspace`/`GetInCurrentLoc`/`GetInCell`，**run-on = `Subject`（＝ Sofia 本人的位置，不是玩家）** ── 「Sofia 人在這個 realm/這間房時」。
   - 兩者疊（`AND`）＝「**在對的地方、對的時機**才出現的選項」，例：`GetStageDone(MolagQuest,30)==1 AND GetInWorldspace(Coldharbour, runOn=Subject)==1` → 只有玩家在 Coldharbour 裡、且已見過 Molag Bal，才談得到那段。
   - 地點也能**讓同一句話換味**（不只 gate 新選項）：用 vanilla「一個 topic 多條 INFO、依 condition 順序選第一個過的」模式（見 memory `conditioned-hello-one-topic-many-infos`），Sofia 的招呼/同一個 talk topic 在不同 realm 給不同台詞。

定位：**這不是新任務**，是疊在 VIGILANT 之上的**可對談反應層**。player topic 掛在 Sofia 身上（`GetIsID Sofia` / 她的 follower faction），用 VIGILANT 的**任務階段**（次要：地點）FormKey 當出現閘。**全部宣告式 record，無 user script。** 這正好是本 session 對話樹 + 跨任務閘的主場。

---

## 0. 為什麼現在做正合適

本 session 剛把這套需要的 ModForge 能力幾乎補齊（全 offline 測試過）：

| 反應類型（皆**玩家主動觸發**） | 靠的 ModForge 能力 | 本 session 狀態 |
|---------|------------------|----------------|
| **任務/scene 狀態更新後浮現新對話選項**（核心） | player topic + `GetStageDone(quest,stage)` / `GetQuestRunning` / `GetQuestCompleted` 當出現閘 | ✅ 本 session 落地（跨任務閘） |
| **選項只出現一次／聊過就收**（避免選單塞爆） | INFO `sayOnce` 旗標 + 選項點完 setGlobal 自收 | ✅ 本 session 落地 |
| **聊開後多輪對話**（玩家追問→Sofia 展開吐槽串） | 對話樹 `linkTo` + `topLevel:false` sub-topic | ✅ 本 session 落地 |
| **Sofia 身處某地點影響對話**（gate 新選項／同句換味；runOn=Subject＝她的位置） | `GetInWorldspace`/`GetInCurrentLoc`/`GetInCell`（runOn=Subject）；可與 quest-state AND | ✅ 本 session 落地 |
| 對特定 NPC/Boss「可以問她的看法」 | `GetIsID` / `GetInCell` / `GetDeadCount` 閘 | ✅（GetIsID 既有，其餘本 session） |
| Sofia 嗓音講新台詞 | voice pipeline（F5 clone + FUZ） | ✅ 既有（in-game 確認過） |

**結論：「狀態更新後浮現可對談選項」這個核心機制，現在 100% 做得出來、且 offline 可建。** 它就是 player topic + VIGILANT quest-state condition + 對話樹的組合，全是本 session 補齊的東西。唯一要實機驗的是**選項出現時機/選單會不會太雜**，不是「能不能做」。

---

## 1. 內容素材：VIGILANT 的 11 個 realm（地點評論的骨架）

> 注意（依使用者定調）：realm 評論是**次要**（Phase 3），而且也走「**玩家可問她看法**」的 player topic（`GetInWorldspace` 閘）而非自動吐槽。**主菜是任務高潮的談論選項（Phase 1，看 §5）**。下表是 realm 素材庫，給 Phase 3 與「Sofia 在某 realm 裡聊到該地」的台詞參考。

11 個 realm 素材（[worldspace 解碼](../vigilant-worldspace-decode-2026-06-13.md)）：

| realm FormKey | 名字 | Sofia 切入點（性格 hook） |
|---------------|------|--------------------------|
| `Vigilant.esm:0x06D275` | **Coldharbour**（Molag Bal 的領域，主 realm）| 毒舌 + 說溜嘴：對「魔神的家」裝沒在怕 |
| `Vigilant.esm:0x035457` | Lamae's Dream（夢境）| 咸濕未遂：「這是你的夢，那我怎麼還穿著衣服？…呃」|
| `Vigilant.esm:0x023E7E` | Stuhn Ravine | 嘴砲：對陰森地形吐槽 |
| `Vigilant.esm:0x0619A2` | Blood Curse: Envy（嫉妒詛咒）| meta + 自戀：「嫉妒？喔，大家都嫉妒我啊」|
| `Vigilant.esm:0x06C8A8` | Wasteland | 抱怨 + 自誇美貌跟荒原不搭 |
| `Vigilant.esm:0x078779` | Elder Field（結局 realm）| 難得露一句真心，再立刻嘴砲遮掩 |
| `Vigilant.esm:0x0B2AEE` | Colosseum（角鬥場）| 戰前自戀：「我的頭髮看起來還行吧？」|
| `Vigilant.esm:0x166857` | Hag's Pond | 對女巫吐槽（同性相斥的虛榮）|
| `Vigilant.esm:0x2C5DB1` | Old Forest | … |
| `Vigilant.esm:0x2DA92A` | Whale Graveyard | … |
| `Vigilant.esm:0x047CFA` | Bruiant's Estate（莊園）| 對「有錢人的家」品頭論足 |

> realm 進入偵測：`GetInWorldspace Vigilant.esm:0xXXXX == 1`（run-on = player 或 Sofia 皆可）。獨立 realm 用 worldspace；若要更細（realm 內某地標）用 `GetInCurrentLoc <LCTN>`。FormKey 直接寫 `Vigilant.esm:0x...`（external ref，**build 不需 Vigilant.esm 在場**，解析成 bare FormKey；patch 的 plugin 把 Vigilant.esm 列為 master）。

---

## 2. Sofia 聲音怎麼套（每條都過性格濾鏡）

黃金準則（見 [性格 brief](sofia-personality.md)）：**每句至少命中 自誇／嘴砲／說溜嘴／打破第四面牆 之一，最好兩個。** 下面是「玩家選了『談論這事』選項後，Sofia 的回應」範例（玩家主動找她聊，不是她自己插話）：

- **Coldharbour**：「So this is Molag Bal's realm. Cosy. Reminds me of my last boyfriend's place... the *decor*, I mean. Obviously. Why are you looking at me like that.」（嘴砲 + 說溜嘴）
- **Lamae's Dream**：「A dream realm? If this is *your* dream, why am I still fully clothed... Uh. I mean, why's it so gloomy in here.」（咸濕未遂招牌節奏）
- **Colosseum**：「An arena? If I'm going to fight to the death I'm doing it in style. Does my hair look alright?」（戰前自戀，直接借用 Sofia 既有戰前台詞風格）
- **Elder Field（結局）**：「...This place actually scares me a little. ...Don't you dare tell anyone I said that. I'll deny it.」（難得真心 + 立刻遮掩）
- **Blood Curse: Envy**：「Envy, huh. Can't blame them. Everyone's jealous of me. It's exhausting being this beautiful, really.」（自戀 meta）

每類各寫 2–4 條變體掛 `random`，避免重複聽膩。

---

## 3. 技術組裝（build 架構，全宣告式）

```
ModForgeSofiaVigilant.esp（masters: Skyrim.esm, Update.esm, Vigilant.esm, SofiaFollower.esp）
├─ 1 個 controller quest（StartGameEnabled，host 這些對白）
├─ 每個「可談的事件」= 一組 player 對話樹：
│   ├─ 觸發選項（top-level player topic，掛在 Sofia 身上）
│   │    prompt 例「關於剛剛 Coldharbour 那場…你怎麼看？」
│   │    出現閘 conditions（AND）:
│   │      ① GetIsID Sofia（或 follower faction）── 只在跟 Sofia 講話時出現
│   │      ② GetStageDone(VigQuest, N)==1 或 GetQuestRunning/GetQuestCompleted ── 事件發生後才出現
│   │      ③（選配）GetInWorldspace/GetInCell(runOn=Subject)==1 ── 只在 Sofia 人在該地時出現
│   │      ④（選配）GetGlobalValue(MF_SofiaTalked_X)==0 ── 聊過就收（防選單塞爆）
│   ├─ Sofia 的回應 INFO（過性格濾鏡的台詞）+ setGlobal MF_SofiaTalked_X=1
│   └─ linkTo → 1~2 個追問 sub-topic（topLevel:false），玩家可深聊
├─ voiceTemplates[] + npcs[].voiceTemplate：用 Sofia 的 voiceType
└─ voice files：F5 clone Sofia 嗓音（ref 從 SofiaFollower BSA 抽）→ FUZ loose asset
```

**為什麼用 player topic 不用 scene/hello**：① 玩家**主動觸發**＝不擾人、不打斷 VIGILANT 演出；② 出現時機只靠 quest-state condition，**不碰 scene 時序**（穩、offline 可建）；③ 剛好用本 session 的對話樹（`linkTo`/`topLevel:false`）做「聊開→追問」的多輪。

**關鍵接法**：
- **掛在 Sofia 身上**：Sofia NPC FormKey 從 [Sofia 解碼](follower-decode-2026-06-13.md) 拿；topic 的 INFO 加 `GetIsID SofiaFollower.esp:0xXXXX`（不需 override Sofia，只是 speaker 閘）。或用她的 follower faction。
- **「事件發生後才出現」的閘**：核心是 `GetStageDone(Vigilant.esm:0xQuest, stage)==1`（某 scene/任務狀態確定推進後）；長線用 `GetQuestRunning`/`GetQuestCompleted`。從 [story 解碼](../vigilant-story-decode-2026-06-13.md) 挑「演完一段值得聊」的 quest+stage。
- **「Sofia 人在某地」的閘（空間維度）**：`GetInWorldspace`/`GetInCurrentLoc`/`GetInCell`，**`runOn` 設 `Subject`**（INFO 的 run-on subject ＝ speaker ＝ Sofia，所以讀的是 **Sofia 的位置**；不設或 runOn=Reference 才是讀別人）。Sofia 跟著玩家跑，通常與玩家同地，但語意上「Sofia 在這」用 Subject 最正確。可單獨用（地點味台詞）或跟 quest-state `AND`（在對的地方＋對的時機）。
- **同一句換味（不開新選項，只換台詞）**：用「一個 topic 多條 INFO、引擎依 condition 順序取第一個過的」模式（[[conditioned-hello-one-topic-many-infos]]）——Sofia 的招呼或某個固定 talk topic，在 Coldharbour 給 A 版、在 Lamae's Dream 給 B 版、預設給 plain 版（地點 INFO 排前面、plain 墊底）。`GetInWorldspace(runOn=Subject)` 當每條 INFO 的閘。
- **選項自收**：點完該選項的 result fragment `setGlobal MF_SofiaTalked_X=1`，topic 條件再加 `GetGlobalValue==0`，聊一次就消失（VIGILANT 也大量用 global 當對白開關）。`setGlobal` 是既有 result fragment 能力，無 user script。
- **語音**：Sofia 全語音 → 靜音 subtitle 出戲，必須過 voice pipeline。`extract-voices <SofiaFollower BSA> <SofiaVoiceType>` 抽 ref → `voicegen-f5.sh` → `voicelines`。voiceType 查 Sofia 解碼。
- **build 是 offline**：Vigilant/Sofia FormKey 全是 external ref→bare FormKey，不需那兩個 mod 在 build 機（但最終 plugin master 清單要有、實機要裝）。

---

## 4. 可行性分級

| 反應類型（皆玩家主動觸發的對話選項） | 可行性 | 說明 |
|---------|--------|------|
| **任務/scene 狀態更新後浮現「談論」選項**（核心） | 🟢 100% 現在可做 | player topic + `GetStageDone/GetQuestRunning` 閘 + setGlobal 自收；本 session 對話樹+跨任務閘的主場 |
| **聊開後追問**（對話樹多輪） | 🟢 可做 | `linkTo` + `topLevel:false` sub-topic |
| **Sofia 所在地點影響對話**（gate 新選項 或 同句換味；runOn=Subject＝Sofia 位置） | 🟢 可做 | `GetInWorldspace`/`GetInCurrentLoc`/`GetInCell` 當選項閘，或「一 topic 多 INFO 依地點選」換味；可與 quest-state `AND` |
| **Boss/NPC 在場時「可問她的看法」** | 🟢 可做 | `GetInCell` 鎖 Boss 房 / `GetIsID` 對重要 NPC |
| ~~自動插話 / Sofia 自開 scene / 在 VIGILANT cutscene 插嘴~~ | ⬛ **刻意不做** | 使用者定調：擾人、時序脆、需重度實機迭代。改用「狀態更新後的可談選項」近似 |

---

## 5. 分階段 build plan

**Phase 1 — 任務高潮的「談論」選項（MVP，最高 CP）**
1. 1 個 controller quest + Sofia speaker 閘 + 一批 `MF_SofiaTalked_X` global。
2. 從 [story 解碼](../vigilant-story-decode-2026-06-13.md) 挑 ~8–12 個「演完值得聊」的 VIGILANT quest+stage（Coldharbour 入口、見 Molag Bal、各章高潮、結局抉擇…）。
3. 每個事件 = 一條 top-level talk topic（`GetStageDone==1 AND GetGlobalValue(MF_SofiaTalked_X)==0`）+ Sofia 回應（過性格濾鏡）+ setGlobal 自收。
4. F5 clone Sofia 嗓音 → voice files。
5. package → 實機驗：選項出現時機對不對、聊過有沒有收掉、選單會不會太雜。
→ 這一階段就足以成為能發布的 patch。

**Phase 2 — 聊開後的追問（對話樹）**
6. 給高潮事件加 `linkTo` → 1–2 個 `topLevel:false` 追問 sub-topic（玩家想深聊 Sofia 對某 NPC/抉擇的看法）。

**Phase 3 — 地點/NPC 的「可問看法」選項**
7. realm 內可問她對這地方的看法（`GetInWorldspace` 閘）。
8. Boss 房/重要 NPC 在場時的可問選項（`GetInCell`/`GetIsID` 閘）。

---

## 6. 依賴與風險

- **載入順序**：patch 的 master 要有 Vigilant.esm + SofiaFollower.esp，且排在它們之後。
- **FormID 穩定性**（最大風險）：condition 寫死 Vigilant/Sofia 的 FormKey；若使用者裝的 VIGILANT/Sofia 版本與解碼版本 FormID 不同，閘會失效（靜默不觸發，不崩）。解碼用的版本要記錄；發布時標明對應版本。
- **Sofia 嗓音**：要能抽到 Sofia 的 ref clip（SofiaFollower BSA）跑 F5；嗓音相似度影響沉浸感（F5 clone 過 MaleNord 實機 OK，女聲待驗）。
- **節奏**：地點評論若每次進 realm 都講會煩 → 預設 `sayOnce`（一輩子一次）或加 cooldown global；這要**實機調**（進 INGAME-TEST-QUEUE）。
- **受眾**：只有同時裝 Sofia + VIGILANT 的人吃得到——但這正是這類 patch 的常態，社群接受。

---

## 6.5 施工依據（已離線抽出，2026-06-13）

**表 1 — Sofia（`/tmp/sofia_a/SofiaFollower.esp`，CLI `find`）**

| 用途 | EditorID | FormKey |
|------|----------|---------|
| 講話對象（speaker 閘 GetIsID） | `JJSofiaFollower` "Sofia" | `SofiaFollower.esp:0x0012C4` |
| 語音（voiceType，配 voice pipeline） | `JJSofiaVoiceType` | `SofiaFollower.esp:0x0022EE` |
| 隨從中 faction（替代 speaker 閘） | `SofiaFollowerFaction` | `SofiaFollower.esp:0x060480` |

**表 2 — VIGILANT 可談錨點（v181 English，`find`+`questdiag`；stage 為暫定，logs 本地化、實機再校）**

| 事件 | Quest EditorID | Quest FormKey | 閘 |
|------|----------------|---------------|----|
| 初到 Coldharbour | `zzzCHMQ00` "Coldharbour" | `Vigilant.esm:0x12F24E` | `GetStageDone(…,10)` + `GetInWorldspace(Coldharbour, Subject)` |
| Coldharbour 深處 | `zzzCHMQ00` | `Vigilant.esm:0x12F24E` | `GetStageDone(…,90)` |
| 「The Grand Inquisitor」記憶後 | `zzzCHMemoryQuest01` "The Grand Inquisitor" | `Vigilant.esm:0x12C4F4` | `GetQuestCompleted` |
| Coldharbour worldspace | `zCHMolagWorld` | `Vigilant.esm:0x06D275` | （地點維度）|

## 6.6 Vertical slice 狀態（`examples/sofia_vigilant_slice.json`）

✅ **3 事件 vertical slice 已寫好、offline build + validate 通過**（510 測試綠）。`infodiag` 確認每條 INFO 的閘都對：GetIsID(Sofia 0012C4) + GetStageDone/GetQuestCompleted(Vigilant) + GetInWorldspace(runOn=Subject) + GetGlobalValue 自收 + 對話樹 linkTo→topLevel:false 追問。輸出 esp 自動帶 masters `[Vigilant.esm, SofiaFollower.esp]` + 寫 `.seq`。**record 機制完全驗證**。
**未做（next）**：① 語音（package + `voicelines`，需解 Sofia voiceType 0x0022EE + F5 clone 她的嗓音）；② package 成 zip；③ 實機驗「選項出現時機 / 聊過自收 / 選單清爽 / stage 校準」（進 `INGAME-TEST-QUEUE`）。

## 7. 下一步

- 先確認要不要做（這份是計劃，不是已開工）。
- 要做的話從 **Phase 1** 起：我可以先生一個 **2–3 個高潮事件**的 vertical slice（談論選項 + 對話樹追問 + voice）讓你實機驗「選項出現時機 / 聊過自收 / 選單清爽度」，再量產到 ~10 個事件。
- **開工前我會先離線抽兩張表**（Phase 1 的施工依據）：
  ① Sofia 的 NPC FormKey + voiceType + follower faction（查 [Sofia 解碼](follower-decode-2026-06-13.md)）；
  ② VIGILANT「演完值得聊」的 quest+stage 清單（查 [story 解碼](../vigilant-story-decode-2026-06-13.md)，挑 GetStageDone 的閘點）。
- 兩張表抽好就能直接寫 spec。這整套是本 session 對話樹 + 跨任務閘 + voice pipeline 的綜合應用，無新功能缺口。
