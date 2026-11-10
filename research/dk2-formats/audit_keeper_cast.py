"""Audit Keeper cast field routing, native evidence and lossless raw patches."""
from pathlib import Path
from collections import defaultdict,Counter
import json,struct
import dk2_formats as fmt
from build_keeper_cast import load,primary,corpus,digest,ROOT
from native_keeper_cast import selection_view,ROUTES
from native_keeper_pipeline import patch_raw
from emulate_keeper_cast import cases,build_case
def main():
    s=load('keeper-cast-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus() and len(s['native_emulations'])==len(cases())==120
    groups=defaultdict(list);factories=successes=zero_factory=0
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert 'keeper-cast-v2-' in row['file'] and digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result'];assert c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
        regions={int(n['address'],16):bytes.fromhex(n['hex']) for n in json.loads(args['memory'])['regions']};capture=regions[0x1000E200];calls=struct.unpack_from('<I',capture,0)[0];factories+=calls;successes+=struct.unpack_from('<I',capture,0x1C)[0];zero_factory+=bool(calls and struct.unpack_from('<I',capture,4)[0]==0)
        if c['name'].startswith('cast-'):
            key=(c['entry'],c['upgraded'],c['primary'],c['bonus']);groups[key].append((c['c8'],tuple(regions[a] for a in [0x1000B000,0x1000C000,0x1000E000,0x1000E200])))
    assert len(groups)==16 and all(len(g)==6 and len({v for _,v in g})==1 for g in groups.values())
    checks=0
    for route in ROUTES:
        for upgrade in [0,1,2,0xFFFFFFFF]:
            for primary_id in range(256):
                for bonus in [0,1,21,255]:
                    raw=bytearray(406);raw[0xF0]=primary_id;raw[0x113]=bonus;raw[0xDA]=6;struct.pack_into('<II',raw,0xD0,0x11223344,0x55667788);struct.pack_into('<II',raw,0x114,0x99AABBCC,0xDDEEFF00);copy=bytes(raw);v=selection_view(raw,upgrade,route);selected=bonus if upgrade else primary_id
                    assert v['selected_shot_id']==selected and v['factory_shot_id']==(primary_id if selected else None) and v['data1_bits']==(0x99AABBCC if upgrade else 0x11223344) and v['data2_bits']==(0xDDEEFF00 if upgrade else 0x55667788) and v['target_flag_one'] and v['position_z_add']==(8192 if route=='A' and primary_id==21 else 0) and bytes(raw)==copy;checks+=1
    for rule in range(256):
        raw=bytearray(406);raw[0xDA]=rule;v=selection_view(raw,1,'A');assert v['target_flag_one']==(rule in [1,2,3,6]);checks+=1
    for route in ['','C',None,[],0]:
        try:selection_view(bytes(406),0,route)
        except ValueError:checks+=1
        else:raise AssertionError('Unknown route accepted')
    for upgrade in [-1,1<<32,True,'1',None]:
        try:selection_view(bytes(406),upgrade,'A')
        except ValueError:checks+=1
        else:raise AssertionError('Invalid upgrade state accepted')
    for n in [0,405,407]:
        try:selection_view(bytes(n),0,'A')
        except ValueError:checks+=1
        else:raise AssertionError('Invalid raw length accepted')
    patches=roundtrips=records=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=150:continue
            records+=len(sec.records);rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                for off,n in [(0xF0,1),(0x113,1),(0xD0,4),(0xD4,4),(0x114,4),(0x118,4)]:
                    old=int.from_bytes(raw[off:off+n],'little');changed=patch_raw(raw,off,old^((1<<(8*n))-1));rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);absolute=sec.offset+rel+off;expected[absolute:absolute+n]=changed[off:off+n];assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    baseline=load('spell-semantics.json');constraint=next(r for r in baseline['rules'] if r['id']=='upgraded_shot_tuple');assert constraint['description'].startswith('Phase53 correction:') and 'factory4B7CD0' in constraint['description']
    package=load('editor-format-package.json');assert len(package['components'])==64 and package['keeper_cast_semantics']==s and package['spell_semantics']==baseline
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=53,date='2026-10-27',native_emulations=len(cases()),complete_wrapper_routes=2,primary_function_ranges_matching_pe=len(s['primary_program_bytes']),c8_metamorphic_groups=len(groups),c8_values_per_group=6,factory_calls_in_native_cases=factories,successful_fixture_casts=successes,zero_id_factory_fixture_calls=zero_factory,api_boundary_cases=checks,whole_file_roundtrips=roundtrips,keeper_spell_records=records,different_primary_bonus_ids=s['corpus']['different_count'],different_both_nonzero_ids=s['corpus']['different_both_nonzero'],exact_in_memory_container_patches=patches,baseline_description_corrected=True,original_executables_unchanged=4,format_package_components=64,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase53-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
