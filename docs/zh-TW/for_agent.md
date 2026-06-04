# FOR_AGENT — 以 AI 代理身份操作 ModForge

您（AI 代理）驅動 ModForge 將內容需求轉換為 Skyrim 外掛，並翻譯外掛文字。ModForge 是確定性的部分；**您是自然語言 → 規格的部分。** 您不手寫外掛位元組或 FormID — 您輸出一份 **規格**，工具輸出有效的 `.esp`/`.esl`。

## 驅動 ModForge 的兩種方式

| 路徑 | 適用時機 | 指南 |
|---|---|---|
| **CLI + JSON**（預設） | 描述模組 → 撰寫 JSON 規格 → 執行 CLI。可審閱、可差異比較、無需編譯步驟。 | **[for_agent_cli.md](for_agent_cli.md)** |
| **函式庫**（`ModForge.Core`） | 規格必須以程式*計算*而來 — 迴圈/條件判斷、從他處取得的資料、嵌入更大的程式、或以程式碼回應建置警告。 | **[for_agent_lib.md](for_agent_lib.md)** |

預設使用 CLI + JSON；只在規格必須計算而非手寫時才使用函式庫。兩種路徑產生相同的外掛，並共用下方的欄位參考與限制。

- **規格欄位參考（兩種路徑均適用）：** [SPEC-index.md](SPEC-index.md) · 完整範例：`../examples/sample_spec.json`
- **讓 NPC 更有生命力**（沙盒 / 日常生活 / 戰鬥 / 施法）：從 [lifelike/](lifelike/README.md) 開始 — 食譜、雙系統洞察、原版 FormID 參考、常見陷阱。
- **產生器背後的引擎機制：** [engine-internals.md](engine-internals.md)。
- **帶入您自己的網格 / 貼圖 / 音效 / 動畫**（自訂內容模組）：外部資源合約 — ModForge 負責參考與打包 vs. 您需要在別處製作的內容 — 詳見 **[external_assets.md](external_assets.md)**。

## 限制 — 請如實說明，不要過度宣稱

ModForge 輸出的是**結構有效**的記錄，這與**遊戲內可正常運作**是不同的：

- **NPC 現在可以是功能性角色** — 透過原版 ref 設定 `race` + `class`（+ `outfit`），NPC 就會像真實角色一樣行動。
- **放置物件適用於室內空間與開放世界（室外）：** `placements` 可將 NPC/物件放入 (a) 規格中新建的室內空間（`cell` = 其 editorId；用 `coc <editorId>` 進入），(b) **原版室內空間**（`cell` = `"Skyrim.esm:0xFORMID"`），或 (c) **室外/開放世界**（`worldspace` = `"Skyrim.esm:0x00003C"` = Tamriel；`position` 為世界座標）。所有原版放置會覆寫空間以*新增*您的 ref（原版內容不受影響），並讀取遊戲 `Data` 資料夾 — 若非預設 Steam 路徑請設定 `MODFORGE_SKYRIM_DATA`。
- **物品/法術現在帶有遊戲屬性：** 武器有 `damage`/`speed`/`reach`，護甲有 `armorType` + 身體部位 `slots`，**法術/藥水有 `effects`**（MagicEffect *ref* + magnitude/area/duration），法術還有 `spellType`/`castType`/`targetType`/`baseCost`。一個有效果的藥水可完全運作；法術需要效果加上施放欄位。`effects[].magicEffect` *ref* 可以是原版 MGEF **或**規格中的 `magicEffects` 條目 — 為自訂效果撰寫 MGEF（`archetype`/`actorValue`/`magicSkill`/`resistValue`/`flags`/…）。
- **分級列表 + 容器：** `leveledItems`/`leveledNpcs`（加權等級門控條目）與 `containers`（物品 ref + 數量）— 戰利品表、商人箱等。
- **合成：** `recipes`（COBJ）讓物品可在 `workbench` 關鍵字處合成（預設為鍛造爐），消耗 `components`（物品 ref + 數量）。
- **職業：** `classes`（CLAS）定義 NPC 的「職業」— `healthWeight`/`magickaWeight`/`staminaWeight` + `skillWeights`（技能 → 0–255）+ `teaches`；npc 的 `class` ref 可指向一個。
- **戰鬥風格（CSTY）+ NPC.spells：** `combatStyles[]` 定義 NPC *如何*戰鬥 — 六個 `equipMult*` 欄位是 AI 的每武器類偏好分數。npc 的 `combatStyle` ref 指向一個。搭配 `npcs[].spells`（SPEL ref 陣列）讓引擎根據 CombatStyle 偏好選擇法術施放。
- **AI 行動套件（PACK）：** `packages` 賦予 NPC 決策層行為（在某處沙盒、前往旅店等）。
- **更多記錄類型**（相同的規格→建置→dump 模式）：`ingredients`（煉金，含 `effects`）、`ammunitions`（`damage`）、`scrolls`（`effects` + 施放欄位）、`soulGems`（`maximumCapacity`）、`keys`、`keywords`、`outfits`、`statics`/`activators`（`.nif` 路徑 — 參考原版網格 — 作為放置基底）。
- **外部/原版表單可被參考**（種族/職業/裝束/關鍵字/派系/魔法效果/放置基底+空間，透過 `"<master>:0xFORMID"`）。
- **對話**記錄有效，但一條對話實際出現在對話中可能需要任務旗標/分支調整，且**沒有配音**（僅字幕）。
- 您無法在此確認任何東西能**在遊戲中**正常運作 — 那需要 Proton/Skyrim 實際啟動。請說「已產生並透過 dump 結構驗證」，而非「在遊戲中可運作」，除非有人類實際測試過。

當有需求超出限制時，請直接說明，並提供實際可做的部分。
