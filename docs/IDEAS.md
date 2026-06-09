# 模組創作想法隨記

個人構想備忘，未必有明確優先順序，隨時增補。
**已落地功能的實作細節見 `CLAUDE.md`「已落地功能」與 git log**——本檔只留「想做的事」與判斷依據（決策、鐵律、缺口）。

---

## 1. 擴充停止更新的隨從模組

許多高品質隨從模組已停更，想在其上擴充：

- 補日常對話與情境反應（旅途閒聊、特定地點/事件台詞）
- 深化與玩家互動（任務後感想、好感度觸發對話）
- 多隨從互相對話（A 評論 B、爭執、調侃）

**在場偵測**：不靠多隨從框架，用 vanilla 三層——同 Cell（已載入）→ `GetDistance < 2048`（夠近）→ `HasLOS`（看得見，按需）。「是否在隊」用 `IsPlayerTeammate()`，自訂跟隨機制才讀其 Quest。**已落地為 `MFSceneBanterController` autoStart**（見 CLAUDE.md）。

**Scene 前提**：Scene 的 Actor 必須是同 Quest 的 Alias（`ForceRefTo()` 填入；注意死亡/解散/未載入時的釋放）；輪詢用 `RegisterForSingleUpdate` 鏈式（勿用 OnUpdate 持續循環——存檔膨脹）。

**語音前提（所有對話想法共通）**：Skyrim 無語音檔的台詞字幕一閃即過。預設假設玩家裝 **Fuz Ro D'oh**；要讓新台詞像「本人」說的需 AI 語音合成（xVASynth voice cloning / ElevenLabs），屬之後的工作流，不在 ModForge 本體。

---

## 1b. NPC 劇情演出（Scene 驅動）

特定時機（如玩家選某選項後）讓 NPC 完整演出：走到指定地點 ✅、播動畫 ✅（PlayIdle，限 vanilla 腳本跑過的 IDLE）、使用場景物件 ✅、NPC 間對話、可選鏡頭。**已落地見 CLAUDE.md；剩「附帶鏡頭」（Camera Shot CAMS）未做**——簡單演出不需要，之後再補。

---

## 1c. 多重身份 / 輕量職業系統

做某些事 → 取得身份（聖騎士/商人/冒險者/龍裔…）→ 賦予技能與常駐加成 + 解鎖專屬互動 → 互動回頭強化身份（近似 D&D 職業）。NPC 用「當前主身份」稱呼你；身份可疊加、主身份按優先序解析；取得走「讀書宣誓 / faction 會員」式。

設計見 `docs/superpowers/specs/2026-06-06-identity-system-design.md`；**MVP 進行中**（plan `docs/superpowers/plans/2026-06-07-identity-system-mvp.md`，待實機）。前置 PlayIdle scene-action 已落地。子專案切分：身份系統本體 → 身份對應互動（交易 UI、護衛任務…）。

---

## 2. 喜愛劇情模組的遺憾分支改版

劇情模組關鍵節點常缺想要的選擇，想自己補：做平行分支讓玩家走「作者沒寫的那條路」；保留原人設世界觀、盡量以 Patch 形式存在；可能涉及新 INFO/對話樹、條件觸發、任務階段。

---

## 3. 商隊與船隊生活

流浪商人視角：加入/組建陸路商隊沿固定路線交易；船隊（海路、港口停靠買賣）；**空艇冒險**（Airship 作移動基地穿越異域）。可能需自訂 AI Package（巡邏路線）+ 商業系統 UI。

---

## 4. 異世界冒險（另開 Worldspace）

開全新 Worldspace、設定迥異於泰姆瑞爾；以穿越/傳送門進入並有劇情驅動；主題不限（奇幻異界、蒸汽龐克、廢土…）。

---

## 5. 其他遊戲資源移植 / 引擎復現

把電腦上其他遊戲的場景/角色/玩法概念「翻譯」進 Skyrim——不是完整移植，而是用 Skyrim 的敘事與互動語言重現精髓。需評估資源格式轉換（見 §14）與遊戲規則的系統化對應。⚠️ 法律面：他遊資產轉了不能發布。

---

## 6. 在 SkyUI 基礎上擴充 UI

先例：快捷欄擴充（iEquip、Wheeler）。想加技能槽（快速切換施法序列）、任務追蹤懸浮框、小地圖增強。核心挑戰：SkyUI 以 ActionScript/Flash 實作，需 AS3 / Scaleform 知識。

