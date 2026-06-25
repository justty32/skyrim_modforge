# Dynamic Animation Casting（DAC / DAC NG Plus）

← [action-system 中樞](../README.md)

> **Layer：動畫驅動施法框架**。loki（loki_DynamicAnimationCasting.dll）的 SKSE plugin，靠**一張純文字 config 表**把「播到某 animation event → 在該 actor 身上釋放某些 spell」綁起來。對 ModForge 角度與 [BDI](behavior-data-injector.md)/[PIE](payload-interpreter.md) 同類：**DLL 不可生成，但它讀的 config 完全可生成**，且格式比 BDI/PIE 更貼近 ModForge 已有的 magic/perk record 與 OAR 的 `Plugin.esp|0xFormID` 字串。
>
> ⚠️ **命名陷阱**：本批次下載有三個都叫「DAC / DAc0da」的 mod，只有一個是這套框架。見最後「版本與同名混淆」。

## 是什麼（What it does）

- 一個 SKSE plugin，hook 動畫事件系統。當任一 actor（玩家或 NPC）播放到設定中指定的 **animation event**（如 `HitFrame`、`BeginCastLeft`、`MRh_SpellFire_Event`…），DAC 就在該 actor 身上**釋放一組 spell**。
- 等於把「揮刀的那一幀」「開始唸咒的那一幀」變成 spell 觸發點——招式特效、反擊光環、近戰附魔射彈、weapon-art 全靠它。是 BFCO/SCAR 那條「動畫驅動戰鬥」鏈裡負責「動畫 → 施法」的一支（PIE 負責「動畫 → 設 graph var」，DAC 負責「動畫 → 放 spell」）。
- 對 mod 用戶：裝 DLL 即可，零操作；對 modder：寫一個 `.toml` config 就能擴。**這正是 ModForge 主場——可生成的純文字 config。**

## Config schema（核心交付，**實檔驗證 NG Plus 73293 v3.2.4**）

出貨在：`SKSE/plugins/_DynamicAnimationCasting/*.toml`（資料夾下所有 `.toml` 都會被讀；檔名隨意，慣例 `<Mod>.toml`）。**格式是 TOML，不是 JSON**——每筆綁定是一個 `[[event]]` array entry。隨附 `AnimEvents.txt`（約 1300 個合法 animation event 名清單，給作者查 `AnimationEvent` 該填什麼）。

FormID 一律走 **DAR/OAR 風格字串** `"Plugin.esp|0xFormID"`（NG Plus 官方 template 明寫「same as DAR format」；玩家 = `"Skyrim.esm|20"`）。

### 必填
- `AnimationEvent` = `<String>` — 要在哪個動畫事件上觸發（查 `AnimEvents.txt`）。

### Spell（要放什麼）
- `SpellFormIDs` = `<Array[Form ID | 特殊符號]>` — 要釋放的 spell 清單。特殊符號（**實檔 template 列出**）：
  - `@FOREHAND` 當前手上裝備的 spell／`@OFFHAND` 另一手／`@POWER` 選定的 power/shout／`@FAVOURITE` 我的最愛裡的一個（由 Papyrus 選）／`@<STRING>` 由 Papyrus `RegisterCustomSpell("STRING", spell)` 註冊的自訂名。
  - 例：`["Skyrim.esm|0x7D997", "@OFFHAND", "@TEST"]`。
- Spell 過濾旗標：`CastOnlyFirstSpell` / `CastOnlyKnownSpell`（actor 不會就不放）/ `IgnoreConcentrationSpell` / `IgnoreBoundWeaponSpell`（皆 `<Boolean>`）。

### 條件（全部 AND；不用的就省略）
`HasActorFormID` / `HasRaceFormID` / `HasEffectFormID`（有此 active effect 時）/ `HasKeywordFormID` / `HasPerkFormID` / `IsEquippedRightFormID` / `IsEquippedLeftFormID` 皆 `<Form ID>`（`0`/`-1` = 忽略）；`HasWeaponType` = `<Enum>`（`"HandToHandMelee" "OneHandSword" "OneHandDagger" "OneHandAxe" "OneHandMace" "TwoHandSword" "TwoHandAxe" "Bow" "Staff" "Crossbow" "Spell" "Shield" "Torch"`）；`HasWeaponKeyword` / `HasWeaponEnchantEffect` = `<Form ID>`；`IsOnMount` / `IsSneaking` / `IsRunning` = `<Boolean>`；`Chance` = `<Float 0–1>`（亂數沒過就不放）；`Cooldown` = `<Float 秒>`；`ExclusiveGroup` = `<String>`（同 group 在同一 event 只觸發一筆，防同一施法重複觸發）。

### Properties（消耗與強度，皆 `<Float>`，**整筆只扣一次、非每 spell**）
`HealthCost` / `StaminaCost` / `MagickaCost` / `CastMagickaCostFactor`（預設 1.0）/ `EnchantmentCost`（WIP）/ `EnchantmentCostFactor`（WIP）/ `Effectiveness` / `Magnitude`（似乎只對 Restoration 生效）/ `WeaponEnchantMagnitudeFactor`（同前）；`DualCasting` = `<Boolean>`（強制雙手施法加成）。

