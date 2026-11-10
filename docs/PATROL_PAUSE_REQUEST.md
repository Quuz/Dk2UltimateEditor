# Patrol numeric pause request — current implementation

Актуальный частичный живой результат1ноября: [окна EN/RU и связи патрулей](PATROL_LINKS_20261101.md). Он уточняет историческое «не запускалось» ниже только для перечисленных подпунктов; полная ручная группа остаётся открытой.

12 October 2026. **Modify pause** for a selected patrol occurrence now opens an
owned EN/RU OK/Cancel window. It uses a private literal-text draft and applies
one existing manager command only after acceptance. Start and the Append/Insert
area selector keep their separate workflows. This document describes code and
CLI evidence; physical UI, original GUI and gameplay acceptance remain open.

## Original source contract

`Source/Sources/Things/Things_Paths.c:845`, `RectEx_PathsModifyPause`, resolves
the selected area and initializes the request from `Area->waitdelay`, a signed
LONG in `Source/Includes/RectEx.h`. Its flags are
`MBF_ESNUMBER|MBF_NOBEEP|MBF_EDITTEXT|MBF_NUMBER`; it supplies the original question
icon, OK/Cancel, minimum0, maximum250 and follow-mouse positioning. The prompt
describes seconds spent in this area before joining the next area. Cancel/close
return0. Accepted OK writes the pause, increments MapChanges and refreshes the
selected tree caption. This function contains no explicit PathsCheck or Undo
allocation; implicit original TreeView notification timing has not been proved.

`Source/Sources/Misc/Misc_Requests.c` uses signed
`GetDlgItemInt(hDlg,1400,NULL,TRUE)`, initially selects all text, and displays
minimum/maximum in readonly edit fields. There is no spinner or slider in this
request. A value below0 or above250 opens a nested warning, leaves the parent
request open and refocuses/selects its number. The outer NOBEEP flag suppresses
its opening sound; the nested range warning uses the helper's default sound.

The documented [GetDlgItemInt contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdlgitemint)
reads a decimal prefix, stops at a nonnumeric character and returns0 on failed
conversion. The [ES_NUMBER documentation](https://learn.microsoft.com/en-us/windows/win32/controls/edit-control-styles)
allows nondigits through paste even though typing is restricted. These are
primary references, not proof that the original dialog was physically executed.

## Core and App behavior

`EditorPatrolPauseSession` owns the manager, route and occurrence. Its initial
text retains the signed value without clamping, including imported values
outside0–250. `SetText` changes only this draft. The modeled parser skips ASCII
leading whitespace, accepts an optional sign, reads ASCII decimal digits until
the first other character and returns0 for missing digits or Int32 overflow.
For example, `12x` becomes12 and empty text becomes0. Raw Win32 behavior for
all clipboard/Unicode/locale edge cases remains unverified.

`PatrolPauseWindow` filters typed input to ASCII digits while leaving paste
available, displays readonly0/250 bounds and retains text on language changes.
It embeds the original question and warning icons. OK is the default button;
Cancel, Escape and close discard the draft. Invalid range opens an owned
warning with `MessageBeep(0x30)` and returns to the parent for correction.
Opening, typing, range errors and cancellation do not change map bytes,
local lists, history, activity, counters or notifications.

A valid OK calls the existing manager SetPause exactly once, even for an equal
value. The modern one-Undo policy remains a deliberate difference from the
original source's missing pause Undo. No extra explicit warning check is added.
Manager lifetime, private list version, document revision, raw/structural
changes, identity ambiguity, closed/foreign routes and reentrancy are guarded.
An observer error after commit consumes the accepted request; the App reports
it only while the original manager/document still owns the result.

Closing/replacing/reloading the owner invalidates the request. Pending numeric,
warning and removal requests cannot overlap through the coordinator. This
request has no remember checkbox or suppression flag; removal0x1000 and
indirect-change warning0x2000 remain independent.

The window uses the nearest monitor's work area in device pixels, reusing the
existing cursor-placement geometry. Original placement used the primary-screen
helper. Real monitor/DPI, focus, keyboard, sound and layout have not been checked.

## Signed project wait-delay correction

The old read-only `PatrolAreas` getter parsed project Int32 waitdelay as UInt32.
A negative imported value therefore threw OverflowException before the routes
could be shown. `PatrolWaitNumber` now reads Int32 then retains its raw bits;
the numeric request presents those bits as a signed LONG. Native UInt16 values
keep their previous representation. Raw graph mutation method bodies are
unchanged; this read-only getter is a real product fix, not a fixture omission.

## Verification and remaining acceptance

`EditorPatrolPauseChecks` enumerates26 literal numeric cases in both formats
(52 cases), including empty/failed/prefix conversion, signs, boundaries,
overflow, imported65535 and project -1. Independently encoded whole buffers
check native UInt16 wait offset24 and project32-bit wait offset56, retained
opaque bytes, range retry, Cancel, equal/changed acceptance, one counter/
notification/Undo, exact Undo/Redo, lifetime/foreign/nested guards, postcommit
observer error/close and save/reopen/game compilation.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-pause-only
```

Evidence: `artifacts/Evidence/PatrolPause-20261012`. Initial build CS0819 and the
negative-pause OverflowException are retained with exit1; corrected full build
and focused group exited0. All41 projects have zero warnings/errors. Nine fresh
model checks and publish exited0 on the same Core DLL. Full runner92175 exited1:
1809 passes and the exact previous ordered63 failures, all1808 previous passes
retained, original94 compared in full. Interrupted runner62118 has no final
report; its partial log is retained separately and is not passing evidence.

WindowsSmoke CheckPatrolRoutes has now been retargeted and compiled against
the current owned routes/windows; see [prepared harness scope](PATROL_UI_HARNESS_SCOPE.md).
It has not executed these new requests. No editor/game/WPF/original-C launch or render
was performed for this checkpoint. Follow [manual acceptance](MANUAL_ACCEPTANCE_RU.md)
for current EN/RU, keyboard, clipboard, range retry, DPI and sound checks.
All [FEATURE_PARITY](FEATURE_PARITY.md) requirements remain in scope.
