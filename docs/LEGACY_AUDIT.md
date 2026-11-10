# Legacy project audit

Audit date: 2026-10-05. Source revision: `122d2ee3ba65c87f9551cb13bd4ba1872b6edcba`.

Upstream HEAD was rechecked with `git ls-remote --symref origin HEAD` on 2026-10-05: `master` still points to the same revision, dated 2019-05-12. The rewrite is based on the latest source in the user-specified NovFR repository. References to older versions elsewhere in these documents mean historical DKM section layouts supported by that source, not older Git revisions being upgraded one by one.

## Executive summary

The original editor is valuable as documentation of the DK2 formats, but it is not a safe base for continued feature development. It contains roughly 70,000 lines of C/C++ tied to Borland C++ Builder 6, extensive global mutable state, ANSI-only UI strings, direct binary structure casts, and no automated tests. The chosen strategy is an incremental rewrite with the old source retained as a behavioral reference.

## Build and maintenance findings

- The Code::Blocks project references `main.cpp`, `resource.h`, and `resource.rc`, none of which exist at those paths. It cannot build the actual editor.
- The Borland project depends on BCB 6 packages and machine-relative paths such as `..\..\BCB6\Projects`; there is no reproducible toolchain definition.
- The repository ships historical EXE/DLL binaries but has no reproducible release process or provenance checks.
- No license file is present. Redistribution rights must be clarified before publishing binaries or accepting broad contributions.
- There are no unit tests, integration tests, CI jobs, or golden format samples.
- Source files use a legacy Western ANSI encoding without an encoding policy. Russian cannot be added reliably to that UI without a Unicode migration.
- UI text is spread across Win32 resources, preprocessor macros, and 2,948 mutable `char[]` definitions in 24 `Texts_*.c` files.

## High-risk correctness and security findings

### P0 — untrusted dimensions can bypass bounds checks

`Files_Game.c` computes `Width * Height * 4` with 32-bit arithmetic before comparing it with the file size. Crafted dimensions can overflow and let a truncated map reach later conversion code. The new reader performs bounded dimension checks and uses wide arithmetic.

### P0 — record parsing can read beyond truncated files

Thing records are selected from an untrusted type field after checking only that the record pointer has not yet reached the end. The code does not prove that the complete selected structure remains inside the buffer before dereferencing or advancing it.

### P0 — player IDs index arrays without range validation

`Game_ExtractPlayers` obtains `PlayersCount` from an input byte and immediately accesses `MapOptions[PlayersCount - 1]`. Values outside the expected player range can write out of bounds.

### P1 — pervasive unbounded string operations

Static inspection found 479 `strcpy` calls, 734 `strcat` calls, and 255 `wsprintf` calls. Many destinations are `MAX_PATH` or fixed format fields, and several paths are built by concatenation without remaining-capacity checks. Examples include compiler output paths and DLL paths.

### P1 — input buffers are reused as error-message buffers

`Game_ExtractAll` overwrites its `FileName` argument with localized error text using `strcpy` and `strcat`. The caller's capacity is not part of the contract, and the file path is destroyed during error handling.

### P1 — fixed UTF-16 fields assume null termination

KWD metadata is passed to `WideCharToMultiByte` with length `-1`. A malformed fixed-width field without a terminator can make conversion scan beyond the field and potentially beyond the loaded file.

### P1 — saving is not transactional

The legacy writer creates destinations with `CREATE_ALWAYS` and writes sections directly. Later failure can leave a truncated or partially updated map. The rewrite will validate a temporary output before atomic replacement.

### P2 — DLL handling leaks and can loop forever

`Library_Call` returns without unloading the DLL when `GetProcAddress` fails. `Library_Close` retries `FreeLibrary` forever on persistent failure. The DLL path is also assembled with unbounded concatenation.

### P2 — registry strings are insufficiently validated

Registry reads do not validate value type and can accept a maximum-length value without proving null termination before later string operations.

## Quantitative indicators

- C/C++ source and headers: approximately 70,000 lines.
- Localized string arrays: 2,948 definitions across 24 files.
- `GlobalAlloc` call sites: 270.
- `strcpy`: 479 call sites.
- `strcat`: 734 call sites.
- `wsprintf`: 255 call sites.

These counts are risk indicators, not proof that every call is exploitable.

## Validation completed by the rewrite

- Clean .NET build with warnings treated as errors: 0 warnings, 0 errors.
- Synthetic tests cover valid KWD metadata, valid Map.kld dimensions, legacy DKM recognition, corrupt declared sizes, and English/Russian catalog parity.
- 57 real `.kwd` maps from a local Dungeon Keeper 2 installation were inspected with their Map/Players/Things/Triggers/Variables companions.
- Result for that corpus: 57 passed, 0 structural errors, 0 missing companions.

Native editing now adds lossless byte-buffer round trips, explicit record bounds, typed layouts from the original C definitions, 262 global property offsets/flag masks, all 103 original event types, rollback and undo/redo, ID-link remapping, independent script graph cloning, and map-scoped definition exports. All 57 corpus maps pass unchanged round trips and scoped terrain-flag edit/reopen/undo checks. Initial semantic checks report script IDs/links and object coordinates, but do not constitute complete gameplay validation.

## Format corrections discovered during porting

- KWD reference records are variable in count and location; use the parameter length and header counts, not filename stems or a fixed 15-entry table.
- Retail `GlobalVariables.kwd` can contain extra opaque data beyond its declared size; preserve it instead of truncating or refusing every map.
- `GAMEBANDHERO` slots begin at byte 51 even though the C `GAMEBAND` structure rounds to 52 bytes.
- Definition files may be packed as concatenated native sections in `Globals.kld`.
- The 5537-byte creature definition variant is retained without guessing editable offsets; the legacy compiler's schema uses 5449 bytes.
- DKM/DKZ `HEADER.Size` excludes the 12-byte header; native KWD/KLD size includes it.
- `Things_GetThingPos` is a misleading name: it returns X/Y coordinates, not array indices. A blanket scripted-map deletion ban was therefore removed after inspecting this function.
- The old player compiler counts keepers with hearts, then serializes that many consecutive option indices. This can assign the wrong settings when keeper IDs have gaps. The rewrite iterates actual active player IDs; a regression test uses only keeper 5 alongside hero/neutral players.
- DKM-to-native compilation needs both editor values and native base definitions: the project omits graphics and other native definition bytes. The rewrite retains these bytes and checks known definition counts/layouts before applying editor values.
- New-map definition copying originally placed shared variables after map-local variables; selecting the last signature match then sent edits to shared variables. Collection insertion now prefers the map-local export path, with a regression test proving shared global-variable bytes remain unchanged.
- A duplicate `Field.Players` localization key caused WPF resource loading to fail at startup with an unhandled XAML exception (Windows reported `0xe0434352`). The previous set-based key-parity check hid duplicates. Resource validation now rejects duplicate keys before comparing languages; startup with a real map was rechecked in both languages after the fix.
- A separate Windows .NET Runtime event at 2026-10-05 11:43 identified `MainWindow`'s `GeometryDrawing.Pen` receiving the string `#2B251F`, causing another startup XAML exception. It now uses an explicit `Pen` with Brush and Thickness. Debug (the path in the crash report) and Release were rebuilt and launched in both languages. The in-process WPF runner now tests the full shell in addition to EditorWorkspace, so this resource is included in regression coverage.
