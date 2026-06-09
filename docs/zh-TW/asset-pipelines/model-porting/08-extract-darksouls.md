# 08 — 從 Dark Souls / FromSoft 解包（soulstruct-blender）

← [README](README.md) · 相關：[02-source-mesh-prep.md §5](02-source-mesh-prep.md)、[07-skinned-characters.md](07-skinned-characters.md)

通用 MVP（[02]–[06]）不綁來源；本章把 Dark Souls 變成*真正*的來源。**FromSoft 是現存最乾淨的 Linux 來源**——`soulstruct` + `soulstruct-blender` 是純 Python,所以整條解包→Blender 路是**原生 Manjaro、零 Wine**。一個 DS **map piece** 正是 survey 推薦的首個 MVP:靜態 prop,直接落上 [04] 的 nif 脊椎。

> 法務（不變）:你擁有遊戲;轉出資產留**本機**,絕不發布。

---

## 1. 為何這是最簡單的一個

`soulstruct` 在 **Python 裡**讀 FromSoft 的容器格式（DCX/BND/BHD/BDT/TPF/FLVER）——匯入路上無 Windows 工具。`soulstruct-blender` 把它包成 Blender add-on,直接匯入 FLVER 含材質、UV,以及（角色的）armature + weights。所以:

- **Map pieces / objects（靜態）** → 直上 [04] §1,甜蜜點。
- **角色** → 帶 FromSoft 骨架 → 重定向（[07]）。

唯一*可能*出現 Windows 工具的地方,是某些遊戲的一次性 archive 解包（§3）——而且常可避開。

---

## 2. 挑遊戲（最乾淨的先）

| 遊戲 | soulstruct-blender | DCX 壓縮 | 備註 |
|------|-------------------|----------|------|
| **DS1: Remastered (DSR)** | 完整（DS1 是原始目標） | DEFLATE | **最乾淨**——檔案易存取,無 Oodle |
| **DS3** | 完整 | DEFLATE | 乾淨;UXM 解包一次 |
| **Sekiro / Elden Ring** | ER **實驗性匯入**（FLVER、anim、navmesh）;尚無 ER *匯出* | **Oodle**（需 `oo2core` DLL） | 最重;Oodle 是 Wine/Windows 皺褶 |
| DS2 | 部分 | — | 支援最少 |

**建議:** 從 **DS1 Remastered 或 DS3** 起。首跑避開 Elden Ring/Sekiro——Oodle 解壓需專有 `oo2core_*.dll`（Wine/Windows）,證明管線時可避的摩擦。

---

## 3. 讓檔案可讀（多半原生）

FromSoft 遊戲把資料存在大 `bhd`/`bdt` 對（dvdbnds）+ `*.bnd.dcx` 容器。兩條路:

**路線 A — 讓 soulstruct 讀（原生,首選）。** `soulstruct` 在 Python 裡解 DCX、走 BND/BHD/BDT/TPF。對 DSR/DS3,把 soulstruct-blender 的 **Game Directory** 指向安裝目錄,它直接導覽 archive——多數 map/chr/obj 內容無需另外解包、無 Wine。

**路線 B — 一次性批量解包（僅在需要時）。** 若某遊戲主 archive 必須先展開:
- **DS1 PTDE** → **UDSFM**（Unpack Dark Souls For Modding）——patch exe 讀鬆散檔。*（DSR 通常不需。）*
- **DS2 / DS3 / Sekiro / ER** → **UXM Selective Unpacker**。
- **Yabber** 解個別 `bnd`/`bhd`/`tpf`（非巨型 dvdbnds）。

這三個是 .NET/Windows 工具 → 走 **Wine**,或在 **Windows 分割區**（雙系統）跑一次,再讓 Manjaro soulstruct-blender 指向解好的資料夾。優先路線 A;只有 soulstruct 直接看不到檔案時才用 B。

---

## 4. 安裝 soulstruct-blender（原生 Manjaro）

