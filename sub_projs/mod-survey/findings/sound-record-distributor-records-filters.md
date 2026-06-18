# 支援的 record 類型對照 + Filter 語法

← [sound-record-distributor](sound-record-distributor.md)

## 三、支援的 record 類型對照表

### Target record 類型 × 可設的 sound 欄位

| Target（改誰） | 可設欄位 | Sound 類型（Source） |
| --- | --- | --- |
| **Weapon**（WEAP） | Pick Up, Put Down, Impact Data Set, Attack, Attack 2D, Attack Loop, Attack Fail, Idle, Equip, Unequip | BGSSoundDescriptorForm（Impact Data Set 除外用 BGSImpactDataSet） |
| **Armor Addon**（ARMA） | Footstep | BGSFootstepSet |
| **Armor**（ARMO） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Misc. Item**（MISC） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Soul Gem**（SLGM） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Magic Effect**（MGEF） | Sheathe/Draw, Charge, Ready, Release, Cast Loop, On Hit | BGSSoundDescriptorForm |
| **Projectile**（PROJ） | Active, Countdown, Deactivate | BGSSoundDescriptorForm |
| **Explosion**（EXPL） | Interior, Exterior | BGSSoundDescriptorForm |
| **Effect Shader**（EFSH） | Ambient | BGSSoundDescriptorForm |
| **Ingestible**（ALCH） | Consume | BGSSoundDescriptorForm |
| **Region**（REGN） | RDSA 陣列（Sound + Flags + Chance）| BGSSoundDescriptorForm，可新增或替換 |

**不支援**（v1.5.3）：
- NPC_（NPC 本身沒有直接 sound field，音效透過 footstep set 在 ARMA 上）
- WEAP 的 `swingDownSound`（CK 顯示的欄位，但 SRD 未列入）
- SPEL 本身（魔法音效走 MGEF，不走 SPEL）
- MusicType（MUSC）— 有另一個 mod「Music Type Distributor」(Nexus #119571) 專門處理

---

## 四、Filter 語法

SRD 的 filter 機制**遠比 SPID 簡單**。SPID 有 StringFilter/FormFilter/LevelFilter/Traits 四層；SRD 只有：

1. **Requirements**（頂層 mod 存在判斷）：整個 config 是否生效，以 mod 載入狀態決定
   - `"Mod.esp"` → 此 mod 必須存在
   - `"Mod.esp!"` → 此 mod 必須**不存在**（後綴驚嘆號）

2. **Form 直接指定**：每個 entry 直接寫 EditorID 或 FormID，沒有「按條件批量選多個 form」的能力

**沒有**：race filter、faction filter、keyword filter、NPC 名稱 filter、level range filter 等。

**結論**：SRD 不做「找哪些 NPC 來分發」，而是「直接指定某個 form，修改它的音效欄位」。精準但不批量（除非寫很多 entry）。

---

