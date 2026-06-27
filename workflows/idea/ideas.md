# 模組創作想法隨記（idea 工作流入口）

← [INDEX](../../INDEX.md)

奇思妙想備忘（**不確定要不要做**——確定會做但未排程的進 [roadmap](../roadmap/README.md)）。未必有優先順序，隨時增補。**已落地功能見 [feature-dev/landed](../feature-dev/landed/README.md) 與 git log**——本檔只留「想做的事」與判斷依據（決策、鐵律、缺口）。

階梯：**idea（要不要做？）** → [roadmap](../roadmap/README.md)（會做，何時？）→ [spec](../specs/README.md)（討論後方案）→ [plan](../plans/README.md)（動工前詳規）→ build。

**子研究**：[asset-pipelines/](asset-pipelines/README.md) —— 五條「想做但還沒蓋」的資產管線（外部工具 → Skyrim 素材）可行性研究（idea-research）。

---

## 索引

| § | 想法 | 分類 | 詳情 |
|---|------|------|------|
| 1 | 擴充停止更新的隨從模組 | 隨從/NPC | [followers.md](followers.md#1-擴充停止更新的隨從模組) |
| 1b | NPC 劇情演出（Scene 驅動） | 隨從/NPC | [followers.md](followers.md#1b-npc-劇情演出scene-驅動) |
| 1c | 多重身份 / 輕量職業系統 | 隨從/NPC | [followers.md](followers.md#1c-多重身份--輕量職業系統) |
| 2 | 喜愛劇情模組的遺憾分支改版 | 敘事 | [narrative.md](narrative.md#2-喜愛劇情模組的遺憾分支改版) |
| 3 | 商隊與船隊生活 | 世界 | [world-building.md](world-building.md#3-商隊與船隊生活) |
| 4 | 異世界冒險（另開 Worldspace） | 世界 | [world-building.md](world-building.md#4-異世界冒險另開-worldspace) |
| 5 | 其他遊戲資源移植 / 引擎復現 | 世界 | [world-building.md](world-building.md#5-其他遊戲資源移植--引擎復現) |
| 6 | 在 SkyUI 基礎上擴充 UI | 工具 | [tools.md](tools.md#6-在-skyui-基礎上擴充-ui) |
| 7 | 遊戲內嵌入網頁 UI | 工具 | [tools.md](tools.md#7-遊戲內嵌入網頁-ui) |
| 8 | 程序生成的世界 | 世界 | [world-building.md](world-building.md#8-程序生成的世界) |
| 9 | 大量劇情自動生成 | 敘事 | [narrative.md](narrative.md#9-大量劇情自動生成獨立工作流) |
| 10 | 翻譯 + 插件合併 | 工具 | [tools.md](tools.md#10-翻譯--插件合併) |
| 11 | 騎馬與砍殺 in Skyrim | 世界 | [world-building.md](world-building.md#11-騎馬與砍殺-in-skyrim機制復刻) |
| 12 | 明亮美術基調 / 光照管線 | 美術 | [visuals.md](visuals.md#12-明亮美術基調--光照管線2026-06-04) |
| 13 | 通用 NPC 美化：morph 轉換 | 美術 | [visuals.md](visuals.md#13-通用-npc-美化morph-空間轉換規則2026-06-04) |
| 14 | 資產格式轉換管線（glTF → NIF） | 工具 | [tools.md](tools.md#14-資產格式轉換管線gltffbx--nif2026-06-04) |
| 15 | Blender/Unity 視覺場景編輯器 | 工具 | [tools.md](tools.md#15-unity--blender-插件作為-ck-替代視覺場景編輯器2026-06-15) |
| 16 | ESL 合併工具 | 工具 | [tools.md](tools.md#16-esl-合併工具modforge-外掛指令2026-06-15) |
| 17 | 任務節點圖 + 批量隨從反應 | 隨從/NPC | [followers.md](followers.md#17-skyrim-原版任務節點圖--批量隨從反應生成2026-06-15) |
| 18 | 隨從記憶系統 | 隨從/NPC | [followers.md](followers.md#18-隨從記憶系統任務經歷追蹤與對話更新2026-06-15) |
| 19 | Godot Worldspace Editor | 工具/世界 | [sub_projs/godot-worldspace-editor/](../../sub_projs/godot-worldspace-editor/README.md) |
| 20 | In-world 技能樹（玩家+NPC） | 隨從/NPC · 養成 | [sub_projs/inworld-skill-tree/](../../sub_projs/inworld-skill-tree/README.md) |
| 21 | 養成與戰鬥體系擴充（Keystone + 職業核心機制） | 養成/戰鬥 | [progression-combat-overhaul.md](progression-combat-overhaul.md) |
| 22 | 漂泊開拓慢活（統整 #3+#4+#8，含可行性盤點） | 世界/玩法 | [world-building.md](world-building.md#22-漂泊開拓慢活移動基地--程序生成異域--開拓經營) |
| 23 | 具名冒險者的活世界模擬（給 standalone follower 一條命） | 隨從/NPC · 世界 | [living-adventurers.md](living-adventurers.md) |
