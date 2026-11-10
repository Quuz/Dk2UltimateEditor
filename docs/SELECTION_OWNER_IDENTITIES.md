# Владельцы сценариев при вставке выделения

25 октября 2026. Исправлен последний конкретный маршрут R14 таблицы I03.

В исходных scopes владелец определяется **категорией и UID**: creature0,
door1, object3 или magic4; соответствующие scopes —2/64/128/256. Поля
TRIGGERLINK `creature`, `door`, `object` выбираются по scope. Одинаковое число
у существа и двери само по себе не означает одного владельца.

Проверка selection уже различала категорию и UID, но PasteSelection строил
словарь только по UID. Последняя вещь с этим числом перезаписывала адрес для
остальных категорий. При вставке links также перенумеровывались все три поля,
включая неактивные bytes. Ошибку воспроизвели четыре из пяти новых групп:
подмена владельца в direct/DKZ/Save маршрутах и изменение неактивного поля.

Теперь capture/validation/paste используют общий `OwnedLinkTarget`.
Таблица переноса имеет ключ `(ThingType, UID)`, и меняется только действительное
поле владельца. Новые UID по-прежнему уникальны во всём целевом проекте и не
занимают UID, зарезервированные чужими orphan links. Неоднозначный владелец
внутри одной категории по-прежнему вызывает отказ до изменения документа.
Неактивные поля TRIGGERLINK сохраняются побайтно; graph/event IDs переводятся
своим прежним маршрутом. Глобальные aliases целевой карты не заменяются.

Все пять новых групп успешны:

- Direct paste четырёх категорий с одним исходным numeric UID.
- Настоящая запись/import обоих DKZ: buffer и direct selection.
- Distinct UID и точное сохранение неактивных target fields.
- Отказ при same-category ambiguity без изменения bytes/history.
- Reserved UID/event collision, одна операция Undo/Redo, настоящий DKM
  Save/reopen, отдельный clipboard и сохранение DestinationAlias.

Пять связанных групп selection-files также успешны: DKZ graph retention,
маски, исторические layouts, raw terrain, direct/buffer order и transform
callers. Это нужная проверка изменённого shared helper, а не новый full corpus.
До правки:4FAIL/1PASS; после5PASS и related5PASS. Все41 проекта собраны без
ошибок/предупреждений; опубликован проверенный Core. EN/RU2881 не менялись.

Артефакты: `artifacts/Evidence/SelectionOwnerIdentity-20261025` — исходные
owner constants/списки, source/provider hashes, терминальные журналы и
`i03-closure-proof.json`. Входы, Source/, Binaries/, fixtures и установленные
карты не исправлялись. GUI/WPF/Computer Use/игра не запускались.

Исправление относится к нашему расширению DKZ с attached graph. Официальный
совместимый KMAP/KTGS сохраняет прежние вещи/terrain; его reader не объявлен
читателем DKCP graph. Реальное открытие в официальном GUI остаётся M04.
