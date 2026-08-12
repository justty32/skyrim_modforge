# Ideas — 隨從 / NPC 互動

← [ideas 索引](ideas.md)

| § | 狀態 | 說明 |
|---|------|------|
| 1 | 現役 idea | 待選第一個 follower 與情境切片 |
| 1b | 主體已落地 | 只剩可選 CAMS 鏡頭 |
| 1c | MVP 已落地 | 新身份互動應另立 idea/roadmap |
| 17 | 現役 idea | 任務節點圖與批量反應 |
| 18 | 現役 idea | 持久經歷與對話更新 |

## 1. 擴充停止更新的隨從模組

許多高品質隨從模組已停更，想在其上擴充：

- 補日常對話與情境反應（旅途閒聊、特定地點/事件台詞）
- 深化與玩家互動（任務後感想、好感度觸發對話）
- 多隨從互相對話（A 評論 B、爭執、調侃）

**施工參考**：[`../sofia-patch/`](../../../sofia-patch/README.md)（獨立專案）→ [`follower-decode-2026-06-13.md`](../../../sofia-patch/reference/follower-decode-2026-06-13.md) — 解碼 Sofia Follower（30 quest / 28 scene / 1135 INFO / 57 GLOB）拆出五個可複用 pattern（小 controller-quest 星座、每情境一個 comment-scene、GLOB 狀態機、quest-stage+marker、玩家裝備驅動對話）+ ModForge 對應施工表。結論：Sofia 全是 ModForge 已落地能力的規模化組合，直接夠用。

**在場偵測**：不靠多隨從框架，用 vanilla 三層——同 Cell（已載入）→ `GetDistance < 2048`（夠近）→ `HasLOS`（看得見，按需）。「是否在隊」用 `IsPlayerTeammate()`，自訂跟隨機制才讀其 Quest。**已落地為 `MFSceneBanterController` autoStart**（見 CLAUDE.md）。

**Scene 前提**：Scene 的 Actor 必須是同 Quest 的 Alias（`ForceRefTo()` 填入；注意死亡/解散/未載入時的釋放）；輪詢用 `RegisterForSingleUpdate` 鏈式（勿用 OnUpdate 持續循環——存檔膨脹）。

**語音前提（所有對話想法共通）**：Skyrim 無語音檔的台詞字幕一閃即過。無語音時預設假設玩家裝 **Fuz Ro D'oh**。AI 語音合成（voice cloning）**已落地進 ModForge 本體（2026-06-10 起，進行中）**——spec `voiceTemplates[]`（f5 zero-shot / 微調 clone）+ `npcs[].voiceTemplate` + `voicelines` CLI（TTS → xwm → .fuz，含 .lip），見 `SPEC-workflow.md § Voice`。

---

## 1b. NPC 劇情演出（Scene 驅動）

特定時機（如玩家選某選項後）讓 NPC 完整演出：走到指定地點 ✅、播動畫 ✅（PlayIdle，限 vanilla 腳本跑過的 IDLE）、使用場景物件 ✅、NPC 間對話、可選鏡頭。**已落地見 CLAUDE.md；剩「附帶鏡頭」（Camera Shot CAMS）未做**——簡單演出不需要，之後再補。

---

## 1c. 多重身份 / 輕量職業系統

做某些事 → 取得身份（聖騎士/商人/冒險者/龍裔…）→ 賦予技能與常駐加成 + 解鎖專屬互動 → 互動回頭強化身份（近似 D&D 職業）。NPC 用「當前主身份」稱呼你；身份可疊加、主身份按優先序解析；取得走「讀書宣誓 / faction 會員」式。

設計見 `workflows/specs/archive/2026-06-06-identity-system-design.md`；**MVP 已落地**（plan `workflows/plans/archive/2026-06-07-identity-system-mvp.md`）。前置 PlayIdle scene-action 已落地。子專案切分：身份系統本體 → 身份對應互動（交易 UI、護衛任務…）。

---

## 17. Skyrim 原版任務節點圖 + 批量隨從反應生成（2026-06-15）

