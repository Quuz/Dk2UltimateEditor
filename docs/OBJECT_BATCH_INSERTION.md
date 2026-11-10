# Object manager: full templates and atomic batch insertion

This extends the read-only [placement stage](OBJECT_BATCH_PLACEMENT.md) into a visible editing workflow. In the Objects tab, **Batch add objects** opens an EN/RU window for all five original categories. Whole-manager parity is not claimed: bulk modification/removal, sorting, sprite selection and historical/game acceptance remain separate requirements.

## Source and editable properties

Reference: `Editor.c` lines 173–179, `Things.h::MAPTHING`, `Things_Manager.c::Things_ManagerAdd` passes 1–7, `Things_Selection.c::Things_SelectOptionsProc` / `Things_SelectObjectiveProc`, `Strings.h` and `Compiler_Generate.c` object/door/trap/creature generators. The five independent defaults are not the generic single-record defaults:

| Category | Type ID | Owner | Category integer | Other defaults |
| --- | ---: | ---: | ---: | --- |
| Creature | 1 | 0 automatic | Level 1 | Health 100; behavior 9; objective 19 |
| Door | 6 | 0 automatic | Locked 0 | Zero |
| Trap | 4 | 0 automatic | Shots 4 | Zero |
| Object | 1 | 2 neutral | Gold/spell 0 | Zero |
| Special item | 117 | 2 neutral | 0 | Zero |

The complete immutable template retains type, owner, health, held gold, the signed category integer, hero/creature option masks, objective, area and enemy. Coordinates come from the sampled plan. Unique identities and scenario roots are never template inputs. Named type/owner/level/door-state/spell/objective/area/enemy suggestions coexist with numeric input; unknown values and bits are not normalized during language changes. All eleven named creature/hero bits preserve unrelated bits. Family drafts, including invalid input, and random frequency survive switching categories and reopening the window within the workspace session.

The native serializer chooses C7/40-byte hero, C6/32-byte neutral or C8/36-byte keeper independently for each candidate's effective owner. Heroes store **enemy**, not owner, in `bGlobalPlayer`; standalone band ID is 255. Native casts and inactive gold/spell fields follow the original generator. Project MAPTHING retains full-width fields. Door suggestions include open 0, locked 1 and blueprint 2; traps include blueprint 0 and built 4, with other shot counts still editable. An arbitrary native ID outside the historical special-item range cannot encode a separate special-family tag; the project does retain its explicit family.

## Placement, replacement and confirmation

All three modes, current/numeric selection, source whole-map bounds, frequency 2–32, automatic owner, terrain/enemy/fatal/boundary rules, warning flags and five replacement policies use the verified placement planner. Random positions are sampled once, before validation, in row order. Each preview row exposes add/skip, position, owner, first existing overlap and translated warnings. Locked rows cannot be enabled by either a checkbox or the all-unlocked command. Keep-all skips additions; it does **not** delete existing map objects.

The modern window always requires a built preview and explicit confirmation, even when the original flags would permit bypassing a dialog. Flags retain source default-decision semantics, not a claim of identical historical dialog sequencing. Editing inputs or candidate decisions clears confirmation; document edits invalidate the preview. Translation retains the sampled plan and selected decisions. A pending generic property edit commits first and makes its older preview stale. Cancel, close, document replacement and workspace unload revoke application authority and release event subscriptions. Selection and the copy buffer are retained.

Source defects deliberately repaired: the original random slider changes displayed frequency but `Things_ManagerAddSave` never saves it; both numeric and slider input now affect the actual sampler and session draft. The creature-options dialog displays its immediate-death 0x04 checkbox but the ordinary-creature branch omits loading/saving it; the named modern control edits that bit. The source also clears other option bits and commits options/objectives on Cancel; this window retains unknown bits and never edits a map on Cancel. The location bit 0x40 remains available as a named/raw template value even though that original checkbox was disabled.

## Transaction and format integrity

