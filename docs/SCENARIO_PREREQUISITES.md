# Требуемые объекты на страницах сценариев

Основной код,13 октября2026; доказательства
`artifacts/Evidence/ScenarioPrerequisites-20261013`.

Исправлено пропущенное требование четырёх условий: создание отряда,
погибшие/заключённые/без сознания в отряде. Оригинальные ParamsCheck проверяют
`MapHerosBands.next` и запрещают продолжение без отрядов. Теперь common
`MissingRequirements` сообщает Band, окно показывает существующее сообщение
EN/RU и блокирует Apply; private создание/изменение также используют этот
guard. Присутствующий отряд0 удовлетворяет требованию.

Весь набор явных проверок коллекций в обоих исходных ParamsCheck:

| Страницы | Требуется |
| --- | --- |
| BANDCREATED / DEATHSINBAND / JAILEDINBAND / STUNNEDINBAND |Отряд|
| RELEASEBAND |Отряд, затем зона|
| SHOWAREA / ENABLEFOG / MOVECAMERA / ROTATECAMERA / CAMERAPATH |Зона|
| DESTROYGATE |Проход героев|
| TOGGLEEFFECT |Генератор эффекта|

Итого12 страниц:4 условия и8 действий. Source capture извлекает все37
condition и33 action top-level cases исходных parameter-check функций и
сохраняет12 проверок с порядком требований и хешами обоих файлов. Это явные
collection prerequisites, не все Init allocation errors и не полномочия
конкретного ID. Для native DESTROYGATE учитываются реальные проходы героев;
сердце хранителя не подменяет source MapGates.

Отсутствие требуемой коллекции блокирует семантическую страницу даже при
пустом Apply. Существование коллекции не разрешает неизвестный импортированный
ID как новый выбор: обычные choice/import guards остаются. Read-only просмотр,
raw APIs и сохранение исходных bytes не превращаются в ремонт карты. Общий
guard проверяет страницу, а не каждый raw сценарный operand.

## Проверки и границы

Модель сравнивает все103 типа ×16 масок присутствия Band/Area/Effect/Gate ×2
формата:3296 сравнений требований. Отдельно проверены пустой/явный Apply на
четырёх условиях, отсутствие/наличие отряда0 и private creation/editing pages.
Чтение/отказ сравнивается с полными bytes, ревизией, Undo и editor counter.
Результаты итогового прогона текущей DLL приводятся в CURRENT_STATUS.

Первоначальное предположение capture о13 страницах было неверным; исходник
содержит12. Первоначальный журнал сохранён, число и область capture исправлены
до генерации fixture. Это не исходная ошибка редактора и не удаление capability.

WPF smoke подготовлен для EN/RU ×native/project ×4 условия ×absence/presence:
Apply/Cancel/no-op, доступность кнопки и перевод пояснения. Он компилируется,
но не запускался. Source/Core tests не доказывают физическую работу окон.

## Сверка остальных notification обработчиков

Конкретные обработчики parameter страниц сопоставлены с текущими маршрутами:

| Исходное уведомление | Современный маршрут и граница |
| --- | --- |
| Variable literal/variable radio, alias primary/secondary selection |ScenarioAliasEventEditor/private composition; initial hidden secondary0 исправлен отдельно|
| TIMEELAPSED/DISPLAYTIMER EN_CHANGE |Общие duration units с23/59/59; alias TRIGGER_TIMER отдельно принимает60–99|
| Три band-selection population labels |ScenarioBandHints/ScenarioParameterWindow; read-only prefix|
| Terrain selection/forced owner |ScenarioTerrainOwners, paired private inputs на двух страницах|
| ADDCREATURE flags/point/area selection |SpawnFlags/private input и SpatialArea/point callback; game acceptance остаётся|
| CHANGESQUARE/CHANGEOWNER area-point selection/rectangle toggle |Private spatial page/creation rectangle; source atomic history differences документированы отдельно|
| CAMERAPATH library button |Owned predefined path selection и ShowCell inversion|
| RELEASEBAND checkbox500/501 dependency |Обычный трёхзначный выбор0/1/2, соответствующий source Store; layout отличается|
| SAY selection/text/double-click sound |ScenarioSpeechPanel; sound/files/system-dialog actual acceptance остаётся|

Эта таблица — чтение маршрутов и ссылки на ранее проверенные подсистемы, а не
повторный physical callback прогон или доказательство всех GUI деталей.
Уведомления списка выбора типа и Repeat-контекст лежат вне этой таблицы.
Полное source-counter/caller/selection/history и physical GUI/game acceptance
не закрываются отсутствием новых пропусков в этих notification switch.

## Ручные шаги

1. На копии карты без отрядов откройте одно из четырёх условий. Apply должен
   быть недоступен, сообщение объясняет необходимость отряда. EN/RU переводит
   сообщение; Cancel не меняет карту.
2. Создайте отряд0 и заново откройте условие. Выбор отряда0 должен быть доступен;
   unchanged Apply не добавляет Undo. Повторите в private мастере создания и
   изменения, включая Back/Next/Cancel.

Игровая проверка — только семифайловый `DK2Preview_6a88240eaf814f78` через
`DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78`.
