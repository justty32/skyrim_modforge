# Mod Survey Index

## 內容型

| Mod | Finding | Plugin | 敘事價值 | 重點 |
| --- | --- | --- | --- | --- |
| Follower Commentary Overhaul SE | [findings/follower-commentary-overhaul.md](findings/follower-commentary-overhaul.md) | `FCO - Follower Commentary Overhaul.esp` | 中 | generic follower ambient commentary；voice type + location/quest/player-state conditions |
| Improved Follower Dialogue - Lydia | [findings/improved-follower-dialogue-lydia.md](findings/improved-follower-dialogue-lydia.md) | `ImprovedCompanionsBoogaloo.esp` | 高 | unique follower arc；stage/global/VM quest variable；moral objection；scene quests |
| Relationship Dialogue Overhaul | [findings/relationship-dialogue-overhaul.md](findings/relationship-dialogue-overhaul.md) | `Relationship Dialogue Overhaul.esp` | 高 | relationship/follower system overhaul；shared info；voice type matrix；generic recruit/command compatibility |
| I'm Glad You're Here | [findings/im-glad-youre-here.md](findings/im-glad-youre-here.md) | `ImGladYoureHere.esp` | 高（動作層） | follower/family hug action service；scene protection；camera/idle/package cleanup；Sofia compatibility hooks |

## 框架型

