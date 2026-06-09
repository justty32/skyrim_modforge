# Skyrim 粒子 / VFX 匯入與重用管線

← 索引：[README.md](README.md) · 相關：既有的 MGEF/SPEL/PROJ/EXPL builders、[external_assets.md](../external_assets.md)

**研究日期：** 2026-06-08。範圍：Manjaro Linux 上個人/單人的 SSE modding、ModForge（C#/Mutagen ESP 生成器）。開發期間無遊戲內測試；Wine/Blender/NifSkope 可用。

**先講結論：** 兩個可清楚分離的層級。**記錄層**（EFSH effect shaders、ARTO art objects、HAZD hazards，以及 MGEF 內的 FormID 接線）正是 Mutagen 記錄生成器擅長之事 — 高價值、低工作量。**資源層**（`.nif` 粒子系統本身）是一道難以逾越的牆：沒有程序化生成器、沒有 Blender 匯出路徑；粒子 nif 只能用 NifSkope 製作或從既有 mod 複製，沒有別的辦法。實際可行的 ModForge 功能是「參考/捆綁既有 nif + 從 JSON 製作 EFSH 記錄」，**而非**「生成粒子」。

---

## 1. Skyrim 如何表示粒子 / 視覺效果

一個「視覺效果」= 一個指向**資源**（`.nif` + `.dds`）的**記錄**（ESP 中的資料列）。關鍵記錄類型：

| Record | Sig | 它是什麼 | 需要自訂網格？ |
|---|---|---|---|
| **Effect Shader** | `EFSH` | 一個*membrane shader*（投影到目標的網格上）+ 一個*particle shader*（sprites），由**貼圖路徑 + 數值/顏色/blend 參數**定義 | **否** — 純記錄 + `.dds` |
| **Art Object** | `ARTO` | 一個 wrapper，其 payload 是一條 **`.nif` 模型路徑**（MODL）+ type flag（DNAM：Magic Casting / Hit Effect / Enchantment Effect）。nif 持有粒子系統 | **是** — nif *本身就是*效果 |
| **Hazard** | `HAZD` | 持續性 AoE（火焰地塊、毒氣雲）：nif + spell/effect + IMAD + 音效 + lifetime/radius/limit | 通常是 |
| **Impact Data Set** | `IPDS`→`IPCT` | 依材質而定的表面命中反應（貼花、音效、impact art） | IPCT 參考 nif/effect art |

**關鍵區別（交付物）：**
- **EFSH 是純記錄。** Membrane = 帶 blend modes + 動畫 color/alpha keys 投影到目標既有網格上的貼圖；particle shader 發射貼圖的扁平 **2D sprites** — 無網格。你可以僅用一個 `.dds` + 數字打造全新的火光/霜寒微光。**記錄生成器的領域。**
- **ARTO 依賴網格。** 記錄本身很簡單（模型路徑 + type flag），只有當該路徑存在一個帶 `NiParticleSystem`/`BSStripParticleSystem` 的 `.nif` 時才有意義。ModForge 可以建立 ARTO 並捆綁/參考 nif，但無法建立 nif 的粒子內容。

**MGEF 如何參考這些**（「Visual Effects」分頁 = MGEF `DATA` 中的 FormID 欄位）：
- **Hit Effect Art** → `ARTO` · **Enchant Effect Art** → `ARTO` · **Casting Art** → `ARTO`
- **Hit Shader** → `EFSH` · **Enchant Shader** → `EFSH`
- **Image Space Modifier** → `IMAD` · **Impact Data Set** → `IPDS`
- **Light** → `LIGH` · **Projectile** → `PROJ` · **Explosion** → `EXPL` · **Hazard** → `HAZD`

一個火球的「外觀」= MGEF →（Casting ARTO + Hit ARTO + Hit EFSH）+（PROJ → 它自己的 trail nif）+（EXPL → EFSH/IMAD/light）+ 選用的 HAZD。ModForge 已能建構 MGEF/PROJ/EXPL；缺口在於 **EFSH/ARTO/HAZD + FormID 接線**。

