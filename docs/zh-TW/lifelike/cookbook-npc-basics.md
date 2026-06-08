<!-- 基本 NPC 模式 -->
# 食譜手冊 — 基本 NPC 模式

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「酒館常客」（僅沙盒模式）

```jsonc
{ "packages": [
    { "editorId": "MF_InnSandbox", "template": "Skyrim.esm:0x01C254",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter", "WorldInteractions" ],
      "sandbox": { "radius": 512, "allowEating": true, "allowSleeping": false,
                    "allowConversation": true, "allowIdleMarkers": true,
                    "allowSitting": true, "allowWandering": true,
                    "allowSpecialFurniture": true, "energy": 50.0 } }
  ],
  "npcs": [
    { "editorId": "MF_Patron", "race": "Skyrim.esm:0x013746", "class": "<...>",
      "voiceType": "Skyrim.esm:0x013AE6", "level": 5, "autoCalcStats": true,
      "packages": [ "MF_InnSandbox" ] }
  ],
  "placements": [
    { "base": "MF_Patron", "cell": "Skyrim.esm:0x01605E",   // Bannered Mare
      "position": { "x": 0, "y": 0, "z": 0 } }
  ] }
```

## 「跨城通勤者」（Travel + 沙盒 + 市民身份）

在上方酒館常客的基礎上新增：
```jsonc
{ "packages": [
    { "editorId": "MF_GoOut", "template": "Skyrim.esm:0x016FAA",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
      "travel": { "place": "Skyrim.esm:0x109826", "radius": 256 } },  // stables
    { "editorId": "MF_InnSandbox", ... }                                 // as above
  ],
  "npcs": [
    { "editorId": "MF_Commuter", ...,
      "crimeFaction": "Skyrim.esm:0x0267EA",
      "factions":     [ "Skyrim.esm:0x0267EA", "Skyrim.esm:0x028172" ],
      "unique":        true,
      "packages": [ "MF_GoOut", "MF_InnSandbox" ] }  // order matters: Travel first
  ] }
```

## 「具備戰鬥能力的法師」

```jsonc
{ "combatStyles": [
    { "editorId": "MF_MageCS",
      "offensiveMult": 0.77, "defensiveMult": 0.3, "groupOffensiveMult": 0.74,
      "equipMultMelee": 0.51, "equipMultMagic": 8.1, "equipMultRanged": 0.55,
      "equipMultShout": 0.21, "equipMultUnarmed": 0.98, "equipMultStaff": 2.15,
      "avoidThreatChance": 0.2, "flags": [ "Dueling" ] }
  ],
  "npcs": [
    { "editorId": "MF_Mage", ..., "level": 25, "autoCalcStats": true,
      "combatStyle": "MF_MageCS",
      "spells":     [ "Skyrim.esm:0x0C969A" ],   // Flames
      "aggression": "Aggressive",                 // CRITICAL — without this he flees
      "confidence": "Brave",                      // CRITICAL — without this he flees
      "assistance": "HelpsFriendsAndAllies", "energyLevel": 50 }
  ] }
```

職業應以魔力為主，並側重破壞系魔法的技能權重。

## 「友善的自我防衛者」（只在受到攻擊時才還手的市鎮居民）

此設定刻意與戰鬥法師形成對比：**不**使用 `Aggressive`（這樣做有將玩家視為敵對的風險）。Aggression 決定的是*主動發動攻擊*的行為；`Brave` 則決定受到攻擊後是逃跑還是迎戰。

```jsonc
{ "npcs": [
    { "editorId": "MF_Townsperson", ...,
      "combatStyle": "<MF_BalancedCS>",
      "aggression": "Unaggressive",   // never starts a fight
      "confidence": "Brave",          // but stands and fights once attacked
      "assistance": "HelpsFriendsAndAllies" }
  ] }
```

## 「儀式施法者」（UseMagic — 非戰鬥排程施法）

```jsonc
{ "packages": [
    { "editorId": "MF_Ritual", "template": "Skyrim.esm:0x0504F5",
      "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
      // Both knobs needed for CONTINUOUS casting (see gotchas) — without them the
      // package completes after numToCastMax casts and the NPC goes idle.
      "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 1440, "dayOfWeek": "Any" },
      "useMagic": {
        "spell":         "Skyrim.esm:0x043324",   // SPEL FormLink — NOT a category enum
        "radius":        256,
        "target":        "",                      // optional placed-ref; omit ⇒ PackageTargetSelf
        "castTimeMin":   1.5, "castTimeMax":   2.5,
        "cooldownTimeMin": 8.0, "cooldownTimeMax": 12.0,
        "numToCastMin":  1, "numToCastMax":  1000,
        "dualCast":      false } }
  ],
  "npcs": [
    { "editorId": "MF_Priest", ..., "level": 15, "autoCalcStats": true,
      "spells":   [ "Skyrim.esm:0x043324" ],   // Candlelight (self-cast, visible orb)
      "aggression": "Aggressive", "confidence": "Brave",
      "packages": [ "MF_Ritual" ] }
  ] }
```

「Spell」欄位是指向特定 SPEL 記錄的 `PackageTargetObjectID` FormLink，**不是**類別列舉。目標欄位預設為 `PackageTargetSelf`（適用於 Candlelight/Healing/Ward 等自身施放的法術）；若需對特定目標施法，請將 `target` 設為已放置的參照物件。戰鬥狀態會搶占 UseMagic 的執行，除非加入 `flags: [ "IgnoreCombat" ]`。

## 「跟隨玩家的同伴」（Follow — 僅限移動層）

一個以玩家為目標的 Follow 套件搭配市民身份食譜，可讓生成的 NPC 跟隨玩家**並在快速旅行後持續存在**（引擎會對執行「跟隨玩家」套件的角色一同進行快速旅行）。*移動層*本身不需要管理用的任務——僱用/解僱對話與跟隨派系（見 [hireable-follower gotcha](gotchas.md)）只是讓玩家能夠自行切換開關的必要條件。

```jsonc
{ "packages": [
    { "editorId": "MF_FollowPlayer", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "", "minRadius": 128, "maxRadius": 256, "accompany": true } }
  ],                                  // target "" ⇒ defaults to the player
  "npcs": [ { "editorId": "MF_Companion", ..., "packages": [ "MF_FollowPlayer" ] } ] }
```
