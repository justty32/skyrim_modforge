# 這個工具做什麼 + 工作原理

← [base-object-swapper](base-object-swapper.md)

## 一、這個工具做什麼 + 工作原理

Base Object Swapper（BOS）是一個 SKSE plugin（`po3_BaseObjectSwapper.dll`），讓 mod 作者**在 runtime 把某個 base form 替換成另一個**，完全不需要 ESP patch 也不需要修改 cell reference。

**工作原理**：

1. 遊戲啟動時，BOS 掃描 `Data\` 目錄下所有檔名 suffix 為 `_SWAP` 的 `.ini` 檔。
2. 讀取各 ini 的 section/key，建立「base formID → swap formID + 屬性覆蓋 + 條件」的對應表。
3. 遊戲運行中，當引擎要實例化某個 reference 時，BOS 的 hook 攔截並在記憶體中把 base form 替換成指定的目標 form。
4. 替換是 runtime only，不修改任何存檔或 ESP，可以被隨時移除（移除後恢復原貌）。

**適用 record 類型**：任何繼承自 `TESBoundObject` 的 form type，包含 STAT（Static）、FURN（Furniture）、CONT（Container）、ACTI（Activator）、MSTT（MovableStatic）、LIGH（Light）、TREE（Tree）等。**不適用**：NPC / Actor（那是 NPCSwap 或 SPID 的工作）。

