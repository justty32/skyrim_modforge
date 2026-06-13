# ModForge — Session Log / 進度日誌

**當前進度 + 每個 session 做了什麼**都記在這裡（newest first）。

分工：
- **CLAUDE.md** 只放 durable 的東西——專案慣例、`已落地功能` 目錄、`鐵律與踩坑`、`之後可做` roadmap。
- **本檔**放即時進度：in-flight 狀態、in-game 待確認、這個 session 改了什麼。**不要把 session 進度寫進 CLAUDE.md。**
- 功能真正「落地」後，把濃縮的一句話 + 實作細節指標移進 CLAUDE.md `已落地功能`；in-flight 的過程細節留在這裡。

想法備忘錄另見 `docs/IDEAS.md`。

---

## 進行中 / in-flight（跨 session 的活狀態，就地更新）

> **待實機測試的項目 + 測試步驟 → `docs/INGAME-TEST-QUEUE.md`**（不要寫進本檔或 CLAUDE.md）。

**任務標記（quest-markers，2026-06-13）**:三件套 — **A** `objectives[].targets[]`→QSTA 羅盤/地圖箭頭、**B** `placements[].kind:"xmarker"/"xmarkerHeading"` 隱形錨點、**C** `mapMarkers[]`（XMRK）。CODE_MAP/SPEC/schema/tests 已同步。A 已 in-game 確認；**B/C 大地圖修復第三次 in-game 待確認** → 步驟與 zip 見 `docs/INGAME-TEST-QUEUE.md`，根因見 memory `worldspace-override-must-carry-topcell`。

**身份系統 Phase-2/C**:① Adventurer 預設身份自動授予 ✅、② `activeWhen` 情境條件 ✅、④ controller 主身份+手動覆寫 ✅（皆 in-game 確認 2026-06-07）；✅ ⑤ 身份對應互動 **#5a 商人交易 UI + #5b 護衛任務 + #5c 聖騎士 smite 細調（grantPerks）** + **龍裔首吼（autoGrantWhen）**（皆 in-game 確認 2026-06-07，見 CLAUDE.md「已落地」）；尚未做：③ 聲望/行為追蹤。

---

## 下次要做（next session — 2026-06-14 規劃，全部離線/維護向）

四件維護/整理任務（記於 2026-06-13）：

1. **整理本資料夾的 Claude memory**（`~/.claude/projects/-home-lorkhan-repo-ModForge/memory/`）— 通讀所有 memory 檔，去重、刪除過時/錯誤的、修 `[[links]]`、重整 `MEMORY.md` index。**已知過時**：`ingame-test-todos`（寫「no open items」但 quest-markers map-fix 待測——改指向 `docs/INGAME-TEST-QUEUE.md`）。順手確認新加的 `headless-vanilla-strings-provision`、`working-style`（剛改）連結正確。

2. **拆檔與重構**（behavior-preserving，**一次只動一個面向**，見 CLAUDE.md Workflow 2）— **`src/ModForge.Cli/Program.Build.Voice.cs` 已 325 行、超過 300 上限**，優先拆。其餘接近上限的：`Generator.Build.Scene.cs`(283)、`Diagnostics.Records.cs`(280)、`Generator.Build.Identity.cs`(275)、`Package.cs`(272)。拆完跑離線測試確認行為不變 → **立即同步 CODE_MAP**（含 Tests 欄）→ commit。

3. **整理並優化現有工作流** — 檢視 CODE_MAP 維護鏈、build/package/test loop、`scripts/test-offline.sh`、新的三檔分離（CLAUDE / SESSION-LOG / INGAME-TEST-QUEUE）是否順手；找重複手動步驟看能否腳本化（例如 fresh-clone 後那六個 `.psc` 編譯、`build`→`voicelines`→`voicediag`→zip 的語音出貨鏈）。

4. **盤整外部工具依賴**（產出一份清單，建議新檔 `docs/TOOLING.md` 或 CLAUDE.md 一節）— 列出所有外部 binary + `MODFORGE_*` env var，每項標「必需/選配 + 缺了會怎樣（降級/warn/skip）+ 本機路徑」。目前 grep 到的 env：`MODFORGE_SKYRIM_DATA`、`MODFORGE_PAPYRUS_{COMPILER,COMPILER_BIN,HEADERS,BASE}`、`MODFORGE_TTS_BIN`、`MODFORGE_XWMAENCODE`、`MODFORGE_LIPGEN`、`MODFORGE_FACEFX`、`MODFORGE_FONIXDATA`、`MODFORGE_DEBUG`（CLAUDE.md 另提 `MODFORGE_FISH_SPEECH_BIN`——盤整時補齊）。外部 binary：Wine + CK PapyrusCompiler / native `~/tools/papyrus-compiler`、F5-TTS venv（python 3.11 + torch cu128）、`xWMAEncode.exe`、`LipGenerator.exe`、`ffmpeg`、`Skyrim - Interface.bsa`（STRINGS 來源）。

