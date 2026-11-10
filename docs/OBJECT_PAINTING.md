# Single-object painting and live object brush

The normal **Add** command now routes the five placed-object categories to a dedicated EN/RU single-placement window. Other record categories retain their existing insertion workflow. **Use as object brush** configures the new canvas tool; each click performs the same single-placement operation, not a terrain gesture or a manager batch.

## Original-source behavior

Inspected `Source/Sources/Things/Things_Display.c:50–207` (`Things_Paint`), `Things_Functions.c:287–319` (remove-at-position), `Things_Functions.c:508–737` (terrain permissions), and `Source/Sources/Map/Map_Mouse.c:213–224` (nonzero permission check before painting).

- Creature painting always creates another creature, including on an occupied cell.
- Doors, traps, ordinary objects and special items update the **first object of the same category** at the chosen position. Identity, physical order and scenario root remain. Only type, resolved owner, hero-option bits and union data change; health, gold held, creature flags, objective, area and enemy retain their previous values. The multi-edit normalization pass is not used.
- Repainting a door with matching type and resolved owner toggles any nonzero lock value to zero, or zero to one. This also applies to barricades; the manager's barricade normalization is deliberately not used.
- Only when no same-category entry exists does creation remove **every opposite-category entry** at the position: door/trap or ordinary/special object. Entries in other cells and categories are untouched. Imported coexistence is retained during same-category updates, matching the source's early-update path.
- Fresh nonhero templates clear hero options, objective, area and enemy. Fresh project identities remain zero until an Action Point is attached, as in the source; replaced objects' IDs and script bindings are not copied.
- Permission checks use the raw template owner: owner zero bypasses territory checks before the terrain owner is resolved. Solid/protected terrain is forbidden. Source enemy and fatal-water/lava return values are warnings, not hard refusals. Live brush status displays these warnings.

The existing verified serializer preflight and unknown-definition/layout guards remain. Negative/outside coordinates are safely refused instead of copying the source's invalid-pointer behavior. Historical Things versions require explicit Things-only upgrade; its consent is not transferred to another document.

## Preview, scenarios and transactions

The window shows create/update/toggle behavior, all-opposite removal count, scenario-bound removal count, resulting union value and terrain warnings. Its confirmation applies to every listed replacement. Shared family templates retain numeric IDs and unknown bits, named EN/RU choices and independent drafts; noncreature hero-option bits are available for the source update operation. Batch-only policies, random geometry and terrain overrides are hidden.

The live brush refuses a replacement involving scenario bindings and asks for review through the single-placement window. This does not remove the original capability: the confirmed window can perform it. Owned obsolete project bindings are removed while scenario events are retained. Ambiguous shared identities are refused, not arbitrarily reassigned. Same-category updates keep existing bindings without a detach/rebind operation.

Fingerprint-bound immutable plans prepare all buffers before one transaction. They reuse the established insertion/modification serializers but **not** their differing placement/collision/normalization rules. Fresh insertion removes the complete approved opposite list; same-category modification bypasses batch collision review because an imported opposite-category entry must not be deleted. The generic ApplyObjectPainting API retains redo and emits no notification for byte-identical updates. Actual editor commands use ApplyEditorObjectPainting and retain one accepted Undo/count even when equal; see the current caller section below. Undo/Redo restores exact bytes, including unrelated collections and scenario files.

Document changes invalidate previews. Language changes retain inputs, preview and confirmation. Pending generic property commits are checked before applying. Closing/replacing the map or window revokes its mutation callback. The canvas click operation does not paint terrain, accumulate a drag stroke, display a terrain-heart footprint, interfere with region picking, or replace the map selection/copy buffer.

## Verification scope

The focused Core group covers both formats, all five categories, automatic/hero/neutral owners, terrain/border/enemy/fatal paths, same-cell creatures, first same-category updates, retained payloads, duplicate opposite removals, imported coexistence, stable/fresh identities, retained events, repeated barricade lock toggles, one-step exact Undo/Redo, saved reopening and project compilation, no-op redo retention, stale/foreign plans, and explicit Things upgrades for versions 1–5. Native corpus checks additionally toggle a verified eligible existing door in memory, compare unrelated bytes and undo; locked/missing-door cases do not claim an edit.

Four actual in-process WPF cases exercise EN/RU × native/project: shared template inputs, confirmed all-opposite replacement, lock-toggle preview/application, solid-terrain refusal, live translation, brush activation, the same canvas operation as mouse-down, creature stacking, script-bound brush refusal and confirmed replacement, retained events, copy/selection invariants, region-pick guarding and source lifetime. Rendered layouts are inspected separately from hardware/native-dialog/game acceptance.

Final gates are terminal. Both complete solution builds have zero warnings/errors. Full Debug Core has **73 groups / zero failures**, exit 0 (`Test/object-painting-final-debug-20261007.json`). Both complete loaded WPF configurations have **200 passing cases each**, exit 0 (`Test/object-painting-ui-debug-temp-fixed-20261007.log`, `Test/object-painting-ui-release-20261007.log`), including Alcatraz/Ancients. Final rendered EN/RU layouts are inspected under `Test/painting-ui-temp-fixed-20261007/`. The terminal Release corpus has **1722 passes / 94 failed checks**, exit 1, with 560 native, 530 supported-text and 559 original-policy passes (`Test/expanded-corpus-object-painting-temp-fixed-20261007.json`). Its complete ordered failure array exactly equals the previous masked-modification baseline. Source/Binaries and whitespace checks remain clean; installed inputs are read-only.

The initial full Debug UI run stopped on insufficient C: temporary space. The first corpus therefore had 1685 passes / 131 failures, with exactly 37 additional disk-space failures; this diagnostic report is retained and is **not** the accepted checkpoint. Complete corrected runs use process-local TEMP/TMP under the workspace on D:, without changing user-global settings or deleting user files. An overlapping build also encountered a locked validation apphost; the final Debug build was rerun after that process ended and is clean. Earlier reports/renderings remain historical or diagnostic, not final acceptance artifacts.

Manager-session preference retention is implemented by the later [session stage](OBJECT_MANAGER_SESSION.md), with separate shared manager templates and single-record drafts. Sprite selection, remaining malformed-reference fixtures, original-GUI/hardware/DPI comparison and actual game acceptance are still open; this feature does not establish complete editing parity.


## Текущие команды кисти объектов — 14 октября 2026

Кисть и одиночное размещение используют ApplyEditorObjectPainting: счётчик
1 + число удалённых противоположных объектов, один шаг Undo, принятие равного
обновления и одна статистика использования. Базовый ApplyObjectPainting
сохраняет прежние правила равного результата. Подробности и ручные шаги:
[EDITOR_OBJECT_PAINTING_CALLERS](EDITOR_OBJECT_PAINTING_CALLERS.md).

## ObjectBrushCommands-20261018

[Пять прямых команд и private template Cancel](OBJECT_BRUSH_COMMANDS.md).
Workspace single-placement drafts публикуются только после успешного Apply
или UseBrush. Cancel сохраняет ранее принятые шаблоны. Core painting не менялась.
