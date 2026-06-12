# ModForge 規格說明 — 簡介與記錄類型表

← [index](SPEC-index.md)

**spec** 是一個 JSON 檔案，描述一個 Skyrim 外掛的內容。它是意圖（自然語言，由 AI 代理轉換為 spec）與確定性生成器（Mutagen）之間的契約。你撰寫/產生一個 spec，對其執行 `validate`，然後執行 `build` 或 `package`。

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
  `[type]`（例如 `Race`、`Class`、`Outfit`、`Keyword`、`Faction`、`Weapon`、`Npc`）可將搜尋範圍縮小至單一記錄類型。（搜尋/顯示以 **EditorID** 為準；本地化顯示*名稱*打包在 BSA 中，無頭模式下無法解析 — 像 `NordRace` 這樣的 EditorID 已足夠描述性。）
- `validate` 會檢查 ref 欄位：spec 內部的 ref 必須存在；外部 ref 必須格式正確。

## 頂層結構

```jsonc
{
  "pluginName": "MyMod.esp",   // 輸出檔名 / ModKey
  "esl": true,                  // 輕量 master 旗標（預設 true）；≤2048 筆新記錄

  "miscItems": [...], "books": [...], "weapons": [...], "npcs": [...],
  "voiceTemplates": [...],       // TTS/voice cloning recipe；NPC 可用 voiceTemplate 指向它
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
  "encounterZones": [...]        // ECZN — 某區域的等級縮放/重生（一個 cell/spawn 指向它）
}
```

## 記錄類型

