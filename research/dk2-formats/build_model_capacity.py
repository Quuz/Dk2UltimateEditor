"""Prepare compact model statistics and attest relevant original Ghidra bytes."""
from pathlib import Path
import csv, gzip, hashlib, json, re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
import pefile
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')


def main():
    inventory=load('model-complexity-inventory.json');assert inventory['passed']
    cpu=load('model-count-limits-verification.json');assert cpu['passed'] and cpu['controls']==22
    exe=ROOT.parents[1]/'Dungeon Keeper 2/DKII-DX.exe';raw=exe.read_bytes();pe=pefile.PE(data=raw)
    assert sha(raw)==cpu['exe_sha256']
    primary=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for info in sorted((ROOT/'ghidra').glob('model-limits-*-info.txt')):
        name=info.stem[:-5];text=info.read_text(encoding='utf8')
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',text);assert m
        start,end=[int(x,16) for x in m.groups()];n=end-start+1
        mem=ROOT/'ghidra'/f'{name}-memory.txt';memory=json.loads(mem.read_text(encoding='utf8'))
        data=bytes(memory['data']);assert len(data)==n and data==pe.get_data(start-pe.OPTIONAL_HEADER.ImageBase,n)
        asm=ROOT/'ghidra'/f'{name}-assembly.txt'
        pcs=[int(m.group(1),16) for m in re.finditer(r'^([0-9a-fA-F]{8}):',asm.read_text(encoding='utf8'),re.M)]
        dis={x.address:x for x in md.disasm(data,start)}
        assert pcs and all(pc in dis for pc in pcs),(name,[hex(x) for x in pcs if x not in dis])
        primary.append(dict(name=name,start=hex(start),end_inclusive=hex(end),bytes=n,code_sha256=sha(data),instruction_starts=len(pcs),
                            files=[dict(file=str(p.relative_to(ROOT)).replace('\\','/'),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in [info,mem,asm]]))
    assert len(primary)==10
    dump('model-capacity-primary.json',dict(passed=True,exe_sha256=sha(raw),functions=primary,
        primary_bytes=sum(x['bytes'] for x in primary),instruction_starts=sum(x['instruction_starts'] for x in primary),
        original_bytes_match_PE=True,scope='Static primary functions and isolated constructor-loop controls; no full rendering/load benchmark.'))
    with (ROOT/'model-complexity.csv').open('w',encoding='utf-8-sig',newline='') as f:
        fields=['file','kind','triangles_LOD0','triangles_LOD15','groups','geometry_or_itab_entries','vertices_referenced_LOD0','stored_group_vertices','active_group_vertices_LOD0','frames','bytes','sha256']
        writer=csv.DictWriter(f,fields);writer.writeheader()
        for r in sorted(inventory['archive'],key=lambda r:r['file'].lower()):
            if r['kind']=='grop':continue
            writer.writerow(dict(file=r['file'],kind=r['kind'],triangles_LOD0=r['triangles_per_lod'][0],triangles_LOD15=r['triangles_per_lod'][-1],
                groups=r['group_count'],geometry_or_itab_entries=r['index_count'],vertices_referenced_LOD0=r['referenced_geometry_per_lod'][0],
                stored_group_vertices=r['stored_group_vertices'],active_group_vertices_LOD0=r['active_group_vertices_per_lod'][0],
                frames=r['frame_count'],bytes=r['bytes'],sha256=r['sha256']))
    inventory_raw=(ROOT/'model-complexity-inventory.json').read_bytes()
    (ROOT/'model-complexity-inventory.json.gz').write_bytes(gzip.compress(inventory_raw,mtime=0))
    summary={k:v for k,v in inventory.items() if k not in ['archive','loose']}
    summary['primary_evidence']='model-capacity-primary.json';summary['native_boundary_controls']='model-count-limits-verification.json'
    summary['inventory_gzip_sha256']=sha((ROOT/'model-complexity-inventory.json.gz').read_bytes())
    summary['interpretation']=dict(triangle_count_per_group_per_LOD=dict(maximum=255,evidence='original-loop execution;256->0'),
        addressable_group_vertex_indices=dict(minimum=0,maximum=255,slots=256,stock_export_max_vertices=255,evidence='KMF byte indices and original renderer byte reads'),
        lod_entries=dict(maximum=16,evidence='original-loop LOD17 overwrites header fields'),
        temporary_loader_group_capacity=dict(maximum=128,evidence='static stack layout, not boundary executed or overall stability guarantee'),
        shared_draw_batch_triangles=dict(split_threshold=256,evidence='static58F640; not a whole-model cap'),
        complete_model_triangle_ceiling='not established',safe_gameplay_budget='not measured')
    dump('model-complexity-summary.json',summary)
    print('PASS:10 original function extents,',sum(x['bytes'] for x in primary),'bytes,',sum(x['instruction_starts'] for x in primary),'instruction starts')
    print('Compact inventory', (ROOT/'model-complexity-inventory.json.gz').stat().st_size,'bytes; CSV',(ROOT/'model-complexity.csv').stat().st_size,'bytes')


if __name__=='__main__':main()
