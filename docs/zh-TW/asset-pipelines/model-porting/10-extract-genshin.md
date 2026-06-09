# 10 — 從 Genshin Impact 原神解包（3DMigoto frame-dump + GIMI）

← [README](README.md) · 相關：[02-source-mesh-prep.md §5](02-source-mesh-prep.md)、[03-materials-textures.md](03-materials-textures.md)、[07-skinned-characters.md](07-skinned-characters.md)

原神是 **Unity** 遊戲,其資產 bundle 是**加密的**——所以乾淨的 Unity 解包器(AssetRipper、原版 AssetStudio)直接讀不了 mesh。這使原神成為這裡三個遊戲來源中**最難、最不 Linux-乾淨的一個**:[08] Dark Souls 原生/純 Python(最乾淨)、[09] WuWa 居中(Windows 側抽出、原生轉換),而原神是牆。正規路根本不是檔案解包器——而是把運行中的遊戲做 **3DMigoto frame-dump**、用 **GIMI**(GI-Model-Importer)擷取,再在 Blender 重建。鑑於你的**雙系統**,最省事路徑與 [09] 相同:**在 Windows 側做 frame-dump、在 Blender(原生 Manjaro)重建、接續脊椎。**

> 法務(不變):你擁有遊戲;轉出資產留**本機**,絕不發布。原神 bundle 加密——抽你自己的副本供個人用是我們守的線。

> **反作弊 / ToS 警告(先讀這個)。** 原神在 Windows 上跑**核心級反作弊**(`mhyprot`)。把工具(3DMigoto)注入**運行中的線上 client** 違反 HoYoverse 服務條款,帶有真實的**封號風險**。動手前對自己講清楚:這是這組裡風險最高的來源,而且風險是帳號級的,不只是技術性的。

---

## 1. 為何原神是最難的一個

另外兩個來源遞給你一個可離線解析的檔案。原神不行:

- **加密的 Unity bundle。** 原神的 `.blk` 容器是加密的,所以 **AssetRipper / 原版 AssetStudio 在 mesh 上直接失敗**。修改過的 fork(各種「GenshinStudio」/ AnimeStudio AssetStudio 分支)能用 XOR key + asset-index json 解出*部分*資產,但 mesh 還原是**部分且隨版本脆弱的**,而 animation clip 經常根本出不來。這不是乾淨、可重複的解包路。
- **所以社群標準是渲染時擷取,而非檔案解包。** 你把遊戲透過 3DMigoto wrapper 跑、dump GPU 實際在畫的幾何。

結論:沒有 soulstruct 式的「把 Python library 指向安裝目錄」路,也沒有 FModel 式的「解密 pak 再匯出 glTF」路。你擷取 frame。

---

## 2. 正規路 — 3DMigoto + GIMI

