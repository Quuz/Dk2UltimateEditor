"""Publish verified search-model improvement without assigning unknown bonuses."""
from pathlib import Path
from copy import deepcopy
import json,re,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import scan_stack_crypt_pointers as scanner
from probe_shot_chainfire import p,EXE
ROOT=Path(__file__).resolve().parent
KEY='crypt_stack_search_semantics'
SCHEMA='crypt-stack-search-semantics.json'
sha=lambda b:hashlib.sha256(b).hexdigest()
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())
UNKNOWN=[0x108A,0x108B,0x1092,0x1093,*range(0x1096,0x109C)]

def primary():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;rows=[]
    for a in [0x4C6EF0,0x4C5A20]:
        assembly=f'ghidra/phase177-search-{a:06x}-assembly.txt';txt=(ROOT/assembly).read_text(encoding='utf-8')
        pcs=[int(v,16) for v in re.findall(r'^([0-9a-fA-F]{8}):',txt,re.M)];assert pcs and pcs[0]==a
        last=next(md.disasm(p.pe_read(blob,pcs[-1],15),pcs[-1]));n=last.address+last.size-a
        memory=f'ghidra/phase177-search-{a:06x}-memory.txt';raw=bytes(load(memory)['data'])
        assert len(raw)==n and raw==p.pe_read(blob,a,n)
        rows.append(dict(address=a,length=n,assembly=link(assembly),memory=link(memory),bytes_sha256=sha(raw),static_only=True))
    def ins(a):return next(md.disasm(p.pe_read(blob,a,15),a,count=1),None)
    cleanup=scanner.make_cleanup_reader(ins)
    assert cleanup(0x4C5A20)==24 and cleanup(0x4C6EF0)==20
    # Exact bounded slot index is established by original XOR/CMP/INC code;
    # LEA at4C6FA3 is an address computation, not a Crypt byte memory read.
    expected={0x4C6F02:'33f6',0x4C6F0D:'83fe02',0x4C6F18:'46',0x4C6FA3:'8d0c82'}
    for a,h in expected.items():assert p.pe_read(blob,a,len(bytes.fromhex(h))).hex()==h,(hex(a),p.pe_read(blob,a,4).hex())
    return dict(profile=dict(file=str(EXE),sha256=sha(blob)),functions=rows,static_only=True,
        verified_RET_cleanup={hex(a):cleanup(a) for a in [0x4C5A20,0x4C6EF0]},
        slot_bound=dict(index_min=0,index_max=2,stride=25,first_flag=0x1C2,last_flag=0x1F4,last_timestamp_end=0x20D,
            address_lead=0x4C6FA3,new_Crypt_consumer=False),new_gameplay_native_runs=0)

