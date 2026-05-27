# ModForge — working notes (autonomous-loop anchor)

> Scratch/handoff file. Each loop iteration: read this, do the next unchecked item,
> update it, commit. Build/test commands at the bottom.

## Where we are (2026-05-24)
- New standalone repo (C#/.NET/Mutagen), separate from the SKSE `my_skyrim_plugin_1` repo.
- Foundation proven on this Linux box: Mutagen generates valid `.esp`/`.esl`; Papyrus
  compiles via Wine; Mutagen VMAD bridges scripts↔forms (see the parent repo's memory
  `project_authoring_toolchain_roadmap`).
- **Translate pipeline DONE incl. CJK** (2026-05-24): `extract` → fill JSON `target` →
  `apply` (Latin/inline) **or** `applyloc` (CJK). CJK was unblocked by the user's official
  CHS mod: inspection showed Simplified-Chinese SSE uses **Localized `<plugin>_chinese.STRINGS`
  in UTF-8** (NOT GBK). `applyloc` writes exactly that: sets `TranslatedString.DefaultLanguage
  = Language.Chinese`, `UsingLocalization = true`, a `StringsWriter` with a UTF-8
  `IMutagenEncodingProvider`, then lowercases `_Chinese`→`_chinese` (Mutagen capitalizes it;
  case matters on Linux/Proton). Verified: CnDemo → Strings/CnDemo_chinese.STRINGS with valid
  UTF-8 Chinese. Official CHS mod reference extracted at /tmp/chs-mod (Strings/*_chinese.*).
  **PAUSED 2026-05-24**: user hasn't installed a CJK font yet → CJK content + in-game testing
  on hold. `applyloc` code is done + byte-verified; don't push CJK further until the font's in.

## Current focus: the ESP GENERATOR (spec → plugin)
Generalize the hardcoded `gen` demo into a data-driven `build <spec.json> <out.esp>`.
Layered design: structured spec (JSON IR, human/AI-reviewable) → Mutagen → plugin.
(The NL→spec layer is an AI agent driving this tool per `FOR_AGENT.md` — NOT an in-tool LLM
call; the spec IS the contract.)

### Iterations
- [x] **It.1 — basic records**: spec for MiscItem / Book / Weapon / Npc; `build` command;
      sample spec; round-trip test (build → extract verifies names/text).
- [x] **It.2 — quest + dialogue in spec** (done 2026-05-24): spec now has `quests`
      (+objectives) and `dialogue` (topic prompt + responses, referencing quest &
      speaker NPC by editorId; GetIsID condition). Verified: sample_spec → 7 records +
      1 dialogue topic; extract shows quest name/objective + prompt + response line.
      (Gotcha: `DialogResponse.ResponseNumber` is `byte`.)
- [x] **It.3 — more record types** (done 2026-05-24): added Spell, Potion(Ingestible),
      Armor (value/weight/armorRating), Faction, Message(description) to spec + Build.
      Verified: sample_spec → 12 top-level records, extract shows all. Also fixed an
      extract cosmetic bug (`TrimStart('I')` was eating the 'I' in "Ingestible"; concrete
      record names have no interface prefix). Still TODO if wanted: MagicEffect, Container,
      Activator, Ammunition, Ingredient — same trivial pattern.
- [x] **It.4 — refs & FormLinks across records** (done 2026-05-24): two-pass build —
      pass 1 creates all records, then one editorId→FormKey table from
      `EnumerateMajorRecords()` resolves forward refs. Demo: NpcSpec.factions (list of
      faction editorIds) → `Npc.Factions` RankPlacement (FormLink<IFaction>, Rank 0).
      sample_spec wires 1 link (MF_Smith → MF_Guild). Build prints a "cross-ref link(s)"
      count. (Round-trip currently trusted from build output; a `dump` command — list
      records + key FormLinks — would let extract-style verify links. Good next helper.)
      More refs are the same pattern: container contents, leveled lists, npc CrimeFaction,
      keywords, npc→class/race/outfit.
- [~] **It.5 — Papyrus hook** (in progress):
  - [x] **5a — compile command** (done 2026-05-24): `compile <script.psc> <outDir>` drives
        the CK's `PapyrusCompiler.exe` under `wine` from C# (Process), parses stdout
        (`Failed on`) + checks the `.pex` exists (exit code is unreliable). Verified:
        examples/scripts/MFDemoQuestScript.psc → valid .pex (magic fa57c0de). Paths via
        env `MODFORGE_PAPYRUS_COMPILER` / `MODFORGE_PAPYRUS_BASE`, defaults to the local CK
        + `~/.cache/modforge/papyrus/Source/Scripts`.
        **PREREQ (one-time, already done on this box):** extracted base sources +
        TESV_Papyrus_Flags.flg from `<CK>/Data/Scripts.zip` (`Source/Scripts/*`, 14301 .psc)
        to `~/.cache/modforge/papyrus/`. (For SKSE functions, add the SKSE .psc to that dir.)
  - [x] **5b — VMAD attach** (done 2026-05-24): spec.scripts[] attaches a script (by
        Scriptname) to any record by editorId, with typed properties (int/float/bool/
        string/object; object resolves an editorId→FormLink). The VMAD setter isn't on
        IHaveVirtualMachineAdapter (get-only) and its type varies (Quest→QuestAdapter,
        else VirtualMachineAdapter), so Build REFLECTS the concrete `VirtualMachineAdapter`
        property + `System.Activator.CreateInstance`s the right adapter, then adds a
        ScriptEntry to `.Scripts`. Properties get `ScriptProperty.Flag.Edited`. Verified:
        sample attaches MFDemoQuestScript to MF_Q1 (int GreetingCount=3 + object PlayerRef
        →MF_Smith); `strings` confirms script+prop names in the esp; re-read OK.
  - [x] **5c — packaging** (done 2026-05-24): `package <spec.json> <outModDir>` = build esp
        + compile each script `source` + lay out a MO2/Vortex-ready folder
        (`<PluginName>` at root, `Scripts/*.pex`, `Scripts/Source/*.psc`). ScriptAttachSpec
        gained an optional `source` (.psc path rel. to spec). Verified: sample_spec →
        SampleMod/ with SampleMod.esp + Scripts/MFDemoQuestScript.pex + Source/.psc.
        **It.5 COMPLETE — full spec→packaged-mod pipeline works on Linux.**
- [x] **It.6 — NL→spec layer** (the "AI agent" front) — delivered as an AGENT-DRIVEN workflow,
      not an in-tool LLM call (see 6c):
  - [x] **6a — `validate <spec.json>`** (done 2026-05-24): semantic guardrail — editorId
        presence/uniqueness + referential integrity (dialogue→quest/npc, npc→faction,
        script→target, object-prop→record, property types). Exit non-zero on any problem
        so an NL→spec front can self-correct. Verified: good sample passes; a broken spec
        surfaces all 4 seeded errors.
  - [x] **6b — SPEC.md + spec.schema.json** (done 2026-05-24): `SPEC.md` full field
        reference + NL→spec workflow; `examples/spec.schema.json` (draft 2020-12, enum on
        property type, object-prop conditional-required); README CLI list refreshed to all
        7 commands. Both JSON files parse-validated.
  - [DROPPED] **6c — live in-tool LLM hook** (NOT doing it — user decision 2026-05-25): a
        `describe "<NL>"` command calling an LLM API was once planned. **Dropped:** the NL→spec
        layer is delivered by an AI agent (Claude Code) driving this tool — the agent reads
        `FOR_AGENT.md`, writes spec.json, runs validate→build→package. An in-tool LLM API would
        be a redundant extra layer + key/provider management. So THIS IS the workflow, not a
        stopgap; don't plan/build in-tool LLM integration.

- [~] **It.7 — gameplay-complete fields** (autonomous track, originally run while 6c was deferred):
  - [x] **7a — `dump <esp>`** (done 2026-05-24): reads a plugin back + prints records,
        names, npc faction membership (FormLink resolved to editorId), VMAD scripts +
        prop count, dialogue prompt/INFO groups, quest objectives. Round-trip verification
        helper. Confirmed It.2/It.4/It.5b actually persist (Smith→Guild faction, Q1→script
        [2 props] + objective all read back correctly). Also a general .esp inspector.
  - [x] **7a+ — `find <plugin> <query> [type]`** (done 2026-05-24): search a master
        (Skyrim.esm, 250 MB) for records whose EditorID/Name contains <query>, print
        `Skyrim.esm:0xFORMID  Type  EditorID`. Lazy read-only **overlay** (`CreateFromBinaryOverlay`)
        so the master isn't fully materialized; optional [type] uses typed group enumeration
        (`EnumerateMajorRecords(I<Type>Getter)`) to skip irrelevant groups (~0.9s typed vs
        ~3.3s full-ESM). Name is localized + BSA-packed → unresolvable headless (no plugins.txt);
        resolved **best-effort**, falls back to EditorID-only (which is inline + always read,
        and descriptive: `NordRace`, `IronSword`). Verified FormIDs match vanilla
        (IronSword=0x012EB7, NordRace=0x013746). `MODFORGE_DEBUG=1` prints full stack on error.
  - [x] **7b — external/vanilla form refs** (done 2026-05-24): a **ref** is an in-spec editorId
        OR external `"<master>:0xFORMID"`. Central resolver `TryResolveRef` (+ `LooksExternalRef`/
        `TryExternalRef`, mask off the master-index byte). Wired: npc `race`/`class`/`outfit`
        (Race=IFormLink, DefaultOutfit=IFormLinkNullable), npc `factions` (now external-capable),
        armor/weapon/misc `keywords` (cast to `IKeyworded<IKeywordGetter>`, `.Keywords` list),
        and script object-properties. Master auto-added on write (MastersListContent=Iterate, no
        manual MAST needed; ESL+master OK). `validate` accepts/format-checks refs; `dump` shows
        race/class/outfit/keywords + masters list. Sample wires NordRace + VendorBlacksmith +
        BlacksmithOutfit01 + CrimeFactionWhiterun + ArmorClothing — round-trip verified via dump.
        **NPCs are now functional actors (race+class); still NOT world-placed.**
  - [x] **7c — gameplay stats on existing record types** (done 2026-05-24): the
        self-contained, headless-verifiable half of "make generated records functional".
        - **Weapons:** `WeaponSpec` gained `value`/`weight`/`damage`(ushort)/`speed`/`reach`.
          Build sets `Weapon.BasicStats` (WeaponBasicStats{Damage,Value,Weight}) + `Weapon.Data`
          (WeaponData{Speed,Reach}) whenever any stat is given; speed/reach default to 1.0 so the
          weapon is swingable (0 = unusable).
        - **Armor:** `armorType` (light/heavy/clothing → `ArmorType` enum via `ParseArmorType`)
          + `slots` (BipedObjectFlag names, OR'd via `Enum.TryParse`) → `Armor.BodyTemplate`
          (ArmorType + FirstPersonFlags).
        - **Spell/Potion effects:** new `EffectSpec{magicEffect(ref),magnitude,area,duration}`;
          Spell+Ingestible both implement `IHasEffects` → one `WireEffects` path in pass 2 adds
          `new Effect{ BaseEffect.SetTo(fk), Data = new EffectData{...} }` to `he.Effects`.
          magicEffect resolves through the existing `Resolve`/`TryResolveRef` (so vanilla
          `Skyrim.esm:0x03EB15`=AlchRestoreHealth, `0x03EB42`=AlchDamageHealth work + auto-master).
        - `validate` checks armorType enum / slot names / effect refs (+empty magicEffect);
          `dump` prints weapon damage/speed/reach, armor type/slots, and effect→magEffect+mag/area/dur.
        - Verified: sample_spec round-trips via dump (Blade dmg12 spd1 reach1; Apron Clothing/Body;
          Spark→AlchDamageHealth mag10 dur5; Tonic→AlchRestoreHealth mag25). Negative test: 5/5
          bad fields caught by validate. NOT in-game tested (needs Proton). API verified via ilspy.
  - [x] **7d — world placement** (the real "appears in-game" blocker — all 3 phases done):
    - [x] **7d phase 1 — new interior cells + placement** (done 2026-05-24): `cells` (new
          interior Cell, IsInteriorCell, reachable via `coc <editorId>`) + `placements` (a
          base ref → `PlacedNpc` (ACHR) if the base is an NPC else `PlacedObject` (REFR), with
          `Placement{Position, Rotation}`; rotation authored in degrees → radians). Cell nesting
          = one `CellBlock`(GroupType InteriorCellBlock=2) → `CellSubBlock`(InteriorCellSubBlock=3)
          → Cells at block 0/0 (interior block numbers aren't engine-enforced). Records made with
          `new Cell(mod, editorId)` / `new PlacedNpc(mod)` / `new PlacedObject(mod)` (auto FormKey);
          placed refs go in `cell.Temporary` (or `.Persistent` if `persistent:true`). base resolves
          through the existing ref resolver (in-spec or external). `validate` registers cell ids +
          checks placement base/cell/kind (and flags external-cell placement as unsupported); `dump`
          prints cell interior/persistent/temporary counts + each placed npc/obj's base + position.
          Verified: sample → MF_TestRoom with PlacedNpc(MF_Smith)+PlacedObject(MF_Coin), dump
          round-trips (so the bytes re-parse via CreateFromBinary); negative test 5/5 caught.
          **NOT in-game tested** (needs Proton): coc reachability + actor actually standing there.
          Mutagen API (Cell/CellBlock/CellSubBlock/PlacedNpc/PlacedObject/Placement/P3Float/
          GroupTypeEnum) all verified via ilspy.
    - [x] **7d phase 2 — placement into a VANILLA INTERIOR cell** (done 2026-05-24): `placements[].cell`
          now also accepts an external `"<master>:0xFORMID"` (find it: `find <Skyrim.esm> <name> Cell`,
          e.g. WhiterunBanneredMare = Skyrim.esm:0x01605E). Lazily loads the master as an overlay +
          `ToImmutableLinkCache<ISkyrimMod,ISkyrimModGetter>()` (data dir from MODFORGE_SKYRIM_DATA or
          the Steam default), `TryResolve<ICellGetter>` the cell.
          **PITFALL hit:** the obvious `cache.TryResolveContext<ICell,ICellGetter>(fk).GetOrAddAsOverride(mod)`
          throws `Could not determine plugin listings path` — GetOrAddAsOverride DEEP-COPIES the cell,
          and copying the localized `Name` (`TranslatedString.DeepCopy`) resolves string sources →
          BSA archive load order → plugins.txt (absent headless on Linux; same wall as `find`'s Name).
          **WORKAROUND (manual override, no deep copy):** make `new Cell(vanillaFk, SkyrimRelease)`
          (same FormKey = an override), copy ONLY `Flags` off the getter (so the interior flag isn't
          blanked; Flags is inline, not localized), leave Name/Lighting/etc null → omitted on write →
          inherited from master (no ITM, no BSA read). Add it to our interior block (shared lazy
          `InteriorSub()`), then add our placed ref to its Temporary. Vanilla refs are NOT re-stated
          (they come from the master; omitting ≠ deleting) → no bloat/conflict. One override per cell
          (cached in `vanillaCellOverrides`); multiple placements into the same cell share it.
          Only **interior** cells (checks `Cell.Flag.IsInteriorCell`); exterior warns + skips.
          Verified: sample places MF_Chest into Bannered Mare → dump shows `[01605E:Skyrim.esm] Cell`
          (an override) temporary=1 + our PlacedObject, master=[Skyrim.esm], re-parses via CreateFromBinary.
          NOT in-game tested.
    - [x] **7d phase 3 — placement into the EXTERIOR / open world** (done 2026-05-25): `placements[]`
          gained `worldspace` (a `<master>:0xFORMID` ref, e.g. Tamriel = `Skyrim.esm:0x00003C`). When set,
          `position` is the WORLD position; the target exterior cell = `floor(x/4096), floor(y/4096)`. We
          find the existing master cell at that grid and OVERRIDE it (same Flags+Grid-only manual override
          as p2 — no localized deep-copy), hosted on a minimal `Worldspace` override that re-states only
          our block tree (vanilla cells stay in the master). Mutagen nesting:
          `Worldspace.SubCells` (`ExtendedList<WorldspaceBlock>`, GroupType=ExteriorCellBlock=4,
          BlockNumberX/Y) → `WorldspaceBlock.Items` (`WorldspaceSubBlock`, ExteriorCellSubBlock=5) →
          `WorldspaceSubBlock.Items` (`Cell`); cell grid = `Cell.Grid.Point` (`Noggog.P2Int`).
          **THE FOOTGUN (verified, not guessed):** block = floor(grid/32), sub-block = floor(grid/8), and
          this must be FLOOR division (toward -inf), NOT C#'s truncating `/`. PROVED against real Tamriel
          via a throwaway `_probe`: cell (5,5)→block(0,0)/sub(0,0); cell (7,-41)→block(0,-2)/sub(0,-6)
          (C# `/` would give (0,-1)/(0,-5) — wrong group). Helpers `FloorDiv`/`PosToGrid`/`CellSize=4096`.
          If a grid has no master cell, a NEW exterior cell `MF_Ext_<x>_<y>` is made at that grid (warns;
          structural-only). `worldspace` wins over `cell` if both set. validate: worldspace must be a
          well-formed external ref; cell waived when worldspace is set. dump: prints worldspace block/cell
          counts + each cell's `grid=(x,y)`.
          Verified: sample places MF_Coin into Tamriel @ (22528,22528) → overrides master cell
          `[009123:Skyrim.esm]` grid=(5,5) temporary=1, re-parses via CreateFromBinary. Negative-grid test
          @ (30720,-165888) → master cell `[00EEF3:Skyrim.esm]` grid=(7,-41) (FloorDiv path). New-cell test
          @ grid (200,200) → `MF_Ext_200_200` warn. validate negative: 4/4 caught. Mutagen API via ilspy.
          NOT in-game tested (needs Proton: object actually present at the world coords + cell merges clean).
  - [x] **7e — aggregates + spell cast-type** (done 2026-05-24): self-contained, low-risk.
        - **LeveledItem (LVLI) / LeveledNpc (LVLN):** `leveledItems`/`leveledNpcs` with
          `chanceNone` (0–100 → `Noggog.Percent`), `flags` (LVLI/LVLN flag names OR'd via the
          generic `ParseFlags<T>`), and `entries[]` (`reference` ref + `level`/`count` shorts).
          Built as `mod.LeveledItems/LeveledNpcs.AddNew()`; entries wired in pass 2 via the ref
          resolver (`Entry.Data.Reference.SetTo(fk)`; LVLI ref = IItem, LVLN ref = INpcSpawn).
        - **Container (CONT):** `containers` with `name`/`weight`/`items[]` (`item` ref + `count`).
          `ContainerEntry{ Item = ContainerItem{ Item.SetTo(fk), Count } }`.
        - **Spell cast-type:** SpellSpec gained `spellType`/`castType`/`targetType` (enums via
          Enum.TryParse) + `baseCost`/`chargeTime`, set in pass 1.
        - validate registers the ids + checks entry/item refs, flag names, and the 3 spell
          enums; dump prints lvli/lvln entries, container contents, and spell type/cast/target/cost.
        - Verified: sample (MF_LootList, MF_GuardList, MF_Chest, MF_Spark cast-type) round-trips
          via dump (13 cross-ref links); negative test 6/6 caught. Mutagen API verified via ilspy.
          NOT in-game tested.
  - [x] **7f — long-tail record types** (done 2026-05-25): nine more types, all the same
        spec-class + pass-1-build (+ pass-2 ref wiring) pattern, reusing the existing
        `WireKeywords`/`WireEffects`/`Resolve` helpers:
        - **Ingredient (INGR)** name/value/weight + `effects` (reuses `WireEffects` — Ingredient
          implements `IHasEffects`) + keywords. **Ammunition (AMMO)** name/value/weight + `damage`
          (float) + keywords. **Scroll (SCRL)** name/value/weight + `effects` + spell cast fields
          (`Type`/`CastType`/`TargetType`/`BaseCost`) + keywords. **SoulGem (SLGM)** name/value/
          weight + `maximumCapacity` (`SoulGem.Level` enum) + keywords. **Key (KEYM)** name/value/
          weight + keywords.
        - **Keyword (KYWD)** = just an editorId — lets a spec DEFINE its own keyword and reference
          it in any record's `keywords` (the ref resolver already resolves in-spec editorIds;
          keyword created in pass 1 so it's in the formKey table for pass 2). **Outfit (OTFT)**
          `items[]` (refs → `IFormLink<IOutfitTargetGetter>`); an npc `outfit` ref can now point at
          an in-spec outfit. **Static (STAT)** `model` only (no Name; `r.Model.File.GivenPath = path`,
          `File` is `AssetLink<SkyrimModelAssetType>`). **Activator (ACTI)** name + `model` + keywords
          (+ script via `scripts`).
        - validate: Reg() all nine; keyword/effect/outfit-item refs via CheckRef/CheckEffects; scroll
          spell enums; soulGem `maximumCapacity` enum. dump: ammo damage, scroll cast, soulgem
          capacity, outfit items, static/activator model.
        - Verified: sample now has all nine (25 top-level records, 18 cross-ref links). Highlights:
          in-spec keyword `MF_KwTrinket` referenced by `MF_Gear` + `MF_Herb`; outfit `MF_Outfit` →
          in-spec armor `MF_Apron`; static/activator model paths. build→dump re-parses. validate
          negative: 6/6 caught. Mutagen API (Ingredient/Ammunition/Scroll/SoulGem/Key/Keyword/Outfit/
          Static/Activator + Model.File + SoulGem.Level + Outfit.Items) verified via ilspy. NOT in-game tested.
        Remaining gameplay gaps are minor long-tail (more record types/fields — same pattern) and
        the medium ones that aren't pure pattern-adds: MagicEffect (MGEF — archetypes), Race/Class,
        ConstructibleObject (COBJ crafting — workbench-keyword conditions). World placement
        (interior + vanilla interior + exterior) is complete (7d p1–p3). Biggest unblocked-but-
        untouched item: in-game testing (needs Proton). (It.6c in-tool LLM API was dropped —
        NL→spec is agent-driven; see It.6.)
- [x] **It.8 — FIRST IN-GAME TEST + model gap fix** (done 2026-05-26, Proton/MO2, Skyrim
      1.6.1170). Built a minimal smoke ESP (`examples/proof_spec.json` → `ModForgeProof.esp`:
      weapon/potion/misc/book/npc, all names "ModForge*") and human-tested it. **Core premise
      CONFIRMED:** generated ESP loads, `help "ModForge" 0` lists all 5, names render, additem/
      placeatme work, NPC animates, potion restores HP. **Bug found in-game:** equipping the
      weapon and reading the book CRASHED — root cause: generated records carry **NO 3D model
      (.nif)**. They sit fine in inventory (icon only), but any interaction that attaches a model
      to the scene crashes (weapon equip → skeleton; book read → 3D reading view). Drink/additem
      don't load a model → fine.
    - **Fix:** weapons/books/miscItems/potions take an optional `template` ref (`"<master>:0xFORMID"`,
      a vanilla record of the right kind). Build clones it via `DeepCopyIn` → real model + (weapon)
      firstPersonModel/animationType/equip slot + keywords/sounds, then overrides EditorID/Name/stats.
      proof templates: weapon→IronSword `0x012EB7`, book→Book1CheapNordsArise `0x0ED161`,
      misc→GemRuby `0x063B42`, potion→RestoreHealth06 `0x039BE5`.
    - **Two gotchas (cost a build cycle each):** (1) MUST pass a `TranslationMask { Name=false,
      Description/BookText=false }` to DeepCopyIn — copying a localized TranslatedString from
      Skyrim.esm calls `TranslatedString.DeepCopy()→ResolveAllStringSources()` which enumerates
      BSAs via the load-order/plugins.txt listing → "Could not determine plugin listings path"
      headless on Linux. We override those strings anyway, so skip them. (2) For potions also
      `r.Effects.Clear()` after clone, else the cloned effects + WireEffects-added spec effects
      stack (double potion). DeepCopyIn preserves OUR FormKey (verified: records stay in the
      plugin, master still just Skyrim.esm). MasterCache moved to top of Build() so item loops reach it.
    - **Re-tested in-game (2026-05-26): ALL PASS** — weapon equips + swings (OneHandSword), book
      reads, potion bottle + ruby misc show 3D models when dropped. Same model gap still untouched
      for armor (equip) / ingredient / ammo / scroll / soulGem / key — extend `template` the same
      way when those are exercised. NPCs already fine (race template). dump now prints weapon
      anim/model/firstPersonModel + book/misc/potion model for verification.
- [x] **It.9 — IN-GAME exterior placement + cell/worldspace-env fix** (done 2026-05-26, Proton/MO2).
      `examples/place_spec.json` → `ModForgePlace.esp`: a Talos statue (vanilla base `Skyrim.esm:
      0x0D1846`) + the generated NPC placed in Tamriel at world coords → grid (-23,4) [floor-div on
      negatives confirmed; overrode existing master cell `0x009536`, 0 new cells]. **In-game: statue
      + NPC appeared — open-world placement WORKS.** But two override-completeness bugs surfaced:
    - **"Whole world underwater":** an override CELL/WORLDSPACE does NOT inherit omitted data
      subrecords from the master — the engine defaults them. The minimal worldspace override dropped
      `LandDefaults`, resetting DefaultWaterHeight from Tamriel's real **-14000** to **0**, flooding
      all terrain between -14000 and 0 (player z=-3725). (The cell's own WaterHeight=FLT_MAX is just
      the "use worldspace default" sentinel — a red herring; copying it faithfully changed nothing.)
    - **Save shows "unknown location":** the same minimal worldspace override also blanked the
      worldspace Name.
    - **Fix:** `CopyCellEnv` (cell: water height/textures, lighting+template, regions, imagespace,
      music, acoustic, encounter zone, location, owner, sky-from-region) AND `CopyWorldspaceEnv`
      (worldspace: LandDefaults [land/water defaults], MaxHeight, MapData, Parent, water forms +
      LOD water height, climate, location, encounter zone, interior lighting, music, flags, object
      bounds, map-offset scale, distant-LOD mult). Both SKIP the localized Name (string-lookup
      landmine) + the giant child structures (cell ref lists / worldspace SubCells/TopCell/OffsetData
      — we add only our ref / build our own block tree) + AssetLink texture paths (cosmetic). For the
      Name, restate a plain `"Skyrim"` for Tamriel (0x3C) — headless can't read the localized master
      name (TODO: spec field for other worldspaces). Used hand-copy not DeepCopyIn-mask (worldspace
      SubCells/TopCell are MaskItem sub-masks, awkward to exclude). dump prints cell water/lightTmpl
      + worldspace defaultWater/nameSet.
    - **Re-tested in-game (2026-05-26): ALL PASS** — statue + NPC present, no underwater, location
      name correct. Exterior open-world placement is now in-game-verified end-to-end. (Interior /
      vanilla-interior placement get the same CopyCellEnv but are still not in-game tested.)
- [x] **It.10 — interior (vanilla-cell) placement: FAILED then FIXED, IN-GAME CONFIRMED (2026-05-27).**
      `examples/interior_spec.json` placed a Dibella statue + NobleChest into the Bannered Mare
      (`WhiterunBanneredMare 0x01605E`) at user getpos local coords. dump looked correct (cell
      override with `lightTmpl` carried, 2 temporary refs). **In-game: objects did NOT appear,
      lighting was normal (i.e. the override was effectively IGNORED — vanilla cell intact).**
      SUSPECTED ROOT CAUSE: `InteriorSub()` (Program.cs ~636) hardcodes the override cell into
      `CellBlock{BlockNumber=0}` / `CellSubBlock{BlockNumber=0}`, but Skyrim groups interior cells
      into block/sub-block BY FORMID — exterior computes block/subblock from the grid (works), the
      interior path never did. A cell in the wrong block GRUP isn't matched as an override → ignored.
      NOTE: the tester also saw tofu subtitles + a fogged map — those are unrelated to this ESP (it
      has zero strings + only touches an interior cell); they're from the tester's recent CJK-font/
      UI install, to be checked separately.
    - **ROOT CAUSE CONFIRMED + FIXED (2026-05-27), pending in-game retest.** Added a throwaway-turned-
      kept diagnostic `cellblk <esp> [0xFORMID]` (walks the interior CELL block tree, prints block/
      sub per cell) and walked Skyrim.esm: **WhiterunBanneredMare 0x01605E (dec 90206) lives in block
      6 / sub 0.** Derived + cross-checked the formula over ~40 cells — it's **block = id % 10, sub =
      (id / 10) % 10** (decimal, 24-bit ID). NB: the It.10 guess above had the two HALVES SWAPPED
      (block is id%10, NOT (id/10)%10). The old code hardcoded 0/0, so the override sat in the wrong
      GRUP and the engine never matched it to the master → silently ignored.
    - **Fix:** replaced the single lazy `InteriorSub()` (always 0/0) with `InteriorSubFor(FormKey)` —
      a (block, sub)-keyed get-or-add over `mod.Cells.Records`, computing block/sub from the cell's
      FormID (same get-or-add shape as the exterior `ws.SubCells` path). Applies to BOTH the vanilla-
      cell override (uses the master FormID) and new in-spec interior cells (use their assigned FormID).
      Kept the manual same-FormKey override (not `GetOrAddAsOverride`) to keep dodging the localized-
      Name string-lookup landmine. Structurally verified: interior_spec → override 0x01605E now in
      block 6/sub 0 with lightTmpl + 2 temp refs; sample_spec's two cells split correctly into 4/7
      (new MF_TestRoom 0x00081A) and 6/0 (override) instead of colliding in 0/0.
    - **IN-GAME CONFIRMED (2026-05-27, Proton/MO2):** packaged `ModForgeInterior.zip` (esp at archive
      root, MO2-ready), loaded it, `coc WhiterunBanneredMare` — the Dibella statue + noble chest
      now APPEAR at the authored local coords and the cell lighting is normal. Interior vanilla-cell
      placement is now in-game-verified end-to-end. So ALL THREE placement paths (new interior /
      vanilla interior / exterior worldspace) are in-game-confirmed.
      (Still open for the NEW-interior-cell path: a void cell needs a lighting template + floor
      static, else black/no-floor — addressed in It.11.)
- [ ] **It.11 — new interior cell: lighting + floor (built, NOT YET in-game tested) (2026-05-27).**
      A brand-new in-spec interior cell had no Lighting/LightingTemplate (renders PITCH BLACK) and
      no floor geometry (player `coc`s in and falls into the void). Fix is two parts:
    - **Lighting (code):** added an optional `template` field to `CellSpec` — a vanilla INTERIOR cell
      ref `"<master>:0xFORMID"`. The new-cell build loop resolves it via `TryResolveTemplate<ICellGetter>`
      and runs `CopyCellEnv(tmpl, cell)` (the same env-copy used by the vanilla-override path: inline
      Lighting + LightingTemplate FormLink + water/etc., NO localized Name → no string-lookup landmine).
      `CopyCellEnv` overwrites Flags from the template, so we re-assert `IsInteriorCell` after. Warns +
      continues if the ref is unresolved (cell still builds, just dark) or points at an exterior cell.
      validate: a malformed `template` ref is now a problem. dump already prints `lightTmpl`.
    - **Floor (data, not code):** a floor is just a placed static — the existing placement system already
      does this. No new code; the example demonstrates it.
    - **Example:** `examples/newcell_spec.json` → `ModForgeNewCell.esp`: cell `MF_TestChamber` with
      `template` = WhiterunBreezehome `0x0165A8`; placements = a 3×3 grid of `WRIntFloorSTMid01Large`
      `0x1044AA` (z=0, 256 spacing) + a `DefaultSunlightHalfOmni01` `0x0172C4` omni light at z=300 +
      a `StatueDibella` `0x08F965` landmark + a `NobleChest01` `0x06B30E`. Built + verified: cell
      0x000800 (dec 2048) → block 8/sub 4 (own FormID block), `lightTmpl=06175D:Skyrim.esm` copied,
      12 temporary refs, name preserved, validate clean. Packaged → `~/skyrim_mods/ModForgeNewCell.zip`.
    - **NEXT (needs the tester):** `coc MF_TestChamber` and check: room is LIT (not black), there's a
      walkable FLOOR (don't fall), the Dibella statue + chest are present. UNKNOWNS to watch:
      (1) `coc` with no designated COC marker spawns the player at cell origin (0,0,0) — the center
      floor tile is there, but if the floor-piece mesh pivot isn't at its top surface the player may
      spawn slightly in/under it (tcl to check); (2) floor tile real size vs the 256 spacing — if gaps
      show, tighten spacing. Report back and I'll adjust z/spacing.
- [ ] **It.12 — custom MagicEffect (MGEF) authoring (built, NOT YET in-game tested) (2026-05-27).**
      Picked this big gap (over Race/Class + COBJ) because it's the highest-leverage one: the
      spell/potion/ingredient/scroll `effects[]` pipeline (It.7c) already links a `magicEffect` ref,
      but you could only point at VANILLA effects. Now a spec can DEFINE its own MGEF and reference it.
    - **Spec:** new `magicEffects[]` (MagicEffectSpec): `editorId`, `name`, `description`, `archetype`
      (MagicEffectArchetype.TypeEnum — ValueModifier is the common damage/heal/fortify; also Summon/
      Bound/Light/Paralysis/…), `actorValue` (affected AV: Health/Magicka/Stamina/…), `magicSkill`
      (school = an ActorValue: Alteration/Conjuration/Destruction/Illusion/Restoration), `resistValue`
      (AV that resists), `castType`, `targetType`, `baseCost` (float), `flags[]` (MagicEffect.Flag:
      Hostile/Recover/Detrimental/NoArea/…), `association` (ref → summoned/bound form, optional).
    - **Build:** pass-1 `mod.MagicEffects.AddNew()`, all fields via `Enum.TryParse` (mirrors the spell
      cast-enum idiom); archetype = `new MagicEffectArchetype { Type, ActorValue }`; MagicSkill/
      ResistValue default to `ActorValue.None` when unset. pass-2 wires `archetype.Association` (ref,
      may point forward/vanilla — cast `Archetype` to `IMagicEffectArchetype`). In-spec MGEFs land in
      `formKeyByEd`, so the EXISTING `WireEffects` resolves a spell/potion `effect.magicEffect` to them
      with zero pipeline changes. validate: Reg() + a generic `CheckEnum<TEnum>` on every enum string
      + `CheckRef` on association. dump: prints `mgef: archetype/av/skill/resist/cast/target/cost/flags
      (+assoc)`.
    - **Example:** `examples/mgef_spec.json` → `ModForgeMagic.esp`: two MGEFs — `MF_RestoreHealthEffect`
      (ValueModifier/Health/Restoration/Self/Recover) and `MF_FireDamageEffect` (ValueModifier/Health/
      Destruction/Aimed/ResistFire/Hostile,Detrimental) — referenced by spells `MF_HealSelf` + `MF_Firebolt`
      and potion `MF_HealthDraught` (the heal effect is REUSED by both a spell and the potion). Built +
      verified via dump (5 records, 3 in-spec effect links), validate clean, negative test catches bad
      archetype/AV/flag/association (4/6). Regression: sample_spec (vanilla effect refs) unchanged.
      Packaged → `~/skyrim_mods/ModForgeMagic.zip`.
    - **NEXT (needs the tester):** `help "ModForge" 0` to get FormIDs, then `player.addspell <MF_HealSelf>`
      → cast → Health refills (the bulletproof check); `player.additem <MF_HealthDraught> 1` → drink →
      Health restores. KNOWN LIMITATION: a custom `ValueModifier` MGEF applies its value but has NO
      visual art/projectile — so `MF_Firebolt` (Aimed) has no visible bolt and may not "travel" without
      a projectile + casting/hit art (ART/PROJ records — a deeper rabbit hole, future work). Self/Touch
      + potions work fully. (Race/Class still untouched; COBJ crafting done in It.13.)
- [ ] **It.13 — crafting recipes (ConstructibleObject / COBJ) (built, NOT YET in-game tested) (2026-05-27).**
      Makes generated items craftable at a workbench. Turned out SIMPLER than NOTES feared — the
      workbench is a plain `WorkbenchKeyword` FormLink, NOT a CTDA condition; components live in
      `Items` (the same `ContainerEntry`/`ContainerItem` type the container support already uses);
      `Conditions` (perk/skill gating) is optional and a basic recipe needs none.
    - **Spec:** new `recipes[]` (RecipeSpec): `editorId`, `createdObject` (*ref* — the produced item),
      `count` (CreatedObjectCount, default 1), `workbench` (keyword *ref*; empty → forge 0x088105),
      `components[]` (RecipeComponentSpec: item *ref* + count, consumed on craft).
    - **Build:** pass-1 `mod.ConstructibleObjects.AddNew()` (editorId + count, registers it); pass-2
      wires createdObject + workbench (defaulting to forge) + components via the existing `Resolve` +
      `ContainerEntry` idiom. validate: Reg() + empty-createdObject + no-components + every ref checked.
      dump: prints `recipe: makes Nx <obj> at <bench>` + each `component -> <ref> xN`.
    - **Example:** `examples/recipe_spec.json` → `ModForgeRecipe.esp`: an in-spec weapon `MF_ForgedBlade`
      (IronSword template for model, damage 12) + recipe `MF_ForgedBladeRecipe` making it at the forge
      (workbench omitted → defaulted) from 3× IngotIron (0x05ACE4) + 1× LeatherStrips (0x0800E4). Built
      + round-tripped via dump (recipe → forge 088105, 2 components; 4 cross-ref links, 1 in-spec).
      validate clean; negative test catches empty createdObject / no components / bad component ref (3/3).
      Packaged → `~/skyrim_mods/ModForgeRecipe.zip`.
    - **NEXT (needs the tester):** stand at any forge → the "ModForge Forged Blade" recipe should appear
      when you have ≥3 iron ingots + 1 leather strip → craft it → blade in inventory (equips fine, has
      the IronSword model from It.8 templating). FUTURE: optional `conditions` (perk gating, e.g. require
      Steel Smithing) + temper recipes (armor table / sharpening wheel benches). (Race/Class is the last
      big gap left.)

## Build / test
```
cd /home/lorkhan/repo/ModForge
export PATH="$PATH"   # .NET 8/10 already on PATH
dotnet build src/ModForge.Cli/ModForge.Cli.csproj -v q
# run (no rebuild): dotnet run --project src/ModForge.Cli --no-build -- <cmd> ...
dotnet run --project src/ModForge.Cli --no-build -- build examples/sample_spec.json /tmp/mf-test/Built.esp
dotnet run --project src/ModForge.Cli --no-build -- extract /tmp/mf-test/Built.esp /tmp/mf-test/built.json
```
Mutagen 0.53.1 gotchas: `AddNew()` needs `using Mutagen.Bethesda;`; write with
`BinaryWriteParameters { ModKey = ModKeyOption.NoCheck }` when out-filename ≠ ModKey;
`DialogBranch.CategoryType` = {Player, Command}; API discovery via
`ilspycmd -t <Type> ~/.nuget/packages/mutagen.bethesda.*/0.53.1/lib/net9.0/*.dll`.
