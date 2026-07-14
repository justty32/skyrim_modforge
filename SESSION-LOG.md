# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

> **條目格式**：每條只留**一行 open 狀態 + 指向細節的連結**（設計決策/修了什麼落到該工作流或 sub_proj 文件、已落地功能進 [landed](workflows/feature-dev/landed/README.md)、待你驗的進 [WAIT_USER](WAIT_USER.md)）。完成即整條刪除。

## 最新進度

> ### 🎯 現在在哪（2026-07-13）
>
> **2026-07-12 實機全過**：navmesh **P0**＋**T2.0**、referrer **價值證明**＋**DLL 端標記/匯出**、編輯器 **P7/P8**＋**匯出三改**、`sc ed ax` per-axis 還原＋palette load/replace、**`sc capp` 分身臉＝本人**（含 PROTEUS 拿不到的 `tintLayers` 戰紋）——細節都已進 [landed](workflows/feature-dev/landed/README.md)，這裡不重複。
>
> **2026-07-13 做完（離線）**：① 那份沒被消費的 scene json **已 build＋出貨 → `~/skyrim_mods/mine/ModForgeGoblets.zip`**（`noHavokSettle` 的最終驗收；v1 測法失效，當晚改用懸空判別法重出 v2，見下）；② **`Export requires` 整條結案**——跨端對帳一致、假依賴（activeEffects/base）兩端都不污染名單（一變數實驗釘死），驗收記錄進 [phases](workflows/plans/scene-capture-bridge/phases.md)＋[landed/world](workflows/feature-dev/landed/world.md)。
>
> **2026-07-13 晚實機——兩條都 PASS、都結案**：① 🐞 **`isPlayer` ＋玩家 perk ＝ PASS**（`isPlayer: true`、perk 12→26 且是真的單手樹 perk）；隨即依使用者拍板 **(b) 全收**（base ＋ addedPerks 去重，DLL `e19ad4ca`）——**同日也 PASS**（32 ＝ 12＋20 零重疊），消費端要不要過濾留 [backlog](workflows/plans/scene-capture-bridge/backlog.md)。② **`noHavokSettle` ＝ PASS**——但**第一版測法是無效的**（8 顆銀杯 z 全在同一平面＝地板，settle 對貼地靜止物本來就不做事 ⇒ 有無旗標都不動）；改用**懸空判別法** v2（地板上方 128 units、3 顆帶旗標 vs 3 顆不帶）→ 實機 **3 顆浮空 / 3 顆落地** ⇒ 旗標確實 ship 進 esp 並生效。兩條的濃縮句都進 [landed](workflows/feature-dev/landed/README.md)。③ **`sc capp` 數值 ＝ PASS**（練過的角色 level 7 / health 160 / 單手 49 vs 白紙 level 1 / 100 / 15，互為對照組 ⇒ `GetPermanentActorValue` 讀到真實成長）。④ 使用者回報**模式開關/實例附魔/referrer 殘項/跨存檔 reacquire 全過** ⇒ 採集橋只剩**動作鍵 `.ini`** 一項待實機。
>
> **2026-07-14 做完（離線，重構輪）**：**面板欄位一致化**——起點是 07-13 那個 🐞「buffer 與 registry 靜默分叉」，但沒有停在補一個提交路徑：六個面板各抄一份同樣的錯是**結構在漏**，所以把契約收成單一擁有者 **`UI.Fields`**（RULE 1 非編輯中每幀從 registry 回種 ⇒ 面板結構上不可能說謊；RULE 2 Enter／apply／點走都提交）——buffer 失效這個概念整個退場。接著把 **label＋note 補齊到四本 registry ＋ 六頁面板**（co-save `'ERSR'` v3／`'OVRD'` v2／`'SCCP'` v10；palette 的筆記走磁碟 json 所以 `save to file` 帶得走），並依拍板 **(b) 加欄位、非破壞**讓筆記進匯出（`removals[]` **沒寫筆記就還是裸字串**，寫了才變 `{ref, note}` 物件 ⇒ 一般匯出與以前逐位元相同、舊 spec 照讀）。細節見 [phases](workflows/plans/scene-capture-bridge/phases.md)。

> **2026-07-14 晚做完（離線）**：**Browser 目錄 ＋ 世界內 ghost 預覽**（＝CK 的 Object Window，使用者當日提的兩個新方向之一）。**「借用 Skyrim 物品欄 UI」這條路查完否決**——物品欄只吃可攜帶 form type，**山脈/樹/家具（STAT/TREE/FURN）進不了 inventory**，最需要的那類正好不支援。改成：面板開一頁 **Browser**（全 load-order 目錄，搜尋靠**模型路徑**——SSE runtime **沒有 EditorID**），預覽＝**世界本身**（選中即在瞄準點生一個非碰撞、凍結的 ghost，真尺寸真光照）。ghost commit 與 `sc pl` **收斂成同一條擺放路徑**，匯出契約與 ModForge C# 端**零改動**。細節見 [phases](workflows/plans/scene-capture-bridge/phases.md)。

