# Step 6–7：在地化 + 測試 + 用 ModForge 生成 + Checklist

← [custom-skill-tree-guide](README.md)

## 8. Step 6 — 在地化

三層**互相獨立**的在地化通道：

| 在地化什麼 | 在哪改 | 怎麼做 |
|------------|--------|--------|
| **技能名/述**（JSON 的 `name`/`description`） | `Data/Interface/Translations/<Plugin>_<LANG>.txt` | `$`-key 對應真文字；換語言檔即翻譯 |
| **perk 名/述**（PERK 的 FULL/DESC） | esp 的 STRINGS 或 inline 文字 | 出一份翻譯版 esp / STRINGS |
| **UI 字串** | 同 Translations 機制（與 MCM 同套） | — |

**Translations 檔格式**：`Data/Interface/Translations/MySkills_ENGLISH.txt`，**UTF-16 LE + BOM、tab 分隔、key↔value**，語言後綴 `ENGLISH`/`CHINESE`/…。**無 fallback 到 ENGLISH**——玩家須備妥對應語言檔。範例（抄 Constellations 的格式）：

```
$BeastLore_Name	Beast Lore
$BeastLore_Description	The study of beasts: how to track them, endure them, and outlast them.
```

翻譯 = 多放一份 `MySkills_CHINESE.txt`（同 key、換 value）。

> 注意一個分裂：**技能名/述（JSON 那層）翻譯靠換 Translations 檔；perk 名/述（esp 那層）翻譯靠換 esp**。兩套通道，VIGILANT 與 Constellations 都一致。一份完整中文化要兩個檔都換。

---

## 9. Step 7 — 測試（最小煙霧測試）

1. **檔案落位檢查**（在地化、JSON、esp 都到位）：
   - `Data/MySkills.esp` 啟用、排在 CSF 之後。
   - `Data/SKSE/Plugins/CustomSkills/SKILLS.json` 與 `MySkills/BeastLore.json` 存在、是合法 JSON（拿掉教學註解！）。
   - `Data/Interface/Translations/MySkills_ENGLISH.txt` 是 UTF-16 LE BOM。
2. **進遊戲、開新檔或乾淨存檔**，按 ESC → Skills，應該看到 Beast Lore 出現在你 `skills[]` 排的位置。技能名顯示正確 = Translations 接上了；顯示成 `$BeastLore_Name` = Translations 沒讀到（檢查編碼/檔名語言後綴）。
3. **console 驗證 GLOB**（GLOB 要有 editor id 才行）：
   - `getglobalvalue SkillBeastLoreLevel` 應為 15（init script 跑過了）。
   - `set SkillBeastLoreLevel to 50` 後重開選單，看等級變化、可點的 perk 數變化。
   - 若你做了 `showMenu` GLOB（路線 B）：`set BeastLoreShowMenu to 1` 直接開選單。
4. **點一個 perk**，看 esp 裡那個 PERK 的效果有沒有生效（entry-point 看數值、ability 看 active effects）。
5. **找訓練師對話**，確認 `ShowTrainingMenu` 跳出訓練界面。
6. **XP 推進**：`CustomSkills.IncrementSkill("BeastLore")` 或做相關動作，看 level 漲、升級訊息（若做了 `showLevelup`）。

> entry-point perk 一進選單就 CTD？回去看 4.1 的 `PerkConditionTabCount` 地雷。

---

## 10. 用 ModForge 生成

依 survey §5/§6.5 的 MVP 結論，把上面的工作分成「ModForge 現在能做」與「還缺的 generator」：

### 現在能生成（既有能力可重用）
- **PERK records**（Step 2.1）：技能樹全部節點 perk（含多階鏈）就是普通 PERK record，ModForge 既有 perk 支援直接適用（`PerkConditionTabCount` 地雷仍適用）。
- **GLOB records**（Step 2.2）：`level`/`ratio`/`legendary` 是簡單 GLOB，要給 editor id——ModForge 能產。
- **KYWD records**（Step 2.3）：`CustomSkillAdvance_<Id>` 等是普通 keyword record。

