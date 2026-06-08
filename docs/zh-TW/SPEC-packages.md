<!-- AI 套件、天氣與氣候 -->
# ModForge 規格說明 — AI 套件與天氣

← [目錄](SPEC-index.md)

### packages — AI 套件（NPC 的行為）
`packages` 條目是一個 AI 套件。Skyrim 的 PACK 記錄採用**模板驅動**方式：你透過 `template`
參考一個原版「程序模板」表單，由該模板定義資料輸入結構（插槽索引及類型）。我們的套件為
模板所定義的插槽填入輸入值。

ModForge 目前實作八個模板——**Sandbox**（`Skyrim.esm:0x01C254`）、**Sleep**
（`Skyrim.esm:0x019717`）、**Travel**（`Skyrim.esm:0x016FAA`）、**UseMagic**（`Skyrim.esm:0x0504F5`）、
**Patrol**（`Skyrim.esm:0x017723`）、**Follow**（`Skyrim.esm:0x019B2C`）、**Escort**
（`Skyrim.esm:0x023B73`）及 **SitTarget**（`Skyrim.esm:0x0A9277`）。撰寫對應的子物件
（`sandbox` / `sleep` / `travel` / `useMagic` / `patrol` / `follow` / `escort` / `sitTarget`），
建置便會填入該模板的 Data 插槽。若要指向 ModForge 尚未處理的模板
（UseWeapon / …），仍然設定 `template`；套件會輸出結構上有效但無 Data 覆寫（套用模板預設值）
的記錄並附上警告。在新增支援前，使用
`packagediag <Skyrim.esm> <0xFORMID>` 探索任何模板的具名插槽結構。

**Sandbox 指定參考 vs Travel：** Sandbox 的 `location` 參考讓 NPC 在該參考**附近**
徘徊/進食/就坐（radius 涵蓋附近家具）。Travel 的 `place` 參考讓 NPC 真正**走向**該參考
並停在其 `radius` 範圍內。常見組合：同一 NPC 的 `packages` 清單上一個 Travel 套件 + 一個
Sandbox 套件（Travel 在前）——Travel 執行直到 NPC 抵達，然後 Sandbox 接管。

```jsonc
{ "editorId": "MF_HangAtSpotPackage",
  "template": "Skyrim.esm:0x01C254",        // Sandbox 程序模板（以 EditorID "Sandbox" 查找）
  "preferredSpeed": "Walk",
  "interruptFlags": [                        // 擬真 NPC 開關 — 大多保持開啟
    "HellosToPlayer", "RandomConversations", "ObserveCombatBehavior",
    "GreetCorpseBehavior", "ReactionToPlayerActions", "FriendlyFireComments",
    "AggroRadiusBehavior", "AllowIdleChatter", "WorldInteractions" ],
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 0, "dayOfWeek": "Any" },
  "sandbox": {
    "radius": 1024,                          // 離錨點的徘徊距離
    "location": "",                           // 空 -> LocationFallback（NPC 的編輯器位置）；
                                              // 一個 ref -> LocationTarget 錨定在該已放置 ref
    "allowEating": true,  "allowSleeping": false,  "allowConversation": true,
    "allowIdleMarkers": true, "allowSitting": true, "allowWandering": true,
    "allowSpecialFurniture": true, "energy": 50.0 } }
```
然後附加到一個 NPC：`"npcs": [{ ..., "packages": [ "MF_HangAtSpotPackage" ] }]`。

**為何是這些輸入：** Sandbox 模板為它們命名（見 `packagediag <Skyrim.esm> 0x01C254`）。
`location: ""` 是最安全的預設——引擎將 sandbox 錨定在 NPC 被放置之處。
指定的 `location` ref（一個 REFR/ACHR FormID）將 sandbox 錨定在該 reference 的位置。
`Allow Sleeping = false` 讓 NPC 全天候活躍（適合遊戲內可見的測試）；一般的日夜循環則保持
為 true。`Energy = 50` 是原版預設（越高 = 越多徘徊）。

