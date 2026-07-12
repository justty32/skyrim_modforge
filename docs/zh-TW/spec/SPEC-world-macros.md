# ModForge spec — in-world 人口 macro（技能樹、聚落、活世界 NPC）

← [index](SPEC-index.md) · cell、放置與地圖標記 → [SPEC-world](SPEC-world.md) · 光照 →
[SPEC-lighting](SPEC-lighting.md)

高階 *macro* spec 區塊，展開成其他每個章節都已涵蓋的低階記錄 —— 一棵可點擊的 in-world 技能樹、
一座帶日程作息的聚落、以及會遊走、被人談論的活世界 NPC。`skillTrees` 與 `settlements` 是純資料
展開（無新 runtime 腳本）；`livingNpcs` 會 ship 兩個可重用的 `.pex`。

## in-world 技能樹（`skillTrees`）

一棵**可點擊的 in-world 養成樹** — 漂浮的星節點，玩家走上前活化以消耗點數、學會 ability，
帶前置 gate 與亮起的視覺回饋。**零外部 mod 依賴**（只 `Skyrim.esm`）；IN-GAME CONFIRMED。
`skillTrees` 是一個高階 *macro*：generator 把它展開成低階記錄（per-node rank globals、共享
points global、節點 + 連線 activators、它們的 placements、以及 `MFSkillNode` 腳本接線）——
就是一棵手刻樹會用到的同一批記錄。

```jsonc
"skillTrees": [
  { "editorId": "MFForgeTree", "name": "Forge Mastery",
    "cell": "Skyrim.esm:0x01605E",                 // 它住在哪（原版室內或 in-spec cell）
    "origin": { "x": -49, "y": -504, "z": 110 },   // ROOT（最底）節點的世界座標
    "spacing": 65,                                  // 垂直間距；65 = 線 mesh 的原生貼合值
    "startingPoints": 3,                            // 玩家起始點數
    "nodes": [                                      // 由下往上 ORDERED；node[i] gate 在 node[i-1]
      { "editorId": "Resolve", "name": "Forged Resolve", "ability": "MFGen_Node0Ability" },
      { "editorId": "Vigor",   "name": "Forged Vigor",   "ability": "MFGen_Node1Ability" },
      { "editorId": "Mastery", "name": "Forged Mastery", "ability": "MFGen_Node2Ability" }
    ] }
],
"assets": "assets/skilltree"                        // 打包星/線 meshes（見下方）
```

遊戲內：玩家活化一個節點 → 若其前置已擁有且有點數可用，該節點的 `ability` 會加給玩家、
星星亮起、連線亮起、扣一點。重新活化已學會的節點、或前置未滿足的節點，會被拒絕並彈出
通知。

**欄位**（`skillTrees[]`）：`editorId`（前綴所有生成的 id）、`name`、`cell`（in-spec 室內
editorId **或**原版 `"<master>:0xFORMID"`）、`origin`（Vec3，root 節點位置）、`spacing`
（預設 65）、`pointsGlobal`（用既有 GLOB 從別處驅動點池 — 空則自動建 `<editorId>_Points`
並以 `startingPoints` 播種）、`startingPoints`（預設 3）、`nodeModel` / `lineModel`
（Data-relative mesh 覆寫）、以及 `nodes`。
**Node**（`nodes[]`）：`editorId`（樹內唯一）、`name`（活化提示 + 通知）、`ability`
（一個 SPEL ref — 通常是 in-spec `spells[]` ability，或原版 — 學會時給）。

**Ability 是你的。** 一個節點參照你在 `spells[]`/`magicEffects[]` 裡定義的 `ability`
（或一個原版 SPEL）。樹驅動*學習 UX*；*效果*是一個普通 ability。

**美術（不需裝 Campfire）。** 預設節點/線 meshes 是 Campfire 的星/線 nifs — 但它們**不是**
master 依賴：把這套件（兩個 `.nif` + 它們全原版的貼圖）經 `assets` 以 loose 檔打包
（提供於 `examples/assets/skilltree`）。覆寫 `nodeModel`/`lineModel` 以使用你自己的 meshes。
`MFSkillNode.pex`（節點行為）會隨 `package` 自動出貨。

**MVP 範圍。** 一條**垂直線性鏈**（節點堆疊，各 gate 在下方那個，由垂直線連接）——
IN-GAME-CONFIRMED 的佈局。分支 / 自由 2-D 佈局是未來擴充（對角連線方向需校準）。
範例：`examples/skill_tree_spec.json`（generator）vs
`examples/inworld_skill_tree_standalone_spec.json`（同結果手刻版）。

## 聚落人口（`settlements`）

一座**住滿活人的聚落** — 具名住民住在一個 cell 裡，各帶綁在已擺放錨點 ref 上的睡/工作/
遊蕩日程作息、可選店家、共享 faction。`settlements` 是一個高階 *macro*（同 `skillTrees`）：
generator 把每個住民展開成既有 build pass 都已處理的低階記錄 —— 一個 ACHR placement、2–3 個
日程 package、faction 成員、以及（店家用的）vendor FACT + 一個放置的 merchant chest。**零新
record 型別、零 runtime 腳本** —— 純資料展開，故離線完全可驗。它把約 100 筆手刻記錄（10 住民 ×
packages + factions + vendors + placements）壓成十幾行。

