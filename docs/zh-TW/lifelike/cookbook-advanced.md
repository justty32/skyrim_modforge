<!-- 進階模式 -->
# 食譜手冊 — 進階模式

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「自訂龍吼」——SHOU + WOOP + 字詞之牆（遊戲內已確認 2026-06-01）

自訂龍吼的結構為 `MGEF → Voice SPEL → WOOP → SHOU`，加上一種**學習**方式。要在遊戲中實際觸發龍吼，需要在基本記錄之外補充四個要素：

1. **每個 Voice 法術都需要一個裝備槽。** Build 現在會**自動預設**可施放類型（Spell/Voice/Power/LesserPower）為 **EitherHand**（`Skyrim.esm:0x00013F44`）——這與每個原版龍吼字詞法術使用的相同 EQUP。
2. **MGEF 需要一個 `projectile`**，否則龍吼的力量無聲無息地發出。彈體攜帶移動中的模型、命中效果和命中音效。配合主題——冰霜龍吼用冰霜彈體（`0x02F774` FrostIcicle），力量龍吼用衝擊波（`0x013DF4` VoicePush）。加入 `castingArt` 產生施法時的閃光效果。
3. **`Release` 音效是效果音效**（雷聲/冰霜特效），透過 `magicEffects[].sounds` 設定。
4. **SHOU 需要一個 `menuDisplayObject`**（`0x0A59AC`），才能在龍吼選單中顯示預覽圖。

```jsonc
{ "magicEffects": [
    { "editorId": "MF_ForgedVoiceEffect", "archetype": "Stagger",
      "castType": "FireAndForget", "targetType": "Aimed", "flags": [ "NoHitEvent" ],
      "projectile": "Skyrim.esm:0x00013DF4",               // VoicePush 衝擊波（模型+命中+音效）
      "sounds": [ { "type": "Release", "sound": "Skyrim.esm:0x000A0F52" } ] } ],  // UnrelentingForce FX
  "spells": [   // 每個充能等級各一個 Voice 法術——spellType 必須為 "Voice"；equipType 自動 = EitherHand
    { "editorId": "MF_FV1", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 1 } ] },
    { "editorId": "MF_FV2", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 2 } ] },
    { "editorId": "MF_FV3", "name": "Forged Voice", "spellType": "Voice", "castType": "FireAndForget",
      "targetType": "Aimed", "effects": [ { "magicEffect": "MF_ForgedVoiceEffect", "magnitude": 3 } ] } ],
  "wordsOfPower": [
    { "editorId": "MF_Dov", "name": "Dov", "translation": "Dragon" },
    { "editorId": "MF_Ah",  "name": "Ah",  "translation": "Hunter" },
    { "editorId": "MF_Vul", "name": "Vul", "translation": "Forged" } ],
  "shouts": [
    { "editorId": "MF_ForgedVoice", "name": "Forged Voice", "menuDisplayObject": "Skyrim.esm:0x000A59AC",
      "words": [   // 恰好 3 個：word1 = 點擊，1+2 = 長按，1+2+3 = 完整充能
        { "word": "MF_Dov", "spell": "MF_FV1", "recoveryTime": 12 },
        { "word": "MF_Ah",  "spell": "MF_FV2", "recoveryTime": 18 },
        { "word": "MF_Vul", "spell": "MF_FV3", "recoveryTime": 25 } ] } ],
  "wordWalls": [
    { "editorId": "MF_ForgedVoiceWall", "name": "Forged Voice Word Wall",
      "shout": "MF_ForgedVoice", "wordIndex": 1,
      "scriptName": "ForgedVoiceWordWallScript",
      "cell": "Skyrim.esm:0x0371DE",
      "position": { "x": 0, "y": 0, "z": 0 } } ]
}
```

**主機台測試：** `help "Forged Voice" 0` → `player.addshout <SHOUT>`，然後對每個字詞使用 **`player.teachword <WORD>`**——`teachword`（而非只是 `unlockword`）才能使字形**顯示**在龍吼選單中。裝備它，長按龍吼按鍵：光束飛出，觸發效果，播放特效音效 + 命中效果。

**遊戲內已確認有效：** 可施放的龍吼、投射物 + 命中 + 效果音效、3 個充能等級。

