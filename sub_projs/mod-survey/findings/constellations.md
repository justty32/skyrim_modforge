# Constellations — Additional Player Skills 技術調查

> 調查對象：**Constellations - Additional Player Skills**（Nexus 117352，v1.0.2）
> 本機解壓：`~/skyrim_mods/unzip/Constellations-117352-1-0-2-1730665883/`
> 目的：理解 Constellations 的技術架構，與 CSF 做比較，評估對 ModForge 技能樹生成路線的影響。
> 慣例：散文用繁體中文，config 欄位 / record type / API 名稱 / FormId 保留 English。
> 姊妹文件：[custom-skills-framework.md](../custom-skills-framework.md)（CSF 框架深挖）、[custom-skill-tree-guide.md](../custom-skill-tree-guide.md)（實作指南）

---

## 一、Constellations 做什麼 ＋ 架構概覽

**Constellations - Additional Player Skills** 是 Custom Skills Framework（CSF）的作者 Exit-9B **親自掛保證的「現代 JSON 格式」示範 mod**，同時也是一個功能完整的玩家 mod：它把 Skyrim 原版的 18 個技能擴充成 **21 個**，新增三棵自訂技能樹：

| 技能 | id | 主題 |
|------|-----|------|
| **Hand-to-Hand（徒手）** | `HandtoHand` | 拳擊、武徒手格鬥 |
| **Athletics（運動）** | `Athletics` | 衝刺、耐力、體能 |
| **Sorcery（法術器具）** | `Sorcery` | 法杖、卷軸、魔法道具 |

三棵新樹**直接出現在原版的 ESC → Skills 頁面**，不需另開選單，對玩家完全無感接軌。

### 架構組成

```
ConstellationsNewSkills.esp               ← PERK / GLOB / KYWD / MGEF 全在這
SKSE/Plugins/Constellations.dll           ← mod 私有 SKSE plugin（讀 AVG.toml，處理 Fortify 機制）
SKSE/Plugins/CustomSkills/SKILLS.json     ← 特例：覆寫原版技能選單，把三棵新樹插進去
SKSE/Plugins/CustomSkills/Constellations/HandToHand.json
SKSE/Plugins/CustomSkills/Constellations/Athletics.json
SKSE/Plugins/CustomSkills/Constellations/Sorcery.json
SKSE/Plugins/ActorValueData/Constellations_AVG.toml  ← Fortify-Skill 附魔/藥水映射
Interface/Translations/ConstellationsNewSkills_ENGLISH.txt
Source/Scripts/CNS_InitScript.psc         ← 初始化（繼承 ReferenceAlias）
Source/Scripts/CNS_ModObjects.psc         ← 屬性容器 Quest
Source/Scripts/CNS_TIF__Training*.psc     ← 七支訓練師 TIF fragment
Meshes/Constellations/Interface/INTPerkSkydome.nif   ← 自訂星圖（21 技能版）
Textures/Constellations/...               ← 技能樹貼圖
Meshes/Constellations/Apocrypha/          ← Apocrypha perk 重置祭壇 NIF
Sound/Voice/ConstellationsNewSkills.esp/  ← 訓練師台詞音效（.fuz）
Seq/ConstellationsNewSkills.seq           ← 對話 .seq（讓現有存檔的對話正確觸發）
```

**核心依賴**：Custom Skills Framework（CSF，Nexus 41780）的 `CustomSkills.dll` + `CustomSkills.psc`（玩家須另裝）。Constellations 本身的 `Constellations.dll` 是私有擴充（只負責 Fortify 附魔 / 藥水機制），而 CSF `CustomSkills.dll` 才是「選單外殼 + XP / 升級引擎」的提供者。

---

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

### 2.2 esp Record 需求

每棵自訂技能樹需要的 esp record：

| Record 類型 | 數量（per 技能） | 說明 |
|-------------|----------------|------|
| **PERK** | 節點數 × 階數 | 樹的全部節點；多階 perk node 只填第一階，後續靠 Next Perk 鏈 |
| **GLOB** | 3（最小） | `level` / `ratio` / `legendary`；需有 editor id |
| **KYWD** | 2–3 | `CustomSkillAdvance_<Id>`（掛 perk Modify-Skill-Use）、`CustomSkillBook_<Id>`（技能書）；可選 `CustomSkillWorkbench_<Id>` |
| **MGEF** | 可選（4+ per 技能） | 「Fortify 技能」附魔/藥水效果；需搭配私有 SKSE plugin |
| **QUST** | 1（全域共享） | init script 所在 quest（start game enabled）；可加 ModObjects 容器 quest |

Constellations 三棵技能的 GLOB FormId 分配：

