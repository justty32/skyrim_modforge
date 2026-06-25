# Real Estate（Nexus 14408, v3.2 Final）— 把 Skyrim 變成「買房收租當地主」的房產經濟系統

調查 **Real Estate**（DDProductions83/Davide，v3.2 Final）：一套讓玩家「買下既有建築物的所有權 → 收租 / 收礦產 / 收農產 → 賣出」的**房產投資經濟 mod**。它是 [trade-and-barter](trade-and-barter.md) 之外另一條「玩家側經濟系統」路線，但機制完全不同——不是改商人定價，而是**用 GlobalVariable + 腳本化 Activator「告示牌」把整棟 vanilla 房子包裝成一個可買賣的資產**。對 ModForge 的意義集中在 idea #22「聚落」的**「所有權 / 收益」那一面**（[settlement-npc-expansions](settlement-npc-expansions.md) 補的是 staffing/店家結構，本 mod 補的是「這棟房子是誰的、產出多少錢」）。

## Scope / sources

| 項目 | 內容 |
|------|------|
| archive | `~/skyrim_mods/hdd/Real Estate v3.2 Final-14408-v3-2-1583378603.7z`（另存 `Real Estate - Core v3.1 Final-…zip` 與 `Real Estate - USSEP v3.1 Final-…zip` 兩變體）|
| 解壓 | `~/skyrim_mods/unzip/RealEstate/RE v3.2 Final/`（Core 另解到 `unzip/RealEstateCore/`）|
| plugin | `RE_RealEstate.esp`（482 records；masters = Skyrim.esm, Update.esm, **RE_RealEstate_Core.esp**）|
| master | `RE_RealEstate_Core.esp`（Core 變體只含這一支；是放 GlobalVariable 與共用 `RE_MainSafe` 容器的依賴容器 esp）|
| 出貨內容 | 1 esp + 11 `.pex`（**無 `.psc` source**）+ MCM 翻譯 `Interface/translations/RE_RealEstate_ENGLISH.txt` + 12 NPC 的 FaceGen mesh/texture + 自製告示牌 NIF（`RE_RealEstate/Sign/RE_Sign.nif`）+ 金錠堆 NIF |
| **無** | 無 SKSE `.dll`；無 PapyrusUtil / JContainers / JsonUtil / StorageUtil 任何呼叫（逐一 grep 11 個 pex 確認，零命中）|

工具：`7z x` 解壓；ModForge CLI `dump`/`questdiag`（lazy overlay，記憶體鐵律遵守）；機制 ground truth 取自 **`.pex` 的 `strings` 反推**（無 champollion 反編譯器，只能讀 pex 內的函式名/屬性名/字串字面值，**不是完整 source**——標 UNVERIFIED 處即此限制）。

## Classification

- **類型**：玩家側**房產經濟系統**（buy/sell property + 被動收租），框架性質但有薄敘事包裝（一條教學 quest）。
- **是否有 plugin**：✅ `RE_RealEstate.esp`（+ Core master）。
- **SKSE 依賴**：✅ **僅 SkyUI**（MCM 用 `SKI_ConfigBase`，見 Mechanism）。**無**自訂 SKSE DLL、**無** PapyrusUtil/JContainers——狀態全靠 GlobalVariable + 腳本屬性 + cell 所有權，純 vanilla Papyrus。
- **敘事價值**：**低**。一條 7-stage 教學 quest `RE_Quest "Becoming a Landlord"`（買書→開保險箱→買第一棟→更新帳本→收租→「打造你的房產帝國」），無角色弧線、無對白分支。
- **系統價值**：**高（對 idea #22 的所有權/收益面）**。是「玩家擁有並從一個地點獲利」這條機制最乾淨的 vanilla-only 範本。

## What it does

玩家在每棟「可買」的 vanilla 建築外會看到一塊**告示牌（Property Sign）**；啟動它跳出 message-box 選單可**買下該房產**（依城市與類型計價）。買下後：

- **房子（Houses）**：可進、可放東西、變成你的住所。
- **店鋪 / 旅店（Shops / Inns）**：每隔 N 天（MCM 可調）產生**被動收入**，錢自動進你的「地主保險箱（Landlord's Safe）」。
- **礦場（Mines）**：買下後（MCM 開關下）**敵人替換成礦工 NPC**，定期產出礦石/錠送進保險箱。
- **農場（Farms）**：定期產出農產，可選送保險箱或送某旅店。
- **賣出**：隨時可把房產賣回（賣價 = 買價 × MCM 的 `SellPriceMult`）。
- **MCM 全參數化**：基準價、各城市倍率、各類型收益倍率、收租週期、隨機收益、是否需要 perk 才能買、礦場敵人替換開關等（見 `RE_RealEstate_ENGLISH.txt` 的 page 結構：Houses/Shops、Inns/Specials、Mines/Farms、LocationMult、Compatibility、Help）。

