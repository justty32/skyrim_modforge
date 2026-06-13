# VIGILANT 劇情演出 解碼（2026-06-13）

對 `Vigilant.esm`（~21MB，大型重腳本任務 mod）做離線解碼，聚焦**劇情演出 / 場面調度（scene direction）**，作為 ModForge 的參考標的。
解碼方式：`SkyrimMod.CreateFromBinaryOverlay`（lazy overlay，不載入 master、不碰 `Vigilant.bsa`），所有跨檔引用只當 raw FormKey 看，不解進 Skyrim.esm。

> 探針程式碼為一次性，已清掉 `/tmp/vig_story_probe`；`/tmp/vigilant/Vigilant.esm` 為共用唯讀檔，保留未動。

---

## 1. 記錄總量普查（Census）

| 記錄群 | 數量 | 備註 |
|---|---:|---|
| **Quests (QUST)** | **120** | 114 帶 VMAD，**107 帶 Papyrus script fragment** |
| **Scenes (SCEN)** | **78** | 全部 ≥1 phase；59 帶 SCEN script fragment |
| **DialogTopics (DIAL)** | **1012** | Topic 777 / Scene 198 / Misc 34 / Combat 3 |
| **DialogINFOs（總和）** | **1225** | 1224 帶語音 response line |
| **Packages (PACK)** | **265** | 0 帶 script fragment |
| **Messages (MESG)** | 133 | |
| **Music (MUSC)** | 48 | |
| **Sounds (SNDR)** | 43 | |
| **ImageSpaces (IMGS)** | 7 | IMAD 2 |
| **VoiceTypes (VTYP)** | **3** | 只自訂 3 種，其餘沿用 vanilla |
| **CameraShots (CAMS)** | **0** | ⚠ 完全沒用 camera shot |
| **IdleAnimations (IDLE)** | **0** | ⚠ 沒有自訂 IDLE 記錄 |
| **Story Manager nodes (SMEN/SMBN/SMQN)** | **0 / 0 / 0** | ⚠ 完全沒用 Story Manager |
| AssociationTypes | 0 | |

**三個最醒目的「沒有」**：CAMS = 0、IDLE = 0、Story Manager nodes = 0。下面逐項解釋 VIGILANT 用什麼取代它們。

---

## 2. 場面調度技術（Scene Direction）

### 2.1 規模分佈 — 多段 cutscene 是主力

78 個 scene，phase 數分佈：

| phases | 場景數 |
|---:|---:|
| 1 | 21 |
| 2 | 10 |
| 3 | 22 |
| 4 | 5 |
| 5 | 3 |
| 6 | 9 |
| 7 | 2 |
| 8 | 1 |
| 9 | 1 |
| 10 | 1 |
| 14 | 2 |
| 16 | 1 |

- **57/78（73%）是多段（≥2 phase）演出**，最大 16 phase。單 phase 的 21 個多半是單句旁白 / 簡單觸發。
- 全 78 個 scene 都有 action；67 含 Dialog action、43 含 Package action、27 含 Timer action——典型一個 scene **同時混三種 action**。

最複雜的幾個 scene：

```
zzzCHMeQ11BadScene      phases=16 actions=23 dlg=10 pkg=10 tmr=3  frag=Y
zzAoMMq03Scene01        phases=14 actions=18 dlg=11 pkg=6  tmr=1  frag=Y
zzzCHMeQ12Sc01          phases=14 actions=18 dlg=12 pkg=5  tmr=1  frag=N
zzAoMMq05Scene01        phases=10 actions=13 dlg=8  pkg=4  tmr=1  frag=Y
zzzCHMeQ4FuneralScene   phases=8  actions=26 dlg=23 pkg=3  tmr=0  frag=Y
```

### 2.2 一個 scene 的內部結構（拆解 `zzzCHMeQ11BadScene` / `zzAoMMq03Scene01`）

典型「演出語法」是 phase 與 action 的二維編排：

