# 第 2 幕 支線 03 - 鮮血主母

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋翻譯問題或特定條件時出現簡短的原始片段。
- CLI 診斷提供確定的階段/目標/條件數據。

## 任務記錄

[`038526 zzzBMMq03 "鮮血主母"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260)

CLI：
- `questdiag Vigilant.esm 0x038526`
- `infodiag Vigilant.esm 0x038526`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x038526`
- EditorID: `zzzBMMq03`
- 名稱: `鮮血主母`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest` (支線任務)
- 過濾器: `BM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 60 | 無 | 空 |
| 70 | 無 | 空 |
| 80 | 無 | 空 |
| 90 | 無 | 空 |
| 100 | CompleteQuest | 空 |
| 200 | 無 | 空 |
| 210 | 無 | 空 |
| 220 | CompleteQuest | 空 |
| 9999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 任務文本 |
|---:|---|---|
| 60 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) | 擊敗 `<Alias=LamaeBal>` |
| 90 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) | 打破 `<Alias=MolagBal>` 的詛咒 |

目標目標：
- 目標 60 有 1 個目標。
- 目標 90 有 2 個目標。
- （推論：目標引用可能是兩位主要反派，拉瑪·巴爾和莫拉格·巴爾，或是他們關聯的地點/物品；如果空間分期重要，則需要更深入的轉儲。）

## 別名 / 分期骨幹

主線任務有多個對話分支，針對三個主要別名：`LamaeBal`（別名 #1）、`MolagBal`（別名 #0）和 `LoveBound`（別名 #2）。`MolagBal` 別名與一個可以腐化為魔龍的吸血鬼後裔相關聯，而 `LamaeBal` 似乎是處於鮮血詛咒形態下的拉瑪·巴爾。

主機任務：
- [`038526 zzzBMMq03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260)

來自 `infodiag` 的別名摘要：
- 別名 #0：`MolagBal`（在分支 `zzzBMMq03B01mbGreet` 中找到引用）
- 別名 #1：`LamaeBal`（透過 `GetIsID` 檢查在問候主題 `zzzBMMq03HelloVamp` 中引用）
- 別名 #2：`LoveBound`（在分支 `zzzBMMq03B01LBGreet` 中引用，階段門限於 50 和 >=200）

推論：
- 任務透過階段 30、50、200 的對話和選擇點推進。
- 階段 100 和 220 標誌著完成分支（可能存在兩條路徑）。
- 階段 9999 可能是清理/關閉階段。

## 場景記錄

根據 `infodiag`，該任務沒有直接附加場景 (SCEN) 記錄。所有分期均由對話驅動。

找到的場景引用：
- `TOPIC 0x03D77F`（場景類型，無 EditorID，由任務 `038526` 擁有）
  - `INFO[0] 0x03D780`：[拉瑪，醒醒。發生了……如果你咬碎那傢伙的喉嚨，我會讓你再次做夢](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:635)
  - 這似乎是敘述或激活提示，而非傳統場景；對該主題 FormID 執行 `scenediag` 返回「不是 Vigilant.esm 中的場景」，暗示它可能只是對話觸發器。

## 問候主題：吸血鬼後裔回應

[`03AE0F zzzBMMq03HelloVamp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573)

說話者條件模式：
- 每個 INFO 均以截然不同的 NPC FormID 進行 `GetIsID` 條件限制（非基於別名）。
- 說話者變體（共 5 個 INFO）：每個都針對不同的吸血鬼後裔身份。

