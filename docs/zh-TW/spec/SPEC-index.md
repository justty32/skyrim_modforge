# ModForge spec — 索引

> **「spec」消歧義**：本夾＝**JSON spec 欄位手冊**（一份 spec `.json` 可含哪些 key）。設計方案在 `workflows/specs/`；spec `.json` 檔本體在 `examples/`。

spec 是一個 JSON 檔 → `.esp` plugin。選擇一個主題：

| File | Contents |
|------|----------|
| [SPEC-intro](SPEC-intro.md) | 交叉參照與 ID、最頂層結構、完整 record 型別表 |
| [SPEC-magic](SPEC-magic.md) | 遊戲性數值、法術/藥水效果、自訂 MGEF、附魔（ENCH） |
| [SPEC-dialogue](SPEC-dialogue.md) | 職業、對話、閒聊、場景（SCEN）、CTDA 條件 |
| [SPEC-quests](SPEC-quests.md) | 任務階段與目標、Story Manager 事件任務、Papyrus 腳本 |
| [SPEC-identities](SPEC-identities.md) | 輕量化職業/身分系統（書→陣營+能力+問候+商人切換） |
| [SPEC-world](SPEC-world.md) | cell 與放置、地圖標記、自訂光源與照明（LGTM/IMGS/DALC）、in-world 技能樹 |
| [SPEC-worldspaces](SPEC-worldspaces.md) | 世界空間與區域、區域音樂、等級清單與容器、formLists、遭遇區、商販 |
| [SPEC-items](SPEC-items.md) | 配方（COBJ）、天賦、外部資產（網格/音效）、貼圖組（TXST） |
| [SPEC-packages](SPEC-packages.md) | AI 套件（Sandbox/Travel/UseMagic/Follow/Sleep/Patrol/Escort）、天氣與氣候 |
| [SPEC-animation](SPEC-animation.md) | 動作系統散裝檔：OAR replacer/moveset、BDI graph-var 注入、PIE 巨集表（`.hkx` 自備） |
| [SPEC-distribution](SPEC-distribution.md) | SKSE 分發器設定（無 ESP patch）：SPID `_DISTR.ini`、MCM Helper `config.json`＋`settings.ini`、FLM `_FLM.ini`、KID `_KID.ini`、BOS `_SWAP.ini`、AOS `_ANIO.ini`、SkyPatcher `.ini` —— 依過濾器分發／標記／交換／patch 記錄，零衝突 |
| [SPEC-workflow](SPEC-workflow.md) | CLI 工作流（`validate` / `build` / `package`）、語音克隆管線（`voicelines` / `extract-voices`）＋尚未涵蓋的功能 |
| [SPEC-refs](SPEC-refs.md) | `$ref` / `$env` 引入與參數化（具名預設庫、file/pointer/same-doc refs、env vars） |

CLI 快速參考：
```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp
dotnet run --project src/ModForge.Cli -- voicediag myspec.json out.esp
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp --plan
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir
```

另見：[lifelike hub](../lifelike/README.md) — NPC 食譜、cookbook、陷阱、formid 參考。
