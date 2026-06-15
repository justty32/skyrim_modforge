# Roadmap — 生成能力待補清單

← [roadmap](README.md)

ModForge 該長出的**生成能力** backlog（解碼浮現 + 已有設計待續）。階梯：idea → roadmap → [spec](../specs/README.md) → [plan](../plans/README.md) → build。

## 待補清單（解碼浮現，按優先序）

1. **scene Dialog action 的 `Emotion`/`EmotionValue`** + 泛化 scene phase fragment（不只 PlayIdle，能跑 SetStage 等）：VIGILANT 演出靠 headtrack+emotion 取代 CAMS（78 cutscene、0 CAMS → CAMS 可延後）。
2. **worldspace LAND 高度圖**（自訂地圖地形，VIGILANT realm 的本體）、region-driven weather（REGN）—— 待先確認 ModForge worldspace builder 現況。
- **Scene 演出續做**：PlayIdle / 手勢動畫；camera shot（VIGILANT 證明可延後）。
- **多解 SM 事件**（SkillIncrease/Jail/Bribe…，須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- **新 record**：Imagespace / Word of Power 等。（Music + Hazard 已落地，見 [feature-dev/landed](../feature-dev/landed/README.md)。）
- **自訂技能樹生成（Custom Skills Framework / CSF）** — 讓 ModForge 從 spec 生成一棵自訂技能樹（VIGILANT/GLENMORIL/Constellations 同款）。研究已完成：分析見 [mod-survey/custom-skills-framework.md](../../sub_projs/mod-survey/custom-skills-framework.md)，實作指南見 [mod-survey/custom-skill-tree-guide.md](../../sub_projs/mod-survey/custom-skill-tree-guide.md)。
  - **MVP scope**：esp 端＝PERK（既有 perk 支援可重用，注意 [[perk-conditiontabcount-ctd]]）+ 3 個 GLOB（level/ratio/legendary）+ Advance/Book/Workbench KYWD；資產端＝產生器輸出 `Data/SKSE/Plugins/CustomSkills/<X>.json`（form-ref 用 `"Plugin.esp|FormId"`，與 ModForge FormId 流程契合）+ 可選 `SKILLS.json`（掛進原版技能頁）+ 一支 init alias script + 訓練 TIF。
  - **進階（後排）**：Fortify-skill 附魔/藥水支援需 `ActorValueData/*.toml` + 原生 SKSE plugin（純 esp+json 做不到）。
  - **下一步**：idea → 寫 [spec](../specs/README.md)（spec 欄位長相草案已在指南 §10），再 plan → build。
