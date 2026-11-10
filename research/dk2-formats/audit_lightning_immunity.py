"""Verify King flag observations and both full native immunity gates."""
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_lightning_immunity import *
from emulate_lightning_immunity import cases,build_case
from build_lightning_immunity import sources,primary,OLD,CURRENT
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('creature-lightning-immunity.json');assert schema['observed_data']==sources() and schema['primary_program_bytes']==primary()
    for v in schema['versions']:assert digest(Path(v['file']).read_bytes())==v['sha256']
    assert digest((ROOT/'source-binaries/DKII-local13.exe').read_bytes())==schema['versions'][0]['sha256']
    assert len(schema['native_emulations'])==len(cases())==22
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    boundaries=0
    for flags in [0,1,2,3,15,0x80000000,0xFFFFFFFF]:
        for slot in ['game','editor']:
            raw=bytearray([165])*5537;struct.pack_into('<I',raw,OFFSETS[slot],flags)
            for enabled in [False,True]:
                out=set_immunity(bytes(raw),enabled,slot);want=flags|1 if enabled else flags&~1;o=OFFSETS[slot];assert flag_value(out,slot)==want and out[:o]==raw[:o] and out[o+4:]==raw[o+4:];boundaries+=1
    patches=0
    for row in schema['observed_data']:
        if 'king' not in row:continue
        path=Path(row['file']);b=path.read_bytes();parts=fmt.parse(b)
        for si,s in enumerate(parts):
            if s.signature!=170:continue
            for ri,raw in enumerate(s.records):
                if raw[0xF4B]!=21:continue
                for enabled in [False,True]:
                    new=set_immunity(raw,enabled);records=list(s.records);records[ri]=new;changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);changed.replace_records(records)
                    packed=b''.join(changed.pack() if j==si else q.pack() for j,q in enumerate(parts));want=bytearray(b);o=s.offset+len(s.header)+ri*len(raw)+0x1545;value=flag_value(raw)|1 if enabled else flag_value(raw)&~1;struct.pack_into('<I',want,o,value)
                    assert packed==want and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    instructions=0
    for e in schema['evidence']:
        lines=(ROOT/'ghidra'/e['file']).read_text().splitlines();assert all(s in lines for s in e['instructions']);instructions+=len(e['instructions'])
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=47 and package['creature_lightning_immunity']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=36,date='2026-10-26',native_emulations=22,native_runs_per_exe=11,boundary_cases=boundaries,exact_in_memory_container_patches=patches,primary_instructions=instructions,primary_ranges_matching_pe=len(schema['primary_program_bytes']),data_sources_unchanged=len(schema['observed_data']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,additional_executable_original_and_copy_match=schema['versions'][0]['sha256'],format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase36-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(result,ensure_ascii=True))
if __name__=='__main__':main()
