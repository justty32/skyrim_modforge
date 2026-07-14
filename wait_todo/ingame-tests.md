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

## scene-capture-bridge — 面板欄位一致化（bound field 重構 ＋ 六頁 label／note，2026-07-14，**已部署** DLL `c4460315`）

⚠️ **已部署**——**完全關閉遊戲再開**才吃得到新 DLL。co-save 升版（`'ERSR'` v3／`'OVRD'` v2／`'SCCP'` v10）：**舊存檔讀得進來**（新欄位＝空字串），但**新存檔存的東西舊 DLL 讀不到**——這是單向的，正常。

**這批在修什麼**：你 07-13 回報的 🐞「打完字沒按 Enter 就點走，面板一直顯示新名字、存的卻是舊的」。修法是把六個面板的欄位收成一個共用 bound field（RULE 1：非編輯中的每一幀都從 registry 回種 ⇒ 面板不可能顯示 registry 沒有的值；RULE 2：Enter／apply／點走都提交）。順帶把 label＋note 補齊到 Eraser／Captures／Editor(overrides)／Palette。

1. **🐞 主證（就是這條要驗）**：Markers 頁隨便一列，label 改個字 → **不要按 Enter**，直接用滑鼠點面板別的地方 → ① 名字**應該真的改掉**（不是彈回舊的、也不是「看起來改了其實沒改」）；② 立刻 `Export player cell` → 開匯出的 json，`annotations[].label` **應該是新名字**。**以前這裡：面板顯示新的、json 是舊的、且毫無跡象。**
2. **撞名要看得見**：References 頁把某列的 label 改成**跟另一列一樣** → 應出現橘字「label already used…」，且該欄位**視覺上彈回**原本的 label（以前會繼續顯示你打的、實際沒生效）。
3. **六頁都能取名＋寫筆記**（新欄位）：Eraser／Captures／Editor(overrides)／Palette 每一列現在都有 `label`＋`note`＋`apply`。各挑一列寫點字 →
   - **存檔 → 完全重開遊戲 → 讀檔** → label/note **應該還在**（Eraser／Overrides／Captures 走 co-save）。
   - **Palette 的筆記走磁碟**：寫完直接去看 `…/SKSE/scene-capture-palette.json`，該 slot 應多一個 `"note"`；按 `save to file` 存成另一個檔，筆記也應該跟著走。
4. **匯出**（拍板 (b)「加欄位、非破壞」）：
   - `sc del` 擦兩個東西，**只給其中一個寫 note** → `Export player cell` → json 的 `removals[]` 應該是**混合的**：沒寫 note 的仍是**裸字串** `"Skyrim.esm:0x…"`，寫了的變成 `{"ref": "...", "note": "..."}`。（沒寫筆記的匯出跟以前逐位元相同，舊 spec 照讀。）
   - 編輯器移動一個 vanilla 物件 + 寫 note → `overrides[]` 該筆應帶 `note`。
5. **⚠️ 回歸（這次重構最可能踩的地方，請特別試）**：
   - **列錯位**：Eraser 頁擦 3–4 個東西、每列給不同 label → 對**中間那列**按 `undo` → 剩下的列，label **應該還跟著各自那筆**（不會整批往上錯一格）。Editor(overrides) 頁的 `revert` 同理。
   - **Palette 索引位移**：Palette 有 3 個以上 slot，**正在某列打字打到一半**（沒提交）→ 直接按**另一列**的 `del`（或 `load from file`／`clear all slots`）→ 你打到一半的字應該**乾脆丟掉**，**絕不能**跑去蓋到別的 slot 名字上。
6. **⚠️ 「打到一半切走」（code review 抓到的漏洞，已修，請驗）**：這是 RULE 2 原本漏掉的第三種離開法——**整列根本沒被畫**。ImGui 在該列缺席的那一幀直接清掉 ActiveId、widget 的 flush 也沒跑，所以「失活」沒有任何人看見 ⇒ 修之前這樣會**無聲丟字**。三個入口各試一次，打完字**都不要按 Enter**：
   - Markers 頁某列 label 打到一半 → **切到別的分頁**（Export/Settings）→ 切回來 → 字**應該已經提交**（不是變回舊的）。
   - 打到一半 → **直接關掉面板**（F1）→ 再開 → 同樣應該已提交。
   - 打到一半 → 勾 **`this cell only`** 把那列濾掉 → 取消勾選 → 同樣應該已提交。
7. **舊存檔**：讀一個 07-13 之前的存檔 → 面板列照常出現，label/note 欄是空的（不是亂碼、不是消失）。

**回報**：① 第 1 條主證（點走到底有沒有真的提交、json 對不對）；② 六頁欄位跨存檔留不留得住；③ 第 5 條兩個回歸有沒有中；④ 第 6 條三個入口有沒有丟字；⑤ 匯出的 `removals[]` 混合形狀對不對。

## scene-capture-bridge — `sc ed` numpad 長按持續作用（2026-07-14，**已部署** DLL `c4460315`）

⚠️ **已部署**——完全關遊戲再開。（此 DLL 同時含上一節「面板欄位一致化」，兩節可以一起測。）

1. **長按會動**：`sc ed` 選中一個物件 → **按住 numpad 8 不放** → 物件應**持續前進**，而且**越按越快**（前 0.35 秒不動＝死區，之後由慢加速到快）。4/6/1/3、7/9（旋轉）、+/-（縮放）同理。
2. **單點仍是精準一步**：輕點 numpad 8 一下 → 只移動**一個 step**（Settings 裡的步長），不會多飄——死區就是為了這個。
3. **🔴 單發鍵絕不能連發（最重要的防呆）**：
   - 按住 **numpad 0**（commit）不放 → 應該**只 commit 一次**就離開編輯模式，不會連續 commit／狂跳通知。
   - 按住 **numpad .**（cancel）、**numpad 5**（reset）、**numpad \***（ray select）同理，**都只作用一次**。
   - 按住**動作鍵**（F11 那組，例如 `sc del` 的擦除鍵）→ 應**只擦一個**，不會把整條街連續擦掉。
4. **rotate 模式的 8/2 是還原、不是位移**：`sc ed ax` → 按住 **numpad 8**（roll 還原）→ 應**只還原一次**，不會連續重複；但同模式下按住 **4/6**（yaw）、**1/3**（pitch）、**7/9**（roll）**應該要連續轉**。
5. **縮放不會壞掉**：按住 **numpad -** 很久 → 物件應該縮到很小就**停住**（clamp 0.05），**不會**變成負值/隱形/翻面。
6. **卡頓不瞬移**：長按時開個選單／讓遊戲卡一下再回來 → 物件**不應該**因為那段空窗一次暴衝出去。

**回報**：① 長按有沒有連續動＋加速手感如何（太快/太慢我調 rate）；② 單點還準不準；③ **第 3 條有沒有任何一顆單發鍵變成連發**；④ rotate 模式 8/2 有沒有亂重複。
