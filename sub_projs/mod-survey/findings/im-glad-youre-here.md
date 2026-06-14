# I'm Glad You're Here / GYH

- 類型：通用互動型 / follower-family interaction and animation framework
- Plugin：`ImGladYoureHere.esp`
- 已解壓：`~/skyrim_mods/unzip/Glad You're Here - Main File-41856-3-6-0-1775486612/`
- 抽出資料：`../game-data/mods/ImGladYoureHere/`
- 規模：10 quests，550 dialogue lines，10 NPCs，60 books，58 items，1 location，15 magic records

## 結構

GYH 的核心不是敘事台詞，而是「把一句對話轉成可控的互動動作」。它提供 follower、spouse、children、animals 的擁抱、salute、embrace、petting、welcome home、letters、boon 等功能。

代表 quest：

- `000D62 WW42HugFollower "GYH Quest to Handle Follower Content"`：follower/animal 擁抱與動作核心。
- `21AF77 WW42HugFamily`：spouse/children/family 擁抱。
- `18D29E WW42TrackPlayerCells`：追蹤玩家換 cell，支援 welcome home。
- `014FEA WW42ConfigMenu`：MCM 設定。
- `0BD8EC WW42ExtraFollowerContent`：相容 patch、第三方 follower 偵測。
- `32C625 WW42LettersQuest`：信件系統。
- `299896 WW42GYHDoNotMove`：動作期間固定 NPC 的 package/alias。

主要 dialogue 入口：

- `WW42HugFollowerBranchTopic`：prompt 是 `I'm glad you're here.`，對 follower 觸發擁抱。
- `WW42HugSpouseBranchTopic`：prompt 是 `I love you, you know.`，對 spouse/family 觸發互動。

大量 TIF script 只是呼叫：

```papyrus
(GetOwningQuest() as WW42HugFollowerScript).HugActor(akSpeaker)
```

這說明 GYH 把行為集中在 quest script，topic fragment 只負責把 speaker 傳進去。這是很乾淨的架構。

## 動作系統

`WW42HugFollowerScript.psc` 是最重要的參考。它把一次互動拆成一個完整 pipeline：

1. 檢查 speaker 是否在 scene 中；若在 scene 且沒有 ignore faction，禁止 animation。
2. 對 player 和 target 播放 `IdleStop_Loose`，避免卡在前一個 idle。
3. 判斷 actor type：adult、child、animal，可被 external property override。
4. 根據 MCM、性別、same-sex/opposite-sex、child/animal 設定選 animation。
5. 處理第一/第三人稱鏡頭；embrace 或特定 petting 需要強制第三人稱並隱藏 first-person geometry。
6. 可選擇收武器、卸盾，等待 sheathe 完成。
7. 決定誰主動抱誰：依第一/第三人稱、身高差、random、child/animal、external role override。
8. 播放 animation：vanilla hug、salute、embrace、horse hug、dog petting 等。
9. 發 boon spell，更新 alias 與 duration。
10. 清理 IdleStop、DoNotMove、globals、external override，送出 `GYH_OnHugActor` mod event。

這個 pipeline 的價值在於它處理了 Skyrim 動作最容易壞的地方：scene protection、weapon/shield、camera、idle cleanup、NPC package、身高/角色站位、動作後狀態恢復。

## Animation 選擇與站位

GYH 沒有只靠一個 hug idle。它用多種策略：

- vanilla hug：`PlayIdleWithTarget(pa_HugA, akTarget)`。
- salute：對 target 和 player 發 `idlesalute` animation event。
- embrace：重用 Cicero dance idles，配合移動 NPC 到 player 前方、match rotation、`SetDontMove`、狀態 global。
- horse / dog：使用特定 idle 或 JaySerpa pet animation，並固定動物。

`GetWhoHugsWho` 用身高差決定角色順序：玩家與 NPC 的 scale 差距超過約 `0.035` 時會影響誰播放主動 idle。這是很實用的細節，因為擁抱類 animation 對 actor order 很敏感。

