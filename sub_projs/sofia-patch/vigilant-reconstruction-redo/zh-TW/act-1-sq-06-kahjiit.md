# 第一章 任務 06 - 虎人如是說 (Also sprach Kahjiit)

狀態：第一個重製片段。基於原始碼，連結優先，非劇情摘要。

來源策略：
- 原始對話行連結回提取的來源檔案，而非完整複製。
- 僅在需要解釋翻譯問題或階段限制時才顯示短小的原始碼片段。
- `SCEN` 編排來自 CLI 診斷（此任務未發現場景紀錄）。

## 任務紀錄 (Quest Record)

[`009E68 zzzAoMMq06 "Also sprach Kahjiit"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69)

CLI：
- `questdiag Vigilant.esm 0x009E68`
- `infodiag Vigilant.esm 0x009E68`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x009E68`
- EditorID: `zzzAoMMq06`
- 名稱 (Name): `Also sprach Kahjiit`
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
| 25 | 無 | 空白 |
| 30 | 無 | 空白 |
| 35 | 無 | 空白 |
| 40 | 無 | 空白 |
| 50 | 無 | 空白 |
| 60 | 無 | 空白 |
| 65 | 無 | 空白 |
| 70 | 無 | 空白 |
| 79 | 無 | 空白 |
| 80 | 無 | 空白 |
| 90 | CompleteQuest | 「謝謝你。喬凡尼非常感謝你。」 |
| 255 | ShutDownStage | 空白 |
| 999 | FailQuest | 空白 |
| 9999 | CompleteQuest | 空白 |

目標 (Objectives)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:71) | 與阿爾塔諾對話 (Talk to Altano) |
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:72) | 在破碎大鍋與阿爾塔諾見面 (Meet Altano in the Ragged Flagon) |
| 20 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:73) | 尋找喬凡尼 (Find Jo'vanni) |
| 21 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:74) | 聽取美瑞蒂亞信徒的建議（選項） (Take advice from Meridia believer (Option)) |
| 25 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:75) | 你殺了我，喬凡尼？為什麼？喬凡尼只是想見坎帕內拉！！為什麼！？ |
| 50 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:76) | 它就在我體內，喬凡尼！！求求你，把它從喬凡尼體內弄出來！！ |
| 60 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:77) | 從瑪索手中奪回坎帕內拉，而非喬凡尼！！拜託，滿足喬凡尼最後的請求！！ |
| 70 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:78) | 喬凡尼等不及了！！動作快！！喬凡尼想盡快見到坎帕內拉 |
| 80 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:79) | 向阿爾塔諾回報 (Report to Altano) |

目標對象 (Objective targets)：
- ESM 中有 9 個目標，每個目標各有 1 個對象，`questdiag` 未列出明確條件。
- 若目標位置很重要，則需要更深入的 QUST 目標轉儲以獲取精確的對象引用。

## 別名 / 編排主幹 (Alias / Staging Backbone)

此任務定義了至少 17 個別名（索引 0–16，由 `infodiag` 條件引用 `GetIsAliasRef` 推斷而來）。

