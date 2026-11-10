"""Publish Guided28 Sessionclock correction and positive velocity branch."""
from pathlib import Path
from copy import deepcopy
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_guided_clock as native
import crosscheck_guided_clock as cross
import prepare_guided_clock_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='guided_clock_semantics'
SCHEMA='guided-clock-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase173-guided-clock-emulations.json');assert len(manifest['replays'])==12
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd'
    raw=next(r for s in native.probe.old.fmt.parse(source.read_bytes()) if s.signature==260 for r in s.records if r[0xBE]==35)
    traces=[];unchanged=refresh=0
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert c==row['case'] and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        unchanged+=checks[native.p.shot.RAW]==raw;refresh+=out['refresh'];traces.append(saved['result'])
    controls=load('phase173-guided-clock-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase173-guided-clock-primary.json')
    blob=native.EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    checks={0x4AF573:'mov ecx, dword ptr [0x6e5050]',0x4AF579:'call 0x517490',0x4AF57E:'test al, 7',0x4AF580:'jne 0x4af5f0'}
    instruction_proof=[]
    for pc,wanted in checks.items():
        ins=next(md.disasm(native.p.pe_read(blob,pc,15),pc));actual=ins.mnemonic+' '+ins.op_str;assert actual==wanted,(hex(pc),actual,wanted)
        instruction_proof.append(dict(address=pc,text=actual,bytes=bytes(ins.bytes).hex()))
    getter=native.p.pe_read(blob,0x517490,7);assert getter.hex()=='8b41048b4004c3'
    prior=load('phase173-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=12,complete_collision_calls=visit(0x4ABD60),Guided_callbacks=visit(0x4AF3A0),positive_velocity_refreshes=refresh,
        unchanged_source_Shot35_inputs=unchanged,independent_Ghidra_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        original_vtables=4,original_trig_bytes=0x2800,new_exact_container_edits=0,preserved_prior_components=150,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    return dict(phase=173,date='2026-11-02',families=['Shots'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,next='Consolidatedfirstblockcoverage/boundaryaudit;sourcechildchainsremainauditquestion.',next_block='Crypt groups'),
        source=dict(file=str(source),sha256=sha(source.read_bytes()),ShotID=35,record_sha256=sha(raw),raw_Data1=100,raw_Data2=0,raw_ProcessFlags=4,raw_Flags=0x127,raw_Speed=2867),
        correction=dict(previous='Phase171calledtheuncheckedGuidedvelocitybranchRNG-dependent. Thattermwasincorrect; priorboundedHP/state/collisionresultswerenotinvalidated.',
            authoritative='517490returnsDWORD[[World+4]+4],Sessionclock. Guided4AF579callsitand4AF57EtestsAL&7. Velocitywritesexecuteonlywhenthosebitsare0.',
            scope='ThiscorrectsGuidedclocksourceonly. Itdoesnotgloballyrenameotherhelpers/callsortheGasPRNGpath.',
            instructions=instruction_proof,getter=dict(address=0x517490,bytes=getter.hex()),
            evidence=[link('ghidra/phase173-order-4af3a0-assembly.txt'),link('ghidra/phase173-order-517490-assembly.txt')]),
        data_flow=dict(callback=0x4AF3A0,clock='ActualpreparedSession+4readbyoriginalgetter;values100,103,104,105,112,FFFFFFF8,FFFFFFFF.',
            writes='Atmatchingclock: original4AF5E6/4AF5EA/4AF5EDwriteShot104/108/10C. WORDheadingF4isupdatedonthevalidtargetpathindependentlyofthisgate.',
            trig='ActualinitializedoverlappingPEtrigbytes6C1488..6C3C87,10240B. No recomputed math or sparse cardinal replacement. ExactbytescheckedthroughGhidraandimmutability.',
            selected='ColocatedXYandselectedCreature1Z/heightgivehorizontalangle0,verticallookup1024. SourceSpeed2867 ->runtimeWORD4B ->speed*16=45872. Matchingclockwrites[0,-45872,+45872].',
            marker='Matchedcontrolsuseoriginal4ABA10setterwith[123,456,789]beforewholecollision. Clock100/103/105/FFFFFFFFkeepsit;104/112/FFFFFFF8replacesit.',
            ordinary_hit='Guidedreturns1andDamage7stillrequests-100regardlessofclockbranch. HP1000->900. Timer445blocksordinaryhitbutdoesnotpreventvelocityrefresh;mode0retainsrefreshwithoutHPrequest.',
            source_event='UnchangedrawShot35ProcessFlags4selectsGuidedoncollision;itsFlags127hmarksShotHP0there. ThisdoesnotprovecontinuoushomingforanunmodifiedsourceShot35inflight.'),
        fixture=dict(inherited='Frozenphase171rawloaders,CreatureSpellwrapperandwholecollision;explicitactualclasssnapshots/resources. Originalvelocitysetteronlyinmarkercontrols.',
            overlays='LegacyexpandedWorldbufferaliasesitsseparateSessionregion. ClockissetintheactualSessionbufferloadedlast,andallobserved517493readsarecheckedagainsttheinputclock.',
            excluded='EarlierdiscoverywroteclockintheoverlappingWorldbufferandwasoverriddenbySession100;notacceptedpositivegateevidence.',
            scope='Preparedtargetposition/level1,onewholecollision. Nocompleteflight/originalWorldscheduler,allangles/targetclassesorcrossEXEclaims.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        storage_validation=dict(new_layout_fields=0,new_exact_edits=0,
            inherited_checks=[link('phase171-target-storage.json'),link('phase172-instant-xp-storage.json')],
            explanation='No new physicalfield/codecchange; changeddiagnosticmask/modearecoveredbypriorfrozenexactstoragecontrols. No redundantneweditrunisclaimed.'),
        helpers=[link(n) for n in ['probe_guided_clock.py','emulate_guided_clock.py','crosscheck_guided_clock.py','prepare_guided_clock_primary.py','create_guided_clock_helpers.py']],
        limitations=['ExactprovidedDXSHA,selectedcompletecollisionandlevel1targetgeometry.',
            'Time0notincluded:theCreatureSpellwrapperhasitsowncooldowngate;do notinferitsbehaviorfromthesecontrols.',
            'Firstblocknotclosedbythiscorrectionalone;sourcechildShotchainsandfinalcoverage/limitsummaryremainexplicitquestions.',
            '150priorcomponents/codec/baselinepreserved; historical171RNGtermretainedasprovenanceandcorrectedbythislatercomponent.'])

def documents(s):
    n=s['counts'];text=f'''# Guided: время обновления скорости

Этап173, 2 ноября2026. Уточнение внутри блока ProcessType/DamageType. Спецификация и доказательства — `guided-clock-semantics.json`.

## Исправление прежнего описания

В этапе171 непроверенная ветка обновления скорости Guided была названа «зависящей от RNG». Это было ошибочное название источника условия. Настоящая517490 читает DWORD Session+4 через World+4. По4AF579 Guided вызывает этот getter, а4AF57E проверяет `AL & 7`.

Скорость пересчитывается, когда младшие три бита счётчика равны нулю. Это условие времени сессии. Оно не является случайной проверкой. Ранее принятые HP и порядок столкновения сохраняются; исправлено описание этого конкретного условия. Другие PRNG-обработчики этим не переименовываются.

## Положительная ветка

Теперь используются точные10240 байт перекрывающихся таблиц направлений из исходного EXE. В старом ограниченном стенде171 была явно заданная разреженная таблица кардинальных значений; здесь она заменена полными исходными байтами и проверена через Ghidra.

Для выбранной позиции цели с теми жеXY снаряд имеет углы0 и1024 в соответствующих lookup. Speed исходного Shot35 равен2867; оригинальный loader передаёт его в WORD4B, а Guided сдвигает на4. Проверенный вектор скорости равен `[0, -45872, +45872]`.

Чтобы отличить сохранение вектора от повторной записи того же значения, перед collision оригинальный setter4ABA10 устанавливает диагностический вектор `[123,456,789]`.

| Время Session+4 | Действие Guided | Вектор после столкновения |
|---:|---|---|
| 100,103,105 | Пересчёт пропущен | 123,456,789 |
| 104,112 | Пересчёт выполнен | 0,−45872,+45872 |
| FFFFFFF8h | Пересчёт выполнен | 0,−45872,+45872 |
| FFFFFFFFh | Пересчёт пропущен | 123,456,789 |

Непосредственно наблюдаются три записи4AF5E6/4AF5EA/4AF5ED. HeadingF4 в выбранной ветке живой цели обновляется отдельно от этого условия.

## Сочетание с обычным уроном

При обоих исходах проверки Guided возвращает1, а независимый DamageType7 наносит исходные100: HP1000→900. Таймер445 блокирует обычный удар, сохраняя пересчёт скорости. При диагностическом DamageType0 скорость также пересчитывается, но HP не меняется.

Исходный Shot35 имеет ProcessFlags4 — callback вызывается на столкновении. Его Flags127h затем обнуляют HP снаряда. Здесь не доказано непрерывное самонаведение неизменённого Shot35 до столкновения: для такого вывода нужен иной момент вызова/реальный полёт. Число35 и имя GuidedBolt сами по себе этого не гарантируют.

## Проверки и границы

Приняты12 native-сцен, {n['Guided_callbacks']} вызовов Guided и {n['positive_velocity_refreshes']} положительных пересчётов. В{n['unchanged_source_Shot35_inputs']} сценах rawShot35 побайтно исходный. Пять независимых Ghidra-контролей сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных адресов покрыты {n['inherited_function_extents']} прежними точными диапазонами; новых функций для этого не создавалось. Четыре vtable и10240 байт таблиц сохранены. Ненормальные обращения ниже4096 отвергаются, кроме DWORD FS:[0].

Ранняя проба меняла значение внутри расширенного World-буфера, но отдельный Session-буфер перекрывал его исходным100. Она исключена. В принятых сценах меняется фактический Session, а все чтения оригинального getter сверяются с заданным временем. Это особенность подготовки контекста, не ошибка игры.

Новых физических полей/codec-правок нет. Сохранены прежние проверки точных пересборок этапов171/172; новые пересборки не приписываются этому этапу. SHA420 текущих источников и7 путейEXE совпали с baseline. Пакет151 компонентов; прежние150, codec и исторические различия карт сохранены.

Проверены одна выбранная геометрия цели уровня1 и полное столкновение. Время0, все углы, полный полёт/планировщик, прочие классы цели и другие EXE не обобщаются. Блок1 остаётся открытым до итоговой таблицы покрытия и проверки существенных оставшихся границ, включая дочерние Shot. К Crypt пока не переходим.
'''
    (ROOT/'GUIDED_CLOCK_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап173: пакет151 компонентов. Guided28 обновляет скорость при(SessionClock & 7)==0; прежняя пометкаRNG исправлена. '
        'Точные10240B таблиц EXE; native-setter задаёт контрольный вектор, clock100/103/105 сохраняет его,104/112/FFFFFFF8 пересчитывает; Damage7 остаётся100. '
        f'12native/5Ghidra,{n["Ghidra_compared_regions"]} областей/{n["Ghidra_compared_bytes"]} байт;0 новых/{n["inherited_function_extents"]} прежних диапазонов,{n["unique_executed_original_PCs"]}PC. '
        'Прежние150 компонентов/codec/420источников/7EXE сохранены. Первый блок ещё ждёт итогового аудита; Crypt отложен. См.`GUIDED_CLOCK_RU.md`.\n\n')
    file=ROOT/'INTEGRATION_RU.md';txt=file.read_text(encoding='utf-8')
    if 'Актуальный этап173:' not in txt[:1400]:
        first,rest=txt.split('\n\n',1);file.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    file=ROOT/'READINESS_RU.md';txt=file.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 173'):
        _,rest=txt.split('\n',1);file.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 173\n\n'+note+'Экспертная оценка около75%; первый блок открыт.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase173-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==150
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==150 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['guided_clock_correction']=dict(source=link(SCHEMA),block_complete=False,
        rule='GuidedrefreshisgatedbyactualSessionclock&7,notPRNG. PositivebranchwithrealPEtrigexecutesvelocitywrites;independentDamage7stillapplies. Earlierphase171RNGdescriptionissuperseded.')
    package['effective_shot_parameters']=effective
    note='guided_clock_semantics phase173:12native/5wholeGhidra;original517490readsSession+4,GuidedtestAL&7gatesvelocityupdates,notRNG. ExactPEtrig10240B,matchednativevelocitysettercontrolsandhighclockinputs;Damage7independent. Supersedesonlyphase171GuidedRNGterm,priorHP/orderresultsretained. Prior150components/codec/420baseline/7EXEpreserved;no newphysicalfields/edits. Firstblockstillawaitsconsolidatedscope/childchainaudit.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase173-search-status.json',dict(phase=173,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase172publishedandaudited;firstblockauditfoundandcorrectedGuidedclock/RNGmislabelwithpositivebranchtests.'),
        counts=s['counts'],remaining=s['priority'],evidence=[link(SCHEMA),link('GUIDED_CLOCK_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
