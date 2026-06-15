# po3's Tweaks（powerofthree's Tweaks）Finding

**版本**：1.15.1  
**作者**：powerofthree  
**Nexus**：https://www.nexusmods.com/skyrimspecialedition/mods/51073  
**檔案**：`SKSE/Plugins/po3_Tweaks.dll` + `po3_Tweaks.ini`（初次執行後自動生成於 overwrite）  
**前置**：SKSE64（需配合 Skyrim 版本：SE v1.5.97 或 AE v1.6.629+）  
**Papyrus API**：`po3_Tweaks.pex`（`IsTweakInstalled(String asTweak) -> bool`）

---

## 一、這個工具做什麼 + 工作原理

po3's Tweaks 是一個純 SKSE native 外掛（C++ DLL），**不生成任何 .esp record**，純粹在引擎層打 patch 或修改行為。它將所有開關集中在 `SKSE/Plugins/po3_Tweaks.ini` 中，以 key = value 形式控制。

**三大分區**：

| 分區 | 性質 | 說明 |
|------|------|------|
| `[Fixes]` | Bug fix | 修引擎本身的 bug，幾乎全部預設 `true` |
| `[Tweaks]` | 行為改變 | 可選的遊戲機制調整，預設多為 `false` 或低衝擊值 |
| `[Experimental]` | 實驗性 | 效能最佳化或邊緣功能，需手動啟用 |

**工作原理**：遊戲啟動時 SKSE 載入 DLL，DLL 在記憶體對引擎函數做 detour/hook（不碰 ESM/ESP），ini 設定決定哪些 hook 啟動。無執行期依賴，也無 Quest/Script/Form 需求。

---

## 二、ini 設定檔結構（完整說明）

### [Fixes] — 引擎 Bug 修正

所有 Fixes 預設 `= true`（部分有數值模式）。

| Key | 說明 | 與 ModForge 的交集 |
|-----|------|--------------------|
| `Distant Ref Load Crash = true` | 修 distant ref 缺少 3D 時的載入 crash | - |
| `Map Marker Placement Fix = true` | 允許在 fast travel 關閉時仍能放置 map marker | 和 ModForge worldspace/map-marker 生成有關 |
| `Restore 'Can't Be Taken Book' Flag = true` | 恢復書本「不可取得」flag 功能 | 若 ModForge 生成 Book record 可利用此 flag |
| `Projectile Range Fix = true` | 修正移動中射擊的彈道距離計算 | - |
| `CombatToNormal Dialogue Fix = true` | 修 NPC 誤用 LostToNormal dialogue 取代 CombatToNormal | **直接影響 ModForge NPC dialogue**（見下） |
| `Cast Added Spells on Load = true` | 場景載入後重新施加 AddSpell 添加的效果 | 若 ModForge 生成 NPC 的永久 ability SPEL 需要注意 |
| `Cast No-Death-Dispel Spells on Load = true` | 重新施加 no-death-dispel 法術效果於死亡 actor | - |
| `IsFurnitureAnimType Fix = true` | 修 IsFurnitureAnimType 條件/console 函數 | 和 scene/furniture 相關（ModForge SceneSpec） |
| `Light Attach Crash = true` | 修角色未載入時光源 attach crash | - |
| `No Conjuration Spell Absorb = true` | 所有缺少 NoAbsorb flag 的召喚法術自動補上 | 若生成召喚系 SPEL 可不用手動設 flag |
| `EffectShader Z-Buffer Fix = true` | 修粒子特效透視渲染 | - |
| `ToggleCollision Fix = true` | Console ToggleCollision 改為切換選取物件的碰撞 | - |
| `Skinned Decal Delete = true` | 即時刪除標記移除的蒙皮貼花（脫裝甲時的血跡） | - |
| `Jumping Bonus Fix = true` | 跳躍高度乘以 JumpingBonus actor value 的 1%/點 | 若生成 perk/ability 修改 JumpingBonus 需知道 |
| `Toggle Global AI Fix = true` | TAI/Debug.ToggleAI() 現在真正切換所有 NPC AI | - |
| `Use Furniture In Combat = 1` | `0`=關, `1`=僅玩家, `2`=玩家+NPC；允許戰鬥中使用家具 | 影響 ModForge SceneSpec 中家具使用的 idle |
| `Breathing Sounds = true` | 修呼吸聲在換 cell 後持續播放 | - |
| `Load EditorIDs = true` | 執行期載入跳過的 form 的 editorID | 對開發 debug 有用 |
| `First Person SetAlpha Fix = true` | 修 SetAlpha 讓第一人稱手隱形的 bug | - |
| `Worn Restrictions For Weapons = true` | 啟用附魔的 Worn Restrictions 功能於武器 | - |
| `MagicItemFindKeywordFunctor Crash = true` | 修 keyword 查找 crash（效果缺少魔法效果） | 若 ModForge SPEL 有 keyword 條件需知 |
| `Left Handed Weapon Enchantment Node Fix = true` | 修左手武器附魔節點（XMPSE/HDT-SMP 環境） | - |
| `Validate Screenshot Location = true` | 驗證截圖路徑有效性 | - |

