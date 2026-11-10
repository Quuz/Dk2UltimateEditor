# I12: проценты характеристик по уровням

Актуальное продолжение: [окно EN/RU и копия каталога](CREATURE_LEVEL_MODIFIER_WINDOW.md),
19 октября. Кодовая часть I12 закрыта конечной сверкой; M19/M21/M23 ещё открыты.
Далее — исторические результаты предыдущего этапа.

Актуальное продолжение: [общие значения и новые записи](CREATURE_LEVEL_MODIFIER_DEFAULTS.md),
19 октября. Ядро подключает defaults/reset/missing overrides к общему Apply;
EN/RU UI и экспорт общего каталога ещё открыты. Далее — предыдущий этап.

Актуальное продолжение: [частный контекст и DKM](CREATURE_LEVEL_MODIFIER_SESSION.md),
19 октября. Существующие map tuples уже подключены к Apply/Undo/save/compile;
shared/default/reset и EN/RU UI остаются открыты. Далее — предыдущий этап.

Актуальное продолжение: [запись native коэффициентов](CREATURE_LEVEL_MODIFIER_WRITER.md),
19 октября. Независимый writer уже добавлен; документ/DKM/UI остаются открыты.
Далее — результаты и границы предыдущего этапа.

**I12 открыт.** CreatureLevelModifiers-20261018 установил хранение и чтение
источников. Следующий [CreatureLevelModifierEditing](CREATURE_LEVEL_MODIFIER_DRAFT.md)
подтвердил official callbacks/диапазон и добавил частный draft. Окно продукта,
native/DKM writer и общий Apply ещё не подключены.

## Где находится функция

Текущий доступный C-исходник не содержит отдельной таблицы роста характеристик:
GLOBALCREATURE в Source/Includes/GlobalVariables.h и девять страниц
Properties_Global_Creatures.c хранят базовые свойства вида, jobs, light,
attacks и уровни получения трёх заклинаний. MapCreatureModifiers — отдельные
четыре переопределения вида в свойствах карты. Они не заменяют I12.

В установленном официальном `DK2 Editor.exe` статически прочитаны PE resources:

| Ресурс | Назначение |
| --- | --- |
| Dialog213 | Edit Level Variables; вход Creature Stats |
| Dialog238 | Modify Creature Stats; выбор типа/характеристики и Stat Increase Percentages For Level |
| Controls1841/1842 | Creature Type / Creature Stat |
| Edit1847,1851,1853,…,1865 | Девять полей для уровней2…10 |
| UpDown1848,1852,1854,…,1866 | Соответствующие стрелки |
| Edit1867 / UpDown1868 | Отдельный First Person |
| Dialog318 | Edit Global Variables; вход Creature Stats Level Increases |

Обычный и Pro EXE имеют эти ресурсы. Pro readme описывает модифицированную
официальную2.0 версию; он не является исходником данного репозитория.
EXE только читались как файлы: ни редактор, ни Computer Use не запускались.
Полные resource snapshots, позиции/стили/IDs и hashes —
artifacts/Evidence/CreatureLevelModifiers-20261018/official-dialog-resources.json.
Порядок ресурсов сначала был ошибочно принят за порядок уровней. Исправление
выше подтверждено DDX/store/load code. Сам ресурс не доказывает обработчики:
новая [статическая сверка](CREATURE_LEVEL_MODIFIER_DRAFT.md) подтверждает
signed−10000..10000, SHORT load/store и переходы. Native/DKM publication
и фактический Apply всё ещё остаются I12.

## Проверенный wire layout

Это записи коллекции переменных E6, а не поля внутри Creatures.kwd:

| Смещение в16-байтовой записи | Размер | Значение |
| --- | --- | --- |
|0 |4 | Тип:0x41 для процентов уровней,0xE0 для отдельного режима вселения |
|4 |4 | Индекс характеристики |
|8 |4 | Точные DWORD bits процента |
|12 |4 | Уровень |

