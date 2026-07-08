# Step 3–4：寫 X.json + 掛進選單

← [custom-skill-tree-guide](README.md)

## 5. Step 3 — 寫 `<X>.json`（逐欄帶寫）

這是核心。我們照 Constellations 的 `HandToHand.json` 逐行改編成 `BeastLore.json`。建一個檔：

`Data/SKSE/Plugins/CustomSkills/MySkills/BeastLore.json`

（子資料夾 `MySkills/` 是慣例，避免和別的 mod 撞檔名。）

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/skill.json",
  "id": "BeastLore",                          // 技能唯一 ID；Papyrus / 訓練選單 / console 都用它引用
  "name": "$BeastLore_Name",                  // localizedString：$-key → Translations 檔（Step 6）
  "description": "$BeastLore_Description",
  "level":      "MySkills.esp|800",           // form：指向 4.2 的 level GLOB（"Plugin.esp|FormId"）
  "ratio":      "MySkills.esp|801",           // form：ratio GLOB
  "legendary":  "MySkills.esp|802",           // form：legendary GLOB
  "experienceFormula": {                      // 升級曲線——五個調校鈕
    "useMult":      0.8,                       // 每次 AdvanceSkill 得到的 XP = useMult * 量 + useOffset
    "useOffset":    27.0,
    "improveMult":  2.0,                       // 升一級所需 XP 隨等級成長的係數
    "improveOffset": 0.0,
    "enableXPPerRank": true                    // true = per-rank 累積制（原版式）
  },
  "nodes": [                                  // perk 樹節點；陣列第一個是入口，最多 127 個
    {
      "id": "Mastery",                         // 給 links 引用的節點名
      "perk": "MySkills.esp|D65",              // form：本節點的 PERK（多階只填第一階）
      "x": 0.0,                                // 佈局座標（x 正向朝左、y 正向朝上）
      "y": 0.0,
      "links": [ "Tracking", "Resilience" ]    // 連到的子節點（用 id 字串，或 1-based 索引）
    },
    { "id": "Tracking",   "perk": "MySkills.esp|D66", "x": -1.2, "y": 1.0, "links": [ "Predator" ] },
    { "id": "Resilience", "perk": "MySkills.esp|D67", "x":  1.4, "y": 1.0, "links": [ "ThickHide" ] },
    { "id": "Predator",   "perk": "MySkills.esp|D68", "x": -1.8, "y": 2.5 },   // 末端：無 links
    { "id": "ThickHide",  "perk": "MySkills.esp|D69", "x":  2.0, "y": 2.5 }
  ]
}
```

**逐欄要點**：

- **`version` 不在這裡**。`version: 1` 是 root（`CustomSkill.json` / `SKILLS.json`）的欄位，不是 skill 物件的欄位。
- **`id`**：給 Papyrus / 訓練選單 / console 引用。提醒：Constellations 的 JSON 寫 `"HandtoHand"`（小寫 t）但訓練 TIF 卻呼叫 `"HandToHand"`——疑似容錯/筆誤仍可運作，但**你自己務必前後一致**，別賭它。
- **`name`/`description`**：以 `$` 開頭即翻譯 key（推薦）；不以 `$` 開頭視為直接字面值（deprecated）。
- **`level`/`ratio`/`legendary`**：值是 `form` 字串 `"PluginName.es[lmp]|FormId"`，指向你在 4.2 建的三個 GLOB。FormId 可 3–8 位 hex、可選 `0x` 前綴（範例 `D65`/`800` 都合法）。**這是 load-order 無關的**——CSF runtime 用 plugin 名 + 本地 FormId 查表，不受載入順序索引影響。這也是它與 ModForge FormId 配置流程天然契合的原因。
- **`experienceFormula`**：五個旋鈕就是「練多快 / 升多貴」。三棵真實技能各不同（H2H `useMult:0.8 useOffset:27`；Athletics `useMult:7.0 improveOffset:120`；Sorcery `useMult:1.8`），證明這組參數就是調曲線用的。先抄一組能動的再微調。
- **`nodes`**：第一個元素是入口（即使技能沒有 perk，**第一個 node 仍必填**）。每個 node：`id`（可選，給 links 用）、`perk`（必填 form）、`x`/`y`（必填浮點）、`links`（可選）。

---

## 6. Step 4 — 掛進選單

兩條路，二選一。

### 路線 A（推薦）：用 `SKILLS.json` 住進原版技能頁

`SKILLS.json` 是 CSF **唯一被特殊對待的檔名**：它直接取代/擴充原版技能選單那一頁，玩家按 ESC → Skills 就看得到，無感接軌。

`Data/SKSE/Plugins/CustomSkills/SKILLS.json`：

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/Exit-9B/CustomSkills/main/docs/schema/CustomSkill.json",
  "version": 1,                               // root 欄位：schema 版本常數
  "skydome": {                                // 背景星圖；可重用 vanilla 預設免自製
    "model": "DLC01/Interface/INTVampirePerkSkydome.nif",
    "cameraRightPoint": 2                      // 1=vanilla skydome 視角、2=beast skydome 視角
  },
  "skills": [
    "Enchanting", "Smithing", "HeavyArmor", "Block",
    "TwoHanded", "OneHanded", "Marksman",
    "LightArmor", "Sneak", "Lockpicking", "Pickpocket", "Speechcraft",
    "Alchemy", "Illusion", "Conjuration", "Destruction", "Restoration", "Alteration",
    { "$ref": "MySkills/BeastLore.json" }      // ← 自訂技能用 $ref 內嵌，放你想要的位置
  ]
}
```

要點：
- **`skills[]` 把原版技能字串列舉與自訂技能 `{ "$ref": "…" }` 混排**，**陣列順序 = 選單裡的排列順序**。把 `$ref` 插在語意相近的原版技能旁邊（Constellations 就把 HandToHand 接在 Block 後、Sorcery 壓軸）。
- 原版技能名（`Alchemy`、`Destruction`、`OneHanded`、`Marksman`、`VampirePerks`、`WerewolfPerks` … 共 20 個）是字串列舉，直接列。**只列你要顯示的**——上面省略某個原版技能，那頁就不顯示它。
- `$ref` 讓每棵樹各自存成乾淨的 `MySkills/<Skill>.json`，`SKILLS.json` 只做組裝。
- `skydome` 可指你自己的 `.nif`，或像上面重用 vanilla 的 `INTVampirePerkSkydome.nif`（免自製）。

### 路線 B：獨立選單群組

若檔名**不是** `SKILLS.json`（例如就叫 `BeastLore.json` 放在 `CustomSkills/` 根、自帶 `version`/`skills`），它就是一個**獨立選單群組**，原版技能頁看不到，必須靠 `CustomSkills.OpenCustomSkillMenu("BeastLore")` 或 console 才開得起來。VIGILANT/GLENMORIL 走這條（自帶 `showMenu` GLOB + 觸發腳本）。MVP 建議走路線 A。

---

