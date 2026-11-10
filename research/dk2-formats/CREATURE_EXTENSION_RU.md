# Creatures 5537: происхождение расширения и независимость флагов

Дополнение этап50 — `RC3_COMPATIBILITY_RU.md`: все описания long5537 writer ниже относятся к Pro. RC3 writer4AD720 выдаёт5449 и заполняет raw1545 из encoder429270. RC3 reader419FF0 используетmid1935/presence260, а Promid1999/presence264. Нельзя делать общий сдвиг всех raw-полей или автоматически укорачивать исходные записи. Два слота сохраняются независимо.

26 октября 2026, этап43. Выполнены полные native writer, converters и parsers двух предоставленных EXE. Оригинальные игровые данные не менялись.

## Раскладка и писатель

| Raw | Байт | Проверенный путь |
|---|---:|---|
|1545..1548 |4 | Игровые дополнительные флаги; присутствуют и в короткой записи |
|1549..1598 |80 | Обоими converters не переносятся; writer оставляет содержимое стека |
|1599..159C |4 | Дополнительные флаги модели Editor Pro, отдельный слот |
|159D..15A0 |4 | Остатки сохранённого context ECX и адреса возврата stack probe |

Смещения hex. Короткая запись —5449 байт, длинная —5537. Расширение составляет88 байт; игровой DWORD1545 расположен перед ним.

Полный writer `0x4A7F00` выводит5537 байт. После выравнивания и настоящего stack probe4AD490 raw начинается с frameESP+94. **Raw1545..1598 writer не назначает**: игровой DWORD плюс80 байт остаются прежним содержимым стека. Проверены seeds00/A5/FF/меняющийся шаблон, разные флаги, две последовательные записи и animation-type update. Это не копирование исходного raw-record и не80 свойств модели.

Actual encoder41EB80 читает model DWORD1880/1884/1888/1828, формирует биты1/2/4/8; writer записывает их только в1599. Game1545 не является автоматическим зеркалом этого значения.

Условие bounded имени важно: реальный4AB3B0 копирует всю CString length, count32 только дополняет короткую строку. Синтетическая длина5450 переписывает game DWORD и первый opaque byte;5530 — весь scratch84 и первый editor-flag byte. Допустимость таких имён настоящим GUI не проверялась.

## Последние четыре байта

Writer не назначает raw159D..15A0 как поля. В его проверенном stack layout они перекрывают старые слова, оставленные CALL/PUSH ECX в4AD490:

```text
raw159D = byte1 исходного ECX
raw159E = byte2 исходного ECX
raw159F = byte3 исходного ECX
raw15A0 = byte0 адреса возврата4A7F10, то есть10
```

Probe сохраняет context, переносит стек вниз, а старые source slots остаются около конца output-buffer. Младший байт сохранённого ECX позже перекрывается editor flags; следующие три и первый байт прежнего return address попадают в payload. Полные прогоны с ECX10000000/10234560/20ABCDF0/0054F550 подтвердили зависимость.

Для штатного глобального context0054F550 получается **`F5 54 00 10`**. Все3504 длинные записи корпуса имеют именно такой хвост. Это точное совпадение с механизмом inspected writer, но не доказательство единственного исторического производителя каждого файла. Свой редактор должен сохранять исходные4 байта, а не генерировать адреса процесса.

## Converters и реальные parsers

12 paired initializer→converter прогонов сравнивают полный intermediate:6457 байт игры568820→568E30 и6561 Editor Pro4281B0→4287C0. Изменяются opaque80, последние4 байта или противоположный flag slot. Game использует1545→intermediate1935, editor1599→1999/presence264. В соответствующих случаях полный результат совпадает.

Дополнительно выполнены8 полных parsers: оба EXE, stride5449/5537, по1/2 записи. Реальные initializer и converter исполняются; каждый callback сравнивает весь буфер с isolated native reference.

У игры raw-buffer746EC0 рассчитан на5449 байт, intermediate начинается сразу за ним:

```text
746EC0 +1549 =748409
```

Parser56A250 инициализирует intermediate до чтения. Чтение5537 временно перекрывает первые88 байт intermediate. I/O-fixture подтвердил точное перекрытие после raw-read; затем converter заново назначает prefix, и итоговый буфер совпадает с отдельным преобразованием. Это учитывает aliasing, которое не проверялось бы одним поиском прямых обращений.

У editor53F540+15A1=540AE1. Короткий record читается в предварительно обнулённый5537-byte raw-buffer, поэтому отсутствующий editor slot1599 равен0. Другие размеры этими тестами не сертифицированы: native fallback читает заявленный stride без ограничений нашего API.

## Иммунитет и корпус

Этап36 установил: Process9 использует bit1 definition flags, происходящих из **game1545**, без специального теста CreatureId21. См. `KING_LIGHTNING_RU.md`. Новый writer-факт показывает, почему checkbox Pro нельзя автоматически считать настройкой этого игрового слота: модель идёт в1599,1545 остаётся stack-содержимым.

82 файла содержат3734 записи:230 коротких и3504 длинных. У всех длинных game1545=0; editor1599 ненулевой у35, его bit1 установлен у26. В `Battle for the Sun Qz2Globals.kld`, `Battle for the Sun QzGlobals.kld`, `NeedsTestGlobals.kld` у Короля editor1599=1 при game1545=0. Это данные конкретных каталогов; ссылки карты и runtime-патчи требуют отдельного учёта.

Следствие двух подтверждённых цепочек: если native writer выведет game1545 со stack-bit1, этот бит будет значим при соответствующей загрузке Process9. Изменённая игра для проверки следствия не запускалась. Редактировать выбранный слот явно и сохранять второй независимо.

Opaque80 имеет четыре разных ненулевых буфера, включая `Torch1`. Из такого текста не создавалась вымышленная настройка Torch. Native-происхождение writer bytes установлено; история каждого старого буфера не доказана.

## Воспроизводимость

`phase43-validation.json`: **38 native cases** —18 full writers,12 paired converter chains,8 full parsers. Writer assertions покрывают последние92 payload bytes, count и выбранные DWORD, а не все5537 bytes. Converters/parsers сравнивают целые intermediate.

475 API checks,2484 точечных пересборки в памяти,82 exact parse→pack.4279 инструкций/53 live initialized ranges совпали с исходными PE; SHA-25617 основных файлов и2 EXE сохранились.

Models prebuilt: numeric fields нулевые, flags/ID/strings заданы явно. Original getters, flags encoder, resources, ftol, CString и probe выполняются. Заменены дата/file I/O; parser callback сохраняет callee-saved registers. Точные нулевые float-входы не проверяют общее дробное x87 rounding. Constructor, GUI, gameplay и renderer не запускались.

Первое предположение fixture о seed в last4 отклонено и сохранено как superseded после анализа probe. Первые parser callbacks не сохраняли ESI/EDI; все8 случаев исправлены и повторены, старые snapshots исключены из manifest. Эти промежуточные ошибки не считаются подтверждениями.

Схема `creature-extension-semantics.json` — компонент54 пакета. API `native_creature_extension.py`: read-modify-write независимых slots, явная правка80 байт и диагностическая проекция stack-tail. `writer_suffix` не является lossless serializer.

Доказательства: `phase43-primary-manifest.json`, `phase43-creature-extension-emulations.json`, `ghidra/phase43-emulation-extension-*.json`. Воспроизведение: `emulate_creature_extension.py`, `prepare_creature_extension_primary.py`, `ghidra_probe.py phase43-primary-queries.json`, `build_creature_extension.py`, `build_format_package.py`, `audit_creature_extension.py`.
