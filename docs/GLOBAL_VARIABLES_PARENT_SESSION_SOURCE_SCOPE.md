## Current canonical project — 12 October 2026

The five-page element and nine-page creature parents are implemented directly
in current `src/`. No staging directory or archived source restoration is
required. Current executed build/model/corpus proof and remaining physical
requirements are in [CURRENT_STATUS](CURRENT_STATUS.md). The new owned creature
job request is [implemented and model-tested](CREATURE_JOBS_WARNING_REQUEST.md),
with physical UI/audio/DPI/original-GUI acceptance still open. Lower dated
staging/run statements are historical evidence, not current build instructions.

## Актуальная привязка к основной версии 2026-10-10: terminal Debug458

Original shared14 GLOBALVARS callbacks (9 creature/5 elements) bound to unchanged
current root399 source/DLL and8 actual composed Debug cases EN/RU×native/project×
two sheets. Proof: `Test/editor-global14-root-debug-binding-20261010-01.json`.
Generic setter skips equal bytes and commits changed fields in one parent Undo;
shared private draft/navigation and explicit current Jobs warnings retained.
Do not increment once per visited page. Full Release gate and physical original
GUI acceptance remain pending. Lower historical statements about missing parent
routing are superseded only for this implemented and Debug-accepted scope.

# Original shared global-variable session

The original `Properties_GlobalVariables` in
[Properties_Global_Elements.c](../Source/Sources/Properties/GlobalVariables/Properties_Global_Elements.c)
allocates one `GLOBALCTX`, copies the complete live `GLOBALVARS` into it and
passes the same context to five pages6820..6824. All five procedures save row
and page switches into that private context. Reset998 targets the current row;
Reset999 targets the complete current family. Both reset the private variables.
Each page's Apply compares and copies the shared context to the live variables
inside one `Undo_CreateGlobalVariables` operation. Cancel discards the context.

The independent `Test/shared-global-variables-source-scope-20261009-01.json`
captures4 source hashes,18 complete functions, the context declaration and all
five page/reset contracts. The families have98 source rows: Terrain38, Room24,
Door8, Trap15 and Spell13. There are2016 values in verified editable/reset rules;
these include the hidden resettable Trap.ShotDelay. The header also contains24
unused Room.CeilingHeight values and padding, which must remain opaque. They
are absent from room Save/reset rules. The complete copied context also contains
unedited Creatures and CrSpells arrays; changes to the five pages must preserve
those arrays and unrelated bytes.

Current per-row field windows and live workspace reset commands do not prove
this shared transaction behavior. Required implementation and acceptance:

- One private working context spanning all five families and selected rows.
- Validated row/page switches save pending controls privately; reopening a row
  restores its prior private values and nested options.
- Current/all reset operates privately, including source hidden/reset fields;
  Cancel and owner close leave the live map byte-identical.
- One common Apply commits all changed rows/families in one Undo. No-op does
  not materialize project families or consume history.
- Current/native/project layouts, unknown values, opaque data, stale ownership,
  pending raw edits, nested dialogs, EN/RU and minimum layouts require actual
  tests across this entire scope.

The existing `ClonePropertiesDocument` implementation provides an in-memory
copy of map files and native metadata, but `MapPropertiesSession` intentionally
restricts its Apply scope and excludes global definitions. Reusing its clone
does not authorize widening that scope. A separate global-variable session
must enforce its own five-family changes and preflight before one atomic
`SetGlobalDefinitionValues` commit, while preserving other data. The original
creature/creature-spell context is a separate remaining scope.

The isolated `Test/global-variables-session-staging-20261009-01` now implements
the shared model and common EN/RU window. Its immutable snapshot contains
344 C# sources,20 independent fixtures and16 assemblies. Parent Spell339 had
accepted Core at creation; its WPF/full acceptance remains separately pending.
See [implementation and acceptance status](GLOBAL_VARIABLES_PARENT_WINDOWS.md).
The window is not delivered to production. Actual WPF, manual image inspection
and full regression remain required; implementation alone does not close this
requirement or the wider goal.


Текущее продолжение19 октября: [I11_CODE_CLOSURE](I11_CODE_CLOSURE.md). Конечная14-page callback сверка завершена; I11 закрыт по коду. Исторические слова «осталось»/staging/UI ещё не означают текущий отсутствующий код. Физическая M19/M21/M23 приёмка открыта.
