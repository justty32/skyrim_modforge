<!-- 法術、附魔、法術書 -->
# 食譜手冊 — 法術與附魔

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「自訂瞄準戰鬥法術」（MGEF + 彈體 + SPEL）

```jsonc
{ "magicEffects": [
    { "editorId": "MF_Firebolt", "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFire",
      "castType": "FireAndForget", "targetType": "Aimed", "baseCost": 12.0,
      "flags": [ "Hostile", "Detrimental", "NoArea" ],   // 不加 Recover（這是即時效果）
      "projectile": "Skyrim.esm:0x10FBEA",               // 重複使用原版火焰箭彈體（可見光束 + 命中效果）
      "castingArt": "Skyrim.esm:0x01B211" }              // 雙手特效
  ],
  "spells": [
    { "editorId": "MF_FireboltSpell", "name": "Forged Firebolt",
      "spellType": "Spell", "castType": "FireAndForget", "targetType": "Aimed",
      "equipType": "Skyrim.esm:0x013F44",                // EitherHand — 必填，否則 NPC 無法裝備/施放
      "effects": [ { "magicEffect": "MF_Firebolt", "magnitude": 25, "area": 0, "duration": 0 } ] }
  ] }
```

重複使用原版的 `projectile` 與 `castingArt`，才能讓光束可見並傳遞命中效果。若缺少 `equipType`，NPC 會改用近戰攻擊——這是生成戰鬥法術時最常見的無聲失敗原因。

## 「為自訂效果製作附魔武器」（MGEF + ENCH + WEAP + COBJ）

三個層次：自訂 **MGEF**（命中時觸發的效果）→ **附魔** / ENCH（`enchantType: weapon`）→ 引用它並帶有充能槽的**武器**。加入 COBJ 讓玩家可以製作。（若為被動**裝備**附魔，使用 `enchantType: apparel` 放在 `armor` 上——無需 `enchantmentAmount`，穿著時持續生效。）

> **盔甲必須帶有 `template`，否則裝備後會隱形**（已於 2026-06-01 在遊戲中確認）。ARMO 穿著時的網格位於其 Armature（ARMA 附加記錄）上，而非 ARMO 本身。將 `template` 設為相同槽位的原版盔甲，例如 `"template": "Skyrim.esm:0x00012E49"`（ArmorIronCuirass）。Build 現在會在護甲沒有 `template` 時發出警告。

```jsonc
{ "magicEffects": [
    { "editorId": "MF_FrostDamageEnchEffect", "name": "Frost Damage",
      "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFrost",
      "castType": "FireAndForget", "targetType": "Touch", "baseCost": 1.5,
      "flags": [ "Hostile", "Detrimental", "NoArea" ] }
  ],
  "enchantments": [
    { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
      "enchantType": "weapon",
      "enchantmentCost": 15,            // 每次攻擊從武器充能槽消耗的量
      "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
  ],
  "weapons": [
    { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
      "template": "Skyrim.esm:0x012EB7", "damage": 8,
      "enchantment": "MF_FrostWeaponEnch", "enchantmentAmount": 1500 }
  ],
  "recipes": [
    { "editorId": "MF_FrostIronSwordRecipe", "createdObject": "MF_FrostIronSword",
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 2 },
                      { "item": "Skyrim.esm:0x02E4FC", "count": 1 } ] }
  ] }
```

完整檔案：[`examples/enchantment_spec.json`](../../examples/enchantment_spec.json)。**注意——僅通過結構驗證：** 附魔在遊戲中實際*觸發*尚未確認。

## 「法術書」（自訂法術的 BOOK，首次閱讀時教授法術：MGEF → SPEL → BOOK）

法術書是一種 BOOK，其 `teaches` 欄位在首次閱讀時授予一個 SPEL。最佳組合：撰寫法術（自訂 MGEF + SPEL，如上）以及一本教授它的書卷——全都在同一份規格中。閱讀書卷後，玩家即可獲得法術。

```jsonc
{ "magicEffects": [ { "editorId": "MF_EmberLanceEffect", /* …archetype/projectile/castingArt… */ } ],
  "spells":       [ { "editorId": "MF_EmberLanceSpell", "name": "Ember Lance", /* …effects… */ } ],
  "books": [
    { "editorId": "MF_SpellTomeEmberLance", "name": "Spell Tome: Ember Lance",
      "text": "<p>Reading this grants the Ember Lance spell.</p>",
      "template": "Skyrim.esm:0x10F7F4",                 // 克隆 SpellTomeIncinerate 的 MODEL（否則閱讀時會崩潰）
      "value": 250, "flags": [ "CantBeTaken" ],          // 原版法術書旗標
      "teaches": { "kind": "spell", "spell": "MF_EmberLanceSpell" } },   // ← 教授規格內的法術

    // 也可以：透過外部參照教授原版法術…
    { "editorId": "MF_SpellTomeFirebolt", "name": "Spell Tome: Firebolt (copy)",
      "template": "Skyrim.esm:0x10F7F4",
      "teaches": { "kind": "spell", "spell": "Skyrim.esm:0x012FD0" } },

    // …或是在閱讀時提升技能的技能書（保留 template 就不會發生模型崩潰）
    { "editorId": "MF_SkillBookDestruction", "name": "Pyromancy for Beginners",
      "template": "Skyrim.esm:0x0ED161",
      "teaches": { "kind": "skill", "skill": "Destruction" } }
  ] }
```

注意事項：教授書卷可以被取用和閱讀，因此仍然需要一個 `template`（用於克隆 `.nif` 模型的原版 BOOK）——沒有模型的書卷在開啟閱讀介面時會**崩潰**。規格內的 `spell` 參照在建構第二階段才會連結，因此書卷可以教授在同一份規格中稍後定義的法術。透過 CLI 確認書卷的模型或 `Teaches` 結構：`bookdiag <Skyrim.esm> 0x10F7F4`（原版法術書）或 `0x01AFD2`（技能書）。實際上的*閱讀後授予*在結構上已連結，但此處尚未在遊戲中確認。
