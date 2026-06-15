# Mod 調查：Keyword Item Distributor（KID）v3.4.0

> 為 ModForge（JSON spec → `.esp` 產生器）做的可重用性調查。記錄型別 / key / 程式碼一律 English；散文繁中。
> 來源：`Keyword Item Distributor-55728-3-4-0-1710234193.7z`（Nexus 55728，作者 powerof3）。
> 同系列工具：SPID（Spell Perk Item Distributor）。兩者合稱「**分發層**」。

---

## 一、這個 mod 做什麼 + 怎麼運作

KID 是一個 SKSE DLL plugin（`po3_KeywordItemDistributor.dll`）。它在**遊戲啟動時**讀取 Data 資料夾下所有以 `_KID.ini` 結尾的設定檔，按設定把指定的 **Keyword（KYWD）掛到各種 item/object record 上**，完全不需要修改任何 ESP/ESM。

### 運作流程

1. 遊戲啟動，SKSE 載入所有 plugin DLL。
2. KID 掃描 `Data/*.ini`（含子資料夾），篩出 `_KID.ini` 結尾的檔案。
3. 每行 `Keyword = ...` 依設定的 type/filter/traits/chance 批次套用。
4. Keyword 物件如果 KID 找不到對應的 FormID/EditorID，**可以動態建立一個新的 KYWD**（僅限此特殊模式）。
5. 套用結果寫進 `po3_KeywordItemDistributor.log`（`My Games/Skyrim Special Edition/SKSE/`）供除錯。

### 與 ESP 模式的差異

KID 的修改發生在**記憶體層（runtime）**，不寫入任何 ESP。多個 mod 可以各自帶一份 `_KID.ini`，互不衝突，不需要相容補丁。

---

## 二、Config 格式語法全集

### 檔案命名

```
<任意名稱>_KID.ini      ; 放在 Data/ 或 Data 任意子資料夾下
```

### 主語法行

```ini
Keyword = <keyword>|<type>|<strings_or_formIDs>|<traits>|<chance>
```

每個欄位以 `|` 分隔，後面欄位可省略（省略即為 `NONE`/不限）。

---

### 欄位一：`<keyword>`（必填）

指定要分發的 Keyword record，三種寫法：

| 寫法 | 說明 | 範例 |
|------|------|------|
| EditorID | 直接用 keyword 的 EditorID | `WeapTypeSword` |
| `formID~esp` | FormID（省略前導零）+ ESP 名稱 | `0x1234~MyMod.esp` |
| Skyrim/DLC | DLC/Skyrim.esm 的 keyword 可省略 `~esp` | `0x0806E1` |

> **動態建立 Keyword**：若填入的 EditorID 在所有載入的 plugin 中都找不到，KID 會**自動建立一個新的 KYWD**（此行為供 keyword 生成工具使用）。

---

### 欄位二：`<type>`（必填）

指定要把 keyword 套到哪種 record 類型：

```
Weapon, Armor, Ammo, Magic Effect, Potion, Scroll, Location,
Ingredient, Book, Misc Item, Key, Soul Gem, Spell, Activator,
Flora, Furniture, Race, Talking Activator, Enchantment
```

---

### 欄位三：`<strings_or_formIDs>`（可省略）

用來**過濾候選物件**的字串或 FormID 清單，逗號分隔。省略或填 `NONE` 表示套用到該 type 的全部物件。

| 運算子 | 語意 | 範例 |
|--------|------|------|
| 預設（無前綴） | OR 匹配：名稱 / EditorID / keyword 之一符合即入選 | `Iron Sword, Steel Sword` |
| `+` 前綴 | AND 要求：物件**必須同時具備**所有列出的 keyword | `ArmorTypeHeavy+ArmorGauntlet` |
| `-` 前綴 | 排除：符合此條件的物件**不套用** | `-Wooden Sword` |
| `*` 前綴 | Wildcard：名稱/EditorID 包含此字串即符合 | `*Iron` |
| `formID~esp` | 直接以 FormID 過濾 | `0x02019C9D~Skyrim.esm` |
| `.nif` 路徑 | 以 NIF model 路徑過濾（Weapon/Armor） | `*steelmace.nif` |

評估順序：Requirements（+） → Exclusions（-） → Matches → Wildcards

---

