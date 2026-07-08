# Act 1 Conversion Spec — Sofia × VIGILANT
# 警戒者（第一幕）ModForge JSON 轉換規格

> This spec is the sole deliverable. Do NOT write the final JSON yet — implement that in a
> subsequent session using this spec as the blueprint.
>
> Engineer task: produce `examples/sofia_vigilant_act1.json` from the shapes in §3.
> All FormIDs confirmed against game-data tsvs/mds unless explicitly marked PROVISIONAL.

---

## §0 Source Map

| Document | Role |
|---|---|
| `examples/sofia_vigilant_slice.json` | Canonical pattern — copy its field names exactly |
| `docs/spec/SPEC-dialogue.md` | Authoritative field definitions for `dialogue` / `banter` / `scene` / `conditions` |
| `docs/spec/SPEC-quests.md` | Quest shape, aliases, story-event |
| `docs/spec/SPEC-workflow.md` | CLI sequence, voice pipeline |
| `sub_projs/sofia-patch/reference/follower-decode-2026-06-13.md` | Five reusable Sofia patterns; banter 踩坑 |
| `sub_projs/sofia-patch/plans/expansion-plan-2026-06-13.md` | F1–F16 feasibility; banter vs autoStart guidance |
| `sub_projs/game-data/mods/Vigilant/quests.md` | VIGILANT quest FormIDs and objective/stage text |
| `sub_projs/game-data/mods/Vigilant/locations.tsv` | VIGILANT worldspace/cell FormIDs |
| `sub_projs/game-data/mods/SofiaFollower/npcs.tsv` | Sofia NPC FormID confirmed |
| `sub_projs/sofia-patch/plans/vigilant-support-plan-2026-06-13.md` | voiceType 0x0022EE cross-reference |

---

## §1 Confirmed External References

### 1.1 Sofia

| Field | Value | Source | Status |
|---|---|---|---|
| Sofia NPC FormID | `SofiaFollower.esp:0x0012C4` | npcs.tsv row `0012C4 JJSofiaFollower Sofia` | CONFIRMED |
| Sofia NPC editorId | `JJSofiaFollower` | npcs.tsv | CONFIRMED |
| Sofia voiceType editorId | `JJSofiaVoiceType` | reference/follower-decode-2026-06-13.md §VTYP section | CONFIRMED |
| Sofia voiceType FormID | `SofiaFollower.esp:0x0022EE` | plans/vigilant-support-plan-2026-06-13.md table | CONFIRMED (used in slice _note) |
| Follower faction | `SofiaFollower.esp:0x060480` | slice _note | CONFIRMED (carried over from slice) |
| Master string in conditions | `SofiaFollower.esp` | slice + summary.txt "SofiaFollower" | CONFIRMED |

> Note: The slice guessed `JJSofiaVoiceType` and `0x0022EE`. Both are confirmed correct from
> the follower-decode §VTYP list: "JJSofiaVoiceType(Sofia)". The tsv has no VTYP rows (different
> record group), so confirmation comes from the decode doc — treat as CONFIRMED.

### 1.2 VIGILANT

