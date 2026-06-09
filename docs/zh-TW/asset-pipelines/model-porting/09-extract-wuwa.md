# 09 — 從 Wuthering Waves 鳴潮解包（FModel / CUE4Parse）

← [README](README.md) · 相關：[02-source-mesh-prep.md §5](02-source-mesh-prep.md)、[03-materials-textures.md](03-materials-textures.md)、[07-skinned-characters.md](07-skinned-characters.md)

鳴潮是 **Unreal Engine 5** 遊戲;資產住在加密的 UE `.pak`/`.utoc`/`.ucas` archive。解包器是 **FModel**(GUI),建於 **CUE4Parse**(跨平台 UE 解析 library)。鑑於你的**雙系統**,最省事路徑是:**在 Windows 側用 FModel 原生抽出、把 glTF 拎回 Manjaro、接續脊椎。** 想不重開機的話,有 Manjaro-原生路(CUE4Parse CLI / Wine FModel)——§6。

> 法務(不變):你擁有遊戲;轉出資產留**本機**,絕不發布。鳴潮 archive 加密——抽你自己的副本供個人用是我們守的線。

---

## 1. UE5 強加的兩個前置

不像 FromSoft,UE5 解包需要兩個**隨每次遊戲 patch 變**的活動件:

1. **AES 解密金鑰。** 鳴潮的 pak 是 AES 加密。金鑰每版輪換。別寫死過時的——在 FModel 把 **AES key endpoint** 設成社群維護的 feed,如 `https://yarik0chka.github.io/wuwa-keys/keys.json`,FModel 拉當前金鑰。(固定遊戲版本用單一靜態金鑰也行,如 patch-1.2.0 的 `0x4D65...6469`,但 endpoint 撐得過 patch。)
2. **`mappings.usmap`。** UE5 需 type-mappings 檔來解讀資產屬性。拿當前鳴潮 `.usmap`(社群提供,或在你的安裝上用 Dumper-7 dump),再 FModel → Settings → General → **Local Mapping File** → 啟用 + 指向它。

usmap 不符或 AES 過時 = 資產解析不出。這兩個是鳴潮真正的摩擦;mesh 匯出本身很簡單。

---

## 2. 把 FModel 指向遊戲

1. FModel → Settings → **Game's archive directory** = 鳴潮的 `...\Client\Content\Paks`(`pakchunk*.pak` + `.utoc`/`.ucas`)。
2. Settings → **UE Versions** = **`GAME_WutheringWaves`**。**用這個遊戲 profile——別手選原始 UE 版本號;** profile 編碼了鳴潮確切的(修改過)引擎 build。(FModel 設定確認,2026。)
3. 載入 archive;FModel 用 AES 金鑰解密、用 usmap 解析。

---

## 3. 找到並匯出 mesh

在 Archives/Folders 樹,導覽到角色或環境資產。右鍵某 mesh 資產 → **Export**。格式選項(Settings → Export):

| 格式 | 用途 | 備註 |
|------|------|------|
| **glTF 2.0** | **建議** | mesh + 骨架 + 貼圖一包;FModel 的 glTF 匯出 2024 修好了。乾淨餵進 Blender [02]。 |
| **PSK / PSKX** | ActorX | PSK = 蒙皮、PSKX = 靜態。較舊;需 Blender PSK 匯入器。 |
| **UEFORMAT** | 最新 | 比已死的 ActorX 豐富;需 UEFormat Blender 匯入器。 |

FModel 對多數資產**連同貼圖一起匯出**——所以你免第二趟拿到 diffuse/normal/packed maps。靜態環境資產匯出為靜態 mesh(PSKX/glTF)——那是你的易得勝;角色帶 UE 骨架匯出。

---

## 4. 靜態 vs 角色(走哪條)

- **環境 / props(靜態)** → glTF 或 PSKX → 直上模型移植脊椎([02]→[04])。最易得的鳴潮勝,無骨架。
- **角色(蒙皮)** → glTF/PSK 帶 **UE 骨架 + weights** → [07] 重定向路(UE skeletal → Skyrim 骨架 bone-map)。這是牆;先做靜態。