### 欄位四：`<traits>`（可省略）

對各種 type 專屬的物件屬性做精確過濾：

#### Armor 特有 traits

| 代碼 | 說明 | 範例 |
|------|------|------|
| `E` | 已附魔（enchanted） | `-E`（排除已附魔） |
| `T` | 模板物件（template） | `T` |
| `AR(min/max)` | 護甲值範圍（浮點） | `AR(10/50)` |
| `W(min/max)` | 重量範圍（浮點） | `W(0/5)` |
| `HEAVY` | 重甲類 | `HEAVY` |
| `LIGHT` | 輕甲類 | `LIGHT` |
| `CLOTHING` | 服裝類（無護甲值） | `CLOTHING` |
| 槽位 30–61 | 身體部位槽位（Body=32, Head=30…） | `32` |

#### Weapon 特有 traits

| 代碼 | 說明 |
|------|------|
| `E` | 已附魔 |
| `T` | 模板物件 |
| `W(min/max)` | 重量範圍 |
| `D(min/max)` | 傷害範圍 |
| `HandToHandMelee` | 徒手 |
| `OneHandSword` / `OneHandDagger` / `OneHandAxe` / `OneHandMace` | 單手武器各型 |
| `TwoHandSword` / `TwoHandAxe` | 雙手武器各型 |
| `Bow` / `Crossbow` / `Staff` | 弓、弩、法杖 |

#### Magic Effect（MGEF）特有 traits

| 代碼 | 說明 |
|------|------|
| `H` | 敵對效果（hostile） |
| `D` | delivery 類型 |
| `CT` | casting type |
| `R(value)` | resistance type |
| `school(min/max)` | 魔法系 + 技能值範圍（如 `20(0/25)` = Destruction 0–25 skill） |

School 數值：Alteration=18, Conjuration=19, Destruction=20, Illusion=21, Restoration=22, EnchantingSkill=23

#### Ammo 特有 traits

| 代碼 | 說明 |
|------|------|
| `B` | bolt（弩矢） |
| `D(min/max)` | 傷害範圍 |

#### Potion / Ingredient 特有 traits

| 代碼 | 說明 |
|------|------|
| `P` | 是毒藥（poison） |
| `F` | 是食物（food） |

#### Book 特有 traits

| 代碼 | 說明 |
|------|------|
| `S` | 傳授法術的書（spell tome） |
| `AV` | 傳授 actor value 的書（技能書） |
| 數值（如 `20`） | 指定具體 ActorValue 編號（20=Destruction…） |

#### Soul Gem 特有 traits

| 代碼 | 說明 |
|------|------|
| `BLACK` | 黑魂石 |
| `SOUL(size)` | 已填充靈魂大小（1=Petty … 5=Grand） |
| `GEM(size)` | 石材等級 |

#### Spell / Enchantment 特有 traits

| 代碼 | 說明 |
|------|------|
| `ST` | Spell Type（0=Disease, 1=Power, 2=Ability, 3=Poison, 4=Enchantment, 5=Potion, 6=Scroll…） |

---

### 欄位五：`<chance>`（可省略）

套用機率，範圍 0.0–100.0。預設 100（省略或填 `NONE`）。每次遊戲開始時固定，不會每次載入都重算。

---

### 完整範例

```ini
; 把 MysticismSpells keyword 套到 MysticismMagic.esp 裡所有 Magic Effect
Keyword = MysticismSpells|Magic Effect|MysticismMagic.esp

; 把 0x1234 keyword 套到所有「名稱含 Iron」的重甲手套，排除已附魔品
Keyword = 0x1234~MyMod.esp|Armor|*Iron|ArmorTypeHeavy+ArmorGauntlet,-E

; 把 NoviceDestruction 套到所有 Destruction 技能值 0–25 的 MGEF
Keyword = NoviceDestruction|Magic Effect|NONE|20(0/25)

; 把 PoisonousFood 套到同時是毒藥且是食物的 Potion
Keyword = PoisonousFood|Potion|NONE|P,F

; 套到名稱含 Bound 的所有弓矢，機率 50%
Keyword = MysticalAmmo|Ammo|*Bound|NONE|50

; 直接用 FormID 過濾兩個 MGEF
Keyword = MagicDamageSun|Magic Effect|0x02019C9D~Skyrim.esm,0x0200A3BB~Skyrim.esm

; spell tome for Destruction 書
Keyword = SpellTomeDestruction|Book|NONE|S,20
```

