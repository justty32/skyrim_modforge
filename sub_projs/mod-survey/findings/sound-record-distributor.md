# Sound Record Distributor（SRD）— Survey Finding

> 調查日期：2026-06-15  
> 來源：DLL 版本 1.5.3（Nexus #77815）；原始碼 github.com/doodlum/skyrim-srd  
> 作者：doodlum（原 doodlez）  
> ⚠️ ModForge 缺口評估為**推斷**，未做完整 src/ code pass。

---

## 一、SRD 做什麼 + 工作原理

Sound Record Distributor（SRD）是一個 SKSE plugin，在遊戲載入後（`kDataLoaded`）讀取 `Data\` 下的 config 檔，把 Sound 相關欄位「注入」到已載入的 record 裡——完全不需要額外的 .esp patch。

### 核心對比：SRD vs SPID

| 面向 | SPID | SRD |
| --- | --- | --- |
| 分發**對象**（Target） | NPC（Actor） | 物件 record（WEAP/ARMO/MGEF/PROJ/EXPL 等） |
| 分發**內容** | Spell、Perk、Item、Faction 等 | Sound Descriptor（SNDR）、Impact Data Set、Footstep Set |
| config 格式 | `_DISTR.ini`（行格式） | `_SRD.json/.jsonc/.yaml` |
| filter 機制 | StringFilter/FormFilter/LevelFilter/Traits 詳細 | 僅 `Requirements`（mod 存在與否）+ 直接指定 Form |
| 同系列？ | po3 出品 | doodlum 出品，同概念不同作者 |

**工作流程**：
1. 遊戲正常載入所有 form（load order 不受影響）
2. SRD 掃 `Data\` 尋找所有 `*_SRD.json`、`*_SRD.jsonc`、`*_SRD.yaml`
3. 檔名含 `.es`（如 `MySoundMod.esp_SRD.yaml`）→ plugin-bound config，只在對應 plugin 載入時生效
4. 其餘（如 `MySoundMod_SRD.yaml`）→ general config，按字母順序套用
5. 逐 record 把欄位覆蓋（或 Region 的情況是「找不到就追加」）
6. 完成後記錄 conflict summary（同一欄位被多個 config 寫過則警告）

**關鍵優勢**：SRD 的音效注入在 runtime 發生，等同零 load-order 衝突。同一武器的 Impact Data Set 可讓多個 SRD config 同時關注，最後寫入的 config 得分。和 SPID 不同，SRD **不做 per-NPC 分發**，它改的是 form 本身的屬性。

---

## 二、Config 語法全集

### 命名規則

```
Data\
  MySoundPatch_SRD.yaml          ← general config（按字母順序套用）
  MySoundPatch_SRD.jsonc         ← 同上，但 JSONC 格式
  Dawnguard.esm_MySoundPatch_SRD.yaml    ← plugin-bound：只在 Dawnguard.esm 載入時生效
```

格式支援：`.json`、`.jsonc`（支援 `//` 注解）、`.yaml`（建議，最緊湊）

### 頂層結構（YAML 示意）

