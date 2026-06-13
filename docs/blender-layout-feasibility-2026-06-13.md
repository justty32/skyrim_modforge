# Blender 場景擺設 → Skyrim placement 可行性調查（2026-06-13）

> 調查目標：在 Blender 中用 Skyrim 自家的 NIF 模型擺設場景佈局，再把這份佈局轉成
> ModForge 的 `placements[]` JSON（重用已 in-game 確認的放置管線），最後 `build` 成 ESP。
> 本文純文獻 / 設計可行性研究，**沒有載入任何 Skyrim 檔案，無記憶風險**。

---

## 1. 結論一句話

**可行，而且偏簡單** —— 因為 ModForge 已經把困難的一半（JSON → ESP placement）做完並 in-game 驗證過，
缺的只是一個**純 Blender 端的 Python 匯出腳本**（幾十行），把選取物件的 transform 轉成
`placements[]` JSON。**ModForge 本體不需要新功能**（匯出腳本直接吐合法 spec JSON，走既有 `build`）。
唯二真正的硬點是 **(a) 座標/旋轉轉換的正確性**（#1 風險）與 **(b) 每個 Blender 物件如何對應到正確的
Skyrim base record**（asset identity）。兩者都有乾淨的工程解法，但都需要一次實機校正。

---

## 2. 建議管線（recommended pipeline）

```
┌─ Blender 端（人工 + 一支腳本）────────────────────────┐
│ 1. PyNifly 匯入 vanilla 靜態 NIF（meshes\…\*.nif）        │
│ 2. 用 Blender 視覺化擺設：移動/旋轉/縮放物件             │
│ 3. 給每個物件標 base 身份（命名慣例 or custom property） │
│ 4. 跑匯出腳本 → 走訪選取物件 → 吐 placements[] JSON      │
└──────────────────────────────────────────────────────┘
            │  placements[].json（或整份 spec 的一段）
            ▼
┌─ ModForge 端（既有、已驗證、零改動）──────────────────┐
│ 5. 把 placements[] 併進一份 spec.json                    │
│ 6. dotnet run --project src/ModForge.Cli -- build spec   │
│      → ESP（Generator.Build.Placements.cs 既有管線）     │
│ 7. validate / package / 進遊戲                           │
└──────────────────────────────────────────────────────┘
```

**為什麼瞄準 `placements[]` JSON 而不是讓 Blender 直接寫 ESP**：

- ModForge 的 `Generator.Build.Placements.cs` 已處理室內 cell / 室外 worldspace / vanilla-override
  三種放置、persistent flag、cell 錨定、grid 計算（`floor(x/4096)`）—— 這些 in-game 驗證過
  （`SPEC-world.md § cells & placements`）。從 Blender 直接寫 ESP 等於重造這一切並重新踩坑。
- `placements[]` 是純資料、可人工檢視 / diff / 版控，比二進位 ESP 友善。
- 匯出腳本只負責「transform + 身份 → JSON」這件純函式工作，沒有 Mutagen / record 知識負擔。

**meshes 本身不用重新匯出**：佈局只需要「每個物件用哪個 vanilla asset」+「它的 transform」。
ModForge 用 `base: "<master>:0xFORMID"` 引用 vanilla STAT，遊戲自己去載對應的 NIF（含碰撞）。
所以 PyNifly 在這條管線裡**只當匯入/視覺化工具**，匯出走我們自己的腳本，不碰 PyNifly 的 NIF 匯出。

---

## 3. Blender → ModForge 的精確 transform 轉換（含所有坑）

### 3.1 單位（units）—— 最簡單的一項

- **1 Blender unit == 1 Skyrim game unit**（社群共識；128 units ≈ 6 英尺）。
- 前提：Blender 的 **Unit Scale 設成 1.0**、場景以「game unit」為單位來操作（不要用 Blender
  預設的「公尺」心智模型去想尺寸）。PyNifly 匯入 vanilla NIF 時，物件的 mesh 尺寸本來就是 game unit，
  所以只要不去動 Unit Scale，匯入的 vanilla 靜態尺寸天生正確。
- 結論：**position 不需任何縮放**——Blender `object.location.x/y/z` 直接就是
  ModForge `position.x/y/z`（但見 3.3 的 handedness 與 origin 注意事項）。

### 3.2 旋轉（rotation）—— #1 坑

ModForge `placements[].rotation` 是 **degrees**（`SPEC-world.md`：`// rotation in degrees`），
底層交給 Mutagen 寫進 REFR 的 DATA rotation（引擎內部以 **radians** 存）。Blender 物件的
`rotation_euler` 是 **radians**。所以最少要做 **radians → degrees**：`deg = rad * 180 / π`。

