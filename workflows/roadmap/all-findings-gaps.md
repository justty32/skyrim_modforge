# Roadmap — Findings 缺口全集

← [roadmap](README.md)

findings 缺口全集的完整 2026-06-15 人工審閱（逐檔讀 32 份 findings）已凍結 → [archive/findings-audit-2026-06-15.md](archive/findings-audit-2026-06-15.md)。當時列的缺口除下列殘餘外幾乎全已落地（見 git log / [feature-dev/landed](../feature-dev/landed/README.md)）。

## open 殘餘

| 組 | 缺口 | 優先 | 指標 |
|----|------|------|------|
| G #4 | 程序化法術族生成器（school × level × delivery → MGEF+SPEL+tome+FLST）— 🧊 冷凍（2026-06-22，純便利層、不解鎖新 mod 類型） | 🟢 | — |
| H | CSF 技能樹生成器 — ⏸️ 暫緩（已有 MVP scope + spec 草案，這裡只留指標） | 🟡 | [generation.md](generation.md) |
| A partial | SM branch/quest-node 多層巢狀 SMBN、LVLN alias fill 一等模式 | 🟡 | [mod-survey-gaps.md](mod-survey-gaps.md) ⚠️ partial |
| J 尾巴 | `ActorUtil.AddPackageOverride`（臨時 package 覆蓋，需成對清理＝兩 fragment）、`MiscUtil.ScanCellNPCs`（情境偵測，需條件分支語法）— follower expansion 真正需要時再做 | 🟡 | — |
