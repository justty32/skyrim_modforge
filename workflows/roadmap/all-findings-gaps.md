# Roadmap — Findings 缺口全集

← [roadmap](README.md)

> **本檔由人工逐檔閱讀 `sub_projs/mod-survey/findings/` 全部 32 個檔案後整合（2026-06-15）。**  
> 每條缺口附來源 findings、scope 與優先級。已在其他 roadmap 檔記錄的缺口打 `→ 見 X.md`；新缺口標 `🆕`。  
> 優先序：🔴 高（alts 沒有就做不了某類 mod）→ 🟡 中（有替代路但繁瑣）→ 🟢 低（nice-to-have）。

---

## A 組：Radiant Quest / Alias 填充系統（前置必備）

這組是「能不能生 radiant 任務」的根基。來源：**missives.md**、**encounter-mods.md**、**sm-subsystem.md**。  
→ 缺口 #7 #8 #9 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 7 | ✅ QuestAlias `findMatchingLocation` fill（LocationAlias 型）—— radiant 地點隨機化根基 | ✅ 落地 06-17 |
| 8 | ✅ QuestAlias 在地點內找 ref（`findInLocationAlias`，scope 校正：非 ALNA 而是 LocationAliasReference）| ✅ 落地 06-17 |
| 9 | ✅ `UpdateCurrentInstanceGlobal` fragment codegen——gather/計數型 quest objective 文字即時更新 | ✅ 落地 06-17 |

**PARTIAL（多數已支援，留窄缺口）：**
- SM branch/quest-node 多層巢狀 SMBN（→ [mod-survey-gaps.md](mod-survey-gaps.md) ⚠️ partial）
- LVLN alias fill 一等模式（→ [mod-survey-gaps.md](mod-survey-gaps.md) ⚠️ partial）

---

## B 組：Perk 互動式 Entry-Point + Fragment

來源：**perk-entry-points.md**、**arrowblock.md**、**immersive-interactions.md**。  
→ 缺口 #1 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 1 | ✅ Perk `AddActivateChoice`/`SetText` + PerkAdapter VMAD + fragment 生成器 | ✅ 落地 06-17 |

---

## C 組：Package 目標/地點 → Quest Alias 間接

來源：**encounter-mods.md**、**sm-subsystem.md**。  
→ 缺口 #2 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 2 | ✅ Package `target`/`location` 支援 quest alias index（`PackageTargetAlias`）—— radiant 演出 package 必需 | ✅ 落地 06-17 |

---

## D 組：SKSE Plugin 輔助輸出 Pipeline（次要輸出檔）

ModForge 的 `build` 主輸出是 `.esp`。這組是對應 SKSE framework 的 **ini/json 輔助輸出**，讓 mod 作者免寫 ESP override 就能做到 NPC 標記、物件替換、動畫切換等功能。

### ✅ D-1 SPID `_DISTR.ini` 輸出（**已落地 2026-06-17**）
**來源：** spid.md、common-framework-mods.md  
**狀態：** ✅ 離線實作 `spidDistributions:` spec section → mod 根 `<file>_DISTR.ini`（loose，非 esp）。`Spec.SpidDistribution.cs`（DTO）+ `SpidGen.cs`（emitter，尾段 NONE 修剪、Item count/Package index 第 6 欄）+ `Generator.Validate.Spid.cs`（type/record/chance 校驗）+ `Package.cs` 接 loose-file 段。**14 測綠**；格式逐欄查 spid.md（SPID 7.3）。docs [SPEC-distribution.md](../../docs/spec/SPEC-distribution.md)、example `spid_distribution_spec.json`、schema 已同步。  
支援 spell/perk/faction/keyword/package/outfit/item 分發給 NPC（依 race/faction/keyword/level/trait 條件）。  
**用途：** 無 ESP patch 給 follower/NPC 加 faction、標記 keyword、分發 ability spell；OAR 條件讀 faction → animation 切換。

