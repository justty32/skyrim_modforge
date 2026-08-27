# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [DEV-GUIDE「膨脹即拆」](../../DEV-GUIDE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### 2026-08-27 上午那一輪（`opus-modforge` 第一段）——已做完的部分不列

前一輪（Batch 0–5）收在「最大單檔 301 行」。本輪重新量：`src/` 已長到 28,891 行，
違反 300 行規則的只剩 2 個檔，都已拆掉（`Generator.Build.Dialogue.cs` 325→240、
`Package.cs` 311→94）。**檔案大小這個面向基本上到頂了**，剩下的是**方法長度**——
那是 300 行規則量不到的東西（一個 280 行的檔可以只有一個 250 行的方法）。

### 2026-08-27 下午這一輪（`opus-modforge` 第二段）——open ① 與 ② 都收掉了

**open ① 三個過長方法：全部拆完。** 判準沿用上一段那句「有作者編號過的階段就沿著切，沒有就別硬切」：

| 方法 | 前 | 後 | 怎麼切的 |
|---|---|---|---|
| `ValidateQuestsAndDialogue` | 271 | **8** | 它其實是**四段各自獨立的驗證**（quest／conditionTemplate／dialogue／scene），段與段之間只有一個共享資料 `stageIndexByQuest`。所以本體只留分派，四段成為 partial 方法，共享資料用**回傳值明著傳**，不留隱藏狀態。拆進 `Generator.Validate.Quests.Dialogue.cs`／`.Scenes.cs` |
| `BuildPlacements` | 207 | **52** | 單一大迴圈，但迴圈體是「找 cell → 建記錄 → 套屬性 → 登記 → 決定要不要 persistent」五關。因為 `BuildContext` 是**實例**，五個 helper 直接拿得到全部欄位，**不用包 context 物件**——這是它跟 `BuildWorldspaces` 的關鍵差別。拆進 `Generator.Build.Placements.Record.cs` |
| `BuildWorldspaces` | 246 | **127** | **只拆得動一半，而且是刻意的。** 它是 `static`，中間那個 `EmitCell` local function 捕獲了 6 個外層變數並回寫 2 個計數器——把它整個剝出去就是 Batch 4 判定過「包了儀式的 back-pointer」那種東西。所以只搬走**五段真的不捕獲可變狀態**的：`ApplyWorldDefaultsAndMap`／`ResolveTextureLayers`／`GetOrAddSubBlock`／`BuildCellLandscape`／`EmitHeightmapCells`（進 `Generator.Build.Worldspace.Terrain.cs`），`EmitCell` 本身留在原地並在註解裡寫明為什麼留 |

搬移期間踩到的兩個機械性坑，兩個都是**「把迴圈體抽成方法」才會出現、編譯器會擋下來**的那種：
`continue` 抽出去之後不在迴圈裡（改成 `return null`，呼叫端補 `if (x is null) continue;`）、
以及具名引數 `navmesh: false` 傳給 `Action<>` 委派時不成立（委派參數名是 `arg1..argN`，改成位置引數）。

驗收：測試 **1203/0**（與基線相同）；三支護欄全部 **byte-identical**——golden-hash 197 artifact、
cli-dispatch 330 shape、package-snapshot 1433 行。before 基線是**先 `git stash` 掉本輪改動再取的**，
因為第一次取基線時背景腳本與我的編輯重疊，不能保證它量到的是乾淨樹。

**open ② 73 個壞掉的 markdown anchor：查清楚了，73 = 60 + 13，而且 60 那組不是文件的錯。**

- **13 條是真的斷**（本輪已修）。全部是同一個模式：`docs/zh-TW/` 把標題譯成中文之後，
  指過去的交叉引用（含**同檔案內的自我參照**）還留著英文版的 anchor。修法**不是**去反推中文標題的新 slug
  （下次再改標題就再斷一次），而是在被指的標題上方補一個**穩定英文 `<a id="…">`**，id 直接沿用英文鏡像的 slug——
  這樣 EN 版與 zh-TW 版的連結文字變成同一串，鏡像維護少一個漂移點。另有 1 條
  （`sub_projs/inworld-skill-tree/`）是手寫 anchor 多算一個 `-` 的筆誤，同樣給它穩定 id 並改連結。
