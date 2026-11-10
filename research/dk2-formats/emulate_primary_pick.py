"""Original572820->588A60->dynamic/animatedmodel virtual0C geometry selection.

Original raw Creature/Object loaders retained from phase153. Prepared native
primitive descriptors/geometry/camera/registry, not actual KMF/GPU or Actor-to-
primitive creation. No original code, vtable or geometry callback replacement.
"""
from pathlib import Path
import json,struct,sys
import emulate_pickup_selection as prior
import native_unicorn_primarypicktrace as cpu
from emulate_resistance import post_context
from emulate_element_colors import region
from build_effect_semantics import EXE
ROOT=Path(__file__).resolve().parent
sha,put=prior.sha,prior.put
REGISTRY=0x769A78;CAMERA=0x760AB8;CACHE_GLOBALS=0x766654;SCRATCH=0x766A78;FLAG=0x10600000
BASE=0x10400000;STEP=0x10000;DUMMY=0x10500000
IDENTITY=struct.pack('<9f',1,0,0,0,1,0,0,0,1)
def cases():
    rows=[('dynamic-nearer',{}),('dynamic-reverse',dict(order=[1,0])),('dynamic-tie',dict(depths=[2,2])),
        ('animated-nearer',dict(kinds=['animated','animated'])),('animated-tie',dict(kinds=['animated','animated'],depths=[2,2])),
        ('mixed-nearer',dict(kinds=['dynamic','animated'])),('mixed-reverse',dict(kinds=['animated','dynamic'],order=[1,0])),
        ('triangle-miss-coarse-fallback',dict(offsets=[10,10])),('far-exact-beats-near-coarse',dict(offsets=[0,10])),
        ('all-miss-reverse',dict(offsets=[10,10],order=[1,0])),
        ('animated-miss-coarse-fallback',dict(kinds=['animated','animated'],offsets=[10,10])),
        ('registry-outside',dict(registry_centers=[20,20])),('registry-tangent',dict(registry_centers=[10,10])),
        ('registry-inside',dict(registry_centers=[9,9])),('empty-registry',dict(empty=True)),
        ('triangle-winding-miss',dict(reverse_winding=True)),('mean-depth',dict(vertex_z=[[1,4,4],[2,2,2]],depths=[0,0])),
        ('animated-mean-depth',dict(kinds=['animated','animated'],vertex_z=[[1,4,4],[2,2,2]],depths=[0,0])),
        ('no-triangles-coarse',dict(no_triangles=True)),('near-depth-clamp',dict(depths=[-1,-2],offsets=[10,10]))]
    for a,b in [(0,255),(255,0),(1,40)]:rows.append((f'priority-{a}-{b}',dict(priorities=[a,b])))
    for jump in [0x80000000,0xFFFFFFFF]:rows.append((f'jump-{jump:08x}',dict(jump=jump)))
    for value in [0,255]:rows.append((f'object-priority-{value}',dict(nodes=[dict(kind=2,dx=800),dict(kind=2,dx=64)],object_priority=value)))
    return [dict(name=n,**c) for n,c in rows]
