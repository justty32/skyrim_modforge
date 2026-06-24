# Settlement NPC expansions（Immersive College NPCs / ICMF / ETaC Orc Strongholds）— 把單一聚落「住滿人＋擺出店家結構」

調查三個「擴充一個既有聚落人口」的 mod：**Immersive College NPCs**（學院塞滿學生/學者）、**ICMF（Immersive College Mini Factions）**（學院加一排店家/服務）、**ETaC Immersive Orc Strongholds**（四個獸人要塞填滿守衛/匠人/商人）。三者都是 idea #22「漂泊開拓慢活：建立並住滿一個異世界聚落」的**「單點聚落」版對照組**——比 [populated-skyrim-family](populated-skyrim-family.md)（地毯式全 Skyrim 填充）小而精，重點在「**一個地方怎麼變得有人住、有人上班、有店可逛**」。人口密度基準看 populated-family，本檔看的是**單一聚落的 staffing 配方 + 服務 faction 結構**。

## Scope / sources

| mod | archive (`~/skyrim_mods/hdd/`) | plugin | size | masters |
|-----|------|------|-----:|------|
| Immersive College NPCs | `Immersive College NPCs-9252-1-1-02-…7z` | `ICNs_ImmersiveCollegeNPCs.esp`（另有 `ICNs_Lite.esp`） | 127 KB | Skyrim/Update/Dawnguard |
| ICMF Immersive College Mini Factions | `ICMF Immersive College Mini Factions AE-2291-3-5-6-…zip` | `ICMF Immersive College Mini Factions.esp` | 727 KB | Skyrim/Update/DG/HF/DB + 多 CC（fish/spellpack/curios/BA-armor/staves/redguard）|
| ETaC Immersive Orc Strongholds | `ETaC - Immersive Orc Strongholds SE-Cht.7z`（中文化） | `Immersive Orc Strongholds.esp` | 651 KB | Skyrim/Update/DG/HF/DB + `ETaC - RESOURCES.esm` |

抽取：`7z x` → `~/skyrim_mods/unzip/<name>/`，`extract.sh` → `game-data/mods/<name>/`，record tally 用 `dump`，細節 `packagediag`/`cellrefs`/`factdiag`/`reladiag`/`npcdiag`。記憶體鐵律遵守（只走 CLI lazy overlay）。ETaC 為中文化版，inline Name 是 cp1252→utf-8 mojibake（見 memory `chinese-mod-gamedata-mojibake`），不影響機制判讀。

## Classification

- 類型：**single-settlement NPC expansion**（把一個既有聚落從半空變成住滿/可逛）。
- 敘事價值：**低**（ICN/ETaC 純人口無對白；ICMF 有少量 generic 訓練/服務對白 + 三條 errand 小任務，無角色弧線）。
- 系統價值：**高**（對 #22）——「**單點聚落 staffing**」最乾淨的範本：少量 unique base + per-NPC 手刻日程 + 服務 faction。

## Record shape（`dump` tally，未整載）

| record | ICN | ICMF | ETaC Orc |
|--------|----:|-----:|---------:|
| 總 records | 239 | 3999 | 1867 |
| **Npc**（base） | 16 | 70 | 19 |
| **Package** | 101 | 38 | 29 |
| **PlacedNpc**（ACHR） | 26 | 46 | 32 |
| PlacedObject（XMarker/家具/裝飾 REFR） | 87 | 3391 | 1638 |
| **Cell**（多為 vanilla override） | 8 | 65 | 37 |
| **Faction** | 0（全用 vanilla 學院 faction）| 7 | 10（3 新 + 7 vanilla override）|
| Relationship | 0 | 3 | 0 |
| Outfit | 0 | 14 | 0 |
| Quest | 0 | 4 | 0 |
| DialogTopic / DialogBranch | 0 | 46 / 12 | 0 |
| Book / Container | 0 / 0 | 40 / 43 | 1 / 8 |
| Spell / MagicEffect | 0 | 30 / 6 | 0 |
| Worldspace（Tamriel override carry）| 1 | 1 | 1 |

