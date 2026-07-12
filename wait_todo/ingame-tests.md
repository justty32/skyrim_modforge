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

## scene-capture-bridge P9 擷取器（DLL `d3e1b5d0`，co-save `'SCCP'` v3）

**2026-07-11 端到端全通**：`sc cap` 模式（F11）吸 Mirabelle → Export → 消費 → build → 進遊戲**分身在學院庭院原地出現** ✅（OPEN-A 核心流程、OPEN-C 18-morph 修復、消費端 Phase 1 一次實證；落地記錄 [landed/npcs](../workflows/feature-dev/landed/npcs.md)）。殘餘：

**OPEN-A 殘餘（最後一小項）**：存檔完全重開 → capture 的 aim source（er0/er1）還原（co-save SETT v4）。（`sc cap er1` 射線吸取 2026-07-11 已實證 OK。）另留意：模式制下重複按 F11 會吸出重複列（正常行為，消費端 editorId 已防撞）。

## scene-capture-bridge — `sc capp` 直接吸玩家（2026-07-12，DLL crc `c5049c78`，**已部署**）

> **第一輪實機已過（2026-07-12）**：`sc capp` 抓對玩家 base（`Skyrim.esm:0x000007`），分身臉**確認是本人**——faceMorphs/headParts/hairColor/faceTexture ＋ **`tintLayers` 戰紋**全中（戰紋正是 PROTEUS 路線拿不到的那層）。交付 `~/skyrim_mods/mine/MFCapHatak.zip`。**剩下面的「數值」那條要用練過的角色重驗**。

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動）。co-save **SCCP 維持 v8**（+label +顯式 H/M/S +18 技能）——舊存檔的 captures 照讀（缺的欄位＝0，行為同以前）。

**這是什麼**：`sc capp` 直接讀玩家（外貌走 base TESNPC，進度走 runtime actor）→ **不再需要 PROTEUS clone 當中介**。順帶所有 actor 的擷取都改帶**真實數值**（H/M/S＋18 技能）→ ModForge 寫 DNAM、不再靠 class autocalc 估算（那正是「clone 自報 L1、50/50/50」的來源）。計畫＋落地：[plans/player-capture-capp.md](../workflows/plans/player-capture-capp.md)。

> **2026-07-12 第一輪 export 的「出廠值」是誤報**：`captures_20260712-0958.json` 裡玩家 lvl 1 / 100-100-100 / 種族起始技能，看起來像讀到 base 出廠值——**但那個測試角色（Hatak）本來就是全新 1 級布萊頓**（存檔 header：level 1、XP 0.0/100、0 技能升過）。那些數字是**真值**。同一份 export 的 Ancano（lvl 15 / 167-143-50 / 破壞 51）也對。
> 但「讀 base actor value」在**練過的角色**上確實是錯的（等級/血魔耐/技能的成長存在 **permanent modifier**，base 永遠停在 chargen 起始表）——所以還是改成 **`GetPermanentActorValue`**（base＋永久修正，**不含**藥水/裝備附魔/受傷）。**沒練過的角色兩種讀法結果一樣**，所以下面第 0 步務必先把角色練起來，不然驗不出差別。

0. **⚠️ 先讓角色「有進度」，否則這輪等於沒驗**（Hatak 是 1 級白紙，base 讀法和新讀法會吐出一模一樣的數字）。用**練過的角色**存檔，或 console 現場造一個：
   - `player.advskill destruction 20000`、`player.advskill onehanded 20000`（技能會跳好幾級，並累積升級點數）
   - 升級（背包 → 技能畫面按升級）幾次，**每次選 +10 生命/魔力/耐力**
   - 記下 console 的 **`player.getav health`**（含 buff）、**`player.getbaseav health`**（＝chargen 100）、以及角色實際的等級/技能，等一下比對。
1. **吸自己**：隨便站哪（**室內比較好驗**，之後分身就站那）→ console 打 **`sc capp Hero`**（label 隨你取，大小寫會保留）→ console 應印 `SCB: captured the player as 'Hero'`。
   - **關鍵驗（本輪重點）**：匯出的 json 裡 `level` / `health` / `skills` 應該是**你練出來的數字**——`health` 應該 > 100（100 是 chargen 值，**看到 100 就是又讀到 base 了，回報**），`skills` 的破壞/單手應該是你剛練上去的等級，不是 15。
   - **buff 不該吸進來**：喝一瓶**強化生命/技能**的藥水再 `sc capp` → 吸到的數字應該**跟沒喝一樣**（永久值不含臨時 buff；`player.getav` 會變、我們吸的不該變）。
   - **F1 → Captures 頁**應多一列（kind=npc、你的角色名）。
   - **驗 label 大小寫**：`sc capp MyHero` 之後匯出的 json 裡 `editorId` 應是 `MFCap_MyHero`（**不是** `mfcap_myhero`）——大小寫沒了就是踩到 `Lower()` 坑，回報。