**兩個誠實的限制：**
- **沒有口說龍語。** 玩家呼喊龍語音節（「FUS RO DAH」）是一個**已錄製的語音資產**（它同樣是一個 MGEF `Release` 音效，但是一個*帶語音*的 `.fuz`，例如 `VOCShoutDragon01AFus`）。程式化產生的龍吼沒有語音資產，因此字詞語音為靜音——只有效果特效播放。要提供它需要一個真實的語音檔案（見 voice-gen 計畫）。若要提供 3 個等級的*漸進式*效果音效，可使用 3 個 MGEF（每個字詞法術各一個），各自帶有其自己的 A/B/C `Release` 音效。
- **字詞之牆的學習是 `OnInit`，而非走近觸發。** 教授任務在遊戲開始時啟用，因此龍吼 + 字詞 1 在**插件載入後立即授予**——放置的 `WordWallTrigger` 只是裝飾（將其繫結至 `OnTriggerEnter` 以實現真正的走近學習屬於 CK 工作）。而範例 cell `0x0371DE` 是**原版**的力量龍吼房間，因此你在那裡看到的字牆是原版的，不是我們的（我們的在原點）。藍色的字詞發光 VFX 屬於 CK/網格 + Imagespace——並未產生。**原版**龍吼引用無法自動推導其字詞——請明確設定 `word`。

## 「自訂天空」（WTHR + CLMT——大氣效果，尚未指派）

一個詭異的綠色霧氣天氣，加上循環此天氣的氣候。完整範例規格：[`../../examples/weather_spec.json`](../../examples/weather_spec.json)。

```jsonc
{
  "pluginName": "ModForgeWeather.esp",
  "weathers": [{
    "editorId": "MF_EerieFog",
    "flags": ["Cloudy", "Rainy"],
    "skyUpperColor": { "day": { "r": 46, "g": 92, "b": 58 }, "night": { "r": 8, "g": 20, "b": 14 } },
    "fogNearColor":  { "day": { "r": 60, "g": 120, "b": 70 } },
    "sunlightColor": { "day": { "r": 120, "g": 170, "b": 110 } },   // 灑在世界上的病態綠光
    "clouds": [{ "index": 0, "texture": "Sky\\SkyrimCloudsUpper04.dds",
                 "xSpeed": 0.012, "ySpeed": -0.006, "alphaNight": 0.8 }],
    "precipitation": "Skyrim.esm:0x10780F",                          // 原版降雨 SPGD
    "windSpeed": 0.35, "windDirection": 210, "fogDayNear": 256, "fogDayFar": 9000
  }],
  "climates": [{
    "editorId": "MF_EerieClimate",
    "weathers": [ { "weather": "MF_EerieFog", "chance": 75 } ],
    "sunriseBegin": "06:00", "sunsetEnd": "20:00", "moons": ["Masser", "Secunda"]
  }]
}
```

結構驗證：`validate` → `build` → `dump`（或 `weatherdiag <esp> <0xFORMID>` / `climatediag <esp> <0xFORMID>`）。要尋找降水 SPGD，對某個原版的多雨天氣執行 `weatherdiag`（`find <Skyrim.esm> Rain Weather` → 例如 `SkyrimStormRain` `0x0C8220`，其 `Precipitation = 0x10780F`）。

**讓它在遊戲中毫無作用的唯一原因：** 一個 `WTHR`+`CLMT` 只是資料，直到某個東西*指派*了這個氣候。原版透過**世界空間**（`WRLD` 的 `Climate` 欄位）或**地區**（`REGN` 天氣資料）來實現——這兩者 ModForge 現在*都能*產生（見下一個配方）。因此這個配方交付一個有效、可檢視的氣候，你接著用 WRLD/REGN 指向它。**僅通過結構驗證；天空實際渲染尚未在遊戲中確認。**

## 「自訂室外世界空間 + 天氣地區」（WRLD + REGN + 平坦地形）

建立一個新的室外世界，附加氣候（天空/光照循環），加入可行走的平坦地形 cell，並新增一個天氣表格驅動某區域天氣播放的地區。