В установленном GlobalVariables.kwd первая объявленная E6-секция содержит
288 записей0x41: все32 индекса0…31 × уровни2…10. Ещё32 записи0xE0 имеют
уровень10; их нельзя смешивать с обычной таблицей. Уровень1 не представлен
отдельной записью в этом наборе. Эффективные значения/defaults и разрешение
конфликтов данным этапом не угадываются. Определение соответствующей модели
есть также в первичном [исходнике OpenKeeper](https://github.com/tonihele/OpenKeeper/blob/master/src/toniarts/openkeeper/tools/convert/map/Variable.java),
но основание текущего decoder — сопоставление локального бинарного набора
с собственными literal fixtures. Внешняя модель не доказывает поведение
официального editor writer или допустимый диапазон ввода.

В shared файле621 запись в первой секции. Объявленный конец9992, фактический
размер13636; оставшиеся3644 байта сохраняются как непрочитанное продолжение.
Первый inspection script неверно ожидал, что размер файла равен размеру
первой секции. Исправлена граница проверки, исходный файл не изменялся.

## Текущий Core API

CreatureLevelModifiers.ReadCollection проверяет version4, E6/E7/E8 markers,
размер header/info/records, count и объявленный конец. Чтение останавливается
после declared records. ReadRetained читает подтверждённый массив16-байтовых
native occurrences. Для0x41/0xE0 сохраняются порядок, дубли, неизвестные keys
и unsigned value bits; malformed keys не становятся разрешением на запись.

NativeMapDocument.ReadCreatureLevelModifiers возвращает отдельные источники:
SharedDefaults, MapVariables, OtherNativeCollection, RetainedNativeSource.
Он не объединяет их last-wins, не выбирает произвольный дубликат и не меняет
generic record schemas. Поля EditorChangeCount, revision, history,
events/activity, existing Save/compiler/clone logic не менялись. Ни один
существующий Core file не переписан — добавлен только новый read-only файл.
EN/RU интерфейс текущего редактора остаётся прежним, новой кнопки пока нет.

Существующая DKM-конверсия относит эти tuples к нераспознанным переменным:
хранит их byte-for-byte в immutable native-source companion и компилирует
обратно. Текущий ValidateRetainedNativeVariables требует, чтобы каждое
сохранённое occurrence существовало в исходном bundle. Поэтому будущий
редактор не должен просто переписать RetainedVariables и сломать его
provenance guard. Рабочий DKM не должен получать придуманную секцию старого
редактора; игровой экспорт должен по-прежнему содержать ровно7 файлов.

## Проверка и конечный следующий шаг

CLI `--creature-level-read-only` прошёл: literal offset/size/header/truncation,
duplicates/unknown keys/unsigned DWORD, отдельные possession records,
игнорирование opaque trailer, весь реальный32×9+32 shared набор, карта и
DKM retained source без изменения source bytes/history/revision/events.
Первая focus01 неверно пыталась создать literal fixture через generic
Unused fields, которые LegacySchemas намеренно не экспонирует. Fixture
теперь записывает свои временные байты явно; decoder/product не менялся,
failed log сохранён. Focus02 успешен.

Полный корпус не повторялся: изменение Core ограничено новой read-only API,
все прежние Core files совпали с packaged baseline. Последний full1831/63
ScenarioExpansion относится к предыдущей Core, а не к новой DLL. Новая DLL
имеет отдельный focus02 и solution build; счётчики не смешиваются.

Незакрытый I12 остаётся конкретным:

1. Callback/type/stat mapping и signed limits теперь подтверждены следующим
   этапом. Остались composition общих/map defaults, inheritance/reset,
   wire writer/omission и момент публикации в рабочий документ.
2. Реализовать один частный EN/RU draft этих процентов с корректным общим
   Apply/Cancel/Undo и рабочим source storage. Не менять установленный shared
   файл при редактировании карты и не ослаблять provenance guard DKM.
3. Проверить writer/save/reopen/семифайловую компиляцию и фактический официальный
   GUI/game результат в M19. Исторические варианты и0xE0 требуют явного
   исходного сопоставления, а не предположения из имени.

Это сохранённый I12, не новый верхний scope и не завершённая функция.
Все16 I scopes/23 M groups остаются в [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md).
