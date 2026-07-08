# Constellations 參考實作：現代 X.json schema

← [custom-skills-framework](README.md)

## 範例研究：Constellations（現代 JSON 格式參考實作）

「**Constellations - Additional Player Skills**」（Nexus 117352，本機解壓 v1.0.2）是 CSF 作者 Exit-9B 親自掛保證的「sophisticated example」，也是本調查唯一的**現代 JSON 格式實檔**——前面 §3/§4 的 VIGILANT/GLENMORIL 都還停在舊 INI 格式。它把原版 18 技能擴成 21：新增 **Hand-to-Hand（徒手）/ Athletics（運動）/ Sorcery（法術器具）** 三棵自訂技能樹，並**直接接進原版的技能選單**（按 ESC → Skills 就看得到，不需另開選單）。它 ship 的東西恰好是一整套「現代 CSF 技能該長怎樣」的權威範本：

```
ConstellationsNewSkills.esp                       ← PERK/GLOB/KYWD/MGEF 都在這
SKSE/Plugins/Constellations.dll                   ← 本 mod 自己的 SKSE plugin（非 CustomSkills.dll）
SKSE/Plugins/CustomSkills/SKILLS.json             ← 特例：覆寫原版技能選單
SKSE/Plugins/CustomSkills/Constellations/HandToHand.json / Athletics.json / Sorcery.json
SKSE/Plugins/ActorValueData/Constellations_AVG.toml
Interface/Translations/ConstellationsNewSkills_ENGLISH.txt
Source/Scripts/CNS_*.psc                           ← Init / ModObjects / 七支訓練 TIF fragment
```

### 6.1 完整的現代 `X.json` schema（逐欄位）

