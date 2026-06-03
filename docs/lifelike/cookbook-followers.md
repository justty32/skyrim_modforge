<!-- Follower patterns -->
# Recipe cookbook — followers

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## "Recruitable follower" (hire → follow → dismiss), in-game confirmed

Hard-won lessons (It.27–It.30 — see the [follower gotchas](gotchas.md) and probe everything with
`infodiag` first):

- **You cannot reuse vanilla's PAID hireling line.** Every recruit INFO in `HirelingQuestTopic1` is
  gated `GetIsID==<a specific vanilla mercenary>`, so a custom NPC fails them all (and the topic even
  *vanishes* once you can afford it). `PotentialHireling` membership alone only buys the refusal line.
- **`SetPlayerTeammate(true)` ≠ follows.** It makes her fight for you and obey commands, but physical
  following needs a **Follow package** targeting the player.
- **Don't piggyback `CurrentFollowerFaction` for dismiss.** Vanilla's dismiss line is driven by the
  *DialogueFollower quest* and only releases followers it registered; a manually-faction'd NPC gets the
  "you're dismissed" notification but keeps following. **Manage follower state yourself.**

**Three paths — prefer the vanilla-integrated ones (a)/(c); they're compatible with follower-manager
mods (AFT/EFF/NFF) and need no custom command dialogue.**

**(a) Free "Follow me, I need your help"** — reuse vanilla's free-follow topic (`0x0B0EE6`), which gates
on relationship + follower voice, *not* GetIsID. Needs: a follower voice (e.g. `FemaleEvenToned
0x013ADD`), `PotentialFollowerFaction 0x05C84D`, **not** `PotentialHireling`, a `greeting` (so she's
conversable), and a tiny quest script setting the relationship (a static player RELA reads 0 at
runtime). See `examples/follower_hireable_spec.json` + `MFHireFollowerSetup.psc`.

**(c) Paid via vanilla `SetFollower` — RECOMMENDED for a paid follower (the user prefers this).** Author
your own paid recruit topic, but in its fragment hand the NPC straight to the vanilla follower system:
```papyrus
Quest Property DialogueFollower Auto   ; bound to Skyrim.esm:0x0750BA
...
player.RemoveItem(Gold001, 500)
(DialogueFollower as DialogueFollowerScript).SetFollower(akSpeaker)   ; compiles vs base scripts
```
`SetFollower` sets relationship + `SetPlayerTeammate` + `ForceRefTo`'s the follower alias (which carries
the follow package and adds `CurrentFollowerFaction`). After that, **vanilla's own trade/wait/follow/
dismiss dialogue all work** and AFT/EFF/NFF pick her up — no custom command topics needed. Gate the
recruit line on `GetGlobalValue PlayerFollowerCount (0x0BCC98) == 0` so it never stomps the single
follower slot. See `examples/follower_vanilla_spec.json` + `MFHireVanillaRecruit.psc`.

> **What survives vanilla follower status** (the worry about losing your lifelike work): a follower is
> just an alias stacking a high-priority *follow package* on top — it's additive, not destructive.
> **CombatStyle is preserved** (verified: `PlayerFollowerPackage`/the combat-override package set no
> CombatStyle, so the actor's base CSTY drives combat). **Your custom dialogue is preserved** and can be
> *conditioned on* follower state — e.g. a follower-only self-introduction gated on `GetInFaction
> CurrentFollowerFaction (0x05C84E) == 1` (see the vanilla example). **Sandbox/travel/schedule packages
> are only out-prioritized while she's actively trailing you** (you can't both follow and commute) and
> resume the moment she's dismissed or told to wait.

**(b) Paid, fully self-managed** — *the user found this less preferable than (c); kept for reference.* No
vanilla follower involvement: an OWN flag faction is the "is my follower" state; OWN recruit + dismiss +
trade + wait topics carry result fragments; the Follow package gates on the flag. Upside: zero conflicts,
no single-slot limit, runs alongside a real vanilla follower. Downside: you re-implement every command,
and follower-manager mods don't see her. Skeleton (full: `examples/follower_paid_spec.json` +
`MFHirePaidRecruit/Dismiss.psc`):

