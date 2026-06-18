# 對 ModForge 的參考價值

← [mcm-helper](mcm-helper.md)

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
