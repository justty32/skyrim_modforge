# filter/條件語法詳解 + 真實範例

← [animobject-swapper](animobject-swapper.md)

## 三、filter / 條件語法詳解

### 完整評估流程

```
Actor 即將使用 ANIO "X"
  → 查 _animObjectsConditional["X"] 的條件列表
  → 逐一 PassFilter(actor, conditions)
    → 評估 ALL / NOT / MATCH / ANY
    → 評估 Traits（sex / child）
  → 找到第一個符合條件的 ConditionalSwap → 從其 swappedAnimObjects 隨機選一
  → 若無條件符合 → 查 _animObjects["X"] 無條件替換（若存在）
  → 若仍無 → 用原始 ANIO
```

### Filter 查找方式

當 filter 字串為 EditorID：
1. 先嘗試 `GetFormEditorID` 解析為 FormID
2. 解析失敗則當作 string（用於 `*` ANY 模式）

BOS 的 filter 支援 cell/worldspace/region；AOS 的 filter 更偏向 **actor 本身的屬性**（faction, race, keyword, spell, npc base），不是地點。AOS 沒有 location/worldspace filter（那是 BOS 的強項）。

## 四、真實範例（從原始碼逆向的格式示意）

> 注意：以下範例是根據原始碼語法構造的示意，非從真實 `_ANIO.ini` 文件直接讀取。

**範例一：無條件隨機替換**

```ini
[DrinkingCupANIO]
; 持有 DrinkingCup idle 的 actor 隨機拿 3 種杯子之一
DrinkingCupANIO|WoodCupANIO,MeadHornANIO,GlassCupANIO
```

解釋：所有 actor 使用「喝飲料」idle 時，拿什麼杯子隨機從三種中選一個。

**範例二：依 NPC 身份替換**

```ini
[DrinkingCupANIO|SofiaFollower]
; 只對 Sofia 這個 NPC（base form）生效
DrinkingCupANIO|SofiaSpecialMugANIO
```

解釋：Sofia 拿她的專屬杯子，其他人拿原本的 cup。

**範例三：依 Faction + 性別替換**

```ini
[BookReadingANIO|+ThievesGuildFaction|F]
; 盜賊公會的女性成員拿特定書本
BookReadingANIO|ThievesGuildLedgerANIO
```

**範例四：排除小孩 + 依種族**

```ini
[DrinkingCupANIO|ArgonianRace|-C]
; 亞龍人（非小孩）拿亞龍人風格杯子
DrinkingCupANIO|ArgonianDrinkingVesselANIO
```

**範例五：FormID 格式（更精確，避免 editorID 衝突）**

```ini
[0x12345~Skyrim.esm|0xABC~CompanionsMod.esp]
0x12345~Skyrim.esm|0xDEF~CompanionsMod.esp,0xDEF2~CompanionsMod.esp
```

