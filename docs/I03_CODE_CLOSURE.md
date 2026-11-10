# I03 закрыт по коду: перенос сценариев между форматами

Дополнение 25 октября: [исправлено сопоставление compiled owners](NATIVE_COLLECTION_GUARDS.md). Writer сортирует категории, и восстановление рабочих IDs теперь использует тот же порядок. Устранены лишние виртуальные orphan roots; смешанный список/native→DKM и все9 scopes PASS. Сами игровые payloads и порядок writer не менялись. Историческая closure matrix ниже дополнена этим исправлением.

25 октября 2026. [Таблица14 маршрутов](I03_ROUTE_MATRIX.md) сведена с текущими
поставщиками и датированными доказательствами. Последний открытый R14 исправлен
и проверен: [владелец category+UID при paste](SELECTION_OWNER_IDENTITIES.md).
Незакрытых направлений внутри установленного объёма I03 не осталось.

| Требование I03 | Что подтверждено |
| --- | --- |
| Владельцы | Все9 scopes/Present/empty/orphan сопоставлены I01; native/project conversion восстанавливает проверенные таблицы. Выделение включает только своих физических owners; новые5 групп проверяют четыре категории, UID collision/ambiguity и source/target isolation. |
| Aliases | Native↔DKM сохраняет весь146×64 raw tail. KTRG2/3 проверяет его точную длину. DKZ не переносит глобальное пространство имён и использует контекст цели; новый Save/reopen проверяет сохранение DestinationAlias. MAP ограничен своей грамматикой и сообщаемыми потерями. |
| Graph links/IDs | Канонический signed remap переводит roots/edges вместе и резервирует dangling IDs. Zero/duplicate IDs, unknown types и cycles не угадываются. Shared DAG сохраняет все связи; первый predecessor и Information — описанная policy. Source recipes всех103 типов и последующие целевые fixes имеют сохранённые proofs. |
| Координаты | DKCP хранит local/external provenance; crop не делает external точку local, transforms не выводят locality заново из изменённых bounds. Bias1 native DESTROYGATE, обычные LONG/WORD casts и derived gate/gem targets сопоставлены. Текущие связанные группы проверяют соответствующие selection callers. |
| Политика смены формата | Каждый из14 маршрутов хранит поддержанное содержимое либо даёт явный Loss/Error/отказ. DKM source companion сохраняет исходные bytes. Supported/no-scenario MAP различаются явно; mixed-format DKZ со scripts требует полной компиляции проекта. Нет скрытого fallback или выбора похожего владельца. |
| Исторические upgrades | KTRG1:4/10 рецептов, IDs1…32767 и−1…−32768, empty-link correction, one Undo и atomically rejected overflow. KTRG2 расширяет link layout, остальные события/aliases сохраняются. Unknown версии/layouts не угадываются, их editable поддержка относится к I04. |

Найденные расхождения исправлены последовательно:

1. [Отсутствующие ворота native→DKM](NATIVE_MISSING_GATE_CONVERSION.md):
   явный Loss вместо лишнего полного отказа, сохранение missing ID, защита
   от восстановления старых координат на новые посторонние ворота.
2. [Цель ворот в MAP](TEXT_MAP_GATE_IDENTITIES.md): рабочая physical target
   вместо повторного поиска координат, явный NativeEditorState boundary.
3. [KTRG1 action count](HISTORICAL_SCENARIO_COUNTS.md): все32768 отрицательных
   IDs допустимы и в одном условии; переполнение остаётся атомарным отказом.
4. R14: typed owner UID mapping и сохранение неактивных target fields.

Границы не скрыты: raw native files не сериализуют частные рабочие ID и
orphan state (I01/R19), Source word ChildAnd у native action не является edge,
DKZ не копирует все global definitions/aliases/regions, MAP не является полным
проектом. Для рабочего состояния служит DKM с его immutable source companion.
Входные повреждённые карты не исправляются автоматически. Проверки I18 и
неизвестные layouts I04 сохраняют самостоятельный объём.

`artifacts/Evidence/SelectionOwnerIdentity-20261025/i03-closure-proof.json`
фиксирует требования, source/provider hashes и terminal records. Доказательства
I01/I02 повторно сопоставлены по текущим providers; четыре изменённых с I01
файла покрыты последующими I02 и missing-gate fixes. После route inventory
изменились только два участвующих selection providers, с точной проверкой diff.

Текущая сборка: новые5PASS и связанные5PASS; все41 проекта и publish завершились
с ExitCode0. Прежние successful conversion/text/upgrade runs сохраняют свои
даты и Core hashes. Они не переименованы в прогоны текущей DLL и не сложены
в новый full. Исторические1840PASS/63FAIL не стали зелёным текущим корпусом.

**Ручная M04 остаётся открытой**: настоящие исторические файлы, официальный
GUI и игра. Автоматическая часть I03 не доказывает ручную совместимость.
Остаются6 других кодовых пунктов (I04/I07/I08/I09/I10/I18) и23 ручные группы,
всего29 пунктов работы. Все23 исходные возможности и полная цель сохранены;
редактор целиком ещё не объявлен завершённым.
