# ModForge — In-Game Test Queue / 待實機測試清單

**還沒在遊戲裡確認過的東西**，以及**你（justty32）該怎麼測**，都記在這裡。

分工（跟 `docs/SESSION-LOG.md` 同理）：
- **CLAUDE.md** = durable 參考。
- **SESSION-LOG.md** = 進度日誌 / in-flight 狀態。
- **本檔** = **只列還沒在遊戲裡確認的待測項目** + 測試步驟。功能一旦 in-game 確認，就**從本檔移除**（不留已確認清單），並在 CLAUDE.md `已落地` 補上 in-game 確認註記；歷史記錄看 git log。
- 我（Claude）**不能跑遊戲**——只能結構性驗證（diag / 逐位元對齊）+ 打包；真正的驗收門檻是你實機跑。見 memory `ingame-test-workflow`。

---

## 怎麼測（通用流程）

1. **拿 zip**：我會把打包好的 zip 放到 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根目錄，不要有多層資料夾——曾因 zip 根有殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根目錄是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類要放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務類的鐵律**：
   - 對話只在**遊戲 LOAD** 時註冊 → 用**全新遊戲**，或任務啟動後 **save 再 load**；主選單 `coc` 進場不會註冊對話。
   - build/package 已自動寫 `Data/Seq/<plugin>.seq`，但**既有存檔**仍要 save+reload 才吃。
   - 強制天氣非侵入測：`sw <XX>000800`（XX = 該 plugin 的 load order 槽位 hex，MO2 右欄看）。build 成功後會印出 WTHR 的 `sw` 指令。
   - console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK、哪些怪、有沒有 CTD / 空白；附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：若我請你手動 patch 某檔進 MO2 mod 夾，從 zip 重裝會把它復原成 build 時的 mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

---

## 待測（active）

（目前無待測項目——全部已確認，已移除；歷史見 CLAUDE.md「已落地」與 git log）