```jsonc
"settlements": [
  { "editorId": "MFV_Riverwatch",
    "cell": "MFV_RiverwatchInterior",          // in-spec cell editorId 或原版 "<master>:0xFORMID"
    "settlementFaction": "",                    // 空 → 自動建 "<editorId>_Faction"，每個住民加入
    "crimeFaction": "Skyrim.esm:0x0267EA",      // 選填 → 每個住民的 CrimeFaction（城內通行權）
    "friendlyResidents": true,                  // 選填 → 住民兩兩 Friend RELA（預設關）
    "dailyRoutine": {                           // 聚落預設；住民的 `routine` 逐時段覆寫
      "sleep": { "from": 22, "to": 7 },         // 時數 0..24；時段可跨午夜（from > to）
      "work":  { "from": 8,  "to": 18 }
    },
    "residents": [
      { "npc": "MFV_Brelin",                    // ref → 既有 npcs[] editorId（住民）
        "home":    "MFV_BrelinBed",             // ref → 已放置的 bed/marker REFR（Sleep 錨點）
        "work":    "MFV_BrelinForge",           // ref → 已放置的工作站/marker REFR（Work 錨點）；選填
        "spawnAt": "MFV_BrelinSpawn",           // ref → 已放置的 XMarker（ACHR 生在其座標）
        "vendor": { "sellBuyList": "Skyrim.esm:0x06CB48", "notSellBuyList": true,
                    "startHour": 9, "endHour": 18, "gold": 500 } },
      { "npc": "MFV_Millie", "home": "MFV_MillieBed", "spawnAt": "MFV_MillieSpawn",
        "routine": { "sleep": { "from": 21, "to": 6 } } }   // 只覆寫睡眠時段
    ] }
]
```

**每個住民展開成：**一個在 spawn marker 座標（或 `spawnPosition` fallback）、在聚落 cell 裡的
**ACHR placement**；一個 gate 在睡眠時段、錨在 `home` 的 **Sleep package**；一個 gate 在工作
時段、錨在 `work` 的小半徑 **Sandbox「工作」package** —— 只在有給 `work` 錨點時才生；一個
always-on 大半徑 **Sandbox「遊蕩」package**（最低優先序）；以及 faction 成員。有 `vendor` 時，
一個 Vendor-flag FACT（住民加入它，外加引擎的 `JobMerchantFaction`）+ 一個含 `gold` 的放置
merchant chest。Package 依日程時數排序（wander 最後）—— 原版 package 優先序。

**錨點哲學。** `home`/`work`/`spawnAt` 是**你**擺放的 ref 的 editorId（在 Godot 編輯器或
`placements[]`）。macro 只負責把 package **綁**上去 —— 絕不憑空生抽象 sandbox 點（純抽象
sandbox = NPC 呆站）。擺一張床/攤位/marker，給它 editorId，作息就接上去。（Sleep 會主動搜尋
錨點附近的真實床 —— 在一個沒有床家具的空白自訂 cell 裡 NPC 不會躺下；在 `home` 錨點旁擺一張
原版床，或蓋在已有床的原版 cell 裡。）

**欄位**（`settlements[]`）：`editorId`、`cell`、`settlementFaction`（空 → 自動建）、
`crimeFaction`、`dailyRoutine`（`sleep`/`work` 各 `{from,to}` 時數）、`friendlyResidents`
（預設 false）、`residents`。**Resident**（`residents[]`）：`npc`（in-spec npcs[] ref）、
`home`/`work`/`spawnAt`（已放置 ref 的 editorId）、`spawnPosition`（無 `spawnAt` 時的 Vec3
fallback）、`vendor`（`sellBuyList`/`notSellBuyList`/`startHour`/`endHour`/`gold`）、`routine`
（逐住民覆寫）。

**MVP 範圍。** 具名住民 + 靜態 ACHR + 綁錨點作息 + 可選 vendor（確定性、離線可驗那格）。
**Phase 2**（未做）：`crowd:` 匿名群眾（leveled 靜態或 spawn-controller `.pex`）、
`reaction: flee|fight`（需 `flee` PACK 模板）、inline npc、進階逐 weekday/季節作息。
範例：`examples/settlement_spec.json`。

## 活世界 NPC（`livingNpcs`）

一小撮**具名、持久、過著自己離場人生的 NPC** —— 接任務的冒險者、學院學徒、跑商的行商 ——
玩家在世界各處不斷撞見他們，酒館傳唱他們的事蹟。不像 `settlements`（住民錨在單一 cell），
活 NPC 會遊走：引擎無法模擬離場 actor，所以 `livingNpcs` 跑那條經典的**抽象幽靈模擬 + 就地
實體化**迴圈。它是個 macro，展開成一個 controller quest + 每 NPC 的接線，**並 ship 兩個可重用
`.pex`**（所以它**會**帶 runtime 腳本，跟 `settlements` 不同）。**產品的核心是 on-ramp：加一個
既有 archetype 的 NPC = 一個極小 entry —— 一個 ref、一個 archetype、幾個錨點。**

