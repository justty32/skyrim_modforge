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

### 1. 任務標記大地圖修復（quest-markers map-fix）— 第六次嘗試

- **累積結果（2026-06-13 第五次，TNAM/UNAM 修復）**：
  - ✅ CTD 消失、marker 可見、可傳送、不摔死（Z=-3800 修正落地）
  - ❌ 大地圖**仍白茫茫**（TNAM/UNAM 不是根因）
- **根因（第六次，2026-06-13）**：binary 比對 vanilla Tamriel WRLD 後發現兩個缺漏：
  1. **LargeReferences（RNAM ×8455）**：LOD 大物件清單，世界地圖渲染需要；我們從未複製
  2. **OffsetData（OFST，45600 bytes）**：引擎 cell-streaming offset table；從未複製
  已在 `CopyWorldspaceEnv` 補上兩者（RNAM DeepCopy loop + `od.ToArray()` for OFST）。ESP 從 ~5KB 增到 1.4MB（WRLD 記錄大小接近 vanilla）。511 tests pass。
- **zip**：`~/skyrim_mods/mine/ModForgeQuestMarkers-mapfix.zip`（第六次，RNAM+OFST 修復後重建）
- **怎麼測**：裝好 → 開大地圖確認 Tamriel 底圖有**地形貼圖**（目標：看得到山丘/雪地/海岸等背景地形，不是全白）。

---

## 已打包待測（zip 在 ~/skyrim_mods/mine/）

（目前無其他待測項目）

---

## 已確認（in-game confirmed，新→舊；詳見 CLAUDE.md「已落地」與 git log）

- **IsSceneActionComplete CTDA**（2026-06-13）：`sceneActionIndex:1`（SCEN action index 是 1-based）→ phase 1→2 推進正常。
- **DualValueModifier SecondActorValue**（2026-06-13）：Health+Stamina 同時掉 ✅；Concentration+Aimed 需 `castingArt`+`projectile` 否則 CTD（已加 BarrierFireConcAimed beam refs）。
- **Sofia×VIGILANT CTDA mechanics**（2026-06-13）：`GetStageDone`/`GetInWorldspace`/`sayOnce`/`linkTo` 全部正常，有 lips，無語音（預期，未加 TTS）。
- **npcPatches[] AI Overhaul 用法**（2026-06-13）：Carlotta packages override 正常，白天留在家附近 sandbox。
- 身份系統 Phase-2/C 全套（2026-06-07）、語音管線 + lip sync（2026-06-13）、明亮室內/室外光照（2026-06-09）、Scene PlayIdle（2026-06-07）、SM Kill→quest（2026-06-05）、自訂對話（含 .seq）、navmesh 自訂 worldspace、follower 等。歷史完整清單在 CLAUDE.md。
