# Perk Entry-Point 機制深挖

← [findings index](../index.md)

> **動機**：Perk 的 entry-point 是 Skyrim 引擎的「hook 插槽」——mod 在這裡插入「當 X 發生時做 Y」。ModForge 目前只支援 `ModifyValue`（數值型 entry-point）與 `ability`（SPEL 載具），`AddActivateChoice`（互動啟動選單）與 `SetText`（覆寫啟動按鈕文字）兩個互動型完全未支援，是 mod-survey-gaps 缺口 #1。本 finding 把完整機制拆清楚，作為 builder 擴充的設計基礎。

---

## 內容拆分

- [機制概覽 + Entry-point 種類全表](perk-entry-points-mechanism.md) — hook 插槽概念、record 層次（perk/effect 條件、PerkConditionTabCount）、EntryType(91)/FunctionType 全表
- [AddActivateChoice + SetText 深挖](perk-entry-points-activate-choice.md) — 兩個互動型 entry-point 的機制、record layout、effect-level conditions、tab-count CTD、搭配模式
- [Perk-fragment 膠水](perk-entry-points-fragment-glue.md) — PerkAdapter VMAD、`Extends Perk` script 格式、`Fragment_N` 命名、dispatcher 模式、與 TIF/QF/SF 比較
- [ModForge 現有 builder 與缺口](perk-entry-points-gaps.md) — 現有 Perks builder 能力（有 code 為據）+ 確認缺口 #1 對照表
- [實作建議](perk-entry-points-impl.md) — PerkEffectSpec 擴充 / WirePerks emit / PerkAdapter pass / fragment 腳本生成 / tab-count 表補充 / perkdiag

## 相關筆記連結

- `perk-conditiontabcount-ctd`（記憶）：tab-count byte 必須非 0，否則 load CTD；FilterActivation=2 已修；SetActivateLabel 未補
- `immersive-interactions.md`：Immersive Interactions 完整機制拆解，真實 AddActivateChoice 實例（29 effects + 4 SetText）
- `arrowblock.md`：ModIncomingDamage Set 0 的 entry-point 模式，現有 builder 可生成的代表性案例
- `scene-playidle-recipe.md`：SceneAdapter fragment 膠水，PerkAdapter 可仿此模式
- `dispatcher-magic-trigger.md`：perk fragment → quest script dispatcher 模式的同源概念
- `mod-survey-gaps.md`（roadmap）：缺口 #1 正式定義
