# Удаление физических объектов из панели свойств

Этап InspectorObjectRemoval-20261016. Основной проект один: `src/` и
`DK2MapEditor.slnx`. Здесь описан новый маршрут интерфейса. Core не изменялся;
физическая проверка WPF остаётся ручной.

## Как пользоваться

В списке записей панели свойств выберите категорию существ, дверей, ловушек
или предметов. Ctrl/Shift позволяют выбрать несколько строк. Нажмите
«Удалить». В DKM физические типы находятся в общей категории Project.MAPTHING;
в native карте категории GAMEKEEPER/GAMEHERO/GAMENEUTRAL, GAMEDOOR, GAMETRAP
и GAMEOBJECT поддерживают групповой выбор. Специальные предметы входят
в GAMEOBJECT. Выбираются видимые строки, а не объекты из буфера копирования
или прямоугольника карты.

Первый вопрос — тот же, что в менеджере объектов. Он спрашивает об удалении
всей выбранной группы и поддерживает «Запомнить Да». При использовании
выбранных ID в сценарии по правилам исходного менеджера появляется второй
вопрос с «Да», «Нет», «Отмена». «Нет» сохраняет все записи с назначенным ID,
«Отмена» и закрытие окна отменяют удаление. Флажок запоминается только после
ответа «Да»; закрыть окно с установленным флажком недостаточно.

Undo возвращает полное прежнее состояние, порядок предметов и прежнюю цель
кристалла. Удаление группы — один шаг Undo со счётчиком по удалённым объектам.
В отличие от удаления под курсором, оно не создаёт отдельный Undo на каждый
предмет. Принятый «Нет», исключивший все записи, сохраняет исходный parent Undo
с нулевым приростом счётчика. Сценарные точки/операнды сохраняются согласно
source manager policy; это не generic detachment.

Изменение выделения, подмена документа или закрытие редактора отменяет
открытый запрос. Переключение EN/RU сохраняет ту же группу, если документ
не изменился. «Запомнить Да» использует общие runtime preferences менеджера;
новый запуск начинает с нуля. Буфер копирования и прямоугольник карты
этой командой не меняются.

Если нажатие сначала применяет незавершённую правку поля, удаление
останавливается с сообщением: проверить текущий выбор и нажать «Удалить» ещё
раз. Это предотвращает обращение к старым адресам строк после изменения
размера/типа записи. Неверное поле остаётся на исправление без удаления.

Нельзя смешивать физические объекты и служебные записи в одном удалении.
Регионы по-прежнему удаляются группами одного вида через собственный запрос;
отряд целиком — через подтверждение отряда, остальные raw/definition/script
записи — индивидуально с прежними guards. Группа неподходящих записей
не превращается в удаление только первой строки. Непроверенный физический
layout отвергается общим EditorThingRequestPolicy, а не обходится generic API.

## Как устроено

`EditorWorkspace.InspectorRemoval.cs` хранит EditorThingRequestPolicy,
связанный с ObjectManagerSession.RequestPreferences, открытое окно и context
generation. SelectedRemovalRecords фиксирует выбранные записи в порядке
видимого списка. TryRemoveSelectedObjects сверяет их с ObjectManagerEntries;
смешанный набор отклоняется до создания запроса. Begin заново проверяет
физические layouts, revision и fingerprint. RegionRemovalWindow получает
тот же IEditorConfirmationRequest и проверку актуальности selection/context.

`DeleteRecord_Click` сначала завершает PropertyGrid edit, проверяет document
и revision, затем выбирает physical/region/individual route. Regions helper
больше не принимает любую не-региональную группу за смешанные регионы.
Record_Changed, Document setter и Unloaded закрывают physical confirmation;
перестроение списка при EN/RU сохраняет выделение без ложного context change.

Source: Things_ManagerKill в `Source/Sources/Things/Things_Manager.c`:
REQUEST_MNGRKILL, Triggers_IsUsed(SCOPE_CREATURE|SCOPE_KILL), фильтр всех
uniqueid при KillAll=false, один parent Undo, MapChanges на удалённую запись.
Switch поддерживает creature/door/trap/object/magic. Общий обработчик уже
покрыт Core проверками. Новый raw Inspector — современный вход в этот
обработчик, а не заявление о наличии такой же панели в исходном редакторе.

## Доказательства и пределы

Evidence: `artifacts/Evidence/InspectorObjectRemoval-20261016`.
Build-02 всех41 проектов: exit0, предупреждений/ошибок0. Два App текста
добавлены EN/RU, всего2579 ключей; все2577 прежних literal values сохранены.
Targeted CLI: manager23 и point33 groups, exit0; publish exit0.

Весь Core source и DLL побайтно совпали с ManagerCategoryScopes-20261016.
Полный корпус заново не запускался. Прежние1811 PASS/63 отказа и исходные94
пересопоставлены целиком по raw ordered Inputs/Passed/Failures; SHA входов
корпуса проверены. Это унаследованная проверка того же Core, а не физическое
исполнение нового App маршрута.

InspectorObjectRemovalSmoke добавлен к WindowsSmoke и собран. Он подготовлен
для native/DKM × EN/RU: пять физических типов, group Undo/counts, checked
close, used No, language/selection/document lifecycle, pending field commit,
mixed refusal и общая preference с менеджером. WPF/renderer/GUI инструменты
не запускались. Работа интерфейса, мышь/keyboard/focus/audio/DPI, официальный
редактор и игра требуют отдельной ручной приёмки.

Source/runtime ZIP сверяются по всем entries; Source/Binaries, fixtures,
установленные карты и прежние7 TestMap сохраняются. Core/compiler не менялся,
семифайловый игровой формат не расширялся. Реальные игровые тесты только
DK2Preview_6a88240eaf814f78 через `DKII-DX.exe -q -level`.

Не закрываются прочие manager branches/single-list membership, Config request
callers, native/imported variants, compiler/historical/catalogue/compound/
callback/counter требования и все23 области цели. См. [остаток](REMAINING_WORK.md),
[общие запросы](SHARED_SCENARIO_REQUESTS.md), [удаление под курсором](UNDER_CURSOR_REMOVAL.md),
[сохранение категории/UID](MANAGER_CATEGORY_SCOPES.md) и [приёмку](MANUAL_ACCEPTANCE_RU.md).
