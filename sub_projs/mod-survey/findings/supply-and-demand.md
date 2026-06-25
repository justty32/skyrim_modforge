# Supply and Demand (Nexus 32365 v1.1) — mod-survey finding

Researched 2026-06-25。**全部 grounded 在本地實檔**：archive `~/skyrim_mods/hdd/Supply and Demand-32365-1-1-1590816246.zip`，解壓到 `~/skyrim_mods/unzip/SupplyAndDemand/`。plugin 用 ModForge CLI `dump` 看過，5 個 `.pex` 用 `strings` 抽過（**未隨檔附 `.psc` 原始碼**，且本機無 Papyrus decompiler，故所有 script 內部行為以 `strings` 出的 function 名 / property 名 / 字串常數為據，無法逐行確認 → 凡 script 細節皆標 *inference*）。

作者 `dylan`（pex header 殘留 `DESKTOP-BOV3JDJ / dylan`）。

---

## 1. Classification（類型）

- **類型**：dynamic economy / 物價系統 mod（**transaction-tracking Papyrus controller**，非靜態 record overhaul）。
- **plugin**：`Supply and Demand.esp`（52 KB，**非 ESL-flagged**；master = `Skyrim.esm`, `Update.esm`），**僅 12 筆 record**。
- **SKSE 依賴**：**SkyUI + SKSE**（MCM 走經典 `SKI_ConfigBase`，見 §3/§5）。**無 SKSE DLL plugin、無 SPID/KID/FLM ini、無 MCM-Helper json、無 PapyrusUtil/JContainers 依賴**（解壓目錄只有 `.esp` + `Scripts/*.pex`，全程已 `find` 確認無 `.dll`/`.ini`/`.json`/`.psc`）。
- **系統價值**：**高**。這是 survey 裡第一個確認的「**真・動態物價引擎**」——它在執行期實際改寫物品的 gold value，跟 Trade & Barter 那種「靜態 conditioned-perk 物價修正」是**根本不同的 lever**，對 ModForge roadmap 的「transaction-tracking controller pattern」缺口是直接證據。

## 2. What it does

把 Skyrim 商人經濟變成**供需驅動**：玩家**大量買進**某物 → 該物市場價**因 demand 上升**；**大量賣出** → 價格**因 supply 下降**；隨遊戲時間流逝，市場價**逐日回歸正常**（"As time passes, market values shift back to normal."）。玩家會收到通知（"The market price of … increased/decreased by … due to demand/supply."、"Supply and Demand begins!"）。pex 字串實證的 feature 點：

- 物品 gold value 被**實際讀寫**（`GetGoldValue` / `SetGoldValue`、`GetValue`，配 `Modifier` / `NewValue`）。
- **30 組值陣列**：`ValuesArray1…ValuesArray30` 與 `OriginalValuesArray1…30`——記住每個追蹤類別的**目前值**與**原始值**，回歸 baseline 用（*inference*：30 個物品分類 bucket）。
- **每日回歸**由 MCM 的 "Daily Extinction Ratio" 控制（GLOB `tc_Global_ExtinctionRatio`）。
- 物價對**地點類型**敏感：`LocTypeSettlement` / `LocSetCave` / `LocSetCaveIce` / `LocSetDwarvenRuin` / `LocSetNordicRuin` / `LocSetMilitaryCamp` / `LocSetMilitaryFort`（pex 內 location keyword 變數，*inference*：不同地點 / 容器歸不同市場）。
- 增減量「Increase the item's value by either a percentage, or increase by 1」、四捨五入分歧（"Split for rounding up on decreases or rounding down on increases"）。

## 3. Mechanism（核心：STATIC vs DYNAMIC — 這是關鍵軸）

**結論：DYNAMIC transaction-tracking Papyrus controller。完全沒有 GMST、沒有 ModBuyPrices/ModSellPrices perk、沒有 VendorValues／merchant-gold record 編輯。** `dump` 出的 12 筆 record 全在下面，沒有任何 perk / GMST / Faction / LVLI override：

實際 record（ModForge `dump` 實看）：

