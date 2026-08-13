# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [DEV-GUIDE「膨脹即拆」](../../DEV-GUIDE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### `src/` 拆檔分層 + hub 解耦 — Batch 0–5 全部完成，只剩收尾

計畫全文：[src-layout-plan.md](src-layout-plan.md)。2026-08-13 於離線機 Windows 一次做完全部六批：

- ✅ **Batch 0**（`793615b`）：`scripts/golden-hash.sh` 護欄。
- ✅ **Batch 1**（`1e638d1`）：345 個純 rename，`src/`＋`tests/` 進領域資料夾。
- ✅ **Batch 2**（`c9c8207` `807b0a6` `f4035c9` `3104459`）：四個 hub 檔拆完——`Spec.cs` 102→5 個成員、`BuildContext` 57→27 個欄位、`Program.cs` 204→76 行、`Validate.cs` 收尾。**`Generator.Build.cs` 評估後刻意不動**，理由寫在該檔檔頭。
  - 另加第二層護欄 `scripts/cli-dispatch-snapshot.sh`（330 種 argv 形狀），因為 golden hash 只走 `build`、測試幾乎不碰 CLI。兩支護欄用法都在 [testing.md](../testing.md)。
- ✅ **Batch 3**（`e5061b9`）：`BuildContext` 60 個 partial 從 `private` 改 `internal`，測試終於碰得到；附上第一批「只跑一個 build step」的單元測試（`Build/BuildContextUnitTests.cs`）。測試數 1122 → **1126**。停在 `internal` 巢狀、沒有真的 un-nest，理由在計畫 Batch 3。
- ⏹ **Batch 4**（`5266a1c`）：**做 3 項後主動停住**。抽出 `VanillaMasters`（build 唯一的檔案系統邊界，附 4 個單元測試）、`MakeLocationSlot` 歸位到唯一呼叫端、5 個 `TranslationMask` 變 `file static class`。`BuildContext` 欄位 57 → **25**。剩下的 Lighting／ExteriorCells／NpcPatches／Scene 幾群**量過之後決定不剝**——原排序只數欄位、沒數方法相依，實際每群要注入 4～10 個成員（含泛型方法、回寫的計數器、別人擁有的 queue），剝出來是包了儀式的 back-pointer。理由與量測表在計畫 Batch 4。
- ✅ **Batch 5**（`9c24ab4` `6281659`）：`Validate.Items2/World2` → `.More` ＋ 四個檔加「驗哪些 record family」的檔頭清單；`PlayerRef`/`McmPlayerRef` 合併成 **`PlayerNpcBase`**（不只是重複，名字本來就錯——`0x000014` 是玩家 NPC base，`PlayerRef` 是 `0x000007`）；補 `Fuz` + `Translator` 測試（1130 → **1141**）；`for_agent_lib.md` 講明那張表就是全部支援介面。**清單裡有兩項前提是錯的**（`OarGen.HkxCopy` 和 `SceneCoordinates` 都不是死碼），查證結果記在計畫。

**整條 `src/` 拆檔線（Batch 0–5）到此結束，收尾也做完了。** 這一條可以在下次整理時整個刪掉——只剩兩件待你拍板的，不屬於重構。

**收尾（文檔面向，2026-08-13 已做完）：**

1. ~~未 push~~ **已推**：ModForge `7961c14..453e507`（16 commits），母 repo `b95ee0d..7ed4bbd`（submodule 指標已 bump）。兩邊與 origin 0/0 同步。
2. ~~文檔裡 5 條既有死連結~~ **已處理（`19fcadc` + 本次）**——查證後是三種不同的東西：`docs/engine-internals.md`（含 zh-TW）是**真的寫錯**（說生成器在 `src/ModForge.Cli/Build.cs`，其實一直在 Core）已修；`plans/ingame-scene-export.md` 那兩條是**計畫原文**（一條被自己文件上方的進度段取消、一條落地時改了名），就地註記不刪；archive 那兩條**指的檔案真的存在過**（`c0a4885` 加入、`8758d76` 退場），凍結的歷史不動。細節見計畫 Batch 1 的註腳。
3. ~~`DEV-GUIDE.md` 缺「平鋪太多即包夾」~~ **已補（`19fcadc`）**——現在是觸發 C，並寫明它與觸發 A 的因果關係。

**兩件留給你拍板的（不是重構決定）：**

- 要不要把 ~100 個 public 成員降 `internal`（要先給 Cli 加 `InternalsVisibleTo`，並決定有 SPEC 文件的型別算不算對外契約）——量測與選項在計畫 Batch 5。
- `Archives.cs` / `Papyrus.cs` 仍零測試覆蓋（`Papyrus` 離線測不了）。
