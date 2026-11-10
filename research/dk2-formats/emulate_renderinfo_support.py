"""Nativeallocator andRenderInfo coordinate helpers with independent arithmetic oracles."""
from pathlib import Path
import json,struct
import emulate_renderinfo_creation as create
from emulate_resistance import post_context
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
p=create.p;sha=create.sha
OUT1,OUT2=0x10820000,0x10821000
def cases():return [dict(name='allocate141',kind='allocator',size=141),dict(name='allocate153',kind='allocator',size=153),
    dict(name='position-zero',kind='position',offset=[0,0,0]),dict(name='position-signed',kind='position',offset=[1,-2,3])]
def build(c):
    args,checks,_=p.build(dict(name='support',stage='creation',offset=c.get('offset',[0,0,0])));mem={a:bytearray(b) for a,b in checks.items()};mem[0x7FFEE000]=bytearray([165])*0x4000
    if c['kind']=='allocator':entry=0x57CEB0;registers=dict(ECX='0');stack=[c['size']]
    else:
        mem[p.XYZ0]=bytearray(struct.pack('<3i',8192,4096,-4096));mem[p.XYZ0+0x100]=bytearray(struct.pack('<3i',-4096,0,8192))
        mem[OUT1]=bytearray([165])*28;mem[OUT2]=bytearray([165])*28
        entry=0x59C2A0;registers=dict(ECX='0');stack=[p.parent.prior.ACTORS[0]+0x44,p.XYZ0,p.XYZ0+0x100,OUT1,OUT2]
    args.update(address=hex(entry),registers=json.dumps(registers),before_calls=[],stack_args=stack,max_steps=100000,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),observe_reads=[],observe_writes=[])
    return args,{a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000}
def execute(c):
    args,checks=build(c);backend=sha(Path(p.parent.cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];path=ROOT/'ghidra'/f'phase155-support-native-{tag}.json'
    if path.exists():d=json.loads(path.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=p.parent.cpu.run(p.parent.EXE,args,list(checks.items()),p.parent.prior.p.OBS,p.POOL,384));path.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,d,path
def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'] and not r['before_calls']
    wanted={a:bytearray(b) for a,b in checks.items()}
    if c['kind']=='allocator':
        create.put(wanted[p.GLOBALS],0x98+30*4,p.POOL+0xC0);wanted[p.POOL][18]=1;create.put(wanted[p.POOL],0xC0+4,0);ret=p.POOL+20
    else:
        off=c['offset']
        for a,xyz in [(OUT1,[8192,4096,-4096]),(OUT2,[-4096,0,8192])]:
            struct.pack_into('<3f',wanted[a],0,(xyz[0]+off[0])/4096,(xyz[1]+off[1])/4096,-(xyz[2]+off[2])/4096)
        ret=OUT1
    post=post_context(checks,r);assert all(post[a]==b for a,b in wanted.items()),[(hex(a),post[a].hex()[:40],b.hex()[:40]) for a,b in wanted.items() if post[a]!=b]
    assert int(r['registers']['EAX'],16)==ret,(c['name'],r['registers'])
    return post,dict(native_return=ret,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
def main():
    rows=[]
    for c in cases():
        args,checks,d,path=execute(c);_,out=verify(c,args,checks,d['result']);rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],'verified',flush=True)
    (ROOT/'phase155-support-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
