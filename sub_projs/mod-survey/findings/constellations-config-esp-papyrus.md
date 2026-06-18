# Config：esp record / toml / Papyrus / 在地化

← [constellations](constellations.md)

### 2.2 esp Record 需求

每棵自訂技能樹需要的 esp record：

| Record 類型 | 數量（per 技能） | 說明 |
|-------------|----------------|------|
| **PERK** | 節點數 × 階數 | 樹的全部節點；多階 perk node 只填第一階，後續靠 Next Perk 鏈 |
| **GLOB** | 3（最小） | `level` / `ratio` / `legendary`；需有 editor id |
| **KYWD** | 2–3 | `CustomSkillAdvance_<Id>`（掛 perk Modify-Skill-Use）、`CustomSkillBook_<Id>`（技能書）；可選 `CustomSkillWorkbench_<Id>` |
| **MGEF** | 可選（4+ per 技能） | 「Fortify 技能」附魔/藥水效果；需搭配私有 SKSE plugin |
| **QUST** | 1（全域共享） | init script 所在 quest（start game enabled）；可加 ModObjects 容器 quest |

Constellations 三棵技能的 GLOB FormId 分配：

```
HandToHand: level=00F / ratio=010 / legendary=011
Athletics:  level=012 / ratio=013 / legendary=014
Sorcery:    level=015 / ratio=016 / legendary=017
```

### 2.3 `ActorValueData/*.toml`（私有 Fortify 映射）

`Constellations_AVG.toml` 供 `Constellations.dll` 讀取，讓自訂技能借用閒置的原版 ActorValue 槽：

```toml
[HandToHandEnchantments]
type = "Adaptive"
alias = "OneHandedSkillAdvance"

[HandToHandPotions]
type = "Adaptive"
alias = "TwoHandedSkillAdvance"

[SorceryEnchantments]
type = "Adaptive"
alias = "ConjurationSkillAdvance"

[SorceryPotions]
type = "Adaptive"
alias = "EnchantingSkillAdvance"

[Include]
"ConstellationsNewSkills.esp" = ["HandToHandEnchantments", "HandToHandPotions", "SorceryEnchantments", "SorceryPotions"]
```

> ⚠️ 這個 `.toml` 是 **Constellations 私有的 SKSE plugin（`Constellations.dll`）** 讀的，不是 CSF 本身的功能。純 esp 做不到 Fortify 附魔/藥水——需要自寫 native SKSE plugin。

### 2.4 Papyrus 接線

**`CNS_InitScript.psc`**（`extends ReferenceAlias`，掛在 PlayerRef 身上）：

```papyrus
Scriptname CNS_InitScript extends ReferenceAlias

Perk Property CNS_AlchemyEffects Auto
Perk Property CNS_H2H_AutoPerk Auto
GlobalVariable Property SkillAthleticsLevel Auto
GlobalVariable Property SkillHandToHandLevel Auto
GlobalVariable Property SkillSorceryLevel Auto

int Property CurrentVersion = 1 AutoReadOnly
int KnownVersion = 0

Event OnInit()
    RegisterForSingleUpdate(1.0)
EndEvent

Event OnPlayerLoadGame()
    if KnownVersion != CurrentVersion : DoUpdate()
EndEvent

Function DoNewInstall()
    int startingLevel = Game.GetGameSettingInt("iAVDSkillStart")   ; = 15
    SkillAthleticsLevel.SetValue(startingLevel)
    SkillHandToHandLevel.SetValue(startingLevel)
    SkillSorceryLevel.SetValue(startingLevel)
EndFunction
```

**`CNS_TIF__Training*.psc`**（七支，各一行）：

```papyrus
CustomSkills.ShowTrainingMenu("Sorcery", 90, akSpeaker)
; 訓練師等級上限 50/75/90 = adept/expert/master
```

### 2.5 在地化

- `Interface/Translations/ConstellationsNewSkills_ENGLISH.txt`：UTF-16 LE BOM、tab 分隔、`$key\tValue`。
- JSON 的 `$`-key 對應技能名/述；perk 的名字/描述走 esp 的 PERK record FULL/DESC（兩套獨立通道）。
- 翻譯 = 多放 `_CHINESE.txt`（同 key，換 value）；無 ENGLISH fallback。

---