### ✅ D-2 MCM Helper `config.json`/`settings.ini` 輸出（**已落地 2026-06-20**）
**來源：** mcm-helper.md、mcm-helper-config-json.md  
**狀態：** ✅ 離線實作 `mcmConfigs:` spec section → `MCM/Config/<modName>/config.json`（menu 佈局）+ `settings.ini`（預設值）。`Spec.Mcm.cs`（DTO）+ `McmGen.cs`（`System.Text.Json` 生 config.json + ini）+ `Generator.Validate.Mcm.cs` + `Package.cs` 接 loose-file 段。**17 測綠**；格式逐欄查 mcm-helper-config-json.md（MCM Helper 1.6.1）。docs [SPEC-distribution.md](../../docs/spec/SPEC-distribution.md)、example `mcm_config_spec.json`、schema 已同步。  
控件：header/empty/toggle/slider/stepper/enum/keymap/hiddenToggle + group/position 佈局；`sourceType: ModSettingBool/Int/Float/String`；id `key:Section` → ini `[Section] key=`。  
**MVP 範圍：** 純 ini-backed（`ModSetting*`）**零 Quest/Papyrus/master**——DLL 全自動。`PropertyValue*`/`action.CallFunction`（需 Quest 掛 `MCM_ConfigBase` script）**範圍外，validate 擋掉**，留待日後接 Papyrus host。  
**用途：** 任何需要玩家設定面板的 mod（難度、開關功能、follower 行為調整）。  
⚠️ **待主力機：** live menu 只能實機驗（裝 MCM Helper + SkyUI，看選單渲染/存讀）；ModForge 只驗結構。

### 🆕 D-3 SkyPatcher ini 輸出
**來源：** skypatcher.md  
**現況：** ModForge 不輸出 SkyPatcher ini。  
**Scope：** `skyPatcher:` spec section → `SKSE/Plugins/SkyPatcher/<mod>.ini`。  
支援 29 record types 的 in-memory runtime patch（Hair/Eye/Skin/WNAM/ANAM/…），語法類 SPID。  
**用途：** 批量修改大量 vanilla NPC 外觀（不衝突，不生 ESP）；NPC 美化 pipeline。  
**優先：** 🟡（專門用途，與 SPID 互補）

### ✅ D-4 FormList Manipulator `_FLM.ini` 輸出（**已落地 2026-06-20**）
**來源：** formlist-manipulator.md（v1.8.1）、flst-factory.md  
**狀態：** ✅ 離線實作 `formListInjects:` spec section → `<file>_FLM.ini`（mod 根＝`Data/`）。`Spec.FormListInject.cs`（DTO）+ `FlmGen.cs`（emit）+ `Generator.Validate.Flm.cs` + `Package.cs` 接 loose-file 段。**11 測綠**；格式逐欄查 formlist-manipulator-config-core/-advanced.md。docs [SPEC-distribution.md](../../docs/spec/SPEC-distribution.md)、example `formlist_inject_spec.json`、schema 已同步。  
正確語法＝`FormList = <FList>|<forms>|<Filter>`（非 roadmap 原寫的舊 `Form=/Target=`）；涵蓋 FormList 操作行 + Filter/Alias/Group/Collection 定義（Collection 20 FormType 白名單）。  
**MVP 範圍外：** `ModEvent`（需 Papyrus 發送）+ 特化快捷語法（Plant/BToys/GToys/HairColors/AtronachForge/…）。  
**用途：** 把自家 spell/item/NPC 零衝突加進外部 mod 的 FLST（Spellforge 法術池、SPID 分發目標 FLST、領養禮物池…）；自建 FLST 仍走 esp-side `formLists[]`。  
⚠️ **待主力機：** FLST/form ref 由玩家 load order 解析，runtime 才驗（裝 FLM DLL 開 `FormListManipulator_DEBUG.ini` 看 log 確認追加成功）；ModForge 只驗結構。

