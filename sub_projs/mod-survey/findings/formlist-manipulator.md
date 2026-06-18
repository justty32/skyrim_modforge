# Mod 調查：FormList Manipulator（FLM）v1.8.1

> 為 ModForge（JSON spec → `.esp` 產生器）做的可重用性調查。記錄型別 / key / 程式碼一律 English；散文繁中。
> 來源：`FormList Manipulator - FLM-74037-1-8-1-1727507011.zip`（Nexus 74037，作者 MaskedRPGFan）。
> 內含：`SKSE/Plugins/FormListManipulator.dll` + `.pdb`（純 SKSE DLL，無 ESP、無 ini 範例）。

---

## 內容拆分

- [做什麼 + 怎麼運作](formlist-manipulator-overview.md) — 解決的核心問題、運作流程
- [Config 核心語法](formlist-manipulator-config-core.md) — 檔案命名 / FormID 格式 / 主操作行 / Filter / Alias / Group
- [Config 進階語法](formlist-manipulator-config-advanced.md) — Collection / ModEvent / 快捷語法 / 完整範例 ini / Debug
- [對 ModForge + 分工](formlist-manipulator-modforge.md) — 可生成/需新支援/純參考、FLM vs ESP-side FLST、FLM vs KID
