<!-- 簡介與記錄類型 -->
# ModForge 規格說明 — 簡介與記錄類型

← [目錄](SPEC-index.md)

**spec** 是一個 JSON 檔案，描述一個 Skyrim 外掛的內容。它是意圖（自然語言，由 AI 代理轉換為規格）與確定性生成器（Mutagen）之間的契約。你撰寫或產生一個 spec，對其執行 `validate`，然後執行 `build` 或 `package`。

```
NL / idea ──(AI agent: Claude Code)──▶ spec.json ──(validate)──▶ ──(build | package)──▶ .esp [+ .pex]
```

屬性名稱**不區分大小寫**（`editorId` == `EditorId`）；範例使用 camelCase。

## 交叉參照與 ID

- 每筆記錄都有一個 **`editorId`** — 你自行選擇的穩定且唯一的名稱。這是記錄在 *spec 內部*相互參照的方式（一個 npc 透過其 `editorId` 加入陣營；一個對話透過 `editorId` 指名其任務）。它**不是** FormID：Mutagen 會自動指派 FormID 與 master。
- `editorId` 必須在整個 spec 中**非空且唯一**（`validate` 會強制執行）。
- `esl: true`（預設值）將外掛標記為輕量 master — 新記錄的 FormID 必須符合 **0x800–0xFFF，即總計 ≤ 2048** 筆。超過此限制在寫入時會產生嚴重錯誤（附帶明確訊息）；若需要更多記錄，請設定 `esl: false` 或將內容分散到多個外掛中。

### 參照 vanilla / 外部 form
某些欄位為 **refs**：它們接受 *spec 內部的* `editorId` *或*另一個外掛中的外部 form，寫作 **`"<master>:0xFORMID"`**（例如 `"Skyrim.esm:0x013746"` = `NordRace`）。外部 refs 讓你的內容可以指向 vanilla 的種族、職業、裝束、關鍵字、陣營等。指定的 master 在建置時會**自動加入外掛**。

- **使用 `find` 指令查找 FormID：**
  `find "<Skyrim Data>/Skyrim.esm" <query> [type]` → 印出 `Skyrim.esm:0xFORMID  Type  EditorID`。
  `[type]`（例如 `Race`、`Class`、`Outfit`、`Keyword`、`Faction`、`Weapon`、`Npc`）可將搜尋範圍縮小至單一記錄類型。（搜尋與顯示以 **EditorID** 為準；本地化顯示*名稱*打包在 BSA 中，無頭模式下無法解析 — 像 `NordRace` 這樣的 EditorID 已足夠描述性。）
- `validate` 會檢查 ref 欄位：spec 內部的 ref 必須存在；外部 ref 必須格式正確。

## 頂層結構

```jsonc
{
  "pluginName": "MyMod.esp",   // 輸出檔名 / ModKey
  "esl": true,                  // 輕量 master 旗標（預設 true）；≤2048 筆新記錄

  "miscItems": [...], "books": [...], "weapons": [...], "npcs": [...],
  "quests": [...], "dialogue": [...], "banter": [...], "spells": [...], "potions": [...],
  "armors": [...], "factions": [...], "messages": [...],
  "scripts": [...],             // Papyrus 附加（見下方）
  "cells": [...], "placements": [...],  // 新室內 cell + 在其中放置 form
  "leveledItems": [...], "leveledNpcs": [...], "containers": [...],
  "ingredients": [...], "ammunitions": [...], "scrolls": [...], "soulGems": [...],
  "keys": [...], "keywords": [...], "outfits": [...], "statics": [...], "activators": [...],
  "textureSets": [...],          // TXST — 在不使用新 .nif 的情況下重新貼圖現有網格
  "furniture": [...], "sounds": [...],  // 自訂網格家具 + 音效描述符（外部資產）
  "assets": "path/to/asset/dir",        // 來源目錄，其 Meshes/Textures/Sounds 由 `package` 打包
  "packages": [...],             // AI Packages — NPC 的行為（沙盒/移動/使用家具）
  "weathers": [...], "climates": [...],  // 自訂天空（WTHR）+ 天氣循環（CLMT）
  "encounterZones": [...]        // ECZN — 某區域的等級縮放/重生
}
```

## 記錄類型

