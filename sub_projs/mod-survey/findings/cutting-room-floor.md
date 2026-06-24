# Cutting Room Floor (Arthmoor, mod 276) — 把「被砍掉的活人口」非破壞性地塞回 vanilla 聚落

## Scope / sources

- Archive：`~/skyrim_mods/hdd/Cutting Room Floor-276-3-1-11-1638226201.7z`（4.9 MB；另含 `Cutting Room Floor.bsa`（Papyrus）+ `- Textures.bsa`）
- 解壓：`~/skyrim_mods/unzip/Cutting Room Floor/`
- Plugin：`Cutting Room Floor.esp`，1.3 MB，**4643 records**，masters = Skyrim/Update/Dawnguard/HearthFires/Dragonborn/**USSEP**
- 抽出：`../game-data/mods/Cutting Room Floor/`（books=11 dialogue=161 quests=36 npcs=79 items=14 loc=141 magic=17）
- EditorID 前綴：**新內容一律 `CRF*`**；無前綴的（`ArgiFarseer`/`Hadring`/`OlfinaGrayMane`…）是 **vanilla.esm 裡被砍掉的休眠記錄，CRF override 重新啟用**
- 性質：**內容復原（content restoration）**——按 UESP「Unused NPCs / Unfinished Quests / Unobtainable Items」清單，把 Bethesda 做好卻沒接上的東西接回去

## Classification

- 類型：**vanilla 聚落人口/內容復原**（add living NPCs + 小聚落 + 小任務，非破壞性整合）
- Plugin：是，單一 ESP（Papyrus 在 BSA）
- 敘事價值：**中**——個別 NPC 有 vanilla 級小設定，但無角色弧；機制（怎麼非破壞地把人塞進既有聚落）才是 #22 要學的
- 系統價值：**高**——這是「在**既有**世界裡長出一個有人住的小聚落」最乾淨的官方風範本，正面對應 idea #22 的「異世界裡有人住」

## Record shape（`dump` 數出，未整載）— 新增 vs override 是重點

| 記錄 | 新增(CRF.esp) | override(masters) | 角色 |
|------|---:|---:|------|
| PlacedObject (REFR) | 3178 | 442 | 聚落建物/家具/clutter（多用 vanilla static），override 是往 vanilla cell 加東西 |
| PlacedNpc (ACHR) | 78 | 34 | **靜態放置**的活 NPC（非 spawn marker！）|
| Package | 72 | 17 | **per-NPC 手刻日程**（eat/work/sandbox/sleep/chopwood…）|
| Npc | 33 | 46 | 33 個新角色；**46 個是 override = 復活 vanilla 休眠 NPC** |
| Cell | 15 | 109 | 15 新 interior；**109 override**＝往 vanilla cell 加人加物 |
| Quest | 11 | 25 | 見下（新 quest 多是無文字的狀態機）|
| DialogTopic / DialogResponses | 11 / 23 | 60 / 120 | 新對白少，多是補 vanilla 已存在 topic |
| Location | 14 | 23 | 新聚落 LCTN + 把新 cell 掛進 vanilla location |
| Faction | 9 | 3 | 每聚落一個 Town/Services(vendor) faction（見下）|
| Relationship 6・Outfit 0(用 vanilla)・Static 13・Key 6・LeveledItem 12 | | | 角色組裝零件 |
| StoryManagerQuestNode | **1**（`CRFNode`）| 6 | 只加一個自家 SM 節點，其餘 additive 掛 vanilla 根 |

對照 Immersive Wenches：IW 是 415 個 **spawn marker + LeveledNpc**（執行期生怪）；CRF 是 **78 個靜態 ACHR + 33 具名 NPC**（編輯期就擺好的固定居民）。兩種「填人口」路線的極端：CRF = 手擺固定住民，IW = 動態生匿名人群。

## Mechanism pattern（核心，三件事）

### 1. 新聚落 = 「override 幾個 vanilla 外景 cell + 加新 interior + 手擺 ACHR + 每人 faction/日程」

以 **Frost River**（Hjaalmarch 一個 vanilla 殘樁聚落）為完整範例：
- **外景**：`CRFFrostRiverFarmEast/West/SE/SmithCell`（FormID `0x0093xx`，**住在 Skyrim.esm**）——是 vanilla 外景 cell 的 **override**，CRF 給它們指派 EditorID 並 additive 塞進建物 REFR。新 **interior** cell 才是真新增（`Rogen's House 0x1A8B79`、`Meadery 0x031377`、`Henrik's House 0x1A8B7A`）。
- **interior 內裝**＝純 placements：`cellrefs 0x031377` 全是 vanilla static/furniture（`029CB0`、`012FE7`…）＋少數 `CRF` 自家 static，標準佈置，無腳本。
- **居民**＝具名 NPC + 每人手刻日程。`npcdiag` Iddli Iron-Blood（`0x023906`）：vanilla race/class/voice/outfit、`AutoCalcStats`+Class（避開 autocalc-no-class 死 NPC 陷阱）、`Unique`、CrimeFaction=該 hold、加入聚落 faction（`FrostRiverFarmFaction 0x08F17F`）。Packages 直接列在個體上（**不走 template DefaultPackageList**，與 IW 相反）：
  - `CRFFrostRiverFarmEatMorning`（template `EatX`，hour 5 / 60min）
  - `CRFFrostRiverFarmWork`（template，hour 10 / 480min，多個 work-marker LocationTarget）
  - `CRFFrostRiverfarmIndoorSandbox`（fallback，NearEditorLocation radius 2048）
  → 早餐→白天工作（綁工作點）→室內 sandbox→（晚上）睡，一人一套。**最費工、最不可規模化**的部分，跟 IW 的 473 package 同病。
- **聚落 faction 三件套**：`CRFFrostRiverFaction`（鎮民歸屬）、`CRFServicesFrostRiverBlacksmith`（vendor faction，帶 sellBuyList/merchantContainer/vendorLocation/營業 8–20）、house faction（門禁/所有權）。Heljarchen/Stonehills 各複製同一套。

### 2. 非破壞整合＝一排無文字的「ChangeLocation 狀態機」quest（不直接砍 vanilla）

新 quest 11 個裡 **9 個是 `CRFChangeLocation0X` + `CRFInitializer`**，全部**無 log/objective 文字**——它們是 Start-Game-Enabled 的管理 quest，靠 startUpStage fragment 在執行期切換 cell/ref 的啟用狀態，避免硬改 vanilla：
- `CRFInitializer`（`0x0368FD`，flags=17 含 StartGameEnabled，filter `Arthmoor\`）：開局把所有編輯過的 quest/物件初始化到正確狀態。
- `CRFChangeLocation03 "Civil War swap at Frost River"`（`0x02600A`，RunOnce，event=CLOC）：用 3 個 reference alias（`CRFFarmMillImperials`/`...Sons` 帶 `AllowDisabled`/`AllowReserved` flag + LocationAliasReference RefType 過濾）依內戰歸屬 enable/disable 對應的帝國/風暴兵 ACHR。
- 其餘：`CRFChangeLocation01` 戰後解鎖塔門、`02` Vigilant 死亡、`04/05` 依別的 quest 是否完成 enable 物件、`06/07` MG08 後讓 Orthorn 回家、`08` 填 Riften 守衛 alias、`09` Ofrid/Vignar 恩怨 scene。
→ **pattern＝用「條件填充 reference alias + AllowDisabled flag」當開關，而非刪 vanilla record**。這就是「相容、可疊加」的官方做法。

### 3. 小內容＝復活的 vanilla cut quest + 一兩個聚落 radiant

- 25 個 **override quest** 是復活 vanilla 半成品（`MGR01/MGRRogue/MGR12 College`、`C01 Proving Honor`、`DB01Misc Cicero`、`CR03 Pelt Collection`…）——CRF 補完 stage/objective/INFO，不是自己寫故事。
- 唯一純新的聚落 radiant：`FreeformFrostRiver "Supply Line"`（`0x0681D4`）——meadery 主人 Signar（`0x023907`）給「送一箱蜜酒到某酒館」repeatable 任務，帶分支對白（`How's business?`→招募鋪陳→accept），目標城市隨機（Winterhold/Whiterun/Solitude）。**這就是「小聚落 + 一個輕量在地任務 + 在地對白」的最小單元**，正是 #22 想要的密度。
- 只新增 1 個 SM 節點 `CRFNode`，其餘觸發 additive 掛 vanilla SM 根。

## ModForge meaning & gap（對 idea #22）

#22「異世界裡有人住的聚落」要的「固定住民 + 在地生活 + 一點在地任務/對白」，CRF 是比 IW 更貼切的藍圖——因為 #22 多半是**手擺固定居民**（村莊／據點），不是動態生匿名人群。對照 landed（`workflows/feature-dev/landed/`）：

**ModForge 已能直接生成（占 CRF 機制 ~85%）：**
- placements（interior 內裝 + 往 cell additive 加 REFR/ACHR）、NPC build（race/class/voice/outfit/faction/crimefaction/**autocalc+class 配對**/Unique）、per-NPC packages（eat/work/sandbox/sleep，含 template 繼承 + work-marker LocationTarget）、vendor faction（sellBuyList/merchantContainer/vendorLocation/營業時段）、Relationship、新 interior cell + Location、radiant quest（stage/objective/branch dialogue）、SM 節點 additive 掛根、GlobalShort gate。
- **異世界更省事**：自家 worldspace 不必 override vanilla 外景 cell（CRF 一半複雜度來自此 + USSEP 相容），直接在新 cell 擺 ACHR + 內裝。

