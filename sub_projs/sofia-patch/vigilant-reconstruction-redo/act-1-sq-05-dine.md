# Act 1 Quest 05 - Dine and Dash

Status: source-grounded slice. Link-first, dialogue-centric, branch map from conditions.

Source policy:
- Original dialogue lines are linked to extracted source files instead of copied in full.
- Short snippets appear only when needed to explain ambiguity or typo/encoding.
- Scene staging comes from CLI diagnostics and dialogue topic structure, not plot summary.

## Quest Record

[`0098C9 zzzAoMMq05 "Dine and Dash"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:218)

CLI:
- `questdiag Vigilant.esm 0x0098C9`
- `infodiag Vigilant.esm 0x0098C9`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x0098C9`
- EditorID: `zzzAoMMq05`
- Name: `Dine and Dash`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 10 | none | empty |
| 11 | none | empty |
| 20 | none | empty |
| 21 | none | empty |
| 22 | none | empty |
| 23 | none | empty |
| 25 | none | empty |
| 30 | none | empty |
| 40 | none | empty |
| 45 | none | empty |
| 50 | none | empty |
| 60 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

(15 stages total, verified; `CompleteQuest` on stages 60 and 9999)

Objectives from `questdiag`:

| Index | Source | Log |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:219) | Talk to Altano in the Candle Hearth Hall |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:220) | Follow Altano or Join Altano at Stendarr's Beacon |
| 20 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:221) | Follow Alatano or Join Altano at The Bee and Barb |
| 25 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:222) | Defeat Daedra |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:223) | Talk to Keerave |
| 40 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:224) | Pay 1000G Keerave |
| 41 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:225) | Barrow Money from Altano (Option) |
| 50 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:226) | Report to Jacob |

