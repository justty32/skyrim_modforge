# ModForge spec — recipes、perks、assets 與 texture sets

← [index](SPEC-index.md)

### recipes（製作 / COBJ）
讓某個物品可以在工作台製作、強化（temper）或熔煉（smelt）。一個 recipe 的 `kind` 決定它的
類型（預設 `craft`）與**預設工作台**；`workbench` 是一個**具名選擇器**（`forge` /
`sharpeningWheel`（=`grindstone`）/ `armorTable`（=`workbench`）/ `smelter` / `tanningRack` /
`skyforge`）——或一個原始的 `<master>:0xID` keyword ref，它會覆寫 kind 的預設。省略
`workbench` 則採用該 kind 的預設。

```jsonc
{ "editorId": "MF_ForgedBladeRecipe",
  "kind": "craft",                      // craft | temper | smelt | breakdown   (default craft)
  "createdObject": "MF_ForgedBlade",    // a ref — usually an in-spec weapon/armor
  "count": 1,
  "workbench": "forge",                 // named selector OR a keyword ref; OMIT -> kind default
  "components": [                        // consumed on craft (ref + count)
    { "item": "Skyrim.esm:0x05ACE5", "count": 2 },   // SteelIngot
    { "item": "Skyrim.esm:0x0800E4", "count": 1 } ], // LeatherStrips
  "conditions": [                        // perk/item/skill gating (shared CTDA) — optional
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind` 預設值** — `craft` → forge、`temper` → sharpening wheel、`smelt`/`breakdown` → smelter。

**`kind: "temper"`** — 在 grindstone（武器）/ armor table（防具）上 IMPROVE 一件既有的武器/防具。
`createdObject` 就是要被強化的那件物品（必須是 in-spec 武器/防具或一個
外部 ref）；component 是強化材料。模仿原版的做法，在 smithing 的 `HasPerk` 之前
加上附魔物品的守衛條件 `TemperIsEnchanted`（`or: true`）：
```jsonc
{ "editorId": "MF_ForgedBladeTemper", "kind": "temper",
  "createdObject": "MF_ForgedBlade", "workbench": "sharpeningWheel",
  "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],
  "conditions": [
    { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind: "smelt"` / `"breakdown"`** — 礦石 → 錠，或在 smelter 把一件物品分解成材料
（`createdObject` = 輸出的錠，component = 被消耗的礦石/物品）。

**`conditions`** — 每一個都是一個 shared CTDA（與 dialogue/package 守衛條件相同的 `ConditionSpec`——見 [SPEC-dialogue](SPEC-dialogue.md)）。
`function` ∈ `HasPerk` | `GetItemCount` | `GetGlobalValue`（各需一個 `param` ref）|
`TemperIsEnchanted`（無 param）。`comparison` 是運算子（`==` `!=` `>` `>=` `<` `<=`，預設
`>=`），`value` 是測試值，`or: true` 與**下一個** condition 做 OR 串接。用 `find Skyrim.esm
<name> Perk` 來查 perk FormIDs；`cobjdiag <esp> <0xID>` 會印出任一 recipe 的完整結構。

常見工作台 keyword FormIDs（從 Skyrim.esm 探測）：`0x088105` forge、`0x0ADB78` armor table、
`0x088108` sharpening wheel、`0x0A5CCE` smelter、`0x07866A` tanning rack、`0x0F46CE` Skyforge。

### perks（PERK）
一個 perk 是一個被動能力或一個量化的數值/戰鬥修正——是技能樹、種族能力與任務獎勵
加成的基本建構單元。主幹攜帶 `name`/`description`、`playable`/`hidden`/`trait` 旗標、
`level` + `numRanks`（≥1）、選用的面向玩家的 `conditions`（perk-level CTDA 守衛條件），
以及一個 `effects` 清單。支援兩種 effect 類型：

```jsonc
{ "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
  "effects": [
    // (a) ABILITY — grant a SPEL. Pair with an in-spec Ability/constant-effect spell + MGEF.
    { "kind": "ability", "spell": "MF_IronHideAbility" } ] }

{ "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
  "conditions": [   // perk-level gate (when the perk applies at all)
    { "function": "GetBaseActorValue", "actorValue": "OneHanded",
      "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
  "effects": [
    // (b) ENTRY-POINT — a quantitative modifier on a named EntryPoint.
    { "kind": "entryPoint",
      "entryPoint": "ModAttackDamage",      // an EntryType name
      "function": "Multiply",               // Set | Add | Multiply
      "value": 1.2,                          // ×1.2 = +20%
      "conditions": [                        // effect-level gate (when the bonus fires)
        { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",  // WeapTypeSword
          "comparison": "EqualTo", "value": 1 } ] } ] }
```

- **`entryPoint`** 是 Skyrim 的 `EntryType` 值之一 — `ModAttackDamage`、`ModSpellMagnitude`、
  `CalculateMyCriticalHitChance`、`ModArmorRating`、`GetMaxCarryWeight`、… 用
  `perkdiag <Skyrim.esm> entrypoints` 探出完整集合，或傾印一個原版 perk 來抄一個可用的結構：
  `perkdiag <Skyrim.esm> 0x079343`（Armsman20 = ModAttackDamage ×1.4）。
