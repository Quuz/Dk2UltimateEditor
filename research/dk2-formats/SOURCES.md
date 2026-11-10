# Источники и воспроизводимость

## Этап87: независимая RC3-проверка HateJobs и API точечнойправки

`RC3_HATE_JOBS_RU.md`,`rc3-job-overlap-semantics.json`,`phase87-primary-manifest.json`,`phase87-validation.json`;`emulate_rc3_hate_jobs.py`,`native_creature_jobs.py`,`prepare_rc3_jobs_primary.py`,`build_rc3_jobs.py`,`audit_rc3_jobs.py`,`finish_phase87.py`.13новыхRC3nativeкомпозиций/14init+converter/6полныхwriter,10wholePE. Пересечениеmid126C/1270 иwriterrawE7C/E80 подтверждено;4полныхмодельныхфункции совпадают сPro.1722проверки новойAPIреализации,82источника/3734физическиезаписи,246views,6отказов;16прежнихnativeDX/Proслучаевсопоставленыбезповторногоисполнения.6clock4B0B30 adapters только вEmulatorHelper;игровыеданные/EXEнеизменны. Предыдущие166RC3/22phase86случаевнеучтеныкакновые. СтарыйRC3пакет сохранён целиком плюс новый supplement;неглобальноесравнениеверсий.

## Этап86: HateJobs/alternative0 alias и реальные Prince-записи

`CREATURE_HATE_JOBS_RU.md`,`creature-job-overlap-semantics.json`,`phase86-validation.json`,`phase86-primary-manifest.json`;`emulate_creature_job_overlap.py`,`emulate_creature_hate_writer.py`,`emulate_creature_jobs_real_maps.py`,`prepare_creature_jobs_primary.py`,`build_creature_jobs.py`,`audit_creature_jobs.py`,`finish_phase86.py`.22native-композиции:14paired-converter,2неизменённыереальныекарты,6полныхwriter;30converters/9native game loaders/72membership.48wholePE+1World-vtable,4EXEсверены.1722exactpatches/82sources/3734physicalrecords;19recordswithalternative0-inducedextraHateJob. Clock4AB320 заменён только в6writer-эмуляциях;никакойподменыисследуемыхconverter/loader/writer/getter/vtable. FullAI/GUIнеисполнялись. Исправленостароеутверждение«14E1нечитается»:читаетсяи затемзатерается. Priority/Jump gameplay остаётсянеизвестным.

## Этап85: полный Creature definition,координаты и presence

`CREATURE_DEFINITION_RU.md`,`creature-definition-semantics.json`,`phase85-primary-manifest.json`,`phase85-validation.json`;`emulate_creature_definition.py`,`emulate_raw_creature_definition.py`,`memory_readout_ymm.py`,`prepare_creature_definition_primary.py`,`build_creature_definition.py`,`audit_creature_definition.py`,`finish_phase85.py`.171успешная эмуляция/19полных2199-байтных образов,54rawinit/converter/loader-цепочки,1710getters;17wholePE+1World-vtable.5904exact-пересборки координатныхDWORD/82файла/3734записи;4EXEunchanged. НулевойmainGFX,пустыеWorldref-массивы,фиксированныйsound;глобальныйPriority/Jump gameplay неизвестен. Старыйsynthetic self-jump иunsupportedAVX512 исключены. Индексные диагностические зацепки сохранены отдельно;14прочих уникальныхсобытий не объявлены классифицированными.

## Этап 84: KeeperSpells DB в игровом UI

`KEEPER_UI_ORDER_RU.md`,`keeper-ui-semantics.json`,`phase84-validation.json`,`phase84-primary-manifest.json`;`scan_keeper_order_calls.py`,`native_keeper_ui.py`,`emulate_keeper_ui.py`,`prepare_keeper_ui_primary.py`,`build_keeper_ui.py`,`audit_keeper_ui.py`,`finish_phase84.py`.28 native-сценариев,329raw-init/converter/insert,420селекторов,28tab/28click callbacks,20локальныхдействий;33диапазонафункций/5данных,82файла/4EXE сверены,4точечныхin-memory DB-пересборки реальных Qz-контейнеров. Код/World/UIvtable сохранены;originalSessionqueue выполняется. Звук задаётся внешним provider,реальныхGUI/network/framebuffer-вызовов нет. Исходные карты не изменены;C8 остаётся неизвестным.

## Этап 83: KeeperSpells runtime-массив

`KEEPER_ARRAY_RU.md`,`keeper-array-semantics.json`,`phase83-validation.json`,`phase83-primary-manifest.json`;`scan_keeper_array_roots.py`,`native_keeper_array.py`,`emulate_keeper_array.py`,`prepare_keeper_array_primary.py`,`build_keeper_array.py`,`audit_keeper_array.py`,`finish_phase83.py`.41 native-сценарий,33 raw-конвертации/38 callback-вызовов,20 диапазонов кода/1vtable;82 файла/1066 записей и4EXE сверены,246 точечных DB-пересборок/82 перестановки целых записей. Две реальные строки Battle for the Sun Qz2 проверены по Order9. Исходные инструкции/vtable и данные сохранены. Старые18 PE-кандидатов остаются нетипизированными;C8 не объявлено расшифрованным.

## Этап 82: третий CreatureSpell и упреждение цели

`TARGETED_SPELL_RU.md`, `targeted-spell-semantics.json`, `phase82-validation.json`, `phase82-primary-manifest.json`; `native_targeted_spell.py`, `emulate_targeted_spell.py`, `prepare_targeted_spell_primary.py`, `build_targeted_spell.py`, `audit_targeted_spell.py`, `finish_phase82.py`. 27 сценариев/81 запуск,21 успешное состояние Shot371,50 диапазонов функций/17 диапазонов данных,246 точечных пересборок Height,83 исходных файла и4EXE сверены. Все исходные инструкции и таблицы методов сохранены;GFX/sound и коэффициенты явно ограничивают опыт. Метаданные включены в существующие67 компонентов;неизвестныеE8/E9 и другие поля не объявлены расшифрованными.

## Этап 81: полное создание EffectElement

`ELEMENT_INITIALIZATION_RU.md`, `element-initialization-semantics.json`, `phase81-validation.json`, `phase81-primary-manifest.json`; `native_element_initialization.py`, `emulate_element_initialization.py`, `prepare_element_initialization_primary.py`, `build_element_initialization.py`, `audit_element_initialization.py`, `finish_phase81.py`. 26 сценариев / 78 native-запусков, 23 успешных состояния; полные 287 байт собраны из трёх независимых запусков. Исходные инструкции и таблицы не подменялись. 22 диапазона функций (20 содержат исполненные пути), три диапазона таблиц, 2460 точечных пересборок, 82 файла / 48416 записей и четыре EXE сверены. Полные сырые записи сохраняются; глобальное назначение B1/B2 остаётся открытым.

## Этап80: native pool/Shot creation/CreatureSpell success

`NATIVE_SHOT_SUCCESS_RU.md`,`native-shot-success-semantics.json`,`phase80-validation.json`,`phase80-primary-manifest.json`;`memory_readout.py`,`emulate_native_shot_pool.py`,`emulate_native_shot_success.py`,`emulate_native_creature_shot.py`,`prepare_native_shot_success_primary.py`,`build_native_shot_success.py`,`audit_native_shot_success.py`,`finish_phase80.py`.45logicalscenarios/117nativeinvocations,40PE/8data;33allocatedstates,36rawsnapshots(include3unallocateddiagnostics).492exactpatches,83inputs+Shotexample/4EXE unchanged. No originalcode/Shotvtablepatches;nullGFX/fixedsound/cardinalcoefficientsdataremainexplicit. Tables65FB14/65FC94arefilebackedmathconstants;preparedtrigcoefficientsnotclaimedasactualpopulation.

## Этап79: CreatureSpell pre-allocation Shot request

`CREATURE_SHOT_REQUEST_RU.md`,`creature-shot-request-semantics.json`,`phase79-validation.json`,`phase79-primary-manifest.json`,`phase79-creature-shot-request-emulations.json`;`native_creature_shot_request.py`,`emulate_creature_shot_request.py`,`prepare_creature_shot_request_primary.py`,`build_creature_shot_request.py`,`audit_creature_shot_request.py`,`finish_phase79.py`.52 native/8executed/2static wholePE;3vslots. Factorygate0/explicitcoefficientdata/initializedfallbackguard scopes retained.2961exactpatches,4EXEunchanged,67components. No newlysuccessfulallocation,noE8E9meaningassigned.

## Этап78: native CreatureSpell predicates и таблица AI

`SPELL_PREDICATES_RU.md`,`spell-predicate-semantics.json`,`phase78-validation.json`,`phase78-primary-manifest.json`,`phase78-spell-predicate-emulations.json`;`native_spell_predicates.py`,`spell_state_tables.py`,`emulate_spell_predicates.py`,`prepare_spell_predicate_primary.py`,`build_spell_predicates.py`,`audit_spell_predicates.py`,`finish_phase78.py`.56 native-композиций,11 исполнявшихся/5 статических wholePE функций,3data. Native47BBE0 и49A6C0 заполняютA5-initialized buffers;всяAIтаблица7025bytes/281rows проверена. Значенияcallback/category не выдаются за файловые указатели в virtual-only диапазоне PE.2248 exactcontainerpatches,82inputs/4EXEs unchanged. E8/E9 remainunknown,AIcastnotrun.

## Этап77: Sight-of-Evil receiver и Data1

`SIGHT_OF_EVIL_RU.md`,`sight-event-semantics.json`,`phase77-validation.json`,`phase77-primary-manifest.json`,`phase77-sight-event-emulations.json`;`native_sight_event.py`,`emulate_sight_event.py`,`prepare_sight_primary.py`,`build_sight_event.py`,`audit_sight_event.py`,`finish_phase77.py`.30 native композиций/8 исполнявшихся функций;11 wholePE/7data. Первичный table-pair13→448580,process25 registry initializer4AD1B0;нулевая таблица в PE не заменяется вымышленной статической регистрацией. Data1cellhalfextent=min(low16,7),fullDWORDgate отличает0/65536. Graphics providers/final mask остаются оговорёнными.656 exact patches/82Keeperfiles/4EXE unchanged;C8unknown сохраняется.

## Этап76: AP linked lookup и lifetime указателя

`AP_LOOKUP_RU.md`,`ap-lookup-review.json`,`phase76-validation.json`,`phase76-primary-manifest.json`,`phase76-ap-lookup-emulations.json`,`phase76-ap-spill-leads.json`,`phase76-ap-tag-spill-leads.json`;`native_ap_lookup.py`,`emulate_ap_lookup.py`,`prepare_ap_lookup_primary.py`,`prepare_ap_spill_search.py`,`prepare_ap_tag_spill_search.py`,`build_ap_lookup_review.py`,`audit_ap_lookup_review.py`,`finish_phase76.py`.36nativecases/4executedfunctions/5wholePE/3vslotranges. Поиск сохраняет локальные aliases только через явно проверенные ABI;восстановлен47205E/Patrol40. Старые negative passes оставляют stack-lifetime blindspot;новая политика не является универсальным контрактом всех CALL. Расширение2623roots имеет8root/28callee limits. Flags04/08 gameplay open;106AP inputs учитывают snapshot AutoSave,4EXE unchanged.

## Этап75: WanderRadius и отдельный StateUtils limit

`WANDER_STATE_RU.md`,`wander-state-semantics.json`,`phase75-validation.json`,`phase75-primary-manifest.json`,`phase75-wander-state-emulations.json`,`phase75-wander-corpus.json`,`phase75-wander-exposure.json`;`native_wander_state.py`,`emulate_wander_state.py`,`scan_wander_radius_exposure.py`,`survey_wander_radius.py`,`prepare_wander_state_primary.py`,`build_wander_state.py`,`audit_wander_state.py`,`finish_phase75.py`.18 полныхDX композиций/5 исполнявшихся native функций;9 полныхPE/3 data. Пара placement0/255 не меняет проверенные наблюдения;единицы placementbyte не установлены.109Things/4EXE неизменны;67 компонентов.

## Этап74: Terrain enumeration, глобальный кэш и выбор определения

`TERRAIN_CACHE_RU.md`,`terrain-cache-semantics.json`,`phase74-validation.json`,`phase74-primary-manifest.json`,`phase74-terrain-cache-emulations.json`;`native_terrain_cache.py`,`emulate_terrain_cache.py`,`prepare_terrain_argument_search.py`,`prepare_terrain_cache_search.py`,`prepare_terrain_cache_queries.py`,`prepare_terrain_cache_primary.py`,`build_terrain_cache.py`,`audit_terrain_cache.py`,`finish_phase74.py`.44 native композиции/90 дополнительных ordinal query,11 полных PE/6 vslot-диапазонов;561 новая точка bounded поиска,469 контрольных зацепок,1 limited callee. Unknown198/224 не расшифрованы;82 источника и4 EXE неизменны.

## Этап73: Creature effective/raw lookup и pointer-search

`CREATURE_LOOKUP_RU.md`,`creature-lookup-review.json`,`phase73-validation.json`,`phase73-primary-manifest.json`,`phase73-creature-lookup-emulations.json`;`native_creature_lookup.py`,`emulate_creature_lookup.py`,`prepare_creature_lookup_primary.py`,`build_creature_lookup_review.py`,`audit_creature_lookup_review.py`,`finish_phase73.py`.34wholegettercases/3completePE/2vslotranges. NewcurrentCreature739roots/357controlleadsandObject246roots/3controls;1limitedcalleeineach,noPriority/Jumpmeaning. `phase73-runtime-launch-review.json` records reusedlive8032 server and TCPguard;noexistingserverstop/projectlockremoval.82sourcehashes/4EXEsunchanged;nonewrawpatches.

## Этап72: Keeper lookup и argument-forward search

