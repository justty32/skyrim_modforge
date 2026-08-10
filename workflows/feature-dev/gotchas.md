# 功能開發踩坑（feature-dev）

← [INDEX](../../INDEX.md)｜本工作流：[landed](landed/README.md) 已落地 · [session-log](session-log.md) 進度｜跨工作流共通踩坑見 [common/gotchas](../common/gotchas.md)、解碼類見 [investigation/gotchas](../investigation/gotchas.md)

開發具體功能（SM / scene / dialogue / npc / voice…）時踩到的坑，**含外部工具的內部開發聯動**（Papyrus 編譯、Wine shell-out 等——對應的使用面文檔在 [docs/asset-pipelines](../idea/asset-pipelines/README.md)）。`[[...]]` 連 Claude memory。

---

## 外部工具（Papyrus / Wine）

- **Papyrus 編譯**：`Papyrus.Compile`（Wine+CK）用 cache 全 source（`~/.cache/modforge/papyrus/Source/Scripts`）；native `~/tools/papyrus-compiler` 用 loose Source，headers 不全設 `MODFORGE_PAPYRUS_HEADERS` 指向 cache（`extends ReferenceAlias` 必設）。dispatcher/controller `.psc` embed 進 CLI、Package 編 user script 時解到 temp 當 sibling header → `Fire()` 免 per-machine cache。
- **Wine 工具吃 Windows path**：`xWMAEncode.exe` / `LipGenerator.exe` / `FaceFXWrapper.exe` 在 Wine 下要 `Z:\...` 路徑；C# shell-out 前用 `winepath -w`（`LipGenerator` 的 `<wav>` 與 `-OutputFileName:<lip>` 兩個路徑都要轉）。直接傳 Unix `/tmp/...wav` 會讓 xWMAEncode 報「Must specify input and output filenames」並導致 voice pipeline 降級成 loose `.wav`。

## Story Manager

- **SM 結構** [[story-manager-kill-recipe]]：一事件根→一條共用分支→多 quest node（串 PreviousSibling）；事件根下多分支互斥；**引擎一事件只啟動一個最先符合的 quest**（正確 radiant，非 bug）；ESL 能裝 SM；`SimpleActor`（雞/兔）不發 Kill 事件。
- **SM alias** [[story-manager-kill-recipe]]：① location 槽 alias 必須 `Type=Location`（fromEvent 'L' 自動）；② 任一必填 alias 填不上 → quest 靜默不啟動；③ 殺/指向被 `ReservesLocationOrReference` 保留的 NPC 需 `allowReserved`（uniqueActor 強制）；④ `QuestAlias.Flags` nullable，旗標用 `GetValueOrDefault()` 起底。
- **SM 事件可靠性** [[dispatcher-magic-trigger]]：additive 無條件分支只在 vanilla 少/沒密集處理的事件上可靠；密集事件（ActorDialogue/Hello）會輸掉互斥競爭、劫持原版對話——須用 conditions（或走自訂 ScriptEvent keyword）。
## Scene（autoStart / 動作 / PlayIdle）

- **autoStart scene 閘門**：用 `autoStart.gateGlobal`（controller 端檢查），**不要**用 scene-level `conditions`——controller 強制 `Scene.Start()`，繞過 scene begin-conditions（後者只 gate `beginOnQuestStart` scene）。
- **scene 動作**：`SceneAction.TypeEnum` 只有 Dialog/Package/Timer——「走位/坐/活化」走 Package action 引用 PACK；**「播動畫」走 `SceneActionSpec.Idle`（SceneAdapter phase fragment，非 SceneAction）**。
- **scene PlayIdle**（in-game 2026-06-07 確認，多坑連環）[[scene-playidle-recipe]]：① **SceneAdapter VMAD 三個 canonical 值不可少,否則引擎靜默跳過 fragment**——`ScenePhaseFragment.Unknown=16777216`(0x01000000;=quest 的 `Unknown2=1` 坑的 scene 版)、`SceneScriptFragments.ExtraBindDataVersion=2`、`ScriptEntry.Flags=Local`(全 265 vanilla phase-frag scene 一致)。② **每個帶 fragment 的 phase 必須有一個 SceneAction(Timer)**,空 phase 引擎不 run、fragment 不 fire(故 idle action 同時發一個 Timer 當 hold)。③ **不是每個 IDLE 都能 PlayIdle**:跪/祈禱(`IdleBlessingKneel*`/`IdleCrouchedPray*`)綁神壇家具,自由 `PlayIdle` 無效;挑 vanilla 腳本實際 `.PlayIdle()` 過的(鞠躬 `IdleSilentBow`/獻手 `IdleGive`/`IdleStop`/offset 類),`grep -ri '.PlayIdle(' ~/.cache/modforge/papyrus/Source/Scripts` 查。④ 連播同一 idle 不明顯重播,要不同手勢才看得出兩 fragment 都 fire。⑤ 座椅/sandbox NPC 忽略 PlayIdle → 給站立包(Sandbox `allowSitting:false`)。⑥ console `playidle` 吃 EditorID 不吃 FormID(Papyrus `PlayIdle(form)` 吃 form,spec `idle` ref 綁的就是 form)。
## NPC 裝備 / 偷竊

- **NPC 裝備/偷竊**：武器要有傷害（templated 武器 spec 留空會保留 template 原值；0 傷害武器 NPC 評分低於拳頭、不拔）；未裝備物品免 perk 偷，已裝備武器/穿戴衣物需 Misdirection/Perfect Touch perk；`essential` NPC 不可 loot，要可 loot 改用 `protected`。
## Voice assets

- **Voice assets 不是 plugin record**：Skyrim voice 檔必須在 loose path `Sound/Voice/<PluginName.esp>/<VoiceType>/<CK-name>.fuz|wav|xwm`，ESP/ESM 只提供 INFO FormID/Quest/Topic 查找依據；`package` 只會複製 `--assets`/`spec.assets` 的 `Sound/`，不會自動抓另一個 build 目錄旁邊的 voice output。產 voice 的最穩流程是「先 package 到最終資料夾，再對該資料夾內 plugin 跑 `voicelines`」，或「build+voicelines 到 staging dir → package --assets staging dir」。

## Voice / ship-voice

- **TIF 內聯編譯的「spurious fail」其實是 native headers 不全** [[sofia-vigilant-pipeline-confirmed]]：Linux native compiler 預設看的 Steam `Data/Scripts/Source` 可能缺 `GlobalVariable.psc` 等 header，故含 `setGlobal`/`sayOnce` 的 fragment 在 `package` 失敗，手動 `compile`（Wine＋完整 cache）卻成功。2026-08-10 已修 `Papyrus.CompileBest`：native 失敗會自動 fallback CK/Wine，並保留兩邊錯誤；正常 `package` 應直接帶齊 TIF `.pex`。離線機沒有 Wine 時仍須把 `MODFORGE_PAPYRUS_HEADERS` 指向完整 header 目錄。
- **LipGenerator wine crash** [[voice-gen-interface-future]]：lip 嘴型設 `MODFORGE_LIPGEN`＝CK `LipGenerator.exe`；但它在 wine 下會 crash/重試把配音拖到極慢，量大時可先**不設＝跳過**（嘴不動），之後再統一補 lip。

## adapter / quest stages

- **adapter 合併**：`WireQuestStages` 要**合併**進既有 `QuestAdapter`（不能 `=` 覆寫，否則清掉 alias 腳本的 `.Aliases`）；`GetOwningQuest()` 在執行時 alias OnActivate 可用，dialogue TIF 在 game-load 是 None。
