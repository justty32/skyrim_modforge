# Custom Skills Framework (CSF) 技術調查

> 調查對象：Exit-9B 的 **Custom Skills Framework**（Nexus 41780）與其上層自訂技能樹案例（VIGILANT、GLENMORIL、Unarmored Defense）。
> 目的：弄懂 CSF 如何運作、自訂技能樹如何定義，評估 ModForge 未來「生成 CSF 設定」的可行性。
> 慣例：散文用繁體中文，code / JSON key / API 名稱保留 English。
> 參考：GitHub wiki <https://github.com/Exit-9B/CustomSkills/wiki>、JSON Schema `docs/schema/{CustomSkill,skill,defs}.json`，以及本機解壓的封存實檔。

---

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

## 3. 案例研究：VIGILANT 技能樹

「Vigilant of Stendarr」——專為 VIGILANT 任務 mod 設計的職業技能：學習對抗 Undead / Vampire / Ghost / Daedra。

- 技能 id：**`VigPious`**（設定檔 `CustomSkill.VigPious.config.txt`）。
- esp：`Perk-Vigilant.esp`（93 KB，小，安全）。
- skydome：`Interface/VIGILANT/intVigilantskydome.nif`（自帶，Stendarr 星座貼圖）。
- Globals（皆在 `Perk-Vigilant.esp`）：Level `0xD64`、Ratio `0xD63`、ShowLevelup `0xD62`、ShowMenu `0xD61`、PerkPoints `0x877`、Legendary `0x878`。
- 支援腳本：`zVCSFSkillManagerQuestScript`、`zVCSFTriggerOpenMenuScript`、`zVCSFVigilantPerkMenuOpenScript`、`zVCSFMgePerkPointScript`、`zVCSFKillActorQuestScript`（用一個 quest 管理升級、靠擊殺特定敵人推進技能、玩家觸發開選單）。

**Perk 樹**（21 節點，Node0 為隱形 root，Node1 為入口）。每節點 `PerkFile`+`PerkId`+`X`/`Y`（浮點佈局）+`GridX`/`GridY`（網格分欄）+`Links`：

| # | Perk 名 (EDID) | FormId | 作用（取自 esp 描述） |
|---|----------------|--------|------------------------|
| 1 | Prayer (`zVigP00CriticalChance01`) | 0xD65 | 入口；暴擊機率 |
| 2 | Exorcist (`zVigP01AUndead01`) | 0xD68 | 對亡靈額外傷害 |
| 3 | Wolf's Bane (`zVigP02AWerewolf01`) | 0xD66 | 對狼人額外傷害（多階 6/10/15/20%） |
| 4 | Inquisition (`zVigP02AInquisition01`) | 0xD67 | 偵訊/審判系 |
| 5 | Daedra Banisher (`zVigP03ADaedra01`) | 0xD69 | 對魔族額外傷害 |
| 6 | Holy Water (`zVigP01BResistUndead01`) | 0xD6A | 對亡靈抗性 |
| 7 | Insensitivity (`zVigP02BResistGhost01`) | 0xD6C | 對幽靈抗性 |
| 8 | Silver Powder (`zVigP02BResistWerewolf01`) | 0xD6B | 減狼人傷害（多階 6/10/15/20%） |
| 9 | The Blessed (`zVigP03BResistDaedra01`) | 0xD6D | 對魔族抗性 |
| 10 | Steadfast Belief (`zVigP01CCriticalDamage01`) | 0xD90 | 暴擊傷害 |
| 11 | Merciful Forbearance (`zVigP02CCharity01`) | 0xD93 | 慈悲/施捨系 |
| 12 | Righteous Might (`zVigP02CRigidity01`) | 0xD97 | 剛性/格擋強化 |
| 13 | Keeper (`zVigP03CShieldRate01`) | 0xD99 | 盾牌格擋率 |
| 14 | Share Knee Pain (`zVigP01DGuard01`) | 0xD9E | 守衛系 |
| 15 | Long Lecture (`zVigP02CTurnUndead01`) | 0xDA5 | 驅散亡靈（Turn Undead） |
| 16 | Creaking Gate (`zVigP02CWard01`) | 0x800 | 防護 ward |
| 17 | Garlic (`zVigP03CResistVampDrain01`) | 0xDA0 | 抗吸血鬼吸取 |
| 18 | Great Noon (`zVigP03CSun01`) | 0x807 | 陽光/Sun 系 |
| 19 | Blood of ANU (`zVigP04BDaedricWeapon01`) | 0x80C | 魔族武器強化 |
| 20 | Blood of PADOMAY (`zVigP04ADaedricArmor01`) | 0x811 | 魔族護甲強化 |

樹形拓撲（`Links`）：Node1 → {2,6,10,14}（四條支線：A 攻擊/B 抗性/C 信仰/D 守衛），各支線往下分岔，末端匯入 Blood of ANU/PADOMAY 等高階節點。多階 perk 在 esp 內以 02/03/04/05 後綴的 PERK record 鏈接（樹只放 01 起點）。

翻譯：EN 版與 ZH 版只是換 `config.txt`（技能名/述）+ 換 `Perk-Vigilant.esp`（perk 名/述為 inline CJK）。

