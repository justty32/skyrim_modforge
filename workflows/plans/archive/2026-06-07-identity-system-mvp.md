# Identity System MVP (sub-project B) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use `- [ ]`.
> Compressed for autonomous single-session execution (author = executor): tasks list file targets +
> key code/test sketches rather than every micro-step. TDD + commit per task throughout.

**Goal:** A lightweight identity/class system: do something → gain an identity (a FACT) → it grants a
standing ability AND gates identity-specific dialogue. Headline: a Paladin oath (read book → MessageBox
→ bow oath scene [reuses PlayIdle] → join faction + gain Smite ability → NPCs greet you as a paladin).

**Architecture:** Each identity = a `faction` (persistent signal) + `priority` + optional `grants`
(abilities) + optional `onAcquire.scene`. Acquire via a reusable `MFIdentityBook.psc` (OnRead →
MessageBox → AddToFaction/RemoveFromFaction + player AddSpell/RemoveSpell + optional Scene.Start),
attached to a BOOK with bound properties — no dispatcher/SM glue. Gate via `identity`/`primaryIdentity`
tags on dialogue that expand to `GetInFaction` CTDA (primary also excludes higher-priority identities).
Grant = add the ability on join. Pure-data primary resolution (priority + GetInFaction exclusion).

**Tech Stack:** C# net10.0, Mutagen 0.53.1, xUnit, Papyrus (Wine/native; package-time only).

**Design:** `workflows/specs/2026-06-06-identity-system-design.md` (MVP = sub-project B).
**Reuses:** PlayIdle scene-action (just shipped) for the oath performance.

**Test cmd:** `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj`

---

## Task 1: `IdentitySpec` model + `ModSpec.Identities` + schema

**Files:** Create `src/ModForge.Core/Spec/Spec.Identity.cs`; Modify `src/ModForge.Core/Spec/Spec.cs` (add `Identities`);
`examples/spec.schema.json`; Test `tests/ModForge.Core.Tests/Build/IdentityTests.cs`.

- [ ] Test: `new IdentitySpec()` defaults — `Priority==0`, `Grants` empty, `Toggle==false`, `Default==false`, `Faction==""`.
- [ ] Implement `IdentitySpec { string Id; string Faction; int Priority; List<string> Grants; bool Toggle; bool Default; IdentityAcquireSpec? OnAcquire; }` and `IdentityAcquireSpec { string Scene; }`. Add `List<IdentitySpec> Identities` to `ModSpec`.
- [ ] Schema: add `identities` array + `identity` object def.
- [ ] Commit `feat(identity): IdentitySpec model + ModSpec.Identities`.

## Task 2: Build a FACT per identity + validate

**Files:** Create `src/ModForge.Core/Build/Generator.Build.Identity.cs`; Modify `Generator.Build.cs` (call `BuildIdentities` in pass 1); `Generator.Validate.cs` (+`ValidateIdentities`); Test `IdentityTests.cs`.

- [ ] Test: a spec with `identities:[{id:Paladin, faction:MF_FactPaladin, priority:30}]` builds a FACT editorId `MF_FactPaladin`. (If `faction` is an in-spec editorId not already a `factions[]` entry, BuildIdentities creates the FACT; if it's an external ref `<master>:0x..`, it's used as-is — no build.)
- [ ] Test: validate flags duplicate identity `id`, empty `faction`, and a `grants` ref that doesn't resolve.
- [ ] Implement `BuildIdentities`: for each identity whose `faction` is a bare in-spec editorId with no matching external ref and no existing `factions[]`/built FACT, create a `Faction { EditorID=faction, Name=id }`. Record an `identityByName` map (id → resolved faction ref + priority + grants) for Task 3/4.
- [ ] `ValidateIdentities`: unique ids; non-empty faction; each `grants` ref CheckRef; `onAcquire.scene` (if set) is a scene editorId.
- [ ] Commit `feat(identity): build a FACT per identity + validation`.

## Task 3: `identity` / `primaryIdentity` tags → CTDA expansion on dialogue

**Files:** Modify `src/ModForge.Core/Spec/Spec.Dialogue.cs` (add `Identity`/`PrimaryIdentity` string fields to `DialogueSpec`); `src/ModForge.Core/Build/Generator.Build.Conditions.cs` (expand tags into CTDA on the INFO, alongside existing `Conditions`); Test `IdentityTests.cs`.

