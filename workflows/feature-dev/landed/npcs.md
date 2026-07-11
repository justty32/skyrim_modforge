# 已落地 — NPC / packages

← [landed index](README.md)｜對應 [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md)

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

- **radiant 演出 package — alias target/location（C組 #2，2026-06-17，offline + byte 待驗）**：package 的 target/location 槽 ref 支援 `alias:<name>`（→ target `PackageTargetAlias{Alias}` / location `LocationFallback{AliasForReference}`）+ `aliasLoc:<name>`（→ location `LocationFallback{AliasForLocation}`），對 package 的 **in-spec `ownerQuest`** 解 alias index。讓 radiant 演出 package 在「alias 填好的 actor/location」上動作（travel→aliasLoc:Dungeon、escort target→alias:VIP）。共用解析 `Generator.Build.Packages.AliasRefs.cs`（`TryParseAliasRef`/`TryResolveAliasIndex`）接 `MakeLocationSlot`（threading `packageEd`）+ `WireDeferredTargets`；validate `PkgSlotRef`（需 in-spec ownerQuest + alias 存在）。Mutagen shape 反射驗證（`PackageTargetAlias`；`LocationTargetRadius.LocationType.AliasForReference/AliasForLocation`）。**8 測綠**，example `radiant_package_spec.json`。**radiant 鏈完整：alias 填 actor/location（#7/#8）→ package 演出（#2）→ 計數 objective（#9）。** ⚠ AliasFor* 選擇 + PackageTargetAlias byte 待主力機 xEdit 比對真 radiant package（WAIT_USER）。

**record builders（npc 域）**
- **NPC 外貌配方 + capturedNpcs[] 消費（Phase 1，IN-GAME 確認 2026-07-11）**：`NpcSpec` 外貌配方欄（female/weight/height/bodyTint(QNAM)/hairColor/faceTexture/headParts/tintLayers/faceMorphs/faceParts）＋擷取器 `capturedNpcs[]` → `ExpandCapturedNpcs` 鑄 NpcSpec＋擷取點 ACHR。實測：吸 Mirabelle Ervine → build → **分身在學院庭院原地出現**；built esp vs vanilla 本尊逐欄一致（faceMorph 18-slot 映射實機對照全中）。**兩個關鍵刻度**：faceMorph index↔`NpcFaceMorph` 具名欄同序（NAM9 檔案序，映射表在 [plans/captured-npcs-consumption.md](../../plans/captured-npcs-consumption.md)＋鎖定測試）；tint `value`＝引擎原生 **0–100**（Build ÷100 餵 Mutagen 0–1 視圖）。advisory 不消費：base/dead/activeEffects/perk rank/hairColor rgb。**Phase 2（FaceGeom/facetint 烘焙）未做——自訂臉灰/暗臉是已知界線**；capture 帶 mod 注入 perk 會拉 master（發佈前要拔）。`CapturedNpcsTests.cs` 15 測。
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。
- **NPC patch（override vanilla NPC）**（in-game 確認 2026-06-13）：`npcPatches[]` override 既有 NPC 的 `Packages` 等（AI Overhaul 核心，如 Carlotta 留在家）。headless 解 localized Name 的字串牆解法見 memory [[headless-vanilla-strings-provision]]；輸出英文名 inline、non-localized。`examples/npc_patch.json`、`npcdiag`。注意 USSEP/load-order 衝突。
