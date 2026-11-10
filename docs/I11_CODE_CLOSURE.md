# I11: законченная кодовая сверка глобальных страниц

19 октября2026. Кодовый пункт I11 закрыт по конечному сопоставлению
14 исходных страниц и их обработчиков с текущим единым проектом. Это не
прохождение ручной приёмки редактора и не завершение всей цели.

Исходные границы: Properties_Global_Elements.c (5 страниц) и
Properties_Global_Creatures.c (9 страниц), их обычные helpers, общий
Properties_GlobalReset и ранее сопоставленные typed codecs. Список не
расширяется на произвольные новые форматы или отсутствующие игровые данные.
Доказательства: artifacts/Evidence/GlobalCallbackClosure-20261019/callback-audit.json.

## Конечная таблица страниц

| Страница | Сопоставленные обработчики и зависимости | Текущая реализация |
| --- | --- | --- |
| Terrain | Save/GetParams при выборе и уходе; source masks; два RGB picker; signed Damage, numeric callbacks; current/all reset | GlobalTerrainWindow/Controls, GlobalElementInputs |
| Room | Save/GetParams; шесть effects/objects masks и отключённый BattleEffect; torch picker; четыре numeric callbacks; reset | GlobalRoomWindow/Controls, GlobalElementInputs |
| Door | Save/GetParams; material/crate/death effect; пять numeric callbacks с отключённым ManaCost buddy; reset | GlobalDoorWindow/Controls, GlobalElementInputs |
| Trap | Save/GetParams; MagicDoor masks; trigger151/shot155 children и независимые данные; numeric callbacks; hidden ShotDelay в reset | GlobalTrapWindow/Controls, GlobalTrapOptionsWindow |
| Keeper spells | Save/GetParams; primary145/upgrade148 children; четыре numeric callbacks; отдельные слоты; reset | GlobalSpellWindow/Controls, GlobalSpellOptionsWindow |
| Attributes | EN_CHANGE/UDN/LVN,38 значений и defaults, signed/fixed units; row save и reset | CreaturePageWindow/Controls, CreatureAttributeInputs |
| Parameters | CBN101 row save;124/125 и127/128 type/target attraction callbacks;129 amount; None/source cleanup; reset | CreaturePageWindow, CreatureAttractionInputs, CreatureParameterInputs |
| Flags | Row save, checkbox inversion DoNotTrain; inactive Emotionless/ImmuneLightning сохранены; reset | CreaturePageWindow/Controls |
| Attacks | CBN101 row save;104/116/130 shot dependencies; None сохраняет данные;16 numeric callbacks; reset | CreatureAttacks, EditorWorkspace.CreatureAttacks |
| Resistances | CBN101 row save; четыре type/slider/readout пары; WM_HSCROLL и ChangeRes; None; reset | CreatureOptions, CreatureResistanceInputs, EditorWorkspace.CreatureOptions |
| Possession | Row save; ability/swipe/gamma lists,5 numeric callbacks, camera flag; reset | CreatureOptions, CreatureAuxiliaryInputs, EditorWorkspace.CreatureOptions |
| Jobs | Row/leave/Apply warnings; четыре группы; Add/Remove/выбор слота,9 numeric callbacks; retained page buffers; reset | CreatureJobs/WarningPolicy/PageContext, EditorWorkspace.CreatureJobs |
| Light | Row save; четыре numeric callbacks; RGB picker/preview; reset | CreaturePageWindow/Controls, CreatureLightInputs |
| Creature spells | Row save;109 shot child; два numeric callbacks; шесть flags; source WORD limits и независимый spell selection; reset | CreatureSpellWindow/Controls, CreatureSpellOptionsWindow; read-only used-by list123 — [I13](CREATURE_SPELL_USAGE.md) |

У каждой страницы сохранены исходные notification/control IDs и ссылки на
текущие App/Core файлы. Все33 непосредственно вызываемых helper names имеют
описанное соответствие; неприкреплённых вызовов в этом inventory нет.
Это результат чтения исходника и кода, а не33 дополнительных успешных теста.
Числовые helpers, вложенные ShotProc/TypeProc и подробности source limits
закрывались предыдущими focused этапами; их результаты и текущие hashes связаны
с этой сверкой, проверки без изменений не запускались заново.

## Общий контекст, переключение и сбросы

GLOBALCTX пяти страниц и GLOBALCTX2 девяти страниц представлены отдельными
GlobalVariablesSession и CreatureGlobalsSession. Они клонируют документ,
ограничивают разрешённые families/fields, перепроверяют fingerprint и replay
перед одним атомарным Apply/Undo. MapPropertiesSession не расширял свои права.

