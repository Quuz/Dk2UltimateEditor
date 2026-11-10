# I10: закрытие кодовой сверки параметров сценария

25 октября 2026. **I10 закрыт по коду.** [Матрица](I10_CALLBACK_MATRIX.md)
сводит все103 source types и15 stateful контекстов с текущими providers.
Не найдено отдельной неописанной ветки, требующей нового product-кода.

69 типов имеют страницы параметров;26 условий — только общую страницу Repeat;
8 действий не имеют параметров. Source содержит37 condition Init/Check/Store
и33 action Init/Check, но32 Store. Дополнительный SHOWCRYSTAL имеет PageID=-1;
его старые Init/Check не открывают отдельную страницу и не означают пропуск.
65 active ordinary/compound types перечислены в ScenarioControlPages;4 alias
types идут через отдельный alias editor. Все69 active Store cases учтены.

Оставшиеся зависимости рассмотрены конечными семьями: type/context/defaults,
prerequisites, literal/alias operands, terrain-dependent owner, duration/Repeat,
band/area hints и имена, paired category/ID, release modes, spawn flags,
area/point/creation rectangle, camera library/ceiling, speech entry/text/audio,
DestroyGate identity/coordinate pair, Back/Next/Finish и guards/translation.
19 полных source functions,37/33/32 cases, строки и SHA сохранены в evidence;
current provider fingerprints проверены. I13 уже закрывает315 list-population
sites; известные103 schemas/127 descriptions и100/138 numeric/context projections
не поставлены на повторную проверку без изменения.

Это сопоставление исходных веток с текущим кодом и существующими независимыми
fixtures/датированными результатами. Новых runtime tests, сборки и publish
на этом этапе нет: src/tests и runtime bytes не менялись. Статический script
фиксирует объём и providers; его Exit0 не объявляется исполнением GUI callbacks.
Все product/source/fixture/game inputs и2890 EN/RU literals сохранены.

Современные согласованные поправки сохранены: private aliases/nested pages и
Cancel вместо ранних source global writes, полный Undo/atomic rollback,
raw unknown/inactive payload retention, source bug corrections. Back по-прежнему
пытается Store приватной страницы и возвращается к Type; Next/Finish требует
успех. Existing edit не превращается в rectangle-creation chain.

Ручная M18/M23 приёмка обязательна: фактический ввод, отображение controls,
раскрытия/выборы, Back/Next/Cancel/Apply, звук, системные диалоги, DPI и игра.
Статическая матрица их не закрывает. Неоднозначные/повреждённые identities и
owners остаются в I18; новые типы сценария не выдуманы для неизвестных bytes.

Evidence: `artifacts/Evidence/ScenarioCallbackMatrix-20261025/` содержит
baseline.json, audit-i10.py, i10-closure-proof.json, finish-delivery.py и delivery.json.
Основной проект компилируется из src/ и решения, без цепочек восстановления.
После этого этапа остаётся1 кодовый пункт I18 и23 ручные группы,всего24.
Полная цель активна.
