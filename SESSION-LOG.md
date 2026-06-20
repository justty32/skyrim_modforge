# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **2026-06-20 D 組分發器實機 + MCM 補完整 session（接續同日大確認）**：
  - 🔧 **6 個 DLL loose-ini zip 全用 masterless-fix 後的 CLI 重 build**（先前是 fix 之前 build、esp 42b masterless 會被丟）；現在 esp 都正確掛 Skyrim.esm master（73b）。送 `~/skyrim_mods/mine/`。
  - ✅ **SkyPatcher（D-3）IN-GAME CONFIRMED**：`filterByRaces=NordRace:height=1.5` → 全 Nord NPC 變高 1.5×。**坑：NPC 尺寸 key 是 `height` 不是 `setScale`**（後者被 SkyPatcher 忽略）。新 visible-test `examples/skypatcher_scale_test_spec.json`。
  - ✅ **MCM Helper（D-2）從零補成完整功能 + IN-GAME CONFIRMED**：原本只生 loose config.json → **選單根本不出現**（早期「零 Quest/Papyrus」假設錯）。補了 ESP 端註冊：`Generator.Build.Mcm.cs`（`BuildMcmQuests` 生 Start-Game-Enabled QUST + `ModForgeMCM`/extends MCM_ConfigBase + `PlayerAlias`/`SKI_PlayerLoadGameAlias`）+ 可重用 `assets/papyrus/ModForgeMCM.psc`（embed .pex，編譯需 MCM Helper+SkyUI headers）+ `Package.cs` ShipEmbeddedPex gated。
    - 🔴→✅ **第二坑（config「check json syntax」）**：MCMHelper.log 顯示它找 `MCM/Config/<plugin-stem>/`，但 McmGen 寫到 `<spec-modName>/`。查 MCM Helper **C++ 源碼**（`ConfigStore.cpp`→`FormUtil.cpp:55` `path(plugin).stem()`）確認**資料夾名永遠 = 插件檔名 stem，不讀 Papyrus ModName property**。修 McmGen 用 `identity`（插件 stem）當資料夾 + config.json `modName` 欄位（self required-plugin）。+1 regression 測，796 綠。
    - **修正了誤導實作的錯誤 findings**（`mcm-helper-modforge.md`「純 ini 不需 Quest」、`mcm-helper-config-json.md`「modName=目錄名」）。docs（SPEC-distribution EN+zh-TW+html）、CODE_MAP.infra 同步。memory [[mcm-helper-registration-recipe]]。
- **2026-06-20 主力機實機大確認 session（接續 J+M）**：一個下午連續實機驗收多個離線功能 + 抓修一個系統性 bug。
  - ✅ **storageWrites（J 組）IN-GAME CONFIRMED**（見上條 + memory [[storage-writes-ingame-confirmed]]）。
  - ✅ **動態生怪管線 IN-GAME re-CONFIRMED**：MFSpawnDiagAny 走出旅館即生 3 怪（OnStoryChangeLocation 無過濾）。
  - ✅ **Idea #20 技能樹 Phase 0「施法練功」全鏈 IN-GAME CONFIRMED**：自訂法術 → MFSE_SpellTrigger → MFStoryEventDispatch.Fire → SM ScriptEvent → quest OnStoryScript → **JFormDB 持久（重複施放 lvl 累加）+ Adaptation perk 同步 + 好感度 gate（set Affinity 0 即停）** 全部實機通過。新 example `examples/skill_cast_spec.json`（含 dispatcher/spell-trigger/handler 三 .pex）。memory [[storage-writes-ingame-confirmed]] 旁附 JFormDB。
  - 🔴 **發現 + 修 + 防呆**：**引擎被動 CastMagicEvent SM root 不會對玩家普通施法觸發**（OnStoryCastMagic 從不跑；root 0x046829 雖合法存在）。原 flagship `npc_skill_persist_spec.json` 用它 → 死路。**修**：技能 demo 改走已驗證的 MGEF→dispatcher→ScriptEvent 路徑（`skill_cast_spec.json`）；**防呆**：`BuildStoryManager` 對 `storyEvent.event=CastMagic` 出 build-time Warn 指向 skill_cast；原 example 加 `_WARNING` 註記。memory [[masterless-plugin-silent-load-failure]] 同 session 的姊妹發現。
  - ✅ **修掉 masterless 靜默不載入 bug**（見下方 J+M 條的 🔴→✅）：根因 masterless（非 ESL），`PluginIo.Write` 零外部 ref 自動補 Skyrim.esm master。+2 測。
  - **測試方法論**：實機驗收靠「手工在生成的 fragment 加 Debug.Notification readback + 用 native Go papyrus-compiler 重編」把不可見的 KV/JFormDB 寫入變成畫面數字；單變數隔離 zip（masterless vs ESL）定位載入失敗根因。詳見 memory。
