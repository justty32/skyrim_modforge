# VIGILANT scene/對話 vs ModForge 對照稽核（2026-06-13）

對 `Vigilant.esm`（v181，~21MB；離線 `SkyrimMod.CreateFromBinaryOverlay`，不載 master、不碰 BSA）只讀其自身的 **SCEN / DIAL / INFO / DLBR** 記錄，與 ModForge 的 `SceneSpec`／`DialogueSpec`（`src/ModForge.Core/Spec.Scene.cs`、`Spec.Dialogue.cs`）、build 路徑（`Generator.Build.Scene.cs`、`Generator.Build.Dialogue.cs`）、與 CTDA 白名單（`Generator.Build.Conditions.cs`）逐欄對照。所有數字皆來自實際記錄普查（探針已清，見文末）。

> 範圍：本稽核**只看「能不能表達（authoring 模型）」**，不看 runtime 行為。VIGILANT 的演出邏輯大量在 Papyrus fragment body 內（在未讀的 BSA），本稽核只能看 record 層的掛載點與條件資料。

---

## 普查數字（本次實測，作為一切論述的根據）

**Scene（78 個，phase 281，action 395）**
- action 型別：`Dialog=225 / Package=135 / Timer=35`（Mutagen `SceneAction.TypeEnum` 本身**只有這三種**，與 ModForge 一致——VIGILANT 沒有用任何 ModForge 缺的 action 型別）。
- scene Dialog action：**225/225 都帶 Emotion**（Neutral:179, Happy:18, Sad:9, Anger:8, Fear:5, Disgust:3, Surprise:2, **Puzzled:1**）、**225/225 都帶 HeadtrackActorID**、197/225 連 Topic、EmotionValue 非零 48、跨多 phase 的 Dialog action 20。
- scene Dialog action 的 `Flags`：`FaceTarget:78 / FaceTarget+HeadtrackPlayer:28 / HeadtrackPlayer:23`。
- Package action：135 個，**每個剛好 1 個 PACK**，54 個跨多 phase（`startPhase..endPhase` 區間鋪底走位）。
- **per-phase CompletionConditions 是常態：281 phase 中 280 個有**（StartConditions 只有 1 個）。Completion CTDA 分佈：`IsSceneActionComplete:274, GetDistance:15, GetInCell:13, GetInCurrentLoc:3, GetStage:2, GetInWorldspace:2, GetDead:1, GetQuestCompleted:1`。
- SCEN VMAD（fragment）：本次 binary-overlay 讀回 0（Mutagen overlay 對 SCEN VMAD 的已知限制）；沿用 `vigilant-story-decode-2026-06-13.md` 的 raw-byte 結論 **59/78 帶 SCEN fragment**（OnBegin/OnEnd 為主，24 個 phase fragment）。

**Dialogue（DIAL 1012 / INFO 1225 / DLBR 451）**
- topic category：`Topic:777 / Scene:198 / Misc:34 / Combat:3`；subtype：`Custom:777 / Scene:198 / Hello:25 / Goodbye:9 / Death:3`。**777 個 player Custom topic 全部掛 Branch**；663 個 topic 有顯示 Name。
- **DialogBranch (DLBR) = 451**——player 對話用 branch 大量分組。
- INFO 連結：**LinkTo (ENAM) = 285（其中 multi = 144）**、**PreviousDialog (PNAM) = 213**、WalkAwayTopic = 66、Speaker(覆寫)=26、result script (VMAD) = 234、shared ResponseData (DNAM) = 0。
- INFO 旗標分佈：`(none):703, Goodbye:235(+61+6+3), SayOnce:87(+61+12+1), WalkAway:55(+12), Random:42, InvisibleContinue:7, AudioOutputOverride:4, ForceSubtitle:1`；`ResetHours>0` 的 25 個；response flag 全部 `UseEmotionAnimation`，非中性 emotion response 674。
- **INFO CTDA 函式分佈（全 1225 INFO）**：
  `GetIsAliasRef:702, GetStage:449, GetIsID:310, GetQuestCompleted:79, GetInFaction:38, GetItemCount:28, GetStageDone:27, GetGlobalValue:22, GetEquipped:19, GetIsVoiceType:12, GetInCell:10, GetDeadCount:10, GetQuestRunning:5, GetGold:4, GetActorValue:2, GetSitting:1, GetRelationshipRank:1, GetMapMarkerVisible:1`。