但「光除以 π 再乘 180」**不夠**，有三個方向性 / 順序的坑：

1. **方向相反（sign flip）**：NIF / Bethesda 的旋轉與 Blender 的旋轉**方向相反**。文獻給的換算式
   （NIF 角度 → Blender ZYX Euler 度數）是
   `degrees = -360 * value / (2π * 1000)`，那個**負號**就是「Blender 旋轉是反方向」。
   反推（Blender → 遊戲）時，**很可能需要對各軸取負**。這必須**實機校正**：先放一個明顯不對稱的
   vanilla 物件（如一張椅子或門），在 Blender 轉 +30° 繞 Z，build 進遊戲看它是順時針還逆時針，
   據此鎖定每軸的符號。

2. **Euler 順序（rotation order）**：Bethesda REFR 旋轉是固定軸序套用。文獻指出 NIF 視角下
   對應 Blender 的 **ZYX** Euler 模式（即 X 先、Y 次、Z 最後套用的慣例）。
   **務必在 Blender 把要匯出的物件 `rotation_mode` 設成相容的 Euler 模式**（建議 `'XYZ'` 或 `'ZYX'`，
   兩者擇一並在校正時確認），**不要用 Quaternion**。若用 Quaternion，匯出腳本要先
   `obj.rotation_quaternion.to_euler('XYZ')` 再取度數，否則順序對不上會看起來「轉錯軸」。

3. **角度繞回**：度數可正規化到 `[0,360)` 或 `(-180,180]`，引擎都吃；非硬性，但建議正規化避免
   出現 `720°` 這種 diff 噪音。

**保守實作建議**：匯出腳本對每個物件先 `m = obj.matrix_world`，分解出 location / 一個固定 Euler 順序
的角度 / scale；旋轉先輸出 `rad→deg` 原值，**留一組 per-axis 正負號常數（預設全 +1）**，第一次實機校正後
把正確的符號填死。把校正過程寫進腳本註解，避免下次重猜。

### 3.3 座標系 / handedness —— 多半不用動，但要驗

- **兩邊都是 Z-up、右手系**：Skyrim 與 Blender 同為 Z-up、右手座標系。這是好消息——
  position 的 X/Y/Z **預期不用交換軸、也不用翻 Y**（不像 Z-up↔Y-up 引擎那種要 swap）。
- **要驗的是 origin（原點對齊）**：
  - **室內 cell**：`position` 是 cell 局部座標。Blender 場景的原點 `(0,0,0)` 要對應到你心中
    cell 的哪個點，要自己定（建議匯出時讓使用者指定一個「cell 原點物件」或直接用 Blender world origin）。
  - **室外 worldspace**：`position` 是**世界座標**，ModForge 用 `floor(x/4096),floor(y/4096)`
    找/建外部 cell。Blender 端最好直接用世界座標擺放（數字會很大，如 `22528`），或匯出時加一個
    使用者給的 offset 把 Blender 局部座標平移到世界座標。
- **scale**：ModForge `placements[].scale`（optional）對應 Blender 的 `obj.scale`。
  **限制**：Skyrim REFR 只有**單一均勻 scale**（一個 float），Blender 可非均勻 `(sx,sy,sz)`。
  匯出腳本應**檢查三軸是否相等**，不等就 warn（取平均或取 X，並提示使用者該物件無法忠實重現）。
  另外 PyNifly 匯入 vanilla 通常 scale=1，多數場景擺設不會動 scale，這坑次要。

### 3.4 轉換對照表（一個物件）

| Blender（`bpy` 物件屬性） | 轉換 | ModForge placement 欄位 |
|---|---|---|
| `obj.location.x` | ×1（必要時 + 世界 offset） | `position.x` |
| `obj.location.y` | ×1 | `position.y` |
| `obj.location.z` | ×1 | `position.z` |
| `obj.rotation_euler.x`（rad） | `* 180/π`，再乘 per-axis 符號常數 | `rotation.x`（deg） |
| `obj.rotation_euler.y`（rad） | 同上 | `rotation.y`（deg） |
| `obj.rotation_euler.z`（rad） | 同上 | `rotation.z`（deg） |
| `obj.scale`（若三軸相等） | 取單值 | `scale`（optional） |
| 物件身份（見 §4） | 命名/property → 查表 | `base` |

> **唯二必須實機校正的數**：旋轉的 (a) 三軸符號、(b) Euler 順序。其餘（單位、position、handedness）
> 理論上 1:1，但第一次也要拿一個**已知 vanilla 擺放**（從 CK 或 xEdit 抄一個現成 REFR 的 pos/rot）
> 反向比對來鎖定，最穩。

