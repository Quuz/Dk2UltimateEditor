"""Publish AP lookup and stack-lifetime search review in existing AP component."""
from pathlib import Path
from collections import Counter
import json,hashlib
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase76-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        assert int.from_bytes(b,'little')==r['target']
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return functions,blocks

def main():
    functions,blocks=primary();emulations=load('phase76-ap-lookup-emulations.json');search=load('phase76-ap-spill-leads.json');baseline=load('phase64-ap-pointer-leads-v2.json');tag_search=load('phase76-ap-tag-spill-leads.json')
    strong_old=[r for r in baseline['candidates'] if r['field']=='possibleAPFlags' and r['root']['producer']!='global-tag-table-untyped']
    assert not strong_old
    controls=[r for r in search['candidates'] if r['field']=='possibleAPFlags'];assert len(controls)==1 and controls[0]['read']['address']=='0x47205e'
    old_addresses={r['read']['address'] for r in baseline['candidates'] if r['field']=='possibleAPFlags'}
    tag_new_addresses={r['read']['address'] for r in tag_search['candidates'] if r['field']=='possibleAPFlags'}-old_addresses
    assert tag_new_addresses=={'0x47205e'}
    corpus=load('ap-retreat-semantics.json')['corpus']
    s=dict(phase=76,family='ActionPoint linked ID lookup and native-call stack lifetime review',
        primary_program_bytes=functions,primary_vslot_data_ranges=blocks,native_emulations=emulations['cases'],native_execution_scope=emulations['scope'],
        functions=dict(DX=dict(lookup=0x50FAE0,tag_table=0x6EE540),old=dict(lookup=0x504140,tag_table=0x6D95C8)),
        runtime_layout=dict(head_in_world=0x12,next_in_entity=8,id_in_entity=0x37,flags_in_entity=0x32,allocation_bytes=64),
        lookup=dict(world_slot=0x2C8,
            rule='Read WORDhead fromworld12;resolvepointer throughglobal tagtable;compareAPBYTE37 toargumentlow8. ReturnfirstmatchedpointerinEAX;followWORDnext8 untilresolvedpointerNULL. No ordering comparison.',
            results=['Unsorted IDs remain searchable.','Duplicate IDs return first node in runtime linked order.','ID0 and255 are ordinary comparisons when matching nodes exist.','High24 argument bits ignored.','Flags04/08 and owner/kind fields not read by this helper.'],
            tag0='No explicit tag0 sentinel check:syntheticnonNULLtable0 is followed. Nativefixtures cover head0 andnext0 resolving to valid nodes,not normal-game reachability.',
            local_guards='No local kind/owner/bounds/cycle/recordcount guard. Upstream AP-list invariants remain assumed. Wrongkind/owner examples are synthetic and do not prove editor corruption.',
            diagnostic_return='Ghidra initially typed the lookup asvoid;nativeEAX capturedbeforeobserver proves pointerreturn. Do not use provisional decompiler return type as storage evidence.',
            ID_scope='Getter acceptingIDs0/255 does not establish all editor/game-wide constraints or allow automatic renumbering. Raw physicalorder andfields stayauthoritative.'),
        native_contract_compositions=dict(cases=4,flags=[0,4,8,12],
            path='NativeAPgetter→syntheticcaller→native5558D0→native508FE0→reloadsameAPspill→readflags32. Syntheticcode beginsaftergetternativeRET.',
            stack='Caller stores APpointer in[SP+20],point12bytesin[SP+28]. PRNG has4DWORDargs andRET0;mapgetter takes1DWORD pointerandRET4. Unpassedpointerspillandpointbytesremainunchangedinall4fixtures.',
            PRNG='Actual5558D0 mutates worldseed andmaywriteits ownstackargument3 while computingROR;argument cells cannot be treatedasimmutable. Loggingdisabledbyfixture75B2F4=0.',
            map='Actual508FE0 returns base+16*(signedQ12Y*worldWidth+signedQ12X),without boundscheck. Testsuse32widthand10/12point;no proofout-of-bounds mapaccesssafety.'),
        search_review={**link('phase76-ap-spill-leads.json'),
            'baseline':link('phase64-ap-pointer-leads-v2.json'),'baseline_lookup_root_flags_candidates':0,
            'new_producer_roots':len(search['producer_roots']),'new_root_states':search['root_states'],'callee_summaries':search['callee_summaries'],
            'limited_roots':search['limited_roots'],'limited_callees':search['limited_callees'],'field_candidates':dict(Counter(r['field'] for r in search['candidates'])),
            'recovered_control':dict(root_call=0x471ED0,spill_store=0x471EF8,spill_reload=0x472053,flags_read=0x47205E,mask_test=0x472061,mask=0x40),
            'cause':'Earlierwalkerresetallstack/frameknowledgeaftereverycall;lostAPpointerspillatESP20 beforeknownPRNGandworldmaplookup. ThusitmissedevenknownPatrol40onlookuproots.',
            'new_rule':'RetainunpassedlocalaliasesonlyacrossexplicitknownABIcallcontracts;invalidateargumentcells/deadlowerstackandvolatileEAX/ECX/EDX. Unknowncallsretainpreviousconservativereset.',
            'known_direct_contracts':{'0x5558D0':dict(argument_bytes=16,callee_pop_bytes=0),'0x50FAE0':dict(argument_bytes=4,callee_pop_bytes=4),'0x508FE0':dict(argument_bytes=4,callee_pop_bytes=4)},
            'virtual_hints':{'0x1D4':dict(argument_bytes=4,callee_pop_bytes=4),'0x2C8':dict(argument_bytes=4,callee_pop_bytes=4)},
            'scope':'Firstnewpassuses8DXlookup/vslotroots. Asecondpassnewlyappliestheknown-calllifetimerulesalsoon2623rootsincludinguntypedglobaltagloads;historicaloutputnotrewritten. Virtualreceivertyping,pointeeescapes,unknowncalls,indirectreturns,dynamicindicesandlineardecodingremainunresolved. Contractrulesproducecandidateleads,notuniversalABIproof.',
            'tag_table_extension':{**link('phase76-ap-tag-spill-leads.json'),'producer_roots':len(tag_search['producer_roots']),'root_states':tag_search['root_states'],
                'limited_roots':tag_search['limited_roots'],'callee_summaries':tag_search['callee_summaries'],'limited_callees':tag_search['limited_callees'],
                'limited_argument_context_roots':sum(r['limited_argument_contexts'] for r in tag_search['producer_roots']),
                'field_candidates':dict(Counter(r['field'] for r in tag_search['candidates'])),'new_unique_flag_read_addresses':sorted(tag_new_addresses),
                'warning':'Tag-tableloadsarenotAPtypeproof;function_hintmayrefernearestknownCALLtargetratherthanactualcontainingfunction. Realcontrolisprimary471EB0,notadvisoryhint471E80.'},
            'unknown_flags':'No new04/08consumer established bythisboundedpass. Earlierabsenceclaimsmustretainstack-lifetimeblindspot;rawbitmeaningsnotchanged.'},
        exploratory_consumers=dict(functions=[0x4C8620,0x5100B0],scope='FreshstaticAPcoordinateusersinspected;notnativelyexecutedornamedasHeroGateconsumers. No04/08read established. Outputsnotusedtoassignunknownflagmeaning.'),
        corpus=corpus,api=dict(file='native_ap_lookup.py',operation='lookup_tag',scope='Read-only overexplicittag/ID/nexttable. CycleValueErrorisdiagnosticguardonly;nativemethodhasnoguard. No rawsorting/serializer/repair.'),
        limitations=['36nativecompositions:32standalonegetters(16perEXE)and4DXgettter/PRNG/mapgettercallercompositions.4completeexecutedfunctionsplus1staticPatrol;3vslotrangesattested.',
            'Whole5888-byteworld/64-byte-nodearrays/prebuiltglobaltagtables/outputcompared. Maxhead65535testedwith65536-entrytable. Allocationofsuchtablesinrealgame nottested.',
            'AllnativeEAXreturnsarecapturedbeforeobserver;finalEAX/EBParesuccessofcomparisons. Rootentryisrealgetter;compositioncodeisonlypostreturn.',
            'No fullPatrolnativeexecutionnewlyperformed;sourcePatrolcontrolisstaticandpriorphase33scopeisunchanged.',
            'Getter/order/typeexamplesarenotrawparser,allocator,worldregistrationoreditorGUIproof. No realgameplaycorruptionclaimed.',
            'AP04/08 gameplaystillunknown. Zeroadditionalconsumersisnotglobalnonuse. Knownpositivecontrolrecoveryshowswhyhistoricalnegativepasseswerebounded.',
            'Widerupdatedtag-tablepasshas8limitedrootsand28limitedcallees;globaltagrootsremainuntyped. Itrecoverssameknown40readandnoothernewflagsreadaddress;notexhaustiveAPsemantics.',
            '106APcorpusinputsretainphase47AutoSaveThingssnapshot;notaclaimcurrentliveAutoSaveunchanged. No newrawpatchcasesororiginalgame/EXEwrites.'])
    assert len(s['native_emulations'])==36
    (ROOT/'ap-lookup-review.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    component=load('action-point-semantics.json');component['phase76_lookup_review']=s
    (ROOT/'action-point-semantics.json').write_text(json.dumps(component,ensure_ascii=False,indent=2),encoding='utf8')
    print('AP lookup:36native;spillsearch8roots/control40recovered;unknown04/08open')

if __name__=='__main__':main()
