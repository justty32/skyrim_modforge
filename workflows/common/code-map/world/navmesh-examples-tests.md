# CODE_MAP.world — Navmesh（Example・Tests）

← [CODE_MAP.world](../CODE_MAP.world.md)

## Navmesh（續：Example・Tests）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Example | `examples/navcut_spike_spec.json` | **T2.0 證偽實驗**（白漫大街 A/B 對照：同樣的 NPC/package/marker/告示牌，只差一顆 navcut box）。14 個座標全部**讀 Skyrim.esm 的 navmesh 挑出來**（marker 不會貼地，猜 z ＝ patrol 靜默失效）|
| Example | `examples/navmesh_noop_spike_spec.json` | **P0 證偽實驗**：整份 esp **只有** 10 張 vanilla NAVM 原樣搬過來（Bannered Mare 內裝 ＋ 白漫外景 (5,-2)/(5,-3)）。沒 NPC 沒擺放沒腳本＝失敗只有一個可能成因。`esl:false`（ESL 安全性是另一顆未知數 U7，不混進來）|
| Example | `examples/navmesh_patch.json` | **P3 實機 spike**：從 Bannered Mare 真實 boundary edge 推導 64-unit 平台，append 4 vertex/2 triangle，NPC 在新舊兩側 marker 間巡邏；連續跨 seam＝PASS |
| Tests | `PrimitiveTests.cs` | 20 條：預設＝vanilla 觸發箱配方、color/opacity 可寫、type 四名稱＋生數字、sphere 補三軸＋不等值警告、省略＝無 XPRM、collisionLayer 有寫/沒寫、ACHR 上 validate＋build 雙重擋、缺 bounds／零 bounds／未知 type／opacity 出界四條 validate、**以及 navcut 共用 helper 後仍吐原本的 CollisionMarker 黃箱配方**（重構行為不變的釘子）|
| Tests | `NavCutTests.cs` | 記錄形狀（base/layer 49/Box/黃色/0.15/temporary）、size＝全尺寸、padding 三軸外脹、`navCut` 的 bool/物件 JSON 三形、validate 5 條、**離線＝零 navcut**；RequiresSkyrim：auto 裁大牆（`autoNavCuts` **預設 `true`**，2026-07-12 T2.0 實機 PASS 後翻回）、跳過雜物、尊重 `false`、`autoNavCuts:false` 改成警告、`navCuts[].placement` 包 OBND |
| Tests | `NavmeshCheckTests.cs` | 自建內裝預設沉默＋`warnEmptyCells` 開了才講、每 cell 只警告一次、總開關、**無 master ＝ 完全沉默**；RequiresSkyrim：白漫街上站好＝沉默／離網格＝警告／浮空＝警告／刻意 off-stage 可 `navmeshCheck:false` 豁免／大牆未裁＝警告／有 navcut ＝閉嘴／雜物永不吵；**spike spec 本身必須零警告＋剛好 1 顆 navcut** |
| Tests | `NavmeshOverrideTests.cs` | **無 master ＝零記錄零警告**、validate 3 條；RequiresSkyrim：同 FormKey ＋ record flags(0x40000) ＋ 掛在 vanilla cell override 下、**逐 triangle/vertex 比對 vanilla（含 EdgeLink 三欄、grid blob、divisor、door tri、min/max）＝一個索引都沒動**、**零 NAVI 記錄**、外景 6 張全搬且 WRLD override 帶 EDID/RNAM/**FULL**/TopCell(0x40400)、**無 OFST**、`navmesh` 只挑一張＋同 cell 列兩次只搬一次、空 cell 要警告。**U10 clobber（合成 plugin，離線可跑）**：假 master 帶 cell+NAVM ＋ 假 patch override 同張 → 警告點名該 plugin＋mesh；只有 owner 在場＝不警告；`warnNavmeshClobber:false`＝閉嘴仍照 build |
| Tests | `NavmeshPatchTests.cs` | 純幾何：validate 凹形／錯 link mode、CW 正規化、fan 新新 adjacency、新舊 seam 雙向、grid/bounds、無 seam mutation-free、offline 零產出；RequiresSkyrim：Bannered Mare 真 NAVM append-only、298→302 vertex／318→320 triangle、舊 index/vertex 全不動、只一條舊 EdgeLink 改、NAVI 零記錄 |
