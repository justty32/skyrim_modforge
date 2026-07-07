# Pirates of Skyrim - The Northern Cardinal（海盜 quest 大作 + 「船＝傳送樞紐」自建系統）

← [survey index](../index.md)

| 項目 | 值 |
| --- | --- |
| 類型 | **內容型 quest 大作**（雙海盜劇情線 + 自訂 worldspace + 派系 + NPC）＋螺栓上去的**「船隻旅行樞紐」機制**（Papyrus + SkyUI MCM，**無 DLL**） |
| Plugin | `NorthernCardinal.esp`（v1.3.1，FIXED 版）；master = Skyrim.esm + Dawnguard/Dragonborn（部分 record 有 DLC/Falskaar/Wyrmstooth 相容分支） |
| 規模 | quests=11 npcs=111 items=61 magic=15 books=12 dialogue_lines=131 loc=64；**無 BSA（全 loose）**、**無 SKSE DLL**、~135 支 `.pex` 且**附完整 `.psc` source**、380 NIF |
| 依賴 | SkyUI（MCM `SKI_ConfigBase`）；純 Papyrus + record，無 native code |
| 敘事價值 | **中-高**（兩條完整海盜任務線 + 船長 progression + crew 招募 + 自訂海域/島嶼世界） |

## 是什麼

「你成為 Northern Cardinal 號船長」的海盜生活 mod。主線 `0ShipQuest`（清剿 Frostreef 海盜、奪回失竊船隻、當上船長）＋一堆船務支線（升級船、給 crew 跑腿、Sea Shanty），並**內嵌第二條獨立海盜線** `POPPirateQuest`（"Protect the Scrolls"：加入 Captain Morgan 的 Marie Elena 號、Kaloi 島、Elder Scroll——是另一位作者 "Pirates of Skyrim POP" 的劇情被併進來）。有自訂護甲（Seadog / Explorer / 海盜頭巾）、旗幟、figurehead 等裝飾道具。

**自訂 worldspace 兩個**：`aaSeaOfGhosts`（03DB46，開放海域，跑海戰/沈船/寶藏）與 `0ShipFrostreefWorld`（097EA0，海盜要塞島）。其餘港口是**vanilla cell override**（Solitude/Dawnstar/Windhelm 碼頭 + Raven Rock/Volkihar 靠 DLC 分支）。

## 關鍵架構

### 1. 船隻＝「多實例靜態船 + Enable/Disable 傳送樞紐」（**不是**會動的船）

對照 [animated-vehicles.md](animated-vehicles.md)：vanilla 動態船是「NIF 自帶航行動畫的裝飾」，這裡**完全不同**——船是**純靜態置放**，「航行」是一次淡入淡出傳送：

- 每個港口都**預先擺一整艘船的副本**。核心資料結構 `ShipList` ＝ **FormList of FormList**，用地點 index 定址：`ShipList[loc]` 是該港的 FormList，`GetAt(0)`=要 Enable 的主船 ref、`GetAt(1)`=玩家落點 marker、`GetAt(3)`=旗幟 FormList、`GetAt(4)`=figurehead FormList。
- `aaashiptravelquest.ShipTravel(pre, new)`：`FadeOut()` → `EnableNewShip(new)`（Enable 目的港的船＋按 `BannerIndex`/`FigureheadIndex` global 挑對應裝飾 ref）→ `Player.MoveTo(ShipList[new].GetAt(1))` → `DisablePreShip(pre)`（Disable 舊港的船）→ `MoveCrew()` → `FadeIn()`。
- 裝飾更換（`aaaShipModel`：旗幟/figurehead/床）＝在該港 FormList 內 **Disable 舊 index ref / Enable 新 index ref**，零 record 生成、純開關預置物。
- 觸發點是船上 activator 掛 `aaashiptravel`（甲板）/`aaashiptravelint`（船內）/`aaashipteleport`（爬繩 MoveTo marker）/`aaashipdoorteleport`（艙門 FastTravel，含 CarryWeight 暫時 +load 以免超重卡住的 trick）。

### 2. Sea of Ghosts（index 7）特例＝Enable/Disable 隨機重擲的「海戰/海域事件」