## Mechanism（取自 `dump` + `questdiag` + pex `strings`）

### Record shape（`dump` tally，未整載）

| record | count | 角色 |
|--------|------:|------|
| PlacedObject | 144 | 多數是 `RE_Sign_<HouseName>` 告示牌 ref（每棟可買房一個）+ 礦工/載入畫面靜物 |
| PlacedNpc (ACHR) | 134 | 礦場買下後出現的礦工 + 其他 |
| Cell | 122 | vanilla cell override（塞告示牌 ref / 礦場內部） |
| **Message (MESG)** | 22 | **核心 UI**：buy/sell 選單 + 所有提示框 |
| Npc | 12 | `RE_Miner01..` 礦工 base（autoCalcStats + class，避開死 NPC 陷阱）|
| Quest | 6 | `RE_Quest`(教學)、`RE_MCM`、`RE_MsgReplacements`、`RE_VendorQuest`、`RE_Thief01 "Robbed!"`、`RE_Arena01` |
| Perk | 4 | `RE_RelationshipPerk_Rival/Enemy/Ally/Friend`（租客關係修正）|
| ModSellPrices / ModBuyPrices | 4 / 4 | vanilla **PERK entry-point**（房產自帶買賣折扣？UNVERIFIED 細節）|
| Activator | 4 | `RE_PropertySign` / `RE_FarmSign` / `RE_MineSign` / `RE_MainSafeAct_Safe`（腳本化告示牌 + 假保險箱）|
| Book | 2 | `RE_IntroBook "How to become a Landlord"`、`RE_Ledger "Landlord's Ledger"` |
| GlobalFloat/Short | 2(本 esp) | `RE_MineOreMult` / `RE_EnableRelationshipPerks`；**大量計價/收益 GLOB 住在 Core esp** |
| Weapon | 5 | `RE_OwnerReplacement` / `RE_LocationReplacement` / `RE_Product1..3Replacement`（**佔位 token**，見下）|
| MiscItem | 1 | `RE_LedgersQuill "Ledger's Quill"`（更新帳本的道具）|
| Key / Container / LeveledNpc | 1/1/1 | `RE_SafeKey` / `RE_MainSafe`(Core) / `RE_LCharMiner` |

### 1. 「可買房產」= 一塊**腳本化 Activator 告示牌 ref**（不是改房子本身）

可買的房子**不靠任何 keyword 標記、不掃描、不改房子記錄**。作者**手工在每棟 vanilla 房子外置入一個 `RE_PropertySign` 的 PlacedObject**，editorId 即房名：`RE_Sign_LeigelfHouse`、`RE_Sign_TheFrozenHearth`、`RE_Sign_GraveConcoctions`…（`dump` 可見 base = `RE_PropertySign` 的告示牌散佈各 cell）。每個 sign ref 掛 `RE_PropertySignScript`，**帶 4 個 per-instance 屬性**（價格 / 地點 / 收益型別覆寫——具體欄位 UNVERIFIED，但 base script 有 `__PriceOverride` 屬性可逐房調價）。

→ **「發現可買房」= 純靜態置放一個帶腳本的 Activator**，零 runtime 掃描。礦場用 `RE_MineSign`、農場用 `RE_FarmSign`（各自的 script 帶 ~46 個屬性）。

### 2. 買 / 賣 = `RE_PropertySignBaseScript` 的 **state machine（`Owned` / `Not owned`）**

`RE_PropertySignScript`（72 屬性，掛在 sign ref）繼承 `RE_PropertySignBaseScript`（基底，含計價/所有權邏輯）。確認到的函式（pex strings）：

