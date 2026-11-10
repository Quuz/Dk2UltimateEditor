# Кодовое закрытие I05: игроки, число хранителей и сердца

24 октября2026. PlayerImportPolicy-20261024. **I05 закрыт по коду** после
конечной сверки11 случаев и добавления последнего сообщения о пустых slots.
Это не завершение всей компиляции, graph/identity safety или ручной приёмки.

## Новое сообщение при native → DKM

DKM имеет семь позиционных MAPOPTIONS: heroes, neutral и пять keeper slots.
Native сохраняет только реально присутствующие GAMEPLAYER records. Ранее
при сохранении native в DKM отсутствующие записи оставляли соответствующие
project slots с source-import defaults, но отдельное сообщение этого не
объясняло. Теперь `PlayerSlotDefaults` уровня Information перечисляет такие
IDs, включая неиспользуемых хранителей. EN/RU summary показан в существующем
окне принятия сохранения через обычный ConversionIssues route.

Это сообщение, а не новый алгоритм ремонта. Project slots заполнялись так и
раньше. Импорт сохраняет исходные значения имени/ресурсов; source-import
availability/pools defaults отличаются от нового полного player reset.
Все оригинальные native файлы, включая физический порядок и отсутствующие
записи, сохраняются целиком в companion-архиве данных при обычном Save As.
Это архив исходной карты, а не версия кода или цепочка восстановления проекта.
Игровой семифайловый экспорт и его writer не менялись.

## Конечная таблица I05

| Случай | Итог текущего кода |
| --- | --- |
| Native record count / IDs | bGlobalPlayer определяет игрока; physical order не определяет его номер. Диагностика DK2N033–036 объясняет неверный count/ID/set, ничего не переставляя. |
| Семь DKM slots | Всегда позиционные1–7;не объявляются семью активными игроками. Metadata keepers1–5 — другое поле. |
| Отсутствующие native records | Specialized settings не создают записи. Явный native→DKM projection оставляет source-import defaults и теперь сообщает IDs этих slots. |
| Повторный native ID | Specialized edit/geometry/reset требуют уникальности;mixed batch проверяется до записи. Conversion сохраняет source physical last assignment и явный DuplicatePlayer loss report, оригиналы остаются в companion. |
| ID вне1–7 | Specialized edit отвергается;conversion имеет InvalidPlayer Error, обычное принятие DKM blocked. Исходные записи сохраняются. |
| Сердце и здоровье | GAMEGATE14 по owner;last matching health как source. Ранние duplicates не меняются. Без проверенного сердца native health отсутствует;отсутствующее сердце не создаётся. |
| Камера | Source first row-major heart room tile плюс(2,2);malformed first не заменяется более поздним сердцем. Project X=-1 управляет всей парой;NPC0,0. |
| Player generation / declaration | Обычная компиляция сохраняет объявленный count буквально и использует heart-derived presence. New-map factory имеет отдельный includeDeclaredKeepers для пустой создаваемой карты. |
| Камера/resources callers | UI/settings работают по stored ID;ambiguous geometry не получает edit/overlay authority. Capacity/mana считают принадлежность terrain/things, а не ordinal player row. |
| Resize | Только computer keepers3–7,только unique verified row;source sentinels/WORD wrap прежние,без reorder/clipping ибез дополнительного per-player count. |
| Historical/future / Save | V1/V2 ignored camera bytes не становятся editable до explicit upgrade;unknown layouts не угадываются. Native SaveCopy literal;normal native→DKM Save/reopen сохраняет original bundle иявный projection. |

Полная запись source anchors/current providers/hashes, сохранённых руководств
и ограничений — `artifacts/Evidence/PlayerImportPolicy-20261024/i05-route-audit.json`.
Source: Game_ExtractAll/Players/Things, Properties_MapResetOptions и native
Compiler_InfoHeartPosition/GeneratePlayers/TaskTestPlayers. В исходнике
MapOptions[storedID−1] повторно присваивается в физическом порядке;health
аналогично берётся из последнего matching gate. Modern raw native editing
сохраняет оригиналы и не повторяет опасное индексирование ID0/8/255.

Отдельно важно различать read-only диагностику и полномочия редактирования:
warning о дубликате не даёт согласия выбрать первую запись. Native→DKM есть
явное преобразование с loss/error report и retention. Неподтверждённые graph
owners/aliases у дублированных игроков остаются в I01/I18; этот аудит не
объявляет такой граф однозначным. Проверка gameplay heart terrain/presence
в actual human Compile выполняется до generation; direct unfinished-map
Core API не является обещанием запуска некорректной карты в игре.

## Проверки и ограничения

Новые6 PASS-групп: все keeper counts1–5 и обратныйphysical order с CP1252
именами;явные отсутствующие heroes/keeper slots;duplicate last-wins+loss;
invalid0/8/255+blocking issue;реальный unified Save/reopen с чтением companion;
source heart-derived player presence/camera инеизменённыйdeclared count.
Проверены буквальная сохранность исходных файлов иархивных bytes, source
history/revision/counters при подготовке. Камерный случай проверяет source
две точки heart/centre, а не навязывает полную5×5проверку вместо source helper.

На прежнем коде после уточнения fixture получено5FAIL/1PASS:не хватало именно
информационного сообщения, а heart-derived compiler case уже проходил.
После добавления сообщения все6PASS,ExitCode0. Существующая группа
NativeProjectConversionChecks также прошла на текущем Core:5PASS,ExitCode0.
Она затронута изменением общего conversion provider. Предыдущие успешные
player gold/AI/geometry/resize группы без изменений повторно не запускались;
их кодовые guard/route доказательства использованы при сверке таблицы.

Первый test build ошибочно обращался к internal Discover;fixture переведён
на публичные LastSave.SourceArchives/Read без расширения продукта. Первый
heart fixture имел валидный centre(3,3) для candidate(1,1);по source он был
исправлен на candidate(2,1) с отсутствующимcentre(4,3). Продуктовый алгоритм
камеры не менялся. Неуспешные логи сохранены, не выданы за успех.

Build01 всех41 проектов/focus01/native-conversion01/publish01 завершились
ExitCode0,warnings/errors0.2871ENRU keys;все2870предыдущих literals сохранены.
Продукт меняет только informational report идве resource строки; прежние
присваивания/конверсия/writer/guards сравнены буквально с published source.
Source/Binaries/Fixtures/4485game inputs/15WAV/corpus inputs сохранены.
Полный корпус не повторялся:1840PASS/63FAIL — исторический,не зелёныйcurrent full.
GUI/WPF/audio/game/Computer Use не запускались;physical M01/M03/M04/M09/M14/M20
иостальные ручные группы сохраняются. Общее число I scopes теперь10, M23,
всего33 пункта работы. Полная цель ещё не достигнута.
