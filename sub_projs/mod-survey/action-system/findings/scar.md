# SCAR — Skyrim Combos AI Revolution

← [action-system 中樞](../README.md)

> **Layer 4（招式框架／NPC AI）**。Maxsu/Monitor144hz 的 SKSE plugin。給 NPC 一顆「會用招式」的腦——把 Elden Ring/Dark Souls 式的 boss 連段帶進 Skyrim。183 mod 依賴。

## 問題與做法
- vanilla 引擎**根本沒有連段概念**（只有左右揮砍）。即便裝了 MCO/SkySA 把現代招式帶進來，NPC 仍不會好好用：MCO 下 NPC 一次只揮一招、SkySA 下 NPC 隨機亂連不看情境。
- SCAR：**每套 moveset 一份攻擊 AI 設定**（"One Moveset, One Attack AI Setup"）。NPC 出招前檢查**距離、角度及其他條件**，挑符合的攻擊動作執行 → NPC 能依情境正確連段。
- 每套 moveset 須**預先 patch** 才支援 SCAR（用 moveset patcher 工具加 SCAR 註釋），否則行為同 vanilla。內附 MikeNike/Distar 的 ADXP 預設動畫包（全 vanilla 武器類型）。

## 與本批次其他 mod 的關係
- 與 **AMR** 互補（AMR 管位移精準、SCAR 管 NPC 智能出招——作者原話「兩 mod 一起最閃」）。
- **BFCO** 已內建 NPC 連段 AI；有 SCAR 註釋的動畫交給 SCAR，無註釋的由 BFCO 自帶 AI 管 → 兩者可共存。
- 需 Nemesis patch（作者宣告未來新 plugin 只支援 1.5.97）。也能用於生物（需有人做生物 behavior 支援）。

## 對 ModForge
- **不可生成**（SKSE DLL + moveset 內 SCAR 註釋屬動畫/hkanno 管線）。
- 概念價值：SCAR 的「**距離/角度條件 → 選攻擊動作**」與 ModForge 的 CTDA condition 思路同構，但發生在 behavior/動畫層而非 esp。**ModForge 能生的是「動畫包 + 條件 config」那一半**（如 OAR），SCAR 的 AI 決策層不在 record 範疇。
- 若 follower-patch 想讓某 NPC 用現代連段，SCAR（或 BFCO 內建 AI）是前置；ModForge 負責的是 NPC record/裝備/動畫包，不負責出招 AI。
- 原始碼見 mod 頁 GitHub（maxsu2017）。
