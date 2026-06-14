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
| Nether's Follower Framework | [findings/nether-follower-framework.md](findings/nether-follower-framework.md) | `nwsFollowerFramework.esp` | 高（主要 follower 框架） | DialogueFollower slot expansion；regular vs imported followers；Sofia import/export；NoImport faction；sandbox/regard/home/storage |

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

## 修復型

尚未調查。

## 美術型

尚未調查。
