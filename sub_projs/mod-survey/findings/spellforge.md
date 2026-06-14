# Mod 調查：Spellforge（v2.3）

> 為 ModForge（JSON spec → `.esp` 產生器）做的可重用性調查。記錄型別 / key / 程式碼一律 English；散文繁中。
> 來源：`Spellforge-46482-2-3-1681048930.7z`（Nexus 46482）。**無 SKSE DLL、無 Interface/MCM SWF**——純 ESP + Papyrus + BSA。
> 相關既有筆記：[`docs/spec/SPEC-magic.md`](../../docs/spec/SPEC-magic.md)、[`docs/lifelike/cookbook-magic.md`](../../docs/lifelike/cookbook-magic.md)、[`workflows/feature-dev/landed/items-magic.md`](../../workflows/feature-dev/landed/items-magic.md)。

---

## 1. 這個 mod 做什麼

Spellforge 是一個**法術鍛造工作站**：玩家召喚 / 找到一座「Spellforge」鐵砧，把材料（轉成 *Resin* 的鍊金素材 + 紙 + 墨水）投進去，選一組條件，工作站就把對應的法術**教給玩家**（`AddSpell`），或產出捲軸 / 法杖。它也能逆向——把已會的法術「回收」(recycle) 換回材料。

關鍵在於：**它本身幾乎不含任何「可施放的法術」內容**。鍛造出來的法術全部是**別人寫好的**——vanilla 或一卡車法術 mod（Apocalypse、Odin、Mysticism、Triumvirate、Forgotten Magic、Colorful Magic…）。Spellforge 只是一個**目錄 + 取得機制**疊在這些既有法術之上。

## 2. 怎麼運作（鍛造機制）

**結論：100% 預製 SPEL 池（pre-authored pool），零 runtime MGEF 組裝。** 沒有「把火 effect + 範圍 effect 拼成新法術」這回事；玩家只是用條件**篩選並取得一個早就存在的 SPEL 記錄**。

### 2a. 兩層 esp 架構

| esp | 角色 | record 普查（用 ModForge `dump`） |
|-----|------|-----------------------------------|
| `Spellforge.esp`（122 KB） | **機器本體**：UI、狀態、FX、材料邏輯 | 80 Message、43 PlacedObject、33 FormList、30 Activator、27 GlobalShort、21 MiscItem、10 Explosion、**8 Spell、8 MagicEffect**、3 Projectile、3 Hazard、2 Quest、2 Book |
| `Spellforge - Library - *.esp`（每個法術 mod 一個，5–22 KB） | **純索引 metadata**：把該 mod 的既有法術分類進 FormList。**不含任何新 SPEL/MGEF** | 例如 `AE Spells`：32 FormList + 1 Quest + 1 Message，**零 Spell** |

核心 esp 的 8 個 SPEL / 8 個 MGEF **全是工作站機械**，不是目錄法術：`SFM_ConjureForge`（`Script` archetype，召喚鐵砧）、`SFM_ForgeEnkindle`（點火）、`SFM_ForgeHeatingHazardSpell`（Hazard：靠太近燙傷，`ValueModifier`/Health/`ResistFire`/Touch）、`SFM_ForgeVortex*`（吸入特效）、`SFM_SpellCreationFX`。沒有一個是給玩家施放的內容法術。

### 2b. 法術目錄 = 平行 FormList 的「座標系」

每個 library esp 為它的法術，沿幾條正交軸建**平行 FormList**（patch lists），library quest 上的 `sfm_librarytransferscript` 在載入時把它們 merge 進核心 esp 的 base lists：

- **Delivery**：`DeliveryAimed` / `DeliveryLocation` / `DeliverySelf`
- **Level**（複雜度）：`Level0Novice` … `Level4Master`
- **Method**：`MethodConcentration` / `MethodFireForget`
- **Principle**（「做什麼」分類）：`Principle00` … `Principle19`（20 個語意 bucket：傷害火、召喚、護盾…）

一個法術由它在這些平行清單裡的**索引位置**辨識（同一 index 跨清單對齊）。`sfm_spellstorage` 警告 *"Missing level/method/delivery flist for spell at index N"* 證明這是 index-aligned 的平行陣列，不是 keyword 標記。

### 2c. 鍛造一次的流程（`sfm_forgescript`，33 KB，核心）

