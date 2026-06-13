# NPC 日程 / AI 擴充計畫（2026-06-13）— 對照 ModForge 可實現性

← 配套解碼：[ai-overhaul-decode-2026-06-13.md](ai-overhaul-decode-2026-06-13.md)（AI Overhaul.esp：424 NPC override / 744 package = Sandbox+Eat+Sleep+Sit 日程堆疊）

## 1. 目標

把 AI-Overhaul 式的「會生活的 NPC」日程，做成可由一份 ModForge JSON spec 生成的能力——讓鎮民按時段吃飯、睡覺、開店、走市集、巡邏、上教堂，而非站在原地當雕像。本計畫拆兩條線：**(A) 為 ModForge 自己新建的 NPC 排完整日程**（這條 ModForge 今天就大致能做：10 個 PACK 模板 + `npcs[].packages` 順序堆疊 + `schedule` 時段 + `conditions` 全在）；**(B) 把日程當 patch 套到既有 vanilla NPC 上**（這是 AI Overhaul 的本質，也是 ModForge 目前的硬缺口——ModForge 只建**新** record，沒有「override 既有 NPC 的 package 清單」這條路）。本文逐功能標出 ✅能 / 🟡需小幅擴充 / 🔴缺口，並為缺口給出具體 spec/build 設計。

> 鐵律前置（解碼確認，與 ModForge 既有慣例一致）：package 堆疊**順序 = 優先級**，具體時段包（eat/sleep/sit/travel）在前、broad `sandbox` fallback 在最後一個（`npcs[].packages` 陣列順序就是這個語意，見 SPEC-packages.md）。

---

## 2. 功能清單

### F1. 為新 NPC 排完整日程（sandbox+eat+sleep+sit+travel 堆疊）
(a) 一個自訂鎮民：白天在攤位 sit、用餐時段 eat、夜間 sleep、其餘時間在家附近 sandbox 遊蕩。
(b) **✅ 能**。10 個 PACK 模板齊全（`eat` 0x019714 / `sleep` 0x019717 / `sittarget` 0x0A9277 / `travel` 0x016FAA / `sandbox` 0x01C254…）；每個 package 帶 `schedule {hour,durationInMinutes}` 開時段窗，`npcs[].packages` 順序即優先級。這正是 AI Overhaul 對單一 NPC 做的事（單 NPC 4–8 包），ModForge 對**新** NPC 完全可複製。
(c)
```jsonc
"npcs": [{ "editorId": "MF_Carlotta", "race": "...", "class": "MF_Vendor",
  "packages": [ "MF_Eat8", "MF_Eat19", "MF_Sleep", "MF_StallSit", "MF_HomeSandbox" ] }]
"packages": [
  { "editorId": "MF_Eat8",  "template": "Skyrim.esm:0x019714", "schedule": {"hour":8, "durationInMinutes":60},  "eat": {"radius":400} },
  { "editorId": "MF_Sleep", "template": "Skyrim.esm:0x019717", "schedule": {"hour":22,"durationInMinutes":480}, "sleep": {} },
  { "editorId": "MF_StallSit","template":"Skyrim.esm:0x0A9277", "schedule": {"hour":9, "durationInMinutes":600}, "sitTarget": {"target":"MF_StallChairRef"} },
  { "editorId": "MF_HomeSandbox","template":"Skyrim.esm:0x01C254","sandbox": {"radius":1024,"location":""} }   // broad fallback，放最後
]
```

