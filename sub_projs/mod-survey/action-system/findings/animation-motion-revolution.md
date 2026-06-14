# Animation Motion Revolution（AMR）

← [action-system 中樞](../README.md)

> **Layer 2（行為資料注入／位移）**。alexsylex 的 SKSE plugin，2.1M unique DL。解決「動畫動作與角色實際位移不同步」的引擎缺陷——讓每段動畫**逐一**定義真實位移。MCO/現代招式系統的位移地基。

## 問題與做法
- vanilla：只有 power attack、踉蹌、少數 clutter 互動用 motion data；一般動畫的位移是一堆 preset 值，動畫只是「裝飾」→ 砍空氣、滑步。
- AMR：在動畫裡加**位移/旋轉註釋**，plugin 解讀後注入引擎驅動角色移動 → 動作與位移完美同步（boss 戰、武器/生物專屬移動）。
- 註釋格式（hkanno64 加）：
  - 位移：`[time] animmotion [x] [y] [z]`（值/格式同 Bethesda 的 `animationdatasinglefile.txt` adsf 條目）
  - 旋轉：`[time] animrotation [degrees]`（如 1.5 秒轉 360°：`0.5 animrotation 90` … `1.5 animrotation 360`）
  - 兩者可混用。
- **生效條件**：動畫對應的 behavior 要設 `bAllowRotation` 或 `bAnimationDriven`（power attack vanilla 即有；用 Skyrim Behavior Tool 編）。

## 與 MCO/SCAR 的關係
- **MCO（Modern Combat Overhaul）圍繞 AMR 打造**，把普通攻擊（非 power attack）也變成 motion-data 驅動 → 解鎖 AMR 全部潛力。
- 與 **SCAR**（NPC combo AI）互補——AMR 管位移精準、SCAR 管 NPC 智能出招。
- 與 DAR 是天作之合（雖非硬前置）。

## 對 ModForge
- **不可生成**（SKSE DLL）；`animmotion`/`animrotation` 註釋屬 **hkanno 動畫管線**，非 esp record 層。
- 但與 [Payload Interpreter](payload-interpreter.md) 同理：**註釋是固定格式純文字**。若 ModForge 接 hkanno 工具鏈做動畫管線，這些位移/旋轉行可程序化生成——adsf↔hkanno 的數值對映在本頁上方已記錄，是確定性轉換。
- 列為位移驅動招式的**前置依賴**。
- 原始碼：https://github.com/alexsylex/AnimationMotionRevolution
