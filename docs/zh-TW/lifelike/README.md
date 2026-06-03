# 有生命力的 NPC — 讓生成的 NPC 更真實

「我們現在所知的一切」的精華，用於製作能移動、戰鬥、說話、活在世界中的 NPC。
本頁是中心：完整食譜 + 一個關鍵洞察。其他內容分拆至：

- **[gotchas.md](gotchas.md)** — 我們踩過的陷阱，以及修復方法（依領域分組）
- **[formid-reference.md](formid-reference.md)** — 我們使用的每個原版 FormID（模板、語音、派系、CombatStyles、標記物、法術、光源…）
- **[cheatsheets.md](cheatsheets.md)** — 診斷指令、遊戲內控制台、Papyrus + CJK 設定
- **[cookbook.md](cookbook.md)** — 複製貼上食譜（旅館常客、通勤者、法師、儀式施法者、跟隨者、可合成物品…）
- **[../engine-internals.md](../engine-internals.md)** — *為什麼*：覆寫語意、GRUP 公式、PACK 模板、本地化字串地雷
- **[../SPEC.md](../SPEC.md)** — 完整欄位規格參考 · **[../for_agent.md](../for_agent.md)** — 代理工作流程（CLI + 函式庫）

## TL;DR — 完整 NPC 食譜

```jsonc
{
  "race":         "Skyrim.esm:0x013746",     // NordRace（或其他）
  "class":        "<MF_YourClass>",
  "voiceType":    "Skyrim.esm:0x013AE6",     // MaleNord — hello/idle 音訊
  "outfit":       "Skyrim.esm:0x09D5DF",     // BlacksmithOutfit01（任何原版裝束）
  "level":        25,
  "autoCalcStats": true,                       // 職業驅動 H/M/S + 技能值

  // 市民身份 — 跨空間 Travel 所需（引擎在沒有此設定時拒絕門傳送）
  "crimeFaction": "Skyrim.esm:0x0267EA",       // CrimeFactionWhiterun
  "factions":     [ "Skyrim.esm:0x0267EA",     // （強化）
                    "Skyrim.esm:0x028172" ],   // TownWhiterunFaction
  "unique":       true,                         // 引擎 AI 追蹤 — 原版跨空間 NPC 全部有此設定

  // 戰鬥 — 兩個系統都必須製作
  "combatStyle":  "<MF_YourCS>",               // 他如何戰鬥（武器類偏好）
  "spells":       [ "Skyrim.esm:0x0C969A" ],   // 他施放什麼（FlamesRightHand）
  "aggression":   "Aggressive",                 // 他是否戰鬥 — 預設 Unaggressive = 連自衛都不會
  "confidence":   "Brave",                      // 預設 Cowardly = 面對任何威脅都會逃跑
  "assistance":   "HelpsFriendsAndAllies",
  "energyLevel":  50,

  // 行為 — 引擎依列表順序評估
  "packages":     [ "<MF_TravelPkg>",          // 最高優先 — 前往某處
                    "<MF_SandboxPkg>" ]        // 備用 — 抵達後做什麼
}
```

省略上面任何一行，角色在遊戲中都會有明顯退化 — 每個省略的具體失敗模式詳見 [gotchas.md](gotchas.md)。

## 關鍵洞察 — Skyrim NPC AI 有兩個獨立系統

| 系統 | 決定 | 製作方式 | 未設定時的預設 |
|---|---|---|---|
| **CombatStyle**（CSTY） | NPC **如何**戰鬥（魔法 vs. 近戰 vs. 法杖 vs. 遠程） | `combatStyle` ref → CSTY 記錄（`equipMult*` 欄位） | 平面預設 — 拾取角色恰好持有的任何武器 |
| **AIData.Aggression + Confidence** | NPC **是否**戰鬥 | NPC 上的 `aggression` / `confidence` | `Unaggressive + Cowardly` → **面對任何威脅都逃跑**，無論 CombatStyle 如何 |

只設定 CSTY 的結果是「想用魔法但看到狼就逃跑」。**兩個系統都必須製作。**（還有第三個軸：`aggression` 控制*主動發起* — `Unaggressive` + `Brave` 的 NPC 不會主動挑釁，但一旦被攻擊就會站立反擊，這是城鎮居民的正確設定。）
