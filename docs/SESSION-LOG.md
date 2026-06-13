# ModForge — Session Log / 進度日誌

**當前進度 + 每個 session 做了什麼**都記在這裡（newest first）。

分工：
- **CLAUDE.md** 只放 durable 的東西——專案慣例、`已落地功能` 目錄、`鐵律與踩坑`、`之後可做` roadmap。
- **本檔**放即時進度：in-flight 狀態、in-game 待確認、這個 session 改了什麼。**不要把 session 進度寫進 CLAUDE.md。**
- 功能真正「落地」後，把濃縮的一句話 + 實作細節指標移進 CLAUDE.md `已落地功能`；in-flight 的過程細節留在這裡。

想法備忘錄另見 `docs/IDEAS.md`。

---

## 進行中 / in-flight（跨 session 的活狀態，就地更新）

**任務標記（quest-markers，2026-06-13 開發中）**:三件套 — **A** `objectives[].targets[]`→QSTA 羅盤/地圖箭頭（指 alias 填的 NPC/地點）、**B** `placements[].kind:"xmarker"/"xmarkerHeading"` 隱形錨點 helper、**C** 新 top-level `mapMarkers[]`（XMRK 地圖圖示）。CODE_MAP/SPEC/schema/tests 已同步。**in-game 狀態**:A（任務日誌雙目標 + 羅盤箭頭）已確認;B/C 與「放置進 vanilla Tamriel」連動的**大地圖空白 + CTD** 踩了三次坑,根因已定位=**worldspace override 的持久 cell(Tamriel TopCell 0xD74)必須(1)加性帶上、(2)複製其記錄標頭旗標 `MajorRecordFlagsRaw=0x00040400`(CopyCellEnv 只複製 DATA 旗標,漏了標頭→引擎不認得持久 cell→載 actor CTD)、(3)ref 自身帶 0x400 持久旗(vanilla XMarker/地圖標記全有)**。修好後 esp 已逐位元對齊 vanilla/USSEP,**第三次 in-game 待確認**(`~/skyrim_mods/mine/ModForgeQuestMarkers-mapfix.zip`;安全版 = `ModForgeQuestMarkers.zip`)。細節見 memory `worldspace-override-must-carry-topcell`。

**身份系統 Phase-2/C**:① Adventurer 預設身份自動授予 ✅、② `activeWhen` 情境條件 ✅、④ controller 主身份+手動覆寫 ✅（皆 in-game 確認 2026-06-07）；✅ ⑤ 身份對應互動 **#5a 商人交易 UI + #5b 護衛任務 + #5c 聖騎士 smite 細調（grantPerks）** + **龍裔首吼（autoGrantWhen）**（皆 in-game 確認 2026-06-07，見 CLAUDE.md「已落地」）；尚未做：③ 聲望/行為追蹤。

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
