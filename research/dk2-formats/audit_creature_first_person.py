"""Audit first-person PE evidence, native runs, corpus and lossless container patches."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from native_creature_first_person import *
from emulate_creature_first_person import build_case

ROOT=Path(__file__).resolve().parent
def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('creature-first-person-semantics.json');boundaries=0
    # Independent signed division through sign correction plus arithmetic shifts.
    def divpow(v,n):return (v+((1<<n)-1 if v<0 else 0))>>n
    assert load('phase25-native-sine-table.json')==schema['phase_table']
    table=schema['phase_table']['values']
    for amp in [0,1,4095,4096,4097,65536,0x80000000,0xFFFFFFFF]:
        for cycle in [0,1,2,4,12,255]:
            for phase in [0,1,2,6,13,0xFFFFFFFF]:
                for half in [False,True]:
                    p=(cycle*phase)&0xFFFFFFFF;w=(((p<<7)&0xFFFFFFFF)>>2)&2047;o=(((p<<8)&0xFFFFFFFF)>>2)&2047
                    wp=struct.unpack('<i',struct.pack('<I',(table[w]*amp)&0xFFFFFFFF))[0];op=struct.unpack('<i',struct.pack('<I',(table[o]*amp)&0xFFFFFFFF))[0]
                    osc=divpow(divpow(op,7),12);osc=divpow(osc,1) if half else osc
                    assert motion_terms(amp,amp,cycle,phase,half)==dict(waddle_index=w,oscillate_index=o,input_waddle=divpow(divpow(wp,9),12),action80_waddle=divpow(divpow(wp,12),12),oscillate=osc);boundaries+=1
    assert motion_terms(4096,4096,4,4)==dict(waddle_index=512,oscillate_index=1024,input_waddle=128,action80_waddle=16,oscillate=0);boundaries+=1
    assert motion_terms(4096,4096,4,6,True)['oscillate']==-256;boundaries+=1
    slots=[[0,0],[7,2],[9,1]]
    for raw,factor,commands in [(6144,1,0),(8191,1,0),(8192,2,2),(11878,2,2),(12288,3,2)]:
        for flags in [0,0x80000000]:
            r=sniper_commands(raw,2,slots,3,4096,flags)
            assert r['integer_factor']==factor and len(r['commands'])==commands
            if commands:assert r['commands'][0]['value']==(4096 if flags else 4096*factor)
            boundaries+=1
    for count,spell in [(0,None),(1,None),(2,7),(3,7),(255,7)]:
        assert sniper_commands(8192,2,slots,count,4096,0)['selected_spell']==spell;boundaries+=1
    for flags in [0,0x80000000,0xFFFFFFFF,0xA5A5A5A5]:
        for value in [0,1,2,3,0xFFFFFFFF]:
            assert sniper_flag_after_action(flags,value)&0x7FFFFFFF==flags&0x7FFFFFFF
            assert sniper_flag_after_action(flags,value)>>31==value&1
            assert sniper_flag_after_action(flags,value,False)==flags;boundaries+=1
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
    game=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();tablebytes=pe_read(game,0x6C1488,8192)
    assert digest(tablebytes)==schema['phase_table']['sha256'] and list(struct.unpack('<2048i',tablebytes))==table
    for row in schema['vtables']:assert pe_read(game,row['address'],4).hex()==row['bytes']
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        for reg,value in expected.items():assert int(saved['result']['registers'][reg],16)==value
    assert len(schema['native_emulations'])==65
    unchanged=[]
    for name in ['sniper-zoom','first-person-input','camera-getter','sniper-action','creature-slot-getter','world-e8','dispatch-action']:
        before=ROOT/'ghidra'/('phase25-'+name+'-assembly.txt');after=ROOT/'ghidra'/('phase25-'+name+'-after.txt')
        assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(before.name)
    total=0;counts={name:Counter() for name,_ in FIELDS.values()};factors=Counter();fractional=0;eligible_names=Counter();files=[]
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='Creatures' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];n=0
        for section in fmt.parse(b):
            if section.signature!=170:continue
            for r in section.records:
                view=record_view(r);n+=1;total+=1
                for key in counts:counts[key][view[key]]+=1
                factors[view['sniper_integer_factor']]+=1;fractional+=bool(view['SniperZoomMultiplier']&4095)
                if view['sniper_multiplier_gate']:eligible_names[(fmt.string(r[:32]),view['SniperZoomMultiplier'])]+=1
        if n:files.append(dict(file=file['file'],records=n,sha256=file['sha256']))
    patches=0
    for path in [DATA/'Creatures.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            if section.signature!=170:continue
            for ri,record in enumerate(section.records):
                for off,(_,width) in FIELDS.items():
                    for value in ([0,1,12,255] if width==1 else [0,4096,8191,8192,11878,0xFFFFFFFF]):
                        r=patch_raw(record,off,value);replacement=value.to_bytes(width,'little');assert r[:off]==record[:off] and r[off+width:]==record[off+width:]
                        records=list(section.records);records[ri]=r;changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);changed.replace_records(records)
                        packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections));expected=bytearray(original)
                        absolute=section.offset+len(section.header)+ri*len(record)+off;expected[absolute:absolute+width]=replacement
                        assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=36 and package['creature_first_person_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=25,date='2026-10-25',boundary_cases=boundaries,native_emulations=65,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        corpus=dict(creature_files=len(files),creature_records=total,file_details=files,raw_value_counts={k:dict(sorted(v.items())) for k,v in counts.items()},
            sniper_integer_factor_counts=dict(sorted(factors.items())),fractional_raw_zoom_records=fractional,
            multiplier_gate_records=[dict(name=k[0],raw_zoom=k[1],records=v) for k,v in sorted(eligible_names.items())]),
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=evidence_hashes,unchanged_disassembly=unchanged,table_sha256=digest(tablebytes)),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=36,limitations=schema['limitations'])
    (ROOT/'phase25-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'files;',total,'creatures; integer factors',dict(factors),'fractional raw',fractional)
    print('Multiplier gate',json.dumps(result['corpus']['multiplier_gate_records']))
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(unchanged),'unchanged disassemblies')
if __name__=='__main__':main()
