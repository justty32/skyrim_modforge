# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **近期已落地（細節在 git log）**：docs/workflows 大重構（CLAUDE.md 瘦成路由器、開發流程移到頂層 `workflows/`）→ 拆檔門檻定案（docs 300 行 / workflows 8192 bytes）→ DEV-GUIDE 改被動參考、plans/specs 命名去日期 → docs/ SPEC 拆分 → **zh-TW + html 全面重譯重組（1:1 鏡像 EN，原 deferred 的 re-sync 已補完）**。語音合成已解耦為 `sub_projs/skyrim-voicegen/`、Sofia 擴充為 `sub_projs/sofia-patch/`；`gamedata` CLI + `sub_projs/game-data/` 供並行 agent 取用。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #19 Godot Worldspace Editor**：地形鏈（heightmap→VHGT/VNML→LAND）已通並驗證；godotPlacements 後端已完成。**2026-06-17 離線補上 Godot 前端物件擺放**（Place Mode + placement 筆 + box proxy + `placements.json` 匯出/匯入，4 新 .gd + terrain/world_ui/main 改；前端輸出欄位/座標已離線核對與後端 `GodotPlacements.cs` 一致）→ 「擺物件→.esp」整鏈已串，**待主力機開 Godot GUI 跑一次**（WAIT_USER）。durable 見 [sub_projs/godot-worldspace-editor/README.md](sub_projs/godot-worldspace-editor/README.md)。
  - **紋理：單層 BTXT + 多層 VTXT 後端都已做**（2026-06-17 離線）：① 單層 `worldspace.baseTexture`（LTEX ref）→ 每格四象限 BTXT base 層；② **多層混合 `worldspace.textureLayers`**（LTEX + grayscale splatmap PNG）→ 每格四象限稀疏 ATXT+VTXT alpha 層（新 `Splatmap.cs`/`Vtxt.cs`，`Generator.Build.Worldspace.cs` EmitCell 接線，`WorldspaceBaseTextureTests`+`WorldspaceSplatmapTests`，**604 全綠**）。Mutagen API 全程反射查證（`AlphaLayer{Header,AlphaLayerData:ExtendedList<AlphaLayerData{Position,Opacity,Unused}>}`、`AlphaLayer:BaseLayer`、`AlphaLayerData` 預設 null 需自建 list）。byte-verify（BTXT+VTXT position/layer 打包）待主力機 xEdit（WAIT_USER）。**前端 splat-paint 筆刷也已補上**（2026-06-17 離線）：`splat_tool.gd`/`splat_ui.gd`/`splatmap_io.gd` + terrain overlay（Splat Mode 多層 alpha 筆、active 層即時上色、8-bit 灰階 splatmap PNG 匯出含可貼 spec 片段；Y-flip/網格與後端 `Splatmap.cs` 一致）。離線無 Godot 無法 parse-check，GDScript 已逐行人工複查，**待主力機 Godot GUI 跑一次**（WAIT_USER）。B 路線（單層+多層+前端筆）整鏈離線完成。
- **model-converter sub_proj（2026-06-17 新開，🟡 載體已實作、等實機驗）**：以 `.nif`(+dds) 為中心的模型格式雙向互轉工具；**MVP 鎖＝vanilla nif→glTF 批量代理**。CLI 協議契約 [PROTOCOL.md](sub_projs/model-converter/PROTOCOL.md)（`MODFORGE_NIF2GLTF_BIN`、`--in/--out/--flat`、manifest 批量、exit 0/1/2/3；backend-agnostic）。**2026-06-17 離線自寫參考後端 `nif2gltf/`**（Python＋pygltflib，使用者選的技術棧）：手寫 Skyrim NIF（20.2.0.7/user12）靜態 mesh parser——**LE**（NiTriShape/NiTriStrips→Data，全 float）+**SSE**（BSTriShape/BSDynamic/BSSubIndex，走 BSVertexDesc offset 表+Full_Precision 自描述解碼）、NiNode 樹 transform 組合、Skyrim Z-up→glTF Y-up、含 skin/動畫→exit 3；Block Size 給每塊邊界 offset 防骨牌漂移。glTF writer 用 pygltflib（`.gltf`+`.bin`、平色 material）。**不再依賴 NifSkope**（原卡關用自寫繞過）。格式逐欄查 niftools nif.xml（非憑記憶）。**23 測綠**（writer round-trip 7／reader LE+SSE 合成 fixture 9／CLI 契約 7）。⚠️ 離線無遊戲素材，合成 fixture 只證自洽，**對真實 vanilla nif 的 byte 驗證待主力機**（SSE 半精度 offset 最需驗），見 WAIT_USER。durable 見 [sub_projs/model-converter/README.md](sub_projs/model-converter/README.md)。
