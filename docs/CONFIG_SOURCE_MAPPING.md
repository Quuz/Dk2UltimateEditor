# Сопоставление CONFIG исходного редактора

17 октября 2026, RegionUndoConfigScope-20261017. Это конечная карта 42 полей
CONFIG из Source/Includes/Config.h внутри I17. Исходный inventory содержит
337 лексических ссылок в 37 C-файлах, включая исходные defaults. Он сохранён
в artifacts/Evidence/ViewVisibilityCommands-20261017/source-config-inventory.json.
Строка таблицы не означает, что её физический GUI уже принят.

«Есть маршрут» означает найденное место реализации и существующий отдельный
scope. «Нет активного caller» относится именно к исходному полю, а не ко всей
связанной возможности. «Сверить» остаётся работой по коду, не переводится
автоматически в ручную приёмку. Параметры без исходной команды настройки
не требуют выдумывать новый пользовательский диалог только ради CONFIG.

| Поле CONFIG | Исходное назначение / caller | Текущий маршрут или конкретный остаток | Результат |
| --- | --- | --- | --- |
| HideSplash | Windows_Splash.c: caller внутри #if (!SPLASH_DISABLED) | Editor.h задаёт SPLASH_DISABLED1; единственное чтение поля исключено при компиляции. Новый переключатель/окно не требуются | [Нет активного compiled caller](FILE_IO_PROGRESS.md) |
| HideToolbar | Windows_Toolbar.c, MainWindowProc | MainWindow.View.cs + EditorWorkspace.MapToolbar; переключение текущего окна, без записи поля в JSON | Есть маршрут; M21/M23 |
| HideMinimap | Windows_Minimap.c, Windows_Info.c | EditorWorkspace.SetOverviewVisibility и View → Minimap; текущая видимость согласована с прежним флажком | Есть маршрут; M21/M23 |
| HideDisk | Windows_Disk:5 file method callers; default FALSE, команды переключения нет | MainWindow.DiskProgress, Core Load observer, DiskProgressStream для прежнего Save writer; recovery/quiet autosave scopes | [Маршруты подключены](FILE_IO_PROGRESS.md); M03/M20/M21/M23 |
| UseFilename | Map_NewTitle: FALSE по умолчанию означает внутреннее имя карты | EditorWindowCaption и MainWindow.Document_Changed читают живое metadata name с fallback только для неподдержанного header | Есть маршрут; [проверка и M03/M14/M23](WINDOW_MAP_TITLE.md) |
| PlayerUsesShell | Config_GetPlayerPath + Properties_Map_Voices | GameAudioPreview / GameResourceSettings: default association либо явно выбранный player, отдельный аргумент пути | Есть маршрут; M14 |
| ShowFullName | Map_NewTitle: путь или basename только при UseFilename=TRUE | В исходнике нет активной команды изменения поля; default internal-name route реализован, выдуманный settings dialog не добавляется | Default route закрыт вместе с UseFilename |
| MinimapSize | Windows_Minimap: размер миникарты | OverviewSize_Changed, MapOverview, варианты96/144; точная геометрия окна остаётся M21 | Есть маршрут; M21 |
| MinimapKeepProps | MainWindowProc + Minimap + Thumbnail | OverviewProportions_Click и thumbnail callback сохраняют KeepProportions | Есть маршрут; M21 |
| MainWindowRect | В CONFIG объявлено, активных ссылок в source C нет | Не считать отсутствие сохранённого RECT недостающей исходной командой | Нет активного caller |
| MainWindowShow | В CONFIG объявлено, активных ссылок в source C нет | Не считать поле отдельной отсутствующей функцией | Нет активного caller |
| DialogPosition | Default FOLLOWMOUSE; само поле не читается активным caller | Исходные окна выбирают позицию собственными flags; соответствие проверяется в scopes этих окон | Нет активного caller поля |
| RequestsPosition | Default CENTER; само поле не читается активным caller | Реальные request callers имеют собственные позиции и отдельный SOURCE_REQUEST_AUDIT | Нет активного caller поля |
| AutoSaveTime | Files_Save: период автосохранения | MainWindow.Save.cs, AutoSaveSettingsWindow, EditorSaveSession | Есть маршрут; M03/M20 |
| ZoomCX | Misc_Menus, Windows_ZoomButton: пользовательский zoom X | EditorWorkspace.Zoom.cs: userZoomWidth и ApplyUserZoom | Есть маршрут; M21 |
| ZoomCY | Misc_Menus, Windows_ZoomButton: пользовательский zoom Y | EditorWorkspace.Zoom.cs: userZoomHeight и ApplyUserZoom | Есть маршрут; M21 |
| CompilerWindowX | Windows_Compiler: положение X окна задачи | CompilerWindowPlacement: last closed normal native RECT в памяти MainWindow, monitor work-area clamp; actual Test/Compile/Run task, не геометрия отчёта | [Маршрут подключён](COMPILER_TASK_WINDOW.md); M20/M21/M23 |
| CompilerWindowY | Windows_Compiler: положение Y окна задачи | CompilerWindowPlacement: last closed normal native RECT в памяти MainWindow, monitor work-area clamp; actual Test/Compile/Run task, не геометрия отчёта | [Маршрут подключён](COMPILER_TASK_WINDOW.md); M20/M21/M23 |
| CompilerWindowWidth | Windows_Compiler: ширина окна задачи | CompilerWindowPlacement: last closed normal native RECT в памяти MainWindow, monitor work-area clamp; actual Test/Compile/Run task, не геометрия отчёта | [Маршрут подключён](COMPILER_TASK_WINDOW.md); M20/M21/M23 |
| CompilerWindowHeight | Windows_Compiler: высота окна задачи | CompilerWindowPlacement: last closed normal native RECT в памяти MainWindow, monitor work-area clamp; actual Test/Compile/Run task, не геометрия отчёта | [Маршрут подключён](COMPILER_TASK_WINDOW.md); M20/M21/M23 |
| ThumbnailWindowX | Windows_Thumbnail: положение окна миниатюры | ThumbnailWindowPlacement: native XY в памяти workspace, owner-right default, clamp ближайшего monitor work area; rebind при смене карты | [Маршрут подключён](THUMBNAIL_WINDOW_LIFETIME.md); M21/M23 |
| ThumbnailWindowY | Windows_Thumbnail: положение окна миниатюры | Тот же маршрут; размер между открытиями не сохраняется, JSON/карта/Undo не меняются | Есть маршрут; M21/M23 |
| UndoLevels | Map_Undo: глубина истории | HistorySettingsWindow и NativeMapDocument.HistoryDepth; современный полный Undo согласован | Есть маршрут; M10 |
| RecentFiles | Default10, AddRecent и Registry_GetRecentFiles | MainWindow.Save.cs: лимит10. В исходнике нет команды выбора этого лимита; существующие команды очистки уже перенесены | Есть маршрут; M20 |
| RecentPaths | DefaultFALSE, Config_UpdateRecentMenu | Меню показывает basename и полный путь в tooltip. Отдельного source caller переключения поля нет | Есть маршрут; M20 |
| SoundsVolume | Map_Sounds: DirectSound SetVolume, default−500 | EditorSoundRules.LinearVolume и EditorSoundPlayer:0.56234 amplitude, без изменения системной громкости | Есть маршрут; фактическое аудио M14/M21 |
| SoundsFlags | Map_Sounds: terrain/interface категории | EditorWorkspace.Sounds/MapCanvas.Sounds, palette и object-brush callers;15 штатных WAV в publish | Есть маршрут; [правила и приёмка](EDITOR_SOUNDS.md) |
| MapViewFlags | MainWindowProc, Things_Display, Map_Display, Map_Cameras | ConfigureObjectDisplay/Overlays/TerrainVisibility, source категории и дополнительные display modes в отдельных scopes | [Все15 битовых маршрутов сопоставлены](MAIN_MENU_SOURCE_MAPPING.md); M21/M23 |
| RequestsFlags |20 исходных ссылок на запросы | EditorRequestPreferences и source policies, SOURCE_REQUEST_AUDIT; R02 согласован отдельно | Есть маршруты; M07/M10/M12/M18/M20 |
| SaveReportFlags | Files_Save и Compiler_Report: тип отчёта, версия и severity | EditorReportRules и реальные Save compatibility notices, полные Load/Test/Compile/Run reports; прежние loss/blocking guards сохранены | [Маршруты подключены](DOCUMENT_REPORTS.md); I02 полнота message generators и ручная приёмка остаются |
| PathsColor | Things_Display: COLORREF патрульных путей | EditorWorkspace.ApplyPathColour переводит RRGGBB ↔ COLORREF, MapOverlayOptions.PathColour | Есть маршрут; M12/M21 |
| ManagerSortMethod | Things_ManagerSort: пять способов сортировки | ObjectManagerSession.Sort, ObjectManagerWindow filter/sort; порядок сериализации не меняется сортировкой вида | Есть маршрут; M20 |
| ManagerModifyParams | ModifyFlags, coords, replacement, оба bit modes/masks | ObjectModificationSession, ObjectManagerOptionsWindow и ManagerExecutionFlow. Hidden bits и исходный single/multi выбор сохранены | Есть маршрут; M20 |
| ManagerAddParams | AddFlags, Mode, replacement, frequency | ObjectBatchSession и ManagerExecutionFlow; исправленная действующая random frequency сохранена | Есть маршрут; M20 |
| ManagerSubType | Общий subtype door/trap и object/special | ObjectManagerSession.SubType, KindFor и ChooseKind; отдельные шаблоны пяти категорий | Есть маршрут; M20 |
| RandomFreq | Map_Display: случайный рисунок | RandomPainting и RandomFrequency workspace/canvas, default2; не смешивается с AddRandomFreq менеджера | Есть маршрут; M06 |
| MapPath | Source Load/Save/title: принятый путь карты | MainWindow синхронизирует отдельный MapTarget с EditorSaveSession после принятого состояния; chooser filename/directory, прежний LastOpenedFile fallback сохранён | [Маршрут подключён](FILE_DIALOG_PATHS.md); M03 |
| AreaPath | Map_Select: общий путь import/export выделения и буфера | EditorFileDialogSession.AreaPath во всех4 chooser callers, после OK перед IO; сохраняется при ошибке чтения/записи | [Маршрут подключён](FILE_DIALOG_PATHS.md); M04/M07 |
| GamePath | Config_GetGamePath, compiler, speech/text, registry | GameResourceSettings.Root, MainWindow.GameRun.cs и file/resource selection; DKII-DX.exe и согласованное имя preview | Есть маршруты; M03/M14 |
| PlayerPath | Config_GetPlayerPath, Properties_Map_Voices | GameResourceSettings.Player, GameAudioPreview с собственным lifecycle временных файлов | Есть маршрут; M14 |
| ThumbPath | Thumbnail_Functions: путь BMP до writer | Общий ThumbnailPath workspace/дочернего окна, chooser OK перед IO; подтверждённая замена BMP через staging, generic API new-only | [Маршрут подключён](FILE_DIALOG_PATHS.md); M21 |
| Thumbnail | THUMBOPTS:6 полей KeepOldBitmap/UseColorKey/HiddenColor/ActualPlayer/WorldFlags/PlayersFlags | MapThumbnailOptions и ThumbnailWindow, 37 source options и BMP export в отдельном scope; вложенные поля не пропали за одной строкой CONFIG | Есть маршрут; M21 |

