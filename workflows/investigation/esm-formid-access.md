# ESM / FormID 存取 — agent 工具參考

← [investigation README](README.md)｜外部工具總表 [tooling.md](../tooling/README.md)

給 AI agent 的「怎麼從 `.esm/.esp` 取出可讀內容 / 查 FormID」操作手冊。配 [tooling.md](../tooling/README.md)（環境變數、binary、字串依賴的完整表）一起看。

## 鐵律（先讀，違反會吃光記憶體）

- **絕不**用自己寫的 loader 整載 `Skyrim.esm`（250 MB）。一律走 ModForge CLI——它用 **lazy read-only overlay**。
- **不要** `.ToList()` 整個 record group。要遍歷就 `foreach` 串流、邊讀邊寫、不囤。
- 改檔禁區：不在這些工具上「順手」改原始 mod 檔；只讀。

## 跑 CLI

```bash
DLL="<repo>/src/ModForge.Cli/bin/Release/net10.0/ModForge.Cli.dll"
dotnet "$DLL" <command> <plugin> [args...]
```
（沒有 Release build 就先 `dotnet build src/ModForge.Cli -c Release`。）
主檔在 `MODFORGE_SKYRIM_DATA`（預設 Steam `…/Skyrim Special Edition/Data`）；下載的 mod 在 `~/skyrim_mods/unzip/<mod>/`。

## 批次抽取（整個 plugin → 資料夾）

```bash
dotnet "$DLL" gamedata <plugin> <outDir>
```
把一個 plugin 的 書/對白/任務/NPC・物品・地點・魔法清單 串流進 `<outDir>`（檔案版圖見 [`sub_projs/game-data/README.md`](../../sub_projs/game-data/README.md)）。
**已預抽好**的 vanilla+DLC+主要內容 mod 在 `sub_projs/game-data/{vanilla,mods}/` ——多數情況直接讀那裡即可，不必重跑。要補抽未涵蓋的 mod：解壓到 `~/skyrim_mods/unzip/` 後跑 `sub_projs/game-data/extract.sh`，或 `extract.sh <esp>` 單抽。

## 單筆診斷（要先有 FormID）

先用 `find` 把 EditorID/Name → FormID，再用對應 `*diag`：

| 指令 | 給什麼 | 取出 |
|------|--------|------|
| `find <plugin> <query> [type]` | 字串（editorId/name 子串）+ 選填型別 | 命中的 `0xFORMID` |
| `dump <plugin>` | — | 整個 plugin 所有記錄概覽（**僅限小 esp**，別對 Skyrim.esm 用）|
| `booktext <plugin> <0xFORMID>` | 書 FormID | 書名 + 完整內文（自動解在地化 STRINGS）|
| `infodiag <plugin> <0xFORMID> [substr]` | quest FormID | 該 quest 所有 topic 的 INFO 回應 + 完整 CTDA 條件 |
| `scenediag <plugin> <0xFORMID>` | SCEN FormID | scene 的 host quest / actor alias / phase / action |
| `questdiag <plugin> <0xFORMID>` | quest FormID | stages（log + flags）+ objectives（顯示文字 + targets）|
| `npcdiag <plugin> <0xFORMID>` | NPC FormID | race/class/voice/factions/packages/flags |
| `cellrefs <plugin> <0xFORMID>` | cell FormID | cell 內所有 ref → `placements[]` JSON（逆向佈置）|

其餘型別還有 `mgefdiag / enchdiag / lightdiag / packagediag / cstydiag / perkdiag / cobjdiag / weatherdiag / factdiag / reladiag / shoutdiag / smtree …`（完整清單跑 `dotnet "$DLL"` 無參數看 usage）。

## FormID 慣例

- 印出/比對一律用 24-bit 形式 `0xXXXXXX`（去掉 load-order 高位元組）。
- 字串解析：在地化主檔的 Name/書內文住在 `.STRINGS`（在 `Skyrim - Interface.bsa` 等 BSA）；`booktext`/`gamedata` 會自動抽 English 版到暫存夾再解。舊 mod 多半 inline、不需此步。詳見 [tooling §3](../tooling/data-assets.md) 與記憶 `headless-vanilla-strings-provision`。
