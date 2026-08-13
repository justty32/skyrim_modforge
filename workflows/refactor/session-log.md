# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [DEV-GUIDE「膨脹即拆」](../../DEV-GUIDE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### `src/` 拆檔分層 + hub 解耦 — Batch 0–2 完成，Batch 3 未開始

計畫全文：[src-layout-plan.md](src-layout-plan.md)。2026-08-13 於離線機 Windows 一次做完 Batch 0、1、2：

- ✅ **Batch 0**（`793615b`）：`scripts/golden-hash.sh` 護欄。
- ✅ **Batch 1**（`1e638d1`）：345 個純 rename，`src/`＋`tests/` 進領域資料夾。
- ✅ **Batch 2**（`c9c8207` `807b0a6` `f4035c9` `3104459`）：四個 hub 檔拆完——`Spec.cs` 102→5 個成員、`BuildContext` 57→27 個欄位、`Program.cs` 204→76 行、`Validate.cs` 收尾。**`Generator.Build.cs` 評估後刻意不動**，理由寫在該檔檔頭。
  - 另加第二層護欄 `scripts/cli-dispatch-snapshot.sh`（330 種 argv 形狀），因為 golden hash 只走 `build`、測試幾乎不碰 CLI。兩支護欄用法都在 [testing.md](../testing.md)。
- ✅ **Batch 3**（`e5061b9`）：`BuildContext` 60 個 partial 從 `private` 改 `internal`，測試終於碰得到；附上第一批「只跑一個 build step」的單元測試（`Build/BuildContextUnitTests.cs`）。測試數 1122 → **1126**。停在 `internal` 巢狀、沒有真的 un-nest，理由在計畫 Batch 3。
- ⏹ **Batch 4**（`5266a1c`）：**做 3 項後主動停住**。抽出 `VanillaMasters`（build 唯一的檔案系統邊界，附 4 個單元測試）、`MakeLocationSlot` 歸位到唯一呼叫端、5 個 `TranslationMask` 變 `file static class`。`BuildContext` 欄位 57 → **25**。剩下的 Lighting／ExteriorCells／NpcPatches／Scene 幾群**量過之後決定不剝**——原排序只數欄位、沒數方法相依，實際每群要注入 4～10 個成員（含泛型方法、回寫的計數器、別人擁有的 queue），剝出來是包了儀式的 back-pointer。理由與量測表在計畫 Batch 4。
- ⏸ **Batch 5 未開始（選做）**：public surface 清理（`Generator` 上 ~46 個沒進文件的 public static、`OarGen.HkxCopy` 死碼、`SceneCoordinates` 沒接線、`Validate.Items2/World2` 改檔名、`PlayerRef`/`McmPlayerRef` 重複常數、4 個零測試覆蓋的檔）。

**⚠ 三件跨機／後續要注意的：**

- **這些 commit 全都還沒 push**，母 repo 的 submodule 指標也還沒 bump。另一台若有未推的 `src/` 改動，會撞上 Batch 1 的巨型 rename——先合再繼續。
- **文檔裡有 5 條既有死連結**（`src/ModForge.Cli/Build.cs`、`Generator.Build.SceneNpcRoles.cs`、`SceneImport.cs`、`StoryManagerProbe.cs`、`StoryManagerProbeTests.cs`），指向從來不存在或早已改名的檔。不是這次造成，屬文檔面向，另開一次。
- **`DEV-GUIDE.md` 還缺「平鋪太多即包夾」那條**（[refactor/README](README.md) 已經在引用它，本次重構就是它的第一個實例）。屬文檔面向，同上。
