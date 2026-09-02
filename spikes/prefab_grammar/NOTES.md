# prefab_grammar spike — 筆記

## 1. 這是什麼

離線、確定性的 spike：只驗證「prefab 用 connector 對接組成 layout」這套 grammar 概念是否成立與是否可 byte-identical 重現。**不進 ModForge 主管線**、不生 `.esp`、不碰遊戲、不碰 navmesh。怎麼跑看同目錄 [README.md](README.md)；凍結介面看 [../../../../agentctl/handoffs/spike-2026-09-02/CONTRACT.md](../../../../agentctl/handoffs/spike-2026-09-02/CONTRACT.md)。

## 2. grammar 規則

- 四種 prefab kind：`entrance`（0 個 entrance-role connector、≥1 exit-role）、`hall`／`room`（恰 1 entrance-role、≥1 exit-role）、`cap`（恰 1 entrance-role、0 exit-role）。
- connector 是三元組 `(type, position, facing)`，`facing` 是 socket **朝外法向**（0=+X/90=+Y/180=-X/270=-Y，必為 90 倍數）。兩個 connector 能對接（mate）的充要條件：`type` 相等、世界座標相同、世界 facing 相差恰 180°。
- 對接數學：把 prefab 的 entrance connector `c` 接到既有開放 socket `w`：
  ```
  yaw    = (w.facing + 180 - c.facing) mod 360
  origin = w.position - rotate_xy(c.position, yaw)
  ```
  **每次都由 socket 直接算出 yaw，不像 Mundusform 累加 `bearing`**——不會有狀態累積誤差。
- yaw 只允許 0/90/180/270，旋轉矩陣元素只剩 `{-1,0,1}`（`geometry.rotate_xy`），是精確浮點運算、零三角函數漂移——這是「同 seed byte-identical 輸出」的前提。
- bbox 碰撞用三軸 AABB（`geometry.Aabb.overlaps`），貼面共用牆面不算重疊：測試前用 `EPSILON=1e-6` 內縮，允許走廊貼牆放置。
- 決定論三條：① 亂數只能來自 `random.Random(seed)` 實例，檔案內不得有 module-level `random.xxx()` 呼叫；② 任何 dict/set 迭代前先 `sorted()`；③ 輸出一律走 `schema.dump_layout()`（`sort_keys=True, indent=2`，UTF-8、結尾換行）。

## 3. 與 Mundusform 的差異

**借了什麼**（概念層，不搬碼）：connector 型別配對＋bbox 拒絕重疊；主支路／側支路佇列＋死路自動封口。
本 spike 在對接上另加 `type` 與 180° facing 的雙重檢查，支路邏輯由 `generator.py` 重寫。

**丟了什麼**：

| 面向 | Mundusform | 本 spike |
|---|---|---|
| 隨機性 | `srand(time(NULL))`（`Work()`） | 僅 `random.Random(seed)` 實例 |
| 累積誤差 | bearing 累加轉向（`TranslateBlock`） | 每次由 socket 直接算 yaw，無累加 |
| 碰撞維度 | 只測 X/Y 二軸（`Intersects`） | 三軸 AABB，含 Z（可堆疊樓層） |
| bbox 慣例 | min-corner + 寬高（`RotateAroundPivot`） | 中心點 + 全尺寸，對齊 `NavCutSpec` |
| 容器/落地 | 手刻 raw array、global state、runtime `PlaceAtMe` 生怪（`BuildRift`） | dataclass、無 global、離線純函式、輸出 JSON |
| 序列化 | 位置陣列式、無鍵 JSON | 具名欄位 JSON，`dump_layout()` 正規化 |

上表五個 Mundusform 函式的出處（皆已逐行核對）：
`analysis/tool-survey/repos/Mundusform/Undaunted/RiftManager.cpp:210`（`Work()`）、
`analysis/tool-survey/repos/Mundusform/Undaunted/RiftManager.cpp:155`（`TranslateBlock`）、
`analysis/tool-survey/repos/Mundusform/Undaunted/RiftManager.cpp:328`（`BuildRift`）、
`analysis/tool-survey/repos/Mundusform/Undaunted/BoundingBoxs.cpp:21`（`Intersects`）、
`analysis/tool-survey/repos/Mundusform/Undaunted/BlockLibary.cpp:80`（`RotateAroundPivot`）。
判定結論摘自 `analysis/tool-survey/findings/mundusform-borrow-assessment.md` 第 5 節。

## 4. 與 ModForge placement 模型的接點

- `PlacementSpec`（`projects/ModForge/src/ModForge.Core/Spec/Spec.World.cs:64`，欄位 `Base:66`／`EditorId:67`／`Cell:69`／`Worldspace:70`／`Position:72`／`Rotation:73`／`Scale:74`）與本 spike `blocks[].placements[]` 的欄位名（`base`/`editorId`/`position`/`rotation`/`scale`）**刻意一對一**，所以 layout 未來可以直接倒進 `ModSpec.placements[]`，不用改欄位名。
- `Vec3`（`projects/ModForge/src/ModForge.Core/Spec/Spec.World.cs:10`，`X`/`Y`/`Z` 皆 `float`）對應本 spike `[x,y,z]` 三元陣列；`rotation` 單位是**度**，與 `PlacementSpec.Rotation` 一致。
- `NavCutSpec`（`projects/ModForge/src/ModForge.Core/Spec/Spec.NavCuts.cs:55`，`Position:61`＝box **中心**、`Size:62`＝**全尺寸**、`RotationZ:63`＝度）——本 spike 的 bounding box 刻意採用這一套慣例而非 Mundusform 的 min-corner+寬高，理由是不要在同一個 repo 造第二套座標慣例。

再往上一層，整份 layout 對應的容器與落地入口：

- `CellSpec`（`projects/ModForge/src/ModForge.Core/Spec/Spec.World.cs:9`）——整份 layout 概念上對應「一個 interior cell」的內容物。
- `BuildPlacements`（`projects/ModForge/src/ModForge.Core/Build/Generator.Build.Placements.cs:14`）——未來若真的落地，layout 的 `blocks[].placements[]` 會從這個函式的輸入端（`placements` 清單）進主管線。

**還沒接上**：本 spike 不產生 `ModSpec` JSON、layout 沒有 `cell`/`worldspace` 欄位、不碰 navmesh（`NavCutSpec` 只是座標慣例參照，未實際產生 navcut）、也沒有 teleport door 對接（`PlacementSpec.Teleport`）。

## 5. 下一步與沒做到的

**下一步（planning 候選）**：
1. layout → `ModSpec.placements[]`／`CellSpec` 的 emitter（補 `cell` 欄位、走 `BuildPlacements` 驗證）。
2. connector 多型別（門／拱／樓梯）與垂直堆疊（Z 軸對接）。
3. 用真實 vanilla dungeon kit 的 OBND 取代手刻 bbox。
4. navcut box 自動由 block bbox 產生（比照 `PlacementNavCutSpec` 的 auto 規則）。
5. in-game preview（coc 進去看 layout）要不要做，值得單獨評估。

**沒做到的**：
- 未驗證任何真實 vanilla kit 資產，bbox／connector 座標全是手刻假資料。
- bbox 是手刻，不是從 OBND 算出來的。
- 沒有 navmesh，也沒有驗證走廊是否真的可通行。
- 沒有實機驗證（沒開遊戲、沒生 esp）。
- `cap` 放不下時（`max_blocks` 用盡）會留下 open connector，`generator.py` 目前只記錄不補救。