```
HandToHand: level=00F / ratio=010 / legendary=011
Athletics:  level=012 / ratio=013 / legendary=014
Sorcery:    level=015 / ratio=016 / legendary=017
```

### 2.3 `ActorValueData/*.toml`（私有 Fortify 映射）

`Constellations_AVG.toml` 供 `Constellations.dll` 讀取，讓自訂技能借用閒置的原版 ActorValue 槽：

```toml
[HandToHandEnchantments]
type = "Adaptive"
alias = "OneHandedSkillAdvance"

[HandToHandPotions]
type = "Adaptive"
alias = "TwoHandedSkillAdvance"

[SorceryEnchantments]
type = "Adaptive"
alias = "ConjurationSkillAdvance"

[SorceryPotions]
type = "Adaptive"
alias = "EnchantingSkillAdvance"

[Include]
"ConstellationsNewSkills.esp" = ["HandToHandEnchantments", "HandToHandPotions", "SorceryEnchantments", "SorceryPotions"]
```

> ⚠️ 這個 `.toml` 是 **Constellations 私有的 SKSE plugin（`Constellations.dll`）** 讀的，不是 CSF 本身的功能。純 esp 做不到 Fortify 附魔/藥水——需要自寫 native SKSE plugin。

### 2.4 Papyrus 接線

**`CNS_InitScript.psc`**（`extends ReferenceAlias`，掛在 PlayerRef 身上）：

```papyrus
Scriptname CNS_InitScript extends ReferenceAlias

Perk Property CNS_AlchemyEffects Auto
Perk Property CNS_H2H_AutoPerk Auto
GlobalVariable Property SkillAthleticsLevel Auto
GlobalVariable Property SkillHandToHandLevel Auto
GlobalVariable Property SkillSorceryLevel Auto

int Property CurrentVersion = 1 AutoReadOnly
int KnownVersion = 0

Event OnInit()
    RegisterForSingleUpdate(1.0)
EndEvent

Event OnPlayerLoadGame()
    if KnownVersion != CurrentVersion : DoUpdate()
EndEvent

Function DoNewInstall()
    int startingLevel = Game.GetGameSettingInt("iAVDSkillStart")   ; = 15
    SkillAthleticsLevel.SetValue(startingLevel)
    SkillHandToHandLevel.SetValue(startingLevel)
    SkillSorceryLevel.SetValue(startingLevel)
EndFunction
```

**`CNS_TIF__Training*.psc`**（七支，各一行）：

```papyrus
CustomSkills.ShowTrainingMenu("Sorcery", 90, akSpeaker)
; 訓練師等級上限 50/75/90 = adept/expert/master
```

### 2.5 在地化

- `Interface/Translations/ConstellationsNewSkills_ENGLISH.txt`：UTF-16 LE BOM、tab 分隔、`$key\tValue`。
- JSON 的 `$`-key 對應技能名/述；perk 的名字/描述走 esp 的 PERK record FULL/DESC（兩套獨立通道）。
- 翻譯 = 多放 `_CHINESE.txt`（同 key，換 value）；無 ENGLISH fallback。

---

## 三、Constellations vs CSF 比較表

> **重要前提**：Constellations 本身**就是 CSF 框架**的使用者 mod，不是獨立框架。兩者不是競爭關係——Constellations 是「CSF 路線的最佳實作範本」。下表的「比較」是技術面的對照，而非「兩個框架的選擇」。

| 面向 | Constellations（本 mod） | CSF 框架（CustomSkills.dll） |
|------|--------------------------|------------------------------|
| **角色** | 使用者 mod（三棵新技能樹） | 底層框架（選單外殼 + XP 引擎） |
| **Config 格式** | JSON（v3，現代格式） | 支援 JSON（v2/v3）與舊 INI（v1）兩種 |
| **UI 整合方式** | 覆寫 `SKILLS.json`，直接住進原版技能頁 | `SKILLS.json`（併入原版頁）或具名 `.json`（獨立選單群組）二選一 |
| **選單機制** | 重用原版 skill perk tree UI（星座背景 + perk 節點網格） | 同左（CSF 提供的機制） |
| **perk 格式** | 普通 PERK record，沒有特殊格式 | 同左（CSF 不發明新 perk 格式） |
| **Papyrus API** | 使用 `CustomSkills.ShowTrainingMenu()` 等 v3 函式 | `CustomSkills.psc`（v3：10 個 native 函式 + 事件 extension scripts） |
| **runtime 依賴** | CSF（必裝）+ 私有 `Constellations.dll`（Fortify 功能） | SKSE64 + Address Library |
| **Fortify-技能附魔/藥水** | 有（靠 `Constellations.dll` + `ActorValueData/*.toml`） | CSF 本身不提供；需 mod 自實作 native plugin |
| **skydome** | 自製 NIF（21 技能版星圖） | 可重用 vanilla（免自製） |
| **XP 推進方式** | keyword（用量）+ 訓練選單（花錢）+ MGEF（附魔/藥水） | 框架支援以上三路 + `AdvanceSkill()` / `IncrementSkill()` 任意腳本推進 |
| **節點上限** | 9 個（三棵各 9） | 127 個（schema 定義） |
| **生成難度** | 高（含私有 dll；但 dll 是選配，不做 Fortify 則不需要） | 中（純 JSON + PERK + GLOB + KYWD，無 native code） |
| **主要生成阻力** | `Constellations.dll` 的 native code（Fortify 功能）無法用 ModForge 生成 | 無原生阻力；JSON + esp record 全部可生成 |