---

## 7. 遊戲內嵌入網頁 UI

在 Skyrim 視角內顯示可互動「瀏覽器」面板（CEF + SKSE）。應用：遊戲內查攻略、顯示 AI 代理回傳資訊、即時地圖。技術難度高，需 SKSE/C++ 介入。

---

## 8. 程序生成的世界

地形/地城/NPC 組成/事件都帶程序生成成分。參考 Requiem 縮放 + Radiant Story 延伸 + 自訂世界生成。ModForge 的 Generator 可作批次生成「骨架 ESP」起點。長期：每次開新檔世界佈局不同。

---

## 9. 大量劇情自動生成（獨立工作流）

手寫規格無法擴展，需 LLM 驅動的生成管線：

```
故事生成系統（獨立工作流）          ModForge（下游，記錄層）
  ├─ LLM 構思劇情/人物弧線/對話        └─ spec → 合法 ESP，不參與敘事
  ├─ 展開成 ModForge spec JSON
  └─ 呼叫 build → .esp
```

**故事系統自己要解的難題**：跨任務 NPC 狀態記憶；人物個性一致；大量劇情不重複；語音必須排進管線（見 §1）。

**引擎規模天花板（量產前必面對）**：載入順序上限（ESP ~254 / ESL ~4096）→「一任務一 ESP」走不遠，要合併輸出或回收；ESL FormID 預算（2048/4096，一條有對話的任務吃幾十~幾百）；存檔膨脹（每個有腳本的 running Quest 都進存檔）→ 須「完成即 Stop + 清 Alias」。

**量產關鍵槓桿：Story Manager + 條件式 Alias**——輸出「模板任務 + 條件 Alias」而非寫死 NPC，同一 ESP 劇情變化量放大一個數量級。引擎帶事件資料走訪 SM 節點樹、逐層評估條件、Alias 動態填充，全部成功才啟動。**此管線已落地並實機驗證**（SM spec 管線、十種 engine-native 事件、五種 alias fill、可複用 trigger 庫——細節與鐵律見 CLAUDE.md / git）。

**ModForge 可貢獻的未做想法 — `catalog` 資源索引**：故事系統需知「一隻狼 / 一條麵包用哪個 FormKey」。擴充 `catalog` 把 Skyrim.esm（或任意 ESP）批次匯出成**可查詢索引**（SQLite / 分類分片，非單一大 JSON——幾十萬筆 record LLM 讀不完）。兩層內容：

- **資料層**：FormKey / EditorID / 名稱 / 類型 + 關鍵屬性（種族等級、回復量、傷害…）
- **美術層**：NPC 外型、模型/貼圖路徑、語音類型、idle 動畫 event、地點清單；QUST/DIAL/INFO（含第三方模組，避免衝突重複）；FACT/BOOK/RACE/KYWD/WTHR…（原則上涵蓋所有記錄類型）

現有診斷（`npcdiag`/`dump`/`find`）已能拉這些欄位，批次化即可產出。

---

## 10. 翻譯 + 插件合併

- **翻譯**：`extract`/`apply`/`applyloc`（含 UTF-8 `_chinese.STRINGS`）已可用，英文模組中文化直接用。
- **ESP/ESL 合併（未做）**：合併小插件釋放載入順序空位，對 §9 量產尤重要；要處理 FormID 重映射 + 所有引用（含腳本屬性、SEQ）同步改寫——工程不小，Mutagen 有基礎能力。

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

**引擎硬限制（決定設計上限）**：同屏活躍戰鬥 AI 超過 ~30-50 就掉幀 / AI 崩壞 → 百人會戰不能硬做，要分波增援 / 戰場區隔 / 小隊抽象；Skyrim 無「大地圖」→ 世界地圖 + 快速旅行事件化，或選單 / 書本 UI 抽象外交封地。先研究 Open Civil War / Immersive Patrols 怎麼處理規模。

**與其他想法交集**：部隊跟隨 = §1 多隨從放大；商隊護衛/劫掠 = §3 共用；募兵/外交對話 = §9 餵內容；大規模事件調度 = §9 Story Manager；架空 worldspace = §4/§8 應用。

**技術難題（按致命度）**：

