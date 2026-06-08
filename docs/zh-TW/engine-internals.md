# 引擎內部原理 — ModForge 生成程式碼背後的「原因」

那些生成器（`src/ModForge.Cli/Build.cs`）必須遵守的、非顯而易見的 Skyrim/Mutagen 機制。這是從（現已封存的）迭代日誌中提煉出的長青設計知識；症狀→修復的查詢請參閱 [lifelike/gotchas](lifelike/gotchas.md)，欄位逐一的規格文件請參閱 [SPEC-index.md](SPEC-index.md)。

## 核心原則：覆寫**不會**繼承省略的子記錄

當你覆寫一筆原版記錄（相同的 FormKey）時，引擎**不會**將你的稀疏覆寫合併到主記錄上——它會直接採用你所撰寫的記錄，並將所有你省略的欄位**重設為預設值**。這個單一事實是大多數 cell/worldspace 錯誤的根源：

- 省略某 worldspace 的 `LandDefaults` → `DefaultWaterHeight` 從 Tamriel 真實的 `-14000` 重設為 `0` → 所有低於海平面的地形都被淹沒（「整個世界都在水下」）。
- 省略某內部 cell 的 `LightingTemplate` → 房間漆黑一片。

因此，覆寫必須**重新宣告內聯環境資料**。`Build` 透過 `CopyCellEnv`（水面高度/類型/材質、光照 + 模板、區域、影像空間、音樂、音響空間、遭遇區域、位置、所有權、天空/天氣）和 `CopyWorldspaceEnv`（陸地/水面預設、水體形式、氣候、地圖、邊界、父級、光照）來做這件事。這兩者故意**跳過**本地化的 Name 以及龐大的子結構（cell/worldspace 區塊樹——我們自己建立；原版引用保留在主記錄中、不重新宣告，因此不會膨脹也不會衝突）。

> cell 上的 `WaterHeight = FLT_MAX` **不是**錯誤——它是「使用 worldspace 預設值」的哨兵值。

## 本地化字串的地雷（在 Linux 上無頭執行時）

Skyrim.esm 是**本地化**的：`TranslatedString` 欄位（Name / Description / BookText）是字串索引，其文字存放於 BSA 內的 `.STRINGS` 中。解析這些字串需要遊戲的 plugins.txt / 載入順序封存列表——**在 Linux 上無頭執行時不存在**。

任何觸及這些字串的操作都會拋出 *"Could not determine plugin listings path"*：
- 對原版記錄執行 `DeepCopyIn` → 傳入 `TranslationMask { Name=false, Description=false,
  BookText=false }`（我們無論如何都會覆寫這些欄位）。
- 對 cell 執行 `GetOrAddAsOverride` → 改為建立一個**手動的相同 FormKey 覆寫**
  （`new Cell(vanillaFk, SkyrimRelease)`），只複製內聯欄位，並將 Name/Lighting 保留為
  null，使其從主記錄繼承。
- `find` 的 Name 解析 → 盡力解析，遇到第一個失敗即停止，僅以 EditorID 搜尋。

EditorID 和 FormID 是內聯儲存的、永遠可讀——這就是為什麼每個 `find`/`*diag` 都以 EditorID 為鍵，而非顯示名稱。

## Cell GRUP 的放置是以 FormID/網格為鍵

### 內部 Cell GRUP 公式

Skyrim 將內部 cell 巢狀為 `CellBlock(type 2) → CellSubBlock(type 3) → Cell`，並**依 FormID** 分組：

```
block = id % 10
sub   = (id / 10) % 10          # decimal, 24-bit ID
```

（已透過遍歷 Skyrim.esm 驗證：WhiterunBanneredMare `0x01605E` = 十進位 90206 → block 6，sub 0。）
**對覆寫至關重要：** 一個放在錯誤 block GRUP 中的原版 cell 覆寫永遠不會與主記錄 cell 匹配，因此引擎會**靜默忽略它**（放置的物件 + 光照都不套用）。以 `cellblk` 確認。

