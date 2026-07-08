# 24. 遊戲內編輯器：「施法即編輯」→ 快照 cell 狀態 → 生成 patch mod（2026-07-07，2026-07-08 擴展：蓋城鎮北極星）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

**核心 Idea**：把**遊戲內本身**當成編輯器。玩家施放一組「編輯法術」直接在遊戲裡擺物件 / 放 NPC / 錄行為 /（野心）改地形，然後施法**快照當前房間（cell）狀態**，diff vs vanilla → ModForge 生成一份 **patch mod（override 記錄）**。等於用真實遊戲鏡頭 + 物理當所見即所得的編輯台，取代 CK。

**為何吸引 / 為何優於外部編輯器（決策依據，2026-07-07）**：CK 崩、Windows-only；外部編輯器（#15 Blender/CK 替代、#19 Godot Worldspace Editor）擺完**看不到真實渲染態**。使用者經驗回饋：**現有 [Godot worldspace editor](../../../sub_projs/godot-worldspace-editor/README.md) 實際用起來不好用**——它重建的是近似場景，不是玩家實際會看到的畫面。遊戲內編輯的決定性優勢是**吻合指定渲染狀態**：ENB、光照、特效、天氣、後處理全部就位，「你看到什麼就是成品」（真 WYSIWYG）。外部工具永遠追不上 ENB/community shaders 那層。→ **本 idea（遊戲內路線）相對 #15/#19（外部路線）是更受青睞的方向**；外部路線退為輔助/離線批次用途。

---

## 北極星情境：在遊戲內蓋一座城鎮並匯出（2026-07-08，使用者定調）

把上面的抽象能力串成一條**具體、可交付**的使用者旅程——這是本 idea 真正的驗收畫面：

> 玩家站在某片空地，喝一瓶「Plans: 木屋」→ 進定位模式 → 房子落地；再擺城牆、市集攤、路燈；把**自己現在這隻角色**（外貌 / 裝備 / perk / 法術全包）「拓印」成一個站在城裡的**獨立 NPC**，指定他是「鐵匠」；在鐵匠鋪門口放一個**語意標籤**、村口放一個**地圖 marker**、廣場中央放一個**篝火特效錨點**；最後施法**快照整片區域** → ModForge 收到一份場景 spec → `build` 出一個 **patch mod（一座能載入、能造訪、有住民、有 marker/特效、住民會講 ModForge 生成的對話）的城鎮**。

這條旅程把下面的**能力光譜**綁成一個垂直切片，並新增三塊使用者本次點名的東西：**(A) 玩家角色 → 獨立 NPC 的拓印**、**(B) 語意標註**（marker / 特效 / 標籤，非只實體物件）、**(C) 整場景 export**。三者都收斂回 ModForge 既有生成鏈，難點一律在「遊戲內採集 → spec 的橋」。

---

## 能力光譜（由易到難，可各自獨立落地）

