# Числовые обработчики пяти страниц Global Elements

GlobalElementInputs-20261019, часть открытого I11. Исправлены все33 явных
EN_CHANGE/UDN controls Properties_Global_Elements.c. Отдельные typed windows
используются и в общем GlobalVariablesWindow; новой параллельной формы нет.

| Страница | Поля и границы целой части | Source controls |
| --- | --- | --- |
| Terrain —9 | Damage−32768…32767; GoldValue/StartingHealth/MaxHealth/ManaGain/MaxManaGain0…65535; WibbleH/V0…255; TerrainLightH0…16000 |124,126/128/130/132/134,136/138,150 |
| Room —4 | Cost0…65535; TorchIntensity0…255; TorchLightH0…8; TorchLightRad0…3 |143,148,150,152 |
| Door —5 | Cost/BuildPoints/Health/HealthGain/ManaCost0…65535 |114/116/118/120/122 |
| Trap —11 | IdleEffectDelay/ChargeTime/RechargeTime/ThreatDuration0…16000; GoldCost/BuildPoints/Health/HealthGain/ManaUsage/ManaCostToFire/Threat0…65535 |120/138/140/148;158/160/134/136/142/144/146 |
| Spells —4 | ManaCost0…999999; ManaDrain/RechargeTime/UpgRechargeTime0…32767 |124;126/128/130 |

Terrain Damage использует signed Get/Set; остальные — unsigned. Спиннеры
имеют ID=control+100. Room, Trap и Spells используют **SetDlgItemInt при
ограничении**, даже для полей с fixed-point storage. Поэтому Room TorchLightH
`9.5` становится `8`, Trap ChargeTime`16001.5` — `16000`, Spells RechargeTime
`32768.5` — `32767`. Только TerrainLightH использует float setter: `16001.5`
становится `16000.0`. Дробь внутри допустимой целой части сохраняется.

## Реализация

GlobalElementInputs задаёт source read/limits/clamp formatting для33 полей.
Реальные TextChanged ограничивают явный ввод; RepeatButton−/+ и Up/Down
без модификаторов выполняют один whole-part step. Terrain/Room получили
отсутствовавшие спиннеры. Все пять окон меняют существующий TextBox при шаге,
не пересоздавая форму или удерживаемую кнопку. GlobalDoorControls.Step
также направлен в source getter/setter; disabled ManaCost buddy остаётся
недоступным и для прямого Step API.

Права ограничены current/editable owner, текущим зарегистрированным контролом,
source enabled mask и отсутствием Apply/colour/options операции. ManaCost
двери, Cost нестроящейся комнаты и inactive поля Magic Door не получают
нового доступа. RGB-текстовые поля и named/boolean controls не выданы за
эти33 исходных numeric controls. Количество страниц, каталоги, colour picker,
reset, вложенные options, units, storage и compiler сохранены.

Загрузка, смена языка и неизменённый Apply не нормализуют импорт. Пустой ввод
остаётся пустым до явного шага; увеличение даёт1. Signed/unsigned overflow
следует source integer getter/setter. Изменённые невалидные строки остаются
ошибкой Apply; строгий decoder не заменён небезопасным разбором текста.
Частный Apply страницы меняет только WorkingDocument, общий Apply — один Undo.

В этом же этапе исправлен ранее установленный остаток CreaturePageWindow:
Attributes/Parameters/Light получили RepeatButton и Up/Down; Step меняет
существующий TextBox вместо Rebuild. Их прежние bounds/units/attraction/colour
callbacks не менялись. Этот code gap закрыт; фактические удержание/фокус/DPI
по-прежнему требуют ручной приёмки, а не объявляются прошедшими по компиляции.

## Проверки

CLI `--global-element-input-only`: буквальная таблица33 controls, отличия
integer/`.0` clamp, whole-part/partial/overflow/signed/unsigned step, disabled
door buddy API. Native/DKM private context изменяет32 активных поля пяти
страниц (ManaCost buddy не редактируется), независимо проверяет wire values,
общий один Undo/Redo, Cancel и imported room light/disabled buddy no-op.

