# mod-survey 缺口 — 🏘️ 人口 / 聚落量產缺口

← [mod-survey-gaps](../mod-survey-gaps.md)

（人口 mod 調查 2026-06-24，餵 idea #22）

調查 8 個人口類 mod（[Populated 全家桶](../../../../../analysis/mod-survey/findings/populated-skyrim-family.md)＋[prison-cells](../../../../../analysis/mod-survey/findings/populated-prison-cells.md)、[Immersive Citizens AIO](../../../../../analysis/mod-survey/findings/immersive-citizens-ai-overhaul.md)、[Immersive Wenches](../../../../../analysis/mod-survey/findings/immersive-wenches.md)、[Cutting Room Floor](../../../../../analysis/mod-survey/findings/cutting-room-floor.md)、[settlement-npc-expansions](../../../../../analysis/mod-survey/findings/settlement-npc-expansions.md)、[wench-derivatives](../../../../../analysis/mod-survey/findings/wench-derivatives.md)、[JK's set-dressing](../../../../../analysis/mod-survey/findings/jks-skyrim-setdressing.md)）。**一致結論：每個低階機制都已 landed**（NPC base、全 PACK 模板、LeveledNpc、ACHR placement、additive cell-override、Vendor faction、RELA、SM 觸發 scene、radiant quest、MCM——各 finding 附 `src/` symbol evidence）。缺口集中在**便利層 + 一個小 PACK 模板**：

1. ✅ **聚落量產 macro-expansion spec section（最高價值，= idea #22 待深挖 a）— MVP 已落地 2026-06-25**：`settlements:` macro（`Spec.Settlement.cs` / `Generator.Settlements.cs` / `Generator.Validate.Settlements.cs`），pass-0 把具名住民展開成 ACHR placement + Sleep/Work/Wander 綁錨點 package + faction 三件套 + 可選 vendor FACT/chest；零新 record、零 runtime 腳本、離線可驗。順手修了 sandbox/sleep location 對 in-spec 錨點 eager-解析→NearSelf 的舊坑（改 deferred）。example `settlement_spec.json`、SPEC-world、schema、CODE_MAP 同步，834 測綠。**剩主力機實機驗收**（NPC 是否真走到工作站/上床）見 [WAIT_USER](../../../WAIT_USER.md)。設計方案見 [archive](../../specs/archive/README.md)。下方 5 原型是該設計的參數化來源（Phase 2 = crowd/leveled/controller 那幾格）：
   - **靜態密度**（Populated 系）：base + package + ACHR mass placement，無 controller。
   - **腳本生怪**（Immersive Wenches）：XMarker spawn point + LeveledNpc + count GLOB + controller script。
   - **固定具名住民**（CRF）：unique NPC + faction 三件套（town/vendor/house）+ per-NPC 日程 + 可選在地 radiant。
   - **店家/服務面**（settlement-npc-expansions）：**per-NPC Vendor faction**（非 rank 公會，是迷你商圈：Vendor flag + 營業時段 + sellBuyList FLST + MerchantContainer）。
   - **室內抽卡填充**（prison-cells）：carrier NPC → LeveledNpc 兩層 template 抽卡。
   建議參數雛形：`settlementPopulation:`/`wildernessPopulation:`/`spawnPoints[]`（cell + markers + residents/leveled + count-global + 可選 controller + dailySchedule + shops）。
2. ❌ **`flee` PACK template（小 record 缺口，來源 Immersive Citizens AIO）** — Flee-template package + 預擺安全點（Location to Flee）+ 可選 CombatStyle，讓受襲聚落有反應（平民逃、守衛迎戰）。慢活聚落需要。
3. 📌 **配方鐵律（非缺口，是借鏡）**：日程 package **必須綁實際擺放的床/攤位/工作站 ref**——純抽象 sandbox 會讓 NPC 呆站（ICAIO/CRF/settlement-expansions 三方印證）。聚落量產 section 生 NPC 時須連帶生家具錨點。這跟 [Godot 程序化擺放](../../../../godot-worldspace-editor/design/stitching.md#相關gdscript-程序化擺放) 接得上（擺家具時一併產日程錨點）。
4. 🧊 **輕量便利層（低優先）**：`leveledListInject[]`（純資料把 form 注入既有 LeveledNpc，來源 Deadly Wenches）；非破壞 `enableState[]` toggle（CRF 的 ChangeLocation 狀態機 enable/disable 既有 ref）。
5. ✅ **set-dressing = placement volume（非缺口，已解）** — JK's 系 18550 靜態 REFR/零任務，`cellrefs` 欄位與 Godot 編輯器 `placements.json` **1:1 對齊**；聚落佈景是 placement-volume 問題，現有 placement 管線 + Godot 編輯器即天然 authoring 工具，BOS 作 runtime 補。

> **延伸調查**：缺口 #2（alias indirection）、#3（navmesh-tester）以及 LVLN fill（partial）均由 IWE/EE encounter mod 調查交叉驗證，見 [../../analysis/mod-survey/findings/encounter-mods.md](../../../../../analysis/mod-survey/findings/encounter-mods.md)。

> 校正前的原始推斷清單見 git 歷史（commit 前一版）。撤銷/降級的依據全為實際 builder symbol。