### 實例（NG Plus 官方 template 內的範例，**逐字引自檔案**）
```toml
# Instant cast spells (no charge time) when sneaking
[[event]]
AnimationEvent = "BeginCastLeft"
HasActorFormID = "Skyrim.esm|20"
SpellFormIDs = ["@FOREHAND"]
HasWeaponType = "Spell"
IsSneaking = true
```

### Papyrus API（隨附 `DynamicAnimationCasting.psc`，3 個 global native）
```papyrus
bool function RegisterCustomSpell(string name, Spell spell) global native  ; → @NAME
bool function SelectFavouriteSpell(int index) global native                ; → @FAVOURITE
int  function NextFavouriteSpell(int delta) global native
```
即：config 只能寫死 FormID 或 `@FOREHAND`/`@OFFHAND`/`@POWER`/`@FAVOURITE`；要動態指定任意 spell，得在 config 用 `@MYNAME` 佔位，再由 Papyrus 在 runtime `RegisterCustomSpell("MYNAME", someSpell)` 綁上去。

## 機制 / 依賴（Mechanism / deps）

- **出貨內容（NG Plus 實檔驗證）**：`SKSE/plugins/loki_DynamicAnimationCasting.dll`（+ `.pdb`）、`Scripts/DynamicAnimationCasting.pex` + `Source/Scripts/*.psc`、`_DynamicAnimationCasting/template.toml`（含完整欄位註解）+ `AnimEvents.txt`。**無 esp/esm、無 MCM、無 Nemesis patch**——純 DLL + config 表 + 薄 Papyrus API。
- **驅動方式**：純 **config-file driven**（讀 `.toml`），不是 hkx 內 annotation。它不需要動畫師在 hkx 裡加註釋——只要動畫**本來就會送出**某個 animation event（vanilla 行為圖天然有 `HitFrame`/`BeginCastLeft`/`attackStart`…，BFCO/MCO 等再補更多），config 把那個 event 名綁到 spell 即可。這點與 PIE（需 hkx annotation）關鍵不同，對 ModForge **更友善**。
- **依賴**：SKSE64 + Address Library。`@FAVOURITE`/`@NAME` 路徑需 DAC 自帶的 `DynamicAnimationCasting.psc` 編譯產物（已附 pex）。**未見** PapyrusUtil/JContainers/SPID 等外部相依。

## 版本與同名混淆（**實檔驗證，務必看**）

本批次三個壓縮檔，只有第一個是這套框架：

1. **Dynamic Animation Casting NG Plus（mod 73293, v3.2.4）** — ✅ **就是本框架**。上述 TOML 格式以此檔為準。NG Plus 是原版 DAC（**mod 65512**，由 Maxsu 維護的 fork）的「NG」分支；NG = 統一 AE/SE runtime。
2. **DAC v1.0（mod 123113）** — ❌ **不是這框架**。實檔是一個 **Dragon 動畫包**（`meshes/actors/Dragon/animations/OpenAnimationReplacer/...`，16 檔，純 OAR `config.json` + `.hkx`，無 DLL/esp/toml）。只是縮寫剛好也叫 DAC。其 `config.json` 是標準 OAR 條件格式（`IsRace` → `pluginName/formID`），與施法 config 無關。**別拿來當 DAC 的新格式參考。**
3. **DAc0da v103（mod 134405）** — ❌ **完全無關**。實檔是一個大型任務 mod（`DAc0da.esm` + bsa + 語音 fuz + FOMOD，一個 Oblivion 風異界「Dac0da」），與動畫施法零關係。**不是 DAC 的後繼。**

### NG Plus vs 舊 DAC config 格式差異（**雙實檔比對**）
真實消費者 `Vokriinator Black - DAC Improved`（mod 26702）出貨的 `_DynamicAnimationCasting/FZmx - DAC - Vokriinator.toml` 用的是**舊格式**：spell 與 plugin 名**分兩欄**——`SpellFormIDs = [0xE172, 0x1EA0E, ...]`（裸 int 陣列）+ 另一欄 `SpellEspName = "SPERG-SSE.esp"`；條件也分 `RaceFormID`/`RaceEspName`、`ActorFormID`/`ActorEspName` 兩欄、且用 `TargetPlayer`/`TargetCaster`。**NG Plus 73293 已把這些統一成單一 `"Plugin.esp|0xFormID"` 字串**（`HasActorFormID`/`HasRaceFormID`…），刪掉了所有 `*EspName` 配對欄。
→ **產生器若要做，鎖定 NG Plus 73293 的統一字串格式**（與 ModForge 既有 OAR/SPID FormID 寫法一致）；舊雙欄格式只是相容性備註。

- **是否被取代（superseded）**：UNVERIFIED。Nexus 上同時存在原版 DAC（65512）與 NG Plus（73293），且原版仍有 Maxsu 的維護 fork；搜尋未明確指出單一「現行推薦」者。下載的 NG Plus 73293 是這批裡最新、runtime 統一的一支，作為格式基準合理；但「73293 已正式取代 65512」這句**未經證實**。

