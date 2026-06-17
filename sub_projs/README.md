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
| `godot-worldspace-editor/` | 工具前端 | 🟡 等實機 | Godot 4 離線地形編輯器；匯出 PNG heightmap + placements JSON → ModForge LAND/REFR。地形鏈已通並實機驗；**物件擺放前端 + 單層 baseTexture 後端已做（2026-06-17，待主力機 Godot GUI 跑一次）**。剩 open＝per-vertex splatmap、box proxy 換真實 glTF。 |
| `inworld-skill-tree/` | 消費者 | 🔵 規劃中 | In-world 3D 星樹技能樹生成路線（Idea #20，玩家+NPC）。放棄 CSF，走 Campfire/Frostfall 世界內星樹 + JContainers per-NPC 狀態。主線設計已成稿，待 U1–U5 主力機/code pass 驗證。 |
| `model-converter/` | 基石/工具 | 🔵 規劃中 | 以 Skyrim `.nif`（+dds）為中心的模型格式雙向互轉工具（↔ glTF/Godot ↔ FBX/OBJ）。收斂 model-porting 正向 + worldspace editor 需要的 nif→glTF 反向。**MVP 鎖＝vanilla nif→glTF 批量代理；CLI 協議契約草案已成（`PROTOCOL.md`）**。缺口＝無已驗證的批量 nif→glTF 載體（待主力機測 NifSkope fork CLI）。 |

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）留在 `docs/`、繼續 committed；只有體量太大的才 gitignore 主體、留摘要。
