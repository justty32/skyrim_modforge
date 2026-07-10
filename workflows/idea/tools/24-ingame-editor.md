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
- **② 施法擺設 / 移動物件**：一支「擺放法杖」spawn / grab / 旋轉 / 吸附 refs（先例：SIGE 遊戲內 3D gizmo，見 #15 Gemini 調查）→ 快照時一起收進 ①。**Tundra Defense 就是這條的現成成品藍本**（喝瓶→定位→確認，見「Tundra 角色定位」段）；定位軸 Tundra 有旋轉+距離、**縮放/位移要自補 mode**（見該段）。**但 Tundra 的可擺清單是設計期寫死的 REFR FormID——本 idea 用 §E 的「編輯法術組」把它變開放式**：
  - **滴管（單點吸取）**：準星吸一個 ref 的 base+旋轉+縮放進具名插槽（+吸中成功特效）→ 選插槽擺放。
  - **範圍吸取**：一次吸半徑內所有 ref（整叢佈景）進捕獲集。
  - **移除物件（橡皮擦）**：準星指一個 ref → 移除（自擺的直接刪；既有 vanilla ref → `removals[]` 生 disable/delete override）。
  - 三支細節 + ModForge 落點見 **§E**（滴管/範圍吸取生成端零改動；移除既有 vanilla ref＝`removals[]`，**已落地** 2026-07-08）。
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

> **🔄 2026-07-10 使用者定調：PROTEUS 降為可選。** 預設的 NPC 來源是 **「大眾臉」路徑 C**——ModForge 直接生 `NpcSpec`（有 `Race`，**無** headpart/tint/facegen 欄位 → 引擎用種族預設頭）。這條**今天就能跑**（vendor / hireable follower / identity 系統都實機出貨過），產物完全自足、玩家端不需裝任何東西，而且它的 placement 是 in-spec authored ⇒ **ref 有耐久 id**，`npcRoles[].actorRef` 指得到。想要一群沒名字的村民時，facegen GAP 根本不在關鍵路徑上。
>
> 路徑 A（拓印玩家本人的臉）保留為**可選加值**，不再是 §A 的唯一解、也不再阻塞 MVP。它另有一個未解風險：clone 出來的 actor **ref 必然是 dynamic**（`PlaceAtMe`），拿不到耐久 ref id；若其 NPC_ base 也是 runtime 生成，採集橋會直接把它 `skipped`。下面「crux 已拍板：clone 穩定可引用」須釐清指的是 **base 還是 ref**——實機待驗。

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

## §E. 開放式調色盤：滴管取樣 + 具名插槽（取代 Tundra 的寫死目錄，2026-07-08 使用者定調）

**Tundra 的死穴**：可擺的建物是**設計期寫死的 REFR/base FormID**（109 個 `Plans:` Ingestible 一一對應固定 Activator，見 finding §2）。要加新東西得改 esp。**使用者要的**：一支**滴管法術**——施放時記下**準星指向目標的 base FormID**（像小畫家滴管吸色），存進一個插槽；之後選那個插槽就能擺該物件。**多插槽 + 可自行命名**，等於玩家在遊戲內即時建自己的**開放式調色盤**（想擺什麼就吸什麼——任何 mod 的牆、樹、家具、雕像…）。

**機制（grounded，API 標「待驗確切呼叫」）**：

1. **吸取**：滴管法術 OnEffectStart → 讀**準星 ref**（`Game.GetCurrentCrosshairRef()`，SKSE；比「投射物命中」穩，因 STAT 靜物不吃魔法效果）→ 取其 **base**（`ObjectReference.GetBaseObject()` → Form）+ **當前旋轉/縮放**（`GetAngleX/Y/Z()` + `GetScale()`，2026-07-08 使用者要一起吸）→ 拿 FormID + rot + scale。**吸 base + transform**：滴管取的不只「顏料＝物件種類」，還帶被吸物件的姿態（轉了 45°、放大 2× 的那個樣子），之後 `PlaceAtMe(base)` 落地時**預設回填吸到的 rot/scale**，再用 controller 微調。
   - **✨ 吸取成功特效（2026-07-08 使用者要）**：吸中瞬間在**被吸 ref 上播一個夠明顯的視覺回饋**——`EffectShader.Play(crosshairRef, ~1.5s)`（vanilla 有現成發光 shader，如 soul-trap/transmute 那類）或 `crosshairRef.PlaceAtMe(ArtObject)` + 一聲 UI 音效。**純 runtime 回饋，不進 scene.json、與 ModForge 無關**；只需選/附一個 EffectShader 資產。
