# ModForge — In-Game Test Queue / 待實機測試清單

**還沒在遊戲裡確認過的東西**，以及**你（justty32）該怎麼測**，都記在這裡。

分工（跟 `docs/SESSION-LOG.md` 同理）：
- **CLAUDE.md** = durable 參考。
- **SESSION-LOG.md** = 進度日誌 / in-flight 狀態。
- **本檔** = 待實機測試的具體項目 + 測試步驟。功能一旦 in-game 確認，就把它從「待測」搬到「已確認」尾段（附日期），並在 CLAUDE.md `已落地` 補上 in-game 確認註記。
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

### 1. 任務標記大地圖修復（quest-markers map-fix）— 第三次嘗試

- **是什麼**：`mapMarkers[]`（XMRK 地圖圖示）+ `placements[].kind:"xmarker"/"xmarkerHeading"` 放進 **vanilla Tamriel**。前兩次踩到**大地圖全空白 + 載 actor CTD**，根因已定位=worldspace override 的持久 cell（Tamriel TopCell 0xD74）必須(1)加性帶上、(2)複製記錄標頭旗標 `MajorRecordFlagsRaw=0x00040400`、(3)ref 自身帶 0x400 持久旗。修好後 esp 已逐位元對齊 vanilla/USSEP。細節見 memory `worldspace-override-must-carry-topcell`。
- **zip**：`~/skyrim_mods/mine/ModForgeQuestMarkers-mapfix.zip`（修復版）；`ModForgeQuestMarkers.zip` = 安全版（不放進 vanilla Tamriel，只測 A 任務日誌+羅盤）。
- **怎麼測**：
  1. 裝 mapfix 版，新遊戲或乾淨存檔，去 Riverwood 一帶。
  2. **開大地圖** → 確認：(a) 地圖**有正常底圖、不是全空白**；(b) 我們的地圖標記有出現。
  3. **在 Riverwood 附近走動、讓 NPC/actor 載入**（如 Dorthe、河木鎮居民）→ 確認**不 CTD**（前兩次就是這裡崩）。
  4. 任務日誌應有雙目標、羅盤有箭頭（A 已確認過，順帶複驗）。
- **要盯的 UNKNOWN**：load order 放在 USSEP 之後；若仍崩，抓 CrashLogger log 看是不是還在 cell-load。
- **已確認的部分**：A（任務日誌雙目標 + 羅盤箭頭）✅。

---

## 本 session 離線做好、未打包（要測再跟我說，我打包）

這些都已 offline 測試 + 結構驗證，但**還沒打包成可實機的 zip**。多為宣告式 record/條件接線，會疊進你現有的 spec 用；想實機驗哪個跟我說，我 package 成 zip 放 `~/skyrim_mods/mine/`。

- **`npcPatches[]` override vanilla NPC + 換 AI 包**（已用 `npcdiag` 驗 Carlotta 整筆 override + 英文名 inline）。**怎麼測**：裝後去找該 NPC，觀察行程是否換成新 package（如叫她待在家/去酒館）；注意 USSEP/AI Overhaul load-order 衝突。
- **新 CTDA 函式 + INFO 旗標**（`GetIsAliasRef` 等 10 個、`sayOnce`/`walkAway`…）。**怎麼測**：寫一條用 `GetIsAliasRef` 綁 alias 的對白或 `sayOnce` 一次性台詞，跑對話確認閘門/一次性行為。
- **MGEF：Script-archetype 掛 Papyrus + DualValueModifier 第二 AV**。**怎麼測**：做個 `archetype:"Script"` + `scripts[]` 的法術看腳本有沒有跑；DualValueModifier 看是否同時動兩個屬性。
- **Sleeping Giant Inn 逆向 `examples/sleeping_giant_inn.json`**（423 placements 進 vanilla 室內）。**怎麼測**：build+package 後進睡巨人客棧看擺設；**注意**檔頭警告——含 3 個 NPC override 會**複製** vanilla Delphine/Orgnar/Embry，純擺設測試請拿掉 npc 項。

---

## 已確認（in-game confirmed，新→舊；詳見 CLAUDE.md「已落地」與 git log）

- 身份系統 Phase-2/C 全套（2026-06-07）、語音管線 + lip sync（2026-06-13）、明亮室內/室外光照（2026-06-09）、Scene PlayIdle（2026-06-07）、SM Kill→quest（2026-06-05）、自訂對話（含 .seq）、navmesh 自訂 worldspace、follower 等。歷史完整清單在 CLAUDE.md。
