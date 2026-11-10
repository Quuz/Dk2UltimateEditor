"""Publish native temple recipe semantics, corpus warnings and lossless patches."""
from pathlib import Path
from collections import Counter
import json,hashlib,struct,itertools
import dk2_formats as fmt
import native_sacrifice as api
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXE=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe')
def load(n):return json.loads((ROOT/n).read_text(encoding='utf-8-sig'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def save(n,v):(ROOT/n).write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')

def primary():
    manifest=load('phase88-sacrifice-primary-manifest.json');result={}
    for group in ['functions','data']:
        rows=[]
        for r in manifest[group]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());data=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(data)==r['length']==v['length']
            assert data==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            rows.append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(data)))
        result[group]=rows
    assert len(result['functions'])==16 and len(result['data'])==2
    return result

def recipe_warnings(records):
    out=[];prior=[]
    for i,raw in records:
        r=api.runtime_recipe(raw);n,kind,value,speech=struct.unpack_from('<IIII',r,18)
        seq=[tuple(struct.unpack_from('<IBB',r,j*6)[:2]) for j in range(n)]
        if kind==1 and not any(k==1 for k,_ in seq):out.append(dict(record=i,code='creature_reward_divides_by_zero_on_match'))
        if n==0:out.append(dict(record=i,code='empty_recipe_matches_every_history'))
        if kind not in [1,2,3,4]:out.append(dict(record=i,code='no_specific_reward_branch_in_DX',reward_type=kind))
        for earlier,old in prior:
            if len(old)<=len(seq) and (not old or seq[-len(old):]==old):
                out.append(dict(record=i,code='earlier_ordered_suffix_shadows_recipe',earlier_record=earlier));break
        prior.append((i,seq))
    return out

def permutation_groups(records):
    groups={}
    for index,raw in records:
        runtime=api.runtime_recipe(raw);count=struct.unpack_from('<I',runtime,18)[0]
        sequence=tuple(tuple(struct.unpack_from('<IBB',runtime,i*6)[:2]) for i in range(count));key=tuple(sorted(sequence))
        groups.setdefault(key,[]).append(dict(record=index,sequence=sequence,reward=list(struct.unpack_from('<III',runtime,22))))
    out=[]
    for key,rows in groups.items():
        possible=set(itertools.permutations(key));present={r['sequence'] for r in rows};same_reward=len({tuple(r['reward']) for r in rows})==1
        out.append(dict(ingredients=key,records=[r['record'] for r in rows],possible_orders=len(possible),present_orders=len(present),
                        all_orders_encoded=possible==present,same_reward=same_reward,order_independent_table_result=possible==present and same_reward))
    return out

def corpus():
    source=Path('D:/Games/DK2/Dungeon Keeper 2/Data/editor/GlobalVariables.kwd')
    paths=[source]+[Path(p) for p in (ROOT/'phase88-variable-paths.txt').read_text(encoding='utf-8-sig').splitlines() if p]
    files=[];errors=[];records=patches=0;aggregate=hashlib.sha256();all_warnings=[]
    for path in paths:
        data=path.read_bytes()
        try:parts=fmt.parse(data)
        except ValueError as e:
            errors.append(dict(file=str(path),sha256=digest(data),error=str(e),scope='Excluded from structural patching; original retained'))
            continue
        assert b''.join(s.pack() for s in parts)==data
        n=0;warnings=[];permutations=[]
        for si,section in enumerate(parts):
            if section.signature!=0xe6:continue
            recipes=[(i,r) for i,r in enumerate(section.records) if struct.unpack_from('<I',r)[0]==43]
            warnings.extend(dict(section=si,**w) for w in recipe_warnings(recipes))
            permutations.extend(dict(section=si,**group) for group in permutation_groups(recipes))
            for ri,raw in recipes:
                n+=1;offset=section.offset+len(section.header)+16*ri;assert data[offset:offset+16]==raw
                for name,(off,width) in api.FIELDS.items():
                    for value in [0,1,(1<<(width*8))-1]:
                        changed=api.patch_field(raw,name,value);section.records[ri]=changed
                        output=b''.join(s.pack() for s in parts);expected=bytearray(data);expected[offset+off:offset+off+width]=value.to_bytes(width,'little')
                        assert output==expected
                        reparsed=fmt.parse(output);assert reparsed[si].records[ri]==changed
                        aggregate.update(digest(output).encode());patches+=1;section.records[ri]=raw
        assert b''.join(s.pack() for s in parts)==data and digest(path.read_bytes())==digest(data)
        files.append(dict(file=str(path),sha256=digest(data),recipes=n,warnings=warnings,permutation_groups=permutations));records+=n
        all_warnings.extend(dict(file=str(path),**w) for w in warnings)
    print('Corpus input inventory:',len(paths),'parsed',len(files),'recipes',records,'errors',[(Path(e['file']).name,e['error']) for e in errors])
    assert records>=44
    result=dict(source_files_considered=len(paths),canonical_files_hashchecked=len(files),recipe_records=records,
      exact_container_patch_checks=patches,patch_digest=aggregate.hexdigest(),files=files,excluded_noncanonical_files=errors,
      warning_counts=dict(Counter(w['code'] for w in all_warnings)),warnings=all_warnings,
      scope='Every type43 record, nine physical fields × three boundary values; no game or source writes')
    save('phase88-sacrifice-corpus.json',result)
    return result

