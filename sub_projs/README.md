# sub_projs/ — 衍生專案

這裡放**把 ModForge 當工具使用的專案**，而不是工具本身。ModForge 的唯一目的是「JSON spec → Skyrim mod，且為 AI agent 適配」;這些專案**消費**它（或**作為它的基石**透過協議連接），但**不與它整合**——ModForge 不該為了某個專案長出特例功能。可以在推進這些專案時吸取經驗來改進工具，但專案本身不是工具的一部分。

目前 tracked（不 gitignore），方便在別處 clone 同步。某專案**成熟或體量龐大**後，再移去自己的 repo。

| 專案 | 類型 | 說明 |
|------|------|------|
| `sofia-patch/` | 消費者 | 用 ModForge 做 Sofia 風格隨從擴充；`README.md` 為索引。**agent 工作區**（劇情討論）|
| `skyrim-voicegen/` | 基石 | 語音合成（text+emotion+ref→.wav）；靠 `MODFORGE_TTS_BIN` 協議接 ModForge，合約見 `PROTOCOL.md` |
| `mod-survey/` | agent 工作區 | 調查 `~/skyrim_mods/` 那批已下載 mod；產出 `findings/`。方法見 [investigation/mod-survey-guide](../workflows/investigation/mod-survey-guide.md) |
| `game-data/` | 共用參考 | 抽取出的全遊戲文本/清單（vanilla+DLC+CC+mod）；`extract.sh` 重生，文本 gitignore。給上面兩個工作區唯讀取用 |

對比：**對其他 mod 的解碼/調查**（餵 ModForge roadmap 的參考）留在 `docs/`、繼續 committed；只有體量太大的才 gitignore 主體、留摘要。