`ApplyObjectBatchInsertion` validates the document fingerprint and explicit selections, prepares aggregate buffers, then assigns them inside one `Edit("Batch add objects")`. It never repeatedly snapshots the growing collection per object. Unrelated records, padding, collection gaps/tails and scenario events remain byte-exact. Only the first selected source-priority overlap is removed; collocated duplicates are not all deleted. Native new roots are zero. Project identities reserve existing/nested IDs and orphan bindings; old identities are not reused. Approved project replacements detach only their owned scope links, retaining events and aliases. Shared ambiguous identities fail without changes rather than stripping another object's binding.

The history tree records one aggregate batch entry, not the original pointer-based per-object child entries. Undo/Redo still restores the complete batch as one step; this avoids quadratic snapshots for large insertions. General per-object history-tree parity is not inferred.

Historical Things versions 1–5 require an explicit checkbox to upgrade **only Things** to version 6 inside the same undo operation. No-op selection does not upgrade, emit changes or consume redo. Unknown identity-bearing sections require the existing explicit whole-project upgrade/repair; unknown layouts are not guessed. Preflight enforces native count (1,000,000), file size and full serialized project size (64 MiB) before committing. Another final fingerprint check catches a caller enumerable that changes the document. Nested transaction failure restores every file and history exactly.

## Acceptance

`ObjectBatchInsertionChecks` independently encodes literal Things.h/Game.h byte layouts; it does not use production field offsets to compute expected new records. It covers every category, seven owners, gold IDs 1–3/spell 4/inactive payloads, full-width and byte-cast values, owner-dependent creature signatures, automatic owners, read-only planning, random decisions without resampling, source-priority replacement, roots/events, ambiguous identities, old Things versions 1–6, native opaque tails, saved/reopened edits, independent direct-native versus project compilation, nested rollback, no-op/stale/foreign/locked guards, 15,876-object batches and 1024² count/project-size refusals.

Four EN/RU × native/project actual WPF cases cover all five template defaults, named bits and unknown-bit retention, named objectives/owners, invalid drafts, buttons and per-row checkbox binding, selection/boundary locks, all/none decisions, slider/numeric frequency, live translation without resampling, atomic Apply/Undo, pending generic commits, historical upgrade confirmation/undo, external edits and closed-window/source lifetime. These are in-process offscreen tests, not hardware input or gameplay tests. Rendered EN/RU previews are inspected; the initial missing foreground and clipped columns are corrected.

Core terminal results: final full Debug has **70 passing groups / zero failures** (`Test/object-insertion-blueprint-debug-20261007.json`, exit 0). Release expanded corpus has **1719 passes / 94 failed checks**, including 560 native / 530 supported-text / 559 original-policy checks (`Test/expanded-corpus-object-insertion-20261007.json`, exit 1). The **complete ordered failure array exactly matches** the preceding placement checkpoint. This does not make the corpus green. Final solution Debug/Release builds have zero warnings/errors; focused insertion Core/WPF cases pass in both. Final complete WPF gates have **188 passing cases in each configuration**, `WINDOWS UI SMOKE PASSED`, exit 0 (`Test/object-insertion-ui-blueprint-debug-20261007.log` / `Test/object-insertion-ui-blueprint-release-20261007.log`), including Alcatraz/Ancients and the pending-generic/historical-upgrade additions. All final sessions are terminal. A prior concurrent Debug rebuild failed because its DLL was held by the live smoke process; after that process completed the final build passed cleanly. No live test was killed/restarted to hide that failure. The earlier planning-only 69/184 checkpoint is historical.

Original Source/Binaries are unchanged, and installed map inputs are read-only. Remaining acceptance includes the original GUI/PRNG, hardware/DPI, playable game behavior and the rest of the object manager. Session drafts are not claimed as persisted JSON preferences or a sprite picker.

Source revision verified on 2026-10-07: read-only `git ls-remote origin HEAD` returned `122d2ee3ba65c87f9551cb13bd4ba1872b6edcba`, exactly the local original commit. No upstream source update was needed. The initial sandboxed network request failed; the approved read-only request succeeded.
