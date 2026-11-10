"""Original44DEC0 cursor fallback after real Creature/Object raw loaders.

Prepared cell lists and Actor instances; native World vtable and all code
remain intact. This is the fallback helper, not complete primary ray picking.
"""
from pathlib import Path
import hashlib,json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import dk2_formats as fmt
import emulate_raw_creature_definition as source
import native_unicorn_keepertrace as cpu
import native_object_loading as obj
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from build_effect_semantics import EXE
ROOT=Path(__file__).resolve().parent
p=source.parent
MAP,CTX,OUT=0x10080000,0x10090000,0x10091000
ACTORS=[0x100A0000+0x1000*i for i in range(4)]
ODEF,OMID,ORAW=0x100B0000,0x100C0000,0x100C2000
CDEF2,CMID2,CRAW2=0x100D0000,0x100D2000,0x100D4000
sha=lambda b:hashlib.sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def cases():
    rows=[('first-farther-creature',{}),('reversed-creatures',dict(order=[1,0])),
        ('near-object-before-creature',dict(nodes=[dict(kind=2,dx=16),dict(kind=0,dx=1000)])),
        ('nearer-creature-after-object',dict(nodes=[dict(kind=2,dx=800),dict(kind=0,dx=64)])),
        ('objects-nearest',dict(nodes=[dict(kind=2,dx=800),dict(kind=2,dx=64)])),
        ('objects-tie',dict(nodes=[dict(kind=2,dx=64),dict(kind=2,dx=-64)])),
        ('creatures-tie',dict(nodes=[dict(kind=0,dx=64),dict(kind=0,dx=-64)])),
        ('object-unpickable',dict(nodes=[dict(kind=2,dx=16)],object_id=17)),
        ('heart-special-ID13',dict(nodes=[dict(kind=2,dx=16)],object_id=13)),
        ('no-candidates',dict(nodes=[])),('outside-radius',dict(nodes=[dict(kind=0,dx=0x800)])),
        ('object-default-boundary',dict(nodes=[dict(kind=2,dx=0x400)])),
        ('object-default-inside',dict(nodes=[dict(kind=2,dx=0x3FF)])),
        ('object-only-boundary',dict(nodes=[dict(kind=2,dx=0x800)],mode=1)),
        ('object-only-inside',dict(nodes=[dict(kind=2,dx=0x7FF)],mode=1)),
        ('object-only-nearest',dict(nodes=[dict(kind=0,dx=16),dict(kind=2,dx=800),dict(kind=2,dx=64)],mode=1)),
        ('first-excluded-state',dict(nodes=[dict(kind=0,dx=100,state=0xD1),dict(kind=0,dx=800)])),
        ('first-excluded-typebyte',dict(nodes=[dict(kind=0,dx=100,typebyte=0x2E),dict(kind=0,dx=800)])),
        ('first-excluded-flag',dict(nodes=[dict(kind=0,dx=100,flags=0x200000),dict(kind=0,dx=800)])),
        ('earlier-cell-first',dict(origin=[0x8100,0x8100],nodes=[dict(kind=0,dx=-1000,dy=-1000),dict(kind=0,dx=16)])),
        ('Y-outer-X-inner-order',dict(origin=[0x8F00,0x8F00],nodes=[dict(kind=0,dx=1000,dy=-100),dict(kind=0,dx=-100,dy=1000)])),
        ('edge-zero',dict(origin=[0x100,0x100],nodes=[dict(kind=0,dx=16)])),
        ('edge-last',dict(origin=[0xFF00,0xFF00],nodes=[dict(kind=0,dx=-16)]))]
    for a,b in [(0,255),(255,0),(1,40),(128,255),(255,255)]:rows.append((f'priority-{a}-{b}',dict(priorities=[a,b])))
    for v in [0,1,255]:rows.append((f'object-priority-{v}',dict(nodes=[dict(kind=2,dx=800),dict(kind=2,dx=64)],object_priority=v)))
    for v in [0,0x80000000,0xFFFFFFFF]:rows.append((f'jump-{v:08x}',dict(jump=v)))
    return [dict(name=n,**c) for n,c in rows]
