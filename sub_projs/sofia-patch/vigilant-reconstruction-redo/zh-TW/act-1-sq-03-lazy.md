# 第一章任務 03 — 悠閒的下午 (Act 1 Quest 03 - Lazy Afternoon)

狀態：基於來源 (Source-grounded) 的切片。連結優先，以對話為中心。

來源方針：
- 原始對話行連結回提取的來源文件，而非完整複製。
- 僅在需要解釋歧義、誤寫或編碼問題時才出現短小的片段。
- 場景編排來自 CLI 診斷和對話話題結構，而非劇情摘要。

## 任務記錄 (Quest Record)

[`00627F zzzAoMMq03 "Lazy Afternoon"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:199)

CLI 指令：
- `questdiag Vigilant.esm 0x00627F`
- `infodiag Vigilant.esm 0x00627F`

ESM 路徑：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務元數據：
- FormID: `Vigilant.esm:0x00627F`
- EditorID: `zzzAoMMq03`
- 名稱: `Lazy Afternoon` (悠閒的下午)
- 標誌: `RunOnce`
- 優先級: `90`
- 類型: `SideQuest`
- 過濾器: `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 標誌 | 日誌 |
|---:|---|---|
| 0 | 無 | 空 |
| 10 | 無 | 空 |
| 11 | 無 | 空 |
| 15 | 無 | 空 |
| 20 | 無 | 空 |
| 23 | 無 | 空 |
| 25 | 無 | 空 |
| 30 | 無 | 空 |
| 40 | CompleteQuest | 空 |
| 255 | ShutDownStage | 空 |
| 999 | CompleteQuest | 空 |

(總計 11 個階段，已驗證)

來自 `questdiag` 的目標 (Objectives)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:200) | 在敕旗母馬客棧與阿爾塔諾交談 |
| 10 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:201) | 跟隨阿爾塔諾或在燭爐堂與阿爾塔諾會合 |
| 15 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:202) | 跟隨阿爾塔諾 |
| 20 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:203) | 擊敗魔族 |
| 30 | [任務目標](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:204) | 向阿爾塔諾報告 |

目標對象 (Objective targets)：
- 5 個目標，每個都有地點對象。
- 目前的 CLI questdiag 不列印目標引用；如果確切的引用地點重要，則需要更深層的 QUST 別名/對象轉儲。

## 對話分支 (Dialogue Branches)

### 分支 1：任務簡報 (階段 0→10)

自定義話題：
- [`00884F zzAoMMq03B1Mission3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:56)

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`00884F zzAoMMq03B1Mission3`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:56) | `008850` | 無 | GetInCell `01605E:Skyrim.esm` (敕旗母馬客棧); 任務 `00627F` 階段 < 10; GetIsAliasRef alias #0 | [「風盔城的客棧有魔族惹出的麻煩。這件事可能跟之前的事件有關。我們走吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:57) |
| | | | 結束時執行 VMAD: `AoM03_TIF__01008850` Fragment_0 | |

### 分支 2：場景話題 (階段 10→20)

這些是對話話題結構化的場景交流。多個 INFO，無分支，無自定義條件 (無條件的場景流程)：

場景交流 1 (抵達風盔城客棧)：

| 話題 | INFO | 說話者 | 回覆 | 翻譯 |
|---|---|---|---|---|
| [`008853` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:59) | `008854` | 阿爾塔諾 | [「我們是斯丹達爾警戒者。我們聽說這家客棧裡有魔族。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:60) |
| [`008855` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:62) | `008856` | 客棧老闆 | [「是的，魔族幾天前出現後就一直待在這裡……你們能幫幫我們嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:63) |
| [`008857` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:65) | `008858` | 阿爾塔諾 | [「交給我吧。我們立刻去擊敗魔族……順便問一下，魔族是怎麼出現的？被召喚出來的？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:66) |
| [`008859` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:68) | `00885A` | 客棧老闆 | [「是的……一名被醉漢搭訕挑釁的女性召喚了魔族。醉漢被魔族撕碎了……我不想再回想了。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:69) |
| [`00885B` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:71) | `00885C` | 阿爾塔諾 | [「你知道那名女性去哪了嗎？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:72) |
| [`00885D` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:74) | `00885E` | 客棧老闆 | [「不，我不知道。那是她最後一次出現。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:75) |
| [`00885F` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:77) | `008860` | 阿爾塔諾 | [「明白了。我們感謝您的配合。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:78) (強制顯示字幕) |
| [`008861` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:80) | `008862` | — | (空) |
| [`008863` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:82) | `008864` | 阿爾塔諾 | [「魔族狩獵開始了。來吧。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:83) |

場景交流 2 (魔族遭遇，魔族正在睡覺)：

| 話題 | INFO | 說話者 | 回覆 | 翻譯 |
|---|---|---|---|---|
| [`008866` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:85) | `008867` | 阿爾塔諾 | [「喂，醒醒！！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:86) |
| [`008868` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:88) | `008869` | 魔族 | [「地毯多麼舒胡啊……感覺真好……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:89) |
| [`00886A` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:91) | `00886B` | 阿爾塔諾 | [「我在想……？他可能不危險……擊敗魔族的事就託付給你了。我去尋找召喚師。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:92) |

翻譯筆記：
- [第 69 行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:69): "teared up" → 可能意指「被撕碎」或「被殺害」 (腳本殘留物)。
- [第 66 行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:66): "apperas" → typo，意指 "appears"。
- [第 89 行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:89): "comfatoble" → typo，意指 "comfortable" (此處翻譯為「舒胡」以體現慵懶感)。
- [第 92 行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:92): "enturst" → typo，意指 "entrust"。

### 分支 3：任務完成 (階段 30→40)

自定義話題：
- [`00886D zzAoMMq03B2Mission3Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:94)

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`00886D zzAoMMq03B2Mission3Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:94) | `00886E` | Goodbye | 任務 `00627F` 階段 == 30; GetIsAliasRef alias #0 | 提示：(無) 回覆 1 (疑惑): [「一個古怪的魔族……總之，最重要的事情是抓住召喚師。魔力的痕跡顯示她就在附近……」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:95) 回覆 2 (中性): [「我會在風盔城尋找一會兒召喚師。如果你準備好了，就來找我。」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:96) |
| | | | 結束時執行 VMAD: `AoM03_TIF__0100886E` Fragment_0 | |

翻譯筆記：
- [第 95 行](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:95): "inmportant" → typo，意指 "important"; "cathcnig" → typo，意指 "catching"。

### 分支 4：魔族對話 (階段 20-30)

自定義話題：
- [`008870 zzAoMMq03B3Greet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:98)

