# Roadmap — Findings 缺口全集

← [roadmap/archive](README.md)

> **已凍結（archived）**：本檔是 2026-06-15 一次性 findings 審閱的完整快照。所列缺口除 G/H/A-partial/J-尾巴外幾乎全已落地（見 git log / feature-dev/landed）。活檔的 open 殘餘見 [../all-findings-gaps.md](../all-findings-gaps.md)。內部連結容忍 stale。

> **本檔由人工逐檔閱讀 `sub_projs/mod-survey/findings/` 全部 32 個檔案後整合（2026-06-15）。**  
> 每條缺口附來源 findings、scope 與優先級。已在其他 roadmap 檔記錄的缺口打 `→ 見 X.md`；新缺口標 `🆕`。  
> 優先序：🔴 高（alts 沒有就做不了某類 mod）→ 🟡 中（有替代路但繁瑣）→ 🟢 低（nice-to-have）。

---

## A 組：Radiant Quest / Alias 填充系統（前置必備）

這組是「能不能生 radiant 任務」的根基。來源：**missives.md**、**encounter-mods.md**、**sm-subsystem.md**。  
→ 缺口 #7 #8 #9 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 7 | ✅ QuestAlias `findMatchingLocation` fill（LocationAlias 型）—— radiant 地點隨機化根基 | ✅ 落地 06-17 |
| 8 | ✅ QuestAlias 在地點內找 ref（`findInLocationAlias`，scope 校正：非 ALNA 而是 LocationAliasReference）| ✅ 落地 06-17 |
| 9 | ✅ `UpdateCurrentInstanceGlobal` fragment codegen——gather/計數型 quest objective 文字即時更新 | ✅ 落地 06-17 |

**PARTIAL（多數已支援，留窄缺口）：**
- SM branch/quest-node 多層巢狀 SMBN（→ [mod-survey-gaps.md](../mod-survey-gaps.md) ⚠️ partial）
- LVLN alias fill 一等模式（→ [mod-survey-gaps.md](../mod-survey-gaps.md) ⚠️ partial）

---

## B 組：Perk 互動式 Entry-Point + Fragment

來源：**perk-entry-points.md**、**arrowblock.md**、**immersive-interactions.md**。  
→ 缺口 #1 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 1 | ✅ Perk `AddActivateChoice`/`SetText` + PerkAdapter VMAD + fragment 生成器 | ✅ 落地 06-17 |

---

## C 組：Package 目標/地點 → Quest Alias 間接

來源：**encounter-mods.md**、**sm-subsystem.md**。  
→ 缺口 #2 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 2 | ✅ Package `target`/`location` 支援 quest alias index（`PackageTargetAlias`）—— radiant 演出 package 必需 | ✅ 落地 06-17 |

---

## D 組：SKSE Plugin 輔助輸出 Pipeline（次要輸出檔）

ModForge 的 `build` 主輸出是 `.esp`。這組是對應 SKSE framework 的 **ini/json 輔助輸出**，讓 mod 作者免寫 ESP override 就能做到 NPC 標記、物件替換、動畫切換等功能。

### ✅ D-1 SPID `_DISTR.ini` 輸出（**已落地 2026-06-17**）
**來源：** spid.md、common-framework-mods.md  
**狀態：** ✅ 離線實作 `spidDistributions:` spec section → mod 根 `<file>_DISTR.ini`（loose，非 esp）。`Spec.SpidDistribution.cs`（DTO）+ `SpidGen.cs`（emitter，尾段 NONE 修剪、Item count/Package index 第 6 欄）+ `Generator.Validate.Spid.cs`（type/record/chance 校驗）+ `Package.cs` 接 loose-file 段。**14 測綠**；格式逐欄查 spid.md（SPID 7.3）。docs [SPEC-distribution.md](../../../docs/spec/SPEC-distribution.md)、example `spid_distribution_spec.json`、schema 已同步。  
支援 spell/perk/faction/keyword/package/outfit/item 分發給 NPC（依 race/faction/keyword/level/trait 條件）。  
**用途：** 無 ESP patch 給 follower/NPC 加 faction、標記 keyword、分發 ability spell；OAR 條件讀 faction → animation 切換。

