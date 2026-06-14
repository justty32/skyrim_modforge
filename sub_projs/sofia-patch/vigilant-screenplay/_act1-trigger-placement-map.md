# Act 1 — Sofia 評論觸發點 / 場景 / 地點 放置地圖

> 目的：確定每一條 Sofia 評論都掛在**對的 VIGILANT stage / 場景 / 地點**上（情境正確）。
> 來源：BSA 明文 QF_/SF_/TIF_ 碎片逆向（2026-06-14，6 個 sonnet agent 平行解碼）。各任務細節見同夾 `_triggers/agent*.md`。
> ESM（英文）：`~/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`；BSA 碎片 cache：`vigilant-reconstruction-redo/_bsa-psc-cache/`。
> VIGILANT 第一幕官方名稱＝**Act of Magnificence**（`zzzAoM*`）：hub `Mq00` → 分支 `Mq01–10` → 結局 `MqGoodEnd`(mercy) / `Mq06BadEnd`(tyranny)；角色支線 `SubQ01–03`。

## 1. 評論放置總表（已解碼，取代先前 provisional 猜測）

| beat | 機制 | 正確 gate（除 `GetIsID Sofia` 外） | 依據（碎片） | 信心 |
|---|---|---|---|---|
| **1-A 入會** | 玩家可問 | `GetStageDone(Mq00 0x005CE2, 10)` | s10 Fragment_3＝`AddToFaction(Vigilant)+AddSpell+護符`；s5 無 fragment（原 gate 會啞火） | 高 |
| **1-B 學法術** | 玩家可問 | `GetStageDone(Mq00, 10)` + 跟隨 faction | 無真正「教法術」事件，s10 是入會後最早 hook（主題為原創） | 高(stage)/中(主題) |
| **1-C 首任務後** | 玩家可問 | `GetStageDone(Mq01 0x005CE3, 50)` | Mq00 在 s40 就 `Stop()`；Mq01 s50＝首次吸血鬼狩獵完成 | 高 |
| **1-D 貓人夢（出夢後）** | 玩家可問 | `GetStageDone(Mq06 0x009E68, 90)` | s90 有 CompleteQuest flag＋「Jo'vanni thank you」 | 高 |
| **1-E 信標屠殺** | 玩家可問(Sad) | `GetStageDone(Mq07 0x00A3FE, 20)` | s20＝抵達信標大廳、Jacob「全死了，只剩我」 | 高 |
| **1-F 信標 boss 後** | 玩家可問 | `GetStageDone(Mq07, 50)` | s40＝開打、s50＝Bal 真死（Obj40 完成、Scene03 和解） | 高 |
| **1-G 女巫疑慮** | 玩家可問 | `GetQuestRunning(SubQ01 0x17576E)` + `s10==1` + `s20==0` | 遭遇後、宣戰前的窗口 | 高 |
| **1-H 殺女巫** | 玩家可問(Anger) | `GetStageDone(SubQ01, 50)` | Fragment_36＝`Karma.Mod(-6)`＋殺光受害者；s20 只是開打 | 極高 |
| **1-H 放女巫** | 玩家可問(Happy) | `GetStageDone(SubQ01, 230)` | Fragment_38＝`Karma.Mod(+2)+Stop()`；（虔誠線另有 s300 `+Pious`） | 極高 |
| **1-I 疑 Altano** | 玩家可問 | `GetStageDone(Mq08 0x00EA8A, 200)` | s200＝Altano 狂熱顯露「殺！無論如何」；Altano＝叛徒 `zzzAoMVigilantTraitor 0x000D62`，reveal 在 s300 | 高 |
| **1-J 殺 Carene** | 玩家可問 | `GetStageDone(MqGoodEnd 0x4D0376, 35)` | Fragment_11「35 Carene Dead→Bad End」；s29 只是變可殺 | 高 |
| **1-J 放 Carene** | 玩家可問(Happy) | `GetStageDone(MqGoodEnd, 100)` | Fragment_13：`GetStageDone(35)==false → SetStage(100)`＝good 分支 | 高 |
| **1-K 章節收束** | 玩家可問 | `GetStageDone(MqGoodEnd, 130)` | s130＝`qAct2.Start()`＋GoodEnd Stop＝真正 Act1→2 交接 | 高 |
| **Hag's Pond 地點** | 玩家可問 | `GetInWorldspace(0x166857)` + `GetQuestRunning(SubQ01)` | zAoMWitchWorld 屬 SubQ01 | 高 |