- **GLOB** ×2：`tc_Global_ExtinctionRatio`（Float）、`tc_Global_HideNotifications`（Short）。
- **MGEF** ×3：
  - `tc_MonitorEffect`（archetype=**Script**, ConstantEffect/Self）→ 掛 `tc_PlayerScript`（3 props）。
  - `tc_ApplyingEffect`（archetype=**Script**, Concentration/Aimed）→ 掛 `tc_AttachScript`(1) + `tc_PlayerScript`(2)。
  - `tc_CloakEffect`（archetype=**Cloak**, ConstantEffect/Self, assoc=`tc_ApplyingSpell`）。
- **SPEL** ×4：`tc_CloakSpell`（Ability，效果=CloakEffect）、`tc_MonitorAbility`（Ability，效果=MonitorEffect）、`tc_ApplyingSpell`（Spell，Concentration/Aimed）、（以上三者 equip=`013F44:Skyrim.esm` VoiceEquip）。
- **QUST** ×2：`tc_SupplyDemandMCM`（掛 `tc_SupplyDemandMCM` 3 props，flags=17 = StartGameEnabled+RunOnce 類）、`tc_SupplyDemandQuest`（flags=273）。
- **vanilla override** ×2：`WhiterunBanneredMare` CELL + `WhiterunBanneredMareChestRef`（容器 ref）——*inference*：拿一個 vanilla 商人容器當測試/掛載錨點。

**運作鏈（pex `strings` 實證 + inference）：**

1. **player 掛載**：`tc_CloakSpell`（Ability）→ `tc_CloakEffect`（Cloak archetype）→ cloak 命中目標時 `tc_AttachScript`(內含 `AddSpell` / `GotoState`) 把能力掛上去（"Attaches an ability to the player"）。`tc_PlayerScriptAddSpell` 也有 `CloakAbility` / `mymod_CloakEffectOn` GLOB-gate + `PlayerRef` ReferenceAlias + `OnInit`/`OnUpdate`——*inference*：開局把系統 bootstrap 到 player。
2. **交易偵測 = 真動態核心**：`tc_PlayerScript`（68 KB，最大 script）有 **`OnItemAdded` / `OnItemRemoved`**，配 `akSourceContainer` / `akDestContainer` / `aContainer`——監聽物品在容器/商人/玩家之間移動，即「買/賣」事件。
3. **改價**：偵測到交易 → `GetGoldValue` 讀目前市價，依 `Modifier` 算 `NewValue`，`SetGoldValue` **寫回該物的 base value**（連帶 `HasKeyword`/`GetCurrentLocation`/`GetFactionOwner`/`GetInheritedOwner`/`GetParentCell` 判斷是哪個市場/分類）。`ValuesArray*` 存目前值、`OriginalValuesArray*` 存原始值。
4. **回歸**：`tc_MonitorScript`（`OnUpdate` / `RegisterForSingleUpdate` / `UnregisterForUpdate` / `GetCurrentGameTime`）週期把市價依 `tc_Global_ExtinctionRatio` 往 `OriginalValuesArray*` 拉回。

**Papyrus（共 5 個 controller script，全 hand-authored，無 generated fragment 跡象）**：`tc_PlayerScript`（主控/交易偵測/改價）、`tc_MonitorScript`（時間回歸 OnUpdate）、`tc_AttachScript`（cloak→AddSpell 掛載）、`tc_PlayerScriptAddSpell`（bootstrap）、`tc_SupplyDemandMCM`（MCM）。**這就是 Trade & Barter 那條 finding 推測「動態供需幾乎一定要 script 追蹤交易」的活證據**。

**MCM**：`tc_SupplyDemandMCM.pex` 是經典 **`SKI_ConfigBase`**（`AddSliderOption`/`AddToggleOption`/`OnConfigInit`/`OnPageReset`/`OnOptionSliderAccept`…），**不是 MCM-Helper**。只配 **兩個選項**：① **"Daily Extinction Ratio"** slider → `tc_Global_ExtinctionRatio`；② **"Hide Notifications"** toggle → `tc_Global_HideNotifications`。MCM 寫值 → GLOB → script 讀（典型 MCM→GLOB→runtime 連線）。