### 兩代格式對比（CSF 舊 INI vs 新 JSON）

| 面向 | 舊 INI（v1.x，VIGILANT/GLENMORIL） | 新 JSON（v2/v3，Constellations） |
|------|-------------------------------------|-----------------------------------|
| 後端 | NetScriptFramework | 原生 SKSE plugin（`CustomSkills.dll`） |
| 設定路徑 | `Data/NetScriptFramework/Plugins/CustomSkill.<Id>.config.txt` | `Data/SKSE/Plugins/CustomSkills/<X>.json` |
| 座標格式 | `GridX`/`GridY`（整數）+ `X`/`Y`（浮點） | 只有 `x`/`y`（浮點自由佈局） |
| 語意 | 大致與 JSON 欄位一一對應 | 現代格式、JSON schema 驗證 |
| Papyrus API | 極少（只能靠 global 間接觸發） | v3：10 個 native 函式 + 事件 extension |
| 新 mod 建議 | **不要用**（維護舊 mod 才碰） | **用這個** |

---

## 四、對 ModForge 技能樹生成路線的影響

### 4.1 關鍵結論：Constellations ≠ 獨立路線

Constellations 不是「另一條路」——它是**CSF 路線的最高品質範本**。調查的結論是：

**ModForge 的技能樹生成路線 = CSF 路線（JSON v3 格式），Constellations 是這條路線的參考實作。**

Constellations 的主要貢獻是：它把「純 CSF 最小可行」（JSON + PERK + GLOB + KYWD）和「進階加值」（私有 dll + AVG + Fortify MGEF）這兩層**清楚地拆開**，讓 ModForge 的 generator MVP 邊界一目了然。

### 4.2 技能樹生成的兩個層次

```
MVP（ModForge 可完整生成）
│
├── esp records
│   ├── PERK（節點 perk，普通格式）
│   ├── GLOB × 3（level / ratio / legendary）
│   └── KYWD × 2（CustomSkillAdvance_<Id> / CustomSkillBook_<Id>）
│
├── JSON config
│   ├── SKILLS.json（原版技能頁整合）或 <Id>.json（獨立群組）
│   └── <Mod>/<Skill>.json（技能樹 node 佈局）
│
└── Papyrus（薄接線）
    ├── init alias script（初始化 level GLOB = iAVDSkillStart）
    └── 訓練 TIF fragment（一行 ShowTrainingMenu）

進階加值（超出 ModForge 純 esp 能力，不做也能運作）
│
└── Fortify-技能附魔/藥水
    ├── 需要自寫 native SKSE plugin（讀 ActorValueData/*.toml）
    ├── Fortify MGEF 組（esp 內，每技能 4+ 個）
    └── ActorValueData/<Mod>_AVG.toml
```

### 4.3 和既有 ModForge 能力的契合點

| 元素 | 與 ModForge 的契合 |
|------|-------------------|
| PERK records | 既有 perk 生成能力**直接重用**（`PerkConditionTabCount` CTD 地雷仍適用） |
| GLOB records | 簡單 GLOB，需有 editor id；ModForge 能產 |
| KYWD records | 普通 keyword record；ModForge 能產 |
| JSON `form` 字串格式 | `"Plugin.esp|FormId"` load-order 無關；與 ModForge FormId 配置**天然契合**——生成器只要知道輸出的 plugin 名 + 本地 FormId 就能填 |
| Translations 檔 | `$`-key + UTF-16 LE BOM + tab 分隔；需新增 generator |

---

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

> **Constellations 的最大價值是它把「最簡可行（JSON + 三個 GLOB + keyword + 訓練 TIF）」和「進階加值（私有 dll + AVG + Fortify MGEF）」清楚地拆分開來，精確界定了 ModForge 技能樹 generator 的 MVP 邊界。ModForge 走 CSF 路線是正確的，Constellations 是這條路線的最佳參考實作。**
