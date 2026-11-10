# I01: рабочие метаданные native-карты

24 октября 2026. Закрыта конечная сверка поддержанных видов метаданных и
их привязки к владельцам. Код приложения и тестов на этом этапе не менялся.
Сборка, запуск проверок, GUI и игра повторно не выполнялись.
Аудит: `artifacts/Evidence/NativeMetadataAudit-20261024/i01-audit.json`.

Результат — таблица хранения и маршрутов для **14 видов состояния** и
**19 групп операций**. Это не 14 найденных ошибок и не 19 новых тестов.
Непривязанных поддержанных видов метаданных и маршрутов I01 не осталось.

## Где находятся данные

`ScenarioNames.TransactionFiles()` механически сопоставлен со всеми восемью
объявленными nullable NativeFile-полями рабочего состояния NativeMapDocument:
шесть используются native-документом, два — DKM. Они участвуют в историях,
снимках, fingerprints и откате по полным буферам. Остальные шесть строк ниже
явно отделены от виртуальных файлов.

| ID | Данные | Хранение | Загрузка, сохранение и компиляция |
| --- | --- | --- | --- |
| T01 | 129 имён переменных и 17 имён таймеров | Native `.dk2names`, D2SN; DKM KTRG2/3, 146×64 bytes | Ограниченный reader сохраняет неизвестный layout непрозрачно. Копия диалога и source ZIP сохраняют raw bytes; compiler переносит имена в редакторскую metadata, отдельно от семи игровых файлов |
| T02 | Целевая версия игры | `#GameTarget`, DWORD; DKM `MAPPROPERTIES.Version` | Обычный native import выбирает17; explicit target13/16/17 переживает рабочую конвертацию. Это не поле версии GAMEINFO, которое остаётся4 |
| T03 | Фон расширения карты | `#ResizeFill`, DWORD; DKM `FillID` | Native default — MAP_DEFAULTID; выбранный packed preset копируется в частный документ, архив и результат project compiler |
| T04 | Рабочие ID ворот и цель DESTROYGATE | `#GateBindings`, D2GB1/2 | Отдельные адреса ворот/действий, рабочий ID и исходные координатные WORD. Конвертация сохраняет ID в param5; compiler восстанавливает рабочую таблицу после создания коллекций |
| T05 | Точки областей, в том числе пустые и без владельца | `#AreaScopes`, D2AS1 | Адрес физической области отделён от её логического ID; хранится raw root. При удалении адрес ребазируется, retained root становится orphan. DKM — TRIGGERLINK scope4 |
| T06 | Точки шести уникальных owner scopes и UID ловушки | `#OwnerScopes`, D2OS1/2 | Scope2/8/16/64/128/256, UID, member/band operand, root и явный Present. Scope0 вv2 хранит только identity ловушки, не сценарную точку. DKM хранит UID в owner records и TRIGGERLINK |
| T07 | Изменения исходных level-modifier occurrences | `#CreatureLevelModifierDelta`, D2LM | DKM extension содержит изменения и fingerprint оригинальных native variables. Исходные occurrences остаются в source companion; применяются только к проверенной базе |
| T08 | Созданные редактором level-modifier records | `#CreatureLevelModifierOwned`, D2LO | Отдельный typed список map overrides; это не подмена исходных occurrences. Копируется, сериализуется и компилируется отдельно от delta |
| T09 | Native variables без представления в обычных DKM полях | `nativeRetainedVariables`, byte[] и manifest | Каждый полный 16-byte occurrence должен существовать в hashed original E6. Копия получает собственный массив; compile использует effective retained/delta/owned правила |
| T10 | Происхождение native bundle и событий | Immutable `EditorSourceArchive` | Полные SourceFiles, NativeEventIDs, issues и ContentHash. Архивные пути — метки, они не извлекаются и не используются как инструкции чтения файлов. Повторное восстановление строит baseline из сохранённых bytes |
| T11 | Статистика и продолжающийся сеанс | `ActivityStatisticsSession`, archive Statistics и DKM statistics | Отдельный завершённый scope I16. Первый native→DKM Save продолжает текущий сеанс; фактическая загрузка следует правилам rebasing. Фоновый compilation snapshot не разделяет live timer/history |
| T12 | Непрозрачные Effects/EffectElements для официального комплекта | Private `officialDefinitionExtras`, readonly список byte[] | Захватываются как неизменяемые секции0x10E/0xFA; private copy может разделять их, поскольку существующие маршруты их не изменяют. При публикации входят внутрь Globals.kld |
| T13 | Раскрытие условий в дереве | Native HashSet ID; DKM expanded member | Копируется в private snapshot и учитывается отдельным save fingerprint. Native→DKM записывает expanded до canonical ID remap. В обычных игровых событиях для этого состояния нет поля |
| T14 | Контекст разрешения файлов и вычисляемое состояние | MissingReferences, observed fingerprints и временные mutation flags | Копия получает свой readonly список отсутствующих ссылок. Наблюдения пересчитываются из bytes и не назначают владельца; transient flags не сериализуются как новые identities |