**Travel 模板（`Skyrim.esm:0x016FAA`）— `travel` 子物件：**
```jsonc
{ "editorId": "MF_GoToWhiterun",
  "template": "Skyrim.esm:0x016FAA",       // Travel
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  "travel": {
    "place": "Skyrim.esm:0x0567F7",        // 指向一個已放置 REFR/ACHR 的 ref（目的地）
    "radius": 256,                          // 抵達此單位數內即視為到達（0 = 精確點）
    "rideHorse": false,                     // 模板預設
    "preferPath": false } }                 // 模板預設
```
Travel 只有 3 個插槽：`Place to Travel` / `Ride Horse if possible?` / `Prefer Preferred Path?`。
**沒有 `place` ref 時 NPC 不會真的移動**——引擎退回 NearSelf
（退化情況：移動到你已在的地方），套件變成空操作。在其後串接一個 Sandbox 套件
（在 NPC 的 `packages` 清單中優先級較低）讓 NPC 抵達後有事可做。

**UseMagic 模板（`Skyrim.esm:0x0504F5`）— `useMagic` 子物件：**
```jsonc
{ "editorId": "MF_AltarRitual",
  "template": "Skyrim.esm:0x0504F5",       // UseMagic
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  // 若要持續施法，兩個旋鈕都必需（見下方「It.18 注意事項」）：
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 1440, "dayOfWeek": "Any" },
  "useMagic": {
    "spell":           "Skyrim.esm:0x043324",   // 必需 — 指向一個 SPEL record 的 FormLink（Candlelight）
    "location":        "",                       // 可選的已放置 ref（站在哪）；空 -> NearSelf
    "radius":          256,                      // location 半徑（模板預設 500）
    "target":          "",                       // 可選的已放置 ref（對誰施法）；空 -> PackageTargetSelf
    "holdWhenBlocked": true,
    "castTimeMin":     1.5, "castTimeMax":     2.5,
    "cooldownTimeMin": 8.0, "cooldownTimeMax": 12.0,
    "numToCastMin":    1, "numToCastMax":    1000,
    "dualCast":        false } }
```
UseMagic 有 11 個作用中插槽（2-12）。**「Spell」插槽是一個指向特定 SPEL record 的
`PackageTargetObjectID` FormLink**——而非類別 enum。（`Spell` 實作 `IObjectId`。）當 `target`
為空時，建置將插槽 4（Target）寫為 `PackageTargetSelf`，比照原版自我施法套件如
`WCollegePracticeCastWard`；將 `target` 設為已放置 ref 即為對 X 施法（原版
`WCollegeOnmundPracticeFlames12x4` 指向一個標靶假人）。

**It.18 注意事項（用血淚換來的——3 個遊戲內回合）：**
1. **插槽 3（Spell）必須是 `PackageTargetObjectID`，而非 `PackageTargetObjectType`。** 模板
   預設顯示 `PackageTargetObjectType`（一個類別 enum），但所有 46 個原版 UseMagic 套件都用
   `PackageTargetObjectID`（FormLink）覆寫它。enum 形式能建置、能 dump，但遊戲內空操作。
2. **插槽 4（Target）必須設定**——自我施法用 `PackageTargetSelf`，否則用
   `PackageTargetSpecificReference`。維持為模板的 `PackageTargetLinkedReference`
   退回值在實務上也會空操作。
3. **`numToCastMax` 是整個套件生命周期的施法總次數**，並非每循環次數。當
   `schedule.durationInMinutes=0`（預設）時，套件在其配額被滿足的那一刻便完成。若要持續施法，
   同時使用高上限（像原版 Onmund 的 1000）**及**一個非零的 `schedule.durationInMinutes`
   （例如 1440 = 24 小時）。
4. **戰鬥會搶占 UseMagic。** 原版行為——對於閒置的儀式施法者這是正確的（NPC 切換為攻擊而非
   站立施放 Candlelight）。若要強制施法持續（例如 boss 儀式），加入
   `flags: [ "IgnoreCombat" ]`，比照原版 `SprigganCallOverride`。
5. **使用 `pkgsbytemplate <plugin> <0xFORMID>`** 掃描一個 master 中所有使用給定模板的套件。
   有必要這樣做，因為 `find` 僅比對 EditorID，而許多基於模板的套件
   （例如 `WhiterunTempleCastHealingSpellSoldier`）的 EditorID 並不帶有模板名稱。

