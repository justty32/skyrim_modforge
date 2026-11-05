# CODE_MAP.items-magic — Examples・Tests

← [CODE_MAP.items-magic](../CODE_MAP.items-magic.md)

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/customspell_spec.json` | 自訂法術 + magic effect |
| `examples/mgef_spec.json` | 自訂 MGEF（archetype / actorValue）|
| `examples/spell_tome_spec.json` | spell tome 教學 |
| `examples/enchantment_spec.json` | weapon / apparel enchantment |
| `examples/perk_spec.json` | perk entry-point + ability spell |
| `examples/mcm_global_perk.json` | MCM bool toggle → GLOB → `ModBuyPrices` perk CTDA 完整接線 |
| `examples/interactive_perk_spec.json` | **#1 互動式 perk：addActivateChoice（[E] 選項 + spell/fragmentBody）+ setText（改活化提示）；PerkAdapter fragment（package 路徑）** |
| `examples/shout_spec.json` | 三段 shout + word of power |
| `examples/texture_set_spec.json` | TXST 8 槽路徑 |
| `examples/custom_asset_spec.json` | 外部網格 / 貼圖 / 音效打包 |
| `examples/assets/customasset/` | 配套示範資產（.nif / .dds / .wav）|
| `examples/textures/` | 貼圖集示範資產（.dds）|
| `examples/projectile-explosion.json` | 自訂法術飛行彈 + 爆炸（PROJ/EXPL，完整可施法 firebolt 鏈）|
| `examples/effect_shader.json` | EFSH membrane + sprite particles，並接 MGEF hit/enchant shader |
| `examples/message_menu.json` | MESG 多按鈕 menu（按鈕順序＝Papyrus `Message.Show()` 回傳 index）|
| `examples/showcase-multi2.json` | 多功能 showcase #2（firebolt PROJ/EXPL + NPC 庫存武器 + scene 條件閘）|

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `WeaponTests.cs` | templated weapon 傷害：未給傷害保留 template 值 / 顯式傷害覆寫 |
| `MagicFxTests.cs` | Projectile + Explosion build（scalar + ref 解析 + enum/flag + validate）|
| `EffectShaderTests.cs` | EFSH texture/scalar/key build、palette warning、MGEF shader wiring、validate |
| `MessageTests.cs` | MESG notification 相容、1–10 個有序按鈕 build、空 label/超量 validate |
| `EnchantmentTests.cs` | ENCH scalar fields + effect ref 接線 |
| `ExternalAssetTests.cs` | external asset 打包（Meshes/Textures/Sounds 複製）|
| `PerkTests.cs` | Perk trunk + entry-point modifier + ability-spell grant |
| `McmGlobalWiringTests.cs` | MCM QUST VMAD property 與 perk `GetGlobalValue` 指向同一顆 GLOB FormKey |
| `PerkActivateChoiceTests.cs` | **#1 addActivateChoice（EntryType.Activate/ButtonLabel/Spell/conditions/tab-count）+ setText（Text）+ fragment source（Fragment_N body）+ PerkAdapter VMAD 綁定[fake .pex]（IndexedScriptFragment + Flags.RunImmediately/FragmentIndex）+ 無 .pex 不掛 + validate（空 label / do-nothing / 空 text）** |
| `ShoutTests.cs` | Shout word-of-power + spell tier + cooldown build |
| `SpellTomeTests.cs` | spell tome teaching mechanics |
| `TextureSetTests.cs` | TXST 8 槽路徑 build |
| `TextureSetValidateTests.cs` | texture-path / model-path sanity validate |
