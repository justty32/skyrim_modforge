# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就搬進 [phases.md](phases.md)（已落地實作記錄，標日期/DLL crc），從這裡刪除。

---

## ❌ 已否決（別再做第三次）

- **遊戲內 rebind（面板抓鍵改動作鍵）——放棄，改走 `.ini`**（使用者 2026-07-12 實機後拍板：「這太麻煩了，先隱藏掉這個功能吧，我們之後把他擺進 .ini 設定」）。**為什麼不再試**：面板（SKSE Menu Framework）**不暫停遊戲**，所以「抓玩家想綁的那顆鍵」永遠在跟**玩家手上還按著的鍵**搶同一條輸入串流——人剛用滑鼠點完 `Rebind`，手多半還在 WASD 上。**兩次嘗試、兩種設計都在實機失敗**：① P5（2026-07-11）armed 後來者不拒 → 綁成 W；② 重作（`ddf6324`，2026-07-12）加了保留鍵黑名單＋按下再放開才 commit → **使用者實機回報仍失敗**。現況＝**`SceneCaptureBridge.ini`**（SKSE 資料夾、缺檔自動生成、寫鍵名不寫 scancode、面板一顆 `reload keys from ini`）——檔案沒有那條賽道可輸：沒有 armed 狀態、沒有 input sink、沒有時序。實作與完整驗屍見 [phases.md](phases.md) 該兩節；抓鍵狀態機已從 `Modes`／`plugin.cpp` 移除（要看舊碼去 git `ddf6324`）。

## 🐞 已修、已部署、待實機驗

- **`isPlayer` 永遠 false ＋ 玩家 perk 吸不到**（2026-07-12，commit `eb6ae75`，DLL `dd7afd82` 已部署）。**對帳錨點**：使用者已點 Restoration 第一個 perk ＝ `Skyrim.esm:0x0F2CAA`（`RestorationNovice00`），修正前的 `captures_20260712-2250.json` 那 12 個 perk **不含它** ⇒ 驗收＝重吸後 `perks` 要出現 `0x0F2CAA`（二元判斷，不數數量）。`Captures.cpp` 用 `actor->As<RE::PlayerCharacter>()` 判玩家身份——**該 cast 對任何 actor（含玩家）都必定回傳 nullptr**：`TESForm::As<T>()` 是 `switch (GetFormType())`（CommonLibSSE `FormTraits.h`），只肯從 FORM_TYPE 的具體類別**往 base 轉**；玩家 ref 的 form type 是 `kCharacter` → 具體類別 `Character`，而 switch 裡沒有 `PlayerCharacter` case（它沒有自己的 FORM_TYPE）⇒ 向下轉型、`is_convertible` false ⇒ 靜默 null。已改為**單例指標比對** `actor == RE::PlayerCharacter::GetSingleton()`，`isPlayer` 與 perk 路徑一併修好。全 DLL 其餘 4 處 `As<>` 皆為 upcast／formtype 精確命中，安全。**待驗**：`sc capp` 的 log 要印出 `PLAYER`、匯出 json 要有 `"isPlayer": true`，且**點過 perk 後**要吸得到玩家真正的 perk。細節見 [plans/player-capture-capp.md](../player-capture-capp.md) 末節。

## ⏳ 待使用者拍板（2026-07-12 收工時開著）

- **玩家分身要不要保留 base 的 12 顆管線 perk（2026-07-13 新增）**：`isPlayer` 修好後，`Captures.cpp:150-163` 的 if/else 讓玩家**只走 `addedPerks`**（26 顆真 perk），**不再讀** base TESNPC 的 12 顆（`AllowShoutingPerk`／`VampireFeed`／`AlchemySkillBoosts`／`DBWellFitted`…）。那些是 vanilla **Player 記錄專用的管線 perk**（讓玩家能吼、能吸血、技能加成），對一個 NPC 分身多半是死資料——但 `AllowShoutingPerk` 之類若哪天要讓分身用吼聲就會需要。**選項**：(a) 維持現狀（只 added，乾淨）；(b) 合併 base＋added（完全複製優先，與 masters 那條的拍板一致）。**未拍板**。

- **`area:<label>` 前綴（非破壞的中間路）**：referrer 的 slot-kind 教訓（label 進 **SingleRef target 槽**＝鎖定那一個 ref；進 **location 槽**＝只是「那附近一塊區域」，引擎自己挑家具 —— 而且 build 綠、dump 乾淨、零警告，只有進遊戲才看得出來，見 [landed/world](../../feature-dev/landed/world.md)「referrer 原語」）目前只用一行 build INFO 提示。**中間路提案**：讓作者可以寫 `"sandbox.location": "area:sofia's chair"` **明示**「我就是要一塊區域」 → 沒寫 `area:` 而把 label 丟進 location 槽時，提示可以更強硬（甚至報錯）。非破壞（舊 spec 不受影響）。**未拍板**。
- **`package` 要不要把 `requires.txt` 也寫進出貨的 mod 資料夾**：現況＝`build` 寫 `<plugin>.requires.txt` 旁檔、**`package` 只印摘要不寫檔**（理由：輸出夾就是要出貨的 mod，不該多塞檔案，見 [phases](phases.md)）。但玩家最需要那份「我要裝哪些前置」的清單——寫進去（或改寫成 `README.txt`）也許才對。**未拍板**。
- （第三件 **U10**〔NAVM override 後蓋前、要不要做成 build 警告〕住 [plans/navmesh.md](../navmesh.md) §6 U10。）

## 仍未做

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../navmesh.md)——兩個結論同日皆已 🎮 實機 PASS，不再只是離線推論**：① **「擺的東西擋住 NPC」根本不必改 navmesh，已結案**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR——白漫大街 TEST/CONTROL 對照實驗實機證明有效（TEST 繞開、CONTROL 直穿），`autoNavCuts` 已預設開（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 的地基已實機驗證可行**（no-op override 裝上後白漫 NPC 一切正常；離線 byte-diff 早已 IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。**現況：P1/T2.0/P0 全部做完且過關，下一步是 P3 add+link**（原訂的 P2 NAVM-cut 備案因 T2.0 PASS 而整段作廢）；P4 遊戲內採集殿後。
- **外部 mod 依賴——剩下的兩個候選**（(a) 可見性＋(b) 宣告式 `requires:` 契約已做，見 [phases](phases.md)）：**(c) modlist / load order 快照**（把當下 MO2 `plugins.txt` 存進 spec 旁，之後能重現「當時是在哪個 load order 上吸的」）；**(d)「依賴檢查」指令**（給一個 esp ＋ 一份 load order → 回報缺什麼；(b) 檢查的是 spec↔build，這個檢查的是 build↔**玩家的實際安裝**，是出貨前最後一道）。兩者都要讀 MO2/遊戲側檔案，離線機做不了完整迴圈，優先度不高。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
