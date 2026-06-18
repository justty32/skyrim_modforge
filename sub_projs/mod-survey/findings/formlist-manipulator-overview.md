# 這個 mod 做什麼 + 怎麼運作

← [formlist-manipulator](formlist-manipulator.md)

## 一、這個 mod 做什麼 + 怎麼運作

FLM 是一個 SKSE DLL plugin，在**遊戲啟動時**讀取 `_FLM.ini` 設定檔，把指定的 Form record 加入（或移除）目標 FormList（FLST），完全不需要 override ESP。

### 核心問題它解決了什麼

Skyrim 的 FormList（FLST）是一種「容器 record」，存放一組 form ref（可以是任何類型：Spell/Item/Race/NPC/FLST 本身…）。大量系統靠 FLST 運作：
- Missives 的任務物件池
- 採集系統（plant ingredient）
- Boys/Girls Toys（孤兒院玩具池）
- 毛髮顏色池（character creation）
- Atronach Forge 配方

「兩個 mod 都想往同一個 FLST 加東西」傳統上需要一份**相容補丁 ESP**（override 那個 FLST record）。FLM 讓每個 mod 各帶一份 `_FLM.ini`，runtime 依序 add，**零衝突，零補丁**。

### 運作流程

1. 遊戲啟動，SKSE 載入 `FormListManipulator.dll`。
2. FLM 掃描並**按字母順序**讀取：先 `Data/*.ini`（含 `_FLM.ini` 結尾），再 `Data\FLM\*.ini`。
3. 在讀取自身的 ini 之前，先處理**全部 Alias / Filter / Group / Collection 定義**。
4. 執行 FormList 操作，把指定 form 加入目標 FLST。
5. 操作完成後發送 `FLM_SetupDone` Mod Event（供 Papyrus 監聽確認）。

---

