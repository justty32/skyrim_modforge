# CSF 是什麼 + 架構細節

← [custom-skills-framework](README.md)

## 1. CSF 是什麼

CSF 是一個 **SKSE plugin（`CustomSkills.dll`）+ Papyrus API（`CustomSkills.psc`）** 的框架。它解決的問題是：原版 Skyrim 只有 18 個固定技能（外加 Vampire / Werewolf 兩棵 Beast perk 樹），mod 作者想做「全新的技能與 perk 樹」（例如「斯坦達爾的警戒者」這種職業技能）時，原本沒有原生的選單與升級機制。

CSF 提供的是 **選單層 + 經驗值層**：
- 重用原版的「skill perk tree」UI（星座背景 skydome + perk 節點網格），渲染一棵作者自訂的 perk 樹。
- 提供 XP / 升級 / 升級訊息 / legendary 重置 / 自訂 perk point 池等機制。
- perk 本身**仍然是 esp 裡正常的 PERK record**——CSF 不發明新的 perk 格式，它只負責「把這些 perk 排成一棵樹、給它一個技能名、追蹤等級、開選單」。

換句話說：**perk 的效果由 esp 決定，技能的「外觀與進度」由 CSF 設定檔決定。**

### 關鍵架構斷層：兩代設定格式（很重要）

調查中最重要的發現：**CSF 有兩種完全不同的設定檔格式，分屬不同世代**，而題目給的 VIGILANT/GLENMORIL 封存屬於**舊格式**：

| 世代 | 後端 | 設定檔位置與格式 | API version |
|------|------|------------------|-------------|
| **舊（v1.x）** | NetScriptFramework | `Data/NetScriptFramework/Plugins/CustomSkill.<Id>.config.txt`，INI 風格 key=value | — |
| **新（v2.x / v3.x）** | 原生 SKSE plugin (`CustomSkills.dll`) | `Data/SKSE/Plugins/CustomSkills/<X>.json`，JSON | 2（v2.0.2）/ 3（v3.1.0） |

- 本機解壓的 **VIGILANT v200 / v20a、GLENMORIL v200** 三個技能樹，shipped 的設定都是 `NetScriptFramework/Plugins/CustomSkill.VigPious.config.txt`（VIGILANT）、`CustomSkill.GLHunter.config.txt`（GLENMORIL）——**舊 INI 格式**，需要 NetScriptFramework runtime。它們尚未被移植到 JSON。
- 框架封存 `Custom Skills Framework-41780-*` 三個版本（v2.0.2 SE、v2.0.2 for 1.5.97、v3.1.0）shipped 的只有 `CustomSkills.dll` + `CustomSkills*.psc/pex` + console YAML，**完全沒有 JSON 範例**——JSON 設定由各技能樹 mod 自帶。
- JSON 格式的權威定義在 wiki 與 `docs/schema/*.json`（見下節）。兩代的**欄位語意幾乎一一對應**（INI 的 `LevelFile/LevelId` ↔ JSON 的 `level: "File.esp|FormId"`），只是序列化形式不同。

> 對 ModForge 的意義：要生成「現代 CSF」設定，目標是 **JSON 格式（v2/v3）**；舊 INI 只作為理解語意的對照與既有 mod 的相容性參考。

---

## 2. 架構細節

### 2.1 JSON 設定檔（v2/v3，`Data/SKSE/Plugins/CustomSkills/X.json`）

- `X` = 該「技能選單群組」的唯一 id。**`SKILLS.json` 是特例**：它會「取代原版技能選單」（Constellations - Additional Player Skills 就是把原版 18 技能擴成 21，靠的就是覆寫 `SKILLS.json` + 自訂 skydome）。其他檔名則是獨立的自訂選單群組，用 Papyrus / console 開啟。

**Root 物件**（`CustomSkill.json` schema）：

