# 資產管線研究（External-Asset Pipelines）

ModForge 目前是**記錄層**生成器（Mutagen 寫 ESP）＋ 既有的**資產搭便車**管線（`model`/`sounds`/`package` 引用並打包使用者自備的 `.nif`/`.dds`/`.wav`，見 [../external_assets.md](../../../docs/external_assets.md)）。本資料夾調查「ModForge 還缺、但想做的五條資產管線」的**可行性與計劃**——把外部資產真正變成 Skyrim SE 可用素材的工作流。

調查日期 2026-06-08，五份報告各自做了 2026 年現況的 web 調研、Linux/Proton 可行性、逐步工作流、ModForge 整合點與 MVP。**個人單人遊玩用途**貫穿全部——他遊資產轉檔自用合法，但**轉出的資產不得發布**（使用者已確認）。

**狀態更新 2026-06-13：** #01 voice **已 in-game 確認**——真 F5-TTS 克隆嗓音在自訂 NPC 上實機播出（`ModForgeVoiceTest.zip`）。落地路徑：`voiceTemplates[]`、`npcs[].voiceTemplate`、`voiceLine`、`voicediag`、`voicelines`、TTS wrapper shell-out、Wine `xWMAEncode.exe`、native `.fuz` writer、loose `Sound/Voice/...` 輸出。真模型設定要點見 CLAUDE.md voice 段（Blackwell→torch cu128、F5 `ref_text=""` 自動轉寫 ref、xWMAEncode 吃 24kHz、空 cell NPC 墜落）。仍待：lip（FaceFX）、loose-`.wav` fallback 實機、Fish-S2。其餘 asset pipelines 仍是研究/計劃。

| # | 主題 | 一句話結論 |
|---|------|-----------|
| [01](01-voice-cloning-fuz.md) | **語音克隆 → `.fuz`** | ModForge 核心結構已落地，能從 built plugin 的 INFO 規劃/生成 loose voice assets；`.fuz` writer 與 xWMA Wine 路徑已驗。剩餘牆是真模型設定、`.lip`（FaceFXWrapper/Wine 或跳過）、實機確認。 |
| [02](02-particle-vfx.md) | **粒子 / 視覺特效** | 分兩層：**EFSH 特效著色器是純記錄層**（貼圖+數值，無 mesh）＝低成本高價值首選；**粒子 `.nif` 是牆**（無程序生成、Blender 不能匯出）只能 NifSkope 改或抄現成 mod。「Effect Seeker」不存在（指 Apply Visual Effect/Director's Tools）。外部 VFX 工具**無法**匯出 Skyrim 粒子，只能貢獻貼圖。 |
| [03](03-3d-model-import.md) | **3D 模型匯入 → `.nif`** | **靜態物件接近全自動**（甜蜜點）；蒙皮角色半自動（卡綁骨/重定向）。解包：DS（soulstruct-blender，最乾淨）＞ WuWa（FModel/.NET）＞ Genshin（加密，須 Proton 端 3DMigoto dump）。**PyNifly 只有 Windows**——Linux 用 NifTools addon ＋ ck-cmd(Wine)。 |
| [04](04-map-scene-porting.md) | **地圖 / 場景移植** | 使用者最感興趣。**每個來源引擎的關卡都正好是 `{資產, transform}` 實例清單＝Skyrim placed refs**，所以核心問題化簡為「產出擺放清單＋轉幾何」。**FromSoft MSB 用 C# in-process 直讀**（與 ModForge 同棧，零 Wine）＝決定性首選。內景先做；外景卡 heightmap/LOD（ModForge 已知缺口）。 |
| [05](05-animation-pipeline.md) | **動作 → `.hkx` 資產** | 使用者新增。**動作四層**（clip/skeleton/behavior graph/events），難點在「讓 behavior graph 認得你的 clip」。**OAR（Open Animation Replacer）條件式替換＝純資料夾+JSON，ModForge 可直接生成**＝最高槓桿。Linux：**serde-hkx**（native 轉檔）＋ **Pandora**（取代 Nemesis 的 native behavior engine）解掉兩道歷史牆；剩 Blender→hkx 匯出（PyNifly 只有 Windows）是唯一的牆。 |

---

## 跨主題的關鍵共識（Cross-cutting findings）

**1. ModForge 的角色一致：記錄層＋編排，不自造資產位元組。**
五條管線都落在同一架構：ModForge 生**記錄 / 設定檔 / 資料夾結構**，並像 shell-out Papyrus 編譯器、xLODGen 那樣 **shell-out 外部工具**（Blender headless、ck-cmd、texconv、serde-hkx、Pandora、TTS）。這正是 IDEAS.md §14 講的「與 Mutagen 記錄層平行的資產層軸」。**不要自造 nif/hkx/粒子 writer**（xLODGen 態度）。

**2. 共享的 Linux 資產骨幹**（影響全部五條）：

