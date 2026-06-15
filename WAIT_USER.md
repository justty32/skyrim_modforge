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
- **外部資源**（Nexus 下載 mod / 外部工具實跑）：
  - **Nexus 下載（美化/body/工具，掃完 ~/skyrim_mods 確認缺）**：
    - **CBBE 3BA**（30174）— OBody 必需的 body framework，現有 CBBE 是舊版
    - **OBody NG**（77016）— 每個 NPC 自動隨機 body preset + ORefit 服裝貼合
    - **AutoBody AE**（61321）— OBody 的輕量替代（zero config randomize）
    - **Modpocalypse NPCs**（54422）或 **Nordic Faces**（40658）— 通用 NPC 美化底座擇一
    - **EasyNPC**（52313）— NPC appearance 合併工具（避免暗臉衝突）
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

**待測（active）**

- **Sofia × VIGILANT 第一幕（2026-06-14）** — 兩版交付 `~/skyrim_mods/mine/`：`SofiaVigilantAct1.zip`（v1 對話+語音）、`SofiaVigilantAct1v2.zip`（v2 +PlayIdle 動作）。spec＝`examples/sofia_vigilant_act1{,_v2}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act1-警戒者.md`。
  - **✅ v1 核心 pipeline 已實機確認（2026-06-14）**：對話有註冊、觸發點對、語音有播（跑了一小段任務線）。
  - **仍 open（待你續測）**：① **各 beat 完整覆蓋**——把 1-A~1-K 跑滿，看有沒有哪個選項該出現卻沒出現（stage 解碼誤）；② **殺/放分支正確性**（殺女巫=SubQ01 s50 / 放=s230；殺 Carene=GoodEnd s35 / 放=s100——殺了卻跳「放過」台詞＝分支錯）；③ **嘴型**有沒有動（fuz 內嵌 lip，待目視確認）；④ **v2 動作**——換裝 v2（一次只裝一版，editorId 不同），看 1-A 諷刺鼓掌 / 1-E 嘆氣 / 1-H-殺 怒 / 1-I 東張西望 有沒有播。
  - gate 解碼地圖見 `sub_projs/sofia-patch/vigilant-screenplay/_act1-trigger-placement-map.md`（BSA QF_ 碎片逆向，高信心）。
  - **後續（非待測，待方向確認後我做）**：夢境/更多動作機制位置已定（夢 cell 0x00185C、stage25 進）未實作。

- **Sofia × VIGILANT 第二/三/四幕（2026-06-14）** — 交付 `~/skyrim_mods/mine/SofiaVigilantAct{2,3,4}.zip`（FLAT，語音齊 + setGlobal pex 齊；Act2=34 fuz/11 pex、Act3=51 fuz/14 pex、Act4=16 fuz/13 pex）。spec＝`examples/sofia_vigilant_act{2,3,4}.json`，臺詞＝`sub_projs/sofia-patch/vigilant-screenplay/act{2,3,4}-*.md`，gate 解碼＝同夾 `_act{2,3,4}-trigger-placement-map.md`。
  - **與 Act 1 唯一差別：沒嘴型**（這批跳過 lip 避免 LipGenerator wine crash 拖死；對話/語音正常，只是嘴不動）。方向確認後可統一補 lip 重打包。
  - 測法同 Act 1（裝在 SofiaFollower+Vigilant 後、save+reload 吃 .seq、跑對應幕的任務、到 beat 對 Sofia 按對話鍵）。回報哪些選項沒出現 / 分支對不對 / 語音正常否。
  - gate 重點：Act2 空牢 0x038524 / 沉船 0x038525 / 血祭母 0x038526；Act3 Child of Oblivion 0x065932；Act4 多數記憶靜默、僅 MeQ01/02/07/Pelinal MeQ10/Molag Bal/Karma 結局有評論。
