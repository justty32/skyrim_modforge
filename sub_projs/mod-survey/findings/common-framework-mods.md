# Common Framework / Utility Mods

這批不做深挖，只記「可被 follower dialogue expansion 利用的機制」。本地已解壓的放在 `~/skyrim_mods/unzip`；`I Am Walking Here` / `I Am Talking Here` 目前沒有在 `~/skyrim_mods` 或 `~/skyrim_mods/unzip` 找到檔案。

## 快速表

| Mod | 本地狀態 | 核心機制 | 我們可利用的點 |
| --- | --- | --- | --- |
| Spell Perk Item Distributor / SPID | 已解壓：`Spell Perk Item Distributor-36869-7-3-0-1778353486` | SKSE plugin；用 config 分發 spell / perk / item / shout / package / outfit / keyword / faction 到 NPC | 無 patch 標記 NPC：給 follower 加 faction/keyword/spell，讓 dialogue condition、OAR、其他系統能識別 |
| Open Animation Replacer / OAR | 已解壓：`Open Animation Replacer-92109-3-1-5-1778597444` | SKSE animation replacer；用條件替換 animation，有 in-game editor | 讓特定 follower / faction / 狀態使用不同 idle、gesture、walk、interaction animation |
| PapyrusUtil | 已解壓：`PapyrusUtil AE SE - Scripting Utility Functions-13048-4-6-1705639805`；另有 `PapyrusUtil` | Papyrus native utility；StorageUtil、JsonUtil、ActorUtil、MiscUtil | 存 follower memory、外部 JSON config、actor/package override、掃描 cell NPC |
| JContainers | 已解壓：`JContainers SE`；是的，本地已經有 | JSON-based serializable data structures；array/map/form map；Lua | 複雜 dialogue state、動態資料表、外部 JSON-driven topic/state/relationship 設定 |
| Conditional Expressions | 已解壓：`Conditional Expressions-45148-1-29-1755293339`；已抽取 game-data | ESP + magic effects + scripts；用 MFG/expression override 做玩家狀態表情 | 參考「狀態驅動表情」：喝酒、吃東西、冷、痛、疲勞、潛行、隨機表情 |
| Base Object Swapper / BOS | 已解壓：`Base Object Swapper-60805-3-4-1-1752606013` | `_SWAP.ini` 以 base object 替換物件，可帶 chance/property overrides | 針對 follower home、scene set dressing、互動物件做無 ESP patch 的替換 |
| AnimObject Swapper / AOS | 已解壓：`AnimObject Swapper-75167-1-1-0-1666410165` | `_ANIO.ini` 替換 idle 使用的 AnimObject，可 random/conditional | 換對話/idle animation 中手上的杯子、書、樂器、道具；適合角色化演出 |
| I Am Walking Here | 本地未見 | SKSE plugin；防止 NPC/follower 推擠玩家或阻塞窄路 | 降低 follower 場景、走位、forcegreet 後跟隨時的碰撞干擾 |
| I Am Talking Here | 本地未見 | SKSE plugin；玩家對話中壓住 follower idle chatter | 可作為 ambient commentary 的兼容模型：重要對話期間不要讓 follower bark 插話 |

## SPID

SPID 是「無 patch 分發標記」工具。Nexus 描述它可用 config 把 spells、perks、items、shouts、packages、outfits、keywords、factions 分發到 NPC。

對 follower dialogue expansion 最有用的是 faction / keyword / spell：

- 給一批 NPC 打上 `MyMod_FollowerDialogueEligibleFaction`，之後 dialogue INFO 用 `GetInFaction` 判斷。
- 給 custom follower 加 compatibility faction，讓 GYH、OAR 或自家 action service 能識別。
- 用 invisible ability / spell 當狀態容器或 condition hook，但敘事狀態仍建議放 quest/global/script，不要全靠 SPID。
- 用 package 分發要謹慎，因為 follower framework、scene package、AI overhaul 可能搶 priority。

SPID 適合做「標記與分發」，不適合做需要 runtime 精細更新的 relationship state。

## OAR

OAR 是 animation 條件替換框架。它適合接在對話/狀態系統後面：對話模組負責設 faction、keyword、global、quest stage；OAR 負責在條件成立時換 animation。

可利用場景：

- Sofia 或其他 unique follower 專屬 idle / gesture。
- relationship rank 或 romance state 改變後，換站姿、等待姿勢、擁抱/親密互動 animation。
- 任務後受傷、喝醉、生氣、害羞等狀態，用 condition 換短 idle。
- 配合 GYH 類 action service：腳本觸發場景，OAR 負責替換更合適的 animation asset。

它通常比在 ESP 裡覆蓋大量 animation record 乾淨，兼容性也比較好。

## PapyrusUtil

PapyrusUtil 本地 readme 的重點：

- `StorageUtil`：在任意 form 或 global namespace 上存 int / float / form / string 與 list。
- `JsonUtil`：把同類資料存到外部 JSON，資料不綁 save，適合 out-of-game 調整。
- `ActorUtil`：actor package override，priority 0-100，會進 save。
- `MiscUtil`：掃描 cell object/NPC、檔案與 console 等 utility。

對 follower dialogue 的用法：

- `StorageUtil` 存每個 NPC 的輕量記憶，例如最近互動時間、是否被某 follower 認識、暫時 cooldown。
- `JsonUtil` 存可調整資料，例如 topic cooldown、mood weight、commentary pool、兼容名單。
- `ActorUtil.AddPackageOverride` 可做臨時站位/等待/看向玩家，但一定要有清理策略；`ClearPackageOverride` 會清掉其他 mod 的 override，不能亂用。
- `MiscUtil.ScanCellNPCs` 可支援「附近有誰」的情境對話。

注意：key namespace 要嚴格加 mod prefix，避免和其他 mod 的 StorageUtil key 撞名。

## JContainers

JContainers 已經在本地。它提供 Papyrus 可用的 JSON-like serializable container：`JArray`、`JMap`、`JFormMap`、`JIntMap`、`JDB`、`JValue`，也有 user directory 與檔案操作。

它比 PapyrusUtil 更適合複雜資料結構：

- dialogue state table：`follower -> topic -> lastSeenStage/lastSpokenTime`。
- relationship/mood matrix：`actor form -> trust/flirt/anger/familiarity`。
- 外部 JSON 配置：把 commentary bucket、condition weight、scene tags 做成資料表。
- 動態 lookup：例如 custom follower compatibility map、voice profile map、animation profile map。

選擇建議：簡單 per-form key/value 用 PapyrusUtil；需要 nested map/array、資料表、外部 JSON schema 時用 JContainers。

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

## 參考來源

- Nexus：Spell Perk Item Distributor - `https://www.nexusmods.com/skyrimspecialedition/mods/36869`
- Nexus：Open Animation Replacer - `https://www.nexusmods.com/skyrimspecialedition/mods/92109`
- Nexus：Base Object Swapper - `https://www.nexusmods.com/skyrimspecialedition/mods/60805`
- Nexus：AnimObject Swapper - `https://www.nexusmods.com/skyrimspecialedition/mods/75167`
- Nexus：PapyrusUtil SE - `https://www.nexusmods.com/skyrimspecialedition/mods/13048`
- Nexus：JContainers SE - `https://www.nexusmods.com/skyrimspecialedition/mods/16495`
- Nexus：I'm Walkin' Here - `https://www.nexusmods.com/skyrimspecialedition/mods/27742`
- Nexus：I'm Talkin' Here - `https://www.nexusmods.com/skyrimspecialedition/mods/93694`
