# Ideas — 世界建構 / 玩法系統

← [ideas 索引](ideas.md)

## 3. 商隊與船隊生活

流浪商人視角：加入/組建陸路商隊沿固定路線交易；船隊（海路、港口停靠買賣）；**空艇冒險**（Airship 作移動基地穿越異域）。可能需自訂 AI Package（巡邏路線）+ 商業系統 UI。

---

## 4. 異世界冒險（另開 Worldspace）

開全新 Worldspace、設定迥異於泰姆瑞爾；以穿越/傳送門進入並有劇情驅動；主題不限（奇幻異界、蒸汽龐克、廢土…）。

---

## 5. 其他遊戲資源移植 / 引擎復現

把電腦上其他遊戲的場景/角色/玩法概念「翻譯」進 Skyrim——不是完整移植，而是用 Skyrim 的敘事與互動語言重現精髓。需評估資源格式轉換（見 §14）與遊戲規則的系統化對應。⚠️ 法律面：他遊資產轉了不能發布。

---

## 8. 程序生成的世界

地形/地城/NPC 組成/事件都帶程序生成成分。參考 Requiem 縮放 + Radiant Story 延伸 + 自訂世界生成。ModForge 的 Generator 可作批次生成「骨架 ESP」起點。長期：每次開新檔世界佈局不同。

---

## 11. 騎馬與砍殺 in Skyrim（機制復刻）

要的是 **Mount & Blade 玩法機制**（募兵/帶兵/會戰/攻城/封地/征服），不是它的世界素材；再加 **三國志精隨**（城池換領主、勢力興衰、武將被俘/招降/倒戈）——一個活的戰略格局。

**舞台：架空自訂 worldspace（2026-06-04 決定）**——動天際省會跟 vanilla quest / 城市 mod 打架且 lore 綁手；自訂 worldspace ≠ 自訂美術（全擺 vanilla 資產）；地圖可為戰略玩法設計（城距、隘口、糧倉、好打的攻城戰）；ModForge 已能生 worldspace + 平坦地形 + navmesh，缺非平坦地形與聚落級 placed-ref 量產。

**M&B 核心循環**：募兵與部隊管理（招募→兵種樹→跟隨）、野戰、攻城戰、戰略層（大地圖/外交/封地/征服）、騎戰（Skyrim 1.6+ 原生）。

**城池換領主——引擎有現成先例**：vanilla 內戰系統就是完整的「城市換勢力」（衛兵/旗幟/領主/crime faction 全換），可一般化為每城 × 每勢力一組 Enable Parent Marker，換主時舊組 `Disable()`、新組 `Enable()`。vanilla 只硬編 2 勢力，N 勢力 = marker 組 ×N（組合爆炸正是 spec 量產甜蜜點）。動態外交：`Faction.SetEnemy()`/`SetAlly()` 是 vanilla Papyrus，執行期可改。

**戰略模擬層（三國志部分）= 模擬 + 演出兩層**：

- 模擬層：常駐 quest `RegisterForSingleUpdateGameTime(24)` 每日 tick——勢力 AI 決策、玩家不在場的戰役 autocalc 結算（兵力×質量×城防）、武將資料（忠誠/能力/俘降）純資料存放。
- 演出層：玩家在場才實打（受 actor 上限約束、波次增援）；換主 = marker 翻轉；戰況傳聞/信使由 §9 管線餵。
- 武將是靈魂：每個 lord 是真 NPC record，招降 = 改 faction + AI package + 解鎖對話。
- 風險：守軍須 spawn-on-demand（LvlN，絕不 persistent）；每小時級精細模擬要考慮 SKSE native；攻城戰演出最難。

**引擎硬限制（決定設計上限）**：同屏活躍戰鬥 AI 超過 ~30-50 就掉幀 / AI 崩壞 → 百人會戰不能硬做，要分波增援 / 戰場區隔 / 小隊抽象；Skyrim 無「大地圖」→ 世界地圖 + 快速旅行事件化，或選單 / 書本 UI 抽象外交封地。已解碼 Immersive Patrols（靜態巡邏/戰區）、Populated Skyrim Civil War（純 placed population）、OBIS Patrols Addon（route alias factory）、WARZONES（marker/activator warzone factory）與 Civil War Overhaul Redux（alias 固定席位 + reinforcement tickets）；Open Civil War 尚待取得 plugin 做 record 級解碼。

**與其他想法交集**：部隊跟隨 = §1 多隨從放大；商隊護衛/劫掠 = §3 共用；募兵/外交對話 = §9 餵內容；大規模事件調度 = §9 Story Manager；架空 worldspace = §4/§8 應用。

**技術難題（按致命度）**：

- **致命級（設計必須繞著走）**：① 戰鬥 AI 上限 → 會戰必須 **20v20 波次制**（陣亡補位 + 後台增援池），是設計前提非優化選項；② 攻城戰尋路（navmesh 靜態、AI 擠隘口卡死）→ 城在設計期預埋突破口 + 預鋪攻城動線 navmesh。
- **困難級**：③ 非平坦地形 + LOD（LOD 是真硬點，務實解 shell out xLODGen；短期小世界 + 霧遮遠景）；④ 聚落 navmesh（建築 footprint 挖洞三角化）；⑤ 戰略層 UI（CEF 是最好試驗場，原型用 message box + 書本保底）；⑥ Papyrus 陣列上限 128 → JContainers 必須，規模大終點是 SKSE native。
- **工程量級**：部隊跟隨照抄 EFF/NFF（catch-up teleport / 門口排隊）；NPC 騎乘戰鬥 AI 爛（騎兵大概率做成腳本化假騎兵）；聚落量產量大但近 ModForge 已有能力。

**已拍板決策（2026-06-04）**：

- **A. 玩家定位＝混合**——M&B 傭兵起步、後期解鎖三國志君主玩法；最小可玩先做 M&B 前段（募兵+野戰）。
- **B. 時間行軍＝即時派**——真的帶兵走、敵軍世界內真實移動（AI package 巡邏），「野外撞見敵軍」不事件化。
- **C. 依賴基線（適用所有想法，視為玩家標配）**：SKSE + SkyUI + JContainers + po3 Extender/Tweaks + Fuz Ro D'oh + **Pandora Behaviour Engine+** + **Community Shaders**（含 Light Limit Fix）。ModForge 配合：Papyrus 編譯認得第三方腳本源（PO3/JContainers/SKSE/SkyUI import path）、可考慮 MCM 鷹架生成、`package` 輸出 Pandora 認得的 Nemesis-format / native Pandora 目錄結構，美術可假設 Light Limit Fix（解除每 mesh 4 燈限，放大 §12）。
- **D. 世界規模＝先小後大**——~8×8 cells、3-5 城起步，霧遮遠景；「世界是 spec 生成的」保證日後重生成大世界不是重做。
- **E. 勢力/武將**按「N 勢力 M 武將」參數化，原型 3×5、成品 5-8 勢力 ×30+ 武將。
- **F. 兵種樹架空設計**，全用 vanilla 裝備模型拼。
- **G. 第一個垂直切片＝波次會戰原型**——平地 + 兩隊 spawn + 波次增援，驗證難題①手感；是整個企劃的試金石，且對 ModForge 需求最小。

**待深挖**：(a) Open Civil War plugin（war-map/策略層 UI 與 cut city battle 接法）；(b) 戰略層資料模型（城/武將/勢力狀態怎麼存、AI 決策規則）；(c) 聚落量產（一座城 spec → placed refs + N 勢力 marker 組）；(d) 玩家循環（募兵→帶兵→受封→自立的機制接點）。