> CK 術語 **RFCT**（Visual Effect）是一個小型記錄，把 EFSH + ARTO 配對成一個可重用單元；「apply visual effect」類工具吃 RFCT/EFSH/ARTO FormID。

來源：UESP MGEF/ARTO/EFSH/HAZD 格式頁面；CK wiki EffectShader。

---

## 2. 重用其他已安裝 mod 的粒子效果（個人使用）

**辨識你喜歡的某個效果背後是什麼：**
1. 在 **SSEEdit/xEdit**（Wine）中找到該 MGEF；讀取它的 `ARTO`/`EFSH`/`IPDS` FormID + 來自哪個 plugin。
2. 開啟該 ARTO 的 `MODL` 取得 nif 路徑（例如 `meshes\magic\firefxnimble01.nif`）。
3. 在 **NifSkope**（Wine）中檢視該 nif，確認 `NiParticleSystem`/`BSStripParticleSystem` 並讀取其 `BSShaderTextureSet` 的 `.dds` 路徑。

**參考 vs. 捆綁 — 核心取捨：**
- **參考（依賴）：** 讓你的 ARTO/MGEF 指向另一個 mod 的 nif 路徑 + 把該 mod 加為 **master**。佔用最小，但形成永久的載入順序依賴。通常是不必要的摩擦。
- **複製/捆綁（獨立）：** 把 `.nif` + 它的 `.dds` 複製進你自己的 `Meshes/`/`Textures/`（以 mod 命名的子資料夾），讓你的 ARTO 指向*你的*路徑，**不加 master**。自給自足、無載入順序風險。**建議的預設值** — 符合既有的 `model`+`package` 哲學。

**記錄與資源是獨立的 master：** 複製 *nif 檔案*永遠不會產生 master（資源不是記錄）。只有當你參考另一個 plugin 的**記錄 FormID** 時才會產生 master。乾淨的獨立配方：**複製 nif、在 ModForge 中製作全新的 ARTO/EFSH 記錄、除 Skyrim.esm 外零 master。**

> 個人使用的法律性：把另一位作者的資源複製進一個*私人、單人、永不分享*的 plugin 不是問題。

---

## 3. 製作 / 編輯粒子 nif — 那道牆

**NifSkope 是唯一實際可行的製作路徑。** 直接以 block 欄位編輯 `NiParticleSystem`/`BSStripParticleSystem` + `NiPSysData` + `NiPSysModifier` 鏈（emitters、gravity、age-death、color/size）。對*調整*既有效果（換色/縮放/換貼圖/birth-rate）可行，但從零開始很痛苦。

**Blender 匯出並不支援粒子系統。** 已從 **PyNifly** README（2026，Blender 4.4+，**Windows-only / Linux 上的 Wine**）驗證：支援 = 網格/shaders/碰撞/skinning/動畫（HKX）/connect-points — **不支援粒子系統。** 較舊的 `io_scene_niftools` 也一樣。所以你**無法**在 Blender 裡建模一個火焰漩渦並匯出成可運作的 Skyrim 粒子 nif。

