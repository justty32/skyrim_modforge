# 16. ESL 合併工具（ModForge 外掛指令）（2026-06-15）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

**Idea**：把一堆動作包、服裝包、武器包 ESL 合併成單一 ESL（含資源），釋放 ESL 插槽、簡化 MO2 管理。

**為何需要**：ESL 雖比 ESP 省插槽，但下載一卡車 ESL 仍占上限（4096/mod）；MO2 管理大量小 mod 麻煩。合併後只剩一個 mod，覆蓋資源也在同一地方。

**技術核心**：
- **FormID 重映射**：ESL 用 `0x000xxx`~`0x000FFF`（最多 2048/4096 筆），合併後需重算每個來源 ESL 的 FormID 區段並更新**所有引用**（record 內 link、腳本 property、SEQ）。
- **資源合併**：Meshes/Textures/Sound/Scripts 複製並 dedup（同路徑同內容不重複）。
- **衝突處理**：同 EditorID 的 record 需 override 策略（後者蓋前者 / 保留兩者改名）。
- **實作路線**：Mutagen 已有 FormID remapping 基礎能力（Synthesis 的 link 更新）；ModForge 加 `merge` CLI 指令，輸入 ESL 清單 → 輸出合併 ESL + 合併資源資料夾。

**已知先例**：[zMerge (zEdit)](https://github.com/z-edit/zedit) 做 ESP 合併，原理相通；ESL 特化版尚無成熟工具。

**難點**：① 腳本 property 的 FormID 更新（要解析 pex）；② 有 leveled list override 的 ESL 合併後行為；③ MO2 整合（輸出結構需 MO2 認識）。
