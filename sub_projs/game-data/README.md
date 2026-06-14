# game-data/ — 抽取出的全遊戲文本與清單（agent 參考用）

> **共用參考資料**，不是工具。給 `../sofia-patch/`（劇情討論）和 `../mod-survey/`（mod 調查）的 AI agent **唯讀取用**。
> 文本約 12 MB、可由 `extract.sh` 重生 → `vanilla/`、`mods/` 已 **gitignore**（不進版控）。主 session 大重構期間**不碰本夾**。

## 怎麼重生 / 補抽

```bash
./extract.sh                 # 抽 主檔+DLC+CC（Data 夾）+ ~/skyrim_mods/unzip 下所有 plugin
./extract.sh <path/to.esp>   # 補抽單一 plugin → mods/<basename>/
```
背後是 ModForge CLI 新指令 `gamedata <plugin> <outDir>`（lazy overlay、單趟串流、記憶體安全，能跑 250MB 的 Skyrim.esm）。在地化主檔/mod 自動掃同夾 `.bsa` 解 English `.STRINGS`；非在地化 mod 走 inline。

## 內容版圖

`vanilla/<master>/` 與 `mods/<plugin>/`，每個 plugin 一夾，內含：

| 檔 | 內容 |
|----|------|
| `dialogue.md` | 對白：按 topic 分節（prompt + 每句 INFO 回應 + speaker FormID 閘）|
| `books.md` | 書籍全文（EditorID / Name / 完整內文）|
| `quests.md` | 任務：stage log 文字 + objective 顯示文字 |
| `npcs.tsv` | NPC：`formid｜editorid｜name` |
| `items.tsv` | 武器/防具/藥水/食材/雜項：`formid｜editorid｜type｜name` |
| `locations.tsv` | CELL/WRLD/LCTN：`formid｜editorid｜type｜name` |
| `magic.tsv` | SPEL/SHOU/SCRL/MGEF：`formid｜editorid｜type｜name` |
| `summary.txt` | 各類數量 + 是否在地化 |

## 已抽範圍（首輪）

- **vanilla（10）**：Skyrim / Update / Dawnguard / HearthFires / Dragonborn + CC（Fish / AdvDSGS / Curios / SurvivalMode）+ _ResourcePack。
  - 規模感：Skyrim.esm = 書 821、對白 34427 句、任務 1811、NPC 5118、物品 6074、地點 2606、魔法 1926。
- **mods（unzip/ 下有 plugin 者）**：SofiaFollower、Vigilant（**英文版**）、Relationship Dialogue Overhaul、FCO、ImprovedCompanionsBoogaloo（IFDL 核心）+ 各 IFDL/Lydia patch、ImGladYoureHere、Alternate Start…
  - **尚未涵蓋**：`~/skyrim_mods/` 裡仍是壓縮檔（`.7z/.rar/.zip`）、未解壓的 mod。要補：先解壓到 `~/skyrim_mods/unzip/`，再 `./extract.sh`（或對單一 esp 跑 `./extract.sh <esp>`）。mod-survey agent 可按需自行補抽。

## 坑

- **多語 FOMOD**：VIGILANT 同時有 `10 English/` 與 `10 Japanese/` 的 `Vigilant.esm`；`extract.sh` 已優先取 English（否則會抽到日文 mojibake）。補抽多語 mod 時自行指定英文那份。
- speaker 只給 FormID（不跨記錄解名，省記憶體）→ 要對照名字查同夾 `npcs.tsv`。