| Field | Value | Source | Status |
|---|---|---|---|
| Master string in conditions | `Vigilant.esm` | slice conditions + summary.txt "Vigilant" | CONFIRMED |
| `zzzAoMMq00` "Vigilant of Stendarr" | `Vigilant.esm:0x005CE2` | quests.md | CONFIRMED |
| `zzzAoMMq06` "Also sprach Kahjiit" | `Vigilant.esm:0x009E68` | quests.md (stage 90, obj 0–80) | CONFIRMED |
| `zzzAoMSubQ01` "Witch of Ivarstead" | `Vigilant.esm:0x17576E` | quests.md (no stage list; obj NOT enumerated in md) | CONFIRMED FormID; stages PROVISIONAL |
| `zzzAoMMqGoodEnd` "Art of Mercy" | `Vigilant.esm:0x4D0376` | quests.md (obj 0/10/20/29/30/110) | CONFIRMED FormID; stages PROVISIONAL |
| `zzzAomBountyWitch` "Bounty: Witch" | `Vigilant.esm:0x4E010E` | quests.md | CONFIRMED FormID |
| `zzzAoMMq02` "The Untouchable One" | `Vigilant.esm:0x006271` | quests.md | CONFIRMED FormID |
| `zzzBMGuide` "Stendarr Guide" | `Vigilant.esm:0x43B81F` | quests.md (obj 0/10 only) | CONFIRMED FormID; stages PROVISIONAL |
| `zzzAoMMq05` "Dine and Dash" | `Vigilant.esm:0x0098C9` | quests.md (obj 10 = Stendarr's Beacon) | CONFIRMED FormID |
| `zzzAoMMq07` "Old Paladin" | `Vigilant.esm:0x00A3FE` | quests.md | CONFIRMED FormID |
| Bruiant's Estate worldspace | `Vigilant.esm:0x047CFA` | locations.tsv `047CFA zCOBruiantWorld WRLD Bruiant's Estate` | CONFIRMED |
| Hag's Pond worldspace | `Vigilant.esm:0x166857` | locations.tsv `166857 zAoMWitchWorld WRLD Hag's Pond` | CONFIRMED |
| Stendarr's Beacon Basement cell | `Vigilant.esm:0x00185B` | locations.tsv `00185B zzzAoMBeaconBasement CELL Stendarr's Beacon Basement` | CONFIRMED |
| Hag's Pond House of Pond cell | `Vigilant.esm:0x16E303` | locations.tsv `16E303 zzzAoMWitchHouse CELL House of Pond` | CONFIRMED |

### 1.3 Beacon Massacre Quest — IMPORTANT FINDING

The VIGILANT Act 1 "信標屠殺" (beats 1-E and 1-F) corresponds to **two quests** cross-referenced
in dialogue.md (topic `00EA75 zzAoMMq07B1AssaultReason`): "All is dead except me" — this indicates
the massacre is discovered during `zzzAoMMq07` "Old Paladin" (`0x00A3FE`). However, the quests.md
stages for Mq07 only list objectives (0/10/33/40/60/70), not numeric stage values.

The dialogue also mentions "Attacked by the summoner....All is dead except me" as a response in
Old Paladin. **Stage numbers for zzzAoMMq07 are PROVISIONAL** — the MD only shows objectives, not
stage numbers (log text is localized and was blank in the dump). Use `GetQuestRunning` as a fallback
gate (quest is running = massacre is happening). Similarly, `zzzAoMMq05` "Dine and Dash" has obj 10
= "Join Altano at Stendarr's Beacon" — that visit may precede the massacre.

**Recommendation**: Gate beat 1-E on `GetQuestRunning(Vigilant.esm:0x00A3FE) == 1` (Mq07 running)
OR `GetStageDone(Vigilant.esm:0x00A3FE, 10) == 1` (PROVISIONAL stage 10 = past beacon discovery).
Beat 1-F uses `GetStageDone(Vigilant.esm:0x00A3FE, 40) == 1` (PROVISIONAL stage 40 = boss defeated,
mirroring Mq07 obj 40 "Defeat Bal").

---

## §2 Beat → Mechanism Table

| Beat | Name | Beat Type | Mechanism | Quest Gate(s) | GLOB Once-Flag | Emotion | Notes |
|---|---|---|---|---|---|---|---|
| **1-A** | 加入警戒者 | 在場·玩家可問 | player-topic | `GetStageDone(0x005CE2, 5)==1` (obj 5 done = joined) | `MF_SofA1_JoinedVigilant` | Humor/Disgust | Gate = after joining; no worldspace gate |
| **1-B** | 學法術吐槽 | 在場·環境吐槽 | banter (IDLE) | `GetStageDone(0x005CE2, 10)==1` AND `GetInFaction(SofiaFollower.esp:0x060480)==1` | none (random repeating banter) | Happy | Random IDLE ambient; no sayOnce — can repeat; gated on follower faction so only fires when following |
| **1-C** | 前期任務通用 | 在場·玩家可問 | player-topic | `GetQuestRunning(Vigilant.esm:0x005CE2)==1` (spine running) AND `GetStageDone(0x005CE2, 10)==1` | `MF_SofA1_EarlyMission` | Neutral | sayOnce; general reaction after early quests |
| **1-D** | 貓人任務夢境 | 夢中·幻影掛件 | player-topic (post-dream variant) | `GetStageDone(Vigilant.esm:0x009E68, 90)==1` (stage 90 = Jo'vanni thanks = quest resolved) | `MF_SofA1_KhajiitDream` | Fear (追問 branch: Neutral) | sayOnce; 追問 sub-topic via linkTo + topLevel:false; after-dream variant ("出夢後問") recommended |
| **1-E** | 信標屠殺（嚴肅） | 在場·環境吐槽 | player-topic (sombre, no auto-banter) | `GetQuestRunning(Vigilant.esm:0x00A3FE)==1` PROVISIONAL | `MF_SofA1_BeaconMassacre` | Sad | sayOnce; emotion=Sad (rare for Sofia); 追問 sub-topic linkTo; the beat explicitly suppresses banter — use player-topic, not banter |
| **1-F** | 信標地下boss後 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x00A3FE, 40)==1` PROVISIONAL (Defeat Bal = boss killed) | `MF_SofA1_BeaconBoss` | Neutral | sayOnce; reflective mood shift; no worldspace gate |
| **1-G** | 女巫道德質疑 | 在場·玩家可問 | player-topic | `GetQuestRunning(Vigilant.esm:0x17576E)==1` PROVISIONAL (Witch of Ivarstead active) | `MF_SofA1_WitchDoubt` | Neutral | sayOnce; 追問 sub-topic; triggers when approaching Ivarstead/witches |
| **1-H kill** | 殺女巫後 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x17576E, 20)==1` PROVISIONAL (kill branch stage) | `MF_SofA1_WitchKilled` | Anger | sayOnce; branching: H-kill and H-spare are separate editorIds sharing the quest gate |
| **1-H spare** | 放女巫後 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x17576E, 10)==1` PROVISIONAL (spare branch stage; obj 10="Stop Carene" may map to a spare option) | `MF_SofA1_WitchSpared` | Happy | sayOnce; see note on GoodEnd quest below |
| **1-I** | Artano 起疑 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x17576E, 10)==1` PROVISIONAL (after returning from witches) | `MF_SofA1_ArtanoSuspicion` | Disgust | sayOnce; distinct from 1-H gates |
| **1-J kill** | Carene 殺 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x4D0376, 29)==1` PROVISIONAL (obj 29 = "Eliminate Carene" option done) | `MF_SofA1_CareneKilled` | Neutral | sayOnce; Art of Mercy quest |
| **1-J spare** | Carene 放 | 在場·玩家可問 | player-topic | `GetStageDone(Vigilant.esm:0x4D0376, 20)==1` PROVISIONAL (obj 20 = "Talk to Carene" = spared path) | `MF_SofA1_CareneSpared` | Happy | sayOnce |
| **1-K** | 章節收束 | 在場·環境吐槽 | banter (IDLE) | `GetStageDone(Vigilant.esm:0x4D0376, 30)==1` PROVISIONAL (obj 30 = go away = quest wrapping) AND `GetInFaction(SofiaFollower.esp:0x060480)==1` | none (random banter) | Happy | Light comic relief; not sayOnce; can repeat on cooldown |
| **Bruiant banter** | 莊園吐槽 | 在場·環境吐槽 | banter (IDLE) | `GetInWorldspace(Vigilant.esm:0x047CFA)==1` (Bruiant's Estate WRLD) AND `GetInFaction(SofiaFollower.esp:0x060480)==1` | none | Disgust | worldspace gate; repeating; no cooldown restriction needed (player visits once) |
| **Hag's Pond banter** | 女巫之池吐槽 | 在場·環境吐槽 | banter (IDLE) | `GetInWorldspace(Vigilant.esm:0x166857)==1` (Hag's Pond WRLD) AND `GetInFaction(SofiaFollower.esp:0x060480)==1` | none | Disgust | worldspace gate; repeating |