---

## (1) 對照表

### Scene 特性

| VIGILANT 用的 scene 特性（實測） | ModForge 對應欄位 | 評級 |
|---|---|---|
| 多段 cutscene（≥2 phase，混 Dialog+Package+Timer） | `SceneSpec.Phases[]` + `Actions[]`（Package/Timer/Idle） | ✅ 有 |
| SceneAction 三型別 Dialog/Package/Timer（**無其他型別**） | `SceneActionSpec`（Dialog 自動／`package`／`timerSeconds`） | ✅ 有（型別完全覆蓋） |
| Package action 跨多 phase 鋪底（`startPhase..endPhase`，1 PACK，54 例） | `SceneActionSpec.StartPhase/EndPhase` + `Package` | ✅ 有 |
| Package action 每個剛好 1 PACK | `SceneActionSpec.Package`（單 ref） | ✅ 有（VIGILANT 不用多 PACK，無差） |
| scene Dialog action 帶 **Emotion**（225/225） | `Generator.Build.Scene.cs` 把 `ScenePhaseSpec.Emotion/EmotionValue` 寫到 SceneAction.Emotion ✅ | ✅ 有（但見 ⚠ 粒度，下） |
| Emotion = **Puzzled**（1 例） | `ScenePhaseSpec.Emotion` 只列 `Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled`，**漏 Puzzled** | ⚠ 語意有差（落 Neutral） |
| scene Dialog action 帶 **HeadtrackActorID**（225/225） | `ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`（phase 級） | ⚠ 粒度差（phase vs per-action） |
| per-phase **CompletionConditions**（280/281 phase；`IsSceneActionComplete` 274 為主） | `ScenePhaseSpec.CompletionConditions`（**有欄位**，但 BuildCondition 白名單**沒有 `IsSceneActionComplete`**） | ⚠ 欄位有、函式缺 |
| phase completion 用 `GetDistance:15 / GetInCell:13 / GetInCurrentLoc:3 / GetInWorldspace:2`（等玩家走到才推進） | `CompletionConditions` 欄位有，**這四個函式全不在白名單** | ❌ 缺 |
| SCEN fragment 跑任意 Papyrus（59/78：OnBegin/OnEnd + 24 phase fragment） | ModForge 只在 `idle` 時發 SceneAdapter phase fragment 跑 `PlayIdle()`，**body 寫死**；無 OnBegin/OnEnd fragment | ❌ 缺（同掛載點、不能寫任意 body） |
| 自訂 IDLE 動畫演出 | VIGILANT **0 個 IDLE 記錄**；ModForge 有 `SceneActionSpec.Idle` | ✅ ModForge 超前（VIGILANT 沒用） |
| CameraShot 運鏡 | VIGILANT **0 CAMS**；ModForge 也無 | ✅ 對等（雙方都不用） |
| Story Manager 起 scene/quest | VIGILANT **0 SM node**，全腳本/對話/書本啟動；ModForge 兩路都有 | ✅ 有（且 ModForge 多了 storyEvent 路線） |

### Dialogue 特性

