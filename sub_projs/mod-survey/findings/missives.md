# Mod Survey — Missives (v2.03 SSE)

對象：`Missives.esp`（Nexus 17576）。本文從 ModForge（JSON spec → `.esp` 生成器）角度拆解這顆高評價的 **radiant quest / 公告板** mod 怎麼運作、哪些機制可生成 / 需新支援 / 純參考。

> 記錄方法：只用 ModForge lazy CLI verb（streamed/overlay-safe）跑這顆 esp，沒有整顆載入 Skyrim.esm。

---

## 1. 這個 mod 做什麼 + 怎麼運作

Missives 在各城鎮放置一塊**公告板（Missive Board）**，板上不定期刷出一疊**告示（missive，BOOK 道具）**。玩家拿走一張 missive → 對應的 **radiant quest 自動啟動**：到隨機地點（leveled）幹活（殺賞金頭目 / 取物 / 採集 / 送信 / 追捕逃犯）→ 回報領賞（金幣 + leveled 物品）。

關鍵架構結論：**這顆 mod 完全沒有用 Story Manager**（`smtree`/`dump` 沒有任何 `StoryManagerEventNode`/`SMEN`/`SMBN`/`SMQN`，quest 全是 `type=Misc`、`event=` 空）。**radiant 行為的本體是「引擎的 quest-alias Find-matching-conditions 填充系統」**，由 CK 裡 quest record 的 alias 定義驅動；Papyrus 只負責「板子上刷 quest」「objective 推進」「結算發獎/清 missive」這些膠水。換句話說 Missives = **一個 Activator 控制器 + 265 顆預先寫死的 radiant quest 模板**，沒有中央 controller quest，沒有 SM 子樹。

record census（`dump`）說明性質：

| 記錄類型 | 數量 | 角色 |
|---|---|---|
| `Quest` | 265 | radiant 任務模板＝**9 holds × 約 7 job-family × tier** 的笛卡兒積 |
| `DialogTopic`/`DialogResponses`/`DialogBranch` | 各 426 | 每顆 quest 一條「回報領賞」對話 topic（player line，給 radiant 填出來的 QuestGiver/Steward） |
| `GlobalShort` | 249 | per-job-type × per-tier 的「目標數量 min/max」「reward 金幣」「current count」計數器 |
| `GlobalFloat` | 95 | 刷新機率（Low/Med/High/VeryHigh chance）、refresh rate、courier 期限 |
| `Package` | 28 | 追捕型逃犯（Thief/Vampire/Fugitive）NPC 的行為包 |
| `FormList` | 38 | **9 holds × 4 tier = 36 個 quest 池** + `_M_ListLocationsForbidden` + `_M_ListPeopleForbidden` |
| `LeveledItem` | 32 | 兩用：`_M_LItemItem*`＝採集/取物的目標物品；`_M_LItemReward*`＝結算發的獎勵 |
| `Book` | 26 | missive 告示本身（每 job-type 一種，標題用 alias 代入） |
| `LeveledNpc` | 3 | `_M_LCharThief` / `_M_LCharVampire` / `_M_LCharFugitive`（追捕目標） |
| `Message` | 11 | 動態改寫任務物品的顯示名（`<Alias.ShortName=QuestGiver>'s <BaseName>`） |
| `Activator`/`Container` | 各 1 | `_M_ActivatorBoard`（觸發器）＋ `_M_MissiveBoard`（裝 missive 的容器） |
| `Cell`/`PlacedObject`/`Worldspace` | 21/52/6 | 各城鎮放板子的擺件 |

`gamedata` 報 `dialogue_lines=0` 是因為這些 topic 是 player 主動講的領賞句、不是 NPC response 體（diag census 只算後者）。`scnscan` 無 scene。

### 公告板 → missive → radiant 目標 → 回報 的完整鏈

