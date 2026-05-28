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
- [x] **It.11 — new interior cell: lighting + floor — IN-GAME CONFIRMED (lighting fixed) (2026-05-27).**
      Tester: `coc MF_TestChamber` → floor + Dibella statue + chest all present and walkable, and
      crucially "至少不是全黑的" (NOT pitch-black) — so the `template`/CopyCellEnv lighting copy fixed
      the original black-cell problem. BUT the lighting looks flat/"荒诞" — uniform ambient, no
      directional light or shadows (the copied inline Lighting + LightingTemplate give ambient fill,
      and the placed `DefaultSunlightHalfOmni01` evidently isn't reading as a real light here). COSMETIC
      follow-up (the cell is usable): try a proper omni/shadow light base, or tune the copied Lighting's
      ambient/directional. Core It.11 goal (lit enough to use + floor) MET; polish deferred.
    - **LIGHTING FIX (2026-05-27, rebuilt — NOT yet re-tested):** root cause found with a new `lightdiag`
      command — the placed `DefaultSunlightHalfOmni01` is **radius 256 + `PortalStrict`**: a PortalStrict
      light only lights inside a ROOM PORTAL, and our cell has no room markers, so it illuminated almost
      nothing → "feels unlit." Swapped it for `WRShadowOmni 0x0C82AE` (omnidirectional shadow-casting,
      warm white, radius 512, on-by-default, NOT PortalStrict — matches the Whiterun stone floor) as the
      key light + two non-shadow warm fills `WRInteriorLightBrite01 0x06ED46` at ±220. Repackaged
      `~/skyrim_mods/ModForgeNewCell.zip` (now 14 refs: 9 floor + 3 lights + statue + chest). The
      open-floor-in-a-void shape still has no walls/ceiling, so a fully "room"-like look would need an
      enclosure (a separate step); this fix targets the actual light source. NEW CLI: `lightdiag`.
    - **RE-TEST PASSED (2026-05-27): tester confirms the lighting is fixed.** The `WRShadowOmni` swap
      (dropping the PortalStrict/radius-256 sunlight) resolved the "feels unlit/flat" look. It.11 DONE.
      OPTIONAL future polish: the cell is still an open floor platform (no walls/ceiling) — enclosing it
      with a wall kit (WRInt* pieces, e.g. WRIntWallStr01Low 0x0CB43B) would make it read as a true room,
      but that's a visual-iteration task best done with the tester watching, not a blind autonomous one.
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
- [x] **It.12 — custom MagicEffect (MGEF) authoring — IN-GAME CONFIRMED after a flag fix (2026-05-27).**
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
    - **IN-GAME (2026-05-27, first try): FAILED — `MF_HealSelf` cast but didn't heal + magicka cost
      was absurd.** Root-caused with a new `mgefdiag` diagnostic (prints an MGEF's functional fields
      from any plugin, master or generated) by diffing my effect vs vanilla `AlchRestoreHealth 0x03EB15`:
      (1) **the `Recover` flag was the killer** — `Recover` reverts the modified value when the effect
      *ends*; on an instant effect (duration 0) it ends immediately, so the +100 heal was applied then
      instantly undone → net zero. Vanilla restore uses `NoDuration, NoArea` and NO Recover. (2) `baseCost`
      8 (vs vanilla 0.5) × magnitude 100 under autocalc = the absurd cost. The bug was in the EXAMPLE's
      authored flags, not the build code — but it's the #1 MGEF gotcha, so it's now documented in SPEC.md.
    - **FIX (example + docs, rebuilt — NOT yet re-tested):** redesigned `mgef_spec.json` to teach the
      flag/timing rule: `MF_RestoreHealthEffect` = instant, `["NoDuration","NoArea"]`, baseCost 0.5 (now
      matches vanilla via mgefdiag) used by spell `MF_HealSelf` + potion `MF_HealthDraught`; NEW
      `MF_FortifyHealthEffect` = timed (+50 Health/60s), `["Recover","NoArea"]` — the CORRECT use of
      Recover — used by spell `MF_FortifyHealth`. Dropped the Aimed `MF_Firebolt` (a no-projectile Aimed
      spell is a poor demo). SPEC.md now spells out instant→NoDuration/no-Recover vs timed→Recover, and
      "keep baseCost low (autocalc)". `mgefdiag` promoted to a documented CLI command (README + Usage).
    - **RE-TEST (needs the tester):** broad `help "ModForge" 0` to list ALL records + FormIDs (the
      earlier `help "ModForge Health Draught" 0` found nothing — likely a query quirk, the potion IS in
      the file with that name per dump; confirm via the broad list). Then **take damage first**, then
      `player.addspell <MF_HealSelf id>` → cast → Health refills; `player.addspell <MF_FortifyHealth id>`
      → cast → max Health +50 for 60s then reverts; `player.additem <MF_HealthDraught id> 1` → drink → heals.
      STILL FUTURE: Aimed damage spells need a projectile + casting/hit art (ART/PROJ). (Race/Class
      untouched; COBJ done in It.13.)
    - **RE-TEST PASSED (2026-05-27): tester confirms the spells AND the potion are all present/working.**
      Custom MGEF authoring is in-game-verified end-to-end. The `Recover`-on-instant trap is the key
      lesson (now in SPEC.md). Both big gaps this session — MGEF (It.12) + COBJ (It.13) — are confirmed.
- [x] **It.13 — crafting recipes (ConstructibleObject / COBJ) — IN-GAME CONFIRMED (2026-05-27).**
      Tester: at a forge with 3× iron ingot + 1× leather strip, "ModForge Forged Blade" appeared,
      crafted it, the blade equips and swings normally. COBJ crafting works end-to-end.
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
- [x] **It.14 — spell projectiles + visual art — IN-GAME CONFIRMED (2026-05-27).**
      Tester: `MF_Firebolt` now launches a visible fire bolt from the hand that travels + impacts and
      deals fire damage. Custom magic is complete end-to-end: effect → spell/potion → projectile + FX.
      Completes It.12: a custom Aimed damage spell had no visible bolt (a ValueModifier MGEF applies
      its value but carries no visuals). Added four optional ref fields to MagicEffectSpec —
      `projectile` (PROJ, the traveling bolt), `castingArt` (ARTO, FX at the hands), `hitEffectArt`
      (ARTO, FX at impact), `explosion` (EXPL, AoE) — wired in pass 2 alongside `association` (Resolve
      skips empty, so only authored refs are set). validate CheckRef's each. Harvested the real vanilla
      values with `mgefdiag`: `FireDamageFFAimed75 0x10F7F1` → projectile `0x10FBEA` + castingArt
      `0x01B211` (the projectile carries its own impact visuals, so hitEffectArt/explosion optional).
    - **Example:** re-added `MF_FireDamageEffect` (ValueModifier/Health/Destruction/Aimed/ResistFire,
      flags Hostile,Detrimental,NoDuration,NoArea, baseCost 1.5, **projectile 0x10FBEA + castingArt
      0x01B211**) + spell `MF_Firebolt` (Aimed, magnitude 25) to `mgef_spec.json`. Built + verified via
      mgefdiag (Projectile + CastingArt now set on the effect). validate clean; negative test catches a
      malformed/unresolved projectile/castingArt ref. Repackaged `~/skyrim_mods/ModForgeMagic.zip` (now
      7 records: 3 MGEF + 3 spells + 1 potion).
    - **RE-TEST (needs the tester):** `help "ModForge Firebolt" 0` → `player.addspell <id>` → aim at an
      enemy/wall and cast → should see a fire bolt LEAVE the hand, travel, and impact (vs nothing
      before), dealing ~25 fire damage. (Race/Class is the last big gap.)
- [x] **It.15 — Class (CLAS) authoring + npc level/autoCalcStats — IN-GAME CONFIRMED (2026-05-27).**
      Tester A/B: the battlemage NPC (class M60) reads high magicka/low health, the warrior NPC (class
      H60) the opposite — the class drives an auto-calc NPC's attribute distribution. (Needed the
      It.15-fix `level` + `autoCalcStats`; a bare NPC ignores class and reads flat 50/50/50.)
      Did the safe half of the last big gap (Class); skipped custom Race (deep asset rabbit hole —
      bodies/skeleton/voice + heavy in-game iteration). A `classes[]` entry = an npc "profession" an
      npc's `class` ref can point at. CLAS has NO FormLinks (all enums + weight dicts), so it's built
      fully in pass 1.
    - **Spec/build:** ClassSpec → `name`/`description`, `teaches` (Skill enum, trainer NPCs),
      `maxTrainingLevel`, `healthWeight`/`magickaWeight`/`staminaWeight` (BasicStat distribution dict),
      `skillWeights` (`{Skill: 0–255}`). `mod.Classes.AddNew()`; enums via `Enum.TryParse<Skill>` /
      `<BasicStat>`; all-zero stat weights default to balanced 1/1/1 (avoid a degenerate distribution);
      unknown skill keys warn + skip. validate: Reg() + `CheckEnum<Skill>` on `teaches` + every
      skillWeight key. dump: `class: teaches/maxTrain/stats[…]/skills[…]`.
    - **Example:** `examples/class_spec.json` → `ModForgeClass.esp`: class `MF_Battlemage` (teaches
      Destruction, H30/M50/S20, skills Destruction100/Restoration75/Alteration40/OneHanded50/HeavyArmor25)
      + npc `MF_BattlemageNpc` (race NordRace 0x013746 + class MF_Battlemage). Built + dump verified
      (weights correct, npc→class link resolves in-spec); validate clean; negative test catches bad
      `teaches`/skill key. Packaged → `~/skyrim_mods/ModForgeClass.zip`.
    - **IN-GAME (2026-05-27): tester spawned the NPC, `getav health/magicka/stamina` = 50/50/50 — class
      had NO effect.** Correct + expected: a class only drives an actor's stats when the NPC
      **auto-calculates from a level**; a bare NPC (no level, no auto-calc) uses flat 50/50/50 and never
      consults the class weights. The class record itself was fine (dump).
    - **FIX (2026-05-27, rebuilt — re-test pending):** added `level` (int) + `autoCalcStats` (bool) to
      NpcSpec → `Configuration.Level = new NpcLevel{Level}` + `Configuration.Flags |=
      NpcConfiguration.Flag.AutoCalcStats`. Reworked `class_spec.json` into an A/B test: `MF_BattlemageNpc`
      (class M60/H25/S15) vs `MF_WarriorNpc` (class H60/S35/M5), BOTH NordRace, level 25, autoCalcStats.
      dump now prints `level=/autoCalcStats=`. Repackaged `~/skyrim_mods/ModForgeClass.zip` (4 records).
    - **RE-TEST (needs the tester):** `help "ModForge" 0` → `player.placeatme` BOTH NPCs (pick the `NPC_`
      entries) → click each → `getav magicka` / `getav health`: the **battlemage should read high magicka /
      low health, the warrior the opposite** (vs the old flat 50/50/50). That contrast = the class driving
      stats. Custom Race remains the one untouched big gap.
