# Original-image object selection

The later [property/template/band image stage](PROPERTY_IMAGES.md) extends these images to selected physical type fields, Add templates and band controls. Its complete loaded UI checkpoint passes 212 cases per configuration; the counts below describe this earlier 208-case checkpoint. Canvas sprites and other catalogue displays remain separate.

The single-placement, batch-addition and masked-modification template windows now show the original bitmap alongside each named type and provide a separate image chooser. The object manager has a 32-pixel image column resolved from the actual physical category and type ID. These are original assets, not generated substitute art.

## Source and mapping

[The source audit](OBJECT_SPRITE_SCOPE.md) identifies eight original lists and the independent resource IDs in `Datas_Things.c`. `ObjectImages.props` preserves 183 numeric RC identities through 179 embedded physical bitmap files: resources 1214/1215 alias 1213, 1306 aliases 1300 and 1412 aliases 1400. Elites share ordinary-creature images without changing their distinct game IDs. Runtime mapping uses `LegacyCompilerRules.Element.Icon`, never list indices or game IDs as resource IDs.

The chooser keeps original catalogue order, separate ordinary/elite/hero creature groups, and named EN/RU search. It requests 64 × 64 ordinary-object images and 32 × 32 other families, as the source chooser does. Dropdowns and manager rows request 32 × 32. Cached images are frozen, sized once and independent of a document. Source picker images have no transparency mask or player-background recolouring; those are separate source display operations.

The pre-1.70 and 1.70 special-item catalogues are selected from the document version. ID 127 is Kill Enemy Imps before 1.70 and Kill a Creature in 1.70, with the same source image. Unknown IDs retain their raw draft and do not select an invented known value; manager image fallback uses the preserved family-header image and does not normalize stored data.

## Editing and lifetime

Selecting a dropdown item updates only its existing numeric type draft. The separate chooser updates that draft only after an explicit Use selected type (or item double-click). Cancel, search, group navigation, window reopening and language changes do not edit map bytes or create history. Parent close, source edits and document replacement revoke an open chooser. The caller's document guard and captured family are rechecked before a choice is accepted. Preview/application still follow the existing full-template, single-painting or masked-modification transaction; adding pictures does not bypass scenario review or confirmations.

Language refresh initially exposed lost tree selection: selection must be restored after the leaf joins the live tree, not during its detached initializer. This was corrected and covered by actual controls.

## Verification state

`ObjectImagesSmoke.cs` independently reads the preserved C catalogues and CP1252 RC paths, verifies all 209 entries across the eight lists, and compares the embedded image pixels with the original physical files. It checks sizes/cache/frozen images, unknown IDs, all five actual picker families, creature grouping, selected elite IDs, version-dependent names, numeric draft synchronization, search, live translation, cancellation, parent/map lifetime and unchanged bytes/history/copy buffer. Actual manager fixtures additionally assert the live image column, all five physical categories, exact cached identities and 32-pixel sizing during their full sorting/removal/modification/lifetime workflows.

All final processes are terminal. Both Debug and Release solution builds have zero warnings/errors. Complete Debug Core passes **73 groups / zero failures** (`Test/object-images-debug-20261007.json`). Both complete loaded WPF configurations pass **208 cases each**, exit 0 (`Test/object-images-ui-debug-20261007.log`, `Test/object-images-ui-release-20261007.log`), including four new image cases and four loaded Alcatraz/Ancients cases per configuration. The Release 578-map corpus exits 1 with **1722 passes / 94 failed checks** (`Test/expanded-corpus-object-images-20261007.json`): 73 synthetic / 560 native / 530 supported-text / 559 original-policy passes. Its complete ordered failure array exactly equals the manager-session baseline; the existing refusals are neither suppressed nor counted as accepted maps. All test temporary roots are process-local on D:. Source/Binaries diffs and whitespace checks remain clean.

Actual EN/RU object-picker renders under `Test/object-images-final-ui-20261007/`, manager renders under `Test/object-images-manager-final-ui-20261007/` and parent single-placement renders under `Test/object-images-parent-ui-20261007/` are inspected. An additional four actual painting/brush cases pass on the final Debug binary. Offscreen rendered controls do not establish hardware/DPI or historical-GUI/game acceptance. Band-member/other property pickers, canvas sprites, palette/world assets and remaining named catalogue/semantic behavior remain separate open work. This is not whole-editor parity.
