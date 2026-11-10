"""Complete native Map callback -> Room creation/join -> decoration/remesh.

The scene is still a prepared 5x5 world, with a bounded one-room free-list,
native constructors and fixed-size Object allocator. Host-produced MapMID is
the established parser projection; the full native file reader is not executed.
Semantic and paired-memory checks are separate from whole-scene CPU agreement.
"""
import json,struct
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_map_room_placement as probe
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read
import native_room_adjacency as adjacency

ROOT=Path(__file__).resolve().parent
p=probe.old.prior
NAVGLOBALS=0x6E5AC0
sha=probe.old.sha

def cases():
    square=[(x,y) for y in range(1,4) for x in range(1,4)]
    return [dict(name=n,points=points,flags=flags) for n,points,flags in [
        ('baseline',[],0),('single',[(2,2)],0),('pair',[(2,2),(3,2)],0),
        ('square',square,0),('square-substrate',square,2),('square-flag1',square,1),
        ('square-flag80',square,128),('square-flagFF',square,255),
        ('missing-diagonal',[pt for pt in square if pt!=(3,1)],0),
        ('reverse-square',list(reversed(square)),0),
        ('rectangle',[(x,y) for y in range(1,4) for x in range(1,3)],0)]]

def execute(c):
    args,checks=probe.build(c['points'],c['flags']);key=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    path=ROOT/'ghidra'/f'phase138-map-room-native-{key}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=probe.cpu.run(EXE,args,list(checks.items()),p.STOP,p.ROOM,233)
        saved=dict(case=c,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['registers'],r['trace_tail'])
    return args,checks,saved,path

def read32(post,a):
    for base,b in post.items():
        if base<=a and a+4<=base+len(b):return int.from_bytes(b[a-base:a-base+4],'little')
    raise ValueError(hex(a))

def pool_objects(post):
    used=[];free=[]
    for head,tail,dest in [(p.WORLD+0x5E,p.WORLD+0x52,used),(probe.BLOCK+16384+12,probe.BLOCK,free)]:
        prev=head;h=read32(post,head+8)
        while h!=tail:
            assert probe.BLOCK+12<=h<probe.BLOCK+16384+12 and len(dest)<100
            length,back,nxt=struct.unpack_from('<3I',post[probe.BLOCK],h-probe.BLOCK)
            assert back==prev and length>=12 and h+length<=probe.BLOCK+16384+12
            dest.append((h,length));prev,h=h,nxt
        assert read32(post,tail+4)==prev
    spans=sorted(used+free);assert spans[0][0]==probe.BLOCK+12 and sum(n for _,n in spans)==16384
    assert all(a+n==b for (a,n),(b,_) in zip(spans,spans[1:]))
    assert sum(n for _,n in used)==read32(post,p.WORLD+0x76)
    out=[]
    for h,n in used:
        assert n==408;node=h+12;raw=post[probe.BLOCK][node-probe.BLOCK:node-probe.BLOCK+393]
        tag=int.from_bytes(raw[4:6],'little');ident=raw[0x12C]
        assert read32(post,0x6EE540+4*tag)==node and post[0x6ED540][tag]==1
        assert int.from_bytes(raw[:4],'little')==0x66D654 and int.from_bytes(raw[0x28:0x2A],'little')==2
        assert int.from_bytes(raw[0x2A:0x2E],'little')==probe.PLAYERS+0x1125
        out.append(dict(address=node,tag=tag,ObjectId=ident,position=list(struct.unpack_from('<3i',raw,0x1A))))
    return out

