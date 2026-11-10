"""Mode18 slot bounds, real child Effect factories/Next chains, and opaque words."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_effect_mode18 as p
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
sha=p.cpu.sha
IDS=[1,2,3,4,143,221]

def cases():
    return [dict(name='source-chains',count=4),dict(name='count3',count=3),dict(name='count7',count=7),dict(name='count8',count=8),
        dict(name='flag-clear',count=4,flags=0),dict(name='both-target-flags',count=4,flags=0xC0),
        dict(name='sparse-slots',count=4,slots=[1,0,3,0,65535,65535,65535,65535]),
        dict(name='parent-misc',count=4,misc=[0x1234,0xFEDC]),dict(name='child-misc',count=4,child_misc=[0x1234,0xFEDC]),
        dict(name='next-cleared',count=4,next_ids={1:0,2:0}),dict(name='zero-lifetime',count=4,life=0),
        dict(name='partial-chain',count=4,pool_size=1080)]

def requests(c):
    if c.get('life',100)<=0 or not c.get('flags',0x40)&0x40:return []
    slots=c.get('slots',[1,2,3,4,65535,65535,65535,65535])
    return [i for _ in range(c['count']//4) for i in reversed(slots[:4]) if i]

def planned_creation(c):
    nexts={1:143,2:221,3:0,4:0,143:0,221:0};nexts.update(c.get('next_ids',{}))
    capacity=c.get('pool_size',p.scaffold.POOL_SIZE)//360;created=[];outputs=[]
    for req in requests(c):
        chain=[];i=req
        while i and len(created)<capacity:
            assert i in nexts;chain.append(i);created.append(i);i=nexts[i]
        outputs.append(dict(effect_id=req,returned=int(bool(chain)),first=chain[0] if chain else None,chain=chain))
    return created,outputs

def execute(c):
    args,checks,saved,path=p.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert r['unfinished_effect_factory_call'] is None
    return args,checks,saved,path

def verify(c,args,checks,r):
    post=post_context(checks,r);base=p.base;u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    for a,n in [(p.scaffold.ERAW,246),(p.scaffold.EMID,426),(base.PARENT_DEF,148),(p.DEFS,len(IDS)*148),(p.CHILD_MID,426)]+[(p.CHILD_RAW+0x1000*i,246) for i in range(len(IDS))]:
        assert post[a][n:]==checks[a][n:]
    assert len(r['before_calls'])==29 and all(x['returned'] for x in r['before_calls'])
    assert post[p.scaffold.ERAW]==checks[p.scaffold.ERAW] and post[p.scaffold.ERAW][0xF2]==18
    assert post[base.PARENT_DEF][0x85]==18 and post[base.PARENT_DEF][0x8A]==c['count']
    for index,ident in enumerate(IDS):
        raw=post[p.CHILD_RAW+0x1000*index];d=post[p.DEFS][index*148:(index+1)*148]
        assert raw==checks[p.CHILD_RAW+0x1000*index] and u(d,0x79,2)==ident
        assert d[0x4B:0x4F]==raw[0xEE:0xF2] and u(d,0x7B,2)==u(raw,0xC0,2)
    # Every low-page access must be the original x86 SEH FS:[0] access.
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(p.EXE.read_bytes(),int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    assert not [x for x in r['observed_writes'] if 0x66CBB4<=int(x['address'],16)<0x66CBD4]
    created,outputs=planned_creation(c);n=len(created);pool=post[p.scaffold.POOL]
    ptr=u(post[base.BRIDGE],0x336);children=[];seen=set();newer=0
    while ptr:
        assert ptr not in seen and p.scaffold.POOL<=ptr<=p.scaffold.POOL+c.get('pool_size',p.scaffold.POOL_SIZE)-345;seen.add(ptr)
        b=pool[ptr-p.scaffold.POOL:ptr-p.scaffold.POOL+345];ident=u(b,0x105,2)
        assert ident in IDS and u(b,0)==0x66CBB4 and u(b,0x107)==p.DEFS+IDS.index(ident)*148
        assert u(b,0x127)==newer and u(b,0x12D,2)==7
        tag=u(b,0xF2,2);assert u(post[0x6D6458],tag*4)==ptr
        assert b[0xD6:0xE2]==checks[base.PARENT][0xD6:0xE2]
        raw=checks[p.CHILD_RAW+IDS.index(ident)*0x1000]
        assert u(raw,0xBA,2)<=u(b,0x10B)<=u(raw,0xBC,2)
        children.append(dict(pointer=ptr,id=ident,tag=tag));newer=ptr;ptr=u(b,0x123)
    assert [r['id'] for r in reversed(children)]==created
    byptr={x['pointer']:x for x in children}
    calls=r['effect_factory_calls'];assert len(calls)==len(outputs)
    for actual,wanted in zip(calls,outputs):
        assert actual['effect_id']==wanted['effect_id'] and actual['returned']==wanted['returned']
        if wanted['returned']:assert byptr[actual['output_pointer']]['id']==wanted['first']
        else:assert actual['output_pointer'] is None
    assert r['visited_instructions'].get('0x4347e0',0)==n and r['visited_instructions'].get('0x4352e0',0)==0
    assert u(post[base.BRIDGE],0x135F)==360*n and u(post[0x69793C],0)==4094-n
    assert u(post[base.OUT],0)==n # explicit declined-light provider calls
    size=c.get('pool_size',p.scaffold.POOL_SIZE)
    assert pool[size:]==checks[p.scaffold.POOL][size:]
    # Partition original free/used lists, including exhausted free-list control.
    regions=[]
    for head,tail,used in [(base.UH,base.UT,True),(base.FH,base.FT,False)]:
        cur=u(post[head],8);previous=head
        while cur and cur!=tail:
            assert cur>=p.scaffold.POOL and cur<p.scaffold.POOL+size
            b=pool[cur-p.scaffold.POOL:];width=u(b,0);assert u(b,4)==previous and width>=12
            regions.append((cur-p.scaffold.POOL,width,used));previous=cur;cur=u(b,8)
        assert u(post[tail],4)==previous
    ordered=sorted(regions);assert ordered and ordered[0][0]==0
    assert all(a+width==b for (a,width,_),(b,_,_) in zip(ordered,ordered[1:])) and ordered[-1][0]+ordered[-1][1]==size
    assert sum(w for _,w,used in regions if used)==360*n
    reads=[x for x in r['observed_reads'] if x['pc']=='0x439157']
    offsets=[int(x['address'],16)-base.PARENT_DEF for x in reads]
    batches=c['count']//4 if c.get('life',100)>0 else 0
    assert offsets==[0x55,0x53,0x51,0x4F]*batches
    unknown=[]
    for x in r['observed_reads']:
        a=int(x['address'],16)
        for start in [base.PARENT_DEF]+[p.DEFS+148*i for i in range(len(IDS))]:
            if a<start+0x4F and a+x['size']>start+0x4B:unknown.append(x)
    assert not unknown
    return post,dict(requests=requests(c),creation_order=created,first_output_per_request=outputs,registered_children=n,
        slot_read_offsets=offsets,unknown_definition_reads=unknown,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def pair(reference,changed,c):
    expected={a:bytearray(b) for a,b in reference.items()};b=bytes.fromhex('3412dcfe')
    if c['name']=='parent-misc':
        for a,o in [(p.scaffold.ERAW,0xEE),(p.scaffold.EMID,0x1A2),(p.base.PARENT_DEF,0x4B)]:expected[a][o:o+4]=b
    else:
        for i in range(len(IDS)):
            expected[p.CHILD_RAW+0x1000*i][0xEE:0xF2]=b;expected[p.DEFS][148*i+0x4B:148*i+0x4F]=b
        expected[p.CHILD_MID][0x1A2:0x1A6]=b
    assert {a:bytes(b) for a,b in expected.items()}==changed
    return dict(case=c['name'],whole_compared_bytes=sum(map(len,expected.values())),regions=len(expected),only_opaque_storage_copies_differ=True)

def main():
    rows=[];pairs=[];reference=None
    for c in cases():
        args,checks,saved,path=execute(c);post,out=verify(c,args,checks,saved['result'])
        if c['name']=='source-chains':reference=post
        if c['name'] in ['parent-misc','child-misc']:pairs.append(pair(reference,post,c))
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
        print(c['name'],out['creation_order'],flush=True)
    (ROOT/'phase143-effect-mode18-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=pairs),indent=2),encoding='utf8')

if __name__=='__main__':main()
