# Списки и прямые команды менеджера объектов

Этап ManagerListCommands-20261018, часть I17. Основа: Things_ManagerProc/Init,
SetCreaturesList/SetDoorsnTrapsList/SetObjectsList, MainWindowProc и menu resource.
Hashes и конечный список source controls сохранены в evidence source-scope.json.

## Исправлено

Оригинал при открытии выбирает текущее выделение, если оно есть. Без выделения
он выбирает всю карту и отключает выбор области. Теперь ObjectManagerWindow
делает так же при каждом новом открытии: прежний выбор «Вся карта» не подменяет
актуальное выделение. Search/sort, общие templates, X/Y и остальные session
preferences сохраняются по прежним правилам. Пустое/недействительное выделение
не становится скрытым разрешением на работу со всей картой.

Три source Set*List функции проверяют границы даже в режиме WholeMap. Теперь
список менеджера также исключает координаты меньше0 или не меньше Width/Height.
При выбранной области применяется пересечение с границами карты; суммы
координат вычисляются в64-bit. Клетки0 и Width−1/Height−1 входят в WholeMap,
как в source. Данные за картой остаются в документе/Inspector: фильтр ничего
не удаляет и не исправляет в импортированном файле.

В меню «Правка» подключены три исходные команды IDM_CREATURES,
IDM_DOORSTRAPS и IDM_OBJECTS: «Существа», «Двери/ловушки», «Предметы/особые
предметы». Они и прежняя кнопка используют один workspace factory, историю,
подтверждения и общий session. Menu route задаёт только нужную family.
Без карты команды отключены; чужой workspace document, закрытый owner и
активный pen не создают новое окно. Pending Inspector fields принимаются
существующим CommitFields до создания менеджера. Неподтверждённый ввод не
превращается в карту по умолчанию. В source accelerator table для этих трёх
ID нет отдельных клавиш; новых сочетаний не выдумано.

## Конечное сопоставление Things_Manager controls

| Source control/event | Современный caller / scope |
| --- | --- |
|100 selected/map | ManagerSelectionOnly + ObjectManagerListRules; source default исправлен этим этапом |
|101 sort | ManagerSort → ObjectManagerOrdering; сериализация не сортируется |
|300 pair subtype | ObjectManagerSession.KindFor/ChooseKind и общий template editor; общий subtype пары сохранён |
|105 template properties | ManagerTemplate → CreateTemplateWindow; [execution flow](OBJECT_MANAGER_EXECUTION_FLOW.md) |
|106/107,186/187 destination | ManagerDestinationX/Y, navigation draft; [navigation](OBJECT_MANAGER_NAVIGATION.md). Исправлено ManagerDestinationCoordinates: ранний source clamp, числовой session и spinners186/187 |
|108 show destination,109 choose point | ShowDestinationOnMap/BeginPointPick, private preview/OK/Cancel; прежний navigation scope |
|114/115,124/125 | AskOptions/Preview для modification/addition; shared session flags |
|116, NM_DBLCLK | RequestModification → ExecuteManagerFlow; single и multi ветки, свежий selection request |
|117,127 | ShowOptions/CreateOptionsWindow с private draft; [options](OBJECT_MANAGER_OPTIONS.md) |
|126 Add | RequestAddition, source option/preview sequence, прежний atomic Core writer |
|156 Remove | BuildRemovalPreview/ApplyRemoval → request policy; прежние R01–R05 и destructive consent |
|157 show selected | ShowSelectedOnMap, ровно одна выбранная строка |
|158 select all | ManagerSelectAll, только видимые строки; новые фильтры не расширяют выделение |
|180 Undo | EditorObjectManagerHistory, только принадлежащие текущему manager шаги; прежний scope |
|200/201 creature/hero options | Общий template editor: CreatureOptions/HeroOptions и именованные bit controls |
|202 objective/area/enemy | Тот же template editor: Objective/Area/Enemy с прежними context choices |
|IDOK/IDCANCEL | Cancel/Release, закрытие дочерних drafts/request policy и отписка от документа |

Создание окна и mutating routes используют прежний canApply → CommitFields.
После такого commit изменение документа сбрасывает старый selection/preview;
ApplyRemoval дополнительно сверяет ссылку на plan после canApply. Add/Modify,
options и navigation сохраняют свои существующие lifetime guards. Read-only
sort/filter/select-all/preview не принимаются за новые операции Undo. Эта
таблица связывает callers; она не является новым физическим исполнением GUI.

## Принадлежность одному физическому списку