1. **刷 quest（板子控制器）**：`_M_ActivatorBoard` 上的 `_M_ActivatorScript`（extends ObjectReference）`OnTriggerEnter`：玩家走近且距上次刷新超過 `RefreshRate` 天，就對 4 個 tier 各跑一次 `UpdateQuests(chance, FormList)`：遍歷該池每顆 quest，`Utility.RandomInt() < chance` 就 `MissiveQuest.Start()`；若 quest 已在跑但玩家還沒接（`GetStage()==0`）就 `SetStage(110)` 收掉（讓位給新的）。**這就是「板子刷新」的全部**——機率 roll + `Quest.Start()`。
2. **接任務（Start 自動填 alias + 投放 missive）**：`Quest.Start()` 觸發引擎的 alias 填充（見 §2）：Location alias 依 hold 條件挑一個隨機地點，nested Reference alias 在該地點裡找箱子/頭目/物品，並把 missive BOOK（`Alias_Missive`）放進板子容器。玩家把 missive 收進背包 → `_M_AliasMissiveScript.OnContainerChanged` → `SetStage(20)`（任務正式開始、objective 顯示）。
3. **做任務（alias 事件推進 objective）**：各 job-type 的目標達成靠掛在 alias 上的 ReferenceAlias 腳本，而非中央輪詢：
   - **Kill/Retrieve 頭目**：`_M_AliasBossScript.OnDeath` → `SetStage(Stage)`。
   - **取物/送信**：`_M_AliasItemScript` / `_M_AliasDeliveryScript.OnContainerChanged` → 物品進玩家背包就完成 objective、離開就退回。
   - **採集**：quest fragment 用 `Game.GetPlayer().GetItemCount(...)` 對 `ItemTotal`（一個 `Utility.RandomInt(min,max)` 決定的隨機數量）比對。
   - **送信期限**：`_M_AliasPlayerCourier.OnUpdateGameTime`（每 6 遊戲小時）比對 `GameDaysPassed > DeliveryDate`，逾期 `SetStage(103)`（失敗）。
4. **回報領賞**：玩家對 radiant 填出來的 QuestGiver/Steward/Jarl 講該 quest 的 `*RewardTopic` 對話 → quest fragment `CompleteAllObjectives()` + `Player.AddItem(Gold001, GoldReward)` + 採集/送信型再 `AddItem(Reward)`（LVLI）→ `SetStage(110)` → fragment 把 missive 從板子或玩家身上移除、`Stop()`。

比重：**控制器 Papyrus 極輕**（一個 Activator 腳本 roll 機率），**任務膠水 Papyrus 中等**（每 job-type 一個 quest fragment 腳本 + 幾個 alias 事件腳本，都只做 objective/stage/發獎），**radiant variety 100% 靠引擎 alias 填充 + 預先寫死的 265 顆模板**。沒有 SM、沒有 controller quest。

---

## 2. 關鍵 record 與模式（重點：radiant quest 的可生成結構）

### 2a. quest 模板的笛卡兒積 + tiered FormList 池

EditorID 本身就是生成表：`_M_Quest<Hold><JobFamily><Variant><Tier>`，例如 `_M_QuestWhiterunKillBandit`、`_M_QuestRiftGatherOreVeryHigh`、`_M_QuestEastmarchCourierLetterHigh`。

- **9 holds**：Whiterun / Eastmarch / Falkreath / Haafingar / Hjaalmarch / Pale / Reach / Rift / Winterhold（各約 29–30 顆）。
- **job families**：Kill（Bandit/Animal/Dragon/Giant/Forsworn）、Retrieve（Wilderness/Ruins/Hideout）、Gather（Ingr/Ore/SoulGem/Inn 各 Low/Med/High[/VeryHigh]）、Courier（Letter/Weapon/Potion 各 Low/Med/High）、Track（Thief/Vampire/Fugitive）。
- **4 個難度 tier**：`_M_ListQuests<Hold><Tier>`（Low/Med/High/VeryHigh）＝ FormList 池。板子用 4 個 `QuestChance*` global 控各 tier 出現率。

**每塊板子（每個 hold）綁該 hold 的 4 個 FormList**；同一顆 quest「下次再接」靠 `Start()` 重填 alias，所以模板可重複使用。

### 2b. quest 模板的內部骨架（`questdiag`）

所有模板共用同一套 stage：`0=StartUpStage / 20=接取 / (30,40=取物/送達) / 100=Complete / 105=Fail / 110=ShutDown`。objective display text 大量用 alias token：