### 🆕 D-5 KID `_KID.ini` 輸出
**來源：** keyword-item-distributor.md  
**現況：** ModForge 不輸出 `_KID.ini`。  
**Scope：** `kidDistribution:` spec section → `<mod>_KID.ini`。  
語法：`Keyword|Type|ItemID|Strings|Traits|Chance`  
**用途：** 給道具批量加 keyword（品質分類、SPID 配合識別、OAR 條件）。  
**優先：** 🟢（KID 通常手寫，spec 生成效益有限）

### 🆕 D-6 BOS `_SWAP.ini` 輸出
**來源：** base-object-swapper.md、common-framework-mods.md  
**現況：** ModForge 不輸出 `_SWAP.ini`。  
**Scope：** `objectSwap:` spec section → `<mod>_SWAP.ini`。  
格式：`[Forms]`/`[References]`/`[Properties]` sections；條件欄位（faction/keyword/race/location/random）；`[Properties]` 可 override scale/activate flag。  
**用途：** follower home 根據關係進度替換裝飾物；task-based 場景換道具。  
**優先：** 🟢（場景美化用途，手寫亦可）

### 🆕 D-7 AOS `_ANIO.ini` 輸出
**來源：** animobject-swapper.md  
**現況：** ModForge 不輸出 `_ANIO.ini`。  
**Scope：** 角色化 idle 道具替換（喝酒 idle 中 Sofia 拿特定酒瓶、法師拿書）。條件支援 NPC base/faction/race/keyword。  
**用途：** follower 角色化演出包（搭配 OAR）。  
**優先：** 🟢（純視覺細節；低成本但很 low priority）

---

## E 組：Encounter 地點感知 / 冷卻機制

來源：**encounter-mods.md**（IWE/EE）。  
→ 缺口 #5 #6 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)，此處僅摘要。

| # | 缺口 | 優先 |
|---|------|------|
| 5 | ✅ LocType keyword 路由 + Hold 偵測（locationFilter + LocAliasHasKeyword）| ✅ 落地 06-17 |
| 6 | ✅ WITimeout 冷卻模式（cooldownHours → GLOB + MFEncounterCooldown script）| ✅ 落地 06-17 |

---

## F 組：NavmeshTester 動態生怪模板

來源：**encounter-mods.md**。  
→ 缺口 #3 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 3 | ✅ NavmeshTester 動態生怪 Papyrus script 模板（quest.spawn → MFDynamicSpawn）| ✅ 落地 06-17 |

---

## G 組：Perk 程序化法術族生成器

來源：**spellforge.md**、**flst-factory.md**。  
→ 缺口 #4 已登入 [mod-survey-gaps.md](mod-survey-gaps.md)。

| # | 缺口 | 優先 |
|---|------|------|
| 4 | 程序化法術族生成器（school × level × delivery 網格 → 對齊 MGEF+SPEL+tome+FLST） | 🟢 |

---

## H 組：Custom Skills Framework 技能樹生成器

來源：**constellations.md**（含 CSF v3 JSON 格式完整文件化）。  
→ 已登入 [generation.md](generation.md)（含 MVP scope + spec 欄位草案）。

**提醒**：CSF skill tree generator 是 generation.md 待補清單的一環，可接著現有 PERK 生成能力做。  
MVP 輸出：`SKSE/Plugins/CustomSkills/<X>.json` + `SKILLS.json`（整合進原版技能頁）+ 3 個 GLOB + 2 個 KYWD + Translations UTF-16 LE BOM + init alias script 模板 + 訓練 TIF。  
**優先：** 🟡（有完整設計文件，下一步寫 spec）

---

## I 組：MagicEffectSpec inline script-attach（DX 缺口）

來源：**arrowblock.md**、**mgef-vmad.md**。

