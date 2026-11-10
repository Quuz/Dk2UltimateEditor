"""Publish phase172 XP/Instant compositions, target timing and event boundaries."""
from pathlib import Path
from copy import deepcopy
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_shot_instant_experience as native
import crosscheck_shot_instant_experience as cross
import prepare_shot_instant_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_instant_experience_semantics'
SCHEMA='shot-instant-experience-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def event_evidence():
    blob=native.EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    rows=[]
    for push,call,mask in [(0x4AB27D,0x4AB281,64),(0x4AB30B,0x4AB30F,2),(0x4AB49E,0x4AB4A5,8),(0x4AB65B,0x4AB662,128),(0x4ABFBD,0x4ABFC1,4)]:
        ins=next(md.disasm(native.p.pe_read(blob,push,8),push));assert ins.mnemonic=='push' and int(ins.op_str,0)==mask
        ins=next(md.disasm(native.p.pe_read(blob,call,5),call));assert ins.mnemonic=='call' and int(ins.op_str,0)==0x4AD730
        rows.append(dict(push=push,call=call,event_mask=mask))
    xrefs='ghidra/phase172-process-dispatch-xrefs.txt';text=(ROOT/xrefs).read_text(encoding='utf-8')
    reported=[int(x,16) for x in re.findall(r'^From ([0-9a-fA-F]+)',text,re.M)]
    assert set(reported)=={r['call'] for r in rows}
    files=[xrefs]+[f'ghidra/phase172-order-{a:06x}-assembly.txt' for a in [0x4AD730,0x4AB180,0x4ABD60,0x4AE630,0x457880,0x48B120,0x4AFCB0,0x51DC40,0x441990,0x4363F0]]
    return dict(identified_direct_dispatch_callers=rows,reported_xrefs=link(xrefs),
        event1='None of the five identified directcall sites passesmask1. FullsourceXPShot182creation/collision/tickscenesneverexecute Process7withsourceProcessFlags1. This isnot a universal no-indirect-call/no-patch statement.',
        sources=[link(n) for n in files])

