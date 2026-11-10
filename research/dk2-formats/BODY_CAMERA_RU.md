# Камера режима11 для CDeadBody: этап65

Проверен ранее открытый путь `44ACE0`: выбранный узел существует в таблице валидности и имеет native kind6 — **CDeadBody**, размещённое тело существа. Выполнены24 полных вызова активации камеры:23 через эту ветку и1 через fallback. Ещё20 отдельных native-вызовов подтвердили арифметику генератора5558D0. Три камерные проверки предварительно выполняют настоящий21-байтный фрагмент загрузки EyeHeight.

Источник — предоставленный `DKII-DX.exe`, SHA256 `d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a`. Исходные игровые файлы не изменялись. Точные кадры, terrain implementation и покадровая интерполяция камеры не проверены.

## Доказательство типа и связь файлов

Constructor4B8050 назначает vtable66D904. Его native RTTI ведёт через locator67D8B0 к type descriptor69F1A0 с именем `.?AVCDeadBody@@`. Init4B0F20 передаёт6 в4B1DF0→4B28D0→4B4A10; последний сохраняет аргумент в entityDWORD12. Это независимая проверка типа, а не вывод из одинакового численного смещения.

Тот же init получает Creature definition через world virtualC0 и сохраняет указатель в **body+129**, ID — body128, Level — body12D. Камера читает этот указатель. У обычного живого Creature definition находится в370; эти структуры нельзя объединять или считать равномерно сдвинутыми.

Сам формат `Things CA` уже описан в `DEAD_BODIES_RU.md`: payload16 содержит координаты, поле здоровья редактора, CreatureId и Level. **Угла в CA нет**: штатный callback5212D0 создаёт его через RNG(2048), затем AND7FF. Новый камерный consumer использует runtimeWORD F4 без дополнительной маски при индексировании таблиц; значения вне0..2047 здесь не исполнялись и безопасными не объявляются.

## Как используется Eye Height

Связь с каталогом Creatures:

| Слой | Поле |
|---|---|
| Raw Creatures5449/5537 | DWORD+EB0, native GUI `Eye Height (Tiles)` |
| Game intermediate | DWORD+12A0, presenceDWORD F4 |
| Runtime Creature definition | **младшие16 бит** в WORD+65C |
| CDeadBody | Указатель на это определение в+129 |

Converter568E9D..568EAE переносит DWORD. Loader4923C1..4923D5 проверяет presence и сохраняет AX в definition65C. На этом этапе настоящий фрагмент loader выполнен перед полной камерой для DWORD00001000/FFFF1000: получены одинаковые definitionWORD1000 и полное состояние камеры. Третий случай presence0 сохранил priorWORD2000 при входномFFFFFFFF.

Это **фрагментные цепочки**, не новые полные прогоны Creature parser/loader: временные переходы в EmulatorHelper пропускают остальную функцию492320. Raw→mid связь отдельно подтверждается исходными инструкциями и предыдущими схемами. Пять полных вновь исполняемых функций камеры/PRNG не подменяются.

Физическое поле файла остаётся DWORD. Безопасный редактор сохраняет старшие16 бит и меняет весь выбранный raw-span явно; нельзя автоматически заменять DWORD значением runtimeWORD.

## Расчёт положения

Камера использует два массива signedDWORD из исходного PE: baseX6C1488 и baseY6C1C88. Они перекрываются; проверенный общий диапазон10240 байт, а не две независимые копии8192. Значения таблиц не пересчитаны через floating-point sin/cos.

Пусть `h=unsignedWORD(definition65C)`, `a=unsignedWORD(bodyF4)`, а исходные runtime-координаты тела — X/Y/Z. Тогда:

```text
dx = signed32(low32(tableX[a] * h)) >> 16
dy = signed32(low32(tableY[a] * h)) >> 16
targetX = u32(X - dx)
targetY = u32(Y + dy)
```

Terrain virtual204 получает `(targetX,targetY,bodyZ)`. Камерный targetZ затем равен `u32(returnedGroundHeight+256)`. Таким образом, этот consumer использует EyeHeight как множитель смещения **по XY**, а не просто прибавляет его к Z.

При h4096, X11111/Y22222 и углах0/512/1024/1536 получаются XY соответственно `(11111,26318)`, `(7015,22222)`, `(11111,18126)`, `(15207,22222)`. Поставщик ground в проверке возвращает768, поэтому targetZ1024; это не измерение высоты настоящей карты.

Произведение сохраняет только32 бита до арифметического сдвига. Например, h65535 и tableX65536 дают bit patternFFFF0000, затем dx=−1, а не65535. Полный native-вызов подтвердил targetX=X+1 в этом случае. Заменять формулу неограниченным Python-умножением или signed64 без сужения нельзя.

## Настоящий PRNG и зависимость углов от позиции

Ненулевой путь5558D0 выполняется целиком без подмены инструкций:

