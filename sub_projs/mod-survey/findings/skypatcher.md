# Mod Survey — SkyPatcher（Nexus 106659，v6.4.1）

> ModForge 取向逆向調查：這是一個「策略級」runtime patcher，直接衝擊 ModForge 的產物策略（生成 `.esp` vs 生成 SkyPatcher config）。
> 來源：`SkyPatcher - AE-106659-6-4-1-1777328355.zip`（zip 只含 dll + ini + 空 config 資料夾；語法文件來自 GitHub repo `Zzyxz/SkyPatcher` + Nexus 文章）。
> 相關既有筆記：`followers-patch-and-mod-survey`、`common-framework-mods`、SPID（已在 common-framework-mods finding 中）。

---

## 內容拆分

- [做什麼 + 怎麼運作](skypatcher-overview.md) — 執行時序、三種更新模式、不修改存檔可熱移除
- [record 類型與 config 語法](skypatcher-records-and-config.md) — 2-A 支援類型主表 / 2-B ini 格式 / 2-C NPC 欄位完整清單
- [其他 record 類型關鍵欄位](skypatcher-other-records.md) — 2-D 各 record 類型欄位
- [vs SPID + ModForge + 策略](skypatcher-modforge-and-strategy.md) — 差異、可生成/需新支援/純參考、esp vs SkyPatcher config 策略

## 六、參考連結

- Nexus 主頁：https://www.nexusmods.com/skyrimspecialedition/mods/106659
- GitHub 原始碼：https://github.com/Zzyxz/SkyPatcher
- Nexus 官方文章（需登入）：
  - NPC Patcher：https://www.nexusmods.com/skyrimspecialedition/articles/6092
  - SkyPatcher Information：https://www.nexusmods.com/skyrimspecialedition/articles/11194
  - ini 小撇步：https://www.nexusmods.com/skyrimspecialedition/articles/9850
  - 詳細使用指南：https://www.nexusmods.com/skyrimspecialedition/articles/9835
