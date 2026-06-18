# 其他 record 類型關鍵欄位

← [skypatcher](skypatcher.md)

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

