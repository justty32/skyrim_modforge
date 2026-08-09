# 模組創作想法隨記（idea 工作流入口）

← [INDEX](../../INDEX.md)

奇思妙想備忘（**不確定要不要做**——確定會做但未排程的進 [roadmap](../roadmap/README.md)）。未必有優先順序，隨時增補。**已落地功能見 [feature-dev/landed](../feature-dev/landed/README.md) 與 git log**；已升級的題目以 roadmap/spec/plan/sub-project 為現況真相，本區只保留原始發想脈絡。

階梯：**idea（要不要做？）** → [roadmap](../roadmap/README.md)（會做，何時？）→ [spec](../specs/README.md)（討論後方案）→ [plan](../plans/README.md)（動工前詳規）→ build。

**子研究**：[asset-pipelines/](asset-pipelines/README.md) —— 五條「想做但還沒蓋」的資產管線（外部工具 → Skyrim 素材）可行性研究（idea-research）。

---

## 現役 ideas

這張表只列仍在回答「要不要做／採哪條路」的題目；「下一刀」是再次討論時最小的決策入口，不代表已承諾施工。

| § | 想法 | 分類 | 下一刀 |
|---|------|------|--------|
| 1 | [擴充停止更新的隨從模組](followers.md#1-擴充停止更新的隨從模組) | 隨從/NPC | 選第一個目標 follower 與一組情境反應 |
| 2 | [喜愛劇情模組的遺憾分支改版](narrative.md#2-喜愛劇情模組的遺憾分支改版) | 敘事 | 選目標 mod 與單一分歧點 |
| 5 | [其他遊戲資源移植 / 引擎復現](world-building/05-cross-game-porting.md) | 世界 | 選來源遊戲與「素材／玩法」範圍 |
| 6 | [在 SkyUI 基礎上擴充 UI](tools/06-skyui-extension.md) | 工具 | 在技能槽、任務框、小地圖中選一項 |
| 7 | [遊戲內嵌入網頁 UI](tools/07-ingame-web-ui.md) | 工具 | 先做 CEF 可行性 spike |
| 9 | [大量劇情自動生成](narrative.md#9-大量劇情自動生成獨立工作流) | 敘事 | 定義故事系統與 ModForge 的資源 catalog 契約 |
| 11 | [騎馬與砍殺 in Skyrim](world-building/11-mount-and-blade.md) | 世界/玩法 | 20v20 波次會戰垂直切片 |
| 13 | [通用 NPC 美化：morph 轉換](visuals.md#13-通用-npc-美化morph-空間轉換規則2026-06-04) | 美術 | 先以寫實 head asset 驗證轉換規則 |
| 14 | [資產格式轉換管線（glTF/FBX → NIF）](tools/14-gltf-to-nif-pipeline.md) | 工具/資產 | 靜態物件端到端轉換 |
| 15 | [Blender/Unity 視覺場景編輯器](tools/15-blender-unity-visual-editor.md) | 工具 | 重新判斷是否只保留離線批次用途 |
| 16 | [ESL 合併工具](tools/16-esl-merge-tool.md) | 工具 | 先證明 record/FormID 重映射，不碰 `.pex` |
| 17 | [任務節點圖 + 批量隨從反應](followers.md#17-skyrim-原版任務節點圖--批量隨從反應生成2026-06-15) | 隨從/NPC | 定義 quest-node JSON schema |
| 18 | [隨從記憶系統](followers.md#18-隨從記憶系統任務經歷追蹤與對話更新2026-06-15) | 隨從/NPC | 一個任務節點 × 一個隨從的追蹤 spike |
| 21 | [養成與戰鬥體系擴充](progression-combat-overhaul.md) | 養成/戰鬥 | 先選少量 Skyrim 化 Keystone |
| 22 | [漂泊開拓慢活](world-building/22-wandering-frontier.md) | 世界/玩法 | 據點建設垂直切片 |

## 已升級：現況不在 idea 維護

| § | 題目 | 現況真相 |
|---|------|----------|
| 19 | Godot Worldspace Editor | 已是獨立專案，見 [專案 README](../../../godot-worldspace-editor/README.md) |
| 20 | In-world 技能樹 | 已成 [sub-project](../../sub_projs/inworld-skill-tree/README.md)；原始判斷留在 [idea 頁](inworld-skill-tree.md) |
| 23 | 活世界人口框架 | 已成 [sub-project](../../sub_projs/living-adventurers/README.md)；原始願景留在 [idea 頁](living-adventurers.md) |
| 24 | 遊戲內編輯器 | 已進 [spec](../specs/ingame-scene-export-design.md)、[plan](../plans/scene-capture-bridge/README.md) 與獨立 `scene-capture-bridge`；[idea 頁](tools/24-ingame-editor.md)只保存北極星與設計憲法 |

## 已吸收／大致落地：保留編號作歷史導航

| § | 題目 | 去向／剩餘缺口 |
|---|------|----------------|
| 1b | NPC 劇情演出 | Scene 主體已落地；只剩非必要的 CAMS 鏡頭，見 [原段落](followers.md#1b-npc-劇情演出scene-驅動) |
| 1c | 多重身份系統 | MVP 已落地；後續互動應各自成新 idea/roadmap，見 [原段落](followers.md#1c-多重身份--輕量職業系統) |
| 3、4、8 | 商隊／異世界／程序世界 | 已整合成 [#22 漂泊開拓慢活](world-building/22-wandering-frontier.md)；原頁留作來源脈絡 |
| 10 | 翻譯 + 插件合併 | 翻譯已落地；未做的合併統一由 [#16](tools/16-esl-merge-tool.md) 承接 |
| 12 | 明亮美術基調／光照管線 | 主體已落地；只剩 weather/IMGS 掛 region，見 [原段落](visuals.md#12-明亮美術基調--光照管線2026-06-04) |

## Idea-research：外部資產管線

[asset-pipelines/](asset-pipelines/README.md) 是五條「想做但還沒全蓋」的獨立研究樹：語音、粒子/VFX、模型、地圖場景、動作。它們有自己的成熟度與優先序，不重複塞進上面的產品 idea 清單。
