"""Attest executed PE bytes and request only previously uncaptured functions."""
from pathlib import Path
import json,re
import emulate_conversion_score as native
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha,load,dump=native.sha,native.load,native.dump
EXTRA=[0x4F2800,0x48D050,0x511650,0x486E20]


def resolve(name):
    p=ROOT/name
    if not p.exists():p=ROOT/'ghidra'/name
    return p


def span(row,data):
    a=row['address'];a=int(a,0) if isinstance(a,str) else a
    n=row['length'];p=resolve(row['memory']);s=json.loads(p.read_text(encoding='utf8'))
    raw=pe_read(data,a,n);assert s['length']==n and bytes.fromhex(s['hex'])==bytes(s['data'])==raw
    asm=resolve(row['assembly']);assert asm.read_text()[:8].lower()==f'{a:08x}'
    return dict(address=a,length=n,memory=str(p.relative_to(ROOT)),memory_sha256=sha(p.read_bytes()),
                assembly=str(asm.relative_to(ROOT)),assembly_sha256=sha(asm.read_bytes()),bytes_sha256=sha(raw))


def build():
    data=native.EXE.read_bytes();manifest=load('phase192-conversion-emulations.json')
    allpcs={int(a,16) for row in manifest['replays'] for a in load(row['file'])['result']['visited_instructions']}
    pcs={a for a in allpcs if 0x400000<=a<0x800000}
    for row in manifest['replays']:
        saved=load(row['file']);allowed=saved['input']['allowed_external_IO_code']
        assert all(0x400000<=int(a,16)<0x800000 or any(p<=int(a,16)<p+n for p,n in allowed) for a in saved['result']['visited_instructions'])
    inherited=load('phase189-crypt-completion-primary.json');rows=[]
    for row in inherited['functions']+inherited['inherited']:
        a=row['address'];n=row['length']
        if any(a<=pc<a+n for pc in pcs):rows.append(span(row,data))
    queries=[];extras=[]
    for a in EXTRA:
        stem=f'phase192-score-primary-{a:x}'
        info=ROOT/'ghidra'/(stem+'-info.txt')
        if not info.exists() or 'Body:' not in info.read_text():
            # These exact entries come from original executed CALLs or
            # unchanged vtable slots24/344, not guessed byte patterns.
            for tool,suffix in [('create_function','create'),('get_function_by_address','info'),('disassemble_function','assembly')]:
                arguments=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':arguments['disassemble_first']=True
                queries.append(dict(id=stem+'-'+suffix,tool=tool,args=arguments,print_limit=0))
            continue
        text=info.read_text();m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',text);assert m,(stem,text[:200])
        start,end=map(lambda x:int(x,16),m.groups());assert start==a,(stem,start)
        n=end+1-start;memory=ROOT/'ghidra'/(stem+'-memory.txt')
        if not memory.exists():queries.append(dict(id=stem+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0));continue
        extras.append(span(dict(address=a,length=n,memory=str(memory.relative_to(ROOT)),assembly='ghidra/'+stem+'-assembly.txt'),data))
    rows.extend(extras)
    missing=sorted(pc for pc in pcs if not any(r['address']<=pc<r['address']+r['length'] for r in rows))
    fragments=[]
    if not queries and missing:
        groups=[]
        for pc in missing:
            if not groups or pc-groups[-1][-1]>32:groups.append([])
            groups[-1].append(pc)
        md=native.Cs(native.CS_ARCH_X86,native.CS_MODE_32)
        for group in groups:
            a,last=group[0],group[-1];ins=next(md.disasm(pe_read(data,last,15),last,count=1));n=last+ins.size-a
            name=f'ghidra/phase192-executed-block-{a:x}-memory.txt'
            if not (ROOT/name).exists():
                queries.append(dict(id=f'phase192-executed-block-{a:x}-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=0))
                continue
            saved=load(name);raw=pe_read(data,a,n)
            assert saved['length']==n and bytes.fromhex(saved['hex'])==bytes(saved['data'])==raw
            fragments.append(dict(address=a,length=n,memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(raw),
                scope='Executed-PC byte block, not a complete function boundary',executed_PCs=group))
        rows.extend(fragments)
    if queries:
        dump('phase192-score-primary-queries.json',queries)
        print('Primary pending queries',len(queries),'uncovered PCs',len(missing),'first',list(map(hex,missing[:16])),flush=True)
        return None
    assert all(0x400000<=pc<0x800000 for pc in pcs)
    assert all(any(r['address']<=pc<r['address']+r['length'] for r in rows) for pc in pcs)
    # The untouched original vtable, not a fabricated callback, selects511650.
    vtable=pe_read(data,0x66E3EC,0x3A0);assert int.from_bytes(vtable[0x344:0x348],'little')==0x511650
    instructions=[];md=native.Cs(native.CS_ARCH_X86,native.CS_MODE_32)
    for a in [0x51D6AD,0x4F2893,0x4F28A3,0x4F28B9,0x4F28C1,0x51165A,0x511662]:
        x=next(md.disasm(pe_read(data,a,15),a,count=1));instructions.append(dict(address=hex(a),hex=x.bytes.hex(),mnemonic=x.mnemonic,operands=x.op_str))
    return dict(program_sha256=sha(data),functions=extras,executed_blocks=fragments,inherited=[r for r in rows if r not in extras and r not in fragments],
                unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
                declared_external_audio_PCs=len(allpcs-pcs),
                original_World_vtable=dict(address=0x66E3EC,length=len(vtable),sha256=sha(vtable),slot=0x344,target=0x511650),
                key_instructions=instructions,
                caller_static_only=dict(function=0x47B590,call=0x47B71F,target=0x4F2800,
                    assembly='ghidra/phase167-DX-47b590-assembly.txt',scope='Torture chance/threshold path read statically; not executed by these conversion scenes.'))


def main():
    result=build()
    if result is not None:
        dump('phase192-conversion-primary.json',result)
        print('Primary passed',len(result['functions']),len(result['inherited']),result['unique_executed_original_PCs'],flush=True)


if __name__=='__main__':main()