Project membership определяется проверенным node.type0…4. В native hero,
neutral и keeper относятся к Creature; door/trap — к своим видам; object IDs
0x75…0x81 — SpecialItem, остальные — Object. Последнее правило совпадает с
Files_Game.c. Band heroes, глобальные bands, regions/effects и definitions не
подменяются физическими things. Неизвестный layout с пустыми fields не угадывается.

ObjectManagerEntries создаёт максимум одну строку на физическую record;
family projection даёт ровно одну из трёх source families. UID/сценарная ссылка
не являются ключом дедупликации: разные физические записи с одним UID не
склеиваются. После изменения перечень строится заново из текущих records.
Source multi modification вызывает Undo_StoreMoveThing, который снимает запись
со старого списка, затем добавляет в новый. В обычном Things_Select тип контекста
задаёт диалог; source single-list состояние не является вторым хранилищем данных.
Современные расширенные type changes сохраняют ранее описанную canonical
membership и orphan-scope policy из [MANAGER_CATEGORY_SCOPES](MANAGER_CATEGORY_SCOPES.md).

Source continuation при нехватке Undo memory не переносится как требование к
двойной принадлежности pointer lists: R02 уже согласован как полный rollback.
I14 и согласованное восстановление порядка Undo этим этапом не меняются.
Оставшаяся неоднозначность identity/catalogue/неизвестных layouts относится к
I01/I04/I06/I18; этот этап её не объявляет исправленной.

## Доказательства и остаток

Новый pure CLI check использует канонический App predicate: whole-map/selection,
краевые клетки, координаты вне карты, пустое/утраченное выделение, overflow-safe
границы и единственная family для пяти kinds. Это проверка view policy, не Core
writer. Core не менялся: последний полный1822 PASS/63 прежних отказа остаётся
этапом DiskProgress; корпус и старые успешные checks не повторялись.

WindowsSmoke добавлен для EN/RU native/project: реальные menu click/factory,
правильная family, selected/map defaults, скрытый imported out-of-map record
без удаления, reopening, translation, live-pen/closed-owner guards. Прежний
ObjectManagerSmoke адаптирован к source selected-area default. Они только
скомпилированы; физическое меню, ввод, original GUI/DPI/game остаются M20/M21/M23.

I17 целиком не закрыт. Следующие конкретные main-menu routes:
IDM_HEROS → Heros_Select, IDM_AREAS/GATES/EFFECTS → RectEx_Select,
IDM_PATHS → RectEx_MakePaths, IDM_EVENTS → Triggers_Select. Нужно сопоставить
их с существующими workspace editors и их собственными commands/pending fields.
Уже закрытые Thing Manager policy/Undo/requests не ставятся на новый прогон.

## Ручная приёмка

1. С выделением открыть каждую из трёх команд «Правка»: проверить family и
   selected-area default. Перейти ко всей карте, закрыть/открыть: снова выбрано
   текущее выделение. Без выделения checkbox недоступен, показана вся карта.
2. В копии карты с записью вне границ убедиться, что её нет в WholeMap manager,
   но запись осталась в Inspector и после сохранения без изменения.
3. Проверить SelectAll/Modify/Remove/Show selected/Undo при0/1/нескольких строках,
   pending Inspector edit и отказ валидации. Карта должна меняться только после
   прежних подтверждений. Закрытый/заменённый документ не сохраняет authority.
4. Повторить в EN/RU и DPI100/125/150, проверить доступность длинного меню
   клавиатурой/мышью. Сборка не подтверждает эти физические результаты.

## Продолжение 18 октября

[Шесть следующих workspace manager entries подключены](WORKSPACE_MANAGER_ROUTES.md).
Старое указание «следующие menu callers» выше — исторический следующий шаг,
в текущей поставке эти entry routes реализованы. Остаток I17 — в OPEN_FUNCTIONS_RU.


## Уточнение ScenarioNewPointCommand-20261018

Описанное выше raw destination text — установленное отличие от source
Things_ManagerProc106/107 и UpDown186/187, а не согласованная политика.
После закрытия MC01–MC06 это конкретный следующий участок I17. Source/App
paths/hashes — evidence ScenarioNewPointCommand/remaining-i17-site.json.


## ManagerDestinationCoordinates-20261018

Уточнение предыдущего этапа закрыто: destination106/107/186/187 используют
ранний source clamp и accepted numeric session. Private picker Cancel/Confirm
и Add/show guards сохранены. [Доказательства](MANAGER_DESTINATION_COORDINATES.md).
[I17 закрыт по коду](I17_CODE_CLOSURE.md); physical M20/M23 остаётся.
