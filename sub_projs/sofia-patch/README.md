# Sofia 擴充專案（sub_projs/sofia-patch/）

> **這是一個獨立專案**，把 ModForge 當工具使用（JSON spec → `.esp`），不與 ModForge 整合。成熟或體量龐大後可移去自己的 repo。

> **🤖 給劇情討論 agent**：本夾就是你的工作區。手邊資源（已 symlink 進來）：
> - `game-data/` → 全遊戲文本/清單（vanilla+DLC+Sofia+VIGILANT…）：對白 `game-data/*/dialogue.md`、書 `books.md`、任務 `quests.md`、清單 `*.tsv`。
> - `esm-formid-access.md` → 要補抽/查 FormID 時的 CLI 工具參考。
> - 既有解碼：下面檔案索引（personality / follower-decode / expansion-plan / vigilant-support）。
> **記憶體鐵律**：要探 esp 一律走 ModForge CLI（lazy overlay），絕不整載 Skyrim.esm、不 `.ToList()` 整個 group。

**專案目標**：用 ModForge（JSON spec → 生成 `.esp`）做一個 **Sofia 風格的隨從擴充**——不手改 CK，而是把 Sofia 賴以成立的那些 pattern（在場偵測 banter、GLOB 狀態、小型 controller quest、條件分歧對話、克隆語音）規模化成更多吐槽、更深互動、好感度系統、新演出 scene 與 mini-quest。

解碼總結論：**Sofia 沒用到任何 ModForge 做不出的機制**，它是已落地能力的規模化組合，ModForge 直接夠用。

## 檔案索引

| 檔案 | 說明 |
|------|------|
| [`sofia-personality.md`](sofia-personality.md) | **性格分析 / 寫作 brief**（本專案中心）——Sofia 的原型、幽默機制、說話癖、不安全感、情緒光譜，附大量原文台詞範例 + 「寫新台詞 checklist」。要生成「聽起來像 Sofia」的新對話先讀這份。 |
| [`follower-decode-2026-06-13.md`](follower-decode-2026-06-13.md) | **結構+內容解碼**——`SofiaFollower.esp` 的記錄普查（30 quest / 28 scene / 1135 INFO / 57 GLOB）、五個可複用架構 pattern、quest/scene/formlist 內容索引、對 ModForge 的施工法。 |
| [`expansion-plan-2026-06-13.md`](expansion-plan-2026-06-13.md) | **擴充計畫 + 可行性對照**——F1–F16 十六個具體功能，逐個標 ✅/🟡/🔴 並給 spec 範例與降級方案；含建議實作順序與缺口彙總表。 |
| [`vigilant-support-plan-2026-06-13.md`](vigilant-support-plan-2026-06-13.md) | **Sofia × VIGILANT 支援計劃（機制層）**——讓 Sofia 對 VIGILANT 進度有「可對談反應」：任務/scene 狀態更新後在她身上**浮現談論選項**（玩家主動找她聊），用 quest-state condition + 對話樹組裝，**刻意不用 scene/自動插話**。本 session 對話樹+跨任務閘的綜合應用，無新功能缺口。 |
| [`vigilant-sofia-逐章演出設計.md`](vigilant-sofia-逐章演出設計.md) | **逐章 beat sheet（敘事層設計意圖，2026-06-14）**——VIGILANT 每個劇情節點 Sofia 該做/不做什麼。核心原則：夢/回憶/單人場景＝**Sofia 以「幻影隨身掛件」在場**（她跟入但夢中人無視、只跟玩家吐槽；跟不跟逐場決定）、跟隨合理性＝**命運糾纏**、角色弧＝老練但抗拒成長、**Molag Bal 高位格特例**。**四幕 beat 全到位。** |
| [`vigilant-screenplay/`](vigilant-screenplay/README.md) | **四幕對白劇本草稿（DRAFT 待審）**——把上面的 beat 寫成 Sofia 的實際台詞，按四幕拆檔（警戒者／墮入／宅邸／冷港）+ README 放共用原則。審核校正後才轉 `examples/*.json`。 |

外部參考（repo 主 spec 文檔）：[`SPEC-dialogue-quests.md`](../../docs/spec/SPEC-dialogue-quests.md)、[`SPEC-packages.md`](../../docs/spec/SPEC-packages.md)、[`SPEC-world.md`](../../docs/spec/SPEC-world.md)、[`SPEC-workflow.md`](../../docs/spec/SPEC-workflow.md)。

**相關工具（cell 逆向）**：[`docs/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md`](../../workflows/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md) — 用新 CLI 子指令 `cellrefs <esp> <0xFORMID>` 把 vanilla interior cell（範例 RiverwoodSleepingGiantInn `0x0133C6`，480 ref）逆向成 `placements[]` JSON（`examples/sleeping_giant_inn.json`）。旋轉 esm radian→ModForge degree、cell-override 寫法、scale 缺欄等坑都記在那。要把 Sofia 演出搬進某個 vanilla 室內、或重佈置一個既有 cell 時用得上。

## 雜七雜八 / misc data

施工參考雜項——原始 mod 磁碟位置、用 CLI 探 esp 的正路（`find`/`infodiag`/`scenediag`，記憶體鐵律）、可行性總帳（16 功能 ✅11 / 🟡3 / 🔴2）、本專案會吃到的 ModForge 已落地能力清單、寫作鐵律提醒 → 全在 [misc-reference.md](misc-reference.md)。
