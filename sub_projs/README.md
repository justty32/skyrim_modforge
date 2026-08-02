# sub_projs/ — 衍生專案

這裡放**把 ModForge 當工具使用的專案**，而不是工具本身。ModForge 的唯一目的是「JSON spec → Skyrim mod，且為 AI agent 適配」;這些專案**消費**它（或**作為它的基石**透過協議連接），但**不與它整合**——ModForge 不該為了某個專案長出特例功能。可以在推進這些專案時吸取經驗來改進工具，但專案本身不是工具的一部分。

目前 tracked（不 gitignore），方便在別處 clone 同步。某專案**成熟或體量龐大**後，再移去自己的 repo——見下方「已移出」。

| 專案 | 類型 | 狀態 | 說明 |
|------|------|------|------|
| `gemini-research/` | 原始素材 | 📄 純存檔 | Gemini CLI 聯網搜尋原始輸出（combat-mods/npc-beautification/outfit-fitting/tool-survey）；品質參差，需人工篩選後才搬入正式 finding。 |
| `inworld-skill-tree/` | 消費者 | 🟡 核心已落地、Phase 2 未做 | In-world 3D 星樹技能樹生成路線（Idea #20，玩家+NPC）。放棄 CSF，走 Campfire/Frostfall 世界內星樹 + JContainers per-NPC 狀態。U1–U5 全解；Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）+ Phase 3（generator `skillTrees:`）皆離線落地並 IN-GAME CONFIRMED。剩 Phase 2（NPC 版橋接）未動工 + Phase 0 實機驗收。 |
| `living-adventurers/` | 消費者 | 🟡 核心離線落地、等實機 | 給 standalone follower 一條命：人口/沈浸型 mod，一小撮具名持久冒險者過自主冒險人生（抽象幽靈模擬 + MoveTo 就地實體化），玩家各處撞見、酒館傳唱。idea #23。核心已落地：P1 泛化控制器（build 綠零警告）+ P2 `livingNpcs:` enroll macro（845 測綠）+ P3 玩家互動與 alignment（848 測綠）；待主力機編 .pex + 實機驗證。任務層（真 missive）卡 roadmap #7–9；cast 接真 standalone follower mod 未開。 |

---

## 已移出（2026-08-02）

一輪把**能獨立的都獨立**：有自己程式碼／建置／產物的抽成 `projects/` 下的**同層 git repo**；純文檔的搬去工作區 `analysis/`。全部**未帶 commit 歷史**（舊歷史查本 repo 的 `git log -- sub_projs/<name>`），對接方式**完全不變**——照樣靠協議/CLI，不整合。**原位置各留一份 stub 導引**，所以指向 `sub_projs/<name>/README.md` 的舊連結都還通。

| 專案 | 新位置 | 類型 | 狀態 | 對接 |
|------|--------|------|------|------|
| [`godot-worldspace-editor/`](godot-worldspace-editor/README.md) | `../../godot-worldspace-editor` | 工具前端 | 🟢 整鏈 in-game 確認 | heightmap/splatmap PNG + `placements.json` → spec |
| [`scene-capture-bridge/`](scene-capture-bridge/README.md) | `../../scene-capture-bridge` | 基石聯動 | 🟢 P1–P3 實機全過 | `scene.json` → `build` 出 patch esp |
| [`model-converter/`](model-converter/README.md) | `../../model-converter` | 基石/工具 | 🟡 53 測綠、等實機驗 | 黑盒 exec，`MODFORGE_NIF2GLTF_BIN` |
| [`agent-bridge/`](agent-bridge/README.md) | `../../agent-bridge` | 基石聯動 | 🟢 迴圈全通、已結案 | 測試治具：console + `/state` 斷言驗生成的 mod |
| [`darksouls-port/`](darksouls-port/README.md) | `../../darksouls-port` | 消費者 | 🟡 P1 已交付待實機 | spec → worldspace esp（**資產僅本機、不發佈**）|
| [`sofia-patch/`](sofia-patch/README.md) | `../../sofia-patch` | 消費者 | 🟡 Acts 1-4 已交付、等實機 | 內容 authoring → spec `.json` |
| [`skyrim-voicegen/`](skyrim-voicegen/README.md) | `../../skyrim-voicegen` | 基石 | ✅ 可用（主力機）| 黑盒 exec，`MODFORGE_TTS_BIN` |
| [`game-data/`](game-data/README.md) | `../../game-data` | 共用參考 | ✅ 可用（主力機）| 消費 CLI `gamedata` 指令產出文本 |

**純文檔的三份改放工作區 `analysis/`**（零程式零建置，不值得做成 git repo）：

| 專案 | 新位置 | 說明 |
|------|--------|------|
| [`mod-survey/`](mod-survey/README.md) | `../../../analysis/mod-survey` | 136 份他人 mod 結構化調查 + action-system / custom-skill-tree 專題 |
| [`tool-survey/`](tool-survey/README.md) | `../../../analysis/tool-survey` | 模組製作**工具**調查（SkyrimIngameEditor 等）|
| [`followers-patch/`](followers-patch/README.md) | `../../../analysis/followers-patch` | 8 份隨從 personality brief，✅ 完成 |

**留在 ModForge 的**：這些專案的**契約/spec/計畫/驗收文檔**（`workflows/specs/`、`workflows/plans/`、`docs/spec/`、`workflows/feature-dev/landed/`、`wait_todo/`、`workflows/investigation/`）與**全部生成端 C# 程式碼**。移出的只有前端／工具／內容本身。

**還留在 `sub_projs/` 的三個**，理由各不同：`gemini-research/` 是餵本 repo findings 的原始素材；`inworld-skill-tree/` 與 `living-adventurers/` 雖然掛「消費者」名義，但**實作已經落地成 ModForge 的 generator 能力**（`skillTrees:` / `livingNpcs:` macro），設計文檔跟著 spec 走比較合理，體量也還小。

---

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）現在歸工作區 `analysis/`；`docs/` 只放 ModForge 自己的使用手冊。
