# Текущие имена документа и подписи ссылок

23 октября 2026. `DocumentReferencePresentation-20261023`, часть I13.

Карточка открытой карты теперь читает внутреннее имя, автора, размеры и имя
файла из текущего документа и принятого save target. Раньше при наличии
первоначального inspection report эти значения могли остаться от загрузки,
хотя заголовок окна уже показывал новое имя. Обновление подключено к
Document_Changed и RenderInspection: свойства карты, resize, Undo/Redo,
Save As, открытие и смена языка получают актуальные значения.

Имя файла и внутреннее имя карты — разные значения. Save As меняет показанный
файл/путь, но не переименовывает внутреннюю карту. Полный путь доступен в
tooltip имени файла. История диагностики загрузки, размер прочитанного файла
и прочие поля inspection report сохраняют свой прежний смысл; сам отчёт не
выдаётся за заново выполненную проверку после каждого редактирования.

## Чтение метаданных

`EditorDocumentPresentation.Read` — чистое представление документа.
`EditorWindowCaption` использует общие проверенные читатели Name/Author.
Они не вызывают Records(), создание черновика или полный fingerprint графа.
Для native GAMEINFO v4 читаются существующие UTF16 fields: Name/Author по
128 bytes. Независимый source anchor Author — offset2232. Для DKM принимаются
ровно известные KINF layouts1–7; Name — 64-byte Western ANSI. Author появляется
только в layouts5–7, а в1–4 отсутствует. Отсутствующий автор не выдумывается.
Неподдержанный version, неверный declared size/extent или неоднозначная KINF
возвращают отсутствие значения. Полные импортированные64-character поля и
пустое имя сохраняют смысл. Кодеки/offsets берутся из действующих схем.

Заголовок окна продолжает исходную политику UseFilename=false. В source
UseFilename/ShowFullName инициализируются FALSE и не имеют другого активного
setter. Filename/fullpath preference не добавлялась как новая вымышленная
настройка. Main title, compiler title и карточка используют один reader
внутреннего имени с прежними fallback правилами.

## Ссылки

Общие и сценарные каталоги теперь используют единые EN/RU подписи вида
Area/Область, Band/Отряд, Gate/Проход, Effect generator/Генератор эффектов.
Имена и комментарии из карты остаются пользовательскими строками, не
переводятся и не перезаписываются. Для областей/проходов/генераторов сохранился
RectEx_StrCpy display: CR убирается, LF заменяется на ` | `, tab — на пробел.
Исходные band names показываются буквально, как раньше.

При повторяющемся ID числовая ссылка не определяет единственную запись.
Раньше общая подпись могла показывать имя первой записи. Теперь EN/RU label
сообщает число совпадений и не выдаёт первое имя за единственную цель.
В generic choices сохранились прежние distinct values и порядок; в named
каталогах — прежние строки и значения, включая повторяющиеся. Это только
отображение: ни renumber/repair, ни нового разрешения или запрета semantic
editing не добавлено. Namespace, optional zero, band0 и sentinel255 прежние.
Native destroy-gate использует координаты и не получает выдуманного gate ID.

## Доказательства и остаток

Снимок source содержит 25 вызовов Map_NewTitle/RectEx_StrCpy и metadata
declarations. Целевая проверка покрывает native/DKM private draft → Apply →
Undo/Redo и настоящий CLI Save As, Unicode, семь исторических metadata
layouts (Author только5–7), full-width/future/truncation guards, 27 существующих
типизированных reference fields, namespaces, ambiguity, sentinels и различие
RectEx display/буквального band name. Чтение не меняет bytes, counters,
history, dirty или notifications. Общий title reader и affected scenario
named-page group проверяются из-за изменения общих helper branches.

Первые два запуска новой проверки выявили неверные предположения fixture:
native gate проверялся как ID, а ранние KINF — как содержащие Author.
Fixture исправлена по source; product guards не ослаблялись.

Для конечного source-caller audit зафиксированы315 lexical insertion-message
sites в70 функциях non-Obsolete C. Это функции заполнения списков/деревьев,
а не315 дефектов и не новая очередь тестов. Следующий результат I13 — связать
этот конечный перечень с уже имеющимися providers и доказательствами, выделив
только действительно непокрытые случаи. Повторять старые успешные тесты
ради самого перечня не требуется. I13 пока не закрыт;14I/23M сохраняются.

## Ручная приёмка

На тестовой копии изменить внутреннее имя на `Name check` и автора; применить
свойства и изменить размеры. Сравнить title, карточку и canvas, выполнить
Undo/Redo. Сохранить как `DK2Preview_6a88240eaf814f78.dkm` в другом тестовом
каталоге: внутреннее имя должно сохраниться, tooltip пути — обновиться.
Переключить EN/RU. Проверить читаемость длинных имён и ссылок; настоящие окна
агент не запускал. Дубликаты ID проверяются только на отдельном fixture,
установленные карты ради этого не ремонтируются.

English: the map card now reflects live name, author, dimensions and accepted
save path instead of stale load metadata. Shared reference captions identify
ambiguous numeric IDs without guessing the first target. Map data and editing
authority are unchanged. Actual-window and DPI acceptance remains manual.

Итог: build04 всех41 проектов/focus04/publish01 — ExitCode0;
0предупреждений/ошибок,все3affected groups прошли. Поставка иобаZIP
обновлены с SHA256 каждого entry. Full corpus без изменений не повторялся.