`KEEPER_LOOKUP_RU.md`,`keeper-lookup-review.json`,`phase72-validation.json`,`phase72-primary-manifest.json`,`phase72-keeper-lookup-emulations.json`;`native_keeper_lookup.py`,`emulate_keeper_lookup.py`,`prepare_keeper_lookup_primary.py`,`prepare_keeper_pointer_search.py`,`scan_keeper_pointer_arguments.py`,`scan_old_keeper_pointer_arguments.py`,`build_keeper_lookup_review.py`,`audit_keeper_lookup_review.py`,`finish_phase72.py`.26fullnativegetters/2completePE/2explicitdata-codefragmentranges;DX45old48roots/54controlleads/0C8purpose.82sourcehashes/4EXEsunchanged;nonewrawpatches.

## Этап71: полные Object загрузчики и Light

`OBJECT_LOADING_RU.md`,`object-loading-semantics.json`,`phase71-validation.json`,`phase71-primary-manifest.json`,`phase71-object-loading-emulations.json`,`phase71-canonical-boundary-review.json`;`native_object_loading.py`,`emulate_object_loading.py`,`prepare_object_loading_primary.py`,`build_object_loading_semantics.py`,`audit_object_loading.py`,`finish_phase71.py`.86 successfulnativecases+4expectedDIV0stops;8executed/15codeextents/3data;full894/1022/285comparisons(successonly).23512newlightpatches/84sources11756Objects/4EXEsunchanged;6falseanchorsrejected;gameplay34Copen.

## Этап70: Objects indexed-origin review

`OBJECT_INDEXED_REVIEW_RU.md`,`object-indexed-consumer-review.json`,`phase70-validation.json`,`phase70-primary-manifest.json`,`phase70-indexed-origin-review.json`,`phase70-old-object-field-candidates-v2.json`;`scan_old_object_field_v2.py`,`inspect_old_object_indexed.py`,`prepare_old_object_indexed_queries.py`,`review_old_object_indexed.py`,`prepare_object_indexed_primary.py`,`build_object_indexed_review.py`,`audit_object_indexed_review.py`,`finish_phase70.py`.Static17indexed-root proof,2switches/130recoveredinstructions/4883states.11codeextents+3explicitranges,84sourcehashes/4EXEsunchanged. No new native executions or raw patches;Objects34C purpose open.

## Этап69: kind/owner filter и происхождение candidate pointers

`ENTITY_KIND_FILTER_RU.md`,`entity-kind-filter-semantics.json`,`phase69-validation.json`,`phase69-primary-manifest.json`,`phase69-entity-kind-filter-emulations.json`;`native_entity_kind_filter.py`,`emulate_entity_kind_filter.py`,`prepare_entity_kind_filter.py`,`prepare_entity_kind_filter_primary.py`,`build_entity_kind_filter.py`,`audit_entity_kind_filter.py`,`finish_phase69.py`.32 native filter calls;2 executed/9 complete PE functions,1 jump-table data range. Complete91-byte match includes finalRET operand;initial89-byte prefix corrected with diagnostic retained.6 type-rooted DX routes,1 untyped command route;not exclusive global type proof.82 source hashes/4 EXEs unchanged;no new raw patches.

## Этап68: native Trap readiness и типизация кандидатов

`TRAP_READINESS_RU.md`,`trap-readiness-semantics.json`,`phase68-validation.json`,`phase68-primary-manifest.json`,`phase68-trap-readiness-emulations.json`;`native_trap_readiness.py`,`emulate_trap_readiness.py`,`prepare_trap_gate_queries.py`,`prepare_trap_readiness_primary.py`,`build_trap_readiness.py`,`audit_trap_readiness.py`,`finish_phase68.py`.42 whole native readiness calls with6 executed original functions;10 complete PE bodies and3 RTTI data ranges attested. Two DX reads4A7D58/4A9C99 typed CTrap,other candidates remain separate.82 source hashes/1293 Trap records,4 EXEs unchanged;no new raw patches.

## Этап67: полные Door initializer/converter/loader

`DOOR_LOADING_RU.md`,`door-loading-semantics.json`,`phase67-validation.json`,`phase67-primary-manifest.json`,`phase67-door-loading-emulations.json`;`native_door_loading.py`,`emulate_door_loading.py`,`prepare_door_loading_primary.py`,`build_door_loading_semantics.py`,`audit_door_loading.py`,`finish_phase67.py`.58 native cases/8 complete executed PE functions/2 native data ranges;full616/732/181-byte comparisons.656 new224-WORD container patches/82 sources656 Doors. Raw holes ignored only in this chain;unknown gameplay consumers open. Search outputs`phase67-old-door-word236-leads.json`,`phase67-DX-door-word236-leads.json` retain untyped candidates without assigning a Door meaning.

## Этап66: Door writers и oldAP getter search

`DOOR_WRITER_RU.md`,`door-writer-semantics.json`,`phase66-validation.json`,`phase66-primary-manifest.json`,`phase66-door-writer-emulations.json`;`native_door_writer.py`,`emulate_door_writer.py`,`prepare_door_writer_primary.py`,`build_door_writer_semantics.py`,`audit_door_writer.py`.42nativefullwriters(21perEXE)/50records/10wholefunctions/4data-staticranges;8unassignedrecordbytesandWORD236. OldAP`scan_old_ap_pointer_arguments.py`,`phase66-old-ap-pointer-leads.json`,`phase66-old-ap-search-review.json`:2553roots121718states234leads,8root/28calleelimits,purposeunresolved.82sources656Doors2624rawpatches,4EXEsunchanged,67components.

## Этап65: CDeadBody mode11 и PRNG

`BODY_CAMERA_RU.md`,`body-camera-semantics.json`,`phase65-validation.json`,`phase65-primary-manifest.json`,`phase65-body-camera-emulations.json`;`native_body_camera.py`,`emulate_body_camera.py`,`prepare_body_camera_primary.py`,`build_body_camera_semantics.py`,`audit_body_camera.py`.44native:24camera/20PRNG,3assignmentfragmentchains;10wholePE(5executed/5static),7data/fragments,10240trigbytes. NativeRTTI69F1A0CDeadBody/66D904vtable. Searchfollowups:`phase65-read-replay-manifest.json`,`phase65-old-creature-bulk-copy-leads-v2.json`;bounded/untyped,no semanticpromotion.82catalogues3734records3734patches,4EXEs unchanged,67components.

## Этап64: AP retreat и native memory-read calibration

`AP_RETREAT_RU.md`,`ap-retreat-semantics.json`,`memory-read-policy.json`,`phase64-validation.json`,`phase64-primary-manifest.json`,`phase64-ap-retreat-emulations.json`;`native_ap_retreat.py`,`emulate_ap_retreat.py`,`prepare_ap_retreat_primary.py`,`build_ap_retreat_semantics.py`,`audit_ap_retreat.py`,`native_memory_access.py`,`audit_memory_read_policy.py`.23full4611D0,real517490;2wholePE/7instructionfragments/1nativepath.4correctedboundedsearches linkedbySHA;negativeevidenceonly. Descriptorsearch396numericcandidates/2EXEs,noassignedpurpose.106sources3501AP10503patches,phase47AutoSaveThingssnapshot,4originalEXEsunchanged;67components.

## Этап63: native применение CD и отдельный bounded Creature search

`CAMERA_ACTIVATION_RU.md`, `camera-activation-semantics.json` (embeddedcamera-semantics.phase63_activation), `phase63-validation.json`, `phase63-primary-manifest.json`, `phase63-camera-activation-emulations.json`; `native_camera_activation.py`, `emulate_camera_activation.py`, `prepare_camera_activation_primary.py`, `build_camera_activation_semantics.py`, `audit_camera_activation.py`.68scenarios/69fullactivation/19rawchains/7completePEfunctions/1dispatchrange;providers/limitationsявные. Diagnosticv1mode11 excluded. Отдельные`scan_old_creature_thiscall_consumers.py`,`scan_old_creature_argument_consumers.py`:822roots14495/14506states333/335controlreads,46/77calleecontexts;unknownfieldsнеобъявленырасшифрованными.3identicalsourcefiles117CD=39distinctrecords,2808patches,4EXE unchanged,67components.

## Этап62: старые сущности, typed pointers и границы поиска

`OLD_ENTITY_DEFINITIONS_RU.md`, `entity-definition-semantics.json`, `phase62-validation.json`, `phase62-primary-manifest.json`, `phase62-entity-emulations.json`; `native_entity_definitions.py`, `emulate_old_entity_definitions.py`, `prepare_entity_definition_primary.py`, `build_entity_definition_semantics.py`, `audit_entity_definitions.py`, `scan_old_typed_consumers.py`.31native:21 oldgetters/10 oldoverlay;4distinctwholefunctions execute.19wholePE functions separately attested from16 initializeddata/explicitcodefragments. Constructors/base/factory/AIconfiguration/Objectsstorage staticonly;native RTTI andComputerPlayerOn label. Onlyv3search authoritative:408roots5966states10leads,0limited;v1incorrectoldoffsets/v2ESPseeds retaineddiagnostic.84files13705records13705exactpatches,32APIchecks,4EXEs unchanged;67components;unknown204/34C gameplay open.

## Этап61: полные Traps loading и отвергнутая old106 зацепка

`TRAP_LOADING_RU.md`, `trap-loading-semantics.json`, `phase61-validation.json`, `phase61-primary-manifest.json`, `phase61-trap-loading-emulations.json`; `native_trap_loading.py`, `emulate_trap_loading.py`, `build_trap_loading.py`, `audit_trap_loading.py`, `prepare_trap_loading_primary.py`, `match_old_trap_converter.py`, `scan_old_structure_fields.py`.60cases:6init/12converter/24loader/16chains/2initializedchains;30soundcaptures.8distinctcompletefunctions execute;11wholePE and5data attested also cover staticDXparser and oldCEEffectElement identity. Full747/209-byte comparisons,579raw unchanged, explicit nullvisualprovider and boundedsoundadapter;no realaudio/filesystem/worldinsert. All266 displacementleads remain searchcandidates except explicitly rejected42F0F0 identity.82files/1293Traps/1293exactpatches;27APIchecks;4EXEs unchanged;67components. Metadata missingoldstrncpy was created after PEboundarycheck,not treatedasmissingcode.

## Этап60: Trap writer и граница доверия к оригинальным редакторам

`TRAP_WRITER_RU.md`, `trap-writer-semantics.json`, `phase60-validation.json`, `phase60-primary-manifest.json`, `phase60-trap-writer-emulations.json`; `native_trap_writer.py`, `emulate_trap_writer.py`, `prepare_trap_writer_primary.py`, `build_trap_writer.py`, `audit_trap_writer.py`. Только38 v4 cases авторитетны:26 writer/12 DDX→commit→writer,44 полных output records;26 полных header checks,12 chains исключают46/47.22 полных PE functions/2 data;82files1293records3879patches. Native flags/resource/string/ftol/TriggerData helpers, явные MFC/date/file/virtual providers; DDV не исполнялась. Два empty-array cases корректны; четыре null-slot cases короткие, возможность возникновения через GUI не доказана. Независимое manufacture-game evidence наследовано по SHA, не считается новым запуском. Unknown204 не переименован; общая ошибка редакторов возможна; оригиналы не изменены.

## Этап59: производители указателей и Height overlay

`structure-overlay-semantics.json`, `STRUCTURE_OVERLAY_RU.md`, `phase59-validation.json`, `phase59-primary-manifest.json`, `phase59-overlay-emulations.json`; `scan_typed_definition_consumers.py`, `native_structure_overlay.py`, `emulate_structure_overlay.py`, `prepare_structure_overlay_primary.py`, `build_structure_overlay.py`, `audit_structure_overlay.py`.49 cases:22 helper +27 caller;48 renderer-fixture submissions,18 wholePE/8 data attested. Converters full, height loader only declared fragments, intermediate snapshot only immutability proof for otherfields. Entity kind binding from real init/base/store plus CTrap/CDoor RTTI;DoorOpen from native dump/string.83 sources/1293Traps+656Doors/3898 exactpatches. Unknown fields not renamed. Failed metadata-only queries preserved separately; real4B5C10 created after PE boundary verification. No original-data writes.

## Этап58: полная цепочка Terrain

`terrain-catalogue-validation.json`, `TERRAIN_LOADING_RU.md`, `phase58-validation.json`, `phase58-primary-manifest.json`, `phase58-terrain-emulations.json`; `native_terrain_catalogue.py`, `emulate_terrain_catalogue.py`, `prepare_terrain_catalogue_primary.py`, `build_terrain_catalogue_validation.py`, `audit_terrain_catalogue.py`.32 финальных v2 cases:24 полные parser chains,8 полные partial loaders; все дополнены constructor и9 lookup.11 полных PE bodies повторно сверены после эмуляций.82 исходных Terrain-файла/3306 записей/6612 точечных пересборок. Первый неуспешный диагностический прогон с ошибочным адресом cursor сохранён отдельно, в manifest не входит. Два внешних адаптера: ограниченное чтение и sound handles; null graphics provider. Схема вложена в существующий Terrain component;67 компонентов. `phase58-adjusted-unknown-leads.json` — лишь неподтверждённые поисковые подсказки, не назначения полей.

## Этап57: Creature anger, threshold и SET_CREATURE_MOODS

`creature-anger-semantics.json`, `CREATURE_ANGER_RU.md`, `phase57-validation.json`, `phase57-primary-manifest.json`, `phase57-anger-emulations.json`; `native_creature_anger.py`, `emulate_creature_anger.py`, `build_creature_anger.py`, `prepare_creature_anger_primary.py`, `audit_creature_anger.py`.129 cases(48core/30thresholdfragments/13actor/20giftfragments/6readers/12actionchains);onlynotification/requestfixtures,fullcontainerbytepatchaudits.12actioncasesfinalv2addentire20bytenodecomparison.20completePEranges/7data/RTTI;theseincludestaticallyreviewedfunctions,not20fullnativeexecutionpaths.82catalogues3734records7468patches+110Triggerfiles9actionpatches=7477. BoundednegativeCreaturesearchesretainedwithlimitationsandfalseleadrejections;notdecodedfields. Package67;4originalEXEs unchanged.

## Этап56: native AP TriggerGeneric predicates