- **致命級（設計必須繞著走）**：① 戰鬥 AI 上限 → 會戰必須 **20v20 波次制**（陣亡補位 + 後台增援池），是設計前提非優化選項；② 攻城戰尋路（navmesh 靜態、AI 擠隘口卡死）→ 城在設計期預埋突破口 + 預鋪攻城動線 navmesh。
- **困難級**：③ 非平坦地形 + LOD（LOD 是真硬點，務實解 shell out xLODGen；短期小世界 + 霧遮遠景）；④ 聚落 navmesh（建築 footprint 挖洞三角化）；⑤ 戰略層 UI（CEF 是最好試驗場，原型用 message box + 書本保底）；⑥ Papyrus 陣列上限 128 → JContainers 必須，規模大終點是 SKSE native。
- **工程量級**：部隊跟隨照抄 EFF/NFF（catch-up teleport / 門口排隊）；NPC 騎乘戰鬥 AI 爛（騎兵大概率做成腳本化假騎兵）；聚落量產量大但近 ModForge 已有能力。

**已拍板決策（2026-06-04）**：

- **A. 玩家定位＝混合**——M&B 傭兵起步、後期解鎖三國志君主玩法；最小可玩先做 M&B 前段（募兵+野戰）。
- **B. 時間行軍＝即時派**——真的帶兵走、敵軍世界內真實移動（AI package 巡邏），「野外撞見敵軍」不事件化。
- **C. 依賴基線（適用所有想法，視為玩家標配）**：SKSE + SkyUI + JContainers + po3 Extender/Tweaks + Fuz Ro D'oh + **Nemesis** + **Community Shaders**（含 Light Limit Fix）。ModForge 配合：Papyrus 編譯認得第三方腳本源（PO3/JContainers/SKSE/SkyUI import path）、可考慮 MCM 鷹架生成、`package` 輸出 Nemesis 認得的目錄結構、美術可假設 Light Limit Fix（解除每 mesh 4 燈限，放大 §12）。
- **D. 世界規模＝先小後大**——~8×8 cells、3-5 城起步，霧遮遠景；「世界是 spec 生成的」保證日後重生成大世界不是重做。
- **E. 勢力/武將**按「N 勢力 M 武將」參數化，原型 3×5、成品 5-8 勢力 ×30+ 武將。
- **F. 兵種樹架空設計**，全用 vanilla 裝備模型拼。
- **G. 第一個垂直切片＝波次會戰原型**——平地 + 兩隊 spawn + 波次增援，驗證難題①手感；是整個企劃的試金石，且對 ModForge 需求最小。

**待深挖**：(a) 戰略層資料模型（城/武將/勢力狀態怎麼存、AI 決策規則）；(b) 聚落量產（一座城 spec → placed refs + N 勢力 marker 組）；(c) 玩家循環（募兵→帶兵→受封→自立的機制接點）。

---

## 12. 明亮美術基調 / 光照管線（2026-06-04）

Skyrim 光照太陰暗，偏好原神/薩爾達那種明亮——vanilla 只白天 worldspace 有，地下城/洞窟一律暗（偏偏玩家大部分時間在裡面）。**核心認知：暗是美術方向不是引擎限制，光照幾乎全是記錄層的事，正是 ModForge 主場**：

- 室外：Weather 內建調色盤（日光/環境/霧/天空 × 黎明~夜晚）+ **IMGS（ImageSpace，HDR 眼適應/bloom/飽和）**——「亮乾淨高飽和」大半是 IMGS。
- 室內：CELL Lighting（+ DALC 六方向環境光）+ **LGTM（地城多用 DefaultDungeon 暗模板）** + 稀疏 LIGH——全是「選擇」（Zelda 神廟也封閉但它亮）。
- 引擎真限制：無 GI（正解：環境光打底 + 少量光源做層次）；每 mesh 4 燈——**Community Shaders + Light Limit Fix 已入基線（§11-C），視為解除**；卡通渲染要 shader 級（CS feature 是可能出路）。

**ModForge 光照管線 — ✅ 室內+室外皆落地（in-game 確認 2026-06-09，見 CLAUDE.md「已落地」/ `SPEC-world.md § lighting`）**：① CELL 逐欄光照進 spec ✅；② 自製明亮 LGTM 模板（模板抄+覆寫，含 DALC）✅；③ 自訂 IMGS 掛 **cell ✅** 與 **weather per-ToD ✅**（`weathers[].imageSpaces`）；④ `lgtmdiag`/`imgsdiag`/`weatherdiag` ✅；⑤ **`WeatherSpec.template`** 抄 vanilla 天氣繼承雲/天空 ✅（from-scratch 天氣無雲，室外務必抄 template）。**剩下**：明亮 LGTM/IMGS 抽成具名 preset 庫；weather/IMGS 掛 region。

