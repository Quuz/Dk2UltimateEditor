# Кодовое закрытие I15: принятие команд и счётчик изменений

24 октября 2026. EditorAcceptanceAudit-20261024. **I15 закрыт по коду.**
Это закрытие оставшейся сверки MapChanges/EditorChangeCount, а не всей
семантики команд, форматов или ручной приёмки редактора.

Конечный новый перечень из активных C-исходников без комментариев содержит:

- 89 мест увеличения в55 функциях;83 места находятся вне двух painting files.
  Все path/line/function точно совпали с прежним frozen inventory.
- 5 уменьшений в аварийном откате генератора.
- 5 присваиваний: успешный Load/New/Save, Undo restore и закрытие окна.

Всего99 прямых записей, распределённых по27 семействам обработки. Это объём
исходного перечня, не99 ошибок, тестов или процент готовности. Старые числа
89/83 описывали только увеличения; пять дополнительных присваиваний не были
пропущенными командами редактирования. Указанные здесь функции не считаются
полностью проверенными по каждому возможному игровому аргументу.

Для каждого места сохранены source function/line/context/hash, текущий provider
с точкой входа, правило принятия и количества, прежняя документация,
существующие assertion sources и граница вывода. Отчёт:
`artifacts/Evidence/EditorAcceptanceAudit-20261024/caller-route-audit.json`.
Это результат сверки исходных обработчиков с текущим кодом; совпадение имени
или SHA само по себе не считается функциональным тестом.

## Правила, которые нельзя объединять в одно «любая правка = +1»

| Действие | Увеличение editor counter / история |
| --- | --- |
| Shared global Apply | 1 при изменении общего draft;0 при равном.14 page callbacks не дают14 увеличений: первый переносит общую структуру. |
| Map properties | Equal Apply1;resize ещё1;сдвиги и crop removals добавляют source counts;один parent Undo. |
| Cut/Paste | 1 за принятие плюс каждый снятый physical object;новые вставленные объекты не добавляют counts. Copy0. |
| Rotate/Reflect | 1 плюс каждый перемещаемый physical object, включая protected-edge objects;один Undo даже при равных клетках. |
| Single object paint | 1 плюс противоположные removals;один Undo и один отдельный ObjectPaint activity use. |
| Under-cursor remove | По1 на удаляемую запись с source per-object history;Cancel/empty0. |
| Manager Kill/Modify/Add | Physical removal по1;accepted modify по1 даже equal;insertion по1 плюс реальная replacement removal. Пять вариантов kind — альтернативы, не сумма. |
| Hero band Save/Delete | По1 за родительскую команду;вложенные member edits приватны. Equal existing Save имеет Undo;пустой новый не принимается. |
| Region Modify/Add | По1 за parent acceptance, плюс вложенные generator contributions;один Undo. |
| Region Delete | 1 за всю принятую команду, включая пустой post-policy результат;не по числу выбранных регионов. |
| Patrol | Accepted Start-with-area/Pause/Link/Append/Insert/Remove/Clear по1;cursor-only Start0. |
| Generator | Каждая новая gate/area/logical binding/condition/action отдельно;один Undo. No-work0,ошибка полностью откатывается. |
| Scenario point Save/Delete/Move | По1 за принятую команду;equal/native empty acceptance имеет явный count-only route;edge/refusal0. |
| Condition/action Save/Finish | По1 на сохранённое событие;rectangle Finish считает каждое расширенное событие, сохраняя один parent Undo. |
| Equal parameter Save | +1 без нового Undo;увеличение переносится через обе прежние history stacks. Changed Save остаётся undoable по решению пользователя. |
| Alias rename | Equal/changed acceptance +1 без Undo;rebasing только нужного alias slot и carried counts. Generic setter сохраняет changed-only/Undo behavior. |
| Branch move/delete | По1 за принятую ветвь, не по числу descendants;все link/backlink изменения внутри parent edit. |
| Ordinary Save/New/Load | Только successful acceptance создаёт чистую baseline/сбрасывает counter;temporary/cancel/failure не объявляются успешными. |
| Undo/Redo | Восстанавливаются before/after editor counts;carried non-Undo alias/equal-Save counts сохраняются. Activity Undo counter отдельно. |

Generic Edit, setters и trusted sparse APIs сохраняют прежнюю no-op политику.
Человеческие команды используют отдельные callers; равная raw-правка не должна
становиться принятой командой только из-за общего метода записи. EditCore
публикует counter/Undo после подготовки buffers/history; вложенные операции
не прибавляют самостоятельный верхний счётчик. EditorChangeCount хранит
исходное32-битное unchecked состояние, современный ChangeCount — другую величину.

