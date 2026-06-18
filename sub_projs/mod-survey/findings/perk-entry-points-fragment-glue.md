# Perk-fragment 膠水

← [perk-entry-points](perk-entry-points.md)

## 五、Perk-fragment 膠水

### 5-1 VMAD on Perk（PerkAdapter）

Perk record 的 VMAD 是 `PerkAdapter`（不是 QuestAdapter 也不是 VirtualMachineAdapter）。Mutagen 型別：

```csharp
perk.VirtualMachineAdapter = new PerkAdapter
{
    Version = 5,
    ObjectFormat = 2,
    ScriptFragments = new PerkScriptFragments
    {
        ExtraBindDataVersion = 2,
        FileName = "AnimimationsReborn_Fragments",  // .psc scriptname（不含 .pex）
        Fragments = new List<PerkScriptFlag>
        {
            new PerkScriptFlag
            {
                FragmentIndex = 0,               // 對應 Fragment_0
                Flags = PerkScriptFlag.Flag.RunImmediately,
            },
            new PerkScriptFlag
            {
                FragmentIndex = 1,               // 對應 Fragment_1
                Flags = PerkScriptFlag.Flag.RunImmediately,
            },
            // ... 每個 AddActivateChoice effect 一個
        }
    }
};
```

`PerkScriptFragments` 欄位：
- `ExtraBindDataVersion`：2（vanilla 一致值）
- `FileName`：fragment script 的 Papyrus Scriptname（CK 規範是 `AnimimationsReborn_Fragments`；ModForge 可用 `PF_<PerkEditorId>` 格式）
- `Fragments`：每個 `PerkScriptFlag` 對應一個 effect；`FragmentIndex` 對應該 effect 的 `Flags.FragmentIndex` 欄位

### 5-2 `Extends Perk` script 格式

Fragment script 的 Papyrus 格式（Immersive Interactions 真實結構）：

```papyrus
Scriptname AnimimationsReborn_Fragments extends Perk Hidden

; Properties: 通常掛一個 Quest（中央 quest script）
ObjectReference Property Activate Auto   ; AR_QuestScript

Function Fragment_0(Actor akActor, ObjectReference akTargetRef)
    Activate.fOpen(akActor, akTargetRef)    ; → 中央 quest script 的函式
EndFunction

Function Fragment_1(Actor akActor, ObjectReference akTargetRef)
    Activate.fTake(akActor, akTargetRef)
EndFunction

; ... Fragment_N 對應第 N 個 AddActivateChoice effect
```

**固定簽名**：`Fragment_N(Actor akActor, ObjectReference akTargetRef)`——引擎呼叫時傳入啟動者與目標 ref，與 TIF（`Fragment_0(ObjectReference akSpeakerRef)`）不同。

### 5-3 `Fragment_N` 命名規則

- Fragment index 從 0 開始，每個 `AddActivateChoice` effect 各佔一個。
- Index 值由 effect 的 `Flags.FragmentIndex` 欄位指定（與 ScenePhaseFragment 的 `Index` 欄位同義）。
- 同一個 Perk 的多個 effect 不能有重複的 FragmentIndex。
- `PerkScriptFragments.Fragments` 列表長度 ≥ 最大 FragmentIndex + 1；unlisted index 留空（引擎跳過）。

### 5-4 Dispatcher 模式

Immersive Interactions 示範的生成樣板：

```
PerkSpec.Effects[]
  effect[N]: AddActivateChoice
    EntryPoint = FilterActivation
    ButtonLabel = "（動畫）開門"
    Flags.FragmentIndex = N
    Flags.RunImmediately = true
    Conditions = [GetIsID(target, DoorFormId)]

Perk.VirtualMachineAdapter (PerkAdapter)
  ScriptFragments.FileName = "PF_MyPerk"
  Fragments[N].FragmentIndex = N

PF_MyPerk.psc (extends Perk)
  Quest Property CentralScript Auto   ← 綁定到 quest script FormKey
  Function Fragment_N(Actor a, ObjectReference t)
      CentralScript.HandleActivate(a, t, N)   ; 或直接展開動作
  EndFunction
```

比較既有 ModForge fragment 家族：

| fragment 家族 | 目標 record | script extends | 函式簽名 | adapter 型別 |
|---|---|---|---|---|
| TIF（dialogue） | DialogResponses | `TopicInfo` | `Fragment_0(ObjectReference akSpeakerRef)` | `DialogResponsesAdapter` |
| QF（quest stage） | Quest | `Quest` | `Fragment_Stage_XXXX_Item00000()` | `QuestAdapter` |
| SF（scene phase） | Scene | `Scene` | `Fragment_N()` | `SceneAdapter` |
| **PF（perk effect）** | **Perk** | **`Perk`** | **`Fragment_N(Actor, ObjectReference)`** | **`PerkAdapter`** |

---