**Gap（CRF 有、ModForge 缺便利層的）：**
1. **「聚落 generator」**：CRF 每個聚落 = {幾個 cell + N 個具名居民 + 每人三件套 package + 三件套 faction（town/services/house）+ 內裝 placements}。ModForge 能逐件生，但缺一個 `settlement[]` / `village[]` 高階原語：給 cell + 居民清單（名字/角色/vendor?）→ 自動展開出 faction 三件套、vendor 設定、daily-schedule package、把居民 ACHR 擺進 cell。**最該補的一格**（與 IW finding 的 `populate[]` 同源，但 CRF 版偏「固定具名住民」而非「LL spawn」）。
2. **per-NPC dailySchedule template**：72 個手刻 package 不可規模化。一個「eat→work(綁 workmarker)→sandbox→sleep」參數化模板（給 hour/duration/location）能一鍵展開，省掉 CRF/IW 共同最痛點。
3. **非破壞 toggle 慣用法**：`CRFChangeLocation` 的「StartGameEnabled 無文字 quest + 條件填 reference alias + AllowDisabled flag enable/disable ref」是疊加式整合的標準手法。ModForge 已有 quest/alias/fragment 零件（`radiant-alias-package-byte-truths`、`dispatcher-magic-trigger`），但值得封一個 `enableState[]` / `worldEdit[]` 便利層（「依條件啟用/停用某 ref」）——#22 若要在世界推進中讓聚落「長出來/變化」會直接用到。