讀法：三者都是 **少量 unique base（16/70/19）+ 約 1:1.5 的 ACHR**（一人一個放置點），**人口工作量集中在 Package**（ICN 16 人配 101 個包 ≈ 每人 6 個時段包）。PlacedObject 的暴量在 ICMF/ETaC 是**翻修聚落佈景**（家具/裝飾/店面）——不只是放人，是「把場地裝潢成有人住的樣子」。ICMF 的 Book/Spell/Container/Outfit 是**店家庫存**（賣的書、卷軸、法術、商品容器 + 店員制服）。

## Mechanism pattern — 三者的共同骨架（＝「單點聚落 staffing 配方」）

**unique NPC base（指 vanilla race/class/voice/outfit/combatStyle）→ 每人一疊逐時段 Package → ACHR 直接置入聚落的 vanilla cell override → 服務 faction 讓他變店家**。EditorID 前綴分群（ICN=`ICNs_`、ETaC=`MJB…`/`pym_`、ICMF=人名）。下面逐機制拆。

### 1. 置放 = vanilla cell override，**additive 帶 vanilla ref + 加自家 ACHR**

`cellrefs DushnikhYalLonghouse(0x0198E2)`（ETaC 獸人長屋）：

```
npcP 013B7B:Skyrim.esm  ArobREF        ← vanilla 原住民（additive 帶回，不刪）
npcP 013B7F:Skyrim.esm  NagrubREF      ← vanilla
npcT 83013B:…Orc Strongholds  MJBMurzolREF    ← 新增獸人法師商人
npcT 830268:…              MJBBugdurashREF ← 新增
npcT 830269:…              MJBShagarREF   ← 新增
objP 830264:…             （新增家具/裝飾）
# 1 placed object, 5 placed npc, 1 disabled-skipped
```

→ **聚落擴充的本質 = override 該聚落的每個 cell，保留 vanilla 居民、additive 塞進新 NPC 的 ACHR + 新佈景**。ICN 同理 override 學院 8 個 cell（HallofTheElements / Courtyard / dorm 等，全 Skyrim.esm FormID）；ETaC override 四要塞共 37 個 ext/int cell。**異世界版更省**：自家 cell 不必 override、不必背 vanilla 相容（這是 ICN/ETaC 一半複雜度的來源）。

### 2. 行為 = 每人一疊「逐時段 schedule package」（最大工作量、最不可規模化）

ICN 16 人共 101 個包，命名極細：`ICNs_Melker_Sleep_Pkg` / `_Room_Pkg` / `_Study_Pkg` / `_Arc_Pkg`（拱廊閒晃）/ `_Tavern_Pkg` / `_Train_Group2_Part1/2_Pkg`（分組練習）。每包帶 `Schedule: hour/minute/durationMin` + `PackageDataLocation radius` + 目標：

```
ICNs_Lentilus_HotE_Practice_Pkg（0x803）：
  PackageTemplate -> 自製 template；PreferredSpeed=Walk
  Schedule: dayOfWeek=Weekdays hour=7 minute=30 durationMin=150  ← 工作日 07:30 起 2.5h
  Data: LocationTarget(XMarker) radius=32 + TargetObjectType(MeleeWeapons) + SingleRef(練習目標)
```

ETaC 同形但更簡（每獸人 1–2 包）：`MJBDushnikhYalOrcMurzolWork`（hour=8 dur=600 → 早 8 上工 10h），`PackageTemplate -> Skyrim Sandbox/Work template`，target = vanilla 家具 ref。**這是 staffing 的勞力核心：每個 NPC 手刻「睡→上工→用餐→閒晃」一套包，靠 `Schedule` 時段 + `LocationTarget radius` 串成日程**。

