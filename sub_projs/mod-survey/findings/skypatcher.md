# Mod Survey — SkyPatcher（Nexus 106659，v6.4.1）

> ModForge 取向逆向調查：這是一個「策略級」runtime patcher，直接衝擊 ModForge 的產物策略（生成 `.esp` vs 生成 SkyPatcher config）。
> 來源：`SkyPatcher - AE-106659-6-4-1-1777328355.zip`（zip 只含 dll + ini + 空 config 資料夾；語法文件來自 GitHub repo `Zzyxz/SkyPatcher` + Nexus 文章）。
> 相關既有筆記：`followers-patch-and-mod-survey`、`common-framework-mods`、SPID（已在 common-framework-mods finding 中）。

---

## 一、這個 mod 做什麼 + 怎麼運作

SkyPatcher 是一個 **SKSE 外掛（CommonLibSSE-NG）**，讓作者／使用者**不需要 esp 插件就能批次修改既有 record 的欄位**。它讀取放在 `Data/SKSE/Plugins/SkyPatcher/<recordType>/` 資料夾中的 `.ini` 設定檔，在遊戲啟動時套用修改。

### 執行時序

從 `main.cpp` 分析，有**兩個掛鉤點**：

1. **`kDataLoaded`（資料載入後，遊戲進主選單前）**：讀取所有 ini 檔案，並對靜態資料型別（武器、防具、法術、種族、formlist、leveled list、容器等）立刻套用修改。這是「**load-time patch**」——遊戲啟動後一次性，儲存於記憶體中的 record 物件。

2. **`kPostLoadGame`（讀取存檔後）**：對所有當前已讀入的 Actor（NPC）重新套用視覺樣式、戰鬥風格、技能等。同時如果 `iUpdateNPC=1` 啟用，還會掛 `Load3D` hook，讓任何 NPC 3D 模型被載入時都自動套用最新的 NPC 修改。

### 三種更新模式（NPC 專屬）

| 設定 | 時機 | 說明 |
|------|------|------|
| `iUpdateNPC=1` | 每次 NPC Load3D | 動態：任何 NPC 進入渲染範圍都重新套用修改 |
| `iRefreshNPCStats=1` | 讀檔時 | 讀取存檔後重整所有 NPC 數值，免去重新建構 NPC |
| `iUpdateRefs=1` | 讀檔時 | 實驗性：對 REFR（放置物件）套用修改（仍開發中） |

### 不修改存檔、可熱移除

移除 ini 或 SkyPatcher 本身後**不留殘存資料在存檔中**——只影響記憶體中的資料，下次啟動重新計算。這與 esp override 不同（esp 的修改被序列化進 REFR/Actor 存檔資料）。

---

## 二、支援的 record 類型與 config 語法

### 2-A. 支援的 record 類型（主 ini 開關）

由 `SkyPatcher.ini` 的 `[Patcher]` 段與 config 子資料夾可列出共 **29 種 record 類型**：

| Record 類型 | 子資料夾 | 主要用途 |
|-------------|----------|---------|
| NPC_ | `npc/` | NPC 外貌、數值、技能、裝備、法術、派系 |
| ARMO | `armor/` | 防具屬性、biped slot、keyword |
| WEAP | `weapon/` | 武器數值、keyword、彈藥、特效 |
| SPEL | `spell/` | 法術屬性 |
| MGEF | `magicEffect/` | 魔法效果屬性 |
| LVLN/LVLI/LVSP | `leveledList/` | Leveled list 條目增刪 |
| FLST | `formList/` | FormList 條目增刪 |
| CONT | `container/` | 容器物品 |
| RACE | `race/` | 種族屬性、法術、移動 |
| FACT | `faction/` | 派系 |
| ENCH | `enchantment/` | 附魔 |
| PROJ | `projectile/` | 射彈 |
| BOOK | `book/` | 書本 |
| ALCH | `ingestible/` | 藥水/食物 |
| INGR | `ingredient/` | 鍊金素材 |
| MISC | `misc/` | 雜物 |
| AMMO | `ammo/` | 弓箭彈藥 |
| COBJ | `constructibleObject/` | 合成配方 |
| CELL | 無子資料夾（ini 開關） | 房間/地牢格 |
| ECZN | 無子資料夾 | Encounter Zone |
| LCTN | 無子資料夾 | Location |
| OTFT | `outfit/` | 套裝 |
| MOVT | `movementType/` | 移動類型 |
| SLGM | `soulGem/` | 靈魂石 |
| SCRL | `scroll/` | 卷軸 |
| OBME | `objectModification/` | Object Mod（OMOD） |
| REFR | 無子資料夾（實驗性） | 放置物件（Reference） |
| RACE（hook 版）| `raceHook/` | 種族 hook 版（特殊時序） |

### 2-B. ini 格式

每個 ini 檔放在對應子資料夾下（可再有子資料夾組織）。**一個 ini 可含多行**，每行是一條 patch 指令，用 `:` 分隔欄位：

```ini
; 格式：filter欄位=值:filter欄位=值:修改欄位=值:修改欄位=值...
filterByNpcs=AdrianneAvenicci:copyVisualStyle=Bijin_HousecarlSolitude:skin=Bijin_HousecarlSolitude
filterByRaces=NordRace:spellsToAdd=MagicResistance50:perksToAdd=HalfCostSpells
```

**Form 參照語法**（兩種格式均支援）：
- EditorID 直接寫：`filterByNpcs=AdrianneAvenicci`
- FormID 帶 plugin 名：`Skyrim.esm|1397D` 或 `MyMod.esp|0x807`

**條件性讀取**：ini 檔名包含 plugin 名（如 `Skyrim.esm.ini`）→ 只在該 plugin 載入時才讀這個 ini，否則略過。