### 外部 Cell 網格 → GRUP

外部 cell 巢狀為 `WorldspaceBlock(type 4, /32 grid) → WorldspaceSubBlock(type 5, /8 grid) →
Cell(grid x,y)`：

```
cellGrid = floor(worldPos / 4096)        # CellSize = 4096
block    = FloorDiv(cellGrid, 32)
sub      = FloorDiv(cellGrid, 8)
```

除法**必須向 −∞ 取底**，而非像 C# 的 `/` 那樣截斷（例如 C# 中 `-41 / 8 == -5`，但底數為 `-6`）。否則負座標會落入錯誤的 GRUP。（已對照 Tamriel 驗證：cell (7,−41) → block (0,−2)，sub (0,−6)。）

### LAND record 的陷阱（平坦地形生成）

生成平坦 LAND records 時已在遊戲內確認三個 bug（2026-06-03）：

1. **`Landscape.Flags` 必須包含 `VertexNormalsHeightMap`（0x01）。** 若無此 DATA flag，引擎會跳過整個 VHGT/VNML 資料——cell 沒有地形碰撞，玩家會直接穿透落下。

2. **Z=0 ≈ Skyrim 的海平面。** VHGT `Offset=0` → 地形在 Z=0 = 海面高度。請使用 `Offset = height / 8`（例如 height=4000 → Offset=500 → Z=4000，安全地高於水面）。

3. **ESL 外掛不能包含 LAND records。** Skyrim 引擎會靜默忽略從 ESL（輕型）外掛載入的地形資料——外部地形載入路徑只讀取完整的 ESP/ESM。有 `cells` 的 spec 必須設定 `"esl": false`。`validate` 指令會強制此項。

4. **室外 NAVM 用 `WorldspaceNavmeshParent`，而非 `CellNavmeshParent`。** 規則：室外 cell → `WorldspaceNavmeshParent { Parent = worldspace.FormKey }`；室內 cell → `CellNavmeshParent { Parent = cell.FormKey }`。任一 parent 設為 null 都會 CTD（Mutagen 寫入時 `NullReferenceException`）。

5. **NavmeshGrid 格式：** 以列優先（row-major）順序，每個網格子 cell 的格式為 `[uint32 triCount][ushort idx0]...[ushort idxN]`。`GridDivisor` = N 代表一個 N×N 網格；`MaxDistanceX/Y` = cellWidth/N（每個子 cell 的遊戲單位）。對於一個獨立的 2 三角形平坦 cell：`GridDivisor=1`，`MaxDistance=4096`，grid bytes = `02 00 00 00 00 00 01 00`（8 bytes，一個包含兩個三角形的 cell）。

## 程式化 navmesh（NAVM + NAVI）——遊戲內已確認 2026-06-03

為自訂 worldspace 產生能讓引擎正常載入（不 CTD）的 navmesh，經歷了一段漫長的除錯歷程。決定性的事實（透過遊戲內進入 `MFTestWorld` 確認，並以解碼 Vigilant.esm——一個真實 CK-finalize 過的自訂 worldspace mod——作為 known-good 對照）：

1. **NAVI 是全遊戲唯一、所有外掛都去 OVERRIDE 並累加合併的單一 record：`Skyrim.esm:0x00012FB4`。** 全遊戲只有這一個 `NavMeshInfoMap`（vanilla 有 15,462 筆條目）。引擎會把**每個外掛的 `0x12FB4` NVMI 清單 additive 合併**（Vigilant 的 override 只列它自己的 897 筆 navmesh，不是 vanilla 的 15,462 筆）。所以：
   ```csharp
   var navi = new NavigationMeshInfoMap(FormKey.Factory("012FB4:Skyrim.esm"), SkyrimRelease.SkyrimSE);
   // ...add only YOUR NavigationMapInfo entries...
   mod.NavigationMeshInfoMaps.Add(navi);   // additive override; conflict-safe for new worldspaces
   ```
   **建立一筆全新的 NAVI record（`AddNew()`）是最致命的錯誤**——它會產生第二個、孤立的 `NavMeshInfoMap`，其 runtime pathing cell 為 null，引擎會在 navmesh-init 工作執行緒（約 5 秒 uptime）的 `NavMeshInfoMap::InitItemImpl`（`mov edx,[rcx+0x10]`, rcx=0）CTD。崩潰 log 的 `R14 = NavMeshInfoMap [0x0A000804]`（我們的 rogue record）對比 `[0x00012FB4]`（vanilla master，當我們的 NAVM 完全沒有 NAVI 條目時）就是線索。

