# 原版 FormID 參考

透過 `find` / `*diag` 收集。所有 ref 均為 `Skyrim.esm:0xFORMID` — 可直接用於規格中任何 `ref` 欄位。永遠用 `find` 在您的安裝中重新確認；**絕對不要猜測 FormID**。

← 返回 [lifelike 中心](README.md)

## 程序模板（用於 `packages[].template`）

| 模板 | FormID | 插槽用途 | 適用時機 |
|---|---|---|---|
| Sandbox | `Skyrim.esm:0x01C254` | 12 | NPC 在某個位置閒逛，與傢俱/idle 標記物/其他 NPC 互動 |
| Sleep | `Skyrim.esm:0x019717` | 14 | 主動**尋找床鋪**並就寢的特化沙盒；可鎖門。透過 `packages[].sleep` 設定。睡眠時間窗口 = 套件的 `schedule`（`hour`+`durationInMinutes`）；`sleep.lockDoors` 預設為 true — 共享/旅館睡眠請設為 false |
| Travel | `Skyrim.esm:0x016FAA` | 3 | NPC 步行前往特定 REFR/空間 |
| Patrol | `Skyrim.esm:0x017723` | 6 | 警衛路線。透過 `packages[].patrol` 設定（`start` → 第一個標記物）；路線是標記物的 `linkedRefs` 鏈（m1→m2→m3→m1 循環，null 關鍵字）。標記物必須在導航網格上 |
| UseMagic | `Skyrim.esm:0x0504F5` | 11 | 有時程的非戰鬥施法（祭壇前的祭司、法師自我增益）。透過 `packages[].useMagic` 設定 |
| Follow | `Skyrim.esm:0x019B2C` | 6 | NPC 實際跟隨玩家（或另一個角色）。透過 `packages[].follow` 設定。純粹的跟隨移動層 — 可雇傭的跟隨者還需要管理任務 + 跟隨派系 + 對話 |
| Escort | `Skyrim.esm:0x023B73` | 9 | **Follow 的對偶** — NPC **帶領**被護送的目標前往目的地，若對方落後則暫停。透過 `packages[].escort` 設定。導航網格規則與 Patrol/Travel 相同；目的地標記物會自動設為持久 |
| UseWeapon | `Skyrim.esm:0x01C338` | — | 在目標處練習攻擊 — ModForge 尚未支援 |

> **沒有原版的 `UseItemAt` 模板**。「前往特定傢俱」= Sandbox + `location` ref 到傢俱 REFR + `allowSpecialFurniture: true`。

