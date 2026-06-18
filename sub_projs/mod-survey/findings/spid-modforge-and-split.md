# 對 ModForge 的參考價值 + 與 KID/SkyPatcher 分工

← [spid](spid.md)

## 六、對 ModForge 的參考價值

### 可生成的輸出：`_DISTR.ini`

SPID 的 config 是純文字 `.ini`，**無需 ESP**。ModForge 理論上可以輸出 `<ModName>_DISTR.ini` 作為 mod 的一部分。

對 ModForge 最直接的使用場景：

| 場景 | SPID 行格式 | 說明 |
|---|---|---|
| 給特定 NPC 加 faction（dialogue condition 用）| `Faction = MyFaction\|TargetNPCEditorID\|NONE\|NONE\|NONE\|NONE\|NONE` | 最輕量，不需 patch ESP |
| 給一批 NPC 加 keyword | `Keyword = MyKeyword\|ActorTypeNPC` | OAR 或 dialogue condition 用 |
| 給 follower 加 ability（invisible spell）| `Spell = MyAbility\|FollowerEditorID` | 狀態 hook / buff 注入 |
| 給 NPC 分發 perk（戰鬥/技能用）| `Perk = 0xFormID~Plugin.esp\|ActorTypeNPC` | 廣泛分發 |
| 給 NPC 加入一個 outfit | `Outfit = MyOutfit\|TargetRace\|NONE\|NONE\|NONE\|NONE\|100` | 替換外觀 |
| 給死亡 NPC 加額外掉落 | `DeathItem = 0xLVLI~Plugin.esp\|ActorTypeNPC` | 戰利品分配 |

### ⚠️ ModForge 需要新支援的項目（推斷，未查 src/）

- **`_DISTR.ini` 輸出器**：目前 ModForge 產出 `.esp`，若要支援 SPID config，需要新增一個輸出模組，能把 JSON spec 裡的 distribution 設定翻譯成 `_DISTR.ini` 行格式。
- **FormID cross-plugin 解析**：SPID config 的 `0xFormID~Plugin.esp` 格式要求知道目標 form 的 FormID 和所在 plugin，ModForge 的 form 建立流程需要能在 spec 層記錄這個 reference。
- **EditorID 穩定性管理**：SPID 強烈建議用 EditorID 而非 FormID（merge 穩定），ModForge 生成記錄時給每個 form 一個穩定 EditorID 是前提。
- **Outfit / SleepOutfit / Skin 分發**：這三種 type 涉及 per-actor 追蹤（7.2+），若要支援需了解 Outfit 分發的「第一條優先」語義，避免多條 config 衝突。

### 現有能力（不需新支援）

- ModForge 已可建立 FACT / SPEL / PERK / KYWD 等記錄 → 可以在 ESP 裡直接做；SPID 只是讓你**不需要 patch 其他 mod 的 NPC record**。
- 若 ModForge 的目標 NPC 是自己 ESP 內的 NPC（不是 vanilla / 第三方 NPC），直接在 NPC record 裡加 faction/spell 即可，不需要 SPID。
- **SPID 的槓桿點在「跨 mod 無 patch 分發」**——自家 mod 的 NPC 不需要它。

---

## 七、與 KID / SkyPatcher 的分工

### KID（Keyword Item Distributor）

- **目標**：把 keyword 分發到**道具（item）記錄**（武器、盔甲、藥水、書、彈藥、材料、魔法效果等）。
- **不能做**：給 NPC 加 spell / perk / faction。
- **與 SPID 的關係**：SPID 運行在 KID 之後（如果都安裝了），兩者互補——KID 負責標記道具，SPID 負責標記 NPC。
- **使用場景**：給武器加 `WeapTypeSword` keyword、給藥水加自訂分類 keyword、讓 OAR/BFCO 能用 keyword 條件識別裝備。

### SkyPatcher

- **目標**：更廣泛的 runtime record patch——可以修改 LVLN（leveled list）、容器、種族、武器、NPC 屬性等，不限於「分發」。
- **能力**：可增加/修改道具到 leveled list、容器，可以直接修改 NPC 的屬性欄位，不只是 attach/add。
- **與 SPID 的關係**：互補，不互斥。複雜修改（直接 override NPC 欄位）用 SkyPatcher；單純 attach（加 spell/perk/faction/keyword）用 SPID。SPID 更輕量，兼容性更好。
- **使用場景**：修改 leveled list 讓新武器出現在商人、修改 NPC 戰鬥風格、直接 patch 種族記錄。

### 三者分工一覽

| 工具 | 主要目標 | 主要操作 | 典型 ini 後綴 |
|---|---|---|---|
| SPID | NPC actorbase | 加 spell / perk / item / faction / keyword / outfit / package 到 NPC | `_DISTR.ini` |
| KID | 道具記錄（ARMO/WEAP/ALCH 等） | 加 keyword 到道具 | `_KID.ini` |
| SkyPatcher | 廣泛 record（LVLN/CONT/NPC/RACE 等）| 修改記錄欄位、加入 leveled list | 自定義 `.ini` in `SkyPatcher/` |

> **待補**：KID 和 SkyPatcher 的深挖 survey 完成後，補充各自完整語法與更多使用場景對比。

---

