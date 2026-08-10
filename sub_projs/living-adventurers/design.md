# living-adventurers — 怎麼做（設計文件）

← [README](README.md)｜idea [#23](../../workflows/idea/living-adventurers.md)。本檔把 idea 變成**可動工的工程設計 + 建造順序**，全部踩在 ModForge 既有機制上（已 code-pass 驗證，見文末「機制依據」）。

## 0. 一句話架構

**一個 pass-0 macro（`livingNpcs:`）把每個活 NPC 的極小輸入展開成低階記錄 + 一個可重用控制器 .pex（`MFLivingNpc*`）。** 機制＝`settlements:` 的「pass-0 展開 + deferred 錨點」＋ `skillTrees:` 的「每單元 attach 可重用 .pex + 綁 property」的合體。

兩層引擎（idea #23 的鐵律）落到具體機制：
- **抽象幽靈模擬**＝控制器在 game-time tick 推進 per-NPC 的 StorageUtil 狀態（無 actor 處理）。
- **就地實體化**＝real-time 在場 poll，把 NPC 的 persistent ref `MoveTo` 到對應錨點 marker，並 `EvaluatePackage()` 讓該錨點的 AI package 生效。

## 1. 控制器設計（named 層）

**選定形態：一個 host 控制器 quest（startGameEnabled）+ N 個 reference alias，每 alias 一個活 NPC。** 理由：alias 是 per-NPC 狀態的乾淨家，`GetReference()` 取得 actor（對 in-spec npc 與外部 follower ref 都成立），AI package 可經 ALPS 掛在 alias 上（memory `radiant-alias-package-byte-truths`，已修的 real gap）。

**效能取捨**：**poll 與 tick 放在 quest 腳本層、一次迴圈掃所有 filled alias**，而非每個 alias 各自 `RegisterForSingleUpdate`——N 個 NPC 只有一個 real-time poll + 一個 game-time tick，存檔/CPU 不隨 N 線性膨脹。alias 腳本只持有狀態與 props。

```
MFLivingWorldController extends Quest          ; 一個 host quest
  FormList Roster                              ; 所有活 NPC 的 alias 索引（或直接迭代 alias 0..N）
  Float SimIntervalHours, PollInterval
  OnInit:        RegisterForSingleUpdateGameTime(SimIntervalHours); RegisterForSingleUpdate(PollInterval)
  OnUpdateGameTime: for each alias → AdvanceSim(alias); re-register
  OnUpdate:         for each alias → Presence(alias); re-register

  ; per-alias 狀態全在 StorageUtil，keyed on 該 NPC 的 ActorRef：
  ;   _LA_deeds (int)  _LA_task (int enum)  _LA_deadline (float)
  ;   _LA_anchorIdx (int)  _LA_playerRel (int)  _LA_alignment (int)
```

每個 alias 的 archetype / 錨點 / rumor GLOB 用 **alias 的 scriptProperties** 綁（`MFLivingNpcAlias extends ReferenceAlias`，持有 `Int Archetype`、`FormList Anchors`、`ObjectReference HoldMarker`、`GlobalVariable DeedCount`）。controller 透過 alias 腳本讀這些。

**archetype = 控制器內的有限分支（switch on Int Archetype）。** 這是關鍵分工：
- **加一個既有 archetype 的新 NPC ＝純資料**（macro 多展開一組 alias+marker，零程式）——這就是「快速 enroll」要的。
- **加一種全新生活型態 ＝擴控制器**（多一個 archetype 分支，偶爾為之）。

## 2. enroll spec（產品核心，最小輸入）

```jsonc
"livingNpcs": {
  "simIntervalHours": 4,
  "npcs": [
    {
      "ref": "SomeFollower.esp:0x000D62",   // 外部 follower 的 ActorRef；或 in-spec npc editorId
      "name": "Kjeld the Wanderer",
      "archetype": "adventurer",            // adventurer|merchant|herbalist|priest|bandit|mageApprentice
      "alignment": "friendly",              // friendly|neutral|hostile
      "tier": 2,
      "backstory": "一個逃離戰爭的傭兵……",   // 驅動 rumor/對話；可 AI 起草
      "anchors": [                          // 他可能現身的地方（vanilla cells）
        { "cell": "Skyrim.esm:0x0133C6", "pos": {"x":-300,"y":250,"z":0}, "kind": "inn" },
        { "cell": "Skyrim.esm:0x...",     "pos": {...}, "kind": "jarlHall" }
      ],
      "rumors": ["聽說 Kjeld 一個人清了……"],  // 傳唱台詞；可 AI 批量生成（#17）
      "interactions": { "hire": true, "parley": false }
    }
  ]
}
```

**macro `ExpandLivingNpcs`（pass-0，排在 `ExpandSettlements` 後）每個 NPC 展開成**：
| 產出 | 用途 | 既有機制 |
|---|---|---|
| host 控制器 quest（全模組共一個）+ 一個 reference alias | per-NPC 狀態家；fill = `uniqueActor:<ref>`（外部 follower）或 in-spec npc | alias fill / ALPS |
| HoldMarker xmarker（off-stage）+ in-spec npc 的 ACHR placement | 冷凍位；外部 ref 不放置（用既有 ref，靠 MoveTo） | placement/xmarker，deferred-wire 自動 persistent |
| anchor xmarkers（每個 `anchors[]` 一個）+ 收進該 alias 的 `Anchors` FormList | 現身點 | FLST + placement |
| 每 archetype 的 on-stage AI package（sandbox@anchor）掛到 alias | 現身時的行為 | ALPS package（radiant package fix） |
| per-NPC deed GLOB | rumor 對話 condition `GetGlobalValue>=N` | GLOB + dialogue conditions |
| rumor dialogue（吟遊詩人/innkeeper topic，gated on deed GLOB） | 傳唱 | dialogue + conditions |
| 把 ref 加進 archetype FormList + alias 綁 `Archetype` int + scriptProperties | 控制器分支 | scriptProperties |

控制器 .pex 由 `Package.cs` **ship-gate on `spec.LivingNpcs != null`** 自動打包（同 MFSceneBanterController 的 gate 模式）。

## 3. 建造順序（每步都能獨立驗，不在未驗地基上疊）

| 階段 | 內容 | 驗證 | 卡關 |
|---|---|---|---|
| **P0 spike** | 一個硬寫 MFAdvController 證核心迴圈（已 build 過） | **主力機 package + 實機驗三件事**（離場推進/MoveTo 現身/rumor） | — |
| **P1 泛化控制器** | MFAdvController → `MFLivingNpcAlias`+`MFLivingWorldController`（archetype int、anchors FormList、StorageUtil 狀態、quest 層 poll/tick）。手寫 spec 掛 2 個 archetype（adventurer + mageApprentice） | 實機：兩種 NPC 各過各的生活、各自現身 | — |
| **P2 enroll macro** | `livingNpcs:` pass-0 macro + ship-gate。**「加 NPC = 幾行 JSON」成立** | 離線 build：一份 3-NPC spec 展開出正確記錄（xEdit byte 檢視） | — |
| **P3 互動 + alignment** | parley/hire/fund 對話 + playerRel KV；敵對分岔；關係記憶（放過→下次友好） | 實機：搶任務/雇用/放生強盜後再遇 | — |
| **P4 任務層（真 missive）** | 隨機地牢/採集/送信目標 | 實機 | **卡 roadmap #7–9**（LocationAlias / nested ReferenceAlias / UpdateCurrentInstanceGlobal） |
| **P5 整合** | LAL 出身 seed 關係、follower mod 共存、AI backstory 管線 | 實機 + mod-survey 查 LAL | LAL 暴露機制**待查證** |
| **P6 環境群像層** | 無名背景人口（行商隊/采藥人），量大 → pooled ref / LVLN-spawn | 實機 | 不同實體化策略 |

**先做 P0 實機**——核心迴圈沒實機驗過之前不該把 macro 疊上去。

## 4. 硬問題（要正面對付的）

1. **現身可信度**：NPC 只在一個旅館出現 ≠「到處都是」。錨點必須落在玩家真會去的 vanilla cell；少量錨點 + 野外用 spawn 補（road encounter）。P4 任務層才給真隨機地點。
2. **存檔膨脹 / tick 經濟**：named 層 N 要小；poll/tick 收斂到 quest 層單一迴圈（見 §1），不要 per-alias 各自 register。
3. **外部 follower ref 填充 + 共存**：`uniqueActor:<followerEsp>:0xNPCID`（強制 base form 會靜默失敗，memory）；**只在玩家未雇用時驅動他**。Phase-3.5 已補 `IsPlayerTeammate()` 安全閥：已招募時停止抽象 sim 與全部 `MoveTo`/`EvaluatePackage`，dismiss 後先回 hold marker 再重新納管。YUA 是 vanilla follower faction 路線，仍須實機驗證它的 teammate flag 時序。
4. **MoveTo 後行為**：MoveTo 完 `EvaluatePackage()` 讓錨點 package 立即生效（否則呆站）。
5. **rumor condition 的 per-NPC 維度**：named 層每 NPC 一個 deed GLOB（可控）；環境群像層不做個別 rumor。

## 5. 機制依據（code-pass 驗證，2026-06-27）

- **pass-0 macro 展開**：`Generator.Build.cs:24-27`（`ExpandSkillTrees`→`ExpandSettlements`，idempotent guard）；範本 `Generator.Settlements.cs:31`（resident→ACHR+package+faction+RELA）、`Spec.Settlement.cs`。
- **deferred 錨點 + 自動 persistent**：`Generator.Build.Packages.cs:60-95`（`deferredLocationWires.Add`）→ `Generator.Build.PlacementRefs.cs:120`（`WireDeferredLocations`）；`Generator.Build.Placements.cs:14-26,190-204`（錨點 editorId 在 deferred 清單→強制 persistent）。
- **可重用 .pex ship-gate**：`Package.cs:166-209`（`ShipEmbeddedPex`，per-feature gate）。
- **quest 綁控制器 + property**：`Generator.Build.Scene.cs:201-231`（`AttachSceneController`：ScriptObjectProperty/Int/Float，pass-2 `formKeyByEd` 解析）；spike 已用同法。
- **alias-script + properties**：spec `alias.script/scriptSource/scriptProperties`（schema $defs）。
- **per-ref 狀態**：StorageUtil `Generator.StorageWrites.cs:34-121`（target=ref→`SWRef_<i>` Form prop）；或 JFormDB `Generator.JContainers.cs`（per-Form KV + affinity gate）。核心用 StorageUtil（無 JContainers 依賴）。
- **dynamic spawn / AI package**：`Generator.Build.StoryManager.Encounter.cs:30-51`（MFDynamicSpawn 綁定）；`Generator.Build.Packages.Advanced.cs:7-23`（Travel/Sandbox，deferred slot）。
- **alias package（ALPS）**：radiant package（memory `radiant-alias-package-byte-truths`，已修「package 未掛 alias」gap）。

## 6. P1 實作筆記（2026-06-27，`p1/`，離線 build 綠）

寫出泛化控制器 + 手寫 2-NPC/2-archetype spec，**build 零警告**（9 record / 2 dialogue / 3 script attach / 7 placement in 2 vanilla inn）：
- `MFLivingWorldController.psc`（host quest，單一 roster 迴圈跑 game-time tick + real-time poll，`AliasCount` 屬性界定 alias 0..N）。
- `MFLivingNpcAlias.psc`（per-NPC，extends ReferenceAlias，`Archetype` int 分支 + `Markers` FLST + `DeedCount` GLOB；StorageUtil 狀態留 P3）。
- `living_adventurers_p1.json`：Kjeld（adventurer，在 Sleeping Giant Inn `0x0133C6` ↔ Bannered Mare `0x01605E` 之間輪替現身）+ Falas（mageApprentice，Bannered Mare）+ 吟遊詩人 Bjorn 兩條 per-NPC rumor。

**發現的兩個 core 缺口 → 已在 core 修掉（2026-06-27，836 測綠，2 條新回歸測 `AliasScriptObjectPropTests`）**：
1. ~~alias `scriptProperties` object prop 不能解析 placement REFR~~ **已修**：`FillProperties` 對未解析的 object prop 改 queue 到新的 `deferredScriptObjectProps`，由 `WireDeferredScriptObjectProps()`（placements 後跑）解析，比照 `deferredForcedAliases`。移除舊 `MakeObjectProp`。→ macro 可**直接傳 ObjectReference props**，不必 FLST workaround（P1 spec 仍用 FLST，無妨）。
2. ~~forced-alias 的 ACHR 不被自動 persistent~~ **已修**：`deferredForcedAliases.Select(w => w.Ref)` 併入 `Generator.Build.Placements.cs` 的 `deferredAnchorEds` → forced-alias 目標自動 persistent。→ macro **不必對 living-NPC ref 顯式標 persistent**（P1 spec 仍標，無妨）。

**狀態**：離線 build 綠；**.psc 未編譯驗證**（新 .pex 非預編，`package` 時才從 `source` 編，需主力機 Papyrus compiler）+ **未實機**。P1 驗收 = 主力機 `package` → 實機看兩 NPC 各過各生活、跨旅館現身、per-NPC rumor。
