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
| **spike：證明模擬迴圈**（`spike/`，1 NPC，build 綠） | ⚪ 過程原型；驗收已由 macro 版 generic 整鏈取代 |
| **P1：泛化控制器**（`p1/`，2 NPC / 2 archetype，build 綠零警告） | ⚪ 過程原型；兩 actor／兩 anchor 已由 macro 版實機驗證 |
| **P2：`livingNpcs:` macro 落地**（core） | ✅ 落地＋實機：`examples/living_npcs_spec.json` 的 generic 包由 agent-bridge 完整跑過；「加 NPC = 幾行 JSON」、兩 actor、兩 archetype、anchor 往返輪替成立 |
| **P3：玩家互動 + alignment**（core） | ✅ generic neutral parley 實機通過（結構化讀選項、選取 TopicInfo、favor 0→5）；YUA Phase-3.5 follower handoff 亦通過。剩新功能方向：敵對-交戰中浮現 parley，或 controller 讀 favor/alignment 改行為 |
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

## Generic runtime acceptance

[`living_npcs.qa.json`](living_npcs.qa.json) 是可重跑的無人值守 acceptance：安裝
`MFLivingNpcs.zip`、確認 Riverwood/Whiterun cell actors、加速兩次 abstract tick、驗
Kjeld 的 anchor 往返輪替、依名字移到 Falas 身旁、開啟並讀取對話、依顯示文字
選 parley，最後確認 TIF 把 `MFLiving_MFLN_Falas_Favor` 從 0 改成 5。2026-08-10
實機結果 **31/31 PASS**，runner teardown 後沒有留下 QA mod。