def build():
    old=load('phase157-bounded-owner-crypt-leads.json');now=load('phase177-stack-owner-crypt-leads.json');controls=load('phase177-stack-controls.json')
    assert now['program_sha256']==old['program_sha256']==sha(EXE.read_bytes())
    assert controls['passed'] and controls['no_gameplay_claim'] and len(controls['controls'])==5
    assert controls['legacy_scanner_sha256']==link('scan_bounded_crypt_pointers.py')['sha256'] and controls['stack_scanner_sha256']==link('scan_stack_crypt_pointers.py')['sha256']
    for c in controls['controls']:
        assert c['synthetic'] and c['static_offsets']==[0x108A] and c['legacy_static_offsets']==[] and c['native_reads']==[dict(offset=0x108A,size=1)]
    key=lambda r:(r['root']['address'],r['field'],r['read']['address'],r['read']['offset'],r['read'].get('callee'),r['read'].get('call'))
    assert {key(r) for r in old['candidates']}=={key(r) for r in now['candidates']}
    assert len(now['producer_roots'])==3171 and now['root_states']==283570 and now['limited_roots']==70 and now['callee_summaries']==813 and now['limited_callees']==69
    assert len(now['candidates'])==33 and len(now['indexed_events'])==291
    evkey=lambda r:(r['root']['address'],r['event']['address'],r['event']['constant'],r['event']['index'],r['event']['scale'],r['event'].get('callee'),r['event'].get('call'),r['event']['access'])
    previous={evkey(r) for r in old['indexed_events']};added=[r for r in now['indexed_events'] if evkey(r) not in previous]
    assert len(added)==1 and added[0]['event']['address']=='0x4c6fa3' and added[0]['event']['access']=='address'
    unknown_hits=[r for r in now['candidates'] if r['read']['offset'] in UNKNOWN]
    assert all(0x4D8E60<=int(r['read']['address'],16)<0x4D9410 for r in unknown_hits)
    proof=primary();baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    prior=load('phase177-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    assert link('dk2_formats.py')==prior['codec']
    effects={
        0x108C:('Temporary speed multiplier in whole Creature tick; later restore', 'crypt-tick-semantics.json'),
        0x108D:('Recipient-owner CreatureThreat factor (10-count)*10%', 'crypt-threat-semantics.json'),
        0x108E:('Emitter-owner CreatureThreat +30% per positive signed count', 'crypt-threat-semantics.json'),
        0x108F:('Gate for +40%*Vampire108E coefficient, not own count', 'crypt-threat-semantics.json'),
        0x1090:('Attacker-owner melee +10% per positive count with integer truncation', 'melee-attack-semantics.json'),
        0x1091:('Attacker-owner melee +15% per positive count with separate truncation', 'melee-attack-semantics.json'),
        0x1094:('Actor WorkEnhancement +205 per positive count in whole Creature tick; not all job output proved', 'crypt-tick-semantics.json'),
        0x1095:('Recipient-owner generic HP coefficient subtract8/128 per positive count', 'room-species-bonus-semantics.json')}
    groups=[]
    for g in load('room-species-bonus-semantics.json')['species_groups']:
        offset=g['owner_byte_offset'];row=deepcopy(g)
        row.pop('effect');row.update(assignment='confirmed' if offset in effects else 'unknown')
        if offset in effects:row.update(effect=effects[offset][0],evidence=link(effects[offset][1]))
        else:row.update(effect=None,meaning_not_assigned=True)
        groups.append(row)
    assert len(groups)==18 and sum(g['assignment']=='confirmed' for g in groups)==8
    return dict(phase=177,date='2026-11-03',families=['Creatures','Rooms'],profile=proof['profile'],
        priority=dict(current_block='Crypt groups',block_complete=False,next='Inspect remaining typed owner call contexts/returns and populated-room registration lifecycle; do not switch globals or resistances.'),
        conclusion=dict(new_Crypt_group_meanings=0,assigned_groups=8,remaining_groups=10,unknown_offsets=UNKNOWN,global_nonuse_proved=False,
            result='Searchstackblindspotreducedandverified;all33previouscounterleadsretained,newindexedleadruledoutstatically. No new raw field or bonus invented.'),
        groups=groups,
        model=dict(prior=link('scan_bounded_crypt_pointers.py'),current=link('scan_stack_crypt_pointers.py'),
            change='UniformboundedRETcleanupkeepscallerspillsandadvancesSP. UnknowncleanupsetsSPunknowninsteadofrebase0;nonvolatileframeoriginsremain. MOVESP,knownframecanrestoreorigin.',
            proof='Fiveconstructedx86callers/stdcall/cdecl/indirect/mixedRETdemonstraterealowner108Aread:legacysearchmissesallfive,newsearchretainsallfive. SyntheticnotDK2gameplay.',
            safety='Cleanupunknownforindirectjumps,decodelimits,inconsistentRETorout-of-boundtails;numericmemoryvaluesremainunknown. Everyleadrequiresoriginaltype/byte/executionvalidation.'),
        search=dict(previous=link('phase157-bounded-owner-crypt-leads.json'),current=link('phase177-stack-owner-crypt-leads.json'),
            decoded_instructions=now['decoded_instructions'],producer_roots=3171,root_states=283570,limited_roots=70,callee_contexts=813,limited_callee_contexts=69,
            call_stack_statistics=now['call_stack_statistics'],previous_leads=33,current_leads=33,new_counter_leads=0,previous_indexed_events=290,current_indexed_events=291,new_indexed_events=added,
            context_comparison='Largercontextcountsreflectlongersurvivingpaths;70/69limitsarenotapercentageorlessaccuratebythemselves. Old43/49limitsnotdirectcoverageequivalent.',
            numeric_controls=dict(three_entry=now['numeric_control'],forty_six_entry=now['numeric_DEC_control'])),
        new_lead_review=dict(original_CALL=0x512882,function=0x4C6EF0,nested_callee=0x4C5A20,address_instruction=0x4C6FA3,
            status='Staticoriginalcodeconfirmsboundedthree-slotstructure,notCryptcounterread',
            formula='Indexi=0..2;flag1C2+25*i;data1C3..1DA+25*i;maxexclusiveend20D. NewLEAcalculatesowner+20*i,followingLEAadds5*i,notarbitraryindexinto108A.',
            distinction='Owneridentityhintitselfwasuntypedglobaltaglookup. Broaderstructuregameplaynameisnotassigned;notnewformatsemantics.',primary=proof),
        algorithm_controls=dict(evidence=link('phase177-stack-controls.json'),synthetic_executions=5,new_original_gameplay_executions=0),
        preservation=baseline,
        counts=dict(producer_roots=3171,root_states=283570,callee_contexts=813,limited_roots=70,limited_callee_contexts=69,retained_counter_candidate_chains=33,
            new_counter_candidate_chains=0,new_indexed_chains=1,synthetic_x86_controls=5,new_original_gameplay_native_scenes=0,new_original_Ghidra_CPU_controls=0,
            original_static_function_ranges=2,original_static_bytes=sum(r['length'] for r in proof['functions']),crypt_groups_assigned_total=8,crypt_groups_remaining=10,
            new_raw_edits=0,new_opaque_field_names=0,preserved_prior_components=154,preserved_source_files=420,preserved_EXE_paths=7,source_writes=0),
        helpers=[link(n) for n in ['scan_stack_crypt_pointers.py','check_crypt_stack_search.py','constant_branch_domain.py']],
        limitations=['ExactprovidedDXSHA;noautomaticversiontransfer.',
            'Searchstillbounded:1500root/1000callee states,8KBcallee span,3directcalllevels,256contexts/root,untypedentity/tag/stridehints. Unknown indirect calls/returns, bulk/string/indexed consumers not exhaustive.',
            'UniformRETimmdoesnotprovecompleteABIorcallee-sideeffectsemantics. Escapedstackaddressesandcalleechangesofcallerargument/localmemorynotfullymodeled;nonstandardcallee-savedregisterchangesnotproved. Allcandidatesneedmanualproof.',
            'Newindexedpathonlystaticallyreviewed;syntheticexecutiondoesnotcountasoriginalDK2native/GhidraCPUscene.',
            'No newCryptbonusassigned;tenremainunknown,notlabelledunused. No newserializedfield/codec/effectiveparameterchanges. Fullgoalactive.'])

def documents(s):
    text='''# Crypt: поиск указателей через CALL и актуальные группы

Этап177, 3 ноября2026. Точный предоставленный DKII-DX.exe. **Назначено8из18групп; десять пока неизвестны. Нового бонуса на этом этапе не объявляется.**

Прежний поиск послеCALL полностью сбрасывал модель стека. Это могло скрыть код, который сохраняет указатель владельца в локальной переменной, вызывает другую функцию и затем восстанавливает указатель.

Новый анализатор сохраняет локальные значения и невременные frame-регистры. Если все доступные ограниченному CFG выходы вызываемой функции имеют одинаковыйRET imm, учитывает эту очистку. Если очистка неизвестна, ESP становится неизвестным, а не условнымнулём; доступы через сохранённыйEBP всё ещё можно проследить. Это поисковая модель, не полныйCPU и не доказательство всехABI/side-effects.

Пять сконструированных x86-программ выполнены независимо вUnicorn: EBP/ESPspill черезRET8, cdeclRET0 с очисткой caller, косвенныйCALL и разныеRET4/8. Реальные чтения подтверждаютowner+108A; старый анализатор теряет их во всех пяти, новый находит. **Это проверки инструмента, не пять новых игровых сценDK2.** Численные контроли3и46записей из прошлого поиска также сохранены.

## Результат повторного поиска

3171начальнаяподсказка;283570состояний;813callee-контекстов. Сведения о стеке сохранились через8572перехода с известной очисткой, изних3146с сохранёнными значениями;10692перехода имели неизвестную очистку. Это число обходов анализа, не число разных функций или игровых вызовов.

Все33прежние цепочки чтений Crypt сохранились. Новых чтений десяти неизвестных групп не найдено. Индексированных цепочек стало291вместо290. Ghidra-разбор новой цепочки4C6EF0→4C5A20 показал3записи сошагом25: flags1C2/1DB/1F4, данные заканчиваютсядо20D. LEA4C6FA3 — вычисление адреса, не чтениеCrypt. До108Aэтот ограниченный индекс не доходит. Более широкое игровое название структуры не назначается.

Точные байты двух статических функций —701байт — сверены с исходнымPE иGhidra. Новый путь не исполнялся как игровая сцена. На этом этапе нет новых original-gameplay/native/GhidraCPUпроверок; не подменяем их синтетическими тестами.

Сохранились70root- и69callee-ограничений. Путей стало больше, потому что указатели дольше сохраняются; эти числа нельзя выдавать за процент готовности или напрямую сравнивать с прежними43/49. Неисчерпаны косвенныевызовы/возвраты, строковые/bulkчтения, все индексированные диапазоны, глубина3 и запись callee в переданные caller-локальные адреса. Неизвестность десяти групп **не превращается в утверждение об их неиспользуемости**.

## Текущая карта групп

| Owner byte | ТекущиеCreatureID/имена | Подтверждённый потребитель |
|---|---|---|
'''
    ru={0x108C:'Временная поправка скорости внутриCreaturetick; затем восстановление.',0x108D:'Threat получателя:−10%за положительныйсчётчик,безобщегоclamp.',
        0x108E:'Threat источника:+30%за счётчик.',0x108F:'Условие бонусаThreat; коэффициент40%берётся от108E,не своего108F.',
        0x1090:'Ближний удар:+10%за счётчик владельца атакующего.',0x1091:'Ближний удар:+15%за счётчик владельца атакующего.',
        0x1094:'WorkEnhancement:+205за счётчик; не доказаны все выходы работы.',0x1095:'ОбычныйHP-factor:−8/128за счётчик владельца пострадавшего.'}
    for g in s['groups']:
        desc=', '.join(f'{i} {name.strip()}' for i,name in zip(g['creature_ids'],g['current_names']))
        text+=f'|`{g["owner_byte_offset"]:04X}`|{desc}|{ru.get(g["owner_byte_offset"],"Неизвестно; назначения не найдено.")}|\n'
    text+='''
Регистрация/удаление вCrypt меняет счётчик владельца комнаты, а подтверждённые потребители используют владельца атакующего/пострадавшего/источника. Это не бонус исключительно стоящему в комнате существу и не новый процентKWD. Подробности ранних исполнений сохранены вCRYPT_BONUSES_RU.md,CRYPT_THREAT_RU.md,CRYPT_TICK_RU.md иMELEE_ATTACK_RU.md; старые количества назначенных групп относятся к своим этапам.

Пакет155компонентов;154прежних,codec/effective-представления, SHA420текущихисточников и7путейEXE сохранены. Новыхraw-редактирований/назначенийполей нет. Машинная схема — `crypt-stack-search-semantics.json`; новый поиск — `scan_stack_crypt_pointers.py`. Старый анализатор/результаты оставлены.

Текущий блокCrypt открыт. Дальше — ограниченные контексты с достоверным owner-pointer и возвратами/передачами указателей, затем неисполненная часть регистрации с непустой комнатой. Глобальные параметры и резисты остаются в установленной очереди. Общая цель по15семействам активна, оценка около75%экспертная.
'''
    (ROOT/'CRYPT_STACK_SEARCH_RU.md').write_text(text,encoding='utf-8')
    note='Актуальный этап177: пакет155компонентов. Cryptпоиск сохраняет owner-pointer черезCALL:5синтетическихx86контролей ловят прежний stack-resetпробел;3171roots/283570states/813contexts,33прежних чтения сохранены. Единственная новая indexedзацепка относитсяк3записям1C2..20C,неCrypt. Назначено8/18групп;10неизвестны,глобальнаянеиспользуемостьнедоказана. 70/69ограниченийпоиска,701байт2статическихфункций сверены;игровыхnative/GhidraCPUсценновых0. Прежние154/codec/effective/baselineсохранены. См.`CRYPT_STACK_SEARCH_RU.md`.\n\n'
    f=ROOT/'INTEGRATION_RU.md';txt=f.read_text(encoding='utf-8')
    if 'Актуальный этап177:' not in txt[:1800]:
        first,rest=txt.split('\n\n',1);f.write_text(first+'\n\n'+note+rest,encoding='utf-8')
    f=ROOT/'READINESS_RU.md';txt=f.read_text(encoding='utf-8')
    if not txt.startswith('# Оценка готовности на 3 ноября 2026 — после этапа 177'):
        _,rest=txt.split('\n',1);f.write_text('# Оценка готовности на 3 ноября 2026 — после этапа 177\n\n'+note+'Экспертная оценка около75%;Crypt открыт.\n'+rest,encoding='utf-8')

def main():
    package=load('editor-format-package.json');f=ROOT/'phase177-prior-package-state.json'
    if not f.exists():
        assert len(package['components'])==154
        dump(f.name,dict(package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),components=deepcopy(package['components']),codec=link('dk2_formats.py'),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(f.name);s=build();dump(SCHEMA,s)
    assert len(old['components'])==154 and set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_stack_search_semantics phase177:stack/framepointerretentionacrossCALLwithuniformRETcleanuporunknownSP;5syntheticx86controlslegacy misses/newretainsactual108Aread. 3171roots/283570states/813contexts;33priorcounterchainsretained,newindexed4C6FA3bounded3slots1C2..20C,notCrypt. 8/18assigned,10unknown;70root/69callee limits,no globalnonuse. No new original-gameplay execution/rawmeaning. Prior154/codec/effective/baselinepreserved.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package);documents(s)
    dump('phase177-search-status.json',dict(phase=177,date='2026-11-03',goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Phase176publishedandaudited,first-blockreviewconsolidated. Cryptstacksearchblindspotreduced,independentlyvalidatedandnewindexedleadreviewed.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA),link('CRYPT_STACK_SEARCH_RU.md')]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