| VIGILANT 用的 dialogue 特性（實測） | ModForge 對應欄位 | 評級 |
|---|---|---|
| player Custom topic（777）+ NPC Hello（25）+ Goodbye/Death | `DialogueSpec`（Custom 預設、`hello:true`、`goodbye:true`） | ✅ 有 |
| topic 顯示 Name（663）/ player 選單文字 | ModForge 用 `prompt` 當選單文字（INFO Prompt），VIGILANT 用 **topic.Name**——表達等效 | ⚠ 結構不同但可表達 |
| **DialogBranch (DLBR) = 451** 分組 player 對話 | `DialogueSpec` **無 branch 概念**；每個 topic 隱含一條 branch | ⚠ 語意有差（無顯式 branch 分組/拓樸） |
| **GetIsAliasRef（702，第一名）**——把 INFO 綁到 speaker 所填的 quest alias | 白名單**只有 GetIsID**，無 GetIsAliasRef | ❌ 缺（VIGILANT 最核心對話手法） |
| GetStage（449）/ GetStageDone（27）/ GetQuestCompleted（79）/ GetQuestRunning（5） | 白名單有 GetStage；**缺 GetStageDone / GetQuestCompleted / GetQuestRunning** | ⚠ 部分缺 |
| GetIsID（310）/ GetInFaction（38）/ GetItemCount（28）/ GetGlobalValue（22）/ GetActorValue（2）/ GetRelationshipRank（1） | 白名單全有 | ✅ 有 |
| GetEquipped（19）/ GetIsVoiceType（12）/ GetInCell（10）/ GetDeadCount（10）/ GetGold（4）/ GetSitting（1）/ GetMapMarkerVisible（1） | **全不在白名單** | ❌ 缺 |
| INFO **LinkTo (ENAM)** 285（multi 144）——對話樹分支 | `DialogueSpec` **無 LinkTo 欄位** | ❌ 缺 |
| INFO **PreviousDialog (PNAM)** 213——INFO 鏈/排序 | **無欄位**（ModForge 同 topic 多 INFO 靠 list order，跨 topic 無 PNAM） | ❌ 缺 |
| INFO 旗標 SayOnce（161）/ WalkAway（67）/ Random（59）/ InvisibleContinue（7）/ ForceSubtitle（1）/ ResetHours（25） | `DialogueSpec` **只有 `goodbye`**；Random 只在 `BanterSpec` 隱含 | ❌ 多數缺 |
| Goodbye 旗標（305） | `DialogueSpec.Goodbye` ✅ | ✅ 有 |
| INFO result script（VMAD，234） | `DialogueSpec.ResultScript/ResultScriptSource/ResultProperties`（須 CK 編） + 內建 `setStage/openBarter/setGlobal/rewardItem/...` | ✅ 有（純 record 動作更完整；任意 body 須自備 .psc） |
| response Emotion + UseEmotionAnimation（674 非中性） | `DialogueSpec.Emotion/EmotionValue`（套到所有 response） | ✅ 有 |
| WalkAwayTopic（66）/ Speaker 覆寫（26）/ AudioOutputOverride（4） | **無欄位** | ❌ 缺（低頻） |
| Scene-category topic（198，scene 內台詞） | `Generator.Build.Scene.cs` 自動產 Scene/SCEN topic+INFO | ✅ 有 |

---

## (2) 缺口清單（按影響排序）

1. **`GetIsAliasRef` CTDA 函式（VIGILANT 702 次、對話第一名）** — VIGILANT 幾乎每條任務對白都用 `GetIsAliasRef(aliasIdx)==1` 把 INFO 綁到「speaker 是這個 quest 的第 N 個 alias」，而非 `GetIsID(具名 NPC)`。這是因為它的演出 NPC 是 alias-bound（一個 alias 可被不同 actor 填）。ModForge 的 `DialogueSpec.SpeakerNpcEditorId` 只會發 **GetIsID**（`Generator.Build.Dialogue.cs`），白名單（`Generator.Build.Conditions.cs:6`）也沒有 GetIsAliasRef。**後果**：ModForge 無法為「alias 演員」寫對白，只能對寫死的 NPC editorId 寫——對 alias 重度（VIGILANT 550 alias）的劇情演出是結構性限制。範例：`zzAoMMq0B1Tvigilant` INFO `0x00005CE7` = `GetStage<10 AND GetIsAliasRef aliasIdx=0`。

2. **任意 scene-phase / OnBegin / OnEnd Papyrus fragment** — VIGILANT 59/78 scene 帶 SCEN fragment，跑 SetStage/召喚/移動等劇情邏輯。ModForge 的 SceneAdapter phase fragment **只在 `SceneActionSpec.Idle` 時產生，且 body 寫死成 `<alias>.GetActorRef().PlayIdle()`**（`Generator.Build.Scene.cs` 註解 + `SceneFragmentTests`）。沒有 OnBegin/OnEnd fragment，也不能塞任意 body。**這是「演出能不能驅動狀態機」的核心缺口**（與 decode doc 的「fragment 泛化」建議一致）。

