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
- **實檔驗證**：出貨 = `PayloadInterpreter.dll` + 一個 `Nemesis_Engine/mod/evfmgo/` behavior patch 包（注入 `evfmgo` event 到全套 behavior project：1hm/bow/magic/shout/horse/stagger…）。即「DLL + Nemesis-format patch」，故需跑 Nemesis/Pandora。無 esp。

## 在堆疊中的位置
這三支構成「**動畫驅動狀態**」的鐵三角：
1. **BDI** — 宣告/注入變數與事件（config，免 patch）
2. **PIE** — 動畫在指定 frame 把值寫進那些變數（annotation）
3. **OAR** — 依變數值選下一段動畫（condition）

BFCO 的招式派生（`BFCO_iAttackVariants` 等）就是這條鏈的實戰應用。

## 巨集 config（`SKSE/PayloadInterpreter/Config/*.ini`，**實檔驗證、可生成**）
- 除了 hkx 內的 annotation，PIE 還讀一層**純文字 `.ini` 巨集表**，把命名巨集映射到 payload 指令。實例（Stormcloaks DAR-MCO-SCAR 的 `VikingAxe.ini`）：
  ```ini
  [Intensify]
  $enableIframe = @SETGHOST|1
  $disableIframe = @SETGHOST|0
  ```
  - `[Section]` = 範圍（通常對應 behavior/動畫 project）；`$name = @CMD|args` 定義巨集。動畫 annotation 之後可引用 `$enableIframe`（無敵幀 = 變 ghost 去碰撞）而非寫死 `@SETGHOST|1`。
- 這是 **ModForge 可確定生成的純文字 config**——修正了「PIE 全在 hkx 內」的早期判斷：巨集表這層是可生成的，只有 hkx 內 annotation 屬動畫管線。

## 對 ModForge
- **DLL 不可生成**；hkx 內 annotation 屬 hkanno 管線。
- **但 `Config/*.ini` 巨集表可生成**（見上，格式已驗證）——若做 dodge/iframe/招式效果 config，這是 record 層外的可生成產物。
- 註釋字串本身格式固定（`PIE.@SGVF|var|val`）——若 ModForge 日後接 hkanno 工具鏈（見 [AMR](animation-motion-revolution.md)），亦可程序化生成。
- 列為 ModForge 動畫/招式產物的**前置依賴**（凡用 graph-var 派生的招式都需要它）。