- [x] **It.16a — AI Package (PACK) authoring — Sandbox template — IN-GAME CONFIRMED (2026-05-28).**
      Tester: `coc WhiterunBanneredMare` → `MF_InnPatronNpc` ("ModForge Patron"); ~1 minute after
      cell load (sandbox AI cold-start delay) **he walked to a chair and sat down**. Sandbox is alive:
      `AllowWandering` finds the path, `AllowSitting` snaps him to inn furniture. First "lifelike NPC"
      brick complete — generated NPCs can now decide what to do, not just stand. ESP-side authoring
      (Mutagen+PACK), zero SKSE-plugin C++; matches the AI Overhaul / Immersive Citizens approach.
    - **API discovery (Mutagen 0.53.1):** Skyrim PACK is **template-driven** — every concrete
      Package references a vanilla "procedure template" form via `PackageTemplate` (a `IFormLink<
      IPackageGetter>`), and `Data` is a `IDictionary<sbyte, APackageData>` keyed by the template's
      named slot indices. Templates have `Type = PackageTemplate`; concrete packages `Type = Package`.
      Sandbox = `Skyrim.esm:0x01C254` (EditorID "Sandbox"). All vanilla `Default*Sandbox*` packages
      reference it. Slot schema discovered via the new `packagediag` command on 0x01C254 (28 named
      slots, 12 used by concrete sandboxes): 0=Location(LocationTargetRadius), 1=AllowEating(bool),
      3=AllowSleeping, 4=AllowConversation, 5=AllowIdleMarkers, 6=AllowSitting, 7=AllowWandering,
      14=UnlockOnArrival, 25=PreferredPathOnly, 27=RideHorseIfPossible, 29=Energy(float), 31=AllowSpecialFurniture.
    - **NEW CLI `packagediag <esp> <0xFORMID>`** (in It.12 mgefdiag style): dumps Type/PackageTemplate/
      Flags/InterruptFlags/Speed/Schedule/DataInputVersion/Unknown/XnamMarker + each Data entry's
      sbyte key, concrete subtype name (PackageDataLocation/Bool/Float/Int/Target/Topic/ObjectList)
      and its key field — for LocationFallback also `Type` (LocationTargetRadius.LocationType enum)
      + `Data`. Both LocationFallback traps below were debugged with this command.
    - **Spec/Build:** new `packages[]` (PackageSpec) — `template` (ref, required), `flags`/
      `interruptFlags` (string arrays via ParseFlags<T>), `preferredSpeed`/`combatStyle`/`ownerQuest`,
      a `schedule` subobject, and a `sandbox` subobject (template-input UX). NpcSpec gained
      `packages` (refs → wired into `npc.Packages`) AND `voiceType` (ref → `npc.Voice`; without one,
      NPC is silent — no hello/idle chatter audio or subtitle). Pass-1: `mod.Packages.AddNew()` +
      scalars (Type forced to Package, flags, schedule). Pass-2: resolves template/combatStyle/
      ownerQuest refs + (only when template is the Sandbox FK) fills `Data[0..31]` with
      PackageDataLocation/Bool/Float matching `DefaultSandboxCurrentLocation256`'s shape, overriding
      from SandboxSpec. Non-Sandbox templates emit a structurally-valid package with no Data
      (template defaults apply) + a warning — Travel/UseItemAt/Find/EatSleep go in It.16b.
    - **TWO LocationFallback TRAPS (both verified in-game, each cost a debug cycle):**
      1. **Wrote as LocationTarget, not LocationFallback.** `new LocationFallback()` with `Type` left
         at 0 silently writes as `LocationTarget` in the binary — Mutagen's writer picks the
         ALocationTarget binary shape from `LocationFallback.Type` (a `LocationTargetRadius.LocationType`
         enum), NOT from the C# class identity. Fix: explicitly set `Type`.
      2. **Wrong fallback type → sandbox finds no anchor.** First fix used `NearEditorLocation`
         (what `DefaultSandboxEditorLocation256` uses, hence the name). **In-game failure mode:**
         the NPC stood still doing nothing for an unbounded time, even with all data slots correct
         and full InterruptFlags (he'd still greet on approach because that's a separate flag, but
         no wandering / sitting / chatter). Root cause: `NearEditorLocation` requires the NPC to have
         an "Editor Location" field set in CK — vanilla CK-edited actors get one, but Mutagen-
         generated `Npc` records don't, so sandbox finds no anchor and silently no-ops. Fix: use
         `LocationTargetRadius.LocationType.NearSelf` (what `DefaultSandboxCurrentLocation256` and
         `WE18WitchSandboxNearSelf` use) — anchors at the actor's current position, no external link.
         Verified by `packagediag` byte-diff vs vanilla NearSelf sandboxes after the fix.
    - **validate/dump:** validate Reg() registers package editorIds; checks template (required +
      well-formed external ref), combatStyle/ownerQuest/sandbox.location/voiceType refs, Flag/
      InterruptFlag/Speed/DayOfWeek enum names; NpcSpec.packages refs. dump: prints `package: type/
      template/flags/interrupt/speed/schedule/data N slot(s)` + on each NPC `package -> <ref>` +
      `voice -> <ref>` lines.
    - **Example evolution (3 spec rewrites driven by in-game findings):**
      1. Wilderness placement at Tamriel grid (-23,4) (proven It.9 coords). Validated structurally
         but **no in-game sandbox visible** — the grid is empty wilderness; sandbox needs furniture/
         idle markers/other NPCs nearby to do anything observable. Lesson: structural pass-through
         isn't the same as a meaningful test arena.
      2. Added `voiceType: Skyrim.esm:0x013AE6` (MaleNord). In-game: NPC now greets on approach
         ("嗯/啊" generic male responses) — voice chain works — but without faction membership only
         the most generic filler audio plays. Idle chatter requires faction-conditioned dialogue
         topics; deferred as a follow-up (NpcSpec already takes `factions`, just not wired in the
         example).
      3. Relocated to Bannered Mare interior (`Skyrim.esm:0x01605E`, position 0,0,0; the It.10
         vanilla-interior placement path). After the NearSelf fix above: tester reports NPC walks to
         a chair and sits after ~1 minute. Final in-game-confirmed example.
    - **Tester gotcha (worth flagging next iteration):** Skyrim sandbox has a **~30-90s cold start**
      after cell load before the first decision tick fires. If you `coc` in and watch for 10s seeing
      no movement, that's normal — wait the full minute. Vanilla NPCs in the same cell hide this
      because they were initialized when the cell was first generated, well before the player arrived.
    - **Cosmetic byte-diff still open (NOT in-game blocking, deferred):** `packagediag` shows three
      fields differ from vanilla (verified harmless via the in-game confirm above):
      `DataInputVersion` (vanilla=10, ours=0), `Unknown` byte (vanilla=196, ours=0), `XnamMarker`
      length (vanilla=1, ours=0). Cargo-culting these to match vanilla is a 3-line follow-up; flag
      it the moment any future test depends on them.
    - **What this UNBLOCKS:** It.16b — more procedure templates (Travel `Skyrim.esm:0x01C266`,
      UseItemAt, Find, EatSleep), each one a `Data[...]` slot-fill helper analogous to the Sandbox
      one. The Sandbox path is the proof; the others reuse the same plumbing. It.17 = CombatStyle
      (CSTY) — for "NPC uses my mod spells" + "rushes to block". The "idle chatter via faction"
      cleanup is a minor task — extend the example to add a citizen faction once we know which one
      gives the chattiest dialogue without contaminating the faction's behaviour.