def build(c):
    args,checks,_=prior.build(c);mem={a:bytearray(b) for a,b in checks.items()};mem[0x7FFEE000]=bytearray([165])*0x4000
    camera=bytearray(0x50);camera[12:48]=IDENTITY;struct.pack_into('<f',camera,0x30,1);mem[CAMERA]=camera
    mem[FLAG]=bytearray([165])*4
    registry=bytearray(4100);kinds=c.get('kinds',['dynamic','dynamic']);depths=c.get('depths',[2,1]);offsets=c.get('offsets',[0,0]);centers=c.get('registry_centers',[0,0]);order=c.get('order',[0,1]);handles=[]
    for i in range(2):
        primitive=BASE+STEP*i;handle=primitive+0x1000;descriptor=primitive+0x2000;mesh=primitive+0x3000;vertices=primitive+0x4000;indices=primitive+0x5000;triangles=primitive+0x6000
        compressed,lookup,times,samples=primitive+0x7000,primitive+0x8000,primitive+0x9000,primitive+0xA000
        animated=kinds[i]=='animated';obj=bytearray(0x98);put(obj,0,0x66FD64 if animated else 0x66FD1C);struct.pack_into('<3f',obj,8,0,0,depths[i]);obj[0x14:0x38]=IDENTITY
        put(obj,0x54 if animated else 0x38,handle);put(obj,0x50 if animated else 0x4C,prior.ACTORS[i]);mem[primitive]=obj
        h=bytearray(24);put(h,0,descriptor);mem[handle]=h;handles.append(handle)
        d=bytearray(0x58);m=bytearray(0x5C);zs=c.get('vertex_z',[[0,0,0],[0,0,0]])[i]
        points=[[-1+offsets[i],-1,zs[0]],[1+offsets[i],-1,zs[1]],[offsets[i],1,zs[2]]]
        if animated:
            put(d,8,2);put(d,0x20,mesh);put(d,0x24,0 if c.get('no_triangles') else 1);put(d,0x28,3);put(d,0x2C,lookup);put(d,0x30,compressed);put(d,0x34,times);put(d,0x38,samples)
            struct.pack_into('<3f',d,0x4C,1/(1<<20),1/1024,1)
            packed=bytearray()
            for x,y,z in points:
                code=((int(x)+512)<<20)|((int(y)+512)<<10)|(int(z)+512);packed+=struct.pack('<2I',code,code)
            mem[compressed]=packed;mem[lookup]=bytearray(6);mem[times]=bytearray([0,1]*3);mem[samples]=bytearray(struct.pack('<3I',0,2,4))
            idx=bytearray(3*18)
            for j in range(3):put(idx,18*j+16,j,2)
            m[4]=1;put(m,0x14,triangles)
        else:
            put(d,8,mesh);put(d,0xC,vertices);put(d,0x18,0 if c.get('no_triangles') else 1)
            mem[vertices]=bytearray(b''.join(struct.pack('<3f',*xyz) for xyz in points));idx=bytearray(3*20)
            for j in range(3):put(idx,20*j,j)
            put(m,4,triangles);m[0x44]=1
        put(m,0x54,indices);put(m,0x5A,3,2);mem[indices]=idx;mem[triangles]=bytearray([0,2,1] if c.get('reverse_winding') else [0,1,2]);mem[mesh]=m;mem[descriptor]=d
    dummy=bytearray(24);put(dummy,0,DUMMY+0x1000);mem[DUMMY]=dummy;mem[DUMMY+0x1000]=bytearray(0x58);handles.append(DUMMY)
    for i,a in enumerate(handles):put(mem[a],4,handles[i+1] if i+1<len(handles) else 0);put(mem[a],8,handles[i-1] if i else 0)
    cache=bytearray(0x420);put(cache,0,len(handles));put(cache,4,handles[0]);put(cache,0x41C,handles[-1]);mem[CACHE_GLOBALS]=cache
    mem[SCRATCH]=bytearray([165])*(36+16);mem[0x7810A4]=bytearray(struct.pack('<f',1))
    for slot,i in enumerate(order):struct.pack_into('<3fI',registry,16*slot,centers[i],0,100,BASE+STEP*i)
    put(registry,0x1000,0 if c.get('empty') else len(order));mem[REGISTRY]=registry
    args.update(address='0x572820',registers=json.dumps(dict(ECX='0')),stack_args=[0,0,FLAG],memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),
        observe_reads=args['observe_reads']+[[BASE+STEP*i,0x98] for i in range(2)]+[[REGISTRY,4100]],observe_writes=[[FLAG,4],[CACHE_GLOBALS,0x420],[SCRATCH,52]])
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,{a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000}
def execute(c):
    args,checks=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];file=ROOT/'ghidra'/f'phase154-primary-pick-native-{tag}.json'
    if file.exists():d=json.loads(file.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),prior.p.OBS,FLAG,4));file.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,d,file
def reference(c):
    order=[] if c.get('empty') else c.get('order',[0,1]);centers=c.get('registry_centers',[0,0]);eligible=[i for i in order if centers[i]**2<100]
    exact=[i for i in eligible if c.get('offsets',[0,0])[i]==0 and not c.get('reverse_winding') and not c.get('no_triangles')]
    depths=c.get('depths',[2,1]);zs=c.get('vertex_z',[[0,0,0],[0,0,0]])
    def mean(i):return sum(max(0.001,z+depths[i]) for z in zs[i])/3
    if exact:return min(exact,key=mean),1,eligible
    if eligible:return min(eligible,key=lambda i:max(0.001,depths[i])),0,eligible+eligible
    return None,0,[]
