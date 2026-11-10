"""Publish nonempty Crypt cleanup proof and deeper owner-search boundaries."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_occupant_cleanup as native
import crosscheck_crypt_occupant_cleanup as cross
import prepare_crypt_occupant_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_occupant_cleanup_semantics'
SCHEMA='crypt-occupant-cleanup-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def build():
    manifest=load('phase178-crypt-cleanup-emulations.json');assert len(manifest['replays'])==24
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,checks,saved,file=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(file.read_bytes()) and row['file']==str(file.relative_to(ROOT))
        traces.append(saved['result'])
    controls=load('phase178-crypt-cleanup-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase178-crypt-cleanup-primary.json')
    before=load('phase177-stack-owner-crypt-leads.json');deep=load('phase178-deep-owner-crypt-leads.json')
    assert deep['program_sha256']==before['program_sha256']==proof['profile']['sha256']
    assert len(deep['producer_roots'])==539 and deep['root_states']==9961 and deep['limited_roots']==1 and deep['callee_summaries']==164 and deep['limited_callees']==12
    key=lambda r:(r['root']['address'],r['field'],r['read']['address'],r['read']['offset'],r['read'].get('callee'),r['read'].get('call'))
    assert {key(r) for r in deep['candidates']}=={key(r) for r in before['candidates']} and len(deep['candidates'])==33
    for row in deep['producer_roots']:assert row['producer'] in ['exact-lookup-return','entity-field-hint']
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase178-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=24,whole_Crypt_registration_setters=visit(0x4D8E60),whole_room_cleanups=visit(0x4D9570),
        whole_Creature_reset_wrappers=visit(0x48AD30),whole_room_Creature_unlinks=visit(0x4E5E00),Crypt_groups_with_nonempty_room_removal=18,
        independent_Ghidra_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_function_extents=len(proof['functions']),inherited_function_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],original_vtables=5,
        search_roots=539,search_root_states=9961,search_callee_contexts=164,search_limited_roots=1,search_limited_callees=12,
        Crypt_groups_assigned_total=8,Crypt_groups_remaining=10,new_raw_edits=0,new_opaque_field_names=0,preserved_prior_components=155,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0)
    return dict(phase=178,date='2026-11-03',families=['Creatures','Rooms'],profile=proof['profile'],counts=counts,
        priority=dict(current_block='Crypt groups',block_complete=False,next='Crypt room object list/type143 cleanup and remaining unknown indexed/indirect consumers; no global/resistance switch.'),
        setter=dict(root=0x4D8E60,old_tag_word=0x53,cleanup=0x4D9570,
            trigger='Onlynonzerooldroomtagtriggerscleanup. Emptyoldtagwithsamepreparedoccupantsdoesnotcleanlistorresetstates,evenwhennewmemberisregistered.',
            sequence='Cleanupofoldregistrationfirst,oldCreaturegroupcounterdecrementaftercleanup,thennewmembercounterincrementifreplace,andfinalroomtagwrite.',
            tested='18representativescoverall18CryptcountergroupswithtwoactualCreatureoccupants;eacholdcounter1->0. Clear/replace/owner8/validstate255controlsadditional.'),
        populated_room=dict(room_head=0xA,Creature_previous=0x236,Creature_next=0x238,Creature_room_tag=0x3B9,
            reset='Original4D9570calls48AD30(1)->476D30foreachheadCreature;originalstate1entryandroomunlinkingareexecuted. Correctpreparedprev/nextlinksallowfinitetwo-elementremoval.',
            unlink='Original4E5E00clearslinkedlistpointersandCreature3B9. Roomheadbecomes0;registeredCreaturecounterchangesareseparatefromtheoccupantlist.',
            state255='Bothoccupantsentered255throughoriginal491830beforecleanup;acceptedtracesobserve255then1writes. Witholdtag0theysurvivein255andremainlinked.',
            bookkeeping='RoomDWORD55becomes0onlywhencleanupcalled. WORD6B/6DsubtracteachremovedCreaturedefinitionBYTE672ifusage<=6B. Preparedoccupantusage1and0give100->99;standaloneemptyroomstays100.',
            identity='PreparedRoomtype26/owner7or8/tag300;registeredminimalmembertag301andnewtag302areseparatefromactualoccupanttags101/100. NotclaimautomaticroomconstructionorrealentryAI.',
            survival='Actualoccupantsremainregistered/alivewithunchangedHP;listremovalisnotCreaturedestruction.'),
        controls=dict(replace='OldMonk1095=1becomes0;newWarlock108A=1,newroomtag302. Owner8changesowner8counterswhileowner7counterbytesremainunchanged.',
            no_old='Emptyoldtag:insertincrementsnewgroupandwrites302butkeepsoccupants/head101/state255/roomtag300/room55=1234/capacity100.',
            excluded='EarlierexploratoryrootstackargumentwasnotpassedbythefrozenCPUrunner;thosefilesareexcluded. Acceptedcallplaces4D8E60withexplicitstack_argsinbefore_callsandusesargument-freeownergetterasroot. All24acceptedcallsandall6Ghidracontrolsverifythatargument.'),
        deeper_search=dict(source=link('phase178-deep-owner-crypt-leads.json'),scanner=link('scan_deep_crypt_pointers.py'),
            roots=dict(exact_owner_lookup=118,entity_field_hints=421,untyped_global_tag_roots_excluded=2612,stride_hints_excluded=20),
            limits=dict(root_states=5000,callee_states=5000,direct_call_depth=6,callee_span=16384,contexts_per_root=512),
            result='Same33counterchains,noadditional10-groupgameplayconsumer;1root/12calleelimitsremain. 421field2Ahintsarenotautomaticowneridentityproof.',
            indexed_adjuster='4D93E0index108Areadhasoneidentifieddirectcaller4D9262insideknownsetter;previousphase94alreadyprovedBYTEincrement/clamp. Existingcodewasreviewed,notcountedasnewbonus.',
            global_nonuse_proved=False),
        fixture=dict(original='FullrawCreature/Shot/CreatureSpellloadingandAIinitializersfromfrozen171,originalCreaturestateentry/roommethods,preparedvalidownerregistry/roomlists.',
            diagnostics='Room/memberrecordsandroomlinks/countersexplicitprepareddata. SourceKWDandEXEunaltered. NoobjectsinroomWORD0E;typed143objectdestructionbranchstillunexecuted.',
            lowpage='Everyread/writebelow4096mustbeDWORD FS:[0]. Fatalmissingmemory,nolazymapping. Allrawinputs/vtables/guardschecked.',
            IO='InheritedexplicitexternalresourceIOadapters;nogameplaymethods/vtablespatched. NoWorldscheduler,automaticCryptentry,fullactorfactory/roomfactoryorliveGPU/audio.'),
        native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_occupant_cleanup.py','emulate_crypt_occupant_cleanup.py','crosscheck_crypt_occupant_cleanup.py','prepare_crypt_occupant_primary.py','scan_deep_crypt_pointers.py']],
        limitations=['ExactprovidedDXSHA;noautomaticother-versiontransfer.',
            '18grouplifecycleremovalsdonotassignbonusmeaningto10unknowngroups;8/18stillassigned. Donotlabelunknownsunused.',
            'Preparedminimalroom/registeredmemberandoriginalinitializedCreature snapshots. Roomobjectdestruction/capacityfailure/automaticentryandWorldcleanupnotcomposed.',
            'Deepsearchretainsbounded/indirect/pointerreturn/bulk/escapedstack/nonstandardABIlimitations;1/12limitsnotglobalcoverage.',
            'All155priorcomponents/codec/effectiveparameters/baselinepreserved;newserializedfieldsandrawedits0;fullgoalactive.'])

def documents(s):
    n=s['counts']
    text=f'''# Crypt: очистка комнаты с обитателями

Этап178, 3 ноября2026. Предоставленный DKII-DX.exe. Впервые для этого блока выполнен Crypt-setter с непустым настоящим Creature-списком; ранее4D9570проверялся с пустыми списками.

При **ненулевом старом tag комнаты+53** setter4D8E60 сначала вызывает4D9570. Для каждого Creature из roomWORD0A исходная48AD30(1)→476D30сбрасывает AI, а4E5E00разрывает связи списка и обнуляет CreatureWORD3B9. Комната теряет голову списка, DWORD55становится0. Только затем setter уменьшает старый счётчик вида, при замене увеличивает новый и записывает новый tag.

Это проверено с двумя исходными инициализированными Creature snapshot и корректными двусторонними room-связями. Они остаются активны и с прежнимHP: удаление из списка комнаты не уничтожает их. Дополнительный контроль сначала действительно вводит обоих вstate255через491830; исходная очистка переводит255→1. Это наблюдаемые записи, не запись конечного состояния стендом.

Причинный контроль со старымtag0и теми же обитателями **не вызывает очистку**. Регистрация новогоWarlockвсё равно повышает108Aи пишет новыйtag302, но остаются голова101, связи обоихCreature, их roomtag300/state255, roomDWORD55=1234и bookkeeping100. Следовательно, условие — прежняя регистрация, а не просто наличие обитателей либо любая запись новогоtag.

Все18групп проверены по одному ранее установленному виду-представителю: старый счётчик1→0при двухобитателях. При заменеMonk→Warlock1095становится0,108Aстановится1. Контроль владельца8 меняет счётчики8; байты счётчиков владельца7сохраняются. Зарегистрированныйmember301/новый302 — явно отдельные подготовленные записи; обитатели имеютtags101/100. Автоматический приход вCryptэтим не исполняется.

Метод4E5E00также обновляет runtimeWORD6B/6Dкомнаты: вычитает usageBYTEизdefinition672приusage<=6B. У двух выбранных snapshotбайты1и0, потому подготовленные100→99, а с пустым списком остаются100. Нового физического поляKWDили процентаCryptздесь не назначается.

## Проверки и границы

24принятыеnative-сцены, {n['whole_Crypt_registration_setters']}полных4D8E60, {n['whole_room_cleanups']}полных4D9570, {n['whole_Creature_reset_wrappers']}Creature-reset и {n['whole_room_Creature_unlinks']}Room-unlink. Шесть независимых GhidraCPUконтролей сравнили {n['Ghidra_compared_regions']}областей/{n['Ghidra_compared_bytes']}байт. Все {n['unique_executed_original_PCs']}исполненныхPCпокрыты {n['inherited_function_extents']}прежними диапазонами и одним новым полным4E5E00на484байта. Пятьvtable/исходныеraw/guardsсверены; низкаястраница толькоDWORD FS:[0].

Ранние exploratory-пробы неверно передавали аргумент rootвызову: замороженныйCPUrunnerобрабатываетstack_argsтолько вbefore_calls. Они исключены. Все принятые24сцены вызывают4D8E60черезявныйbefore_callснужнымаргументом; безаргументный4E52F0используется конечнымroot. НезависимаяGhidraпроверка строит те же вызовы и проверяет всю память.

Дальнейшее ограничение — списокObjectsroomWORD0Eпустой: удаление объекта типа143/8Fпока не выполнено. Нет полной фабрики комнаты, автоматическогоCrypt-entry, Worldscheduler, физическогоосвобожденияCreature или живыхgraphics/audio. Подготовленные room/member не выдаются за полную загрузку карты.

## Углублённый поиск десяти групп

Отдельныйпоиск118исходныхowner-lookupи421подсказкиполя+2Aпрошёлдо6уровнейвызовов/5000состояний, без2612нетипизированныхglobaltagподсказок. Поле+2Aсамопосебе всё ещё не гарантируеттипCPlayer. Получены9961root-состояние,164callee-контекста,33прежниецепочкиCrypt. Нового игрового потребителя не найдено; остаются1root/12calleeограничений. Это отдельная областьпоиска, её числа не складываются с3171rootэтапа177какновыеклассы.

Индексированная4D93E0— уже известное изменение счётчика, подтверждённое этапом94; повторное чтение не считается новымбонусом. Установлен8из18групп,10неизвестны. Неисчерпанные косвенныевызовы/возвраты/bulkчтения и ABI/stackside-effects запрещают объявить их глобальнонеиспользуемыми.

Пакет156компонентов. Сохранены155прежних,codec/effective-представления,SHA420текущихисточникови7путейEXE. Новыхraw-правок0; сериализатор менять не требуется. Машиннаясхема — `crypt-occupant-cleanup-semantics.json`. Следующий шаг того же блока — object-веткаCryptи оставшиеся индексированные/косвенныепотребители. Глобальныепараметры и резисты остаютсяпозже. Общаяцельактивна.
'''
    (ROOT/'CRYPT_OCCUPANT_CLEANUP_RU.md').write_text(text,encoding='utf-8')
    note=(f'Актуальный этап178: пакет156компонентов. Crypt4D8E60снепустымсписком: старыйtag!=0 вызываетreset/unlinkобитателей и сменусчётчика; oldtag0сохраняетсписок/state255дажеприновомmember. '
        f'24native/18групп/{n["whole_room_Creature_unlinks"]}unlink/6Ghidra,{n["Ghidra_compared_regions"]}областей/{n["Ghidra_compared_bytes"]}байт;{n["unique_executed_original_PCs"]}PC,1новый+{n["inherited_function_extents"]}прежнихдиапазонов. '
        'Углублённый539root/6уровней поиск:33прежнихцепочки,новыхбонусов0,1/12ограничений;назначено8/18. Прежние155/codec/effective/baselineсохранены. СледующаяCryptobject-ветка143. См.`CRYPT_OCCUPANT_CLEANUP_RU.md`.\n\n')
    f=ROOT/'INTEGRATION_RU.md';txt=f.read_text(encoding='utf-8')
    if 'Актуальный этап178:' not in txt[:1800]:
        first,rest=txt.split('\n\n',1);f.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 3 ноября 2026 — после этапа 178'):
        _,rest=txt.split('\n',1);f.write_text('# Оценка готовности на 3 ноября 2026 — после этапа 178\n\n'+note+'Экспертнаяоценкаоколо75%;Cryptоткрыт.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');f=ROOT/'phase178-prior-package-state.json'
    if not f.exists():
        assert len(package['components'])==155
        dump(f.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(f.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==155 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_occupant_cleanup_semantics phase178:24native/6Ghidra;all18groupcounterremovalswithactualtwo-Creaturelist. Nonzerooldroomtagtriggersreset1/unlink/room55clearbeforecounterdecrement/newincrement. Causaloldtag0preserveshead101/links/255evennewmember. Originalstate255->1andruntimeusage1/0bookkeeping100->99verified;object143branchopen. Deep539owner-hints/6levels/5000statesretains33counterleads,no newbonus,1root/12callee limits. Prior155/codec/effective/baselinepreserved;Crypt8/18assigned,goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase178-search-status.json',dict(phase=178,date='2026-11-03',goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase177publishedandaudited. Deeperownersearchpreservedunknownboundaries;24originalnonemptyCryptlifecycleexecutionsplus6independentGhidracomparisonscloseprioroccupant-listgap.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA),link('CRYPT_OCCUPANT_CLEANUP_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