## 4. vs Trade & Barter（同一目標，相反的 lever）

兩者都想「讓商人物價更有層次」，但**機制完全相反**：

- **Trade & Barter**：**STATIC**——一堆 conditioned `ModBuyPrices`/`ModSellPrices` **EntryPoint perks**（faction/location/skill/race 為 CTDA gate），唯一 script 是 MCM。價格修正是**規則化、條件式、不隨遊玩改變**；近乎純 record overhaul，可被 ModForge **今天就生成**。
- **Supply and Demand**：**DYNAMIC**——**零 perk、零 GMST**，靠 Papyrus 監聽 `OnItemAdded`/`OnItemRemoved` 後 `SetGoldValue` **改寫物品真實 base value**，再隨時間回歸。價格**隨玩家行為演化**。這是 ModForge **生不出邏輯本體**的東西（見 §5）。

一句話：T&B 改的是「barter 公式的係數」，S&D 改的是「物品本身值多少錢」且會浮動。

## 5. ModForge relevance（逐塊對應，"做不到"必 grep 驗證）

把 S&D 拆成「scaffold」與「邏輯本體」兩半看 ModForge：

**ModForge 今天就能生成的 scaffold（已驗證有對應 spec）**：

- ✅ **GLOB**：`Spec.Globals.cs` + `Generator.Build.Globals.cs` 存在 → `tc_Global_ExtinctionRatio`/`tc_Global_HideNotifications` 兩個 GLOB 可生成。
- ✅ **MGEF / SPEL（含 Script & Cloak archetype）**：`Spec.Magic.cs`/`Generator.Build.Magic.cs` 存在。
- ✅ **script-attach 到 MGEF 的 VMAD（含 typed properties + 自帶 `.psc`）**：`Spec.Magic.cs` L59-63 有 `List<ScriptAttachSpec> Scripts`（"Inline Papyrus script attach (I組 DX)"），`ScriptAttachSpec`（`Spec.Dialogue.cs` L227）有 `ScriptName`、**`Source`（指向 `.psc`，由 `package` compile）**、`List<PropertySpec> Properties`。→ **S&D 那種「script-bearing MGEF + props」結構可表達**，且 ModForge 可把使用者自寫的 `.psc` 編譯進來（`Papyrus.cs` 有 CK-Wine 與 native 兩條編譯路徑）。
- ✅ **MCM**：`Spec.Mcm.cs`/`McmGen.cs`/`Generator.Build.Mcm.cs` 存在。**但 ModForge 走 MCM-Helper（config.json + 生成 QUST/alias，見 MEMORY recipe）**，S&D 走 hand-scripted `SKI_ConfigBase`——**不同 MCM tech**，功能上都能「ship 一個有 slider/toggle 的設定選單」。
- ✅ **vanilla CELL / 容器 ref override**：ModForge 有 worldspace/cell override 能力（見 MEMORY）。

**ModForge 生不出來的邏輯本體**：

- ❌ **動態交易偵測 + 改價邏輯本身（`OnItemAdded`/`OnItemRemoved` + `SetGoldValue` + 30-array 回歸）是 hand-authored Papyrus**。ModForge **只生成自家 fragment**（quest/scene/dialogue/perk adapter），**不會替你寫 `tc_PlayerScript` 這種 controller 的演算法**。要重製 S&D，這顆 controller 必須**人工撰寫 `.psc`**，再用 `ScriptAttachSpec.Source` 掛上去由 ModForge 編譯打包——**ModForge 是 packager，不是邏輯 author**。（這不是 bug，是設計邊界。）
- ❌ **GMST editing：確認缺席**。`grep -rilE "gamesetting|gmst" src/ModForge.Core/` → **空**（與 `trade-and-barter.md` 記的 gap 一致）。S&D **本身不需要 GMST**（它不碰 barter 公式），所以這對「重製 S&D」**不構成阻礙**；但 GMST gap 仍是 economy 類普遍缺口（見 roadmap）。
- ⚠️ **MCM toggle/slider → GLOB → runtime 的端到端連線**：S&D 正是這個 pattern（MCM 寫 GLOB，script 讀）。ModForge 是否能把生成的 MCM 選項**綁到一個生成的 GLOB**，仍 **UNVERIFIED**（與 T&B finding 同一個待確認項，需對 `Generator.Build.Mcm.cs` ↔ globals 做一次 code pass）。