def build(c):
    args0,_=source.build(dict(name='raw-imp',id=1,modify=None),0)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args0['memory'])['regions'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [p.DRIVER,p.OBS,0x7FFEE000,source.SHADOW]}
    calls=[];priorities=c.get('priorities',[0,255]);raw0=next(r for r in source.RECORDS if r[0xF4B]==1)
    for definition,mid,raw_at,priority in [(p.DEF,p.MID,source.RAW,priorities[0]),(CDEF2,CMID2,CRAW2,priorities[1])]:
        raw=bytearray(raw0);raw[0xF55]=priority;put(raw,0xEE0,c.get('jump',0))
        mem[definition]=bytearray([165])*2560;mem[mid]=bytearray([165])*6457;mem[raw_at]=raw
        calls += [dict(address=hex(a),registers=dict(ECX=hex(ecx)),stack_args=stack) for a,ecx,stack in [
            (0x491DC0,definition,[]),(0x568820,mid,[]),(0x568E30,mid,[raw_at]),(0x492320,definition,[mid,p.WORLD,0])]]
    object_source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'
    oraw=bytearray(next(r for s in fmt.parse(object_source.read_bytes()) if s.signature==240 for r in s.records if r[0x35A]==c.get('object_id',1)))
    if 'object_priority' in c:oraw[0x35D]=c['object_priority']
    mem[ODEF]=bytearray(285);mem[OMID]=bytearray([165])*1022;mem[ORAW]=oraw
    calls += [dict(address=hex(a),registers=dict(ECX=hex(ecx)),stack_args=stack) for a,ecx,stack in [(0x56D4F0,OMID,[]),(0x56D620,OMID,[ORAW]),(0x49DD60,ODEF,[OMID,0])]]
    mem[0x6F2590]=bytearray(pack32(20));mem[0x6FCDCF]=bytearray(4);mem[0x740C20]=bytearray(32)
    put(mem[p.WORLD],0x9E6,16);put(mem[p.WORLD],0x9EA,16);put(mem[p.WORLD],0x9EE,MAP)
    mem[CTX]=bytearray(8);put(mem[CTX],4,p.SESSION);mem[OUT]=bytearray(pack32(0xCAFEBABE))
    nodes=c.get('nodes',[dict(kind=0,dx=1000),dict(kind=0,dx=64)])
    order=c.get('order',list(range(len(nodes))));origin=c.get('origin',[0x8800,0x8800]);cells=bytearray(16*16*16);tags=bytearray(32*4);lists={}
    for i,n in enumerate(nodes):
        b=bytearray(0x500);put(b,0x12,n['kind']);x,y=origin[0]+n.get('dx',0),origin[1]+n.get('dy',0);struct.pack_into('<3i',b,0x1A,x,y,0x1000)
        put(b,4,9+i,2);put(b,0x370,p.DEF if i==0 else CDEF2);put(b,0x12D,ODEF);put(b,0x25E,n.get('state',0));put(b,0x3E6,n.get('flags',0));b[0x36F]=n.get('typebyte',0);b[0x12C]=c.get('object_id',1)
        mem[ACTORS[i]]=b;put(tags,4*(9+i),ACTORS[i]);assert 0<=x>>12<16 and 0<=y>>12<16
    for i in order:
        b=mem[ACTORS[i]];x,y=struct.unpack_from('<2i',b,0x1A);lists.setdefault((x>>12,y>>12),[]).append(i)
    for (x,y),indices in lists.items():
        put(cells,16*(y*16+x),9+indices[0],2)
        for i,j in zip(indices,indices[1:]):put(mem[ACTORS[i]],0xC,9+j,2)
    mem[MAP]=cells;mem[0x6EE540]=tags;mem[0x7FFEE000]=bytearray([165])*0x4000
    args=dict(program='DKII-DX.exe',address='0x44dec0',registers=json.dumps(dict(ECX=hex(CTX))),stack_args=[*origin,OUT,c.get('mode',0)],before_calls=calls,
        max_steps=1000000,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),
        allowed_external_IO_code=[[a,len(mem[a])] for a in [p.GFX_CB,p.SOUND_CB]],
        observe_reads=[[p.DEF,2199],[CDEF2,2199],[ODEF,285],[0,4096]]+[[ACTORS[i],0x500] for i in range(len(nodes))],observe_writes=[[OUT,4]])
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    checks={a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000}
    return args,checks,dict(nodes=nodes,order=order,origin=origin,lists=lists)
