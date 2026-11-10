"""Attest all original PCs reached by phase197 against full Ghidra byte extents."""
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_variables_reader as n
from build_effect_semantics import pe_read
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha


def main():
    report=load('phase197-Variables-emulations.json');pcs=set();files=[report['registration']]+report['replays']
    for row in files:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];saved=load(row['file'])
        for token in saved['result']['visited_instructions']:
            a=int(token,16)
            if 0x400000<=a<0x800000:pcs.add(a)
            else:assert any(p<=a<p+size for p,size in saved['input'].get('allowed_external_IO_code',[]))
    old=load('phase196-Level-primary.json');known=old['inherited_functions']+old['new_executed_functions']
    known+=load('phase139-native-map-reader-primary.json')['functions']+load('phase163-creature-work-cell-primary.json')['functions']
    known+=load('level-global-parameters-semantics.json')['static_primary']['functions']
    data=n.EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();rows=[];seen=set()
    infos=[]
    for p in sorted((ROOT/'ghidra').glob('phase197-PC-*-info.txt')):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',p.read_text(encoding='utf8'));assert m,p
        start,end=[int(x,16) for x in m.groups()];infos.append((start,end,p))
    queries=[]
    for start,end,p in infos:
        memory=p.with_name(p.name.replace('-info','-memory'));assembly=p.with_name(p.name.replace('-info','-assembly'))
        if memory.exists() and assembly.exists():known.append(dict(address=start,length=end-start+1,memory=str(memory.relative_to(ROOT)),assembly=str(assembly.relative_to(ROOT))))
        else:
            for tool,f in [('read_memory',memory),('disassemble_function',assembly)]:
                if not f.exists():queries.append(dict(id=f.stem,tool=tool,args=dict(program='DKII-DX.exe',address=hex(start),**(dict(length=end-start+1) if tool=='read_memory' else {})),print_limit=0))
    for row in known:
        a,size=row['address'],row['length']
        if (a,size) in seen or not any(a<=pc<a+size for pc in pcs):continue
        seen.add((a,size));memory=ROOT/row['memory'];assembly=ROOT/row['assembly']
        blob=json.loads(memory.read_text(encoding='utf8'));raw=bytes(blob['data']) if 'data' in blob else bytes.fromhex(blob['hex'])
        assert len(raw)==size and raw==pe_read(data,a,size),(hex(a),size,len(raw))
        if row.get('memory_sha256'):assert sha(memory.read_bytes())==row['memory_sha256']
        starts=set(int(m,16) for m in re.findall(r'^([0-9a-fA-F]{8}):',assembly.read_text(encoding='utf8'),re.M))
        for pc in list(starts):
            ins=next(md.disasm(pe_read(data,pc,15),pc,count=1))
            if ins.mnemonic=='wait':starts.add(pc+ins.size)
        covered.update(starts);rows.append(dict(address=a,length=size,memory=row['memory'],memory_sha256=sha(memory.read_bytes()),assembly=row['assembly'],assembly_sha256=sha(assembly.read_bytes()),bytes_sha256=sha(raw)))
    missing=pcs-covered
    for start,end,_ in infos:missing.difference_update(range(start,end+1))
    if missing:
        clusters=[]
        for a in sorted(missing):
            if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
            else:clusters[-1].append(a)
        for c in clusters:queries.append(dict(id=f'phase197-PC-{c[0]:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(c[0])),print_limit=0))
    if queries:dump('phase197-PC-queries.json',queries);print('Pending queries',len(queries),'uncovered PCs',len(pcs-covered));return
    assert not pcs-covered,[hex(x) for x in sorted(pcs-covered)[:20]]
    dump('phase197-Variables-primary.json',dict(profile=dict(file=str(n.EXE),sha256=sha(data)),functions=rows,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope='Whole function bytes attest reached instruction PCs; only registration509AEF..509B3A segment was executed, not its containing constructor. Full file open/close/reference traversal not executed.'))
    print('PASS primary',len(pcs),'PCs;',len(rows),'whole function byte extents')


if __name__=='__main__':main()
