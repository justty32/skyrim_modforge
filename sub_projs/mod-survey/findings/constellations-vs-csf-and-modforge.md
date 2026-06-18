# vs CSF 比較 + 對 ModForge 生成路線的影響

← [constellations](constellations.md)

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

