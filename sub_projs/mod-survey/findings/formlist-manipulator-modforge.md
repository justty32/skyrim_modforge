# 對 ModForge 的參考價值 + 與同層工具的分工

← [formlist-manipulator](formlist-manipulator.md)

## 三、對 ModForge 的參考價值

### 現況分析

ModForge 的 ESP-side `formLists[]`（若已實作）可以**建立並填充 FLST**，在 ESP 本身裡確定地寫入要分發的 form。FLM 的場景是「不想修改或 override 別人的 FLST，但想在 runtime 追加內容」。

| 場景 | ModForge ESP 方式 | FLM 方式 |
|------|----------------|---------|
| 建立新的 FLST 並填入自有 form | **ESP-side `formLists[]`** | 不適用（FLM 操作既有 FLST） |
| 往**別人 mod 的 FLST** 加東西 | 需要 override record → 衝突風險 | FLM ini 零衝突 |
| 條件性加入（某 plugin 存在才加） | 需 Papyrus / MCM 邏輯 | FLM Filter 語法直接支援 |
| Runtime 動態加入（遊戲中按需） | Papyrus `AddForm()` | FLM ModEvent |

### 可生成的部分（推斷）

- **`_FLM.ini` 的生成**：若 spec 有「往某個 vanilla/外部 mod 的 FLST 追加 form」的需求，ModForge 可以輸出一份 `_FLM.ini` 文字檔。純文字生成，無需 Mutagen。→ **可生成（純文字輸出，低成本）**（**推斷**）
- **Spellforge / Missives 等 mod 的相容補丁**：Missives 或 Spellforge 用 FLST 當任務物件池，若 ModForge 生成的 mod 想向這些池加物件，FLM 是最低衝突的路徑。

### 需新支援的部分（推斷）

- ModForge 目前沒有「FLM ini 生成器」的 spec 欄位（如 `flmEntries[]`）。若要支援，需要在 spec schema 加一個 FLM-ini-generator 路徑。→ **需新支援（spec schema + 文字輸出器）**（**推斷**）

### 純參考部分

- **Collection 的 keyword-filter 概念**：與 KID 的 trait 過濾概念相近，可作為 ModForge spec 裡批次選取 form 的設計靈感。
- **`FLM_SetupDone` Mod Event**：若 ModForge 生成的 mod 需要確保 FLM 已完成操作才執行 Papyrus 邏輯，可在 OnInit 監聽此 event。

---

## 四、與同層工具的分工

### FLM vs ESP-side FLST

| 面向 | ESP-side `formLists[]` | FLM `_FLM.ini` |
|------|----------------------|----------------|
| **建立新 FLST** | 是 | 否（只能操作既有 FLST） |
| **操作目標** | 自己 ESP 裡的 FLST | 任何已載入的 FLST（含 vanilla、其他 mod） |
| **衝突風險** | 若 override 別人的 FLST 有衝突 | 零衝突 |
| **條件性加入** | 需 Papyrus/Script | Filter 語法直接支援 |
| **Runtime 動態** | 需 Papyrus `AddForm()` | ModEvent 觸發 |
| **可預測性** | 高（esp 打開即見） | 依賴 SKSE 載入環境 |
| **ModForge 最佳路線** | 自建 FLST + 填自有 form | 往外部 FLST 追加 / 相容補丁替代 |

**分工原則**：
- **自己建的 FLST，填自己建的 form** → ESP-side `formLists[]`，確定且可驗證
- **往別人 mod（vanilla/Skyrim.esm/外部 mod）的 FLST 加 form** → FLM，零衝突

### FLM vs KID 的協作

FLM 完成後發送 `FLM_SetupDone`，KID 在 FLM 之後執行（或同期）；兩者互補：
- KID 負責把 keyword **掛到物件上**（item 層）
- FLM 負責把物件（或 FormList）**加入某個 FLST**（pool 層）
- 典型流程：KID 給武器加 `WeaponTypeSword` keyword → FLM 把帶有此 keyword 的武器集合（via Collection）加進某個 FLST pool

---

**一句話總結**：FLM = runtime FLST 追加器，以 `_FLM.ini` 把 form 加入任意 FormList，零衝突、零 override ESP；補 ModForge ESP-side FLST 的死角（無法無衝突地往外部 mod 的 FLST 追加）。兩者分工清晰：ModForge ESP-side 建自己的 FLST，FLM ini 處理「追加到別人的 FLST」場景。
