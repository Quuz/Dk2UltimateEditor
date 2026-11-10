# Подписи полей: конечный перечень ключей

Этап FieldLabelAudit-20261019 завершён 20 октября2026 после прерванного обновления.

Собран снимок полей, которые предоставляет текущий Core: записи новой native
карты со всеми RecordTemplate, новой DKM со всеми LegacyAddChoices, редактируемые
глобальные определения, native global schemas и все поддерживаемые project
schema layouts, включая исторические. Это конечная выборка схем, а не обход
произвольных файлов игры. Дублирующиеся поля и элементы массивов не считаются
отдельными функциями или успешными тестами.

Для каждого поля применён настоящий FieldNaming.CanonicalName и тот же способ
выделения индексов, который использует FieldLabel. Сохранена аннотация
ValueCatalogues.ArrayElement. В снимке27551 вхождение с повторениями:
962 native records,5355 DKM records,262 definition editor,262 native globals,
20710 project layout fields. Снимки до/после совпадают по полям и каталогам;
изменилась только доступность14 ресурсов подписей.

У текущих native/DKM records и definition editor пропусков ключей не было.
14 отсутствующих ключей найдены в исторических Compatibility.h layouts:
Actions.type, Activate, ComputerPlayer, Creatureid, Element, Gate, HeartX,
HeartY, Reusable, Room, Seconds, Squares, Terrainid, Triggers.type.
Добавлены английские и русские подписи. Обычные названия взяты из объявлений
полей; назначение creature/terrain/interface/room/seconds/reusable сверено
с исходным маршрутом преобразования LegacyProjectUpgrade. Служебный node type
назван типом узла списка; смысл отдельного значения type не выдумывался.

Некоторые старые структуры преобразуются в текущую схему до показа окна.
Наличие подписи для такого поля не означает появления нового редактора старой
структуры. Изменение затрагивает только два словаря: FieldNaming, структуры,
offsets, каталоги значений, parser, writer, compiler и Undo прежние.

Все прежние2787 значений в каждом словаре сохранены,14 ключей добавлены;
в каждом языке теперь2801 ключ. У всех ключей этой выборки есть непустая
подпись EN/RU. Решение41 проектов собрано без ошибок и предупреждений,
publish завершён успешно. Исходный код, DLL Core, tests/Fixtures, Source,
Binaries, установленные карты, семифайловый TestMap и15WAV не менялись.

Этот результат закрывает поиск отсутствующего ресурса для данной выборки.
Он не доказывает точность всех старых переводов, семантику каждого enum,
полноту контекстных custom captions или читаемость реального окна.
I13 остаётся открытым. Следующий результат — сверка оставшихся enum/radio
captions по source symbols и текущим control routes, затем reference previews.
Уже успешные проверки неизменённых writers и карт повторно не запускаются.
GUI, renderer, игра и Computer Use не запускались.

Доказательства: artifacts/Evidence/FieldLabelAudit-20261019 — before/after
field inventory, label-changes, scope verification, terminal build/publish
и манифесты поставки. Общий перечень остаётся14I/23M;
все23 возможности FEATURE_PARITY и полная цель сохранены.