### F2. 商人營業時段（vendor faction + 攤位 sit package + 時段 condition）
(a) 商人只在白天到攤位營業、按時段坐攤位，打烊回家。
(b) **✅ 能**。`factions[].vendor` 有 `startHour/endHour`（引擎自動 gate「我想交易」topic）；攤位由 `sittarget` package 在營業時段 `schedule` 內把 NPC 帶到攤位椅子；merchant chest 由 placement 提供（`merchantContainer`）。AI Overhaul 的 `ServicesWhiterunCarlotta` 就是這個組合。（vendor 本身 in-game-unconfirmed，但結構與 vanilla 商人 `factdiag` diff 過。）
(c)
```jsonc
"factions": [{ "editorId":"MF_StallFaction", "vendor": {
  "startHour":9, "endHour":18, "sellBuyList":"Skyrim.esm:0x06CB48",
  "merchantContainer":"MF_StallChestRef" } }]
"packages": [{ "editorId":"MF_StallSit", "template":"Skyrim.esm:0x0A9277",
  "schedule": {"hour":9,"durationInMinutes":540}, "sitTarget": {"target":"MF_StallChairRef"} }]
```

### F3. 放置 sandbox / travel / idle 標記讓 NPC 有去處
(a) NPC 要有「家」「市場中心」「攤位椅」等真實座標當 anchor，否則日程無處可去。
(b) **✅ 能**。`placements[]` 可放 `kind:"xmarker"` 隱形 anchor（forced persistent），或直接引用 vanilla ref（市場中心 marker、酒館中心 marker）。`travel.place` / `sandbox.location` / `sitTarget.target` 都吃「placement editorId 或 vanilla ref」。注意 navmesh 可達性（攤位椅須在 NPC 走得到的同 cell）。
(c)
```jsonc
"placements": [
  { "editorId":"MF_MarketMarker", "kind":"xmarker", "cell":"Skyrim.esm:0x01605E", "position":{"x":120,"y":40,"z":0} } ]
"packages": [{ "editorId":"MF_GoMarket", "template":"Skyrim.esm:0x016FAA",
  "travel": {"place":"MF_MarketMarker","radius":256} }]
```

### F4. 巡邏路線（守衛日夜繞城）
(a) 守衛沿一串點位巡邏。
(b) **✅ 能**。`patrol` 模板（0x017723）已實作（`examples/patrol_spec.json`），點位走 placement ref。可配 `schedule` 做日夜班。
(c) `{ "template":"Skyrim.esm:0x017723", "patrol": { /* 巡邏點 refs */ }, "schedule": {"hour":6,"durationInMinutes":720} }`

### F5. 教堂 / 酒館作息（時段 + 地點堆疊）
(a) 鎮民傍晚上酒館喝一杯、特定時辰去神壇祈禱。
(b) **✅ 能（祈禱動作 🟡）**。「去酒館」= travel→sandbox/eat 堆疊（F1 套路）；「去神壇祈禱」的**移動到神壇**可做（travel + sandbox），但**跪拜祈禱動畫**目前只在 scene 演出裡走 `SceneActionSpec.Idle`（PlayIdle），**一般日程 package 沒有「播 idle 動畫」這格**——日程要動畫得包成一個 scene 或 alias 腳本 PlayIdle，非純 package。標 🟡：日常 ambient 祈禱動畫缺 package-level idle hook。
(c)
```jsonc
"packages": [
  { "editorId":"MF_GoTavernEve","template":"Skyrim.esm:0x016FAA","schedule":{"hour":18,"durationInMinutes":30},"travel":{"place":"Skyrim.esm:0x...InnCenter","radius":200} },
  { "editorId":"MF_TavernEat",  "template":"Skyrim.esm:0x019714","schedule":{"hour":19,"durationInMinutes":90},"eat":{"radius":400} } ]
```

### F6. 條件式行為（時段 / 室內外 / 玩家在場）
(a) 「只在白天」「只在室內」「下雨才躲屋裡」的窄化條件。
(b) **✅ 能（天氣條件 🟡）**。`packages[].conditions` 共用 `ConditionSpec`：`GetCurrentTime`（時段）/ `IsInInterior` / `GetRandomPercent` / `GetGlobalValue` 等都在。**天氣條件（下雨躲雨）**——CTDA 有 vanilla `GetCurrentWeatherPercent` 之類函式，但 ModForge 的 `ConditionSpec.function` 白名單目前未列天氣函式，標 🟡（加一個 enum 名即可）。
(c) `"conditions": [ { "function":"GetCurrentTime", "comparison":">=", "value":9 }, { "function":"GetCurrentTime","comparison":"<","value":18 } ]`

