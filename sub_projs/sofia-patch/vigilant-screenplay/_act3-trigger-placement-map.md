# Act 3 — Sofia 評論觸發點 / 場景 / 地點 放置地圖

> 目的：確定每一條 Sofia 評論都掛在**對的 VIGILANT stage / 場景 / 地點**上（情境正確）。
> 來源：BSA 明文 QF_/SF_/TIF_ 碎片逆向 + CLI questdiag/infodiag（2026-06-14）。Stage 語意由 PSC fragment 直接解碼。
> Act 3 VIGILANT 任務鏈：`zzzCOMq01`（Child of Oblivion, 0x065932）主線 + `zzzCOGuide`（Stendarr Guide, 0x43CBAE）curse-breaking 副線 + `zzzCOSubQ01`（Successor, 0x324E7E）燒死分支 + `zzzCOqOwl`（Weaver's Needle 2, 0x444115）owl 插線。

---

## 1. Act 3 任務鏈說明

```
zzzCOMq01 (0x065932) "Child of Oblivion"
  s0  startup: gCurrentAct=3, Obj0 "Talk to Gwyneth"（Fragment_16）
  s10 after Gwyneth dispatch; cart enabled; Khajiit01 enabled
  s20 Go to Noble Mansion: Obj20 displayed; Baal disabled; Khajiit disabled; Mansion01IntDoor disabled
  s30 Investigate mansion（Obj30 "Investigate the mansion and to solve the case"）
  s40 Julia defeated → BalScene starts; GhostMarker enabled
  s45 BalScene ends（SF_zzzCOq01BalScene01 Fragment_0 → SetStage(45)）
  s50 Julius phase begins（qGuide.SetStage(70); Obj60 "Defeat Julius" displayed; MusJulius.Add）
      Fragment_13 in PSC fires at s50
  s60 [used by qGuide obj60 "Release Julius"]; in Mq01 context: Julius combat active
  s70 Julius defeated（Fragment_8）: MolagBal enabled; PieceOfBal given; MusJulius removed; Obj70 displayed
  s80 MartyrTRG enabled; Obj80 "Martyrdom or Corruption" displayed（Fragment_10）
  s90 Martyrdom chosen（burn path）: Pious+9, MartyrTRG disabled, zzzCOSubQ01 starts → Act3 completes Mq01 Stop
  s100 Good End / CompleteQuest
  s200 Corruption chosen（Molag Bal deal = into Coldharbour）: Pious+3 Radiance+6, MansionDoor disabled,
       MartyrQuest(SubQ01) starts, → Act4 CHMq00 starts
  s999 Skip

zzzCOGuide (0x43CBAE) "Stendarr Guide" — optional curse-breaking side quest
  s0  startup
  s1  Guide enabled
  s10 First totem: Obj10 "Break curse of Shivering"
  s20 Totem01 broken, get Servant Key → Obj20 "Break curse of Depravity"
  s22 Totem02 broken, get Julius Room Key → Obj22 "Go to Julius's Room"
  s24 Get Basement Key → Obj24 "Go To Basement"
  s30 Totem03 broken (Foamy), get Garden Key → Obj30 "Break curse of Foamy"
  s35 Totem04 broken, get Vigil Key → Obj35 "Gain Key of Bartolo's Room"
  s40 Totem04 complete → Obj40 "Break curse of Chain"
  s50 Totem05 broken (Envy) → Obj50 "Break curse of Envy" → Obj60 "Release Julius"
  s60 [Julia room key phase]
  s70 CompleteQuest（Fragment_21: Julius released, Mq01 continues to Julius boss）
  s999 CompleteQuest (fallback)

zzzCOSubQ01 (0x324E7E) "Successor" — martyrdom / burn path
  s0  Fade out, RemoveSpell, MoveTo NewLifeMarker → SetStage(10)（player "dies"）
  s10 Player appears in Stendarr temple as new character
  s20 ShowRaceMenu（character re-roll = meta twist）→ SetStage(20)
  s30 Confirm keeper's safety; MolagMouthTRG enabled; StendarrHorn given; MolagCurse added
  s40 Report to Gwyneth → CompleteQuest
  s60 CompleteQuest (fast-track)
  s9999 CompleteQuest (skip/fallback)

zzzCOqOwl (0x444115) "Weaver's Needle 2" — Bal owl optional interaction
  s0  Owl call: owl enabled at MansionMarker
  s5  Obj5 "Talk to stranger" (skip option to Act4)
  s10 Needle given; Bal becomes non-ghost / killable
  s20 Objective "Talk to Orland"
  s30 Objective "Go ahead"
  s40 CompleteQuest（→ qCH/Act4 starts）
  s255 Alternative end (skip → qCO.SetStage(999) then Act4)
```