主任務：
- [`009E68 zzzAoMMq06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69)

別名映射（推論自 `infodiag` 別名條件）：

| 別名 | 角色 | 推論填入類型 |
|---:|---|---|
| 0 | 任務夥伴 (阿爾塔諾) | forcedRef 至 NPC 別名 |
| 3 | 喬凡尼 (瘋狂的虎人) | forcedRef 或 uniqueActor |
| 4 | 瑪索 (虎人，佔有了坎帕內拉) | forcedRef 或 uniqueActor |
| 5 | 坎帕內拉 (NPC / 記憶) | forcedRef 或 uniqueActor |
| 16 | 美瑞蒂亞信徒 (對話夥伴) | forcedRef 至 NPC |

推論：
- 別名 `#0` (阿爾塔諾) 透過 `GetIsAliasRef == 1` 檢查開啟所有任務分支。
- 別名 `#3` (喬凡尼) 受限於階段 20–25（尋找並與瘋狂虎人對話）。
- 別名 `#4` (瑪索) 與 `#5` (坎帕內拉記憶) 受限於階段 30–60（記憶 / 居家場景）。
- 別名 `#16` (美瑞蒂亞信徒) 開啟信徒分支主題與對話。
- `infodiag` 輸出中未列出明確的 SCEN 紀錄；此為純對話任務。

## 主任務分支：鼠道魔族召喚

開場主題序列，階段 0–25，與夥伴阿爾塔諾的對話。

### `00A3D6 zzAoMMq06B1AboutMission6` (主題/自訂)

開啟條件：`GetStage LessThan 10` + `GetIsAliasRef == 1` (別名 #0, 阿爾塔諾)

提示：[About Ratway](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:221)
（翻譯：關於鼠道）

| 回應 | 情感 | 回應文本 |
|---|---|---|
| 1 | Happy | [Ratway...I have Friends in Ragged Flagon. I will get information from them.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:222) |
| 2 | Happy | [I will go forward. If you are ready, come on.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:223) |

（翻譯：回應 1「鼠道……我在破碎大鍋有朋友。我會從他們那裡獲取資訊。」回應 2「我先走一步。如果你準備好了，就跟上。」）

VMAD: `AoM06_TIF__0100A3D7.Fragment_0` (OnEnd)

推論：INFO 0x00A3D7 可能在片段結束時將階段推進至 10。

### `00A3D9 zzAoMMq06B2AboutRitual` (主題/自訂)

開啟條件：`GetStage >= 10` 且 `GetStage < 20` + `GetInCell == 1` (Skyrim.esm 0x016BCF, 鼠道) + `GetIsAliasRef == 1` (別名 #0, 阿爾塔諾)

提示：[What did you discover about Daedra conjuring in Ratway?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:225)
（翻譯：關於鼠道的魔族召喚，你發現了什麼？）

| 回應 | 情感 |
|---|---|
| 1 | Disgust |

回應文本：[I heard that Kajiit called Jo'vanni summon Daedra. You examine that Khajiit, I will search Daedra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:226)
（翻譯：「我聽說有個叫喬凡尼的虎人在召喚魔族。你去調查那個虎人，我去搜尋魔族。」）

VMAD: `AoM06_TIF__0100A3DA.Fragment_0` (OnEnd)

推論：將階段從 ~10–20 推進至階段 20 (尋找喬凡尼)。

## 喬凡尼遭遇：瘋狂虎人分支

階段 20–25，與喬凡尼對話（別名 #3，受階段限制）。

### `00A3DC zzAoMMq06B3Crazycat` (主題/自訂)

開啟條件：`GetStage >= 20` 且 `GetStage < 25` + `GetIsAliasRef == 1` (別名 #3, 喬凡尼)

提示：（未列出；問候語）

| 回應 | 情感 |
|---|---|
| 1 | Puzzled |

回應文本：[Jo'vanni is looking for Campaner'Ra. My Prescious Campaner'Ra!! Where are you!?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:229)
（翻譯：「喬凡尼在找坎帕內拉。我親愛的坎帕內拉！！妳在哪裡！？」）

推論：喬凡尼的開場獨白，無階段推進。

### `00A3DE zzAoMMq06B3WhoWoman` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #3)

提示：[Who is golden woman?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:231)
（翻譯：誰是金色女人？）

| 回應 | 情感 |
|---|---|
| 1 | Puzzled |
| 2 | Happy |

回應文本：
- [Jo'vanni noticed....Jo'vanni is very smart. The liver of triangular rat is not good...Jo'vanni noticed!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:232)
- [Your liver....Septim by your liver !! Jo'vanni say like Jo'vanni said!! Campaner'Ra will also say!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:233)

（翻譯：回應 1「喬凡尼注意到了……喬凡尼很聰明。三角形老鼠的肝臟不好……喬凡尼注意到了！」回應 2「你的肝臟……用你的肝臟換賽普汀！！喬凡尼說就像喬凡尼說的那樣！！坎帕內拉也會這麼說！！」）

VMAD: `AoM06_TIF__0100A3DF.Fragment_0` (OnEnd)

推論：玩家的選擇決定了分支（可能影響之後的不同結果）。

### `00A3E0 zzAoMMq06B3CarzyTalk` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #3)

提示：[You summoned Daedra, is that true?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:235)
（翻譯：你召喚了魔族，是真的嗎？）

| 回應 | 情感 |
|---|---|
| 1 | Happy |
| 2 | Sad |

回應文本：
- [Of course!!Jo'vaannin knows becouse Jo'vanni septimed! Septimed By Round Skooma and a liver of triangular rat!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:236)
- [But..but..Nothing is come!! Jo'vanni septimed as golden woman tell me, Jo'vanni!! Why?Jo'vanni?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:237)

（翻譯：回應 1「當然！！喬凡尼知道，因為喬凡尼充能了！用圓形斯庫瑪和三角形老鼠的肝臟充能了！！」回應 2「但是……但是……什麼都沒出現！！喬凡尼按照金色女人告訴我的那樣充能了，喬凡尼！！為什麼？喬凡尼？」）

推論：闡述了喬凡尼對召喚魔族與坎帕內拉的痴迷。無階段推進；純對話。

翻譯筆記：
- 「三角形老鼠的肝臟 (Liver of triangular rat)」語意不明；可能指斯庫瑪配料或是某種象徵性的侮辱。保留字面翻譯。
- 「Septimed」似乎是一個新詞（源自 Septim），意指「被附魔」或「被賦予力量」（在此翻譯為「充能」）。

## 記憶 / 居家場景分支

階段 30–60，在居家環境（可能是記憶序列）中與瑪索（別名 #4）和坎帕內拉（別名 #5）對話。

### `00A3E3 zzAoMMq06B4Memory` (主題/自訂)

開啟條件：受階段限制 (30, 35, 40, 60)；`GetIsAliasRef == 1` (別名 #5, 坎帕內拉，或別名 #4, 瑪索)

提示：（未列出；問候語/環境對話）

| INFO | 階段 | 條件 | 回應 | 情感 |
|---|---|---|---|---|
| 0x00A3E4 | 30 | `GetIsAliasRef == 1` 別名 #5 | [Jo'vanni! Wake up, Jo'vanni! It is morning!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:240) | Anger |
| 0x00A3E9 | — | `GetSitting NotEqualTo 3` + 別名 #5 | [Sit down, Jo'vanni. A stand-up meal is bad manner.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:241) | Anger |
| 0x00A3EA | 35 | `GetIsAliasRef == 1` 別名 #5 | [This is self confident soup today. you will be encahnted.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:242) | Happy |
| 0x00A3F1 | 40 | `GetIsAliasRef == 1` 別名 #4 | [Beautiful pelt. Very beautiful pelt. very...very....very...](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:243) | Happy |
| 0x00A3F8 | 60 | `GetIsAliasRef == 1` 別名 #4 | [Campaner'Ra is warm. Mar'so is happy. Very happy.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:244) | Happy |

（翻譯：0x00A3E4「喬凡尼！醒醒，喬凡尼！天亮了！」0x00A3E9「坐下，喬凡尼。站著吃飯沒禮貌。」0x00A3EA「這是我今天很有自信的湯。你會著迷的。」0x00A3F1「漂亮的毛皮。非常漂亮的毛皮。非常……非常……非常……」0x00A3F8「坎帕內拉很溫暖。瑪索很快樂。很快樂。」）

推論：
- 階段 30–35：坎帕內拉叫醒喬凡尼，提供早餐。
- 階段 40–60：瑪索與坎帕內拉的居家親密互動；瑪索對坎帕內拉的毛皮/皮膚表現出佔有欲。
- 未發現明確的 `SCEN` 紀錄，因此這是沒有場景主幹的環境主題對話。
- `GetSitting` 旗標暗示在階段 ~35 期間有受家具限制的動作。

### `00A3E5 zzAoMMq06B4WakeUP` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #5)

提示：[What...? Campaner'Ra?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:246)
（翻譯：什麼……？坎帕內拉？）

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Wake up! Jo'vanni! Breakfast is ready.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:247)
（翻譯：「醒醒！喬凡尼！早餐準備好了。」）

### `00A3E7 zzAoMMq06B4GotIt` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #5)

提示：[Jo'vanni got it.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:249)
（翻譯：喬凡尼知道了。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Today, I made tomato soup you like. Do go ahead with your soup before it gets cold.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:250)
（翻譯：「今天我做了你喜歡的番茄湯。趁熱趕快喝吧。」）

VMAD: `AoM06_TIF__0100A3E8.Fragment_0` (OnEnd)

推論：在此處退出對話循環；可能推進階段。

### `00A3EB zzAoMMq06B4Skoom` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #5)

提示：[Where is my skooma? Campaner'Ra?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:252)
（翻譯：我的斯庫瑪呢？坎帕內拉？）

優先度：55（高於預設值）

| 回應 | 情感 |
|---|---|
| 1 | Disgust |

回應文本：[Are you half asleep? You promised me to stop skooma?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:253)
（翻譯：「你還沒睡醒嗎？你答應過我要戒掉斯庫瑪的？」）

### `00A3ED zzAoMMq06B4kidding` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #5)

提示：[Just kidding, Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:255)
（翻譯：開玩笑的，坎帕內拉。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Anymore!Soup is getting cold.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:256)
（翻譯：「別再說了！湯要涼了。」）

VMAD: `AoM06_TIF__0100A3EE.Fragment_0` (OnEnd)

### `00A3F2 zzAoMMq06B4Skin01` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #4, 瑪索)

提示：[Mar'so...the pelt of what...?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:258)
（翻譯：瑪索……這是什麼的毛皮……？）

旗標：`WalkAway`

優先度：55

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[This is Campaner'Ra...My precious Campaner'Ra. Mar'so and Campaner'Ra become one soon.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:259)
（翻譯：「這是坎帕內拉……我親愛的坎帕內拉。瑪索很快就要和坎帕內拉合而為一了。」）

推論：瑪索佔有性地提到坎帕內拉的毛皮/皮膚。「合而為一」暗示了一種親密或轉化行為（可能是剝皮，或是隱喻上的結合）。

### `00A3F4 zzAoMMq06B4Skin02` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #4)

提示：[Why....Mar'so...Why!?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:261)
（翻譯：為什麼……瑪索……為什麼！？）

旗標：`WalkAway`

優先度：54

| 回應 | 情感 |
|---|---|
| 1 | Sad |

回應文本：[Campaner'Ra won't look Mar'so. but, Mar'so wants to be with Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:262)
（翻譯：「坎帕內拉不肯正眼看瑪索。但瑪索想和坎帕內拉在一起。」）

推論：坎帕內拉（角色）拒絕了瑪索的追求。

### `00A3F6 zzAoMMq06B4Skin03` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #4)

提示：[Jo'vanni never excuse you, Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:264)
（翻譯：喬凡尼絕不原諒你，瑪索。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Sad |
| 2 | Happy |

回應文本：
- [Jelaousy? Jo'vanni? Envy is ugly....Mar'so was also ugly...But now, Mar'so is not because Campaner'Ra with Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:265)
- [Goodbye, Jo'vanni. Mar'so and Campaner'Ra set off on our journey. With Campaner'Ra, Mar'so is not cold in winter Skyrim.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:266)

（翻譯：回應 1「嫉妒嗎？喬凡尼？嫉妒是很醜陋的……瑪索以前也很醜陋……但現在瑪索不醜了，因為坎帕內拉和瑪索在一起。」回應 2「再見，喬凡尼。瑪索要和坎帕內拉踏上旅程了。有了坎帕內拉，瑪索在天際的寒冬中就不會冷了。」）

VMAD: `AoM06_TIF__0100A3F7.Fragment_0` (OnEnd)

推論：結束記憶序列；瑪索帶著坎帕內拉離開。玩家的選擇可能影響階段轉換。

### `00A3F9 zzAoMMq06B4BackSkin` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #4)

提示：[Return Campaner'Ra, Mar'so.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:268)
（翻譯：瑪索，把坎帕內拉還來。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Sad |
| 2 | Anger |

回應文本：
- [No!No No No!! With difficulty! Campaner'Ra and Mar'so become one!! Why do you disturb us!! Mar'so hate you!!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:269)
- [...smell bad....like envy...from you!!  you smell like Jo'vanni!! I hate Jo'vanni! ](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:270)

（翻譯：回應 1「不！不不不！！好不容易！坎帕內拉才和瑪索合而為一！！你為什麼要打擾我們！！瑪索討厭你！！」回應 2「……好臭……你身上有嫉妒的味道！！你聞起來就像喬凡尼！！我討厭喬凡尼！」）

VMAD: `AoM06_TIF__0100A3FA.Fragment_0` (OnEnd)

推論：若玩家試圖強行奪回坎帕內拉時的替代結局。

## 任務完成分支

階段 80–90，最終與阿爾塔諾對話進行回報。

### `00A3FC zzAoMMq06B5Mission6Comp` (主題/自訂)

開啟條件：`GetStage == 80` + `GetIsAliasRef == 1` (別名 #0, 阿爾塔諾)

提示：[The matter about Khajiit is done. Also defeated Daedra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:272)
（翻譯：關於虎人的事情辦妥了。魔族也被擊敗了。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Really? I am very glad to have a excellent partner like you. Return to our base. Summoner may be caught.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:273)
（翻譯：「真的嗎？我很高興能有你這麼優秀的夥伴。回基地吧。召喚師可能已經被抓到了。」）

VMAD: `AoM06_TIF__0100A3FD.Fragment_0` (OnEnd)

推論：將階段從 80 推進至 90 (`CompleteQuest`)，結束任務。

## 環境 / 問候主題

### `4CCD81 zzzAoMMq06Hello` (Misc/Hello)

開啟條件：`GetIsAliasRef == 1` (別名 #16, 美瑞蒂亞信徒對話夥伴) + 受階段限制

提示：（無；Hello/問候）

| INFO | 階段條件 | 回應 | 情感 |
|---|---|---|---|
| 0x4CCD82 | `GetStage < 80` | [It's dark here. We need more light. Yes, the light of Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3041) | Happy |
| 0x4CCD83 | (無) | [What lights up the darkness is the light of Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3041) | Happy |

（翻譯：「這裡很暗。我們需要更多光。沒錯，美瑞蒂亞之光！」「照亮黑暗的是美瑞蒂亞之光！」）

推論：美瑞蒂亞信徒的問候語。重複著對美瑞蒂亞之光的痴迷。

### `4CCD84 zzzAoMMq06Goodbye` (Misc/Goodbye)

開啟條件：`GetIsAliasRef == 1` (別名 #16)

提示：（無；Goodbye）

| 回應 | 情感 |
|---|---|
| 1 | Neutral |

回應文本：[Believe Meridia. It is the only salvation.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3043)
（翻譯：「信仰美瑞蒂亞。那是唯一的救贖。」）

## 選項：信徒對話分支

階段 20–25，與美瑞蒂亞信徒（別名 #16）的有條件對話。受階段限制，發生在找到喬凡尼之後，但在對峙瑪索之前。

### `4CDF78 zzzAoMMq06CultB01T01` (主題/自訂)

開啟條件：`GetStage < 25` + `GetIsAliasRef == 1` (別名 #16)

提示：[This place is a cesspool. It suits the Meridian faithful.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3044)
（翻譯：這地方就是個糞坑。倒是挺適合美瑞蒂亞信徒的。）

| 回應 | 情感 |
|---|---|
| 1 | Happy |
| 2 | Happy |

回應文本：
- [I don't like the sound of that, but that's exactly what it is! This place is full of people who don't appreciate the light of Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3045)
- [I must have been guided by Meridia. Give light to these men. ...... Oh, how merciful Meridia is!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3046)

（翻譯：回應 1「我不喜歡這種說法，但事實就是如此！這裡到處都是不識美瑞蒂亞之光的人。」回應 2「我一定是受到了美瑞蒂亞的指引。賜予這些人光芒吧。……喔，美瑞蒂亞是多麼慈悲啊！」）

### `4CDF7A zzzAoMMq06CultB01T02` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #16)

提示：[You're a tough opponent if you can't handle sarcasm.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3048)
（翻譯：如果你聽不出諷刺，那你還真是個難對付的對手。）

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[There are no enemies to those who believe in Meridia. In other words, we are invincible.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3049)
（翻譯：「對於信仰美瑞蒂亞的人來說，沒有敵人。換句話說，我們是無敵的。」）

### `4CDF7D zzzAoMMq06CultB02T01` (主題/自訂)

開啟條件：`GetStageDone NotEqualTo 1` (階段 21 未完成) + `GetStage >= 20` + `GetStage < 25` + `GetIsAliasRef == 1` (別名 #16)

提示：[Can you think of anything that could have summoned Daedra here?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3051)
（翻譯：你能想到這裡有什麼東西能召喚魔族嗎？）

| 回應 | 情感 |
|---|---|
| 1 | Sad |
| 2 | Happy |

回應文本：
- [None. Not even close. I'm sorry I can't help you. I'm sorry.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3052)
- [But I will help you, my friend, if you will say a few words of Hail Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3053)

（翻譯：回應 1「完全沒有。一點頭緒也沒有。很抱歉我幫不上忙。很抱歉。」回應 2「但我會幫你的，我的朋友，只要你說幾句美瑞蒂亞萬歲。」）

VMAD: `AoMMq06_TIF__024CDF7E.Fragment_0` (OnBegin)

推論：玩家可選擇拒絕信徒的幫助或接受信徒的祝福（階段 21 完成標誌著此選擇）。

### `4CDF7F zzzAoMMq06CultB02T02` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #16)

提示：[I don't remember being friends with you.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3055)
（翻譯：我不記得和你做過朋友。）

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Neutral |

回應文本：[Don't be lonely. For me, everything is my friend. And you, of course.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3056)
（翻譯：「別感到寂寞。對我來說，萬物皆我友。當然也包括你。」）

### `4CDF81 zzzAoMMq06CultB02T03` (主題/自訂)

開啟條件：`GetStage < 25` + `GetIsAliasRef == 1` (別名 #16)

提示：[Long live Meridia.(bullshit)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3058)
（翻譯：美瑞蒂亞萬歲。（屁話））

旗標：`Goodbye`

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Now, take it. In front of Meridia's light, everything is dazzling. Even dreams.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3059)
（翻譯：「現在，拿去吧。在美瑞蒂亞之光面前，萬物皆是耀眼的。連夢境也是。」）

VMAD: `AoMMq06_TIF__024CDF82.Fragment_0` (OnEnd)

### `4CDF84 zzzAoMMq06CultB03T01` (主題/自訂)

開啟條件：`GetStageDone == 1` (階段 21 已完成) + `GetStage >= 20` + `GetStage < 25` + `GetIsAliasRef == 1` (別名 #16)

提示：[Is this light really safe to shine on people?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3061)
（翻譯：這光照在人身上真的安全嗎？）

| 回應 | 情感 |
|---|---|
| 1 | Happy |

回應文本：[Only Meridia knows that. The important thing is to believe, and more importantly, to forgive.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3062)
（翻譯：「那只有美瑞蒂亞知道。重要的是信仰，更重要的是寬恕。」）

推論：僅在完成階段 21（信徒祝福）後開啟。

## 喬凡尼死後的信徒主題 (階段 79+)

在喬凡尼死後開啟的主題（階段 79，可能推論自標題或惡魔附身結果）。

### `4CDF87 zzzAoMMq06CultB04T01` (主題/自訂)

開啟條件：`GetStageDone == 1` (階段 79 已完成) + `GetIsAliasRef == 1` (別名 #16)

提示：[Jo'vanni is dead. What's happened?](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3064)
（翻譯：喬凡尼死了。發生了什麼事？）

| 回應 | 情感 |
|---|---|
| 1 | Sad |
| 2 | Happy |

回應文本：
- [A dream is an inner light, and ephemeral. The light is too strong for those who live in dreams. His existence is dazzled along with dreams.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3065)
- [It's sad. But do not be sad. His death will be followed by the next salvation. Now, chant. For the Meridia.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3066)

（翻譯：回應 1「夢境是內在之光，且轉瞬即逝。對於活在夢中的人來說，這光太強烈了。他的存在隨同夢境一起消散在了強光中。」回應 2「很悲傷。但別難過。他的死亡將會帶來下一個救贖。現在，吟誦吧。為了美瑞蒂亞。」）

推論：信徒對喬凡尼之死的扭曲解讀——將其框定為啟蒙（「消散在強光中」）而非悲劇。

### `4CDF89 zzzAoMMq06CultB04T02` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #16)

提示：[You, come on, man.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3068)
（翻譯：你這人真是……夠了。）

| 回應 | 情感 |
|---|---|
| 1 | (未指定) |

回應文本：[The thought of saving someone with mortal is unthinkable. It is this conceit that leads to tragedy. You should know that.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3069)
（翻譯：「想以凡人之力拯救某人是不可理喻的。正是這種自負導致了悲劇。你應該明白這一點。」）

### `4CDF8B zzzAoMMq06CultB04T03` (主題/自訂)

開啟條件：`GetIsAliasRef == 1` (別名 #16)

提示：[Long live Meridia (frustrated).](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3071)
（翻譯：美瑞蒂亞萬歲（沮喪地）。）

| 回應 | 情感 |
|---|---|
| 1 | (未指定) |

回應文本：[M, E, R, I, D, I, A!! Glory to the great Meridia, For the Meridia, For the Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3072)
（翻譯：「M, E, R, I, D, I, A！！榮耀歸於偉大的美瑞蒂亞，為了美瑞蒂亞，為了美瑞蒂亞！」）

推論：喬凡尼死後信徒的吟誦。

## 壞結局分支：瑪索自殺

（索引中有引用，但詳情不在此處；參見獨立片段 `act-1-sq-06-badend.md`）

壞結局 Hello 主題的問候語（預覽）：

[`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3074)：
- [No more interruptions. It's just you and me now, Campaner'Ra.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3075)
- [I'll be here forever, Campanella. Always and forever.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3076)
- [Here in the deep end of the pond, no one can disturb us anymore. Even Jo'vanni wouldn't be able to come here.](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3077)
- [Hail Meridia in hard times and sad times, oh, hail Meridia!](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3078)

（翻譯：「再也沒有干擾了。現在只有妳跟我了，坎帕內拉。」「我會永遠待在這裡，坎帕內拉。直到永遠。」「在水池深處這裡，再也沒有人能打擾我們了。就連喬凡尼也沒辦法來到這。」「在艱難與悲傷的時刻讚美美瑞蒂亞，喔，讚美美瑞蒂亞！」）

## 相關紀錄

這些是在任務對話中引用，但未明確列在 `infodiag` 輸出中的相關 NPC / 物品。

NPCs：
- [`0EFC32 zzzCHSummonAltano` - 阿爾塔諾 (Altano)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:270) (召喚變體)
- [`001841 zzzAoMCatMale01` - 喬凡尼 (Jo'vanni)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:859) (瘋狂的虎人)
- [`001844 zzzAoMCatFemale01` - 坎帕內拉 (Campaner'Ra)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:884) (女性虎人，喬凡尼痴迷的對象)

## 重建筆記

基於原始碼：
- 此任務由 [`009E68 zzzAoMMq06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:69) 代表，名稱為「虎人如是說 (Also sprach Kahjiit)」。
- 包含**無 SCEN 紀錄**；所有編排皆基於對話，透過階段限制來控制主題的可用性。
- 任務結構圍繞著**三個主要子敘事**：
  1. 鼠道調查（階段 0–25）：玩家與阿爾塔諾揭露了魔族召喚；可選的美瑞蒂亞信徒支線。
  2. 記憶序列（階段 30–60）：展現了喬凡尼與瑪索及坎帕內拉的居家生活；玩家見證/介入。
  3. 喬凡尼的命運（階段 65–90）：暗示死亡或附身（階段 79）；玩家向阿爾塔諾回報。
- **信徒替代路徑**：玩家可以接受美瑞蒂亞信徒的祝福（階段 21），這會開啟不同的對話分支並重新定義事件。
- **壞結局標記**：階段 999 (`FailQuest`) 與獨立的任務紀錄 `4CDF8D zzzAoMMq06BadEnd` 暗示了一個瑪索留住坎帕內拉的替代結局。

分支極性（推論）：
- **主要路徑**：從瑪索手中救出坎帕內拉；擊敗喬凡尼的附身或瘋狂；回到阿爾塔諾處（階段 90，`CompleteQuest`）。
- **信徒結盟路徑**：獲得美瑞蒂亞信徒的祝福（階段 21），導致喬凡尼之死被重新詮釋為啟蒙。
- **壞結局路徑**（階段 999，獨立任務）：瑪索 + 坎帕內拉 + 坎帕內拉的毛皮；玩家無法救回她。

開放驗證：
- 反編譯 `AoM06_TIF__*` VMAD 片段（OnEnd 腳本），以確定每個對話選擇的精確階段轉換。
- 直接轉儲 QUST 別名以確認填入內容：哪些是 forcedRef（特定 NPC），哪些是 uniqueActor（持久性角色）或其他。
- 驗證喬凡尼在階段 50 的觸發（「它就在我體內，喬凡尼！！」）——推論：受魔族附身；透過 VMAD 或任務觸發器確認。
- 驗證瑪索/坎帕內拉的「毛皮 (skin)」機制：是模型替換、物品交換，還是在敘事中的隱喻？
- 檢查目標中引用的地點（鼠道、破碎大鍋），以及別名是否將 NPC 置於這些儲存格中，或者玩家是否必須自行導航。
- 檢查階段 65–79 是否有明確的日誌項目或 `CompleteQuest` 旗標（`questdiag` 輸出顯示階段 79 為空；階段 90 具有 `CompleteQuest`）。
