# 動作系統 asset/config 生成設計（OAR / BDI / PIE）

> 日期:2026-06-14 · 狀態:**草案（draft），待自審 → plan**
> 調查依據:[mod-survey/action-system/](../../sub_projs/mod-survey/action-system/)（五層堆疊 + 實檔驗證的 schema）。OAR 實作指南 §10 有更細的欄位雛形:[oar-replacer-guide.md](../../sub_projs/mod-survey/action-system/oar-replacer-guide.md)。
> roadmap 對應項:OAR 生成、BDI config 生成（皆在 [roadmap](../roadmap.md)）。
> 本文件只是設計成果,**不含實作**。

## 為什麼要

2026 的動作/戰鬥系統（movesets、條件式動畫、招式派生）已經把「整合層」從**改不得的 Havok behavior binary** 降為**擺檔案 + 生 JSON/ini**——這正是 ModForge 的主場。整套交付物裡，**除 `.hkx` 動畫本體外全是確定性可生成的文字**（見 action-system README 的「動畫驅動狀態鐵三角」）。

這份 spec 把這條線上**三個可生成產物**收進 ModForge 的一個新能力區（**非-esp 的 asset/config 生成**）：

1. **OAR replacer/moveset**（最高槓桿、主交付）— 條件式動畫替換的資料夾樹 + config.json。
2. **BDI config**（companion）— 免 behavior patch 注入 graph variable/event。
3. **PIE `.ini` 巨集表**（companion）— 命名巨集 → payload 指令。

三者構成一條鏈:**BDI 宣告變數 → 動畫 annotation 設值（hkx 內，不在範圍）→ OAR 讀變數選動畫**;PIE 巨集表是招式效果（iframe 等）的旁支。

## 範圍邊界

- **在範圍（ModForge 確定性產出）**:OAR 資料夾樹 + 兩層 config.json、BDI config JSON、PIE `.ini` 巨集表、把既有 condition 模型序列化成 OAR/DAR 條件形狀、把使用者提供的 `.hkx` 擺進重建的 vanilla 路徑。
- **不在範圍**:
  - **`.hkx` 動畫本體** — 屬 Havok/Blender 管線（[havok-blender.md](../idea/asset-pipelines/animation/havok-blender.md)）;本功能只「擺檔」。
  - **hkx 內 annotation**（`PIE.@SGVF|…`、`animmotion`）— 屬 hkanno 動畫管線。
  - **SCAR AI 決策層、behavior graph 本體** — 由 Pandora（另一 spike）/SKSE DLL 處理。
- **前置（玩家端執行時依賴，spec 只標、不生成）**:OAR + Address Library + Animation Queue Fix;BDI/PIE 各自的 DLL;moveset 另需 Pandora 跑一次建 base behavior。

## 核心模型:三個子生成器（schema 皆已實檔驗證）

### A. OAR replacer/moveset 生成器（主）

兩層結構（實檔:Holmgang / NAMC / BFCO）:
- **replacer-mod 層** `config.json` = `{name, author, description}`（**無 priority/conditions**）。
- **submod 層** `config.json` = `{name, description, priority, conditions[]}`;`.hkx` 與被替換 vanilla 同名,落在 submod 內重建的 vanilla 相對路徑。

spec 片段（建構在 guide §10 之上,擴充 moveset 語法糖）:
```jsonc
{
  "animationReplacer": {
    "mod": { "name": "Sofia Katana Moveset", "author": "ModForge", "description": "..." },
    "submods": [
      {
        "name": "Sword & Shield - Attack",
        "priority": 100008,
        "replaces": "actors/character/animations/...",   // 被替換的 vanilla/MCO 相對路徑
        "hkx": ["build/anims/ss_atk1.hkx", "..."],         // 你提供的成品（Havok 管線產）
        "conditions": [                                     // 對映既有 CTDA 模型 → 序列化成 OAR 形狀
          { "all": [
            { "condition": "IsEquippedType", "type": 1, "leftHand": false },
            { "condition": "IsEquippedType", "type": 11, "leftHand": true },
            { "condition": "IsActorBase", "form": "Skyrim.esm|0x000007", "negated": true }
          ]}
        ]
      }
    ]
  }
}
```

**moveset 語法糖（實檔反覆出現的配方 → 一行展開成上面的條件束）**:
```jsonc
{ "npcMoveset": {
    "rightWeapon": "sword", "leftWeapon": "shield",   // → 兩條 IsEquippedType（enum 見 findings）
    "playerOnly": false,                               // false → IsActorBase ¬player(Skyrim.esm|0x7)
    "race": "Skyrim.esm|0x13749",                      // 選用 → IsRace
    "randomPick": 0.4                                  // 選用 → Random < 0.4（連段機率分支）
}}
```
- `rightWeapon`/`leftWeapon` 字串 → `IsEquippedType` 的 `Type.value`（findings 的 enum 表:劍1/匕首2/斧3/錘4/雙手劍5/雙手斧6/盾11…）。
- 空條件 submod = `"conditions": []`（NAMC 的 base 層,無條件套所有人）;零參數條件如 `IsFemale` 只需 `{condition}`。
- **DAR 後路**:同一內部模型可改 emit `_CustomConditions/<priority>/_conditions.txt` DSL（`IsEquippedRight("esp"|0xID) AND NOT IsEquippedLeftType(N)`）+ hkx,給 legacy 用。OAR 為主、DAR 為相容輸出。

