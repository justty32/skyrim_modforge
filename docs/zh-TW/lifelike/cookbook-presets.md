# 食譜手冊 — presets

← [食譜目錄](cookbook-index.md) | [lifelike 主頁](README.md)

`presets` 是一份不直接 emit record 的具名 copy-paste 食譜目錄。builder 會忽略它；要真正建立記錄，請把 preset 展開成一般頂層陣列（`lightingTemplates`、`imageSpaces`、`weathers`、`climates`、`packages`、`books`、`identities` 等）。

完整範例：[`examples/presets-cookbook.json`](../../examples/presets-cookbook.json)。它同時包含 catalog 與已展開的具體 records，因此 `validate`/`build` 可以測這些食譜。

## Lighting presets

- `brightInterior` — 乾淨、可閱讀的室內補光。把 LGTM + IMGS 用在 `cells[].lightingTemplate` 與 `cells[].imageSpace`。
- `warmTavern` — 琥珀色、低霧的旅店/商店光照，帶輕微 bloom。
- `coldDungeon` — 藍灰、低飽和地城光照，帶淡霧。

## Weather presets

- `clearBright` — 清朗 vanilla sky base，加上更明亮的室外 ImageSpace grading。
- `foggyPale` — 淡色多雲天氣，近距離霧與低飽和 grading。
- `stormCinematic` — 雨暴天氣，包含 rain SPGD、較強風與更重對比的 grading。

Weather presets 只產生 WTHR/CLMT records。要讓它在遊戲中發揮作用，請把 climate 指派到 worldspace/region，或使用 `build` 印出的 console `sw` 指令強制測試生成的 weather。

## Package presets

- `guardPost` — 讓守衛守住本地崗位的 compact sandbox。
- `wanderMerchant` — 偏服務導向的商人 sandbox；真正的商店仍需要 vendor faction 與 merchant chest。
- `campFollower` — 純移動層 Follow package；省略 target 時預設跟隨玩家。

把 package editorId 掛到 `npcs[].packages`。Package 順序仍重要：具體 travel/follow 或 quest-gated package 放在 broad sandbox fallback 前面。

## Identity presets

- `Adventurer` — 從遊戲開始授予的預設 baseline identity。
- `Merchant` — 從 ledger book 取得的 toggle identity。
- `Guard` — 從 writ book 取得的 toggle identity。
- `Paladin` — 從 oath book 取得，且玩家穿 heavy armor 時 active。
- `Dragonborn` — `DragonSouls >= 1` 後自動授予。

Dialogue gates 使用既有 `identity` 與 `primaryIdentity` 欄位。Identity acquire books 需要 `package` command 交付可複用的 identity scripts。