### ✅ D-2 MCM Helper `config.json`/`settings.ini` 輸出（**已落地 2026-06-20**）
**來源：** mcm-helper.md、mcm-helper-config-json.md  
**狀態：** ✅ 離線實作 `mcmConfigs:` spec section → `MCM/Config/<modName>/config.json`（menu 佈局）+ `settings.ini`（預設值）。`Spec.Mcm.cs`（DTO）+ `McmGen.cs`（`System.Text.Json` 生 config.json + ini）+ `Generator.Validate.Mcm.cs` + `Package.cs` 接 loose-file 段。**17 測綠**；格式逐欄查 mcm-helper-config-json.md（MCM Helper 1.6.1）。docs [SPEC-distribution.md](../../../docs/spec/SPEC-distribution.md)、example `mcm_config_spec.json`、schema 已同步。  
控件：header/empty/toggle/slider/stepper/enum/keymap/hiddenToggle + group/position 佈局；`sourceType: ModSettingBool/Int/Float/String`；id `key:Section` → ini `[Section] key=`。  
**MVP 範圍：** 純 ini-backed（`ModSetting*`）**零 Quest/Papyrus/master**——DLL 全自動。`PropertyValue*`/`action.CallFunction`（需 Quest 掛 `MCM_ConfigBase` script）**範圍外，validate 擋掉**，留待日後接 Papyrus host。  
**用途：** 任何需要玩家設定面板的 mod（難度、開關功能、follower 行為調整）。  
⚠️ **待主力機：** live menu 只能實機驗（裝 MCM Helper + SkyUI，看選單渲染/存讀）；ModForge 只驗結構。

### ✅ D-3 SkyPatcher ini 輸出（**已落地 2026-06-20**）
**來源：** skypatcher-records-and-config.md、skypatcher-modforge-and-strategy.md  
**狀態：** ✅ 離線實作 `skyPatchers:` spec section → `SKSE/Plugins/SkyPatcher/<recordType>/<file>.ini`。`Spec.SkyPatcher.cs`/`SkyPatcherGen.cs`/`Generator.Validate.SkyPatcher.cs`/`Package.cs`，docs+example+schema 同步。flat `filterK=v:modK=v`（無 section header）、filters AND 在前、mods 在後；recordType 8 白名單（npc/armor/weapon/ammo/leveledList/formList/race/container）。  
**MVP：** 欄位**不白名單**（SkyPatcher 欄位集巨大）——verbatim emit `{key,value}` pairs，作者照 SkyPatcher 文件填；高價值用例＝NPC 加 spell/perk by race、leveled list 注入。  
**用途：** 零衝突批量改 vanilla NPC（外觀/spell/perk/keyword）、注入 leveled list。  
⚠️ **待主力機：** runtime 由 DLL 對 load order 解析 ref（開詳細 log 驗）。

### ✅ D-4 FormList Manipulator `_FLM.ini` 輸出（**已落地 2026-06-20**）
**來源：** formlist-manipulator.md（v1.8.1）、flst-factory.md  
**狀態：** ✅ 離線實作 `formListInjects:` spec section → `<file>_FLM.ini`（mod 根＝`Data/`）。`Spec.FormListInject.cs`（DTO）+ `FlmGen.cs`（emit）+ `Generator.Validate.Flm.cs` + `Package.cs` 接 loose-file 段。**11 測綠**；格式逐欄查 formlist-manipulator-config-core/-advanced.md。docs [SPEC-distribution.md](../../../docs/spec/SPEC-distribution.md)、example `formlist_inject_spec.json`、schema 已同步。  
正確語法＝`FormList = <FList>|<forms>|<Filter>`（非 roadmap 原寫的舊 `Form=/Target=`）；涵蓋 FormList 操作行 + Filter/Alias/Group/Collection 定義（Collection 20 FormType 白名單）。  
**MVP 範圍外：** `ModEvent`（需 Papyrus 發送）+ 特化快捷語法（Plant/BToys/GToys/HairColors/AtronachForge/…）。  
**用途：** 把自家 spell/item/NPC 零衝突加進外部 mod 的 FLST（Spellforge 法術池、SPID 分發目標 FLST、領養禮物池…）；自建 FLST 仍走 esp-side `formLists[]`。  
⚠️ **待主力機：** FLST/form ref 由玩家 load order 解析，runtime 才驗（裝 FLM DLL 開 `FormListManipulator_DEBUG.ini` 看 log 確認追加成功）；ModForge 只驗結構。