## 對 ModForge 的意義（ModForge relevance）

**可生成，且風險低於 BDI/PIE。** 逐項對照（已對 `src/ModForge.Core/` grep 驗證）：

- ModForge 目前**無任何 DAC 支援**（`grep -rE "DynamicAnimationCasting|loki_Dynamic|AnimationCasting" src/` 為空）。
- ModForge 已具備所有前置零件：
  - `OarGen.cs` / `BdiGen.cs` / `SpidGen.cs` / `KidGen.cs` / `FlmGen.cs` / `McmGen.cs` 證明「**生 loose-config 檔（含 OAR JSON / SPID-KID-FLM ini）**」是成熟既有能力。
  - **DAC 的 FormID 字串格式 ModForge 已會解析**：`OarConditions.ParseForm`（`src/ModForge.Core/OarConditions.cs`）正是吃 `"Plugin.esp|0xFormID"`（程式碼註解逐字寫 `"form ref must be 'Plugin.esp|0xFormID'"`，且玩家硬值用 `"Skyrim.esm|0x000007"`）——DAC 要的字串與 OAR 條件完全同形，可直接複用同一 FormRef 解析/序列化。
  - DAC 綁的 spell 多半是 ModForge **本來就在 record 層生的 SPEL**（`Generator.Build.Magic.cs` / `Spec.Magic.cs`）——能在同一份 spec 裡「定義 spell + 把它綁到動畫事件」，閉環極乾淨。
- **唯一不可生成**：`loki_DynamicAnimationCasting.dll`（與 BDI/PIE 的 DLL 同性質，列為前置依賴，玩家自裝）。`@NAME`/`@FAVOURITE` 的 runtime 綁定需作者寫 Papyrus（ModForge 的 fragment 生成能力可選配涵蓋，但非必要）。
- 與既有 action-system MVP 的關係：DAC 補上「**動畫事件 → 釋放 spell**」這條，正好接在 BDI（注入 var/event）→ PIE（動畫設 var）→ OAR（依 var 選動畫）之後，是同一條動畫戰鬥鏈的「施法輸出」端。

**判定：可生成（confirmed），格式風險低（已驗證 + FormRef 解析已存在），是動畫戰鬥 config 鏈裡最值得補的一塊。**

## Roadmap 啟示（Roadmap implications）

乾淨的 roadmap 候選：**新增 DAC config 生成器**，spec 區塊例如 `animCasts:` / `dynamicAnimationCasting:`，與 `bdi:`/`oar:` 並列。

- **輸入 spec 形狀**：`[{ animationEvent, spellFormIDs:[FormRef|"@FOREHAND"|...], conditions:{actor?,race?,weaponType?,keyword?,perk?,effect?,sneaking?,...}, chance?, cooldown?, exclusiveGroup?, costs:{health,stamina,magicka}, magnitude?, dualCasting? }]`。
- **輸出**：`SKSE/plugins/_DynamicAnimationCasting/<modName>.toml`，逐筆 emit `[[event]]`。**需要一個 TOML emitter**——目前 `grep -liE "Toml" src/ModForge.Core/` 只命中 `Vtxt.cs`（terrain，非 TOML），**沒有現成 TOML 寫出器**；但 DAC 的 TOML 子集極簡（只有 `key = value`、字串、bool、float、`[array]`、`[[event]]` 表頭），手寫 emitter 幾行即可，無需引第三方庫。
- **FormRef 直接複用** `OarConditions.ParseForm` 那條 `"Plugin.esp|0xFormID"` 管線；spell 可引用同 spec 內 `magic:` 定義的 SPEL（reverse-resolve 同既有 OAR/SPID 流程）。
- **前置依賴**：在生成的 mod requirements 標 `loki_DynamicAnimationCasting.dll`（如 BDI/PIE 那樣列為玩家自裝相依）。
- **鎖 NG Plus 73293 統一字串格式**（非舊雙欄 `*EspName`）。
- 風險評級：**低**（格式已實檔驗證、FormRef 解析已存在、無需新依賴庫）——與 BDI 生成器「實作幾乎零風險」同級，且使用面更廣（凡「招式/反擊/近戰射彈/光環」皆走它）。

---
*實檔來源（已讀）*：
- `~/skyrim_mods/unzip/dac/NGPlus/Dynamic Animation Casting NG/SKSE/plugins/_DynamicAnimationCasting/template.toml`（完整欄位定義）
- `.../_DynamicAnimationCasting/AnimEvents.txt`（合法 event 清單）
- `.../Source/Scripts/DynamicAnimationCasting.psc`（Papyrus API）
- `~/skyrim_mods/unzip/dac/VokriiDAC/SKSE/plugins/_DynamicAnimationCasting/FZmx - DAC - Vokriinator.toml`（真實授權 config，舊雙欄格式）
- `~/skyrim_mods/unzip/dac/DACv1/...DragonBase/config.json`（證實 mod 123113 是 OAR 龍動畫包，非本框架）
- `~/skyrim_mods/unzip/dac/DAc0da/...DAc0da.esm`（證實 mod 134405 是無關任務 mod）
