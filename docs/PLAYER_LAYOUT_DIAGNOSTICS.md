# Диагностика записей игроков native-карты

24 октября2026: [I05 закрыт конечной кодовой сверкой](I05_CODE_CLOSURE.md). Native→DKM теперь отдельно объясняетsource-default slots;текущая поставка PlayerImportPolicy-20261024. Остаток10I/23M, ручнаяприёмка сохранена;датированные состояния ниже исторические.


24 октября 2026. PlayerLayoutDiagnostics-20261024 — конкретная доработка I05.
I05 целиком пока открыт: эта поставка закрывает найденный пробел общей
диагностики, но не объявляет завершённой всю сверку импорта/компиляции/камеры.

Раньше специализированные настройки уже запрещали изменение повторных или
неверных player IDs. Общая проверка NativeMapValidator не объясняла, почему
такие записи неоднозначны, и не сопоставляла физические записи с объявленным
числом хранителей. Теперь у проверки есть отдельный read-only provider.

| Код | Значение | Уровень |
| --- | --- | --- |
| DK2N033 | Stored player ID вне1–7 | Warning |
| DK2N034 | Несколько записей с одним допустимым ID | Warning |
| DK2N035 | Объявленное число хранителей вне1–5 либо число физических player records не равно keepers+2 | Warning |
| DK2N036 | Отсутствуют ожидаемые IDs или есть IDs вне объявленных мест | Warning |
| DK2N037 | Владелец native heart не имеет ровно одной player record | Warning, только gameplay check |
| DK2N038 | Несколько native hearts хранителя; health использует последнюю подходящую запись | Information, только gameplay check |

Все шесть общих описаний имеют EN/RU ресурсы. В диагностических деталях остаются
исходные числовые IDs/counts/offsets и прежний формат технических сообщений.
Сообщения не перенумеровывают записи, не выбирают игрока по порядку файла,
не меняют здоровье/камеру и не создают отсутствующих игроков или сердец.
Они не являются дополнительным автоматическим запретом SaveCopy или новым
разрешением на semantic edit. Прежние guards специализированных настроек
и требования конверсии остаются действующими.

Для DKM семь MAPOPTIONS — позиционные слоты heroes/neutral/keepers, а не
физическое подтверждение семи активных игроков. Поэтому provider не сравнивает
их с metadata.Players+2. Для native используются только уже прочитанные
GAMEPLAYER v4/205-byte records; неизвестный layout/версия остаётся в прежней
opaque диагностике. Отсутствие читаемой схемы не выдаётся за отсутствующего
игрока. Без player collection не придумывается её содержимое.

Привязки к источнику:

- Game_ExtractPlayers проверяет defines size против (Players+2)*205 и переносит
  каждую запись в MapOptions[bGlobalPlayer−1], а не по ordinal.
- Source дубль присваивает слот повторно в physical order. Современный редактор
  сохраняет все original records, а specialized editing требует уникальный ID.
  Native→DKM conversion уже имеет отдельный DuplicatePlayer loss report; новая
  диагностика не принимает это преобразование и не заменяет его consent.
- Game_ExtractThings присваивает health по каждому matching heart по порядку;
  действует last matching. Предыдущие hearts не переписываются ради подписи.
- Source compiler считает heroes/neutral и хранителей с heart; существующее
  правило first-heart tile/centre для камеры и terrain heart warnings не менялись.

Source hashes/anchors сохранены в
`artifacts/Evidence/PlayerLayoutDiagnostics-20261024/source-scope.json`.

## Проверено

Новые шесть headless PASS-групп покрывают все пять keeper counts, обратный
physical order корректных записей, missing row, duplicate/invalid ID,
неверный metadata count и same-size wrong ID set, heart без unique owner,
несколько hearts и last-health41 против раннего5, отдельную gameplay boundary,
семь проектных слотов и future player layout. Проверен отказ смешанного batch:
предшествующий valid player не изменяется, если следующий ID неоднозначен.

Каждое чтение проверяет точные buffers/file set, EditRevision/dirty,
EditorChangeCount/ChangeCount, оба history stacks/cost и отсутствие Changed/
HistoryChanged. Повторная диагностика стабильна; NativeMapValidator подключает
те же строки без пропусков/дубликатов. Для missing-player fixture выполнен
реальный SaveCopy: каждый файл буквально совпадает с исходным buffer,
отсутствующий игрок не восстанавливается. Fixtures сгенерированы отдельно;
установленные карты не менялись.

Build01 всех41 проектов/focus01/publish01 завершились ExitCode0,
warnings/errors0.2870ENRU keys; все2864 прежних keys/literals сохранены.
Source/Binaries/Fixtures,4485game inputs,15WAV истарые corpus inputs/resolvers
сверены. Никакой renderer/WPF/UI/audio/game/Computer Use не исполнялся.
Полный корпус без изменений его карт не повторялся;1840PASS/63FAIL остаётся
историческим, а не зелёным текущим полным прогоном.

Для ручной приёмки откройте копию проблемной карты и выполните «Проверить»;
сверьте EN/RU summary, technical details и отсутствие изменений/перестановки
записей после просмотра. Проверка в игре — только согласованное имя
DK2Preview_6a88240eaf814f78 через DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78.
Эта инструкция пока не означает получения ручного результата.

Общий остаток сохраняется:11I/23M,34 пункта работы; все23 возможности
FEATURE_PARITY иполная цель остаются действующими.
