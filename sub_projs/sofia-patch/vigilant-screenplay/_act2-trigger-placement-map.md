# Act 2 — Sofia 評論觸發點 / 場景 / 地點 放置地圖

> 目的：確定每一條 Sofia 評論都掛在**對的 VIGILANT stage / 場景 / 地點**上（情境正確）。
> 來源：BSA 明文 QF_/SF_/TIF_ 碎片逆向（2026-06-14）。Stage 語意由 PSC fragment 直接解碼。
> Act 2 VIGILANT 官方名稱**未確認**，任務鏈：`zzzBMMq01`（Empty Jails, 0x038524）→ `zzzBMMq02`（The Wreck, 0x038525）→ `zzzBMMq03`（Blood Matron, 0x038526）。

---

## 1. Act 2 任務鏈說明

```
zzzBMMq01 (0x038524) "Empty Jails"
  s0  startup: gCurrentAct=2, Obj0 "Talk to Courier"
  s10 Obj10 "Talk to Steward" (Courier done)
  s20 Obj20 "Search Windhelm Dungeon" + DungeonKey given
  s30 Obj30 "Talk to Steward about Maiden Statue" (statue found)
  s40 Obj40 "Find Report in Temple of Stendarr" + BookCase unlocked
  s50 ALL objectives done; Obj50 "Talk to Steward"; JacobNote+StoneFaceKey given
  s60 Obj60 "Defeat Vampires under Windhelm" → starts zzzBMMq02
  s100 Obj100 "Report to Steward" (vampires defeated)
  s110 Good End: ModPious(4), +2000g, starts qAct3, Stop()
  s255 Bad End / Skip

zzzBMMq02 (0x038525) "The Wreck"
  s0  Obj10 "Defeat Vamp01" (Vamp01 spawns in combat)
  s10 Vamp01 killed → Vamp02 package evaluated
  s20 Obj30 "Defeat Vamp02" (Vamp02 joins MolagFaction, hostile)
  s30 Vamp02 killed
  s40 Obj50 "Defeat Vamp03" (Vamp03 spawns, combat)
  s50 Vamp03 killed
  s60 Vamp04→Vamp04Ess; Obj70 "Give Vamp04 Mercy of Stendarr" displayed
  s70 Obj70 completed; Passage disabled; qGuide s10
  s80 Obj90 "Defeat Vamp05" (Vamp05 hostile)
  s90 Vamp05 killed → ModPious(5), starts zzzBMMq03, Stop()

zzzBMMq03 (0x038526) "Blood Matron"
  s0  qGuide.SetStage(20); DisablePlayerControls; Lamae bound effect → MoveTo(DreamMarker) → SetStage(30)
  s30 In dream; Molag Bal fades out (VanishEffect)
  s50 Obj60 "Defeat LamaeBal"; LamaeBal joins MolagBalFaction; combat + MusLamae
  s80 Obj90 "Break Curse"; LamaeZombie resurrects; StandUpSpell
  s90 Good End: ModPious(6), Karma+3, LamaeZombie killed, DisenchantEffect, Mq01.SetStage(100), Stop()
  s200 Bad End: AbBloodofLamae given, Karma-3, starts CHMq00 (Act 3), Stop()
```