2. **NVNM 照 Mutagen 寫出的原樣出貨——不做 byte patch。** 權威格式（xEdit wbNVNM）：`Version(u32=12) | Magic(4) | ParentWorldspace(FormID) | {GridY,GridX i16}|ParentCell | Vertices | Triangles | EdgeLinks | DoorTris | CoverTris | GridDivisor(u32) | MaxX/YDist | Min XYZ | Max XYZ | NavMeshGrid`。VertexCount 位於 offset 16，**在 8-byte parent 之後**。（先前「把 parent 從 offset 8 移出」的 hack 是在 stale-ESP 測試下的誤診；刪掉 parent 既會讓記錄錯位，又會剝離 init 階段所需的 worldspace 連結。）

3. **「Magic」常數是 `0xA5E9A03C`**（檔案位元組 `3C A0 E9 A5`）。它出現在兩個地方，都對照每一筆 vanilla/Vigilant navmesh 驗證過：
   - `NavigationMeshData.CrcHash`（NVNM offset 4）。
   - `NavigationMapInfo.Unknown2`（NVMI 緊接在 `Parent Worldspace` 之前的 4-byte 欄位）。
   任一留成 0 都是錯的（它本身不會 CTD，但要符合引擎的預期）。

4. **真實 navmesh 不是 island。** 一般 mesh 的 `NavigationMapInfo.Island` 保持 null（`Is Island = 0`）——已對照 Vigilant 全面確認。（設成 island 沒幫助，也不是 CK 的做法。）

5. **三角形邊鄰接**：`EdgeLink_n` 是跨越本地頂點 n 與 n+1 之間那條邊的鄰居三角形索引，邊界邊則為 `-1`。對於共用 V0–V2 對角線的 2 三角形 quad：T0 `EdgeLink_2_0 = 1`、T1 `EdgeLink_0_1 = 0`、其餘 `-1`。

6. **與 NAVM 相同的 parent 規則**適用於每筆 NVMI：室外 → `NavigationMapInfoWorldParent { ParentWorldspace = ws.FormKey, ParentWorldspaceCoord = (gridX,gridY) }`；室內 → `NavigationMapInfoCellParent { ParentCell = cell.FormKey }`。

> 一個讓上述問題被掩蓋好幾輪迭代的打包陷阱：MO2 安裝 zip 必須是**扁平**結構（plugin 在 zip 根目錄、`Seq/` 同層）。一個遺留在 zip 根目錄的較舊 `.esp` 被裝成而不是剛 build 的那個，所以「還是 crash」一再其實是 *stale* plugin。永遠要 `unzip -l` + `md5sum` 把 zip 內的 esp 跟剛 build 的對拍。

## AI 套件以模板為基礎驅動

每個具體的 `Package` 都透過 `PackageTemplate`（`IFormLink<IPackageGetter>`）引用一個原版的**程序模板**，其 `Data` 是一個以模板**具名插槽索引**為鍵的 `IDictionary<sbyte, APackageData>`。具體套件的 `Type = Package`；模板本身的 `Type = PackageTemplate`（永遠不要撰寫後者）。以 `packagediag <Skyrim.esm> <templateFormId>` 探索任何模板的插槽結構；以 `pkgsbytemplate` 尋找具體範例。

