# 快速表 + SPID / OAR / PapyrusUtil / JContainers

← [common-framework-mods](common-framework-mods.md)

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