- [x] **It.16b — Travel template + Sandbox-at-ref demo — IN-GAME CONFIRMED (2026-05-28).** Tester:
      `coc RiverwoodSleepingGiantInn` → `MF_TravelerNpc` walks across the inn floor to the
      RiverwoodInnCenterMarker, then **transitions into Sandbox** behaviour around it. Both halves
      of the chain confirmed: (a) Travel actually relocates the actor when the target is reachable
      from spawn on continuous navmesh, (b) multi-package list ordering works — the engine evaluates
      packages in spec order, runs Travel until "arrived within radius", then falls through to the
      next package (Sandbox) as the arrival behaviour. Generated PACK records are functionally
      equivalent to vanilla CK-authored ones.
    - **First in-game attempt failed (instructive):** placed NPC inside Bannered Mare with Travel
      destination = `debugWhiterunOrigin` (WhiterunWorld exterior marker, outside the city walls).
      NPC sandboxed inside the inn — **Travel was silently rejected** and the engine fell through
      to the second package. Cross-worldspace travel (Bannered Mare interior cell → WhiterunWorld
      exterior, two cell transitions through door teleports) needs more than a bare Travel package;
      vanilla NPCs that do this have additional setup (faction trespass rules, "starting context"
      links, sometimes a quest alias to anchor them). Lesson: **for the demo to verify Travel
      itself, keep both endpoints on continuous navmesh** (same cell or same worldspace, no door
      transitions). The fallback-to-Sandbox behaviour IS still a useful structural confirmation
      that multi-package lists evaluate in order — but it's not a positive Travel test.
    - **Second example (this one): both endpoints in Sleeping Giant Inn.** Spawned NPC at cell-
      local (0,0,0) (entrance area), Travel destination = RiverwoodInnCenterMarker (the inn's
      centre XMarker, `Skyrim.esm:0x01DC0A`), Sandbox.location = same marker, radius 384. Same
      cell, navmesh continuous. Tester confirms: walks across inn → sandboxes at centre.
    - Originally scoped Travel + UseItemAt + Find; Originally scoped Travel + UseItemAt + Find; collapsed to **Travel only** after
      discovering that **vanilla has no `UseItemAt` named template** (Sandbox + a `location` ref to a
      furniture REFR + `allowSpecialFurniture: true` is the "go to specific furniture" pattern; no new
      code needed — already supported in It.16a) and Find has no clear template-form match either.
      Travel = `Skyrim.esm:0x016FAA` (NOT the `0x01C266` written in the It.16a NOTES — that was a
      guess, the real ID was found by `packagediag`-ing concrete vanilla Travel packages and reading
      their `PackageTemplate` ref).
    - **Template inventory** (via packagediag on candidate FormIDs):
      - Sandbox  `Skyrim.esm:0x01C254` — 12 slots (It.16a, done)
      - **Travel `Skyrim.esm:0x016FAA` — 3 slots** (this iteration)
      - Patrol  `Skyrim.esm:0x017723` — 6 slots, uses PackageDataTarget for a LinkedReference chain
        of idle markers (more complex; deferred)
      - UseMagic `Skyrim.esm:0x0504F5` — 13 slots, scheduled non-combat spell casting (interesting
        for "NPC casts buff at altar"; deferred to It.16c or later)
      - UseWeapon `Skyrim.esm:0x01C338`, Follow `0x019B2C`, Escort `0x023B73` — discovered, deferred
    - **Travel slot schema:** 0=Place to Travel (PackageDataLocation), 2=Ride Horse if possible?
      (bool, default false), 4=Prefer Preferred Path? (bool, default false). Simpler than Sandbox.
    - **Refactor:** Build pass-2 PACK loop now **dispatches by template FormID** — central
      `MakeLocationSlot(name, owner, refStr, radius)` helper returns either LocationTarget(Link=fk)
      or LocationFallback(NearSelf) (reused by both Sandbox slot 0 and Travel slot 0; consolidates
      the two-trap fix from It.16a). Template ID switch: SandboxTemplateId → SandboxSpec fill,
      TravelTemplateId → TravelSpec fill, else warn ("template not yet supported; no Data overrides").
      Unknown templates still emit a structurally valid package (template defaults apply).
    - **Spec:** `PackageSpec` gained `travel` (TravelSpec) sibling of `sandbox`. TravelSpec is just
      `place` (ref, required for actual movement) / `radius` (default 0 = exact arrival) / `rideHorse`
      / `preferPath`. validate's `CheckRef` covers `travel.place`. dump unchanged (`data=N slot(s)`
      already shows whichever variant filled).
    - **Example `examples/package2_spec.json`** → `ModForgePackage2.esp` (4 records → 6 with cell
      override + placement): one NPC with **two packages**: `MF_TravelToWhiterunPackage` (Travel,
      destination = `debugWhiterunOrigin` Skyrim.esm:0x0567F7 — the `coc whiterun` marker, a known
      stable XMARKER REFR) + `MF_SandboxAtWhiterunPackage` (Sandbox with `location` =
      **same ref** — anchored sandbox at the Travel destination). The NPC is placed inside Bannered
      Mare; engine evaluates packages in list order, so Travel runs first and Sandbox is the
      arrival behaviour. dump: 2 packages on the NPC, Travel package has 3 data slots, Sandbox has
      12. packagediag confirms slot 0 of both = LocationTarget(0567F7:Skyrim.esm). Packaged →
      `~/skyrim_mods/ModForgePackage2.zip`.
    - **Known limitation surfaced (worth flagging next iteration):** the Mutagen-generated Travel
      package CAN be silently rejected when it requires the actor to traverse cell boundaries through
      a door teleport (especially interior→exterior+different-worldspace, e.g. Bannered Mare → outside
      Whiterun). Vanilla NPCs that do this have extra plumbing CK sets up (faction permissions,
      "starting context" linked refs, sometimes a quest alias). Within a single cell — or within a
      worldspace with continuous navmesh — Travel works as confirmed above. Cross-boundary travel is
      a CONTENT problem, not a Mutagen / PACK-records problem.
    - **What this UNBLOCKS:** It.16c — Patrol (LinkedReference idle-marker chains, for "guards walk
      this route") + UseMagic (scheduled spell casting, for "altar buff" scenarios). Same Build
      dispatch pattern; each is a fresh SubSpec + a `case TemplateId:` branch.
- [x] **It.16c — cross-cell Travel ("let NPC walk OUT of the inn") — IN-GAME CONFIRMED (2026-05-28).**
      Tester: `coc WhiterunBanneredMare` → `MF_CrossCellTravelerNpc` walked out of the inn, through
      Whiterun's city streets, all the way to the gate area near the Whiterun-origin marker. THIS is
      the real "lifelike NPC daily life" unlock — generated NPCs can now genuinely participate in
      cross-cell traversal, not just stay within one cell.
    - **Why It.16b's first attempt failed (the right diagnosis, after the fact):** the It.16b NPC
      was rejected at the door teleport because she was **not a citizen of Whiterun** — engine treats
      her as having no traversal rights through city gates. Sandbox kicking in as a fallback hid the
      Travel rejection completely silently. The Mutagen-generated NPC was structurally identical to
      vanilla packages but missing the "I belong here" identity that vanilla actors get from CK.
    - **Diagnosis: npcdiag-style diff of a known cross-cell vanilla NPC (Ysolda, 0x013BAB) vs
      MF_InnPatronNpc.** New `npcdiag <esp> <0xFORMID>` CLI command (in the It.12 mgefdiag pattern):
      dumps an Npc's race/class/voice/outfits + factions/CrimeFaction + Template/DefaultPackageList +
      ObserveDead/GuardWarn/CombatOverride package lists + Configuration.Flags + MajorFlags +
      Packages/Keywords/ActorEffect/Perks. Ran on Ysolda + our NPC side by side. Ysolda has:
        - `CrimeFaction = 0267EA:Skyrim.esm` (CrimeFactionWhiterun) — the city's "citizen recognition"
          faction. Ours: empty.
        - Faction membership in CrimeFactionWhiterun + TownWhiterunFaction (028172). Ours: 0 factions.
        - `Configuration.Flags = Female, AutoCalcStats, Unique, LoopedScript, LoopedAudio`. Ours: just
          AutoCalcStats. The interesting bit was **`Unique`** — marks the actor as a one-off (vs a
          leveled-list template instance).
      All four of Ysolda's packages packagediag'd identical-shape to ours (no OwnerQuest, no
      Conditions, no special Flags). So the difference is in the NPC base record, NOT the packages.
    - **Spec / Build:** NpcSpec gained `crimeFaction` (ref → FACT) and `unique` (bool —
      Configuration.Flag.Unique). Pass-2 resolves the crimeFaction ref onto `npc.CrimeFaction`; pass-1
      OR's in the Unique flag when set. validate: CheckRef on crimeFaction. dump: prints
      `crimeFaction -> <ref>` on each NPC when set.
    - **Example `examples/package3_spec.json`** → `ModForgePackage3.esp`. Same shape as It.16b's
      failed first attempt (NPC spawned in Bannered Mare, Travel to `debugWhiterunOrigin` outside
      city walls + Sandbox-at-marker fallback), PLUS the three It.16c additions: `crimeFaction:
      Skyrim.esm:0x0267EA`, `factions: [CrimeFactionWhiterun, TownWhiterunFaction]`, `unique: true`.
      Build + dump verify: CrimeFaction wired, both factions present rank 0, Configuration.Flags
      shows `AutoCalcStats, Unique`. Packaged → `~/skyrim_mods/ModForgePackage3.zip`.
    - **A/B GAP (honest open follow-up):** we changed THREE things at once (CrimeFaction +
      TownWhiterunFaction membership + Unique). The in-game success doesn't tell us which is
      individually load-bearing. Hypothesis: CrimeFaction is the primary one (city-citizen
      identity); TownWhiterunFaction is reinforcing; Unique helps engine track the actor's AI
      state across cell transitions. Future A/B: build 3 variants, each missing one of the three,
      see which fails. Not blocking — the "all three" recipe works.
    - **Texture/LOD glitch at gate (NOT our ESP):** tester reported visual seams + flickering LOD
      around the Whiterun gate area. **Verified false alarm.** `dump ModForgePackage3.esp` shows we
      ONLY override Bannered Mare interior cell (0x01605E); we touch zero exterior cells, zero
      worldspaces, zero landscape/LOD data. Cause is the well-known Skyrim `coc`-into-interior +
      walk-out-without-load-screen quirk — exterior LOD doesn't preload. Tester confirmed by fast-
      travelling away and back: LOD fully reloaded, glitch gone. Worth flagging in future testing:
      use `coc <exteriorMarker>` (which does run a normal load) rather than `coc <interiorCell>` +
      walk-out, OR fast-travel in/out once after the cold-start sandbox period.
    - **The "lifelike NPC" minimum recipe is now complete:**
      ```jsonc
      { "editorId": "MF_LifelikeNpc",
        "race": "Skyrim.esm:0x013746",
        "class": "<some class>",
        "voiceType": "Skyrim.esm:0x013AE6",      // hello/idle audio
        "crimeFaction": "Skyrim.esm:0x0267EA",   // city citizen — needed for cross-cell travel
        "factions": [ "Skyrim.esm:0x0267EA",
                       "Skyrim.esm:0x028172" ],  // town faction reinforces
        "unique": true,                           // one-off NPC, not leveled
        "level": 5, "autoCalcStats": true,
        "packages": [ "<Travel>", "<Sandbox>" ] }
      ```
      Add this `crimeFaction` + `unique` + faction memberships and any generated NPC can have a
      proper daily life that includes leaving the building.
    - **What this UNBLOCKS:** the original user goal ("讓 NPC 出門") is met. Daily schedule with
      multiple Travel + Sandbox packages on time-of-day is now structurally + semantically possible.
      Next: It.17 (CombatStyle — "rush forward to block / use my mod spells") or It.16d (Patrol +
      UseMagic templates, completing the PACK side).
- [x] **It.16d — cross-WORLDSPACE Travel: out the city to the wilderness — IN-GAME CONFIRMED (2026-05-28).**
      Follow-up to It.16c. The It.16c test reached an INSIDE-Whiterun marker (debugWhiterunOrigin
      at marketplace, near but inside the main gate) — that's ONE worldspace transition (Bannered Mare
      interior → WhiterunWorld exterior). It.16d's test reached an OUTSIDE-Whiterun marker
      (WhiterunStablesHorseMarker at the stables in Tamriel) — that's TWO worldspace transitions
      (Bannered Mare interior → WhiterunWorld exterior → Tamriel exterior, through both the inn door
      AND the main city gate). Tester confirmed: `player.moveto <NPC>` lands at the stables → the
      same three-piece "citizenship" recipe scales to as many cell/worldspace transitions as the
      route demands.
    - **Example `examples/package4_spec.json`** → `ModForgePackage4.esp` → `~/skyrim_mods/ModForgePackage4.zip`.
      Differs from It.16c only in the Travel + Sandbox destination ref: `Skyrim.esm:0x109826`
      (WhiterunStablesHorseMarker) instead of `0x0567F7` (debugWhiterunOrigin). Same NPC recipe
      (crimeFaction CrimeFactionWhiterun, factions [CrimeFactionWhiterun, TownWhiterunFaction],
      unique=true). No code changes needed — the iteration validated It.16c's recipe at greater scope.
    - **Practical implication:** the "lifelike NPC" recipe in It.16c is the WHOLE recipe. Once an
      NPC has citizen identity (CrimeFaction + town faction membership) and Unique, the engine
      treats any Travel destination — be it across one door teleport or several — as legitimate.
      The recipe handles full day-and-night cycles (sleep at inn, work at farm, travel to market)
      with multiple Travel + Sandbox packages on a time-of-day schedule.
    - **A/B GAP still open** (same as It.16c): we still haven't isolated which of CrimeFaction /
      faction-membership / Unique is THE necessary one — likely it's CrimeFaction primarily, but
      this remains a small follow-up test, not a blocker.
