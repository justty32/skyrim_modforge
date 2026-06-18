# Filter 語法：Traits / Chance / 關係

← [spid](spid.md)

### 4.4 Traits（第 5 欄）

特質字母以 `/` 分隔組合，代表 AND（同時符合）：

| 字母 | 語義 |
|---|---|
| `M` | 男性 NPC |
| `F` | 女性 NPC |
| `U` | Unique NPC（actorbase 標記 unique） |
| `S` | 可召喚（Summonable） |
| `C` | 兒童（IsChild） |
| `L` | Leveled NPC |
| `T` | 玩家隊友（Player Teammate） |

**否定**：在字母前加 `-`，代表「不符合此特質」：
- `-U` = 非 unique NPC
- `-C` = 非兒童

**組合範例**：
```ini
; 所有女性 NPC
Spell = MySpell|NONE|NONE|NONE|F

; 所有男性 unique 且可召喚的 NPC
Perk = MyPerk|NONE|NONE|NONE|M/U/S

; 非 unique 的 NPC
Item = MyItem|NONE|NONE|NONE|-U
```

> Chance（機率）只對非 unique（`-U`）NPC 生效；unique NPC 忽略 chance，永遠分發（或不分發）。

### 4.5 Chance（第 7 欄）

```
0-100    ; 百分比，0=永不，100=必定（預設）
```

- 只對 **非 unique** NPC 有效。
- Unique NPC 忽略 chance 值，一律視為 100%（只要其他 filter 符合）。
- 省略、留空、或寫 `NONE` 等同 `100`。

### 4.6 多個 Filter 之間的關係

- 同一行的所有 filter 欄位是 **AND**（全部都要通過）。
- 同一欄位內的多個條目（逗號分隔）是 **OR**（任一符合即可），但帶 `-` 前綴的條目是排除（NPC 必須不符合那個值）。
- `+` 連接的多個 StringFilter 條目是 **AND**（NPC 必須同時有全部）。

範例說明：
```ini
; 以下分發給：同時符合 ActorTypeNPC 且不是 Nazeem，且在 BanditFaction 或 EnemyFaction 中
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|BanditFaction,EnemyFaction|NONE|NONE|3000
```

---

