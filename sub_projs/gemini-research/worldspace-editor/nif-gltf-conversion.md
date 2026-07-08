# NIF ↔ glTF/Godot 轉換調查（Gemini，2026-06-16）

> 📦 **此調查已收斂進 sub_proj** → [sub_projs/model-converter/](../../model-converter/README.md)（模型格式雙向互轉工具）。下方為原始調查素材，正式結論看該 sub_proj 的工具盤點表。

> ⚠️ **查證修正 banner（2026-06-16，背景 agent 對真實 GitHub/release 核對）**——下文 Gemini 原稿多處有誤，採信前看這裡：
> - ❌ **`nif2gltf` Rust CLI 不存在**（`amPerl/nif` 只是 NIF parser、非 Skyrim 版、無 glTF 輸出；`cargo install nif` 用法是捏的）。**勿規劃此工具。**
> - ✅ **NifSkope glTF export 真實**，但在 **`fo76utils/nifskope` fork**（非 hexabits、非 CLI 是 GUI），且 **Linux+Windows 雙平台**（最新 build 2025-12-30）。**這是最可信的選擇，主力機 Manjaro 可直接跑。** geometry-only，正好夠視覺代理。
> - ✅ **PyNifly 真實、Windows-only 屬實**（V27.2.0，2026-06-15）；headless 批次「技術可行但無官方文件背書」。高保真但鎖 Windows。
> - ⚠️ **Outfit Studio 的 glTF 支援未證實**（只確認 OBJ/FBX/NIF）；**ck-cmd 只做 FBX↔NIF 非 glTF**。
> - ⚠️ 原文 SkyMeshGLTF 連結錯，正解 `github.com/nlaha/SkyMeshGLTF`。
> - 結論已併入 [worldspace-editor-design.md](../../../workflows/specs/worldspace-editor-design.md) 與 idea README。

關聯：[workflows/idea/worldspace-editor/README.md](../../godot-worldspace-editor/README.md)
先讀：[workflows/idea/asset-pipelines/model-porting/README.md](../../../workflows/idea/asset-pipelines/model-porting/README.md)（2026-06-09 靜態 mesh pipeline 研究，tool 確認度較高）

---

## Query 1: NIF to glTF 工具現況

The current state (2024–2025) of Skyrim NIF to glTF conversion has matured significantly, shifting away from legacy plugins toward more robust, active tools.

### 1. The Modern Standard: PyNifly
**PyNifly** has effectively replaced the official Blender Niftools addon for most modern modding workflows.
- **Current State:** Highly active. It supports **Blender 4.2, 4.4, and 5.0 (alpha/beta)**. It is the recommended tool for Skyrim SE/AE (using `BSTriShape`) and remains compatible with Skyrim LE (`NiTriShape`).
- **glTF Workflow:** PyNifly acts as a NIF <-> Blender bridge. To get a glTF, you import the NIF using PyNifly, then use Blender's native **glTF 2.0 exporter**.
- **Static vs. Skinned:** It handles both excellently. It is particularly strong at preserving skin weights, partitions, and rigging for use in modern Blender versions.
- **Textures (DDS):** PyNifly imports shader properties into Blender materials. However, because the glTF 2.0 specification does not natively support DDS, Blender's native exporter will automatically convert them to **PNG** or **JPEG**.

### 2. Direct Export: NifSkope (Experimental Forks)
If you need a quick "one-click" conversion without a 3D suite, specific development forks of NifSkope 2.0 are the current go-to.
- **The "Meshageddon" Forks:** Specifically the **fo76utils** and **hexabits** forks of NifSkope 2.0.
- **Features:** These versions allow you to right-click a node and **"Export to glTF."**
- **Capabilities:** They support both static and skinned meshes (including bone hierarchies).
- **Limitations:** While export is robust, **importing** a glTF back into NifSkope is still limited (it often loses skin partitions and does not support texture mapping on import).

### 3. The Power User's CLI: ck-cmd
- **Current State:** Stable and essential for high-fidelity custom rigging (Skywind/Skyblivion standard).
- **Role:** It is primarily an **FBX <-> NIF** converter. It does **not** support glTF directly.
- **Workflow:** Use `ck-cmd` to convert NIF to FBX, then use a tool like `FBX2glTF` or Blender to reach glTF. It is the gold standard for custom creature skeletons and complex `.hkx` animation workflows.

### 4. Legacy: Blender Niftools Addon
- **Current State:** **Stagnant/Legacy.** The official addon (v0.1.1) is generally stuck on **Blender 3.6 LTS**.
- **Warning:** It does not natively support Blender 4.0+. While community forks (like the "DuncanWasHere" fork) exist to bridge the gap, they are often buggy compared to PyNifly.

### Summary Table