### ✅ D-5 KID `_KID.ini` 輸出（**已落地 2026-06-20**）
**狀態：** ✅ `kidDistributions:` → `<file>_KID.ini`。`Spec.KidDistribution.cs`/`KidGen.cs`/`Generator.Validate.Kid.cs`，docs+example+schema 同步。`Keyword = kw|type|filters|traits|chance` 尾段 NONE 修剪（仿 SPID）；type 19 白名單；unknown keyword EditorID → KID 自建 KYWD。  
**用途：** 給道具/record 批量加 keyword（品質分類、SPID/OAR 配合識別）。⚠️ runtime 待主力機驗。

### ✅ D-6 BOS `_SWAP.ini` 輸出（**已落地 2026-06-20**）
**狀態：** ✅ `objectSwaps:` → `<file>_SWAP.ini`。`Spec.ObjectSwap.cs`/`BosGen.cs`/`Generator.Validate.Bos.cs`，docs+example+schema 同步。MVP＝`[Forms]`/`[Forms|cond]` section + `base|swaps|properties|chance`（gap 留 `||`，多 swap 隨機）。  
**MVP 範圍外：** 獨立 `[Properties]`（無 swap 的 transform）+ `[References]` section。  
**用途：** 場景換道具（vanilla clutter → 精緻版、依 location 閘）。⚠️ runtime 待主力機驗。

### ✅ D-7 AOS `_ANIO.ini` 輸出（**已落地 2026-06-20**）
**狀態：** ✅ `animObjectSwaps:` → `<file>_ANIO.ini`。`Spec.AnimObjectSwap.cs`/`AosGen.cs`/`Generator.Validate.Aos.cs`，docs+example+schema 同步。`[Base|FILTERS|TRAITS]` header + `base|swaps`（隨機池）；換 idle 手持 ANIO（非動畫，動畫走 OAR）。  
**用途：** follower 角色化（Sofia 喝酒拿特定杯、法師讀特定書），搭配 OAR。⚠️ runtime 待主力機驗。

---

## E 組：Encounter 地點感知 / 冷卻機制

來源：**encounter-mods.md**（IWE/EE）。  
→ 缺口 #5 #6 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 5 | ✅ LocType keyword 路由 + Hold 偵測（locationFilter + LocAliasHasKeyword）| ✅ 落地 06-17 |
| 6 | ✅ WITimeout 冷卻模式（cooldownHours → GLOB + MFEncounterCooldown script）| ✅ 落地 06-17 |

---

## F 組：NavmeshTester 動態生怪模板

來源：**encounter-mods.md**。  
→ 缺口 #3 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 3 | ✅ NavmeshTester 動態生怪 Papyrus script 模板（quest.spawn → MFDynamicSpawn）| ✅ 落地 06-17 |

---

## G 組：Perk 程序化法術族生成器

來源：**spellforge.md**、**flst-factory.md**。  
→ 缺口 #4 已登入 [mod-survey-gaps.md](../mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 4 | 程序化法術族生成器（school × level × delivery 網格 → 對齊 MGEF+SPEL+tome+FLST）— 🧊 **不必做（2026-06-22 冷凍，等非常有空再考慮）**；純便利層、不解鎖新 mod 類型 | 🟢 |

---

## H 組：Custom Skills Framework 技能樹生成器 — ⏸️ 暫緩（暫時先不做）

來源：**constellations.md**（含 CSF v3 JSON 格式完整文件化）。  
→ 已登入 [generation.md](../generation.md)（含 MVP scope + spec 欄位草案）。

> ⏸️ **2026-06-22 決定暫緩**：研究與 spec 草案皆完整，但暫時先不動工，等之後再排。