def reference(c,e):
    chosen=None;best=0x800;object_candidate=None;object_best=0x400;cx,cy=[v>>12 for v in e['origin']]
    flags=int.from_bytes(next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd').read_bytes()) if s.signature==240 for r in s.records if r[0x35A]==c.get('object_id',1))[0x344:0x348],'little')
    visited=[]
    for y in range(max(0,cy-1),min(15,cy+1)+1):
        for x in range(max(0,cx-1),min(15,cx+1)+1):
            for i in e['lists'].get((x,y),[]):
                visited.append(i);n=e['nodes'][i];dx,dy=abs(n.get('dx',0)),abs(n.get('dy',0));d=max(dx,dy)+min(dx,dy)//2
                if d>=best:continue
                eligible_object=n['kind']==2 and (flags&0x100 or c.get('object_id',1)==13)
                if c.get('mode',0):
                    if eligible_object:chosen=i;best=d
                elif chosen is None:
                    if n['kind']==0 and n.get('typebyte',0)!=0x2E and not n.get('flags',0)&0x200000 and n.get('state',0) not in [0xD6,0x4C,0xD1]:chosen=i;best=d
                    elif eligible_object and d<object_best:object_candidate=i;object_best=d
    if chosen is None:chosen=object_candidate
    return chosen,visited
def execute(c):
    args,checks,e=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];file=ROOT/'ghidra'/f'phase153-pickup-native-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf8'));assert saved['input']==args and saved['backend_sha256']==backend
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),p.OBS,OUT,4));file.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,checks,e,saved,file
def verify(c,args,checks,e,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'],r['registers'])
    assert len(r['before_calls'])==11 and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);index,order=reference(c,e);ptr=0xCAFEBABE if index is None else ACTORS[index]
    actual_order=[ACTORS.index(int(x['address'],16)-0x1E) for x in r['observed_reads'] if int(x['pc'],16)==0x44DFBC]
    assert actual_order==order,(c['name'],actual_order,order)
    assert int(r['registers']['EAX'],16)==int(index is not None) and int.from_bytes(post[OUT],'little')==ptr,(c,index,r['registers'],post[OUT].hex())
    for definition,mid,raw_at in [(p.DEF,p.MID,source.RAW),(CDEF2,CMID2,CRAW2)]:
        raw=checks[raw_at];assert post[definition][0x6EB]==raw[0xF55] and post[definition][0x75C:0x760]==raw[0xEE0:0xEE4]
        assert int.from_bytes(post[definition][0x5AF:0x5B3],'little')==sum(post[mid])
    omid=obj.intermediate(checks[ORAW],bytes(1022));assert post[OMID]==omid
    assert post[ODEF]==obj.definition(omid,bytes(285),sha(EXE.read_bytes()),20,0,0x11223344)
    mutable={p.DEF,p.MID,CDEF2,CMID2,ODEF,OMID,p.OUT,0x740C20,OUT}
    for a,b in checks.items():
        if a not in mutable:assert post[a]==b,(c['name'],hex(a))
    overlaps=[];md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for x in r['observed_reads']:
        pc=int(x['pc'],16);a=int(x['address'],16)
        if a<4096:
            # Full Creature loader uses FS:0; decode from original PE below.
            from build_effect_semantics import pe_read
            ins=next(md.disasm(pe_read(EXE.read_bytes(),pc,15),pc));assert a==0 and x['size']==4 and 'fs:[0]' in ins.op_str
        if not 0x44DEC0<=pc<0x44E125:continue
        for base,o,n,name in [(p.DEF,0x6EB,1,'CreaturePriority1'),(CDEF2,0x6EB,1,'CreaturePriority2'),(p.DEF,0x75C,4,'CreatureJump1'),(CDEF2,0x75C,4,'CreatureJump2'),(ODEF,0x112,1,'ObjectPriority')]:
            if a<base+o+n and a+x['size']>base+o:overlaps.append(name)
    assert not overlaps
    return post,dict(selected_index=index,selected_tag=None if index is None else 9+index,scan_order=order,
        native_return=int(index is not None),priority_or_Jump_reads_in_fallback=0,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[]
    for c in selected:
        args,checks,e,saved,file=execute(c);_,out=verify(c,args,checks,e,saved['result']);rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out['selected_tag'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase153-pickup-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
