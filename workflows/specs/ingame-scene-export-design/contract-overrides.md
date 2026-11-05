# 遊戲內場景匯出 — 座標 round-trip、合法 ModSpec、既有 ref 的 `overrides[]`

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

### ✅ 座標契約 round-trip（2026-07-10 離線閉環，不開遊戲）

拿 houseCARL 讀到的 vanilla 真值造一份採集橋形狀的 scene.json → `build` → 拆產出 esp 的 REFR `DATA` 子記錄：

```
vanilla plugin   rot.z = 2.2730167 rad   （01605E:Skyrim.esm Temporary[15]）
  ↓ 採集橋 kRadToDeg
scene.json       rot.z = 130.23426°
  ↓ ModForge Deg2Rad
產出 plugin      rot.z = 2.2730169 rad   差 2.3e-7 rad（float32 捨入，可忽略）
```

position 三分量完全一致。`dump` 確認 vanilla cell 是**加法式 override**（`temporary=1`），master 僅 `Skyrim.esm`。

**✅ runtime 端也驗完了（2026-07-10 實機）**：在 `01605E`（The Bannered Mare）`player.placeatme` 後匯出，`position = (48.99, 259.99, 321.48)`，與 SkyLink `get_cell_info` 回報的玩家座標**一字不差**，且與同 cell 的 vanilla ref `(-453.7, -965.8, 67.8)` 同一空間 → **interior 的 `ref.data.location` 就是 cell-local**。座標契約全部條目結案。

### ⚠️ 採集橋輸出必須是合法 ModSpec（2026-07-10 修正）

`scene.json` **就是一份 ModSpec**，而 `ReadOpts`（`Program.cs:145`）**沒設 `UnmappedMemberHandling`** → System.Text.Json 預設**靜默忽略未知鍵**。所以採集橋吐的每一個鍵都必須是真的 ModSpec 成員，否則無聲消失。首編後發現三處不合：

- ❌ `npcRefs[]` 不是 ModSpec 成員（ModSpec 只有一個 `Placements` list）→ 整段被丟掉。**當時的修正：actor 與物件一律進 `placements[]`**（actor base 會讓 ModForge 生 ACHR；XSCL 對 actor 無效故不帶 scale）。本節開頭契約表的 `npcRefs[]` 那列**是概念分段，不是 JSON 鍵名**。**🔴 已被 2026-07-12 拍板推翻**——cell 掃描根本不出 actor（見下「場景匯出不含 NPC」（[captures](captures.md)）），NPC 走 marker/captures。
- ❌ 頂層 `cell` / `worldspace` 不是 ModSpec 成員 → 被丟掉。**歸屬欄位在每一筆 `PlacementSpec` 上**（`Spec.World.cs:6-7`）。
- ❌ 兩者皆空的 placement 會被 `Generator.Build.Placements.cs:48` 以 `cell '' not found in spec — skipped` 丟棄。採集橋現在在解不出 cell/worldspace 時直接中止並 warn。

### ✅ 拍板（2026-07-11）：既有 ref 的 override 形狀＝**B 案，頂層 `overrides[]`**

**問題**（原文）：`scene.json` 只有 `removals[]` 碰既有 ref。玩家**移動/縮放過的 vanilla ref** 一律被 vanilla diff 跳過——吸一面牆擺下去，玩家自然會想把既有的牆對齊挪動。

**定案形狀**（`Spec.Overrides.cs` / `Generator.Build.Overrides.cs`，2026-07-11 全鏈落地）：

```json
"overrides": [
  { "ref": "Skyrim.esm:0x0D1991",
    "position": {"x": 19265.9, "y": -12816.5, "z": -4539.0},
    "rotation": {"x": 0, "y": 0, "z": 90.0},
    "scale": 1.5 }
]
```

position/rotation（度）必填＝新的完整 transform（不是 delta）；`scale` 選填——**省略＝不碰原記錄的 XSCL、1.0＝清掉 XSCL 回引擎預設**（採集橋一律明寫 live scale；actor 不帶 scale）。

**為什麼推翻原「傾向 A」**（當時說「先做 M6 累積實感再拍板」——實感累積完，一致指向 B）：

1. **PlacementSpec 已經長胖**：teleport/lock/ownership/linkedRefs/enableParent/count/persistent… 全部在 override 語境下無意義。A 案要 validate 排除的非法組合面隨 PlacementSpec 每次成長而變大；B 案的新型別只有 4 個欄位，永遠不會長錯東西。
2. **生成路徑本質不同**：AddNew 需要 cell/worldspace 歸屬（`BuildPlacements` 對空 cell 直接丟棄），GetOrAddAsOverride 不需要（resolved context 自帶 parent chain）。A 案得在 BuildPlacements 迴圈前面加特判繞過歸屬檢查——耦合進最熱的一條路徑。
3. **removals 前例**：碰既有 ref 的操作已住頂層段；overrides 是它的兄弟（remove existing / move existing 並列）。DLL 端同構：Eraser→`removals[]`、Overrides 登記→`overrides[]`。
4. **placements 語意已被實機固化**（2026-07-11 Winterhold 驗收）：「placements[] = 玩家新增的動態 ref」這條不變式是 vanilla diff 的根基，混入 override entry 會弄髒它。

與 removals 撞名＝validate 警告（矛盾）；build 讓 removal 後蓋而贏（`Build.cs` 順序：Overrides → Removals）。

#### 「怎麼知道 ref 被移動過」——不能用 diff，維持明示模型（已照此實作）

`TESObjectREFR::GetPosition()` 的定義**就是** `return data.location;`（`TESObjectREFR.h:405`）——引擎回報的是**當前**位置，不是 authored 值，且 havok 會自己移動東西（杯子從桌上滾下來），純 diff 會吐出一堆假的 override。所以與 removals 決策同構走**明示**：只有**經過 numpad 編輯器 commit** 的 authored ref 才進 `Overrides` 登記簿（`src/Overrides.{h,cpp}`，比照 Eraser），匯出時 emit live pose（commit 後物理沉降照實）。登記簿在 RAM：**關遊戲重開後，移動過的 pose 活在存檔裡但不會自動重新登記**——重新編輯一次（numpad 5 → 微調 → 0）即可，MVP 接受此限制（README「持久化與 adopt」表有列）。revert 按鈕回到 first-select baseline（havok 已滾動過的物件，baseline 是滾動後的 pose——不影響匯出，只影響 revert 落點）。

---
