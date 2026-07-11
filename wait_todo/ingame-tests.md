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

- **living-adventurers 整鏈 P0–P3 — ✅ 已打包交付 `~/skyrim_mods/mine/MFLivingNpcs.zip`（2026-07-11，FLAT）**（idea #23 / `sub_projs/living-adventurers/`）。這是「抽象幽靈模擬 + 就地實體化 + 傳唱 + 互動/favor + alignment」的 **runtime 第一次驗證**，是 P0–P3 共同的 acceptance gate。測 `examples/living_npcs_spec.json`（macro 版，涵蓋全部；spike/p1 是過程原型，不必另測）。
  - zip 內容已結構驗證：esp（13 records、quest StartGameEnabled、4 globals、6 topics）＋ `.seq` ＋ 6 pex（`MFLivingWorldController`/`MFLivingNpcAlias`＋**4 個 TIF 全數內聯編譯成功**，這次沒踩 spurious-fail）＋ TIF 源碼。7 scripts attached（quest 1 + alias 2 + TIF 4；dump 不渲染 alias 層 VMAD，數字對帳自 build summary）。
  - 打包途中踩了一雷已修＋記進 dev-env：`MFLivingNpcAlias` 用 SKSE 的 `GetDisplayName` → 純 vanilla header cache 編不過（undefined function）→ SKSE 版 headers（Steam Data/Scripts/Source 64 檔）疊進 cache 後過。

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

## scene-capture-bridge P7 backlog 一輪（2026-07-11 晚，DLL `a46ed0b2` 已部署，esp 不動）

⚠️ 完全關遊戲重開吃新 DLL；co-save 又升版（SETT v3）——舊存檔的設定記錄跳過一次（登記簿的 marker/eraser/overrides 不受影響，marker 仍自動 adopt）。

1. **`sc delc`**：console 點一個物件（滑鼠點畫面中的桶/椅，console 顯示其 ref）→ 打 `sc delc` → 物件消失、Eraser 頁多一列。點 NPC 打 `sc delc` → 應拒絕（印「actor」）。
2. **準星↔射線切換**：`sc del er1` → 刪除模式動作鍵改用射線（可擦樹）；`sc del er0` 切回準星。`sc pk er1`/`sc ed er1` 同理（吸樹/編輯樹不必再按 numpad *）。Settings 頁應顯示各模式現況。
3. **純旋轉子模式**：`sc ed` 選中物件 → **`sc ed ax`** → Editor 頁提示行變 ROTATE → numpad **4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零**（位置不變）；打 **`sc ed`** 退回移動模式（4/6/1/3 回到位移）。
4. **編輯 marker 位置**：`sc ed` 準星對一個匕首 marker → 動作鍵選中（log `editing MARKER`）→ numpad 推移＋**0 commit** → 匕首移到新位置、跳「marker moved」；F1 Markers 頁該筆座標更新、**不**進 Editor overrides 列。numpad 5 可復原、`.` 取消。
5. **palette load / save from file**：`sc pk` 吸幾個 → Palette 頁文字框輸入檔名（如 `my-palette.json`）→ **save to file** → SKSE 夾生出該檔。清空插槽（或換存檔）→ 同檔名 **load from file** → 插槽**追加**回來、排最上。
6. **Export all**：室外站著放幾個物件、走幾格再放 → Export 頁 **Export all (loaded cells)** → json 的 `placements` 應含**多個已載入 cell** 的（單 cell 匯出只會有當前 cell）；統計行 cell 顯示「ALL/N loaded cells」。（未載入 cell 的撈不到＝正常，log 有講。）
7. **co-save v3**：改 er／`sc ed ax`／步長 → 存檔重開 → 設定還原。

## scene-capture-bridge P8（2026-07-11 深夜，DLL `4ba5b9ae`＋新 esp 已部署）

⚠️ 完全關遊戲重開吃新 DLL＋**新 SceneCaptureTools.esp**（marker 模型換鐵匕首）。co-save MKRS 升 v2——舊存檔的 marker angle/scale 讀成 angleZ-only 補 0（不致命）。

1. **marker＝鐵匕首**：`sc mk` 放 → 應出現懸浮**鐵匕首**（劍尖朝玩家面向）、不掉不被踢；準星/E 照樣選得到、開編輯視窗。
2. **記錄朝向＋大小**：`sc ed` 選中匕首 marker → `sc ed ax` 進旋轉模式轉個角度、`+/−` 改大小 → **numpad 0** → Markers 頁該筆更新 → **Export** → `annotations[]` 該筆帶 `rotation{x,y,z}`（非 0）＋`scale`。
3. **numpad 5 per-mode**：編輯**移動模式**下 5＝整個復原到編輯前；`sc ed ax` 進**旋轉模式**下 5＝只把角度歸零（位置/大小不動）。

## scene-capture-bridge P9 擷取器（DLL `d3e1b5d0`，co-save `'SCCP'` v3）

