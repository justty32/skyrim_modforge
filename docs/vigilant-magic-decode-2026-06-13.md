# VIGILANT 魔法 解碼（2026-06-13）

以唯讀 binary overlay 解碼 VIGILANT（`Vigilant.esm`，~21MB）的自訂魔法系統，作為 ModForge（已能建 SPEL/MGEF/ENCH/PERK/PROJ/EXPL/HAZD/SCRL）的對照參考。**全程不載入 Skyrim.esm 或任何 master**，跨檔引用一律以 raw FormKey 呈現（`<FormID>:<plugin>`）。

> 探測碼在 `/tmp/vig_magic_probe`（已清理），Mutagen 0.49.0 `CreateFromBinaryOverlay`。

---

## 1. 普查（先看數量）

| 記錄 | 數量 | 記錄 | 數量 |
|------|-----:|------|-----:|
| **Spells (SPEL)** | **712** | Projectiles (PROJ) | 80 |
| **MagicEffects (MGEF)** | **550** | Explosions (EXPL) | 44 |
| ObjectEffects / 附魔 (ENCH) | 71 | Hazards (HAZD) | 8 |
| Perks (PERK) | 102 | **Scrolls (SCRL)** | **0** |
| Shouts (SHOU) | 23 | WordsOfPower (WOOP) | **0**（全引 vanilla 三字根 `0252C3/C4/C5`） |
| Ingredients (INGR) | 10 | Ammunitions (AMMO) | 10 |
| Weapons (WEAP) | 317 | 其中帶附魔的武器 | 56 |

重點：**712 法術 / 550 魔法效果**是核心體量；附魔多在武器（56 把）+ 少量穿戴。**0 卷軸、0 自訂吼字根**——吼叫全部復用 vanilla 的三段字根、只換 spell。命名前綴一致：`zzzCH*` / `zzzAoM*` / `zzzBM*` / `zzzCO*`（作者統一 namespace）。

---

## 2. MGEF 原型（archetype）使用分佈

`MagicEffectArchetype.Type` 在 550 個 MGEF 上的分佈：

| Archetype | 數量 | 純 record? | 備註 |
|-----------|-----:|:---:|------|
| **Script** | **260** | ⚠️ | 近半數！archetype=Script 必綁 Papyrus 才有行為 |
| DualValueModifier | 78 | ✅ | 雙 AV 同時改（如同時扣血+耐力） |
| **SummonCreature** | 76 | ✅ | `Association`→召喚的 NPC/critter FormKey |
| ValueModifier | 71 | ✅ | 最基本傷害/治療 |
| Light | 16 | ✅ | `Association`→LIGT；常駐發光附魔 |
| PeakValueModifier | 14 | ✅ | 曲線型（漸強漸弱） |
| Cloak | 10 | ✅ | `Association`→近身觸發的 SPEL；護體火焰類 |
| Stagger | 9 | ✅ | 擊退硬直 |
| Absorb | 4 | ✅ | 吸血/吸魔 |
| Werewolf | 3 | ✅ | 變身 |
| Bound | 2 | ⚠️ | `Association`→武器；但實機綁 `BoundBowEffectScript` |
| **SpawnHazard** | 2 | ✅ | `Association`→HAZD（只有 2 個，但 HAZD 記錄有 8） |
| EnhanceWeapon | 1 | ✅ | `Association`→ENCH；改 `WeaponSpeedMult` |
| Banish / SpawnScriptedRef / DetectLife / SlowTime | 各 1 | 混 | DetectLife/Banish 純 record；SlowTime 綁 imod 腳本 |

**Casting / delivery 分佈（MGEF）**：CastType `FireAndForget 404 / ConstantEffect 129 / Concentration 17`；TargetType `Self 334 / Aimed 101 / TargetLocation 77 / Touch 38`。

**關鍵發現：550 個 MGEF 中 239 個帶 VMAD（Papyrus 腳本）** —— 約 43%。其中 archetype=Script 的 260 個幾乎全靠腳本驅動。最高頻的自訂腳本：

