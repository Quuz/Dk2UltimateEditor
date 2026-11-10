# Историческая сверка native creature variants

Текущие guards на25октября2026: [NATIVE_DEFINITION_LAYOUTS](NATIVE_DEFINITION_LAYOUTS.md). Ниже сохранён исторический аудит и его датированные проверки; прежний отказ5537 больше не описывает текущий продукт.

Read-only audit, 2026-10-08. No schema or binary has been changed by this audit.

## Behavior at the 2026-10-08 audit baseline

The original `Source/Sources/Compiler/Compiler_Generate.c:2498–2526` allocates
5449 bytes per creature, declares version 4 / information version 28, and writes
46 records. The compiler's explicit offset assignments are the primary source
for `LegacyGlobalSchemas` editable fields. `NativeMapDocument.ParseDefinitions`
preserves other record lengths but exposes no inferred fields.

`GlobalDefinitionRows` additionally requires the exact source length and one
complete catalogue in one section; offsets are stepped by that length.
`ObjectBatchPlacement` separately requires creature length 5449 before reading
terrain-safety flags. The two existing 5537 fixtures consequently check refusal,
not successful editing. They cannot establish support for the extended layout.

## Independent parser evidence

[OpenKeeper's KwdFile creature reader](https://github.com/tonihele/OpenKeeper/blob/master/src/main/java/toniarts/openkeeper/tools/convert/map/KwdFile.java#L1255)
reads its existing creature fields before the extension. Its extension branch
then consumes 80 opaque bytes, a 4-byte flags value, a 2-byte unknown value and
a 2-byte fractional value. The branch is selected for item size above 5536.
This corroborates an appended 88-byte extension; it is not evidence to shift
the original editable offsets, infer every future size, or expose unknown tail
fields. The current local original compiler remains the editable-field source.

The initial filename scan found one installed 5449-byte standalone file. The
subsequent read-only scan of all 5116 `.kwd`/`.kld` paths under `D:/Games/DK2`
found **500 valid creature sections**, including **476 real 5537-byte sections**:
16 × 46/5449, 8 × 44/5449, 164 × 44/5537 and 312 × 46/5537. All information
versions are 28. Their paths, offsets, lengths, SHA256 hashes and names are
recorded in `Test/native-creature-sections-audit-20261008.json`; the repeatable
reader is `Test/audit_native_creatures.py`. All inputs were read only.

Actual extended fixtures include
`DKII 13/Dungeon Keeper 2/Data/editor/AutoSaveGlobals.kld` (44 rows, section
offset 45353) and
`Dungeon Keeper 2/Data/editor/maps/ANL - 3 FunGlobals.kld` (46 rows, offset
50174). The first 44 stored names of the standalone original-version file
match the corresponding 1.7 prefix; some packed files have alternate names,
so names alone are not an identity guard. A separate read-only check found
the creature ID byte at common-prefix offset `0xF4B` equal to ordinal+1 in
**all 500 sections**, for both sizes and both counts. OpenKeeper independently
reads the creature ID after the three bytes beginning at `0xF48`. The current
editable-row code still requires 46 records, leaving a real 44-row historical
catalogue gap as well as the size gap.

## Next steps recorded at that audit (historical)

1. Independently account for the reader's common-prefix byte lengths and check
   known original field positions before accepting 5537 as a verified size.
   Use the actual packed 5537 sections above without modifying files.
2. Centralize the explicit accepted-size predicate: all original family sizes,
   plus only creature 5537 once verified. Keep default compiler arrays at 5449.
3. Use the actual common record length for catalogue stepping; require uniform
   rows, a verified 44/46 creature catalogue, matching source fields, common
   prefix ID order, valid header and section bounds. Missing newer species
   must remain unavailable, never invented or renumbered.
   Keep other sizes, split catalogues and unknown container layouts refused.
4. Share that verified predicate with object-placement terrain-safety lookup.
   Do not substitute false/default flags for unverified definitions.
5. Replace only the old 5537-refusal expectation with supported-prefix tests;
   retain neighbouring unknown-size refusals. Exercise every editable field,
   flag bit preservation, named compound/numeric child editors, reset, one
   Undo/Redo, map-scoped export/reopen and unchanged 88-byte tails. Add packed
   known/unknown mixed-section and malformed count/size fixtures.
6. Confirm actual EN/RU WPF use and broader Core/corpus gates after the change.
   Verify the actual 5537 fixtures and unchanged source hashes, not only the
   synthetic extension. Gameplay acceptance remains a separate evidence need.

## Later implementation and dated verification

The parent-properties five-gate regression is accepted (85 Core groups,
256 UI cases in each configuration, corpus 1734 passes / identical ordered
94 failures, actual Ancients.map 86 passes). The audit-baseline statements
above describe the former implementation.

`NativeCreatureDefinitionLayout.cs` now verifies the explicit common-prefix
layouts, 44/46 counts, complete header and dense record bounds, matching
fields and ordinal+1 IDs. Row mapping and placement safety share that guard.
Original compiler/default arrays remain 5449 bytes. The appended 88 bytes
remain opaque; missing newer species remain unavailable. Family reset can
reset available rows without creating a missing species. Other sizes,
wrong ID/info/count and split catalogues retain refusal.

Focused Core exit 0: `Test/editor3-native-creature-focused-debug-04.log`.
Four real standalone/packed sections cover every field and all eight source
reset groups with independently encoded expected buffers, exact Undo/Redo,
no-op history, map-scoped export/reopen and original hashes. Whole real
packed files additionally cover adjacent sections, mixed unknown/known
transaction rejection and independent editing of a verified other family.

Focused WPF exit 0: `Test/editor3-native-creature-ui-focused-debug-01.log`,
40 PASS lines across all four variants in EN/RU. These include full-family
reset from a missing-species selection, exact Undo, unknown-catalogue guards
and existing numeric/jobs/attacks/resistance/possession interaction checks.
Twelve renders are in `Test/native-creature-ui-render-01`; two inspected
views show available and missing species in EN/RU. Both full builds have
zero warnings/errors. Full serial attempt 01 failed only in the older
zero-ID synthetic ABI/compiler fixture (85 passes / one failure). That fixture
now writes the original 1–46 ID sequence; focused compiler verification passes
without weakening a guard. Current full serial regression
`editor3-native-creature-variants-20261008-02` has accepted DebugCore exit 0,
**86 groups / zero failures**. DebugUI subsequently passed 296 cases, exit 0.
ReleaseCorpus completed with 1735 passes / the exact ordered 94 parent failures,
captured exit 1; proof is the matching `-corpus-verified-comparison.json`.
ReleaseUI then failed because history smoke read the next source operation
before its translation existed in the older compiled assemblies. This mixed
run does not close Release UI/real-map acceptance. Consistent combined run
`editor3-scenario-alias-events-20261008-01` covers native variants plus the new
alias window and has all five terminal gates accepted: Core 87/0, Debug/Release
UI 312 PASS each (all exit 0), corpus 1736/94 with captured exit 1 and exactly
the same ordered parent failures on 578 KWD inputs, actual Ancients.map 88/0
exit 0. Status is Completed, workers gone; proof is
`Test/editor3-scenario-alias-events-20261008-01-five-gates-verified.json`.
Original GUI, other compound/catalogue behavior and gameplay remain open.
