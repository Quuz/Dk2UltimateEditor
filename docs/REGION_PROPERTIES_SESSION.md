# Частные свойства областей, проходов и генераторов

12 октября 2026. Команда «Свойства области» теперь открывает отдельное окно EN/RU
с RegionPropertiesSession. До общего Apply ни ввод, ни вложенные options не
меняют карту. Закрытие/общая Отмена удаляют весь черновик.

## Поля и хранение

Для всех трёх типов: inclusive start/end coordinates, использование текущего
выделения, project name/info и rectangle/text colours через существующий
системный выбор цвета. Проход — одна клетка. Native effect width/height — дельты,
поэтому один квадрат хранит нули. В DKM name/info занимают64/128 bytes CP1252;
изменённый текст ограничен63/127 bytes. Неизменённый padding/text tail сохраняется.

Native KLD не содержит name/info/colours. Соответствующие controls отключены с
объяснением о DKM, а не записываются в неизвестные игровые bytes. Все сохраняемые
native bounds, area flags и effect options редактируются обычным образом.
Identity, patrol successor/delay, script roots, gate health/type/orientation и
inactive fields не получают права записи от этого окна.

Вложенная area page содержит четыре исходных checkbox bits: hero lair1,
ignore solid64, visible16, toolbox32. Unknown bits остаются неизменными даже при
применении других options. Исходный child сбрасывал неизвестные bits; эта
современная граница сохраняет импортированные данные, не угадывая их смысл.

Effect page содержит четыре named effect selectors, сохранение неизвестного
импортированного ID и integer frequency1–255 со slider. Новое неизвестное значение
не выбирается; неизменённые импортированные значения сохраняются. Единица времени
не предполагается. Child Done/закрытие принимают valid inputs только в private
parent context, как исходные IDOK/IDCANCEL. Общая Отмена всё равно отменяет их.
Некорректный ввод даёт ошибку до публикации.

## Применение и lifetime

Prepare создаёт frozen plan. Update черновика после Prepare, изменение карты,
Edit+Undo, raw mutation, другой document или Dispose делают его неприменимым.
Fingerprint защищает весь документ, не только выбранную запись. Apply разрешает
только перечисленные fields и сохраняет один parent Undo/Redo с counter increment1,
включая равное принятие. Generic SetMapRegionBounds/ApplyEffectGenerator продолжают
работать как changed-only APIs.

Чтение исходных перевёрнутых/выходящих за границу координат не правит live bytes;
показанный private rectangle соответствует source sorted/clipped context. Его
явное принятие публикует эту нормализацию. Native heart gate14 не считается
редактируемым hero gate этого окна.

## Проверка и оставшиеся требования

RegionPropertiesSessionChecks проверяет6 native/project×area/gate/effect fixtures:
literal packed offsets, private changes/Cancel, equal acceptance/counter/Undo,
составное Apply, unchanged padding/text tail/unknown flags/identity/root,
foreign/stale/raw mutation/disposal/invalid inputs, Save/reopen. Core focus exit0.
EN/RU resources и actual command route скомпилированы; подготовлен presenter seam
для будущего UI fixture без модального зависания. UI fixture здесь не запускался.
Текущие evidence: `artifacts/Evidence/RegionProperties-20261012`.

Физическое окно, цветовой системный диалог, child close, DPI/мониторы и игра ещё
не проверены пользователем. Интерактивный ChoosePoint теперь подключён через
[REGION_GEOMETRY_PICKING](REGION_GEOMETRY_PICKING.md), но его physical route ещё
не проверен. Вложенная [генерация проходов](REGION_GATE_GENERATION.md) теперь
использует private command replay, source allocation counters и один outer Undo;
подтверждённые дочерние изменения отменяются вместе с родителем. Отдельный
manager workflow сохранён. Физическое региональное соответствие и вся цель
FEATURE_PARITY этим этапом не закрыты.

Исторический этап RegionProperties:1800 passes/63 failures, terminal exit1; все63
сообщения буквально совпадают с прежним этапом и в прежнем порядке; все1799passes
сохранены. Все41 проекта собраны0warnings/errors. НовыйWindowsSmoke source helper
сначала не компилировался; failure сохранён, corrected build прошёл. СамUI fixture
не запускался. EN/RU29-key parity и static actual command binding проверены.
Текущие retention/conversion/archive/activity checks exit0; installed4453 и
Source/Binaries1006 неизменны. Эта проверка не доказывает physical UI/game.
Свежие общие результаты и Core SHA перечислены в [CURRENT_STATUS](CURRENT_STATUS.md).

Native ACTION_DESTROYGATE теперь следует за выбранной физической записью
при общем применении геометрии, включая imported/generated replay. Критерий
сердца — type14, а не bGateID. [Границы рабочих связей](NATIVE_GATE_MUTATION.md).