幾個容易踩到的插槽細節：
- `LocationFallback` 的二進位形狀由其 **`Type` 列舉決定，而非 C# 類別** — `new LocationFallback()` 帶 `Type = 0` 會靜默地以 `LocationTarget` 形式寫出。請務必設定 `Type = NearSelf`（錨定在角色目前位置；不需要外部連結）。永遠不要用 `NearEditorLocation`——它需要由 CK 設定的 Editor Location，而透過 Mutagen 建立的 NPC 缺少此設定。
- UseMagic 插槽 0/1 是繼承的 `APackageData` 佔位符——保持不動（所有 46 個原版具體 UseMagic 套件均如此）。

### PACK 資料插槽對應表

| 模板 | 插槽對應（索引 → 含義，原版預設值） |
|---|---|
| **Sandbox** `0x01C254` | 0 Location · 1 AllowEating · 3 AllowSleeping · 4 AllowConversation · 5 AllowIdleMarkers · 6 AllowSitting · 7 AllowWandering · 14 UnlockOnArrival · 25 PreferredPathOnly · 27 RideHorseIfPossible · 29 Energy · 31 AllowSpecialFurniture |
| **Travel** `0x016FAA` | 0 Place (Location) · 2 RideHorse · 4 PreferPath |
| **Patrol** `0x017723` | 0 Start (SingleRef) · 1 Radius (150) · 2 Repeatable · 4 StartAtNearest · 6 RideHorse · 8 StaticPathing |
| **Follow** `0x019B2C` | 0 Target (SingleRef → player) · 1 MinRadius (128) · 2 MaxRadius (256) · 4 Accompany · 6 RideHorse · 8 NeedLOS |
| **Escort** `0x023B73` | 11 Target (SingleRef → player) · 3 Destination (Location) · 2 NumFollowers (1) · 4 WaitDistance (512) · 5 FollowerMin (120) · 6 FollowerMax (256) · 13 RideHorse · 15 PreferPath · 17 RunIfBehind (500) |

### 巡邏路線拓撲存在於放置引用的連結引用中

路線**不在**套件裡——每個標記 REFR 都有一個連結引用（null 關鍵字）指向下一個；透過將最後一個連回第一個來形成迴圈。`LinkedReferences` **分別**存在於 `IPlacedObject` / `IPlacedNpc` 上（沒有共用的可設定介面——須轉型為具體類型）。任何連結引用的來源，以及套件的延遲錨點所指向的任何放置，都必須被強制設為**持久（Persistent）**——引擎可能會丟棄某個被其他東西錨定的臨時引用。

### 跨 Cell 的 Travel 是內容關卡，而非記錄關卡

一個與原版位元組完全相同的 Travel 套件，在門傳送時會被靜默拒絕，除非該 NPC 具備**市民身份**（`crimeFaction` + 城鎮派系成員資格 + `unique: true`）。誠實的警告：這三者是一起加入的；哪一個單獨起決定性作用尚未驗證（假設：CrimeFaction 為主，Unique 有助於引擎跨 cell 轉換時追蹤 AI 狀態）。

## 魔法效果時序

- **即時**效果（duration 0）必須使用 `["NoDuration","NoArea"]` 且**不加 `Recover`** — `Recover` 會在效果結束時還原數值，而對即時效果而言這等同*立即*還原，淨效果歸零（「無法治癒的治癒」錯誤）。
- `Recover` 只對**有時限的**強化效果正確（例如 60 秒內 +50 生命值）。
- 保持 `baseCost` 較低：autocalc 會將 baseCost × magnitude，因此高 baseCost 會產生荒謬的魔力消耗。
- 戰鬥 SPEL 需要一個 `equipType`（EitherHand `0x013F44`），否則 NPC 無法將其裝備到手上，並會靜默地永遠不施放。

## 克隆原版模型

沒有 `.nif` 的記錄在物品欄中可以正常存放，但**在任何將模型附加到場景的互動中都會崩潰**（武器裝備、書本 3D 閱讀）；`additem`/drink 是安全的（不載入模型）。給武器/書本/雜項/藥水一個 `template` 引用；`Build` 執行 `DeepCopyIn`（關閉本地化字串遮罩），這會**保留你自己的 FormKey**（記錄保留在你的插件中；模板的子表單成為指向其主記錄的 FormLinks），然後覆寫身份/屬性。對於藥水，它會先清除克隆的 `Effects`，以免規格效果與模板的效果疊加。

