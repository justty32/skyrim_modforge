# 第四章記憶 10 - 血腥的佩林納爾 (Pelinal the Bloody)

狀態：重構切片（最大的記憶，共 40 個階段）。基於來源、連結優先，非劇情摘要。

來源方針：
- 原始語句連結回抽取的來源文件，而非全文複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` 編排來自 CLI 診斷，因為抽取的 `dialogue.md` 僅保留場景話題文本，而非場景階段/動作。
- 英文是從日文機器翻譯而來，語意經常不明；語意不明的詞彙將在來源欄位中保留原樣並加上標註。正體中文翻譯將未解決的短語標註為 `待驗證`。

## 任務紀錄 (Quest Record)

[`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)

CLI：
- `questdiag Vigilant.esm 0x2A532E`
- `infodiag Vigilant.esm 0x2A532E`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x2A532E`
- EditorID: `zzzCHMemoryQuest10`
- 名稱: `Pelinal the Bloody`
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (40 個階段；僅兩個 `CompleteQuest` 與 `ShutDownStage` 具有標誌，所有日誌皆為空)：

| 階段 | 標誌 |
|---:|---|
| 0 | `StartUpStage` |
| 10, 20, 30, 32, 34 | 無 |
| 40, 41, 42, 43, 44, 45, 46, 47, 48 | 無 |
| 50, 60, 62, 64 | 無 |
| 70, 80, 90 | 無 |
| 100, 105, 110, 115, 120 | 無 |
| 130, 140, 150, 160, 170, 175 | 無 |
| **180** | **`CompleteQuest`** |
| 190 | 無 |
| **300** | **`CompleteQuest`** |
| 310, 320, 330 | 無 |
| 999 | `ShutDownStage` |

任務目標：
- `questdiag` 報告 `Objectives (0)`。本任務**不帶任務目標文本**（在 [quests.md:401](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401) 的條目僅為標題行）。這是一個完全透過場景與階段門檻對話驅動的非追蹤型「記憶」任務。

## 主體 (Subject)

已透過話題 EditorID 與 INFO 文本驗證：
- **佩林納爾 (Pelinal)** (Pelinal Whitestrake) —— 記憶的主角 / 玩家在對話中的角色。
- **瑪麗 (Mary)** —— 尤瑪里爾囚禁的奴隸，懷有尤瑪里爾的孩子；180/300 分歧取決於她。
- **尤瑪里爾 (Umaril)** (Umaril the Feathered) —— 佩林納爾在任務中途殺死的 Ayleid 領主。
- **莫拉格·巴爾 (Molag Bal)** ("Bal") —— 設定道德抉擇的誘惑者。
- **Korn** —— 佩林納爾的獵犬 (別名 `#5`), 僅發出 `(Bark)` / `(Whine)`。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

以下兩個 `SCEN` 紀錄共用相同的主機任務與 11 個別名名冊（來自 `scenediag`）。

主機任務：
- [`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)

來自 `scenediag` 的主機任務別名：

| 別名 | 名稱 | 填充 | NPC 來源 |
|---:|---|---|---|
| 0 | `Umaril` | 唯一演員 `2955ED` | [`2955ED zzzCHBossUmaril "Umaril the Feathered"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:761) |
| 1 | `Mary` | 唯一演員 `2A0679` | [`2A0679 zzzCHSlaveMary "Mary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:793) |
| 2 | `Bal` | 唯一演員 `2A4000` | [`2A4000 zzzCHBardMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:795) |
| 3 | `Prey01` | 唯一演員 `29F2F7` | [`29F2F7 zzzCHPreySlave01 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:767) |
| 4 | `Prey02` | 唯一演員 `29F2F9` | [`29F2F9 zzzCHPreySlave02 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:786) |
| 5 | `Korn` | 唯一演員 `2A3FFC` | [`2A3FFC zzzCHMemoryKorn "Korn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:794) |
| 6 | `Pelinal` | 唯一演員 `0B0EB3` | [`0B0EB3 zzzCHBossPelinal "Pelinal Whitestrake"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1075) |
| 8 | `UmarilTA` | 強制引用 `2A5347` | CLI 未列印（場景演員引用） |
| 9 | `PelinalMemory` | 唯一演員 `2A66C3` | [`2A66C3 zzzCHMemoryPelinal01 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:796) |
| 10 | `MolagBal` | 唯一演員 `2A7A0A` | [`2A7A0A zzzCHMolagBalInMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) |
| 11 | `PelinalTA` | 強制引用 `2AA091` | CLI 未列印（場景演員引用） |