```jsonc
"livingNpcs": {
  "simIntervalHours": 2,                         // 每隔幾遊戲時推進一次離場「戰功」（sim tick）
  "pollInterval": 5,                             // 每幾秒檢查一次在場
  "rumorSpeaker": "MFLN_Bard",                   // 可選：講傳唱的 npc（或 "<master>:0xFORMID"）
  "npcs": [
    { "ref": "MFLN_Kjeld",                       // in-spec npcs[] editorId（放置+forced）或外部 follower "Mod.esp:0xID"（uniqueActor）
      "name": "Kjeld the Wanderer",              // 標 rumor 對話的 prompt
      "archetype": "adventurer",                 // adventurer|mageApprentice|merchant|herbalist|priest|bandit
      "alignment": "friendly",                   // friendly|neutral|hostile（Phase-2 parley；現在先記）
      "backstory": "一個逃離戰爭的傭兵…",
      "anchors": [                               // 他會現身的 vanilla cell；輪替
        { "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 250, "z": 0 }, "kind": "inn" },
        { "cell": "Skyrim.esm:0x01605E", "position": { "x": 250, "y": 120, "z": 0 }, "kind": "inn" }
      ],
      "rumors": [ "聽說 Kjeld 又一個人清了一座墓穴。" ] }
  ]
}
```

**section 展開成：**一個 StartGameEnabled controller quest 掛 `MFLivingWorldController`（單一
game-time tick + 單一 real-time 在場 poll 掃整個 roster —— cost **不**隨 NPC 數線性膨脹）；一個
共享離場 hold marker + 一個共享「在當地 sandbox」package。**每個 NPC 展開成：**controller quest
上一個 reference alias 掛 `MFLivingNpcAlias`（`Archetype`/`HoldMarker`/`Anchors`/`DeedCount`），
forced 填一個放置 ACHR（in-spec）或 `uniqueActor`（外部 follower —— *給那個美麗的 standalone
follower 一條命*）；每個錨點一個 xmarker + 一個 Anchors FormList；一個 deed GlobalVariable；以及
—— 當 section 有 `rumorSpeaker` 且 NPC 有 `rumors` —— 一個 gate 在 deed global（`GetGlobalValue
>= 1`）的傳唱對話。

**運作方式。** 離場時 controller 推進每個 NPC 的 deed global、輪替他「在」哪個錨點 —— 沒有
actor 在跑。玩家進入符合該 NPC 當前錨點的 cell 時，他**唯一**的 persistent ref 被 `MoveTo` 進場
並 `EvaluatePackage`（讓 sandbox package 生效）；玩家離開就送回 hold marker。具名卡司 ⇒ 一人
一個 persistent ref、MoveTo 進出 —— **無 LVLN spawn churn、無重複**。

**archetype = 固定分支**（在 `MFLivingNpcAlias.psc`）。加一個*既有* archetype 的 NPC 是純資料
（多一個 entry）。加一個*全新*生活型態才需擴腳本的 switch（偶爾）。

**玩家互動 & alignment（`interactions`、`alignment`）。** 跟活 NPC 對話可提供互動，每個是一條
調整該 NPC **favor global**（`MFLiving_<tag>_Favor`）的對話 topic —— 那是未來內容據以 gate 的
關係記憶基底。種類：`fund`（給錢，favor +1）、`praise`（誇他的事蹟，+1，gate 在 deed ≥ 1）、
`parley`（緩和 / 嘗試理解，+5 —— 給中立或敵對 NPC）。`alignment`（`friendly`/`neutral`/`hostile`）
會被記錄；**敵對的 in-spec** NPC 被設 `Aggression=Aggressive`（強盜真的會打 —— 把他的錨點放在
營地而非旅館）。外部 follower ref 保留自己的 AI（macro 只改 in-spec NPC）。

**欄位** —— section：`simIntervalHours`、`pollInterval`、`rumorSpeaker`、`npcs`。**livingNpc**：
`ref`（必填）、`name`、`archetype`、`alignment`、`backstory`、`anchors`（≥1 才會現身）、`rumors`、
`interactions`。**anchor**：`cell`（必填）、`position`、`kind`（標籤）。編 `.pex`（與互動的
`setGlobal` TIF fragment）需 Papyrus 機器（`package` 會 ship；build 條件式嵌入）。範例：
`examples/living_npcs_spec.json`。

**MVP 範圍。** 具名卡司 + 抽象模擬 + 就地實體化 + 傳唱 + 互動/favor + alignment。**Phase 3.5+**
（未做）：雇用為隨從、在敵對-交戰中的 NPC 上浮現 parley（需非戰鬥接近機制）、真 missive 任務
目標（卡 roadmap #7–9 LocationAlias fill）、controller 讀 favor/alignment 改行為、LAL 出身 seed
關係、匿名「群像」層。設計：`sub_projs/living-adventurers/`（idea #23 + design.md）。
