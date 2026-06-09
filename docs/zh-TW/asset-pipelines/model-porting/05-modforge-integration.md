# 05 — ModForge 整合設計

← [README](README.md) · 上一份：[04-nif-and-collision.md](04-nif-and-collision.md) · 下一份：[06-standalone-runbook.md](06-standalone-runbook.md)

驗證過的手跑管線（[06]）如何折進產生器。設計、非程式碼——但它點名具體檔案、spec 欄位、與要照抄的既有慣例。以當前 `src` layout（`docs/CODE_MAP.infra.md`）與真實 `Spec.Items.cs` / `Assets.cs` 為基礎。

**精確照抄這些既有慣例**（別發明新的）：
- **資產層與記錄層平行。** ModForge 寫 records + 資料夾結構並**shell-out** 外部工具（Blender headless、ck-cmd、Compressonator）——**不**自造 nif/dds 位元組。`Assets.cs` 已寫「does NOT author meshes」。這是 xLODGen/Papyrus 姿態。
- **帶 env-var fallback 的 shell-out：** `Papyrus.cs` 是範本——`null → MODFORGE_* env → default`，在 Wine *或* native 下驅動 exe。mesh/貼圖工具照抄此形狀。
- **資產複製 + MO2 組裝：** `Assets.cs` 複製 `Meshes/Textures/Sounds` 樹；`Package.cs` 攤平 MO2 資料夾。mesh 輸出只是更多 `Meshes/…` + `Textures/…` 讓它們撿。
- **兩段 build：** `Generator.Build.cs` = pass 1（records）→ pass 2（link）。mesh 轉換**與 records 無關**（純資產活），所以是**獨立 CLI step**，非 record builder——如 `compile` 與 `build` 分開。

---

## 1. Spec 設計（additive — 無 breaking change）

僅 additive optional 欄位（CLAUDE.md：新 optional 欄位安全；既有 example 不受影響）。加完後更新 `examples/spec.schema.json` + `sample_spec.json`。

**`modelSource`** — 任何已有 `Model` 欄位的 record 上的選用 sibling 區塊（`StaticSpec`、`FurnitureSpec`、`ActivatorSpec`、`MiscSpec`、武器/防具 `Model`）。它說「從此來源產出 `Model` 指向的 `.nif`」：
```jsonc
"statics": [{
  "editorId": "MyCrate",
  "model": "Meshes/Mine/crate.nif",        // 既有欄位 — Data-relative 目標
  "modelSource": {                           // 新 optional 區塊
    "file": "model-work/src/crate.fbx",     // 來源 mesh（本機，不 commit）
    "sourceType": "gltf",                    // gltf | fbx | obj（帶 [02] transform 規則）
    "collision": "convex",                   // convex | box | none
    "materialProfile": "truepbr",            // truepbr | legacy（[03]）
    "backend": "niftools",                   // niftools | ckcmd | pynifly（[01] §4）
    "textures": {                             // 來源 → slot 映射（[03]）
      "diffuse": "model-work/src/crate_d.png",
      "normal":  "model-work/src/crate_n.png",
      "rmaos":   "model-work/src/crate_orm.png"
    }
  }
}]
```
無 `modelSource` = 今日行為（`.nif` 使用者自備、搭 copy-trees）。有 = build 產出它。

> 欄位衛生（CLAUDE.md）：之後刪/改名欄位需 `grep -r "field" examples/` + 同 commit 更新所有命中。新增免費。

---

## 2. 新 CLI step `importmesh`（與 `compile` / `package` 平行）

住在 `Program.Build.cs`，與 `build`/`validate`/`package`/`compile` 並列。不像語音（`voicelines` 需建好 esp 拿 FormID），mesh 轉換**只需 spec**——所以可在 `build` *之前或之後*跑。每個 `modelSource` 的 pipeline：

```
importmesh <spec.json>
  1. 讀每個帶 modelSource 區塊的 record
  2. 快取檢查：(source mtime + opts hash) 未變且目標 nif 在 → 跳過
  3. backend == niftools/ckcmd：
       shell out blender --background --python convert.py -- <args>（原生）
         · 匯入 file、套 per-sourceType transform（[02]）
         · map 材質 → BSLighting/True-PBR；對每張 .dds shell Compressonator（[03]）
         · 生 convex/box bhk 碰撞（[04] §3）
         · 把 Data-relative 貼圖路徑寫進 BSShaderTextureSet
         · 匯出 NiTriShape .nif
     backend == pynifly（蒙皮）：
       此處不跑 — emit 一行 manifest「reboot to Windows, run pynifly_export.py」（[07]）
  4. 把 .nif + .dds 放進 package 已打包的 Meshes/ … Textures/ 樹
```
步驟 1–4 對靜態可自動化；pynifly 分支是刻意的手動交接（雙系統）。接著 `package` 把輸出掃進 zip。

**為何獨立 step：** 轉換慢、有沉重選用外部依賴（Blender、Wine 工具）、且是純資產活。把它移出 `build` 保持 `build` 快又無依賴；`importmesh` 是 opt-in——同 `compile` vs `build` 理由。

