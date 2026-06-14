# Act 3 Child of Oblivion - 065932 zzzCOMq01

Status: first redo slice (Act 3 quest 01). Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain translation difficulty or ambiguity.
- `SCEN` staging comes from extracted text markers; full diagnostics require CLI when on full dev machine.

## Quest Record

[`065932 zzzCOMq01 "Child of Oblivion"`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276)

CLI (when available on full machine):
- `questdiag Vigilant.esm 0x065932`
- `infodiag Vigilant.esm 0x065932`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from extracted `quests.md`:
- FormID: `Vigilant.esm:0x065932`
- EditorID: `zzzCOMq01`
- Name: `Child of Oblivion`
- Type: Presumed story/quest (Act 3 mansion arc)

Objectives from `quests.md`:

| Index | Source | Text |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:277) | Talk to Gwyneth |
| 20 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:278) | Go to Noble Mansion |
| 30 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:279) | Investigate the mansion and to solve the case |
| 60 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:280) | Defeat Julius |
| 70 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:281) | Escape from Mansion |
| 80 | [quest objective](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:282) | Martyrdom or Corruption |

Inference:
- Objective stage 80 (`Martyrdom or Corruption`) suggests branching moral choice.
- The quest involves investigation, combat (`Defeat Julius`), and an escape mechanic.
- `Julius` is identified as `zzzCOJuliusChildOblivion` or `zzzCOJulius` in NPC records.

## Key Characters (NPCs)

From extracted `npcs.tsv`:

| FormID | EditorID | Name | Role |
|---|---|---|---|
| `02749A` | `zzzAoMVigilantLibrarian` | Gwyneth | Quest giver; Vigilant librarian |
| `061404` | `zzzCOJulius` | Julius | Quest target/antagonist |
| `0461D8` | `zzzCOJuliusChildOblivion` | Julius (Child of Oblivion form) | Alternative or phase form |
| `04BFF2` | `zzzCOJuliusDwarvenSpider` | [no name] | Possible minion/trap |
| `3288E4` | `zzzCODregsOfSithisJulius` | Julius | Sithis-corrupted variant |

## Location Contexts

From extracted `locations.tsv`:

| FormID | EditorID | Type | Name |
|---|---|---|---|
| `04A8B9` | `zzzCONobleMansion01` | CELL | South Bruiant Mansion |
| `04DC3F` | `zzzCONobleMansion02` | CELL | North Bruiant Mansion |
| `060D39` | `zzzCONobleMansion03` | CELL | South Bruiant Mansion |
| `2EBC0B` | `zzzCONobleMansionBasement` | CELL | Basement |
| `04F6C8` | `zzzCOUnderMansion` | CELL | Hidden Room |
| `3678E9` | `zzzCOLocBruiantMansionSouth` | LCTN | South Bruiant Mansion |
| `3678EA` | `zzzCOLocBruiantMansionNorht` | LCTN | North Bruiant Mansion |
| `3786FE` | `zzzCOLocBruiantMansionHidden` | LCTN | Hidden Room |

The mansion appears to be a multi-cell structure: south wing, north wing, basement, and hidden room.

## Dialogue Branches

### A. Gwyneth (Librarian) — Initial Quest Dialogue

Branch owner: `zzzAoMVigilantLibrarian` (Gwyneth, `02749A`)

#### Opening and initial dispatch

| Topic | INFO | Conditions | Translation |
|---|---|---|---|
| [`0669E8 zzzCOq01LibB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:775) | (no INFO index extracted) | stage-gated at 0 | Prompt: 「I wonder if a nice little? There is a consultation.」 Response: 「Thought was gathered?」 |
| [`0669EA zzzCOq01LibB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:779) | (no INFO index extracted) | (unspecified) | Prompt: 「Help me later」 Response: 「Later also, I found」 |
| [`0669EC zzzCOq01LibB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:782) | (no INFO index extracted) | (unspecified) | Prompt: 「What's the matter? Gwyneth」 Response: 「Letter seeking reinforcements from guard was sent to the noble mansion of Chorrol is I came / The 'm want to send the keeper anytime soon …」 |
| [`0669EF zzzCOq01LibB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:786) | (no INFO index extracted) | (unspecified) | Prompt: 「Let me preceding. If I do not return even after week, send vigilants to me.」 Response: 「All right. I So be cautious. I'll set aside the horse-drawn carriage on the way to the house in front of the Cathedral」 |
| [`0669F1 zzzCOq01LibB01T05`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:789) | (no INFO index extracted) | (unspecified) | Prompt: 「Let me think」 Response: 「It is solid … more」 |

#### Follow-up branches

