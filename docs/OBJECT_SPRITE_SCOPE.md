# Original object images: next fidelity stage

The subsequent [main-map display audit/implementation](MAP_OBJECT_DISPLAY.md) establishes that the source main canvas uses geometric object marks and creature counts, not chooser bitmap sprites. Do not turn the old shorthand "canvas sprites" into a new parity requirement. Original terrain, region and camera display fidelity still requires separate source verification.

Historical source audit. The later [original-image chooser/manager stage](OBJECT_IMAGES.md) implements the object-template picker and manager-row subset; refer to that document for its current verification state. This audit alone is not implementation or acceptance evidence.

The original `Things_SelectInit` (`Source/Sources/Things/Things_Selection.c`) provides a three-group creature tree (16 ordinary creatures, 13 elites, 16 heroes), and separate image/name lists for 8 doors, 14 traps, 116 objects and 13 special items. Creature/door/trap/special lists request 32 × 32 images; the object picker requests 64 × 64. Band selection excludes elites in this particular source tree; do not blindly reuse unrestricted creature choices there.

`Datas_Things.c` stores each item's game ID, variable ID, bitmap resource ID and name independently. `LegacyCompilerRules.Element.Icon` already retains the resource ID, so the modern UI must not infer image identity from catalogue order or from the game ID. Elites deliberately share ordinary creature bitmaps. The pre-1.70 and 1.70 special-item lists share bitmap IDs but change the meaning/name of game ID 0x7F. `ValueCatalogues.GameVersion` already distinguishes these lists.

On 2026-10-07, a read-only full inventory joined all eight catalogues to `Source/Resources/Editor.rc` using its Western ANSI encoding and verified that every referenced bitmap path exists. Counts: 16 / 13 / 16 / 8 / 14 / 116 / 13 / 13 entries; zero missing resource mappings or files in each catalogue. There is no need to invent replacement artwork or download game assets for these pickers.

`Misc_CreateImageList` (`Source/Sources/Misc/Misc_Images.c:30`) uses `LoadImage` at the requested size and `ImageList_Add` with no mask. Do not infer transparent-background editing or player recolouring from that ordinary-picker path. `Misc_CreatePlayerImageList` is a separate player-background OR-composition routine used by other displays.

The source object manager uses category headers (`CrIcons`, `DtIcons`, `ObIcons`) followed by item images; `Things_ManagerGetImage` resolves physical category plus ID. Its ordinary-object rows use the icon list's 32 × 32 presentation even though the object chooser requests 64 × 64. Unknown IDs fall back to a category image, rather than becoming a known item or changing stored data.

Implementation/acceptance still required: preserved-source embedded resources; typed family/ID/version lookup; image-backed chooser and manager rows; grouped creature selection; unknown-ID/category fallback without coercion; full native/project EN/RU editing/draft/lifetime tests; independent mapping/pixel checks and actual rendered visual QA. This does not close canvas sprites, minimap/entity overlays or historical/game acceptance.
