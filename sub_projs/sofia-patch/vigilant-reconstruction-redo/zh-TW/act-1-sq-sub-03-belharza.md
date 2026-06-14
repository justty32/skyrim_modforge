# 第 1 幕 支線 03 - 貝爾哈扎的遺產

狀態：第一個重做切片。基於源代碼、連結優先，並非劇情摘要。

來源策略：
- 原始台詞連結回提取的源文件，而非完整複製。
- 任務 FormID、EditorID、名稱、優先級已透過 ESM + `questdiag` 驗證。
- 對話主題、條件、說話者來自 `infodiag` 診斷輸出。
- 場景記錄：不存在（根據 CLI 檢查，該任務沒有 SCEN 記錄）。

## 任務記錄

[`51EAC1 zzzAoMSubQ03 "貝爾哈扎的遺產"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:284)

CLI：
- `questdiag Vigilant.esm 0x51EAC1`
- `infodiag Vigilant.esm 0x51EAC1`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x51EAC1`
- EditorID: `zzzAoMSubQ03`
- 名稱: `貝爾哈扎的遺產`
- 標記: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest` (支線任務)
- 過濾器: `AoM\`

來自 `questdiag` 的階段：

| 階段 | 標記 | 日誌 |
|---:|---|---|
| 0 | StartUpStage | 空 |
| 1 | 無 | 空 |
| 5 | 無 | 空 |
| 10 | 無 | 空 |
| 20 | 無 | 空 |
| 30 | CompleteQuest | 空 |
| 40 | 無 | 空 |
| 50 | 無 | 空 |
| 51 | 無 | 空 |
| 60 | 無 | 空 |
| 255 | ShutDownStage | 空 |

目標：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 1 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:285) | 與 `<Alias=Mntr>` 對話 |
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:286) | 會見米諾陶洛斯首領 |

目標目標：
- 目標 1 指向 1 個引用；目標 10 指向 2 個引用。
- CLI 未打印目標詳細信息；這些可能是 ESM 中的別名填充條件（Mntr = Mordog 別名，Chief = Horbahha 別名）。

## 別名 / 階段骨幹

主機任務：
- [`51EAC1 zzzAoMSubQ03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:284)

根據 `infodiag` 輸出中的對話條件和說話者映射推論出的別名：

| 別名 | 說話者 | 名稱 | 填充（推論） |
|---:|---|---|---|
| 0 | `51EAA8` | `半牛人莫多格` (`zzzCHMntrFollower`) | 開場與早期對話中的說話者 |
| 1 | `51D895` | `首領霍巴哈` (`zzzCHMntrLeader`) | 首領對話分支中的說話者 |

推論：
- 別名 0 (莫多格) 在早期問候玩家（階段 < 5），邀請玩家前往村莊（階段 5），等待（階段 40），並提議加入。
- 別名 1 (霍巴哈) 在階段 20 出現在首領對話分支中，談論貝爾哈扎皇帝的遺願。
- 兩個別名都透過對話 INFO 中的 `GetIsAliasRef` 條件與任務掛鉤。

## 對話分支

### Hello 問候語主題

主題：[`51EAC4 zzzAoMSubQ03Hello`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3448) (雜項/問候類別，優先級 50)

