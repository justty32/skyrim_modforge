# 已落地 — NPC / packages

← [landed index](README.md)｜對應 [CODE_MAP.npcs-packages](../../common/code-map/CODE_MAP.npcs-packages.md)｜同域：[npcs-capture.md](npcs-capture.md)（NPC 外貌配方＋capturedNpcs／`sc capp` 吸玩家）

**PACK templates（共 10）**：sandbox / sleep / travel / usemagic / patrol / follow / escort / **sittarget**（坐家具）/ **activate**（活化 lever/door）/ **eat**。

- **radiant 演出 package — alias target/location（C組 #2，2026-06-17，offline + byte 待驗）**：package 的 target/location 槽 ref 支援 `alias:<name>`（→ target `PackageTargetAlias{Alias}` / location `LocationFallback{AliasForReference}`）+ `aliasLoc:<name>`（→ location `LocationFallback{AliasForLocation}`），對 package 的 **in-spec `ownerQuest`** 解 alias index。讓 radiant 演出 package 在「alias 填好的 actor/location」上動作（travel→aliasLoc:Dungeon、escort target→alias:VIP）。共用解析 `Generator.Build.Packages.AliasRefs.cs`（`TryParseAliasRef`/`TryResolveAliasIndex`）接 `MakeLocationSlot`（threading `packageEd`）+ `WireDeferredTargets`；validate `PkgSlotRef`（需 in-spec ownerQuest + alias 存在）。Mutagen shape 反射驗證（`PackageTargetAlias`；`LocationTargetRadius.LocationType.AliasForReference/AliasForLocation`）。**8 測綠**，example `radiant_package_spec.json`。**radiant 鏈完整：alias 填 actor/location（#7/#8）→ package 演出（#2）→ 計數 objective（#9）。** ⚠ AliasFor* 選擇 + PackageTargetAlias byte 待主力機 xEdit 比對真 radiant package（WAIT_USER）。

**record builders（npc 域）**
- **livingNpcs 真外部 follower enrollment — YUA IN-GAME PASS 2026-08-10**：`uniqueActor YUA_Follower.esp:0x000800` 成功納入同一 persistent actor（無 duplicate），離場 deeds、Bannered Mare materialize、Mira rumor、fund/praise/favor、招募期間 `IsPlayerTeammate()` ownership 全過。第一輪 QA 抓到 dismiss 出門後立即 `MoveTo` 造成眼前瞬間消失；正式 handoff 改為保留 follower 自己的 dismissed package 可見步行，只有玩家已無法跟上且連續 30 秒不在 8192-unit loaded guard 內，controller 才 off-screen 回 hold 並恢復抽象 sim。第二輪 agent-bridge QA **15/15 PASS**。
- **livingNpcs generic P0–P3 — IN-GAME PASS 2026-08-10**：macro 版 `MFLivingNpcs.zip` 由 agent-bridge 無人值守跑完 **31/31 PASS**：runtime plugin、Riverwood 的 Kjeld＋Bjorn、Whiterun 的 Kjeld＋Falas、兩次 abstract tick 與 Kjeld anchor 往返輪替全過；bridge 再依 actor 名稱移到 Falas 身旁、開正常對話、從 structured state 讀出 parley 選項、依顯示文字選取，TopicInfo 確實把 favor **0→5**。可重跑 spec：[`living_npcs.qa.json`](../../../sub_projs/living-adventurers/living_npcs.qa.json)。

- **NPC inventory**：`NpcSpec.Items`（攜帶/自動裝備/死亡掉落）；`NpcSpec.essential/protected`。
- **NPC patch（override vanilla NPC）**（in-game 確認 2026-06-13）：`npcPatches[]` override 既有 NPC 的 `Packages` 等（AI Overhaul 核心，如 Carlotta 留在家）。headless 解 localized Name 的字串牆解法見 memory [[headless-vanilla-strings-provision]]；輸出英文名 inline、non-localized。`examples/npc_patch.json`、`npcdiag`。注意 USSEP/load-order 衝突。
