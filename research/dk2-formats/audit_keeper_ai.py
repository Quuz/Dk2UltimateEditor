"""Audit native Keeper flag/AI evidence and raw-preserving catalogue operations."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from extract_editor_dialogs import extract
from native_keeper_ai import *
from emulate_keeper_ai import build_case
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('keeper-ai-semantics.json');boundaries=0
    # Prove the unsigned reciprocal formula over the full u32 input domain:
    # n=q*d+r; q*delta+r*m<K ensures floor(n*m/K)=q.
    d=2499;m=0xD1CC931B;K=1<<43;delta=d*m-K;qmax=0xFFFFFFFF//d
    assert delta>=0 and qmax*delta+(d-1)*m<K
    for cost in [0,1,2498,2499,2500,4997,4998,9996,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        for mode,extra in [('defensive',1),('attacking',2)]:assert random_bound(cost,mode)==cost//2499+extra;boundaries+=1
    for low in range(32):
        for high in [0,0x80000000,0xFFFFFFE0]:
            flags=low|high
            assert editor_saved_flags(flags)==low&30 and game_flags(flags,1)==flags|1;boundaries+=1
            for mode,mask in [('defensive',8),('attacking',4)]:
                assert eligible_flags(flags,mode)==bool((flags&mask) and not(flags&16));boundaries+=1
    spells=[dict(flags=12,cost=0),dict(flags=12,cost=0)]
    for power in range(8):
        de=select_spell('defensive',spells,power,[0,0]);at=select_spell('attacking',spells,power,[0,0])
        assert de['selected_id']==(1 if power==4 else 2)
        assert at['selected_id']==(1 if power==4 else 0);boundaries+=2
    assert select_spell('defensive',[dict(flags=8,cost=9996),dict(flags=8,cost=0)],0,[4,0])['selected_id']==1;boundaries+=1
    assert select_spell('defensive',[dict(flags=8,cost=0),dict(flags=8,cost=9996)],0,[0,4])['selected_id']==2;boundaries+=1
    assert not eligible_flags(0x14,'attacking') and eligible_flags(0x04,'attacking');boundaries+=1
    evidence_hashes={};excerpts=0;instructions=0
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                p=ROOT/'ghidra'/v['file'];lines=p.read_text().splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(s in lines for s in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=digest(p.read_bytes())
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(schema)
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes();dialogs=extract(editor)
    dialog=next(r for r in dialogs if r['resource_path']==schema['native_editor']['dialog_resource_path'])
    for row in schema['native_editor']['model_bools']:
        assert next(c for c in dialog['controls'] if c['id']==row['control']['id'])==row['control']
    for constant in schema['opaque_c8']['scale_constants']:assert pe_read(editor,constant['address'],8).hex()==constant['bytes']
    assert all('+ 0x178)' not in line for line in (ROOT/'ghidra/phase26-keeper-actual-451cc0.txt').read_text().splitlines())
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        for reg,value in expected.items():assert int(saved['result']['registers'][reg],16)==value
    assert len(schema['native_emulations'])==48
    unchanged=[]
    for before,after in [('phase26-keeper-ai-assembly','phase26-keeper-ai-after'),('phase26-keeper-flags-encode-assembly','phase26-keeper-flags-encode-after'),
        ('phase26-keeper-flags-decode-assembly','phase26-keeper-flags-decode-after'),('phase26-keeper-game-converter-assembly','phase26-keeper-game-converter-after'),
        ('game-keeperspell-load-assembly','phase26-keeper-game-loader-after')]:
        assert (ROOT/'ghidra'/(before+'.txt')).read_text().rstrip()==(ROOT/'ghidra'/(after+'.txt')).read_text().rstrip();unchanged.append(before)
    total=0;flags=Counter();c8=Counter();projected=0;details=[];flag_gates=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='KeeperSpells' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];n=0
        for section in fmt.parse(b):
            if section.signature!=150:continue
            for r in section.records:
                v=record_view(r);n+=1;total+=1;flags[v['raw_flags']]+=1;c8[v['opaque_c8']]+=1;projected+=v['editor_saved_flags']!=v['raw_flags']
                flag_gates[(fmt.string(r[:32]),v['raw_flags'],v['attacking_flag_gate'],v['defensive_flag_gate'])]+=1
        if n:details.append(dict(file=file['file'],records=n,sha256=file['sha256']))
    patches=0
    for path in [DATA/'KeeperSpells.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            if section.signature!=150:continue
            for ri,record in enumerate(section.records):
                for value in [0,1,4,8,16,31,0xFFFFFFFF]:
                    r=patch_flags(record,value);assert r[:0xDC]==record[:0xDC] and r[0xE0:]==record[0xE0:]
                    records=list(section.records);records[ri]=r;changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);changed.replace_records(records)
                    packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections));expected=bytearray(original)
                    absolute=section.offset+len(section.header)+ri*len(record)+0xDC;expected[absolute:absolute+4]=value.to_bytes(4,'little')
                    assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=37 and package['keeper_ai_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=26,date='2026-10-26',boundary_cases=boundaries,native_emulations=48,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        reciprocal_proof=dict(divisor=d,multiplier=m,denominator=K,delta=delta,qmax=qmax,upper_error_numerator=qmax*delta+(d-1)*m),
        corpus=dict(keeper_files=len(details),keeper_records=total,file_details=details,raw_flags_counts=dict(sorted(flags.items())),raw_c8_counts=dict(sorted(c8.items())),native_editor_projection_changes=projected,
            flag_gate_details=[dict(name=k[0],flags=k[1],attacking_gate=k[2],defensive_gate=k[3],records=v) for k,v in sorted(flag_gates.items())]),
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=evidence_hashes,unchanged_disassembly=unchanged,dialog_controls_verified=4),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=37,limitations=schema['limitations'])
    (ROOT/'phase26-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(details),'files;',total,'KeeperSpells; flags',dict(flags),'unknown-bit changes',projected,'C8',dict(c8))
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(unchanged),'unchanged disassemblies')
if __name__=='__main__':main()