---

## 4. Asset identity 慣例（哪個 Blender 物件 = 哪個 base record）

這是**第二個硬點**：Blender 物件知道自己的幾何與檔案路徑，但**不知道**對應哪個 Skyrim base record。
同一個 NIF 可能被多個 STAT 引用，所以「NIF 路徑 → base record」不是唯一映射。三個層次的解法：

### 4.1 推薦：在 Blender 物件上掛 custom property `mf_base`

最穩、最明確：每個物件存一個自訂屬性，值就是 ModForge `base` 字串（in-spec editorId 或
`<master>:0xFORMID`）：

```python
obj["mf_base"] = "Skyrim.esm:0x000C3D2A"   # 直接就是 ModForge 的 base ref
```

匯出腳本讀 `obj.get("mf_base")`，缺的就 skip + warn。優點：**零歧義**，一個 NIF 對應多個 STAT 也能精確區分。
缺點：要先建立「想用哪些 vanilla 靜態」的清單並貼 property（可寫一個小 helper panel 批次貼）。

### 4.2 次選：命名慣例（object/collection 名 = base）

把物件命名成 base ref（Blender 物件名可含特殊字元，但 `:` / `0x` 沒問題）：

```
物件名 "Skyrim.esm:0x000C3D2A"  →  base = "Skyrim.esm:0x000C3D2A"
物件名 "Skyrim.esm:0x000C3D2A.001"（Blender 自動加的 .001 重名後綴）→ 砍掉 .NNN 後綴
```

匯出腳本用 regex 把 Blender 的 `.001/.002` 後綴剝掉即可。優點：不用額外 property、肉眼可讀。
缺點：Blender 名稱長度上限、`.NNN` 後綴處理、改名易壞。**建議：命名放 EditorID（人類可讀），
另用一份 sidecar 映射檔把 EditorID → `<master>:0xFORMID`**（見 4.3）。

### 4.3 sidecar 映射檔（NIF 路徑 / EditorID → base ref）

無論用 4.1 或 4.2，準備一份 JSON 對照表很值得，因為它能把「PyNifly 匯入時知道的 NIF 檔案路徑」
或「人類好記的 EditorID」翻成 ModForge 要的 `<master>:0xFORMID`：

```jsonc
{
  "WRTorchWall01": "Skyrim.esm:0x00010D96",
  "ChairWood01":   "Skyrim.esm:0x000C3D2A",
  "meshes\\clutter\\common\\chairwood01.nif": "Skyrim.esm:0x000C3D2A"
}
```

這份表可以用 **ModForge 既有的 `find <Skyrim.esm> <name> Static`** 離線查 FormID 來半自動生成
（`find` 已存在，見 `CODE_MAP.infra.md` / `SPEC-world.md` 多處引用）。
甚至可未來寫一個 ModForge 子指令 `staticmap <Skyrim.esm>` 一次吐「EditorID → FormID + NIF 路徑」全表，
讓 Blender 端 PyNifly 匯入 + 貼 property 完全自動化（**選配增強，非必須**）。

> **務實建議**：先用 4.1（custom property = 直接 base ref），最少前置、零歧義，跑通再說。

---

## 5. 匯出腳本草圖（Blender 端 Python，~可行性層級）

**輸入**：當前 Blender 場景中**選取的物件**、一個目標 `cell`（室內 editorId/vanilla ref）**或**
`worldspace`（室外 ref）、可選的世界座標 offset、per-axis 旋轉符號常數（校正後填）。

**輸出**：一份 JSON，內容是一個 `placements` 陣列（可整段貼進 spec，或當獨立檔再由 spec `$ref` 引入——
ModForge 的 `$ref`/`$env` 解析層支援「另一檔 / 檔#pointer」，見 `SPEC-refs.md`，所以匯出檔可獨立存在）。