```yaml
Requirements:
  - Dawnguard.esm          # 必須存在（可多條）
  - AnotherMod.esp!        # 必須「不存在」（後綴 !）

Weapons:
  - Form: "WeapIronSword"                    # EditorID（需 po3 Tweaks 支援 EditorID lookup，SNDR/Region 例外）
    Pick Up: "ITMWeaponUpSD"                 # Sound Descriptor EditorID 或 "Plugin.esp|0xFORMID"
    Put Down: "ITMWeaponDownSD"
    Impact Data Set: "ImpactSetSwordOneHand"
    Attack: "WPNSwordRightSwingSD"
    Attack 2D: "WPNSwordRightSwing2DSD"
    Attack Loop: "WPNSword1HSwingLoopSD"
    Attack Fail: "WPNSwordFailSD"
    Idle: "WPNSwordIdleSD"
    Equip: "ITMWeaponEquipSD"
    Unequip: "ITMWeaponUnequipSD"

Armor Addons:
  - Form: "ArmorIronCuirass_A"
    Footstep: "FSTPArmorIron"                # BGSFootstepSet EditorID

Armors:
  - Form: "ArmorIronCuirass"
    Pick Up: "ITMArmorUpSD"
    Put Down: "ITMArmorDownSD"

Misc. Items:
  - Form: "MiscGold001"
    Pick Up: "ITMGoldUpSD"
    Put Down: "ITMGoldDownSD"

Soul Gems:
  - Form: "SoulGemCommon"
    Pick Up: "ITMSoulGemUpSD"
    Put Down: "ITMSoulGemDownSD"

Magic Effects:
  - Form: "AbFrostbiteVenom"
    Sheathe/Draw: "MAGICStaffSheatheSD"
    Charge: "MAGICSpellChargeSD"
    Ready: "MAGICSpellReadySD"
    Release: "MAGICSpellReleaseSD"
    Cast Loop: "MAGICSpellLoopSD"
    On Hit: "MAGICSpellHitSD"

Projectiles:
  - Form: "MagicFireProjectile"
    Active: "MAGICFireLoopSD"
    Countdown: "MAGICFireCountSD"
    Deactivate: "MAGICFireDeactivateSD"

Explosions:
  - Form: "ExplFireball"
    Interior: "MAGICFireballSD"
    Exterior: "MAGICFireballSD"

Effect Shaders:
  - Form: "EFSHFireDamageFX"
    Ambient: "MAGICFireAmbientSD"

Ingestibles:
  - Form: "DrinkMead"
    Consume: "ITMPotionUsedSD"

Regions:
  - Form: "HoldTundraRegion"                 # TESRegion（可直接用 EditorID，不需 po3 Tweaks）
    RDSA:
      - Sound: "AMBTundraWindSD"             # BGSSoundDescriptorForm
        Flags: "Pleasant Cloudy"             # 空白分隔；Pleasant/Cloudy/Rainy/Snowy，不寫則全 set
        Chance: 0.05                         # float，不寫則 0.05
```

### Form 引用格式

| 寫法 | 意義 |
| --- | --- |
| `"WeapIronSword"` | EditorID（**多數 record 類型需要 po3 Tweaks 才能 runtime lookup EditorID**） |
| `"Skyrim.esm\|0x12345"` | PluginName\|FormID（十六進位，不需 po3 Tweaks） |

> **特例**：Region（REGN）和 Sound Descriptor（SNDR）可直接用 EditorID，不需 po3 Tweaks。其他類型（WEAP/ARMO/MGEF 等）需要 po3 Tweaks 才能做 EditorID lookup。

---

## 三、支援的 record 類型對照表

### Target record 類型 × 可設的 sound 欄位

| Target（改誰） | 可設欄位 | Sound 類型（Source） |
| --- | --- | --- |
| **Weapon**（WEAP） | Pick Up, Put Down, Impact Data Set, Attack, Attack 2D, Attack Loop, Attack Fail, Idle, Equip, Unequip | BGSSoundDescriptorForm（Impact Data Set 除外用 BGSImpactDataSet） |
| **Armor Addon**（ARMA） | Footstep | BGSFootstepSet |
| **Armor**（ARMO） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Misc. Item**（MISC） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Soul Gem**（SLGM） | Pick Up, Put Down | BGSSoundDescriptorForm |
| **Magic Effect**（MGEF） | Sheathe/Draw, Charge, Ready, Release, Cast Loop, On Hit | BGSSoundDescriptorForm |
| **Projectile**（PROJ） | Active, Countdown, Deactivate | BGSSoundDescriptorForm |
| **Explosion**（EXPL） | Interior, Exterior | BGSSoundDescriptorForm |
| **Effect Shader**（EFSH） | Ambient | BGSSoundDescriptorForm |
| **Ingestible**（ALCH） | Consume | BGSSoundDescriptorForm |
| **Region**（REGN） | RDSA 陣列（Sound + Flags + Chance）| BGSSoundDescriptorForm，可新增或替換 |