```jsonc
{ "factions": [ { "editorId": "MF_FollowerFlag", "name": "My Follower" } ],
  "packages": [
    { "editorId": "MF_FollowPkg", "template": "Skyrim.esm:0x019B2C",
      "follow": { "target": "" },                                   // ⇒ player
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1,
                        "param": "MF_FollowerFlag", "runOn": "Subject" } ] }  // follow only while hired
  ],
  "npcs": [ { "editorId": "MF_Merc", "voiceType": "Skyrim.esm:0x013ADD", "greeting": "Coin talks.",
              "factions": [ "Skyrim.esm:0x0267EA", "Skyrim.esm:0x028172" ],   // citizenship, NOT a follower faction
              "packages": [ "MF_FollowPkg" ], "unique": true } ],
  "quests": [ { "editorId": "MF_Q", "startGameEnabled": true } ],
  "dialogue": [
    { "editorId": "MF_Hire", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Here's 500 gold. Fight at my side.", "responses": [ "Lead the way." ], "goodbye": true,
      "conditions": [
        { "function": "GetItemCount", "comparison": ">=", "value": 500, "param": "Skyrim.esm:0x00000F",
          "runOn": "Reference", "reference": "Skyrim.esm:0x000014" },
        { "function": "GetInFaction", "comparison": "==", "value": 0, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidRecruit", "resultScriptSource": "scripts/MFHirePaidRecruit.psc",
      "resultProperties": [ { "name": "Gold001", "type": "object", "objectEditorId": "Skyrim.esm:0x00000F" },
        { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" },
        { "name": "GoldCost", "type": "int", "int": 500 }, { "name": "RelRank", "type": "int", "int": 3 } ] },
    { "editorId": "MF_Dismiss", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Merc",
      "prompt": "Let's part ways.", "responses": [ "Aye." ], "goodbye": true,
      "conditions": [ { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "MF_FollowerFlag", "runOn": "Subject" } ],
      "resultScript": "MFHirePaidDismiss", "resultScriptSource": "scripts/MFHirePaidDismiss.psc",
      "resultProperties": [ { "name": "FollowerFaction", "type": "object", "objectEditorId": "MF_FollowerFlag" } ] }
  ] }
```
Recruit fragment: `AddToFaction(FollowerFaction)` + `SetPlayerTeammate(true)` (after taking gold).
Dismiss fragment: `RemoveFromFaction(FollowerFaction)` + `SetPlayerTeammate(false)` + `EvaluatePackage()`.

**Trade / wait / follow-again** (the full example wires these too — same fragment pattern):
- **Trade**: a topic gated on `FollowerFlag==1`, **not** `goodbye` (so the menu opens over the
  dialogue, like vanilla), fragment `akSpeaker.OpenInventory(true)`.
- **Wait / resume**: use the **`WaitingForPlayer` ActorValue** vanilla itself uses. Gate the Follow
  package on `GetActorValue WaitingForPlayer == 0` (added to `FollowerFlag==1`). A "wait here" topic
  (gated `WaitingForPlayer==0`) sets it to 1 (`SetActorValue("WaitingForPlayer", 1.0)` + `EvaluatePackage`)
  so she holds position; a "follow me again" topic (gated `WaitingForPlayer==1`) clears it. Dismiss
  clears it too. See `MFFollowerTrade/Wait/Follow.psc`.

## "Lifelike follower" extras — downtime + situational lines (It.33, in-game confirmed)

Once the hire/follow plumbing works, two cheap additions make a follower feel alive. Both are in
`examples/follower_vanilla_spec.json`.

**Downtime behaviour** — give the follower NPC an *unconditioned* Sandbox package. It's her
lowest-priority fallback, so it runs exactly when the vanilla follow-alias package is NOT active:
before recruit, after dismiss, and while she's told to wait. Instead of standing frozen she
eats/sits/wanders wherever she's placed. While actively trailing you the alias package overrides it;
combat preempts it and she resumes after.
```jsonc
"packages": [ { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter", "WorldInteractions" ],
  "sandbox": { "radius": 512, "allowEating": true, "allowSitting": true, "allowWandering": true } } ],
// ...and reference it on the npc: "packages": [ "MF_Sandbox" ]   (no condition needed)
```

