"""Publish Chainfire source/event8 proof and consolidated first-priority result."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_shot_chainfire as native
import crosscheck_shot_chainfire as cross
import prepare_shot_chainfire_primary as primary
import validate_shot_chainfire_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_chainfire_semantics'
SCHEMA='shot-chainfire-semantics.json'
SUMMARY='shot-combination-block-summary.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def summary():
    before=load('shot-combination-coverage.json');manifest=load('phase176-chainfire-emulations.json')
    assert len(manifest['replays'])==13
    rows=deepcopy(before['rows'])
    pair=next(r for r in rows if (r['ProcessType'],r['DamageType'])==(29,7))
    for name in ['source50','source36','source14','source155','child36-four','clear8-two','clear256-two','whole-selected-chain']:
        scene=next(r for r in manifest['replays'] if r['case']['name']==name)
        assert sha((ROOT/scene['file']).read_bytes())==scene['sha256']
        pair['selected_composition_executions'].append(dict(phase=176,case=name,trace=dict(file=scene['file'],sha256=scene['sha256']),source_schema=SCHEMA))
    pair['rule']+=' ActualsourceChainfire50/36/14/155 graph executed; event8 zero skips flags1 deletion/decay and permits repeated solid-child creation in selected path. Causalclear8vs256 controls prove gate.'
    counts=deepcopy(before['counts']);counts['native_scenes_current_block']+=13;counts['independent_Ghidra_controls_current_block']+=6
    assert counts['native_scenes_current_block']==201 and counts['independent_Ghidra_controls_current_block']==62
    return dict(phase=176,date='2026-11-03',profile=before['profile'],counts=counts,source_scope=before['source_scope'],
        previous_matrix=link('shot-combination-coverage.json'),rows=rows,
        priority_handoff=dict(previous='ProcessType/DamageType',next='Crypt groups',
            current_priority_block_review_complete=True,full_goal_complete=False,
            reason='Corecompositionrules,all14observedmixedpairs:selectedexecution,sharedparameteroverrides,eventreturncontractsandconcreteactualsourcechildgapsconsolidated. Remainingbroaderexecution/versionboundariesexplicitlyretained.'),
        completed_requirements=[
            dict(requirement='Inventory exact provided source combinations',evidence=link('phase168-shot-combinations-storage.json'),result='82containers/5551records/49pairs/14mixed/43mixedsourcevariants;notcertifiedshippedcatalogue.'),
            dict(requirement='Selected composition for each observed nonzero pair',evidence=link('shot-combination-coverage.json'),result='14/14selected,notall43variants/alltargetclasses. Phase176addsactualChainfirevariantsandcausalevent8proof.'),
            dict(requirement='Start/tick/collision/solid/death/terminal ordering and return meaning',evidence=[link('shot-lifecycle-semantics.json'),link('shot-instant-experience-semantics.json')],
                result='D5before64,2abort,collision4ignored,conditional8abort,death128distinctfromterminalD6. Originaldispatcherreturns0canhavecaller-dependentconsequences.'),
            dict(requirement='Raw/default/effective Data1/2 and source child inheritance',evidence=[link('shot-child-chain-semantics.json'),link('shot-source-status-semantics.json')],result='FullDWORDcopyafterchildfactoryforHitThing/Death;newphase176alsoexecutesHitSolidcopy4AB43F/445.'),
            dict(requirement='Reproducible execution/independent CPU/primary bytes',evidence=link('phase176-chainfire-primary.json'),result='BoundedacceptedfixturesandexactPEbytes;noinstructions/vtablespatched.'),
            dict(requirement='Physical edits and source preservation',evidence=link('phase176-chainfire-storage.json'),result='246exactin-memoryedits;allunknownbytesandoriginalfilespreserved.'),
            dict(requirement='Explicit unresolved boundaries',result='Listedbelow;priorityhandoffdoesnotmarktheseachievedorclaimcompletegameengineunderstanding.'),
        ],
        event_contract=deepcopy(before['event_contract']),parameter_sources=deepcopy(before['parameter_sources']),
        ordinary_damage_modes=deepcopy(before['ordinary_damage_modes']),correction_precedence=deepcopy(before['correction_precedence']),
        resolved_previous_immediate_gap='ActualChainfireevent8/0x10Ccausalproofandchildgraphnowcompletefor13declaredscenes.',
        residual_questions=[
            dict(topic='Every source variant and target kind',status='not_exhausted',scope='201selectedscenesarenotall43mixedvariants,all49pairswithallvalues,allCreature/Object/Trap/Door/deathbranches.'),
            dict(topic='World scheduler and cleanup',status='unproved_composition',scope='Completeobjectmethodsandactualchildrenbuthostorder;noWorldinterleaving,state255/freeze recovery,DPS,terminalpool-freeorperpetuallifetimeclaim.'),
            dict(topic='Other event/child/failure graphs and extreme arithmetic',status='not_exhausted',scope='FiveidentifieddirectCALLmasksarenotuniversalindirectconsumerproof;noallcyclicgraphs/capacity-failures/INT_MIN/map-overflowcoverage.'),
            dict(topic='Other versions and DLL hooks',status='not_transferred',scope='ExactprovidedDXSHAonly;RC3/localold/normalEXE/patchDLLsemanticsneedownproof.'),
        ],
        future_order=['Crypt groups','Global parameters','Creature resistances and flags','Other unknowns and recorded cross-block followups'],
        overall_goal='Active;all15familiesandremainingopaqueconsumersstayintact.Readinessabout75%expertjudgement,notmeasuredbytecoverage.')

def build():
    manifest=load('phase176-chainfire-emulations.json');assert len(manifest['replays'])==13
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(saved['result'])
    controls=load('phase176-chainfire-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase176-chainfire-primary.json')
    stored=storage.validate();assert stored==load('phase176-chainfire-storage.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase176-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    summ=summary();assert summ==load(SUMMARY)
    source,ids,raws=native.probe.base.source(native.probe.config({}));sources=[]
    for ID in ids:
        r=raws[ID];sources.append(dict(ID=ID,name=native.probe.base.old.target.fmt.string(r[:32]),record_sha256=sha(r),ProcessType=r[0xC7],DamageType=r[0xC5],Data1=native.u(r,0x98),Data2=native.u(r,0x9C),
            mask=native.u(r,0xA0),Flags=native.u(r,0xA8),Health=native.u(r,0xBC,2),DeathShotId=r[0xBF],TimedDelay=r[0xC0],HitSolidShotId=r[0xC1],HitThingShotId=r[0xC4]))
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=13,complete_Shot_ticks=visit(0x4AB180),whole_Shot_factories=visit(0x4B7CD0),whole_targeted_firing=visit(0x4A7FD0),Deckem_callbacks=visit(0x4AF610),
        event8_zero_returns=sum(row['outcome']['event8_zero_returns'] for row in manifest['replays']),source_records=5,source_record_inputs=65,diagnostic_mask_copy_scenes=3,
        independent_Ghidra_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtables=5,
        exact_container_edits=246,Shot_files=82,Shot_records=5551,preserved_prior_components=153,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    return dict(phase=176,date='2026-11-03',families=['Shots','Traps'],profile=proof['profile'],counts=counts,
        priority=dict(current_priority_block_review_complete=True,completed_review='ProcessType/DamageType',next='Crypt groups',full_goal_complete=False),
        source=dict(file=str(source),sha256=sha(source.read_bytes()),records=sources,scope='Providedlocalmodmap,notcertifiedshippedcontent.'),
        graph=[dict(parent=50,HitThing=36,HitSolid=36,Death=75),dict(parent=36,HitThing=14,HitSolid=14,Death=75),dict(parent=14,HitThing=155,HitSolid=155,Death=75),dict(parent=155,HitThing=75,HitSolid=75,Death=0)],
        inheritance=dict(HitSolid_factory=0x4AB3C9,Data1_copy=0x4AB43F,Data2_copy=0x4AB445,
            rule='Originalsolid-childfactoryinitializesownvalues,parentcopiesbothDWORDsafterwardsbeforeevent8. Actual75raw25/3becomesruntime250/3;otherChainfirechilddefaults250/3matchbutwritesstillobserved.'),
        event8=dict(call=0x4AB4A5,return_test=0x4AB4AA,zero_return=0x4AB4B5,next_flag1_delete=0x4AB4C6,decay=0x4AB4DF,
            callback='P29/4AF610 returns0 even when it can enter state255 on the inherited selected Creature; dispatcher propagates0 for matched mask8.',
            order='Original4B2030 reportsselectedsolidpath;nativeHitSolidchildcreationandparametercopyhappenbeforeevent8. Zero returnexitsbeforeflags1deletionanddecay.',
            source_result='Actualcreated36getsHealth40. One/two/fourselectedwholeshottickscreateone/two/four14childrenandretainHealth40;ageincreases. Child14and155alsoreturn0onevent8inselectedchain.',
            causal='CPUcopy36mask0x10C->0x104clearsjust8:dispatcherreturns1,originalflags1vslot20setsHealth0;nextselectedtickcreatesnofurtherchild. Clearingjust256to0x0Cretainsevent8zeroandtwocreated14children/Health40.',
            boundary='Notproofofperpetualexistenceorunboundedgrowthinlivegame;Worldscheduler,subsequenttrajectory/resourcefailures/cleanupandCreatureticknotcomposed.'),
        collision=dict(rule='Originalraw50/36/14/155eachhasonefullselectedtargetedfirsttick:collisionevent4zeroignored,state255thenhit250causes500actualHPdelta. FlagsendShotHPFFFFFFFFafterdeletionanddecay;notpoolfree.',
            children='50/36/14alsocreate75byoriginaldeathpath;155DeathID0doesnot. HitThingchildcreatedbeforeprocess4aspreviouscomponents.'),
        area=dict(parent_death75='Created75rawData25becomes250. Selecteddeath75tickrequests158/98fromrangefalloff;firsttargetstate255meansactual316/98HPdelta.',
            solid_chain75='Hostselected50->created36->created14->created155->created75 hasarea250/229atthatnativeposition. Initialhit500+area500givesselectedHP99000;secondHP99771from100000.',
            invulnerable='Timer445suppressesfirsttargetordinaryhitandarea;secondtargetstillloses229. Order-dependentD2workretained.',
            model='Independentboundedphase131D2integerformularecomputedfromselectedstationaryexplosionoriginandpreparedtargets;positionsarenativeoutput,notaflightreimplementation.'),
        fixture=dict(prepared='Fulloriginalrawloaders,targetedTrap17fire,nativepool/registry/AI/currentArt/mapandwholeShotmethods. DiagnosticHP100000avoidsdeath. FiveID-sorteddefinitions14/36/50/75/155.',
            diagnostic='Threecausalmasksceneseditonlybit8or256ofCPUcopy36;allotherrawinputsunchanged. NooriginalEXE/data/editorwrites.',
            boundary='Hostchooseswhichcreatedobjectmethodrunsnext. NoWorldinterleaving/fulltrajectory/DPS,Creature255recovery,physicalpoolfreeorunlimitedcapacityclaim.',
            memory='AllactualparameterMOVs,defaults/checksums/tags/pointers/ID/counts/guardschecked;onlyDWORD FS:[0]allowedbelow4096;invalidreadprobe0.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=stored,preservation=baseline,block_summary=link(SUMMARY),
        helpers=[link(n) for n in ['probe_shot_chainfire.py','emulate_shot_chainfire.py','crosscheck_shot_chainfire.py','prepare_shot_chainfire_primary.py','validate_shot_chainfire_storage.py']],
        limitations=[r['scope'] for r in summ['residual_questions']])

def documents(s,summ):
    n=s['counts']
    text=f'''# Chainfire: исходная цепочка и ранний выход event8

Этап176, 3 ноября2026. Предоставленный DKII-DX.exe, исходный модифицированный `NeedsTestGlobals.kld`. Воспроизводимый результат закрывает конкретный вопрос таблицы этапа175; перенос на другиеEXE не подразумевается.

| ID | Название | Process/Damage | Data1/2 | Mask | Health | HitThing / HitSolid / Death |
|---:|---|---|---|---|---:|---|
|50|Chainfire1|29/7|250/3|0x10C|40|36 /36 /75|
|36|Chainfire2|29/7|250/3|0x10C|40|14 /14 /75|
|14|Chainfire3|29/7|250/3|0x10C|40|155 /155 /75|
|155|Chainfire4|29/7|250/3|0x10C|128|75 /75 /0|
|75|Firebomb Explosion|0/2|25/3|0x104|1|0 /0 /0|

У14/36 TimedDelay=251 — это отдельный байт0xC0, а не ссылка ShotID251. В цепочке нет попытки загрузить отсутствующего251.

## Возврат процесса меняет жизнь снаряда

У исходного50 первый целый targeted tick реально создаёт36 через HitThing и75 через Death. Process29 события4 возвращает0, но обычный hit250 всё равно выполняется. В выбранном Creature-контексте процесс до удара входит вstate255: запрос250 изменяетHP на500. Аналогично отдельно выполнены неизменённые36/14/155.

Затем реально созданный36 проходит следующий целый tick. В проверенной геометрии исходная4B2030 сообщает ветку столкновения с поверхностью. Она **сначала создаёт14**, затем копируетData1/2 по4AB43F/4AB445, затем вызывает процесс события8 по4AB4A5. Callback29 возвращает0, диспетчер передаёт0, и4AB4B5 выходит изtick.

Из-за этого пропускаются последующий original flags1 запрос удаления и уменьшениеHP по4AB4DF. Health36 остаётся40. Один, два и четыре выбранных тика создают соответственно1/2/4 экземпляра14, возраст36 увеличивается, Health40 сохраняется. Это доказано на исходной mask0x10C.

Причинные контроли меняют толькоCPU-копию записи36:

| Маска36 | Изменение | Возврат dispatcher на8 | Результат двух выбранных тиков36 |
|---|---|---|---|
|0x10C|Исходная|0|Два14, Health36=40|
|0x104|Очищен только8|1|Один14, исходный flags1 ставитHealth36=0; второйtick нового ребёнка не создаёт|
|0x00C|Очищен только256|0|Два14, Health36=40|

Следовательно, наблюдаемый ранний выход определяется битом8. Эта проверка не устанавливает общий смысл бита256 за пределами выбранных потребителей.

**Не следует называть снаряд бессмертным в живой игре:** выполнены выбранные целые методы и реальные фабрики, но не полныйWorld scheduler, все последующие координаты/коллизии/лимиты, Creature recovery и освобождение памяти. Результат показывает конкретный путь повторного создания при пропущенной штатной обработке послеevent8.

## Наследование урона взрывом

Оригинальный75 имеетData25/3. Реальные HitThing/HitSolid/Death родители заменяют его instance-параметры на250/3 после загрузки defaults. Поэтому собственный25 не предсказывает повреждение этого взрыва.

Отдельный выбранный tick death75 наносит запросы158/98 после дистанционного ослабления; state255 усиливает первый фактическийHP-сдвиг до316. Полная выбранная последовательность50→36→14→155→75 даёт для последнего взрыва250/229 на других native-координатах. ПриHP100000 начальный hit500 иarea500 оставляют выбранной цели99000, второй —99771. Таймер445 блокирует обычные повреждения первой, но вторая по-прежнему теряет229. Это результаты стендовой последовательности, не живойDPS.

## Проверки

13 native-сцен: {n['complete_Shot_ticks']} целых Shot tick, {n['whole_Shot_factories']} полных Shot-фабрик, {n['event8_zero_returns']} нулевых возвратовevent8. Пять source-записей загружены65раз; три сцены имеют явно объявленную копию mask36. Проверены реальные MOV, tag/registry/count/ID, initial defaults, runtime-параметры, checksums и все guards.

Шесть независимых Ghidra-контролей сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных PC покрыты {n['inherited_function_extents']} прежними точными диапазонами; новых функций не создавалось. 246 пересборок82контейнеров в памяти сохранили остальные байты. SHA420текущих источников и7путейEXE, codec и153прежних компонентов совпали.

Машинная схема — `shot-chainfire-semantics.json`. Общий итог первого приоритетного блока — `SHOT_COMBINATION_BLOCK_RESULT_RU.md`. Исходные игровые файлы и параллельный редактор не менялись.
'''
    (ROOT/'SHOT_CHAINFIRE_RU.md').write_text(text,encoding='utf-8')
    text='''# Итог первого блока: ProcessType / DamageType

3 ноября2026, этап176. Основной разбор сочетаний сведен и проверен; следующий приоритет — группыCrypt. Это завершение текущего блока с перечисленными границами, а не объявление всех15форматов или игрового движка полностью расшифрованными.

В82предоставленных контейнерах5551запись,49пар,14смешанных пар и43варианта смешанных записей. Для14/14пар есть выбранные исполнения композиции. В этапах168–176 накоплена201принятая native-сцена и62независимых Ghidra CPU-контроля. Число14/14 не равно исчерпывающему покрытию43вариантов или всем значениям полей.

Подтверждены независимые Process/Damage dispatcher, caller-зависимое значение возврата, последовательность первогоtick/обычногоtick/collision/solid/death/terminal, совместное использованиеData1/2, переопределения отCreatureSpell/Trap и перенос родительских параметров после настоящих фабрик детей. Выполнены исходные цепочкиMagistorm,Crystal иChainfire. Доказательства и ID/маски/параметры/пути всех49пар — `shot-combination-block-summary.json`; таблица этапа175 сохранена как история.

Практически важные правила для редактора:

1. Два поляData1/2 одновременно обслуживаютProcessType иDamageType. Глобально назначать им один смысл, масштаб илиsignedness нельзя.
2. Значения Shot-файла — defaults. Исходная стрельба может заменить их данными заклинания/ловушки; созданные дети получают обаDWORD родителя после загрузки своихdefaults. Нули в дочерней записи не гарантируют отсутствие урона.
3. Возврат0 процесса не имеет единого смысла: наcollision4 обычныйhit продолжается; наtick2 прерывается; наsolid8 пропускается оставшаяся обработка, включая удаление/decay. УChainfire это подтверждено причинным контролем одного бита8.
4. Состояние цели изменяет тот же последующий удар: Freeze160/128, Deckem/state255 — двойной отрицательныйHP-сдвиг в проверенных ветках. Независимый прямой урон не всегда подчиняется иммунитету самого процесса.
5. D2 имеет общий порядкозависимый накопитель ослабления. TIME/RING используют разные источники/масштабыData2; названия режима сами по себе не описывают геометрию. D6 вызывается отдельнымterminalcallback, а не автоматически при любомHP0.
6. Guided обновляет скорость поSession clock&7==0, а неRNG. XPData1 — unsignedselector, не число уровней; исходнаяmask1 не вызываетXP в проверенных путях.

Остаются вопросы, которые сохраняются в общей цели и не считаются решёнными:

- Полное исполнение каждого из43исходных вариантов, всех классов целей/смертей/пределов и неиспользованных значений.
- World scheduler, interleaving детей, Creature recovery, естественныйterminalcleanup/pool-free, живойDPS и продолжительность всей траектории.
- Все косвенные потребителимасок, циклические/ошибочные графы, неограниченные значения/переполнения.
- Перенос выводов конкретногоDKII-DX.exe наRC3, старые/обычныеEXE и перехватыpatchDLL.

Эти границы не мешают документировать подтверждённые поля и сохранять их без потерь. Они запрещают автоматически превращать выбранные исполнения в утверждение о100%механики всех версий. Новые связанные неизвестные продолжают храниться в общей очереди; текущий следующий рабочий блок — Crypt, затем глобальные параметры, затем резисты/флаги.

Общая цель активна. Экспертная оценка около75%не меняется автоматически от количества сцен или компонентов. Пакет154компонента;153прежних,codec,420текущихисточников/7путейEXE сохранены. Проверки этапа176 и полные ограничения — `SHOT_CHAINFIRE_RU.md`.
'''
    (ROOT/'SHOT_COMBINATION_BLOCK_RESULT_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап176: пакет154 компонента. ИсходныйChainfire50/36/14/155→75 иevent8 проверены: callback29=0 пропускаетflags1 удаление/decay; source36 за4выбранныхtick создаёт4детей и сохраняетHP40; clear8ставитHP0, clear256неизменяетветку. '
        f'13native/{n["complete_Shot_ticks"]}wholeShot-tick/6Ghidra,{n["Ghidra_compared_regions"]}областей/{n["Ghidra_compared_bytes"]}байт;{n["unique_executed_original_PCs"]}PC/{n["inherited_function_extents"]}диапазонов;246пересборок. '
        'Основной итог первого блока:49пар/14смешанных выбранные14/14,201native/62Ghidra;границы вариантов/World/версий сохранены. Следующий приоритетCrypt. См.`SHOT_COMBINATION_BLOCK_RESULT_RU.md` и `SHOT_CHAINFIRE_RU.md`.\n\n')
    f=ROOT/'INTEGRATION_RU.md';txt=f.read_text(encoding='utf-8')
    if 'Актуальный этап176:' not in txt[:1800]:
        first,rest=txt.split('\n\n',1);f.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 3 ноября 2026 — после этапа 176'):
        _,rest=txt.split('\n',1);f.write_text('# Оценка готовности на 3 ноября 2026 — после этапа 176\n\n'+note+'Экспертная оценка около75%; следующий блокCrypt, общая цель открыта.\n'+rest,encoding='utf-8')
    (ROOT/'RESEARCH_PRIORITIES_RU.md').write_text('''# Очерёдность исследования

Установлена пользователем2ноября2026. Последовательно блоками:

1. ProcessType/DamageType — основной разбор сведен этапом176. Выбранные композиции14/14наблюдаемых смешанных пар, общие параметры, возвраты событий и исходные дочерние цепочки подтверждены. Полный итог и неразрешённые границы: `SHOT_COMBINATION_BLOCK_RESULT_RU.md`, `shot-combination-block-summary.json`.
2. **Crypt группы — текущий блок.**
3. Глобальные параметры.
4. Резисты и флаги существ.
5. Остальные неизвестные и сохранённые межблочные вопросы.

Итог первого блока не означает исполнение всех43исходныхвариантов/всехклассов/Worldscheduler/другихверсий. Эти вопросы явно сохранены, не считаются доказанными и входят в общую цель.

ДляCrypt сначала свести уже доказанные группы, родственные/владельческие зависимости и конкретные нерешённые потребители; затем добивать оставшиеся назначения по исходным инструкциям. Ранние имена вроде бонуса«работающему» не подменяют область применения счётчиков/потребителей.

Этап167 по глобальнымпараметрам сохранён безпубликации; до третьего блока не возобновлять. Общая цель по15семействам активна. Игровыеисточники/EXE/параллельныйредактор не менять.
''',encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase176-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==153
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);summ=summary();dump(SUMMARY,summ);s=build();dump(SCHEMA,s)
    assert len(old['components'])==153 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters']);effective['combination_rules']['chainfire_event8_extension']=dict(source=link(SCHEMA),summary=link(SUMMARY),current_priority_block_review_complete=True,
        rule='ActualChainfireHitSolidchildreninherit250/3;event8callback29zeroexitsbeforeflags1deletion/decay. Sourcechild36Health40andfourchild14creations;clear8causalcontrolHealth0,clear256retainsrepeat. NoWorldperpetuallifetimeclaim. Corecombinationreviewconsolidated,recordedboundariesretained;Cryptnext.')
    package['effective_shot_parameters']=effective
    note='shot_chainfire_semantics phase176:13native/6Ghidra;actualsource50->36->14->155->75,HitSolidData1/2copies4AB43F/445. P29event8zeroearlyexitbeforeflags1delete/decay;source36fourwholecallsHP40/four14children,clear8HP0/onechild,clear256repeatunchanged. Corefirstblockconsolidated49pairs/14mixedselected14/14/201native/62Ghidra,unresolvedWorld/allvariants/versionsretained. Cryptnext;goalactive. Prior153components/codec/baselinepreserved.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s,summ)
    dump('phase176-search-status.json',dict(phase=176,date='2026-11-03',goal_status='active',current_block='Crypt groups',previous_block_review_complete=True,
        previous_goal_turn=dict(classification='progress',reason='Phase175publishedandaudited;actualsourceChainfireevent8causalcontrolsandgraphcompletedandindependentlycrosschecked.'),
        counts=s['counts'],coverage_counts=summ['counts'],residual=summ['residual_questions'],next='Inventory Cryptgroups/knownowner-countconsumersandunresolvedfieldsbeforeselectingfreshnativeproof.',
        evidence=[link(SCHEMA),link(SUMMARY),link('SHOT_CHAINFIRE_RU.md'),link('SHOT_COMBINATION_BLOCK_RESULT_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
