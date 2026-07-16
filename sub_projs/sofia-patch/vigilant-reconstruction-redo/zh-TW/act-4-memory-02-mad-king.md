# 第四章記憶 02 — 瘋王 (Act 4 Memory 02 - The Mad King)

狀態：重做切片。基於來源 (Source-grounded)，連結優先，非劇情摘要。

來源方針：
- 原始文本行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋翻譯問題時才出現短小的來源片段。
- `SCEN` (場景) 編排來自 CLI 診斷，因為提取的 `dialogue.md` 僅保留了場景話題文本 (且這些場景是由程序包驅動的，完全沒有說話話題)。
- 主體已從此任務自身的 `zzzCHMeQ2King…` 話題以及 `King` 別名的 `uniqueActor` (唯一演員) 確認，而非來自任何次要參考資料。`_gemini-quarantine` 的 `memory-02*.md` 文件虛構了與 ESM 不符的對話 (「月亮……我的月亮……」)；未從中複製任何內容。

## 主體 (Subject)

瘋王是 [`106660 zzzCHDrozel "Mad King Dro'zel"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:284) (瘋王德羅澤爾)。

在此記憶任務中，說話者是記憶變體 [`137126 zzzCHDrozelMemory "Dro'zel"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542)，填寫在 `King` 別名中 (`uniqueActor=137126`)。

- 確認：先前波段的警告 (「不要假設是德羅澤爾」) 在此不適用。此任務自身的話題 EditorID 為 `zzzCHMeQ2King…`，且 `King` 場景別名的 `uniqueActor` 解析為 `zzzCHDrozelMemory`。德羅澤爾**確實**是此記憶任務的主體 (基於來源)，這與他同樣出現的 `zzzCHsq*` 支線任務話題不同。
- 存在一個記憶地點記錄：[`38366D zzzCHMemDrozel "Memory"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:548)。

## 任務記錄 (Quest Record)

[`13712B zzzCHMemoryQuest02 "The Mad King"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)

CLI 指令：
- `questdiag Vigilant.esm 0x13712B`
- `infodiag Vigilant.esm 0x13712B`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x13712B`
- EditorID: `zzzCHMemoryQuest02`
- 名稱: `The Mad King` (瘋王)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `Misc`
- 過濾器: `CH\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 25 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 40 | 無 | 空 |
| 125 | 無 | 空 |
| 130 | CompleteQuest | 空 |
| 140 | 無 | 空 |
| 150 | 無 | 空 |
| 160 | 無 | 空 |
| 999 | ShutDownStage | 空 |

目標 (Objective)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:39) | 瘋狂隨月亮一同墜落。 |
- 註：原文 `Insanity fall down with the moon.` 是語法錯誤的英文；上方的繁體中文呈現了其字面意義。待驗證 — 預期的慣用語不明。

目標對象 (Objective targets)：
- ESM 中有 1 個對象，0 條件。
- 目前的 CLI 輸出不列印目標引用；如果目標位置重要，則需要更深層的 QUST 對象轉儲。

## 分支 / 結果映射 (Branch / Outcome Mapping)

兩波段 `CompleteQuest` 模式：階段 **30** (早期波段) vs 階段 **130** (晚期波段) —— 索引中的好/壞 (業障) 特徵。

兩個對話分支映射到兩個場景和兩個完成路徑：

| 波段 | 階段 | 分支 | 場景 | 極性 (推論) |
|---|---:|---|---|---|
| 早期 | 30 | `B01` (`zzzCHMeQ2KingB01`) | `zzzCHMeQ2Scene01` (`1376E8`) | 好 / 仁慈 —— TODO 待確認 |
| 晚期 | 130 | `B02` (`zzzCHMeQ2KingB02`) | `zzzCHMeQ2BadScene` (`1376EB`) | 壞 / 墮落 —— TODO 待確認 |

