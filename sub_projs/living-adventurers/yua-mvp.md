# YUA MVP — first external follower enroll

← [README](README.md)｜[design](design.md)｜idea [#23](../../workflows/idea/living-adventurers.md)

## Done when

YUA is documented as the first external-follower enrollment target, with a role brief, a minimal `livingNpcs` spec shape, the extracted facts from `YUA_Follower.esp`, and an implementation checklist that reuses the existing `livingNpcs` macro instead of inventing a new system.

Status: MVP + P1 interactions have been built and shipped as `/home/lorkhan/skyrim_mods/mine/MFLivingYUA.zip`. The patch now also yields to the follower system while YUA is the player's teammate. It does not install the archive or change MO2.

## Source candidate

- Archive: `/home/lorkhan/skyrim_mods/hdd/YUA Follower ESL.7z`
- Plugin candidate: `YUA_Follower.esp`
- Package traits observed from archive listing:
  - Has FaceGen and FaceTint under `YUA_Follower.esp`.
  - Has custom body, hair, face, armor, weapon/effect textures and meshes.
  - Has a small set of spell scripts.
  - No obvious large quest, voice, `.fuz`, or dialogue payload was visible from archive structure/string probe.
- Plugin facts from `ModForge.Cli dump` / `npcdiag`:
  - YUA NPC base: `YUA_Follower.esp:0x000800` (`EditorID=YUA`, name `YUA`).
  - Placed ref: `YUA_Follower.esp:0x000817`, in `WhiterunJorrvaskr` (`Skyrim.esm:0x0165B5`), at roughly `(-526.6, -452.4, 42)`.
  - VoiceType: `Dawnguard.esm:0x007A32`.
  - Flags: female, essential, autocalc, unique.
  - AI: unaggressive, brave, helps friends/allies.
  - Class: `YUA_Follower.esp:0x000818`; combat style: `YUA_Follower.esp:0x00087D`.

Conclusion: good MVP candidate because she appears to be a visual/equipment-focused standalone follower rather than a heavy custom-quest follower.

## Character brief

YUA is a bright, pretty young woman with a young-female voice, knightly armor, and a sword. She should read as a new heroic presence in the world: energetic, clean, brave, a little too willing to take danger personally.

She is not a grim mercenary, a cynical monster hunter, or a drunken brawler. The core fantasy is: the player keeps hearing about and running into a young knight who is slowly becoming a local heroine even when the player is not recruiting her.

## Archetype

Use a new data-level flavor over the existing `adventurer` script branch first:

```jsonc
{
  "archetype": "adventurer",
  "_flavor": "young_knight_adventurer"
}
```

Do not add a new Papyrus archetype branch for the MVP unless the current `adventurer` branch blocks the tone. The first build should prove external follower enrollment, not a new controller behavior.

Later, if YUA works well, add a first-class `knightAdventurer` archetype with sword/paladin-specific notifications, rumor pools, and anchor weighting.

## Minimal life loop

| State | MVP meaning | Presentation |
|---|---|---|
| resting | Back from work, off duty | Bannered Mare sandbox; sits, idles, or stands near a table |
| traveling | Off-screen travel | Pure abstract sim; no actor processed |
| atTask | Off-screen contract | Stronghold/bandit/undead task abstracted into deed increment |
| reporting | Returns to Whiterun | Same anchor as resting for MVP |
| injured | Future flavor only | Rumor text can imply injury; no mechanical branch yet |

For the MVP, the current controller's deed increment + anchor rotation is enough. "Task type" can live in text until the controller stores richer state.

## First anchors

Prefer Whiterun because the QA loop already uses Bannered Mare often and the cell is familiar.

| Anchor | Cell | Use |
|---|---|---|
| Bannered Mare | `Skyrim.esm:0x01605E` | Primary appearance point |
| Temple of Kynareth | TODO FormID | Later injury/rest flavor |
| Dragonsreach | TODO FormID | Later reporting flavor |

MVP uses only Bannered Mare unless two-anchor rotation is required by current macro assumptions. If two anchors are needed, add a second Bannered Mare position rather than pretending a new behavior exists.

## Rumor tone

Use low-key tavern gossip. The player should feel YUA has a life, not that a quest is yelling for attention.

Seed lines:

- "That young woman in the white armor? She came back through the gate after midnight with frost on her sword."
- "YUA called it a few bandits. The caravan folk called it a rescue."
- "She smiles like it was nothing, but I saw her hide her left hand under that cloak."
- "Some say she's too young to take those contracts alone. Funny thing is, she keeps coming back."

Prompt shape for rumor speaker:

```text
Any word of YUA?
```

Response policy:

- One or two sentences per rumor.
- Mention visual identity: white/knightly armor, young woman, sword.
- Avoid romance-bait and avoid making her helpless.
- Keep heroic but not overpowered.

## Interactions

P1 enables the two low-risk relationship interactions:

```jsonc
"interactions": ["fund", "praise"]
```

`fund` and `praise` each increment `MFLiving_N0_Favor`; praise remains gated on deeds ≥ 1. The former packaging blocker is fixed: `CompileBest` now falls back from the native compiler's incomplete header set to the CK/Wine compiler, and both generated TIF fragments are present in the shipped zip.

Future interaction meanings:

- `fund`: buy supplies / pay for healing.
- `praise`: your deeds are being talked about.

Do not implement hire-as-follower yet; YUA's own follower mod already owns recruitment, and living-adventurers must not break it.

## Draft spec shape

Concrete draft: [yua/living_yua_spec.json](yua/living_yua_spec.json).

This uses YUA's NPC base as the external `uniqueActor` ref. It also places a tiny custom rumor speaker in the Bannered Mare so the first build does not have to inject topics into a vanilla innkeeper.

Build result (2026-08-08):

- Shipped zip: `/home/lorkhan/skyrim_mods/mine/MFLivingYUA.zip`
- Plugin: `MFLivingYUA.esp`
- Masters: `Skyrim.esm`, `YUA_Follower.esp`
- Package contents: `MFLivingYUA.esp`, `REQUIREMENTS.txt`, `Seq/MFLivingYUA.seq`, the two reusable living scripts, and two generated interaction TIF `.pex` + `.psc` files.
- Static dump: 28 records; controller quest StartGameEnabled; alias `Living0_N0` fills `uniqueActor 000800:YUA_Follower.esp`; rumor topic `Any word of YUA?` is gated on `MFLiving_N0_Deeds`; fund/praise INFOs target YUA and carry their TIF VMAD.

```jsonc
{
  "pluginName": "MFLivingYUA.esp",
  "esl": true,
  "requires": [
    { "plugin": "YUA_Follower.esp", "reason": "YUA's unique NPC base is enrolled as the living-world actor." }
  ],
  "livingNpcs": {
    "simIntervalHours": 2,
    "pollInterval": 5,
    "rumorSpeaker": "MFLY_RumorPatron",
    "npcs": [
      {
        "ref": "YUA_Follower.esp:0x000800",
        "name": "YUA",
        "archetype": "adventurer",
        "alignment": "friendly",
        "backstory": "A bright young swordswoman in knightly armor who takes dangerous contracts around Whiterun and tries to make the work look easier than it is.",
        "anchors": [
          {
            "cell": "Skyrim.esm:0x01605E",
            "position": { "x": 250, "y": 120, "z": 0 },
            "kind": "inn"
          }
        ],
        "rumors": [
          "That young woman in the white armor? She came back through the gate after midnight with frost on her sword.",
          "YUA called it a few bandits. The caravan folk called it a rescue.",
          "Some say she's too young to take those contracts alone. Funny thing is, she keeps coming back."
        ],
        "interactions": ["fund", "praise"]
      }
    ]
  }
}
```

## Facts to extract before building

1. Follower-framework ownership.
   - Confirm whether YUA uses vanilla follower dialogue/factions or custom scripts.
   - MVP rule: if `IsPlayerTeammate()` is true or she is in a follower state, controller should leave her alone. If current controller cannot check this yet, record as P3.5 safety work before using on a real recruited follower.
2. Rumor speaker.
   - Existing example places a custom Bjorn. For YUA, prefer a vanilla Bannered Mare NPC only if dialogue injection to external speaker is already proven for this macro; otherwise keep the custom bard/speaker pattern.
3. Runtime behavior of `uniqueActor` fill for an external NPC that also has a placed ref.
   - The spec uses YUA's NPC_ base `YUA_Follower.esp:0x000800`.
   - The original mod also places `YUA_Follower.esp:0x000817` in Jorrvaskr.
   - In-game QA must confirm whether the alias resolves and MoveTo's the intended unique actor, not a duplicate.

## Implementation checklist

1. Build [yua/living_yua_spec.json](yua/living_yua_spec.json).
2. Package as `MFLivingYUA.zip`.
3. Run static gates:
   - Build succeeds.
   - `.pex` scripts are included.
   - Controller quest is StartGameEnabled.
   - Alias fill points to YUA correctly.
   - Rumor topic is gated on YUA deed global.
   - Both generated interaction TIF `.pex` files are present and attached to their INFO records.
4. In-game QA:
   - Automated setup/engine assertions: run [living_yua.qa.json](yua/living_yua.qa.json) through `agent-bridge/client/qa_runner.py` with `MO2_PROFILE=QA`; it installs both archives, proves both plugins + YUA + Mira, then pauses for the human interaction gate and always uninstalls on teardown.
   - Install YUA follower archive.
   - Install `MFLivingYUA.zip` after it.
   - `coc WhiterunBanneredMare`.
   - Wait enough game hours or set deed global.
   - Confirm YUA appears only once, can leave/re-enter, and rumor appears.
   - Recruit YUA and confirm the living controller stops deed simulation and never `MoveTo`s her while she is the player's teammate; dismiss her and confirm the controller resumes ownership.

## Risks

- External follower may not have a suitable persistent placed ref; the macro may need a "base NPC plus generated placement" mode.
- Moving a follower mod's actor can break its recruitment scene if done before the mod initializes.
- If she has DAR/OAR or combat animation assets, the MVP should not touch them.
- The current controller's `adventurer` branch may be too generic; accept that for the first test.
- Follower frameworks that do not implement `IsPlayerTeammate()` still need a future adapter; YUA uses the vanilla follower factions and is the first runtime proof of the guard.
- If YUA uses an ESL-flagged plugin, confirm FormID serialization in spec uses the same durable `<plugin>:0xLOCALID` convention as ModForge external refs.

## Next decision

Treat YUA as **the first named enrollment target**, not the first proof of the runtime architecture. The runtime architecture already has `MFLivingNpcs.zip` waiting for acceptance; YUA is the next layer: prove that a real visual follower from the user's archive can be enrolled with a few lines of data.
