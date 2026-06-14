# Recipe cookbook — presets

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

`presets` 是一份不會 emit 的具名 copy-paste 食譜目錄。builder 會忽略它；要建立
記錄，請把 preset 展開成一般的頂層陣列（`lightingTemplates`、`imageSpaces`、
`weathers`、`climates`、`packages`、`books`、`identities` 等）。

完整範例：[`examples/presets-cookbook.json`](../../../examples/presets-cookbook.json)。它同時包含
目錄與已展開的具體記錄，讓 `validate`/`build` 能實際演練這些食譜。

## 用 `$ref` / `$env` 把預設片段拉進來

除了 copy-paste 之外，預設片段也可以放在自己的檔案裡、用 `$ref` 拉進來——完整參考見
[SPEC-refs](../spec/SPEC-refs.md)。內附的
[`examples/presets/bright-interior.json`](../../../examples/presets/bright-interior.json) 放了一組
現成的 `lgtm` + `imgs`；[`examples/spec-refs-demo.json`](../../../examples/spec-refs-demo.json)
把兩者都拉了進來：

```json
"lightingTemplates": [ { "$ref": "presets/bright-interior.json#/lgtm" } ],
"imageSpaces":       [ { "$ref": "presets/bright-interior.json#/imgs" } ]
```

`$ref` 旁邊的 key 會覆寫 preset（`{ "$ref": "…#/lgtm", "fogFar": 12000 }`）；array 形態的
`$ref` 會疊加數個 preset（後者勝出）；而 `$env` 用來參數化路徑或值
（`{ "$env": "MF_PRESET_DIR", "default": "presets" }`）。下方的同文件 `presets` 目錄
也是有效的 `$ref` 目標，可透過 `#/presets/…` 引用。

## Lighting 預設片段

- `brightInterior` — 乾淨、可閱讀的室內補光。把 LGTM + IMGS 用在 `cells[].lightingTemplate`
  與 `cells[].imageSpace` 上。
- `warmTavern` — 琥珀色、低霧、帶輕微 bloom 的旅店／商店光照。
- `coldDungeon` — 藍灰色、低飽和度、搭配淡霧的地城光照。

## Weather 預設片段

- `clearBright` — 清朗的 vanilla sky base，加上更明亮的室外 ImageSpace grading。
- `foggyPale` — 淡色多雲天氣，搭配近距離霧與去飽和的 grading。
- `stormCinematic` — 雨暴天氣，包含 rain SPGD、更強的風，以及對比強烈的 grading。

Weather 預設片段只會產生 WTHR/CLMT 記錄。把 climate 指派到某個世界空間／region，或使用
`build` 印出的 console `sw` 指令來強制測試所產生的 weather。

## Package 預設片段

- `guardPost` — 讓守衛守住本地崗位的精簡 sandbox。
- `wanderMerchant` — 偏服務導向、給商人 NPC 用的 sandbox；真正的商店仍需要一個 vendor
  陣營與 merchant chest。
- `campFollower` — 純移動層的 Follow 套件；省略 target 時預設為玩家。

把套件的 editorId 掛到 `npcs[].packages`。套件順序仍然重要：把特定的 travel/follow
或受任務管控的套件放在廣泛的 sandbox fallback 之前。

## Identity 預設片段

- `Adventurer` — 從遊戲開始即授予的預設 baseline 身分。
- `Merchant` — 從 ledger book 取得的 toggle 身分。
- `Guard` — 從 writ book 取得的 toggle 身分。
- `Paladin` — 從 oath book 取得，且玩家穿著 heavy armor 時 active。
- `Dragonborn` — 一旦 `DragonSouls >= 1` 即自動授予。

對話條件使用既有的 `identity` 與 `primaryIdentity` 欄位。身分取得書需要
`package` 指令來出貨可重複使用的身分腳本。