**Beat count summary**: 11 named beats (1-A through 1-K) + 2 realm banters = 13 entries total.
- **player-topic (sayOnce)**: 10 (1-A, 1-C, 1-D, 1-E, 1-F, 1-G, 1-H×2, 1-I, 1-J×2)
- **banter IDLE (repeating)**: 3 (1-B, 1-K, Bruiant, Hag's Pond = 4 banter entries)
- **dream/幻影 handled as player-topic** (post-dream): 1 (1-D)

**Confirmed gates**: 1-A (obj 5 stage confirmed); 1-D (stage 90 confirmed from quests.md); Bruiant/Hag's Pond worldspaces (confirmed from locations.tsv).

**PROVISIONAL gates**: 1-B (Mq00 stage 10 number), 1-C (Mq00 running), 1-E/1-F (Mq07 stages 10/40), 1-G/1-H/1-I (SubQ01 stages), 1-J/1-K (MqGoodEnd stages 20/29/30). Reason: quests.md shows objectives but not numeric stage values for these quests (logs localized, dumped blank).

**Stage validation strategy**: When in-game, run `questdiag Vigilant.esm 0x<FormID>` to read the real stage numbers, then replace all PROVISIONAL values. Meanwhile, `GetQuestRunning` is a safe interim gate.

---

## §3 ModForge JSON Shapes (Exact Field Names per SPEC-dialogue.md)

### 3.1 Player-Topic Shape (sayOnce, no branch)

Used for: 1-A, 1-C, 1-F, 1-I, 1-K (simple single-response beats).

```jsonc
{
  "editorId": "MFSofVig_1A_JoinVigilant",
  "questEditorId": "MFSofVigAct1Controller",
  "prompt": "So we're moral police now? Go around hitting things with hammers depending on what god they follow.",
  "responses": [
    "So we're moral police now? Go around hitting things with hammers depending on what god they follow. Oh don't misunderstand, I like hitting things. I'd just rather it be something valuable. ...Wait, I have to join too? Wear that grey uniform? Darling, I glow, I don't need a uniform."
  ],
  "sayOnce": true,
  "setGlobal": { "global": "MF_SofA1_JoinedVigilant", "value": 1 },
  "emotion": "Disgust",
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 },
    { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE2", "stage": 5, "comparison": "==", "value": 1 },
    { "function": "GetGlobalValue", "param": "MF_SofA1_JoinedVigilant", "comparison": "==", "value": 0 }
  ]
}
```

### 3.2 Player-Topic Shape (sayOnce, with follow-up branch)

Used for: 1-D, 1-E, 1-G, 1-H, 1-J (beats with a 追問 player sub-topic).

```jsonc
// Root topic
{
  "editorId": "MFSofVig_1D_KhajiitDream",
  "questEditorId": "MFSofVigAct1Controller",
  "prompt": "You were counting my breaths out there.",
  "responses": [
    "You just... stood there, eyes glazed, then snapped back. I counted sixty-three breaths from out here. So? What was in there? Don't tell me it was another Khajiit's childhood trauma, my arm went numb standing around."
  ],
  "sayOnce": true,
  "emotion": "Neutral",
  "setGlobal": { "global": "MF_SofA1_KhajiitDream", "value": 1 },
  "linkTo": ["MFSofVig_1D_KhajiitDreamFollowup"],
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 },
    { "function": "GetStageDone", "param": "Vigilant.esm:0x009E68", "stage": 90, "comparison": "==", "value": 1 },
    { "function": "GetGlobalValue", "param": "MF_SofA1_KhajiitDream", "comparison": "==", "value": 0 }
  ]
},
// Follow-up sub-topic (追問)
{
  "editorId": "MFSofVig_1D_KhajiitDreamFollowup",
  "questEditorId": "MFSofVigAct1Controller",
  "topLevel": false,
  "prompt": "You were worried.",
  "responses": [
    "Worried? I was calculating how much of your gear I could carry if you died. ...It was sixty-three breaths. Who counts the breaths of someone they're not worried about. Shut up, keep moving."
  ],
  "goodbye": true,
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 }
  ]
}
```

### 3.3 Sombre Beat Shape (1-E: Beacon Massacre — rare serious moment)

The screenplay explicitly says Sofia "收起嘴砲" (stops the banter). Use `player-topic` (NOT `banter`)
so the player controls the timing. Use `emotion: "Sad"` — rare for Sofia, marks character arc.

```jsonc
{
  "editorId": "MFSofVig_1E_BeaconMassacre",
  "questEditorId": "MFSofVigAct1Controller",
  "prompt": "Are you alright?",
  "responses": [
    "...I knew a few of them. That rookie who kept hitting on me at the door was in there too. I was going to mock him for an entire year."
  ],
  "sayOnce": true,
  "emotion": "Sad",
  "setGlobal": { "global": "MF_SofA1_BeaconMassacre", "value": 1 },
  "linkTo": ["MFSofVig_1E_BeaconMassacreFollowup"],
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 },
    { "function": "GetQuestRunning", "param": "Vigilant.esm:0x00A3FE", "comparison": "==", "value": 1 },
    { "function": "GetGlobalValue", "param": "MF_SofA1_BeaconMassacre", "comparison": "==", "value": 0 }
  ]
},
{
  "editorId": "MFSofVig_1E_BeaconMassacreFollowup",
  "questEditorId": "MFSofVigAct1Controller",
  "topLevel": false,
  "prompt": "You're alright.",
  "responses": [
    "I'm fine. I've seen worse. ...Just. Give me a moment, alright. Just a moment. Then we go find whatever did this and tear it apart. That part I'm good at."
  ],
  "goodbye": true,
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 }
  ]
}
```

### 3.4 Branching Beat Shape (1-H: Kill/Spare witch — two separate root topics)

Both use `sayOnce` + different GLOBs. The key principle: two separate top-level topics with
mutually exclusive conditions (kill vs spare stage). They will both appear in the menu if
conditions pass simultaneously — use the GLOB once-flags to prevent both from showing.

```jsonc
// Kill branch
{
  "editorId": "MFSofVig_1H_WitchKilled",
  "questEditorId": "MFSofVigAct1Controller",
  "prompt": "You saw what I did back there.",
  "responses": [
    "You did it. ...I won't say you were wrong, orders are orders, and we don't know the full picture. But did you see the way she looked at you at the end. I'll remember that look. You'd better too — don't let the one giving orders use you as a hammer next time."
  ],
  "sayOnce": true,
  "emotion": "Anger",
  "setGlobal": { "global": "MF_SofA1_WitchKilled", "value": 1 },
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 },
    { "function": "GetStageDone", "param": "Vigilant.esm:0x17576E", "stage": 20, "comparison": "==", "value": 1 },
    { "function": "GetGlobalValue", "param": "MF_SofA1_WitchKilled", "comparison": "==", "value": 0 }
  ]
},
// Spare branch
{
  "editorId": "MFSofVig_1H_WitchSpared",
  "questEditorId": "MFSofVigAct1Controller",
  "prompt": "I let them go.",
  "responses": [
    "You let them go. Ha, I knew you were soft. ...But you know what, this time I don't mind. If Artano had a legitimate reason, he'd explain it himself instead of telling us to act without asking questions. We wait and see what he says."
  ],
  "sayOnce": true,
  "emotion": "Happy",
  "setGlobal": { "global": "MF_SofA1_WitchSpared", "value": 1 },
  "conditions": [
    { "function": "GetIsID", "param": "SofiaFollower.esp:0x0012C4", "comparison": "==", "value": 1 },
    { "function": "GetStageDone", "param": "Vigilant.esm:0x17576E", "stage": 10, "comparison": "==", "value": 1 },
    { "function": "GetGlobalValue", "param": "MF_SofA1_WitchSpared", "comparison": "==", "value": 0 }
  ]
}
```

> Note for 1-J (Carene): same branching pattern, referencing `Vigilant.esm:0x4D0376` (Art of Mercy)
> stages 29 (kill) and 20 (spare). See §2 table for PROVISIONAL stage numbers.

### 3.5 Banter (IDLE) Shape — Auto-fire ambient lines

Used for: 1-B, 1-K, Bruiant banter, Hag's Pond banter.

Per `SPEC-dialogue.md §banter`: all `banter` entries sharing a (speaker, quest) collapse into ONE
ambient topic with `Random`-flagged INFOs. The engine picks one whose conditions pass.

**Iron law from follower-decode 踩坑**: use `autoStart.gateGlobal` for autoStart scenes (not
scene-level conditions). For plain `banter` entries, the condition goes directly on the INFO.
The speaker needs an AI package with `AllowIdleChatter` — Sofia's vanilla follow package already
carries this (she has 247 idle lines using exactly this mechanism).

```jsonc
// 1-B: learning spells banter
{
  "questEditorId": "MFSofVigAct1Controller",
  "speakerNpcEditorId": "JJSofiaFollower",
  "responses": [
    "You want me to learn... holy light. Me. Healing spells are fine, you're always the one getting hurt. But Turn Undead? My only relationship with dead things is making more of them. ...Fine, I'll learn it. But if I ever start lecturing anyone, you have my full permission to hit me."
  ],
  "emotion": "Happy",
  "conditions": [
    { "function": "GetStageDone", "param": "Vigilant.esm:0x005CE2", "stage": 10, "comparison": "==", "value": 1 },
    { "function": "GetInFaction", "param": "SofiaFollower.esp:0x060480", "comparison": "==", "value": 1 }
  ]
},
// Bruiant's Estate banter
{
  "questEditorId": "MFSofVigAct1Controller",
  "speakerNpcEditorId": "JJSofiaFollower",
  "responses": [
    "Rich people. Big empty house, daedric altar in the basement — typical. Look at that crystal chandelier, better taste than their religion. We can... 'confiscate evidence' when we're done."
  ],
  "emotion": "Disgust",
  "conditions": [
    { "function": "GetInWorldspace", "param": "Vigilant.esm:0x047CFA", "runOn": "Subject", "comparison": "==", "value": 1 },
    { "function": "GetInFaction", "param": "SofiaFollower.esp:0x060480", "comparison": "==", "value": 1 }
  ]
},
// Hag's Pond banter
{
  "questEditorId": "MFSofVigAct1Controller",
  "speakerNpcEditorId": "JJSofiaFollower",
  "responses": [
    "Witches. Hmph. Old and ugly and trying to grab glory with magic. No wonder they hide in a swamp. ...What? I'm just stating facts. I'm very compassionate, actually."
  ],
  "emotion": "Disgust",
  "conditions": [
    { "function": "GetInWorldspace", "param": "Vigilant.esm:0x166857", "runOn": "Subject", "comparison": "==", "value": 1 },
    { "function": "GetInFaction", "param": "SofiaFollower.esp:0x060480", "comparison": "==", "value": 1 }
  ]
}
```

### 3.6 Dream Beat (1-D) — Why NOT scene/幻影

The screenplay offers two options for 1-D: (a) Sofia enters the dream as 幻影 (ghost companion),
or (b) post-dream player-topic ("出夢後問").

**Recommendation: use option (b) — post-dream player-topic. Do NOT use autoStart scene for this.**

Justification:
- The 幻影 concept requires Sofia to be present inside VIGILANT's scripted dream scene without
  interfering. A ModForge `autoStart` scene would fire based on proximity, which conflicts with
  VIGILANT's own cutscene timing (Sofia entering the dream could desync from VIGILANT's scene).
