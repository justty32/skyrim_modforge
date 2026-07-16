# 第 1 幕 中心 - 斯坦達爾警戒者

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 僅在需要解釋翻譯問題時出現簡短的原始片段。
- 對話條件和分支來自 CLI 診斷，因為提取的文本僅保留主題文本，而不保留條件鏈。

## 任務記錄

[`005CE2 zzzAoMMq00 "斯坦達爾警戒者"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:L1)

CLI：
- `questdiag Vigilant.esm 0x005CE2`
- `infodiag Vigilant.esm 0x005CE2`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x005CE2`
- EditorID: `zzzAoMMq00`
- 名稱: `斯坦達爾警戒者`
- 標記: `273` (RunOnce, Repeatable 標記)
- 優先級: `90`
- 類型: `SideQuest` (支線任務)
- 過濾器: `AoM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | StartUpStage | 空 |
| 5 | 無 | 空 (存在目標日誌，第二個條目有 3 個條件) |
| 10 | 無 | 空 (第二個條目有 2 個條件) |
| 15 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | CompleteQuest | 空 |
| 999 | CompleteQuest | 空 |
| 9999 | CompleteQuest | 空 |

目標：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 5 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L2) | 加入斯坦達爾警戒者 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L3) | 跟隨阿爾塔諾或在斯坦達爾神廟與阿爾塔諾會合 |
| 15 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L4) | 與阿爾塔諾對話 |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L5) | 與索隆迪爾對話 |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md#L6) | 與阿爾塔諾對話 |

目標標靶：
- 目標 5 指向 1 個標靶 (0 條件)
- 目標 10 指向 2 個標靶 (均為 0 條件)
- 目標 15 指向 1 個標靶 (0 條件)
- 目標 20 指向 1 個標靶 (0 條件)
- 目標 30 指向 1 個標靶 (0 條件)
- 當前 CLI 輸出未打印標靶位置；如果確切位置很重要，則需要更深入的 QUST 標靶轉儲。

## 對話主題

### 初始招募 (分支 005CE5)

**階段門限**：階段 < 10 接受首次招募主題；階段 >= 10 接受神廟的後續對話。

#### 主題 1：招募宣傳 (005CE6)

[`005CE6 zzAoMMq0B1Tvigilant` 提示：「讓我加入斯坦達爾警戒者。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L1)

| INFO | 標記 | 條件 | 翻譯 |
|---|---|---|---|
| [`005CE7`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L2) | `SayOnce`, `WalkAway` | `GetStage < 10`; `GetIsAliasRef 別名 #0` | [「你有一雙好眼力。為什麼不加入斯坦達爾警戒者呢？一起讓斯坦達爾的仁慈充滿天際省？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L2) |
| [`005CEC`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L3) | `WalkAway` | `GetStage < 10`; `GetIsAliasRef 別名 #0` | [「改變主意了？我們歡迎你。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L3) |

推論：
- 別名 #0 是阿爾塔諾 (招募者)。
- 第一個回應上的 `SayOnce` 暗示這是最初的提議；隨後的訪問將重複使用第二個 INFO。

#### 主題 2：是的，接受招募 (005CE8)

[`005CE8 zzAoMMq0B1Yes` 提示：「是的，讓我加入。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L4)

| INFO | 標記 | 條件 | 回應 | VMAD |
|---|---|---|---|---|
| [`005CE9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L5) | 無 | `GetIsAliasRef 別名 #0` | (1) [「我很高興收到肯定的答覆。斯坦達爾祝福你。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L5) / (2) [「我會引導你前往斯坦達爾神廟。跟我來。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L6) | 結束時執行 `AoM00_TIF__01005CE9.Fragment_0` |

推論：
- 多行回應暗示了傳送前的對話氛圍。
- VMAD 片段可能將玩家推進至階段 5+ 並發布旅行包裹。

#### 主題 3：拒絕招募 (005CEA)

[`005CEA zzAoMMq0B1No` 提示：「不，沒興趣。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L7)

| INFO | 標記 | 條件 | 翻譯 | VMAD |
|---|---|---|---|---|
| [`005CEB`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L8) | `Goodbye` | `GetIsAliasRef 別名 #0` | [「噢……我就在這裡。如果你改變主意的話……再來這裡吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L8) | 結束時執行 `AoM00_TIF__01005CEB.Fragment_0` |

推論：
- `Goodbye` 標記表示這將結束對話分支。
- VMAD 片段可能重置對話可用性或記錄拒絕。

### 在神廟 (分支 027A3B, 027A40, 027A43)

**階段門限**：階段 >= 15 接受神廟問候；階段 = 20 接受索隆迪爾問候；階段 = 30 接受說明。

#### 主題 4：到達神廟 (027A3C)

[`027A3C zzzAoMMq00B2ArriveTemple`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L9)

| INFO | 標記 | 條件 | 回應 | VMAD |
|---|---|---|---|---|
| [`027A3F`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L10-11) | `Goodbye` | `GetInCell == 025091:Vigilant.esm`; `GetStage == 15`; `GetIsAliasRef 別名 #0` | (1) [「這是斯坦達爾神廟，警戒者的基地之一。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L10) / (2) [「你应该去和索隆迪爾打個招呼。他是斯坦達爾的守護者。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L11) | 結束時執行 `AoM00_TIF__01027A3F.Fragment_0` |

推論：
- `GetInCell` 確認位置在斯坦達爾神廟內部 (025091)。
- 階段 15 門限：玩家應在階段 5-10 旅行序列後達到此階段。
- VMAD 可能將階段推進至 20 (索隆迪爾問候準備)。

#### 主題 5：索隆迪爾問候 (027A41)

[`027A41 zzzAoMMq00B3NiceToMeet` 提示：「很高興見到你，我是 <Alias=Player>」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L12)

| INFO | 標記 | 條件 | 回應 | VMAD |
|---|---|---|---|---|
| [`027A42`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L13-15) | `Goodbye` | `GetStage == 20`; `GetIsAliasRef 別名 #4` | (1) [「你就是阿爾塔諾提到的那個新人嗎。你有一雙好眼力。我感受到了非常強大的意志。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L13) / (2) [「閒聊就到此為止吧。因為那會是無聊的老故事。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L14) / (3) [「阿爾塔諾會暫時照顧你。如果你有什麼事，儘管跟阿爾塔諾說。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L15) | 結束時執行 `AoM00_TIF__01027A42.Fragment_0` |

推論：
- 別名 #4 是索隆迪爾 (守護者)。
- 多重回應暗示了問候 + 指導流程。
- VMAD 可能將階段推進至 30 (說明)。

#### 主題 6：神廟說明 (027A44)

[`027A44 zzzAoMMq00B04Explanation` 提示：「告訴我關於這座神廟的事」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L16)

| INFO | 標記 | 條件 | 回應 | VMAD |
|---|---|---|---|---|
| [`027A45`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L17-22) | `Goodbye` | `GetStage == 30`; `GetIsAliasRef 別名 #0` | (1) [「我會簡短介紹一下可用的設施。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L17) / (2) [「你現在站的地方是斯坦達爾之間。這是向斯坦達爾祈禱的地方。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L18) / (3) [「地下室有一個圖書館，存放著我們先輩收集的書籍。希望你有空去看看。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L19) / (4) [「一樓右側靠近熔煉裝置，二樓是從入口處看到的休息室。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L20) / (5) [「二樓左側是食堂。如果你感到餓了，可以在那裡吃點。雖然不是什麼豐盛的大餐。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L21) / (6) [「說明就到此為止。你可能累了。希望你能休息一下。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md#L22) | 結束時執行 `AoM00_TIF__01027A45.Fragment_0` |

推論：
- 別名 #0 在這裡切換了背景 (可能切換回阿爾塔諾進行環境描述)。
- 六部分回應是一段獨白，涵蓋了：介紹 → 祈禱大廳 → 圖書館 → 一樓設施 → 食堂 → 休息建議。
- VMAD 可能推進至階段 40 (任務完成)。

## 相關記錄

NPCs (任務附屬)：
- [`0274A6 zzzAoMVigilantKeeper` - 索隆迪爾 (斯坦達爾的守護者)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)
- [`000D62 zzzAoMVigilantTraitor` - 阿爾塔諾 (招募者)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)
- [`02748B zzzAoMVigilantKeeper` - 索隆迪爾 (備用記錄)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv#L1)

地點：
- [`025091 zzzAoMTempleInteriorStendarr` - 斯坦達爾神廟 (內部單元)](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant)

## 重建筆記

基於源代碼：
- 這個中心任務 (`005CE2 zzzAoMMq00`) 是第 1 幕的入口點：玩家被阿爾塔諾招募，前往斯坦達爾神廟，並與索隆迪爾見面。
- 它包含 6 個對話主題 (`005CE6`, `005CE8`, `005CEA`, `027A3C`, `027A41`, `027A44`)，共有 7 個 INFO 記錄。
- 階段進展：階段 0 → 階段 5 (加入提議) → 階段 10 (旅行) → 階段 15 (到達) → 階段 20 (會見守護者) → 階段 30 (說明) → 階段 40 (完成)。
- 所有 INFO 都透過 `GetIsAliasRef` 與別名 #0 (阿爾塔諾) 或別名 #4 (索隆迪爾) 綁定。
- Goodbye/SayOnce 回應上的 VMAD 片段暗示了對話腳本的推進；這裡未解碼確切的 Papyrus 行為。

翻譯筆記：
- [005CE7]：「Why do't you」 = 「Why don't you」(源代碼中的拼寫錯誤)。
- [027A41]：「<Alias=Player>」是個對話佔位符；實際遊戲中會替換為玩家名稱。
- [027A44]：多重回應行來自單個 INFO 記錄，可能作為一段獨白序列播放。

公開驗證：
- 如果源碼或反編譯路徑存在，請檢查腳本 `AoM00_TIF__01005CE9`、`AoM00_TIF__01005CEB`、`AoM00_TIF__01027A3F`、`AoM00_TIF__01027A42`、`AoM00_TIF__01027A45`；
- 如果有更豐富的別名轉儲可用，請直接檢查 QUST 別名 (別名 #0 = 阿爾塔諾，別名 #4 = 索隆迪爾)；
- 檢查階段 5 的條件鏈 (questdiag 顯示第二個條目有 3 個條件) 以驗證加入的確切要求；
- 如果空間分期重要，請驗證單元位置 025091；
- 如果招募 → 章節序列是視情況而定的，請與第 1 幕章節任務 (`act-1-sq-01-squeezer.md` 以後) 進行交叉連結。
