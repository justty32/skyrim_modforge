# Act 1 Quest 03 - Lazy Afternoon

Status: source-grounded slice. Link-first, dialogue-centric.

Source policy:
- Original dialogue lines are linked to extracted source files instead of copied in full.
- Short snippets appear only when needed to explain ambiguity or typo/encoding.
- Scene staging comes from CLI diagnostics and dialogue topic structure, not plot summary.

## Quest Record

[`00627F zzzAoMMq03 "Lazy Afternoon"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:199)

CLI:
- `questdiag Vigilant.esm 0x00627F`
- `infodiag Vigilant.esm 0x00627F`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x00627F`
- EditorID: `zzzAoMMq03`
- Name: `Lazy Afternoon`
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
| 15 | none | empty |
| 20 | none | empty |
| 23 | none | empty |
| 25 | none | empty |
| 30 | none | empty |
| 40 | CompleteQuest | empty |
| 255 | ShutDownStage | empty |
| 9999 | CompleteQuest | empty |

(11 stages total, verified)

Objectives from `questdiag`:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:200) | Talk to Altano in The Bannered Mare |
| 10 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:201) | Follow Altano or Join Altano at Candlehearth Hall |
| 15 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:202) | Follow Altano |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:203) | Defeat Daedra |
| 30 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:204) | Report to Altano |

Objective targets:
- 5 objectives, each with location targets.
- Target refs not printed by CLI questdiag; requires deeper QUST alias/target dump if exact ref locations matter.

## Dialogue Branches

### Branch 1: Mission Briefing (Stage 0→10)

Custom topic:
- [`00884F zzAoMMq03B1Mission3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:56)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`00884F zzAoMMq03B1Mission3`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:56) | `008850` | none | GetInCell `01605E:Skyrim.esm` (The Bannered Mare); GetStage < 10 on quest `00627F`; GetIsAliasRef alias #0 | [「Daedra trouble in Windhelm Inn. This affair maybe related to the previous affair. Here we go.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:57) |
| | | | VMAD: `AoM03_TIF__01008850` Fragment_0 on end | |

### Branch 2: Scene Topics (Stage 10→20)

These are dialogue topics structured as scene exchanges. Multiple INFOs, no branches, no custom conditions (condition-free scene flow):

Scene exchange 1 (Windhelm Inn arrival):

| Topic | INFO | Speaker | Response | Translation |
|---|---|---|---|---|
| [`008853` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:59) | `008854` | Altano | [「We are vigilants of Stendarr. We heard that there is Daedra in this Inn.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:60) |
| [`008855` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:62) | `008856` | Innkeeper | [「Yes, Daedra appears few days ago and stay here...can you help us?」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:63) |
| [`008857` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:65) | `008858` | Altano | [「Leave that up to me. we defeat Daedra immediately...by the way, how did Daedra apperas? Summoned?」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:66) |
| [`008859` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:68) | `00885A` | Innkeeper | [「Yes...a woman who is picked a quarrel by drunkard had summoned Daedra. drunkard was teared up by Daedra...I don't want to remember anymore.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:69) |
| [`00885B` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:71) | `00885C` | Altano | [「Do you know where the woman go?」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:72) |
| [`00885D` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:74) | `00885E` | Innkeeper | [「No, I don't know. That was the last I saw of her.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:75) |
| [`00885F` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:77) | `008860` | Altano | [「Understand. We appreciate your cooperation.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:78) (ForceSubtitle) |
| [`008861` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:80) | `008862` | — | (empty) |
| [`008863` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:82) | `008864` | Altano | [「Daedra hunt is begun. Come on.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:83) |

Scene exchange 2 (Daedra encounter, Daedra sleeping):

| Topic | INFO | Speaker | Response | Translation |
|---|---|---|---|---|
| [`008866` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:85) | `008867` | Altano | [「Hey, wake up!!」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:86) |
| [`008868` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:88) | `008869` | Daedra | [「How comfatoble rug is it...good feeling...」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:89) |
| [`00886A` - Scene/Scene](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:91) | `00886B` | Altano | [「I wonder....? He maybe not dangeraous....I enturst to defeat daedra. I seek the summoner.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:92) |

Translation notes:
- [Line 69](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:69): "teared up" → likely "torn apart" or "killed" (past tense, scripting artifact).
- [Line 66](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:66): "apperas" → typo for "appears".
- [Line 89](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:89): "comfatoble" → typo for "comfortable".
- [Line 92](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:92): "enturst" → typo for "entrust".

### Branch 3: Mission Complete (Stage 30→40)

Custom topic:
- [`00886D zzAoMMq03B2Mission3Comp`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:94)

| Topic | INFO | Flags | Conditions | Translations |
|---|---|---|---|---|
| [`00886D zzAoMMq03B2Mission3Comp`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:94) | `00886E` | Goodbye | GetStage == 30 on quest `00627F`; GetIsAliasRef alias #0 | Prompt: (none) Response 1 (Puzzled): [「A queer Daedra....it was. Anyway, most inmportant thing is cathcnig the summoner. Traces of magicka shows she was near...」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:95) Response 2 (Neutral): [「I seek the summoner in Windhelm for a while. If you are ready, come to me.」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:96) |
| | | | VMAD: `AoM03_TIF__0100886E` Fragment_0 on end | |

Translation notes:
- [Line 95](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:95): "inmportant" → typo for "important"; "cathcnig" → typo for "catching".

### Branch 4: Daedra Dialogue (Stage 20-30)

Custom topic:
- [`008870 zzAoMMq03B3Greet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:98)

