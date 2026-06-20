# Config 語法：Collection / ModEvent / 快捷 / 完整範例 / Debug

← [formlist-manipulator](formlist-manipulator.md)

### Collection 定義

用 FormType + Keyword 條件批次選取 form，類似 KID 的 filter 概念：

```ini
Collection = <Name>|<FormType>|<Keyword>, -<ExcludeKeyword>, ...|<Filter>
```

支援的 FormType（不分大小寫）：
```
Armor, Weapon, Ammo, MagicEffect, AlchemyItem, Scroll, Location,
Ingredient, Book, Misc, Key, Soulgem, Activator, Flora, Furniture,
Race, TalkingActivator, Enchantment, NPC, Spell
```

Keyword 邏輯：列出的 keyword 全部要**同時具備**（AND）；`-Keyword` 排除。

```ini
; 所有鐵製單手戰斧
Collection = IronWarAxes|Weapon|0x0001E718~Skyrim.esm, WeapTypeWarAxe
; 在 FormList 中展開此 Collection 的內容
FormList = SomeList|#IronWarAxes
```

---

### Mod Event 操作

由 Papyrus 在執行期發送 event，觸發 FLM 在 runtime 動態修改 FLST：

```ini
ModEvent = <EventName>|<FList>|<Form>, <Form>, ...
```

FLM 發送確認 event：`<EventName>OK`（如 `TestEventOK`）。

```ini
ModEvent = TestEvent|BYOHRelationshipAdoptionPlayerGiftChildMale|BYOHChefDoll
```

---

### 快捷語法（特化場景）

FLM 為 Skyrim 的特定系統提供語法糖：

```ini
; 植物採集綁定（Ingredient/Alchemy ↔ Flora/Tree/Container）
Plant = <ingredient_or_alchemy>|<flora_or_tree_or_container>|<Filter>

; 孤兒院男孩玩具池
BToys = <Form>, <Form>, *<FormList>, #<Group>|<Filter>

; 孤兒院女孩玩具池
GToys = <Form>, #<Group>|<Filter>

; 角色建立毛髮顏色池
HairColors = <Form>, *<FormList>|<Filter>

; Atronach Forge 配方（材料 → 結果）
AtronachForge = <Recipe>|<Result>|<Filter>

; Atronach Forge 印記石配方
AtronachForgeSigil = <Recipe>|<Result>|<Filter>

; Dragonborn 蜘蛛工藝配方
DragonbornSpiderCrafting = <Recipe>|<Result>|<Filter>
```

---

### 完整範例 ini

> ⚠️ **更正（IN-GAME 2026-06-20）**：下方原本的 `[General]` 區段頭為臆測且**有害**——FLM v1.8.1 見到它會判 `Config file is empty` 跳過整檔。實際 config 無區段頭，直接從定義/操作行開始。

```ini
; 定義別名（多個目標 FLST 合一）
Alias = TestAlias|0x8246~HearthFires.esm, 0x03008246~HearthFires.esm

; 定義 filter（需要 HearthFires 安裝）
Filter = HFFilter|+HearthFires.esm

; 定義 Group（form 集合）
Group = Dolls|BYOHChefDoll, BYOHDBDoll, BYOHDragonbornDoll

; 定義 Collection（以 keyword 批次選型）
Collection = IronWarAxes|Weapon|0x0001E718~Skyrim.esm, WeapTypeWarAxe

; 把一組娃娃加入 Alias 的所有目標 FLST
FormList = #TestAlias|#Dolls

; 直接操作（帶 filter）
FormList = BYOHRelationshipAdoptionPlayerGiftChildMale|BYOHChefDoll|#HFFilter

; Mod Event 動態操作
ModEvent = TestEvent|BYOHRelationshipAdoptionPlayerGiftChildMale|BYOHChefDoll

; 植物綁定
Plant = zzzCHMountainFlower01White|zzzCHTreeFloraWhiteFlowers

; 孩童玩具
BToys = #Dolls
```

---

### Debug 模式

在 Data/ 下放置一個 `FormListManipulator_DEBUG.ini` 空檔（或任意內容），啟用詳細 log 輸出。

---

