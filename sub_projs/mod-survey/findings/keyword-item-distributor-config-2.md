# Config 語法：欄位四 traits / 欄位五 chance / 完整範例

← [keyword-item-distributor](keyword-item-distributor.md)

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

