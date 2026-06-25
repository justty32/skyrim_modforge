# Mod Survey Index

## 內容型

| Mod | Finding | Plugin | 敘事價值 | 重點 |
| --- | --- | --- | --- | --- |
| Follower Commentary Overhaul SE | [findings/follower-commentary-overhaul.md](findings/follower-commentary-overhaul.md) | `FCO - Follower Commentary Overhaul.esp` | 中 | generic follower ambient commentary；voice type + location/quest/player-state conditions |
| Improved Follower Dialogue - Lydia | [findings/improved-follower-dialogue-lydia.md](findings/improved-follower-dialogue-lydia.md) | `ImprovedCompanionsBoogaloo.esp` | 高 | unique follower arc；stage/global/VM quest variable；moral objection；scene quests |
| Relationship Dialogue Overhaul | [findings/relationship-dialogue-overhaul.md](findings/relationship-dialogue-overhaul.md) | `Relationship Dialogue Overhaul.esp` | 高 | relationship/follower system overhaul；shared info；voice type matrix；generic recruit/command compatibility |
| I'm Glad You're Here | [findings/im-glad-youre-here.md](findings/im-glad-youre-here.md) | `ImGladYoureHere.esp` | 高（動作層） | follower/family hug action service；scene protection；camera/idle/package cleanup；Sofia compatibility hooks |
| Immersive Patrols SE/AE | [findings/immersive-patrols.md](findings/immersive-patrols.md) | `Immersive Patrols II.esp` | 低（系統高） | no quest/dialogue；static placed patrols + patrol/follow packages + custom aggro factions；M&B static patrol slice reference |
| Civil War Lines Expansion | [findings/civil-war-lines-expansion.md](findings/civil-war-lines-expansion.md) | `Civil War Lines Expansion.esp` | 中 | 415 combat/idle/hello bark lines；faction/voice/equipment/location/random condition matrix；voice + seq pipeline reference |

## 框架型

