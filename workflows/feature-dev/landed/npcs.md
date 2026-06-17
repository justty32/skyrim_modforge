# 已落地 — NPC / packages

← [landed index](README.md)｜對應 [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md)

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

- **radiant 演出 package — alias target/location（C組 #2，2026-06-17，offline + byte 待驗）**：package 的 target/location 槽 ref 支援 `alias:<name>`（→ target `PackageTargetAlias{Alias}` / location `LocationFallback{AliasForReference}`）+ `aliasLoc:<name>`（→ location `LocationFallback{AliasForLocation}`），對 package 的 **in-spec `ownerQuest`** 解 alias index。讓 radiant 演出 package 在「alias 填好的 actor/location」上動作（travel→aliasLoc:Dungeon、escort target→alias:VIP）。共用解析 `Generator.Build.Packages.AliasRefs.cs`（`TryParseAliasRef`/`TryResolveAliasIndex`）接 `MakeLocationSlot`（threading `packageEd`）+ `WireDeferredTargets`；validate `PkgSlotRef`（需 in-spec ownerQuest + alias 存在）。Mutagen shape 反射驗證（`PackageTargetAlias`；`LocationTargetRadius.LocationType.AliasForReference/AliasForLocation`）。**8 測綠**，example `radiant_package_spec.json`。**radiant 鏈完整：alias 填 actor/location（#7/#8）→ package 演出（#2）→ 計數 objective（#9）。** ⚠ AliasFor* 選擇 + PackageTargetAlias byte 待主力機 xEdit 比對真 radiant package（WAIT_USER）。

**record builders（npc 域）**
- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。
- **NPC patch（override vanilla NPC）**（in-game 確認 2026-06-13）：`npcPatches[]` override 既有 NPC 的 `Packages` 等（AI Overhaul 核心，如 Carlotta 留在家）。headless 解 localized Name 的字串牆解法見 memory [[headless-vanilla-strings-provision]]；輸出英文名 inline、non-localized。`examples/npc_patch.json`、`npcdiag`。注意 USSEP/load-order 衝突。