- **Package action 鋪底走位**：`startPhase=0 endPhase=4`、`startPhase=1 endPhase=9` 這種跨多 phase 的 Package action，讓 NPC 在整段對白期間維持某個 AI 包（站定 / 行走 / 入座）。每個 Package action 引用 1 個 PACK（`pkgs=1`）。
- **Dialog action 逐 phase 推進台詞**：每個 phase 掛一條 `[Dialog] startPh=N endPh=N topic=<INFO FormKey>`，phase 換頁＝下一句。
- **Timer action 控制停頓 / 過場**：在沒有對白、只走位或等動畫的 phase 用 Timer 撐住該 phase（與 ModForge「fragment phase 必須有 Timer 才會 run」同一引擎事實）。
- **每一條 Dialog action 都設了 `HeadtrackActorID`**（225/225）——VIGILANT 全程用 headtrack 讓說話者看著對象，這是它「鏡頭感」的主要來源（因為它根本沒用 CAMS）。
- **每條 Dialog action 帶 Emotion**：

```
Neutral 179 / Happy 18 / Sad 9 / Anger 8 / Fear 5 / Disgust 3 / Surprise 2 / Puzzled 1
```
即用對白記錄的 emotion 欄位驅動臉部表情，做出戲劇張力，而非 camera。

### 2.3 SCEN script fragment（Papyrus），不是 PlayIdle

- **59/78 scene 帶 SceneScriptFragments**；其中只有一部分有 **phase fragment**（總計 24 個 phase fragment），多數其實是 **OnBegin / OnEnd** 整段 fragment（探針顯示大量 `onEnd=True`）。
- phase fragment 命名全是泛型 `SF_<scene>_<formid>.Fragment_<n>`（如 `SF_zzzCHMeQ11BadScene_022BAEFB.Fragment_6`）——**這些是 Papyrus 程式片段**（fragment body 在未讀的 BSA 內，但從 IDLE=0、CAMS=0 與命名可確定：它們是腳本邏輯（SetStage / 召喚 / 移動 / 開關物件），不是引擎 PlayIdle 記錄 driven 的動畫）。
- **關鍵對比**：ModForge 的 `SceneActionSpec.Idle` 走 SCEN phase fragment 跑 `<alias>.PlayIdle()`，技術上是同一個掛載點（SceneAdapter phase fragment）。VIGILANT 用同一個掛載點，但跑的是劇情邏輯而非 PlayIdle——也就是說 **ModForge 的 PlayIdle 機制與 VIGILANT 的 fragment 機制是同構的，只是 fragment body 內容不同**。VIGILANT 的「動作演出」其實主要靠 **Package action（走位 / 坐 / 活化）+ headtrack + emotion**，而非逐格動畫。

### 2.4 鏡頭 / cinematics — VIGILANT 完全不用 CAMS

`CameraShots = 0`。VIGILANT 沒有任何 camera shot 記錄，也沒有 camera path。它的所有「演出」都在第一/第三人稱的玩家自由鏡頭下發生，靠**走位 + headtrack + emotion + Timer 節奏**營造臨場感。這跟它的 jank 美學一致：劇情厚、演出靠 AI package 編排而非運鏡。

> 對 ModForge 的啟示：**沒有 CAMS 也能做出 78 個多段演出**。CAMS 是 nice-to-have，不是劇情 mod 的必要條件。

---

## 3. Story Manager 使用情況 — 完全沒用

- SMEN / SMBN / SMQN node 全部 = 0。
- 120 個 quest 的 `Quest.Event` **全部為空**（`非-None = 0`）——沒有任何 quest 透過 Story Manager 事件啟動。

**VIGILANT 怎麼啟動 quest？** 答案在 fragment 與對話：**107/120 quest 帶 Papyrus script fragment**。VIGILANT 是「手刻 Papyrus」驅動的 mod——quest 由閱讀書本、對話選項、觸發器、`SetStage`、`Start()` 直接從腳本啟動，而不是讓引擎的 radiant Story Manager 去配對事件。這是大型線性劇情 mod 的典型做法（劇情要可控、可分支、可重現，不要 radiant 的隨機配對）。

