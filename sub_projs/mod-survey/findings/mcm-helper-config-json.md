# config.json 格式結構（完整）+ settings.ini + 翻譯

← [mcm-helper](mcm-helper.md)

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
| `modName` | string | ⚠️ **不是目錄名**（2026-06-20 源碼修正）。目錄名 = 宿主插件檔名 stem（`FormUtil::GetModName` = `path(plugin).stem()`），與此欄位無關。此欄位實為「**required plugins**」，慣例設成插件 stem（自我前置，永遠滿足）。見 [mcm-helper-modforge](mcm-helper-modforge.md) 修正框 |
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