- The post-dream player-topic is safer: it gates on `GetStageDone(Mq06, 90)==1` (dream resolved),
  so Sofia reacts AFTER the VIGILANT sequence has finished. Zero risk of interfering with
  VIGILANT's scripted content.
- The screenplay itself acknowledges both options and suggests flexibility ("第一個夢，看要不要讓她跟").
- Per the 踩坑 (writing iron laws from follower-decode): "dialogue 在 dense 事件上要 conditions
  才不劫持原版" — cutscenes are the densest events; reactive dialogue after the fact is the
  correct approach.

If future acts need the 幻影 mechanic for non-cutscene dream content (Acts 2/4), consider
`autoStart` scene with `playOnce:true` + `gateGlobal` to ensure it fires exactly once at the
right moment. For Act 1, the post-dream player-topic is the right call.

---

## §4 Top-Level Plugin Scaffold

```jsonc
{
  "pluginName": "ModForgeSofiaVigilant.esp",
  "esl": true,

  "voiceTemplates": [
    {
      "id": "sofia-f5",
      "engine": "f5",
      "referenceWav": "refs/sofia_ref.wav",
      "referenceText": "",
      "language": "en"
    }
  ],

  "voiceSpeakers": [
    {
      "speaker": "SofiaFollower.esp:0x0012C4",
      "voiceType": "JJSofiaVoiceType",
      "template": "sofia-f5"
    }
  ],

  "globals": [
    { "editorId": "MF_SofA1_JoinedVigilant",    "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_EarlyMission",       "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_KhajiitDream",       "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_BeaconMassacre",     "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_BeaconBoss",         "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_WitchDoubt",         "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_WitchKilled",        "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_WitchSpared",        "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_ArtanoSuspicion",    "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_CareneKilled",       "type": "short", "value": 0 },
    { "editorId": "MF_SofA1_CareneSpared",       "type": "short", "value": 0 }
  ],

  "quests": [
    {
      "editorId": "MFSofVigAct1Controller",
      "name": "",
      "startGameEnabled": true,
      "priority": 60,
      "type": "None"
    }
  ],

  "dialogue": [ /* all player-topic entries per §3.1–3.4 */ ],
  "banter":   [ /* all banter IDLE entries per §3.5 */ ]
}
```

