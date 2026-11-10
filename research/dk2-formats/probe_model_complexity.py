"""Inventory actual KMF topology, separating triangles from animation samples.

Reads game archives and existing loose exports without changing either.
"""
from pathlib import Path
import argparse, hashlib, json, struct, sys, importlib.util
from collections import Counter
ROOT = Path(__file__).resolve().parent
WAD_READER = Path(r'D:\Games\DK2\DK2_Model_Loader\dk2_tools\scripts\dk2wad.py')
sha = lambda b: hashlib.sha256(b).hexdigest()


class Reader:
    def __init__(self, data): self.data, self.p = data, 0
    def take(self, n):
        assert 0 <= n <= len(self.data)-self.p, (self.p,n,len(self.data))
        b=self.data[self.p:self.p+n]; self.p+=n; return b
    def u32(self): return struct.unpack('<I',self.take(4))[0]
    def string(self):
        end=self.data.index(b'\0',self.p); return self.take(end-self.p+1)[:-1].decode('latin1')
    def tag(self,t): assert self.take(4)==t.encode(), (self.p-4,t)
    def chunk(self,t): self.tag(t); return self.u32()


def parse(data):
    r=Reader(data); declared=r.chunk('KMSH'); version=r.u32(); r.chunk('HEAD'); kind=r.u32(); unknown=r.u32()
    out=dict(version=version, kind={1:'mesh',2:'anim',3:'grop'}[kind], declared_size=declared, bytes=len(data), sha256=sha(data))
    if kind==3:
        r.chunk('GROP'); r.chunk('HEAD'); children=[]
        for _ in range(r.u32()):
            r.chunk('ELEM'); children.append(dict(name=r.string(), translation=struct.unpack('<3f',r.take(12))))
        out['children']=children
    else:
        r.chunk('MATL'); materials=[]
        for _ in range(r.u32()):
            r.chunk('MAT2'); name=r.string(); textures=[r.string() for _ in range(r.u32())]
            flags=r.u32(); r.take(8); env=r.string()
            materials.append(dict(name=name, textures=textures, flags=flags, environment=env))
        r.chunk('ANIM' if kind==2 else 'MESH'); r.chunk('HEAD'); out['name']=r.string()
        ng=r.u32()
        if kind==2:
            frames, indices, geometry=r.u32(),r.u32(),r.u32(); r.u32(); r.take(20)
        else:
            frames,geometry=1,r.u32(); indices=geometry; r.take(16)
        lods=r.u32(); assert 0 < lods <= 256 and ng < 10000
        r.chunk('CTRL'); ctrl=r.u32(); r.take(ctrl*8)
        r.chunk('SPRS'); heads=[]
        for i in range(ng):
            r.chunk('SPHD'); counts=[r.u32() for _ in range(lods)]; nv=r.u32(); r.take(4)
            heads.append(dict(group=i, triangles=counts, vertices=nv))
        for h in heads:
            r.chunk('SPRS'); h['material']=r.u32(); assert h['material'] < len(materials)
            if kind==2:r.chunk('POLY')
            tri=r.take(3*sum(h['triangles'])); h['max_triangle_index']=max(tri, default=-1)
            assert h['max_triangle_index'] < h['vertices']
            used=[]; offset_tri=0
            for nt in h['triangles']:
                used.append(set(tri[offset_tri:offset_tri+nt*3])); offset_tri+=nt*3
            h['referenced_vertex_slots_per_lod']=[len(x) for x in used]
            if kind==2:r.chunk('VERT')
            vertices=r.take(18*h['vertices']); offset=16 if kind==2 else 0
            refs=[struct.unpack_from('<H',vertices,18*i+offset)[0] for i in range(h['vertices'])]
            h['max_geometry_or_itab_index']=max(refs,default=-1)
            assert h['max_geometry_or_itab_index'] < indices
            h['referenced_geometry_per_lod']=[sorted({refs[i] for i in u}) for u in used]
        if kind==2:
            r.chunk('ITAB'); r.take(((frames+127)//128)*indices*4)
            r.chunk('GEOM'); r.take(geometry*5)
            r.chunk('VGEO'); r.take(indices*frames)
        else:
            r.chunk('GEOM'); r.take(geometry*12)
        out.update(materials=materials, groups=heads, frame_count=frames, index_count=indices,
                   geometry_samples=geometry, lod_count=lods,
                   triangles_per_lod=[sum(h['triangles'][j] for h in heads) for j in range(lods)],
                   stored_group_vertices=sum(h['vertices'] for h in heads), group_count=ng,
                   active_group_vertices_per_lod=[sum(h['vertices'] for h in heads if h['triangles'][j]) for j in range(lods)],
                   referenced_vertex_slots_per_lod=[sum(h['referenced_vertex_slots_per_lod'][j] for h in heads) for j in range(lods)],
                   referenced_geometry_per_lod=[len(set().union(*(h['referenced_geometry_per_lod'][j] for h in heads))) for j in range(lods)])
        for h in heads:del h['referenced_geometry_per_lod']
    out['parsed_end']=r.p; out['trailing_bytes']=len(data)-r.p
    assert out['trailing_bytes']==0 and declared==len(data)
    return out


def summarize(rows):
    models=[r for r in rows if r['kind']!='grop']
    brief=lambda r: {k:r[k] for k in ['file','kind','group_count','triangles_per_lod','stored_group_vertices','active_group_vertices_per_lod','referenced_vertex_slots_per_lod','referenced_geometry_per_lod','index_count','geometry_samples','frame_count','bytes']}
    return dict(files=len(rows), kinds=dict(Counter(r['kind'] for r in rows)),
        top_triangles=[brief(r) for r in sorted(models,key=lambda r:r['triangles_per_lod'][0],reverse=True)[:25]],
        top_vertices=[brief(r) for r in sorted(models,key=lambda r:r['stored_group_vertices'],reverse=True)[:10]],
        top_bytes=[brief(r) for r in sorted(models,key=lambda r:r['bytes'],reverse=True)[:10]],
        max_group_vertices=max(h['vertices'] for r in models for h in r['groups']),
        max_group_triangles=max(max(h['triangles']) for r in models for h in r['groups']),
        max_lod_count=max(r['lod_count'] for r in models),
        group_vertices_histogram=dict(sorted(Counter(h['vertices'] for r in models for h in r['groups']).items())),
        group_triangle_max_histogram=dict(sorted(Counter(max(h['triangles']) for r in models for h in r['groups']).items())))


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--data',type=Path,default=Path(r'D:\Games\DK2\Dungeon Keeper 2\Data'))
    ap.add_argument('--wad-reader',type=Path,default=WAD_READER); args=ap.parse_args()
    spec=importlib.util.spec_from_file_location('_model_inventory_wad',args.wad_reader)
    reader=importlib.util.module_from_spec(spec);sys.modules[spec.name]=reader;spec.loader.exec_module(reader)
    wadpath=args.data/'Meshes.WAD'; wadraw=wadpath.read_bytes(); wad=reader.read_wad(wadpath)
    archive=[]
    for entry in wad.entries:
        assert Path(entry.name).suffix.lower()=='.kmf'
        row=parse(wad.extract(entry)); row['file']=entry.name; archive.append(row)
    loose=[]
    for path in sorted((args.data/'Meshes export').glob('*.kmf')):
        row=parse(path.read_bytes()); row['file']=path.name; loose.append(row)
    byname={r['file'].lower():r for r in archive}
    identical=sum(r['file'].lower() in byname and r['sha256']==byname[r['file'].lower()]['sha256'] for r in loose)
    different=[r['file'] for r in loose if r['file'].lower() in byname and r['sha256']!=byname[r['file'].lower()]['sha256']]
    extra=[r['file'] for r in loose if r['file'].lower() not in byname]
    # Group files are compositions, not another independent triangle list.
    memo={}
    def expand(name, parents=()):
        key=Path(name.replace('\\','/')).name.lower()
        if not key.endswith('.kmf'):key+='.kmf'
        if key in memo:return memo[key]
        assert key not in parents, (key,parents)
        row=byname[key]
        if row['kind']!='grop':result=(row['triangles_per_lod'][0],row['stored_group_vertices'],1)
        else:
            children=[expand(c['name'],parents+(key,)) for c in row['children']]
            result=tuple(sum(x[i] for x in children) for i in range(3))
        memo[key]=result; return result
    groups=[]
    for row in archive:
        if row['kind']=='grop':
            t,v,n=expand(row['file']); groups.append(dict(file=row['file'],triangles_LOD0=t,stored_group_vertices=v,leaf_instances=n))
    report=dict(schema_version=1,date='2026-11-10',passed=True,
        inputs=dict(wad=str(wadpath),wad_sha256=sha(wadraw),wad_bytes=len(wadraw),reader_dependency=str(args.wad_reader),reader_sha256=sha(args.wad_reader.read_bytes()),probe_sha256=sha(Path(__file__).read_bytes())),
        summary=dict(archive=summarize(archive),loose=summarize(loose),loose_identical_to_archive=identical,loose_different=different,loose_extra=extra),
        top_compositions=sorted(groups,key=lambda x:x['triangles_LOD0'],reverse=True)[:25],
        archive=archive,loose=loose,
        limits=['Topology inventory only, no original loader/render execution or frame-rate/stability measurement.',
                'LOD0 means first stored detail level; not sum of all LODs or animation samples.',
                'Group totals count all recursively referenced leaf instances at LOD0, not a measured simultaneously visible frame.',
                'Existing loose export directory inspected separately; no claim about runtime override precedence.'])
    (ROOT/'model-complexity-inventory.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')
    print('PASS:',len(archive),'archive models;',len(loose),'loose files;',identical,'loose files byte-identical to archive')
    for x in report['summary']['archive']['top_triangles'][:8]:
        print(x['file'],'triangles',x['triangles_per_lod'][0],'referenced vertices',x['referenced_geometry_per_lod'][0],'groups',x['group_count'])


if __name__=='__main__':main()
