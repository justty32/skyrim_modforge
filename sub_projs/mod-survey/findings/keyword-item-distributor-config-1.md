# Config 語法：命名 / 主語法行 / 欄位一~三

← [keyword-item-distributor](keyword-item-distributor.md)

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