| Mod | Finding | Plugin / Runtime | 參考價值 | 重點 |
| --- | --- | --- | --- | --- |
| Common Framework / Utility Mods | [findings/common-framework-mods.md](findings/common-framework-mods.md) | SPID / OAR / PapyrusUtil / JContainers / BOS / AOS / Conditional Expressions / IWH / ITH | 高（工具層） | distribution、animation conditions、state storage、object/animobject swap、expression state、collision/dialogue suppression |
| PapyrusUtil SE（深挖） | [findings/papyrusutil.md](findings/papyrusutil.md) | `PapyrusUtil.dll`（SKSE，無 ESP） | 高（狀態儲存 + cell 掃描 + package override） | StorageUtil per-form KV + list（int/float/string/Form 四型）；JsonUtil 外部 JSON 讀寫 + path API；ActorUtil package override priority 0-100；MiscUtil ScanCellNPCs/Objects、檔案操作；PapyrusUtil 陣列 push/diff/merge/slice；v4.6 |
| JContainers SE（深挖） | [findings/jcontainers.md](findings/jcontainers.md) | `JContainers64.dll`（SKSE，無 ESP） | 高（複雜資料結構 + 外部 JSON 雙向） | JArray 無上限動態陣列；JMap/JFormMap/JIntMap key-value 容器；JDB 全域資料庫（跨 mod 共享）；JFormDB per-Form 嵌套結構；JValue readFromFile/writeToFile JSON 序列化；JAtomic 原子操作；生命週期需手動 retain/release；API 4 / Feature 2 |
| Conditional Expressions（深挖） | [findings/conditional-expressions.md](findings/conditional-expressions.md) | `Conditional Expressions.esp` + 16 .psc | 高（表情層） | MFG SetModifier/SetPhoneme/SetExpressionOverride 全索引表；16 種狀態 effect 機制；busy gate 設計；三段式漸變 pattern；GlobalVariable 中介狀態可用於 dialogue condition |
| I'm Walking Here + I'm Talkin' Here | [findings/iwh-ith.md](findings/iwh-ith.md) | `ImWalkinHere.dll`（SKSE）+ `ImTalkinHere.esp` | 中（品質層） | IWH：TOML 四開關碰撞抑制，無 API，純被動；ITH：`PlayerInDialogue` Conditional property，bark condition hook；follower mod 可讀 GetScriptVariable 或自實作 PlayerBusy global |
| Nether's Follower Framework | [findings/nether-follower-framework.md](findings/nether-follower-framework.md) | `nwsFollowerFramework.esp` | 高（主要 follower 框架） | DialogueFollower slot expansion；regular vs imported followers；Sofia import/export；NoImport faction；sandbox/regard/home/storage |
| Extensible Follower Framework | [findings/extensible-follower-framework.md](findings/extensible-follower-framework.md) | `EFFCore.esm` + `EFFDialogue.esp` | 高（slot-bank follower framework） | 100 follower aliases + 100 hidden inventory containers；plugin quests；dialogue menu；alias package override stack；slotFactory reference |
| Base Object Swapper (BOS) | [findings/base-object-swapper.md](findings/base-object-swapper.md) | `po3_BaseObjectSwapper.dll`（SKSE，無 ESP） | 中（場景佈置層） | `_SWAP.ini` runtime 替換 base form；`[Forms/Properties/References/Transforms]` 四 section；FormID `0xID~Plugin` 語法；location/region/keyword/cell/worldspace filter；chance 機率；transform 覆蓋（pos/rot/scale/flags）；follower home set dressing 無 patch 方案 |
| AnimObject Swapper (AOS) | [findings/animobject-swapper.md](findings/animobject-swapper.md) | `po3_AnimObjectSwapper.dll`（SKSE，無 ESP） | 低→中（角色化演出層） | `_ANIO.ini` runtime 替換 idle ANIO；`[BaseANIO\|FILTERS\|TRAITS]` section 格式；ALL(+)/NOT(-)/MATCH/ANY(*) filter；faction/race/keyword/spell/NPC/FormList 條件；sex/child traits；多值隨機池；OAR 換動作 + AOS 換道具 配對模式 |
| SkyPatcher | [findings/skypatcher.md](findings/skypatcher.md) | `SkyPatcher.dll`（SKSE，CommonLibSSE-NG） | 高（策略層） | 通用 ini-based runtime patcher；28+ record 類型；kDataLoaded 套用；NPC 視覺/數值/法術/perk 批量修改；esp vs config 產物策略核心 |
| po3's Tweaks | [findings/po3-tweaks.md](findings/po3-tweaks.md) | `po3_Tweaks.dll`（無 esp） | 低（純前置環境標記） | 純 SKSE DLL，無 record；[Fixes]/[Tweaks]/[Experimental] ini 三區；CombatToNormal dialogue fix、Cast Added Spells on Load、Furniture in Combat 與 ModForge 生成物有交集；`IsTweakInstalled()` API 可做 graceful fallback |
| MCM Helper | [findings/mcm-helper.md](findings/mcm-helper.md) | `MCMHelper.dll` + `MCMHelper.esp`（ESL） | 高（設定選單生成候選） | JSON config.json → SkyUI MCM 自動建構；控件型別：toggle/slider/stepper/enum/keymap/header；sourceType 對應 ModSetting ini 或 script property；最小 esp 需求：一個 Quest（若有 action）；ModForge 可從 spec 生成 config.json + settings.ini |
| SPID（深挖） | [findings/spid.md](findings/spid.md) | `po3_SpellPerkItemDistributor.dll`（SKSE，無 ESP） | 高（NPC 無 patch 分發層） | 完整 _DISTR.ini 語法；12 種 type（Spell/Perk/Item/Shout/LevSpell/Package/Outfit/SleepOutfit/Keyword/DeathItem/Faction/Skin）；StringFilter/FormFilter/LevelFilter/Traits 詳解；與 KID/SkyPatcher 分工 |
| Keyword Item Distributor（KID） | [findings/keyword-item-distributor.md](findings/keyword-item-distributor.md) | `po3_KeywordItemDistributor.dll`（SKSE，無 ESP） | 中（分發層工具） | `_KID.ini` 批次把 KYWD 掛到 item/armor/weapon/MGEF 等；與 SPID 正交（KID 管物件，SPID 管 NPC）；可輸出 ini 替代 override |
| Sound Record Distributor（SRD） | [findings/sound-record-distributor.md](findings/sound-record-distributor.md) | SRD SKSE plugin | 中 | 音效分發（SPID 的 sound 版）；esp-side vs SRD 取捨 |
| FormList Manipulator（FLM） | [findings/formlist-manipulator.md](findings/formlist-manipulator.md) | `FormListManipulator.dll`（SKSE，無 ESP） | 中（FLST 追加層） | `_FLM.ini` 把 form 加入任意 FLST（含 vanilla/外部 mod），零衝突；補 ESP-side FLST 無法無衝突追加外部 FLST 的死角 |
| Enchantments and Potions Work for NPCs（EPW4NPCs） | [findings/epw4npcs.md](findings/epw4npcs.md) | 純 SPID `EPW4NPCs_DISTR.ini`（無 plugin/DLL/script） | 低（SPID 全域注入 pattern） | 整包就 2 行：把 vanilla EntryPoint perk `PerkSkillBoosts`(0xCF788)/`AlchemySkillBoosts`(0xA725C) SPID 廣播給 `ActorTypeNPC`，補回 NPC 缺的附魔/藥水 entry-point 通路；**ModForge 已可 100% 等價生成（SpidGen Perk+StringFilter 已驗證），無新缺口**；「global NPC perk via SPID」recipe 範本，對 vendor/settlements 全 NPC 注入有用；NPCsUsePotions(67489) 才是不可生成的喝藥 AI controller |

