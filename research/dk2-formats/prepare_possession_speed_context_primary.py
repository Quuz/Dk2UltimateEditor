"""Attest all original PCs in fresh Creature/file/context speed compositions."""
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
import emulate_possession_speed_context as n
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha


def main():
    blob=n.p.EXE.read_bytes();ranges=executable_ranges(blob);pcs=set()
    accepted=load('phase201-speed-context-emulations.json')
    for row in accepted['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];d=load(row['file'])
        for token in d['result']['visited_instructions']:
            a=int(token,16)
            if any(p<=a<p+s for p,s in ranges):pcs.add(a)
            else:assert any(p<=a<p+s for p,s in d['input']['allowed_external_IO_code'])
    previous=load('phase200-global-index-primary.json');known=previous['functions']+previous['alias_search_functions'];queries=[];fresh=[]
    for a in [0x48ABB0,0x48DEA0,0x48ED40,0x508FE0]:
        info=ROOT/f'ghidra/phase201-DX-{a:x}-info.txt';m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',info.read_text());assert m
        start,end=[int(v,16) for v in m.groups()];assert start==a
        memory=info.with_name(info.name.replace('-info','-memory'));assembly=info.with_name(info.name.replace('-info','-assembly'));fresh.append((start,end))
        if memory.exists() and assembly.exists():known.append(dict(address=a,length=end-a+1,memory=str(memory.relative_to(ROOT)),assembly=str(assembly.relative_to(ROOT))))
        else:
            # Reuse a prior whole attested range instead of rereading completed evidence.
            if any(r['address']==a and r['length']==end-a+1 for r in known):continue
            for tool,path in [('read_memory',memory),('disassemble_function',assembly)]:
                if not path.exists():queries.append(dict(id=path.stem,tool=tool,args=dict(program='DKII-DX.exe',address=hex(a),**(dict(length=end-a+1) if tool=='read_memory' else {})),print_limit=0))
    for info in (ROOT/'ghidra').glob('phase201-PC-*-info.txt'):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',info.read_text());assert m
        start,end=[int(v,16) for v in m.groups()];fresh.append((start,end))
        mp=info.with_name(info.name.replace('-info','-memory'));ap=info.with_name(info.name.replace('-info','-assembly'))
        if mp.exists() and ap.exists():known.append(dict(address=start,length=end-start+1,memory=str(mp.relative_to(ROOT)),assembly=str(ap.relative_to(ROOT))))
        else:
            for tool,path in [('read_memory',mp),('disassemble_function',ap)]:
                if not path.exists():queries.append(dict(id=path.stem,tool=tool,args=dict(program='DKII-DX.exe',address=hex(start),**(dict(length=end-start+1) if tool=='read_memory' else {})),print_limit=0))
    md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();rows=[];seen=set()
    for r in known:
        a,size=r['address'],r['length']
        if (a,size) in seen or not any(a<=pc<a+size for pc in pcs):continue
        seen.add((a,size));mp=ROOT/r['memory'];ap=ROOT/r['assembly'];j=load(r['memory']);raw=bytes(j['data']) if 'data' in j else bytes.fromhex(j['hex'])
        assert len(raw)==size and raw==pe_read(blob,a,size)
        if r.get('memory_sha256'):assert sha(mp.read_bytes())==r['memory_sha256']
        if r.get('assembly_sha256'):assert sha(ap.read_bytes())==r['assembly_sha256']
        starts=set(int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',ap.read_text(),re.M))
        for pc in list(starts):
            x=next(md.disasm(pe_read(blob,pc,15),pc,count=1))
            if x.mnemonic=='wait':starts.add(pc+x.size)
        covered.update(starts);rows.append(dict(address=a,length=size,memory=r['memory'],memory_sha256=sha(mp.read_bytes()),assembly=r['assembly'],assembly_sha256=sha(ap.read_bytes()),bytes_sha256=sha(raw)))
    missing=pcs-covered;clusters=[]
    for a in sorted(missing):
        if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
        else:clusters[-1].append(a)
    for c in clusters:
        if not any(a<=c[0]<=b for a,b in fresh):queries.append(dict(id=f'phase201-PC-{c[0]:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(c[0])),print_limit=0))
    if queries:dump('phase201-context-primary-queries.json',queries);print('Pending',len(queries),'missing PCs',len(missing));return
    assert not missing,[hex(x) for x in sorted(missing)[:20]]
    dump('phase201-speed-context-primary.json',dict(profile=dict(file=str(n.p.EXE),sha256=sha(blob)),functions=rows,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope='Fresh rawCreature constructor/converter/loader, real Variables E6 section/World callback, original possession setter, full48ABB0 with actualWorld1D4 andActor88. Prepared Actor/World/terrain and external file/null-resource providers; no movement scheduler.'))
    print('PASS primary',len(pcs),'original PCs;',len(rows),'function byte extents')


if __name__=='__main__':main()
