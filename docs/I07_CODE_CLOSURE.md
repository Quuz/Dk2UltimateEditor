# I07: конечная сверка размещения

Уточнение ссылок при сверке I08: I05 относится к player import. Ссылки на physical-object evidence исправлены на OBJECT_PAINTING/EDITOR_OBJECT_PAINTING_CALLERS. Маршруты I07, код и его результаты не менялись. Текущий остаток —3I+23M=26; прежние counts ниже исторические.
25 октября 2026. **I07 закрыт по коду.** Все14 маршрутов ниже сопоставлены с
текущими реализациями и исходником. Найденный дефект заливки исправлен;
повторная реализация уже работающих footprint/pen/object маршрутов не нужна.

| ID | Маршрут | Исходник | Текущий результат |
| --- | --- | --- | --- |
| R01 | Native/project cell identity on import | `Source/Sources/Compiler/Compiler_Conversions.c:183`, `MAPINFO* Compiler_GameMapToEditorMap` | Only terrain/owner/variant define source cells; invalid owner/variant and unknown terrain use the documented import projection. Original raw cells remain untouched until edited. |
| R02 | World/room/path/wall/bridge/gate owner and variant | `Source/Sources/Map/Map_Functions.c:92`, `DWORD Map_TranslateWorldItem` | Fixed World/Gates owners come from source catalogues; owned Rooms/Walls retain player. Both bridge17 variants stay distinct. |
| R03 | 39 catalogue footprints and cursor edge shift | `Source/Sources/Map/Map_Mouse.c:589`, `void Mouse_UpdateCursor` | Eight constrained entries; zero maximum is unbounded. Existing 39-row and eight-entry evidence retained. |
| R04 | Press acceptance and bounded-footprint protection | `Source/Sources/Map/Map_Areas.c:301`, `int Map_CanPaint` | Initial protected special/object footprint refused before LMB count; ordinary terrain accepts and skips protected writes. Positional perimeter reconstructs loader locks; arbitrary unsaved transient locks cannot be inferred. |
| R05 | Fixed-size immediate rectangle | `Source/Sources/Map/Map_Mouse.c:276`, `PaintImmRect:` | Equal min/max axes stamp filled rectangle immediately for every ordinary terrain tool. |
| R06 | Brush/fill with a multi-cell minimum | `Source/Sources/Map/Map_Mouse.c:251`, `case PAINTMODE_FILL:` | Heart/portal/gate minimum selects immediate rectangle rather than free pen or flood. |
| R07 | Free pen start/move/release | `Source/Sources/Map/Map_Mouse.c:246`, `MapCursorPenOld.x1 = 0;` | Source0,0 sentinel, one LINE per distinct cell move, no extra release segment, one full-map Undo. Existing evidence retained. |
| R08 | Five geometric tools and constrained release | `Source/Sources/Map/Map_Mouse.c:299`, `void Mouse_LeftButtonUp` | Rectangle/filled rectangle/ellipse/filled ellipse/line; source masks retained. Multi-cell minimum forces filled rectangle; expanded release skips individual locked cells. |
| R09 | Shift/random sampling order | `Source/Sources/Map/Map_Display.c:730`, `void Map_PaintElement(` | Five random counterparts sample row-major bounding cells before mask/protection. PRNG seed identity and physical input remain manual acceptance. |
| R10 | Ordinary flood component | `Source/Sources/Map/Map_Display.c:533`, `void Map_FillArea(` | Fixed here: compare projected source identity, not native reserved. Four neighbours, selection clip, protected perimeter; equal start still traverses. Seven new current-Core groups PASS. |
| R11 | Single terrain helper and prepared raw brush | `Source/Sources/Map/Map_Display.c:693`, `void Map_PaintElementSimple(` | Map_PaintElementSimple has no active C caller. Current prepared brush is an explicit changed-only API; actual free pen uses R07. Raw FloodFill keeps four-byte comparison and equal early return. |
| R12 | Five single physical-object tools | `Source/Sources/Map/Map_Mouse.c:65`, `case PAINTMODE_CREATURE:` | Creature/door/trap/object/magical-object dispatch uses source CanPaint and first same-category policy; doors toggle, creation removes opposing category. OBJECT_PAINTING and EDITOR_OBJECT_PAINTING_CALLERS supply existing object-route evidence. |
| R13 | Terrain versus explicit gate/area region records | `Source/Sources/Compiler/Compiler_Generate.c:831`, `int Compiler_GenerateGates(` | Terrain paint/flood only writes tile cells, never creates/deletes/repositions region records. Compiler iterates explicit MapGates; orientation generation/transform remains independently scoped by I09. Exact unrelated records are asserted by new flood fixtures. |
| R14 | Acceptance/history and modern selection extension | `Source/Sources/Map/Map_Mouse.c:255`, `Undo_CreateFullMap(szMainMenuFill` | Accepted equal/protected fill keeps caller Undo/count rules. Modern selection-limited painting rejects a partial special footprint; explicit documented extension. Atomic full rollback and source-bug corrections stay as agreed. |

## Условие закрытия

- Every active source paint entry is mapped; the unused simple helper is explicitly distinguished.
- All catalogue owner/variant/footprint policies and protected press/release ordering have current providers and dated evidence.
- Terrain callers preserve independent region/object/scenario buffers; gate orientation transforms remain I09 scope.
- Last uncovered flood identity mismatch fixed and reproduced: six failing groups now pass; existing successful geometry/history/object tests not repeated.

Текущая проверка:7PASS/0FAIL; до исправления6FAIL/1PASS. Сборка всех41 проектов
и публикация завершены без ошибок и предупреждений. Полный diff затрагивает
два product-файла, один новый test-файл и CLI диспетчер Validation.
Source/Binaries/fixtures, все игровые inputs и EN/RU literals сохранены.

Датированные успешные проверки использованы без перезапуска: full.log от
CreatureLevelModifierWindow-20261019 (terrain rules и footprint группы),
HistoryMemoryNotice-20261017/history-03.log (drawing/history),
AreaRemovalPatrol-20261015/gate-retention.log (независимые gate records),
[OBJECT_PAINTING](OBJECT_PAINTING.md) и [object callers](EDITOR_OBJECT_PAINTING_CALLERS.md) (объекты), [terrain placement](EDITOR_TERRAIN_PLACEMENT.md),
[terrain rules](EDITOR_TERRAIN_RULES.md), [drawing history](EDITOR_DRAWING_HISTORY.md).
Это прежние сборки; их успехи не объявляются новым full/current run.

I08 сохраняет band selection/copy/cut/paste; I09 — перемещения и ориентацию
ворот при transform; I10 — сценарные context callbacks; I18 — malformed
owners/identities. Все23 M-группы сохраняются, включая физический ввод,
отображение, palette UX, звук и игру. PRNG seed identity и произвольные
несохранённые transient locks исходного GUI не выводятся из native padding.

Текущий остаток4I+23M=27. Полная цель не завершена.