## 系統 / 機制型（2026-06-14 批次）

逐 mod 機制拆解 + 對 ModForge 的「可生成 / 需新支援 / 純參考」標記。共通缺口已彙整進 [roadmap](../../workflows/roadmap.md)「mod-survey 浮現的 record/生成缺口」。

| Mod | Finding | 機制重點 | ModForge 缺口 |
| --- | --- | --- | --- |
| Extended Encounters | [findings/extended-encounters.md](findings/extended-encounters.md) | 純 SM 驅動 ~140 遭遇；navmesh-tester 動態生怪 | SM branch/quest-node 子樹；spawn-near-player 模板 |
| Immersive World Encounters | [findings/immersive-world-encounters.md](findings/immersive-world-encounters.md) | SM 容器 quest + Scene(Package/Timer/Dialog) | LVLN alias fill；package target=alias |
| Missives | [findings/missives.md](findings/missives.md) | 公告板 radiant 工廠（Activator+FLST+Quest.Start，無 SM）；alias findMatching 填 | FLST 建立（最高價值）；LVLN/alias 間接 |
| Spellforge | [findings/spellforge.md](findings/spellforge.md) | 預製 SPEL 池、索引對齊 FLST、非 runtime 組裝 | FLST 建立；程序化法術族（高階） |
| Arrowblock | [findings/arrowblock.md](findings/arrowblock.md) | PERK `ModIncomingDamage` + Script-MGEF `OnHit` | MagicEffectSpec 缺 script-attach(VMAD) |
| Immersive Interactions | [findings/immersive-interactions.md](findings/immersive-interactions.md) | perk `AddActivateChoice` + Global-as-DAR-selector | perk entry-point AddActivateChoice；_conditions.txt 生成器 |
| Animated Ships / Carriage | [findings/animated-vehicles.md](findings/animated-vehicles.md) | ship=NIF 自動畫；carriage=linkedRef 節點鏈路線 | placements 缺 `linkedRef` 欄位 |
| SM（Story Manager）子系統 | [findings/sm-subsystem.md](findings/sm-subsystem.md) | SMBN/SMQN/SMEN record 結構；event 路由；多層巢狀設計 | 多層巢狀 SMBN（缺口 #2 partial）；LVLN alias fill；package target=alias |
| Script-attached MGEF（VMAD） | [findings/mgef-vmad.md](findings/mgef-vmad.md) | VMAD 結構 + ActiveEffect 繼承；OnEffectStart/Finish 事件；三層 PERK→SPEL→MGEF→Script | partial 缺口 #3（MagicEffectSpec 缺 inline scripts 欄位；通用 AttachScripts 可繞路） |
| FLST 工廠模式 | [findings/flst-factory.md](findings/flst-factory.md) | FLST record | 高（缺口撤銷，模式有價值） | 索引對齊池 / 分類容器 / FLM 追加 三種模式 |
| Global-as-Selector + linkedRef 鏈 | [findings/runtime-selector-patterns.md](findings/runtime-selector-patterns.md) | GLOB/XLKR | 中 | runtime 狀態共享 + 路線節點鏈 + OAR/DAR condition 銜接 |
| PERK entry-point 機制 | [findings/perk-entry-points.md](findings/perk-entry-points.md) | PERK record | 高（缺口 #1） | entry-point 種類全表 + fragment 膠水 + AddActivateChoice 深挖 |
| Civil War Overhaul Redux | [findings/civil-war-overhaul-redux.md](findings/civil-war-overhaul-redux.md) | `Civil War Overhaul.esp` | 高（M&B / 戰略戰役參考） | campaign GLOB state machine；fixed attacker/defender aliases；ticket-based reinforcement controller；fort/city siege phase triggers |
| WARZONES - Civil Unrest | [findings/warzones-civil-unrest.md](findings/warzones-civil-unrest.md) | `WARZONES - SSE - Civil Unrest.esp` | 高（M&B ambient warzone） | marker/activator-driven encounter sites；spawnometer activators；global/MCM toggles；leveled spawn pools |
| Populated Skyrim Civil War | [findings/populated-skyrim-civil-war.md](findings/populated-skyrim-civil-war.md) | `Populated Skyrim Civil War.esp` | 中（world population） | 430 NPC bases + placed civil-war actors；no quest/dialogue controller；static battlefield density baseline |
| OBIS SE Patrols Addon | [findings/obis-patrols-addon.md](findings/obis-patrols-addon.md) | `OBIS SE Patrols Addon.esp` | 高（route spawn pattern） | 100-alias patrol quest；CreateReferenceToObject from leveled lists；ALPS package override per route；book/MCM globals |
| Populated Skyrim family (Steelfeathers: Cities/Lands/Dungeons/Hell) | [findings/populated-skyrim-family.md](findings/populated-skyrim-family.md) | `Populated Cities Towns Villages Legendary.esp` / `Populated Lands Roads Paths.esp` / `Populated Dungns Caves Ruins Legendary.esp` / `Populated Skyrim Legendary.esp`(Hell) | 無（人口 pattern 極高） | 純靜態置放人口（base+package+cell override，無 controller），#22 聚落量產 spec section 的活藍本——機制全已可生成，缺 macro-expansion 便利層 |
| Immersive Citizens - AI Overhaul | [findings/immersive-citizens-ai-overhaul.md](findings/immersive-citizens-ai-overhaul.md) | `Immersive Citizens - AI Overhaul.esp` | 低（系統 pattern 極高） | alias-ALPS 分派 quest 替既有 NPC 掛整疊 bespoke 日程包（不碰 NPC 記錄）+ Flee-template 防禦/逃跑 AI；#22 直接借鏡日程配方、需補 `flee` PACK 模板 |
| Immersive Wenches SE | [findings/immersive-wenches.md](findings/immersive-wenches.md) | `Immersive Wenches.esp` | 中 | cell-override XMarker + LeveledNpc 腳本生怪 + per-inn 時段 package + SM 觸發的環境 scene；#22 活人口最完整藍圖，~80% 已 landed，缺「人口填充 generator」便利層 |
| Populated Skyrim prison cells | [findings/populated-prison-cells.md](findings/populated-prison-cells.md) | `Populated Skyrim Prisons Cells.esp` | 無 | 家族同骨架，但置放走 carrier→LeveledNpc 兩層抽卡（牢房隨機囚犯）；單一 sandbox package、敵視玩家 faction；無新 gap，收束 Populated 全家桶 |
| Cutting Room Floor | [findings/cutting-room-floor.md](findings/cutting-room-floor.md) | `Cutting Room Floor.esp` | 中 | vanilla 聚落人口復原：override 幾個 vanilla cell + 新 interior + 手擺具名住民（faction 三件套 + per-NPC 日程）+ 無文字 ChangeLocation 狀態機做非破壞整合；#22「固定住民聚落」最乾淨骨架，缺 settlement generator 便利層 |
| Settlement NPC expansions (Immersive College NPCs / ICMF / ETaC Orc Strongholds) | [findings/settlement-npc-expansions.md](findings/settlement-npc-expansions.md) | `ICNs_ImmersiveCollegeNPCs.esp` / `ICMF Immersive College Mini Factions.esp` / `Immersive Orc Strongholds.esp` | 低（staffing/shop pattern 極高） | 單點聚落「住滿＋擺出店家」配方：unique base + 逐時段 package + additive cell override + **per-NPC Vendor faction（非 rank 公會，是迷你商圈）**；補 #22 聚落量產 section 的店家/服務面，機制全已 landed |
| Wench derivatives (Deadly Wenches / Buxom Yuriana) | [findings/wench-derivatives.md](findings/wench-derivatives.md) | `Deadly Wenches.esp`(依賴 IW) / `YurianaWench.esp`(standalone) | 無 / 中 | DW=override vanilla 敵人 LeveledNpc 注入野外戰鬥人口（異世界不適用，倒出輕量 `leveledListInject[]` 念頭）；Yuriana=standalone 語音隨從範本（90-cell-override 小 quest-mod，非單一隨從）|
| JK's Skyrim (set-dressing) | [findings/jks-skyrim-setdressing.md](findings/jks-skyrim-setdressing.md) | `JKs Skyrim.esp` | 無 | 18550 靜態 REFR、零任務的 mass cell-override 佈景：placement-volume 範本，cellrefs 欄位 1:1 對齊 Godot 編輯器 placements.json，天然 authoring 工具＝Godot worldspace editor |

