"""Publish native Terrain enumeration/cache/selection in the existing component."""
from pathlib import Path
from collections import Counter
import json,hashlib
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase74-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return functions,blocks

def search(name):
    q=load(name)
    return {**link(name),'program_sha256':q['program_sha256'],'producer_roots':len(q['producer_roots']),'root_states':q['root_states'],
        'callee_summaries':q['callee_summaries'],'limited_roots':q['limited_roots'],'limited_callees':q['limited_callees'],
        'limited_argument_context_roots':sum(r['limited_argument_contexts'] for r in q['producer_roots']),
        'field_candidates':dict(Counter(r['field'] for r in q['candidates']))}

def main():
    functions,blocks=primary();emulations=load('phase74-terrain-cache-emulations.json');assert len(emulations['cases'])==44
    corpus=load('terrain-catalogue-validation.json')['corpus']
    profiles=Counter((x['count'],x['maximum_id'],x['distinct_ids'],x['zero_ids'],x['header_count']) for x in corpus['id_profiles'])
    s=dict(phase=74,family='Terrain enumeration, global definition cache and lighting selection',
        primary_program_bytes=functions,primary_vslot_data_ranges=blocks,native_emulations=emulations['cases'],native_execution_scope=emulations['scope'],
        functions=dict(DX=dict(cache_init=0x455D40,select=0x452DC0,ordinal_id=0x50CEB0,getter=0x508DD0,count=0x508DF0,cache=0x6DA4A8,global_world=0x6DA4A4),
            old=dict(cache_init=0x44E7E0,select=0x44B910,ordinal_id=0x501900,getter=0x4FD7F0,count=0x4FD810,cache=0x6CD1A0,global_world=0x6CD19C)),
        runtime_layout=dict(record_bytes=460,array_in_world=0x15A4,capacity_in_world=0x15A8,loaded_in_world=0x15AC,id_in_definition=0x1C4,flags=0x17B,cache_entries=256),
        enumeration=dict(world_slot=0x60,rule='Scan slots1..capacity-1;skip records whose BYTE1C4==0. Ordinal is full DWORD. Return matched record ID in AL,or AL0 when no match. Upper24 EAX bits are not a documented return.',
            counterexamples=['Slot0 is never enumerated,even with nonzeroID.','capacity1 exposes no records,even when physicalslot1 exists.','Mismatched slot1 ownID7 returns7;cache then obtains direct slot7,not slot1.'],
            probes='Five ordinal queries per18 cache compositions:90 full native calls; capture AL before observer comparisons.'),
        cache=dict(world_slots=dict(ordinal_id=0x60,getter=0x70,count=0x74),
            rule='Clear all256 DWORD entries,then for ordinal0..<argument_world.loaded fetch ID using GLOBAL_world.slot60;get definition using argument_world.slot70(ID);store cache[ID]. Native count getter is queried before loop and after each iteration.',
            consequence='Extra loaded count beyond nonempty enumerated slots produces ID0 and sets cache0 to direct slot0. Entries never selected remain NULL,not a pointer to their empty slot.',
            global_argument_split='One explicit fixture perEXE,two total,uses distinct global and argument worlds:IDs come from global world,definition addresses and loaded count from argument world. Not evidence normal gameplay passes different worlds.',
            allocation_scope='Runtime buffers preallocated by harness;allocator/count-pass/parser not executed here. Earlier catalogue-loading.json statically establishes Terrain allocation sum_header_counts+1. This phase does not newly execute that allocator.'),
        selection=dict(input='ECX=16-byte cell fixture;stackDWORD x,y,output-pointer. Caller441CD0 uses selected definition before preparing terrain-light channels.',
            occupied='If WORDcell2&FFF !=0: require cellC==FF and currentFlags&10000000;write current definition and return1. Otherwise return0 without writing output.',
            special='If low12 cell2==0,cell9 bit20==0 and cellB bit40!=0: choose current if currentFlags&01000000,else native getter(ID38). Always write chosen pointer;return chosenFlags bit28.',
            other='If currentFlags bit28==0 return0. Otherwise call global_world.vslot21C(x,y,0);if return0 and currentFlags bit28 still set,write current pointer and return1;else return0 preserving prior output.',
            fallback_id=38,output_write_on_false='Verified current-writes-false and fallback38-writes-false in bothEXEs. The output can change even when native bool is0;caller must honor returned bool.',
            fixture='World vslot21C is a capture/result adapter. Coordinates,ECX,third argument0 and call count verified;actual geometry query and renderer not executed.',
            null_scope='Inputs deliberately supply valid cache pointers. No missing-cache handling or normal-game reachability of NULL entries established.'),
        searches=[search(f'phase74-{profile}-terrain-{kind}-leads.json') for profile in ['DX','old'] for kind in ['argument','cache']],
        search_scope='All linear PE.text;constant aliases;ESP/EBP spills;ECX and first8 possible stack arguments;up to3 direct-call levels. Nativecache writer proves cache element type. Virtual70 hints still require receiver identity for each root. No new183/1B5 semantic read established.',
        corpus=dict(files=corpus['files'],file_count=corpus['file_count'],records=corpus['records'],id_profiles=[dict(records=k[0],maximum_id=k[1],distinct_ids=k[2],zero_ids=k[3],header_count=k[4],files=v) for k,v in sorted(profiles.items())],
            scope='Physical82-file corpus has consecutive positive IDs1..N,without duplicates. This is data observation,not permission to sort or normalize other input files.'),
        api=dict(file='native_terrain_cache.py',operations=['enumerate_id','cache_ids','light_selection'],policy='Read-only over explicit runtime fixtures;no raw file serializer or automatic repairs.'),
        limitations=['44 complete native compositions:18cache initialization with90additional ordinal queries;26selection helpers.10realexecuted functions,plus1staticcaller;6vslot data ranges.',
            'Full1024-byte cache,world5888-byte buffers,whole460-byte-row arrays,output and cell fixtures compared. EAX/EBP final registers are observer equality,not native function return. Selection EAX captured before observer.',
            'Cache initializer has void-like return;no public return value inferred from incidental final EAX. Enumeration returns meaningful AL only.',
            'Searches have one limited DX getter callee. Indirect dispatch/returns,out-parameter pointees,dynamic indices,other binaries and linear-decoding blind spots remain. Zero unknown matches is not global non-use proof.',
            'Unknown raw198→definition183 and raw224→definition1B5 retain unresolved purpose and signedness. No new field name or gameplay effect assigned.',
            'No original game data/EXE writes,no modified-data game runs,no GUI or renderer launch. No new raw patch cases;phase58 complete-byte preservation remains prior evidence.'])
    (ROOT/'terrain-cache-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    component=load('terrain-lighting-semantics.json');component['phase74_cache']=s
    (ROOT/'terrain-lighting-semantics.json').write_text(json.dumps(component,ensure_ascii=False,indent=2),encoding='utf8')
    print('Terrain cache:44native/90ordinalqueries;11PEfunctions/6data ranges;',sum(r['producer_roots'] for r in s['searches']),'newsearchroots')

if __name__=='__main__':main()