**提醒**：CSF skill tree generator 是 generation.md 待補清單的一環，可接著現有 PERK 生成能力做。  
MVP 輸出：`SKSE/Plugins/CustomSkills/<X>.json` + `SKILLS.json`（整合進原版技能頁）+ 3 個 GLOB + 2 個 KYWD + Translations UTF-16 LE BOM + init alias script 模板 + 訓練 TIF。  
**優先：** 🟡（有完整設計文件，下一步寫 spec）— **目前 ⏸️ 暫緩**

---

## I 組：MagicEffectSpec inline script-attach（DX 缺口）

來源：**arrowblock.md**、**mgef-vmad.md**。

✅ **MagicEffectSpec `scripts[]` inline 欄位（已落地 2026-06-20）**  
**狀態：** ✅ `MagicEffectSpec.Scripts: List<ScriptAttachSpec>`（targetEditorId 隱含＝此 effect、忽略）；`AttachScripts` 抽出 `AttachOneScript` helper 共用、`Package.cs` compile loop + `Generator.Validate.Quests.cs`（抽 `CheckScriptProps`）都納入 inline。docs SPEC-magic、schema 同步。**2 測綠**（attach 到 MGEF VMAD + 空 scriptName 驗證）。功能本就通（通用 `scripts[]` 路徑），這是 DX/co-location sugar。

---

## J 組：Papyrus Script 模板（JContainers / PapyrusUtil 模式）

來源：**jcontainers.md**、**papyrusutil.md**、**common-framework-mods.md**。

✅ **PapyrusUtil StorageUtil per-Form KV `storageWrites`（已落地 2026-06-20）**  
**狀態：** JContainers 的 nested per-Form 狀態（`JFormDB.solve…`）其實已由 **persist/syncPerks**（Idea #20）覆蓋；J 組真缺的「簡單 + 自動管理」那半（eval 標為**最高槓桿、最固定**）已補：`storageWrites: [{key, target, int/float/str, delta?}]` → `StorageUtil.Set/Adjust{Int,Float,String}Value`，掛 **dialogue line TIF** 與 **quest stage fragment**（與 persist 同機制 + SM quest 路由 `OnStory<Event>`）。target=speaker/player/none 皆**純 Papyrus 表達式 → body-only、零 VMAD property**，故無 binding-site 改動。新增 `Generator.StorageWrites.cs`，改 `Generator.QuestFragments.cs`（5 處 gate/emit）+ `Build.Scripts.cs`/`Build.QuestStages.cs`（needs-frag gate）+ `Spec.Dialogue.cs`（`StorageWriteSpec`）+ `Generator.Validate.Quests.cs`（`ValidateStorageWrites`）。docs SPEC-quests/SPEC-dialogue、schema、CODE_MAP 同步。**17 測綠**。  
✅ **arbitrary-ref target 已落地（2026-06-22）**：`storageWrites.target` 接受任意 ref（placed-ref EDID / `Master:0xFORMID`）→ 綁 `SWRef_<i>` Form property（仿 persist key），dialogue TIF + quest stage 皆通，per-NPC/per-container 記憶。  
✅ **JsonUtil 讀檔已落地（2026-06-22）**：`storageWrites[].fromJson:{file,key}` → value 改由 `JsonUtil.Get{Int,Float,String}Value(file,key,<literal 作 missing default>)` 取（PapyrusUtil 外部 config 讀取）；複用 storageWrites 全套 target 機制、int/float/str 對稱。編 fragment 多需 `JsonUtil.psc` 上 header path。  
**剩（未做，需求度低）：** `ActorUtil.AddPackageOverride`（臨時 package 覆蓋，需成對清理 = 兩 fragment）、`MiscUtil.ScanCellNPCs`（情境偵測，需條件分支語法 = 大坑）。屬 follower expansion 真正需要時再長。  
**優先：** 🟡 → ✅ 核心 KV 已落地，.pex 隨任何含 fragment 的 quest/dialogue 編（需 PapyrusUtil .psc 上 header path）。

---

## K 組：Quest Script Global Write（SetValue spec）

來源：**runtime-selector-patterns.md**。

