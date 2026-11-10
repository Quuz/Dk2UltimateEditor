# Свойства областей: незакрытая часть исходного интерфейса

Аудит 12 октября 2026, `Things_RectEx.c`, `RectEx_Modify`. Оригинальное окно
редактирует частный ModifyContext. После успешного DialogBoxParam оно создаёт
Undo_CreateEntry для AREA/GATE/EFFECT, принимает coordinates, rectangle/text colors,
flags, четыре effect slots, frequency, name/info и увеличивает MapChanges один раз.
Обычного memcmp/equal guard в этом caller нет: принятие равного черновика также
сохраняет Undo и счётчик. Cancel/error возвращаются до этих действий.

На этапе source audit RegionProperties_Click выбирал запись на общей вкладке
свойств. Геометрический route вызывает SetMapRegionBounds, обычный changed-only
Edit. Это полезные редактируемые поля и геометрия, но не доказательство полного
соответствия исходному property dialog. Модельные geometry fixtures не проверяют
равное принятие именно исходного caller.

## Следующая реализация

Основной private session и окно EN/RU теперь реализованы в
[REGION_PROPERTIES_SESSION](REGION_PROPERTIES_SESSION.md). Ниже исходный план
аудита; закрывать всю область до оставшихся ChoosePoint/gate-generation и physical
проверок нельзя. Этот source audit не заменяет текущую документацию реализации.

- Аудировать все controls/defaults/validation RectEx_Proc и дочерние area/effect
  options, диапазоны координат, colors, name/info, inactive field handling.
- Реализовать независимый region-property draft и семантическое окно EN/RU;
  использовать отдельный original-editor caller для equal acceptance, one Undo
  и одного MapChanges, сохраняя generic SetMapRegionBounds как changed-only API.
- Cancel/ошибка/устаревший owner должны сохранять все bytes/identity/root/историю.
  Вложенные options остаются частными до общего Apply. Нет скрытой нормализации
  неизвестных flags/fields или inferred identity в native gates.
- Подтвердить literal source mapping в native/project model fixtures, stale/raw
  mutation guards и полный regression, затем ручной реальный UI/game цикл.

Исходные hashes и выполненные проверки текста:
`artifacts/Evidence/SelectionOrder-20261012/region-caller-source-audit.json`.
Actual UI не запускался. Полный региональный property workflow остаётся открытым
требованием FEATURE_PARITY; не считать его завершённым из наличия общей таблицы.
