# Shared alias parameter pages

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

VARIABLE, TIMER, SETVARIABLE and SETTIMER are embedded in the common
parameter window through the reusable alias editor in current src.

The alias fields now use one reusable private editor, shared with the existing
standalone window. The embedded editor has no independent Apply; the parent
accepts its operands, current editable names and condition repeat in one Edit.
Outer Cancel discards all inputs, Reload refreshes both drafts, and translation
retains the typed names/IDs/repeat. Source choices, empty-name retention,
CP1252 limits, unknown imports and the timer's source 60–99 minute arithmetic
remain in the existing source-specific alias logic. Both drafts must belong
to the same unchanged document and event. The parent checks its own and the
child's input revisions around ordinary grid commits; stale, closed, replaced
and reentrant owners cannot apply.

## Historical checkpoint evidence (2026-10-08)

Core Debug/Release each have four parameter groups plus the alias group,
including 24 independent parent boundary cases. Final isolated alias UI
Debug/Release each have 20 passing cases: sixteen standalone cases and four
groups exercising all sixteen parent page/language/format combinations.
Both builds have zero warnings/errors. Each configuration retains 32 wide and
minimum-size renders. Six layouts were inspected on identical App/Core
assemblies, including EN/RU timer, variable comparison and both actions.
Immutable evidence:
`Test/scenario-alias-parent-staging-20261008-01/alias-parent-focused-verified-04.json`.
The earlier -03 proof is historical; -04 incorporates the numeric render-driver
fixes without changing the tested App/Core assemblies.

The old held numeric/alias patches and production-application instructions
are historical. Their changes are already in the canonical src/tests tree;
no patch application or worker wait is required now. Current physical GUI
acceptance remains manual, as recorded in CURRENT_STATUS.
