# Populated Skyrim prison cells SE edition (Steelfeathers, 6033) — 監獄人口（家族最後一員）

Steelfeathers「Populated」家族的監獄變體：把 vanilla 各城監牢/地牢塞滿囚犯。家族總論與機制骨架見 [populated-skyrim-family.md](populated-skyrim-family.md)，戰士版見 [populated-skyrim-civil-war.md](populated-skyrim-civil-war.md)；本檔只記**與家族不同之處**。對應 idea #22「有人住的世界」的室內密度面。

## Scope / source

| archive (`~/skyrim_mods/hdd/`) | plugin |
|------|------|
| `Populated skyrim prison cells SE edition-6033-3-2.7z` | `Populated Skyrim Prisons Cells.esp`（240 records，master=Skyrim+Update）|

抽取：`7z x` → `~/skyrim_mods/unzip/`，`extract.sh`，tally 用 `dump`。記憶體鐵律遵守（CLI lazy overlay）。

## Classification

- Type：world population / 純置放 NPC。**無 quest、無 scene、0 對白**（`extract.sh`：dialogue=0 quests=0）。
- 敘事價值：**無**（唯一文字物件是 flavor 書 `rrrrAssassinNote "Sentence of Death"`，無教學、純佈景）。
- 系統價值：**中**（家族機制已 100% 解析；本檔只多示範一個「室內 random 填充」結構變體）。

## Key records & scale（`dump` tally）

| record | 數量 |
|--------|---:|
| Npc（base） | 196 |
| **LeveledNpc** | 16 |
| **PlacedNpc**（ACHR） | 16 |
| Cell（全 override vanilla 監牢/地牢） | 9 |
| Package | **1** |
| Faction | 1 |
| Book | 1 |

## Mechanism — **與家族同骨架，但置放走「兩層 template 抽卡」（本檔唯一新點）**

家族共同骨架仍在：自製 base 全指 vanilla FormID → ACHR 直接置入 **vanilla cell override**、無 controller / 無 runtime spawn。但置放層**不是家族慣用的「unique base 1:1 ACHR」**，而是**leveled-template 間接化**：

```
16× ACHR  →  16× thin carrier Npc (eeee*PrisonerRandom*, MajorFlags=262144=UseTemplate,
                                    Template -> kkkk<Hold>Prisoners)
          →  16× LeveledNpc (kkkk, 每個 ~6 entry；bis/tris/quater 變體把各 hold 的囚犯名單互相打亂)
          →  196× detailed base (eeee<Hold>Prisoner0N，逐個編好 race/class/voice/outfit/level/crimeFaction)
```

即**每個牢房槽位在進場時隨機抽一名囚犯**（carrier 用「Use Traits from leveled list」旗標），所以才會 196 base 卻只有 16 ACHR。這是家族其他成員沒有的置放結構——Cities 是 unique 1:1，Dungeons 是少 base 大量複用直接置放，本檔是 **carrier→LeveledNpc 抽卡**。

監獄專屬細節：
- **單一共用 package** `ooooPrisonerSandboxDefault`（sandbox，template=`01C254` DefaultSandbox，radius 128，hour 19:35 dur 1440）＝所有囚犯就地在牢房裡 sandbox，**無逐時段排程**（對比 Cities 的 1190 個排程 package）。合理：囚犯不上工。
- **`llllPrisonerFaction`**（HiddenFromPC）對 `000DB1 PlayerFaction` 設 `reaction=Enemy`＝囚犯敵視玩家（靠近會打）；detailed base 另帶 hold 的 `crimeFaction=02816E`。這層「敵視玩家的監獄 faction」是 Civil War/Cities 的中立路人沒有的。
- 9 個 CELL 全是 vanilla 監牢/地牢 override：Cidhna Mine、The Chill(Winterhold)、Castle Dour Dungeon、Riften Jail、各城 Barracks Jail…（含 FaceGen，archive 帶 mesh/texture）。

## ModForge meaning & gap

機制逐欄對得上家族 finding 已列的能力——**無新 gap**：
- 自製 base + Faction + 跨 faction 敵我（含敵視 PlayerFaction）— landed。
- **LeveledNpc** — landed。
- **carrier-NPC 走 Template=LeveledNpc（UseTemplate 旗標）抽卡** — 屬 NPC base 的 template/leveled 欄位，家族 finding 的 npc base 能力已涵蓋（`npcs.md`）；本檔證實這條 vanilla 慣用的「random 牢房居民」結構也是純既有低階記錄組合。
- ACHR 置入 vanilla cell override、單一 sandbox package — landed。

→ 仍是家族那一個結論：**缺的是 #22 的「量產便利層」macro section**，不是底層能力。本檔可作該 section 的一個 archetype 變體輸入：「**室內定點 + leveled 抽卡 + 就地 sandbox + 敵視玩家**」的填充模板（vs Cities 的排程市民、Lands 的 travel 路人）。

## Verdict

**可借鏡（中）**。機制 100% 已可生成、零新 gap；價值是替 idea #22 量產 section 補一個「監獄/室內 random 抽卡填充」archetype 模板，並收束 Populated 全家桶調查（最後一員）。
