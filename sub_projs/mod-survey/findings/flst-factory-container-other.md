# 分類容器模式 + 其他模式

← [flst-factory](flst-factory.md)

## 三、分類容器模式（Missives 風格）

### HasForm condition 與分類邏輯

Missives 用 FLST 當**集合成員判斷門**，不取值、不走 index，只問「這個 form 在不在清單裡」：

```
FLST "_M_ListLocationsForbidden"  → 禁止的地點清單
FLST "_M_ListPeopleForbidden"     → 禁止的 actor 清單
FLST "_M_ListQuests<Hold>Low"     → Whiterun hold、Low tier 的 quest 池
FLST "_M_ListQuests<Hold>Med"     → ...
```

**使用方式分兩層**：

1. **quest alias fill 條件（engine-side）**：Quest alias 的 fill conditions 可以用 `HasForm(FLST)` 或 `NOT HasForm(FLST)` 過濾——例如 `Alias_Dungeon` 的 fill condition 說「`NOT HasForm(_M_ListLocationsForbidden)`」，引擎就不會挑選禁止地點。

2. **Papyrus 遍歷池**（`_M_ActivatorScript.UpdateQuests`）：

```papyrus
Function UpdateQuests(Int chance, FormList akQuestPool)
    Int i = 0
    While i < akQuestPool.GetSize()
        Quest q = akQuestPool.GetAt(i) as Quest
        If Utility.RandomInt(0, 100) < chance
            q.Start()
        EndIf
        i += 1
    EndWhile
EndFunction
```

此處 FLST 當「容器」被遍歷，而非當「索引表」取對應值。

### HasForm vs GetAt 的語義差異

| API | 語義 | 典型用法 |
|---|---|---|
| `HasForm(form)` | 集合成員測試（O(n) 掃描，但 FLST 通常小） | condition gate；分類判斷 |
| `GetAt(index)` | 按位置取值 | 索引對齊交集；遍歷池 |
| `AddForm(form)` | runtime 追加（需 .pex 呼叫） | library merge；FLM 模式 |

**在 quest alias fill conditions 裡**，engine-side 原生支援「`HasForm(FLST)` 作為 fill 過濾條件」，這是 Papyrus 以外的第二個 FLST 使用場景。

---

## 四、其他模式

### FLST as SPID target

SPID（po3 SpellPerkItemDistributor）的 ini 可以把 FLST 當分發目標（type=Spell 時 value 可以是 FLST，分發清單內的所有 SPEL）或作為 FormFilter（`Form = 0x123~mod.esp` 可以是 FLST，SPID 展開其成員）。這讓 FLST 成為**無衝突 NPC 分發的彙整點**：各 mod 把自己的 spell 加到同一個 FLST，SPID 的 ini 只需指向該 FLST 就能一次分發所有成員。

此模式的 FLST 本身是靜態（編譯期填入），追加由 FLM 或 SPID ini 層處理，不需 runtime Papyrus。

### FLM runtime 追加模式

FormList Manipulator（FLM DLL）的 `_FLM.ini` 讓外部 plugin 在 `kDataLoaded` 時動態把 form 追加進任意 FLST（包括 vanilla 或他人 mod 的 FLST），**不衝突**（不需要 override 那個 esp）。

```ini
; _FLM.ini 語法（FormListManipulator finding）
[FormList]
; 把 MySpell 加進 SomeOtherMod 的 FLST
Form = 0x123456~SomeOtherMod.esp
Target = 0xABCDEF~TargetMod.esp
```

此模式是 ESP-side 無法達成的缺口補完（esp override FLST 會覆蓋其他 mod 的追加）。

---