每個模板的 `Data` 插槽結構詳見 [engine-internals → PACK 插槽對應表](../engine-internals.md#pack-data-slot-maps)。

## 語音類型（用於 `voiceType`）

| EditorID | FormID |
|---|---|
| MaleNord | `Skyrim.esm:0x013AE6` |
| FemaleNord | `Skyrim.esm:0x013AE7` |
| MaleNordCommander | `Skyrim.esm:0x0E5003` |

沒有語音類型，NPC 就是靜音的 — 沒有 hello/idle 音訊，沒有字幕。

## 「市民身份」派系（用於 `crimeFaction` + `factions`）

| EditorID | FormID | 用途 |
|---|---|---|
| CrimeFactionWhiterun | `Skyrim.esm:0x0267EA` | 白奔（犯罪 + 市民身份） |
| TownWhiterunFaction | `Skyrim.esm:0x028172` | 白奔（強化） |
| PotentialFollowerFaction | `Skyrim.esm:0x05C84D` | 原版免費「跟我來」雇用對話所需（搭配盟友關係） |
| CurrentFollowerFaction | `Skyrim.esm:0x05C84E` | 「目前是我的跟隨者」派系。`SetFollower` 的別名新增它；將**跟隨者專屬對話**的條件設為 `GetInFaction CurrentFollowerFaction == 1`（背景故事、情境臺詞、閒聊）以使其只在跟隨時出現 |
| PlayerFollowerCount（全域） | `Skyrim.esm:0x0BCC98` | 原版的「跟隨者數量」GLOB。將招募臺詞的條件設為 `GetGlobalValue == 0` 以防止覆蓋單一跟隨者槽位 |
| PotentialHireling | `Skyrim.esm:0x0BCC9A` | 付費傭兵門控 — 但**僅有成員資格只能取得*拒絕*臺詞，無法取得招募臺詞**（已在 It.27 反證，詳見 gotchas #hireling-getsid）。`HirelingQuestTopic1`（0x0BCC84）中實際的招募 INFO 每個都有寫死的 `GetIsID == <特定原版傭兵>` — 自訂 NPC 全部無法通過 |
| CurrentHireling | `Skyrim.esm:0x0BD738` | 招募 INFO 需要 `GetInFaction CurrentHireling == 0`（尚未被雇用）；結果腳本在雇用時將您加入其中 |

其他地區的犯罪/城鎮派系遵循相同命名模式；`find <Skyrim.esm> CrimeFaction Faction`。

## CombatStyle 設定檔（透過 `cstydiag` 取得）

| EditorID | FormID | OffMult | DefMult | EquipMult（M/Mg/R/Sh/U/St） | Avoid | Flags | 適用於 |
|---|---|---|---|---|---|---|---|
| csVampireMagic | `Skyrim.esm:0x02DFB5` | 0.77 | 0.3 | 0.51 / 8.1 / 0.55 / 0.21 / 0.98 / 2.15 | 0.2 | Dueling | 強力法師 |
| csSoldierMagic | `Skyrim.esm:0x046B9E` | 0.5 | 0.5 | 1 / 3 / 1 / 1 / 1 / 0 | 0 | — | 戰鬥法師（平衡偏向） |
| csForswornMagic | `Skyrim.esm:0x0442CD` | 0.5 | 0.5 | 1 / 1 / 1 / 1 / 1 / 1 | 0.2 | Dueling | 平衡 — **名稱具有誤導性** |

## 裝備類型（用於 `spells[].equipType`）

| EditorID | FormID | 備註 |
|---|---|---|
| EitherHand | `Skyrim.esm:0x013F44` | 可裝備到任一手的手部法術 — **必填**，否則 NPC 無法將法術裝備到手上施放 |

（BothHands / LeftHand / RightHand 變體也存在 — `find <Skyrim.esm> hand EquipType`。）

## 放置/Travel 目的地用的標記物

| EditorID | FormID | 世界空間 | 備註 |
|---|---|---|---|
| WhiterunBanneredMare（空間） | `Skyrim.esm:0x01605E` | 室內 | `coc` 目標 |
| RiverwoodSleepingGiantInn（空間） | `Skyrim.esm:0x0133C6` | 室內 | `coc` 目標 |
| WhiterunBreezehome（空間） | `Skyrim.esm:0x0165A8` | 室內 | 室內照明的好用 `cells[].template` 來源 |
| RiverwoodInnCenterMarker | `Skyrim.esm:0x01DC0A` | 旅館內部 | 空間內 Travel 目標 |
| debugWhiterunOrigin | `Skyrim.esm:0x0567F7` | WhiterunWorld | `coc whiterun` 目標 — 在城牆內 |
| debugRiverwood | `Skyrim.esm:0x0567F6` | Tamriel | 河木村室外 |
| WhiterunStablesHorseMarker | `Skyrim.esm:0x109826` | Tamriel | 白奔主城門外 |
| Tamriel（世界空間） | `Skyrim.esm:0x00003C` | — | 室外 `placements` 的世界空間 ref |

### XMarker 基底（用於第一類暫存標記物 — Travel/Patrol/場景/`coc` 錨點）

`base` 為這些之一的 `placement` 是原版用作目的地/節點的不可見標記物。
**標記物不會吸附到地板** — 請將它們錨定在已知可行走的座標上（用 `refpos` 參考一個原版 ref，或在已導航網格化的室內），否則路徑尋找會靜默失敗。

| EditorID | FormID | 備註 |
|---|---|---|
| XMarker | `Skyrim.esm:0x00003B` | 純位置標記物（無朝向） |
| XMarkerHeading | `Skyrim.esm:0x000034` | 位置 **+ 朝向** — 常用的巡邏/Travel 節點 |

## 載入門（用於傳送配對 — 連接兩個空間）

兩個 `base` 為載入門的 `placement`，且其 `teleport` 相互指向，會形成穿行連結（XTEL）。抵達點自動設定為配對門的位置。

| EditorID | FormID | 備註 |
|---|---|---|
| FarmhouseLDoor01 | `Skyrim.esm:0x029CB0` | 木農舍載入門（Sleeping Giant Inn 使用） |
| WRShackDoor01 | `Skyrim.esm:0x024E26` | 白奔小屋載入門 |
| ImpDoorSingleLoad01MinUse | `Skyrim.esm:0x0EF53A` | 帝國單扇載入門 |
| NorDoorSmLoad01MinUse | `Skyrim.esm:0x0F1C16` | 北方小型載入門 |
| ImpWoodDoorCaveLoad01 | `Skyrim.esm:0x10C62A` | 洞穴/地城載入門 |

## 參考角色（引擎內建）

| EditorID | FormID | 備註 |
|---|---|---|
| Player（NPC 基底） | `Skyrim.esm:0x000007` | RELA 子項預設 / `GetRelationshipRank` 目標 |
| PlayerRef（已放置 ref） | `Skyrim.esm:0x000014` | 預設的 Follow/Escort 目標 |

## 測試敵人基底（用於遊戲內 `placeatme <id> 1`）

| EditorID | FormID |
|---|---|
| EncWolfIce_Indoor | `Skyrim.esm:0x10F2A3` |
| EncWolf_Indoor | `Skyrim.esm:0x10F2A2` |
| EncBandit05MagicArgonianM | `Skyrim.esm:0x0C3CA7` |

## 分級敵人生成（用於 `placements[].base` 分級角色生成 → ACHR）

`base` 必須是 **NPC_ 包裝器**（`Lvl*`），其 TEMPLATE 鏈參照 LeveledNpc 列表，可在載入時生成等級適當的角色。為原版基底加上 `"kind": "npc"`（建置工具無法在無頭模式下讀取主外掛的記錄類型）。搭配 `encounterZone` 控制等級範圍。

| EditorID | FormID | 角色 |
|---|---|---|
| LvlBanditMeleeAny | `Skyrim.esm:0x01E79C` | 通用近戰強盜 |
| LvlBanditMissileNordM | `Skyrim.esm:0x01B0D5` | 弓箭手強盜 |
| LvlBanditBossNordM | `Skyrim.esm:0x01B0E1` | 強盜首領（更強，等級縮放） |

> **CTD 警告：** 底層的 `LChar*` LVLN 列表（`LCharBanditMeleeAny` `0x03DECD`、`LCharBanditMissileNordM` `0x01A348`、`LCharBanditBossNordM` `0x01A341`）可用於*建構*分級列表，但原始 LVLN **不是**有效的 placement 基底——放置它會導致 Skyrim 在載入時崩潰。

## 遭遇區域（原版 ECZN 範例 — 用 `eczndiag <Skyrim.esm> <id>` 檢查）

| EditorID | FormID | 等級 / 旗標 |
|---|---|---|
| HelgenZone | `Skyrim.esm:0x0F94A6` | min 6 / max 0（無上限），`NeverResets` |
| BoulderfallCaveZone | `Skyrim.esm:0x0F52DB` | min 6 / max 0（無上限），無旗標 |
| NoResetZone | `Skyrim.esm:0x0F90B1` | min 1 / max 0，`NeverResets`（可重用的「不重生」區域） |

## 法術 / 魔法效果（用於 `spells` 列表和 `effects[].magicEffect`）

| EditorID | FormID | 種類 | 備註 |
|---|---|---|---|
| FlamesRightHand | `Skyrim.esm:0x0C969A` | 法術 | 新手毀滅系錐形 — 法師測試的好選擇 |
| SparksRightHand | `Skyrim.esm:0x0C96A1` | 法術 | 電系變體 |
| FireboltStormBasic | `Skyrim.esm:0x0D07CD` | 法術 | 學徒火焰投射物 |
| Candlelight | `Skyrim.esm:0x043324` | 法術 | 自我施放，可見光球 — 理想的 UseMagic 示範 |
| AlchRestoreHealth | `Skyrim.esm:0x03EB15` | MGEF | 恢復生命值參考設定（NoDuration/NoArea，baseCost 0.5，無 Recover） |
| AlchDamageHealth | `Skyrim.esm:0x03EB42` | MGEF | 傷害生命值 |
| FireDamageFFAimed75 | `Skyrim.esm:0x10F7F1` | MGEF | 自訂瞄準法術的瞄準火球設定來源 |

### 自訂瞄準/投射法術的視覺子表單

| 用途 | FormID | 類型 |
|---|---|---|
| 火球投射物（攜帶衝擊視覺效果） | `Skyrim.esm:0x10FBEA` | PROJ |
| 火球施放藝術（在手上的效果） | `Skyrim.esm:0x01B211` | ARTO |

## 複製模型用的模板（用於 `template`）

| EditorID | FormID | 複製模型用於 |
|---|---|---|
| IronSword | `Skyrim.esm:0x012EB7` | 武器 |
| Book1CheapNordsArise | `Skyrim.esm:0x0ED161` | 書籍 |
| GemRuby | `Skyrim.esm:0x063B42` | 雜項物品 |
| RestoreHealth06 | `Skyrim.esm:0x039BE5` | 藥水 |

## 靜態物件 / 光源（用於建置室內空間）

| EditorID | FormID | 用途 |
|---|---|---|
| WRShadowOmni | `Skyrim.esm:0x0C82AE` | 球形陰影主光源，半徑 512，預設開啟，**非** PortalStrict — 適合開放式室內 |
| WRInteriorLightBrite01 | `Skyrim.esm:0x06ED46` | 無陰影暖色補光 |
| DefaultSunlightHalfOmni01 | `Skyrim.esm:0x0172C4` | **避免作為唯一光源** — 半徑 256 + PortalStrict，在無入口的空間中幾乎無用 |
| WRIntFloorSTMid01Large | `Skyrim.esm:0x1044AA` | 地板磚（3×3 網格，間距 256） |
| WRIntWallStr01Low | `Skyrim.esm:0x0CB43B` | 白奔室內牆壁件 |

## 合成（用於 `recipes`）

| EditorID | FormID | 用途 |
|---|---|---|
| CraftingSmithingForge（關鍵字） | `Skyrim.esm:0x088105` | 預設工作台關鍵字（鍛造爐） — `recipes[].workbench` 預設使用此值 |
| IngotIron | `Skyrim.esm:0x05ACE4` | 合成材料 |
| LeatherStrips | `Skyrim.esm:0x0800E4` | 合成材料 |

## 裝束

| EditorID | FormID |
|---|---|
| BlacksmithOutfit01 | `Skyrim.esm:0x09D5DF` |

## 商人 / 店主（用於 `factions[].vendor` 店主）

| EditorID | FormID | 種類 | 用途 |
|---|---|---|---|
| JobMerchantFaction | `Skyrim.esm:0x051596` | FACT | 通用「我想交易」話題的條件為成員資格 — **Build 自動將其加入**任何規格中商人派系的 NPC |
| ServicesWhiterunBelethorsGoods | `Skyrim.esm:0x09CAF5` | FACT | 參考原版雜貨商人派系 — 用 `factdiag` 將您生成的 FACT 與之對比 |
| VendorItemsMisc | `Skyrim.esm:0x06CB48` | FormList | 雜貨類別列表（搭配 `notSellBuyList: true` 用於「賣所有東西」的商店） |
| VendorItemsBlacksmith | `Skyrim.esm:0x066333` | FormList | 鐵匠類別列表（武器/護甲/礦石/錠/…） |
| VendorGoldMisc | `Skyrim.esm:0x072AE7` | LVLI | 商人的金幣池 — 放一個進商人箱讓他有錢可以收購 |
| LItemMiscVendorMiscItems75 | `Skyrim.esm:0x09AF0A` | LVLI | 雜貨庫存分級列表（商店販售的物品） |
| Gold001 | `Skyrim.esm:0x00000F` | MISC | 普通金幣（若偏好固定數量而非分級金幣池可用此） |

通用交易提示（`DialogueGeneric.OfferServicesTopic` `0x07F6BB`）是原版通用對話 — 您**不需要**發出它。它會出現在任何可對話的 NPC 上，前提是該 NPC 在 `JobMerchantFaction` + 擁有商人箱的商人旗標派系中，且在派系交易時間內。
