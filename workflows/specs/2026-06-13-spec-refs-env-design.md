# Spec `$ref` / `$env` resolution layer — design

Date: 2026-06-13
Status: approved (design), pending implementation plan

## Goal

Give ModForge specs a generic JSON include + parameterization layer so a "named preset
library" becomes a folder of preset JSON files pulled in by reference, instead of a
category-specific preset-expansion mechanism baked into the builder.

Two directives, resolved **before** the spec is deserialized into `ModSpec`:

- **`$ref`** — splice JSON in from another file, a sub-node of another file, or a sub-node of
  the same document.
- **`$env`** — substitute an environment variable's value, with an optional default.

This closes the standing CLAUDE.md TODO "明亮 LGTM/IMGS 抽成具名 preset 庫" — the proven
bright-interior LGTM/IMGS values move into a preset file that any spec `$ref`s.

Non-goals: no builder changes, no new record types, no LLM/templating beyond `$ref`/`$env`.

## Architecture

A pre-deserialization JSON preprocessor operating purely on a `JsonNode` tree. It does not
touch the builder, validator semantics, or any record code.

Lives in `src/ModForge.Core/SpecRefs.cs` as `public static class SpecRefs`. The core engine
takes injected delegates — `Func<string,string?> readFile` (path → text, null if missing) and
`Func<string,string?> getEnv` (var → value, null if unset) — so it is unit-testable without
touching the filesystem or process environment (same pattern as `Voice.BuildLipGenArgs`).
A thin `ResolveFile(path)` convenience wires the real `File.ReadAllText` + `Environment`.

## Pipeline

```
spec.json
  → File.ReadAllText
  → JsonNode.Parse
  → SpecRefs.Resolve(root, baseDir = dir(spec.json))
  → resolved JSON string
  → CheckUnknownFields(resolved)   (Validate path only)
  → JsonSerializer.Deserialize<ModSpec>(resolved, ReadOpts)
```

CLI gets a single chokepoint `ResolveSpecJson(path) -> string`. Both `ReadSpec` (used by
build / package / voicelines / voicediag) and `ValidateCmd` route through it. Unknown-field
detection runs on the **resolved** JSON, so:

- `$ref` / `$env` keys are already gone → never reported as unknown fields.
- Typos inside a `$ref`'d file are still caught.

`ReadOpts` gains `NumberHandling = JsonNumberHandling.AllowReadingFromString` so a string from
`$env` deserializes into numeric spec fields.

## `$ref` semantics

A node is a ref node when it is an object containing the key `$ref`. The value has three forms:

| `$ref` value | meaning |
|--------------|---------|
| string `"a.json"` / `"a.json#/x"` / `"#/x"` | single reference: whole file, file + JSON Pointer, or same-document pointer |
| array `["base.json","warm.json","local.json"]` | chained deep-merge, **later overrides earlier** |
| object `{ "from": <resolvable>, "pointer": "/x" }` | explicit long form of a single ref; `from` is itself resolvable (may be `$env`), `pointer` optional |

After the `$ref` value resolves to a node, the **sibling keys** (everything in the node except
`$ref`) deep-merge on top — **sibling wins**:

```json
{ "$ref": "presets/brightInterior.json", "fogFar": 12000 }
```
→ brightInterior's content with `fogFar` overridden to 12000.

**Deep-merge rule:** objects merge recursively; arrays replace wholesale (the overriding array
replaces the base array — no element-wise merge or concat).

**Path resolution:** file paths are relative to the *referring document's* directory (so a
preset file can `$ref` its siblings relatively). Same-document pointers (`#/...`) resolve
against the root of the document in which the pointer textually appears.

**JSON Pointer:** RFC 6901 (`/a/b/0`, with `~1`→`/`, `~0`→`~`), navigating objects by key and
arrays by index.

**Long-form `merge` key:** reserved for a future per-ref merge toggle; v1 always merges. Other
unknown keys inside the long-form object are an error.

## `$env` semantics

A node is an env node when it is an object containing the key `$env`.

```json
{ "$env": "MF_PRESET_DIR" }                         // value required; error if unset
{ "$env": "MF_PRESET_DIR", "default": "presets" }   // value if set, else default
```

- The env value is inserted as a **JSON string** node.
- `default` is inserted as-is (any JSON type: string / number / object / array).
- Unset and no `default` → **error, build stops** (never silently empty).

## Recursion & safety

- Spliced-in content may itself contain `$ref` / `$env` → resolved recursively, each in its own
  document/base-dir context.
- **Cycle detection:** a stack of (resolved file, pointer) entries; re-entering one throws with
  the cycle path printed. A recursion-depth ceiling backstops pathological inputs.
- A node containing **both** `$ref` and `$env` → error.
- Real spec data never legitimately uses keys named `$ref` / `$env`, so there is no collision
  with genuine fields.

## Relationship to existing `presets{}` catalog

`PresetCatalogSpec` (the non-emitting cookbook bag from commit a6dccfb) is unchanged. It now
doubles as the natural target for same-document `$ref: "#/presets/..."`. The "named preset
library" is realized as an `examples/presets/` folder of preset JSON files referenced via
`$ref`. After resolution the `presets` object still deserializes into `PresetCatalogSpec` and
is ignored by the builder — no double-emit.

## Determinism note

`$env` makes a spec's resolved output environment-dependent (by design — that is the
parameterization). Record FormID determinism is unaffected (it derives from EditorIDs, not from
the raw spec text), so voice `.fuz` filename stability still holds.

## Files

**New**
- `src/ModForge.Core/SpecRefs.cs` — resolver engine (delegate-injected) + `ResolveFile`. <300 lines.
- `tests/ModForge.Core.Tests/SpecRefsTests.cs` — string ref, file+pointer, same-doc pointer,
  array chain-merge (later wins), long-form object, `$env` present, `$env` default, `$env`
  missing→throw, `$ref`+`$env` conflict→throw, cycle→throw, nested recursion, sibling override.
- `examples/presets/bright-interior.json` — a real preset (full LGTM + IMGS records) extracted
  from the proven bright-interior values.
- An example spec that `$ref`s the preset file and uses `$env`.

**Modified**
- `src/ModForge.Cli/Program.cs` — `ResolveSpecJson(path)` chokepoint; `ReadSpec` routes through
  it; `ReadOpts` gains `NumberHandling = AllowReadingFromString`.
- `src/ModForge.Cli/Program.Build.cs` — `ValidateCmd` resolves first, then runs
  `CheckUnknownFields` + `Deserialize` on the resolved JSON.

**Docs**
- New `docs/SPEC-refs.md` (full `$ref`/`$env` reference) + link from `SPEC-index.md`.
- `docs/CODE_MAP.infra.md` — SpecRefs row + SpecRefsTests row.
- `docs/lifelike/cookbook-presets.md` (+ zh-TW) — show `$ref` usage.
- `CLAUDE.md` — strike the "抽成具名 preset 庫" TODO; add `$ref`/`$env` gotchas.
- `examples/spec.schema.json` — best-effort note only; `$ref`/`$env` are preprocessor directives
  resolved before deserialize, so the data schema does not enforce them.

## Build sequence

1. `SpecRefs.cs` engine + unit tests (TDD: tests drive each form).
2. CLI chokepoint wiring (`ResolveSpecJson`, `ReadOpts`, ValidateCmd) + offline regression green.
3. Example preset file + demo spec; `validate`/`build` exercise it.
4. Docs + CODE_MAP + CLAUDE.md + schema note.
