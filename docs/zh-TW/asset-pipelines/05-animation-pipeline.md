# 動作 → Skyrim SE 資產管線

← 索引：[README.md](README.md) · 相關：[IDEAS.md §14](../IDEAS.md)（Havok =「那道牆」）、memory `scene-playidle-recipe`、[03-3d-model-import.md](03-3d-model-import.md)（rigged-mesh 交接）

**研究日期：** 2026-06-08。在 Manjaro Linux（Blender native、SSE 透過 MO2/Proton）上、供個人/單人遊玩，把任意一段動作 clip（FBX/BVH/mocap/AI 生成）變成一個*可觸發的 Skyrim SE 資產*。焦點在於**轉檔＋整合管線**——這正是使用者的切入點：動作*內容*本身已不再稀缺（免費 pack、AI 動作生成、手機 mocap）；缺的是**把一段 clip 變成可用資產的工作流。**

**信心度說明：** 這是 Skyrim modding 裡最脆弱、最受版本影響的一塊。記錄層（ModForge 的本行）是決定性的；Havok 層則是靠一堆受版本影響的社群工具勉強撐起來的閉源 SDK 逆向工程。不確定處會在內文標註。**對 Linux 來說的頭條好消息：** 歷史上最難的幾道牆現在已能 native 解決——*behavior-patch 引擎*由 **Pandora**（.NET，跨平台）攻克、*hkx 轉檔*由 **serde-hkx**（純 Rust）攻克。剩下的一道牆是 Blender→hkx 的*匯出*步驟（最佳工具 PyNifly 只有 Windows）。

---

## 1. Skyrim 動作堆疊——一個「動作資產」到底是什麼

一個 Skyrim「動作」是**四層**，而光是把檔案丟進資料夾什麼都不會發生，除非有更高的一層去引用它。核心心智模型：

1. **clip — `.hkx`（Havok 二進位）。** 一段動作，放在 `Meshes\actors\character\animations\...` 底下：隨時間變化的逐骨 transform ＋ **annotations**。Skyrim 只認 **root Z-translation ＋ X/Y-rotation**（其餘 root motion 在匯入時被剝除）。
2. **skeleton — `skeleton.nif` ＋ `skeleton.hkx`。** 骨架階層 ＋ rest pose，`NPC <name> [tag]` 命名。一段動作是*針對某個 skeleton* 製作的；骨名/階層不符 → T-pose/亂掉。一段動作 ＋ 它要播放的 skeleton **必須是同一個 rig。**
3. **behavior graph — Havok behavior `.hkx`（最難的部分）。** 狀態機（`0_master.hkx`、`defaultmale.hkx`…）決定一段 clip *何時*播放、轉場、條件。**守門人。** 一段 graph 從不引用的 clip 就是死重量。這些東西「不是設計來給人改的」——這正是 FNIS/Nemesis/Pandora 存在的原因（它們*替你 patch graph*）。
4. **animation events / annotations。** 具名事件（`weaponSwing`、idle tag），graph 會對它們作出反應；引擎/Papyrus 觸發它們（`Debug.SendAnimationEvent`、`PlayIdle`）。**一個 IDLE 記錄本質上就是 graph 已經暴露出來的一個具名 handle**——這也是為什麼 ModForge 既有的 `PlayIdle` *只能*對 vanilla behavior 本來就在驅動的 vanilla IDLE 生效。

**直白地說：** 內容是第 1 層；*可播放性*活在第 3 層。「新增一個動作」的整個難處在於**讓第 3 層去引用你的第 1 層檔案**，而不必手改一個沒有文件的二進位狀態機。§5c 裡的每個框架存在的目的，都是為了自動化這一件事。

---

## 2. Havok 格式之牆

- **閉源 SDK**（Havok 專屬，在 Microsoft 收購後撤下）。所有工具不是逆向二進位，就是包裝舊 SDK。
- **版本敏感。** Skyrim 的動作用 Havok **`hk_2010.2.0`**（已確認；PyNifly 曾必須*修掉*一個 bug——它對 Skyrim 用了 Fallout 4 的 `hk_2014` class hash）。**錯的 Havok class hash → CTD 或 T-pose。** FO4 = `hk_2014`；Skyrim LE/SE = `hk_2010`。
- **packfile/tagfile，32-bit vs 64-bit。** Skyrim LE 的 hkx = 32-bit（win32）；SE 的 hkx = 64-bit（amd64）。不可互換。經典路徑：在 LE/32-bit 製作，再轉成 64-bit 給 SE。

