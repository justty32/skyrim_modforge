# 第一章 任務 10 - 降落地點 (Landing Spot)

狀態：基於原始碼的片段，連結優先，非劇情摘要。

來源策略：
- 原始對話行連結回提取的來源檔案。
- 僅在需要解釋翻譯問題或分支極性時才顯示短小的原始碼片段。
- 對話條件與場景主題結構來自 CLI 診斷。

## 任務紀錄 (Quest Record)

[`011B75 zzzAoMMq10 "Landing Spot"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1)

CLI：
- `questdiag Vigilant.esm 0x011B75`
- `infodiag Vigilant.esm 0x011B75`

ESM：
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

來自 `questdiag` 的任務資料：
- FormID: `Vigilant.esm:0x011B75`
- EditorID: `zzzAoMMq10`
- 名稱 (Name): `Landing Spot`
- 旗標 (Flags): `RunOnce`
- 優先度 (Priority): `90`
- 類型 (Type): `SideQuest`
- 過濾器 (Filter): `AoM\`

來自 `questdiag` 的階段 (Stages)：

| 階段 | 旗標 | 日誌 |
|---:|---|---|
| 0 | 無 | 空白 |
| 5 | 無 | 空白 |
| 10 | 無 | 空白 |
| 20 | 無 | 空白 |
| 30 | 無 | 「莫拉格·巴爾在我身上留下了詛咒。我的靈魂將在第二次腐化時被拖入他的領域。」 |
| 39 | 無 | 空白 |
| 40 | CompleteQuest | 空白 |
| 50 | CompleteQuest | 空白（ESM 中的重複行） |
| 255 | ShutDownStage | 空白 |
| 999 | 無 | CompleteQuest |
| 9999 | 無 | CompleteQuest |

來自 `questdiag` 的目標 (Objectives)：

| 索引 | 來源 | 翻譯 |
|---:|---|---|
| 0 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | 與阿爾塔諾對話 (Talk to Altano) |
| 10 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | 摧毀莫拉格·巴爾的釘頭錘 (Destroy Mace of Molag Bal) |
| 30 | [任務目標](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | 回到斯丹達爾神殿 (Back To Temple of Stendarr) |

目標對象 (Objective targets)：
- 總共 3 個目標（每個目標 1 個對象）。
- 目前的 CLI 輸出未印出目標對象；若位置標記很重要，則需要更深入的 QUST 目標轉儲。

## 別名 / 編排主幹 (Alias / Staging Backbone)

CLI 在此任務的 `questdiag` 輸出中未印出別名。從 `infodiag` 的說話者模式可推斷：
- 別名 ReferenceAliasIndex=1：由 `zzAoMMq10B1*` 分支使用（分支 013675）。
- 別名 ReferenceAliasIndex=6：由 `zzzAoMMq10B2*` 分支使用（分支 028538）。

推論：別名 #1 是垂死的阿爾塔諾（分支 1 發生在對峙期間）；別名 #6 是神殿事件後的倖存 NPC（分支 2）。

## 場景紀錄 (Scene Records)

`infodiag` 輸出中僅明確存在一個場景主題：

### 0x013BE5 (未命名場景主題)

來自 `infodiag`：
- 主題 FormID: `0x013BE5`（場景子類型）
- 類別: `Scene`
- SNAM: `SCEN`
- 優先度 (Priority): `50`
- 任務擁有者: `011B75:Vigilant.esm`
- 分支擁有者：無（無分支）

INFO：
- FormID: `0x013BE6`
- 旗標：無
- 提示 (Prompt)：空白
- 回應 (Response): [`"Son of Stendarr..I see you. When your soul is corrupt, you open the gate of my realm..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914)
- 情感：中立
- VMAD: 腳本檔案 `AoM10_TIF__01013BE6` 包含 OnEnd 片段

翻譯：
- 該回應是莫拉格·巴爾或受其附身的實體的獨白，在關鍵時刻對玩家說出。
- 翻譯：「斯丹達爾之子……我看到你了。當你的靈魂腐化時，你便開啟了通往我領域的大門……」
- 這段對話很可能發生在階段 30–40 的轉換期間，即莫拉格·巴爾詛咒顯現之時。

備註：
- `scenediag 0x013BE5` 回傳「非 Vigilant.esm 中的場景」，暗示這是一個標記為場景的 DIAL 主題，而非真正的 SCEN 紀錄。其中不存在階段或動作。

## 自訂對話分支 1：阿爾塔諾的背叛

分支：
- `013675:Vigilant.esm`（分支紀錄，包含主題 `013676`, `013678`, `01367A`）

說話者條件模式：
- 所有 INFO 皆要求別名 `#1` 滿足 `GetIsAliasRef == 1`。
- 此分支與階段無關；無 `GetStage` 條件。

主題與 INFO：

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`013676 zzAoMMq10B1LastWord`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:913) | `013677` | 無 | `GetIsAliasRef alias #1` | [`"aa.....uaa..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:913) |
| [`013678 zzAoMMq10B1BetrayReason`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:915) | `013679` | 無 | `GetIsAliasRef alias #1` | 提示：[`"Why did you do Such things?Altano?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:915) 回應 1：[`"After the battle with Bal.....I heard sweet whispering....I can't help but follow the voice."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:916) 回應 2：[`".....I yield to temptation....Excuse me....."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917) |
| [`01367A zzAoMMq10B1BlessAltano`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:918) | `01367B` | `Goodbye` | `GetIsAliasRef alias #1` | 提示：[`"Rest in peace,Altano...Stenndarr always with us."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:918) 回應：[`"Thank you......I..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:919) VMAD: `AoM10_TIF__0101367B` 包含 OnEnd 片段 |

翻譯筆記：
- `"aa.....uaa..."` 似乎是臨終前的喘息或受腐化的言語。
- 提示 013678 翻譯：「你為什麼要做這種事？阿爾塔諾？」回應 1 翻譯：「與巴爾戰鬥過後……我聽到了甜美的低語……我忍不住跟隨那個聲音。」回應 2 翻譯：「……我向誘惑屈服了……對不起……」
- 提示 01367A 翻譯：「安息吧，阿爾塔諾……斯丹達爾永遠與我們同在。」回應翻譯：「謝謝……我……」
- `01367B` 上的 `Goodbye` 旗標 + VMAD 暗示此主題會完成或觸發任務階段轉換（很可能是階段 40 `CompleteQuest`）。

## 自訂對話分支 2：神殿餘波

分支：
- `028538:Vigilant.esm`（分支紀錄，包含從 `028539` 到 `028543` 的 6 個主題）

說話者條件模式：
- 所有 INFO 皆要求別名 `#6` 滿足 `GetIsAliasRef == 1`（除了 `028539` 的開場 INFO 僅有別名條件）。
- 階段限制僅出現在 `028539`：`GetStage == 30`（僅限開場）。

主題與 INFO：

| 主題 | INFO | 旗標 | 條件 | 翻譯 |
|---|---|---|---|---|
| [`028539 zzzAoMMq10B2LastWord`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) | `02853A` | 無 | `GetStage == 30`; `GetIsAliasRef alias #6` | [`"Welcome back ... How was Altano...?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) |
| [`02853B zzzAoMMq10B2AltanoDead`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) | `02853C` | 無 | `GetIsAliasRef alias #6` | 提示：[`"Altano died"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) 回應：[`"You killed him....?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) |
| [`02853D zzzAoMMq10B2MolagKillHim`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) | `02853E` | 無 | `GetIsAliasRef alias #6` | 提示：[`"No, Molag Bal killed him"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) 回應：[`"I'm sorry ... that so ... Molag Bal appeared ... what soul we do..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) |
| [`02853F zzzAoMMq10B2DefeatMolag`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) | `028540` | 無 | `GetIsAliasRef alias #6` | 提示：[`"I defeated Molag Bal. There is no danger for a while"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) 回應 1：[`"I can not believe you did defeated Molag Bal ... Molag Bal is Daedra Lord..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:924) 回應 2：[`"It is incredible... but the eyes are saying that it is true. All right, I believe you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:924) |
| [`028541 zzzAoMMq10B2DoNext`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) | `028542` | 無 | `GetIsAliasRef alias #6` | 提示：[`"What should we do now?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) 回應 1：[`"Well, you and I are suvivor in temple... Keeper died ..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) 回應 2：[`"I got it!! You should become new keeper of Stendarr  because you have power defeated Molag Bal"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:926) |
| [`028543 zzzAoMMq10B2DecideKeeper`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) | `028544` | `Goodbye` | `GetIsAliasRef alias #6` | 提示：[`"Can we decide it?"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) 回應 1：[`"It's OK!! Stendharr will admit you. I'm belive you"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) 回應 2：[`"I'm sure it's okay if you. Okay ..."`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:928) VMAD: `AoM10_TIF__01028544` 包含 OnBegin 片段 (Fragment_1) 與 OnEnd 片段 (Fragment_0) |

分支極性：
- 玩家在 `02853B` → `02853D` 的選擇代表了**責任歸屬**（是玩家殺了阿爾塔諾，還是莫拉格·巴爾？）。
- `02853F` 是一個後續動作，測試玩家是否聲稱擊敗了莫拉格·巴爾。
- `028541` 轉向**未來的角色**：原有的負責人已不在；玩家成為新負責人。
- `028543`（標記為 `Goodbye`）是結案主題；`028544` 上的 VMAD 可能觸發階段 40 `CompleteQuest` 或任務最終結算。

翻譯筆記：
- 提示 028539 翻譯：「歡迎回來……阿爾塔諾怎麼樣了……？」
- 提示 02853B 翻譯：「阿爾塔諾死了。」回應翻譯：「是你殺了他的……？」
- 提示 02853D 翻譯：「不，是莫拉格·巴爾殺了他。」回應翻譯：「我很遺憾……原來是這樣……莫拉格·巴爾現身了……我們的靈魂該如何是好……」
- 提示 02853F 翻譯：「我擊敗了莫拉格·巴爾。暫時沒有危險了。」回應 1 翻譯：「我不敢相信你擊敗了莫拉格·巴爾……莫拉格·巴爾可是魔神……」回應 2 翻譯：「這令人難以置信……但你的眼神告訴我這是真的。好吧，我相信你。」
- 提示 028541 翻譯：「我們現在該怎麼辦？」回應 1 翻譯：「好吧，你我是神殿的倖存者……負責人死了……」回應 2 翻譯：「我明白了！！你應該成為斯丹達爾的新負責人，因為你擁有擊敗莫拉格·巴爾的力量。」
- 提示 028543 翻譯：「這能由我們決定嗎？」回應 1 翻譯：「沒問題的！！斯丹達爾會認可你的。我相信你。」回應 2 翻譯：「如果是你的話肯定沒問題。好吧……」
- 「suvivor in temple」包含拼字錯誤（應為「survivor」）；原始碼按原樣保留。
- `028539` 上的階段 30 `GetStage` 條件限制了開場問候；在玩家完成主要任務目標（擊敗莫拉格·巴爾或阿爾塔諾）後，此對話開始。

## 任務流程摘要

1. **階段 0–30**：玩家接近神殿或遇到受腐化的阿爾塔諾。
2. **階段 30**：任務日誌項目出現；[`013BE5` 場景主題](#scene-topic-0x013be5) 播放莫拉格·巴爾的詛咒獨白。
3. **分支 1** (`zzAoMMq10B1*`)：玩家透過主題 `013676–01367A` 與垂死/受腐化的阿爾塔諾（別名 #1）互動。
   - 若玩家選擇祝福阿爾塔諾 (`01367A`)，則執行腳本片段 `AoM10_TIF__0101367B`。
4. **階段 40**：任務標記為 `CompleteQuest`；玩家返回神殿。
5. **階段 50**：另一個 `CompleteQuest` 項目（可能是 ESM 中的重複項或保險措施）。
6. **分支 2** (`zzzAoMMq10B2*`)：在階段 30+，玩家與倖存者（別名 #6，可能是與索隆迪爾相同的 NPC 或另一位負責人）交談。
   - 對話鏈：歸咎阿爾塔諾 vs. 莫拉格·巴爾 → 玩家宣稱勝利 → 角色協議。
   - 最終主題 `028543` (Goodbye, VMAD) 可能觸發階段 40 的完成。

## 相關紀錄

根據 `infodiag`，這些不全然屬於任務 `011B75` 的一部分，但被對話或敘事背景所引用：

NPCs：
- [`zzzAoMMq07` 別名背景](#) — 阿爾塔諾出現在任務 7 (老聖騎士) 與任務 10 (降落地點) 中；任務 10 可能是直接的續作。

物品：
- 莫拉格·巴爾的釘頭錘 (Mace of Molag Bal) —— 在目標 10 中提到，但 FormID 未在對話中提取。

## 重建筆記

基於原始碼：
- 此任務代表阿爾塔諾受到莫拉格·巴爾誘惑/腐化後與其進行的對峙。
- 任務有兩個對話分支：一個發生在高潮時刻（阿爾塔諾的死亡/腐化），一個發生在神殿餘波中。
- 場景主題 (`013BE5`) 傳達了莫拉格·巴爾的詛咒獨白，但它只是純主題紀錄，而非具有階段/動作的真正 SCEN。
- 任務在倖存者（別名 #6）與玩家就新負責人角色達成共識後完成。

開放驗證：
- 若存在原始碼，請檢查腳本 `AoM10_TIF__0101367B` 與 `AoM10_TIF__01028544`；它們可能包含階段推進邏輯。
- 直接檢查 QUST 別名定義（若有更豐富的別名轉儲）以確認別名 #1 = 阿爾塔諾，別名 #6 = 倖存者。
- 驗證分支 1 對話發生的儲存格/地點（可能是冷港或任務室內場景）。
- 驗證莫拉格·巴爾釘頭錘的 FormID，並確認目標 10 是摧毀特定的物品實例。
- 確定階段 40 與 50 是否代表不同的完成路徑（玩家選擇的結果），或是階段 50 僅是殘留的重複項。
