# 多重身份系統(輕量職業/角色)設計

> 日期:2026-06-06 · 狀態:**已 brainstorm 完成,封存待 round 2**
> 相依前置:**PlayIdle scene-action**(`workflows/specs/2026-06-06-...-playidle-design.md`,先獨立一輪)必須先落地——聖騎士宣誓演出的下跪/祈禱動畫靠它。
> 本文件只是設計成果,**不含實作**;PlayIdle 落地後再單獨跑 spec 自審 → plan → 實作。

## 為什麼要

NPC 預設把玩家當龍裔,但玩家想扮演別的角色(聖騎士、商人…),像 vanilla 加入盜賊工會後某些對話會變。延伸出去,這其實是個**輕量「職業/身份」系統**:做某些事 → 取得身份 → 身份賦予技能/加成 + 解鎖身份專屬互動 → 那些互動又回頭強化身份。一個良性循環(近似 D&D 職業)。

## 範圍邊界(本系統 = 子專案 B)

- **當前**:身份只影響**我方/新增 NPC** + Sofia(master 引用)+ 我們新寫的對話。**原版 NPC 不改口**。
- **很後面**:讓原版 NPC 也按身份改口(需 override 原版 INFO 或掛 Story Manager,有相容性風險)。地基為此預留——**身份狀態存成 faction**,未來原版對話用 `GetInFaction` 就 gate 得到,不必重做。

## 核心模型:三面向

| 面向 | 意義 | 落在哪 |
|------|------|--------|
| **Acquire 取得** | 怎麼獲得/失去身份 | BOOK `OnRead` + MessageBox + 選用 scene + fragment |
| **Gate 閘門** | 解鎖哪些對話/scene/行為 | CTDA 條件(`identity`/`primaryIdentity` 標籤) |
| **Grant 賦予** | 給玩家的技能/常駐加成 | SPEL/MGEF/PERK(加入時給、移除時收) |

**資料模型**:可疊加身份(各自獨立持有)+ 一個「當前主身份」(決定 NPC 預設稱呼/招呼)。主身份用**優先序自動解析,純資料、無 controller script**。

## 資料表示

每個身份一個 `faction`(持久持有訊號)+ `priority` + 選用欄位:

```jsonc
"identities": [
  {
    "id": "Paladin", "faction": "MF_IdentityPaladin", "priority": 30,
    "grants": ["Abil_SmiteEvil"],                 // 加入時給的常駐 ability,移除時自動收
    "onAcquire": { "scene": "PaladinOathScene" }  // 選用:取得時播的演出
  },
  { "id": "Merchant",   "faction": "MF_IdentityMerchant",   "priority": 20, "toggle": true },
  { "id": "Adventurer", "faction": "MF_IdentityAdventurer", "priority": 0,  "default": true }
]
```

- `faction` 可引用原版/Sofia 既有 faction(如盜賊工會),或 build 自製 FACT。
- `default: true`:所有玩家預設持有(冒險者);`toggle: true`:同一入口可解除(商人)。

## build 展開規則(純 CTDA)

- `identity: Paladin`(內容標籤)→ `GetInFaction(Paladin.faction) ≥ 1`(+ 未來 `activeWhen` 情境條件)。
- `primaryIdentity: Paladin`(招呼語標籤)→ 上述 + 對每個 `priority` 更高的身份 J 補 `GetInFaction(J.faction) == 0`(排除)。確保只有最高優先序的「主」招呼會 fire。
- `grantsIdentity: X` / `removesIdentity: X`(語法糖,放在對話選項或任務階段/書本)→ 自動產 fragment:`AddToFaction`/`RemoveFromFaction` + 加/收 `grants[]` ability(+ 選用 `Scene.Start()`)。複用 dispatcher embed 模式,免 per-machine 編譯。

`identity`/`primaryIdentity` 標籤對**對話行 / Hello 招呼 / banter / scene 起始**皆適用。

## 新增:Book 觸發器

併入既有可複用 trigger 庫(magic / potion / activator / dialogue / alias 五入口的同一個 `Fire()` 家族),新增第六入口:
`Book.OnRead`(script `extends Book`)→ MessageBox(「要立下誓言嗎?」)→ 選是 → 走 `grantsIdentity`/`removesIdentity`。

## 三個 showcase 身份(MVP)

- **Adventurer(冒險者)— baseline**:人人預設(**不是**龍裔;龍裔要第一次吸龍魂後才追加,屬後續)。priority 0、無 grant。示範解鎖:旅店老闆處接護衛任務(機制後做)。
- **Paladin(聖騎士)— 最完整展示**:讀特定書 → MessageBox → **宣誓演出**(下跪祈禱[需 PlayIdle] + 特效 + 冥冥之音「我從今天起…」)→ `AddToFaction` + 設主身份 + 授予常駐 ability(攻擊邪惡生物加成)。走完整 Acquire→Grant 鏈。
- **Merchant(商人)— 可切換**:讀書 → 選成為商人 → `AddToFaction`。讀同書可解除(toggle)。示範解鎖:面對市民多一個對話選項開啟交易(交易 UI/機制後做)。

## MVP / 之後

- **MVP(B 本體)**:`identities` 抽象、`identity`/`primaryIdentity` 標籤展開、Book 觸發器、`grants` 加/收、`grantsIdentity`/`removesIdentity` 語法糖、Paladin 全鏈 showcase + Merchant toggle + Adventurer baseline。
- **之後(C / Phase-2)**:
  - 情境 `activeWhen`(服裝 `WornHasKeyword` / 技能 `GetBaseActorValue` / 關係 `GetRelationshipRank`)成為一等公民。
  - 聲望全域子系統、行為追蹤全域(如偷竊次數)。
  - controller script 管理主身份 + 玩家手動覆寫。
  - 龍裔:第一次吸龍魂事件 → 追加 Dragonborn 身份。
  - 身份對應互動(子專案 C):商人交易 UI、護衛任務、聖騎士 smite 細調等。

## 待解(round 2 brainstorm 時收斂)

- `activeWhen` 情境條件與「主身份排除」的交互:情境 gated 身份無法乾淨地被 `GetInFaction==0` 排除(條件束的否定在 CTDA 不好表達)。MVP 規避法:主身份解析只在 faction 訊號上運作;`activeWhen` 只窄化「成立」,不參與排除。round 2 若要情境身份當主,可能得走 controller(Phase-2 機制)。
- `grants` 的「技能」具體形式:常駐 ability(MVP,如 smite evil)/ 教學主動法術 / AV 加值——MVP 先做常駐 ability。