```python
import bpy, math, json

# --- 校正後填這三個（見 §3.2）；先全 +1 跑一次再修 ---
SIGN = (1.0, 1.0, 1.0)              # rotation per-axis 符號
TARGET = {"cell": "MF_TestRoom"}    # 或 {"worldspace": "Skyrim.esm:0x00003C"}
OFFSET = (0.0, 0.0, 0.0)            # 室外世界座標平移；室內通常 0

def base_of(obj):
    b = obj.get("mf_base")          # §4.1 首選
    if b: return b
    # 退化：用物件名剝掉 .NNN 後綴（§4.2）
    import re
    return re.sub(r"\.\d+$", "", obj.name)

placements = []
for obj in bpy.context.selected_objects:
    base = base_of(obj)
    if not base:
        print(f"[skip] {obj.name}: 無 base 身份"); continue
    loc = obj.matrix_world.to_translation()
    eul = obj.matrix_world.to_euler('XYZ')     # 固定順序；§3.2 校正
    scl = obj.matrix_world.to_scale()
    p = {
        "base": base,
        **TARGET,
        "position": {"x": loc.x+OFFSET[0], "y": loc.y+OFFSET[1], "z": loc.z+OFFSET[2]},
        "rotation": {
            "x": math.degrees(eul.x)*SIGN[0],
            "y": math.degrees(eul.y)*SIGN[1],
            "z": math.degrees(eul.z)*SIGN[2],
        },
    }
    if abs(scl.x-1) > 1e-3 or abs(scl.y-1) > 1e-3 or abs(scl.z-1) > 1e-3:
        if abs(scl.x-scl.y) > 1e-3 or abs(scl.y-scl.z) > 1e-3:
            print(f"[warn] {obj.name}: 非均勻 scale，Skyrim 只支援均勻 scale，取 X")
        p["scale"] = round(scl.x, 4)
    placements.append(p)

print(json.dumps({"placements": placements}, indent=2, ensure_ascii=False))
```

**可行性評估**：以上是一支「走訪選取物件 → 吐 JSON」的純資料腳本，沒有任何 Mutagen / NIF 二進位知識，
Blender Python API（`bpy`, `matrix_world`, `to_euler`）都是穩定老 API。**實作工作量極小（半天等級）**，
**唯一耗時的是 §3.2 旋轉符號 / 順序的一次性實機校正**。`matrix_world` 已含父子層級 / Apply 過的 transform，
比直接讀 `obj.location/rotation_euler` 更穩（會把 parenting、delta transform 都算進去）。

---

## 6. 先行研究（prior art）

| 工具 / 專案 | 方向 | 與本管線的關係 |
|---|---|---|
| **PyNifly**（BadDogSkyrim）| NIF ↔ Blender（mesh）| **本管線匯入端的首選**。Blender 4.4+，**支援 SE NIF**、簡單碰撞、shader。能直接匯入 vanilla 靜態來擺設。NIF 匯出在本管線**用不到**（我們只要 transform，不重出 mesh）。|
| **NifCity**（Nexus 149772）| 批次 NIF → Blender | PyNifly/Niftools 的批次匯入器，能一次匯入整堆 vanilla NIF，加速「把要用的 asset 都拉進場景」。|
| **F4RefToBlender**（ElectronicsArchiver）| **CK → Blender**（refs）| **最接近的先行概念**：把遊戲一個 cell 的**參考擺放**（含 editorID + 內部檔案路徑的 object data table）匯進 Blender，重建場景。**證明「ref placement ↔ Blender」這條橋是成立的**——但它是**反方向且單向**（只進不出），正好是本管線缺的「Blender → placement」的鏡像。它的 object data table（editorID ↔ NIF 路徑）正是 §4.3 sidecar 映射的現成範式。|
| **Dropper for Blender**（jhocking, itch.io）| Blender → JSON/XML | 通用「Blender 關卡編輯器」：擺物件 → 吐 positions/rotations/custom properties 成易解析的 JSON/XML。**證明 §5 的匯出腳本路線是業界常規做法**，非新發明。|
| **xEdit「move references into another worldspace」script** | ESP 內 ref 搬移 | 與 Blender 無關，但顯示 placement 批次操作通常落在 xEdit 腳本層；ModForge 等於把這層搬到 spec JSON。|

**結論**：**沒有**現成、成熟的「Blender → Skyrim ESP placement」工具。最接近的 F4RefToBlender 是反方向。
所以本管線（Blender 擺設 → JSON → ModForge build）填的是一個**真實存在的空白**，而且兩端的子能力
（PyNifly 匯入、JSON 匯出腳本、ModForge build）都已是成熟現貨，只差中間那層薄薄的轉換約定。