2. **匯出**：**F1 → Export 頁（或 Captures 頁）→ `Export captures`** → SKSE 夾生出 `captures_<時間>.json`。**把這個檔給我**（或直接說檔名，我去讀）。
3. **我 build 出分身 zip 之後（第二輪）**：裝 → 進遊戲到擷取點 → 分身應該是**你自己**：
   - **臉/髮/體型**＝你的角色（RaceMenu 雕塑/overlay 不在配方層，屬已知落差；facegen 未烘焙前臉可能偏灰/暗＝已知）。
   - **數值＝你的真實血/魔/耐＋18 技能**（不是 50/50/50，不是 L1）。
   - **perk ＝你點過的 perk**（玩家 perk 存在 `addedPerks`，這次才收得到）。
   - **裝備**：穿在身上的護甲應該有穿（走鑄 OTFT 路），武器/雜物在物品欄。
   - **可能落差（先照實回報，不算 bug）**：① 玩家 base 的 **voiceType 可能是空的** → 分身啞巴（不會 hello/閒聊）——0958 那份 export 玩家那筆確實**沒有** `voiceType` 欄，已實證；② 物品欄**全吸**（含任務物品/金幣/鑰匙）→ 分身身上東西很多。這兩項要不要處理等你看了再說。
   - **驗 `isPlayer` 標示（2026-07-12 新增，co-save SCCP v9）**：這次匯出的 json 裡玩家那筆應多一個 `"isPlayer": true`（一般 NPC 的 `sc capc` 不該有這欄）。若玩家 voiceType 仍是空的，`build` 的輸出裡應該印一句 warning，類似「is a player capture — no voiceType … the clone will be silent … This is expected, not a bug」（**不是紅色錯誤，是提示**）；有 voiceType 的話則不該印。
   - **`weight: 0.0` 不是 bug**：那是你 chargen 的體重滑桿真值（Skyrim.esm 裡 Player base 是 100，我們吸到 0 → 正好證明讀的是**存檔改寫過的** TESNPC 而不是磁碟原始記錄；Ancano 的 0 也跟 Skyrim.esm 一字不差）。想對照就 console `player.getnpcweight`。
4. **順手複驗（不破舊路）**：對一個**普通 vanilla NPC** `sc capc` → 匯出的那筆現在也該多出 `health/magicka/stamina/skills`，且 build 出來的分身**不再開 autoCalcStats**（數值照抄本尊）。舊 capture json（沒這些欄位的）照舊走 class-autocalc——**不該壞**。

**回報**：① `sc capp` 有沒有吸到、label 大小寫對不對；② 分身的數值/perk/裝備對不對；③ voiceType/物品欄的落差要不要修。

## scene-capture-bridge — 模式開關套件 `py` / `ed` / `pkc`（2026-07-12，DLL crc `5434abd4`，**已部署**）

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動）。co-save：`SETT` 升 **v6**、新 record **`'PLEX'` v1**——**舊存檔照讀**（讀不到就落回預設：place `py1`、edit `py0`、extra data 全關＝跟以前一模一樣）。

**這是什麼**：四個 per-mode 開關 ＋ 一個 aim-free 滴管。核心是 **`sc pl py0`＝「擺好的東西不要被物理彈飛」**，而且**這次真的會 ship 進 esp**（不只是遊戲內凍住）。

**離線已閉環**（不用你驗）：手寫 DLL 形狀 json → build → REFR 帶 `0x20000000`（DontHavokSettle）、鑄造的附魔劍 WEAP+ENCH 都在、placement 的 base 解得到它；C# 932 測綠。**要你驗的是遊戲內那一半。**

