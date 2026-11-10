# Read-only gameplay terrain diagnostics

Evidence: preserved `Source/Sources/Compiler/Compiler_Compile.c`,
`Compiler_TaskTestMap`, `Compiler_TaskTestMapRect`, `Compiler_TaskTestMapExpand`,
and the size columns in `Datas_Map.c`. Original files are unchanged.

`TerrainLayoutDiagnostics.Validate` reports the original compiler's terrain
checks on both native maps and editable projects. It never paints, normalizes,
changes records, sends document change notifications, or consumes undo/redo.

| Code | Rule | Severity |
| --- | --- | --- |
| DK2N019 | Consecutive nonempty rows must have equal widths | Error |
| DK2N020 | Dungeon heart must be exactly 5 × 5 | Error |
| DK2N021 | Portal or gate fails its catalogue's minimum/maximum size | Warning |
| DK2N022 | More than one heart for the same keeper | Error |
| DK2N023 | No heart on the map | Error |
| DK2N024 | No heart for keeper 1, native owner 3 | Error |
| DK2N025 | A later keeper has a heart but an earlier keeper is absent | Error |

Portal and mercenary portal: 3–5 × 3–5. Gate limits include both orientations of
terrain 37, zero/unlimited maxima, and the unconstrained terrain 29. See the
[placement table](EDITOR_TERRAIN_PLACEMENT.md) for all original dimensions.

## Deliberate source fidelity

- Scan is raster ordered, from each unhandled top-left tile. Compare terrain and
  variant, **not owner**. Touching hearts of different owners are therefore one
  oversized region, just as in the original. Mixed owners inside a rectangular
  heart are not a new ownership-consistency check.
- Do not use native reserved byte as a catalogue discriminator. The project and
  native representations differ from the original editor's in-memory tile.
- Check row size before comparing row widths, then check total height. A narrow
  row may therefore be a size warning rather than a nonrectangle error.
- Expand each reported region to the minimum consecutive row width and full
  height, not a flood-component bounding box. Subsequent exposed remainders may
  produce further reports. `MapDiagnostic.Region` uses zero-based tile coordinates.
- Duplicate hearts are allowed for Heroes/Neutral (owners 1/2), not keepers.
  Keeper sequence uses heart-region anchor owners, not player-record array order.

One intentional UX extension: the new diagnostic pass continues after fatal
regions and lists **all** missing earlier keeper positions. The old compiler
stops at its first fatal region and reports only the first missing position.
This avoids repeated validate/fix cycles without changing source severity rules.
Unknown terrain/variant identities are not assigned guessed footprint semantics.

Bottom-up row summaries make shape analysis linear in tile count; their results
are compared with an independent direct-row reference in randomized tests.

## Validation and saving

`NativeMapValidator.Validate(document)` remains structural/script validation for
lossless APIs and unfinished documents. `includeGameplay: true` adds this pass.
The actual main-window Validate menu uses the full pass and displays EN/RU
summaries, source file, detailed technical information and error/warning colours.
Live language switching retains and retranslates the results; map changes clear
stale results. Compilation from the menu checks the project before any output
dialogs and the compiled bundle before export, refusing errors and retaining the
existing warning confirmation. Saving an editable copy remains possible.

Programmatic byte-preserving conversion and saving do not imply a playable map;
they remain available for incomplete maps. These checks alone do not prove game
acceptance or validate every gameplay rule, object constraint, gate-region link,
player option, script argument or unknown variant.

## Verification

- Native/project fixtures: all nine original special terrain/variant entries,
  minimum/maximum/unlimited dimensions, shape-versus-size precedence, diagnostic
  regions, repeated hearts, Heroes/Neutral exceptions, missing hearts/keepers,
  source owner-insensitive adjacency, reserved bytes and save/reopen.
- 100 deterministic random malformed maps for each representation are compared
  with an independently written row scanner and explicit dimension constants.
- A 1024 × 1024 unrestricted gate region is handled as one rectangle.
- Exact unchanged file buffers, change notifications and undo/redo state.
- Four main-window WPF cases (native/project × EN/RU) exercise the shared menu
  validation path, actual result list, severity colours, language switching and
  refresh after undo/redo, without visible windows or saved settings.
- External corpus runs report terrain errors/warnings separately from structural
  failures and require identical diagnostic results after round-trip/compilation.
  An existing map's gameplay report is not used to reject lossless preservation.

Historical-GUI comparison and execution in Dungeon Keeper 2 remain outstanding.
