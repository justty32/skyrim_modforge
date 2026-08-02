# Mutagen Documentation — Navigation Index

## What this is

A searchable index of the locally-mirrored [Mutagen](https://mutagen-modding.github.io/Mutagen/) documentation (the C# library for reading/writing Bethesda game plugins — Skyrim `.esp`/`.esm`/`.esl`). Mirror downloaded 2026-05-31; 73 pages indexed. Paths are relative to the `reference/` folder (where this index lives).

## Most relevant to ModForge

ModForge generates Skyrim plugins from a spec via Mutagen. These pages matter most:

- [FormLink Nullability](mutagen-modding.github.io/Mutagen/best-practices/FormLink-Nullability/index.html) — null vs set FormLinks; getting the nullability right when wiring records together in generated output.
- [ModKey, FormKey, FormLink](https://mutagen-modding.github.io/Mutagen/plugins/ModKey,%20FormKey,%20FormLink/index.html) — the three core identifiers every generated record is built from.
- [Create, Duplicate, and Override](mutagen-modding.github.io/Mutagen/plugins/Create,-Duplicate,-and-Override/index.html) — how to construct new records (FormKey required + immutable) and override masters — the core generate flow.
- [New Mods](mutagen-modding.github.io/Mutagen/plugins/New-Mods/index.html) — making a mod object from scratch (no file), the starting point for generated output.
- [Exporting](mutagen-modding.github.io/Mutagen/plugins/Exporting/index.html) — writing the mod object to an esp/esm on disk.
- [ITPO Avoidance](mutagen-modding.github.io/Mutagen/best-practices/ITPO-Avoidance/index.html) — don't emit Identical-to-Previous-Override records; keeps generated plugins clean.
- [Skyrim Link Interfaces](mutagen-modding.github.io/Mutagen/game-specific/skyrim/Skyrim-Link-Interfaces/index.html) — which record types a FormLink can point at (e.g. Container holds Armor/Weapon/Ingredient) — needed to type links correctly.
- [Skyrim Aspect Interfaces](mutagen-modding.github.io/Mutagen/game-specific/skyrim/Skyrim-Aspect-Interfaces/index.html) — common aspects (INamed, etc.) for setting shared fields generically across record types.

Also worth a look: [Winning Override Iteration](mutagen-modding.github.io/Mutagen/loadorder/Winning-Overrides/index.html), [Header Structs](mutagen-modding.github.io/Mutagen/lowlevel/Header-Structs/index.html), and [Translation Masks](mutagen-modding.github.io/Mutagen/plugins/Translation-Masks/index.html) for copy/equality control.

## Top-level

- [Mutagen Documentation](mutagen-modding.github.io/Mutagen/index.html) — landing page; overview of the C# library for analyzing, modifying, and creating Bethesda mods with strongly-typed records.
- [Big Cheat Sheet](mutagen-modding.github.io/Mutagen/Big-Cheat-Sheet/index.html) — a massive list of code snippets with little explanation; quick copy-paste reference.
- [Strings](mutagen-modding.github.io/Mutagen/Strings/index.html) — localized strings: newer Bethesda titles let a string record hold multiple language translations.
- [Json](mutagen-modding.github.io/Mutagen/Json/index.html) — JSON support (targets Newtonsoft.Json; System.Text.Json possible on demand).
- [Archives (BSAs)](mutagen-modding.github.io/Mutagen/Archives/index.html) — reading/writing Bethesda asset archives (.bsa/.ba2) holding textures, meshes, etc.
- [Correctness](mutagen-modding.github.io/Mutagen/Correctness/index.html) — how Mutagen verifies correctness via unit tests and a passthrough test suite.
- [Contributing](mutagen-modding.github.io/Mutagen/Contributing/index.html) — how to contribute to and improve the documentation itself.

## Best Practices

- [Accessing Known Records](mutagen-modding.github.io/Mutagen/best-practices/Accessing-Known-Records/index.html) — looking up specific well-known records from base masters like Skyrim.esm.
- [Enriching Exceptions](mutagen-modding.github.io/Mutagen/best-practices/Enrich-Exceptions/index.html) — adding context to Mutagen's intentionally lightweight exceptions for better diagnostics.
- [Enumerable Laziness](mutagen-modding.github.io/Mutagen/best-practices/Enumerable-Laziness/index.html) — a common LINQ/IEnumerable pitfall (repeated/deferred evaluation) and how to avoid it.
- [FormLink Nullability](mutagen-modding.github.io/Mutagen/best-practices/FormLink-Nullability/index.html) — FormIDs can be all-zeros (null); handling null vs set FormLinks correctly.
- [FormLinks Target Getter Interfaces](mutagen-modding.github.io/Mutagen/best-practices/FormLinks-Target-Getter-Interfaces/index.html) — FormLinks carry typing info on which record type they target; how to specify it.
- [FormLinks vs EditorID as Identifiers](mutagen-modding.github.io/Mutagen/best-practices/FormLinks-vs-EditorID-as-Identifiers/index.html) — choosing between EditorIDs and FormLinks for lookups and stored identifier lists.
- [ITPO Avoidance](mutagen-modding.github.io/Mutagen/best-practices/ITPO-Avoidance/index.html) — avoid exporting records identical to the previous override (ITPO/ITM).
- [Mo2 Compatibility](mutagen-modding.github.io/Mutagen/best-practices/Mo2-Compatibility/index.html) — working around a Mod Organizer 2 issue with a default-on .NET 9 feature.
- [Modifying Groups Being Iterated](mutagen-modding.github.io/Mutagen/best-practices/Modifying-Groups-Being-Iterated/index.html) — why mutating a group while iterating its winning overrides is dangerous.
- [Access Overlays Once](mutagen-modding.github.io/Mutagen/best-practices/Overlays-Single-Access/index.html) — overlay properties re-parse on each access; access once to avoid repeated parsing.
- [Prefer Readonly Types](mutagen-modding.github.io/Mutagen/best-practices/Read-Only/index.html) — Mutagen offers records in several mutability flavors; prefer readonly getters where possible.
- [Reuse Translation Masks](mutagen-modding.github.io/Mutagen/best-practices/Reuse-Translation-Masks/index.html) — translation masks are powerful for Copy/Equality control; reuse them rather than rebuilding.
- [TryGet Concepts](mutagen-modding.github.io/Mutagen/best-practices/TryGet-Concepts/index.html) — the TryGet pattern for optional/nullable concepts that may not link at runtime.

## Environment

- [Environment](mutagen-modding.github.io/Mutagen/environment/index.html) — the bootstrapper object that assembles a full game environment (load order, link cache, etc.).
- [Environment Construction](mutagen-modding.github.io/Mutagen/environment/Environment-Construction/index.html) — building your own environment (note: Synthesis patchers should use the provided IPatcherState instead).
- [Game Locations](mutagen-modding.github.io/Mutagen/environment/Game-Locations/index.html) — locating game install/data folders; prefer environments where possible.

## Link Cache

- [Link Cache](mutagen-modding.github.io/Mutagen/linkcache/index.html) — the record lookup engine, built relative to a set of mods, doing complex resolves.
- [Mod Contexts](mutagen-modding.github.io/Mutagen/linkcache/ModContexts/index.html) — opt-in advanced LinkCache alternative carrying extra contextual info for added features.
- [Previous Override Iteration](mutagen-modding.github.io/Mutagen/linkcache/Previous-Override-Iteration/index.html) — digging past winning overrides to access earlier (non-winning) record versions.
- [Record Resolves](mutagen-modding.github.io/Mutagen/linkcache/Record-Resolves/index.html) — the core LinkCache feature: resolving/looking up a record relative to mods.
- [Scoping Type](mutagen-modding.github.io/Mutagen/linkcache/Scoping-Type/index.html) — supplying a type to LinkCache calls narrows the search, improving speed and memory.

## Load Order

- [Load Order](mutagen-modding.github.io/Mutagen/loadorder/index.html) — ordered list of ModKey'd items where later entries win/override earlier ones; also a dictionary by ModKey.
- [Winning Override Iteration](mutagen-modding.github.io/Mutagen/loadorder/Winning-Overrides/index.html) — retrieving each record's highest-priority (winning) version, as the game would use it.

## Low Level

- [Low Level Tools](mutagen-modding.github.io/Mutagen/lowlevel/index.html) — direct, less-safe binary access for tasks/users that need more than the strongly-typed record suite.
- [Binary Streams](mutagen-modding.github.io/Mutagen/lowlevel/Binary-Streams/index.html) — IBinaryReadStream / BinaryReadStream for reading ints, shorts, bytes, spans from a stream.
- [Binary Utility](mutagen-modding.github.io/Mutagen/lowlevel/Binary-Utility/index.html) — BinaryStringUtility helpers for Bethesda's single-byte null-terminated on-disk strings.
- [C# Span](https://mutagen-modding.github.io/Mutagen/lowlevel/C%23-Span/index.html) — primer on C# Spans (a general C# concept used heavily by Mutagen's parsers).
- [Game Constants](mutagen-modding.github.io/Mutagen/lowlevel/Game-Constants/index.html) — per-game header constants accounting for layout differences across Bethesda titles.
- [Header Structs](mutagen-modding.github.io/Mutagen/lowlevel/Header-Structs/index.html) — cheap lightweight overlays over raw bytes exposing header fields/content with lazy parsing.

## Plugins (Record Suite)

- [Plugin Record Suite](mutagen-modding.github.io/Mutagen/plugins/index.html) — overview of the custom classes/interfaces/functionality per Bethesda record type.
- [ModKey, FormKey, FormLink](https://mutagen-modding.github.io/Mutagen/plugins/ModKey,%20FormKey,%20FormLink/index.html) — the three fundamental identifiers and how they relate.
- [Interfaces (Aspect/Link/Getters)](mutagen-modding.github.io/Mutagen/plugins/Interfaces/index.html) — the categories of interfaces Mutagen exposes over records.
- [Importing](mutagen-modding.github.io/Mutagen/plugins/Importing/index.html) — creating mod objects from their binary plugin format (esp/esm/esl).
- [Exporting](mutagen-modding.github.io/Mutagen/plugins/Exporting/index.html) — writing mod objects out to an esp/esm on disk.
- [New Mods](mutagen-modding.github.io/Mutagen/plugins/New-Mods/index.html) — making a new mod object from scratch without a backing file.
- [Create, Duplicate, and Override](mutagen-modding.github.io/Mutagen/plugins/Create,-Duplicate,-and-Override/index.html) — constructing new records (FormKey required + immutable), duplicating, and overriding.
- [Copy Functionality](mutagen-modding.github.io/Mutagen/plugins/Copy-Functionality/index.html) — copying data into an already-existing object.
- [Translation Masks](mutagen-modding.github.io/Mutagen/plugins/Translation-Masks/index.html) — masks that customize which members participate in Equality, DeepCopy, etc.
- [Equality Checks](mutagen-modding.github.io/Mutagen/plugins/Equality-Checks/index.html) — record equality functionality (present but not thoroughly tested).
- [Printing](mutagen-modding.github.io/Mutagen/plugins/Printing/index.html) — turning a Mutagen object into a string for inspection/logging.
- [Flags and Enums](mutagen-modding.github.io/Mutagen/plugins/Flags-and-Enums/index.html) — record data exposed as strongly-typed flags and enums.
- [Abstract Subclassing](mutagen-modding.github.io/Mutagen/plugins/Abstract-Subclassing/index.html) — handling records that are abstract base classes with concrete subclasses.
- [Asset Links](mutagen-modding.github.io/Mutagen/plugins/AssetLink/index.html) — strongly-typed wrapper around an asset subpath string.
- [Bethesda Format Abstraction](mutagen-modding.github.io/Mutagen/plugins/Bethesda-Format-Abstraction/index.html) — hiding binary-format implementation complexity that doesn't reflect record content.
- [Compaction](mutagen-modding.github.io/Mutagen/plugins/Compaction/index.html) — the compaction styles of Bethesda mod files (esl-tagging / form-id ranges).
- [Exceeding Master Limits](mutagen-modding.github.io/Mutagen/plugins/Exceeding-Master-Limits/index.html) — the 255-master hard limit and Mutagen's automatic multi-file splitting.
- [FormKey Allocation and Persistence](mutagen-modding.github.io/Mutagen/plugins/FormKey-Allocation-and-Persistence/index.html) — experimental proof-of-concept for allocating/persisting FormKeys.
- [Remapping FormLinks](mutagen-modding.github.io/Mutagen/plugins/Remapping-FormLinks/index.html) — tools for repointing FormLinks when duplicating/overriding/reorganizing records.
- [Other Utility](mutagen-modding.github.io/Mutagen/plugins/other-utility/index.html) — handling the auxiliary files that accompany a plugin (strings, bsa, ini, etc.).

## Plugins — Specific Records

- [Specific Records](mutagen-modding.github.io/Mutagen/plugins/specific/index.html) — note that per-class/field docs aren't feasible (thousands generated); this section covers notable ones.
- [Globals and GameSettings](mutagen-modding.github.io/Mutagen/plugins/specific/Globals-And-GameSettings/index.html) — Global and GameSetting records and their special type-encoding rules (e.g. FNAM char).
- [Keywords](mutagen-modding.github.io/Mutagen/plugins/specific/Keywords/index.html) — convenience methods for checking/adding/removing keywords on keyworded records (Armor, Weapon, Npc, etc.).
- [Placed Objects](mutagen-modding.github.io/Mutagen/plugins/specific/Placed/index.html) — placed records (objects/NPCs/traps) living in a Cell's Persistent/Temporary lists.
- [ExtraData](mutagen-modding.github.io/Mutagen/plugins/specific/ExtraData/index.html) — Container-entry ExtraData storing item ownership and condition info.

## Game-Specific — Skyrim

- [Skyrim Aspect Interfaces](mutagen-modding.github.io/Mutagen/game-specific/skyrim/Skyrim-Aspect-Interfaces/index.html) — interfaces exposing common record aspects (e.g. INamed) across Skyrim record types.
- [Skyrim Link Interfaces](mutagen-modding.github.io/Mutagen/game-specific/skyrim/Skyrim-Link-Interfaces/index.html) — interfaces letting a FormLink point at multiple record types at once (e.g. Container contents).
- [Skyrim Perks](mutagen-modding.github.io/Mutagen/game-specific/skyrim/Skyrim-Perks/index.html) — the perk-effect hierarchy (abstract APerkEffect base and its subclasses).

## Game-Specific — Oblivion

- [Oblivion Aspect Interfaces](mutagen-modding.github.io/Mutagen/game-specific/oblivion/Oblivion-Aspect-Interfaces/index.html) — common-aspect interfaces (e.g. INamed) across Oblivion record types.
- [Oblivion Link Interfaces](mutagen-modding.github.io/Mutagen/game-specific/oblivion/Oblivion-Link-Interfaces/index.html) — multi-target FormLink interfaces for Oblivion records.

## Familiar (C# Concepts)

- [Namespaces](mutagen-modding.github.io/Mutagen/familiar/Namespaces/index.html) — what namespaces are and which to import for Mutagen access.
- [Nullability to Indicate Record Presence](mutagen-modding.github.io/Mutagen/familiar/Nullability-to-Indicate-Record-Presence/index.html) — Mutagen uses C# nullable references so a field's nullability signals an optional subrecord.

## WPF

- [WPF Library](mutagen-modding.github.io/Mutagen/wpf/index.html) — Mutagen.Bethesda.WPF tooling for Bethesda-specific content in WPF UI apps.
- [Adding Required Resources](mutagen-modding.github.io/Mutagen/wpf/Adding-Required-Resources/index.html) — Mutagen.WPF ships style-less controls; how to add required resources/styling.
- [FormKey Picker](mutagen-modding.github.io/Mutagen/wpf/FormKey-Picker/index.html) — a control to let users select record(s) by typing.
- [ModKey Picker](mutagen-modding.github.io/Mutagen/wpf/ModKey-Picker/index.html) — a control to let users select mod(s) by name.
- [Reflection Powered Settings](mutagen-modding.github.io/Mutagen/wpf/Reflection-Powered-Settings/index.html) — auto-generates a settings UI for any DTO class's fields via reflection.
