# wait_user — 等待使用者的事

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

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
  - **【動態生怪 F組 #3 + 地點感知遭遇 A組 #6】合併 demo 已打包，剩 runtime 驗 — 待主力機（2026-06-17 進展）**：兩 controller `.pex` **已在主力機編好**（`MFDynamicSpawn.pex` 2420B / `MFEncounterCooldown.pex` 1782B，headers＝`~/.cache/modforge/papyrus/Source/Scripts`），example 三個 FormID **已對 Skyrim.esm 查證修正**（LocTypeBanditCamp `0x0130DF`、LocTypeDungeon `0x0130DB`、spawn 改用 leveled list `LCharBanditMeleeAny 0x03DECD`＝每次 roll 隨機近戰 bandit）。**已交付 `~/skyrim_mods/mine/ModForgeLocationEncounter.zip`（FLAT，esp 在根 + `Scripts/` 兩 .pex）**。結構驗過：CLOC 事件 + `MFBanditAmbush_LastFired` GLOB + SMQuestNode + scripts 都接上（locationFilter CTDA 掛 SM node）。
    **2026-06-17 實機回報「startquest 也生不出怪、quest 立刻 stopped」→ 已抓到 root cause 並修復**：兩 controller 原本靠 `OnInit` 觸發，但 Papyrus `OnInit` 整個 quest 生命週期只跑一次、SM relaunch 不重觸發 → 完全不 fire。改由生成的 `<quest>_Stages` **startUpStage fragment** 每次 start 呼叫 `TryFire()`（冷卻）+ `SpawnNow()`（生怪）。已重編 3 .pex（含新 `MFBanditAmbush_Stages.pex`）、重新交付 `~/skyrim_mods/mine/ModForgeLocationEncounter.zip`（FLAT，含 stage fragment）。commit `1afc857`，683 測綠。
    **2026-06-17 續：spawn pipeline 已實機完全確認 ✅**（用隔離測試 `examples/spawn_isolation_test.json`＝StartGameEnabled、無 SM/alias 的純 spawn quest）：載入存檔即生 3 隻活盜賊。debug 出 3 層真因＋placement，見 memory `[[dynamic-spawn-debugging]]`：OnInit 只一次→改 startUpStage fragment；VMAD QuestScriptFragment 綁定要含 startUpStage；MoveTo z=0 埋怪→改 +128 Z 落體；距離 1200-3500 太遠→改 400-900。commit `e3e28d3`。`MFSpawnTest.zip` 隔離版實機 OK。
    ⚠️ **唯一還沒過的一關：SM ChangeLocation 事件實際觸發**。`ModForgeLocationEncounter.zip`（真 SM encounter）實機**仍不生怪**，且：
      - **謎團**：`startquest MFBanditAmbush` 連 console 都不生、`sqv`=stopped、`getstage`=0 —— 即 SM quest 一 startquest 就 stop（StartGameEnabled 的隔離 quest 卻好好的）。已排除：alias optional flag 確實有套（esp FNAM=0x02=Optional）、spawn/fragment/cooldown 全好。
      - **下次方向**：① 查「為何 SM quest（StartGameEnabled 清掉）console startquest 起不來/立刻 stop」——可能 console startquest 對 SM quest 不跑 startUpStage，或別的 stop 原因；② 做**帶 Debug.Notification 的診斷 encounter 版**（TryFire/SpawnNow 各印一行），讓使用者**真的走進 dungeon**（跨地點邊界、左上角跳地名）看 SM 有沒有 fire→才知是「SM 沒觸發」還是「觸發了但 quest 不跑」；③ 若 SM 沒觸發，比對 vanilla ChangeLocation SM quest（root 0x01320E 下）的 branch/condition 結構（GetKeywordDataForCurrentLocation==1 是否正確、branch 要不要 sibling-chain 進 vanilla 既有 branch）。④ 測冷卻語法是 `set MFBanditAmbush_LastFired to 0`（不是 setglobal）。
  - **【radiant 演出 package C組 #2】alias target/location 的 AliasFor* 選擇 + byte 驗證 — 待主力機**：✅ 已離線實作 package target/location 槽的 `alias:<name>`（→ `PackageTargetAlias`〔target〕/`LocationFallback{AliasForReference}`〔location〕）+ `aliasLoc:<name>`（→ `LocationFallback{AliasForLocation}`），對 in-spec `ownerQuest` 解 alias index。**Mutagen shape 反射驗證、8 測綠**，example `examples/radiant_package_spec.json` build/validate 通。⚠️ **離線無法定的點，待你主力機驗**：① 用 xEdit 開一個真實 radiant 演出 package（如 Missives/vanilla radiant 的 PACK，target/location 指向 quest alias 的），對照 ModForge 產出的 **PackageTargetAlias**（`Alias` index）與 **LocationFallback**（`Type`=AliasForReference 還是 AliasForLocation、`Data`=alias index）byte 是否對——特別是「alias 持 ref」vs「location alias」用哪個 LocationType；② example 的 LocType FormID 是 placeholder，換真值、把 package 掛上 alias 填好的 radiant quest、實機看 NPC 有沒有走到/演出到正確 alias 目標。收尾驗證，不擋使用。
  - **【互動式 perk B組 #1】PerkAdapter fragment byte + Activate 簽名 byte 驗證 — 待主力機**：✅ 已離線實作 `addActivateChoice`（[E] 選項 + 可選 spell/`fragmentBody`）+ `setText`（改活化提示），perk fragment dispatcher 生 `<perk>_Frags extends Perk`、PerkAdapter VMAD 綁 `IndexedScriptFragment`。**Mutagen shape 反射驗證、9 測綠**，example `examples/interactive_perk_spec.json` build/validate 通。**record-only 部分（addActivateChoice+spell、setText）離線完全可驗。** ⚠️ **fragment dispatcher 有離線無法定的 byte，待你主力機驗**：① 用 `perkdiag`/xEdit dump 一個真實 **Immersive Interactions**（或任何含 activate-choice 的）perk，對照 ModForge 產出的 **PerkAdapter**（`version`/`objectFormat`、`PerkScriptFragments.ExtraBindDataVersion`、`IndexedScriptFragment` 的 Unknown/Unknown2）與 **AddActivateChoice.Flags**（RunImmediately bit + FragmentIndex）byte 是否對；② 確認 Activate 型 perk fragment 的**函數簽名**（我假設 `Fragment_N(ObjectReference akTargetRef, Actor akActor)`）；③ example 的 keyword/spell FormID 是 placeholder，用 `gamedata find` 換真值，給玩家 AddPerk 後實機按 [E] 看選項出現/fragment 跑。收尾驗證，不擋使用。
  - **【radiant quest alias #7/#8】LocationAlias fill 的 CK 語義 byte 驗證 — 待主力機**：✅ 已離線實作兩個 radiant alias fill（roadmap A組 #7/#8，見 [mod-survey-gaps.md](workflows/roadmap/mod-survey-gaps.md)）：`findMatchingLocation:<locType>[@<parentAlias>]`（#7，建 Location 型 alias）+ `findInLocationAlias:<locAlias>[#<LCRT>]`（#8，在地點內找 ref）。**Mutagen binary shape 已反射驗證**（`QuestAlias.Location=LocationAliasReference{AliasID,Keyword,RefType}`；ALNA 排除＝離線驗只 LinkedRefChild），**10 測綠**，example `examples/radiant_alias_spec.json` build/validate 通。⚠️ **離線無法定的兩點，待你主力機驗**：① **CK 語義**——用 xEdit 開一個真實 **Missives**（或任何 radiant bounty）quest 的 alias，對照 ModForge 產出的 `Location` 子記錄欄位是否擺對位置/語義（特別是「在 parent location alias 內 narrow」靠 `Location.AliasID` 還是別的旗標）；② **真實 FormID**——example 的 LocType keyword（Hold/Dungeon）與 BossContainer LCRT 都是 **placeholder**，用 `gamedata find Skyrim.esm <名稱> Keyword` / `... LocationReferenceType` 查真值換上，再 build 進帶 worldspace 的 spec、實機 `sqv <quest>` 看 alias 有沒有填上。收尾驗證，不擋使用。
  - **【instanceGlobals A組 #9】per-instance 計數 objective runtime 驗證 — 待主力機**：✅ 已離線實作 `StageSpec.instanceGlobals[]`（`{global, randomMin/Max?, value?}`）→ `<quest>_Stages` stage fragment 生 `<g>.SetValue(Utility.RandomInt(min,max)|值)` + `UpdateCurrentInstanceGlobal(<g>)`，把 GLOB 綁到 quest instance 讓 `<Global=X>` objective 文字顯示 per-instance 計數（Missives 同模板多開不同數量）。VMAD GLOB property 綁定離線測過（11 測綠，用 fake .pex），example `examples/gather_quest_spec.json`。⚠️ **待你做（主力機）**：① **編 .pex**——這是 per-quest 的 `<quest>_Stages` fragment（隨任何含 fragment 的 quest 一起編，非新 controller，走既有 quest-build 路徑，故沒列「編 MF*.pex」那類）；② **runtime 驗 per-instance 計數**——build 一個 radiant gather quest（stage 帶 `instanceGlobals` + objective 文字含 `<Global=X>`），多開幾個 instance，確認各 instance 的 objective 顯示**不同**隨機數（`SetValue`+`UpdateCurrentInstanceGlobal` 把 GLOB 綁到「當前 instance」這點離線無法跑驗——若綁錯會所有 instance 共享同一數字）。收尾驗證，不擋使用。
  - **【model-converter MVP】nif→glTF 載體已自寫，剩對真實 vanilla 檔驗證 — 待主力機**：
    ✅ 已**離線自寫** Python 載體 `nif2gltf`（[sub_projs/model-converter/](sub_projs/model-converter/README.md)）：Skyrim NIF 靜態 mesh→glTF，**LE**（NiTriShape/Strips+Data，全 float）+**SSE**（BSTriShape，BSVertexDesc offset 表解碼）、NiNode transform、Z-up→Y-up、含 skin→exit 3、batch manifest，**23 測綠**。不再依賴 NifSkope（原「測有沒有 CLI」那關已用自寫繞過）。⚠️ 勿用 `amPerl/nif`、`SkyMeshGLTF`（幻覺）。**待你做（主力機，有遊戲素材）**：① 解出幾個真實 vanilla `.nif`（LE BSA 一個、SSE BSA 一個，例如某 rock/clutter static），跑 `python -m nif2gltf --in X.nif --out X.gltf --flat`；② 把 `.gltf` 拖進 Blender 或 Godot 看形狀對不對（**SSE 半精度 offset 解碼是最需驗的點**——若 SSE 出來變形/錯位/空，回報該 nif 的 BSVertexDesc）；③ LE 若也有就一併驗。離線只證了「reader 讀回它照 nif.xml 編的合成 fixture」，真檔 byte 對齊跨不過去。收尾驗證，不擋使用。
  - **【Idea #19 紋理 + 物件 — Godot 整鏈實機回測中（2026-06-18）】**：✅ **Godot GUI 已實跑**（主力機 `godot-mono`）：Place Mode 擺 19 物件 + Splat Mode 刷紋理，匯出 `placements.json`（`godot4_y_up`）+ `splat_0.png`（97×65=3×2 cell，尺寸=`cells×32+1` ✅）。整鏈 build/package/交付通過（`GodotEditorDemo.zip`，FLAT）。
    **第一次實機（2026-06-18）：地形起伏 OK，但無紋理、無物件 → 抓到 3 個真因並修復**：
      - **紋理**（commit `208f4eb`，已對 vanilla Tamriel cell byte-verify）：① `LAND.Flags` 缺 `Layers`(0x04) bit（只設了 VertexNormalsHeightMap）→ 引擎整個跳過紋理層；② BTXT base 的 LayerNumber 應為 `0xFFFF`(-1) 非 0、ATXT alpha 應 0-indexed（我原本 base=0/alpha=1 錯位）。兩者皆修，695 測綠。新增 `landdiag` 指令做此 byte 比對。
      - **物件**：placements 的 base ref 填了 `0x000D4B52`，反查發現它是個 **REFR（PlacedObject）不是 base 物件** → 引擎無法實例化＝隱形。這是 Godot 裡填錯 base（要填 STAT/TREE 等 base form，如 RockL01 `0x018199`），非 ModForge bug。新增 `find <plugin> 0xFORMID` 反查能力。
    → **待你做（主力機，重測已重交付的 `GodotEditorDemo.zip`）**：MO2 重裝（md5 `6df71d…`，注意 MO2 reinstall 陷阱）→ `coc GodotEditorWorld_Cell_0_0` → ① **紋理**：base 泥土 + 刷過處透出草，看混合方向對不對；② **物件**：西南 cell 該有 19 顆 RockL01 石頭浮在地表（不再隱形）。回報render 對不對。
    ⚠️ 仍未離線驗的尾巴：VTXT 每點 position 的 **row/col 編碼順序**（layer 號 + flags + texture FormID 已對 vanilla 驗；position 序只能靠這次 in-game 看混合方向是否正確來反證）。
  - **【Idea #20 技能樹 Phase 0】JContainers 持久層 — 編 .pex 需 JContainers headers + 實機驗 — 待主力機**：✅ 已離線實作結構化 `persist`/`syncPerks`（巢狀 JFormDB 寫入 + 依 rank AddPerk/RemovePerk），**三 host／key 形態**：對話 TIF fragment、**quest stage fragment**（到達 stage 觸發）、**任意-ref key**（綁 Form property 當 key）。695 測綠、example `examples/npc_skill_persist_spec.json` build/validate 通（含 stage-fragment ref-key demo：startUpStage 用 `MFSkill_Trainer` ref key 初始化 storage）、解 design U5。**待你做（主力機）**：① **JContainers headers 上 Papyrus header path**——生成的 `TIF_*.psc` **與 `MFSkill_Q_Stages.psc`** 呼叫 `JFormDB.solveXxxSetter`/`solveInt`，編譯需把 **JContainers SE 的 `Data/Scripts/Source/*.psc`**（JFormDB.psc 等）放進 `MODFORGE_PAPYRUS_BASE`（或裝了 JContainers 的 Data 一併在 import path）——否則 `package` 編 fragment 會 unresolved；先確認 JContainers headers 在不在編譯環境；② `package examples/npc_skill_persist_spec.json` 編出 `TIF_MFSkill_TrainEndurance.pex` + `MFSkill_Q_Stages.pex`，裝進**含 JContainers SE** 的 MO2；③ 實機：(a) 載入存檔 → 看 startUpStage 是否把 `.initialized` 寫進 trainer 的 storage（任意-ref key）；(b) 對 Skill Trainer NPC 選「Train your Endurance」→ 多選幾次 → 看 Adaptation perk 是否在 stored rank 到 2 後 AddPerk（可用 `JDB.writeToFile` 或 console `getav`/perk 檢查驗 JFormDB 真有寫入、rank 累加）。⚠ runtime 需 **JContainers SE 已安裝**否則 fragment 報錯。收尾驗證，不擋離線續做（剩好感度 gate）。
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