```
 31  CHSpMUnleashPowerScript        （釋放/環擊類）
 17  VigUniqueTrailEffectScript     （拖尾 VFX）
 15  CHGiantSwordEffectScript
 14  CHSpMChargeScript              （蓄力衝刺）
 13  CHSpMShootSpellCrossbowStyleScript
 12  CHUmarilSwordGateEffectScript
 11  CHCastSpellAroundScript / CHCastRainScript   （範圍多重施放 / 落雨）
  8  CHSpMStrikeDownScript
```

這些是 **Boss/獨特敵人的演出型法術**（蓄力、環形彈幕、落雨、拖尾），是 record 資料表達不出的程序化行為。

---

## 3. 自訂法術樣本（SPEL）

法術型別分佈：`Spell 446 / Ability 157 / LesserPower 82 / Voice 24 / Disease 2 / Poison 1`。
CastType：`FireAndForget 519 / ConstantEffect 160 / Concentration 33`。

典型樣本（皆 1~2 個 effect，magnitude 隨等級線性遞增——作者用「同 MGEF、多 SPEL 換 magnitude」做難度階梯）：

| SPEL editorId | 型 | cast/target | cost | effects |
|---|---|---|---|---|
| `zzzBMLamVampireBall00..05` | Spell | FF/Aimed | 39→115 | 同一 MGEF `10C878`，mag 15→40、area=15（吸血鬼火球 6 階） |
| `zzzCHHolyFlameCloakDmg` | Spell | **Concentration**/Aimed | 14 | MGEF `115EAC` mag8/dur1（聖焰持續灼燒） |
| `zzzCHTentacleWallMary` | Spell | FF/Aimed | 50 | MGEF `11D464` mag40/dur1/area12（觸手牆，見 §4 完整鏈） |
| `zzzCHOrderSpearLv1` | Spell | FF/Aimed | 75 | MGEF `123397` mag25/area8（秩序之矛） |
| `zzzCHcrArachenCorruption` | Spell | FF/Aimed | 3 | 2 effect：vanilla 毒 `0F198A` mag60 + `0A44C0`（蜘蛛吐毒，雜兵） |

模式：**雜兵法術 cost 極低（3）借 vanilla MGEF；Boss/獨特法術 cost 高、用自訂 MGEF + 自訂 VFX 鏈**。

---

## 4. VFX 鏈：SPEL → MGEF → PROJ → EXPL（與 HAZD）

ModForge 正好建 **EXPL←PROJ←MGEF←SPEL** 與 HAZD。VIGILANT 有 **79 個 MGEF 指向自家 PROJ、55 個指向自家 EXPL**，是大量 bespoke 飛行物特效。一條完整解出的鏈：

```
SPEL [zzzCHTentacleWallMary]    11D463  FireAndForget/Aimed  cost50
  → MGEF [zzzCHMgETentacleDamageFFMary] 11D464  arch=ValueModifier av=Health
  → PROJ [zzzCHTentacleProjectile03]    11D461  speed=1000 grav=0 flags=0
  → EXPL [zzzCHTentacleImpactExp]        0D20DC  force=200 dmg=0 radius=106
```

PROJ 樣本（速度/重力/旗標差異）：thunder arrow、tentacle 系皆 `speed≈1000, gravity=0`（直線快彈）；多數自訂 PROJ 的 `Explosion` 指回自家 EXPL。

EXPL 樣本：
- `zzzCHExplosionThunderArrow` force128/dmg2/radius320，**`ObjectEffect`→自家 MGEF `2F0CE6`**（爆炸 AoE 掛一個範圍 MGEF——ModForge 的 `ExplosionSpec.ObjectEffect` 正好對應）。
- `zzzCHForceStormExplosion` force10/radius1800（純擊退、無傷）；`zzzCHExplosionDetectEvil` radius5333（純偵測脈衝、force/dmg=0）。

HAZD（全 8 個）：每個 `Spell` 欄掛一個週期觸發的 SPEL（`TargetInterval` 多為 0.3，每 0.3s 對範圍內施放），`Flags` 常見 `AlignToImpactNormal`/`DropToGround`/`InheritDurationFromSpawnSpell`：
- `zzzCHBoneSpearHazard` radius4/life15/spell `1C6BC2`（骨矛地刺）
- `zzzCHOrderBarrierHazard01` spell→**vanilla `0591A4`** radius3.5/life30（秩序屏障）
- `zzzCHKyneHazard` radius40/life40，`Inherit{Duration,Radius}FromSpawnSpell`（從施放 spell 繼承半徑——配 §2 的 `SpawnHazard` MGEF `13EFC1` 投放）