✅ **Quest stage `globalWrites: [{global, value}]` 一等 spec 語法（已落地 2026-06-20）**  
**狀態：** ✅ 離線實作 `StageSpec.globalWrites[]` → stage fragment 生 `<global>.SetValue(value)`（plain write，非 instance bind，無 UpdateCurrentInstanceGlobal）。`Spec.Dialogue.cs`（`GlobalWriteSpec`）+ `Generator.QuestFragments.cs`（property 宣告/per-stage emit/SM quest 路由到 `OnStory<Event>` handler，沿用 persist 真因）+ `Generator.Build.QuestStages.cs`（VMAD GLOB property 綁定 + needsFrag）+ `Generator.Validate.Quests.cs`（global 非空 + CheckRef）。docs SPEC-quests、schema、CODE_MAP 同步。**6 測綠**（含 SM quest OnStory 路由 + build VMAD 綁定）。  
**剩（alias 側）：** `AliasSpec` 的 OnInit globalWrites 屬不同 codegen 路徑，未做（需求度低，手寫 alias script 可繞）。stage 側已覆蓋主要用例（stage 里程碑設 flag/counter global）。  
**優先：** 🟡 → ✅ stage 已落地，.pex 隨任何含 fragment 的 quest 一起編（既有 quest-build 路徑，無特殊 headers）。

---

## L 組：Dialogue INFO 讀 Papyrus property 條件型 — ✅ 落地 2026-06-20

來源：**iwh-ith.md**、**conditional-expressions.md**。

✅ **`GetVMQuestVariable` / `GetVMScriptVariable` condition**（code pass 結論：舊 `GetScriptVariable` 名不對，SSE 讀 Papyrus property 的現代函式是這兩個 VM 變體）。
離線實作：`ConditionSpec.VariableName` 欄位；`Generator.Build.Conditions.cs` 加兩 case（`GetVMQuestVariable`→`Quest`+`VariableName`、`GetVMScriptVariable`→`Target`+`VariableName`，`param` 帶 quest/object form）；`Generator.Validate.Helpers.cs` 驗 param+variableName 必填；docs/schema/`ConditionTests` 同步，720 測綠。
**用途：** 讀取 ITH 的 `PlayerInDialogue` property 做 bark 抑制；讀任意 mod 的 quest/object script property 做 follower dialogue 分支。
⚠️ **待主力機收尾**（見 WAIT_USER）：`variableName` 引擎期望的字串格式（bare property 名 vs backing `::Prop_var`）依目標 script 而定，需 xEdit/實機驗——ModForge verbatim 寫進 CTDA。

---

## M 組：Dialogue INFO 批次建立 + 條件模板

來源：**follower-commentary-overhaul.md**（FCO 設計）、**relationship-dialogue-overhaul.md**（RDO 設計）。

✅ **condition template 共享機制 + INFO 陣列批次建立（兩半皆已落地 2026-06-20）**  
**狀態（template 部分 ✅）：** `conditionTemplates: [{name, conditions}]` 命名條件組 + `dialogue[].useConditionTemplates: [name…]` 展開到 INFO（inline conditions 之後、同 `BuildCondition` 路徑、alias-aware）。`Spec.Dialogue.cs`（`ConditionTemplateSpec`）+ `Generator.Build.Conditions.cs`（`WireDialogueConditions` 展開）+ `Generator.Validate.Quests.cs`（name 唯一/非空 + 每條 CheckCondition + 引用須存在）。docs SPEC-dialogue、schema、CODE_MAP 同步。**4 測綠**。解決 FCO 265 條共用 gate 的痛點。  
**INFO 陣列批次建立 ✅（2026-06-20）：** `dialogue[].variants: [{responses, conditions?, emotion?, emotionValue?, sayOnce?}]` → 同一 topic 掛多條 sibling INFO，各帶 `Random` flag（引擎在條件符合的 sibling 隨機選），**共用** parent entry 的 speaker gate + `conditions` + `useConditionTemplates` + `identity`，再各接自有 conditions/lines。parent `responses` 空 → 純批次 header 不發 parent INFO。`Generator.Build.Dialogue.cs`（`DialogueVariantId(ed,i)` + variant INFO 建立）+ `Generator.Build.Conditions.cs`（`ApplyShared` 套 parent 與每 variant）+ `Spec.Dialogue.cs`（`DialogueVariantSpec`）+ `Generator.Validate.Quests.cs`（variant responses/emotion/conditions + hello 互斥）。docs SPEC-dialogue、schema、CODE_MAP 同步。**11 測綠**。正解 FCO 265 條共用 gate 痛點。  
**剩：** 兩半皆已落地；commentary 大量生成（旅途/地點/時間/天氣/玩家狀態反應）所需的 spec 語法齊備。

