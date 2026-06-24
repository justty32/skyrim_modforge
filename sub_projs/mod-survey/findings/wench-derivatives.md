# Wench 衍生三件套 — Deadly Wenches（戰鬥人口）＋ Buxom Wench Yuriana（獨立隨從＋小任務 mod）

對照 sibling finding：[immersive-wenches.md](immersive-wenches.md)（IW = 動態填充酒館的活人口）。本篇只講「相對 IW，這三個是什麼、差在哪、對 #22 多了什麼」，**不重抄 IW 機制**。

## Scope / sources

| Mod | Archive | Plugin | 規模 | 與 IW 關係 |
|-----|---------|--------|------|-----------|
| **Deadly Wenches SE** | `Deadly Wenches SE-599-1-2-5SE.7z`（554 KB） | `Deadly Wenches.esp` 997 rec | npcs=737 quests=1 magic=7（無 dialogue/loc）| **master 依賴 IW**（refs into IW.esp = 4812）|
| **Buxom Wench Yuriana** | `Buxom Wench Yuriana-598-1-2-2.7z`（277 MB，含 BSA/voice/facegen）| `YurianaWench.esp` 3869 rec | books=51 dialogue=143 quests=17 npcs=117 cell=92 worldspace=7 | **無 IW master**（standalone），但保留 `lalawench_` 同源記錄 |
| **Less Buxom Yuriana Watcher Overhaul** | `…-88082-2-…rar`（2.9 GB）| **無 plugin** | 純 meshes/textures/DAR 動畫/FNIS | Yuriana 的美術替換包，override `yurianawench.esp` 的 facegen，**無敘事價值**（一行帶過）|

抽出：`../game-data/mods/{Deadly Wenches, Buxom Wench Yuriana}/`。CLI lazy overlay，未整載 Skyrim.esm。

---

## A. Deadly Wenches — 「戰鬥變體人口」靠 vanilla Leveled List 注入

### Classification
- 類型：**敵對/中立戰鬥 NPC 分發層（leveled-list injection）**，是 IW 的戰鬥職業 add-on，**硬依賴 IW**（用 IW 的 race/keyword/base effect 組裝戰鬥版 wench）。
- 敘事價值：**無**（純戰鬥 spawn，唯一 quest 是 MCM 控制器）。
- 系統價值：中——示範了**與 IW 截然不同的第二種人口填充機制**。

### Record shape（`dump` 數，未整載）
| 記錄 | 數量 | 角色 |
|------|------|------|
| Npc | 737（全 DW-new）| `DW_Enc<Faction>_<race><n>_<role>` 戰鬥變體（Bandit/Forsworn/Vampire/VigilantOfStendarr × race × melee/2H/magic/archer/tank/assassin/mage）|
| LeveledNpc | 120 = **91 vanilla override** + 29 DW-new | **核心**：override 91 個 vanilla 敵人 LL |
| LeveledItem/Spell | 67 / 35 | 戰利品與技能桶 |
| Mod* (ModAttackDamage/ModSpellMagnitude…) | 58 | 難度縮放 perk 用的 entry-point |
| Outfit 23・Perk 3・Spell 4・Armor 4・MagicEffect 3 | | 戰鬥組裝；6 個 spell/mgef 仍掛 `lalawench_` 前綴 |
| Quest | 1 | `lalawench_DWMCM`（純 MCM）|

### Mechanism（與 IW 對比，這是重點）
**IW = 在 vanilla cell 放 XMarker，執行期 script `PlaceAtMe` 生 wench。**
**DW = 改寫 vanilla 敵人 LeveledNpc，靠引擎既有 spawn 點自動生出戰鬥 wench。** 例（byte 已驗）：

```
[01A321:Skyrim.esm] LeveledNpc LCharBanditMeleeNordF   ← override 進 Skyrim.esm 的 LL
    lvln entry -> 039CF5/03CF5C:Skyrim.esm (原 vanilla 條目保留)
    lvln entry -> DW_WenchSubCharBandit_FemaleNord_melee  ×6 (additive 注入)
```

被改寫的 91 個全是 `LCharBandit* / LCharForsworn* / LCharVampire* / LCharSoldier* / SubCharBandit*`。任何貼這些 LL 的 vanilla 生怪點（土匪營、棄誓者、吸血鬼巢、內戰兵），現在有機率生出 DW 戰鬥 wench。**零 placement、零 package、零 scene** —— 純粹「改 LL 讓 vanilla 系統替你鋪人」。

> 對照：IW 自己也有 34 個 `lalawench_lvl_*` LL，但那是**新 LL** 給自己的 marker 用；DW 是**改 vanilla LL** 寄生 vanilla spawn。兩條路互補——IW 鋪「室內生活人口」，DW 鋪「野外戰鬥人口」。

---

## B. Buxom Wench Yuriana — 不是「單一隨從」，是隨從＋自帶任務/地牢的小型 quest-mod

> ⚠️ 修正 IW finding 的一行注記（「單一語音獨立隨從，與本機制無關」）：Yuriana **規模遠超單一隨從**。它是 standalone（無 IW master）的完整 quest-mod。

### Classification
- 類型：**獨立語音隨從 + 自帶 radiant 內容 + 跨 vanilla 世界的 placement 改造**。
- 敘事價值：**中**（17 quest 多為 generic「captured/enslaved wenches」radiant + 商人/服務對白，無強角色弧，但有 145 條 cloned voice）。

