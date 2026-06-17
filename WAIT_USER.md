# wait_user — 等待使用者的事

← [CLAUDE.md](CLAUDE.md)｜[workflows/INDEX](INDEX.md)

需要**你（justty32）親自做 / 驗證**才能繼續的事，全列這裡——不只遊戲實機，也包含 **bash 指令、環境變數設定、權限測試、Nexus 下載 mod、外部工具實跑**等。我能做結構性驗證 + 打包；跨不過去的那一關記這裡等你。

**只列還沒做的**——做完即移除（不留已完成清單）；功能類確認後濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，歷史看 git log。

> **膨脹就拆**：本檔若因等你做的事太多而過大，就在 repo 頂層新立 **`wait_todo/`** 資料夾，按類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的待你項，② 收**不屬任何工作流**的雜項（bash/env/權限/Nexus…）——後者堆太多時就是拆進 `wait_todo/` 的觸發。

## 各工作流的待你項

屬於某工作流的待你事項連到該工作流（`workflows/<wf>/`）。目前各工作流無此類 open 項目。

## 不屬任何工作流的（堆太多 → 拆進 `wait_todo/`）

- **環境設定**（env var / 權限 / 本機工具安裝）：（無）
- **外部資源**（Nexus 下載 mod / 外部工具實跑）：
  - **【Idea #20】In-world 技能樹 — 機制已逆向，剩原始碼層級確認（U1–U3，待主力機）**：
    in-world 3D 星樹機制已由 mod-survey 逆向完成：引擎＝**Campfire**（`Campfire.esm` 的 Skill System），**Frostfall** 的 Endurance 樹是其消費者活範例；星＝普通 in-world ACTI、準心 `OnActivate` 點 perk、視覺狀態靠 GLOB、走遠 480u 自毀；公開 API `CampUtil.RegisterPerkTree`。全解見 [`sub_projs/mod-survey/findings/campfire.md`](sub_projs/mod-survey/findings/campfire.md)。設計（玩家+NPC 版）見 [sub_projs/inworld-skill-tree/](sub_projs/inworld-skill-tree/design-inworld-jcontainers.md)。
    → **待你做（主力機讀 BSA 原始碼）**：**U1** Campfire 能否不靠營火、對任意 ref/位置 spawn 樹（讀 `CampCampfire.psc` / `_Camp_PlaceableObjectBase`）；**U2** node 的 `required_perk_rank_global` 是否全域單例（決定 session GLOB 橋接成不成立）；**U3** Frostfall 的 perkPoint 消費/gate 邏輯。結論補回 design 檔 §五。
  - **【地點感知遭遇 A組 #6】WITimeout 冷卻腳本 runtime 驗證 — 待主力機**：✅ 已離線實作 `storyEvent.cooldownHours` → 建 `<quest>_LastFired` GLOB + 掛 reusable `MFEncounterCooldown.psc`（extends Quest，OnInit 比 `GetCurrentGameTime - LastFired < cooldown/24` → `Stop()` 中止）。**#5 locationFilter（OR'd `GetKeywordDataForCurrentLocation`）是純 CTDA、離線完全可驗**；#6 的 GLOB/掛載/property 也離線測過（8 測綠）。example `examples/location_encounter_spec.json`。⚠️ **待你做（主力機）**：① **編 .pex**——`dotnet run -- compile assets/papyrus/MFEncounterCooldown.psc assets/papyrus/`（同其他 MF* controller，prebuilt .pex 要主力機 Papyrus compiler 編一次才能 embed/出貨）；② **runtime 驗冷卻邏輯**——build 一個帶 `cooldownHours` 的 ChangeLocation 遭遇 quest，進遊戲反覆進出同類地點，確認 12 小時內不重觸發、過後會（`GetCurrentGameTime` 回傳天數、`OnInit` 在 SM 每次 start 觸發這兩點我離線無法跑驗）。收尾驗證，不擋使用。
  - **【radiant 演出 package C組 #2】alias target/location 的 AliasFor* 選擇 + byte 驗證 — 待主力機**：✅ 已離線實作 package target/location 槽的 `alias:<name>`（→ `PackageTargetAlias`〔target〕/`LocationFallback{AliasForReference}`〔location〕）+ `aliasLoc:<name>`（→ `LocationFallback{AliasForLocation}`），對 in-spec `ownerQuest` 解 alias index。**Mutagen shape 反射驗證、8 測綠**，example `examples/radiant_package_spec.json` build/validate 通。⚠️ **離線無法定的點，待你主力機驗**：① 用 xEdit 開一個真實 radiant 演出 package（如 Missives/vanilla radiant 的 PACK，target/location 指向 quest alias 的），對照 ModForge 產出的 **PackageTargetAlias**（`Alias` index）與 **LocationFallback**（`Type`=AliasForReference 還是 AliasForLocation、`Data`=alias index）byte 是否對——特別是「alias 持 ref」vs「location alias」用哪個 LocationType；② example 的 LocType FormID 是 placeholder，換真值、把 package 掛上 alias 填好的 radiant quest、實機看 NPC 有沒有走到/演出到正確 alias 目標。收尾驗證，不擋使用。
  - **【互動式 perk B組 #1】PerkAdapter fragment byte + Activate 簽名 byte 驗證 — 待主力機**：✅ 已離線實作 `addActivateChoice`（[E] 選項 + 可選 spell/`fragmentBody`）+ `setText`（改活化提示），perk fragment dispatcher 生 `<perk>_Frags extends Perk`、PerkAdapter VMAD 綁 `IndexedScriptFragment`。**Mutagen shape 反射驗證、9 測綠**，example `examples/interactive_perk_spec.json` build/validate 通。**record-only 部分（addActivateChoice+spell、setText）離線完全可驗。** ⚠️ **fragment dispatcher 有離線無法定的 byte，待你主力機驗**：① 用 `perkdiag`/xEdit dump 一個真實 **Immersive Interactions**（或任何含 activate-choice 的）perk，對照 ModForge 產出的 **PerkAdapter**（`version`/`objectFormat`、`PerkScriptFragments.ExtraBindDataVersion`、`IndexedScriptFragment` 的 Unknown/Unknown2）與 **AddActivateChoice.Flags**（RunImmediately bit + FragmentIndex）byte 是否對；② 確認 Activate 型 perk fragment 的**函數簽名**（我假設 `Fragment_N(ObjectReference akTargetRef, Actor akActor)`）；③ example 的 keyword/spell FormID 是 placeholder，用 `gamedata find` 換真值，給玩家 AddPerk 後實機按 [E] 看選項出現/fragment 跑。收尾驗證，不擋使用。
  - **【radiant quest alias #7/#8】LocationAlias fill 的 CK 語義 byte 驗證 — 待主力機**：✅ 已離線實作兩個 radiant alias fill（roadmap A組 #7/#8，見 [mod-survey-gaps.md](workflows/roadmap/mod-survey-gaps.md)）：`findMatchingLocation:<locType>[@<parentAlias>]`（#7，建 Location 型 alias）+ `findInLocationAlias:<locAlias>[#<LCRT>]`（#8，在地點內找 ref）。**Mutagen binary shape 已反射驗證**（`QuestAlias.Location=LocationAliasReference{AliasID,Keyword,RefType}`；ALNA 排除＝離線驗只 LinkedRefChild），**10 測綠**，example `examples/radiant_alias_spec.json` build/validate 通。⚠️ **離線無法定的兩點，待你主力機驗**：① **CK 語義**——用 xEdit 開一個真實 **Missives**（或任何 radiant bounty）quest 的 alias，對照 ModForge 產出的 `Location` 子記錄欄位是否擺對位置/語義（特別是「在 parent location alias 內 narrow」靠 `Location.AliasID` 還是別的旗標）；② **真實 FormID**——example 的 LocType keyword（Hold/Dungeon）與 BossContainer LCRT 都是 **placeholder**，用 `gamedata find Skyrim.esm <名稱> Keyword` / `... LocationReferenceType` 查真值換上，再 build 進帶 worldspace 的 spec、實機 `sqv <quest>` 看 alias 有沒有填上。收尾驗證，不擋使用。
  - **【model-converter MVP】nif→glTF 載體已自寫，剩對真實 vanilla 檔驗證 — 待主力機**：
    ✅ 已**離線自寫** Python 載體 `nif2gltf`（[sub_projs/model-converter/](sub_projs/model-converter/README.md)）：Skyrim NIF 靜態 mesh→glTF，**LE**（NiTriShape/Strips+Data，全 float）+**SSE**（BSTriShape，BSVertexDesc offset 表解碼）、NiNode transform、Z-up→Y-up、含 skin→exit 3、batch manifest，**23 測綠**。不再依賴 NifSkope（原「測有沒有 CLI」那關已用自寫繞過）。⚠️ 勿用 `amPerl/nif`、`SkyMeshGLTF`（幻覺）。**待你做（主力機，有遊戲素材）**：① 解出幾個真實 vanilla `.nif`（LE BSA 一個、SSE BSA 一個，例如某 rock/clutter static），跑 `python -m nif2gltf --in X.nif --out X.gltf --flat`；② 把 `.gltf` 拖進 Blender 或 Godot 看形狀對不對（**SSE 半精度 offset 解碼是最需驗的點**——若 SSE 出來變形/錯位/空，回報該 nif 的 BSVertexDesc）；③ LE 若也有就一併驗。離線只證了「reader 讀回它照 nif.xml 編的合成 fixture」，真檔 byte 對齊跨不過去。收尾驗證，不擋使用。
  - **【Idea #19 紋理】LTEX 地形貼圖 byte 驗證（單層 BTXT + 多層 VTXT）— 待主力機**：✅ 後端兩段都已離線實作+測：**單層** `worldspace.baseTexture` → 每格四象限 BTXT base 層（`WorldspaceBaseTextureTests`）；**多層混合** `worldspace.textureLayers`（LTEX + grayscale splatmap PNG）→ 每格四象限稀疏 ATXT+VTXT alpha 層（`Splatmap.cs`+`Vtxt.cs`+`WorldspaceSplatmapTests`，604 全綠）。**待你做（主力機）**：① 先查真實 vanilla LTEX FormID（`gamedata find <Skyrim.esm> <名稱> LandscapeTexture`，例如某 dirt/grass）——⚠️ 我離線**沒驗 FormID 不敢捏**，測試/example 都用 placeholder `0x000C16`/`0x0008C5`，請換成查到的真值；② 填進帶 `cells`/`heightmap` 的 worldspace（單層填 `baseTexture`、多層填 `textureLayers[].texture` + 一張 splatmap PNG）build；③ 用 xEdit 對 vanilla Tamriel cell **逐 byte 驗**：BTXT（quadrant 順序 / LayerNumber / texture FormID）+ **VTXT（每點 position 的 row/col 編碼順序、per-quadrant LayerNumber 打包方式、opacity float）**——這兩點離線無法對 vanilla 比，我已用文件慣例實作（視覺混合方向正確、byte 序待驗）。收尾驗證，不擋使用。
  - **【Idea #19 物件擺放】Godot 前端 placement — 待主力機 GUI 跑一次**：離線已實作 Place Mode + placement 筆 + box proxy + `placements.json` 匯出/匯入（前端輸出欄位/座標換算已離線核對與後端 `GodotPlacements.cs` 一致）。**待你做（主力機開 Godot）**：開 `sub_projs/godot-worldspace-editor/godot/`，切 Place Mode、填 base ref、點地形擺幾個方塊、存 `placements.json` → 餵 ModForge build 看 REFR 進不進得了遊戲。回報 UI 有無報錯 / 方塊吸不吸地表 / 匯出 JSON 格式對不對。
  - **【Idea #19 紋理】Godot 前端 splat-paint 筆刷 — 待主力機 GUI 跑一次**：離線已實作 Splat Mode + 紋理 alpha 筆（多層、Paint/Erase、radius/strength）+ active 層即時上色 + 8-bit 灰階 splatmap PNG 匯出/匯入（`splat_tool.gd`/`splat_ui.gd`/`splatmap_io.gd`；PNG Y-flip/網格約定與 heightmap、後端 `Splatmap.cs` 一致）。**離線無 Godot 無法 parse-check**（GDScript 已逐行人工複查）。**待你做（主力機開 Godot）**：切 Splat Mode、填 LTEX ref、刷地形看顏色有沒有跟著畫、存 `splat_0.png`（console 會印可貼進 spec 的 `textureLayers` 片段）→ 把片段 + PNG 填進帶 `cells`/`heightmap` 的 worldspace spec → ModForge build。回報 UI 有無報錯 / 上色對不對 / PNG 尺寸是否 = `cells×32+1` / build 吃不吃。
  - **Nexus 下載（美化/body/工具，掃完 ~/skyrim_mods 確認缺）**：
    - **CBBE 3BA**（30174）— OBody 必需的 body framework，現有 CBBE 是舊版
    - **OBody NG**（77016）— 每個 NPC 自動隨機 body preset + ORefit 服裝貼合
    - **AutoBody AE**（61321）— OBody 的輕量替代（zero config randomize）
    - **Modpocalypse NPCs**（54422）或 **Nordic Faces**（40658）— 通用 NPC 美化底座擇一
    - **EasyNPC**（52313）— NPC appearance 合併工具（避免暗臉衝突）