| Feature | **PyNifly** (via Blender) | **NifSkope (Dev Forks)** | **ck-cmd** |
|:---|:---|:---|:---|
| **Best For** | High-quality rigging & editing | Quick previews & extraction | Batching & custom skeletons |
| **Direct glTF?** | No (uses Blender's) | **Yes** | No (FBX only) |
| **Static Meshes** | Excellent | Very Good | Excellent (via FBX) |
| **Skinned Meshes** | Best in class | Good (Export only) | Best for custom rigs |
| **DDS Handling** | Auto-converts to PNG | Maps to PBR (PNG/JPG) | Preserved in FBX |
| **Active in 2025?** | Yes | Yes (Experimental) | Yes |

---

## Query 2: Skyrim NIF → Godot 4 pipeline

For converting Skyrim `.nif` files to **Godot 4**, the recommended pipeline involves **Blender** as the bridge, specifically utilizing the **PyNifly** add-on.

### Required Tools
- **Blender (4.4+ recommended)**
- **PyNifly Add-on (by BadDogSkyrim):** https://github.com/BadDogSkyrim/PyNifly — *Note: PyNifly is currently Windows-only as it relies on a native DLL (`NiflyDLL`).*
- **Texconv or ImageMagick (Optional):** To batch convert `.dds` textures to `.png` or `.webp` if Godot has trouble with specific DDS compressions (like BC7).

### The Conversion Process
1. **Import to Blender:** Use **PyNifly** to import your `.nif`. It will automatically read the `BSLightingShaderProperty` and map them to Blender's **Principled BSDF** shader nodes.
2. **Texture Handling (The `_n` Map):** Skyrim's `_n.dds` textures are non-standard: **RGB** contains the Normal map, but the **Alpha channel** contains the **Glossiness/Smoothness** map. Godot expects a separate **Roughness** map. In Blender, connect the Alpha output of your normal texture to "Roughness" (plus an `Invert` node, since `Glossiness = 1 - Roughness`).
3. **Export as glTF 2.0:** Export from Blender as a `.glb` or `.gltf` file.
4. **Import to Godot 4:** Drop the glTF into your Godot project. **Normal Map Correction:** Skyrim uses **DirectX (Y-)** normal maps, while Godot defaults to **OpenGL (Y+)** → check **"Normal Map > Flip Y"** in Godot material inspector.

### BSLightingShaderProperty Texture Slots
- Slot 0: Diffuse (`_d`)
- Slot 1: Normal + Gloss (`_n`) — packed alpha = glossiness
- Slot 2: Glow/Subsurface or Environment Mask (`_m` or `_g`)

### Skyrim vs Godot Material Mapping

| Feature | Skyrim NIF | Godot 4 (`StandardMaterial3D`) |
|:---|:---|:---|
| **Normal Map** | DirectX (Y-) | OpenGL (Y+) — *Requires Flip Y* |
| **Glossiness** | Packed in `_n` Alpha channel | Separate Roughness map — *Requires Invert* |
| **Emission** | `_g` texture + Emissive Color | Emission slot |
| **Transparency** | Alpha Testing / Blending flags | Transparency mode (Alpha, Scissor, or Hash) |

---

## Query 3: glTF → NIF 反向轉換

### Primary Tools for glTF to NIF Conversion

| Tool | Format Path | Best For | Support Level |
|:---|:---|:---|:---|
| **PyNifly** (Blender) | Blender → NIF | **Skinned Meshes** (Armor, Bodies) | Full skinning, partitions, animations |
| **Outfit Studio** | glTF → NIF | **Armor/Clothing** (Rigging) | UI-based weight copying |
| **ck-cmd** (CLI) | glTF/FBX → NIF | **Static Meshes** (Clutter, Kits) | Batch processing large asset libraries |
| **CK Official Tool** | glTF/FBX → NIF | **Statics & Basic Rigging** | Official Bethesda tool in modern Steam CK |

### Critical Limitations
1. **Material/PBR Mismatch:** glTF's `Metal/Rough` maps won't work in-game — must convert textures to `.dds` (Diffuse, Normal+Gloss, Specular) and use NifSkope or Outfit Studio to point the NIF to these files.
2. **Skinned Mesh Partitions:** Skyrim armor requires `BSDismemberSkinInstance` partitions — most glTF exporters won't know these exist; must set in PyNifly or Outfit Studio *after* importing glTF.
3. **Collision (Statics):** glTF doesn't have a native "Skyrim Collision" type — in PyNifly you can designate a mesh as `bhkCollisionObject`; in `ck-cmd` you can auto-generate a convex hull.
4. **Vertex Limit:** Skyrim LE can struggle with high-poly meshes — stay under ~65k vertices per `NiTriShape`.

### Outfit Studio glTF Import
Outfit Studio now has native glTF import/export (as of v5.6+): `File > Import > From glTF`. Better than OBJ because **glTF preserves skinning weights** and vertex colors. World-class for skinned meshes (armor). For statics, works fine but no batching power.

---

## Query 4: 批量 NIF → glTF CLI 工具

### Dedicated CLI Tools (Fastest for Batch)

#### amPerl/nif (Rust CLI)
- **Repository:** https://github.com/amPerl/nif
- **Feature:** Includes a standalone **`nif2gltf`** binary.
- **Installation:** `cargo install nif`
- **Usage:** `nif2gltf -i ./meshes -o ./gltf_output`

#### SkyMeshGLTF (Python)
- **Repository:** https://github.com/SnottyButt/SkyMeshGLTF
- Specifically designed for mass-converting Skyrim libraries while preserving material paths.
- **Status:** Stable and actively used for engine-migration projects (Unity/Godot).

### PyNifly + Blender Headless Mode
```python
import bpy, os, sys

def batch_convert(input_folder, output_folder):
    bpy.ops.preferences.addon_enable(module="io_scene_pynifly")
    for file in os.listdir(input_folder):
        if not file.lower().endswith(".nif"): continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.pynifly(filepath=os.path.join(input_folder, file))
        bpy.ops.export_scene.gltf(filepath=os.path.join(output_folder, file.replace(".nif", ".glb")), export_format='GLB')

# Run: blender -b -P batch_nif_to_gltf.py -- C:/in C:/out
batch_convert(sys.argv[-2], sys.argv[-1])
```

### Recent GitHub Projects (2024-2025)

| Project | Status (2025) | Best For |
|:---|:---|:---|
| **hexabits/nifskope** | **Active (Dec 2025)** | Leading dev fork, experimental glTF 2.0 export |
| **fo76utils** | **Active** | High-performance CLI for Bethesda assets |
| **BadDogSkyrim/PyNifly** | **Active** | Standard for Blender 4.x/5.0 pipelines |

---

## 關鍵發現摘要（待人工驗證）

### 工具清單（名稱、repo URL、狀態）⚠️ URL 需人工驗證

| 工具 | URL | 狀態 | 用途 |
|---|---|---|---|
| PyNifly | https://github.com/BadDogSkyrim/PyNifly | Active 2025 | NIF↔Blender，最高品質，**Windows-only** |
| NifSkope hexabits fork | https://github.com/hexabits/nifskope | Active Dec 2025 | 快速 NIF→glTF 直出，無需 Blender |
| NifSkope fo76utils fork | https://github.com/fo76utils/fo76utils | Active | 高效能 CLI，含 Starfield |
| amPerl/nif (Rust) | https://github.com/amPerl/nif | 不明（Gemini 可能幻覺）| `nif2gltf` CLI，批量 |
| SkyMeshGLTF | https://github.com/SnottyButt/SkyMeshGLTF | 不明（Gemini 可能幻覺）| 保留材質路徑的批量轉換 |
| ck-cmd | https://github.com/aerisarn/ck-cmd | Active（model-porting 研究已確認）| FBX↔NIF，不直接支援 glTF |
| Outfit Studio | https://github.com/ousnius/BodySlide-and-Outfit-Studio | Active（已確認）| glTF→NIF 蒙皮 mesh |

### 雙向轉換可行性

| 類型 | NIF→glTF | glTF→NIF | 備註 |
|---|---|---|---|
| 靜態 mesh | ✅ 可行（PyNifly + Blender headless / hexabits NifSkope）| ✅ 可行（ck-cmd / PyNifly）| 最容易的方向 |
| 蒙皮 mesh | ✅ 可行（PyNifly，最高品質）| ✅ 可行（PyNifly + Outfit Studio）| 需要 Windows（PyNifly DLL）|
| 紋理 DDS | ⚠️ 自動轉 PNG（glTF 不支援 DDS）| ⚠️ 需手動重映射回 BSLightingShaderProperty | 需額外處理 |

### 對 Godot Worldspace Editor 的影響

- **物件預覽（靜態 NIF → glTF 批量）**：PyNifly + Blender headless 是最可靠路線，但 **PyNifly 是 Windows-only**——需要在 Windows 環境跑批量轉換後，把 glTF 帶到 Godot/Linux 用。amPerl/nif Rust 工具若真實存在則可跨平台，值得驗證。
- **Normal map**：NIF→Godot 需要 Flip Y；Godot→NIF 反向需要反轉回 DirectX。
- **glTF 作為中間格式**：glTF 不攜帶 DDS，轉換往返需要「材質重映射」步驟。對純預覽用途（只看形狀）可跳過材質直接用 proxy 顏色。
- **SkyUnity/SkyMeshGLTF 類工具**：若真實，可直接支援 Skyrim→Unity/Godot pipeline，值得確認。
