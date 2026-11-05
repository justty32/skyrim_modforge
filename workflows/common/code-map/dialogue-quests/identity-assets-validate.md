# CODE_MAP.dialogue-quests — 身份系統（Papyrus 資產・驗證・打包）

← [CODE_MAP.dialogue-quests](../CODE_MAP.dialogue-quests.md)

## 身份系統（續：Papyrus 資產・驗證・打包）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Asset | `assets/papyrus/MFIdentityBook.psc` | 可複用身份書（**extends ObjectReference**，OnRead → AddToFaction+AddSpell+**AddPerk(GrantPerk)**+Scene.Start；Toggle 反向移除）；**鐵律：OnRead 是 ObjectReference 的 event，不是 Book 的——`extends Book` 永遠收不到 OnRead（Book/ObjectReference 都 extends Form、是兄弟）；綁在 BOOK base form 上、背包讀也會 fire（in-game 2026-06-07 確認）**；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityDefault.psc` | 預設身份授予（**extends Quest**，OnInit 遍歷 `Factions[]` AddToFaction + `Grants[]` AddSpell + **`Perks[]` AddPerk**，idempotent 跳過已持有）；StartGameEnabled host quest 開局/載入觸發（進 `.seq` 故舊存檔也跑，in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityController.psc` | 主身份 controller（**extends Quest**，OnInit + `RegisterForSingleUpdate(3.0)` poll → `Recompute`：Override(若持有)否則 priority 最高持有 → 寫 `Primary` GLOB）；Factions[]/Codes[] 平行 priority DESC（in-game 2026-06-07 確認）；改 .psc 要重編 .pex；embed 進 CLI（條件式 EmbeddedResource）|
| Asset | `assets/papyrus/MFIdentityAutoGrant.psc` | 自動授予 trigger（**extends Quest**，OnInit + 5s poll → `Check`：`p.GetActorValue(AvNames[i]) >= Thresholds[i] && !IsInFaction → AddToFaction`，純 Papyrus 讀 AV、免 SKSE/事件 hook）；Factions[]/AvNames[]/Thresholds[] 平行；只授 faction；改 .psc 要重編 .pex；embed 進 CLI（條件式）|
| Validate | `Generator.Validate.cs` | `RegisterIdentityFactions`（早登錄自建 FACT editorId 供 condition 解析）+ `ValidateIdentities`（unique id、非空 faction、grants/**grantPerks**/acquireBook/**activeWhen param** CheckRef、**autoGrantWhen actorValue 非空**）|
| Validate | `Generator.Validate.Quests.cs` | dialogue 規則；**`hello:true` 招呼免 prompt**（招呼非玩家選項；其餘 dialogue 仍須 prompt）；**`setPrimaryIdentity` 須是已知 id 或 `auto`**；**`rewardItem` CheckRef**；**`persist`/`syncPerks`（dialogue + stage 共用 `ValidatePersistBlock`/`ValidateSyncPerksBlock`）：storage 非空、key 三態(`ValidatePersistKey`：speaker〔dialogue only〕/player/ref→CheckRef；stage allowSpeaker=false)、persist entry 恰一值型(int/float/str/form)、delta 限 int/float、form/perk CheckRef**；**`storageWrites`（`ValidateStorageWrites`，dialogue + stage 共用）：key 非空、恰一值型(int/float/str)、delta 限 int/float、target 三態(speaker〔dialogue only，stage allowSpeaker=false 拒，含空缺省〕/player/none/**ref→CheckRef**)、**`fromJson` 須 file+key 非空****；**M組 `variants`：parent 無 responses 時免「無回應行」、每 variant 須有 responses + emotion 合法 + 自有 conditions `CheckCondition`、`hello` 不可帶 variants**|
| Package | `src/ModForge.Cli/Commands/Package.cs` §5d–§5g | §5d acquireBook → `MFIdentityBook.pex`；§5e `default:true` → `MFIdentityDefault.pex`；§5f primaryIdentity/setPrimaryIdentity → `MFIdentityController.pex`；§5g `autoGrantWhen` → `MFIdentityAutoGrant.pex`（皆進 Scripts/）；dialogue fragment 編譯傳 `IdentityCode` 給 `GenerateDialogueFragmentSource`|
