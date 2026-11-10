# Patrol removal requests / Подтверждение удаления патруля

Актуальный частичный живой результат1ноября: [окна EN/RU и связи патрулей](PATROL_LINKS_20261101.md). Он уточняет историческое «не запускалось» ниже только для перечисленных подпунктов; полная ручная группа остаётся открытой.

Point removal and whole-route removal now use an owned EN/RU Yes/No window,
replacing the confirmation checkbox on the patrol panel. Buttons follow the
existing source root/point availability. Removing a route changes its links
and local list; it does not delete the area records.

## Reply and suppression

The source `RectEx_PathsKill` uses `REQUEST_RECTEXKILLPATH=0x1000`, Yes/No,
default gadget1, `MBF_AUTOREPLY`, the original question icon and follow-mouse
position. `Misc_MessageBoxExtProc` maps the last button of a multi-button
request to return0. Thus Yes returns1; No and window close return0, and the
caller exits before removal for0. It is not a two-nonzero-button protocol.

Checking the source checkbox1200 disables every nondefault button; for this
request that means No. The modern checkbox says **Always answer Yes to patrol
removal requests during this editor session**. While checked, No is disabled;
window close and modern Escape can still cancel without remembering it. Only
Yes remembers the checkbox. Unchecking it before responding enables No again.
Actual original keyboard notification/focus behavior remains unverified.

Suppression applies to both point and whole-route removal. When enabled, the
next request executes the same guarded Yes command without opening a window;
this automatic reply does not rewrite the flag. **Ask before removing patrol
links again** resets the choice. A new application session starts unsuppressed;
changing maps or reloading lists in the same main workspace retains it.

The removal bit and the changed-route warning bit0x2000 have independent runtime
policies. Resetting either does not change the other. Neither is persisted in
map files or AppSettings JSON; copy-buffer confirmation is separate. The source
startup zeros Config; separate disk persistence of these bits is not proved.

## One owned acceptance

`EditorPatrolRemovalPolicy.BeginPoint/BeginRoute` validates the owned current
manager/route and occurrence before offering a request. Private manager version,
document revision, full area payload fingerprint, structure, unique identities,
closed-owner state and point predecessor requirements are guarded. Stale or
foreign acceptance cannot change the map or preference.

Opening, No, closing, cancellation and preference reset leave every map byte,
local list, Undo/Redo entry, counter, dirty state and activity record unchanged.
Yes consumes one request and calls the existing manager Remove or Clear once.
The source remembers its preference before returning Yes to PathsKill; the
modern request follows that ordering after preflight. It is not a private
whole-map draft and closing a later warning does not reverse the removal.

The existing manager's source effects remain: one-point removal leaves an empty
root; removing that empty root counts one accepted command; whole-route removal
retains the final area's outgoing chain. Modern one-command Undo for no-byte
acceptances remains the documented difference in [patrol callers](PATROL_EDITOR_CALLERS.md).
No raw manager operation or original grouping rule was changed here.

Reentrant replies/reset during an applying command are rejected. If an observer
throws after commit, the request remains consumed, accepted map/private-list
changes are retained, and App reports that the notification failed. Status and
follow-up warning are bound to the original manager/document so a replacement
map does not receive the previous map's result. Closing the manager in an
observer also does not resurrect its local lists or permit another acceptance.

After normal removal, the existing all-route warning check runs once. Warning
dialogs are held back while the precommit removal request is pending; binding
refreshes do not open duplicate requests. Actual native implicit TreeView
selection notifications remain a separate timing gate.

## Position and evidence

The new window embeds the unchanged original Question32 icon and uses the same
device-pixel nearest-monitor work-area placement as the warning window. Source
primary-screen/taskbar placement differs on multiple monitors; the improvement
and physical DPI gate are documented in [warning requests](PATROL_WARNING_REQUESTS.md).

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --patrol-removal-only
```

The Core check covers160 native/project cases: five unrelated-flag masks,
point/route, remember checked/unchecked, Yes/No/close, plus suppressed default
replies. It independently writes expected packed next-ID bytes at31/native and
52/project, compares complete files and opaque bytes, checks one notification/
counter/Undo and exact Undo/Redo, and checks whole read-only state for declined
requests. Further cases cover shared-route post-removal warnings, independent
flag resets, map replacement/restart, empty roots, stale private/raw/revision/
structure/reload/closed/reset frames, foreign managers, reentrant callbacks,
postcommit exceptions/owner close and Save/reopen/project game compilation.

Static source/ENRU/App/embedded-icon evidence and terminal results are in
`artifacts/Evidence/PatrolRemoval-20261012`. All previous manager/selector/raw
APIs and warning Core/window code are retained. This checkpoint did not execute
any original C, editor, game or WPF window. Legacy `CheckPatrolRoutes` in
WindowsSmoke still targets the older inline controls and raw route view; it
must be retargeted before future authorized execution, and its old passes must
not be used as evidence for current requests.

Physical EN/RU Yes/No/checkbox/Enter/Escape/close, owner focus, icon/text layout,
monitor/DPI and exact original notification timing remain open in
[manual acceptance](MANUAL_ACCEPTANCE_RU.md). All23 capability rows and complete
[FEATURE_PARITY](FEATURE_PARITY.md) requirements remain in the active goal.

The current [UI harness](PATROL_UI_HARNESS_SCOPE.md) now replaces the old panel
checkbox/raw-route checks with actual owned Yes/No/default/remember/reset paths.
It compiles but remains unexecuted; previous WPF passes cannot prove these requests.
