# ModForge — Claude Code 專案備忘

## 開發環境

- 測試：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`（net10.0）
- 離線測試（不需要 Skyrim.esm）：`dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"` 或 `scripts/test-offline.sh`
- 需要本機 `Skyrim.esm` 的測試已標記 `Category=RequiresSkyrim`；一般離線迴歸請排除該 category
- Commit 訊息用多個 `-m` flag 組多行（PowerShell here-string 易出問題）
- 重構必須行為不變（behavior-preserving）；不要未經確認就 push 或開新工作

**前置步驟（fresh clone 後，`dotnet build` 前必做一次）：**
`assets/papyrus/MFStoryEventDispatch.pex` 被 `ModForge.Cli.csproj` embed 為 EmbeddedResource，但 `.pex` 在 `.gitignore` 裡不進 repo。需先編譯：
```
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFStoryEventDispatch.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFSceneBanterController.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityBook.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityDefault.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityController.psc assets/papyrus/
dotnet run --project src/ModForge.Cli -- compile assets/papyrus/MFIdentityAutoGrant.psc assets/papyrus/
```
（需要 Wine + CK PapyrusCompiler 環境；native 走 `~/tools/papyrus-compiler` + `MODFORGE_PAPYRUS_HEADERS=~/.cache/modforge/papyrus/Source/Scripts`。）這六個 `.psc`（dispatcher、在場偵測 Scene controller、身份書 MFIdentityBook、預設身份授予 MFIdentityDefault、主身份 controller MFIdentityController、自動授予 trigger MFIdentityAutoGrant）有任何改動時，同樣需要重跑對應步驟並將新的 `.pex` 保留在本機（不 commit）。六個 `.pex` 都被 `ModForge.Cli.csproj` embed 為 EmbeddedResource（條件式：缺檔仍可 build，runtime 才 warn）。

## 程式碼慣例

- `partial class` 按領域拆檔：CLI 是 `Program.cs` + `Diagnostics.*.cs` + `Package.cs`；Core 是 `Generator.Build.*.cs`
- 所有 src 檔案維持在 300 行以下
- **Spec 欄位 breaking change**：新增欄位安全（optional，舊 example 不受影響）；**刪除或改名欄位**前必須先 `grep -r "舊欄位名" examples/`，找出所有受影響的 JSON 並在同一個 commit 裡一起更新。
- **新增 Spec 欄位後**：手動更新 `examples/spec.schema.json`（IDE autocomplete 用；無自動同步機制，允許偶爾落後，但 commit 前盡量補上）。

## CODE_MAP 工作流程

程式碼導航 index 在 `docs/CODE_MAP.md`（頂層）→ 五份子 index：

| 子 index | 涵蓋 |
|---------|------|
| `docs/CODE_MAP.dialogue-quests.md` | quest / dialogue / scene / Story Manager / ScriptEvent / word wall |
| `docs/CODE_MAP.world.md` | cell / placement / worldspace / region / leveled list / container / encounter zone |
| `docs/CODE_MAP.items-magic.md` | weapon / armor / spell / magic effect / enchantment / perk / shout / long-tail |
| `docs/CODE_MAP.npcs-packages.md` | NPC / faction / class / AI package / combat style / weather / climate |
| `docs/CODE_MAP.infra.md` | CLI / build orchestrator / validate / package / Papyrus / translate / plugin I/O |

三個面向構成維護鏈：**程式碼（含 examples/ 與 assets/）→ CODE_MAP → 文檔**（HTML bundle 最低，只在明確要求時更新）。

`examples/*.json`、`examples/scripts/*.psc`、`examples/assets/`、`assets/papyrus/MFStoryEventDispatch.psc`、`spec.schema.json` 均視為**源碼**——功能變動時必須同步，不是次要的附屬物。

**優先級（衝突或時間不夠時，依序保持一致）：**
程式碼（含 examples + assets）> CODE_MAP > 文檔（`docs/SPEC-*.md` / `for_agent*.md`）> HTML

**CODE_MAP 與程式碼衝突時：以程式碼為準，立即修正 CODE_MAP。**

**日常規則：**
1. **修改前**：先讀 `docs/CODE_MAP.md`，找到相關子 index，只讀清單中列出的檔案——不要讀無關領域的檔案。
2. **修改後**：若新增或刪除了 `.cs` 檔案，或某檔案的職責有顯著改變，必須同步更新對應子 index（含 Tests 欄）。
3. `.cs` 檔案本身不加「對應 CODE_MAP」的註釋（維護成本過高）；反向查找直接 `grep` CODE_MAP 文件。