**不支援**（v1.5.3）：
- NPC_（NPC 本身沒有直接 sound field，音效透過 footstep set 在 ARMA 上）
- WEAP 的 `swingDownSound`（CK 顯示的欄位，但 SRD 未列入）
- SPEL 本身（魔法音效走 MGEF，不走 SPEL）
- MusicType（MUSC）— 有另一個 mod「Music Type Distributor」(Nexus #119571) 專門處理

---

## 四、Filter 語法

SRD 的 filter 機制**遠比 SPID 簡單**。SPID 有 StringFilter/FormFilter/LevelFilter/Traits 四層；SRD 只有：

1. **Requirements**（頂層 mod 存在判斷）：整個 config 是否生效，以 mod 載入狀態決定
   - `"Mod.esp"` → 此 mod 必須存在
   - `"Mod.esp!"` → 此 mod 必須**不存在**（後綴驚嘆號）

2. **Form 直接指定**：每個 entry 直接寫 EditorID 或 FormID，沒有「按條件批量選多個 form」的能力

**沒有**：race filter、faction filter、keyword filter、NPC 名稱 filter、level range filter 等。

**結論**：SRD 不做「找哪些 NPC 來分發」，而是「直接指定某個 form，修改它的音效欄位」。精準但不批量（除非寫很多 entry）。

---

## 五、對 ModForge 的評估

### 現有 ModForge 音效支援（確認自 src/）

ModForge 已能在 esp 內設定：

| 類型 | 支援狀態 |
| --- | --- |
| Sound Descriptor（SNDR）建立 | ✅ `BuildSounds()` + `WireSounds()` |
| SNDR Category + OutputModel | ✅ 有預設值 |
| Weapon PickUp / PutDown | ✅ `WireSounds()` |
| MiscItem PickUp / PutDown | ✅ `WireSounds()` |
| Activator ActivationSound / LoopingSound | ✅ `WireSounds()` |
| Music Track（MUST）+ Music Type（MUSC） | ✅ 獨立建構路徑 |
| NPC 語音（Voice） | ✅ 完整 voice gen pipeline |

ModForge **尚未**在 esp 內設定的音效欄位（推斷，未驗 spec schema）：

| 類型 | 備注 |
| --- | --- |
| Weapon Attack / Equip / ImpactDataSet 等完整欄位 | WireSounds 只寫了 PickUp/PutDown |
| ArmorAddon Footstep Set | 可能需要 |
| MGEF 六個音效 slot | MagicEffect 音效未見 wire |
| PROJ / EXPL / EFSH / ALCH 音效 | 可能走 vanilla template copy |
| Region 音效（RDSA） | 未見 |

### esp-side 設音效 vs SRD 分發：取捨

**直接在 esp 設定（ModForge 現行做法）**：
- 優點：自含 mod、無額外依賴（不需 SRD SKSE plugin）、load order 清晰
- 優點：自建 NPC 的 ARMA footstep、自建 WEAP 的 attack sound 直接寫 record 最簡單
- 缺點：若需要「讓外部 mod 的武器也有我的音效」就必須做 patch（衝突）

**SRD 分發**：
- 優點：不寫 esp patch、無 load order 衝突、音效 mod 對玩家 mod 友善
- 適合場景：你是「音效作者」，想讓你的 SNDR 替換外部 mod（AOS、ISC 等）的武器音效
- **不適合**：ModForge 自建 NPC/武器的情況——自建 form 直接在 esp 設音效更直接

### 結論

**ModForge 自建 NPC / WEAP / ARMO 時，優先在 esp 內設定音效**（現行做法正確）。SRD 的主要價值是「audio mod 對別人 mod 的無 patch 覆蓋」，不在 ModForge 的核心生成路徑上。

有一個潛在擴充方向：若 ModForge 日後支援「音效 patch config 生成」（讓玩家的 NPC/武器吃上 ISC 等音效而無 patch），可輸出 `_SRD.yaml` 作為配套產物。這是 **roadmap 等級的 feature**，目前不是缺口。

### 依賴注意事項

- SRD 本身：需要 SKSE（已是 ModForge mod 的基礎前置）
- EditorID lookup（Region/SNDR 以外的 form）：額外需要 **po3 Tweaks**（po3's Tweaks）
- 如果只引用 FormID（`Plugin.esp|0xID` 格式）則不需 po3 Tweaks

---

## 參考

- Nexus 頁面：https://www.nexusmods.com/skyrimspecialedition/mods/77815
- 原始碼：https://github.com/doodlum/skyrim-srd
- STEP Wiki：https://stepmodifications.org/wiki/SkyrimSE:Sound_Record_Distributor
- ISC SRDified（使用範例）：https://www.nexusmods.com/skyrimspecialedition/mods/78446
- Music Type Distributor（MUSC 分發的姐妹 mod）：https://www.nexusmods.com/skyrimspecialedition/mods/119571