```text
updated = u32(seed * 0x19660D + 0x3C6EF35F)
*seed = updated
return ROR32(updated,13) % unsignedBound
```

20 отдельных вызовов покрывают seeds0/1/80000000/FFFFFFFF и bounds1/256/512/77758/FFFFFFFF. Проверяются native return и все16 байт seed/backing buffer. Debug global75B2F4 задан0; logging/debug-window и bound0 этим новым набором не проверяются.

В body-ветке последовательность другая, чем простое использование общего seed мира:

1. `a=R(0x77758,&localCopyOfBodyX)`.
2. `b=R(0x2278,&localCopyOfBodyY)`.
3. `localSeed=a XOR b`.
4. `r=R(256,&localSeed)`, `p=R(512,&localSeed)`, `y=R(256,&localSeed)`.
5. `targetYaw=(activeYaw+y−128)&2047`, `targetRoll=(r−128)&2047`, `targetPitch=(p+768)&2047`.

В терминах manager это WORD DC6/DCA/DC8. При одинаковых runtime X/Y и activeYaw арифметика воспроизводит те же углы. Обновляются локальные копии seed; координаты самого тела не перезаписываются генератором.

## Состояние тела и переход камеры

В данной ветке снимается bit0 **bodyBYTE F3**, ещё до завершения перехода камеры. Других изменений всего предоставленного1024-байтного backing buffer тела не произошло. Это размер проверяемого fixture, не размер native-объекта: штатный CDeadBody создаётся с320 байтами.

Общие действия режима11 из этап63 сохраняются: целевой Lens берётся из LensMin, события запрашиваются как `(9,now,1500)` и `(7,1500)`, helper получает3000. Даже E3C bit40 не превращает этот режим в мгновенный: специальная ветка позже назначает3000. Проверяется запись целевых параметров; фактическое перемещение камеры и результат отрисовки не заявляются.

Для invalid node выполняется прежний fallback: activeXY и ground+256; тело не получает снятие F3. Отдельно проверены highWORD аргумента nodeID, отрицательные координаты, h0/4096/65535, крайний допустимый угол2047 и переполнение clock.

## Проверки и машинная схема

`phase65-validation.json`:44 сценария,24 полных44ACE0,20 отдельных PRNG,3 EyeHeight-fragment→camera цепочки. Сравниваются все0xF00 байт manager, весь явный body/definition и фактически переданная terrain-позиция. Clock, размеры мира, ground height, hooks и event queue остаются явно заданными поставщиками; PRNG и таблица в этой ветке теперь исходные.

Пять полных выполняемых функций:44ACE0,44BD30,44BD70,56F850,5558D0. Ещё пять полных bodies для статической идентичности — body ctor/init и три base helpers. Всего10 полных PE bodies сверены отдельно от7 data/fragment ranges. Loader-fragment не посчитан полной функцией.

82 каталога содержат3734 записи Creatures. Для каждой проверена точечная замена DWORD EB0 с побайтным сопоставлением всего контейнера:3734 пересборки **только в памяти**, без изменения остальных полей, opaque tail и соседних секций. Все82 источника совпали с историческими SHA этап43; все четыре исходных EXE неизменны.

`body-camera-semantics.json` вложена в `camera-semantics.json.phase65_body_camera`; `dead-body-semantics.json.phase65_camera_consumer` содержит SHA-ссылку. Пакет остаётся67 компонентов. API `native_body_camera.py`, native harness `emulate_body_camera.py`, primary capture `prepare_body_camera_primary.py`, сборка `build_body_camera_semantics.py`, проверка `audit_body_camera.py`.

## Остальные неизвестные и поисковые результаты

В этом же этапе закончены corrected-DX проходы с исправленным TEST memory-read rule:8605 generic-this summaries,647664 состояний,115 limited functions; Object/Trap413 roots,6126 states,10 прежних кандидатов без нового unknown consumer. Единственное пересечение Traps204 снова относится к Door Height.

Новый ограниченный REP/MOVS поиск старого EXE дал25 копирований от нетипизированных vslotC0 hints:21 камеры по443EB2/152 байта,24/2/1 байт по421EE0/2/4 и36 байт по58E35A. Такие hints не доказывают тип Creature. Runtime tables, dynamic indices, heap destinations, неизвестные return types и frame origins до точки входа не исчерпаны. Gameplay AbilityJump/PickUpPriority из этих копирований не установлен. Диагностический первый REP-проход пропускал MOVSW; authoritative v2 учитывает его.

Данные этих поисков сохранены как bounded leads; они не добавляют вымышленных смыслов полям. KeeperC8, AP04/08 gameplay, CreatureJump/Priority, Terrain198/224, Traps204, Objects34C, Spell alternatives и WanderRadius остаются открытыми. Общая цель продолжается; новый режим11 не означает завершения расшифровки или проверки всего renderer.
