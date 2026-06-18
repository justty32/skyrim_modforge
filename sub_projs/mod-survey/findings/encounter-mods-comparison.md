# 兩個 mod 對比 + ModForge 擴充建議

← [encounter-mods](encounter-mods.md)

## 兩個 mod 對比 + ModForge 擴充建議

### 機制對比

| 面向 | IWE v3.6 | EE v1.6.7 |
|------|----------|-----------|
| SM branch 數 | 7 SMBN | 3 SMBN |
| SM quest-node 數 | 37 SMQN | 31 SMQN |
| Encounter quest 數 | ~100（新增）+ 4 vanilla override | WE164 + WI147 + LI14 |
| 演員來源 | 主要 LVLN（65 個），每次隨機 | 多數 vanilla named NPC，少量 LVLN（8 個） |
| 有 Scene（SCEN）| 是（56 個，含對白）| 否（0 個） |
| 有 Dialogue | 是（1409 個 INFO）| 否 |
| Source scripts 公開 | 否 | 是（~80 .psc） |
| DLC 依賴 | 全部（Skyrim + 3 DLC）| 僅 Skyrim.esm |
| SKSE 依賴 | 否 | 是（SE 觸發） |
| 動態 navmesh 定位 | 否（預擺 marker 或 alias-to-alias）| 是（NavmeshTester trick）|
| 地點類型過濾 | 是（LocTypeCity/Village/Tavern/…）| 是（22 種 LocType）|
| MCM 可調 | 否 | 是（per-encounter 開關 + Chance slider）|

### 設計哲學差異

**IWE** 是「**戲劇性小場景**」路線：每個遭遇都有完整的 Scene + Dialogue，演員有台詞，反應玩家狀態（穿什麼裝備、完成了什麼主線）。成本高（422 NPC + 1409 INFO），但體驗豐富，有「真人感」。

**EE** 是「**動態世界動起來**」路線：幾乎不用 Scene/Dialogue，靠 AI Package 讓 NPC 自然行動（行旅、戰鬥、待機）。NavmeshTester trick 讓遭遇能在玩家附近任意地形生成。MCM 高度可調，適合不喜歡 scripted 的玩家。

### ModForge 擴充優先順序

1. **alias fill from LeveledNpc（LVLN picker）**：兩個 mod 都用，IWE 更重度依賴。補上這個缺口讓 ModForge 能生成「每次演員不同」的遭遇，是 encounter generator 的核心。`Spec.Quest.cs` 的 alias fill 模式需新增 `fromLeveled` 型。

2. **Package/marker target alias indirection**：IWE 的 Travel package target = quest alias 的 marker ref。EE 的 NavmeshTester 動態移動 marker alias 後，package 同樣靠 alias indirection 跟著走。需讓 `packages[].travel.place` 支援 `{ alias: "TravelMarker1" }` 語法。

3. **NavmeshTester 動態 spawn Papyrus 樣板**（EE 專屬）：把 `EE_QF_EE_DynamicWE_010465F2` 那段腳本（隨機 ±6000 偏移 → EnableAI 吸 navmesh → MoveTo → Delete）做成 encounter-spawn 的 script 樣板，讓 spec 宣告 `spawnMode: "dynamicNavmesh"` 就能自動生成這段 Papyrus。

4. **SM branch/quest-node 多層分流 + 加權**（兩個 mod 共用）：目前 ModForge 能做「一個 quest 掛到 vanilla SM event root」，但無法建構「一顆 SMBN 底下掛多個 SMQN、每個 SMQN 掛多個候選 quest + 條件/權重」的選台機。這是兩個 mod 的核心組織方式，需要在 `Generator.Build.StoryManager.cs` 擴充。

5. **LocType keyword 路由 + Hold 偵測 alias**（兩個 mod 共用）：SMQN 的 LocType 條件 + LocationAlias 的 Hold 偵測是「地點感知遭遇」的關鍵，可以包裝成 encounter spec 的 `locationFilter: [LocTypeBanditCamp, LocTypeTown]` + `holdDetection: true` 高層語法。

6. **WITimeout 冷卻模式**（EE）：防 spam 的機制，讓 encounter generator 支援 `cooldownHours: 12` 之類的 spec 欄位，自動生成 Global + script 冷卻邏輯。

### 可複用設計模式總結

- **Random encounter table（LVLN list）**：IWE 的 65 LVLN + alias fill 是最正統的做法。
- **Level-scaled spawn**：`LeveledNpc` 的 chanceNone + entries[]（每個 entry 有 level 門檻）= 自動 level scaling。
- **Location-type filter**：SMQN 條件 + `LocType*` keyword 讓同一套框架輕鬆分地點類型。
- **Hold state context**：LocationAlias `myHoldImperial`/`myHoldSons` 讓遭遇感應世界政治狀態。
- **Zero-bloat cleanup**：演員 `DeleteWhenAble()`、marker `MoveToMyEditorLocation()` — 不留殘留，存檔不膨脹。
- **「骨架 quest 無 journal」**：SM 驅動的隱形遭遇 quest，不污染任務日誌。

---

### 相關既有調查

- [extended-encounters.md](extended-encounters.md)（更詳細的 EE 分析，含 record census 與完整 questdiag 輸出）
- [immersive-world-encounters.md](immersive-world-encounters.md)（更詳細的 IWE 分析 v2.3.1，含 scnscan/scenediag 輸出；本次手工分析的是 v3.6.1）
- 相關 memory：`story-manager-kill-recipe`、`dispatcher-magic-trigger`（SM 掛載）、`scene-playidle-recipe`（Scene/Package）、`sm-quest-journal-progression`（隱形 quest）、`conditioned-hello-one-topic-many-infos`（CTDA 對白）
