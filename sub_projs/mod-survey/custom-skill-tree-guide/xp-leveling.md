# Step 5：XP / 升級 / 訓練接線

← [custom-skill-tree-guide](README.md)

## 7. Step 5 — XP / 升級 / 訓練接線

三件事：**XP 怎麼進、等級怎麼初始化、訓練怎麼接**。v3 API 把這些都包好了，不必再自寫 VIGILANT 那種 `zVCSF*` kill-actor 管理腳本。

### 7.1 Papyrus API（`CustomSkills.psc`，v3）

全部 `global native`：

```papyrus
int  GetAPIVersion()                                            ; 目前回傳 3
void OpenCustomSkillMenu(string asSkillId)                      ; 開某技能/群組選單
void ShowTrainingMenu(string asSkillId, int aiMaxLevel, Actor akTrainer)
void AdvanceSkill(string asSkillId, float afMagnitude)          ; 依使用量推進技能
void IncrementSkill(string asSkillId)                           ; +1 級
void IncrementSkillBy(string asSkillId, int aiCount)
string GetSkillName(string asSkillId)
int  GetSkillLevel(string asSkillId)
void ShowSkillIncreaseMessage(string asSkillId, int aiSkillLevel)
void DebugReload()                                             ; debug only
```

額外 extension scripts（v3）讓任意 script/alias/magic-effect 監聽事件：
- `CustomSkills_FormExt.psc`：`RegisterForCustomSkillIncrease(Form)` + `Event OnCustomSkillIncrease(string asSkillId)`；`RegisterForCustomSkillBookRead(...)` + `Event OnCustomSkillBookRead(...)`。

### 7.2 init alias script（仿 `CNS_InitScript`）

在 esp 裡建一個 quest（start game enabled），加一個指向 PlayerRef 的 ReferenceAlias，掛上這支 script。它在**首次安裝**時把 level GLOB 設成 `iAVDSkillStart`（=15）、授予基礎 auto-perk，並用版本號 gate（避免每次載入重設）：

```papyrus
Scriptname BL_InitScript extends ReferenceAlias

Perk Property BL_BeastLore_AutoPerk Auto       ; 安裝即給的基礎被動

GlobalVariable Property SkillBeastLoreLevel Auto

int Property CurrentVersion = 1 AutoReadOnly
int KnownVersion = 0

Event OnInit()
    RegisterForSingleUpdate(1.0)
EndEvent

Event OnPlayerLoadGame()
    if KnownVersion != CurrentVersion
        DoUpdate()
    endif
EndEvent

Event OnUpdate()
    DoUpdate()
EndEvent

Function DoUpdate()
    Actor actorRef = self.GetActorReference()
    if KnownVersion < 1
        DoNewInstall()
        actorRef.AddPerk(BL_BeastLore_AutoPerk)
    endif
    KnownVersion = CurrentVersion
EndFunction

Function DoNewInstall()
    int startingLevel = Game.GetGameSettingInt("iAVDSkillStart")   ; = 15
    SkillBeastLoreLevel.SetValue(startingLevel)
EndFunction
```

這就是 `CNS_InitScript` 的精簡版（原檔處理三棵樹 + 四個 auto-perk，結構一模一樣）。`CurrentVersion`/`KnownVersion` 是安裝/升級 gate：日後改版只要 bump `CurrentVersion`，`OnPlayerLoadGame` 就會跑一次 `DoUpdate`。

> 一個慣例做法：再建一個 `extends Quest` 的「ModObjects」純屬性容器 quest（仿 `CNS_ModObjects`），把樹裡的具名 perk 接出來給其他腳本/條件引用——非必要，但讓 form 管理乾淨。

### 7.3 訓練師（一行 TIF fragment）

把現有 NPC 重新利用成訓練師最省事：在那個 NPC 的對話 TopicInfo 上掛一個 fragment，body 只要一行（這就是 `CNS_TIF__TrainingEnthir` 的全部 code）：

```papyrus
;BEGIN CODE
CustomSkills.ShowTrainingMenu("BeastLore", 90, akSpeaker)
;END CODE
```

`ShowTrainingMenu(skillId, maxLevel, trainer)`：第二參數是該訓練師的等級上限（**50/75/90 對應 adept/expert/master**）。CK 對話編輯器會自動生成 fragment 的外殼（`Function Fragment_0(ObjectReference akSpeakerRef)` + `Actor akSpeaker = akSpeakerRef as Actor`），你只填中間那一行。

### 7.4 XP 三路怎麼進

- **用量**：`CustomSkillAdvance_BeastLore` keyword 掛在 perk 的 Modify-Skill-Use entry-point，玩家用相關動作就漲。
- **訓練**：上面的 `ShowTrainingMenu`（花錢）。
- **任意腳本**：直接 `CustomSkills.AdvanceSkill("BeastLore", mag)` 或 `IncrementSkill("BeastLore")`。
- **附魔/藥水（Fortify 技能）**：**非要 SKSE 不可**——得走 4.4 的 fortify MGEF + `ActorValueData/*.toml` + 自寫 native plugin。純 esp/JSON 做不到。

CSF runtime 用 `ratio`/`legendary` GLOB 與 `experienceFormula` 自己算等級與 perk 點數；perk 授予走原版 perk-tree 花點數 UI；perk 效果全是 esp 裡的普通 PERK record。

---

