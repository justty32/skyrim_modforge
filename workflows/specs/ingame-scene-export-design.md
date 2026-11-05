# 遊戲內場景匯出 — 設計方案（in-game 蓋城鎮 → scene JSON → ModForge patch）

← [specs 入口](README.md)｜idea：[#24 遊戲內編輯器](../idea/tools/24-ingame-editor.md)｜藍本：[Tundra Defense](../../../../analysis/mod-survey/findings/economy-building/tundra-defense.md)・[PROTEUS](../../../../analysis/mod-survey/findings/skills-survival/proteus.md)

本 spec 涵蓋 **[Idea #24](../idea/tools/24-ingame-editor.md) 北極星「遊戲內蓋城鎮並匯出」的 ModForge 側契約 + 最小垂直切片**。2026-07-08 的原始判定是：**ModForge 的生成端幾乎全部已具備**（`PlacementSpec` 已含 position/rotation/scale/enableParent/ownership…、map marker/hazard/keyword/身份系統都已實機），當時把採集橋 SKSE DLL、placement-controller `.pex` 與「採集 → spec」橋列為 net-new。這是歷史起點：現行採集橋核心已存在、`.pex` 已由 C++ Palette/Preview 取代、scene.json 直接由 CLI deserialize；後文保留原設計並在各節標示現況。

本 spec 按主題拆成下列子檔：

<!-- wf-nav -->
| 檔案 | 內容 |
|------|------|
| [scope-mvp.md](ingame-scene-export-design/scope-mvp.md) | 目標 / 成功判準、範圍邊界（YAGNI）、ModForge 落點（generable-today / net-new）、歷史最小垂直切片、測試策略、開放 / 後續 |
| [architecture.md](ingame-scene-export-design/architecture.md) | 歷史架構：四個元件 + 一座橋；③附 滴管取樣 + 具名插槽 |
| [contract.md](ingame-scene-export-design/contract.md) | **契約：scene.json**（採集橋 output ↔ ModForge input）、座標契約、vanilla diff |
| [contract-overrides.md](ingame-scene-export-design/contract-overrides.md) | 座標契約 round-trip、採集橋輸出必須是合法 ModSpec、既有 ref 的 override 形狀＝頂層 `overrides[]` |
| [references.md](ingame-scene-export-design/references.md) | referrer 的形狀＝頂層 `references[]`：定案形狀、label 語意、槽位表（SingleRef vs location） |
| [references-targets.md](ingame-scene-export-design/references-targets.md) | `references[]` 兩類目標（檔內 / 外部）、persistent 坑與 `anchor`、build 順序、DLL 端、範例與測試 |
| [captures.md](ingame-scene-export-design/captures.md) | 場景匯出不含 NPC ＋ captures 拆成獨立檔；`sc capp` 直接吸玩家 |
| [npc-source.md](ingame-scene-export-design/npc-source.md) | NPC 來源（PROTEUS 可選）、ESL local-id 寬度、§D NPC 角色 macro |
