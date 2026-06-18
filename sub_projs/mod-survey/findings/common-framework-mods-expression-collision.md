# Conditional Expressions / BOS / AOS / I Am Walking / I Am Talking

← [common-framework-mods](common-framework-mods.md)

## Conditional Expressions

本地抽取結果：`Conditional Expressions.esp` 有 1 quest、17 magic records、0 dialogue lines。主 quest 是 `CondiExp_StartMod "Quest Conditional Expressions"`；magic effects 包含 `Drunk Effect`、`Eating Effect`、`Cold Effect`、`Pain Effect`、`Sneaking Effect`、`Fatigue Effect`、`Random and Vanilla/Frostfall/Frostbite Effect` 等。

腳本機制：

- 以 quest alias 監聽玩家裝備/食用物品，設 `CondiExp_PlayerIsDrunk`、`CondiExp_PlayerIsHigh`、`CondiExp_PlayerJustAte` 等 global。
- 以 magic effects 定期檢查玩家狀態。
- 用 `MfgConsoleFunc.SetModifier`、`SetPhoneme`、`Actor.SetExpressionOverride`、`ClearExpressionOverride` 控制表情。
- 用 `CondiExp_CurrentlyBusy` 避免多個表情效果互相踩。

對我們的參考是「表情也需要狀態鎖與清理」。如果 follower dialogue 要做面部表情，應使用短時間、可恢復、帶 busy gate 的控制，不要永久覆蓋 actor expression。

## BOS

BOS 用 Data 目錄下 suffix `_SWAP.ini` 的設定檔分發 base object swap。Nexus 文件的基本格式是：

```ini
[Forms]
origBaseID|swapBaseID|propertyOverrides|chance
```

可利用場景：

- follower home 根據關係進度替換裝飾物、床、椅子、禮物、信件展示物。
- 任務或 scene 需要某些靜態物件變成可互動/可拾取版本。
- 不想 patch 每個 cell reference 時，用 base object swap 做廣泛替換。

注意：它是 base object 級別替換，適合環境/物件，不適合 narrative state 本身。

## AOS

AOS 用 suffix `_ANIO.ini` 的設定檔替換 AnimObject。Nexus 文件的基本格式是：

```ini
[ANIO]
origEDID|swapEDID
```

也支援一個原物件對多個替換物件、隨機選擇、條件 section。

可利用場景：

- 同一個喝酒 idle，Sofia 拿特定酒瓶；Lydia 拿杯子；法師拿書或卷軸。
- 表白/安慰/營地 scene 中替換手持道具。
- 搭配 OAR：OAR 換動作，AOS 換動作中的道具。

這比複製 idle/animation record 更輕量，尤其適合角色化細節。

## I Am Walking Here

本地未見檔案。外部資料顯示它是 SKSE/no-collision 類工具，核心價值是避免 NPC/follower 推擠玩家或堵住窄路。

對 follower dialogue 的意義偏品質層：

- 長 dialogue / forcegreet / home scene 中，降低 follower 擠動玩家造成 camera 或站位破壞。
- 多 follower 場景比較不容易被碰撞卡住。
- 如果沒有它，自家 action service 仍應設計站位與 `SetDontMove` / package cleanup，不能依賴玩家裝這個。

## I Am Talking Here

本地未見檔案。外部資料顯示它會在玩家對話時壓住 follower idle chatter，並標榜支援 vanilla/custom voiced followers。

對我們最重要的是設計原則：

- ambient commentary 不應在玩家正在進行重要對話時插話。
- FCO 類旅途 bark、Sofia 類吐槽、RDO 類 shared idle 都要有「dialogue busy」概念。
- 如果沒有可用 API，就在自家系統加 quest/global cooldown：進入重要 scene/topic 前設 busy，結束後清掉。
- 對 custom follower，重要劇情應使用 scene 或 high priority Hello；ambient line 要讓路。

