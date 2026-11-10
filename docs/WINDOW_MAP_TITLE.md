# Внутреннее имя карты в заголовке окна

17 октября 2026, WindowMapTitle-20261017. Исправлен установленный внутри I17
default title: основное окно показывает внутреннее имя карты. Имя сохранённого
файла больше не подменяет его при обычном открытии или Save As.

## Исходное поведение

Source/Sources/Map/Map_Setup.c:405, Map_NewTitle, выбирает Map.Properties.Name,
если Config.UseFilename=FALSE. Это исходный default. ShowFullName участвует
только при UseFilename=TRUE; активной команды изменения этих полей в исходном
CONFIG inventory нет. Новый settings dialog ради них не добавлялся.

Современные дополнения сохраняются: звёздочка при несохранённых изменениях,
локализованная отметка восстановления и название приложения EN/RU. У
восстановленной карты отображаются и её внутреннее имя, и отметка recovery.
После Save As отметка recovery снимается существующим save lifecycle;
внутреннее имя карты сохраняется, если пользователь отдельно его не изменил.

## Чтение и обновление

EditorWindowCaption.cs находится в App. Он использует существующие
LegacySchemas/LegacyProjectSchemas и NativeField.Read, без второй таблицы
offsets и без изменения Core. Native KWD читает wstrMainName в проверенной
GAMEINFO v4; project находит единственную текущую KINF и её Name в layouts
1–7. Порядок имён в файле/списке не используется для выбора при неоднозначной
KINF. Future version, неверная длина или выход поля за объявленный payload
возвращают отсутствие результата. Тогда окно использует basename текущего
пути как запасной вариант, не изменяя данные и не изобретая название.

UTF16 native и Western ANSI project читаются их существующими codecs.
Полное импортированное64-символьное project name и пустое имя сохраняют
смысл; padding после NUL не становится частью заголовка и не переписывается.

Основное окно читает имя при обычном Document_Changed и смене языка.
Rename/Apply/Undo/Redo читают актуальные bytes, а не старый сериализованный
корневой контейнер DKM или закэшированное имя. Reader не вызывает Records(),
CreateMapGeneralDraft() или fingerprint сценариев. Для project перебираются
только заголовки небольшого набора Files; для native — только MainFile.
Это не полный разбор объектов/сценариев при каждом обновлении заголовка.

## Проверка

Evidence: artifacts/Evidence/WindowMapTitle-20261017. Focus проверяет новые
чтения на native/project: внутреннее имя отличается от FileName, live Rename
и Undo, отсутствие изменений bytes/revision/history от чтения. Отдельно
проверяются Unicode, границы native metadata, все7 KINF layouts, malformed/
future versions, пустые и full-width имена. Неподдержанные bytes не угадываются.

Существующий SaveLifecycleUiSmoke assertion обновлён: после Save As ожидается
внутреннее имя, без dirty marker, вместо нового basename. Этот WPF caller
только скомпилирован; GUI не запускался. Подтверждённые коды build/focus/publish
и hashes — в delivery.json. Старые успешные CLI/corpus/official проверки не
повторяются; Core/Source/Binaries/фикстуры/семифайловая TestMap не изменялись.

## Ручная M03/M14/M23

1. Открыть копию карты, у которой внутреннее имя отличается от имени файла:
   в title отображается внутреннее имя.
2. Изменить имя в общих свойствах. Cancel сохраняет прежний title; Apply
   меняет его и показывает `*`. Undo/Redo возвращают соответствующие имена.
3. Save As под другим именем файла: title сохраняет внутреннее имя и снимает
   `*`. Проверить EN/RU и восстановленную копию с отметкой recovery.
4. Реальные historical/unknown layouts проверять согласно M04; запасное имя
   файла не означает разрешения редактировать неподдержанный формат.

Кодовый default title закрыт в указанном объёме. Остальной I17 — геометрия
служебных окон, независимые пути, report semantics и оставшиеся manager callers —
сохраняется в [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).
