# Исходные изображения сообщений и камер

23 октября 2026. `RequestCameraImages-20261023`, часть I13.

Все 35 прежних вызовов MessageBox в 17 файлах текущего приложения используют
общий `OriginalMessageBox`. Тексты, заголовки, типы кнопок, проверки ответов,
делегаты тестовых presenters и тела команд сохранены. Новый показ использует
исходные 32×32 ресурсы: information80, warning81, error82, question83.
Текстовые сообщения без значка остаются без значка. При предупреждении о
потере изменений используется исходный New.ico75 и question sound, как в
`Misc_CheckChanges`; прежний вопрос, Yes/No и bool-проверка ответа сохранены.

Кнопки OK/Cancel/Yes/No имеют собственные EN/RU ресурсы. Общий показ сохраняет
наборы OK, OKCancel, YesNo и YesNoCancel, доступную кнопку по умолчанию и
соответствующий MessageBoxResult. Закрытие или Escape возвращает Cancel при
наличии Cancel, иначе No при наличии No, иначе OK. Закрытие не даёт Yes и не
разрешает потерю карты. Enter выбирает кнопку по умолчанию; после загрузки
фокус переводится на неё. Окно модальное, принадлежит прежнему owner,
предусматривает перенос и прокрутку длинного сообщения. MessageBeep вызывается
при реальном показе; настоящая аудиопроверка ещё не проведена.

Это общий показ сообщений. Уже реализованные специальные окна с флажками
запоминания, их политиками, consent/owner guards и частными черновиками
сохранены. Их вызовы не подменялись новым показом.

Два camera image consumers уже реализованы. `Map_Cameras.c` ресурсы20–24
подключены через `OriginalCameraImages` к существующим camera overlays.
`Libraries_CameraPaths.c` использует 52 исходных ID/ShowCell и 52 BMP256×192;
их показывает существующее `CameraPathWindow`. Все RC mappings, BMP/ICO bytes
и PE resources сверены; EN/RU описания и ShowCell совпали с исходной таблицей.
Повторной реализации camera commands, picker или writer не было.

Целевая headless проверка подтверждает допустимый default, невозможность
подтверждения destructive Yes через Close, варианты severity, пять request
ICO, пять camera ICO и 52 BMP. Она читает PE resources, не исполняет WPF.
Отдельное сравнение с предыдущим Source ZIP восстанавливает прежние строки
вызовов и подтверждает буквальное сохранение command bodies. Весь Core,
Source/Binaries/Fixtures, игровые файлы и семифайловый TestMap сохраняются.

Ручная M-приёмка: открыть разные сообщения OK/YesNo/OKCancel, проверить
исходный значок, EN/RU кнопки, Enter/Escape/Close и фокус. На несохранённой
тестовой копии проверить отмену New/Open/Close через No/Close, затем явный Yes.
Проверить длинную диагностику, прокрутку и DPI. Подтвердить звук и модальность.
Для камер проверить видимость значков и всех схем, поиск, смену языка и
Cancel. Агент эти окна и звук не запускал.

Прямой consumer `Misc_Requests.c:1247`, FILE_THUMBNAIL, ещё открыт: встроенный
предпросмотр выбранного BMP внутри файлового chooser отсутствует. Это часть
общего source file-preview pane, который также показывает карты и AREA;
память путей уже реализована, но не доказывает существование preview pane.
Нужна доработка кода для этого preview workflow; его нельзя объявлять только
ручной проверкой. Также открыто конечное покрытие имён документов, ссылок и
source callers. I13 целиком не закрыт; сохраняются 14 I-пунктов и 23 M-группы.

English: the 35 existing message call sites now share original request icons and
bilingual buttons while retaining caller texts and result handling. Source camera
icons and all 52 diagrams were verified without reimplementing their commands.
Real modal, keyboard, DPI and audio acceptance remains manual. The source file
chooser preview pane is still a concrete implementation gap.

Результат: build01 всех41 проектов/focus01/publish01 — ExitCode0,
0 предупреждений/ошибок. Поставка и оба ZIP обновлены с SHA256 каждого entry.
Полный corpus и прежние успешные проверки Core не повторялись.
