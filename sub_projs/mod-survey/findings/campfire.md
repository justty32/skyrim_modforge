# Campfire — Complete Camping System（求生框架 + in-world 3D 技能樹）

← [survey index](../index.md)｜姊妹文件：[custom-skills-framework/README.md](../custom-skills-framework/README.md)（CSF＝**另一條**自訂技能樹路線）

| 項目 | 值 |
| --- | --- |
| 類型 | **框架型**（survival 框架，被 Frostfall 等 mod 當依賴）+ 內含一套**自訂技能樹引擎** |
| Plugin | `Campfire.esm`（調查版本：1.11SE，內含於 Frostfall Campfire SSE Fix）| 
| 規模 | quests=25 npcs=0 items=112 magic=79 books=43；附 `Campfire.bsa`（155 支 .psc 原始碼隨附）+ native `Campfire.dll`? （無，純 Papyrus + meshes；DLL 在更新版才有，1.11SE 無 DLL，全 Papyrus + SKSE event）|
| 敘事價值 | 無（純機制）；**機制價值：極高** |

> 調查重點＝使用者問的兩件事：**(1) Frostfall 天賦樹**（→ 它是 Campfire 的「Skill System」，見 [frostfall.md](frostfall.md)）、**(2) 那些天賦星點怎麼變成 3D world space 裡的 object**（← 本檔主體）。

---

## 1. 一句話結論

Campfire 的天賦樹**不是 Scaleform/UI 選單**。它在玩家面前的**真實世界座標**裡，用 Papyrus 動態 spawn 出一堆**普通的 in-world ObjectReference**——星點是 NIF activator、連線是 NIF activator、背板是一張 static「art plane」——排成一棵樹的形狀、整體轉向面對玩家；玩家用準心**啟動（OnActivate）**某顆星來點 perk，走遠 480 unit 整棵樹自動 disable+delete。這套引擎透過公開 API `CampUtil.RegisterPerkTree(...)` 開放給任何 mod 掛自己的樹（Frostfall 的「Endurance」就是這樣掛上去的）。

對照組：[CSF（Custom Skills Framework）](../custom-skills-framework/README.md) 走的是 Scaleform 假 perk-skydome 選單（重用原版星座菜單外殼）。**Campfire 與 CSF 是兩條完全不同的自訂技能樹技術路線**——CSF＝改 UI 層、Campfire＝擺世界物件。

---

## 2. 機制全解：星點如何成為 3D 世界物件

### 2.1 物件家族（meshes/campfire/）

| NIF | 角色 | 對應 record |
| --- | --- | --- |
| `_camp_intperkstars01.nif` | **天賦星點**（一顆可點的星） | Activator + `CampPerkNode` script |
| `_camp_intperkline01.nif` | **節點間連線**（點亮後播 `Unlock` 動畫） | Activator（無腳本，靠動畫狀態） |
| `_camp_perkartplane.nif` | 背板「art plane」（深色面板，營造選單氛圍 + 播 `UISkillsGlow` 音效） | Static |
| `_camp_perksystementerexp.nif` | 進入特效 | — |
| 三隻「Bug」(Next/Prev/Exit) activator | 導覽螢火蟲（切換樹／離開） | `_Camp_NextBug`/`_Camp_PrevBug`/`_Camp_ExitBug` |

### 2.2 三層 controller（都 extends `_Camp_PlaceableObjectBase`）

```
CampCampfire（營火本體，玩家對它選「Tend / Skills」）
   │  ShowPerkTree() → 在營火位置 spawn ↓
   ├── CampPerkNodeController          ← 一棵樹一個；持有 12 槽 PerkNode + 12 槽 PerkLine + 1 ArtPlane
   │      持有 PositionRef 標記（PerkNodeXX_PositionRef）＝每顆星相對中心的擺位
   └── _Camp_PerkNavController         ← 導覽；spawn Next/Prev/Exit「bug」、管距離自毀
```

### 2.3 擺位的數學：相對 CenterObject 的偏移（這就是「3D 空間」的關鍵）

`_Camp_PlaceableObjectBase.Initialize()` 的核心序列：

