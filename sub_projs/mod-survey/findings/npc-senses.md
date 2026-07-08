# NPC Senses（Nexus 178532 v1.0.0.1）— NPC 感知區可視化 + 進出事件派發（native DLL + JSON 規則）

← [survey index](../index.md)

| 項目 | 值 |
| --- | --- |
| 類型 | **系統 / 機制型（modder's resource / debug 工具）**，核心是 **native `NPCSenses.dll`（~1MB，CommonLibSSE-NG，SE/AE/VR/GOG）** |
| Plugin | `NPC Senses.esp`（646 bytes，僅 6 個 ACTI，master=Skyrim.esm）|
| 配置 | `Data/SKSE/Plugins/NPC Senses/Settings.json` + `Rules/*.json`（每 NPC 規則）+ 選用 `Language.json` |
| 依賴 | SKSE64 + Address Library（無 SPID/PapyrusUtil/JContainers/MCM）|
| 敘事價值 | 無 |
| **對 ModForge** | **純參考**（核心 native；無 GMST/SPID/perk 可等價生成），**無新缺口** |

> 來源：`NPC Senses-178532-1-0-0-1777483841.zip`。**無 README、無 ini、無 .psc**（只有 esp + dll + 6 個 nif）。以下機制由 DLL 匯出符號 / 字串 + esp dump 推斷，如實標註。作者疑為葡語系（log 為 pt-BR）。

---

## 一、它到底做什麼（別望文生義）

不是改偵測/潛行的 detection **平衡** mod，也**不碰任何 game setting / GMST / AVIF**。它是一個 **NPC 感知區的可視化 + 事件派發框架**：

- 對被規則選中的 NPC，在其身上 attach 兩種 3D 幾何：
  - **Vision**＝視線偵測**錐**（可選 line-of-sight，`useLineOfSight` / "Vision uses Line of Sight (LOS)"）
  - **Aura**＝近身**範圍球**（不需 LOS 的鄰近圈）
- 當有東西（actor／projectile／視 filter 而定）**進入/離開**某 NPC 的錐或球，DLL 對 Papyrus 廣播 SKSE mod event：
  - `NPCSenses_VisionEnter` / `NPCSenses_VisionExit`
  - `NPCSenses_AreaEnter` / `NPCSenses_AreaExit`
  → 任何 script 可 `RegisterForModEvent` 接收 → **這才是它的功能負載**：給模組作者一個「NPC 看到/靠近某物」的 hook，以及一個看得見的 debug 疊層。

換言之：**modder's resource + 除錯工具**（在世界裡畫出 NPC 的「感知範圍」並在進出時通知你），不是玩家向的平衡 mod。

---

## 二、實作核心

### esp（可生成，但無意義）
6 個 ACTI，各指向一個錐/球 nif，命名帶三段細節層級（LOD 或半徑檔位）：

```
VisionCMF  → NPCVision\NPCVision0.nif      AuraCMF  → NPCVision\NPCAura0.nif
VisionCMF2 → NPCVision\NPCVision1.nif      AuraCMF2 → NPCVision\NPCAura1.nif
VisionCMF3 → NPCVision\NPCVision2.nif      AuraCMF3 → NPCVision\NPCAura2.nif
```

DLL 執行期把這些 ACTI 當作 base，`PlaceAtMe`/attach 成節點掛到目標 NPC（符號 `VisionManager::ReattachNodes`、`SpawnedNodeData`、`visionNodes`/`auraNodes`）。esp 純粹當「幾何供應站」，零 script、零邏輯。

### DLL（純 native，不可生成）
- **`VisionRuleManager`**：`LoadRules()` / `SaveRules()` / `BuildDatabase()` — 從 `Rules/` 下多個 JSON 檔讀規則，建「受影響 NPC」資料庫（log：「Carregadas N 規ras de arquivos JSON separados」「Construindo banco de dados de NPCs afetados」「NPCs afetados: {}」）。規則 filter 維度由 `Manager::PopulateList<T>` 具現化推得：**`TESNPC` / `TESRace` / `BGSPerk` / `TESObjectACTI`**，加上字串欄 `FormID` / `GetFormEditorID` / `Name / EditorID` / `Race` / `col.formid`。
- **`VisionManager`**：`InitCachedForms()` / `ReattachNodes()` / `ClearActorMap()` / `LogTriggerDebug()` — 管理節點生成、LOS 判定、進出觸發。
- **`TriggerEventHandler::Register()`** + `MessagingInterface` / `PapyrusInterface` — 派發上述 4 個 mod event。
- **內建 ImGui 疊層**（非 MCM，非 Scaleform）：`igCollapsingHeader`、`igTextColoredV`、「Filter Name/EditorID」、「Enable All: Logs EVERYTHING that enters/exits (Actors, Projectiles, etc)」、「Vision Rules updated!」— 遊戲內即時**規則編輯器 + debug log**，改完 `SaveRules()` 寫回 JSON。
- 事件驅動（進出觸發），非被動 patch；規則資料庫在載入/更新時重建。

---

## 三、對 ModForge 的判定

**純參考。無新缺口。**

- **不像 SPID/SkyPatcher/EPW4NPCs 那類**：它**不分發** spell/perk/item，**不改** GMST/game setting，**沒有** ModForge 能等價生成的「spec 化」產物。功能全在 native LOS raycast / 節點 attach / mod-event 派發 / ImGui UI / JSON 規則引擎 — 這些 ModForge 一律生不出來。
- **唯一可生成的部分**（6 個 ACTI 指向 nif）ModForge 早已支援，但脫離 DLL 毫無作用 → 不值得生成。
- **不觸發任何 roadmap 缺口**：沒有 spec-shaped 的東西缺失；價值 100% 落在執行期 native。
- 概念旁註：它的「JSON 規則 filter（FormID/EditorID/Race/Keyword/Perk → 每-NPC 感知參數）」DSL 形狀近似 SPID `_DISTR.ini` / BOS `_SWAP.ini`。若哪天 ModForge 要當 **config emitter** 輸出 NPC Senses 規則檔，是個 ini/JSON-emitter 練習（同 SpidGen 類），但目前**無此需求**，不列缺口。
- **消費面用法**：ModForge 生成的 script 可 `RegisterForModEvent("NPCSenses_VisionEnter", ...)` 來讓生成內容對「被 NPC 看見/靠近」做反應 — 但這是**依賴它、非生成它**。

**對 Sofia**：一句話 — 可訂閱 `NPCSenses_VisionEnter` 讓隨從在「被 NPC 發現」時吐評論/反應，但純消費端 runtime hook，與生成無關。
