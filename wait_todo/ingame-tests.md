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

## scene-capture-bridge P9 擷取器（現行部署＝ DLL `dd7afd82`，co-save `'SCCP'` v9）

**2026-07-11 端到端全通**：`sc cap` 模式（F11）吸 Mirabelle → Export → 消費 → build → 進遊戲**分身在學院庭院原地出現** ✅（OPEN-A 核心流程、OPEN-C 18-morph 修復、消費端 Phase 1 一次實證；落地記錄 [landed/npcs](../workflows/feature-dev/landed/npcs.md)）。殘餘：

**OPEN-A 殘餘（最後一小項）**：存檔完全重開 → capture 的 aim source（er0/er1）還原（co-save SETT v4）。（`sc cap er1` 射線吸取 2026-07-11 已實證 OK。）另留意：模式制下重複按 F11 會吸出重複列（正常行為，消費端 editorId 已防撞）。

## scene-capture-bridge — 玩家 perk 改「全收」（2026-07-13，DLL **`e19ad4ca` 已部署**，小驗）

> ⚠️ **完全關遊戲重開**才吃得到新 DLL（esp 不動）。

使用者拍板 (b)：橋端**不再二選一**——base TESNPC 的 perk **＋** 玩家 runtime `addedPerks`，**兩個都收**（依 durable id 去重、同 perk 取高 rank）。取捨留給 ModForge 端（[backlog](../workflows/plans/scene-capture-bridge/backlog.md)）。

**驗**：`sc capp <label>` 重吸一次 → Export captures →
- perk 數應從 **26 變成約 38**（＝12 base ＋ 26 added，去重後）；
- 名單裡應**多出 `Skyrim.esm:0x0F11A9`（`AllowShoutingPerk`）**這類 Player 記錄的管線 perk（上一版沒有）；
- **原本那 26 顆真 perk 一顆都不能少**（`Armsman00` 等單手樹的還在＝沒退化）。

把新的 `captures_*.json` 檔名給我，我對帳。

## scene-capture-bridge — `sc capp` 直接吸玩家：**數值那條**（2026-07-12，DLL `dd7afd82` 已部署）

> **✅ 外貌路徑已 🎮 PASS（2026-07-12）**：`sc capp` 抓對玩家 base（`Skyrim.esm:0x000007`），分身臉**確認是本人**——faceMorphs/headParts/hairColor/faceTexture ＋ **`tintLayers` 戰紋**全中（戰紋正是 PROTEUS 路線拿不到的那層）。交付 `~/skyrim_mods/mine/MFCapHatak.zip`。落地句進 [landed/npcs](../workflows/feature-dev/landed/npcs.md)。
>
> **↓ 下面只剩「數值」這條**（必須用**練過的角色**驗——白紙角色兩種讀法吐一樣的數字，等於沒驗）。可以跟上面那節的 perk 驗收**同一次吸取一起做**。

co-save **SCCP v9**——舊存檔的 captures 照讀（缺的欄位＝0，行為同以前）。

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
   - （`isPlayer` ＋ perk 的驗收已獨立成上面那節，別重複做。）
   - **`weight: 0.0` 不是 bug**：那是你 chargen 的體重滑桿真值（Skyrim.esm 裡 Player base 是 100，我們吸到 0 → 正好證明讀的是**存檔改寫過的** TESNPC 而不是磁碟原始記錄；Ancano 的 0 也跟 Skyrim.esm 一字不差）。想對照就 console `player.getnpcweight`。
4. **順手複驗（不破舊路）**：對一個**普通 vanilla NPC** `sc capc` → 匯出的那筆現在也該多出 `health/magicka/stamina/skills`，且 build 出來的分身**不再開 autoCalcStats**（數值照抄本尊）。舊 capture json（沒這些欄位的）照舊走 class-autocalc——**不該壞**。

**回報**：① `sc capp` 有沒有吸到、label 大小寫對不對；② 分身的數值/perk/裝備對不對；③ voiceType/物品欄的落差要不要修。

## scene-capture-bridge — 模式開關套件 `py` / `ed` / `pkc`（2026-07-12，**已部署**）

> **✅ `noHavokSettle` 整條 🎮 PASS（2026-07-13）——這節的核心已結案**，濃縮句進 [landed/world](../workflows/feature-dev/landed/world.md)。**剩下**：`ed1`（實例附魔）／`pkc`（console 滴管大小寫）／`ed py0/py1` 回歸／跨存檔 reacquire。

⚠️ co-save：`SETT` v6、record `'PLEX'` v1——**舊存檔照讀**（讀不到就落回預設：place `py1`、edit `py0`、extra data 全關＝跟以前一模一樣）。

**這是什麼**：四個 per-mode 開關 ＋ 一個 aim-free 滴管。核心是 **`sc pl py0`＝「擺好的東西不要被物理彈飛」**，而且**這次真的會 ship 進 esp**（不只是遊戲內凍住）。