**工具（Linux 狀態）：**

| 工具 | 用途 | Linux？ |
|---|---|---|
| **hkxcmd**（figment） | hkx↔XML、hkx→KF；Havok 2010 但**無法寫 amd64/SE**——只支援 LE | Windows；Wine；legacy |
| **ck-cmd**（Caprica） | 主力：`importanimation`（FBX→hkx）、`convert` XML→hkx（win32 **和** amd64）、exportanimation；包裝 Havok SDK | Windows；Wine，難搞 |
| **hkxconv**（ret2end） | SE（amd64）hkx → XML（hkxcmd 缺的方向） | Windows |
| **serde-hkx / `hkxc`**（SARDONYX-sard） | **純 Rust、跨平台** 的 `hk_2010.2.0`（反）序列化器；雙向 win32↔amd64↔XML | **✅ Native Linux** |
| **HavokBehaviorPostProcess.exe**（Bethesda，CK Tools） | 官方 LE→SE：`--platformamd64` 把 32-bit→64-bit 重寫 | Windows；Wine |
| **Havok Content Tools 2014** | 正規但已停擺的 Maya/Max exporter | Windows；abandonware——避開 |
| **HKXPack** | Java hkx↔XML（較舊） | JVM；已被 serde-hkx 取代 |

**Linux 重點：** 歷史上最脆弱的依賴（Windows hkx 轉檔器）現在已由 **serde-hkx（`hkxc`）** native 涵蓋轉檔/packfile。ck-cmd 的 `importanimation` 仍是最豐富的單發工具，但只有 Windows/Wine。

---

## 3. 務實的 Blender → Skyrim 動作路徑（2026）

**Blender exporters：**
- **PyNifly**（BadDogSkyrim）——目前最佳；**直接 import/export `.hkx` 動作**（FO4/SE/LE），且自 2025 年起**直接以二進位寫出 skeleton+animation hkx，不需 hkxcmd**；修掉了 Blender 5.0 的 layered-action bug；尊重 export FPS。**但只有 Windows**（Nifly/Bodyslide 的 native 層），需 Blender 4.4+，且在 4.4 SE 上 kf/hkx 匯出有未解決的 issue（#384）——即使在 Windows 上也不是萬無一失。**Linux 上最大的摩擦點。**
- **io_scene_niftools**——較舊；nif/kf 可以，但**現代 SE hkx 動作不行**；動作方面已被 PyNifly 取代。
- **Bethesda Animation Tools /「Bethesda Havok」/ armaToHKX / jgernandt 的 blender-hkx / opparco 的 io_anim_hkx**——社群 addon，傾向**鎖定 Blender 版本**，並且需要先把 SE/amd64 降轉成 win32/Oldrim。

**典型社群鏈（以及哪些已被淘汰）：**
```
animate/import on the Skyrim skeleton in Blender
  → export .hkx (PyNifly direct)   [Windows]
       OR  export FBX → ck-cmd importanimation → .hkx   [Windows/Wine]
  → ensure Havok hk_2010, win32 first
  → HavokBehaviorPostProcess --platformamd64  (win32→amd64)
       OR  serde-hkx hkxc  (win32→amd64, Linux-native)
```
**脆弱性：** 每個方塊都會因版本不符而壞掉（PyNifly↔Blender、2010-vs-2014 hash、win32-vs-amd64、鎖在古老 Blender 上的舊 addon）。**已淘汰/避開：** HCT 2014、純 hkxcmd 輸出 SE。**2026 可行：** PyNifly（Windows）做 direct hkx；serde-hkx（Linux）做轉檔；ck-cmd（Windows/Wine）做 FBX→hkx 匯入。

---

## 4. Mocap & AI 動作匯入——retarget 問題