`action-point-condition-semantics.json`, `AP_TRIGGER_CONDITIONS_RU.md`, `phase56-validation.json`, `phase56-primary-manifest.json`, `phase56-ap-condition-emulations.json`; `emulate_ap_conditions.py`, `native_ap_conditions.py`, `build_ap_conditions.py`, `prepare_ap_conditions_primary.py`, `audit_ap_conditions.py`.61 native dispatcher/counter/filter/marker/comparison executions,12 complete functionranges/4native tables,110Triggers/26299nodes/1412APconditions/4236exactpatches. Soletilegetterfixture;noAPtick/actionchain/gameplayrun. `build_trigger_layouts.py` nowcorrects40/41BYTE2TerrainId andretainsbaseline_fields;7native notes. Component66. `phase56-ap-getter-leads.json`8candidatecalls remainslead-only;AP04/08unknown.

## Этап55: CreatureSpells pipeline/lifetime supplement

`spell-catalogue-validation.json` вложен в существующий `spell-alternative-semantics.json` как `phase55_pipeline`. `CREATURE_SPELL_LOADING_RU.md`, `phase55-primary-manifest.json`, `phase55-spell-catalogue-emulations.json`, `phase55-validation.json`; builders `build_spell_catalogue_validation.py`/`prepare_spell_catalogue_primary.py`, fixture `emulate_spell_catalogue.py`, audit `audit_spell_catalogue.py`.18 finalv3 executions/54records/126getters,12 complete PE ranges;314 exact raw patches/82files3535records. Resourceprovider null,allocator/world preallocated,only memory stream read replaced. Two explicit scratch reuse tests invoke real Traps initializer,not fullTrapsparser. Prior phase54 counts preserved;package remains65. Original4EXEs unchanged.

## Этап54: полная native Shot creation chain

`shot-creation-semantics.json`, `SHOT_CREATION_RU.md`, `phase54-primary-manifest.json`, `phase54-shot-create-emulations.json`, `phase54-validation.json`; builders `build_shot_creation.py`/`prepare_shot_creation_primary.py`, fixtures `emulate_shot_creation.py`/`chain_keeper_shot_creation.py`, audit `audit_shot_creation.py`.32 finalv3 native cases,11 complete PE ranges,82 catalogues/5551 Shot/16653 exact patches. Включены реальные factory/constructor/initializer, перечислены заменённые helpers. ProcessType callback не запускался. `phase54-log-recovery.json` и supervisor описывают отдельное обслуживание исследовательского сервера; это не новый факт о формате. Component65; оригинальные EXE/data не менялись.

## Этап53: Keeper cast routing

`keeper-cast-semantics.json`, `KEEPER_CAST_RU.md`, `phase53-primary-manifest.json`, `phase53-validation.json`.120 complete wrappers,5 finalwholefunctionranges=PE;factory staticbody confirms firstargument→Shot124. Explicit helper fixtures;actualfactory/Shotprocessingnotrun.8461API/6396exactpatches/82files1066records. Only371-bytev2Shotfixtures accepted. Baseline upgraded_shot_tuple description andbuilder corrected. BoundedC8CFGsearch45seeds838states islead-only,notglobalunusedproof. Component64; originaldata/EXEsuntouched.

## Этап52: точные old/DX профили

`version-crosscheck-semantics.json`, `VERSION_CROSSCHECKS_RU.md`, `phase52-primary-manifest.json`, `phase52-label-manifest.json`, `phase52-validation.json`.90nativecases,26wholefunctionranges/6RTTIranges=PE,18native diagnosticstrings,1158API,7070exactpatches/82files3535records. InitialbadAPwriterfixture timedout and excluded;onlyv2evidence accepted.4EC430RET-onlyprobe excluded;actual4EC440loader. Component63. No original EXE/data edits, no GUI/gameplay. OldSHA provenance beyondfolder13notcertified.

## Этап51: RC3 Hero Gate08

`rc3-action-point-semantics.json`, `RC3_ACTION_POINTS_RU.md`, `phase51-primary-manifest.json`, `phase51-validation.json`.45 полных функций после эмуляций совпали с PE;156 native cases с явно описанными fixtures/boundaries;7002 exact patches,110 files,3501AP/65with08. Ресурс сравнения phase49-editor-dialog-comparison.json подтверждён SHA исходных EXE. Component62. Direct native label does not establish gameplay. Plinth/CE leads excluded from decoded formats.

## Этап50: расширенный RC3 native audit

`rc3-compatibility-semantics.json`, `RC3_COMPATIBILITY_RU.md`, `phase50-primary-manifest.json`.37 complete function ranges/live bytes=PE.10 fullconverter pairs equal exceptCALLrelocs;2 fullhelper pairs identical.166 nativecases:25writers,14codecs,4parsers,6pairedcreatureconverters,18independentcatalogueoracles,9Flagschains,90cross-buildcomparisons.605 API/120exactmemorypatches/82roundtrips. CorrectRC3parser41B5E0;early41B5D0 RET-onlyquery excluded. Game data/EXEs untouched. Component61.

## Этап49: второй блок GlobalVariables, два camera writers, RC3 UI

`camera-catalogue-semantics.json`, `CAMERA_CATALOGUE_RU.md`, `phase49-primary-manifest.json`:25 функций/1602 инструкции, Pro/DX/RC3 live bytes=PE.82 cases (26fullwriters,46chains,6registration fragments,4ordered39-recordbanks),596 API/626 memorypatches.3930-source survey lists27rejections;3extraAutoSave drifts independentlysnapshotted. RC3 GUI resources andscalarbindings staticonly;full GUI/renderer untested. Component60;componentcamera_semantics annotated withlater_scope_update.

## Этап48: первичный Terrain light codec и загрузчик

`terrain-lighting-semantics.json`, `TERRAIN_LIGHTING_RU.md`, `phase48-primary-manifest.json`:17 full ranges editor/game, live initialized PE bytes совпали.99 native cases (21 color setter/getter,38 Terrain flag decode/encode,40 game fragment),9361 API+24 partial checks,216 memory patches,82 file roundtrip. Native game+255/editor+256 подтверждены; renderer не сертифицирован. Пакет59. Исторические AutoSave hashes сохранены вместе с phase47 drift snapshots.

## Этап47: два точных EXE и runtime instance layout

`CREATURE_VERSION_LAYOUT_RU.md`, компонент58 `creature-version-layout-semantics.json`. Primary old565630/565730/565840/488270 и current56F330/56F430/56F540/490240:8 full ranges,6 native labels Party/Wander/Trigger, livePE equality.48 cases (36full converter,12dump fragments),2057 checks/64 exact patches. Input manifest records3AutoSave drift and snapshots;14 other main inputs3EXEs unchanged. No old numeric version certification, complete game compatibility or WanderRadius unit inferred.

## Этап46: Machine Spec Level и camera dependency

`CAMERA_MACHINE_SPEC_RU.md`, компонент57 `camera-machine-spec-semantics.json`. Primary full566520,565CF0,449A30,554A60,562670,5621E0,constructor563C50,open563E30,parser5A63B0.9 repeated disassembly/livePE ranges,3 native data ranges:66F560 pointer,6BDA30 Machine Spec Level string,66F5A0 profile table396. PE imports identify RegSetValueExA/RegCloseKey.41 native cases use open/import fixtures; constructors/parser/open inspected, not executed.270 API checks/110 Things roundtrips;17data2EXE hashes unchanged. No registry operations/gameplay.

## Этап45: AP visibility consumers

`ACTION_POINT_VISIBILITY_RU.md`, компонент56 `action-point-visibility-semantics.json`. Primary whole50B5B0,454B80,517480,510E20;4 repeated disassembly/livePE ranges. Исполнен только world fragment50C05D..50C170, полные tile/helper/toolbox;36 native cases,30 reveal/6 toolbox. Combined result buffer16384 bytes; event/update/room callbacks — fixtures.625 API checks,60 exact raw patches,106 AP-bearing files3429 records. Failed event capture4/16 preserved/excluded. Unknown scans phase45 are bounded candidates, not proof of non-use.

## Этап44: KeeperSpells writer, C8 storage и opaqueE0/E1

`KEEPER_PIPELINE_RU.md`, компонент55 `keeper-pipeline-semantics.json`. Primary: game56B990/56BAB0/4F7CE0, editor42B600/42B720/4A25E0/4A99B0, flags420230, resources41CE60, getters424EA0/424EC0, string4AB3B0/6361A0, ftol4ACDA8; date4AB320 inspected/substituted.15 full disassembly/livePE ranges,1496 instructions.68 native cases compare complete writer output,526-byte converters and122-byte scalar definitions. Constructor/GUI/gameplay/resource success branches not certified.646 API checks,522 exact patches,82files1066records.17inputs2EXE unchanged; C8 gameplay purpose remains unresolved.

## Этап43: Creatures extension и stack provenance

`CREATURE_EXTENSION_RU.md`, компонент54 `creature-extension-semantics.json`. Primary full writer4A7F00, probe4AD490, flags41EB80, model getters/resource helpers, ftol4ACDA8, string4AB3B0, strncpy4ACFD0/6361A0. Initializers4281B0/568820, converters4287C0/568E30, parsers429D90/56A250.53 repeat assembly/live ranges matchPE,4279 instructions. Constructor41D7E0 retained but not executed; date4AB320 inspected, substituted.

38 native cases:18 full writers compare suffix92,12 paired converters compare6457/6561 bytes,8 full parsers compare callback buffers and game raw/mid alias88. Three context variations plus default, four stack seeds, consecutive records and two overlong names. Superseded last4 seed assumption/ESI-EDI callback ABI snapshots retained and excluded;8 parsers rerun.82files3734 records,3504 longF5540010;2484 patches;17inputs2EXE unchanged. No game/GUI run.

## Этап42: CreatureSpells alternatives/UI/intermediate346

`SPELL_ALTERNATIVES_RU.md`, `spell-alternative-semantics.json`, компонент53. Primary: full initializers56A490/42A000, converters56A560/42A0D0, loader4F7860; editor model4A2260/reference fragment, writer4A9730, constructor442C40(dialog257), init442E80, DDX443010, fill4439A0, commit443CF0, lookup443C20/443C50, builders443370/443550, flag decoder41D760/encoder41D6F0. Dialog resources re-extracted from exact PE;18 repeat assembly/live ranges match disk. Newly created research function443010, no program-byte edits.

63 native cases with346-byte complete intermediate,70-byte definition, whole provided panel/model buffers and12 DDX bindings compared. Full MFC/GUI, resource-provider branches and gameplay replacement are not asserted. Missing index−1 uses explicit guard fixture, not real-memory prediction.1110 raw-preserving container patches;82 files3535 records157 reference pairs0 missing paired targets. Prior phase41 native converter inputs/results retained:320-byte prefix comparison, now corrected machine/human full-size description346. Headless server restarted on existing research project; original data17 and EXE2 hashes unchanged.

## Этап41: CreatureSpells writer

`SPELL_WRITER_RU.md`, компонент52 `spell-writer-semantics.json`. Full Editor Pro4A9730 writer;41D6F0 encoder/41D760 decoder;4A2260 model flag fragment;41CE60 resource getter;4AB3B0 CString;4ACDA8 ftol;4AB320 date routine inspected but substituted in emulation. Full converters game56A560/editor42A0D0 with actual strncpy6361A0/4ACFD0. `phase41-primary-manifest.json`:12 saved/repeated assembly ranges and live initialized bytes match PE. strncpy initial snapshots captured after first emulator runs; no earlier snapshot claimed.

`phase41-spell-writer-emulations.json`:61 cases, including19 complete writers with whole output comparison. No original-data or EXE edits, no game/GUI run.393 API cases,360 exact container patches,82 files3535 records; originals17+2 hashes unchanged. Inputs and explicit fixtures retained per native case. Full corpus includes4 distinct tails,2707 nonzero; origin of each old tail unproven. Initial Creature75C/68E/6EB displacement search had no new semantic reader; candidate dumps retained as leads, not primary proof.

## Этап40: Things CD Cameras

`CAMERAS_RU.md`, компонент51 `camera-semantics.json`. Primary game570A20 parser/56F850 initializer/56F900 converter,5215F0 callback/449A30 preset-store. Active controls449AC0/449BD0 position with449B30 bounds,449CA0 zoom delta,449D40 angles and jump tables,449FC0 view delta/449F70 absolute,44A070 lens delta/44A020 absolute;44ACE0 activation mapping/flags80 reviewed statically, not wholly emulated. Editor431220 parser/42FD70 initializer/42FE20 converter,42C730 zero context,48B5F0 registration,4947C0 writer. Addresses4921B0/4922B0 are trigger callbacks and excluded as camera consumers.

84 native cases:16 parser6 converter6 seek15 full callback/store21 scalar delta4 absolute6 position7 angles3 registration fragments. Synthetic provenance:110 corpusThings have0CD;16 raw append/472 exactpatch in memory,1786 API checks.2857 instructions21 unchanged disassembly/21 livePE ranges;17 primarydata2EXE unchanged. Actual matrix rebuild/provider/checksum/filesystem helpers explicit fixtures. Raw53 ignored, ID lowBYTE, relative flags10/20 and exclusive bounds confirmed; no universal screen-unit/gameplay/normal GUI camera save assertion. Native writer omission is full assembly evidence, not a real GUI run.

## Этап39: Things CC Room overlays

`PLACED_ROOMS_RU.md`, компонент50 `placed-room-semantics.json`. Primary game5708A0 parser/521420 callback,4559B0 existing-room resolver,4E5300 health delta helper,4DB700 heart object helper,4EF6C0 direction rebuild. Helpers reviewed but substituted in game emulation; actual full callback arithmetic/branching/lookup executed. Editor430FA0 parser/42FBD0 init/42FC20 converter,492030 callback registered48B790,487270 terrain plane,488850/488830 player lookup,4226B0/422B10/422B60 portal add/find/get,422C20/423120/423170 gate add/find/get,48B380/48B4D0 resolve,4225A0 heart serializer,4947C0 writer. Dispatch tables and catalogue terrain-to-room links verified from PE/data.

