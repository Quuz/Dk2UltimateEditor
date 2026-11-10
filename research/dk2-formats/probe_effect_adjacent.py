"""Discovery: original GenerationType2 and real child Effect factories.

Uses explicit phase143 connected-engine providers. Prepared parent, successful
raw child loaders. This is not a whole World or real asset/GPU simulation.
"""
from pathlib import Path
import json,struct,sys
import probe_effect_mode18 as p
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.cpu.sha

def build(c):
    args,checks=p.build(c);mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    mem[p.scaffold.ERAW][0xF2]=2
    struct.pack_into('<8H',mem[p.scaffold.ERAW],0xCA,*c.get('slots',[1,0,0,0,0,0,0,0]))
    mem[0x69752C][0]=c.get('scale',255)
    mem[p.TILE][9]=c.get('tile_flags',0x20)
    if not c.get('flags',0x40)&0x40:
        element_calls=p.scaffold.build(c)[0]['before_calls'][:4]
        args['before_calls']=element_calls+args['before_calls']
    args['before_calls'][-1]['address']='0x4376e0'
    args.update(memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=3000000)
    args['observe_reads'] += [[0x69752C,1]]
    return args,{a:bytes(mem[a]) if a in mem else b for a,b in checks.items()}

def execute(c):
    args,checks=build(c);backend=sha(Path(p.cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];file=ROOT/'ghidra'/f'phase158-adjacent-probe-{tag}.json'
    if file.exists():d=json.loads(file.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=p.cpu.run(p.EXE,args,list(checks.items()),p.scaffold.element.STOP,p.base.PARENT,345));file.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,d,file

if __name__=='__main__':
    c=dict(name='one-child',count=1,flags=0x40)
    args,ch,d,file=execute(c);r=d['result'];print('FILE',file.name);print(json.dumps({k:r[k] for k in ['success','emulation_error','trace_tail','before_calls','effect_factory_calls','unfinished_effect_factory_call']}))
    if r['success']:
        post=post_context(ch,r);pool=post[p.scaffold.POOL];u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little');ptr=u(post[p.base.BRIDGE],0x336);out=[]
        while ptr:
            b=pool[ptr-p.scaffold.POOL:ptr-p.scaffold.POOL+345];out.append(dict(pointer=hex(ptr),id=u(b,0x105,2),xyz=list(struct.unpack_from('<3i',b,0xD6))));ptr=u(b,0x123)
        print('CHILDREN',out)