Статистика, раскрытие дерева и сведения об отсутствующих файлах не становятся
сценарным UID. UI preferences и текущий selection принадлежат своему окну,
а не создают ещё один неописанный native metadata файл.

## Три разных правила сопоставления

Физический адрес — адрес в конкретной коллекции, не вечный ID объекта.
У владельца он включает индекс BE-файла среди BE-файлов, ordinal среди всех
верхних записей и при необходимости member0..15. GAMEBANDHERO не увеличивает
верхний ordinal. AreaScopes считают только GAMEAREA; GateBindings — только
GAMEGATE. Адрес действия включает индекс D2-файла и byte offset. Перестановка
определений в документе не меняет индексы BE/D2-файлов.

Уникальный рабочий UID хранится отдельно. Пустая точка отличается от отсутствия
точки явным Present. Удалённая точка остаётся с orphan address, а её UID
зарезервирован. Новое существо или предмет не получает чужую ветку только
потому, что совпали тип, позиция или освободился физический адрес. Повторный
живой адрес отвергается decoder; нулевой/повторный логический UID не даёт
права выбрать владельца. План проверяет owner reference, revision, fingerprint
и состояние записей до изменения данных.

У ворот есть отдельное исходное правило: `RectEx_GetGateAtMapPos` выбирает
первые физические ворота с точными координатами. Оно воспроизведено при
обычном native import; дальше сохраняется выбранный рабочий ID. Это явное
правило исходника, а не универсальный fallback для неоднозначных UID.
Освободившийся ID ворот может снова стать целью DESTROYGATE — тоже согласно
исходнику. Уникальные owner UID, включая orphan, так не переиспользуются.

`RestoreUneditedNativeRecords` и `RestoreUneditedNativeGates` требуют
единственный source/baseline/output key, совпадение поддержанных полей и
проверенную геометрию. Для отрядов также сравниваются owner/member UID.
При неоднозначности восстановление opaque bytes пропускается; первая
попавшаяся запись по похожим данным не выбирается. Нормальные compiler roots
и изменённые значения сохраняют своё значение.

Source archive выбирается по согласованному имени companion и его ContentHash.
Два различных подходящих архива вызывают отказ. Это контракт пары DKM+ZIP,
а не криптографическая аутентификация произвольно переименованного чужого ZIP.
Храните DKM вместе с его companion; архив содержит данные карты, не старую
версию кода и не звено восстановления сборки.

## Конечные маршруты

