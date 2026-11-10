# Предпросмотр в системном выборе файлов

23 октября 2026. `FileChooserPreview-20261023`, часть I13.

Предпросмотр добавлен в восемь существующих мест выбора файла: Open Map,
Save/Save As, основу компиляции, основу запуска игры, Export Selection,
Export Copy Buffer, Import/Import Into Selection и Save Thumbnail. Выбор
исполняемого файла, каталога, аудио или отдельных global definitions остаётся
в своих прежних диалогах.

Это один системный Explorer-style диалог Win32 с добавленной нижней панелью.
Нового подтверждения после выбора не добавлено. Панель показывает картинку,
размеры и пять категорий объектов: существа, двери, ловушки, предметы и
специальные предметы. Для BMP показаны его изображение и размеры. Необязательный
предпросмотр может быть недоступен для нового, повреждённого или неподдержанного
файла; это не запрещает обычный выбор. Прежний reader/writer выполняет свои
проверки после OK. Предпросмотр не является разрешением на запись.

## Источник и Win32

Исходный `Misc_FileRead` обрабатывает FILE_MAP (game/editor/text), FILE_AREA
и FILE_THUMBNAIL. Width/Height находятся в controls101/102, пять количеств —
103–107. `Thumbnail_Generate` создаёт пиксели по исходной палитре;
`Misc_FileDraw` вписывает их с сохранением пропорций и чёрными полями.

Текущий chooser использует GetOpenFileNameW/GetSaveFileNameW, child template,
OFN_EXPLORER/ENABLEHOOK/ENABLETEMPLATEHANDLE/ENABLESIZING. Уведомления
CDN_SELCHANGE/FOLDERCHANGE/INITDONE обновляют выделенный файл через
CDM_GETFILEPATH. CDN_TYPECHANGE обновляет default extension через CDM_SETDEFEXT,
поэтому переключение DKM/MAP не оставляет прежнее расширение.
Контракт сверен с локальным Windows SDK10.0.26100 commdlg.h и официальными
[правилами расширения общего диалога](https://learn.microsoft.com/en-us/windows/win32/dlgbox/open-and-save-as-dialog-boxes),
[OPENFILENAMEW](https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamew)
и [CDM_GETFILEPATH](https://learn.microsoft.com/en-us/windows/win32/dlgbox/cdm-getfilepath).
Размеры controls вычисляются через MapDialogRect с учётом native dialog units.

Реальный callback и GDI не выполнялись агентом. Headless проверка подтверждает
ABI текущего 64-битного процесса, размеры и alignment структур/шаблона, но не
доказывает создание native child window, фактический font/layout или работу
клавиатуры. Ожидаемая 32-битная схема зафиксирована, отдельно не исполнялась.

## Чтение и отмена

Core `EditorFilePreview.Read` создаёт отдельный документ/буфер. Поддержаны
native KWD/Map.kld, DKM, DKZ и исторический MAP; для карты и области выдаётся
top-down BGRA по исходной MapOverviewPalette. AREA читается без применения
recovery, ремонта или вставки. Для текстовой карты дополнительно доступны
количества из действующего читателя; старый preview оставлял их нулевыми.
Заголовок BMP читается для размеров; сам BMP декодируется через Windows
LoadImage при реальном показе, как в исходнике. При недоступной статистике
объектов сохраняется доступный terrain preview.

Чтение выполняется в фоне, по одному файлу за раз. Новое выделение отменяет
прежний запрос; epoch не позволяет запоздавшему результату заменить новое
изображение или обратиться к закрытому диалогу. Карта, выделение, буфер,
Undo/Redo, dirty, counters, path memory и текущий save target от просмотра
не меняются. FileName/FilterIndex принимаются wrapper только после OK.
Cancel оставляет прежние значения; исходные точки AcceptAreaPath/
AcceptThumbnailPath/SetMapTarget, recovery/consent и сохранение остались
в прежних телах команд.

Native bitmap и память шаблона освобождаются при смене картинки/закрытии;
GDI DC всегда восстанавливает выбранный объект. Ошибка preview не закрывает
chooser и не меняет ответ пользователя. EOF/неверный BMP/неизвестный формат
показываются как недоступный preview. Чтение ограничено 64MiB на входной файл,
terrain raster — 4Mpx, BMP metadata — 16Mpx; эти ограничения относятся только
к предварительному просмотру и не меняют ограничения открытия/сохранения.

## Проверка и ручная приёмка

Целевая CLI проверка читает native/DKM/AREA/text/BMP, сверяет пять категорий,
top-down pixels, отмену, отсутствующий/повреждённый файл, source bytes/sidecars
и отсутствие изменений live document/history/dirty/notifications. Позднее
завершение старого запроса проверяется через независимую очередь продолжения.
Также проверены filter pairs, validation/overwrite/no-CWD flags и смена
default extension. Native chooser, GDI и WPF при этом не запускаются.

Ручная M03/M04/M07/M21/M23 пока открыта:

1. В Open выбрать разные DKM/KWD/Map.kld/MAP, не открывая их: картинка,
   размеры и counts должны меняться. Быстро сменить выбор: старое изображение
   не должно возвращаться. Cancel не меняет открытую карту.
2. В Import/Export Selection и Copy Buffer выбрать DKZ. Проверить картинку,
   counts, Cancel, прежнюю память каталога/имени и обычный import после OK.
3. В Save As сменить DKM на MAP, задать имя без расширения. Проверить выбранное
   расширение и overwrite prompt на существующем файле; Cancel не меняет target.
4. В Save Thumbnail выбрать существующий BMP, затем новое имя. Preview не
   сохраняет и не перезаписывает файл. Прежняя защита записи остаётся.
5. Проверить EN/RU, изменение размеров окна, табуляцию, Enter/Escape, native
   owner/modal, DPI100/150/200% и границы монитора. Закрыть окно во время чтения.

По коду последний прямой file-preview consumer подключён. I13 ещё требует
конечного покрытия document-name/reference/source callers. Все 14 I-пунктов
и 23 M-группы сохраняются; общий full corpus без изменений не повторяется.

English: a single native file chooser now includes a detached image/metadata
preview for maps, selections and BMPs. Reading never accepts paths, applies
recovery or changes the current document. Cancellation and late results are
isolated. The actual native dialog, keyboard, overwrite prompts and DPI still
require manual acceptance.

Итог: build03 всех41 проектов/focus02/publish01 — ExitCode0,
0предупреждений/ошибок. Поставка и оба ZIP обновлены с SHA256 каждого entry.
После первого прохода уточнены x86 reference offset, overflow guard filter index
и native dialog-unit height; final CLI проверяет текущие DLL.
