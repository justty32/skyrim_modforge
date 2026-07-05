# plan — 北方不死院 → Skyrim worldspace

← [README](README.md)

## 目標

DS1 北方不死院（`m18_01_00_00`）在 Skyrim 裡**能 coc 進去、看得到、走得動**。長期：管線可重跑 → 換張地圖只換 map ID。

## 素材盤點（本機 DSR v1.04，`~/games/Dark.Souls.Remastered.v1.04-GoldBerg/`）

| 檔 | 內容 | 用途 |
|----|------|------|
| `map/MapStudio/m18_01_00_00.msb` | 全部 part 的 placement（map piece / object / enemy / collision，含 transform）| → ModForge placements 的來源 |
| `map/m18_01_00_00/m*B1A18.flver.dcx` ×43 | map piece 靜態幾何（FLVER）| → NIF |
| `map/m18/*.tpfbhd/bdt` | 貼圖分卷（TPF 內是 DDS）| → 直取 DDS |
| `map/m18_01_00_00/h*/l*.hkxbhd/bdt` | 高/低精度 Havok 碰撞（DS 版 Havok，**不能直搬**）| **碰撞管線的輸入**（FromSoft 手工優化的專用碰撞低模；抽幾何 → 重生 Skyrim 碰撞，不用視覺 FLVER 硬算）|
| `map/m18_01_00_00/*.nvmbnd.dcx` + `.mcg/.mcp` | NVM navmesh + AI 圖 | 參考；Skyrim NAVM 另生（programmatic navmesh 已驗）|
| `obj/*.objbnd.dcx` ×752（全遊戲）| 門/雕像/籠子等 | P2/P3 選用 |
| `chr/` | 敵人模型（骨架/動畫牆極高）| P3 用 Skyrim 生物替代優先 |

## 架構：全程走 ModForge spec 管線

```
MSB ──(extractor)──> ModForge JSON spec（placements + 每 map piece 一個自訂 STAT）
FLVER/TPF ──(extractor)──> glTF + DDS ──(model-converter 反向)──> NIF
                                          └→ spec assets/ → package → esp + loose
```

- **extractor 選 C#**：[SoulsFormats](https://github.com/JKAnderson/SoulsFormats)（MIT、C#）直接讀 DCX/BND/FLVER/TPF/MSB，與 ModForge 同生態；獨立 console 專案放本夾 `extractor/`，不進 ModForge.Core（sub_projs 鐵律：不讓工具長特例）。備援：WitchyBND 手動解包。
- **glTF→NIF 是 model-converter 的既定反向缺口**：現有 `nif2gltf/`（Python）已有 BSTriShape 完整 parser 可當鏡子反寫。靜態 mesh（無 skin）先行。
- **座標/尺度**：DS 1 unit = 1m、Y-up；Skyrim 1m ≈ 70.03 units、Z-up。轉換統一在 extractor 出 spec 時做（glTF 保原尺度，placement scale/rot 換算進 spec）。

## 技術牆（風險排序，P0 spike 逐一驗）

1. **碰撞**（walkable 的前提；**路線已定案 2026-07-05：A→B→C 順序試**）：
   - **輸入＝DS 自帶 h/l 碰撞網格**（不用視覺 FLVER）。巧合紅利：bhk shape 內部存 Havok 公尺（Skyrim units ÷69.99）、DS 本來就公尺制 → 碰撞頂點近乎 1:1，只有渲染 mesh 要 ×70。
   - **A（首選）凸分解繞開 MOPP**：MOPP 只有凹網格需要；V-HACD 把碰撞網格拆成凸包串 → `bhkListShape` + `bhkConvexVerticesShape`（免 MOPP）。全程自寫程式碼、離線可重跑。風險＝樓梯/拱門的凸包數量與精度，spike 實測；局部不行可手補 box。
   - **B（保底）LE MOPP 工具鏈 + wine**：NifUtilsSuite/ChunkMerge（內嵌 Havok 2010，真算 MOPP，LE `bhkPackedNiTriStripsShape`）→ SSE NIF Optimizer 轉 `bhkCompressedMeshShape`。vanilla 品質保證，代價＝wine 下兩個黑盒。
   - **C（最後）Blender + PyNifly**：SSE 靜態網格碰撞支援度未驗，且為碰撞引入 Blender 大依賴，性價比低。