## 主要工作流程

### Workflow 1：新增功能

```
修改程式碼（增量）
  → 使用者測試 → 回報問題 → 修程式碼 → 重複
  → 全數通過後：補齊 CODE_MAP → 補文檔 → commit
```

- 測試迭代期間，CODE_MAP / 文檔可以暫時落後。
- 若迭代跨越多個 session，在本檔「進行中的方向」補一行 `[功能名] 文檔/CODE_MAP 待同步`，下一個 session 接手時不會誤以為已同步。
- **commit 前**：CODE_MAP + 文檔必須對齊（HTML 不要求，examples/assets 視情況）。

### Workflow 2：重構整理（拆分 / 模塊化）

維護鏈中**一次只動一個面向**，做完 commit 再看下一個：

```
Step 1  程式碼重構（behavior-preserving 拆分）
          → 立即更新 CODE_MAP 與相關文檔以對齊新結構
          → 跑測試確認行為不變 → commit

Step 2  （視需要）CODE_MAP 若臃腫 → 單獨重構 CODE_MAP
          → 同步更新 CODE_MAP 中連結到的文檔段落 → commit

Step 3  （視需要）文檔若臃腫 → 單獨重構文檔
          → 同步更新 CODE_MAP 中指向這些文檔的連結 → commit

Step 4  （視需要）examples/assets 若需更新 → 單獨處理 → commit
```

- **禁止**：同一 session 內同時重構超過一個面向。
- 每個 Step 完成前不啟動下一個，確保任意時間點維護鏈是一致的。

## 進行中的方向

> **當前進度／每個 session 做了什麼 → 寫 `docs/SESSION-LOG.md`（不要寫進本檔）。**
> **待實機測試的項目 + 該怎麼測 → 寫 `docs/INGAME-TEST-QUEUE.md`（不要寫進本檔或 session log）。**
> 本檔只放 durable 的東西：專案慣例、`已落地功能` 目錄、`鐵律與踩坑`、`之後可做` roadmap。
> in-flight 狀態與 session 進展在 session log；in-game 待確認與測試步驟在 test queue；
> 功能真正 in-game 落地後，才把濃縮的一句話 + 實作細節指標移進下面的 `已落地功能`。
> 想法備忘錄另見 `docs/IDEAS.md`。

### 已落地功能（時間序；實作細節見 git log / CODE_MAP / SPEC）

**對話 / 任務 / Story Manager**
- **SM spec 管線**：`QuestSpec.storyEvent`(event+conditions) + `aliases`；build 自動生 SMBN→SMQN 掛原版根、清 StartGameEnabled。事件表 `StoryManagerEvents`（十個 engine-native 事件）。
- **alias fill 五種**：`fromEvent:<slot>` / `forced:<ref>` / `uniqueActor:<ref>` / `createObject:<ref>@<alias>` / `findMatching:closest|any`。
- **可複用 trigger 庫（五入口，同一 `Fire()`）**：magic-effect / potion / activator / dialogue / alias-OnActivate。zip `~/skyrim_mods/ModForge{Magic,Potion,Activator,Dialogue,Alias}Trigger.zip`。通用派發器 `assets/papyrus/MFStoryEventDispatch`（embed 進 CLI）。
- **Quest 階段**：`StageSpec.startUpStage`（啟動自顯 objective）+ stage 推進（`MFSE_AdvanceStage.psc`）；alias 也適用一般（非 storyEvent）quest。
- **身份系統（輕量職業）Phase-2/C 完成**（in-game 確認 2026-06-07，`ModForgeIdentity.zip`）。**完整欄位/語意見 `SPEC-dialogue-quests.md`「Identities」、build wiring 見 `CODE_MAP.dialogue-quests.md`、建好的 esp 用 `identitydiag` 探。** 一句話地圖：
  - **取得**：讀書（`MFIdentityBook`）/ `default:true` 開局自動授（`MFIdentityDefault`）/ `autoGrantWhen{actorValue,threshold}` 玩家 AV 過門檻自動授（`MFIdentityAutoGrant`，如 Dragonborn `DragonSouls≥1`；純 Papyrus 讀 AV、免 SKSE）。
  - **閘**：`identity`（持有）/ `primaryIdentity`（主身份，由 `MFIdentityController` poll 算 `MF_PrimaryIdentity` GLOB、`setPrimaryIdentity` 對話可覆寫）；`activeWhen` 情境窄化正向閘。
  - **授予**：`grants`(SPEL) + `grantPerks`(PERK，如 smite-vs-undead 條件 perk)。
  - **互動**（多動作 TIF result fragment，皆純 record+生成、無 user script）：`hello` 招呼 / `openBarter` 交易 / `rewardItem` 獎勵 / `evaluateSpeakerPackages` 重評估 → 組出 Adventurer 護衛 quest（follow PACK gated on `GetStage`）。
  - **四個 reusable .psc**（Book/Default/Controller/AutoGrant）embed 進 CLI、Package 條件式出貨。
  - **踩坑**：`MFIdentityBook` 必 **extends ObjectReference**（OnRead 非 Book event）[[book-onread-needs-objectreference]]；state-varying 招呼=**一個 Hello topic 多條 INFO**（順序定優先，非多 topic 競 priority）[[conditioned-hello-one-topic-many-infos]]；NPC `autoCalcStats` 必配 `class` 否則 0 血倒地 [[autocalc-without-class-dead-npc]]；acquire scene 用 `beginOnQuestStart:false`（書 Start() 唯一觸發）。
  - **未做**：#3 聲望/行為追蹤（需先定設計）。

