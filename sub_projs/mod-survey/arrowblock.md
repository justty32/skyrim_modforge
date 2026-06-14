# Arrowblock SSE — ModForge 取經筆記

來源：`Arrowblock SSE-25960-1-1558536960.7z`（Nexus 25960）。是一個**小型戰鬥機制 mod**：玩家舉盾／武器格擋（block）時，能擋下／彈開飛來的箭（與其他攻擊）。

## 1. 這個 mod 做什麼 + 怎麼運作

**機制比重：Papyrus script 為主（核心擋箭判定全在腳本），PERK / MGEF / SPEL 只是「載具」把腳本掛上玩家。**

整條鏈：

```
讀書 BigIron(BOOK + AddPerkBook.psc OnRead)
  → AddPerk(Expert Block PERK) + AddSpell(Expert Parry FX SPEL)
PERK "Expert Block"(ArrowBlock 0x00639F)
  ├ effect[ability] → SPEL Blockingmod   → MGEF Blocking(archetype=Script, ConstantEffect, Self)
  │                                          → 掛 Blocking.psc(extends activemagiceffect)
  ├ effect[ability] → SPEL BlocingmodWard → MGEF BlockingWard(ValueModifier, WardPower)  ← 給一道隱形 ward
  └ effect[entryPoint] ModIncomingDamage Set 0 (conds=2)  ← 把擋下的傷害歸零
```

**真正的擋箭判定在 `Blocking.psc`（activemagiceffect）**，常駐掛在玩家身上：

- `OnHit(... akProjectile, abHitBlocked)`：
  - 判定條件：`GetAnimationVariableBool("IsBlocking") == true` **且** `abs(GetHeadingAngle(aggressor)) < 60.0`（面向攻擊者 ±60°）。
  - 成立時 `TargetActor.ClearExtraArrows()` ← **這是「擋箭」的真正動作**：清掉插在身上的箭。
  - 若來源武器 `HasKeyword(WeapType1)`（弓 `WeapTypeBow` 0x01E715 一類）：播 `BlockHitStart` 動畫事件、播音效、`Cast` 兩個 FX spell、`DamageAV("Stamina", 10)`。
  - 另有 `if(abHitBlocked)` 分支：一般格擋成功也施放 FX。
- `OnWardHit(...)`：擋法術時同理，`ClearExtraArrows()` + FX + `DamageAV("Magicka", 10)`。

**傷害怎麼被「擋掉」其實是兩層：**
1. `PERK ModIncomingDamage Set 0`（純 record，引擎層）把傷害設 0；
2. `Blocking.psc` 的 `ClearExtraArrows()`（腳本層）把已插上的箭移除，做視覺/物理上的「彈開」。

也就是說**箭的「停下」靠 PERK entry-point（引擎），箭的「拔除/彈開」靠 Papyrus（ClearExtraArrows）**，兩者合演。沒有用到 SKSE（純原版 Papyrus）。

## 2. 關鍵 record 與模式（census via ModForge `dump`/`perkdiag`）

esp 共 17 筆，master 只有 `Skyrim.esm`，非 localized。

| Record | EditorID / FormID | 角色 |
|---|---|---|
| PERK | `ArrowBlock` 0x00639F "Expert Block" | 主載具，下詳 |
| MGEF (Script) | `Blocking` 0x00434B | `archetype=Script, cast=ConstantEffect, target=Self, flags=Recover/HideInUI/Painless`；掛 `Blocking.psc`（5 properties） |
| MGEF (ValueModifier) | `BlockingWard` 0x006E69 | `av=WardPower, skill=Restoration, cast=Concentration`，帶 3 個 keyword |
| MGEF (CurePoison) | `BlockingFX` 0x005373 "ExpertParry" | FX-only，`HideInUI/Painless` |
| SPEL (Ability) | `Blockingmod` 0x0058D6 | → MGEF Blocking；ConstantEffect/Self；equip=EitherHand 0x013F44 |
| SPEL (Ability) | `BlocingmodWard` 0x006E6B | → MGEF BlockingWard（mag=35） |
| SPEL (Spell) | `BlockingmodFX` 0x00434A "Expert Parry" | → MGEF BlockingFX，FireAndForget |
| BOOK | `BigIron` 0x009981 | 掛 `AddPerkBook.psc`，OnRead 給 perk + spell；放進 vanilla 商人箱 0x10C430 override |
| ARTO / IPCT | `BlockFX` 0x006E6C / `Newimpact` 0x00639E | 擋箭視覺/撞擊特效 |
| GMST × 5 | `fBloodSplatter*` | 微調 override（與擋箭主機制無關，是 mod 順手帶的調整） |

