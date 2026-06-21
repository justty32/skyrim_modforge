# wait_todo — 實機測試（in-game，MO2 / Proton）

← [WAIT_USER](../WAIT_USER.md)（總入口）

我**不能跑遊戲**，只能 diag / 逐位元對齊 + 打包；實機驗收靠你（memory `ingame-test-workflow`）。

**怎麼測（通用流程）**
1. **拿 zip**：我把打包好的 zip 放 `~/skyrim_mods/mine/`（**FLAT**：plugin 在 zip 根，別有多層；曾因 zip 根殘留舊 esp 蓋掉新的而誤判「還在崩」）。`~/skyrim_mods` 根是你的 Nexus 下載，別混。
2. **裝**：MO2 從 zip 安裝 → 啟用 → 排 load order（override 類放衝突 mod 之後，如 USSEP / AI Overhaul）。
3. **跑**：Proton 啟動。
4. **對話／任務鐵律**：對話只在**遊戲 LOAD** 時註冊 → 用全新遊戲或任務啟動後 save+reload（`coc` 不註冊）；既有存檔要 save+reload 才吃 `.seq`；強制天氣 `sw <XX>000800`（XX=load order 槽位 hex，build 會印）；console `playidle` 吃 EditorID 不吃 FormID。
5. **回報**：哪些 OK／怪／CTD／空白，附 CrashLoggerSSE log 最好。

**MO2 重裝會還原手動塞的檔**：手動 patch 進 MO2 mod 夾的檔，從 zip 重裝會復原成 build-time mtime → 測前 md5/mtime 確認受測檔是新的（memory `mo2-reinstall-reverts-manual-pex`）。

## 待測（active）

- **【Idea #20 Phase 1】in-world 技能樹 — ✅ 零外部依賴 standalone 版（2026-06-21，使用者明確不想裝 Campfire/Frostfall）**：交付 `~/skyrim_mods/mine/ModForgeSkillTree.zip`（FLAT：esp 在根 + `Scripts/MFSkillNode.pex`+source）。spec＝`examples/inworld_skill_tree_standalone_spec.json`，腳本＝`examples/MFSkillNode.psc`（已用 native compiler 對 vanilla header cache 編綠）。**只依賴 Skyrim.esm**。
  - 不是 Campfire 那種營火 radial menu，而是**最直白的 in-world 樹**：3 顆漂浮水晶節點（vanilla `WispCrystal01.nif`）擺在**白漫酒館 Bannered Mare**（vanilla cell 0x01605E override，非自建房間），各掛我自己寫的 `MFSkillNode` 腳本（純 vanilla 型別）。點擊 → gate 檢查（前置節點 + 點數）→ 給一個自訂 Fortify ability + 扣 1 點。Forged Resolve（root，永遠可點）/ Forged Vigor / Forged Mastery（各給 Fortify Health/Stamina/CarryWeight）。MFSkill_PointsAvail 預設 3。
  - **改放 Bannered Mare 的原因（2026-06-21 二修）**：第一版放自建內景 cell `coc MFSkillTreeRoom`，使用者回報**完全沒反應**。改放 vanilla cell → `coc WhiterunBanneredMare` 是遊戲內建、100% 會傳送 + 有地板有燈，**同時當診斷**：若傳送進酒館卻沒看到水晶＝plugin 沒載入（非 coc 問題）。座標 (-119/-49/21, -504, 150) 取自已出貨 `interior_spec.json` 的確認開放地板點，橫排在眼睛高度（不用抬頭）。
  - **結構已全驗**（dump 輸出 esp）：**masters 只有 Skyrim.esm（零 Campfire/Frostfall）**；cell 0x01605E override 帶 3 個節點 ref；MFSkillNode 屬性全解析無 null；gating 鏈正確（Node0 無 prereq、Node1.prereq=N0_Rank、Node2.prereq=N1_Rank）；3 個 Ability/ConstantEffect/Self spell 包對應 ValueModifier MGEF。
  - **怎麼測**：裝 zip+**確認 MO2 右欄勾選啟用** → 進遊戲（既有存檔即可）→ console `coc WhiterunBanneredMare` → 一定傳送進酒館 → **環顧找 3 顆並排漂浮發光水晶**（約眼睛高度）→ 準心對**最左**那顆（Forged Resolve）按 [E] → 跳通知「Learned: Forged Resolve — 2 point(s) left」+ Magic>Active Effects 出現 Fortify Health → 中間、右邊依序。**先別點最左、直接點中/右 → 應跳「Locked. Learn the node below it first.」**（驗 gating）。
  - **三種結果**：① 看得到水晶 + 可點 + 學到 ability + gating 正確 → Phase 1 過，「JSON spec → 可點 in-world 節點樹 + 自編腳本 + 給效果」整條成立。② **傳送進酒館了但沒半顆水晶** → plugin 沒載入：查 MO2 右欄 plugin 有沒有勾、是不是 ESL 載入位置、`help "Forged Resolve" 4` 看找不找得到 SPEL（找不到＝沒載）。③ 水晶在但點了沒反應/CTD → 回報我 + `Papyrus.0.log`。
  - **快速隔離**：console `getglobalvalue MFSkill_N0_Rank`（點前 0、點後 1）、`player.getav Health`（學 Resolve 後 +50）。
  - **備註**：Campfire radial-menu 版（`examples/inworld_skill_tree_spec.json`）留在 repo 當 Phase 3 設計範本，不交付。

