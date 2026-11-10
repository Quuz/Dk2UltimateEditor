# Native references, size metadata and source-compatible payload bounds

## Western ANSI references

Native KWD reference entries contain 64-byte `char` paths, not Unicode paths. The preserved French Win32 editor passes these paths to ANSI file APIs; the local `¡Bestial!` map stores byte 0xA1 for `¡`. The modern loader previously decoded references as 7-bit ASCII, replacing this and other extended characters with `?`, then incorrectly rejecting the resulting path as unsafe.

References now use the existing strict Windows-1252 field codec for reading, factory writing and changed-definition-reference writing. Unchanged descriptor bytes, including opaque bytes after NUL, are preserved. The 63-byte terminated-field limit remains enforced; unrepresentable characters throw instead of becoming `?`. This does not claim that every historical machine's CP_ACP was 1252, nor add automatic code-page guessing. Newly generated map stems retain their existing ASCII restriction; importing existing Western ANSI references is separate.

`NativeReferenceChecks` reproduces the previous failure before the fix with `références/¡éßœ€_...` paths. It verifies all loaded/missing references, exact companion bytes, unchanged save/reopen, map-scoped definition edits/reopened values, reference rewrites, exact undo and input immutability. Parent traversal, rooted drive paths and real `?` characters still fail. It also retains the earlier stale-local-companion collision regression.

## Tile size metadata and opaque tails

`Source/Sources/Files/Files_Game.c`, `Game_ExtractMap` (around lines 505–519), verifies section headers and checks that actual available bytes are at least `width * height * 4`. It does not require `dwMapDefineSize` to equal that amount. Three local Jibbits bundles have complete 128 × 128 tile data (65,572-byte files) but store zero at offset 32. The modern inspector/loader incorrectly treated these as truncated data.

The loader now bounds cell access by actual file length and validated dimensions, while retaining existing version/header, 1–1024 dimension and safe file-size limits. A mismatching stored tile-size field generates inspector warning `DK2W124`, not a false truncation error. Actual missing tile bytes remain fatal. Extra bytes beyond the dimension-derived cell array are opaque, not extra tiles.

Painting, ordinary saving and undo preserve size metadata/tails exactly. Resize explicitly regenerates dimensions and tile-byte count while carrying opaque trailing bytes after the new cell array. The file-size header includes the complete file; the tile-size field counts only actual cells. Unsafe oversized results fail before mutation.

`NativeTileLayoutChecks` uses stored sizes 0/1/120/UINT_MAX, empty and nonzero odd-length tails, independently encoded painting bytes, save/reopen, shifted resize, exact undo/redo and unchanged input files. Actual one-byte/full-payload truncation and invalid/overflowing dimensions are refused. `NativeTileLayoutSmoke` now has eight EN/RU actual-control cases for stored 0/UINT_MAX and actual/undersized generic file sizes: canvas painting, live translation, resize refresh, undo/redo and reopened exports with retained tails. The four undersized-file cases also open the actual MainWindow, verify editing remains visible and status is valid, and check both size warnings in each language and after live switching.

## Generic native file size is not an exact end marker

The earlier tile correction left a separate inspector defect: it required offset-8 generic file size to equal physical length. Twelve complete corpus bundles still received errors even though native editing/save/reopen succeeded. Three Jibbits tile files store 36 at offset 8 despite containing all 65,572 bytes; other affected companion files likewise understate physical length.

`Game_ExtractMain`, `Game_ExtractPlayers`, `Game_ExtractMap`, `Game_ExtractThings`, `Game_ExtractVariables` and `Game_ExtractTriggers` in `Files_Game.c` never use their generic file-size field as an exact end marker. Map bounds use actual length; Things/Variables/Triggers iterate declared record counts against actual data, while Players uses its separate definition size. The modern editing loader already permits a generic size smaller than physical length and separately validates actual tile/record access.

The structural inspector now follows that existing native contract: a smaller generic size produces **warning DK2W141**, not an error; sizes larger than physical length remain errors. This is not blanket acceptance of any payload: missing cells/records and invalid native versions remain rejected by editing. Tile information version must also be 8, matching original and modern loading; tile-companion diagnostics are no longer emitted twice. DKM/DKZ editor `HEADER.Size` is a distinct payload-length contract and retains exact-size validation. No file is normalized or repaired by inspection.

`NativeFileSizeChecks` first reproduces the former false rejection, then covers all six native bundle files × four generic sizes (0/12/actual−1/actual) × empty/nonzero tails: 48 variants. Each verifies warning presence, unchanged record parsing, no history, byte-exact save/reopen/undo and input immutability. Oversized generic sizes for all six files, actual one-byte-short tile/record payloads despite a smaller stored size, strict editor outer sizes and invalid tile-info versions are independently refused. Both size warnings have EN/RU resource keys, checked in the actual shell by the eight tile WPF cases above.

Final full regression results are tracked in [EXPANDED_CORPUS.md](EXPANDED_CORPUS.md); original GUI/game acceptance remains separate.

## Truly truncated Variables fixtures

The nine version/declared-size refusals in the expanded corpus all have version 4 but lack exactly one 16-byte variable record. Their three repeated layouts are:

| Actual bytes | Required by record count / declared file size | Missing bytes |
| ---: | ---: | ---: |
| 4,152 | 4,168 | 16 |
| 3,880 | 3,896 | 16 |
| 3,976 | 3,992 | 16 |

The original `Game_ExtractVariables` checks its current pointer against actual file size before each of the declared records and would fail at that final record. These are not cases where relaxing a metadata equality restores source-compatible reading. No record/header is automatically invented or repaired, and no input is changed.
