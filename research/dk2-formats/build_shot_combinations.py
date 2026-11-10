"""Publish bounded collision-pair evidence without closing the overall block."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_shot_combinations as native
import crosscheck_shot_combinations as cross
import prepare_shot_combinations_primary as primary
import validate_shot_combinations as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_combination_semantics'
SCHEMA='shot-combination-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
canonical=lambda d:sha(json.dumps(d,ensure_ascii=False,sort_keys=True).encode())
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    manifest=load('phase168-shot-combinations-emulations.json');assert len(manifest['replays'])==24
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
    controls=load('phase168-shot-combinations-crosschecks.json')['replays']
    assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase168-shot-combinations-primary.json')
    serial=storage.validate();assert serial==load('phase168-shot-combinations-storage.json')
    preservation=load('phase165-source-baseline.json')
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=load('phase168-prior-package-state.json')
    for row in previous['components'].values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    tested_pairs=sorted({(c['process'],c['mode']) for c in native.cases()})
    pending=[r for r in serial['mixed_pairs'] if (r['ProcessType'],r['DamageType']) not in tested_pairs]
    assert len(pending)==12
    counts=dict(native_scenes=24,independent_Ghidra_controls=5,
        Ghidra_compared_regions=sum(x['checked_regions'] for x in controls),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),
        unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtable_blocks=len(proof['blocks']),
        source_Shot_files=82,source_Shot_records=5551,observed_pairs=49,mixed_pairs=14,
        mixed_pairs_with_selected_new_collision_scenes=2,mixed_pairs_without_new_scenes=12,
        exact_container_edits=246,preserved_prior_components=145,preserved_source_files=420,preserved_EXE_paths=7,
        new_opaque_field_names=0,source_writes=0)
    return dict(phase=168,date='2026-11-02',family='Shots ProcessType/DamageType combinations',profile=proof['profile'],
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,order=['ProcessType/DamageType','Crypt groups','Global parameters','Creature resistances and flags','Other unknowns'],
            parked='Unpublished phase167 global-parameter leads retained; resume only after the first two blocks.'),
        collision_order=dict(root=0x4ABD60,event=4,process_call=0x4ABFC1,hit_test=0x4ABFCC,hit_call=0x4ABFDE,
            rule='Original collision callsProcess(event4) first, thenindependently checksDamageType7 and callsHIT_THING. A0mask gates onlyprocess callback. Callbackreturn0 is ignored at thiscallsite.',
            boundary='Thisdoesnotestablish returnvaluehandlinginotherevents; wholeShot tickevent2 explicitlytests thereturn separately.'),
        shared_parameters=dict(raw_Data1_u32=0x98,raw_Data2_u32=0x9C,raw_ShotProcessFlags_u32=0xA0,raw_DamageType_u8=0xC5,raw_ProcessType_u8=0xC7,
            meaning='Bothconsumers canread the same fullinstanceDWORDs. Onefield maymeanstatusduration anddirectdamagesimultaneously; keeprawbitsandpresentbothmeanings.',
            examples=['P5/D7: Data1=80 writesarmourtimer80,thenrequests-80HP; ordinarypreparedtargetloses40HP.',
                'P10/D7: Data1=80 withTPS20 writesinvulnerabletimer1600,thenHIT_THING returnswithoutHPrequest.',
                'P11/D7: Data1=80 withTPS20 writesfreezetimer1600; HPrequest-80 usescoefficient160/128 andloses100HP.',
                'P38/D7: Data1=80 writespoisontimer80andrequests-80directdamage; Data2=5 isdelayedseverity. OneCreatureticklater totalHP1000->915.',
                'P38/D7 withpreexistinginvulnerability2: directhitblocked,poisonstillapplied; followingCreaturetickrequests-5andHP1000->995.',
                'P38/D7 Data1FFFFFFFF: poisonlow16=65535 whileHIT_THING ignores signednonpositiveData1; noimmediateHPrequest.',
                'P10/D7 Data1=16384,TPS20: timerlow16=0anddirecthitremains16384; diagnosticinitialHP100000->83616.',
                'P38/D7 Data1=65538,Data2=65541: poison2/severity5butdirecthit65538; diagnosticinitialHP100000->34462.']),
        fixture=dict(source='Acceptedphase115three-raw-loaders/caster/collisionfixturewithpreparedactualclasssnapshots,World/map/owners/resources.',
            chain='RawCreaturedefinition,rawShotdefinitionandrawCreatureSpell loadedbyoriginalcompletefunctions; originalCreatureSpellwrappercreatesShotandsets caster100. Originalcollisionfinds target101incommoncell,sets targettaganddispatchesprocess4. OptionalwholeCreaturetick.',
            adjusted_inputs='DiagnosticrawShot62: ProcessType,DamageType,maskandsharedparameters varied; CreatureSpellData1/2 explicitlymatchrequestedinstancevalues. InitialtargetHP1000except2wide cases100000.',
            limits='No naturalflight,liveWorldscheduler,graphics/audio,alltargetclasses,alllethal/deletionbranchesorautomaticcrossversiontransfer. DirectCPUcalls; adaptersonlyforexternalresources/audio.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=serial,preservation=preservation,counts=counts,
        block_remaining=dict(observed_mixed_pairs_without_new_scenes=pending,
            next=['P17/D2 Wind','P30/D1 andP30/D2 Inferno','P31/D2 Gas','P39/D2 JackInTheBox'],
            additional=['P7/D7','P9/D7','P28/D7','P29/D1','P29/D7','P34/D2','P34/D7'],
            closure='SelectedscenesforP11/D7andP38/D7arenotcompletepaircoverage. Eventmasks,tick/start/end,overrides,otherclassesandapplicableversionsstillneedexplicitboundaries.'),
        helpers=[link(n) for n in ['emulate_shot_combinations.py','crosscheck_shot_combinations.py','prepare_shot_combinations_primary.py','validate_shot_combinations.py']],
        limitations=['ExactprovidedDXSHA; nochangestooriginalgameorparalleleditor.',
            '49pairscountall82providedcontainersincludingmap/modvariants; not49shippedBullfrogcombinations.',
            '24diagnosticcollisioncases,notall360enumcombinations,notallvariantsfor49observedpairs.',
            'Twohigher-HPboundaryscenesisolate narrowingwithoutlethalbranches; donotclaimnormalmaximumhealth100000.',
            'FiveGhidraCPUcontrolscompareallpreparednonstackregions; theircontentsarenotallsemanticallydecoded.',
            'Prior145componentsandphysicalcodecunchanged; historical4preexistingmapdriftsremainrecordedbyphase165baseline.',
            'No newglobalresist/creatureflagconclusion; thisphaseonlytracksstatusinteractionsneededforcollisionpairs.'])

def documents(s):
    lines=['# ProcessType / DamageType: сочетания и порядок попадания','',
        'Этап 168, 2 ноября 2026. Первый приоритетный блок открыт. Следующий блок — Crypt; глобальные параметры, резисты и флаги отложены в установленном порядке. Этап 167 оставлен для уже сохранённых, но не опубликованных глобальных зацепок.','',
        'Проверен предоставленный DKII-DX.exe, SHA-256 `'+s['profile']['sha256']+'`.','',
        '## Подтверждённый порядок','',
        'Целая `4ABD60` находит цель и вызывает `4AD730(event=4)` по `4ABFC1`. Затем независимо проверяет DamageType=7 по `4ABFCC` и вызывает HIT_THING / `4AC420` по `4ABFDE`. Возврат процесса в этом месте не проверяется. Поэтому callback, вернувший 0, не отменяет прямой урон. У других событий правило может отличаться: tick отдельно проверяет возврат event=2.','',
        'Бит `0x4` поля `ShotProcessFlags` по raw `0xA0` разрешает процесс на попадании. Его отсутствие пропускает процесс, но не отключает HIT_THING. Контроли с mask=2 это подтвердили.','',
        '## Выбранные сочетания','',
        'Обычная подготовленная цель: HP=1000, Data1=80, Data2=5, TPS=20, event mask=4. Варианты меняют именно указанные параметры. Это диагностические сочетания на базе исходной записи Shot62; все исходные файлы сохранены.','',
        '| Процесс + DamageType | HP после столкновения | Дополнительный результат |',
        '|---|---:|---|',
        '| NONE + HIT_THING | 920 | Контроль прямого урона |',
        '| MODIFY_ARMOUR + HIT_THING | 960 | Таймер брони80 уже действует на этот удар |',
        '| INVULNERABLE + HIT_THING | 1000 | Таймер1600 установлен до удара; запрос изменения HP отсутствует |',
        '| FREEZE + HIT_THING | 900 | Заморозка1600; запрос −80 усиливается коэффициентом160/128 |',
        '| POISON_SPIT + HIT_THING | 920 | Яд80/5; после одного полного Creature tick HP915, таймер79 |',
        '| POISON_SPIT + HIT_THING, начальная неуязвимость2 | 1000 | Яд накладывается; следующий tick даёт HP995 |',
        '| FEAR(no-op) + HIT_THING | 920 | Нулевой возврат callback не отменяет обычный урон |','',
        'Варианты без обычного урона и варианты без бита4 выполнены отдельно. Например, броня+DamageType0 оставляет HP1000 и ставит таймер80; тот же процесс с DamageType7/mask2 оставляет таймер0 и наносит80.','',
        'Data1 одновременно имеет два смысла. У P38/D7 значение `FFFFFFFF` задаёт таймер яда65535, но прямой HIT_THING игнорирует отрицательный signed32. У `10002h` таймер2, а прямой урон65538; Data2=`10005h` даёт силу яда5. У P10/D7 Data1=16384 приTPS20 обнуляет 16-битный таймер, но сохраняет прямой урон16384. Для двух больших положительных значений начальный HP явно поднят до100000; смерть и её последующая обработка в эти сцены не включены.','',
        '## Проверки и сохранность','',
        '24 принятые native-сцены проверяют HP, последовательность запросов, записи HP, таймеры, исходные записи, три пути загрузки, фабрику, caster/target и оригинальные vtable. Пять независимых Ghidra CPU-контролей сравнили 325 областей / 652470 байт. Первичные байты покрывают все 9006 исполненных адресов инструкций: 55 дополнительных диапазонов, 155 унаследованных и четыре исходные vtable. Полные диапазоны не означают исполнение всех ветвей.','',
        '246 точных пересборок в памяти на82 контейнерах: Data1/Data2/ShotProcessFlags/DamageType/ProcessType, обратное восстановление и decode→rebuild. Прочие байты не изменены. Проверены SHA420 текущих источников и7 EXE по уже сохранённому baseline; четыре прежних изменения карт явно остаются в его истории. Предыдущие145 компонентов, codec и разметки каталогов сохранены.','',
        '## Очередь внутри первого блока','',
        'В5551 записи из82 предоставленных файлов найдено49 пар, из них14 имеют оба ненулевых типа. Корпус включает модифицированные карты: эти числа не означают штатный комплект Bullfrog. Для двух смешанных пар — P11/D7 иP38/D7 — добавлены выбранные CPU-сцены; это не объявляет все их значения и события полностью проверенными. Остальные12 смешанных пар остаются в очереди этого же блока.','',
        'Далее: Wind+2, Inferno+1/+2, Gas+2, JackInTheBox+2; затем7/7,9/7,28/7,29/1,29/7,34/2,34/7. После этого — сводка порядка start/tick/collision/end, общих параметров, масок событий и границ проверенных вариантов. К Crypt пока не перехожу.','',
        '| ProcessType | DamageType | Записей в корпусе | Сцены этапа168 |','|---:|---:|---:|---|']
    for r in s['storage_validation']['observed_pairs']:
        lines.append(f'| {r["ProcessType"]} | {r["DamageType"]} | {r["source_records"]} | '+('Выбранные диагностические столкновения' if r['phase168_native_cases'] else 'Нет новых сцен этого этапа')+' |')
    lines += ['', 'Машинная схема: `shot-combination-semantics.json`. Инвентарь с точными исходными путями, именами, ID, масками и вариантами параметров: `phase168-shot-combinations-storage.json`. Блок не завершён; общая цель активна.']
    (ROOT/'SHOT_COMBINATIONS_RU.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    note=('Приоритет пользователя от 2 ноября: ProcessType/DamageType → группы Crypt → глобальные параметры → резисты и флаги существ → остальное. '
          'Текущий блок ProcessType/DamageType открыт; порядок закреплён в `RESEARCH_PRIORITIES_RU.md`.\n\n'
          'Актуальный этап168: пакет146 компонентов. Выбранные столкновения подтверждают process4 перед HIT_THING, независимость маски процесса и обычного урона, '
          'влияние только что поставленных брони/неуязвимости/заморозки и сочетание яда с прямым уроном. '
          '24native/5Ghidra,325областей/652470байт;55дополнительных/155унаследованных диапазонов,9006PC;246точныхпересборок82Shots. '
          'Инвентарь49пар/14смешанных включает модифицированные карты;12смешанныхпар без новых сцен остаются в этом же блоке. '
          'Прежние145компонентов/codec/420источников/7EXE сохранены. См.`SHOT_COMBINATIONS_RU.md`; блок и общая цель открыты.\n\n')
    f=ROOT/'INTEGRATION_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('Приоритет пользователя от 2 ноября:'):f.write_text(note+txt,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 168'):
        f.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 168\n\n'+note+'Экспертная оценка около75% сохраняется. Первый приоритетный блок не завершён.\n\n'+txt,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase168-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==145
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);assert len(old['components'])==145
    s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package['components'][KEY]=link(SCHEMA);package[KEY]=s
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']=dict(source=link(SCHEMA),block_complete=False,collision_event=4,
        rule='Process first,thenindependentDamage7. Eventmaskgatesonlyprocess; ignorecallbackreturnatcollision. BothmayreadsameData1/2; preparedstatuscanaltertheverysamehit.',
        observed_pairs=49,mixed_pairs=14,selected_collision_cases=24,not_a_complete_pair_matrix=True)
    package['effective_shot_parameters']=effective
    note='shot_combination_semantics phase168: selectedoriginalcollisionpairs,process4beforeindependentHIT7; armour/invulnerable/freezeaffectsamehit,poisonanddirectdamagecoexist.24native/5wholeGhidra/246exactcontaineredits;49observedpairs14mixedincludeprovidedmods,12mixedwithoutnewscenes. Prior145components/codec/420currentbaseline sources/7EXEpreserved. Priorityblock1open; Crypt/globals/resists/otherfollowinuserorder.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase168-search-status.json',dict(phase=168,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        counts=s['counts'],remaining=s['block_remaining'],priority_order=s['priority']['order'],phase167='Retained unpublished global leads; deferred by explicit user priority.',
        readiness='Expert estimate remainsabout75%; not measured byte coverage.',evidence=[link(SCHEMA),link('SHOT_COMBINATIONS_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
