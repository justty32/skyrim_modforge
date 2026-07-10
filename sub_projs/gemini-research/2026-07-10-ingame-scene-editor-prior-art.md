# in-game 場景編輯 → esp 匯出：先行者搜尋（2026-07-10）

> ⚠️ Haiku agent 網路搜尋原始輸出，**連結與宣稱未逐條人工驗證**。確認過的結論才搬進 idea #24 / findings。

**Verdict：不存在**「遊戲內編輯場景 → 匯出 esp」的現成 mod。定位類 mod 都只持久化到存檔、JSON 或 ini，沒有任何 SKSE plugin 在 runtime 寫 esp。

| Mod | 編輯什麼 | 持久化 | AE |
|-----|---------|--------|-----|
| Jaxonz Positioner | 物件位置/旋轉/縮放 | 存檔 only | 棄坑 |
| Cobb Positioner | 同上＋門＋**NAVCUT** | 存檔 only | LE only |
| Decorator Helper | 物件位置/旋轉/縮放 | 存檔 only | 可用（無 SKSE）|
| Object Placement Saver (141360) | 物件位置/旋轉 | **JSON**＋存檔 | 可用 |
| **In-Game Patcher (158681)** | 物件位置/旋轉/縮放/刪除＋衝突 | **BOS/KID .ini**（非 esp）| 維護中 |
| SkyrimIngameEditor | 天氣/光照/渲染 | JSON profile | 未知 |

Navmesh：**Debug Menu 可在遊戲內視覺化 navmesh**（無編輯）；NavMesh Auditor 只稽核 esp。無 in-game navmesh 記錄/匯出工具。

對我們的意義：
1. **niche 是真的空的**——「in-game 記錄 → 離線生 esp」這條路沒人走過；最接近的 In-Game Patcher 架構同構（記錄→外部檔→runtime 套用），只是它出 ini 我們出 esp。
2. **M7a 的 UX 藍本**：Jaxonz / Cobb / Decorator Helper 解過「抓取/移動/旋轉」互動；Cobb 甚至處理過 NAVCUT。Cobb 的 source 公開（DavidJCobb 慣例），值得讀。
3. **navmesh 視覺化有先例**（Debug Menu）——回答了 idea 的開放問題「能否在遊戲內顯示 navmesh」：可以，去讀它怎麼畫的。