**2026-07-11 端到端全通**：`sc cap` 模式（F11）吸 Mirabelle → Export → 消費 → build → 進遊戲**分身在學院庭院原地出現** ✅（OPEN-A 核心流程、OPEN-C 18-morph 修復、消費端 Phase 1 一次實證；落地記錄 [landed/npcs](../workflows/feature-dev/landed/npcs.md)）。殘餘：

**OPEN-A 殘餘（最後一小項）**：存檔完全重開 → capture 的 aim source（er0/er1）還原（co-save SETT v4）。（`sc cap er1` 射線吸取 2026-07-11 已實證 OK。）另留意：模式制下重複按 F11 會吸出重複列（正常行為，消費端 editorId 已防撞）。

**P10 擷取器補完（DLL crc `923e348b` 已部署雙夾，co-save SCCP 升 v4——舊存檔照讀）**：NPC 擷取新收 **class＋level＋equipped**（worn 護甲＋手持武器/火把）；新指令 **`sc capc`**＝console 點選誰就吸誰（物品與 NPC 都行，不用準星）。消費端同步：class/level 直通＋有 class 開 autoCalcStats；**equipped→inventory（自動穿最好的）且非空時 skip outfit**（治 PROTEUS 裸體）。⚠️ 完全關遊戲重開吃新 DLL。
   - ✅ 重吸＋消費已跑完（2026-07-11 晚）：新 json 帶 class=CombatBarbarian/level=1/equipped=[ClothesMGBoots]，**`~/skyrim_mods/mine/MFCapHatak.zip` 已出貨**（舊 MFCapPrisoner 記得停用，免得兩個分身疊著）。
   - **驗**：① 裝 MFCapHatak → 分身 Hatak 應**至少穿著學院法師靴**、數值照 CombatBarbarian L1 算（不再是 flat 預設）；② **關鍵觀察**：你的活 PROTEUS clone 若明顯穿整套裝備、但吸到的 equipped 只有靴子一件 → PROTEUS 幫 clone 穿衣大概走 runtime outfit 填充、inventory 沒掛 worn 旗標——回報實況，需要的話 DLL 再補一路（讀 outfit 的 runtime instance 或 3D 裝備槽）；③ 順手：console 點一把武器 `sc capc` 應吸進 capturedItems。

 wait_todo — 實機測試（in-game，MO2 / Proton）

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

- **living-adventurers 整鏈 P0–P3 — ✅ 已打包交付 `~/skyrim_mods/mine/MFLivingNpcs.zip`（2026-07-11，FLAT）**（idea #23 / `sub_projs/living-adventurers/`）。這是「抽象幽靈模擬 + 就地實體化 + 傳唱 + 互動/favor + alignment」的 **runtime 第一次驗證**，是 P0–P3 共同的 acceptance gate。測 `examples/living_npcs_spec.json`（macro 版，涵蓋全部；spike/p1 是過程原型，不必另測）。
  - zip 內容已結構驗證：esp（13 records、quest StartGameEnabled、4 globals、6 topics）＋ `.seq` ＋ 6 pex（`MFLivingWorldController`/`MFLivingNpcAlias`＋**4 個 TIF 全數內聯編譯成功**，這次沒踩 spurious-fail）＋ TIF 源碼。7 scripts attached（quest 1 + alias 2 + TIF 4；dump 不渲染 alias 層 VMAD，數字對帳自 build summary）。
  - 打包途中踩了一雷已修＋記進 dev-env：`MFLivingNpcAlias` 用 SKSE 的 `GetDisplayName` → 純 vanilla header cache 編不過（undefined function）→ SKSE 版 headers（Steam Data/Scripts/Source 64 檔）疊進 cache 後過。

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

## scene-capture-bridge P7 backlog 一輪（2026-07-11 晚，DLL `a46ed0b2` 已部署，esp 不動）

⚠️ 完全關遊戲重開吃新 DLL；co-save 又升版（SETT v3）——舊存檔的設定記錄跳過一次（登記簿的 marker/eraser/overrides 不受影響，marker 仍自動 adopt）。

1. **`sc delc`**：console 點一個物件（滑鼠點畫面中的桶/椅，console 顯示其 ref）→ 打 `sc delc` → 物件消失、Eraser 頁多一列。點 NPC 打 `sc delc` → 應拒絕（印「actor」）。
2. **準星↔射線切換**：`sc del er1` → 刪除模式動作鍵改用射線（可擦樹）；`sc del er0` 切回準星。`sc pk er1`/`sc ed er1` 同理（吸樹/編輯樹不必再按 numpad *）。Settings 頁應顯示各模式現況。
3. **純旋轉子模式**：`sc ed` 選中物件 → **`sc ed ax`** → Editor 頁提示行變 ROTATE → numpad **4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零**（位置不變）；打 **`sc ed`** 退回移動模式（4/6/1/3 回到位移）。
4. **編輯 marker 位置**：`sc ed` 準星對一個匕首 marker → 動作鍵選中（log `editing MARKER`）→ numpad 推移＋**0 commit** → 匕首移到新位置、跳「marker moved」；F1 Markers 頁該筆座標更新、**不**進 Editor overrides 列。numpad 5 可復原、`.` 取消。
5. **palette load / save from file**：`sc pk` 吸幾個 → Palette 頁文字框輸入檔名（如 `my-palette.json`）→ **save to file** → SKSE 夾生出該檔。清空插槽（或換存檔）→ 同檔名 **load from file** → 插槽**追加**回來、排最上。
6. **Export all**：室外站著放幾個物件、走幾格再放 → Export 頁 **Export all (loaded cells)** → json 的 `placements` 應含**多個已載入 cell** 的（單 cell 匯出只會有當前 cell）；統計行 cell 顯示「ALL/N loaded cells」。（未載入 cell 的撈不到＝正常，log 有講。）
7. **co-save v3**：改 er／`sc ed ax`／步長 → 存檔重開 → 設定還原。

