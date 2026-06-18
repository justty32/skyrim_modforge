# Mod Survey — Missives (v2.03 SSE)

對象：`Missives.esp`（Nexus 17576）。本文從 ModForge（JSON spec → `.esp` 生成器）角度拆解這顆高評價的 **radiant quest / 公告板** mod 怎麼運作、哪些機制可生成 / 需新支援 / 純參考。

> 記錄方法：只用 ModForge lazy CLI verb（streamed/overlay-safe）跑這顆 esp，沒有整顆載入 Skyrim.esm。

---

## 內容拆分

- [做什麼 + record 與模式](missives-mechanism-records.md) — 公告板→missive→radiant→回報鏈、quest 模板笛卡兒積、alias fill、LVLI 雙用、BOOK/Activator
- [對 ModForge 的參考價值](missives-modforge.md) — 可生成部分、新缺口（附 evidence）、可生成/需新支援/純參考標記、相關連結