Пять element pages сохраняют выбор строки независимо для каждой family.
Восьми creature pages доступен общий creatureIndex; CrSpells имеет отдельный
spellIndex. Перед реальной сменой строки/страницы вызывается private SaveCurrent.
Ошибка ввода оставляет прежнюю страницу. Равный выбор не создаёт запись истории.
В Jobs предупреждение проверяется до source row/leave save; общий Apply проверяет
retained page buffer. Открытое дочернее окно/colour/warning блокирует навигацию,
reset и общий Apply; Cancel сохраняет live document.

Reset998 меняет текущую запись,999 — все записи только активной секции.
Сверены все262 source reset assignments и native offsets с текущими metadata.
Скрытый Trap.ShotDelay участвует в reset; unused Room.CeilingHeight,
inactive creature flags и лишний46-й slot не выданы за редактируемые поля.
Reset заменяет невалидный pending page без попытки сначала записать его.
Прежние executed model proofs проверяют semantic/packed results, один Undo,
Cancel и out-of-scope отказ; соответствующие Core файлы сохранены побайтно.

В этой сверке исправлен один дополнительный пробел GlobalVariablesWindow:
после private SaveCurrent повторно проверяется Current до смены строки и до
подготовки общего Apply. Навигация сразу отключается на время transition;
ожидаемые ошибки private Save возвращают отказ. Окно больше не продолжает
переход, если обработчик Changed во время сохранения закрыл его, сменил владельца
или изменил live source. CreatureGlobalsWindow уже имел такую проверку.

Подготовлены4 WPF suites EN/RU×native/DKM: событие private WorkingDocument.Changed
инъецирует owner/source/close между успешным row Save и продолжением Select/Apply.
Проверяются запрет reentry, отсутствие вызова live commit и пересоздания закрытого
row editor. Suites только скомпилированы; выполнение не объявляется прошедшим.

## Осознанные границы соответствия

- Согласованные пользователем Undo/OOM rollback, Ctrl+N/New и Ctrl+Y/Redo сохранены.
- Для ShotProc пользователь выбрал независимый второй spinner и signed минимум−32768.
- Импортированные неизвестные/выходящие за control range данные удерживаются
  при неизменённом Apply. В частности, label255/thumb100 не записывает100 до
  явного действия. Source unconditional trackbar Save мог бы изменить импорт.
- Строгий типизированный Apply и native packed-width preflight сохранены:
  частичный ввод может остаться в поле, но не превращается в частичную запись.
- Вместо selected attribute list/value editor показаны все значения с defaults;
  вместо набора job buttons — полный grid и явные операции. Редактируемые данные,
  зависимости и частный контекст сопоставлены; физическое взаимодействие ещё
  не принято сравнением настоящих окон.

Подписи/bitmap vocabulary вне уже выполненных привязок остаются I13; compiler
I02, неизвестные layouts I04, связи metadata I01/I18, остальные command counts
I15 и activity I16 имеют собственные конечные критерии. Они не считаются
закрытыми этой сверкой и не превращаются в новые задания I11.

## Проверки и оставшаяся ручная приёмка

Audit-callbacks проверяет исходные хеши,14 pages/262 assignments/native offsets,
reset masks, Save/GetParams/Apply contracts, helper bindings и сохранность
неизменённых Core контекстов. Read-only metadata export не добавляет PASS.
Новый Core код в этом этапе не менялся; полный green storage/model regression
без изменений не повторялся. Последний полный1840PASS/63 отказа принадлежит Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`.
Новые numeric focus результаты относятся к собственным DLL, а не прибавляются
к этому историческому полному отчёту.

Все41 проекта собраны без ошибок/предупреждений; текущая App опубликована из
единого проекта. Source/Binaries, fixtures, установленные карты, семифайловый
TestMap и15 WAV сохранены. GUI/renderer/Computer Use/game/audio device не запускались.

M19/M21/M23 сохраняют приёмку настоящих страниц/children, списка/ползунка/каретки,
drag/keyboard/focus/DPI, текстов и системных окон. Инструкции для конкретных
полей находятся в [Global Elements](GLOBAL_ELEMENT_INPUT_CALLBACKS.md),
[Shot/trigger](PROJECTILE_CHILD_INPUT_CALLBACKS.md),
[Resistances](CREATURE_RESISTANCE_SLIDERS.md) и
[общем перечне ручной приёмки](MANUAL_ACCEPTANCE_RU.md).
В текущем управляющем списке остаются14 кодовых пунктов и23 ручные группы.
Все23 исходные возможности FEATURE_PARITY и полная цель сохранены.