| 欄位 | 型別 | 必填 | 說明 |
|------|------|------|------|
| `version` | integer | 是 | 常數 `1`（schema 版本，不是 API 版本） |
| `skills` | array | 是 | 此選單要顯示的技能；元素可為「原版技能名字串列舉」或「自訂技能物件」 |
| `skydome` | object | 否 | 背景 skydome 模型規格 |
| `showMenu` | form\|null | 否 | 一個值為 0 的 global，外部設成 1 即開選單 |
| `debugReload` | form\|null | 否 | 值為 0 的 global，設 1 重載設定（debug 用） |
| `perkPoints` | form\|null | 否 | 自訂 perk point 池的 global；不設則用玩家標準 perk point。**一個選單群組只能有一個 perk point 來源** |

`skydome` 子物件：`model`（相對 `Data/Meshes` 的 nif 路徑，預設 `DLC01/Interface/INTVampirePerkSkydome.nif`）、`cameraRightPoint`（整數，1=vanilla skydome、2=beast skydome，預設 2）。

`skills[]` 元素若為原版技能，是字串列舉（`Alchemy`、`Destruction`、`OneHanded`、`Smithing`、`VampirePerks`、`WerewolfPerks` … 等 20 個）；若為自訂技能，則是下面的 **skill 物件**（`skill.json` schema）。

**Skill 物件**（`skill.json`）：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `id` | string | 此技能的唯一 ID，供 Papyrus 函式引用（如 `"VigPious"`、`"GLHunter"`） |
| `name` | localizedString | 技能顯示名（建議 `$`-key，見 §2.4） |
| `description` | localizedString | 技能描述 |
| `level` | form\|null | 存「目前技能等級」的 global variable；省略則做成像 Beast 技能那樣無等級 |
| `ratio` | form\|null | 存「距下一級進度」的 global（0–1） |
| `legendary` | form\|null | 存「歸零成 legendary 的次數」的 global |
| `color` | form\|null | 存技能名 RGB 顏色的 global（如 `0xFFFFFF`） |
| `showLevelup` | form\|null | 值為 0 的 global，設 1 顯示升級訊息 |
| `experienceFormula` | object | 經驗公式參數 |
| `nodes` | array | perk 樹節點清單；**第一個 node 必填，即使技能沒有 perk** |

`experienceFormula`：`useMult`(1.0) / `useOffset`(0.0) / `improveMult`(1.0) / `improveOffset`(0.0) / `enableXPPerRank`(false)。

**Node 物件（perk 節點）**，`nodes` 陣列最多 **127** 個：

| 欄位 | 型別 | 必填 | 說明 |
|------|------|------|------|
| `id` | string | 否 | 給 `links` 引用的節點 ID |
| `perk` | form | 是 | 該節點的 perk（多階 perk 只需第一階的 FormId） |
| `x` | number | 是 | 水平位置（**正方向朝左**） |
| `y` | number | 是 | 垂直位置（**正方向朝上**） |
| `links` | array | 否 | 連到的節點，用 `id` 字串或 **1-based 索引** |

**`form` 字串格式**（`defs.json`）：`"PluginName.es[lmp]|FormId"`，FormId 為 3–8 位十六進位，可選 `0x` 前綴。正則：`^[^\\\/:*?"<>|]+\.es[lmp]\|(0[Xx])?[\dA-Fa-f]{3,8}$`。例：`"Perk-Vigilant.esp|D65"`。**這是 load-order 無關的**——CSF runtime 用 plugin 名 + 本地 FormId 查表，所以不受載入順序索引影響。

`localizedString`：以 `$` 開頭視為翻譯 key（推薦），不以 `$` 開頭視為直接字面值（deprecated）。

### 2.2 Papyrus API 表面（`CustomSkills.psc`，v3.1.0）

全部是 `global native`，`Scriptname CustomSkills Hidden`：

