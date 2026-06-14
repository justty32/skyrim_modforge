# BFCO — Attack Behavior Framework（+ Universal Support）

← [action-system 中樞](../README.md)

> **Layer 4（招式框架）**。Viny/maxsu/doodlum 的現代攻擊動畫框架，2026 的 MCO/SkySA 後繼選擇。melee+ranged、跳/泳/蓄力攻擊、NPC 連段、方向重擊。163 mod 依賴。**本批次最能示範「整套動作系統怎麼拼起來」的範例。**

## 核心能力
- 動畫表：`BFCO_Attack1..20`、`BFCO_PowerAttack1..20`、方向重擊 `BFCO_PowerAttackA/B/L/R`、跳/衝刺/泳/雙持/蓄力攻擊、弓弩 `BFCO_RangeAttack1..10` + bash。
- **vanilla 攻速 + 方向重擊** → 相容**所有技能樹大修**（Vokrii/Ordinator…），這是它對 MCO 的主賣點。
- **NPC 連段內建**（與玩家共用派生規則），無 SCAR 也能連；有 SCAR 註釋的動畫則交給 [SCAR](scar.md)。
- MCM 可配置（LMB 同 vanilla 出輕重擊 / 綁獨立重擊鍵 / 長按連輕擊…）。需 dTry's Key Utilities 做 keytrace。
- **Universal Support**（doodlum）：recovery DLL port 到全版本——「recovery」frame 讓玩家用方向輸入提前脫離攻擊後搖。

## 派生機制 = 三層注入鏈的實戰（ModForge 重點）
BFCO 的招式串接**全靠動畫內的自訂事件註釋**驅動，不改 esp：
- 串接事件（hkanno 加在 hkx）：`BFCO_NextIsAttack1`（首個須在 0.0s，指定下一段 N 攻擊編號）、`BFCO_NextIsPowerAttack2`、`BFCO_NextWinStart`（攻擊窗開）、`BFCO_DIY_EndLoop`（窗關）、`BFCO_DIY_recovery`（可用移動/格擋鍵結束攻擊）。
- 攻速：`PIE.@SGVF|BFCO_AttackSpeed|1.0`（走 [Payload Interpreter](payload-interpreter.md)）。
- **攻擊變體 → OAR 條件**（mod 頁記載的**作者可選範例**，v3.2 起）：行為整數變數 `BFCO_iAttackVariants`（及 `A..E`）。動畫用 `PIE.@SGVI|BFCO_iAttackVariants|1` 設值，再開一個 OAR 動畫資料夾，config 加：
  ```json
  { "condition": "CompareValues", "requiredVersion": "1.0.0.0",
    "Value A": { "graphVariable": "BFCO_iAttackVariants", "graphVariableType": "Int" },
    "Comparison": "==", "Value B": { "value": 1.0 } }
  ```
  → OAR 即在該資料夾選下一段攻擊動畫（攻擊結束 `BFCO_iAttackVariants` 重置 0）。

> **⚠️ 實檔核對（BFCO v3.100.3）**：上面這段 `CompareValues`+`graphVariable` 是 **mod 頁給作者的可選範例**，是合法 OAR 語法；但 **BFCO 自己出貨的 21 個 OAR submod config 用的是 `IsEquippedType` 等裝備條件、不含 graphVariable**（攻擊狀態走 behavior 層 + BDI 變數）。
> 它出貨的 **BDI config（已驗證）** 是 `BFCO_ComboLocked`(kBool) / `BFCO_LastAttack` / `BFCO_NextNormal` / `BFCO_NextPower`(kInt)——**不是** `BFCO_iAttackVariants`（後者只出現在 Nemesis behavior `.txt`，是 behavior 變數）。出貨的 OAR root config 僅 `{name, description, author}`，submod 才有 `priority`+`conditions[]`，與 [OAR 指南](../oar-replacer-guide.md)一致。
> **ModForge 可生成的部分**：OAR 資料夾結構 + condition JSON（裝備條件或 graph-variable 條件皆可）+ BDI config，全是確定性產物——唯 .hkx 本體與 hkanno 註釋屬動畫管線。

## 相容性（值得記）
- ✅ DMCO（任意版本，攻擊↔閃避互相派生）、SCAR、Perk 大修、True Directional Movement、Elden Counter（勿勾其 vanilla behavior patch）。
- ❌ MCO/SkySA/ABR（功能重複，有 MCO→BFCO 轉換器）、power attack 熱鍵 mod（OCPA/Elden Power Attack，功能內建於 MCM）、Dual Wield Parrying、CGO、**FNIS（過時，請用 Pandora）**。
- 安裝：跑 Nemesis/**Pandora** 勾 BFCO patch（多數情況需要，才能與 DMCO/TK Dodge 相容）。

## 對 ModForge — 待辦
- **OAR 攻擊變體生成**：模板化「給某武器/NPC 一套 N 段連擊」→ 生 OAR 資料夾 + `BFCO_iAttackVariants` 條件 config。屬 OAR 生成器（roadmap）的具體應用案例。
- 列 BFCO + Payload Interpreter + DMK 為前置；.hkx 與 hkanno 註釋走動畫管線。
- 原始碼：https://github.com/vinymayan/BFCO
