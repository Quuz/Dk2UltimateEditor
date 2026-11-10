"""Attest reached Pro writer instructions using full Ghidra function extents."""
import json,re
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
import emulate_editor_variables_writer as n
import prepare_terrain_text_primary as old
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha


def main():
    accepted=load('phase199-editor-Variables-writer-emulations.json');pcs=set();blob=n.EXE.read_bytes();ranges=executable_ranges(blob)
    for row in accepted['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];d=load(row['file'])
        for token in d['result']['visited_instructions']:
            a=int(token,16)
            if any(p<=a<p+s for p,s in ranges):pcs.add(a)
            else:assert any(p<=a<p+s for p,s in d['input']['providers'])
    known=load('phase165-terrain-text-primary.json')['functions']+old.inherited_rows()+load('phase166-editor-variables-primary.json')['functions']
    known+=[r for r in load('phase122-primary-manifest.json')['functions'] if r['profile']=='Pro']
    qs=[]
    for p in sorted((ROOT/'ghidra').glob('phase199-Pro-*-info.txt')):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',p.read_text(encoding='utf8'))
        if not m:continue
        a,b=[int(x,16) for x in m.groups()];mem=p.with_name(p.name.replace('-info','-memory'));asm=p.with_name(p.name.replace('-info','-assembly'))
        if mem.exists() and asm.exists():known.append(dict(address=a,length=b-a+1,memory=str(mem.relative_to(ROOT)),assembly=str(asm.relative_to(ROOT))))
        else:
            for tool,path in [('read_memory',mem),('disassemble_function',asm)]:
                if not path.exists():qs.append(dict(id=path.stem,tool=tool,args=dict(program='DK2 Editor pro.exe',address=hex(a),**(dict(length=b-a+1) if tool=='read_memory' else {})),print_limit=0))
    covered=set();rows=[];seen=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for entry in known:
        r=dict(entry)
        for key in ['memory','assembly']:
            if isinstance(r.get(key),dict):
                v=r[key];r[key]=v['file'];r[key+'_sha256']=v['sha256']
        a,size=r['address'],r['length']
        if 'assembly' not in r:continue
        if (a,size) in seen or not any(a<=pc<a+size for pc in pcs):continue
        seen.add((a,size));mp=ROOT/r['memory'];ap=ROOT/r['assembly'];j=load(r['memory']);raw=bytes(j['data']) if 'data' in j else bytes.fromhex(j['hex'])
        assert len(raw)==size and raw==pe_read(blob,a,size),(hex(a),size,len(raw))
        for key,path in [('memory_sha256',mp),('assembly_sha256',ap)]:
            if r.get(key):assert r[key]==sha(path.read_bytes())
        starts=set(int(t,16) for t in re.findall(r'^([0-9a-fA-F]{8}):',ap.read_text(encoding='utf8'),re.M))
        for pc in list(starts):
            ins=next(md.disasm(pe_read(blob,pc,15),pc,count=1))
            if ins.mnemonic=='wait':starts.add(pc+ins.size)
        covered.update(starts);rows.append(dict(address=a,length=size,memory=r['memory'],memory_sha256=sha(mp.read_bytes()),assembly=r['assembly'],assembly_sha256=sha(ap.read_bytes()),bytes_sha256=sha(raw)))
    missing=pcs-covered
    clusters=[]
    for a in sorted(missing):
        if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
        else:clusters[-1].append(a)
    # Existing fresh metadata need byte queries before asking its owner again.
    fresh=[(int(m[1],16),int(m[2],16)) for p in (ROOT/'ghidra').glob('phase199-Pro-*-info.txt') for m in re.finditer(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',p.read_text(encoding='utf8'))]
    for c in clusters:
        if any(a<=c[0]<=b for a,b in fresh):continue
        qs.append(dict(id=f'phase199-Pro-{c[0]:x}-info',tool='get_function_by_address',args=dict(program='DK2 Editor pro.exe',address=hex(c[0])),print_limit=0))
    if qs:dump('phase199-primary-queries.json',qs);print('pending',len(qs),'missing PCs',len(missing));return
    assert not missing,[hex(a) for a in sorted(missing)[:20]]
    blocks=[]
    for a,size in [(0x494314,163),(0x50AC58,8)]:
        path=ROOT/f'ghidra/phase199-Pro-block-{a:x}-memory.txt'
        if not path.exists():qs.append(dict(id=path.stem,tool='read_memory',args=dict(program='DK2 Editor pro.exe',address=hex(a),length=size),print_limit=0));continue
        j=load(str(path.relative_to(ROOT)));raw=bytes(j['data']);assert raw==pe_read(blob,a,size)
        blocks.append(dict(address=a,length=size,memory=str(path.relative_to(ROOT)),memory_sha256=sha(path.read_bytes()),bytes_sha256=sha(raw)))
    if qs:dump('phase199-primary-queries.json',qs);print('pending blocks',len(qs));return
    dump('phase199-editor-Variables-writer-primary.json',dict(profile=dict(file=str(n.EXE),sha256=sha(blob)),functions=rows,blocks=blocks,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,
        scope='Full byte extents attest every reached original PC. Prepared map/double array, native Terrain conversion/callback, default initializer and entire Variables writer. Empty player/pool/availability/Type77 groups; explicit bounded external heap/time/CFile providers and initial IAT data rebinding.'))
    print('PASS primary',len(pcs),'PCs;',len(rows),'function extents')


if __name__=='__main__':main()
