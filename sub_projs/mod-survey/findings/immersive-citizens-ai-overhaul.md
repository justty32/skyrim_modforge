# Immersive Citizens - AI Overhaul (ICAIO) — by Shurah

> ⚠️ 不要和 **AI Overhaul（janquadrant）** 混淆。後者已解碼於
> [`workflows/investigation/decode/ai-overhaul-decode-2026-06-13.md`](../../../workflows/investigation/decode/ai-overhaul-decode-2026-06-13.md)。
> 兩者**目標相同（讓 vanilla NPC 活起來）但機制完全不同**——本檔重點在對比這個差異。

## 分類

- 類型：**NPC AI / 日程 overhaul（framework 等級）**。改善「standard state」（沒察覺威脅時的日程 / sandbox）與「combat state」（被攻擊時的防禦 / 逃跑），不碰「alert state」。
- Plugin：有，單一大 ESP。
- 敘事價值：**低**（不講故事）；**系統 / pattern 價值：極高**——是整個 mod-survey 裡「如何替成群 NPC 排豐富日程」最完整的真實範本，**直接服務 idea #22**。

## 規模 / 關鍵記錄

主檔 `Immersive Citizens - AI Overhaul.esp`，6.5 MB，**6505 records**，`localized=False`（英文 inline）。
Masters：Skyrim + Update + Dawnguard + HearthFires + Dragonborn + ccBGSSSE001-Fish（**疊在 vanilla 上、非 USSEP**——與 janquadrant 疊 USSEP 不同）。

| 群組 | 數量 | 意義 |
|------|------|------|
| **PlacedObject (REFR)** | **3078** | 大量 NPCO 自訂 **sandbox / sleep / flee 標記 ref + 家具 ref**——package 的 location/target 槽指這些 |
| **Package** | **1497**（1404 新 + 93 override）| AI 行為包，**全部 bespoke、不是 10 個共用模板** |
| **Quest** | **376** | 46 個新 `NPCO_AI<地點>NPCs` 分派 quest + 1 TrackingSystem + Init + ~320 vanilla `Dialogue*Scene` override |
| **Scene** | **260**（2 新 + 258 override）| 微調 vanilla 環境閒聊 scene（配合新日程修條件）|
| **Cell** | **263** / **NavMesh 190** / **Landscape 15** | 大量 cell override（放標記 / 改 navmesh 讓逃跑路徑通）|
| **IdleMarker 21 / Furniture 13** | 新放置 | sandbox / 工作動畫錨點 |
| **Npc** | **6** | ⚠️ 只 6 筆（5 匹馬 + 1 衛兵），**幾乎不 override NPC 記錄** |
| GlobalShort | 35 | `NPCO_AIGlobal<Hold>` / `NPCO_WeatherFactorGlobal<Hold>`——分區開關 + 天氣因子 |
| GameSetting | 8 | `fSandboxSleepStart/Duration`、`fCombatDetectionLostTimeLimit`、`iAIMaxSocialDistanceToTriggerEvent`——全域調 sandbox / 戰鬥偵測手感 |
| Faction 32 / CombatStyle 7 / Spell 15 / FormList 198 | — | vendor 營業時段 faction、戰鬥風格、輔助 |

## 核心機制 pattern：**alias-package 分派 quest（不碰 NPC 記錄）**

這是與 janquadrant 的**根本差異**，也是本調查最重要的發現：

```
NPCO_AI<地點>NPCs  (Start-Game-Enabled QUST, flags=4096, 無 stage/objective)
  └─ Reference alias[i]  → 指向某 vanilla NPC（Optional + AllowReserved）
       └─ ALPS（alias-override packages）= 該 NPC 的整疊 bespoke 日程包
```

- 一個地點一個 quest。例：Whiterun quest 有 **117 個 alias / 111 個帶 package 疊**；Riverwood 67 alias。整個 mod ~46 個這種 quest 覆蓋全 Skyrim + Solstheim + 各陣營。
- **package 經 alias 的 ALPS 槽下發，NPC 記錄本身不動**——所以才只有 6 筆 NPC override。這正是「override 既有 vanilla NPC 的 package 清單」的**正規、低衝突替代法**（CK 的 "alias package override"）。
- alias 多為**具名 ref**（Alvor / Camilla…），少數是**條件 alias**（`RiverwoodExtraCivilian` conds=10：`HasKeyword` + `GetInFaction` + `GetIsID` 把非具名雜魚也收進來）。

