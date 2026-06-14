# 第四章記憶 03 — 獵犬騎士 (Act 4 Memory 03 - Knight of Hound)

狀態：重做切片。基於來源 (Source-grounded)，連結優先，非劇情摘要。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- 此任務**無擁有場景 (SCEN) 記錄** (在 `find` 中無 `…Sc…` 話題，且 `scenediag 0x13965A` 報告「不是場景」)。編排是由**強制問候 (force-greet) AI 程序包**驅動的；詳見「編排骨幹」。

## 任務記錄 (Quest Record)

[`13965A zzzCHMemoryQuest03 "Knight of Hound"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154)

CLI 指令：
- `questdiag Vigilant.esm 0x13965A`
- `infodiag Vigilant.esm 0x13965A`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x13965A`
- EditorID: `zzzCHMemoryQuest03`
- 名稱: `Knight of Hound` (獵犬騎士)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 1 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 40 | 無 | 空 |
| 100 | 無 | 空 |
| 105 | 無 | 空 |
| 110 | 無 | 空 |
| 120 | 無 | 空 |
| 130 | CompleteQuest | 空 |
| 999 | ShutDownStage | 空 |

在 **30** 和 **130** 處有兩波段 `CompleteQuest` —— 這是索引中提到的業障/分支特徵。極性映射見下文。

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:155) | 血脈從不分離，而是相連。 |

目標對象 (Objective targets)：
- ESM 中有 1 個對象，0 條件。CLI 未列印目標引用；如果目標位置重要，則需要更深層的 QUST 對象轉儲。

## 演出表 (主體 / 說話者) (Cast (subject / speakers))

