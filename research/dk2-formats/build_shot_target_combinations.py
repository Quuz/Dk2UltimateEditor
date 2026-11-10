"""Publish phase171 target reaction / ordinary-damage composition evidence."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_shot_target_combinations as native
import crosscheck_shot_target_combinations as cross
import prepare_shot_target_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='shot_target_combination_semantics'
SCHEMA='shot-target-combination-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase171-target-emulations.json');assert len(manifest['replays'])==32
    traces=[];raw_sources=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert c==row['case'] and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes()) and row['outcome']==out
        traces.append(saved['result'])
        if c.get('source_record'):
            q=c['source_record'];file=Path(q['file']);raw=next(s.records[q['record_index']] for s in native.probe.fmt.parse(file.read_bytes()) if s.signature==260)
            assert checks[native.p.shot.RAW]==raw
            assert not any(raw[o] for o in [0xBF,0xC1,0xC2,0xC3,0xC4]),c['name']
            raw_sources.append(dict(case=c['name'],file=str(file),file_sha256=sha(file.read_bytes()),record_index=q['record_index'],ID=raw[0xBE],name=native.probe.fmt.string(raw[:32]),record_sha256=sha(raw),unchanged=True))
    controls=load('phase171-target-crosschecks.json')['replays'];assert [x['case'] for x in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase171-target-primary.json')
    storage=load('phase171-target-storage.json');assert storage['new_exact_container_edits']==328 and storage['source_writes']==0
    baseline=load('phase165-source-baseline.json')
    for row in baseline['source_files']+baseline['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=load('phase171-prior-package-state.json')
    for row in previous['components'].values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=32,complete_Shot_ticks=visit(0x4AB180),complete_collision_calls=visit(0x4ABD60),
        Lightning_callbacks=visit(0x4AF1E0),Deckem_callbacks=visit(0x4AF610),Guided_callbacks=visit(0x4AF3A0),unchanged_source_Shot_records=3,
        independent_Ghidra_controls=8,Ghidra_compared_regions=sum(x['checked_regions'] for x in controls),Ghidra_compared_bytes=sum(x['checked_bytes'] for x in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        original_vtables=len(proof['blocks']),exact_container_edits=328,Shot_files=82,Shot_records=5551,preserved_prior_components=148,
        preserved_source_files=420,preserved_EXE_paths=7,source_writes=0,new_opaque_field_names=0)
    return dict(phase=171,date='2026-11-02',families=['Shots','Creatures'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='ProcessType/DamageType',block_complete=False,selected_pairs_added=['P9/D7','P28/D7','P29/D1','P29/D7'],
            mixed_pairs_without_accepted_positive_scenes=['P7/D7','P34/D2','P34/D7'],next_block='Crypt groups'),
        physical_sources=raw_sources,
        collision_order=dict(callback_event4=0x4ABFC1,normal_damage7=0x4ABFDE,process_return_ignored=True,
            whole_tick='First64, then tick2 return gate, then collision4, then ordinary area dispatcher. Callback0 at collision does not cancel ordinary damage.'),
        Lightning=dict(process=9,callback=0x4AF1E0,
            direct_damage='Valid Creature target without definition68A bit1: generic HP4B2CE0(-abs(signed32(Data1))), with 32-bit doubling before abs when selected Terrain+17B has80. Tested small80/-80/0 only; no overflow generalization.',
            immunity='RawCreature1545 bit1 -> runtimeExtraFlags68A bit1 skips Process9 damage AND subsequent reaction; does not gate independent DamageType7.',
            reaction='After its HP request, Process9 sets targetWORD4E3=20. With selected ordinary AI/category/flags it calls491830(255), which enters currentstate25E=255 before HIT_THING.',
            amplification='Generic HP helper4B2CE0 calls48F350. State D1/FF/100/101 returns1 and adds128 to damage coefficient, whose baseline is128. Newly entered255 doubles subsequent ordinary damage in the same collision. Process9 own first request occurred before this state entry.',
            selected=dict(Data1=80,HP_start=1000,ordinary=dict(requests=[-80,-80],HP=760,state=255),
                conductive=dict(requests=[-160,-80],HP=680),immuneExtra1=dict(requests=[-80],HP=920,state=1),
                invulnerable445=dict(requests=[-80],HP=920,state=255),armour443=dict(requests=[-80,-80],HP=840),
                no_AI_tables=dict(requests=[-80,-80],HP=840,state=1)),
            important='A generic HP request is not proof that HP changed. PrimaryCreatureFlag4000 blocks both negative requests in HP helper; ExtraFlags bit1 is a separate Process9 gate. Timer445 is checked by ordinary hit, not by Process9 own selected damage branch.'),
        Deckem=dict(process=29,callback=0x4AF610,
            gates='Target is live Creature, instance3E6 bit200000 clear and definition686 bit4000 clear. Then491830(255). Callback reads neither Data1 norData2; normal damage still reads them.',
            selected_hit='Data1=80: actual state255 entry before HIT_THING doubles its one -80 request to HP delta-160. Mask miss or rejected transition leavesHP920 rather than840. Timer445 still blocks ordinary hit after successful state entry.',
            selected_area='With event4, complete collision sets selected target state255 before CONSTANT ordinaryarea: HP840 for selected target,HP920 for nearby secondtarget. Same80 rawdamage is scaled by currentstate independently.',
            start_event='With event64 and no target selected yet, no state entry, callback returns0 but first64 ignoresit; collision and ordinaryarea still execute,HP920/920.',
            tick_event='With event2 and no target selectedyet, callback returns0; completeShot tick returns before collision/ordinaryarea/decay. HP1000/1000, targettag0. Does not prove a pretargeted variant behavesidentically.'),
        Guided=dict(process=28,callback=0x4AF3A0,
            selected='Original callback returns1 and ordinaryDamage7 still executes after it. Originalwhole rawShot35GuidedBolt Data1=100 givesHP900 in preparedcollision.',
            boundary='This is callback/HP/event composition evidence. RNG-dependent velocity update, complete trigonometric table initialization, varied flight angles, interception and natural flight scheduler are not established by these scenes.'),
        fixture=dict(loaders='Full rawCreature,rawShot and diagnosticCreatureSpell loaders; originalCreatureSpellwrapper selectsactualcollision target; originalcollision/tick execute unchanged.',
            parameters='Unchanged sourceShot records are used inthreecases; diagnosticCreatureSpellShotID/Data1/Data2 are explicitly set to that sourceShot so wrapper overrides reproduce its parameters. This does not prove the sourceCreatureSpell uses those values.',
            AI='Original47BBE0/4798F0/4950C0 initialize state/category/animationtables. Actorctx256/25A refer to self/World; currentArt6C refers to loadeddefinitionArt1 atdefinition+27. No source/rawArt field edited.',
            rejected_discovery='Earlier nullcurrentArt6C caused lowpage0Freadat4967BA; excluded. Earlier XP gain with uninitialized level scaling and instant-hit with missing/late target are discoveryonly, notaccepted positive evidence.',
            source_children='Selectedthree sourceShots havezero childShotIDs; source152 P29/D1 childchain remainsunaccepted. DiagnosticP29/D1 hasnochildren.',
            boundaries='Prepared32x32map,Creature1 snapshots,validemptyalertlists,nullresourceIO,one/twoenemies; no subsequentCreature255tick/recovery or completeWorldscheduler. Extra target isexplicitcopiedCreatureincludingarmour/445.',
            lowpage='Allreads/writes below4096 mustbe DWORD FS:[0]; allothers reject.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,storage_validation=storage,preservation=baseline,
        helpers=[link(n) for n in ['probe_shot_target_combinations.py','emulate_shot_target_combinations.py','crosscheck_shot_target_combinations.py','prepare_shot_target_primary.py','validate_shot_target_storage.py','create_shot_target_helpers.py']],
        limitations=['ExactprovidedDXSHA; notautomaticallyRC3/1.3/otherEXEs.',
            'Onlyboundednewselectedpairs; priorphases168/169/170retained. Three mixedpairs stillneedpositivecontextchecks; firstblocknotclosed.',
            'P28flight/RNGbranches, XPlevelscales, instanttargetselection, variedtargetclasses andsourcechildchains remainexplicitboundaries.',
            'No engine/source/editorcodewrites;148priorcomponents preserved. Historical4mapdrifts inphase165baseline retained.'])

def documents(s):
    n=s['counts'];doc=f'''# Сочетания Lightning, Deckem и Guided с обычным уроном

Этап171, 2 ноября2026. Продолжается первый блок ProcessType/DamageType; перехода к Crypt нет. Проверен конкретный предоставленный DKII-DX.exe, SHA и трассы — в `shot-target-combination-semantics.json`.

## Молния: отдельный урон и реакция перед вторым ударом

Process9 сначала вызывает общий метод HP с `-abs(Data1)`. На проверенном признаке Terrain+17B bit80 перед abs значение удваивается. Затем обработчик ставит WORD Creature4E3=20 и при подходящем состоянии/флагах вызывает `491830(255)`. С оригинальными AI-таблицами текущий state25E действительно становится255.

Обычный DamageType7 вызывается после этого. Общий метод HP проверяет `48F350`: состояния209,255,256,257 добавляют128 к базовому коэффициенту128. Поэтому только что вошедшее состояние255 удваивает последующий удар уже в том же столкновении.

| Подготовленные условия, Data1=80, HP1000 | Запросы HP | Итог HP | State |
|---|---|---:|---:|
| Process9 + Damage7 | −80, −80 | 760 | 255 |
| Признак Terrain удвоения | −160, −80 | 680 | 255 |
| ExtraFlags bit1 защиты от молнии | −80 обычный | 920 | 1 |
| Таймер неуязвимости445=1 | −80 специальный | 920 | 255 |
| Броня443=2 | −80, −80 | 840 | 255 |
| Контроль без AI-таблиц | −80, −80 | 840 | 1 |

Это объясняет, почему сумму запросов нельзя просто вычесть из HP. Броня и текущий state меняют масштаб внутри общего метода. При первичном флаге Creature4000 метод получает оба запроса, но HP сохраняется1000.

**Защита ExtraFlags bit1 относится к специальному Process9.** Она пропускает его собственный урон и реакцию255; независимый DamageType7 остаётся. Таймер445, наоборот, в выбранной сцене блокирует обычный удар, но Process9 наносит урон. Это проверка сочетания, а не завершённый разбор всех резистов/флагов.

Неизменённый rawShot97 `Stun` из локальной карты с Data1=50 далHP850: сначала−50, затем усиленный обычный−100. Фактические CreatureSpell-параметры этой карты здесь не восстанавливались: диагностический wrapper явно получает значения данного Shot.

## Deckem меняет результат обычного урона

Process29 сам не читает Data1/Data2 и не вызывает HP. Он проверяет Creature, instance3E6 bit200000 и первичный флаг4000, затем пытается войти в255. При успешном входе обычный Damage7 с Data1=80 уменьшает HP на160. Неизменённый rawShot52 `Reaper Fireball` с Data1=50 далHP900 в выбранном столкновении.

Для DamageType1 выполнен целый Shot tick с двумя целями. Внутри него **столкновение стоит до обычного площадного dispatcher**. На event4 Deckem успевает поставить255 только выбранной цели: HP840 противHP920 у второй цели при одинаковом запросе−80.

При маске64 первый callback исполняется ещё до выбора цели. Он возвращает0, но первый вызов64 игнорирует возврат: обычная область затем даётHP920/920. При маске2 callback также ещё не имеет цели и возвращает0, а этот возврат уже прекращает tick до столкновения/области/decay: HP1000/1000. Здесь нельзя смешивать «процесс не выбрал цель» и «обычный урон отключён типом процесса».

## Guided и границы выполненных сцен

Process28 в выбранном столкновении возвращает1, после него обычный Damage7 выполняется. Неизменённый rawShot35 `Guided Bolt`, Data1=100, далHP900. Дальнейший полёт, обновление скорости в случайной ветви, полная инициализация тригонометрических таблиц и перехват движущейся цели этим не проверены.

В accepted-набор не включены ранние пробы XP с нулевыми коэффициентами следующего уровня и instant-hit без действительной заранее выбранной цели. Исходный P7/D7 имеет маску1, поэтому один только вызов столкновения не доказывает смену уровня. У P34 важен момент появления targettag; эти вопросы остаются в первом блоке.

В трёх принятых sourceShot нет дочерних Shot ID. Карта с P29/D1 и дочерними снарядами ещё требует отдельной цепочки; её ранняя сцена с отсутствующими определениями детей не считается доказательством всей цепи.

## Проверки

Приняты {n['native_scenes']} native-сцены, {n['complete_Shot_ticks']} полных Shot tick и {n['complete_collision_calls']} полных вызовов collision. Восемь независимых исполнений Ghidra сравнили {n['Ghidra_compared_regions']} областей / {n['Ghidra_compared_bytes']} байт. Все {n['unique_executed_original_PCs']} исполненных адресов оригинала покрыты первичными инструкциями: {n['additional_function_extents']} дополнительных / {n['inherited_function_extents']} унаследованных диапазонов и четыре vtable. Это не означает выполнение всех ветвей этих функций.

Контекст включает настоящие AI-таблицы, Actor/World и указатель текущего ArtResource. Все обращения ниже4096 отвергаются, кроме DWORD FS:[0]. Ранняя проба с nullArt6C читала0Fh и исключена; её остановка не объявляется ошибкой игры.

328 точных пересборок82 Shot-контейнеров в памяти проверили четыре сочетания, обратное восстановление и сохранность неизвестных байтов. SHA420 текущих источников и7 путей EXE проверены по прежнему baseline; четыре исторических изменения карт сохранены в его записи. Пакет149 компонентов, прежние148 и codec сохранены. Исходники игры и параллельный редактор не менялись.

Следующие положительные сцены: P7/D7, P34/D2 и P34/D7. Далее — итоговая сводка событий/параметров/классов целей и границ совместимости первого блока. До её завершения к Crypt не переходим. Общая экспертная оценка около75%, не измеренное покрытие байтов.
'''
    (ROOT/'SHOT_TARGET_COMBINATIONS_RU.md').write_text(doc,encoding='utf-8')
    note=(f'Актуальный этап171: пакет149 компонентов. Lightning9: собственный урон → state255 → усиленный обычныйDamage7; ExtraFlags bit1 блокирует только специальную часть. '
        'Deckem29 меняет state до обычного hit/area; event2 с нулевым возвратом обрывает tick, event64 возврат игнорирует. Guided28 проверен в выбранном столкновении. '
        f'32native/8Ghidra, {n["Ghidra_compared_regions"]} областей/{n["Ghidra_compared_bytes"]} байт;7 дополнительных/157 унаследованных диапазонов,12082PC;328 точных пересборок. '
        'Прежние148 компонентов/codec/420источников/7EXE сохранены. Положительные сцены P7/D7,P34/D2,P34/D7 и итог блока ещё открыты; Crypt отложен. См.`SHOT_TARGET_COMBINATIONS_RU.md`.\n\n')
    file=ROOT/'INTEGRATION_RU.md';txt=file.read_text(encoding='utf-8')
    if 'Актуальный этап171:' not in txt[:1600]:
        first,rest=txt.split('\n\n',1);file.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    file=ROOT/'READINESS_RU.md';txt=file.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 2 ноября 2026 — после этапа 171'):
        _,rest=txt.split('\n',1);file.write_text('# Оценка готовности на 2 ноября 2026 — после этапа 171\n\n'+note+'Общая экспертная оценка около75%; первый блок открыт.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');file=ROOT/'phase171-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==148
        dump(file.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),
            effective_shot_parameters=deepcopy(package['effective_shot_parameters']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','effective_shot_parameters','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==148 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    effective=deepcopy(old['effective_shot_parameters'])
    effective['combination_rules']['target_reaction_extension']=dict(source=link(SCHEMA),block_complete=False,
        rule='Lightning/Deckem may enter255 before ordinary damage; HPhelper adds128 coefficient forstate255. ExtraFlagsbit1 gates Process9 only. Collisionprecedesnormalarea inwholeShot tick.',
        selected_pairs_added=['9/7','28/7','29/1','29/7'],remaining_positive_scene_pairs=['7/7','34/2','34/7'])
    package['effective_shot_parameters']=effective
    note='shot_target_combination_semantics phase171:32native/8wholeGhidra;P9ownHP->state255->amplifiedDamage7;ExtraFlagsbit1onlygatesProcess9. Deckemstate255beforehit/area;wholeShotcollisionbeforeordinaryarea;event2zeroabortswhileevent64zeroignored. Guidedselectedcollision;3unchangedsourceShots. Prior148components/codec/420sourcebaseline/7EXEpreserved;328exactedits. Priorityblock1remainsopen:P7/D7,P34/D2,P34/D7positivecontextsandfinalscopeaudit.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase171-search-status.json',dict(phase=171,date='2026-11-02',goal_status='active',block='ProcessType/DamageType',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase170 hostileJack/firing evidencepublished;phase171 validates remainingtargetreactionpairswithoutchangingpriority.'),
        counts=s['counts'],remaining=s['priority'],priority_order=['ProcessType/DamageType','Crypt groups','Global parameters','Creature resistances/flags','Other unknowns'],
        readiness='Expert estimate staysabout75%; notmeasuredbytecoverage.',evidence=[link(SCHEMA),link('SHOT_TARGET_COMBINATIONS_RU.md'),link('RESEARCH_PRIORITIES_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
