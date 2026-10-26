# External data / asset dependencies (read at runtime, not in repo)

← [tooling README](README.md)

執行期讀取、不在 repo 裡的外部資料／資產：誰需要、怎麼找到、測試怎麼閘。

已抽到 [data-assets.json](data-assets.json)（7 列）。欄位：

- `Dependency`：依賴的資料或資產。
- `Needed by`：用到它的功能。
- `Located via`：定位方式（env var、預設路徑、CLI 參數）。
- `Test gate`：測試怎麼處理（`RequiresSkyrim` trait、gitignore、不閘）。

統計：7 列；靠 `MODFORGE_SKYRIM_DATA` 定位的 2 列、Papyrus 相關 2 列、語音相關 2 列、spec 相對路徑 1 列。
