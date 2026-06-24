# Populated Skyrim family (Steelfeathers) — 純人口填充 mod 全家桶

調查 Steelfeathers 的「Populated」系列（城鎮 / 荒野道路 / 地城 / Hell 極限版）。同家族的 **Civil War 變體已另寫** → [populated-skyrim-civil-war.md](populated-skyrim-civil-war.md)，本檔不重複它；置放式巡邏的最小範例見 [immersive-patrols.md](immersive-patrols.md)。本系列正是 idea #22「漂泊開拓慢活：有人住的聚落 + 有人走的荒野/道路」最直接的對照組。

## Scope / sources

| 變體 | archive (`~/skyrim_mods/hdd/`) | plugin |
|------|------|--------|
| Cities/Towns/Villages | `Populated Cities Towns Villages SE BSA-2005-…7z` | `Populated Cities Towns Villages Legendary.esp` (2.1 MB) |
| Lands/Roads/Paths | `Populated Lands Roads Paths Legendary loose files-1840-…7z` | `Populated Lands Roads Paths.esp` (0.7 MB) |
| Dungeons/Caves/Ruins | `Populated Dungeons Caves Ruins Legendary Edition-2820-1-0.7z` | `Populated Dungns Caves Ruins Legendary.esp` (0.28 MB) |
| Hell Edition（極限版＝四者聯集＋更多） | `Populated Skyrim Hell Edition-5017-…7z` | `Populated Skyrim Legendary.esp` (2.8 MB) |

抽取：`7z x` → `~/skyrim_mods/unzip/`，`extract.sh` → `sub_projs/game-data/mods/<name>/`，record 概覽用 `dump`，package 用 `packagediag`。記憶體鐵律遵守（只走 CLI lazy overlay）。

## Classification

- Type：world population / 純置放 NPC（無任務、無對白演出）。
- 敘事價值：**無~低**。沒有 quest / scene / 有意義對白（Hell 的少數 DialogTopic 只是「雇傭兵/商人雇用」交易選項，沿用 vanilla hireling 框架）。
- 系統價值：**高**（對 #22）。這是「把世界填滿活人」的密度基準與 archetype 字典。

## Key records & scale（record-type tally，`dump`）

| record | Cities | Lands/Roads | Dungeons | Hell |
|--------|-------:|------:|------:|-----:|
| 總 records | 6755 | 1743 | 1464 | 8350 |
| **Npc**（base） | 1115 | 943 | 90 | **3171** |
| **PlacedNpc**（ACHR） | 190 | 146 | 549 | **1863** |
| PlacedObject（REFR/marker/idle） | 3910 | 125 | 604 | 1518 |
| **Package** | 1190 | 161 | 66 | 649 |
| LeveledNpc | 62 | 138 | — | 267 |
| Cell（多為 override vanilla） | 164 | 88 | 104 | 558 |
| Faction | 25 | 24 | 3 | 56 |
| NavigationMesh | 29 | — | — | — |
| masters | Skyrim+Update | Skyrim+Update | Skyrim+Update | +Dawnguard+HearthFires+Dragonborn |

讀法：Cities = **1 unique base : 1 ACHR**，但 **package 數 ≈ base 數**（每個市民有專屬排程 package）。Dungeons 反過來 = 90 base : 549 ACHR（少量共用敵性 base 大量置放）。Hell = 把前三者疊起來再加 bandit/militia/prisoner。

## Mechanism pattern（四變體的差異就是機制的差異）

共同骨架（與 Civil War / Immersive Patrols 同源，無 controller）：
**自製 NPC base（race/class/voice/outfit/combatStyle/factions 全指 vanilla FormID）→ 指派 package → 直接 ACHR 置入 vanilla cell（cell override）→ 靠 faction 敵我與 package AI 產生 emergent 行為**。EditorID 前綴 `ssss`/`oooo`/`iiii`/`eeee`/`rrrr`/`kkkk`/`llll` 分群。Cities 還自帶 29 個 NavigationMesh override（置放紀律 = 不踩壞尋路）。

四變體各自的「人口機制」：

| 變體 | base 策略 | package 類型 | faction / 敵我 | 場景 |
|------|-----------|-------------|----------------|------|
| **Cities** | 每市民一個 unique base（乞丐/勞工/商人/僕役/旅人…），1190 個**逐時段排程 sandbox**（EditorID 編了城+活動+時刻，如 `ooooRiftenTavernBeeandBarbEAT2007`＝20:07 到 Bee&Barb 吃飯）。`Schedule: hour/minute/durationMin` + 多個 LocationTarget(radius) 串成「白天上工→旅店用餐→閒晃」 | sandbox/eat/work/tour，build on vanilla template | 中立、不打玩家 | override 旅店/商店/民居等 interior CELL |
| **Lands/Roads** | wilderness archetype 字典：`Merchant`/`WanderingKnight`/`Pilgrim`/`Adventurer(+Horse)`/`Refugee`/`Mercenary(Warrior/Missile/Wizard)`/`BountyHunter`/`VigilantOfStendarr`。大量 **LeveledNpc(138)** 撐難度，少量 unique 撐身分。ACHR 置於 **Tamriel exterior 大座標** | **travel/wander**（PreferredSpeed=Walk，雙 LocationTarget 走點對點路線，`*Dead` 變體用 condition gate boss-死後行為），refugee 分 day/night | 多為中立路人；BountyHunter/Assassin 有敵性 faction | 荒野/道路/橋 |
| **Dungeons** | 只 90 base 卻 549 ACHR：少量敵性 fauna/undead base（wolf/warg/skeleton…）大量複用置放 | follow-boss / pack（如 `…PackWolvesBossDead` boss-死後散開） | **`iiiiIhatebandit`/`kkkkIhatePlayer`/`rrrrIhatePlayerSkeleton` 自製 aggro faction** + autoCalcStats + Aggressive/Foolhardy AI | override 洞窟/遺跡 interior |
| **Hell（極限）** | 前三者聯集（3171 base / 1863 ACHR），再加 bandit/militia/prisoner 敵性人口、可雇用商人傭兵（`*HIRELING` faction + `HirelingQuest*Topic` 沿用 vanilla 雇傭對白）、Hell 風惡搞屍體 activator（`FoolishMiner…`，model=HumanSkull.nif） | 全部 | 全部 + bandit attack-player | 全 Skyrim + 全 DLC |