### B. BDI config 生成器（companion）

flat JSON array,放 `SKSE/Plugins/BehaviorDataInjector/<x>_BDI.json`（schema 實檔驗證,見 [BDI finding](../../sub_projs/mod-survey/action-system/findings/behavior-data-injector.md)）:
```jsonc
{ "behaviorData": {
    "file": "MyMod_BDI",
    "entries": [
      { "projectPath": "Actors", "type": "kInt",   "name": "MF_Combat", "value": 0 },
      { "projectPath": "Actors", "type": "kBool",  "name": "MF_Sworn",  "value": false },
      { "projectPath": "Actors", "type": "kEvent", "name": "MF_OnVow" }     // event 省 value
}]}
```
→ 直接序列化成 `[{projectPath,type,name,value}]`。`type` ∈ `kInt|kBool|kFloat|kEvent`。**實作幾近零風險**(格式固定)。生出的變數可被 A 的 OAR 條件以 graph-variable 比較引用。

### C. PIE `.ini` 巨集表生成器（companion，小）

`SKSE/PayloadInterpreter/Config/<x>.ini`（實檔:Stormcloaks VikingAxe.ini）:
```ini
[Intensify]
$enableIframe = @SETGHOST|1
$disableIframe = @SETGHOST|0
```
spec → ini 序列化:`{section, macros:[{name, command}]}`。命名巨集映射到 payload 指令,動畫 annotation 引用 `$name`。屬招式效果（dodge iframe 等）的旁支,優先序最低。

## build 展開規則

- **A**:`npcMoveset` 糖 → 展開成 `conditions[]` 條件束 → 序列化成 OAR JSON;`conditions` 走**既有 CTDA condition 模型**,只多一個「emit 成 OAR `{condition, requiredVersion, negated, 巢狀 all/any}` 形狀」的序列化器。資料夾樹 + `.hkx` 擺位是純路徑操作。
- **B/C**:純資料序列化,無條件邏輯。
- **跨子生成器**:A 的 OAR 條件可引用 B 注入的 graph variable（如 `MF_Combat == 1`）;ModForge 應在同一 spec 內讓三者共享 form/變數命名。

## MVP / 之後

- **MVP**:
  - **A** 的 `animationReplacer`（root + submods + `replaces`/`hkx`/`conditions`）+ `IsActorBase`/`IsEquippedType`/`IsFemale`/空條件 + `npcMoveset` 糖（rightWeapon/leftWeapon/playerOnly）。一個「給某 NPC 一套單手劍 moveset」的 showcase。
  - **B** BDI config 全功能（格式簡單,一次做完）。
  - **CTDA → OAR condition 序列化器**(A 的核心)。
- **之後**:
  - A 的 `variants`/`presets`/`functions`/進階 submod flag（interruptible 等）;`Random`/`IsRace` 糖;DAR `_conditions.txt` 後路輸出。
  - C 的 PIE 巨集表（招式效果線開始做時才需要）。
  - 接 hkanno 工具鏈後:生成 hkx 內 annotation（`PIE.@SGVI|…`、`animmotion`）——屆時與動畫管線交界。
  - 與 Pandora shell-out（[pandora.md](../../sub_projs/mod-survey/action-system/pandora.md)）串成「生 config → 跑 behavior 基底」完整出貨。

## 待解（自審時收斂）

- **condition 序列化器的型別覆蓋**:OAR condition 的值可為 static/global/AV/graph-variable 四型 + 巢狀 AND/OR 容器。MVP 先覆蓋 moveset 實際用到的（IsEquippedType/IsActorBase/IsFemale/IsRace/Random/CompareValues-graphVar），其餘漸進補。需先盤點 ModForge 既有 CTDA 模型能直接對映多少。
- **`.hkx` 交界**:本 generator 只擺檔,但需定義「使用者怎麼把成品 hkx 餵進來」（路徑?build 產物引用?）——與 havok-blender 管線的介面待定。
- **DAR vs OAR 輸出**:是否值得維護雙輸出,或 OAR-only(legacy 用戶自行轉)。傾向 OAR-only MVP,DAR 列「之後」。
- **三子生成器是否同一 spec 區塊**:目前設計讓 `animationReplacer`/`behaviorData`/`payloadMacros` 三個 top-level key 共存於一個 mod spec,共享 form 命名。自審時確認這個邊界。
