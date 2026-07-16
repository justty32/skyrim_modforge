# Act 4 Memory 03 - Knight of Hound

Status: redo slice. Source-grounded, link-first, not a plot summary.

Source policy:
- Original lines are linked back to extracted source files instead of copied in full.
- Short source snippets appear only when needed to explain a translation issue.
- This quest owns **no `SCEN` records** (no `…Sc…` topics in `find`, and `scenediag 0x13965A` reports "not a Scene"). Staging is driven by **force-greet AI packages** instead; see Staging Backbone.

## Quest Record

[`13965A zzzCHMemoryQuest03 "Knight of Hound"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154)

CLI:
- `questdiag Vigilant.esm 0x13965A`
- `infodiag Vigilant.esm 0x13965A`

ESM:
- `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

Quest metadata from `questdiag`:
- FormID: `Vigilant.esm:0x13965A`
- EditorID: `zzzCHMemoryQuest03`
- Name: `Knight of Hound`
- Flags: `RunOnce`
- Priority: `90`
- Type: `Misc`
- Filter: `CH\`

Stages from `questdiag`:

| Stage | Flags | Log |
|---:|---|---|
| 0 | none | empty |
| 1 | none | empty |
| 10 | none | empty |
| 20 | none | empty |
| 30 | CompleteQuest | empty |
| 40 | none | empty |
| 100 | none | empty |
| 105 | none | empty |
| 110 | none | empty |
| 120 | none | empty |
| 130 | CompleteQuest | empty |
| 999 | ShutDownStage | empty |

Two-band `CompleteQuest` at **30** and **130** — the karma/branch signature noted in the index. Polarity mapping below.

Objective:

| Index | Source | Translation |
|---:|---|---|
| 0 | [quest objective](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:155) | 血脈從不分離，而是相連。 |

Objective targets:
- 1 target in ESM, 0 conditions. CLI does not print the target ref; needs a deeper QUST target dump if the target location matters.

## Cast (subject / speakers)

The player relives the memory **as the knight "Varla"** (the second-person addressee in nearly every Emperor line; `Varla` is also a major late-game boss — see [`0E6A48 zzzCHBossVarla "Varla the Human Hunter"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577)). Memory NPCs spoken to:

| Role | NPC | Notes |
|---|---|---|
| Emperor (subject) | [`137E63 zzzCHBelharzaMemory "Belharza the Man"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577) | The Emperor branch (`B01`–`B05`); foster-father to Varla (inference, from "son of the real you" / "as father"). |
| Enola (child) | [`137E65 zzzCHEnolaMemory "Enola"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:578) | The Ayleid child survivor Varla spares; her skull is an item, [`13965E zzzCHEnolaSkullFull "Enola's Skull"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/items.tsv:977). |
| Ja'zhan (memory) | [`139094 zzzCHJazhanMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:582) | Khajiit fisherman; `GetIsID` speaker of the Jazhan branch. |
| Ritho (memory) | [`23611E zzzCHRithoMemory "Ritho"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:695) | Giant knight, Varla's comrade; `GetIsID` speaker of the Ritho branch. |
| Bard | alias `#5` (not resolved by CLI here) | Sings of "Eroisa and Polydor" at the departure. |

## Staging Backbone (packages, not scenes)

No `SCEN` records. Force-greet AI packages carry the staging:

- [`139C40 zzzCHMeq3EmperorForceGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577) (inference: Emperor approaches Varla)
- [`139C38 zzzCHMeq3BardForceGreet`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:577)
- `139C3B zzzCHMeq3EnolaForceGreet`, `139C39 zzzCHMeq3EnolaCaptive`, `139C3A zzzCHMeq3EnolaFollowPlayer` — Enola goes captive → follows the player (the spared child escorted out).

Alias indices used by INFO conditions (`GetIsAliasRef`):
- alias `#0` — Emperor (Belharza) — all `EmperorB0x` and `B04T02` INFOs.
- alias `#1` — Enola — the `EnolaB01`/`EnolaB02` INFOs.
- alias `#5` — Bard — the `BardB01` INFOs.
- Ja'zhan and Ritho are gated by **`GetIsID`** on the NPC FormID, not by alias.

## Dialogue Branches

All topics are `cat=Topic sub=Custom SNAM=CUST prio=50`, owned by quest `13965A`. Source lines are garbled machine-English; translations are best-effort with `Note:` where unresolved. zh-TW kept faithful, no dropped lines.

### Emperor branch B01 — `139660` ([opening](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1790))

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139661 …EmperorB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1790) | `139662` | `SayOnce, WalkAway` | alias `#0` | 「拿下 Mackamentain 是場硬仗。但這還不能說我們離 Malada 更近了一步。」 Note: `Mackamentain` 為地名/人名，待驗證。 |
| [`139663 …EmperorB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1793) | `139664` | `SayOnce`; VMAD `CHMeq3_TIF__02139664.Fragment_0` (OnEnd) | alias `#0` | Prompt:「我深感榮幸，陛下。」 Response:「Varla，別這麼嚴肅。血緣雖出乎意料卻相連——但我把你當作真正的、我的兒子來信任。」 Note: 原文「Blood unexpected yet connected」語意不清。 |

### Emperor branch B02 — `139665`

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139666 …EmperorB02T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1797) | `139667` | none | `GetStage==20`; alias `#0` | Prompt:「您為何執著於 Malada？」 Response:「Alessia 教團想把它當作禱告之地。他們說在那裡禱告也能窺見 Shezarr 的下落。若能找到 Shezarr 的下落，就沒有理由不攻下 Malada——這也是為了帝國。」 |

### Emperor branch B03 — `139668`

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139669 …EmperorB03T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1801) | `13966A` | none | `GetStage==20`; alias `#0` | Prompt:「Meridia 的 Auroran 出現了。」 Response (Disgust/Anger/Fear):「那女人壞到不肯罷手。Umaril 的落敗讓她非常不甘，否則她不會出手相助沒落的 Ayleid。」／「Ayleid 也真是的，在 Shiki 神廟裡鬧出這種事。若早點放棄，他本不必死。」／「真是蠢透了。必須加速推行 Alessia 教義。那麼，我該和 Borgas 談一談。」 Note:`Umariru`=Umaril、`Shiki`、`Borgas` 為專名，待驗證。 |

