# Retained creature Jobs page and source callback scope

12 October 2026. The parent no longer scans the first changed creature's job
warnings before common Apply. It checks the retained Jobs page buffer, matching
the original GLOBALCTX2 split between shared Variables and Happy/Unhappy/Angry
arrays. This fixes a concrete source mismatch when multiple creatures have
different nonstandard job totals.

## Independent source and Windows evidence

`Source/Sources/Properties/GlobalVariables/Properties_Global_Creatures.c`
registers nine pages sharing one GLOBALCTX2, Jobs at ordinal6. The page structures
are zero-initialized; the creator does not set PSP_PREMATURE. Microsoft documents
[lazy creation without PSP_PREMATURE](https://learn.microsoft.com/en-us/windows/win32/controls/pss-propsheetpage).
The Jobs page is therefore not modeled as visited on initial parent creation.
This is an inference from the source flags and the documented contract, not an
executed observation of the old editor.

The source checker has three call sites: changing the creature while Jobs is
active, Jobs PSN_KILLACTIVE and Jobs PSN_APPLY. KillActive checks before saving
the Jobs tuples to Variables. Apply checks before comparing/copying the shared
Variables to the live map; that branch does not reload/save the Jobs arrays.
The helper totals read only the separate job buffers, never all changed rows.
The arrays are loaded by GetParamsCr page6, after activation/row selection.
Selecting a creature on another page changes CurrentCreature but does not
replace these job arrays. The source error message reports group/total, not
the creature ID; the modern cache records the last loaded row as provenance.

Microsoft's [PSN_APPLY contract](https://learn.microsoft.com/en-us/windows/win32/controls/psn-apply)
sends KillActive to the active page before Apply to pages. Its
[PSN_KILLACTIVE contract](https://learn.microsoft.com/en-us/windows/win32/controls/psn-killactive)
allows rejection to retain the active page. Combined with the explicit source
checks, this predicts two questions for an active bad Jobs buffer when the
first Continue was not remembered: one on deactivation, one on Apply. If the
first reply enables source auto-reply, the second uses Continue without a
window. An inactive, previously visited Jobs page checks its last buffer once
on Apply, including a no-byte Apply. An unvisited/zero-initialized buffer does
not invent a warning. Actual old common-control notification delivery is still
unexecuted and must be compared physically; no window/harness was launched here.

## Current implementation

`CreatureJobsPageContext` owns an immutable copied warning projection, last row,
initialized state and a revision. Capture sorts Happy/Unhappy/Angry in source
order and rejects foreign-row, duplicate-group and Hate input without changing
the old snapshot. Disposal invalidates and clears it. It has no map-edit
authority and does not save or persist data.

`CreatureGlobalsWindow` captures on Jobs activation and before saving an active
Jobs page. Source-style deactivation confirmation uses this buffer. The common
Apply confirmation uses the initialized retained buffer, replacing the previous
`ConfirmJobs(plan.JobWarnings)` scan. Other-page/creature navigation and a fresh
private session after Apply do not erase the last buffer. Returning to Jobs or
resetting that page loads the current row into it. Opening a new parent starts
uninitialized. Continue checks its captured buffer revision as well as the
existing owner, row/input/context and complete private-plan guards.

Example: edit creature0 Happy to37%, accept navigation, then creature1 Happy to73%
and accept navigation to Attributes of creature2. Common Apply now asks about
73%, the retained Jobs buffer. The previous all-changed-row scan asked about
creature0's37% instead. Cancel preserves live map bytes/history and leaves the
private draft open. A later valid common Apply still commits the complete
declared context in one Undo, even when several creatures were edited.

The generic Core plan still lists changed-row warnings for its separate direct
API acknowledgement contract. That API and the warning policy remain unchanged;
the UI caller now supplies the source callback selection rather than widening
raw write permissions. Remember/reset/default/Cancel rules remain those in
[the job warning request](CREATURE_JOBS_WARNING_REQUEST.md).

## Atomicity boundary

The modern parent retains one atomic common Apply. In the original source,
each page's Apply handler may copy the entire context before a later Jobs
callback rejects. We do not reproduce a possible earlier live commit on later
cancellation. Its exact occurrence depends on native delivery order and is not
claimed as executed evidence. Original partial-commit behavior, native timing,
focus and history remain comparison gates; a safe atomic transaction is not
declared proof of complete historical behavior.

## Verification

`CreatureJobsPageContextChecks` independently distinguishes plan row0=37 from
retained row1=73 in native/project private contexts. It verifies read-only
capture/consent, current/inactive/unvisited/no-op states, immutable input copies,
source group ordering, malformed capture rollback, closed and replaced buffer
guards, all6 reply/remember KillActive→Apply traces, one atomic two-row common
Apply and exact original-byte/counter Undo. Source/static audit captures three
preserved inputs and verifies the three checker sites and separate array loads.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --creature-jobs-page-context-only
```

Build, focused model and nine fresh model checks have terminal exit0; the full
unfiltered ordered corpus proof is in CURRENT_STATUS and
`artifacts/Evidence/CreatureJobCallbacks-20261012`. EN/RU2533 keys are unchanged.
`CreatureJobsCallbacksSmoke.cs` prepares full/focused EN/RU×native/project
actual-window cases, including a second-request Cancel, two-row cached total,
inactive/unvisited/no-op and remembered first response. It compiles but has not
executed. The future UI switch is `--creature-jobs-callbacks-only`; it opens real
windows and may play sound, so it cannot run under the current no-UI instruction.
No Core model result proves physical modern/original GUI, audio, DPI or game
acceptance. See [manual checks](MANUAL_ACCEPTANCE_RU.md) and the complete
[parity requirements](FEATURE_PARITY.md).