### package 本身：per-NPC × per-地點 × per-時段，全手刻

命名即配方：`NPCOWhiterunBrenuinSleep1x8` = Brenuin 在 Whiterun，凌晨 **1 點睡 8 小時**（`hour=1 durationMin=480`）。`packagediag` 證實：
- `PackageTemplate -> 019717 Sleep`（vanilla 模板）
- `PackageDataLocation`（指一個 NPCO 放置的 location ref）+ `PackageDataTarget`（指特定那張床 ref）+ `Schedule(hour/duration)`。
- 一個 NPC 的 ALPS 疊 = eat / sleep / sit-工作 / travel / 多個時段 sandbox（**具體在前、broad sandbox fallback 在後**，與 ModForge 鐵律一致）。

### 防禦 / 逃跑 / 戰鬥 AI（「combat state」）

不是普通 sandbox，是專門的 **Flee-template package**（`packagediag` 看 data input 名稱很白）：
- `...DefenselessCivilian`：data 有 `"Flee From Target(s) Object List"`、`"Location 1 to Flee"`（`LocationFallback NearEditorLocation` radius 128）、`"Distance to Flee"`、`"Distance to Keep from threat"`——**手無寸鐵者往預放的安全點跑**。
- `...ArmedCivilian Combat`：同樣有逃跑槽 **＋ 自訂 `CombatStyle`**——能打的居民會還手。
- 武裝 / 平民 / hero / mage 各一套，按 NPC 戰力分。逃跑落點靠**預放 ref + 補的 navmesh**（解釋了 3078 REFR / 190 NavMesh）。

### 分區開關 + 全域手感

- `NPCO_AIGlobal<Hold>` GlobalShort（每個 hold / 陣營一顆）= 玩家可關掉某區 AI 的相容開關。
- 8 個 GameSetting override 把 sandbox 睡眠時段、戰鬥失蹤偵測時限、社交觸發距離整體調軟，是「感覺更像人」的隱形底層。
- vendor faction（`ServicesWhiterun*` 帶 `hours=6-17 radius` + `vendorLocation` 攤位 marker）= 商人只在營業時段顧攤——與 janquadrant 的 vendor 手法同源。

## 與 janquadrant AI Overhaul 的對比

| 面向 | **ICAIO（本檔）** | **AI Overhaul（janquadrant）** |
|------|------|------|
| package 下發 | **alias ALPS 分派 quest**（NPC 記錄不動，6 筆）| **直接 override NPC 記錄的 Packages 清單**（424 筆）|
| package 來源 | **1400+ bespoke**（per-NPC×地點×時段手刻）| **~10 個 vanilla 模板規模化套用** |
| 世界編輯 | 重（3078 REFR + 190 NavMesh + 263 Cell + 家具/idle 標記）| 輕（少量 cell/worldspace 放標記）|
| 防禦/逃跑 | **有，專門 Flee-package + CombatStyle 分級** | 無此重點 |
| 衝突面 | **低**（不爭 NPC 記錄；爭的是 Cell/NavMesh + vanilla Scene override）| **高**（每個被它改的 NPC 都和別的 NPC-mod 互踩）|
| 疊在 | vanilla（非 USSEP）| USSEP 之上 |

**結論差異**：janquadrant 是「換掉 NPC 的包清單」；ICAIO 是「**不碰 NPC，用一個 quest 從旁把整疊包掛上去**」。後者技術上更乾淨、衝突更低，是替**既有**世界打日程 patch 的更佳範式。

## 對 ModForge 的意義 & gap（聚焦 idea #22：替我們自己的新 NPC 排日程）

idea #22 是**新拓荒聚落的新 NPC**——我們**不需要**「override vanilla NPC」那條難路；我們從零建 NPC，可以直接把日程寫進 NPC 記錄。這讓 ICAIO 的精華對我們**幾乎全可借鏡、且更省事**。

