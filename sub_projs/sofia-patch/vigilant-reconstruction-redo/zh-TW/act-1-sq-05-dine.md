# 第一章 任務 05 - 霸王餐 (Dine and Dash)

狀態：基於原始碼的片段。連結優先，以對話為中心，根據條件繪製的分支圖。

來源策略：
- 原始對話行連結至提取的來源檔案，而非完整複製。
- 僅在需要解釋歧義或拼字錯誤/編碼問題時才顯示短小的原始碼片段。
- 場景編排來自 CLI 診斷與對話主題結構，而非劇情摘要。

## 任務紀錄 (Quest Record)

[`0098C9 zzzAoMMq05 "Dine and Dash"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:218)

CLI：
- `questdiag Vigilant.esm 0x0098C9`
- `infodiag Vigilant.esm 0x0098C9`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x0098C9`
- EditorID: `zzzAoMMq05`
- 名稱 (Name): `Dine and Dash`
- 旗標 (Flags): `RunOnce`
- 優先度 (Priority): `90`
- 類型 (Type): `SideQuest`
- 過濾器 (Filter): `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 10 | 無 | 空白 |
| 11 | 無 | 空白 |
| 20 | 無 | 空白 |
| 21 | 無 | 空白 |
| 22 | 無 | 空白 |
| 23 | 無 | 空白 |
| 25 | 無 | 空白 |
| 30 | 無 | 空白 |
| 40 | 無 | 空白 |
| 45 | 無 | 空白 |
| 50 | 無 | 空白 |
| 60 | CompleteQuest | 空白 |
| 255 | ShutDownStage | 空白 |
| 9999 | CompleteQuest | 空白 |

（總共 15 個階段，已驗證；階段 60 與 9999 為 `CompleteQuest`）

來自 `questdiag` 的目標 (Objectives)：

| 索引 | 來源 | 日誌 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:219) | 在燭爐堂與阿爾塔諾對話 (Talk to Altano in the Candle Hearth Hall) |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:220) | 跟隨阿爾塔諾，或是在斯丹達爾燈塔與他會合 (Follow Altano or Join Altano at Stendarr's Beacon) |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:221) | 跟隨阿爾塔諾，或是在蜂與勾刺與他會合 (Follow Alatano or Join Altano at The Bee and Barb) |
| 25 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:222) | 擊敗魔族 (Defeat Daedra) |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:223) | 與基拉瓦對話 (Talk to Keerave) |
| 40 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:224) | 支付 1000 金幣給基拉瓦 (Pay 1000G Keerave) |
| 41 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:225) | 向阿爾塔諾借錢（選項） (Barrow Money from Altano (Option)) |
| 50 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:226) | 向雅各回報 (Report to Jacob) |

目標對象 (Objective targets)：
- 具有位置目標的 8 個任務目標（裂谷城的設施：燭爐堂、斯丹達爾燈塔、蜂與勾刺）。
- CLI `questdiag` 未印出目標對象 (target refs)；若確切的對象位置很重要，則需要更深入的 QUST 別名/目標轉儲。

## 對話主幹 (Dialogue Backbone)

對話分為 5 個分支，具有不同的條件限制（階段與別名檢查）。階段進度：0 → 10/11 (任務說明) → 20/21/22/23/25 (旅程) → 30 (酒館遭遇) → 40/45 (支付協商) → 50/60 (完成)。

所有自訂主題皆需要對別名 (`alias #0` = 阿爾塔諾, `alias #1` = 雅各, `alias #7` = 酒館老闆基拉瓦) 執行 `GetIsAliasRef` 檢查。

### 分支 1：任務簡報 (階段 0→10)

自訂主題：
- [`009E30 zzAoMMq05B1Mission5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:156)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`009E30 zzAoMMq05B1Mission5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:156) | `009E31` | 無 | GetInCell `016789:Skyrim.esm` (燭爐堂); 任務 `0098C9` 的階段 < 10; GetIsAliasRef 別名 #0 | 「我收到了一封來自斯丹達爾燈塔的信。有人在裂谷城目擊到了召喚師。我們去斯丹達爾燈塔聽取詳情吧。」 |
| | | | VMAD: `AoM05_TIF__01009E31` 結束時觸發 Fragment_0 | |

### 分支 2：斯丹達爾燈塔的場景主題 (階段 10→20)

場景交流：在斯丹達爾燈塔與雅各進行簡報。這些是以場景交流形式建構的對話主題（無條件的場景流）：

| 主題 | INFO | 說話者 | 回應 | 翻譯 |
|---|---|---|---|---|
| [`009E3D` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:159) | `009E3E` | — | [「Master Jacob, Long time no see. How are you?」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:160) |
| [`009E3F` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:162) | `009E40` | — | [「Hahaha, don't stand on ceremony so much. You and I are agents of Stenndarr.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:163) |
| [`009E41` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:165) | `009E42` | — | [「So...we heard you find the summoner...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:166) |
| [`009E43` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:168) | `009E44` | — | [「Viglants find her in the Bee and Barb. They will catch her....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:169); [「She summoned Powerful Daedra..so vigilants are at a loss what to do.To make matters worse, theat Daedra stay at Inn.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:170); [「But we are fully occupied to chase summoner. I entrust defeating Deadra to you.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:171) |
| [`009E45` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:173) | `009E46` | — | [「Let us handle this. The Daedra will regret to be summoned.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:174) |
| [`009E47` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:176) | `009E48` | — | [「Hahaha! You are reliable! By the way..about your partner...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:177) |
| [`009E49` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:179) | `009E4A` | — | [「You have good eyes as letter from Altano. Your look is like Stendarr....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:180); [「Be carefull, Daedra is astute.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:181) |
| [`009E4B` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:183) | `009E4C` | — | [「Here, we go.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:184) |

（翻譯：009E3E「雅各長老，好久不見。您身體可好？」009E40「哈哈哈，別這麼客氣。你我都是斯丹達爾的代理人。」009E42「所以……我們聽說您找到了召喚師……」009E44「警戒者在蜂與勾刺發現了她。他們會抓住她的……」「她召喚了強大的魔族……警戒者現在束手無策。更糟的是，那名魔族留在酒館裡不走。」「但我們正忙於追捕召喚師。擊敗魔族的事就託付給你們了。」009E46「交給我們處理吧。那名魔族會後悔被召喚出來的。」009E48「哈哈哈！你們真可靠！順帶一提……關於你的夥伴……」009E4A「你的眼神正如阿爾塔諾信中所說的一樣。你的神情就像斯丹達爾……」「小心點，魔族是很狡詐的。」009E4C「我們出發吧。」）

備註：
- 場景主題中無明確的說話者名稱；根據背景推論（在燈塔的雅各，玩家/阿爾塔諾作為回應者）。
- 此交流將任務目標 10（到達燈塔）銜接至目標 20（到達酒館）與階段 20。

### 分支 3：蜂與勾刺的場景主題 (階段 20→30)

魔族遭遇場景。多個 INFO，無分支，無條件流動。

場景交流：抵達酒館並與魔族互動。

| 主題 | INFO | 說話者 | 回應 | 翻譯 |
|---|---|---|---|---|
| [`009E4F` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:186) | `009E50` | 魔族 | [「Hey, Waiter!! Bring more foods and drinks, or I will eat your head!!」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:187) |
| [`009E51` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:189) | `009E52` | — | [「Where is your summoner? if you admit, I kill you peacefully.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:190) |
| [`009E53` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:192) | `009E54` | 魔族 | [「Kill? Mortal say kill immmortal Daedra? Hahahahaha! Mortal is very funny.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:193); [「You want infomation about summoner? I admit you enter my stomack.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:194) |

（翻譯：009E50「嘿，服務生！！再拿更多吃的喝的來，不然我就把你的腦袋吞了！！」009E52「妳的召喚師在哪裡？如果你招供，我會讓你死得痛快點。」009E54「殺？凡人竟然說要殺死不朽的魔族？哈哈哈哈！凡人真是有趣。」「你想要關於召喚師的資訊？我准許你進入我的肚子。」）

備註：
- 魔族的幽默與食慾將對峙與喜劇基調結合。
- 場景導向任務目標 25（擊敗魔族）與階段 30（戰後，酒館老闆對話）。
- 拼字錯誤：「immmortal」（原始碼如此）；之前的場景中「theat」應為「that」。

### 分支 4：支付協商 (階段 30→45)

三個與支付相關的主題，代表玩家擊敗魔族後的選項：全額支付、延期/沒錢，或是向阿爾塔諾借錢。

#### 子分支 4a：要求支付 (階段 30)

自訂主題：
- [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196)

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196) | `009E57` | SayOnce, WalkAway | 任務 `0098C9` 的階段 == 30; GetIsAliasRef 別名 #7 (基拉瓦) | 「嘿！等等！！你該為那魔族吃的喝的付帳。總共是 1000 金幣。我絕不降價！！」 |
| | | | VMAD: `AoM05_TIF__01009E57` 結束時觸發 Fragment_0 | |
| [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196) | `009E58` | WalkAway | 任務 `0098C9` 的階段 >= 40; 階段 < 50; GetIsAliasRef 別名 #7 | 「你能付那 1000 金幣嗎？」 |

備註：
- 第一個 INFO 上的 `SayOnce` → 僅觸發一次階段 30 對話。
- `WalkAway` 旗標表示 NPC 中斷對話。
- 在階段 40+ 重新開啟，提示語較簡單（「你能付那 1000 金幣嗎？」），暗示有多個對話機會。

#### 子分支 4b：全額支付路徑 (支付 1000 金幣)

自訂主題：
- [`009E59 zzAoMMq05B2Pay1000`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:200)

| 主題 | INFO | 旗標 | 條件 | 回應 | VMAD |
|---|---|---|---|---|---|
| [`009E59 zzAoMMq05B2Pay1000`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:200) | `009E5A` | Goodbye | 持有金幣 >= 1000 (玩家引用 `000014:Skyrim.esm`); GetIsAliasRef 別名 #7 | [「Thank you. You should choose your friends very carefully.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:201) | `AoM05_TIF__01009E5A` Fragment_1 (OnBegin) + Fragment_0 (OnEnd) |

（翻譯：009E5A「謝謝。你該慎重選擇你的朋友。」）

備註：
- `Goodbye` 旗標 → 結束對話。
- 在玩家角色（原生引用 0x000014）上執行金幣檢查。
- VMAD 回呼可能扣除 1000 金幣並推進任務。

#### 子分支 4c：沒錢路徑 (延期)

自訂主題：
- [`009E5B zzAoMMq05B2NoMoney`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:203)

| 主題 | INFO | 旗標 | 條件 | 回應 |
|---|---|---|---|---|
| [`009E5B zzAoMMq05B2NoMoney`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:203) | `009E5C` | Goodbye | GetIsAliasRef 別名 #7 | [「OK.I wait a minute for you. if you dine and dash... I will call gurads.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:204) |

（翻譯：009E5C「好吧。我再等你一下。如果你想吃霸王餐……我會叫衛兵。」）

備註：
- 給沒有 1000 金幣的玩家的回呼。
- 「dine and dash (吃霸王餐)」引用（任務名稱來源） —— 若玩家不付錢，酒館老闆會發出威脅。
- 拼字錯誤：「gurads」→「guards」。

#### 子分支 4d：借錢路徑 (向阿爾塔諾借)

自訂主題：
- [`009E5E zzAoMMq05B3Debt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:206)

| 主題 | INFO | 旗標 | 條件 | 回應 | VMAD |
|---|---|---|---|---|---|
| [`009E5E zzAoMMq05B3Debt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:206) | `009E5F` | SayOnce | 任務 `0098C9` 的階段 == 40; GetIsAliasRef 別名 #0 (阿爾塔諾) | [「Huh...OK. I will pay 800G.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:207) | `AoM05_TIF__01009E5F` 結束時觸發 Fragment_0 |

（翻譯：009E5F「哼……好吧。我會幫你付 800 金幣。」）

備註：
- 阿爾塔諾（別名 #0）代付了 1000 金幣債務中的 800 金幣（玩家支付 200 金幣）。
- 門檻：階段 40（協商後的時間點）。
- 替代方案：借錢路徑對比全額支付路徑。

### 分支 5：完成 (階段 50→60)

兩個回報主題，向任務地點的雅各回報。

#### 子分支 5a：任務成功回報

自訂主題：
- [`009E61 zzAoMMq05B4Mission5Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:209)

| 主題 | INFO | 旗標 | 條件 | 回應 |
|---|---|---|---|---|
| [`009E61 zzAoMMq05B4Mission5Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:209) | `009E62` | 無 | 任務 `0098C9` 的階段 == 50; GetIsAliasRef 別名 #1 (雅各) | [「Many thanks for your trouble」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:210) |

（翻譯：009E62「非常感謝你辛勞奔波」）

#### 子分支 5b：戰後調查 (階段 50→60)

自訂主題：
- [`009E63 zzAoMMq05B4Summoner`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:212)

| 主題 | INFO | 旗標 | 條件 | 回應內容 |
|---|---|---|---|---|
| [`009E63 zzAoMMq05B4Summoner`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:212) | `009E64` | 無 | GetIsAliasRef 別名 #1 (雅各) | [「Yes, viglants run the summoner down in Ratway...but we fail to catch. There is a swordman who equips Ebony mail with the summoner.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:213); [「Swordman maybe hired by the summoner. He is very strong. He broke through the besieging vigilants...head-on...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:214); [「Special Chasers started just now. How many people survive.....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:215) |

（翻譯：009E64「是的，警戒者在鼠道圍堵了召喚師……但我們沒能抓到她。召喚師身邊有一名裝備黑檀鎖子甲的劍士。」「那名劍士可能是召喚師雇來的。他非常強大。他正面突破了警戒者的包圍網……」「特別追擊小隊剛出發。不知道有多少人能活下來……」）

備註：
- 回報召喚師逃往鼠道，並有一名身分不明的傭兵（裝備黑檀鎖子甲者）隨行。
- 銜接至第一章任務 06（「虎人如是說」）的鼠道支線劇情。
- 結果模糊：儘管軍事上佔優勢，但在戰術上卻是失利的。

#### 子分支 5c：下一個任務簡報

自訂主題：
- [`009E65 zzAoMMq05B4NextMission`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:217)

| 主題 | INFO | 旗標 | 條件 | 回應內容 | VMAD |
|---|---|---|---|---|---|
| [`009E65 zzAoMMq05B4NextMission`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:217) | `009E66` | Goodbye | GetIsAliasRef 別名 #1 (雅各) | [「Invetstigate Ratway. There is the marks of Cojurring Daedra.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:218); [「At the moment, no damage was reported in Ratway. But there is dangerous. Search Daedra and destroy.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:219) | `AoM05_TIF__01009E66` 結束時觸發 Fragment_0 |

（翻譯：009E66「去調查鼠道。那裡有召喚魔族的痕跡。」「目前鼠道還沒有傳出傷亡報告。但那裡很危險。搜尋魔族並摧毀它們。」）

備註：
- 最終目標：向雅各回報 → 任務完成（階段 60）。
- 分支回應（兩個版本）：暗示敘事路徑的變異（良善與激進的解決方式）。
- 拼字錯誤：「Invetstigate」→「Investigate」；「marks」（複數）暗示有多個魔族或召喚地點。

## 相關紀錄

NPCs (任務別名)：
- `阿爾塔諾 (Altano)` (別名 #0)：主角同伴、借款來源、簡報夥伴。
- `雅各 (Jacob)` (別名 #1)：任務發布者、戰後回報對象。
- `基拉瓦 (Keerave)` (別名 #7)：酒館老闆、收款人。

地點 (目標對象)：
- `016789:Skyrim.esm` — 燭爐堂（裂谷城），最初簡報地點。
- (斯丹達爾燈塔與蜂與勾刺的目標對象未被 `questdiag` 印出；需要查閱儲存格引用)。

引用的物品：
- 對話中無明確引用；魔族對進餐的描述（「進入我的肚子」）是敘事基調。

## 重建筆記

基於原始碼：
- 此任務由 [`0098C9 zzzAoMMq05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:218) 代表，包含橫跨裂谷城設施的 8 個目標以及支付機制。
- 階段進度：簡報 (0–10) → 旅程 (10–25) → 酒館戰鬥 (25–30) → 支付協商 (30–45) → 完成回報 (45–60)。
- 包含 5 個對話分支：
  - 與阿爾塔諾簡報（自訂主題，`GetInCell` 檢查）。
  - 在斯丹達爾燈塔與雅各的簡報場景（11 個無條件的場景主題）。
  - 在蜂與勾刺與魔族的遭遇（3 個場景主題）。
  - 與基拉瓦的支付協商（4 種支付路徑：全額、沒錢、借錢或未指定）。
  - 與雅各的完成與後續（3 個主題：成功、召喚師逃脫、下一個任務）。
- 6 個 INFO 上的 VMAD 回呼顯示了階段推進與資源扣除（金幣、任務狀態）。
- 此任務不擁有明確的 `SCEN` 紀錄（對話是純粹的主題-INFO 鏈，而非場景編排）。

敘事弧：
- 裂谷城酒館出現魔族騷亂 → 警戒者小隊（玩家 + 阿爾塔諾）受命調查 → 擊敗魔族 → 酒館老闆要求支付魔族餐點/財產損失的費用（1000 金幣） → 玩家可以全額支付、延期或向阿爾塔諾借錢 → 回報成功，但召喚師逃往鼠道 → 為下一個任務（第一章任務 06）進行簡報。

分支極性：
- **良善路徑**：全額支付（1000 金幣）給基拉瓦 → 圓滿解決。
- **替代路徑**：向阿爾塔諾借錢（阿爾塔諾付 800 金幣，玩家付 200 金幣） → 標記為債務。
- **延期路徑**：拒絕/沒錢 → 酒館老闆威脅（霸王餐引用） → 緊張局勢未解決。
- 所有路徑最終都會導向階段 50 的完成回報，但敘事基調會有所不同。

發布狀態：
- 任務已完全實現在遊戲對話中；提取出的文本中未發現缺失的語音行或占位用的 TODO。
- 存在英文拼字錯誤 (immmortal, theat, gurad[s], Invetstigate) —— 可能來自原始模組來源。

開放驗證：
- 若存在原始碼或反編譯路徑，請檢查腳本 `AoM05_TIF__01009E31`、`AoM05_TIF__01009E57`、`AoM05_TIF__01009E5A`、`AoM05_TIF__01009E5F`、`AoM05_TIF__01009E66`；
- 若確切位置很重要，請驗證斯丹達爾燈塔與蜂與勾刺的目標對象引用位置；
- 若更豐富的任務狀態/封裝很重要，請直接檢查 QUST 別名（`別名 #0`、`#1`、`#7`）。