2. **FLVER 材質 → BSLightingShaderProperty**：diffuse/normal/specular 對映、DS normal map 慣例（G 反轉？）、alpha/雙面 flag。錯了頂多醜，不擋走。
3. **NIF 寫出器正確性**：以 nif2gltf parser 反向 + 對 vanilla nif byte-diff 驗（repo 已有 `landdiag`/byte-diff 方法論）。
4. 體量：43 塊 map piece 是小數目，不是牆；obj/chr 才是，全推遲。

## 分階段

- **P0 spike「一塊石頭」**（✅ 全數實機收官 2026-07-05）：**路線 A 實機定案**——單 hull 直掛（cube）與 **57-hull `bhkListShape` 不包 Mopp**（m0046）都能撞能站。過程修掉 gltf2nif 四個引擎級 bug（NiFooter 缺失→heap 損毀、共面 hull 靜默丟棄→地板破洞、BSTriShape 尾 u32 缺失→循序讀全盤位移、bhk 鏈前向引用→null link CTD），方法＝crash log 定位 + 對 vanilla 逐 byte（SFarmhouseSilo/Basket01/Bucket01）。貼圖第一輪漏抽 `m19_wall_13`（**教訓：tpf 分卷要全掃**，m18_0003 才有）已補齊。目視確認 5/5 mesh 全渲染——m0046 本體＝一面牆組件（主牆+窗緣飾條+側邊條），「房間感」屬鄰塊（m0045 等），P1 全量擺放後才成形。已知 P2 事項：DS 多層貼圖混合材質目前只取第一層；lightmap 未接。
- **P1「空殼院」**：extractor 讀 MSB → 全 43 塊按 transform 擺進小 worldspace（`SmallWorld`，平 LAND 沉到樓下當保底）；碰撞可先只做地板大件；coc 進去逛一圈。
- **P2「能走能看」**：完整碰撞 + programmatic navmesh + 照明/天氣（lighting pipeline 已驗；陰鬱多霧 IMGS/LGTM）+ 少量關鍵 obj（大門、電梯以 Skyrim door/activator 替代優先）+ map marker。
- **P3「有生命」（可選）**：敵人以 Skyrim 生物 leveled 替代（chr 移植不做）；篝火 = ACTI + dispatcher（已有全套經驗）；Oscar 事件用 quest/dialogue/scene 重現。
- 每階段落地物 = extractor 可重跑輸出 + 一個 spec；離線結構驗證 → 主力機實機（acceptance gate 照舊）。

## 待定決策

| 決策 | 傾向 | 定案時機 |
|------|------|---------|
| worldspace vs interior cell 群 | worldspace（院子有天空；SmallWorld + LAND 保底）| P1 |
| ~~碰撞路線~~ | ✅ 已定案（2026-07-05）：輸入＝DS h/l 碰撞網格；A 凸分解 → B LE-MOPP+wine → C PyNifly 依序試 | ~~P0~~ 剩 A 的實測 |
| glTF→NIF 放哪邊 | model-converter 反向後端（Python，鏡射 nif2gltf）| P0 |
| 資產移植 vs Skyrim 資產重砌 | 移植（本專案的意義所在）；重砌只當個別缺件備援 | — |

## IP

僅本機個人使用；**任何管道都不發佈**移植資產。repo 只 commit extractor 原始碼、spec、文檔；`extracted/`、`out/`、一切 FLVER/DDS/NIF 產物 gitignore。
