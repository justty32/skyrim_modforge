# 這個 mod 做什麼 + 怎麼運作

← [skypatcher](skypatcher.md)

## 一、這個 mod 做什麼 + 怎麼運作

SkyPatcher 是一個 **SKSE 外掛（CommonLibSSE-NG）**，讓作者／使用者**不需要 esp 插件就能批次修改既有 record 的欄位**。它讀取放在 `Data/SKSE/Plugins/SkyPatcher/<recordType>/` 資料夾中的 `.ini` 設定檔，在遊戲啟動時套用修改。

### 執行時序

從 `main.cpp` 分析，有**兩個掛鉤點**：

1. **`kDataLoaded`（資料載入後，遊戲進主選單前）**：讀取所有 ini 檔案，並對靜態資料型別（武器、防具、法術、種族、formlist、leveled list、容器等）立刻套用修改。這是「**load-time patch**」——遊戲啟動後一次性，儲存於記憶體中的 record 物件。

2. **`kPostLoadGame`（讀取存檔後）**：對所有當前已讀入的 Actor（NPC）重新套用視覺樣式、戰鬥風格、技能等。同時如果 `iUpdateNPC=1` 啟用，還會掛 `Load3D` hook，讓任何 NPC 3D 模型被載入時都自動套用最新的 NPC 修改。

### 三種更新模式（NPC 專屬）

| 設定 | 時機 | 說明 |
|------|------|------|
| `iUpdateNPC=1` | 每次 NPC Load3D | 動態：任何 NPC 進入渲染範圍都重新套用修改 |
| `iRefreshNPCStats=1` | 讀檔時 | 讀取存檔後重整所有 NPC 數值，免去重新建構 NPC |
| `iUpdateRefs=1` | 讀檔時 | 實驗性：對 REFR（放置物件）套用修改（仍開發中） |

### 不修改存檔、可熱移除

移除 ini 或 SkyPatcher 本身後**不留殘存資料在存檔中**——只影響記憶體中的資料，下次啟動重新計算。這與 esp override 不同（esp 的修改被序列化進 REFR/Actor 存檔資料）。

---

