"""Offline replay audit,full-container patch checks,PE and package integrity."""
from pathlib import Path
import json,struct
from build_element_initialization import ROOT,load,digest,primary,corpus,snapshots
import emulate_element_initialization as native
import memory_readout

def main():
    s=load('element-initialization-semantics.json');functions,blocks=primary()
    assert s['primary_program_bytes']==functions and s['primary_vtable_data']==blocks
    assert len(functions)==22 and sum(f['executed'] for f in functions)==20 and len(blocks)==3
    assert bytes.fromhex(blocks[0]['hex'])==struct.pack('<I',0x4412C0)
    assert bytes.fromhex(blocks[1]['hex'])==struct.pack('<II',0x440A70,0x4409A0)
    by={(c['name'],page):(c,page) for c in native.cases() for page in range(3)};chunks={};expectations={}
    for row in s['native_replays']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c,page=by.pop((row['case'],row['page']))
        args,e=native.build(c,page);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['page']==page and saved['input']==args and saved['expected']==e
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        regions=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
        protected=[(f['address'],f['length']) for f in functions]+[(0x66CC94,0x160),(0x66CBDC,0x100)]
        assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected)
        chunks.setdefault(c['name'],{})[page]=memory_readout.decode(r['registers']);expectations[c['name']]=e
    assert not by
    for c in native.cases():
        b=b''.join(chunks[c['name']][i] for i in range(3))[:287]
        assert (ROOT/'ghidra'/('phase81-element-'+c['name']+'.bin')).read_bytes()==b
        e=expectations[c['name']];native.verify_node(c,b,e)
        if e['success']:
            assert list(struct.unpack_from('<3H',b,0x20))==e['plan']['constructor_angles']
            assert struct.unpack_from('<H',b,0xE2)[0]==123
    base=(ROOT/'ghidra/phase81-element-base.bin').read_bytes()
    for name in ['palette-random','palette-table','palette-both','id-high-slot-bits','no-cull-flag']:
        assert (ROOT/'ghidra'/('phase81-element-'+name+'.bin')).read_bytes()==base
    assert snapshots()==s['raw_snapshots'] and len(s['raw_snapshots'])==26 and sum(r['allocated'] for r in s['raw_snapshots'])==23
    assert corpus()==s['corpus'] and s['corpus']['unique_source_files']==82 and s['corpus']['new_exact_container_patches']==2460
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['element_color_semantics']['phase81_complete_initialization']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    assert package['shot_creation_semantics']['phase80_native_pool_and_cast']==load('native-shot-success-semantics.json')
    v=dict(phase=81,date='2026-10-28',logical_scenarios=26,native_emulation_invocations=78,
      successful_allocation_scenario_states=23,successful_allocation_replays=69,complete287byte_snapshots=26,unallocated_diagnostic_snapshots=3,
      original_PE_function_spans=22,executed_path_function_spans=20,static_only_function_spans=2,file_backed_vtable_ranges=3,
      source_files_hashchecked=82,source_file_records=48416,exact_container_patches=2460,source_executables_unchanged=4,package_components=67,
      original_code_or_vtable_replacements=0,raw_loaders_executed_in_new_emulations=0,palette_global_nonuse_proved=False,
      research_writes_to_original_game_data=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase81-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))

if __name__=='__main__':main()
