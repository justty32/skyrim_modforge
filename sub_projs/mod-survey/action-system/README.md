# action-system/ — 現代動作 / 動畫系統框架

← [mod-survey](../README.md)｜[mod-survey index](../index.md)

這個資料夾收 **2026 現代動作/動畫系統**那一整套互相疊起來的框架（行為引擎 → 動畫替換 → 動作 mod），是 Sofia/follower 動作擴充與 ModForge 動畫生成功能的共同依據。

## 四層堆疊（由底而上）

1. **Pandora Behaviour Engine+** — behavior graph 的 patch/生成層（2026 取代 Nemesis/FNIS）。→ [pandora.md](pandora.md)
2. **OAR（Open Animation Replacer）** — runtime 條件式動畫替換（DAR 後繼），純 folder+JSON、最高槓桿的 ModForge 整合目標。→ [oar-replacer-guide.md](oar-replacer-guide.md)（實作指南）；原理/四層定位分析在 [animation/integration-layer.md §5](../../../workflows/idea/asset-pipelines/animation/integration-layer.md)
3. **.hkx 動畫資產本體** — 製作管線屬 [animation/havok-blender.md](../../../workflows/idea/asset-pipelines/animation/havok-blender.md) 線（Blender/Havok），不在本夾。
4. **觸發/動作 mod 層** — MCO / movesets / 攻擊框架等（**一整套新動作系統的調查即將進來，放本夾**）。

## 與 roadmap 的關係
- OAR 生成、CSF 生成（[../custom-skills-framework.md](../custom-skills-framework.md)）、ModForge↔Pandora 整合 spike 都已列入 [roadmap](../../../workflows/roadmap.md)。

## 待補
- 一整套新動作系統（MCO/ADXP、movesets、攻擊/閃避框架…）的調查 — 進來後逐 mod 一份，並更新本 README 的第 4 層。
