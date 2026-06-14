# Roadmap — 之後可做

← [INDEX](../INDEX.md)｜已完成的見 [feature-dev/landed](feature-dev/landed/README.md)、解碼依據見 [investigation/decode](investigation/decode/README.md)

**確定未來會做、但不確定何時**做的 backlog（比 [ideas](idea/ideas.md) 的「不確定要不要做」更篤定；非當前 in-flight——in-flight 在各工作流 session-log）。階梯：idea → **roadmap** → [spec](specs/README.md) → [plan](plans/README.md) → build。

---

## 待補清單（解碼浮現，按優先序）

1. **scene Dialog action 的 `Emotion`/`EmotionValue`** + 泛化 scene phase fragment（不只 PlayIdle，能跑 SetStage 等）：VIGILANT 演出靠 headtrack+emotion 取代 CAMS（78 cutscene、0 CAMS → CAMS 可延後）。
2. **worldspace LAND 高度圖**（自訂地圖地形，VIGILANT realm 的本體）、region-driven weather（REGN）—— 待先確認 ModForge worldspace builder 現況。
- **Scene 演出續做**：PlayIdle / 手勢動畫；camera shot（VIGILANT 證明可延後）。
- **多解 SM 事件**（SkillIncrease/Jail/Bribe…，須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- **新 record**：Imagespace / Word of Power 等。（Music + Hazard 已落地，見 [feature-dev/landed](feature-dev/landed/README.md)。）
- **自訂技能樹生成（Custom Skills Framework / CSF）** — 讓 ModForge 從 spec 生成一棵自訂技能樹（VIGILANT/GLENMORIL/Constellations 同款）。研究已完成：分析見 [mod-survey/custom-skills-framework.md](../sub_projs/mod-survey/custom-skills-framework.md)，實作指南見 [mod-survey/custom-skill-tree-guide.md](../sub_projs/mod-survey/custom-skill-tree-guide.md)。
  - **MVP scope**：esp 端＝PERK（既有 perk 支援可重用，注意 [[perk-conditiontabcount-ctd]]）+ 3 個 GLOB（level/ratio/legendary）+ Advance/Book/Workbench KYWD；資產端＝產生器輸出 `Data/SKSE/Plugins/CustomSkills/<X>.json`（form-ref 用 `"Plugin.esp|FormId"`，與 ModForge FormId 流程契合）+ 可選 `SKILLS.json`（掛進原版技能頁）+ 一支 init alias script + 訓練 TIF。
  - **進階（後排）**：Fortify-skill 附魔/藥水支援需 `ActorValueData/*.toml` + 原生 SKSE plugin（純 esp+json 做不到）。
  - **下一步**：idea → 寫 [spec](specs/README.md)（spec 欄位長相草案已在指南 §10），再 plan → build。