def verify(c,args,checks,r):
    post=post_context(checks,r);points={tuple(pt) for pt in c['points']};world=post[p.WORLD];room=post[p.ROOM];cells=post[p.MAP]
    u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    pcs={int(a,16) for a in r['visited_instructions']}
    assert all(call['returned'] for call in r['before_calls']) and len(r['before_calls'])==47+len(points)
    assert u(world,0x72E7)==sum(sum(v if v<128 else v-256 for v in checks[probe.CELLS_MID][i:i+16]) for i in range(0,16*len(points),16))&0xFFFFFFFF
    assert u(world,0x9D0)==int(bool(points))
    expected_nav=bytearray(25);masks={}
    for y in range(5):
        for x in range(5):
            at=16*(y*5+x)
            assert u(cells,at+8,2)&0xFFF==2
            if (x,y) not in points:assert cells[at+10]==3 and u(cells,at+2,2)&0xFFF==0;continue
            assert cells[at+10]==22 and u(cells,at+2,2)&0xFFF==1000
            assert u(cells,at+6,2)==10000
            assert bool(u(cells,at+8,2)&0x1000)==bool(c['flags']&2)
            expected=adjacency.construction7_mask(cells,5,5,x,y,22);assert cells[at+12]==expected
            masks[f'{x},{y}']=expected
            if expected==255:expected_nav[y*5+x]=4
    assert post[probe.old.BITS]==expected_nav
    assert post[p.TRIANGLES][1454:]==bytes([165])*64
    assert post[probe.BLOCK][16384+24:]==bytes([165])*64
    assert room[169:]==checks[p.ROOM][169:] and post[probe.ROOM2][169:]==checks[probe.ROOM2][169:]
    if points:
        assert u(room,4,2)==1000 and room[0x18]==13 and u(room,0x19)==p.ROOMDEF and u(room,0x1D,2)==2
        assert u(room,0xA7,2)==len(points) and u(room,0x35)==10000*len(points)
        # Native room-tile chain runs from first to last cell; terminal(0,0).
        seen=[];xy=tuple(room[0x2D:0x2F])
        while xy!=(0,0):
            assert xy in points and xy not in seen;seen.append(xy);at=16*(xy[1]*5+xy[0]);xy=tuple(cells[at+4:at+6])
        assert set(seen)==points and tuple(room[0x2F:0x31])==seen[-1]
    objects=pool_objects(post)
    if points:assert objects and all(o['ObjectId']==111 for o in objects)
    assert (0x4EC490 in pcs)==bool(points) and (0x4EC650 in pcs)==(len(points)>1)
    #4CEF20 always builds the initial all-zero 5x5mesh; construction7 adds
    #one further rebuild if the finished room contains an FF cell.
    assert r['visited_instructions'].get('0x4ce770',0)==1+int(255 in masks.values())
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    return post,dict(room_count=u(world,0x9D0),room_tiles=len(points),room_health_sum=u(room,0x35) if points else None,
        masks=masks,nav=list(expected_nav),live_objects=objects,used_object_pool_bytes=u(world,0x76),signed_MapMID_sum=u(world,0x72E7),
        full_native_remesh_calls=r['visited_instructions'].get('0x4ce770',0),steps=r['instruction_count'],
        whole_prepared_regions=len(post),whole_prepared_bytes=sum(map(len,post.values())),
        semantic_oracle_scope='Map owners/terrain/health/RoomTag/masks/NAV bytes, room tile chain/count/health, pool partition/linkage and live Object registry; other output bytes opaque measured CPU state.')

def paired(base,changed,c):
    # Same placement order and raw catalogues; change only flags in MapMID.
    wanted={a:bytearray(b) for a,b in base.items()}
    wanted[probe.CELLS_MID][:]=changed[probe.CELLS_MID]
    for x,y in c['points']:
        off=16*(y*5+x)+8;value=int.from_bytes(wanted[p.MAP][off:off+2],'little')
        probe.put(wanted[p.MAP],off,(value&~0x1000)|(0x1000 if c['flags']&2 else 0),2)
    probe.put(wanted[p.WORLD],0x72E7,(27+(c['flags'] if c['flags']<128 else c['flags']-256))*len(c['points']))
    assert changed=={a:bytes(b) for a,b in wanted.items()},[(hex(a),[hex(i) for i,(x,y) in enumerate(zip(changed[a],wanted[a])) if x!=y][:12]) for a in changed if changed[a]!=wanted[a]]
    return dict(case=c['name'],whole_compared_regions=len(base),whole_compared_bytes=sum(map(len,base.values())),
        allowed_outputs=['input MapMID flags','World+72E7 signed sum','Map cell WORD8 bit1000'],all_other_prepared_bytes_equal=True)

def main():
    rows=[];posts={};pairs=[]
    for c in cases():
        args,checks,saved,path=execute(c);post,outcome=verify(c,args,checks,saved['result']);posts[c['name']]=post
        rows.append(dict(case=c['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        if c['name'].startswith('square-'):pairs.append(paired(posts['square'],post,c))
        print(c['name'],outcome['room_tiles'],outcome['full_native_remesh_calls'],len(outcome['live_objects']),flush=True)
    report=dict(phase=138,scope=__doc__,replays=rows,paired_controls=pairs)
    (ROOT/'phase138-map-room-emulations.json').write_text(json.dumps(report,indent=2),encoding='utf8')

if __name__=='__main__':main()
