# Honed Metal — NPC 打造/附魔服務框架（付費請鐵匠/附魔師代工）

調查日 2026-06-25（純 web 研究，本機未下載）。主 mod：**Honed Metal -NPC Crafting and Enchanting Services-**（[SE Nexus 61015](https://www.nexusmods.com/skyrimspecialedition/mods/61015)，原 LE [51024](https://www.nexusmods.com/skyrim/mods/51024)）。Nexus 與 Fandom wiki 對 WebFetch 回 403，主要佐證來自 [skyrimodding.com 的 Nexus 描述鏡像](https://www.skyrimodding.com/honed-metal-npc-crafting-and-enchanting-services/) + [STEP wiki](https://stepmodifications.org/wiki/SkyrimLE:Honed_Metal_-NPC_Crafting_and_Enchanting_services) + WebSearch 摘要。**只在單一搜尋摘要見到、未由實際頁面證實者標 ⚠UNVERIFIED；無捏造任何 FormID/腳本名/MCM 選項/版本號。**

跟剛落地的 vendor + `settlements:` 工作直接相關：vendor = 開 barter 選單；Honed Metal = 開 **craft/enchant 選單**——「付費請 NPC 代工」是 vendor 的姊妹型態。

## Classification

- 類型：**服務型框架 mod（含原生 SKSE DLL）**，非 record-edit mod。
- 敘事價值：**無**（純機制，對白重用原版語音）。
- 系統價值：**中高**（對 ModForge）——浮現「服務對話開原生製作選單」「付錢→給/改物件」兩個可重用 primitive；但核心是 bespoke runtime（見 §3）。

## 1. 它做什麼

玩家付錢請原版（與 modded）NPC 鐵匠/附魔師代工，而非自己做：**鐵匠打造+強化武防；附魔師附魔+充能物件**。NPC 能做什麼、收多少，隨 **NPC 的 smithing/enchanting 技能等級**（決定其等效 perk + 可處理的裝備 tier）+ **玩家 barter + 經濟設定**。⚠UNVERIFIED：後期版本把成本**更偏 NPC 技能、較不偏物件價值**，材料成本另加。材料：**NPC 用自己的材料、可花時間/金幣取得缺的常見材料**，但**稀有材料（如龍骨）玩家須自備**。明確**支援 mod 新增的武防**。可選「Skill Based Smithing」讓成品數值隨 NPC 技能加成。預設把若干 lore NPC（Eorlund、Neloth、Baldor 等）設為大師匠人。

## 2. 機制 / 怎麼實作

**框架/腳本驅動 + 原生 SKSE DLL**：

- **SKSE C++ DLL（核心）**：新版出 **per-runtime C++ DLL**，選錯版本啟動即 SKSE 報錯（另有社群 repack）。**另有「最後一個無 SKSE plugin 的版本」**（純 Papyrus fallback、號稱任何遊戲版本可跑）——證明 DLL 加能力但歷史上存在 Papyrus 路徑。config 在 `Data/SKSE/plugins/HonedMetal.ini`（perk-ID 黑名單，避免 perk-overhaul 干擾）。⚠UNVERIFIED：DLL 與 Papyrus 的確切分工（讀不到 C++ 源碼）。
- **硬依賴：SkyUI（或 SkyAway）+ 對版 SKSE**。MCM ⇒ SkyUI MCM 框架。**無證據**依賴 PapyrusUtil / ConsoleUtil / JContainers / SPID——**勿假設**。
- **物品發現靠 FormList + 原版 perk/COBJ 系統**：可用材料存在 **plugin 內的 FormList**（一個常見：礦/錠/充魂石；一個稀有/違禁）。用「掛在遊戲 perk 樹上的 smithing/enchanting perk」判斷 NPC 能做什麼與 tier。⚠UNVERIFIED：可製作物品到底是從原版 **COBJ** 配方列舉，還是純 keyword/FormList 成員——只證實「材料用 FormList、能力用 perk-gate」，未證實成品列舉路徑。
- **對話是真 dialogue 記錄、非 SPID**：mod **對匠人 NPC 加了新 topic/response**，**重用原版語音**（移除自帶語音、指向既有音檔、隨機挑；伴隨 mod「Voice Tweak」修字幕/voicetype 不符）。把 NPC 變匠人靠 **faction 成員**——MCM 有「Add NPC to Smithing/Enchanting Faction」，對話條件幾乎必 gate 在這些 faction。⚠UNVERIFIED：附掛機制（quest alias vs 共用 info 條件在 faction）——faction-driven 模型已證實。
- **互動流程（最吃重的 bespoke 部分）**：附魔：對話「能幫我附魔嗎？」→ **開容器/轉移視窗**（玩家放入物件 + 可選充魂石）→ 關閉後**腳本以程式開啟原版附魔選單**，玩家像自己附魔般選。**「開容器→腳本開原生製作選單」是核心 trick，最可能由 SKSE DLL 撐**。MCM「NPCs Have Materials」切換 NPC 是否供應充魂石。
- **軟依賴/patch**：伴隨 mod（Additional Materials、CCOR/CCOR patch、FLM patch、翻譯）擴充材料 FormList——即**擴充性靠改 FormList**，本身是有用的 pattern。

## 3. 對 ModForge 的意義

「付費請 NPC 代工」**部分可表達**，但核心是 bespoke runtime。

**乾淨對應既有功能**：
- **faction-gate 服務對話**——ModForge 已會對原版 NPC 注入對話（quest+alias+INFO 條件）+ **vendor faction**。「加 NPC 進匠人 faction → 條件對話 topic」正是 vendor/dialogue pattern；剛落地的 `settlements:` 概念相鄰（大量 faction-tag NPC）。
- **MCM**——ModForge 生 MCM（memory `mcm-helper-registration-recipe`）。切換/技能滑桿可生。
- **FormList**——ModForge 能出 FormList + FLM 分發；材料清單擴充模型契合。
- **付錢交易 + 訊息框**——有 message box / GLOB / Papyrus fragment；「扣金幣→給/強化物件」fragment 可達。
- **Storage**——JContainers/PapyrusUtil KV（已實機確認）可存 per-NPC 技能/材料狀態。

**不對應——須手寫 `.pex`（或 ModForge 產不出的 DLL）**：
- **轉移容器後腳本開原生附魔/打造選單**——引擎級（HM 用 C++ SKSE DLL）。**ModForge 產不出 C++ SKSE plugin**；只能附帶預寫 DLL，或找純 Papyrus 等價（舊版無 DLL 暗示有 Papyrus 路徑但較受限）。
- **成本公式**（物件值 × NPC 技能 × barter × 材料）——非平凡 bespoke Papyrus controller。
- **perk 驅動能力 gating** + runtime 讀原版 COBJ/perk 樹判可製作性——bespoke。
- **容器轉移 UX + 強化/充能套用**——bespoke fragment。

**誠實結論**：ModForge 能生**外殼**（faction、條件服務對話、MCM、FormList、訊息框、扣金幣 fragment、storage）。**controller**（開選單、成本數學、perk/技能 gating、強化/附魔套用）是**大量手寫 Papyrus controller**，原生選單 trick 還可能要 ModForge 以 asset 出貨**預建 SKSE DLL**。即：scaffolding 可生成、brain 須 bespoke。

## 4. Roadmap 意涵

1. **「服務對話開原生製作/物品選單」**——高價值新 primitive：fragment 開容器轉移 +/或原生選單。HM 證實需求也證實它要 SKSE-DLL 肌肉 → 標「**需附帶 helper .pex/.dll asset**」，非純生成。與 vendor 成對（vendor 開 barter；此開 craft）。
2. **付錢→給/改物件交易 pattern**——通用 fragment 模板：`Player.GetGoldAmount() ≥ cost` → 扣金幣 → 給/強化/附魔目標。可重用於 vendor/服務/賄賂。乾淨的可生成 macro 候選。
3. **FormList 驅動的物品/材料發現 + 擴充性**——HM「兩個 FormList（常見/稀有）+ FLM/CCOR patch 擴充」是 ModForge 已有記錄（FormList + FLM 分發）的 pattern。值得寫成 spec idiom：*能力清單即 FormList、由分發擴充*。
4. **faction-tag NPC 開服務**——直接重用 vendor-faction + dialogue-condition + 新 `settlements:`。一個 `services:` macro（鐵匠/附魔/訓練）把 NPC tag 進服務 faction + 自動接條件對話，是 `settlements:` 的天然下一個姊妹。
5. **技能/perk-gate 行為**——記為缺口：ModForge 有 GLOB/perk，但「服務品質隨 NPC 技能」要 Papyrus controller 讀 actor 技能——多半是附帶 helper 而非生成。

**淨結論**：scaffolding（對話+faction+FormList+MCM+金幣交易 fragment+storage）可生成；**原生選單-from-對話 + 成本/技能 controller 是真缺口**，傾向做一小套**預寫「服務 controller」.pex/.dll asset** 讓 ModForge 接線，而非生成那段邏輯。

---

### 來源
- https://www.skyrimodding.com/honed-metal-npc-crafting-and-enchanting-services/（已抓，Nexus 描述鏡像，主來源）
- https://www.nexusmods.com/skyrimspecialedition/mods/61015（SE 頁；403，靠搜尋摘要）
- https://www.nexusmods.com/skyrim/mods/51024（LE 頁；摘要）
- https://stepmodifications.org/wiki/SkyrimLE:Honed_Metal_-NPC_Crafting_and_Enchanting_services（摘要）
- https://www.nexusmods.com/skyrimspecialedition/mods/51254（Additional Materials — FormList 機制）
- https://www.nexusmods.com/skyrimspecialedition/mods/34393（Voice Tweak — 對話/語音機制）

**未能完全證實**：成本公式權重；可製作物品來自 COBJ 列舉 vs FormList/keyword；對話附掛機制（alias vs faction 條件共用 info）；DLL 與 Papyrus 確切分工；任何 FormID/腳本/MCM 選項識別符（皆未捏造）。
