# 做什麼 + 工作原理 + Config 語法全集

← [sound-record-distributor](sound-record-distributor.md)

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

