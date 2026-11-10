# Original scenario parameter-page scope

This checkpoint follows accepted complete event descriptions. Descriptions are
read-only projections; they do not implement the original parameter-page controls.
All editing capabilities in FEATURE_PARITY remain the goal.

## Authoritative inventory

`Test/audit_scenario_control_pages.py` reads the original C/catalogue/resource
files and current translation/property-choice sources, without executing C or
editing maps. Its append-only output is
`Test/scenario-control-pages-source-audit-20261008-03.json`.
The active catalogue has 63 conditions and 40 actions. There are 37 condition
parameter pages and 32 action parameter pages: 69 total. The remaining 26
conditions/eight actions use dialog -1 and have no original parameter page.
Do not invent pages from opaque imported operands for those events. Common
condition repeat choices are a separate page, resource **7199**, not 7150.

The inventory retains exact top-level cases and their shared bodies for
SetDefault, ParamsInit, ParamsCheck, ParamsStore and ParamsSave. Nested switch
cases do not become top-level event cases. It retains all 69 resource headers
and declarations, the full event callbacks/new-wizard functions, gate/list
helpers and the repeat callback/resource. Sixteen additional original helpers
cover ten named display lists, four ordinary combo factories and the nested
creature-options dispatcher/callback. All input hashes remained unchanged.

Thirty condition pages have explicit source control IDs 200..205 assigning
comparison operand param1 values 1..6. `ComparisonCases` retains the literal
assignments and corresponding resource declarations for independent testing.
No assumption that every condition uses that operand is permitted.

Direct Init/Check/Store cases reference 96 condition and 65 action operands.
At the initial audit, 32 condition/47 action operands lacked an EventParam label
in both languages. These counts are a conservative direct-case inventory:
callback-only compound flags and temporary FullRect/Point values need separate
review. They are not the number of remaining controls or a completion percentage.
Complete defaults are preserved as evidence, not treated as visible UI inputs.

## Initial implementation and actual gaps

`EditorWorkspace.xaml.cs` uses event-specific EventParam resources when present,
falling back to ordinary field labels. Property choices first use alias slots,
then entity references, then `ValueCatalogues.ForField`. The latter has no
condition/action-specific enum/catalogue routing. Generic numeric editing and
translated descriptions do not supply source comparisons, player/species/room
choices, radio states, dependent inputs or compound dialogs.

The four inline variable/timer pages are fully accepted in
SCENARIO_ALIAS_EVENTS.md. The camera-path selector is separately accepted in
CAMERA_PATH_SELECTION.md; its existence does not close the entire camera-action
parameter page. Keep those accepted draft/lifetime guarantees when composing
the remaining controls.

## Source differences that must survive implementation

- Resource 7103 (elapsed time) contains a disabled numeric duration box and
  separate hour/minute/second inputs. The shared callback updates the numeric
  value. Retain the actual source limits and distinguish this from the accepted
  variable/timer alias page's two-digit units.
- ACTION_DISPLAYTIMER clamps edited hours/minutes/seconds to 23/59/59 in
  `Triggers_ActionDlgParams` and stores the duration. Its disabled timer-name
  combo does not authorize a fifth alias-renaming page.
- Creature/room/door/trap/terrain/player/interface selectors use different
  lookup identities and context flags. Any entries use list sentinel -1 which
  Store may convert to 0; category headings use 0 and are not selectable values.
  Elites, special rooms and room inclusion are call arguments, not one universal
  catalogue. Preserve unknown imports without offering headings as choices.
- Lost/killed creature conditions select players through param4; creature-level
  conditions use param4 for the level (Combo404 + 1). Ordinary creature-count
  species use param2. Native compiler
  omission of some project operands is independently audited in
  SCENARIO_DESCRIPTION_SOURCE_SCOPE.md; do not add absent native fields.
- Door/trap state param3 uses literal source values 1/9/17, not enum ordinals.
  Player/owner source IDs and variable-room identities must not be replaced
  with displayed indexes or unrelated catalogue IDs.
- ACTION_ADDCREATURE button 313 invokes `Things_SelectOptions` with
  THING_OPTIONSADD and the param4 DWORD. The nested resource is 6111; option
  bits/radio exclusivity differ from the ordinary creature dialog. Preserve
  unrelated/imported bits and compose the nested draft with the parent edit.
- Coordinate point/rectangle controls and area choices use shared callback
  temporaries and source enable/disable branches. Gate IDs in project actions
  are native compiled coordinates, not an interchangeable numeric parameter.
  Native unresolved/ambiguous matches require explicit handling.
- All source paths for inactive values, default initialization, validation,
  audio/mentor selection and original gate/point selection remain acceptance
  requirements. A plain numeric field or a description is not proof of them.

## Current implementation and remaining verification

The later option stage fills all 79 missing direct-case semantic labels in both
languages and adds all thirty comparison and twenty literal radio groups.
The first actual shared windows now cover nineteen complete scalar pages and
twenty-six repeat-only condition pages. See SCENARIO_CONTROL_WINDOWS.md for
controls, independent fixtures and acceptance limits. This does not close all
69 parameter pages or their named/nested/controller workflows.

The next exact catalogue/helper contexts are captured in
SCENARIO_NAMED_PARAMETERS_SOURCE_SCOPE.md and the -02 named-source audit.
This separates packed/variable/physical IDs, combo item data/indexes and
callback-derived coordinate choices before the remaining controller work.

Implement meaningful EN/RU parameter labels, actual source value catalogues,
comparisons/radios/units and compound pages across the entire inventory. Preserve
exact typed native/project field layouts and imported inactive/unknown data;
guard missing/future/foreign/stale/raw/edit-and-Undo inputs. Keep graph IDs,
connections, repeats, owner bindings, selections and copy buffers intact.
Build private drafts, include nested settings, and apply a whole accepted page
atomically with one history entry. Cancel must discard the complete draft.

Independent fixtures must encode literal original compiler offsets, IDs,
radio values and unit arithmetic rather than echoing implementation methods.
Exercise every source choice and dependent state, exact bytes/no-op/Undo/Redo,
reopen/compile, shared/dangling/unknown IDs and lifetime/reentrancy. Verify actual
EN/RU controls for both formats and minimum-size layouts. Freeze all C# and
assemblies during the subsequent full five-gate regression and compare the
entire ordered 94-error baseline on all 578 KWD inputs.

This source audit is preparation evidence. It does not claim those controls are
implemented, does not replace original-GUI/audio/native-dialog/hardware/DPI or
in-game acceptance, and does not close the full goal.
