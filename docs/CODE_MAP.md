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

## 架構概覽

```
ModSpec（Spec.cs）
  └─ 各領域 Spec.*.cs（純資料 DTO）
        ↓ Generator.Build.cs（兩段 pipeline）
        Pass 1：建 record                   → Generator.Build.*.cs
        Pass 2：接 cross-record FormLink    → Generator.Build.*.cs
        ↑ Generator.BuildContext.cs（state）
  └─ Generator.Validate.cs / Validate.*.cs（語義驗證）
CLI（Program.cs）
  ├─ Program.Build.cs → Generator → PluginIo
  ├─ Program.Translate.cs → Translator
  ├─ Package.cs → Papyrus + Assets + PluginIo
  └─ Diagnostics.*.cs（dump / find / smtree）
```

## 整體資料流

1. JSON spec → `Spec.cs` + `Spec.*.cs`（反序列化）
2. `Generator.Validate.*` → 語義/ref 合法性檢查
3. `Generator.Build.cs` → pass 1 建所有 record，pass 2 接所有 FormLink
4. `PluginIo` → 寫 `.esp`；`SeqFile` → 寫 `.seq`
5. `Package.cs` → 編 Papyrus + 複製 Assets → MO2 資料夾

## 常用起點（讀此檔後直接跳）

| 任務 | 直接讀 |
|-----|-------|
| 新增 SM 事件種類 | `src/ModForge.Core/StoryManagerEvents.cs` |
| 修改 SM build 邏輯 | `src/ModForge.Core/Generator.Build.StoryManager.cs` |
| 修改對話 build | `src/ModForge.Core/Generator.Build.Dialogue.cs` |
| 修改條件邏輯 | `src/ModForge.Core/Generator.Build.Conditions.cs` |
| 修改驗證規則 | `src/ModForge.Core/Generator.Validate.Quests.cs` / `Validate.StoryManager.cs` |
| 修改 CLI 命令 | `src/ModForge.Cli/Program.cs` / `Program.Build.cs` |
| 修改 dump 輸出 | `src/ModForge.Cli/Diagnostics.*.cs`（對應領域） |
| 新增 Spec 欄位 | `src/ModForge.Core/Spec.Dialogue.cs` 或對應 `Spec.*.cs` |