- [x] **It.17 — CombatStyle (CSTY) + NPC.spells + AIData — IN-GAME CONFIRMED (2026-05-28).**
      Tester: spawn EncWolfIce next to MF_MageNpc → mage stands ground and casts Flames at the wolf.
      Closes the third and last of the user's original three "lifelike NPC" goals (cf. the It.16
      series for goals 1+2): "NPCs use the spells my mod adds." Pure record-authoring, no SKSE/C++.
      **The full lifelike-NPC ESP-side authoring toolkit is now feature-complete in ModForge.**

      KEY INSIGHT discovered the hard way: Skyrim NPC combat decision has **TWO independent
      systems**, and BOTH need to be authored for a generated NPC to actually fight:
        - **CombatStyle (CSTY)** controls "**HOW** the AI fights" — `equipMult*` weights determine
          weapon class preference (magic vs melee vs staff vs ranged).
        - **AIData.Aggression / Confidence** controls "**WHETHER** the NPC fights at all" —
          Mutagen-generated NPCs default to **Aggression=Unaggressive + Confidence=Cowardly** which
          means flee from any threat, regardless of CombatStyle.
      A CSTY-only setup gives you "wants to use magic but flees the moment it sees a wolf" — the
      It.17 round-1 failure mode the tester reported. The fix is `aggression: "Aggressive"` (defends
      when attacked) + `confidence: "Brave"` (doesn't flee a fair fight) on the NPC.
    - **API discovery:** ICombatStyleGetter has 9 main fields — Offensive/Defensive/Group offensive
      multipliers (~aggression/blocking/group boldness) + six `EquipmentScoreMult*` weights
      (Melee/Magic/Ranged/Shout/Unarmed/Staff) + `AvoidThreatChance` + `Flags` (Dueling/Flanking/
      AllowDualWielding) + 3 sub-records (Melee/CloseRange/Flight — left at defaults for now). The
      six EquipMult scores are the AI's combat-path preference; push Magic high for a mage.
    - **NEW CLI `cstydiag <esp> <0xFORMID>`** (mgefdiag-style): dumps a CSTY's Offensive/Defensive/
      Group + the six EquipMults + AvoidThreatChance + Flags. Used to harvest vanilla CSTY values:
      `csVampireMagic` (0x02DFB5) = gold mage profile (Magic=8.1, Staff=2.15, Melee=0.51), aggressive
      OffensiveMult=0.77, AvoidThreatChance=0.2, Dueling flag. `csSoldierMagic` (0x046B9E) = mildly
      magic-preferring (Magic=3 vs others=1). `csForswornMagic` is misleadingly named — all EquipMults
      are 1.0 (balanced, not magic-preferring despite the name).
    - **Spec/Build:** new top-level `combatStyles[]` (CombatStyleSpec) — editorId + 9 floats + flags
      array. All fields are floats / enums (no FormLinks), so fully built in pass 1. NpcSpec gained
      two related fields: `combatStyle` (ref → CSTY; wired in pass-2 alongside race/class/etc.) and
      `spells` (array of refs → SPEL records; populates `npc.ActorEffect` ExtendedList in pass-2 via
      the existing Resolve helper). validate Reg() registers CSTY ids + checks Flag enum names +
      CheckRef on combatStyle/spells. dump prints `cs: …` summary for each CSTY + on each NPC
      `combatStyle -> <ref>` + each `spell -> <ref>`.
    - **Example `examples/combat_spec.json`** → `ModForgeCombat.esp` → `~/skyrim_mods/ModForgeCombat.zip`.
      `MF_MageCombatStyle` lifts csVampireMagic's numeric values exactly (proven vanilla mage profile).
      `MF_MageClass` is magicka-heavy (Magicka 65/Health 20/Stamina 15) with Destruction=100,
      Restoration=60, Alteration=40 skill weights. `MF_MageNpc` (level 25 autoCalc + the It.16c
      citizenship recipe so she's "valid" anywhere) has `combatStyle = MF_MageCombatStyle` and
      `spells = [Skyrim.esm:0x0C969A]` (vanilla `FlamesRightHand`, the basic novice destruction
      cone). Placed at the It.9 wilderness Tamriel coords (proven exterior placement). validate +
      dump verify all wired: spell → 0C969A, combatStyle → MF_MageCombatStyle, all factions present.
    - **Round-1 (CSTY only) IN-GAME FAILED — mage fled from wolf, never cast.** Tester observed
      MF_MageNpc just running away from the spawned wolf. `npcdiag` diff against vanilla bandits
      revealed the AIData defaults problem (above). Fix: add Aggression/Confidence/Assistance/Mood/
      EnergyLevel as direct NpcSpec fields → set them at pass-1 on Configuration.AIData.
    - **Round-2 (CSTY + AIData) IN-GAME PASSED.** With `aggression: "Aggressive"` + `confidence:
      "Brave"` + `assistance: "HelpsFriendsAndAllies"` + `energyLevel: 50`, MF_MageNpc holds his
      ground, casts Flames at the wolf, kills it. Re-tested in Tamriel wilderness near the It.9
      coords. The CombatStyle's Magic=8.1 priority gave him magic as the chosen weapon; AIData
      Aggressive+Brave gave him the will to engage.
    - **The complete "lifelike NPC" recipe (now feature-complete):**
      ```jsonc
      { "race": "Skyrim.esm:0x013746",         "class": "...",
        "voiceType": "Skyrim.esm:0x013AE6",     // hello/idle audio
        "crimeFaction": "Skyrim.esm:0x0267EA",  // citizen identity (cross-cell travel)
        "factions": [ ... ],                     // reinforcing town faction
        "unique": true,                          // engine AI tracking
        "combatStyle": "<MF_MageCS>",           // HOW he fights
        "spells": [ ... ],                       // WHAT he casts
        "aggression": "Aggressive",              // WHETHER he fights (vs. fleeing)
        "confidence":  "Brave",
        "assistance":  "HelpsFriendsAndAllies",
        "energyLevel": 50,
        "level": 25, "autoCalcStats": true,
        "packages": [ "<Travel>", "<Sandbox>" ] }
      ```
      This is the minimum-viable set for a generated NPC to: (a) sandbox/talk/sit/eat (It.16a),
      (b) walk to a specific point (It.16b), (c) cross cells / leave the inn / city (It.16c/d),
      (d) defend itself with mod-added spells in combat (It.17). Future iterations are POLISH
      (Patrol / UseMagic templates, A/B testing the citizenship recipe, custom MGEF→spell→NPC end-
      to-end), not foundational. **ModForge is now a complete lifelike-NPC authoring toolkit.**

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