| 角色 | Linux-native ✅ | Wine ⚙️ | Windows-only ❌（牆） |
|------|----------------|---------|---------------------|
| DCC | **Blender**（headless `--background --python`） | — | — |
| 解包 | **SoulsFormats / FModel / AssetRipper（.NET）** | — | 3DMigoto（Genshin，Proton） |
| 模型→nif | **NifTools addon**（靜態） | ck-cmd, Outfit Studio, NifSkope, SSE NIF Optimizer | **PyNifly**（最佳但只有 Windows） |
| 貼圖→dds | **Compressonator** | texconv | — |
| hkx 轉檔 | **serde-hkx（Rust）** | HavokBehaviorPostProcess, hkxcmd | — |
| behavior engine | **Pandora（.NET）** | FNIS（將就） | Nemesis（Wine 有 thread-race，**建議棄用**） |
| 記錄探查 | — | **xEdit/SSEEdit, xLODGen** | — |
| 語音 | GPT-SoVITS/F5/XTTS/RVC（CUDA） | xWMAEncode, FaceFXWrapper | — |

**反覆出現的牆：PyNifly 只有 Windows**（粒子#02、模型#03、動作#03/#05 都撞到）。蒙皮 mesh 匯出與 Blender→hkx 匯出是兩處需要 Windows VM 或 fragile Wine-Blender 的點。靜態物件、記錄層、轉檔則全部 Linux 可行。

**3. 兩個對既有 baseline / 文檔的修正建議**（待使用者確認後再改 IDEAS.md，本資料夾僅研究，不動維護鏈）：
- **IDEAS.md §14**：「PyNifly / ck-cmd」在 Linux 上應為 **NifTools addon / ck-cmd**（PyNifly 只有 Windows）。並建議材質目標走 **True PBR**（CS baseline 已含），讓 glTF→Skyrim 貼圖映射變成乾淨的 channel-repack。
- **IDEAS.md §11-C baseline**：動作整合在 Linux 上應 **Nemesis → Pandora**（Nemesis 在 Wine 下 thread-race 常失敗；Pandora 原生跨平台且吃 Nemesis 格式）。

**4. 結構先行驗證。** 使用者無法自己跑遊戲（記憶 `ingame-test-workflow`），所有管線的 MVP 都先做**結構驗證**（xEdit / `*diag` 探 esp、確認檔名/路徑/transform 合理）再交手動 MO2/Proton 實機。錯路徑＝隱形物件/無聲音、無報錯（記憶 `vanilla-nif-paths-must-be-verified`）；MO2 重裝會還原手 patch 的檔（記憶 `mo2-reinstall-reverts-manual-pex`）。

---

## 建議優先級（價值 / 工程量）

跨五條管線，按「最快證明價值 × 最貼合 ModForge 現有強項」排序：

1. **#02 EFSH 特效著色器** — 純 Mutagen 記錄＋既有貼圖打包，零 nif 依賴，立刻給玩家新特效。**最低成本高價值，先做。**
2. **#01 語音 pipeline polish** — 結構已落地；下一步是真 TTS 參考音/模型、`voicelines --plan` 對照、MO2/Proton 實機播放、lip tier 決策。核心優勢仍是 ModForge 的 FormID→檔名映射。
3. **#04 地圖移植 MVP（DS1 MSB → 內景 cell）** — 使用者最想要。先做**只有 layout 的 smoke test**（refs 指向 vanilla mesh）證明座標轉換，再換真 mesh。`importscene` 用 SoulsFormats in-process（同棧、零 Wine）。
4. **#05 動作 OAR 生成器** — `animations[]` → OAR 資料夾+config.json＋IDLE/scene 串接；先做 replacer/單一 OAR submod。最高槓桿的「整合層」自動化，但前置依賴 Linux hkx 工具鏈（serde-hkx/Pandora）就緒。
5. **#03 靜態模型匯入** — 是 #04 的 mesh 子步驟；可與 #04 合併推進（先 DS map-piece 靜態物件）。蒙皮角色與 #05 的 Havok 牆綁一起，最後做。

> #03 與 #04 共用「mesh→nif（含碰撞）」這一步——一起推進最省力（#04 的 layout 轉換 ＋ #03 的 mesh 轉換 ＝ 完整移植）。#05 與 #03 蒙皮路徑共用 Blender 重定向骨架。三者形成一個 Blender-中心的資產層叢集。

---

## 下一步

除 #01 voice 已部分落地外，這些仍主要是**研究與計劃**。要落地任一條時，依 CLAUDE.md Workflow 1（增量改 code → 實機 → 補 CODE_MAP/文檔 → commit），並把選定的 MVP 切片當第一個 It.N。建議從上面優先級 #1（EFSH）或 #3（DS1 MSB 內景，使用者最感興趣）起手；voice 則優先補真模型、lip 與實機驗證。

*狀態：研究完成 2026-06-08；voice 核心整合 2026-06-12 部分落地。五份報告為 web 調研＋ModForge 既有能力交叉分析；標註的不確定處（`.lip`-on-Wine、Genshin 加密、heightmap、exact 座標 handedness、PyNifly-Wine）需落地時實測確認。*
