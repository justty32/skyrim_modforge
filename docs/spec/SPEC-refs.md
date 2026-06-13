# ModForge spec — `$ref` / `$env` includes & parameterization

← [index](SPEC-index.md)

Two preprocessor directives resolve **before** a spec is deserialized, so they may appear in
place of *any* value anywhere in the spec and never reach the record builder:

- **`$ref`** — splice JSON in from another file, a sub-node of another file, or a sub-node of
  the same document. This is how a **named preset library** works: keep reusable fragments in
  their own files and pull them in.
- **`$env`** — substitute an environment variable's value, with an optional default.

> These are ModForge directives in the **spec data**. They are unrelated to the internal
> `$ref` used inside `spec.schema.json` (that is JSON-Schema's own keyword, interpreted by
> schema validators, not by ModForge).

Resolution runs at the top of every `validate` / `build` / `package` / `voicelines` /
`voicediag` (anything that reads a spec). Unknown-field checking runs on the **resolved** JSON,
so a typo inside a `$ref`'d file is still caught.

## `$ref`

A node is a ref node when it is an object containing the key `$ref`. The value takes three forms.

### String — a single source

```json
{ "$ref": "presets/bright-interior.json" }          // whole file
{ "$ref": "presets/bright-interior.json#/lgtm" }    // a sub-node of a file (JSON Pointer)
{ "$ref": "#/presets/lighting/brightInterior" }     // a sub-node of the SAME document
```

File paths are relative to the **referring document's** directory (so a preset file's own
`$ref`s resolve relative to that preset file, not the top spec). The part after `#` is an
RFC 6901 JSON Pointer (`/a/b/0`, with `~1`→`/` and `~0`→`~`).

### Array — chained deep-merge, later wins

```json
{ "$ref": [ "presets/base.json", "presets/warm.json", "presets/local.json" ] }
```

Each source resolves, then they deep-merge left→right: later sources override earlier ones.
Use it to layer a base preset + a variant + local tweaks.

### Object — explicit long form (lets `$env` drive the path)

```json
{ "$ref": { "from": { "$env": "MF_PRESET_FILE", "default": "presets/bright-interior.json" },
            "pointer": "/lgtm" } }
```

`from` is the file path (itself resolvable, so it may be `$env`); `pointer` is the JSON Pointer
into that file. This is the superset of the string form — reach for it only when you need an
env-driven path. `merge` is reserved for a future per-ref toggle. Any other key is an error.

### Sibling override — sibling wins

Keys next to `$ref` deep-merge **on top of** the ref result:

```json
{ "$ref": "presets/bright-interior.json#/lgtm", "fogFar": 12000 }
```

→ the preset's LGTM with `fogFar` overridden to 12000. Merge rule: **objects merge recursively,
arrays replace wholesale.** (Chained-array `$ref` "later wins" is a separate axis — it merges
ref *sources*; data arrays inside a value still replace, never concatenate.)

## `$env`

A node is an env node when it is an object containing the key `$env`.

```json
{ "$env": "MF_PRESET_DIR" }                          // value required; error if unset
{ "$env": "MF_PRESET_DIR", "default": "presets" }    // value if set, else default
```

The env value is inserted as a JSON **string**; the CLI deserializes with
`NumberHandling.AllowReadingFromString` so a string lands fine in a numeric spec field. The
`default` is inserted as-is (any JSON type). An unset variable with **no** default is a hard
error — `$env` never silently produces an empty value.

## Errors

All of these stop the run with a clear `SpecRefException`:

- `$ref` file not found / pointer not found.
- A `$ref` cycle (`a → b → a`).
- A node containing **both** `$ref` and `$env`.
- A long-form `$ref` with an unknown key, or a non-string `from`.
- `$env` unset with no `default`.

## Named preset library

Put reusable fragments in files under e.g. `examples/presets/` and `$ref` them. The shipped
example `examples/presets/bright-interior.json` holds a bright-interior `lgtm` + `imgs`;
`examples/spec-refs-demo.json` pulls both in and attaches them to a cell. Specs may also keep
fragments in their own non-emitting `presets` object (see
[cookbook-presets](../lifelike/cookbook-presets.md)) and `$ref` them with `#/presets/...`.