---

## 4. 案例研究：GLENMORIL（較簡）

「Insight（洞察）」——GLENMORIL 任務 mod（Bloodborne 風）的技能：使用「園丁竊取的神秘學」與槍械（rifle/pistol/gatling）的獵人能力。

- 技能 id：**`GLHunter`**（`CustomSkill.GLHunter.config.txt`，UTF-8 編碼，與 VIGILANT 的 UCS-2 不同）。
- esp：`Perk-Glenmoril.esp`（36 KB）。skydome：`Interface/GLENMORIL/intGlenmorilskydome.nif`。
- Globals（`Perk-Glenmoril.esp`）：Level `0xD62`、ShowMenu `0xD65`、Legendary `0x862`。**注意 Ratio/ShowLevelup 標為 `_Dummy UNUSED`、PerkPoints/Color/DebugReload 為空（`""`/`0`）**——示範了「可選 global 不啟用」的最小組態。
- 支援腳本含 `zGLCSFRegainQuest`/`zGLCSFRegainEffectScript`（生命/狀態回復機制），其餘與 VIGILANT 同套（kill-actor 推進、trigger 開選單、skill-level manager）。

**Perk 樹**（20 節點 + root），縱深較深（GridY 直到 5）：Hunter(0xD68) 入口 → 分槍系（Rifle 0x817 / Pistol 0x815）→ Rule(0x820) → 分 Metamorphosis 順/逆時鐘(0x81D/0x81A)、Lake(0x822)、Pistol cost(0x825) → Impurity(0x82D) → Blood Rapture(0x830)/Beast Embrace(0x835)/Gatling(0x83A)/Gatling cost(0x83F) → Communion(0x844) → Holy Body(0x847)/Holy Grail(0x84C)/Eyes(0x851, rifle 偷襲倍率 x3/x4)/Radiance(0x854)/Apocrypha(0x859) → Guidance(0x85E)。主題圍繞槍械精通、變身、神聖/邪穢二元。

翻譯模型同 VIGILANT。

---

## 5. 對 ModForge 的相關性

**結論：ModForge 完全有能力生成一個現代（JSON）CSF 自訂技能，而且大部分所需 record 已是 ModForge 既有能力。**

一個 generated 自訂技能需要產出兩塊：

**(A) esp 端 record（ModForge 既有 perk 支援可重用）**
- PERK records：技能樹的全部 perk（含多階鏈），就是普通 PERK record——ModForge 的 perk 支援直接適用（注意 memory 裡的 `PerkConditionTabCount` CTD 陷阱仍適用）。
- GLOB（GlobalVariable）records：`level`、`ratio`、`showMenu`、`showLevelup`、可選 `legendary`、`color`、`perkPoints`、`debugReload`。這些是簡單的 GLOB record，需要 editor id（console 用）。
- KYWD（Keyword）records：可選的 `CustomSkillAdvance_<Id>`、`CustomSkillBook_<Id>`、`CustomSkillWorkbench_<Id>`，並把它們掛到對應 perk entry-point / book / workbench。
- 可選：BOOK（技能書）、COBJ/workbench、以及驅動「開選單 / 推進技能」的 quest+script（如 VIGILANT 的 `zVCSF*` 那套）；或改用 v3 的 `CustomSkills.psc` API 直接呼叫，省去自寫管理腳本。
- 資產：一個 skydome `.nif`（可重用 vanilla `DLC01/.../INTVampirePerkSkydome.nif` 當預設，免自製）。

**(B) CSF 設定檔（ModForge 需新增的「generator」）**
- 產 `Data/SKSE/Plugins/CustomSkills/<X>.json`：`version:1` + `skills:[{id,name,description,level/ratio/...指向上面 GLOB 的 "Plugin.esp|FormId",nodes:[{perk,x,y,links}]}]`。
- 因為 `form` 是 `"Plugin.esp|FormId"` 字串（load-order 無關），ModForge 只要知道自己產出的 plugin 檔名 + 各 record 的本地 FormId 就能填——**這與 ModForge 既有的 FormId 配置流程天然契合**。
- 可選 `$`-key + `Data/Interface/Translations/<Plugin>_ENGLISH.txt`（UTF-16 LE BOM、tab 分隔）做在地化。

**最小可行產物** = 「N 個 PERK + 對應 GLOB（至少 level/ratio/showMenu/showLevelup）」的 esp ＋「描述樹形佈局的 X.json」。XP/選單/升級這層由 `CustomSkills.dll` runtime 接手，perk 效果這層完全沿用 ModForge 現有 perk 生成。CSF 只是「選單 + XP 殼層」，不改變 perk 本身的生成方式。

> 若要相容**舊 INI 技能樹（VIGILANT/GLENMORIL 那代）**，則改產 `Data/NetScriptFramework/Plugins/CustomSkill.<Id>.config.txt`（UCS-2/UTF-8、`Name=`/`LevelFile`+`LevelId`/`Node<n>.PerkFile`+`PerkId`+`X`/`Y`/`GridX`/`GridY`+`Links`）。語意與 JSON 一一對應，但需 NetScriptFramework runtime——建議只在維護既有 mod 時才需要，新生成一律走 JSON。