手機 mocap / AI 生成器輸出的是*它們自己*骨架上的 **BVH/FBX/glTF**（Mixamo、Rokoko、SMPL、UE5 Mannequin…）。要弄到 Skyrim 的 `NPC <name> [tag]` 骨架上，是一個 **retarget**——也就是 IDEAS §13/§14 的「每個來源骨架只寫一次 retarget 規則」哲學。

**三個子問題：**（1）**骨名映射**（`mixamorig:LeftArm` → `NPC L UpperArm [LUar]`）——每個來源是決定性的，一張只寫一次的 JSON 表；（2）**比例/rest-pose** 差異（為什麼天真的複製會壞）；（3）**root motion**（Skyrim 只認 root Z ＋ X/Y-rot；in-place vs root-driven 必須調和，否則會 foot-skate）。

**工具（全部 Blender-native，可在 Linux 跑）：** **Rokoko Studio Live**（免費 retarget 面板，存 preset、重用）、**Auto-Rig Pro Remap**（最穩健，有 Mixamo/Rokoko/Xsens preset，付費）、**Blender Rigify / native bone-constraint**（免費，較手動）、**Mixamo**（先繞過去拿到一個已知骨架，retarget 一次）。

**與 ModForge 相關的洞見：** Skyrim 骨架的骨名映射是每個 provider 一份的**只寫一次產物** → *retarget* 比 *hkx 轉檔*遠遠更可自動化。

---

## 5. 讓自訂動作真的能 PLAY——整合層（真正的交付物）

三個等級，由易到難：

### (a) 替換既有動作（零 behavior 編輯）
把你的 `.hkx` 丟到一個 **vanilla 動作路徑**（例如 `...\animations\mt_idle.hkx`）。graph 已經引用該路徑 → 它就播你的動作。**優點：** 不必編 behavior、立即見效。**缺點：** 全域覆寫（每個播放那個 idle 的 actor 現在都播你的）。最簡單的勝利，完美的 MVP。

### (b) IDLE 記錄 ＋ 既有 behavior（ModForge 已經在做的）
graph 暴露出一組有限的 **idle handle / animation event。** 一個 **IDLE 記錄**（`PlayIdle` / `Debug.SendAnimationEvent`）*透過 graph 已有的 handle* 觸發一段 clip。ModForge 透過 SCEN SceneAdapter 的 `PlayIdle` fragment 來驅動這件事。**不碰 behavior 就能定址的空間 = vanilla 已經接好線的那一組**（弓、手勢、家具 idle，已解碼的 offset/IdleGive/IdleSilentBow 家族）。你**無法**用這種方式引入一個真正全新的動作類別——只能搭乘既有 handle（並且配合 (a)，替換 handle 所指向的東西）。

### (c) 透過框架的新動作（現代解答）
要在不手改 Havok behavior 的情況下**新增**動作，就用一個*替你 patch/生成 graph* 的框架：
- **FNIS**（legacy）/ **Nemesis**（你的 baseline）——從一份 mod 提供的清單生成 patch 過的 behavior `.hkx`。Nemesis 能力更強，但是一個 Windows exe（Linux 問題，§6）。
- **DAR（已淘汰）→ OAR（Open Animation Replacer）**——SKSE-plugin 框架，做**執行時依條件替換**：註冊一個資料夾的替換 clip ＋ 一組條件，OAR 在引擎內換進去。
- **Pandora Behaviour Engine+**——現代、*跨平台 .NET* 的 Nemesis/FNIS 替代品（§6）。

