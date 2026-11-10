"""Full byte extents for all phase200 reached DX instructions and alias owners."""
import re,json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
import emulate_global_table_indices as n
import trace_global_parameter_aliases as trace
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha


def main():
    blob=n.EXE.read_bytes();ranges=executable_ranges(blob);pcs=set()
    for row in load('phase200-global-index-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];d=load(row['file'])
        for token in d['result']['visited_instructions']:
            a=int(token,16)
            if any(p<=a<p+s for p,s in ranges):pcs.add(a)
            else:assert any(p<=a<p+s for p,s in d['input']['allowed_external_IO_code'])
    pcs.update(load('phase200-stat-getter-native.json')['visited_original_PCs'])
    known=load('phase197-Variables-primary.json')['functions']+load('phase198-reference-primary.json')['functions']
    known+=[r for r,_ in trace.primary()]
    for a,size in [(0x51DC40,36),(0x51DC70,26)]:
        info=f'ghidra/phase200-DX-{a:x}-info.txt';text=(ROOT/info).read_text();m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',text);assert m
        start,end=[int(v,16) for v in m.groups()];assert start==a and end-start+1==size
        known.append(dict(address=a,length=size,memory=f'ghidra/phase200-DX-{a:x}-memory.txt',assembly=f'ghidra/phase200-DX-{a:x}-assembly.txt'))
    md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];covered=set();seen=set()
    for r in known:
        a,size=r['address'],r['length']
        if (a,size) in seen or not any(a<=pc<a+size for pc in pcs):continue
        seen.add((a,size));mp=ROOT/r['memory'];ap=ROOT/r['assembly'];j=load(r['memory']);data=bytes(j['data']) if 'data' in j else bytes.fromhex(j['hex'])
        assert data==pe_read(blob,a,size) and len(data)==size
        if r.get('memory_sha256'):assert sha(mp.read_bytes())==r['memory_sha256']
        if r.get('assembly_sha256'):assert sha(ap.read_bytes())==r['assembly_sha256']
        starts=set(int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',ap.read_text(),re.M))
        for pc in list(starts):
            x=next(md.disasm(pe_read(blob,pc,15),pc,count=1))
            if x.mnemonic=='wait':starts.add(pc+x.size)
        covered.update(starts);rows.append(dict(address=a,length=size,memory=r['memory'],memory_sha256=sha(mp.read_bytes()),assembly=r['assembly'],assembly_sha256=sha(ap.read_bytes()),bytes_sha256=sha(data)))
    missing=pcs-covered
    if missing:
        clusters=[]
        for a in sorted(missing):
            if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
            else:clusters[-1].append(a)
        dump('phase200-missing-PC-queries.json',[dict(id=f'phase200-PC-{c[0]:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(c[0])),print_limit=0) for c in clusters]);print('missing PCs',len(missing),[hex(c[0]) for c in clusters]);return
    dump('phase200-global-index-primary.json',dict(profile=dict(file=str(n.EXE),sha256=sha(blob)),functions=rows,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        alias_search_functions=[r for r,_ in trace.primary()],scope='All reached original PCs from19file-reader scenes and512wholegettercalls.109wholebyteextents separately attest bounded affine static alias search; static search is not observed gameplay.'))
    print('PASS primary',len(pcs),'PCs;',len(rows),'executed function extents;109static alias extents')


if __name__=='__main__':main()
