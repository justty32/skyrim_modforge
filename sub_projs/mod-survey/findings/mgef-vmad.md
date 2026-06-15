# Script-attached MGEF（VMAD on Magic Effect）

> 為 ModForge（JSON spec → `.esp` 生成器）整理的機制筆記。Record 名 / 函式名 / Mutagen 型別名留英文；散文繁中。
> 主要來源：arrowblock.md finding、`Generator.Build.Scripts.cs`、`Generator.Build.Magic.cs`、`Spec.Magic.cs`。

---

## 一、VMAD on MGEF 的機制

### 1a. VMAD 結構

MGEF record 和多數帶腳本的 record 一樣，在 ESP 層以 VMAD（Virtual Machine Adapter）subrecord 附掛 Papyrus。VMAD 的核心欄位：

```
VirtualMachineAdapter
  Scripts[]                  ← ScriptEntry 清單（一個 MGEF 通常只有一個腳本）
    ScriptEntry
      Name         (string)  ← Papyrus class 名，必須與 .pex 的 scriptname 完全一致
      Flags        (Flag)    ← Local = 0x00（MGEF 用 Local）
      Properties[] (list)    ← typed 屬性，在 CK 裡稱「property」
        ScriptProperty
          Name     (string)
          Flags    (Flag)    ← Edited = 0x01（代表 CK 已填值）
          Data     (typed)   ← int / float / bool / string / object(FormKey + optional AliasID)
```

MGEF 的 VMAD 使用標準 `VirtualMachineAdapter`（非 Quest 用的 `QuestAdapter`，也不是 Scene 用的 `SceneAdapter`）。

### 1b. ActiveEffect 繼承需求

Papyrus 腳本若要收 MGEF 的生命週期事件，必須宣告：

```papyrus
ScriptName MyScript Extends ActiveMagicEffect
```

`ActiveMagicEffect`（或全稱 `activemagiceffect`，大小寫無關）是 Papyrus 內建的基礎類別，代表一個正在施效中的 MagicEffect 實例。只有繼承它，腳本才能接收以下事件：

| 事件 | 觸發時機 |
|---|---|
| `OnEffectStart(Actor akTarget, Actor akCaster)` | 效果開始套用（ConstantEffect 是載入時；FireAndForget 是施放後） |
| `OnEffectFinish(Actor akTarget, Actor akCaster)` | 效果結束（duration 到期、被驅散、actor 死亡） |
| `OnHit(...)` | 目標被攻擊（只有當效果持續時才攔截） |
| `OnWardHit(...)` | 目標的 ward 被擊中 |
| `OnMagicEffectApply(...)` | 另一個魔法效果嘗試套用到目標 |
| `OnSleepStart/Stop(...)` | 目標進入 / 離開睡眠 |

### 1c. MGEF Archetype 與腳本的關係

archetype 決定引擎對 MGEF 的內建行為；設為 `Script` 時，**引擎本身不做任何 gameplay 效果**，效果完全由腳本定義。其他 archetype（`ValueModifier`、`SummonCreature` 等）仍可掛腳本，腳本與引擎效果並行執行。

Arrowblock 的 `Blocking.psc` 範例：`archetype=Script`、`cast=ConstantEffect`、`target=Self`——引擎除了「把腳本掛在 actor 身上」以外什麼都不做，腳本的 `OnHit` 才是全部邏輯。

---

## 二、MGEF VMAD 在 ModForge 的支援狀況

### 2a. 通用 AttachScripts() 機制

`Generator.Build.Scripts.cs` 的 `AttachScripts()` 是 pass 2 階段執行的通用 script-attach：

```csharp
var vmadProp = target.GetType().GetProperty("VirtualMachineAdapter");
if (vmadProp is null || !vmadProp.CanWrite)
{ Warn($"  ! '{sa.TargetEditorId}' ({target.GetType().Name}) takes no script"); continue; }
```