排程的「定位點」是 cell 裡置入的 **XMarkerHeading（base 0x000034）**：`cellrefs HallofTheElements` 顯 17 個 `ICNs_*_XMarkH`（如 `ICNs_Practice_Lentilus_XMarkH`）+ `ICNs_*_Target`（base 0x00003B，練習對象）——**先在 cell 放命名 marker，再讓 package 的 LocationTarget 指它**。這正是 [immersive-wenches](immersive-wenches.md) 同款 marker 模式（只是 IW 在 marker 上 script-spawn LL，這三者是靜態 ACHR）。

### 3. 「mini faction」= **per-NPC Vendor 服務 faction（無 rank！）**，不是 rank-tiered 公會

ICMF/ETaC 的「factions」名字唬人，`factdiag` 拆開都是**每個店員一個 Vendor-flag faction**：

```
factdiag GuntherVendorFaction(ICMF 0x5AFB)：
  Flags = Vendor    Ranks (0)    Relations (0)
  VendorValues: startHour=8 endHour=17 radius=0
  VendorBuySellList = 0937A1:Skyrim.esm   MerchantContainer = 005AFA（自家容器）

factdiag MJBDushnikhYalMageVendorFaction(ETaC 0x830266)：
  Flags = Vendor, CanBeOwner    Ranks (0)
  VendorValues: startHour=8 endHour=18 radius=256
  VendorBuySellList = MJB_VendorItemsMage(自製 FormList)   item -> 9 個 vanilla 法術書
```

→ **「開一間店」的最小配方 = 一個 Vendor-flag FACT（含營業時段 + sell/buy FormList + 自家 MerchantContainer）＋把 NPC 加進該 faction**。ICMF 7 個、ETaC 3 個新 Vendor faction＝給聚落補上「法師店/鐵匠/旅店主」三種服務。**沒有 rank 階層、沒有 crime faction（用 vanilla 學院/獸人 crime faction）、沒有 faction 內部敵我**——所以這不是「迷你公會」而是「**迷你商圈**」。對白接 vanilla generic `OffersTrainingTopic`/服務 menu（NPC 一掛進 Vendor faction 引擎自動上「I'd like to trade」），ICMF 額外手寫各專長的訓練建議對白（`DialogueWinterhold…SpecialtyTopic`）。

ICMF 的 3 個 **Relationship（RELA）** 給少數 NPC 補人際（`MaedrosRelationMirabelle rank=Lover`）——讓店員彼此有關係，是「活感」點綴，非結構。

### 4. NPC base 組裝（`npcdiag`）

ETaC Murzol：`Race/Class/Outfit` 全指 vanilla，`AutoCalcStats + Unique + Protected`、`Level=10`、`AIData Aggression=Unaggressive Confidence=Average`、6 個 faction（vanilla 獸人/crime/服務 + 新 Vendor faction）、3 個 package（vanilla 通用 + 自製 Work + vanilla observe）。**注意都配了 Class**（避開 memory `autocalc-without-class-dead-npc` 的死 NPC 陷阱）。ICN 的 `ICNs_Guardian` 是唯一帶 CombatStyle 的（守衛），其餘學者/學生中立無戰鬥。ICMF 的店員另帶自製 Outfit（店員制服）。

## ModForge meaning & gap（對 idea #22）

**已能逐欄生成（這三個 mod 的每一條低階機制都已 landed）：**

| 機制 | landed |
|------|--------|
| NPC base（race/class/voice/outfit/combatStyle/factions/aiData/autocalc+class 配對）| `npcs.md` |
| 逐時段 schedule package（`Schedule` hour/min/dur + LocationTarget radius）+ 10 PACK 模板（含 sandbox/sleep/travel/sittarget/eat）+ alias-target radiant 包 | `npcs.md` |
| ACHR 直接置放 + PlacementSpec 六欄 + **vanilla cell override（additive 帶 vanilla ref）** + XMarker | `world.md` |
| **Vendor faction**（`FactionSpec.Vendor`：Vendor flag + 營業時段 + sellBuyList FormList + MerchantContainer）— `Generator.Build.Vendor.cs` / `examples/vendor_spec.json` | landed |
| **Faction（含 rank）+ Relationship（RELA, parent/child/rank）** — `Spec.Actors.cs` | landed |
| dialogue INFO（含 alias 條件填充）/ 小 errand quest（stage+objective+alias）/ Outfit / Container（店家庫存）| `dialogue-quests.md` / `items-magic.md` |