1. 玩家在工作站選條件，組成一個 **"definition"**：`compose_flag_school` + `compose_flag_principle` + `compose_flag_level` + `compose_flag_method` + `compose_flag_delivery`。
2. `find_all_spells_for_definition` / `get_all_indices_for_definition`：**交集**那幾條平行 FormList，找出符合 definition 的所有預製 SPEL。
3. 技能 / 任務 gate：`get_school_skill_level` 對 `get_desired_skill_level`（MCM `RequireMagicSkill`、`RequireMasterQuest`、`_SchoolLock` / `_SchoolPrincipleLock`）。
4. 收費 + 扣料：`ApplyCostMultipliers`，magicka 花費 = `MagickaCostPerComplexity × complexity`（complexity = level tier），材料 = `Resin`（`ConvertIngredientsToResin` / `ClaimResin`）+ `paper_cost` + `ink_cost`（`Inkwell01`）。全部走 Global + MCM 滑桿可調。
5. 交付：`Game.GetPlayer().AddSpell(theSpell)`（法杖 / 捲軸則產 item）。`AddSpellExclusion` 處理互斥組。
6. **回收**（`sfm_spellrecyclescript`）：`GetEquippedSpell` → `RemoveSpell` + `RemoveAndConvert` 退回部分材料。

### 2d. SKSE / Papyrus / MCM 比重

- **SKSE DLL：0%**。完全沒有 native plugin。
- **Papyrus：100% 的邏輯**。14 個 `.pex`（藏在 BSA，根目錄那兩個只是 stub）：`sfm_forgescript`(33K, 主控)、`sfm_spellstorage`(14K, 目錄存取)、`sfm_mcmconfigscript`(13K)、`sfm_configbookscript`(13K, 用一本「設定書」當 UI)、`sfm_spellrecyclescript`、`sfm_librarycontainerscript` / `sfm_librarytransferscript`(library merge)、`sfm_principleselectorscript`、`sfm_deliveryselectorscript`、`sfm_playertrackingscript`、`sfm_castspell`、`sfm_setglobalonload`。
- **MCM**：有 SkyUI MCM（`sfm_mcmconfigscript`，`AddToggleOptionST` / `AddSliderOptionST`），但**也有一條無 SkyUI 的退路**——`SFM_ConfigBook`「Spellforge Manual」用 80 個 Message-box 串成選單。UI 完全靠 vanilla engine 的 Message / Activator / Book，**不需要任何外部 UI 資產**。

## 3. 關鍵 record 與模式

### 代表性 MGEF（皆為機械，非內容）

```
SFM_ConjureForgeEffect (0x01B8C0)   archetype=Script, CastType=FireAndForget, TargetType=Self,
  BaseCost=50, MagicSkill=Conjuration, Flags=FXPersist|PowerAffectsMagnitude,
  CastingArt=Skyrim:0x10E3CE, EquipAbility=Skyrim:0x0E755C, Keywords=Skyrim:0x0806E1
  → 召喚鐵砧；真正的工作交給 VMAD 上的 Papyrus（Script archetype）。

SFM_ForgeHeatingHazardEffect (0x01F9BD)  archetype=ValueModifier, ActorValue=Health,
  TargetType=Touch, ResistValue=ResistFire, BaseCost=5,
  Flags=Hostile|Detrimental|NoArea|FXPersist|NoRecast|PowerAffectsMagnitude|NoDeathDispel,
  Taper(Weight=0.3,Curve=2,Dur=1), Projectile=Skyrim:0x012E84
  → 配 Hazard，靠太近持續燙傷的傷害模式（教科書級的 detrimental-touch ValueModifier）。
```

### 模式重點

- **cost/magnitude/duration/area**：在 Spellforge 裡這些**不是 record 算的**——magnitude/area/duration 早就烤在預製 SPEL 裡，Spellforge 只額外加一層**取得成本**（magicka + resin/paper/ink），由 Global + MCM 滑桿 × complexity 算出。
- **casting type / delivery**：不是新設定的，而是用既有 SPEL 的 castType/targetType **當分類軸**（Aimed/Location/Self、Concentration/FireForget）反過來索引。
- **keyword**：分類用的不是 record keyword，而是**平行 FormList 成員資格**（更輕、跨 mod 不衝突）。
- **FormList 當資料庫**：33+ FormList 是整個系統的骨幹——目錄、互斥、獎勵排除、被困法術(`SFM_TrappedSpells`)全靠它。
- **Book 當 UI**：`SFM_ConfigBook` + 80 Message = 零依賴的選單系統。
- **Activator + Global + Quest（隱形控制器）**：鐵砧是 Activator，狀態存在 27 個 GlobalShort，邏輯掛在 2 個 alias-script quest 上。

## 4. 對 ModForge 的參考價值

### 對得上多少（ModForge 已支援）

