# AnnoRim（Anno 式聚落建造／經濟經營；純 vanilla-Papyrus + 自訂海域 worldspace）

← [survey index](../index.md)

| 項目 | 值 |
| --- | --- |
| 類型 | **內容型 + 自釀輕系統**：自訂島嶼／港口 worldspace 內容，疊一層「建造→生產→稅收→貿易航線」的 Anno 式經營層。**非框架**（無公開 API、每棟建物一個 bespoke script instance）|
| Plugin | `AnnoRim.esp`（v1.2，50.5MB）；masters＝Skyrim+4 DLC + **`Sailable Ship.esm`**（船隻框架）|
| 規模 | quests=13 npcs=155 items=367 magic=12（多是 vanilla 卷軸/資源 MiscObject）loc=42（自訂 cell＋自訂 worldspace `AzurianSea`/`AzurianSeaPAST`）|
| 資產 | **無 BSA（全 loose）**，~900MB（textures 4.8G＋meshes 350M，含大量第三方靜態）；45 `.psc` 全附原始碼；51 `.pex` |
| 依賴 | SKSE **否**、PapyrusUtil/JContainers **否**、SkyUI/MCM **否**；僅 `Sailable Ship.esm` |
| 敘事價值 | 低（books=2 dialogue=0）；系統/經營價值中高（直指 #22/#24）|

## 是什麼

一座自建的海島貿易殖民地（自訂 `AzurianSea` 海域 worldspace＋港口/倉庫/貿易站/市政廳等自訂 cell＋155 個工人/商人 NPC），玩家在其上**蓋建物、跑生產鏈、收稅、開海運貿易賺錢**——把 Anno（模擬經營）的循環用**純 vanilla Papyrus** 手搓進 Skyrim。沒有任何 SKSE DLL、外部儲存 lib、MCM、SWF；**所有狀態＝真實 inventory 物品 + 少量 GLOB + script-instance property**，UI＝vanilla `Message.Show()` 是非框 + `Debug.Notification`。

## 關鍵架構

### 1. 建造系統＝預置 disabled 物件 + activator script 的 Enable/Disable（**#24 最直接先例**）
作者在 CK 裡**設計期就把每一階建物擺好但 disabled**，掛在 XMarker enable-parent 下；玩家去點的是一個 Activator，其 `ANNOBuildingScript`（extends ObjectReference）`OnActivate`：
- 檢查玩家背包裡的 5 種主資源＋選配（MiscObject/Ingredient/Potion）`GetItemCount` ≥ 需求 → `RemoveItem` 扣料 →
- `BuildingMarker.Enable()` / `RoadMarker.Enable()` / `MapMarker.Enable()`（點亮預置物件與地圖標記）→ 可選 `AddItem` 獎勵 → `Self.Disable()`（收掉施工牌）。
- 變體：`BuildWithGoldActivatorScript`（純付金）、`ANNOUpgradeBuildingScript`（升級＝`XMarkerToEnable.Enable()` + `XMarkerGroupDisable.Disable()` 換階）。確認建物是**切預置可見性、非 runtime `PlaceAtMe`**（對比 Tundra Defense 用 spawner PlaceAtMe）。
- 蓋建物的「Yes/No」用 `Message Property … .Show()` 讀 button index → **再度撞上 `MessageSpec` 無多按鈕欄位缺口**。

### 2. 資源／生產鏈＝真實 inventory 物品 + 定時 container（**無 StorageUtil/JContainers**）
- **生產**：`ANNOSurplusContainerDynamic` 每 12 in-game 小時對 source container 每種物品 `AddItem +N`（capped `MaxPerItem`）＝產出節點；`ANNORespawnContainerScript` `OnCellAttach` 過 N 小時 `Reset()` 補貨＝採集點。皆 `RegisterForSingleUpdateGameTime`/`GetCurrentGameTime` 驅動。
- **倉庫聚合**：`ANNOMasterContainerManager` 開啟時把多個 parent container 全數搬進一個 master container、關閉時歸還——因 Papyrus 陣列上限 128，用 **16×128 個 1D `Form[]`/`ObjectReference[]` chunk 手動分頁**（明擺著「沒有 JContainers 只好硬幹」）。
- **收入**：`ANNOTaxScript` 若玩家持有該建物 token 物品，每 7 天 `AddItem Gold001 +TaxAmount`＝被動收租（同 Real Estate 邏輯）。
- **貨幣/兌換**：`ANNOTokenExchangeScript` 把帶 keyword 的物品按重量比例換成 token MiscObject；`ANNOPaidActivator` 付金才能採集。