→ **結論：「住滿並 staff 一個聚落」需要的所有原語 ModForge 全已具備**——base、日程包、cell override additive 置放、Vendor faction、Relationship、店家庫存、服務對白。連 [immersive-wenches](immersive-wenches.md)/[populated-family](populated-skyrim-family.md) 反覆指出的同一結論：**缺的不是能力，是「量產便利層」**。

**單一最重要 GAP（直指 #22 roadmap 的「聚落量產 spec section」）：一個把「一個聚落」一句話展開成上述幾百筆記錄的 macro-expansion section。** 本三 mod 把 populated-family 提的 `settlementPopulation:` 設想**補上了「店家結構」這一面**——所以該 section 的參數至少要含：

- `cells:`（要 override / 自家的 cell 清單）+ 每 cell 一組 `markers`（XMarker 自動配對 package LocationTarget）；
- `residents:`（一組 archetype：學生/學者/守衛/匠人/商人…）× count，每個附一份 **`dailySchedule` 模板**（睡/上工/用餐/閒晃 時段）macro-expand 成 unique base + 逐時段 package + ACHR；
- `shops:`（新增**最有價值的一格**）：給 `{ vendorType（法師/鐵匠/旅店/雜貨）, owner, hours, sellBuyList, inventory }` → 自動生 **Vendor FACT + MerchantContainer + 庫存 + 店員 base + 服務對白掛接**，把本三 mod 手刻的「per-NPC Vendor faction」變一鍵。
- 選配 `relationships:`（店員/居民間 RELA）+ `enableParent`/`gate GLOB`（補靜態置放「無狀態」弱點，順手做 MCM 式密度/開店開關）。

[`skillTrees:`](world.md)（idea #20 Phase 3 landed）已證明這條 macro-expansion 路在 ModForge 完全可行（`Build()` pass-0 `Expand*` 展開成既有低階記錄、重用全 pass、新碼極少）。本三 mod 就是該 section 要生出來的活樣本：**ICN = 日程 staffing 樣本，ICMF/ETaC = 店家/服務 faction 樣本，三者合起來 = 「一個有人住、有人上班、有店可逛的聚落」的完整參數字典**。

風險（沿用 family finding）：① cell override 數/置放數膨脹快，量產層要給上限與 navmesh-safe 紀律；② 大量 base 要 FaceGen 提醒（ICN/ETaC 都背 facegen）；③ Vendor faction 別忘 MerchantContainer + sellBuyList（漏一個就「店員不開店」）；④ 靜態日程 ≠ 動態狀態。

## Verdict

**可借鏡（高）**。三者 100% 機制已 landed，是 #22「聚落量產 spec section」的**「單點聚落 + 店家結構」面**最直接藍圖——補齊了 [populated-skyrim-family](populated-skyrim-family.md)（密度面）與 [immersive-wenches](immersive-wenches.md)（生怪面）沒涵蓋的「**服務商圈 staffing**」。內容本身（generic 訓練對白 + errand）對 #22 無敘事價值，**只借 staffing/shop 配方，不借內容**。與 Sofia patch 無交集（不改 follower topics；但 override 學院/獸人要塞 cell，與任何也改這些 cell 的 mod 需相容 patch）。最小垂直切片建議：1 個異世界聚落 cell + 3 居民（各一份 dailySchedule）+ 1 個 Vendor faction 店家，驗「有人住、會上班、可交易」三件事，再擴。
