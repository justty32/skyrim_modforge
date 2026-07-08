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

- **vanilla unique NPC 不能用 placement 複製**（2026-07-08）：對一個帶 **Unique** flag 的 vanilla NPC base 下 `placements[]`（kind:npc）想「在別處放一個」——**引擎只保留單一實例**（原版那個），複製出的 REFR 不生成 → 場景裡看不到人（但 `GetIsID` 對話/faction override 仍作用在原版那一個身上，造成「對話有效、人卻不在」的錯覺）。要在場景放一個會現身的角色，用**新的 in-spec NpcSpec**（race+voiceType+outfit+crimeFaction，比照 `follower_vanilla_spec.json`；autoCalcStats 必配 class 見 [[autocalc-without-class-dead-npc]]）——這也正是真實 PROTEUS clone 的形態（全新 base）。ModForge 的 npcRole macro 因此分 in-spec（直接掛 package/faction）/ external（NpcPatch override）兩路。

- **macro 生成的 dialogue/quest 片段必須在編譯迴圈前展開**（2026-07-08）：`PackageCmd` 先編譯 `spec.Dialogue`/`spec.Quests` 的 TIF/QF 片段 `.psc` → 再 `Build()`。但 pass-0 macro（`npcRoles` openBarter trade topic、`livingNpcs` 互動 setGlobal、settlements…）是在 `Build()` 內才把記錄加進 spec 的——所以 macro **生成的**片段在編譯迴圈時還不存在 → 不編譯、VMAD 被靜默丟掉（症狀:選項在但點了沒反應/交易不開）。修:`Generator.ExpandMacros(spec)`（所有 pass-0 展開、idempotent）在 Package.cs 編譯迴圈**前**先跑一次，`Build()` 再跑是 no-op。新增 macro 若會生片段,務必確認它進 `ExpandMacros`。
