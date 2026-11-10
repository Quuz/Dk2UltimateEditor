# Creature job probability request — current source scope

[Проверка и исправления 2 ноября 2026](CREATURE_JOBS_LIVE_20261102.md): реальный RU125% запрос37 и общий Apply; четыре WPF варианта EN/RU/native/project прошли. Другие ручные условия перечислены в остатке.

12 October 2026. The nine-page creature parent now uses an owned EN/RU
**Continue/Cancel** warning for nonstandard job totals. It replaces the ordinary
Yes/No MessageBox. Source Continue is the default; a checkbox can remember
Continue during this editor session. This is a consent request, not a map edit.

## Original source

`Source/Sources/Properties/GlobalVariables/Properties_Global_Creatures.c:1379`,
`Properties_GlobalJobsCheckApply`, checks Happy, Unhappy and Angry in that order.
The first total other than0 or100 opens one request; Hate has no probability
total. `Properties_GlobalJobsGetTotal` sums occupied slots across the complete
group, including occupied slots after a hole. Source Windows LONG arithmetic is
32-bit; the request projection retains that signed bit pattern. The existing
raw long totals and generic job setters remain unchanged.

The request uses `szGlobalReplies` (`Continuer|Annuler` in the source text),
`MBF_AUTOREPLY`, `REQUEST_GLOBALJOB=0x00010000`, defaultgadget1, the exclamation
icon and centered placement. The checker is called before a creature selection
change, page deactivation and Jobs Apply. It performs no Undo or MapChanges
increment. The caller saves only after a nonzero reply. The shared parent still
compares and applies the complete private context once; visiting pages does not
produce a separate live map transaction.

`Source/Sources/Misc/Misc_Requests.c` returns default1 without a window when the
runtime bit is set. The final Cancel gadget is remapped to0. Checkbox1200 disables
other buttons while leaving the default Continue enabled. Close returns0;
only a default reply copies the checkbox into the bit. The helper's opening
sound for this icon is `MessageBeep(MB_ICONEXCLAMATION)`. Runtime Config is
initialized on startup; map/JSON persistence of this preference is not implied.

## Implemented policy and owner checks

`CreatureJobsWarningPolicy` owns one request at a time. Continue accepts one
read-only consent and updates only bit0x10000 for a prompted reply. Auto-Continue
retains existing flags. Cancel/close/dispose do not remember the checkbox. Reset
clears only this bit and invalidates any active ticket. Restart or an independent
workspace starts without suppression. This preference is independent from patrol
removal, patrol warnings and copy-buffer confirmation.

The actual workspace passes one policy across parent reopenings and map changes.
The parent footer includes **Ask about nonstandard job totals again**. Reset is
unavailable during a nested request and does not reset the private creature
values or commit history. The parent Cancel discards its map draft; a previously
accepted runtime preference remains a session preference, as in the source.

The parent captures the document/session plan, row editor, context revision and
pending input revision before asking. Continue is refused if any of these become
stale. A replaced/closed document closes the owned warning; a stale ticket cannot
remember consent. Reentrant navigation, Reset or Apply is blocked while asking.
The older test confirmation delegate remains supported but cannot grant stale
authority and has no remember option. The normal UI uses the actual owned window.

The current parent now checks the retained Jobs page buffer before common
Apply, replacing the earlier changed-row plan scan. See [callback scope](CREATURE_JOBS_CALLBACK_SCOPE.md)
for active KillActive/Apply, last visited/inactive/unvisited/no-op cases and the
atomicity difference. Original native delivery remains physically unverified.
Changing language can invalidate the parent's captured context; close and reopen
the request in the selected language rather than applying old consent.

## Window

`CreatureJobsWarningWindow` displays the first invalid group's name and signed
total, embeds the original Warning32 icon and centers on its owner. Continue is
default; checking the remember box disables Cancel, while Escape/close still
discard consent. Only Continue remembers. Loaded wires the source system
exclamation sound. EN/RU labels use shared dynamic resources; a stale request
shows the existing stale-owner notice and refuses Continue.

The standalone raw Jobs panel still offers explicit acknowledgement for its
additional direct Apply workflow. The nine-page parent hides that checkbox and
uses this request on its shared private route. Generic direct job edit APIs and
their explicit acknowledgement contract have not been widened.

## Evidence boundaries

`CreatureJobsWarningChecks` covers11 literal signed LONG projections and96
flag/reply/remember cases across native/project formats. Whole live and private
file snapshots, history, activity, dirty state, revisions, counters and
notifications prove read-only consent. Tests cover invalid groups, simultaneous
requests, one-use replies, stale pending input, policy disposal/reset, restart,
reopened/new-map consent and unrelated flag preservation.

```powershell
dotnet tests/DK2MapEditor.Validation/bin/Release/net10.0/DK2MapEditor.Validation.dll --creature-jobs-warning-only
```

Initial focused exit1 came from a fixture requesting removal of empty job slots;
the fixture now selects occupied slots. The product guard remains unchanged.
Both final full build and focused model check exited0; all41 projects have zero
warnings/errors. Nine fresh model checks and publish exited0 on the same Core.
The complete ordered corpus comparison is recorded separately in
`CURRENT_STATUS.md` and `artifacts/Evidence/CreatureJobRequest-20261012`.

`CreatureJobsRequestSmoke.cs` prepares real owned-window EN/RU×native/project
checks for Cancel, remembered/default Continue, reopening, replacement, reset
and owner close. It is compiled, not executed. A future explicitly authorized
UI run would use `--creature-jobs-request-only` in WindowsSmoke and open real
windows with the warning sound. No UI/game/original-C/audio execution or render
was performed here. Existing dated WPF results do not prove this new request.
Manual checks are in [manual acceptance](MANUAL_ACCEPTANCE_RU.md); all
[FEATURE_PARITY](FEATURE_PARITY.md) requirements remain active.
