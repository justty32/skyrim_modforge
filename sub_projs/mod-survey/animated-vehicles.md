# Animated Vehicles 調查 — Animated Ships + Animated Carriage

兩個同作者（`zx` / Vicn 資產）的「動態載具」mod，主題成對：海面上會動的船、會跑的馬車。
本篇合併調查，重點放在**「東西怎麼動 + 玩家怎麼搭」**，以及對 ModForge（JSON spec → .esp 生成器）哪些是資料層可生、哪些靠動畫資產或 Papyrus。

- 來源：`Animated Ships-110260-1-2-0`、`Animated Carriage-112397-1-1-0`
- plugin：
  - Ships：`AnimatedShips.esl`（1101 records，light master）
  - Carriage：`AnimatedCarriage.esm`（1660 records，master）＋ 每條路線一個 `ACLine_<Hold>.esp`（範例 `ACLine_Whiterun.esp`，97 records，純 placements）

---

## 一、Animated Ships — 做什麼 + 怎麼運作

**做什麼**：在 Solitude / Windhelm / Dawnstar / Winterhold / Riften 等港灣外海，讓玩家看到大型帆船、長船、Katariah、沉船等在海面上「航行 / 上下浮動」，多數還可登船站到甲板上隨船移動，部分港船附帶 trader/fisherman vendor faction。

**核心機制 = 自帶動畫的 NIF + 腳本同步 NPC，不是 AI package 在移動船**：

1. **船 = `Activator` base，model 指向自帶動畫的 NIF**。例：
   `[00081D] Activator zxActDistantShipLong01 → model: Clutter\Vicn\AnimatedShip\Distant\shiplongboat01.nif`，
   掛 `script: zxShp_DistantShiptBase`。NIF 內部有 NiControllerSequence（航行 / 上下浮動 / 沉船三類路徑：`Distant/` `NarrowPath/` `UpDown/` 三套 NIF + `*_BASE.nif`）。**船體的「動」完全是 NIF 內嵌動畫驅動，引擎只是播放它，沒有任何 ref 在被腳本搬動。**
2. **Papyrus `zxShp_DistantShiptBase`（extends ObjectReference）只做三件事**：
   - 維護一個 `ShipMarker`（隱形 XMarker），每個 tick `MoveToNode(self,"ShipCenterNode")` 貼到船 NIF 的中心節點 — 因為 NIF 在動，所以要用一個跟得上的 marker 當「船現在的真實座標」。
   - `SetNPConShip()`：把 linked-ref 串起來的乘客 Actor，用 `SplineTranslateToRefNode(self,"RidingShipNode<idx>", …)` 黏到甲板節點上（NodeMax 個座位，輪流取模）；NPC 飄離或落水就 `ResetPassengerPosition` 重貼。
   - `OnActivate`：玩家在 `fRidableHeight` 內就 `MoveToNode(self,"RidingShipNodePlayer")`，並播船板嘎吱環境音（`AMBShipCreakBaseLP.Play`）。
   - 距離分級 `RegisterForSingleUpdate`（>32000 停、>9000 待命、近距才同步），純效能節流。
3. **排程 / 隨機出現**：`zxShp_SingleShipManagerQuestScript`（Quest）用 `GameHour` 切 5 個時段 bitmask，比對每艘船的 valid time zone；`RandomShipsPerDay` 用 global `zxASgChanceShips` 擲骰決定今天這艘船航不航；不符就 `DisableNoWait` + `DisableLinkChain`，符合就 `Enable` + `EnableLinkChain`。`zxShp_TriggerLoadForDistantShip`（觸發 Activator）在 `OnLoad/OnCellAttach` 喚醒對應船的 `UpdateShip()`。
4. **4 個 Package（`zxShPlayIdleOnShipboard*` / `zxShCreatureOnShipboard01` / `zxSHFencerSneakingOnShip`）全是 template `Skyrim.esm:0x0654E2`**（vanilla 站樁/idle 模板）— 只是讓甲板上的 NPC 站好/偷偷摸摸，**不負責船的移動**。

**玩家怎麼搭**：走到船邊 activate → 被 spline 黏到甲板 player 節點 → 之後船的 NIF 動畫帶著你「看起來在動」（你其實是貼在隨 NIF 動的節點上）。沒有真正的物理載具。

---

## 二、Animated Carriage — 做什麼 + 怎麼運作

