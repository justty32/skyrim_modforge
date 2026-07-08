# Sofia 擴充專案（sub_projs/sofia-patch/）

> **這是一個獨立專案**，把 ModForge 當工具使用（JSON spec → `.esp`），不與 ModForge 整合。成熟或體量龐大後可移去自己的 repo。

> **🤖 給劇情討論 agent**：本夾就是你的工作區。手邊資源（已 symlink 進來）：
> - `game-data/` → 全遊戲文本/清單（vanilla+DLC+Sofia+VIGILANT…）：對白 `game-data/*/dialogue.md`、書 `books.md`、任務 `quests.md`、清單 `*.tsv`。
> - `esm-formid-access.md` → 要補抽/查 FormID 時的 CLI 工具參考。
> - 既有解碼：[`reference/`](reference/README.md)（personality / follower-decode）、[`plans/`](plans/README.md)（expansion-plan / vigilant-support）。
> **記憶體鐵律**：要探 esp 一律走 ModForge CLI（lazy overlay），絕不整載 Skyrim.esm、不 `.ToList()` 整個 group。

**專案目標**：用 ModForge（JSON spec → 生成 `.esp`）做一個 **Sofia 風格的隨從擴充**——不手改 CK，而是把 Sofia 賴以成立的那些 pattern（在場偵測 banter、GLOB 狀態、小型 controller quest、條件分歧對話、克隆語音）規模化成更多吐槽、更深互動、好感度系統、新演出 scene 與 mini-quest。

解碼總結論：**Sofia 沒用到任何 ModForge 做不出的機制**，它是已落地能力的規模化組合，ModForge 直接夠用。

## 檔案索引

| 資料夾/檔案 | 說明 |
|------|------|
| [`reference/`](reference/README.md) | **人設 / 解碼 / 世界觀參考**——`sofia-personality.md`（性格分析/寫作 brief，本專案中心）、`follower-decode-2026-06-13.md`（`SofiaFollower.esp` 結構+內容解碼）、書籍理解、對 Skyrim 元素的反應、lore 典籍翻譯、雜項施工參考。要生成「聽起來像 Sofia」的新對話先讀這夾的 `sofia-personality.md`。 |
| [`plans/`](plans/README.md) | **擴充計畫 + 演出設計**——`expansion-plan-2026-06-13.md`（F1–F16 可行性對照）、`vigilant-support-plan-2026-06-13.md`（Sofia × VIGILANT 機制層支援計劃）、`vigilant-sofia-逐章演出設計.md`（逐章 beat sheet，敘事層設計意圖）。 |
| [`dialogue-lists/`](dialogue-lists/README.md) | **完整台詞列表**——EN 全本 + 繁中四部（Custom Topics / Misc+Combat / 首遇與婚禮 / 原版任務評論），從 `SofiaFollower.esp` 提取的全部對白。 |
| [`vigilant-screenplay/`](vigilant-screenplay/README.md) | **四幕對白劇本草稿（DRAFT 待審）**——把 `plans/` 的 beat 寫成 Sofia 的實際台詞，按四幕拆檔（警戒者／墮入／宅邸／冷港）+ README 放共用原則。審核校正後才轉 `examples/*.json`。 |

外部參考（repo 主 spec 文檔）：[`SPEC-dialogue-quests.md`](../../docs/spec/SPEC-dialogue.md)、[`SPEC-packages.md`](../../docs/spec/SPEC-packages.md)、[`SPEC-world.md`](../../docs/spec/SPEC-world.md)、[`SPEC-workflow.md`](../../docs/spec/SPEC-workflow.md)。

**相關工具（cell 逆向）**：[`docs/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md`](../../workflows/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md) — 用新 CLI 子指令 `cellrefs <esp> <0xFORMID>` 把 vanilla interior cell（範例 RiverwoodSleepingGiantInn `0x0133C6`，480 ref）逆向成 `placements[]` JSON（`examples/sleeping_giant_inn.json`）。旋轉 esm radian→ModForge degree、cell-override 寫法、scale 缺欄等坑都記在那。要把 Sofia 演出搬進某個 vanilla 室內、或重佈置一個既有 cell 時用得上。

## 雜七雜八 / misc data

施工參考雜項——原始 mod 磁碟位置、用 CLI 探 esp 的正路（`find`/`infodiag`/`scenediag`，記憶體鐵律）、可行性總帳（16 功能 ✅11 / 🟡3 / 🔴2）、本專案會吃到的 ModForge 已落地能力清單、寫作鐵律提醒 → 全在 [reference/misc-reference.md](reference/misc-reference.md)。
