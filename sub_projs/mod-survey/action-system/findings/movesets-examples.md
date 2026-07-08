# Moveset 實例庫拆解（DAR / OAR / SCAR 真實檔案結構）

← [action-system 中樞](../README.md)

> 樣本來源：`~/skyrim_mods/hdd/Animatecc/`（~57 個 MCO/SCAR/OAR/DAR movesets，玩家＋NPC）。解壓樣本於 gitignored `game-data/mods/action-system/movesets/`。
> **價值**：這是未來 ModForge [OAR 生成器](../../../../workflows/roadmap/README.md)的**確切輸出規格**——把「條件式動畫包」從概念釘到真實 JSON/資料夾佈局。以下全是實檔驗證。

## 三種佈局並存（同一目的、不同世代）

### A. DAR（legacy，`_conditions.txt` DSL）— 例：ER Rapier (SCAR)
```
meshes\actors\character\animations\DynamicAnimationReplacer\_CustomConditions\
   20000029\                       ← 資料夾名＝priority 整數（大者勝）
      _conditions.txt              ← 條件（DSL 純文字）
      mco_attack1..7.hkx           ← MCO 連段動畫（檔名＝被替換的 vanilla/MCO handle）
      mco_powerattack1..7.hkx
      1hm_idle.hkx
      SCAR_1hmReadyDummy.hkx       ← SCAR 標記動畫（讓此 moveset 被 SCAR AI 接管）
   20000030\ …                     ← 另一變體（更高 priority + 額外條件）
```
`_conditions.txt` DSL（實檔）：
```
IsEquippedRight("Dark Souls Item Pack by Team TAL.esp" | 0x0000091A) OR
IsEquippedRightHasKeyword("NewArmoury.esp" | 0x000801) AND
HasMagicEffect("EldenSkyrim.esp"|0x000801) AND
NOT IsEquippedLeftType(1) AND
NOT IsEquippedLeftType(2) AND ...
```
- 函式式 DSL，`AND`/`OR`/`NOT` 中綴，form ref＝`"plugin.esp" | 0xFormID`。OAR 會原樣讀入並轉成 "Legacy" replacer-mod 的 submod（見 [oar-replacer-guide](../oar-replacer-guide.md)）。

### B. OAR（現代，JSON）— 例：Holmgang - ADXP MCO Moveset for NPCs
```
meshes\actors\character\animations\OpenAnimationReplacer\Holmgang\
   config.json                     ← replacer-mod 層：{name, author, description}
   Attack - Sword & Shield\
      config.json                  ← submod：{name, description, priority, conditions[]}
      <被替換動畫>.hkx
   Idle - Dagger\ …                ← 每個 武器組合 × (Attack|Idle) 一個具名 submod
```
- root config 實檔：`{ "name":"Holmgang", "author":"…", "description":"NPC Moveset" }`（**無 priority/conditions**）。
- submod config 實檔（"Sword & Shield - [Attack]"）：
  ```json
  { "name":"…", "description":"…", "priority":100008,
    "conditions":[ { "condition":"AND", "requiredVersion":"1.0.0.0", "Conditions":[
      { "condition":"IsEquippedType", "requiredVersion":"1.0.0.0", "Type":{"value":1.0}, "Left hand":false },
      { "condition":"IsEquippedType", "requiredVersion":"1.0.0.0", "Type":{"value":11.0}, "Left hand":true },
      { "condition":"IsActorBase", "requiredVersion":"1.0.0.0", "negated":true,
        "Actor base":{"pluginName":"Skyrim.esm","formID":"7"} }
    ]}] }
  ```

## NPC moveset 的條件配方（實檔反覆出現 → 可模板化）
- **右手武器型 + 左手武器型** 限定組合：兩條 `IsEquippedType`（`Left hand` 分 false/true），`Type.value` 用武器型 enum（見下）。
- **排除玩家**：`IsActorBase` + `negated:true` + `Skyrim.esm|0x7`（PlayerRef base）→ 只給 NPC 用。這是「NPC-only moveset」的標準手法。
- **連段隨機分支**（Tweaked Conditions 版才加）：`Random`（`Random value:{min,max}` + `Comparison` + `Numeric value`）做機率選招，讓 NPC 連段有變化。
- **種族/陣營限定**（Tweaked 版）：`IsRace`（plugin|formID）等——把某 moveset 綁特定敵人。

### priority 疊加 + 零參數條件（例：NAMC Magic Casting OAR）
- **空條件 = 無條件套用**：base submod `"Magic"` 的 `"conditions": []`（priority 21）→ 套所有施法動畫；上面再疊 `"FemaleOnly"`（priority 22）只多一條零參數 `IsFemale`，蓋過 base 給女性專屬版。
- **模式**：「通用 base（空條件）+ 高 priority 特例（性別/種族/裝備條件）」是 OAR 最常見的疊法。零參數條件（`IsFemale`/`IsPlayerTeammate`…）只需 `{condition, requiredVersion}`。
- submod 也可**巢狀資料夾**（`NAMC SSE/Magic/…`）分組共用動畫。

### IsEquippedType 的 `Type.value`（OAR 標準 enum）
`0`=Fist/空手 `1`=單手劍 `2`=匕首 `3`=單手斧 `4`=單手錘 `5`=雙手劍 `6`=雙手斧/錘 `7`=弓 `8`=法杖 `9`=弩 `11`=盾 `12`=火把。（如「劍+盾」＝右 1、左 11；「匕首」idle＝右 2、左 0。）

## 「Tweaked Conditions」是什麼（實檔驗證）
- 一個**獨立小 mod**，路徑與原 moveset 完全相同（`OpenAnimationReplacer\Holmgang\…\config.json`），裝在原 moveset **之後覆蓋**它的 `config.json`——是**整檔替換、非 user.json**。
- 改動：在原條件外**加 `Random`（機率）＋ `IsRace`（種族限定）**，把通用 NPC moveset 收斂成「特定敵人才用、且連段隨機」。
- → 證實 OAR config 是**可被第三方逐檔覆寫的純資料**，與「ModForge 生成 + 玩家/patch 微調」模型完全契合。

## SCAR 那一半（不可文字檢視）
- `(SCAR)` moveset ＝ 在動畫 hkx 內**烘焙 SCAR 註釋** + 帶一個 `SCAR_*Dummy.hkx` 標記動畫；SCAR AI 據此讓 NPC 依距離/角度智能出招（見 [scar.md](scar.md)）。
- 註釋在 hkx 內、需 hkanno 才看得到 → **屬動畫管線、非 record/JSON 層，ModForge 不生成這半**。ModForge 能生的是 DAR/OAR 的條件 + 資料夾結構（A、B 兩佈局）。

## 對 ModForge — 結論
- **OAR 生成器的輸出規格現已具體**：root `{name,author,description}` + 每 submod `{name,description,priority,conditions[]}`，conditions 為巢狀 `AND/OR` 容器包 `IsEquippedType`/`IsActorBase`/`Random`/`IsRace`/`HasMagicEffect`… 全對映 ModForge 既有 CTDA。
- **可直接做的模板**：「給某 NPC/武器一套 N 段 moveset」→ 生 `IsEquippedType(右)+IsEquippedType(左)+IsActorBase¬player [+IsRace][+Random]` 的 submod 群。八向移動包則疊 [DMK](directional-movement-keys.md) 的 `DirecionalCycleMoveset` 條件。
- DAR `_conditions.txt` DSL 生成更簡單（純字串），但 OAR 是現代目標、且 condition 表達力更強——**生成器應以 OAR JSON 為主、DAR 為相容後路**。
- 已併入 roadmap 的 OAR 生成器項。