**對記錄層工具而言，OAR 是務實的現代解答——它的註冊純粹是資料夾 ＋ JSON，完全可生成。** 結構：
```
Data\Meshes\actors\character\animations\OpenAnimationReplacer\
  <ModName>\
     config.json                 ← {name, description}  (mod level)
     <SubmodName>\
        config.json              ← {name, description, priority, conditions[...]}
        <clip>.hkx               ← same filename as the vanilla anim being replaced
```
- submod 的 **`config.json`** 帶有 `priority`（高者勝）＋ 一個 **`conditions` 陣列**（例如 `IsActorBase` 帶 plugin/formID、`Random`、`IsEquippedType`、比較式），每個都有 `negated` ＋ `requiredVersion`。例如：透過 `IsActorBase("Skyrim.esm", 0x000007)` 把一個 idle 限制給玩家。
- OAR **以被替換 clip 的路徑/檔名來比對**，並套用條件通過、priority 最高的那個 submod。`user.json` 覆寫 `config.json`。開發者建議用遊戲內編輯器，但那份 JSON **就是**一個穩定、有文件的 schema——機器生成可行（**DAR-to-OAR Converter** 以程式化方式生成這些 JSON，證明了它的決定性）。
- OAR **需要 Nemesis 或 Pandora 跑一次**以建立 base behavior，但 **OAR 本身不加任何 behavior 編輯**——它是在其之上做執行時依條件的替換。

**ModForge 能生成 OAR 結構嗎？能——毫無疑問。** 資料夾樹 ＋ `config.json`（name/description/priority/conditions）正好就是 ModForge 產出的那種決定性的記錄＋資產產物。**最高槓桿的整合目標。**

---

## 6. 整合工具的 Linux/Proton 現實（很可能的痛點）

| 工具 | 角色 | Linux 評斷 |
|---|---|---|
| **Nemesis**（你的 baseline） | behavior-graph patch 生成（Windows exe） | **⚠️ 在 Wine/Proton 下有問題——有文件記載的 thread-race bug 會卡住/失敗。** 不要假設它在 Manjaro 上能跑。 |
| **FNIS**（`GenerateFNISforUsers.exe`） | behavior 生成（Windows exe） | **可在 Wine 下跑，但有但書**（先建好 tools 目錄、在大 loadorder 下可能看似凍住、把 Languages 砍到只剩英文）。將就可用，legacy。 |
| **Pandora Behaviour Engine+** | 現代 Nemesis/FNIS 替代品 | **✅ 最佳 Linux 選項。** Native .NET，有 Windows/Linux/macOS build，**同時吃 Nemesis 和 FNIS 格式**，**headless CLI**（`--auto_run`、`--auto_close`、`-o <out>`、`--tesv <gamedir>`）。但書：「只有 Windows 經過充分測試」（團隊建議若 native 版行為怪異就用 Proton 包 Windows build）。但它是唯一一個*為 Linux 設計*的 behavior 引擎。 |
| **OAR / DAR runtime** | 由遊戲載入的 SKSE plugin | 跑在**遊戲內** → SSE+SKSE 在 Proton 下能跑的地方它就能跑（你的 baseline）。沒有獨立的 Linux 步驟。 |
| **serde-hkx（`hkxc`）** | hkx↔XML、win32↔amd64 | **✅ Native Linux（Rust）。** |
| **ck-cmd / hkxcmd / HavokBehaviorPostProcess** | FBX→hkx、LE→SE | Windows；Wine，版本難搞。 |
| **PyNifly** | Blender 直接 hkx 匯出 | **❌ 只有 Windows。** 最難的 Linux 缺口。 |

**Manjaro 的結論：** *引擎*問題靠**把 Nemesis → Pandora**（native、讀 Nemesis 格式、headless）解決。*轉檔*問題靠 **serde-hkx**（native）。*Blender→hkx 匯出*問題在 Linux 上**沒有**乾淨的解（PyNifly 只有 Windows；Linux 可用的 Blender hkx addon 都鎖版本＋需要 win32 中間檔）。**建議：把假定的堆疊從 Nemesis → Pandora 重新定基**以求 Linux 可行性；把 Nemesis-under-Wine 當成非目標。*（這與 IDEAS §11-C 的「Nemesis」baseline 矛盾——已在 [README.md](README.md) 標註。）*

---

## 7. 端到端工作流

目標：**FBX/mocap → Skyrim skeleton → SE hkx → 以 OAR 條件式替換（或 vanilla-path replacer）出貨。** **[AUTO]** = 可腳本化 / ModForge 可擁有，**[MANUAL]** = 人/Windows/Wine，**[WALL]** = 脆弱的斷點。

