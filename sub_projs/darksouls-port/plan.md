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
| `map/m18_01_00_00/h*/l*.hkxbhd/bdt` | 高/低精度 Havok 碰撞（DS 版 Havok，**不能直搬**）| 只當參考；Skyrim 碰撞另生 |
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

1. **碰撞**（walkable 的前提）：DS 的 hkx 是舊版 Havok、SSE 是 64-bit Havok 2010→2015 重打包，不可直轉。選項：① Blender + PyNifly 匯出 bhkCollision（對靜態 mesh 支援度要驗）② ck-cmd ③ 先 bhkNiTriStripsShape 這類簡單 shape（效能差但小場景可接受）。**P0 就要定案**。
2. **FLVER 材質 → BSLightingShaderProperty**：diffuse/normal/specular 對映、DS normal map 慣例（G 反轉？）、alpha/雙面 flag。錯了頂多醜，不擋走。
3. **NIF 寫出器正確性**：以 nif2gltf parser 反向 + 對 vanilla nif byte-diff 驗（repo 已有 `landdiag`/byte-diff 方法論）。
4. 體量：43 塊 map piece 是小數目，不是牆；obj/chr 才是，全推遲。

## 分階段

- **P0 spike「一塊石頭」**：挑 1 塊小 map piece → FLVER→glTF→NIF + DDS → 放進 vanilla 測試 cell（`placements`）→ 實機**看得到、站得上去**。證三件事：mesh 轉換、貼圖、碰撞。❗碰撞方案在此定案。
- **P1「空殼院」**：extractor 讀 MSB → 全 43 塊按 transform 擺進小 worldspace（`SmallWorld`，平 LAND 沉到樓下當保底）；碰撞可先只做地板大件；coc 進去逛一圈。
- **P2「能走能看」**：完整碰撞 + programmatic navmesh + 照明/天氣（lighting pipeline 已驗；陰鬱多霧 IMGS/LGTM）+ 少量關鍵 obj（大門、電梯以 Skyrim door/activator 替代優先）+ map marker。
- **P3「有生命」（可選）**：敵人以 Skyrim 生物 leveled 替代（chr 移植不做）；篝火 = ACTI + dispatcher（已有全套經驗）；Oscar 事件用 quest/dialogue/scene 重現。
- 每階段落地物 = extractor 可重跑輸出 + 一個 spec；離線結構驗證 → 主力機實機（acceptance gate 照舊）。

## 待定決策

| 決策 | 傾向 | 定案時機 |
|------|------|---------|
| worldspace vs interior cell 群 | worldspace（院子有天空；SmallWorld + LAND 保底）| P1 |
| 碰撞路線 ①②③ | spike 驗完再說 | **P0** |
| glTF→NIF 放哪邊 | model-converter 反向後端（Python，鏡射 nif2gltf）| P0 |
| 資產移植 vs Skyrim 資產重砌 | 移植（本專案的意義所在）；重砌只當個別缺件備援 | — |

## IP

僅本機個人使用；**任何管道都不發佈**移植資產。repo 只 commit extractor 原始碼、spec、文檔；`extracted/`、`out/`、一切 FLVER/DDS/NIF 產物 gitignore。