邏輯：用反射取 `VirtualMachineAdapter` property → 若存在且可寫，就 `Activator.CreateInstance(vmadProp.PropertyType)` 建立正確型別的 adapter → 取其 `Scripts` list → 建 `ScriptEntry { Name = sa.ScriptName }` → 呼叫 `FillProperties` 填 typed properties → `scriptsList.Add(entry)`。

**關鍵問題**：Mutagen 的 `IMagicEffect`（即 MGEF）是否有可寫的 `VirtualMachineAdapter` property？

根據 Mutagen 的 record 設計，`MagicEffect` **確實有可寫的 `VirtualMachineAdapter`** 屬性（型別為 `VirtualMachineAdapter`，非特製 adapter），所以反射不會進入 `Warn` 分支。這一點也被 spellforge.md 的評估佐證：

> "MagicEffect（Script/ValueModifier archetype、castType、targetType、flags、taper、art/projectile）— `magicEffects[]` 全支援（含 `Script` + `scripts[]` VMAD 掛載）"

### 2b. Spec 缺口確認

然而 `MagicEffectSpec`（`Spec.Magic.cs`）**沒有 `scripts` / `vmad` 欄位**：

```csharp
public sealed class MagicEffectSpec
{
    public string EditorId { get; set; } = "";
    public string Archetype { get; set; } = "ValueModifier";
    // ... 所有欄位都是 scalar / ref，沒有 scripts 欄位 ...
    public List<MagicEffectSoundSpec> Sounds { get; set; } = new();
    // ← 沒有 scripts / vmad 欄位
}
```

`AttachScripts()` 的資料源是 `spec.Scripts`（頂層的通用 script-attach 清單），不是 `spec.MagicEffects[i].scripts`。所以：

- **通用路徑**：在 spec 頂層的 `scripts[]` 寫一條 `{ targetEditorId: "MyMGEF", scriptName: "MyScript", properties: [...] }` → `AttachScripts()` 就能把腳本掛到 MGEF 上。這條路**今天就能用**。
- **直覺路徑缺失**：`magicEffects[].scripts[]` 的 inline 寫法不存在——使用者必須把 script-attach 拆到頂層 `scripts[]`，增加了 spec 撰寫負擔，且文件上沒有說明這個繞路方式。

### 2c. 限制：Flags 必須手動設定

通用 `AttachScripts()` 建立 `ScriptEntry` 時，`Flags` 預設為 0（即 `Local`）。這對 MGEF 是正確的（MGEF 上的 script 應標 Local），但若未來有需要標 `Inherited` 的情境，需要在 `ScriptAttachSpec` 加 flag 欄位才能支援。

---

## 三、ActiveEffect 內建 property 表

這些是 `ActiveMagicEffect` 提供給子腳本的「隱性 property」，不需要在 VMAD 的 `Properties[]` 中宣告——由引擎在 effect 開始時自動注入：

| Property 名 | 型別 | 說明 |
|---|---|---|
| `Self` | `ActiveMagicEffect` | 指向自身（此 effect 實例） |
| `GetCasterActor()` | method → `Actor` | 施法者（注：是 method，非 property） |
| `GetTargetActor()` | method → `Actor` | 目標（注：是 method，非 property） |

`OnEffectStart(Actor akTarget, Actor akCaster)` 直接把 caster/target 以參數傳入，所以一般不需要 property binding；caster/target 只有在 `OnEffectStart` 以外的地方（如腳本的其他 function）需要透過 `GetCasterActor()` 取得。

從 Arrowblock 的 `Blocking.psc` 可見，5 個 property 全是手動宣告並在 VMAD 中綁定的業務型 property（存放 FX spell ref、sound ref 等），並非引擎自動注入。**使用者需要自己在 spec `properties[]` 裡宣告並在 VMAD 中綁定所有業務 property**——引擎只自動提供 caster/target 事件參數與 `Self`。

典型業務 property 範例（來自 Arrowblock `Blocking.psc`）：

