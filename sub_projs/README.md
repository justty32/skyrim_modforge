# sub_projs/ — 衍生專案

這裡放**把 ModForge 當工具使用的專案**，而不是工具本身。ModForge 的唯一目的是「JSON spec → Skyrim mod，且為 AI agent 適配」;這些專案**消費**它（或**作為它的基石**透過協議連接），但**不與它整合**——ModForge 不該為了某個專案長出特例功能。可以在推進這些專案時吸取經驗來改進工具，但專案本身不是工具的一部分。

目前 tracked（不 gitignore），方便在別處 clone 同步。某專案**成熟或體量龐大**後，再移去自己的 repo。

| 專案 | 類型 | 狀態 | 說明 |
|------|------|------|------|
| `sofia-patch/` | 消費者 | 🟡 等實機 | Sofia × VIGILANT Acts 1-4 已打包交付；Acts 1 核心已實機確認（2026-06-14）；Acts 2-4 待首測；Act 1 各 beat 覆蓋/分支/嘴型/v2 動作尚待驗收。擴充計畫（F1-F14）已有設計稿，待功能開發。 |
| `followers-patch/` | 消費者 | ✅ 完成 | 8 份隨從 personality brief（Auri/Morgaine/Onean/Neisa/Remiel/Recorder/Serana/Mirai），作為日後評論 patch / 語音擴充的寫作依據。純參考資料，無 open 項。 |
| `skyrim-voicegen/` | 基石 | ✅ 可用（主力機） | 語音合成（text+emotion+ref→.wav）；靠 `MODFORGE_TTS_BIN` 協議接 ModForge，合約見 `PROTOCOL.md`；主力機（Wine + TTS）才能執行。 |
| `mod-survey/` | agent 工作區 | ✅ 大量已完成 | 拆解框架型（SPID/JContainers/PapyrusUtil/NFF/BOS/AOS…）、內容型（FCO/RDO/Lydia）、系統型（Extended Encounters/Missives）mod；浮現缺口已彙整進 roadmap。 |
| `tool-survey/` | agent 工作區 | 🟡 部分完成 | SkyrimIngameEditor 完整調查（擴展路徑清楚）；TES5Edit/F4RefToBlender/BodySlide 僅 Gemini raw，尚未深挖。 |
| `game-data/` | 共用參考 | ✅ 可用（主力機） | 抽取出的全遊戲文本/清單（vanilla+DLC+CC+mod）；`extract.sh` 重生，文本 gitignore。給 mod-survey / sofia-patch 唯讀取用。 |
| `gemini-research/` | 原始素材 | 📄 純存檔 | Gemini CLI 聯網搜尋原始輸出（combat-mods/npc-beautification/outfit-fitting/tool-survey）；品質參差，需人工篩選後才搬入正式 finding。 |
| `agent-bridge/` | 基石聯動 | 🟢 0.4.0 + `mo2ctl` 實機驗過 | AI 全自動 mod QA 迴圈的**兩端**（計畫在工作區 `workflows/plans/ai-ingame-qa-loop.md`）。**遊戲內端**＝SKSE C++23 DLL，在 Skyrim 進程內開 `127.0.0.1:5099` HTTP：`GET /ping`、`GET /state`（player+game 永遠回傳，nearby/inventory/quests/**plugins** 走 `?include=`）、`POST /console`（含 `load <存檔>` 載入 baseline）。**Linux 端**＝`client/mo2ctl.py`（純 stdlib），免 GUI 裝卸 mod／啟動關閉遊戲；2026-08-02 對真實 109-mod load order 跑完整條 install→launch→`/state` 斷言→uninstall，三份 profile 檔 byte-identical 還原。**刻意與 `scene-capture-bridge` 分家**——後者是人用熱鍵驅動的創作工具、與內容一起出貨；本專案是測試治具，QA 跑完就卸，不能進玩家 load order。Linux clang-cl 交叉編譯直接出貨，不走 Windows CI。兩份 README 共五條血淚坑（`ConsoleLog::VPrint` detour 會 crash、winsock2 include 順序、profile 三檔換行符不一致、MO2 執行中改 profile 會被靜默回滾、進程比對要認 argv[0]）。剩 MCP server（2.2）、`qa.json` runner（3.x）；`POST /screenshot`／`/input` 依 D6 延後。 |
| `scene-capture-bridge/` | 基石聯動 | 🟡 骨架 + stub 離線落地、未編譯 | 遊戲內採集橋 SKSE DLL（Idea #24 元件③，spec §契約）。走訪 cell → 讀 base+transform+enable → FormID 反解 `<plugin>:0xLOCALID` → 吐 scene.json 餵 ModForge。建置架構改編自 my_skyrim_plugin_1（C++23 + CommonLibSSE-NG + vcpkg + nlohmann-json + clang-cl 跨編譯 + CI），plugin 邏輯自寫。骨架 + `SceneExporter` stub 2026-07-09 離線落地；待主力機 clang-cl / CI 首編（多處 `TODO(runtime-verify)`）。 |
| `godot-worldspace-editor/` | 工具前端 | 🟡 等實機 | Godot 4（自製 terrain）離線地形編輯器；匯出 PNG heightmap + splatmap + placements JSON → ModForge LAND/REFR。地形鏈已通並實機驗；**物件擺放 + 紋理（單層 BTXT + 多層 VTXT splatmap，含前端 splat 筆刷）整鏈離線完成（2026-06-17，待主力機 Godot GUI + xEdit byte-verify）**。剩 open＝box proxy 換真實 glTF（收斂到 model-converter）。 |
| `inworld-skill-tree/` | 消費者 | 🟡 核心已落地、Phase 2 未做 | In-world 3D 星樹技能樹生成路線（Idea #20，玩家+NPC）。放棄 CSF，走 Campfire/Frostfall 世界內星樹 + JContainers per-NPC 狀態。U1–U5 全解；Phase 0（純效果成長）+ Phase 1（玩家版 in-world 樹）+ Phase 3（generator `skillTrees:`）皆離線落地並 IN-GAME CONFIRMED。剩 Phase 2（NPC 版橋接）未動工 + Phase 0 實機驗收。 |
| `living-adventurers/` | 消費者 | 🟡 核心離線落地、等實機 | 給 standalone follower 一條命：人口/沈浸型 mod，一小撮具名持久冒險者過自主冒險人生（抽象幽靈模擬 + MoveTo 就地實體化），玩家各處撞見、酒館傳唱。idea #23。核心已落地：P1 泛化控制器（build 綠零警告）+ P2 `livingNpcs:` enroll macro（845 測綠）+ P3 玩家互動與 alignment（848 測綠）；待主力機編 .pex + 實機驗證。任務層（真 missive）卡 roadmap #7–9；cast 接真 standalone follower mod 未開。 |
| `darksouls-port/` | 消費者 | 🟡 P1 離線完成、已交付待實機 | 本機 DS Remastered 地圖移植成 Skyrim worldspace（首目標：北方不死院 m18_01_00_00）。P0 三段驗收 2026-07-05 全過（mesh/貼圖/碰撞路線 A 凸分解免 Mopp 實機成立；gltf2nif 反向後端隨之落地 model-converter）。P1「空殼院」離線完成、已交付（2026-07-06）：38 渲染 NIF + 47 碰撞件全量（4893 hulls→116 載體）+ 210 貼圖 + 自有 `DSPortWorld` → `DSPortP1.zip`（94MB）進 `~/skyrim_mods/mine/`，待實機。**移植資產僅本機、不發佈**。 |
| `model-converter/` | 基石/工具 | 🟡 載體已實作、等實機驗 | 以 Skyrim `.nif`（+dds）為中心的模型格式雙向互轉工具（↔ glTF/Godot ↔ FBX/OBJ）。收斂 model-porting 正向 + worldspace editor 需要的 nif→glTF 反向。**MVP 鎖＝vanilla nif→glTF 批量代理**。**2026-06-17 離線自寫參考後端 `nif2gltf/`**（Python+pygltflib）：手寫 Skyrim NIF 靜態 mesh parser（LE NiTriShape + SSE BSTriShape）、NiNode transform、Z-up→Y-up、含 skin→exit 3、batch manifest，不依賴 NifSkope。**反向 `gltf2nif/` 後端已落地**（2026-07-05，隨 darksouls-port P0 對真實 vanilla SSE `.nif` 逐 byte 核過）。正反雙向 **53 測綠**（正向 24 + 反向 29）。CLI 契約 `PROTOCOL.md`。剩 open＝更多真實 `.nif` byte 覆蓋（待主力機，SSE 半精度 offset 最需驗）。 |

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）留在 `docs/`、繼續 committed；只有體量太大的才 gitignore 主體、留摘要。
