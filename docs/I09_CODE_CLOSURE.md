# I09: ворота при повороте и отражении

25 октября 2026. **I09 закрыт по коду.** Все12 маршрутов сопоставлены с source
и текущими providers; новые проверки не выявили расхождения в product code.

Поворот/отражение исходного редактора переносит клетки, сохраняя terrain,
owner и variant буквально. Он не меняет горизонтальный gate37 variant1 на
вертикальный variant2. Отдельная запись области ворот не следует за клетками.
Native direct editing сохраняет её stored orientation/ID. Project compilation
использует остающийся на месте anchor и вычисляет результат по текущим клеткам.
Это исходное поведение; семантическое разворачивание ворот не добавлено.

| ID | Маршрут | Source | Результат |
| --- | --- | --- | --- |
| T01 | Direction and shape/axis acceptance | `Source/Sources/Map/Map_Select.c:504` | CW/CCW square mappings and minimum reflection axis retained. Existing square/axis rejection proofs reused; no fallback into clipboard on rejection. |
| T02 | Source literal tile transform | `Source/Sources/Map/Map_Select.c:725` | All four bytes are permuted literally; terrain owner/variant is not rotated semantically. Six source gate catalogue variants retain variant1/2 and raw reserved markers under all four operations. |
| T03 | Physical lists and unchanged non-XY fields | `Source/Sources/Map/Map_Select.c:556` | Five source physical lists move; native HERO/KEEPER/NEUTRAL plus DOOR/TRAP/OBJECT, project MAPTHING0..4. Fields other than XY stay exact. Existing IDs/scopes remain; no orientation rewrite guessed for arbitrary payload bytes. |
| T04 | Independent global records stay stationary | `Source/Sources/Map/Map_Select.c:762` | No MapGates/areas/effects/bands/players/path transform. Native GAMEGATE location/type/orientation/ID remain literal; project MAPRECTEX gate anchor/rectangle stays exact. Global bands/members are not physical things, as independently closed in I08. |
| T05 | Protected tile versus physical movement ordering | `Source/Sources/Map/Map_Select.c:561` | All selected physical objects move before destination tile-lock checks. Protected destinations keep tiles; masked source becomes neutral solid. Existing mask/provenance tests reused without repeat. |
| T06 | Gate footprint and region anchor separation | `Source/Sources/Compiler/Compiler_Generate.c:845` | Project compiler iterates explicit MapGates anchors. Rotated gate terrain does not carry a region with it; default type33/orientation0 applies at a now nonoriented anchor. All four moved-anchor fixtures verify this source behavior. |
| T07 | Derived horizontal orientation | `Source/Sources/Compiler/Compiler_Info.c:402` | For gate37 variant1, south neighbour solid/out-of-map gives0, open gives4. Transform retains variant1; changed neighbour determines fresh compiled value. |
| T08 | Derived vertical orientation and solid predicate | `Source/Sources/Compiler/Compiler_Info.c:391` | For gate37 variant2, east neighbour solid/out-of-map gives6, open gives2. Solid IDs1/2/6/7/9/30/FE match Things_IsSolid. Other verified gate types use existing source fallback33/0; unsupported variant policy remains explicit. |
| T09 | Native raw edit versus project regeneration | `Source/Sources/Compiler/Compiler_Info.c:375` | Raw native transforms preserve independent gate bytes. Project compilation derives them from current tiles/anchors; source-only ID/orientation restored only when bounded baseline/identity/local-geometry checks permit. No arbitrary global facing rotation. |
| T10 | Native-source archive, Undo and Redo eligibility | `Source/Sources/Compiler/Compiler_Generate.c:850` | New actual native-to-DKM save/archive/reopen fixtures retain whole original gate bytes before editing, derive new orientation/ID1 after local transforms, recover originals after Undo, derive again after Redo. Four operations exercised. |
| T11 | Buffered rotation, DKZ and coordinate provenance | `Source/Sources/Map/Map_Select.c:504` | Buffered transforms reuse literal tile/payload mapping and retained provenance. Nonsquare explicit rotated copy is a named modern extension, separate from rejected nonsquare in-place turn. Existing DKCP/local/external/mask proofs reused. |
| T12 | Editor caller/history and workspace routing | `Source/Sources/Map/Map_Select.c:540` | Workspace invokes TransformEditorSelection after pending-field commit; accepted1+K physical removals, one Undo, unchanged activity. Original identities preserved and atomic rollback retained as agreed. Existing TransformRemoval evidence reused. |

## Проверки и границы

Новые4 группы/16 fixtures: четыре преобразования × native six-variant literal
buffers; project oriented neighbours; moved terrain/fixed region anchor;
actual native→DKM save/archive/reopen and Undo/Redo restoration eligibility.
Для gate37/1 solid южнее даёт0, open4; gate37/2 solid восточнее даёт6, open2.
Клетки за границей считаются solid; source solid IDs1/2/6/7/9/30/FE сверены.
Другой поддержанный gate terrain у anchor даёт source default33/0. Native
unknown bytes не получают придуманного нового смысла. Все buffers exact;
неизменённые gate records сохраняют nonzero IDs и ориентации.

Отдельно доказано: local terrain transform отменяет восстановление obsolete
source-only gate bytes при компиляции DKM; Undo возвращает исходную eligibility
и весь оригинальный gate, Redo снова генерирует current orientation иID1.
Исходники/установленные карты не менялись; игровые temp inputs отсутствуют.
Исходные square/axis/1+K и mask/provenance успехи взяты из датированных
TransformRemoval, NativeGateRetention и I08 доказательств без перезапуска.
Рабочий код и runtime bytes не менялись; собран только Validation/Core.
Нет нового full run, запуска renderer/окна, Computer Use или игры.

Evidence: `artifacts/Evidence/GateTransformSemantics-20261025/`:
i09-closure-proof.json, scoped-source-diff.patch, test-build-01/focus-01 logs
и terminal ExitCode, baseline.json, delivery.json. Все provider/input hashes
проверены. Source ZIP остаётся единой поставкой, не входом сборки.

Остались I10 context callbacks и I18 malformed owners/identities; все23 M-группы
сохранены, включая физические transforms, отображение и запуск в игре.
Кодовая сверка не заменяет ручную приёмку. Текущий остаток2I+23M=25.