Greeting (spoken by Daedra, alias #5):

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`008870 zzAoMMq03B3Greet`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:98) | `008871` | WalkAway | GetIsAliasRef alias #5 | [「Hoaaaaaaaaa!!Incredibles!!」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:99) |

Wake-up topic (player option to taunt Daedra):

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`008DD7 zzAoMMq03B3GetUp`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:104) prompt="Wake up, Daedra! You are troublesome." | `008DD8` | Goodbye | GetIsAliasRef alias #5 | Response (Sad): [「I know, I know. so....what?」](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:105) |

Inference on Daedra alias:
- Alias #5 is the active Daedra during stage 20–30 (greeting and wake-up interactions).
- Alias #0 is Altano (used in mission briefing and completion).

## Related Records

These are not all part of quest `00627F` according to `infodiag`, but are contextual to the quest flow.

NPCs:
- `Altano` (alias #0, speaker in scenes and branching dialogue) — referenced via FormID in quest alias structure.
- Daedra (alias #5, speaker in Greet and GetUp topics) — referenced via FormID in quest alias structure.

Locations:
- `01605E` (The Bannered Mare, Windhelm) — condition target in branch 1; mission briefing location.

## Reconstruction Notes

Source-grounded:
- This quest is represented by [`00627F zzzAoMMq03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:199) with five objectives spanning a daedra encounter in an inn.
- It contains one stage-gated custom dialogue branch (mission briefing, stage 0→10).
- It contains eight scene-exchange topics (unrestricted, stage-contextual flow, 008853–00886A).
- It contains one stage-gated completion branch (stage 30→40).
- It contains two Daedra-specific topics (greeting, wake-up taunt, aliases on Daedra NPC).
- No explicit SCEN records found in ESM; scene flow is dialogue-driven (topic chain, no phase/action staging).

Stage progression inference:
- Stage 0: initial (quest starts).
- Stage 10: player has spoken to Altano at The Bannered Mare; scene flow begins.
- Stage 20: Daedra encountered; Daedra greeting/taunt flow active.
- Stage 30: player has defeated Daedra; completion dialogue available.
- Stage 40: quest complete (CompleteQuest flag set).
- Stages 11, 15, 23, 25: intermediate checkpoints (purpose inferred from objective sequence; no explicit conditions in CLI output).
- Stage 255, 9999: cleanup (ShutDownStage flag on 255; CompleteQuest flag on 9999, possible redundancy or retry).

Open verification:
- Inspect QUST aliases directly (alias #0 = Altano, alias #5 = Daedra) if exact actor refs or forced refs matter.
- Inspect cell refs for The Bannered Mare (`01605E`) if precise trigger locations are needed.
- Decompile scripts `AoM03_TIF__01008850.Fragment_0`, `AoM03_TIF__0100886E.Fragment_0` if stage progression logic or branch outcome routing needs to be decoded.
- Verify stage 11, 15, 23, 25 purpose via quest trigger or in-game testing if intermediate checkpoints matter.
- Verify if stage 9999 is a fallback/retry path or dead code (appears unreachable if stage 40 completes quest first).
