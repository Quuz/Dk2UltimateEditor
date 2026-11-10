# Текущие отказы корпуса: 63 проверки, 27 карт

15 октября 2026. Это разбор полного неизменённого отчёта CreatureModifiers,
подтверждённого следующим интеграционным запуском. Отказы не исключались,
файлы установленных карт не исправлялись. Один дефект комплекта может давать
несколько отказов: инспекция, native round-trip и два режима текстового экспорта.

| Причина | Карт | Отказавших проверок | Что доказано |
| --- | --- | --- | --- |
| Нет файла клеток | 9 | 27 | Не найден ни явный Map reference, ни местный fallback; клетки не придумываются |
| Усечённый Variables | 6 | 24 | Заголовок обещает ровно на одну 16-байтовую запись больше; исходный pointer/count loop тоже возвращает EOF |
| Нулевые/повторные сценарные ID | 8 | 8 | Native данные читаются и сохраняются; расширенный сценарный MAP экспорт отказывает |
| Ссылки на отсутствующие события | 4 | 4 | Указанных target IDs нет в реально выбранном Triggers; расширенный MAP экспорт отказывает |

Для последних двух категорий отказ сценарной текстовой проекции не доказывает
вылет игры. Исходный no-scenario MAP маршрут и полная native копия имеют
отдельные правила. Загрузка не является разрешением перенумеровать события
или подменить недостающие связи. Отсутствующий файл или последняя запись
не восстанавливаются из соседней карты без отдельного решения пользователя.

## Карты и повторяемые причины

| Карта | Причина | Отказов |
| --- | --- | --- |
| [Battle for the Sun Qz.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Battle for the Sun Qz.kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Battle for the Sun Qz2.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Battle for the Sun Qz2.kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Level19-.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Level19-.kwd>) | Нет файла клеток | 3 |
| [Level20-2.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Level20-2.kwd>) | Нет файла клеток | 3 |
| [Tactics.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Tactics.kwd>) | Нет файла клеток | 3 |
| [XXLevel14.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/XXLevel14.kwd>) | Ссылки на отсутствующие события | 1 |
| [XXLevel19-.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/XXLevel19-.kwd>) | Нет файла клеток | 3 |
| [XXLevel20-2.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/XXLevel20-2.kwd>) | Нет файла клеток | 3 |
| [Shortcuts/test/Hell+.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Shortcuts/test/Hell+.kwd>) | Ссылки на отсутствующие события | 1 |
| [Skirmish/Jabbits/Jibbits1.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Skirmish/Jabbits/Jibbits1.kwd>) | Ссылки на отсутствующие события | 1 |
| [Skirmish/StoneKnight/LEVEL (10).kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Skirmish/StoneKnight/LEVEL (10).kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Skirmish/StoneKnight/LEVEL (6).kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Skirmish/StoneKnight/LEVEL (6).kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Campaings/good maps ideas/skirmish/Jibbits1.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/good maps ideas/skirmish/Jibbits1.kwd>) | Ссылки на отсутствующие события | 1 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel16.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel16.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel19-.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel19-.kwd>) | Нет файла клеток | 3 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel20-2.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel20-2.kwd>) | Нет файла клеток | 3 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by D/XXlevel4.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by D/XXlevel4.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel6b.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by D/XXLevel6b.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level16.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level16.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level19-.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level19-.kwd>) | Нет файла клеток | 3 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level20-2.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level20-2.kwd>) | Нет файла клеток | 3 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/level4.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/level4.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level6b.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by drubjak - копия/Level6b.kwd>) | Не хватает одной записи Variables | 4 |
| [Campaings/Remake Standert Campaing/DK2 Campaign by Electr0H0use/level18.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Remake Standert Campaing/DK2 Campaign by Electr0H0use/level18.kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Special/StoneKnight/Survival G/Survival G.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Special/StoneKnight/Survival G/Survival G.kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Campaings/Skarok/Seats of power/Campaing/Thumbnails/battle for the sun/Battle for the Sun.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Skarok/Seats of power/Campaing/Thumbnails/battle for the sun/Battle for the Sun.kwd>) | Нулевые/повторные сценарные ID | 1 |
| [Campaings/Skarok/Seats of power/Campaing/Thumbnails/battle for the sun/level1.kwd](<D:/Games/DK2/Dungeon Keeper 2/Data/editor/maps/Campaings/Skarok/Seats of power/Campaing/Thumbnails/battle for the sun/level1.kwd>) | Нулевые/повторные сценарные ID | 1 |

JSON содержит все 63 исходные сообщения в исходном порядке внутри каждой карты,
SHA дескрипторов и разобранных файлов, имена отсутствующих references, физические
offsets нулевых событий, отсутствующие target IDs, declared/actual counts Variables.
Чтение перепроверяет хеши после анализа. Source Files_Game.c подтверждает EOF;
хеш текущего NativeDescriptorReferences фиксирует приоритет явного reference.
Полный корпус для этого read-only разбора отдельно не перезапускался.

Ручное исправление ID теперь защищено от неоднозначного переноса ссылок:
[SCRIPT_IDENTITY_SAFETY](SCRIPT_IDENTITY_SAFETY.md). Никакая карта не была
исправлена этим блоком. Детальные доказательства:
artifacts/Evidence/ScriptIdentities-20261015/corpus-failure-analysis.json.
