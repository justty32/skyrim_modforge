# Roadmap — mod-survey 浮現的 record/生成缺口

← [roadmap](README.md)

> ✅ **已做 code 驗證 pass（2026-06-15）** — 原本由 survey agent 推斷的 9 條缺口已逐條核對 `src/` 實際 builder。結論:**3 條本就支援（撤銷）、2 條降級為 partial、4 條確認真缺**。下方按驗證後狀態重列；每條附 evidence（file:symbol）。機制理解的後續深挖見 [survey-backlog.md](survey-backlog.md) C 組。

## ✅ 已支援（原缺口判斷有誤，撤銷）

- **~~建立新 FLST + 填 ref~~** — 早有。`Generator.Build.Lists.cs:BuildFormLists()` 從 `spec.FormLists` 建新 FLST record；`Generator.Build.Lists.Wire.cs:WireFormLists()` 經 `Resolve(...)` 填 item，支援 in-spec editorId（自家 esp ref）與 vanilla `Plugin.esm:0xID`。
- **~~`placements[].linkedRef` 欄位（+ keyword 變體）~~** — 早有。`Generator.Build.PlacementRefs.cs:WireLinkedRefs()` 讀 `pl.LinkedRefs`、設 XLKR，且支援具名 keyword link（`link.KeywordOrReference.SetTo(...)`）。
- **~~`MagicEffectSpec` script-attach (VMAD)~~** — 用通用機制即可。`Generator.Build.Scripts.cs:AttachScripts()` 是 record-type-agnostic 的:反射任何 record 的 `VirtualMachineAdapter` 掛 `ScriptEntry`+typed property。MGEF 在 Mutagen 有可寫 VMAD，validator 無型別限制 → `scripts[]` 指向 MGEF editorId 今天就能用。**至多是文件缺口**（`MagicEffectSpec` 無專屬 script 欄位，但通用 `scripts[]` 已涵蓋）。

## ⚠️ 降級為 partial（大部分已支援，只剩窄缺口）

- **SM branch/quest-node 子樹 + keyword 路由** — `Generator.Build.StoryManager.cs:BuildStoryManager()` 已建 SMBN+SMQN、以 `PreviousSibling` 串同層 quest node、按 `root|keyword` 一分支路由（帶 `GetEventData/GetIsID Keyword` 條件）。**真缺**:只建 vanilla event root 下**單層**分支（兄弟＝quest node），不支援**任意深度/巢狀 SMBN 子樹**或非 vanilla event root。scope 收窄為「多層分支巢狀」，非「子樹生成」通稱。參 [[story-manager-kill-recipe]]。
- **alias 從 LeveledNpc(LVLN) 填** — 現有 fill 模式:`fromEvent`/`forced`/`uniqueActor`/`createObject`/`findMatching`（`StoryManagerEvents.cs`）。`createObject` 已能 `cro.Object.SetTo(objFk)` 帶 `Level=Easy`、且 base 可為 leveled actor → 用 `createObject:<LVLN>@<alias>` 已能把 leveled actor 生進 alias。**真缺**:沒有**一等的** LVLN-aware fill 模式，也無「createObject 之 ref 可為 LVLN」的 validation。scope:加 LVLN fill 模式，或把 createObject+LVLN 文件化/驗證化。

## ❌ 確認真缺（保留，scope 已校正；按價值）

1. **Perk entry-point `AddActivateChoice` / `SetText` + fragment 膠水** —（最高價值，#6 原序）`Generator.Build.Perks.cs:WirePerks()` 只 emit `PerkEntryPointModifyValue`（+ `ability`），`EntryPointTabCount` 表（`Generator.Build.Perks.EntryPoints.cs`）也沒列這兩個。Immersive Interactions 需 29× AddActivateChoice + 4× SetText。scope:新增 `entrypoint` 子類 emit `PerkEntryPointAddActivateChoice`（帶 GetIsID/keyword/FLST 條件）與 `PerkEntryPointSetText` + Perk-fragment dispatcher（VMAD `Extends Perk`、`Fragment_N`→quest-script call，仿 `Generator.Build.Scripts.cs` 既有 dialogue/scene fragment 膠水）。注意 [[perk-conditiontabcount-ctd]]。
2. **package/marker 目標指向 quest alias（alias 間接）** —（高價值）package target/location 目前只解到 placed ref 或 NearSelf（`Generator.Build.Packages.Advanced.cs` 用 `PackageTargetSpecificReference`/`PackageTargetObjectID`；`Generator.BuildContext.Utilities.cs:MakeLocationSlot()` 產 `LocationTarget{Link=IPlacedGetter}` 或 NearSelf）。無 `PackageTargetAlias`/alias-index location。radiant 演出 package 在 alias-filled actor 上必需。scope:package `target`/`location` 支援 quest alias index。
3. **navmesh-tester 動態生怪 Papyrus 模板** — `Generator.Build.Navmesh.cs` 只生靜態 flat-quad NVNM + NAVI override，無 runtime「在玩家附近找合法點生成」模板。屬 **script-template** 功能（非 record），補既有靜態 navmesh 之外的 [[programmatic-navmesh]] 預置法。
4. **程序化法術族生成器（高階）** — 無此 generator;`Generator.Build.Magic.cs` 是 1:1 spec→record。scope:在既有 builder 之上加「school × level × delivery 網格 → 對齊 MGEF+SPEL+tome」的高階層。依賴 FLST（現已確認支援）。