1. Blender **4.1+**（[01] 已有）。soulstruct-blender 跟 Blender 4.1–5.0。
2. 從 [github.com/Grimrukh/soulstruct-blender/releases](https://github.com/Grimrukh/soulstruct-blender/releases) 下最新 release zip。它附 **`io_soulstruct_lib`**（正確版本的 `soulstruct` + `soulstruct-havok`）——與 `io_soulstruct` add-on 一起裝進 Blender 的 scripts/add-ons 資料夾（release README 給確切路徑;因為有 lib 資料夾,是手動複製、非標準安裝器）。
3. 在 Preferences → Add-ons 啟用 **`io_soulstruct`**。
4. 在 add-on 的 **General Settings**:設 **Game**（如 DSR/DS3）、**Game Directory**（安裝目錄）、與 **Image Cache Directory**（抽出貼圖快取為 `.tga`/`.dds` 處）。

---

## 5. 匯入（實際工作）

在 Soulstruct 面板:
- **Map Piece** 匯入 → 靜態建築/prop/地形塊。*這是你的 MVP 資產。* Map piece 用靜態 posing（無骨架）→ 直上 [04]。
- **Object (OBJBND)** 匯入 → props;貼圖從對應 map 貼圖資料夾拉。
- **Character (CHRBND)** 匯入 → mesh + **FromSoft armature + weights** → [07] 重定向路。

**貼圖:** FLVER 匯入時啟用「import textures」。Soulstruct 找 TPF（在 FLVER 的 BND 或 map 的貼圖資料夾）,快取進你的 Image Cache Directory 為 `.tga`/`.dds`。所以你連同 mesh *免費*拿到來源貼圖——餵給 [03]（channel-repack 到 True PBR / legacy、BC 壓縮）。

**材質:** Soulstruct 讀定義各 FLVER 材質的 **MTD**（DS1/DS3）/ **MATBIN**（ER）,建忠實 Blender node tree,且——關鍵——把緊密打包的 FLVER UV 層正確指派到具名層。你不用手動拆 UV。

---

## 6. 交給模型移植脊椎

一個 **map piece** 帶材質 + 貼圖進 Blender 後:
1. **[02] §2** — 校準 transform。FromSoft 用**公尺**;轉到 Skyrim Z-up/−Y、對 vanilla 尺縮放。FromSoft→Skyrim 常數記一次,所有 DS 資產重用。
2. **[03]** — 其 TPF 貼圖 → `.dds`（Compressonator）、channel-map 到你選的 profile。
3. **[04]** — NifTools 匯出 → `NiTriShape` 靜態 `.nif` + convex/box 碰撞。**原生。**
4. **[05]/[06]** — `StaticSpec.Model` + `package` → 遊戲內。

DS map piece 是整個資產層最低摩擦的端到端證明——完全原生 Manjaro。

---

## 7. 踩坑（DS 專屬）

- **Oodle（僅 ER/Sekiro）** — 那裡 DCX 用 Oodle;需 `oo2core_*.dll`（從遊戲複製,Wine/Windows）。DSR/DS3 用 DEFLATE → 無此問題。又一個從 DSR/DS3 起的理由。
- **Scale** — 公尺;cm-vs-m 坑不咬（不像 UE）,但仍對尺校準（[02]）。
- **Map piece 是靜態 posed** — 對靜態完美;部分「植物/建築部件」靠那靜態姿勢,別期待綁骨。
- **角色需重定向** — FromSoft 骨架 ≠ Skyrim 骨架;那是 [07] 的牆,非靜態 MVP 的事。
- **不支援 ER 匯出** — 你能實驗性*匯入* ER 但不能 round-trip;對*移出*到 Skyrim 無關（你只需匯入）。
- **`soulstruct-havok`** 在 lib 裡 — 僅在你之後想拉 FromSoft *動畫*時相關（另一條管線,survey [05]）。

---

## 8.「完成」長什麼樣

- soulstruct-blender 裝好,Game + Game Directory + Image Cache 設好。
- 一個 **DS map piece** 帶材質 + 快取貼圖匯入,**原生、無 Wine**。
- Transform 校準（FromSoft→Skyrim 常數記下）,交給 [04] → 遊戲內 Skyrim 靜態。

---

### 來源
[soulstruct-blender（GH Grimrukh — 安裝/lib、FLVER+TPF+MTD/MATBIN 匯入、map pieces、Blender 4.1–5.0）](https://github.com/Grimrukh/soulstruct-blender) · [soulstruct-blender README](https://github.com/Grimrukh/soulstruct-blender/blob/main/README.md) · [Yabber（GH JKAnderson — bnd/bhd/tpf/dcx、非 dvdbnds）](https://github.com/JKAnderson/Yabber) · [UnpackDarkSoulsForModding（Nexus #1304）](https://www.nexusmods.com/darksouls/mods/1304) · [Souls Modding Wiki — Game Engine & File Formats](http://soulsmodding.wikidot.com/game-engine-file-formats)。2026-06-09 確認。