🆕 **MagicEffectSpec `scripts[]` inline 欄位**  
**現況：** 通用 `scripts[].targetEditorId` 路徑**已能**把腳本掛到 MGEF（`AttachScripts()` 反射 VMAD）。`MagicEffectSpec` 無專屬 `scripts` 欄位——使用者必須拆到頂層 `scripts[]`，且文件沒說明此繞路方式。  
**Scope：** 在 `MagicEffectSpec` 加 `scripts: List<ScriptAttachSpec>` inline 欄位，讓 MGEF script-attach 和 record 定義貼在一起；更新 docs。  
**優先：** 🟢（功能已通，純 DX/文件缺口）

---

## J 組：Papyrus Script 模板（JContainers / PapyrusUtil 模式）

來源：**jcontainers.md**、**papyrusutil.md**、**common-framework-mods.md**。

🆕 **JContainers / PapyrusUtil Papyrus script 模板**  
**現況：** ModForge 不生成使用 JContainers/PapyrusUtil 的 Papyrus 腳本片段。  
**Scope：** script-template 功能（非 record），提供常見模式的 snippet：  
- `StorageUtil.SetIntValue(akActor, "key", val)` 做 per-form KV 狀態（follower 記憶、cooldown）
- `JFormDB.solveObjLN(root, ".follower.topics.lastSpoken", true)` 做 nested map 狀態表
- `ActorUtil.AddPackageOverride(akActor, pkg, 0-100)` 臨時 package 覆蓋（帶清理配對）
- `MiscUtil.ScanCellNPCs(...)` 附近 NPC 偵測  
**用途：** follower 複雜對話狀態、relationship matrix、dialogue cooldown。  
**優先：** 🟡（follower expansion 的核心需求，但屬 script-gen 功能非 record-gen）

---

## K 組：Quest Script Global Write（SetValue spec）

來源：**runtime-selector-patterns.md**。

🆕 **Quest script `SetValue(global, val)` 一等 spec 語法**  
**現況：** Global write 只能在 dialogue TIF fragment（`GetOwningQuest()` 獲取 quest script）裡做，沒有 spec-level「在 QuestStage fragment 或 alias OnInit 裡寫入 GlobalVariable」的一等支援。  
**Scope：** `QuestStageSpec` 或 `AliasSpec` 支援 `globalWrites: [{global: "MyGlobal", value: 1}]`，自動在對應 fragment 生成 `MyGlobal.SetValue(1)`；或更通用的 `scriptlets: [...]` 語法。  
**優先：** 🟡（目前手寫 fragment 可繞過，但生成器覆蓋率不完整）

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

🆕 **INFO 批次建立 + condition template 共享機制**  
**現況：** ModForge 逐條 spec INFO，無法共享 condition block（FCO 有 265 條共用相同地點/狀態條件）。  
**Scope：** spec 支援 `conditionTemplates: [...]` 定義命名條件組，`info.useTemplate: MyConditionSet` 展開繼承；或支援 INFO 陣列批次建立（同 topic 多條、共享 conditions）。  
**用途：** ambient commentary 大量生成（旅途/地點/時間/天氣/玩家狀態反應）。  
**優先：** 🟡（只要做旅途 commentary 就需要）

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
7. **H 組**（CSF skill tree）→ 接 generation.md 現有設計，已有詳細 spec 草案
8. ~~**L 組**（GetVMQuestVariable/GetVMScriptVariable 條件）~~ ✅ **已落地 2026-06-20** → follower ambient bark 品質（variableName 字串格式待主力機 xEdit 驗）
9. **J 組**（JC/PapyrusUtil 模板）→ follower 複雜狀態管理
10. **M 組**（INFO 批次 + 條件模板）→ ambient commentary 生成效率
11. ~~**A 組 #9**（UpdateCurrentInstanceGlobal）~~ ✅ **已落地 2026-06-17** → gather 型任務 per-instance 計數 objective
12. **K 組**（quest script global write spec）→ 生成器覆蓋率
13. **D-3~7**（其餘 ini pipeline）→ 按需

> **說明**：A 組 #5/#6 + E 組（LocType/WITimeout）、F/#3（NavmeshTester）、G/#4（法術族）已在 mod-survey-gaps.md；此處未重列。