### Record shape
| 記錄 | 數量 | 角色 |
|------|------|------|
| PlacedObject (REFR) | 1648 | 大量靜態佈置（改裝酒館/自家地牢內裝）|
| PlacedNpc (ACHR) | 210 | 靜態放置 wench |
| DialogTopic/Responses | 203 / 208 | 真對白量（IW 才 39）|
| Npc | 117 | Yuriana 本體 + 被擄/服務 wench |
| Cell | 92 = **90 vanilla override** + 2 own | override vanilla 酒館；2 個自家 cell |
| Worldspace | 7（全 Skyrim.esm vanilla 引用）| 內容散佈在 Tamriel/各城/Solstheim，**無自建 worldspace** |
| GlobalShort 85・Package 52・Quest 17・Book 51・NavMesh 4 | | 完整 quest-mod 骨架 |
| voice | 145 `.fuz`（FemaleSultry/Commander/EvenToned/UniqueGhost… 8 voicetype）| 真語音 |

### Yuriana follower NPC（`npcdiag 0x000D70`）
- 自家 Race/Class/CombatStyle，Voice = `013AE0:Skyrim.esm`（FemaleSultry）。
- Flags = `Female, Essential, AutoCalcStats, Unique` + 明確 Class → **正確避開 autocalc-no-class 死 NPC 陷阱**（與 memory `autocalc-without-class-dead-npc` 一致）。
- Factions 含 `0x05C84D CurrentFollowerFaction` + `0x05C84E PotentialFollowerFaction` → **走 vanilla 隨從框架**（非 NFF/AFT，免框架依賴）。
- 47 Perk、本體 2 Package。`lalawench_` 前綴記錄（foodfaction/ghost/Rfaction）證明與 IW 同血緣，但已**自帶一份、不依賴 IW**。

> follower 部分對 #22 = **「standalone 語音隨從」的範本**（vanilla follower faction + Essential+Class+AutoCalc + 自家 race/voicetype + cloned `.fuz`），與 ModForge 已 in-game confirmed 的 voice-gen 管線（memory `voice-gen-interface-future`）完全對得上。其餘 90-cell override + radiant captured-wenches 是 IW 內容層的平行重做，**對 #22 無新機制**。

---

## ModForge meaning & gap（對 idea #22）

### 相對 IW，這批新增的唯一機制：**Deadly Wenches 的 vanilla-LL 注入**
IW finding 已涵蓋 marker-spawn / package / scene / radiant。DW 補上**第二種人口原語**：

| 機制 | IW | DW | #22 用途 |
|------|----|----|----------|
| 室內生活人口 | XMarker + 新 LL + script spawn | — | 酒館裡有人住、幹活 |
| 野外/戰鬥人口 | — | **override vanilla 敵人 LL，additive 注入自家 NPC** | 開拓路上會遇到的敵人/旅人多樣化 |

**ModForge 生成性：** override 一個既有 LeveledNpc 並 additive 加 entry，是 ModForge 已能做的 record 操作（與 worldspace/cell override 同類，見 landed）。**缺的便利層** = 一個 `leveledListInject[]` generator：給「target vanilla LL FormID + 要注入的 NPC/LL + 數量/權重」，自動產出保留原條目的 additive override。這比 IW finding 列的 `spawnPoints[]` generator **更輕、更該先做**——因為它不需要 marker/script，純資料。

> 但對 #22 的「異世界」場景：DW 機制依賴 **vanilla 敵人 LL 存在**。異世界自建 worldspace 沒有 vanilla LL 可寄生 → DW 模式不可直接移植，你得自己定義敵人 LL 並貼到自己的 spawn 點（那其實就退回 IW 的「自家新 LL」路徑）。所以 **DW 的價值是「在 vanilla Skyrim 上鋪人口」的範本，對純異世界 #22 反而是 IW 路徑更適用。**

### Yuriana 對 #22
無新人口機制（90-cell override + radiant 是 IW 內容層的重做）。唯一可借 = **standalone 語音隨從打包範本**（vanilla follower faction + 正確 NPC flags + cloned voice），補足 #22「異世界有名有姓、可同行的住民」這一格——這與 ModForge voice-gen 管線直接銜接，**屬借鏡而非新缺口**。

---

## Verdict

| Mod | 判定 | 理由 |
|-----|------|------|
| **Deadly Wenches** | **可借鏡（中）** | 唯一新機制 = vanilla-LL additive 注入（補 IW 的野外/戰鬥人口維度）；ModForge 已能生 override，缺輕量 `leveledListInject[]` generator。但**對純異世界 #22 用處有限**（無 vanilla LL 可寄生），主要適用「改造 vanilla Skyrim」情境。內容無敘事價值。**需相容**：DW override 91 vanilla 敵人 LL，與任何也改這些 LL 的 mod 衝突；且硬依賴 IW.esp。 |
| **Buxom Wench Yuriana** | **可借鏡（低）/部分可忽略** | 人口機制無新意（IW 內容層平行重做，90-cell override）。可借 = standalone 語音隨從範本（follower faction + Essential+Class+AutoCalc + cloned voice），呼應 ModForge voice-gen 管線。**需相容**：90 vanilla cell override。 |
| **Less Buxom … Overhaul** | **可忽略** | 無 plugin，純美術/動畫替換包。 |

與 Sofia patch 無直接交集。**淨增量結論：** 相對已調查的 IW，這三件套對 #22 只新增「vanilla-LL 注入」一種人口原語（DW），且它在異世界場景不如 IW 的自家-LL 路徑適用；Yuriana 提供的是隨從打包範本而非人口機制。**重點仍回到 IW finding 列的 generator 缺口。**