| Mod | Finding | Plugin / Runtime | 參考價值 | 重點 |
| --- | --- | --- | --- | --- |
| Common Framework / Utility Mods | [findings/common-framework-mods.md](findings/common-framework-mods.md) | SPID / OAR / PapyrusUtil / JContainers / BOS / AOS / Conditional Expressions / IWH / ITH | 高（工具層） | distribution、animation conditions、state storage、object/animobject swap、expression state、collision/dialogue suppression |
| PapyrusUtil SE（深挖） | [findings/papyrusutil.md](findings/papyrusutil.md) | `PapyrusUtil.dll`（SKSE，無 ESP） | 高（狀態儲存 + cell 掃描 + package override） | StorageUtil per-form KV + list（int/float/string/Form 四型）；JsonUtil 外部 JSON 讀寫 + path API；ActorUtil package override priority 0-100；MiscUtil ScanCellNPCs/Objects、檔案操作；PapyrusUtil 陣列 push/diff/merge/slice；v4.6 |
| JContainers SE（深挖） | [findings/jcontainers.md](findings/jcontainers.md) | `JContainers64.dll`（SKSE，無 ESP） | 高（複雜資料結構 + 外部 JSON 雙向） | JArray 無上限動態陣列；JMap/JFormMap/JIntMap key-value 容器；JDB 全域資料庫（跨 mod 共享）；JFormDB per-Form 嵌套結構；JValue readFromFile/writeToFile JSON 序列化；JAtomic 原子操作；生命週期需手動 retain/release；API 4 / Feature 2 |
| Conditional Expressions（深挖） | [findings/conditional-expressions.md](findings/conditional-expressions.md) | `Conditional Expressions.esp` + 16 .psc | 高（表情層） | MFG SetModifier/SetPhoneme/SetExpressionOverride 全索引表；16 種狀態 effect 機制；busy gate 設計；三段式漸變 pattern；GlobalVariable 中介狀態可用於 dialogue condition |
| I'm Walking Here + I'm Talkin' Here | [findings/iwh-ith.md](findings/iwh-ith.md) | `ImWalkinHere.dll`（SKSE）+ `ImTalkinHere.esp` | 中（品質層） | IWH：TOML 四開關碰撞抑制，無 API，純被動；ITH：`PlayerInDialogue` Conditional property，bark condition hook；follower mod 可讀 GetScriptVariable 或自實作 PlayerBusy global |
| Nether's Follower Framework | [findings/nether-follower-framework.md](findings/nether-follower-framework.md) | `nwsFollowerFramework.esp` | 高（主要 follower 框架） | DialogueFollower slot expansion；regular vs imported followers；Sofia import/export；NoImport faction；sandbox/regard/home/storage |
| Base Object Swapper (BOS) | [findings/base-object-swapper.md](findings/base-object-swapper.md) | `po3_BaseObjectSwapper.dll`（SKSE，無 ESP） | 中（場景佈置層） | `_SWAP.ini` runtime 替換 base form；`[Forms/Properties/References/Transforms]` 四 section；FormID `0xID~Plugin` 語法；location/region/keyword/cell/worldspace filter；chance 機率；transform 覆蓋（pos/rot/scale/flags）；follower home set dressing 無 patch 方案 |
| AnimObject Swapper (AOS) | [findings/animobject-swapper.md](findings/animobject-swapper.md) | `po3_AnimObjectSwapper.dll`（SKSE，無 ESP） | 低→中（角色化演出層） | `_ANIO.ini` runtime 替換 idle ANIO；`[BaseANIO\|FILTERS\|TRAITS]` section 格式；ALL(+)/NOT(-)/MATCH/ANY(*) filter；faction/race/keyword/spell/NPC/FormList 條件；sex/child traits；多值隨機池；OAR 換動作 + AOS 換道具 配對模式 |
| SkyPatcher | [findings/skypatcher.md](findings/skypatcher.md) | `SkyPatcher.dll`（SKSE，CommonLibSSE-NG） | 高（策略層） | 通用 ini-based runtime patcher；28+ record 類型；kDataLoaded 套用；NPC 視覺/數值/法術/perk 批量修改；esp vs config 產物策略核心 |
| po3's Tweaks | [findings/po3-tweaks.md](findings/po3-tweaks.md) | `po3_Tweaks.dll`（無 esp） | 低（純前置環境標記） | 純 SKSE DLL，無 record；[Fixes]/[Tweaks]/[Experimental] ini 三區；CombatToNormal dialogue fix、Cast Added Spells on Load、Furniture in Combat 與 ModForge 生成物有交集；`IsTweakInstalled()` API 可做 graceful fallback |
| MCM Helper | [findings/mcm-helper.md](findings/mcm-helper.md) | `MCMHelper.dll` + `MCMHelper.esp`（ESL） | 高（設定選單生成候選） | JSON config.json → SkyUI MCM 自動建構；控件型別：toggle/slider/stepper/enum/keymap/header；sourceType 對應 ModSetting ini 或 script property；最小 esp 需求：一個 Quest（若有 action）；ModForge 可從 spec 生成 config.json + settings.ini |
| SPID（深挖） | [findings/spid.md](findings/spid.md) | `po3_SpellPerkItemDistributor.dll`（SKSE，無 ESP） | 高（NPC 無 patch 分發層） | 完整 _DISTR.ini 語法；12 種 type（Spell/Perk/Item/Shout/LevSpell/Package/Outfit/SleepOutfit/Keyword/DeathItem/Faction/Skin）；StringFilter/FormFilter/LevelFilter/Traits 詳解；與 KID/SkyPatcher 分工 |
| Keyword Item Distributor（KID） | [findings/keyword-item-distributor.md](findings/keyword-item-distributor.md) | `po3_KeywordItemDistributor.dll`（SKSE，無 ESP） | 中（分發層工具） | `_KID.ini` 批次把 KYWD 掛到 item/armor/weapon/MGEF 等；與 SPID 正交（KID 管物件，SPID 管 NPC）；可輸出 ini 替代 override |
| Sound Record Distributor（SRD） | [findings/sound-record-distributor.md](findings/sound-record-distributor.md) | SRD SKSE plugin | 中 | 音效分發（SPID 的 sound 版）；esp-side vs SRD 取捨 |
| FormList Manipulator（FLM） | [findings/formlist-manipulator.md](findings/formlist-manipulator.md) | `FormListManipulator.dll`（SKSE，無 ESP） | 中（FLST 追加層） | `_FLM.ini` 把 form 加入任意 FLST（含 vanilla/外部 mod），零衝突；補 ESP-side FLST 無法無衝突追加外部 FLST 的死角 |

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

> ⚠️ survey agent 對「ModForge 缺什麼」是**推斷**、未查 code，已知有誤判（如 Missives 說「不能生成 alias」其實可——ModForge 有 forced/uniqueActor/createObject/findMatching/alias-script）。roadmap 的缺口清單**待一次 code 驗證 pass** 校正。各 finding 講「mod 怎麼運作」的部分可信。

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
