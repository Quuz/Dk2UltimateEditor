"""Publish verified area pairs while retaining the user-ordered open block."""
from pathlib import Path
from copy import deepcopy
import json
import dk2_formats as fmt
import emulate_shot_area_combinations as native
import crosscheck_shot_area_combinations as cross
import prepare_shot_area_combinations_primary as primary
import validate_shot_area_combinations as storage
import create_shot_pair_observer as observer
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_area_combination_semantics'
SCHEMA='shot-area-combination-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    assert (ROOT/'native_shot_pair_trace.py').read_text(encoding='utf-8')==observer.expected()
    manifest=load('phase169-area-combinations-emulations.json');assert len(manifest['replays'])==40
    source_records=[];traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert c==row['case'] and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        traces.append(saved['result'])
        if c.get('source_record'):
            q=c['source_record'];b=next(s.records[q['record_index']] for s in fmt.parse(Path(q['file']).read_bytes()) if s.signature==0x104)
            assert b==checks[native.p.p.base.RAW]
            source_records.append(dict(case=c['name'],file=q['file'],file_sha256=sha(Path(q['file']).read_bytes()),record_index=q['record_index'],
                ShotID=b[0xBE],raw_record_sha256=sha(b),ProcessType=b[0xC7],DamageType=b[0xC5],unchanged_raw_record=True))
    assert len(source_records)==7
    controls=load('phase169-area-combinations-crosschecks.json')['replays']
    assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase169-area-combinations-primary.json')
    serial=storage.validate();assert serial==load('phase169-area-combinations-storage.json')
    baseline=load('phase165-source-baseline.json')
    for row in baseline['source_files']+baseline['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    old=load('phase169-prior-package-state.json')
    for row in old['components'].values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    prior_pairs={(r['case']['process'],r['case']['mode']) for r in load('shot-combination-semantics.json')['native_executions']['replays']}
    pairs=prior_pairs|{(c['process'],c['mode']) for c in native.cases()}
    missing=[x for x in serial['mixed_pairs'] if (x['ProcessType'],x['DamageType']) not in pairs];assert len(missing)==7
    counts=dict(native_scenes=40,full_Shot_ticks=sum(r['visited_instructions'].get('0x4ab180',0) for r in traces),
        original_process_dispatches=sum(len(r['process_events']) for r in traces),unchanged_source_record_scenes=7,
        independent_Ghidra_controls=9,Ghidra_compared_regions=sum(x['checked_regions'] for x in controls),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        primary_vtables=4,exact_container_edits=328,Shot_source_files=82,Shot_source_records=5551,observed_pairs=49,observed_mixed_pairs=14,
        mixed_pairs_with_some_selected_scenes=7,mixed_pairs_without_selected_scenes=7,preserved_prior_components=146,
        preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    return dict(phase=169,date='2026-11-02',family='Shots ProcessType/DamageType area composition',profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,next_block='Crypt groups',global_phase167='Unpublished leads parked by explicit user priority.'),
        event_contract=dict(root=0x4AB180,start_event=64,start_call=0x4AB281,start_return='Ignored; first-tick149guard written1 and processing continues.',
            tick_event=2,tick_call=0x4AB30F,tick_return='Exactzero returns immediately beforecollision,ordinaryarea,decayandpositional-audio updates.',
            normal_call=0x4AB329,additional_events='Selectedscenealsoevent8 afterordinaryarea; sourceInfernoHealth1 invokes128 onexpiry. No Worldterminal/poolfree composed.',
            jack_control='ValidfriendlyCreaturetag2; process39 returns0. Mask64 stillallowsmode2 tohitotherenemy,HP930; mask2 or66 abortsordinaryarea and keepsShotHP32. Repeatedmask2 tick reachesage2/HP32.',
            scope='Friendlytarget branch only; hostile Jack damage/createCreature46 path remains unexecuted andopen.'),
        inferno=dict(process=30,callback=0x4AF660,tested_damage_modes=[0,1,2],
            ordering='Process30 damage completesbeforeordinaryDamageType1/2. Data1 magnitude isusedinboth; eachcanmakeanHPrequest.',
            selected_HP=dict(Data1=80,Data2=2,normal_mode2=dict(friendly=920,enemies=[840,850]),normal_mode1=dict(friendly=920,enemies=[840,840])),
            filters='SelectedInferno loop hasno owner orCreature445timer exclusion. NavGridbit3 blocksitscell; Creatureprimaryflags20excludesunless2000set. Ordinary1/2usesitsownowner/timergatesanddoesnotsharetheseNavGrid/flagfilters.',
            geometry='SelectedsmallpositiveR: inclusiveclippedcellbox,strictdistance-squaretest. Radius-squarethresholdhalvedwhenShotage12D==1; thisisR/sqrt2,notR/2. LaterageusesfullR-square.',
            native_targets='PreparedthreeCreatures: friendlytag100 andtwotargets101/103; owner7 versus8, positionsandflagsstatedpercase. No alltargetclasses/mapboundaries/overflowclaim.',
            observed_source_pairs=['P30/D1 inprovidedmodmaprecord49','P30/D2 firstevent64record97 andtickevent2record108'],
            side_effects='Original48EBD0 and517480 execute; broadersemanticmeaning oftheirqueues/statistics not newlydecodedhere.'),
        wind=dict(process=17,callback=0x4AE990,tested_damage_modes=[0,2],
            shared_Data1='Processradiusinwholecells; ordinarymode2 damage magnitude.',shared_Data2='Processpercentage; ordinarymode2 radiusinwholecells.',
            bounded_formula='ForaneligibleenemyatdistanceD,R=Data1: trunc(trunc(rawCreatureMaxHP*(D+4096R)/(8192R))*Data2/100), negated. Selectednonoverflowintegerinputs only; no levelcoefficientreadinthiscallback.',
            selected_control=dict(Data1=2,Data2=2,raw_max_HP=2500,process_damage=[25,28],ordinary_damage=[2,1],enemy_final_HP=[973,971]),
            friendly='Selectedfriendlytargetisrotatedandinstanceflag08000000set,butprocessHPdamageskipped. Four-bytecurrentstate25Eremains1inacceptedscenes; original48AD30(210)requestisnotanimmediatecurrentstateswitch.',
            rotation='ForselectedpositiveR, angle=(old+floor(R*600000hex/(D+4096R)))&7FF. Validatedforfriendlyandbothenemies,andtwoticks.',
            source_zero='WholeprovidedTerrorCycloneDamageShot84 hasData1=0/Data2=2. SelectedtickexecutesbothhandlerswithnoHPrequests; thisdoesnotprovebroaderability/castcreatesnootherdamage.'),
        gas=dict(process=31,callback=0x4AF8E0,tested_damage_modes=[0,1,2],
            branch='At4AF90E/4AF913 nonzeroDamageType skipstheownCreature/RNGwalk. Callbackstillcallsoriginal4B32A0 firstandreturns1.',
            selected_counts='Gas+Damage2 doesnoownHPrequest; twoordinaryrequests. Gas+Damage0 makesnoHPrequestbutexecutes6original5558D0callsinthree-Creaturescene. Randomsideeffects arecomparedaswholememory,notfullyrenamed.',
            source='WholeprovidedmainShots3/55/58 allData1=50/Data2=2: selectedenemyHP950/957 fromordinarymode2. Notapoison38application; no45D/45Fpoisonstatusclaim.'),
        source_records=source_records,fixture=dict(prepared='World/map/threeCreatures,pools,tagtables,emptyvalidPlayeralertlists andseparate32x32NavGrid. OriginalrawCreature/Shotloaders,factory,processinitializerandwholeShotupdates execute.',
            source_record_scenes='Sevenwhole239-byteprovidedShotrecords,unchangedbeforeconverter; converters/MID375/definitionchecksumandrawretentionchecked.',
            adapters='Frozenphase148externalresource/audioIO plusobservation-onlyphase169engine; no originalinstruction/vtable replacement.',
            excluded_trials='EarlydiscoverywithoutvalidNavGrid/alertlistorwithunboundJacktag2isnotaccepted. Corrected40scenesrejectallnon-FS lowpage reads; earlyoutputisnotgamebehavior evidence.',
            scheduler='No naturalWorldscheduler,flight,terminalcallback/poolfree,orCreaturetickafterWindrequest. Preparednullresourceprovidersdonotrepresentrealgraphics/audio.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=serial,preservation=baseline,
        observer=dict(original=link('native_aoe_trace.py'),derivative=link('native_shot_pair_trace.py'),generator=link('create_shot_pair_observer.py'),policy='Onlyreads/logsofprocesseventargumentsandactualEAXreturns; CPUexecution/state logicunchanged.'),
        block_remaining=dict(mixed_pairs_without_selected_scenes=missing,
            immediate_next='HostileJackInTheBoxbranch: validtargettag,HPdamage,originalCreature46creationandownerassignment,theninteractionwithordinaryarea.',
            additional=['P7/D7','P9/D7','P28/D7','P29/D1','P29/D7','P34/D2','P34/D7'],
            still_required='Selectedscenesarenotclosureforallsevenvisitedmixedpairs: remainingevents/overrides/otherclasses/applicableversionboundariesandfinalblocksummaryremain.'),
        helpers=[link(n) for n in ['probe_shot_area_combinations.py','emulate_shot_area_combinations.py','crosscheck_shot_area_combinations.py','prepare_shot_area_combinations_primary.py',
            'validate_shot_area_combinations.py','create_shot_area_helpers.py','create_shot_pair_observer.py','native_shot_pair_trace.py']],
        limitations=['ExactprovidedDXSHAonly. Sourcecount49/14 includesprovidedmodmaps; no certifiedshipped-corpusclaim.',
            'All40casespreparedanddirectCPUinvoked;sevenwholeSourceShotrecordsareadditionalraw-inputfidelity,notafullmapload/livecast.',
            'HostileJackbranchandCreature46creationnotexecuted; no completeJackmechanicclaim.',
            'Windcurrentstateunchanged,orientation/flagupdatesverified. NaturalAIresponseafterrequestnotcomposed.',
            'Damage2sharedfalloffaccumulatorfromphase131retained; no intended-independent-targetfixapplied.',
            'SmallpositiveR/scalars; no fulloverflow/negativegeometry/allmapedge/alltargetclasscoverage.',
            '146priorcomponentsandcodecunchanged;sourcebaselinepreservedincluding4explicitpreexistingmapdifferences. Priorityblock1andfull15familygoalremainopen.'])

def documents(s):
    count=s['counts'];sources=s['source_records']
    lines=['# ProcessType / DamageType: площадные сочетания','',
        'Этап 169, 2 ноября 2026. Первый приоритетный блок продолжается. Проверен предоставленный DKII-DX.exe, SHA-256 `'+s['profile']['sha256']+'`.','',
        '## Порядок событий и возврат процесса','',
        'В полном Shot tick первый event=`0x40` вызывается один раз. Его нулевой возврат игнорируется: guard149 получает1, выполнение продолжается. Event=`0x2` вызывается перед столкновением и обычным DamageType; его нулевой возврат немедленно прекращает эту часть tick. До обычного урона, уменьшения HP снаряда и обновления положения звуковых дескрипторов выполнение тогда не доходит.','',
        'JackInTheBox возвращает0. В контрольной сцене Data2=2 обозначает существующее существо с tag2, принадлежащее владельцу снаряда. При mask64 процесс не наносит ему урон, а обычный DamageType2 затем снижает HP другой вражеской цели до930. При mask2 или66 обычная область вообще не вызывается, HP снаряда остаётся32. Два прямых tick при mask2 дают age2/HP32. Это проверка допустимой союзной ветки и контракта событий. **Вражеская ветка с нанесением урона и созданием существа46 ещё не выполнена; JackInTheBox полностью не закрыт.**','',
        '## Inferno: два механизма урона','',
        'Process30 использует Data1 как величину повреждения, Data2 как радиус. После него обычный DamageType1/2 может использовать те же значения и ещё раз повредить цель.','',
        '| Сцена, Data1=80/Data2=2 | HP союзной цели | HP двух врагов |',
        '|---|---:|---|',
        '| Только обычный DamageType2 | 1000 | 920 /930 |',
        '| Только Inferno | 920 | 920 /920 |',
        '| Inferno + DamageType1 | 920 | 840 /840 |',
        '| Inferno + DamageType2 | 920 | 840 /850 |','',
        'Это три подготовленных существа с начальным HP1000. В этой ветке Inferno нет исключения своего владельца и таймера неуязвимости445. Его клетку исключает бит3 отдельной NavGrid; цель исключается по CreatureFlags20 без2000. Обычная область продолжает применять собственные условия владельца/таймера и эти два фильтра Inferno не наследует. Новых имён флагов здесь не назначается.','',
        'В первом обновлении Inferno уменьшает вдвое **квадрат** радиуса сравнения. Для выбранных небольших положительных значений это радиус R/√2, а не R/2. При R=2 цель на расстоянии6000 единиц Q12 в первом tick не получает урон процесса, в следующем получает; обычная область проверяется отдельно. Границы карты и переполнение больших радиусов этим не закрываются.','',
        '## Wind: одни DWORD, разные единицы','',
        'В Process17 Data1 — радиус ветра в клетках, Data2 — процент. В DamageType2 те же Data1/Data2 становятся величиной обычного урона и радиусом. Поэтому их нельзя описывать одной универсальной подписью.','',
        'При Data1=2/Data2=2 и загруженном базовом MaxHP2500 ветер запросил25 и28 повреждений у врагов на расстояниях0 и1024. Затем обычная область запросила2 и1; итог HP973/971. Ветер берёт raw базовый MaxHP из определения; в этом callback коэффициент уровня не читается. Проверенная формула для этих небольших положительных значений: `trunc(trunc(MaxHP*(D+4096R)/(8192R))*Data2/100)`.','',
        'Союзная цель меняет направление и получает instance-бит08000000 без запроса изменения HP. Направление проверено отдельно для всех трёх целей и двух tick. Вызов48AD30(210) исполнен, но DWORD текущего состояния25E остаётся1. Последующее выполнение запроса обычным планировщиком существа не запускалось. Это уточняет предварительное описание «меняет состояние»: немедленное переключение состояния не подтверждено.','',
        'Неизменённая предоставленная запись Terror Cyclone Damage, Shot84, содержит Data1=0/Data2=2. Её выбранное полное обновление не дало ни одного запроса повреждений. Это относится к данной записи и сцене; остальные снаряды способности и сам каст здесь не исследованы.','',
        '## Gas: ветка зависит от DamageType','',
        'Process31 сначала вызывает4B32A0. Затем **любой ненулевой DamageType** пропускает собственный обход существ и случайные проверки Gas. При DamageType2 повреждения в выбранных сценах исходят от обычного обработчика области. При DamageType0 обработчик Gas запросов изменения HP не делает, но в сцене с тремя существами выполняет6 вызовов оригинального генератора случайных чисел; побочные изменения сверены целиком, их новое игровое название не назначается.','',
        'Полные неизменённые записи основного Shots.kwd — Fart3, Gas Missile Cloud55 и Gas Trap Cloud58 — с50/2 дали HP950/957 у двух врагов. Это не наложение PoisonSpit38.','',
        '## Исходные записи и проверки','',
        '| Сцена | Исходный файл | Shot ID | P /D |','|---|---|---:|---|']
    for x in sources:lines.append(f'| {x["case"]} | {Path(x["file"]).name} | {x["ShotID"]} | {x["ProcessType"]} /{x["DamageType"]} |')
    lines += ['',
        f'Приняты40 native-сцен: {count["full_Shot_ticks"]} полных Shot tick, {count["original_process_dispatches"]} диспетчеризаций процессов. Семь239-байтовых исходных Shot-записей переданы конвертеру без правок. Проверены все375байт intermediate и контрольная сумма определения, HP/порядок запросов, точные события и EAX-возвраты, счётчики и таймер снаряда, исходные байты, таблица callback и направления Wind.','',
        f'Девять независимых Ghidra CPU-контролей сравнили {count["Ghidra_compared_regions"]} областей / {count["Ghidra_compared_bytes"]} байта. Все {count["unique_executed_original_PCs"]} исполненных адресов покрыты первичными инструкциями: {count["additional_function_extents"]} дополнительных и {count["inherited_function_extents"]} унаследованных диапазонов, четыре vtable. Это не исполнение всех ветвей этих функций.','',
        'Стенд имеет отдельную NavGrid и корректные пустые списки получателей оповещений. Ранние пробы без этих объектов или с отсутствующим Jack tag2 исключены: чтения нулевой страницы не принимались за рабочий игровой контекст. В40 принятых сценах разрешены только настоящие DWORD FS:[0], остальные низкие чтения отвергаются. Оригинальные инструкции/vtable не заменены; ресурсные и звуковые провайдеры объявлены отдельно.','',
        '328 точных пересборок82 контейнеров проверили четыре площадных сочетания, обратное восстановление и decode→rebuild. Неизвестные и остальные байты сохранены. Проверены420 текущих источников и7 путей EXE по прежнему baseline; четыре более ранних изменения карт остаются явно записанными. Пакет147 компонентов, прежние146 компонентов и физический codec сохранены.','',
        '## Что осталось в первом блоке','',
        'Следующая работа — полноценная вражеская ветка JackInTheBox: живой target tag, его повреждение, оригинальное создание существа46, присвоение владельца и совместный обычный урон. Затем семь смешанных пар без выбранных сцен:7/7,9/7,28/7,29/1,29/7,34/2,34/7. Уже выполненные сцены тоже не заменяют итоговых границ по событиям, переопределениям параметров, классам целей и версиям.','',
        'Порядок пользователя сохраняется: сначала завершение ProcessType/DamageType, затем Crypt, глобальные параметры, резисты и флаги, остальное. Блок и общая цель открыты. Схема — `shot-area-combination-semantics.json`; результаты — `phase169-validation.json`.']
    (ROOT/'SHOT_AREA_COMBINATIONS_RU.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    note=('Актуальный этап169: пакет147 компонентов. Полные площадные сочетания Wind/Inferno/Gas и союзная ветка JackInTheBox: '
        'process40 возврат игнорируется, process2 ноль прекращает обычный урон и decay. Inferno и обычная область наносят раздельный урон с разными фильтрами; '
        'Wind использует общие DWORD в других единицах, меняет направление/флаг, текущий state25E остаётся1. Gas при ненулевом DamageType пропускает собственный обход. '
        '40native/7неизменённыхsourceShot/9Ghidra,567областей/1234593байта;7дополнительных/128унаследованных диапазонов,6095PC;328пересборок82Shots. '
        'Прежние146компонентов/codec/420источников/7EXE сохранены. HostileJack/create46 и7смешанныхпар без сцен остаются в первом блоке. '
        'См.`SHOT_AREA_COMBINATIONS_RU.md`; блок1 и общая цель активны, оценкаоколо75%.\n\n')
    f=ROOT/'INTEGRATION_RU.md';text=f.read_text(encoding='utf-8')
    if 'Актуальный этап169:' not in text[:1400]:
        first,rest=text.split('\n\n',1);f.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';text=f.read_text(encoding='utf-8')
    if not text.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 169'):
        _,rest=text.split('\n',1);f.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 169\n\n'+note+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase169-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==146
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);assert len(old['components'])==146
    s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['area_extension']=dict(source=link(SCHEMA),block_complete=False,
        rule='CompleteShotupdates: ignoreprocessreturnatfirstevent64; abortbeforeordinaryareaonzeroevent2. Infernoandordinarydamagecanbothapply. GasownwalksuppressedbynonzeroDamageType. Windsharedparametershaveindependentunits.',
        current_selected_mixed_pairs=7,mixed_pairs_without_selected_scenes=7,hostile_Jack_pending=True)
    package['effective_shot_parameters']=effective
    note='shot_area_combination_semantics phase169:40originalfulltickscenesincluding7unchangedsourceShotrecords;9wholeGhidra567regions1234593bytes;6095PCs,7new/128inheritedextents;328exactedits. P30canapplyalongsideD1/D2withdifferentfilters,P17sharesparameterswithD2inotherunits,P31ownwalkonlyatD0. P39friendlybranchproveszeroevent2abortversusignoredzeroevent64;hostile/create46pending. Priorityblock1open;prior146components/codec/420sources/7EXEpreserved.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase169-search-status.json',dict(phase=169,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',evidence='phase168-validation.json',reason='Publishedcollisionpairevidence,independentCPUchecksandcomponent146.'),
        counts=s['counts'],remaining=s['block_remaining'],priority_order=['ProcessType/DamageType','Crypt groups','Global parameters','Creature resistances/flags','Other unknowns'],
        phase167='Unpublishedgloballeadsretained,deferredbyuserpriority.',readiness='Expert estimate staysabout75%,notmeasuredbytecoverage.',evidence=[link(SCHEMA),link('SHOT_AREA_COMBINATIONS_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