5. **LocType keyword 路由 + Hold 偵測 alias 的語法糖（來源：IWE + EE）** — SMQN 的 LocType keyword 條件（`LocTypeCity/Village/BanditCamp…`）+ LocationAlias 內戰歸屬偵測（`myHoldImperial/Sons/Contested`）是「地點感知遭遇」的關鍵。目前需手工填 CTDA，尚無高層 spec 語法。scope：encounter spec 支援 `locationFilter: [LocTypeBanditCamp, LocTypeTown]` + `holdDetection: true`，自動生成對應 CTDA + LocationAlias。
6. **WITimeout 冷卻模式（來源：EE）** — `EE_WITimeout`（Global）+ script 記錄上次觸發時間 + OnLocationChange 前比對時間差，防止同地點連續觸發。目前無冷卻機制。scope：encounter spec 支援 `cooldownHours: 12`，自動生成 Global + alias script 冷卻邏輯。
7. ✅ **QuestAlias `findMatchingLocation` fill 模式（LocationAlias 型）（來源：Missives）（已落地 2026-06-17）** — `QuestAliasSpec` 新增 `findMatchingLocation:<locTypeKeyword>[@<parentLocationAlias>]` fill：emit `QuestAlias.Type=Location` + `LocationAliasReference{Keyword=locType, AliasID=parent}`（建 `Generator.Build.StoryManager.cs`、驗 `Generator.Validate.StoryManager.cs`、`RadiantAliasTests`）。**Mutagen binary shape 已反射驗證**（`QuestAlias.Location=LocationAliasReference{AliasID,Keyword,RefType}`，比原推測的 CTDA 路線乾淨）。⚠ CK 語義（哪欄驅動哪個約束）待主力機 xEdit byte 比對真 Missives alias（WAIT_USER）。
8. ✅ **QuestAlias 在地點內找 ref fill 模式（來源：Missives）（已落地 2026-06-17，scope 已校正）** — **原 scope 假設用 ALNA（`FindMatchingRefNearAlias`）；反射驗證發現 ALNA `TypeEnum` 只有 `LinkedRefChild`（＝只找 editor-linked-ref 子物件，非地點內搜尋，證實既有 findMatching 註記）。** 改用 `LocationAliasReference`（其 `RefType`/LCRT 欄位對 Location 型 alias 無意義 → 證明它在 Reference 型 alias 上＝「在某 location alias 範圍內找 RefType 的 ref」）。落地 fill：`findInLocationAlias:<locationAlias>[#<refTypeLCRT>]` → `Type=Reference` + `LocationAliasReference{AliasID=locAlias, RefType=LCRT}` + conditions。Missives 的 Alias_target/Alias_chest。⚠ 同 #7，CK 語義待主力機驗。
9. **`UpdateCurrentInstanceGlobal` fragment codegen（來源：Missives）** — Missives 的 gather 型 quest 在 `StartUpStage` fragment（`Fragment_5`）呼叫 `ItemTotal.SetValue(Utility.RandomInt(min,max))` 再 `UpdateCurrentInstanceGlobal(ItemTotal)`，讓 `<Global=...Count>/<Global=...Total>` 的 objective 文字即時顯示「已蒐集/目標數」。這個 Papyrus call 把一個 GlobalVariable 綁定到「此 quest instance」而非全局，支援同一模板多次同時跑出不同數量。目前 ModForge fragment codegen（`Generator.Build.QuestStages.cs`、`Generator.SceneFragments.cs`）無此模式。scope：`QuestStageSpec` 支援 `instanceGlobals: [ItemCount, ItemTotal]`，在對應 stage fragment 末尾生成 `UpdateCurrentInstanceGlobal(X)` 呼叫；或作為 Papyrus 模板 snippet 供 radiant gather spec 使用。

> **延伸調查**：缺口 #2（alias indirection）、#3（navmesh-tester）以及 LVLN fill（partial）均由 IWE/EE encounter mod 調查交叉驗證，見 [sub_projs/mod-survey/findings/encounter-mods.md](../../sub_projs/mod-survey/findings/encounter-mods.md)。

> 校正前的原始推斷清單見 git 歷史（commit 前一版）。撤銷/降級的依據全為實際 builder symbol。