- **OAR 動畫替換生成（Open Animation Replacer）** — 讓 ModForge 從 spec 生成一個 OAR 替換 mod（condition-based 動畫替換，DAR 後繼、最高槓桿的動畫整合目標）。分析見 [idea/animation/integration-layer.md](../idea/asset-pipelines/animation/integration-layer.md) §5，實作指南見 [mod-survey/action-system/oar-replacer-guide.md](../../sub_projs/mod-survey/action-system/oar-replacer-guide.md)。
  - **可生成範圍（純確定性 folder+JSON，不需 esp）**：`OpenAnimationReplacer\<Mod>\<Submod>\` 樹 + replacer-mod/submod `config.json`（name/description/priority/conditions）。OAR 的 condition 模型對應 ModForge 既有 CTDA condition 支援；form-ref 用 `Plugin|FormID`。variants/presets/functions/進階 submod 設定可分階段補。
  - **輸出規格已用真實 moveset 驗證**（57 個 Animatecc 範例拆解見 [mod-survey/action-system/findings/movesets-examples.md](../../sub_projs/mod-survey/action-system/findings/movesets-examples.md)）：root `{name,author,description}` + submod `{name,description,priority,conditions[]}`；NPC moveset 配方＝`IsEquippedType(右)+IsEquippedType(左)+IsActorBase¬player(Skyrim.esm|0x7) [+IsRace][+Random]`；含 IsEquippedType 武器型 enum 表。DAR 的 `_CustomConditions/<priority>/_conditions.txt` DSL 為相容後路。
  - **已知可用 graph-variable 條件源**（生「方向/招式變體動畫包」的一等輸入，2026-06-14 action-system 批次浮現）：`BFCO_iAttackVariants`（[BFCO](../../sub_projs/mod-survey/action-system/findings/bfco.md) 攻擊變體，附 `CompareValues` JSON 範例）、`DirecionalCycleMoveset`/`CameraMovementCMF` 八向整數（[DMK](../../sub_projs/mod-survey/action-system/findings/directional-movement-keys.md)）。→ 八向移動包、N 段連擊包可模板量產。
  - **邊界**：`.hkx` 動畫本體不在此功能內（屬 [animation/havok-blender](../idea/asset-pipelines/animation/havok-blender.md) 線）；OAR 需先跑一次 Nemesis/Pandora 建 base behavior（玩家端前置）。
  - **狀態**：✅ **MVP 已落地（2026-06-14）** — `animationReplacers` spec block + OAR 條件序列化器 + `npcMoveset` 語法糖 + package 擺檔，全離線測試（落地記錄 [feature-dev/landed/infra.md](../feature-dev/landed/infra.md)、[SPEC-animation](../../docs/spec/SPEC-animation.md)）。**之後**：variants/presets/functions、DAR 後路輸出、`importanim` shell-out。
- **BDI config 生成器（Behavior Data Injector）** — 讓 ModForge 從 spec 生成 BDI config，往 behavior project 注入自訂 graph variable（Int/Bool/Float）+ animation event，**免 Nemesis/behavior patch**。調查見 [mod-survey/action-system/findings/behavior-data-injector.md](../../sub_projs/mod-survey/action-system/findings/behavior-data-injector.md)。
  - **價值**：給 NPC/follower 加自訂狀態變數（戰意、好感階段…）供動畫 annotation 與 OAR 條件讀，全程不碰 behavior binary、不寫 esp script——與 OAR 生成器是同一條「動畫驅動狀態」鏈的上游（鏈圖見 [action-system README](../../sub_projs/mod-survey/action-system/README.md)）。
  - **scope**：`{projectPath, variables:[{name,type,default}], events:[name]}` → BDI config。**格式已從實檔驗證**（DMK/BFCO 隨附的 `*_BDI.json`）：flat JSON array of `{ "projectPath":"Actors", "type":"kInt|kBool|kFloat|kEvent", "name":..., "value":... }`（event 省 value），放 `SKSE/Plugins/BehaviorDataInjector/<x>_BDI.json`。實作幾近零風險。schema 與樣本見 finding。
  - **邊界**：動畫端的 `PIE.@SGVI/SGVF` 與 AMR `animmotion` annotation 屬 hkanno 動畫管線（非 esp record），僅在 ModForge 接 hkanno 工具鏈後才談生成。
  - **狀態**：✅ **已落地（2026-06-14）** — `behaviorData` spec block → `BdiGen`（flat JSON array，格式實檔驗證）。PIE `.ini` 巨集表（`payloadMacros`→`PieGen`）一併落地。見 [SPEC-animation](../../docs/spec/SPEC-animation.md)。
- **ModForge ↔ Pandora 整合（behavior 生成步驟）** — OAR/自訂動畫的 behavior 基底由 Pandora 產生（2026 取代 Nemesis/FNIS）。調查見 [mod-survey/action-system/pandora.md](../../sub_projs/mod-survey/action-system/pandora.md)。模型＝**shell-out**（同 ModForge 驅動 Papyrus/xLODGen）：產出 records+OAR config+.hkx → `Pandora --auto_run --auto_close -o … --tesv:…`。**不能 library 嵌入**（plugin API 不穩）。**spike（需實機）**：① Manjaro 上 native dotnet vs Proton-wrap；② 自動化跑能否 displayless（headless 是 Pandora 未解 feature request，可能需 xvfb）。

## 已有設計、待續

- **身份系統 ③ 聲望/行為追蹤**：需先定設計（GLOB 好感度系統是現成藍圖，見 sofia-patch 的 F6 分析）。in-flight 細節見 [feature-dev/session-log](../feature-dev/session-log.md)。