問候 (由魔族，別名 #5 說出)：

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`008870 zzAoMMq03B3Greet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:98) | `008871` | WalkAway | GetIsAliasRef alias #5 | [「吼阿阿阿阿阿阿！！不可思議！！」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:99) |

喚醒話題 (玩家可以選擇嘲諷魔族)：

| 話題 | INFO | 標誌 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`008DD7 zzAoMMq03B3GetUp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:104) 提示="醒醒，魔族！你太麻煩了。" | `008DD8` | Goodbye | GetIsAliasRef alias #5 | 回覆 (悲傷): [「我知道，我知道。所以……怎麼了？」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:105) |

關於魔族別名的推論：
- 別名 #5 是階段 20–30 期間處於活躍狀態的魔族 (問候和喚醒互動)。
- 別名 #0 是阿爾塔諾 (用於任務簡報和完成)。

## 相關記錄 (Related Records)

根據 `infodiag`，這些並不完全屬於任務 `00627F`，但與任務流程相關。

NPC：
- `阿爾塔諾` (別名 #0，場景與分支對話中的說話者) — 在任務別名結構中透過 FormID 引用。
- 魔族 (別名 #5，問候與喚醒話題中的說話者) — 在任務別名結構中透過 FormID 引用。

地點：
- `01605E` (敕旗母馬客棧，風盔城) — 分支 1 中的條件目標；任務簡報地點。

## 重建筆記 (Reconstruction Notes)

基於來源 (Source-grounded)：
- 此任務由 [`00627F zzzAoMMq03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:199) 代表，包含五個目標，跨越客棧中的一次魔族遭遇。
- 包含一個由階段限制的自定義對話分支 (任務簡報，階段 0→10)。
- 包含八個場景交流話題 (不受限，與階段相關的流程，008853–00886A)。
- 包含一個由階段限制的完成分支 (階段 30→40)。
- 包含兩個魔族特有的話題 (問候、喚醒嘲諷，魔族 NPC 的別名)。
- 在 ESM 中未發現顯式 SCEN 記錄；場景流程是透過對話驅動的 (話題鏈，無階段/動作編排)。

階段進度推論：
- 階段 0：初始 (任務開始)。
- 階段 10：玩家已在敕旗母馬客棧與阿爾塔諾交談；場景流程開始。
- 階段 20：遭遇魔族；魔族問候/嘲諷流程啟動。
- 階段 30：玩家已擊敗魔族；完成對話可用。
- 階段 40：任務完成 (設置 CompleteQuest 標誌)。
- 階段 11, 15, 23, 25：中間檢查點 (用途從目標序列推論；CLI 輸出中無顯式條件)。
- 階段 255, 9999：清理 (255 上的 ShutDownStage 標誌；9999 上的 CompleteQuest 標誌，可能為冗餘或重試)。

待驗證事項 (Open verification)：
- 如果確切的演員引用或強制引用重要，直接檢查 QUST 別名 (別名 #0 = 阿爾塔諾, 別名 #5 = 魔族)。
- 如果需要精確的觸發地點，檢查敕旗母馬客棧 (`01605E`) 的單元引用。
- 如果需要解碼階段進度邏輯或分支結果路徑，反編譯腳本 `AoM03_TIF__01008850.Fragment_0`, `AoM03_TIF__0100886E.Fragment_0`。
- 如果中間檢查點重要，透過任務觸發器或遊戲內測試驗證階段 11, 15, 23, 25 的用途。
- 驗證階段 9999 是後備/重試路徑還是無效代碼 (如果階段 40 先完成了任務，則此階段似乎無法到達)。
