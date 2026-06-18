# 對 ModForge 的評估

← [constellations](constellations.md)

## 五、對 ModForge 的評估

### 5.1 可生成性

**MVP（純 esp + JSON + 薄 Papyrus）= 高可生成性。**

- JSON config 格式完全機器可生成：`id` / `name` / `description` / 三個 `form` 指向 GLOB / `experienceFormula` 五參數 / `nodes[]` 陣列（id + form + x/y + links）。
- `form` 字串的 load-order 無關性讓 generator 直接把「plugin 名 + 本地 FormId」填進去，不需額外查表。
- 三個 GLOB 是最簡單的 esp record，每棵技能只要三個。
- 訓練 TIF fragment 只有一行 Papyrus，可模板化。

**唯一超出純 esp 能力的**：Fortify-技能附魔/藥水需要 native SKSE plugin（`Constellations.dll` 那層）。這條屬進階加值，MVP 可不做。

### 5.2 需新增的支援

| 需求 | 說明 | 優先級 |
|------|------|--------|
| `skill.json` / `SKILLS.json` 生成器 | 把 spec 的技能樹規格序列化成 CSF JSON；包含 `$ref` 組裝 | **MVP 必要** |
| Translations 檔生成 | UTF-16 LE BOM + tab 分隔 + `$key\tValue` | **MVP 必要** |
| GLOB generator（帶 editor id） | 三個 per 技能；editor id 供 init script / console 引用 | **MVP 必要**（可能既有能力已支援，待確認） |
| init alias script 模板 | `CNS_InitScript` 的薄 Papyrus（`extends ReferenceAlias`；初始化 level GLOB；版本 gate） | **建議** |
| Fortify MGEF + AVG toml + native dll | 讓附魔/藥水對自訂技能生效 | ⚠️ 進階，超出純 esp；不在 MVP |

### 5.3 推薦 ModForge spec 欄位構想（⚠️ 推斷，非現況）

> ⚠️ 以下是基於 Constellations 實檔推斷的 spec 設計方向，**非目前 ModForge 已支援的語法**。

```jsonc
// PROPOSAL — 尚未實作，僅供後續 feature-dev 參考
{
  "customSkill": {
    "id": "BeastLore",
    "name": "$BeastLore_Name",
    "description": "$BeastLore_Description",
    "menu": "SKILLS",               // "SKILLS" = 住進原版頁；具名 = 獨立選單群組
    "insertAfter": "Block",         // SKILLS.json 排序提示
    "skydome": "DLC01/Interface/INTVampirePerkSkydome.nif",  // 省略可重用 vanilla
    "experienceFormula": {
      "useMult": 0.8,
      "useOffset": 27.0,
      "improveMult": 2.0,
      "improveOffset": 0.0,
      "enableXPPerRank": true
    },
    "nodes": [
      {
        "id": "Mastery",
        "perk": "BL_Mastery",        // 引用 spec 裡的 PERK EDID；FormId 由 generator 自動填
        "x": 0.0, "y": 0.0,
        "links": [ "Tracking", "Resilience" ]
      },
      { "id": "Tracking",   "perk": "BL_Tracking01",  "x": -1.2, "y": 1.0, "links": [ "Predator" ] },
      { "id": "Predator",   "perk": "BL_Predator01",  "x": -1.8, "y": 2.5 }
    ]
  }
}
// Generator 從這份 spec 自動產出：
//   - PERK records（沿用既有 perk 生成）
//   - 3 個 GLOB（level/ratio/legendary，自動 editor id）
//   - 2 個 KYWD（CustomSkillAdvance_BeastLore / CustomSkillBook_BeastLore）
//   - SKSE/Plugins/CustomSkills/MyMod/BeastLore.json
//   - SKSE/Plugins/CustomSkills/SKILLS.json（插入 "BeastLore" 到 Block 之後）
//   - Interface/Translations/MyMod_ENGLISH.txt（UTF-16 LE BOM）
//   - init alias script 模板（CNS_InitScript 精簡版）
```

### 5.4 一句話總結

