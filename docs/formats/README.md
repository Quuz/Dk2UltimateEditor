# Форматы Dungeon Keeper 2

Снимок исследования от 13 ноября 2026: **182 интегрированных компонента**, последний этап машинного пакета — **205**. Это база для совместимого редактора; существенные неизвестные остаются открытыми. Наличие свойства в схеме ещё не означает его поддержку в приложении UltimateEditor.

## Где искать информацию

| Материал | Ссылка |
|---|---|
| Актуальные результаты и правила интеграции | [INTEGRATION_RU.md](../../research/dk2-formats/INTEGRATION_RU.md) |
| Таблицы полей по каталогам | [FAMILIES.md](FAMILIES.md) |
| Обзор всех файлов Dethware и графы фонового звука | [DETHWARE_SOUND_REVIEW_RU.md](../../research/dk2-formats/DETHWARE_SOUND_REVIEW_RU.md) |
| SDT: пересборка и ограничения замены звуков | [SDT_EDITING_READINESS_RU.md](../../research/dk2-formats/SDT_EDITING_READINESS_RU.md) |
| Звуковые категории, дорожки и пороги музыки | [SOUND_RESOURCES_RU.md](../../research/dk2-formats/SOUND_RESOURCES_RU.md) |
| Level: ссылки, порядок и ошибки загрузки | [LEVEL_REFERENCE_LOADING_RU.md](../../research/dk2-formats/LEVEL_REFERENCE_LOADING_RU.md) |
| Храм: файл рецептов и выдача gold/Object-награды | [TEMPLE_FILE_REWARDS_RU.md](../../research/dk2-formats/TEMPLE_FILE_REWARDS_RU.md) |
| Скорость: Variables224/65, ранения, поверхность | [POSSESSION_SPEED_CONTEXT_RU.md](../../research/dk2-formats/POSSESSION_SPEED_CONTEXT_RU.md) |
| Variables65/224: границы индексов и соседние таблицы | [GLOBAL_TABLE_INDICES_RU.md](../../research/dk2-formats/GLOBAL_TABLE_INDICES_RU.md) |
| Variables: сохранение Pro и вычисление margin74 | [EDITOR_VARIABLES_WRITER_RU.md](../../research/dk2-formats/EDITOR_VARIABLES_WRITER_RU.md) |
| Variables: чтение и порядок параметров | [VARIABLES_READER_ORDER_RU.md](../../research/dk2-formats/VARIABLES_READER_ORDER_RU.md) |
| Физическая структура полей | [FIELD_LAYOUTS.md](../../research/dk2-formats/FIELD_LAYOUTS.md) |
| Машинный индекс и порядок компонентов | [format-package.index.json](../../research/dk2-formats/format-package.index.json) |
| Перечисления и исторические имена | [ENUMS.md](../../research/dk2-formats/ENUMS.md) |
| Текущий остаток | [READINESS_RU.md](../../research/dk2-formats/READINESS_RU.md) |
| Очередь исследования | [RESEARCH_PRIORITIES_RU.md](../../research/dk2-formats/RESEARCH_PRIORITIES_RU.md) |
| Документы, исходники, отчёты и первичные доказательства | [Архив исследования](../../research/dk2-formats/README.md) |
| Хеши каждого перенесённого файла | [snapshot-manifest.json](../../research/dk2-formats/snapshot-manifest.json) |

## Как использовать результаты

Физическое смещение в записи KWD, смещение промежуточной модели, runtime-offset объекта и адрес инструкции EXE — разные координаты. Размер, signedness, fixed-point, единицы времени и сужение у потребителя указываются отдельно. Сохраняйте полные исходные значения, неизвестные байты, порядок записей, заголовки и хвосты секций.

Актуальные уточнения находятся в `effective_catalogue_layouts`, `effective_shot_parameters`, `effective_enums` и `semantic_corrections`. Базовые layouts/enums сохранены как происхождение данных. Например:

