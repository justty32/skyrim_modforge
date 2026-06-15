# MCM Helper Finding

**版本**：1.6.1  
**作者**：Parapets  
**Nexus**：https://www.nexusmods.com/skyrimspecialedition/mods/53000  
**檔案**：`SKSE/Plugins/MCMHelper.dll` + `MCMHelper.esp`（ESL flagged）  
**前置**：SKSE64、SkyUI（MCM 框架本體）  
**Papyrus API**：`MCM.pex`、`MCM_ConfigBase.pex`

---

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

## 二、config.json 格式結構（完整說明）

### 頂層結構

```json
{
  "modName": "MyMod",
  "displayName": "My Mod Name",
  "customContent": { "source": "path/to/splash.swf" },
  "pages": [ ... ]
}
```

| 欄位 | 型別 | 說明 |
|------|------|------|
| `modName` | string | **關鍵**：對應 `Data/MCM/Config/<modName>/` 目錄名，也是 MCM 識別 mod 的 key |
| `displayName` | string | MCM 左側 mod 列表顯示的名稱，可用 `$Key` 做翻譯 |
| `customContent` | object（可選） | 自訂 SWF 畫面（如 splash logo），`source` = SWF 路徑 |
| `pages` | array | 頁面陣列，每個元素是一個頁面 |

### page 結構

```json
{
  "pageDisplayName": "$General",
  "cursorFillMode": "topToBottom",
  "content": [ ... ]
}
```

| 欄位 | 說明 |
|------|------|
| `pageDisplayName` | 頁籤名稱（支援 `$` 翻譯 key） |
| `cursorFillMode` | `"topToBottom"`（由上往下填）或 `"leftToRight"`（雙欄左往右） |
| `content` | 控件陣列 |

### content 控件型別一覽

所有控件共有欄位：

| 欄位 | 說明 |
|------|------|
| `id` | **設定識別碼**，格式 `"key:Section"`（對應 ini 的 `[Section]\nkey=`），若有 `sourceType` 需填 |
| `text` | 顯示文字（支援 `$` 翻譯 key 和 `{value}` 插值） |
| `help` | 游標懸停說明文字 |
| `type` | 控件類型（見下表） |
| `valueOptions` | 數值選項物件 |
| `action` | 值變更時的額外動作 |
| `groupControl` | 整數 ID，標記此控件為群組開關 |
| `groupCondition` | 整數 ID 或 `{"NOT": id}`，條件控制顯示/隱藏 |
| `groupBehavior` | `"disable"`（灰化）或 `"skip"`（隱藏） |
| `position` | 雙欄模式下強制指定欄位（0=左, 1=右） |

**控件型別**：

| type | 說明 | 典型用途 |
|------|------|---------|
| `"toggle"` | 勾選方塊（bool） | 開關選項 |
| `"hiddenToggle"` | 隱藏的 toggle，僅用於 groupControl 判斷條件 | 偵測 gamepad/鍵盤切換佈局 |
| `"slider"` | 拉桿（int 或 float），需設 min/max/step | 數值調整 |
| `"stepper"` | 左右步進（int index → options 陣列） | 小列舉選項 |
| `"enum"` | 下拉選單（int index → options 陣列），有 `shortNames` | 多選項 |
| `"keymap"` | 按鍵綁定（int keycode），有衝突提示 | 熱鍵設定 |
| `"header"` | 分組標題（無 `id`，無值） | 視覺分隔 |
| `"empty"` | 空白佔位 | 對齊版面 |

### valueOptions 欄位

```json
"valueOptions": {
  "sourceType": "ModSettingInt",
  "min": 0,
  "max": 100,
  "step": 1,
  "formatString": "{0} s",
  "options": ["$Small", "$Medium", "$Large"],
  "shortNames": ["S", "M", "L"],
  "defaultValue": false,
  "propertyName": "MyProperty"
}
```

| 欄位 | 說明 |
|------|------|
| `sourceType` | 資料來源（見下表，**最關鍵欄位**） |
| `min/max/step` | slider 的範圍與步進 |
| `formatString` | slider 顯示格式，`{0}`=整數, `{1}`=小數 |
| `options` | stepper/enum 的文字選項陣列 |
| `shortNames` | enum 的縮短顯示名 |
| `defaultValue` | 找不到儲存值時的預設（通常在 settings.ini 裡設） |
| `propertyName` | `PropertyValueBool/Int/Float` 時對應的 script property 名稱 |

**sourceType 類型**：

| sourceType | 讀寫目標 | 說明 |
|------------|---------|------|
| `ModSettingBool` | ini `[Section]\nkey=0/1` | 最常用，持久化到 MCM/Settings/ |
| `ModSettingInt` | ini `[Section]\nkey=整數` | 整數 |
| `ModSettingFloat` | ini `[Section]\nkey=小數` | 浮點 |
| `ModSettingString` | ini `[Section]\nkey=字串` | 字串（少用） |
| `PropertyValueBool` | script property（bool） | 寫到 Quest 腳本的 property，不持久化 ini |
| `PropertyValueInt` | script property（int） | 同上 |
| `PropertyValueFloat` | script property（float） | 同上 |

### action 欄位

