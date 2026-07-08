# Constellations 參考實作：SKSE/Papyrus 接線 + 在地化 + 意義修正

← [custom-skills-framework](README.md)

### 6.3 SKSE plugin + Papyrus 接線

**三個層次協作**：

**(1) `Constellations.dll`（mod 自己的 SKSE plugin，≠ `CustomSkills.dll`）**——這是本 mod 私有的 native plugin，與框架的 `CustomSkills.dll` 分開（後者是依賴、需玩家另裝）。從 ship 的 `ActorValueData/Constellations_AVG.toml` 可推斷它的職責：把自訂技能接上**附魔/藥水的「Fortify <Skill>」機制**。`Constellations_AVG.toml`：

```toml
[HandToHandEnchantments]
type = "Adaptive"
alias = "OneHandedSkillAdvance"
[HandToHandPotions]
type = "Adaptive"
alias = "TwoHandedSkillAdvance"
[SorceryEnchantments]
type  = "Adaptive"
alias = "ConjurationSkillAdvance"
# …
[Include]
"ConstellationsNewSkills.esp" = ["HandToHandEnchantments", "HandToHandPotions", ...]
```

`ActorValueData/` 是 `Constellations.dll` 讀的設定：它讓自訂技能**借用一個閒置的原版 ActorValue 槽**（`OneHandedSkillAdvance`/`TwoHandedSkillAdvance`/`ConjurationSkillAdvance`/`EnchantingSkillAdvance` 這些 *SkillAdvance AV）當「載體」，使原版的 Fortify-Skill 附魔/煉金框架能對自訂技能生效（原版引擎只認得固定的 AV 列舉，自訂技能沒有自己的 AV）。`type = "Adaptive"` 即「動態適配到那個 AV」。對應地，esp 內有整組 `CNS_FortifySkillHandToHand01..04` / `…Sorcery…` / `…Athletics…` **MGEF**（共 24 個 magic effect，附魔/藥水裝備時把對應的 `Skill<X>Level` GLOB 拉高）。**這層是 Constellations 私有的加值，不是 CSF 框架本身的一部分。**

**(2) `CNS_InitScript` / `CNS_ModObjects`（Papyrus，掛在 esp 的 quest/alias）**：
- `CNS_InitScript`（`extends ReferenceAlias`，掛在玩家身上）：玩家首次安裝/載入存檔時，把三個 `Skill<X>Level` GLOB **初始化成 `Game.GetGameSettingInt("iAVDSkillStart")`**（即原版技能起始值 15），並 `AddPerk` 幾個「自動 perk」（`CNS_H2H_AutoPerk` 等基礎被動）。用 `CurrentVersion`/`KnownVersion` 做安裝/升級 gate。
- `CNS_ModObjects`（`extends Quest`）：純屬性容器 quest，把樹裡的具名 perk（`UnarmedTemper`/`SprintEvade`/`StaffReflect`…）和 `StaffEnchantmentList` 等 form 接出來給其他腳本/條件用。

**(3) Keyword 掛鉤（esp 內，命名完全照 §2.3 慣例）**——`strings` 確認 esp 內含：
- `CustomSkillAdvance_HandToHand` / `_Athletics` / `_Sorcery`：貼在 perk 的「Modify Skill Use」entry-point 上，調整對應技能的升級速度。
- `CustomSkillBook_HandToHand` / `_Athletics` / `_Sorcery`：技能書 keyword，首讀推進對應技能。
- （**沒有** `CustomSkillWorkbench_*`——這三棵技能沒走製作台 XP 路線。）

**(4) 訓練（trainer）= TIF fragment 直呼 API**：七支 `CNS_TIF__Training<NPC>.psc`（Durak/Enthir/Erandur/Mauhulakh/NjadaStonearm/VipirtheFleet/Wylandriah）都是對話 TopicInfo fragment，body 只有一行：

```papyrus
CustomSkills.ShowTrainingMenu("Sorcery", 90, akSpeaker)    ; Enthir = 大師級 Sorcery 訓練師
```

把現有原版 NPC 重新利用成自訂技能的訓練師：`ShowTrainingMenu(skillId, maxLevel, trainer)` 是 v3 API，第二參數是該訓練師的等級上限（50/75/90 對應 adept/expert/master）。**這就是「XP 增益 / perk 授予 / 訓練」三件事的完整接線：XP 由 keyword（用量）+ MGEF（附魔藥水）+ 訓練選單（花錢）三路推進 `Skill<X>Level` GLOB；perk 授予走原版 perk-tree 花點數 UI（CSF runtime 用 `ratio`/`legendary` GLOB 與 `experienceFormula` 算等級與點數）；perk 效果全是 esp 內普通 PERK record。**

### 6.4 在地化

走 §2.4 的「現代 JSON」路線，**一個 mod 一份** `Interface/Translations/ConstellationsNewSkills_<LANG>.txt`（UTF-16 LE BOM、tab 分隔、key↔value）。本機只 ship 了 `_ENGLISH.txt`，內容就是把 JSON 裡的 `$`-key 對應到真文字：

