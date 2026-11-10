# GAMEDATE при компиляции игровых файлов

24 октября 2026, CompilerGameDates-20261024. Исправлен следующий участок I02:
созданные компилятором native headers теперь получают исходные даты создания
и изменения вместо нулевых дат. Обычное сохранение native bytes не превращается
в перекомпиляцию и не переписывает существующие даты.

## Правило исходника

`Compiler_SetDate` вызывает `GetLocalTime` и записывает одинаковые локальные
год/месяц/день/час/минуту/секунду в creation и modified части GAMEDATE.
Это12 присваиваемых полей. Структура занимает20 bytes; шесть padding bytes
нулевые вследствие исходного zero allocation. Миллисекунды и UTC offset
в игровой структуре отсутствуют.

| Файлы/секции | Дата | Условие |
| --- | --- | --- |
| GAMEINFO, GAMEPLAYERS, GAMETHINGS, map GAMEVARIABLES | Offset28, GAMEDATE20 | Новый writer header всегда |
| GAMETRIGGERS | Offset32, GAMEDATE20 | Новый writer header всегда |
| Terrain, Rooms, Doors, Traps, KeeperSpells, Creatures, CreatureSpells | Offset28, GAMEDATE20 | Только source `Properties_GlobalChanged` для семьи |
| Map.kld | Нет GAMEDATE | Header и клетки не затрагиваются |
| Неизменённые скопированные definition sections | Исходные20 bytes | Дата и noncanonical padding сохраняются буквально |

В исходнике12 вызовов helper:5 постоянных и7 условных. Дата относится к
созданию игрового файла, а не заменяет creation/compiled statistics рабочего DKM.
Код статистики сеанса и её согласованные правила этим изменением не менялись.

## Как определяется изменение global definitions

Наличие необязательного массива в DKM не означает изменения относительно
исходных defaults. Проверяются полные проектные значения до native BYTE/WORD
casts. Поэтому65536 в поле с native WORD может записаться как0, но всё равно
вызывает исходную регенерацию и новую дату.

Множества262 полей `Properties_GlobalReset` и `Properties_GlobalChanged`
совпадают.259 выражений defaults совпадают буквально после удаления пробелов.
Три RGB-поля читают BYTE0..255: casts BYTE в Reset и LONG в Changed дают
эквивалентные значения. Сопоставление выражений и SHA256 сохранены в
`artifacts/Evidence/CompilerGameDates-20261024/source-date-proof.json`.
Используются существующие source reset rules и definition order; отрицательные
служебные entries порядка не превращаются в новые project rows.

`CompilerGameDates.cs` ставит дату один раз на созданный файл/секцию,
не на каждую запись. Header должен иметь известную версию4 и подходящую
структуру. Неизвестный future header не получает запись по предполагаемым
смещениям; отказ не меняет source project/basis. Полная сверка остальных
unknown layouts остаётся в I04.

## Подтверждено

`--compiler-game-dates-only`: **5 PASS, 0 FAIL**, exit0 на текущей DLL.

- Literal offsets, layout, creation=modified, padding0 и local time bounds
  для пяти headers в target13/16/17; дата-free Map.kld остаётся точным.
- Все семь global families: отсутствие массива/default values копируют
  старую дату; реальное изменение создаёт новую; материализованный массив,
  возвращённый к исходным defaults, снова сохраняет basis date.
- Решение до WORD truncation, отказ future header без изменения source bytes.
- Обычный native SaveCopy сохраняет старые bytes даты; generated SaveCopy/reopen
  сохраняет новую дату и ровно семь preview-файлов.

Время проверяется диапазоном local clock до/после compile с точностью
исходного формата до секунды. Игровое время не подменяется фиксированной
тестовой датой. При успешной компиляции сравниваются source project/basis
bytes, revision, dirty, counters, Undo/Redo, snapshot cost и уведомления.

Связанные проверки выполнены на той же DLL после изменения compiler headers:
NativeRecordRetention — все97 полных AREA/BAND/PLAYER records и mutation cases;
NativePayloadRetention — все1800 cells/711 event payloads, сохранение правок,
Undo/resize и защита от obsolete restoration. Оба runner завершились exit0.
Это целевые потребители изменённого compiler path, не повтор всей неизменённой
регрессии. Все41 проекта собраны без предупреждений/ошибок, App опубликован.

До исправления все пять новых групп завершались FAIL. Первое размещение вызова
date helper было дополнительно исправлено по review: вызов вынесен за record
loop, чтобы соответствовать одному source-вызову на файл. Неудачные результаты
и промежуточные evidence сохранены, checks не ослаблялись.

Полный corpus не запускался. Исторические1840 PASS/63 FAIL не являются новым
результатом этой DLL. GUI, renderer, аудио, DPI и игра не запускались.
Установленные карты, исходники и fixtures не исправлялись. Во всех новых
экспортных проверках имя — `DK2Preview_6a88240eaf814f78`.

## Остаток

В I02 закрыты конкретные ветки DESTROYGATE fallback и GAMEDATE. Сам I02
остаётся открытым: необходимо закончить полное сопоставление stored/derived
полей metadata/player/AI/things/bands/events/globals и validator/version routes.
Source inventory из24 writer functions сохраняется; наличие записи в inventory
не считается доказательством совпадения всех полей. Следующий маршрут —
metadata/player/AI assignments и оставшиеся literals/derived fields.

Общий остаток:8 участков кода и23 ручные группы; исходная цель не завершена.