筆記：
- 別名 `#7` 未出現在名冊中（在 `#6` 與 `#8` 之間存在空缺）；這並非本轉儲錯誤，QUST 單純沒有別名 `#7`。
- `Bal`（別名 `#2`，運行自定義分支的談話莫拉格·巴爾）由 NPC `zzzCHBardMemoryPelinal` 填充 —— 關於引擎的一個推論：同一個演員紀錄 (`2A4000`) 兼作記憶內的莫拉格·巴爾化身；坐在王座上的莫拉格·巴爾是一個獨立的紀錄 [`2A7A0A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) 用作別名 `#10` `MolagBal` (BasScene 的配音)。（推論）
- `GetIsID` 對象 `2A5346` 是 **Umaril** 對話分支（無別名；`GetIsID == 2A5346`）的調節紀錄。它無法對應到 `npcs.tsv` 中的行；視為記憶內的 Umaril 說話者紀錄。（推論 —— 需要直接對 `2A5346` 進行 ESM NPC 轉儲。）
- `Korn` (別名 `#5`) 是佩林納爾的狗：每個 Korn INFO 皆為 `(Bark)` / `(Whine)`。

觸發器：
- 激活器 [`4DEF09 zzzCHMeq10GateTrigger "Gate"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv) (`find` 結果) 是位於記憶前的世界內「傳送門」；確切的啟動掛鉤（階段 0 `StartUpStage` 推進）未在此解碼。（推論 —— 驗證激活器腳本 / XLOC。）

## 場景紀錄 (Scene Records)

兩個 `SCEN` 紀錄。兩者皆由任務 `2A532E` 擁有；`scenediag` 的「由任務擁有」列表中共享 13 個 `Scene/Scene` 話題，但每個場景的 `actions` 引用了不同的子集（`GoodScene` 播放 7 個佩林納爾獨白話題 + 3 個歌唱話題；`BasScene` 播放 6 個歌唱/迴聲話題）。場景文本行連結至 `dialogue.md`；階段/動作來自 `scenediag`。

### 2A66C6 zzzCHMeQ10GoodScene

CLI：
- `scenediag Vigilant.esm 0x2A66C6`

編排：
- 主機任務：[`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)
- 標誌：無
- 演員：別名 `#9` (`PelinalMemory`), `behaviorFlags=DeathEnd`, `flags=NoPlayerActivation, Optional`
- 階段：9 個（階段 0 與階段 4 有 2 個完成條件；其餘 1 個；無開始條件）
- 動作 (12)：`Package` 移動動作與 `Dialog` 獨白動作的混合，皆作用於演員 `#9`。

| 動作 | 類型 | 階段 | 話題 | 台詞 |
|---:|---|---:|---|---|
| 1 | Package | 0 | — | — |
| 2 | Package | 1 | — | — |
| 3 | Dialog | 1 | [`2A66C8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2549) | 風暴 / 狂怒獨白 |
| 4 | Dialog | 2 | [`2A66CA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2552) | 戰鬥後的雨 |
| 5 | Dialog | 3 | [`2A66CC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2555) | Kyne 總在哭泣 |
| 6 | Dialog | 4 | [`2A66CE`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2558) | 我的戰爭結束了 |
| 7 | Package | 2-4 | — | — |
| 8 | Package | 5 | — | — |
| 9 | Package | 6-8 | — | — |
| 10 | Dialog | 6 | [`2A66D7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2561) | 奔跑，太陽快要沉沒 |
| 11 | Dialog | 7 | [`2A66D9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2564) | 金色麥田 |
| 12 | Dialog | 8 | [`2A66DB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2567) | Kyne 之風，終於找到了 |

翻譯（佩林納爾的結束獨白 —— 「好/和平」弧線）：
- [`2A66C8` / INFO `2A66C9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2549): 「我曾是一場風暴，是憤怒本身。所以我衝過戰場，斬下婦孺的首級，焚毀村莊。」
- [`2A66CA` / INFO `2A66CB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2552): 「戰鬥之後總是下雨。溫柔的雨沖刷、治癒我的身體，把血流向大海的盡頭。」
  - 筆記：來源 `It carrued the end of the sea` 拼字錯亂（carried），語意待驗證。
- [`2A66CC` / INFO `2A66CD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2555): 「我曾這麼想，也想要這麼相信。儘管 Kyne 一直在哭 —— 不是為我，而是為那些倒下的無辜之人。」
- [`2A66CE` / INFO `2A66CF`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2558): 「再沒有弱者流血了。Kyne 不再落淚。我的……我的戰爭結束了……」
- [`2A66D7` / INFO `2A66D8`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2561): 「我一直奔跑著……懺悔之雨若停了，太陽也快沉落遠方。我所渴望的，是那不斷延伸的陰影。」
  - 筆記：來源 `Rain of contritionif has stop` 拼字錯亂，語意待驗證。
- [`2A66D9` / INFO `2A66DA`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2564): 「金色的麥田，微風拂過麥穗。女人拍打羽絨被，散落的羽毛化作雪，孩子與狼群在上頭嬉戲。」
- [`2A66DB` / INFO `2A66DC`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2567): 「這是……這就是 Kyne 之風。終於找到了……終於……」

### 2AA092 zzzCHMeQ10BasScene

CLI：
- `scenediag Vigilant.esm 0x2AA092`

編排：
- 主機任務：[`2A532E zzzCHMemoryQuest10`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)
- 標誌：無
- 演員 (2)：別名 `#10` (`MolagBal`) 與別名 `#11` (`PelinalTA`)，皆為 `behaviorFlags=DeathEnd`, `flags=NoPlayerActivation, Optional`
- 階段：6 個，每個皆為 0 開始條件 / 1 完成條件。
- 動作 (6)：莫拉格·巴爾 (別名 `#10`) 說出階段 0-2；佩林納爾 (別名 `#11`) 回應階段 3-5。莫拉格·巴爾有兩句台詞帶有 `Flags=HeadtrackPlayer`。

| 動作 | 演員 | 階段 | 頭部追蹤 | 話題 | 台詞 |
|---:|---|---:|---|---|---|
| 1 | #10 MolagBal | 0 | 玩家 | [`2AA093`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2570) | 辱罵其血統 / 舊 Ehlnofey |
| 2 | #10 MolagBal | 1 | — | [`2AA095`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2573) | 驅使精靈諸王將佩林納爾砍成八塊 |
| 3 | #10 MolagBal | 2 | 玩家 | [`2AA097`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2576) | 「好歌……你不覺得嗎，佩林納爾？」 |
| 4 | #11 PelinalTA | 3 | — | [`2AA099`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2579) | （風暴獨白的迴聲） |
| 5 | #11 PelinalTA | 4 | — | [`2AA09B`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2582) | （雨之獨白的迴聲） |
| 6 | #11 PelinalTA | 5 | — | [`2AA09D`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2585) | 「雨何時才會停……以血洗血」 |

翻譯（莫拉格·巴爾敘述歷史上對佩林納爾的屠殺與肢解；佩林納爾的台詞是 GoodScene 獨白的括號迴聲）：
- [`2AA093` / INFO `2AA094`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2570): 「……在他之上，辱罵他的血統，以及所有從舊 Ehlnofey 渡海而來的人。」
  - 筆記：此句為片段（承接前句），原文無前文，語意待驗證。
- [`2AA095` / INFO `2AA096`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2573): 「激怒了其餘的精靈諸王，將他們逼向瘋狂，把佩林納爾砍成八塊。」
- [`2AA097` / INFO `2AA098`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2576): 「這會是首好歌。你不覺得嗎，佩林納爾？」
- [`2AA099` / INFO `2AA09A`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2579): 「（我曾是一場風暴，是憤怒本身。所以我衝過戰場，斬下婦孺的首級，焚毀村莊。）」
- [`2AA09B` / INFO `2AA09C`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2582): 「（戰鬥之後總是下雨。溫柔的雨沖刷、治癒我的身體，把血流向大海的盡頭。）」
- [`2AA09D` / INFO `2AA09E`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2585): 「（雨何時才會停……我何時才要以血洗血……）」

## 自定義對話分支 (Custom Dialogue Branches)

任務擁有 **9 個 `DialogBranch` 紀錄**加一個 `Hello` 話題。說話者受限於 `GetIsAliasRef` (別名索引) + `GetStage`。條件別名映射：`#1` = Mary, `#2` = Bal (Molag Bal), `#5` = Korn (dog), 以及 `GetIsID == 2A5346` = Umaril。VMAD `OnEnd` 片段 (`CHMeq10_TIF__02<INFO>`) 在玩家推進狀態的選擇上觸發。

### 分支：Korn 01 — `2A5335 zzzCHMeQ10KornB01` (階段 30, 別名 #5)

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A5336 zzzCHMeQ10KornB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2474) | `2A5337` | 無 | `GetStage==30`; 別名 `#5` | (Bark) |
| [`2A5338 zzzCHMeQ10KornB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2477) | `2A5339` | `Goodbye` | `GetStage==30`; 別名 `#5`; VMAD `CHMeq10_TIF__022A5339` | 提示語：「滾開，別煩我」 → (Whine) |

### 分支：Umaril 01 — `2A534C zzzCHMeQ10UmarilB01` (階段 70, GetIsID 2A5346)

在殺死「羽翼」尤瑪里爾之前的對質。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A534D zzzCHMeQ10UmarilB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2480) | `2A534E` | 無 | `GetStage==70`; `GetIsID 2A5346` | 「你真是樂在殺戮。你像個逗弄昆蟲的嬰孩。」 |
| [`2A534F zzzCHMeQ10B01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2483) | `2A5350` | 無 | `GetIsID 2A5346`; `GetStage==70` | 提示語：「……（沉默）」 回應：「只要 Ada 的污血還沾在大地上，我們的神話紀元就尚未消逝。你和我……」 |
| [`2A5351 zzzCHMeQ10UmarilB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2486) | `2A5352` | `Goodbye` | `GetIsID 2A5346`; `GetStage==70`; VMAD `CHMeq10_TIF__022A5352` | 提示語：「來吧。我是來殺你的。」 回應：「當然。等你被斬首之後，我們再談。那時你就願意聽了。」 |

筆記：`Ada` = 艾德拉 / 眾神 (Ayleid 用法)；依 ES 傳說慣例不予翻譯。

### 分支：Bal 01 — `2A535A zzzCHMeQ10BalB01` (階段 90, 別名 #2)

殺死目標後：莫拉格·巴爾致意佩林納爾。與下方的 `Hello` 開場對話成對。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A535B zzzCHMeQ10BalB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2489) | `2A535C` | `SayOnce` | `GetStage==90`; 別名 `#2` | 提示語：「你和 Meridia 勾結？」 回應：「勾結？才不。那位傲慢的老巫婆跟誰都不勾結。不過托她的福，我的買賣才能順利進行。」 |

