# Config：JSON 設定（CustomSkills/）

← [constellations](constellations.md)

## 二、Config 格式（完整語法 + esp Record 需求）

### 2.1 JSON 設定（`SKSE/Plugins/CustomSkills/`）

Constellations 是**現代 JSON 格式（CSF v2/v3）**的典型實作。它使用了**兩層 JSON 結構**：

#### `SKILLS.json`（Root，特殊檔名）

位置：`Data/SKSE/Plugins/CustomSkills/SKILLS.json`

`SKILLS.json` 是 CSF 唯一的特殊檔名：它直接**取代/擴充原版技能選單頁**。Root 物件（`CustomSkill.json` schema）：

```json
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/CustomSkill.json",
  "version": 1,
  "skydome": {
    "model": "Constellations/Interface/INTPerkSkydome.nif",
    "cameraRightPoint": 1
  },
  "skills": [
    "Enchanting", "Smithing", "HeavyArmor", "Block",
    { "$ref": "Constellations/HandToHand.json" },
    "TwoHanded", "OneHanded", "Marksman",
    { "$ref": "Constellations/Athletics.json" },
    "LightArmor", "Sneak", "Lockpicking", "Pickpocket", "Speechcraft",
    "Alchemy", "Illusion", "Conjuration", "Destruction", "Restoration", "Alteration",
    { "$ref": "Constellations/Sorcery.json" }
  ]
}
```

| 欄位 | 說明 |
|------|------|
| `version` | 常數 `1`（schema 版本，不是 API 版本；放在 root，**不在 skill 物件裡**） |
| `skydome.model` | 相對 `Data/Meshes/` 的 `.nif` 路徑；可重用 vanilla `DLC01/Interface/INTVampirePerkSkydome.nif` |
| `skydome.cameraRightPoint` | `1` = vanilla skydome 視角、`2` = beast skydome 視角 |
| `skills[]` | 陣列順序 = 選單排列順序；字串 = 原版技能枚舉；`{ "$ref": "..." }` = 自訂技能 |

#### `Constellations/<Skill>.json`（skill 物件）

以 `Athletics.json` 為例（完整欄位）：

```json
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/skill.json",
  "id": "Athletics",
  "name": "$Athletics_Name",
  "description": "$Athletics_Description",
  "level": "ConstellationsNewSkills.esp|012",
  "ratio": "ConstellationsNewSkills.esp|013",
  "legendary": "ConstellationsNewSkills.esp|014",
  "experienceFormula": {
    "useMult": 7.0,
    "useOffset": 0.0,
    "improveMult": 0.5,
    "improveOffset": 120.0,
    "enableXPPerRank": true
  },
  "nodes": [
    {
      "id": "Mastery",
      "perk": "ConstellationsNewSkills.esp|175",
      "x": -0.9,
      "y": 0.0,
      "links": [ "Warmth", "CombatRestore" ]
    },
    { "id": "Warmth",       "perk": "ConstellationsNewSkills.esp|188", "x": 0.7,  "y": 0.3  },
    { "id": "CombatRestore","perk": "ConstellationsNewSkills.esp|186", "x": -0.5, "y": 1.8,
      "links": [ "PredatorFriend", "Falling", "SprintEvade" ] },
    { "id": "SprintEvade",  "perk": "ConstellationsNewSkills.esp|168", "x": -0.6, "y": 3.5, "links": [ "SlowTime" ] },
    { "id": "SlowTime",     "perk": "ConstellationsNewSkills.esp|17E", "x": -1.2, "y": 5.2  }
    // …共 9 個節點
  ]
}
```

| 欄位 | 型別 | Constellations 實況 | 說明 |
|------|------|----------------------|------|
| `id` | string | `"HandtoHand"` / `"Athletics"` / `"Sorcery"` | Papyrus 訓練 TIF 引用；注意大小寫前後一致 |
| `name` | localizedString | `$Athletics_Name` | `$`-key → Translations 檔 |
| `description` | localizedString | `$Athletics_Description` | 同上 |
| `level` | form | `"ConstellationsNewSkills.esp|012"` | 存技能等級的 GLOB |
| `ratio` | form | `"ConstellationsNewSkills.esp|013"` | 存升級進度（0–1）的 GLOB |
| `legendary` | form | `"ConstellationsNewSkills.esp|014"` | 存 legendary 重置次數的 GLOB |
| `experienceFormula` | object | 三棵各不同 | 升級曲線五參數 |
| `nodes` | array | H2H 9 / Athletics 9 / Sorcery 9 個 | 最多 127；第一個是入口，必填 |

`form` 字串格式：`"PluginName.es[lmp]|FormId"`，FormId 3–8 位 hex，load-order 無關。

**Constellations 省略的可選欄位**（照常運作）：`showMenu` / `showLevelup` / `perkPoints` / `color` / `debugReload`——這些是「獨立選單群組 / 自訂點數池 / console 開選單」才需要的，走原版技能頁整合的情況下全部可以省略。

#### `experienceFormula` 三棵樹對比

| 技能 | useMult | useOffset | improveMult | improveOffset | enableXPPerRank |
|------|---------|-----------|-------------|---------------|-----------------|
| HandToHand | 0.8 | 27.0 | 2.0 | 0.0 | true |
| Athletics | 7.0 | 0.0 | 0.5 | 120.0 | true |
| Sorcery | 1.8 | 0.0 | 2.0 | 0.0 | true |

這組參數是「練多快 / 升多貴」的調校旋鈕，三棵樹刻意設計成不同曲線。