```
$Athletics_Name	Athletics
$HandToHand_Name	Hand-to-hand
$Sorcery_Name	Sorcery
$Athletics_Description	Those trained in athletics are able to run faster and maintain their stamina longer.
$HandToHand_Description	The art of hand-to-hand combat. …
$Sorcery_Description	The art of drawing power from magical objects such as staves and scrolls. …
```

翻譯 = 多放一份 `ConstellationsNewSkills_CHINESE.txt`（同 key、換 value），無 ENGLISH fallback。**perk 的名字/描述**仍是 esp 內 PERK record 的 FULL/DESC（與 VIGILANT 同），翻譯需另出翻譯版 esp 或 STRINGS——技能名/述（JSON 那層）和 perk 名/述（esp 那層）是**兩套獨立的在地化通道**，這點與舊 INf 案例完全一致。

### 6.5 更新「對 ModForge 的意義」（看過真 schema 後的修正）

§5 的結論大方向成立，看過 Constellations 後**收斂出更精準的最小產物與分工**：

**ModForge 一個「CSF 技能」generator 該 emit 的東西：**

1. **esp records（既有能力可重用）**
   - **PERK**：樹的全部節點 perk（多階鏈只在 node 填第一階）——普通 PERK record，沿用既有 perk 生成（`PerkConditionTabCount` CTD 陷阱仍適用）。
   - **GLOB**：**實證最小只需 `level` + `ratio` + `legendary` 三個 per skill**（Constellations 連 `showMenu`/`showLevelup`/`perkPoints`/`color`/`debugReload` 都沒做也照常運作——後面這些是「獨立選單群組 + console 開選單 + 自訂點數池」才需要的可選件）。GLOB 要給 editor id。
   - **KYWD**：`CustomSkillAdvance_<Id>`（掛 perk Modify-Skill-Use entry-point，控升級速度）、`CustomSkillBook_<Id>`（技能書）、可選 `CustomSkillWorkbench_<Id>`（製作台）。
   - **MGEF**（**新發現的可選件**）：若要支援「Fortify <技能>」附魔/藥水，得做一組 fortify-skill MGEF + 一份 `ActorValueData/<Mod>_AVG.toml` 把自訂技能映射到閒置原版 *SkillAdvance AV。**這條需要一個 native SKSE plugin（Constellations 自己的 `.dll`）**——ModForge 純 esp 做不到，屬「進階加值」，預設可不做。

2. **JSON 設定**
   - 若要「住進原版技能頁」（最像原生）→ 產 `SKSE/Plugins/CustomSkills/SKILLS.json`：root 帶 `version:1` + `skydome` + `skills[]`，把 20 個原版技能字串與自訂技能 `{ "$ref": "<Mod>/<Skill>.json" }` 混排；各技能樹獨立存成 `CustomSkills/<Mod>/<Skill>.json`。
   - 若只要一棵「另開選單」的獨立技能 → 產具名 `<Id>.json`（同 skill schema），靠 `OpenCustomSkillMenu` 開。
   - skill JSON 欄位：`id`/`name`(`$`-key)/`description`/`level`/`ratio`/`legendary` 指向上面 GLOB 的 `"<Mod>.esp|FormId"`、`experienceFormula`（五參數旋鈕）、`nodes[]`（`id`/`perk`/`x`/`y`/`links`，浮點佈局、無 GridX/Y）。`form` 字串 load-order 無關，與 ModForge FormId 配置天然契合。

3. **在地化**：`$`-key + `Interface/Translations/<Mod>_ENGLISH.txt`（UTF-16 LE BOM、tab 分隔）；perk 名/述另循 esp STRINGS/inline。

4. **接線腳本（可選，視野心）**
   - **訓練師**：最省事——一支 TopicInfo TIF fragment 一行 `CustomSkills.ShowTrainingMenu(id, maxLevel, trainer)` 即可（無需自寫管理 quest）。
   - **初始化**：一支 alias script（仿 `CNS_InitScript`）在 OnInit/OnPlayerLoadGame 把 `level` GLOB 設成 `iAVDSkillStart`、授予基礎 auto-perk、做版本 gate。
   - XP 推進靠 keyword（用量）+ 訓練選單（花錢），或任意腳本呼叫 `CustomSkills.AdvanceSkill(id, mag)` / `IncrementSkill(id)`（v3 API）。**這層不必再自寫 VIGILANT 那種 `zVCSF*` kill-actor 管理腳本**——v3 API 已把 advance/training/getlevel 都包好。

**一句話分工**：**純 esp + JSON（+ 幾支薄 Papyrus）就能做出一棵接進原版技能頁的完整自訂技能；只有「Fortify-技能附魔/藥水」這條需要額外的 native SKSE plugin（`ActorValueData` + fortify MGEF）。** Constellations 的價值在於它把「最簡可行（JSON + 三個 GLOB + keyword + 訓練 TIF）」和「進階加值（私有 dll + AVG + fortify MGEF）」這兩層清楚地分了開來，正好界定了 ModForge generator 的 MVP 邊界與可選擴充。
