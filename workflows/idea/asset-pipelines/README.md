# 資產管線研究（External-Asset Pipelines）

ModForge 目前是**記錄層**生成器（Mutagen 寫 ESP）＋ 既有的**資產搭便車**管線（`model`/`sounds`/`package` 引用並打包使用者自備的 `.nif`/`.dds`/`.wav`，見 [../external_assets.md](../../../docs/external_assets.md)）。本資料夾調查「ModForge 還缺、但想做的五條資產管線」的**可行性與計劃**——把外部資產真正變成 Skyrim SE 可用素材的工作流。

調查日期 2026-06-08，五份報告各自做了 2026 年現況的 web 調研、Linux/Proton 可行性、逐步工作流、ModForge 整合點與 MVP。**個人單人遊玩用途**貫穿全部——他遊資產轉檔自用合法，但**轉出的資產不得發布**（使用者已確認）。

**狀態更新 2026-06-13：** #01 voice **已 in-game 確認**——真 F5-TTS 克隆嗓音在自訂 NPC 上實機播出（`ModForgeVoiceTest.zip`）。落地路徑：`voiceTemplates[]`、`npcs[].voiceTemplate`、`voiceLine`、`voicediag`、`voicelines`、TTS wrapper shell-out、Wine `xWMAEncode.exe`、native `.fuz` writer、loose `Sound/Voice/...` 輸出。真模型設定要點見 CLAUDE.md voice 段（Blackwell→torch cu128、F5 `ref_text=""` 自動轉寫 ref、xWMAEncode 吃 24kHz、空 cell NPC 墜落）。仍待：lip（FaceFX）、loose-`.wav` fallback 實機、Fish-S2。其餘 asset pipelines 仍是研究/計劃。

| # | 主題 | 一句話結論 |
|---|------|-----------|
| [01](01-voice-cloning-fuz.md) | **語音克隆 → `.fuz`** | ModForge 核心結構已落地，能從 built plugin 的 INFO 規劃/生成 loose voice assets；`.fuz` writer 與 xWMA Wine 路徑已驗。剩餘牆是真模型設定、`.lip`（FaceFXWrapper/Wine 或跳過）、實機確認。**已展開子工作流 → [voice-clone/](voice-clone/README.md)**。 |
| [02](particle-vfx/README.md) | **粒子 / 視覺特效** | 分兩層：**EFSH 特效著色器是純記錄層**（貼圖+數值，無 mesh）＝低成本高價值首選；**粒子 `.nif` 是牆**（無程序生成、Blender 不能匯出）只能 NifSkope 改或抄現成 mod。「Effect Seeker」不存在（指 Apply Visual Effect/Director's Tools）。外部 VFX 工具**無法**匯出 Skyrim 粒子，只能貢獻貼圖。**已展開子工作流 → [particle-vfx/](particle-vfx/README.md)**。 |
| [03](03-3d-model-import.md) | **3D 模型匯入 → `.nif`** | **靜態物件接近全自動**（甜蜜點）；蒙皮角色半自動（卡綁骨/重定向）。解包：DS（soulstruct-blender，最乾淨）＞ WuWa（FModel/.NET）＞ Genshin（加密，須 Proton 端 3DMigoto dump）。**PyNifly 只有 Windows**——Linux 用 NifTools addon ＋ ck-cmd(Wine)。**已展開子工作流 → [model-porting/](model-porting/README.md)**。 |
| [04](map-scene/README.md) | **地圖 / 場景移植** | 使用者最感興趣。**每個來源引擎的關卡都正好是 `{資產, transform}` 實例清單＝Skyrim placed refs**，所以核心問題化簡為「產出擺放清單＋轉幾何」。**FromSoft MSB 用 C# in-process 直讀**（與 ModForge 同棧，零 Wine）＝決定性首選。內景先做；外景卡 heightmap/LOD（ModForge 已知缺口）。**已展開子工作流 → [map-scene/](map-scene/README.md)**。 |
| [05](animation/README.md) | **動作 → `.hkx` 資產** | 使用者新增。**動作四層**（clip/skeleton/behavior graph/events），難點在「讓 behavior graph 認得你的 clip」。**OAR（Open Animation Replacer）條件式替換＝純資料夾+JSON，ModForge 可直接生成**＝最高槓桿。Linux：**serde-hkx**（native 轉檔）＋ **Pandora**（取代 Nemesis 的 native behavior engine）解掉兩道歷史牆；剩 Blender→hkx 匯出（PyNifly 只有 Windows）是唯一的牆。**已展開子工作流 → [animation/](animation/README.md)**。 |

**已展開子工作流**（上表 0X 是概覽；超標的已長成各自的步驟化子工作流）：

| 子工作流 | 由哪條概覽展開 | 內容 |
|----------|----------------|------|
| [voice-clone/](voice-clone/README.md) | [01](01-voice-cloning-fuz.md) | 語音克隆 → `.fuz` 的逐步施工計畫（README + 01~06 步驟檔）。 |
| [model-porting/](model-porting/README.md) | [03](03-3d-model-import.md) | 外部 mesh → Skyrim `.nif` 的逐步施工計畫（README + 01~10 步驟＋解包來源檔）。 |
| [map-scene/](map-scene/README.md) | [04](map-scene/README.md) | 外部關卡 → Skyrim cell/worldspace（README + layout-extraction / geometry / workflow-modforge）。 |
| [animation/](animation/README.md) | [05](animation/README.md) | 動作 → `.hkx`（README + havok-blender / integration-layer / linux-workflow-modforge）。 |
| [particle-vfx/](particle-vfx/README.md) | [02](particle-vfx/README.md) | 粒子 / VFX（README + efsh-record-layer / particle-nif-wall）。 |

---

跨主題共識、工具矩陣、優先序與下一步 → [cross-cutting.md](cross-cutting.md)