```papyrus
int  GetAPIVersion()                                              ; 目前回傳 3
void OpenCustomSkillMenu(string asSkillId)                        ; 開某技能/群組(設定檔)的選單
void ShowTrainingMenu(string asSkillId, int aiMaxLevel, Actor akTrainer)
void AdvanceSkill(string asSkillId, float afMagnitude)            ; 依使用量推進技能
void IncrementSkill(string asSkillId)                             ; +1 級
void IncrementSkillBy(string asSkillId, int aiCount)
string GetSkillName(string asSkillId)
int  GetSkillLevel(string asSkillId)
void ShowSkillIncreaseMessage(string asSkillId, int aiSkillLevel)
void DebugReload()                                               ; debug only
```

> v2.0.2 的 `CustomSkills.psc` 只有三個函式：`GetAPIVersion()`、`OpenCustomSkillMenu()`、`ShowSkillIncreaseMessage()`。v3 大幅擴充了 advance/increment/training/getlevel 等便利函式。**這是 v2→v3 的主要 API 差異。**

額外的 extension scripts（v3）：
- `CustomSkills_FormExt.psc`：`RegisterForCustomSkillIncrease(Form)` + `Event OnCustomSkillIncrease(string asSkillId)`；`RegisterForCustomSkillBookRead(Form, bool abReplaceDefault)` + `Event OnCustomSkillBookRead(string asSkillId, int aiIncrement)`。讓任意 script 監聽技能升級 / 技能書被讀事件。
- `CustomSkills_AliasExt.psc`、`CustomSkills_ActiveMagicEffectExt.psc`：同類事件註冊的 alias / magic-effect 版本。

**Console**（`SKSE/CustomConsole/CustomSkills.yaml`，需 CustomConsole 框架）：alias `csf`，子命令 `showstatsmenu`、`advanceskill`/`advskill`、`incrementpcskill`/`incpcs`、`reload`。題目提到的 `set myskillopenmenu to 1` 等則是「直接設那個 `showMenu`/`level`/`ratio`/`showLevelup` global」的 console 用法（前提是該 global 在 esp 裡有 editor id）。

### 2.3 Keyword 掛鉤

三個慣例命名的 keyword，後綴是技能的 id（`CustomSkill<Type>_<MySkill>`）：

- **`CustomSkillAdvance_MYSKILL`**：配合 perk 的「Modify Skill Use」entry point + `EPModSkillUsage_AdvanceObjectHasKeyword` 條件，用來改變升級速度（取代原版 `EPModSkillUsage_IsAdvanceSkill`）。
- **`CustomSkillBook_MYSKILL`**：貼在 book 上，首次閱讀即推進對應自訂技能（等同原版技能書）。
- **`CustomSkillWorkbench_MYSKILL`**：貼在 constructible workbench 上，在該處製作即給該技能 XP。

### 2.4 Localization（在地化）模型

**現代 JSON**：`name`/`description` 用 `$`-key，真正文字放 `Data/Interface/Translations/<Plugin>_<LANG>.txt`（tab 分隔、UTF-16 LE BOM、語言後綴 ENGLISH/CHINESE/…），與 MCM 在地化同套機制；**無 fallback 到 ENGLISH**，玩家須備妥對應語言檔。

**舊 INI（VIGILANT/GLENMORIL 實況）**：技能 `Name`/`Description` 是**直接寫死在 `config.txt` 字面值**（見 §3）。實測 EN 與 ZH 兩份 `config.txt` 差異只在 `Name = "Vigilant of Stendarr"` vs `Name = "斯坦达尔的警戒者"`——翻譯=換 config 檔。

**perk 的名字與描述兩代都一樣**：它們是 esp 裡 PERK record 的 FULL/DESC 欄位。實測 VIGILANT 英文 esp 內含 `"Wolf's bane" / "Weapons do 6% more damage againt Werewolf."` 等 inline 文字；而中文翻譯封存提供**另一個 esp**，內含 inline CJK 文字（如 `斯坦达尔的警戒者`、`角笛`、`拜领` 等），並非用主檔 STRINGS。**結論：CSF 技能樹的在地化 = 翻譯版 config.txt（技能名/述）+ 翻譯版 esp（perk 名/述）。**

---