136 native cases with preallocated editor arrays and exact byte comparisons,419 API cases15444 in-memory patches110 Things roundtrips858 CC records;3049 instructions26 unchanged disassemblies/26 livePE ranges.586 opaque0F nonzero; no padding-zero assumption. No gameplay/helper rebuild/heart creation simulation. Phase38 provider ABI fixed after inspecting actual488850 RET4/4889D0 RET16: old fixtures used net-balanced12/8; all29 body editor cases rerun, snapshots preserved under phase38-superseded-provider-abi; unchanged field results and regenerated component49. This correction is explicit, not hidden as an original verification.

## Этап38: Things CA DeadBody

`DEAD_BODIES_RU.md`, компонент49 `dead-body-semantics.json`. Primary game:570500 parser,5212D0 callback,4B7FC0 creator,4B8050 constructor,4B0F20 initializer,402AE0 vector-zero,4B8100 reset,4B12D0 level consumer,4363F0 multiply,4B18B0 Level label; editor430CD0 parser,42FA60/42FAA0 init/converter,491DE0 callback,41CE20 definition lookup,488B30/488D30 model count/get,4ACDA8 ftol,4947C0 writer. Native raw0F→runtime12D labelLevel corrects baselinePlayerId; raw0C follows health path, not GoldHeld. Lookup pointer/link helper sources retained separately; full owner lists not simulated.

74 emulations:8 parser,3 converter,18 game creation,29 editor callback,7 arithmetic fragments,9 writer. Of29 editor cases,9 legacy float cases explicitly capture ST0 as double and substitute integer-return fixture: original Ghidra FISTP ignored requested truncation for9.9→10, so this is documented as emulator limitation, not accepted CPU output. Superseded original helper snapshots/debug99 retained outside final manifest. Initial guessed4B1460 fell inside another function and failed creation; only the correct function boundaries enter manifest. Initial writer fixture lacked virtual enabled34, corrected with failed snapshot retained.1968 instructions21 unchanged assembly/21 live ranges matchPE;244 API checks144 exact in-memory patches,110 Things roundtrip12 records.17 main data/2 EXE remain unchanged.

## Этап37: Things CB EffectGenerator

`EFFECT_GENERATORS_RU.md`, компонент48 `effect-generator-semantics.json`. Primary game:570630 parser,521380 callback,4B81E0 creator,4B7560 constructor,4B19A0 initializer,4B1B60 tick,4B75A0/B0 getters,5558D0 RNG,4B1CF0 native labels. Editor:430DB0 parser,42FAF0 initializer,42FB50 converter,491F20 callback,41F810 constructor,41F990/41F9E0 bounds setters,41FA30/50 effect getter/setter,401A50 no-op flags setter,41FA70 zero flags getter,48AD00 array getter,4947C0 writer. Предварительно неверно названные candidates сохранены для аудита; `phase37-editor-effect-callback` фактически CA и не входит в финальную CB manifest. Имя `phase37-editor-model-flags-set` относится к effect setter41FA50; адрес/assembly имеют приоритет над временным именем.

62 native cases;1703 инструкции23 повторных assembly/23 live ranges matchPE;1137 API boundaries,2880 in-memory exact container patches,110 Things roundtrip80 CB records. В14 raw-хвостах ненулевые значения; padding assumption не применяется. Superseded fixtures сохранены отдельно: адрес записи RNG log исправлен с EBP-relative на absolute; full writer length36 capture учитывает header+payload2 calls. Финальная manifest содержит только прошедшие snapshots. Исходные17 входов/2 EXE сохранены. World/provider/helper fixtures и отсутствие gameplay квалифицированы в схеме и документации.

## Этап36: King lightning immunity и квалификация уверенности

`KING_LIGHTNING_RU.md`,47-й компонент `creature-lightning-immunity.json`: два standalone/три Globals, KeeperSpells/Shot links, Level20 references;10 новых source hashes. Оригинальный EXE изDKII13 SHA93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342 скопирован в source-binaries и импортирован в существующий headless Ghidra project без изменения original. PE version resource отсутствует: имя папки не удостоверяет все исторические сборки1.3. В старом Ghidra создана разметка фактических функций4A60E0/55F170/48A150 по PE-границам; отсутствие исходной функции не трактовалось как отсутствие поведения.

22 full-handler emulations, actual flag gate/arithmetic/target lookup, event/health fixtures;28 raw boundaries10 exact in-memory patches.6383 инструкции6 повторных assembly/6 live program ranges matchPE;10 data sources/старыйoriginal+copy/прежние17 данных2 EXE хеши сохранены. Первый fixture ошибочно ожидал чистый DWORD sourceTag; original ABI задаёт WORD, старшие биты аргумента не определены caller. Snapshot оставлен как phase36-superseded-source-tag-upper-bits.json, readout исправлен на MOVZX WORD,22 финальных cases проходят. Полное здоровье/агрессия/runtime patches не проверены. Никакая количественная99,9% гарантия всем исследованным полям не приписывается.

## Этап35: Level DF, scratch и native CString

Русское изложение и границы выводов — `LEVEL_DESCRIPTOR_RUNTIME_RU.md`; физическая таблица — `LEVEL_LAYOUT.md`.

`build_level_descriptor.py` формирует46-й компонент `level-descriptor-semantics.json`. Writer493200 сравнивается целиком, с настоящим4AD490 stack probe,48CF90/4AB3B0,48EA60 и getters массивов. UI/CString ownership/name/message/date/file-provider fixtures явны. Native converters56C100/42BDE0 и их CRT helpers также исполняются целиком.14 writer cases (13 bounded +1 Information1027),18 converters,6 string helpers; сохранены полные args/expected/results в phase35-emulation-level-*.json. Во всех сравнениях CMPSB, не хеш-сходство.

113 Level (112 corpus+AutoSave),53 разных opaque520,55 ненулевых;1017 точечных пересборок2054 boundary cases,14 созданных в памяти файлов parse→pack.1456 инструкций23 повторные disassembly23 actual-memory ranges matchPE.17 входных данных2 EXE хеши неизменны. Первый аудит ошибочно принял `Section.records[0]` Level за отдельный DF; исправлен разрез по declared DF size с сохранением DE tail, входные файлы не затрагивались. UnknownHistory — baseline alias; native writer scratch установлен только при оговорённых helper/model/string условиях, не универсальная история происхождения всех буферов.

## Этап34: ActionPoint editor controls, auto-ID и self-links

`build_action_point_editor.py` формирует45-й компонент `action-point-editor-semantics.json` из constructor4322B0, fill4323A0, DDX432530, apply432850, combo4325C0/index432820, auto-ID41B840 и setters41B930/50/70/90. Resource278 `[5,278,2057]` взят из `editor-dialogs.json` с SHA-256; control IDs/field pointers подтверждены32-byte CMPSB. Первоначальные поисковые файлы phase34-AP-panel-DDX* фактически относятся к destructor432330 и не включены в доказательства; phase34-AP-panel-commit* содержит DDX432530, apply —432850. В схемах указаны правильные адреса.

46 native runs с явными UI/CString/map/array fixtures;3 apply сравнивают все60 байт модели,8 combo — весь выделенный массив;4 дополнительных выполняют allocation-ветку с настоящим zero helper403CA0 и буфером A5. Первый apply остановился из-за отсутствующего sentinel в readout адаптере после native RET, сохранив правильные выходные поля; неудачный fixture оставлен как `phase34-superseded-apply-readout-missing-sentinel.json`, исправленный adapter явно PUSH DEADBEEF;RET.4603 boundary cases252 exact in-memory patches;3422 AP109 файлов,170 self-links31 файл, predicted UI projection→0.583 инструкции13 повторных disassembly13 live ranges совпадают с исходным PE;17 данных2 EXE не изменены. Full real GUI allocation/save и игровой смысл04/08 не заявляются.

## Этап33: ActionPoint geometry/timer, flags и patrol

Primary: parsers5700A0/430910, converters56F790/42F8E0, loader5211A0, create4B7F20/init4B0420, trigger4B0C90, five flag setters4B0AF0..4B0B70; editor packed codecs41B9D0/41B9B0, loader4918B0, legacy-clear41B950, array getter48A910, writer4947C0. Consumers471EB0 patrol,472150 next,4721C0 wait,4B07E0 inclusive fast area,510E20 flag20 linked-list selection. sgmap names used as leads; misleading radius/update/helper candidates identified as saved-game I/O and trigger dispatcher by actual code. UI labels dialog278 are matching text only; complete control-to-bit path not claimed.

70 native runs:14 complete parsers,17 complete loader/create/init with allocation/constructor/base-init/checksum/trigger fixtures,8 full writers with2 AP and other counts0,5 next,4 area,11 patrol,5 wait,6 editor codecs. CMPSB full92/64/112 buffers, writer mutated flags and actual state outputs. Native nav/terrain/PRNG/clock/provider fixtures explicit.65626 boundaries252 exact container edits in memory;3422 AP in109 files, no missing nonzero next links.1082 quoted instructions,26 unchanged after-disassemblies and26 actual Ghidra memory ranges match PE.17 inputs/2 EXE hashes unchanged. Kinds4B0460 etc originally absent from Ghidra were created in research DB; no program patches. Test-case insertion caused cache identity rejection for positional names; inputs retained, wait cases moved to end,4 extra area outputs excluded from final70 manifest. Component44 — action_point_semantics.


## Этап32: HeroParty variants, opaque header и member rules

Primary: game570210 parser,5212A0 callback,50FA70 world store; editor430A70 reader,42F9A0/42F9E0 initializer/converter,491A40 party callback,4947C0 writer,4952F0 Good serializer,421190/4211B0 member get/add,421170/421340 count/free,41CFA0/41CFC0 leader/follower setters,48DDE0/48DE00 party count/get. All relevant Ghidra function bytes read via read_memory and compared directly with original PE:46 ranges;46 unchanged after-disassemblies. Assembly first-party, sgmap used only for leads.

50 native runs:16 full parsers with read/callback/error UI fixtures,4 full game callback/world-store with checksum fixture,24 post-construction editor member fragments and6 full Things writers using controlled counts/date/virtual I/O. Original strncpy, converters, party get/count/member get/add/free/leader/follower and full Good serializer execute. REPE CMPSB compares complete631/520/96/555 byte expected buffers, not hash equality. Prefix CFG593 instruction states checks direct stack writes/PUSHes; bounded date/Good output and readonly file contracts explicit.942 cited instructions,517 boundary checks1184 exact in-memory edits and8 appended records of four sizes.109 Things contain782 C9 records/5768 occupied members.17 data/2 EXE hashes unchanged. Error-case missing UI fixture retained as phase32-superseded-error-ui-fixture-missing.json and excluded; corrected case asserts0 return. JSON tuple/list normalization in stack-audit output fixed without changing native evidence. Component43 — hero_party_semantics.


## Этап31: creature placements, проценты и два unknown bytes

Primary: game converters56F330/56F430/56F540, callbacks520180/520460/5208B0, twelve boolean setters48DF10..48E140, trigger49A950, gold48AE70, ftol634F30. Editor converters42F410/42F510/42F620, callbacks490EB0/491120/491340, Good serializer4952F0, codecs41D170/41D1C0/41D220/41D250, finalizer41CF20, AP41CF30/41CF40. Function boundaries editor callbacks verified against PE; create_function modifies research DB only. Native debug dump490240 reads417/418 and uses PE strings69CF54 PartyId/69CF3C WanderRadius. Real same-party consumer458C70 independently ties417 to leader/follower filtering. sgmap names were leads, not semantic proof. No runtime radius consumer established by mapped-span search.

88 native runs:27 full modern editor callbacks,12 converters,18 game trigger/flag fragments,6 full Good writers in paired24+8 readouts,12 gold helpers,7 health x87 expression captures before ftol,6 full same-party consumers. Explicit construction/array/I/O/objective/virtual fixtures; real getter/setter/lookup/vector/finalizer/integer Gold bodies. Full health/dying and legacy major1 editor floating conversion excluded.2213 checked instructions,41 unchanged full after-disassemblies,1015 boundary checks,2064 exact container patches in memory.109 files/8717 direct creatures, C9 members excluded.17 input/2 EXE hashes unchanged. Early Keeper AP observer read model24 instead of1C; original saved as phase31-superseded-keeper-ap-observer.json and excluded. Fixture overlap guard prevented an earlier definition/vtable overlap before any emulation. Missing function48D050 query error retained; no semantics inferred from it. Component42 — placed_creature_semantics; prior component audits allow later components.


## Этап30: placed Objects и native writer gates

Primary: game converter56F730/callback520BB0/Gold helper49C760 и PE dispatch tables520D64/520DA8/520DC0. Editor4916B0 найден через xrefs4206A0 и проверенную границу PE4916B0; функция создана только в исследовательской Ghidra DB. Lookup4205D0, setters4206A0/420730, getters420610/420670/4206F0, vector492380 и writer4947C0 подтверждены assembly. Флаги3D8/3DC/3E4/3EC сверены с native420E80 и прежними native-подписями типов. Имена remap targets Special117..129 взяты из самостоятельного Objects.kwd, не подменяют разрешение ссылок в Globals карты.

99 native-прогонов:74 полных game callbacks с checksum/create fixtures и настоящим integer Gold helper;12 полных editor callbacks с constructor/array fixtures и настоящими lookup/setter/selector/vector;10 writer payload fragments с настоящими getters и захватом всех24 байт;3 converters. Эмуляция проверяет ID7/selector258 с Book bit8, а штатное определение ID7 в standalone имеет Special, не Book. Эти условия явно отделены.562 инструкции,12 повторных полных assembly без изменения,17 данных/2 EXE сохранили хеши.1586 границ,96 точечных Things-пересборок в памяти.109 файлов/1805 Objects,12 пропускаемых ID28. Ранний position-fixture с перекрытием регионов сохранён как phase30-superseded-position-fixture-overlap.json и исключён; исправленные99 cases проверяют непересечение fixture regions. Компонент41 — placed_object_semantics; audit29 допускает>=40 компонентов.


## Этап29: Things Trap/Door placements

