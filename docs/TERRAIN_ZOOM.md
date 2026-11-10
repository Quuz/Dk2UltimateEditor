# Original terrain images and two-dimensional zoom

Implemented 2026-10-07. This closes terrain-resource rendering, source zoom selection and terrain visibility, not the remaining camera/region/view-flag or whole-editor parity requirements.

## Source correspondence

`Map_Display.c` / `Map_Setup.c` use World, Rooms, Gates and Walls image lists. `Misc_CreatePlayerImageList` scales both the room BMP and owner frame before `SRCPAINT` composition. All 11 World / 23 active Rooms / six Gates / fourteen Walls entries are mapped independently of editing IDs; the commented Crypt row is excluded. There are 53 distinct terrain BMPs plus six owner frames (932–937). Thirty terrain images already embedded for fullscreen hover are reused, with 29 additional original resources embedded directly from the preserved source paths. No source bitmap, original binary or game map is replaced.

Hero rooms have no owner frame; owners 2–7 combine the scaled room and frame using bitwise RGB OR, not alpha blending or an approximate border. WPF decodes immutable original pixels; opaque GDI COLORONCOLOR scaling matches the original `LoadImage` output. GDI DCs, selected objects and bitmap handles are restored/released on every path. Frozen sized images use a 256-entry / 64 MiB LRU; raw resources are separately cached. Unknown terrain projects to a read-only rock fallback, invalid owners to owner 2 and invalid variants to variant 1, without normalizing any imported editing bytes.

`Windows_ZoomButton.c` supplies all eleven square presets: 10, 16, 20, 32, 40, 48, 60, 80, 96, 100 and 128. The User entry restores separately configured width/height; the source default is 64×64. A real EN/RU custom-size dialog stages both integer dimensions and rejects invalid drafts without partial application. Bounds 2–512 are an explicit managed safety extension, not a source UI range claim. Existing multiplicative buttons remain additional conveniences. Arbitrary nonpreset button sizes get a separate current-size entry rather than impersonating a source preset.

## Read-only geometry and preferences

Canvas extents, culling, mouse/captured-drag coordinates, selection, previews, object glyphs/counts, region rectangles, patrol links/self-loops, property navigation, minimap viewport/navigation and fullscreen centring use CellWidth/CellHeight independently. The compatibility CellSize API remains square-only. Changing zoom cancels active drawing/region geometry gestures but retains ordinary selection and the internal copy buffer. Logical top-left scroll position is retained subject to normal viewport bounds.

Terrain visibility paints the background black while leaving independent object layers available. Actual shell settings restore/capture current dimensions, User dimensions and terrain visibility. Invalid/nonfinite persisted dimensions fall back independently to 64; valid other dimensions survive. Translation retains view state. Closed/replaced-map custom dialogs cannot apply stale callbacks. None of these operations creates editing history or writes map records.

The existing overview smoke explicitly sets its original 12-pixel fixture zoom so its small test viewport remains meaningful after the source startup default changes to 64. Its navigation/viewport assertions are retained.

## Focused evidence

Core checks independently parse the comment-stripped source catalogue, join all 256 terrain IDs × nine owner values × four variants, compare all source zoom presets and malformed settings, and exercise nonsquare object/special/door-lock geometry across source and boundary sizes.

Windows checks independently call `LoadImageW(LR_LOADFROMFILE)` on the original source BMPs, extract GDI pixels, scale frames separately and compute OR. **2688 byte-exact comparisons** cover every active catalogue entry, every room owner, all eleven presets and three nonsquare/extreme sizes. They check frozen-cache reuse and GDI handle lifetime. Debug and Release focused checks pass.

Four actual in-process EN/RU × native/project cases exercise the twelve-entry preset/User control, all source extents, invalid/valid custom dialog inputs, every cell's nonsquare pointer coordinates, bounds, scroll preservation, overview axes, independent terrain/entity visibility at actual object pixels, gesture cancellation, language retention, JSON and actual MainWindow settings, document replacement and stale-dialog rejection. Pixel sampling deliberately returns scrolling to the origin because RenderTargetBitmap includes the scroll-content visual offset; separate earlier assertions retain the nonsquare scrolled viewport tests. The fixture explicitly creates a valid floor object and checks its non-solid projection. Original strict map/history/selection/copy assertions remain.

All four EN/RU native/project renders in `Test/terrain-zoom-focused-20261007` are inspected; original image details and owner frames are visible and the wrapping toolbar is readable. These are offscreen WPF integration/pixel tests, not historical executable, physical mouse, hardware/DPI or gameplay acceptance.

## Full acceptance

All stage gates are terminal. Debug and Release solution builds have zero warnings/errors. Full Debug has **75 Core groups / zero failures**, exit 0 (`Test/terrain-zoom-debug-20261007.json`). Both complete loaded WPF configurations have **221 passing cases each**, `WINDOWS UI SMOKE PASSED`, exit 0 (`Test/terrain-zoom-ui-debug-20261007.log`, `Test/terrain-zoom-ui-release-20261007.log`); four loaded-map cases include Alcatraz and Ancients in both languages. Release expanded corpus has **1724 passes / 94 failed checks**, exit 1 (`Test/expanded-corpus-terrain-zoom-20261007.json`): 75 synthetic, 560 native round trips, 530 supported-text and 559 original-policy text passes. The complete ordered failure array exactly matches the previous main-map physical-display checkpoint; the corpus is still not green. Process-local test temporary directories remain on D:. Original Source/Binaries diffs and tracked whitespace checks are clean.

Initial test corrections are retained in the work: dialog controls use their actual Window namescope; Western ANSI is explicitly registered for the independent source resource reader; the placed-object fixture now has a proven valid floor; document replacement is separately required to clear selection before restoring the ordinary selection for the final read-only view comparison. No production pixel mismatch or GDI leak was hidden by weakening the independent LoadImage/OR comparison.

Camera markers, source region labels/expanded gates, other view flags, broader catalogues/semantics and historical/game acceptance remain required by the full objective. Their next independent source requirements are recorded in [overlay scope](MAP_OVERLAY_SOURCE_SCOPE.md), not counted as implementation by this stage.