def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==11 and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);index,exact,visits=reference(c);pointer=0 if index is None else prior.ACTORS[index]
    assert int(r['registers']['EAX'],16)==pointer and post[FLAG]==bytes([exact,165,165,165]),(c,index,exact,r['registers'],post[FLAG].hex())
    for definition,mid,raw_at in [(prior.p.DEF,prior.p.MID,prior.source.RAW),(prior.CDEF2,prior.CMID2,prior.CRAW2)]:
        raw=checks[raw_at];assert post[definition][0x6EB]==raw[0xF55] and post[definition][0x75C:0x760]==raw[0xEE0:0xEE4]
        assert int.from_bytes(post[definition][0x5AF:0x5B3],'little')==sum(post[mid])
    omid=prior.obj.intermediate(checks[prior.ORAW],bytes(1022));assert post[prior.OMID]==omid
    assert post[prior.ODEF]==prior.obj.definition(omid,bytes(285),sha(EXE.read_bytes()),20,0,0x11223344)
    handles=[BASE+0x1000,BASE+STEP+0x1000,DUMMY];wanted_order=list(handles)
    for i in visits:wanted_order.remove(handles[i]);wanted_order.append(handles[i])
    cache=bytearray(checks[CACHE_GLOBALS]);put(cache,4,wanted_order[0]);put(cache,0x41C,wanted_order[-1]);assert post[CACHE_GLOBALS]==cache
    for i,a in enumerate(wanted_order):
        h=bytearray(checks[a]);put(h,4,wanted_order[i+1] if i+1<len(handles) else 0);put(h,8,wanted_order[i-1] if i else 0);assert post[a]==h,(c['name'],hex(a))
    mutable={prior.p.DEF,prior.p.MID,prior.CDEF2,prior.CMID2,prior.ODEF,prior.OMID,prior.p.OUT,0x740C20,FLAG,CACHE_GLOBALS,SCRATCH,*handles}
    for a,b in checks.items():
        if a not in mutable:assert post[a]==b,(c['name'],hex(a))
    assert post[SCRATCH][-16:]==bytes([165])*16
    kinds=c.get('kinds',['dynamic','dynamic'])
    actual_visits=[]
    for row in r['observed_reads']:
        if int(row['pc'],16) in [0x582D39,0x585AD9]:
            actual_visits.append((int(row['address'],16)-BASE)//STEP)
    assert actual_visits==visits,(c['name'],actual_visits,visits)
    for kind,entry in [('dynamic',0x582D30),('animated',0x585AD0)]:
        assert r['visited_instructions'].get(hex(entry),0)==sum(kinds[i]==kind for i in visits),(c['name'],kind,visits)
    unknown_reads=[];low_reads=0
    for x in r['observed_reads']:
        a=int(x['address'],16);pc=int(x['pc'],16)
        if a<4096:
            from capstone import Cs,CS_ARCH_X86,CS_MODE_32
            from build_effect_semantics import pe_read
            ins=next(Cs(CS_ARCH_X86,CS_MODE_32).disasm(pe_read(EXE.read_bytes(),pc,15),pc));assert a==0 and x['size']==4 and 'fs:[0]' in ins.op_str;low_reads+=1
        for base,offset,n in [(prior.p.DEF,0x6EB,1),(prior.CDEF2,0x6EB,1),(prior.p.DEF,0x75C,4),(prior.CDEF2,0x75C,4),(prior.ODEF,0x112,1)]:
            if a<base+offset+n and a+x['size']>base+offset:unknown_reads.append(x)
    assert not unknown_reads,(c['name'],unknown_reads)
    return post,dict(selected_index=index,selected_tag=None if index is None else 9+index,exact_triangle_flag=exact,
        model_callback_order=actual_visits,native_model_callback_count=len(visits),priority_or_Jump_reads=0,
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),original_steps=r['instruction_count'],low_reads_only_FS_SEH=True,FS_reads=low_reads)
def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[]
    for c in selected:
        args,checks,d,path=execute(c);_,out=verify(c,args,checks,d['result']);rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],out['selected_tag'],out['exact_triangle_flag'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase154-primary-pick-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