**SitTarget 模板（`Skyrim.esm:0x0A9277`）— `sitTarget` 子物件：**
```jsonc
{ "editorId": "MF_BorinSit",
  "template": "Skyrim.esm:0x0A9277",       // SitTarget（「去使用那件家具」）
  "preferredSpeed": "Walk",
  "sitTarget": {
    "target":       "InnChair",            // 必需 — 指向一個已放置 FURNITURE reference 的 ref
                                           //   （原版 REFR 或 spec 內 placement 的 editorId）
    "waitTime":     0,                     // 保持就坐的秒數（0 = 直到套件/phase 結束）
    "stopMovement": false } }
```
SitTarget 是「走過去並坐下/使用一件家具」的例程（從原版 `MQ306EsbernSit` 解碼）。它填入
3 個作者插槽：**16** `Target`（SingleRef → 家具 ref，必需）、**3** `Wait Time`（float）、
**4** `Stop Movement Flag`（bool）。引擎會引導 NPC 走到家具**並**讓他就坐，因此
**一個 SitTarget 動作同時涵蓋走路與就坐**（不需另外的 Travel）。與 Travel 相同的尋路網格
規則：家具必須是 NPC 尋路網格上可抵達的已放置 ref（保持在同一個室內 cell）。家具 ref 會被
自動強制為**永久**（它是套件 SingleRef 目標）。沒有 `target` 時套件空操作。主要用途：一個
**scene 演出 beat**——一個 scene Package 動作參照一個 SitTarget 套件，讓演員在對話中途就坐
（見 `examples/scene-sit-performance.json`）。

**Activate 模板（`Skyrim.esm:0x019B2D`）— `activate` 子物件：**
```jsonc
{ "editorId": "MF_PullLever",
  "template": "Skyrim.esm:0x019B2D",       // Activate
  "preferredSpeed": "Walk",
  "activate": {
    "target":           "MF_Lever",        // 必需 — 指向要活化的物件的 ref（placement editorId 或原版 ref）
    "numberToActivate": 1 } }              // 預設 1
```
NPC 走向並**活化** `target`（一個拉桿/門/活化物——觸發其 OnActivate）。插槽 0 是 SingleRef
目標（與 Patrol/Follow 插槽 0 一樣延遲接線，因此可以是 spec 內 placement；自動強制為
**永久**），插槽 2 是「Number to Activate」。從原版
`dunHillgrundsUnlockExteriorDoorActivate` 解碼。沒有 `target` 時套件空操作。很適合作為一個
scene Package 動作 beat（演員在 scene 中途拉動鐵鏈／開門）。與 Travel/SitTarget 相同的尋路
可抵達性規則。

**Eat 模板（`Skyrim.esm:0x019714`）— `eat` 子物件：**
```jsonc
{ "editorId": "MF_TavernMeal",
  "template": "Skyrim.esm:0x019714",       // Eat
  "schedule": { "hour": 19, "durationInMinutes": 60 },
  "eat": {
    "location":          "",               // 可選的已放置 ref（在哪吃）；空 -> NearSelf
    "radius":            500,
    "allowSitting":      true,
    "allowWandering":    true,
    "numFoodItems":      1,
    "energy":            0,
    "minWanderDistance": 300 } }
```
Eat 是一個基於 LOCATION 的 Sandbox 變體：NPC 前往 `location`，尋找食物 + 椅子（建置器輸出的
一個固定引擎搜尋——插槽 1 Food Criteria、4 Found Food、5 Chair Target、6 Found Chair），
坐下並進食。仿照 Sleep 模板的插槽填法。以 `schedule` 設定用餐時段。
「去酒館用餐。」（注意：這是一個環境例程——若需要精確的 scene「坐在
這張椅子上」beat，請改用 **SitTarget**。）

**旗標（Package.Flag）：** `OffersServices`、`MustComplete`、`MaintainSpeedAtGoal`、`ContinueIfPcNear`、
`OncePerDay`、`PreferredSpeed`、`AlwaysSneak`、`AllowSwimming`、`IgnoreCombat`、`WeaponsUnequipped`、
`WeaponDrawn`、`NoCombatAlert`、`WearSleepOutfit`。

**中斷旗標（Package.InterruptFlag）：** `HellosToPlayer`、`RandomConversations`、
`ObserveCombatBehavior`、`GreetCorpseBehavior`、`ReactionToPlayerActions`、`FriendlyFireComments`、
`AggroRadiusBehavior`、`AllowIdleChatter`、`WorldInteractions`。**這些旗標決定了一個 NPC 是
沉默的石像還是栩栩如生的角色。** 原版 DefaultSandbox 啟用所有這些旗標。

