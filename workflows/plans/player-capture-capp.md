# Player capture — `sc capp <label>`（去 PROTEUS 化）

**狀態（2026-07-12 收工）**：**外貌路徑 ✅ 🎮 實機 PASS**（分身臉＝本人，含 `tintLayers` 戰紋——PROTEUS 拿不到的那層；落地句進 [landed/npcs-capture](../feature-dev/landed/npcs-capture.md)）。**仍待實機的兩條**：① **數值**（必須用**練過的角色**驗，白紙角色驗不出差別）；② **`isPlayer` ＋ 玩家 perk**（下面那顆 `As<PlayerCharacter>()` bug（[gotcha](player-capture-capp/gotcha-playercharacter-cast.md)） 已修，commit `eb6ae75`）。現行部署＝ DLL `dd7afd82`（含該修正），co-save SCCP v9。實機步驟與**對帳錨點**見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)「`sc capp` 直接吸玩家」。

| 檔案 | 內容 |
|------|------|
| [landed.md](player-capture-capp/landed.md) | isPlayer 標示（照實輸出，不加 voice fallback）、玩家 base vs runtime 前提修正、落地摘要（2026-07-12） |
| [original-plan.md](player-capture-capp/original-plan.md) | 原始計畫：動機、DLL 側（SCCP → v8）、ModForge 消費側、風險、量級 |
| [gotcha-playercharacter-cast.md](player-capture-capp/gotcha-playercharacter-cast.md) | 踩坑：`As<RE::PlayerCharacter>()` 永遠回傳 nullptr（已修） |
