# Immersive Wenches SE — 動態填充酒館/世界的活人口（spawn marker + LL + 環境 scene）

## Scope / sources

- Archive: `~/skyrim_mods/hdd/Immersive Wenches SE-595-1-6-0SE.7z`（15 MB；另含 `Immersive Wenches.bsa` 104 MB + Textures.bsa 14 MB，scripts/voice 都在 BSA 內）
- 解壓：`~/skyrim_mods/unzip/Immersive Wenches SE/`
- Plugin：`Immersive Wenches.esp`，1.4 MB，**2919 records**，masters = Skyrim/Update/Dawnguard/Dragonborn
- 抽出：`../game-data/mods/Immersive Wenches/`（books=46 dialogue=39 quests=15 npcs=646 items=36 loc=85 magic=319）
- 全 EditorID 前綴 `lalawench_`（作者 lalafaye）

姊妹/衍生（各一行帶過，不深挖）：
- **Deadly Wenches SE**（599，0.5 MB）：把 wench 變成可戰鬥的敵對/中立戰士，是 IW 的戰鬥職業層（necro/mage/mystic/ranger/2H… 對應 IW 的 122 SPEL/191 MGEF/14 Class/5 CombatStyle）。
- **Buxom Wench Yuriana**（598，277 MB）：單一語音獨立隨從，沿用 wench 美術，與本機制無關。
- 另有 IW/DW 的 CHS 中文修正包（只改 STRINGS/inline，機制相同）。

## Classification

- 類型：**世界人口填充（dynamic tavern/world population）+ 輕量內容層（ambient scene + radiant quest + 隨從/配偶）**
- Plugin：是，單一 ESP（重度依賴 BSA 內 Papyrus）
- 敘事價值：**中**（內容層是 generic radiant + ambient，無角色弧線；機制價值才是重點）
- 系統價值：**高**——這是「把活 NPC 鋪滿 vanilla 酒館」最完整的範本，直接對應 idea #22。

## Record shape（用 `dump` 數出來，未整載）

| 記錄 | 數量 | 角色 |
|------|------|------|
| Npc | 646 | wench 基底（按 race × 戰鬥職業 × Sultry 變體命名）|
| Package | 473 | **per-inn × per-時段 × per-role** 的 serve/sandbox/sleep/patrol |
| PlacedObject (REFR) | 415 | 主要是 **per-inn XMarker 生怪點**（`lalawenchXMarker_<city>_<inn><n>`）|
| PlacedNpc (ACHR) | 146 | 少量靜態放置（劇情/獨特 wench、被擄者）|
| Cell | 91 | **幾乎全是 Skyrim.esm/DLC 的 vanilla inn/dungeon override**（Bannered Mare、Bee and Barb、Winking Skeever…）|
| LeveledNpc | 34 | `lalawench_lvl_<race>` / `_sultry_` / `_Riften` / `_randompatrons` / `_bodyguard`…（生怪來源）|
| GlobalShort | 27 | MCM 開關 + radiant quest 計數器 |
| Quest | 15 | 4 個 Scene-host + 1 MCM + Captured/Enslaved/Misc/Follower/Spouse/HomeWork radiant |
| Scene | 4 | tavern ambient scenes（serving / cheers / request_perverts / thanks）|
| StoryManager node | 4 | scene 觸發器（見下）|
| Outfit 18・Class 14・Faction 33・Keyword 33・CombatStyle 5・Relationship 8・Perk 26 | | wench 角色組裝 |

## Mechanism pattern（核心，三層）

### 1. 人口填充 = vanilla cell override 放 XMarker → 腳本生 leveled wench

不是 SPID 分發、也不是逐個 ACHR 靜態放置：

- **override 91 個 vanilla inn/聚落/dungeon cell**，每個塞 3–6 個具名 `Static` XMarker（如 `lalawenchXMarker_whiterun_innbannered1..3`）。`cellrefs Bannered Mare(0x01605E)` → 3 個 placed object、**0 placed npc**：酒館本身只放生怪點，wench 是執行期生出來的。
- 生怪來源是 34 個 `lalawench_lvl_*` **LeveledNpc**（按 race / Sultry / 城市 / patron / bodyguard 分桶）。BSA 內的 Papyrus 控制器在 marker 上 `PlaceAtMe` 對應 LL，數量/開關由 27 個 GlobalShort（`morewenches`/`nobottles`/`moretravelers`/`noscenes`/`nojarlhouses`/`novamps`…，皆 MCM 綁定）控制。
- 另有 **"Wench Bottle" ALCH（每 race/職業各一）**：玩家喝下 → 腳本在身邊生一個對應 wench（手動生怪入口，呼應 `nobottles` 開關）。

### 2. 行為 = per-inn × per-時段 × per-role 手作 package

473 個 package 命名極細，例：
`winterhold_innfrozenheart_sandbox_barmaid` / `_nightservice` / `_night_dancedrunk` / `_sleep` / `_alldayservice` / `jarlshouse_sandbox` / `morningpatrol` / `afternoonmarket`，外加 12 個 generic `Followpatrolwench` 跟隨包。
→ 每間酒館手刻一套日程（白天端酒、晚上跳舞/喝酒、夜裡睡覺、清晨巡邏）。**這是工作量最大、最不可規模化的部分**。