`ExtOverrideNPCHeight`、`ExtForceWhoHugsWho`、`ExtForceAnimationType` 等 external properties 是給 patch 用的。某些 TIF 會先設 `ExtOverrideNPCHeight = 0.9` 或 `actorType = 2`，再呼叫 `HugActor()`。這種設計讓第三方 follower 不必改主腳本，只要在 topic fragment 注入少量 override。

## Scene protection

GYH 對「NPC 正在 scene 中」很謹慎：

- 若 `target.GetCurrentScene()` 不是 none，且 target 不在 `WW42GYHIgnoreSceneProtectionFaction`，就不播放 animation。
- 這避免對話 scene、AI scene、任務演出中硬插 idle 造成卡死。
- 若某個 follower 確實允許 scene 中互動，可以用 ignore faction 明確放行。

這點對 follower dialogue expansion 很重要。自家模組如果有長 scene，又想加入 hug/kiss/pat 等動作，應該同樣做 scene guard；否則玩家在 scene 中點互動很容易破壞站位或 dialogue flow。

## Welcome home 與 tracking

`WW42WelcomeHomeScript.psc` 和 `WW42TrackPlayerCellsScript.psc` 讓 GYH 不只是手動點選互動：

- 追蹤最多 30 個 actor 與 last seen time。
- 玩家換 cell 時，如果 spouse/child 在同 cell 且 cooldown 足夠，讓對方 forcegreet。
- forcegreet 後可接 family hug。
- 支援 child welcome home chance、Hearthfire adoption、Multiple Adoptions alias。

這是通用 follower/family 互動的另一個關鍵：互動不只由玩家按 topic 觸發，也可以由「玩家回家」這種情境觸發。

## 相容 patch 與 Sofia 線索

GYH 使用 INI / FormList / alias 方式支援第三方 follower：

- `ImGladYoureHere_FLM.ini` 把多個 custom voice 加入 form list，包括 Auri、Inigo、Kaidan、Lucien、Mirai、Mrissi、Sofia、IFD Lydia 等。
- Sofia 對應 `WW42SofiaVoiceFormList | 0x22ee~SofiaFollower.esp`。
- `ImGladYoureHere_DISTR.ini` 會把 faction 發到 `JJSofiaFollower`，修正 Sofia 類 follower dialogue。
- `WW42ExtraFollowerContentPAScript.psc` 偵測 `SofiaFollower.esp`，把 Sofia actor `0x00001827` force 到 alias，並設定 patch global。

這代表 GYH 已經處理過 Sofia 類 unique follower 的對話/互動相容問題。後續做 Sofia dialogue expansion 時，應檢查是否可復用 GYH 的 faction/form list 條件，而不是自己另建一套相同判斷。

## 對隨從對話擴展的參考價值

GYH 是動作層參考，不是 narrative arc 參考。它最值得學的是「把動作封裝成穩定 service」：

- dialogue INFO fragment 只呼叫 `HugActor(akSpeaker)`，不把 animation 細節散落到每條台詞。
- 所有 override 都集中在少量 external properties，呼叫後統一 reset。
- 使用 alias/global/package 暫時控制 NPC，動作結束立刻清理。
- 提供 mod event，讓其他系統能在 hug 完成後反應。
- 用 MCM 控制 same-sex/opposite-sex、animation pool、camera、weapons、boon、welcome home。

對 Sofia 或自家 follower，如果要做擁抱、拍肩、親吻、喝酒、坐下、跳舞、安慰、調情等互動，建議做一個類似的 action quest script，而不是每個 topic 各自寫 animation fragment。

## 對 Sofia / roadmap 的意義

- Sofia 的 dialogue expansion 可以把「情緒節點」接到 action service：重逢、任務後安慰、表白、道歉、醉酒、營地互動。
- 任何會移動 actor 或播放 idle 的對話，都需要 GYH 這種 scene guard、IdleStop、DoNotMove、camera restore、cleanup。
- 如果直接相容 GYH，應確認 Sofia 是否已在 GYH form list/faction 裡，避免重複 patch。
- ModForge roadmap：需要能檢視 topic fragment 呼叫的 Papyrus function、quest script property、alias/package、mod event，否則只看 dialogue line 會漏掉真正行為。
