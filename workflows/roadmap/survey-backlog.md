# Roadmap — 通用框架/庫 survey backlog

← [roadmap](README.md)

> 「Skyrim 通用底座」這一層的調查缺口盤點（2026-06-15；觸發:盤 mod-survey 的框架/庫掌握度）。產出格式同既有 survey:`findings/<mod>.md` + 更新 [mod-survey/index.md](../../sub_projs/mod-survey/index.md)（框架型分類）。**survey 對「ModForge 缺什麼」一律標推斷、待 code 驗證**（同 index 第 33 行鐵律）。

## A. 完全沒碰，要新建 finding（按對 ModForge 策略的價值）

**全部需主力機**——要實際 mod 檔（`~/skyrim_mods/`，多數還沒解壓）+ 能探 esp/讀 config。離線機只能擺著。

1. **SkyPatcher** — 通吃型 runtime patcher（ini 改幾乎任何 record 欄位，正取代大批手寫相容 patch）。**最高優先**:直接衝擊「ModForge 該生成 esp，還是生成 SkyPatcher config」的產物策略——不只是補一個 finding，是策略級問題。
2. **KID（Keyword Item Distributor）** — po3 出品、SPID 姊妹:用 config 把 keyword 分發到 item/armor/weapon/MGEF。補完 SPID 那條「無 patch 分發/打標」線。
3. **SkyPatcher / KID 共同問題**:這兩個 + 既有 SPID 一起決定 ModForge「分發層」的生成方向，建議同批調查、一起出策略結論。
4. **Address Library for SKSE** — 幾乎所有 SKSE plugin 的硬依賴（跨版本記憶體定位）。純前置，但要弄懂「為何一堆 mod 列它當依賴」。
5. **po3's Tweaks / powerofthree's Papyrus Extender** — 補大量 Papyrus 原生函式，現代 mod 腳本直接用；和 PapyrusUtil/JContainers 同層更底。
6. **MCM Helper** — config 驅動 SkyUI MCM，免寫 MCM 腳本。凡有遊戲內設定面板的 mod 多半靠它。
7. **needs/survival 框架**（Frostfall / Survival Mode 的需求系統本體）— Conditional Expressions finding 只提到 Frostfall effect，沒拆需求框架本身。
8. **DynDOLOD / xLODGen** — LOD 生成。歸 index 的「美術型尚未調查」，列此備忘、實際併美術型批次做。
9. ~~DAR（Dynamic Animation Replacer）~~ — **明確不做**（OAR 後繼已取代；action-system 已間接涵蓋舊格式）。

## B. 中掌握，要從「淺記可利用點」升級成深挖

**需主力機**（同 A，要實際 mod 檔）。來源 [findings/common-framework-mods.md](../../sub_projs/mod-survey/findings/common-framework-mods.md)，該檔自述「不做深挖，只記可利用機制」。下列需專文拆解（record/config 結構、API 表、ModForge 生成可行性）:

- **SPID** — 深挖 config 語法全集（filter/string/level 條件、chance、形式）；和 A.1/A.2 一起出分發層策略。
- **PapyrusUtil** — StorageUtil/JsonUtil/ActorUtil/MiscUtil 完整 API 表 + 對 ModForge 生成 Papyrus 的支援度。
- **JContainers** — 容器型別（JArray/JMap/JFormMap/JDB…）+ 外部 JSON schema 驅動的可行性。
- **Base Object Swapper / AnimObject Swapper** — `_SWAP.ini`/`_ANIO.ini` 完整語法（property override、conditional section、random）+ ModForge 能否生成這些 ini。
- **Conditional Expressions** — 已抽 game-data，待把「狀態鎖 + busy gate + 清理」模式拆成可複用設計（若 follower dialogue 要做表情）。
- **I Am Walking/Talking Here** — 本地無檔，需先取得；其價值偏「設計原則」（碰撞/bark 抑制），確認有無可用 API。

> OAR 不在 B 列:action-system 已有深挖 + 生成器已落地。

## C. 淺掌握，要從「別的 finding 順手點到」升級成系統拆解

2026-06-14 機制型批次裡被點到、但**從沒當框架/引擎子系統獨立拆過**的底層機制（多為 vanilla 引擎子系統，非第三方 mod）。

> ✅ **code 驗證 pass 已做（2026-06-15）**，下列各條對應的「缺口」狀態已更新（見 [mod-survey-gaps.md](mod-survey-gaps.md)）:FLST(#1)、MGEF-VMAD(#3)、linkedRef(#5) **本就支援**→ 對應的 C 項降為純機制好奇（低優先）;SM(#2) 降 partial、PERK entry-point(#6) 確認真缺 → 這兩條深挖仍有價值。

- **SM（Story Manager）子系統** — record 結構（SMBN/SMQN/SMEN）、event node 樹、條件路由全貌。來源 Extended Encounters / Immersive World Encounters。坐實缺口 #2。參 [[story-manager-kill-recipe]]。
- **PERK entry-point 機制** — entry-point 種類全表（`ModIncomingDamage`/`AddActivateChoice`/`SetText`…）+ 各自的 fragment 掛法。來源 Arrowblock / Immersive Interactions。坐實缺口 #6（注意 [[perk-conditiontabcount-ctd]]）。
- **Script-attached MGEF（VMAD on magic effect）** — magic effect 掛 Papyrus script 的 VMAD/property 綁定結構。來源 Arrowblock。坐實缺口 #3（相對已具體）。
- **FLST 工廠模式** — FormList 當「池」、runtime 索引/隨機取的慣用法（含 Spellforge 的索引對齊、Missives 的 radiant 取用）。來源 Spellforge / Missives。坐實缺口 #1（最高價值；注意 index 對 Missives alias 的判斷已知誤判）。
- **Global-as-DAR-selector / linkedRef 節點鏈** — global 當動畫選擇器、linkedRef 表示巡邏/路線。來源 Immersive Interactions / Animated Carriage。坐實缺口 #5。

> C 組多數**不需主力機**（讀 record 結構、查 vanilla esm 即可，部分離線機能做）——與 A/B 的「必須實機 mod 檔」不同。回家排序時，離線空檔可先啃 C。
