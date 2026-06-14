# 第 2 幕 支線 02 - 殘骸

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋翻譯問題或特定條件時出現簡短的原始片段。
- CLI 診斷提供確定的階段/目標/條件數據。
- 場景暫存來自 `scenediag` CLI 輸出（相位、動作、演員別名）。

## 任務記錄

[`038525 zzzBMMq02 "殘骸"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:108)

CLI：
- `questdiag Vigilant.esm 0x038525`
- `infodiag Vigilant.esm 0x038525`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x038525`
- EditorID: `zzzBMMq02`
- 名稱: `殘骸`
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
| 65 | 無 | 空 |
| 70 | 無 | 空 |
| 80 | 無 | 空 |
| 90 | 無 | 空 |
| 100 | CompleteQuest | 空 |
| 9999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 任務文本 |
|---:|---|---|
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:109) | 擊敗 `<Alias=Vamp01>` |
| 30 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:110) | 擊敗 `<Alias=Vamp02>` |
| 50 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:111) | 擊敗 `<Alias=Vamp03>` |
| 60 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:112) | 與 `<Alias=Vamp04Ess>` 對話 |
| 70 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:113) | 給予 `<Alias=Vamp04>` 斯坦達爾的仁慈 |
| 90 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:114) | 擊敗 `<Alias=Vamp05>` |

目標目標：
- 目標 10, 30, 50, 70 在 ESM 中各有 1 個目標。
- 目標 60, 90 有 0 個目標。
- （推論：目標可能是吸血鬼戰鬥引用；如果空間分期重要，則需要更深入的轉儲。）

## 別名 / 分期骨幹

根據 `infodiag` 輸出，該任務擁有 14 個對話主題。其中兩個是場景主題（無 EditorID）：
- 主題 `0x03A0C4`（場景動作）
- 主題 `0x03A0C6`（場景動作）
- 主題 `0x03A0C8`（場景動作）

剩餘的 11 個主題是自定義對話分支，由 INFO 條件中引用的別名索引組織：
- 別名 `#1`（推論：`Vamp04Ess` — 必要的倖存者）
- 別名 `#5`（推論：`Vamp04` — 非必要的吸血鬼）

（推論：其他別名索引 #2, #3, #4 可能代表目標 10, 30, 50 中引用的三隻吸血鬼 `Vamp01`, `Vamp02`, `Vamp03`；別名索引可能隱含自 FormID 順序或任務創建順序。）

## 自定義對話分支

該任務有兩個主要對話分支，均受階段限制並與特定的吸血鬼別名相關聯。

### 分支 1：Vamp04Ess (別名 #1，可能的倖存者)

分支：
- `039B39:Vigilant.esm`（根，從主題擁有模式推論）

說話者條件模式：
- INFOs 要求別名 `#1` (`Vamp04Ess`) 上的 `GetIsAliasRef == 1`。
- 每個主題的階段門限各不相同。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`039B3A zzzBMMq02B01v2FearGreet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:527) | `039B3B` | WalkAway | `GetStage EqualTo 20`; `GetIsAliasRef 別名 #1` | [我不再生氣了嗎？雅瑞德爾，我終於恢復了理智……我們去瓜吉吧。好吧，讓我們回去……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:528) |
| [`039B3C zzzBMMq02B01v2AreUOK`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:530) | `039B3D` | Goodbye, CanMoveWhileGreeting | `GetIsAliasRef 別名 #1` | 提示：「你還好嗎？」回應：[如果你先走，怪物！別靠近！走開！！快走！快走！啊啊走開！](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:531) VMAD：結束時執行 `BM02_TIF__01039B3D.Fragment_0`。 |

翻譯筆記：
- "Aredhel" 和 "Gwaji" 可能是提取文本中未翻譯的名稱或術語；可能需要 NPC/地點驗證。
- 「恢復理智」暗示角色在遭受創傷/佔有後恢復意識或正常狀態。

### 分支 2：Vamp04 (別名 #5，非必要吸血鬼)

分支：
- `039B41:Vigilant.esm`（根，從主題擁有模式推論）

說話者條件模式：
- INFOs 要求別名 `#5` (`Vamp04`) 上的 `GetIsAliasRef == 1`。
- 密集的階段門限（20, 60, 65, 70 範圍）。