**2026-08-12 進度：schema 與 mechanical import 已落地。** `schemas/quest-node.schema.json` 定義 exchange contract；`questnodes` 由 QUST 非空 stage logs 生第一批 schema-valid nodes，`game-data/extract.sh` 寫到 `catalog/quest-nodes/`。機械 pass 不杜撰 branch/location/NPC，這些與 `unclassified` tags 是下一刀 AI semantic pass 的輸入。

**Idea**：把原版 Skyrim 的任務文本 / 對話全部抽出、由 AI 標記節點（「龍裔主線 MQ：殺了第一條龍」），再以已知隨從模組在這些節點的反應為例，批量 AI 生成新隨從在各節點的評論。

**兩步驟**：
1. **節點圖建構**：`gamedata` 已能 dump QUST/DIAL/INFO；加上 AI pass 標記每個 stage 的語義（「在做什麼、地點、NPC」），輸出結構化節點 JSON（`questId, stage, summary, location, npcs`）。
2. **反應批量生成**：以 Sofia / RDO / FCO 等已知隨從的節點反應為 few-shot 範例，LLM 為目標隨從在相同節點生成 ModForge spec INFO → 直接 build 出 ESP。

**ModForge 已有的基礎**：
- `gamedata dump` / `gamedata find` 可拉 QUST 記錄
- Sofia patch 已驗證「隨從對外部任務的節點反應」pipeline（stage-gate + INFO + voice + .seq）
- LLM → spec JSON → ESP 鏈已走通

**核心缺口**：① 節點語義標記（AI pass + 人工校正）；② 從第三方隨從 mod 萃取「既有節點反應樣本」作 few-shot。JSON schema 與 QUST mechanical extractor 已完成。

**關聯**：§18（記憶系統）是這個節點圖的消費端；§9（大量劇情生成）共用 LLM→spec pipeline。

---

## 18. 隨從記憶系統：任務經歷追蹤與對話更新（2026-06-15）

**Idea**：隨從記住玩家（和自己）的任務經歷，通關後改變平時對話——不是死板的「任務後一句評論」，而是持久改變隨從的對話庫（如通關暗影組織後，隨從偶爾提及暗殺技巧）。若隨從陪同通關，則有更豐富的「我們當時一起……」版本。

**兩個維度**：
- **玩家完成了什麼**：追蹤特定 quest stage（`QuestID + stage`）是否達到，更新一個「經歷集」GLOB 或 StorageUtil KV → 對話 condition 讀這個集合。
- **隨從在不在場**：`IsPlayerTeammate()` 在 quest stage 達到時記錄（存 faction 或 per-follower StorageUtil key）→ 在場版 INFO 有更親密的「我們當時……」文本。

**技術路線**：
- **追蹤層**：Story Manager `OnStage` 事件（或 Quest alias script `OnStageSet`）→ 寫 `StorageUtil.SetIntValue(follower, "did_mq_dragon", 1)`；或更輕量：直接 condition 讀 `GetStage(QUST)` ≥ N。
- **對話層**：Hello / Idle topic 加 condition `StorageUtil.GetIntValue(...)==1`，有兩套 INFO（隨從在場版 / 不在場版）；優先序管理（conditioned > generic）。
- **批量化**：配合 §17 的節點圖，可程式生成「每個重大節點 × 每個隨從 × 在/不在場」的 INFO batch，走 LLM→spec pipeline。

**難點**：① 追蹤時機（OnStageSet 需隨從有腳本，SM 版更通用但多一層）；② 隨從未在隊時仍要追蹤（需常駐 quest，不是隨從 alias 上的腳本）；③ INFO 量爆炸（10 任務 × 2 版本 × N 隨從 = 大量 record，需 batch 生成）。

**關聯**：§17 節點圖是輸入；§9 LLM pipeline 是生成端；Sofia patch 已有小規模先例（GLOB 狀態機 + conditioned Hello）；[[conditioned-hello-one-topic-many-infos]] 鐵律適用。