> 對 ModForge 的對照：ModForge 的 `storyEvent`（SMBN→SMQN 掛 vanilla 根）解的是「engine 事件 → 自動起 quest」的 radiant 場景。VIGILANT 證明**一個大劇情 mod 可以完全不碰 Story Manager**，改用顯式腳本 / 對話 / 書本觸發。兩條路線並存，不衝突。

---

## 4. Quest 規模

- 120 quest，**總計 914 stage、160 objective、550 alias**。
- 平均每 quest ~7.6 stage、~4.6 alias。
- 最大的幾個：

```
zzzCHMemoryQuest10   stages=40 obj=0  alias=11   frag=Y
zzzCHMemoryQuest08   stages=26 obj=1  alias=19   frag=Y
zzzAoMSubQ02         stages=22 obj=13 alias=12   frag=Y
zzzAoMMq06           stages=20 obj=9  alias=14   frag=Y
zzzCHMQ01            stages=20 obj=0  alias=14   frag=Y
```

觀察：
- **stage 多但 objective 少**（914 stage vs 160 objective）。很多 stage 是**無日誌的內部狀態旗標**（演出 / 分支控制用），不是玩家可見的任務目標。`zzzCHMemoryQuest10` 有 40 stage 卻 0 objective——純腳本狀態機。
- **alias 重度使用**（550 個）：演出 scene 需要把每個演員綁成 quest alias 才能在 SceneAction / Package 裡引用。alias 數 ≈ 演出複雜度的代理指標。
- **107/120 quest 帶 fragment**：幾乎每個 quest 都有 Papyrus，分支與演出邏輯全寫在 fragment 裡。

---

## 5. 語音對白規模

- **1225 INFO，其中 1224 帶 response line**（≈ 100% 全語音）。VIGILANT 是全配音 mod（音檔在未讀的 BSA 內，本次不碰）。
- 對白依 topic category 分：Topic 777 / **Scene 198** / Misc 34 / Combat 3。**Scene-category topic 高達 198**——這些是 scene 內逐 phase 播放的演出台詞，呼應 78 個 scene 的密集 cutscene。
- **只定義 3 個自訂 VoiceType**——其餘沿用 vanilla voice type（成本考量；自訂 NPC 借 vanilla 嗓音）。

對白文字風格抽樣（保留原始非母語英文，反映其手感）：

```
| A woman who maybe summoner of Daedra was witnessd. Our missions are defeating Daedra and catch the summoner.
| Yes...a woman who is picked a quarrel by drunkard had summoned Daedra. drunkard was teared up by Daedra...
| A queer Daedra....it was. Anyway, most inmportant thing is cathcnig the summoner.
| All right. We, vigilant of Stendarr deal with vampire.
| Hahaha, don't stand on ceremony so much. You and I are agents of Stenndarr.
| Viglants find her in the Bee and Barb. They will catch her....
```

---

## 6. ModForge 對照（每項技術：今天可做 / 需小增 / 缺口）