- **需你跑的 bash / 指令**：（無）

## 實機測試（in-game，MO2 / Proton）

我**不能跑遊戲**，只能 diag / 逐位元對齊 + 打包；實機驗收靠你（memory `ingame-test-workflow`）。

**怎麼測（通用流程）**
1. **拿 zip**：我把打包好的 zip 放 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根，別有多層；曾因 zip 根殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務鐵律**：對話只在**遊戲 LOAD** 時註冊 → 用全新遊戲或任務啟動後 save+reload（`coc` 不註冊）；既有存檔要 save+reload 才吃 `.seq`；強制天氣 `sw <XX>000800`（XX=load order 槽位 hex，build 會印）；console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK／怪／CTD／空白，附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：手動 patch 進 MO2 mod 夾的檔，從 zip 重裝會復原成 build-time mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

**待測（active）**

- **VNML 法線效果（2026-06-16）— 已自驗修正，下面只剩「想看再看」的選配確認**：axis/編碼/尺度已對 vanilla Tamriel LAND 逐 byte 驗過（修了三個 bug，見 SESSION-LOG），不必硬測。新 zip 已交付 `~/skyrim_mods/mine/HeightmapDemo.zip`（FLAT）。**若你某次順手進遊戲**：進 HeightmapDemo worldspace 走坡面，背光側偏暗、向光偏亮、平順漸層即正常——若看到整片黑塊／詭異反光／上下顛倒陰影再回報（理論上不會）。