2. **具名插槽**：把吸到的 `{Form, rot, scale}` 存進 **StorageUtil KV**（string-key：`slot 名 → Form` + 平行存 rot/scale，memory [[storage-writes-ingame-confirmed]] J-group 已實機）。多插槽＝多 key；命名走文字輸入 UI（PROTEUS 用的 UILib `TextInputMenu`，或 [SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md) ImGui 輸入框——與編輯器面板同一套 UI 元件）。
3. **擺放**：選插槽 → `PlaceAtMe(slot.Form)` → **回填 slot 的 rot/scale**（`SetAngle`/`SetScale`）→ 進 §②/Tundra 的 placement-controller 定位模式微調落地。**滴管只是把「喝哪瓶」從固定目錄換成動態插槽**，落地那半段完全共用 controller。
4. **⚠️ 耐久 FormID 的關鍵坑（runtime 側，非 ModForge 側）**：runtime FormID 的高位元組 = load-order index，**跨載入順序不穩**。當次 session 內擺放無妨（StorageUtil 存 runtime Form 直接可 `PlaceAtMe`）；但**匯出進 scene.json 時必須反解成耐久的 `<plugin>:0xLOCALID`**——這要 SKSE `TESDataHandler`（`LookupModByIndex`/把 formID 高位→mod 名 + 取本地 ID），純 Papyrus 做不到。→ **這是採集橋 SKSE DLL 的活**（見 §C / spec 的採集橋元件），不是 controller 或 ModForge 的。

**對 ModForge 側的衝擊＝零（已驗 2026-07-08）**：滴管吸來的 base 進 `placements[].base`（形如 `SomeMod.esp:0x001234`），而 **ModForge 對外部 ref 會自動把來源 mod 加為 master**（`PluginIo.cs:35`：有外部 ref 就用 Mutagen 預設 `Iterate` 算精確 master 集；`TryResolveRef` 全面吃 `<plugin>:0xID`）。所以「吸任意 mod 物件 → 擺 → 匯出 → build」在生成端**天然成立、零改動**——代價只是**產物 patch 的 master 清單會隨你吸過的東西增長**（可接受；且可在匯出時提示「本場景依賴這些 mod」）。

### §E 的編輯法術組（2026-07-08 使用者擴充：不只單點滴管，要三支法術）

| 法術 | 幹嘛 | 機制（runtime）| ModForge 側衝擊 |
|---|---|---|---|
| **① 滴管（單點吸取）** | 準星吸一個 ref 的 base+rot+scale 進插槽 + 成功特效 | 上述 `GetCurrentCrosshairRef`→`GetBaseObject`/`GetAngle`/`GetScale` + `EffectShader.Play` | **零**（進 `placements[]`）|
| **② 範圍吸取** | 一次吸**半徑內所有 ref**（整叢建物/佈景）進捕獲集或群組插槽 | 需**列舉半徑內 refs**——SKSE（PO3 Papyrus Extender `FindAllReferencesOfType`/`FindAllReferencesWithKeyword`）或**重用採集橋的 cell 走訪、以半徑 bound**；每個 ref 取 base+transform+scale | **零**（一樣是一批 `placements[]`；只是來源是範圍不是單點）|
| **③ 移除物件（橡皮擦）** | 準星指一個 ref → 標記移除 | session 內自擺的 dynamic ref → 直接 `Delete()`；**既有 vanilla ref → 記進 scene.json `removals[]`** | **✅ 已落地**（`BuildRemovals`）|

**③ 移除物件（已落地 2026-07-08）**：`removals: ["<master>:0xFORMID", …]` → `BuildRemovals` 用 master link cache `TryResolveContext<IPlaced>` → `GetOrAddAsOverride(mod)`（自動把 parent cell/worldspace 一起 override 進來）→ 設 `InitiallyDisabled`(0x800) + 深埋 Z−30000（避 havok 殘留）。標準「disable vanilla clutter」patch、可逆。RequiresSkyrim（要 master link cache）。其餘兩支法術（滴管/範圍吸取）生成端仍零改動。

→ **§E 把 §② 從「固定目錄」升級成「開放調色盤」，且不動 scene.json 契約也不動 ModForge**：新增能力全落在 runtime 側（滴管法術 + StorageUtil 插槽 + 命名 UI + 採集橋的 FormID 反解）。這是相對 Tundra 最有感的體驗升級。

---

## PROTEUS 的角色定位（更新）

