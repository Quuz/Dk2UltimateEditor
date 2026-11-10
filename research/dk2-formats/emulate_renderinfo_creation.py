"""Complete original two-primitive creation after realrawdefinitionloaders.

Prepared RenderInfo inputs/nativeallocator/cache/geometry. Fullcaller59CCF0,
native57CEB0/57CD30 and580480 execute, including realFCOS/FSIN. Picking is not
attempted until actual renderer staging is provided; field70 remainsopaqueA5.
"""
from pathlib import Path
import json,struct,math,sys
import probe_renderinfo_creation as p
from emulate_resistance import post_context
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha,put=p.sha,p.put
def cases():
    rows=[('base',{}),('priority-reversed',dict(priorities=[255,0])),('jump-highbit',dict(jump=0x80000000)),
        ('bank1',dict(bank=1)),('bank255',dict(bank=255)),('alpha-zero',dict(alpha=0)),
        ('half-width',dict(width=2048)),('double-height',dict(height=8192)),
        ('signed-position-offset',dict(offset=[1,-2,3])),('flags4000',dict(flags=0x4000))]
    return [dict(name=n,stage='creation',**c) for n,c in rows]
def execute(c):return p.execute(c)
def verify(c,args,checks,tokens,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==12 and all(x['returned'] for x in r['before_calls']) and int(r['registers']['EAX'],16)==tokens[-1]
    post=post_context(checks,r);heap=bytearray(checks[p.GLOBALS]);put(heap,0x98+30*4,0);assert post[p.GLOBALS]==heap
    pool=post[p.POOL];assert pool[-64:]==bytes([165])*64
    width,height=c.get('width',4096)/4096,c.get('height',4096)/4096;off=c.get('offset',[0,0,0]);definitions=[]
    for i,token in enumerate(tokens):
        at=token-p.POOL;b=pool[at:at+141];h=pool[at-20:at]
        wanted=bytearray(checks[p.POOL][at-20:at]);put(wanted,4,0);wanted[18]=1;assert h==wanted
        assert b[:4]==struct.pack('<I',0x66FD1C)
        xyz=struct.unpack_from('<3f',b,8);expected=(off[0]/4096,off[1]/4096,c.get('depths',[2,1])[i]-off[2]/4096)
        assert xyz==expected,(c['name'],xyz,expected)
        matrix=struct.unpack_from('<9f',b,20);wanted_diag=[-width,-height,width]
        assert all(abs(matrix[j]-wanted_diag[j//4])<1e-5 for j in [0,4,8]) and all(abs(matrix[j])<1e-5 for j in [1,2,3,5,6,7]),(c['name'],matrix)
        assert int.from_bytes(b[0x38:0x3C],'little')==p.parent.BASE+p.parent.STEP*i+0x1000
        assert struct.unpack_from('<3f',b,0x3C)==(255.0,255.0,255.0)
        assert struct.unpack_from('<f',b,0x48)[0]==(0 if c.get('alpha',255)==0 else 1)
        assert int.from_bytes(b[0x4C:0x50],'little')==p.parent.prior.ACTORS[i]
        assert int.from_bytes(b[0x50:0x54],'little')==c.get('flags',0)
        assert struct.unpack_from('<f',b,0x58)[0]==max(width,height)
        assert b[0x70:0x74]==bytes([165])*4 and b[0x5C:0x60]==bytes([165])*4
        assert b[0x8C]==c.get('bank',0)
        assert pool[at+141:at+172]==checks[p.POOL][at+141:at+172]
        definitions.append(dict(pointer=token,world_xyz=list(xyz),matrix=list(matrix),mesh_selection_index_unassigned_hex=b[0x70:0x74].hex(),output_token=p.parent.prior.ACTORS[i],forwarded_AC_byte=b[0x8C]))
    mutable={p.parent.prior.p.DEF,p.parent.prior.p.MID,p.parent.prior.CDEF2,p.parent.prior.CMID2,p.parent.prior.ODEF,p.parent.prior.OMID,p.parent.prior.p.OUT,0x740C20,p.POOL,p.GLOBALS}
    for a,b in checks.items():
        if a not in mutable:assert post[a]==b,(c['name'],hex(a))
    for definition,mid,raw_at in [(p.parent.prior.p.DEF,p.parent.prior.p.MID,p.parent.prior.source.RAW),(p.parent.prior.CDEF2,p.parent.prior.CMID2,p.parent.prior.CRAW2)]:
        raw=checks[raw_at];assert post[definition][0x6EB]==raw[0xF55] and post[definition][0x75C:0x760]==raw[0xEE0:0xEE4]
        assert int.from_bytes(post[definition][0x5AF:0x5B3],'little')==sum(post[mid])
    omid=p.parent.prior.obj.intermediate(checks[p.parent.prior.ORAW],bytes(1022));assert post[p.parent.prior.OMID]==omid
    assert post[p.parent.prior.ODEF]==p.parent.prior.obj.definition(omid,bytes(285),sha(p.parent.EXE.read_bytes()),20,0,0x11223344)
    unknown=[];low=0;md=Cs(CS_ARCH_X86,CS_MODE_32)
    for row in r['observed_reads']:
        a=int(row['address'],16);pc=int(row['pc'],16)
        if a<4096:
            ins=next(md.disasm(pe_read(p.parent.EXE.read_bytes(),pc,15),pc));assert a==0 and row['size']==4 and 'fs:[0]' in ins.op_str;low+=1
        for base,o,n in [(p.parent.prior.p.DEF,0x6EB,1),(p.parent.prior.CDEF2,0x6EB,1),(p.parent.prior.p.DEF,0x75C,4),(p.parent.prior.CDEF2,0x75C,4),(p.parent.prior.ODEF,0x112,1)]:
            if a<base+o+n and a+row['size']>base+o:unknown.append(row)
    assert not unknown
    for pc,n in [(0x59CCF0,2),(0x57CEB0,2),(0x57CD30,2),(0x580480,2)]:assert r['visited_instructions'].get(hex(pc))==n
    return post,dict(primitives=definitions,allocator_calls=2,native_complete_creators=2,raw_priority_Jump_reads=0,
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'],low_reads_only_FS_SEH=True,FS_reads=low)
def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[]
    for c in selected:
        args,checks,tokens,d,path=execute(c);_,out=verify(c,args,checks,tokens,d['result']);rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase155-renderinfo-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