## scene-capture-bridge P8（2026-07-11 深夜，DLL `4ba5b9ae`＋新 esp 已部署）

⚠️ 完全關遊戲重開吃新 DLL＋**新 SceneCaptureTools.esp**（marker 模型換鐵匕首）。co-save MKRS 升 v2——舊存檔的 marker angle/scale 讀成 angleZ-only 補 0（不致命）。

1. **marker＝鐵匕首**：`sc mk` 放 → 應出現懸浮**鐵匕首**（劍尖朝玩家面向）、不掉不被踢；準星/E 照樣選得到、開編輯視窗。
2. **記錄朝向＋大小**：`sc ed` 選中匕首 marker → `sc ed ax` 進旋轉模式轉個角度、`+/−` 改大小 → **numpad 0** → Markers 頁該筆更新 → **Export** → `annotations[]` 該筆帶 `rotation{x,y,z}`（非 0）＋`scale`。
3. **numpad 5 per-mode**：編輯**移動模式**下 5＝整個復原到編輯前；`sc ed ax` 進**旋轉模式**下 5＝只把角度歸零（位置/大小不動）。

## scene-capture-bridge P9 擷取器（DLL `d3e1b5d0`，co-save `'SCCP'` v3）

**2026-07-11 端到端全通**：`sc cap` 模式（F11）吸 Mirabelle → Export → 消費 → build → 進遊戲**分身在學院庭院原地出現** ✅（OPEN-A 核心流程、OPEN-C 18-morph 修復、消費端 Phase 1 一次實證；落地記錄 [landed/npcs](../workflows/feature-dev/landed/npcs.md)）。殘餘：

**OPEN-A 殘餘（最後一小項）**：存檔完全重開 → capture 的 aim source（er0/er1）還原（co-save SETT v4）。（`sc cap er1` 射線吸取 2026-07-11 已實證 OK。）另留意：模式制下重複按 F11 會吸出重複列（正常行為，消費端 editorId 已防撞）。

**P10 擷取器補完（DLL crc `923e348b` 已部署雙夾，co-save SCCP 升 v4——舊存檔照讀）**：NPC 擷取新收 **class＋level＋equipped**（worn 護甲＋手持武器/火把）；新指令 **`sc capc`**＝console 點選誰就吸誰（物品與 NPC 都行，不用準星）。消費端同步：class/level 直通＋有 class 開 autoCalcStats；**equipped→inventory（自動穿最好的）且非空時 skip outfit**（治 PROTEUS 裸體）。⚠️ 完全關遊戲重開吃新 DLL。
   - ✅ 重吸＋消費已跑完（2026-07-11 晚）：新 json 帶 class=CombatBarbarian/level=1/equipped=[ClothesMGBoots]，**`~/skyrim_mods/mine/MFCapHatak.zip` 已出貨**（舊 MFCapPrisoner 記得停用，免得兩個分身疊著）。
   - **驗**：① 裝 MFCapHatak → 分身 Hatak 應**至少穿著學院法師靴**、數值照 CombatBarbarian L1 算（不再是 flat 預設）；② **關鍵觀察**：你的活 PROTEUS clone 若明顯穿整套裝備、但吸到的 equipped 只有靴子一件 → PROTEUS 幫 clone 穿衣大概走 runtime outfit 填充、inventory 沒掛 worn 旗標——回報實況，需要的話 DLL 再補一路（讀 outfit 的 runtime instance 或 3D 裝備槽）；③ 順手：console 點一把武器 `sc capc` 應吸進 capturedItems。

**PROTEUS clone 玩家分身驗收（`~/skyrim_mods/mine/MFCapPrisoner.zip` 待裝）**：OPEN-B 已結案（PROTEUS 寫 TESNPC ✅，見 [landed/npcs](../workflows/feature-dev/landed/npcs.md)）；分身 zip 已出貨——裝了進遊戲看：**你自己的分身**應站在擷取點（Tamriel (109098, 103520, -9017)，冬堡往學院方向），臉形/髮型/鬍子/髮色/體重＝你的角色。**預期落差（非 bug）**：①戰紋/膚色細節層可能沒有（PROTEUS 沒把 tintLayers 寫回 TESNPC）；②服裝可能不對（defaultOutfit 指向 PROTEUS.esp runtime 模板，esp 檔上是空殼）；③RaceMenu 雕塑/overlay 本來就不在配方層。masters：Skyrim.esm＋PROTEUS.esp＋nwsFollowerFramework.esp。
