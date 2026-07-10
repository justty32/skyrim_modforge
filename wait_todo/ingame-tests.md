# wait_todo — 實機測試（in-game，MO2 / Proton）

← [WAIT_USER](../WAIT_USER.md)（總入口）

我**不能跑遊戲**，只能 diag / 逐位元對齊 + 打包；實機驗收靠你（memory `ingame-test-workflow`）。

**怎麼測（通用流程）**
1. **拿 zip**：我把打包好的 zip 放 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根，別有多層；曾因 zip 根殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務鐵律**：對話只在**遊戲 LOAD** 時註冊 → 用全新遊戲或任務啟動後 save+reload（`coc` 不註冊）；既有存檔要 save+reload 才吃 `.seq`；強制天氣 `sw <XX>000800`（XX=load order 槽位 hex，build 會印）；console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK／怪／CTD／空白，附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：手動 patch 進 MO2 mod 夾的檔，從 zip 重裝會復原成 build-time mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

## 待測（active）

- **darksouls-port P1「空殼院」全量擺放（2026-07-06 已交付 `~/skyrim_mods/mine/DSPortP1.zip`，94MB FLAT）**：38 塊 map piece 渲染 NIF + **全 47 碰撞件**（4893 hulls 切成 116 個 ≤57-hull 載體 NIF，P0 實機確認的規模）+ 210 貼圖 + 自有 `DSPortWorld`（SmallWorld、平 LAND 保底 z=4000 沉在院子下方 16k units）。**ESL=false**（LAND 鐵律）、除 Skyrim.esm 無其他 master。
  - **進場**：`cow DSPortWorld 0 0`（會落在保底 LAND 上，抬頭應可見懸空的不死院）→ `player.setpos z 19935`（升到起始牢房地板；MSB 玩家出生點正對 cell (0,0) 中心）。
  - **驗收三段**：① 42 塊拼起來的院子**整體成形**（P0 那面牆 m0046 周圍應接上鄰塊、出現房間/中庭/迴廊）；② 貼圖大致對（個別多層混合材質仍只取第一層，屬 P2 已知）；③ **地板站得住**——起始牢房、中庭、樓梯試走；碰撞這次是全量（不是只有地板大件）。
  - 已知排除（P2 再說）：m9000/m9100（±1.5–2.9km 天幕遠山）、m5201（±550m 遠景地形）、m9999（黑幕 occluder）→ 地平線只有天空與平地，正常。
  - 回報：哪裡缺塊/破洞/穿地板/黑面/CTD，CrashLoggerSSE log 最好。

- **living-adventurers 整鏈 P0–P3（2026-06-27）— 全離線建構 + 848 測綠，但 .pex 從未編譯、從未實機**（idea #23 / `sub_projs/living-adventurers/`）。這是「抽象幽靈模擬 + 就地實體化 + 傳唱 + 互動/favor + alignment」的 **runtime 第一次驗證**，是 P0–P3 共同的 acceptance gate。測 `examples/living_npcs_spec.json`（macro 版，涵蓋全部；spike/p1 是過程原型，不必另測）。

  **打包（主力機，需 Papyrus toolchain）**：
  1. `scripts/bootstrap-pex.sh` —— 把 `assets/papyrus/*.psc` 全編成 `.pex`（含新的 `MFLivingWorldController` / `MFLivingNpcAlias`；它們是 conditional EmbeddedResource，沒 .pex 就不嵌入、runtime 缺腳本）。需 `MODFORGE_PAPYRUS_HEADERS`（native）或 Wine+CK。
  2. `scripts/ship.sh examples/living_npcs_spec.json` —— build + package + FLAT zip → `~/skyrim_mods/mine/MFLivingNpcs.zip`。
  3. **TIF 陷阱**（memory + dev-env）：互動 `setGlobal` 會生 `TIF_*` result fragment，package 內聯自動編譯**可能 spurious fail（zip 出 0 個 TIF .pex）**→ 互動點了 favor 不加。`unzip -l ~/skyrim_mods/mine/MFLivingNpcs.zip | grep TIF`；若缺，逐一 `dotnet run --project src/ModForge.Cli -- compile <stage>/Scripts/Source/TIF_*.psc <stage>/Scripts` 再 `zip` 補進去。

  **實機驗（新遊戲或 save+reload — StartGameEnabled quest + 對話 + `.seq`）**：
  - **① 離場推進**：`getglobalvalue MFLiving_MFLN_Kjeld_Deeds` → 過幾遊戲時（可調 timescale）再看，**你不在 Kjeld 身邊時它也會爬**；每 tick 跳通知「Kjeld the Wanderer completed another contract.」（Falas＝「pores over a tome…」）。
  - **② 就地實體化**：`coc RiverwoodSleepingGiantInn` ↔ `coc WhiterunBanneredMare`（中間等 tick 讓 anchor 輪替）→ Kjeld 出現在「他當前 anchor」那間旅館；離開再回 → 重新現身且**不重複**。Falas 只在 Bannered Mare。
  - **③ 傳唱**：Sleeping Giant Inn 找 Bjorn 對話 → Kjeld deeds≥1 後出現「Any word of Kjeld the Wanderer?」→ 傳唱台詞（`set MFLiving_MFLN_Kjeld_Deeds to 3` 可強逼）。
  - **④ 互動 + favor（P3）**：Kjeld 現身時對話 → 「Here's some coin…」(fund) / deeds≥1 後「Your deeds are the talk…」(praise) → 點完 `getglobalvalue MFLiving_MFLN_Kjeld_Favor` 應 +1。Falas（neutral）給 parley「Lower your weapon. Let's talk.」。
  - **回報**：四層各 OK／怪／空白／CTD；通知有沒有跳；favor/deed 有沒有動。**這是架構成不成立的第一個經驗證據**——哪層不動回報現象我來定位（最可能的雷：alias 沒填到 ref、MoveTo 後沒 EvaluatePackage、TIF 沒編進 zip）。

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