- **VNML 法線效果（2026-06-16）— 已自驗修正，下面只剩「想看再看」的選配確認**：axis/編碼/尺度已對 vanilla Tamriel LAND 逐 byte 驗過（修了三個 bug，見 SESSION-LOG），不必硬測。新 zip 已交付 `~/skyrim_mods/mine/HeightmapDemo.zip`（FLAT）。**若你某次順手進遊戲**：進 HeightmapDemo worldspace 走坡面，背光側偏暗、向光偏亮、平順漸層即正常——若看到整片黑塊／詭異反光／上下顛倒陰影再回報（理論上不會）。

- **Sofia × VIGILANT 第一幕（2026-06-14）** — 兩版交付 `~/skyrim_mods/mine/`：`SofiaVigilantAct1.zip`（v1 對話+語音）、`SofiaVigilantAct1v2.zip`（v2 +PlayIdle 動作）。spec＝`examples/sofia_vigilant_act1{,_v2}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act1-警戒者.md`。
  - **✅ v1 核心 pipeline 已實機確認（2026-06-14）**：對話有註冊、觸發點對、語音有播（跑了一小段任務線）。
  - **仍 open（待你續測）**：① **各 beat 完整覆蓋**——把 1-A~1-K 跑滿，看有沒有哪個選項該出現卻沒出現（stage 解碼誤）；② **殺/放分支正確性**（殺女巫=SubQ01 s50 / 放=s230；殺 Carene=GoodEnd s35 / 放=s100——殺了卻跳「放過」台詞＝分支錯）；③ **嘴型**有沒有動（fuz 內嵌 lip，待目視確認）；④ **v2 動作**——換裝 v2（一次只裝一版，editorId 不同），看 1-A 諷刺鼓掌 / 1-E 嘆氣 / 1-H-殺 怒 / 1-I 東張西望 有沒有播。
  - gate 解碼地圖見 `sub_projs/sofia-patch/vigilant-screenplay/_act1-trigger-placement-map.md`（BSA QF_ 碎片逆向，高信心）。
  - **後續（非待測，待方向確認後我做）**：夢境/更多動作機制位置已定（夢 cell 0x00185C、stage25 進）未實作。

- **Sofia × VIGILANT 第二/三/四幕（2026-06-14）** — 交付 `~/skyrim_mods/mine/SofiaVigilantAct{2,3,4}.zip`（FLAT，語音齊 + setGlobal pex 齊；Act2=34 fuz/11 pex、Act3=51 fuz/14 pex、Act4=16 fuz/13 pex）。spec＝`examples/sofia_vigilant_act{2,3,4}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act{2,3,4}-*.md`，gate 解碼＝同夾 `_act{2,3,4}-trigger-placement-map.md`。
  - **與 Act 1 唯一差別：沒嘴型**（這批跳過 lip 避免 LipGenerator wine crash 拖死；對話/語音正常，只是嘴不動）。方向確認後可統一補 lip 重打包。
  - 測法同 Act 1（裝在 SofiaFollower+Vigilant 後、save+reload 吃 .seq、跑對應幕的任務、到 beat 對 Sofia 按對話鍵）。回報哪些選項沒出現 / 分支對不對 / 語音正常否。
  - gate 重點：Act2 空牢 0x038524 / 沉船 0x038525 / 血祭母 0x038526；Act3 Child of Oblivion 0x065932；Act4 多數記憶靜默、僅 MeQ01/02/07/Pelinal MeQ10/Molag Bal/Karma 結局有評論。