### Emperor branch B04 — `13966B` (the choice)

This branch holds the **fork**. Varla is ordered to kill the surviving Ayleid child; the player picks acceptance (`B04T02`) or refusal (`B04T03`→…→`T07`).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`13966C …EmperorB04T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1806) | `13966D` | `WalkAway` | `GetStage==20`; alias `#0` | Prompt:「那名倖存者，我們該怎麼處置？」 Response:「Varla，即使是婦孺，Ayleid 也必須處死。記住，身為帝國騎士要捨棄那份多愁善感。」 |
| [`13966E …B04T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1810) | `13966F` | `Goodbye, SayOnce`; VMAD `CHMeq3_TIF__0213966F.Fragment_0` (OnBegin) | alias `#0` | Prompt:「是，陛下……」 Response:「很好，Varla。殺掉 Ayleid。好的 Ayleid 只有死掉的那種。」 — **服從分支（殺孩）**。 |
| [`139670 …EmperorB04T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1813) | `139671` | `WalkAway` | alias `#0` | Prompt:「我也流著 Ayleid 之血。」 Response:「為何……你竟知道此事？不，看得出來。是那個怪異吟遊詩人暗示你的嗎？聽著，Ayleid 拋棄了你——他們把剛出生的你丟進 Rumare 湖。若非 Imga 的先知拾起你，你早成了魚食。即便如此，你仍要選擇 Ayleid 的血嗎？」 Note:`Imuga`=Imga，待驗證。 |
| [`139672 …EmperorB04T04`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1818) | `139673` | `WalkAway` | alias `#0` | Prompt:「我的選擇不是血脈。」 Response:「Varla，把你當作我真正的兒子，我才慎重地說。我不是把你收作騎士了嗎？人之子啊，我本該把你當作 Shezarr 之子養大。難道只能為帝國吶喊、揮劍嗎？」 |
| [`139674 …EmperorB04T05`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1822) | `139675` | `WalkAway` | alias `#0` | Prompt:「身為父親，我一直渴望（您的認可），陛下。」 Response:「我關心你。所以你必須處死那個誤導你的 Ayleid。你明白我的意思嗎？」 |
| [`139676 …EmperorB04T06`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1825) | `139677` | `WalkAway` | alias `#0` | Prompt:「我準備好了。為了那小女孩的性命，求您。」 Response:「……既然如此，也罷。就到今天為止吧。帶著那不潔的小女孩，去你想去的任何地方。」 — **饒恕分支（放走 Enola）**。 |
| [`139678 …EmperorB04T07`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1828) | `139679` | `Goodbye, SayOnce`; VMAD `CHMeq3_TIF__02139679` (OnBegin Fragment_1, OnEnd Fragment_0) | alias `#0` | Prompt:「謝謝您，陛下。」 Response:「……三天後，往 Alinor 的最後一班船。搭上它。」 |

### Emperor branch B05 — `13967A` (bad-ending gate)

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`13967C …EmperorB05T01b`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1831) | `13967D` | `Goodbye` | `GetStage==30`; alias `#0` | Response:「去把那女孩殺了。我是為你好才說的。」 |

**Polarity (inference, source-grounded):** `B05T01b` is gated on `GetStage==30` (the first `CompleteQuest`), and its content reaffirms the kill order → **stage 30 = the "kill the child / obey" (bad/corruption) completion**. The refusal chain (`B04T03`→`T07`) ends with passage to Alinor and Enola following the player out (Enola follow/captive packages, `EnolaB02` "Mam…" at `GetStage<100`) → **stage 130 = the "spare Enola / mercy" (good) completion**. The two `CompleteQuest` stages thus map: **30 = obey/kill (bad), 130 = spare/exile (good)** (inference from condition gating + branch content; not stated in stage logs, which are empty).

### Bard branch B01 — `139C25`

Speaker: alias `#5` (Bard).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139C26 …BardB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1834) | `139C27` | `WalkAway` | alias `#5` | 「噢，是 Varla 大人嗎？聽說您捨棄了騎士的身分。」 |
| [`139C28 …BardB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1837) | `139C29` | `WalkAway` | alias `#5` | Prompt:「你的來意是？」 Response:「正逢您期盼已久的啟程，我想為您獻上一曲。我要唱 Eroisa 與 Polydor 的故事。」 Note:`Eroisa`、`Polydor` 待驗證。 |
| [`139C2A …BardB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1840) | `139C2B` | `Goodbye, SayOnce`; VMAD `CHMeq3_TIF__02139C2B.Fragment_0` (OnEnd) | alias `#5` | Prompt:「請容我推辭。你的歌太悲傷了。」 Response:「真可惜。有緣再續此曲。那麼，祝您一路順風。」 |

### Enola branch B01 — `139C2D`

Speaker: alias `#1` (Enola).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139C2E …EnolaB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1843) | `139C2F` | `WalkAway` | alias `#1` | 「我們搭這艘船要去哪裡？」 |
| [`139C30 …EnolaB01T02`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1846) | `139C31` | `WalkAway` | alias `#1` | Prompt:「Alinor。精靈之島。」 Response:「那裡是個好地方嗎？」 |
| [`139C32 …EnolaB01T03`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1849) | `139C33` | `Goodbye`; VMAD `CHMeq3_TIF__02139C33.Fragment_0` (OnEnd) | alias `#1` | Prompt:「一定是個好地方。來吧，我們走。」 Response:「嗯。」 |

### Enola branch B02 — `139C3C`

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139C3D …EnolaB02T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1855) | `139C3E` | `Goodbye` | `GetStage<100`; alias `#1` | Response (Sad):「媽……」 Note:原文`Mam...`。`GetStage<100` 表示僅在前半段（尚未進入 100+ 結局段）有效。 |