### [Tweaks] — 可選行為調整

| Key | 預設值 | 說明 | 與 ModForge 的交集 |
|-----|--------|------|--------------------|
| `Faction Stealing = false` | false | 在同派系友好成員面前偷竊才算偷竊 | 影響 ModForge 生成的 NPC 所屬 faction |
| `Voice Modulation = 1.0` | 1.0（無效果） | 戴面甲 NPC 聲音失真；0.85-0.90 推薦值 | 若 ModForge NPC 戴面甲可告知設定 |
| `Game Time Affects Sounds = false` | false | 時間減速時聲調同步降低 | - |
| `Dynamic Snow Material = false` | false | 帶方向雪的靜態物件自動加雪碰撞材質 | - |
| `Disable Water Ripples On Hover = false` | false | 懸浮 NPC 不觸發水面漣漪 | - |
| `Screenshot Notification To Console = false` | false | 截圖通知轉為 console 輸出 | - |
| `No Attack Messages = 0` | 0 | 0=關，1=關暴擊訊息，2=關背刺訊息，3=全關 | - |
| `Sit To Wait = false` | false | 只能坐著等待 | 影響 NPC/玩家互動設計 |
| `Sit To Wait Message` | 字串 | 自訂提示文字 | - |
| `Disable God Mode = 0` | 0 | 0=關，1=只關神模式，2=只關不死，3=全關 | - |
| `No Hostile Spell Absorb = false` | false | 所有非敵對/非負面法術自動加 NoAbsorb flag | 若生成 buff/aura SPEL 可用 |
| `Grabbing Is Stealing = false` | false | 拿取擁有物件算偷竊 | - |
| `Load Door Activate Prompt = 0` | 0 | 0=關，1=替換提示文字，2=替換+出口顯示內部房名 | - |
| `Enter Label / Exit Label` | 字串 | 配合上面的入口/出口文字 | - |
| `No Poison Prompt = 0` | 0 | 0=關，1=停用確認，2=通知顯示，3=兩者 | - |
| `Silent Sneak Power Attacks = false` | false | 潛行蓄力攻擊時防止玩家呼喊 | - |
| `Offensive Spell AI = false` | false | NPC 裝備攻擊法術前先驗證條件有效性 | 若 ModForge 生成 AI 包含攻擊法術 NPC 可用 |

### [Experimental] — 實驗性

| Key | 預設值 | 說明 |
|-----|--------|------|
| `Fast RandomInt() = false` | false | 加速 Utility.RandomInt 呼叫 |
| `Fast RandomFloat() = false` | false | 加速 Utility.RandomFloat 呼叫 |
| `Clean Orphaned ActiveEffects = false` | false | 移除缺少 ability perk 的 NPC 的 active effects |
| `Update GameHour Timers = false` | false | GameHour.SetValue 推進時間後同步更新遊戲計時器 |
| `Stack Dump Timeout Modifier = 30.0` | 30.0 | Papyrus stack dump 等待秒數（0=停用） |