- **Sofia × VIGILANT 第一幕（2026-06-14）** — 兩版交付 `~/skyrim_mods/mine/`：`SofiaVigilantAct1.zip`（v1 對話+語音）、`SofiaVigilantAct1v2.zip`（v2 +PlayIdle 動作）。spec＝`examples/sofia_vigilant_act1{,_v2}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act1-警戒者.md`。
  - **✅ v1 核心 pipeline 已實機確認（2026-06-14）**：對話有註冊、觸發點對、語音有播（跑了一小段任務線）。
  - **仍 open（待你續測）**：① **各 beat 完整覆蓋**——把 1-A~1-K 跑滿，看有沒有哪個選項該出現卻沒出現（stage 解碼誤）；② **殺/放分支正確性**（殺女巫=SubQ01 s50 / 放=s230；殺 Carene=GoodEnd s35 / 放=s100——殺了卻跳「放過」台詞＝分支錯）；③ **嘴型**有沒有動（fuz 內嵌 lip，待目視確認）；④ **v2 動作**——換裝 v2（一次只裝一版，editorId 不同），看 1-A 諷刺鼓掌 / 1-E 嘆氣 / 1-H-殺 怒 / 1-I 東張西望 有沒有播。
  - gate 解碼地圖見 `sub_projs/sofia-patch/vigilant-screenplay/_act1-trigger-placement-map.md`（BSA QF_ 碎片逆向，高信心）。
  - **後續（非待測，待方向確認後我做）**：夢境/更多動作機制位置已定（夢 cell 0x00185C、stage25 進）未實作。

