# Encounter Mods 調查

> 調查日期：2026-06-15  
> 目的：為 ModForge 未來的 random encounter 生成功能蒐集技術機制依據  
> 注意：**兩個 mod 各自都有更詳細的獨立調查**：[extended-encounters.md](extended-encounters.md)、[immersive-world-encounters.md](immersive-world-encounters.md)。本檔是從 `~/skyrim_mods/hdd/` 直接對 zip/7z 做原始分析（`unzip -p`, `7z e`, `strings`, psc source）的整合對比，補充獨立檔未記錄的細節。

---

## 內容拆分

- [Immersive World Encounters SE](encounter-mods-iwe.md) — 機制、spawn 邏輯、可生成部分、設計模式
- [Extended Encounters](encounter-mods-extended.md) — 機制、spawn 邏輯、可生成部分、設計模式
- [對比 + 擴充建議](encounter-mods-comparison.md) — 機制對比、設計哲學差異、擴充優先順序、可複用模式
