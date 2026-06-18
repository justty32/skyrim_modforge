# Filter 語法：String / Form / Level

← [spid](spid.md)

## 四、Filter 語法詳解

### 4.1 StringFilters（第 2 欄）

接受：**Keyword EditorID**、**NPC actorbase EditorID**、**NPC 顯示名稱（Name）**。

多個字串用逗號 `,` 分隔。

**修飾子（modifier）**——每個「expression」只能用一種：

| 修飾子 | 語義 | 範例 |
|---|---|---|
| （無） | 精確比對（exact match）| `Lydia` — 只對名字 = Lydia 的 NPC |
| `-` 前綴 | 排除（exclusion）— NPC 不符合才通過 | `-Nazeem` — 排除 Nazeem |
| `*` 前綴 | 前綴/部分比對（partial match）| `*Guard` — 名字含 Guard 的 NPC |
| `+` 連接 | 複合條件（AND）— NPC 必須同時符合全部 | `ActorTypeGhost+Vampire` — 必須是 ghost 且 vampire |

**範例**：
```ini
; 所有 ActorTypeNPC（精確 keyword）
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC

; 分發給 ActorTypeNPC，但排除 Nazeem
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem

; 名字含 Bandit 的 NPC
Spell = 0x1A6CC~Skyrim.esm|*Bandit

; 同時是 ghost 且是 vampire 的 NPC
Shout = 0x13E07~Skyrim.esm|ActorTypeGhost+Vampire
```

> **注意**：`*` 只在 StringFilter 有效，FormFilter 不支援萬用字元。

### 4.2 FormFilters（第 3 欄）

接受：特定 form 的 FormID 或 EditorID，以逗號 `,` 分隔。

**可接受的 form 型別**：
- `Faction`（FACT）
- `Race`（RACE）
- `Class`（CLAS）
- `CombatStyle`（CSTY）
- `Outfit`（OTFT）
- `NPC_`（NPC，指定特定 actorbase）
- `Spell`（SPEL）
- `VoiceType`（VTYP）
- `FormList`（FLST）
- `EditorLocation`（位置）

**排除**：同樣用 `-` 前綴排除某個 form：`-0x101~MyMod.esp`。

**範例**：
```ini
; 分發給屬於 BanditFaction 的 NPC
Perk = MyPerk|NONE|BanditFaction

; 分發給 Nord 種族 NPC
Spell = MySpell|NONE|NordRace

; 分發給某 FormList 內的 NPC
Spell = MySpell|NONE|0x123~MyMod.esp

; 排除某 faction
Faction = MyFact|NONE|-EnemyFaction
```

### 4.3 LevelFilters（第 4 欄）

兩種子語法：**Actor 等級**與**技能等級**。

#### Actor 等級範圍

```
min/max      ; 等級在 min 到 max 之間（含端點）
min/         ; 等級 >= min（開放上限）
/max         ; 等級 <= max（開放下限）
```

範例：
```ini
; 等級 25-50 的 NPC
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50
; 等級 10 以上
Spell = MySpell|NONE|NONE|10/
```

> 玩家等級動態 NPC（leveled NPCs）的等級在 load 時才確定，可能被跳過。

#### 技能等級

格式：`SkillIndex(min/max)`

技能索引（0-17）：

| Index | 技能 | Index | 技能 |
|---|---|---|---|
| 0 | One-Handed | 9 | Sneak |
| 1 | Two-Handed | 10 | Alchemy |
| 2 | Archery | 11 | Speech |
| 3 | Block | 12 | Alteration |
| 4 | Smithing | 13 | Conjuration |
| 5 | Heavy Armor | 14 | Destruction |
| 6 | Light Armor | 15 | Illusion |
| 7 | Pickpocket | 16 | Restoration |
| 8 | Lockpicking | 17 | Enchanting |

範例：
```ini
; Destruction 技能 50-100 的 NPC
Perk = MyPerk|NONE|NONE|14(50/100)

; 一個括號語法：Actor 等級 14 以上，且 Destruction 技能 10 以上
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|14(10)|M/U
```

