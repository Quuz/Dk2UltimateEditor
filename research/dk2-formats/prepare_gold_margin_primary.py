"""Byte attestation of original registration, raw loaders and gold predicate."""
from pathlib import Path
import json,re
import emulate_gold_margin as native
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha,load,dump=native.sha,native.load,native.dump
EXTRA=[0x51AA90,0x51DC40,0x464360]


def validate(row,data):
    row=dict(row);a=row['address'];n=row['length'];p=ROOT/row['memory'];s=load(row['memory']);raw=pe_read(data,a,n)
    assert s['length']==n and bytes.fromhex(s['hex'])==bytes(s['data'])==raw
    row['memory_sha256']=sha(p.read_bytes());row['bytes_sha256']=sha(raw)
    return row


def build():
    data=native.EXE.read_bytes();manifest=load('phase193-gold-emulations.json');allpcs=set()
    for row in manifest['replays']:
        saved=load(row['file']);allowed=saved['input']['allowed_external_IO_code'];visited={int(a,16) for a in saved['result']['visited_instructions']}
        assert all(0x400000<=a<0x800000 or any(p<=a<p+n for p,n in allowed) for a in visited)
        allpcs.update(visited)
    pcs={a for a in allpcs if 0x400000<=a<0x800000}
    old=load('phase192-conversion-primary.json');inherited=[]
    for row in old['functions']+old['inherited']+old['executed_blocks']:
        if any(row['address']<=pc<row['address']+row['length'] for pc in pcs):inherited.append(validate(row,data))
    queries=[];functions=[]
    for a in EXTRA:
        stem=f'phase193-primary-{a:x}';info=ROOT/'ghidra'/(stem+'-info.txt')
        if not info.exists() or 'Body:' not in info.read_text():
            for tool,suffix in [('create_function','create'),('get_function_by_address','info'),('disassemble_function','assembly')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                queries.append(dict(id=stem+'-'+suffix,tool=tool,args=args,print_limit=0))
            continue
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',info.read_text());assert m
        first,last=(int(x,16) for x in m.groups());assert first==a;n=last+1-first;name='ghidra/'+stem+'-memory.txt'
        if not (ROOT/name).exists():queries.append(dict(id=stem+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0));continue
        functions.append(validate(dict(address=a,length=n,memory=name,assembly='ghidra/'+stem+'-assembly.txt',scope='complete-function'),data))
    rows=inherited+functions;missing=sorted(pc for pc in pcs if not any(r['address']<=pc<r['address']+r['length'] for r in rows));blocks=[]
    if not queries and missing:
        groups=[];md=native.Cs(native.CS_ARCH_X86,native.CS_MODE_32)
        for pc in missing:
            if not groups or pc-groups[-1][-1]>32:groups.append([])
            groups[-1].append(pc)
        for group in groups:
            a,end=group[0],group[-1];x=next(md.disasm(pe_read(data,end,15),end,count=1));n=end+x.size-a;name=f'ghidra/phase193-block-{a:x}-memory.txt'
            if not (ROOT/name).exists():queries.append(dict(id=f'phase193-block-{a:x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0));continue
            blocks.append(validate(dict(address=a,length=n,memory=name,scope='executed-byte-block-not-whole-function'),data))
    if queries:
        dump('phase193-primary-queries.json',queries);print('Primary pending',len(queries),'uncovered',len(missing),flush=True);return None
    rows+=blocks;assert all(any(r['address']<=pc<r['address']+r['length'] for r in rows) for pc in pcs)
    md=native.Cs(native.CS_ARCH_X86,native.CS_MODE_32);checks=[]
    for a in [0x51CFE0,0x51DC59,0x464372,0x464389,0x4643A2,0x4643C7,0x4643CD,0x4890D7,0x51E708]:
        x=next(md.disasm(pe_read(data,a,15),a,count=1));checks.append(dict(address=hex(a),hex=x.bytes.hex(),mnemonic=x.mnemonic,operands=x.op_str))
    return dict(program_sha256=sha(data),functions=functions,inherited=inherited,executed_blocks=blocks,
                unique_executed_original_PCs=len(pcs),declared_caller_and_external_audio_PCs=len(allpcs-pcs),key_instructions=checks,
                all_executed_original_PCs_covered=True,static_only='4890D7 second whole-Creature consumer and51E708 Level/TPS writer are read statically, not executed in this stage.')


if __name__=='__main__':
    p=build()
    if p is not None:dump('phase193-gold-primary.json',p);print('Primary passed',len(p['functions']),len(p['inherited']),len(p['executed_blocks']),p['unique_executed_original_PCs'])