- **`OnActivate(player)`** → 跳 message-box；`Buy` / `Sell` 函式切 state（`GotoState`，state 名 `Owned` / `Not owned`，pex 字面值 `Owned` / `Not owned` 確認）。
- 計價：`GetBasePrice` 讀 `RE_HouseBasePrice` / `RE_ShopBasePrice` / `RE_InnBasePrice` GLOB × **`GetLocationMult`**（依房子所在城市讀 `RE_WhiterunMult`/`RE_RiftenMult`/…/`RE_SkyrimMult` GLOB，靠 `Game.GetPlayer().IsInLocation(<XxxLocation>)` 判定城市，pex 見 `WhiterunLocation` 等屬性）× `RE_PriceMult` 全域倍率，並夾在 `RE_MinPrice` 之上。
- **`RE_UsePerks` GLOB 開啟時**：買房前 `Game.GetPlayer().HasPerk(...)` 檢查（house=Haggling / shop=Merchant / inn=Investor / special=Master Trader，見翻譯檔 `RE_UsePerksInfoText`），不足跳 `RE_NeedPerkMsg`。
- 計數：每買一棟把對應 `RE_HousesOwned` / `RE_ShopsOwned` / `RE_InnsOwned` / `RE_SpecialsOwned` GLOB +1。

### 3. **所有權變更 = 用「佔位 Weapon token」+ 引擎 ownership**（巧妙繞道，可借鏡）

`ChangeOwnership`（base script）不直接設玩家所有權，而是操作那組**佔位 Weapon**：`RE_OwnerReplacement` / `RE_LocationReplacement` / `RE_Product1..3Replacement`（全是 0-value、隱形用的 token Form，給腳本當「可替換的 actor/location/product 引用容器」）。base script 帶 `_Owner`(actor[]) / `_Location`(location) / `_PropertyDeed` 屬性，買下時把 sign 綁定的房子/容器所有權改成玩家、產出型別綁到 product token。

保險箱用更直接的 vanilla API：`RE_SafeScript`（掛 `RE_MainSafe` 容器）在 `OnInit` `SetActorOwner(Game.GetPlayer().GetActorBase())`，並 `OnItemRemoved` 重設——這是標準 **XOWN/SetActorOwner** 防盜。`RE_FakeSafeScript`（掛 `RE_MainSafeAct_Safe` Activator）是「假保險箱」門面，啟動轉接到真容器（`Activate`），並用 `RE_SafeKey` Key + `RE_NoKeyMsg` 做上鎖門面。

> ⚠️ **UNVERIFIED**：`ChangeOwnership` 內部到底呼叫 `Reference.SetActorOwner` / `SetFactionOwner` / `Location` API 哪一個——pex strings 只見 token 屬性與 `ChangeOwnership` 函式名，未見明確的 `SetLocationOwner` 字面值（PropertySign base 無此 import；只有 SafeScript 有 `SetActorOwner`）。token-replacement 是「把房子的 OwnerReplacement ref 換成玩家」的間接法，細節需反編譯才能定論。

### 4. 收租 = `RegisterForUpdateGameTime` / `OnUpdateGameTime` 被動 timer

`RE_PropertySignScript` 買下後 `RegisterForUpdateGameTime`（pex 見 `Buy - Registered for update` / `Sell - Unregistered for update`）；到期觸發 `OnUpdateGameTime` → `ChangeIncomeLevel` / 加錢（pex 見 ` Adding Income (GDP = ` / ` Income = ` debug 字面值）。收益 = 買價 × 該類型 `RE_HouseIncomeMult`/`RE_ShopIncomeMult`/`RE_InnIncomeMult` GLOB × `RE_IncomeMult`，週期由 `RE_IncomeRate`（MCM「每 N 天」），可選 `RE_RandomIncome` 在 min/max 間抖動。錢 `AddItem(Gold001)` 進 `RE_MainSafe`。

帳本（master ledger）：玩家用 `RE_LedgersQuill`（`RE_UpdateMLScript`，`OnEquipped`）`UpdateCurrentInstanceGlobal` 把所有權狀態寫回 `RE_IncomeAvailable` 等 GLOB——即「**用一個 quest instance + InstanceGlobal 當總帳**」的 pattern（quest = `RE_Quest`）。

### 5. 租客關係 = 4 個 **PERK** 當 ±rank 修正

`RE_RelationshipPerk_Rival(-1)` / `_Enemy(-3)` / `_Ally(+3)` / `_Friend(+1)`（Name `RE_RP -1/-3/+3/+1`）。`RE_PropertySignScript` 有 `SetRelationshipRank` / `SetTenantsRelationship`，買下店鋪/旅店後可把店員（租客）對玩家的 relationship rank 調整，受 `RE_EnableRelationshipPerks` GLOB gate。

### 6. 礦場敵人替換

`RE_MineSignScript`（46 屬性）帶 `Miner0..Miner19`（20 個礦工 ref 屬性）+ `_Enemy` 屬性 + `EnableMiners` / `Enable` / `Disable` 函式。買下礦場且 MCM `RE_MineReplaceEnemies` 開啟時：`Disable` 原敵人、`Enable` 礦工 ACHR（賣出反向）——即 **enable-parent 式的 ref 開關**，非生怪。