- **① 快照 cell 狀態 → patch**（地基能力）：SKSE 列舉當前 cell 的 placed refs（座標/旋轉/縮放/enable state）→ diff vanilla → 輸出 override CELL + `placements[]`。ModForge 已有 cell/worldspace override 生成基礎（見記憶 [[worldspace-override-must-carry-topcell]]）。**這是整個框架的核心，先做這個**；「export 整座城鎮」（§C）就是這條放大到整片區域。
- **② 施法擺設 / 移動物件**：一支「擺放法杖」spawn / grab / 旋轉 / 吸附 refs（先例：SIGE 遊戲內 3D gizmo，見 #15 Gemini 調查）→ 快照時一起收進 ①。**Tundra Defense 就是這條的現成成品藍本**（喝瓶→定位→確認，見下 §D）。
- **③ 施法錄製 NPC 行為**（原 #24 小野心）：走一條路徑，沿途取樣座標放 PatrolMarker/IdleMarker + 停留動作 → 輸出 sandbox/travel/patrol package（見記憶 [[radiant-alias-package-byte-truths]]：package 掛在 alias 的 ALPS 上）。
- **④ 施法擺放 NPC / 拓印玩家角色 → 靠 PROTEUS**：用 [PROTEUS](../../../sub_projs/mod-survey/findings/proteus.md) 遊戲內生成 / 定位 / 控制 NPC 的既有能力當「放 NPC」前端；**進一步（本次新增）把 PROTEUS「序列化整個角色 build」的能力拿來把玩家自己拓印成獨立 NPC**——見下 §A。⚠️ PROTEUS 核心是**閉源 native DLL**，只能**消費**它、不能改它；若要自建放置也可走既有 `quest.spawn`（見記憶 [[dynamic-spawn-debugging]]）。
- **⑤ 施法修改地形（LAND）**：野心項，**技術牆**。runtime 編輯 LAND heightmap 極難，ModForge 目前僅支援平坦地形（見 #14/#15 地形段）。先擱置，優先做 cell 內物件 / NPC。
- **⑥ 語意標註（本次新增）**：不只擺實體物件，也**下「意圖標記」**——這裡放一個地圖 marker、那裡放一個特效錨點、門口貼一個身份/功能標籤 → ModForge build 時**展開成真記錄**（XMRK map marker / HAZD 或 placed VFX / KYWD tag）。見下 §B。**這是把編輯器從「擺模型」升級成「標作者意圖」的關鍵一步**，且幾乎全 generable-today。
- **⑦（最大野心，暫緩）**：變身 NPC + 錄軌跡途中施法插事件節點（對話/idle/換場景）→ 生成任務/scene。狀態機複雜，最後再碰。

---

## §A. 玩家角色 → 獨立 NPC 的拓印（PROTEUS serialize → ModForge generate）

使用者要的：把**當前玩家角色**（外貌 + 身上裝備 + perk + 法術 + 技能）弄成一個**獨立 NPC**，之後這個 NPC 由 ModForge 灌對話 / 行為。

**PROTEUS 正好補在 ModForge 最弱的一環**。ModForge 的 NPC 生成能力（grep `Spec.Actors.cs` / `Generator.Build.Actors.cs` 驗證，2026-07-08）：

| 玩家 build 的成分 | ModForge 能否生成到 NPC 記錄 | 證據 |
|---|---|---|
| **Perk** | **能** | `NpcSpec.Perks`（`Spec.Actors.cs:43`）→ 每個 PerkPlacement 掛上，entry-point 被動生效（vanilla 種族就這樣帶天生 perk）|
| **裝備 / 隨身物品** | **能** | `NpcSpec.Outfit`（DefaultOutfit ref）+ `Items[]` 隨身欄，武器/防具自動裝備 |
| **法術 / 技能 / 屬性** | **大致能** | spell 授予、AV 設定走既有 actor 生成；`autoCalcStats` 記憶 [[autocalc-without-class-dead-npc]]（要配 class）|
| **外貌（headpart / tint / 髮色 / 臉 morph / 體重）** | **❌ GAP（facegen）** | `Spec.Actors.cs` 無任何 headpart/tint/face 欄；grep 全 `src/ModForge.Core/` 無 facegen 生成。這是 **CK/facegen 領域**（NPC_ 記錄 + FaceGeom NIF + tint DDS），ModForge 目前不生 |

→ **關鍵洞見（分工天成）**：**外貌/facegen 正是 ModForge 最弱、而 PROTEUS（native DLL）最強的地方**。PROTEUS 的 6 個 JSON 模板（`Proteus_Character_GeneralInfo/_Skills/_Armor/_Weapon/_Spell`，見 finding）就是它 runtime 序列化整個角色 build（含外貌）的 schema。兩條路：