### F7. 季節 / 天氣行為
(a) 冬天多待室內、暴風雪減少外出。
(b) **🟡 需小幅擴充**。同 F6：缺天氣 CTDA 函式白名單項；「季節」Skyrim 無原生季節變數，需綁某 GLOB（可由 ModForge `globals[]` + 外部 mod/腳本驅動）。`GetGlobalValue` 條件已可讀任意 GLOB，故「季節」靠約定一個 GLOB 即可，天氣才是真正要補的 CTDA 名。
(c) `"conditions": [ { "function":"GetGlobalValue", "param":"MF_Season", "comparison":"==", "value":3 } ]`（GLOB 由 `globals[]` 建）

### F8. 不重複 / 一天一次的特殊日程節點
(a) 「每天只去一次市集叫賣」。
(b) **✅ 能**。`Package.Flag` 已含 `OncePerDay`（SPEC-packages.md flag 清單），列進 package `flags` 即可。
(c) `"flags": ["OncePerDay","PreferredSpeed"]`

### F9. 配合新作息的城鎮對話 / banter
(a) 攤主在攤位時主動吆喝、夜裡打烊時嘟囔。
(b) **✅ 能（限新對話）**。`banter[]` 做不請自來的 ambient 線（需 NPC 帶 `AllowIdleChatter` 的 sandbox 包，已是日程標配），用 `conditions`（`GetCurrentTime`/`IsInInterior`）按時段切詞。**但**只能掛在**新建** NPC 或新 quest 上；**改既有鎮民的 vanilla 對話條件**（AI Overhaul 改了 12 個 `Dialogue<City>` quest 配合新作息）= 對既有 INFO 的 override，落在 F10/F11 的同一缺口裡。
(c) `"banter": [{ "questEditorId":"MF_TownLife", "speakerNpcEditorId":"MF_Carlotta", "responses":["Fresh produce!"], "conditions":[{"function":"GetCurrentTime","comparison":">=","value":9}] }]`

### F10. **Override 既有 vanilla NPC 的 packages（核心缺口）**
(a) 拿 vanilla 的 Carlotta / Beirand，**換掉**其 package 清單為我們排的日程堆疊——AI Overhaul 的 424 個 override 全是這件事。
(b) **🔴 缺口**。`NpcSpec` 只有 `EditorId`（永遠 `new Npc(...)` 建**新** record），沒有「指向既有 vanilla NPC FormID 並覆寫其 Packages」的路徑。詳見 §3。
(c)（提議的 spec 形狀）
```jsonc
"npcPatches": [
  { "overrideOf": "Skyrim.esm:0x0001A6A0",          // vanilla Carlotta
    "packages": [ "MF_Eat8","MF_Sleep","MF_StallSit","MF_HomeSandbox" ],
    "packageMode": "replace" } ]                      // replace | prepend | append
```

### F11. Override 既有 cell / 對話 INFO 以配合 patch
(a) 在 vanilla 鎮的 cell 放新 anchor / 改既有對話條件。
(b) **✅ cell 放 anchor 能 / 🔴 改既有 INFO 缺口**。往 vanilla cell **additive** 加 placement（anchor、椅子）今天就能（`placements[].cell:"<master>:0xID"`，§3 同 override 機制，**additive**）；但**修改既有 vanilla INFO 的條件**（如 AI Overhaul 的 `Dialogue<City>` patch）= override 既有 dialogue record，與 F10 同類缺口，目前無路。
(c) anchor 部分見 F3；改 INFO 部分待 §3 的通用 override 基建。

### F12. 互斥日程衝突防護（與 USSEP / 其他 mod）
(a) 兩個 mod 都 override 同一 NPC，load order 後者全覆蓋前者整份 record。
(b) **🔴 缺口（隨 F10 一起）**。這是 record-level override 的固有問題，非 ModForge 特有；設計 F10 時必須在文檔講清楚 load-order caveat（見 §3）。