### 天氣與氣候 — 自訂天空（WTHR）+ 天氣循環（CLMT）

**天氣**（`WTHR`）是一個*天空*：雲層、每日時段的天空/霧氣/雲朵/太陽顏色、降水、風速、
霧距。**氣候**（`CLMT`）是一個*循環*：哪些天氣會出現（各自帶有相對 `chance` 權重）以及
日出/日落時間與太陽/月亮貼圖。氣候參照天氣；兩者共同賦予一個世界空間或地區其大氣氛圍。

```jsonc
"weathers": [{
  "editorId": "MF_EerieFog",
  "flags": ["Cloudy", "Rainy"],          // 預設 ["Pleasant"]
  "skyUpperColor": {                      // 每個顏色：日出/白天/日落/夜晚，RGB 0–255
    "day":   { "r": 46, "g": 92, "b": 58 },
    "night": { "r": 8,  "g": 20, "b": 14 }   // 省略的時段從 `day` 繼承
  },
  "fogNearColor": { "day": { "r": 60, "g": 120, "b": 70 } },
  "sunlightColor": { "day": { "r": 120, "g": 170, "b": 110 } },  // 世界上的方向性光
  "clouds": [{ "index": 0, "texture": "Sky\\SkyrimCloudsUpper04.dds",
               "xSpeed": 0.012, "ySpeed": -0.006, "alphaDay": 1.0, "alphaNight": 0.8 }],
  "precipitation": "Skyrim.esm:0x10780F",  // 一個雨效 SPGD（透過對原版雨天 WTHR 跑 weatherdiag 查找）
  "windSpeed": 0.35, "windDirection": 210,  // speed 0–1（或 0–100）；direction 以度計
  "fogDayNear": 256, "fogDayFar": 9000
}],
"climates": [{
  "editorId": "MF_EerieClimate",
  "weathers": [ { "weather": "MF_EerieFog", "chance": 75 },
                { "weather": "MF_PlainClear", "chance": 25 } ],   // chance 為相對權重
  "sunriseBegin": "06:00", "sunriseEnd": "09:30",
  "sunsetBegin": "17:00",  "sunsetEnd": "20:00",
  "moons": ["Masser", "Secunda"], "volatility": 40
}]
```

- **最簡結構即有效。** 只含 `editorId` 的天氣是一個符合原版規範的晴天天空；
  氣候只需 `editorId` + 至少一個 `weather`。其他一切皆有預設值。
- **顏色**為 8 位元 RGB（0–255）。任何省略的時段從 `day` 取種子值，因此部分顏色
  仍然有效。Validate 會標記超出範圍的分量。
- **風向**以**度**（0–360）編寫；它在磁碟上儲存為整圈的分數。**風速**接受
  0–1 分數或 0–100 百分比。
- **`precipitation`** 是指向著色粒子幾何體（`SPGD`）的 *ref*。以
  `weatherdiag <Skyrim.esm> <a-rainy-WTHR-formid>` 查找原版雨效（例如 `SkyrimStormRain`
  → `Skyrim.esm:0x10780F`）。`Rainy`/`Snow` 旗標驅動引擎的降水系統。
- **檢查**一個生成或原版記錄，使用 `weatherdiag <esp> <0xFORMID>` /
  `climatediag <esp> <0xFORMID>`，或 `dump`（會印出兩者）。

> **指派氣候是獨立步驟。** 輸出一個 `WTHR`+`CLMT` 本身**不會**改變任何遊戲內天空。
> 原版遊戲透過一個**世界空間**（`WRLD` `Climate` 欄位）或一個**地區**（`REGN` 天氣資料）
> 記錄套用氣候——這兩者皆不在此建置（世界空間/地區撰寫不在此範圍內）。此處產生的記錄是
> 讓這類記錄指向的有效目標；以手動方式（或透過未來的 WRLD/REGN 功能）這麼做即是掛鉤。
> **已於遊戲內確認（It.36，2026-06-02）：** 透過控制台 `sw <XX>000800` 強制天氣，其中
> `XX` = 外掛在載入順序中的 slot（十六進位，見 MO2 右側面板）。`build` 指令在建置成功後
> 會印出所有 WTHR 記錄的 `sw` 指令。
