# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **最近一次 session（2026-06-20 續，純離線，J + M 兩 roadmap 組收尾，~28 新測、793 測綠）**：
  - **J 組（PapyrusUtil StorageUtil `storageWrites`）**：`dialogue[].storageWrites` / `stages[].storageWrites: [{key, target, int/float/str, delta?}]` → `StorageUtil.Set/Adjust{Int,Float,String}Value`，掛 dialogue TIF 與 stage fragment（與 persist 同機制 + SM quest 路由 `OnStory<Event>`）。target=speaker/player/none 皆純表達式 → **body-only 零 VMAD property**。新增 `Generator.StorageWrites.cs`。JContainers 的 nested 狀態本就由 persist 覆蓋，這補「簡單+自動管理」那半（eval 最高槓桿點）。**剩 ActorUtil/MiscUtil/ref-target 留尾**（需求度低）。
  - **M 組（INFO 陣列批次 `variants`）**：`dialogue[].variants: [{responses, conditions?, emotion?, sayOnce?}]` → 同 topic 掛多條 sibling INFO，各帶 `Random` flag、共用 parent 的 speaker gate + conditions + templates + identity，再各接自有 conditions。parent `responses` 空→純批次 header。正解 FCO 265 條 ambient commentary 痛點（條件模板那半上次已落地）。
  - 兩組 docs（SPEC-quests/SPEC-dialogue）、schema、CODE_MAP、roadmap all-findings-gaps 同步。
  - **zh-TW 鏡像已同步**：SPEC-quests 補 storageWrites 段、SPEC-dialogue 補 variants + 「對選取做出反應」段，html 重生（spec-quests/spec-dialogue 兩頁）。
  - ✅ **storageWrites 實機確認（2026-06-20 主力機）**：PapyrusUtil 4 個 .psc（StorageUtil/PapyrusUtil/ActorUtil/MiscUtil）複製進 native headers cache `~/.cache/modforge/papyrus/Source/Scripts`（同 JContainers provisioning），`MODFORGE_PAPYRUS_HEADERS` 指它即可編 storageWrites fragment。用 spawn-isolation 隔離骨架（`examples/storage_writes_spawn_diag_spec.json`，setstage 觸發 + readback `Debug.Notification`）驗：`setstage MFSWSpawn_Q 10` → StorageUtil.AdjustInt/Float/SetString 寫入 + 讀回 round-trip 全成立，畫面 count 累加。example：`storage_writes_spec`（CastMagic SM）+ `storage_writes_diag`（純 setstage）+ `storage_writes_spawn_diag`（疊 spawn）。
  - 🔴 **附帶抓到一個真 bug（調查中）**：**masterless + ESL 的 esp 遊戲靜默丟棄**（不報錯、console `help`/`setstage` 都 not-found、MO2 勾了也沒用）。原 `storage_writes_diag`（無外部 ref → masterless + esl:true）載入失敗；改 master+非 ESL 即正常。已做 ESL+master 單變數隔離（`storage_writes_esl_diag_spec.json`）定兇手＝masterless 還是 ESL；確認後對 ModForge 下修（output 一律 master Skyrim.esm，或 validate warn masterless）。
- **更早一次 session（2026-06-20，純離線大推進，~16 commit、774 測綠）**：一口氣推完一批 roadmap 離線功能 + zh-TW 鏡像全同步。
  - **zh-TW 鏡像全補**：SPEC-quests 同步（279→362）+ 補缺口（**新建 SPEC-distribution**、SPEC-worldspaces 補 4 bullet、SPEC-identities 查證已完整），SPEC-distribution 隨 D-group 擴成 7 框架全譯；html 重生。
  - **roadmap L 組**：`GetVMQuestVariable`/`GetVMScriptVariable` condition（讀 Papyrus property；code-pass 結論舊名 GetScriptVariable 不對）。
  - **D-group SKSE loose-ini 全完成（D-2/3/4/5/6/7）**：MCM Helper（`mcmConfigs:`）、SkyPatcher（`skyPatchers:`）、FLM（`formListInjects:`）、KID（`kidDistributions:`）、BOS（`objectSwaps:`）、AOS（`animObjectSwaps:`）——七個 SKSE 分發器/設定輸出全走 SPID 同款 loose-file pattern（含 SPID 共 7 個），docs 統一進 [SPEC-distribution.md](docs/spec/SPEC-distribution.md)。
  - **K 組**：quest stage `globalWrites: [{global,value}]` 一等 `SetValue` 語法（SM quest 路由 OnStory）。
  - **I 組**：`MagicEffectSpec.scripts[]` inline script-attach（DX co-location）。
  - **M 組**：`conditionTemplates` + `dialogue[].useConditionTemplates` 共用條件模板（FCO 265 條痛點；INFO 陣列批次建立另一半待做）。
  - **剩 in-game/runtime 驗收**（不擋使用）見 [WAIT_USER](WAIT_USER.md)：L variableName 格式、MCM live menu、FLM/KID/BOS/AOS/SkyPatcher runtime（裝對應 DLL 開 debug log）。
  - **roadmap 還剩離線可做**：J 組（JC/PapyrusUtil script 模板，scope 較模糊）、H 組（CSF in-game 技能樹，量大、有完整設計草案）——適合下次專注做。
