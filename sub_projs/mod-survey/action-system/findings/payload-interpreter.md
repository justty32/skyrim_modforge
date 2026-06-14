# Payload Interpreter（PIE）

← [action-system 中樞](../README.md)

> **Layer 2（行為資料注入）**。dTry/alexsylex 的 SKSE plugin，runtime 解讀**動畫 payload 註釋**。281 mod 依賴。是「動畫 → 設定 graph variable / 觸發遊戲行為」的標準橋樑。

## 是什麼
- 動畫師在 hkx 裡用 [hkanno64](https://www.nexusmods.com/skyrimspecialedition) 加**註釋（annotation）**，PIE 在播放到該時間點時**解讀並執行 payload**。
- 最常見語法（本批次 BFCO 大量用到）：
  - `PIE.@SGVF|<varName>|<float>` — Set Graph Variable Float
  - `PIE.@SGVI|<varName>|<int>` — Set Graph Variable Int
  - （亦有 bool / 觸發事件等變體，詳見 GitHub: D7ry/PayloadInterpreter）
- 搭配 [BDI](behavior-data-injector.md) 注入的變數使用：BDI 把變數加進 graph，動畫用 PIE 註釋在特定 frame 設定它，OAR/condition 再讀它。
- 安裝：MO2 裝好，跑 Nemesis/Pandora patch。對 mod 用戶「啥都不做」，是純 modder resource。

## 在堆疊中的位置
這三支構成「**動畫驅動狀態**」的鐵三角：
1. **BDI** — 宣告/注入變數與事件（config，免 patch）
2. **PIE** — 動畫在指定 frame 把值寫進那些變數（annotation）
3. **OAR** — 依變數值選下一段動畫（condition）

BFCO 的招式派生（`BFCO_iAttackVariants` 等）就是這條鏈的實戰應用。

## 對 ModForge
- **不可生成**（SKSE DLL + 動畫內 annotation 屬 hkanno 管線，非 record 層）。
- 但**註釋字串本身是純文字、格式固定**——若 ModForge 日後接 hkanno 工具鏈（見 AMR 的 [animation-motion-revolution.md](animation-motion-revolution.md)），可程序化生成 `PIE.@SGVF|...` 行。屬動畫管線而非 esp 管線。
- 列為 ModForge 動畫/招式產物的**前置依賴**（凡用 graph-var 派生的招式都需要它）。