**做什麼**：在 Tamriel 各 Hold 之間，沿預鋪路徑出現「馬拉著走的馬車」radiant 事件（含囚車、商隊、婚禮、葬禮、衛兵、敵對劫車等變體），馬車跑到終點站變回靜態擺設、乘客下車；玩家也能 activate 上車跟著跑。

**核心機制 = linked-ref 路徑節點鏈 + `TranslateToRef` 平移 + radiant quest 工廠生成**：

1. **路徑 = 一串放在世界裡的 marker Activator，用 `GetLinkedRef()` 串成 linked list**。
   在 `ACLine_Whiterun.esp` 裡看得最清楚：大量 `PlacedObject`，base 是 `zxACCartMarker01/02`（`[0009DC]/[0009DE]`，model 是 `CarriageMarker0X.nif`），**每個 ref 都帶 `linkedRef → 下一個節點`**。`StartMarker → … → ENDMarker` 即一條路線；節點還能掛 `kwAlternativePath` 第二條 linked-ref 做 50% 機率分岔。
2. **`zxAC_StartMarkerScript`（marker 上的腳本）= 觸發器**：`OnCellAttach`/距離 <6000 時依日夜 + global 機率（`zxACgEventChanceDay/Night`）擲骰，從 FormList 抽一個 radiant 旅程 Quest，`Reset()`→`Start()`，並把自己當 start point 餵給它（`SendStartPoint(self)` + HoldLocation + CrimeFaction + sprint flag）。`gCarriageLine` global 當「這條線正在用」的鎖。
3. **`zxAC_RqBaseScript`（旅程 Quest 腳本）= 移動本體**：
   - `CreateCart()`：`StartPoint.PlaceAtMe(...)` 從 cart-type/horse-type FormList **動態生成**一台 cart Activator ref（`zxACCartA05Shadowmere` 之類，base 也是 Activator + 自帶動畫 NIF）。
   - `GoToNextMarker()` → `CartRef.TranslateToRef(NextMarker, fSpeed, …)`：**引擎平移把整台 cart ref 搬向下一節點**。
   - cart 的 `OnTranslationAlmostComplete` → 回呼 `UpdateCartMoving()` → `SetNextMarker()`（沿 `GetLinkedRef()` 走下一格）→ 再 `TranslateToRef`。如此沿節點鏈一格一格走，這是**引擎驅動的真實 ref 移動**，跟船完全不同。
   - 到 ENDMarker：`GenerateStaticCartAt`（放靜態 cart 擺設 `zxACCartStatic*`）+ `GenerateHorseMarkerAt` + `GenerateLivingHorseAt`（`PlaceAtMe` 一匹真馬 Actor + `SetOutfit` 換皮 + `SetVehicle(HorseMarker)`），cart ref `Disable`，乘客 `ExitPassenger` 下車；全程結束 `RemoveCarriage` 把所有臨時 ref `Delete`。
4. **乘客系統（多個 alias 腳本）**：
   - `zxAC_PassengerAliasScript`：用 `SetVehicle(CartRef)` 把 Actor 綁到 cart（vanilla 載具機制），加友善 faction、設 crime faction；監聽動畫事件 `ExitCartEnd` 下車、`RemoveCharacterControllerFromWorld` 處理 ragdoll；被玩家/衛兵攻擊就 `StopCartAtCurrentLoc` 停車並轉敵對。
   - `zxAC_PlayIdleOnCartAliasScript` + `zxAC_MgEPlayIdleOnCart`（ActiveMagicEffect）：用一個 **Spell（FormList 隨機抽）** 當載體，magic effect 觸發時 `Debug.SendAnimationEvent(MyRef, "IdleSitCrossLeggedEnterInstant" / "IdleJarlChairEnterInstant" / …)` — **車上坐姿全是 vanilla idle 動畫事件名，不是自製 HKX**。另有 `Idle Property IdleCartDriverSway` 等一票 Idle records 做車身搖晃姿態。
5. **`zxAC_ManagerQuestScript`（單例 manager Quest）= 工廠 + 工具庫**：`GenerateCart/StaticCart/HorseMarker/LivingHorse`、`MovePassengerTo/EnablePassenger/SetPassengerOn/ReplaceVehicle/ExitPassenger`、token 計數等，全靠 keyword + FormList 查表（cart type → horse type → 具體 base 的多層 FormList）。

**玩家怎麼搭**：activate 跑動中的 cart（`bRidable`）→ `MoveOnCart` 把玩家 spline 到 `RidingNode<seat>` → 用 `SetVehicle` 綁定 → 隨 `TranslateToRef` 一起被搬到終點。

