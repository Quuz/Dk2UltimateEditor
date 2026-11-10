"""Accepted mode8 rawShot/rawTerrain/fullNAV paths, including complete Rock destruction."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_tremor_damage as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha

def cases():
    square=[[x,y,2] for x in range(-2,2) for y in range(-2,2) if (x,y)!=(0,0)]
    rows=[('ordinary',{}),('full-tick',dict(tick=True)),('second-full-tick',dict(tick=True,pre_ticks=1)),
        ('extent-zero',dict(data2=0)),('extent-subcell',dict(data2=4095)),('extent-raw1',dict(data2=1)),
        ('extent-negative',dict(data2=0xFFFFF000)),('extent2-filled-square',dict(data2=8192,tiles=square)),
        ('positive-boundary-excluded',dict(tiles=[[-1,-1,2],[-1,1,2],[1,-1,2]])),
        ('edge-zero',dict(origin_tile=[0,0],data2=8192,tiles=square)),('edge-last',dict(origin_tile=[31,31],data2=8192,tiles=square)),
        ('terrain-filter',dict(tiles=[[-1,-1,2],[-1,0,1],[0,-1,3]])),
        ('owner7',dict(owner=7)),('owner-highbits',dict(owner=7,owner_high=0xF000)),
        ('data1-zero',dict(damage=0)),('data1-negative80',dict(damage=0xFFFFFFB0)),
        ('data1-65536',dict(damage=65536)),('data1-65537',dict(damage=65537)),
        ('data1-INTMIN',dict(damage=0x80000000)),('data1-negative65536',dict(damage=0xFFFF0000)),
        ('destroy-single',dict(damage=165,tiles=[[-1,-1,2]],long_run=True)),
        ('destroy-three',dict(damage=165,long_run=True)),('destroy-full-tick',dict(damage=165,tick=True,long_run=True)),
        ('zeroHP-same-type',dict(damage=165,destroyed_type=2))]
    return [dict(name=name,**c) for name,c in rows]

def execute(c):
    args,checks,saved,path=p.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path

def reference(c,checks):
    origin=struct.unpack_from('<3i',checks[p.f.ARGS]);cx,cy=origin[0]>>12,origin[1]>>12
    data2=c.get('data2',4096);radius=(data2 if data2<0x80000000 else data2-0x100000000)>>12
    damage=c.get('damage',80);delta=(-damage)&0xFFFFFFFF;delta=delta if delta<0x80000000 else delta-0x100000000
    rounds=1+c.get('pre_ticks',0);cells=[dict(terrain=checks[p.f.MAP][16*i+10],health=int.from_bytes(checks[p.f.MAP][16*i+6:16*i+8],'little'),owner=int.from_bytes(checks[p.f.MAP][16*i+8:16*i+10],'little')) for i in range(1024)]
    requests=[];destroyed=[]
    for _ in range(rounds):
        for x in range(cx-radius,cx+radius):
            for y in range(cy-radius,cy+radius):
                if not(0<=x<32 and 0<=y<32):continue
                cell=cells[y*32+x]
                if cell['terrain']!=2:continue
                hp=cell['health'];requests.append(dict(x=x,y=y,delta=delta,owner=cell['owner']&0xFFF,HP_before=hp,terrain_id=2))
                if delta<0:hp=max(0,hp-((-delta)&0xFFFF))
                else:hp=min(32000,hp+(delta&0xFFFF))
                # Accepted positiveHPoutcomes allremain below sourceRockmax660.
                assert hp<660
                if hp==0 and c.get('destroyed_type',3)!=2:
                    assert cell['owner']&0xFFF==0
                    hp=1;cell['terrain']=3;destroyed.append([x,y])
                cell['health']=hp
    return dict(cells=cells,requests=requests,destroyed=destroyed,extent_cells=radius,rounds=rounds)

def verify(c,args,checks,r):
    e=reference(c,checks);post=post_context(checks,r)
    assert len(r['before_calls'])==23+c.get('pre_ticks',0) and all(x['returned'] for x in r['before_calls'])
    got=[{k:t[k] for k in ['x','y','delta','owner','HP_before','terrain_id']} for t in r['terrain_requests']]
    assert got==e['requests'],(c['name'],got,e['requests'])
    assert not r['hp_requests']
    for i,cell in enumerate(e['cells']):
        at=16*i;assert post[p.f.MAP][at+10]==cell['terrain'] and int.from_bytes(post[p.f.MAP][at+6:at+8],'little')==cell['health'],(c['name'],i,cell)
        if cell['terrain']==2:assert post[p.BITS][i]==8
        elif cell['terrain']==1:assert post[p.BITS][i]==8
        else:assert post[p.BITS][i]==0,(c['name'],i,post[p.BITS][i])
    assert r['visited_instructions'].get('0x452f60',0)==len(got)
    assert r['visited_instructions'].get('0x44fc40',0)==len(e['destroyed'])
    assert r['visited_instructions'].get('0x4acca0',0)==e['rounds']
    for a in [p.base.RAW,p.base.target.RAW]+[p.RAW0+i*0x20000 for i in range(3)]:assert post[a]==checks[a]
    assert post[p.TRIANGLES][-64:]==bytes([165])*64
    assert post[0]==checks[0] # Restore the supplied SEH chain head.
    assert all(int.from_bytes(post[a][0x40:0x44],'little')==1000 for a in p.area.TARGETS[:2])
    node=post[p.f.BLOCK][p.base.NODE-p.f.BLOCK:p.base.NODE-p.f.BLOCK+371];ticks=int(c.get('tick',False))+c.get('pre_ticks',0)
    assert int.from_bytes(node[0x40:0x44],'little')==32-ticks
    assert int.from_bytes(node[0x12D:0x131],'little')==ticks
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.area.EXE.read_bytes();low=0
    for row in r['observed_reads']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(p.area.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],row,ins.op_str)
        low+=1
    return post,dict(extent_cells=e['extent_cells'],rounds=e['rounds'],requests=got,destroyed=e['destroyed'],
        final_affected_HP=[e['cells'][t['y']*32+t['x']]['health'] for t in e['requests']],
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'],low_reads_only_FS_SEH=True,FS_reads=low)

def main():
    rows=[]
    for c in cases():
        args,checks,saved,path=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],'requests',len(out['requests']),'destroyed',out['destroyed'],flush=True)
    (ROOT/'phase151-tremor-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':main()