- **`conditions`**（perk-level 與 per-effect 兩者）使用共用的 CTDA builder（與
  dialogue/package/recipe 守衛條件相同的 `ConditionSpec`）。Perk 相關的 functions：
  `GetBaseActorValue`/`GetActorValue`（需 `actorValue`）、`HasKeyword`/`WornHasKeyword`/`HasPerk`/
  `GetIsID`/`GetIsRace`/`GetItemCount`/`IsSpellTarget`（需一個 `param` ref）、`GetEquippedItemType`
  （`itemType` = `Left`/`Right`/`Voice`/`Instant`）、`GetRandomPercent`、`GetLevel`。每一個都帶一個
  `comparison`（`EqualTo`/`GreaterThanOrEqualTo`/… 或符號形式）對比 `value`、一個選用的
  `runOn`（`Subject` 預設 / `Target`），以及 `or`（與下一個 condition 做 OR）。
- **附加到一個 NPC** 透過 `npcs[].perks: ["MF_IronHidePerk", …]` — 該 actor 會在遊戲開始時
  被動取得這些 perk（每次 placement 攜帶該 perk 的 `numRanks`）。**把一個 perk 賦予
  玩家需要一個 Papyrus `AddPerk` 呼叫**（`scripts` + 一個 quest fragment）——沒有任何純記錄的方式
  能在遊戲開始時把一個 perk 放到玩家身上；那是一條 CK/script 路線，在此誠實記載。
- **遊戲內注意事項：** 結構上這些發出的東西與原版 perks 完全一樣（用 `dump` /
  `perkdiag` 驗證），但一個 entry-point 修正是否真的改變了戰鬥數值，或一個 ability
  perk 的 SPEL 是否生效，只能透過真正啟動 Skyrim 來確認。實例：
  `examples/perk_spec.json`。

