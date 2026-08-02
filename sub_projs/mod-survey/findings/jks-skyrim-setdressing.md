# JK's Skyrim family（set-dressing 全家桶） — Nexus 6289 v1.7

調查 **JK's Skyrim**（Jokerine/JK 系列）：城鎮 / 內裝的**靜態雜物與佈景大改**——把聚落塞滿桌椅、貨攤、招牌、繩索、植栽、藤蔓、燈、籬笆…讓城市看起來「有人住、有在用」。

> **這是 set-dressing（靜態佈景），不是 population（活人）**。它與 [populated-skyrim-family.md](populated-skyrim-family.md) 是同一個「讓聚落活起來」問題的**兩半**：Populated 填活人 + 排程，JK's 填**物件密度**。兩者疊起來才是完整的「有人住的城」。對照另一種做法見 [base-object-swapper.md](base-object-swapper.md)（runtime 換物，與 JK's 的靜態置放正相反）。

## Scope / sources

| | archive (`~/skyrim_mods/hdd/`) | plugin |
|------|------|------|
| **本檔主角：all-in-one（EN）** | `JK's Skyrim all in one-6289-1-7-1614998676.zip`（3.2 MB esp + 11 MB BSA mesh/texture） | `JKs Skyrim.esp` (3.2 MB) |
| 模組化（per-city / per-interior，~30 個） | `JK's Whiterun…` `JK's Riverwood…` `JK's Dragonsreach…` `JK's The Bannered Mare…` `JK's Belethor's General Goods…` … | 各自一個小 esp，**同一置放 pattern、同一 `XJK*` 命名**，差別只在範圍切片（一城或一店一檔）；玩家照需求挑裝，all-in-one = 全部聯集 |

抽取：`7z x` → `~/skyrim_mods/unzip/JKs-Skyrim-AIO/`，記錄概覽用 `dump`，新內裝用 `cellrefs`。記憶體鐵律遵守（只走 CLI lazy overlay）。

## Classification

- **Type**：world set-dressing / 純靜態置放（mass STAT placement via vanilla cell override）。
- **敘事價值：無**。零 quest、零 scene、零有意義對白；39 個新 NPC 全是為新增的幾間商店補的店主（`XJKsDawnstarGeneralvendor "Balgus"`…），53 個 package 也只是這些店主的 sandbox/vendor 排程。**它不講故事，它佈置舞台。**
- **系統價值：高**（對 #22）。這是「**靠置放量讓空間顯得被使用**」的密度基準與 placement-volume 活樣本。

## Key records & scale（record-type tally，`dump`）

5 masters：Skyrim + Update + Dawnguard + HearthFires + Dragonborn。21325 records，壓倒性是置放：

| record | count | 說明 |
|--------|------:|------|
| **PlacedObject (REFR)** | **18550** | 全部精華都在這裡——雜物/家具/招牌/植栽的靜態置放 |
| Static (base) | 293 | 少量自製 STAT base（多為走道/招牌等，textureSet 改皮）|
| **Cell** | **182**（170 Skyrim + 12 Dragonborn）vanilla **override** ＋ **12 新內裝**（`XJK*`）| 置放的載體 |
| **NavigationMesh** | **140**（138 Skyrim + 2 Dragonborn）override | 置放紀律：改完佈景**重做尋路**避免卡 NPC |
| Worldspace | 7 override | Tamriel + Solstheim + 5 座牆內城（Whiterun/Windhelm/Riften/Markarth/Solitude World）|
| PlacedNpc (ACHR) | 109（96 新 + 13 vanilla override）| 新店主置入 |
| Npc (base) | **39** | 只有新店主，全新建（指 vanilla race/voice/outfit）|
| Package | 53 | 店主排程 |
| Container/Faction/Outfit/Key… | 數十 | 配合新商店的零碎支援（vendor faction + merchant chest）|

對比 [Populated Cities](populated-skyrim-family.md)：那邊 1115 Npc / 190 ACHR / 1190 Package（**人**為主）；JK's 是 18550 REFR / 39 Npc（**物**為主）。數字直接證明 set-dressing vs population 的分工。

## Mechanism pattern（單一手法，重複一萬八千次）

**核心 = 加性 vanilla cell override（additive cell override）**：拿一個 vanilla exterior/interior cell，**原樣帶回它既有的 vanilla refs，再追加自己的數百筆新 REFR**。實測 145 個 exterior override 中 **119 個 total > new**（帶回 vanilla refs 後加料），平均**每個 vanilla override 新增 106 筆**置放：

| cell（override） | new refs | total refs |
|------|------:|------:|
| `WindhelmOrigin` 0x03837E | 631 | 689 |
| `RiftenOrigin` 0x042247 | 594 | 637 |
| `DragonBridgeExterior01` 0x009328 | 431 | 456 |
| `RoriksteadExterior03` 0x009597 | 400 | 452 |
| `FalkreathExterior01` 0x009C80 | 353 | 389 |
| `SolitudeArch` 0x037EE7 | 334 | 393 |
| `MarkarthOrigin` 0x020EE7 | 310 | 332 |
| `XJKDawnstarShipInterior` 0x0021D8（新內裝）| 755 | 755 |

