"""Audit native successful creation, complete raw snapshots and remaining limits."""
from pathlib import Path
import json,struct
from build_native_shot_success import ROOT,load,digest,link,primary,corpus,node_rows
import emulate_native_shot_pool as pool
import emulate_native_shot_success as factory
import emulate_native_creature_shot as cast
import memory_readout
def check_input(args,functions):
    memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
    for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
    assert all(a+len(b)<=0x66D86C or a>=0x66D96C for a,b in memory.items())
def factory_node(c,b,e):
    assert len(b)==371;u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    if not e['success']:assert b==bytes([c['seed']])*371;return
    assert u(0)==0x66D86C and u(4,2)==e['entity_id'] and u(0x12)==1 and u(0x16)==0 and u(0x28,2)==7
    assert u(0x125)==factory.DEFINITION and u(0x124,1)==c['shot'] and u(0x40)==65535
    assert u(0x13D)==c['data1'] and u(0x141)==c['data2'] and u(0x11C,2)==c['speed'] and u(0x11E,2)==0xFEDC
    assert list(struct.unpack_from('<3I',b,0xF6))==c['position']
    current=list(c['position']);current[2]=0x2000 if current[2]>0x3000 else current[2]
    assert list(struct.unpack_from('<3I',b,0x1A))==current and u(0x26,2)==0x1000
    assert u(0x2E)==(0x13572468 if c['sound'] else 0) and u(0x32)==0x13572468
def main():
    s=load('native-shot-success-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==40 and len(blocks)==8
    by={c['name']:c for c in pool.cases()}
    for row in s['native_pool_emulations']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=by.pop(row['case']);args,e=pool.build(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==e
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        assert memory_readout.decode(r['registers']).hex()==e['readout_hex'];check_input(args,functions)
    assert not by
    for role,module,key,want_ebp,prefix in [('factory',factory,'native_factory_replays',0,'phase80-native-shot-'),('creature',cast,'native_creature_replays',1,'phase80-native-creature-shot-')]:
        by={(c['name'],page):(c,page) for c in module.cases() for page in range(3)};chunks={};expectations={}
        for row in s[key]:
            p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c,page=by.pop((row['case'],row['page']));args,e=module.build(c,page);r=saved['result']
            assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['page']==page and saved['input']==args and saved['expected']==e
            assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=want_ebp).items())
            check_input(args,functions);chunks.setdefault(c['name'],{})[page]=memory_readout.decode(r['registers']);expectations[c['name']]=e
        assert not by
        for c in module.cases():
            b=b''.join(chunks[c['name']][i] for i in range(3))[:371];assert (ROOT/'ghidra'/(prefix+c['name']+'.bin')).read_bytes()==b
            if role=='factory':factory_node(c,b,expectations[c['name']])
            else:cast.verify_node(c,b,expectations[c['name']])
    assert s['raw_snapshots']==node_rows() and len(s['raw_snapshots'])==36 and sum(r['allocated'] for r in s['raw_snapshots'])==33
    for root in ['4982b0','4984d0']:
        b=(ROOT/'ghidra'/f'phase80-native-creature-shot-{root}-base.bin').read_bytes()
        assert all((ROOT/'ghidra'/f'phase80-native-creature-shot-{root}-{n}.bin').read_bytes()==b for n in ['alt-zero','alt68','alt255'])
    base=(ROOT/'ghidra/phase80-native-shot-base.bin').read_bytes();zero=(ROOT/'ghidra/phase80-native-shot-seed-zero.bin').read_bytes()
    dependent=[i for i,(a,b) in enumerate(zip(base,zero)) if a!=b]
    assert dependent==s['factory']['source_dependent_object_bytes']==[124,125,126,127,145,163,164,165,166,167,168,169,170,171,172,173,174,241,243]
    assert s['corpus']==corpus() and s['corpus']['unique_source_files']==83 and s['corpus']['new_exact_container_patches']==492
    example=s['hardcoded_id30']['source_example'];assert digest(Path(example['file']).read_bytes())==example['sha256'] and example['process_type']==9
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['shot_creation_semantics']['phase80_native_pool_and_cast']==s
    for key in ['creature_subrecords','spell_alternative_semantics','shot_parameters']:assert pack[key]['phase80_native_success_reference']==link('native-shot-success-semantics.json')
    assert pack['spell_alternative_semantics']['phase78_predicate_runtime']==load('spell-predicate-semantics.json')
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=80,date='2026-10-28',logical_scenarios=45,native_emulation_invocations=117,pool_scenarios=9,factory_scenarios=14,creature_success_scenarios=22,
        successful_allocated_scenario_states=33,successful_allocation_replays=99,complete_native_PE_code_spans=40,primary_file_backed_data_ranges=8,raw371byte_snapshots=36,allocated_snapshots=33,
        seed_dependent_instance_bytes=19,raw_source_files_hashchecked=83,additional_standalone_Shot_example=1,exact_container_patch_cases=492,source_executables_unchanged=4,package_components=67,
        original_code_or_Shot_vtable_replacements=0,AI_or_collision_process_ticks_executed=0,AlternativeShotRoom_global_nonuse_proved=False,research_writes_to_original_game_data=0,
        significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase80-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