**Notes on scaffold**:
- `esl: true` — safe; Act 1 has well under 2048 records.
- One controller quest `MFSofVigAct1Controller` hosts all Act 1 dialogue and banter.
  When adding Acts 2–4, add additional controller quests (`MFSofVigAct2Controller`, etc.)
  per the Sofia pattern of "small quest constellations" (follower-decode §①).
- `voiceSpeakers` uses `JJSofiaVoiceType` editorId (the string the engine uses for the
  `Sound/Voice/<plugin>/JJSofiaVoiceType/` folder). The `voiceType` field here is the
  editorId string, not a FormID — check SPEC-workflow.md §voiceSpeakers for exact semantics.
- No `voiceType` FormID ref is needed in `voiceSpeakers` — the string `"JJSofiaVoiceType"` is
  what the CLI uses to derive the file path.
- 11 GLOBs for 11 sayOnce beats. The 4 banter entries (1-B, 1-K, Bruiant, Hag's Pond)
  do NOT need GLOBs — they are repeating ambient lines gated only by conditions.

---

## §5 Mechanism Recommendation: Banter vs AutoStart Scene

For all Act 1 beats, the choice is:
- **banter (IDLE)** — Sofia talks unprompted; player doesn't need to open dialogue menu.
- **player-topic (dialogue)** — player must actively speak to Sofia.
- **autoStart scene** — Sofia talks to another NPC (requires a second alias actor).

**For Act 1, use**:
- **player-topic** for all sayOnce story-reaction beats (1-A through 1-J). These are
  narrative moments that should be player-controlled. The screenplay calls them "在場·玩家可問".
- **banter (IDLE)** for ambient environmental commentary (1-B, 1-K, Bruiant, Hag's Pond).
  These are the "在場·環境吐槽" beats — they should fire automatically to enhance atmosphere,
  not require menu interaction.
- **No autoStart scene** needed in Act 1. The screenplay has no beat where Sofia needs to
  banter WITH another named NPC. If such a beat appears in later acts, use the `F1 pattern`
  from expansion-plan (SceneSpec + autoStart + UniqueActor aliases).

**Iron law respected**: Beat 1-E (beacon massacre) is typed "在場·環境吐槽" in the screenplay
but should be implemented as a **player-topic**, not banter. Reason: the beat is a serious
character arc moment ("角色弧露出"). Auto-firing it as banter risks it playing at the wrong
moment (e.g., Sofia saying the sombre line during combat). Player-topic gives the player
control of when to surface it. The screenplay's type label is a writing/tonal label, not a
mechanical mandate.

---

## §6 CLI Command Sequence

From `SPEC-workflow.md §Workflow`. Run from repo root. Substitute `OutModDir` with the MO2
mod folder path (see memory/packaged-zip-delivery-path.md → `~/skyrim_mods/mine/`).

```bash
# Step 1: Validate spec (offline; no Skyrim required; catches condition/field errors)
dotnet run --project src/ModForge.Cli -- validate examples/sofia_vigilant_act1.json

# Step 2: Build the plugin (offline; produces the .esp)
dotnet run --project src/ModForge.Cli -- build examples/sofia_vigilant_act1.json out/ModForgeSofiaVigilant.esp

# Step 3: Package as MO2 mod folder (offline; compiles scripts, writes .seq)
dotnet run --project src/ModForge.Cli -- package examples/sofia_vigilant_act1.json ~/skyrim_mods/mine/SofiaVigilant

# Step 4 (OFFLINE TEST): Run offline tests (Category!=RequiresSkyrim)
dotnet test --filter "Category!=RequiresSkyrim"

# Step 5: Voice diagnostic (check speaker resolution before TTS; Manjaro only)
dotnet run --project src/ModForge.Cli -- voicediag examples/sofia_vigilant_act1.json ~/skyrim_mods/mine/SofiaVigilant/ModForgeSofiaVigilant.esp

# Step 6: Generate voiced audio (Manjaro only; requires MODFORGE_TTS_BIN + MODFORGE_LIPGEN)
# CRITICAL ORDER: package FIRST, then voicelines on the PACKAGED esp.
# "package first then voicelines so Sound/Voice/<plugin> folder name matches"
dotnet run --project src/ModForge.Cli -- voicelines examples/sofia_vigilant_act1.json ~/skyrim_mods/mine/SofiaVigilant/ModForgeSofiaVigilant.esp
```

**Offline machine**: Steps 1–4 only. Steps 5–6 require Manjaro (Wine/CK/TTS). Do NOT run
`voicelines` without a MODFORGE_TTS_BIN configured — it will error on missing env var.

**Voice ref extraction** (if sofia_ref.wav is missing or needs refresh):
```bash
dotnet run --project src/ModForge.Cli -- extract-voices \
  "~/skyrim_mods/unzip/Sofia Follower v.2/Data/SofiaFollower.bsa" \
  JJSofiaVoiceType refs/sofia_ref/ SofiaFollower.esp
```

**Delivery path**: Copy the resulting `~/skyrim_mods/mine/SofiaVigilant/` folder to MO2 as a
flat mod. The plugin, scripts, Sound/Voice, and Data/Seq/<plugin>.seq will all be inside it.

---

## §7 Open Risks and Decisions

### R1 — PROVISIONAL stage numbers (HIGH priority, blocking in-game test)

All gates using specific stage numbers for `zzzAoMMq07` (Old Paladin), `zzzAoMSubQ01` (Witch of
Ivarstead), and `zzzAoMMqGoodEnd` (Art of Mercy) are PROVISIONAL. The quests.md dump shows
objective numbers but NOT internal stage numbers (log text was localized/blank).

**Resolution path**: On Manjaro with VIGILANT installed, run:
```
questdiag <path>/Vigilant.esm 0x00A3FE   # Old Paladin
questdiag <path>/Vigilant.esm 0x17576E   # Witch of Ivarstead
questdiag <path>/Vigilant.esm 0x4D0376   # Art of Mercy
```
Read the actual stage numbers, then replace every PROVISIONAL gate in the spec.

**Mitigation for offline dev**: Use `GetQuestRunning` as a fallback gate (safer than a wrong
stage number that never fires). Add a comment in the JSON for each PROVISIONAL gate.

### R2 — Branching beats 1-H and 1-J: both topics visible simultaneously

If the player's kill/spare choice sets BOTH branch stages (e.g., if VIGILANT sets both obj 10
and obj 20 for tracking purposes), both 1-H topics would appear in the menu. The GLOB once-flags
(`MF_SofA1_WitchKilled` / `MF_SofA1_WitchSpared`) protect against Sofia repeating a line, but
both prompts could still appear before the player talks to Sofia.

**Resolution**: If in-game testing shows both prompts appear, add a mutual exclusion condition:
add `GetGlobalValue(MF_SofA1_WitchSpared)==0` to the kill branch, and vice versa.

### R3 — Sofia voiceType FormID vs editorId in voiceSpeakers

The slice's `_note` says voiceType FormID = `0x0022EE` (without master prefix). The
`voiceSpeakers[].voiceType` field takes the editorId string `"JJSofiaVoiceType"` (per SPEC-workflow.md
— it drives the folder name). Confirm whether `voiceSpeakers[].voiceType` expects the editorId
string or the `<master>:0xFormID` ref. From the slice and SPEC-workflow.md, it is the editorId
string. If `voicediag` shows a resolution error, switch to `"SofiaFollower.esp:0x0022EE"`.

### R4 — Beat 1-D "幻影掛件" dream mechanic deferred

The screenplay leaves open whether Sofia enters the dream (幻影) or reacts post-dream. This spec
recommends post-dream player-topic (see §3.6). If the user wants the in-dream 幻影 experience,
it would require an `autoStart` scene (SceneSpec + `playOnce:true` + `gateGlobal`) firing only
after `GetStageDone(Mq06, 20)==1` (player enters Jo'vanni's dream). This is a future decision
for Act 2 Lamae dream (confirmed "跟入") and Act 4 — not needed for Act 1.

### R5 — Beat 1-E trigger timing: GetQuestRunning vs a specific stage

`GetQuestRunning(Vigilant.esm:0x00A3FE)==1` fires the moment Old Paladin is active, which may
be BEFORE the player discovers the massacre. The sombre tone of beat 1-E requires that the
player has already seen the dead Vigilants.

**Resolution**: If in-game testing shows 1-E appearing too early (before entering the Beacon),
add a location gate: `GetInCell(Vigilant.esm:0x00185B)==1` (Stendarr's Beacon Basement cell,
confirmed from locations.tsv) to ensure Sofia is inside the Beacon when reacting.
This tightens the gate to: quest running AND player is inside the beacon basement.

### R6 — Menu crowding (too many player-topic prompts at once)

11 sayOnce topics + branching = up to 13 options theoretically visible. In practice, each
has a GLOB gate (`GetGlobalValue==0`) that auto-removes it after talking. But before talking,
several may appear at once (e.g., 1-G, 1-H, 1-I all fire during the witch quest).

**Resolution**: Sequence the gates carefully. 1-I (Artano suspicious) should only show AFTER
1-H (kill/spare decision) is done — add `GetGlobalValue(MF_SofA1_WitchKilled)==1 OR
GetGlobalValue(MF_SofA1_WitchSpared)==1` condition to 1-I. Use OR via the `"or": true`
condition field. This ensures 1-I unlocks only after the player has discussed the choice.

### R7 — Beat 1-C (general early-mission) gate is very broad

`GetQuestRunning(Mq00)==1` fires as soon as the main spine starts, potentially before the
player has done any missions. Consider gating 1-C on `GetStageDone(Mq00, 15)==1` (obj 15
"Talk to Altano" — PROVISIONAL but suggests post-join assignment phase) to ensure Sofia
reacts after the first real mission, not immediately after joining.

### R8 — ESL cap

`esl: true` is safe for Act 1 alone (well under 2048 new records). When Acts 2–4 are added to
the same plugin, count records: if approaching 2048, split into per-act ESL plugins or promote
to a full ESP. The current slice is ESL and serves as precedent.

---

## §8 Quest Stage Reference Summary

| Quest | FormID | Confirmed Stages/Objs from quests.md |
|---|---|---|
| `zzzAoMMq00` "Vigilant of Stendarr" | `0x005CE2` | obj 5 (join), 10 (follow Altano), 15, 20, 30 |
| `zzzAoMMq06` "Also sprach Kahjiit" | `0x009E68` | **stage 90** (confirmed text "thank you"), obj 0/10/20/21/25/50/60/70/80 |
| `zzzAoMSubQ01` "Witch of Ivarstead" | `0x17576E` | NO stages listed; no objectives in md either — ALL PROVISIONAL |
| `zzzAoMMqGoodEnd` "Art of Mercy" | `0x4D0376` | obj 0/10/20/29/30/110 — obj 29="Eliminate Carene", obj 20="Talk to Carene" (spare path) — stages PROVISIONAL |
| `zzzAomBountyWitch` "Bounty: Witch" | `0x4E010E` | obj 0 only — PROVISIONAL if used |
| `zzzAoMMq07` "Old Paladin" | `0x00A3FE` | obj 0/10/33/40/60/70 — obj 40="Defeat Bal" maps to 1-F boss — stages PROVISIONAL |
| `zzzAoMMq05` "Dine and Dash" | `0x0098C9` | obj 10 = join at Stendarr's Beacon — earlier visit to beacon |
| `zzzBMGuide` "Stendarr Guide" | `0x43B81F` | obj 0/10 only — PROVISIONAL; likely not needed for Act 1 |

> The only **fully confirmed** numeric stage used is `zzzAoMMq06 stage 90` — the quests.md dump
> explicitly shows `[stage 90] Thank you. Jo'vanni thank you very much.` as a log entry for that
> quest. All other stage numbers used in conditions are derived from objective numbers as proxies
> and must be validated in-game.
