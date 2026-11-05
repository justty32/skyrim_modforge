# Idea #23 — 活世界人口框架：給每張捏臉一條命（living-adventurers） — 基礎、風險與下一步

← [入口](../living-adventurers.md)

## ModForge 已有的基礎 vs 缺口

**已有（memory 全有確認配方）**：dynamic spawn / MoveTo、radiant alias+package fill、StorageUtil KV、SM 觸發、對話 INFO+condition、faction、語音生成。任務層的 quest stage/objective/fragment、FLST、GLOB 量產、AI package、board container/activator —— 見 `../../analysis/mod-survey/findings/missives-modforge.md`,**ModForge 都能生**。

**缺口（前置工程,gating）**：
- **roadmap #7 LocationAlias fill / #8 nested ReferenceAlias fill / #9 `UpdateCurrentInstanceGlobal`** —— 隨機選任務目標地點的命脈,做任務層前必補。見 [mod-survey-gaps.md](../../roadmap/mod-survey-gaps.md)。
- **幽靈模擬 controller**（狀態機 + game-time timer）：手寫 .psc,比照 dispatcher/controller embed,生成器只負責 wire（memory `dispatcher-magic-trigger`）。
- **就地實體化 controller**（玩家進地點 → MoveTo 對的冒險者 + package）。

## 待證的可行性風險（idea 階段要先 spike）

1. **MoveTo 進出 churn**：玩家快速穿梭 cell 時的效能 + actor cleanup;persistent unique actor 的 package 在 MoveTo 後是否乾淨重掛。
2. **成敗骰平衡**：sim roll 的權重曲線（太強搶光玩家任務 / 太弱沒存在感）。
3. **玩家搶任務的雙向同步**：missive 是玩家也能接的真 quest,還是冒險者專屬的抽象任務?兩者如何對帳。
4. **storyLog → 傳唱對話**的條件爆炸（戰功組合 × NPC 類型）。
5. **與 follower 原 mod 共存**：standalone follower 自帶招募 quest + 常駐在某 cell。把他接進模擬＝劫持他的 ActorRef 去過冒險生活,但**不能破壞 vanilla 招募對話**。設計：玩家未雇用時過冒險者人生,雇用時回歸正常 follower（hire 互動本就在設計裡,天然對接）。follower mod 當 master（依賴它的 ActorRef FormID）—— ModForge 跨 master 引用 vanilla/外部 record 已是熟路（memory `vanilla-nif-paths-must-be-verified` / `esm-formid-access`）。

## 建議的下一步

idea → **先做一個最小 spike 證明模擬迴圈**（1 個具名冒險者：timer 推進抽象任務 + 玩家進酒館時 MoveTo 現身 + 一條傳唱對話），跑通再進 roadmap / spec。任務層的隨機地點要等 roadmap #7–9 補完,但**模擬迴圈本身不依賴它們**,可先獨立驗證。
