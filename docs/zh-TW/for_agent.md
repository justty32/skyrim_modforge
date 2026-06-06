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

- **NPC 現在可以是功能性角色** — 透過原版 ref 設定 `race` + `class`（+ `outfit`），NPC 就會像真實角色一樣行動。用 `items` 給 NPC 攜帶裝備（一份 `{ item: <ref>, count: N }` 清單 — 原版或規格內的 weapon/armor/potion/gold）；NPC 攜帶的武器會自動裝備（這就是武裝 NPC 的方式 — 給它真實的 `damage`，或 template 一把原版武器讓複製品繼承傷害，否則一把 0 傷害武器的評分會低於拳頭、永遠不會拔出來），而這些物品會在**死亡時**作為戰利品掉落。注意：`essential` NPC 永遠不會死，所以它的裝備無法被搜刮（而一個*活著*的 NPC 身上穿戴/裝備的物品只能透過偷竊看到，且需要 perk 才能取得）— 若希望玩家能殺死並搜刮，請改用 `protected`。
- **放置物件適用於室內空間與開放世界（室外）：** `placements` 可將 NPC/物件放入 (a) 規格中新建的室內空間（`cell` = 其 editorId；用 `coc <editorId>` 進入），(b) **原版室內空間**（`cell` = `"Skyrim.esm:0xFORMID"`，例如 `0x01605E` = Bannered Mare — `find <Skyrim.esm> <name> Cell`），或 (c) **室外/開放世界**（`worldspace` = `"Skyrim.esm:0x00003C"` = Tamriel — `find <Skyrim.esm> <name> Worldspace`；`position` 此時為世界座標，floor(x/4096)、floor(y/4096) 處的室外 cell 會被找出並覆寫）。所有原版放置會覆寫 cell/worldspace 以*新增*您的 ref（原版內容不受影響），並讀取遊戲 `Data` 資料夾 — 若非預設 Steam 路徑請設定 `MODFORGE_SKYRIM_DATA`。（未生成過的室外網格會得到一個全新的 cell — 僅結構性，未經遊戲內驗證；放置在既有地點附近是較安全的做法。）
- **物品/法術現在帶有遊戲屬性：** 武器有 `damage`/`speed`/`reach`，護甲有 `armorType` + 身體部位 `slots`，**法術/藥水有 `effects`**（MagicEffect *ref* + magnitude/area/duration），法術還有 `spellType`/`castType`/`targetType`/`baseCost`。一個有效果的藥水可完全運作；法術需要效果加上施放欄位。`effects[].magicEffect` *ref* 可以是原版 MGEF **或**規格中的 `magicEffects` 條目 — 為自訂效果撰寫 MGEF（`archetype`/`actorValue`/`magicSkill`/`resistValue`/`flags`/…）。
- **分級列表 + 容器：** `leveledItems`/`leveledNpcs`（加權等級門控條目，每筆為一個 *ref*）與 `containers`（物品 *ref* + 數量）— 戰利品表、商人箱等。
- **合成：** `recipes`（COBJ）讓物品（`createdObject` *ref*）可在 `workbench` 關鍵字處合成（預設為鍛造爐），消耗 `components`（物品 *ref* + 數量）。
- **職業：** `classes`（CLAS）定義 NPC 的「職業」— `healthWeight`/`magickaWeight`/`staminaWeight` + `skillWeights`（技能 → 0–255）+ `teaches`；npc 的 `class` ref 可指向一個。
- **戰鬥風格（CSTY）+ NPC.spells：** `combatStyles[]` 定義 NPC *如何*戰鬥 — 六個 `equipMult*` 欄位是 AI 的每武器類偏好分數（要做法師 NPC 就把 `equipMultMagic` 設高；原版 csVampireMagic 用 8.1）。npc 的 `combatStyle` ref 指向一個。搭配 `npcs[].spells`（SPEL ref 陣列，填入 AI 的法術清單），引擎會根據 CombatStyle 偏好從清單中選一個法術施放。用 `cstydiag <Skyrim.esm> <0xFORMID>` 檢視任何原版 CSTY 的數值。
- **AI 行動套件（PACK）：** `packages` 賦予 NPC 決策層行為（「在某處沙盒」、「前往旅店」等）。Skyrim 的 PACK 使用原版**程序模板**（`template` *ref*，例如 `Skyrim.esm:0x01C254` = Sandbox）來定義資料輸入結構；套件再填入這些輸入。支援的模板：Sandbox / Travel / UseMagic / Patrol / Follow / Escort（見 [lifelike/formid-reference.md](lifelike/formid-reference.md)）。`interruptFlags` 陣列（`HellosToPlayer`、`AllowIdleChatter`、`WorldInteractions`…）正是區分沉默雕像與生動 NPC 的關鍵。透過 `npcs[].packages` 把套件指派給角色。用 `packagediag <Skyrim.esm> <0xFORMID>` 傾印模板的插槽結構，或檢視任何套件。
- **更多記錄類型**（相同的規格→建置→dump 模式）：`ingredients`（煉金，含 `effects`）、`ammunitions`（`damage`）、`scrolls`（`effects` + 施放欄位）、`soulGems`（`maximumCapacity`）、`keys`、`keywords`（定義你自己的 → 從任何記錄的 `keywords` 引用它）、`outfits`（物品 *ref*；npc 的 `outfit` 可指向規格內的 outfit）、`statics`/`activators`（`model` `.nif` 路徑 — 參考原版網格 — 作為放置基底）。
- **外部/原版表單可被參考**（種族/職業/裝束/關鍵字/派系/魔法效果/放置基底+cell+worldspace/分級+容器條目，透過 `"<master>:0xFORMID"`）。
- **對話**記錄有效，但一條對話實際出現在對話中可能需要任務旗標/分支調整，且**沒有配音**（僅字幕）。
- **Story Manager（SM）事件驅動任務：** 在任務中加入 `storyEvent` + `aliases`，建置時自動在原版事件根下配線 SMBN→SMQN。支援的事件：`KillActor`、`ChangeLocation`、`CastMagic`、`AddItem`、`Assault`、`CraftItem`、`PlayerRemoveItem`、`Arrest`、`IncreaseLevel`、`ScriptEvent`。別名填充：`fromEvent:<槽>`、`uniqueActor:<ref>`、`forced:<ref>`、`createObject:<ref>@<targetAlias>`（在另一個 alias 處生成指向 `<ref>` 的新 ref — 遊戲內確認 2026-06-05）、`findMatching:closest`|`findMatching:any`（以載入區域中最近/第一個符合該 alias `conditions` 的既有 ref 填充，例如最近的 NPC）。alias 也可攜帶一段 Papyrus **alias 腳本**（`script`/`scriptSource`/`scriptProperties`，extends `ReferenceAlias`），它會跟著填入的 ref 一起走 — 例如在 `createObject` 生成的 ref 上掛 `OnActivate` 以串接下一個 story event（遊戲內確認 2026-06-05）。**遊戲內確認（2026-06-04）**，涵蓋所有變體（含 ESL 插件）。見 SPEC-dialogue-quests.md → 「Story Manager 任務」。
- **SM 任務的日誌推進：** 將某個 stage 標記為 `startUpStage:true`，引擎會在任務啟動的瞬間自動執行它，因此 SM 觸發的任務無需外部 `SetStage` 即可顯示開場日誌條目 / 顯示第一個目標。之後由玩家動作搭配 alias `script` 的 `OnActivate` 呼叫 `GetOwningQuest().SetStage(N)` 來完成/關閉它（可複用的 `examples/MFSE_AdvanceStage.psc`）。完整劇情弧示範：`examples/story-manager-queststage.json`（施法 → 顯示目標 + 生成箱子 → 開箱 → 完成目標 + 關閉任務）。遊戲內確認 2026-06-05。
- **一般任務上的別名（無 `storyEvent`）：** `aliases[]` 區塊在普通的 StartGameEnabled 任務上也有效 — `forced`/`uniqueActor`/`createObject`/`findMatching` 填充以及 alias `script` 全部適用（只有 `fromEvent` 在沒有事件時無效）。它們在任務啟動時（= 遊戲載入）填充。所以一個普通任務也能把 NPC/ref 強制填入別名、在其上生成物件、並攜帶 `OnActivate` alias 腳本，全程不需要 SM 事件。示範 `examples/quest-alias-standalone.json`。遊戲內確認 2026-06-05。
- **Script Event（自訂 Story 觸發）：** `ScriptEvent` 任務宣告一個 `keyword`；Papyrus 呼叫端透過 `MFStoryEventDispatch.Fire(kw, ref1, ref2, loc)` 發送事件，引擎將其路由到符合的 SM 任務。派發器 `.pex` 已嵌入 CLI，並由 `package` 自動複製到 `Scripts/`——無需每個模組單獨編譯 Papyrus。**遊戲內確認（2026-06-04）。**
- **可複用的觸發器函式庫** — 這一行 `Fire()` 呼叫接到四個真實的遊戲內進入點，全部遊戲內確認（2026-06-05）：magic effect（施放法術）、potion（飲用）、activator（`OnActivate`，拉動拉桿 — 模型必須是真實的原版 NIF）、dialogue 對話行（`TopicInfo` `Fragment_0`，NPC 給出任務），以及一段 **alias 腳本**（`ReferenceAlias` `OnActivate`，活化任務別名持有的某個 ref — 包括由 `createObject` 在執行時生成的）。範例：`story-manager-{magic,potion,activator,dialogue,alias}trigger.json`。
- 您無法在此確認任何東西能**在遊戲中**正常運作 — 那需要 Proton/Skyrim 實際啟動。請說「已產生並透過 dump 結構驗證」，而非「在遊戲中可運作」，除非有人類實際測試過。

當有需求超出限制時，請直接說明，並提供實際可做的部分。
