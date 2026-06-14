# ModForge — 已落地功能目錄（index）

← [INDEX](../../../INDEX.md)（專案地圖）｜[feature-dev README](../README.md)

功能真正 in-game 落地（或 offline 完整）後才濃縮一句話 + 實作細節指標進這裡。實作細節見 git log / [CODE_MAP](../../common/code-map/CODE_MAP.md) / [SPEC-index](../../../docs/spec/SPEC-index.md)。鐵律與踩坑見 [gotchas](../gotchas.md)，未做的見 [ROADMAP](../../roadmap.md)。

**按 CODE_MAP 五分法分檔**（兩套 index 互通——每檔對應同名 CODE_MAP 子 index）：

| 檔 | 涵蓋 | 對應 CODE_MAP |
|----|------|---------------|
| [dialogue-quests.md](dialogue-quests.md) | 對話 / 任務 / Story Manager / trigger 庫 / 身份系統 / Scene 演出 | [CODE_MAP.dialogue-quests](../../common/code-map/CODE_MAP.dialogue-quests.md) |
| [world.md](world.md) | Light(LIGT) / Map marker(WRLD override) / 光照管線（室內 LGTM+IMGS、室外 weather IMGS） | [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| [items-magic.md](items-magic.md) | GLOB / PROJ+EXPL / MGEF 擴充 / FormList / Hazard / Music | [CODE_MAP.items-magic](../../common/code-map/CODE_MAP.items-magic.md) |
| [npcs.md](npcs.md) | PACK templates / NPC inventory / NPC patch（override vanilla） | [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md) |
| [infra.md](infra.md) | Voice pipeline / Spec `$ref`·`$env` 解析層 / showcase + diag | [CODE_MAP.infra](../../common/code-map/CODE_MAP.infra.md) |
