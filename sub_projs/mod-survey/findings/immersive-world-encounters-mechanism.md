# 這個 mod 做什麼 + 怎麼運作

← [immersive-world-encounters](immersive-world-encounters.md)

## 1. 這個 mod 做什麼 + 怎麼運作（機制總結）

IWE 是一個**腳本化路邊遭遇**包：你在野外、路上、城裡走動時，會「自然」撞見一段有劇情的小場面——犯人押送隊、雙人決鬥、爭吵、酒館賭酒、Forsworn 圍攻 Vigilant、Left for Dead 倖存者、賞金獵人、商隊腳夫、傷者求救等等。每個遭遇都是一段**會演出的小戲**（NPC 走位 + 對話 + 計時），不是單純丟幾個敵人。

它**完全寄生在 Skyrim 原版的 Story Manager 事件框架上**，沒有自己的 SMEN event root（`smtree` 對這個 esp 回報 `0 event roots`——所有節點都是原版 root 的 additive child）。骨架是：

```
原版 SM event root（WEQuests / WIChangeLocation* / WITavernQuestNode* / DLC2WE…）
   └─ IWE 加掛的 StoryManagerBranchNode（WE_SetteRandomBranch / WE_SetteQuests…，帶條件分流）
        └─ StoryManagerQuestNode（WE_SetteRoads / WE_SetteRandom / WE_SetteFactions…，帶權重 + 條件）
             └─ 觸發一個 encounter Quest（WE_Sette*）
                  ├─ Quest aliases：把演員從 LeveledNpc 隨機填進來、travel marker、TRIGGER、Hold/陣營偵測 alias
                  ├─ QF_ fragment script + WEScript（共用控制器）：推進 stage、起 Scene、清場
                  ├─ AI Package：用原版 Travel template，target = quest alias 的 marker
                  ├─ Scene（SCEN）：phase 序列，動作 = Dialog / Package / Timer 交織
                  └─ Dialogue INFO：用 CTDA 條件（GetStage / GetIsAliasRef / HasKeyword / GetEquipped / GetIsVoiceType）做出反應性對白
```

**選取 → 生成 → 演出** 的生命週期：

1. **選取**：玩家移動觸發原版 World Encounter / Change-Location 事件 → SM 沿 branch/quest node 樹下走，逐條比對節點條件（位置、Hold、陣營、時段、隨機權重）→ 命中一個 IWE quest node → 起對應 quest。多樣性靠**很多並列的 quest node**（37 個 SMQN）+ branch 分流（7 個 SMBN）+ 每個 node 的條件/權重。
2. **生成**：quest 啟動 → aliases 把演員**在執行期從 LeveledNpc 隨機填入**（全 mod 只有 30 個 PlacedNpc 靜態擺放，但有 422 個 NPC base + 65 個 LeveledNpc list——絕大多數演員是 runtime 隨機，這就是「同一個遭遇每次長得不一樣」的來源）。
3. **演出**：QF fragment 推 stage，起 Scene；Scene 的 phase 用 Package 動作把 NPC 走到 marker、用 Timer 控節奏、用 Dialog 動作播對白；對白 INFO 再用 CTDA 依「誰被填進來、玩家穿什麼、任務進度」分歧。死亡/完成 → ShutDownStage 清場。

**量級**（`dump` 記錄普查）：Quest 148、Scene 56、Package 488、NPC 422、DialogResponses 1409 / DialogTopic 582 / DialogBranch 201、LeveledNpc 65 / LeveledItem 52、StoryManagerQuestNode 37 / BranchNode 7、Outfit 31、FormList 50、GlobalShort 57（大量 runtime 旗標）。

---

