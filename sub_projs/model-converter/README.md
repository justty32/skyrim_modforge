# Model Converter — 模型格式互轉工具

← [sub_projs 索引](../README.md)

**一句話**：以 Skyrim **`.nif`（含 `.dds` 紋理）** 為中心，做與 **Godot 可用格式（glTF）** 及 **各種常用模型格式（FBX / OBJ / glTF）** 的**雙向**互轉工具。定位是 ModForge 生態的**基石工具**——不整合進 ModForge，靠協議/CLI 被消費。

**為什麼開這個 sub_proj**：兩個消費需求撞在一起，且現有家底都是半套——
- **worldspace 物件編輯**（[godot-worldspace-editor](../godot-worldspace-editor/README.md)）要把 vanilla `.nif` → glTF 丟進 Godot 當視覺代理（**反向**，純預覽）。
- **model-porting**（[workflows/idea/asset-pipelines/model-porting](../../workflows/idea/asset-pipelines/model-porting/README.md)）已把**正向**（外部 FBX/OBJ/glTF → `.nif`）規劃得很深，但**只有正向**。
- 兩者其實是同一把工具的兩個方向。與其各做各的，不如收斂成一個轉換器，正向沿用 model-porting 的決策、反向補上。

---

## 關係定位（不重造輪子）

| 既有資產 | 方向 | 角色 |
|---|---|---|
| [model-porting/](../../workflows/idea/asset-pipelines/model-porting/README.md)（01–10） | **正向** 外部→nif | 正向 deep-dive 的真相在那裡；本工具正向部分**沿用**其工具決策（NifTools 靜態 / PyNifly 蒙皮 / Compressonator dds），不複製內容 |
| [gemini-research/.../nif-gltf-conversion.md](../gemini-research/worldspace-editor/nif-gltf-conversion.md) | **反向** nif→glTF | 反向工具調查的**唯一**現有來源（已帶幻覺更正 banner）；結論濃縮進下方工具表，原稿留存 |
| [godot-worldspace-editor](../godot-worldspace-editor/README.md) | 消費者 | nif→glTF 代理是它物件編輯的前置依賴 |
| ModForge `package`（`StaticSpec.Model`） | 消費者 | 正向產出的 `.nif`+`.dds` 由它打包進 Meshes/Textures 樹 |

---

## 工具盤點（2026-06-17，Explore agent 核對 repo 既有 finding）

> ⚠️ **幻覺黑名單（勿規劃）**：`amPerl/nif`（Rust `nif2gltf` CLI）**不存在**；`SkyMeshGLTF` 來源存疑。兩者 Gemini 捏造，[nif-gltf finding banner](../gemini-research/worldspace-editor/nif-gltf-conversion.md) 已標。

| 工具 | 方向 | 平台 | 型態 | 確認度 |
|---|---|---|---|---|
| **NifSkope（fo76utils fork）** | nif↔glTF | **Linux + Win** | GUI（CLI 批量未證） | ✅ 真實，最新 2025-12；**Linux 唯一原生 nif→glTF 選項** |
| **PyNifly**（+ Blender glTF 匯出） | nif↔glTF（雙向最高品質） | **Win-only**（native `NiflyDLL`） | Blender addon | ✅ 真實，但鎖 Windows（2026 仍 Win-only） |
| **Outfit Studio** | glTF→nif（蒙皮） | Linux 可編譯 / Wine | GUI | ✅ 真實，批量未證 |
| **ck-cmd** | **FBX**↔nif（**非 glTF**） | Win / Wine | CLI | ✅ 真實，但**不直接吃 glTF**（要 FBX 中轉） |
| **Compressonator** | dds 編碼 | Linux/Win/Mac | CLI | ✅ 真實（model-porting 已選定） |

**現況關鍵缺口**：**沒有任何已驗證的「批量 nif→glTF」pipeline**。PyNifly+Blender headless 範例存在但卡 Win-only；NifSkope fork 能單檔但批量能力未證。**這是本工具要填的第一個洞。**

---

## 跨平台架構難點（決定工具長相）

- **PyNifly 鎖 Windows**（native DLL）→ 蒙皮/高保真路線無法在 Manjaro 原生跑。
- **NifSkope fo76utils fork 跨平台** → 靜態 mesh 的 nif→glTF 可在 Linux 原生。
- 因此工具大概率走 **dual-backend**（沿用 model-porting 的「靜態原生 Linux / 蒙皮 reboot Windows」決策），用環境變數或設定切後端——與 ModForge `MODFORGE_TTS_BIN` 那種協議掛法同精神。

## 紋理 round-trip 注意（lossy，兩向都要處理）

- **glTF 不帶 dds** → nif→glTF 時 dds 自動轉 PNG/JPEG；glTF→nif 時要重新編回 dds（BCn + mipmap，Compressonator）。
- **法線約定**：Skyrim = DirectX(Y−)、glTF/Godot = OpenGL(Y+) → 轉換要 **Flip Y**（往返各翻一次）。
- **`_n` 貼圖**：alpha = glossiness；Godot 要分離的 roughness（= 1 − gloss，需 Invert）。
- **純預覽代理**：只要形狀的話可**完全跳過紋理**，Godot 用平色 proxy——worldspace editor 物件擺放的 MVP 走這條最省。

---

## Scope

**MVP（已鎖，2026-06-17）：vanilla `.nif` → glTF 批量轉換**——靜態 mesh、Linux 原生、**跳紋理用平色 proxy**，輸出餵 [worldspace editor](../godot-worldspace-editor/README.md) 當物件代理。
一鎚解三件事：① 填上「無已驗證批量 nif→glTF」缺口；② 讓 worldspace 物件編輯能往前走；③ 避開 PyNifly 的 Windows 鎖（靜態走 NifSkope fo76utils fork 即可，Linux 原生）。

**完整目標（MVP 後）**：`.nif`+紋理 ↔ glTF/Godot ↔ FBX/OBJ 全矩陣雙向，含紋理重映射與蒙皮（蒙皮走 Windows/PyNifly 後端）。

## Open

- **批量 nif→glTF 的可行載體**（MVP 第一關，**待主力機實測**）：NifSkope fo76utils fork 有沒有可腳本化的 CLI/headless？agent 查到 GUI 確定、CLI 批量未證。若 GUI-only → 找替代或考慮自寫 NIF 靜態 mesh parser（只需 geometry，門檻不高）。
- ~~**協議形狀**~~ ✅ 草案 2026-06-17 [PROTOCOL.md](PROTOCOL.md)（離線設計）：掛勾 `MODFORGE_NIF2GLTF_BIN`（黑盒 exec，照 voicegen 慣例）、單檔 `--in/--out/--flat` CLI、批量靠呼叫方給的 `manifest.json`（轉換器不讀 ESM）、exit code、Flip-Y 法線約定。**契約 backend-agnostic，後端載體換掉不動契約**——但載體本身仍待主力機實測（下一項）。
- **與 model-porting 的邊界**：正向內容留在 model-porting、本 sub_proj 只放工具實作與反向？還是把 model-porting 的 runbook 也收斂進來？（MVP 不碰正向，此邊界 MVP 後再定。）
