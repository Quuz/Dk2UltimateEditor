# KWD reference-aware structural inspection

The former descriptor inspector checked only five files beside the KWD, named from its stem. The editing loader instead follows the descriptor's link table, including shared definitions, searches parent roots and uses local fallbacks. The inspector could therefore report wrong dimensions, inspect stale copies, or miss a bad shared definition.

`Formats/NativeDescriptorReferences.cs` now owns the same read-only resolution plan for inspection and editing. The native loader's byte-preserving export, definition rewriting and editable-record validation remain separate. No source file is rewritten by resolution or inspection.

## Resolution contract

- Parameters header `0xDF` at offset 48; parameter length at 52; reference header `0xDE` after `56 + length`; records begin eight bytes later.
- The low word at offset 20 is the count, bounded to 128. Each record is 72 bytes: type, reserved word, and a 64-byte Western ANSI path. Reserved bytes and post-NUL bytes remain untouched.
- Existing strict Windows-1252 decoding, safe relative segments, `.kwd` extension inference, nearest-root precedence (descriptor directory plus five ancestors), then local leaf fallback.
- Resolved physical files are deduplicated case-insensitively in reference order. Missing labels/order/repetitions are retained. Blank entries are ignored.
- Five stem-based local suffix fallbacks remain available. A fallback cannot shadow an explicit resolved source or its export destination. A malformed/unsafe table is an error, not permission to inspect unrelated fallback files.

These are the existing modern native loader's resolution rules, now shared rather than independently reimplemented. The preserved original `Source/Includes/Game.h` describes the descriptor fields; its `Files_Game.c` uses associated suffix files for the five editing collections. Sharing the modern resolver does not prove every historical GUI's path behavior or code-page choice.

## Inspection and errors

All resolved links, including common definitions, receive generic header, expected nonzero reference signature, version and actual-versus-declared size checks. Reference type `0` is untyped, as in real original-generated packed `Globals.kld`; it does not mean the file's signature must be zero. Actual tile signatures govern dimension inspection. Tiles retain dimension-derived payload validation and opaque-tail warnings. First resolved tile dimensions match the loader's first tile selection. Missing links remain warnings; this does not claim a bundle without tiles is editable.

Each component is inspected independently: a bad version/size/header or unreadable file does not discard valid descriptor metadata or dimensions obtained from other files. Unsafe/corrupt reference tables use `DK2E113`; unreadable linked files use `DK2E133`; linked/descriptor files exceeding the loader's 64 MiB bound use `DK2E134`. English/Russian resource labels are provided. Signature errors refer to the actual resolved file, not a guessed neighbour.

This remains structural inspection, not a complete semantic validator. Packed Globals currently receives its first-section generic header checks, not independently reported validation of every packed section. Collection record counts/internal sections still receive deeper checks in `NativeMapDocument.Load` and `NativeMapValidator`; inspector-only record/packed-section validation, effective gameplay, original-GUI comparison and additional installations remain acceptance work. Oversized standalone tile inspection is not newly claimed by this descriptor/linked-file milestone.

## Evidence

`InspectorReferenceChecks` covers all five stale local companion collisions, custom CP1252 names with extension inference, shared-definition signature/version/size/truncation errors, metadata/dimension retention, repeated resolved references, missing tile warnings, local leaf fallback, all six allowed search roots and rejection beyond them, nearest-root precedence, count 0/128/129, malformed/truncated/overflowing tables, unsafe paths and 64 MiB bounds. Inspection/load history and original fixture bytes remain unchanged.

Four actual WPF shell cases cover English/Russian × custom-name/stale-local bundles, loaded canvas dimensions, translated warnings naming the resolved tile, and retained source/history. Invalid-table diagnostics are injected into the existing rendering path to avoid modal failed-open dialogs; error colour/status and live translation are checked. This is not automation of native dialogs.

The first corpus is terminal: 1714 passed checks / 471 failed checks (`Test/expanded-corpus-inspector-references-20261007.json`). Its 377 extra failures were the incorrect expected-zero packed-Globals signature; native/text pass counts remained 560/530/559. That report is preserved as evidence of the detected defect, not the corrected checkpoint. The initial WPF Debug process was explicitly stopped after this production change (140 groups completed); it is not a passed full gate.

Corrected Debug/Release builds have zero warnings/errors. Updated focused core (including packed/untyped, search-depth and size cases) and all four WPF cases pass in each configuration. Fresh full Debug synthetic regression is terminal: **65 groups / zero failures**, in `Test/inspector-references-debug-fixed-20261007.json`. Corrected full Release corpus is terminal (exit 1): **1714 passes / 94 failed checks**, with 65 synthetic / 560 native / 530 supported-text / 559 original-policy text passes (`Test/expanded-corpus-inspector-references-fixed-20261007.json`). The entire failure array matches the thumbnail baseline; no expected-zero signature errors remain. Full WPF Debug is terminal with **172 passing cases**, exit 0. That checkpoint's Release terminal output was not recovered; later complete 176-case camera and 180-case history gates each pass in both configurations and include all inspector cases, superseding the missing result. Whole-editor/historical/game acceptance remains incomplete.
