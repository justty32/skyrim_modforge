# vs SPID + 對 ModForge 的參考價值 + esp vs config 策略

← [skypatcher](skypatcher.md)

## 三、SkyPatcher vs SPID 差異

SPID（Spell Perk Item Distributor）是另一個常見的「無 esp」分發工具，兩者**部分重疊、部分互補**：

| 比較面向 | SkyPatcher | SPID |
|---------|-----------|------|
| **核心定位** | 通用 record 欄位修改 | 專用法術/perk/道具/裝備分發給 NPC |
| **支援 record 類型** | 28+ 種（防具、武器、種族、leveled list、container…） | NPC、Actor（透過分發） |
| **NPC 法術分發** | ✓（`spellsToAdd`） | ✓（SPID 主打功能） |
| **NPC perk 分發** | ✓（`perksToAdd`） | ✓ |
| **NPC 外觀修改** | ✓（skin、copyVisualStyle、hair…） | ✗ |
| **防具/武器欄位修改** | ✓（傷害、重量、keyword…） | ✗（只能把 item 加入 NPC 裝備，不修改 item 本身） |
| **FormList 增刪** | ✓ | ✗（SPID 靠 FormList 過濾，但不能改 FormList 本身） |
| **Leveled List 增刪** | ✓ | ✗ |
| **種族屬性修改** | ✓（血量、體型、起始屬性…） | ✗ |
| **容器物品修改** | ✓ | ✗ |
| **NPC 等級修改** | ✓（level, levelRange, setPcLevelMult…） | ✗ |
| **SPID 過濾語法** | 無（SkyPatcher 用 filter 欄位） | 強（`ActorBase\|Outfit\|Keyword\|…\|Level\|…`） |
| **多對多分發（群體）** | ✓（filterByRaces/filterByKeywords） | ✓ |
| **熱移除不留存檔** | ✓ | ✓（SPID 也不寫存檔） |
| **執行時機** | DataLoaded（靜態）+ PostLoadGame/Load3D（NPC） | PostLoad（分發時機） |
| **SKSE 依賴** | ✓ | ✓ |

**結論**：SPID 在「把東西分發給 NPC」這件事上語法更豐富（可精確篩選等級範圍、faction rank、chance%）；SkyPatcher 在「修改 record 欄位本身」這件事上更通用（武器傷害、防具評級、種族屬性等）。**兩者可並用，無需二選一。**

---

## 四、對 ModForge 的參考價值（可生成 / 需新支援 / 純參考）

### 可生成（ModForge 現有能力涵蓋）

- **esp 方式的 record 修改**：對於 NPC、武器、防具的欄位修改，ModForge 現有 `BuildNpcs`、武器/防具 builder 已可生成 override esp——這些場景 SkyPatcher 是「替代品」而不是「必要品」。
- **FormList 操作**：ModForge 可生成 FLST record；SkyPatcher config 的 `objectsAdd/objectsRemove` 是它的替代路徑。（**推斷**：ModForge 是否有 FormList 增量 override 能力待確認。）
- **Leveled List 增刪**：同上，esp 方式可生成 LVLN/LVLI override。

### 需新支援（生成 SkyPatcher config 的新輸出管道）

- **SkyPatcher config 生成器**（整體新功能）：目前 ModForge 只輸出 `.esp`；若要支援「以 SkyPatcher config 取代 esp 相容 patch」這條路線，需要：
  1. 新增一種 output target（`SkyPatcherConfig` 或類似）
  2. 把現有 spec（NPC override、keyword 增刪、leveled list 注入）轉成對應的 ini 語法
  3. 確定輸出路徑：`Data/SKSE/Plugins/SkyPatcher/<recordType>/<mod名>/patch.ini`
  
  （**推斷**：src/ 中目前無此輸出路徑，需查 Generator 出口點才能確認工程量。）

### 純參考（了解生態，不必生成）

- **視覺替換類（copyVisualStyle / skin / setRandomVisualStyle）**：這些欄位修改的是 NPC 外貌，屬於「相容 patch / NPC 改外觀 mod」的領域，ModForge 不生成此類內容。
- **iUpdateNPC 動態更新機制**：這是 SkyPatcher runtime 特性，ModForge 不可控。

---

## 五、策略問題：esp vs SkyPatcher config

這是本次 survey 的核心問題。

### 情境分析

| 情境 | 建議產物 | 理由 |
|------|---------|------|
| **新增全新 NPC、地點、任務、對話** | **esp 不可替代** | SkyPatcher 只能修改既有 record，無法新增不存在的 record |
| **新增全新武器、法術、技能** | **esp 不可替代** | 同上，SkyPatcher 是 patcher 不是 creator |
| **對「多個 mod 的 NPC」加統一 keyword/perk/spell** | **SkyPatcher config 更優** | 一個 ini 行可篩選多個種族/keyword，比逐一 override esp 快，且無 esp slot 消耗 |
| **調整 vanilla 武器/防具數值（傷害、重量）** | **SkyPatcher config 更優** | 無需 esp 衝突、可熱移除、filter 批量套用 |
| **NPC 外觀替換（相容 patch）** | **SkyPatcher config 更優** | `copyVisualStyle` 是 SkyPatcher 最成熟的用例，已有大量 mod 採用 |
| **Leveled List 注入新物品到現有 LVLN** | **兩者皆可**（取決於場景） | SkyPatcher `objectsToAdd` 可做；但若已有 esp（含新物品 record），直接在 esp 裡 override LVLN 較簡單，不需要多一層 SkyPatcher 依賴 |
| **FormList 批量添加**（跨 mod 整合用） | **SkyPatcher config 更優** | 同 Leveled List 邏輯；條件性 ini（`PluginName.esp.ini`）可做到「只在某 mod 存在時才注入」 |
| **複雜條件觸發（Story Manager、Scene）** | **esp 不可替代** | SkyPatcher 無法生成 Quest、SM、Scene、Script 等邏輯型 record |
| **Papyrus 腳本邏輯** | **esp 不可替代** | SkyPatcher 不能 attach 腳本，只能改資料欄位 |
| **對已發布 mod 的相容 patch** | **SkyPatcher config 更優** | 製作者提供一個小 ini 取代過去的 esp 相容 patch，使用者免 merge；符合現在社群走向 |

### ModForge 產物策略建議

**ModForge 的核心是生成「有新內容」的 esp**——新 NPC、新任務、新法術、新地點。這些 SkyPatcher 做不到，esp 仍是主力。

**但在「相容 patch」與「批量欄位調整」這個次要場景上，SkyPatcher config 是值得支援的第二輸出路徑**：

1. **短期（不改架構）**：在 ModForge spec 系統中，對「addToFormList」、「addToLeveledList」、「addKeywordToNpcs」等操作，標記為「可選擇 SkyPatcher output 輸出」。讓使用者自己寫 ini，ModForge 在 spec 文件中給出範本語法即可。

2. **中期（新增生成器）**：為 `npc_patch`、`armor_patch`、`leveled_list_inject` 等場景型 spec 新增 `output: skypatcher` 選項，Generator 輸出 ini 而非 esp。適合「相容 patch 套件」或「全 load order 掃描式調整」。

3. **不建議完全放棄 esp**：SkyPatcher 無法做新增型 record（QUST、DIAL、SCEN、NAVM、NPC_[新建]等），esp 仍是 ModForge 的核心輸出，兩者應並存。

> ⚠️ 以上「需新支援」標記和策略建議均為**推斷**（未查 ModForge src/ 的 Generator 出口實現），需一次 code pass 校正實際工程量。

---

