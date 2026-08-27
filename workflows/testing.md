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

## 重構護欄：golden hash（`scripts/golden-hash.sh`）

行為不變的重構（[refactor 工作流](refactor/README.md)）光靠上面的測試**不夠**：1203 個 test method 裡約八成只走 `Generator.Build`/`Validate` 對記錄下斷言，內部怎麼重組它們一律看不見。`golden-hash.sh` 補這個洞——把 `examples/` 全部 build 一次，逐一輸出 `.esp`／`.seq` 的 SHA-256：

```bash
scripts/golden-hash.sh /tmp/before.txt        # 重構前（第二參數＝並行度，預設 4）
# ...重構...
scripts/golden-hash.sh /tmp/after.txt
diff /tmp/before.txt /tmp/after.txt && echo "byte-identical"
```

**任何一行變動＝輸出變了＝這次重構不是 behavior-preserving。** 143 支 spec ／ 197 個產物，離線機約 3 分鐘。

⚠️ **跑的時候不能有別的東西在 build 這個 repo**（2026-08-27 修）。三支護欄都是直接執行
`src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli`；別條 agent 線、IDE 或 watcher 只要跑一次
`dotnet build`，這顆執行檔就會被刪掉重寫，**當下還沒跑到的 spec 全部 exec 失敗**。
實測：run 開始 3 秒後插一次 `-t:Rebuild`，143 支全綠的 spec 變成 135 支 `BUILD-FAIL`。

舊版會把這種 exec 失敗**記成 `BUILD-FAIL`**——也就是記成「你的重構把輸出改掉了」，正好是這支
腳本唯一該講準的結論。現在兩支都會在 run 前後對執行檔取 SHA-256 指紋，一旦中途變了就
**印錯誤並 `exit 2`，不交出可以拿去 diff 的檔案**（exec 失敗另記成 `HARNESS-FAIL`，不再跟
spec 自己 build 失敗混為一談）。看到 `ABORTED` 就是這件事，把別的 build 停掉重跑即可。

⚠️ **輸出跟機器綁定，不要 commit、不要跨機比對**：離線機沒有 `Skyrim.esm`，凡是指向 vanilla cell 的 placement 都會被 skip，產出的是縮水版 plugin，bytes 與 Manjaro 上同一支 spec 不同。永遠**在同一台機器上比 before/after**。

## 重構護欄：package（`scripts/package-snapshot.sh`）

`golden-hash.sh` 只走 `build`，碰不到真正出貨用的 `package`。修改 package 編排、Papyrus
編譯或 loose-file 出貨時，用這支把 143 個 example 的正規化 stdout/stderr、產物路徑與內容
SHA-256 做成快照：

```bash
scripts/package-snapshot.sh /tmp/before.txt   # 第二參數＝並行度，預設 4
# ...重構...
scripts/package-snapshot.sh /tmp/after.txt
diff /tmp/before.txt /tmp/after.txt && echo "package unchanged"
```

`.pex` 只比路徑、不比內容：Papyrus compiler 會寫入時間戳，同一顆 CLI 對同一支 spec 連跑兩次
bytes 也不同；其他產物照常雜湊。每個 parallel worker 先寫自己的 report，最後才依檔名排序
串接，避免 `xargs -P` 交錯輸出。這支也有 CLI SHA-256 前後指紋；中途遇到 concurrent build
會印 `ABORTED` 並 `exit 2`，不可拿該次結果做 diff。

## 重構護欄：CLI dispatch（`scripts/cli-dispatch-snapshot.sh`）

golden hash 只經過 `build` 一條路徑，測試也幾乎不碰 `ModForge.Cli`——**改 CLI 的 argv 分派時用這支**。它對每個命令名餵 0～5 個佔位參數，記錄 `exit code` 與「有沒有掉回 Usage()」：

```bash
scripts/cli-dispatch-snapshot.sh /tmp/before.txt
# ...改 dispatch...
scripts/cli-dispatch-snapshot.sh /tmp/after.txt
diff /tmp/before.txt /tmp/after.txt && echo "dispatch unchanged"
```

55 個命令 × 6 種長度 ＝ 330 種 argv 形狀。**`usage=yes` 代表那個形狀沒被接受**，所以 diff 一乾淨就等於「接受的參數形狀完全沒變」。佔位參數指向不存在的檔案是刻意的——命令真的被分派到才會因為找不到檔案而失敗，這正是它跟 fall-through 的區別。

## 找洞用：coverage（`scripts/coverage.sh`）

上面三支護欄回答「行為變了沒」，**都不回答「哪裡根本沒被跑到」**——那是你動手寫測試前想知道的事。這支包住 Microsoft.NET.Test.Sdk 內建的 collector（不需額外套件，離線可跑），按**未覆蓋行數**排序 ModForge 自己的檔案：

```bash
scripts/coverage.sh                 # 離線跑，報告印到 stdout
scripts/coverage.sh /tmp/cov.txt    # 順便存一份
```

第三方 source-linked 相依（DynamicData／Humanizer／Mutagen）與 `obj/` 生成碼會被濾掉，否則真正的結果會被埋掉。2026-08-13 基準：**73.0%**（27500/37669）。

⚠️ **看數字前先看 filter**。預設排除 `Category=RequiresSkyrim`，所以**只有那些測試才走得到的程式碼會顯示成沒覆蓋**——這是「離線機能回歸測到什麼」的實話，但**不等於那段程式碼沒測試**。下結論前先確認該檔的 `*Tests.cs` 是不是整份都標了 `RequiresSkyrim`：`Generator.LivingNpcs.cs` 之前讀起來是 4%，就是因為 `LivingNpcTests` 每一條都要 `Skyrim.esm`，而不是因為它沒測試。要含 RequiresSkyrim 一起算：

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
