# mod-survey 缺口 — 🏰 settlements Phase-2「build / manage / defend」

← [mod-survey-gaps](../mod-survey-gaps.md)

（Tundra Defense 深挖 2026-06-25，使用者標 ⭐ 最重要；逆向自 56 .pex string-table，每宣稱 pex-strings 標注）

[findings/tundra-defense.md](../../../../../analysis/mod-survey/findings/tundra-defense.md)＝idea #22「自建據點＋募兵＋守城」**唯一完整藍圖**。機制：建材＝Ingestible(potion)→script-MGEF"Construct X"→spawner `PlaceAtMe`→`aaaFortMainQuestScript` OnUpdate 定位狀態機；募兵＝程序化 `RemoveItem(Gold)`→`PlaceActorAtMe`→`AddToFaction`+`SetPlayerTeammate`+cap；守城＝`aaaFortPlayerQuestScript` 的 MESG 選單挑 raid type→`PlaceActorAtMe` `Raider*` base at 玩家擺的 boundary marker→OnUpdate 數 `CurrentRaiders` 到 EndRaid；持久化＝Enabled REFR 留在 cell + quest-script counter（**0 GLOB、0 JContainers、無 SM**）。

**ModForge 對 #22 裁決（每個「做不到」已 grep `src/` 驗證）**：全部**靜態零件可生成**（ALCH/MGEF-script/ACTI/FACT/LVLN/NPC/PACK/KYWD/BOOK/CONT/SHOU/WOOP/SPEL/MESG-shell），且 `scriptAttach`（`Generator.Build.Scripts.cs` `AttachScripts`→`AttachOneScript`，反射式掛任何 VMAD record，已驗）能把 Tundra 56 個 controller `.pex` 掛回去。**兩硬缺口**：① **MESG 無多按鈕選單**（同 §6）；② **執行期玩法（定位狀態機/raid OnUpdate spawner/程序化募兵/跨存檔 counter）irreducibly bespoke Papyrus**——ModForge 只能隨附+掛載手寫 controller `.pex`，無法生成該行為。

**Phase-2 新原語清單（各標 今天可生成 / 需 controller）**：
- `buildables:` — potion+MGEF-script+ACTI 三件組 **可生成**；placement 定位狀態機 **需 controller**（內建 1 個泛用 placement controller `.pex`）。
- `defense:` / `siege:` — 敵方 base/LVLN/faction/boundary marker **可生成**；wave engine **需 controller**（可能半數由既有 SM + `quest.spawn` 動態生怪管線承接，待評估）。
- `recruitment:` — 程序化 `PlaceActorAtMe`+faction 路徑 **需 controller**；對話+`SetFactionRank` 路徑 **今天可生成**（重用 vendor/dialogue + EPW4NPCs 的 SPID faction recipe）。
- `manageMenu:` — **MESG 多按鈕選單（§6）是關鍵前置、可生成**；接 fragment 分支到 build/recruit/raid 動作。
- `territory:` — boundary marker + faction enmity **可生成**（無需 XOWN）。

**建議架構**：ModForge 內建 1–2 個泛用 controller `.pex`（placement + raid），generator 生靜態三件組並 `scriptAttach` 掛上——複刻 MCM-Helper / dispatcher / PapyrusUtil 的「隨附 reusable .pex + 生成接線」先例。這是 settlements 之後最大的一塊，值得單獨開 design（先補 §6 MESG buttons 解鎖 manageMenu）。
