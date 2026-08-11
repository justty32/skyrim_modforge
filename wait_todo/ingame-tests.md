# wait_todo — 實機測試（in-game，MO2 / Proton）

← [WAIT_USER](../WAIT_USER.md)（總入口）

機械式 setup／console assertion 可由 `agent-bridge` QA runner 自動跑；需要辨認畫面、體感或對話是否自然的 handoff 仍由你判定。

**怎麼測（通用流程）**
1. **拿 zip**：我把打包好的 zip 放 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根，別有多層；曾因 zip 根殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務鐵律**：對話只在**遊戲 LOAD** 時註冊 → 用全新遊戲或任務啟動後 save+reload（`coc` 不註冊）；既有存檔要 save+reload 才吃 `.seq`；強制天氣 `sw <XX>000800`（XX=load order 槽位 hex，build 會印）；console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK／怪／CTD／空白，附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：手動 patch 進 MO2 mod 夾的檔，從 zip 重裝會復原成 build-time mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

## 待測（active）

- **EFSH effectShaders（2026-08-11，offline 結構已驗）**：拿 `examples/effect_shader.json` 配三張真 `.dds`（fill / particle / palette）build+package，對 actor 套 `MFEff_FireGlow`，目視確認 membrane additive glow、sprite particle 與 fade/key 時序。特別驗「有 palette 可見；拿掉 palette 可能完全不 render」及 inanimate STAT 不發 actor particles。公司機只證 record/wiring，無法代做外觀驗收。

- **Map-scene 座標 profile cube calibration（2026-08-11，pure math 已驗）**：`SceneCoordinates` 已用 `B * R * B^-1` 單測 Unity/Unreal profile；等 `importscene` 第一個 consumer 接好後，用一顆已知朝向、尺寸與座標的 cube 做端到端目視，確認 Skyrim exact handedness/sign、Euler order 與 art-scale fudge。FromSoft profile 在這個實測前刻意不內建猜測。

- **VNML 法線效果（2026-06-16）— 已自驗修正，下面只剩「想看再看」的選配確認**：axis/編碼/尺度已對 vanilla Tamriel LAND 逐 byte 驗過（修了三個 bug，見 SESSION-LOG），不必硬測。新 zip 已交付 `~/skyrim_mods/mine/HeightmapDemo.zip`（FLAT）。**若你某次順手進遊戲**：進 HeightmapDemo worldspace 走坡面，背光側偏暗、向光偏亮、平順漸層即正常——若看到整片黑塊／詭異反光／上下顛倒陰影再回報（理論上不會）。

- **Sofia × VIGILANT 第一幕（2026-06-14）** — 兩版交付 `~/skyrim_mods/mine/`：`SofiaVigilantAct1.zip`（v1 對話+語音）、`SofiaVigilantAct1v2.zip`（v2 +PlayIdle 動作）。spec＝`examples/sofia_vigilant_act1{,_v2}.json`，臺詞＝`../sofia-patch/vigilant-screenplay/act1-警戒者.md`。
  - **✅ v1 核心 pipeline 已實機確認（2026-06-14）**：對話有註冊、觸發點對、語音有播（跑了一小段任務線）。
  - **仍 open（待你續測）**：① **各 beat 完整覆蓋**——把 1-A~1-K 跑滿，看有沒有哪個選項該出現卻沒出現（stage 解碼誤）；② **殺/放分支正確性**（殺女巫=SubQ01 s50 / 放=s230；殺 Carene=GoodEnd s35 / 放=s100——殺了卻跳「放過」台詞＝分支錯）；③ **嘴型**有沒有動（fuz 內嵌 lip，待目視確認）；④ **v2 動作**——換裝 v2（一次只裝一版，editorId 不同），看 1-A 諷刺鼓掌 / 1-E 嘆氣 / 1-H-殺 怒 / 1-I 東張西望 有沒有播。
  - gate 解碼地圖見 `../sofia-patch/vigilant-screenplay/_act1-trigger-placement-map.md`（BSA QF_ 碎片逆向，高信心）。
  - **後續（非待測，待方向確認後我做）**：夢境/更多動作機制位置已定（夢 cell 0x00185C、stage25 進）未實作。

- **Sofia × VIGILANT 第二/三/四幕（2026-06-14）** — 交付 `~/skyrim_mods/mine/SofiaVigilantAct{2,3,4}.zip`（FLAT，語音齊 + setGlobal pex 齊；Act2=34 fuz/11 pex、Act3=51 fuz/14 pex、Act4=16 fuz/13 pex）。spec＝`examples/sofia_vigilant_act{2,3,4}.json`，臺詞＝`../sofia-patch/vigilant-screenplay/act{2,3,4}-*.md`，gate 解碼＝同夾 `_act{2,3,4}-trigger-placement-map.md`。
  - **與 Act 1 唯一差別：沒嘴型**（這批跳過 lip 避免 LipGenerator wine crash 拖死；對話/語音正常，只是嘴不動）。方向確認後可統一補 lip 重打包。
  - 測法同 Act 1（裝在 SofiaFollower+Vigilant 後、save+reload 吃 .seq、跑對應幕的任務、到 beat 對 Sofia 按對話鍵）。回報哪些選項沒出現 / 分支對不對 / 語音正常否。
  - gate 重點：Act2 空牢 0x038524 / 沉船 0x038525 / 血祭母 0x038526；Act3 Child of Oblivion 0x065932；Act4 多數記憶靜默、僅 MeQ01/02/07/Pelinal MeQ10/Molag Bal/Karma 結局有評論。