---

## 參考：已登入其他 roadmap 檔的非缺口項

以下是 findings 中「ModForge 值得未來支援」但非技術缺口的項目，已在其他檔：

| 項目 | 狀態 | 位置 |
|------|------|------|
| OAR animation replacer config 生成 | ✅ **已落地** | generation.md + SPEC-animation.md |
| BDI behavior variable 注入 config 生成 | ✅ **已落地** | generation.md + SPEC-animation.md |
| CSF custom skill tree JSON 生成 | 🗂️ 設計完整，待 spec→build | generation.md |
| Pandora shell-out 整合 | 🗂️ spike 待主機實測 | generation.md |
| 場景 Emotion/EmotionValue 生成 | 🗂️ 待做 | generation.md |

---

## 快速執行順序建議

按「做了解鎖最多後續功能」排：

1. ~~**A 組 #7 + #8**（LocationAlias + ALNA fill）~~ ✅ **已落地 2026-06-17**（#7 findMatchingLocation、#8 findInLocationAlias；#8 scope 校正：ALNA 離線驗＝LinkedRefChild-only，改走 LocationAliasReference）→ radiant quest 生成解鎖（CK 語義待主力機驗）
2. ~~**B 組 #1**（Perk AddActivateChoice + fragment）~~ ✅ **已落地 2026-06-17** → 互動式 perk mod（PerkAdapter byte 待主力機驗）
3. ~~**C 組 #2**（package alias 間接）~~ ✅ **已落地 2026-06-17** → radiant 演出 package（byte 待主力機驗）
4. ~~**D-1**（SPID _DISTR.ini）~~ ✅ **已落地 2026-06-17** → 無衝突 NPC 標記與兼容 patch
5. ~~**D-2**（MCM Helper）~~ ✅ **已落地 2026-06-20** → 玩家設定面板（live menu 待主力機實機驗）
6. ~~**D-4**（FLM ini）~~ ✅ **已落地 2026-06-20** → 外部 FLST 注入（Spellforge/SPID 兼容），runtime 待主力機驗
7. ⏸️ **H 組**（CSF skill tree）→ **暫緩（2026-06-22 決定暫時先不做）**；接 generation.md 現有設計，已有詳細 spec 草案，之後再排
8. ~~**L 組**（GetVMQuestVariable/GetVMScriptVariable 條件）~~ ✅ **已落地 2026-06-20** → follower ambient bark 品質（variableName 字串格式待主力機 xEdit 驗）
9. ~~**J 組**（JC/PapyrusUtil 模板）~~ ✅ **storageWrites 已落地 2026-06-20**（StorageUtil per-Form KV；ActorUtil/MiscUtil/ref-target 留尾）→ follower 輕量狀態管理
10. ~~**M 組**（INFO 批次 + 條件模板）~~ ✅ **兩半皆已落地 2026-06-20**（條件模板 + `variants` INFO 陣列批次）→ ambient commentary 生成效率
11. ~~**A 組 #9**（UpdateCurrentInstanceGlobal）~~ ✅ **已落地 2026-06-17** → gather 型任務 per-instance 計數 objective
12. ~~**K 組**（quest stage global write spec）~~ ✅ **已落地 2026-06-20**（stage 側；alias 側未做）→ 生成器覆蓋率
13. ~~**D-3~7**（其餘 ini pipeline：SkyPatcher/KID/BOS/AOS）~~ ✅ **已落地 2026-06-20** → D-group SKSE loose-ini pipeline 完整（runtime 待主力機驗）

> **說明**：A 組 #5/#6 + E 組（LocType/WITimeout）、F/#3（NavmeshTester）、G/#4（法術族）已在 mod-survey-gaps.md；此處未重列。