**代表性 PERK entry-point（`perkdiag 00639F` 實測）：**

```
PERK Expert Block (0x00639F), NumRanks=1, perk-level conds=0
  effect[ability]    → BlocingmodWard
  effect[ability]    → Blockingmod
  effect[entryPoint] ModIncomingDamage  Function=Set  Value=0   (effect-level conds=2)
```

- entry-point 型別 **`ModIncomingDamage`**，function `Set`、operand `0`（把傷害歸零）。
- 帶 **2 條 effect-level 條件**（CTDA），典型就是「IsBlocking / 面向角度 / 武器型別」之類把效果限縮在格擋當下——配合 `perk-conditiontabcount-ctd`：`ModIncomingDamage` 的 PerkConditionTabCount 在 vanilla 是 **3**，必須非零、否則載入即 CTD。

**MGEF Script 模式**：核心邏輯不寫在 record 裡，而是 `archetype=Script` 的 MGEF 透過常駐 ability 掛 `extends activemagiceffect` 的腳本，用 `OnHit/OnWardHit` 攔截戰鬥事件——這是 Skyrim「被動戰鬥反應」的通用做法。

## 3. 對 ModForge 的參考價值

### 可生成（ModForge 現成能做）
- **PERK 整個骨架**：`PerkSpec` + `PerkEffectSpec`（`kind="ability"` 與 `kind="entryPoint"`）都支援。
- **`entryPoint` = `ModIncomingDamage`、`function="Set"`、`value=0`**：`ModIncomingDamage` **在 EntryPointTabCount 表內（值=3）**，ModForge build 會自動寫正確的 PerkConditionTabCount → **不會踩 `perk-conditiontabcount-ctd` 那個 0-tab CTD**。✅ 可生成。
- **effect-level conditions（2 條 CTDA）**：走共用 `ConditionSpec`/`BuildCondition`，`HasKeyword`/`GetEquippedItemType` 等都支援。✅
- **MGEF（ValueModifier `BlockingWard`、CurePoison-as-FX `BlockingFX`）+ SPEL（Ability/Spell）**：`MagicEffectSpec`/`SpellSpec` 直接對應，含 keyword、school、cast/target、equip。✅
- **MGEF `archetype="Script"`**：archetype 欄位可填 `Script`，build 會 `Enum.TryParse` 設上。✅（但見下方「需新支援」）
- **BOOK + 放進 vanilla 容器 override**：`BookSpec` + 容器 override 都在 ModForge 範圍。✅
- **GMST override、IPCT/ARTO 引用**：可引用 vanilla、或當作 override。✅

### 需新支援（spec 目前缺欄位）
- **把 Papyrus script 掛到 MGEF**：`Blocking` MGEF 的全部行為靠 `Blocking.psc`，但 `MagicEffectSpec` **沒有 `scripts`/VMAD 欄位**（只有 `ItemSpec` 等帶 `Scripts: List<ScriptAttachSpec>`）。要生成這類「Script-archetype MGEF + 附腳本」，ModForge 需在 MGEF spec 加 script-attach（VMAD）支援。⚠️
- 真要 end-to-end 生成，還需把 `.psc` 編譯/打包進 build（ModForge 已有 conditional `.pex` 流程，但 MGEF 的 VMAD 綁定是缺口）。

### 純參考（靠 Papyrus / 引擎，ModForge 不生成邏輯）
- **`Blocking.psc` 的核心擋箭判定**：`OnHit/OnWardHit` + `GetAnimationVariableBool("IsBlocking")` + `GetHeadingAngle < 60°` + **`ClearExtraArrows()`**（拔箭的關鍵 API）+ `SendAnimationEvent("BlockHitStart")` + `DamageAV`。這些是 runtime 行為，ModForge 只能生成載具 record，腳本邏輯需手寫。
- **`AddPerkBook.psc`（OnRead → AddPerk/AddSpell）**：和記憶裡 `book-onread-needs-objectreference` 一致——腳本 `extends ObjectReference` 才會觸發 OnRead（此 mod 正是如此），可生成 BOOK 但 OnRead 腳本仍需手寫掛上。

**一句話結論**：擋箭 = PERK `ModIncomingDamage Set 0`（引擎歸零傷害）＋ Script-archetype MGEF 掛 `Blocking.psc`（`ClearExtraArrows` 拔箭 + IsBlocking/角度判定）。ModForge 能完整生成 PERK/MGEF/SPEL/BOOK 載具與這個（已支援的）entry-point，唯一缺口是「替 MGEF 掛 Papyrus 腳本」的 VMAD spec 欄位，以及核心擋箭腳本本身屬純參考。