1. **[AUTO]** 取得 clip（依你的切入點，超出範圍）。
2. **[AUTO，write-once]** 在 headless Blender（`blender --background --python retarget.py`）裡，用每來源一份的骨名映射 import ＋ **retarget 到 Skyrim skeleton**。**在 Linux 上跑。**
3. **[MANUAL / WALL]** **匯出成 `.hkx`。** 最佳工具 PyNifly **只有 Windows** → Windows VM/機器，或脆弱的 Wine-Blender+PyNifly，或 **FBX 匯出 [AUTO Linux] → ck-cmd `importanimation` [Wine]**。*牆 #1。*
4. **[AUTO，Linux]** 正規化 Havok：確認 `hk_2010`，用 **serde-hkx `hkxc`**（或 Wine 下的 `HavokBehaviorPostProcess --platformamd64`）把 win32→amd64。*錯的版本 = 牆 #2（T-pose/CTD）。*
5. **以其中一個等級出貨：**
   - **(a) Replacer [AUTO]：** 把 `.hkx` 放到 vanilla 路徑。立即播放，全域覆寫。
   - **(c) OAR set [AUTO]：** 生成 `OpenAnimationReplacer\<Mod>\<Submod>\config.json`（priority ＋ conditions）＋ 丟入 `.hkx`。**ModForge 可生成。** 需要 Pandora 跑一次以建立 base behavior。
6. **[MANUAL，Linux-OK]** 跑 **Pandora**（`--auto_run --auto_close -o <out>`）以（重新）生成 behavior baseline。Native Linux。*（若 behavior 已生成，純 replacer (a) 可略過。）*
7. **[MANUAL]** 遊戲內測試（你無法跑遊戲 → 只能做結構驗證；由使用者透過 MO2/Proton 測試）。

**牆：**（1）Linux 上的 Blender→hkx 匯出；（2）Havok 版本/位元數不符。

---

## 8. ModForge 整合——務實的分工

ModForge 是**記錄層（Mutagen）＋ 資產打包。**

**ModForge 擁有（決定性）：**
- **(i) IDLE 記錄 ＋ SCEN/scene 串接**，透過既有的 `PlayIdle` 機制*觸發*動作——**已出貨。** 擴充成可引用搭乘 vanilla handle 的新出貨 clip。
- **(ii) OAR 設定資料夾生成**——產出 `OpenAnimationReplacer\<Mod>\<Submod>\config.json`（name/description/priority/`conditions[]`）＋ 把 `.hkx` 打包到正確的 `Meshes\...` 路徑底下。正好是 ModForge 產出的那種記錄＋資產產物；DAR→OAR Converter 證明了決定性。**最高槓桿的新能力。**
- **(iii) Vanilla-path replacer 打包**——把使用者提供的 `.hkx` 簡單地放到扁平 MO2 zip 裡的 vanilla 路徑。
- **(iv) Shell-out 編排**——像 Papyrus/xLODGen 那樣：驅動 Blender headless（retarget）、serde-hkx（`hkxc`）、Pandora（`--auto_run --auto_close`）。

**ModForge 不擁有（shell out 或手動）：** 實際的 **Havok hkx 編碼**（serde-hkx/ck-cmd/PyNifly）、**behavior-graph patching**（Pandora/Nemesis）、**Blender→hkx 匯出**（Windows 之牆）、製作/retarget 的*判斷*（Blender；其*呼叫*是可腳本化的）。

**具體 spec/CLI 提案：**
- **新的 `animations[]` spec 區塊：** `{ source: <hkx|fbx>, sourceSkeleton: <map-id>, target: <vanilla anim path | new clip name>, ship: "replacer" | "oar", oar?: { mod, submod, priority, conditions[] }, idleRecord?: {...} }`。
  - `ship: "replacer"` → 把 hkx 打包到 vanilla 路徑。
  - `ship: "oar"` → 從 `oar.conditions[]` 生成 OAR 資料夾 ＋ config.json（盡量重用 ModForge 既有的 condition 詞彙）＋ 打包 hkx；選用地產出一個 **IDLE 記錄** ＋ scene `PlayIdle` 串接，讓它也可被腳本觸發。
