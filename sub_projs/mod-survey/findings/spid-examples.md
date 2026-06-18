# 真實 ini 範例（附中文解釋）

← [spid](spid.md)

## 五、真實 ini 範例（附中文解釋）

來源：本地 `~/skyrim_mods/unzip/` 的真實 mod 檔。

```ini
; === nwsFF_SkillBoostsPerks_DISTR.ini ===
; 給所有帶 ActorTypeNPC keyword 的 NPC 加兩個 perk：
; 讓 NPC 也能受 alchemy/skill boost perk 效果影響（原本只有玩家能用）
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC
Perk = 0xA725C~Skyrim.esm|ActorTypeNPC
```

```ini
; === nwsFF_SpellMag_DISTR.ini ===
; 給所有 NPC 加法術威力 perk（NFF 自訂 perk，讓 NPC 法術隨等級縮放）
Perk = 0x4F9D6D~nwsFollowerFramework.esp|ActorTypeNPC
```

```ini
; === nwsFF_FriendlyFire_DISTR.ini ===
; 給所有 NPC 加友軍傷害 perk（NFF 的友軍傷害控制系統）
Perk = 0x4F9D6C~nwsFollowerFramework.esp|ActorTypeNPC
```

```ini
; === ImGladYoureHere_DISTR.ini ===
; 給特定 NPC（JJSofiaFollower、PumpkinTheFoxActor）加入 GYH 的 faction，
; 讓 GYH 能識別這些 follower 並執行擁抱/互動場景
Faction = WW42GYHSofialikeFollowerDialogueFixFaction|JJSofiaFollower|NONE|NONE|NONE|NONE|NONE
Faction = WW42GYHPetPatchFaction|PumpkinTheFoxActor|NONE|NONE|NONE|NONE|NONE
```

**其他常見模式（源自文件範例）**：

```ini
; 給等級 25-50 的女性 NPC 加 Flames 法術
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50|F

; 給男性 unique NPC（Destruction 技能 >= 10）加 Flames
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|14(10)|M/U

; 給某 NPC（除 Nazeem 外）的 inventory 加 3000 個金幣
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|NONE|NONE|NONE|3000

; 給 ActorTypeGhost 且 Vampire 的 NPC 加龍吼
Shout = 0x13E07~Skyrim.esm|ActorTypeGhost+Vampire

; 給貧民 NPC 加 ActorTypePoor keyword（按 EditorID 指定目標 NPC）
Keyword = ActorTypePoor|Brenuin

; 給 BanditFaction 中的 NPC 加一個 perk，機率 50%（非 unique）
Perk = 0x9DE80~test.esp|NONE|0x1BCC0~test.esp|NONE|NONE|NONE|50
```

---

