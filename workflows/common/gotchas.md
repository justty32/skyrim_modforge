# 共通踩坑（跨工作流）

← [INDEX](../../INDEX.md)｜各工作流專屬踩坑：[feature-dev/gotchas](../feature-dev/gotchas.md) · [investigation/gotchas](../investigation/gotchas.md)

引擎行為 / 開發流程層級的坑，不專屬任一工作流，任何人都可能撞到。`[[...]]` 連 Claude memory。

## 哪類坑記哪裡（三處 gotchas 歸類）

| 坑的性質 | 記/查這裡 |
|---------|----------|
| 引擎行為 / 開發流程，不專屬任一工作流 | **common/gotchas**（本檔）|
| 開發具體功能（SM/scene/dialogue/npc/voice…）+ 外部工具內部開發聯動（Papyrus 編譯、Wine path）| [feature-dev/gotchas](../feature-dev/gotchas.md) |
| 逆向 vanilla 記錄、覆寫 vanilla WRLD/CELL 的解碼坑 | [investigation/gotchas](../investigation/gotchas.md) |

---

- **存檔已固化**：GLOB value / scene `.seq` 只是初值，既有存檔保留 runtime 值。
- **worktree 並行** [[feature-swarm-branches]]：worktree 一律從 **stale base** 分出（持續性 harness 行為）；先離線解碼 vanilla 再下精確施工單（agent 不負責猜）、分配互斥檔案領域；整合用 cherry-pick + keep-both（同名 test class 用 `--ours` 重貼）。

- **vanilla 外部 worldspace 擺物：座標 Z 不能亂填**（2026-07-08）：Tamriel exterior 的地面高度隨地形劇烈變化，隨手填的 `position.z` 幾乎必錯——高於地面 → 靜物懸空（「天上有房子」）、map marker 懸空 → 快旅即墜落摔死；低於地面 → 埋進地裡。**先取真實地面 Z**：用 Mutagen overlay 讀一個附近 **vanilla marker/ref 的 position**（`SkyrimMod.CreateFromBinaryOverlay(SkyrimData/Skyrim.esm).EnumerateMajorRecords<IPlacedObjectGetter>().First(x=>x.FormKey.ID==0x…).Placement.Position`；`find <esm> <名> ` 拿 FormID），選**平坦開闊點**（farm/stables 凍原，如白漫馬廄 `WhiterunStablesMapMarker` 0x072879 = (18313,−10665,**−4590**)）當錨、緊湊擺放同 Z。campfire 等 Hazard 有 `DropToGround` 會自動貼地。map marker 別跟房子同 XY（會傳送進屋內）。同類：[[vanilla-nif-paths-must-be-verified]]（錯 nif 路徑→隱形）。
