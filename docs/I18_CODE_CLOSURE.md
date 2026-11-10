# I18: завершение кодовой сверки identities/owners

25 октября 2026. **I18 закрыт по коду.** [25 классов](I18_IDENTITY_POLICY.md)
имеют явную политику raw load/save, semantic editing и projection/export.
Новые неизвестные значения не получают придуманного смысла; карты не ремонтируются.

Исправлены два реальных ограничения действующих функций:

- ProjectOwnerID прежде считал одинаковый UID во всех категориях конфликтом.
  Теперь считает только активных ScriptOwners того же scope. Существо, дверь,
  обычный/магический предмет, отряд и его участник могут иметь одинаковое число,
  оставаясь разными владельцами. Неактивный хвост не скрывает активного участника.
  Два активных владельца одного scope с тем же UID по-прежнему неоднозначны.
- CopySelection прежде строил уникальный dictionary всех событий карты.
  Теперь индекс группирует события, а уникальность проверяется при посещении
  выбранного reachable ID. Неиспользуемые zero/duplicate/dangling события не
  мешают unscripted copy или чистой выбранной ветке и не попадают в DKZ/Paste.
  Реально выбранный повторный либо отсутствующий target даёт явный отказ.

Primary source Triggers_GetStructureFromUniqueID имеет отдельные scope lists
и обходит active band prefix. Source free-ID allocator не проверяет object/magic
list; совпадение между категориями нельзя автоматически объявлять реальной
неоднозначностью. Текущий allocator всё ещё резервирует известные/orphan IDs
для новых записей и не меняет существующие. Native/project conversion, typed
scope correction и source companion сохраняют прежние явные правила.

Проверки:8 новых логических групп и6 related. Текущий Core:10PASS/0FAIL
(selected graph3, scope API1, selection owner5, corrected existing root1).
На предыдущей namespace-only DLL:4 индивидуальных PASS (band parent/member,
inactive tail, same-scope refusal и corrected compilation). Его focus-01
в целом Exit1: fixtures имели magic ID0 вместо0x75 и stale event offset после
вставки TRIGGERLINK. Исправлены fixtures, ожидаемые условия не ослаблены;
повторены только failed cases. Это не объявляется новым current14/full run.
Actual native compilation, DKZ serialization/Parse/Paste, полные buffers,
Undo/Redo/counters и отсутствие source mutation проверены.

Все41 проекта собраны; final test build/publish Exit0 без warnings/errors.
Два product файла и три test файла изменены. Исторические1840PASS/63FAIL
не пересчитаны:63 сообщения относятся к27 неизменённым картам в четырёх
известных классах. Missing Map и truncated Variables — I04 guards; full
semantic export malformed graph по-прежнему имеет осмысленный отказ.
Raw source/resolver/corpus inputs, Source/Binaries/fixtures и игра не менялись.

Evidence: `artifacts/Evidence/OwnerScopeNamespaces-20261025/`: baseline,
scoped diff, build/test/publish terminal results, i18-closure-proof.json,
delivery.json. Editor/renderer/WPF/CU/game не запускались. Конечная таблица
закрывает известные кодовые ветки, а не фактический ввод/отображение/игровой
результат. После I18 остаются23 ручные M-группы; полная цель не завершена.
