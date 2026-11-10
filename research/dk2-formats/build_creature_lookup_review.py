"""Publish native unique/raw lookup consequences and bounded priority searches."""
from pathlib import Path
import json
from collections import Counter
from build_effect_semantics import pe_read
from build_trap_readiness import digest
ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))


def primary():
    spec=load('phase73-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    assert [int.from_bytes(bytes.fromhex(r['hex']),'little') for r in blocks]==[0x50D390,0x50E3A0]
    return functions,blocks


def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))


def search(name):
    q=load(name)
    return {**link(name),'program_sha256':q['program_sha256'],'producer_roots':len(q['producer_roots']),'root_states':q['root_states'],
        'callee_summaries':q['callee_summaries'],'limited_roots':q['limited_roots'],'limited_callees':q['limited_callees'],
        'limited_argument_context_roots':sum(r['limited_argument_contexts'] for r in q['producer_roots']),
        'field_candidates':dict(Counter(r['field'] for r in q['candidates'])),
        'warning':'Candidate getter/vslot/entity-offset seeds require actual object type and native-boundary proof. Limits and unhandled indirect/index paths prevent global non-use claims.'}


def main():
    functions,blocks=primary();emu=load('phase73-creature-lookup-emulations.json');assert len(emu['cases'])==34
    exposure=load('phase73-creature-pointer-exposure.json')
    s=dict(phase=73,family='Creatures native effective/raw ID lookup and remaining priority/jump search',
        primary_program_bytes=functions,primary_vslot_data_ranges=blocks,native_emulations=emu['cases'],native_execution_scope=emu['scope'],
        executable_sha256=exposure['profiles'][0]['sha256'],
        runtime_layout=dict(record_bytes=2199,array_in_world=0x15C4,count_in_world=0x15CC,ownID=0x770,cloneID=0x5B3,flags=0x686),
        raw_paths=dict(ownID=dict(raw=0xF4B,intermediate=0x133B,definition=0x770),cloneID=dict(raw=0xD3B,intermediate=0x112B,definition=0x5B3),flags=dict(raw=0xEFC,intermediate=0x12EC,definition=0x686),
            evidence='Earlier catalogue-loading.json/phase7 scalar storage proofs;not newly executed raw converters.'),
        lookup=dict(effective=dict(entry=0x50D390,world_slot=0xC0,helper=0x494360,key='CloneID5B3 whenFlags686&08000000;otherwise ownID770'),
            raw=dict(entry=0x50E3A0,world_slot=0x1C8,key='Always ownID770'),
            shared='argumentlow8;startmin(ID-1,count-1);walkback;equalreturnsrow;smallerkeyreturnsNULL.',
            order_counterexample=dict(rows_A=['unique own32 clone12','ordinary own13'],rows_B=['ordinary own13','unique own32 clone12'],
                A=dict(raw32='NULL',effective13='row1',effective12='row0'),B=dict(raw32='row1',effective13='NULL',effective12='row1'),
                consequence='Permuting same definitions trades which lookups are reachable. No universal automatic physical-file sort or renumbering policy follows.'),
            preservation='Native callback insertion and runtime ordering are separate from raw file storage.34 cases prebuild arrays;full parser/insertion not newly executed.'),
        searches=[search('phase73-DX-creature-argument-leads.json'),search('phase73-object-priority-pointer-leads.json')],
        search_scope='Current Creature three-direct-call-level pass withECX/8potentialstackarguments/ESP-EBPspill aliases and correctTEST/CMPreads;previouscurrentphase65wasoneadjustedthiscall. ObjectPriority112usedasadditionalhypothesisalongunknown106andHealth104. No newunknownconsumerfound.',
        pointer_exposure={**link('phase73-creature-pointer-exposure.json'),
            'counts_by_exe':[{ 'profile':p['profile'],'candidates':len(p['candidates']),
                'address_taking_or_indexed_or_immediate_candidates':sum(r['kind']=='immediate' or bool(r.get('index')) or r['instruction'].startswith('lea ') for r in p['candidates'])} for p in exposure['profiles']],
            'scope':'Exact-displacement operations and selectedimmediates for6EB/75C;includeswritesandcopies,notnewsemanticreads. No LEA/indexed/offset-immediatelead established in this exact pass. Adjusted/dynamic pointers remain unresolved.'},
        exploratory_virtuals=dict(file='phase73-pickup-virtual-prefixes.json',scope='Selection hints only;CObjectvslot18actuallytestsnegatedflag40000. BaselinePILLARlabelnotnewlyverifiedhere. No pickability/priority meaning inferred merely from slotnumber or provisional filename. Not part of native lookup cases.'),
        corpus=load('body-camera-semantics.json')['corpus'] if 'corpus' in load('body-camera-semantics.json') else load('creature-extension-semantics.json')['corpus'],
        api=dict(file='native_creature_lookup.py',operation='lookup_index',policy='Read-only overexplicitruntime ownID/cloneID/flags rows. Does not insert,sort,delete,renumber or serialize file records.'),
        limitations=['34wholegetters,17perroute;494360realhelperexecuteswithineffectivegetter.3completePEfunctions/2vslotrangesattested.',
            'World5888bytes/whole2199-byte-rowarrayscomparedunchanged. NativeEAXsavedbeforeobserver;finalEAX/EBPnotoriginalreturn.',
            'Searchesareboundedstaticwithuncertaincandidate types;1Creatureand1Objectcalleesummaryhitlimits. Indirectreturns/calls,dynamicindices,computedjumps,unknownABIremain.',
            'AbilityJump75C/PickUpPriority6EB and ObjectPriority112 gameplay ordering remain open;no rename from negative search.',
            'No newrawpatchcases;earlier exactID/clone/flags field paths retained. No originalgamefiles/EXEs orparalleleditorwrites.'])
    (ROOT/'creature-lookup-review.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    catalogue=load('catalogue-loading.json');catalogue['phase73_creature_lookup']=s
    (ROOT/'catalogue-loading.json').write_text(json.dumps(catalogue,ensure_ascii=False,indent=2),encoding='utf8')
    print('Creaturelookups34native;uniqueorderingverified;priority/jumppurposesremainopen')


if __name__=='__main__':main()
