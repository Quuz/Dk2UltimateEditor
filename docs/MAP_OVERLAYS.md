# Original keeper cameras, region labels, gates and paths

Implemented 2026-10-07, following the independently read [overlay source scope](MAP_OVERLAY_SOURCE_SCOPE.md). This stage is not a whole-editor or gameplay completion claim.

## Source projections

The main canvas now uses immutable region/path/camera snapshots refreshed on document assignment, edits and Undo, rather than re-reading complete record collections during every mouse repaint. Regions draw in source Effects → Gates → Areas order, followed by Paths → Cameras; physical object layers and selection retain their existing order.

Project rectangle names, hidden flags, boundary colour and masked 24-bit text colour are retained. Native names are read-only translated category/ID labels, as the source native loader synthesizes names rather than storing editable editor strings in game records. Native gate display IDs are ordinals excluding heart gates; the source consumes heart records into player health, not its gate list. Project gate display always starts at its raw first endpoint as a one-tile point, regardless of a different saved second endpoint. Editing geometry and the original record bytes are not rewritten by these projections.

Region labels are placed in the viewport-clipped rectangle with source three-pixel margins, black text shadow and one-pixel upper-left foreground offset, then a one-pixel boundary at the inclusive last pixel. Text wraps inside the available clipped region. Names/hidden states are not inferred from serialized pointer words. Generic highlighted/ordinary selections remain separate overlays.

Expanded gates follow the original ordered left/right row scan, then up at the resulting left edge and down at the resulting right edge. Matching requires the Gates catalogue terrain/variant slot, ignoring owner. This is not flood fill or a connected-component bounding box. Explicit next-coordinate guards fix the original right/bottom precedence/out-of-bounds bug; invalid origins and unknown/non-Gates terrain never trigger data repair. Both point and expanded extents are captured read-only.

Paths use source integer centre division and integer one-ninth self-loop insets, one-pixel lines and configurable COLORREF, default white. Missing/ambiguous endpoint identities are omitted rather than guessed. Paths can remain visible with area outlines and the entity master off.

Five keeper cameras use actual player IDs 3–7 and original fixed 16×16 ICO resources 20–24. The icon origin uses C-style truncation for `(zoom-16)/2`. Source overlap offsets use the global keeper index, not occupancy rank: all earlier absent/unshown keepers still affect that index. Two-pixel owner-colour ellipses have two-cell radii and the source overlap adjustment. Stored project X=-1 or Y=-1 suppresses the camera rather than drawing its compiler-resolved effective heart position. Verified old-reader, duplicate-ID, unsupported-layout and out-of-map guards are inherited from the actual player geometry resolver. Tiny managed extension zooms may produce an inverted source overlap ellipse; the safe renderer retains the icon but skips invalid negative-radius ellipses.

Original camera ICOs are embedded directly from preserved source paths, decoded at their actual 16×16 frame and cached/frozen. No generated substitute, external download or source asset edit is used.

## View controls and preferences

Six EN/RU overlay menu options correspond to Areas 64, Gates 128, Paths 256, Effects 512, Cameras 1024 and ExpandedGates 131072. Together with the eight physical-object menu options and terrain visibility bit, all fifteen original source view options are available. Category defaults are enabled, expansion and the two physical owner-colour options remain off. The existing patrol checkbox synchronizes with the Paths menu bit; it is not an independent competing flag. Existing modern entity-master hiding remains a documented extra, not an original flag.

The path-colour input exposes RRGGBB, converted explicitly to/from source COLORREF. Invalid input cannot partially apply preferences. Language switching retains an unfinished colour draft and all view flags, including unknown imported bits. Actual MainWindow and JSON settings restore/capture the separate overlay preferences; a missing older settings entry supplies source defaults while existing physical-object/terrain/zoom preferences remain unchanged. Preferences do not modify map files, editing history, ordinary selection or internal copy buffers.

## Evidence

Core independently checks source flag words and the fifteen-option union, defaults, 5625 integer camera/overlap geometries across source/nonsquare/boundary zooms, integer path geometry, all 32 keeper overlap subsets, native/project stored/default cameras, suppressed Y sentinels/out-of-map coordinates, project names/hidden/text-colour masks, gate-point semantics, native heart exclusion, draw ordering and byte/history retention. Twelve thousand random-grid origin checks compare safe ordered edge expansion, including variant/owner changes and map borders. Invalid gate origins remain guarded.

Windows independently loads each original camera with `LoadImageW` and renders `DrawIconEx` onto black, white and a nontrivial colour. All **15 RGB/transparency comparisons** match WPF. Frozen-cache reuse is checked. Each explicit reference DC/DIB/icon is restored/released with successful API results and exact per-iteration GDI handle-count equality after native one-time initialization; WPF render-target caches are outside that native lifetime measurement. The initial whole-process counter assertion conflated retained/initial graphics resources with explicit native handles and was replaced by this narrower, stricter per-reference lifetime audit, not by loosening the pixel comparison.

Four actual EN/RU × native/project WPF cases cover all six actual menu toggles, synchronized patrol controls, RGB/COLORREF conversion and invalid drafts, captured gate expansion, independent rendering of all five overlay categories, exact 16-pixel first-keeper placement on nonsquare canvas, live camera edit/Undo refresh, language/draft/unknown-bit retention, actual shell preferences and closed-document cleanup. Complete byte/history/selection/copy comparisons remain strict. All four final renders in `Test/map-overlay-final-ui-20261007` are inspected: native translated and project saved names, gate bounds, white links/self-loops, five camera icons/ellipses and wrapping EN/RU toolbar are visible/readable.

## Full acceptance

All stage sessions are terminal. Focused Debug/Release Core and WPF checks pass; both builds have zero warnings/errors. Full Debug has **76 Core groups / zero failures**, exit 0 (`Test/map-overlays-debug-20261007.json`). Both complete loaded WPF configurations have **226 passing cases each**, `WINDOWS UI SMOKE PASSED`, exit 0 (`Test/map-overlays-ui-debug-20261007.log`, `Test/map-overlays-ui-release-20261007.log`); four actual loaded-map cases include Alcatraz and Ancients in both languages. Release expanded corpus has **1725 passes / 94 failed checks**, exit 1 (`Test/expanded-corpus-map-overlays-20261007.json`): 76 synthetic, 560 native, 530 supported-text and 559 original-policy text passes. The complete ordered failure array exactly matches terrain/zoom; the corpus is not green. Temporary test roots remain process-local on D:. Source/Binaries diffs and tracked whitespace checks remain clean.

Historical GDI text/ellipse raster, source GUI, physical input/DPI, broader malformed native variants and gameplay acceptance are not established by these offscreen tests. Other source catalogues/semantics, editing workflows and compatibility refusals remain in the full objective. The next independently confirmed functional gaps are [copy-buffer consent and unused event cleanup](REMAINING_EDITING_WORKFLOWS.md), not newly invented completion requirements.

## SourceMenuInventory-20261018

[Все15 флагов доступны также из постоянного меню «Вид»](MAIN_MENU_SOURCE_MAPPING.md),
когда MapToolbar скрыта. Используются прежние flags и Configure*; актуальные
отметки синхронизируются при открытии меню. Повторных render/GUI прогонов не было.