Primary: game converters 56F690/56F600 и callbacks 520E50/520F40, activation 4A96D0, blueprint setter 4A66C0, state setter 4A6800; editor codec 41ED30/41ED10 и writer 4947C0. Полные assembly — основа вывода о NumberOfShots 0/неноль и Door flags 01/02. Верхние биты EAX encoder не определены; используется AL. Helper 5066E0 отдельно просмотрен: запрос manufacture учитывает лимиты и существующие ящики; 505680 добавляет tag в companion list. Сам запрос не доказывает создание новой задачи.

28 native-прогонов: 8 полных Trap callbacks с явными create/checksum/manufacture/refresh/neighbor/upkeep stubs и настоящим activation body; 7 Door flag/setter fragments с resource/nav fixtures, 7 editor codecs, 6 converters. Проверены 281 инструкция и 9 полных повторных assembly без изменений; 17 данных и 2 EXE сохранили хеши. 527 граничных проверок и 294 точечные пересборки Things в памяти. В 109 файлах — 5803 Trap и 4700 Door; ненулевые неизвестные хвосты сохранены. Компонент40 — placed_structure_semantics; audit28 допускает >=39 компонентов. Objects 520BB0/56F730 собраны для следующего разбора; новые назначения их полей здесь не установлены.


## Этап28: Map cells и writer

Primary: game parser56D2D0 и callback51EC60, editor parser42D1F0, cell init42D1A0/converter42D1B0, writer494470. Полные original writer/parser в22 native-прогонах с явными I/O/callback fixtures, guard-байтами и scratch seeds; loader room construction не эмулировался. Game saved-game CMap44F3F0/44F4B0 исследован как отдельный формат и не принят за KWD writer. Capstone поиск immediate100/101/102 дал правильного editor writer494470. Native hardcoded IDs4/5/17/26/36 сопоставлены с текущим Terrain.kwd.

1 цитата218 инструкций,4 повторные assembly без изменения,17 данных/2 EXE сохранили хеши.1287 границ96 точечных Map-пересборок,109 карт583365 клеток. Четвёртый байт0/1/2 сохранён raw; вариации0/1/A5/FF дали одинаковые callbacks полного game parser. В раннем runtime-fixture readout ошибочно стоял перед store; он сохранён как phase28-superseded-runtime-readout-before-store.json и не входит в22 успешных. Корректные fixture-выходы расположены послеOR/AND. Новый компонент39map_cell_semantics; container aliases bridge→flags/unknown→opaque, audit27 допускает>=38.


## Этап27: Effects writer fidelity

Primary: editor4AA040 writer,41FF90 decoder,41FEC0 encoder,4A2F50 callback,41FDA0 default generation,41CE60 resource getter,4AB3B0 CString helper и4AB320 header-date helper; game56AFE0 converter/4358C0 loader.332 проверенные инструкции,9 полных повторных assembly без изменения,17 входов/2 EXE сохранили хеши. Статический CFG writer по PE:190 instruction/stack states, без прямой записи/PUSH вentryESP-4..-2, соответствующие rawF4. Косвенные копирования отделены, предпосылкаCString<=32 и read-only file callback явная.

31 native-прогон:10 чистый flags decode/encode,10 callback/default/encode,3 nonzero tail overlay,4 whole-converter и4 writer-suffix capture. Последние исполняют original helper/resource/name/copies и захватывают246 байт, включая предварительно заданный F4. Один ранний fixture имел отсутствующий return sentinel адаптера, сохранён как superseded и исключён из31; после исправления все readouts и нормальный возврат совпали. FNV нужен только fixtures, не означает checksum игры. Void-декомпиляция41FEC0 скрыла возвращаемый EAX и не используется; полные инструкции и PE проверены.

`phase27-validation.json`:530 границ,14430 точечных пересборок в памяти;80 файлов29467 Effects, всеF4 ненулевые,12 значений. СамостоятельныйEffects.kwd:370 нулей;AutoSaveGlobals:370 значений3880. Новый компонент38effect_editor_semantics; audit26 теперь допускает>=37 компонентов. Новое назначение RandomColorIndex/TableColorIndex и Misc2/Misc3 не заявлено.


## Этап26: KeeperSpells battle AI и native flags

Primary: game503060, converter56BAB0, loader4F7CE0; editor converter42B720/initializer42B600/callback4A25E0, decoder420270/encoder420230/writer4A99B0; верная Keeper panel451CC0, constructor450A10, DDX450F80, commit451FD0. Номера control1796..1799 и native-подписи заново извлечены из PE dialog258. PRNG5558D0: оригинальные ROR/DIV инструкции устанавливают0..bound-1; distribution не утверждается. ReciprocalD1CC931B/2^43 для cost/2499 доказан на всём u32 диапазоне оценкой ошибки в phase26-validation.json.

48 native-прогонов,329 границ,273 пересборки в памяти;80 файлов1040 KeeperSpells.13 цитат347 инструкций,5 полных assembly не изменились после эмуляций;17 исходных данных и2 EXE сохранили хеши. Компонент37 — keeper_ai_semantics. Прогоны selection используют явные stubs внешних helpers, реальный target/cast/GUI не запускается.

Xc8 остаётся неизвестным: новое доказательство editor Q12/model178/writerC8 отделено от RechargeTime CC/model180. Похожий fill4439A0 отвергнут: getter4446F0 использует массив+598/count+59C другого каталога; настоящий Keeper getter452BE0 соответствует+560/+564. Поиск displacement3A с ошибочным фильтром vcallE4 был повторён с правильным keeper slot110; найденный4BB2A0 читает3A списка сущностей, не spell definition. Поиски Creatures6EB/75C/68E дали только storage/set/copy и ложные DWORD-старты внутри WORD-префиксов, новых семантических назначений этим полям не присвоено.


## Этап25: Creatures first-person consumers

Primary: game0x40E910 и0x40FDF0, getter0x4943F0, world lookup0x50DFA0, camera getter0x43A980, SniperMode action0x5171D0. Новые assembly в `ghidra/phase25-*-assembly.txt`; native diagnostic label SniperMode — прежняя выгрузка490240. Storage paths дополнительно проверены по game-creature-convert/type. Точные значения2048 signed DWORD table0x6C1488 и указатели vtable66CC94+D0/66E3EC+E8 прочитаны из исходного PE. Initializer511A80 дополнительно связывает type27 с handler5171D0: запись6A5A1C, type+0, handler+4; подтверждён assembly, а не только имя функции. Структура камеры из sgmap используется только как дополнительная раскладка; физическая единица camera+1D8 не присвоена.

`phase25-first-person-emulations.json`:65 оригинальных native-прогонов;40 motion fragments,21 command producer,4 flag handler. Capture и entry/exit adapters только в EmulatorHelper memory; native getter и пустой lookup исполняются, renderer/полный caller не исполняются. `phase25-validation.json`:613 границ,3036 контейнерных пересборок в памяти,80 файлов/3642 Creatures.13 цитат/273 инструкции,7 повторных assembly без изменения;17 входов/2 EXE сохранили SHA-256. Новый компонент36 `creature_first_person_semantics`; старый audit24 допускает>=35 компонентов. Поисковые кандидатные списки сами по себе не доказывают назначение поля.


## Этап24: Creatures Ability Creep Percentage

25 октября2026 прежний Ghidra-server не работал. `list_instances` вернул пустой список; первый probe сохранил Unknown tool как error-артефакт. Исследовательский `start_research_headless.ps1` исправлен на прямой java.exe установленного JDK21, чтобы PID не указывал на Oracle shim. Новый PID записан в `headless-instance.json`; проект DriverResearch открыт штатно. Lock-файлы вручную не удалялись, очистку старых presave произвёл сам Ghidra. Повторные probes успешно работают через localhost8089.

`probe_phase24_creature_creep.json`, `probe_phase24_creep_consumers.json`, `probe_phase24_creep_controls.json`:8 функций содержат11 inline-ветвей runtime+760, game converter568E30/loader492320 и Stat26 setter494A70. Diagnostic dump490240 прямо называет instance3E6 bit1000 Creeping; native STRINGTABLEF85C — Ability Creep Percentage.Полная маршрутизация команд не додумана: исследованный handler517310 меняет бит по low1 action-value, но вся dispatch-цепочка не заявлена. Функция517310 создана только в исследовательском проекте после сверки74 байт с существующей картой и PE. Ошибка No function до создания сохранена отдельно.

`build_creature_creep.py` / `creature-creep-semantics.json`:13 цитат и396 инструкций, signed level-product/100 затем unsigned low32 distance-product/100, строгая числовая sight-граница, direct DWORD storage и отдельное native-editor byte narrowing. Прямая инструкция509A96 связывает runtime pointer6E4154 с6F2550; адрес lane стат26 выведен из native65-handler и проверен для256 индексов. В first-person speed helper mode2 выбирает runtime5BC/ShuffleSpeed, не760. Строки стороннего .bt послужили ориентиром, назначение установлено независимо по EXE.

`emulate_creature_creep.py` / `phase24-creep-emulations.json`:116 успешных оригинальных instruction-прогонов. Первый level-scaled f1-прогон выявил отсутствие кэшированной метрики из пропущенного пролога; неудачный и два предыдущих fixture сохранены как `phase24-superseded-cache-fixture-f1-*.json`, исключены из116. После явной передачи cache-slot оба вариантаf1/f2 совпали с независимой арифметикой. Game pipeline цепляет converter к loader, editor pipeline — callback к writer; адаптеры существуют только в EmulatorHelper.

`audit_creature_creep.py` / `phase24-validation.json`:467 границ,1104 точечные пересборки в памяти;3642 Creatures80 карт:3562 значения50,80 значений0; native-byte projection не меняет эти данные. В105 успешно разобранных map-файлах Variables нет type65/Stat26;9 базовых записей уровней отдельно прочитаны из GlobalVariables.kwd.14 полных повторных assembly сохранили инструкции,17 данных и2 EXE сохранили хеши. Добавлен компонент35. Старый audit этапа23 теперь допускает>=34 компонентов при неизменных семантических проверках.

## Этап23: native Terrain/Traps mana consumers

`probe_phase23_terrain_mana.json`, `probe_phase23_player_mana.json`, `probe_phase23_mana_sources.json`: новые разрешённые запросы действующего Ghidra MCP8089. Ключевые функции4559D0 (ownership),455B90 (type change),4C1770 (gross),4C10E0 (income tick),4C17A0 (cost helper),4C1B60/4C1B90 (trap upkeep). Debug dump4BCF10 прямо называет player10DE MaxManaGainPerSecond,10EA MaxMana и другие поля. Полные assembly подтверждают MOVSX WORD, signed MIN, последовательность gross/cost/multiplier, numeric mode3/settings0/2 и unsigned deadline. Native-подписи режимов по числам не назначены.

`build_mana_semantics.py`, `mana-semantics.json`, `native_mana_semantics.py`:3 подписанных игровых пути от raw через intermediate к runtime,23 проверенные цитаты и282 инструкции. Сохранены прежние baseline-u16 схемы; signed16 — семантический вид exact-EXE. Caller xrefs и4 дополнительных decomp в `probe_phase23_upkeep_callers.json` / `probe_phase23_upkeep_paths.json` показывают bit20-условия и add/remove вокруг других helper. Их полный смысл не додуман.

`emulate_mana_semantics.py` / `phase23-mana-emulations.json`:62 успешных прогона с точными inputs/results. В ownership stub только4BE070, режим4 отключён; Terrain-change и upkeep выполняют полные исходные тела с readout-trampoline. Income — исходный fragment с явно заданными двумя cost-helper возвратами. Первый upkeep-прогон имел перекрывающиеся синтетические player/trap buffers; сохранён как `phase23-failed-overlapping-adapter-upkeep-zero.json`, исключён из62. Буферы разнесены, успешный прогон сохранён отдельно.5 whole-function assembly после эмуляции совпали побайтово с исходными; программа Ghidra и EXE не патчились.

`audit_mana_semantics.py` / `phase23-validation.json`:279 границ,1764 пересборки контейнеров в памяти;3224 Terrain и1261 Traps в80 файлах карт. Отрицательных raw-signed значений нет, signed-границы подтверждены native-эмуляцией.17 файлов и2 EXE сохранили хеши. Добавлен компонент34 общего пакета. Для совместимости старого аудита этапа22 ограничение количества компонентов изменено с==33 на>=33; его семантические проверки не изменялись.

`scan_adjusted_pointers.py` / `phase23-adjusted-pointer-candidates.json`:8729 mapped spans,22572 LEA/ADD/SUB seed,38 коротких кандидатов. Только fall-through register aliases, без межпроцедурного анализа, stack reloads и доказательства identity. Четыре прежних неизвестных поля этим поиском не расшифрованы.

## Этап22: девять native-сумм и массив Terrain

25 октября2026 через действующий MCP8089 получены assembly7 загрузчиков; для Rooms/Creatures использованы и повторно проверены прежние выгрузки0x4EACC0/0x492320. `probe_phase22_checksums.json`, `probe_phase22_after.json` сохраняют первичные ответы. `build_catalogue_checksums.py` закрепляет9 размеров,9 runtime-DWORD и9 callback-прибавлений; различает global0x6FCDCF и world+0xA3C7. `0x5108C0` заново декомпилирован: MOVSX из byte в signed-аккумулятор world+0x72E7. Effects/EffectElements в схему9 семейств не включены.

`emulate_catalogue_checksums.py` / `phase22-checksum-emulations.json`:53 успешных прогона исходных ветвей с явными entry/readout adapters. На первом Doors-прогоне выяснилось, что установка EFLAGS через инструмент сама по себе не задаёт ожидаемый ZF; эта неуспешная проверка сохранена как `phase22-failed-eflags-adapter-Doors-zero.json` и исключена из53 успешных. Adapter исправлен на исполняемый MOV/TEST перед исходным JNZ. Флаги предыдущих TEST не считаются проверенными этим adapter; адреса и условия доказаны полной assembly. После эмуляций9 выгрузок инструкций не изменились; у прежнего Rooms есть только дополнительная завершающая пустая строка.

