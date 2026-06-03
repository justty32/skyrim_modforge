# 常見陷阱 — 我們踩過的坑，以及解法

依領域分組。每一列為「症狀 → 根本原因 → 修正方式」。這些都是真正耗費過除錯時間的失敗模式；修正欄位是已驗證的解決方法。

← 返回 [lifelike 主頁](README.md) · 另見 [engine-internals](../engine-internals.md) 了解「原因」

## NPC 行為（套件、戰鬥、對話）

| 症狀 | 根本原因 | 修正方式 |
|---|---|---|
| 產生的套件其 `target` 讀取為 `LocationTarget` 而非 `LocationFallback`，即便使用了 `new LocationFallback()` | Mutagen 從 `LocationFallback.Type` 列舉取得二進位結構，而非 C# 類別 | `new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf }` |
| 裝備 Sandbox 的 NPC 無限期站立不動 | 使用了 `Type = NearEditorLocation`——需要 CK 設定的 Editor Location，而 Mutagen 產生的 NPC 缺乏此設定 | 使用 `Type = NearSelf`——錨定於目前位置，無需外部連結 |
| NPC 不移動，只在生成點站立（等待約 1 分鐘後仍無反應） | Sandbox 在附近找不到家具 / idle 標記 / 其他 NPC——區域過於空曠 | 將生成點移至有內容的場景（Bannered Mare、Sleeping Giant Inn）。Sandbox **需要**可互動的內容 |
| Sandbox NPC 在場景載入後的前 30–90 秒靜止不動 | 引擎 Sandbox 冷啟動延遲；屬正常現象 | **等待整整一分鐘**再判定失敗——原版 NPC 因玩家抵達前早已初始化而掩蓋了這個現象 |
| 規格中有 Travel 套件但 NPC 忽略它（在原地 Sandbox） | 引擎靜默拒絕跨場景 Travel——NPC 沒有「市民」身份無法穿越城門 | 設定 `crimeFaction` + 將城鎮派系加入 `factions` + `unique: true` |
| NPC 會走動但從不說話；靠近時只會發出喃喃聲 | 未設定 `voiceType`，或已設定聲音但無符合派系條件的對話主題 | 設定 `voiceType: MaleNord`（或類似值）。若要更多對話，加入城鎮派系讓派系條件對話主題生效 |
| 法師 NPC 遇到任何威脅就逃跑 | `Aggression=Unaggressive + Confidence=Cowardly`（Mutagen 預設值）——無論 CombatStyle 為何都會逃跑 | `aggression: "Aggressive"` + `confidence: "Brave"` |
| 城鎮居民將玩家視為敵對目標 / 主動挑釁 | `aggression: "Aggressive"` 會使 NPC *主動發起*攻擊 | 若要「只防禦不主動攻擊」的 NPC，使用 `aggression: "Unaggressive"` + `confidence: "Brave"`——Aggression 決定是否主動發起，Brave 決定受攻擊時是逃跑還是迎戰 |
| UseMagic NPC 站在位置上但**從不施法** | 欄位 3「Spell」以 `PackageTargetObjectType`（分類列舉）撰寫。全部 46 個原版 UseMagic 套件使用 `PackageTargetObjectID` 並連結至特定 SPEL | 規格欄位 `useMagic.spell: "<master>:0xFORMID"` 指向 SPEL；Build 寫入 `PackageTargetObjectID` |
| UseMagic NPC 施法 1–2 次後永久停止 | `numToCastMax` 是**套件整個生命週期的總施法次數**，而非每輪次數。當 `schedule.durationInMinutes=0` 時，套件在達到配額的瞬間即完成 | `numToCastMax: 1000` + `schedule.durationInMinutes: 1440`（24 小時持續） |
| UseMagic NPC 在戰鬥開始時停止施法 | 戰鬥 AI 搶佔閒置套件（原版行為） | 若施法必須持續（例如 Boss 儀式），加入 `flags: [ "IgnoreCombat" ]` |
| 排程套件（夜間睡眠、用餐時段 Sandbox）從不執行，或在錯誤時段執行錯誤套件 | NPC 的 `packages` 清單按**順序 = 優先級**評估：放置在排程套件**上方**的無條件套件在每個小時都會獲勝 | 依**從具體到一般**排序：排程 / 有條件套件**在前**，無條件後備**在後**。例：`[ "MF_NightSleep" (h=22 dur=540), "MF_Sandbox" (無條件) ]` |
| Sleep 套件（範本 `0x019717`）NPC 鎖住她入住的旅館 / 共用建築 | `sleep.lockDoors` 預設為 **true**（原版行為：NPC 夜晚鎖自己的房子） | 對任何在共用或公共空間（旅館、兵營）就寢的 NPC，設定 `sleep.lockDoors: false` |
| 產生的戰鬥法術從不被施放（NPC 改用近戰） | SPEL 沒有 `equipType`——NPC 無法將無法裝備的法術裝備到手上 | 設定 `spells[].equipType: "Skyrim.esm:0x013F44"`（EitherHand） |
| 巡邏 NPC 靜止不動，從不移動 | **靜態 REFR 不會像 Actor 一樣貼齊地板**——在猜測的外部 z 座標放置的標記會落在導航網格之外 | 將標記錨定在**已確認可行走**的座標上：使用 `refpos <plugin> <0xFORMID>` 複製可到達的原版 Ref 位置 |
| 插件包含程式建立的 **NAVM + NAVI** 時，**主選單 / 載入時崩潰**——**2026-06-01 已找出根本原因（來自 CrashLoggerSSE 日誌）** | 非 CK 產生的導航網格**沒有 PathingCell / 偏好路徑圖**——該結構由 CK 的**「Finalize Navmesh」**步驟產生。不完整的 NAVI **比完全沒有更糟**：有未完成 NAVI 的場景**在載入時崩潰** | **切勿從產生器輸出 NAVM/NAVI。** `autoNavmesh` 已確認為死路。需要真實路徑規劃的新場景，必須在 Creation Kit 中建立導航網格並執行 **Finalize Navmesh** |
| 想要可僱用的跟隨者，但「跟隨我」主題從未出現（**遊戲內已確認 It.24，2026-05-30**） | 原版免費跟隨僱用主題受一系列條件限制：聲音必須在 VoicesFollowerNeutral 中（FemaleNord/MaleNord 是市民聲音，**不在**跟隨者清單裡）；靜態 RELA 對玩家在執行期讀取為 0（**永遠無效**，玩家關係必須由腳本在執行期設定） | 使用**跟隨者聲音**：女性 `FemaleEvenToned 0x013ADD`；男性 `MaleEvenToned 0x013AD2`。加入 `PotentialFollowerFaction 0x05C84D`，設定 `greeting`，並用腳本設定關係值 |
| 付費僱傭招募主題在金幣 <500 時出現，但在金幣 ≥500 時**消失**（**遊戲內 It.26，It.27 找出根本原因**） | `HirelingQuestTopic1`（`0x0BCC84`）每個招募 INFO 都帶有 `GetIsID == <特定原版傭兵>`——自訂 NPC 全部無法通過。**沒有**通用的僅派系招募 INFO | 不要單靠 PotentialHireling 來招募。使用：**(a)** 免費跟隨 + 設定關係值的腳本；或 **(c)（推薦）** 自訂付費招募話題，在結果片段中呼叫 `DialogueFollowerScript.SetFollower(akSpeaker)` |
| 啟用含有 `relationships[]` 項目的模組時，**主選單崩潰** | RELA 的 `parent`/`child` 必須指向 **NPC_ 基礎記錄**；`0x000007` 是 **PlayerRef**（放置的 ACHR），而非 NPC_ 基礎 | 使用 `child: "Skyrim.esm:0x000014"` |
| 啟用含有自訂 `dialogue[]` 的模組時，**主選單崩潰** | 產生的 `DialogTopic` 留有 **SNAM = null**（`0x00000000`）；空的子類型在引擎建立對話主題索引時崩潰 | 在 `Subtype = Custom` 的同時設定 `topic.SubtypeName = new RecordType("CUST")` |
| 自訂對話不會崩潰，但**主題從未出現**（**遊戲內已確認修正 It.23，2026-05-30**） | 每個玩家 INFO 都缺少 `ENAM`（資訊旗標）+ `CNAM`（恩惠等級）——沒有 `ENAM` 的 INFO 被視為**無效**，其主題從選單中靜默移除。同時缺少每個任務的 **DialogView（DLVW）** 以及每個說話者的 **Hello** | 對每個產生的 INFO 設定 `Flags = new DialogResponseFlags()` 和 `FavorLevel = FavorLevel.None`。加上每個任務的 DLVW 和每個說話者的 Hello |
| 多個自訂主題時，有 2 個以上 NPC 沉默不語（**遊戲內 It.25**） | 所有玩家主題共享相同的**優先級（50）** | 給每個主題**不同的優先級**。Build 分配遞減序列（90、89、88…） |
| **自訂對話從不出現，除非在任務開始後重新載入遊戲**（**整個 It.23–26 對話問題的根本原因；遊戲內已確認 2026-05-30**） | Skyrim 在**遊戲載入時**註冊任務的玩家對話，而非任務開始時。從主選單 `coc` 或中途 `startquest` 從不重新註冊對話 | **測試方式：** 任務執行後，**`save` 再 `load`** 該存檔。**永遠不要**信賴主選單 `coc` 或中途 `startquest` 來顯示對話 |
| **在現有存檔上，Start-Game-Enabled 對話任務在首次載入時不會自動啟動**（**遊戲內已確認修正 2026-05-31**） | 引擎只在 SGE 任務列在 **`.seq` 檔案**（`Data/Seq/<plugin>.seq`）中時，才在存檔載入時強制啟動它們 | **`build`/`package` 現在自動寫入 `Seq/<plugin>.seq`**。隨插件一起出貨 `Seq/` 資料夾 |
| **任務執行中（`sqv` 顯示為進行中）但沒有日誌項目**（**遊戲內 It.35→It.36，2026-06-02**） | QUST **類型（DNAM）** 為 **None**——type=None 的任務是後台任務，**永遠不會出現在玩家的日誌中** | **`build` 現在設定任務類型**：任何有日誌內容的任務預設為 **SideQuest** |
| **日誌崩潰**——當任務可見的頁面被建立時立即發生（**遊戲內 It.36**） | QSDT 子記錄在普通階段遺漏——孤兒 CNAM 使引擎的任務解析器失去同步 → 記憶體存取違規 | **永遠指定 `le.Flags`（無完成/失敗時為 0）**，這樣 Mutagen 就會為每個日誌項目輸出 QSDT 標記 |
| **任務階段推進（`setstage` 有效）但任務目標從不更新**（**遊戲內 It.36**） | `QuestScriptFragment.Unknown2` 為 **0**——引擎在 `SetStage()` 觸發時靜默跳過片段 | 每個 `QuestScriptFragment` 使用 **`Unknown2=1, StageIndex=0`** |
| **對話結果片段觸發（階段推進），但目標仍不更新** | TIF 片段呼叫了 `GetOwningQuest().SetStage(N)`——`GetOwningQuest()` 對 StartGameEnabled 任務**在 `OnBegin` 時回傳 None** | 在 TIF `.psc` 中使用**明確的 Quest 屬性**：`Quest Property OwningQuest Auto`，繫結至任務的 FormKey |
| **INFO 結果腳本在 `OnEnd` 觸發，但階段不推進** | 原版 CK 對推進任務的對話使用 **`OnBegin`**，而非 `OnEnd` | 在 `DialogResponsesAdapter` 中使用 **`OnBegin`** |
| **玩家選過後，NPC 持續提供相同對話台詞** | 沒有 `GetStage` 條件限制該台詞 | `package` 現在會為每個 `setStage` 對話台詞**自動加入** `GetStage(quest) < setStage` 條件 |
| 放置在場景原點 **(0,0,0)** 的無套件 NPC 無法被到達/交談 | (0,0,0) 是場景原點，通常在牆壁內部 / 導航網格之外 | 放置在靠近原版 NPC 的真實室內座標。Actor 會貼齊導航網格，例如 Sleeping Giant Inn 公共區域 ≈ (-350,180,0) |
| 自訂對話回應字幕一閃而過 / 難以閱讀（或 NPC 看起來沉默無聲） | 未配音台詞因缺少語音檔而獲得約 0 秒的持續時間 | 安裝 **Fuz Ro D-oh — Silent Voice** 並開啟對話字幕 |