### 還缺的 generator
- **`<X>.json` 產生器**：把「樹形規格」序列化成 skill JSON。關鍵契合點——`form` 字串是 `"Plugin.esp|FormId"`、load-order 無關，ModForge 只要知道自己產出的 plugin 檔名 + 各 record 本地 FormId 就能填，**與既有 FormId 配置流程天然契合**。
- **`SKILLS.json` 組裝器**：把原版技能字串與 `{ "$ref": ... }` 混排成 root。
- **Translations 檔**（`$`-key + UTF-16 LE BOM）。

### 未來 spec 欄位構想（proposal，非現況）

一個可能的 ModForge spec 片段長相（**僅為後續實作參考，非目前已支援**）：

```jsonc
// PROPOSAL — 尚未實作
{
  "customSkill": {
    "id": "BeastLore",
    "name": "$BeastLore_Name",
    "description": "$BeastLore_Description",
    "experienceFormula": { "useMult": 0.8, "useOffset": 27.0, "improveMult": 2.0 },
    "menu": "SKILLS",                          // "SKILLS" → 併入原版頁；或具名 → 獨立群組
    "insertAfter": "Block",                    // SKILLS.json 排序提示
    "skydome": "DLC01/Interface/INTVampirePerkSkydome.nif",
    "nodes": [
      { "id": "Mastery", "perk": "BL_Mastery", "x": 0.0, "y": 0.0, "links": ["Tracking","Resilience"] },
      { "id": "Tracking", "perk": "BL_Tracking01", "x": -1.2, "y": 1.0, "links": ["Predator"] }
      // perk 用 EDID 引用既有 spec 裡的 PERK；GLOB/KYWD 由 generator 自動帶出
    ]
  }
}
```

ModForge generator 拿到這份 spec 後：自動建 level/ratio/legendary 三個 GLOB + 升級 KYWD、把 `perk` 的 EDID 解析成 `"Plugin.esp|FormId"` 寫進 JSON、emit `SKILLS.json` 與 Translations。**一句話分工**：純 esp + JSON（+ 幾支薄 Papyrus）就能做出一棵接進原版技能頁的完整技能；只有「Fortify-技能附魔/藥水」那條需要額外的 native SKSE plugin（`ActorValueData` + fortify MGEF），屬框架外的進階加值，不在 MVP。

---

## 11. 常見地雷 / Checklist

**Do**
- 鎖定 **v3 JSON** 格式，用 `CustomSkills.psc` v3 API（省去自寫管理 quest）。
- 每棵技能至少 `level`/`ratio`/`legendary` 三個 GLOB，**都給 editor id**。
- `id` 在 JSON、訓練 TIF、console、Papyrus 呼叫處**前後完全一致**（別學 Constellations 的 `HandtoHand`/`HandToHand` 不一致賭容錯）。
- 出貨 JSON 是**合法 JSON**：拿掉所有 `//` 教學註解。
- Translations 存成 **UTF-16 LE + BOM、tab 分隔**，檔名語言後綴正確（`_ENGLISH`）。
- init script 用 `CurrentVersion`/`KnownVersion` gate，首裝才設 level=`iAVDSkillStart`。
- `nodes` 第一個是入口（必填），最多 127 個。
- 多階 perk 在 node 只填**第一階** FormId。
- skydome 可重用 vanilla `DLC01/Interface/INTVampirePerkSkydome.nif` 免自製。

**Don't**
- 別做 entry-point perk 卻把 `PerkConditionTabCount` 留成 0 → 一進選單 CTD。
- 別在 skill 物件裡放 `version`（那是 root 欄位）。
- 別忘了 `x` 正向朝左、`y` 正向朝上（座標反直覺）；別用 `GridX`/`GridY`（那是舊 INI）。
- 別把檔名取成 `SKILLS.json` 卻以為是獨立群組——`SKILLS.json` 會覆寫原版技能頁。
- 別期待 Translations 有 ENGLISH fallback——缺對應語言檔就顯示成 `$key`。
- 別以為純 esp 能做 Fortify-技能附魔——那條一定要 native SKSE plugin + `ActorValueData/*.toml`。
- 別在玩到一半的舊存檔裝技能後就斷定「沒生效」——init script 靠 `OnPlayerLoadGame`/`OnInit`，乾淨存檔或重載一次再判。

---

> 深水區（兩代格式斷層、舊 INI 對照、VIGILANT/GLENMORIL 案例、完整 schema 欄位表）見 [`custom-skills-framework/README.md`](../custom-skills-framework/README.md)。本指南只負責「照著做就能跑」。
