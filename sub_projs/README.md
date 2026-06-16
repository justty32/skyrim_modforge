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
| `godot-worldspace-editor/` | 工具前端 | 🔵 規劃中 | Godot 4 + HTerrain 離線地形編輯器；匯出 PNG heightmap + placements JSON → ModForge LAND/REFR。MVP = 單格 PNG → COW 進入有地形起伏。 |

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）留在 `docs/`、繼續 committed；只有體量太大的才 gitignore 主體、留摘要。