| section | fields |
|---------|--------|
| `miscItems` | `editorId`、`name`、`value`（int≥0）、`weight`（數字）、`keywords`（*refs* 陣列）、`template`（vanilla MISC ref，用於複製模型）、`model`（自訂 `.nif` 路徑 — 覆蓋 `template` 的網格）、`pickUpSound`/`putDownSound`（SNDR *refs*）— 見 [external_assets.md](external_assets.md) |
| `books` | `editorId`、`name`、`text`（書本內文）、`template`（*ref* → vanilla BOOK，用於複製模型 — 一本可拾取/閱讀的書**需要它，否則在 3D 閱讀時會 CRASH**）、`value`（int；0 ⇒ 保留 template 的值）、`weight`（數字；0 ⇒ 保留 template 的值）、`flags`（`Book.Flag` 名稱陣列，例如 `CantBeTaken`）、`teaches`（可選 — 一本*教學*書；見下方） |
| `books[].teaches` | `{ "kind": "spell", "spell": <ref> }` — 一本**法術書**，首次閱讀時授予一個 SPEL（`spell` 為 spec 內部的法術 editorId 或 vanilla `<master>:0xFORMID`）；或 `{ "kind": "skill", "skill": <name> }` — 一本**技能書**，首次閱讀時提升某項 `Skill`（例如 `Destruction`、`OneHanded`、`Smithing`）；或省略 ⇒ 普通書（不教授任何內容）。教學書必須有 `template`。 |
| `weapons` | `editorId`、`name`、`value`、`weight`、`damage`（int≥0）、`speed`（數字）、`reach`（數字）、`keywords`（*refs* 陣列）、`enchantment`（*ref* → ENCH，spec 內部或 vanilla `<master>:0xFORMID`）、`enchantmentAmount`（int — 武器的充能池，例如 1500–3000；0 = 引擎自動計算）、`template`（vanilla WEAP ref — 複製模型/動畫/裝備；需要此項以避免裝備時 CRASH）、`model`（自訂世界網格 `.nif` 路徑 — 搭配 `template` 一起使用）、`pickUpSound`/`putDownSound`（SNDR *refs*） |
| `voiceTemplates` | `id`、`engine`（`f5`、`fish-s2`；`chatterbox`/`gptsovits`/`xtts` 為保留 wrapper 名）、`referenceWav`、`referenceText`、`modelPath`、`language`、`seed`、`speed`、`exaggeration`。`voicelines` 會依 template 生成 voice assets；見 [SPEC-workflow](SPEC-workflow.md) |
| `npcs` | `editorId`、`name`、`factions`（*refs* 陣列）、`race`（*ref*）、`class`（*ref*）、`outfit`（*ref* → DefaultOutfit）、`level`（int）、`autoCalcStats`（bool — 根據等級 + 職業推算 H/M/S + 技能）、`packages`（*refs* → PACK 陣列；NPC 的 AI 套件清單，按順序評估）、`voiceType`（*ref* → VTYP；決定 `Sound/Voice/<plugin>/<voiceType>/` folder）、`voiceTemplate`（*ref* → `voiceTemplates[].id`；決定 TTS engine/參考聲音）、`crimeFaction`（*ref* → FACT；城市市民身份，跨 cell 移動所需）、`unique`（bool — 單一角色，有助於引擎 AI 追蹤）、`combatStyle`（*ref* → CSTY；AI 的戰鬥方式）、`spells`（*refs* → SPEL 陣列；AI 的法術清單）、`perks`（*refs* → PERK 陣列；遊戲開始時作為被動能力/入口點 perk 授予角色）、`greeting`（字串 — Hello 台詞；當此 NPC 有自訂 `dialogue` 時，會自動發出一個 Hello info 使其可對話。空字串 ⇒ 使用預設台詞） |
| `quests` | `editorId`、`name`、`startGameEnabled`（bool，預設 true）、`priority`（0–255）、`objectives`（`{ index (int), text, showStage?, completeStage? }` 陣列）、`stages`（`{ index (int), logEntry?, completeQuest?, failQuest?, conditions? }` 陣列）— 見 [SPEC-dialogue-quests](SPEC-dialogue-quests.md) 中的 *Quest stages* |
| `dialogue` | `editorId`、`questEditorId`、`speakerNpcEditorId`（可選）、`prompt`、`responses`（字串陣列）、`emotion`（可選 — `Neutral`\|`Anger`\|`Disgust`\|`Fear`\|`Sad`\|`Happy`\|`Surprise`）、`emotionValue`（0–100）、`voiceLine`（可選 `{ format: "wav"|"xwm"|"fuz", skipLip: bool }`）。`setStage`（int — 選取此台詞時將任務推進到此階段；`package` 自動編譯 + VMAD 附加 TIF fragment 並自動加上 `GetStage < N` 條件使台詞不重複）。可選的**自訂 result fragment**（覆蓋自動 TIF）：`resultScript`（Scriptname，`Extends TopicInfo`，`Fragment_0`）、`resultScriptSource`（`.psc`）、`resultProperties`（綁定 props）、`goodbye`（bool — 之後關閉選單）。Build 串接完整鏈（Quest→DialogView→Branch→Topic→INFO + 一個 Hello）— 見 [SPEC-dialogue-quests](SPEC-dialogue-quests.md) |
| `banter` | `editorId`（可選）、`questEditorId`、`speakerNpcEditorId`、`responses`（字串陣列 — 一條無提示的評論）、`emotion`/`emotionValue`、`conditions`（情境 CTDA 閘門）。主動（NPC 發起）的台詞；共享 (speaker, quest) 的條目合併為一個帶 Random INFO 的環境 Misc/`IDLE` topic。需要 speaker 啟用閒談（Sandbox/follow 套件）。見 [SPEC-dialogue-quests](SPEC-dialogue-quests.md) |
| `scenes` | `editorId`、`questEditorId`（宿主任務）、`actors`（`{ aliasId (int), npc (*ref*), name }` 陣列）、`phases`（有序的 `{ speaker (一個 aliasId), lines (字串陣列), emotion, emotionValue }` 陣列）、`beginOnQuestStart`（bool，預設 true）、`stopQuestOnEnd`（bool）。一個 **SCEN** — 兩個 NPC 互相對話。見 [SPEC-dialogue-quests](SPEC-dialogue-quests.md) |
| `spells` | `editorId`、`name`、`effects`（*effects* 陣列）、`spellType`、`castType`、`targetType`、`baseCost`（int）、`chargeTime`（數字）、`equipType`（EQUP *ref*）。**可施放類型（Spell/Voice/Power/LesserPower）省略時自動預設為 EitherHand `Skyrim.esm:0x00013F44`** — 一個沒有 EQUP 的 Voice/shout 法術會被學會但**無法喊出**；只在需要覆蓋時設定 |
| `magicEffects` | `editorId`、`name`、`description`、`archetype`、`actorValue`、`magicSkill`、`resistValue`、`castType`、`targetType`、`baseCost`（數字）、`flags`（陣列）、`association`（*ref*）、`projectile`/`castingArt`/`hitEffectArt`/`explosion`（*refs* — 可見的飛行彈 + 施法/撞擊 FX；一個 Aimed 法術/咆哮需要一個 `projectile`，否則會隱形/無聲地發射）、`sounds`（`{ type (預設 `Release`), sound (SNDR *ref*) }` 陣列 — `Release` 是施放出去/效果音；咆哮的口說*語音*是錄製的語音資產，無法在此設定）— 一個 `effect` 可指向的自訂 MGEF |
| `enchantments` | `editorId`、`name`、`enchantType`（`weapon`\|`apparel`\|`staff`）、`castType`/`targetType`（可選覆蓋）、`enchantmentCost`（int — 每次施放充能成本 / 穿戴成本）、`chargeTime`（數字 — 法杖充能）、`effects`（*effects* 陣列）— 一個武器/盔甲 `enchantment` 欄位指向的物品效果（ENCH） |
| `potions` | `editorId`、`name`、`value`、`weight`、`effects`（*effects* 陣列） |
| `armors` | `editorId`、`name`、`value`、`weight`、`armorRating`（數字）、`armorType`（`light`\|`heavy`\|`clothing`）、`slots`（雙足槽位名稱陣列）、`keywords`（*refs* 陣列）、`enchantment`（*ref* → ENCH，通常是一個 `apparel` 常效型）、`template`（vanilla ARMO *ref* — 複製其 **Armature**（穿戴網格）+ WorldModel；**必須有，否則盔甲裝備隱形**，例如 `Skyrim.esm:0x00012E49` ArmorIronCuirass）、`model`（自訂地面網格 `.nif` 路徑 — 搭配 `template` 一起使用） |
| `factions` | `editorId`、`name`、`vendor`（可選子物件 — 將此轉為**商人**陣營；見 [SPEC-world](SPEC-world.md)） |
| `classes` | `editorId`、`name`、`description`、`teaches`（Skill）、`maxTrainingLevel`、`healthWeight`/`magickaWeight`/`staminaWeight`（屬性分配）、`skillWeights`（`{ Skill: 0–255 }`）— 一個 npc 的 `class` 可指向它 |
| `messages` | `editorId`、`name`、`description`（本文內容） |
| `cells` | `editorId`、`name`、`template`（vanilla 室內 cell `<master>:0xFORMID`，用於複製光照 — 否則新 cell 全黑）、`encounterZone`（*ref* → ECZN — 整個 cell 的等級縮放/重生） |
| `placements` | `base`（*ref* — 一個具體的 NPC_ actor 或物件 form；**絕對不能是原始的 LeveledNpc 清單（LVLN）** — LVLN 作為 ACHR base 在載入時 CTD，見 [SPEC-world](SPEC-world.md)）；**室內：** `cell`（spec 內部 editorId **或** vanilla 室內 cell `<master>:0xFORMID`）**或室外：** `worldspace`（`<master>:0xFORMID`，position 為世界座標）；`kind`（`npc`\|`object`）、`position`（`{x,y,z}`）、`rotation`（`{x,y,z}` 度）、`persistent`（bool）、`encounterZone`（*ref* → ECZN — 對該 cell 區域的逐 ref 覆蓋） |
| `leveledItems` | `editorId`、`chanceNone`（0–100）、`flags`（陣列）、`entries`（`{ reference (*ref*), level (int), count (int) }` 陣列） |
| `leveledNpcs` | 與 `leveledItems` 相同結構，但 `reference` 為一個 npc/leveled-npc |
| `containers` | `editorId`、`name`、`weight`、`items`（`{ item (*ref*), count (int) }` 陣列） |
| `ingredients` | `editorId`、`name`、`value`、`weight`、`effects`（*effects* 陣列）、`keywords`（*refs* 陣列） |
| `ammunitions` | `editorId`、`name`、`value`、`weight`、`damage`（數字）、`keywords`（*refs* 陣列） |
| `scrolls` | `editorId`、`name`、`value`、`weight`、`effects`（*effects* 陣列）、`spellType`、`castType`、`targetType`、`baseCost`（int）、`keywords`（*refs* 陣列） |
| `soulGems` | `editorId`、`name`、`value`、`weight`、`maximumCapacity`（`None`\|`Petty`\|`Lesser`\|`Common`\|`Greater`\|`Grand`）、`keywords`（*refs* 陣列） |
| `keys` | `editorId`、`name`、`value`、`weight`、`keywords`（*refs* 陣列） |
| `keywords` | `editorId`（定義你自己的關鍵字，使 spec 內部記錄可在其 `keywords` 中列出它） |
| `outfits` | `editorId`、`items`（*refs* → 盔甲/武器陣列；一個 npc 的 `outfit` 可指向此 editorId） |
| `statics` | `editorId`、`model`（一個 `.nif` 路徑 — vanilla 或自訂網格；一個 placement base，無 name）、`alternateTextures`（陣列 — 將網格的貼圖替換為一個 TXST；見 [SPEC-items](SPEC-items.md)） |
| `activators` | `editorId`、`name`、`model`（`.nif` 路徑）、`keywords`（*refs* 陣列）、`alternateTextures`（陣列 — 同 `statics`）、`activationSound`/`loopingSound`（SNDR *refs*）；透過 `scripts` 附加行為 |
| `furniture` | `editorId`、`name`、`model`（`.nif` 路徑 — vanilla 或自訂網格）、`keywords`（*refs* 陣列）— 一個可放置的互動物件（椅子/床/長椅/idle 標記）；以一個 `placement` 放置 |
| `sounds` | `editorId`、`files`（Data 相對的 `Sound\...` `.wav`/`.xwm` 路徑陣列）、`category`（SNCT *ref*，預設 AudioCategorySFX）、`outputModel`（SOPM *ref*，預設 vanilla SFX）、`priority`（0–255）、`staticAttenuation`（dB）— 一個記錄的 sound 欄位指向的音效描述符（SNDR）。見 [external_assets.md](external_assets.md) |
| `recipes` | `editorId`、`kind`（`craft`/`temper`/`smelt`/`breakdown`）、`createdObject`（*ref*）、`count`（int）、`workbench`（具名選擇器 `forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge` 或一個關鍵字 *ref*；依 kind 預設）、`components`（`{ item (*ref*), count (int) }` 陣列）、`conditions`（共用 CTDA `{ function, param (*ref*), comparison, value, or }` 陣列 — perk/物品/技能設置條件）— 一個合成/改良/熔煉配方（COBJ） |
| `packages` | `editorId`、`template`（*ref* → 一個 vanilla 程序模板）、`flags`、`interruptFlags`、`preferredSpeed`、`combatStyle`、`ownerQuest`、`schedule`、`sandbox`/`sleep`/`travel`/`useMagic`/`patrol`/`follow`/`escort`（模板子物件）、`conditions` — 見 [SPEC-packages](SPEC-packages.md) |
| `combatStyles` | `editorId`、`offensiveMult`/`defensiveMult`/`groupOffensiveMult`、`equipMultMelee`/`equipMultMagic`/`equipMultRanged`/`equipMultShout`/`equipMultUnarmed`/`equipMultStaff`、`avoidThreatChance`、`flags`（`Dueling`\|`Flanking`\|`AllowDualWielding`） |
| `encounterZones` | `editorId`、`minLevel`（0–255）、`maxLevel`（0–255；**0 = 無上限**）、`rank`、`owner`、`location`、`flags`（`NeverResets`\|`MatchPcBelowMinimumLevel`\|`DisableCombatBoundary`）— 見 [SPEC-world](SPEC-world.md) |
| `perks` | `editorId`、`name`、`description`、`playable`/`hidden`/`trait`、`level`、`numRanks`（≥1）、`nextPerk`、`conditions`、`effects`（陣列 — `ability` 或 `entryPoint`）— 見 [SPEC-items](SPEC-items.md) |
| `wordsOfPower` | `editorId`、`name`（龍語字符）、`translation`（英文字義）— 一個力量之語（WOOP） |
| `shouts` | `editorId`、`name`、`description`、`menuDisplayObject`、`words`（最多 3 個 `{ word, spell, recoveryTime }` 的陣列）— 一個 SHOU |
| `wordWalls` | `editorId`、`name`、`shout`、`wordIndex`（1\|2\|3）、`word`、`scriptName`、`triggerEditorId`/`triggerBase`、placement（`cell`/`worldspace` + `position`/`rotation`） |
| `textureSets` | `editorId`，八個可選的 `.dds` 槽位路徑（`diffuse`、`normal`、`mask`、`glow`、`height`、`environment`、`multilayer`、`backlight`），路徑相對於 `Data\Textures\`，`flags` — 見 [SPEC-items](SPEC-items.md) |
| `weathers` | `editorId`、`flags`、各時段顏色、`clouds`、`precipitation`、`windSpeed`/`windDirection`、`fogDayNear`/`fogDayFar`/`fogNightNear`/`fogNightFar`、`transitionDelta` — 見 [SPEC-packages](SPEC-packages.md) |
| `climates` | `editorId`、`weathers`（`{ weather, chance }` 陣列）、日出/日落時間、`sunTexture`/`sunGlareTexture`、`moons`、`phaseLength`、`volatility` — 見 [SPEC-packages](SPEC-packages.md) |

一個標記為 *ref* 的欄位接受 spec 內部的 `editorId` **或** `"<master>:0xFORMID"`（見上方*參照 vanilla / 外部 form*）。一個站立的 NPC 至少需要 `race` + `class` 才能在遊戲中表現為真實角色；`outfit` 為其提供服裝/裝備。
