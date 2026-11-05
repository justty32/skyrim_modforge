# CODE_MAP.dialogue-quests — Examples（對話・場景・quest）

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `fixtures/quest-nodes/*.json` | Quest-node schema fixtures: linear, branched, and converging MQ101 stages |
| `examples/dialogue_spec.json` | 單一 NPC 對話樹 |
| `examples/dialogue_conversation_spec.json` | 多輪對話（conversation 模式）|
| `examples/scene_spec.json` | 雙 NPC 場景（SCEN）|
| `examples/scene-presence-banter.json` | 在場偵測自動觸發 Scene（autoStart + MFSceneBanterController）|
| `examples/scene-action-performance.json` | Scene 非對話 action（§1b 演出）：beat phase + Package action（NPC 走位到 marker，Travel PACK）+ Timer action（停頓）|
| `examples/scene-sit-performance.json` | Scene 演出第二切片：NPC 走到椅子並**坐下**（SitTarget PACK，走位+坐合一）+ Timer + brawlOnEnd（SitTarget 細節見 [CODE_MAP.npcs-packages.md § AI Packages](../npcs-packages/packages.md#ai-packagespack)）|
| `examples/scene-playidle.json` | Scene 演出第三切片：actor 在 phase 邊界**播 IDLE 動畫**（`SceneActionSpec.Idle` → SCEN SceneAdapter phase fragment `SF_<scene>.Fragment_N`，跑 `<alias>.GetActorRef().PlayIdle()`）；站→鞠躬→獻手（用 vanilla `IdleSilentBow 0x0D8734` / `IdleGive 0x0B5E20`，限 vanilla 腳本 PlayIdle 過的 idle，跪/祈禱綁家具的不行）|
| `examples/scene-setstage.json` | 受限 scene phase fragment：phase OnStart 對綁定 quest 呼叫 `SetStage(stage)`；省略 quest 時用 scene host，不接受任意 Papyrus body |
| `examples/identity-paladin.json` | 輕量身份/職業系統 MVP：讀書(MFIdentityBook OnRead)→入 FACT+授常駐 ability+播宣誓鞠躬 scene(複用 PlayIdle)；Merchant toggle；NPC 依 `primaryIdentity` 改招呼(GetInFaction CTDA，高優先序排除)；**Adventurer baseline（`default:true`，開局自動授予 + 墊底招呼）**；**Paladin `activeWhen` 重甲才 active；對話選項 `setPrimaryIdentity` 手動覆寫主身份（controller）；#5a Merchant-only `openBarter` 交易（Townsfolk 為 vendor）；#5b Adventurer-only 護衛 quest（MF_Traveler follow PACK gated on GetStage、`rewardItem` 200 金）；#5c Paladin `grantPerks`→`MF_SmiteEvilPerk`（+25% vs undead/daedra）；Dragonborn `autoGrantWhen` DragonSouls≥1（priority 40，首吼後升主身份）**|
| `examples/scene-headtrack.json` | Scene 每-phase headtrack/facing：說話者 gaze 指向另一 actor／玩家／無人（`ScenePhaseSpec.HeadtrackActor`/`HeadtrackPlayer`/`FaceTarget`）|
| `examples/showcase-multi.json` | 多功能 showcase（一包一次測）：自訂 Light + headtrackPlayer + SitTarget beat + autoStart/brawl |
| `examples/scene-conditions.json` | Scene 條件閘：scene-level + per-phase start/completion CTDA（GetGlobalValue 等，refs by editorId）|
| `examples/showcase-multi2.json` | 多功能 showcase #2：firebolt PROJ/EXPL（spell tome 學）+ NPC 庫存武器 + scene 條件閘（GLOB gate）|
| `examples/scene-replay-policy.json` | autoStart 重播策略：`playOnce`（只播一次）/ `playHour`/`playHourTolerance`（到某遊戲時辰才播）/ `gateGlobal`（GLOB re-arm token）|
| `examples/quest_stages_spec.json` | quest stages + objectives + log entries |
| `examples/gather_quest_spec.json` | **#9 instanceGlobals：gather 型 radiant quest，startUpStage fragment 隨機 SetValue + UpdateCurrentInstanceGlobal 綁 instance，objective `<Global=...>` 顯示 per-instance 計數** |
| `examples/word_wall_spec.json` | word wall 觸發教字 |