- **更早一次 session（2026-06-19）**：**動態生怪 SM ChangeLocation 真因破解 + 修復**。診斷三段定位（OnInit/OnStoryChangeLocation MessageBox）實機確認：SM 啟動的 quest **不跑 startUpStage 的 Papyrus fragment**，但 `OnStory<Event>` 每次可靠觸發 → 把 storyEvent encounter 的 spawn/cooldown 觸發改掛 `OnStory<Event>` handler（`Generator.StoryTrigger`），實機生怪成功。`StoryManagerEvents` 加 `StoryHandler` 簽名、`Package.cs` .psc 複製 gate 補 `StoryTrigger`、新增 `smsub` SM 子樹 dump 工具，698 測綠。**剩真實 filtered encounter（走進盜賊營/地城 + 冷卻）的最終實機確認**見 [WAIT_USER](WAIT_USER.md)。詳見 memory `[[dynamic-spawn-debugging]]` + git log。
- **再更早 session（2026-06-18）**：Godot worldspace editor **WYSIWYG 整鏈全確認**——地形/紋理/物件 build 進遊戲、編輯器內顯示真實草貼圖 + 真實物件模型/貼圖；過程修了 LAND 紋理三鐵律（Layers flag/BTXT 0xFFFF/ATXT 0-indexed）+ nif2gltf SSE 精度 bug；新增 CLI `landdiag`/`texexport`/`nifexport`/`texpath`/`find 反查`；`WAIT_USER.md` 拆成 `wait_todo/`；編輯器 `.gd` 大檔按職責拆。細節在 landed/world.md + git log。
- **更早已落地**：docs/workflows 大重構、拆檔門檻定案、zh-TW+html 1:1 鏡像；語音 `sub_projs/skyrim-voicegen/`、Sofia `sub_projs/sofia-patch/`、`gamedata` CLI + `sub_projs/game-data/`。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #20 in-world 技能樹 — Phase 0 JContainers 持久層（好感度 gate + 施法觸發 + 編譯交付）全落地**（2026-06-18~19；🟢 離線 Phase 0 完備 + .pex 已編交付，剩實機見 WAIT_USER）：結構化 JFormDB `persist`+`syncPerks`（對話 TIF / quest stage / 任意-ref key）；**好感度 gate**（`gate:{global,atLeast?,atMost?}` 把寫入/sync 包進 GLOB 閾值 `If`，Sofia F6 藍圖，綁 `PGate`/`SGate`、validation 擋未解 GLOB+反向 band）；**施法即觸發**——新增 `StoryHandlerNeeded`，有 storyEvent 的 quest 其 stage persist 自動路由到 `OnStory<Event>` handler（SM quest 不跑 startUpStage fragment，沿用動態生怪真因），example 重做成 CastMagic SM quest（玩家施任何法術即長技能，免找 NPC）。解 design U5，707 測綠。**.pex 已主力機編好**（JContainers 12 .psc 併入 native headers cache + `MODFORGE_PAPYRUS_HEADERS`），FLAT zip 交付 `~/skyrim_mods/mine/ModForgeNpcSkillPersist.zip`。durable [design](sub_projs/inworld-skill-tree/design-inworld-jcontainers.md)、memory `[[headless-jcontainers-papyrus-headers]]`。⚠ zh-TW SPEC-quests 鏡像待同步（整個 persist 段落）。

- **Idea #19 Godot Worldspace Editor — WYSIWYG 整鏈已落地**（2026-06-18 GUI/in-game 全確認）：地形/紋理/物件 build 鏈 + 編輯器**真實貼圖 + 真實物件模型/貼圖**都實機確認；nif2gltf 對真實 vanilla nif 修復；`.gd` 大檔已按職責拆。完整收進 [landed/world.md](workflows/feature-dev/landed/world.md)「Godot 編輯器 WYSIWYG」「model-converter」條。剩非阻塞小尾巴（物件 normal/spec 貼圖、LE-format nif、VTXT position 目視）在 [WAIT_USER](WAIT_USER.md)。
