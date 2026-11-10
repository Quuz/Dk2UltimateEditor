"""Whole-function attestation for two exact Terrain loader profiles.

Executed roots/direct callees are separate from static-only callbacks. An old
interior-function hypothesis was removed; the canonical512820 callback is used.
"""
from pathlib import Path
from functools import cache
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import emulate_terrain_opaque_table as native

ROOT=Path(__file__).resolve().parent
STATIC={'DX':[0x51EB50],'old':[0x512820]}

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)

@cache
def traces(profile):
    out=[]
    for row in load('phase134-Terrain-table-emulations.json')['replays']:
        saved=load(row['file'])
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        if saved['case']['profile']==profile:out.append(saved)
    return out

def visited(profile):
    return {int(a,16) for s in traces(profile) for a in s['result']['visited_instructions'] if 0x400000<=int(a,16)<0x670000}

def entries(profile):
    blob=native.PROFILES[profile]['exe'].read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);out=set()
    for s in traces(profile):
        out.add(int(s['input']['address'],16));out.update(int(c['address'],16) for c in s['input']['before_calls'])
    for pc in visited(profile):
        ins=next(md.disasm(pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.op_str.startswith('0x') and 0x400000<=int(ins.op_str,16)<0x670000:
            out.add(int(ins.op_str,16))
    return sorted(out)

def spans(profile):
    blob=native.PROFILES[profile]['exe'].read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for address in sorted(set(entries(profile)+STATIC[profile])):
        name=f'ghidra/phase134-primary-{profile}-{address:06x}-assembly.txt'
        text=(ROOT/name).read_text(encoding='utf8');pcs=[int(s,16) for s in re.findall(r'^([0-9A-Fa-f]{8}):',text,re.M)]
        assert pcs and pcs[0]==address,(profile,hex(address),text[:160])
        last=next(md.disasm(pe_read(blob,pcs[-1],15),pcs[-1]));size=last.address+last.size-address
        cap=[i.address for i in md.disasm(pe_read(blob,address,size),address)]
        split=[p for p in cap if p not in pcs]
        assert all(p-1 in pcs and pe_read(blob,p-1,3)==bytes.fromhex('9bd97d') for p in split),(profile,hex(address),split)
        assert [p for p in cap if p not in split]==pcs,(profile,hex(address))
        rows.append(dict(address=address,length=size,assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),
            static_only=address in STATIC[profile],explicit_FWAIT_split_addresses=split,instruction_addresses=sorted(set(pcs+split))))
    return rows

def build():
    profiles={}
    for profile,p in native.PROFILES.items():
        blob=p['exe'].read_bytes();rows=spans(profile);covered=set()
        for row in rows:
            name=f'ghidra/phase134-primary-{profile}-{row["address"]:06x}-memory.txt'
            saved=load(name);data=bytes(saved['data'])
            assert int(saved['address'],16)==row['address'] and len(data)==row['length'] and data==pe_read(blob,row['address'],row['length'])
            row.update(memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(data))
            covered.update(row.pop('instruction_addresses'))
        pcs=visited(profile);assert pcs<=covered,[hex(p) for p in sorted(pcs-covered)[:20]]
        profiles[profile]=dict(executable=str(p['exe']),sha256=sha(blob),functions=rows,
            executed_function_spans=len(entries(profile)),static_only_function_spans=len(STATIC[profile]),
            unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True)
    return dict(phase=134,profiles=profiles,scope=__doc__,metadata_correction=dict(
        rejected_interior_address=0x512830,canonical_entry=0x512820,
        evidence='phase134-oldTerrain-metadata-correction-queries.json',original_EXE_changed=False,
        note='Interior512830 used incoming EAX/ESI. Primary bytes show prior512800 RET at51281D then NOPs; canonical prologue starts512820. Old callback static-only, not part of native12 compositions.'))

if __name__=='__main__':
    mode=sys.argv[1] if len(sys.argv)>1 else ''
    if mode in ['--queries','--memory']:
        for profile,p in native.PROFILES.items():
            if mode=='--queries':
                qs=[]
                for a in sorted(set(entries(profile)+STATIC[profile])):
                    for tool,suffix in [('create_function','create'),('disassemble_function','assembly')]:
                        args=dict(program=p['program'],address=hex(a))
                        if tool=='create_function':args['disassemble_first']=True
                        qs.append(dict(id=f'phase134-primary-{profile}-{a:06x}-{suffix}',tool=tool,args=args,print_limit=0))
                name=f'phase134-primary-{profile}-assembly-queries.json'
            else:
                qs=[dict(id=f'phase134-primary-{profile}-{r["address"]:06x}-memory',tool='read_memory',
                    args=dict(program=p['program'],address=hex(r['address']),length=r['length']),print_limit=0) for r in spans(profile)]
                name=f'phase134-primary-{profile}-memory-queries.json'
            (ROOT/name).write_text(json.dumps(qs,indent=2),encoding='utf8');print(profile,len(qs),name)
    else:
        value=build();(ROOT/'phase134-Terrain-table-primary.json').write_text(json.dumps(value,indent=2),encoding='utf8')
        print({p:(v['executed_function_spans'],v['static_only_function_spans'],v['unique_executed_original_PCs']) for p,v in value['profiles'].items()})