**離線已閉環**（不用你驗）：手寫 DLL 形狀 json → build → REFR 帶 `0x20000000`（DontHavokSettle）、鑄造的附魔劍 WEAP+ENCH 都在、placement 的 base 解得到它；C# 932 測綠。**要你驗的是遊戲內那一半。**

1. **`sc pl py0/py1` — 擺放物理（最重要，先驗這個）**
   - `sc pk` 吸一個**雜物**（杯子/盤子/書/武器——havok 會動的那種），`sc pl` 進擺放模式。
   - **先看壞的樣子**：`sc pl py1`（預設）→ 動作鍵（**F11**）在**桌面上**擺一個 → 它會**掉下來/滾走/被你走過去撞飛**。這是現況，正常。
   - **再看好的樣子**：`sc pl py0` → console 應印 `placed objects have physics OFF (py0)` ＋ 一行說明 → 再擺一個 → **它應該定在原地**（不掉、撞不動）。
   - **console `sc` 印出來的 usage** 應該看得到 `sc pl py1 / py0`、`sc ed py0 / py1`、`sc pk ed0 / ed1`、`sc pl ed0 / ed1`、`sc pkc [Label]` 這幾行。**F1 → Settings** 也應該有 `Physics (…)` 與 `Extra data (…)` 兩節，顯示 place/edit/pick/place 的現況。
   - **🔑 真正的驗收在 esp——✅ 已 PASS（2026-07-13）**：`ModForgeGoblets.zip` v2 的懸空判別法，實機**3 顆浮在半空 / 3 顆掉到地上** ⇒ `0x20000000` 確實會 ship 進 esp 並生效。（⚠️ 判讀教訓：**貼地靜止的物件驗不出這個旗標**——settle 對它們本來就不做事；要判別必須讓物件**懸空**。v1 的 8 顆銀杯 z 全在同一平面 ⇒ 無效實驗。）
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

## scene-capture-bridge — referrer 原語 `sc ref` / `sc refc`（2026-07-12，**已部署**）

> **✅ 匯出這一半 2026-07-12 已 🎮 PASS**（你遊戲內標了 5 筆）：**3 筆甲路徑**（外部 vanilla ref → 記耐久 FormID）＋ **2 筆乙路徑**（檔內目標 → `references[].ref` ＝ editorId `MFRef_ref_4_4`／`MFRef_ref_5_5`，對應的 placement 真的被蓋章）——`scene-export_Tamriel_x28y27_20260712-2243.json`，5 references。**價值證明（NPC 真的去坐被命名的那張椅子）也早已 PASS**。⇒ **下面 1./2./5. 已結案**，只剩 **3.（拒收三類）** 與 **4.（跨存檔重開撿回）**，外加**改 label 的路徑沒走過**（你這次用的是預設 label `ref_4`/`ref_5`，所以「大小寫保留」「撞名擋下」都還沒驗）。

⚠️ co-save record `'RFRR'` v1、`SETT` v5——**舊存檔照讀**（沒有 referrer 記錄＝空登記簿）。

**這是什麼**：第三個原語。marker 標的是**空座標**（「這裡放東西」）；**referrer 標的是「一個已經存在的東西」的身份 ＋ 一個自由 label**，而且**什麼都不動**（不新建、不移動、不 disable）。label 進 ModForge 後就是一個**可以被任何 ref 欄位引用的名字**——例：指一張椅子標 `sofia's chair`，Sofia 的 sandbox package 就能錨在**那張**椅子。三兄弟：`removals[]` 擦掉既有、`overrides[]` 移動既有、**`references[]` 命名既有**。

**離線已閉環**（不用你驗）：手寫 DLL 形狀的 json → build → 椅子 REFR 帶 **0x400 persistent**、落在 cell 的 Persistent group、package 錨到它；C# 928 測綠。**要你驗的是遊戲內那一半**：指得到嗎、標籤留得住嗎、匯出的 json 對得上嗎。

1. **改 label 的路徑**（這次沒走到——你用的是預設 label `ref_4`/`ref_5`）：
   - **F1 → References 頁**把某一列的 label 改成 `sofia's chair`（打字後按 Enter 或 `apply`）→ 匯出的 json 裡該 placement 的 editorId 應變成 **`MFRef_sofia_s_chair_<seq>`**、`references[].ref` 指同一個。
   - **console 一次到位版**：`sc ref SofiaChair2` → **大小寫應保留**（不是 `sofiachair2`）。⚠️ console 參數以空白分隔：**有空格的 label 要加引號**（`sc ref "sofia's chair"`），或乾脆在面板改名。
   - **擋撞名**：把第二列的 label 也改成同一個 `sofia's chair` → **應該改不動**，橘字說 label already used。（ModForge 那邊 label 是全域名字，撞名會炸整份 spec，所以在這裡就擋。）
2. **拒收三類（每個試一下，看有沒有照講的話拒絕）**：
   - 對 **marker 光球**按動作鍵 → `SCB: that's a marker gem`（marker 本來就有 label，走 `annotations[]`）。
   - 對**你自己用 marker 生出來的 NPC／你 spawn 的 actor** → `SCB: that's an actor you spawned`（cell 匯出不含 actor，沒有 placement 可指）。**vanilla NPC（如 Lydia）則應該可以指**（走外部路，白字 `Skyrim.esm:0x...`）。
   - 同一個 ref 再標一次 → `SCB: that ref is already referred to`。