```
Kill:     obj20 "Kill the Leader of <Alias=Dungeon>"  obj40/41 "Collect bounty from <Alias=Steward>/<Alias=Jarl>"
Retrieve: obj20 "Retrieve <Alias=Item>"               obj40 "Return <Alias=Item>"
Gather:   obj20 "Gather <Alias=Item> (<Global=Count>/<Global=Total>)"  obj40 "Bring ... to <Alias=QuestGiver>"
Courier:  obj20 "Collect <Alias=Item>" obj30 "Recover <Alias=Item>" obj40 "Deliver ... by <Global.Day=Time>"
Track:    obj10 "Find the Thief in <Alias=OtherHold> and Retrieve <Alias=Item>"
```

注意 obj40/41 的雙軌（有 Steward 走 40、沒有走 41 領 Jarl）＝靠 quest fragment `if(Alias_Steward.GetRef())` 分流。Gather 的數量顯示靠 `<Global=...Count>/<Global=...Total>` 兩個計數 global 即時更新。

### 2c. alias fill 模式（這是 radiant variety 的真正引擎）

從各 quest 腳本的 alias property 宣告，可逆推出每顆模板的 alias 套組（fill 模式存在 quest record 的 alias 定義裡，由 `Quest.Start()` 時引擎執行）：

- **`LocationAlias Alias_Hold`**：用 keyword 條件（hold location type）挑中**這個 hold**——這就是「Whiterun 板子只給 Whiterun 任務」的根。
- **`LocationAlias Alias_Dungeon` / `Alias_Inn` / `Alias_Destination` / `Alias_City`**：在 `Hold`（或排除 forbidden）範圍內**Find matching location**（依 location-type keyword：dungeon / inn / city…）隨機挑一個合法地點＝目標地點的隨機化。
- **nested `ReferenceAlias`（Find in alias）**：在已填好的 `Dungeon` 裡找 ——
  - `Alias_Steward`/`Alias_Jarl`/`Alias_QuestGiver`/`Alias_recipient`＝在城鎮 location 裡找特定 ref / unique actor（領賞對象）；
  - `Alias_chest`/`Alias_Item`＝在 dungeon 裡找一個容器，再把 LVLI 目標物投進去（取物型）；
  - `Alias_target`/`Alias_Thief`＝Create Reference to a LeveledNpc（`_M_LCharThief` 等）或 Find boss ref（殺/追捕型）。
- **`_M_ListLocationsForbidden` / `_M_ListPeopleForbidden`**：alias 條件的排除清單，避免挑到不該用的地點/人。
- **追捕型的跨 hold**：`Alias_OtherHold` 另填一個**不同**的 hold，逃犯 NPC（LVLN）由 Papyrus `Enable()` + 投物到該 hold 的 inn marker（`Alias_Inn1/2`、`Alias_InnMarker1/2`），追到殺掉取回失物。

**結論：variety = (a) 模板把 job-type 寫死，(b) Location alias 用 keyword/forbidden-list 隨機挑地點，(c) nested Reference alias 在那地點裡 Find/Create 出箱子・頭目・領賞人・LVLN 目標。** 完全是引擎原生 radiant 機制，跑時零 Papyrus 介入填充。

### 2d. 物品與獎勵的 LVLI 雙用

- **目標物品**＝`_M_LItemItem*`（Ingr/Ore/SoulGem/Inn/Jewelry/Heirloom/Armor/BookSkills/Potion…）：填進 dungeon 箱子或當採集目標，由 tier 決定稀有度。
- **獎勵**＝`_M_LItemReward*`：結算時 `AddItem(Reward)`。金幣另由 `GoldReward` global 給。
- **動態命名**：`Message` record（如 `_M_MessageItemRetrieve "<Alias.ShortName=QuestGiver>'s <BaseName>"`）把通用 LVLI 物品改寫成「某人的傳家寶」這種具體名，增強敘事而不需做獨立 record。

### 2e. missive BOOK 與板子 Container/Activator

- **Book**（`_M_Missive*`）＝告示，標題 `"Missive: Kill the Leader of <Alias=Dungeon>"` 用 alias token 代入；填進 `_M_MissiveBoard`（Container）。
- **Activator + Container 分工**：`_M_ActivatorBoard`（隱形 trigger box，掛刷新腳本）+ `_M_MissiveBoard`（玩家能開的容器，裝 missive）。刷新時 `BlockActivation(true/false)` 鎖住容器避免並發。
- **領賞對話**：每顆 quest 一條 `_M_Quest...RewardTopic`（DialogTopic，player line），條件綁該 quest 在跑 + objective 狀態，講給 radiant 填出的領賞 alias。