- **最近一次 session（2026-06-20 續，純離線，J + M 兩 roadmap 組收尾，~28 新測、793 測綠）**：
  - **J 組（PapyrusUtil StorageUtil `storageWrites`）**：`dialogue[].storageWrites` / `stages[].storageWrites: [{key, target, int/float/str, delta?}]` → `StorageUtil.Set/Adjust{Int,Float,String}Value`，掛 dialogue TIF 與 stage fragment（與 persist 同機制 + SM quest 路由 `OnStory<Event>`）。target=speaker/player/none 皆純表達式 → **body-only 零 VMAD property**。新增 `Generator.StorageWrites.cs`。JContainers 的 nested 狀態本就由 persist 覆蓋，這補「簡單+自動管理」那半（eval 最高槓桿點）。**剩 ActorUtil/MiscUtil/ref-target 留尾**（需求度低）。
  - **M 組（INFO 陣列批次 `variants`）**：`dialogue[].variants: [{responses, conditions?, emotion?, sayOnce?}]` → 同 topic 掛多條 sibling INFO，各帶 `Random` flag、共用 parent 的 speaker gate + conditions + templates + identity，再各接自有 conditions。parent `responses` 空→純批次 header。正解 FCO 265 條 ambient commentary 痛點（條件模板那半上次已落地）。
  - 兩組 docs（SPEC-quests/SPEC-dialogue）、schema、CODE_MAP、roadmap all-findings-gaps 同步。
  - **zh-TW 鏡像已同步**：SPEC-quests 補 storageWrites 段、SPEC-dialogue 補 variants + 「對選取做出反應」段，html 重生（spec-quests/spec-dialogue 兩頁）。
  - ✅ **storageWrites 實機確認（2026-06-20 主力機）**：PapyrusUtil 4 個 .psc（StorageUtil/PapyrusUtil/ActorUtil/MiscUtil）複製進 native headers cache `~/.cache/modforge/papyrus/Source/Scripts`（同 JContainers provisioning），`MODFORGE_PAPYRUS_HEADERS` 指它即可編 storageWrites fragment。用 spawn-isolation 隔離骨架（`examples/storage_writes_spawn_diag_spec.json`，setstage 觸發 + readback `Debug.Notification`）驗：`setstage MFSWSpawn_Q 10` → StorageUtil.AdjustInt/Float/SetString 寫入 + 讀回 round-trip 全成立，畫面 count 累加。example：`storage_writes_spec`（CastMagic SM）+ `storage_writes_diag`（純 setstage）+ `storage_writes_spawn_diag`（疊 spawn）。
  - ✅ **附帶抓到並修掉一個真 bug**：**masterless 的 esp 遊戲靜默丟棄**（不報錯、console `help`/`setstage` 都 not-found、MO2 勾了也沒用）。單變數隔離確認（ESL+master 版讀檔正常生怪+通知）→ **兇手＝masterless，ESL 無關**（一個外部 ref 都沒有的 spec 會輸出零 master 的 esp）。**修在 `PluginIo.Write`**：build 後若零外部 ref，自動補 Skyrim.esm 為唯一 master 並以 `MastersListContent=NoCheck` 寫（Mutagen 仍按 FormKey 正確映射 master index，FormID 不變、byte 與「天生有 Skyrim.esm master 的 ESL」一致）；有外部 ref 維持 `Iterate`。+2 測（`RelationshipAndEslTests`：masterless→補 master、有 ref→不重複），795 測綠。
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