### Ja'zhan branch B01 — `139C35`

Speaker gated by `GetIsID` on [`139094 zzzCHJazhanMemory`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:582).

| Topic | INFO | Flags | Conditions | Translation |
|---|---|---|---|---|
| [`139C36 …JazhanB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:1852) | `139C37` | `Goodbye` | `GetIsID 139094` | Prompt:「釣得好嗎？」 Response:「不行啊。我餵 Alessia 金幣，魚根本不上鉤。可惡的吟遊詩人，Khajiit 被騙了。」 Note:原文`Kajito`=Khajiit。 |

### Ritho branch B01 — `236131`

Speaker gated by `GetIsID` on [`23611E zzzCHRithoMemory "Ritho"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/npcs.tsv:695). One topic, four INFOs (random pool + a late-stage variant).

| INFO | Flags | Conditions | Translation |
|---|---|---|---|
| `236133` | `Goodbye, Random` | `GetStage<=40`; `GetIsID 23611E` | 「Belharza 大人太性急了……我們不必攻下這座城……」 |
| `236134` | `Goodbye, Random` | `GetStage<=40`; `GetIsID 23611E` | 「那裡只有婦女、孩童和手無寸鐵的祭司。這稱不上戰爭……」 |
| `236135` | `Goodbye, Random, RandomEnd` | `GetStage<=40`; `GetIsID 23611E` | 「Varla，我的朋友。這場戰爭的目的究竟是什麼……我不懂小個子們的想法。」 |
| `236136` | `Goodbye` | `GetStage>=100`; `GetIsID 23611E` | 「願你健康，Varla。把帝國交給我們，走你自己的路吧。」 |

