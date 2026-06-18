# Immersive World Encounters SE (v3.6.1)

← [encounter-mods](encounter-mods.md)

## Immersive World Encounters SE (v3.6.1)

### 基本資訊

- **档名**：`Immersive World Encounters SE-18330-V3-6-1-1639501058.7z`
- **ESP**：`Immersive Encounters.esp`（3 MB）；資料在 `Immersive Encounters.bsa`（764 MB，含語音/mesh）
- **Master files**：`Skyrim.esm`, `Update.esm`, `Dawnguard.esm`, `HearthFires.esm`, `Dragonborn.esm`（全部 DLC）
- **作者前綴**：`Sette`（所有 EditorID 帶 `_Sette` 或 `Sette`）
- **無 Source scripts**：BSA 裡沒有 `.psc`，只有編譯好的 `.pex`

### 核心機制

IWE 完全寄生 Skyrim 原版 Story Manager，沒有自己的 event root。觸發鏈：

```
原版 SM event root（WEQuests / WIChangeLocation* / WITavernQuestNode* / DLC2WE）
  └─ IWE SMBN（7個，做分流）：WE_SetteRandomBranch / WE_SetteQuests / WI_SetteCL* …
       └─ IWE SMQN（37個，掛實際 quest）：WE_SetteRoads / WE_SetteFactions / WE_SetteCLNode{City/Village/…} …
            └─ 單顆 encounter Quest（WE_Sette*, WI_Sette*）
                 ├─ Quest aliases：演員（LeveledNpc fill）+ TRIGGER marker + TravelMarker + Hold偵測
                 ├─ QF fragment script + WEScript 共用控制器
                 ├─ AI Package（vanilla Travel/Sandbox template 薄包裝）
                 ├─ Scene（Dialog/Package/Timer 三動作交織）
                 └─ Dialogue INFO（CTDA 多條件分歧對白）
```

**vanilla quest override**：直接 override 幾個原版 WE quest（`WE01`, `WE24`, `WE25`, `WE31`）加入 IWE 邏輯，並用 `WE24_LocationMatters_Sette` 這個 Global 做地點加權。其餘 ~100+ 遭遇是新增的 SM leaf quest。

**靜態預擺 PE Markers**：5 個 `IWE_PEMarker` 持久擺放在 Tamriel 固定位置（Haafingar / Reach / Riften / Whiterun / Eastmarch），作為 SE 版新增的持久遭遇觸發錨點，比純 alias-MoveTo 更穩定地控制遭遇發生區域。

### Spawn 邏輯

- **演員填充**：alias fill = `from LeveledNpc`（65 個 LVLN list），如 `_SetteLCharWEWandererAll`、`_SetteLCharWEBountyHunter`。每次遭遇演員不同。
- **走位**：AI Package 以 `AliasForReference` 做 target，NPC 走到 quest 的 travel-marker alias 位置。
- **地點分流**：SMQN 的條件含 `WIChangeLocation` keyword（`LocTypeCity` / `LocTypeVillage` / `LocTypeTavern`…），讓不同地點類型觸發不同遭遇桶。
- **Hold 偵測**：alias `myHoldImperial`/`myHoldSons`/`myHoldContested` 偵測內戰歸屬，讓對白/陣營條件化。
- **演出節奏**：Scene phase = Package 動作（走位）+ Timer 動作（卡節奏）+ Dialog 動作（播台詞）。

### ModForge 可生成的部分

| 機制 | 狀態 |
|------|------|
| SM additive branch/quest node（掛原版 WEQuests root） | ✅ 已支援 |
| 隱形 encounter quest（無 journal/objective，純狀態機） | ✅ 已支援 |
| Scene 三動作交織（Dialog/Package/Timer + multi-phase） | ✅ 已支援 |
| CTDA 反應性對白（GetIsAliasRef / GetStage / GetEquipped…） | ✅ 已支援 |
| AI Package vanilla template 薄包裝 | ✅ 已支援 |
| LeveledNpc / LeveledItem / Outfit | ✅ 已支援 |
| **alias fill from LeveledNpc（LVLN picker）** | ⚠️ **缺口，最高優先** |
| **Package/marker target 指到 quest alias（alias indirection）** | ⚠️ **缺口** |
| SM branch/quest-node 多層分流 + 加權 | ⚠️ 缺口（「選台機」擴充） |

### 設計模式筆記

- **Hold 偵測 alias**：`LocationAlias` fill from 原版 Hold location + 內戰歸屬條件 → 同一個遭遇在帝國控 Hold / 風暴披風控 Hold 說不同台詞、生不同陣營 NPC。可當「context-aware encounter」的範本。
- **多桶 SMQN 命名即路由表**：`WE_SetteCLNode{City,Village,Tavern,Dragon}` 看名字就知道它對應哪種地點類型。AI-agent 友善。
- **PE Marker 作為「地域錨」**：5 個固定擺在不同 Hold 的持久 marker，讓某些遭遇能「以某 Hold 中心點為半徑」觸發，比純 SM 觸發更能控制地理分布。

---