Четыре WPF suites EN/RU × native/DKM подготовлены и **только скомпилированы**:
все пять standalone/shared страниц, настоящие TextBox/RepeatButton и сохранение
identity controls при шаге, masks/disabled buddy/Magic Door, language/stale,
общий Apply/Undo/import. Добавлены identity cases Attributes/Light/Parameters.
Старые UI invalid cases теперь используют `invalid`, поскольку прежние
выходящие за границы числа должны автоматически ограничиваться.

Полный корпус без изменений файловых алгоритмов не повторялся. Последние
1840 PASS/63 прежних отказа относятся к предыдущему Core этапа
CreatureLevelModifierWindow; новый focus к ним не прибавляется. GUI, renderer,
игра, Computer Use и audio device не запускались. Source/Binaries/fixtures,
семифайловый TestMap и15 WAV сохранены; игровые карты не изменялись.

## Конечный остаток I11

Основные EN_CHANGE/UDN четырнадцати страниц сопоставлены по числовым полям;
это **не закрывает весь I11**. Следующие конкретные маршруты:

1. Вложенный trap TypeProc — control101/спиннер102 и ограничение TriggerData.
2. Общий ShotProc — формы1–5, controls101/103/203/205 и их source flags,
   ограничения/шаги, включая signed setter semantics. Существующие codec,
   catalogue и opaque retention доказательства использовать повторно,
   не переписывать их по наличию новых UI callbacks.
3. Resistances WM_HSCROLL/ChangeRes — четыре rate/type пары, None и сохранение
   значений; затем конечная сверка оставшихся selection/reset/child callers
   из inventory14 страниц. Перечень вызовов сохраняется в evidence этого этапа.

Это прежние callbacks/helpers I11; верхняя очередь15I/23M не расширяется.
Нельзя переносить незавершённый source mapping в ручную приёмку.

## Ручные шаги M19/M21/M23

1. В Terrain введите Damage−32769 и TerrainLightH16001.5: сразу получите
   −32768 и16000.0. В Room TorchLightH9.5 →8; в Trap ChargeTime16001.5 →16000;
   в Spells RechargeTime32768.5 →32767; Door Health65536 →65535.
2. Очистите поле, увеличьте: получится1. Проверьте удержание кнопки и Up/Down,
   Tab/каретку, EN/RU, resize/DPI. Кнопка/поле не должны исчезать после шага.
3. Убедитесь, что отключённый ManaCost двери и inactive поля Magic Door
   остаются недоступными. Повторите в standalone/shared формах native/DKM.
4. Измените несколько страниц и отмените общее окно — карта прежняя. Повторите
   и примените — один Undo возвращает всё. Открытие импортированных значений
   за границей и смена языка не должны менять байты.
5. В Attributes/Light/Parameters проверьте удержание и сохранение фокуса
   отдельно: данные source bounds остались прежними, изменился lifecycle шага.

Эти шаги не требуют игры. Последующие игровые прогоны — только
DK2Preview_6a88240eaf814f78 через DKII-DX.exe -q -level.


Итог: validation-build-01/focus-01/build-01/build-02 всех41 проектов/publish-01 — ExitCode0. Финальный build02 без ошибок и предупреждений. Предфинальная поставка и оба пакета обновлены из единого проекта;4 WPF suites не исполнялись.


Продолжение: [ProjectileChildInputs-20261019](PROJECTILE_CHILD_INPUT_CALLBACKS.md) — trigger/shot numeric input/step исправлены в трёх child classes и inline panel; source TypeProc spinner102. Два дефекта исходника исправлены по выбору пользователя. I11 открыт: Resistances и конечная сверка remaining callers.


Продолжение: [CreatureResistanceSliders-20261019](CREATURE_RESISTANCE_SLIDERS.md) — rate TextBox заменены четырьмя Slider/readout, сохранены None и imported raw. I11 открыт: конечная сверка14-page selection/reset/child callers.


Текущее продолжение19 октября: [I11_CODE_CLOSURE](I11_CODE_CLOSURE.md). Конечная14-page callback сверка завершена; I11 закрыт по коду. Исторические слова «осталось»/staging/UI ещё не означают текущий отсутствующий код. Физическая M19/M21/M23 приёмка открыта.
