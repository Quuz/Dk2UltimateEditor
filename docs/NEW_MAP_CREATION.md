# New-map settings and original defaults

Source evidence: `Map_Setup.c` (`Map_NewInit`, `Map_NewReset`, `Map_CreateNew`,
`Map_CreateDefaultMap`, `Map_CreateLimitWall`), `Properties_Map_Global.c`
(`Properties_MapResetOptions`, `Properties_MapOptionsToDefault`, default AI),
`Datas_Map.c` (`NewMap`, `Versions`), and `Map.h` (80 × 80 default size).
Original source and game files are unchanged.

The actual New menu now uses `NewMapOptions`, a bilingual `NewMapDialog` and
`NativeMapDocument.CreateNewMap`. It creates an independent, unsaved document
with empty undo/redo history. Construction writes no files. Save/export still
creates a new directory and refuses overwriting existing exports.

## Original settings

- All 23 backgrounds: water, lava, gold, gems, solid/rock/unclaimed terrain,
  hero rest, mana vault, and owned reinforced walls/claimed paths for all seven
  owners. Decode the full packed catalogue ID, not only its low byte.
- 1–5 keepers; all seven project player option rows remain editable.
- Game rule versions 1.3/1.6/1.7 in project metadata, not DKM section versions.
- Separate width/height and square-height locking. Minimum size is 7 × 7.
- Initial defaults are 80 × 80, one keeper, version 1.7, rock background.
- Current document supplies name, dimensions, keeper count, target version and
  known background when reopening New, not its objects, scripts or other options.

Unlike the ASCII importer, original New sets `Options2 = 0`. Hero/Neutral pools
are all -1, and their availability arrays stay zero. Keepers have pool 32 for
ordinary eligible creatures, -1 for imp/skeleton/vampire/elites/heroes, and 20 for
dark angel and maiden. Keeper room/trap/door/spell availability, neutral alliances,
map variable defaults, health 100, gold 3000, automatic camera/build-area -1
sentinels and all 84 default AI assignments follow the original reset routine.

No objects, bands, areas or scenario events are invented. Alias slots are empty.
Fresh projects omit actual override arrays: unchanged definitions come from
the selected compilation basis. Verified project definition families are
default-backed and materialized lazily on a changed edit; resetting an absent
family to its existing defaults is a no-op. Native missing definitions remain
read-only. Full historical-GUI/game integration acceptance remains open.

## Explicit modern options

- File base name (ASCII, 1–32 characters) is separate from display name. Project
  display text uses historical Windows-1252, up to 63 bytes; native display names
  use Unicode, up to 63 UTF-16 units. Unrepresentable text is rejected, not replaced.
- The user can choose DKM or a native KWD bundle. Native mode reuses independent
  copies of available loaded game definitions; its version follows those assets,
  so the project version picker is disabled. It does not manufacture missing
  graphics/definitions. Native factory defaults include each requested keeper,
  even without a heart; camera falls back to map centre and pools/availability are
  initialized. This factory-only path does not change normal compilation's
  heart-dependent player/variable selection.
- Optional starter layout places exactly one 5 × 5 heart for each requested
  keeper (owners 3–7), separated by a tile and inside the protected border. It
  does not add portals, rooms, resources, objectives or creatures. Capacity is
  validated before construction. This option is not original New behavior.
- Original New creates a **blank map without hearts**. It remains the default.
  The full validator consequently reports the missing heart until the map is
  edited; editable saving remains available.
- Dimensions up to 1024 extend the original dialog's maximum 300. Above 120²
  tiles (the original warning threshold), or either dimension above 300, creation
  requires explicit large-map acknowledgement. File capacity is not evidence
  that those sizes are supported/performance-safe in the game.

New documents immediately display validation results, including missing native
assets. The unsaved shell shows its actual display name and dimensions rather
than “no map loaded”, and does not invent a saved file size. Live translation
retains choices and active errors.

## Verification and limits

- 276 fixtures cover all backgrounds × three versions × both formats × blank/
  starter choices, cycling through all keeper counts. Expected tile bytes, border,
  hearts, all player arrays, AI/map defaults, metadata and native camera/player/
  variable behavior are checked independently.
- First-edit undo is byte exact. Selected fixtures save/reopen every buffer exactly.
  Normal project compilation still excludes keepers without hearts. Invalid paths,
  Windows device names, unrepresentable/oversized names, invalid dimensions/count/
  version/background and insufficient starter capacity fail before construction.
- Four in-process WPF cases (EN/RU × project/native) exercise real named dialog
  controls, square/rectangle settings, overflow checks, starter capacity, Unicode
  format switching, large acknowledgement, live translation, factory and canvas.
  A synthetic private shell-document fixture checks the unsaved metadata/header
  path without invoking modal New/Discard dialogs; this is not button/OS automation.
- The 57-map native corpus also creates independent new native starters from each
  map's definitions, checking exact copied definition bytes and unchanged source
  buffers. Existing six text imports and native round trips remain regression gates.

These tests do not prove a playable level, historical-GUI equivalence, sprite
rendering, missing-asset installation, complete global-reset integration or
execution in Dungeon Keeper 2. Those acceptance tasks remain open.

## BigMapRequests-20261017

The large-map checkbox has been replaced by an original-style information
question after valid Create. No/Escape/checked close leaves the settings form;
Remember Yes is shared with resizing in map properties for this editor session.
Restore large-map question clears only0x20. Approval alone does not construct
a map and cannot survive changed settings/caller. Existing size1024 extension
and its additional>300 warning remain; the source-area threshold is14400.
The old WPF results above are historical; updated harnesses were compiled only.
[Current rules and evidence](BIG_MAP_REQUESTS.md).