---

## 三、對 ModForge 的參考價值

**定位**：純前置 + 行為環境標記。ModForge 不生成 po3_Tweaks 的任何 record（它沒有 esp record）。

### 直接影響 ModForge 生成物的 tweak

1. **`CombatToNormal Dialogue Fix`（Fixes）**  
   - 修引擎 bug：NPC 戰鬥結束回 normal 狀態時誤用 LostToNormal 分支。  
   - **影響**：ModForge 生成的 NPC dialogue 若有 CombatToNormal topic，只有在 po3's Tweaks 安裝後才能正確觸發。若不假設安裝，需用額外的 LostToNormal INFO 做 fallback，或將 CombatToNormal 設計成和 LostToNormal 相同（最保險）。

2. **`Use Furniture In Combat`（Fixes）**  
   - **影響**：ModForge SceneSpec 若設計 NPC 戰鬥中坐椅/使用家具，此 tweak 決定能否成立。不安裝時家具 action 在戰鬥中會失效。

3. **`Cast Added Spells on Load`（Fixes）**  
   - **影響**：ModForge 若為 NPC 生成永久 ability SPEL（例如特殊被動）並用 AddSpell 添加，此修正確保存檔載入後效果不丟失。不安裝時可能需要在 OnInit/OnPlayerLoadGame 重新 AddSpell。

4. **`IsFurnitureAnimType Fix`（Fixes）**  
   - **影響**：ModForge 生成的 dialogue/condition 若用 IsFurnitureAnimType，需此 fix 才能在家具 reference 上正確求值。

5. **`No Conjuration Spell Absorb` / `No Hostile Spell Absorb`（Fixes/Tweaks）**  
   - **影響**：ModForge 生成召喚或 buff 法術時，若有安裝 po3's Tweaks 則不需手動設 NoAbsorb flag（自動補上）；若沒安裝則需在 SPEL record 手動指定。

6. **`Offensive Spell AI`（Tweaks）**  
   - **影響**：ModForge 生成的 NPC 若裝備攻擊法術 AI package，此 tweak 決定 NPC 是否預先驗證法術條件。可讓法術 AI 行為更符合設計意圖。

7. **`Clean Orphaned ActiveEffects`（Experimental）**  
   - **影響**：ModForge 更新 NPC perk/ability 後，若舊的 active effect 未清理，此 tweak 自動清除。對 ModForge 的版本迭代 mod 有用。

8. **`Update GameHour Timers`（Experimental）**  
   - **影響**：若 ModForge 生成的 Quest/Script 依賴 GameHour 推進做計時觸發，需此 tweak 確保 timer 同步更新。

### Papyrus API

```papyrus
; 任何 psc 可查詢某 tweak 是否啟用
bool bFix = po3_Tweaks.IsTweakInstalled("CombatToNormal Dialogue Fix")
```

可用於 ModForge 生成的 script 做條件分支（有 po3 時用 A 路徑，沒有時用 B 路徑）。

### 結論

- **ModForge 立場**：po3's Tweaks 是「建議安裝前置」層級，不是必須。ModForge spec 可加一個 `optional_dependencies` 欄位說明「若安裝 po3's Tweaks，下列 tweak 會改善行為」。
- **生成物設計原則**：不應以 po3's Tweaks 存在為假設前提；若依賴某 fix，應在 spec 文件備注「需要 po3's Tweaks」。
- **立即可用的 API**：`IsTweakInstalled()` 可在 generated script 裡做 graceful fallback。

> ⚠️ 以上「ModForge 缺什麼」欄位為推斷，未查 ModForge src/，可能有誤判。
