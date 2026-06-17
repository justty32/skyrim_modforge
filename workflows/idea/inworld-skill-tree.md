# Idea #20：In-world 技能樹（玩家 + NPC，Campfire 引擎路線）

← [ideas 索引](ideas.md)

**已成 sub_proj**（體量上來，照 Idea #19 模式）：durable 設計、方案脈絡、社群調查、待調查全在
→ **[sub_projs/inworld-skill-tree/](../../sub_projs/inworld-skill-tree/README.md)**

**一句話**：用 Campfire/Frostfall 的 **in-world 3D 星樹**做 PoE-like 技能樹的生成路線，玩家與 NPC 通用。
**核心判斷**：perk **效果層** 100% 可行（`Actor.AddPerk` 對玩家/NPC 皆有效）；**UI 層**——**放棄 CSF**（`OpenCustomSkillMenu` 無 Actor 參數 + 需 native dll），改走 Campfire 世界內 3D 星樹（純 ESP record + 薄 Papyrus，玩家端只依賴 `Campfire.esm`）。NPC 版加 JContainers `JFormDB` per-NPC 狀態橋接。