- 推論依據：第二個場景的 EditorID 字面上就是 `zzzCHMeQ2BadScene` (壞結局場景)，以此命名壞結局分支；其演員標誌 (`DeathEnd`, `CombatEnd`, `DialoguePause`) 與敵對/死亡結果相符，且它增加了第二個演員 (`Molag`, 別名 #4)，這是好結局場景所沒有編排的。因此，`BadScene` 被解讀為晚期波段 (130) 的壞結果，而 `Scene01` 為早期波段 (30) 的好結果。
- `B01` 開端話題 `zzzCHMeQ2KingB01T01` (`137130`) 受限於 `GetStage == 10` + 別名 #1 (King)；`infodiag` 在 `B02` 的開端話題上未列印任何特定分支的 `GetStage` 門檻，因此玩家選擇引導的確切「階段→分支」接線**尚未完全從 `infodiag`/`questdiag` 中確定**。上述分支→結果的分配是推論，並非經過字節驗證的。請透過場景階段的完成條件 (completeConds) 和 VMAD 階段片段進行確認 (見「待驗證事項」)。
- 相關的壞結局記錄 (交叉參考，所有權未驗證)：[`5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) — `infodiag` 未將其列在 `13712B` 下；在使用前請先驗證。

## 別名 / 編排骨幹 (Alias / Staging Backbone)

下述兩個 `SCEN` (場景) 記錄共用相同的宿主任務和別名。

宿主任務：
- [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)

來自 `scenediag` 的宿主任務別名：

| 別名 | 名稱 | 填寫方式 |
|---:|---|---|
| 0 | `MemoryMarker` (記憶標記) | 強制引用 `13712A:Vigilant.esm` |
| 1 | `King` (王) | 唯一演員 [`137126 zzzCHDrozelMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) |
| 2 | `ReturnMarker` (返回標記) | 強制引用 `13712C:Vigilant.esm` |
| 3 | `Door` (門) | 強制引用 `136FC6:Vigilant.esm` |
| 4 | `Molag` (莫拉格) | CLI 未列印 (無強制引用/唯一演員 —— 可能透過腳本/條件填寫) |
| 5 | `DrozelMemory` (德羅澤爾記憶) | CLI 未列印 |
| 6 | `GuideMarker` (引導標記) | 強制引用 `42E0B3:Vigilant.esm` |

推論：
- 所有自定義對話 INFO 皆以別名 `#1` (`King` = 德羅澤爾) 的 `GetIsAliasRef == 1` 為條件，因此德羅澤爾是兩個分支的唯一說話者。
- `Molag` (別名 #4) 僅在 `BadScene` 中作為第二個演員編排；可能的填寫內容是莫拉格·巴爾的記憶變體，例如 [`2BC374 zzzCHMemoryMolagBalMad "Molag Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) (推論 —— CLI 未列印此別名的填寫內容；請透過 QUST 別名轉儲確認)。此任務中未出現關於 `Molag` 別名的 INFO 條件，這與 `Molag` 是一個程序包驅動的無聲演員而非對話說話者的情況相符。

## 場景記錄 (Scene Records)

這些場景是**程序包 (Package) 驅動的**：每個動作都是 `Package` 或 `Timer` (計時器)，沒有一個是 `Dialog` (對話) 動作。說出的對話行存在於下方的兩個自定義分支中，而非場景中。

### 1376E8 zzzCHMeQ2Scene01 (好結局 / 早期波段)

CLI 指令：
- `scenediag Vigilant.esm 0x1376E8`

編排：
- 宿主任務：[`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- 標誌：無
- 演員：別名 `#1` (`King`), 別名 `#4` (`Molag`) —— 皆為 `NoPlayerActivation`。
- 階段：3 個，每個階段有 0 個開始條件和 1 個完成條件。
- 動作 (皆為 `King`, 別名 #1)：
  - 索引 1：`Package`, 階段 0。
  - 索引 2：`Package`, 階段 1。
  - 索引 3：`Timer`, 階段 1, `1` 秒。
  - 索引 4：`Package`, 階段 2。
- 註：雖然 `Molag` 在此被列為場景演員，但在 `Scene01` 中沒有針對別名 #4 的動作 —— 只有「王」在行動。(推論：`Molag` 在好結局場景中存在但處於閒置狀態，僅在 `BadScene` 中變為活躍。)

### 1376EB zzzCHMeQ2BadScene (壞結局 / 晚期波段)

CLI 指令：
- `scenediag Vigilant.esm 0x1376EB`

編排：
- 宿主任務：[`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)
- 標誌：無
- 演員：別名 `#1` (`King`) 和 別名 `#4` (`Molag`)，皆帶有行為標誌 `DeathEnd`, `CombatEnd`, `DialoguePause`。
- 階段：3 個 (階段 1 有 2 個完成條件；階段 0 和 2 各有 1 個)。
- 動作：
  - 索引 1：`Package`, 演員 `#1` (`King`), 階段 0。
  - 索引 2：`Package`, 演員 `#1` (`King`), 階段 1→2。
  - 索引 3：`Timer`, 演員 `#1` (`King`), 階段 1, `5` 秒。
  - 索引 4：`Package`, 演員 `#4` (`Molag`), 階段 2。
  - 索引 5：`Timer`, 演員 `#4` (`Molag`), 階段 2, `5` 秒。
- 註：`Molag` 演員僅在此 `BadScene` 中行動 (索引 4/5)，這加強了壞結果會編排莫拉格·巴爾幻象的判讀。

場景程序包 (來自 `find King`, 供參考)：
- `1376E7 zzzCHMeq2KingSitonBed` (床邊久坐)
- `1376E9 zzzCHMeq2KingMoveToDoor` (移向門口)
- `1376EA zzzCHMeq2KingFroceGreet` (強制問候)
- `1376F0 zzzCHMeq2MolagbalRising` (莫拉格·巴爾升起 —— 與 `BadScene` 的索引 4 動作匹配)
- 註：這些是場景動作所引用的程序包記錄；CLI 未列印每個動作的程序包 FormID，因此動作與程序包的繫結是從 EditorID 名稱推論而來的。

## 自定義對話分支：B01 (好波段，階段 30)

分支：
- [`13712E zzzCHMeQ2KingB01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) (對話分支)
- 視圖：`13712D zzzCHMeQ2KingView`

說話者條件模式：
- 所有 INFO 需要別名 `#1` (`King` = 德羅澤爾) 的 `GetIsAliasRef == 1`。
- 開場白還需要任務 `13712B` 的 `GetStage == 10`。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`13712F zzzCHMeQ2KingB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1765) | `137130` | `WalkAway` | `GetStage == 10`; `GetIsAliasRef alias #1` | (悲傷) 「哈薩瑪……」 |
| [`137131 zzzCHMeQ2KingB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1768) | `137132` | `WalkAway` | `GetIsAliasRef alias #1` | 提示：「你也有在擔心什麼嗎？」 回覆 (恐懼)：「那個該死的吟遊詩人唱的埃羅伊莎 (Eroisa) 與波利多爾 (Polydor) 的故事，冒犯了我。」/ (憤怒)：「他為什麼要用那種令人沮喪的口氣說話？」 |
| [`137133 zzzCHMeQ2KingB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1772) | `137134` | `Goodbye` | `GetIsAliasRef alias #1`; 結束時執行 VMAD `CHMeq2_TIF__02137134.Fragment_0` | 提示：「但是，那個故事是真的嗎？」 回覆 (厭惡)：「真不真，都是無關緊要的小事。淨說些空話，真是個拙劣的說書人。別再粉飾這座宅邸了。」 |

翻譯筆記：
- "Hasama" (`137130`) = 索引主體圖中標註的重複出現的名稱 **Hasaama (哈薩瑪)**；保持原樣。待驗證專有名詞拼寫。
- "Eroisa and Polydor" (`137132`) 是國王自稱冒犯了他的吟遊詩人之歌；兩個名字皆保持原樣，待驗證。
- 原文中的 "singind" (`137132`) 是 "singing" 的拼寫錯誤。
- `137134` 整個句子都是蹩腳的英文 (`To talk to fuff`, `Faking the house anymore`)；上方的繁體中文盡力呈現了其意。待驗證 —— 保持原文。

## 自定義對話分支：B02 (壞波段，階段 130)

分支：
- [`1376DC zzzCHMeQ2KingB02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) (對話分支)

說話者條件模式：
- 所有 INFO 需要別名 `#1` (`King` = 德羅澤爾) 的 `GetIsAliasRef == 1`。
- `infodiag` 在這些開端話題上未列印 `GetStage` 門檻。

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`1376DD zzzCHMeQ2KingB02T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1775) | `1376DE` | `SayOnce`, `WalkAway` | `GetIsAliasRef alias #1` | (疑惑) 「那個吟遊詩人是從哪裡來的？」 |
| [`1376DF zzzCHMeQ2KingB02T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1778) | `1376E0` | `WalkAway` | `GetIsAliasRef alias #1`; 開始時執行 VMAD `CHMeq2_TIF__021376E0.Fragment_2` | 提示：「基爾維戴爾 (Gilverdale)，他說……」 回覆 (中性)：「………………」 |
| [`1376E1 zzzCHMeQ2KingB02T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1781) | `1376E2` | `Goodbye` | `GetIsAliasRef alias #1`; 結束時執行 VMAD `CHMeq2_TIF__021376E2.Fragment_0` | 提示：「你打算怎麼做？」 回覆 (開心)：「………………」 |
| [`1376E3 zzzCHMeQ2KingB02T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1784) | `1376E4` | 無 | `GetIsAliasRef alias #1`; 開始時執行 VMAD `CHMeq2_TIF__021376E4.Fragment_2` | 提示：「沒有任何地方是像他那樣的吟遊詩人的歸宿。」 回覆 (開心)：「是啊。我今天就去睡了。為何如此深的情感？因為任何殘酷的故事到了時候也會迎來清晨。」 |
| [`1376E5 zzzCHMeQ2KingB02T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1787) | `1376E6` | `Goodbye` | `GetIsAliasRef alias #1`; 結束時執行 VMAD `CHMeq2_TIF__021376E6.Fragment_0` | 提示：「這樣很好。」 回覆 (中性)：「quitely」 (安靜地) |

翻譯筆記：
- "Gilverdale" (`1376DF` 提示) 是傳說中吟遊詩人提到的專有名詞；保持原樣 (基爾維戴爾)。它也出現在一個無關的 [奴隸筆記 (`0B0826 zzzCHSlaverNote05`)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/books.md:534) 中 (「基爾維戴爾沉沒了兩次。」) —— 僅供交叉參考，並非同一個任務。待驗證。
- `1376E0` / `1376E2` 的回覆在原文中就是 `........................` (無聲/省略號)；保持為「………………」。
- `1376E4` 是蹩腳的英文 (`What deep emotion too because would have gone by the time...`)；繁體中文為盡力呈現之意。待驗證 —— 保持原文。
- `1376E6` 回覆是單個單字 `quitely` ("quietly" 的拼寫錯誤)；保持原樣。待驗證。

## 相關記錄 (Related Records)

用於完整重建的交叉連結；除非 `infodiag` 已確認，否則不主張其屬於 `13712B`。

NPC：
- [`106660 zzzCHDrozel "Mad King Dro'zel"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:284) —— 主世界的德羅澤爾。
- [`137126 zzzCHDrozelMemory "Dro'zel"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542) —— `King` 別名演員 (透過場景別名驗證擁有者)。
- [`12A73D zzzCHDrozelShadow`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:552) —— 德羅澤爾影子變體 (所有權未驗證)。
- [`2BC374 zzzCHMemoryMolagBalMad "Molag Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:777) —— `Molag` 別名填寫內容的候選者 (推論)。
- [`5714DF zzzCHMemoryAyledKing_BadEnd "Ayleid King"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) —— 冠以壞結局之名的記錄；與此任務的關係未驗證。

地點：
- [`38366D zzzCHMemDrozel "Memory"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/locations.tsv:548) —— 德羅澤爾記憶地點。

物品 (以國王為主題，所有權未驗證)：
- [`2BFDAC zzzCHKingAmulet "Amulet of King"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:137) (王者護身符)
- [`2BFDAD zzzCHKingAmuletReplica "Amulet of King (Replica)"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:138) (王者護身符複製品)

書籍：
- 沒有書籍是由 `13712B` 擁有或透過 `infodiag` 直接連結的。未執行 `booktext` (此任務話題中未出現候選書籍 FormID)。唯一匹配 "Gilverdale" 的書籍是上述無關的奴隸筆記。

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 此記憶任務為 [`13712B zzzCHMemoryQuest02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:38)，目標為 [`Insanity fall down with the moon.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:39) (瘋狂隨月亮一同墜落)。
- 主體是德羅澤爾 (透過 `King` 場景別名的 `uniqueActor` 確認為 [`zzzCHDrozelMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:542))；儘管索引中提醒要謹慎，但「德羅澤爾」的假設對**此任務**來說是正確的。
- 包含兩個程序包驅動的 `SCEN` 記錄 (`1376E8 zzzCHMeQ2Scene01`, `1376EB zzzCHMeQ2BadScene`)；皆未使用 `Dialog` (對話) 動作。
- 包含兩個自定義對話分支，皆由 `King` 別名 (#1) 說出：
  - `B01` (`13712E`)，開端受限於 `GetStage == 10`。
  - `B02` (`1376DC`)，在 `infodiag` 中其開端不受階段限制。
- 大多數玩家選擇上都存在 VMAD 片段 (`137134`, `1376E0`, `1376E2`, `1376E4`, `1376E6`)，表明它們會推進狀態或觸發結果。確切的 Papyrus 行為此處未解碼。

待驗證事項 (Open verification)：
- **30 vs 130 的極性** (哪個是好/壞結局) 是從 `BadScene` 的 EditorID + 演員標誌推論而來的；請透過反編譯階段/TIF 片段 (`CHMeq2_TIF__02137134`, `CHMeq2_TIF__021376E0`, `...E2`, `...E4`, `...E6`) 以及任務階段片段來確認，查看每個選擇會引向哪個 `SetStage`/`CompleteQuest`；
- 直接轉儲 QUST 別名，以解析 CLI 未列印的 `Molag` (#4) 和 `DrozelMemory` (#5) 填寫內容；
- 如果空間/行為編排重要，檢查四個場景程序包 (`zzzCHMeq2KingSitonBed`, `...MoveToDoor`, `...FroceGreet`, `zzzCHMeq2MolagbalRising`)；
- 對照任何遊戲內書籍或 NPC 記錄驗證專有名詞 `Hasama`/`Hasaama`, `Eroisa`, `Polydor`, `Gilverdale`；
- 在將 [`5714DF zzzCHMemoryAyledKing_BadEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:1217) 用於敘事之前，確認其是否屬於此任務的壞結局。