| INFO FormID | 條件 | 翻譯 | 標記 |
|---|---|---|---|
| [`51EAC5`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3449) | `GetStage < 5`; `GetIsAliasRef 別名 #0` | 「老朋友，我們一直在等你。我很榮幸能在我的世代見到你。」 | 無 |
| [`51EAC6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3450) | `GetStage == 5`; `GetIsAliasRef 別名 #0` | 「老朋友，你願意來我們的村莊嗎？」 | 無 |
| [`51EAC7`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3451) | `GetStage == 10`; `GetIsAliasRef 別名 #0` | 「這是我們的米諾陶洛斯村莊。首領正在後方等著我們。」 | Goodbye |
| [`51EAF6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3452) | `GetStage == 40`; `GetIsAliasRef 別名 #0` | 「老朋友，等等我。」 | 無 |
| [`51EADD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3453) | `GetStage == 20`; `GetIsAliasRef 別名 #1` | 「我們的老朋友，你來得正好。」 | 無 |

### 莫多格分支 01 - 認出

主題：[`51EACA zzzAoMSubQ03MntrB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3455) (提示：「你怎麼知道是我？」)

| INFO FormID | 條件 | 翻譯 |
|---|---|---|
| [`51EACB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3456) | `GetStage <= 5`; `GetIsAliasRef 別名 #0` | 「我們的家族繼承了貝爾哈扎皇帝遺留下來的記憶。絕對不會認錯人。」 |

### 莫多格分支 01 - 血脈

主題：[`51EACC zzzAoMSubQ03MntrB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3458) (提示：「你的意思是你是貝爾哈扎的後裔？」)

| INFO FormID | 條件 | 翻譯 |
|---|---|---|
| [`51EACD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3459) | `GetStage <= 5`; `GetIsAliasRef 別名 #0` | 「不，我的家族只是侍奉貝爾哈扎皇帝。半神的血脈仍然下落不明。」 |

### 莫多格分支 02 - 初次會面

主題：[`51EACF zzzAoMSubQ03MntrB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3461) (提示：「你想要什麼？」)

| INFO FormID | 條件 | 翻譯 | VMAD |
|---|---|---|---|
| [`51EAD0`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3462) | `GetStage < 5`; `GetIsAliasRef 別名 #0` | 「根據貝爾哈扎皇帝的遺願，我有件東西要給你看。你願意來我們的村莊嗎？」 | OnBegin=`AomSq03_TIF__0251EAD0.Fragment_1`; OnEnd=`AomSq03_TIF__0251EAD0.Fragment_0` |

推論：VMAD 片段可能在回應時管理階段進展或場景觸發。

### 莫多格分支 03 - 旅程準備

主題：[`51EAD2 zzzAoMSubQ03MntrB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3464) (提示：「走吧。」)

| INFO FormID | 條件 | 翻譯 | VMAD |
|---|---|---|---|
| [`51EAD3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3465) | `GetStage == 5`; `GetIsAliasRef 別名 #0` | 「米諾陶洛斯村莊是一個隱蔽的村落。我們想請你閉上一會兒眼睛。」 | OnEnd=`AomSq03_TIF__0251EAD3.Fragment_0` |

標記：Goodbye (結束問候/對話階段)。

### 莫多格分支 04 - 延遲

主題：[`51EAD5 zzzAoMSubQ03MntrB04T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3467) (提示：「我現在辦不到。」)

| INFO FormID | 條件 | 翻譯 |
|---|---|---|
| [`51EAD6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3468) | `GetStage == 5`; `GetIsAliasRef 別名 #0` | 「那麼我會在這裡等。當你準備好時，請告訴我們。」 |

標記：Goodbye (允許任務停滯)。

### 霍巴哈 (首領) 分支 01 - 詢問

主題：[`51EAE0 zzzAoMSubQ03ChiefB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3470) (提示：「你想給我看什麼？」)

| INFO FormID | 條件 | 翻譯 | 回應 |
|---|---|---|---|
| [`51EAE1`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3471) | `GetStage == 20`; `GetIsAliasRef 別名 #1` | 回應 1：「貝爾哈扎皇帝曾叮囑我們要帶你看這個村莊。遺憾的是，我們仍然不知道他的意圖是什麼。」 回應 2：「我們的老朋友知道貝爾哈扎皇帝的意圖嗎？儘管我是一族之長，但我仍感到焦慮。」 | 無 |

### 霍巴哈分支 02 - 膽怯評價

主題：[`51EAE3 zzzAoMSubQ03ChiefB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3474) (提示：「你的膽怯讓我想起了貝爾哈扎皇帝。」)

| INFO FormID | 條件 | 翻譯 | 情感 |
|---|---|---|---|
| [`51EAE4`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3475) | `GetStage == 20`; `GetIsAliasRef 別名 #1` | 「能得到貝爾哈扎皇帝般的……這真是榮幸。我深受感動。」 | Happy |

標記：SayOnce (防止重複)。

筆記：原始文本包含省略號佔位符；可能是原始 ESM 中的文本切斷或編碼偽影。

### 霍巴哈分支 03 - 滿意

主題：[`51EAE5 zzzAoMSubQ03ChiefB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3477) (提示：「我很滿意。無話可說。」)

| INFO FormID | 條件 | 翻譯 | VMAD |
|---|---|---|---|
| [`51EAE6`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3478) | `GetStage == 20`; `GetIsAliasRef 別名 #1` | 回應 1：「我很高興你感到滿意。我們很高興終於傳達了貝爾哈扎皇帝最後的遺願。」 回應 2：「我們族人非常感謝你。如果沒有你，我們仍然會被世人當作野獸一樣獵殺。」 | OnEnd=`AomSq03_TIF__0251EAE6.Fragment_0` |

### 莫多格分支 05 - 夥伴提議（階段 40）

主題：[`51EAF8 zzzAoMSubQ03MntrB05T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3497) (提示：「你還想見我嗎？」)

| INFO FormID | 條件 | 翻譯 |
|---|---|---|
| [`51EAF9`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3498) | `GetStage == 40`; `GetIsAliasRef 別名 #0` | 回應 1：「我想知道我是否可以陪伴你一同旅行。我已經得到了首領的許可，可以離開村莊。」 回應 2：「我不會拖累你的。我會用莫之角擊敗你的敵人。」 |

### 莫多格分支 05 - 接受夥伴

主題：[`51EAFA zzzAoMSubQ03MntrB05T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3501) (提示：「好的，跟我來吧。」)

| INFO FormID | 條件 | 翻譯 | VMAD |
|---|---|---|---|
| [`51EAFB`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3502) | `GetStage == 40`; `GetIsAliasRef 別名 #0` | 「我很感激。我一定會為你效勞的。」 | OnEnd=`AoMSq03_TIF__0251EAFB.Fragment_0` |

標記：Goodbye (結束問候/結束分支)。

### 莫多格分支 05 - 推遲夥伴

主題：[`51EAFC zzzAoMSubQ03MntrB05T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3504) (提示：「現在還不是時候。」)

| INFO FormID | 條件 | 翻譯 | VMAD |
|---|---|---|---|
| [`51EAFD`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:3505) | `GetStage == 40`; `GetIsAliasRef 別名 #0` | 「我明白了。我會磨練我的戰士技能，直到時機成熟。希望能有一天與你並肩作戰。」 | OnEnd=`AoMSq03_TIF__0251EAFD.Fragment_0` |

標記：Goodbye (允許推遲而不接受)。

## 相關 NPCs

此任務中透過對話別名和所說台詞引用的 NPC：

- [`51EAA8 zzzCHMntrFollower "半牛人莫多格"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:487) — 任務中的別名 0；初次見面、村莊嚮導、夥伴提議。
- [`51D895 zzzCHMntrLeader "首領霍巴哈"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:482) — 任務中的別名 1；米諾陶洛斯村莊的首領，貝爾哈扎遺願的執行者。

其他背景 NPC（在此任務中沒有直接對話）：
- [`510B22 zzzCHMntrBelharza "半牛人貝爾哈扎"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:344) — 貝爾哈扎皇帝，貫穿始終（血脈遺產、半神身份）。

## 重建筆記

任務結構：
- **階段 0–5**：初次見到莫多格；玩家了解繼承的記憶以及與貝爾哈扎的聯繫。
- **階段 5–10**：前往隱蔽村莊（可能是地圖任務標記或場景觸發）。
- **階段 10–20**：到達村莊；會見首領 (霍巴哈)。
- **階段 20–30**：首領對話與說明；根據玩家回應可能產生分歧結果。
- **階段 30**：`CompleteQuest` 標記 — 主線任務結束。
- **階段 40–60**：完成後的狀態；莫多格提議加入玩家成為夥伴，並提供推遲選項。

對話極性：
- **沒有明顯的好/壞分支** — 所有對話都是說明性的；玩家的回應會影響夥伴提議（接受與推遲），但不會改變任務完成情況。
- 主要結果：在首領會面後任務於階段 30 完成；夥伴標記於階段 40 開啟。

存在的 VMAD 片段：
- `AomSq03_TIF__0251EAD0` (INFO `51EAD0` 上的 OnBegin/OnEnd) — 可能觸發進入村莊的階段進展。
- `AomSq03_TIF__0251EAD3` (INFO `51EAD3` 上的 OnEnd) — 可能觸發跳轉至到達村莊的階段。
- `AomSq03_TIF__0251EAE6` (INFO `51EAE6` 上的 OnEnd) — 可能觸發任務完成或夥伴設置。
- `AoMSq03_TIF__0251EAFB` (INFO `51EAFB` 上的 OnEnd) — 可能管理隨從招募腳本。
- `AoMSq03_TIF__0251EAFD` (INFO `51EAFD` 上的 OnEnd) — 可能維持推遲狀態。

翻譯問題：
- INFO `51EAE4` 在源代碼中包含省略號；如果確切意圖很重要，請檢查 ESM 字節轉儲。
- `51EAF9` 中的 「Mor's horns」(莫之角) — 可能是異教/魔族的詛咒引用；按原樣保留。

公開驗證：
- 如果反編譯源碼或 CK 導出存在，請檢查 `AomSq03_TIF__0251EAD0`、`AomSq03_TIF__0251EAD3`、`AomSq03_TIF__0251EAE6`、`AoMSq03_TIF__0251EAFB`、`AoMSq03_TIF__0251EAFD` 中的腳本；
- 直接在 ESM 中檢查別名填充條件（CLI 不打印目標引用）；
- 驗證階段 30 的 `CompleteQuest` 標記沒有明確的腳本觸發器（推論：任務在 VMAD 片段結束時自動關閉）；
- 如果空間分期相關，請驗證階段跳轉時（階段 5→10, 階段 10→20）的地圖標記或單元轉換；
- 打包後確認莫多格的遊戲內外觀和對話交付（語音、口型同步）。