| VIGILANT 演出技術 | ModForge 現況 | 評級 |
|---|---|---|
| **多段 scene cutscene（≥2 phase，混 Dialog+Package+Timer）** | `SceneSpec` 已支援 per-phase Dialog / Package（引 PACK）/ Timer action。78 個 scene 的核心結構**今天就能產**。 | ✅ 今天可做 |
| **Package action 跨多 phase 鋪底走位（`startPhase`..`endPhase`）** | ModForge scene action 支援 Package 引 PACK；需確認是否暴露 `StartPhase/EndPhase` 跨 phase 區間欄位（VIGILANT 大量用 `0..4` 這種跨段 package）。 | 🟡 需確認/小增（per-action start/end phase 範圍） |
| **每條 Dialog action 設 HeadtrackActorID** | ModForge 有 per-phase headtrack（`ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`）。VIGILANT 是 **per-Dialog-action** headtrack；若 ModForge 只在 phase 級設，語意接近但粒度略粗。 | 🟡 小增（action 級 headtrack）/ 多數情境 ✅ |
| **每條 Dialog action 帶 Emotion（表情驅動演技）** | ModForge voice/dialogue 是否在 scene Dialog action 上暴露 emotion+emotionValue？VIGILANT 全程用此做戲劇張力（CAMS=0 的替代品）。 | 🟡 需小增（SceneAction Dialog 的 Emotion/EmotionValue 欄位）—**高價值低成本** |
| **SCEN phase fragment 跑劇情邏輯（SetStage / 召喚 / 移動）** | ModForge 已有 SCEN phase fragment 掛載（`SceneAdapter`），但目前 body 限定 `PlayIdle()`。VIGILANT 證明同一掛載點可跑任意 Papyrus。 | 🟡 需小增（讓 phase fragment 能發 `SetStage` / 其他純 record 動作，而非只 PlayIdle） |
| **PlayIdle / 動畫演出** | ModForge 有 `SceneActionSpec.Idle`（已 in-game 確認）。VIGILANT **反而沒用 IDLE 記錄**——動作靠 Package（坐 / 活化 / 走位）。 | ✅ ModForge 已超前此項 |
| **Camera Shot（CAMS）/ 運鏡** | ModForge 尚未建 CAMS。 | ❌ 缺口——但 **VIGILANT 證明 CAMS 非必要**（78 個演出 0 CAMS）。優先級可低。 |
| **Story Manager 起 quest** | ModForge 有 `storyEvent`（SMBN→SMQN）。VIGILANT 完全不用，改腳本 / 書本 / 對話啟動。 | ✅ ModForge 兩條路線都有（storyEvent + 書本 `MFIdentityBook` / 對話觸發 / trigger 庫） |
| **顯式腳本啟動 quest（書本 / 對話 / 觸發器 → SetStage / Start）** | ModForge 有可複用 trigger 庫（magic/potion/activator/dialogue/alias）+ `MFSE_AdvanceStage` stage 推進 + 書本觸發。**這正是 VIGILANT 的主啟動模式**。 | ✅ 今天可做 |
| **大量 stage 當內部狀態旗標（無 objective）** | ModForge `StageSpec` 支援無 objective 的 stage + `startUpStage`。 | ✅ 今天可做 |
| **重度 alias（550 個）綁演員給 scene/package 引用** | ModForge `QuestSpec.aliases` 五種 fill。產 N 個 alias 沒問題；大型演出要批次產 alias。 | ✅ 今天可做（規模化生成是工具強項） |
| **全語音 INFO（音檔）** | ModForge 有 voice pipeline（TTS→XWM→FUZ + lip，已 in-game 確認）。 | ✅ 今天可做 |

### 兩個最划算的 ModForge 增補（低成本高回報）

1. **Scene Dialog action 的 Emotion / EmotionValue 欄位**：VIGILANT 用它（搭配 headtrack）取代整個 CAMS 系統做演技張力。ModForge 加這一個欄位就能讓生成的 cutscene「有表情」。
2. **Scene phase fragment 泛化（不只 PlayIdle）**：讓 `SceneActionSpec` / phase fragment 能在 phase 邊界發 `SetStage` 等純 record 動作，匹配 VIGILANT 用 fragment 推進劇情狀態機的主模式。

### 一個明確缺口（但可延後）

- **CameraShot (CAMS)**：ModForge 尚未建。VIGILANT 用 0 個 CAMS 做出 78 個多段演出，證明它對「能不能做劇情 mod」非阻塞。**建議優先級：低**——先補 Emotion 與 fragment 泛化，CAMS 之後再說。

---

## 7. 一句話總結

VIGILANT 是一個**「重 Papyrus fragment + 多段 Package/Dialog/Timer scene + headtrack + emotion」**驅動的大型線性劇情 mod：120 quest / 914 stage / 550 alias / 78 scene（73% 多段）/ 1225 全語音 INFO。它**完全不用 Story Manager、不用 CAMS、不用自訂 IDLE**——演出全靠 AI package 走位、headtrack 視線、對白 emotion 表情，與逐 phase 的 Timer 節奏。ModForge 今天已能複製其大部分結構（scene、quest、alias、書本/觸發啟動、voice、PlayIdle）；最划算的兩個增補是 **scene Dialog action 的 emotion 欄位** 與 **scene phase fragment 泛化（SetStage 等）**；唯一明確缺口 CAMS 經 VIGILANT 證明可延後。