- Door `ManufToBuild` — отдельный u16; соседний `Unknown236` сохраняется самостоятельно.
- Trap `Unknown204` — один WORD; его игровой смысл пока не установлен.
- Историческое Variables74 `GAME_TICKS` подтверждено как `GOLD_CAPACITY_MARGIN` в проверенном потребителе.
- Наличие таблицы резистов в файле не доказывает работающий процентный расчёт защиты.

Различайте исполненные инструкции в подготовленной сцене, статическое чтение функции, подсказку стороннего редактора и гипотезу. Xref не доказывает выполнение в обычной игре; отсутствие найденного чтения не доказывает глобальную неиспользуемость. Условия, профиль EXE/SHA и непроверенные ветви остаются частью каждого результата. В игре и оригинальных редакторах встречаются ошибки: совместимость файла не требует портить неизвестные данные или обещать исправленную игровую механику.

Датированные документы сохраняют старые очереди и исправленные интерпретации. Для текущего состояния используйте верхние актуализации и машинный пакет с его заменами. Оценка готовности не вычисляется из числа документов или тестов. Crypt отложен, десять его назначений остаются неизвестными; глобальные параметры не завершены.

## Проверка без установленной игры

Из корня репозитория, Python 3.12+:

```sh
python tools/validate_format_research.py
python tools/assemble_format_package.py
python -m unittest discover -s tests/FormatResearch -p 'test_*.py'
```

Сборка восстанавливает полный исходный `editor-format-package.json` с точным SHA-256 в игнорируемый `artifacts/formats/`. В Git хранятся отдельные компоненты и индекс, без второй копии большого JSON. CI проверяет целостность и синтетические правила сохранения; он не заменяет игровые CPU-сцены, GUI и приёмку в игре.

Первичные инструкции/декомпиляции и небольшие снимки упакованы в `primary-evidence.zip`. `python tools/unpack_format_evidence.py` восстанавливает игнорируемую папку `research/dk2-formats/ghidra`. Много-GiB сырые эмуляторные JSON остаются в исходном исследовательском workspace; их хеши и результаты сохранены в схемах/манифестах. Исторические абсолютные пути описывают стенд, а не настройки приложения.

Этап196 подтвердил [полный обработчик параметров уровня и runtime-связи Globals](../../research/dk2-formats/LEVEL_GLOBAL_PARAMETERS_RU.md):9 native/3 Ghidra,21 строка переноса существующих полей,68 обратимых правок. Действует последняя запись TPS. Стандартный поздний проход Variables после DF разобран статически; полный автоматический file/World-loader ещё не выполнен. SpeechHornyId хранится как u16, но эта runtime-копия получает low8.

Этап195 подтвердил [Variables19 и множитель длительностей](../../research/dk2-formats/SIMULATION_TICK_RATE_RU.md); его статическая граница Level уточнена этапом196.

Этап194 включён в пакет: [направленная поправка ближнего удара](../../research/dk2-formats/BACKSTAB_DIRECTION_RU.md),54 native/6 Ghidra и проверенные границы. Текущий приоритет — глобальные параметры; Crypt отложен.

Дальнейшие находки добавляйте по [правилам исследования](CONTRIBUTING_RU.md).

Дополнение от 10 ноября: [проверка исправления удара со спины в Flame](../../research/dk2-formats/FLAME_BACKSTAB_RU.md). Исполнен отдельный блок установленной DLL: основная правка работает, угловой шов остаётся; режим `original-compatible` отключает исправление. Состав машинного пакета не менялся.

Отдельно проверены [сложность моделей KMF и ограничения движка](../../research/dk2-formats/MODEL_COMPLEXITY_RU.md): все 2015 моделей архива, [таблица геометрии](../../research/dk2-formats/model-complexity.csv), границы счётчиков SPRS и LOD. Безопасный бюджет новых моделей в живой игре пока не измерен.

Дополнение 13 ноября: [расчёт вершинного света DKII-DX](../../research/dk2-formats/native-vertex-lighting/README_RU.md) — оригинальная lookup-таблица, радиус, интенсивность и нормаль; 80 CPU-исполнений. Отдельная схема и manifest дополнения; замороженный пакет этапа205 не изменён. Финальное изображение игры ещё не проверено.