**Chain trigger points:**
- Mq01 s0/s10 → player dispatched to mansion
- Mq01 s40 (Julia defeated) → BalScene plays → s45
- Mq01 s50 → Julius boss phase (qGuide.SetStage(70) means curse-breaking complete)
- Mq01 s70 (Julius defeated) → Molag Bal appears → s80 martyrdom choice
- Mq01 s90 → burn/martyrdom → SubQ01 starts (meta twist: race re-roll)
- Mq01 s200 → corruption deal → CHMq00 (Act 4) starts

---

## 2. 評論放置總表

| beat | 機制 | 正確 gate（除 `GetIsID Sofia` 外） | 依據（碎片 + questdiag） | 信心 |
|---|---|---|---|---|
| **3-A 宅邸基調：恐怖主動吐槽** | 玩家可問（多條輪播） | `GetStageDone(Mq01 0x065932, 20)==1` + `GetStageDone(Mq01, 50)==0` | Mq01 s20=Mansion arrived（Obj30 active）；s50 前 investigation 仍在進行中；mansion cell 期間最長窗口 | 高 |
| **3-B 讀文本 → 評論** | 玩家可問（讀後可問） | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 50)==0` | 同 3-A；讀書 beat 在 investigation 期（Obj30 active）最合理 | 高 |
| **3-C 觸碰塑像 / 進地圖打怪** | 玩家可問（環境吐槽） | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 40)==0` | s20-s40 = mansion exploration（Julia 未倒）；塑像/curse totem 與 zzzCOGuide s10-s50 同期 | 高 |
| **3-D 紅女巫（雜兵）→ 邊吐槽邊打** | 玩家可問（戰鬥期） | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 40)==0` | 同 3-C；紅女巫是 mansion 雜兵，Julia 未倒前 | 高 |
| **3-E 紅魔女（大 boss Julia）感想（戰前/戰後）** | 玩家可問（戰前 s30+；戰後 s40+） | 戰前：`GetStageDone(Mq01, 30)==1` + `GetStageDone(Mq01, 40)==0`；戰後：`GetStageDone(Mq01, 40)==1` + `GetStageDone(Mq01, 50)==0` | Mq01 s40=Julia defeated（Fragment_6: BalScene starts, GhostMarker enabled）；戰前接近尾聲探索；戰後進入 Bal scene 過渡期 | 高 |
| **3-F 管家 Baal（認出吟遊詩人）Sofia 生氣** | 玩家可問（主動觸發，認出時） | `GetStageDone(Mq01, 20)==1` + `GetStageDone(Mq01, 40)==0` | Alias_Bal 在 Mq01 s20 時被 disable（Mansion01IntDoor disabled），Baal 作為 butler 應在 mansion entry 期間出現；s40 後 Bal alias 仍在 GhostMarker 場景 | 中（Baal 具體 alias/FormID 待確認；infodiag 顯示 alias_index=3 for BalB01 branch，可能是 Balthoro = butler，非 bard = Baal；兩者可能同一角色的不同稱謂） |
| **3-G Julius 火焰 boss → 奮起** | 玩家可問（戰中） | `GetStageDone(Mq01, 50)==1` + `GetStageDone(Mq01, 70)==0` | Mq01 Fragment_13(s50): "VS Julius"，MusJulius added；Obj60 "Defeat Julius" displayed；s70 = Julius defeated | 高 |
| **3-H 抉擇前（Sofia 表態）** | 玩家可問 | `GetStageDone(Mq01, 70)==1` + `GetStageDone(Mq01, 80)==0` + `GetStageDone(Mq01, 90)==0` | Mq01 s70（Julius defeated, Molag Bal spawns）→ s80（Fragment_10: MartyrTRG enabled, Obj80 displayed）；fire sequence starts | 高 |
| **3-H 進入冷港分支** | 玩家可問 | `GetStageDone(Mq01, 200)==1` + `GetGlobalValue("MF_SofA3_BurnChoice")==0` | Mq01 s200（Fragment_5: Pious+3 Radiance+6, → CHMq00/Act4）= 玩家接受 Molag Bal 要求入冷港 | 高 |
| **3-H 被燒死分支（+後來的茫然）** | 玩家可問（兩條：擇前 + 傳送後） | 被燒：`GetStageDone(Mq01, 90)==1` + `GetGlobalValue("MF_SofA3_BurnChoice")==0`；茫然：`GetStageDone(SubQ01 0x324E7E, 10)==1` | Mq01 s90（Fragment_14: Pious+3 Radiance+6, MartyrFire enabled → SubQ01 starts）= 燒死路；SubQ01 s10 = 傳送後「新生」（換臉前） | 高 |
| **3-I Meta 轉折（焦屍台詞，可選）** | 玩家可問（可選，進宅邸後見焦屍） | `GetStageDone(SubQ01 0x324E7E, 20)==1` + `GetStageDone(SubQ01, 40)==0` | SubQ01 s20 = ShowRaceMenu（重新捏臉 = 龍裔接管）；焦屍可能是 BurntCorpse property（PSC 有引用）；s40 CompleteQuest 後 | 中（焦屍具體 cell/ref 位置未確認；此 beat 本身標記為「可選」） |

每條皆 `sayOnce` + 各自 GLOB once-flag；冷港/燒死兩分支互斥加對方 GLOB==0。

---

## 3. 關於 Baal / Balthoro 身份釐清（3-F 中信心說明）

從 `infodiag` 輸出：
- `zzzCOq01BalB01T01`（alias_index=3）：說話者問候「我丈夫在等你」→ **女性 NPC，Balthoro 之妻**
- `BalScene`（Fragment_6 in Mq01 s40）是打倒 Julia 後的 cutscene；`SF_zzzCOq01BalScene01` 在結束時 SetStage(45)
- 本 patch 設計中「管家 Baal = 第二幕吟遊詩人」是劇情詮釋；Vigilant ESM 中 Balthoro 是一個 Vigilant 成員（Gwyneth 提到他被派去調查）
- **Sofia 認出他的 gate 最安全用 `GetStageDone(Mq01, 20)==1`**（已入宅邸），因為他是玩家在 mansion 見到的第一個 NPC；alias_index=3 對應 Mq01 alias slot 3（Bal alias，在 s20 時 TryToDisable，表示他是初期出現者）
- 3-F beat 的「認出」本質是 Sofia 劇情延伸，無需精確對應 VIGILANT 場景事件

---

## 4. 關於 3-H 火焰/冷港分支機制

從 PSC 明確讀出：
- **冷港路**：Mq01 Fragment_5 (s90)= `CHMq00.Start(); CHMq00.SetStage(0)` → 直接進 Act4。沒有「進門」的動畫，就是直接啟動下一 quest。
- **燒死路**：Mq01 Fragment_14 (s200)= `MartyrFire.Enable(); Game.GetPlayer().RemoveItem(StendarrHorn)` → `MartyrQuest(SubQ01).Start()` → SubQ01 Fragment_0 淡出 → MoveTo NewLifeMarker → ShowRaceMenu。

**注意**：Mq01 stage 語意中 s90 = Martyrdom/Burn（良知路，Pious+9），s200 = Corruption/Coldharbour（Molag Bal 交易路，Pious+3 Radiance+6）。即：
- **進冷港** = s90（接受燒死後「神蹟」送入冷港，Pious=虔誠路）← 但 CHMq00 在 s90 Fragment_14 中並未直接 Start；再看：Fragment_14 啟動 SubQ01（燒死分支），Fragment_5 的 `CHMq00.Start()` 是在 s90 被觸發的。
- **PSC 重確認**：Fragment_5 in Mq01 = `SetObjectiveCompleted(70); SetObjectiveFailed(80); ModPious(9.0); CHMq00.Start()` → 這對應 **martyrdom resolved / burn complete** → CHMq00（Coldharbour quest）啟動。Fragment_14 = `SetObjectiveFailed(70); SetObjectiveCompleted(80); ModPious(3.0); ModRadiance(6.0); MartyrQuest.Start()` → 這是「燒死但選擇腐化」路或「死後去另一個地方」。
- **結論**：Fragment_5 (s90 label) = 虔誠路/燒死＋接受 → 進冷港（Act4 starts）；Fragment_14（s200 label） = 腐化路/接受 Molag Bal 交易 → SubQ01（meta 轉折重生）。

修正後的分支映射：
- **Sofia 3-H 冷港台詞**：gate = `GetStageDone(Mq01, 90)==1`（Fragment_5 fires → CHMq00 starts）
- **Sofia 3-H 燒死台詞**：gate = `GetStageDone(Mq01, 200)==1`（Fragment_14 fires → SubQ01 starts）
- **Sofia 3-H 茫然（傳送後）**：gate = `GetStageDone(SubQ01, 10)==1`

---

## 5. 場景 / 地點清單（Act 3）

| cell/worldspace | FormID | 屬 | 用途 |
|---|---|---|---|
| Bruiant's Estate worldspace | `Vigilant.esm:0x047CFA` (zCOBruiantWorld) | Act3 外部 | Sofia 到達宅邸前（worldspace 確認） |
| South Bruiant Mansion | `04A8B9` (zzzCONobleMansion01) | Mq01 | 3-A / 3-B / 3-C / 3-D 主要 exploration |
| North Bruiant Mansion | `04DC3F` (zzzCONobleMansion02) | Mq01 | 進階 exploration |
| Mansion Basement | `2EBC0B` (zzzCONobleMansionBasement) | Mq01/qGuide | curse 副線；3-C |
| Hidden Room / Under Mansion | `04F6C8` (zzzCOUnderMansion) | Mq01 | Julius boss 場（3-G）推測 |

**注意**：Stage gate 條件不依賴 `GetInCell`；`GetInWorldspace(Vigilant.esm:0x047CFA)` 可用於到達宅邸前的 ambient 台詞（3-A 第一條輪播），但正式 dialogue 直接用 Mq01 stage 閘即可。

---

## 6. 仍需實機確認

- Mq01 s90 與 s200 的玩家觸發機制：確認是玩家與 Molag Bal 的對話選擇（MolagB02T02/T03 gated at stage 80 → Fragment_24 starts combat / Fragment_11 opens gate），還是 MartyrTRG 觸碰。
- Baal/Balthoro alias 在 mansion 的具體 cell 出現位置，確認 3-F gate 是否需要加 `GetInCell(Mq01 mansion cells)` 輔助。
- BurntCorpse property（SubQ01 PSC）的具體 cell ref，以利 3-I 可選台詞的 `GetInCell` gate。
- Julia boss（zzzCOJuliusChildOblivion, 0x0461D8）是否就是「紅魔女」——ESM NPC 記錄 EditorID 有 "ChildOblivion" suffix，確認其戰鬥 phase 對應 Mq01 s30-s40。
