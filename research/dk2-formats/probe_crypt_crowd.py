"""Phase187 extension: King creates40 Dancers through real Crypt activation.

Then the generic room dispatcher assigns a second Vampire visitor, whose AI
chooses waiting276/action76. No explicit Dancer count/factory call is supplied.
"""
from pathlib import Path
import json
import probe_crypt_movement as old
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u

def build():
    c=dict(species=21,owner=7,entry_state=274,register=False,activate=False,tick=False,exit=True,native_nav=True,HP=900,
        room_area=True,null_renderer_cache=True,registration='ai',register_dispatch=True,movement_steps=2,spare_blocks=42)
    args,ch,offsets=old.build(c);calls=args['before_calls'];other=old.old.RECIPIENT
    at=next(i for i,x in enumerate(calls) if x['address']==hex(old.CONTROL+256*2))
    calls[at+1:at+1]=[
        dict(address='0x4e8da0',registers=dict(ECX=hex(other)),stack_args=[0]),
        dict(address='0x476fc0',registers=dict(ECX=hex(other+0x24E)),stack_args=[]),
        dict(address='0x476fc0',registers=dict(ECX=hex(other+0x24E)),stack_args=[])]
    return args,ch,offsets

def execute():
    args,ch,offsets=build();tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase187-crypt-crowd-King-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.old.prior.old.base.cpu.run(EXE,args,list(ch.items()),p.f.OBS,old.ACTOR,len(ch[old.ACTOR]))
        saved=dict(input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,ch,saved,file,offsets

def main():
    a,ch,s,f,ofs=execute();r=s['result'];post=post_context(ch,r);rows=[]
    for i in range(6):
        b=post[old.LOG][i*old.STRIDE:(i+1)*old.STRIDE];at=lambda k:b[ofs[k][0]:ofs[k][0]+ofs[k][1]]
        main,visitor,room,owner=at('actor'),at('recipient'),at('room'),at('owner')
        rows.append(dict(index=i,King_state=u(main,0x25E),King_action=u(main,0x3A9),KingHP=u(main,0x40),
            visitor_state=u(visitor,0x25E),visitor_action=u(visitor,0x3A9),visitorHP=u(visitor,0x40),
            registered=u(room,0x53,2),dancers=u(room,0x55),counters=list(owner[0x108A:0x109C])))
    out=dict(success=r['success'],error=r['emulation_error'],failures=r['protected_write_or_execution_failures'],snapshots=rows,
        tail=r['trace_tail'][-20:],file=str(f.relative_to(ROOT)))
    (ROOT/'phase187-crypt-crowd-discovery.json').write_text(json.dumps(out,indent=2),encoding='utf-8');print(json.dumps(out),flush=True)

if __name__=='__main__':main()