---

## 三、共通模式 vs 差異

兩者**頂層思路相同**：載具 base 都是 **`Activator` + 自帶動畫 NIF**，乘客都用 **`SplineTranslateToRefNode` 黏到 NIF 的命名節點**（座位 / 甲板），都有**距離分級的 `RegisterForSingleUpdate` 效能節流**，都用 **global + 隨機擲骰**控制出現，坐姿都靠 **vanilla idle 動畫事件**。

最關鍵的差異在「**船 vs 車到底誰在動**」：

| 面向 | Animated Ships | Animated Carriage |
| --- | --- | --- |
| 載具「動」的來源 | **NIF 內嵌動畫**（船自己跑，ref 座標不變）；marker 反過來追船的 `ShipCenterNode` | **引擎 `TranslateToRef`** 真正搬動整台 cart ref 沿節點走 |
| 路徑定義 | 無真正路徑，動畫即「航線」；只有 ShipMarker 同步 | **linked-ref 節點鏈**（`PlacedObject` + `linkedRef` 串接，`kwAlternativePath` 分岔），路線 = 一個 `ACLine_*.esp` |
| 載具 ref 生命週期 | 靜態 placement，靠 Enable/Disable + Disable/EnableLinkChain 開關 | **動態 `PlaceAtMe` 生成、跑完 `Delete`**（radiant 臨時 ref） |
| 觸發 | `SingleShipManagerQuest` 時段 bitmask + 每日機率 | start-marker 距離觸發 + 日夜機率 → radiant 旅程 Quest |
| 乘客綁定 | 只 `MoveToNode` / spline 貼節點（無 `SetVehicle`） | `SetVehicle(CartRef)` vanilla 載具綁定 + spline 貼節點 |
| AI Package | 4 個，全 idle 模板 0x0654E2，**僅站樁** | ~29 個，多數 idle 模板 0x0654E2（applaud/wound/reveler/wedding 等情境站樁）＋ 1 個 Flee 模板 |
| Papyrus 角色 | 同步 marker + 黏乘客 + 排程 | 路徑遍歷 + 生成/回收 + 載具綁定 + 乘客行為 |
| 馬 | 無 | 終點 `PlaceAtMe` 真馬 Actor + `SetOutfit` 換色 + `SetVehicle` 綁 cart |

**串法總結**：兩者都不是用 vanilla Travel/Patrol package 在驅動移動。Package 在這兩個 mod 裡只是「讓 NPC 站在載具上擺對姿勢」的配角。真正的移動是：船＝美術（NIF 動畫），車＝Papyrus（`TranslateToRef` + linked-ref 鏈）。

---

## 四、關鍵 record 與資產（代表性）

- **載具 base（兩者）**：`Activator`，model 指向自帶動畫 NIF。
  - Ship：`[00081D] zxActDistantShipLong01` → `shiplongboat01.nif`（+ `zxShp_DistantShiptBase` script）
  - Cart：`[0009C2] zxACTESTCartAShadowmere2NS "Carriage"` → `Carriage02_Shadow.nif`（含 `activationSound`/`loopingSound`，keyword 標 cart-type/horse-type/`zxACCartIsRunning`）
- **路徑節點 base（Carriage 專有）**：`[0009DC] zxACCartMarker01` → `CarriageMarker01.nif`（marker 美術，放置後靠 `linkedRef` 串）。
- **路徑 placement（Carriage）**：`ACLine_Whiterun.esp` 的 `PlacedObject`，每筆 `placed obj → base 0009DC:AnimatedCarriage.esm @ (x,y,z)` + `linkedRef → 下一節點`。**這就是「一條路線 = 一堆帶 linkedRef 的 placement」的純資料表達**。
- **Idle / 動畫掛接**：
  - Ship NIF 命名節點：`ShipCenterNode` / `RidingShipNode0..N` / `RidingShipNodePlayer`（乘客座位）。
  - Cart NIF 命名節點：`RidingNode<seat>` / `HorsePosition`（馬位）。
  - 坐姿 = vanilla 動畫事件名（`IdleSitCrossLeggedEnterInstant`、`IdleJarlChairEnterInstant`…）＋ 一組 `Idle` records（`IdleCartDriverSway` 等）。
