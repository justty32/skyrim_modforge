# Frostfall — Hypothermia Camping Survival（求生 mod；天賦樹＝Campfire Skill System 消費者）

← [survey index](../index.md)｜機制母體：[campfire.md](campfire.md)

| 項目 | 值 |
| --- | --- |
| 類型 | 內容/系統型 survival mod，**依賴 Campfire.esm** |
| Plugin | `Frostfall.esp`（3.4.1 SE）+ `Frostfall.bsa`（96 支 .psc 隨附）|
| 規模 | quests=39 items=17 magic=169 books=32 npcs=0；無對白 |
| 敘事價值 | 無；機制參考價值：中（exposure/warmth 系統）｜技能樹：見下 |

## 天賦樹（使用者指定重點）

Frostfall **沒有自己的技能樹引擎**——它的「天賦樹」就是把一棵樹**註冊進 Campfire 的 in-world 3D Skill System**（機制全在 [campfire.md](campfire.md)）。

- **技能名＝「Endurance」**。升級點數/進度 GLOB：`EndurancePerkPoints` / `EndurancePerkPointProgress` / `EndurancePerkPointsEarned` / `EndurancePerkPointsTotal`。
- **6 個 perk 節點**（每個一顆 `_camp_intperkstars01` 星，掛 `CampPerkNode`，配一對 rank GLOB）：

| Perk | rank GLOB | max GLOB |
| --- | --- | --- |
| Adaptation | `_Frost_PerkRank_Adaptation` (067B8D) | `_Frost_PerkRank_Adaptation_Max` (067B96) |
| Windbreaker | `_Frost_PerkRank_Windbreaker` (067B98) | `_..._Max` (067B99) |
| Well Insulated | `_Frost_PerkRank_WellInsulated` (067B9C) | `_..._Max` (067B9F) |
| Glacial Swimmer | `_Frost_PerkRank_GlacialSwimmer` (067BA4) | `_..._Max` (067BA5) |
| Frost Warding | `_Frost_PerkRank_FrostWarding` (067BA6) | `_..._Max` (067BA7) |
| Inner Fire | `_Frost_PerkRank_InnerFire` (067BA8) | `_..._Max` (067BA9) |

→ 這組 `_Frost_PerkRank_*` GLOB 正是 `CampPerkNode.required_perk_rank_global(_max)` 屬性所指；點星 → `IncreasePerkRank()` 寫回該 GLOB → perk 效果（ability/MGEF，如 `_Frost_PerkArmorFFSelf30/50/70` 之類 warmth/armor 加值）依 GLOB 條件生效。**視覺（星）與效果（MGEF）解耦**，星只是 GLOB 的可點介面。

**對 ModForge**：Frostfall 是 [campfire.md §3/§4](campfire.md) 那條「掛樹 API」的活範例——一棵 6 節點 Endurance 樹 = 6 ACTI(node)+連線+1 controller ACTI+register quest+12 個 rank GLOB+description Message。全在 ModForge record 能力域內，只缺 PositionRef layout 模板。

## 其它系統（一行帶過）

exposure / warmth / wetness / coverage 系統（`_Frost_ExposureSystem` 等大量 monitor 腳本）＝持續輪詢玩家狀態 + meter widget HUD；`FrostfallAPI.psc` 對外暴露查詢。純參考，與 ModForge 生成目標無交集。
</content>