| 區段 | 欄位 |
|---------|--------|
| `miscItems` | `editorId`、`name`、`value`（int≥0）、`weight`（數字）、`keywords`（*refs* 陣列）、`template`（vanilla MISC ref，用於複製模型）、`model`（自訂 `.nif` 路徑 — 覆蓋 `template` 的網格）、`pickUpSound`/`putDownSound`（SNDR *refs*）— 見 [external_assets.md](external_assets.md) |
| `books` | `editorId`、`name`、`text`（書本內文）、`template`（*ref* → vanilla BOOK，用於複製模型 — 可拾取/閱讀的書**必須有此項，否則在 3D 閱讀時會 CRASH**）、`value`（int）、`weight`（數字）、`flags`（`Book.Flag` 名稱陣列，例如 `CantBeTaken`）、`teaches`（可選 — 一本*教學*書；見下方） |
| `books[].teaches` | `{ "kind": "spell", "spell": <ref> }` — 一本**法術書**，首次閱讀時授予 SPEL；或 `{ "kind": "skill", "skill": <name> }` — 一本**技能書**，首次閱讀時提升某項 `Skill`；或省略 ⇒ 普通書（不教授任何內容）。教學書必須有 `template`。 |
| `weapons` | `editorId`、`name`、`value`、`weight`、`damage`（int≥0）、`speed`（數字）、`reach`（數字）、`keywords`（*refs* 陣列）、`enchantment`（*ref* → ENCH）、`enchantmentAmount`（int — 武器的充能池；0 = 引擎自動計算）、`template`（vanilla WEAP ref — 複製模型/動畫/裝備；需要此項以避免裝備時 CRASH）、`model`（自訂世界網格 `.nif` 路徑 — 搭配 `template` 一起使用）、`pickUpSound`/`putDownSound`（SNDR *refs*） |
| `npcs` | `editorId`、`name`、`factions`（*refs* 陣列）、`race`（*ref*）、`class`（*ref*）、`outfit`（*ref* → DefaultOutfit）、`level`（int）、`autoCalcStats`（bool — 根據等級 + 職業推算 H/M/S + 技能）、`packages`（*refs* → PACK 陣列；NPC 的 AI 套件清單，按順序評估）、`voiceType`（*ref* → VTYP）、`crimeFaction`（*ref* → FACT；城市市民身份，跨 cell 移動所需）、`unique`（bool — 單一角色，有助於引擎 AI 追蹤）、`combatStyle`（*ref* → CSTY；AI 的戰鬥方式）、`spells`（*refs* → SPEL 陣列；AI 的法術清單）、`perks`（*refs* → PERK 陣列；遊戲開始時作為被動能力/入口點 perk 授予角色）、`greeting`（字串 — Hello 台詞；當此 NPC 有自訂 `dialogue` 時，會自動生成一個 Hello info 使其可對話。空字串 ⇒ 使用預設台詞） |
| `quests` | `editorId`、`name`、`startGameEnabled`（bool，預設 true）、`priority`（0–255）、`objectives`（`{ index (int), text, showStage?, completeStage? }` 陣列）、`stages`（`{ index (int), logEntry?, completeQuest?, failQuest?, conditions? }` 陣列）— 見 [SPEC-dialogue-quests](SPEC-dialogue-quests.md) |
| `dialogue` | `editorId`、`questEditorId`、`speakerNpcEditorId`（可選）、`prompt`、`responses`（字串陣列）、`emotion`（可選）、`emotionValue`（0–100）。`setStage`（int）、`resultScript`/`resultScriptSource`/`resultProperties`（Papyrus fragment）、`goodbye`（bool）。 |
| `banter` | `editorId`（可選）、`questEditorId`、`speakerNpcEditorId`、`responses`（字串陣列）、`emotion`/`emotionValue`、`conditions`（情境 CTDA 閘門）。 |
| `scenes` | `editorId`、`questEditorId`（宿主任務）、`actors`（`{ aliasId, npc (*ref*), name }` 陣列）、`phases`（有序的 `{ speaker (aliasId), lines, emotion, emotionValue }` 陣列）、`beginOnQuestStart`（bool）、`stopQuestOnEnd`（bool）。 |
| `spells` | `editorId`、`name`、`effects`（*effects* 陣列）、`spellType`、`castType`、`targetType`、`baseCost`（int）、`chargeTime`（數字）、`equipType`（EQUP *ref*）。**可施放類型省略時自動預設為 EitherHand `Skyrim.esm:0x00013F44`** |
| `magicEffects` | `editorId`、`name`、`description`、`archetype`、`actorValue`、`magicSkill`、`resistValue`、`castType`、`targetType`、`baseCost`（數字）、`flags`（陣列）、`association`（*ref*）、`projectile`/`castingArt`/`hitEffectArt`/`explosion`（*refs*）、`sounds`（陣列） |
| `enchantments` | `editorId`、`name`、`enchantType`（`weapon`\|`apparel`\|`staff`）、`castType`/`targetType`（可選）、`enchantmentCost`（int）、`chargeTime`（數字）、`effects`（陣列） |
| `potions` | `editorId`、`name`、`value`、`weight`、`effects`（陣列） |
| `armors` | `editorId`、`name`、`value`、`weight`、`armorRating`（數字）、`armorType`（`light`\|`heavy`\|`clothing`）、`slots`（陣列）、`keywords`（陣列）、`enchantment`（*ref*）、`template`（vanilla ARMO *ref* — **必須有，否則裝備隱形**）、`model`（自訂地面網格） |
| `factions` | `editorId`、`name`、`vendor`（可選子物件 — 商人陣營） |
| `classes` | `editorId`、`name`、`description`、`teaches`（Skill）、`maxTrainingLevel`、`healthWeight`/`magickaWeight`/`staminaWeight`、`skillWeights`（`{ Skill: 0–255 }`） |
| `messages` | `editorId`、`name`、`description`（本文內容） |
| `cells` | `editorId`、`name`、`template`（vanilla 室內 cell，用於複製光照 — 否則全黑）、`encounterZone`（*ref* → ECZN） |
| `placements` | `base`（*ref* — **絕對不能是 LVLN**，否則 CTD）；`cell` 或 `worldspace`；`kind`（`npc`\|`object`）、`position`（`{x,y,z}`）、`rotation`（`{x,y,z}` 度）、`persistent`（bool）、`encounterZone`（*ref*） |
| `leveledItems` | `editorId`、`chanceNone`（0–100）、`flags`（陣列）、`entries`（`{ reference (*ref*), level (int), count (int) }` 陣列） |
| `leveledNpcs` | 與 `leveledItems` 相同結構，但 `reference` 為 npc/leveled-npc |
| `containers` | `editorId`、`name`、`weight`、`items`（`{ item (*ref*), count (int) }` 陣列） |
| `ingredients` | `editorId`、`name`、`value`、`weight`、`effects`（陣列）、`keywords`（陣列） |
| `ammunitions` | `editorId`、`name`、`value`、`weight`、`damage`（數字）、`keywords`（陣列） |
| `scrolls` | `editorId`、`name`、`value`、`weight`、`effects`（陣列）、`spellType`、`castType`、`targetType`、`baseCost`（int）、`keywords`（陣列） |
| `soulGems` | `editorId`、`name`、`value`、`weight`、`maximumCapacity`（`None`\|`Petty`\|`Lesser`\|`Common`\|`Greater`\|`Grand`）、`keywords`（陣列） |
| `keys` | `editorId`、`name`、`value`、`weight`、`keywords`（陣列） |
| `keywords` | `editorId`（定義你自己的關鍵字） |
| `outfits` | `editorId`、`items`（*refs* → 盔甲/武器陣列） |
| `statics` | `editorId`、`model`（`.nif` 路徑）、`alternateTextures`（陣列） |
| `activators` | `editorId`、`name`、`model`（`.nif` 路徑）、`keywords`（陣列）、`alternateTextures`（陣列）、`activationSound`/`loopingSound`（SNDR *refs*） |
| `furniture` | `editorId`、`name`、`model`（`.nif` 路徑）、`keywords`（陣列） |
| `sounds` | `editorId`、`files`（路徑陣列）、`category`（SNCT *ref*）、`outputModel`（SOPM *ref*）、`priority`（0–255）、`staticAttenuation`（dB） |
| `recipes` | `editorId`、`kind`（`craft`/`temper`/`smelt`/`breakdown`）、`createdObject`（*ref*）、`count`（int）、`workbench`（具名選擇器或關鍵字 *ref*）、`components`（陣列）、`conditions`（CTDA 陣列） |
| `packages` | `editorId`、`template`（*ref*）、`flags`、`interruptFlags`、`preferredSpeed`、`schedule`、`sandbox`/`sleep`/`travel`/`useMagic`/`patrol`/`follow`/`escort`（模板輸入子物件）、`conditions` |
| `combatStyles` | `editorId`、`offensiveMult`/`defensiveMult`/`groupOffensiveMult`、各種 `equipMult*`、`avoidThreatChance`、`flags` |
| `encounterZones` | `editorId`、`minLevel`（0–255）、`maxLevel`（0–255；**0 = 無上限**）、`rank`、`owner`（*ref*）、`location`（*ref*）、`flags` |
| `perks` | `editorId`、`name`、`description`、`playable`/`hidden`/`trait`（bool）、`level`、`numRanks`、`nextPerk`（*ref*）、`conditions`（陣列）、`effects`（陣列） |
| `wordsOfPower` | `editorId`、`name`（龍語字符）、`translation`（英文字義） |
| `shouts` | `editorId`、`name`、`description`、`menuDisplayObject`（*ref* → STAT）、`words`（最多 3 個 `{ word, spell, recoveryTime }` 的陣列） |
| `wordWalls` | `editorId`、`name`、`shout`（*ref*）、`wordIndex`（1\|2\|3）、`word`（*ref*，vanilla 咆哮必須提供）、`scriptName`、觸發器位置 |
| `textureSets` | `editorId`，八個可選的 `.dds` 槽位路徑（`diffuse`、`normal`、`mask`、`glow`、`height`、`environment`、`multilayer`、`backlight`），路徑**相對於 `Data\Textures\`**，`flags`（陣列） |
| `weathers` | `editorId`、`flags`、各時段顏色、`clouds`（陣列）、`precipitation`（*ref* → SPGD）、風速/霧距 |
| `climates` | `editorId`、`weathers`（陣列）、日出/日落時間、`sunTexture`/`sunGlareTexture`、`moons`（陣列）、`phaseLength`、`volatility` |

標記為 *ref* 的欄位接受 spec 內部的 `editorId` **或** `"<master>:0xFORMID"`。一個站立的 NPC 至少需要 `race` + `class` 才能在遊戲中表現為真實角色；`outfit` 為其提供服裝/裝備。
