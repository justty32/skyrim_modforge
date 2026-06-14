# wait_user — 等待使用者的事

← [CLAUDE.md](CLAUDE.md)｜[workflows/INDEX](INDEX.md)

需要**你（justty32）親自做 / 驗證**才能繼續的事，全列這裡——不只遊戲實機，也包含 **bash 指令、環境變數設定、權限測試、Nexus 下載 mod、外部工具實跑**等。我能做結構性驗證 + 打包；跨不過去的那一關記這裡等你。

**只列還沒做的**——做完即移除（不留已完成清單）；功能類確認後濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，歷史看 git log。

> **膨脹就拆**：本檔若因等你做的事太多而過大，就在 repo 頂層新立 **`wait_todo/`** 資料夾，按類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的待你項，② 收**不屬任何工作流**的雜項（bash/env/權限/Nexus…）——後者堆太多時就是拆進 `wait_todo/` 的觸發。

## 各工作流的待你項

屬於某工作流的待你事項連到該工作流（`workflows/<wf>/`）。目前各工作流無此類 open 項目。

## 不屬任何工作流的（堆太多 → 拆進 `wait_todo/`）

- **環境設定**（env var / 權限 / 本機工具安裝）：（無）
- **外部資源**（Nexus 下載 mod / 外部工具實跑）：（無）
- **需你跑的 bash / 指令**：（無）

## 實機測試（in-game，MO2 / Proton）

我**不能跑遊戲**，只能 diag / 逐位元對齊 + 打包；實機驗收靠你（memory `ingame-test-workflow`）。

**怎麼測（通用流程）**
1. **拿 zip**：我把打包好的 zip 放 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根，別有多層；曾因 zip 根殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務鐵律**：對話只在**遊戲 LOAD** 時註冊 → 用全新遊戲或任務啟動後 save+reload（`coc` 不註冊）；既有存檔要 save+reload 才吃 `.seq`；強制天氣 `sw <XX>000800`（XX=load order 槽位 hex，build 會印）；console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK／怪／CTD／空白，附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：手動 patch 進 MO2 mod 夾的檔，從 zip 重裝會復原成 build-time mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

**待測（active）**：（目前無——全部已確認移除；歷史見 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md) 與 git log）
