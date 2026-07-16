# Act 1 Quest 06 - Mar'so Suicide (Bad End)

Status: first source-grounded slice for Act 1 ending branch. Link-first, ESM-verified.

Source policy:
- Quest metadata from `questdiag` Vigilant.esm; no hallucinations.
- Dialogue lines linked to extracted sources instead of copied wholesale.
- Scene staging (if any SCEN found) from CLI diagnostics.

## Quest Record

[`4CDF8D zzzAoMMq06BadEnd "Mar'so Suicide"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:539)

CLI:
- `questdiag Vigilant.esm 0x4CDF8D`
- `infodiag Vigilant.esm 0x4CDF8D`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x4CDF8D`
- EditorID: `zzzAoMMq06BadEnd`
- Name: `Mar'so Suicide`
- Flags: `RunOnce`
- Priority: `50`
- Type: `Misc`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 100 | CompleteQuest | empty |

Objectives:
- None (0 objectives)

## Alias / Staging Backbone

No aliases printed by `questdiag` for this quest. The quest is purely dialogue-driven via the Hello topic.

Host quest:
- [`4CDF8D zzzAoMMq06BadEnd`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:539)

## Dialogue Branch: Mar'so (Bad Ending Hello)

Topic:
- [`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1191)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#0` (Speaker implied: Mar'so in the bad ending variant).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`4CDF8E zzzAoMMq06BadEndHello`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1191) | `4CDF8F` | Goodbye | `GetIsAliasRef alias #0` | "No more interruptions. It's just you and me now, Campaner'Ra." |
| | `4CDF90` | Goodbye | `GetIsAliasRef alias #0` | "I'll be here forever, Campanella. Always and forever." |
| | `4CDF91` | Goodbye | `GetIsAliasRef alias #0` | "Here in the deep end of the pond, no one can disturb us anymore. Even Jo'vanni wouldn't be able to come here." |
| | `4CDF92` | Goodbye | `GetIsAliasRef alias #2` | "Hail Meridia in hard times and sad times, oh, hail Meridia!" |

Translation notes:
- Alias `#2` is distinct in the final INFO; likely a fallback speaker condition or cultist variant.

## Reconstruction Notes: Bad End Context

Source-grounded context (inferred from dialogue + related quest structure):

**Narrative arc:**
- This is the **Bad Ending** branch of the Act 1 Khajiit subplot (paired with `4D0376 zzzAoMMqGoodEnd "Art of Mercy"`).
- The protagonist has either failed to stop Mar'so or failed a moral choice with Campaner'Ra.
- Mar'so, a male Khajiit, has been corrupted (likely by Molag Bal) and has metaphorically or literally taken/consumed Campaner'Ra (female Khajiit).
- The dialogue lines reference:
  - Mar'so and Campaner'Ra being "one" (union/possession/death).
  - Jo'vanni (a third Khajiit NPC) being separated from them.
  - A "pond" location (game-world geography TBD).
  - Meridia invocation (suggests tie to Daedric corruption or moral choice point).

**Related records (context only, not owned by this quest per `infodiag`):**
- NPC [`001842 zzzAoMCatMale02` / `0B15B3 zzzCHMarso` – Mar'so](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:61)
- NPC [`001844 zzzAoMCatFemale01` / `2D35C3 zzzCHEpiCat01` – Campaner'Ra](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:65)
- Dialogue context from main branch [`009E68 zzzAoMMq06 "Also sprach Kahjiit"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:949) — dialogue topics [`00A3E3`–`00A3F9`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:960) show Campaner'Ra + Mar'so domestic scenes and Jo'vanni conflict.

**Relationship to Good End:**
- Paired quest [`4D0376 zzzAoMMqGoodEnd "Art of Mercy"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:541) has opposite condition flow and outcome (protagonist stops tragedy, saves Campaner'Ra).

## Open Verification

- **Alias ownership**: `questdiag` did not print aliases for this quest; verify via Mutagen QUST record if aliases exist but are not printed (e.g., forcedRef fills).
- **Engagement trigger**: Unknown stage/condition that routes player to Bad End path vs Good End path; likely in parent quest `zzzAoMMq06` dialogue conditions or script.
- **Cell/ref geography**: "pond" and NPC ref positions TBD.
- **Relationship to Meridia**: The final INFO condition on alias `#2` suggests a Daedric flavor (Meridia invocation); verify if this is a separate speaker or fallback.
- **Script behavior**: Whether stage 100 CompleteQuest is auto-fired or script-driven.
