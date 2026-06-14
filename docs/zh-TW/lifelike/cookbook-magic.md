<!-- Magic patterns -->
# 食譜手冊 — 魔法

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## 「自訂瞄準型戰鬥法術」（MGEF + projectile + SPEL）

```jsonc
{ "magicEffects": [
    { "editorId": "MF_Firebolt", "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFire",
      "castType": "FireAndForget", "targetType": "Aimed", "baseCost": 12.0,
      "flags": [ "Hostile", "Detrimental", "NoArea" ],   // NOT Recover (it's instant)
      "projectile": "Skyrim.esm:0x10FBEA",               // reuse vanilla firebolt projectile (visible bolt + impact)
      "castingArt": "Skyrim.esm:0x01B211" }              // hands FX
  ],
  "spells": [
    { "editorId": "MF_FireboltSpell", "name": "Forged Firebolt",
      "spellType": "Spell", "castType": "FireAndForget", "targetType": "Aimed",
      "equipType": "Skyrim.esm:0x013F44",                // EitherHand — REQUIRED or the NPC can't equip/cast it
      "effects": [ { "magicEffect": "MF_Firebolt", "magnitude": 25, "area": 0, "duration": 0 } ] }
  ] }
```

重用原版的 `projectile` + `castingArt`，正是讓彈道可見、並能傳遞命中的關鍵。少了 `equipType`，NPC 會改用近戰／永遠不施法——這是生成型戰鬥法術第一名的無聲失敗。

## 「自訂效果的附魔武器」（MGEF + ENCH + WEAP + COBJ）

三層：自訂的 **MGEF**（命中時發生什麼）→ 一個附魔／ENCH（可重用的「物件效果」，`enchantType: weapon`）→ 一把引用它並帶有充能池的武器。再加一個 COBJ，讓玩家可以打造它。（若要被動的**護甲**附魔，改用 `enchantType: apparel`，並把 `enchantment` 放到 `armor` 上——不需要 `enchantmentAmount`，穿戴時恆定生效。）

> **護甲必須帶 `template`，否則裝備時會 INVISIBLE**（遊戲內確認 2026-06-01：套了模板的胸甲穿戴時顯示鐵甲網格）。ARMO 的穿戴網格存在於它的 Armature（ARMA addon 記錄）上，而非 ARMO 本身——一個只有 `armorType`+`slots` 的 spec 護甲穿戴時不會渲染出任何東西（它*不會*當機）。把 `template` 設成同部位的原版護甲，例如 `"template": "Skyrim.esm:0x00012E49"`（ArmorIronCuirass）；這個複製會帶來 Armature（穿戴網格）、WorldModel（地面模型）與 BodyTemplate。build 時若缺少 `template` 會發出警告。

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
      "enchantType": "weapon",          // → EnchantType=Enchantment, cast=FireAndForget, target=Touch
      "enchantmentCost": 15,            // per-strike charge drained from the weapon's pool
      "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
  ],
  "weapons": [
    { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
      "template": "Skyrim.esm:0x012EB7", "damage": 8,   // template = model (else CRASH on equip)
      "enchantment": "MF_FrostWeaponEnch", "enchantmentAmount": 1500 }   // 1500 = charge pool
  ],
  "recipes": [
    { "editorId": "MF_FrostIronSwordRecipe", "createdObject": "MF_FrostIronSword",
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 2 },     // IngotIron
                      { "item": "Skyrim.esm:0x02E4FC", "count": 1 } ] }  // SoulGemGrand
  ] }
```

完整檔案：[`examples/enchantment_spec.json`](../../../examples/enchantment_spec.json)。用 `enchdiag <out.esp> <0xFORMID>`（ENCH 的 type/cost/effects）與 `dump`（武器的 `enchantment ->` 連結 + 充能）驗證。**注意——僅結構驗證：**這些記錄能 build、驗證、連結並正確 round-trip，且完全鏡像原版 ENCH 結構，但附魔在遊戲內實際*觸發*尚未確認（沒有跑過遊戲內測試）。`enchantmentCost` ↔ `enchantmentAmount` 的調校、以及引擎是否會自動為充能定價，是最可能需要在遊戲內驗證的部分。

## 「自訂法術的法術書」（MGEF → SPEL → 教授它的 BOOK）

法術書是一本 BOOK，其 `teaches` 在首次閱讀時授予一個 SPEL。殺手級組合：撰寫法術（自訂 MGEF + SPEL，如上）以及一本教授它的法術書——全部在同一個 spec 裡。閱讀該書即把法術給予玩家。

```jsonc
{ "magicEffects": [ { "editorId": "MF_EmberLanceEffect", /* …archetype/projectile/castingArt… */ } ],
  "spells":       [ { "editorId": "MF_EmberLanceSpell", "name": "Ember Lance", /* …effects… */ } ],
  "books": [
    { "editorId": "MF_SpellTomeEmberLance", "name": "Spell Tome: Ember Lance",
      "text": "<p>Reading this grants the Ember Lance spell.</p>",
      "template": "Skyrim.esm:0x10F7F4",                 // clone SpellTomeIncinerate's MODEL (else CRASH on read)
      "value": 250, "flags": [ "CantBeTaken" ],          // vanilla spell-tome flag
      "teaches": { "kind": "spell", "spell": "MF_EmberLanceSpell" } },   // ← teaches OUR in-spec spell

    // also valid: teach a VANILLA spell by external ref…
    { "editorId": "MF_SpellTomeFirebolt", "name": "Spell Tome: Firebolt (copy)",
      "template": "Skyrim.esm:0x10F7F4",
      "teaches": { "kind": "spell", "spell": "Skyrim.esm:0x012FD0" } },

    // …or a SKILL book that raises a skill on read (no model crash if you keep a template)
    { "editorId": "MF_SkillBookDestruction", "name": "Pyromancy for Beginners",
      "template": "Skyrim.esm:0x0ED161",
      "teaches": { "kind": "skill", "skill": "Destruction" } }
  ] }
```

陷阱：教學書是可拿取／可閱讀的，所以它*仍然*需要 `template`（一本可從中複製 `.nif` 模型的原版 BOOK）——一本沒有模型的書會讓閱讀畫面當機。in-spec 的 `spell` ref 在 build pass 2 接好，因此這本書可以教授同一個 spec 中稍後定義的法術。用 CLI 探索一本書的模型／`Teaches` 形狀：`bookdiag <Skyrim.esm> 0x10F7F4`（一本原版法術書）或 `0x01AFD2`（一本技能書）。實際的*閱讀時授予*已在結構上接好，但此處遊戲內未確認。