注意 **SpawnHazard MGEF 只有 2 個，但 HAZD 有 8 個**——其餘 HAZD 由其它途徑（Papyrus `PlaceAtMe` 那類 `magicPlaceActivatorScript`，見 Kyne MGEF 的 2 個 VMAD）投放。

---

## 5. 附魔（ENCH）與 Perk

**ENCH（71 個 ObjectEffect）**：絕大多數武器附魔走 `CastType=FireAndForget, Target=Touch, EnchType=Enchantment`（接觸觸發），Cost=Amount；自身常駐型走 `ConstantEffect/Self`。樣本：

| ENCH | cast/target | cost | effects |
|---|---|---|---|
| `zzzCHEnchFrostSword` | FF/Touch | 69 | 自家 MGEF `0E2CFB` mag40 + **vanilla 減速 `0B72A0`** mag0.5（雙效冰劍） |
| `zzzCHEnchGoldenAbsorb` | FF/Touch | 109 | MGEF `0EA4C3` mag5/dur5（吸取） |
| `zzzCHEnchBlueEye/RedEye` | **ConstantEffect/Self** | 134 | 常駐自身（眼睛發光/夜視類） |
| `zzzCHEnchLightHistBin` | ConstantEffect/Self | 117 | 自家 Light MGEF `11880D` + vanilla `07A0F8`（常駐照明） |

帶附魔武器 56 把，多數 `vmad=0`（純 record，附魔在 ENCH 上）；ENCH 引用混 vanilla（`05B464` 等）與自家。

**Perk（102 個）**：**101 個是 entry-point perk、0 個帶 VMAD**——全部純 record！清一色 `EPModifyValue`（修改傷害值/抗性的數值乘修）。樣本：
- `zzzVigBossDamage`（10 個 EPModifyValue，Boss 全域傷害調整）
- `zzzCHPerkResistDaedra1H` / `zzzCHcrResistNPC75`（對特定陣營減傷）
- `zzzCHPerkBowIgnoreArmor40`（無視 40% 護甲）

**這是好消息：VIGILANT 的 perk 體系全是 ModForge 可純 record 重現的 EPModifyValue/Conditions 模式**，無 entry-point 腳本依賴。

---

## 6. ModForge 對照（純 record 可做 vs 需 Papyrus 缺口）

ModForge spec 欄位（已查證源碼）：`SpellSpec` / `MagicEffectSpec`(含 `Archetype` + `Association` + `Projectile`/`Explosion`/`CastingArt`/`HitEffectArt`) / `EnchantmentSpec` / `PerkSpec`(EntryPoint effects + Conditions) / `ProjectileSpec` / `ExplosionSpec`(含 `ObjectEffect`) / `HazardSpec` / `ScrollSpec` / `ShoutSpec`+`WordOfPowerSpec`。