**程序化生成的可行性：** 理論上 nif 是結構化的二進位，可透過 **pyffi**/**nifly** 發出，但從零打造一個*正確、引擎能接受*的粒子 nif 是一個龐大的研究專案（modifier 排序、controller 連結、shader flags、bounding data — 一個欄位寫錯 = 靜默失敗）。**誠實的結論：別建造粒子 nif 生成器。** 槓桿在於*把已知良好的 nif 的副本參數化*（換掉 texture-set `.dds`、縮放 birth rate）— 這更適合留給 NifSkope 或一個微小的 pyffi 欄位修補，而非 ModForge 核心。

---

## 4. EFSH effect shaders — 廉價的勝利，詳述

EFSH 是 100% 記錄層：貼圖路徑 + 數字，無網格。CK 把它拆成 **Membrane Shader**（投影到目標既有網格上）與 **Particle Shader**（扁平 sprites）。

**貼圖路徑欄位**（`.dds`，EFSH 唯一需要的資源）：fill/base texture、particle texture、**holes/gradient（「palette」）texture** — *CK 警告：若未定義 palette 貼圖，shader 可能靜默不渲染。*

**Membrane 參數：** source/dest blend modes + blend op；fill color 帶動畫 color keys（3 個 RGB stops）+ 在 fade-in/hold/fade-out 期間的 alpha ratio/amplitude/frequency/phase；edge color + edge falloff；fade-in/full/fade-out 時間。

**Particle 參數：** birth rate（+ramp）；lifetime（+delta）；initial speed/acceleration/rotation；生命週期內的 scale keys；color-key 動畫；flags（grayscale→color/alpha、additive）。

> CK 坑：particle-shader 的「Time」欄位是在效果持續期間內正規化成 **0–1**；membrane 的「Time」是以秒為單位。請在 spec 中記載這點。

**提議的 `effectShaders[]` spec 條目：**
```jsonc
{
  "editorId": "MFEffShFireGlow",
  "fillTexture":     "Textures/MFVfx/firefill.dds",
  "particleTexture": "Textures/MFVfx/spark.dds",
  "paletteTexture":  "Textures/MFVfx/grad.dds",   // don't omit — silent fail
  "membrane": {
    "srcBlend": "SrcAlpha", "destBlend": "One",    // additive glow
    "fillColor": [255,140,40], "edgeColor": [255,80,0],
    "fadeInTime": 0.25, "fullTime": 1.0, "fadeOutTime": 0.5,
    "alphaKeys": [{ "t":0.0,"a":0.0 },{ "t":0.2,"a":1.0 },{ "t":1.0,"a":0.0 }]
  },
  "particle": {
    "birthRate": 80, "lifetime": 1.2, "initialSpeed": 30, "acceleration": -10,
    "scaleKeys": [{ "t":0.0,"s":0.4 },{ "t":1.0,"s":1.2 }],
    "colorKeys": [{ "t":0.0,"rgba":[255,200,50,255] },{ "t":1.0,"rgba":[120,20,0,0] }]
  }
}
```
用 Mutagen（`EffectShader`）即可直接建構，只需捆綁 `.dds`（package 已處理 textures），在不碰任何 nif 的情況下提供真正全新的效果。

---

## 5.「Effect Seeker」與 VFX 瀏覽工具

**並不存在名為「Effect Seeker」的工具**（已驗證）。你可能想到的是下列其中一個真實工具：
- **Apply Visual Effect**（SE #45603）— *最接近的對應。* SKSE lesser power；輸入一個 RFCT/EFSH/ARTO 的 **FormID** 就會套用到玩家身上；可 list/clear；**隨附一個 vanilla EditorID↔FormID 的 info 檔**；SE 版本可將已套用的集合存/讀成 **JSON**。最佳的遊戲內「尋找/預覽效果」工具。
- **Director's Tools**（SE #61996）— 對 actors 施放數百種 effect shaders/visual effects + imagespace + weather。無法自動偵測卡住效果的 FormID — 先在 xEdit 找出來。
- **More Informative Console**（SE #19250）— 對你點選的任何東西顯示 FormID/EditorID/記錄細節（需要 Address Library）。
- **xEdit/SSEEdit** — 真正的「目錄」：跨已載入的 mod 過濾到 `EFSH`/`ARTO`/`IPDS`/`MGEF`。

**對你實用（無遊戲內測試）：** 離線倚靠 **xEdit（瀏覽記錄）+ NifSkope（預覽粒子 nif）**。遊戲內的套用工具是當你*能*跑遊戲時的手動驗證步驟。

---

## 6. 外部 VFX 工具互通 — 現實狀況

**沒有任何現代 VFX 工具能匯出到 Skyrim 的粒子格式。** Unreal Niagara、Unity VFX Graph/Shuriken、EmberGen、Houdini、After Effects — **沒有一個**能匯出到 Gamebryo/NetImmerse `.nif` 粒子系統。它們的架構（GPU compute、node graphs、VAT/flipbook）與 `NiParticleSystem` + `NiPSysModifier` 沒有對應關係。

**唯一能跨越的東西：flipbook/sprite-sheet `.dds` 貼圖。** 製作一張動畫貼圖（或在 EmberGen/AE 中算出一張 sprite sheet），存成 `.dds`，把它餵給某個 EFSH 的 **particle/fill texture** 或某個被複製的粒子 nif 的 texture-set。那是*唯一*合理的外部工具貢獻。

**據此定位：** Skyrim 粒子只能用 NifSkope 製作或從既有 mod 複製。外部工具貢獻的是**貼圖**，不是粒子系統。別承諾 Niagara/Unity 匯入功能 — 它不存在，也無法合理地建造。

---

## 7. 提議的 ModForge 整合（依價值/工作量排序）

全都符合既有的 `model`/MGEF/PROJ/EXPL/`package` 模式。

**① `effectShaders[]` → EFSH builder — 最高價值、低工作量。** 純 Mutagen 記錄，spec 如 §4，只捆綁 `.dds`。新效果，無 nif 牆。接入 MGEF Hit/Enchant Shader + EXPL。**最先建造。**

**② `artObjects[]` → ARTO builder — 高價值、低工作量（記錄）但受資源限制。** 簡單記錄：`editorId`、`model`（重用既有的 `model` 欄位 + 捆綁）、`type` flag。價值取決於使用者提供/複製一個真正的粒子 nif。與捆綁（④）搭配。

**③ 接入 MGEF/SPEL/PROJ — 中等價值、低工作量。** 新增選用的 MGEF FormID 欄位：`hitEffectArt`、`enchantEffectArt`、`castingArt`（→ 依 editorId 對 ARTO）、`hitShader`、`enchantShader`（→ EFSH）。讓 PROJ/EXPL 能參考新的 EFSH/ARTO。讓 ①/② 真正顯現出來。

**④ 從選定 mod 捆綁粒子 nif — 中等價值、低工作量。** 擴充 `package` 以納入一份明確的 nif+dds 清單（或來源目錄）獨立形式，外加一個 `referenceOnly` flag（把來源 plugin 加為 master）。預設 = 複製/獨立。新增一個 build 時的路徑存在性檢查。

**⑤ `hazards[]` → HAZD builder — 較低價值、中等工作量。** 串接 nif + spell/effect + imagespace + 音效 + radius/lifetime/limit；placed-hazard（PHZD）需要 worldspace/cell 系統（已有）。冷門；最後做。

**明確不建議：** 一個粒子 nif *生成器*，或任何「從 Niagara/Unity 匯入」功能（§3、§6 — 那道牆）。

---

## 8. 端到端工作流程：「mod X 中很酷的火焰漩渦 → 我的自訂法術」

1. **找到它** *（手動，xEdit）：* 定位 MGEF/ARTO/EFSH；記下 ARTO `MODL` nif 路徑 + EFSH FormID。*（選用的遊戲內預覽：Apply Visual Effect/Director's Tools。）*
2. **檢視該 nif** *（手動，NifSkope/Wine）：* 確認粒子系統；讀取其 `BSShaderTextureSet` 的 `.dds` 路徑。
3. **複製資源** *（自動）：* 把 nif + 每一個被參考的 `.dds` 複製進 `Meshes/MFVfx/` + `Textures/MFVfx/`。*（若你移動了 textures，就要修正 nif 內的路徑 — 手動 NifSkope 或一個 pyffi 腳本。）*
4. **製作記錄** *（自動，ModForge）：* 新增一個指向你複製的 nif 的 `artObjects[]` 條目；選用一個 `effectShaders[]` 做 membrane 輝光。
5. **接到法術** *（自動）：* 把 MGEF `hitEffectArt`/`castingArt` 設成該 ARTO editorId + `hitShader` 設成該 EFSH；把 MGEF 掛到你的 SPEL。
6. **建置 + 打包** *（自動）：* 發出 ESP + 捆綁 Meshes/Textures → 扁平 MO2 zip。無 master（獨立）。
7. **驗證** *（現在做結構性的，之後做遊戲內的）：* 在 xEdit 重新開啟，確認 ARTO MODL 路徑 + FormID 都能解析；確認 zip 在確切被參考的路徑上有 nif+dds。遊戲內：施放；若隱形，幾乎總是路徑錯誤（§9）。

自動：3–6。手動：1–2（探索）、步驟 3 的 nif 貼圖路徑修正、步驟 7 的遊戲內驗證。

---

## 9. MVP 建議 + 坑

**MVP：** 先出貨 **`effectShaders[]`（EFSH）+ MGEF 接線（hitShader/enchantShader）** — 唯一零 nif 依賴、完全 Mutagen、重用貼圖捆綁的 VFX 功能。其次新增 **`artObjects[]`（ARTO）+ nif 捆綁**，用於從 mod 重用。延後 **HAZD**。**絕不**嘗試粒子 nif 生成或外部 VFX 匯入。

**坑（在文件中標註）：**
- **nif/貼圖路徑錯誤 = 隱形、無報錯** — 與 memory `vanilla-nif-paths-must-be-verified` 相同。為每個 EFSH 貼圖路徑 + ARTO 模型路徑加一個 build 時的檔案存在性檢查，對照捆綁的目錄樹（warn，不要 fail）。
- **EFSH palette/holes 貼圖遺漏 = 靜默不渲染**（CK 確認）。把 palette 視為實質必填；缺失時 warn。
- **貼圖路徑存在於 nif *內部*** — 把一個 ARTO 的 nif 獨立複製是不完整的，除非它的 `.dds` 也被複製*且* nif 內的 texture-set 路徑仍能解析。最簡單的安全預設值：**在 textures 的原始相對路徑上捆綁它們**，讓未修改的 nif 能找到它們。
- **Master 依賴：** 複製資源永遠不會新增 master；參考一個記錄 FormID 才會。預設複製/獨立；只有 `referenceOnly` 才新增 master。缺 master = CTD/載入失敗；缺資源 = 隱形但能載入。
- **BSStripParticleSystem vs NiParticleSystem：** 在 ENB complex lights 下，只有 `NiParticleSystem` 會發出 ENB light；strip particles 不會。裝飾性的文件註記。
- **EFSH particle shader 只從 Actors 發射**（CK）：hit/cast-shader 的粒子不會從無生命的已放置 STATs 發射 — 對法術而言沒問題。
- **既有存檔固化：** EFSH/ARTO 是靜態記錄（套用到既有存檔沒問題，無 `.seq` 顧慮），但一個存檔上已習得的法術使用其烘焙的 MGEF；重新習得/重新裝備才能看到變更。

---

### 已驗證工具摘要（Linux/Wine）
- **SSEEdit/xEdit** — Wine ✅（主要的探索 + 驗證）
- **NifSkope** — Wine ✅（唯一實際可行的粒子製作/檢視）
- **PyNifly** — Blender 4.4+，**Windows-only（Wine）**，**不支援粒子** ⚠️
- **Apply Visual Effect / Director's Tools / More Informative Console** — 遊戲內（Proton），僅手動驗證
- **「Effect Seeker」** — 不存在 ❌
- **Niagara/Unity/EmberGen → nif** — 不存在 ❌（僅貼圖）

*Mutagen 的 `EffectShader`/`ArtObject`/`Hazard` 類別以名稱暴露各欄位，所以實作上不需要確切的 EFSH byte offsets；在敲定 builder 前，先在 xEdit 中對照一個原版 EFSH 驗證欄位語意。*