Сохранены явные решения пользователя: Undo паузы и изменённых scenario
parameters, полное восстановление прежнего порядка/состояния объектов и
регионов, полный rollback с понятной ошибкой при нехватке Undo памяти,
современные Ctrl+N/New и Ctrl+Y/Redo. Эти решения не расширяются произвольно
на остальные различия и не объявляются физически проверенными.

## Полный указатель исходных функций

Каждая функция указана один раз; строки присваивания/отката отделены оператором.

| Source function | ++ / -- / = | Семейство | Текущие поставщики |
| --- | --- | --- | --- |
| `Files_LoadMap` | 0 / 0 / 1 | Load/New counter reset | EditHistory.cs, MainWindow.xaml.cs |
| `Files_SaveMapQuick` | 0 / 0 / 1 | Ordinary Save counter reset | EditorProjectSaveSession.cs, EditorSaveSession.cs, EditorTextMapSaveSession.cs, MainWindow.Save.cs |
| `Map_FillArea` | 1 / 0 / 0 | Painting | MapCanvas.cs, LivePenStroke.cs |
| `Map_PaintElementSimple` | 1 / 0 / 0 | Inactive helper | MapCanvas.cs, LivePenStroke.cs |
| `Map_PaintElement` | 2 / 0 / 0 | Painting | MapCanvas.cs, LivePenStroke.cs |
| `Select_Rotate` | 1 / 0 / 0 | Selection transform | EditorSelectionCallers.cs, EditorDrawingHistory.cs |
| `Select_Symmetry` | 1 / 0 / 0 | Selection transform | EditorSelectionCallers.cs, EditorDrawingHistory.cs |
| `Select_Copy` | 1 / 0 / 0 | Selection Cut/Paste | EditorSelectionCallers.cs, EditorDrawingHistory.cs |
| `Select_Paste` | 1 / 0 / 0 | Selection Cut/Paste | EditorSelectionCallers.cs, EditorDrawingHistory.cs |
| `Map_New` | 0 / 0 / 1 | Load/New counter reset | EditHistory.cs, MainWindow.xaml.cs |
| `Undo_RestoreNode` | 0 / 0 / 1 | Undo/Redo counter restore | EditHistory.cs, ScenarioParameterSave.cs, EditorScenarioNameCaller.cs |
| `Properties_GlobalAttributesProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalParamsProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalFlagsProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalAttacksProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalResistancesProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalPossessionProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalJobsProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalLightProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalCrSpellsProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalTerrainProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalRoomProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalDoorProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalTrapProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_GlobalSpellsProc` | 1 / 0 / 0 | Shared global parents | CreatureGlobalsSession.cs, GlobalVariablesSession.cs, GlobalDefinitionDefaults.cs |
| `Properties_MapOptions` | 2 / 0 / 0 | Map properties/resize | EditorMapPropertiesCaller.cs, MapPropertiesSession.cs |
| `Properties_UpdateThings` | 1 / 0 / 0 | Map properties/resize | EditorMapPropertiesCaller.cs, MapPropertiesSession.cs |
| `Properties_RemoveRectEx` | 1 / 0 / 0 | Map properties/resize | EditorMapPropertiesCaller.cs, MapPropertiesSession.cs |
| `Properties_UpdateRectEx` | 1 / 0 / 0 | Map properties/resize | EditorMapPropertiesCaller.cs, MapPropertiesSession.cs |
| `Things_Paint` | 2 / 0 / 0 | Single object painting | ObjectPainting.cs |
| `Things_RemoveAtMapPos` | 1 / 0 / 0 | Physical removal helpers | UnderCursorRemoval.cs, EditorObjectManagerCallers.cs, EditorSelectionCallers.cs |
| `Things_RemoveAtMapPosSafe` | 1 / 0 / 0 | Physical removal helpers | UnderCursorRemoval.cs, EditorObjectManagerCallers.cs, EditorSelectionCallers.cs |
| `Things_RemoveAtMapPosEx` | 1 / 0 / 0 | Physical removal helpers | UnderCursorRemoval.cs, EditorObjectManagerCallers.cs, EditorSelectionCallers.cs |
| `Heros_SelectKill` | 1 / 0 / 0 | Hero-band deletion | EditorHeroBandDeletion.cs, EditorWorkspace.HeroBands.cs |
| `Heros_SelectChange` | 1 / 0 / 0 | Hero-band parent Save | EditorHeroBandSession.cs, HeroBandWindow.cs |
| `Things_ManagerKill` | 1 / 0 / 0 | Manager removal | EditorObjectManagerCallers.cs, ObjectManagerWindow.Flow.cs |
| `Things_ManagerModify` | 15 / 0 / 0 | Manager modification | EditorObjectManagerCallers.cs |
| `Things_ManagerAdd` | 10 / 0 / 0 | Manager insertion | EditorObjectInsertionCaller.cs |
| `RectEx_PathsKill` | 1 / 0 / 0 | Patrol commands | EditorPatrolCallers.cs, EditorPatrolManagerSession.cs |
| `RectEx_PathsAddArea` | 1 / 0 / 0 | Patrol commands | EditorPatrolCallers.cs, EditorPatrolManagerSession.cs |
| `RectEx_PathsModifyPause` | 1 / 0 / 0 | Patrol commands | EditorPatrolCallers.cs, EditorPatrolManagerSession.cs |
| `RectEx_DrawPath` | 1 / 0 / 0 | Patrol commands | EditorPatrolCallers.cs, EditorPatrolManagerSession.cs |
| `RectEx_RemoveRects` | 1 / 0 / 0 | Region removal | EditorRegionRemovalPolicy.cs |
| `RectEx_Modify` | 1 / 0 / 0 | Region parent editing | RegionPropertiesSession.cs, RegionCreationSession.cs |
| `RectEx_AddRect` | 1 / 0 / 0 | Region parent editing | RegionPropertiesSession.cs, RegionCreationSession.cs |
| `RectEx_Generate` | 5 / 5 / 0 | Hero-gate generator | HeroGateGeneration.cs |
| `Triggers_PointDlgParamsStore` | 1 / 0 / 0 | Scenario point Save | EditorScenarioPointSession.cs |
| `Triggers_ActionDlgParamsSave` | 1 / 0 / 0 | Scenario parameter Save | ScenarioParameterSave.cs, EditorScenarioAliasParentCaller.cs, ScenarioAliasEventEditor.cs, ScenarioCreationSession.cs |
| `Triggers_SelectKillPoint` | 1 / 0 / 0 | Scenario deletion | EditorScenarioPointOperations.cs, ScriptBranchEditing.cs, EditorScenarioDeletionPolicy.cs |
| `Triggers_SelectMovePtUp` | 1 / 0 / 0 | Scenario point order | EditorScenarioPointOperations.cs |
| `Triggers_SelectMovePtDown` | 1 / 0 / 0 | Scenario point order | EditorScenarioPointOperations.cs |
| `Triggers_SelectMoveRight` | 2 / 0 / 0 | Scenario branch move | ScriptBranchEditing.cs |
| `Triggers_SelectMoveLeft` | 2 / 0 / 0 | Scenario branch move | ScriptBranchEditing.cs |
| `Triggers_SelectMoveUp` | 2 / 0 / 0 | Scenario branch move | ScriptBranchEditing.cs |
| `Triggers_SelectMoveDown` | 2 / 0 / 0 | Scenario branch move | ScriptBranchEditing.cs |
| `Triggers_SelectKillTrigger` | 1 / 0 / 0 | Scenario deletion | EditorScenarioPointOperations.cs, ScriptBranchEditing.cs, EditorScenarioDeletionPolicy.cs |
| `Triggers_SelectKillAction` | 1 / 0 / 0 | Scenario deletion | EditorScenarioPointOperations.cs, ScriptBranchEditing.cs, EditorScenarioDeletionPolicy.cs |
| `Triggers_TriggerDlgParamsSave` | 1 / 0 / 0 | Scenario parameter Save | ScenarioParameterSave.cs, EditorScenarioAliasParentCaller.cs, ScenarioAliasEventEditor.cs, ScenarioCreationSession.cs |
| `Triggers_VariablesSetName` | 1 / 0 / 0 | Scenario alias rename | EditorScenarioNameCaller.cs, ScenarioNamesWindow.cs |
| `Window_ProcessMessages` | 0 / 0 / 1 | Close lifecycle reset | MainWindow.Save.cs |

Не осталось непривязанных прямых записей счётчика в выбранной source границе.
Большинство команд уже имело собственную реализацию и проверки; повторной
реализации или запуска успешных неизменённых групп ради нового счётчика нет.
Существующие assertion sources и датированные ExitCode0 перечислены отдельно
в отчёте и prior-functional-evidence.json. Эти старые DLL не выдаются за новые
прогоны текущей сборки: текущие callers/guards/increments сопоставлены по коду.

Продукт, тесты, все2864EN/RU resources и App-файлы в этом этапе не менялись.
Бинарная поставка — ActivitySaveContinuation-20261023:41 проекта ранее собраны
без warnings/errors, publish exit0. Full corpus не повторялся;1840PASS/63FAIL
остаётся историческим, а не зелёным полным прогоном. Документация и оба ZIP
обновлены. GUI/WPF/renderer/audio/game/Computer Use не запускались.

I01–I10 и I18 сохраняют identity, writer, layout, players, diagnostics, terrain,
selection и scenario-callback границы. Все23 M-группы остаются открытыми.
Общий остаток —11 участков кодовой сверки и23 ручные группы,34 пункта работы;
все23 возможности FEATURE_PARITY сохранены. Полная цель ещё не достигнута.