1. **`sc pl py0/py1` — 擺放物理（最重要，先驗這個）**
   - `sc pk` 吸一個**雜物**（杯子/盤子/書/武器——havok 會動的那種），`sc pl` 進擺放模式。
   - **先看壞的樣子**：`sc pl py1`（預設）→ 動作鍵（**F11**）在**桌面上**擺一個 → 它會**掉下來/滾走/被你走過去撞飛**。這是現況，正常。
   - **再看好的樣子**：`sc pl py0` → console 應印 `placed objects have physics OFF (py0)` ＋ 一行說明 → 再擺一個 → **它應該定在原地**（不掉、撞不動）。
   - **console `sc` 印出來的 usage** 應該看得到 `sc pl py1 / py0`、`sc ed py0 / py1`、`sc pk ed0 / ed1`、`sc pl ed0 / ed1`、`sc pkc [Label]` 這幾行。**F1 → Settings** 也應該有 `Physics (…)` 與 `Extra data (…)` 兩節，顯示 place/edit/pick/place 的現況。
   - **🔑 真正的驗收在 esp（這是這條的重點）**：把 `Export player cell` 出來的 json 給我 → 我 build → **裝進去看那些杯子還在不在桌上**。（json 裡那幾筆 placement 應帶 `"noHavokSettle": true`。）**遊戲內凍結是活不過存檔的**——會 ship 的是這個記錄旗標。
   - ⚠️ 預期邊界：`py0` **只擋「載入時被 havok settle 彈飛」**，不是把物件變成不可推的石頭——玩家還是可以撿/撞。這是 vanilla 杯子的行為（Bethesda 自己 3791 個 REFR 就是這樣做的）。若你要的是「連撞都不動」，回報，那要另一條路（script keyframed）。

2. **`sc ed py0/py1` — 編輯期物理**
   - `sc ed`（預設 `py0`）→ 選中一個雜物 → numpad 微調 → 物件**不會跟你打架**（現行行為，不該有變化——這條主要是**回歸**：確認我抽共用碼沒把它弄壞）。
   - `sc ed py1` → 再選一個 → 控制期間 **havok 還跑著**（推它時會晃/掉）。切回 `sc ed py0` 應恢復凍結。

3. **`sc pk ed1` / `sc pl ed1` — 實例附魔（extra data）**
   - 準備一把**你自己附魔的武器**（或撿一把**已附魔的 vanilla 武器**）**丟在地上**。
   - **先看壞的樣子**：`sc pk ed0`（預設）→ `sc pk` 吸它 → `sc pl` 擺出來 → 撿起來看：**是白板武器**（附魔沒了）。這就是要修的現況。
   - **好的樣子**：`sc pk ed1` → 再吸一次 → palette 頁該插槽（log 會印 `+extra[weapon ench …]`）→ `sc pl ed1` → 擺出來。
     - **durable 附魔**（撿到的 vanilla 附魔武器）：擺出來那把**應該真的帶附魔**（撿起來看名字/附魔）。
     - **你自己附魔的**（runtime ENCH）：世界裡那把**是白板**（預期！那顆 ENCH 是存檔綁定的 form，不能快取在落盤的 palette 上）——但**匯出照樣鑄造**，見下。
   - **驗收在 json**：`Export player cell` → 該份檔應同時有
     - 一段 **`capturedItems[]`**（`"editorId": "MFPal_<插槽名>_<seq>"`、`"kind": "weapon"`、`"base"` ＝實體模板、`"enchantment"` 帶 base 或 effects），且
     - `placements[]` 裡那筆的 **`"base"` 正是那個 `MFPal_…` editorId**（**不是** `Skyrim.esm:0x...`——看到耐久 id 就是沒吃到 `ed1`，回報）。
   - 把該 json 給我 → build → 裝進去 → **撿起來應該是那把附魔武器**。

4. **`sc pkc [XXX]` — console 選取版滴管**
   - console 點選一個物件（滑鼠點它，console 左上會顯示它）→ `sc pkc` → 該物件進 palette（`SCB: picked console ref into the palette`）。
   - 帶標號：`sc pkc MyChair` → palette 該插槽名字**就是 `MyChair`（大小寫保留**——不是 `mychair`，這是 `sc capp` 踩過的坑）。
   - 也吃 `sc pk ed1`（若開著，`pkc` 一樣會連附魔吸進來）。