| Маршрут | Проверенный результат сопоставления |
| --- | --- |
| R01 | Native load и lazy initialization: известные roots/addresses, bounded aliases, defaults target/fill |
| R02 | Independent document/private parent/compilation clone: отдельные физические и virtual buffers, копия retained variables, безопасное разделение immutable provenance |
| R03 | Transaction/fingerprint/rollback: все6 native и2 DKM virtual files; полный выбранный пользователем Undo |
| R04 | Native→DKM: UID/Present/empty/orphan в project fields и TRIGGERLINK, полный исходник в companion |
| R05 | Normal first/repeated SaveAs: архив до публикации проекта, bind source и continuation статистики перед adoption, failure без adoption |
| R06 | Temporary save: независимый снимок без смены live document/path/history/counters |
| R07 | DKM reopen: bounded/hash validation, occurrence provenance, явный отказ при нескольких source candidates |
| R08 | DKM→native working result: compiler roots, затем явные RestoreCompiled gate/area/owner tables; names/target/fill отдельно от GAMEINFO version |
| R09 | Manager remove/replace против generic remove: source caller сохраняет нужные orphan points, generic API сохраняет свой прежний контракт |
| R10 | Insert/delete/reorder верхних записей: отдельный rebasing трёх пространств физических ordinals |
| R11 | Member move/remove/private band Apply: verified original-slot mapping, UID и canonical reconstruction; band operand не подменяется visible ID |
| R12 | Resize/crop: source owner-removal policy, rebasing и отдельные derived gate/gem rules |
| R13 | New/Modify/Delete Point и UID allocation: пустая/отсутствующая/удалённая точка, reserved UID, запрет неоднозначного выбора |
| R14 | Stale/foreign parent authority: fingerprint/file set/allowed masks; private properties не заменяют original source archive |
| R15 | Record/event/terrain retention: проверенная база и уникальные keys вместо ordinal guessing |
| R16 | Совпадающие координаты ворот: исходный first-match import и последующая стабильная рабочая привязка |
| R17 | Семь игровых файлов: рабочие UID и orphan state не превращаются в лишние KLD; opaque definition extras упакованы в Globals.kld |
| R18 | Неподдержанные layouts: отказ семантического чтения либо непрозрачное сохранение, без догадки о field offsets |
| R19 | Presentation/derived context: live clock/cache/expanded не подменяют identity; plain game reload не восстановит отсутствующие редакторские поля |

Перечень методов, строк и SHA256 текущих providers, исходников и assertion
sources зафиксирован в JSON. Механическое сопоставление фиксирует все восемь
таких NativeFile-полей в текущем коде; изменение набора требует новой
классификации и проверки TransactionFiles.

## Доказательства и границы

Использованы девять конкретных исторических результатов с terminal exit0,
включая SharedScenarioRequests point operations33, native conversion,
unified save, record/payload/gate retention, level-modifier storage,
activity continuation и последнюю read-only definition diagnosis. Их полные
логи, исходные Core hashes и terminal records сохранены. В актуальных
assertion sources отдельно найдены проверки: новый owner не получает orphan
identity, metadata не выходит восьмым игровым файлом, member band operand,
ambiguous choice status, Save/reopen и opaque record association.

Это аудит текущего control flow и переиспользование прежних доказательств,
**не новый запуск всех этих проверок на текущей DLL**. Старые failures не
скрывались; полный corpus не повторялся и остаётся не зелёным. Последний
полный результат1840 PASS/63 FAIL — исторический.

На этом этапе новых расхождений в указанной связке метаданных не установлено.
Уже закрытые шесть owner scopes, gate/crystal/crop callers не переписывались.
Восстановление прежнего порядка при Undo и остальные принятые пользователем
отличия сохранены.

I01 закрыт в описанном объёме. I02 всё ещё требует полной field/version
сверки writer/validator; I03 — остальных межформатных сценарных маршрутов;
I04/I18 — оставшихся неизвестных layouts и повреждённых/неоднозначных inputs.
Эти пункты не заменяются ручными. M12/M13/M15/M18/M20/M23 и прочие ручные
группы также остаются: окно, язык, фокус и фактическая игра здесь не проверялись.

Для продолжения редактирования используйте DKM+companion. Игровая публикация
содержит ровно семь официальных файлов; plain KWD reload способен восстановить
только то, что хранит игровой формат. Все ручные игровые проверки используют
`DK2Preview_6a88240eaf814f78` через `DKII-DX.exe -q -level`.
