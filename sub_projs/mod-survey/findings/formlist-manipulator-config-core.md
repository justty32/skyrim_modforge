# Config 語法：命名 / FormID / 主操作行 / Filter / Alias / Group

← [formlist-manipulator](formlist-manipulator.md)

## 二、Config 格式語法全集

### 檔案命名

```
<任意名稱>_FLM.ini      ; 放在 Data/ 下
<任意名稱>.ini           ; 放在 Data\FLM\ 子資料夾下
```

處理順序：Data/ 字母序 → Data\FLM/ 字母序。所有 Alias / Filter / Group / Collection **跨檔案共用**（在後面的 ini 裡仍可引用前面 ini 定義的 alias）。

---

### FormID 格式

FLM 自動處理 light plugin（ESL/ESPFE）與 standard plugin 的 FormID，不需要手動切割位元組：

```
0xD80          ; 普通寫法（省略前導零）
0x03008246     ; 完整寫法
0xFE00080A     ; ESL plugin（0xFExxxxx）
0x8246         ; 也可以省略全部前導零
```

---

### 主操作行：FormList

> ⚠️ **更正（IN-GAME 2026-06-20）**：**不要**寫 `[General]` 區段頭。FLM v1.8.1 的 config 解析器吃**裸的 `Key = ...` 行清單**；一旦檔首有 `[General]`，FLM 會 log `Config file is empty` 並**整檔跳過**（實機抓到，ModForge `FlmGen` 已移除）。真實能跑的 config（如 `ImGladYoureHere_FLM.ini`）皆無區段頭。下面示例原本的 `[General]` 為早期臆測，已誤。

```ini
FormList = <FList>|<Form>, <Form>, *<FormList>, #<Group>, #<Collection>|<Filter>
```

| 部分 | 說明 | 範例 |
|------|------|------|
| `FList` | 目標 FormList（EditorID / `FormID~ESP` / `#Alias`） | `BYOHRelationshipAdoptionPlayerGiftChildMale` |
| `Form` | 要加入的 form（EditorID / `FormID~ESP`） | `0x8246~HearthFires.esm` |
| `*FormList` | 加入**另一個 FormList 的內容**（展開），而非 FormList 本身 | `*SomeOtherList` |
| `#Group` | 引用 Group 定義 | `#Dolls` |
| `#Collection` | 引用 Collection 定義 | `#IronWeapons` |
| `Filter` | 可選，引用 Filter 定義（`#FilterName`） | `#AdditionalHearthfireDollsFilter` |

---

### Filter 定義

```ini
Filter = <FilterName>|<Condition>, <Condition>, ...
```

| 條件語法 | 語意 |
|---------|------|
| `+PluginName.esp` | 此 plugin 必須**已啟用**才執行 |
| `-PluginName.esp` | 此 plugin 必須**未啟用**才執行 |
| `+A.esp&-B.esp` | A 啟用**且** B 未啟用（AND） |
| 多個 Condition 用逗號分隔 | OR 邏輯（任一為真即通過） |

```ini
; 範例：HearthFires 啟用且 Vigilant 未啟用
Filter = MyFilter|+HearthFires.esm&-Vigilant.esm
```

---

### Alias 定義

把多個目標 FormList 綁成一個名稱，統一操作：

```ini
Alias = <AliasName>|<FList>, <FList>, ...
```

引用：在 FormList 的 FList 欄位用 `#AliasName`。

```ini
Alias = TestAlias|0x8246~HearthFires.esm, 0x03008246~HearthFires.esm
FormList = #TestAlias|BYOHChefDoll
```

---

### Group 定義

把一組 form ref 打包成可重用的「form 集合」：

```ini
Group = <GroupName>|<Form>, <Form>, *<FormList>, #<Collection>, ...
```

引用：在 FormList 的 Form 欄位用 `#GroupName`。

```ini
Group = Dolls|BYOHChefDoll, BYOHDBDoll, BYOHDragonbornDoll, BYOHJesterDoll
FormList = BYOHRelationshipAdoptionPlayerGiftChildMale|#Dolls
```

---