Terrain: raw1AE→mid266→editor model328; getter425400/setter425420, прямые xrefs49F883/4A7972 и writer4A7690. `audit_catalogue_checksums.py`:669 границ,369 пересборок в памяти;3224 массива80 файлов карт полностью нулевые.17 цитат и229 инструкций сверены;17 файлов и2 EXE сохранили хеши. Подготовлен компонент33 `catalogue-checksums.json`. Полное построение intermediate из raw и игровой смысл массива не заявлены.

`scan_overlapping_unknowns.py` воспроизводит1344 кандидата,446 предварительных пересечений для4 неизвестных полей. Новый разбор4D5C00 связывает DWORD+106 с navigation/path_last_progress; это отвергнутый кандидат Objects.Mana Gen. Поиск не доказывает глобальное отсутствие потребителей. Ошибка отсутствия функции60C010 не стала доказательством назначения.

## Этап21: восстановленный Ghidra-сеанс и EffectElements color/fade

25 октября2026 восстановлен headless-проект `headless-project/DriverResearch.gpr`, подключён native-server по127.0.0.1:8089. Сначала случайно оставшийся исследовательский сервер8080 держал проект; он штатно закрыл DriverResearch через API и был остановлен. Файлы блокировок вручную не удалялись, новый импорт EXE не делался. `headless-instance.json` теперь содержит реальный PID сервера и PID Java-launcher отдельно. На новом сеансе повторно получены0x435F20/0x436430/0x4366A0 и полные assembly первых двух функций.

Динамические инструменты Ghidra зарегистрировались на сервере, но не появились в фиксированном списке инструментов этого клиента. Запросы выполнены существующим `ghidra_probe.py` через тот же локальный MCP-bridge; ошибки отсутствовавшего проекта сохранены как error-артефакты и не используются как доказательства. Это новые разрешённые запросы25 октября, не повтор заблокированного ранее allocator0x554B30. К нему запросов в этапе21 не было.

`build_element_color_semantics.py`: raw-converter0x56B570, definition loader0x4366A0, initializer0x435F20, tick0x436430. Primary evidence — `phase21-element-init-assembly.txt`0x4361CB..0x4361FA и0x43630B..0x4363B4, `phase21-element-tick-assembly.txt`0x436430..0x436445/0x4364DD..0x43659C. Signed IDIV RGB, signed alpha-condition и unsigned DIV alpha проверены отдельно. Хранение RGB/индексов/процента закреплено прежними converter/loader-цитатами; индексы семантически неизвестны.

`emulate_element_colors.py` / `phase21-color-emulations.json`:26 EmulatorHelper-прогонов исходных fragment-инструкций. В emulator memory добавлены jump/readout/RET adapters и player-color stubs; программа Ghidra и EXE не патчились. Whole-function code, ресурсы, RNG и framebuffer не эмулируются. `phase21-element-init-assembly-after-emulation.txt` и tick-выгрузка совпали с доэмуляционными. Сохранены точные inputs, adapters, expected registers и results каждого прогона; аудит пересчитывает ожидания независимой read-only проекцией и проверяет хеши.

`element-color-semantics.json`, `native_element_colors.py`, `audit_element_colors.py`:330 границ,10728 точечных пересборок в памяти,18 цитат и145 инструкций. В80 файлах47224 элемента; индексы везде0, FadePercentage100/70 в47144/80 записях.80 значений70 принадлежат Blood Pool Element294. Пороговые проекции крайних MinHp/MaxHp выполнены76577 раз.17 файлов данных и2 EXE сохранили хеши; модифицированная игра не запускалась.

## Этап20: callback-нормализация и first-person DDX

`build_creature_editor_normalization.py` соединяет editor converter0x4287C0, callback0x4A01B0 и writer0x4A7F00 для raw+0xDBC/+0xDC0/+0xD3D/+0xCF9. Сбросы стоят после presence-ветвей в сохранённом callback. Точные double-пороги1/10000 и масштабы1/4096/4096 проверены в PE. Для целых raw-u32 это точные строгие пороги4096/40960000. Сброс byte>12 в4 не применяется к0.

First-person constructor0x4392A0 запрашивает dialog310; init0x439470, DDX0x439770 и commit0x43A060 подтверждают edit IDs0x821/0x741/0x743 и диапазоны0..1/0..1/1..12. Имена поисковых phase17-файлов с f55 не используются как доказательство связи с Pick-Up Priority. Для полного поведения при ошибке DDX не хватает анализа helper/caller; commit вызывает UpdateData(1) и не проверяет возврат перед следующими копиями. Поэтому0 у Walk Cycle считается только вне объявленного диапазона, а не доказанно несохраняемым.

В writer0x4A8577..0x4A8582 два PUSH между чтением model byte и записью ESP+0xDD9 меняют положение буфера:0xDD9-(0x94+8)=0xD3D. Эта поправка проверена отдельно. Game loader0x493C51..0x493C8F копирует соответствующие DWORD/DWORD/u8→u32 без editor callback-сбросов; позднейшие игровые потребители этим не описываются.

`creature-editor-normalization.json` / `phase20-validation.json`:92 границы,552 точечные пересборки в памяти,34 цитаты,96 инструкций и ресурс dialog310. В80 файлах3642 существа; нормализация не меняет ни одну из этих реальных записей.17 входов и2 EXE сохранили хеши. Работа выполнена по сохранённым выгрузкам; новых запросов к Ghidra и попыток обойти ранее заблокированный запрос allocator не было.

## Этап19:22 дробных свойства и условная Torture Time ветка

`build_creature_editor_scaled.py` использует те же сохранённые editor-функции этапа18. Конвертерный pcVar9 рассматривается как raw-base только между явным присваиванием param_1 и первым изменением; последующие loops не получают фиктивный фиксированный base. Fill-alias iVar3 проверяется при каждом присваивании, чтобы объединить две дополнительные property-цепочки Work Modifier Level Increase и Sniper Zoom Multiplier. У22 выбранных полей отсутствует найденная в других ветках callback-нормализация двойных значений.

Из PE непосредственно проверены double-константы0x50AC48/0x50AC58,0x5163B0/0x5163C8,0x5163A8/0x5163C0. Native writer assembly закрепляет пары scale/helper/store и raw-offset относительно буфера ESP+0x94. Callback zero-extension отражена сохранённой последовательностью local_14/uStack_10=0. Типы runtime-игры этим не переопределяются.

Property0xF856 в `phase17-creature-f55-panel-update.txt` вызывает `__ftol` и преобразует low8 в double; main commit вызывает dispatcher для выбранного ID+0x1AA8. Это подтверждает условную ветку GUI, не автоматическую модификацию поля каждым save. Сценарий941/3642 — рациональная проекция исходных значений с предположением truncation и без текстового округления. Новая native-эмуляция и GUI-проверка не проводились. Новый Ghidra-запрос к ранее заблокированному allocator не выполнялся; использованы сохранённые выгрузки и чтение констант других функций.

Схема — `creature-editor-scaled.json`, API — `native_creature_editor_scaled.py`, аудит — `audit_creature_editor_scaled.py` / `phase19-validation.json`. Проверены424 границы и3036 пересборок контейнеров в памяти,22 строки ресурсов и6 double-констант;17 входов и2 EXE сохранили хеши. Пакет расширен компонентом `creature_editor_scaled`.

## Этап18:35 скалярных привязок Creatures

`build_creature_editor_scalars.py` соединяет прямые присваивания в сохранённых editor-функциях0x4287C0/0x4A01B0/0x43F2A0/0x4405E0/0x43FD60/0x441160/0x4A7F00. Базис int*-указателей this/piVar1 в callback учитывается явно: this+0x57A означает byte-offset0x15E8. Source-параметры decoder и game-converter не смешиваются; промежуточные смещения из разных структур не отождествляются. При неполной или неоднозначной цепочке новая привязка не создаётся.

`creature-editor-scalars.json` хранит35 полей с STRINGTABLE-ресурсами, точными excerpt/line и writer assembly. `audit_creature_editor_scalars.py` проверяет350 цитат,72 инструкции,35 ресурсов строк и сходство Pick-Up Priority с этапом17. API возвращает числовые view и копии байтов, исходные файлы не пишет. В корпусе80 карт3642 существа;+0xF53 хранит188/200 во всех записях. Отдельная проверка диапазона GUI по подписи Percentage не выводится.1001 граничный случай,4830 пересборок в памяти, неизменность17 входов и2 EXE — `phase18-validation.json`.

Этап добавляет native UI evidence для шести ранее подтверждённых game-полей из `verified-overrides.json`; эти игровые семантики заново открытыми не считаются. Новое назначение Fly Height Percentage относится к родной панели; игровой эффект+0xF53 не установлен. Использованы уже сохранённые выгрузки, новые Ghidra-запросы после отказа автоматической проверки из-за лимита не выполнялись. Это не обход запрета на заблокированный запрос к allocator0x554B30.

## Этап17: цепочки Effects и Pick-Up Priority

`build_phase17_semantics.py` строит `effect-chain-semantics.json` и `creature-priority-semantics.json` по сохранённым native-выгрузкам. `audit_phase17_semantics.py` проверяет цитаты,77 инструкций, четыре указателя vtable из PE и ресурс строки0xF855 с хешами текста/блока. Результаты — `phase17-validation.json`, документация — `EFFECT_CHAINS_RU.md`, `CREATURE_PRIORITY_RU.md`.

Создание Effects — game0x440350, `ghidra/phase17-effect-chain-create-assembly.txt`; initializer0x4347E0 и getter0x440BC0 сохранены отдельно. Vtable base0x66CC94, slots90/94/98/184 проверены непосредственно в EXE. Хранение NextEffectId подтверждают converter0x56AFE0 и loader0x4358C0. Декомпиляция general-gate0x440420 имеет неполное восстановление аргументов стека и не служит полной моделью параметров. Allocator0x554B30 не разобран: запрос отклонён автоматической проверкой разрешения из-за лимита. Обход проверки не выполнялся; размер запроса0x159 не интерпретируется как ёмкость пула.

Creatures: raw-converter editor0x4287C0 и game0x568E30, editor callback0x4A01B0, main fill0x43F2A0, property extract/update0x4405E0/0x43FD60, commit0x441160, writer0x4A7F00. Полный writer assembly устанавливает относительное смещение0xFE9-0x94=0xF55. `extract_editor_strings.py` извлекает436 Win32 STRINGTABLE-строк из EXE; resource0xF855 содержит Pick-Up Priority. Игровой loader0x492320 сохраняет байт в+0x6EB. Первый поисковый след через intermediate+0x1345 оказался неоднозначен между структурами игры/редактора; окончательная схема это исправляет. SpellSwipe+0xF45 отдельно ведёт в runtime+0x684 и consumer0x4100F0.

Поисковые имена phase17 не считаются доказательством назначения. `phase17-creature-f55-ui-fill.txt` содержит destructor, а не fill; first-person материалы относятся к SpellSwipe. Правильный main fill хранится как `phase17-creature-main-panel-fill.txt`. Full-text displacement candidates содержат совпадения других классов и не подтверждают игровой смысл+0x6EB или палитровых индексов EffectElements.

Проверены80 файлов карт,29467 Effects,4682 ненулевые ссылки и3642 Creatures;52 граничных случая,1248 точечных пересборок контейнеров в памяти. Новые native-эмуляции и игровой запуск с изменёнными данными не выполнялись. SHA-25617 входов и2 EXE совпали. Область записи — только исследовательская папка.

## Локальные файлы

1. Входы из `D:\Games\DK2\Dungeon Keeper 2\Data\editor`; их SHA-256 — `inventory.json` и `validation.json`.
2. `DK2 Editor pro.exe`, SHA-256 `989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09`. Ghidra 12.0.4, x86 little-endian 32 bit Windows, анализ 8497 функций. Отдельная папка проекта `/DK2-format-research/`. EXE не запускался и не патчился.
3. `D:\Games\DK2\DK2-Map-Editor\Source\Includes\Game.h`, `Source\Sources\Compiler\Compiler_Generate.c` — исходники исторического NovFR. Это отдельная реализация, не исходники Bullfrog EXE.
4. `D:\Games\DK2\DK2-Map-Editor\src\DK2MapEditor.Core\Editing\LegacyGlobalSchemas.cs`, `NativeCreatureDefinitionLayout.cs`, `NativeMapDocument.cs` — текущая реализация для сравнения покрытия, без изменений.
5. `C:\Users\Quuz\Downloads\DKII GlobalValue.xlsx`: формулы и сохранённые значения прочитаны непосредственно из OOXML, без изменения/пересчёта книги. Полный разбор — `workbook-cells.json`, точечная сверка — `workbook-comparison.json`. Названия и гипотезы из таблицы не считаются инструкциями.

## OpenKeeper

Исходники закреплены на коммите **59c1966b80b589cf397476b421fd779527b8e2c4**. Ответ API — `sources/openkeeper-commit.json`, дерево — `sources/openkeeper-tree.json`. У сохранённых Java-файлов оставлены исходные заголовки OpenKeeper GPL-3.0-or-later. Код OpenKeeper не подключался к параллельному приложению.