### 7. MCM = **SkyUI `SKI_ConfigBase`**（古典 MCM，非 MCM-Helper）

`RE_MCMScript` extends **`SKI_ConfigBase`**（pex 確認），是手寫腳本式 MCM（`OnPageReset` / `OnConfigOpen` / `AddSliderOptionST` / `AddMenuOptionST` / `OnSelectST`…），**不是** MCM-Helper 的 config.json 宣告式。頁面/選項文字全在 `Interface/translations/RE_RealEstate_ENGLISH.txt`（`$RE_*` key）。含 cheat 頁（`Add Safe Key` / `Set RE_Quest stage`）。設定值寫進那批 `RE_*Mult`/`RE_*BasePrice`/`RE_IncomeRate` GLOB。

### 8. 教學 quest `RE_Quest`（`questdiag 0x0038C0`，已驗）

7 stages（0/10/20/50/55/60/70）+ 6 objectives（買書→開保險箱→買第一棟→用 quill 更新帳本→收租→「打造你的房產帝國」）。type=ThievesGuild、StartUpStage。3 個 alias：`ML`(QuestObject+UsesStoredText, 指 `RE_Ledger`)、`Quill`(指 `RE_LedgersQuill`)、`Main Safe`(ForcedReference 指 Core esp 的容器)。`RE_IntroBook`（`RE_IntroBookScript`, `OnRead` AddItem 帳本+quill 並推進 quest）。另有 `RE_Thief01 "Robbed!"`（保險箱被偷的小事件）與 `RE_Arena01`（UNVERIFIED 用途）。

## ModForge relevance

逐機制對照 ModForge 既有能力（凡「ModForge 不能」都已 grep `src/ModForge.Core/` 核實）：

| RE 機制 | ModForge 對應 | 狀態（核實） |
|---------|---------------|------|
| 計價 / 收益 / 倍率 GLOB（`RE_*Mult`/`*BasePrice`/`*Owned`）| `GlobalSpec`（`Spec.Globals.cs`，`spec.globals[]`，含 Short/Float/Int）| ✅ landed |
| 教學 quest（stages + objectives + alias QuestObject/ForcedReference）| QUST（`Spec.Quests.cs`，含 InstanceGlobals / GlobalWrite fragment sugar）| ✅ landed |
| 腳本化 Activator 告示牌（ACTI + 多屬性 script-attach + `OnActivate`）| ACTI + script-attach + fragments | ✅ landed（[skillTrees](world.md) 的 node activator 即此 pattern）|
| 保險箱 `SetActorOwner` / 告示牌房產所有權（XOWN/owner）| **placement `OwnershipSpec`（XOWN owner+rank）**（`Generator.Build.Placements.cs:143`、`Spec.World.cs:58`）| ✅ landed（靜態 XOWN）；**runtime `Reference.SetActorOwner` 切換**需走 script fragment（可生成，無一級 sugar）|
| 4 個 relationship PERK ±rank | PERK（含 entry-point，[perk-entry-points](perk-entry-points.md)）+ RELA | ✅ landed |
| 被動收租 timer（`RegisterForUpdateGameTime`）| script-attach + fragment（任意 Papyrus 可寫進 attach script）| ✅ 可生成（非宣告式 sugar，需手寫 fragment）|
| `RE_LedgersQuill` 總帳（quest InstanceGlobal）| `Spec.Quests.cs` InstanceGlobals / GlobalWriteSpec | ✅ landed |
| 礦工 base（autoCalcStats+class）+ enable/disable ACHR | NPC（[npcs](npcs.md)）+ placement + enableParent | ✅ landed |
| MCM（買賣 / 倍率 / 週期 / 開關 / cheat 頁）| **MCM 生成（`McmGen.cs`/`Generator.Build.Mcm.cs`，`Spec.Mcm.cs`）** | ✅ landed，**但生成的是 MCM-Helper `config.json` 風格**，非 RE 的 `SKI_ConfigBase` 手寫腳本（兩者最終都出 SkyUI MCM，功能等價；ModForge 走宣告式那條，見 [mcm-helper](mcm-helper.md)）|
| **多按鈕 message-box 選單**（Buy / Sell / Cancel 等 22 個 MESG，買賣 UI 核心）| **`MessageSpec` 只有 `EditorId/Name/Description`**（`Spec.Items.cs:42`）——**無 menu buttons / ITXT choices 欄位** | ⚠️ **GAP（已核實）**：ModForge 的 MESG 只能生「純文字提示框」，**生不出「多按鈕選單型 message-box」**。RE 的買/賣分支全靠這種選單。`Spec.Identity.cs` 的 `AcquireText` 是寫死的 yes/no prompt，非通用多按鈕。 |
| 「一棟房子 = 一個資產」的 spec 抽象 | **settlements macro（`Spec.Settlement.cs`/`Generator.Settlements.cs`）** | ⚠️ 部分：macro 涵蓋 residents+routine+shop faction，**不涵蓋「property ownership / buyable / 收租」維度**（見 roadmap）|