5. **跨存檔/重開（co-save `SETT` v6 ＋ `'PLEX'`）**：`sc pl py0` 擺幾個、`sc pl ed1` 擺一把附魔武器 → **存檔 → 完全關遊戲 → 重開 → 讀檔** →
   - Settings 頁的四個開關**還是你設的值**。
   - 走回那個 cell → `Export player cell` → 那幾筆**仍帶 `noHavokSettle`／仍指 `MFPal_…`**（DLL 會按 base＋座標把 dynamic ref 撿回；log 會說 `re-acquired placed ref #N`）。**若掉了**（旗標/附魔沒了）**回報**——我要知道 reacquire 撿不回的頻率。

**回報**：① `sc pl py0` 擺的東西在**遊戲內**定不定得住；② **build 出來裝進遊戲後**，杯子還在不在桌上（這條才是真的驗收）；③ `ed1` 吸的附魔武器擺出來/匯出/build 後撿起來有沒有附魔；④ `sc pkc XXX` 的大小寫有沒有留住；⑤ `sc ed` 的凍結有沒有被我改壞（回歸）。

## scene-capture-bridge — referrer 原語 `sc ref` / `sc refc`（2026-07-12，DLL crc `112be269`，**已部署**）

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動）。co-save 新增 record `'RFRR'` v1、`SETT` 升 **v5**——**舊存檔照讀**（沒有 referrer 記錄＝空登記簿）。

**這是什麼**：第三個原語。marker 標的是**空座標**（「這裡放東西」）；**referrer 標的是「一個已經存在的東西」的身份 ＋ 一個自由 label**，而且**什麼都不動**（不新建、不移動、不 disable）。label 進 ModForge 後就是一個**可以被任何 ref 欄位引用的名字**——例：指一張椅子標 `sofia's chair`，Sofia 的 sandbox package 就能錨在**那張**椅子。三兄弟：`removals[]` 擦掉既有、`overrides[]` 移動既有、**`references[]` 命名既有**。

**離線已閉環**（不用你驗）：手寫 DLL 形狀的 json → build → 椅子 REFR 帶 **0x400 persistent**、落在 cell 的 Persistent group、package 錨到它；C# 928 測綠。**要你驗的是遊戲內那一半**：指得到嗎、標籤留得住嗎、匯出的 json 對得上嗎。

1. **(乙) 檔內相依——最重要的一條，先驗這個**（referrer 指**我們自己擺的**物件）：
   - 找個空地／室內 → `sc pk` 吸一張椅子（或任何家具）→ `sc pl` **擺一張出來**（這是 dynamic ref，沒有耐久 FormID）。
   - `sc ref`（進 referrer 模式）→ 對著**剛擺的那張椅子**按動作鍵（**F11**）→ 螢幕/console 應說 `SCB: reference recorded`。（指不到？`sc ref er1` 改用射線再按。）
   - **F1 → References 頁**：應有一列，**綠字** `ours -> MFRef_ref_1_1`（＝將寫進 json 的 editorId）＋ base ＋座標。把 label 改成 `sofia's chair`（欄位打字後按 Enter 或按 `apply`）。
   - **一次到位版**：再擺一張，直接 console `sc ref SofiaChair2`（**大小寫保留**）——不必先進面板。⚠️ console 參數以空白分隔：**有空格的 label 要加引號**（`sc ref "sofia's chair"`），或乾脆在 References 頁改名（面板打什麼都行）。
   - **擋撞名**：把第二列的 label 也改成 `sofia's chair` → **應該改不動**，橘字說 label already used。（ModForge 那邊 label 是全域名字，撞名會炸整份 spec，所以在這裡就擋。）
   - **F1 → Export 頁 → `Export player cell`** → 開那份 `scene-export_*.json`：
     - `placements[]` 裡那張椅子那筆**多了 `"editorId": "MFRef_sofia_s_chair_1"`**；
     - 頂層多一段 **`references[]`**，其 `"ref"` **正是同一個 editorId**（不是 `0xFF......` 的 FormID——**看到 FormID 就是錯的，回報**），`"label": "sofia's chair"`，帶 base/position/rotation/cell 或 worldspace，**沒有 `anchor` 欄位**（正確，那是 ModForge 的選擇權）。
2. **(甲) 外部既有 ref**（vanilla 的東西）：
   - 對一張 **vanilla 椅子/桌子** → `sc ref InnChair`（或任何 label），或 console 點選它再 `sc refc InnChair`（aim-free，跟 `delc`/`capc` 同路）。
   - References 頁該列是**白字**、ref id 長 `Skyrim.esm:0x0XXXXX`。匯出的 `references[].ref` 就是這個耐久 id。**該 vanilla 椅子不該有任何變化**（沒消失、沒移動——referrer 不碰世界）。