def build():
    manifest=load('phase172-instant-xp-emulations.json');assert len(manifest['replays'])==32
    traces=[];unchanged=[];xp_inputs=0;refs=[]
    for process,mode in [(7,7),(34,2),(34,7)]:
        q=native.probe.source(process,mode);file=Path(q['file']);raw=next(s.records[q['record_index']] for s in native.probe.target.fmt.parse(file.read_bytes()) if s.signature==260)
        refs.append(dict(process=process,mode=mode,file=str(file),sha256=sha(file.read_bytes()),record_index=q['record_index'],record_sha256=sha(raw),ShotID=raw[0xBE],name=native.probe.target.fmt.string(raw[:32]),
            Data1=native.u(raw,0x98),Data2=native.u(raw,0x9C),ProcessFlags=native.u(raw,0xA0),Flags=native.u(raw,0xA8)))
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        source=next(x for x in refs if (x['process'],x['mode'])==(c['process'],c.get('mode',7)))
        if sha(checks[native.p.shot.RAW])==source['record_sha256']:unchanged.append(c['name'])
        if c['process']==7:xp_inputs+=288
        traces.append(saved['result'])
    gv=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
    raw_levels=[r for s in native.probe.target.fmt.parse(gv.read_bytes()) if s.signature==230 for r in s.records if native.u(r,0)==65]
    assert len(raw_levels)==288
    level_input=bytes(16)+raw_levels[0]
    controls=load('phase172-instant-xp-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase172-instant-xp-primary.json')
    storage=load('phase172-instant-xp-storage.json');assert storage['exact_container_edits']==410 and storage['source_writes']==0
    baseline=load('phase165-source-baseline.json')
    for row in baseline['source_files']+baseline['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=load('phase172-prior-package-state.json')
    for row in previous['components'].values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=32,XP_scenes=16,Instant_scenes=16,complete_Shot_ticks=visit(0x4AB180),complete_collision_calls=visit(0x4ABD60),
        XP_callbacks=visit(0x4AE630),Instant_callbacks=visit(0x4AFCB0),whole_targeted_firing=visit(0x4A7FD0),whole_level_up_calls=visit(0x48B120),
        unchanged_GlobalVariables65_records=288,unchanged_GlobalVariables65_record_inputs=xp_inputs,unchanged_source_Shot_inputs=len(unchanged),
        independent_Ghidra_controls=len(controls),Ghidra_compared_regions=sum(x['checked_regions'] for x in controls),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        original_vtables=len(proof['blocks']),exact_container_edits=410,Shot_files=82,Trap_files=82,Shot_records=5551,Trap_records=1293,
        preserved_prior_components=149,preserved_source_files=420,preserved_EXE_paths=7,new_opaque_field_names=0,source_writes=0)
    return dict(phase=172,date='2026-11-02',families=['Shots','Traps','Creatures','Variables'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,new_selected_pairs=['P7/D7','P34/D2','P34/D7'],
            mixed_pairs_without_selected_composition_scenes=[],next='Wholefirstblockcoverageandremainingboundaryaudit; do notswitchCryptuntilthatresult.',next_block='Crypt groups'),
        physical_sources=refs,unchanged_source_Shot_inputs=unchanged,
        level_table=dict(file=str(gv),sha256=sha(gv.read_bytes()),type=65,record_count=288,raw_records_sha256=sha(b''.join(raw_levels)),callback=0x51DC40,
            loading='EveryXPsceneexecutesall288originalcallbackswithunchanged16Bsourcepayloadsafterthestate/category/animationinitializers,beforeanyHP/XPconsumer. Preparedprefix16Bonlymatchesnativecallbackrecordstructure.',
            stat1='Healthpercent: level2=125,3=150,9=400,10=600.',stat13='XPthresholdpercent: level2=130,3=180,9=850,10=1000.'),
        XP=dict(process=7,callback=0x4AE630,
            input='LiveCreaturetargetviaShot147; unsignedData1>1 addsonecurrentlevelXPthreshold,Data1=0/1subtractsthatthreshold. Data2notreadbythiscallback. ItdoesnotinterpretData1asthenumberoflevels.',
            threshold='RawCreatureF00 ->runtime6B5. Atlevel1readbaseXP; higherlevelusesSIGNEDtype65/stat13percentage,clampto0..65534. InselectedCreature8baseXP3000,level2threshold3900.',
            add='457880addsXP;reachingthresholdcallswhole48B120onceandthenresetsXP0. Level10doesnotincreasebutthesameXPresetoccursinselectedscene. Subtractsaturates0withoutdecreasinglevel.',
            HP='48B120preservesHPfractionusingQ12integerdivisionbeforemultiplication:newHP=floor(floor(oldHP*4096/oldMaxHP)*newMaxHP/4096). OriginalgenericHPmethodappliesdifference;thenindependentDamage7runs.',
            selected=dict(data1=50,oldLevel=1,oldHP1000=dict(level=2,level_HP_request=249,normal_HP_request=-50,finalHP=1199),
                fullHP2500=dict(level=2,level_HP_request=625,normal_HP_request=-50,finalHP=3075),
                unsignedFFFFFFFF=dict(level=2,finalHP=1249,normal_hit=False),selector2=dict(level=2,finalHP=1247),
                selector1Level2XP5000=dict(level=2,XP=1100,finalHP=999),level9HP10000=dict(level=10,finalHP=14950),
                invulnerable445=dict(level=2,finalHP=3125,normal_hit=False)),
            source_mask='WholeunchangedShot182("levelup")hasProcessFlags1. Completecreation/collision/wholefirsttickdoesnotinvokeXPcallbackandonlyordinaryDamage7applies50. Matchedmask4diagnosticsprovethepositiveXPcompositionwithoutchangingthesourcefile.',
            events='Tickevent2withnotyetselectedtargetreturns0andaborts; firstevent64withnotyetselectedtargetreturns0butcontinues. Collisionevent4return0ignored,soordinaryhitstillruns.'),
        Instant=dict(process=34,callback=0x4AFCB0,
            positive='ValidnonzeroShot147/liveentity/notkind9: original4B5560(targetXYZ)thenMOV[Shot14D]=1at4AFCFF;returns1. Originalcollisiontakesforcedtargetbranch,clearstheflag,dispatchesevent4andindependentordinarydamage.',
            parameter_independence='InstantcallbackreadsneitherData1norData2,butordinaryD2/D7useboth/sharedparameters. TargetedfirecopyiesdiagnosticTrapData1/2intoinstance; TrapID17avoidsJack16tag-as-radiusoverride.',
            missing_target='Null/missingtargetcallsoriginalShotvslot20(HP0)andreturns1. Thealreadyenteredwholetickstillrunsordinaryarea. NativeShotHPmayendFFFFFFFFafterdecay,notactualdeallocation.',
            selected=dict(Data1=80,Data2=2,HP1000_targeted_area=[920,930],no_target_sameXY=[920,930],targeted_onecell_away=[920,930],
                no_target_onecell_away=[960,985],repeat_targeted_twoTicks=[840,860],invulnerable_first=[1000,930],
                source_area=dict(ShotID=58,Data1=10,Data2=3,HP=[990,991]),source_hit=dict(ShotID=89,Data1=0,HP=[1000,1000])),
            repeat='P34/D7sourceFlags126hmarksShotHP0oncollisionanddecaysittoFFFFFFFF;nextwholeShotcalldoesnotexecuteanotherhit. ExplicitdiagnosticFlags4controlkeepsShotpositiveandallowstwohits80.',
            boundary='Shot58/89rawinputsunchangedinsourcecases;Trap17isexplicitCPUcopyofsourceTrap16withID/ShotID/ShotDatachanged. ThisdoesnotclaimthestoredgameTrap17orCreatureSpellactuallyfiresthemthatway. Selectedtargetprovidedtooriginalfiringwrapper;naturalacquisitionschedulernotrun.'),
        event_evidence=event_evidence(),
        fixture=dict(prepared='ValidActor/World/celllist/NavGrid/alerts/pools/free-slotandexternalresourceprovidersfromfrozen170/171. InstantneedsnoCreaturestateentry;XPtargetsusevalidcurrentArt/self/Worldcontext.',
            copied_inputs='Allsourcefilesunchanged. Shot/TrapdiagnosticsonlyCPUcopies. Type65payloadsunmodified; noopaquephysicalfieldrenamed.',
            lowpage='Allreads/writesbelow4096rejectedexceptDWORD FS:[0]; OSreadprobeinvalidcounter0inInstantchains.',
            excluded='EarlierzerolevelscalesandmissingArt/targetdiscoveryarenotacceptedproof; current32scenesuseactualtablesandexplicitvalidtargetcontrols.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=storage,preservation=baseline,
        helpers=[link(n) for n in ['probe_shot_instant_experience.py','emulate_shot_instant_experience.py','crosscheck_shot_instant_experience.py','prepare_shot_instant_primary.py','validate_shot_instant_storage.py','create_shot_instant_helpers.py']],
        limitations=['ExactprovidedDXSHA;otherEXEs/patchDLLsnotcoveredautomatically.',
            'SelectedordinaryCreaturetargets,positiveboundedradiusesandatmosttwowholeticks;noeverytargetclass/overflow/deathcleanupclaim.',
            'InstantfactoryloadsunusedJack/Keeper4asfrozenfixturecontext;noJackfactoryrunsinthisphase.',
            'Sourcechildren/completeWorldscheduler/Guidedflight/Jack273subsequentbehaviorremainexplicitboundariesofthefirstblocksummary.',
            'All14observedmixedpairsnowhavecompositionevidence,notallsourcevariantsandeventmasks. Firstblockstillawaitsconsolidatedaudit;goalremainsactive.'])

def documents(s):
    n=s['counts']
    text=f'''# Опыт и instant-hit в сочетании с обычным уроном

Этап172, 2 ноября2026. Закрыты положительными сценами последние три смешанные пары:7/7,34/2,34/7. Первый блок пока открыт до общей сверки покрытия; к Crypt ещё не перешли. Машинная схема `shot-instant-experience-semantics.json` содержит SHA конкретного DKII-DX.exe и все доказательства.

## Process7: опыт, уровень, HP и следующий удар

Data1 сравнивается как unsigned DWORD. Значения0 и1 снимают один порог опыта текущего уровня; любое значение больше1 добавляет такой порог. Это не количество уровней. Data2 специальный обработчик не читает, но обычный урон продолжает использовать общие параметры.

В каждой сцене выполнены оригинальные загрузчики всех288 неизменённых записей type65 из GlobalVariables.kwd. Для выбранного Creature8 базовый максимумHP2500 и порогXP3000. На втором уровне процентHP125, а процент порогаXP130 — следующий порог3900.

Добавление порога через457880 вызывает целую48B120 один раз и обнуляет XP. Уровень10 остаётся10. Снятие опыта ограничено нулём и само по себе не понижает уровень. Проверенный контроль уровня2 с XP5000 и Data1=1 далXP1100 и тот же уровень2.

Повышение сначала пересчитывает HP с двумя округлениями Q12:

`newHP = floor(floor(oldHP * 4096 / oldMaxHP) * newMaxHP / 4096)`

Затем выполняется отдельный DamageType7. В одном столкновении при Data1=50:

| Начальные условия | HP после повышения | Обычный удар | ИтогHP |
|---|---:|---:|---:|
| Уровень1, HP1000 из2500 | 1249 | −50 | 1199 |
| Уровень1, HP2500 из2500 | 3125 | −50 | 3075 |
| То же, таймер445=1 | 3125 | Пропущен | 3125 |
| Data1=FFFFFFFFh, HP1000 | 1249 | Signed-неположительное значение пропущено | 1249 |

Поэтому один и тот же DWORD может запустить положительное действие XP и одновременно оказаться отрицательным для проверки обычного урона. Data1=2 также повышает на один уровень; обычный удар тогда равен2.

## Почему исходный «level up» не повышает в проверенной цепочке

Неизменённая запись Shot182 из локальной карты имеет ProcessFlags=1. Найденные Ghidra пять прямых вызовов dispatcher передают64,2,8,128 и4; маски1 среди них нет. В полном создании/столкновении/первом Shot tick с этой исходной записью Process7 не вызван: уровень остаётся1, а Damage7 уменьшаетHP1000→950.

Положительная парная сцена меняет только копию маски на4 и получает реальное повышение до обычного удара. Исходник не переписан. Вывод ограничен проверенным EXE, найденными прямыми вызовами и исполненными цепочками; он не доказывает отсутствие неизвестных косвенных вызовов или изменений сторонней DLL.

При маске2 специальный callback приходит до выбора цели, возвращает0 и прерывает тик до столкновения. При маске64 нулевой возврат также возможен без выбранной цели, но первый вызов64 его игнорирует и обычный удар позже происходит.

## Process34: заранее выбранная цель и принудительное столкновение

Теперь цель Shot147 устанавливает оригинальная targeted-стрельба ловушки до полного Shot tick. Для этого в памяти CPU использована явно диагностическая копия Trap16 с ID17, новым Shot ID и ShotData. ID17 позволяет проверить общий firing-wrapper без особого правила Jack16, которое заменяет Data2 на target tag. Это не утверждение о настоящей записи Trap17 или параметрах CreatureSpell в карте.

Если tag ненулевой, сущность жива в registry и её kind не9, Process34 вызывает оригинальную4B5560 с координатами цели, затем пишет Shot14D=1. Оригинальный collision принимает принудительную цель и сбрасывает флаг. Далее независимо выполняется обычный урон.

Для Data1=80/Data2=2 целый тик далHP920/930 двум целям. При переносе обеих целей на клетку дальше результат тот же: центр области перенесён к выбранной цели. У неизменённого Shot58 Data1=10/Data2=3 результатHP990/991. При Damage7 диагностический Data1=80 даёт один запрос−80 выбранной цели.

**Без цели обработчик обнуляет HP снаряда, возвращает1, а уже начатый тик продолжает обычную область.** В парном контроле с целями на клетку дальше область остаётся в прежней позиции: HP960/985 вместо920/930. После decay HP снаряда может статьFFFFFFFFh; полное удаление/освобождение объекта здесь не выполнялось.

У исходного Shot89 Data1=0. В проверенной цепочке обычного урона нет, но это не доказывает бесполезность настоящего заклинания: wrapper заклинания или ловушки может подставить своё значение. Его исходные Flags126h обнуляют HP при столкновении; второй целый tick уже не наносит удар. Диагностический контроль с Flags4 сохраняет положительный HP и подтверждает два удара80 за два тика.

## Проверки и сохранность

Приняты32 native-сцены:16XP и16Instant. Выполнены {n['complete_Shot_ticks']} полных Shot tick, {n['complete_collision_calls']} collision, {n['whole_targeted_firing']} целых targeted-fire и {n['whole_level_up_calls']} целых level-up. В16XP-сценах оригинальный callback загрузил суммарно4608 записей из288 неизменённых type65.

Десять независимых Ghidra-контролей сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных адресов покрыты точными первичными инструкциями: {n['additional_function_extents']} дополнительных и {n['inherited_function_extents']} унаследованных диапазонов, пять vtable. Tail-jump к площадному обработчику покрыт прежним проверенным диапазоном этапа131. Все обращения к низкой странице, кроме DWORD FS:[0], отвергаются.

410 точных пересборок82 Shots и82 Traps выполнены в памяти с обратным восстановлением и сохранностью остальных байтов. SHA420 текущих исходников и7 путейEXE сверены с прежним baseline; четыре исторических изменения карт в нём сохранены. Пакет150 компонентов, прежние149 и codec сохранены. Исходники игры и параллельный редактор не менялись.

У всех14 смешанных пар локального корпуса теперь есть сцены совместного выполнения. Это не покрытие каждой карты, каждого события, любого класса цели и всех переполнений. Следующий шаг того же первого блока — единая таблица доказательств, оставшихся границ и влияния параметров после firing-wrapper. Общая цель остаётся активной, экспертная оценка около75%.
'''
    (ROOT/'SHOT_INSTANT_EXPERIENCE_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап172: пакет150 компонентов. P7/D7: реальные288 записей роста → XP/level-up/HP → обычный удар; источникShot182 mask1 не вызываетXP в проверенных цепочках. '
        'P34/D2,D7: original targeted-fire задаёт цель, Instant переносит Shot/ставит принудительный collision; без цели HP0 не прерывает уже начатую обычную область. '
        f'32native/10Ghidra, {n["Ghidra_compared_regions"]} областей/{n["Ghidra_compared_bytes"]} байт;{n["additional_function_extents"]} дополнительных/{n["inherited_function_extents"]} унаследованных диапазонов,{n["unique_executed_original_PCs"]}PC;410 пересборок. '
        'Прежние149 компонентов/codec/420источников/7EXE сохранены. Все14 смешанных пар имеют выбранные сцены; итоговый аудит первого блока ещё открыт, Crypt отложен. См.`SHOT_INSTANT_EXPERIENCE_RU.md`.\n\n')
    file=ROOT/'INTEGRATION_RU.md';txt=file.read_text(encoding='utf-8')
    if 'Актуальный этап172:' not in txt[:1700]:
        first,rest=txt.split('\n\n',1);file.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    file=ROOT/'READINESS_RU.md';txt=file.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 172'):
        _,rest=txt.split('\n',1);file.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 172\n\n'+note+'Общая экспертная оценка около75%; первый блок ждёт итогового аудита.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase172-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==149
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==149 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['instant_experience_extension']=dict(source=link(SCHEMA),block_complete=False,
        rule='P7usesunsignedData1selectorandrealleveltables;levelupHPbeforeDamage7. P34movesShottooriginalselectedtargetandforcescollision;nulltargetmarksHP0butreturns1allowingordinaryarea. RawsourceXPmask1unmatchedinidentifieddirectcallers.',
        selected_pairs_added=['7/7','34/2','34/7'],remaining_mixed_pairs_without_selected_scenes=[],next='ConsolidatedfirstblockauditbeforeCrypt')
    package['effective_shot_parameters']=effective
    note='shot_instant_experience_semantics phase172:32native/10wholeGhidra;XP7real288type65tables->level/HP->Damage7,unsignedselector0/1subtracts,>1addsoneXPthreshold;sourceShot182mask1notmatchedbyidentifieddirectcallers. Instant34originaltargetedfire->targetXYZ/forcedcollision;missingtargetHP0return1stillallowsordinaryarea;sourceShot58/89anddiagnosticTrap17explicit. Prior149components/codec/420sourcebaseline/7EXEpreserved;410exactedits. All14mixedpairshaveselectedcompositionevidence;firstblockawaitscoverage/boundaryaudit.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase172-search-status.json',dict(phase=172,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Publishedandauditedphase171;phase172discoveryestablishedpositiveXPwithrealleveltablesandpretargetedInstant.'),
        counts=s['counts'],remaining=s['priority'],priority_order=['ProcessType/DamageType','Crypt groups','Global parameters','Creature resistances/flags','Other unknowns'],
        readiness='Expertestimateabout75%,notmeasuredbytecoverage.',evidence=[link(SCHEMA),link('SHOT_INSTANT_EXPERIENCE_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