| ICAIO 用到的能力 | ModForge 現況（見 [landed/npcs.md](../../../workflows/feature-dev/landed/npcs.md)）|
|------|------|
| 整疊 eat/sleep/sit/travel/sandbox（具體在前、sandbox fallback 在後）| ✅ 10 個 PACK 模板全有，順序已是鐵律；新 NPC 直接 `NpcSpec.Packages` |
| package 綁特定 location/target ref（床、攤位、工作台）| ✅ placements + package target/location 槽；radiant 版支援 `alias:`/`aliasLoc:`（C組 #2）|
| 時段 Schedule（`hour×duration`）| ✅ PACK 模板帶 schedule 欄位 |
| 預放 sandbox / idle 標記 + 家具 | ✅ placements（IdleMarker / Furniture base 可放）|
| vendor 營業時段 | ✅ vendor faction（`ServicesWhiterunCarlotta` 類）已可生 |
| **逃跑 / 防禦 package（Flee-template + CombatStyle）** | ⚠️ **gap**：PACK 模板清單沒有 `flee` 模板；CombatStyle 可生（`cstydiag` 同款欄位）但「平民被攻擊→往安全點跑」這條沒有現成 spec。**這是 #22「被野獸/匪徒襲擊時聚落 NPC 會逃/會還手」最值得補的一塊。** |
| **alias-package 分派 quest（替既有 NPC 掛包）** | ❌ 只在 radiant package（alias target/location）邊緣碰到；要「patch 既有 vanilla NPC」才需要——**#22 用不到**，記為他用 roadmap 點子即可 |
| 分區 GLOB 開關 + GameSetting 手感調整 | △ GLOB 可生；GameSetting override 非典型 ModForge 產出（也不是 #22 必要）|

### #22 最重要的 takeaway

我們的新聚落 NPC **走「NPC 記錄直接帶 package 疊」這條（janquadrant 式，但對象是新 NPC）最省**，不必學 ICAIO 的 alias-quest 繞道——那繞道只是為了不動 vanilla 記錄。ICAIO 真正值得抄進 ModForge 的是兩個**內容配方**：①「per-NPC × 時段 × location-ref 的日程疊」要綁到**實際放置的床/攤位/工作家具 ref**（不能只有抽象 sandbox，否則 NPC 站著發呆）；② **新增一個 `flee` PACK 模板**（Flee-template + 預放安全點 location + 可選 CombatStyle），讓聚落在受襲時有「平民逃、守衛戰」的生命感——這是把「慢活聚落」從靜態佈景變成會反應的活聚落的關鍵一步。

## Verdict：**可借鏡（內容配方）＋ 需補一個 `flee` PACK 模板**

- **可借鏡**：日程疊配方、綁實體家具 ref、vendor 營業時段、分區開關——ModForge 已有對應能力，照 ICAIO 的密度去「填內容」即可生出活聚落。
- **需補（建議 roadmap）**：`flee` PACK 模板（防禦/逃跑 AI），這是 #22 受襲反應的硬缺口。
- **可忽略**：alias-package 分派 quest 與整片 vanilla Scene/Cell/NavMesh override——那是「patch 既有 Skyrim」才需要，#22 建新世界用不到（若日後做 vanilla-NPC patch，再回頭參考此 alias-ALPS 範式，它比 janquadrant 的 NPC-override 更乾淨）。

## 其他變體（一行帶過）

- `Immersive Citizens - AI Overhaul - chinese`（.rar，1.2 MB）：中文在地化版。
- `Immersive Citizens Patch ESL`（hdd，20 KB）：把主檔 flag 成 ESL 的第三方 patch。
- 解出的 FOMOD 內另含 ELE / ELFX / Open Cities 相容 patch（純燈光/城市相容，無敘事價值）。

## 解碼方法備忘（記憶體安全）

主檔 6.5 MB，`dump` / `questdiag` / `packagediag` 全走 ModForge CLI lazy overlay，**未整載 Skyrim.esm**。
- record 普查：`dump | grep -oP '\] \K[A-Za-z]+' | sort | uniq -c`。
- 機制定位：`questdiag <AI quest>` 看 alias 的 **ALPS** 行 → 證實 package 經 alias 下發；`packagediag <pkg>` 看 `PackageDataLocation/Target` + `Schedule` + Flee data-input 名稱。
- vanilla package template editorId（如 Sleep 0x019717）只標已知值，未為解 editorId 去載 master。
