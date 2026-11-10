"""Publish actual child-Shot parameter inheritance and selected complete chain."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_shot_child_chain as native
import crosscheck_shot_child_chain as cross
import prepare_shot_child_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_child_chain_semantics'
SCHEMA='shot-child-chain-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase174-child-chain-emulations.json');assert len(manifest['replays'])==9
    source=native.probe.SOURCE;raws={r[0xBE]:r for s in native.probe.old.target.fmt.parse(source.read_bytes()) if s.signature==260 for r in s.records}
    refs=[]
    for ID in native.probe.IDS:
        raw=raws[ID];refs.append(dict(ID=ID,name=native.probe.old.target.fmt.string(raw[:32]),record_sha256=sha(raw),
            ProcessType=raw[0xC7],DamageType=raw[0xC5],Data1=native.u(raw,0x98),Data2=native.u(raw,0x9C),mask=native.u(raw,0xA0),Flags=native.u(raw,0xA8),Health=native.u(raw,0xBC,2),
            HitThingShotId=raw[0xC4],DeathShotId=raw[0xBF]))
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert c==row['case'] and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        assert checks[native.probe.RAWS]==b''.join(raws[i] for i in native.probe.IDS)
        traces.append(saved['result'])
    controls=load('phase174-child-chain-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase174-child-chain-primary.json')
    storage=load('phase174-child-chain-storage.json');assert storage['exact_container_edits']==246 and storage['source_writes']==0
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase174-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=9,complete_Shot_ticks=visit(0x4AB180),complete_collision_calls=visit(0x4ABD60),whole_Shot_factories=visit(0x4B7CD0),
        original_Deckem_callbacks=visit(0x4AF610),original_Inferno_callbacks=visit(0x4AF660),whole_targeted_firing=visit(0x4A7FD0),
        source_definitions=4,unchanged_source_record_inputs=36,independent_Ghidra_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtables=5,
        exact_container_edits=246,Shot_files=82,Shot_records=5551,preserved_prior_components=151,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    return dict(phase=174,date='2026-11-02',families=['Shots','Traps'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,next='Consolidatedfirstblockmatrix/event/parameter/versionboundaryaudit.',next_block='Crypt groups'),
        source=dict(file=str(source),sha256=sha(source.read_bytes()),records=refs,scope='Localmap/modcontainer,notcertifiedshippedBullfrogcatalogue.'),
        graph=[dict(parent=152,event='HitThing',child=153),dict(parent=152,event='Death',child=158),dict(parent=158,event='Death',child=49)],
        inheritance=dict(order='FullchildShotfactory/initializerfirstloadsitsowndefaults,thenparentoverwritesinstance13D/141. Childdefinition/rawremainunchanged.',
            hit=dict(factory_call=0x4ABF54,Data1_write=0x4ABF7D,Data2_write=0x4ABF83,process_event4_after_copy=0x4ABFC1),
            death=dict(Data1_write=0x4AB61B,Data2_write=0x4AB621,event128_after_copy=0x4AB662),
            forwarded='WholeDWORD Data1/2 pluscastertag145,targettag147,positionvector131/135/139 andmarker151arecopiedbyinspectedpaths. Acceptedchildrenallowner7/caster0/target2.',
            defaults='Raw153and158Data0/2becomeinstance50/3. Raw49Data100/1becomesinstance50/3through152->158->49. Loadeddefinitiondefaultsremain0/2,0/2,100/1.',
            upstream='DiagnosticTrap17Data80/2overridesparentraw50/3atfiringandthenallcreatedchildreninherit80/2;originalfourrawShotrecordsarenotchanged.'),
        execution=dict(parent='Originalraw152P29/D1,Health8: selectedfirstcollisioncreatesone153beforeDeckemevent4; wholeparentticks1..8dodecayandordinaryarea. At8itcreates158.',
            hit_child='Selectedcreated153P0/D1executes1or4wholeticks; inheritedData50/3causesrealordinarydamagealthoughrawData1is0. At4itsHPbecomes0,withnochildShot.',
            wait='Created158P0/D0hasHealth8;selected7ticksdo_not_create49, eighthcreates49withoutdamagingtargets.',
            inferno='Actuallycreated49P30/D1receivesinherited50/3insteadofraw100/1;firstfulltickexecutesspecialInfernoandindependentordinaryarea.',
            HP='Preparedfriendly/enemy1/enemy2start100000each. Parent8+child153four+wait158eight+Inferno49onetickyield99950/98600/99300. Data80/2overrideyields99920/97760/98880.',
            invulnerable='Timer445onselectedenemy1blocksordinaryparent/153/Infernoarea;specialInfernoownrequeststillapplieswithretainedstate255coefficient2. SelectedfullchainHP99950/99900/99300.',
            ordering_boundary='Thesearenaturallycreatednativechildrenandnaturalper-objectlifetimedecay,butthehostchooseswhichwholeShotmethodtorunnext. Worldinterleaving,clockadvance,Creature255recoveryandterminalcleanupnotexecuted. ExperimentalHPisnotlivegameDPS.',
            count_boundary='OnlyoneHitThing153wascreatedintheselectedparent8-ticktrace;donotinferonechildeverytick. RelativeWorldShotcount+36andrealregistry/pooladdressesarechecked.'),
        lookup=dict(native=[0x50E2D0,0x50E1E0],rule='PrepareddefinitionsareID-sorted49,152,153,158. Lookupstartsmin(ID-1,count-1),walksbackandstopswhenstoredID<requestedID. Unsortedsubsetisnotavalidnativecataloguecontext.',
            evidence=[link('ghidra/phase174-order-50e2d0-assembly.txt'),link('ghidra/phase174-order-50e1e0-assembly.txt')]),
        fixture=dict(prepared='FouractualsourceRawrecords/fullconstructors/converters/loaders,diagnosticTrap17templatedfrom16,originalfiring,wholeShotfactories,16spare512Barenablocks,validAI/currentArt/World/NavGrid/resources.',
            source_HP='HP100000isexplicitdiagnosticpreparedvalueonunchangedclasssnapshots,notclaimvalidnormalmaxHP. ActualCreaturedeathisavoided;negativeHP cleanupnotinthischain.',
            source_changes='No originalfileschanged. All36rawShotinputsverbatim;TrapID/ShotDatachangesaredeclaredCPUcopies.',
            memory='Everychildtag/pointer/ID/definition/default/runtimeparameter/copyinstruction/HP/tickchecked. AllsparearenaandMID/definitionguardschecked;lowpageonlyDWORD FS:[0];OSprobeinvalid0.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=storage,preservation=baseline,
        helpers=[link(n) for n in ['probe_shot_child_chain.py','emulate_shot_child_chain.py','crosscheck_shot_child_chain.py','prepare_shot_child_primary.py','validate_shot_child_storage.py','create_shot_child_helpers.py']],
        limitations=['ExactprovidedDXSHAandoneactualmap/modchildgraph;othergraphs,eventtypes,cyclic/failedfactoriesnotautomaticallycovered.',
            'NoWorldscheduler/Creaturestate255recovery/livesound/graphics/death-poolfree. Actualchildrencreated,butselectedsubsequencenotnaturalgameinterleaving.',
            'Onechild153inselectedparenttrace;notallcollisioneventbranches. Primaryfullrangesdonotproveallbranches.',
            'Firstblockstillrequiresfinalscopeaudit;151priorcomponents/codec/420baseline/7EXEpreserved.'])

def documents(s):
    n=s['counts'];text=f'''# Дочерние Shots: наследование параметров и целая цепочка

Этап174, 2 ноября2026. Дополнительная проверка блока ProcessType/DamageType перед общим итогом. Проверен предоставленный DKII-DX.exe и реальный граф из локального `NeedsTestGlobals.kld`; это карта/модификация, а не утверждение о штатном каталоге Bullfrog.

## Исходный граф

| ShotID | Process/Damage | Data1/Data2 в файле | Health | Дочерний Shot |
|---:|---|---|---:|---|
| 152 | Deckem29 / Constant1 | 50/3 | 8 | HitThing153, Death158 |
| 153 | None0 / Constant1 | 0/2 | 4 | Нет |
| 158 | None0 / None0 | 0/2 | 8 | Death49 |
| 49 | Inferno30 / Constant1 | 100/1 | 32 | Нет |

Выполнены оригинальные loader/factory, полный targeted-fire диагностической ловушки и целые Shot tick. Все четыре исходные записи Shots побайтно неизменены. Определения отсортированы49,152,153,158: настоящий native lookup идёт назад и рассчитывает на порядок ID.

## Когда перезаписываются параметры

При попадании152 создаёт153 через настоящую4B7CD0. Сначала инициализатор153 загружает его собственные0/2, затем родительские MOV по4ABF7D/4ABF83 заменяют поля экземпляра13D/141 на50/3. Эта операция происходит **до Process29 события4 родителя**.

При истечении152 создаётся158. Код4AB61B/4AB621 также копирует родительские50/3. Через восемь собственных тиков158 создаёт49 и теми же инструкциями передаёт50/3 дальше. В определении49 по-прежнему100/1, но созданный экземпляр реально использует50/3.

Наследуются также caster tag, target tag, вектор цели и связанный marker. В проверенной цепочке владелец7, caster0, target2. Реальные записи параметров наблюдаются в трассах; проверка не ограничивается конечным совпадением байтов.

Отдельный контроль задаёт диагностической Trap17 ShotData80/2. Оригинальная стрельба сначала подставляет их родительскому152, после чего153,158 и49 получают80/2. Исходные значения четырёх Shots не редактируются.

Это правило важно для редактора: **значение в файле дочернего Shot и эффективное значение при таком создании различаются**. Поле Data1=0 у153 здесь не означает отсутствие урона; оно перезаписано до первого тика ребёнка. Аналогично собственный радиус49=1 заменён унаследованным3.

## Выполненные участки

1. Первый целый тик152 создаёт один153 и выполняет Deckem/обычную область.
2. Восемь тиков152 естественно уменьшают егоHP8→0 и создают158.
3. Созданный153 отдельно проходит один либо четыре собственных тика и наносит унаследованный обычный урон. На четвёртомHP4→0.
4. Семь тиков158 ещё не создают49; восьмой создаёт его через оригинальную фабрику.
5. Первый целый тик реально созданного49 выполняет Inferno и обычную область с унаследованными параметрами.

В исходном8-тиковом parent trace появился только один153. Из этого нельзя выводить создание ребёнка на каждом тике.

## Контрольные HP и граница модели

Для изоляции цепочки от смерти три настоящих class snapshot получили явно диагностическийHP100000. Без Creature recovery tick выбранная цель сохраняет state255, который удваивает отрицательные HP-изменения. Последовательность parent8 + child153four + wait158eight + Inferno49one дала:

| Условия | Союзная цель | Выбранная вражеская | Вторая вражеская |
|---|---:|---:|---:|
| Родитель/дети50/3 | 99950 | 98600 | 99300 |
| Trap подставляет80/2 | 99920 | 97760 | 98880 |
| Таймер445 у выбранной,50/3 | 99950 | 99900 | 99300 |

Союзная цель получает специальный Inferno, но обычная область её пропускает. Таймер445 блокирует обычный урон выбранной цели; собственный Inferno в проверенной ветке всё равно вызывает метод HP.

**Эти числа описывают выбранную последовательность целых native-методов, а не DPS в живой игре.** Дети реально созданы, их таймеры уменьшаются исходным кодом, но порядок следующих вызовов задан стендом. Полный World scheduler, продвижение Session clock, Creature ticks/выход из255, живой renderer и окончательное освобождение памяти здесь не выполнялись. Объекты сHP0 ещё зарегистрированы в конце сцены.

## Доказательства и сохранность

Девять принятых native-сцен выполнили {n['complete_Shot_ticks']} целых Shot tick, {n['whole_Shot_factories']} настоящих Shot-фабрик и {n['whole_targeted_firing']} целых targeted-fire. Четыре исходные записи загружены суммарно36 раз. Проверены tag/pointer/ID, начальные defaults и перезаписанные instance-параметры, счётчики, HP, сроки жизни, конкретные MOV и guard-байты.

Пять независимых Ghidra-контролей сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных адресов покрыты {n['inherited_function_extents']} прежними точными диапазонами и пятью vtable; новых функций не создавалось. Все обращения ниже4096 отвергаются, кроме DWORD FS:[0]; OS read-probe не обнаружил недопустимых чтений.

246 точных пересборок82 Shot-контейнеров проверили HitThingShotId/DeathShotId/Data1/Data2, обратное восстановление и сохранность неизвестных байтов. SHA420 текущих источников и7 путейEXE совпали с baseline. Пакет152 компонентов, прежние151 и codec сохранены. Игровые файлы и параллельный редактор не менялись.

Машинная схема — `shot-child-chain-semantics.json`. Следующий шаг того же блока — единая таблица всех найденных сочетаний, событий, перезаписей параметров и оставшихся границ. До этого Crypt остаётся вторым в очереди. Общая цель активна.
'''
    (ROOT/'SHOT_CHILD_CHAIN_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап174: пакет152 компонентов. Исходная цепочка152→HitThing153/Death158→Death49 реально создана и выполнена; '
        'Data1/2 родителя заменяют defaults детей после factory, включая0/2→50/3 и Inferno100/1→50/3. Trap80/2 передаются всей цепочке. '
        f'9native/{n["complete_Shot_ticks"]} полныхShot-tick/5Ghidra,{n["Ghidra_compared_regions"]} областей/{n["Ghidra_compared_bytes"]} байт;0 новых/{n["inherited_function_extents"]} прежних диапазонов,{n["unique_executed_original_PCs"]}PC;246 пересборок. '
        'Прежние151 компонентов/codec/420источников/7EXE сохранены. Выбранные вызовы без World/Creature scheduler; первый блок ждёт итоговой таблицы, Crypt отложен. См.`SHOT_CHILD_CHAIN_RU.md`.\n\n')
    file=ROOT/'INTEGRATION_RU.md';txt=file.read_text(encoding='utf-8')
    if 'Актуальный этап174:' not in txt[:1600]:
        first,rest=txt.split('\n\n',1);file.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    file=ROOT/'READINESS_RU.md';txt=file.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 174'):
        _,rest=txt.split('\n',1);file.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 174\n\n'+note+'Экспертная оценка около75%; первый блок ждёт общего итога.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase174-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==151
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==151 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['child_chain_extension']=dict(source=link(SCHEMA),block_complete=False,
        rule='NativeHitThing/DeathchildfactoryinitializesowndefaultsthenparentcopiesfullData1/Data2andtags. Actualsource152->153/158->49executeswith50/3;Trap80/2forwardsthroughall. Childraw/default100/1doesnotpredictcreatedInfernoinstance.')
    package['effective_shot_parameters']=effective
    note='shot_child_chain_semantics phase174:9native/122wholeShot-ticks/5wholeGhidra;actualsource152P29/D1->HitThing153,Death158->Death49P30/D1. OriginalchildfactoriesloaddefaultsbutparentMOVoverwrites13D/141with50/3orTrap80/2;definitionsunchanged. SelectedcallsequenceexplicitwithoutWorld/CreaturerecoveryDPSclaim. Prior151components/codec/420baseline/7EXEpreserved;246exactchildID/dataedits. Firstblockawaitsconsolidatedmatrixandboundaryaudit.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase174-search-status.json',dict(phase=174,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase172/173publishedandaudited;firstblockboundaryauditledtoactualsourceparent/childchainexecution.'),counts=s['counts'],remaining=s['priority'],
        evidence=[link(SCHEMA),link('SHOT_CHILD_CHAIN_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