3. **拒收三類（每個試一下，看有沒有照講的話拒絕）**：
   - 對 **marker 光球**按動作鍵 → `SCB: that's a marker gem`（marker 本來就有 label，走 `annotations[]`）。
   - 對**你自己用 marker 生出來的 NPC／你 spawn 的 actor** → `SCB: that's an actor you spawned`（cell 匯出不含 actor，沒有 placement 可指）。**vanilla NPC（如 Lydia）則應該可以指**（走外部路，白字 `Skyrim.esm:0x...`）。
   - 同一個 ref 再標一次 → `SCB: that ref is already referred to`。
4. **跨存檔/重開（co-save `'RFRR'`）**：標好幾筆 → **存檔 → 完全關遊戲 → 重開 → 讀檔** →
   - References 頁**該列全部還在**、label/note 沒掉。
   - **(乙) 檔內那幾列**：走回那個 cell（讀檔會自動掃玩家所在 cell）→ 該列**不該**顯示 `TARGET LOST`（DLL 會按 base＋座標把 dynamic ref 撿回來）。若顯示 TARGET LOST → 該列匯出時會被跳過（Export 頁會說 `N reference(s) skipped`），**回報這個現象**（我要知道 reacquire 撿不回的頻率）。
   - **⚠️ 已知限制**：檔內目標的 placement 若不在這次匯出掃到的 cell 裡，那筆 reference **不會**寫進 json（寫了 build 也對不上）——Export 頁會列出 skipped 數，log 有每筆的原因。
5. **（端到端）**：referrer 的**最終價值證明**已經先做成一份可直接裝的 demo 了 → 見下一節 **`ModForgeReferrerChair.zip`**（不必等你匯出 json）。你這邊只要驗 DLL 那一半（①～④）。

**回報**：① `references[]` 的 `ref` 是不是 editorId（不是 FormID）、跟 placements 那筆對不對得上；② 撞名有沒有擋住、label 大小寫有沒有留住；③ 三類拒收對不對；④ 重開讀檔後檔內目標撿不撿得回。

## 🔑 referrer 價值證明 — 她真的會去坐**被命名的那一張**椅子（2026-07-12 交付 `~/skyrim_mods/mine/ModForgeReferrerChair.zip`，FLAT，ESL，只吃 Skyrim.esm）

spec＝`examples/referrer-chair-anchor.json`。**這是 referrer 原語的價值主張本身**：擺一張椅子 → 用 label 命名它 → 一個 NPC 的 AI package 拿那個 label 當**特定 reference 錨點** → 她走過去坐**那一張**。上一節驗的是 DLL 能不能「指＋標」；**這一節驗的是標了以後到底有沒有用**。

**不需要新遊戲**（沒有對話註冊需求；她的招呼語走 `.seq`，既有存檔 save+reload 一次就好，不看招呼語也行）。裝 zip → 啟用 → load order 隨意（只 override Breezehome 這個 cell）。

**🧪 對照組設計（整份 demo 的成敗關鍵，看懂這段再進遊戲）**
房間裡有**兩張一模一樣的椅子**——同一個 base（`CommonChair01F`）、同角度、同尺寸、同一張 navmesh、排成一直線在 Sofia 的正北方：

```
   (北牆)
     ▣  ← 【被命名的】"sofia's chair"   y=400   離 Sofia 380 單位   REFR 0x808
     
     ▣  ← 【對照組】沒被命名的椅子       y=170   離 Sofia 150 單位   REFR 0x807
     
     🧍 Chairwarden Sofia               y=20
   ✧ 你 coc 落點（在她西邊幾步）
```

兩張椅子**唯一的差別是其中一張出現在 `references[]` 裡**。近的那張是空的、可以坐、就擋在她路上。所以：

- **成功長什麼樣**：她**繞過／走過那張近的空椅子**，一路走到北牆邊，坐上**遠的那一張**。（她可能在你 loading 結束前就已經坐好了——**沒關係，重點不是看她走，是看她坐在哪一張**。）
- **失敗長什麼樣**：① 她**站著不動**（＝ package 的 target 沒解到 → label 沒接上）；② 她坐**近的那張**（＝根本不是特定 reference targeting，只是「找張椅子坐」）；③ 她**不在那**（cell override / 擺放出問題）。
- 「她坐了某張椅子」**不可能**被誤讀成「她坐了我們命名的那張」——這就是對照組存在的理由。

