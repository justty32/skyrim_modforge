# 對 ModForge 的參考價值 + 與同層工具的分工

← [keyword-item-distributor](keyword-item-distributor.md)

## 三、對 ModForge 的參考價值

### 現況分析（推斷）

KID 本身不是一個「要生成內容」的工具，而是一個**輔助分發層**。ModForge 主路線是生成 ESP 裡有完整 record（含 keyword）的物件，而 KID 的場景是「我不想改原有 ESP，但我想在 runtime 給它們加 keyword」。

| 場景 | ModForge ESP 方式 | KID 方式 |
|------|-----------------|---------|
| 自己建立的物件加 keyword | 直接在 spec 裡設 `keywords[]` | 不必要 |
| 給**別的 mod 的物件**加 keyword | 需要 override record → 有衝突風險 | KID ini 零衝突 |
| 動態批次套用（如：所有名稱含 Iron 的武器） | 需逐一列舉或寫 Papyrus | KID 一行搞定 |
| 修改 vanilla record 的 keyword | 需要 override Skyrim.esm record | KID 不改 ESP |

### ModForge 可生成的部分（推斷）

- ModForge 已能在 spec 裡給自建物件設 `keywords[]`，這覆蓋了**自有物件**的場景。
- **`_KID.ini` 的生成**：若 spec 有「我想給某類物件批次分發 keyword」的需求，ModForge 可以**輸出一份 `_KID.ini` 文字檔**，不需要任何 Mutagen 支援——純文字生成。這是最低成本的整合路線。→ **可生成（純文字輸出，低成本）**（**推斷**）

### 需新支援的部分（推斷）

- ModForge 目前沒有「批次 keyword 分發 spec」的欄位（如 `distributeKeywords[]`）。若要支援，需要在 spec schema 加一個 KID-ini-generator 路徑。→ **需新支援（spec schema + 文字輸出器）**（**推斷**）

### 純參考部分

- **Trait 過濾語法**：`AR(min/max)`, `OneHandSword`, `HEAVY/LIGHT/CLOTHING` 等分類概念可以作為 ModForge spec 裡 `filter` 欄位設計的靈感。

---

## 四、與同層工具的分工

### KID vs SPID

| 面向 | KID | SPID |
|------|-----|------|
| **分發目標** | **物件**（item/armor/weapon/MGEF/book/ammo…） | **NPC**（Actor） |
| **分發的東西** | Keyword（KYWD） | Spell/Perk/Item/Shout/Package/Outfit/Keyword/Faction/DeathItem |
| **filter 軸** | 物件屬性（護甲值、武器型、魔法系…） | NPC 屬性（等級/技能/性別/種族/location） |
| **config 後綴** | `_KID.ini` | `_DISTR.ini` |
| **共同點** | 同一套 filter 運算子（+/-/*）；都是 runtime，不改 ESP |

**分工原則**：
- 「這個 keyword 屬於**物件本身**的分類屬性」→ **KID**
- 「這個 spell/perk/item **要給 NPC 帶上**」→ **SPID**
- 兩者可互補：SPID 可以把 KID 生成的 keyword 的物件分發給 NPC（`Item = 0x...|...|keyword+` 形式的 keyword filter 引用 KID 打好的 keyword）。

### KID 與 ESP-side keyword 的分工

| 場景 | 推薦方式 |
|------|---------|
| ModForge 自己建的物件 | **ESP-side**：在 spec 直接設 `keywords[]`，乾淨且可驗證 |
| 需要改別人 mod 的物件 | **KID**：零衝突，不需要 override record |
| 需要動態批次（按名稱/型別/屬性掃描） | **KID**：一行 `*Iron|Armor|NONE|HEAVY` 搞定 |
| 需要在遊戲邏輯中判斷 keyword 是否存在 | 兩者皆可；ESP-side 更可預測 |

---

**一句話總結**：KID = runtime keyword 批次分發器，以 `_KID.ini` 對物件（非 NPC）做 no-ESP-override keyword 掛載；與 SPID 正交（KID 管物件，SPID 管 NPC）。對 ModForge 最實際的整合是加一條「輸出 `_KID.ini` 文字」的路徑，補足「給其他 mod 物件加 keyword」這個 ESP override 做不到的場景。
