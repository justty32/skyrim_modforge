# 01 — 工具鏈設定（Manjaro + Windows 側）

← [README](README.md) · 下一份：[02-source-mesh-prep.md](02-source-mesh-prep.md)

一次裝完的東西，按執行位置分欄。靜態脊椎（[02]–[06]）**全在左欄**（原生 Manjaro）。Windows 欄*只*是蒙皮的 PyNifly 升級（[07]）。Wine 是中間小欄，兩個 LE-only / Windows-only 轉檔器可後置。

---

## 0. Layout

```
~/model-work/
  src/        進來的 .fbx/.obj/.gltf + 來源貼圖
  blender/    convert.py（headless 腳本，之後 repo 出貨 — [05]）
  out/        匯出的 .nif + .dds，組進 build 樹
  vanilla/    幾個抽出的 vanilla .nif 當尺寸/格式參考（[04]）
```

---

## 1. 原生 Manjaro — 靜態脊椎（無 Wine、無重開機）

```bash
# Blender（DCC + 原生匯入器 + headless 腳本）
sudo pacman -S --needed blender
# 貼圖壓縮（DDS / BCn / mipmaps）— 原生 Linux CLI
yay -S compressonator-bin     # 或從 GPUOpen-Tools/compressonator 編；提供 compressonatorcli
# 檢查/修復檯（Qt，原生可編；Wine 亦可）
sudo pacman -S --needed nifskope   # AUR；若沒有，用 Windows build 走 Wine
```

