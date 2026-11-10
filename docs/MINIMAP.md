# Original-colour minimap and viewport navigation

The Map tab now contains a left-hand minimap, shown by default. Its toolbar checkbox hides/shows it. It starts at the original 144 × 144 size with proportions retained; the new size picker also offers 96, 216 and 288 pixels. These presets are modern UI conveniences, not a claim that the original had the same picker. The Keep proportions checkbox switches between letterboxing and a stretched square. Labels/tooltips are English and Russian and switching language retains view settings.

## Original colour evidence

`Source/Sources/Thumbnail/Thumbnail_Functions.c: Thumbnail_Generate` chooses WorldCols, RoomsCols, THUMB_HEROPORTAL or WallsCols using the original in-memory catalogue discriminator. The native fourth byte is **not** that discriminator. The overview first follows `Compiler_GameMapToEditorMap` and `Map_TranslateID/Ex`: World, Rooms, Gates, then owner-specific Walls. It then uses the original palette from `Datas_Thumbnails.c` and `Thumbnails.h`. These preserved sources are embedded into the core; source files/binaries are not modified.

Coverage is all eleven world entries, 23 room/bridge entries × seven players, six gate variants and fourteen wall/path entries. This includes brighter hearts, the neutral portal colour, both bridge variants, owned paths/walls and the locked-limit terrain. The commented-out crypt is not promoted to a verified room. Palette colours retain Windows COLORREF channel order before explicit conversion to top-down BGRA; the view uses nearest-neighbour bitmap scaling and one stored pixel per map cell. This is the original terrain overview, not a sprite rendering or object layer.

The source native converter displays invalid owners as Neutral, invalid variants as variant 1 and unknown terrain as Rock. Those are **read-only display fallbacks** here: stored tile bytes, unknown values and the reserved byte remain untouched. No normalization is performed by opening or navigating the overview.

## Navigation and lifetime

The frame tracks the actual ScrollViewer viewport, ordinary scrolling, zoom and resize/undo. Left-click centers the visible area on the chosen map position; captured left-drag continuously scrolls. Initial clicks in the black letterbox are ignored, while a drag beyond an edge clamps to that edge. Fully visible maps do not produce navigation. Painting, selection, copy buffers, pending geometry picks and history are not commands of the overview.

The modern viewport supports partial visible cells and pixel-based ScrollViewer offsets rather than copying historical integer scrollbar rounding or cursor edge off-by-one artefacts. The white frame has a black outline for contrast on light and dark tiles. Only document changes rebuild the cached bitmap; scrolling, zoom, language and view settings reuse it. Switching/closing documents removes the old subscription and raster/viewport authority. Resize obtains new dimensions rather than stretching stale pixels.

## Verification

`MapOverviewChecks` independently reads the preserved tile/colour tables and aliases, checks their exact source counts and verifies original conversion fallback statements. Every terrain byte × every owner byte × five representative variants (0, 1, 2, 3, 255), including unrelated reserved bytes, is compared against a separate direct-loop source oracle. Native/project fixtures cover every catalogue/owner, top-down pixel/channel order, saved/reopened colours, resize/undo and exact read-only file/history/event state, including retained redo.

`OverviewSmoke` adds four EN/RU × native/project cases using actual workspace controls and an actual never-shown Window resource root. They verify the source bitmap, landscape/portrait letterboxes, viewport binding, the shared mouse click/drag navigation operation reaching the actual scroll viewer, ordinary scroll, zoom caching, all size buttons, proportions/show-hide controls, live resource translation, terrain redo, resize/undo, document replacement/close, nonfinite/outside input and exact selection/copy-buffer/history invariants. These tests do not simulate hardware mouse capture. Optional `DK2_OVERVIEW_UI_ARTIFACTS` renders four actual-control PNGs. Visual inspection found a clipped Russian proportions label; it now wraps without truncation.

Focused Debug core and all four WPF cases pass. The completed full checkpoint has zero-warning/error Debug/Release solution builds, **61 synthetic groups / 156 WPF cases** passing in both configurations, including loaded Alcatraz/Ancients cases. All process handles are terminal. `Test/overview-debug-20261006.json` contains 61 passes and no failures; `Test/expanded-corpus-overview-20261006.json` contains 61 synthetic passes, 560 native / 530 text passes and the unchanged 75 failed checks (exit 1). Full corpus compatibility is not achieved.

## Remaining related scope

The original minimap right-click opens `Windows_Fullmap.c`, a separate fullscreen overview with room icons and animated neutral/trap/door/creature overlays. That workflow is now implemented by the subsequent [fullscreen milestone](FULLSCREEN_OVERVIEW.md), with separate source/control checks; the counts above describe the earlier minimap-only checkpoint. [Thumbnail export/options](MAP_THUMBNAILS.md) are now also implemented separately, with thumbnail/shared-proportion settings persistence. Sprites, other view preference persistence and original-GUI/hardware-mouse/game acceptance remain part of the full editor goal; they are not implied by the earlier minimap-only checks.