| 主題 | INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`039B42 zzzBMMq02B01v4Greet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:533) | `039B43` | Goodbye | `GetStage LessThanOrEqualTo 40`; `GetIsAliasRef 別名 #5` | [現在很糟，我不知道耶利哥什麼時候會來](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:534) VMAD：結束時執行 `BM02_TIF__01039B43.Fragment_0`。 |
| [`039B45 zzzBMMq02B02v4Happen`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:537) | `039B46` | SayOnce | `GetStage EqualTo 60`; `GetIsAliasRef 別名 #5` | 提示：「這裡發生了什麼事？」回應 (1)：[她。她很痛苦。那裡有一位鮮血夫人……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:538) 回應 (2)：[我們必須打敗她。我什麼都做不了。所有人，吸血鬼……諾瑪莎那些倖存下來的人，還有……她的血](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:539) VMAD：結束時執行 `BM02_TIF__01039B46.Fragment_0`。 |
| [`039B48 zzzBMMq02B03v4Vampirism`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:541) | `039B49` | 無 | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「難道你不 Naose 嗜血症嗎？」回應 (1)：[太遲了。對鮮血的渴求很強烈。最重要的是，她正因變得強大而喜悅……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:542) 回應 (2)：[每天晚上，我都夢見她流下血淚。自從那天她哭泣以來，她就被污染得太深了……莫拉格·巴爾](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:543) 回應 (3)：[我知道她的悲傷仍然在沉睡中。對我來說，她已經變得無可替代……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:544) |
| [`039B4B zzzBMMq02B04v4Imprison`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:546) | `039B4C` | 無 | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「你為什麼被囚禁？」回應：[拒絕她的血的人都被囚禁在這裡。一直到接受鮮血為止……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:547) |
| [`039B4E zzzBMMq02B05v4OtherVigilant`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:549) | `039B4F` | 無 | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「其他守護者怎麼了？」回應：[每個活下來的人都變成了吸血鬼。每個人都在她的血中 Kuruwasu](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:550) |
| [`039B51 zzzBMMq02B06v4AboutMatron`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:552) | `039B52` | 無 | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「鮮血主母？」回應：[被莫拉格·巴爾玷污的內德少女。她是吸血鬼的始祖。](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:553) |
| [`039B54 zzzBMMq02B07v4Help`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:555) | `039B55` | WalkAway | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「有什麼我可以做的嗎？」回應：[我希望你殺了我。我不想在變成精疲力竭的嗜血野獸之前死掉……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:556) |
| [`039B56 zzzBMMq02B07v4Kill`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:558) | `039B57` | Goodbye | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「好的。我會殺了你」回應：[好……謝謝，快殺了我。我可能無法再忍受對鮮血的渴求了……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:559) VMAD：開始時執行 `BM02_TIF__01039B57.Fragment_1`，結束時執行 `BM02_TIF__01039B57.Fragment_0`。 |
| [`039B58 zzzBMMq02B07v4NotKill`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:561) | `039B59` | 無 | `GetStage EqualTo 65`; `GetIsAliasRef 別名 #5` | 提示：「我辦不到」回應：[我求你……我求你。我想從這種痛苦中解脫出來](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:562) |
| [`03A0C2 zzzBMMq02B01v4GreetEnd`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) | `03A0C2` | Goodbye | `GetStage EqualTo 70`; `GetIsAliasRef 別名 #5` | 回應：[殺了我……拜託，殺了我……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) VMAD：結束時執行 `BM02_TIF__0203A0C2.Fragment_0`。 |

翻譯筆記：
- "Naose" 在「嗜血症」提示中不明確；可能是誤譯或生物名稱的音譯。
- "Nomasa" 在「這裡發生了什麼事？」回應中不明確；可能是名稱或方言術語。
- "Kuruwasu" 在「其他守護者」回應中不明確；可能是扭曲的名稱或術語。
- "Jericho" 在「問候」回應中可能指的是聖經或傳說人物（耶利哥領主，任務給予者？）；需要驗證。

## 場景記錄

三個場景動作主題是該任務的一部分。其底層 SCEN 記錄不存在於提取的文本中；僅提供對話動作和翻譯。

場景主題：
- `0x03A0C4` — 第一場景獨白 (INF0 `0x03A0C5`)
- `0x03A0C6` — 第二場景獨白 (INFO `0x03A0C7`)
- `0x03A0C8` — 第三場景獨白 (INFO `0x03A0C9`)

