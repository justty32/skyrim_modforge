# Enchantments and Potions Work for NPCs (EPW4NPCs) 調查 Finding

- 版本：1.0.2（本地解壓：`~/skyrim_mods/unzip/EPW4NPCs/`，來源 `hdd/Enchantments and Potions Work for NPCs - EPW4NPCs-37607-1-0-2-1669579454.7z`）
- Nexus：[37607](https://www.nexusmods.com/skyrimspecialedition/mods/37607)
- 類型：純 **SPID `_DISTR.ini` config**（無 plugin、無 DLL、無 script）

---

## 1. 分類

| 項目 | 結論 |
|---|---|
| 類型 | 框架型（distribution config），實質是 2 行 SPID 設定 |
| 是否有 plugin | **無**——整包**只有一個檔案** `EPW4NPCs_DISTR.ini`（已 `7z x` 驗證，`find . -type f` 只有這一個） |
| SKSE / SPID 依賴 | **依賴 SPID**（`po3_SpellPerkItemDistributor.dll`），SPID 又依賴 SKSE。無 PapyrusUtil / 其他 |
| Papyrus | **無**（不附 `.pex` / `.psc`） |
| 敘事價值 | 無 |
| 系統價值 | 低（機制本身極簡，但「把 vanilla EntryPoint perk SPID 廣播給全 NPC」這個 pattern 對 ModForge 有對照價值） |

---

## 2. 做什麼

讓 NPC 也能享有玩家本來才有的兩類被動效果：

1. **附魔裝備對 NPC 生效**——原本 NPC 只受約一半的裝備附魔影響（ini 註解原文「They were only affected by about half the enchantments, before.」），補上後 NPC 穿戴的附魔護甲/武器效果能完整作用。
2. **技能增益藥水對 NPC 生效**——技能提升類藥水（Fortify 系）原本對 NPC「no effect」，補上後 NPC 喝下會真的有效。

注意：它**不是**讓 NPC「主動喝藥水」的 AI 控制器——那是另一支重量級 mod `NPCsUsePotions`（見下）。EPW4NPCs 只是讓**已生效的**藥水/附魔在 NPC 身上不被引擎吃掉。

---

## 3. 機制（ground truth：唯一檔案 `EPW4NPCs_DISTR.ini`）

整個 mod 的全部內容就是兩行 SPID 分發：

```ini
;PerkSkillBoosts
Perk = 000CF788~Skyrim.esm|ActorTypeNPC

;AlchemySkillBoosts
Perk = 000A725C~Skyrim.esm|ActorTypeNPC
```

機制＝**把兩個 vanilla Skyrim.esm EntryPoint perk，透過 SPID 廣播給帶 `ActorTypeNPC` keyword 的全體 NPC actorbase**。`ActorTypeNPC` 是 SPID 的 StringFilter（第 2 欄），命中所有人型 NPC。

兩個 perk 已用 ModForge CLI `perkdiag` 對 vanilla `Skyrim.esm` 驗證（EditorID 與 ini 註解完全吻合）：

| FormID | EditorID | 性質 |
|---|---|---|
| `0x0CF788` | **PerkSkillBoosts** | 18 條 `entryPoint` effect，全是 `PerkEntryPointModifyActorValue`，含 `ModAlchemyEffectiveness` / `ModSpellCost` / `ModAttackDamage` / `ModIncomingDamage` / `ModPercentBlocked` …（穿戴附魔的各種 AV 修正掛在這裡） |
| `0x0A725C` | **AlchemySkillBoosts** | 18 條 `entryPoint`，含 `ModAlchemyEffectiveness` / `ModSpellDuration` / `ModSpellMagnitude` / `ModAttackDamage` …（藥水/煉金強度修正） |

關鍵理解：這兩個 perk 是 vanilla 用來「讓附魔與藥水數值真的套到角色身上」的隱形載體 perk，玩家天生持有，但**多數 NPC actorbase 沒有掛**，所以引擎在 NPC 身上跳過了那些 entry-point 修正。EPW4NPCs 沒有自建任何 record，純粹**把 vanilla 的這兩個 perk 補發給 NPC**，補回缺失的 entry-point 通路。

**分發方式**：純 SPID，**無 player-ability + quest-alias scan、無 global ability、無 script**。SPID 啟動時掃 `Data/*_DISTR.ini`，依 filter 直接 attach 到 NPC actorbase，零 ESP patch。

> 兼容性副作用（UNVERIFIED，僅 SPID 機制推論）：對全 NPC 加 entry-point perk 不改任何記錄、可疊裝、移除即還原，衝突風險極低。實機影響未測。

---

## 4. 對照：`NPCsUsePotions`（67489，僅快速比對，未深挖）

使用者點名的姊妹 mod。`7z l` 列出檔案類型即可看出量級天差地別：它附 **原生 SKSE `NPCsUsePotions.dll`（SE/VR 各一）＋ 一個 `NPCsUsePotions.esp`＋多支 Papyrus（`NPCsUsePotions_Potions.psc` / `_Poisons.psc` / `AnimatedPotionsScript.psc`）＋ 一大批 `*_NUP_DIST.ini`（SPID）＋ FOMOD 選項**（含 SofiaFollower、各 encounter pack 的相容 ini）。

那才是真正的「**NPC 主動喝藥水/抹毒的戰鬥 AI 控制器**」（DLL + 腳本決策何時喝哪瓶）。EPW4NPCs 與它正交、互補：EPW4NPCs 確保藥水**生效**，NPCsUsePotions 讓 NPC**去喝**。本 finding 聚焦 EPW4NPCs；NPCsUsePotions 若要做需另開深挖（原生 DLL 的戰鬥決策不可生成，但其 SPID 分藥水池 + faction 條件可借鏡）。

---

## 5. 對 ModForge 的相關性（缺口已對 `src/` 驗證，非推斷）

**結論：EPW4NPCs 今天就能被 ModForge 100% 等價生成，無任何新功能缺口。**

它需要的兩件事 ModForge 都已具備：

1. **SPID `Perk` 分發 + `ActorTypeNPC` StringFilter** — ModForge 已有 `SpidGen`（`src/ModForge.Core/SpidGen.cs`）+ `SpidDistributionSpec`（`Spec.SpidDistribution.cs`）。`Type` 明列支援 `Perk`，`StringFilters` 對應 SPID 第 2 欄。一條 spec entry：
   - `Type=Perk, Record="000CF788~Skyrim.esm", StringFilters=["ActorTypeNPC"]`
   - 經 `SpidGen.Line` 會輸出 `Perk = 000CF788~Skyrim.esm|ActorTypeNPC`（已讀 code 確認 NONE-trim 行為：trailing 欄位全 trim 掉，只剩 `Record|StringFilter`）——與本 mod 逐字節同形。
2. **指向 vanilla perk** — 本 mod 分發的是 **Skyrim.esm 既有 perk**，ModForge 不需建立任何 PERK record，只需在 spec 引用 `0xFormID~Skyrim.esm`。SPID 語法允許 Skyrim/DLC 省略 `~plugin`，但帶上更穩。

**順帶驗證（非本 mod 必需，但證明 ModForge 連「自建同類 perk」都辦得到）**：ModForge 已支援 EntryPoint perk 生成——`Generator.Build.Perks.EntryPoints.cs` + `Spec.Perks.cs`（`Kind="entryPoint"`，`EntryPoint` 欄填 EntryType 名如 `ModAttackDamage`）。亦即如果哪天要做「自訂的 NPC 附魔/藥水修正 perk」，ModForge 也能從零建 PERK，再 SPID 廣播。**未發現任何「ModForge 不能做」之處。**

---

## 6. Roadmap 意涵

1. **可複用 pattern：「把一個 perk/spell SPID 廣播給全 NPC」**——`Perk|ActorTypeNPC`（或 `Spell|ActorTypeNPC`）是極高槓桿的零衝突全域注入手法。值得在 ModForge 文件/範例裡收一個「**global NPC ability/perk via SPID**」的 recipe（spec → `_DISTR.ini`），EPW4NPCs 是最小活範例。這條對 vendor / `settlements:` 系列也有用：要給某聚落或某 faction 的全體 NPC 掛一個被動能力時，SPID `StringFilters`/`FormFilters`（Faction/Keyword）就是無 patch 的分發層。
2. **無新缺口**——本 mod 不浮現任何 record/生成缺口，純落在已 landed 的 SPID 輸出能力域內。
3. **延伸候選（來自比對而非本 mod）**：真正的缺口在姊妹 mod `NPCsUsePotions` 那種「**戰鬥中主動喝藥水的 AI controller**」——那需要原生 DLL 或重 Papyrus 戰鬥決策，**不可純生成**。若 roadmap 要做「會用藥水/招式的戰鬥 NPC」，須走 shell-out 預建組件或附帶 controller 腳本，與 action-system（SCAR「NPC 連段 AI 不可生成」）同一類限制。EPW4NPCs 本身不踩這條線。
