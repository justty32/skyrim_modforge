# zh-tw-sync — 同步繁中鏡像 + 重生 html

← [INDEX](../INDEX.md)｜[WORKFLOWS.md](../WORKFLOWS.md)｜程式碼慣例見 [common/conventions](common/conventions.md)

`docs/zh-TW/` 是 EN `docs/` 的 **1:1 繁體中文鏡像**。EN 使用手冊改動後，把對應改動同步進鏡像，再重生 html bundle。`[[zh-tw-translation-mirror]]` 連 Claude memory。

## ① 觸發時機

改了 `docs/`（EN 使用手冊：`SPEC-*.md` / `for_agent*.md` / `external_assets.md` / `engine-internals.md` / `lifelike/` cookbook…）後，同步鏡像。純 `workflows/`（開發流程文檔）**不進**鏡像——鏡像只覆蓋 `docs/` 使用手冊。

## ② 鏡像範圍（1:1）

- `docs/zh-TW/**.md` 與 `docs/**.md` **檔案結構一一對應**（同名同路徑，SPEC 在 `spec/`、cookbook 在 `lifelike/`）。EN 新增/刪除/改名一個 doc → 鏡像同步新增/刪除/改名。
- `docs/zh-TW/html/` 是由 `generate.py` 從 `docs/zh-TW/**.md` **自動產生**的，不手改（改 `.md` 後重生）。

## ③ 翻譯慣例

- **技術名詞 / spec 欄位 key / record 型別 / CLI 指令 / env var / 程式碼識別字**保英文（如 `MODFORGE_SKYRIM_DATA`、`Category`、`SCEN`、`LGTM`、`validate`）。
- **engine-internals 的標題保英文**（跨檔 anchor 目標，翻了會斷連結）。
- **`docs/zh-TW/spec/` 內、指向鏡像樹外的相對連結要多一層 `../`**（zh-TW 比 EN 多一層目錄；逃出鏡像樹的連結補一層 `../`）。鏡像樹內的相對連結照抄 EN 即可。
- **程式碼區塊（```）照抄**，不翻譯內容。
- 只翻譯敘事散文；表格欄位名視 EN 原樣（EN 表頭如 `File` / `Contents` 可保留或譯，與該檔既有風格一致）。

## ④ 重生 html

同步完 `.md` 後，跑（讀取 `docs/zh-TW/**.md`，全量重生 33 頁到 `docs/zh-TW/html/`，含 `index.html`，並刪除 stale 頁）：

```bash
python docs/zh-TW/html/generate.py
```

- 頁面集在 `generate.py` 的 `SECTIONS` 表寫死（代理工作流程 6 頁 + SPEC 14 頁 + lifelike 12 頁 + index）。**新增/刪除一個鏡像 doc → 先改 `SECTIONS` 再跑**，否則新頁不會產、或連結 rewrite 會報 `unmapped link target`。
- 重生會把每頁 `.md` 原文嵌進 `<textarea>` 由 marked.js 前端渲染，並把 in-doc `*.md` 連結改寫成扁平 `.html`——所以 html 永遠不會偏離來源 `.md`。全量 33 頁重生是**正常**的（非只改動頁）。