| Spellforge 用到的 | ModForge 狀態 | 可生成？ |
|---|---|---|
| `MagicEffect`（Script/ValueModifier archetype、castType、targetType、flags、taper、art/projectile） | `magicEffects[]` 全支援（含 `Script` + `scripts[]` VMAD 掛載），見 SPEC-magic | **可生成** |
| `Spell` (SPEL) | `spells[]` | **可生成** |
| `Hazard` (HAZD)（如 ForgeHeating） | `hazards[]`（spell-spawn / placed-trap 兩路） | **可生成** |
| `Projectile` / `Explosion` | `projectiles[]` / `explosions[]` 鏈 | **可生成** |
| `Book`（含 teaches=spell 的 spell tome） | `books[].teaches`（spell tome / skill book） | **可生成** |
| `Enchantment` (ENCH) | `enchantments[]`（weapon/apparel/staff） | **可生成**（in-game 未驗） |
| Global / Activator / Message / MiscItem / Container | 各別 builder 都有（globals、activator placements、misc items） | **大致可生成**（細節依 SPEC） |

ModForge 的 magic 生成涵蓋率對 Spellforge 的**機械層**非常高：MGEF/SPEL/ENCH/tome/hazard/proj/expl 全中，連 Script-archetype MGEF + VMAD 掛 Papyrus 都已支援。**一個 ModForge spec 完全可以生出 Spellforge 那 8 個機械 SPEL/MGEF。**

### 缺口（需新支援）

1. **FormList 還不是 in-spec record 型別**。SPEC-worldspaces 明說「in-spec FormLists aren't a record type yet」——目前只能 reference vanilla FLST。Spellforge 的整套架構**建立在生成大量自訂 FLST + 把任意 form 塞進去**之上。要複製這類「FormList 當資料庫」模式，ModForge 需要 **`formLists[]` 能新建 FLST 並填入 in-spec / vanilla form 的 ref**。→ **這是最值得補的缺口**（不只為 magic，FLST 是泛用基礎建設）。
2. **「平行索引清單」沒有現成 helper**。就算有了 `formLists[]`，要從一批 SPEL 自動沿 school/level/delivery 軸建多條對齊清單，是一個值得做的**高階產生器**（見下）。
3. **MCM / SkyUI 設定面板**：純參考。ModForge 走的是 ESP-side / SKSE 路線，不太可能（也不需要）生成 SkyUI MCM。但 **「用 Book + Message 串成選單」這招零依賴 UI 很值得記**——ModForge 已能生 Book 和 Message，可手搭出同樣的選單 quest。

### 值得當未來功能（程序化）

- **程序化生成「法術族」**：給定 school × level × delivery × archetype 的網格，批量吐出一整族對齊的 MGEF + SPEL（+ 對應 spell tome）。Spellforge 證明了「沿正交軸組織法術」的價值；ModForge 反過來可以**生成**這種有結構的法術集，而不只是逐一手寫。標：**需新支援（高階 generator）**。
- **「目錄 + 平行 FormList」打包器**：補完 `formLists[]` 後，做一個 helper：輸入一組 spec 內的 SPEL，自動產生 Spellforge 風格的分類 FormList 集。標：**需新支援**。
- **效果組合器（runtime MGEF assembly）**：Spellforge **沒有**做這件事（它選預製的）。若 ModForge 想要真正的「runtime 拼法術」，那是另一條路（多半得靠 SKSE / Papyrus 動態建構 SPEL），Spellforge 在此**只提供反例**——它刻意避開 runtime 組裝，改用預製池 + 索引。標：**純參考（反面教材）**。

### 與既有 magic 筆記的關聯

- Spellforge 的 Script-archetype `SFM_ConjureForgeEffect` 正是 SPEC-magic「`Script`-archetype MGEF 跑 Papyrus（VMAD 掛 `scripts[]`）」段落描述的模式的真實範例。
- `SFM_ForgeHeatingHazardEffect` + Hazard 對應 SPEC-magic 的 `hazards[]` 段（spell-spawn 一路）。
- spell-tome 取得對應 cookbook-magic 的「Spell tome for a custom spell」recipe——Spellforge 的取得層其實就是 `AddSpell`，和 tome 的 first-read teach 是同一個 engine 動作。

---

**一句話總結**：Spellforge = 預製 SPEL 池 + 平行 FormList 索引 + Papyrus 取得層（零 SKSE、Book/Message 當 UI、複雜度×滑桿算成本）。對 ModForge 最大的啟示是**補 `formLists[]` 可新建 FLST**這個泛用缺口，以及一個未來的**程序化法術族 generator**；它的機械層（MGEF/SPEL/HAZD/PROJ/EXPL/tome）ModForge 今天就能整套生成。
