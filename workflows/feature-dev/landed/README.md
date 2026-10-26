# ModForge — 已落地功能目錄（index）

← [INDEX](../../../INDEX.md)（專案地圖）｜[feature-dev README](../README.md)

功能真正 in-game 落地（或 offline 完整）後才濃縮一句話 + 實作細節指標進這裡。實作細節見 git log / [CODE_MAP](../../common/code-map/CODE_MAP.md) / [SPEC-index](../../../docs/spec/SPEC-index.md)。鐵律與踩坑見 [gotchas](../gotchas.md)，未做的見 [ROADMAP](../../roadmap/README.md)。

**5 個核心 record 檔對應 CODE_MAP 五分法**（兩套 index 互通——每檔對應同名 CODE_MAP 子 index；超過 8 KB 的按語意拆出同域子檔，核心檔名不變、當入口），**外加 1 個 `godot-editor.md`**（編輯器子專案工具，不對應 CODE_MAP 子 index，durable 在 sub_projs）：

<!-- wf-nav -->
| 檔 | 涵蓋 | 對應 CODE_MAP |
|----|------|---------------|
| [dialogue-quests.md](dialogue-quests.md) | 對話 / 任務 / quest-node contract / Story Manager / trigger 庫 / 身份系統 / Scene 演出 | [CODE_MAP.dialogue-quests](../../common/code-map/CODE_MAP.dialogue-quests.md) |
| [world.md](world.md) | Light(LIGT) / Map marker(WRLD override) / 光照管線 / scene 座標轉換 | [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| ↳ [worldspace.md](worldspace.md) | worldspace heightmap→LAND / VNML / baseTexture·textureLayers / PlacementSpec / godotPlacements / removals[] | [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| ↳ [world-generators.md](world-generators.md) | 高階 generator：聚落人口 `settlements:`、技能樹 `skillTrees:` | [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| ↳ [navmesh.md](navmesh.md) | NAVM no-op override / L_NAVCUT 證偽 / `navPatches[]` | [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| ↳ [scene-capture-bridge.md](scene-capture-bridge.md) | 🔴 部署事故（不 cp 覆寫執行中 DLL）/ 採集橋 M4 / P1 marker / P2–P3 編輯器 | —（DLL 在 `projects/scene-capture-bridge/`）；C# 消費端 [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md) |
| ↳ [scene-capture-bridge-editor.md](scene-capture-bridge-editor.md) | P7/P8 / 匯出三改 / 動作鍵 ini·面板·numpad / Browser ghost 預覽 | 同上 |
| ↳ [scene-capture-bridge-referrer.md](scene-capture-bridge-referrer.md) | referrer 原語 `references[]`（slot-kind 教訓、對照組實驗） | 同上 |
| ↳ [scene-capture-bridge-flags-deps.md](scene-capture-bridge-flags-deps.md) | `noHavokSettle` / 模式開關 py·ed·pkc / `Export requires` 依賴可見性 | 同上 |
| [items-magic.md](items-magic.md) | GLOB / MESG menu / PROJ+EXPL / EFSH / MGEF 擴充 / FormList / Hazard / Music | [CODE_MAP.items-magic](../../common/code-map/CODE_MAP.items-magic.md) |
| [npcs.md](npcs.md) | PACK templates / NPC inventory / NPC patch（override vanilla） | [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md) |
| ↳ [npcs-capture.md](npcs-capture.md) | NPC 外貌配方 + capturedNpcs[] / `sc capp` 吸玩家 | [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md) |
| [infra.md](infra.md) | Voice pipeline / SQLite catalog / Spec `$ref`·`$env` / templated PACK 的 PKCU meta / showcase + diag | [CODE_MAP.infra](../../common/code-map/CODE_MAP.infra.md) |
| ↳ [infra-loose-configs.md](infra-loose-configs.md) | loose 檔 config 生成：MCM→GLOB / OAR·BDI·PIE / SPID / D 組七分發器 | [CODE_MAP.infra](../../common/code-map/CODE_MAP.infra.md) |
| [godot-editor.md](godot-editor.md) | Idea #19 Godot worldspace 編輯器 + model-converter nif→glTF；子專案工具 | —（sub_projs 子專案） |
