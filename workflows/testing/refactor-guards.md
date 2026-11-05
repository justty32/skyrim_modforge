# Testing — 重構護欄（golden hash / package / CLI dispatch）

← [testing](../testing.md)（入口）

## 重構護欄：golden hash（`scripts/golden-hash.sh`）

行為不變的重構（[refactor 工作流](../refactor/README.md)）光靠上面的測試（[testing](../testing.md) 日常跑法）**不夠**：約八成 test method 只走 `Generator.Build`/`Validate` 對記錄下斷言，內部怎麼重組它們一律看不見。`golden-hash.sh` 補這個洞——把 `examples/` 全部 build 一次，逐一輸出 `.esp`／`.seq` 的 SHA-256：

```bash
scripts/golden-hash.sh /tmp/before.txt        # 重構前（第二參數＝並行度，預設 4）
# ...重構...
scripts/golden-hash.sh /tmp/after.txt
diff /tmp/before.txt /tmp/after.txt && echo "byte-identical"
```

**任何一行變動＝輸出變了＝這次重構不是 behavior-preserving。** 離線機約 3 分鐘。

⚠️ **跑的時候不能有別的東西在 build 這個 repo**。三支護欄都是直接執行
`src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli`；別條 agent 線、IDE 或 watcher 只要跑一次
`dotnet build`，這顆執行檔就會被刪掉重寫，**當下還沒跑到的 spec 全部 exec 失敗**。
腳本會在 run 前後對執行檔取 SHA-256 指紋，一旦中途變了就
**印錯誤並 `exit 2`，不交出可以拿去 diff 的檔案**（exec 失敗記成 `HARNESS-FAIL`，跟
spec 自己 build 失敗分開）。看到 `ABORTED` 就是這件事，把別的 build 停掉重跑即可。

⚠️ **輸出跟機器綁定，不要 commit、不要跨機比對**：離線機沒有 `Skyrim.esm`，凡是指向 vanilla cell 的 placement 都會被 skip，產出的是縮水版 plugin，bytes 與 Manjaro 上同一支 spec 不同。永遠**在同一台機器上比 before/after**。

## 重構護欄：package（`scripts/package-snapshot.sh`）

`golden-hash.sh` 只走 `build`，碰不到真正出貨用的 `package`。修改 package 編排、Papyrus
編譯或 loose-file 出貨時，用這支把所有 example 的正規化 stdout/stderr、產物路徑與內容
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