> ⚠️ survey agent 對「ModForge 缺什麼」是**推斷**、未查 code，已知有誤判（如 Missives 說「不能生成 alias」其實可——ModForge 有 forced/uniqueActor/createObject/findMatching/alias-script）。roadmap 的缺口清單**待一次 code 驗證 pass** 校正。各 finding 講「mod 怎麼運作」的部分可信。

## 經濟 / 商販 / 服務型（2026-06-25 批次，使用者指定）

接 vendor + `settlements:` 落地後查兩個商販/服務 mod。**缺口已對 `src/` 驗證**（非推斷）。

| Mod | Finding | 機制重點 | ModForge 缺口 |
| --- | --- | --- | --- |
| Trade & Barter (kryptopyr) | [findings/trade-and-barter.md](findings/trade-and-barter.md) | MCM 可調**經濟/商販 overhaul**：barter 率、Speech 影響、商人金幣隨城市大小、地點/身份/種族/知識(Smithing→鐵匠)定價、庫存刷新——**「一串條件化 EntryPoint perk（ModBuy/SellPrices）+ 一個 MCM 腳本」近乎純 perk overhaul**，依賴 SKSE+SkyUI、無 DLL/SPID | **已驗證**：ModForge 已支援 `ModBuyPrices`/`ModSellPrices` EntryPoint（`Generator.Build.Perks.EntryPoints.cs` L31/L55）+ perk/effect CTDA + MCM + vendor faction → **條件化定價 perk + MCM 的 tweak mod 今天就能生**。**唯一硬缺口＝無 GMST/game-setting 編輯**（`src/` 證實缺）；MCM 切換→GLOB→perk 條件接線待補一例 |
| Honed Metal（NPC 打造/附魔服務） | [findings/honed-metal.md](findings/honed-metal.md) | 付費請 NPC 鐵匠/附魔師代工（打造/強化/附魔/充能），成本隨 NPC 技能+barter；**框架型 + 原生 C++ SKSE DLL**；faction-tag NPC + 條件對話 + FormList 材料 + 「開容器→腳本開原生製作選單」核心 trick；依賴 SKSE+SkyUI | scaffolding（faction 服務對話+MCM+FormList+扣金幣 fragment+storage）**可生成**；**controller（開原生選單、成本數學、perk/技能 gate、強化套用）須 bespoke Papyrus，原生選單 trick 可能要附帶預建 DLL**；浮現 `services:` macro + 「付錢→給/改物件」交易 pattern 兩個候選 |
| Real Estate（Nexus 14408 v3.2） | [findings/real-estate.md](findings/real-estate.md) | 玩家側**房產投資經濟**：買下 vanilla 房子→被動收租/礦產/農產→賣出。**vanilla-only（僅 SkyUI，無 DLL/PapyrusUtil/JContainers）**；機制＝每棟房外手擺一個腳本化 **Property Sign Activator**（`Owned`/`Not owned` state machine）+ 計價/收益 GLOB 組 + `RegisterForUpdateGameTime` 被動收租 + `SetActorOwner`/token-replacement 所有權 + relationship PERK + `SKI_ConfigBase` MCM + 教學 quest；告示牌＝在既有世界疊一層玩家系統的低衝突 pattern | **已驗證**：GLOB/QUST/ACTI script-attach/XOWN(`OwnershipSpec`)/PERK/RELA/MCM/收租 fragment **全已 landed**。**唯一硬缺口＝`MessageSpec` 無多按鈕選單欄位**（`Spec.Items.cs:42` 只有 EditorId/Name/Description）→ 生不出買/賣 message-box 選單（跨多互動 mod 的通用缺口，建議優先補 `buttons:[]`）。settlements macro 缺 `ownership:`/`income:` 維度（#22 收益面）|

