# Recipe cookbook — presets

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

`presets` is a non-emitting catalog for named copy-paste recipes. The builder ignores it; to create
records, expand a preset into the normal top-level arrays (`lightingTemplates`, `imageSpaces`,
`weathers`, `climates`, `packages`, `books`, `identities`, etc.).

Worked example: [`examples/presets-cookbook.json`](../../examples/presets-cookbook.json). It includes
both the catalog and concrete expanded records so `validate`/`build` can exercise the recipes.

## Lighting presets

- `brightInterior` — clean readable interior fill. Use the LGTM + IMGS on `cells[].lightingTemplate`
  and `cells[].imageSpace`.
- `warmTavern` — amber low-fog tavern/shop lighting with mild bloom.
- `coldDungeon` — blue-grey, lower-saturation dungeon lighting with pale fog.

## Weather presets

- `clearBright` — clear vanilla sky base plus brighter outdoor ImageSpace grading.
- `foggyPale` — cloudy pale weather with close fog and desaturated grading.
- `stormCinematic` — rainy storm weather with rain SPGD, stronger wind, and contrast-heavy grading.

Weather presets produce WTHR/CLMT records only. Assign the climate to a worldspace/region or use the
console `sw` command printed by `build` to force-test a generated weather.

## Package presets

- `guardPost` — compact sandbox for guards holding a local post.
- `wanderMerchant` — service-oriented sandbox for merchant NPCs; a real shop still needs a vendor
  faction and merchant chest.
- `campFollower` — movement-only Follow package; omitted target defaults to the player.

Attach package editorIds to `npcs[].packages`. Package order still matters: put specific travel/follow
or quest-gated packages before broad sandbox fallbacks.

## Identity presets

- `Adventurer` — default baseline identity granted from game start.
- `Merchant` — toggle identity acquired from a ledger book.
- `Guard` — toggle identity acquired from a writ book.
- `Paladin` — acquired from an oath book and active while the player wears heavy armor.
- `Dragonborn` — auto-granted once `DragonSouls >= 1`.

Dialogue gates use the existing `identity` and `primaryIdentity` fields. Identity acquire books need
the `package` command to ship the reusable identity scripts.