```
RotateOnStartUp()                       ; 自身先轉 Setting_StartUpRotation
self.SetAngle(0,0, GetAngleZ()+GetHeadingAngle(Player)+180)  ; ★整個 controller 轉向面對玩家
PlacementSystem.RequestLock(self)
PlaceObjects()                          ; 子類覆寫：對每個節點呼叫 PlaceObject(...)
PlacementSystem.wait_all()              ; 等所有 async 放置完成
GetResults()                            ; 收 future、EnableNoWait()、接 controller、連線
PlacementSystem.ReleaseLock(self)
```

- **CenterObject = controller 自己的 PositionRef**。每顆星不是寫死世界座標，而是 `PlacementSystem.PlaceObject(self, PerkNodeXX_Activator, PerkNodeXX_PositionRef, ...)`——`PositionRef` 是一組擺在 controller 周圍的**標記 ObjectReference**，記錄「相對中心的 local 偏移 + 角度 + scale」。
- 因為 controller 先 `SetAngle` 轉成面對玩家，**整組 local 偏移就一起旋轉**，所以無論玩家站哪、營火朝哪，樹永遠正面展開在玩家眼前。連線用 `inverted_local_y=true` + `is_propped=true` + 取 PositionRef 的 X/Z 角度貼合兩星之間。
- `PlaceObject` 回傳的是一個 **future 物件**（`_Camp_ObjectFuture`，async 放置佇列），`wait_all()` 後 `GetFuture(x).get_result()` 才拿到真正 spawn 出的 ref。這是 1.11SE 純 Papyrus 時代避免 `PlaceAtMe` 卡頓的並行放置系統（`_Camp_ObjectPlacementThread01..30` + `ThreadManager`，30 條 worker thread）。

### 2.4 互動與生命週期

- **點 perk**（2026-06-21 原始碼覆核更正）：星 = `CampPerkNode extends ObjectReference`。`OnActivate` → `controller.NodeActivated(self)`（`campperknode.psc:46`）。**不是直連 `IncreasePerkRank`**——`NodeActivated`（`campperknodecontrollerbehavior.psc:25-60`）先 gate：可買 iff **起始 node 或下游 child node 已買**（`downstream_node_*.required_perk_rank_global >= 1`，注意是「**下游 child 已買**」不是「parent rank」——Frostfall 樹根在底、`downstream` 指向原點）且 未滿 rank 且 `required_perk_points_available > 0`；通過後彈 Yes/No 確認選單，選 Yes 才 `IncreasePerkRank()`（+1 寫回 rank GLOB、`PlayAnimation("OwnedWild")`、`UpdateLines()` 下游連線播 `Unlock`）+ 點數池 `-1` + `SendEvent_CampfirePerkPurchased()`（`:117-124`）。**spend/gate/確認選單全在 Campfire 自己的 `CampPerkNodeControllerBehavior`，消費端（Frostfall）只負責賺點數（增 `required_perk_points_available` GLOB）。**
- **視覺狀態靠 GLOB 重建**：`AssignController` 時讀 `required_perk_rank_global.GetValueInt()`，>0 就立刻播 `OwnedWild`——所以**已點的 perk 每次開樹都正確顯示亮起**，狀態全存在 GLOB（存檔安全）。
- **連線拓樸**：每個 node 有 `downstream_node_1/2` + `downstream_line_1/2`（指 Activator base form）。`AssignDownstreamNodes()` 用 controller 的 `NodeActMap`/`NodeRefMap` 把 base form 解析成 runtime ref。**樹形是在 esp 裡用屬性連好的**，不是 JSON。
- **自毀**：`_Camp_PerkNavController.CheckConditions()` 每 3 秒檢查 `Player.GetDistance(self) > 480` → `TakeDownPerkTree()` + 全部 `TryToDisableAndDeleteRef`。另有 `OnCellAttach/Detach` 失效偵測 + `FindClosestReferenceOfType` failsafe 回收漏網 ref——因為這些是 temp ref，**絕不能殘留存檔**。

### 2.5 切換多棵樹

Next/Prev「bug」呼叫 `CampCampfire.ShowNextPerkTree()/ShowPrevPerkTree()`——takedown 當前 controller、spawn 下一個。註冊進來的每棵樹是一個 `CampPerkNodeController` Activator，Campfire 維護清單（`_Camp_PerkNodeControllerCount` GLOB）輪播。

