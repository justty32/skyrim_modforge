# Roadmap — mod-survey 浮現的 record/生成缺口

← [roadmap](README.md)

> ⚠️ 下列缺口多由 survey agent **推斷**、未核 ModForge code，已知部分有誤（ModForge 其實已有 forced/uniqueActor/createObject/findMatching/alias-script 等 alias fill）。**先做一次 code 驗證 pass** 再認定；能做的降級/標掉，真缺的留下補正確 scope。校正所需的機制理解見 [survey-backlog.md](survey-backlog.md) C 組（survey 補機制、code 驗證核對現況，同一件事兩面）。

一輪 mod 挖掘（survey 在 [sub_projs/mod-survey/](../../sub_projs/mod-survey/)）反覆撞到的 ModForge 生成缺口（2026-06-14，按價值）；多個 mod 共用 → 優先。每條標來源 + scope。

1. **建立新 FormList（FLST）+ 填 form ref** — 現在 `formLists[]` 只能引用 vanilla、不能建新。來源：Spellforge（整套靠索引對齊的自訂 FLST）。**通用、最高價值**（遠超 magic）。scope：新 FLST record 生成 + ref 填充（含對自家 esp 內 record 的引用）。
2. **獨立 SM branch/quest-node 子樹 + keyword 路由多候選 quest** — 現在只能把單一 quest 的 `storyEvent` 掛 vanilla 根。來源：Extended Encounters、Immersive World Encounters。隨機事件/遭遇系統核心。scope：生成 SMBN/SMQN 子樹 + 條件路由。參 [[story-manager-kill-recipe]]。
3. **`MagicEffectSpec` 加 script-attach (VMAD) 欄位** — 能設 `archetype="Script"` 卻綁不上 .psc。來源：Arrowblock。scripted MGEF 必需。scope：MGEF 的 VMAD/script property 綁定（仿既有 dialogue/quest fragment 機制）。
4. **alias 從 LeveledNpc 清單填** — 現有 alias fill 模式不能 roll LVLN。來源：Immersive World Encounters、Missives(待確認)。遭遇/radiant 變化核心。scope：alias fill 新增 LVLN 模式。
5. **`placements[]` 加 `linkedRef` 欄位（+ keyword 變體）** — linked-ref 節點鏈＝馬車路線/巡邏路徑的純資料表示。來源：Animated Carriage。scope：placement 間 linkedRef + 具名 keyword link。
6. **Perk entry-point `AddActivateChoice`/`SetText` + fragment 膠水** — 不在 ModForge EntryPoint 表。來源：Immersive Interactions。情境化「啟用」選項。scope：擴充 EntryPointTabCount 表 + perk fragment（注意 [[perk-conditiontabcount-ctd]]）。
7. **package/marker 目標指向 quest alias（alias 間接）** — 動態演出 package 需要。來源：Immersive World Encounters。scope：package/PatrolData target 支援 alias 引用。
8. **navmesh-tester 動態生怪 Papyrus 模板** — 「在玩家附近隨機合法點生成」。來源：Extended Encounters。scope：可重用 Papyrus 模板，補既有 [[programmatic-navmesh]] 的預置法。
9. **程序化法術族生成器（高階）** — school × level × delivery 網格 → 對齊的 MGEF+SPEL+tome 集。來源：Spellforge（其目錄的反向）。scope：高階 generator，依賴 #1。

（DAR/OAR `_conditions.txt` 生成器已併入 [generation.md](generation.md) 的 OAR 功能項。）
