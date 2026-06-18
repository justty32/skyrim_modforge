# Mod Survey — Base Object Swapper (Nexus 60805, v3.4.1)

> ModForge 取向：把這個框架拆成「config 格式全集」+ 「ModForge 可生成 ini 輸出 / 需新支援 / 純參考」。
> 分析對象：`Base Object Swapper-60805-3-4-1-1752606013.7z`（SKSE DLL + FOMOD）+ 原始碼（GitHub powerof3/BaseObjectSwapper）+ `Dynamic Things Alternative - Base Object Swapper-60741-0-5-1777404773.7z`（consumer mod，抽取真實 ini 範例）。

## 內容拆分

- [做什麼 + 工作原理](base-object-swapper-overview.md)
- [config 格式語法全集](base-object-swapper-config.md) — 命名/Section 種類/Forms/Properties/Transforms/References/FormID/Chance/條件過濾
- [條件 + 範例 + ModForge](base-object-swapper-conditions-examples-modforge.md) — 複合條件評估、真實 ini、可生成/需新支援/純參考、小結

## 參考來源

- GitHub: [powerof3/BaseObjectSwapper](https://github.com/powerof3/BaseObjectSwapper)（原始碼：SwapData.cpp, ObjectProperties.cpp, ConditionalData.cpp, Manager.cpp）
- Nexus: [Base Object Swapper - Nexus 60805](https://www.nexusmods.com/skyrimspecialedition/mods/60805)
- Consumer mod 範例: Dynamic Things Alternative - Base Object Swapper（Nexus 60741, v0.5）
