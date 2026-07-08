# Step 2：在 esp 裡建記錄

← [custom-skill-tree-guide](README.md)

## 4. Step 2 — 在 esp 裡建記錄

這一步全在 SSEEdit/CK 裡做。下面逐種記錄說明要填什麼。

### 4.1 PERK records（每個節點一個）

技能樹的每個節點 = 一個普通 PERK record，**和原版 perk 完全一樣**——這正是 ModForge 既有 perk 能力能直接重用的原因。

兩類常見 perk：
- **entry-point perk**（「Modify Skill Use」「Mod Attack Damage」之類）：透過 perk entry-point 改變遊戲行為。
- **ability perk**（掛一個 abilities/spell 的被動 MGEF）：給玩家一個常駐被動效果。

每個 PERK 要填：
- **EDID**（Editor ID）：用你自己的前綴，例如 `BL_Mastery`、`BL_Tracking01`。
- **FULL**（Name）/ **DESC**（Description）：這是 perk 在選單裡顯示的名字與說明。**這層的在地化走 esp**（見 Step 6），與技能名/述（JSON 那層）是兩套獨立通道。
- **Perk Sections / entry points 或 abilities**：perk 的實際效果。
- **多階 perk**：若一個節點要有 4 階，建 4 個 PERK record，用 `Next Perk` 串起來，node 只填第一階。

> **地雷（必看）**：entry-point perk 在載入時如果 `PerkConditionTabCount` 那個 byte 是 0 會直接 CTD。請設成 vanilla canonical 值。這是 ModForge 既有筆記 [perk-conditiontabcount-ctd](記憶) 記錄過的坑——做 entry-point perk 時務必比對。

### 4.2 三個 GLOB（GlobalVariable）——每棵技能一組

CSF 用 global variable 存技能的狀態。**實證最小只需三個 per skill**（Constellations 連 `showMenu`/`showLevelup`/`perkPoints`/`color`/`debugReload` 都沒做，照常運作）：

| GLOB | 命名慣例 | 型別 | 存什麼 |
|------|----------|------|--------|
| level | `Skill<X>Level`（如 `SkillBeastLoreLevel`） | Short/Float | 目前技能等級 |
| ratio | `Skill<X>Ratio` | Float | 距下一級的進度（0–1） |
| legendary | `Skill<X>Legendary` | Short | 歸零成 legendary 的次數 |

每個 GLOB：
- 一定要給 **editor id**（console 指令與 init script 都靠它引用）。
- level 初值建議 0，由 init script（Step 5）設成 `iAVDSkillStart`（=15）。

> 可選的額外 GLOB（只有走「獨立選單群組」或要自訂點數池時才需要）：`showMenu`（值 0，外部設 1 開選單）、`showLevelup`（值 0，設 1 顯示升級訊息）、`perkPoints`（自訂點數池，不設則用玩家標準 perk point）、`color`（技能名 RGB）、`debugReload`。MVP 不用做這些。

### 4.3 升級掛鉤 KYWD（Keyword）

三個慣例命名 keyword，後綴是技能 id（`CustomSkill<Type>_<Id>`）：

| KYWD（EDID） | 貼在哪 | 作用 |
|--------------|--------|------|
| `CustomSkillAdvance_BeastLore` | perk 的「Modify Skill Use」entry-point 條件上 | 改變升級速度（取代原版 `EPModSkillUsage_IsAdvanceSkill`），配 `EPModSkillUsage_AdvanceObjectHasKeyword` 條件 |
| `CustomSkillBook_BeastLore` | 一本 BOOK 上 | 首次閱讀即推進此技能（像原版技能書） |
| `CustomSkillWorkbench_BeastLore`（可選） | 一個 constructible workbench 上 | 在該處製作即給此技能 XP |

Constellations 做了 `CustomSkillAdvance_*` 與 `CustomSkillBook_*`，**沒做** `CustomSkillWorkbench_*`（那三棵技能不走製作台路線）。你按需取捨。

### 4.4 可選：Fortify-技能 MGEF（進階）

若要支援「強化野獸學識」這種附魔/藥水，得做一組 fortify-skill MGEF + 一份 `ActorValueData/<Mod>_AVG.toml`，把自訂技能**借用一個閒置的原版 ActorValue 槽**當載體（原版引擎只認固定 AV 列舉，自訂技能沒有自己的 AV）。

**這條需要一個你自寫的 native SKSE plugin（像 Constellations 的 `Constellations.dll`）來讀那份 toml——純 esp 做不到。** 預設別做，當作未來擴充。細節見 survey §6.3。

---