- **新的 CLI verb `importanim`**（仿照 compile/xLODGen 的 shell-out）：`importanim <clip> --skeleton-map <id> --out <hkx>` → headless Blender retarget → FBX/hkx 匯出 → serde-hkx 轉成 SE/amd64。對匯出子步驟的 Windows 之牆要誠實（標註它；允許用一個預先匯出的 `.hkx` 來繞過）。
- **保持「不自己製作」：** ModForge 生成*設定 ＋ 記錄 ＋ 打包 ＋ 編排*，絕不生成 Havok 位元組。

---

## 9. MVP ＋ 踩坑 ＋ 建議

**最小的驗證切片：** 一段 mocap/FBX clip → 在 Blender 裡 retarget 到 Skyrim skeleton → 轉成 SE/amd64 hkx（serde-hkx）→ 以**單一 idle 的 vanilla-path replacer（等級 a）**出貨，*或* 以**單一 OAR submod 用 `IsActorBase(player)` 條件替換一個 idle（等級 c）**出貨。replacer 是絕對最小（不需 Pandora）；OAR submod 則是能證明 **ModForge 可生成整合層**的最小切片。若被替換的 idle 是 ModForge 已在觸發的那個，兩者也*都*可以透過**既有的 `PlayIdle` scene 機制**暴露出來——用已出貨的能力把整個迴圈閉合。

**為什麼：** 它演練了 retarget（Linux）＋ hkx 轉檔（Linux，serde-hkx）＋ ModForge 資料夾/記錄生成，同時**把兩道牆延後**（PyNifly Windows 匯出 → 一次性手動交接；OAR 避開 behavior graph）。

**踩坑（脆弱清單）：**
- **Havok 版本不符**（FO4 的 `hk_2014` vs Skyrim 的 `hk_2010`）→ CTD/T-pose。確認是 2010。
- **LE/win32 vs SE/amd64 位元數** → 格式錯 = 不播/崩潰。用 serde-hkx 或 `--platformamd64` 轉。
- **骨架不符**（名稱/階層/比例）→ T-pose/炸開/foot-skate。retarget 映射必須精準。
- **Root motion**——只認 root Z ＋ X/Y-rot；不符 → 滑動。
- **behavior-graph 之牆**——真正*全新*的動作類別（非替換）需要 behavior patching；MVP 階段就待在替換國度（a/c）。
- **Nemesis 在 Wine 下壞掉**（thread race）——**用 Pandora**（native、讀 Nemesis 格式、headless）。重新定基。
- **PyNifly 只有 Windows**——Blender→hkx 匯出沒有乾淨的 Linux 路徑；預留一台 Windows 機器/VM/Wine-Blender，或手動預先匯出 hkx。
- **無法在遊戲內測試**——只能做結構驗證；倚賴使用者的 MO2/Proton 迴圈，以及 memory 裡已知的 stale-zip/MO2-reinstall 陷阱。

**整體而言：** 蓋出 OAR-set 生成器（`animations[]` → OAR 資料夾 ＋ config.json ＋ IDLE/scene 串接）＋ 一個薄的 `importanim` shell-out（Blender retarget ＋ serde-hkx），**採用 Pandora 取代 Nemesis 作為 Linux behavior 引擎**，並把 Blender→hkx 匯出當成那唯一一道公認的手動/Windows 牆。

---

### 來源
Arcane University: Implementation of Custom Animations / CK-CMD for Skyrim / Editing Animation Skeletons · PyNifly（GH BadDogSkyrim）＋ issue #384 · hkxcmd（GH figment）· hkxconv（GH ret2end）· serde-hkx（GH SARDONYX-sard）＋ CLI Nexus #126214 · Open Animation Replacer（Nexus #92109, GH ersh1）· DAR-to-OAR Converter（Nexus #93359）· Pandora Behaviour Engine+（GH Monitor221hz, Nexus #133232）· Nemesis（Nexus #60033）· Step Mods forum（Nemesis Wine thread-race）· MO2 issue #1678（FNIS on Linux）· HavokBehaviorPostProcess guide（Nexus #2970）· Rokoko / Auto-Rig Pro Remap docs.
