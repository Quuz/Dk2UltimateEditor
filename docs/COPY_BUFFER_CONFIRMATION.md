# Original copy-buffer replacement consent

Implemented 2026-10-07; lifetime corrected 2026-10-17. Current state: [COPY_CONFIRMATION_LIFETIME](COPY_CONFIRMATION_LIFETIME.md). Earlier WPF results below bind their dated build; the current WPF suite was compiled only. This does not prove full original-editor/game acceptance.

## Source behavior and actual command routing

`Source/Sources/Map/Map_Select.c:1299` returns Yes without a question when the internal `MapCopy.Map` is empty. Otherwise `Select_CopyRequest` uses REQUEST_COPY, MBF_AUTOREPLY and affirmative default gadget 1. `Select_Copy` asks before copying or cutting. `Select_Load(true)` parses the DKZ first and asks before releasing the previous buffer; direct insertion (`false`) never replaces it. `Misc_Requests.c:843` remembers suppression only for the affirmative default reply. This is the editor's internal selection buffer, not the Windows clipboard.

Actual Copy, Cut and rotated-copy menus and Ctrl+C/Ctrl+X now use `RequestCopy`. Actual DKZ buffer import parses first and uses `RequestSelectionFile` / `RequestLoadSelectionIntoBuffer`. An existing buffer prompts with EN/RU operation, dimensions, object and scenario-record counts. Empty-buffer copying and the explicitly disabled preference proceed without asking. Direct DKZ insertion remains independent of replacement consent. Invalid files leave the old buffer intact.

The real modal window has default Yes and Escape/close/No keeping the buffer, as in the original. A checked “Do not ask again (automatically replace)” only applies after an accepted, current, successful operation, never No, cancellation, stale authority or failed execution. The menu reflects the effective state. Only the explicit menu preference persists in JSON; a remembered dialog reply lasts for the current session. Missing older settings default to asking. Eleven resource keys have exact EN/RU parity; live language changes retain the checkbox draft.

Trusted nonmodal `Copy`, `Cut`, `CopyRotatedSelection` and `LoadSelectionIntoBuffer` APIs remain for explicitly scripted/in-process workflows. They are not the actual human-command route. Tests provide explicit decisions through `CopyBufferPrompt`; null uses the actual modal window, not implicit test approval. This distinction avoids blocking scripted tests while retaining the real UI question.

## Cancellation and authority

Replacement captures document, selection, buffer identity, serialized old/incoming buffer bytes and a monotonic workspace context. Selection/document/buffer changes, edit followed by Undo, document close/unload, reentrant replacement and mutable buffer-byte changes invalidate consent. Generic property commits explicitly invalidate the context even while their normal document refresh handler is temporarily disconnected. Permanent shell closure disables subsequent human copy/import commands, including direct file insertion.

Copy constructs its candidate only after consent. Cut captures pre-cut data, performs the existing atomic cut, then publishes its buffer: a cut failure cannot publish a replacement. A declined cut makes no tile/object/script/history edit. Existing property-table drafts are committed before the copy question; that prior commit is not rolled back by declining a later Copy/Cut. No accepted prompt grants authority to modify unrelated map data or remove scenario roots.

## Evidence and visual checks

Four actual EN/RU × native/project cases exercise real menu events; empty-buffer bypass; all three declined commands; real No and Yes/remember buttons; exact buffer/map/history retention; accepted copy/cut with one Undo; selection/document changes, edit+Undo, reentrant buffer replacement, old/incoming mutable tile guards and closed-shell authority; valid DKZ No/Yes and exact reserialization; direct insertion without a question and exact Undo; malformed parse retention; suppression/re-enabling; actual shell capture and JSON restoration.

Initial PNGs revealed dark summary text from derived-window style inheritance and a cropped offscreen rendering. The window and panel now explicitly reference existing theme foreground/background brushes. The renderer maps local content bounds instead of retaining ancestor offsets; both horizontal and vertical button bounds and summary brush are asserted. All four final images under `Test/copy-confirmation-render-final-20261007` are inspected: question, counts, checkbox and complete EN/RU buttons are readable. This is offscreen layout verification, not a hardware/Escape/monitor/DPI or historical GUI claim.

All stage sessions are terminal. Debug/Release builds have zero warnings/errors. Focused checks pass in both configurations. Full Debug Core has 76 groups / zero failures (`Test/copy-confirmation-debug-20261007.json`), exit 0; no Core production code changed. Release corpus is terminal at 1725 passes / 94 failed checks (`Test/expanded-corpus-copy-confirmation-20261007.json`), exit 1: 560 native, 530 supported-text and 559 original-policy checks pass. The entire ordered failure array exactly equals the map-overlay checkpoint. Both final full loaded WPF suites have **230 passing cases each**, `WINDOWS UI SMOKE PASSED`, exit 0 (`Test/copy-confirmation-final-ui-debug-20261007.log`, `Test/copy-confirmation-final-ui-release-20261007.log`), including strengthened horizontal/vertical render-bound assertions and loaded Alcatraz/Ancients. Source/Binaries diffs and tracked whitespace checks are clean; installed input maps are not edited by this workflow. The corpus remains failing, not whole-collection acceptance.

## Remaining objective

Unused compiler-event cleanup, other semantic/catalogue controls, broader native/editor variants and original/game acceptance remain open. See [remaining editing workflows](REMAINING_EDITING_WORKFLOWS.md), [cleanup source scope](UNUSED_EVENT_SOURCE_SCOPE.md) and [full parity ledger](FEATURE_PARITY.md).