---

## 3. 第三方怎麼掛自己的樹（公開 API）

`CampPerkSystemRegister extends ReferenceAlias`（隨 mod 的 quest 出貨）：

```papyrus
; required_node_controller = 你那棵樹的 CampPerkNodeController Activator base form
; mod_name = log 顯示用
Event OnInit() / OnPlayerLoadGame()
    RegisterForModEvent("Campfire_Loaded", "OnCampfireLoaded")
    AttemptRegistration()
Event AttemptRegistration()
    GlobalVariable ver = Game.GetFormFromFile(0x03F1BE,"Campfire.esm") as GlobalVariable  ; CampfireAPIVersion
    if ver.GetValueInt() >= 4
        CampUtil.RegisterPerkTree(required_node_controller, mod_name)   ; ★ 一行掛樹
```

→ **任何 mod，純 esp + 幾支薄 Papyrus，就能在營火選單加一棵 3D 技能樹**：需要 (a) 一個 `CampPerkNodeController` Activator（填 12 槽 PerkNode/Line + PositionRef markers + ArtPlane），(b) N 個 PerkNode Activator（掛 `CampPerkNode` script + 兩個 rank GLOB + 兩個 description Message），(c) N 個 PerkLine Activator，(d) 一個帶 `CampPerkSystemRegister` alias 的 register quest。perk 的**實際效果**另走普通 ability/MGEF（與星點視覺解耦）。

---

## 4. 對 ModForge / roadmap 的意義

**這是 [custom-skill-tree-guide/README.md](../custom-skill-tree-guide/README.md) 之外的第二條自訂技能樹生成路線，且全部落在 ModForge 現有能力域內**（無需 CSF 那種 Scaleform JSON）：

| Campfire 路線零件 | ModForge 現況 |
| --- | --- |
| PerkNode / PerkLine / Controller **Activator** records | ✅ 可生成（普通 ACTI；掛 script 走 VMAD/AttachScripts，與 perk-conditiontabcount 同類已驗證路徑）|
| rank GLOB（`required_perk_rank_global` + `_max`）| ✅ 可生成（簡單 GLOB）|
| perk description **Message** records | ✅ 可生成（MESG）|
| **PositionRef 擺位 markers** + 連線拓樸（node 屬性指向 downstream） | ⚠️ 需要：在某 cell 內擺一組相對 marker（placements）+ ACTI 屬性互指；本質是 **cell ref 佈局 + record 屬性連結**，ModForge 有 placements/cellrefs 基礎，缺「一組固定相對 layout 模板」|
| register quest + `CampPerkSystemRegister` alias（一行 `RegisterPerkTree`）| ✅ 可生成（quest + ReferenceAlias + alias script 屬性）|
| 星/線/背板 **NIF** | 直接重用 Campfire 的（依賴 Campfire.esm 即可引其 form），免自製美術 |

**槓桿點**：相較 CSF（要 native dll 玩家端 + Scaleform JSON + UTF-16 翻譯檔），**Campfire 路線的玩家端依賴只有 Campfire.esm 本身**，產物全是 ESP record + 薄 Papyrus——對 AI-agent 友善的「JSON spec → 技能樹」更貼合。代價：外觀固定（營火旁 3D 樹）、節點上限 12/樹、且綁 survival 情境（要先有營火）。

**建議 roadmap 動作**：把本檔與 [custom-skills-framework/README.md](../custom-skills-framework/README.md) 並列為「自訂技能樹兩條路線」，在 roadmap 標注 Campfire 路線為**低依賴 MVP 候選**（純 record 可生成，只缺 layout 模板生成器）。

---

## 5. 對 Sofia patch 的意義

無直接關係（Sofia 不碰 survival/技能樹）。間接：Campfire 的 **`_Camp_ObjectPlacementThreadManager` async 放置 + future 模式** 是「在玩家面前可靠 spawn 一組臨時物件並保證回收」的成熟範式——若日後 Sofia/follower 要做「召喚一組臨時互動物件」（如行動選單實體化），這套 RequestLock/PlaceObject-future/wait_all/distance-takedown/cell-detach-failsafe 是值得借鏡的健壯骨架。
</content>
</invoke>
