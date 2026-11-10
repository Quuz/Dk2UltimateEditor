# Независимые пути файловых диалогов и сохранение BMP

17 октября 2026, FileDialogPaths-20261017. Внутри I17 подключены независимые
рабочие scopes MapPath/AreaPath/ThumbPath. Запоминаются каталог и имя файла;
AREA import/export и BMP больше не начинают каждый раз с пустого имени или
чужой категории диалога. Предпочтения не являются данными карты или Undo.

## Исходник и момент запоминания

Misc_Requests.c, Misc_FileSelect/Misc_FileSelectSave, сначала копирует Name в
приватный FileName. Только успешный GetOpenFileName/GetSaveFileName копирует
результат обратно; Cancel оставляет Name прежним. Далее callers различаются:

| Scope | Исходный caller | Современный маршрут |
| --- | --- | --- |
| MapPath | Files_Load.c/Files_Save.c используют временное имя и принимают Config.MapPath после успешной операции; native game load очищает working target | MainWindow синхронизирует MapTarget с EditorSaveSession.TargetPath после принятого состояния. Open и Save As получают каталог/имя этого working target. Native/recovered/new map без target не получает адрес для обычного Save |
| AreaPath | Map_Select.c передаёт Config.AreaPath прямо в chooser; имя уже сохранено перед чтением или записью | Один AreaPath на workspace для Import/Import Into Selection/Export Selection/Export Copy Buffer; обновление после OK chooser, перед reader/writer, включая последующий IO failure |
| ThumbPath | Thumbnail_Functions.c:611 получает путь до CreateFile/WriteFile | ThumbnailWindow использует общий ThumbnailPath workspace. После OK путь сохраняется до записи; Cancel ничего не меняет |

EditorFileDialogSession хранит три отдельных значения. Area/Thumbnail живут
при переоткрытии дочерних окон и смене карты в том же workspace. Новый workspace
имеет новый сеанс. Эти два поля не добавлены в AppSettings/JSON. Существующий
LastOpenedFile сохраняется как прежде: это современный fallback каталога карты,
а не разделяемая память AREA/BMP и не разрешение записывать исходную KWD.

MapTarget остаётся производным от принятого save target, не становится второй
системой назначения Save. Отмена Save As, отказ compatibility/loss consent и
неудачная запись не принимают новый target. Новый/native/recovery документ
очищает только MapTarget; AREA/BMP scopes сохраняются. Если запомненный каталог
удалён, InitialDirectory не принуждается к отсутствующему пути; имя остаётся
доступным для выбора другого места. Current directory процесса не меняется
нашим кодом.

Core reader/writer для AREA/DKZ/MAP, source archive, overwrite consent и порядок
direct selection против copy buffer не менялись. Прямые API выбора/сохранения
без native chooser не выдают себя за новое действие файлового диалога.

## Исправление подтверждённой замены BMP

Прежний UI выставлял OverwritePrompt=true, но после согласия пользователя
вызывал MapThumbnail.SaveNewBmp, который корректно запрещает существующий
target. Поэтому подтверждённая замена всё равно завершалась ошибкой.

Теперь Save_Click готовит точные BMP bytes до chooser, как исходный экспорт.
После OK сохраняется ThumbPath, проверяются тот же документ/revision/options.
Изменённый или закрытый источник не записывается; сообщение об устаревании
локализовано EN/RU. ThumbnailFileOutput.SaveConfirmed записывает подготовленные
bytes в новый staging file того же каталога, закрывает/flush и затем заменяет
target. Ошибка записи/flush не затрагивает прежний target; свой staging удаляется.
Неполный BMP отклоняется до открытия staging. Каталог назначения не создаётся
скрыто. Согласие native Save/overwrite dialog требуется до этого UI-маршрута.

Прямой ThumbnailWindow.SaveTo/MapThumbnail.SaveNewBmp остаётся non-overwriting
API: наличие файла всё ещё даёт отказ, не подразумевает согласия на замену.
Различие generic API и подтверждённого UI caller сохранено явно.

## Проверка

Evidence: artifacts/Evidence/FileDialogPaths-20261017. Новая CLI-проверка
использует только собственный уникальный temp-каталог: независимые пути,
имя после неудачного AREA чтения и new/native reset, защита new-only API,
неверный BMP, контролируемый partial write/flush failure, точная публикация
нового и замена существующего файла, отсутствие staging после операций.

Первый focus01 остановился на неправильном предположении, что
InvalidDataException входит в IOException. Отдельный catch добавлен и в
проверку, и в BMP UI handler; исходная ошибка сохранена как evidence.
Последующий итоговый focus указан в delivery.json, не заменяет исходный failed
лог. Файлы теста не являются установленными картами или эталонными ресурсами.

WPF/native dialogs не запускались. Build/publish коды, текущие DLL, архивы и
проверка их состава — в delivery.json. Core не менялся; прежние успешные
CLI/corpus/official проверки не повторялись. Последний full1815/63 на той же
Core сохраняется. Source/Binaries/fixtures/15WAV/семифайловый TestMap неизменны.

## Ручная M03/M04/M07/M21/M23

1. Открыть/сохранить working DKM в каталоге A. Открыть меню Open/Save As:
   ожидаются его каталог и имя. Отменить выбор другого файла: target прежний.
2. Импортировать DKZ из B, затем открыть Export Selection/Copy Buffer:
   ожидается последнее AREA имя/каталог B. Отмена выбора C оставляет B.
3. Выбрать повреждённый DKZ: после отказа чтения этот выбранный путь остаётся
   в AREA chooser, но карта/буфер не меняются сверх действующего recovery route.
4. Сохранить BMP в C, переоткрыть thumbnail window и сменить карту: следующий
   BMP chooser помнит C, AREA помнит B, working map сохраняет свой target.
5. Сохранить поверх BMP через native overwrite Yes: файл заменён. Native No/
   Cancel сохраняют прежний BMP. При недоступной записи прежний файл остаётся;
   сообщение не закрывает редактор и не применяет изменения карты.
6. Проверить новый/native/recovered документ: Save As не предлагает записать
   исходную KWD; AREA/BMP память прежняя. EN/RU, Enter/Escape и системные диалоги
   на разных DPI остаются ручной приёмкой.

Эти code routes закрыты в указанном объёме. I17 остаётся открытым по геометрии
служебных окон и ещё не сопоставленным manager callers; полнота historical
layouts и compatibility semantics остаётся в своих I-пунктах.
