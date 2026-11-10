# Изображения владельцев в мастере точки сценария

21 октября 2026. Этап ScenarioPointOwnerImages-20261021, часть I13.

Восьми физическим страницам выбора владельца исходного Action Point wizard
соответствует текущий `ScenarioPointWindow`. Ранее его строки содержали
только текст. Теперь рядом с тем же текстом показано исходное изображение32×32.
`ListBoxItem.Tag` остаётся `ScenarioPointOwnerChoice`, `IsEnabled` —
`choice.CanSelect`; выделение, фильтрация доступности, Finish, отмена,
независимый черновик и запись точки не менялись.

Источник — `Triggers_ActionPoints.c` и `Triggers_DisplayGetImageId` в
`Triggers_Display.c`, а не картинки категорий дерева TrImages. На страницах
владельцев используются другие изображения:

| Владелец | Исходное соответствие |
| --- | --- |
| Игрок | TrPlayers.Variable1–7 →931–937 |
| Существо, участник отряда | ID существа в BadCreatures/BadElites/Heros,45строк |
| Дверь | Doors.Variable,8строк |
| Область | TrSpecial[2] →1501 |
| Отряд | TrSpecial[3] →1212 |
| Предмет, магический предмет | Исходный default TrSpecial[0] →400 |

Последний случай намеренно сохраняет источник: helper не имеет ветвей
TRIGGERIMAGE_OBJECT и TRIGGERIMAGE_MAGIC и возвращает индекс0. Подстановка
картинки конкретного предмета изменила бы поведение. Для неизвестного ID
игрока/существа/двери исходник также возвращает этот общий ресурс400.
Неизвестный scope или уровень32 не получает вымышленной картинки; у уровня
нет отдельной страницы выбора владельца.

`ScenarioPointOwnerImageResources` в Core читает ID из физической записи:
`bGlobalPlayer`, native `bGlobalCreatureID`/`bDoorID`, project `id`, либо
`heros[index].id` для project-участника отряда. Native-участник имеет отдельную
32-байтную запись. Проверяются диапазоны записи/поля. Изображение является
read-only метаданными и не предоставляет права выбора или редактирования.
App использует уже существующий `OriginalScenarioImages` и общий шаблон
изображение+подпись; новых BMP или объявлений ресурсов нет.

Fixture `tests/DK2MapEditor.Validation/ScenarioPointOwnerImagesSource.json`
содержит109 source cases,49 номеров ресурсов, RC aliases, пути и SHA256.
Headless `--scenario-point-owner-images-only` сверяет таблицы, неизвестные
ID/scopes, байты исходных BMP с managed PE App, чтение физических owners и
двух member slots native/DKM, отсутствие изменений файлов/истории/revision.
App, WPF и bitmap-декодер при этом не загружаются. В подготовленный
ScenarioNewPointSmoke добавлена проверка картинки, исходного Tag и CanSelect;
этот WPF-код только компилируется.

Сверка81 image-creation calls теперь распределена по17 исходным файлам в
`artifacts/Evidence/ScenarioPointOwnerImages-20261021/image-caller-matrix.json`.
Для каждого записаны текущий маршрут, связанный этап и оставшаяся граница.
Это не заявление о закрытии81 функции. Конкретный остаток включает:

- Состав отряда как недоступные для выбора подробности на странице SCOPE_BAND,
  а также заголовки отрядов на странице SCOPE_CREATUREINBAND. Нынешний этап
  подключает картинки selectable owners; эти две структуры ещё не восстановлены.
- Окончательная сверка dynamic Elements/creature-preview потребителей
  Things_Selection и Things_Heros, отдельных selector/list потребителей
  Triggers_Select, а также reward control.
- Поэлементная привязка canvas/palette/toolbar потребителей World/Rooms/Gates/
  Walls/DrawingTools/MiscTools, с повторным использованием существующих
  ресурсов, команд и доказательств.

Закрытые definition/scenario parameter/AI-room/rule/NewMap/list image маршруты
повторно не реализуются. Object/custom/dynamic captions остаются в том же I13.

Для ручной проверки: откройте новую точку сценария и страницу выбора игрока,
существа или двери. Рядом с прежними подписями должны быть картинки. Проверьте
недоступную строку, смену EN/RU и сохранение выделения. У предметов ожидается
общая картинка, у области — карта, у отряда — общий геройский значок. Нажмите
Cancel, если не собирались добавлять точку. Реальные окна, scroll/focus/DPI
и визуальная приёмка пока не проверены; Computer Use и игра не запускались.

Итог: финальный build02 всех41 проектов/focus01/publish01 — ExitCode0,
0предупреждений/ошибок. Исходный build01 exit1 из-за отсутствовавшего generated
fixture сохранён, причина исправлена. Поставка и оба ZIP обновлены, каждый entry
проверен по SHA256.
