# Global-as-selector：概念 + 各消費方語法

← [runtime-selector-patterns](runtime-selector-patterns.md)

## 第一部分：Global-as-selector 模式

### 一、概念說明

GlobalVariable（GLOB record）是 Skyrim 中**唯一能被多種不同系統在執行期同時讀取**的有名數值：

- Papyrus 腳本可 `SetValue()` / `GetValue()` / `Mod()`
- CTDA 條件可用 `GetGlobalValue` 無需 Papyrus
- OAR / DAR 條件 JSON 可直接引用 GLOB 的 FormKey
- SPID `_DISTR.ini` 的 filter 可讀 global
- SkyPatcher ini 也可作 global condition

這代表同一個 GlobalVariable 可以當**多個異質系統的共享信號**：Papyrus 負責**寫**（設狀態），外部 framework 負責**讀**（決定動畫/分發/條件）。這就是 "Global-as-selector" 的核心意義：Global 不是單向開關，而是一個**執行期狀態匯流排**，讓沒有直接 API 相依的系統能間接協作。

**典型實例**：
- **Immersive Interactions**：`AR_DogUp`（GlobalShort）在 Papyrus 播動畫前 `SetValue(N)`（1–7），DAR 的 `_conditions.txt` 用 `ValueEqualTo(...|0x0000AA13, N)` 選對應變體 `.hkx`，播完 `SetValue(0)` 清值。Global 是唯一的狀態橋。
- **Conditional Expressions**：`CondiExp_CurrentlyBusy`（GlobalShort）作 mutex：busy=1 時其他 magic effect 跳過、busy=0 才搶佔執行。`CondiExp_PlayerIsDrunk` 等多個 global 被 dialogue INFO condition `GetGlobalValue` 直接讀取，不走 Papyrus。

---

### 二、各消費方的語法

#### DAR `_conditions.txt`（舊，以 Immersive Interactions 為例）
```
ValueEqualTo("ImmersiveInteractions.esp"|0x0000AA13, 1.0)
```
- 第一參數：GLOB 的 FormLink（`"Plugin.esp"|0xFormID`）
- 第二參數：比對的 static float 值
- 多資料夾分別對應不同 N，DAR 選最高優先通過的那層

#### OAR `config.json` — `CompareValues`
OAR 把 DAR 的 `ValueEqualTo` 通則化為 `CompareValues`，值的型別可以是 static/global/AV/graph variable 四選一：

```json
{
  "condition": "CompareValues",
  "requiredVersion": "1.0.0.0",
  "Value A": {
    "globalVariable": "Plugin.esp|0x0000AA13",
    "type": "Global"
  },
  "Comparison": "==",
  "Value B": { "value": 1.0 }
}
```

或用 BFCO 示範的 behavior graph variable 版本：

```json
{
  "condition": "CompareValues",
  "requiredVersion": "1.0.0.0",
  "Value A": { "graphVariable": "BFCO_iAttackVariants", "graphVariableType": "Int" },
  "Comparison": "==",
  "Value B": { "value": 1.0 }
}
```

兩種 "Value A" 可混搭：`"type": "Global"` 引用 GLOB，`"type": "ActorValue"` 引用 AV，`"type": "FloatValue"` 是 static 常數。

#### SPID `_DISTR.ini` filter
SPID（Spell Perk Item Distributor）的 filter 段支援 global：
```ini
Spell = MySpell|...|...|...|G(MyMod_AnimState == 1)|...
```
`G(EditorID == value)` 語法在 kDataLoaded 後求值；global 為 0/1 常見用法（啟用/停用分發）。

#### Papyrus `GlobalVariable` 直接讀寫
```papyrus
GlobalVariable Property MyMod_AnimState Auto

; 設值（selector 進入某狀態）
MyMod_AnimState.SetValue(2.0)

; 讀值（條件判斷）
if MyMod_AnimState.GetValue() == 0.0
    ; …
EndIf

; 增量（busy counter、reputation）
MyMod_AnimState.Mod(1.0)
```

#### CTDA condition（dialogue INFO / perk / package）
在 CK / Mutagen 層：`GetGlobalValue` function，參數指向 GLOB FormKey，比對值設在 condition 欄位。Dialogue INFO 可用此條件讀玩家狀態，完全不需要腳本。

---

