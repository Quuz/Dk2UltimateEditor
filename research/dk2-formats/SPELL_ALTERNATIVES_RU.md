# CreatureSpells: альтернативные ссылки, панель и полный intermediate

Дополнение этапа 82 — `TARGETED_SPELL_RU.md`: проверен третий полный путь498870. Четыре парыE8/E9 дали одинаковые371 байт Shot; доказательство ограничено описанными входами. Установлены обычная траектория дляID30 в этом методе,предсказаниеXY и половина высоты цели. Глобальная неиспользуемость альтернатив не утверждается.

Дополнение этап80 — `NATIVE_SHOT_SUCCESS_RU.md`:успешные4982B0/4984D0 теперь выполнены снастоящимиpool/factory/base/registration/направлением. ДлячетырёхпарE8/E9созданный371-байтныйShotполностьюсовпадает вкаждомwrapper приописанныхprovider/data условиях. ПолныйAI/renderer/Shotprocess tickнеисполнялся;глобальноеигровоеназначениеE8/E9остаётсяоткрытым.

Дополнение этап79 — `CREATURE_SHOT_REQUEST_RU.md`:два настоящих Creature-shot wrappers передают primaryShot rawE7/runtime44 в nativefactory. Четыре парыE8/E9 дают одинаковый pre-allocation запрос при одинаковых перенесённых полях. Gate0 останавливает фабрику доallocation;успешные альтернативные/trajectory ветви ещё не проверены. Глобальное неиспользованиеE8/E9 не доказано.

Дополнение этап78 — `SPELL_PREDICATES_RU.md`:56 native-проверок rawEE eligibility с настоящими инициализаторами281 AI-state/6predicate. Категория22 — категория AI-состояния;девять начальных состояний и однократный backup подтверждены. Уточнены Heal и null-guard. Это не расшифровка применения альтернативных ID E8/E9; они остаются открытыми.

Дополнение этап55 — `CREATURE_SPELL_LOADING_RU.md`: проверка расширена до полного parser56A6D0→init→converter→worldcallback51FE00→loader4F7860 и настоящего getter50DFA0.18 native chains/126lookups, insertion/update, ID0/duplicates/capacity. E8/E9 влияют только на gated checksum в проверенном пути. Промежуточный объект746FCA переиспользуется; AlternativeRoom747103 пересекается с Traps intermediate, два native-init опыта подтвердили перезапись scratch без изменения runtime definitions.12 PE functions,314 patches; игровой replacement всё ещё открыт.

Дополнение этап52 — `VERSION_CROSSCHECKS_RU.md`: в точном старом EXE подтверждены тот же converter360 (кроме CALL на одинаковый strncpy), intermediate346 и definition70. Старый loader4EC440 всегда считает сумму346; DX4F7860 делает это только при gate6FCDCF==0. AlternativeE8/E9 в обоих loaders не назначаются смысловым полям; игровой механизм замены остаётся открытым. Не применять общее имя «1.3» без проверки SHA.

26 октября2026, этап42. Продолжение прямого анализа двух EXE через Ghidra. Изменения только в исследовательских материалах; оригинальные игровые файлы и параллельный редактор не менялись.

## Точные пути двух ID

| Поле | Raw | Intermediate | Presence converter | Model Pro | Presence model | Writer |
|---|---:|---:|---:|---:|---:|---:|
| Alternative Shot ID |E8 BYTE |138 BYTE |28 DWORD |16F BYTE |2C DWORD |E8 BYTE |
| Alternative Room ID |E9 BYTE |139 BYTE |2C DWORD |16E BYTE |28 DWORD |E9 BYTE |

Converter игры56A560 и редактора42A0D0 устанавливает presence для каждого поля. Model callback4A2260, участок4A23E4..4A2405, **перекрёстно проверяет presence**, но не переставляет сами значения. При28!=0 переносит Room139→16E; при2C!=0 — Shot138→16F. Полный binary converter задаёт оба presence, поэтому это не обмен двух ID при обычном чтении полного файла.

Writer4A9730, полностью проверенный этапом41, возвращает model16F вrawE8, model16E вrawE9. Для частичных промежуточных обновлений нужно учитывать перекрёстные условия, а не повторять их в независимом raw-кодеке.12 новых model-прогонов сравнивают весь512-байтный буфер при четырёх комбинациях presence и трёх парах значений.

**RawEE AlternativeShot rule — отдельное поле**: оно переносится в runtime45 и выбирает predicate из таблицы пригодности цели. Это не Alternative Shot ID изrawE8. Его ранее подтверждённая логика описана в `SPELLS_RUNTIME_RU.md`.

## Исправление размера промежуточной структуры

В этапе41 сравнивался320-байтный префикс. Формулировка о320 байтах как полном размере была неполной. Настоящий размер — **346 (`0x15A`)**; такой размер суммы уже был зафиксирован этапом22.

Оба инициализатора56A490/42A000 обнуляют20 DWORD presence, затем266 байт data block: `80+264+2=346`. Полные converters назначают байты вплоть до13E включительно; последние27 байт intermediate13F..159 оставляют прежними. Штатный initializer делает их нулевыми. **Это другой диапазон памяти, чем rawEF..109**, хотя размеры совпадают. Сырые27 байт файла converter не копирует.