不是要改它，而是它三點正好補位——(a) 遊戲內生成/控制 NPC（能力 ④ 前端）、(b) **序列化整個玩家 build（含外貌 facegen）＝ ModForge 最弱環節的補丁**（§A）、(c)「遊戲中 JSON 序列化狀態」概念先例（與 ModForge build-time JSON 方向相反、schema 互通）。核心 native 閉源、無可生成成分（見 finding 結論），所以是**消費 / 補位 / 仿概念**，非依賴元件；理想終局（路徑 B）是把 facegen 生成能力自建進 ModForge，讓產物不依賴玩家端 PROTEUS。

## Tundra Defense 的角色定位（本 idea 的擺放-UX 藍本）

[Tundra Defense](../../../sub_projs/mod-survey/findings/tundra-defense.md) 是**能力②「施法擺設」的現成完整成品**：喝一瓶 `Plans: X` Ingestible → script-MGEF 觸發 spawner `PlaceAtMe` → `aaaFortMainQuestScript` 即時 follow/rotate/confirm 定位狀態機 → 確認落地為 Enabled REFR。**北極星情境的「喝瓶蓋房子」互動就是照抄這套**。

**定位軸：Tundra 有旋轉+距離、沒縮放也沒自由位移（2026-07-08 使用者要縮放+位移，逆讀 .pex 查證）**——`aaaFortMainQuestScript` 的 state 機有 `MODE_ROTATE_X/Y/Z`+`AXIS_X/Y/Z`+`ChangeRotationAxis`+`RotationSpeed`（三軸旋轉）、`MODE_DISTANCE`+`placeDistance`+`DistanceSpeed`（沿視線推遠拉近）+`MODE_RESET`，**但無 `MODE_SCALE`、也無 `MODE_TRANSLATE`（自由三軸位移微調）**——Tundra 定位是「看哪擺哪 + 距離」，沒有把物件沿 X/Y/Z 精確 nudge（例如把牆貼齊另一面牆）的能力。→ **旋轉照抄 Tundra；縮放 + 位移要在 placement-controller 各補一個 mode**：`MODE_SCALE`（`SetScale`）、`MODE_TRANSLATE`（讀 `GetPositionX/Y/Z`+delta→`SetPosition`，或 `TranslateTo` offset；沿軸 nudge）——都是 vanilla Papyrus、共用 `AXIS_X/Y/Z`+Plus/Minus 輸入、很便宜。**ModForge 生成端三者全已支援**（`PlacementSpec.Position`/`Rotation`/`Scale`(XSCL)），匯出/生成**零改動**，只差 runtime controller 加這兩個 mode。⚠️ **XSCL 對 actor 無效（ACHR 忽略縮放，`Spec.World.cs:43`）**——縮放只對靜物/家具/光，**拓印的 NPC 不能縮放**（位移/旋轉對 actor 都正常）。ModForge 對 Tundra 的裁決（見 [settlements-phase2](../../roadmap/mod-survey-gaps/settlements-phase2.md)）：**所有靜態零件可生成**，唯一不可生成的是那支**常駐 placement-controller `.pex`**（irreducibly bespoke Papyrus）——但 ModForge 有「隨附 reusable `.pex` + `scriptAttach` 掛接」的成熟先例（MCM-Helper/dispatcher/PapyrusUtil）。→ **建議 ModForge 內建一支泛用 placement-controller `.pex`**，本 idea 的「施法擺設」與 settlements Phase-2 的 `buildables:` **共用同一支 controller**，兩條線合流。

---

## ModForge 落點總表（generable-today / GAP，grep `src/ModForge.Core/` 驗證 2026-07-08）

