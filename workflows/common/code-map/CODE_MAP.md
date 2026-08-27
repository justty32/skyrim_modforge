# CODE_MAP — ModForge 程式碼導航索引

**用途**：接到修改指令後，先讀本檔，找到對應子 index，再讀子 index 取得精確的源碼檔案清單，避免讀取無關檔案。

## 功能面向 → 子 index

| 功能面向 | 子 index | 常見指令關鍵字 |
|---------|---------|--------------|
| 對話・任務・場景・Story Manager・腳本 | [CODE_MAP.dialogue-quests.md](CODE_MAP.dialogue-quests.md) | quest, dialogue, scene, storyEvent, alias, script, word wall |
| 世界・放置・地區・等級列表・容器・遭遇區域 | [CODE_MAP.world.md](CODE_MAP.world.md) | placement, cell, worldspace, region, leveledItem, container, vendor |
| 物品・法術・附魔・特技・喊聲 | [CODE_MAP.items-magic.md](CODE_MAP.items-magic.md) | weapon, armor, spell, magic effect, enchantment, perk, shout, ingredient |
| NPC・派系・職業・AI 套件・戰鬥風格 | [CODE_MAP.npcs-packages.md](CODE_MAP.npcs-packages.md) | npc, faction, class, package, combatStyle, outfit, relationship |
| 基礎設施・CLI・驗證・打包・Papyrus・翻譯 | [CODE_MAP.infra.md](CODE_MAP.infra.md) | validate, build, package, compile, translate, extract, apply, plugin I/O |

## 資料夾佈局（2026-08-13 起）

`src/` 依領域分資料夾，**namespace 一律仍是 `ModForge`**（資料夾只影響檔案怎麼找，不影響 `using`）。找檔先看資料夾，再看本檔的子 index。

| 資料夾 | 檔數 | 內容 |
|--------|-----|------|
| `src/ModForge.Core/Spec/` | 52 | `Spec.cs` 根 DTO + 各領域 `Spec.*.cs` + `SpecRefs.cs`（`$ref`/`$env` 前處理）|
| `src/ModForge.Core/Build/` | 69 | `Generator.Build.cs`（兩段 pipeline 的呼叫順序）+ `Generator.Build.*.cs` + `BuildContext` |
| `src/ModForge.Core/Validate/` | 32 | `Generator.Validate.cs` + `Generator.Validate.*.cs`（`ValidateContext`）|
| `src/ModForge.Core/Macros/` | 6 | `ExpandMacros` 那一段：高階語法糖（settlements / livingNpcs / skillTrees / capturedNpcs / capturedItems / npcRoles）在讀 spec 前先展開成低階記錄 |
| `src/ModForge.Core/Papyrus/` | 19 | Papyrus 原始碼與腳本片段生成 + 外部框架設定檔 emitter（SPID / KID / FLM / BOS / OAR / SkyPatcher / MCM）|
| `src/ModForge.Core/Formats/` | 10 | 二進位／磁碟格式與 plugin I/O：`PluginIo` `SeqFile` `Archives` `Fuz` `Heightmap` `Splatmap` `Vtxt` `Vhgt` `Vnml` `NavmeshPatch` |
| `src/ModForge.Core/Catalog/` | 3 | 離線 SQLite/FTS5 記錄目錄 |
| `src/ModForge.Core/Voice/` | 3 | TTS / lip / fuz 工具鏈（build 期的語音**規劃**在 `Build/Generator.Build.Voice*.cs`）|
| `src/ModForge.Core/`（根）| 18 | `Generator.cs` 入口、`Generator.Helpers/Dependencies/Requires`、以及還沒歸類的獨立型別 |
| `src/ModForge.Cli/Commands/` | 11 | 各命令實作（`build` / `package` / `translate` / `catalog` / `texexport` …）|
| `src/ModForge.Cli/Diagnostics/` | 29 | `Diagnostics.*.cs`（dump / find / *diag）|
| `src/ModForge.Cli/`（根）| 2 | `Program.cs`（argv dispatch）+ `GlobalUsings.cs` |

`tests/ModForge.Core.Tests/` 用同一組資料夾名（`Build/` `Validate/` `Spec/` `Papyrus/` `Formats/` `Voice/` `Catalog/`）。

> 規則是**機械式的**：`Generator.Build.*` 一律進 `Build/`，`Generator.Validate.*` 一律進 `Validate/`。所以語音同時出現在 `Build/`（build 期規劃）與 `Voice/`（實際 TTS/lip 工具）——可預測性優先於主題完美歸類。

## 架構概覽

```
ModSpec（Spec/Spec.cs）
  └─ 各領域 Spec/Spec.*.cs（純資料 DTO）
        ↓ Macros/（ExpandMacros：語法糖 → 低階記錄）
        ↓ Build/Generator.Build.cs（兩段 pipeline）
        Pass 1：建 record                   → Build/Generator.Build.*.cs
        Pass 2：接 cross-record FormLink    → Build/Generator.Build.*.cs
        ↑ Build/Generator.BuildContext.cs（state）
  └─ Validate/Generator.Validate.cs + Validate.*.cs（語義驗證）
CLI（Program.cs）
  ├─ Commands/Program.Build.cs → Generator → Formats/PluginIo
  ├─ Commands/Program.Translate.cs → Translator
  ├─ Commands/Package.cs → Papyrus + Assets + PluginIo
  └─ Diagnostics/*.cs（dump / find / smtree）
```

## 整體資料流

1. JSON spec → `Spec/Spec.cs` + `Spec/Spec.*.cs`（反序列化）
2. `Validate/Generator.Validate.*` → 語義/ref 合法性檢查
3. `Build/Generator.Build.cs` → pass 1 建所有 record，pass 2 接所有 FormLink
4. `Formats/PluginIo` → 寫 `.esp`；`Formats/SeqFile` → 寫 `.seq`
5. `Commands/Package.cs` → 編 Papyrus + 複製 Assets → MO2 資料夾

## 常用起點（讀此檔後直接跳）

| 任務 | 直接讀 |
|-----|-------|
| 新增 SM 事件種類 | `src/ModForge.Core/StoryManagerEvents.cs` |
| 修改 SM build 邏輯 | `src/ModForge.Core/Build/Generator.Build.StoryManager.cs` |
| 修改對話 build | `src/ModForge.Core/Build/Generator.Build.Dialogue.cs`（玩家 topic）／`Generator.Build.Dialogue.Hello.cs`（招呼）|
| 修改條件邏輯 | `src/ModForge.Core/Build/Generator.Build.Conditions.cs` |
| 修改驗證規則 | `src/ModForge.Core/Validate/Generator.Validate.Quests.cs` / `Generator.Validate.StoryManager.cs` |
| 修改 CLI 命令 | `src/ModForge.Cli/Program.cs` / `Program.Build.cs` |
| 修改 dump 輸出 | `src/ModForge.Cli/Diagnostics.*.cs`（對應領域） |
| 新增 Spec 欄位 | `src/ModForge.Core/Spec/Spec.Dialogue.cs` 或對應 `Spec.*.cs` |
