# Step 4：條件系統

← [oar-replacer-guide](oar-replacer-guide.md)

## 6. Step 4 — 條件系統

OAR 的條件 = DAR 全部條件 + 許多新增。這是它真正的威力所在。

**每條條件的通用欄位**：
- **`condition`**：條件名（如 `IsActorBase`、`Random`、`IsEquippedType`、`CompareValues`…）。
- **`requiredVersion`**：此條件起作用的最低 OAR 版本（編輯器會填）。
- **`negated`**（可選，預設 false）：反轉這條的真假。
- 部分條件還**需要另一個 plugin** 才可用（編輯器會標示）。

**數值比較值可以是四種型別之一**（這點是 OAR 的關鍵彈性）：
- **static value**：寫死的常數。
- **global variable**：引用一個 GLOB（`Plugin.esp|FormID`）。
- **Actor Value**：某個 AV（如 health、某 skill）。
- **behavior-graph variable**：behavior graph 裡的變數。

**Keyword 用 EditorID 指定**（如 `WeaponKatana`），不必查 FormID。

**容器條件可無限巢狀**：用 **OR** / **AND** 容器把子條件包起來，組出任意布林邏輯。例如「(玩家 AND 裝katana) OR (是某 faction 成員)」。

**代表性條件**（完整清單在編輯器 tooltip，這裡列類別）：
- 身分／陣營：`IsActorBase("Plugin.esm", 0xFormID)`、`IsInFaction`、`IsRace`…
- 裝備／物品：`IsEquipped`、`IsEquippedType`、`IsWornHasKeyword`（keyword 用 EditorID）…
- 數值比較：把 AV / global / graph variable / static 互比（大於、等於…）。
- 隨機：`Random`（配 variants 做隨機變體）。
- 巢狀：`OR` / `AND` 容器。
- **`PRESET`**：引用 replacer-mod config 裡定義好的條件區塊（見下）。

**PRESET（2.2.0+，去重利器）**：在 **replacer-mod 層 config.json** 定義可重用的條件區塊，submod 用一條特殊 `PRESET` 條件引用它（submod 只存 preset 名，內容住在 replacer-mod config）。多個 submod 共用同一組複雜條件時，改一處即可。

```jsonc
// replacer-mod 層 config.json 裡：
{
  "name": "My Idle Swap",
  "description": "...",
  "presets": [
    {
      "name": "PlayerOnly",
      "conditions": [
        { "condition": "IsActorBase", "requiredVersion": "1.0.0.0",
          "Actor base": "Skyrim.esm|0x000007" }
      ]
    }
  ]
}
// submod 層 config.json 裡，用 PRESET 引用：
// "conditions": [ { "condition": "PRESET", "preset": "PlayerOnly" }, ... ]
```

---