**GIMI**([github.com/SilentNightSound/GI-Model-Importer](https://github.com/SilentNightSound/GI-Model-Importer))是針對原神修改的 **3DMigoto** fork。3DMigoto 是個注入遊戲的 **DX11 shader 除錯 wrapper**;其 *frame-analysis dump* 擷取畫面上任何東西的 vertex/index buffer 與貼圖。GIMI 加上原神專屬的 Blender 重建腳本。

當前生態備註:GIMI 是原始版,而較新的 **XXMI / migoto-GIMI** 家族(統一的「X」model-importer launcher,涵蓋原神及其 HoYo 姊妹作)圍繞同一 3DMigoto 核心長出來。下面的擷取 + 重建機制兩者皆同。

流程:

1. **把 3DMigoto-GIMI 注入**運行中的遊戲(`3dmigoto-GIMI-for-playing-mods` build,或開了 frame-analysis 的 dev build)。
2. **在畫面上擺姿勢 / 顯示整個模型**——frame-dump 只擷取正在渲染的東西(§5)。
3. **按 F8**(frame-analysis / frame-dump)。3DMigoto 寫出一個 dump 資料夾:**vertex buffer(`.buf` / `.vb`)、index buffer(`.ib`)、與貼圖(`.dds`)**。
4. 跑 GIMI 的重建(一個 Python step)、透過 **`blender_3dmigoto_gimi.py`** Blender add-on 匯入(Edit → Preferences → Add-ons → Install)。它從原始 buffer 在 Blender 重組 mesh——幾何、UV,以及(角色的)**blend weights + indices**。

重建步驟(GIMI 腳本 + Blender)在 dump 資料夾存在後是**原生 Linux**。只有 dump 本身綁 Windows/DX11。

---

## 3. 雙系統是乾淨答案

3DMigoto 是 **DX11/Windows**,而在 **Proton/Wine 下把它注入原神非常麻煩**——wrapper、overlay 與遊戲自己的 launcher 在模擬下互鬥,而你還是在核心反作弊之上做這一切。別在 Wine 路上燒掉好幾天。

因為你**雙系統**,摩擦以與 [09] 相同的方式消失:

1. **Windows 側(建議)。** 重開機進 Windows、原生跑 3DMigoto-GIMI、frame-dump(F8)、把 dump 資料夾(`.buf`/`.vb`/`.ib` + `.dds`)複製到 Manjaro build 樹。
2. **Manjaro 側。** **原生**跑 GIMI 重建 + Blender add-on 重建 mesh,再續模型移植脊椎。

所以與 WuWa 完全一樣:**Windows 側抽出、Manjaro 側轉換。** *唯一*綁 Windows 的步驟是擷取;下游一切原生。

---

## 4. 靜態 vs 角色(走哪條)

- **多數原神移植是角色(蒙皮)。** 對角色 frame-dump 給 mesh + 貼圖 + 部分 blend weights/indices → **[07] 重定向路**(原神骨架 ≠ Skyrim 骨架)。這是牆,而這裡的重建**比 DS 或 WuWa 更麻煩**(你是從原始 GPU buffer 重建,而非乾淨的蒙皮匯出)。
- **靜態 prop(較易的首個目標)。** 若你只想證明管線,dump 一個**靜態物件**(prop、場景擺設)。無骨架 → 直上模型移植脊椎([02]→[04]),和 DS map piece 或 WuWa 靜態一樣的易得勝。靜態可接受的話先做這個。

---

## 5. 會咬人的坑

- **Frame-dump 只擷取被渲染的東西。** 任何在畫面外、被 cull、或當下沒畫的都不在 dump 裡。你必須**擺姿勢 / 顯示整個模型**——轉相機、觸發對的狀態——才能擷取每個元件。**透明 / 特效 mesh**(頭髮 alpha、FX)特別棘手,可能需分開擷取。
- **加密擋掉乾淨解包器。** 別浪費時間試讓 AssetRipper 讀 bundle——它讀不了(§1)。frame-dump 才是路。
- **Toon / NPR 渲染,非 PBR。** 原神用**非寫實 toon 渲染**——其貼圖慣例是 **Diffuse + Lightmap + Normalmap**,*非* PBR(沒有 albedo/roughness/metallic/AO 那套)。所以材質**必須在 [03] 為 Skyrim 重做**,且結果**不會看起來「Skyrim-native」**——扁平的動漫風與 Skyrim 光照打架。預留材質工;這不只是 channel-repack。
- **部分骨架。** frame-dump 來的 weights/indices 是部分且原始的——角色需要真正的重定向([07]),不是 drop-in 骨架。
- **Unity 單位/軸。** 原神是 Unity → **Y-up、公尺**。[02] transform 完全照 Unity FBX rip 校準(轉到 Skyrim Z-up/−Y、對 vanilla 尺縮放)。

---

## 6. 交給模型移植脊椎

一個重建好的 mesh(+ 貼圖)在 Manjaro 的 Blender 後:
1. **[02]** — 校準 transform;**Unity = Y-up、公尺** → 轉到 Skyrim Z-up/−Y、對 vanilla 尺縮放。Genshin→Skyrim 常數記一次。
2. **[03]** — 貼圖 → `.dds`;**把 NPR Diffuse/Lightmap/Normalmap 重做成 Skyrim 材質**(True PBR RMAOS 或 legacy)。這是原神的重步驟——toon → Skyrim 光照不是免費轉換。
3. **[04]** — NifTools 匯出 → `NiTriShape` 靜態 `.nif` + 碰撞(靜態路),或角色走 [07] 蒙皮路。原生。
4. **[05]/[06]** — `StaticSpec.Model` + `package` → 遊戲內。

角色的話,Blender 之後走 **[07] 重定向** → 依 README 的雙系統決定重開機進 Windows 用 PyNifly(蒙皮/weights 匯出)。

---

## 7.「完成」長什麼樣

- 3DMigoto-GIMI 注入(Windows 側),一個模型在整個模型於畫面上時 **frame-dump**(F8),dump 資料夾(`.buf`/`.vb`/`.ib` + `.dds`)複製到 Manjaro。
- GIMI 重建 + Blender add-on 在 Manjaro **原生重建 mesh**。
- Transform 校準(Unity Y-up/m → Skyrim)、NPR 貼圖重做([03])、交給 [04](靜態)或 [07](角色)→ 遊戲內 Skyrim 資產。
- 你帶著對 **ToS / 封號風險**的認知進場,並把轉出資產留**本機**。

---

### 來源
[GI-Model-Importer / GIMI(GH SilentNightSound — 3DMigoto fork、blender_3dmigoto_gimi.py、frame-dump → buffers、weights)](https://github.com/SilentNightSound/GI-Model-Importer) · [GIMI on Nexus](https://www.nexusmods.com/genshinimpact/mods/89) · [GenshinStudio — modded AssetStudio(加密 bundle 解密、XOR key + asset-index;mesh 部分)](https://github.com/Xiaobin0860/GenshinStudio) · [AssetRipper](https://assetripper.org/) · [Analyzing Genshin Impact's Anti-cheat Module(mhyprot、核心級)](https://research.meekolab.com/analyzing-genshin-impacts-anticheat-module) · [Trend Micro — mhyprot2 driver abuse(核心反作弊脈絡)](https://www.trendmicro.com/en_us/research/22/h/ransomware-actor-abuses-genshin-impact-anti-cheat-driver-to-kill-antivirus.html)。2026-06-09 確認。
