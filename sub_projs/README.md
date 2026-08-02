# sub_projs/ — 衍生專案

這裡放**把 ModForge 當工具使用的專案**，而不是工具本身。ModForge 的唯一目的是「JSON spec → Skyrim mod，且為 AI agent 適配」;這些專案**消費**它（或**作為它的基石**透過協議連接），但**不與它整合**——ModForge 不該為了某個專案長出特例功能。可以在推進這些專案時吸取經驗來改進工具，但專案本身不是工具的一部分。

目前 tracked（不 gitignore），方便在別處 clone 同步。某專案**成熟或體量龐大**後，再移去自己的 repo——見下方「已移出」。

| 專案 | 類型 | 狀態 | 說明 |
|------|------|------|------|
| `sofia-patch/` | 消費者 | 🟡 等實機 | Sofia × VIGILANT Acts 1-4 已打包交付；Acts 1 核心已實機確認（2026-06-14）；Acts 2-4 待首測；Act 1 各 beat 覆蓋/分支/嘴型/v2 動作尚待驗收。擴充計畫（F1-F14）已有設計稿，待功能開發。 |
| `followers-patch/` | 消費者 | ✅ 完成 | 8 份隨從 personality brief（Auri/Morgaine/Onean/Neisa/Remiel/Recorder/Serana/Mirai），作為日後評論 patch / 語音擴充的寫作依據。純參考資料，無 open 項。 |
| `skyrim-voicegen/` | 基石 | ✅ 可用（主力機） | 語音合成（text+emotion+ref→.wav）；靠 `MODFORGE_TTS_BIN` 協議接 ModForge，合約見 `PROTOCOL.md`；主力機（Wine + TTS）才能執行。 |
| `mod-survey/` | agent 工作區 | ✅ 大量已完成 | 拆解框架型（SPID/JContainers/PapyrusUtil/NFF/BOS/AOS…）、內容型（FCO/RDO/Lydia）、系統型（Extended Encounters/Missives）mod；浮現缺口已彙整進 roadmap。 |
| `tool-survey/` | agent 工作區 | 🟡 部分完成 | SkyrimIngameEditor 完整調查（擴展路徑清楚）；TES5Edit/F4RefToBlender/BodySlide 僅 Gemini raw，尚未深挖。 |
| `game-data/` | 共用參考 | ✅ 可用（主力機） | 抽取出的全遊戲文本/清單（vanilla+DLC+CC+mod）；`extract.sh` 重生，文本 gitignore。給 mod-survey / sofia-patch 唯讀取用。 |
| `gemini-research/` | 原始素材 | 📄 純存檔 | Gemini CLI 聯網搜尋原始輸出（combat-mods/npc-beautification/outfit-fitting/tool-survey）；品質參差，需人工篩選後才搬入正式 finding。 |
| `agent-bridge/` | 基石聯動 | 🟢 迴圈全通（DLL + mo2ctl + runner + MCP） | AI 全自動 mod QA 迴圈的**兩端**（計畫在工作區 `workflows/plans/ai-ingame-qa-loop.md`）。**遊戲內端**＝SKSE C++23 DLL，在 Skyrim 進程內開 `127.0.0.1:5099` HTTP：`GET /ping`、`GET /state`（player+game 永遠回傳，nearby/inventory/quests/**plugins** 走 `?include=`）、`POST /console`（含 `load <存檔>` 載入 baseline）。**Linux 端**＝`client/mo2ctl.py`（純 stdlib），免 GUI 裝卸 mod／啟動關閉遊戲；2026-08-02 對真實 109-mod load order 跑完整條 install→launch→`/state` 斷言→uninstall，三份 profile 檔 byte-identical 還原。**刻意與 `scene-capture-bridge` 分家**——後者是人用熱鍵驅動的創作工具、與內容一起出貨；本專案是測試治具，QA 跑完就卸，不能進玩家 load order。Linux clang-cl 交叉編譯直接出貨，不走 Windows CI。兩份 README 共五條血淚坑（`ConsoleLog::VPrint` detour 會 crash、winsock2 include 順序、profile 三檔換行符不一致、MO2 執行中改 profile 會被靜默回滾、進程比對要認 argv[0]）。**`qa.json` runner 已落地**（`client/qa_runner.py` + `QA-SCHEMA.md` + `examples/smoke.qa.json`）：一個檔描述整輪測試，smoke 31 秒跑完且 profile 零殘留；首跑就抓到 ModForge 寫 CELL override 沒保留 EDID 的真 bug。**MCP server 已註冊**（`client/qa_mcp.py`，`~/.claude.json` 與 houseCARL 並列）：`qa_status`／`qa_state`／`qa_console`／`qa_run`；刻意不暴露 install/launch/kill。**計畫主線走完**；`POST /screenshot`／`/input` 依 D6 延後。 |
| `inworld-skill-tree/` | 消費者 | 🟡 核心已落地、Phase 2 未做 | In-world 3D 星樹技能樹生成路線（Idea #20，玩家+NPC）。放棄 CSF，走 Campfire/Frostfall 世界內星樹 + JContainers per-NPC 狀態。U1–U5 全解；Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）+ Phase 3（generator `skillTrees:`）皆離線落地並 IN-GAME CONFIRMED。剩 Phase 2（NPC 版橋接）未動工 + Phase 0 實機驗收。 |
| `living-adventurers/` | 消費者 | 🟡 核心離線落地、等實機 | 給 standalone follower 一條命：人口/沈浸型 mod，一小撮具名持久冒險者過自主冒險人生（抽象幽靈模擬 + MoveTo 就地實體化），玩家各處撞見、酒館傳唱。idea #23。核心已落地：P1 泛化控制器（build 綠零警告）+ P2 `livingNpcs:` enroll macro（845 測綠）+ P3 玩家互動與 alignment（848 測綠）；待主力機編 .pex + 實機驗證。任務層（真 missive）卡 roadmap #7–9；cast 接真 standalone follower mod 未開。 |
| `darksouls-port/` | 消費者 | 🟡 P1 離線完成、已交付待實機 | 本機 DS Remastered 地圖移植成 Skyrim worldspace（首目標：北方不死院 m18_01_00_00）。P0 三段驗收 2026-07-05 全過（mesh/貼圖/碰撞路線 A 凸分解免 Mopp 實機成立；gltf2nif 反向後端隨之落地 model-converter）。P1「空殼院」離線完成、已交付（2026-07-06）：38 渲染 NIF + 47 碰撞件全量（4893 hulls→116 載體）+ 210 貼圖 + 自有 `DSPortWorld` → `DSPortP1.zip`（94MB）進 `~/skyrim_mods/mine/`，待實機。**移植資產僅本機、不發佈**。 |

---

## 已移出（獨立 repo，2026-08-02）

體量與生命週期都獨立了，抽出成 `projects/` 下的**同層 repo**，**未帶 commit 歷史**（舊歷史查本 repo 的 `git log -- sub_projs/<name>`）。對接方式**完全不變**——照樣靠協議/CLI，不整合。各留一份 stub 導引在原位置。

| 專案 | 新位置 | 類型 | 狀態 | 對接 |
|------|--------|------|------|------|
| [`godot-worldspace-editor/`](godot-worldspace-editor/README.md) | `../../godot-worldspace-editor` | 工具前端 | 🟢 整鏈 in-game 確認（2026-06-18）；剩 VTXT position row/col 目視、rotation 實機校準 | heightmap/splatmap PNG + `placements.json` → spec `heightmap`/`textureLayers`/`godotPlacements` |
| [`scene-capture-bridge/`](scene-capture-bridge/README.md) | `../../scene-capture-bridge` | 基石聯動 | 🟢 P1–P3 主線實機全過（2026-07-11）；P5 模式制待實機 | `scene.json`（＝合法 `ModSpec`）→ `build` 出 patch esp |
| [`model-converter/`](model-converter/README.md) | `../../model-converter` | 基石/工具 | 🟡 正反雙向 53 測綠；剩真實 `.nif` byte 覆蓋與實機驗 | 黑盒 exec，掛勾 `MODFORGE_NIF2GLTF_BIN` |

**留在 ModForge 的**：三者的**契約/spec/計畫/驗收文檔**（`workflows/specs/`、`workflows/plans/`、`docs/spec/`、`workflows/feature-dev/landed/`、`wait_todo/`）與**全部生成端 C# 程式碼**。移出的只有前端/工具本身的原始碼。


對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）留在 `docs/`、繼續 committed；只有體量太大的才 gitignore 主體、留摘要。
