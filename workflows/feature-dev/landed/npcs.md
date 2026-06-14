# 已落地 — NPC / packages

← [landed index](README.md)｜對應 [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md)

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

**record builders（npc 域）**
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。
- **NPC patch（override vanilla NPC）**（in-game 確認 2026-06-13）：`npcPatches[]` override 既有 NPC 的 `Packages` 等（AI Overhaul 核心，如 Carlotta 留在家）。headless 解 localized Name 的字串牆解法見 memory [[headless-vanilla-strings-provision]]；輸出英文名 inline、non-localized。`examples/npc_patch.json`、`npcdiag`。注意 USSEP/load-order 衝突。
