# 03 — 材質與貼圖（→ `BSLightingShaderProperty` + `.dds`）

← [README](README.md) · 上一份：[02-source-mesh-prep.md](02-source-mesh-prep.md) · 下一份：[04-nif-and-collision.md](04-nif-and-collision.md)

兩件事：(1) 把來源材質 map 到 Skyrim 的 shader + texture-slot 模型，(2) 把來源貼圖壓成 `.dds`。你選了**兩種材質 profile、build 時選**——故記錄 legacy 與 True-PBR 兩條 channel mapping，True PBR 為建議預設（你 baseline 已含 Community Shaders）。

本章輸出：一組正確命名的 `.dds`，**以及** [04] 烤進 nif `BSShaderTextureSet` 的 Data-relative 路徑。

---

## 1. Skyrim 材質模型

一個 shape 的材質是 **`BSLightingShaderProperty`**（lighting model + flags）連到 **`BSShaderTextureSet`**——一份有序 texture slot：

| Slot | 內容 | 後綴 | 備註 |
|------|------|------|------|
| 0 | Diffuse / albedo | （無） | BC1（不透明）或 BC3（含 alpha） |
| 1 | Normal | `_n` | **DirectX 慣例**（Y/綠相對 OpenGL 反）；BC7 或 BC5 |
| 2 | Glow / skin / subsurface | `_g`/`_sk` | 選用 |
| 3 | Height/parallax | `_p` | 選用 |
| 4 | Environment（cubemap） | `_e` | 選用 |
| 5 | Environment mask | `_em`/`_m` | 選用 |
| 7 | Specular /（True PBR：RMAOS） | `_s` | 見 §3 |

set 裡的路徑是**烤進 nif 的 Data-relative 字串**（如 `Textures\Mine\crate.dds`）。錯路徑 → 隱形/無貼圖，**無報錯**（[[vanilla-nif-paths-must-be-verified]]）。ModForge 擁有這些字串（[04] §4、[05]）。

---

## 2. 貼圖 → `.dds`（原生：Compressonator）

目標：**BCn block 壓縮 + mipmaps**。原生 Linux CLI 是 Compressonator（`compressonatorcli`），確認 Win/Linux/Mac、BC1–BC7 + mipmap 生成 + 資料夾批量。

```bash
# diffuse → BC1（不透明）或 BC3（含 alpha），完整 mip 鏈
compressonatorcli -fd BC1 -miplevels 20 src/crate_diffuse.png out/textures/crate.dds
# normal map → BC7（或 2-channel 用 BC5）；來源若 OpenGL 要 INVERT GREEN（見 §4）
compressonatorcli -fd BC7 -miplevels 20 src/crate_normal_dx.png out/textures/crate_n.dds
```
`-miplevels 20` = 「能生幾層就幾層」（到 1×1 停）。批量整個資料夾就傳目錄 + file filter；CLI 依輸入命名輸出。

**texconv（Wine）是替代**——GPU-accel BC6H/BC7、de-facto Skyrim DDS 工具，但 Compressonator 原生移除了 MVP 的 Wine 依賴。只在碰到 BC7 品質邊界才用 texconv（[01] §3）。

> **絕不用 ffmpeg/ImageMagick 做最終 BCn**，除非你驗過輸出在 NifSkope 載得了——基本 DDS writer 常跳過 mipmaps 或用 Skyrim 不取樣的格式。Compressonator 是可靠原生路。

---

## 3. PBR channel mapping（「寫一次、批量套」槓桿）

來源 PBR（glTF metal/rough）與 Skyrim shader 對「哪個 channel 放什麼」不一致。每種來源慣例 map 一次，再批量。

**來源 — glTF metal/rough：**
- Base color（albedo）→ slot 0 diffuse
- 一張打包 ORM/MR 貼圖：**Occlusion=R、Roughness=G、Metalness=B**
- Normal → slot 1

**目標 A — Legacy spec/gloss（`materialProfile: legacy`，vanilla 相容）：**
- diffuse → slot 0
- normal → slot 1（DirectX）；**gloss 常打包進 normal map 的 alpha**
- 一張 specular map → slot 7
- 有損：metal/rough 必須*轉*成 spec/gloss（roughness→gloss 反相、推導 specular）。可接受，不漂亮。

**目標 B — True PBR / Community Shaders（`materialProfile: truepbr`，建議）：**
- diffuse → slot 0
- normal → slot 1
- **RMAOS pack** → 單張貼圖 **Roughness=R、Metallic=G、AO=B、Specular=A**（PBR shader 用的 slot）+ 一個小的 per-material JSON 給 CS PBR 系統讀。
- glTF metal/rough → RMAOS 是**乾淨 channel repack**（R←roughness、G←metalness、B←occlusion、A←specular const），*非*有損轉換。這就是 True PBR 當預設的理由：你 baseline 已有 Community Shaders，映射是決定性的。

**用 Compressonator/ImageMagick 做 channel-repack** 是 per-source 規則：讀來源 channel、寫目標打包、BC 壓縮、mipmap，再把產出的 Data-relative 路徑交給 [04]。（TruePBR Manager 在 Windows 上就自動化這個——ModForge 轉檔器要 emit 的範本，[05]。）

---

## 4. Normal-map 慣例（一個 flag，易漏）

Skyrim 要 **DirectX 慣例 normal**（綠/Y 朝下）。glTF/Unity/UE 來源常是 **OpenGL 慣例**（綠朝上）。若表面看來「反相」/打光錯：

```bash
# 壓縮前反相綠 channel（ImageMagick），再 Compressonator
convert src/crate_normal_gl.png -channel G -negate +channel src/crate_normal_dx.png
```
每種來源慣例決定一次（同 [02] 的 scale）烤進規則。錯慣例 = 錯打光，但*可見*——比錯路徑安全，好認好修。

---

## 5. 在 Blender 內做（headless 路）

給自動化 [05] 流程，材質映射在匯出 nif 的同一個 `convert.py`：
- 讀匯入材質的 nodes（glTF 是 Principled BSDF：Base Color、Metallic、Roughness、Normal inputs）。
- 各解析到來源圖；emit `.dds`（shell 到 Compressonator）用目標命名；把 NifTools 材質的 texture-slot 路徑設成 Data-relative 字串。
- 匯出時 NifTools 把它們寫進 `BSShaderTextureSet`（[04]）。

所以 [03] 最終非獨立手動階段——折進 Blender 匯出腳本。手動 runbook（[06]）首次逐步做以證明每塊。

---

## 6.「完成」長什麼樣

- 帶 **mipmaps**、每 slot 正確 BCn、normal 為 **DirectX** 慣例的 `.dds`。
- 決定好的 **profile**（legacy 或 truepbr）與該來源的 channel-repack 規則。
- 給 [04] 烤進 nif 的確切 Data-relative 路徑記下。

→ [04](04-nif-and-collision.md) 匯出 nif 並把這些路徑寫進去。

---

### 來源
[Compressonator command-line docs（BC1–7、mipmaps、batch）](https://compressonator.readthedocs.io/en/latest/command_line_tool/commandline.html) · [DirectXTex texconv](https://github.com/microsoft/DirectXTex) · Community Shaders True PBR / RMAOS 慣例 · Skyrim `BSShaderTextureSet` slot 順序（Beyond Skyrim NIF Data Format）。內部：`StaticSpec.AlternateTextures`（`Spec.Items.cs`）供 per-instance texture-set 替換。
