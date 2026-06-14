# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[workflows/INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- **agent 並行 prep（2026-06-14）**：為「其他 agent 並行討論 Sofia 劇情 / 調查 mod」備好兩工作區 + 共用資料 + 工具：① 新 CLI 指令 `gamedata <plugin> <outDir>`（lazy 串流批次抽書/對白/任務/NPC・物品・地點・魔法）；② `sub_projs/game-data/`（已抽 vanilla+DLC+CC + Sofia/VIGILANT/RDO/FCO/IFDL… 文本，`extract.sh` 重生、文本 gitignore）；③ 工作區 `sub_projs/sofia-patch/`（劇情）+ 新 `sub_projs/mod-survey/`（mod 調查），各 symlink game-data/guide/mods；④ 兩份 guide：`workflows/investigation/esm-formid-access.md`、`mod-survey-guide.md`。**大重構期間主 session 不碰這兩工作區 + game-data。**
- **大重構（拆檔門檻）— 主體完成 2026-06-14**：門檻＝**文檔 8192 bytes**（src/examples 維持 300 行）；DEV-GUIDE 觸發 A + conventions 同步。已拆：`tooling.md`→`tooling/`、`landed.md`→`landed/`（CODE_MAP 五分法）、**完成的 plans+specs→各自 archive**（原則：completed→archive 凍結不拆、現役夾清空）、idea 報告升 L4（`particle-vfx/`/`map-scene/`/`animation/`/`voice-clone/engine-setup/`）。全樹相對連結健檢通過。
  - ✅ DEV-GUIDE 改**被動參考**（結構整理原則+四級成長按需取用，類 zh-tw/html）；**鐵律上提到 CLAUDE.md always-on**；INDEX/conventions/各 workflow README 的「元工作流/貫穿」標籤同步改被動。
  - ✅ plans/specs **未來命名去日期**（`<功能>.md` / `<功能>-design.md`，日期記 index）；archived 舊檔保留日期前綴、凍結。
  - **剩餘（小清理）**：`decode/notes-gemini-voice` 微清理（自述可刪→待確認）、`docs→workflows` 殘留舊路徑（`docs/minor/ideas.md`、`docs/CODE_MAP.*.md`）校正、`feature-dev/gotchas.md` 檔內分節。idea `01`/`03` 概覽與 `ideas.md` 經判定 **KEEP**（連貫敘事不硬拆）。
- docs/workflows 大重構：`docs/` 回歸 ModForge 使用手冊（cookbook/cheatsheet/spec）；開發流程全移到 repo 頂層 `workflows/`（INDEX / CODE_MAP / DEV-GUIDE / 各工作流 / 踩坑 / roadmap / 調查）。CLAUDE.md 瘦成路由器；`SESSION-LOG.md` + `WAIT_USER.md` 升到 repo 根。
- 語音合成解耦為基石專案 `sub_projs/skyrim-voicegen/`（協議 PROTOCOL.md）；Sofia 擴充為消費者專案 `sub_projs/sofia-patch/`。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

（無）

## justty32 的隨筆

整理 memory
把dev-guide弄成被動式，不要每次都貫穿所有workflow，他應該是一個獨立的workflow，只在需要時被使用者拿出來作用，就類似zh tw和html那種。
然後是plans和specs，我要重構這兩個工作流與其現有內容。其現有內容的檔名不應該含有日期，日期應該放在index部分
4096 bytes做上限還是太小了，換8192會好點。然後其他如src,example的上限還是保持行數三百行。