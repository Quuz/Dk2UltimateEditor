"""Native extension evidence replay and byte-preserving integration checks."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_creature_extension import *
from emulate_creature_extension import cases,build_case,pattern
from build_creature_extension import primary,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    s=load('creature-extension-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==38
    boundaries=0
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result'];assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
        c=v['case']
        if c['kind']=='writer':
            initial=pattern(c['seed'],0x3000);off=0x7FFF0000-0x1640+0x94-0x7FFED000+0x1545;predicted=writer_suffix(initial[off:off+84],c['flags'],c.get('context_address',0x10000000),b'A'*c['label_length']);memory=json.loads(args['memory'])['regions'];expected=next(bytes.fromhex(x['hex']) for x in memory if x['address']=='0x10014000');assert predicted==expected;boundaries+=1
    assert native_stack_tail(0x54F550)==bytes.fromhex('f5540010')
    for size in [5449,5537]:
        raw=pattern('pattern',size)
        for bit in range(32):
            for enabled in [False,True]:
                for slot in ['game']+(['editor'] if size==5537 else []):
                    off=GAME_FLAGS_OFFSET if slot=='game' else EDITOR_FLAGS_OFFSET;out=patch_flags(raw,slot,1<<bit,enabled);manual=bytearray(raw);old=struct.unpack_from('<I',raw,off)[0];struct.pack_into('<I',manual,off,old|(1<<bit) if enabled else old&~(1<<bit));assert out==manual and len(out)==size;boundaries+=1
        if size==5449:
            try:patch_flags(raw,'editor',1,True)
            except ValueError:boundaries+=1
            else:raise AssertionError('Implicit record extension')
    for n in [0,5448,5450,5536,5538]:
        try:view(bytes(n))
        except ValueError:boundaries+=1
        else:raise AssertionError('Unknown record width')
    long=pattern('pattern',5537)
    for seed in range(256):
        out=patch_opaque80(long,bytes([seed])*80);assert out[:5449]==long[:5449] and out[5449:5529]==bytes([seed])*80 and out[5529:]==long[5529:];boundaries+=1
    for args in [(bytes(5449),bytes(80)),(long,bytes(79)),(long,bytes(81))]:
        try:patch_opaque80(*args)
        except ValueError:boundaries+=1
        else:raise AssertionError('Wrong opaque edit')
    roundtrips=0;total=0;longs=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for sec in parts:
            if sec.signature!=170:continue
            for raw in sec.records:
                total+=1
                if len(raw)==5537:longs+=1;assert raw[0x159D:]==native_stack_tail(0x54F550)
    patches=0
    for p in [DATA/'Creatures.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        b=p.read_bytes();parts=fmt.parse(b);si=next(i for i,q in enumerate(parts) if q.signature==170);sec=parts[si]
        for ri,raw in enumerate(sec.records):
            edits=[]
            for slot in ['game']+(['editor'] if len(raw)==5537 else []):
                for mask in [1,2,4,8,0x80000000]:
                    for enabled in [False,True]:
                        off=GAME_FLAGS_OFFSET if slot=='game' else EDITOR_FLAGS_OFFSET;changed=patch_flags(raw,slot,mask,enabled);edits.append((changed,off,changed[off:off+4]))
            if len(raw)==5537:
                for seed in [0,255]:edits.append((patch_opaque80(raw,bytes([seed])*80),OPAQUE_OFFSET,bytes([seed])*80))
            for changed,off,value in edits:
                records=list(sec.records);records[ri]=changed;replacement=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);replacement.replace_records(records);packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts));wanted=bytearray(b);absolute=sec.offset+len(sec.header)+ri*len(raw)+off;wanted[absolute:absolute+len(value)]=value;assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])==54 and package['creature_extension_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=43,date='2026-10-26',native_emulations=38,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=boundaries,corpus_files=roundtrips,corpus_records=total,long_records=longs,opaque80_variants=len(s['corpus']['extension80_variants']),last4_variants=s['corpus']['last4_variants'],whole_file_roundtrips=roundtrips,exact_in_memory_container_patches=patches,primary_instructions=sum(len(e['instructions']) for e in s['evidence']),primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase43-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