## Roadmap implications

**1. 最高價值 GAP — 多按鈕 Message-box 選單（MESG menu buttons）。** 已核實 `MessageSpec`（`Spec.Items.cs:42`）只有 `EditorId/Name/Description`，無按鈕欄位。任何「啟動物件→跳選單→依選擇分支」的互動（買/賣、是/否/取消、多選服務）目前只能靠 fragment 手刻或 vanilla MESG override。RE 是這類 UI 的典型代表（22 個 MESG，全是買賣/提示選單）。**建議給 `MessageSpec` 補 `buttons: []`（ITXT/menu-button + 對應 quest stage / fragment 分支）**，並讓 ACTI/Book 的 OnActivate fragment 能讀回 `MenuResult`。這個缺口同時解鎖大量「對話框驅動」mod（不限房產）。

**2. settlements macro 的「ownership / 收益」面（idea #22 的另一半）。** [settlement-npc-expansions](settlement-npc-expansions.md) 與 [populated-skyrim-family](populated-skyrim-family.md) 補的是「住滿人 + 店家結構」；RE 補的是**「這個地點屬於誰、產出多少資源」**。`SettlementSpec` 目前無此維度。若 #22 要做「玩家開拓並擁有一個聚落」，可從 RE 借三個原語：(a) **per-asset 計價/收益 GLOB 組** + (b) **被動收益 timer fragment 模板**（`RegisterForUpdateGameTime`→`AddItem(Gold)` 進指定容器）+ (c) **token-replacement 式所有權切換** 或更乾淨的 runtime `SetActorOwner` fragment。這些 ModForge 全能生（GLOB/script-attach/placement 都 landed），缺的是**把它們打包成 `ownership:` / `income:` 宣告層**的便利層——和家族 finding 反覆得到的同一結論（缺量產 sugar，不缺能力）。

**3. 可直接複用的 pattern（無需新支援）：**
- **「腳本化 Activator 告示牌」= 給任意 vanilla 地點掛一個可買/可互動掛鉤**（不改該地點記錄、純 additive 置放一個帶 per-instance 屬性的 ACTI ref）。這是「在既有世界上疊一層玩家系統」最低衝突的做法，[skillTrees](world.md) 的 in-world node 已證明 ModForge 完全能生。
- **token Form 當「可替換引用容器」**（`RE_*Replacement` weapons）：讓 script 屬性指向一個佔位 Form、runtime 改其指向——繞過「Papyrus 不能動態建 Form」的限制。值得記入 conventions 當一個可重用招式。
- **「一個 quest instance + InstanceGlobal 當總帳」**（`RE_Quest` + ledger quill）：ModForge 的 `InstanceGlobals` 正是這個 pattern 的一級支援。

**風險 / 相容**：RE override 大量 vanilla cell（122 cell，在每棟可買房外塞告示牌 ref）——與任何也改這些 cell 的 mod（城市重做、JK's、ETaC…）需相容 patch（RE 自帶 USSEP patch 變體即為此）。與 Sofia patch 無交集。

## Verdict

**可借鏡（高，限「玩家側經濟/所有權系統」與 #22 的收益面）**。RE 是 vanilla-only（僅 SkyUI）實作「買房收租」的乾淨範本，機制原語（GLOB 計價、script-attach timer 收租、XOWN/SetActorOwner 所有權、relationship PERK、MCM、教學 quest）**ModForge 幾乎全已 landed**。**唯一硬缺口是 MESG 多按鈕選單**（`MessageSpec` 已核實無按鈕欄位）——這是買賣 UI 的命脈，也是跨多種互動 mod 的通用缺口，建議優先補。內容本身（教學 quest）無敘事價值，只借機制配方。最小垂直切片：1 棟可買房（告示牌 ACTI + OnActivate 多按鈕 MESG + XOWN 切換 + 一個收益 GLOB + timer fragment + MCM 倍率滑桿），驗「能買、會收租、能賣」。