| INFO | NPC FormID (GetIsID) | 翻譯 |
|---|---|---|
| `03AE10` | `0392A5:Vigilant.esm` | [沒有猶豫。你……樞機，因為今天被選中交給莫拉格·巴爾。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE11` | `0392A6:Vigilant.esm` | [自由！在這個目的地擁有自由！得到了夢寐以求的發洩口！](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE12` | `039824:Vigilant.esm` | [我會歡迎你。好吧，我受洗了。拉瑪血脈，他以他的名字集結在一起](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE13` | `039825:Vigilant.esm` | [接受 Ukero 洗禮。那樣的話，夜晚就成了你的東西](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |
| `03AE14` | `039828:Vigilant.esm` | [喝下拉瑪的血。她的血將承諾你的永恆。](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:573) |

翻譯筆記：
- 提取質量較差；"Moragu Baru" 可能從 "Molag Bal" (莫拉格·巴爾) 損壞而來。
- "Ramae" 可能是 "Lamae"（鮮血主母拉瑪）。
- "Ukero" 及其相關術語不明確，可能需要 ESM 解碼驗證。

## 自定義對話分支：拉瑪·巴爾 (別名 #1)

分支：
- 根 EditorID：`zzzBMMq03B01lhGreet` 分支（CLI 中未指定 FormID，從主題結構推論）

說話者條件模式：
- INFOs 要求別名 #1 (`LamaeBal`) 上的 `GetIsAliasRef == 1`。
- 部分 INFO 受限於階段 30。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`03BC14 zzzBMMq03B01lhGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:602) | `03BC15` | 無 | `GetStage EqualTo 30`; `GetIsAliasRef 別名 #1` | [進行得不順利嗎？快點，那一天我會迎來終結](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:602) |
| [`03BC16 zzzBMMq03B01WhoRU`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:605) | `03BC17` | Goodbye | `GetIsAliasRef 別名 #1` | 提示：「你是誰？」回應：[我是拉瑪，你的拉瑪。你忘了嗎？](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:605) |
| [`03BC18 zzzBMMq03B01lhDestination`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:608) | `03BC19` | 無 | `GetIsAliasRef 別名 #1` | 提示：「你要去哪裡？」回應：[忘記了嗎僧侶？可能是去你父親城堡的地方](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:608) |
| [`03BC1A zzzBMMq03B01lfFather`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:611) | `03BC1B` | Goodbye | `GetIsAliasRef 別名 #1` | 提示：「你的父親是誰？」回應：[曾經沒有過嗎？忘了嗎，我見過另一個](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:611) |

翻譯筆記：
- "Wasuren monk"（忘記了嗎僧侶）不明確；可能是專有名詞（地點或派系）或損壞文本。
- 「你父親的城堡」可能指的是魔族堡壘，或許與莫拉格·巴爾的領域有關。

## 自定義對話分支：莫拉格·巴爾 (別名 #0)

分支：
- 根 EditorID：`zzzBMMq03B01mbGreet` 分支（未指定 FormID）

說話者條件模式：
- 大多數 INFOs 要求別名 #0 (`MolagBal`) 上的 `GetIsAliasRef == 1`。
- 開場白要求 `GetStage EqualTo 50`。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`03BC21 zzzBMMq03B01mbGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) | `03BC22` | Goodbye, SayOnce | `GetStage EqualTo 50`; `GetIsAliasRef 別名 #0` | 提示：(隱含) 回應 (1)：[來得好，斯坦達爾之子。莫拉格·巴爾會歡迎這不同的人](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) 回應 (2)：[是什麼，Minuka 要和這個女兒共度永恆嗎？這個女兒的幸福也是斯坦達爾的希望](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) 回應 (3)：[沒有必要放棄他們的信仰。只要選擇，劈開它，就這麼做吧](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |
| [`03BC23 zzzBMMq03B01mbGreet (續)`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) | `03BC23` | Goodbye | `GetStage EqualTo 50`; `GetIsAliasRef 別名 #0` | 提示：(隱含) 回應：[這不該猶豫，你應該知道嗎？道路，我確定只有一條](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |
| 分支在階段 200 繼續 | `03BC24` | Goodbye, SayOnce | `GetStage EqualTo 200`; `GetIsAliasRef 別名 #0` | 提示：(隱含) 回應 (1)：[兩人的出發，莫拉格·巴爾會為此祝福。你們超越了 Akei 的管理，但會永遠活著](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) 回應 (2)：[現在，進城堡。我會為你們洗禮](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) VMAD：結束時執行 `BM03_TIF__0103BC24.Fragment_0` |
| | `03BC25` | Goodbye | `GetStage EqualTo 210`; `GetIsAliasRef 別名 #0` | 提示：(隱含) 回應：[我被安置在城堡裡是為了什麼？洗禮準備工作已經做好了](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:614) |

翻譯筆記：
- "Minuka" 不明確；可能是角色名稱或概念。
- "Akei" 可能指的是 "Arkay" (阿凱)，死亡與凡人性的神靈。
- 「cleave, do it just」(劈開它，就這麼做吧) 很彆扭；可能意味著「選擇吧，現在就做」或類似的意思。
- 多個回應暗示玩家在階段 50 處有選擇分支。

## 自定義對話分支：Love Bound / 拉瑪吸血鬼形態 (別名 #2)

分支：
- 根 EditorID：`zzzBMMq03B01LBGreet` 分支（未指定 FormID）

說話者條件模式：
- INFOs 要求別名 #2 (`LoveBound`) 上的 `GetIsAliasRef == 1`。
- 開場白受限於階段 50 和 >=200。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`03C190 zzzBMMq03B01LBGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) | `03C191` | 無 | `GetStage EqualTo 50`; `GetIsAliasRef 別名 #2` | [現在，讓我們一起走吧。每個人，都在祝福我們的結合](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| | `03C196` | Goodbye | `GetStage EqualTo 60`; `GetIsAliasRef 別名 #2` | [我會撕裂。Nasai 準備好了](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| | `03C197` | Goodbye | `GetStage GreaterThanOrEqualTo 200`; `GetIsAliasRef 別名 #2` | [永遠，到處，我們都會在一起](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:623) |
| [`03C192 zzzBMMq03lbNoMonster`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) | `03C193` | Goodbye, SayOnce | `GetIsAliasRef 別名 #2` | 提示：「走開，怪物」回應 (1)：[我就喜歡他……真的很冰冷的眼神。我想知道，也會用匕首回應我的愛嗎？](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) 回應 (2)：[我跑不掉，那是絕對不會放手的。我甚至會取走一肢](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:628) VMAD：結束時執行 `BM03_TIF__0103C193.Fragment_0` |
| [`03C194 zzzBMMq03B01LBletGo`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:632) | `03C195` | Goodbye | `GetIsAliasRef 別名 #2` | 提示：「好吧，我們走吧」回應：[幸福。我永遠會和你在一起。永遠永遠，永遠……](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:632) VMAD：結束時執行 `BM03_TIF__0103C195.Fragment_0` |

翻譯筆記：
- "Nasai" 不明確；可能是名稱或腐化。
- 對話強調情感紐帶（「愛」、「在一起」、「永遠」），暗示了浪漫或強迫的主題。
- 在「走開，怪物」與「好吧，我們走吧」之間的玩家選擇似乎會決定分支結果。

## 相關記錄

這些不完全是任務 `038526` 的一部分，但它們是鮮血主母劇情的背景 NPC 和物品。

NPCs (來自 game-data/npcs.tsv)：
- [`037468 zzzBMLamaeBal`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:891) - 拉瑪·巴爾（鮮血主母本人；別名 #1 可能指向這裡）
- [`0368E0 zzzBMMolagBalHuman`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:897) - 莫拉格·巴爾（人類形態；別名 #0 可能指向這裡）
- [`036ECD zzzBMMolagBalSonBadEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:895) - 魔龍領主（莫拉格·巴爾後裔可能的轉化形態；FormID `0x036ECD`）
- 吸血鬼後裔（各種形態）：`zzzBMLamaeVampFeral`, `zzzBMLamaeBeolfag`, `zzzBMLamaeVampLich`, `zzzBMLamaeVampTroll`
- [`03748D zzzBMLamaeZombie`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:890) - 拉瑪殭屍形態

物品：
- [`03B675 zzzBMMolagBalCurseofLamae`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:967) - 莫拉格·巴爾的詛咒（可能是目標 90 所指向的物品）

## 重建筆記

基於源代碼：
- 該支線任務由 [`038526 zzzBMMq03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:260) 代表，名稱為 `"鮮血主母"`。
- 它有兩個主要目標：擊敗拉瑪·巴爾（目標 60）和打破她的詛咒（目標 90）。
- 它沒有 SCEN 記錄；所有分期均透過問候主題和四個自定義分支由對話驅動。
- 任務具有三個主要的對話分支：
  - 拉瑪·巴爾分支（別名 #1），在階段 30 開啟，包含身份和地點問題。
  - 莫拉格·巴爾分支（別名 #0），在階段 50 開啟，帶有三個選項的問候，在階段 200 恢復並包含完成對話。
  - Love Bound / 吸血鬼拉瑪分支（別名 #2），受限於階段 50、60 和 >=200，帶有拒絕（「走開，怪物」）或接受（「好吧，我們走吧」）的玩家選擇。
- 在完成階段的 INFO 上存在多個 VMAD 片段，指示 Papyrus 腳本驅動進度或結果分支。

分支極性：
- 莫拉格·巴爾階段 50 的問候提供了三個回應，暗示了玩家在接受/拒絕莫拉格提議時的主動性。
- Love Bound 分支提供了明確的「拒絕怪物」與「接受」的選擇路徑。
- 完成階段 100、220 和 9999 暗示可能存在多個結局（擊敗拉瑪，或是被腐化/束縛）。

公開驗證：
- 如果 Papyrus 源代碼可用，反編譯 INFO `03BC24`, `03C193`, `03C195` 上的 VMAD 片段。
- 驗證任務記錄本身中別名 #0, #1, #2 的 NPC FormID 連結（CLI `questdiag` 不打印完整的別名列表）。
- 檢查目標標靶（任務階段 60 / 90 目標），確認它們分別指向拉瑪·巴爾和詛咒物品。
- 確認類場景主題 `0x03D77F` 的性質；它可能是後續對話或電影式提示的觸發器。
- 如果可用，對照日語源文本驗證 "Wasuren monk", "Minuka", "Akei", "Nasai", "Ukero" 等術語，或將其標記為未解決的提取偽影。