**Chain trigger points:**
- Mq01 s60 → starts Mq02
- Mq02 s90 → starts Mq03
- Mq03 s0 Fragment_2 → immediately triggers dream entry → SetStage(30)
- Mq03 s90 → good end; sets Mq01 s100 → Mq01 s110 = Act 2 truly complete
- Mq03 s200 → bad end (player accepted Molag Bal's offer / got cursed)

---

## 2. 評論放置總表

| beat | 機制 | 正確 gate（除 `GetIsID Sofia` 外） | 依據（碎片） | 信心 |
|---|---|---|---|---|
| **2-A 衛兵來尋，開第二幕** | 玩家可問 | `GetQuestRunning(Mq01 0x038524)==1` + `GetStageDone(Mq01, 0)==1` + `GetStageDone(Mq01, 10)==0` | Mq01 Fragment_0: gCurrentAct=2 set; s0=startup active, s10=Courier done（話還沒說完前的窗口） | 高 |
| **2-B 書本學識面** | 玩家可問 | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 50)==0` | Mq01 s20=「Search Dungeon」已啟（DungeonKey given）；s50 之前調查仍進行中；「讀書」動作最貼近s20收到任務書 | 中（beat 本身是原創鉤；s20 是最佳 approximation） |
| **2-C 進 Windhelm，下水道前** | 玩家可問 | `GetStageDone(Mq01, 10)==1` + `GetStageDone(Mq01, 20)==0` | Mq01 s10 completed "Talk to Steward"，下一步 s20「Search Dungeon」尚未啟動 | 高 |
| **2-D 下水道內，吸血鬼出現** | 玩家可問 | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 60)==0` | Mq01 s20=進地城搜索中；s60=Mq02 啟動（vampire fight 正式開始）前 | 高 |
| **2-E 搜查隊有名人物** | 玩家可問 | `GetStageDone(Mq01, 30)==1` + `GetStageDone(Mq01, 60)==0` | Mq01 s30-s50 = 調查深化（女神像→報告）；s60 前 NPC 仍可對話 | 中（名人物身份未完全確認） |
| **2-F Lamae 沉睡宮殿** | 玩家可問 | `GetStageDone(Mq02 0x038525, 90)==1` + `GetStageDone(Mq03 0x038526, 30)==0` | Mq02 s90=Mq03 剛啟動；Mq03 Fragment_2 在 s0 立即觸發夢入場→SetStage(30)；此為 Mq03 啟動到夢觸發前的極短窗口 | 中（窗口極短；若 s0 觸發太快可改 GetQuestRunning(Mq03)+s30==0） |
| **2-G 夢後·Lamae 夢入場（DEFERRED）** | 玩家可問（**出夢後**，事後話題） | `GetStageDone(Mq03, 30)==1` + `GetStageDone(Mq03, 50)==0` | Mq03 Fragment_2 SetStage(30)=player 已被送入夢中（dreammarker）；s50 前 Lamae 戰尚未開始 | 高 |
| **2-H 夢中演出·學識面（DEFERRED）** | 玩家可問（**出夢後**，事後話題） | `GetStageDone(Mq03, 50)==1` + `GetStageDone(Mq03, 80)==0` | Mq03 s50=Lamae 戰 (Obj60 active)；s80 前第一戰仍進行中 | 高 |
| **2-I 雙重 Lamae 戰＋王座提示** | 玩家可問 | `GetStageDone(Mq03, 80)==1` + `GetStageDone(Mq03, 90)==0` | Mq03 Fragment_26: s80=LamaeZombie 復活 StandUpSpell；Obj90「Break Curse」顯示 | 高 |
| **2-J 章節收束（好結局）** | 玩家可問 | `GetStageDone(Mq03, 90)==1` | Mq03 Fragment_28: s90=Karma+3 + Mq01.SetStage(100) + Stop；好結局確認 | 高 |
| **2-J 章節收束（壞結局）** | 玩家可問 | `GetStageDone(Mq03, 200)==1` | Mq03 Fragment_30: s200=AbBloodofLamae given + Karma-3；玩家接受 Molag Bal 詛咒 | 高 |

每條皆 `sayOnce` + 各自 GLOB once-flag；好/壞結局互斥另加對方 GLOB==0。

---

## 3. 關於 2-G / 2-H 夢境機制（DEFERRED 說明）

**現況**：Mq03 Fragment_2 在 s0 立即 DisablePlayerControls → 施放 Lamae 束縛效果 → 淡出 → MoveTo(DreamMarker) → SetStage(30)，整個流程是引擎驅動的。ModForge 目前**尚未有能在玩家被強制移動前插入 Sofia 對話的機制**（需要 controller quest + Papyrus + MoveTo 掛件，與 Act 1 的 1-D 夢境掛件相同概念）。

**過渡方案（本版）**：2-G 與 2-H 實作為**玩家出夢後的事後話題**。
- 2-G：`s30==1`（已入夢）+ `s50==0`（Lamae 戰未開始）→ Sofia 說她在外面等、看到你消失、回來後問你經歷了什麼。
- 2-H：`s50==1`（Lamae 戰階段）+ `s80==0`（殭屍 Lamae 前）→ 玩家問夢中看到什麼，Sofia 發表學識見解。

**未來實作**：同 1-D 夢境掛件方案。Mq03 dreammarker 位置待確認後再設計 Sofia MoveTo。

---

## 4. 場景 / 地點清單（Act 2）

| cell/worldspace | FormID | 屬 | 用途 |
|---|---|---|---|
| Windhelm 下水道/地牢 | （需 Mq01 cell 確認） | Mq01 | 2-C / 2-D / 2-E |
| Lamae 沉睡宮殿 | （Mq02 內部 cell 確認） | Mq02 / Mq03 | 2-F |
| Lamae 夢境 cell | （Mq03 DreamMarker 所在） | Mq03 | 2-G/H 夢境（DEFERRED） |

**注意**：Act 2 cell FormID 尚未從 BSA 直接確認，上表留空待日後 Mq01/Mq02/Mq03 cell scan。Gate 使用 stage 條件即可正常運作，不依賴 `GetInCell`。

---

## 5. 仍需實機確認

- Mq03 s0 到 s30 之間的窗口長度（2-F gate 依賴此窗口）；若太短，改用 `GetStageDone(Mq02, 90)==1 + GetStageDone(Mq03, 30)==0` 搭配 Mq03 啟動 1-2 秒內的 TopicBranch。
- 壞結局路徑（Mq03 s200）的觸發條件：玩家在 Molag Bal 對話中選擇接受詛咒的確切機制，待確認。
- zzzBMGuide quest（qGuide）stage gate 與 Mq02/Mq03 的聯動：s10（Trace the elder blood）= Mq02 s70；s20（CompleteQuest）= Mq03 s0。不影響 Sofia dialogue gate，但值得記錄。