**Blender NifTools addon**（`io_scene_niftools`）— 純 Python，原生：
1. 從 [github.com/niftools/blender_niftools_addon/releases](https://github.com/niftools/blender_niftools_addon/releases) 下最新 release `.zip`。
2. Blender → Edit → Preferences → Add-ons → Install from Disk → 選 zip → 啟用。
3. 確認：`File → Export → NetImmerse/Gamebryo (.nif)` 出現。

**此欄給你：** 匯入任何 FBX/OBJ/glTF、修 transform、建 convex/box 碰撞、map 材質、**匯出 SSE 可用的 `NiTriShape` `.nif`**、把貼圖壓成 `.dds`。整條靜態 prop 管線，原生。（[04] 確認 NifTools 做 convex-hull → `bhkConvexVerticesShape` 與 basic 非蒙皮 SSE 匯出。）

---

## 2. Windows 側（重開機）— PyNifly，只給蒙皮升級

PyNifly **只有 Windows**（出貨原生 `NiflyDLL.dll`；2026 確認仍只有 Windows）。你*只*在碰蒙皮角色或 PyNifly 處理更好的碰撞時才重開機進 Windows。靜態永遠不離開 Manjaro。

Windows 分割區上：
1. 裝 **Blender 4.4+**（PyNifly 跟現行 Blender）。
2. 從 [github.com/BadDogSkyrim/PyNifly](https://github.com/BadDogSkyrim/PyNifly) 裝 **PyNifly**（Add-ons → Install release zip）。
3. 確認 SSE 匯出含 shaders + `_0`/`_1` weights + `BSDismemberSkinInstance`。

PyNifly 是這些的黃金標準：skin weights、`BSDismember` partitions、碰撞、完整 `BSTriShape` SSE 輸出。雙系統讓它成為一級後端，而非權宜——見 [07]。

> **為何不用 Wine-Blender-PyNifly？** PyNifly 透過 Blender 內建 Python 載入原生 Windows DLL；Wine 下那個 DLL-load 就是最脆弱、無維護成功路徑的部分。真重開機比硬幹可靠。你選了雙系統——用它。

---

## 3. Wine 中間欄（選用，需要才碰）

兩個轉檔器是 Windows 執行檔，Wine 下可接受地跑，補原生欄的缺口。**MVP 兩個都跳過**——原生欄已能產出可用靜態。

| 工具 | 為何想要 | Wine 狀態 |
|------|----------|-----------|
| **ck-cmd**（[GH aerisarn](https://github.com/aerisarn/ck-cmd)） | 一行 `importfbx … -e` → nif，含 materials→`BSLightingShaderProperty`、vertex colors、「95% game-ready」。若某來源 NifTools 材質映射很煩可用。**只有 LE-form nif**（SSE 內可用）。 | CLI；Wine candidate（Linux/Mono 未文件化——測、設時限） |
| **texconv**（[MS DirectXTex](https://github.com/microsoft/DirectXTex)） | de-facto Skyrim DDS CLI；GPU-accel BC6H/BC7。 | Windows → Wine（成熟）。Compressonator 是原生替代，故選用。 |
| **Outfit Studio**（[GH ousnius](https://github.com/ousnius/BodySlide-and-Outfit-Studio)） | armor refit / Copy-Bone-Weights 給蒙皮（[07]）。有「Building on Linux」路徑；否則 Wine。 | 原生編 *或* Wine——給 [07]，非靜態 |

之後後端選擇對映 Papyrus native-vs-Wine 拆法（`MODFORGE_CKCMD` 可帶 `wine ` 前綴）——見 [05] §4。

---

## 4. 可換後端契約（一個接縫，多個匯出器）

如語音計劃的 `text+ref → wav` wrapper，定義**一個邏輯操作**讓後端可變：

```
mesh_to_nif(blend_or_fbx, target_nif, opts) :
    backend = niftools (原生)  |  ckcmd (wine)  |  pynifly (Windows 重開機)
```

- **`niftools`** — 預設、原生、靜態。headless 接縫是 `blender --background --python convert.py -- <args>`（[05] §3）。
- **`ckcmd`** — Wine 下的替代材質路徑。
- **`pynifly`** — 當 `opts.skinned` 為真時選；你 Windows 側跑（目前手動；[07]）。

貼圖同理：`tex_to_dds(src, slot, profile)` → 預設 Compressonator（原生），texconv（Wine）為替代。ModForge 用 env var 選後端、缺則「warn, skip」（[05] §4）——既有的條件式工具姿態（`Papyrus.cs`）。

---

## 5. VRAM / CPU 現實檢查（16 GB 這裡綽綽有餘）

Mesh 移植**不像**語音克隆 GPU-bound——主要是 CPU + 硬碟：
- **Blender 匯入/匯出、NifTools、碰撞 hull** — CPU/RAM；單 prop 微不足道。
- **Compressonator BC7** — 可用 GPU/APU 加速，但單貼圖 CPU 模式就夠；GPU 只在大批量有感。
- **16 GB VRAM 對這條管線是過剩**——它對*語音*計劃與任何 AI 貼圖放大才重要，對 nif/dds 轉檔不是。這裡不要圍著它調任何東西。

所以：無 VRAM 瓶頸、無模型下載、無 CUDA venv。唯一「重」依賴是 Blender 本身。

---

## 6.「完成」長什麼樣

- `blender` 啟動；NifTools addon 啟用；`.nif` 匯出選單在（原生）。
- `compressonatorcli` 跑得動、吐帶 mipmaps 的 BC7 `.dds`。
- NifSkope 開得了 vanilla `.nif` 當參考。
- *（後置）* Windows 分割區備好 Blender + PyNifly 給 [07]。

→ 進 [02](02-source-mesh-prep.md) 把 mesh 弄進來。

---

### 來源
[Blender NifTools addon releases](https://github.com/niftools/blender_niftools_addon/releases) · [PyNifly（GH，只有 Windows）](https://github.com/BadDogSkyrim/PyNifly) · [Compressonator（GH GPUOpen-Tools，Win/Linux/Mac CLI）](https://github.com/GPUOpen-Tools/compressonator) · [ck-cmd（GH aerisarn）](https://github.com/aerisarn/ck-cmd) · [Outfit Studio（GH ousnius）](https://github.com/ousnius/BodySlide-and-Outfit-Studio)。內部：`src/ModForge.Core/Papyrus.cs`（後端 env-var 模式）。