- **遊戲內場景匯出 · blacksmith 場景（Idea #24 §D，2026-07-08，多輪修正）** — 記錄全驗（dump）；**實機複驗**。`~/skyrim_mods/mine/ModForgeSceneBlacksmith.zip`（**現含 1 個 TIF pex**——openBarter 片段，非純 record 了）。
  - **修正累積**：座標搬白漫馬廄凍原 Z=−4590 + **總共南移 2200** 避開馬廄鑲嵌；鐵匠改新 in-spec NPC **Brynja the Smith**（vanilla unique 不能複製）；交易改 **openBarter「Let me see your wares.」topic**（原本靠 vanilla services faction 不會浮現——沒有通用自動交易對話）。
  - **驗**：地圖 **Forgewatch** marker（白漫馬廄東南更遠）→ 快旅（不摔死）→ 房子 + **Brynja** + 篝火 + 商店。對 Brynja 講話 → 問候「Need something forged?」→ 選單有 **「Let me see your wares.」** → 開交易（鐵匠貨 + 500 金）。
  - **移除物件(§E 橡皮擦)demo**:例子加了 `removals:[0x0D1991]`——白漫馬廄 Skulvar 的一把鋤頭應**消失**(去馬廄看那把鋤頭沒了=橡皮擦成立)。
  - **⚠ 白天測**:vendor 8-20 營業(GetOffersServicesNow 含時間),夜間交易會空——快旅後若是夜晚,`set timescale`/等到白天再試。庫存已放 vanilla 鐵匠 leveled lists(武防+雜貨+金),VendorLocation 錨在店周圍 4096。
  - **回報**:傳送安全否、房子貼地否、Brynja 在否、問候+**交易(有貨有金)**通否。(Brynja 從零建、無 facegen,臉可能陽春/暗臉——能站能講能交易就算過。)

## P1 統一 marker 系統 — ✅ MVP 驗收全過（2026-07-10 IN-GAME，含目視山羊）

四步全過（明細移至 [landed/world.md](../workflows/feature-dev/landed/world.md)）。**殘項（open）**：

- **F11 aimed 放置**：新 DLL 已部署——F11 現在放在**準星指的位置**（射線，無命中 fallback 腳下）。0x57 未實機按過；**pitch 符號未驗**——若 marker 落在身後/天上，回報，翻個符號就好。
- **讀檔後 prune 複核**：讀檔 → F1 Markers 頁不該再列出死 proxy 的鬼條目。
- **adopt 複核**：存檔重載後面板按 `adopt this cell` → 上一 session 放的 marker 應**連名字**回到列表（顯示名活在存檔裡）。

## P2 橡皮擦（2026-07-10 離線齊備，新 DLL 已部署）

1. **擦**：準星指一張 vanilla 椅子按 **F8** → 椅子消失，log `Eraser: marked Skyrim.esm:0x... for removal`。
2. **面板**：F1 → `Eraser`：列表有那筆；`undo` → 椅子回來；再擦掉。指 mod 的物件擦一個 → 該列橘色警告「patch will depend on X.esp」。
3. **真刪除**：F11 放個 marker 再對它按 F8 → marker 連登記簿一起消失（Markers 頁也不見）。
4. **adopt**：擦幾個 → 存檔 → 重載（登記簿死）→ `scan disabled refs in this cell` → 候選列出、逐筆 adopt → 回到清單。
5. **匯出**：F10 → json 有 `removals` 段 → 交給 agent build → 載入 patch 後那張椅子該永久消失。

## P2 滴管／擺放（2026-07-10 離線齊備，新 DLL 已部署）

1. **吸**：準星指任何東西（桌椅、mod 的雕像、NPC）按 **F6** → log `Palette: picked`，F1 → `Palette` 頁出現插槽（含姿態與 scale）。
2. **擺**：看著想放的位置按 **F7** → 該物在準星處出現、帶著吸取時的旋轉/縮放。
3. **匯出**：F10 → 擺的東西進 `placements[]`；若是 actor，該筆應自動帶 `"kind": "npc"`（源頭修掉山羊陷阱——json 裡直接可見）。
4. **串橡皮擦**：F7 擺錯 → F8 對它按 → 無痕消失（真刪除語意）。

## P2 numpad 編輯模式（2026-07-10 離線齊備，新 DLL 已部署）

1. F7 擺個東西 → 準星指著它按 **numpad 5** → log 進編輯模式，F1 → `Editor` 頁顯示 live transform。
2. numpad 8/2/4/6/1/3 推動、7/9 轉、+/− 縮放——**若哪個鍵沒反應**，編輯模式會把未映射的 scancode 印進 log（numpad DIK 常數未驗），亂按一輪把 log 給我即可。
3. **numpad 0** commit；再選、按 **numpad .** → 應還原到選中時的 transform。
4. 對 vanilla 物件按 numpad 5 → 應被拒絕並 log「awaits the overrides[] contract」。
5. 移動後 F10 匯出 → placements 座標應是**移動後**的（live pose）。
6. **物理凍結**：F7 擺一個會滾的東西（書/杯子類）→ numpad 5 選中（log `physics frozen`）→ 推到半空 → 不掉；numpad 0 commit（log `physics restored`）→ 掉下來沉降。