- **OAR 動畫替換生成（Open Animation Replacer）** — 讓 ModForge 從 spec 生成一個 OAR 替換 mod（condition-based 動畫替換，DAR 後繼、最高槓桿的動畫整合目標）。分析見 [idea/animation/integration-layer.md](idea/asset-pipelines/animation/integration-layer.md) §5，實作指南見 [mod-survey/action-system/oar-replacer-guide.md](../sub_projs/mod-survey/action-system/oar-replacer-guide.md)。
  - **可生成範圍（純確定性 folder+JSON，不需 esp）**：`OpenAnimationReplacer\<Mod>\<Submod>\` 樹 + replacer-mod/submod `config.json`（name/description/priority/conditions）。OAR 的 condition 模型對應 ModForge 既有 CTDA condition 支援；form-ref 用 `Plugin|FormID`。variants/presets/functions/進階 submod 設定可分階段補。
  - **輸出規格已用真實 moveset 驗證**（57 個 Animatecc 範例拆解見 [mod-survey/action-system/findings/movesets-examples.md](../sub_projs/mod-survey/action-system/findings/movesets-examples.md)）：root `{name,author,description}` + submod `{name,description,priority,conditions[]}`；NPC moveset 配方＝`IsEquippedType(右)+IsEquippedType(左)+IsActorBase¬player(Skyrim.esm|0x7) [+IsRace][+Random]`；含 IsEquippedType 武器型 enum 表。DAR 的 `_CustomConditions/<priority>/_conditions.txt` DSL 為相容後路。
  - **已知可用 graph-variable 條件源**（生「方向/招式變體動畫包」的一等輸入，2026-06-14 action-system 批次浮現）：`BFCO_iAttackVariants`（[BFCO](../sub_projs/mod-survey/action-system/findings/bfco.md) 攻擊變體，附 `CompareValues` JSON 範例）、`DirecionalCycleMoveset`/`CameraMovementCMF` 八向整數（[DMK](../sub_projs/mod-survey/action-system/findings/directional-movement-keys.md)）。→ 八向移動包、N 段連擊包可模板量產。
  - **邊界**：`.hkx` 動畫本體不在此功能內（屬 [animation/havok-blender](idea/asset-pipelines/animation/havok-blender.md) 線）；OAR 需先跑一次 Nemesis/Pandora 建 base behavior（玩家端前置）。
  - **下一步**：✅ spec 草案已產出 — [specs/action-system-asset-generation-design.md](specs/action-system-asset-generation-design.md)（OAR 為主交付，含 `npcMoveset` 語法糖）。待自審 → plan → build。
- **BDI config 生成器（Behavior Data Injector）** — 讓 ModForge 從 spec 生成 BDI config，往 behavior project 注入自訂 graph variable（Int/Bool/Float）+ animation event，**免 Nemesis/behavior patch**。調查見 [mod-survey/action-system/findings/behavior-data-injector.md](../sub_projs/mod-survey/action-system/findings/behavior-data-injector.md)。
  - **價值**：給 NPC/follower 加自訂狀態變數（戰意、好感階段…）供動畫 annotation 與 OAR 條件讀，全程不碰 behavior binary、不寫 esp script——與 OAR 生成器是同一條「動畫驅動狀態」鏈的上游（鏈圖見 [action-system README](../sub_projs/mod-survey/action-system/README.md)）。
  - **scope**：`{projectPath, variables:[{name,type,default}], events:[name]}` → BDI config。**格式已從實檔驗證**（DMK/BFCO 隨附的 `*_BDI.json`）：flat JSON array of `{ "projectPath":"Actors", "type":"kInt|kBool|kFloat|kEvent", "name":..., "value":... }`（event 省 value），放 `SKSE/Plugins/BehaviorDataInjector/<x>_BDI.json`。實作幾近零風險。schema 與樣本見 finding。
  - **邊界**：動畫端的 `PIE.@SGVI/SGVF` 與 AMR `animmotion` annotation 屬 hkanno 動畫管線（非 esp record），僅在 ModForge 接 hkanno 工具鏈後才談生成。
  - **下一步**：✅ 已併入 [specs/action-system-asset-generation-design.md](specs/action-system-asset-generation-design.md)（子生成器 B，與 OAR/PIE 同一 spec）。PIE `.ini` 巨集表為子生成器 C。
- **ModForge ↔ Pandora 整合（behavior 生成步驟）** — OAR/自訂動畫的 behavior 基底由 Pandora 產生（2026 取代 Nemesis/FNIS）。調查見 [mod-survey/action-system/pandora.md](../sub_projs/mod-survey/action-system/pandora.md)。模型＝**shell-out**（同 ModForge 驅動 Papyrus/xLODGen）：產出 records+OAR config+.hkx → `Pandora --auto_run --auto_close -o … --tesv:…`。**不能 library 嵌入**（plugin API 不穩）。**spike（需實機）**：① Manjaro 上 native dotnet vs Proton-wrap；② 自動化跑能否 displayless（headless 是 Pandora 未解 feature request，可能需 xvfb）。

## mod-survey 浮現的 record/生成缺口（2026-06-14，按價值）

> ⚠️ 下列缺口多由 survey agent **推斷**、未核 ModForge code，已知部分有誤（ModForge 其實已有 forced/uniqueActor/createObject/findMatching/alias-script 等 alias fill）。**先做一次 code 驗證 pass** 再認定；能做的降級/標掉，真缺的留下補正確 scope。

一輪 mod 挖掘（survey 在 [sub_projs/mod-survey/](../sub_projs/mod-survey/)）反覆撞到的 ModForge 生成缺口；多個 mod 共用 → 優先。每條標來源 + scope。

1. **建立新 FormList（FLST）+ 填 form ref** — 現在 `formLists[]` 只能引用 vanilla、不能建新。來源：Spellforge（整套靠索引對齊的自訂 FLST）。**通用、最高價值**（遠超 magic）。scope：新 FLST record 生成 + ref 填充（含對自家 esp 內 record 的引用）。
2. **獨立 SM branch/quest-node 子樹 + keyword 路由多候選 quest** — 現在只能把單一 quest 的 `storyEvent` 掛 vanilla 根。來源：Extended Encounters、Immersive World Encounters。隨機事件/遭遇系統核心。scope：生成 SMBN/SMQN 子樹 + 條件路由。參 [[story-manager-kill-recipe]]。
3. **`MagicEffectSpec` 加 script-attach (VMAD) 欄位** — 能設 `archetype="Script"` 卻綁不上 .psc。來源：Arrowblock。scripted MGEF 必需。scope：MGEF 的 VMAD/script property 綁定（仿既有 dialogue/quest fragment 機制）。
4. **alias 從 LeveledNpc 清單填** — 現有 alias fill 模式不能 roll LVLN。來源：Immersive World Encounters、Missives(待確認)。遭遇/radiant 變化核心。scope：alias fill 新增 LVLN 模式。
5. **`placements[]` 加 `linkedRef` 欄位（+ keyword 變體）** — linked-ref 節點鏈＝馬車路線/巡邏路徑的純資料表示。來源：Animated Carriage。scope：placement 間 linkedRef + 具名 keyword link。
6. **Perk entry-point `AddActivateChoice`/`SetText` + fragment 膠水** — 不在 ModForge EntryPoint 表。來源：Immersive Interactions。情境化「啟用」選項。scope：擴充 EntryPointTabCount 表 + perk fragment（注意 [[perk-conditiontabcount-ctd]]）。
7. **package/marker 目標指向 quest alias（alias 間接）** — 動態演出 package 需要。來源：Immersive World Encounters。scope：package/PatrolData target 支援 alias 引用。
8. **navmesh-tester 動態生怪 Papyrus 模板** — 「在玩家附近隨機合法點生成」。來源：Extended Encounters。scope：可重用 Papyrus 模板，補既有 [[programmatic-navmesh]] 的預置法。
9. **程序化法術族生成器（高階）** — school × level × delivery 網格 → 對齊的 MGEF+SPEL+tome 集。來源：Spellforge（其目錄的反向）。scope：高階 generator，依賴 #1。

（DAR/OAR `_conditions.txt` 生成器已併入上方 OAR 功能項。）

## 結構／工具

- ~~**大檔拆分門檻改用 bytes**~~ ✅ 已改（trigger-to-review、非硬上限；本質不可分可超標；archive/ 與 code-map/ 豁免）。**門檻分兩套**：`workflows/` 開發流程文檔 **8192 bytes**；`docs/` 使用手冊文檔 **300 行**；`src/`、`examples/` **300 行**。DEV-GUIDE 觸發 A + conventions 已同步。
  - **docs/ 拆檔（2026-06-14）**：`SPEC-world`(485)→ `SPEC-world`+`SPEC-worldspaces`；`SPEC-dialogue-quests`(653)→ `SPEC-dialogue`+`SPEC-quests`+`SPEC-identities`。tracked EN docs 零斷鏈。
  - ~~**待辦：zh-TW 鏡像重新對齊**~~ ✅ 已做（2026-06-14）：`docs/zh-TW/` 整批重譯並 1:1 鏡像 EN `docs/`——spec 移入 `spec/` 子夾、`SPEC-dialogue-quests`→dialogue+quests+identities、`SPEC-world`→world+worldspaces、新增 `SPEC-refs`、補 `local-skyrim-extraction`；`asset-pipelines/` 孤兒鏡像已刪（EN 正本在 `workflows/idea/`，不屬使用手冊）。逃出鏡像樹的連結補一層 `../`（zh-TW 深一層）；137 條鏡像內連結零斷鏈。`engine-internals` 標題保留英文（跨檔 anchor 目標）。html bundle 經 `generate.py` 重生（31 頁）。

  ### workflows 文檔拆檔（已調查，2026-06-14；待執行）

  一輪 per-workflow agent 審查已備好拆分地圖。**範圍決定**：此次只動 **`workflows/` 文檔**（`src/` 另排）。**豁免**：`*/archive/`（封存件凍結保脈絡、不在維護鏈）與 `common/code-map/`（CODE_MAP 是 code 鏡像，依**程式碼領域**而非 byte 分檔）一律**不套** 8192-byte 規則。

  待執行清單（按工作量）：
  - ~~**tooling.md**（9.6K）→ 升 L2：`tooling/` + README，按職責分 `env-vars` / `binaries` / `data-assets`~~ ✅ 已拆（`workflows/tooling/`）。
  - ~~**feature-dev/landed.md**（14K）→ `landed/` + INDEX，**對齊 CODE_MAP 五分法**（dialogue-quests / world / items-magic / npcs / infra）~~ ✅ 已拆（`landed/` + README index）。`infra.md` 5.1K 略超但維持 CODE_MAP 粒度（voice 濃縮句明細在 memory/git）。gotchas.md（5K）只需檔內分節，不拆（暫緩）。
  - ~~**plans/** 巨型多階段計畫 → 升 L4 拆 per-Task~~ ✅ **改決定**：已完成的 plan **不拆**，直接移 `plans/archive/`（凍結、不在維護鏈、不套門檻）。9 個現役 plan 全已落地 → 全移 archive；現役 plans/ 清空（待下個 in-flight 才有新 plan）。同理 **specs/** 9 份 design 也全移 `specs/archive/`（維持 spec↔plan 配對）。維護鏈外部連結（ideas / CODE_MAP.dialogue-quests 的 identity 引用）已改指 archive。
  - **idea/**：
    - ✅ `02` particle-vfx → `particle-vfx/`（L4，efsh-record-layer / particle-nif-wall）。
    - ✅ `04` map-scene → `map-scene/`（L4，layout-extraction / geometry / workflow-modforge）。
    - ✅ `05` animation → `animation/`（L4，havok-blender / integration-layer / linux-workflow-modforge）。
    - ✅ `voice-clone/01-engine-setup` → `voice-clone/engine-setup/`（按引擎：f5/chatterbox/gptsovits/fish-speech）。
    - **改判定：`01`/`03` 概覽 KEEP 不瘦身**——它們是連貫研究報告（≠混雜索引），已有「已展開子工作流 →」指標；硬瘦身會丟研究內容，套用「不可分敘事 KEEP」原則。
    - **`ideas.md`（18K）緩**：入口主檔，按主題拆風險高、價值低，暫不動。
    - 步驟檔（model-porting 01~10、voice-clone 02~06）**全 KEEP**（已是拆分結果）。
  - ~~**specs/** 現役 8 份 design 全 KEEP~~ ✅ 已隨 plans 一併移 `specs/archive/`（見上條；一份 spec/plan=一個整體不拆，完成即進凍結 archive）。
  - **investigation/decode/** 解碼筆記**全 KEEP**（單篇連貫）；真議題是 decode/ 是否按 mod 開子夾，建議**等下個 mod 解碼進來再分**。~~`notes-gemini-voice` 微超標且自述「處理完可刪」~~ ✅ 已刪。
  - **refactor**（L2，0 超標）免動。

  順手待辦：~~docs→workflows 殘留舊路徑 `docs/minor/ideas.md`、`docs/CODE_MAP.*.md`~~ ✅ 維護鏈上的 live 檔（model-porting/voice-clone 05、blender-layout）已校正指向 `workflows/common/code-map/`；**archive 內的舊路徑保持凍結**（歷史 build 指令、不在導航鏈，依封存慣例容忍 stale）。`docs/zh-TW/` 鏡像的同類舊路徑屬翻譯同步，另計。plan→spec 反向連結缺 `Design doc:` 行的也都在 archive、凍結。

## 已有設計、待續

- **身份系統 ③ 聲望/行為追蹤**：需先定設計（GLOB 好感度系統是現成藍圖，見 sofia-patch 的 F6 分析）。in-flight 細節見 [feature-dev/session-log](feature-dev/session-log.md)。
