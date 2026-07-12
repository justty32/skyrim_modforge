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

## scene-capture-bridge — referrer 原語 `sc ref` / `sc refc`（2026-07-12，DLL crc `112be269`，**已部署**）

⚠️ **完全關遊戲重開**吃新 DLL（esp 不動）。co-save 新增 record `'RFRR'` v1、`SETT` 升 **v5**——**舊存檔照讀**（沒有 referrer 記錄＝空登記簿）。

**這是什麼**：第三個原語。marker 標的是**空座標**（「這裡放東西」）；**referrer 標的是「一個已經存在的東西」的身份 ＋ 一個自由 label**，而且**什麼都不動**（不新建、不移動、不 disable）。label 進 ModForge 後就是一個**可以被任何 ref 欄位引用的名字**——例：指一張椅子標 `sofia's chair`，Sofia 的 sandbox package 就能錨在**那張**椅子。三兄弟：`removals[]` 擦掉既有、`overrides[]` 移動既有、**`references[]` 命名既有**。

**離線已閉環**（不用你驗）：手寫 DLL 形狀的 json → build → 椅子 REFR 帶 **0x400 persistent**、落在 cell 的 Persistent group、package 錨到它；C# 928 測綠。**要你驗的是遊戲內那一半**：指得到嗎、標籤留得住嗎、匯出的 json 對得上嗎。

1. **(乙) 檔內相依——最重要的一條，先驗這個**（referrer 指**我們自己擺的**物件）：
   - 找個空地／室內 → `sc pk` 吸一張椅子（或任何家具）→ `sc pl` **擺一張出來**（這是 dynamic ref，沒有耐久 FormID）。
   - `sc ref`（進 referrer 模式）→ 對著**剛擺的那張椅子**按動作鍵（**F11**）→ 螢幕/console 應說 `SCB: reference recorded`。（指不到？`sc ref er1` 改用射線再按。）
   - **F1 → References 頁**：應有一列，**綠字** `ours -> MFRef_ref_1_1`（＝將寫進 json 的 editorId）＋ base ＋座標。把 label 改成 `sofia's chair`（欄位打字後按 Enter 或按 `apply`）。
   - **一次到位版**：再擺一張，直接 console `sc ref my chair 2`（**大小寫/空格保留**）——不必先進面板。
   - **擋撞名**：把第二列的 label 也改成 `sofia's chair` → **應該改不動**，橘字說 label already used。（ModForge 那邊 label 是全域名字，撞名會炸整份 spec，所以在這裡就擋。）
   - **F1 → Export 頁 → `Export player cell`** → 開那份 `scene-export_*.json`：
     - `placements[]` 裡那張椅子那筆**多了 `"editorId": "MFRef_sofia_s_chair_1"`**；
     - 頂層多一段 **`references[]`**，其 `"ref"` **正是同一個 editorId**（不是 `0xFF......` 的 FormID——**看到 FormID 就是錯的，回報**），`"label": "sofia's chair"`，帶 base/position/rotation/cell 或 worldspace，**沒有 `anchor` 欄位**（正確，那是 ModForge 的選擇權）。
2. **(甲) 外部既有 ref**（vanilla 的東西）：
   - 對一張 **vanilla 椅子/桌子** → `sc ref skulvar's hoe`（或任何 label），或 console 點選它再 `sc refc <label>`（aim-free，跟 `delc`/`capc` 同路）。
   - References 頁該列是**白字**、ref id 長 `Skyrim.esm:0x0XXXXX`。匯出的 `references[].ref` 就是這個耐久 id。**該 vanilla 椅子不該有任何變化**（沒消失、沒移動——referrer 不碰世界）。
3. **拒收三類（每個試一下，看有沒有照講的話拒絕）**：
   - 對 **marker 光球**按動作鍵 → `SCB: that's a marker gem`（marker 本來就有 label，走 `annotations[]`）。
   - 對**你自己用 marker 生出來的 NPC／你 spawn 的 actor** → `SCB: that's an actor you spawned`（cell 匯出不含 actor，沒有 placement 可指）。**vanilla NPC（如 Lydia）則應該可以指**（走外部路，白字 `Skyrim.esm:0x...`）。
   - 同一個 ref 再標一次 → `SCB: that ref is already referred to`。
4. **跨存檔/重開（co-save `'RFRR'`）**：標好幾筆 → **存檔 → 完全關遊戲 → 重開 → 讀檔** →
   - References 頁**該列全部還在**、label/note 沒掉。
   - **(乙) 檔內那幾列**：走回那個 cell（讀檔會自動掃玩家所在 cell）→ 該列**不該**顯示 `TARGET LOST`（DLL 會按 base＋座標把 dynamic ref 撿回來）。若顯示 TARGET LOST → 該列匯出時會被跳過（Export 頁會說 `N reference(s) skipped`），**回報這個現象**（我要知道 reacquire 撿不回的頻率）。
   - **⚠️ 已知限制**：檔內目標的 placement 若不在這次匯出掃到的 cell 裡，那筆 reference **不會**寫進 json（寫了 build 也對不上）——Export 頁會列出 skipped 數，log 有每筆的原因。
5. **（選配，端到端）**：把步驟 1 的 `scene-export_*.json` 給我 → 我 build 一份帶 Sofia 的 esp → 裝進去看她**真的會回去坐那張椅子**（sandbox 錨在 label 上）。這一步才是 referrer 的最終價值證明。

**回報**：① `references[]` 的 `ref` 是不是 editorId（不是 FormID）、跟 placements 那筆對不對得上；② 撞名有沒有擋住、label 大小寫有沒有留住；③ 三類拒收對不對；④ 重開讀檔後檔內目標撿不撿得回。

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
