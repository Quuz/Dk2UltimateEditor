# Сообщения компилятора о размещении и геройских воротах

25 октября2026, CompilerPlacementDiagnostics-20261025. В full gameplay check
подключены отсутствовавшие сообщения Compiler_TaskTestThings и
Compiler_TaskTestGates. Используются уже существующие ThingPlacementRules,
walking flags и HeroGateSites; второй набор правил размещения не создаётся.

| Code | Уровень | Исходный смысл |
| --- | --- | --- |
| DK2N041 | Warning | Неподходящая местность для существа, двери, ловушки, предмета или special item. |
| DK2N042 | Information | Вражеская территория для creature/object/magic, source result2. |
| DK2N043 | Warning | Вода/лава опасна по проверенным флагам движения существа. |
| DK2N044 | Information | У разрушаемых ворот2×2 нет прохода, окружающей области или событий разрушения. |
| DK2N045 | Warning | У горизонтальных/вертикальных ворот нет прохода. |

Уровни подтверждены не только отсутствием Task->Failed, но и source report
prefix: '?' для Information и '#' для Warning. Число warning increments
в TaskTestThings —5, в TaskTestGates —2. Новые hard errors не добавлены.
Messages имеют EN/RU captions и navigation regions. Для отсутствующей области
2×2 gate расширение навигации равно source: x−1,y−1,width+2,height+2.

Порядок проверки2×2: gate, area, events; выводится первый отсутствующий
элемент. Неразрушаемый perimeter пропускается, display gate не получает
ошибочного требования release/destroy binding. Для liquids walking flags
проверяются раньше territory; unknown source species считается допустимым
на воде и опасным на лаве, как исходные lookup loops. Неподтверждённые
definition flags не подменяются false.

Проверка существующих records не является preview вставки: record не считает
самого себя коллизией или заменяемым объектом. Native HERO/NEUTRAL используют
физических owners1/2, а не enemy byte героя. Raw bytes не нормализуются.
Неизвестные/future layouts, недопустимые позиции и неоднозначные/неверные
region layouts не дают новой authority для этих сообщений. Их прежние
структурные diagnostics/guards сохраняются.

Новый provider вызывается только при includeGameplay=true. Обычная
структурная проверка и незавершённые working maps сохраняют прежний режим.
Пользовательские Check/Compile уже используют full gameplay route; сообщения
попадают в существующий список и правила продолжения с предупреждениями.
Данные карты, история, счётчики, dirty/revision и уведомления при чтении
не изменяются. Сценарий и отсутствующие проходы автоматически не создаются.

## Исправление общего анализатора ворот

Source RectEx_GetGatesList выбирает **первую** подходящую area binding,
как и compiler root writer. HeroGateSites раньше перебирал все такие roots.
Из-за этого поздняя duplicate binding с valid destruction events могла
скрыть отсутствие сценария в первой, реально записываемой привязке.

В общем helper добавлен Take(1). Исправление действует и для сообщений, и
для preview генератора; bindings/graph не переставляются и не ремонтируются.
Другие части helper сохранены буквально. Это не разрешение редактировать
неоднозначные владельцы: соответствующие guards I01/I18 продолжают действовать.

## Доказательства

На прежнем Core новые семь групп дали6 FAIL/1 PASS: gameplay notices
отсутствовали, structural/opaque isolation уже работала. После подключения
получено6 PASS/1 FAIL из-за неверной fixture: owned doors/traps проверялись
как допустимые на нейтральной тропе. Fixture заменена на собственную claimed
path; продуктовые правила ради теста не менялись. Семь групп прошли.

Добавленный случай duplicate area binding затем дал7 PASS/1 FAIL. После
исправления общего helper все **8 PASS**, ExitCode0. Проверены пять families,
severity, walking flags, source unknown-ID fallthrough, priority/regions ворот,
complete generation, два native creature layouts, structural/future isolation,
буквальная неизменность bytes и history. Ошибка definite assignment при
компиляции новой fixture исправлена; неуспешные логи сохранены.

Из-за изменения общего helper повторены две связанные группы: обычная и
вложенная region gate generation. Обе PASS. Все41 проекта build03 и publish01
завершились ExitCode0 без предупреждений/ошибок; App и проверки имеют один
Core hash. Полный корпус и несвязанные успешные группы не повторялись.

Source/report prefixes, first-binding proof, изменения, logs/terminal codes:
`artifacts/Evidence/CompilerPlacementDiagnostics-20261025/`.
2878 EN/RU keys, все прежние2873 literals сохранены. Source/Binaries/Fixtures,
установленные карты и сохранённые samples не менялись. Документация и оба
единственных актуальных архива обновлены.

Все28 writer field tables остаются готовы. I02 ещё открыт для остальных
validator/version routes;8 кодовых и23 ручные группы сохраняются. Исторический
full1840/63 не является полным результатом новой DLL. GUI/WPF/audio/game и
Computer Use не запускались, исходная цель ещё не достигнута.