**Sources**：
- [Arcane University: PyNifly (Beyond Skyrim wiki)](https://wiki.beyondskyrim.org/wiki/Arcane_University:PyNifly_for_Skyrim)
- [BadDogSkyrim/PyNifly (GitHub)](https://github.com/BadDogSkyrim/PyNifly)
- [NifCity batch importer (Nexus 149772)](https://www.nexusmods.com/skyrimspecialedition/mods/149772)
- [Creation Kit Wiki: Unit](https://ck.uesp.net/wiki/Unit)
- [Arcane University: Blender 2.7x Export — units/scale/forward axis](https://wiki.beyondskyrim.org/wiki/Arcane_University:Blender_2.7x_Export)
- [Arcane University: Nifskope Weapons Setup — NIF rotation = radians*1000, Blender 反方向, ZYX](https://wiki.beyondskyrim.org/wiki/Arcane_University:Nifskope_Weapons_Setup)
- [F4RefToBlender (GitHub)](https://github.com/ElectronicsArchiver/F4RefToBlender)
- [Dropper for Blender (itch.io)](https://jhocking.itch.io/dropper-for-blender)

---

## 7. 風險與 gap，以及 ModForge 是否需要新功能

### 7.1 風險（依嚴重度）

1. **【#1 風險】座標 / 旋轉轉換正確性**（§3.2/§3.3）。單位 1:1、同為 Z-up 右手系是大利多，
   但**旋轉的三軸符號 + Euler 順序**必須靠一次實機校正鎖定，否則物件會「位置對、角度歪」。
   緩解：用一個已知 vanilla REFR（從 xEdit/CK 抄 pos+rot）做來回比對，把校正常數寫死進腳本註解。
2. **Asset identity（base record 對應）**（§4）。同一 NIF 可被多 STAT 引用，純靠 NIF 路徑會歧義。
   緩解：custom property `mf_base` 直接存 base ref（零歧義），或 sidecar 映射表用既有 `find` 半自動生成。
3. **Havok / 碰撞**：placements **只設 transform，碰撞來自 base NIF**。**重用 vanilla 靜態 → 碰撞免費正確**
   （遊戲載原 NIF 的 collision）。**只有自訂新 mesh 才需要自己做 collision**——但那超出本管線範圍
   （本管線刻意只擺 vanilla asset）。所以對「用 vanilla 模型擺場景」這個目標，碰撞**不是問題**。
4. **室內 vs 室外目標**：室內 `position` 是 cell 局部、室外是世界座標且要 grid 化（ModForge 既有處理）。
   匯出腳本只要讓使用者選 `cell` 或 `worldspace` 二擇一、室外加世界 offset 即可，無新風險。
5. **新建空 cell 會讓物件墜落**：與本管線無關但提醒——新 in-spec cell 無地板/無光時 NPC/物件會掉進虛空
   （`SPEC-world.md` 已記）。擺進 vanilla 室內，或先放地板 static + 設 cell `template`。
6. **scale 非均勻**：Skyrim 只支援均勻 scale；匯出腳本 warn 即可（§3.3）。

### 7.2 ModForge 是否需要新功能？

**不需要新功能就能跑通。** 匯出腳本直接吐合法的 `placements[]` JSON，走**完全既有、已 in-game 驗證**的
`build` 管線（`Generator.Build.Placements.cs`）。`$ref` 解析層（`SPEC-refs.md`）還讓匯出的 placement 檔
能當獨立檔被 spec `$ref` 進來，連手動複製貼上都省。

**可選的便利增強（非必須，價值由高到低）**：

- **(選配 A) `staticmap <Skyrim.esm>` 子指令** —— 離線吐「STAT EditorID → FormID → NIF 路徑」全表，
  餵給 Blender 端做 §4 的 asset-identity 自動貼標。**這是最有價值的一個小增強**，因為它把 §4 的硬點
  自動化（複用既有 Mutagen overlay + `find` 的邏輯）。
- **(選配 B) 一個薄的 `import-layout` 慣例 / 子指令** —— 若想把「匯出檔 → 併進 spec」這步也自動化。
  但因為匯出腳本已能直接產 spec-相容 JSON、且 `$ref` 能引入，**這層多半多餘**，不建議優先做。

**判定**：先做**純 Blender 端腳本 + custom-property 身份**，零 ModForge 改動跑通整條管線並完成旋轉校正；
之後若覺得貼 base 太煩，再加 **(選配 A) `staticmap`** 自動化身份這一塊。`import-layout` 子指令多半不需要。

---

## 附：與既有 ModForge 知識的銜接

- 放置語意 / 室內室外 / vanilla-override / grid：`docs/SPEC-world.md § cells & placements`、
  `docs/CODE_MAP.world.md § Placements`（`Generator.Build.Placements.cs` / `Generator.Build.ExteriorCells.cs`）。
- `$ref`/`$env` 讓匯出檔可獨立：`docs/SPEC-refs.md`。
- 查 vanilla STAT FormID：既有 `find <Skyrim.esm> <name> Static`（多處引用）。
- 旋轉是 degrees：`SPEC-world.md` placement 範例 `// rotation in degrees`。
