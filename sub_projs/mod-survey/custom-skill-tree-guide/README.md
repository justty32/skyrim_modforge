# 自己動手做一棵 Skyrim 自訂技能樹（Custom Skills Framework 實作指南）

← [mod-survey](../README.md)｜[mod-survey index](../index.md)｜姊妹資料夾：[custom-skills-framework](../custom-skills-framework/README.md)（CSF 技術調查）

> 這是一份**動手教學**：跟著走完，你會得到一棵能在遊戲裡「按 ESC → Skills」就看得到、可以練等、可以花點數點 perk 的自訂技能樹。
> 技術原理（CSF 架構、兩代格式斷層、欄位語意的逐項拆解）請看姊妹文件 [`custom-skills-framework/README.md`](../custom-skills-framework/README.md)；本文**不重複**那些深水區內容，只在需要時連過去。
> 慣例：散文用繁體中文，所有 JSON key / record type / API 名稱 / FormId 保留 English。
> 範例全部改編自本機解壓的 **Constellations - Additional Player Skills**（Nexus 117352），那是 CSF 作者 Exit-9B 親自掛保證的現代 JSON 範本。

---

## 內容拆分

- [總覽 + 前置 + 規劃](overview-planning.md) — 需要哪些拼圖、前置需求、規劃技能樹
- [Step 2：esp 記錄](esp-records.md) — 在 esp 裡建記錄
- [Step 3–4：X.json + 選單](json-and-menu.md) — 逐欄寫 `<X>.json` + 掛進選單
- [Step 5：XP / 升級 / 訓練](xp-leveling.md) — XP/升級/訓練接線
- [Step 6–7：在地化 / 測試 / 生成 / Checklist](l10n-test-gen.md) — 在地化、煙霧測試、用 ModForge 生成、常見地雷
