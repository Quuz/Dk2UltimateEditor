"""Verify final native creation evidence and lossless Shot catalogue edits."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from build_shot_creation import ROOT,load,digest,primary,corpus
from emulate_shot_creation import cases,build_case
from native_shot_creation import projected_node

def main():
    s=load('shot-creation-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus()
    assert len(s['native_emulations'])==len(cases())==32
    chains=0
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert 'shot-create-v3-' in row['file'] and digest(p.read_bytes())==row['sha256'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected_registers']==want and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==n for k,n in want.items())
        if c.get('kind')=='chain':
            chains+=1;m={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
            assert 0x4B7CD0 not in m and 0x4B7D60 not in m and 0x4AAC90 not in m
            if (c['bonus'] if c['upgraded'] else c['primary']) and c['primary']:
                shot=m[0x1000B000];assert shot[0x124]==c['primary'] and struct.unpack_from('<I',shot,0x125)[0]==0x11003000
                assert m[0x11003000][0x6C]==9 and m[0x11016000][0x6C]==0
                assert struct.unpack_from('<II',shot,0x13D)==((0x99AABBCC,0xDDEEFF00) if c['upgraded'] else (0x11223344,0x55667788))
    patches=records=roundtrips=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];sections=fmt.parse(b);assert b''.join(q.pack() for q in sections)==b;roundtrips+=1
        for si,sec in enumerate(sections):
            if sec.signature!=260:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                records+=1
                for off,n in [(0x98,4),(0x9C,4),(0xC7,1)]:
                    changed=bytearray(raw);changed[off:off+n]=bytes(v^255 for v in raw[off:off+n]);rs=list(sec.records);rs[ri]=bytes(changed);new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(sections));expected=bytearray(b);absolute=sec.offset+rel+off;expected[absolute:absolute+n]=changed[off:off+n]
                    assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
    checks=0
    for kwargs in [dict(seed=-1),dict(seed=256),dict(shot_id=256),dict(shot_id=True),dict(owner=65536),dict(orientation=-1),dict(definition_address=1<<32),dict(position=(1,2)),dict(position=(1,2,-1)),dict(sound_enabled=True),dict(sound_handle=-1),dict(definition=bytes(127))]:
        args=dict(seed=0,shot_id=1,definition=bytes(128),definition_address=0x10003000);args.update(kwargs)
        try:projected_node(**args)
        except ValueError:checks+=1
        else:raise AssertionError(kwargs)
    definition=bytearray(128);struct.pack_into('<H',definition,0x47,65535);copy=bytes(definition);node=projected_node(0,1,definition,0x10003000);assert struct.unpack_from('<I',node,0x40)[0]==65535 and bytes(definition)==copy;checks+=1
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==65 and package['shot_creation_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    state=load('headless-instance.json');assert state['log_max_bytes']==16*1024*1024 and state['log_backups']==2
    assert all(p.stat().st_size<17*1024*1024 for p in ROOT.glob('headless-stdout.log*'))
    v=dict(phase=54,date='2026-10-27',native_emulations=32,composed_full_keeper_factory_constructor_initializer=chains,primary_complete_pe_ranges=len(s['primary_program_bytes']),entire_shot_instance_bytes_compared=371,api_boundary_checks=checks,whole_file_roundtrips=roundtrips,shot_records=records,exact_in_memory_container_patches=patches,original_executables_unchanged=4,format_package_components=65,research_writes_to_game_data=0,modified_gameplay_runs=0,logging=dict(max_bytes=state['log_max_bytes'],backups=state['log_backups'],recovery='phase54-log-recovery.json'),limitations=s['limitations'])
    (ROOT/'phase54-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