## Что закрывает таблица

Весь состав CONFIG теперь перечислен по именам, без очереди «прочие настройки»
неопределённого размера. У неактивных полей не создаётся выдуманный GUI.
Из таблицы нельзя вывести, что всё I17 закрыто: там также остаются команды
других менеджеров и оставшиеся pending-field/menu/shortcut routes. Single-list
membership поддержанных физических records сопоставлена в MANAGER_LIST_COMMANDS.
Их нужно сопоставить в пределах существующего I17, а не добавлять новые широкие
задачи поверх [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).

Sound_Play подключён: [EDITOR_SOUNDS](EDITOR_SOUNDS.md). Кодовый пропуск
звуков снят; слышимость/устройство остаются ручной приёмкой. HideDisk подключён, HideSplash compiled out. Все42 fields явно сопоставлены;
состав/битовые маршруты MapViewFlags закрыты [сверкой](MAIN_MENU_SOURCE_MAPPING.md); physical GUI остаётся M21/M23. [Шесть оставшихся menu callers подключены](WORKSPACE_MANAGER_ROUTES.md).
Их entry guards/pending-field commit сопоставлены. Остаточная полнота
own commands/callbacks и keyboard callers остаётся внутри существующего I17.
Все primary entries сопоставлены в [MAIN_MENU_SOURCE_MAPPING](MAIN_MENU_SOURCE_MAPPING.md).