---

## 3. 對 ModForge 的參考價值

Missives 是「**radiant quest 工廠**」的純粹樣本——它把本輪多份 survey 反覆指出的同一個缺口推到最高優先：**ModForge 目前不能生成 quest 的 alias 與 fill 模式**，而那正是所有 radiant 內容的命脈。

### 標記：可生成 / 需新支援 / 純參考

**需新支援（高優先，Missives 強烈強化）**
- **Quest alias 定義 + 全套 fill 模式**：`LocationAlias`（Find matching location by keyword / forbidden FormList）、`ReferenceAlias`（Specific Reference / Find in alias <X> / Create Reference to LVLN / Unique Actor）、nested alias（alias 之間的 from-alias 間接 target）。這是 ModForge 生成任何 radiant quest 的**前置必備**，目前缺。Missives 把它從「nice to have」抬成「核心」。
- **Quest objective → target alias 綁定**：objective 指向 alias（不是寫死 ref）、display text 內嵌 `<Alias=...>`/`<Global=...>`/`<Global.Day=...>` token。
- **FLST 建立 + 引用**：tiered quest 池（`_M_ListQuests<Hold><Tier>`）與 forbidden 清單都是 FormList；這跟 extended-encounters 等 survey 的「需要能建 FLST 並讓別的 record 引用」缺口完全一致——**Missives 再次確認 FLST 建立應排進近期**。
- **LVLN alias fill**：把 LeveledNpc 經 alias「Create Reference to」實體化成可追蹤目標（Track 型），也是共通缺口。
- **GlobalVariable 量產 + script property 連線**：249 個 GlobalShort 當計數器/獎勵額；ModForge 需要能批次建 global 並接到 quest fragment script property 與 objective token。

**可生成 / 接近可生成（用 ModForge 既有能力）**
- **Quest stage + fragment 腳本骨架**：stage flags（StartUp/Complete/Fail）、objective display/complete、`AddItem` 發獎——這層膠水 Papyrus 跟既有 dispatcher/fragment 模式同類，可生成。參見 memory `sm-quest-journal-progression`（startUpStage / WireQuestStages 合併 adapter，不要 clobber alias 腳本）。
- **Book / Message / LeveledItem / Container / Activator** record：都是 ModForge 已能造的基本型；Activator 掛 `extends ObjectReference` 的觸發腳本也走既有 §1 path。
- **動態物品命名 Message**：純 record + token，可生成。

**純參考（架構啟發，不必照抄）**
- **「不用 SM、用 Activator 觸發 + 預生 quest 池」是一條替代路線**：對比 extended-encounters 走 SM additive-parent（memory `story-manager-kill-recipe`），Missives 證明 **radiant 不一定要 SM**——若目標地點固定（板子在城裡）、靠玩家走近觸發，純 Activator+FormList+`Quest.Start()` 更簡單。ModForge 兩條路都該支援，視內容型態選。
- **笛卡兒積生成法**（hold × job × tier 把 265 顆模板鋪開）對 JSON-spec 生成器是天作之合：**spec 寫一個 job-family 模板 + 一張 hold/tier 矩陣，生成器展開成 N 顆 quest + 對應 FormList**，正是 ModForge「JSON → 大量 record」想要的形態。

### 跟既有 memory / 筆記的連結
- `story-manager-kill-recipe`、`sm-quest-journal-progression`：Missives 是「**不走 SM**」的對照組；但 quest stage/objective 推進的 journal 規則共用。
- `programmatic-navmesh`：Missives 的目標投放靠引擎在既有 vanilla 地點的 alias fill，**不自造 navmesh**（跟 extended-encounters 的 `MoveTo`+NavmeshTester 不同）——若 ModForge 生成的 radiant 任務目標落在 vanilla 地點，navmesh 不是問題；落在自製 worldspace 才需 programmatic navmesh。
- 共通缺口彙整（跨本輪 survey）：**FLST 建立、LVLN alias fill、SM 子樹（此顆不需要但別的需要）、alias 間接 target** ——Missives 命中其中三項（FLST、LVLN alias fill、alias 間接 target），**把「quest alias fill 系統」確立為 radiant 生成的第一順位前置工程**。
