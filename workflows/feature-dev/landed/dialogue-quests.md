# 已落地 — 對話 / 任務 / Story Manager / Scene

← [landed index](README.md)｜對應 [CODE_MAP.dialogue-quests](../../common/code-map/CODE_MAP.dialogue-quests.md)

**對話 / 任務 / Story Manager**
- **SM spec 管線**：`QuestSpec.storyEvent`(event+conditions) + `aliases`；build 自動生 SMBN→SMQN 掛原版根、清 StartGameEnabled。事件表 `StoryManagerEvents`（十個 engine-native 事件）。
- **alias fill 七種**：`fromEvent:<slot>` / `forced:<ref>` / `uniqueActor:<ref>` / `createObject:<ref>@<alias>` / `findMatching:closest|any` / **`findMatchingLocation:<locType>[@<parentAlias>]`（#7 radiant LocationAlias）** / **`findInLocationAlias:<locAlias>[#<LCRT>]`（#8 在地點內找 ref）**。後兩種＝radiant quest 生成根基（**離線實作 + build/validate 通、10 測綠，2026-06-17**；CK 語義 + 真 FormID 待主力機 xEdit 驗，見 WAIT_USER）。Mutagen shape 反射驗證＝`QuestAlias.Location=LocationAliasReference{AliasID,Keyword,RefType}`；**scope 校正**：#8 原議用 ALNA(`FindMatchingRefNearAlias`)，反射發現其 `TypeEnum` 只 `LinkedRefChild`（非地點內搜尋）→ 改走 `Location` 欄（其 `RefType`/LCRT 對 Location 型 alias 無意義 → 證明 Location 雙用於 Reference alias 的「在地點內找 ref」）。Missives 的 Hold→Dungeon→BossChest 鏈。example `examples/radiant_alias_spec.json`、測 `RadiantAliasTests.cs`。
- **可複用 trigger 庫（五入口，同一 `Fire()`）**：magic-effect / potion / activator / dialogue / alias-OnActivate。zip `~/skyrim_mods/ModForge{Magic,Potion,Activator,Dialogue,Alias}Trigger.zip`。通用派發器 `assets/papyrus/MFStoryEventDispatch`（embed 進 CLI）。
- **Quest 階段**：`StageSpec.startUpStage`（啟動自顯 objective）+ stage 推進（`MFSE_AdvanceStage.psc`）；alias 也適用一般（非 storyEvent）quest。
- **身份系統（輕量職業）Phase-2/C 完成**（in-game 確認 2026-06-07，`ModForgeIdentity.zip`）。**完整欄位/語意見 `SPEC-dialogue-quests.md`「Identities」、build wiring 見 `CODE_MAP.dialogue-quests.md`、建好的 esp 用 `identitydiag` 探。** 一句話地圖：
  - **取得**：讀書（`MFIdentityBook`）/ `default:true` 開局自動授（`MFIdentityDefault`）/ `autoGrantWhen{actorValue,threshold}` 玩家 AV 過門檻自動授（`MFIdentityAutoGrant`，如 Dragonborn `DragonSouls≥1`；純 Papyrus 讀 AV、免 SKSE）。
  - **閘**：`identity`（持有）/ `primaryIdentity`（主身份，由 `MFIdentityController` poll 算 `MF_PrimaryIdentity` GLOB、`setPrimaryIdentity` 對話可覆寫）；`activeWhen` 情境窄化正向閘。
  - **授予**：`grants`(SPEL) + `grantPerks`(PERK，如 smite-vs-undead 條件 perk)。
  - **互動**（多動作 TIF result fragment，皆純 record+生成、無 user script）：`hello` 招呼 / `openBarter` 交易 / `rewardItem` 獎勵 / `evaluateSpeakerPackages` 重評估 → 組出 Adventurer 護衛 quest（follow PACK gated on `GetStage`）。
  - **四個 reusable .psc**（Book/Default/Controller/AutoGrant）embed 進 CLI、Package 條件式出貨。
  - **踩坑**：`MFIdentityBook` 必 **extends ObjectReference**（OnRead 非 Book event）[[book-onread-needs-objectreference]]；state-varying 招呼=**一個 Hello topic 多條 INFO**（順序定優先，非多 topic 競 priority）[[conditioned-hello-one-topic-many-infos]]；NPC `autoCalcStats` 必配 `class` 否則 0 血倒地 [[autocalc-without-class-dead-npc]]；acquire scene 用 `beginOnQuestStart:false`（書 Start() 唯一觸發）。
  - **未做**：#3 聲望/行為追蹤（需先定設計）。

**Scene 劇情演出**
- **在場偵測 autoStart**：`SceneSpec.AutoStart`（triggerDistance/LOS/cooldown/poll/**brawlOnEnd**）+ 可複用 `MFSceneBanterController`。`ModForgeSceneBanter.zip`。
- **非對話 action**：beat phase + Package（走位，引用 `packages[]` 的 PACK）+ Timer（停頓）。`ModForgeSceneAction.zip`。
- **重播策略**：`playOnce` / `playHour(+tolerance)` / `gateGlobal`。`ModForgeReplayPolicy.zip`。
- **per-phase headtrack/facing**：`ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`。
- **scene 條件**：`SceneSpec.Conditions`（scene-level，僅 `beginOnQuestStart`）+ `ScenePhaseSpec.StartConditions/CompletionConditions`（per-phase）。
- **PlayIdle 演出**（in-game 確認 2026-06-07）：`SceneActionSpec.Idle`（IDLE ref）→ SCEN `SceneAdapter` per-phase OnStart fragment（`SF_<scene>.Fragment_<phase>` 跑 `<alias>.GetActorRef().PlayIdle()`，第三種 fragment 家族）。純產生器 `Generator.SceneFragments.cs`、掛載 `AttachSceneFragments`；idle action 同時發一個 Timer（hold）讓 phase 能 run。`find <esm> <kw> idle` 探 IDLE。`ModForgePlayIdle.zip`（宣誓鞠躬+獻手）。