筆記：`Haughty Hag` = Meridia (莫拉格·巴爾嘲諷的稱號)。

### 分支：Bal 02 — `2A535F zzzCHMeQ10BalB02` (階段 90, 別名 #2)

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A5360 zzzCHMeQ10BalB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2499) | `2A5361` | `Goodbye, SayOnce` | `GetStage==90`; 別名 `#2`; VMAD `CHMeq10_TIF__022A5361` | 提示語：「你到底想幹嘛？我受夠你的把戲了。」 回應：「別這麼說嘛。跟我來，我帶你去尤瑪里爾的工坊看點有趣的。」 |

筆記：`Atelier of Umaril` 「尤瑪里爾的工坊／畫室」；與下方的地點傳說 "Art of Lost Abagarlas" 相關。

### 分支：Bal 03 — `2A668F zzzCHMeQ10BalB03` (階段 105, 別名 #2)

莫拉格·巴爾向佩林納爾展示一件殘酷的「藝術品」。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A6690 zzzCHMeQ10BalB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2502) | `2A6691` | 無 | `GetStage==105`; 別名 `#2` | 「如何？這是失落的 Abagarlas 之藝。他費了好大功夫才做出複製品。」 / 「血雨與堆積的內臟，就像你經歷過的景象。若相遇的方式不同，他會不會成了你的好友呢？」 |
| [`2A6692 zzzCHMeQ10BalB03T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2506) | `2A6693` | `Goodbye` | 別名 `#2`; VMAD `CHMeQ10_TIF__022A6693` | 提示語：「噁心。這就是你要給我看的？」 回應：「看來你不喜歡。那就忘了它吧。」 / 「我真正想給你看的在後面。跟我來。」 |

筆記：`Abagarlas` = Ayleid 遺址城市 (實存傳說)。`He` 指誰待驗證（疑為尤瑪里爾或某工匠）。

### 分支：Bal 04 — `2A6694 zzzCHMeQ10BalB04` (階段 115, 別名 #2) — 抉擇

這是設定 180 對 300 決定權的分支：莫拉格·巴爾呈現了懷有尤瑪里爾孩子的奴隸**瑪麗**，並慫恿佩林納爾殺了她。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A6695 zzzCHMeQ10BalB04T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2510) | `2A6696` | 無 | `GetStage==115`; 別名 `#2` | 「就是這個，這個。」 |
| [`2A6697 zzzCHMeQ10BalB04T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2513) | `2A6698` | 無 | 別名 `#2` | 提示語：「她是？」 回應：「尤瑪里爾的拋棄式性奴。再過一天她就會被溶進那件『藝術品』裡。」 / 「不過尤瑪里爾現在死了。真好，你成了她的救命恩人。」 |
| [`2A6699 zzzCHMeQ10BalB04T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2517) | `2A669A` | 無 | 別名 `#2` | 提示語：「你要我做什麼？」 回應：「她懷著尤瑪里爾的孩子。你想怎麼做？」 |
| [`2A669B zzzCHMeQ10BalB04T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2520) | `2A669C` | 無 | 別名 `#2` | 提示語：「你是說殺了她？」 回應：「我不在乎你殺不殺。但你若不殺它，它將來會威脅世人。」 / 「Ada 之血賦予力量，但心智卻脆弱易碎。那血脈的命運，你最清楚不過。」 |
| [`2A669D zzzCHMeQ10BalB04T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2524) | `2A669E` | `Goodbye` | 別名 `#2`; VMAD `CHMeQ10_TIF__022A669E` | 提示語：「滾。」 回應：「好吧，你需要點時間決定。好好享受。」 |

### 分支：Mary 01 — `2A66A6 zzzCHMeQ10MaryB01` (階段 130, 別名 #1)

在**放過瑪麗**的路徑上觸發：佩林納爾釋放瑪麗並帶她離開。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A66A7 zzzCHMeQ10MaryB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2527) | `2A66A8` | 無 | `GetStage==130`; 別名 `#1` | 「謝……謝謝你……」 |
| [`2A66A9 zzzCHMeQ10MaryB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2530) | `2A66AA` | 無 | `GetStage==130`; 別名 `#1` | 提示語：「你能走嗎？」 回應：「能……可是……」 |
| [`2A66AB zzzCHMeQ10B01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2533) | `2A66AC` | 無 | `GetStage==130`; 別名 `#1`; VMAD `CHMeq10_TIF__022A66AC` | 提示語：「走吧，我們走。」 回應：「好、好的……」 |

### 分支：Korn 02 — `2A66B3 zzzCHMeQ10KornB02` (階段 140, 別名 #5)

在放過瑪麗路徑上的獵犬（「保護她」）。

| 話題 | INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|---|
| [`2A66B4 zzzCHMeQ10KornB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2536) | `2A66B5` | 無 | `GetStage==140`; 別名 `#5` | (Bark) |
| [`2A66B6 zzzCHMeQ10B02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2539) | `2A66B7` | `Goodbye` | `GetStage==140`; 別名 `#5`; VMAD `CHMeq10_TIF__022A66B7` | 提示語：「看好她。」 → (Bark) |

### 分支：Bal 05 — `2A66BE zzzCHMeQ10B05` (階段 160, 別名 #2)

佩林納爾放過瑪麗後莫拉格·巴爾的反應 —— 仁慈路徑的終點。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`2A66BF zzzCHMeQ10B05T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2542) | `2A66C0` | 無 | `GetStage==160`; 別名 `#2` | 「哎呀哎呀，你沒殺她？這可不像你。」 / 「這樣好嗎？她的孩子會犯下錯誤 —— 比你更大的錯誤。」 |
| [`2A66C1 zzzCHMeQ10BalB05T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2546) | `2A66C2` | `Goodbye` | `GetStage==160`; 別名 `#2`; VMAD `CHMeq10_TIF__022A66C2` | 提示語：「未來的人生由她自己決定。我管不著。」 回應：「……是嗎。有緣再會吧 —— 如果你還能的話。」 |

### Hello — `2A535D zzzCHMeQ10Hello` (無分支, 別名 #2 / #1)

單個 Hello 話題中依階段變化的致意（優先級按順序排列 —— 參見儲存庫記憶「條件 Hello = 一個話題，多個 INFO」）：

| INFO | 標誌 | 條件 | 來源 / 翻譯 |
|---|---|---|---|
| `2A535E` | `SayOnce` | `GetStage==90`; 別名 `#2` (Bal) | 「哎呀，佩林納爾先生。漂亮。你的功績將化作歌謠，永世流傳。」 |
| `2A66A2` | `Goodbye` | `GetStage<=120`; 別名 `#1` (Mary) | 「救命……」 |
| `2A66B0` | `Goodbye, SayOnce` | `GetStage==140`; 別名 `#1` (Mary) | 「你……你剛才在跟誰說話……」 |
| `2A66B1` | `Goodbye` | `GetStage==140`; 別名 `#1` (Mary) | 「啊……哈……對不起……」 |

筆記：`Splended` = "Splendid" (拼字錯誤，已照語意翻)。

## 180 對 300 —— 分支結果映射

兩次完成皆為無條件的 `CompleteQuest` 階段日誌（階段上無條件），因此極性是從每個波段可觸及的**對話/場景內容**讀取，而非 `questdiag`。

- **階段 180 = 仁慈 / 「好」的完成。** 放過瑪麗的鏈條完全運行在 130-180 波段：Mary 分支 (`2A66A6`, 階段 130) → Korn「保護她」 (`2A66B3`, 階段 140) → Bal 的「你沒殺她？」反應 (`2A66BE`, 階段 160) → 在 180 處完成。`GoodScene` (`2A66C6`, 佩林納爾的和平/Kyne 之風獨白) 是由 EditorID 命名的「Good」場景並解決此弧線。**(極性：仁慈/好 —— 由 EditorID `GoodScene` + 放過瑪麗的對話強力支撐。)**
- **階段 300 = 備選的 / 「壞」（殺死瑪麗）的完成。** 190-330 波段（階段 190, 300, 310, 320, 330）在 `infodiag` 中**沒有擁有的自定義對話題材** —— 它完全由階段片段 / package 驅動（例如 `zzzCHMeq10PelinalWalkToDie`, `zzzCHMeq10PelinalMeditate`）。這是玩家在 Bal-04 抉擇處（階段 115）殺死瑪麗，繞過 Mary/Korn-02/Bal-05 仁慈鏈條後到達的分支。`BasScene` (`2AA092`, EditorID `BasScene` —— 推論：「Bad/Base」) 敘述了莫拉格·巴爾對佩林納爾歷史上的屠殺與肢解的嚴酷回顧，符合較黑暗的結果。**(極性：殺戮/壞 —— 來自 EditorID `BasScene`、對話為空的 190-330 波段以及殺戮導向的 package 名稱的推論；不如 180 那麼確定，因為不存在可引用的殺戮路徑對話。)**

路由推論：
- **Bal-04 抉擇** (`2A6694`, 階段 115) 是分歧點。選擇仁慈會向階段 130 (Mary 分支) → 180 推進。選擇殺死則跳至 190+ 波段 → 300。確切的階段設定邏輯存在於 VMAD `OnEnd` 片段中（「滾」出口處的 `CHMeQ10_TIF__022A669E` 以及階段片段），此處尚未反編譯。（推論）

## 相關紀錄 (Related Records)

並非皆由任務 `2A532E` 擁有，但為相同的佩林納爾/尤瑪里爾/瑪麗角色 —— 在完整重構中進行交叉連結。

NPCs（記憶角色，別名填充）：
- [`0B0EB3 zzzCHBossPelinal "Pelinal Whitestrake"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1075) — 別名 `#6` Pelinal
- [`2A66C3 zzzCHMemoryPelinal01 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:796) — 別名 `#9` PelinalMemory (GoodScene 演員)
- [`2BC37F zzzCHMemoryPelinal02 "Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:785) — 第二位佩林納爾記憶紀錄 (BasScene `PelinalTA` 候選者，推論)
- [`2955ED zzzCHBossUmaril "Umaril the Feathered"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:761) — 別名 `#0` Umaril
- [`2A0679 zzzCHSlaveMary "Mary"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:793) — 別名 `#1` Mary
- [`2A4000 zzzCHBardMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:795) — 別名 `#2` Bal (談話的莫拉格·巴爾)
- [`2A7A0A zzzCHMolagBalInMemoryPelinal`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:797) — 別名 `#10` MolagBal (BasScene 配音)
- [`2A3FFC zzzCHMemoryKorn "Korn"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:794) — 別名 `#5` Korn (狗)
- [`29F2F7 zzzCHPreySlave01 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:767) / [`29F2F9 zzzCHPreySlave02 "Slave"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:786) — 別名 `#3`/`#4` Prey

地點：
- [`295516 zzzCHMemPelinal "White-Gold Tower"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:619) — 記憶的背景 (LCTN)
- [`0243F1 zAoMMythicPlace`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:162) — "Mythic" 細胞（推論：記憶內部）

書籍（傳說背景，非任務擁有；敘事使用前請先驗證）：
- [`12905C zzzCHBookESOChantTwilight "The Song-Never-Sung-at-Twilight"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:70) — 佩林納爾/尤瑪里爾之歌傳說
- [`140504 zzzCHBalConjurePelinal "Piece of Bal: Pelinal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:253) — 莫拉格·巴爾召喚佩林納爾物品
- [`2C241B zzzCHMeridiaConjureUmaril "Meridia's Beaconl: Umaril"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:506) — Meridia 召喚尤瑪里爾物品（與 Bal-01 中的「Meridia」台詞相關）

## 重構筆記 (Reconstruction Notes)

基於來源：
- 本記憶為 [`2A532E zzzCHMemoryQuest10 "Pelinal the Bloody"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:401)：40 個階段，0 處為 `StartUpStage`, 180 與 300 處為 `CompleteQuest`, 999 處為 `ShutDownStage`, **無任務目標文本**。
- 包含**2 個 `SCEN` 紀錄**：`2A66C6 GoodScene` (9 個階段，12 個動作，佩林納爾的和平獨白) 與 `2AA092 BasScene` (6 個階段，莫拉格·巴爾敘述佩林納爾的屠殺，而佩林納爾迴響其獨白)。
- 包含**9 個自定義 `DialogBranch` 紀錄 + 1 個 Hello 話題**，受別名限制：Korn (狗) ×2, Umaril ×1, Bal (莫拉格·巴爾) ×5, Mary ×1。**Bal-04 分支 (階段 115)** 是關於尤瑪里爾懷孕奴隸**瑪麗**的殺戮或放過分歧點。
- 仁慈路徑 (放過瑪麗) 運行階段 130-180，帶有 Mary/Korn-02/Bal-05 分支，並在 **180** 處完成 (`GoodScene`)。殺戮路徑運行對話為空的 190-330 波段，並在 **300** 處完成 (`BasScene`)。
- 在 `Goodbye`/抉擇 INFO 上的 VMAD `OnEnd` 片段 (`CHMeq10_TIF__02<INFO>`) 驅動階段推進；確切的 Papyrus 未在此解碼。

開放驗證：
- 反編譯 / 檢查 TIF 片段 (`CHMeq10_TIF__022A5339`, `022A5352`, `022A5361`, `022A6693`, `022A669E`, `022A66AC`, `022A66B7`, `022A66C2`) 以確認哪一個設定了殺戮與放過的階段路徑（精確釘定 180/300 路由）。
- 轉儲 NPC 紀錄 `2A5346` (Umaril 分支的 `GetIsID` 對象) —— 在 `npcs.tsv` 中未找到；確認其為記憶內的 Umaril 說話者。
- 確認 `BasScene` EditorID 展開 (「Bad」/「Base」)，以及 190-330 波段即為殺死瑪麗的結果（目前根據 EditorID + 空對話波段 + `PelinalWalkToDie`/`PelinalMeditate` package 名稱推論）。
- 轉儲 QUST 別名/目標以及 `4DEF09 zzzCHMeq10GateTrigger` 激活器 + 啟動掛鉤，以確認觸發器與 `UmarilTA`/`PelinalTA` 的強制引用放置。
- 若空間編排重要，請檢查白金塔記憶 (`295516`) 與神話之地 (`0243F1`) 的細胞/引用。
- 遞延的場景 package 移動動作 (GoodScene 動作 1, 2, 7, 8, 9 = 無話題的 `Package`) 不含文本，在此不予翻譯。
