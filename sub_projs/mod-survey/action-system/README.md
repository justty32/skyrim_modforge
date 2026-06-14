# action-system/ — 現代動作 / 動畫系統框架

← [mod-survey](../README.md)｜[mod-survey index](../index.md)

這個資料夾收 **2026 現代動作/動畫/戰鬥系統**那一整套互相疊起來的框架，是 Sofia/follower 動作擴充與 ModForge 動畫生成功能的共同依據。原始 mod 頁文字存於 [`raws/`](raws/)。

## 五層堆疊（由底而上）

| 層 | 角色 | 代表 mod | 可生成性（ModForge） |
| --- | --- | --- | --- |
| **0 骨架/rig** | 提供擴充骨骼節點（武器掛點/物理/可調骨） | [XPMSSE](findings/xpmsse.md) | 純前置，不生成 |
| **1 行為引擎/runtime** | patch 或 runtime 修正 behavior graph | [Pandora](pandora.md)（patcher）、[Universal Behavior Runtime](findings/universal-behavior-runtime.md)（A-Pose Fix + Auto Skeleton Patch，runtime 容錯/轉換） | Pandora=shell-out；UBR=前置 |
| **2 行為資料注入** | 免 behavior patch 加變數/事件/位移 | [BDI](findings/behavior-data-injector.md)（graph var/event）、[Payload Interpreter](findings/payload-interpreter.md)（annotation→設值）、[AMR](findings/animation-motion-revolution.md)（annotation→位移） | **BDI config 可生成**；annotation 屬動畫管線 |
| **3 動畫選擇** | 依條件在 runtime 換動畫 | [OAR](oar-replacer-guide.md)、[DMK](findings/directional-movement-keys.md)（方向→graph var 供 OAR 條件） | **OAR 結構最高槓桿、可生成** |
| **4 招式框架** | 把上面拼成連擊/招式/NPC AI | [BFCO](findings/bfco.md)（攻擊框架）、[SCAR](findings/scar.md)（NPC 連段 AI） | OAR 變體 config 可生成；.hkx/AI 不可 |
| — .hkx 資產本體 | 動畫製作管線 | — | 屬 [animation/havok-blender](../../../workflows/idea/asset-pipelines/animation/havok-blender.md) 線，不在本夾 |

> OAR 的原理/四層定位分析在 [animation/integration-layer.md §5](../../../workflows/idea/asset-pipelines/animation/integration-layer.md)；本頁 OAR 列在第 3 層的實作指南。

## 跨層鐵三角：動畫如何驅動狀態（ModForge 的甜蜜點）

現代招式系統不改 esp、不改 behavior binary，靠這條鏈運作——**除 .hkx 本體外全是固定格式文字**：

1. **BDI** 注入 graph variable / event（config，免 patch）
2. **動畫 annotation** 在指定 frame 寫值：
   - `PIE.@SGVI|<var>|<int>` / `PIE.@SGVF|<var>|<float>`（[Payload Interpreter](findings/payload-interpreter.md)）
   - `[time] animmotion x y z` / `[time] animrotation deg`（[AMR](findings/animation-motion-revolution.md) 位移）
3. **OAR** 用 `CompareValues` 比對該 graph variable 選下一段動畫
   - 已知可用變數：`BFCO_iAttackVariants`（[BFCO](findings/bfco.md)）、`DirecionalCycleMoveset`/`CameraMovementCMF` 八向（[DMK](findings/directional-movement-keys.md)）

ModForge 能確定生成的：**第 1 步的 BDI config、第 3 步的 OAR condition JSON 與資料夾結構**。第 2 步的 annotation 屬 hkanno 動畫管線（若日後接該工具鏈，這些固定格式字串也可程序化生成）。

## 對 ModForge 的具體機會（已彙整進 roadmap，待 code 驗證）
- **OAR 生成器**（最高槓桿）— 純 folder+JSON+CTDA-like 條件；八向/攻擊變體動畫包可模板量產。
- **BDI config 生成器** — `{project, variables, events}` → config；給 NPC 加自訂狀態變數免 behavior patch。
- **Pandora shell-out** — 生 behavior 基底（headless/Linux 待 Manjaro 實機驗，見 [pandora.md](pandora.md)）。
- 詳見 [roadmap](../../../workflows/roadmap.md) 的動作系統相關項。

## 與其他層的邊界
- **不可生成**（純 SKSE DLL，僅列前置）：XPMSSE、Pandora 引擎本體、UBR、BDI/PIE/AMR 的 DLL、SCAR、DMK。
- **屬動畫資產管線**（非 esp record 層）：.hkx、hkanno 註釋——見 [havok-blender](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)。
