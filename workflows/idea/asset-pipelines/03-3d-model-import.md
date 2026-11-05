# 3D Model Import Pipeline (External Mesh → Skyrim SE `.nif`)

← index: [README.md](README.md) · **deep-dive 子工作流: [model-porting/](model-porting/README.md)**（逐步施工計畫）· related: [IDEAS.md §14](../ideas.md) (format conversion), [external_assets.md](../../../docs/external_assets.md), [map-scene/](map-scene/README.md)

**Research date:** 2026-06-08. Personal/single-player only — ported commercial-game assets (Genshin/WuWa/Dark Souls, Unity-Store/Nexus packs) are converted and used **locally**, never redistributed.

**Headline for Linux:** the biggest tooling decision is **NifTools-addon vs PyNifly vs ck-cmd**, because **PyNifly is Windows-only** (ships a native `NiflyDLL`). This shifts the recommended Linux pipeline away from what IDEAS.md §14 assumes ("PyNifly / ck-cmd"). See §2.

## 內容

內容按主題分 4 檔放在 [03-3d-model-import/](03-3d-model-import/)，本檔是入口。

| 檔案 | 內容 |
|------|------|
| [nif-and-tools.md](03-3d-model-import/nif-and-tools.md) | §1 Skyrim `.nif` 目標格式、§2 mesh→NIF 工具與 Linux 現況 |
| [pipelines-textures.md](03-3d-model-import/pipelines-textures.md) | §3 靜態物件管線、§4 蒙皮角色管線、§5 貼圖／材質轉換 |
| [extraction-workflow.md](03-3d-model-import/extraction-workflow.md) | §6 從自有遊戲解包（Genshin／WuWa／Dark Souls）、§7 端到端流程 |
| [integration-mvp.md](03-3d-model-import/integration-mvp.md) | §8 ModForge 整合、§9 MVP 與坑、來源 |