**Scene 劇情演出**
- **在場偵測 autoStart**：`SceneSpec.AutoStart`（triggerDistance/LOS/cooldown/poll/**brawlOnEnd**）+ 可複用 `MFSceneBanterController`。`ModForgeSceneBanter.zip`。
- **非對話 action**：beat phase + Package（走位，引用 `packages[]` 的 PACK）+ Timer（停頓）。`ModForgeSceneAction.zip`。
- **重播策略**：`playOnce` / `playHour(+tolerance)` / `gateGlobal`。`ModForgeReplayPolicy.zip`。
- **per-phase headtrack/facing**：`ScenePhaseSpec.HeadtrackActor/HeadtrackPlayer/FaceTarget`。
- **scene 條件**：`SceneSpec.Conditions`（scene-level，僅 `beginOnQuestStart`）+ `ScenePhaseSpec.StartConditions/CompletionConditions`（per-phase）。
- **PlayIdle 演出**（in-game 確認 2026-06-07）：`SceneActionSpec.Idle`（IDLE ref）→ SCEN `SceneAdapter` per-phase OnStart fragment（`SF_<scene>.Fragment_<phase>` 跑 `<alias>.GetActorRef().PlayIdle()`，第三種 fragment 家族）。純產生器 `Generator.SceneFragments.cs`、掛載 `AttachSceneFragments`；idle action 同時發一個 Timer（hold）讓 phase 能 run。`find <esm> <kw> idle` 探 IDLE。`ModForgePlayIdle.zip`（宣誓鞠躬+獻手）。

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

**新 record builders**
- **GlobalVariable (GLOB)**：`GlobalSpec`（short/long/float + constant）。
- **Light (LIGT)**：`LightSpec`（color/radius/fade/flags…），用 placements 放置。
- **Projectile (PROJ) + Explosion (EXPL)**：自訂法術飛行彈+爆，鏈 EXPL←PROJ←MGEF←SPEL。
- **MGEF 擴充**（2026-06-13）：`archetype:"Script"`（VMAD 掛 ActiveMagicEffect 腳本，走通用 `scripts[]{targetEditorId:<mgef>}`）+ **DualValueModifier**（`secondActorValue`/`secondActorValueWeight`，一法術扣兩條 AV）。**Health+Stamina in-game 確認 2026-06-13**；**踩坑**：Concentration+Aimed 需 `castingArt`+`projectile` 否則 CTD。
- **FormList (FLST)**（2026-06-13，offline）：`formLists[]`（editorId + items 任意 record ref，順序保留）。FLST **無**獨立 `GetIsInList`，走既有 `*OrList` param（GetItemCount/GetEquipped/GetIsVoiceType/GetInWorldspace 收 FormList）。
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。
- **NPC patch（override vanilla NPC）**（in-game 確認 2026-06-13）：`npcPatches[]` override 既有 NPC 的 `Packages` 等（AI Overhaul 核心，如 Carlotta 留在家）。headless 解 localized Name 的字串牆解法見 memory [[headless-vanilla-strings-provision]]；輸出英文名 inline、non-localized。`examples/npc_patch.json`、`npcdiag`。注意 USSEP/load-order 衝突。
- **Map marker (XMRK) on vanilla worldspace**（in-game 確認 2026-06-13，`ModForgeQuestMarkers-mapfix9.zip`）：`mapMarkers[]`（name/worldspace/position/type/flags）放進 Tamriel persistent cell，可見+可傳送+大地圖底圖完整渲染。**override vanilla WRLD（`CopyWorldspaceEnv`）的兩條鐵律**——詳見「鐵律與踩坑」與 memory [[worldspace-override-must-carry-topcell]]/[[worldspace-override-map-render-fields]]：① 持久 cell（0xD74）要帶 `MajorRecordFlagsRaw=0x00040400`（CopyCellEnv 不複製 record-header flag → CTD）；② 地圖渲染要帶 **EDID + RNAM + TNAM/UNAM 但永不帶 OFST**（OFST=Skyrim.esm 絕對檔案偏移量不可移植；缺 EDID=白圖有高度；缺 RNAM=破圖）。`examples/quest-markers.json`、`mapmarker`/`xmarker` placement kind。
- **Hazard (HAZD)**（2026-06-13，offline 完整、未實機）：`hazards[]`（model/radius/lifetime/targetInterval/limit/spell/flags + light/sound/imad/impactDataSet）。兩種用法：①法術噴出（MGEF `archetype:"SpawnHazard"` + `association`，複用既有 MGEF wiring）②放置（`placements[].base` 是 HAZD 或 `kind:"hazard"`→`PlacedHazard`）。見 `SPEC-magic.md § hazards`、`CODE_MAP.items-magic.md`、`examples/hazard.json`。
- **Music (MUSC + MUST)**（2026-06-13，offline 完整、未實機）：`musicTracks[]`（MUST：SingleTrack→`.xwm`／Palette→子軌池／SilentTrack + loop）+ `music[]`（MUSC：flags/priority/`duckingDecibel`(正 dB 0–655)/tracks）。掛 `cells[].music` + `worldspaces[].music`（後者沿用既有 wire）。音檔 loose asset 走 `assets`。見 `SPEC-world.md § music`、`CODE_MAP.items-magic.md`、`examples/music.json`。**踩坑**：`duckingDecibel` 負值記憶體 OK 但 CLI build 寫檔 range-check（0–655）會炸。

**光照管線（明亮室內）**（in-game 確認 2026-06-09，`ModForgeBrightInterior.zip`）：`LightingTemplate (LGTM)` + `ImageSpace (IMGS, ≠ 既有 IMAD)` base record，模板抄 vanilla + 只覆寫亮度欄位；CELL 逐欄光照 `cells[].lightingTemplate/imageSpace/lighting(inline XCLL)`，含 **DALC 六方向環境光**（打亮地城核心：LGTM→`DirectionalAmbientColors`、XCLL→`AmbientColors`）。inline 無給且有 template → 全繼承。診斷 `lgtmdiag`/`imgsdiag`。**欄位/語意見 `SPEC-world.md § lighting`、wiring 見 `CODE_MAP.world.md`。** 踩坑：① interior CELL 無 XCLL = 黑房；② IMGS 不給 `template` 從零起（HDR 欄位全 0）行為可能怪，建議抄 vanilla IMGS 再調；③ build 期 `ResolveLightingRef` 不分型別，靠 Validate 的 cross-type 檢查擋打錯 slot。

**光照管線（室外調色）**（in-game 確認 2026-06-09，`ModForgeBrightWeather.zip`）：IMGS 掛 **Weather** per-ToD —`weathers[].imageSpaces`（`default` 補未設時段 + sunrise/day/sunset/night；ref=in-spec IMGS 或 vanilla；pass-2 `WireWeatherLinks` 接、`weatherdiag` 探）。**`WeatherSpec.template`**（抄 vanilla 天氣，DeepCopy 繼承雲/雲貼圖/天空色/大氣，只覆寫 spec 給的；null 色保留模板、空 clouds 保留模板雲）——**from-scratch 天氣無雲**故室外務必抄 template（如 `Skyrim.esm:0x10E1F2` SkyrimClear_A）。室外光由 weather sky/sunlight/ambient 顏色（既有）+ per-ToD IMGS grading 決定，LGTM/CELL 室內專用不適用室外。實機 `fw <weatherFormID>` 非侵入測。**未做**：weather/IMGS 掛 region。（明亮 LGTM/IMGS「具名 preset 庫」已由 `$ref`/`$env` 解析層落地，見下。）

**Voice pipeline（TTS → XWM → FUZ loose asset）**（**in-game 確認 2026-06-13**，真 F5-TTS 克隆 MaleNord 嗓音在自訂 NPC 上實機播出，`ModForgeVoiceTest.zip`）：`voiceTemplates[]` + `npcs[].voiceTemplate` + `voiceLine{format,skipLip}`；CLI `voicelines <spec> <built.esp> [--dry-run|--plan]` 走 built plugin 的 INFO，解析 speaker（GetIsID / GetIsAliasRef / GetInFaction / SceneAction），一個 distinct `voiceType` folder 產一份 voice file；`voicediag <spec> <built.esp>` 離線列 INFO→speaker→voiceType→template→`Sound/Voice/<plugin>/<voiceType>/<quest10>_<topic15>_<infoFormId8>_<n>.fuz`，可在 TTS 前失敗。TTS 透過 `MODFORGE_TTS_BIN`（本機 `voicegen.py` wrapper）；engine 目前：`f5` 已接、`fish-s2` 轉呼 `MODFORGE_FISH_SPEECH_BIN`（wrapper 必寫 WAV），`chatterbox/gptsovits/xtts` 為保留名。音訊封包：`MODFORGE_XWMAENCODE` 用 Wine 跑 CK `xwmaencode.exe`，`Voice.EncodeXwma` 需 `winepath -w` 轉 Windows path；缺 xWMA 時降級 loose `.wav`，**不把 raw PCM 塞進 `.fuz`**。lip：**首選官方 CK `LipGenerator.exe`（`MODFORGE_LIPGEN`）**——簽名 `<wav> <text> -Language:<lang> -OutputFileName:<lip>`、FonixData.cdf 自 exe 同夾找（免另設 cdf）、本機 `Tools/LipGen/LipGenerator/` 已備、**Wine 實跑產出合法 .lip 確認 2026-06-13**；退化路徑社群 `MODFORGE_FACEFX` + `MODFORGE_FONIXDATA`；皆缺則 no-lip/static mouth（`voicelines` 開頭發一次 warning）。打包：voice files 是 loose assets，不嵌入 ESP/ESM；對最終 mod folder 的 plugin 直接跑 `voicelines`，或把已產出的 `Sound/` 當 `package --assets <dir>` 餵入。**真模型實機跑通的設定（細節見 memory [[voice-gen-interface-future]]）**：① RTX 50 系（Blackwell sm_120）torch 要 **cu128** 不是文檔舊寫的 cu124；② venv 用 python 3.11（3.14 太新）；③ `MODFORGE_TTS_BIN` 被**直接 exec**，故須是「在 f5 venv 內跑 voicegen.py」的 wrapper（`voicegen-f5.sh`）；④ **F5 在 `ref_text=""` 時自動 Whisper 轉寫 ref**，故 vanilla 抽出的 ref 免手寫 transcript（voicegen.py f5 分支已 default ""）；⑤ ref 用 `extract-voices <Voices BSA> MaleNord` 抽 vanilla clip（`examples/refs/` 已 gitignore，vanilla 音檔不 commit）；⑥ xWMAEncode 直接吃 F5 的 24kHz mono PCM，免 resample；⑦ deterministic build 保 INFO FormID 穩定 → `.fuz` 檔名重 package 後不變，仍須 `voicediag` 對 packaged esp 比對 planned vs shipped 再 zip。**踩坑**：空的自訂 CELL（只有 record、無地板 static/無光）會讓放置的 NPC 直接墜落 → 測試 NPC 放真 vanilla 室內、抄一個 vanilla actor 的 z=0 地板座標（Borin 放 `RiverwoodSleepingGiantInn` 0x0133C6、抄 OrgnarREF 0x013486 旁）。lip sync **in-game 確認 2026-06-13**（`ModForgeLipTest.zip`，MF_Smith/Borin 講話時嘴型跟音節動）：官方 CK `LipGenerator.exe`（`MODFORGE_LIPGEN`）Wine 跑通 → `.fuz` 內嵌真 .lip（lipSize>0），跟上次 voice-only 測試（lipSize=0、閉嘴）唯一差別就是這顆 lip。穩流程：先 `package` 到最終資料夾 → 再對該夾內 plugin 跑 `voicelines`（voice 夾名才會 = plugin 名）→ `voicediag` 比 planned vs shipped → flat zip。

**Spec `$ref`/`$env` 解析層**（反序列化前 JSON 預處理器，`SpecRefs.cs`，純 `JsonNode`，全 offline 測試）：任何欄位可放 `$ref`（引另一檔 / 檔#pointer / 同檔 `#/pointer`；值為 **string** | **array 鏈式後蓋前** | **long-form `{from,pointer}`**，`from` 可被 `$env` 驅動）與 `$env`（值 / `default` / 缺報錯）。`$ref` 旁同層 sibling deep-merge **蓋上去（sibling 贏；物件遞迴合併、陣列整個取代）**。CLI `ResolveSpecJson` 單一 chokepoint，`ReadSpec` + `validate` 都在解析後 JSON 上跑（unknown-field 檢查也是）。「具名 preset 庫」= `examples/presets/` 一資料夾 preset 檔（首發 `bright-interior.json`，demo `spec-refs-demo.json`）。**欄位/語意見 `SPEC-refs.md`、wiring 見 `CODE_MAP.infra.md`。** 踩坑：① `$ref` 路徑相對**引用它的那份文件**（preset 檔自己的 `$ref` 相對 preset 檔目錄，非頂層 spec）；② 同層 sibling 蓋 ref、陣列取代不串接；③ `$env` 缺且無 `default` 是**硬報錯**（刻意，無靜默空值）；④ 指令在 deserialize 前就消失，故 `spec.schema.json` 不強制、不衝突它自身的 schema-內 `$ref`。

**一次測 showcase**：`ModForgeShowcase.zip`（批次#1：Light + headtrack + SitTarget）、`ModForgeShowcase2.zip`（批次#2：firebolt PROJ/EXPL + NPC 武器 + scene 閘）。新 diag：`smtree` / `scnscan` / `packagediag` / `lightdiag` / **`identitydiag`（從建好的 esp 還原身份 registry：controller faction↔code、default grants、acquire books、控制 GLOB）** 等。

### 鐵律與踩坑（複用知識，勿重蹈）

- **SM 結構** [[story-manager-kill-recipe]]：一事件根→一條共用分支→多 quest node（串 PreviousSibling）；事件根下多分支互斥；**引擎一事件只啟動一個最先符合的 quest**（正確 radiant，非 bug）；ESL 能裝 SM；`SimpleActor`（雞/兔）不發 Kill 事件。
- **SM alias** [[story-manager-kill-recipe]]：① location 槽 alias 必須 `Type=Location`（fromEvent 'L' 自動）；② 任一必填 alias 填不上 → quest 靜默不啟動；③ 殺/指向被 `ReservesLocationOrReference` 保留的 NPC 需 `allowReserved`（uniqueActor 強制）；④ `QuestAlias.Flags` nullable，旗標用 `GetValueOrDefault()` 起底。
- **SM 事件可靠性** [[dispatcher-magic-trigger]]：additive 無條件分支只在 vanilla 少/沒密集處理的事件上可靠；密集事件（ActorDialogue/Hello）會輸掉互斥競爭、劫持原版對話——須用 conditions（或走自訂 ScriptEvent keyword）。
- **autoStart scene 閘門**：用 `autoStart.gateGlobal`（controller 端檢查），**不要**用 scene-level `conditions`——controller 強制 `Scene.Start()`，繞過 scene begin-conditions（後者只 gate `beginOnQuestStart` scene）。
- **scene 動作**：`SceneAction.TypeEnum` 只有 Dialog/Package/Timer——「走位/坐/活化」走 Package action 引用 PACK；**「播動畫」走 `SceneActionSpec.Idle`（SceneAdapter phase fragment，非 SceneAction）**。
- **scene PlayIdle**（in-game 2026-06-07 確認，多坑連環）[[scene-playidle-recipe]]：① **SceneAdapter VMAD 三個 canonical 值不可少,否則引擎靜默跳過 fragment**——`ScenePhaseFragment.Unknown=16777216`(0x01000000;=quest 的 `Unknown2=1` 坑的 scene 版)、`SceneScriptFragments.ExtraBindDataVersion=2`、`ScriptEntry.Flags=Local`(全 265 vanilla phase-frag scene 一致)。② **每個帶 fragment 的 phase 必須有一個 SceneAction(Timer)**,空 phase 引擎不 run、fragment 不 fire(故 idle action 同時發一個 Timer 當 hold)。③ **不是每個 IDLE 都能 PlayIdle**:跪/祈禱(`IdleBlessingKneel*`/`IdleCrouchedPray*`)綁神壇家具,自由 `PlayIdle` 無效;挑 vanilla 腳本實際 `.PlayIdle()` 過的(鞠躬 `IdleSilentBow`/獻手 `IdleGive`/`IdleStop`/offset 類),`grep -ri '.PlayIdle(' ~/.cache/modforge/papyrus/Source/Scripts` 查。④ 連播同一 idle 不明顯重播,要不同手勢才看得出兩 fragment 都 fire。⑤ 座椅/sandbox NPC 忽略 PlayIdle → 給站立包(Sandbox `allowSitting:false`)。⑥ console `playidle` 吃 EditorID 不吃 FormID(Papyrus `PlayIdle(form)` 吃 form,spec `idle` ref 綁的就是 form)。
- **NPC 裝備/偷竊**：武器要有傷害（templated 武器 spec 留空會保留 template 原值；0 傷害武器 NPC 評分低於拳頭、不拔）；未裝備物品免 perk 偷，已裝備武器/穿戴衣物需 Misdirection/Perfect Touch perk；`essential` NPC 不可 loot，要可 loot 改用 `protected`。
- **Papyrus 編譯**：`Papyrus.Compile`（Wine+CK）用 cache 全 source（`~/.cache/modforge/papyrus/Source/Scripts`）；native `~/tools/papyrus-compiler` 用 loose Source，headers 不全設 `MODFORGE_PAPYRUS_HEADERS` 指向 cache（`extends ReferenceAlias` 必設）。dispatcher/controller `.psc` embed 進 CLI、Package 編 user script 時解到 temp 當 sibling header → `Fire()` 免 per-machine cache。
- **Voice assets 不是 plugin record**：Skyrim voice 檔必須在 loose path `Sound/Voice/<PluginName.esp>/<VoiceType>/<CK-name>.fuz|wav|xwm`，ESP/ESM 只提供 INFO FormID/Quest/Topic 查找依據；`package` 只會複製 `--assets`/`spec.assets` 的 `Sound/`，不會自動抓另一個 build 目錄旁邊的 voice output。產 voice 的最穩流程是「先 package 到最終資料夾，再對該資料夾內 plugin 跑 `voicelines`」，或「build+voicelines 到 staging dir → package --assets staging dir」。
- **Wine 工具吃 Windows path**：`xWMAEncode.exe` / `LipGenerator.exe` / `FaceFXWrapper.exe` 在 Wine 下要 `Z:\...` 路徑；C# shell-out 前用 `winepath -w`（`LipGenerator` 的 `<wav>` 與 `-OutputFileName:<lip>` 兩個路徑都要轉）。直接傳 Unix `/tmp/...wav` 會讓 xWMAEncode 報「Must specify input and output filenames」並導致 voice pipeline 降級成 loose `.wav`。
- **adapter 合併**：`WireQuestStages` 要**合併**進既有 `QuestAdapter`（不能 `=` 覆寫，否則清掉 alias 腳本的 `.Aliases`）；`GetOwningQuest()` 在執行時 alias OnActivate 可用，dialogue TIF 在 game-load 是 None。
- **vanilla nif 路徑必驗證** [[vanilla-nif-paths-must-be-verified]]：假路徑 → 隱形物件（無報錯）。
- **override vanilla WRLD（Tamriel）** [[worldspace-override-must-carry-topcell]] [[worldspace-override-map-render-fields]]：override 整筆取代記憶體中的 WRLD（last-wins，缺欄位用引擎預設、非繼承），故 `CopyWorldspaceEnv` 要忠實帶。**地圖渲染三欄位**：EDID（地形貼圖 atlas 路徑用 `Textures\Terrain\<EDID>\`，缺→**白圖但有高度**）+ RNAM（×8455 LOD 大物件 `(FormID,世界座標)` 可移植，缺→**破圖**）+ TNAM/UNAM。**永不帶 OFST**（11400 個 uint32 是 Skyrim.esm 絕對檔案偏移量，跨檔=引擎 seek 垃圾→破圖；SSE runtime 自重建，省略安全）。除錯法：byte-parse vanilla vs 輸出 WRLD 逐 subrecord diff。**陷阱**：多欄位同一 commit 加會搞混誰造成什麼，靠 `git show` 確認該 build 實含哪些欄位，別信前一 session 的文字描述。
- **存檔已固化**：GLOB value / scene `.seq` 只是初值，既有存檔保留 runtime 值。
- **worktree 並行** [[feature-swarm-branches]]：worktree 一律從 **stale base** 分出（持續性 harness 行為）；先離線解碼 vanilla 再下精確施工單（agent 不負責猜）、分配互斥檔案領域；整合用 cherry-pick + keep-both（同名 test class 用 `--ours` 重貼）。

### 之後可做

**解碼／計畫／可行性參考檔（2026-06-13，真 mod 解碼 + 對照 ModForge 可實現性；全 esp-only 記憶體安全）**：
- **盤點**：`docs/mod-survey-2026-06-13.md`（下載 mod 的 Tier 1/2/3 解碼價值；解碼方法的記憶體鐵律；本清單的母索引）
- 隨從擴充：`docs/sofia-expansion/`（專案夾，`README.md` 為索引）— `follower-decode-2026-06-13.md`（結構+內容索引）、`expansion-plan-2026-06-13.md`（11✅/3🟡/2🔴）、`sofia-personality.md`（性格分析/寫作 brief）
- NPC 日程：`docs/ai-overhaul-decode-2026-06-13.md`、`docs/ai-overhaul-expansion-plan-2026-06-13.md`（6✅/3🟡/3🔴）
- VIGILANT：`docs/vigilant-{worldspace,story,magic,scene-dialogue-audit}-decode-2026-06-13.md`（11 自訂 worldspace / 120 quest 78 scene / 712 spell 550 MGEF；scene/對話 vs ModForge **~70% 覆蓋**）。**對話缺口已大批補齊（皆 offline）**：✅ `GetIsAliasRef` CTDA（#1 缺口,702 用,`ConditionSpec.Alias`→owning quest alias index,各 quest-scoped 呼叫點傳 `aliasIndexByName`）、✅ 9 個 CTDA 函式（GetQuestRunning/GetInCell/GetInWorldspace/GetEquipped/GetDeadCount/GetSitting/GetGold/GetMapMarkerVisible + 雙參數 GetStageDone 用 `ConditionSpec.Stage`）、✅ INFO(ENAM) 旗標 sayOnce/walkAway/random/invisibleContinue/forceSubtitle（`DialogueInfoFlags` helper）。**已再補**：✅ **對話樹**（INFO `LinkTo`/ENAM + `PreviousDialog`/PNAM + `topLevel:false` sub-topic）。✅ **`IsSceneActionComplete` CTDA**（解鎖 `CompletionConditions`；`ConditionSpec.Scene`(預設 owning scene)+`SceneActionIndex`，scene-cond 呼叫點傳 owning-scene FormKey；**in-game 確認 2026-06-13**：`sceneActionIndex` 1-based，phase 1→2 推進正常）。✅ `Puzzled` emotion（第 8 種，enum 早有、只是訊息/schema 漏列，已補）。**仍開**：任意 scene-phase·OnBegin·OnEnd fragment（結構性 + 需實機，延後；scene Dialog emotion 早已做＝phase Emotion→INFO response）/DialogBranch 顯式分組/WalkAwayTopic·Speaker override（低頻）
- 工作流可行性：`docs/blender-layout-feasibility-2026-06-13.md`（Blender 擺設→`placements[]` JSON,可行、不需新功能、#1 風險=旋轉轉換校準、選配 `staticmap` 子指令）
- 待解碼候選：RDO（dialogue/INFO）、Moons And Stars（weather/IMGS/climate→region）

**待補清單（上述解碼浮現,按優先序；已完成的 npcPatches / MGEF script+DualAV / FLST 見上「已落地」）**：
1. **scene Dialog action 的 `Emotion`/`EmotionValue`** + 泛化 scene phase fragment（不只 PlayIdle 跑 SetStage 等）：VIGILANT 演出靠 headtrack+emotion 取代 CAMS（78 cutscene、0 CAMS → CAMS 可延後）。
2. **worldspace LAND 高度圖**（自訂地圖地形,VIGILANT realm 的本體）、region-driven weather（REGN）—— 待先確認 ModForge worldspace builder 現況。
- Scene 演出續做：PlayIdle / 手勢動畫；camera shot（VIGILANT 證明可延後）。
- 多解 SM 事件（SkillIncrease/Jail/Bribe…，須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- 新 record：Imagespace / Word of Power 等。（Music + Hazard 已落地，見上。）
