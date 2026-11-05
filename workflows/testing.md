# Testing — 跑測試

← [INDEX](../INDEX.md)｜跨機開發/離線環境見 [dev-env.md](dev-env.md)

ModForge 只有一個測試專案：

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj
```

大多數測試是純 .NET 的結構測試。它們可能引用外部 FormKey（如 `Skyrim.esm:0x013746`），但**不會開啟 `Skyrim.esm`**。

## 日常跑法（離線，任何機器）

日常迴歸排除需本機 `Skyrim.esm` 的測試——這些測試會 clone vanilla 模板或複製 vanilla cell/worldspace context，都已標記 `Category=RequiresSkyrim`：

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
```

離線機也可跑 `scripts/test-offline.sh`（同一條 `--filter "Category!=RequiresSkyrim"` 的封裝；`scripts/*.sh` 需經 bash，見 [dev-env.md](dev-env.md)）。

離線 suite 的 `VoiceLiveContractTests` 會用 production `Voice.GenerateWav()` 真正啟動同層
`../skyrim-voicegen/voicegen.py`，最末端才換成 deterministic fake Fish engine；不需語音模型。
測試建立當前平台 wrapper，並同時生成另一平台版本以釘死 wrapper 形狀。若 sibling checkout
或 Python runtime 不在場，該 case 會帶理由明示 skip；可用
`MODFORGE_VOICE_CONTRACT_PYTHON` 指定 Python executable。

### 已知問題：dynamic skip 被 runner 當成 Failed

fresh clone、任何 git worktree 或缺同層 `skyrim-voicegen` checkout 時，離線 suite 回報 `Failed: 1`（非 skip）：`$XunitDynamicSkip$sibling skyrim-voicegen checkout is unavailable`。
`tests/ModForge.Core.Tests/Voice/VoiceLiveContractTests.cs` 的 `GenerateWav_ProductionVoicegen_HonorsProcessContract` 用 `SkipException.ForSkip`；目前 xUnit／VSTest runner 卻當失敗。這是測試基礎建設問題，非產品 bug，也非你改壞。
重現：`git worktree add --detach /tmp/x <任一 commit>`，在 `/tmp/x` 跑離線 suite → `Failed: 1`。
繞法：`ln -sfn <工作區>/projects/skyrim-voicegen /tmp/skyrim-voicegen`，再跑 → `Failed: 0`。
**決定不修**；建議改真正的 `Skip` 屬性或條件式 Fact。

### Modlist snapshot fixture 換行契約

`fixtures/modlist-snapshot/lf/*.txt` 必須以 LF checkout；`crlf/*.txt` 保留儲存的 CRLF 位元組（`-text`，不做 Git 換行轉換）。根 `.gitattributes` 僅限定這兩組 fixture，不依賴全域 `core.autocrlf`，也不改 BOM。六檔目前均無 BOM，每檔六行；`ModlistSnapshotTests.Fixtures_CrlfAndLfProduceByteIdenticalJson` 檢查兩組 `modlist.txt` 換行，並驗證三份 profile 清單解析後 JSON 一致。

既有 checkout 更新屬性後若仍持有舊換行，先確認 fixture 無本機修改並搬到 repo 外備份，再於 Bash 用 `git ls-files -z -- fixtures/modlist-snapshot | git checkout-index --force -z --stdin` 重新 checkout 六檔。跨機驗證應在乾淨暫存 checkout，分別以 `core.autocrlf=true`、`false`、`input` 核對六檔與 Git blob 的 SHA-256 相同；Windows 使用 Git for Windows 與原生 .NET，WSL 的 Git／位元組驗證不代表已跑 Linux .NET runtime。

## 重構護欄

golden hash（`scripts/golden-hash.sh`）、package（`scripts/package-snapshot.sh`）、CLI dispatch（`scripts/cli-dispatch-snapshot.sh`）三道護欄 → [testing/refactor-guards.md](testing/refactor-guards.md)。

## 找洞用：coverage（`scripts/coverage.sh`）

上面三支護欄（[testing/refactor-guards.md](testing/refactor-guards.md)）回答「行為變了沒」，**都不回答「哪裡根本沒被跑到」**——那是你動手寫測試前想知道的事。這支包住 Microsoft.NET.Test.Sdk 內建的 collector（不需額外套件，離線可跑），按**未覆蓋行數**排序 ModForge 自己的檔案：

```bash
scripts/coverage.sh                 # 離線跑，報告印到 stdout
scripts/coverage.sh /tmp/cov.txt    # 順便存一份
```

第三方 source-linked 相依（DynamicData／Humanizer／Mutagen）與 `obj/` 生成碼會被濾掉，否則真正的結果會被埋掉。

⚠️ **看數字前先看 filter**。預設排除 `Category=RequiresSkyrim`，所以**只有那些測試才走得到的程式碼會顯示成沒覆蓋**——這是「離線機能回歸測到什麼」的實話，但**不等於那段程式碼沒測試**。下結論前先確認該檔的 `*Tests.cs` 是不是整份都標了 `RequiresSkyrim`：例：`LivingNpcTests` 每一條都要 `Skyrim.esm`，所以 `Generator.LivingNpcs.cs` 離線讀起來覆蓋極低，而不是因為它沒測試。要含 RequiresSkyrim 一起算：

```bash
MODFORGE_COVERAGE_FILTER= scripts/coverage.sh
```

零覆蓋的大宗是 `ModForge.Cli/Diagnostics/*`（要真的 `Skyrim.esm` 才 dump 得出東西）與 `Papyrus.cs`（要 Wine/CK），兩者離線都測不了。

## RequiresSkyrim 跑法（需本機 Skyrim.esm）

標記 `Category=RequiresSkyrim` 的測試需要本機的 Skyrim Special Edition `Data` 資料夾（內含 `Skyrim.esm`）。生成器優先讀 `MODFORGE_SKYRIM_DATA`，未設時回退到本機 Steam 路徑：

```bash
export MODFORGE_SKYRIM_DATA="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data"
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category=RequiresSkyrim"
```
