"""Publish hostile Jack/Trap firing evidence and explicit earlier AI boundaries."""
from pathlib import Path
from copy import deepcopy
import json
import dk2_formats as fmt
import emulate_jack_in_box as native
import crosscheck_jack_in_box as cross
import prepare_jack_primary as primary
import validate_jack_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='jack_in_box_semantics'
SCHEMA='jack-in-box-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase170-jack-emulations.json');assert len(manifest['replays'])==24
    record_refs=[];traces=[]
    source_files={k:ROOT.parent.parent/('Dungeon Keeper 2/Data/editor/'+k+'.kwd') for k in ['Creatures','KeeperSpells','Traps','Shots']}
    originals={}
    for family,ID,offset,signature in [('Creatures',46,0xF4B,170),('KeeperSpells',4,0xEE,150),('Traps',16,0x20C,130),('Shots',64,0xBE,260)]:
        file=source_files[family]
        raw=next(r for s in fmt.parse(file.read_bytes()) if s.signature==signature for r in s.records if r[offset]==ID)
        originals[family]=raw;record_refs.append(dict(family=family,ID=ID,file=str(file),file_sha256=sha(file.read_bytes()),record_bytes=len(raw),raw_sha256=sha(raw)))
    unchanged_shots=unchanged_traps=0;fired=[];positive_wind=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);post,out=native.verify(c,args,checks,saved['result'])
        assert c==row['case'] and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        assert checks[native.p.JRAW]==originals['Creatures'] and checks[native.p.KRAW]==originals['KeeperSpells']
        unchanged_shots+=int(checks[native.p.p.base.RAW]==originals['Shots'])
        unchanged_traps+=int(checks[native.p.TRAW]==originals['Traps'])
        traces.append(saved['result'])
        if c.get('trap_fire'):
            address=native.p.shot_node(c)+0x141
            fired.append(dict(case=c['name'],instance_Data2_writes=[x for x in saved['result']['observed_writes'] if int(x['address'],16)==address],
                physical_ShotData2=int.from_bytes(checks[native.p.p.base.RAW][0x9C:0xA0],'little'),
                physical_TrapData2=int.from_bytes(checks[native.p.TRAW][0x1E4:0x1E8],'little'),final_instance_Data2=out['Data2']))
        if c.get('process')==17:
            positive_wind.append(dict(case=c['name'],AI_initialized=c.get('ai_tables',True),
                state_after={hex(a):int.from_bytes(post[a][0x25E:0x262],'little') for a in [native.p.p.base.cf.ACTOR]+native.p.p.area.TARGETS[:2]}))
    controls=load('phase170-jack-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase170-jack-primary.json')
    serial=storage.validate();assert serial==load('phase170-jack-storage.json')
    baseline=load('phase165-source-baseline.json')
    for row in baseline['source_files']+baseline['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=load('phase170-prior-package-state.json')
    for row in previous['components'].values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    visit=lambda address:sum(x['visited_instructions'].get(hex(address),0) for x in traces)
    counts=dict(native_scenes=24,full_Shot_ticks=visit(0x4AB180),original_Jack_callbacks=visit(0x4B02E0),original_Trap_firing_wrappers=visit(0x4A7FD0),
        original_Creature46_factories=visit(0x4B78D0),successful_Creature46_initializations=visit(0x4872A0),
        unchanged_rawCreature46_inputs=24,unchanged_rawKeeper4_inputs=24,unchanged_rawShot64_inputs=unchanged_shots,unchanged_rawTrap16_inputs=unchanged_traps,
        independent_Ghidra_controls=8,Ghidra_compared_regions=sum(x['checked_regions'] for x in controls),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        original_vtables=4,exact_container_edits=328,Shot_source_files=82,Trap_source_files=82,Shot_records=5551,Trap_records=1293,
        preserved_prior_components=147,preserved_source_files=420,preserved_EXE_paths=7,new_opaque_field_names=0,source_writes=0)
    return dict(phase=170,date='2026-11-02',families=['Shots','Traps','Creatures'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,next='Seven remaining mixedpairs and finalevent/parameter-boundary summary',next_block='Crypt groups'),
        physical_sources=record_refs,
        firing=dict(wrapper=0x4A7FD0,common_shooter=0x4A80E0,
            pipeline='OriginalTrap16definitionloader/factory/activation -> targetedfiring4A7FD0 -> Shot64factory -> firstcompleteShot4AB180 -> process39 -> actualCreature46constructor/freeslotfactory/fullinitializer.',
            parameters='RawShot98/9C -> instance13D/141 defaults; then rawTrap1E0/1E4 -> loadedTrapdefinition7C/80 -> instance13D/141 at4A8235/4A823B. FinallyTrapID16 targetfire overwritesinstanceData2withtargettag at4A804C.',
            natural_target='Targettag2and3 areactualregisteredCreaturetargetsselectedbytheoriginalTrapwrapper,not a hostwrite to ShotData2/targettag.',
            shared_hazard='Process39 readsData2low16asentitytag; ordinaryDamageType2 readsfullDWORDasradius. Nativefiring thereforepassesthechosenentitytagtoordinaryarearadius,not just rawShot/Trapdefault2.',
            selected_result='WithwholeoriginalTrap16andShot64,enemy2/tag2 receives10000process+10000ordinary; nearbyenemyreceives8750ordinary. Changingchosenentitytagto3givessameprocessdamagebutnearbyordinary9166.',
            overrides_control='DiagnosticShotdefaults123/17,TrapData1=80,TrapData2=2 becomeinstance80/2; separateTrapData2=1isoverwrittenbytargettag2. Originalsourcebytesnevermodified.',
            traces=fired,
            scope='Wholefiringwrapperacceptsselectedvalidtarget; naturalTrapacquisition/scheduler/triggerconditionsnotexecuted. Smalltags2/3andprepared32x32map; no universaloutcomeclaimforlarge tag-as-radius/overflow.'),
        hostile_process=dict(process=39,callback=0x4B02E0,
            target='Worldcoordinateboundscheck theninstanceData2low16 -> entityregistry -> targetownerpointer. OriginalPlayerrelationhelper4BF390 mustreportenemy.',
            damage='Original4B2CE0 receives32-bitnegatedData1; thenvictimWORD28 isrememberedinBX. NoCreature445invulnerabilitycheckintheselectedprocessbranch. Ordinarymode2stillhasitsown445gate.',
            create='AfterHPrequest, originalWorldfree-slotCreaturefactory4B78D0 requests species46 ownedbyShotowner7 atShotposition. ConstructorregistersrealunusedCreature,actualfactorytakesitandinitializerloadsrawJackbaseHP240.',
            success='Onlyafterfactoryreturnsnonzero: findkind3Trapincellvia453670 andcallitsoriginalvslot20/4B2AD0(HP0),clearflagsvia48DFA0/48DF10,setstate273via48AD30/476D30,andstorevictimownerWORDinnewActor2F5.',
            owner='NewCreatureowner28andPlayerpointer2A remainShotowner7; victimowner8isaseparateWORD2F5,not directownershiptransfer.',
            failure='Preparedactivecount448 rejectsCreaturefactoryafterdamage; no Jackinitialized,TrapHPpreserved. Count447allowslastslotandends448. Count/poolcontextexplicit,notwholeWorldcapacitytest.',
            return_contract='Process39alwaysreturns0intheexecutedbranches. Originalfirstevent64ignores0andordinaryareaexecutes; tickevent2abortsordinaryareaanddecayevenafterdamageandsuccessfulJackcreation.',
            selected_controls=dict(Data1=80,start=dict(victimHP=840,otherEnemyHP=930,newJackHP=240,newJackOwner=7,rememberedVictimOwner=8,state=273),
                tick=dict(victimHP=920,otherEnemyHP=1000),capacity448=dict(victimHP=840,otherEnemyHP=930,newJack=False),invulnerableVictim=dict(victimHP=920,otherEnemyHP=930)),
            lethal='WholeoriginalShot64/Trap16Data1=10000withpreparedtargetHP1000executesprocessandordinaryrequests; HPmayremainnegativeinthisboundedtick. NoactualCreaturedeathtick/body/deallocationcomposed. AdditionalpreparedHP100000controlavoidslethality.',
            trap_removal_boundary='4B2AD0onlysetsHP0; registrytagandallocationremainpresentatend. Thisisarequest/mark,notprooffullremoval,manaupkeeprefundorpoolfree.'),
        AI_context_correction=dict(required='Original47BBE0states,4798F0categoriesand4950C0animationinitializersareexecuted. CopiedtargetActorctx256/25AreboundtoselfandWorld; newlycreatedActorctxsetbyoriginal476B40.',
            positive='Withtablesandvalidcontexts,Wind48AD30(210)reallychangescurrentstate25Efrom1to210; Jack48AD30(273)changes1to273.',
            negative='Matchednotablescontrolsretainstate1,thoughHP/rotationandcreationcanalreadyoccur. Thisexplainsphase169boundedno-immediate-state observation; itisnotageneralclaimthatWindstatechangeisalwaysdeferred.',
            tested=positive_wind,
            limitation='No subsequentCreaturetickexecutingstate210/273callbacks; reactionsafterentryandanimations/livebehaviornotclosed.'),
        fixture=dict(prepared='TwoorderedloadedCreaturedefinitions(actor8andJack46),Keeper4,realCreaturefreeslotandtagallocator;actualTrap/Shotarenas,World/Player/map/alertlists,targetsandnullresourceproviders.',
            navigation='FullTrapfiringalsorunsoriginalNAVbuffer/grid/meshinitializers4CE690/4CE740/4C8F30/4CEF20 andCMapWorldbackpointer; originalreaction45AA20 executeswithoutlowpointerfallback.',
            adapters='Explicitexternalresource/soundprovidersandrangecheckedIsBadReadPtrIATprovider; gameplaycode/vtablesunchanged. Importprobezeroinvalidreadsverified.',
            excluded='Earlierdiscoverymissingfree-listhead,prelevel,AI tables,NAVorCMapbackpointerisnotacceptedchainproof. Accepted24scenesrejectallnon-FS lowpagereads.',
            source_changes='None;copiesinCPUonly. PreparedCreaturemax/currentHPcontrolsarenotclaimsaboutnormallegalHPvalues.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=serial,preservation=baseline,
        block_remaining=dict(mixed_pairs_without_selected_scenes=['P7/D7','P9/D7','P28/D7','P29/D1','P29/D7','P34/D2','P34/D7'],
            still_open='Normalfiringtargetacquisition,subsequentJack273executionandvariedlargeData2tag-as-radiusareseparateunexecutedboundaries; do notlabelwholegamecomplete.'),
        helpers=[link(n) for n in ['probe_jack_in_box.py','emulate_jack_in_box.py','crosscheck_jack_in_box.py','prepare_jack_primary.py','validate_jack_storage.py','create_jack_helpers.py']],
        limitations=['ExactprovidedDXSHA; noautomaticRC3/oldEXE/patchDLLtransfer.',
            'DirectcompletecallbackswithpreparedWorld/pools/targets; realTrapwrapperruns,butnaturalWorldscheduler/targetacquisitionnotrun.',
            'Firsttickandselectedevents; no subsequentCreature273/210behavior,naturalflightorcompletepoolfree/Traprefund.',
            'Tags2/3 testedassharednormalAoEradius; largeentitytagsmayoverflowgeometryandarenotpredicted.',
            'Keeper4andJack46rawinputsunchanged; otherdiagnosticShot/Trapparametereditsareexplicit. SomesourceShot64scenesuselargepreparedHPtoisolatecreationfromlethality.',
            'Frozenphase169no-statefixturepreserved; newerAItables/contextproofhasprecedenceforpositivevalidstateentry.',
            '147priorcomponentsandcodecunchanged;420currentbaseline sources/7EXEverifiedwith4preexistingmapdifferencesretained. Priorityblock1andfullgoalremainopen.'])

def documents(s):
    n=s['counts']
    text='''# JackInTheBox: выстрел ловушки, цель и создание Jack

Этап 170, 2 ноября 2026. Первый блок ProcessType/DamageType продолжается. Проверен предоставленный DKII-DX.exe; машинная схема `jack-in-box-semantics.json` содержит SHA, исходные записи, точные адреса и результаты.

## Полная выполненная цепочка

Неизменённые исходные Traps16, Shots64, Creatures46 и KeeperSpells4 загружаются оригинальными функциями. В сценах выстрела создаётся и активируется настоящая ловушка, затем целая `4A7FD0` стреляет по выбранному живому Creature tag. Она вызывает `4A80E0`, настоящую Shot-фабрику и сохраняет цель. После этого выполняется целый первый Shot tick, Process39, оригинальный конструктор свободной ячейки Creature и настоящая фабрика/инициализация Jack46. Следующий tick самого Jack здесь не исполнялся.

Цель для firing-wrapper выбрана явно. Обычный планировщик, который обнаруживает жертву и решает активировать ловушку, не запускался. NAV-структуры и AI-таблицы инициализированы оригинальным кодом; игровые функции и vtable не подменены.

## Почему Data2 нельзя считать только радиусом

Выполнена такая последовательность исходных записей и операций:

1. Shots raw98/9C загружаются как Data1/Data2 определения и экземпляра.
2. Оригинальная стрельба копирует `Traps.ShotData1/ShotData2`, raw1E0/1E4 → definition7C/80 → Shot13D/141. Записи по `4A8235/4A823B` наблюдаются непосредственно.
3. Для Trap ID16 targeted-wrapper по `4A804C` **заменяет Shot Data2 на tag выбранной цели**.
4. Process39 читает low16 Data2 как tag живой сущности. Обычный DamageType2 после него читает полный DWORD как радиус области.

В исходных Shot64 и Trap16 Data2=2. Полный выстрел по tag3 сделал значение экземпляра3, не меняя исходные файлы. При одинаковом Data1=10000 запрос обычного урона второй цели на расстоянии1024 Q12 изменился с8750 на9166. Это следствие совместного использования поля в данном EXE, а не отдельное скрытое поле радиуса.

Отдельные диагностические контроли: Shot defaults123/17 и Trap80/2 дали экземпляр80/2; Trap Data2=1 всё равно заменилось на target tag2. Редактор должен показывать значения файлов и учитывать такую игровую перезапись экземпляра. Сценарии с большими tag/переполнением радиуса ещё не предсказываются.

## Что делает вражеская ветка

После проверки позиции и отношения владельцев Process39 вызывает `4B2CE0(-Data1)` для указанной цели. Затем просит фабрику создать Creature46, `Jack`, в позиции снаряда и с владельцем снаряда. Исходная запись Jack даёт базовый HP240.

Только после успешной фабрики код ищет ловушку kind3 в клетке и вызывает её vslot20. Настоящая `4B2AD0` обнуляет HP ловушки. Далее код ставит новому Jack состояние273 и записывает владельца жертвы в WORD `+0x2F5`.

**Владелец нового Jack остаётся владельцем снаряда.** В проверенной сцене это7, а отдельное поле2F5 получает8 — владельца жертвы. Поле2F5 здесь не означает немедленную передачу Jack другому игроку.

| Подготовленные условия, Data1=80 | HP жертвы | HP второй цели | Создание Jack |
|---|---:|---:|---|
| Process39 на первом event64, DamageType2 | 840 | 930 | HP240, owner7, state273, 2F5=8 |
| Тот же процесс на tick event2 | 920 | 1000 | Создаётся; нулевой возврат прекращает обычную область |
| Число активных Creature448 | 840 | 930 | Отказ фабрики; HP ловушки сохраняется |
| Число активных Creature447 | 840 | 930 | Создаётся последний; счётчик становится448 |
| Таймер неуязвимости жертвы445=1 | 920 | 930 | Process39 наносит80; обычная область эту цель пропускает |
| Союзная цель | 1000 | 930 | Вражеская ветка пропущена, Jack не создаётся |

Повреждение Process39 стоит до фабрики, поэтому заполненный пул не отменяет его. Успешная фабрика требуется для обнуления HP ловушки. Обнуление HP не освобождает её память: tag и allocation ещё присутствуют в конце сцены. Возврат содержания маны, окончательное удаление и World pool free этим не доказаны.

Исходный Data1=10000 также исполнен без правки Shot64/Trap16. В сцене с начальным HP1000 после двух запросов HP жертвы остаётся отрицательным; Creature death tick/тело/освобождение отдельно не запускались. Дополнительный контроль с явно подготовленным HP100000 даёт80000/91250 и проверяет ту же цепочку без летального исхода.

## Уточнение предыдущего результата про состояния

Ранее в этапе169 Wind вызывал `48AD30(210)`, но текущий state оставался1 в его ограниченном стенде. Теперь выполнены оригинальные initializer состояний, категорий и анимаций; контексты скопированных целей привязаны к правильным Actor/World.

С этими условиями Wind действительно меняет текущий DWORD25E на210, а созданный Jack — на273. Парные контроли без AI-таблиц сохраняют state1. Поэтому прежнее наблюдение не следует обобщать до правила «смена состояния всегда откладывается». Исторические результаты сохранены; новая схема имеет приоритет для проверенной положительной ветки.

Последующий Creature tick, выполняющий поведение состояний210/273, и естественная анимация здесь по-прежнему не проверены.

## Проверки и сохранность

COUNTS

328 точных пересборок в памяти на82 Shots и82 Traps проверяют общие параметры, обратное восстановление и decode→rebuild; остальные и неизвестные байты сохранены. SHA420 текущих исходников и7 путей EXE совпали с прежним baseline. Четыре более ранних изменения карт остаются явно записанными в его истории. Пакет148 компонентов, предыдущие147 компонентов, codec и физические разметки не изменены.

Следующие семь смешанных пар внутри первого блока:7/7,9/7,28/7,29/1,29/7,34/2,34/7. Затем нужна итоговая сводка границ событий, параметров и целевых классов. К Crypt пока не переходим. Первый блок и общая цель остаются открытыми.
'''
    paragraph=(f'Приняты24 native-сцены, {n["original_Trap_firing_wrappers"]} полных firing-wrapper ловушки, {n["original_Creature46_factories"]} вызовов Creature-фабрики и '
        f'{n["successful_Creature46_initializations"]} успешных инициализаций. Все24 используют неизменённые rawJack46 и rawKeeper4; '
        f'{n["unchanged_rawShot64_inputs"]} используют неизменённый rawShot64, {n["unchanged_rawTrap16_inputs"]} — неизменённый rawTrap16. '
        f'Восемь независимых Ghidra CPU-контролей сравнили {n["Ghidra_compared_regions"]} областей / {n["Ghidra_compared_bytes"]} байт. '
        f'Первичные байты покрывают все {n["unique_executed_original_PCs"]} исполненных адресов: {n["additional_function_extents"]} дополнительных и '
        f'{n["inherited_function_extents"]} унаследованных диапазонов плюс четыре vtable. Полные диапазоны не означают все ветви. '
        'Проверяются HP и их запросы, порядок событий, Data2-перезаписи, владельцы/tag/счётчики, state273/210, guard-байты и сохранность исходных записей. '
        'Все не-FS чтения нулевой страницы и недопустимые обращения OS read-probe отвергаются. Ранние неполные пробы исключены, а их остановки не объявляются ошибкой игры.')
    (ROOT/'JACK_IN_THE_BOX_RU.md').write_text(text.replace('COUNTS',paragraph),encoding='utf-8')
    note=('Актуальный этап170: пакет148 компонентов. Настоящие Trap16→targeted-fire→Shot64→process39→Creature46 factory/initializer: '
        'Data2 послеTrapdefaults заменяется targettag2/3, обычныйDamage2 читает его какрадиус; Jackowner7 сохраняет victimowner8 в2F5. '
        'Урон доfactory, count448 отказываетбезотменыурона/безобнуленияTrapHP; успешное созданиеHP240/state273 затемобнуляетTrapHP. '
        'ПоложительныйWindstate210 подтверждёнсAIтаблицами/валиднымконтекстом; старыеограниченныефикстурысохранены. '
        f'24native/8Ghidra,{n["Ghidra_compared_regions"]}областей/{n["Ghidra_compared_bytes"]}байт;12дополнительных/241унаследованныйдиапазон,18458PC;328точныхпересборок. '
        'Прежние147компонентов/codec/420источников/7EXE сохранены. Остались7смешанныхпарвпервомблоке; цельактивна. См.`JACK_IN_THE_BOX_RU.md`.\n\n')
    f=ROOT/'INTEGRATION_RU.md';txt=f.read_text(encoding='utf-8')
    if 'Актуальный этап170:' not in txt[:1400]:
        first,rest=txt.split('\n\n',1);f.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 170'):
        _,rest=txt.split('\n',1);f.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 170\n\n'+note+'Общая экспертная оценка около75%; первый блок не завершён.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase170-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==147
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==147 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['jack_extension']=dict(source=link(SCHEMA),block_complete=False,
        rule='OriginalTrap16targetfirecopiesTrapData1/2thenoverwritesData2withtargettag; process39useslow16tagandDamage2fullradius. HostiledamagebeforeCreature46factory; successfulcreationthenTrapHP0,state273,victimowner2F5.',
        AI_correction='WithoriginalAItablesandvalidActor/Worldctx,Windcurrentstatebecomes210; historicalphase169no-statefindingisfixture-bound.',remaining_mixed_pairs=7)
    package['effective_shot_parameters']=effective
    note='jack_in_box_semantics phase170:24originalhostile/firing/AIscenes,8wholeGhidra;Trap16fire→Shot64Data2overwrittenwithtargettag2/3→process39damage→Creature46factory/state273/owner7/victimowner2F5; Damage2thenusestag-as-radius. Count448rejectscreationafterdamage,TrapHP0onlyafterfactorysuccess. Windstate210requiresinitializedAI/validctx;positivepathnowproven. Prior147components/codec/420sources/7EXEpreserved;328exactedits;priorityblock1openwith7mixedpairsremaining.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase170-search-status.json',dict(phase=170,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Publishedphase169area-pairevidence;phase170discoverychangednextactiontoactualTrapfiringandAIcontextcorrection.'),
        counts=s['counts'],remaining=s['block_remaining'],priority_order=['ProcessType/DamageType','Crypt groups','Global parameters','Creature resistances/flags','Other unknowns'],
        readiness='Expert estimate staysabout75%; notmeasuredbytecoverage.',evidence=[link(SCHEMA),link('JACK_IN_THE_BOX_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