```json
"action": {
  "type": "CallFunction",
  "function": "MyFunction",
  "params": ["{value}"]
}
```

值變更時呼叫掛載在 Quest 腳本上的 function。`{value}` 替換為當前值。這是 MCM Helper 中**唯一需要 Papyrus 腳本**的部分——若控件有副作用（如套用顯示設定），才需要 `action`；純存 ini 不需要。

### settings.ini 格式

```ini
; 這是 mod 預設值，儲存在 Data/MCM/Config/<modName>/settings.ini
; 玩家修改存到 Data/MCM/Settings/<modName>.ini（覆蓋預設，不被 mod 更新洗掉）

[Section]
key=value
```

INI key 命名規則：`config.json` 的 `id` 欄位格式為 `"key:Section"`，對應 ini 的 `[Section]` 下的 `key`。

### 翻譯支援

`Data/Interface/Translations/MCMHelper_LANGUAGE.txt` 提供 MCM Helper 自身的 UI 字串（目前只有 keymap 衝突提示）。各 mod 的翻譯放在 `Data/Interface/Translations/<modName>_LANGUAGE.txt`，`$Key` 格式參照。

---

## 三、對 ModForge 的參考價值

### MCM Helper 要求 esp 提供什麼 Record

**最小需求**：

1. **Quest record**（QUST）：掛載 Papyrus script，script 繼承 `MCM_ConfigBase`（若有 `action.CallFunction` 或 `PropertyValue*` sourceType）。Quest 需設為 Start Game Enabled。
2. **`modName` 對應的目錄結構**：`Data/MCM/Config/<modName>/config.json`（必須）+ `settings.ini`（可選預設值）。
3. **`MCMHelper.esp` 為前置**（ESL，幾乎零 form 佔用）。

**若只有 ModSettingInt/Bool/Float（純 ini 存讀）**：甚至不需要 Quest + Papyrus script，MCM Helper DLL 全自動處理。但實務上大多數 mod 都會有一個 Quest 作為 script host。

### ModForge 可生成性分析

**高可生成性**（從 spec 直接輸出，幾乎零推斷）：

- `config.json` 本身就是 JSON，ModForge spec 可 1:1 映射成 config.json 格式，或定義一個更高階的 `mcm:` section 讓生成器展開。
- `settings.ini`：純 ini，字串生成，無難度。
- Quest record（`QUST`）：ModForge 已能生成 Quest，加 `MCM_ConfigBase` script attach 即可。

**需新支援（推斷）**：

- **MCM spec section**：ModForge 目前無「設定選單」相關的 spec 欄位。需在 `mod_spec` 加 `mcm:` key，定義頁面、控件、sourceType 對應。
- **config.json 生成器**：按 MCM Helper schema 序列化 JSON 到 `Data/MCM/Config/<modName>/config.json`。
- **settings.ini 生成器**：從 spec 的預設值序列化 ini。
- **Quest 命名規範**：MCM_ConfigBase 掛載的 Quest 需有一致的命名（如 `<modName>_MCMQuest`）。

**設計建議**（若 ModForge 支援 MCM Helper）：

```yaml
# 假想的 ModForge spec 片段
mcm:
  displayName: "My Mod"
  pages:
    - name: "General"
      content:
        - type: toggle
          id: "bEnableFeature:General"
          text: "Enable Feature"
          default: true
        - type: slider
          id: "fMultiplier:General"
          text: "Multiplier"
          min: 0.5
          max: 3.0
          step: 0.1
          default: 1.0
```

這個 spec 生成器需輸出：
1. `Data/MCM/Config/<modName>/config.json`
2. `Data/MCM/Config/<modName>/settings.ini`（含預設值）
3. QUST record（若有 `action` 或 `PropertyValue*`）
4. script（繼承 `MCM_ConfigBase`，實作 action handler function）

**純參考**：

- `customContent`（自訂 SWF splash）：超出 ModForge 生成範圍，需美術資源，不生成。
- `keymap` 型控件的衝突處理邏輯：MCM Helper DLL 自動處理，ModForge 只需在 JSON 標 `"ignoreConflicts": true/false`。

### 部署結構

MCM Helper config 的部署目錄結構（需一併打包）：

```
Data/
  MCM/
    Config/
      <modName>/
        config.json      ← 必須
        settings.ini     ← 建議（預設值）
    Settings/
      readme.txt         ← 玩家自訂覆蓋（MCM Helper 自動管理，mod 不應打包此處）
```

與 `esp` 一起發布即可，不需要 esp 內有額外的 record 指向這個目錄（MCM Helper DLL 掃描固定路徑）。

### 結論

MCM Helper 是 ModForge 最值得優先支援的框架型工具之一：
- JSON-driven，和 ModForge 的 spec-to-record 思路高度一致。
- 「設定選單」是 ModForge 生成的 mod 的自然需求（讓玩家調整行為、開關功能）。
- 最小 esp 需求只有一個 Quest（若需 action），或甚至零 record（純 ModSetting 存讀）。
- 生成難度低：JSON + ini 格式化，無二進位操作。

> ⚠️ 以上「需新支援」欄位為推斷，未查 ModForge src/，可能有誤判。