- **Sofia × VIGILANT 第二/三/四幕（2026-06-14）** — 交付 `~/skyrim_mods/mine/SofiaVigilantAct{2,3,4}.zip`（FLAT，語音齊 + setGlobal pex 齊；Act2=34 fuz/11 pex、Act3=51 fuz/14 pex、Act4=16 fuz/13 pex）。spec＝`examples/sofia_vigilant_act{2,3,4}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act{2,3,4}-*.md`，gate 解碼＝同夾 `_act{2,3,4}-trigger-placement-map.md`。
  - **與 Act 1 唯一差別：沒嘴型**（這批跳過 lip 避免 LipGenerator wine crash 拖死；對話/語音正常，只是嘴不動）。方向確認後可統一補 lip 重打包。
  - 測法同 Act 1（裝在 SofiaFollower+Vigilant 後、save+reload 吃 .seq、跑對應幕的任務、到 beat 對 Sofia 按對話鍵）。回報哪些選項沒出現 / 分支對不對 / 語音正常否。
  - gate 重點：Act2 空牢 0x038524 / 沉船 0x038525 / 血祭母 0x038526；Act3 Child of Oblivion 0x065932；Act4 多數記憶靜默、僅 MeQ01/02/07/Pelinal MeQ10/Molag Bal/Karma 結局有評論。
