# Named global map variables

[Actual native RU125% acceptance, 2 November 2026](MAP_VARIABLE_LIVE_20261102.md): key4=900, nested/outer cancel, atomic apply/undo, exact bytes, export/reopen. In-game and other residuals remain open.

The seventh workspace tab provides all 193 variables in the original 20 groups, with curated English/Russian names, group/name/hex-key search, stored values, source reference defaults, ranges and modified/missing/read-only indicators. Select rows and apply an integer; multiple rows are one preflighted transaction. Selecting one integer row loads its current value into the input (or the explicit source reference default if a native override is absent). Selection and typed drafts survive refresh/translation; clearing or reset requires confirmation, invalidated by selection/filter/document changes.

## Original evidence

- `Source/Sources/Datas/Datas_Map.c`, `MapVars`: exact order, IDs, unsigned default bit patterns, four fixed-point flags and integer input ranges. `MinWidth`/`MinHeight` are reused as minimum/maximum in this particular catalogue.
- `Source/Sources/Datas/Texts_Map.c`: French labels and 20 grouping separators; translations preserve those meanings and units without claiming they are extracted game-language strings.
- `Source/Sources/Properties/Map/Properties_Map_Variables.c`: integer modify dialog, default shortcut, changed-value marker and reset-all. The four entries with nonzero `Icon` display a fractional value but manual modification is refused. Reset-all restores them in the project array.
- `Source/Sources/Misc/Misc_Maths.c`, `Misc_FloatToText`; `Source/Includes/Editor.h`, `FLOAT_PRECISION`: unsigned 20.12 fixed-point, eight decimal places with trailing zeros removed (retaining one decimal). The modern display uses exact decimal division by 4096; it does not reinterpret bits as IEEE floating-point or signed 16.16.
- `Source/Sources/Files/Files_Game.c`, `Game_ExtractVariables`/`Game_SetGlobalVariable`: original workflow reads the map's `Variables.kld`, ignores the separate shared `GlobalVariables.kwd`, accepts known integer keys, rejects fixed-point/unknown keys and uses last matching integer data.
- `Source/Sources/Compiler/Compiler_Generate.c`, `Compiler_GenerateGlobalVariables`: omit source-default integer values and all four fixed-point entries.
- `Source/Includes/Map.h`/`Compatibility.h` and the original historical reader: current and metadata versions 4–6 have a 256 × LONG array; versions 1–3 do not. Existing explicit project upgrade supplies the missing original defaults.

## Editing and storage

189 source-defined integers are editable. Project edits change only the selected signed 32-bit array elements. Native edits create absent verified keys or update only the last matching 16-byte map record, preserving earlier duplicates and both unused DWORDs. The first DWORD is a dispatch key: it is constructed from a verified catalogue ID, never exposed as a generic mutable header. Reserved headers 1, 2, 0x42 and 0x4D belong to different record categories and cannot enter this editor.

All four fixed-point entries (0x9B, 0x9E, 0xDC, 0xDD) are display-only in this workflow, matching the original. A project can reset them to source defaults. Native reset/clear retains these records unchanged because the original loader/compiler does not support them as overrides. Unknown native records and all uncatalogued project slots remain intact. Raw existing typed data editing is a separate API/workflow.

Project reset writes exact catalogue defaults, including torch red **300**, which is outside its original manual range of **0–255**. It is not silently clamped or rewritten on display. Native reset removes every exact matching integer override, consistent with the original compiler's omission of defaults. Clear is available only for native maps. Neither command changes shared installation variables or other map data. Missing native values display `—`; the separate source-default column is reference information, not a claim that the installed game currently uses it. Reset all covers all supported variables regardless of the search filter. Unknown metadata versions and missing variable sections remain read-only.

Creating arbitrary unknown global keys was previously listed in the parity ledger as a requirement, but the source UI does not offer it and its compiler rejects/omits such keys. That incorrect original-parity requirement is removed; unknown existing records still remain lossless.

Native creation explicitly inserts into the chosen per-map collection, including custom descriptor paths outside `Maps/`. Shared `GlobalVariables.kwd` cannot become an insertion fallback merely because it is loaded last. A missing per-map collection remains read-only. The same scope boundary now applies to availability, pools, modifiers and alliances; the additional scope regression covers all editable keys, both reference orders, custom paths, missing files, undo and export, plus actual EN/RU buttons.

## Automated verification

One additional core regression group covers all 193 projections, 189 minimum/maximum edits in both formats, per-key outside-range rejection, reserved/fixed-point/duplicate-ID mixed-batch failure, signed data, native payload encoding, exact undo/redo/no-op history, independent expected project bytes for batch/reset, duplicate last-wins/update/clear, unknown and reserved payload preservation, shared-versus-map variables, save/reopen/re-export, source compiler omissions and all metadata versions 1–7/future-version refusal. It includes the source-default 300 anomaly and unsigned maximum 20.12 display.

Four additional in-process WPF cases construct and lay out the actual EN/RU × native/project editor. They verify bound values/names, group and bilingual search, multicell edits, inline range/parse errors, no partial changes, selected/full reset, confirmation lifecycle, fixed-point project reset, live selection/draft translation, exact undo and unchanged map selection/copy buffer. They do not constitute mouse/keyboard visual QA or in-game acceptance. Read-only variable projections are also compared before/after export on the real native/text corpus.

Historical-GUI comparison, rendered visual inspection, effective game/default behavior and global-definition reset integration remain open. Complete player reset is now a separate [player workflow](PLAYER_SETTINGS.md) and does not reset these map variables. `Properties_MapResetOptions` is source initialization for new/loading maps, not an existing-map editing command; see [reset scope evidence](MAP_RULE_MATRICES.md#reset-scope-clarification). Full feature parity is not claimed.
