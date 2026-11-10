"""Evidence/corpus audit for phase23, without changes to original game data."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_mana_semantics import *
from emulate_mana_semantics import build_case

ROOT=Path(__file__).resolve().parent
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def digest(data):return hashlib.sha256(data).hexdigest()

def main():
    schema=load('mana-semantics.json');cases=0
    for bits in [0,1,127,128,32767,32768,65526,65535]:
        expected=struct.unpack('<h',struct.pack('<H',bits))[0];assert s16(bits)==expected;cases+=1
        for flags in [0,64,128,255]:
            for word,request in [(0,1),(1,2),(1,0),(1,1),(0xA001,2),(1,0x1001),(1,0x10001)]:
                p=ownership_deltas(word,request,flags,bits,bits)
                old=word&4095;new=request&4095;changed=old!=(request&65535)
                assert p['owner_word']==((word&0xF000)|new if changed else word)
                assert len(p['updates'])==((2 if old else 1) if changed else int(bool(old and flags&128)))
                assert all(abs(u['gain'])==abs(expected) and u['gain']==u['ceiling'] for u in p['updates']);cases+=1
        for adding in [True,False]:
            p=trap_upkeep(bits,100,-100,adding=adding);amount=expected*(1 if adding else -1)
            assert p==dict(maintain=s32(100+amount),extra=s32(-100-amount));cases+=1
    for old,new,of,nf,owner,wf,delta in [
        (8,9,0,0,1,0,4),(8,9,0,0,1,64,0),(8,9,0,0,0,0,0),
        (0,9,0,0,1,0,5),(8,8,0,0,1,0,0),(8,17,0,16,0,64,1),
        (17,26,16,16,0,64,1),(17,8,16,0,0,64,-1),(8,10,0,16,1,0,0)]:
        p=terrain_change_delta(old,new,of,nf,owner,wf,1,5);assert p['gain_delta']==delta and p['ceiling_delta']==0;cases+=1
    for extra,basic,cost,one,limit in [(20,100,30,0,200),(-10,100,30,0,80),(1,2,3,4,-1),(-100,0,0,0,0),(0x7FFFFFFF,1,0,0,999)]:
        total=struct.unpack('<i',struct.pack('<I',(extra+basic+cost+one)&0xFFFFFFFF))[0]
        assert gross_income(extra,basic,cost,one,limit)==min(total,limit);cases+=1
    p=tick_arithmetic(-10,100,30,30,80,2,100,1000);assert p['gross']==80 and p['delta']==100 and p['mana']==200;cases+=1
    p=tick_arithmetic(-200,0,30,30,100,3,10,1000);assert p['delta']==-200 and p['mana']==0;cases+=1
    p=tick_arithmetic(20,100,30,31,200,1,100,1000);assert p['delta']==119 and p['mana']==219;cases+=1
    p=tick_arithmetic(0x40000000,0,0,0,0x7FFFFFFF,1,0,0x7FFFFFFF,3,0);assert p['delta']==-0x20000000 and p['mana']==0;cases+=1
    for last,now,tps,expected in [(100,119,20,False),(100,120,20,True),(0xFFFFFFF0,4,20,True),(100,100,0,True)]:
        assert income_due(last,now,tps)==expected;cases+=1
    for action in [lambda:s16(-1),lambda:s16(65536),lambda:s16(True),lambda:ownership_deltas(65536,0,0,0,0),
        lambda:terrain_change_delta(256,0,0,0,0,0,0,0),lambda:gross_income(1<<31,0,0,0,0),lambda:income_due(0,-1,20),
        lambda:trap_upkeep(0,0,0,adding=1),lambda:tick_arithmetic(0,0,0,0,0,0,0,0,setting=65536)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid native input accepted')
        cases+=1
    excerpts=0;instructions=0;evidence_hashes={}
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                p=ROOT/'ghidra'/v['file'];lines=p.read_text(encoding='utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(line in lines for line in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=digest(p.read_bytes())
            for child in v.values():walk(child)
        elif isinstance(v,list):
            for child in v:walk(child)
    walk(schema)
    emulations=schema['native_emulations'];assert len(emulations)==62
    for row in emulations:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text())
        args,expected=build_case(saved['case']);assert args==saved['input'] and expected==saved['expected_registers']
        assert saved['result']['success'] and saved['result']['hit_return']
        for register,value in expected.items():assert int(saved['result']['registers'][register],16)==value
    compared=[]
    for before,after in [
        ('terrain-mana-node-update-assembly','terrain-owner-after'),('terrain-mana-node-helper-assembly','terrain-change-after'),
        ('player-mana-income-assembly','player-income-after'),('player-mana-tick-assembly','player-preview-after'),('player-mana-helper-source-assembly','player-cost-after')]:
        a=ROOT/'ghidra'/f'phase23-{before}.txt';b=ROOT/'ghidra'/f'phase23-{after}.txt'
        assert a.read_bytes()==b.read_bytes();compared.append(before)
    counts={family:Counter() for family in ['Terrain','Traps']};files=[];records=Counter();negative=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family'] in counts for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];seen=Counter()
        for section in fmt.parse(b):
            family=fmt.FAMILIES.get(section.signature)
            if family not in counts:continue
            for r in section.records:
                if family=='Terrain':
                    gain,ceiling=struct.unpack_from('<hh',r,0x19C);counts[family][(gain,ceiling)]+=1
                    bits=struct.unpack_from('<HH',r,0x19C);p=ownership_deltas(1,2,0,*bits)
                    assert p['updates']==[dict(owner=1,gain=-gain,ceiling=-ceiling),dict(owner=2,gain=gain,ceiling=ceiling)]
                    negative[family]+=gain<0 or ceiling<0
                else:
                    usage=struct.unpack_from('<h',r,0x202)[0];counts[family][usage]+=1
                    p=trap_upkeep(struct.unpack_from('<H',r,0x202)[0],0,0)
                    assert p==dict(maintain=usage,extra=-usage);negative[family]+=usage<0
                records[family]+=1;seen[family]+=1
        files.append(dict(file=file['file'],records=dict(seen),sha256=file['sha256']))
    patches=0
    for path in [DATA/'Terrain.kwd',DATA/'Traps.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            offsets=[0x19C,0x19E] if section.signature==110 else [0x202] if section.signature==130 else []
            for ri,record in enumerate(section.records):
                for offset in offsets:
                    for value in [0,1,32767,32768,65526,65535]:
                        replacement=struct.pack('<H',value);records_copy=list(section.records);r=bytearray(record);r[offset:offset+2]=replacement;records_copy[ri]=bytes(r)
                        changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);changed.replace_records(records_copy)
                        packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections))
                        expected=bytearray(original);absolute=section.offset+len(section.header)+ri*len(record)+offset;expected[absolute:absolute+2]=replacement
                        assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert package['mana_semantics']==schema and len(package['components'])>=34
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=23,date='2026-10-25',boundary_cases=cases,native_emulations=len(emulations),exact_in_memory_container_patches=patches,
        modified_gameplay_runs=0,primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=evidence_hashes,unchanged_disassembly=compared),
        corpus=dict(files=len(files),records=dict(records),negative_records=dict(negative),
            terrain_gain_ceiling_counts=[dict(gain=k[0],ceiling=k[1],count=v) for k,v in sorted(counts['Terrain'].items())],
            trap_upkeep_counts=dict(sorted(counts['Traps'].items())),file_details=files),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=len(package['components']),limitations=schema['limitations'])
    (ROOT/'phase23-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'files;',dict(records),'; negative records:',dict(negative))
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;5 disassemblies unchanged')

if __name__=='__main__':main()
