# 這個工具做什麼 + 工作原理

← [mcm-helper](mcm-helper.md)

## 一、這個工具做什麼 + 工作原理

MCM Helper（原名 MCM Helper）讓 mod 作者**不用寫 Papyrus MCM 腳本**，改用一份 `Data/MCM/Config/<modName>/config.json` 定義整個 Mod Config Menu。SKSE DLL 在 SkyUI MCM 開啟時讀取 JSON，動態建構 UI 頁面。

**工作流程**：

1. mod 作者在 `Data/MCM/Config/<modName>/` 放：
   - `config.json`：UI 結構（頁面、控件）
   - `settings.ini`（可選）：預設值，儲存在 `Data/MCM/Config/<modName>/settings.ini`
2. MCMHelper.dll hook SkyUI MCM 的初始化，讀取所有 mod 的 `config.json`，自動向 SkyUI 注入選單頁面。
3. 玩家修改設定時，DLL 寫入 `Data/MCM/Settings/<modName>.ini`（玩家自訂覆蓋，不會被 mod 更新覆蓋）。
4. 設定值透過 `GetModSettingInt/Float/Bool/String()` 在 Papyrus 中讀取，或直接綁定到 script property（`PropertyValueInt/Bool` sourceType）。

**對比傳統 SkyUI MCM**：傳統方式需繼承 `SKI_ConfigBase`、手動呼叫 `AddTextOption/AddSliderOption`，幾十行重複 boilerplate。MCM Helper 把這些全部換成一個 JSON，script 只需繼承 `MCM_ConfigBase`，甚至可以**零 Papyrus script**（純設定讀寫型的 MCM 不需要任何腳本）。

---

