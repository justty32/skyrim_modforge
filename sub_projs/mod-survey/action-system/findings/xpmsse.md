# XPMSSE — XP32 Maximum Skeleton Special Extended（骨架地基）

← [action-system 中樞](../README.md)

> **Layer 0（骨架／rig）**。整個動作系統最底層的 de-facto 標準人體/生物骨架。241k endorse、2765 mod 依賴它——幾乎所有動畫/戰鬥/物理 mod 的隱性前置。

## 是什麼
- 一份**擴充骨架 NIF**（`skeleton.nif` / `skeleton_female.nif`）+ 一個 SKSE/RaceMenu plugin（NiOverride/skee）。把 vanilla 骨架不存在的節點補上：武器掛點（MOV node）、物理骨（HDT/BBP/TBBP/3BBB）、CME/RM 可調節點、尾巴節點…
- 三種安裝檔：**Minimal**（無前置）/ **Basic**（需 FNIS 或 Nemesis）/ **Extended**（+RaceMenu + MCM）。
- 節點命名規範（modder 要記）：`NPC` = mesh 骨、`HDT` = 物理專用、`CME` = RaceMenu 可調額外骨、`MOV` = 武器 socket（**勿改名**）。

## 在堆疊中的角色
- 動畫綁在**骨骼節點**上；自訂招式/物理/武器位置都假設這套擴充節點存在。沒有它，很多動畫會斷骨/錯位。
- **武器風格（背劍、腰刀…）= 移動 MOV socket 節點 + 切換對應動畫**，由 XPMSE 的 RaceMenu plugin/MCM 在 runtime 做。

## 與 behavior engine 的關係（2026 重點）
- XPMSE 的「Alternative Animations（依風格即時換動畫）」**需要 Nemesis patch**。**Pandora 不實作 Alternative Animations** → 用 Pandora 時 XPMSE MCM 換不了動畫。
- Pandora 用戶的替代方案：**OAR + Weapon Styles**（或 OAR + Immersive Equipment Displays + Weapon Styles for IED）取代 XPMSE MCM 的武器風格切換。
- 骨架本身的 behavior 相容由 [Auto Skeleton Patch](universal-behavior-runtime.md) 在 runtime 處理（免 behavior engine 跑 XPMSE 補丁）。

## 對 ModForge
- **純前置依賴，不生成**。ModForge 產的動畫/招式若用到擴充節點（武器掛點、物理骨），spec 應在 README/需求裡標 XPMSSE 為前置。
- 唯一可生成的相關物：若要做「武器掛在背上」這類效果而不靠 XPMSE MCM，走 **OAR 條件式替換 equip/idle 動畫**（ModForge 可生 OAR 結構，見 [oar-replacer-guide](../oar-replacer-guide.md)）。