**Daily routine — scheduled Sleep on top of the sandbox** (It.35, in-game confirmed). The downtime sandbox is the
*waking-hours* default; layer a **Sleep package** (template `0x019717`) above it to make her bed down
at night. Sleep is a specialized Sandbox that actively **seeks a bed** (built-in) and can lock doors.
The sleep window is the package **`schedule`** (not a data slot); the NPC's `packages` list is in
**priority order** — the engine runs the first package whose schedule + conditions match, so put the
scheduled Sleep *first* and the unconditioned sandbox *last* as the fallback for every other hour.
```jsonc
"packages": [
  { "editorId": "MF_NightSleep", "template": "Skyrim.esm:0x019717",
    "schedule": { "hour": 22, "durationInMinutes": 540 },          // 22:00–07:00
    "interruptFlags": [ "HellosToPlayer" ],
    "sleep": { "radius": 1024, "lockDoors": false } },             // lockDoors:false — shared inn, don't lock it
  { "editorId": "MF_Sandbox", "template": "Skyrim.esm:0x01C254", "sandbox": { ... } } ],
"packages": [ "MF_NightSleep", "MF_Sandbox" ]   // on the npc: Sleep FIRST (priority), sandbox fallback LAST
```
- **`lockDoors` defaults true** (an NPC locks its *own house* at night) — set **false** for a follower
  sleeping in a shared space (an inn), or she'll lock the building.
- Like vanilla `NearEditorLocation` slots, the bed search anchors on **`NearSelf`** (our generated NPCs
  have no CK Editor Location, which would silently no-op) — she finds a bed within `radius` of where
  she is, so keep her placed in a room that *has* beds and widen `radius` (~1024) to reach them.
- **More tiers** (a midday meal spot, a workbench shift) are the same pattern: add more scheduled
  packages *above* the fallback. Relocating her between zones needs a `location`/Travel `place` ref to
  a placed marker; without one each tier sandboxes/sleeps around her current spot.
- This whole routine only runs in downtime — while she's actively following, the alias package
  overrides every package in her list (Sleep included).

**Situational dialogue** — gate a *player-initiated* line on RUNTIME state, ANDed with the
follower gate, so the right line only appears in context. Uses the runtime CTDA functions:
```jsonc
// "You're hurt?" — only when she's below half health
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetActorValuePercent", "comparison": "<", "value": 0.5, "actorValue": "Health", "runOn": "Subject" } ]
// "Make camp?" — only after 7pm.  GetCurrentTime is no-arg (game hour 0..24); no param/ref.
"conditions": [
  { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
  { "function": "GetCurrentTime", "comparison": ">=", "value": 19 } ]
```
Runtime condition functions available: `GetActorValuePercent` (0..1 fraction, AV arg),
`GetCurrentTime` (hour 0..24), `IsInInterior`, `IsInCombat`, `GetRandomPercent` (0..99 roll, for
line variety) — all in addition to the static gates (GetInFaction/GetItemCount/GetGlobalValue/…).
Follower-only **backstory** is the same pattern with just the `CurrentFollowerFaction==1` gate and
more response lines.

**Proactive banter** (It.34, in-game confirmed) — lines she says *unprompted*. Use the `banter` section (not `dialogue`):
all entries sharing a (speaker, quest) collapse into one ambient topic (Misc / SNAM=`IDLE`, no branch)
with Random-flagged INFOs; the engine plays a matching one on its own. **Requires idle chatter
enabled** — the Sandbox package above (or the vanilla follow package) provides it.
```jsonc
"banter": [
  { "editorId": "MF_BHurt", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "I'm bleeding... give me a breath." ], "emotion": "Sad",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetActorValuePercent", "comparison": "<", "value": 0.4, "actorValue": "Health", "runOn": "Subject" } ] },
  { "editorId": "MF_BNight", "questEditorId": "MF_Q", "speakerNpcEditorId": "MF_Npc",
    "responses": [ "Quiet, this hour." ], "emotion": "Neutral",
    "conditions": [
      { "function": "GetInFaction", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x05C84E", "runOn": "Subject" },
      { "function": "GetCurrentTime", "comparison": ">=", "value": 22 } ] }
]
```
Gate each on `CurrentFollowerFaction==1` (so she only banters while travelling with you) + a
situational function. NOTE: ambient/idle only — true combat shouts (Taunt/Attack subtype) aren't
supported yet. Vanilla reference probed for this: `HirelingIdles` (Skyrim.esm 0x055DEB).