| 主題 | INFO | 情感 | 翻譯 |
|---|---|---|---|
| `0x03A0C4` (場景) | `0x03A0C5` | 悲傷 | [我懷念每個人……每個人……都死了，一直被吃掉……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |
| `0x03A0C6` (場景) | `0x03A0C7` | 恐懼 | [而她和我……你現在就在這裡 Ganzen 怪物……](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |
| `0x03A0C8` (場景) | `0x03A0C9` | 憤怒 | [你這怪物莫拉格·巴爾，為了靈魂的和平，你只能死，不可饒恕！晚到的朋友！](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:?) |

（推論：這些可能是場景獨白動作，每個相位或演員一個，代表倖存者在遇到吸血鬼和殘骸現場時的痛苦回憶或現狀。）

## 相關記錄

NPCs:
- `Vamp01`, `Vamp02`, `Vamp03` — 三隻吸血鬼（別名 #2, #3, #4）；FormID 未知。
- `Vamp04` (別名 #5) — 非必要吸血鬼，主要說話者；FormID 為 `03A0C2` 或附近？
- `Vamp04Ess` (別名 #1) — 必要的倖存者；FormID 未知。

物品：
- [`斯坦達爾的仁慈`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:?) — 將在目標 70 中給予的任務物品。

地點（推論）：
- 「殘骸」（未命名地點；任務名稱暗示毀壞的船隻或建築工地）。
- 風盔城地區（從 sq01 延續的第 2 幕地理位置）。

## 重建筆記

基於源代碼：
- 該支線任務由 [`038525 zzzBMMq02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:108) 代表，名稱為 `"殘骸"`。
- 它包含 6 個涵蓋階段 10–90 的目標，在階段 100 和 9999 處有兩個 CompleteQuest 門限。
- 它有三個場景動作主題 (`0x03A0C4`, `0x03A0C6`, `0x03A0C8`)；精確的 SCEN 分期尚未從提取的文本中獲得。
- 它包含兩個截然不同的對話分支：
  - Vamp04Ess 分支（別名 #1），在階段 20 以基於恐懼的問候開啟。
  - Vamp04 分支（別名 #5），涵蓋階段 0（初始問候）→ 60（說明）→ 65（吸血鬼狀況對話）→ 70+（最終狀態或死亡選擇）。
- 多個對話 INFO 帶有 VMAD 腳本（片段 `BM02_TIF__01039B3D`, `BM02_TIF__01039B43`, `BM02_TIF__01039B46`, `BM02_TIF__01039B57`, `BM02_TIF__0203A0C2`），指示階段進展、選擇路由或結果邏輯。

階段進展（推論）：
- **階段 0**：在殘骸現場初始遭遇；Vamp04 的首次問候可用。
- **階段 10–50**：戰鬥目標 (Vamp01, Vamp02, Vamp03) 依次激活並擊敗。
- **階段 60**：最終說明對話；Vamp04 揭示了「鮮血夫人」和吸血鬼的起源故事。目標 60（與 Vamp04Ess 對話）激活。
- **階段 65**：吸血鬼狀況對話變為可用；玩家可以選擇殺死 Vamp04 或表現出仁慈。目標 65–70 受限。
- **階段 70**：餘波；Vamp04 的最終狀態（死亡或轉化？）。目標 90（擊敗 Vamp05）可能激活。
- **階段 80**：不明確；此處無受限目標。
- **階段 90**：最終擊敗目標激活。
- **階段 100 / 9999**：CompleteQuest 標記；精確路由需要 VMAD 反編譯。

公開驗證：
- 反編譯 VMAD 片段 `BM02_TIF__01039B3D`, `BM02_TIF__01039B43`, `BM02_TIF__01039B46`, `BM02_TIF__01039B57`, `BM02_TIF__0203A0C2` 以了解選擇 → SetStage → 目標/CompleteQuest 路由。
- 如果可以從 ESM 中提取場景主題 `0x03A0C4`, `0x03A0C6`, `0x03A0C8` 的 FormID，則對每個主題執行 `scenediag Vigilant.esm 0x<SCEN_FormID>`；將揭示相位、動作、演員別名和時機。
- 驗證別名 FormID (#1 Vamp04Ess, #5 Vamp04, #2–#4 Vamp01–Vamp03) 及其 NPC 對話特徵。
- 驗證地點：「殘骸」是否是風盔城地區的命名單元/世界空間？檢查單元記錄。
- 驗證「斯坦達爾的仁慈」物品 FormID 及其使用案例（祝福？移除？通用任務物品？）。
- 澄清未翻譯的術語："Jericho," "Naose," "Nomasa," "Kuruwasu," "Ganzen."
