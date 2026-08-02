# sub_projs/ — 衍生專案

這裡放**把 ModForge 當工具使用的專案**，而不是工具本身。ModForge 的唯一目的是「JSON spec → Skyrim mod，且為 AI agent 適配」;這些專案**消費**它（或**作為它的基石**透過協議連接），但**不與它整合**——ModForge 不該為了某個專案長出特例功能。可以在推進這些專案時吸取經驗來改進工具，但專案本身不是工具的一部分。

> **2026-08-02：這個資料夾已經清空到只剩三項。** 十一個子專案全部搬出去了，**連 stub 導引都不留**——去哪找見下方對照表。判準：有自己程式碼／建置／產物的成了 `projects/` 下的同層 git repo；純文檔的進工作區 `analysis/`。舊 commit 歷史查 `git log -- sub_projs/<name>`。

| 專案 | 類型 | 狀態 | 說明 |
|------|------|------|------|
| `gemini-research/` | 原始素材 | 📄 純存檔 | Gemini CLI 聯網搜尋原始輸出（combat-mods/npc-beautification/outfit-fitting/tool-survey）；品質參差，需人工篩選後才搬入正式 finding。 |
| `inworld-skill-tree/` | 消費者 | 🟡 核心已落地、Phase 2 未做 | In-world 3D 星樹技能樹生成路線（Idea #20，玩家+NPC）。放棄 CSF，走 Campfire/Frostfall 世界內星樹 + JContainers per-NPC 狀態。U1–U5 全解；Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）+ Phase 3（generator `skillTrees:`）皆離線落地並 IN-GAME CONFIRMED。剩 Phase 2（NPC 版橋接）未動工 + Phase 0 實機驗收。 |
| `living-adventurers/` | 消費者 | 🟡 核心離線落地、等實機 | 給 standalone follower 一條命：人口/沈浸型 mod，一小撮具名持久冒險者過自主冒險人生（抽象幽靈模擬 + MoveTo 就地實體化），玩家各處撞見、酒館傳唱。idea #23。核心已落地：P1 泛化控制器（build 綠零警告）+ P2 `livingNpcs:` enroll macro（845 測綠）+ P3 玩家互動與 alignment（848 測綠）；待主力機編 .pex + 實機驗證。任務層（真 missive）卡 roadmap #7–9；cast 接真 standalone follower mod 未開。 |

這三個為什麼還在：`gemini-research/` 是餵本 repo findings 的原始素材；`inworld-skill-tree/` 與 `living-adventurers/` 雖然掛「消費者」名義，但**實作已經落地成 ModForge 的 generator 能力**（`skillTrees:` / `livingNpcs:` macro），設計文檔跟著 spec 走比較合理，體量也還小。

---

## 已移出 — 去哪找（2026-08-02）

**`projects/` 下的同層 git repo**（有自己的程式碼／建置／產物）：

| 原 sub_proj | 現在在哪 | 類型 | 對接方式 |
|------|--------|------|------|
| godot-worldspace-editor | [`../../godot-worldspace-editor`](../../godot-worldspace-editor/README.md) | 工具前端 | heightmap/splatmap PNG + `placements.json` → spec |
| scene-capture-bridge | [`../../scene-capture-bridge`](../../scene-capture-bridge/README.md) | 基石聯動 | `scene.json` → `build` 出 patch esp |
| model-converter | [`../../model-converter`](../../model-converter/README.md) | 基石/工具 | 黑盒 exec，`MODFORGE_NIF2GLTF_BIN` |
| agent-bridge | [`../../agent-bridge`](../../agent-bridge/README.md) | 基石聯動 | 測試治具：console + `/state` 斷言驗生成的 mod |
| darksouls-port | [`../../darksouls-port`](../../darksouls-port/README.md) | 消費者 | spec → worldspace esp（**資產僅本機、不發佈**）|
| sofia-patch | [`../../sofia-patch`](../../sofia-patch/README.md) | 消費者 | 內容 authoring → spec `.json` |
| skyrim-voicegen | [`../../skyrim-voicegen`](../../skyrim-voicegen/README.md) | 基石 | 黑盒 exec，`MODFORGE_TTS_BIN` |
| game-data | [`../../game-data`](../../game-data/README.md) | 共用參考 | 消費 CLI `gamedata` 指令產出文本 |

**工作區 `analysis/`**（純文檔，零程式零建置，不做成 git repo）：

| 原 sub_proj | 現在在哪 | 說明 |
|------|--------|------|
| mod-survey | [`analysis/mod-survey`](../../../analysis/mod-survey/README.md) | 136 份他人 mod 結構化調查 + action-system / custom-skill-tree 專題 |
| tool-survey | [`analysis/tool-survey`](../../../analysis/tool-survey/README.md) | 模組製作**工具**調查（SkyrimIngameEditor 等）|
| followers-patch | [`analysis/followers-patch`](../../../analysis/followers-patch/README.md) | 8 份隨從 personality brief，✅ 完成 |

**留在 ModForge 的**：這些專案的**契約/spec/計畫/驗收文檔**（`workflows/specs/`、`workflows/plans/`、`docs/spec/`、`workflows/feature-dev/landed/`、`wait_todo/`、`workflows/investigation/`）與**全部生成端 C# 程式碼**。移出的只有前端／工具／內容本身。

跨 repo 連結一律假設各 repo **同層 clone 在 `projects/` 下**。

---

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）現在歸工作區 `analysis/`；`docs/` 只放 ModForge 自己的使用手冊。