```papyrus
Spell Property BlockingmodFX Auto    ; → SPEL ref，施放 FX
Sound Property BlockHitSound Auto    ; → SNDR ref，播音效
Float Property StaminaCost Auto      ; → float 常數
Actor Property pPlayer Auto          ; → 玩家 ref（by unique actor）
```

---

## 四、設計模式：Script MGEF 的常見 pattern

### Pattern A：SPEL + Ability MGEF + Script（常駐被動反應）

```
SPEL (type=Ability, castType=ConstantEffect, targetType=Self)
  effect → MGEF (archetype=Script, castType=ConstantEffect, targetType=Self)
              VMAD → MyScript (extends ActiveMagicEffect)
                        OnHit / OnEffectStart / ...
```

用途：把腳本常駐掛在 actor 上，攔截戰鬥事件。PERK 的 `effect[ability]` 給 SPEL，SPEL 帶 MGEF，MGEF 帶腳本。Arrowblock 的 `Blocking.psc` 是此模式的教科書範例。

### Pattern B：OnEffectStart Gate 模式

```papyrus
Event OnEffectStart(Actor akTarget, Actor akCaster)
    ; 一次性初始化 + 條件檢查 gate
    If akTarget.HasKeyword(MyKeyword) && akCaster != akTarget
        ; 觸發主邏輯
    EndIf
EndEvent
```

OnEffectStart 用於 FireAndForget 型效果的「一次性觸發」；OnEffectFinish 用於清理。Gate 條件判斷通常在 OnEffectStart 內做，避免在非目標情況下執行開銷大的邏輯。

### Pattern C：三層載具鏈（PERK → SPEL → MGEF → Script）

```
PERK
  effect[ability] → SPEL (Ability/ConstantEffect/Self)
                      effect → MGEF (Script/ConstantEffect/Self)
                                  VMAD → Script
  effect[entryPoint] → ... （純 record 層引擎邏輯，與 Script 並行）
```

此三層鏈讓 PERK 同時驅動「引擎層行為」（entry-point 改數值）與「腳本層行為」（Papyrus 事件），兩者共同演一個效果。Arrowblock 正是此模式：entry-point `ModIncomingDamage Set 0` 歸零傷害，Script MGEF 的 `ClearExtraArrows()` 拔箭。

---

## 五、對 ModForge 的評估

| 能力 | 狀態 | 根據 |
|---|---|---|
| 生成 MGEF（archetype=Script、castType、targetType、flags） | **可生成** | `BuildMagicEffects()` 有完整 enum parse；`archetype=Script` 在 `MagicEffectArchetype.TypeEnum` 中 |
| 把腳本掛到 MGEF（VMAD + ScriptEntry + typed properties） | **可生成（繞路）** | 頂層 `scripts[].targetEditorId` 指向 MGEF editorId → `AttachScripts()` 反射 MGEF 的 `VirtualMachineAdapter` property，可寫 |
| `magicEffects[i].scripts[]` inline 寫法 | ⚠️ **不存在**（需新支援） | `MagicEffectSpec` 無 `scripts` 欄位；`Spec.Magic.cs` 確認 |
| PERK → SPEL → MGEF 三層鏈生成 | **可生成** | `PerkSpec`、`SpellSpec`、`MagicEffectSpec` 均有對應 builder |
| 文件化「MGEF script-attach 用頂層 scripts[]」 | ⚠️ **缺文件** | arrowblock.md 標為缺口 partial，但沒有寫明繞路方式 |
| 生成 `.pex` 本體（OnHit/OnEffectStart 腳本邏輯） | **不支援（純參考）** | 腳本邏輯須手寫，ModForge 只生成 record 載具 |

**總結**：MGEF VMAD 在 ModForge 屬於「可生成但需繞路」的 partial 狀態。通用 `scripts[]` 頂層 attach 已能把腳本掛到 MGEF，技術上不缺功能；真正缺的是 spec 的 `magicEffects[i].scripts[]` inline 欄位（讓 MGEF 的腳本宣告貼近 record 本身，更自然），以及對應的文件說明。
