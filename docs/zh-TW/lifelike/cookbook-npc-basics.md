<!-- NPC basic patterns -->
# 食譜手冊 — NPC 基礎

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## 「旅店常客」（僅 Sandbox）

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

## 「跨城通勤者」（Travel + Sandbox + 公民身分）

加到上面的旅店常客之上：
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

Class 應以 magicka 為主，並帶有偏好 Destruction 的技能權重。

## 「友善的自衛者」（只在被攻擊時才戰鬥的鎮民）

刻意與戰鬥法師形成對比：**不要**使用 `Aggressive`（它有把玩家當成敵對的風險）。Aggression 掌管的是*發動*；`Brave` 掌管的是被攻擊後逃跑還是堅守。

```jsonc
{ "npcs": [
    { "editorId": "MF_Townsperson", ...,
      "combatStyle": "<MF_BalancedCS>",
      "aggression": "Unaggressive",   // never starts a fight
      "confidence": "Brave",          // but stands and fights once attacked
      "assistance": "HelpsFriendsAndAllies" }
  ] }
```

## 「儀式施法者」（UseMagic — 非戰鬥的排程施法）

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

「Spell」槽是一個指向特定 SPEL 記錄的 `PackageTargetObjectID` FormLink——而不是一個 category enum。target 槽預設為 `PackageTargetSelf`（對 Candlelight/Healing/Ward 這類自我施放的法術是正確的）；要對 X 施法就把 `target` 設成一個 placed-ref。除非你加上 `flags: [ "IgnoreCombat" ]`，否則戰鬥會搶占 UseMagic。

## 「跟在玩家身後的同伴」（Follow — 僅移動層）

一個以玩家為目標的 Follow 套件 + 公民身分食譜，會讓一個生成的 NPC 跟在玩家身後**並在快速旅行間持續存在**（引擎會把執行跟隨玩家套件的 actor 一起快速旅行）。*移動*層不需要任何管理用的 quest——僱用／解僱對話 + follow faction（見 [hireable-follower gotcha](gotchas.md)）只是用來讓玩家可切換而已。

```jsonc
{ "packages": [
    { "editorId": "MF_FollowPlayer", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "", "minRadius": 128, "maxRadius": 256, "accompany": true } }
  ],                                  // target "" ⇒ defaults to the player
  "npcs": [ { "editorId": "MF_Companion", ..., "packages": [ "MF_FollowPlayer" ] } ] }
```