UNDOFREE подключён в [HistoryClear](HISTORY_CLEAR.md): история очищается с
сохранением текущей UndoLevels/HistoryDepth. Поле настроек не записывается;
[Пять brush-kind callers подключены](OBJECT_BRUSH_COMMANDS.md).
[Справка и сводка версий тоже подключены](HELP_SECTION_VERSIONS.md).
[Thing Manager lists, single physical membership и3 direct commands сопоставлены](MANAGER_LIST_COMMANDS.md).
[Доказательства и ручная приёмка I/O](FILE_IO_PROGRESS.md).
[Окно compiler task и его геометрия подключены](COMPILER_TASK_WINDOW.md).
[Lifetime и положение thumbnail подключены](THUMBNAIL_WINDOW_LIFETIME.md).
[MapPath/AreaPath/ThumbPath подключены](FILE_DIALOG_PATHS.md).
[Report callers и фильтры подключены](DOCUMENT_REPORTS.md); I02 сохраняет
полноту исходных compatibility message generators. [Default title уже исправлен](WINDOW_MAP_TITLE.md) с
обновлением после Rename/Undo и без полного разбора сценариев.
Исходные SaveReport defaults сами по себе не
доказывают необходимости нового диалога настройки фильтров.

Сопоставление и решение I14 не запускали новые проверки продукта. После добавления наблюдаемого Load выполнен новый полный1822/63:
прежние1821 PASS и63 Failures сохранены точно. Без новых изменений повтора нет.

## SourceKeyboard-20261018

[Конечная таблица клавиатуры](SOURCE_KEYBOARD.md):19 новых бесконфликтных
комбинаций подключены;2 конфликта ожидают ответа пользователя.
[3 Insert callers и8 scroll families подключены](SPECIAL_KEYBOARD.md);
полнота собственных manager/scenario callbacks остаётся в I17.

## ManagerCommandScope-20261018

[Конечная таблица внутренних parent controls](MANAGER_COMMAND_SCOPE.md):46 IDs,
11 notifications. Tree Insert/Delete подключены; осталось6 конкретных групп
MC01–MC06 внутри I17 и2 pending shortcut conflicts. Общая историческая фраза
«другие manager callers» не добавляет новую очередь вне этой таблицы.


## ManagerDestinationCoordinates-20261018 — кодовое закрытие I17

Конечный объём этой таблицы включён в [завершённую сверку I17](I17_CODE_CLOSURE.md).
MC01–MC06 и destination106/107/186/187 закрыты по коду; Ctrl+N/Y согласованы.
Исторические pending строки выше не добавляют новых задач. Физическая
приёмка в M остаётся открытой.
