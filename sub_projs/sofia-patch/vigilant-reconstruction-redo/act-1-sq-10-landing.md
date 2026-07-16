# Act 1 Sq 10 - Landing Spot

Status: source-grounded slice, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files.
- Short source snippets appear only when needed to explain a translation issue or branch polarity.
- Dialogue conditions and scene topic structure come from CLI diagnostics.

## Quest Record

[`011B75 zzzAoMMq10 "Landing Spot"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1)

CLI:
- `questdiag Vigilant.esm 0x011B75`
- `infodiag Vigilant.esm 0x011B75`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x011B75`
- EditorID: `zzzAoMMq10`
- Name: `Landing Spot`
- Flags: `RunOnce`
- Priority: `90`
- Type: `SideQuest`
- Filter: `AoM\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 5 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | none | "Molag Bal left a curse on me. My soul would be drawn into the realm of him at Second Corruption." |
| 39 | none | empty |
| 40 | CompleteQuest | empty |
| 50 | CompleteQuest | empty (duplicate line in ESM) |
| 255 | ShutDownStage | empty |
| 999 | none | CompleteQuest |
| 9999 | none | CompleteQuest |

Objectives from `questdiag`:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | Talk to Altano |
| 10 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | Destroy Mace of Molag Bal |
| 30 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:1) | Back To Temple of Stendarr |

Objective targets:
- 3 targets total (one per objective).
- Current CLI output does not print target refs; deeper QUST target dump needed if target locations matter.

## Alias / Staging Backbone

Aliases are not printed by CLI in this quest's `questdiag` output. From `infodiag` speaker patterns:
- Alias ReferenceAliasIndex=1: used by `zzAoMMq10B1*` branch (branch 013675).
- Alias ReferenceAliasIndex=6: used by `zzzAoMMq10B2*` branch (branch 028538).

Inference: alias #1 is the dying Altano (branch 1 happens during the confrontation); alias #6 is the survivor NPC in the temple aftermath (branch 2).

## Scene Records

Only one scene topic is explicitly present in `infodiag` output:

### 0x013BE5 (Unnamed Scene Topic)

From `infodiag`:
- Topic FormID: `0x013BE5` (Scene sub-type)
- Category: `Scene`
- SNAM: `SCEN`
- Priority: `50`
- Quest owner: `011B75:Vigilant.esm`
- Branch owner: none (unbranched)

INFO:
- FormID: `0x013BE6`
- Flags: none
- Prompt: empty
- Response: [`"Son of Stendarr..I see you. When your soul is corrupt, you open the gate of my realm..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:914)
- Emotion: Neutral
- VMAD: script file `AoM10_TIF__01013BE6` with OnEnd fragment

Translation:
- The response is a monologue by Molag Bal or a Bal-possessed entity, addressing the player during a climactic moment.
- Chinese translation pending verification; likely spoken during stage 30–40 transition where Molag Bal's curse manifests.

Note:
- `scenediag 0x013BE5` returns "not a Scene in Vigilant.esm", suggesting this is a scene-tagged DIAL topic rather than a true SCEN record. No phases/actions are present.

## Custom Dialogue Branch 1: Altano's Betrayal

Branch:
- `013675:Vigilant.esm` (Branch record, contains topics `013676`, `013678`, `01367A`)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#1`.
- This branch is stage-independent; no `GetStage` conditions.

Topics and INFOs:

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`013676 zzAoMMq10B1LastWord`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:913) | `013677` | none | `GetIsAliasRef alias #1` | [`"aa.....uaa..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:913) |
| [`013678 zzAoMMq10B1BetrayReason`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:915) | `013679` | none | `GetIsAliasRef alias #1` | Prompt: [`"Why did you do Such things?Altano?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:915) Response 1: [`"After the battle with Bal.....I heard sweet whispering....I can't help but follow the voice."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:916) Response 2: [`".....I yield to temptation....Excuse me....."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:917) |
| [`01367A zzAoMMq10B1BlessAltano`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:918) | `01367B` | `Goodbye` | `GetIsAliasRef alias #1` | Prompt: [`"Rest in peace,Altano...Stenndarr always with us."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:918) Response: [`"Thank you......I..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:919) VMAD: `AoM10_TIF__0101367B` with OnEnd fragment |

Translation notes:
- `"aa.....uaa..."` appears to be death gasps or corrupted speech.
- The `Goodbye` flag + VMAD on `01367B` suggest this topic completes or triggers a quest stage transition (likely stage 40 CompleteQuest).

## Custom Dialogue Branch 2: Temple Aftermath

Branch:
- `028538:Vigilant.esm` (Branch record, contains 6 topics from `028539` to `028543`)

Speaker condition pattern:
- All INFOs require `GetIsAliasRef == 1` on alias `#6` (except the opening INFO at `028539`, which has only the alias condition).
- Stage gating appears on `028539` only: `GetStage == 30` (opening only).

Topics and INFOs:

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`028539 zzzAoMMq10B2LastWord`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) | `02853A` | none | `GetStage == 30`; `GetIsAliasRef alias #6` | [`"Welcome back ... How was Altano...?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:920) |
| [`02853B zzzAoMMq10B2AltanoDead`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) | `02853C` | none | `GetIsAliasRef alias #6` | Prompt: [`"Altano died"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) Response: [`"You killed him....?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:921) |
| [`02853D zzzAoMMq10B2MolagKillHim`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) | `02853E` | none | `GetIsAliasRef alias #6` | Prompt: [`"No, Molag Bal killed him"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) Response: [`"I'm sorry ... that so ... Molag Bal appeared ... what soul we do..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:922) |
| [`02853F zzzAoMMq10B2DefeatMolag`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) | `028540` | none | `GetIsAliasRef alias #6` | Prompt: [`"I defeated Molag Bal. There is no danger for a while"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:923) Response 1: [`"I can not believe you did defeated Molag Bal ... Molag Bal is Daedra Lord..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:924) Response 2: [`"It is incredible... but the eyes are saying that it is true. All right, I believe you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:924) |
| [`028541 zzzAoMMq10B2DoNext`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) | `028542` | none | `GetIsAliasRef alias #6` | Prompt: [`"What should we do now?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) Response 1: [`"Well, you and I are suvivor in temple... Keeper died ..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:925) Response 2: [`"I got it!! You should become new keeper of Stendarr  because you have power defeated Molag Bal"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:926) |
| [`028543 zzzAoMMq10B2DecideKeeper`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) | `028544` | `Goodbye` | `GetIsAliasRef alias #6` | Prompt: [`"Can we decide it?"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) Response 1: [`"It's OK!! Stendharr will admit you. I'm belive you"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:927) Response 2: [`"I'm sure it's okay if you. Okay ..."`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:928) VMAD: `AoM10_TIF__01028544` with OnBegin fragment (Fragment_1) and OnEnd fragment (Fragment_0) |

Branch polarity:
- Player's choices in `02853B` → `02853D` represent **blame attribution** (did player kill Altano, or did Molag Bal?).
- `02853F` is a follow-up that tests if the player claims to have defeated Molag Bal.
- `028541` pivots to **future role**: surviving keeper is absent; player becomes new keeper.
- `028543` (marked `Goodbye`) is the closing topic; VMAD on `028544` likely triggers stage 40 `CompleteQuest` or quest finalization.

Translation notes:
- "suvivor in temple" contains a typo (should be "survivor"); source is preserved as-is.
- Stage 30 `GetStage` condition on `028539` gates the opening greeting; after player completes main quest objective (defeating Molag Bal or Altano), this conversation begins.

## Quest Flow Summary

1. **Stage 0–30**: Player approaches the temple or encounters Altano's corruption.
2. **Stage 30**: Quest log entry appears; [`013BE5` scene topic](#scene-topic-0x013be5) plays Molag Bal's curse monologue.
3. **Branch 1** (`zzAoMMq10B1*`): Player interacts with dying/corrupted Altano (alias #1) via topics `013676–01367A`.
   - If player chooses to bless Altano (`01367A`), script fragment `AoM10_TIF__0101367B` executes.
4. **Stage 40**: Quest marked `CompleteQuest`; player returns to temple.
5. **Stage 50**: Another `CompleteQuest` entry (likely a duplicate or fail-safe in ESM).
6. **Branch 2** (`zzzAoMMq10B2*`): At stage 30+, player talks to survivor (alias #6, likely same NPC as Thorondir or another keeper).
   - Dialogue chain: blame Altano vs. Molag Bal → player claims victory → role negotiation.
   - Final topic `028543` (Goodbye, VMAD) likely triggers stage 40 completion.

## Related Records

These are not all part of quest `011B75` according to `infodiag`, but are referenced by dialogue or narrative context:

NPCs:
- [`zzzAoMMq07` alias context](#) — Altano appears in both quest 7 (Old Paladin) and quest 10 (Landing Spot); quest 10 may be a direct continuation.

Items:
- Mace of Molag Bal — mentioned in objective 10, but FormID not extracted in dialogue.

## Reconstruction Notes

Source-grounded:
- This quest represents a confrontation with Altano after he is tempted/corrupted by Molag Bal.
- The quest has two dialogue branches: one during the climactic moment (Altano's death/corruption), one in the temple aftermath.
- A scene topic (`013BE5`) delivers Molag Bal's curse monologue, but it is a topic-only record, not a true SCEN with phases/actions.
- The quest completes after the survivor (alias #6) and player reach consensus on a new keeper role.

Open verification:
- Inspect scripts `AoM10_TIF__0101367B` and `AoM10_TIF__01028544` if source exists; they likely contain stage progression logic.
- Inspect QUST alias definitions directly (if a richer alias dump exists) to confirm alias #1 = Altano, alias #6 = survivor.
- Verify cell/location where branch 1 dialogue occurs (likely Coldharbour or a quest interior).
- Verify the Mace of Molag Bal FormID and confirm objective 10 is destruction of a specific item instance.
- Determine if stages 40 and 50 represent different completion paths (player choice outcome) or if stage 50 is a vestigial duplicate.
