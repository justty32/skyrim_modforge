# src/ 拆檔重構整理 — 計畫

← [refactor 入口](README.md)｜[DEV-GUIDE 結構整理原則](../../DEV-GUIDE.md)｜[common/conventions](../common/conventions.md)

> 本檔是**一份完整 plan**，依 DEV-GUIDE「本質不可分的單體可超標保留」不套 8192 bytes 門檻。做完後整份移 `archive/`。

**Done when:** `src/ModForge.Core` 與 `src/ModForge.Cli` 的檔案依領域收進子資料夾；`Spec.cs`／`Generator.BuildContext.cs`／`Program.cs` 三個 hub 檔不再是「每加一個功能都要改」的必經點；`BuildContext` 可被測試直接建構；全程每個 batch 都通過 **1122 offline tests + 143 支 example spec 的 byte-level golden hash**。
**不包含**：改 `Build()` 的步驟順序、拆 Placement/Reference 核心、改 namespace、動 `docs/` 與 HTML。

---

## 1. 今天量到的現況（2026-08-13，離線機 Windows）

| 項目 | 數字 |
|---|---|
| `src/ModForge.Core` | **212** `.cs`，**全部平鋪、零子資料夾** |
| `src/ModForge.Cli` | **42** `.cs`，全部平鋪，其中 **41 個都是 `partial class Program`** |
| `tests/ModForge.Core.Tests` | **113** `.cs`，全部平鋪 |
| 合計 | **367 個檔案、0 個子資料夾** |
| namespace | 兩個 assembly **共用一個 `namespace ModForge`**，零分層 |
| src 總行數 | 26,018 行；**最大單檔 301 行** — 300 行規則已經達標 |
| `partial class Generator` | 119 個檔案；文件說有 2 個進入點（`Build`/`Validate`），實際有 **~48 個 public static 方法** |
| `Generator.BuildContext`（private nested） | **60 檔 / 8,097 行 / 82 個欄位 / 238 個方法** |
| offline 測試基準 | **1122 passed / 1 skipped / 0 failed，18 秒** |
| build 決定性 | **143/143 支 `examples/*.json` 全部 byte-deterministic**（每支連建兩次比 SHA256，零例外）|

**結論：300 行規則已經解決「單檔過大」，但把問題換成了「367 個平鋪檔 + 3 個跨檔 god class」。**
這正是 DEV-GUIDE 觸發 B（資料夾雜亂）與 refactor README 提到的「平鋪太多即包夾」。

> 附帶發現：`DEV-GUIDE.md` 只寫了觸發 A（單體過大）與觸發 B（資料夾雜亂），但 [refactor/README](README.md) 已經在引用第三條「**平鋪太多即包夾**」。本次重構正是它的第一個實例——做完把這條補回 DEV-GUIDE。

## 2. 真正的痛點不是檔案大小，是 hub 檔

`git log --since=2026-05-01 -- src` 共 375 個 commit，改動次數前五名：

| 檔案 | 被改次數 | 它是什麼 |
|---|---|---|
| `src/ModForge.Cli/Program.cs` | **95** | argv switch（每加一個命令就要改）|
| `src/ModForge.Core/Build/Generator.Build.cs` | **69** | build 步驟呼叫清單 |
| `src/ModForge.Core/Validate/Generator.Validate.cs` | **56** | validate 步驟呼叫清單 |
| `src/ModForge.Core/Spec/Spec.cs` | **55** | `ModSpec` 根 DTO（100+ 個 `List<XSpec>` 欄位）|
| `src/ModForge.Core/Build/Generator.BuildContext.cs` | **44** | 82 個欄位的狀態袋 |

**加一個 record family = 改這 5 個共用檔 + 新增 4~6 個檔。** 這才是摩擦來源，而且在**兩台機器交替開發**的情況下，這 5 個檔正好是最容易撞到 merge conflict 的位置。把檔案搬進資料夾**不會**改善這件事——第 3 節的 Batch 2 才會。

## 3. 批次計畫（一次一個面向，照 refactor README 的規矩）

### Batch 0 — 先蓋護欄（**其他批次的前置**）— ✅ 已完成 2026-08-13