**步驟**
1. console `coc WhiterunBreezehome`。（Breezehome 是**vanilla 內裝**，所以有 navmesh；自建內裝沒有 navmesh，NPC 根本不會動——這是刻意的選擇。）
2. 站在原地等 **10–20 秒**（剛 `coc` 進去 AI 要幾秒才「醒」）。看她往北走、坐上**靠北牆**那張。
3. **鐵證（不靠肉眼）**：console 點一下**她正坐著的那張椅子** → console 標題列會顯示它的 RefID。應該是 **`FE xxx 808`**（`xxx` ＝ MO2 右欄顯示的 ESL 槽位）。對照組那張是 `FE xxx 807`。**`...808` ＝ 通過；`...807` ＝ 她坐錯張。**
4. **「常回去坐」複驗**：把她從椅子上趕起來（跟她講話／推她／`coc` 出去再回來），再等 10–20 秒 → 她應該**自己走回同一張（808）**坐下。
5. （可選）她的招呼語是 `That chair by the north wall is mine. Find your own.` — 聽到＝她本人沒錯。

**離線已驗（你不用再驗這幾條，列出來是讓你知道失敗時該懷疑哪裡）**
- 被命名的椅子 `MFRef_SofiaChair` = REFR **0x808**，record flag **0x400**，落在 cell 的 **Persistent group**。
- 對照組 `MFRef_DecoyChair` = REFR **0x807**，flag **0x0**，落在 **Temporary group**。兩者 spec 只差一個 label ⇒ **是 `references[]` 讓它 persistent 的**。
- package `MFRefSofiaSit`（SitTarget 模板 `0x0A9277`）slot **16 = `PackageTargetSpecificReference(0x808)`** ⇒ 錨點確實指到**被命名的那張**，**不需要 quest alias**。DataInputVersion/XNAM 與 vanilla `CaravanACamp1Sit` 同形。
- Sofia 的 NPC 記錄 `packages = [MFRefSofiaSit]`。

**回報**：她坐哪一張（RefID 尾碼 807 / 808）？還是站著不動？趕起來會不會自己走回去？

## scene-capture-bridge — Export 頁 `Export requires` 鈕（2026-07-12，DLL crc `008aba47`，**已部署**）

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動、**co-save 不升版**——純新增一顆唯讀按鈕，舊存檔直接用）。

**這是什麼**：`sc capp` 吸一個玩家分身，就會把「給過你法術/perk/裝備的每一個 mod」變成生成 esp 的 **master**；缺 master 時 Skyrim **靜默不載**這個 esp（不報錯、log 也沒有）。`modforge build` 已經會印這份分析——但**那時你已經退出遊戲了**。這顆鈕把它**提前到匯出當下**：你人還站在那間房，覺得那顆 PROTEUS 法術不值得讓整個 mod 變成硬相依，**重吸一次就好**。

**它不改任何東西**（唯讀掃描 → 寫一份 .txt），也**不過濾**你的匯出——只是讓代價可見。

1. **先製造依賴**（重點是「非 vanilla 的來源」）：
   - `sc capp` 吸一次玩家（你身上的 PROTEUS/Ordinator/Apocalypse 法術、mod 的裝備都會進來）；
   - 順手 `sc pk` 吸一個 **mod 來的**物件（JK's Skyrim 之類的家具）→ `sc pl` 擺出來；
   - 對一個 **mod 的** ref 用 `sc del` 擦掉、或 `sc ed` 移動一下（→ `removals[]`／`overrides[]` 也算依賴）。
2. **F1 → Export 頁 → `Export requires`**（在 Export captures 下面那一區）：
   - 面板應**橘字**顯示 `N non-vanilla master(s), M link(s)`（純 vanilla 的話顯示 `vanilla only — the plugin will load for anybody`），下面一行 `Wrote <路徑>`。
   - 檔案在 **SKSE log 夾**（跟 `scene-export_*.json` 同一個夾），名字 `requires_<YYYYMMDD-HHMM>.txt`。同一分鐘再按一次＝ `-2.txt`，**永不覆蓋**。
