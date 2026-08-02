# Mod 調查 guide — agent 操作手冊

← [investigation README](README.md)｜工作區 [`../../analysis/mod-survey/`](../../sub_projs/mod-survey/README.md)

給負責「調查 `~/skyrim_mods/` 那批已下載 mod」的 AI agent。**產出寫進 `../../analysis/mod-survey/`**，不要寫進 `workflows/`（主 session 正在重構它）。

## 目標

逐個 mod 弄清楚：**它做什麼、用什麼機制、對 Sofia patch 或 ModForge roadmap 有何意義**。重點不是全抄，是**判斷可借鏡 / 需相容 / 可忽略**。

## mod 在哪

- 壓縮檔：`~/skyrim_mods/*.7z` `*.zip` `*.rar`（80+，多數**未解壓**）
- 已解壓：`~/skyrim_mods/unzip/<mod>/`（約 18 個）
- 預抽好的文本：`../game-data/mods/<plugin>/`（先查這裡，多數內容型 mod 的對白/書/任務已在）
- **別碰** `~/skyrim_mods/mine/`（ModForge 出貨 zip）；只讀，別改原始 mod 檔

## 流程

1. **分類**：先掃壓縮檔名 → 粗分「內容型（隨從/任務/對白/地點）｜框架型（SKSE/SPID/papyrus util）｜修復型（bug/engine fix）｜美術型（mesh/texture/animation）」。**美術/修復/框架型通常無敘事價值**，記一行帶過即可。
2. **有 plugin 才深挖**：解壓到 `unzip/`（`7z x`、`unrar x`）→ 有 `.esp/.esm/.esl` 才值得抽。
3. **抽文本**：`../game-data/extract.sh <path/to.esp>` → `game-data/mods/<name>/`，讀 `summary.txt` 抓規模，再讀 `dialogue.md`/`quests.md`。
4. **單筆深挖**：要看某 quest 的條件分歧、某 scene 的演出 → 用 [esm-formid-access.md](esm-formid-access.md) 的 `find`→`infodiag`/`scenediag`。
5. **記憶體鐵律**：一律走 ModForge CLI（lazy overlay）；**絕不**整載 Skyrim.esm、不 `.ToList()` 整個 group。

## 產出格式（寫進 `../../analysis/mod-survey/`）

- `findings/<mod-name>.md`：類型 / 是否有 plugin / 關鍵記錄（quest・scene・GLOB・dialogue 規模）/ 用到的機制 pattern / **對 Sofia 或 roadmap 的意義**（可借鏡？需相容？忽略？）
- `index.md`：總表，按上面四分類列出，標每個 mod 的「敘事價值 高/中/無」

## 與 Sofia 討論的銜接

很多隨從/對話 mod（RDO、FCO、IFDL、Nether's Follower Framework…）的 pattern 直接關係到 Sofia patch 的相容性與借鏡。發現可用 pattern 時，在 finding 裡明確指向 `../sofia-patch/` 的對應功能（F1–F16，見其 `expansion-plan`）。
