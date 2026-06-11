# ModForge 規格說明 — 目錄

規格（spec）是一個 JSON 檔案 → `.esp` 插件。選擇主題：

| 檔案 | 內容 |
|------|------|
| [SPEC-intro](SPEC-intro.md) | 交叉參照與 ID、頂層結構、完整記錄類型表 |
| [SPEC-magic](SPEC-magic.md) | 遊戲數值、法術/藥水 effect、自訂 MGEF、附魔（ENCH） |
| [SPEC-dialogue-quests](SPEC-dialogue-quests.md) | 職業、對話、閒聊、場景、CTDA 條件、任務階段、Story Manager 事件任務、Papyrus 腳本 |
| [SPEC-world](SPEC-world.md) | Cells 與放置、世界空間與地區、等級列表、遭遇區域、商販 |
| [SPEC-items](SPEC-items.md) | 配方（COBJ）、特技、外部資源（網格/音效）、材質集（TXST） |
| [SPEC-packages](SPEC-packages.md) | AI 套件（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、天氣與氣候 |
| [SPEC-workflow](SPEC-workflow.md) | CLI 工作流程（`validate` / `build` / `package`）、語音克隆管線（`voicelines` / `extract-voices`）+ 尚未涵蓋的功能 |

快速 CLI 參考：
```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir
```

另請參見：[lifelike 主頁](lifelike/README.md) — NPC 食譜、Cookbook、常見陷阱、FormID 參考。
