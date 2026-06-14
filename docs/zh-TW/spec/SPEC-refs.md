# ModForge spec — `$ref` / `$env` includes & parameterization

← [index](SPEC-index.md)

兩個前處理指令會在 spec 被反序列化**之前**解析，因此它們可以出現在 spec 中*任何*值的位置，且永遠不會傳到記錄建構器：

- **`$ref`** — 從另一個檔案、另一個檔案的子節點，或同一份文件的子節點接合 JSON 進來。這就是**具名預設庫**的運作方式：把可重用的片段放在它們自己的檔案裡，再拉進來。
- **`$env`** — 替換成某個環境變數的值，可附帶一個可選的預設值。

> 這些是 **spec 資料**中的 ModForge 指令。它們與 `spec.schema.json` 內部使用的
> `$ref` 無關（那是 JSON-Schema 自己的關鍵字，由 schema 驗證器解讀，
> 而非 ModForge）。

解析會在每個 `validate` / `build` / `package` / `voicelines` /
`voicediag`（任何會讀取 spec 的指令）的最開頭執行。未知欄位檢查是在**解析後**的 JSON 上進行，
所以 `$ref` 進來的檔案裡的錯字仍會被抓到。

## `$ref`

當一個節點是含有 `$ref` 鍵的物件時，它就是一個 ref 節點。其值有三種形式。

### String — 單一來源

```json
{ "$ref": "presets/bright-interior.json" }          // whole file
{ "$ref": "presets/bright-interior.json#/lgtm" }    // a sub-node of a file (JSON Pointer)
{ "$ref": "#/presets/lighting/brightInterior" }     // a sub-node of the SAME document
```

檔案路徑相對於**引用文件本身的**目錄（所以某個預設檔自己的
`$ref` 是相對於那個預設檔解析，而非頂層 spec）。`#` 之後的部分是
RFC 6901 JSON Pointer（`/a/b/0`，其中 `~1`→`/`、`~0`→`~`）。

### Array — 鏈式深度合併，後者勝出

```json
{ "$ref": [ "presets/base.json", "presets/warm.json", "presets/local.json" ] }
```

每個來源先各自解析，然後由左至右深度合併：後面的來源覆蓋前面的。
用它來疊一個基底預設 + 一個變體 + 在地微調。

### Object — 明確的長形式（讓 `$env` 驅動路徑）

```json
{ "$ref": { "from": { "$env": "MF_PRESET_FILE", "default": "presets/bright-interior.json" },
            "pointer": "/lgtm" } }
```

`from` 是檔案路徑（它本身也可解析，所以可以是 `$env`）；`pointer` 是進入該檔案的 JSON Pointer。
這是 string 形式的超集——只有在你需要 env 驅動的路徑時才動用它。`merge` 保留給未來的 per-ref 開關。
任何其他鍵都是錯誤。

### Sibling override — sibling 勝出

緊鄰 `$ref` 的鍵會深度合併**疊在** ref 結果之上：

```json
{ "$ref": "presets/bright-interior.json#/lgtm", "fogFar": 12000 }
```

→ 該預設的 LGTM，其 `fogFar` 被覆蓋為 12000。合併規則：**物件遞迴合併，
陣列整個取代。**（鏈式陣列 `$ref` 的「後者勝出」是另一個維度——它合併的是 ref *來源*；
值內部的資料陣列仍是取代，從不串接。）

## `$env`

當一個節點是含有 `$env` 鍵的物件時，它就是一個 env 節點。

```json
{ "$env": "MF_PRESET_DIR" }                          // value required; error if unset
{ "$env": "MF_PRESET_DIR", "default": "presets" }    // value if set, else default
```

env 值會以 JSON **字串**插入；CLI 以
`NumberHandling.AllowReadingFromString` 反序列化，所以字串放進數值型 spec 欄位也沒問題。
`default` 則原樣插入（任何 JSON 型別）。一個未設定且**沒有**預設值的變數是硬性錯誤——
`$env` 從不靜默地產生空值。

## Errors

以下全部都會帶著清楚的 `SpecRefException` 中止執行：

- `$ref` 檔案找不到 / pointer 找不到。
- 一個 `$ref` 循環（`a → b → a`）。
- 一個節點**同時**含有 `$ref` 與 `$env`。
- 長形式 `$ref` 帶有未知的鍵，或 `from` 不是字串。
- `$env` 未設定且沒有 `default`。

## 具名預設庫

把可重用的片段放在例如 `examples/presets/` 底下的檔案中，再用 `$ref` 引用它們。隨附的
範例 `examples/presets/bright-interior.json` 收錄了一組 bright-interior `lgtm` + `imgs`；
`examples/spec-refs-demo.json` 把兩者都拉進來並掛到一個 cell 上。Spec 也可以把
片段保存在它自己不會輸出的 `presets` 物件裡（見
[cookbook-presets](../lifelike/cookbook-presets.md)），並以 `#/presets/...` 用 `$ref` 引用。