## 動作 / 動畫系統框架（2026 完整堆疊）

中樞 [action-system/README.md](action-system/README.md) 有**五層堆疊地圖**（骨架→行為引擎→行為資料注入→動畫選擇→招式框架）+ 跨層「動畫驅動狀態」鐵三角 + ModForge 生成機會。原始 mod 頁文字存 `action-system/raws/`。

| 層 | 框架 | 文件 | ModForge 可生成性 |
| --- | --- | --- | --- |
| 0 骨架 | XPMSSE | [findings/xpmsse.md](action-system/findings/xpmsse.md) | 純前置 |
| 1 引擎 | Pandora | [action-system/pandora.md](action-system/pandora.md) | shell-out |
| 1 引擎 | Universal Behavior Runtime（A-Pose Fix + Auto Skeleton） | [findings/universal-behavior-runtime.md](action-system/findings/universal-behavior-runtime.md) | 前置（runtime 容錯/LE→SE 轉換） |
| 2 注入 | Behavior Data Injector（+Universal Support） | [findings/behavior-data-injector.md](action-system/findings/behavior-data-injector.md) | **config 可生成（roadmap）** |
| 2 注入 | Payload Interpreter | [findings/payload-interpreter.md](action-system/findings/payload-interpreter.md) | annotation 屬動畫管線 |
| 2 注入 | Animation Motion Revolution | [findings/animation-motion-revolution.md](action-system/findings/animation-motion-revolution.md) | annotation 屬動畫管線 |
| 3 選擇 | Open Animation Replacer | [action-system/oar-replacer-guide.md](action-system/oar-replacer-guide.md) | **結構可生成（roadmap，最高槓桿）** |
| 3 選擇 | Directional Movement Keys | [findings/directional-movement-keys.md](action-system/findings/directional-movement-keys.md) | 前置；其 graph var 供 OAR 條件 |
| 4 招式 | BFCO（攻擊框架，+Universal Support） | [findings/bfco.md](action-system/findings/bfco.md) | OAR 變體 config 可生成 |
| 4 招式 | SCAR（NPC 連段 AI） | [findings/scar.md](action-system/findings/scar.md) | AI 不可生成 |
| 4 招式 | moveset 實例庫（DAR/OAR/SCAR 真實檔案結構） | [findings/movesets-examples.md](action-system/findings/movesets-examples.md) | **OAR 生成器的輸出規格（已驗證）** |

