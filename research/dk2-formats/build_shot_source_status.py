"""Publish source-status composition and a bounded first-block evidence matrix."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_shot_source_status as native
import crosscheck_shot_source_status as cross
import prepare_shot_source_status_primary as primary
import validate_shot_source_status_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_source_status_semantics'
SCHEMA='shot-source-status-semantics.json'
MATRIX='shot-combination-coverage.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())
STAGES={168:'shot-combination-semantics.json',169:'shot-area-combination-semantics.json',170:'jack-in-box-semantics.json',
    171:'shot-target-combination-semantics.json',172:'shot-instant-experience-semantics.json',173:'guided-clock-semantics.json',174:'shot-child-chain-semantics.json'}
BINDINGS={
 (7,7):[(172,'xp-collision-level1'),(172,'xp-source-mask1')],
 (9,7):[(171,'lightning-hit'),(171,'source-stun97')],
 (11,7):[(168,'freeze-and-hit'),(175,'wave-first'),(175,'wall-source')],
 (17,2):[(169,'wind-range'),(169,'source-wind-zero'),(170,'wind-real-AI')],
 (28,7):[(171,'source-guided35'),(173,'velocity-clock-104'),(173,'velocity-clock-105')],
 (29,1):[(171,'deckem-area-collision'),(174,'full-selected-chain')],
 (29,7):[(171,'deckem-hit'),(171,'source-reaper52')],
 (30,1):[(169,'source-inferno-constant'),(174,'inferno-first')],
 (30,2):[(169,'source-inferno-start'),(169,'source-inferno-range')],
 (31,2):[(169,'source-gas-fart'),(169,'source-gas-missile'),(169,'source-gas-trap')],
 (34,2):[(172,'instant-source-area'),(172,'instant-area-moved'),(172,'instant-area-no-target')],
 (34,7):[(172,'instant-source-hit-zero'),(172,'instant-hit-live-repeat')],
 (38,7):[(168,'poison-and-hit-tick'),(175,'poison85'),(175,'poison88'),(175,'poison95'),(175,'poison85-alternate')],
 (39,2):[(170,'hostile-start'),(170,'fired-source64-tag2'),(170,'hostile-capacity448')],
}
RULES={
 (7,7):'XP selector runs before independent hit; level HP is rescaled with Q12 truncation. Actual source mask1 does not invoke XP in accepted creation/collision/tick paths.',
 (9,7):'Own Lightning damage, then native state255, then independent hit with changed HP coefficient. ExtraFlags1 gates Lightning, not independent hit.',
 (11,7):'Freeze timer is installed before independent hit; selected coefficient160/128. Source162 also creates164 and later163 with inherited parameters.',
 (17,2):'Wind uses Data1 as its own radius and Data2 as percentage; ordinary area uses Data1 as damage and Data2 as radius. Valid AI permits state210.',
 (28,7):'Guided does not consume Data1/2. Velocity refresh uses Session clock&7==0, not RNG; independent hit is not clock gated.',
 (29,1):'Collision state255 affects subsequent normal area; event64 zero ignored, event2 zero aborts. Source152 child graph forwards runtime Data1/2.',
 (29,7):'State255 is entered before independent hit and doubles its negative HP delta in selected Creature context.',
 (30,1):'Inferno own damage and independent constant area both execute; their target and invulnerability filters differ.',
 (30,2):'Inferno own damage and independent range area both execute; normal range retains the shared attenuation accumulator.',
 (31,2):'Nonzero DamageType bypasses Gas own Creature/RNG traversal; ordinary range damage still executes.',
 (34,2):'Instant moves to preselected target and forces collision; ordinary range area then uses effective parameters. Missing target deletion request does not cancel an already-entered area tick.',
 (34,7):'Instant forced collision permits ordinary hit. Source Data1=0 requests no hit damage; later repetition depends on flags/HP and target context.',
 (38,7):'Low16 Data1/2 set poison timer/severity before independent signed-positive hit. Creature poison tick is a separate damage path.',
 (39,2):'Jack own hit precedes real Creature46 factory and ordinary area; timer445 does not gate the own hit. Trap16 Data2 becomes target tag and also normal area radius.',
}

def coverage():
    inventory=load('phase168-shot-combinations-storage.json')
    stages={n:load(f) for n,f in STAGES.items()}
    manifests={n:d['native_executions'] for n,d in stages.items()};manifests[175]=load('phase175-source-status-emulations.json')
    # Existing immutable schemas and selected trace files must still match.
    package=load('editor-format-package.json')
    for n,file in STAGES.items():
        assert any(ref==link(file) for ref in package['components'].values()),(n,file)
    rows=[];processes={r['process_type']:r for r in load('shot-semantics.json')['processes']}
    damage=load('shot-semantics.json')['damage_types']
    for row in inventory['observed_pairs']:
        pair=(row['ProcessType'],row['DamageType']);refs=[]
        for phase,name in BINDINGS.get(pair,[]):
            scene=next(r for r in manifests[phase]['replays'] if r['case']['name']==name)
            assert sha((ROOT/scene['file']).read_bytes())==scene['sha256']
            refs.append(dict(phase=phase,case=name,trace=dict(file=scene['file'],sha256=scene['sha256']),
                source_schema=STAGES.get(phase,SCHEMA)))
        rows.append(dict(row,process_name=processes[pair[0]]['name'],damage_name=damage[str(pair[1])]['name'],
            selected_composition_executions=refs,all_source_variants_exhaustively_executed=False,
            scope='Selected composition proof' if refs else 'Inventory plus earlier individual-consumer evidence; no new mixed-pair trace binding here',
            rule=RULES.get(pair,'See individual process/damage components; no claim of new whole-composition execution.')))
    mixed=[r for r in rows if r['ProcessType'] and r['DamageType']]
    assert {(r['ProcessType'],r['DamageType']) for r in mixed}==set(BINDINGS)
    assert len(rows)==49 and len(mixed)==14 and all(r['selected_composition_executions'] for r in mixed)
    assert sum(r['source_records'] for r in rows)==5551
    return dict(phase=175,date='2026-11-03',profile=stages[174]['profile'],source=link('phase168-shot-combinations-storage.json'),
        counts=dict(Shot_files=82,Shot_records=5551,observed_pairs=49,mixed_pairs=14,mixed_pairs_with_selected_composition_evidence=14,
            mixed_source_variants=sum(len(r['variants']) for r in mixed),native_scenes_current_block=sum(len(m['replays']) for m in manifests.values()),
            independent_Ghidra_controls_current_block=sum(d['counts']['independent_Ghidra_controls'] for d in stages.values())+6),
        source_scope=inventory['source_scope'],rows=rows,block_complete=False,
        evidence_levels=['Original file inventory','Original static code/bytes','Selected execution of original instructions on prepared context','Whole World/live-game behavior remains a separate, unproved level'],
        ordinary_damage_modes=dict(source=link('shot-semantics.json'),rows=damage,
            refinements=[link('shot-damage-semantics.json'),link('area-damage-semantics.json'),link('shot-lifecycle-semantics.json'),link('tremor-damage-semantics.json')],
            warning='Use later explicit refinements: D2 order-dependent accumulator; D3 definition Data2 SAR12; D4 filled expanding square; D5 first tick; D6 separate terminal callback with exact-zero World723B gate; D8 terrain.'),
        event_contract=dict(source=link(STAGES[172]),identified_direct_masks=[64,2,8,128,4],
            first_tick='D5 -> event64 (return ignored) -> positive ShotHP gate -> event2 (zero aborts) -> collision event4 (zero ignored) -> D1/2/3/4/8 dispatcher -> later event8 conditional gate -> decay/death event128.',
            qualification='Event8 is conditional and its zero return aborts later tick work. Event128 belongs to the death-child path, not a synonym for terminal callback4AAF70.',
            mask1='No mask1 among five identified direct dispatcher CALL sites; sourceXP182 mask1 inactive in accepted scenes. No universal absence/indirect-call/patch claim.',
            mask256='No event256 among these five direct sites. Broader field consumers and every mask variant are not exhausted.'),
        parameter_sources=[dict(stage='Raw',fields='Data1/2 raw98/9C -> definition37/3B -> instance13D/141',scope='Physical DWORD preservation; semantic signedness depends on consumer.'),
            dict(stage='CreatureSpell/Trap fire',rule='Original wrappers can overwrite Shot defaults; accepted diagnostic wrappers do not prove every stored caster uses the same values.',sources=[link(STAGES[171]),link(STAGES[170])]),
            dict(stage='HitThing/Death child',rule='Child defaults load first, then parent full Data1/2 overwrite instance; definition/raw stay unchanged.',sources=[link(STAGES[174])])],
        correction_precedence=[dict(old=171,new=173,topic='Guided clock gate replaces historical RNG description'),
            dict(old=169,new=170,topic='Valid AI context proves Wind210/Jack273; old unchanged-state fixture is bounded negative evidence')],
        remaining=[dict(priority=1,topic='P29/D7 source mask0x10C event8 and child graph',reason='Selected P29/D7 collision is proven, but source Chainfire variants include event8; its zero return can skip later decay. Must compose the actual source graph and source flags.'),
            dict(priority=2,topic='Other source masks/variants and parent parameter overrides',reason='14/14 means selected pairs, not every source variant, every target class, or all event interleavings.'),
            dict(priority=3,topic='World scheduling/terminal cleanup and version transfer',reason='No full scheduler, real-time DPS, pool-free, all target deaths, or automatic RC3/old EXE/patch-DLL transfer.')],
        next_block='Crypt groups only after first-block boundary decision; global phase167 remains parked.')

def build():
    manifest=load('phase175-source-status-emulations.json');assert len(manifest['replays'])==15
    traces=[];sources={}
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(file.read_bytes()) and row['file']==str(file.relative_to(ROOT))
        traces.append(saved['result']);source,ids,raws=native.probe.source(c)
        for ID,raw in raws.items():
            sources[(str(source),ID)]=dict(file=str(source),file_sha256=sha(source.read_bytes()),ID=ID,name=native.probe.old.target.fmt.string(raw[:32]),record_sha256=sha(raw),
                ProcessType=raw[0xC7],DamageType=raw[0xC5],Data1=native.u(raw,0x98),Data2=native.u(raw,0x9C),HitThingShotId=raw[0xC4],DeathShotId=raw[0xBF])
    controls=load('phase175-source-status-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase175-source-status-primary.json')
    stored=storage.validate();assert stored==load('phase175-source-status-storage.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase175-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=15,complete_Shot_ticks=visit(0x4AB180),whole_Shot_factories=visit(0x4B7CD0),whole_targeted_firing=visit(0x4A7FD0),
        Freeze_callbacks=visit(0x4AE1D0),Poison_callbacks=visit(0x4B0270),unique_source_records=len(sources),
        unchanged_source_record_inputs=sum(len(native.probe.source(c)[1]) for c in native.cases()),independent_Ghidra_controls=6,
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtables=5,
        exact_container_edits=stored['exact_container_edits'],Shot_files=82,Shot_records=5551,preserved_prior_components=152,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    matrix=coverage();assert matrix==load(MATRIX)
    return dict(phase=175,date='2026-11-03',families=['Shots','Traps'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,next=matrix['remaining'][0],next_block='Crypt groups'),
        source_records=list(sources.values()),
        Crystal=dict(graph=[dict(parent=162,event='HitThing',child=164),dict(parent=162,event='Death',child=163),dict(parent=163,event='HitThing',child=164)],
            parent='Original162 Data60/2 freezes1200 atTPS20 before independent hit60; selected HP coefficient160/128 makes actual delta75.',
            hit_child='Raw164 own50/0 becomes runtime60/2 after factory. Each selected whole child tick requests60; frozen target loses75 and other enemy60. Three ticks end childHP3->0.',
            death_child='Eight parent ticks create163. Its own rawData0/2 becomes runtime60/2. In selected death position its following whole tick has no event4 and no new hit; do not infer a damage event merely from inherited parameters.',
            standalone_wall='Original163 fired alone keeps0/2: freeze0, no positive direct hit; its164 child inherits0/2 and requests zero area deltas in selected next tick.',
            invulnerable='Timer445 suppresses normal parent/child damage for the selected Creature, but does not prevent freeze1200. Other enemy still loses60 per selected child tick.',
            Trap='DiagnosticTrap17 override80/2 yields freeze1600 and actual100+100 parent/child deltas; override0/2 yields no freeze/damage. Raw162/163/164 remain unchanged.'),
        Poison=dict(records=[85,88,95],alternate85='Provided kop3 source variant also executed unchanged.',
            result='Each unchanged raw source uses50/2: timer50, severity2 and independent hit50. Timer445 prevents normal hit but poison50/2 still applies.',
            lifetime='Source85/88 end accepted first tick with ShotHP0; source95 endsFFFFFFFF after original deletion request and decay. Not a physical free/idempotence claim.',
            boundary='No new poison Creature tick here; original complete tick and armor/invulnerability composition already phase168.'),
        fixture=dict(source='Original complete raw loaders, valid original AI/currentArt context, sorted definitions, original targeted fire and native child factories.',
            HP='Explicit prepared100000 avoids lethal cleanup; not a legal game maximum claim.',
            wrapper='Trap17 is a declared CPU copy of original Trap16 with ID/ShotID/ShotData set; not a claim about a stored firing association.',
            scheduler='Host selects complete object methods. No full World interleaving, Creature recovery, real-time DPS, terminal frees, live graphics/audio.',
            memory='Actual copy instructions, registry/tag/pointer/ID, loaded default checksums, runtime values and all guards verified; low page only DWORD FS:[0]; original read-probe invalid0.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=stored,preservation=baseline,coverage_audit=link(MATRIX),
        helpers=[link(n) for n in ['probe_shot_source_status.py','emulate_shot_source_status.py','crosscheck_shot_source_status.py','prepare_shot_source_status_primary.py','validate_shot_source_status_storage.py']],
        limitations=['Exact provided DX SHA only; no automatic RC3/old/normal EXE or DLL-hook transfer.',
            'Actual source graph plus selected object ticks, not all source mask/child variants. First priority block remains open for the concrete event8/source Chainfire check.',
            'Raw defaults differ from effective wrapper/parent values; all modified wrapper values explicitly diagnostic.',
            'No new opaque physical field named; previous152 components/codec/current baseline preserved.'])

def documents(s,m):
    n=s['counts']
    text=f'''# Исходные Freeze/Poison и цепочка Crystal Wave

Этап175, 3 ноября2026. Первый блок ProcessType/DamageType остаётся открыт. Новый результат проверяет неизменённые записи из ваших карт, дополняя диагностические сочетания этапа168.

## Crystal Wave и дети

Исходный `NeedsTestGlobals.kld`:162 Crystal Wave — P11/D7, Data60/2, Health8, HitThing164, Death163. У163 Crystal Wall исходные Data0/2, Health20, HitThing164. У164 Frost Wave Damage Boost — P0/D1, Data50/0, Health3. Это предоставленная модифицированная карта, не утверждение о штатном контенте Bullfrog.

Целый первый tick162 создаёт164, затем накладывает заморозку1200 приTPS20 и вызывает обычный hit60. Уже действующая заморозка меняет коэффициент HP на160/128: цель теряет75. У созданного164 собственные50/0 после фабрики заменены родительскими60/2. Его один полный tick наносит75 замороженной цели и60 другой вражеской; союзная цель пропускается. Три выбранных тика164 дают суммарно225/180 и уменьшают егоHealth3→0.

Восемь полных тиков родителя естественно создают163 с60/2 вместо собственных0/2. В принятой сцене место смерти родителя уже отличается от места первоначального попадания; следующий tick этого163 не исполняет event4 и не наносит новый hit. Наличие параметров60/2 само по себе не гарантирует новое столкновение. Создание, координаты и события остаются отдельными условиями.

Контроль со163, выпущенным самостоятельно, сохраняет0/2: заморозка0, положительного прямого hit нет, ребёнок164 наследует0/2 и в следующем tick передаёт нулевые изменения HP. Это отличает собственные defaults от наследования по цепочке.

Таймер445 у цели блокирует обычные parent/child повреждения, но не установку заморозки. Диагностическая Trap17 подстановка80/2 даёт таймер1600 и два фактических повреждения100 — parent и один child tick. Подстановка0/2 обнуляет оба эффекта. Записи162/163/164 не редактировались.

## Poison

Целиком загружены и выполнены85 Elven Poison Arrow,88 Poison Slab,95 Fog Poison изAutoSaveGlobals и отдельный85 Poison Arrow изkop3Globals. Все используют исходные50/2: ставят таймер яда50, силу2 и наносят независимый прямой удар50. При таймере445 прямой удар пропускается, а яд50/2 устанавливается.

Здесь не запускается последующий Creature tick: его отдельное повреждение и взаимодействие с бронёй/445 уже проверены этапом168. Значение50 — сырые тики таймера яда, не50 секунд; у Freeze параметр60 преобразуется черезTPS.

У85/88 HP снаряда после принятого первого tick равно0; у95 исходные flags приводят кFFFFFFFF после запроса удаления и уменьшения HP. Это результат конкретного whole-tick пути, не утверждение об освобождении памяти или повторном исполнении планировщиком.

## Доказательства

15 принятых native-сцен, {n['complete_Shot_ticks']} целых Shot tick, {n['whole_Shot_factories']} настоящих фабрик, {n['whole_targeted_firing']} targeted-fire. Семь различных исходных записей из трёх контейнеров; в сценах они загружены {n['unchanged_source_record_inputs']} раз без изменения raw. Проверены фактические MOV наследования, tag/registry/ID/definition, defaults и instance-параметры, checksums и guard-байты.

Шесть независимых Ghidra CPU-контролей сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных исходных PC покрыты {n['inherited_function_extents']} ранее полученными точными диапазонами; новых функций не создавалось. Пять исходных vtable проверены. Любые обращения ниже4096 отвергаются, кроме DWORD FS:[0]; invalid read-probe=0.

246 точных пересборок82 Shot-контейнеров проверяют ProcessType/DamageType/Data1/Data2/HitThingShotId/DeathShotId, обратное восстановление и неизменность остальных байтов. Сохранены152 прежних компонентов, codec, SHA420 текущих источников и7 путейEXE. Исходные игровые файлы и параллельный редактор не менялись.

Контекст явно подготовлен, HP100000 диагностический. Порядок полных методов выбран стендом; нет полного World/Creature scheduler, выхода из заморозки, живого DPS, смерти целей и физического освобождения объектов. Выводы относятся к предоставленному DKII-DX.exe с SHA из машинной схемы.

Машинная схема: `shot-source-status-semantics.json`. Итоговая таблица текущего покрытия: `SHOT_COMBINATION_COVERAGE_RU.md`. Следующий конкретный пробел того же блока — исходные Chainfire P29/D7 с mask0x10C: надо выполнить event8 вместе с их flags/детьми, поскольку нулевой возврат event8 может пропускать дальнейшее уменьшениеHP. До этой проверки Crypt не начинаем.
'''
    (ROOT/'SHOT_SOURCE_STATUS_RU.md').write_text(text,encoding='utf-8')
    text='''# ProcessType / DamageType: сводка покрытия

Этап175, 3 ноября2026. Это сводка доказательств, не процент готовности всех форматов. В82 предоставленных контейнерах5551 запись,49 различных пар,14 пар с обоими ненулевыми типами. Корпус включает карты/модификации.

У14/14 смешанных пар есть выбранное исполнение композиции оригинальных инструкций. В этапах168–175 суммарно188 принятых native-сцен и56 независимых Ghidra CPU-контролей. Это не188 непересекающихся игровых механик и не проверка всех исходных вариантов. У XP7 исходная mask1 в проверенных путях не вызывает процесс; положительная композиция доказана отдельной mask4 диагностикой.

| Process/Damage | Записей | Проверенные сцены | Ключевое правило |
|---|---:|---|---|
'''
    ru={(7,7):'XP и изменение HP уровня перед обычным hit; source mask1 не срабатывает.',(9,7):'Собственная молния → state255 → усиленный обычный hit.',
        (11,7):'Заморозка до hit; настоящая Crystal-цепочка передаёт детям параметры.',(17,2):'У процесса и обычной области разные смыслы общих Data1/Data2.',
        (28,7):'Обновление скорости по Session clock&7; обычный hit независим.',(29,1):'State255 до области; исходная цепочка152→153/158→49.',
        (29,7):'State255 усиливает обычный hit; source event8 ещё требует проверки.',(30,1):'Собственный Inferno + обычная константная область с разными фильтрами.',
        (30,2):'Собственный Inferno + обычная область с общим накопителем ослабления.',(31,2):'Ненулевой DamageType пропускает собственный обход Gas.',
        (34,2):'Перемещение/forced collision до обычной области.',(34,7):'Forced collision; sourceData1=0 не наносит hit.',
        (38,7):'Яд low16 плюс отдельный signed-positive прямой удар.',(39,2):'Собственный удар → Jack46 → обычная область; Trap16 подставляет tag вData2.'}
    for row in m['rows']:
        pair=(row['ProcessType'],row['DamageType'])
        if pair not in BINDINGS:continue
        labels=', '.join(f'{x[0]}:{x[1]}' for x in BINDINGS[pair])
        text+=f'| {pair[0]}/{pair[1]} | {row["source_records"]} | {labels} | {ru[pair]} |\n'
    text+='''
## Общий порядок и параметры

Первый tick: D5 → процесс64 (возврат игнорируется) → проверка положительногоHP снаряда → процесс2 (ноль прерывает) → collision/процесс4 (ноль не отменяет обычный hit7) → обычная область → условный процесс8 (ноль прерывает дальнейший tick) → дальнейшее уменьшениеHP/создание death-child/процесс128. Отдельный terminal callback4AAF70 и D6 не равны событию128.

Выявлены пять прямых CALL dispatcher с масками64,2,8,128,4. Среди них нет1 или256; это не универсальное доказательство отсутствия других/косвенных потребителей или перехватов DLL.

RawData1/2 — DWORD98/9C; definition37/3B; instance13D/141. Семантика и signedness зависят от обоих обработчиков. CreatureSpell/Trap могут заменить defaults; после child factory родитель также перезаписывает оба instance DWORD. Исходные записи и загруженные defaults от этого не меняются.

D0 не отключает Process. D1 — константная область; D2 — область с порядкозависимым общим накопителем ослабления. D3 масштабирует повреждение текущимHP и читает definitionData2 SAR12. D4 использует instanceData2 SAR12 и заполненный расширяющийся квадрат, не полое кольцо. D5 — один раз в первом tick. D6 — отдельное завершение при точном нуле World723B. D7 — обычный hit. D8 — повреждение terrain; параметры и геометрия подтверждены отдельно. В исходном Shot-корпусе обнаружены D0/1/2/7; прочие поддерживаемые режимы проверялись диагностически.

Поздние исправления имеют приоритет: этап173 заменяет прежнюю формулировку RNG для Guided на Session clock; этап170 показывает положительный Wind210/Jack273 с корректным AI-контекстом, не отменяя границы старых отрицательных сцен169.

## Что остаётся открытым

1. Конкретные исходные Chainfire P29/D7 сmask0x10C и дочерними Shots. При event8 нулевой возврат может пропустить дальнейший decay — прежняя проверка одного collision этого не покрывает.
2. Все варианты масок/дочерних графов и значений из49 пар. Полный перечень с именами, ID, путями и параметрами сохранён в `shot-combination-coverage.json`. Варианты одной пары нельзя автоматически считать эквивалентными.
3. Полный World scheduler, Creature recovery, terminal cleanup/pool-free, все классы целей/смерти/переполнения и перенос на другие EXE. Это явные границы; выбранный native tick не равен живому игровому балансу.

Первый блок остаётся открыт; следующий исполняемый шаг — пункт1. Crypt второй в очереди, глобальные параметры третий, резисты/флаги четвёртый. Общая оценка около75% остаётся экспертной, не измеренной долей байтов. Исходники и результаты предыдущих этапов сохранены.
'''
    (ROOT/'SHOT_COMBINATION_COVERAGE_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап175: пакет153 компонентов. Неизменённые Crystal162/163/164 и Poison85/88/95/alternate85 выполнены; Freeze60→1200ticks→hit75, child16450/0→60/2; Poison50/2+hit50. '
        f'15native/{n["complete_Shot_ticks"]}wholeShot-tick/6Ghidra,{n["Ghidra_compared_regions"]}областей/{n["Ghidra_compared_bytes"]}байт; {n["unique_executed_original_PCs"]}PC/231прежнийдиапазон;246пересборок. '
        'Матрица49пар/14смешанных с выбранными доказательствами14/14; не все варианты. Первый блок открыт: следующий sourceChainfire P29/D7 event8/mask0x10C. См.`SHOT_SOURCE_STATUS_RU.md` и `SHOT_COMBINATION_COVERAGE_RU.md`.\n\n')
    file=ROOT/'INTEGRATION_RU.md';txt=file.read_text(encoding='utf-8')
    if 'Актуальный этап175:' not in txt[:1800]:
        first,rest=txt.split('\n\n',1);file.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    file=ROOT/'READINESS_RU.md';txt=file.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 3 ноября 2026 — после этапа 175'):
        _,rest=txt.split('\n',1);file.write_text('# Оценка готовности на 3 ноября 2026 — после этапа 175\n\n'+note+'Экспертная оценка около75%; первый блок открыт.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase175-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==152
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),effective_shot_parameters=deepcopy(package['effective_shot_parameters']),
            codec=link('dk2_formats.py'),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);matrix=coverage();dump(MATRIX,matrix);s=build();dump(SCHEMA,s)
    assert len(old['components'])==152 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['source_status_extension']=dict(source=link(SCHEMA),coverage=link(MATRIX),block_complete=False,
        rule='Whole unchanged Crystal162/163/164 and Poison85/88/95 sources confirm shared parameters and child overwrites; consolidated49pairs/14mixed has selected14/14proof, not every variant. Next actual Chainfire P29/D7 event8/mask0x10C remains open.')
    package['effective_shot_parameters']=effective
    note='shot_source_status_semantics phase175:15native/6Ghidra;unchangedCrystal162freeze60->1200ticks/direct75,child164defaults50/0->runtime60/2;death163defaults0/2->60/2. UnchangedPoison85/88/95/alternate85timer50/severity2+hit50;445blocksordinarynotstatus. Firstblockcoverage49pairs/14mixed selected14/14,188native/56Ghidra across168..175;notallvariants. NextsourceChainfireevent8/0x10C,blockopen. Prior152components/codec/baselinepreserved.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s,matrix)
    dump('phase175-search-status.json',dict(phase=175,date='2026-11-03',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Final phase174 audit passed; consolidated inventory exposed unexecuted unchanged Freeze/Poison and Crystal children; executed and independently crosschecked.'),
        counts=s['counts'],coverage_counts=matrix['counts'],remaining=matrix['remaining'],evidence=[link(SCHEMA),link(MATRIX),link('SHOT_SOURCE_STATUS_RU.md'),link('SHOT_COMBINATION_COVERAGE_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