- `scripts/golden-hash.sh <out> [jobs]`：build `examples/` 全部，逐一輸出 `.esp`／`.seq` 的 SHA-256。用法與警告寫進 [testing.md](../testing.md)。
- 重構前後各跑一次 `diff`，**任何一行變動 = 行為變了**，立刻停。
- 理由：**1107 個 test method 裡 965 個（87%）是 E2E**——只驗「spec 進去、plugin/warnings 出來」，對 `BuildContext` 內部怎麼重組**完全無感**。既是好消息（重構不會假性紅燈）也是壞消息（**內部搬錯位置它們抓不到**）。golden hash 補的就是這個洞。
- 實測：143 支 spec → **197 個產物**（143 esp + 54 seq），0 build failure，離線機 4 並行約 3 分鐘；**兩次獨立執行完全逐行相同**，且與另一支循序寫的探針腳本 143/143 吻合（證明並行不影響輸出）。

**兩個與原構想不同的決定：**

1. **只寫 `.sh`，不寫 `.ps1`。** 離線機有 Git Bash（`bash 5.3` + `sha256sum`），維護兩份同邏輯腳本是自找不一致。
2. **hash 清單不進版控**（原本想放 `tests/golden/plugin-hashes.txt`）。因為它**跟機器綁定**：離線機沒有 `Skyrim.esm`，凡是指向 vanilla cell 的 placement 都會被 skip（143 支加起來只有 422KB esp），與 Manjaro 上同一支 spec 的 bytes 不同。一份會過期、又會誘人跨機比對的檔案，價值低於風險——改成**在同一台機器產 before/after 自比**，腳本進版控、輸出不進。

### Batch 1 — 資料夾分層 — ✅ 已完成 2026-08-13

**344 個 `git mv`，零內容修改。** 落地佈局（權威表在 [CODE_MAP](../common/code-map/CODE_MAP.md)）：

| | Core | Cli | tests |
|---|---|---|---|
| `Spec/` | 52 | — | 6 |
| `Build/` | 69 | — | 67 |
| `Validate/` | 32 | — | 12 |
| `Macros/` | 6 | — | — |
| `Papyrus/` | 19 | — | 13 |
| `Formats/` | 10 | — | 6 |
| `Catalog/` | 3 | — | 3 |
| `Voice/` | 3 | — | 4 |
| `Commands/` | — | 11 | — |
| `Diagnostics/` | — | 29 | — |
| 根 | 18 | 2 | 2 |

**三項驗證全過**：`dotnet build` 乾淨、測試 **1122 passed / 1 skipped / 0 failed**（與 batch 前逐項相同）、golden hash **197/197 產物 byte-identical**。csproj 一個字都沒改（預設 glob）。

**做的時候調整的幾點：**

- `Generator.Build.cs` 也進 `Build/`（原稿讓它留在根）——`Generator.Validate.cs` 進了 `Validate/`，兩者不一致沒道理。根只留 `Generator.cs` 當函式庫入口。
- Cli 不開 `Export/`：`TexExport`/`NifExport`/`CatalogCmd`/`QuestNodeCmd` 本來就都是 CLI 命令，一起放 `Commands/` 比硬切一個 2 檔資料夾誠實。
- **`Generator.WordWall.cs` 進 `Build/` 而不是 `Macros/`**：它是 `BuildContext` 的 partial，不是 `Expand*` 那一段。`Generator.JContainers.cs` / `Generator.StorageWrites.cs` 同理進 `Papyrus/`（檔頭自述是「script-template snippets 生成」），所以 `Macros/` 只有 6 檔而非原估的 10。
- **文檔路徑一起改了，含 `*/archive/`**：299 行、33 個檔（其中 20 個在 archive）。archive 雖然凍結，但路徑改名不動任何論述，留 200 多條死連結比動它更糟。
- src 根還剩 18 個各不相干的型別（`Assets` `Demo` `QuestNodes` `SceneCoordinates` `StoryManager*` …）。**沒有硬塞一個 `Support/`**——那只是換個名字的雜物抽屜，依 DEV-GUIDE「不預先過度設計」，等它真的礙事再分。

> **順手發現的既有腐爛（不是本次造成，未修）**：文檔裡有 5 條指向從來不存在／早就改名的檔案——`src/ModForge.Cli/Build.cs`、`src/ModForge.Core/Generator.Build.SceneNpcRoles.cs`、`SceneImport.cs`、`StoryManagerProbe.cs`、`tests/.../StoryManagerProbeTests.cs`。屬於文檔面向，另開一次處理。

