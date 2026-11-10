# Placed-map effect generator options

The properties tab adds an English/Russian editor for the original four effect slots and integer frequency. Open an effect rectangle's properties from the map manager, or select its record. Each slot has all 371 source effect choices (0x000–0x172), independently of the generator's own identity. None, nonzero source “Nothing” entries and the source 1.7 annotations remain distinct.

## Source and storage evidence

- `Source/Sources/Things/Things_RectEx.c`, `RectEx_EffectOptionsProc`: four separate lists, frequency range 1–255, integer input and slider. New effect creation sets frequency to 1. Both original IDOK and IDCANCEL messages write the options; the modern UI deliberately provides explicit Apply and Discard draft commands instead of implying that the original dialog had a non-writing Cancel.
- `Source/Includes/RectEx.h`: project `MAPRECTEX` is 292 bytes in the preserved 32-bit ABI. Four signed LONG slots are at offsets 60/64/68/72; frequency is a signed LONG at 76. ID at 44 is a separate field.
- `Source/Includes/Game.h`: native `GAMEEFFECT` is 44 bytes. The four WORD slots are at 28/30/32/34, frequency BYTE at 36 and generator ID BYTE at 37; opaque unused fields and six trailing bytes are preserved.
- `Source/Sources/Compiler/Compiler_Generate.c`, `Compiler_GenerateEffects`: project slot values are cast to WORD, frequency/identity to BYTE, and native width/height store inclusive-endpoint deltas. `Files_Game.c` reads the slots, frequency and identity independently.

No seconds/ticks conversion is inferred. Frequency is edited as the stored integer, not an invented time unit. New native and project generators now both use the source default frequency 1; existing imported zero/unknown values are not normalized.

## Editing contract

The five-option draft is preflighted in full. Changed effect IDs must be source choices; changed frequency must be 1–255. Unchanged native WORD IDs outside the catalogue and byte frequency 0, as well as imported signed project LONG values, can be retained exactly. Repairing one slot does not silently repair another. All changed options apply in one undoable transaction; no-op Apply creates no history.

The baseline guards the entire physical generator payload. Identity, geometry or raw-property changes invalidate an older compound draft; a pending generic property edit is committed but the stale generator options are refused. Bounds, identity, scripts, other generators and opaque bytes are not changed by applying options. Live language switching retains the current draft; switching records or replacing/closing the document clears it. Generic field properties also expose the named catalogue for the four slots, never for generator identity.

Only verified 44-byte native and 292-byte project layouts are editable in this panel. Unknown native variants remain losslessly saveable, with the specialized controls disabled and without guessed prefix values. Compact windows scroll the options while reserving space for the raw-property table.

## Verification

`EffectGeneratorChecks.cs` exercises every one of the 371 choices in each of four slots in both formats, independently encodes full expected files at literal offsets, verifies frequency endpoints, complete-tuple validation, unknown retention/repair, no-ops, notifications, stale/foreign/removal guards, undo/redo, save/reopen and compilation. A native fixture has nonzero unused/padding bytes throughout the exhaustive sweep. An extended 48-byte native fixture verifies read-only refusal and unchanged saving; altered/missing field layouts are rejected too.

`EffectGeneratorSmoke.cs` supplies four actual WPF cases (EN/RU × native/project): source lists/translations, manager property navigation, all-slot Apply/Revert, integer/slider synchronization, invalid-draft atomicity, no-op/undo, generic named search/apply, separate identity semantics, pending raw commits, unknown values, record/language/document lifecycle and 980 × 500 scrolling. The native cases additionally open the extended unknown-layout fixture and verify disabled, blank controls and unchanged bytes. Eight rendered layouts are saved under `Test/EffectGeneratorsUiQA-Final-20261006`; representative full Russian and compact English layouts have been inspected.

```powershell
dotnet run --project tests/DK2MapEditor.Validation -c Debug --no-build --no-restore -- --effect-generators-only
dotnet run --project tests/DK2MapEditor.WindowsSmoke -c Debug --no-build --no-restore -- --effect-generators-only
```

The focused core group passes Debug/Release; the final synthetic regression has 57 passing groups in both configurations, and all 140 WPF cases pass in each configuration (including four loaded-map cases). Both solution builds have zero warnings/errors. The [expanded 578-map corpus](EXPANDED_CORPUS.md) remains failing and is reported separately. Historical GUI comparison and actual gameplay are still unverified: byte/control tests are not in-game acceptance or proof of complete editor parity.

## Definition lookup responsiveness

Definition ordering now caches read-only tables parsed from immutable embedded source instead of rereading all seven tables for every row/default lookup. The original arrays, including -2 opaque entries, are unchanged. Global-definition tests compare all seven complete literal orders, verify concurrent cache reuse and reject mutation through an `IList<int>` cast; the exhaustive editing tests remain intact.