wench NPC 本體（`npcdiag` Linda 0xD63）：Template 繼承（`0x0012F0`）、`AutoCalcStats`+Class（避開 autocalc-no-class 死 NPC 陷阱）、role keyword（`lalawench_potentialfollower` + `_magic`）、CombatStyle、若干 ActorEffect。package/faction 走 template 的 DefaultPackageList，不寫在個體上。

### 3. 內容層 = Story-Manager 觸發的 vanilla-style ambient scene + radiant quest

- **4 個 SCEN** 都是標準多 phase（6–9 phase）`Dialog + Package` action 的 scene，host quest 的 reference alias（Server/Patron/Wench/Pervert/Barmaid）以**條件 + `MatchingRefInLoadedArea`** 填充：`HasKeyword <wench>`、`GetIsVoiceType`、`IsInFurnitureState`、`GetInFaction`、`IsInInterior` 等。任何在載入酒館內、符合條件的 wench/patron 會被即時抓進 scene。
- `lalawench_Scene_serving`（0x10AAA9）甚至直接複製 vanilla **World-Interaction** 系統：quest `type=Misc event=ADIA filter=World Interactions\Tavern\`，即 wench 掛上 keyword 後就能參與原版酒館互動。
- scene **觸發靠 Story Manager**：SMBN `IWenches`/`IWenchesalways` → SMQN `RandomWenchesScenes` / `LoneWenchScene`，事件驅動隨機挑 scene quest，**無自訂 dispatcher**。
- radiant 內容：`Captured/Enslaved Wenches`（救/賣/留被擄 wench，用 GlobalShort 當計數器與 stage gate）、`Wench Followers`（persuade 對白招募）、`Spouse`、`HomeWork`、`Misc`（買藥水/付小費/租房對白），都是 generic、可重複觸發的輕內容。

## ModForge meaning & gap（對 idea #22）

idea #22「漂泊開拓慢活：異世界裡有人住的酒館/聚落」的人口+生活感，IW 是最貼近的現成藍圖。對照 landed（見 `workflows/feature-dev/landed/`）：

**ModForge 已能直接生成的（占 IW 機制 ~80%）：**
- placements（cell override 放 XMarker / 靜態 ACHR）、NPC build（race/class/voice/outfit/keyword/combatstyle/template/autocalc+class 配對）、packages（sandbox/serve/sleep/patrol）、LeveledNpc、Faction/Relationship、scenes（多 phase Dialog+Package，已 in-game confirmed）、dialogue INFO（含 alias 條件填充）、SM Kill/quest node 觸發（`story-manager-kill-recipe` / `dispatcher-magic-trigger`）、radiant quest stage+objective+GlobalShort gate、MCM（`mcm-helper-registration-recipe`）。
- 異世界場景下**更簡單**：自家 worldspace/cell 不必 override vanilla（IW 一半複雜度來自 91 個 vanilla cell override 與相容性），可直接在新 cell 放 marker + 生怪。

**Gap（IW 有、ModForge 缺便利層的）：**
1. **「生怪點 + LeveledNpc + 控制器」便利層**：IW 的核心是「marker 陣列 ←script→ leveled spawn」。ModForge 能各別生這些 record，但缺一個像 immersive-patrols finding 提的 `patrolGroups[]` 那樣的 **`spawnPoints[]` / `populate[]` generator**（給 cell + marker 數 + LL + 數量 global + 控制器腳本模板，一鍵產出）。這正是 #22 要的「把酒館填滿」原語。**最該補的一格。**
2. **per-location × per-時段 package 套組**：473 個手刻 package 不可規模化；ModForge 若提供「dailySchedule template」（白天 serve / 晚上 socialize / 夜睡 / 早巡）按 location 參數化展開成一組 package + 條件，能一舉省掉 IW 最痛的工作量。
3. **ambient scene 的 condition-filled alias + `MatchingRefInLoadedArea`**：ModForge scene 已能跑，但要確認能生「以 keyword/voicetype 條件 + MatchingRefInLoadedArea 抓現場 NPC」的 alias（IW 整個生活感建立在此），以及把 scene 掛到 **SM Quest node 隨機觸發**（而非單一 trigger）——這是「自然發生」感的關鍵。
4. World-Interaction（`event=ADIA filter=World Interactions\...`）piggyback：給 NPC 一個 keyword 就接上 vanilla 酒館互動——對 vanilla 場景超省力，但異世界無 vanilla WI 可借，需自建一套 WI 風格 quest（屬第 3 點延伸）。

**設計教訓給 #22：** 先做最小垂直切片——1 間異世界酒館 cell + 3 個 spawn marker + 1 個 wench-style LeveledNpc + 一組 daily-schedule package + 1 個 SM 觸發的 serving scene，就能驗證「有人住、會幹活、會互動」的活人口密度，再往聚落擴張。不必一開始就做 IW 的 91-cell 規模或 radiant 任務層。

## Verdict

**可借鏡（高）**——機制範本直接對應 idea #22，且 ~80% 已是 ModForge landed 能力；真正缺的是把零件包成「人口填充 generator」（spawnPoints + dailySchedule + condition-filled ambient scene + SM 觸發）的便利層。內容本身（generic radiant + 成人傾向 ambient）對 #22 無敘事價值，**只借機制、不借內容**。與 Sofia patch 無直接交集（IW 不改 vanilla follower topics，但 override 91 個 vanilla 酒館 cell，與任何也改這些 cell 的 mod 需做相容 patch）。