### 3. 貿易航線 + 突襲風險（依賴 Sailable Ship.esm）
船＝可移動 ObjectReference，沿 `HarborMarker_*` 航點被 script 操舵（keyword 選路，`ANNOShipRouteLinkedChains03`）。`ANNOShipTradeRun03` 航行 N 小時到具名目的地（Solitude/Windhelm/Solstheim/Camlorn…）賣貨換 `Gold001`，帶 `ShipRiskMultiplier`。`ANNORaidSystem`（extends Quest）用 `RaidChanceGlobal × RiskMultiplier` 擲骰 → partial/total/destroyed 三級損失、`RemoveItem` 扣貨並產出戰報字串。旅商船 enable/disable 週期進出港。此層綁外部框架，**純參考**。

### 4. 世界內容
自訂 `AzurianSea` worldspace（+PAST/DUPLICATE 變體）、~十餘自訂 interior cell（港口/倉庫/貿易站/市政廳/客棧/神殿）、155 NPC（多為 `zxSh*DUPLICATE` 的 Sailable-Ship 衍生工人/牲畜/商人）。這是本 mod 體積的主因（loose 900MB 資產）。

## 結論

對 ModForge：

- **可生成（今天就能）**：整套**靜態骨架**——資源 MiscObject、Activator + `scriptAttach` 掛 `ANNOBuildingScript` 等 controller（`Generator.Build.Scripts.cs` 已驗）、**預置 disabled REFR + enable-parent XMarker 的 Enable/Disable 建造 pattern**（placements + linkedRef/enable-parent + activator 全在能力域）、帶 respawn/surplus script 的 container、GLOB、自訂 cell/worldspace、NPC 置放。這是 survey 至今**最完整的 vanilla-Papyrus 經濟循環**，且狀態全走真實 inventory（引擎便宜、無外部 lib）。
- **需新支援 / 硬缺口（皆為已知、再確認）**：① **`MessageSpec` 無多按鈕選單**（建造 Yes/No 用 `Message.Show()` 讀 button——與 Real Estate/Tundra/Honed Metal 同一缺口，續押「優先補 `buttons:[]`」）；② **執行期經營迴圈**（生產計時、稅收、突襲擲骰、船隻操舵）irreducibly bespoke Papyrus，須隨附 controller `.pex`（同 Tundra/S&D「scaffold + 附 .psc」判決）。**無全新缺口**。
- **對 idea #24（遊戲內編輯器：施法擺物→快照→patch）**：AnnoRim 是「遊戲中蓋建物」的成熟出貨先例，但要點在於它蓋的是**預置好的 disabled 物件切可見**、**不是 runtime 自由 PlaceAtMe**。⇒ 它精確示範了 **#24 快照該吐出的產物格式**：disabled REFR + enable-parent XMarker + 資源/金幣 activator。#24 的「擺完快照成 patch」正好對得上這組 placement 記錄。
- **對 idea #22（聚落量產/開拓經營）**：與 Tundra Defense（PlaceAtMe + 波次守城）互補——AnnoRim 是**和平經濟半邊**，給 `settlements:` 補上具體原語：`buildables:`（activator+資源檢查+enable）、`production:`（surplus/respawn container 計時器）、`tax:`/`income:`（`RegisterForUpdateGameTime` 發金）、`currency:`/`exchange:`（keyword→token）。兩者合起來夾出 #22 的「建造+生產+防禦+收益」全循環。
- 對 Sofia：無關。
