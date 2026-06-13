# Sofia Follower 解碼分析（2026-06-13）— 隨從擴充參考

做 Sofia 擴充 / 隨從框架時的施工參考。從 `SofiaFollower.esp`（v2.51，635KB，**只抽 esp、不碰 BSA**，Mutagen overlay 解碼）拆解出的架構與對 ModForge 的對應。

**一句話結論**：Sofia 沒有用到任何 ModForge 做不出的機制——它是 ModForge 已落地能力（scene-banter 在場偵測 + GLOB 狀態 + 小型 controller quest + 對話 condition）的**規模化組合**。擴充 Sofia / 自製隨從，ModForge 直接夠用;Sofia 是最佳模板。

## 記錄普查（esp 群組統計）

| 群組 | 數量 | 群組 | 數量 |
|------|------|------|------|
| Quests | 30 | DialogTopics | 239 |
| Scenes | 28 | **DialogINFOs（總）** | **1135** |
| Packages（AI） | 54 | Globals | 57 |
| Npcs | 9 | Factions | 3 |
| Spells | 9 | MagicEffects | 6 |
| VoiceTypes | 4 | FormLists | 15 |
| Classes | 6 | CombatStyles | 5 |
| Worldspaces | 3 | Cells（blocks） | 1 |

Masters：Skyrim.esm + Update.esm。前綴 `JJ`（作者 tag）。

## 五個可複用架構 pattern

### ① 小型 controller-quest 星座（不是單一巨型 quest）
約 20 個 **SGE（StartGameEnabled）、`type=None`、無 stage** 的 quest，每個只當「一個模組的 dialogue / script / scene 宿主」：
`JJSofiaIdleDialogue` / `JJSofiaDialogue` / `JJSofiaMainQuestDialogue` / `JJSofiaSidequestDialogue`（對話宿主）、`JJSofiaScripts` / `JJSofiaVariables`（腳本 + 全域容器）、`JJSofiaBattleCommands` / `JJSofiaCastSpell` / `JJSofiaBardSongs` / `JJSofiaGiveGift` / `JJSofiaWardrobe` / `JJSofiaRelationship`（行為模組）。
→ **ModForge 對應**：`QuestSpec`（StartGameEnabled + 無 stage + 當 dialogue host）。**啟示：功能拆成多個小 quest,別塞一個。**

### ② 每情境一個「comment」quest + scene（= 在場偵測 banter）
`JJSofia{Nazeem,Carlotta,Braith,Lars,Nelkir,Taarie,Endarie,Guard}Comment`——各**非 SGE、2 個 alias（Sofia + 目標 NPC）**、靠 scene 在玩家靠近該 NPC 時觸發吐槽。
→ **ModForge 對應**：**已落地的 `MFSceneBanterController` autoStart + `SceneSpec` 在場偵測**（triggerDistance/LOS/cooldown）。Sofia 就是把這個 pattern 複製 N 份。「隨從看到 X 就評論」ModForge 完全能生成。

### ③ 狀態全走 GLOB（聲望/行為追蹤的現成藍圖）
- 旗標：`JJSofiaHasMetPlayer` / `JJSofiaMainQuestStage` / `JJSofiaShouldSandbox` / `JJSofiaWitnessDragon`
- 讀玩家穿著驅動吐槽：`JJPlayerOutfitType` + `JJIsBadOutfit/GoodOutfit/MageOutfit/HeavyArmour/Revealing/CriminalOutfit`
- 鏡像玩家 18 技能：`SkillOneHanded` … `SkillSpeech`（技能相關評論）
- MCM 可調：`SofiaCommentFrequency` / `SofiaCatchUpDistance` / `SofiaCatchUpEnabled` / `SofiaCombatStyleIndex`
- 腳本暫存：`JJTempBool/Float/Int/Index`
→ **ModForge 對應**：`GlobalSpec`（GLOB builder 已落地）。**這就是身份系統「③ 聲望/行為追蹤」該怎麼做**：GLOB 記錄玩家行為/狀態（HasMetX、計數器、玩家穿著/技能）→ 對話與行為 condition 在 GLOB 上開閘。

### ④ 真 journal 任務（用到 quest-stage + objective-marker）
- `JJSofiaWeddingCeremony`：SideQuest、**6 alias / 6 stage / 1 objective**（婚禮線）
- `JJSofiaTrackingMarker`：Misc、2 stage、**1 objective + marker**（找回隨從）
- `JJSofiaLeadTheWay`（2 stage）、`JJSofiaDrunk`（6 stage）
→ **ModForge 對應**：`StageSpec`（startUpStage + 推進）+ **`objectives[].targets[]`（quest-markers,2026-06-13 剛做）**。

### ⑤ 玩家裝備驅動對話
把玩家當前裝備類型讀進 `JJPlayerOutfitType` GLOB → 對話 condition 分歧（評論你穿什麼）。
→ **ModForge 可補的便利**：目前要自組 CTDA;可考慮加「condition on 玩家穿著/技能」捷徑。

## 做 Sofia 擴充時的 ModForge 施工法

| 想加的東西 | ModForge 生成方式 |
|-----------|------------------|
| 新「看到某 NPC/地點就吐槽」 | 一個非 SGE quest + 2 alias（隨從 + 目標）+ `SceneSpec` autoStart（在場偵測 + cooldown）+ 對話 INFO |
| 旅途閒聊 / 情境台詞 | dialogue host quest + 多條 hello/idle INFO,condition 在 GLOB/位置/時間上開閘 |
| 好感度 / 聲望 | GLOB 計數器（行為觸發 +1）+ 對話/scene condition gate（仿 `JJSofiaRelationship`） |
| 任務後感想 | condition 在對應 vanilla/自訂 quest 的 `GetStage` |
| 多隨從互評 | 兩個隨從都是同 scene quest 的 alias,scene 對話互相點名 |
| 婚禮/支線 | `StageSpec` 多階段 + `objectives[].targets[]` 指向 alias（已落地） |

**語音**：所有對話想法共通——ModForge 的 voice pipeline（`voiceTemplates[]` + `voicelines`，2026-06-13 實機確認）可給自訂台詞配音;無語音時假設玩家裝 Fuz Ro D'oh。

**踩坑提醒（沿用既有鐵律）**：scene actor 必須是同 quest alias;在場偵測用 `autoStart.gateGlobal` 不要用 scene-level conditions;dialogue 在 dense 事件上要 conditions 才不劫持原版。

## 解碼方法備忘（記憶體安全）
Linux 限制單 process 記憶體、超限會被終止 → **只 `unrar e` 抽單顆 635KB esp,不碰 78M/34M 的 BSA**;Mutagen `CreateFromBinaryOverlay`（lazy)讀 635KB esp 安全。要更深(看實際 INFO 文字/scene 對白)可再抽,但 BSA 與 1G 級 archive 一律不解。
