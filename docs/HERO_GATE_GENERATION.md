# Hero gate generation

Implemented in `Core/Editing/HeroGateGeneration.cs` and `App/Editing/EditorWorkspace.Gates.cs`; open **Generate hero gates** from the map-region manager. English/Russian labels switch live without discarding entered templates or a valid preview. Changing map data or options clears the preview and confirmation.

## Original evidence

The reference is `Source/Sources/Things/Things_RectEx.c`: `RectEx_GetGatesList` (1213), `RectEx_Generate` (1507), `RectEx_GenerateRawDoFmt` (1819), `RectEx_GenerateInit` (2003), `RectEx_GenerateSave` (2136) and `RectEx_GenerateFlags` (2345). Flags come from `Source/Includes/RectEx.h`. Native loading excludes heart gate type 14 and assigns virtual editor ordinals to other gates in `Files_Game.c`; native destruction events compile to 1-based coordinates, not `bGateID`.

The detector scans in row order, excluding the last row/column. An earlier site's footprint suppresses subsequent covered origins; this is not connected-component grouping. The original in-memory catalogue discriminator is reconstructed from terrain/variant entries, never inferred from the native reserved byte.

| Terrain / variant | Editor type | Footprint | New gate point | New area |
| --- | --- | --- | --- | --- |
| 29 / 1 | 2 | 1 × 1 | Origin | Footprint |
| 33 / 1 | 3 | 2 × 2 | Origin | Expanded 4 × 4 when destroyable; otherwise footprint |
| 37 / 1 | 6 | 3 × 1 | Middle tile | Middle tile |
| 37 / 2 | 7 | 1 × 3 | Middle tile | Middle tile |

Only type 3 can be destroyed. Its twelve perimeter tiles must not contain a gate-catalogue entry or terrain 1, 7, 30, 35 or 254. Existing gates are the first contained region; existing area matching follows the original type-specific rules. A blocked type-3 gate prefers an exact footprint area but retains an earlier expanded match if there is no exact match.

Independent flags select display gates (types 6/7), destroyable gates, release gates, destruction areas, release areas and destruction events. Events require both destruction options. UI group switches preserve checked child options while disabling their effect. A generated event pair per keeper targets player IDs 3 onwards: claim-area arguments 5/player/12 and repeat 1; child destroy-gate action with repeat/one 1. Existing top-level scenarios remain before the new conditions.

Existing-event detection intentionally follows the original broad rule: any top-level claim-area condition with value 12 and a reachable destroy-gate action suffices. It does not validate the target gate or require one pair for every keeper. Increasing the keeper count therefore does not overwrite/complete such an existing scenario automatically.

## Templates and storage

Gate and area IDs use separate 1–255 namespaces. Native editor ordinals exclude heart records but include unrenderable non-heart records; native `bGateID` is not a unique identity. Editor-only names, information and colours are stored in project MAPRECTEX records. Native game records do not have these fields; native templates are visibly disabled. Full native/editor metadata integration remains a parity item, not a feature claimed here.

Templates apply only to added records. `%g/%p` insert gate ID, `%a/%z` insert area ID, case-insensitively; `%%` inserts a percent, unknown specifiers disappear. Referring to an absent identity clears the whole output. The original formatter checks its 64/128-byte buffer limit before each token, leaving four bytes of headroom. Inputs must be Windows-1252 representable; the Russian interface does not silently replace Cyrillic project data. Defaults use English text, while labels are localized. Colours retain original 32-bit COLORREF values (UI: eight hexadecimal digits, `00BBGGRR`).

## Transaction and deliberate repairs

Preview never modifies buffers/history. Applying requires confirmation in the UI and a matching document/fingerprint in the core. All primitives and events form one transaction; undo restores every byte and redo restores the generated state. Existing records, tiles and copy buffers are not replaced. Existing root/continuation links are retained; project backlinks are maintained after ACTION offsets shift.

The manager uses the explicit editor caller and counts each added gate, area,
logical binding, condition and action separately while retaining one Undo.
An empty DKM binding differs from a zero native root. See
[source counter rules and remaining nested-draft work](HERO_GATE_GENERATION_COUNTERS.md).

Gate properties now also offer a [private generation child](REGION_GATE_GENERATION.md).
Its confirmed additions remain pending until the parent applies, and outer Cancel
discards them. The existing manager is an independent immediate-apply workflow.

Safety improvements do not reproduce these source defects:

- The boundary-30 literal `0x1E00000000` is corrected to the intended terrain rule.
- Out-of-bounds perimeter reads and truncated footprints fail safely; trailing `%` does not read past the string.
- Event-only repair is allowed even if no new area/gate is required; the old dialog's `NoNeed` check refused this case.
- Malformed shared identities, exhausted IDs, cyclic/dangling owner branches and newly acquired orphan references fail without partial additions. Unrenderable existing records still reserve their IDs.
- An existing action at the end of a top-level chain is retained rather than overwritten by trigger-only tail traversal.

## Automated evidence and limits

Core tests exercise all **40 valid flag combinations × 5 keeper counts × 2 formats (400 cases)**, independently expected counts/geometry, all four types, source scan/coverage rules, blocking perimeter catalogues, area fallback, native ordinal versus gate-ID semantics and reserved bytes. They check exact claim/destruction arguments, project backlinks, 1-based compiled coordinates, template aliases/colours/truncation/encoding, event-only repair, appending existing conditions, repeated no-op generation, source immutability, lossless save/reopen/re-export, one undo/redo, stale/foreign previews, exhaustion, malformed record ID reservation, orphan-reference refusal and rollback after primitives have already been added.

Four in-process WPF cases (EN/RU × native/project) exercise the actual controls: manager navigation, master dependencies, remembered options, keeper counts, native template note, invalid project input, read-only preview, required confirmation, live translation, stale-input/map invalidation, application, repeat generation, exact undo/redo, retained selection/copy buffers and document close.

These are automated data/control checks, not acceptance in the original GUI or Dungeon Keeper 2. Historical visual matching, game gate destruction/release behavior, comprehensive native/editor metadata integration and malformed-map workflow polish remain unverified. The full editing-parity goal stays open.

## GateGenerationRequests-20261017

Current confirmation no longer uses a checkbox. Workspace Apply and child
Keep show the shared source Yes/No request; capacity failure has a separate
OK notice, three variants and independent remember/reset. See current
[request behavior and evidence](GATE_GENERATION_REQUESTS.md). Earlier WPF
results bind their own dated builds; the current suite was compiled only.
