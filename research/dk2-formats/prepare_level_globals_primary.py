"""Build complete original-PC attestation for accepted full Level scenes."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_level_globals as n
from build_effect_semantics import pe_read
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha


def main():
    pcs=set()
    for row in load('phase196-Level-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];saved=load(row['file'])
        for token in saved['result']['visited_instructions']:
            a=int(token,16)
            if 0x400000<=a<0x800000:pcs.add(a)
            else:assert any(p<=a<p+size for p,size in saved['input']['allowed_external_IO_code'])
    data=n.EXE.read_bytes();inherited=[];covered=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    old=load('phase195-tps-primary.json')['inherited_functions']
    for row in old:
        if not any(row['address']<=a<row['address']+row['length'] for a in pcs):continue
        blob=load(row['memory']);raw=bytes(blob['data']) if 'data' in blob else bytes.fromhex(blob['hex'])
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256'] and sha(raw)==row['bytes_sha256']
        assert raw==pe_read(data,row['address'],row['length'])
        starts=set(int(m,16) for m in re.findall(r'^([0-9a-fA-F]{8}):',(ROOT/row['assembly']).read_text(encoding='utf8'),re.M)) if row.get('assembly') else set(x.address for x in md.disasm(raw,row['address']))
        for a in list(starts):
            ins=next(md.disasm(pe_read(data,a,15),a,count=1))
            if ins.mnemonic=='wait':starts.add(a+ins.size)
        covered.update(starts);inherited.append(row)
    # Discover new function extents once, sequentially; metadata never implies execution.
    infos=[];seen=set()
    for p in sorted((ROOT/'ghidra').glob('phase196-PC-*-info.txt')):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',p.read_text(encoding='utf8'));assert m,p
        start,end=[int(x,16) for x in m.groups()]
        if (start,end) not in seen:infos.append((start,end,p));seen.add((start,end))
    missing=set(pcs-covered);queries=[]
    for start,end,info in infos:
        memory=info.with_name(info.name.replace('-info','-memory'));assembly=info.with_name(info.name.replace('-info','-assembly'))
        if not memory.exists() or not assembly.exists():
            for tool,p in [('read_memory',memory),('disassemble_function',assembly)]:
                if not p.exists():queries.append(dict(id=p.stem,tool=tool,args=dict(program='DKII-DX.exe',address=hex(start),**(dict(length=end-start+1) if tool=='read_memory' else {})),print_limit=0))
        missing.difference_update(range(start,end+1))
    if missing:
        # Query only first point in each uncovered contiguous cluster; rerun fills others.
        clusters=[]
        for a in sorted(missing):
            if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
            else:clusters[-1].append(a)
        for c in clusters:
            a=c[0];queries.append(dict(id=f'phase196-PC-{a:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0))
    if queries:
        dump('phase196-PC-queries.json',queries);print('Pending primary queries',len(queries),'uncovered PCs',len(missing));return
    new=[]
    for start,end,info in infos:
        memory=info.with_name(info.name.replace('-info','-memory'));assembly=info.with_name(info.name.replace('-info','-assembly'));raw=bytes(json.loads(memory.read_text(encoding='utf8'))['data'])
        assert raw==pe_read(data,start,end-start+1)
        starts=set(int(m,16) for m in re.findall(r'^([0-9a-fA-F]{8}):',assembly.read_text(encoding='utf8'),re.M))
        for a in list(starts):
            ins=next(md.disasm(pe_read(data,a,15),a,count=1))
            if ins.mnemonic=='wait':starts.add(a+ins.size)
        covered.update(starts)
        new.append(dict(address=start,length=end-start+1,memory=str(memory.relative_to(ROOT)),memory_sha256=sha(memory.read_bytes()),bytes_sha256=sha(raw),assembly=str(assembly.relative_to(ROOT)),assembly_sha256=sha(assembly.read_bytes())))
    assert not pcs-covered,[hex(x) for x in sorted(pcs-covered)[:20]]
    dump('phase196-Level-primary.json',dict(profile=dict(file=str(n.EXE),sha256=sha(data)),inherited_functions=inherited,new_executed_functions=new,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True))
    print('PASS primary',len(pcs),'PCs;',len(inherited),'inherited;',len(new),'new whole functions')


if __name__=='__main__':main()