| VIGILANT 技法 | ModForge 今天能否 | 說明 |
|---|---|---|
| **多階法術（同 MGEF、換 magnitude）** | ✅ 純 record | 每階一個 SpellSpec、effects 共用 MGEF editorId、改 mag。可直接照抄吸血火球/秩序矛模式 |
| **ValueModifier/DualValueModifier 傷害治療** | ✅ 純 record | MagicEffectSpec archetype + ActorValue（DualValueModifier 需確認 ModForge 是否暴露第二 AV，見缺口） |
| **Aimed 投射 VFX 鏈 EXPL←PROJ←MGEF←SPEL** | ✅ 純 record | ModForge 核心能力；觸手/雷箭那類 speed/gravity/explosion 鏈完全可建 |
| **EXPL 掛範圍 MGEF（`ObjectEffect`）** | ✅ 純 record | `ExplosionSpec.ObjectEffect` 對應雷箭爆炸的 AoE |
| **SpawnHazard MGEF + HAZD 週期施放** | ✅ 純 record | `MagicEffectSpec.Archetype=SpawnHazard` + `Association`→HazardSpec；HAZD.`Spell` 週期 + `TargetInterval` |
| **SummonCreature（76 個）** | ✅ 純 record | `archetype=SummonCreature` + `Association`→NPC FormKey（召喚對象須存在） |
| **Cloak 護體（近身觸發 SPEL）** | ✅ 純 record | `archetype=Cloak` + `Association`→觸發 SPEL（聖焰護體） |
| **Light 常駐發光附魔** | ✅ 純 record | `archetype=Light` + `Association`→LIGT；ModForge 已建 LIGT |
| **Absorb / Stagger / Banish / DetectLife** | ✅ 純 record | 皆 archetype-only，無 Association 或腳本 |
| **ENCH 武器/穿戴附魔（雙效、混 vanilla MGEF）** | ✅ 純 record | EnchantmentSpec + effects；effect 可引 vanilla MGEF FormKey |
| **Perk（101 個 EPModifyValue + 抗性/陣營條件）** | ✅ 純 record | **全部可重現**——PerkSpec entry-point + Conditions，無腳本 |
| **Shout（換 spell、復用 vanilla 字根）** | ✅ 純 record | ShoutSpec.Words 三段；word 引 vanilla WOOP、spell 自建 |
| **archetype=Script MGEF（260 個！）** | ❌ **需 Papyrus** | `MagicEffectSpec` **無 VMAD/script 欄位**；蓄力/環擊/落雨/拖尾全靠 `CHSpM*`/`Vig*` 腳本 |
| **Bound 武器（BoundBowEffectScript）** | ⚠️ 部分 | archetype/Association 可建，但實機綁腳本管裝備生命週期 |
| **SlowTime / imagespace 演出（magicImodScript）** | ⚠️ 部分 | archetype=SlowTime 可建，但 imod 套用靠腳本 |
| **HAZD 由 Papyrus `PlaceAtMe` 投放（非 SpawnHazard）** | ⚠️ 部分 | HAZD record 可建，但 8 個中多數靠 `magicPlaceActivatorScript` 投放 |
| **拖尾/連發/十字弓式發射等 VFX 程序** | ❌ 需 Papyrus | `VigUniqueTrailEffectScript`/`CHSpMShootSpellCrossbowStyleScript` 等 |

### 一句話結論

VIGILANT 魔法的 **record 骨架（SPEL/MGEF 非 Script 原型/ENCH/PERK/PROJ/EXPL/HAZD/SHOU）ModForge 今天幾乎全可純 record 重現**——尤其 **101/102 perk、所有 SummonCreature/Cloak/Light/SpawnHazard/Absorb 原型、整條投射 VFX 鏈、多階法術階梯**。**唯一系統性缺口是 `archetype=Script` 的 260 個 MGEF（佔 47%）與 239 個帶 VMAD 的 MGEF**：那是 Boss/獨特法術的程序化演出（蓄力、環形彈幕、落雨、拖尾、SlowTime imod），需要 Papyrus，ModForge 目前 MagicEffectSpec 無 script 欄位。

### 值得照抄的模式

1. **多階法術階梯**：一個 MGEF + N 個 SpellSpec（遞增 magnitude/cost），最省力的難度分級。
2. **EXPL.ObjectEffect 掛範圍 MGEF**：投射命中後在爆點施放 AoE 效果（雷箭模式）。
3. **HAZD 週期 spell + TargetInterval**：地刺/屏障/風暴用 hazard 每 0.3s 對範圍施放，配 `InheritDurationFromSpawnSpell`。
4. **混引 vanilla MGEF**：附魔/雜兵法術第二 effect 直接引 vanilla 減速/毒 FormKey，省做自家 MGEF。
5. **純 record 的 EPModifyValue perk + 陣營/種族 Conditions**：抗 Daedra、無視護甲、Boss 傷害調整——全用條件式數值修改，不碰腳本。

### 建議的 ModForge 後續（若要逼近 VIGILANT 表現力）

- **MagicEffectSpec 加 VMAD/script 欄位**（引用 user `.psc`，比照現有 dialogue/quest fragment 機制）——這是解鎖那 47% Script 原型的唯一途徑。
- 確認 **DualValueModifier 的第二 AV** 是否已由 `SecondActorValue` 暴露（78 個 MGEF 用到）。
- 既有可複用 trigger 庫（magic-effect 入口）已能讓「施法→事件」走腳本，但那是**派發**而非**法術本身的演出**；兩者不同。