**載入順序**：同資料夾內 ini 以英數字排序依序套用；後套用的覆蓋先套用的（同欄位衝突時）。

### 2-C. NPC 欄位完整清單（最常用）

**篩選器（filter）**：
```
filterByNpcs, filterByNpcsExcluded
filterByRaces, restrictToRaces
filterByKeywords, filterByKeywordsOr, filterByKeywordsExcluded
filterByFactions, filterByFactionsOr, filterByFactionsExcluded
filterByClass, filterByClassExclude
filterByCombatStyle, restrictToCombatStyle
filterByGender, restrictToGender
filterByModNames
filterByEditorIdContains, filterByEditorIdContainsOr, filterByEditorIdContainsExcluded
filterByDefaultOutfits
filterByPCLevelMult, filterByAutoCalc, filterByEssential, filterByProtected
restrictToVoiceType, restrictToMaleModelContains
restrictToFlags, restrictToTemplateFlags, restrictToKeywords
```

**視覺外觀**：
```
copyVisualStyle, setRandomVisualStyle, addRandomVisualStyle, rvsRestrictToTraits
skin, haircolor, race, weight, height, fullName
```

**數值／等級**：
```
setAutoCalcStats, setPcLevelMult, level, levelRange
calcLevelMin, calcLevelMax, changeStats, changeSkills
healthBonus, staminaBonus, magickaBonus, speedMultiplier
restrictToSkill, clearClassFromAttributes
```

**法術／技能**：
```
spellsToAdd, spellsToRemove
levSpellsToAdd, levSpellsToRemove
shoutsToAdd, shoutsToRemove
perksToAdd
```

**派系／keyword**：
```
factionsToAdd, factionsToRemove, factionsToAddRank1, factionsToAddRank2
keywordsToAdd, keywordsToRemove
```

**裝備／物品**：
```
objectsToAdd, ObjectsToRemove, objectsToRemoveNew
addOnceToInventory, removeInventoryObjectsByCount, removeInventoryObjectsByKeywords
outfitDefault, outfitSleep, deathItem
```

**行為**：
```
setAggression, setAssistance, setConfidence, setMood, setMorality
aggressionRadiusBehavior, aggressionRadiusRanges
voiceType, class, setRandomCombatStyle
```

**旗標**：
```
setFlags, removeFlags, setTemplateFlags, removeTemplateFlags
setEssential, setProtected, attackDataToAdd, attackDataToChange, attackDataToRemove
changeBaseObject
```

### 2-D. 其他 record 類型關鍵欄位

**防具（ARMO）**：
```
filterByArmors, filterByArmorsExcluded
filterByKeywords, filterByBipedSlots, filterByArmorTypes, filterByArmorAddons
filterByNameContains, filterByEditorIdContains, filterByModNames
damageResist, damageResistMatch, damageResistMultiply
health, value, valueMult, weight, weightMult
keywordsToAdd, keywordsToRemove, bipedSlotsToAdd, bipedSlotsToRemove
armorAddonsToAdd, armorAddonsToRemove, clearArmorAddons
fullName, armorType, modelMale, modelFemale
objectEffect, enchantAmount, equipSlot
reCalcArmorRating, reCalcArmorRatingv2, templateArmor
setFlags, removeFlags, restrictToKeywords
```

**武器（WEAP）**：
```
filterByWFlags, filterAnimationTypeOr, filterAmmos
attackDamage, attackDamageToAdd, attackDamageMult
critDamage, critDamageToAdd, critDamageMult, critPercentMult
speed, speedMult, reach, stagger, bashDamage
keywordsToAdd, keywordsToRemove, ammo, ammoList
overrideProjectile, aimModel, templateWeapon
value, valueMult, weight, weightMult
objectEffect, enchantAmount, setAnimationType, setSkillType
setFlags, removeFlags, mirrorWeapon
```

**FormList（FLST）**：
```
objects（目標 formlist）
objectsAdd（加入的 form 列表）
objectsRemove（移除的 form 列表）
formsToReplace, formsToReplaceWith（替換）
clear（清空再加）
modNames
```

**LeveledList（LVLN/LVLI）**：
```
filterByLists（目標 leveled list）
objectsToAdd, objectsToRemove, objectsToReplace
clear
modNames
```

**容器（CONT）**：
```
filterByContainers
addToContainers, addOnceToContainers
removeFromContainers, replaceInContainers
removeContainerObjectsByCount, removeContainerObjectsByKeywords
objectMultCount, clear
filterByEditorIdContains, filterByEditorIdContainsOr, filterByEditorIdContainsExcluded
```

**種族（RACE）**：
```
object, filterByEditorIdContains
baseMass, baseMassMult, baseCarryweight, baseCarryweightMult
startingHealth/Stamina/Magicka（+Mult）, regenHealth/Stamina/Magicka（+Mult）
damageUnarmed, reachUnarmed（+Mult）
heightMale, heightFemale, weightMale, weightFemale
keywordsToAdd, keywordsToRemove
spellsToAdd, spellsToRemove, levSpellsToAdd, levSpellsToRemove
shoutsToAdd, shoutsToRemove, attackRace
attackDataToChange, removeAttackData
```

---

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

## 六、參考連結

- Nexus 主頁：https://www.nexusmods.com/skyrimspecialedition/mods/106659
- GitHub 原始碼：https://github.com/Zzyxz/SkyPatcher
- Nexus 官方文章（需登入）：
  - NPC Patcher：https://www.nexusmods.com/skyrimspecialedition/articles/6092
  - SkyPatcher Information：https://www.nexusmods.com/skyrimspecialedition/articles/11194
  - ini 小撇步：https://www.nexusmods.com/skyrimspecialedition/articles/9850
  - 詳細使用指南：https://www.nexusmods.com/skyrimspecialedition/articles/9835