Topic anchor: [`236132 zzzCHMeQ03RithoB01T01`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/dialogue.md:2361). The `GetStage>=100` INFO confirms the 100+ band is the post-fork "good"/exile path (Ritho sends Varla off rather than to battle).

## Branch Routing (VERIFIED via psc)

Full `choice → SetStage → CompleteQuest` chain, read from TIF + QF fragments:

| Player choice | TIF file | Fragment | SetStage | QF effect |
|---|---|---|---|---|
| B01T02 "I am honored" (accepted) | `chmeq3_tif__02139664.psc:9` | `Fragment_0` OnEnd | **20** | Advances to B02/B03/B04 |
| B04T02 "Yes, Your highness..." (obey/kill) | `chmeq3_tif__0213966f.psc:9` | `Fragment_0` OnBegin | **30** | QF Fragment → FadeOut(black), MoveTo Alias_Knight, `Stop()` — **bad CompleteQuest** |
| B04T07 "Thank you, Your Majesty" (spare) OnBegin | `chmeq3_tif__02139679.psc:18` | `Fragment_1` OnBegin | **100** | QF Fragment → FadeOut, Enola.TryToMoveTo(TravelMarker), Player.MoveTo(Marker02), Bard.EvalPackage — ship departure |
| B04T07 "Thank you, Your Majesty" (spare) OnEnd | `chmeq3_tif__02139679.psc:9` | `Fragment_0` OnEnd | **105** | QF Fragment → `Karma.Mod(3.0)`, `KarmaUp.Show()` — **good karma reward** |
| BardB01T03 "Let me refrain" (goodbye) | `chmeq3_tif__02139c2b.psc:9` | `Fragment_0` OnEnd | **110** | QF Fragment → Enola+Bard.EvalPackage |
| EnolaB01T03 "Come on, let's go" | `chmeq3_tif__02139c33.psc:9` | `Fragment_0` OnEnd | **120** | QF Fragment → Enola.EvalPackage — board ship |

**Stage 130** (good `CompleteQuest`): QF Fragment → `SetObjectiveCompleted(0)`, `qGuide.SetStage(30)`, `kmyQuest.ModRadiance(3.0)`. Fires after stage 120 (inference; ordering from stage band 100→105→110→120→130).

**Stage 40** (bad branch sub-step): QF Fragment → `Karma.Mod(-3.0)`, `KarmaDown.Show()`, Enola `SetGhost(false)` / `SetInvulnerable(false)` / `SetEssential(false)` / `SetAV("Health", 1)` — makes Enola killable after the obey choice.

**Polarity confirmed:** stage 30 = bad/obey/kill (`chmeq3_tif__0213966f.psc` `SetStage(30)`; QF FadeOut black + Stop); stage 130 = good/spare/exile (`SetObjectiveCompleted`, `qGuide.SetStage(30)`, `Radiance+3.0`, preceded by `Karma.Mod(+3.0)`). Previously marked "(inference)" — **RESOLVED** via psc.

### Hub wiring (MemoryGuide)

QF Fragment_24 (stage 130 good completion) calls `qGuide.SetStage(30)` on `Quest Property qGuide` (`42E0B1 zzzCHMemoryGuide`). MemoryGuide stage 30 is one of 13 "Dream finished" stages (stages 10–90 per dream, 100–120 are the Song-of-Pelinal trio). MeQ03 maps to MemoryGuide **stage 30** (Dream03). Karma is tracked via `GlobalVariable Property kARMA` + `Karma.Mod(±3.0)` on each branch; `AoMAchievementPointQuestScript.ModRadiance(3.0)` also fires on the good completion.

## Quest Aliases (VERIFIED via QF psc)

From `qf_zzzchmemoryquest03_0213965a.psc` `ALIAS PROPERTY` block:

| Alias index (from infodiag) | Property name | Type | Notes |
|---|---|---|---|
| `#0` | `Emperor` | ReferenceAlias | Belharza (`137E63 zzzCHBelharzaMemory`) — confirmed by infodiag `ReferenceAliasIndex=0` on all Emperor INFOs |
| `#1` | `Enola` | ReferenceAlias | Enola (`137E65 zzzCHEnolaMemory`) — confirmed by infodiag `ReferenceAliasIndex=1` on Enola INFOs |
| `#5` | `Bard` | ReferenceAlias | confirmed by infodiag `ReferenceAliasIndex=5` on BardB01 INFOs; **bard ref = unverified (alias target not printed by CLI)** |
| — | `Knight` | ReferenceAlias | MoveTo target in QF Fragment_4 / Fragment_2 — end-of-quest player teleport anchor |
| — | `EnolaSkull` | ReferenceAlias | `13965E zzzCHEnolaSkullFull` — presumably placed in scene and acquired on bad branch |
| — | `ValraMemory` | LocationAlias | the memory cell location |
| — | `MemoryMarker01` | ReferenceAlias | player start marker (stage 0 Fragment_0) |
| — | `MemoryMarker02` | ReferenceAlias | player departure marker (stage 100) |
| — | `EnolaTravelMarker` | ReferenceAlias | Enola teleport target at ship departure |

Alias count vs infodiag: alias indices 0, 1, 5 confirmed by `ReferenceAliasIndex` in infodiag. Alias `#2`–`#4` and indices 6+ are unassigned or unnamed in the psc (QF lists 9 aliases total; numeric index assignment by ESM ordering is not printed by CLI). Bard's actor ref (the `#5` alias fill target) is **unverified** — CLI alias-target dump not available.

## Reconstruction Notes

Source-grounded:
- This memory is [`13965A zzzCHMemoryQuest03 "Knight of Hound"`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:154), objective [`Blood never separate, but join.`](/home/lorkhan/repo/moddings/skyrim/projects/ModForge/sub_projs/sofia-patch/game-data/mods/Vigilant/quests.md:155).
- The player relives the knight **Varla**, ordered by **Emperor Belharza** to execute a surviving Ayleid child, **Enola**, after the sack of Malada.
- **No `SCEN` records.** 11 custom topics across 7 dialogue branches: Emperor `B01`–`B05`, Bard `B01`, Enola `B01`/`B02`, Ja'zhan `B01`, Ritho `B01`.
- **Branch routing confirmed (psc):** stage 30 = bad/obey (`chmeq3_tif__0213966f.psc:9` `SetStage(30)` → QF FadeOut black); stage 130 = good/spare (`chmeq3_tif__02139679.psc` `SetStage(100)`→`SetStage(105)` → QF `Karma.Mod(+3.0)` → stage 130 QF `qGuide.SetStage(30)` + `ModRadiance(3.0)`).
- 5 TIF psc files read: `chmeq3_tif__02139664`, `chmeq3_tif__0213966f`, `chmeq3_tif__02139679`, `chmeq3_tif__02139c2b`, `chmeq3_tif__02139c33`.
- 2 QF psc files read: `qf_zzzchmemoryquest03_0213965a`, `qf_zzzchmemoryguide_0242e0b1`.
- CLI calls: `questdiag 0x13965A`, `infodiag 0x13965A`, `find 0x13965A`, `find zzzCHMemoryGuide`, `questdiag 0x42E0B1`, `packagediag` × 5.

Open verification:
- ~~decompile `CHMeq3_TIF__0213966F` (obey) and `CHMeq3_TIF__02139679` (spare) to confirm they set stage 30 vs stage 100/105~~ **RESOLVED**: `chmeq3_tif__0213966f.psc:9` `SetStage(30)` (bad); `chmeq3_tif__02139679.psc:18` `SetStage(100)` + `:9` `SetStage(105)` (good). Polarity 30=bad / 130=good confirmed.
- ~~dump QUST aliases to confirm alias `#0`=Belharza, `#1`=Enola, `#5`=Bard~~ **RESOLVED** via `qf_zzzchmemoryquest03_0213965a.psc` ALIAS PROPERTY block: `Alias_Emperor`=#0, `Alias_Enola`=#1, `Alias_Bard`=#5. Additional aliases: `Alias_Knight`, `Alias_EnolaSkull`, `Alias_ValraMemory`, `Alias_MemoryMarker01`, `Alias_MemoryMarker02`, `Alias_EnolaTravelMarker`.
- **Alias `#5` bard actor ref** (what NPC fills `Alias_Bard`): `(unverified: CLI packagediag/npcdiag cannot print alias fill target; needs ESM QUST record alias target dump)`.
- resolve garbled proper nouns: `Mackamentain`, `Eroisa`/`Polydor`, `Imuga`(Imga), `Shiki`, `Borgas`, `Umariru`(Umaril) — `(unverified: no extracted source resolves these; likely Japanese-to-English machine-translation artifacts)`.
- no story BOOK is owned by this quest (only `zzzCHBalConjureVarla`/`zzzCHBalConjureRitho` "Piece of Bal" spell items exist) — confirm none was intended.