| 環節 | 狀態 | 缺口 / 依賴 |
|---|---|---|
| ① 快照 → override CELL + `placements[]` | 生成端 **可** | 缺**採集橋 DLL**（讀 cell ref 狀態 → JSON）|
| ② 施法擺設定位（旋轉/縮放/位移）| 靜態零件 **可**；ModForge `Position`/`Rotation`/`Scale`(XSCL) **全已支援**；定位行為**需 controller** | 內建泛用 placement-controller `.pex`（同 Tundra，合流 settlements P2）；**旋轉+距離照抄 Tundra，縮放補 `MODE_SCALE`(SetScale)、位移補 `MODE_TRANSLATE`(SetPosition)**；XSCL 不作用於 actor |
| §E 編輯法術組（滴管/範圍吸取/移除）| 滴管+範圍吸取 **ModForge 零改動**；**移除＝`removals[]` 已落地** | 滴管吸 base+rot+scale+成功特效、範圍吸取（SKSE 列舉半徑 refs）→ 都進 `placements[]`；命名插槽 StorageUtil；**匯出 FormID→`<plugin>:0xID` 反解＝採集橋 SKSE**；**移除既有 vanilla ref＝`BuildRemovals`（GetOrAddAsOverride+disable+深埋，✅）**|
| §A 拓印玩家：perk/裝備/法術/技能 → NPC | **可**（`NpcSpec.Perks`/`Outfit`/`Items`）| — |
| §A 拓印玩家：**外貌 facegen** | **✅ 由 PROTEUS 補位** | 路徑 A（採納，clone 穩定可引用）繞過 GAP；路徑 B（ModForge 自建 facegen）降為未來「可散布獨立 NPC」選項 |
| §B marker / 特效 / 標籤 | **可**（XMRK/HAZD/Light/KYWD 全有）| 缺採集橋輸出 `{kind,at,params}` |
| §C export 整場景 | 生成型別 **全可** | 採集橋（同①放大）；**navmesh 即時採集是硬項** |
| §D 身份 → 對話/行為 | **可**（identity 系統 + 對話 INFO + package + vendor 全實機）| 遊戲內只貼 tag；文本可接 #17 AI 生成 |
| 編輯器 UI（遊戲內面板）| **非可生成，但已落地** | [SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)（ImGui）＝前端；消費者 plugin **就是** [`scene-capture-bridge`](../../../sub_projs/scene-capture-bridge/README.md)（`src/UI.cpp`，2026-07-10 接上，軟相依：沒裝框架仍有 F10）。不必另開子專案。⚠️ `sse-imgui` 在 AE 上不能用，見 finding |

**收斂成一句**：整條管線的**生成端 ModForge 幾乎都有**（唯一真 GAP＝外貌 facegen 生成，且可用 PROTEUS 消費繞過）；**真正要蓋的是中間那支 bespoke 消費 SKSE DLL**——採集橋（讀狀態/貼標籤 → JSON）+ 一支泛用 placement-controller `.pex`。同 Tundra/Honed Metal「須附 native/Papyrus controller」判定。

---

## 建議的最小垂直切片（驗證北極星）

照北極星情境切最小可跑：**擺 1 棟房子（喝瓶→定位→落地，②/Tundra controller）+ 拓印玩家成 1 個 NPC 並標 identity=blacksmith（④/§A 路徑 A + §D）+ 放 1 個地圖 marker + 1 個特效錨點（§B）+ 快照整片 → ModForge build 出可造訪的迷你據點（§C）**。先驗「能擺、能拓印、能標註、能匯出、住民有對話」五件事，再擴。**先決調查**：✅ 已解（PROTEUS clone 穩定可引用，走路徑 A）；**先決生成改動**：無（§B/§D 已具備，②的 controller 與 settlements P2 合流一起做）。→ 已展開成 spec，見 [ingame-scene-export-design](../../specs/ingame-scene-export-design.md)。

## 關聯

- **[settlements Phase-2](../../roadmap/mod-survey-gaps/settlements-phase2.md)**：~~共用同一支 controller~~ **（2026-07-10 修正）共用的是設計，不是程式碼**。兩者部署限制不同：本 idea 的編輯器 controller 跑在**作者的編輯 session**，載體是 `SceneCaptureBridge.dll`，可以直接寫 C++；settlements P2 的 `buildables:` controller 跑在**玩家的遊戲**裡、在 ModForge 生的 mod 中，**必須是 `.pex`**（生成的 mod 夾帶不了 DLL）。詳見 [plan](../../plans/scene-capture-bridge.md#m7滴管e-)。
- **[#23 living-adventurers](../living-adventurers.md)**：§A 路徑 A 的「指向既有 ActorRef」哲學同源；拓印出的 NPC 可直接 enroll 進活世界模擬。
- **[#1c 多重身份系統](../followers.md#1c-多重身份--輕量職業系統)**（記憶 [[identity-system-confirmed]]）：§D 身份→對話的既有落地基礎。
- #15（CK 替代視覺編輯器，本 idea 是遊戲內路線）；#19 Godot Worldspace Editor（外部編輯器另一路）；#17（任務節點圖，§D 對話 AI 生成管線）。
- [PROTEUS finding](../../../sub_projs/mod-survey/findings/proteus.md)（§A 核心依據）；[Tundra Defense finding](../../../sub_projs/mod-survey/findings/tundra-defense.md)（②擺放-UX 藍本）；[SKSE Menu Framework 3](../../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)（編輯器 UI 前端）。
