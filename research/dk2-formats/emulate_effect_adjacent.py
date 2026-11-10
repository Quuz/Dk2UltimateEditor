"""Complete original mode2 at four adjacent Q12 positions with real child factories.

Prepared parent/connected-engine providers. Original raw parent/child loaders,
Effect/Element constructors, pools, tag registration and Next chains execute.
No World scheduler, actual assets, GPU or final child deletion.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_effect_adjacent as n
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=n.p;sha=n.sha;IDS=[1,2,3,4,143,221]

def cases():
    return [dict(name='one',count=1,flags=0x40),dict(name='count0',count=0,flags=0x40),dict(name='count2',count=2,flags=0x40),
        dict(name='eighth-slot',count=1,flags=0x40,slots=[0,0,0,0,0,0,0,1]),
        dict(name='sparse-slots',count=1,flags=0x40,slots=[1,0,3,0,0,0,0,4]),
        dict(name='random-all1',count=2,flags=0x440,slots=[1]*8),
        dict(name='scale50',count=2,flags=0x4040,scale=50),dict(name='scale255',count=2,flags=0x4040,scale=255),
        dict(name='parent-misc',count=1,flags=0x40,misc=[0x1234,0xFEDC]),
        dict(name='child-misc',count=1,flags=0x40,child_misc=[0x1234,0xFEDC]),
        dict(name='life0',count=1,flags=0x40,life=0),dict(name='tile-flags0',count=1,flags=0x40,tile_flags=0),
        dict(name='partial-pool',count=1,flags=0x40,pool_size=1080),
        dict(name='element-flag0',count=1,flags=0,slots=[294,0,0,0,0,0,0,0]),
        dict(name='element-flag80',count=1,flags=0x80,slots=[294,0,0,0,0,0,0,0]),
        dict(name='both-flags',count=1,flags=0xC0)]

def plan(c,center):
    x,y,z=center;positions=[[x,y+4096,z],[x,y-4096,z],[x+4096,y,z],[x-4096,y,z]]
    if c.get('life',100)<=0:return [],[],[]
    slots=c.get('slots',[1,0,0,0,0,0,0,0]);effect=bool(c['flags']&0x40)
    selected=[1] if c['flags']&0x400 else [i for i in reversed(slots) if i]
    if c['flags']&0x400:assert slots==[1]*8
    req=[dict(id=ident,xyz=pos) for pos in positions for _ in range(c['count']) for ident in selected]
    if effect and not c.get('tile_flags',0x20)&0x20:return positions,req,[]
    created=[];outputs=[];capacity=c.get('pool_size',p.scaffold.POOL_SIZE)//(360 if effect else 300)
    nexts={1:143,2:221,3:0,4:0,143:0,221:0}
    for r in req:
        first=None;ident=r['id']
        while ident and len(created)<capacity:
            if first is None:first=ident
            created.append(dict(id=ident,xyz=r['xyz']));ident=nexts[ident] if effect else 0
        outputs.append(int(first is not None))
    return positions,req,created

def execute(c):return n.execute(c)

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'] and r['unfinished_effect_factory_call'] is None
    assert len(r['before_calls'])==(29 if c['flags']&0x40 else 33) and all(b['returned'] for b in r['before_calls'])
    assert r['visited_instructions'].get('0x4376e0',0)==1
    post=post_context(checks,r);u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    raw=post[p.scaffold.ERAW];definition=post[p.base.PARENT_DEF]
    assert raw==checks[p.scaffold.ERAW] and raw[0xF2]==2 and raw[0xF3]==c['count']
    assert definition[0x85]==2 and definition[0x8A]==c['count'] and definition[0x4B:0x4F]==raw[0xEE:0xF2]
    assert definition[0x4F:0x5F]==raw[0xCA:0xDA] and u(definition,0x5F)==c['flags']
    for a,size in [(p.scaffold.ERAW,246),(p.scaffold.EMID,426),(p.base.PARENT_DEF,148),(p.DEFS,len(IDS)*148),(p.CHILD_MID,426)]+[(p.CHILD_RAW+0x1000*i,246) for i in range(len(IDS))]:assert post[a][size:]==checks[a][size:]
    for i,ident in enumerate(IDS):
        b=post[p.DEFS][148*i:148*(i+1)];childraw=post[p.CHILD_RAW+0x1000*i]
        assert childraw==checks[p.CHILD_RAW+0x1000*i] and u(b,0x79,2)==ident and b[0x4B:0x4F]==childraw[0xEE:0xF2]
    center=list(struct.unpack_from('<3i',checks[p.base.PARENT],0xD6));positions,req,created=plan(c,center)
    effect=bool(c['flags']&0x40);pool=post[p.scaffold.POOL];actual=[];newer=0;seen=set()
    ptr=u(post[p.base.BRIDGE],0x336) if effect else u(post[p.base.PARENT],0x113)
    while ptr:
        size=345 if effect else 287;offset=ptr-p.scaffold.POOL;assert ptr not in seen and 0<=offset<=c.get('pool_size',p.scaffold.POOL_SIZE)-size;seen.add(ptr)
        b=pool[offset:offset+size]
        if effect:
            ident=u(b,0x105,2);assert ident in IDS and u(b,0)==0x66CBB4 and u(b,0x107)==p.DEFS+IDS.index(ident)*148 and u(b,0x127)==newer and u(b,0x12D,2)==7
            xyz=list(struct.unpack_from('<3i',b,0xD6));tag=u(b,0xF2,2);nxt=u(b,0x123)
        else:
            ident=u(b,0x101,2);assert ident==294 and u(b,0)==0x66CBDC and u(b,0x103)==p.base.DEF
            assert u(b,0x107,2)==7 and u(b,0x111,2)==u(checks[p.base.PARENT],0xF2,2)
            xyz=list(struct.unpack_from('<3i',b,0xD6));tag=u(b,0xF2,2);nxt=u(b,0x117)
        assert u(post[0x6D6458],4*tag)==ptr
        actual.append(dict(id=ident,xyz=xyz));newer=ptr;ptr=nxt
    assert list(reversed(actual))==created,(c['name'],list(reversed(actual)),created)
    actual_factory_requests=req if effect and c.get('tile_flags',0x20)&0x20 else []
    assert len(r['effect_factory_calls'])==len(actual_factory_requests)
    if effect:
        assert [x['effect_id'] for x in r['effect_factory_calls']]==[x['id'] for x in actual_factory_requests]
        assert r['visited_instructions'].get('0x43a8e0',0)==len(req)
        assert r['visited_instructions'].get('0x440420',0)==len(req)
    else:assert r['visited_instructions'].get('0x4352e0',0)==len(req)
    ncreated=len(created);unit=360 if effect else 300
    assert pool[c.get('pool_size',p.scaffold.POOL_SIZE):]==checks[p.scaffold.POOL][c.get('pool_size',p.scaffold.POOL_SIZE):]
    ranges=[]
    for head,tail,used in [(p.base.UH,p.base.UT,True),(p.base.FH,p.base.FT,False)]:
        cur=u(post[head],8);previous=head
        while cur and cur!=tail:
            assert p.scaffold.POOL<=cur<p.scaffold.POOL+c.get('pool_size',p.scaffold.POOL_SIZE)
            b=pool[cur-p.scaffold.POOL:];size=u(b,0);assert u(b,4)==previous and size>=12
            ranges.append((cur,size,used));previous=cur;cur=u(b,8)
        assert u(post[tail],4)==previous
    ordered=sorted(ranges);cursor=p.scaffold.POOL
    for ptr,size,_ in ordered:assert ptr==cursor;cursor+=size
    assert cursor==p.scaffold.POOL+c.get('pool_size',p.scaffold.POOL_SIZE)
    free=sum(size for _,size,used in ranges if not used)
    assert free==c.get('pool_size',p.scaffold.POOL_SIZE)-unit*ncreated
    assert sum(size for _,size,used in ranges if used)==unit*ncreated
    assert not any(int(x['address'],16)<=0x69752C<int(x['address'],16)+x['size'] for x in r['observed_reads'])
    recovered=[x for x in r['observed_reads'] if int(x['pc'],16)==0x4378C2]
    assert all(int(x['address'],16)==p.base.PARENT_DEF+0x5F and x['size']==4 for x in recovered)
    assert len(recovered)==(4 if positions else 0)
    unknown=[]
    for x in r['observed_reads']:
        a=int(x['address'],16)
        for start in [p.base.PARENT_DEF]+[p.DEFS+148*i for i in range(len(IDS))]:
            if a<start+0x4F and a+x['size']>start+0x4B:unknown.append(x)
    assert not unknown
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(p.EXE.read_bytes(),int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    return post,dict(center_Q12=center,adjacent_positions_Q12=positions,requests=req,creation_order=created,registered_children=ncreated,
        child_kind='Effects' if effect else 'EffectElements',factory_calls=len(r['effect_factory_calls']),free_bytes=free,
        bridge_wrapper_requests=len(req) if effect else 0,
        validation_calls=r['visited_instructions'].get('0x447a70',0),quality_scale_reads=0,unknown_definition_reads=0,
        recovered_argument_flag_reads=len(recovered),
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_pair(base,changed,c):
    want={a:bytearray(b) for a,b in base.items()};b=bytes.fromhex('3412dcfe')
    if c['name']=='parent-misc':
        for a,o in [(p.scaffold.ERAW,0xEE),(p.scaffold.EMID,0x1A2),(p.base.PARENT_DEF,0x4B)]:want[a][o:o+4]=b
    elif c['name']=='child-misc':
        for i in range(len(IDS)):
            want[p.CHILD_RAW+0x1000*i][0xEE:0xF2]=b;want[p.DEFS][148*i+0x4B:148*i+0x4F]=b
        want[p.CHILD_MID][0x1A2:0x1A6]=b
    elif c['name']=='scale255':want[0x69752C][0]=255
    else:raise AssertionError(c)
    assert want==changed
    return dict(case=c['name'],compared_regions=len(want),compared_bytes=sum(map(len,want.values())),only_expected_input_storage_changes=True)

def main():
    rows=[];pairs=[];posts={}
    for c in cases():
        args,ch,d,file=execute(c);post,out=verify(c,args,ch,d['result']);posts[c['name']]=post
        if c['name'] in ['parent-misc','child-misc']:pairs.append(compare_pair(posts['one'],post,c))
        if c['name']=='scale255':pairs.append(compare_pair(posts['scale50'],post,c))
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out['child_kind'],out['registered_children'],'verified',flush=True)
    (ROOT/'phase158-adjacent-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=pairs),indent=2),encoding='utf8')
if __name__=='__main__':main()
