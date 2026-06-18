# Step 3：寫 config.json（兩層逐欄）

← [oar-replacer-guide](oar-replacer-guide.md)

## 5. Step 3 — 寫 config.json（兩層逐欄）

### 5.1 replacer-mod 層 `config.json`

`MyIdleSwap\config.json`——只標示這個 mod：

```jsonc
{
  "name": "My Idle Swap",                  // 在遊戲內編輯器裡顯示的 mod 名
  "description": "Condition-based idle replacements."
}
```

（presets 也定義在這一層——見 §6 末。）

### 5.2 submod 層 `config.json`（核心）

`MyIdleSwap\PlayerKatanaIdle\config.json`——真正的邏輯：

```jsonc
{
  "name": "Player Katana Idle",            // 編輯器顯示的 submod 名
  "description": "Cool katana stance for the player when a katana is equipped.",
  "priority": 100,                         // 數字大者勝；和別的 submod 撞同一 clip 時用它仲裁
  "conditions": [                          // 全部通過才套用這個 submod（見 Step 4 詳解）
    {
      "condition": "IsActorBase",
      "requiredVersion": "1.0.0.0",
      "Actor base": "Skyrim.esm|0x000007"  // 只對玩家（Player actor base）
    },
    {
      "condition": "IsEquipped",
      "requiredVersion": "1.0.0.0",
      "negated": false,
      "Form": "Skyrim.esm|0x0001397E",     // 範例：某把武器；實務可改用 keyword 條件
      "Left hand": false
    }
  ]
}
```

**逐欄要點**：
- **`name` / `description`**：給遊戲內編輯器顯示用；**這兩欄 `user.json` 不能覆寫**（其餘都能）。
- **`priority`**：整數，高者勝。OAR 對同一被替換 clip，挑出**所有條件通過**的 submod 裡 priority 最高那個套用。撞優先序就調這個數字。
- **`conditions`**：陣列，**全部為真**才算這個 submod 命中（要「或」邏輯用 §6 的容器條件）。每條至少有 `condition`（條件名）與 `requiredVersion`；多數還有自己的參數欄位（如 `IsActorBase` 的 `Actor base`）。
- 出貨前**拿掉所有 `//` 註解**（OAR 讀的是嚴格 JSON）。

> 實務上不必手寫——遊戲內編輯器（Author 模式，§9）會幫你生成這份 JSON。但理解它的 schema 才能讓 ModForge 程式化產生（§10）。

---

