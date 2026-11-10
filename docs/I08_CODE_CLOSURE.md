# I08: отряды, выделение и специальные правила вставки

25 октября 2026. **I08 закрыт по коду.** Конечные15 source/caller маршрутов
сопоставлены. Product code не менялся: найденные границы уже реализованы;
добавлены проверки ранее неподтверждённого совместного поведения.

| ID | Маршрут | Исходник | Результат |
| --- | --- | --- | --- |
| S01 | Band member chooser catalogue | `Source/Sources/Things/Things_Selection.c:461` | FROMBAND offers BadCreatures+Heros, excludes BadElites for new choices. Imported elite/unknown identities remain visible and retain literal values; existing HeroBandDisplay evidence covers this distinction. |
| S02 | Member controls and private nested Apply | `Source/Sources/Things/Things_Heros.c:890` | Band member owner is fixed; no spatial XY is edited. Level/health/gold/enemy/options/objective/area are member data, not a placed hero. Existing parent/draft/stale authority retained. |
| S03 | Global band/member versus spatial hero | `Source/Sources/Compiler/Compiler_Generate.c:942` | Global GAMEBAND/MTHBAND and embedded BandHero members are excluded; native GAMEHERO and project MAPTHING creature are spatial. New native/project fixtures retain global bytes and both band/member scenario roots across Copy/DKZ/Cut/Paste. |
| S04 | Included physical categories | `Source/Sources/Map/Map_Select.c:556` | Source Creature/Door/Trap/Object/MagicalObject map to native HERO/KEEPER/NEUTRAL,DOOR,TRAP,OBJECT and project MAPTHING0..4. Magic object IDs75..81 keep their category. Exact full cloned payloads verified on ineligible water, allowing only relocated XY and fresh project uniqueid. |
| S05 | Other excluded categories | `Source/Sources/Map/Map_Select.c:560` | Area/gate/effect rectangles, global bands/members, player/settings and camera/path records are not spatial things. Only reachable owned physical script graphs accompany modern clipboard; independent global roots remain outside. |
| S06 | Special cut background | `Source/Sources/Map/Map_Select.c:1047` | Seven special selected terrains use documented full NewMap FillID; ordinary fill uses normalized selected terrain. Known source low-byte/shift bugs are documented rather than recreated; no load repair. |
| S07 | Protected copy/cut source | `Source/Sources/Map/Map_Select.c:550` | Editor copy excludes locked-edge things; Cut leaves protected tiles/things/scopes. Raw/direct APIs retain their explicit distinction. Existing mask tests cover original data and graphs. |
| S08 | Protected paste destination | `Source/Sources/Map/Map_Select.c:713` | Skip destination tile plus removal/insertion of its physical objects before any source projection. Global records are never destination-removal candidates. |
| S09 | Masked paste source | `Source/Sources/Map/Map_Select.c:1211` | For editable destination remove existing things first, write neutral solid, do not insert masked source objects. Independent mask/provenance is carried through DKCP. |
| S10 | Clipping and insertion origin | `Source/Sources/Map/Map_Select.c:1175` | Buffer dimensions govern paste; destination selection supplies top-left rather than cropping to selection size. Right/bottom map clip retained. Cursor fallback without a rectangle is an explicit modern extension. |
| S11 | No single-object or cursor-footprint validation during paste | `Source/Sources/Map/Map_Select.c:615` | Paste copies literals without Things_CanPaint or Map_CanPaint; it does not coerce special terrain to heart/portal/gate minimums. New five-category water fixtures and fourteen format/terrain cases prove both boundaries. |
| S12 | XY, unique identities and owned scripts | `Source/Sources/Map/Map_Select.c:1256` | Relative XY is translated. Modern clipboard additionally clones reachable scripts with fresh typed IDs/owner identities and exact category+UID scope mapping; previously closed I03 supplies identity proof. Global band/member roots are not cloned. |
| S13 | Copy-buffer order versus direct export order | `Source/Sources/Map/Map_Select.c:553` | Copy visits row/column/five categories; stable within-category stack order. Direct export keeps original category-group order and original first-per-cell behavior where specified. Existing SelectionOrder/DirectSelectionExport evidence retained. |
| S14 | Historical DKZ and native/project transport | `Source/Sources/Map/Map_Select.c:1117` | KMAP/KTGS compatible projection plus DKCP5 raw tiles/things/graphs/masks. Historical versions retain explicit policy from I03/I04; no external global band is fabricated on conversion. |
| S15 | Acceptance, atomic Undo and copy publication | `Source/Sources/Map/Map_Select.c:551` | Copy0, Cut/Paste1+removed, inserted objects add0; one parent Undo. Modern atomic rollback retained. Candidate buffer published only after successful Cut and user-owned replacement request; Undo keeps buffer. |

## Проверка результата

Глобальный отряд — GAMEBAND/MTHBAND и его16 embedded member slots. Это
определение отряда со сценариями, а не физический объект в прямоугольнике.
Размещённый герой — GAMEHERO либо MAPTHING Creature. Source Select_Copy/Paste
обходит только MapCreatures/Doors/Traps/Objects/MagicalObjects. Band/member,
areas/gates/effects, players/settings и camera/path records исключены.
Сценарии band/member остаются независимыми; clipboard берёт только графы
действительно выбранных physical owners. Это не превращает global band
в placed hero и не создаёт отряд в другой карте.

Новые5 логических групп: две native/project band+member roots; две exact
physical payload на заведомо неподходящей для обычной кисти воде; одна
проверка14 native/project special terrain случаев. Проверяются DKZ serialization
и Parse, реальные Cut/Paste, exact unchanged globals/root bytes, все physical
payload bytes кроме relocation XY и fresh project uniqueid,1+removed counter,
полные Undo/Redo buffers. Partial heart/portal/gate paste остаётся буквальной
одной клеткой; cursor footprint при Paste не применяется, как в source.

Три неизменённых группы: focus-01; refined project payload: focus-02;
refined native payload: focus-03. Native refinement сначала упал из-за
неверного предположения теста об общем имени LocationX. Выбраны реальные
поля каждой schema; byte equality сохранена. Повторён только failed native
case; ранее PASS project не повторялся. Итог5PASS/0FAIL для5 логических групп.
Все работают на том же Core, что и опубликованное приложение.

Evidence: `artifacts/Evidence/SelectionBandPlacement-20261025/`:
i08-closure-proof.json содержит current providers/source hashes, каждую
строку и датированные источники прежних успехов. Старые SelectionOrder,
DirectSelectionExport, HistoricalSelection, SelectionCallers, HeroBandDisplay
и I03 identity tests не запускались заново. Нового полного прогона нет.

Современные дополнения остаются явными: exact raw DKCP/graph cloning,
atomic rollback, cursor paste fallback, исправление source cut FillID bug.
Они описаны в [selection callers](EDITOR_SELECTION_CALLERS.md),
[terrain rules](EDITOR_TERRAIN_RULES.md), [direct export](DIRECT_SELECTION_EXPORT.md),
[transforms](SELECTION_TRANSFORMS.md). Исторические ошибки Undo не возвращены.

I09 остаётся для gate transforms; I10 — context callbacks сценариев; I18 —
malformed owners/IDs. Все23 M-группы сохраняются: физическое окно, ввод,
официальный редактор, игра, звук, hardware/DPI не считаются проверенными.
Текущий остаток3I+23M=26. Полная цель активна.
