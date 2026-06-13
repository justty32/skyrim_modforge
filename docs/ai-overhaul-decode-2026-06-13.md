# AI Overhaul 解碼分析（2026-06-13）— NPC 日程 / vanilla NPC override 參考

從 `AI Overhaul.esp`（v1.9，2.2MB，MO2 已解出、直接 overlay 讀）拆解。NPC AI 行為 overhaul 的代表作。

**一句話結論**：AI Overhaul = **ModForge 已有的 10 個 PACK 模板,規模化套到 424 個具名 vanilla NPC 上**（日程堆疊）。ModForge 已能生成同類 package stack;唯一**缺口**是「override 既有 vanilla NPC」——ModForge 目前只建**新** NPC,不改既有 NPC 的 package 清單。

## 記錄普查
| 群組 | 數量 | 說明 |
|------|------|------|
| **Npcs** | **424** | 全是 override（具名 vanilla NPC：Addvar/Beirand/Bryling/Erikur/VittoriaVici…，城鎮居民為主）|
| **Packages** | **744** | AI 行為包,平均 ~1.75/NPC（單 NPC 的 package 清單 4–8 個）|
| Quests | 12 | 多為 `Dialogue<City>` vanilla 對話 quest override（配合新日程修對話條件）+ MG01 + RelationshipMarriageFIN |
| Factions | 3 | `ServicesWhiterunCarlotta`（營業時段）/ `AIOAdriannePlayerUseFaction`（玩家可用打鐵）/ `AIOHunterFaction` |
| Worldspaces | 7 / Cells 10 / Idles 6 / FormLists 5 / DialogINFOs 20 | 多為 cell/worldspace override 放 sandbox/travel 標記 |

Masters：Skyrim.esm + Update + Dawnguard + HearthFires + Dragonborn + ccFish + **USSEP**（疊在 USSEP 上）。

## 核心 pattern：每 NPC 一個「日程堆疊」

**package 模板分布**（vanilla PackageTemplate FormKey → 用量）：
| Template | 用量 | 對應 ModForge PACK 模板 |
|----------|------|----------------------|
| `Skyrim.esm:0x01C254` Sandbox | **202** | `sandbox`（在區域內遊蕩,broad fallback）|
| `Skyrim.esm:0x019714` Eat | 84 | `eat`（用餐時段吃飯）|
| `Skyrim.esm:0x019717` Sleep | 45 | `sleep`（夜間睡覺）|
| 0x068B86 / 0x079ABF / 0x016FAA / 0x019715 / 0x0604EE / 0x0283F0 / 0x017723 / 0x06C873 … | 各 13–26 | travel / sit-target（坐家具）/ use-item-at / services 變體 → `travel`/`sittarget`/`activate` |

**堆疊配方**（順序重要,與 ModForge「具體在前、broad sandbox fallback 在後」鐵律一致）：
```
[時段/地點具體包]  eat(8/13/19點) → sleep(22–6點) → sit/use-furniture(在攤位/工作台) → travel(去市場/教堂)
[broad fallback]   sandbox(整個 cell 區域遊蕩)
```
424 個 NPC 各被換上這樣一疊,daily life 才生動。

**營業時段 vendor**：`ServicesWhiterunCarlotta` faction + 在攤位的 sandbox/sit 包 + 時段條件 → 商人只在白天攤位營業（對應 ModForge vendor 功能可借鏡）。

## 對 ModForge 的意義

| 觀察 | ModForge 現況 |
|------|--------------|
| Sandbox/Eat/Sleep/Sit/Travel package stack | ✅ 10 個 PACK 模板全有（sandbox/sleep/travel/usemagic/patrol/follow/escort/sittarget/activate/eat）|
| 「具體時段包在前、sandbox fallback 在後」 | ✅ 已是鐵律（`npcs[].packages` 順序）|
| 為**新** NPC 排日程 | ✅ `NpcSpec.Packages` |
| **override 既有 vanilla NPC 的 package 清單** | ❌ **缺口** — ModForge 只建新 NPC,不改既有 NPC |

### 路線圖點子：vanilla NPC AI patch
AI Overhaul 的本質是「拿既有 NPC、換掉它的 packages」。ModForge 若想做同類「給某村莊 NPC 加日程」的 patch,需要新能力：
- spec 給 **vanilla NPC ref**（`<master>:0xFORMID`）+ 一疊 package（in-spec PACK 或 vanilla template）
- build 期 `GetOrAddAsOverride` 該 NPC、覆寫 `Packages` 清單（疊我們的包 + 視需要保留原本）
- 注意 USSEP/其他 mod 對同 NPC 的 override 衝突（load order）

這和現有 vanilla-cell override（CopyCellEnv）、vanilla-worldspace override 同類:**override 既有記錄、只改少數欄位**。是 ModForge 從「生新內容」跨到「patch 既有世界」的一步。

## 解碼方法備忘（記憶體安全）
`AI Overhaul.esp` 2.2MB,MO2 已解出於 `~/games/.../mods/AI Overhaul SSE/`,直接 `CreateFromBinaryOverlay`（lazy）讀安全。**未載 master**（要解 package template 的 editorId 需載 250MB Skyrim.esm,記憶體邊界,故只標已知的 Sandbox/Eat/Sleep,其餘以類別概括）。