| Topic | INFO | Conditions | Translation |
|---|---|---|---|
| [`0669F4 zzzCOq01LibB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:792) | (no INFO index extracted) | stage-gated at 20+ | Prompt: 「Name of the vigilant was sent to?」 Response: 「Baltholo. Because it was sent before you become a keeper, I should never met」 |
| [`0669F6 zzzCOq01LibB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:795) | (no INFO index extracted) | stage-gated at 20+ | Prompt: 「What requested content was something?」 Response: 「It 's was something that I want to investigate unnatural death is because one after another in the mansion / Thorondir was said to be a coincidence, but I remember it from had words Waruforo will not withdraw / So Balthoro is but he was headed to one Hall …」 |
| [`0669F9 zzzCOq01LibB03T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:800) | (no INFO index extracted) | stage-gated at 20+ | Prompt: 「Tell me about the noble.」 Response: 「I nobleman who built a fortune in training client blue house, the military dog. I'm about having a seat in the Senate now. / Marx of the father, the mother of Julia, family configuration, one person Julius son / Yulia mother's death last year. I'll found a burned body in the lake Irinaruta / Report at that time by this, I read in the carriage」 |
| [`06C21E zzzCOq01LibB01T06`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:891) | (no INFO index extracted) | stage-gated at 50+ | Prompt: 「Have you got a problem?」 Response: 「It seemed to lag letter to be reachable in civil war, letter 's thing a few months ago / The keeper is not come back, and not a maybe too late / Do you want to leave the decision to you. How Am I supposed to do?」 |

Translation notes:
- Extracted text uses awkward English (appears to be machine-translated Japanese). Rendering as-is for now; Chinese re-translation needed per user workflow.
- "Baltholo" is the vigilant sent earlier; likely same as "Balthoro" in quest dialogue below.
- "Thorondir" and "Waruforo" appear to be NPC names requiring ESM verification.
- "Marx" (father), "Julia"/"Yulia" (mother and possible child), "Julius" (son) — family relationships need clarification.

### B. Balthoro — Mansion Entry Dialogue

Branch owner: (likely `zzzCOJulius` or quest-owned alias)

| Topic | INFO | Conditions | Translation |
|---|---|---|---|
| [`066A23 zzzCOq01BalB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:810) | (no INFO index extracted) | (unspecified) | Prompt: (unknown) Response: 「Had been waiting for. My husband is waiting. Please, into the mansion / Back off me. Please, into the mansion. My husband is waiting for you.」 |
| [`066A26 zzzCOq01BalB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:814) | (no INFO index extracted) | (unspecified) | Prompt: 「The rare people of Ayreid」 Response: 「Fugitive from justice just, things to tell There is not now.」 |
| [`066A28 zzzCOq01BalB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:817) | (no INFO index extracted) | (unspecified) | Prompt: 「Unpleasant name...」 Response: 「It is a name that is often the case with elf. I do not have the unusual name.」 |
| [`066A2A zzzCOq01BalB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:820) | (no INFO index extracted) | (unspecified) | Prompt: 「Letter has came from Balthoro but …」 Response: 「Balthoro is also waiting in the mansion. Well, into the mansion」 |

Inference:
- Speaker is likely Balthoro's wife or an NPC greeting the player at mansion entrance.
- "Ayreid" suggests Ayleid (ancient elf) connection.
- "My husband is waiting" suggests Balthoro is inside the mansion.

Scene marker:
- [`066A2D` [Scene/Scene]](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:823): 「It is stubborn at all guests. If you do not imperative dead soon」

### C. Molag Bal — Confrontation Dialogue (Corruption Branch)

Branch owner: Molag Bal (likely alias in quest)

| Topic | INFO | Conditions | Translation |
|---|---|---|---|
| [`066A31 zzzCOq01MolagB01T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:826) | (no INFO index extracted) | (unspecified) | Prompt: (unknown) Response: 「The brittle, Do not brittle, fragile thing or what the only person …」 |
| [`066A33 zzzCOq01MolagB01T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:829) | (no INFO index extracted) | (unspecified) | Prompt: 「Also do the work of you?」 Response: 「This Moragu Val an unexpected were just alms Those who were hungry. Thanks not remember Saredo be condemned. / Is a miracle, we are also in the emperor. But, he is also a bread more than anything else. For Those who hunger, and none other than the bread on the」 |
| [`066A35 zzzCOq01MolagB01T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:833) | (no INFO index extracted) | (unspecified) | Prompt: 「Why do you play with people …」 Response: 「Is nausea. Anger from my eyes each time to find a Eseriusu in you bastards / All of Mundasu, all of creation is completely innocent mind. Brilliant figure the person which is not trivial bastards are completely innocent / You bastards also Eseriusu also Can you swallow all. To my gut all, my Ayumi I lightning to it」 |
| [`066A37 zzzCOq01MolagB01T04`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:838) | (no INFO index extracted) | (unspecified) | Prompt: 「Molag Bal, What is the purpose?」 Response: 「I came to save the differents. Flames of hatred of Julius from Na will immolate the differents soon. / Hatred of that little child is strong, flame will not to disappear until immolate you. Also by any chance, the flame not to fit as when Ramae / That's why this is Moragu Bal's reaching out to differents.」 |
| [`066A3A zzzCOq01MolagB02T01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:843) | (no INFO index extracted) | (unspecified) | Prompt: (unknown) Response: 「Well, I will listen to hope / The Kugure the copper? Door. Should not, such as time to stop walking」 |
| [`066A3C zzzCOq01MolagB02T02`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:847) | (no INFO index extracted) | (unspecified) | Prompt: 「I does not deal with Daedra」 Response: 「Do you pray to God? Stendhal is not trying to save the poor differents. That you provide about yourself will die in a fire here keeps to's the hope. / Edora can not even hug yo phony after all, not even able to meet even hungry bastards, the bastards that freezing cold / However, this Moragu Bal different. The fuckable to save the differents. It's fuckable Let me also many of the miracle / Here, we are tries to continue to reach out. Until then the bone and flesh and you have a high」 |
| [`066A3E zzzCOq01MolagB02T03`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:853) | (no INFO index extracted) | (unspecified) | Prompt: 「Help me out of here」 Response: 「That hope. Let Kikiireyo」 |

Scene markers:
- [`066A44` [Scene/Scene]](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:859): 「Just give up ease the pain. It 's throw away any hope of.」

Inference:
- `Molag Bal` dialogue addresses the player as "differents" (non-human; possibly Oblivion-touched).
- The quest offers a Daedric bargain ("corruption" path of objective 80).
- "Julius from Na" and "that little child" suggest Julius is possessed or corrupted by a child entity or power.
- The extracted text quality is severely degraded; full re-translation required.

Translation issues flagged:
- "Eseriusu" — unknown term; possibly a name or entity (requires ESM check).
- "Mundasu" — Mundus + Japanese possessive?
- "Moragu Val" — Molag Bal + phonetic corruption.
- "Stendhal" — Stendarr (Vigilant's deity).
- "Edora" — possibly Akatosh or another divine reference.
- Vulgar language and incoherent phrasing throughout; indicates severe encoding or extraction error in source dialogue file.

## Related Records

These records are not directly owned by quest `065932` but appear in the quest context:

NPCs:
- [`02749A zzzAoMVigilantLibrarian` - Gwyneth](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — Vigilant librarian, quest giver.
- [`061404 zzzCOJulius` - Julius](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — primary antagonist; possible possession victim.
- [`0461D8 zzzCOJuliusChildOblivion` - Julius (Child of Oblivion form)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — transformed/boss form.
- [`3288E4 zzzCODregsOfSithisJulius` - Julius (Sithis variant)](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — alternate corruption path.
- [`12339D zzzCHMolagBal` - Molag Bal](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:?) — Daedric voice/influence in mansion.

Locations:
- South Bruiant Mansion (cells `zzzCONobleMansion01`, `04A8B9`)
- North Bruiant Mansion (cells `zzzCONobleMansion02`, `04DC3F`)
- Mansion basement (`zzzCONobleMansionBasement`, `2EBC0B`)
- Hidden room / under mansion (`zzzCOUnderMansion`, `04F6C8`)

## Reconstruction Notes

Source-grounded:
- This quest is represented by [`065932 zzzCOMq01`](/home/lorkhan/repo/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:276) with seven objectives spanning investigation, combat, and moral choice.
- It contains multiple dialogue branches owned by quest-linked aliases (Gwyneth, Balthoro, Molag Bal).
- The quest involves a multi-cell mansion environment with potential hidden areas.
- Objective 80 (`Martyrdom or Corruption`) signals a branching ending with at least two outcomes.

Source-quality warnings:
- Extracted dialogue text exhibits severe degradation: incoherent English, likely from poor OCR or mechanical translation of Japanese source.
- Many Topic/INFO FormIDs are not paired with full condition/flag data in the extraction (limitations of the extraction format).
- Scene records (`066A2D`, `066A44`) lack phase/action data; full `scenediag` output required for staging detail.

Open verification:
- Run `questdiag Vigilant.esm 0x065932` to confirm stage count, CompleteQuest triggers, and priority.
- Run `infodiag Vigilant.esm 0x065932` to list all owned topics and cross-check against dialogue branches above.
- For each topic (e.g., `0669E8`, `066A23`), run `infodiag Vigilant.esm 0x<formid>` to extract INFO flags, conditions (`GetStage`, `GetIsAliasRef`, `GetItemCount`, etc.), and VMAD fragments.
- Decompile VMAD fragments on Balthoro and Molag Bal dialogue to identify stage progression and branch polarity (Martyrdom vs. Corruption).
- Extract full scene action data for `066A2D` and `066A44` via `scenediag`.
- Verify NPC records (`zzzCOJulius*` variants, `zzzCHMolagBal`) for class, perks, equipment, dialogue race/gender conditions.
- Check for hidden mechanics: cursed items, possession scripts, or transformation mechanics on Julius NPC records.
- Investigate location cells for trapped refs, puzzle elements, or stage-gated lock/key interactions.

Open translation work:
- Full re-translation of Molag Bal dialogue required (current extraction is severely corrupted).
- Verify names and terms: "Eseriusu", "Thorondir", "Waruforo", "Marukh", "Ramae", proper noun anchors in Act 4 memory context.
- Cross-check Julius family genealogy against quest text and any books/journals in the mansion.
- Clarify "Child of Oblivion" theme: is Julius reborn/transformed as a Daedric entity, or possessed by one?
