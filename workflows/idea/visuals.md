# Ideas — 美術 / 視覺

← [ideas 索引](ideas.md)

## 12. 明亮美術基調 / 光照管線（2026-06-04）

**狀態：主體已落地。** 本頁保留設計依據；唯一明列的剩餘缺口是 weather/IMGS 掛 region。

Skyrim 光照太陰暗，偏好原神/薩爾達那種明亮——vanilla 只白天 worldspace 有，地下城/洞窟一律暗（偏偏玩家大部分時間在裡面）。**核心認知：暗是美術方向不是引擎限制，光照幾乎全是記錄層的事，正是 ModForge 主場**：

- 室外：Weather 內建調色盤（日光/環境/霧/天空 × 黎明~夜晚）+ **IMGS（ImageSpace，HDR 眼適應/bloom/飽和）**——「亮乾淨高飽和」大半是 IMGS。
- 室內：CELL Lighting（+ DALC 六方向環境光）+ **LGTM（地城多用 DefaultDungeon 暗模板）** + 稀疏 LIGH——全是「選擇」（Zelda 神廟也封閉但它亮）。
- 引擎真限制：無 GI（正解：環境光打底 + 少量光源做層次）；每 mesh 4 燈——**Community Shaders + Light Limit Fix 已入基線（§11-C），視為解除**；卡通渲染要 shader 級（CS feature 是可能出路）。

**ModForge 光照管線 — ✅ 室內+室外皆落地（in-game 確認 2026-06-09，見 CLAUDE.md「已落地」/ `SPEC-lighting.md § lighting`）**：① CELL 逐欄光照進 spec ✅；② 自製明亮 LGTM 模板（模板抄+覆寫，含 DALC）✅；③ 自訂 IMGS 掛 **cell ✅** 與 **weather per-ToD ✅**（`weathers[].imageSpaces`）；④ `lgtmdiag`/`imgsdiag`/`weatherdiag` ✅；⑤ **`WeatherSpec.template`** 抄 vanilla 天氣繼承雲/天空 ✅（from-scratch 天氣無雲，室外務必抄 template）。**剩下**：明亮 LGTM/IMGS 抽成具名 preset 庫；weather/IMGS 掛 region。

---

## 13. 通用 NPC 美化：morph 空間轉換規則（2026-06-04）

**狀態：現役 idea。** 建議先用寫實 head asset 驗證轉換管線，再考慮二次元資產與裝備 refit。

**核心：不是「換成哪種美術」，而是一個轉換規則（morph 空間 → morph 空間的函數）**——讀每個 NPC 原版滑條（編碼了個性），按規則轉成另一模型系統的滑條，全 load order（含 mod 新增 NPC）自動套用，且「在新美術下還認得出是她」。二次元只是其中一個資產包，同管線可換寫實高模頭 / COtR 頭。

**為何現有美化做不到**：替換包硬覆蓋每個 NPC 記錄 + 按 FormID 預烘焙 FaceGen → mod 新增 NPC 漏網、記錄改但 FaceGen 沒配對 = 黑臉 bug。病根是「臉是 per-NPC 烘焙」（身體是 race 級替換故無此問題）。

**輸入端**：NPC_ 的 Face Morph（19 float）+ Face Parts 離散預設 + HDPT + tint（CK 手雕的個性部分只在烘焙 nif 頂點、滑條讀不回，轉換只能近似——風格化美術可接受）。**轉換規則本身就是 spec**：兩邊都是 blendshape 係數空間，可做宣告式對照表，每個目標模型寫一份（一次性），之後全 NPC 自動轉——與翻譯支柱同構（讀插件 → 確定性變換 → 輸出 patch）。

**身體側已驗證此模式**（OBody/AutoBody 按規則套 BodySlide 滑條、SKEE 執行期應用），臉側是空白（SynthEBD 只到貼圖/資產分配層級）。**執行落點兩條路**：執行期（patch 換 head parts、morph 由 SKEE/RaceMenu 套，繞開 FaceGen；Proteus 走過，相容性是難點）或離線烘焙（套 blendshape 算頂點寫 nif，屬資產層超出 Mutagen，或 shell out CK `-ExportFaceGenData`）。二次元真實成本不在臉（動漫頭整顆 mesh 反繞開 FaceGen），而在 **vanilla 裝備 refit + 比例動畫適配**——務實順序：先用寫實資產驗管線。