3. **打開那份 .txt 看三件事**：
   - **開頭講清楚後果**（缺這些 mod → Skyrim 靜默不載）＋ vanilla 五個列在 `# vanilla (...)` 那行；
   - 每個 mod 底下**逐行講是誰把它拉進來的**，例如 `captures.capturedNpcs[0].spells[17] = PROTEUS.esp:0x08073D`——**那就是你要刪的那一行**（`scene.` ＝去改 `scene-export_*.json`，`captures.` ＝去改 `captures_*.json`）；
   - **⚠️ 最該檢查的一條**：你的 `capturedNpcs[].activeEffects[]`（當下 buff 快照）通常會提到一堆 mod 的 MGEF——**這些 mod 不該出現在報告裡**（除非同一個 mod 另有法術/perk 之類的**真**依賴把它拉進來）。activeEffects 不會產生任何 esp link，列它＝說謊（刪掉它並不會拿掉依賴）。同理 `capturedNpcs[].base`（來源 NPC）也不該讓那個 mod 上榜。**若看到一個 mod 只因 activeEffects/base 就被列出來 → 那是 bug，回報。**
4. **跟 C# 端對照**（真正的驗收）：把 `scene-export_*.json` ＋ `captures_*.json` 給我 → 我 `build` 一份 esp → C# 會印它自己的 master 清單＋寫 `<plugin>.requires.txt`。**兩份的 mod 名單應該一致**（歸因粒度可能不同：C# 對 deep-copy 的 template clone 只講得出 `record Weapon:MFCap_…`，DLL 這邊講得出是哪一行——那不算不一致）。**名單本身若對不上，回報**，那代表其中一邊的規則錯了。

**回報**：① 面板數字與 .txt 內容對不對得上；② activeEffects/base 有沒有污染名單（**最關鍵**）；③ 跟 C# `<plugin>.requires.txt` 的 mod 名單一不一致；④ 有沒有哪個 mod 你確定需要、但兩邊都沒列出來（漏報比誤報嚴重）。

## scene-capture-bridge — 旋轉 per-axis 還原 ＋ palette replace（2026-07-12，**已部署**，含在 DLL `c5049c78` 裡)

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動、co-save 不升版、palette json 相容——只是**檔內順序改成「最上面那筆排第一」**，舊檔讀進來順序會上下顛倒一次，之後就穩定了）。

**改了什麼**：① 旋轉子模式的歸零鍵改成 **per-axis 還原**（不是全軸、也不是設成 0，是**還原成進編輯前的該軸原值**）；② palette 的 `load from file` 明確「載入的排最上面」＋新增 **`replace from file`**（清空再載入）。

1. **per-axis 還原**：找一個**本來就有角度**的物件（斜靠的木板、歪的椅子都行）→ `sc ed` 動作鍵選中 → **`sc ed ax`**（Editor 頁提示行應顯示「per-axis revert: 5 yaw, 2 pitch, 8 roll」）→
   - **1/3 轉 pitch** 幾下 → 按 **numpad 2** → **只有 pitch 彈回原本的角度**（yaw/roll 保持你剛剛轉的、位置/大小不動）；螢幕跳「SCB: pitch reverted」。
   - **4/6 轉 yaw** → 按 **numpad 5** → 只有 yaw 回原值（跳「SCB: yaw reverted」）。
   - **7/9 轉 roll** → 按 **numpad 8** → 只有 roll 回原值（跳「SCB: roll reverted」）。
   - **關鍵驗**：物件**原本就有的角度不該被吃掉**——三軸都按一遍還原後，物件應回到你選中它時的樣子（**不是**變成軸對齊的 0 度）。
   - 移動模式（`sc ed` 退回）**不受影響**：numpad 5 仍＝整個編輯復原（位置＋角度＋大小）。
2. **palette append 排最上**：`sc pk` 吸 2 個（A、B）→ 檔名框打 `pal-x.json` → **save to file** → 再吸 1 個 C（面板最上面是 C）→ **load from file (append)** → 面板應變成 **A、B 在最上面**（檔內順序，A 在最頂）、C 及原有的在下面；總數 = 原本 3 + 載入 2 = 5。
3. **palette replace**：**replace from file** 同一個 `pal-x.json` → 插槽**只剩檔案裡那 2 筆**（原有的全清掉）、順序照檔案。
   - **防呆**：檔名框打一個**不存在**的檔名再按 replace → **什麼都不該發生**（插槽不該被清空）；SKSE log 有 warn。
4. **落盤**：關遊戲重開（palette 是磁碟持久、不隨存檔）→ 插槽＝你最後一次操作的結果。

