# 自己動手做一個 Open Animation Replacer 動畫替換（OAR 實作指南）

> 這是一份**動手教學**：跟著走完，你會得到一個能在遊戲裡「在指定條件下、給指定角色、替換掉某個 vanilla 動畫」的 OAR 替換包——**不需要 `.esp`**，整個交付物就是資料夾 + JSON + 你的 `.hkx`。
> 技術原理（OAR 在四層動畫堆疊裡的定位、為何是最高槓桿整合點）請看姊妹文件 [`integration-layer.md`](../../../workflows/idea/asset-pipelines/animation/integration-layer.md) §5；動畫 `.hkx` 本體怎麼從 Blender/mocap 做出來（Havok 牆、retarget、win32↔amd64 轉換）見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)。本文**不重複**那兩塊，只在需要時連過去。
> 慣例：散文用繁體中文，所有 JSON key / condition 名 / 路徑 / EditorID 保留 English。
> 權威來源：OAR 官方 Nexus 頁（#92109，v3.1.5 規格）。

---

## 內容拆分

- [總覽 + 前置 + 規劃 + 資料夾](oar-replacer-guide-overview-planning-folders.md) — 需要哪些拼圖、前置需求、規劃替換、資料夾結構
- [Step 3：config.json](oar-replacer-guide-config-json.md) — 兩層逐欄
- [Step 4：條件系統](oar-replacer-guide-conditions.md)
- [Step 5–7：variants / 進階 / 測試](oar-replacer-guide-variants-advanced-test.md) — 隨機/序列變體、submod+functions、遊戲內編輯器測試
- [用 ModForge 生成 + Checklist](oar-replacer-guide-modforge-checklist.md) — 生成路徑 + 常見地雷
