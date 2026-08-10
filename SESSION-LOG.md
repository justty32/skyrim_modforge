# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

> **條目格式**：每條只留**一行 open 狀態 + 指向細節的連結**（設計決策/修了什麼落到該工作流或 sub_proj 文件、已落地功能進 [landed](workflows/feature-dev/landed/README.md)、待你驗的進 [WAIT_USER](WAIT_USER.md)）。完成即整條刪除。

## 最新進度

> ### 🎯 現在在哪（2026-08-02）
>
> **repo 結構剛做過一次大搬遷**：`sub_projs/` 的十一個子專案全部移出——有程式碼／建置／產物的成了 `projects/` 下的**同層 git repo**（godot-worldspace-editor、scene-capture-bridge、model-converter、agent-bridge、darksouls-port、sofia-patch、skyrim-voicegen、game-data），純文檔的進工作區 `analysis/`（mod-survey、tool-survey、followers-patch）。**stub 也不留**，對照表在 [sub_projs/README](sub_projs/README.md)。C# 側零改動、1013 測綠。跨 repo 連結一律假設各 repo 同層 clone 在 `projects/` 下。
>
> **AI 全自動 mod QA 迴圈已結案**（2026-08-02，計畫在工作區 `workflows/plans/ai-ingame-qa-loop.md`）：`agent-bridge` 的 DLL + mo2ctl + qa.json runner + MCP server 四件實機驗完。**首跑就抓到本 repo 一個真 bug 並修掉**——CELL override 沒保留 EDID 導致 runtime 那個 cell 變無名（`eb0bb6c`），而且是用這條迴圈自己驗證修好的。
>
> **下一步（開工第一件事）**：**先跑完那三條實機**（[wait_todo](wait_todo/ingame-tests.md)：`gh0` 標示、🔴 **`sc ed` numpad 回歸**〔長按時鐘抽成共用 `Numpad.h`，唯一可能傷到既有功能之處〕、登記簿制野外匯出）。之後兩條主線任選：**navmesh P3 add+link**（有兩個未拍板問題，動工前先問）或使用者 2026-07-14 提的 **`sc ed <xx>` 改物件狀態**（火把亮/滅、門開/關——設計形狀已在 [backlog](../scene-capture-bridge/README.md) 對應的 plans/backlog，難點是每個屬性的引擎真相要逐個解碼）。
>
> ⚠️ 採集橋的**程式碼現在在 [`../scene-capture-bridge`](../scene-capture-bridge/README.md)**（同層 repo），但它的**計畫／驗收／契約文檔仍在本 repo**（`workflows/plans/scene-capture-bridge/`、`workflows/specs/ingame-scene-export-design.md`、`wait_todo/ingame-tests.md`）。