---

## 13. 通用 NPC 美化：morph 空間轉換規則（2026-06-04）

**核心：不是「換成哪種美術」，而是一個轉換規則（morph 空間 → morph 空間的函數）**——讀每個 NPC 原版滑條（編碼了個性），按規則轉成另一模型系統的滑條，全 load order（含 mod 新增 NPC）自動套用，且「在新美術下還認得出是她」。二次元只是其中一個資產包，同管線可換寫實高模頭 / COtR 頭。

**為何現有美化做不到**：替換包硬覆蓋每個 NPC 記錄 + 按 FormID 預烘焙 FaceGen → mod 新增 NPC 漏網、記錄改但 FaceGen 沒配對 = 黑臉 bug。病根是「臉是 per-NPC 烘焙」（身體是 race 級替換故無此問題）。

**輸入端**：NPC_ 的 Face Morph（19 float）+ Face Parts 離散預設 + HDPT + tint（CK 手雕的個性部分只在烘焙 nif 頂點、滑條讀不回，轉換只能近似——風格化美術可接受）。**轉換規則本身就是 spec**：兩邊都是 blendshape 係數空間，可做宣告式對照表，每個目標模型寫一份（一次性），之後全 NPC 自動轉——與翻譯支柱同構（讀插件 → 確定性變換 → 輸出 patch）。

**身體側已驗證此模式**（OBody/AutoBody 按規則套 BodySlide 滑條、SKEE 執行期應用），臉側是空白（SynthEBD 只到貼圖/資產分配層級）。**執行落點兩條路**：執行期（patch 換 head parts、morph 由 SKEE/RaceMenu 套，繞開 FaceGen；Proteus 走過，相容性是難點）或離線烘焙（套 blendshape 算頂點寫 nif，屬資產層超出 Mutagen，或 shell out CK `-ExportFaceGenData`）。二次元真實成本不在臉（動漫頭整顆 mesh 反繞開 FaceGen），而在 **vanilla 裝備 refit + 比例動畫適配**——務實順序：先用寫實資產驗管線。

---

## 14. 資產格式轉換管線（glTF/FBX → NIF）（2026-06-04）

主流 3D 格式 → Skyrim 全自動轉換：**「網格」可以，「全套」不行**，卡點集中：

| 內容 | Skyrim 格式 | 自動化可行性 |
|---|---|---|
| 網格/材質 | `.nif`（SSE BSTriShape） | 高（PyNifly / ck-cmd） |
| 貼圖 | `.dds`（BC + mipmaps） | 完全自動（純轉碼） |
| 表情/morph | `.tri` | 高（兩邊都是頂點 delta） |
| 動畫/骨架/物理 | `.hkx`（Havok 二進位） | **這就是那道牆** |

- **靜態物件最接近全自動**：補碰撞（NIF `bhk*` 也是 Havok，但簡單凸包/box 可程式生成）+ 材質映射規則（glTF PBR ↔ `BSLightingShaderProperty`，寫一次批次套）。
- **蒙皮網格半自動**：綁 Skyrim 骨架（`NPC Spine [Spn1]` 命名）、每頂點 ≤4 骨權重、`BSDismemberSkinInstance` 分區；「來源骨架 → Skyrim 骨架」retarget 每體系寫一次（同 §13 哲學）。
- **動畫是真正的牆**：Havok SDK 不公開，社群靠 ck-cmd/hkxcmd 包舊 SDK（版本敏感）；behavior graph 完全無自動轉換（Nemesis/Pandora 領域）。
- **對其他想法的意義**：§13 二次元路線（VRoid/MMD 頭身可管線化、卡在動畫）；§5 資源移植（靜態場景物件是甜蜜點）。⚠️ 他遊資產轉了不能發布。
- **ModForge 視角**：這是**資產層管線**（與記錄層 Mutagen 平行的另一軸）；`package` 已打包 Meshes/Textures，上游接轉換是自然延伸；PyNifly 可腳本化（shell-out 候選，同 xLODGen 態度：不自造）。

---

*最後更新：2026-06-04（2026-06-07 壓縮：已完成的實作軌跡移至 CLAUDE.md / git，本檔聚焦想法與決策）*