- [Проект OpenKeeper](https://github.com/tonihele/OpenKeeper)
- [KwdFile.java: последовательное чтение всех каталогов, карты и Things](https://github.com/tonihele/OpenKeeper/blob/59c1966b80b589cf397476b421fd779527b8e2c4/src/main/java/toniarts/openkeeper/tools/convert/map/KwdFile.java)
- [ResourceChunkReader.java: знаковость и преобразование fixed-point](https://github.com/tonihele/OpenKeeper/blob/59c1966b80b589cf397476b421fd779527b8e2c4/src/main/java/toniarts/openkeeper/tools/convert/ResourceChunkReader.java)
- [Creature.java: перечисления/флаги существ](https://github.com/tonihele/OpenKeeper/blob/59c1966b80b589cf397476b421fd779527b8e2c4/src/main/java/toniarts/openkeeper/tools/convert/map/Creature.java)
- [ArtResource.java: типы и флаги графических ресурсов](https://github.com/tonihele/OpenKeeper/blob/59c1966b80b589cf397476b421fd779527b8e2c4/src/main/java/toniarts/openkeeper/tools/convert/map/ArtResource.java)
- [Variable.java: типы переменных](https://github.com/tonihele/OpenKeeper/blob/59c1966b80b589cf397476b421fd779527b8e2c4/src/main/java/toniarts/openkeeper/tools/convert/map/Variable.java)

Файлы загружались по точному коммиту, не по плавающему master. Имена смысловых полей в схемах прежде всего отражают интерпретацию этой реализации.

22 октября 2026 дополнительно сохранён весь `src` того же коммита: 786 файлов вместе с лицензиями и корневыми описаниями. API подтвердил совпадение текущего master с закреплённым SHA. Сводка архива — `sources/openkeeper-source-snapshot.json`, хеши файлов — `openkeeper-source-manifest.json`, результаты анализа 501 Java-файла game/view — `OPENKEEPER_AUDIT_RU.md` / `openkeeper-audit.json`. Подсказки по эффектам помечены как реализация ремейка; они не заменяют native-сверку.

## Декомпиляция оригинала

| Семейство | Читатель | Писатель |
|---|---|---|
| Terrain | `0x0042F140` | `0x004A7690` |
| Rooms | `0x0042E2C0` | `0x004A7AC0` |
| Creatures | `0x00429D90` | `0x004A7F00` |
| Doors | `0x0042A810` | `0x004A8BF0` |
| Traps | `0x00431860` | `0x004A8F40` |
| Objects | `0x0042D7D0` | `0x004A9390` |
| CreatureSpells | `0x0042A240` | `0x004A9730` |
| KeeperSpells | `0x0042B960` | `0x004A99B0` |
| Shots | `0x0042E8F0` | `0x004A9CE0` |
| Effects | `0x0042ADD0` | `0x004AA040` |
| EffectElements | `0x0042B380` | `0x004AA3B0` |
| Things | `0x0042FF20` | `0x004947C0` |
| Variables | `0x00432080` | `0x004937C0` |
| Level descriptor | `0x0042C270` | `0x00493200` |
| Map | `0x0042D1F0` | Не исследован заново в этом проходе |

Диспетчер секций `0x0042CA50`, общий писатель Globals `0x0047D510`, преобразование записи Creature `0x004287C0`. Декомпиляции в `ghidra/*.txt` и исходные MCP-ответы в `ghidra/*.json`. Имена FUN_ означают адреса, а не восстановленные исходные имена.

## Проверка

`build_research.py` проверяет точное восстановление из JSON всех запрошенных файлов плюс AutoSaveGlobals. Заголовки и raw-записи сохраняются, скаляры патчатся точечно. Совпадение не заявляется как игровой тест или восстановление неизвестной семантики. Нет доказательства, что любое увеличенное число записей поддерживается движком.

## Дополнение: Discord и шаблоны ufdada

[Keeper Klan / публичный WidgetBot](https://keeperklan.com/forum.php): `general` → ветка **DK2 Hex Editing**. Прочитаны участки сообщений BlueFire, quuz., nsdr2025 и других участников за 2022–2026. Отдельные наблюдения и уровень проверки — `DISCORD_FINDINGS_RU.md`. Вложения не были все просмотрены и не входят в заявление о полноте проверки.

Из ссылок в обсуждении найден [ufdada/dk2-tools](https://github.com/ufdada/dk2-tools). Закреплённое дерево `b4e0972847561ac0657ebdbeb8245d676455e7eb` сохранено в `sources/dk2-tools-tree.json`; шаблоны Formats/Level/*.bt и util_struc.bt сохранены рядом. Их комментарии содержат более поздние трактовки; они не переносятся автоматически в базовую схему OpenKeeper. Конфликты F20, Variables 66 и слотов флагов затем проверены по движку — см. ENGINE_FINDINGS_RU.md.

## Оригинальный игровой загрузчик

Исследован `D:\Games\DK2\Dungeon Keeper 2\DKII-DX.exe`, SHA-256 `d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a`. Образ импортирован в ту же отдельную папку проекта Ghidra, EXE не патчился. Имена/подсказки адресов сверялись с [Flame DKII_EXE_v170.sgmap](https://github.com/DiaLight/Flame/blob/f6e2a3d385bf2e6e95dc70cab88aa387bb7659d9/mapping/DKII_EXE_v170.sgmap); локальная копия sources/DKII_EXE_v170.sgmap. Указание названия функции из карты само по себе не считается доказательством семантики — использованы сохранённые декомпиляции.

| Область | Адреса |
|---|---|
| Каталог Creatures: чтение, конвертер, runtime loader | 0x56A250, 0x568E30, 0x492320 |
| Ограничение поворота | 0x4B2AE0, 0x4B2C10 |
| Проверенные применения дополнительных флагов | 0x4AF1E0, 0x478CE0, 0x4DBF90 |
| Variables: парсер, диспетчер, таблица callback | 0x5716E0, 0x521790, 0x51AA90 |
| ResearchOrder | 0x51DB00, 0x506FF0 |
| Sacrifice | 0x51DB30 |
| CreatureLevelStat / FirstPersonStat | 0x51DC40, 0x51DC70 |
| Alliance | 0x51DC90 |
| ModifyCreatureStat / setter | 0x51DD10, 0x494A70 |
| Level: парсер и конвертер | 0x56C4E0, 0x56C100 |
| Runtime накопление байтов | 0x5108C0 |

Патчи Flame, применяемые в памяти при запуске, и изменения других версий EXE отдельно не трассировались. Выводы о чтении полей привязаны к данному SHA-256; устройство пользовательских файлов подтверждено также их измеренными длинами и сборкой.

## Дополнительные потребители и проверка корпуса

`PHASE2_FINDINGS_RU.md` связывает все 39 callback процессов Shots из таблицы 0x4AD1B0 с диспетчером 0x4AD730, отдельно damage-dispatch 0x4AC3C0 и тик 0x4AB180. Семантика спорных вызовов Alert/Jack проверена также по assembly. Скаляры Variables извлечены из 0x51CD90/0x51CB80; no-op 61–64 — из 0x51DC20/0x401200.

Creatures: Footstep consumers 0x48BE50/0x496700; width 0x4B5D60/0x48F690/0x45A550; mass collision 0x4D2B80 и таблица 0x4D2A60, оба дополнительно сохранены как assembly; work modifier ctor/level-up 0x4872A0/0x48B120/0x48B330. Неполные декомпиляции с неверно определённой стековой очисткой не используются как единственное доказательство аргументов.

Для Triggers/Things правила счётчиков сверены с 0x571290/0x56FA00. Восьмибайтовые схемы параметров Triggers извлечены из того же уже закреплённого KwdFile.java, а не из новой непроверенной версии. Корпус — 737 локальных файлов Data/editor/maps, SHA-256 каждого сохранён в corpus-scan.json. Четыре противоречивых Variables имеют отдельную forensic-интерпретацию без изменения оригиналов.

## Native Effects и EffectElements, 22 октября 2026

`EFFECTS_RUNTIME_RU.md` / `effect-semantics.json`: таблица 19 callback по0x697668 прочитана из PE, каждый уникальный ненулевой адрес декомпилирован. Основной CUBE_GEN0x4386F0, переходы signed16/вычитания256, выбор namespace, RNG gate и границы таблицы дополнительно проверены по assembly. Размеры определений0x94/0x4F подтверждены callback0x440720/0x4405A0, loader0x4358C0/0x4366A0. Физика0x4476E0 и столкновения0x446C70 сохранены вместе с assembly. Элементный tick0x436430 подтверждает EffectElements.NextEffectId → Effects. Копия проекта Ghidra — `headless-project/DriverResearch.gpr`, изменены только исследовательские метаданные/прототипы, EXE не патчился. Неисправленные предупреждения декомпилятора о стеке не используются как единственное доказательство ветвления или аргументов: для спорных мест приведены инструкции.

Дополнительно подтверждены SPRAY0x439330 (raw+DA как EffectId), GOLD_COST0x439480 (серия десяти ElementId base+digit), TEST_3_0x439100 (четыре первых слота, партии ElementsPerTurn>>2) и исключённая верхняя граница EVERYTHING0x436AF0. Соответствующие assembly и три дополнительные машинные записи доказательств входят в effect-semantics.json.

## Native CreatureSpells и KeeperSpells

`SPELLS_RUNTIME_RU.md` / `spell-semantics.json`: таблицы selector извлечены из инициализаторов0x49A6C0/0x5174A0/0x517580; границы и callbacks проверены по0x49A7B0/0x5176B0/0x517730 и сохранённым обработчикам. Runtime70/122 байта и переносы —0x4F7860/0x4F7CE0, преобразования16/32 бит дополнительно подтверждены assembly. Actual research threshold:0x507040/0x507060 и их потребители0x4C2DC0/0x4C31D0 связаны с уже разобранным Variables type17. Файлы `phase6-*-candidates.json` — лишь поисковые кандидаты; сканер Capstone не устанавливает тип объекта. Финальные выводы опираются на названные первичные декомпиляции/инструкции. Ни исходники параллельного редактора, ни EXE не изменялись.

## Native размещение и поиск ID

`CATALOGUE_LOADING_RU.md` / `catalogue-loading.json`: последовательность 0x51DD40/0x51DE50, регистрация счётчиков 0x51DF90 и allocator 0x51E2C0 подтверждены assembly. Terrain: header 0x51EB30, запись 0x51EB50, прямой getter 0x508DD0. Все девять record callbacks сохранены также как инструкции `ghidra/game-phase7-*-record-assembly.txt`. Creature effective key 0x494360 и обе ветки unique/nonunique дополнительно проверены в assembly; getters 0x50D390/0x50E3A0 сохранены как декомпиляции. `native_catalogue_loading.py` моделирует только адресацию ID в явно переданной области, `verify_catalogue_loading.py` проверяет границы и реальные наборы.

`unresolved-field-traces.json`: Terrain 0x198/0x224, Traps 0x204 и Objects 0x34C — подтверждённые переносы без установленной игровой семантики. Для Objects вручную учтён единственный alias `local_10c = param_1`, поэтому этот путь хранится отдельно от консервативного bulk-extractor. `phase7-*-candidates.json` — поисковые результаты, не доказательство назначения или отсутствия использования. Внешние исходники для новых выводов не использовались.

## Native GUI и промежуточные модели Editor Pro, 23 октября

`EDITOR_UI_FIELDS_RU.md` / `editor-field-bindings.json` опираются на ресурсы предоставленного EXE (SHA-256 указан в схеме), callback Terrain0x49F5E0 и Traps0x4A1770, DDX0x46D3B0/0x470740, заполнение панелей0x46DEB0/0x471930, применение Traps0x471E00, writers0x4A7690/0x4A8F40. Прямые свидетельства сохранены в `ghidra/editor-phase8-*.txt`. Для слабо типизированного вызова DDX с третьим аргументом и спорного Traps+0x204 использованы успешно полученные assembly0x470740/0x471930/0x4A1770. Коэффициенты double по0x50AC48/0x50AC58 прочитаны непосредственно из PE.

Ресурсы диалогов разобраны локальным `extract_editor_dialogs.py` в `editor-dialogs.json` без запуска редактора. Style-флаги относятся к шаблону, а не состоянию открытого окна. Неоднозначные скрытые подписи не привязаны к полям по соседству в списке. Выдержка успешно полученного0x426000 сохранена отдельно как `editor-phase8-traps-flags-captured.txt` с указанием неполноты. Четыре ответа об ошибке автоматической проверки сохранены как `-unavailable.txt`; это не декомпиляции и не доказательства. Новые внешние исходники для этого этапа не использовались.

## Составная панель Rooms/Terrain и Lean

`EDITOR_COMPOSITION_RU.md` / `editor-composition.json`: init Lean0x4500A0, DDX0x450130, commit0x450200, getters/setters0x425440/0x425460/0x425480/0x4254A0. Реальная граница DDX установлена после RET0x450123 и выравнивания до0x450130; функция размечена в исследовательской копии. Порядок шести контролов подтверждён ресурсом230, переносами converter0x42ED90, моделью0x49F5E0 и writer0x4A7690.

Rooms: ключ raw0x394 проверен converter0x42DF90 и callback0x49FC30; разделение указателей панели —0x4675F0, обработчик Lean0x468900. Commit0x467BB0 сохранён также как assembly; операции обнуления трёх WORD и принудительная часть флагов сопоставлены с0x4254C0. Objects In Hand Mesh: handler0x45B4D0, getter0x472D10 (также assembly), writer0x4A9390. Сохранённые `editor-phase9-*candidate*` / поисковые результаты не считаются подтверждением имени объекта сами по себе; машинная схема ссылается на конкретные проверенные строки. Внешние исходники для новых выводов не использовались.

## Objects: native-привязки ресурсов

`OBJECT_RESOURCES_RU.md` / `object-resource-bindings.json`: восемь MFC-записей проверяются непосредственно по VA0x511770..0x511818 и сопоставляются с контролами PE-диалога327. In Hand Icon: control1587, запись0x5117A0, handler0x45B410, getter0x45BA00, model0x16C, converter0x42D570, callback0x4A1D80, writer0x4A9390. GUI/Mesh getters0x442BE0/0x4484B0, Additional getter0x420D00 и четыре handler0x45B590/0x45B650/0x45B710/0x45B7D0 также сохранены. Восемь кнопок скрыты в шаблоне; динамическое отображение не доказано.

Игровой converter0x56D620 и loader0x49DD60 связывают raw0x170 с intermediate0x1F0 и runtime0x1B. Назначение ECX подтверждено assembly0x49DF9A перед вызовом0x504D80; helper сохранён отдельно. Baseline InHandIconResource0xC8 сохраняется как отдельный слот; отсутствие его загрузки в исследованном теле не превращено в заявление об отсутствии всех потребителей. Эксплораторные `game-phase10-object-vtable-*` и `*resource-selection*` не являются доказательствами дополнительных назначений. Все положительные выводы входят в машинную схему с проверяемыми выдержками. Новые внешние исходники не использовались.

## Rooms: конструкция и формирование имён ресурсов

`ROOM_RESOURCES_RU.md` / `room-resource-semantics.json`: таблица13 ID и UTF-16 подписей по VA0x513438 и sentinel0xFF прочитаны непосредственно из Editor Pro. Инициализатор combo0x466E90 подтверждает шаг8 и раздельное хранение ID; helper0x4674A0 ищет ID независимо от позиции строки. Поле raw0x395 и Flags0x370 связаны с converter0x56DFD0 и loader0x4EACC0.

Полный assembly loader сохранён в `game-phase11-room-loader-assembly.txt`; правила12 веток, суффиксы/границы циклов, ECX-адреса CWorldEntry, форматные строки PE и таблицы принудительного OR8 проверяются генератором. Special Water:0x4EB151..0x4EB1CD; разные Destroyed/_DESTROYED:0x4EB4B8..0x4EB595 и0x4EB72E..0x4EB80B. Schema не объявляет количество ресурсов числом клеток и не устанавливает всю геометрию комнаты.

Initializer0x56DE40 подтверждает предварительное обнуление intermediate-данных: от DWORD0x35 очищается0x107 DWORD и ещё3 байта. Область включает Flags+0x444, куда converter затем выполняет OR исходных Flags. Поэтому проекция low16(raw Flags) OR8 относится к полному обычному преобразованию, а не к произвольному частичному обновлению промежуточной структуры.

Неизвестный ресурс raw0x1C4: converter0x42DF90, callback0x49FC30, writer0x4A7AC0. Девять MFC-записей Room resource-кнопок0x513540..0x513600 сопоставлены с getters; ни одна не возвращает model0x280. Эксплораторный `editor-phase11-room-construction-option-item.txt` возвращает содержащую функцию0x466E90 и не считается отдельной функцией. Нулевые84 байта в2023 corpus-записях не используются как доказательство глобальной неиспользуемости. Новые внешние исходники и исходники параллельного редактора для этих выводов не использовались.

## Traps: native Flags и trigger/charge gate,24 октября

`TRAPS_RUNTIME_RU.md` / `trap-semantics.json`: полные editor decoder0x4260E0 и encoder0x426000, последний также assembly. Вызов decoder в0x4A1C16 проверен assembly callback0x4A1770; writer0x4A8F40 кладёт результат в raw0x1EC. DDX0x470740 и ресурс326 подтверждают13 checkbox-привязок; неизвестным битам0x4/0x4000 не назначены игровые имена. Запрос по0x426110 вернул содержащую функцию0x4260E0: в схеме указан её фактический entry.

Native-таблица trigger options0x514768 прочитана из PE; editor initializer0x470DF0 и lookup0x471790 сохранены. Game init0x4A93A0 прочитан также как инструкции, таблица трёх ID/callback0x66D430 сопоставлена с tick0x4A84E0. Функции0x4A9290/0x4A92C0/0x4A9350 размечены в исследовательской копии на адресах закреплённой карты Flame. Отсутствие entry до разметки не считалось отсутствием кода. Неполные pressure/trigger декомпиляции не используются для утверждений о всех аргументах.

Радиус:0x4A9400, assembly0x4A943F..0x4A9459; строгая/нестрогая граница0x4A9617/0x4A952B. Вызовы с0/1 проверены0x4A9299 и0x4A8ABD. ChargeTime: loader0x4AA440 IMUL/SHR0x4AA9DD..0x4AA9E9, byte increment и сравнение0x4A8BEF..0x4A8C36, сброс0x4A8C64. Проекции работают с DWORD-переполнением и не объявлены полной эмуляцией игрового состояния.

`phase12-trap98-candidate-init.txt` и `phase12-rejected-player-config-call.txt` относятся к инициализации MyComputerPlayer; source+0x98 не идентифицирован как Trap definition. Эксплораторная0x4A9A00 в `phase12-trap-trigger-pressure.txt` не является dispatch-callback Pressure: фактический callback0x4A92C0 установлен по PE-таблице. Подобные поисковые имена файлов не считаются доказательством назначения. Внешние источники для новых выводов не запрашивались; игровые файлы и приложение пользователя не изменены.

## Traps:ShotOffset и состояние после создания shot

`TRAP_FIRE_RU.md` / `trap-fire-semantics.json`: creator0x4A80E0 сохранён также как `phase13-trap-shot-create-assembly.txt`. Частичные имена файлов `phase13-trap-damage.txt` и `phase13-trap-fire.txt` были поисковыми; назначение creator/caller установлено цепочкой0x4A7FD0→0x4A80E0→0x4B7CD0 и конкретными операциями. Поведение синусного/косинусного поворота подтверждено low32 IMUL/SAR16, а не только декомпиляцией. Оба перекрывающихся2048-элементных view0x6C1488/0x6C1C88 прочитаны из PE и закреплены хешами сырых8192 байт.

Перенос Data1/2 —0x4A8222..0x4A823B; low16 target и0x4AB830 —0x4A8209..0x4A821D. Helper0x4AB830 копирует target XYZ в shot0x131/0x135/0x139; после него Data1/2 присваиваются отдельно. Caller0x4A8043..0x4A804C заменяет Data2 для TrapId16 в ненулевой целевой ветке. Recharge/reset/reveal/threat присваивания0x4A83AF..0x4A841D сопоставлены с loader0x4AA440 и debug dump0x4AA130. Tick decrement0x4A857F..0x4A858B и emission gate0x4A8CD3..0x4A8D09 проверены раздельно; глобальная неизменность Threat или срок его игрового воздействия не заявляются.

`phase13-trap-flag-candidates.json`, `phase13-trap-definition-candidates.json`, `phase13-unknown-flag-candidates.json`, `phase13-threat-state-candidates.json` — локальные поисковые результаты Capstone с известными ложными совпадениями классов. Для неизвестных битов нет нового назначения; таблица флагов не расширена вымышленными именами. Запрос0x4AA120 вернул координатный checksum, несмотря на поисковое имя файла `phase13-trap-threat-getter.txt`; как threat-getter он не используется. Внешние источники, игровые данные и исходники пользовательского редактора не изменялись.

## Doors: границы WORD, здоровье и отдельная стрельба

`DOORS_RUNTIME_RU.md` / `door-semantics.json`: game converter0x56AA40 и editor converter0x42A5D0, loaders0x4A7100/0x4A1320 и writer0x4A8BF0 устанавливают два раздельных WORD+0x234/+0x236. Подпись Manf. To Build и диапазон первого слова подтверждены fill0x447590, commit0x447880, DDX0x4466F0 и ресурсом диалога254; смысл второго слова остаётся неизвестным. DDX0x4466F0 вручную создан как функция в исследовательском проекте по адресу из vtable0x50E528. Исходный EXE не менялся.

Signed HealthGain и таймер — assembly0x4A5617..0x4A5680 и helper0x4B2CE0. Флаги — editor decode0x41F510/encode0x41F4E0 и writer. Отдельный выстрел двери —0x4A6186..0x4A6469: XYZ+0x800, payload/tag assignments, ветка отказа factory на mana accounting. Helper0x4C1740 подтверждает signed проверку маны,0x4C19C0 — изменение player+0x82. Выдержки и инструкции закреплены строками/хешами в аудите; проверены80 файлов с640 Doors.

Имена некоторых поисковых файлов phase14 были предварительными. `phase14-editor-door-convert.txt` содержит header-copy0x42B050; реальный raw-converter сохранён отдельно как `phase14-editor-door-raw-convert.txt`. `phase14-editor-door-ddx.txt` — initializer0x446520; `phase14-editor-door-ddx-actual.txt` — destructor0x4463E0; актуальный DDX — `phase14-editor-door-ddx-verified.txt`. Поисковые panel/open-button/close-button относятся к другим UI-веткам и не доказывают привязки ресурсов Doors. В новой схеме эти ошибочные кандидаты не используются. Нулевые байты в карте и ненулевые в standalone сами по себе не раскрывают смысл неизвестных полей.

## Производство: игровые потребители и изолированная эмуляция

`MANUFACTURE_RUNTIME_RU.md` / `manufacture-semantics.json`: add0x5066E0 читает Doors runtime+0x95 и Traps+0x94, копирует WORD в16-байтовое задание. Смещения kind+2/ID+3 подтверждены assembly0x5068A5..0x5068C9, поскольку декомпилятор неверно восстановил stack-аргументы. get/pop0x506930/0x506970 используют head, а completed0x506B50 и percent0x506B70 читают WORD[queue+0]. Оба caller передают player+0x219:0x4C3287/0x4C329C и0x4C31B7. Прогресс word+0x217, сложение и условный reset подтверждены0x4C328D/0x4C330A.

Для14 изолированных вызовов исходных P-code helpers сохранены полные initialized registers/memory и итоговые регистры в `ghidra/phase15-emulation-*.json`:13 штатных возвратов на sentinel DEADBEEF и1 ожидаемая ошибка Divide by0 по0x506B93. Контрольные результаты133%/33% при head1 и разных first/head требованиях подтверждают выбор физического slot0. Эти вызовы не запускали игровой процесс. Снимки перепроверяются Python-аудитом; это не тест всей мастерской.

Creatures raw+0xF0A→mid+0x12FA→runtime+0x6BD подтверждены converter и loader0x4938A4..0x4938CF; contribution0x47B190 читает unsigned скорость и signed level modifier, применяет отдельную ветку для уровня1. Stat setter0x494A70/case18 пишет runtime напрямую. Неизвестный Doors+0x236 не расшифрован: кандидат0x49B9F0 является сериализацией трёх слов координат CObject и отклонён. Новые функции0x470110/0x47B190/0x49B9F0 созданы по входам из локального sgmap в исследовательском Ghidra-проекте; исходные EXE сохранены.

## Objects: Mana Gen и числовая панель

`OBJECT_PROPERTIES_RU.md` / `object-property-semantics.json`: полный поиск `.text` через `find_full_text_memory_candidates.py` дал подтверждённый UI-reader0x45A537 для model+0x3BE. Fill0x45A400, DDX0x4597B0, commit0x45A870 и writer0x4A9390 связывают raw+0x34C с edit1318/Mana Gen. Signed MOVSX и signed DDX-поле устанавливают представление GUI, но игровой эффект runtime+0x106 остаётся неизвестным. Constructor0x459270 запрашивает диалог256; шаблон327 содержит те же ID, но иные visibility/disabled биты. Схема сохраняет оба, не утверждая активность варианта.

Пять unsigned DWORD fixed-point представлений и11 scalar bindings закреплены zero-extended FILD и отдельными DDX LEA/control последовательностями. Speed0x49DE10..0x49DE37 сужается после DIV и проверяет AX, а не полный quotient. Object flags decode0x420E80/encode0x420D20 проверены четырьмя изолированными native-эмуляциями через цепочку возвратов; исходный decoder возвращает на encoder, encoder — на sentinel DEADBEEF. Полные параметры и результаты в `phase16-emulation-object-flags-*.json`. Поисковый файл `phase16-object-flags-encode.txt` содержит getter дополнительных ресурсов0x420D00 и исключён как evidence; правильный encoder — `phase16-object-flags-encode-verified.txt`.

Fill/commit панели выбирает первый из7 object-type битов и очищает остальные только при commit; это отделено от простой реконструкции25 флагов. Проверены80 файлов с11466 Objects и3224 Terrain. В77 файлах существующий Dungeon Heart/Health34464 превышает диапазон GUI32000; значения сохранены. Для Terrain неизвестным полям новые назначения не присвоены. Полные displacement-кандидаты содержат ложные старты внутри WORD-префиксов, поэтому не используются как доказательство назначения без проверки функции и типа объекта.
Дополнение 2026-11-01: точная повторная сверка NovFR/DK2-Map-Editor, master `122d2ee3ba65c87f9551cb13bd4ba1872b6edcba`. Все982 blob Source/README локального `D:\Games\DK2\Tools\DK2-Map-Editor-master` и7 Binaries/Globals совпали с GitHub tree. Ответ API, происхождение и SHA сохранены в `source-references/novfr-122d2ee3/`; обзор `NOVFR_SOURCE_REVIEW_RU.md`, машинная сводка `novfr-source-review.json`, аудит `novfr-source-review-validation.json`. Это исходники стороннего редактора, не Bullfrog/движка; новые игровые назначения из одних названий не вводятся.

Дополнение2026-11-01: Quuz/Dk2maptool, точныйcommit `d38fd0dc333d70ef4edc5412d7ffc98179fee6a4`, подтверждён Git ls-remote икомментариемZIP. Источник и9текстовыхфайлов: `source-references/dk2maptool-d38fd0dc333d/`, SHA/происхождение в `provenance.json`. Обзор `DK2MAPTOOL_SOURCE_REVIEW_RU.md`, проверяемаясводка `dk2maptool-source-review.json`. Основнойпарсер описателя/сетки,14заглушекдругихсемейств; новых назначений неизвестных игровыхполей0. Источник/готовыйEXE не исполнялись, игровые схемы/файлы и параллельный редактор не менялись.


Дополнение2026-11-02,этап165: оригинальныеPro42ED90/49F5E0/4250D0/4A3D30/4A7690,94дополнительных/34унаследованныхдиапазона и3таблицы сверены сPE/Ghidra.13native и5полныхGhidra подтверждаютTerrainStat19→raw224; новоеигровоеназначение неустановлено. Схема `terrain-text-stat-semantics.json`, описание `TERRAIN_TEXT_STAT_RU.md`, снимок текущих420источников/7EXE и4историческихрасхождений карт — `phase165-source-baseline.json`; старыеSHA сохранены.

Дополнение2026-11-02,этап166: оригинальные47FFB0/49EC90 иточнаярегистрация47D6E4/47D6F1;7дополнительных/4унаследованныхдиапазона,2блока,1261PC. Независимое восстановление248double/193ID,16native/4Ghidra/8точныхправокGlobalVariables. `editor-variable-loading-semantics.json`, `EDITOR_VARIABLE_LOADING_RU.md`, `phase166-validation.json`. Новыхигровыхимёнopaque-полей0;оригинальныефайлынеизменены.
