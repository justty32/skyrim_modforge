# linkedRef 節點鏈：概念 + record 層結構

← [runtime-selector-patterns](runtime-selector-patterns.md)

## 第二部分：linkedRef 節點鏈模式

### 五、概念說明

linkedRef（在 ESP 中為 `XLKR` subrecord）是放置物件（placed ref，REFR 或 ACHR）之間的有名指標。一個 ref 可帶**多條** XLKR，每條由兩個欄位組成：**target ref**（指向什麼）+ **keyword**（這條連結叫什麼名字，可為 null）。

這個機制可以實現三種截然不同的功能：

1. **路線鏈（Route Chain）**：marker → marker → … → end，NPC Patrol package 沿 null keyword link 一格一格走，也可用 `TranslateToRef` 讓 Activator 沿鏈移動（Animated Carriage）
2. **觸發序列（Trigger Sequence）**：事件腳本呼叫 `GetLinkedRef(kwEvent1)` 取到下一個觸發點，再 `GetLinkedRef(kwEvent2)` 取分岔——實作無座標的「事件流水線」
3. **目標集合（Target Pool）**：多個 ref 的 linkedRef 都指向同一個「列表頭」，讓腳本輕鬆 scan；Extended Encounters + Immersive Interactions 用 FLST + `FormList.HasForm(ref)` 做類似事，而 linkedRef 版本是純 placement 層（無需 FLST record）

---

### 六、record 層結構

#### XLKR subrecord 格式

每條 `LinkedReference` 結構（Mutagen 型別 `LinkedReferences`）：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `Reference` | `FormLink<IPlacedGetter>` | 目標 placed ref 的 FormKey（必填） |
| `KeywordOrReference` | `FormLink<IKeywordLinkedReferenceGetter>` | keyword KYWD 的 FormKey（可為 null = default link） |

一個 placed ref 可有**多條** XLKR，只要 keyword 不同就是不同的具名連結（named link）。

#### 單向鏈 vs 雙向鏈

**單向鏈**（Carriage 路線）：
```
Start → Node1 → Node2 → Node3 → End
```
每個 ref 只有一條 `null keyword` XLKR 指向下一個。Patrol package 從 Start 出發，`GetLinkedRef()` 每次取 null link 前進；`kwAlternativePath` 是第二條具名 link，讓腳本可以 50% 機率走分岔路。

**雙向鏈**（罕見）：
```
A ←→ B（A 有 XLKR→B，B 也有 XLKR→A）
```
通常用於「對話觸發後可以雙向追溯」或特定 package 需要「回頭走」。Animated Carriage 的路線是純單向（前進 + 到達終點後靜態化）。

#### keyword-typed link（具名連結）的用途

同一個物件上掛多條不同 keyword 的 XLKR，讓同一個 ref 同時參與**不同語義的鏈**：

```
MarkerRef:
  XLKR[keyword=null]              → 下一個巡邏點（Patrol 預設跟的路線）
  XLKR[keyword=kwAlternativePath] → 分岔路線的第一個點（腳本 50% 機率走這條）
  XLKR[keyword=kwBossSpawn]       → Boss 生成點
```

Papyrus 取具名連結：
```papyrus
ObjectReference nextPatrol = MarkerRef.GetLinkedRef()                    ; null keyword
ObjectReference altPath    = MarkerRef.GetLinkedRef(kwAlternativePath)   ; 具名 keyword
```

---

