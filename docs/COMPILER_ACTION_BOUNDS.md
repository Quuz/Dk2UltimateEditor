# Координаты действий при проверке перед компиляцией

25 октября2026, CompilerActionBounds-20261025. Убран лишний отказ при проверке
привязанных ACTION_ADDCREATURE, ACTION_CHANGESQUARE и ACTION_CHANGEOWNER.

Исходный Compiler_TaskTestAction проверяет только `X >= Width || Y >= Height`.
Отрицательные project LONG не запрещаются; Properties_UpdateTriggers может
получить их при сдвиге карты. Writer сужает эти значения к WORD. Отдельная
read-only диагностика DK2N029 уже применяла исходное правило, но план
PlanUnusedScriptRemoval, через который проходит человеческая команда
Check/Compile, дополнительно запрещал X<0/Y<0. Поэтому маршрут мог отказать
до записи исходно допустимого значения, хотя другие проверки его принимали.

Теперь в плане применяется тот же upper-bound test, что в исходнике и
DK2N029. Проверка касается только достижимых от roots действий. X==Width
или Y==Height по-прежнему запрещены; непривязанные действия можно оставить
или удалить через прежний явный выбор. Guards недопустимых ID, layouts,
graph cycles, missing targets и PetBand не изменены.

Project хранит исходные signed32 бита. Компиляция использует прежние
unchecked WORD casts: −1 →65535, Int32.MinValue →0. При повторном открытии
игрового файла это уже unsigned native координаты; такое значение может
превысить карту и получить прежнее предупреждение. Проверка не означает,
что выходящая за карту цель станет рабочим местом в игре. Она воспроизводит
правило исходного компилятора, сохраняет source operands и не ремонтирует их.

Общий raw Resize остаётся строгим. Source editor resize через свойства
карты, наоборот, сохраняет сдвинутые targets — этот отдельный маршрут не
менялся. Код фактического writer, native output и seven-file packing прежний.

## Подтверждение

Шесть новых групп проверяют:

- Три действия, цели13/16/17, отрицательный X или Y, оба Int32.MinValue,
  origin и последний допустимый tile. План и full gameplay validator согласованы.
- Upper equality по каждой оси; bound actions блокируются, unbound остаются
  кандидатами явной очистки.
- Настоящий PrepareEditorMapProperties resize, отрицательные retained targets,
  точный Undo/Redo и повторную проверку после Redo.
- DKM Save/reopen с signed LONG, compile, точные24 action bytes и запись
  ровно семи игровых файлов; native reopen сохраняет эти bytes.

Планирование не меняет исходные bytes, dirty/revision, history, счётчики и
уведомления. До исправления три отрицательных action cases и DKM reopen
получили ложный отказ; upper-bound группа уже проходила. Первоначальный
resize test ошибочно вызывал строгий raw Resize, исправлен на source editor
caller. В fixture также добавлено полное сердце, чтобы unrelated gameplay
error NoHeart не подменял проверку bounds. Первое имя типа плана в тесте
исправлено после compile error. Неуспешные логи сохранены.

Окончательно6 PASS/0 FAIL, ExitCode0. Из-за изменения общего плана повторены
UnusedScriptChecks и две группы MapPropertiesSessionChecks, включая78 literal
resize-script cases,54 player matrices и derived targets. Все PASS. Прочие
успешные группы и весь корпус не повторялись. Решение из41 проекта собрано
без предупреждений/ошибок, опубликованный App использует тот же Core hash.

Source branches и hashes, before/after logs, terminal codes, delivery:
`artifacts/Evidence/CompilerActionBounds-20261025/`.
Source/Binaries/Fixtures, установленные карты, samples и2878 EN/RU literals
сохранены. Поставка и оба актуальных архива обновлены.

Все28 writer tables остаются готовы. Остальная сверка validator/version
routes I02 ещё открыта;8 кодовых и23 ручные группы сохраняются. Исторический
1840/63 не является полным результатом новой DLL. GUI/WPF/audio/game и
Computer Use не запускались, исходная цель не завершена.