**回報**：① 三個歸零鍵是不是各管各軸、且還原成「原本的角度」而非 0；② append 有沒有排最上、replace 有沒有清乾淨、打錯檔名會不會誤清。

## scene-capture-bridge — rebind 重作（2026-07-12，DLL crc `378d3c6c`，**已部署**）

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動）。co-save `SETT` 升 **v6→v7**——舊存檔照讀（過去被丟棄不用的鍵位資料現在會**真的套用**，並過一次保留鍵過濾；`kCapture`/`kReferrer` 這兩個模式過去從沒存過鍵位，這次補上）。

**背景**：P5 實機（2026-07-11）撞到 rebind 捕捉抓錯鍵（誤綁成 W），當時暫時隱藏、固定 F11。這輪重作：armed 狀態下 WASD/Space/Shift/Ctrl/Esc/Tab/Enter/console 反引號**一律不接受**（面板/畫面會提示「that key is reserved, press another」），且必須**按下＋放開同一顆鍵**才真的 commit（面板顯示「release X to confirm」）。Esc 隨時取消。

1. **基本 rebind（先驗這個）**：F1 → Settings 頁 → 找 `del`（刪除模式）那一列 → 按它旁邊的 `Rebind##del` 鈕 →
   - 面板應立刻變黃字：`Rebinding delete -- press a key (Esc cancels; ...)`，且**其他模式的 Rebind 鈕應變灰不能按**（一次只能改一個）。
   - 按一個沒用過的鍵，例如 **F2** → 面板文字應變成 `Rebinding delete -- release F2 to confirm`（還沒放開就先按住看這句）。
   - 放開 F2 → 面板應跳回正常列表，`del` 那一列的鍵位文字應顯示 **F2**、黃字狀態列消失、其他模式的 Rebind 鈕恢復可按。
   - 進 `sc del` 模式（console 或按鈕），瞄準一個東西按 **F2** → 應該真的觸發擦除（原本的 F11 對 `del` 這個模式應該**不再有反應**）。
2. **保留鍵防呆（這是這次修的重點）**：對另一個模式（例如 `pick`）按 `Rebind` → armed 狀態下依序按著移動：
   - 按 **W**（或 A/S/D/Space/LShift/LCtrl）→ 畫面應跳一個小提示（例如「that key is reserved, press another」），**面板應維持在 armed 狀態**（還是「press a key」，不會變成「release ... to confirm」，更不會直接把 pick 綁到 W）。
   - 按 **Tab**、**Enter**、**`**（console 鍵）也應該一樣被拒絕、不解除 armed。
   - 最後按一個正常鍵（例如 F3）並放開 → 才真的綁上。
   - **這一步就是驗收核心**：只要**全程沒有任何一次把某模式意外綁成 W/A/S/D 之類的移動鍵**，這輪修復就算過。
3. **Esc 取消**：按 `Rebind` armed 後、按 **Esc** → 面板應跳回正常（沒有黃字），該模式的鍵位**維持原樣沒被改動**。
4. **同時移動 + rebind（最貼近原本撞坑的情境）**：走位時**手不離開 WASD**、順手用滑鼠點某模式的 `Rebind` 鈕、然後保持按著或反覆點按 W/S 幾下模擬「還沒騰出手」的狀態，最後才按你真正想要的鍵（例如 F4）並放開 → 綁定結果**必須是 F4**，不能是 W 或 S。這是最初 bug 的實際重現路徑，務必測。
5. **持久化（co-save v7）**：把 2–3 個模式改綁成不同的鍵（例如 F2/F3/F4）→ 存檔 → **完全關遊戲重開** → 讀檔 → F1 → Settings 頁確認**改過的鍵位全部還原**（不是退回 F11）→ 用其中一個改過的鍵實際觸發一次動作確認真的生效。
6. **`sc capp` 直接吸的組合場景要沒事**（回歸檢查，不是新功能）：任意切換模式、按 F11（其餘沒改綁的模式應該還是預設 F11）確認**沒改綁的模式完全不受影響**。

**回報**：① 第 2 步（保留鍵防呆）有沒有守住——這是本輪修復要不要算過的關鍵；② rebind 完的鍵位實際觸發動作有沒有生效、舊鍵（如原本的 F11）對該模式是否確實失效；③ 重開遊戲後鍵位有沒有正確還原；④ 面板的黃字狀態列/其他鈕變灰有沒有出現。
