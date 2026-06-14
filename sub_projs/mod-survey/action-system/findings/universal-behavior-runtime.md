# Universal Behavior Runtime（A-Pose Bug Fix + Auto Skeleton Patch）

← [action-system 中樞](../README.md)

> **Layer 1（行為引擎／runtime）**。Monitor221hz（Pandora 作者）的「Universal Behavior Runtime」系列 SKSE plugin，2026 新作。把過去要靠 behavior engine **預先 patch** 才能解的問題，改成**遊戲執行時動態解**。

## A-Pose Bug Fix（v1.1.0-a, 2026）
- **問題**：A-pose = 角色手臂斜垂成「A」字凍結，是 behavior graph 或動畫載入錯誤的**症狀**。
- **做法**（runtime 攔截）：
  - 行為載入錯誤（會造成永久 A-pose）→ 攔截。
  - 動畫載入錯誤（暫時 A-pose）→ 攔截。
  - 若資源存在但格式不相容 → **即時把 hkx 轉成 SE/AE 格式**（用 SARDONYX 寫的 Rust 庫 **Serde-HKX**，毫秒級、轉一次永久相容）。
  - 缺 behavior graph → 載入 dummy graph；缺動畫 → 仍允許 A-pose（無法無中生有，留作錯誤提示）。
- **重大副作用**：裝了它，**所有 LE behavior/動畫 mod 對 SE/AE 100% 開箱相容**（just-in-time 轉換）。
  - 但拿它當 LE 相容層時**必須用 Pandora**——Nemesis SE 拒絕處理 32-bit behavior。
- 相容 Pandora / OAR / DAR / Animation Queue Fix。轉換產物是 overwrite 裡的 `*_packed.hkx`（暫存，可刪）。

## Auto Skeleton Patch（v1.0.4, 2026）
- **問題**：骨架 mod（如 XPMSSE）沒做 behavior patch 會出怪 blend：施法時手不動、strafe 時不播格擋、弓箭動作鬼畜、凍結、CTD。
- **做法**：runtime 動態 patch 骨架 behavior → **骨架 mod 不再需要 behavior engine 跑補丁**，裝這個就好。相容任何骨架。
- ⚠️ 與 Pandora 並用時**不要勾 Pandora 的 XPMSSE patch**（功能重複，這個更穩）。相容 FNIS/Nemesis/Pandora。

## 對 ModForge（重要）
- **這層是「免 behavior patch」趨勢的延伸**——和 ModForge「不碰 Havok binary、只生 record/config/asset」的哲學同向。
- 直接價值：**離線/headless 出貨的後路**。若 ModForge 產的動畫資產卡在「需 Pandora 跑一次」而 Pandora headless 未解（見 [pandora.md](../pandora.md)），A-Pose Bug Fix 的 runtime 轉換 + dummy-graph 容錯，可能讓**部分動畫 mod 免預先 patch 就能跑**（待實機驗證能涵蓋到什麼程度）。
- **不可生成**（純 SKSE DLL），但應列為 ModForge 動畫產物的**推薦前置**，並在 roadmap 的 Pandora spike 裡一併評估「A-Pose Bug Fix 能否降低對 Pandora 預先 patch 的依賴」。