**設計教訓給 #22：** 先做 Frost River 的最小垂直切片——1 個聚落 cell（自家 worldspace，免 override）+ 3 個具名居民（vendor 1 + 一般 2，各帶 eat/work/sandbox/sleep）+ town/services faction + 1 個 `Supply Line` 式在地 repeatable 任務 + 招募/閒聊分支對白。這就驗證了「有名字、會幹活、會交易、有事可做」的固定住民密度，再往多聚落擴張。**CRF 給的是「固定住民聚落」的骨架，IW 給的是「動態人群」的填充——#22 兩者都要，但先抄 CRF 的固定骨架更穩。**

## Verdict

**可借鏡（高）**——官方風的「在世界裡長出有人住的小聚落」最乾淨範本，~85% 已是 ModForge landed 能力；真正缺的是把零件包成「settlement generator（居民 + faction 三件套 + dailySchedule package + vendor）」與「非破壞 enableState toggle」兩個便利層。內容本身（復原 vanilla cut content）對 #22 無直接敘事價值，**只借機制、不借內容**。與 Sofia patch 無交集（CRF 不改 vanilla follower topics），但它 **override 109 個 vanilla cell + 46 個 vanilla NPC**：任何也碰這些聚落/NPC 的 mod（含 #22 若放在 vanilla 場景）都需與 CRF 做相容 patch（如本機已有的 `AI Overhaul - CRF Patch`）。