---

## 3. vanilla-NPC-override 缺口（核心）

AI Overhaul 的本質就一句話：**拿既有 NPC、換掉它的 packages**。ModForge 至今只會「生新內容」，跨不到「patch 既有世界裡的 actor」這一步。要補上一個 **"NPC AI patch"** 功能，需要三層：

### 3.1 Spec 層
新增頂層 `npcPatches[]`（與 `npcs[]` 並列，語意明確分開「新建」vs「改既有」）：
```jsonc
"npcPatches": [
  { "overrideOf": "<master>:0xFORMID",     // 必填：既有 NPC 的 ref（Carlotta = Skyrim.esm:0x0001A6A0）
    "packages": [ "<pack ref>", ... ],     // in-spec PACK editorId 或 vanilla template ref
    "packageMode": "replace|prepend|append" } ]   // 預設 replace（AI Overhaul 全 replace）
```
- `packages` 沿用既有 PACK 解析（in-spec 或 `<master>:0xID`），**完全複用** F1 的 10 模板生成路徑——patch 不需要新 package builder，只需新「把這疊 PACK 掛到既有 NPC」的 wiring。
- 之後可擴 `factions`/`outfit`/`flags` 等同類 override 欄位，但**第一刀只做 packages**（AI Overhaul 的 744 包 = 純 package override，先打這個 80%）。

### 3.2 Build 層
這正是 ModForge 已經在 cell / worldspace override 上跑通的同一個 **GetOrAddAsOverride pattern**：

- **既有精準先例**（`Generator.Build.ExteriorCells.cs:166-176`、`Generator.Build.Cells.cs`）：override vanilla cell 時，從 master cache 取既有 getter，`new Cell(existing.FormKey, SkyrimRelease.SkyrimSE)` 以**同 FormKey** 建一份 override，`CopyCellEnv` 抄回原欄位，再**只改我們要動的**（加 ref / 改光照）。NPC patch 就是把這套搬到 NPC：
  ```
  master cache TryResolve<INpcGetter>(vanillaFk, out src)
    → var npc = new Npc(src.FormKey, SkyrimRelease.SkyrimSE)
    → 抄回必要欄位（或用 Mutagen GetOrAddAsOverride 直接深拷既有 record）
    → npc.Packages = [我們解析出的 PACK FormKey 清單]（replace）
      或 prepend/append 到 src.Packages.DeepCopy()
    → mod.Npcs.Add(npc)
  ```
- Mutagen 對 NPC 提供 `ISkyrimMod.Npcs.GetOrAddAsOverride(srcGetter)`，比手抄欄位更省（NPC 沒有 cell 那種 localized-string 深拷陷阱，故可直接用 `GetOrAddAsOverride`——cell 當初避開它是因為它會深拷 localized Name，NPC patch 通常無此顧慮）。
- masters：override 既有 record 會自動把 `Skyrim.esm`（及該 NPC 所屬 DLC master）加進 plugin 的 master 列表；若要疊在 USSEP 上，patch plugin 需把 USSEP 列為 master 並在 load order 排在其後（AI Overhaul 的 master 列表正是 `Skyrim+Update+DLC+USSEP`）。

### 3.3 衝突 / load-order caveat（必寫進文檔）
- **後載者全覆蓋**：兩個 mod override 同一 NPC，引擎不合併 package 清單——load order 最後那個的整份 NPC record 生效。ModForge patch 與 USSEP（或其他 AI mod）對同一 NPC 衝突時，需玩家用 patcher（如 zMerge / Synthesis）或手調 load order。
- **`replace` vs `append`**：`replace` 最乾淨（AI Overhaul 取法），但會丟掉 vanilla 原本的 package（含 quest 綁定包，可能破任務）；`append`/`prepend` 保留原包但順序語意（優先級）要小心——我們的時段包要 prepend 在 vanilla broad sandbox 之前才會贏。文檔需點明：**動劇情關鍵 NPC 用 append + 高優先時段包**，動純鎮民用 replace。
- **驗證**：建議配一個 `npcdiag <patch.esp> <0xFORMID>` 能 dump override 後的 package 清單（既有 NPC diag 已能列 package，擴成讀 override 即可）。