每條皆 `sayOnce` + 各自 GLOB once-flag；殺/放互斥另加對方 GLOB==0。

## 2. 角色 / 名稱校正
- **Altano**（非 Artano）＝警戒者任務發派人，`zzzAoMVigilantTraitor 0x000D62`，**就是叛徒**，也是最終 boss `zzzAoMBossAltanoInner 0x4D0374`。→ Sofia 1-I 的疑心是正確伏筆。台詞已全改 Altano。
- **Carene**＝`zzzAoMm08Mother 0x0012D0`（Mq08「No Mercy」的母親）；Act 4 另有 `zzzCHCareneDead`＝殺/放有後續。

## 3. 場景清單（Act 1，scnscan/scenediag）
- **夢境 1 個**：`zzzAoMMq06Sc01 0x4CCD7C`（host Mq06）→ 夢 cell **`0x00185C zzzAoMKahjiitDreamLand "Jo'vanni Dream Theater"`**。
- **動作型（Package/Timer）6 個 + 混合 8 個 + 純對話 4 個**。Beacon 三場（Jacob 獨白 / 7-phase 行列 / Jacob&Bal 和解）皆在 basement `0x00185B`。女巫 SQsc01–04 在 House of Pond `0x16E303`。

## 4. 地點對照（realm 評論用）
| worldspace/cell | FormID | 屬 | Sofia 用途 |
|---|---|---|---|
| Hag's Pond (WRLD) | `0x166857` zAoMWitchWorld | SubQ01 女巫 | ✅ Act 1 地點評論 |
| House of Pond (CELL) | `0x16E303` | SubQ01 + Mq08 briefing | 女巫對峙場景所在 |
| Stendarr's Beacon Basement | `0x00185B` zzzAoMBeaconBasement | Mq07 | 1-F boss 所在 |
| Jo'vanni Dream Theater | `0x00185C` | Mq06 夢 | 夢境入場目標 |
| ~~Bruiant's Estate~~ | `0x047CFA` zCOBruiantWorld | **第三幕 zzzCOMq01** | ❌ 已移出 Act 1 |

## 5. 機制 hook（下一步實作，位置已確定）
- **進入夢境（1-D，幻影掛件）**：在 `GetStageDone(Mq06,25)==1`（玩家被打入夢、14 秒淡入窗口）時把 Sofia `MoveTo(dreammarker 0x00A3CC)`（夢 cell `0x00185C` 內，位置約 895/-47.8/-424）；`GetStageDone(Mq06,50)==1`（出夢）時移回玩家。夢無 SCEN，只有對話＋AI package，故 Sofia 進去只需 MoveTo + 一個 controller script（或 magic-effect 觸發）。夢中 Sofia 評論可掛 `GetInCell(0x00185C)`。
- **作動作（PlayIdle）**：可仿 Beacon 7-phase 行列場景，給 Sofia 一段 SCEN phase idle（ModForge `scene` + `idle` action）。最易落點＝1-K 休息（坐下/伸懶腰）或 1-E 默哀。
- 兩者都需 controller quest + 一支 Papyrus；位置已定，待你看過 Act 1 對話觸發正確後再實作。

## 6. 仍需實機確認
- stage 語意雖由碎片高信心解出，但**好/壞分支實際 SetStage 時機**仍建議實測一輪（尤其 1-H 殺=s50/放=s230、1-J 殺=s35/放=s100）。
- 1-I 用 Mq08 s200——需確認玩家此時通常已招募 Sofia 且在場。