3. **scene phase CompletionConditions 的位移/位置函式（`IsSceneActionComplete` 274、`GetDistance` 15、`GetInCell` 13、`GetInCurrentLoc`/`GetInWorldspace`/`GetDead`）** — `ScenePhaseSpec.CompletionConditions` 欄位**存在**，但 `BuildCondition` 白名單一個都不支援。VIGILANT 280/281 phase 都有 completion 條件（標準的「台詞講完才推進」= `IsSceneActionComplete`，以及「玩家走進 512 單位/進到某 cell 才推進」）。**後果**：ModForge 產的 scene phase 全是空 completion（靠引擎預設推進），無法表達 VIGILANT 那種「等玩家就位」的演出節奏；且 `CompletionConditions` 這個欄位目前對 scene 幾乎不可用（沒有能填的函式）。

4. **INFO LinkTo (ENAM) 285 + PreviousDialog (PNAM) 213——NPC 對話樹** — VIGILANT 用 ENAM 把一條 INFO 連到下一個 topic（NPC 說完接下一段、player 選項分支），144 條還是 multi-link（分支）。ModForge `DialogueSpec` 沒有 LinkTo / PreviousDialog 欄位，只能產扁平的單層 topic（一個 player option → 一段 response → 結束）。**無法表達多輪/分支的 NPC 對話樹**（這也是 `dialogue_conversation_spec.json` 之外更深的結構）。

5. **INFO 旗標：SayOnce（161）/ WalkAway（67）/ Random（59）/ InvisibleContinue（7）/ ForceSubtitle（1）/ ResetHours（25）** — `DialogueSpec` 只暴露 `Goodbye`。缺 `SayOnce`（一次性對白，VIGILANT 大量用）、`Random`（一般對白的隨機變體，目前只有 `BanterSpec` 隱含）、`InvisibleContinue`（INFO 鏈無縫接續）、`ForceSubtitle`、`ResetHours`（對白冷卻重置）。

6. **CTDA：`GetQuestCompleted`（79）/ `GetStageDone`（27）/ `GetEquipped`（19）/ `GetIsVoiceType`（12）/ `GetInCell`（10）/ `GetDeadCount`（10）/ `GetQuestRunning`（5）/ `GetGold`（4）/ `GetSitting`（1）/ `GetMapMarkerVisible`（1）** — 對話常用、ModForge 白名單缺。其中 `GetQuestCompleted` / `GetStageDone` / `GetQuestRunning`（跨 quest 進度閘）對串連多任務劇情影響最大。

7. **DialogBranch (DLBR) 顯式分組（451）+ topic.Name 選單文字（663）** — ModForge 沒有 branch 拓樸概念；無法表達 VIGILANT 那種「一個 NPC 在某狀態下展開一組分支選項」的 branch 結構（雖然單條對白仍能各自用 conditions 近似）。

8. **Emotion `Puzzled`** — `ScenePhaseSpec.Emotion`／`DialogueSpec.Emotion` 的解析清單漏了引擎第 8 種 emotion `Puzzled`（VIGILANT scene 用了 1 次）。落地會 fallback 成 Neutral。**一行 enum 修補即可。**

9. **WalkAwayTopic（66）/ Speaker 覆寫（26）/ AudioOutputOverride（4）** — 低頻 INFO 欄位，ModForge 無對應。

---

## (3) Correctness 疑慮（ModForge 模型 vs VIGILANT 實際結構）

1. **`CompletionConditions` 欄位形同虛設（最該注意）** — `ScenePhaseSpec.CompletionConditions` 在 spec 與 `WireScenes()` 都接好了，但能填進去的函式（白名單那 18 個）**沒有任何一個是 VIGILANT 在 scene phase 實際用的**（VIGILANT 用 `IsSceneActionComplete` / `GetDistance` / `GetInCell`…）。也就是說這個欄位目前對「真實 scene 演出」幾乎無用。**疑慮**：文檔/欄位給人「能做 condition-driven phase advance」的印象，但缺對應 CTDA 函式時實際做不到 VIGILANT 那種演出。