- [ ] Test: a dialogue line with `identity:"Paladin"` builds an INFO whose conditions include a `GetInFaction` (param = Paladin's faction, `>= 1`) run on the player. A line with `primaryIdentity:"Paladin"` ALSO adds, for every identity with higher priority, a `GetInFaction(thatFaction) == 0` exclusion.
- [ ] Implement: a helper `ExpandIdentityConditions(string identity, string primaryIdentity, identityTable)` → `List<Condition>`. `GetInFaction` runs on the player (RunOnTarget or a player Reference). Merge the produced conditions into the INFO's CTDA list (AND).
- [ ] Commit `feat(identity): identity/primaryIdentity dialogue gating via GetInFaction CTDA`.

## Task 4: Acquire — reusable `MFIdentityBook.psc` + book attach

**Files:** Create `assets/papyrus/MFIdentityBook.psc`; Modify `Generator.Build.Identity.cs` (attach the book script + bind properties when an identity declares an `acquireBook`); add `IdentitySpec.AcquireBook` (a BOOK editorId) + `IdentitySpec.AcquireText` (MessageBox prompt). Embed `MFIdentityBook.pex` in CLI like the dispatcher. Test `IdentityTests.cs`.

`MFIdentityBook.psc` (extends Book):
```papyrus
Scriptname MFIdentityBook extends Book Hidden
Faction Property TheFaction Auto
Spell  Property GrantAbility Auto      ; optional
Scene  Property AcquireScene Auto      ; optional
Bool   Property Toggle = false Auto
Message Property Prompt Auto           ; optional yes/no; if None, acquire unconditionally
Function OnRead()
    Actor p = Game.GetPlayer()
    Bool has = p.IsInFaction(TheFaction)
    If Toggle && has
        p.RemoveFromFaction(TheFaction)
        If GrantAbility
            p.RemoveSpell(GrantAbility)
        EndIf
        Return
    EndIf
    If has
        Return
    EndIf
    If Prompt && Prompt.Show() != 0
        Return
    EndIf
    p.AddToFaction(TheFaction)
    If GrantAbility
        p.AddSpell(GrantAbility, false)
    EndIf
    If AcquireScene
        AcquireScene.Start()
    EndIf
EndFunction
```

- [ ] Test: an identity with `acquireBook:"MF_PaladinTome"` + `grants:["MF_AbilSmite"]` + `onAcquire.scene:"MF_OathScene"` attaches the `MFIdentityBook` VMAD to the book with `TheFaction`/`GrantAbility`/`AcquireScene` object properties bound (gated on the .pex like other fragment attaches).
- [ ] Implement the attach in `BuildIdentities` pass 2 (mirror `AttachScripts`/`AttachSceneFragments` gating + `ScriptObjectProperty` binding). Package compiles `MFIdentityBook.psc` (embed in CLI; compile loop in `Package.cs`).
- [ ] Commit `feat(identity): MFIdentityBook OnRead acquire (faction+ability+scene) attach`.

## Task 5: Showcase + docs + package

**Files:** Create `examples/identity-paladin.json`; Modify `docs/SPEC-*.md` + `docs/CODE_MAP.dialogue-quests.md` (or a new identity section) + schema; package.

- [ ] `examples/identity-paladin.json`: Paladin tome (book + MFIdentityBook) → MessageBox → `MF_OathScene` (the bow oath scene from scene-playidle, reused) + `AddToFaction MF_FactPaladin` + `AddSpell MF_AbilSmite` (a simple ability MGEF/SPEL); an NPC with a `primaryIdentity:"Paladin"` Hello line ("Well met, paladin."). Merchant tome (toggle). Adventurer default (a startGameEnabled quest OnInit adding the default faction — or note as deferred).
- [ ] `validate` + `build` clean; structural verify (FACT built, book VMAD bound, identity Hello CTDA = GetInFaction).
- [ ] `package` → `~/skyrim_mods/ModForgeIdentity.zip` (esp + MFIdentityBook.pex + SF_ oath pex). Structural self-check.
- [ ] Docs: CODE_MAP rows for `Generator.Build.Identity.cs` + `MFIdentityBook.psc` + tests; SPEC identity section; CLAUDE.md landed-feature line (after in-game).
- [ ] Commit `feat(identity): Paladin/Merchant showcase + docs + package`.

---

## Self-Review notes
- **Spec coverage:** identities model→T1; FACT build (Acquire-data) + validate→T2; Gate (identity/primaryIdentity CTDA)→T3; Acquire (book OnRead) + Grant (AddSpell)→T4; showcase (Paladin chain reuses PlayIdle, Merchant toggle)→T5. Adventurer-default auto-grant = lightest, may defer to a follow-up (note in T5).
- **Risk:** highest in T4 (new Papyrus OnRead + AddToFaction/AddSpell runtime) — mirrors proven trigger scripts; in-game is final judge (user batch-tests). primaryIdentity exclusion CTDA (T3) needs in-game confirm.
- **Deferred to Phase-2/C (NOT MVP):** activeWhen contextual conditions, reputation/behavior tracking, controller-managed primary + manual override, Dragonborn-on-first-shout, identity-linked interactions (merchant trade UI, escort quests).
