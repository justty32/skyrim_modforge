# YUA MVP — first external follower enroll

← [README](README.md)｜[design](design.md)｜idea [#23](../../workflows/idea/living-adventurers.md)

## Done when

YUA is the first runtime-proven external-follower enrollment target: one existing unique actor participates in abstract deeds, materializes at a living-world anchor, exposes rumor/favor interactions, yields completely to the vanilla follower system while recruited, and returns to living-world control without disappearing in front of the player after dismissal.

Status: **complete / in-game PASS 2026-08-10**. The final agent-bridge run passed 15/15 checks. `/home/lorkhan/skyrim_mods/mine/MFLivingYUA.zip` is the shipped patch; QA teardown removed its temporary MO2 installs and restored the Default profile.

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
  - Recruitment uses the vanilla follower state: PotentialFollowerFaction rank 0 and CurrentFollowerFaction rank -1 before recruitment; the runtime `IsPlayerTeammate()` guard therefore applies.

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

Build result (2026-08-10):

- Shipped zip: `/home/lorkhan/skyrim_mods/mine/MFLivingYUA.zip`
- Plugin: `MFLivingYUA.esp`
- Masters: `Skyrim.esm`, `YUA_Follower.esp`
- Package contents: `MFLivingYUA.esp`, `REQUIREMENTS.txt`, `Seq/MFLivingYUA.seq`, the two reusable living scripts, and two generated interaction TIF `.pex` + `.psc` files.
- Static dump: 28 records; controller quest StartGameEnabled; alias `Living0_N0` fills `uniqueActor 000800:YUA_Follower.esp`; rumor topic `Any word of YUA?` is gated on `MFLiving_N0_Deeds`; fund/praise INFOs target YUA and carry their TIF VMAD.
- Runtime QA: 15/15 PASS. Exactly one YUA was enrolled; deed simulation, materialization, Mira's rumor, both favor interactions, recruitment ownership, and dismissal handoff all worked.

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

## Confirmed integration findings

1. **Follower ownership:** YUA uses the vanilla follower state and raises `IsPlayerTeammate()` when recruited. During that state the living alias neither advances deeds nor calls `MoveTo`/`EvaluatePackage`.
2. **Dismissal handoff:** the first QA run exposed a visible pop—after YUA crossed the inn door, the controller reclaimed her immediately. The final controller leaves her on the follower mod's dismissed package while the player can still follow. Once player and actor are separated, a 30-real-second grace applies; loaded actor 3D within 8192 units keeps extending it. Only then may the controller move her off-stage.
3. **Rumor speaker:** the custom Bannered Mare patron Mira Snow-Voice avoids injecting dialogue into a vanilla speaker and worked in-game.
4. **External `uniqueActor`:** the alias filled from NPC base `YUA_Follower.esp:0x000800` and controlled the mod's existing placed actor `0x000817`; runtime QA saw one YUA, not a generated duplicate.

## Implementation checklist

1. ✅ Build [yua/living_yua_spec.json](yua/living_yua_spec.json).
2. ✅ Package as `MFLivingYUA.zip`.
3. ✅ Run static gates:
   - Build succeeds.
   - `.pex` scripts are included.
   - Controller quest is StartGameEnabled.
   - Alias fill points to YUA correctly.
   - Rumor topic is gated on YUA deed global.
   - Both generated interaction TIF `.pex` files are present and attached to their INFO records.
4. ✅ In-game QA (15/15 PASS, 2026-08-10):
   - Automated setup/engine assertions: run [living_yua.qa.json](yua/living_yua.qa.json) through `agent-bridge/client/qa_runner.py` with `MO2_PROFILE=QA`; it installs both archives, proves both plugins + YUA + Mira, then pauses for the human interaction gate and always uninstalls on teardown.
   - Install YUA follower archive.
   - Install `MFLivingYUA.zip` after it.
   - `coc WhiterunBanneredMare`.
   - Wait enough game hours or set deed global.
   - Confirm YUA appears only once, can leave/re-enter, and rumor appears. **Passed.**
   - Recruit YUA and confirm the living controller stops deed simulation and never `MoveTo`s her while she is the player's teammate. **Passed.**
   - Dismiss and follow her across a door for at least 10 seconds; she must keep walking without a visible pop. Separate long enough for the off-screen grace to expire, then confirm living-world materialization resumes. **Passed after the first-run pop was fixed.**

## Risks

- Other external followers may not have a suitable persistent placed ref; they may still need a "base NPC plus generated placement" mode. YUA did not.
- Moving a heavier follower mod's actor can break its recruitment scene if done before the mod initializes; YUA's lightweight vanilla-follower path passed.
- If she has DAR/OAR or combat animation assets, the MVP should not touch them.
- The current controller's `adventurer` branch may be too generic; accept that for the first test.
- Follower frameworks that do not implement `IsPlayerTeammate()` still need a future adapter; YUA proves only the vanilla teammate contract.
- YUA's ESL-flagged plugin worked with ModForge's durable `<plugin>:0xLOCALID` external-ref serialization.

## Next decision

YUA has completed its role as the **first named external-follower proof**. The next coverage target is the generic `MFLivingNpcs.zip` P0–P3 acceptance (two actors/archetypes plus parley/alignment); after that, choose between controller behavior driven by favor/alignment and the hostile-in-combat parley presentation gap.