出海到 `aaSeaOfGhosts` 世界時：`SeaOfGhostsShip.Enable()`（獨立一艘置於海域的船，玩家 MoveTo `WheelAtSeaOfGhosts` 舵盤）＋ `aaDisableEnableOceanFeatures.DisableEnable()` 把 ~13+ 個 `XMarker` 上**預置的沈船/寶藏/商船/鯨魚/划艇**按 `Utility.RandomInt(1,10)` 逐一 Enable/Disable **重擲**，`aaDisableNavalBattles` 管一整批預置的海盜/reaver/battlemage/trader actor（Imperial/Thalmor/Stormcloak captain cabin cell）的開關。**海戰＝pre-placed actor 群的 Enable/Disable 狀態機 + 隨機骰**，非 runtime 程序生成。鬼船是 `aaGhostShipFX`：`OnInit` 給 self 播 `EffectShader`。

### 3. Crew 招募＝alias-bank + global 計數 + morale gate

`aaacrewquest`：9 格 `CrewAlias[]`（Hearthfire 模式縮到 index 4-7），把「帶上船的 follower」`ForceRefTo` 進空 alias，`CrewCount` global 累加；解僱時陣列往前壓縮。`ShipTravel` 每次 `MoveCrew()` 把 crew 搬到新船。`aaCrewMorale` global＋`ShipCrewCount` 當**出海 gate**（morale<1 或人數 <`CrewForSea` 不准出海），沿岸移動扣 0.5、出海扣 1——輕量資源管理層，全走 GlobalVariable + alias，**0 DLL、0 外部儲存**（連 PapyrusUtil 都沒用）。

### 4. UI＝SkyUI MCM（`SKI_ConfigBase`），非原生選單

`aaashipconfig extends SKI_ConfigBase`：MCM 內選港口（`AddTextOption` 點一下 = 呼叫 travel）＋旗幟/figurehead 設定＋`LoadCustomContent("0ship/mcm.dds")` 自訂 logo。船上互動則走一堆 `Message.Show()` message-box 多按鈕選單（旅行目的地、裝飾選單）。

## 結論

對 ModForge 的可生成性標記：

- **可生成（今天就能）**：整個「船＝傳送樞紐」骨架其實**全是已 landed 的 record 能力域**——ACTI + script-attach（`aaashiptravel` 等 `ObjectReference` 腳本，`scriptAttach`/`ScriptAttachSpec.Source` 已驗證可編譯 `.psc` 進 VMAD）、FormList（**FormList-of-FormList 巢狀**，FLST 工廠已可生，見 [flst-factory.md](flst-factory.md)）、GlobalVariable 狀態、QUST + 9-alias crew bank（`ForceRefTo` alias fill 已支援）、自訂/override CELL & WRLD、MESG 多按鈕選單、SkyUI MCM（`config.json` 生成 + `SKI_ConfigBase`，見 [mcm-helper.md](mcm-helper.md)）。**ModForge 是 packager，controller 演算法（travel/crew/海域重擲）＝隨附 `.psc`**——與 Tundra/Honed Metal/Real Estate 同類。
- **純參考 pattern（高價值）**：**「Enable/Disable 多實例預置物做偽移動/偽程序生成」**是本 mod 的核心手法，值得記進 [runtime-selector-patterns.md](runtime-selector-patterns.md)——① 「一物多港副本 + FormList 定址 + 傳送時切換 Enable」＝可移動據點（船/馬車/浮空堡）的**零-NIF-動畫**做法，比 linkedRef 節點鏈更簡單；② 「XMarker 上預置事件群 + `RandomInt` 逐一 Enable/Disable」＝**輕量隨機遭遇**（比 SM/navmesh spawn 便宜，但事件是有限預置池，非真程序生成）。
- **需新支援（缺口）**：無**新**硬缺口——它踩到的都是已知缺口。①「船隻旅行/裝飾/crew」若要做成 spec 便利層，缺一個 `travelHub:` / `enableDisableStateMachine:` macro（把「N 個地點 × 預置物 + FormList 定址 + 切換 fragment」宏展開），但底層零件全在；② 多按鈕 `MessageSpec buttons:[]`（`Spec.Items.cs:42` 缺欄，與 Real Estate/Tundra 同一缺口）本 mod 也大量用到，再加一票。

對 Sofia（follower 子專案）：基本無關；唯一交集是 crew 系統把 follower `ForceRefTo` 進 alias 當船員，Sofia 之類自管 follower 可能與 9-格 crew alias 搶 follower，屬相容性註記而非借鏡。