### Batch 2 — 拆 hub 檔 — ✅ 已完成 2026-08-13

五個 hub 檔的結果（每項獨立 commit，每項都跑完整驗證儀式）：

| # | 檔案 | 之前 | 之後 | commit |
|---|------|------|------|--------|
| 1 | `Spec/Spec.cs` | 159 行 / **102 個成員** | 43 行 / **5 個成員** | `c9c8207` |
| 2 | `Build/Generator.BuildContext.cs` | 278 行 / **57 個欄位** | 237 行 / **27 個欄位** | `807b0a6` |
| 3 | `Cli/Program.cs` | 204 行（switch＋help 全在裡面）| **77 行**（兩者都不在了）| `3104459` |
| 4 | `Validate/Generator.Validate.cs` | 4 個自由函式 + 35 個 ctx 方法 | 38 個 ctx 方法 + 1 個有理由的例外 | `f4035c9` |
| 5 | `Build/Generator.Build.cs` | — | **刻意不動**（理由寫進檔頭）| — |

1. **`Spec.cs`**：每個 `List<XSpec>` 屬性搬到**宣告 `XSpec` 的那個檔**（`weapons[]` 就在 `WeaponSpec` 旁邊），6 個 macro 展開 guard 跟著它守護的家族走。安全性已驗證：全 repo 沒有任何地方 `Serialize` 過 `ModSpec`，`CheckUnknownFields` 也是用 name→Type 字典查表，**宣告順序碰不到任何輸出**。
2. **`BuildContext`**：只有一個 build step 用得到的欄位（scene 的 wire queue、exterior cell 快取、region 計數器、lighting map、banter/dialogue 狀態、master cache 的 disposables）搬進那個 step 自己的 partial，註解一起走。partial class 的欄位在每個 part 都看得到，所以搬宣告不可能改行為，`ToResult()` 照舊讀得到計數器。**留下的判準寫進檔頭了**：只有「不只一個領域讀」的狀態才留在這裡。
3. **`Program.cs`**：命令的 **argv 形狀與 help 文字現在同住一個檔**——`Commands/Program.Dispatch.cs`（生成/打包/翻譯）與 `Diagnostics/Diagnostics.Dispatch.cs`（dump/find/*diag）。dispatcher 對不認得的形狀回 `null`，精確重現舊 switch 的 `default:`。57 行 help 一行沒少、字沒改，只是 3 個 translate 命令從 40 個 diag 後面移到核心區。
4. **`Generator.Validate.cs`**：`ValidateLights`/`ValidateLighting`/`ValidateWeather` 轉成 ctx 方法。**`ValidateRequires` 刻意留成自由函式**——它是 requires 模組的一半（另一半「宣告 vs 實際」必須 build 完才能跑），為了讓清單好看而拆散那一對不划算；理由寫在呼叫處。
5. **`Generator.Build.cs` 刻意不動。** 那 ~150 行的呼叫順序**就是 pipeline 的規格**，而且是這裡唯一「弄丟就再也回不來」的東西：FormID、進而整個 esp 的每個 byte 都由它決定。改成註冊表會把順序移到隱性的地方（宣告順序／反射順序／檔案順序），沒人 review、改動無聲落地。**這是評估後否決，不是漏做**，檔頭註解已經寫明，別再手癢。

> **新增的第二層護欄**：`scripts/cli-dispatch-snapshot.sh`。golden hash 只走 `build` 一條路徑、測試幾乎不碰 CLI，所以動 argv 分派時沒有網。它對 55 個命令 × 6 種參數長度＝**330 種 argv 形狀**記錄 exit code 與「有沒有掉回 Usage()」，改前改後 diff。用法見 [testing.md](../testing.md)。

### Batch 3 — 讓 `BuildContext` 可被測

`ModForge.Core.csproj` 已經有 `<InternalsVisibleTo Include="ModForge.Core.Tests" />`，但 `BuildContext` 是 `Generator` 裡的 **`private sealed partial class`**，測試碰不到——這就是 87% 測試被迫走 E2E 的**唯一結構原因**。

- 把 `BuildContext` 從 nested private 提升為 top-level `internal sealed partial class`（Batch 1 之後它已經在 `Build/` 資料夾裡了）。
- 這是**單純的可見度 + 去巢狀**改動，零行為風險，但立刻讓 Cluster 4/6/7（見下）可以寫真正的單元測試。

### Batch 4 — 從 god object 剝出獨立單元（**選做，投報率遞減**）

耦合分析把 60 個 `BuildContext` 檔分成 8 群。**照這個順序做，隨時可以停**：

| 順位 | 群 | 檔數 | 縫的成本 |
|---|---|---|---|
| 1 | `Build.Items.cs` 的 5 個 `TranslationMask` | 1 | 零——是 `static readonly` 常數，根本不是狀態，今天就能搬成 `static class` |
| 2 | Lighting（`lgtmByEd`/`imgsByEd`）| 1 | 零，100% 自有 |
| 3 | ExteriorCells + Regions | 2 | 零，8 個欄位全部自有 |
| 4 | `BuildContext.Utilities`（master cache / strings）| 1 | 幾乎零——**它已經長得像一個 service 了，當作其他群的樣板** |
| 5 | NpcPatches | 1 | 只有 `npcPatchesByRef` |
| 6 | Scene（`scenesBuilt` 等 6 個欄位全自有）| 1 | 只需要注入 `questsByEd` |
| 7 | Scene/Dialogue/Quest 其餘 | 10 | 只共用 `questsByEd` + `dialogResponsesByEd` |
| 8 | Navmesh | 5 | 要向 Placement 群借 `vanillaCellOverrides`/`builtPlacements` → 改成注入唯讀 view |

**明確不做**：Placement / PlacementRefs / References / Packages / Packages.Advanced / Removals / Conditions 這 7 個檔。`Placements.cs` 一個檔就碰 82 個欄位裡的 19 個，是所有 deferred-wire queue 的匯流點，而且程式碼自己的註解就寫著這段的 two-pass 順序是 **LOAD-BEARING**。**把它當成一個不可分的單元，不要試著一片片剝。**

### Batch 5 — public surface 清理（收尾）

- `Generator` 上 ~46 個沒進文件的 public static 方法：確認哪些是 CLI 真的在用的 API、哪些該降 `internal`。
- `OarGen.HkxCopy`：自己的檔案外沒有任何引用。
- `SceneCoordinates` / `SceneCoordinateProfile`：只有測試碰，沒接進任何 build/import 路徑——確認是預留還是死碼。
- `Generator.Validate.Items2.cs` / `World2.cs`：確認過是純粹的 300 行溢出（`ValidateItems2` 就是 `ValidateItems` 那張表的後半段），Batch 1 搬進 `Validate/` 後改成有意義的檔名。
- `PlayerRef`（`Build.Identity.cs`）與 `McmPlayerRef`（`Build.Mcm.cs`）是同一個字面值 `"Skyrim.esm:0x000014"` 的兩份宣告，合成一個。
- 零測試覆蓋的檔：`Fuz.cs` / `Translator.cs` / `Archives.cs` / `Papyrus.cs`——不在本次範圍，但記一筆。

## 4. 每個 batch 的驗證儀式（固定不變）

```bash
dotnet build src/ModForge.Cli/ModForge.Cli.csproj
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
scripts/golden-hash.sh /tmp/before.txt   # 動工前
scripts/golden-hash.sh /tmp/after.txt    # 動工後
diff /tmp/before.txt /tmp/after.txt
git diff --stat                          # 確認這個 commit 只動了一個面向
```

三項全過才 commit，然後才開始下一個 batch。跨 batch 不合併 commit。

## 5. 風險

| 風險 | 對策 |
|---|---|
| 動到 `Build()` 順序 → FormID 全變 → 輸出 esp 不同 | golden hash 直接抓；Batch 2 明列「不動 `Generator.Build.cs`」 |
| Batch 1 的巨型 rename diff 蓋掉真正的改動 | rename 單獨 commit，`git log --follow` 仍可追 |
| CODE_MAP 五份子 index 全部要改路徑 | 屬於維護鏈第 2 面向，Batch 1 的**同一個 commit** 裡改完（conventions 要求）|
| 兩台機器同時動 src | 這件事**必須在一台機器上連續做完**，做的期間另一台不要碰 `src/` |
| 87% 測試是 E2E，抓不到內部搬錯 | 就是 Batch 0 存在的理由；Batch 3 之後才開始有真正的單元測試 |
