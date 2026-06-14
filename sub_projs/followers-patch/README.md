# followers-patch — 多隨從性格分析與擴充

← [sub_projs](../) ｜ 姊妹專案：[sofia-patch](../sofia-patch/)（Sofia × VIGILANT，已跑通的範本）

## 這是什麼

為一批**自訂隨從**各寫一份 **sofia 式「性格分析 / 寫作 brief」**（personality brief），作為日後給他們做評論 patch、情境台詞、語音擴充的寫作依據。範本＝[sofia-patch/sofia-personality.md](../sofia-patch/sofia-personality.md)（定位→原型→幽默風格→對玩家→背景鉤子→語言癖→情緒光譜→長篇/黑暗劇情反應→lore 寫法→寫作 checklist）。

**素材來源**：各隨從的對白抽取在 `../game-data/mods/<Mod>/`（gitignored 本地參考；英文本體 + 官方中文化對照，中文化的 mojibake 已修）。寫 personality 以**英文本體**理解角色，產出用**繁體中文**。

## 目標隨從與素材

| 隨從 | 角色定位（待 brief 確認）| 主要素材夾 |
|---|---|---|
| **Auri** | Song of the Green，木精靈女獵手 | `Auri_SongOfTheGreen`（EN）+ `Auri_VIGILANTpatch`（她在 VIGILANT 的吐槽）|
| **Morgaine** | 全語音獨立隨從 | `Morgaine`（EN）+ `Morgaine_CHS` |
| **Onean** | 自訂隨從 | `Onean`（EN）+ `Onean_CHT` |
| **Neisa** | 自訂隨從 | `Neisa`（EN）+ `Neisa_CHT` |
| **Remiel** | Dwemer Specialist，矮人科技狂 | `Remiel_DwemerSpecialist`（EN，核心 6210 行）+ LOTD/BeyondReach/DeepElf/ThograBanter 評論 |
| **Recorder** | 記錄者，已有 brief（官方繁中＝`Recorder_CHT`）| `Recorder`（EN）；brief 見本資料夾 `recorder-personality.md` |
| **Serana** | DLC 半正典，Dialogue Add-On 大幅擴充 | `SeranaDialogueAddOn`（EN，7295 行）|

## 產出檔

每隨從一份 `<隨從>-personality.md`，比照 sofia-personality 的節次與深度。

## 狀態

personality brief 已全部產出（2026-06-14，平行 agent 依 sofia-personality 範本）：
- ✅ 全 7 份：`auri`、`morgaine`、`onean`、`neisa`、`remiel`、`recorder`（從原 recorder-patch 移入）、`serana`（正典 Dawnguard + Serana Dialogue Add-On）、`mirai`（SN Mirai 英譯 + more aware；高感知型）。

備註：Onean/Neisa 無英文原文（mod 本身即中文，EN 抽取為 mojibake，已修為可讀中文）；brief 以中文台詞為據。