- **🐞 匯出器把「引擎自己生的」當成「玩家放的」——已改登記簿制（2026-07-14，已部署 DLL `c07dd174`，待實機）**：野外匯出 10 筆 placements 裡**只有 1 筆是使用者放的**，其餘是釣魚 CC 的魚 ×3 ＋ `DoNotPlaceSmallCritterLandingMarkerHelper` ×6（蝴蝶降落 marker）。根因＝vanilla diff 的判準「dynamic ref ＝ 玩家放的」**對引擎生的 ref 同樣為真**（引擎自己也 PlaceAtMe），這個啟發式分辨不了、也永遠分辨不了。**一直都在**，只是以前都在室內測（旅館裡沒有魚）。修法（使用者拍板）＝**所有權從推導改成記錄**：每筆 `sc pl`／ghost commit 都進 placed 登記簿，匯出器**沒登記簿列就不匯出**，另給面板一顆 `adopt dynamic refs in this cell`（明示優於推導）。⚠️ **行為改變**：登記簿制之前擺的、以及 console `placeatme` 生的，**不再自動匯出**（要按 adopt）。細節見 [phases](workflows/plans/scene-capture-bridge/phases.md)。
- **採集橋——待實機只剩三條**（[wait_todo](wait_todo/ingame-tests.md)，同一顆 DLL `c07dd174`）：① 🔴 **`sc ed` numpad 回歸**（長按/加速/`sc ed ax`/單發鍵不連發——本輪把長按時鐘抽成共用 `Numpad.h` 給 ghost 一起用，**唯一可能傷到既有功能之處**）；② **`gh0` 可見性**（Mode 那行 ＋ Settings checkbox）；③ **登記簿制野外匯出**（`scene.json` 應剛好只有你放的，log 多印 `N dynamic refs not ours`）。另有兩個體感待回報：自動縮放的「九分之一」會不會太小（大件會撞到 0.05 下限）、numpad 轉/縮步長順不順手。
- **其餘採集橋功能**（模式開關 py/ed/pkc、實例附魔 ed1、referrer、跨存檔 reacquire、面板欄位一致化、numpad 長按、動作鍵 `.ini` ＋ palette clear）**使用者已回報通過**，濃縮句在 [landed](workflows/feature-dev/landed/README.md)。
- **navmesh — 下一步是 P3**（[plans/navmesh.md](workflows/plans/navmesh.md)）：P1 診斷 / T2.0 L_NAVCUT / P0 no-op override **全部做完且實機 PASS**，症狀①結案、`autoNavCuts` 已預設開。**剩**：**P3 add+link**（症狀②「NPC 走不上新平台」，唯一還需要寫 NAVM 的工作，地基已驗證）＋ **P4**（DLL 讀 live navmesh／射線取樣）。原訂 P2 NAVM-cut 備案**整段作廢**。
- **⏳ 原三件待拍板已全部拍板＋落地（2026-07-29，公司離線機一輪）**：① **U10** build 警告（`CheckNavmeshOverrideClobbers`，合成 plugin 離線測、🎮 對真 USSEP 驗收留主力機，見 [navmesh §6 U10](workflows/plans/navmesh.md)）；② **`area:<ref>` 前綴**（location 槽明示區域意圖、靜音護欄）；③ **`package` 寫玩家面向 `REQUIREMENTS.txt`**。三件都已離線測並 commit（2026-08-02 已 push）。**這條唯一還 open 的**是 U10 對真 USSEP 的主力機驗收；另 navmesh plan 內兩項仍未拍板：§7-3（P3 先只支援內裝？）、§7-4（三角化要不要引 DotRecast，傾向不要）。
- **masters 汙染（設計 open，非 bug）**：玩家身上的 spells/effects/inventory 一半來自 mod → esp 把 PROTEUS/XPMSE/nwsFollower… 全變 master。**使用者拍板：完全複製優先、不過濾**；可見性四候選中 (a) build 印來源 ＋ (b) spec `requires:` 契約**已做**，(c) modlist 快照／(d) 依賴檢查指令**未做**（[backlog](workflows/plans/scene-capture-bridge/backlog.md)）。
- **Phase 2 烘焙臉（未排，優先級⬇）**：實測分身臉正常——頭形引擎 runtime 生、臉色 Face Discoloration Fix SE 補；只剩「發佈給無 FDF 環境」或「完全自足產物」才需要烘。三路評估與界線在 [plans/captured-npcs-consumption.md](workflows/plans/captured-npcs-consumption.md)。
- **🔴 鐵律（血的教訓，2026-07-12）**：遊戲跑著時用 `cp` 就地覆寫 `mods/.../SKSE/Plugins/*.dll` → **遊戲無聲暴斃、無 crash log**（Linux 不鎖載入中的 DLL；`cp` 寫穿同一個 inode，而 DLL 程式碼頁是 demand-paged from that file）。**往後部署一律走 `../scene-capture-bridge/scripts/deploy.sh`**（`pgrep SkyrimSE.exe` 在跑就拒絕 ＋ tmp+rename 換 inode），不要手打 `cp`。成因記入 [dev-env § 部署 SKSE DLL](workflows/dev-env.md)。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 🧊 身份系統 ③ 聲望/行為追蹤（2026-06-22 冷凍，等很有空再做）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #23 living-adventurers**：**YUA 真外部 follower enrollment 於 2026-08-10 實機 15/15 PASS**：plugin/uniqueActor、單一 YUA + Mira、離場 deed、Bannered Mare materialize、rumor、fund/praise/favor、招募期間 teammate ownership 全過。第一輪 QA 抓到 dismiss 後一出門即被 controller `MoveTo`、玩家看見瞬間消失；修成沿用 follower package 可見步行，玩家已無法跟上且連續 30 秒不在 8192 units 載入距離內才 off-screen reclaim，第二輪通過。`~/skyrim_mods/mine/MFLivingYUA.zip` 與 generic `MFLivingNpcs.zip` 均含此版；YUA 不再列 WAIT_USER。Generic P0–P3 兩 NPC acceptance 仍待測，四步見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)。設計/進度 → [README](sub_projs/living-adventurers/README.md)、[design](sub_projs/living-adventurers/design.md)、[YUA MVP](sub_projs/living-adventurers/yua-mvp.md)。
- **Idea #20 in-world 技能樹**：Phase 0 離線完備 + .pex 已編交付，剩實機驗收——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/roadmap-features.md](wait_todo/roadmap-features.md)。sub_proj [inworld-skill-tree](sub_projs/inworld-skill-tree/README.md)。
- **darksouls-port（DS1 北方不死院 → Skyrim worldspace）**：P1「空殼院」離線完成、`DSPortP1.zip` 已交付（2026-07-06），**剩實機驗收**（進場指令與三段驗收見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）；規劃與 P1 結論 [plan.md](../darksouls-port/plan.md)。
- **Idea #19 Godot Worldspace Editor**：整鏈已落地（[landed/world](workflows/feature-dev/landed/world.md) +「Godot 編輯器 WYSIWYG」條 / [godot-editor](workflows/feature-dev/landed/godot-editor.md)），剩非阻塞小尾巴——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/worldspace-editor.md](wait_todo/worldspace-editor.md)。
- **Idea #24 遊戲內編輯器（scene-capture-bridge）**：**生成端＋採集橋整鏈閉環，主線 P1–P8 實機全過**（2026-07-10～07-12）——已落地的一切（marker 系統／橡皮擦／滴管／numpad 編輯／物理凍結／模式制 `sc` 指令／co-save／`overrides[]`／擷取器／referrer／依賴可見性／匯出三改）**全部濃縮在 [landed/world](workflows/feature-dev/landed/world.md)＋[landed/npcs](workflows/feature-dev/landed/npcs.md)**，實作記錄在 [phases](workflows/plans/scene-capture-bridge/phases.md)，本檔不重複。
  - **2026-07-14 再加一輪：Browser 目錄 ＋ ghost 預覽（本體實機結案）＋ 匯出登記簿制**——同樣濃縮在 [landed/world](workflows/feature-dev/landed/world.md)。
  - **open 只剩三類**：① **待實機驗**（三條，見上方「最新進度」）→ [wait_todo](wait_todo/ingame-tests.md)；② **未做的新想法**（`sc ed <xx>` 改物件狀態、面板內 3D 預覽 spike、離線 catalog json、依賴候選 (c)/(d)、紅綠輪廓高亮、marker 寶石下拉）→ [backlog](workflows/plans/scene-capture-bridge/backlog.md)；③ **三件待拍板**（見上方「最新進度」）。
  - **❌ 已否決別再做**：遊戲內 rebind 抓鍵（兩次實機失敗 → 改 `.ini`）；PROTEUS 中介（`sc capp` 直接吸玩家已取代）；**借用 Skyrim 物品欄 UI 做物件目錄**（物品欄只吃可攜帶 form type，山脈/樹/家具根本進不了 inventory → 已改 Browser 面板頁，理由見 [landed/world](workflows/feature-dev/landed/world.md)）；**事後剝除 ghost 的碰撞**（兩種都失敗 → 改在 3D 建起來前 `SetCollision(false)`，同上）。
