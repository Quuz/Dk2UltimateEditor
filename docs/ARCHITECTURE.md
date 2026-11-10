# Current architecture and build

## One canonical application

`DK2MapEditor.slnx` contains the three product projects and all current test projects.
`src/DK2MapEditor.Core` implements formats, schema-derived records, transactions,
geometry, script graphs, conversion, validation and export. The WPF application
in `src/DK2MapEditor.App` references Core directly through ProjectReference.
Product projects have no HintPath or dependency on Test, Archive or an older DLL.

`Source/` and `Binaries/` preserve the original implementation and assets.
Some headers, catalogue tables, default definitions and bitmaps are embedded
from these folders using paths relative to each project. These resource inputs
are part of this repository, not a separate version-restoration procedure.

Build with `./build.ps1` on Windows with the .NET 10 SDK. It builds the solution
sequentially to avoid shared Core obj races and publishes the application to
`artifacts/PreFinal/App`. External package feeds are disabled. The published
application is framework-dependent and requires the Windows Desktop Runtime.
No old executable, archived source backup or checkpoint script is needed.

## Documents and edits

NativeMapDocument retains bounded byte buffers and exposes verified editable
fields. Primitive layouts derive from preserved C declarations with explicit
wire-layout exceptions. Unknown bytes remain raw. Edits record snapshots and
support Undo/Redo. Composite sessions use independent drafts and atomic commits;
revision/fingerprint checks prevent applying stale confirmations.

MapPropertiesSession owns the general and nested map settings draft, resize
plan and crop decisions. Cancel discards the whole draft; Apply is one history
operation. Object, band and scenario editors use corresponding draft/consent
boundaries. Display settings belong to the application rather than map data.

## Conversion and publication

DKM is the editable project format. Native import produces a project plus an
immutable, hashed original map snapshot in an adjacent native-source ZIP.
Archive paths are labels: archived data is read from verified entries, never
loaded by following old filesystem paths. Original event IDs are mapped to
project roots and checked again before retention.

The compiler writes current supported project values. A generated baseline
from the immutable source projection identifies unchanged values. Native
retention restores eligible literal tiles, event payloads, gate bytes and
AREA/BAND/PLAYER service fields with geometry, identity, layout and ambiguity
guards. It never treats matching catalogue labels as proof of identity.
Member slots use the 51-byte band header and sixteen32-byte wire slots.

OfficialMapBundle packages map definitions as Globals.kld and exports exactly
seven map files. Shared GlobalVariables and editor alias metadata stay outside
Maps. Preview publication refuses existing owned destinations and checks shared
catalogue identity. File publication uses explicit validation and confirmation
boundaries; opening/validation does not silently repair installed maps.

## UI, localization and checks

The application uses EN/RU XAML resource dictionaries and persistent settings
under LOCALAPPDATA/DK2MapEditor. Source-based model tests validate logic and byte
roundtrips; the full corpus runner reports ordered passes/failures. WPF and
physical game checks have a separate scope. Building UI test projects does not
mean their UI was launched or accepted.

Current user instructions prohibit Computer Use and app/game launches by the
agent. Physical actions are described to the user. CURRENT_STATUS distinguishes
automated evidence, user observations and unfinished acceptance requirements.

## Local MCP bridge (2026-11-01)

Core/Automation serializes operations against an IEditorAutomationHost; the WPF
host dispatches to its own live document on the Dispatcher. Ordinary launches
leave the bridge disabled. The opt-in current-user named pipe is served by
the editor, while DK2MapEditor.Mcp exposes stdio lifecycle and 24 tools to
Codex. There is no parallel map model, network listener or arbitrary shell tool.
State tokens protect revisions/context/selection and clipboard/tool/paint changes; filesystem outputs stay in
an explicit workspace. See [EDITOR_MCP.md](EDITOR_MCP.md) for the protocol,
consent flags, command limits, tests and actual live-window results. The same NewMap adoption, selection/clipboard
commands, object painting planner and record editing routes serve UI and MCP.

The read-only map fingerprint includes ordered loaded-file hashes and a document
hash over all transaction buffers, including editor metadata and path identity.
It does not hash UI settings, clipboard, undo buffers or runtime activity counters.
Actual common-properties GUI Apply/Cancel and all nine resize anchors have live
evidence in [LIVE_RESIZE_CLIPBOARD_20261101](LIVE_RESIZE_CLIPBOARD_20261101.md).