- **遊戲內場景匯出 · blacksmith 場景（Idea #24 §D，2026-07-08，多輪修正）** — 記錄全驗（dump）；**實機複驗**。`~/skyrim_mods/mine/ModForgeSceneBlacksmith.zip`（**現含 1 個 TIF pex**——openBarter 片段，非純 record 了）。
  - **修正累積**：座標搬白漫馬廄凍原 Z=−4590 + **總共南移 2200** 避開馬廄鑲嵌；鐵匠改新 in-spec NPC **Brynja the Smith**（vanilla unique 不能複製）；交易改 **openBarter「Let me see your wares.」topic**（原本靠 vanilla services faction 不會浮現——沒有通用自動交易對話）。
  - **驗**：地圖 **Forgewatch** marker（白漫馬廄東南更遠）→ 快旅（不摔死）→ 房子 + **Brynja** + 篝火 + 商店。對 Brynja 講話 → 問候「Need something forged?」→ 選單有 **「Let me see your wares.」** → 開交易（鐵匠貨 + 500 金）。
  - **移除物件(§E 橡皮擦)demo**:例子加了 `removals:[0x0D1991]`——白漫馬廄 Skulvar 的一把鋤頭應**消失**(去馬廄看那把鋤頭沒了=橡皮擦成立)。
  - **⚠ 白天測**:vendor 8-20 營業(GetOffersServicesNow 含時間),夜間交易會空——快旅後若是夜晚,`set timescale`/等到白天再試。庫存已放 vanilla 鐵匠 leveled lists(武防+雜貨+金),VendorLocation 錨在店周圍 4096。
  - **回報**:傳送安全否、房子貼地否、Brynja 在否、問候+**交易(有貨有金)**通否。(Brynja 從零建、無 facegen,臉可能陽春/暗臉——能站能講能交易就算過。)

## scene-capture-bridge — ghost ＝ place 模式的擺放游標：**剩兩條**（2026-07-14，**已部署** DLL `c07dd174`）

⚠️ **已部署**——完全關遊戲再開。**不變式**：**ghost 存在 ⟺ `sc pl` ＋ `gh1`（預設開）＋ 有東西被選中**。

**🎮 已 PASS（2026-07-14，使用者「除此之外都很 ok」）**：palette 來源自動出 ghost／換 slot 會跟著換／Browser 點一筆自動切 place 模式並釘住／自動縮到約螢幕 1/9（`numpad 0` 回真實大小）／numpad 轉縮／擺下去帶著角度與大小且 ghost 留著連放。

**還沒驗的兩條：**

1. 🔴 **回歸——`sc ed` 的 numpad 必須完全照舊**：長按連續 ＋ 加速、`sc ed ax` 的 per-axis 還原、**單發鍵不連發**（commit `0`／cancel `.`／select `5`／`*`／動作鍵）。**這輪把長按時鐘抽成共用 `Numpad.h` 給 ghost 一起用，是唯一可能傷到既有功能的地方。**
2. **`gh0` 現在看得見**（上一輪誤報「F11 沒帶到縮放」的根因——其實是 `gh0`）：`sc pl gh0` → 面板 Mode 那行應變成 **`Mode: place [ghost preview: OFF (gh0)]`** ＋ 一行橘字警告「動作鍵會用 slot 自己的大小、而且你看不到」。Settings 頁多了一個 ghost checkbox，勾/取消要與 `sc pl gh1/gh0` 等效。

**順便回報體感**（我照著調）：① 自動縮放的「九分之一」會不會太小（大件如山脈會撞到 0.05 下限）；② numpad 轉/縮的步長順不順手（Settings 頁可調）。

## scene-capture-bridge — 🐞 匯出改「登記簿制」：只匯出我們真的放過的（2026-07-14，**已部署** DLL `c07dd174`）

⚠️ **已部署**（同一顆 DLL）。**為什麼**：匯出器以前的判準是「dynamic ref ＝ 玩家放的」——但**引擎自己也 PlaceAtMe**（魚、蝴蝶、critter marker），生出來的 ref 跟你放的椅子**沒有任何差別**。所以野外匯出會夾帶一堆你沒放的東西。現在：**只有登記簿裡有的才匯出**（`sc pl`／Browser commit 都會自動登記）。

1. **野外重測**（就在你上次那個地方）：擺 1～2 個東西 → 匯出 → **`scene.json` 應該剛好只有你放的那幾筆**，沒有魚、沒有 `0x0C2D47`。log 那行會多印 **`N dynamic refs not ours (engine-spawned…)`**，N 應該就是被擋掉的魚/蝴蝶數。
2. **回歸——舊的東西還在不在**：`sc pk` 滴管 → `sc pl` 擺 → 匯出，照樣要出現；`sc pl py0`（noHavokSettle）與 `ed1`（附魔鑄造）也要照舊。
3. **⚠️ 已知的行為改變（不是 bug）**：**這個改動之前擺下去的東西、以及 console `placeatme` 生的東西，不會再自動匯出**（它們沒有登記簿列）。要救回來 → Palette 頁按 **`adopt dynamic refs in this cell`**。⚠️ 這顆按鈕**不挑食**：魚站在旁邊它就把魚也收編（面板有警告）——所以按完看一下匯出結果。

**回報**：① 匯出檔裡還有沒有你沒放的東西；② adopt 按鈕有沒有把該收的收回來。