3. **跨存檔/重開（co-save `'RFRR'`）**：標好幾筆 → **存檔 → 完全關遊戲 → 重開 → 讀檔** →
   - References 頁**該列全部還在**、label/note 沒掉。
   - **(乙) 檔內那幾列**：走回那個 cell（讀檔會自動掃玩家所在 cell）→ 該列**不該**顯示 `TARGET LOST`（DLL 會按 base＋座標把 dynamic ref 撿回來）。若顯示 TARGET LOST → 該列匯出時會被跳過（Export 頁會說 `N reference(s) skipped`），**回報這個現象**（我要知道 reacquire 撿不回的頻率）。
   - **⚠️ 已知限制**：檔內目標的 placement 若不在這次匯出掃到的 cell 裡，那筆 reference **不會**寫進 json（寫了 build 也對不上）——Export 頁會列出 skipped 數，log 有每筆的原因。

**回報**：① 撞名有沒有擋住、label 大小寫有沒有留住；② 三類拒收對不對；③ 重開讀檔後檔內目標撿不撿得回。

## scene-capture-bridge — 動作鍵改走 `.ini` ＋ palette `clear` 鈕（2026-07-12，commit `1fffb15`，**已部署**）

⚠️ **已部署**（DLL `dd7afd82`，同時含 `isPlayer` 修正）——你**下次啟動遊戲**就會吃到，不必再做任何部署動作。esp 不動、co-save 不升版（SETT v7）。

**背景**：遊戲內 rebind **兩次實機都失敗**（P5 綁成 W；`ddf6324` 的黑名單＋按放開版你回報仍失敗）→ 依你的拍板，**遊戲內 rebind 整個移除**，鍵位改由 **`SceneCaptureBridge.ini`** 設定。co-save 仍照存鍵位（SETT v7 不變），但**ini 有寫的模式以 ini 為準**。

1. **ini 自動生成**：新 DLL 進遊戲一次 → 去 `…/Documents/My Games/Skyrim Special Edition/SKSE/`（＝ palette／匯出檔那個資料夾）→ 應該出現 **`SceneCaptureBridge.ini`**，內含 `[Keys]` 七行（全 F11）＋一大段註解（可用鍵名清單、保留鍵說明）。**先確認這個檔存在且看得懂**。
2. **改鍵生效**：用文字編輯器把 `delete = F11` 改成 **`delete = F4`**、`edit = G`（存檔）→ 回遊戲 F1 → Settings 頁按 **`reload keys from ini`** →
   - 鍵位表應顯示 `delete  F4  (ini)`、`edit  G  (ini)`，其餘仍 F11。
   - `sc del` → 瞄準東西按 **F4** 應真的擦除；按 **F11** 對 delete 模式應**沒反應**（其他沒改的模式 F11 照常）。
   - **不重開遊戲就生效**（這顆鈕的重點）；重開遊戲後也應維持。
3. **保留鍵拒收**：ini 寫 `pick = W`（或 Space/Shift/Ctrl/Tab/Enter/`）→ `reload keys from ini` → 面板應出現橘字（`ini: pick: 'W' is reserved`），`pick` **維持原鍵不變**，SKSE log 有 warn。**這是防呆核心**：ini 不能把你綁死在移動鍵上。
4. **鍵名容錯**：試 `capture = numpad 5`、`marker = NumPad5`、`place = 0x3E`（＝F4）都應該吃得下（面板顯示 `numpad 5` / `F4`）；亂打 `pick = Banana` → 橘字報 unknown key、該模式維持原鍵。
5. **ini vs 存檔優先序**：讀一個**舊存檔**（裡面可能存著舊鍵位）→ Settings 頁的鍵位應該**還是 ini 那組**（標 `(ini)`），不是被存檔蓋回去。把 ini 裡某一行**整行刪掉**再 reload → 該模式改吃存檔/預設（標 `(save / default)`）。
6. **palette `clear all slots`（新鈕）**：Palette 頁 →
   - 先 `sc pk` 吸 2–3 個東西 → 按 **`clear all slots`** → 應**先變成** `really clear all N slot(s)?` ＋ `yes, clear` / `cancel` → 按 **cancel** → **什麼都不該發生**。
   - 再按一次 → `yes, clear` → 插槽全空，且出現 **`undo clear (N slot(s) recoverable...)`**。
   - 按 **`undo clear`** → 插槽**全部回來**。再 clear 一次、**關遊戲重開** → 插槽應該是空的（clear 有寫回磁碟；undo 只在本次 session 有效，這是預期行為）。

**回報**：① ini 有沒有自動生成、看不看得懂；② 改鍵 + `reload keys from ini` 有沒有真的生效（含舊鍵失效）；③ 保留鍵（W 之類）有沒有被拒；④ 舊存檔會不會蓋掉 ini；⑤ palette clear 的二次確認 / undo 有沒有照走。
