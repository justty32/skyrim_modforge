# Animation §5 — Getting a custom animation to actually PLAY (the integration layer)

← [animation index](README.md)

The real deliverable. Three tiers, easiest→hardest:

### (a) Replace an existing animation (zero behavior edits)
Drop your `.hkx` at a **vanilla animation path** (e.g. `...\animations\mt_idle.hkx`). The graph already references that path → it plays your motion. **Pros:** no behavior editing, immediate. **Cons:** global override (every actor playing that idle now plays yours). Simplest win, perfect MVP.

### (b) IDLE record + existing behavior (what ModForge already does)
The graph exposes a finite set of **idle handles / animation events.** An **IDLE record** (`PlayIdle` / `Debug.SendAnimationEvent`) triggers a clip *through a handle the graph already has*. ModForge drives this via the SCEN SceneAdapter `PlayIdle` fragment. **Addressable space without touching behavior = the set vanilla already wires** (bows, gestures, furniture idles, the offset/IdleGive/IdleSilentBow family already decoded). You **cannot** introduce a genuinely new motion category this way — only ride existing handles (and, with (a), replace what a handle points at).

### (c) New animations via a framework (the modern answer)
To **add** animations without hand-editing Havok behavior, use a framework that *patches/generates the graph for you*:
- **FNIS** (legacy) / **Nemesis** (your baseline) — generate patched behavior `.hkx` from a mod-supplied list. Nemesis more capable but a Windows exe (Linux problem, [§6](linux-workflow-modforge.md)).
- **DAR (deprecated) → OAR (Open Animation Replacer)** — SKSE-plugin frameworks doing **condition-based replacement at runtime**: register a folder of replacement clips + a condition set, OAR swaps them in-engine.
- **Pandora Behaviour Engine+** — the modern, *cross-platform .NET* Nemesis/FNIS replacement ([§6](linux-workflow-modforge.md)).

**OAR is the pragmatic modern answer for a record-layer tool — its registration is pure folder + JSON, fully generatable.** Structure:
```
Data\Meshes\actors\character\animations\OpenAnimationReplacer\
  <ModName>\
     config.json                 ← {name, description}  (mod level)
     <SubmodName>\
        config.json              ← {name, description, priority, conditions[...]}
        <clip>.hkx               ← same filename as the vanilla anim being replaced
```
- The **submod `config.json`** carries `priority` (higher wins) + a **`conditions` array** (e.g. `IsActorBase` with plugin/formID, `Random`, `IsEquippedType`, comparisons), each with `negated` + `requiredVersion`. E.g. restrict an idle to the player via `IsActorBase("Skyrim.esm", 0x000007)`.
- OAR **matches by the replaced clip's path/filename** and applies the highest-priority submod whose conditions pass. `user.json` overrides `config.json`. Devs recommend the in-game editor, but the JSON **is** a stable documented schema — machine-generation is viable (the **DAR-to-OAR Converter** generates these JSONs programmatically, proving determinism).
- OAR **needs Nemesis or Pandora run once** to establish base behavior, but **OAR itself adds no behavior edits** — runtime condition-based swapping on top.

**Can ModForge generate the OAR structure? Yes — unambiguously.** Folder tree + `config.json` (name/description/priority/conditions) is exactly the deterministic record+asset artifact ModForge produces. **Highest-leverage integration target.**
