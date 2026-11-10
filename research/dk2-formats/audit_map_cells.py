"""Audit physical Map cells, original-code captures and raw preservation."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_map_cells import *
from emulate_map_cells import build_case
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('map-cell-semantics.json');boundaries=0
    for flags in range(256):
        for old in [0,0xFFFF,0xA5A5,0x1FFF]:
            out=runtime_substrate_flags(old,flags)
            assert (out&0xEFFF)==(old&0xEFFF) and bool(out&0x1000)==bool(flags&2);boundaries+=1
        cell=bytes([26,2,flags,0xA5]);mid=intermediate_cell(cell,0x40)
        assert mid==struct.pack('<III4B',1,1,1,26,2,flags|0x40,0);boundaries+=1
    assert writer_cells(2,1,bytes([17,26]),bytes([1,2]),0xA5,0x5A)==bytes([17,1,0xA5,0x5A,26,2,0xA5,0x5A]);boundaries+=1
    assert writer_cells(3,1,bytes([5,26,4]),bytes([1,2,3]))==bytes([5,1,1,0,26,2,2,0,4,3,1,0]);boundaries+=1
    # Linear-memory cross-row read is intentional native behavior.
    assert writer_cells(3,2,bytes([1,1,5,26,1,1]),bytes([1]*6))[14]==2;boundaries+=1
    for offset in range(4):
        original=bytes([1,2,3,4]);changed=patch_cell(original,offset,255)
        assert changed[offset]==255 and changed[:offset]==original[:offset] and changed[offset+1:]==original[offset+1:];boundaries+=1
    hashes={};excerpts=0;instructions=0
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                p=ROOT/'ghidra'/v['file'];lines=p.read_text().splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(s in lines for s in v['instructions']);instructions+=len(v['instructions'])
                hashes[v['file']]=digest(p.read_bytes())
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(schema)
    parser_results=[];writer_details=[]
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        for reg,value in expected.items():assert int(saved['result']['registers'][reg],16)==value
        if saved['case']['kind']=='parser':parser_results.append(saved['result']['registers'])
        if saved['case']['kind']=='writer':
            c=saved['case'];cells=writer_cells(c['width'],c['height'],bytes(c['terrain']),bytes(c['owners']),c['initial_flags'],c['fourth'],c['guard'])
            writer_details.append(dict(case=c['name'],cell_bytes=list(cells),seed_flags=c['initial_flags'],seed_fourth=c['fourth']))
    assert len(schema['native_emulations'])==22 and all(r==parser_results[0] for r in parser_results)
    unchanged=[]
    for name in ['map-editor-writer','map-editor-cell-converter','map-game-parser','map-game-cell']:
        before=ROOT/'ghidra'/('phase28-'+name+'-assembly.txt');after=ROOT/'ghidra'/('phase28-'+name+'-after.txt')
        assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(before.name)
    total=0;flags=Counter();fourth=Counter();files=[];bridge_neighborhood=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='Map' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256']
        for s in fmt.parse(b):
            if s.signature!=100:continue
            cells=s.data;w=fmt.u32(s.header,20);h=fmt.u32(s.header,24);n=w*h;assert len(cells)==n*4
            fl=Counter(cells[2::4]);fo=Counter(cells[3::4]);flags.update(fl);fourth.update(fo);total+=n
            for y in range(1,h-1):
                for x in range(1,w-1):
                    i=y*w+x
                    if cells[4*i] not in [17,26,36]:continue
                    neighbors=[cells[4*(i+d)] for d in [1,-1,w,-w]]
                    bridge_neighborhood[('lava' if 5 in neighbors else 'water' if 4 in neighbors else 'neither',cells[4*i+2])]+=1
            files.append(dict(file=file['file'],width=w,height=h,cells=n,sha256=file['sha256'],flags=dict(sorted(fl.items())),fourth=dict(sorted(fo.items()))))
    patches=0
    for path in [DATA/'AutoSaveMap.kld',DATA/'maps/kop4Map.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,s in enumerate(sections):
            if s.signature!=100:continue
            n=len(s.data)//4
            for i in [0,n//2,n-1]:
                cell=s.data[4*i:4*i+4]
                for off in range(4):
                    for value in [0,1,2,255]:
                        new=patch_cell(cell,off,value);cells=s.data[:4*i]+new+s.data[4*i+4:]
                        changed=fmt.Section(s.offset,s.signature,s.header,[cells],s.suffix);packed=b''.join(changed.pack() if j==si else part.pack() for j,part in enumerate(sections))
                        expected=bytearray(original);expected[s.offset+len(s.header)+4*i+off]=value
                        assert packed==expected and b''.join(part.pack() for part in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=39 and package['map_cell_semantics']==schema
    assert package['containers']['map']['legacy_field_aliases']==dict(bridge='flags',unknown='opaque')
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=28,date='2026-10-26',boundary_cases=boundaries,native_emulations=22,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        corpus=dict(map_files=len(files),cells=total,file_details=files,flags_counts=dict(sorted(flags.items())),fourth_counts=dict(sorted(fourth.items())),
            interior_bridge_neighbor_counts=[dict(neighbor_class=k[0],raw_flags=k[1],cells=v) for k,v in sorted(bridge_neighborhood.items())]),
        native_writer_fixtures=writer_details,parser_fourth_byte_variants_identical_readouts=True,
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=hashes,unchanged_disassembly=unchanged),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=39,limitations=schema['limitations'])
    (ROOT/'phase28-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Map corpus:',len(files),'files;',total,'cells; flags',dict(flags),'fourth',dict(fourth))
    print('Interior bridges',dict(bridge_neighborhood));print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(unchanged),'unchanged disassemblies')
if __name__=='__main__':main()
