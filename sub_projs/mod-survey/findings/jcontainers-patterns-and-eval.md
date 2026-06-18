# Papyrus 可利用模式 + 對 ModForge 的參考價值

← [jcontainers](jcontainers.md)

## 三、ModForge 生成 Papyrus script 的可利用模式

### Pattern 1：外部 JSON 讀取設定

```papyrus
; 從外部 JSON 讀入 follower 設定表
int config = JValue.readFromFile("Data/SKSE/Plugins/MyMod/config.json")
string voice = JMap.getStr(config, "voiceType", "Female")
float weight = JMap.getFlt(config, "commentaryWeight", 1.0)
```

### Pattern 2：per-Form 複雜狀態（JFormDB）

```papyrus
; 在 NPC Form 上掛複雜結構（關係 + 情緒 + 任務進度）
JFormDB.solveIntSetter(targetNPC, ".mymod.relationship", 2, true)
JFormDB.solveFloatSetter(targetNPC, ".mymod.lastTalkTime", Utility.GetCurrentRealTime(), true)

; 讀取
int rel = JFormDB.solveInt(targetNPC, ".mymod.relationship", 0)
```

### Pattern 3：動態 topic pool（JArray + JDB）

```papyrus
; 建立 commentary pool 並存入 JDB
int pool = JArray.objectWithStrings(new string[3])
JArray.addStr(pool, "看到了嗎？")
JArray.addStr(pool, "有趣的地方。")
JDB.setObj("mymod_commentaryPool", pool)

; 隨機取一句
int pool = JDB.solveObj(".mymod_commentaryPool", 0)
int idx = Utility.RandomInt(0, JArray.count(pool) - 1)
string line = JArray.getStr(pool, idx, "")
```

### Pattern 4：Pool 管理臨時物件

```papyrus
; 批次建立臨時物件時用 pool 防止 GC
int tempMap = JValue.addToPool(JMap.object(), "mymod_tempSession")
int tempArr = JValue.addToPool(JArray.object(), "mymod_tempSession")
; 使用完畢
JValue.cleanPool("mymod_tempSession")
```

### Pattern 5：跨存檔的 Form 索引表（JFormDB + writeToFile）

```papyrus
; 把整個 FormDB 的 storageName 存到磁碟
JDB.writeToFile("Data/SKSE/Plugins/MyMod/persistent.json")
; 遊戲啟動時讀回
int loaded = JValue.readFromFile("Data/SKSE/Plugins/MyMod/persistent.json")
JDB.setObj("mymod", loaded)
```

---

## 四、對 ModForge 的參考價值

| 功能 | 評估 | 說明 |
|---|---|---|
| `JValue.readFromFile / writeToFile` | **可直接生成** | 外部 JSON 讀寫 pattern 固定，適合 config 系統 |
| `JMap.object / getInt / setInt / nextKey` | **可直接生成** | string-keyed KV + 迭代 pattern 固定 |
| `JArray.object / addStr / getStr / count` | **可直接生成** | 動態列表 pattern 固定 |
| `JFormDB.solveIntSetter / solveInt` | **可直接生成**（推斷） | per-Form 嵌套狀態是最常用 pattern |
| `JDB.setObj / solveInt` | **可直接生成**（推斷） | 全域設定/狀態，跨 mod 共享 |
| `JValue.retain / release / addToPool / cleanPool` | **需警告生成** | 生命週期管理是最容易出錯的部分；生成時需確保成對 |
| `JAtomic.fetchAddInt` | **純參考** | 多腳本並發計數器；Papyrus 本身單執行緒，此功能偏工具層 |
| `JLua.evalLua*` | **純參考** | 實驗性，不建議生產環境使用 |
| `JFormMap` 作為臨時 Form→value map | **可直接生成**（推斷） | 用於 scene 內批次 Form 處理 |
| `JArray.unique / sort` | **可直接生成** | 去重 set 操作 pattern 固定 |
| `JString.wrap` | **純參考** | 文字排版；Papyrus 顯示系統少用 |

### 與 PapyrusUtil 的選擇建議

| 場景 | 建議 |
|---|---|
| 簡單 per-NPC int/float/string 值 | **PapyrusUtil.StorageUtil**（更輕量，自動隨 save 管理） |
| 跨存檔設定 / 外部可編輯 config | **PapyrusUtil.JsonUtil**（路徑 API 夠用） |
| Actor package 臨時 override | **PapyrusUtil.ActorUtil**（專用 API） |
| Cell 掃描 NPC/物件 | **PapyrusUtil.MiscUtil**（唯一選擇） |
| 複雜嵌套資料（如 follower 多欄位記憶） | **JContainers.JFormDB**（path 語法比多個 StorageUtil key 乾淨） |
| 動態 topic pool / 大型陣列 | **JContainers.JArray**（無 128 上限） |
| Form 作為 map key（如 Form→狀態表） | **JContainers.JFormMap / JFormDB** |
| 跨 mod 共享結構化資料 | **JContainers.JDB**（全域，有命名空間） |
| 外部 JSON schema 雙向讀寫 | **JContainers.JValue.readFromFile/writeToFile**（比 JsonUtil 更完整支援 nested JSON） |

**總結**：PapyrusUtil 是「簡單 + 自動管理」，JContainers 是「強大 + 需手動管理生命週期」。選 JContainers 的唯一理由是需要 nested 結構、超大陣列、Form-as-key，或是要讀寫 JSON 對結構有嚴格要求。

> ⚠️ 以上「可生成 / 純參考」是本 survey 推斷，**未查 ModForge src/**，可能有誤判。確認需另做 code pass。