---

## 5. 兩個會咬人的 UE 專屬坑

- **Nanite = 超高 poly。** 鳴潮環境 mesh 可能是 Nanite。FModel/CUE4Parse 把 Nanite 轉成標準 LOD mesh,但結果對 Skyrim 可能**太密**(Gamebryo 無 Nanite)。進 [04] 前在 **Blender 抽稀**(Decimate modifier,或匯入較低 LOD),否則 Skyrim 卡死。這是鳴潮→Skyrim 第一大坑。
- **UE 材質不轉。** 你拿到*基礎貼圖*,非材質圖。在 [03] 重做 Skyrim 材質:辨識 diffuse / normal / packed(ORM 風)maps、channel-repack 到 True PBR(RMAOS)或 legacy、BC 壓縮。UE normal map 通常偏 OpenGL——檢查綠 channel 慣例([03] §4)。

---

## 6. Linux 故事(雙系統讓這簡單)

**FModel 的 GUI 是 Windows(WPF)。** 你的選項,最簡單先:

1. **Windows 側(對你建議)。** 重開機進 Windows、跑原生 FModel、匯出 glTF、把 `.glb` + 貼圖複製到 Manjaro build 樹。既然你雙系統,這無摩擦,且避開模擬下的 Wine/usmap/AES 怪症。*轉換*(Blender→nif)接著原生 Manjaro——只有*抽出*在 Windows 側。
2. **Wine。** 不少人 FModel 在 Wine 下跑;AES-endpoint 抓取 + usmap 仍可用。測、設時限。
3. **CUE4Parse CLI(原生)。** CUE4Parse 是跨平台 .NET;headless CLI(如 UnrealExporter,或基於 CUE4Parse 的小 `dotnet` 工具)在原生 Linux 抽出。比 FModel 多些設定,但無重開機、無 Wine。**UModel** 有 Linux CLI 但對 UE5 過 ~5.4 落後 → 像鳴潮這種當前 UE5 標題優先 CUE4Parse/FModel。

鑑於你雙系統,**(1)** 是乾淨答案:Windows 側抽出、Manjaro 側轉換。

---

## 7. 交給模型移植脊椎

一個靜態鳴潮 mesh 成 `.glb`(+ 貼圖)在 Manjaro 後:
1. **[02]** — 匯入 glTF;**UE 單位是公分** → ÷100 再縮到 Skyrim units(經典 cm-vs-m 坑)、轉到 Z-up/−Y、對尺校準。**若 Nanite 密則抽稀。**
2. **[03]** — 貼圖 → `.dds`、channel-map(UE packed maps → RMAOS/True PBR 或 legacy)。
3. **[04]** — NifTools 匯出 → `NiTriShape` 靜態 `.nif` + 碰撞。原生。
4. **[05]/[06]** — `StaticSpec.Model` + `package` → 遊戲內。

---

## 8.「完成」長什麼樣

- FModel 設好:鳴潮 Paks 目錄、`GAME_WutheringWaves`、當前 AES endpoint + 相符 `.usmap`。
- 一個**靜態鳴潮 mesh** 匯出為 glTF + 貼圖(Windows 側)、複製到 Manjaro。
- 抽稀(若 Nanite)、transform 校準(UE cm→Skyrim)、交給 [04] → 遊戲內 Skyrim 靜態。

---

### 來源
[FModel(GH 4sval — UE archive explorer、glTF/PSK/PSKX/UEFormat 匯出、貼圖、Nanite→LOD)](https://github.com/4sval/FModel) · [FModel](https://fmodel.app/) · [CUE4Parse mesh conversion & export(DeepWiki)](https://deepwiki.com/FabianFG/CUE4Parse/4.1-mesh-conversion-and-export) · [wuwa-keys AES endpoint(GH yarik0chka)](https://github.com/yarik0chka/wuwa-keys) · [TCRF — FModel UE5 usmap guide](https://tcrf.net/Help:Contents/Finding_Content/Game_Engines/Unreal_Engine_5/FModel)。2026-06-09 確認。
