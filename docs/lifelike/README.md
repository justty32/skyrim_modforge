# Lifelike NPCs — making generated NPCs feel alive

The distilled "what we know now" for authoring NPCs that move, fight, speak, and live in the world.
This page is the hub: the complete recipe + the one key insight. Everything else is split out:

- **[gotchas.md](gotchas.md)** — the traps that bit us, with the fix (grouped by area)
- **[formid-reference.md](formid-reference.md)** — every vanilla FormID we use (templates, voices, factions, CombatStyles, markers, spells, lights, …)
- **[cheatsheets.md](cheatsheets.md)** — diagnostic commands, in-game console, Papyrus + CJK setup
- **[cookbook.md](cookbook.md)** — copy-paste recipes (inn patron, commuter, mage, ritual caster, follower, craftable item, …)
- **[../engine-internals.md](../engine-internals.md)** — the *why*: override semantics, GRUP formulas, PACK templates, the localized-string landmine
- **[../SPEC.md](../SPEC.md)** — full per-field spec reference · **[../FOR_AGENT.md](../FOR_AGENT.md)** — the agent workflow

## TL;DR — the complete NPC recipe

```jsonc
{
  "race":         "Skyrim.esm:0x013746",     // NordRace (or other)
  "class":        "<MF_YourClass>",
  "voiceType":    "Skyrim.esm:0x013AE6",     // MaleNord — hello/idle audio
  "outfit":       "Skyrim.esm:0x09D5DF",     // BlacksmithOutfit01 (any vanilla outfit)
  "level":        25,
  "autoCalcStats": true,                       // class drives H/M/S + skill values

  // CITIZENSHIP — required for cross-cell Travel (engine refuses door teleports without it)
  "crimeFaction": "Skyrim.esm:0x0267EA",       // CrimeFactionWhiterun
  "factions":     [ "Skyrim.esm:0x0267EA",     // (reinforcing)
                    "Skyrim.esm:0x028172" ],   // TownWhiterunFaction
  "unique":       true,                         // engine AI tracking — vanilla cross-cell NPCs all have this

  // COMBAT — both systems must be authored
  "combatStyle":  "<MF_YourCS>",               // HOW he fights (weapon-class preference)
  "spells":       [ "Skyrim.esm:0x0C969A" ],   // WHAT he casts (FlamesRightHand)
  "aggression":   "Aggressive",                 // WHETHER he fights — default Unaggressive = won't even defend
  "confidence":   "Brave",                      // default Cowardly = flees any threat
  "assistance":   "HelpsFriendsAndAllies",
  "energyLevel":  50,

  // BEHAVIOUR — engine evaluates in list order
  "packages":     [ "<MF_TravelPkg>",          // first priority — go somewhere
                    "<MF_SandboxPkg>" ]        // fallback — what to do once arrived
}
```

Drop any line above and the actor degrades visibly in-game — see [gotchas.md](gotchas.md) for the
exact failure mode each omission produces.

## The KEY insight — Skyrim NPC AI has two independent systems

| System | Decides | Authored via | Default if unset |
|---|---|---|---|
| **CombatStyle** (CSTY) | **HOW** the NPC fights (magic vs melee vs staff vs ranged) | `combatStyle` ref → CSTY record (`equipMult*` fields) | Flat default — picks whatever weapon the actor happens to hold |
| **AIData.Aggression + Confidence** | **WHETHER** the NPC fights at all | `aggression` / `confidence` on the NPC | `Unaggressive + Cowardly` → **flees any threat**, regardless of CombatStyle |

A CSTY-only setup gives you "wants to use magic but flees the moment it sees a wolf". **Both systems
must be authored.** (And a third axis: `aggression` governs *initiation* — an `Unaggressive` +
`Brave` NPC won't start fights but stands and fights once attacked, the right tuning for a townsperson.)