**完整 build 順序：** `importmesh` → `build` → `package`（或 `build` → `importmesh` → `package`；順序無關，因 mesh 不依賴 FormID）。記進 `SPEC-workflow.md`。

---

## 3. 新 core 檔 `Mesh.cs` + `convert.py`

- **`Mesh.cs`**（Core）— shell-out 編排，對映 `Papyrus.cs`：一個 `MeshOptions` class，各後端 exe `null → MODFORGE_* → default`；「tool missing → warn, skip」（絕不硬失敗）。依 `modelSource.backend` 解析後端。維持 ≤300 行（CLAUDE.md）。
- **`convert.py`**（repo 出貨，embed 或像 `.pex` 資源般隨 CLI 出貨）— headless Blender 腳本，做匯入/transform/材質/碰撞/匯出。與 repo 版控；`Mesh.cs` 經 `blender --background --python convert.py -- <json-args>` 呼叫。
- *（無 C# nif writer。）* 不像語音計劃的原生 `WriteFuz`（fuz 小且已驗證），nif 是大型不透明格式——我們**編排 Blender/ck-cmd**，絕不自造。（`nifly` C++ lib 僅在某天想 in-process author 時當 fallback——不建議。）

一個小的**純、可測** helper *可* 住 Core：`MeshPath.Validate(spec)`——確認每個 `Model`/貼圖路徑格式正確且解到打包位置（餵 §6 的 `meshdiag`）。那是 record/字串邏輯，免 Blender 可單測，如 `Generator.SceneFragments.cs`。

---

## 4. 工具設定（env vars、條件式）

對映 `Papyrus.cs`/`PapyrusOptions`：

| Env var | 指向 | 缺則 → |
|---------|------|--------|
| `MODFORGE_BLENDER` | `blender` 執行檔 | 跳過 mesh 轉換、warn（使用者自備 nif 仍可用） |
| `MODFORGE_COMPRESSONATOR` | `compressonatorcli` | 跳過 dds 壓縮、warn（或直通 PNG——非法，故大聲 warn） |
| `MODFORGE_TEXCONV` | `texconv.exe`（Wine） | Compressonator 的替代；未設 = 用 Compressonator |
| `MODFORGE_CKCMD` | `ck-cmd`（可帶 `wine ` 前綴） | 僅 `backend: ckcmd` 時需要 |
| `MODFORGE_PYNIFLY_MANIFEST` | 寫蒙皮交接清單的路徑 | 僅 `backend: pynifly` |

每個缺的工具優雅降到下一個更低能力並 warn，絕不硬失敗——既有的條件式-embed / 條件式-工具姿態。

---

## 5. Package + build-pipeline wiring

- `Assets.cs` 已複製 `Meshes`/`Textures` 樹——確認 glob 涵蓋 `importmesh` 寫的 `Meshes/<sub>/` 與 `Textures/<sub>/`（應該；同使用者自備資產的樹）。
- `Package.cs` 攤平 MO2 組裝已處理 `Meshes/`+`Textures/`；轉換資產搭便車。**無 `.seq` 互動。**
- `StaticSpec.AlternateTextures`（已存在）讓一個 nif 用替換 texture set 重用於不同擺放——變體 prop 免重轉很有用。

---

## 6. 維護鏈落點（落地時，非現在）

依 CLAUDE.md Workflow 1，落地時（這是研究）：
- **程式碼：** `Spec.Items.cs`（+ 一個 `ModelSourceSpec` record）、`Mesh.cs`、`convert.py`、`Program.Build.cs`、`examples/spec.schema.json` + `sample_spec.json`。
- **CODE_MAP：** 把 `Mesh.cs` 列入 `CODE_MAP.infra.md`；`importmesh` 入 CLI 表；`modelSource` cross-ref 進 `CODE_MAP.world.md`（static/placement）與 `CODE_MAP.items-magic.md`（武器/防具模型）。加 Tests 列（`MeshPathTests`）。
- **文檔：** `modelSource` 欄位入 `SPEC-world.md` / `SPEC-items-magic.md`（若長大則開新 `SPEC-assets.md`）；`importmesh` 入 `for_agent_cli.md` + `SPEC-workflow.md`。
- 新 diag **`meshdiag <esp>`**（與 `lightdiag`/`identitydiag` 平行）——從建好 esp 不開遊戲驗 `Model`/貼圖路徑解析。鑑於隱形-on-錯路徑失敗模式，價值高。

---

## 7.「完成」長什麼樣

`modforge importmesh spec.json` 讀每個 `modelSource`、shell `convert.py` 在決定性 Data-relative 路徑產出 `NiTriShape` `.nif` + `.dds`、`package` 打包——設了 `MODFORGE_BLENDER`、其餘（Compressonator/ck-cmd/pynifly）未設時優雅降級。runbook（[06]）就是這 step 自動化內容的 spec；蒙皮 `pynifly` 後端（[07]）是文件化的手動交接，尚未自動化。

---

### 來源
內部慣例讀自 `docs/CODE_MAP.infra.md`、`src/ModForge.Core/Papyrus.cs`、`src/ModForge.Core/Assets.cs`、`src/ModForge.Core/Spec.Items.cs`、`src/ModForge.Cli/ModForge.Cli.csproj`。引擎/格式事實：[01]–[04]。