## 場景、世界空間、光照

| 症狀 | 根本原因 | 修正方式 |
|---|---|---|
| 無頭建置拋出「Could not determine plugin listings path」 | 複製本地化的 `TranslatedString` 觸發需要 plugins.txt 的全字串來源解析——在 Linux 無頭環境下缺少此檔案 | 向 `DeepCopyIn` 傳入 `TranslationMask { Name=false, … }` |
| 外部放置後整個世界被淹沒 / 「全部在水下」 | Cell/Worldspace 覆寫**不會**從主記錄繼承省略的資料 | 手動複製環境子記錄（`CopyWorldspaceEnv` / `CopyCellEnv`） |
| 原版內部場景覆寫靜默地被忽略 | 內部場景依 FormID 分組到 block/sub-block GRUP 中；在錯誤 GRUP 中的覆寫永遠不會與主記錄匹配 | 從場景 FormID 計算 GRUP：**block = id % 10, sub = (id/10) % 10** |
| 規格中的新內部場景在 `coc` 時漆黑一片 / 玩家掉入虛空 | 全新場景沒有光照/LightingTemplate 且沒有地板幾何體 | 給 `cells[].template` 一個原版內部參照；加入放置的靜態地板格（例如 `WRIntFloorSTMid01Large 0x1044AA`） |
| 放置在**無地板**的新場景中的 NPC 行為異常——例如，商人的交易選單從不開啟（**遊戲內已確認 2026-05-31**） | NPC 掉入虛空處於**持續墜落狀態**，永遠不會進入「值勤」狀態 | 在新場景中任何放置的 NPC 下方**鋪設地板** + 給它一個值勤 Sandbox 套件 |
| 放置的光源幾乎照不到任何東西 | 光源基礎是 `PortalStrict`——只照亮房間入口內部的物體，而開放場景沒有房間標記 | 使用非 PortalStrict 的全向陰影光源（`WRShadowOmni 0x0C82AE`，半徑 512） |

