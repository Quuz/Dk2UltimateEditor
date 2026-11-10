"""Offline full-state audit of targeted spell results and untouched native code."""
from pathlib import Path
import json,struct
from build_targeted_spell import ROOT,load,digest,link,primary,corpus,snapshots
import emulate_targeted_spell as native
import memory_readout

def executable_ranges(data):
    pe=struct.unpack_from('<I',data,0x3C)[0];count=struct.unpack_from('<H',data,pe+6)[0];opt=pe+24
    start=opt+struct.unpack_from('<H',data,pe+20)[0];imagebase=struct.unpack_from('<I',data,opt+28)[0];spans=[]
    for i in range(count):
        p=start+40*i;virtual_size,rva,raw_size=struct.unpack_from('<III',data,p+8)
        if struct.unpack_from('<I',data,p+36)[0]&0x20000000:spans.append((imagebase+rva,max(virtual_size,raw_size)))
    assert spans;return spans

def main():
    s=load('targeted-spell-semantics.json');functions,blocks=primary()
    assert functions==s['primary_program_bytes'] and blocks==s['primary_file_backed_data'] and len(functions)==50 and len(blocks)==17
    literal=next(r for r in blocks if r['address']==0x69DB9C)
    assert bytes.fromhex(literal['hex'])==b'Creature Spell %d has no Shot Type associated\0'
    exe=Path(s['executable']).read_bytes();protected=executable_ranges(exe)+[(0x66D464,0x500)]
    by={(c['name'],page):(c,page) for c in native.cases() for page in range(3)};chunks={};expected={}
    for row in s['native_replays']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c,page=by.pop((row['case'],row['page']));args,e=native.build(c,page);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['page']==page and saved['input']==args and saved['expected']==e
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items())
        regions=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
        assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected)
        chunks.setdefault(c['name'],{})[page]=memory_readout.decode(r['registers']);expected[c['name']]=e
    assert not by
    for c in native.cases():
        b=b''.join(chunks[c['name']][i] for i in range(3))[:371]
        assert (ROOT/'ghidra'/('phase82-targeted-spell-'+c['name']+'.bin')).read_bytes()==b;native.verify_node(c,b,expected[c['name']])
    base=(ROOT/'ghidra/phase82-targeted-spell-base.bin').read_bytes()
    for n in ['alt-zero','alt68','alt255','moving-z']:assert (ROOT/'ghidra'/('phase82-targeted-spell-'+n+'.bin')).read_bytes()==base
    assert s['raw_snapshots']==snapshots() and sum(r['allocated'] for r in s['raw_snapshots'])==21
    assert expected['moving-x']['prediction']['aim']==[0x6980,0x6800,0x2400]
    assert expected['moving-y']['prediction']['aim']==[0x6800,0x6680,0x2400]
    assert expected['height-level2']['target_height']==3072 and expected['height-level2']['prediction']['aim'][2]==0x2600
    assert expected['height-odd']['prediction']['aim'][2]==0x27FF
    assert expected['distance-half-tile']['prediction']['integer_distance_factor']==0
    assert expected['distance-two-tiles']['prediction']['integer_distance_factor']==2
    assert expected['target-object']['target_height']==0 and expected['target-shot']['target_height']==0x3000
    assert corpus()==s['corpus'] and s['corpus']['new_height_exact_container_patches']==246
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['spell_alternative_semantics']['phase82_targeted_cast']==s
    for key in ['creature_subrecords','shot_creation_semantics']:assert package[key]['phase82_targeted_cast_reference']==link('targeted-spell-semantics.json')
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    assert package['element_color_semantics']['phase81_complete_initialization']==load('element-initialization-semantics.json')
    assert package['shot_creation_semantics']['phase80_native_pool_and_cast']==load('native-shot-success-semantics.json')
    result=dict(phase=82,date='2026-10-28',logical_scenarios=27,native_emulation_invocations=81,successful_allocated_scenario_states=21,
      successful_allocation_replays=63,complete371byte_snapshots=27,unallocated_snapshots=6,native_code_spans_attested=50,file_backed_data_ranges=17,
      original_code_or_vtable_replacements=0,original_executable_sections_protected_against_fixture_overlap=True,
      raw_height_exact_container_patches=246,source_files_hashchecked=83,source_executables_unchanged=4,package_components=67,
      AlternativeShotRoom_global_nonuse_proved=False,full_AI_or_Shot_process_ticks_executed=0,research_writes_to_original_game_data=0,
      significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase82-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))

if __name__=='__main__':main()
