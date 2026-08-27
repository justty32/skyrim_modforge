# Lifelike NPCs — 讓生成的 NPC 感覺活著

關於撰寫會移動、戰鬥、說話並在世界中生活的 NPC，這是經過提煉的「我們現在所知」。
本頁是樞紐：完整的食譜 + 一個關鍵洞見。其餘所有內容都已拆分出去：

- **[gotchas.md](gotchas.md)** — 咬過我們的陷阱，附上修正（依領域分組）
- **[formid-reference.md](formid-reference.md)** — 我們使用的每一個原版 FormID（templates、voices、陣營、CombatStyles、markers、spells、lights、…）
- **[cheatsheets.md](cheatsheets.md)** — 診斷指令、遊戲內 console、Papyrus + CJK 設定
- **[cookbook-index.md](cookbook-index.md)** — 可複製貼上的食譜，依主題拆分（inn patron、commuter、mage、ritual caster、follower、craftable item、…）
- **[../engine-internals.md](../engine-internals.md)** — 那個*為什麼*：override 語意、GRUP 公式、PACK templates、localized-string 地雷
- **[../SPEC-index.md](../spec/SPEC-index.md)** — 完整的逐欄位 spec 參考 · **[../for_agent.md](../for_agent.md)** — agent 工作流（CLI + library）

<a id="tldr--the-complete-npc-recipe"></a>

## TL;DR — 完整的 NPC 食譜

```jsonc
{
  "race":         "Skyrim.esm:0x013746",     // NordRace (or other)
  "class":        "<MF_YourClass>",
  "voiceType":    "Skyrim.esm:0x013AE6",     // MaleNord — hello/idle audio
  "outfit":       "Skyrim.esm:0x09D5DF",     // BlacksmithOutfit01 (any vanilla outfit)
  "level":        25,
  "autoCalcStats": true,                       // class drives H/M/S + skill values

  // CITIZENSHIP — required for cross-cell Travel (engine refuses door teleports without it)
  "crimeFaction": "Skyrim.esm:0x0267EA",       // CrimeFactionWhiterun
  "factions":     [ "Skyrim.esm:0x0267EA",     // (reinforcing)
                    "Skyrim.esm:0x028172" ],   // TownWhiterunFaction
  "unique":       true,                         // engine AI tracking — vanilla cross-cell NPCs all have this

  // COMBAT — both systems must be authored
  "combatStyle":  "<MF_YourCS>",               // HOW he fights (weapon-class preference)
  "spells":       [ "Skyrim.esm:0x0C969A" ],   // WHAT he casts (FlamesRightHand)
  "aggression":   "Aggressive",                 // WHETHER he fights — default Unaggressive = won't even defend
  "confidence":   "Brave",                      // default Cowardly = flees any threat
  "assistance":   "HelpsFriendsAndAllies",
  "energyLevel":  50,

  // BEHAVIOUR — engine evaluates in list order
  "packages":     [ "<MF_TravelPkg>",          // first priority — go somewhere
                    "<MF_SandboxPkg>" ]        // fallback — what to do once arrived
}
```

少了上面任何一行，這個 actor 在遊戲內就會明顯劣化 — 每個遺漏會產生的確切失敗模式，請見 [gotchas.md](gotchas.md)。

## 關鍵洞見 — Skyrim NPC AI 有兩套獨立系統

| System | Decides | Authored via | Default if unset |
|---|---|---|---|
| **CombatStyle** (CSTY) | NPC **如何**（HOW）戰鬥（magic vs melee vs staff vs ranged） | `combatStyle` ref → CSTY 記錄（`equipMult*` fields） | 平直的預設 — 隨便挑 actor 當下手持的武器 |
| **AIData.Aggression + Confidence** | NPC **到底會不會**（WHETHER）戰鬥 | NPC 上的 `aggression` / `confidence` | `Unaggressive + Cowardly` → **逃離任何威脅**，無論 CombatStyle 如何 |

只設 CSTY 會讓你得到「想用魔法，但一看到狼就逃」。**兩套系統都必須撰寫。**（還有第三條軸：`aggression` 掌管*發動*（initiation）— 一個 `Unaggressive` + `Brave` 的 NPC 不會主動開戰，但被攻擊後會站定迎戰，這正是城鎮居民的正確調校。）