2. **scene Dialog action 的 headtrack 是 phase 級、VIGILANT 是 per-action 級** — ModForge 每個 phase 只發一條 Dialog action（`Generator.Build.Scene.cs`：一 phase 一 line 一 action），所以「phase 級 headtrack」在 ModForge 的單 action/phase 模型下其實等價。但 VIGILANT 有跨多 phase 的 Dialog action（20 例）與一個 phase 多動作的編排，這時 ModForge 的「一 phase 一 Dialog action」假設與 VIGILANT 的二維編排不完全同構——**ModForge 無法在同一 phase 放兩條不同 headtrack/emotion 的 Dialog action**。不是 bug，是模型粒度較粗。

3. **ModForge 用 GetIsID 綁 speaker，VIGILANT 用 GetIsAliasRef** — 兩者對「固定具名 NPC」等價，但 VIGILANT 的 scene 對白（Scene-category topic）在引擎裡是靠 SceneAction 的 ActorID（alias index）派發、**不靠 INFO 上的 GetIsID**。ModForge 的 scene topic INFO（`Generator.Build.Scene.cs`）**不加任何 speaker CTDA**（正確——scene 派發靠 action.ActorID），這點與 VIGILANT 一致 ✅。**疑慮只在 standalone dialogue**：ModForge 對 alias 演員的 standalone 對白只能 GetIsID，無法 GetIsAliasRef（見缺口 #1）。

4. **`Random` 旗標的對話變體** — ModForge 把 `Random` 綁死在 `BanterSpec`（ambient idle），但 VIGILANT 在一般 player/scene 對白也用 Random（59 個）做多條等價回應隨機選。ModForge 的 `DialogueSpec` 無法表達「這條 player 對白有三種隨機講法」。語意上 ModForge 把 Random 窄化成只屬於 banter，與 VIGILANT 用法不符。

5. **VMAD result fragment 的覆蓋面** — ModForge 的 `ResultScript` 對純 record 動作（setStage/openBarter/setGlobal/rewardItem/evaluatePackages）其實**比手寫 fragment 更安全完整**；但 VIGILANT 的 234 個 result script 多半是任意 Papyrus（移動、召喚、條件分支 SetStage）。對任意 body，ModForge 要求作者自備 `.psc` + CK 編譯——可行但非「宣告式產生」。這是能力邊界，不是 correctness bug。

---

## (4) 一句話總結

ModForge 已能宣告式產出 VIGILANT 級演出的**骨架**（多段 Dialog/Package/Timer scene、headtrack、emotion、alias、書本/觸發啟動、voice、PlayIdle，且 CAMS/IDLE/SM 都被 VIGILANT 證明非必要）——**結構覆蓋約 70%**；但要真正寫出 VIGILANT 的對白演出，三個結構性缺口必須補：**`GetIsAliasRef`（對白綁 alias 演員，702 次／第一名）**、**任意 scene-phase/OnBegin/OnEnd Papyrus fragment（驅動劇情狀態機）**、與 **scene-phase completion 的 `IsSceneActionComplete`/`GetDistance`/`GetInCell` 等 CTDA**（讓現有 `CompletionConditions` 欄位真正可用）；其餘為 INFO 對話樹（ENAM/PNAM）、SayOnce/Random 等旗標、跨任務進度 CTDA 與 `Puzzled` emotion 的補強。

---

> 探針：`/tmp/vig_audit_probe`（一次性，已清）。讀取方式 `SkyrimMod.CreateFromBinaryOverlay`（lazy overlay，未載 master、未碰 BSA）。SCEN VMAD 經 overlay 讀回 0（Mutagen 已知限制），fragment 數沿用 `vigilant-story-decode-2026-06-13.md` 的 raw-byte 結論。其餘所有數字均為本次實測普查。