**結論**：S&D 的**外殼**（GLOB+MGEF+SPEL+script-attach+MCM+cell override）ModForge **今天可生成**；S&D 的**靈魂**（動態供需 controller）**必須人工寫 Papyrus**，ModForge 負責編譯與打包。

## 6. Roadmap implications（對接 `workflows/roadmap/mod-survey-gaps.md` 的 economy 缺口）

1. **新確認 pattern：「transaction-tracking controller」是真實存在且生不出邏輯的類別。** S&D 是 survey 第一個實證——roadmap 該把它記成「ModForge 提供 **scaffold + `.psc` 編譯/打包**，controller 邏輯交給使用者手寫 `.psc` + `ScriptAttachSpec.Source`」這條已支援路徑，而**不是**期待 ModForge 生成動態經濟演算法。重點是：**驗證 `ScriptAttachSpec.Source` 的 end-to-end（自帶 `.psc` → compile → 進 VMAD → in-game）真的通**，並補一個 example spec 示範「掛一顆自寫 controller 到 script-MGEF」。
2. **GMST editing gap：再次確認缺席**（與 T&B 同）。S&D **不需要**它，但它仍是 economy/balance 通用 primitive；維持 roadmap 既有的 `gameSettings:`/`gmst:` block 提案，優先級不因 S&D 改變。
3. **MCM→GLOB→runtime 連線：補強同一缺口。** T&B（perk-condition 讀 GLOB）與 S&D（script 讀 GLOB）都靠這條。**這是兩個 economy mod 的共同 enabler**——值得優先 close：確認/實作「生成的 MCM option 綁定生成的 GLOBAL，並讓 fragment/attached-script 讀得到」。
4. **可重製性定位**：T&B「ModForge 今天能生成大部分」；**S&D「ModForge 能生成全部 record scaffold，但 controller 要人工 `.psc`」**。把這組對比寫進 economy batch index，作為「靜態 overhaul = 可生成 / 動態 controller = scaffold-only」的判準範例。

---

### 實查清單（grounding）

- **實檔**：`~/skyrim_mods/unzip/SupplyAndDemand/Supply and Demand.esp` + `Scripts/{tc_PlayerScript, tc_MonitorScript, tc_AttachScript, tc_PlayerScriptAddSpell, tc_SupplyDemandMCM}.pex`（**無 `.psc`/`.dll`/`.ini`/`.json`，已 `find` 確認**）。
- **plugin**：ModForge CLI `dump` → 12 record（2 GLOB / 3 MGEF / 4 SPEL / 2 QUST / 1 CELL+1 ref override；**無 perk / GMST / Faction / VendorValues / LVLI**）。
- **pex**：`strings` 抽得 `OnItemAdded`/`OnItemRemoved`/`GetGoldValue`/`SetGoldValue`/`ValuesArray1-30`/`OriginalValuesArray1-30`/`tc_Global_ExtinctionRatio`/`SKI_ConfigBase`/"market values shift back to normal" 等（function/property/常數名為據；**邏輯細節為 inference，本機無 decompiler**）。
- **ModForge code**：GMST 缺席（`grep gamesetting|gmst` src/ModForge.Core → 空）；`Spec.Globals.cs`/`Generator.Build.Globals.cs`、`Spec.Magic.cs`(L59-63 `Scripts`)/`Generator.Build.Magic.cs`、`ScriptAttachSpec`(`Spec.Dialogue.cs` L227，有 `Source` `.psc` 欄)、`Spec.Mcm.cs`/`McmGen.cs`/`Generator.Build.Mcm.cs`、`Papyrus.cs`(雙編譯路徑) 皆存在。
