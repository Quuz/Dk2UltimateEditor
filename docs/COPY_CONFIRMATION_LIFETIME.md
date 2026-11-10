# Срок действия подтверждения замены буфера

17 октября 2026, CopyConfirmationLifetime-20261017. Исправлена задача R05
из [сверки запросов](SOURCE_REQUEST_AUDIT.md). Подтверждение относится к
внутреннему буферу выделения редактора: Copy, Cut, повёрнутая копия и импорт
DKZ в буфер. Прямая вставка DKZ не заменяет его и не спрашивает об этом.

Ранее remembered Yes присваивал тому же bool, который MainWindow.SaveSettings
сохранял в JSON. Поэтому ответ «не спрашивать снова» становился постоянной
настройкой. Исходные REQUEST_COPY/Config.RequestsFlags действуют в пределах
процесса. Теперь CopyBufferConfirmationState разделяет два состояния:

| Состояние | Откуда берётся | Срок действия |
| --- | --- | --- |
| Configured | Явное изменение пункта меню подтверждения | Записывается в AppSettings и применяется при следующем запуске |
| Suppressed | Yes с галочкой «не спрашивать снова» после успешной замены | Только текущий EditorWorkspace; сохраняется при смене карты, в JSON не попадает |

Вопрос требуется при Configured=true и Suppressed=false. Меню показывает
текущее эффективное состояние. Явное включение вопросов через меню также
сбрасывает временное подавление; явное отключение становится постоянным.
Сохранение любых других настроек во время подавления записывает Configured,
поэтому следующий процесс снова спрашивает при исходном Configured=true.
Существующее сохранённое false загружается как явная настройка: старый JSON
не позволяет надёжно отличить прежний remembered reply от выбора меню.

Все прежние consent guards стоят до perform и записи ответа: актуальность
документа/выделения, identity и literal bytes старого/входящего буфера,
workspace revision, reentrant/closed guards. Keep/Close/stale/false perform
или исключение не вызывают AcceptReplacement. После successful perform
записывается только runtime состояние. Если callback уже явно отключил
вопросы, запись ответа не возвращает их и не выбрасывает ошибку после commit.
Эта настройка остаётся вне карты и Undo; байты карты и форматы не меняются.

Source: Map_Select.c Select_CopyRequest, Misc_Requests.c default-only remember,
Properties_Editor_Config.c Config_SetToDefault/ZeroMemory. Общая реализация
Copy/Import не заменяется другим маршрутом. EN/RU2603 ключа и все literal
values прежние. AppSettings JSON schema остаётся с прежним bool.

CLI-проверка новой модели охватывает ordinary Yes, session remembered Yes,
serialization/new process, явное включение/отключение меню, reset и late reply.
WPF smoke дополнен проверкой actual MainWindow.SaveSettings при временном
подавлении. Все41 проекта собраны; WPF только компилировался, не запускался.
Старые физические/WPF результаты в COPY_BUFFER_CONFIRMATION относятся к
своим датам и не объявляются результатами текущей DLL.

Full regression текущей Core:1812PASS/63FAIL, terminal1. Все1811 предыдущих
passes сохранены в прежнем порядке, добавлен один новый model group; raw
ordered Inputs и63 Failures буквально прежние. Исходные94 сопоставлены
полностью:31 исправлено ранее, одно известное XXLevel14/wMainTrigger52
сообщение отличается; новых отказов и потерянных passes нет. Audit63/27
проверен по input/resolver SHA. Corpus остаётся НЕ зелёным.

Focused state check, fresh official seven-file export и publish terminal0.
Source/Binaries1006, installed maps4453, fixtures, старый7-file TestMap и
свежий official набор неизменны. Source/runtime ZIP и каждый entry проверены,
поставка/документация текущие. Evidence: artifacts/Evidence/CopyConfirmationLifetime-20261017.

Вручную: включить вопрос в меню, скопировать выделение дважды, на втором
Yes запомнить ответ. В текущем запуске вопрос больше не появляется даже
после смены карты; после закрытия/нового запуска снова появляется. Затем
отключить вопрос непосредственно в меню и проверить сохранение отключения
после запуска. Проверить EN/RU, No/Close, Cut/Undo и импорт DKZ. Эти действия
агент не выполнял. Прочие request задачи R01–R04, все23 области/семь очередей
и GUI/audio/system-dialog/hardware/DPI/game приёмка остаются в
[REMAINING_WORK](REMAINING_WORK.md).