```jsonc
{ "esl": false,                              // 必須！ESL 外掛不載入 LAND records
  "worldspaces": [
    { "editorId": "MFTestWorld", "name": "ModForge Test Vale",
      "climate": "Skyrim.esm:0x000812",      // 若無此設定，世界將沒有天空/光照週期
      "water":   "Skyrim.esm:0x000018",      // DefaultWater（可選）
      "parent":  "Skyrim.esm:0x00003C",      // 上層 WRLD = Tamriel（可選）
      "flags":   [ "SmallWorld", "CannotFastTravel" ],
      "defaultLandHeight": -27000, "defaultWaterHeight": -14000,  // 防淹修正——保留這些值
      "cells": [ { "x": 0, "y": 0, "navmesh": true } ] } // 平坦地形 + 尋路網格；進入：cow MFTestWorld 0 0
  ],
  "regions": [
    { "editorId": "MFTestWorldWeather", "worldspace": "MFTestWorld", "weatherPriority": 60,
      "mapColor": "0x3CA0F0", "edgeFallOff": 1024,
      "weather": [ { "weather": "Skyrim.esm:0x10E1F2", "chance": 60 },   // SkyrimClear
                   { "weather": "Skyrim.esm:0x10E1F1", "chance": 30 },   // SkyrimCloudy
                   { "weather": "Skyrim.esm:0x10E1F0", "chance": 10 } ], // SkyrimClearSN
      "area": [ { "x": -16384, "y": -16384 }, { "x": 16384, "y": -16384 },
                { "x": 16384, "y": 16384 }, { "x": -16384, "y": 16384 } ] }
  ] }
```

`cells` 中每個條目生成一個 CELL + LAND（平坦 33×33 高度圖，`height` 預設 4000 game units，安全地高於 Z=0 的海平面）。`"navmesh": true` 額外生成一個平坦四邊形 NAVM（4 頂點、2 三角形覆蓋整個 4096×4096 cell）加上 NAVI 索引條目——NPC 可在 cell 內導航。無相鄰 cell 邊緣連結；在相鄰 cell 也設定 `navmesh: true` 即可跨 cell 尋路。**實機確認**（2026-06-03）：`cow MFTestWorld 0 0` → 玩家降落在堅實的平坦地面上。用 `worlddiag <Skyrim.esm> 0x00003C` 和 `regndiag <Skyrim.esm> <0xFORMID>` 採集原版數值。完整範例：`examples/worldspace_spec.json`。

## 「兩個 NPC 爭論」（SCEN 多角色對話——僅限結構，尚未在遊戲中確認）

`scene` 是讓 NPC **彼此**對話，而非與玩家對話。它由一個任務宿主，任務的**別名**即為參與者；建構程序會產生別名繫結、Scene 記錄，以及每句台詞各一個 Scene/`SCEN` 話題。將兩個 NPC**放置在同一個 CELL、彼此靠近**。

```jsonc
{ "quests": [ { "editorId": "MF_SceneQuest", "name": "...", "startGameEnabled": true } ],
  "npcs": [
    { "editorId": "MF_Borin", "name": "Borin", "greeting": "...", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6", "unique": true },
    { "editorId": "MF_Hilda", "name": "Hilda", "greeting": "...", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE7", "unique": true } ],
  "scenes": [
    { "editorId": "MF_TavernArgument", "questEditorId": "MF_SceneQuest", "beginOnQuestStart": true,
      "actors": [ { "aliasId": 0, "npc": "MF_Borin" }, { "aliasId": 1, "npc": "MF_Hilda" } ],
      "phases": [
        { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
        { "speaker": 1, "emotion": "Disgust", "lines": [ "That swill wasn't worth a clipped septim." ] },
        { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
        { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] } ],
  "placements": [
    { "base": "MF_Borin", "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 180, "z": 0 } },
    { "base": "MF_Hilda", "cell": "Skyrim.esm:0x0133C6", "position": { "x": -300, "y": 280, "z": 0 },
      "rotation": { "x": 0, "y": 0, "z": 180 } } ] }
```

它與原版的對應關係（已透過 `scenediag <Skyrim.esm> <0xFORMID>` 對照原版場景驗證）：
- 宿主任務的每個 `actor` 各有一個 **QuestAlias**，以 `UniqueActor` 繫結至該 NPC——Scene 的 `SceneActors` 參照**別名索引**（aliasId），而非直接參照 NPC FormKey；
- 每個 `phase` → 一個 `ScenePhase` + 一個**對話 `SceneAction`**（說話者 alias、階段窗口、另一個角色作為注視目標）+ 一個**Scene/`SCEN` DialogTopic+INFO** 持有台詞；
- `beginOnQuestStart` 在任務開始的瞬間播放場景（即遊戲載入時）。

**狀態/誠實說明：** `build`/`validate`/`dump` 均乾淨通過，記錄結構逐位元組符合原版，但這**尚未在遊戲中確認**。可能的後續工作：場景可能需要除 `beginOnQuestStart` 之外的**啟動觸發器**（任務階段 / 腳本 `Start()` 呼叫）、演員別名可能需要**填充條件**，以及 NPC 需要**處於清醒且可到達的狀態**（Sandbox 套件讓他們保持活躍）。使用 `scenediag` 探查任何原版場景以進行比較。參見 `examples/scene_spec.json`。
