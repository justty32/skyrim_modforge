# mod-survey/ — 已下載 mod 的調查工作區

> **這是給其他 AI agent 工作的工作區**，不是 ModForge 工具的一部分。
> 主 session 在跑「大重構（拆檔門檻 300 行→4096 bytes）」期間**不碰這個資料夾**——這裡是調查 agent 的地盤。

## 目的

把 `~/skyrim_mods/` 那批已下載的 mod（80+ 個）逐個調查、做成結構化筆記，餵給：
1. **Sofia patch 劇情討論**（`../sofia-patch/`）——哪些 mod 的 pattern / 內容可借鏡或需相容。
2. **ModForge roadmap**——解碼浮現的新能力需求。

## 怎麼做（方法）

調查**方法**寫在 → [`workflows/investigation/mod-survey-guide.md`](../../workflows/investigation/mod-survey-guide.md)
（怎麼解壓、怎麼用 CLI 探 esp、記憶體鐵律、該記什麼）。

ESM/FormID 抽取**工具參考** → [`workflows/investigation/esm-formid-access.md`](../../workflows/investigation/esm-formid-access.md)

全遊戲文本/清單**參考資料** → [`../game-data/`](../game-data/)（vanilla + mod 抽出的劇情/對白/書/清單）。

## 磁碟上的 mod 在哪

- 壓縮檔：`~/skyrim_mods/*.7z` `*.zip` `*.rar`（80+，多數**尚未**解壓）
- 已解壓：`~/skyrim_mods/unzip/<mod>/`（約 18 個，含 Sofia / VIGILANT / RDO / FCO / IFDL / Glad You're Here / Alternate Start 的 plugin）
- **別碰** `~/skyrim_mods/mine/`（那是 ModForge 自己的出貨 zip）；`~/skyrim_mods` 根是使用者的 Nexus 下載

## 產出放這裡

- `findings/<mod-name>.md` — 每個 mod 一份：類型 / 是否有 plugin / 關鍵記錄 / 對 Sofia 或 roadmap 的意義
- `index.md` — 調查總表（建議按「內容型 / 框架型 / 修復型 / 美術型」分類）

（目前空——等 agent 開工。）