- **vendor（Ship）**：`zxSHSolitudeTraderFaction` 等 18 個 Faction 帶 vendor flag + `merchantContainer` + `sellBuyList`（沿用既有 vendor-faction 模式）。
- **內嵌動畫 vs 引擎驅動**：船體擺動 / 航行＝**NIF 內嵌（havok/NiController）**；車身位移＝**引擎 `TranslateToRef`**；車身搖晃姿態 + 兩者坐姿＝**vanilla idle 動畫事件（資料層）**。

---

## 五、對 ModForge 的參考價值

整體判斷：**「東西在動」這件事兩條路都不在 ModForge 的資料生成射程內**——船靠美術（NIF 動畫，屬 havok-blender 線），車靠一支不小的 Papyrus 狀態機。但**支撐它們的骨架幾乎全是 ModForge 該能生成的純資料**。

### 可生成（ModForge 資料層已涵蓋或接近）

- **載具 / marker 的 base records**：`Activator`（model NIF path、keyword、activation/looping sound）、`Static`、`Container`、`Faction`(vendor)、`Outfit`、`FormList`、`Keyword`、`GlobalShort`、`Idle` 引用 — 都是現成 spec record 類型。
- **vendor faction**（Ship 港船的 trader/fisherman）：與既有 vendor-faction 例子同型，直接可生。
- **NPC 上載具的站樁 Package**：兩者的主力 Package 全是 template `0x0654E2` 的 idle 模板。ModForge 的 `packages[]` 已是 template-driven（見 `docs/spec/SPEC-packages.md`）；只要先 `packagediag Skyrim.esm 0x0654E2` 拿到 slot schema，這類「在某 ref 上站樁/演 idle」就能掛上（與 SM/scene PlayIdle 筆記 `scene-playidle-recipe`、`dispatcher-magic-trigger` 的 idle/magic-effect 串法同源）。
- **idle 動畫事件掛接（坐姿）**：透過 magic effect 腳本 `Debug.SendAnimationEvent("Idle…EnterInstant")` 觸發，用的是 **vanilla 動畫事件名**——這層「掛接邏輯」屬腳本，但「掛哪個 idle、配哪個 magic effect / spell / FormList」是純資料，可比照既有 magic/scene 筆記生成。

### 需新支援（ModForge 目前缺，但屬資料層、值得補）

- **placement 的 `linkedRef`（最重要）**：Carriage 的整條路線就是「一串帶 `linkedRef` 的 `PlacedObject`」。目前 `docs/spec/SPEC-world.md` 的 `placements[]` 支援 `base/cell/worldspace/rotation/scale/persistent/enable-parent`，**但沒看到 linked-ref 欄位**。補一個 `linkedRef`（+ 具名 keyword 變體如 `kwAlternativePath`）就能讓 ModForge 直接生成「路徑節點鏈」這種資料結構——這跟 navmesh 筆記（`programmatic-navmesh`）、placement 既有能力是同一層，是高價值的小增量。
- **dynamic-spawn 的工廠資料**：cart/horse 的「cart-type → mode → horse-type 多層 FormList 查表」是純資料（巢狀 FormList），ModForge 已能生 FormList；要完整重現只差把這種「查表用 FormList 樹」當 pattern 記錄即可。

### 純參考（不打算讓 ModForge 生）

- **船體航行 / 浮動動畫**：NIF 內嵌 NiControllerSequence，屬美術資產（havok-blender 線），ModForge 不生 NIF。船的「移動」整個落在這裡。
- **載具移動狀態機（Carriage）**：`zxAC_RqBaseScript` 的 `TranslateToRef` + `OnTranslationAlmostComplete` 路徑遍歷、`PlaceAtMe`/`Delete` 生命週期、`SetVehicle` 綁定、ragdoll/敵對/停車分支——是一支完整的手寫 Papyrus radiant 系統，超出 spec 描述能力，屬「需手寫腳本 + 用 ModForge 生資料骨架」的混合工作流（可參考 dialogue/scene 筆記裡「ModForge 生 records、手寫 .psc 補邏輯」的既有分工）。
- **效能節流 / 排程細節**（距離分級 update、時段 bitmask）：純腳本實作層，參考即可。

### 一句話結論

ModForge 能把這兩個 mod 的**整副骨架**（Activator/marker base、FormList 查表、vendor faction、站樁 package、idle 掛接、以及——若補上 `linkedRef`——整條路徑節點鏈）當資料生出來；真正讓船浮動 / 讓車跑的那一層，分別歸給**美術（NIF 動畫）**與**手寫 Papyrus 狀態機**，ModForge 只負責餵料。