玩家作為**騎士「瓦拉」(Varla)** 體驗這段記憶 (幾乎在每一行皇帝的對話中都是第二人稱受話者；瓦拉也是遊戲後期的重要首領——見 [`0E6A48 zzzCHBossVarla "Varla the Human Hunter"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577))。記憶中交談的 NPC：

| 角色 | NPC | 筆記 |
|---|---|---|
| 皇帝 (主體) | [`137E63 zzzCHBelharzaMemory "Belharza the Man"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577) | 皇帝分支 (`B01`–`B05`)；瓦拉的養父 (從「你真正的兒子」/「作為父親」推論)。 |
| 伊諾拉 (Enola) (孩童) | [`137E65 zzzCHEnolaMemory "Enola"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:578) | 瓦拉饒過的一名艾萊德 (Ayleid) 倖存孩童；她的頭骨是一件物品：[`13965E zzzCHEnolaSkullFull "Enola's Skull"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:977)。 |
| 賈詹 (Ja'zhan) (記憶) | [`139094 zzzCHJazhanMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:582) | 凱吉特漁夫；為賈詹分支的 `GetIsID` 說話者。 |
| 里索 (Ritho) (記憶) | [`23611E zzzCHRithoMemory "Ritho"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:695) | 巨人騎士，瓦拉的戰友；為里索分支的 `GetIsID` 說話者。 |
| 吟遊詩人 | 別名 `#5` (CLI 在此未解析) | 在出發時歌唱「埃羅伊莎 (Eroisa) 與波利多爾 (Polydor)」的故事。 |

## 編排骨幹 (程序包，而非場景) (Staging Backbone (packages, not scenes))

無 `SCEN` 記錄。強制問候 AI 程序包承載了編排：

- [`139C40 zzzCHMeq3EmperorForceGreet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577) (推論：皇帝走向瓦拉)
- [`139C38 zzzCHMeq3BardForceGreet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577)
- `139C3B zzzCHMeq3EnolaForceGreet`, `139C39 zzzCHMeq3EnolaCaptive`, `139C3A zzzCHMeq3EnolaFollowPlayer` —— 伊諾拉變為俘虜 → 跟隨玩家 (被護送離開的饒恕孩童)。

INFO 條件使用的別名索引 (`GetIsAliasRef`)：
- 別名 `#0` — 皇帝 (貝爾哈扎) —— 所有 `EmperorB0x` 和 `B04T02` INFO。
- 別名 `#1` — 伊諾拉 —— `EnolaB01`/`EnolaB02` INFO。
- 別名 `#5` — 吟遊詩人 —— `BardB01` INFO。
- 賈詹和里索是透過 NPC FormID 的 **`GetIsID`** 受限的，而非透過別名。

## 對話分支 (Dialogue Branches)

所有話題皆為 `cat=Topic sub=Custom SNAM=CUST prio=50`，屬於任務 `13965A`。來源對話行是蹩腳的機器翻譯英文；繁體中文翻譯已盡力呈現，並在未解決之處附上「註：」。繁體中文版保持忠實，未遺漏任何行。

### 皇帝分支 B01 — `139660` ([開端](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1790))

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139661 …EmperorB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1790) | `139662` | `SayOnce, WalkAway` | 別名 `#0` | 「拿下麥卡門泰 (Mackamentain) 是場硬仗。但這還不能說我們離馬拉達 (Malada) 更近了一步。」 註：`Mackamentain` 為地名/人名，待驗證。 |
| [`139663 …EmperorB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1793) | `139664` | `SayOnce`; 結束時執行 VMAD `CHMeq3_TIF__02139664.Fragment_0` | 別名 `#0` | 提示：「我深感榮幸，陛下。」 回覆：「瓦拉，別這麼嚴肅。血緣雖出乎意料卻相連——但我把你當作真正的、我的兒子來信任。」 註：原文「Blood unexpected yet connected」語意不清。 |

### 皇帝分支 B02 — `139665`

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139666 …EmperorB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1797) | `139667` | 無 | `GetStage==20`; 別名 `#0` | 提示：「您為何執著於馬拉達 (Malada)？」 回覆：「艾萊西亞 (Alessia) 教團想把它當作禱告之地。他們說在那裡禱告也能窺見希扎爾 (Shezarr) 的下落。若能找到希扎爾的下落，就沒有理由不攻下馬拉達——這也是為了帝國。」 |

### 皇帝分支 B03 — `139668`

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139669 …EmperorB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1801) | `13966A` | 無 | `GetStage==20`; 別名 `#0` | 提示：「美瑞蒂亞 (Meridia) 的奧若蘭 (Auroran) 出現了。」 回覆 (厭惡/憤怒/恐懼)：「那女人壞到不肯罷手。尤瑪里爾 (Umaril) 的落敗讓她非常不甘，否則她不會出手相助沒落的艾萊德 (Ayleid)。」／「艾萊德也真是的，在 Shiki 神廟裡鬧出這種事。若早點放棄，他本不必死。」／「真是蠢透了。必須加速推行艾萊西亞教義。那麼，我該和波加斯 (Borgas) 談一談。」 註：`Umariru` = 尤瑪里爾、`Shiki`、`Borgas` 為專有名詞，待驗證。 |

### 皇帝分支 B04 — `13966B` (選擇)

此分支包含**分歧點**。瓦拉被命令處死倖存的艾萊德孩童；玩家選擇服從 (`B04T02`) 或拒絕 (`B04T03`→…→`T07`)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`13966C …EmperorB04T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1806) | `13966D` | `WalkAway` | `GetStage==20`; 別名 `#0` | 提示：「那名倖存者，我們該怎麼處置？」 回覆：「瓦拉，即使是婦孺，艾萊德也必須處死。記住，身為帝國騎士要捨棄那份多愁善感。」 |
| [`13966E …B04T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1810) | `13966F` | `Goodbye, SayOnce`; 開始時執行 VMAD `CHMeq3_TIF__0213966F.Fragment_0` | 別名 `#0` | 提示：「是，陛下……」 回覆：「很好，瓦拉。殺掉艾萊德。好的艾萊德只有死掉的那種。」 —— **服從分支 (殺掉孩童)**。 |
| [`139670 …EmperorB04T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1813) | `139671` | `WalkAway` | 別名 `#0` | 提示：「我也流著艾萊德之血。」 回覆：「為何……你竟知道此事？不，看得出來。是那個怪異吟遊詩人暗示你的嗎？聽著，艾萊德拋棄了你——他們把剛出生的你丟進魯梅爾 (Rumare) 湖。若非伊姆加 (Imga) 的先知拾起你，你早成了魚食。即便如此，你仍要選擇艾萊德的血嗎？」 註：`Imuga` = 伊姆加 (Imga)，待驗證。 |
| [`139672 …EmperorB04T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1818) | `139673` | `WalkAway` | 別名 `#0` | 提示：「我的選擇不是血脈。」 回覆：「瓦拉，把你當作我真正的兒子，我才慎重地說。我不是把你收作騎士了嗎？人之子啊，我本該把你當作希扎爾 (Shezarr) 之子養大。難道只能為帝國吶喊、揮劍嗎？」 |
| [`139674 …EmperorB04T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1822) | `139675` | `WalkAway` | 別名 `#0` | 提示：「身為父親，我一直渴望 (您的認可)，陛下。」 回覆：「我關心你。所以你必須處死那個誤導你的艾萊德。你明白我的意思嗎？」 |
| [`139676 …EmperorB04T06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1825) | `139677` | `WalkAway` | 別名 `#0` | 提示：「我準備好了。為了那小女孩的性命，求您。」 回覆：「……既然如此，也罷。就到今天為止吧。帶著那不潔的小女孩，去你想去的任何地方。」 —— **饒恕分支 (放走伊諾拉)**。 |
| [`139678 …EmperorB04T07`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1828) | `139679` | `Goodbye, SayOnce`; 開始時執行 Fragment_1, 結束時執行 VMAD `CHMeq3_TIF__02139679` Fragment_0 | 別名 `#0` | 提示：「謝謝您，陛下。」 回覆：「……三天後，往艾利諾 (Alinor) 的最後一班船。搭上它。」 |

### 皇帝分支 B05 — `13967A` (壞結局門檻)

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`13967C …EmperorB05T01b`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1831) | `13967D` | `Goodbye` | `GetStage==30`; 別名 `#0` | 回覆：「去把那女孩殺了。我是為你好才說的。」 |

**極性 (推論，基於來源)：** `B05T01b` 受限於 `GetStage==30` (第一個 `CompleteQuest`)，且其內容重申了殺戮命令 → **階段 30 = 「殺掉孩童 / 服從」(壞結局/墮落) 的完成路徑**。拒絕鏈 (`B04T03`→`T07`) 以前往艾利諾告終，且伊諾拉跟隨玩家離開 (伊諾拉跟隨/俘虜程序包，`EnolaB02` 在 `GetStage<100` 時喊「媽……」) → **階段 130 = 「饒恕伊諾拉 / 仁慈」(好結局) 的完成路徑**。兩個 `CompleteQuest` 階段以此映射：**30 = 服從/殺戮 (壞)，130 = 饒恕/流亡 (好)** (從條件限制與分支內容推論而來；階段日誌中未註明，日誌為空)。

### 吟遊詩人分支 B01 — `139C25`

說話者：別名 `#5` (吟遊詩人)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139C26 …BardB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1834) | `139C27` | `WalkAway` | 別名 `#5` | 「噢，是瓦拉 (Varla) 大人嗎？聽說您捨棄了騎士的身分。」 |
| [`139C28 …BardB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1837) | `139C29` | `WalkAway` | 別名 `#5` | 提示：「你的來意是？」 回覆：「正逢您期盼已久的啟程，我想為您獻上一曲。我要唱埃羅伊莎 (Eroisa) 與波利多爾 (Polydor) 的故事。」 註：`Eroisa`、`Polydor` 待驗證。 |
| [`139C2A …BardB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1840) | `139C2B` | `Goodbye, SayOnce`; 結束時執行 VMAD `CHMeq3_TIF__02139C2B.Fragment_0` | 別名 `#5` | 提示：「請容我推辭。你的歌太悲傷了。」 回覆：「真可惜。有緣再續此曲。那麼，祝您一路順風。」 |

### 伊諾拉分支 B01 — `139C2D`

說話者：別名 `#1` (伊諾拉)。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139C2E …EnolaB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1843) | `139C2F` | `WalkAway` | 別名 `#1` | 「我們搭這艘船要去哪裡？」 |
| [`139C30 …EnolaB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1846) | `139C31` | `WalkAway` | 別名 `#1` | 提示：「艾利諾 (Alinor)。精靈之島。」 回覆：「那裡是個好地方嗎？」 |
| [`139C32 …EnolaB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1849) | `139C33` | `Goodbye`; 結束時執行 VMAD `CHMeq3_TIF__02139C33.Fragment_0` | 別名 `#1` | 提示：「一定是個好地方。來吧，我們走。」 回覆：「嗯。」 |

### 伊諾拉分支 B02 — `139C3C`

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139C3D …EnolaB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1855) | `139C3E` | `Goodbye` | `GetStage<100`; 別名 `#1` | 回覆 (悲傷)：「媽……」 註：原文 `Mam...`。`GetStage<100` 表示僅在前半段 (尚未進入 100+ 結局段) 有效。 |

### 賈詹分支 B01 — `139C35`

說話者受限於 [`139094 zzzCHJazhanMemory`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:582) 的 `GetIsID`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`139C36 …JazhanB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1852) | `139C37` | `Goodbye` | `GetIsID 139094` | 提示：「釣得好嗎？」 回覆：「不行啊。我餵艾萊西亞 (Alessia) 金幣，魚根本不上鉤。可惡的吟遊詩人，凱吉特 (Khajiit) 被騙了。」 註：原文 `Kajito` = 凱吉特。 |

### 里索分支 B01 — `236131`

說話者受限於 [`23611E zzzCHRithoMemory "Ritho"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:695) 的 `GetIsID`。單個話題，四個 INFO (隨機池 + 一個晚期階段變體)。

| INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|
| `236133` | `Goodbye, Random` | `GetStage<=40`; `GetIsID 23611E` | 「貝爾哈扎 (Belharza) 大人太性急了……我們不必攻下這座城……」 |
| `236134` | `Goodbye, Random` | `GetStage<=40`; `GetIsID 23611E` | 「那裡只有婦女、孩童和手無寸鐵的祭司。這稱不上戰爭……」 |
| `236135` | `Goodbye, Random, RandomEnd` | `GetStage<=40`; `GetIsID 23611E` | 「瓦拉 (Varla)，我的朋友。這場戰爭的目的究竟是什麼……我不懂小個子們的想法。」 |
| `236136` | `Goodbye` | `GetStage>=100`; `GetIsID 23611E` | 「願你健康，瓦拉。把帝國交給我們，走你自己的路吧。」 |

話題錨點：[`236132 zzzCHMeQ03RithoB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2361)。`GetStage>=100` 的 INFO 確認了 100+ 波段是分歧後的「好結局」/流亡路徑 (里索送別瓦拉，而非送他上戰場)。

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 此記憶任務為 [`13965A zzzCHMemoryQuest03 "Knight of Hound"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154)，目標為 [`Blood never separate, but join.`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:155) (血脈從不分離，而是相連)。
- 玩家重現了騎士**瓦拉**的記憶，在馬拉達 (Malada) 被洗劫後，奉**貝爾哈扎皇帝**之命處死一名倖存的艾萊德孩童**伊諾拉**。
- **無場景 (SCEN) 記錄。** 跨越 7 個對話分支的 11 個自定義話題：皇帝 `B01`–`B05`、吟遊詩人 `B01`、伊諾拉 `B01`/`B02`、賈詹 `B01`、里索 `B01`。
- 兩個完成路徑：階段 **30** (服從/殺戮，由 `GetStage==30` 時的 `B05T01b` 重申) vs 階段 **130** (拒絕/饒恕；伊諾拉透過跟隨/俘虜程序包被護送到艾利諾)。極性 30 = 壞 / 130 = 好是從條件門檻與內容中**推論**而來的，而非來自空的階段日誌。
- VMAD TIF 片段在關鍵的玩家選擇上觸發：`02139664` (深感榮幸)、`0213966F` (服從)、`02139679` (饒恕→艾利諾)，以及告別片段 `02139C2B` / `02139C33`。確切的 Papyrus 行為此處未解碼。

待驗證事項 (Open verification)：
- 反編譯 `CHMeq3_TIF__0213966F` (服從) 和 `CHMeq3_TIF__02139679` (饒恕) 以確認它們分別設置了階段 30 與 130 —— 這將使極性推論變為事實。
- 直接轉儲 QUST 別名以確認別名 `#0` = 貝爾哈扎, `#1` = 伊諾拉, `#5` = 吟遊詩人，並識別 `#5` 吟遊詩人的引用。
- 解析拼寫模糊的專有名詞：`Mackamentain` (麥卡門泰), `Eroisa`/`Polydor` (埃羅伊莎/波利多爾), `Imuga` (伊姆加/Imga), `Shiki`, `Borgas` (波加斯), `Umariru` (尤瑪里爾/Umaril)。
- 此任務未擁有任何故事書籍 (僅存在 `zzzCHBalConjureVarla`/`zzzCHBalConjureRitho`「巴爾碎片」法術物品) —— 確認並非有意為之。
