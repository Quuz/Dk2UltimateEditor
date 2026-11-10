# Согласование AutoSave с указанными определениями

Adjacent AutoSaveThings/Map are explicitly named comparison inputs: their descriptor paths under Maps do not exist. This does not silently resolve or repair those paths, nor claim the complete map loads. Player/AP/trigger/resource references are separate namespaces.

| Источник проверки | Проверено ненулевых |
|---|---:|
| descriptor_creature_list | 23 |
| adjacent_AutoSaveThings | 109 |
| adjacent_AutoSaveMap_distinct_terrain | 19 |
| GlobalVariables | 61 |
| kop4Globals_process_Data1 | 2 |

Диагностик отсутствующих ID: 0.

| Путь из descriptor | Существует ровно по этому пути |
|---|---|
| Data\Editor\Maps\kop4Globals.kld | да |
| Data\Editor\Maps\AutoSavePlayers.kld | нет |
| Data\Editor\Maps\AutoSaveMap.kld | нет |
| Data\Editor\Maps\AutoSaveTriggers.kld | нет |
| Data\Editor\GlobalVariables.kwd | да |
| Data\Editor\Maps\AutoSaveVariables.kld | нет |
| Data\Editor\Maps\AutoSaveThings.kld | нет |

Process Data1 проверен как ID только для процессов создания существа/объекта/воскрешения. Data2 Jack является live tag ID и не проверяется по справочнику Creatures. У нулевых значений и других контекстных полей нет автоматического переназначения. Полные диагностики и пути: `autosave-link-audit.json`.
