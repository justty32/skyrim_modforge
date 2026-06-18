# Mod Survey — AnimObject Swapper (Nexus 75167, v1.1.0)

> ModForge 取向：把這個框架拆成「config 格式全集」+ 「ModForge 可生成 ini 輸出 / 需新支援 / 純參考」。
> 分析對象：`AnimObject Swapper-75167-1-1-0-1666410165.7z`（SKSE DLL + FOMOD）+ 原始碼（GitHub powerof3/AnimObjectSwapper）。
> 注意：AnimObject Swapper 本身不包含任何 `_ANIO.ini` 範例；真實 consumer mod ini 未能在本機 unzip 目錄找到，以下格式說明從原始碼逆向推導。

## 內容拆分

- [做什麼 + config 語法全集](animobject-swapper-overview-config.md) — 命名/Section/Entry/FormID/Filter/Traits/隨機/條件區分
- [條件語法詳解 + 真實範例](animobject-swapper-conditions-examples.md) — 評估流程、Filter 查找、逆向格式示意
- [對 ModForge 的參考價值](animobject-swapper-modforge.md) — 純參考、潛力支援點、OAR+AOS 搭配、小結

## 參考來源

- GitHub: [powerof3/AnimObjectSwapper](https://github.com/powerof3/AnimObjectSwapper)（原始碼：Manager.cpp, Manager.h, LookupFilters.cpp）
- Nexus: [AnimObject Swapper - Nexus 75167](https://www.nexusmods.com/skyrimspecialedition/mods/75167)