| 自訂技能樹 | Custom Skills Framework | [custom-skills-framework.md](custom-skills-framework.md) + [custom-skill-tree-guide.md](custom-skill-tree-guide.md) | 自訂技能樹分析 + 實作指南（roadmap 功能項） |
| 自訂技能樹 | Constellations（CSF 最高品質參考實作） | [findings/constellations.md](findings/constellations.md) | CSF 路線確認正確；MVP = JSON+PERK+GLOB+KYWD+薄 Papyrus；Fortify 附魔 native dll 超出 MVP |

## 求生 / 框架系統型（Campfire 堆疊 + PROTEUS）

| Mod | Finding | 機制重點 | ModForge 意義 |
| --- | --- | --- | --- |
| Campfire（求生框架） | [findings/campfire.md](findings/campfire.md) | **in-world 3D 技能樹引擎**：星點/連線/背板都是真實 ObjectReference，相對 CenterObject 偏移 spawn、轉向面對玩家、OnActivate 點 perk、距離 480 自毀；公開 API `RegisterPerkTree` | **第二條自訂技能樹生成路線**（vs CSF Scaleform）；零件全在 record 能力域，玩家端只依賴 Campfire.esm；缺 PositionRef layout 模板 |
| Frostfall（求生 mod） | [findings/frostfall.md](findings/frostfall.md) | 天賦樹＝註冊進 Campfire Skill System 的「Endurance」樹（6 perk，`_Frost_PerkRank_*` GLOB ↔ CampPerkNode）；exposure/warmth 系統 | Campfire 掛樹 API 的活範例；星(視覺)與 MGEF(效果)解耦 |
| PROTEUS（角色 build 管理） | [findings/proteus.md](findings/proteus.md) | native `Proteus.dll` + 6 個 JSON 模板 runtime 序列化角色狀態；UILib 選單 | 忽略（閉源 native，無生成成分）；JSON 角色 schema 純對照參考 |

> 使用者 2026-06-16 指定調查：Frostfall 天賦樹 + 「星點如何成為 3D world space object」→ 答案全在 [campfire.md §2](findings/campfire.md)。

## 修復型

尚未調查。

## 美術型

尚未調查。