## 物品、模型、魔法效果

| 症狀 | 根本原因 | 修正方式 |
|---|---|---|
| 產生的物品掉落時沒有模型（或在裝備/閱讀時崩潰） | 武器/書籍/雜項/藥水沒有 `.nif`——在物品欄沒問題，但**在任何場景互動時崩潰** | 設定 `template: "<master>:0xFORMID"` 以複製原版記錄的模型（IronSword `0x012EB7`、Book1CheapNordsArise `0x0ED161`、GemRuby `0x063B42`、RestoreHealth06 `0x039BE5`） |
| 產生的**護甲裝備後隱形**——**已修正 + 遊戲內已確認 2026-06-01** | ARMO 的穿戴網格位於其**骨架**（ARMA 附加記錄）上，*而非* ARMO 本身。只有 `BodyTemplate` 的規格護甲有空骨架——裝備時什麼都不渲染 | 設定 `template: "<master>:0xFORMID"` 以複製相同部位的原版護甲（ArmorIronCuirass `0x00012E49`） |
| 玩家**學會自訂喊聲但無法喊出** | 每個喊聲詞語指向沒有 **`EquipmentType`（EQUP）** 的 Voice 類型 SPEL——引擎無法將其裝備到聲音槽 | Build 現在在省略 `equipType` 時，對**可施放法術類型預設使用 EitherHand** |
| 自訂喊聲觸發了效果，但**沒有龍吼聲音** | 喊出的聲音是 MGEF 的 **Release 音效**——沒有 `sounds` 的喊聲 MGEF 會靜默地施放 | 給 MGEF 設定 `sounds: [{ type: "Release", sound: "<master>:0xFORMID" }]` |
| 複製的藥水效果加倍/堆疊 | `DeepCopyIn` 保留了範本自身的效果，然後與規格效果堆疊 | Build 在複製後執行 `r.Effects.Clear()` |
| 自訂 MGEF 治癒法術施放了但沒有治癒效果 | 即時效果上的 `Recover` 旗標會在效果「結束」的瞬間（立刻）回滾治癒 | 即時效果（持續時間 0）必須使用 `["NoDuration","NoArea"]` 且**不帶** `Recover` |
| 自訂效果消耗誇張的魔力 | 在自動計算下，高 `baseCost` × 效果量級 | 保持 `baseCost` 較低 |
| 啟用含有**入口點天賦**的模組時，**「Loading Files」時崩潰**（**遊戲內 2026-05-31 找出根本原因**） | `PerkConditionTabCount = 0`——索引 0 的 `PRKC` 分頁計數為 0 時陣列溢位 → 垃圾 FormID 查詢 → 記憶體存取違規崩潰。Mutagen 在讀取時忽略該位元組，因此傾印/往返看起來正常——只有執行期解析器崩潰 | `Build` 從原版表格設定 `PerkConditionTabCount`（永不為 0） |

## 對話

| 症狀 | 根本原因 | 修正方式 |
|---|---|---|
| 自訂對話記錄有效，但與 NPC 交談時主題**從不出現** | 宿主**任務未設定 Start Game Enabled** 和/或 **DialogBranch 未設定為 Top-Level** | 任務 `flags |= StartGameEnabled`（+ 一個 `Priority`）；分支 `Flags = TopLevel` |
| 選單台詞標籤錯誤 / 顯示了錯誤的文字 | INFO 的 `Prompt` 被設定了，覆蓋了選單台詞 | 將 INFO 的 `Prompt` 留為 null——選單台詞來自 `topic.Name` |
| 自訂對話台詞沒有聲音 / NPC 嘴唇不動 | 沒有錄製的語音（.fuz/.lip）——對產生的對話這是預期行為 | 開啟**一般 + 對話字幕**（設定 ▸ 顯示） |