def main():
    code=EXE.read_bytes();assert digest(code)==api.DX_SHA256
    p=primary();c=corpus();matching=load('phase88-sacrifice-matching-emulations.json');dispatch=load('phase88-sacrifice-dispatch-emulations.json')
    schema=dict(phase=88,date='2026-10-28',profile=dict(program='DKII-DX.exe',executable=str(EXE),sha256=api.DX_SHA256),
      primary_program_bytes=p,api=link('native_sacrifice.py'),current_recipe_export=link('temple-recipes-current.json'),current_recipe_table=link('TEMPLE_RECIPES_RU.md'),
      raw_layout=dict(record_bytes=16,type=43,input_pairs=[4,6,8],reward_type=10,speech_id=11,reward_value_bits=12,byte_order='little',preserve_inactive_pair_ids=True),
      loading=dict(callback='51DB30',world_append='50F7E0',runtime_record_bytes=34,input_stride=6,input_count_offset=18,reward_triplet_offset=22,
        world_array_offset=0x2e70,world_count_offset=0x731b,capacity=128,compaction='Skip input type0; retain order of other physical pairs',
        overflow='Append returns0 without mutation at count>=128; callback still returns1'),
      history=dict(append='4F1600',storage='Per receiver/room instance; all three input kinds use the same history',entry_offset=0x49,entry_stride=6,
        entry_fields=['type:u32','id:u8','level:u8'],last_slot=0x5b,next_slot=0x5c,count=0x5d,capacity=3,
        append_order='Write next; last=next; next=(next-1)%3; count=min(old+1,3)'),
      matching=dict(function='4F1660',rule='Ordered suffix of last N inputs; compare type+ID, not level; old history beyond N ignored',
        failure='Output triplet and history unchanged',empty_recipe='Matches even count0 history',
        creature_reward='For reward type1 add floor((sum creature BYTE levels mod256)/number of creature inputs)<<16 to raw reward DWORD, modulo2^32',
        zero_creatures='IDIV by zero at4F1749 after a fully matching type1 reward; two native fault cases'),
      selection=dict(function='5133A0',input_kind=3,rule='Recipes in stored order; first matching recipe wins regardless of ingredient count',
        other_static_callers=dict(creature='472930, input type1, actual creature level',object='49F600, input type2, level0')),
      reward=dict(function='4F1780',history_reset='After room-position helper, last=FF,next=0,count=0; all three slots retain bytes; reset occurs before reward-specific branch',
        branches={'1':'Creature factory4B78D0, room ownerWORD1D, ID low8; upgrade loop uses low8 of value>>16',
                  '2':'Object factory4B7A90, owner from globalWORD6F2548, ID low8',
                  '3':'Owner getter4E52F0 then4BA420; gold branch static in this phase',
                  '4':'Owner getter4E52F0→4C19C0; requested amount also added modulo2^32 to owner10EE'},
        other_types='No specific branch in this dispatcher; continue generic room-effect/speech handling',
        mana='Signed32 requested value and low32 sum; negative request clamps negative sum to0; nonnegative request caps signed sum at signed owner10EA; counter records requested amount, not actual change',
        position='4E4D90 chooses a room tile. Supplied caller position is not the source of the factory position in the inspected dispatcher.'),
      native_matching=matching,native_dispatch=dispatch,corpus=c,
      counts=dict(native_cases=len(matching['cases'])+len(dispatch['cases']),matching_and_loading_cases=len(matching['cases']),full_spell_dispatch_cases=len(dispatch['cases']),
        expected_native_faults=2,whole_native_functions_attested=16,functions_entered_in_native_cases=14,file_backed_data_ranges=2,original_code_or_vtable_adapters=0,original_files_written=0),
      limitations=['Profile is exact DX SHA only; older game and editor handling of recipes not cross-checked here.',
        'Creature/object sacrifice entry functions were reviewed statically; full spell sacrifice entry executed.',
        'Successful creature/object creation, gold award, generic visual room effect and live speech delivery not executed.',
        'Notifications stop via actual owner19A gate; room definition effect421 is0; no native function or World vtable patched.',
        'Expected division faults are emulator results under explicit inputs, not observed crashes in a live game.',
        'Other significant unknown fields remain. Prior Terrain indexed search is a separate unfinished investigation.'])
    existing=ROOT/'sacrifice-semantics.json'
    if existing.exists():
        old=load('sacrifice-semantics.json')
        if 'phase89_object_rewards' in old:schema['phase89_object_rewards']=old['phase89_object_rewards']
    save('sacrifice-semantics.json',schema)
    print(json.dumps(dict(counts=schema['counts'],corpus={k:v for k,v in c.items() if k not in ['files','warnings','excluded_noncanonical_files']}),ensure_ascii=True))

if __name__=='__main__':main()