### external assets — 你自己的 meshes / textures / sounds（`model`、`sounds`、`assets`）
與其透過 `template` 複製一個原版記錄的 mesh，不如帶上你**自己的** assets。ModForge
**參照**它們（把 Data-relative 路徑寫進記錄）並**打包**它們（在 `package` 時把檔案
複製到 `.esp` 旁邊）。它不會撰寫 meshes/sounds——完整契約 +
路徑規則見 **[external_assets.md](../external_assets.md)**。
```jsonc
"assets": "my_assets",          // source dir; package copies its Meshes/Textures/Sound/… into the mod
"sounds": [ { "editorId": "MFChimeSD", "files": [ "Sound\\fx\\mymod\\chime.wav" ] } ],
"statics":    [ { "editorId": "MFStone",  "model": "MyMod\\stone.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Bell", "model": "MyMod\\bell.nif",
                  "activationSound": "MFChimeSD" } ]
```
- **`model`**（在 statics/activators/furniture/miscItems/weapons 上）是一個 Data-relative 的 `.nif` 路徑，
  根於 `Meshes\` — 所以**省略 `Meshes\` 前綴**（寫 `MyMod\bell.nif`，不是
  `Meshes\MyMod\bell.nif`）。`validate` 會強制這一點。在一個 `miscItem` 上，`model` 覆寫 `template`
  （會警告）；在一個 `weapon` 上，`model` 要**搭配**一個 `template`（一個無 model/無 template 的武器在裝備時會 CRASH）。
- **`sounds`** 發出 Sound Descriptors（SNDR）。一個記錄透過 *ref* 指向其中之一（in-spec 的 `editorId` 或
  原版的 `<master>:0xFORMID`）：activator 的 `activationSound`/`loopingSound`、misc/weapon 的
  `pickUpSound`/`putDownSound`。`category`/`outputModel` 預設為原版 SFX category/output。
- **`assets`** 指名一個像 `Data/`（`Meshes/`、`Textures/`、`Sound/`、`Music/`、
  `Seq/`）那樣排列的 source dir；`package` 會把那些子樹複製到輸出的 mod 資料夾。用
  `package <spec> <outDir> --assets <dir>` 可逐次覆寫。實例：`../examples/custom_asset_spec.json`。

### textureSets（TXST）— 不需新 mesh 的重貼圖
有一大類 mods 只是**換掉**一個既有 mesh 的 textures（一把重新上色的劍、一隻換皮的
生物、一面重用 Jorrvaskr 旗幟 `.nif` 而漆成 Markarth 風格的旗幟）而沒有撰寫一個新的
`.nif`。那就是一個 **TextureSet（TXST）** 記錄：一組 texture-map 路徑加上一個消費者，把
基底 mesh 上一個具名材質指向它。

一個 TXST 有最多八個選用 slots；只設定你要替換的那些（一個被省略的 slot 會保留
該通道在 mesh 上的原始 map）。每個路徑都是**相對於 `Data\Textures\`**——就像
一個 `model` 路徑相對於 `Data\Meshes\` 一樣——所以你要**省略**開頭的 `Textures\`：

```jsonc
"textureSets": [
  { "editorId": "MF_GildedRubbleTexture",
    "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // slot 0 — color/albedo (_d)
    "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",   // slot 1 — normal + gloss (_n)
    // mask(_m)/glow(_g)/height(_p)/environment(_e)/multilayer/backlight also available — all optional
    "flags": [ "NoSpecularMap" ] }                         // NoSpecularMap|FaceGenTextures|HasModelSpaceNormalMap
]
```

用一個 `statics` 或 `activators` 記錄（任何帶 `model` 的記錄）上的 `alternateTextures` 把它接進一個消費者。
每一個條目覆寫基底 `.nif` 內的一個**具名材質/子網格**：

```jsonc
"statics": [
  { "editorId": "MF_GildedRubble",
    "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // a VANILLA mesh, reused as-is
    "alternateTextures": [
      { "name": "NorRubblePiece03:0",        // MUST match a material/3D-name in the .nif (CK "AltTex" dialog)
        "index": 0,                           // 3D sub-mesh index (the trailing number in `name`)
        "textureSet": "MF_GildedRubbleTexture" } ] }              // ref → a TXST (in-spec or <master>:0xFORMID)
]
```

`name`/`index` 慣例（`<MeshName>:<index>`）模仿原版——用 `txstdiag`（一個 TXST 的 slots）
或 `dump`（一個記錄的 `altTexture` 行）來檢視一個真實範例，例如原版 STAT
`NorExtRubblePiece03_HeavySN` 使用 `name="NorRubblePiece03:0" index=0`。材質名稱來自
CK 的 *Model Data → Edit → 3D Name* 清單（NifSkope 把它們顯示為 `BSLightingShaderProperty`
名稱）；一個錯誤的 `name` 會無聲地什麼都不換。

**誠實的限制：** ModForge 只寫 TXST 記錄 + `alternateTextures` 參照。
`.dds` 檔案本身是**使用者撰寫的**——ModForge 無法建立或渲染 texture 內容，且
headless 工具鏈無法驗證一次換貼在遊戲內看起來對不對。把你撰寫的 `.dds` 檔案
放在打包好的 mod 資料夾裡的 `Data/Textures/<your path>/` 之下。見 `examples/texture_set_spec.json`
（附一個佔位的 `examples/textures/ModForge/rubble/` 樹）與 cookbook recipe。

### globals（GLOB）— 共享的旗標 / 計數器 / 常數
一個 **GlobalVariable** 是一個橫跨**整個遊戲**共享、且持久存於存檔的具名數字。它是
最簡單的跨切面狀態原語：可被**零 Papyrus 的 conditions** 讀取
（`GetGlobalValue`）、被 Papyrus 讀取（`GetValue`/`SetValue`）、以及主控台（`set`/`show`）。把它當作
一個**旗標 / 重新觸發 token**（0/1 — 在某事件後設定、由另一事件清除以重新啟用它）、一個**計數器**、
一個機率/權重（regions、leveled lists），或一個唯讀的**調校常數**。
```jsonc
"globals": [
  { "editorId": "MF_SeenIntro", "type": "short", "value": 0 },               // a 0/1 flag
  { "editorId": "MF_KillCount",  "type": "long",  "value": 0 },               // a counter ("int" = alias for long)
  { "editorId": "MF_FogDensity", "type": "float", "value": 0.35 },            // a tunable weight
  { "editorId": "MF_DamageMult", "type": "float", "value": 1.5, "constant": true } // read-only tuning constant
]
```
- **`type`**：`short` | `long`（int）| `float`。Skyrim 在磁碟上把每個 global 都存為 float，short/long
  在讀取時截斷為整數。預設 `short`。
- **`value`**：**初始**值。⚠️ 一旦存在一個存檔，它就保有自己的執行期值——更改
  plugin 的值不會覆寫一個既有的存檔（一個新遊戲會採用新的初始值）。與
  `.seq`/dialogue 相同的「save已固化」規則。
- **`constant`**：設定 GLOB Constant 旗標（唯讀；無法被 `SetValue`）——用於調校數字。

在任何接受 ref 的地方用 `editorId` 參照一個 global——最有用的是一個 condition 的 `param` 搭配
`function: "GetGlobalValue"`（見 [conditions](SPEC-dialogue.md#conditions--ctda-gates-on-a-dialogue-info-a-banter-info-or-a-package)）
或一個 region 的 `global`。**與 quest stages 互補：** 一個 stage（`GetStage`）是 quest 範圍的進度；
一個 GLOB 是全域、無腳本的共享狀態。見 `examples/globals.json`。

**誠實的限制（這個切面）：** ModForge 建立 GLOB 記錄 + 讓 conditions/records 參照它。
在執行期**翻轉**一個 global（`SetValue`）是從一個 Papyrus result script / quest fragment /
alias script 完成的——那些是另外撰寫/附加的（用一個 GLOB 守衛條件的 scene「replay policy」是一個
規劃中的消費者，尚未建好）。