---

## 三、對 ModForge 的參考價值

### 現況分析（推斷）

KID 本身不是一個「要生成內容」的工具，而是一個**輔助分發層**。ModForge 主路線是生成 ESP 裡有完整 record（含 keyword）的物件，而 KID 的場景是「我不想改原有 ESP，但我想在 runtime 給它們加 keyword」。

| 場景 | ModForge ESP 方式 | KID 方式 |
|------|-----------------|---------|
| 自己建立的物件加 keyword | 直接在 spec 裡設 `keywords[]` | 不必要 |
| 給**別的 mod 的物件**加 keyword | 需要 override record → 有衝突風險 | KID ini 零衝突 |
| 動態批次套用（如：所有名稱含 Iron 的武器） | 需逐一列舉或寫 Papyrus | KID 一行搞定 |
| 修改 vanilla record 的 keyword | 需要 override Skyrim.esm record | KID 不改 ESP |

### ModForge 可生成的部分（推斷）

- ModForge 已能在 spec 裡給自建物件設 `keywords[]`，這覆蓋了**自有物件**的場景。
- **`_KID.ini` 的生成**：若 spec 有「我想給某類物件批次分發 keyword」的需求，ModForge 可以**輸出一份 `_KID.ini` 文字檔**，不需要任何 Mutagen 支援——純文字生成。這是最低成本的整合路線。→ **可生成（純文字輸出，低成本）**（**推斷**）

### 需新支援的部分（推斷）

- ModForge 目前沒有「批次 keyword 分發 spec」的欄位（如 `distributeKeywords[]`）。若要支援，需要在 spec schema 加一個 KID-ini-generator 路徑。→ **需新支援（spec schema + 文字輸出器）**（**推斷**）

### 純參考部分

- **Trait 過濾語法**：`AR(min/max)`, `OneHandSword`, `HEAVY/LIGHT/CLOTHING` 等分類概念可以作為 ModForge spec 裡 `filter` 欄位設計的靈感。

---

## 四、與同層工具的分工

### KID vs SPID

| 面向 | KID | SPID |
|------|-----|------|
| **分發目標** | **物件**（item/armor/weapon/MGEF/book/ammo…） | **NPC**（Actor） |
| **分發的東西** | Keyword（KYWD） | Spell/Perk/Item/Shout/Package/Outfit/Keyword/Faction/DeathItem |
| **filter 軸** | 物件屬性（護甲值、武器型、魔法系…） | NPC 屬性（等級/技能/性別/種族/location） |
| **config 後綴** | `_KID.ini` | `_DISTR.ini` |
| **共同點** | 同一套 filter 運算子（+/-/*）；都是 runtime，不改 ESP |

**分工原則**：
- 「這個 keyword 屬於**物件本身**的分類屬性」→ **KID**
- 「這個 spell/perk/item **要給 NPC 帶上**」→ **SPID**
- 兩者可互補：SPID 可以把 KID 生成的 keyword 的物件分發給 NPC（`Item = 0x...|...|keyword+` 形式的 keyword filter 引用 KID 打好的 keyword）。

### KID 與 ESP-side keyword 的分工

| 場景 | 推薦方式 |
|------|---------|
| ModForge 自己建的物件 | **ESP-side**：在 spec 直接設 `keywords[]`，乾淨且可驗證 |
| 需要改別人 mod 的物件 | **KID**：零衝突，不需要 override record |
| 需要動態批次（按名稱/型別/屬性掃描） | **KID**：一行 `*Iron|Armor|NONE|HEAVY` 搞定 |
| 需要在遊戲邏輯中判斷 keyword 是否存在 | 兩者皆可；ESP-side 更可預測 |

---

**一句話總結**：KID = runtime keyword 批次分發器，以 `_KID.ini` 對物件（非 NPC）做 no-ESP-override keyword 掛載；與 SPID 正交（KID 管物件，SPID 管 NPC）。對 ModForge 最實際的整合是加一條「輸出 `_KID.ini` 文字」的路徑，補足「給其他 mod 物件加 keyword」這個 ESP override 做不到的場景。