> **下一步（開工第一件事）**：**navmesh P3 add+link**（唯一還需要寫 NAVM 的工作，地基已驗證）——但它有兩個未拍板的問題（§7-3 先只支援內裝？§7-4 要不要引 DotRecast），**動工前先問使用者**。另有使用者 2026-07-14 提的 **`sc ed <xx>` 改物件狀態**（火把亮/滅、門開/關）尚未開工，設計形狀已記在 [backlog](workflows/plans/scene-capture-bridge/backlog.md)。

- **同批已部署待驗（DLL `98c24307`，一顆 DLL 含以下全部）**：① 🆕 **Browser ＋ ghost 預覽**（2026-07-14）——**第一輪實機：目錄/ghost 顯示/跟隨/匯出零外洩/numpad 全 PASS，只有一條 FAIL ⇒ ghost 的碰撞箱留在生成點**（根因：`NiAVObject::SetCollisionLayer` 只碰最上層節點，剛體掛在子節點上；且 havok body 本來就不跟著 `SetPosition` 走）→ 改用 `BSVisit::TraverseScenegraphCollision` 逐一改寫**每顆剛體**的 `collisionFilterInfo` layer bits（po3/BOS 那套），**待重測**；仍要順帶回歸「開著 ghost 存檔→讀檔，ghost 被清掉」；② **面板欄位一致化**（2026-07-14）——主證是「打完字不按 Enter 就點走」現在會真的提交，且**回歸**要看列會不會錯位；③ **`sc ed` numpad 長按持續作用**（2026-07-14）——重點驗「**單發鍵沒有變成連發**」（commit／cancel／select／per-axis revert／動作鍵）；④ **動作鍵改走 `SceneCaptureBridge.ini`**——遊戲內 rebind **兩次實機失敗 ⇒ 整條路移除**（不是隱藏；舊碼在 `ddf6324`），改讀 ini（面板一顆 `reload keys from ini`、ini 贏過 co-save、保留鍵拒收）；⑤ palette **`clear all slots`**（雙重防呆＋`undo clear`）。⚠️ ini 要**跑過一次遊戲**才自動生成。
- **navmesh — 下一步是 P3**（[plans/navmesh.md](workflows/plans/navmesh.md)）：P1 診斷 / T2.0 L_NAVCUT / P0 no-op override **全部做完且實機 PASS**，症狀①結案、`autoNavCuts` 已預設開。**剩**：**P3 add+link**（症狀②「NPC 走不上新平台」，唯一還需要寫 NAVM 的工作，地基已驗證）＋ **P4**（DLL 讀 live navmesh／射線取樣）。原訂 P2 NAVM-cut 備案**整段作廢**。
- **⏳ 三件待使用者拍板**：① **U10**——NAVM 沒有加法合併、後蓋前 ⇒ 我們的 override 會整張蓋掉 USSEP 的修正，要不要做成 build 警告（[navmesh §6 U10](workflows/plans/navmesh.md)）；② **`area:<label>` 前綴**——讓「我就是要一塊區域」可明示，避免 label 誤落 location 槽（[backlog](workflows/plans/scene-capture-bridge/backlog.md)）；③ **`package` 要不要把 `requires.txt` 寫進出貨資料夾**（同 backlog）。另 navmesh plan 內兩項未拍板：§7-3（P3 先只支援內裝？）、§7-4（三角化要不要引 DotRecast，傾向不要）。
- **採集橋殘項——待實機三項：「動作鍵 `.ini` ＋ palette clear」、「面板欄位一致化」、「numpad 長按」**（[wait_todo](wait_todo/ingame-tests.md)；後兩項是 2026-07-14 這輪做的，同一顆 DLL）。其餘（模式開關 py/ed/pkc、實例附魔 ed1、referrer 三項、跨存檔 reacquire、OPEN-A aim-source）**使用者 2026-07-13 回報全過**（口頭驗收、無匯出檔佐證——註記在 [landed/world](workflows/feature-dev/landed/world.md)）。
- **masters 汙染（設計 open，非 bug）**：玩家身上的 spells/effects/inventory 一半來自 mod → esp 把 PROTEUS/XPMSE/nwsFollower… 全變 master。**使用者拍板：完全複製優先、不過濾**；可見性四候選中 (a) build 印來源 ＋ (b) spec `requires:` 契約**已做**，(c) modlist 快照／(d) 依賴檢查指令**未做**（[backlog](workflows/plans/scene-capture-bridge/backlog.md)）。
- **Phase 2 烘焙臉（未排，優先級⬇）**：實測分身臉正常——頭形引擎 runtime 生、臉色 Face Discoloration Fix SE 補；只剩「發佈給無 FDF 環境」或「完全自足產物」才需要烘。三路評估與界線在 [plans/captured-npcs-consumption.md](workflows/plans/captured-npcs-consumption.md)。
- **🔴 鐵律（血的教訓，2026-07-12）**：遊戲跑著時用 `cp` 就地覆寫 `mods/.../SKSE/Plugins/*.dll` → **遊戲無聲暴斃、無 crash log**（Linux 不鎖載入中的 DLL；`cp` 寫穿同一個 inode，而 DLL 程式碼頁是 demand-paged from that file）。**往後部署一律走 `sub_projs/scene-capture-bridge/scripts/deploy.sh`**（`pgrep SkyrimSE.exe` 在跑就拒絕 ＋ tmp+rename 換 inode），不要手打 `cp`。成因記入 [dev-env § 部署 SKSE DLL](workflows/dev-env.md)。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 🧊 身份系統 ③ 聲望/行為追蹤（2026-06-22 冷凍，等很有空再做）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **⚠️ 離線測試套件在真離線機（無 Skyrim.esm）是紅的——11 個測試漏標 `RequiresSkyrim`**（2026-07-09 發現，**待決定修法**）。`Category!=RequiresSkyrim` 跑 861 個 → **850 過、11 敗**：10 個 `LivingNpcTests` + 1 個 `SettlementTests.Build_SleepLocationResolvesToInSpecBedAnchor`。根因：這些測試把 ACHR/marker build 進 **vanilla cell**（Riverwood/Whiterun 旅館等），`TestBuild.Ok`（`tests/…/Helpers.cs:18`）把「master 'Skyrim.esm' not found／vanilla cell unresolved」warning 當失敗。**主力機有 Skyrim.esm → 零 warning → 一直是綠的**，所以從沒被發現；這台 fresh clone 是首次在真離線機上跑才暴露。**影響**：CLAUDE.md 鐵律①「改完跑離線測試」在離線機**目前無法達成**，會擋掉在公司碰原始碼的工作。**修法選項**：①（推薦、convention-correct）照兄弟測試（`NpcPatchTests`/`RemovalsTests`/`MapMarkerTests` 已標）給這 11 個補 `[Trait("Category","RequiresSkyrim")]` → 離線套件轉綠，但損失這些 macro 展開的離線覆蓋；②外科修 `TestBuild.Ok`（偵測 Skyrim.esm 缺席時寬容 master-not-found warning）保留覆蓋，但部分測試斷言可能仍需 Skyrim.esm、且動共用 helper。使用者 2026-07-09 選「先只記錄、回家再決定」。
- **Idea #23 living-adventurers**：**已打包交付（2026-07-11，`~/skyrim_mods/mine/MFLivingNpcs.zip`，TIF 4/4 內聯成功；SKSE header 雷已修、配方記 dev-env）**，剩實機驗收（P0–P3 整鏈第一次 runtime，共同 acceptance gate，四步見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）。設計/進度 → idea [#23](workflows/idea/living-adventurers.md)、sub_proj [README](sub_projs/living-adventurers/README.md) + [design.md](sub_projs/living-adventurers/design.md)。
- **Idea #20 in-world 技能樹**：Phase 0 離線完備 + .pex 已編交付，剩實機驗收——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/roadmap-features.md](wait_todo/roadmap-features.md)。sub_proj [inworld-skill-tree](sub_projs/inworld-skill-tree/README.md)。
- **darksouls-port（DS1 北方不死院 → Skyrim worldspace）**：P1「空殼院」離線完成、`DSPortP1.zip` 已交付（2026-07-06），**剩實機驗收**（進場指令與三段驗收見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）；規劃與 P1 結論 [plan.md](sub_projs/darksouls-port/plan.md)。
- **Idea #19 Godot Worldspace Editor**：整鏈已落地（[landed/world](workflows/feature-dev/landed/world.md) +「Godot 編輯器 WYSIWYG」條 / [godot-editor](workflows/feature-dev/landed/godot-editor.md)），剩非阻塞小尾巴——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/worldspace-editor.md](wait_todo/worldspace-editor.md)。
- **Idea #24 遊戲內編輯器（scene-capture-bridge）**：**生成端＋採集橋整鏈閉環，主線 P1–P8 實機全過**（2026-07-10～07-12）——已落地的一切（marker 系統／橡皮擦／滴管／numpad 編輯／物理凍結／模式制 `sc` 指令／co-save／`overrides[]`／擷取器／referrer／依賴可見性／匯出三改）**全部濃縮在 [landed/world](workflows/feature-dev/landed/world.md)＋[landed/npcs](workflows/feature-dev/landed/npcs.md)**，實作記錄在 [phases](workflows/plans/scene-capture-bridge/phases.md)，本檔不重複。
  - **open 只剩三類**：① **待實機驗**（杯子懸空判別法 v2、數值、py/ed/pkc、referrer 剩三項、ini/palette clear）→ [wait_todo](wait_todo/ingame-tests.md)；② **未做的新想法**（依賴候選 (c)/(d)、紅綠輪廓高亮、marker 寶石下拉、`sc cap` vs `sc pk` 分工）→ [backlog](workflows/plans/scene-capture-bridge/backlog.md)；③ **三件待拍板**（見上方「最新進度」）。
  - **❌ 已否決別再做**：遊戲內 rebind 抓鍵（兩次實機失敗 → 改 `.ini`，理由見 backlog）；PROTEUS 中介（`sc capp` 直接吸玩家已取代）。
