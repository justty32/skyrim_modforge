# 調查／解碼踩坑（investigation）

← [INDEX](../../INDEX.md)｜本工作流：[decode/](decode/README.md) 解碼參考檔 · [session-log](session-log.md) 進度｜跨工作流共通踩坑見 [common/gotchas](../common/gotchas.md)、功能開發類見 [feature-dev/gotchas](../feature-dev/gotchas.md)

逆向 vanilla 記錄、覆寫 vanilla WRLD/CELL 時踩到的坑。`[[...]]` 連 Claude memory。

---

- **vanilla nif 路徑必驗證** [[vanilla-nif-paths-must-be-verified]]：假路徑 → 隱形物件（無報錯）。下例/放置用 vanilla model 路徑前，務必用 Mutagen overlay 對 Skyrim.esm 驗證存在。
- **override vanilla WRLD（Tamriel）** [[worldspace-override-must-carry-topcell]] [[worldspace-override-map-render-fields]]：override 整筆取代記憶體中的 WRLD（last-wins，缺欄位用引擎預設、非繼承），故 `CopyWorldspaceEnv` 要忠實帶。**地圖渲染三欄位**：EDID（地形貼圖 atlas 路徑用 `Textures\Terrain\<EDID>\`，缺→**白圖但有高度**）+ RNAM（×8455 LOD 大物件 `(FormID,世界座標)` 可移植，缺→**破圖**）+ TNAM/UNAM。**永不帶 OFST**（11400 個 uint32 是 Skyrim.esm 絕對檔案偏移量，跨檔=引擎 seek 垃圾→破圖；SSE runtime 自重建，省略安全）。除錯法：byte-parse vanilla vs 輸出 WRLD 逐 subrecord diff。**陷阱**：多欄位同一 commit 加會搞混誰造成什麼，靠 `git show` 確認該 build 實含哪些欄位，別信前一 session 的文字描述。