### 3.4 為何這步可行性高
F10 不需要任何新的 record builder——package 生成（10 模板）、condition 接線、placement anchor 全已存在且 in-game 驗過。缺的只是**「把既有 record 拉出來 override 再覆寫一個欄位」這一個 wiring 函式**，而這個 pattern ModForge 已在 cell / worldspace / NAVI override 上跑通三次。是低風險、高槓桿的一步，讓 ModForge 從「生新 mod」跨到「patch 既有世界」。

---

## 4. 建議順序

| 階段 | 內容 | 依賴 | 風險 |
|---|---|---|---|
| **P0** | （已有，無需開發）用 F1–F4/F8 為**新** NPC 排完整日程 + vendor 營業時段，先出一份「活鎮 demo」spec 驗證堆疊語意 in-game | 無 | 低 |
| **P1** | F10 `npcPatches[]` MVP：`overrideOf` + `packages` + `replace`，build 走 `Npcs.GetOrAddAsOverride`，配 `npcdiag` override dump | P0 的 package 生成 | 中（master/load-order，但 pattern 已驗） |
| **P2** | F10 補 `packageMode: prepend/append` + 文檔 load-order/USSEP caveat（§3.3） | P1 | 低 |
| **P3** | F6/F7 補天氣 CTDA 函式白名單（`GetCurrentWeatherPercent` 等）+ 季節 GLOB 約定 | 無 | 低 |
| **P4** | F11 改既有 vanilla INFO（dialogue override）+ F5 日程 idle 動畫 hook（package-level PlayIdle 或標準化 scene 包裝） | P1 的 override 基建 | 中–高 |

---

## 5. 缺口彙總

| # | 功能 | 狀態 | 缺什麼 / 需動什麼 |
|---|---|---|---|
| F1 | 新 NPC 完整日程堆疊 | ✅ | — |
| F2 | 商人營業時段 | ✅ | （vendor 本身 in-game-unconfirmed） |
| F3 | sandbox/travel/idle 標記 placement | ✅ | — |
| F4 | 巡邏路線 | ✅ | — |
| F5 | 教堂/酒館作息 | 🟡 | 日程級祈禱**動畫**無 package idle hook（移動到地點 ✅） |
| F6 | 條件式行為（時段/室內外） | 🟡 | 天氣 CTDA 函式白名單未列（時段/室內外 ✅） |
| F7 | 季節 / 天氣行為 | 🟡 | 天氣 CTDA 名（季節靠 GLOB 已可） |
| F8 | OncePerDay 特殊節點 | ✅ | — |
| F9 | 配合作息的新 banter | ✅ | （改既有鎮民對話落 F11） |
| F10 | **override 既有 vanilla NPC packages** | 🔴 | `npcPatches[]` spec + `Npcs.GetOrAddAsOverride` wiring（§3） |
| F11 | override 既有 cell anchor / INFO | ✅/🔴 | cell additive anchor ✅；改既有 INFO 🔴（同 F10 基建） |
| F12 | 與 USSEP 衝突防護 | 🔴 | load-order/override caveat 文檔 + diag（隨 F10） |

**計數：✅ 6（F1,F2,F3,F4,F8,F9）／🟡 3（F5,F6,F7）／🔴 3（F10,F11-INFO,F12）**（F11 cell-anchor 面算 ✅，INFO 面算 🔴）

**一句話結論**：日程**生成**能力 ModForge 今天就齊（10 PACK + schedule + 順序優先 + 條件 + vendor 時段），AI-Overhaul 式 demo 對**新** NPC 即刻可做；唯一真缺口是把同一疊日程 **patch 到既有 vanilla NPC**——而這恰好複用 ModForge 已在 cell/worldspace override 上跑通的 `GetOrAddAsOverride` pattern，是低風險高槓桿的下一步。