**效能/相容手法**（如何不 CTD / 不卡）：① 純 record，無 runtime spawn 腳本（負載可預期）；② Hell 變體含 **`PopLandsMCM` quest**（flags=273 Start-Game-Enabled、PlayerRef ForcedReference alias）＝ MCM 設定選單，讓玩家**開關各城/各類人口、調密度**（這是這系列出名的「可調人口」核心）；③ Cities 重置 navmesh 避免尋路崩；④ 大量 base 必附 FaceGen（archive 帶 mesh/texture）。注意：與 Civil War / Immersive Patrols 一樣，**靜態置放無法隨劇情狀態改變**——密度給得起，戰略狀態給不起。

對照家族其他成員：本系列比 Civil War 變體**範圍更廣**（不只戰士、含全民生 archetype + 排程 sandbox），但機制同源；比 Immersive Patrols **更暴力、更不講究**（IP 是精選路線交叉，本系列是地毯式填滿）。

## ModForge meaning & gap

**已能生成（逐欄對得上，幾乎全中）**：
- NPC base（race/class/voice/outfit/combatStyle/factions/aiData）— landed `npcs.md`。
- 自製 Faction + 跨 faction 敵我 — landed。
- **PACK 10 模板**：sandbox / sleep / **travel** / usemagic / **patrol** / follow / escort / sittarget / activate / eat — 完整覆蓋本系列用到的 sandbox/eat/work/travel/wander/follow-boss/pack。**逐時段排程**（`Schedule` hour/minute/duration + 多 LocationTarget radius）與 alias-target radiant package 都已落地 — landed `npcs.md`。
- **LeveledNpc**（Lands/Hell 的難度撐法）— landed。
- 直接 ACHR 置放 + **PlacementSpec 六欄**（Scale / InitiallyDisabled / **EnableParent** / Lock / Ownership / **Count**）+ vanilla **cell override** + map marker — landed `world.md`。EnableParent/InitiallyDisabled 正好能補本系列缺的「按狀態開關」。
- **npcPatches[]** override vanilla NPC 的 packages（AI Overhaul 式）— landed。

→ **結論：本系列每一條低階機制 ModForge 都已具備。缺的不是能力，是「量產便利層」。**

**GAP（單一最重要、直指 #22）= 一個 macro-expansion 的高階 spec section，把「填滿這個聚落 / 這條路線」一句話展開成上述幾百筆低階記錄。**

`skillTrees:`（idea #20 Phase 3，landed `world.md`）已證明這條路在 ModForge 完全可行：在 `Build()` pass-0 `Expand*` 把高階指令展開成既有低階記錄、重用全部既有 pass、新建記錄碼極少。比照它做兩個對應 #22 的 section：

- `settlementPopulation:`（對 Cities）：給聚落 + 一組 archetype（乞丐/勞工/商人/旅店常客…）+ count + 一份「日程模板」（上工時段/用餐旅店/作息），macro-expand 成 unique base × N + 逐時段 sandbox/eat/work package × N + ACHR 置入指定 cell。
- `wildernessPopulation:` / `roadTravelers:`（對 Lands/Roads — #22 的「有人走的荒野/道路」）：給一份 archetype 字典（wandering merchant / pilgrim / adventurer+horse / refugee / mercenary）+ 路線/區域 + LeveledNpc 撐難度 + travel/wander package，macro-expand 成 base/leveled + 置放 + 路線 package。內建 `enableParent`/`gate GLOB` 旋鈕（補本系列「靜態無狀態」的弱點，也順手提供 MCM 式密度開關）。

這正是 #22 roadmap 列的「**聚落量產 spec section**」缺口——本系列就是該 section 要生出來的東西的活樣本：archetype 清單、排程 sandbox 結構、travel 路線 package、leveled 撐場、cell override 置放，全部已被本調查解析成可直接照抄的 pattern。Dungeons 變體額外示範敵性 `IhatePlayer`/pack-on-boss-death 的荒野怪物填充（#22「探索」面）。

風險（沿用 Civil War finding）：① plugin 體積/置放數爆漲快，量產層要給 count 上限與 navmesh-safe 置放紀律；② 大量 base 要 FaceGen 提醒；③ 靜態密度 ≠ 戰略狀態，別誤當模擬系統。

## Verdict

**可借鏡（高）**。機制 100% 已可生成，缺一個量產便利層；是 idea #22「聚落量產 spec section」最直接的設計藍本與 archetype 字典來源。家族其他成員見 [populated-skyrim-civil-war.md](populated-skyrim-civil-war.md)（戰士版）與 [immersive-patrols.md](immersive-patrols.md)（精選巡邏版）。