- **✅ 路徑 A（採納）：消費（比照 [#23 living-adventurers](../living-adventurers.md) 的「指向既有 ActorRef」哲學）**——讓 **PROTEUS 在遊戲內把玩家 clone 成一個實體 NPC**（外貌由它的 native code 現成搞定，白賺 facegen），ModForge **只指向那個 persistent ActorRef**，在外圈生成身份/對話/行為/擺放的 patch。**crux 已拍板（2026-07-08，使用者確認）：PROTEUS clone 是穩定、可引用的**——路徑 A 成立，facegen GAP 直接繞過。ModForge「指向外部 ActorRef 生 patch」是熟路（#23、sofia-patch、`esm-formid-access`）。→ 已進 spec，見 [ingame-scene-export-design](../../specs/ingame-scene-export-design.md)。
- **路徑 B（降為未來選項，非阻塞）**——PROTEUS **序列化玩家外貌成 JSON** → ModForge 讀 JSON **生一個真的 NPC_ base 記錄含 facegen**，讓產物**不依賴玩家端裝 PROTEUS**（完全自足、可散布）。需 ModForge 新增 facegen 生成（headpart/tint/morph → NPC_ + FaceGeom NIF + tint DDS，CK-territory 大 GAP，接 asset-pipelines headless facegen 研究）。路徑 A 跑通後若要「可散布獨立 NPC」再投資。

---

## §B. 語意標註 → ModForge 展開（marker / 特效 / 標籤）

使用者：能擺的不只建築/物件，也希望放**標示**，讓 ModForge 生額外東西如 marker / 特效 / 標籤。這幾乎全 **generable-today**（grep 驗證 2026-07-08）：

| 遊戲內放的「語意標記」 | ModForge build 展開成 | 證據 |
|---|---|---|
| 地圖 marker（村口/地城口，可快旅） | **XMRK map marker REFR** | `Spec.MapMarkers.cs`：MapMarkerSpec（Name/Type=City/Town/Cave…/flags=Visible\|CanTravelTo），記憶 [[worldspace-override-map-render-fields]] 已在 vanilla Tamriel 實機確認 |
| 特效錨點（篝火/魔法光/煙霧） | **HAZD radius-VFX** 或 placed 特效 REFR + Light | `Generator.Build.Hazards.cs`（HAZD：model/spell/imad/light/sound）；`Spec.Lights.cs`；光照管線記憶 [[lighting-pipeline-confirmed]] |
| 功能/身份標籤（「這是鐵匠鋪」「這是敵對區」） | **KYWD tag** 掛到 ref/NPC/cell | keyword 生成已在 `Spec.Actors.cs` 等多處（tag 驅動 condition / faction / package 選擇）|
| 邊界 / 領地標記 | boundary marker REFR + faction 安全 | Tundra §4.3 藍本；`Spec.Settlement.cs` `territory:` 方向（見 [settlements-phase2](../../roadmap/mod-survey-gaps/settlements-phase2.md)）|

→ **語意標註的價值**：把「遊戲內編輯」從**擺模型**提升到**標作者意圖**——玩家在現場點「這裡要一盞燈、這是鐵匠、這裡能快旅」，ModForge 把意圖翻成正確的記錄型別。採集橋只需輸出 `{kind: mapMarker|vfx|tag|boundary, at: 座標, params}`，生成端幾乎現成。

---

## §C. Export 整座場景（① 放大到城鎮尺度）

「快照 cell → patch」放大：把玩家蓋的**整片區域**（所有擺放 ref + §A 拓印的 NPC + §B 的語意標註）收成**一份 ModForge 場景 spec** → `build` 出**一個城鎮 patch mod**。

- **產物形狀**＝override CELL/WRLD + `placements[]`（含 enable-parent / initiallyDisabled，見 Tundra §2 佐證的「預置 disabled REFR + Enable 切換」量產藍本）+ NPC 記錄/引用 + map marker + VFX + tag。**全部是 ModForge 既有生成型別**（記憶 [[worldspace-override-must-carry-topcell]] / [[worldspace-override-map-render-fields]] / [[programmatic-navmesh]]）。
- **真正的工程**＝**採集橋**：一支消費型 SKSE DLL 走訪 cell、序列化每個 ref 的 base+transform+enable、把 §A/§B 的標記一起收進同一份 JSON。生成端 ModForge 大多已具備。
- **navmesh 缺口**：能走路的城鎮要 NAVM。ModForge 能生 programmatic navmesh（記憶 [[programmatic-navmesh]]），但**遊戲內即時採集/生成 navmesh 是硬項**，先擱置或半自動（先出可造訪不可完美尋路的版本）。

---

## §D. 身份標註 → ModForge 灌對話 / 行為（對話仍由 ModForge build-time 生）

使用者定調：**NPC 對話還是由 ModForge 這邊直接生成**，只是拓印/擺 NPC 時**指定其身份**，方便 ModForge 灌對話和行為。

→ 這正好接上 ModForge 既有的 **[#1c 多重身份 / 輕量職業系統](../followers.md#1c-多重身份--輕量職業系統)**（記憶 [[identity-system-confirmed]] 已實機）與 **[#23 living-adventurers 的 archetype 框架](../living-adventurers.md)**。設計對齊：

- 遊戲內拓印/擺 NPC 時，只多填一個 **identity/archetype tag**（鐵匠 / 守衛 / 商人 / 冒險者…）+ 幾行 backstory。
- ModForge build 時吃這個 tag → 生對應的**對話 INFO（condition 化問候/服務/閒聊）+ 行為 package + faction/service**——全走既有生成鏈（記憶 [[conditioned-hello-one-topic-many-infos]] / [[radiant-alias-package-byte-truths]] / vendor）。對話**不在遊戲內生**，遊戲內只**貼身份標籤**；文本可走 [#17 批量 AI 生成管線](../followers.md#17-skyrim-原版任務節點圖--批量隨從反應生成2026-06-15)（backstory → 對話草稿）。
- 與 §A 天然對接：拓印玩家成鐵匠 = 拿到那個 ActorRef + identity=blacksmith → ModForge 灌鐵匠對話/擺攤 package/販售 service。

→ **分工再清一次**：**遊戲內＝擺位置 + 貼身份標籤（採集）**；**ModForge＝把身份翻成對話/行為/服務（生成）**。對話生成不下放到遊戲內，維持 ModForge build-time 的本命。

---

## PROTEUS 的角色定位（更新）

不是要改它，而是它三點正好補位——(a) 遊戲內生成/控制 NPC（能力 ④ 前端）、(b) **序列化整個玩家 build（含外貌 facegen）＝ ModForge 最弱環節的補丁**（§A）、(c)「遊戲中 JSON 序列化狀態」概念先例（與 ModForge build-time JSON 方向相反、schema 互通）。核心 native 閉源、無可生成成分（見 finding 結論），所以是**消費 / 補位 / 仿概念**，非依賴元件；理想終局（路徑 B）是把 facegen 生成能力自建進 ModForge，讓產物不依賴玩家端 PROTEUS。

## Tundra Defense 的角色定位（本 idea 的擺放-UX 藍本）

[Tundra Defense](../../../sub_projs/mod-survey/findings/tundra-defense.md) 是**能力②「施法擺設」的現成完整成品**：喝一瓶 `Plans: X` Ingestible → script-MGEF 觸發 spawner `PlaceAtMe` → `aaaFortMainQuestScript` 即時 follow/rotate/confirm 定位狀態機 → 確認落地為 Enabled REFR。**北極星情境的「喝瓶蓋房子」互動就是照抄這套**。ModForge 對 Tundra 的裁決（見 [settlements-phase2](../../roadmap/mod-survey-gaps/settlements-phase2.md)）：**所有靜態零件可生成**，唯一不可生成的是那支**常駐 placement-controller `.pex`**（irreducibly bespoke Papyrus）——但 ModForge 有「隨附 reusable `.pex` + `scriptAttach` 掛接」的成熟先例（MCM-Helper/dispatcher/PapyrusUtil）。→ **建議 ModForge 內建一支泛用 placement-controller `.pex`**，本 idea 的「施法擺設」與 settlements Phase-2 的 `buildables:` **共用同一支 controller**，兩條線合流。

---

## ModForge 落點總表（generable-today / GAP，grep `src/ModForge.Core/` 驗證 2026-07-08）

| 環節 | 狀態 | 缺口 / 依賴 |
|---|---|---|
| ① 快照 → override CELL + `placements[]` | 生成端 **可** | 缺**採集橋 DLL**（讀 cell ref 狀態 → JSON）|
| ② 施法擺設定位 | 靜態零件 **可**；定位行為**需 controller** | 內建泛用 placement-controller `.pex`（同 Tundra，合流 settlements P2）|
| §A 拓印玩家：perk/裝備/法術/技能 → NPC | **可**（`NpcSpec.Perks`/`Outfit`/`Items`）| — |
| §A 拓印玩家：**外貌 facegen** | **✅ 由 PROTEUS 補位** | 路徑 A（採納，clone 穩定可引用）繞過 GAP；路徑 B（ModForge 自建 facegen）降為未來「可散布獨立 NPC」選項 |
| §B marker / 特效 / 標籤 | **可**（XMRK/HAZD/Light/KYWD 全有）| 缺採集橋輸出 `{kind,at,params}` |
| §C export 整場景 | 生成型別 **全可** | 採集橋（同①放大）；**navmesh 即時採集是硬項** |
| §D 身份 → 對話/行為 | **可**（identity 系統 + 對話 INFO + package + vendor 全實機）| 遊戲內只貼 tag；文本可接 #17 AI 生成 |
| 編輯器 UI（遊戲內面板）| **純參考、非可生成** | [SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)（ImGui）現成前端，但選單是編譯進消費 DLL 的 C++ |

**收斂成一句**：整條管線的**生成端 ModForge 幾乎都有**（唯一真 GAP＝外貌 facegen 生成，且可用 PROTEUS 消費繞過）；**真正要蓋的是中間那支 bespoke 消費 SKSE DLL**——採集橋（讀狀態/貼標籤 → JSON）+ 一支泛用 placement-controller `.pex`。同 Tundra/Honed Metal「須附 native/Papyrus controller」判定。

---

## 建議的最小垂直切片（驗證北極星）

照北極星情境切最小可跑：**擺 1 棟房子（喝瓶→定位→落地，②/Tundra controller）+ 拓印玩家成 1 個 NPC 並標 identity=blacksmith（④/§A 路徑 A + §D）+ 放 1 個地圖 marker + 1 個特效錨點（§B）+ 快照整片 → ModForge build 出可造訪的迷你據點（§C）**。先驗「能擺、能拓印、能標註、能匯出、住民有對話」五件事，再擴。**先決調查**：✅ 已解（PROTEUS clone 穩定可引用，走路徑 A）；**先決生成改動**：無（§B/§D 已具備，②的 controller 與 settlements P2 合流一起做）。→ 已展開成 spec，見 [ingame-scene-export-design](../../specs/ingame-scene-export-design.md)。

## 關聯

- **[settlements Phase-2](../../roadmap/mod-survey-gaps/settlements-phase2.md)**：`buildables:` 的 placement-controller 與本 idea 的「施法擺設」**共用同一支 controller**——兩線合流，是最強的協同。
- **[#23 living-adventurers](../living-adventurers.md)**：§A 路徑 A 的「指向既有 ActorRef」哲學同源；拓印出的 NPC 可直接 enroll 進活世界模擬。
- **[#1c 多重身份系統](../followers.md#1c-多重身份--輕量職業系統)**（記憶 [[identity-system-confirmed]]）：§D 身份→對話的既有落地基礎。
- #15（CK 替代視覺編輯器，本 idea 是遊戲內路線）；#19 Godot Worldspace Editor（外部編輯器另一路）；#17（任務節點圖，§D 對話 AI 生成管線）。
- [PROTEUS finding](../../../sub_projs/mod-survey/findings/proteus.md)（§A 核心依據）；[Tundra Defense finding](../../../sub_projs/mod-survey/findings/tundra-defense.md)（②擺放-UX 藍本）；[SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)（編輯器 UI 前端）。