以實檔 `Constellations/HandToHand.json` 為代表（這就是一個 `skill.json` 物件，被 `SKILLS.json` 用 `$ref` 內嵌）：

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/skill.json",
  "id": "HandtoHand",                              // 技能唯一 ID；Papyrus / 訓練選單都用它引用
  "name": "$HandToHand_Name",                      // localizedString，$-key → Translations 檔
  "description": "$HandToHand_Description",
  "level":      "ConstellationsNewSkills.esp|00F", // GLOB：目前技能等級
  "ratio":      "ConstellationsNewSkills.esp|010", // GLOB：距下一級進度 0–1
  "legendary":  "ConstellationsNewSkills.esp|011", // GLOB：legendary 重置次數
  "experienceFormula": {                           // 升級曲線
    "useMult":     0.8,                             // 每次 AdvanceSkill 的 XP = useMult*量 + useOffset
    "useOffset":   27.0,
    "improveMult": 2.0,                             // 升一級所需 XP 隨等級的成長係數
    "improveOffset": 0.0,
    "enableXPPerRank": true                         // true = 用 per-rank 累積制（原版式）
  },
  "nodes": [                                        // perk 樹節點；陣列第一個是入口，最多 127 個
    {
      "id": "Mastery",                              // 給 links 引用的節點名（字串）
      "perk": "ConstellationsNewSkills.esp|16F",    // form：本節點的 PERK（多階只填第一階）
      "x": 0.4,                                      // 佈局座標（x 正方向朝左、y 正方向朝上）
      "y": 0.0,
      "links": [ "UnarmedSpeed", "DamageAttack" ]    // 連到的子節點，用 id 字串（或 1-based 索引）
    },
    { "id": "UnarmedSpeed", "perk": "...|165", "x": -0.3, "y": 0.8, "links": [ "DualUnarmedSpeed", "Danger" ] },
    { "id": "PowerKnockdown", "perk": "...|17C", "x": -1.4, "y": 3.5 }   // 末端節點：無 links
    // …共 9 個節點
  ]
}
```

逐欄位重點（對照 §2.1 的 schema 定義，這裡是「實況」）：

| 欄位 | Constellations 實況 | 備註 |
|------|----------------------|------|
| `version` | **不在 skill 物件裡**，而在 `SKILLS.json` root（`version: 1`） | `version` 是 root（`CustomSkill.json`）欄位，不是 skill 欄位；schema 版本常數，非 API 版本 |
| `id` | `"HandtoHand"`/`"Athletics"`/`"Sorcery"` | 注意大小寫：JSON 裡是 `HandtoHand`，但訓練 TIF 卻呼叫 `"HandToHand"`（見 §6.3，疑似容錯/筆誤，仍運作） |
| `name`/`description` | `$`-key | 真文字在 Translations（§6.4） |
| `level`/`ratio`/`legendary` | 三個都填，連號 `00F/010/011`、`012/013/014`、`015/016/017` | **三棵樹各只用這三個 GLOB**；`showMenu`/`showLevelup`/`perkPoints`/`color`/`debugReload` **全部省略** |
| `experienceFormula` | 三棵各不同（H2H useMult 0.8/useOffset 27；Athletics useMult 7.0；Sorcery useMult 1.8） | 證明這組參數就是調整「練多快/升多貴」的旋鈕 |
| `nodes` | H2H 9 / Athletics 9 / Sorcery 9 個 | 入口節點 id 慣例叫 `"Mastery"`；`x`/`y` 是**浮點自由佈局**（不像舊 INI 還有 `GridX`/`GridY` 整數欄） |

> 關鍵觀察：**現代 JSON 的 node 沒有 `GridX`/`GridY`**（那是舊 INI 的東西），只有浮點 `x`/`y` + `links`，渲染器自己排。`perk` 字串 `"ConstellationsNewSkills.esp|16F"` 即 §2.1 的 load-order 無關 `form` 格式，FormId 用 3 位 hex（`00F`）也合法。

### 6.2 `SKILLS.json` 特例

`SKILLS.json` 是 CSF 唯一被特殊對待的檔名：它**不是另開一個選單群組，而是直接取代/擴充原版技能選單那一頁**。Constellations 的 `SKILLS.json`（root 是 `CustomSkill.json` schema，故有 `version`/`skydome`/`skills`）：

```jsonc
{
  "version": 1,
  "skydome": { "model": "Constellations/Interface/INTPerkSkydome.nif", "cameraRightPoint": 1 },
  "skills": [
    "Enchanting", "Smithing", "HeavyArmor", "Block",
    { "$ref": "Constellations/HandToHand.json" },   // ← 自訂技能用 $ref 內嵌
    "TwoHanded", "OneHanded", "Marksman",
    { "$ref": "Constellations/Athletics.json" },
    "LightArmor", "Sneak", "Lockpicking", "Pickpocket", "Speechcraft",
    "Alchemy", "Illusion", "Conjuration", "Destruction", "Restoration", "Alteration",
    { "$ref": "Constellations/Sorcery.json" }
  ]
}
```

要點：
- `skills[]` 把 **20 個原版技能（字串列舉）** 和 **3 個自訂技能（`{ "$ref": "…" }` 指向獨立檔）** 混排在同一份清單裡——**陣列順序就是選單裡的排列順序**，所以三棵新樹被插在語意相近的原版技能旁（H2H 接 Block 後、Athletics 接 Marksman 後、Sorcery 壓軸）。
- `$ref` 機制讓每棵技能樹各自存成乾淨的 `Constellations/<Skill>.json`，`SKILLS.json` 只做組裝。命名子資料夾 `CustomSkills/Constellations/` 是慣例（避免與別的 mod 撞檔名）。
- 自帶 `skydome.model` 指到 mod 自己的 `INTPerkSkydome.nif`（21 技能的新星圖），`cameraRightPoint: 1` = vanilla skydome 視角。
- **對比「具名 `X.json`」**：若檔名不是 `SKILLS.json`（如 §3 的 `CustomSkill.VigPious` 對應的現代 JSON 會叫 `VigPious.json`），它就是一個**獨立選單群組**，原版選單看不到，必須靠 `CustomSkills.OpenCustomSkillMenu("Id")` 或 console 才開得起來。Constellations 走 `SKILLS.json` 路線，所以它的技能「住在原版技能頁裡」，玩家無感接軌。