## 自訂對話的顯示

必須有兩個旗標，否則主題永遠不會出現：宿主 **Quest** 需要 `StartGameEnabled`（+ 一個用於排序競爭對話的 `Priority` 位元組），否則它會保持休眠且其對話永遠不會載入；**DialogBranch** 需要 `Flag.TopLevel`，否則主題是子分支而非選單選項。將 INFO 的 `ResponseData` 保留為 null（以使用你自己的 Responses），將 `Prompt` 保留為 null——選單行來自 `topic.Name`；硬編碼的 Prompt 會錯誤標記選單。

## 技能進入點帶有一個隱藏的分頁計數位元組

一個進入點技能效果（`PerkEntryPointModifyValue`，例如 ModAttackDamage ×1.2）有一個 `PerkConditionTabCount` 位元組（PERK 進入點 `DATA` 子記錄的第 3 個位元組）。它是進入點的**固有條件分頁數量**——函數所評估的攻擊者 / 目標 / 武器情境——**而非**你撰寫的條件數量。引擎會據此調整每個分頁的條件陣列大小；當計數為 **0** 時，一個撰寫在索引 0 上的 `PRKC` 條件分頁會溢出該陣列、破壞一個指標，並在**「Loading Files」期間發生硬性 CTD**（TESForm 查找雜湊表中因垃圾 FormID 造成的存取違規）。

這是一個純粹的 **「Mutagen 可容忍、引擎致命」** 的二進位錯誤：Mutagen 讀回真實的 `Conditions` 列表並忽略計數位元組，因此 `dump` / 往返 / 連結解析 / ESL 標頭看起來都乾淨——只有執行時的解析器會崩潰。它會一直隱藏到第二個插件改變了記憶體佈局，然後以「兩個 mod 一起崩潰」的報告形式浮現（2026-05-31 從一份 CrashLoggerSSE log 找到根本原因；參閱 [lifelike/gotchas](lifelike/gotchas.md)）。

計數是每個進入點固定的，永遠是 `1`/`2`/`3`，絕不為 `0`——且永遠 ≥ 存在的 PRKC 分頁數量（原版可以自由地設例如計數為 3 卻只有一個或零個分頁）。`Build` 從一張由 Skyrim.esm 的 375 筆 PERK records 提取出的表格設定此值（`ModAttackDamage`/`ModSpellMagnitude`/`CalculateWeaponDamage` = 3；`ModArmorRating`/`ModBuyPrices` = 2；`ModSkillUse`/`ModFallingDamage` = 1；未列出的 → 2）。透過掃描原版來重新生成表格：讀取每個 `IAPerkEntryPointEffectGetter` 並以 `EntryPoint → PerkConditionTabCount` 分組。

## Mutagen API 陷阱

- `AddNew()` 需要 `using Mutagen.Bethesda;`。
- 當輸出檔名與模組的 ModKey 不同時，以 `BinaryWriteParameters { ModKey = ModKeyOption.NoCheck }` 寫出。
- 類型陷阱：`DialogResponse.ResponseNumber` 是 `byte`；`PackageDataInt.Data` 是 `uint`；`LeveledItem.ChanceNone` 是 `Noggog.Percent`（0–100）；cell 網格是 `Noggog.P2Int`；旋轉以度數撰寫但以弧度儲存。
- 外部引用（`<master>:0xFORMID`）在寫出時會自動加入主記錄（`MastersListContent = Iterate`）；構造 FormKey 時以 `& 0x00FFFFFF` 遮罩主記錄索引位元組。
- API 探索：`ilspycmd -t <Type> ~/.nuget/packages/mutagen.bethesda.*/0.53.1/lib/net9.0/*.dll`。