- **60 條是母 repo 那支 `tools/check_markdown_links.py` 自己算錯 slug**，文件與連結都沒問題。
  兩個 bug 都在 `github_heading_slug()`：① 最後一步 `re.sub(r"\s+", "-")` 把**連續空白壓成單一 `-``**，
  但真實 github-slugger 是**每一個空白各換一個 `-`**——所以 `## A — B` 砍掉 em dash 後留下兩個空白，
  真實 anchor 是 `a--b`，腳本卻算成 `a-b`；② 字元過濾只砍標點與 **ASCII** 符號，非 ASCII 的符號
  （`→`、`✅`）會原樣留在 slug 裡，真實 slugger 會砍掉。
  **這支腳本不在本線領地**（母 repo 根的 `tools/`），所以只交出已驗證的 patch，沒有動它：
  `agentctl/handoffs/opus-modforge-2026-08-27/check_markdown_links-slug-fix.patch`。
  驗證方式是複製一份到 scratchpad 改，然後跑三次：修正版對本 repo 從 60 → **0**；
  修正版對**未修 anchor 的樹**仍精準抓出那 13 條（證明它沒有變寬鬆）；
  修正版對**整個母 repo** 的結果與現行腳本完全相同（同樣只剩 1 條既有的 missing file，
  在 `cx-convert` 的 handoff 裡，與 slug 無關）——也就是不會誤傷別的 repo。

> 這是 `agentctl/docs/driving-codex.md` 第五節那條「**會回報都沒事的檢查比沒做更危險**」的鏡像版本：
> 這支檢查沒有說謊成「都沒事」，它說謊成「有 73 個問題」。後果比較輕，但代價一樣——
> 沒人會去看一份 82% 是雜訊的報告，於是那 13 條真的斷了的連結就跟著被埋了半年。

**下一輪的 open（本輪量的）**：方法長度這條線還沒到底，只是不再有 200 行以上的了。
現在 `src/` 最長的十個方法是 `ValidateNpcs` 195、`BuildScenes` 179、`BuildQuestAliases` 175、
`BuildConditionData` 171、`GenerateQuestFragmentSource` 166、`ValidateWorld` 163、
`ExpandLivingNpcs` 162、`WirePerks` 154、`Build` 154、`DumpRecordMagicAiAndText` 141。
**要不要往下拆是個判斷，不是規則**——本輪三個標的之所以值得動，是因為它們都在 200 行以上
且內部是**多個彼此不相干的階段**；上面這十個要先各自問過那個問題再動，不要照名次往下刷。

**不打算做的**：`Generator.Build.Scene.cs` 停在 301 行（超標 1 行）。
前一輪就是收在這個數字並判定達標，為了 1 行去動它正好是「為拆而拆」。


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

**一件留給你拍板的（不是重構決定）：**

- 要不要把 ~100 個 public 成員降 `internal`（要先給 Cli 加 `InternalsVisibleTo`，並決定有 SPEC 文件的型別算不算對外契約）——量測與選項在計畫 Batch 5。

### 覆蓋率收尾（2026-08-13 續做，已 commit）

原本「`Archives.cs` / `Papyrus.cs` 零測試」那條是**用型別名 grep 猜的**，不準。改用 `scripts/coverage.sh`（新增，見 [testing.md](../testing.md)）實測，離線基準 **73.0%**，結論修正如下：

- ✅ **`Archives.cs` 0% → 100%**（`334c378`）。之前測不了是因為**沒東西生得出 `.bsa`**：Mutagen 0.53.1 只能讀不能寫，repo 也不可能 commit Bethesda 的檔。補了 `tests/.../Formats/TestBsa.cs`（最小 SSE v105 writer，只做 Archives 用得到的部分）。48 行裡挖出**三個真 bug**：① filter 永遠不匹配（CLI 用 `/` 組 filter、archive 存 `\`）；② 抽出來的路徑沒跨平台正規化（`\` 在 Linux 不是分隔符，會生出一個檔名含反斜線的單檔，`voice-annotate` 靠檔名 parse FormID 就全爛）；③ 沒擋 path traversal（`.bsa` 是使用者指來的**不可信輸入**），改走既有的 `SafeOutputPath.ResolveUnder`。
- ✅ **`Generator.LivingNpcs.cs` 4% → 100%**（`3aefefd`）。它**不是**沒測試——是 `LivingNpcTests` 每一條都標 `RequiresSkyrim`（測試會 build，而 build 要把 anchor 放進 vanilla cell），離線一 filter 就全跳過。但 `ExpandLivingNpcs` 是純 spec→spec，展開本身根本不需要 `Skyrim.esm`，所以另立 `LivingNpcExpansionTests.cs` 直接驅動展開器、離線可跑。
- ❌ **`Papyrus.cs` 296 行仍 0%**——要 Wine/CK，離線真的測不了。零覆蓋的另一大宗是 `Cli/Diagnostics/*`（要真的 `Skyrim.esm` 才 dump 得出東西）。

測試數 1141 → **1183**。護欄：golden-hash **197/197 byte-identical**、CLI dispatch **330/330 相同**。

> ⚠️ 讀 coverage 數字的陷阱（已寫進 testing.md）：預設排除 `RequiresSkyrim`，所以「只有那些測試走得到的碼」會顯示成沒覆蓋——**那不等於沒測試**，LivingNpcs 就是這樣被誤判成 4% 的。
