# living-adventurers — 給 standalone follower 一條命

ModForge 消費者專案。設計源 idea [#23](../../workflows/idea/living-adventurers.md)（架構、拍板決策、archetype 框架、風險）。**怎麼做** → [design.md](design.md)（踩在 ModForge 既有機制上的工程設計 + 六階段建造順序）。第一個真 follower enrollment 候選 → [yua-mvp.md](yua-mvp.md)。本檔只記**這個子專案在幹嘛 + 目前進度**。

## 一句話

**人口增加 / 沈浸感增強型** mod：讓天際省**有人氣、有人在活動、是個活的世界**。但人口不是匿名填充——是一小撮**具名、持久、有故事的冒險者**，平時各自過冒險人生（接 missive、清地牢、採資源、送信、打強盜營），玩家在酒館 / 領主廳 / 野外撞見他們，酒館傳唱他們的戰功。

**靈魂**：Nexus 上一堆純好看的 standalone follower 只是站著等招募，浪費了——**首選 cast 來源就是那些既有的 follower mod**，給它們一條命。專案形態＝ patch 生成器（比照 sofia-patch / followers-patch）。

## 核心架構（一句話版，細節進 idea #23）

Skyrim 只跑玩家附近的 AI，所以離場冒險者＝**純資料**（StorageUtil/GLOB），timer 推進；**玩家同地點才把那個常駐唯一 actor `MoveTo` 進場**。具名路線 → 一人一個 persistent ref，進出靠 MoveTo，無 spawn/despawn churn。

## 進度

| 階段 | 狀態 |
|---|---|
| idea #23 設計定稿（卡司=具名 / 玩家=可互動 / 模擬=抽象幽靈）+ design.md 工程設計 | ✅ |
| **spike：證明模擬迴圈**（`spike/`，1 NPC，build 綠） | 🔵 待主力機 package + 實機 |
| **P1：泛化控制器**（`p1/`，2 NPC / 2 archetype，build 綠零警告） | 🔵 待主力機 package（編 .pex）+ 實機；2 個 core 缺口已修（design.md §6）|
| **P2：`livingNpcs:` macro 落地**（core，純離線，845 測綠） | 🟢 落地（「加 NPC = 幾行 JSON」成立）｜example `examples/living_npcs_spec.json`｜待主力機編 .pex + 實機 |
| **P3：玩家互動 + alignment**（core） | 🟢 落地：per-NPC favor GLOB + 互動 dialogue（fund/praise/parley，`setGlobal`）+ alignment（hostile in-spec→Aggressive）｜Phase-3.5 follower handoff 已由 YUA 實機通過：teammate 時停 sim/MoveTo；dismiss 後保持原 follower package 的可見步行，玩家失去她 30 秒且不在 8192 units 載入距離內才由 controller 納管。剩敵對-交戰中浮現 parley、controller 讀 favor/alignment + generic P0–P3 整包實機 |
| 任務層（真 missive 隨機地點） | ⏸ 卡 roadmap #7–9（LocationAlias / nested ReferenceAlias / UpdateCurrentInstanceGlobal） |
| cast 來源接真 standalone follower mod | 🟢 YUA MVP 於 2026-08-10 實機 15/15 PASS：uniqueActor、離場 deeds、materialize、rumor、fund/praise/favor、招募 ownership 與 dismiss 後 off-screen handoff 全過；詳見 [yua-mvp.md](yua-mvp.md) |
| 玩家互動（搶任務 / 雇用 / 資助破壞） | ⏸ 未開 |

## spike（`spike/`）

最小可驗證迴圈——**一個**具名冒險者 Kjeld，**不依賴** roadmap #7–9：

- `living_adventurers_spike.json` — spec
- `MFAdvController.psc` — 雙層控制器（game-time 抽象 tick + real-time 在場 poll，idiom 抄 MFSceneBanterController）

**三個要證明的事**：
1. **離場推進**：Kjeld 在 holding marker 凍結時，`MFLA_DeedCount` 每數遊戲時 +1（沒有 actor 在跑）。
2. **就地實體化**：玩家進 Sleeping Giant Inn → Kjeld `MoveTo` 現身；離開 → 回 holding；再進 → 再現身（無重複）。
3. **酒館傳唱**：吟遊詩人 Bjorn 的 Rumors 對話 gated on `MFLA_DeedCount>=1` → 講 Kjeld 的離場戰功。

**測試**（主力機，新檔或存檔 reload）：`coc RiverwoodSleepingGiantInn` 看 Kjeld 是否現身；`coc Riverwood` 再回看是否重現；等幾遊戲時或 `set MFLA_DeedCount to 3` → 跟 Bjorn 對話看 rumor；`getglobalvalue MFLA_DeedCount` 看離場進度。

跑通 spike → 進 roadmap / spec 展開任務層與真 follower cast。