---

## Session 紀錄（newest first）

### 2026-06-13

全部離線測試、commit 到 master（尚未 push，領先 origin 約 60 個 commit）。

**VIGILANT 對話缺口大批補上**（皆 offline，508 測試綠）
- `GetIsAliasRef` CTDA — VIGILANT 對話第一名手法（702 用），把台詞綁到 speaker 所填的 quest alias。新 `ConditionSpec.Alias`，從 dialogue/banter/scene/stage/objective 各 quest-scoped 呼叫點傳 `aliasIndexByName`；package/perk 無 quest context → warn-drop。
- 再 9 個 CTDA：`GetQuestRunning/GetInCell/GetInWorldspace/GetEquipped/GetDeadCount/GetSitting/GetGold/GetMapMarkerVisible` + 雙參數 `GetStageDone`（新 `ConditionSpec.Stage`，湊齊三個跨任務進度閘）。
- INFO(ENAM) 旗標：`sayOnce`(VIGILANT 最常用)/`walkAway`/`random`/`invisibleContinue`/`forceSubtitle`（共用 `DialogueInfoFlags` helper）。
- 仍開：`IsSceneActionComplete`(274 用,解鎖 CompletionConditions,需 Scene+ActionIndex,結構性)/任意 scene-phase·OnBegin·OnEnd fragment/INFO LinkTo(ENAM)·PreviousDialog(PNAM) 對話樹/`GetInCurrentLocation`(Mutagen 0.49 無此型別)。

**Sofia 擴充專案夾** `docs/sofia-expansion/` — 性格分析(`sofia-personality.md`,從 esp infodiag primary-source)+ README 索引 + git-mv 舊兩份解碼文進去，所有連結更新無斷鏈。

**`cellrefs` CLI + Sleeping Giant Inn 逆向** — 新 `cellrefs <esp> <0xFORMID>`(`Diagnostics.CellRefs.cs`)記憶體安全 dump 單一 interior cell 的 placed refs 成 CSV。`examples/sleeping_giant_inn.json`(423 placements)逆向小屋可見佈局。關鍵發現:placements rotation 是**degree**(esm 存 radian,`deg=rad·180/π`),`PlacementSpec` 無 scale 欄位。doc `docs/sleeping-giant-inn-reverse-2026-06-13.md`。

**`npcPatches[]` 解封**（end-to-end 驗證 + RequiresSkyrim 測試）— 本地化字串牆破了。根因:Mutagen 解 localized master 的 Name 要讀 load-order(headless throw)。解法 `Generator.BuildContext.Utilities.cs` `ProvisionStrings`:從 `Skyrim - Interface.bsa` lazy 抽 `<master>_english.*` 到 temp `Strings/`（**檔名照 ModKey 大小寫**,Linux case-sensitive——小寫=靜默空白名,踩過的坑),overlay 用 `StringsReadParameters{English,StringsFolderOverride,BsaFolderOverride=BSA-free 夾}` 開（避開讀 load-order 的 archive scan）。輸出 esp non-localized、英文名 inline（玩英文版+翻譯 mod 路線）。`examples/npc_patch.json`、memory `headless-vanilla-strings-provision`。

**FLST builder + GetInCurrentLoc**（roadmap #3，offline，506 測試綠）— ① 新 `formLists[]`（`FormListSpec`，items 任意 record ref，順序保留）`BuildFormLists`/`WireFormLists`。② 新 CTDA `GetInCurrentLoc`（form arg=Location；Mutagen 真名是縮寫 `GetInCurrentLoc`，先前 probe 錯打 `GetInCurrentLocation` 才誤判缺）。③ 釐清 **`GetIsInList` 在 Mutagen 0.49 無對應 ConditionData**——FLST 改走既有 `*OrList` param（GetItemCount/GetEquipped/GetIsVoiceType/GetInWorldspace 都收 FormList）。`FormListTests.cs`。

**MGEF script + DualValueModifier**（roadmap #2，offline）— ① 發現 **Script-archetype MGEF 早已可掛 Papyrus**:通用 `scripts[]` ScriptAttach 反射任何有 writable VMAD 的 record，MGEF 已在 `recordsByEd`，故 `archetype:"Script"` + `scripts[]{targetEditorId:<mgef>}` 直接生效（補測試 + SPEC 說明，無需新欄位）。② 新增 `MagicEffectSpec.SecondActorValue`/`SecondActorValueWeight`（DualValueModifier 第二 AV，78 vanilla MGEF 用）。503 offline 測試綠。