Objective targets:
- 8 objectives with location targets (Riften establishments: Candlehearth Hall, Stendarr's Beacon, The Bee and Barb).
- Target refs not printed by CLI questdiag; requires deeper QUST alias/target dump if exact ref locations matter.

## Dialogue Backbone

Dialogue structured in 5 branches with distinct condition gates (stage + alias checks). Stage progression: 0 → 10/11 (briefing) → 20/21/22/23/25 (travel) → 30 (inn encounter) → 40/45 (payment negotiation) → 50/60 (completion).

All custom topics require `GetIsAliasRef` checks on aliases (`alias #0` = Altano, `alias #1` = Jacob, `alias #7` = Keerave innkeeper).

### Branch 1: Mission Briefing (Stage 0→10)

Custom topic:
- [`009E30 zzAoMMq05B1Mission5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:156)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`009E30 zzAoMMq05B1Mission5`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:156) | `009E31` | none | GetInCell `016789:Skyrim.esm` (Candlehearth Hall); GetStage < 10 on quest `0098C9`; GetIsAliasRef alias #0 | [「I got a letter from Stendarr's Beacon. The summoner were witnessed in Riften. Let's go to Stendarr's Beacon to listen the detail.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:157) |
| | | | VMAD: `AoM05_TIF__01009E31` Fragment_0 on end | |

### Branch 2: Scene Topics at Stendarr's Beacon (Stage 10→20)

Scene exchange: briefing with Jacob at Stendarr's Beacon. These are dialogue topics structured as scene exchanges (condition-free scene flow):

| Topic | INFO | Speaker | Response | Translation |
|---|---|---|---|---|
| [`009E3D` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:159) | `009E3E` | — | [「Master Jacob, Long time no see. How are you?」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:160) |
| [`009E3F` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:162) | `009E40` | — | [「Hahaha, don't stand on ceremony so much. You and I are agents of Stenndarr.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:163) |
| [`009E41` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:165) | `009E42` | — | [「So...we heard you find the summoner...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:166) |
| [`009E43` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:168) | `009E44` | — | [「Viglants find her in the Bee and Barb. They will catch her....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:169); [「She summoned Powerful Daedra..so vigilants are at a loss what to do.To make matters worse, theat Daedra stay at Inn.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:170); [「But we are fully occupied to chase summoner. I entrust defeating Deadra to you.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:171) |
| [`009E45` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:173) | `009E46` | — | [「Let us handle this. The Daedra will regret to be summoned.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:174) |
| [`009E47` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:176) | `009E48` | — | [「Hahaha! You are reliable! By the way..about your partner...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:177) |
| [`009E49` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:179) | `009E4A` | — | [「You have good eyes as letter from Altano. Your look is like Stendarr....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:180); [「Be carefull, Daedra is astute.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:181) |
| [`009E4B` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:183) | `009E4C` | — | [「Here, we go.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:184) |

Notes:
- No explicit speaker names in scene topics; inferred from context (Jacob at beacon, player/Altano as responders).
- The exchange bridges objective 10 (reach beacon) to objective 20 (reach inn) and stage 20.

### Branch 3: Scene Topics at The Bee and Barb (Stage 20→30)

Daedra encounter scene. Multiple INFOs, no branches, condition-free flow.

Scene exchange: inn arrival and Daedra interaction.

| Topic | INFO | Speaker | Response | Translation |
|---|---|---|---|---|
| [`009E4F` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:186) | `009E50` | Daedra | [「Hey, Waiter!! Bring more foods and drinks, or I will eat your head!!」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:187) |
| [`009E51` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:189) | `009E52` | — | [「Where is your summoner? if you admit, I kill you peacefully.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:190) |
| [`009E53` - Scene/Scene](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:192) | `009E54` | Daedra | [「Kill? Mortal say kill immmortal Daedra? Hahahahaha! Mortal is very funny.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:193); [「You want infomation about summoner? I admit you enter my stomack.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:194) |

Notes:
- Daedra's humor and appetite combine confrontation with comedic tone.
- Scene leads to objective 25 (defeat Daedra) and stage 30 (post-combat, innkeeper dialogue).
- Typo: "immmortal" (source as-is); "theat" in earlier scene appears to be "that".

### Branch 4: Payment Negotiation (Stage 30→45)

Three payment-related topics representing the player's options after defeating the Daedra: full payment, delay/no money, or borrow from Altano.

#### Sub-Branch 4a: Payment Demand (Stage 30)

Custom topic:
- [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196) | `009E57` | SayOnce, WalkAway | GetStage == 30 on quest `0098C9`; GetIsAliasRef alias #7 (Keerave) | [「Hey! Wait!! You should pay for food and drink Daedra had. Payment is 1000G.I never reduce the price!!」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:197) |
| | | | VMAD: `AoM05_TIF__01009E57` Fragment_0 on end | |
| [`009E56 zzAoMMq05B2Payment`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:196) | `009E58` | WalkAway | GetStage >= 40 on quest `0098C9`; GetStage < 50; GetIsAliasRef alias #7 | [「Can you pay 1000G?」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:198) |

Notes:
- `SayOnce` on first INFO → triggers stage 30 dialogue once.
- `WalkAway` flags indicate NPC breaks conversation.
- Re-open at stage 40+ with simpler prompt (「Can you pay 1000G?」), suggesting multiple conversation opportunities.

#### Sub-Branch 4b: Full Payment Path (Pay 1000G)

Custom topic:
- [`009E59 zzAoMMq05B2Pay1000`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:200)

| Topic | INFO | Flags | Conditions | Response | VMAD |
|---|---|---|---|---|---|
| [`009E59 zzAoMMq05B2Pay1000`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:200) | `009E5A` | Goodbye | GetGold >= 1000 (on Player ref `000014:Skyrim.esm`); GetIsAliasRef alias #7 | [「Thank you. You should choose your friends very carefully.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:201) | `AoM05_TIF__01009E5A` Fragment_1 (OnBegin) + Fragment_0 (OnEnd) |

Notes:
- Goodbye flag → ends conversation.
- Gold check on Player character (vanilla ref 0x000014).
- VMAD callback likely deducts 1000G and advances quest.

#### Sub-Branch 4c: No Money Path (Delay)

Custom topic:
- [`009E5B zzAoMMq05B2NoMoney`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:203)

| Topic | INFO | Flags | Conditions | Response |
|---|---|---|---|---|
| [`009E5B zzAoMMq05B2NoMoney`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:203) | `009E5C` | Goodbye | GetIsAliasRef alias #7 | [「OK.I wait a minute for you. if you dine and dash... I will call gurads.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:204) |

Notes:
- Callback for players without 1000G.
- "dine and dash" reference (quest title origin) — inn threat if player doesn't pay.
- Typo: "gurads" → "guards".

#### Sub-Branch 4d: Borrow Path (From Altano)

Custom topic:
- [`009E5E zzAoMMq05B3Debt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:206)

| Topic | INFO | Flags | Conditions | Response | VMAD |
|---|---|---|---|---|---|
| [`009E5E zzAoMMq05B3Debt`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:206) | `009E5F` | SayOnce | GetStage == 40 on quest `0098C9`; GetIsAliasRef alias #0 (Altano) | [「Huh...OK. I will pay 800G.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:207) | `AoM05_TIF__01009E5F` Fragment_0 on end |

Notes:
- Altano (alias #0) covers 800G of the 1000G debt (player pays 200G).
- Gate: stage 40 (post-negotiation window).
- Alternative resolution: borrow path vs. full payment.

### Branch 5: Completion (Stage 50→60)

Two completion topics reporting back to Jacob at quest location.

#### Sub-Branch 5a: Mission Success Report

Custom topic:
- [`009E61 zzAoMMq05B4Mission5Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:209)

| Topic | INFO | Flags | Conditions | Response |
|---|---|---|---|---|
| [`009E61 zzAoMMq05B4Mission5Comp`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:209) | `009E62` | none | GetStage == 50 on quest `0098C9`; GetIsAliasRef alias #1 (Jacob) | [「Many thanks for your trouble」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:210) |

#### Sub-Branch 5b: Post-Combat Investigation (Stage 50→60)

Custom topic:
- [`009E63 zzAoMMq05B4Summoner`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:212)

| Topic | INFO | Flags | Conditions | Responses |
|---|---|---|---|---|
| [`009E63 zzAoMMq05B4Summoner`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:212) | `009E64` | none | GetIsAliasRef alias #1 (Jacob) | [「Yes, viglants run the summoner down in Ratway...but we fail to catch. There is a swordman who equips Ebony mail with the summoner.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:213); [「Swordman maybe hired by the summoner. He is very strong. He broke through the besieging vigilants...head-on...」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:214); [「Special Chasers started just now. How many people survive.....」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:215) |

Notes:
- Reports summoner's escape to Ratway with unknown mercenary (Ebony Mail equipper).
- Links to Act 1 Quest 06 ("Also sprach Kahjii") Ratway subplot.
- Ambiguous outcome: tactical loss despite military superiority.

#### Sub-Branch 5c: Next Mission Brief

Custom topic:
- [`009E65 zzAoMMq05B4NextMission`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:217)

| Topic | INFO | Flags | Conditions | Responses | VMAD |
|---|---|---|---|---|---|
| [`009E65 zzAoMMq05B4NextMission`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:217) | `009E66` | Goodbye | GetIsAliasRef alias #1 (Jacob) | [「Invetstigate Ratway. There is the marks of Cojurring Daedra.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:218); [「At the moment, no damage was reported in Ratway. But there is dangerous. Search Daedra and destroy.」](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:219) | `AoM05_TIF__01009E66` Fragment_0 on end |

Notes:
- Final objective: report to Jacob → quest completion (stage 60).
- Branching response (two versions): suggests narrative path variation (good vs. aggressive resolution).
- Typo: "Invetstigate" → "Investigate"; "marks" (plural) suggests multiple Daedra or conjuring sites.

## Related Records

NPCs (quest aliases):
- `Altano` (alias #0): protagonist companion, borrow source, briefing partner.
- `Jacob` (alias #1): quest giver, post-combat reporter.
- `Keerave` (alias #7): innkeeper, payment collector.

Locations (objective targets):
- `016789:Skyrim.esm` — Candlehearth Hall (Riften), initial briefing.
- (objective targets for Stendarr's Beacon and Bee and Barb not printed by questdiag; require cell ref lookup).

Items referenced:
- None explicit in dialogue; Daedra's meal reference ("enter my stomack") is narrative flavor.

## Reconstruction Notes

Source-grounded:
- This quest is represented by [`0098C9 zzzAoMMq05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:218) with 8 objectives spanning Riften establishments and a payment mechanic.
- Stage progression: briefing (0–10) → travel (10–25) → inn combat (25–30) → payment negotiation (30–45) → completion report (45–60).
- It contains 5 dialogue branches:
  - Briefing with Altano (custom topic, GetInCell check).
  - Jacob briefing scene at Stendarr's Beacon (11× condition-free scene topics).
  - Daedra encounter at Bee and Barb (3× scene topics).
  - Payment negotiation with Keerave (4 payment paths: full pay, no money, borrow, or unspecified).
  - Completion and follow-up with Jacob (3 topics: success, summoner escape, next mission).
- VMAD callbacks on 6 INFOs indicate stage advancement and resource deduction (gold, quest state).
- No explicit SCEN records are owned by this quest (dialogue is pure topic-INFO chains, not scene staging).

Narrative arc:
- Daedra trouble in Riften inn → vigilant team (player + Altano) sent to investigate → Daedra defeated → innkeeper demands payment (1000G) for Daedra's meal/property damage → player can pay full, delay, or borrow from Altano → report success but summoner escapes to Ratway → brief for next quest (Act 1 Quest 06).

Branch polarity:
- **Good path**: full payment (1000G) to Keerave → clean resolution.
- **Alternative path**: borrow from Altano (800G from Altano, 200G from player) → marked obligation.
- **Delay path**: refusal/no money → innkeeper threat (dine and dash reference) → unresolved tension.
- All paths lead to stage 50 completion reporting, but narrative flavor varies.

Release state:
- Quest is fully implemented in-game dialogue; no missing voice lines or placeholder TODOs noted in extracted text.
- English typos present (immmortal, theat, gurad[s], Invetstigate) — likely from original mod source.

Open verification:
- inspect scripts `AoM05_TIF__01009E31`, `AoM05_TIF__01009E57`, `AoM05_TIF__01009E5A`, `AoM05_TIF__01009E5F`, `AoM05_TIF__01009E66` if source or decompile path exists;
- verify objective target ref locations for Stendarr's Beacon and Bee and Barb if exact placement matters;
- inspect QUST aliases (`alias #0`, `#1`, `#7`) directly if richer quest state/packaging matters.
