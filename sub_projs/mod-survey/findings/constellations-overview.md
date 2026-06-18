# Constellations 做什麼 + 架構概覽

← [constellations](constellations.md)

## 一、Constellations 做什麼 ＋ 架構概覽

**Constellations - Additional Player Skills** 是 Custom Skills Framework（CSF）的作者 Exit-9B **親自掛保證的「現代 JSON 格式」示範 mod**，同時也是一個功能完整的玩家 mod：它把 Skyrim 原版的 18 個技能擴充成 **21 個**，新增三棵自訂技能樹：

| 技能 | id | 主題 |
|------|-----|------|
| **Hand-to-Hand（徒手）** | `HandtoHand` | 拳擊、武徒手格鬥 |
| **Athletics（運動）** | `Athletics` | 衝刺、耐力、體能 |
| **Sorcery（法術器具）** | `Sorcery` | 法杖、卷軸、魔法道具 |

三棵新樹**直接出現在原版的 ESC → Skills 頁面**，不需另開選單，對玩家完全無感接軌。

### 架構組成

```
ConstellationsNewSkills.esp               ← PERK / GLOB / KYWD / MGEF 全在這
SKSE/Plugins/Constellations.dll           ← mod 私有 SKSE plugin（讀 AVG.toml，處理 Fortify 機制）
SKSE/Plugins/CustomSkills/SKILLS.json     ← 特例：覆寫原版技能選單，把三棵新樹插進去
SKSE/Plugins/CustomSkills/Constellations/HandToHand.json
SKSE/Plugins/CustomSkills/Constellations/Athletics.json
SKSE/Plugins/CustomSkills/Constellations/Sorcery.json
SKSE/Plugins/ActorValueData/Constellations_AVG.toml  ← Fortify-Skill 附魔/藥水映射
Interface/Translations/ConstellationsNewSkills_ENGLISH.txt
Source/Scripts/CNS_InitScript.psc         ← 初始化（繼承 ReferenceAlias）
Source/Scripts/CNS_ModObjects.psc         ← 屬性容器 Quest
Source/Scripts/CNS_TIF__Training*.psc     ← 七支訓練師 TIF fragment
Meshes/Constellations/Interface/INTPerkSkydome.nif   ← 自訂星圖（21 技能版）
Textures/Constellations/...               ← 技能樹貼圖
Meshes/Constellations/Apocrypha/          ← Apocrypha perk 重置祭壇 NIF
Sound/Voice/ConstellationsNewSkills.esp/  ← 訓練師台詞音效（.fuz）
Seq/ConstellationsNewSkills.seq           ← 對話 .seq（讓現有存檔的對話正確觸發）
```

**核心依賴**：Custom Skills Framework（CSF，Nexus 41780）的 `CustomSkills.dll` + `CustomSkills.psc`（玩家須另裝）。Constellations 本身的 `Constellations.dll` 是私有擴充（只負責 Fortify 附魔 / 藥水機制），而 CSF `CustomSkills.dll` 才是「選單外殼 + XP / 升級引擎」的提供者。

---

