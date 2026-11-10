# I03: конечная таблица маршрутов переноса сценариев

25 октября 2026. Ниже14 маршрутов текущих публичных Load/Save/Convert и
selection callers. Это направления и политики, а не14 новых задач или ошибок.
Таблица заменяет широкую повторную очередь «ещё раз проверить все форматы».
I03 [закрыт по коду](I03_CODE_CLOSURE.md): последний R14 исправлен, все строки
сопоставлены с текущими providers и датированными proofs. Новые5 и связанные5
групп успешны; прежние неизменённые проверки не перезапускались.

| ID | Направление / вход | Владельцы, связи, aliases и координаты | Текущая опора / граница |
| --- | --- | --- | --- |
| R01 | Native Load и raw SaveCopy | Игровые bytes и координатные ссылки сохраняются; native import впервые выбирает первые ворота в точке. Частные рабочие ID/orphan state не являются полями игровых файлов. | I01/R19; I02 seven-file writer. Повторный KWD import не заменяет рабочий DKM+archive. |
| R02 | Native → DKM | Все9 scopes, Present/empty/orphan и UID переводятся в TRIGGERLINK/поля; aliases копируются целиком146×64, включая bytes после NUL. Event IDs переводятся в положительные условия/отрицательные действия; roots/edges переводятся вместе, отсутствующие IDs резервируются. Cycles/dangling/unknown — явный Error; shared predecessor — Information, первый backlink, все исходящие связи сохранены. | NativeProjectConversion; I01/I05; все103 event recipes в NativeProjectConversionChecks. Отсутствующие ворота — явный Loss и сохранение ID, неподдержанная существующая цель — Error. Полный источник в immutable archive. |
| R03 | DKM/AREA Load и DKM Save | KTRG2/3 читаются с проверками counts/strides и точным alias tail. Current секции сериализуются с прежними bytes. Неизвестные секции остаются opaque; unverified editing/compile отклоняются. Save сохраняет source companion и статистику по установленному контракту. | LegacyRecords/SerializeLegacyProject, EditorSaveSession; I01/I16. Unknown layouts относятся к I04. |
| R04 | AREA → full DKM | Существующие секции, события и связи сохраняются. При отсутствии general/player metadata добавляются source defaults с Information. Неизвестные AREA-секции сохраняются в исходнике и явно блокируют неподтверждённую конвертацию. | Отдельная ветка PrepareProjectConversionCore; не подмена AREA сценарием новой карты. |
| R05 | DKM → native compile | Roots/ID/signs и все поддержанные event recipes обрабатываются writer; воротам/областям/владельцам возвращаются проверенные рабочие таблицы. Aliases — отдельная editor metadata. Правила координат/WORD casts/version13/16/17 описаны. | Закрытый I02:28 writer tables/17 validator routes; I01 RestoreCompiled tables. Нет заявления о новых прогонах старых доказательств. |
| R06 | KTRG1 →3 | Четыре условия/десять действий, положительные и отрицательные IDs, OR/AND/next/backlinks, reusable и старые координаты. До32768 действий, в том числе в одном условии; переполнение отвергается целиком. Пустые списки получают0 вместо исходной ссылки на несуществующую будущую запись. | HistoricalScenarioCounts: новые4PASS и related1PASS; Source Editor_ExtractTriggers. |
| R07 | KTRG2 →3 | Расширение TRIGGERLINKV2; события и полный alias tail копируются. Поля новых scopes имеют нулевой default, opaque bytes не угадываются. KTRG3 не обновляется. | UpgradeEventLinks и bounded LegacyRecords; исходный Editor_ExtractTriggers. Неподтверждённые будущие версии не повышаются автоматически. |
| R08 | Текстовый MAP → project | Поддержанная грамматика строит scopes, положительные условия/отрицательные действия и backlinks. Проверяются refs area/gate/band, ranges/counts; неизвестная команда — отказ с номером строки. Aliases не являются частью этой грамматики. | TextMapFile.Parser.Trigger/Action/VerifyReferences; Source Text_ExtractTriggers. Нет подстановки неизвестного сценария. |
| R09 | Native/project → MAP, supported scenarios | Переносятся только представимые roots/условия/цепочки; остальные события/repeat bits сообщаются как Scripts loss. Native gate action следует рабочему физическому владельцу, затем получает projected MAP ID. Отсутствующая цель не переадресуется. | TextGateIdentity:6PASS; explicit losses/mandatory source copy. NativeEditorState сообщает границу R01. |
| R10 | Native/project → MAP, original no-scenario export | TRIGGERS0, root links в тексте не выводятся; omitted counts и режим сообщаются явно. Исходные stored bytes сохраняются в отдельной копии. Нет скрытого fallback из R09 в R10. | OriginalTextExportChecks и Source Text_GenerateFile. Physical native snapshot не сериализует private IDs, см.R01. |
| R11 | Обычный MAP Save/SaveAs | Применяет исходный no-scenario writer с явным согласием на потери. Замена атомарна, stale consent и I/O failures не принимают новый результат. Recovery сохраняется как DKM. | EditorTextMapSaveSession/EditorSaveSession; семь связанных групп Save/отказов/Undo в TextGateIdentity. |
| R12 | Copy/direct selection → DKZ → Read | Original KMAP/KTGS плюс bounded DKCP1–6 с raw records, attached graph и local/external coordinate provenance. Direct и buffer projection различаются по source list order. Нулевые/повторные event IDs, dangling links и неоднозначный owner одной категории отвергаются. | DirectSelectionExport, HistoricalSelectionProjection, SelectionTransforms. Native reserved action ChildAnd не считается edge. Глобальные aliases/definitions/bands/areas не являются содержимым selection; literal references остаются в контексте целевой карты. |
| R13 | DKZ ConvertToFormat | Same-format возвращает сохранённую selection; cross native/project разрешён только без scripts. При attached graph требуется full project compilation, есть явный отказ. Физические things переводятся по проверенным historical recipes. | SelectionFile.ConvertToFormat; не выполняется догадка о graph/alias merge. |
| R14 | Selection → paste: владелец сценария | **Закрыт по коду.** Общий OwnedLinkTarget определяет category/активное field. Mapping использует category+UID; остальные target bytes сохраняются. Same-category ambiguity по-прежнему отказ. | [SelectionOwnerIdentity](SELECTION_OWNER_IDENTITIES.md):5 новых и5 связанных PASS, direct/buffer DKZ,collision/Undo/Redo/Save/reopen. |

R14 воспроизведён до изменения:4FAIL/1PASS. После правки проверены четыре
owner категории/scopes2/64/128/256, inactive fields, same-category refusal,
actual DKZ/Save/reopen и one-step Undo/Redo. Исходные карты не исправлялись.

Эта таблица не закрывает I04 (неизвестные layouts), I08 (состав selection),
I09 (геометрия transform), I10 (UI callbacks/wizard) и I18 (диагностика).
Их исходный объём сохраняется; общий [список](OPEN_FUNCTIONS_RU.md) —6I+23M.
Реальная совместимость официального GUI/исторических файлов остаётся M04.

Датированные прогоны подтверждают свой код и входы, а не новый full corpus.
Их суммы не выданы за результат текущей DLL. Файлы Source/ и product providers
зафиксированы по SHA256 в `artifacts/Evidence/InterformatRouteInventory-20261025`.
Инвентаризация не меняла product/tests/DLL и не запускала GUI/игру.
