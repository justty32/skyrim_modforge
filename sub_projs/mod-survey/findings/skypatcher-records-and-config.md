# 支援的 record 類型與 config 語法（主表 + ini + NPC 欄位）

← [skypatcher](skypatcher.md)

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