每筆 REFR = `base FormID（幾乎全指 vanilla STAT）+ position + rotation + scale`，無腳本、無 enable-parent gate、無條件——**純資料**。同一把 vanilla 雜物 base 被海量複用（如某 base 出現 744 次、另一個 665 次），靠位置/旋轉/縮放變化營造多樣。

新增 12 間商店是「from-scratch 內裝」：新 interior cell 從零塞滿 300-750 筆 REFR + 一個店主 NPC + vendor faction + merchant container（沿用 vanilla 雇傭/商店框架，無新機制）。

**為什麼不 CTD / 不卡**：① 純 record，零 runtime spawn，負載可預期；② **140 個 navmesh override** 把改過佈景的地面尋路一起重做（這是 set-dressing mod 不踩壞 AI 的代價，也是衝突大戶）；③ 自製 base 與紋理隨 BSA 出貨。

**衝突 profile（需相容的點）**：因為是 vanilla cell + worldspace + navmesh 的 override，**任何動到同一城市外觀/尋路的 mod 都會撞**（這正是社群有海量 JK's compatibility patch 的原因，hdd/ 內就有 `CFTO - JK's Skyrim Patch`、各種 -patch）。**靜態置放最大弱點**：給得起密度，給不起狀態——佈景無法隨劇情/陣營/季節改變（與 BOS 的 runtime 換物互補：BOS 才能做「戰後變廢墟」這種狀態化佈景）。

## ModForge meaning（直指 #22 與 Godot editor）

**設定 idea #22「漂泊開拓慢活：有人住的otherworld聚落」要的「看起來被使用的空間」，本質就是 placement-volume 問題——而 ModForge 的 placement pipeline 已完全覆蓋這條低階機制：**

- **REFR 置放 + PlacementSpec 六欄**（Scale / InitiallyDisabled / EnableParent / Lock / Ownership / Count）+ **vanilla cell override** + map marker — 全 landed（`world.md`）。JK's 用到的每一欄 ModForge 都生得出來；ModForge 還多了 EnableParent/gate 旋鈕，正好補 JK's「靜態無狀態」的弱點。
- **Static base + TextureSet 改皮** — 可生成。
- **新 interior cell from-scratch + vendor faction + merchant container + 店主 NPC/package** — 全 landed。
- **NavMesh override**（custom NAVM+NAVI）— landed（記憶 `programmatic-navmesh`）；這是 set-dressing 量產時**必須一起生**的配套。

**真正的契合點 = Godot worldspace editor 就是 set-dressing 的天然 authoring 前端。** [`../godot-worldspace-editor`](../../../sub_projs/godot-worldspace-editor/README.md) 的匯出格式 `placements.json`（`base / position(m) / rotation(rad) / scale / instanceId?`）與本調查 `cellrefs` 倒出的 cell 內容**逐欄 1:1**——JK's 那 106 筆/cell 的雜物擺放，正是「在 WYSIWYG 編輯器裡 hand-place 或 GDScript 程序化散佈」最適合做的事，做完一鍵 `godotPlacements: {$include}` 掛進 worldspace spec → ModForge 生 REFR。**JK's 是用 CK 手刷出來的；ModForge + Godot editor 是這套手藝的可腳本化替代前端。**

對照兩種 set-dressing 路線：
| | JK's（本檔，靜態）| BOS（[base-object-swapper](base-object-swapper.md)，runtime）|
|---|---|---|
| 機制 | cell override 寫死 REFR | SKSE 載入時依 ini 條件換 base |
| 狀態化 | ✗（佈景固定）| ✓（可條件/機率/時段）|
| ModForge 對接 | placement pipeline + Godot editor（生 REFR）| 生 BOS `_SWAP.ini`（純設定，見該檔）|
| #22 用途 | 把新聚落一次塞滿、定調 | 讓佈景隨開拓進度演變 |

→ #22 的 set-dressing 需求 = **placement-volume 問題，ModForge 既有 pipeline 已能解，最佳 authoring 工具是 Godot worldspace editor**；若要「會變化的聚落」再疊 BOS 輸出。與 population sibling 合看：#22 一個聚落 = `settlementPopulation:`（活人，見 populated-skyrim finding 的 GAP）＋ Godot-placed set-dressing（物件密度）＋ navmesh。

風險：① REFR 數爆漲快（單城就上千），量產要 count 上限 + navmesh-safe 紀律；② cell/worldspace/navmesh override 衝突面大，與其他城市 mod 不相容是常態（patch 文化）；③ 靜態密度 ≠ 狀態，別誤當模擬。

## Verdict

**可借鏡（高，系統面）／需相容（外部 mod 衝突面）**。敘事價值無，但它是 #22「讓聚落看起來有人在用」的 placement-volume 活範本：低階機制 ModForge 100% 已具備，**天然 authoring 工具就是 Godot worldspace editor**（`placements.json` 與 cellrefs 逐欄對得上）。模組化 per-city 版本同 pattern、只是範圍切片。與 [populated-skyrim-family.md](populated-skyrim-family.md)（活人那半）、[base-object-swapper.md](base-object-swapper.md)（狀態化那條路）合看，構成 #22 聚落的完整佈置藍圖。