Шесть полных initializer-прогонов,12 converter/init→converter цепочек проверяют все346 байт. Старые61 native case этапа41 сохранены; их сравнения320-байтного префикса остаются действительными в прежних границах. Схема `spell-writer-semantics.json` исправлена: intermediate_bytes346 и phase41_compared_prefix_bytes320. Старый API `native_spell_writer.intermediate` оставлен как явно подписанный prefix-view; новый `native_spell_alternatives.full_intermediate` выдаёт346 байт.

## Полный игровой загрузчик

4F7860 при DWORD6FCDCF==0 суммирует unsigned bytes всех346 байт в definition DWORD1B. При gate!=0 оставляет прежнюю сумму. Intermediate138/139 не присваиваются игровым смысловым полям70-байтного определения в этом loader. Изменение двух ID при gate0 меняет только checksum1B..1E, при gate!=0 — вообще не меняет70 байт в проверенных случаях.

12 native-прогонов выполняют loader целиком с null resource provider и сравнивают все70 байт. Варьируются пары0/0,36/4,68/0,255/255, gate0/1, TPS0/20/FFFFFFFF и неиспользуемый intermediate tail. Ветви загрузки ресурсов не исполнялись. Это подтверждает устройство конкретного loader, а не отсутствие возможного иного читателя во всей игре или других версиях. **Игровое применение альтернативных ID остаётся открытым.**

## Native-панель и отсутствующие ID

Конструктор442C40 выбирает диалог257 (`PUSH101`). Его ресурсы повторно извлечены из PE. Duplicate template328 действительно содержит hidden/disabled alternatives, но не используется как доказательство скрытия контролов в этом конструкторе: у диалога257 контролы видимы.

| Параметр | Combo ID | Panel index | Byte-array pointer/count |
|---|---:|---:|---:|
| Основной Shot |6FE |74 |D4/D8 |
| Alternative Shot |703 |78 |D4/D8 |
| Alternative Room |704 |7C |E8/EC |
| Alternative rule |705 |80 |FC/100 |

Init442E80 вызывает builders443370/443550. Первые два массива содержат0 (`<none>`) и затем byte ID определений в физическом порядке. Builder тела просмотрены статически. Полное DDX-тело443010 подтвердило12 привязок combo/checkbox через capture-fixtures внешних MFC helper; настоящий GUI не запускался.

Полные lookup443C20/443C50 возвращают первый индекс byte-ID, а при отсутствии — signed−1. ID128..255 сравниваются как байты без отдельного отрицательного sentinel. Fill-фрагмент4439A0 сохраняет результат в78/7C, включая−1.

Commit-фрагмент443CF0 читает `shotArray[panel78]` и `roomArray[panel7C]` без проверки границ. В трёх контролируемых случаях индекс−1 действительно прочитал подготовленный byte перед массивом; значение этого байта стало model ID. Это не прогноз реальной памяти или обязательного краша GUI: DDХ-валидация, события выбора и полный путь открытия/сохранения здесь не исполнялись.

Наш API `checked_selection` отклоняет индекс вне массива; исходный raw ID нужно сохранять, даже если его нет в текущем списке. Автоматическая замена отсутствующего ID на0 или байт до массива была бы потерей данных.

## Восемь флагов: native-подписи

Цепочка resource257→DDX443010→panel→model→encoder/decoder подтверждает подписи независимо от OpenKeeper:

| Бит | Подпись EXE | Control | Panel | Model |
|---:|---|---:|---:|---:|
|1 |Attacking |95 |8C |184 |
|2 |Defensive |98 |90 |188 |
|4 |Creature |9A |94 |18C |
|8 |Cast Only On Creatures |9B |A8 |190 |
|20 |Imp Teleport |96 |98 |194 |
|40 |Imp Haste |97 |9C |198 |
|80 |Cast On Self |99 |A0 |19C |
|100 |Cast On Others |9C |A4 |1A0 |

Числа — hex. Подпись checkbox описывает native-редактор; полный игровой эффект каждого флага этим этапом не установлен. Маска1EF и сохранение неизвестных битов описаны в `SPELL_WRITER_RU.md`.

## Корпус и проверки

82 файла/3535 записей, из них157 имеют ненулевую пару альтернатив. Пары:0/0 —3378;36/4 —151;68/0 —2;0/13 —4. Все ненулевые цели найдены в соответствующих Shots/Rooms: map bundle проверяется по его собственным каталогам, standalone — по соседним standalone-файлам. Разрешённая ссылка сама по себе не доказывает применение в игре.

`phase42-validation.json`:63 native case,4361 API checks,7070 проверок сохранения смысловых runtime bytes при правке альтернатив,1110 точечных пересборок контейнеров в памяти,82 whole-file parse→pack.1849 инструкций/18 live initialized ranges совпали с исходными PE;17 основных входов и2 EXE сохранили SHA-256. В базе исследования создана функция443010; байты программы не менялись. Исследовательский headless-сервер восстановлен после остановки; прежняя копия проекта сохранена.

Inputs/results — `phase42-spell-alternative-emulations.json` и `ghidra/phase42-emulation-spell-alt-*.json`. Схема — `spell-alternative-semantics.json`, компонент53 пакета; API — `native_spell_alternatives.py`. Воспроизведение: `emulate_spell_alternatives.py`, `prepare_spell_alternative_primary.py`, `ghidra_probe.py phase42-primary-queries.json`, `build_spell_alternatives.py`, `build_spell_writer.py`, `build_format_package.py`, `audit_spell_alternatives.py`.
