"""Phase185 discovery: original hand release, timed stun, native action completion.

All state/action/position changes after the prepared held state are native.
Clock and observation helpers are declared fixture controls. A null renderer
is explicit; original RenderInfo must finish the source animation increment.
ActorDWORDDC is an animation increment here, not a graphics-instance pointer.
"""
from pathlib import Path
import json,struct,sys
import probe_crypt_room_assignment as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u

def build(c):
    base=dict(species=5,owner=7,entry_state=274,entry_path='hand_drop',register=False,activate=False,tick=False,exit=False,
        native_nav=True,HP=900,x=22528,room_area=True,source_trig=True,null_renderer_cache=True,movement_steps=1)
    if c.get('walk_steps'):
        base.update(native_forces=True,native_speed=c.get('native_speed',256))
    a,ch,ofs=old.build(base);mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(a['memory'])['regions']}
    mem[0x6E4144]=bytearray(b'\x01');mem[0x6E4148]=bytearray(12)
    ch[0x6E4144]=bytes(mem[0x6E4144]);ch[0x6E4148]=bytes(mem[0x6E4148])
    calls=a['before_calls'];stop=next(i for i,x in enumerate(calls) if x['address']=='0x21000000')
    calls=calls[:stop]
    call=lambda address,context:dict(address=hex(address),registers=dict(ECX=hex(context)),stack_args=[])
    snap=lambda i:call(0x21000000+256*i,0)
    clock=0x21001000;session=u(mem[p.f.WORLD],4)
    mem[clock]=bytearray(b'\xc7\x05'+struct.pack('<II',session+4,c.get('clock',40))+b'\xc3')
    calls += [snap(0),call(0x476FC0,old.CONTEXT),snap(1),call(clock,0),call(0x476FC0,old.CONTEXT),snap(2)]
    for i in range(c.get('animation_steps',0)):calls.append(call(0x4B3C10,old.ACTOR+0x44))
    if c.get('action_tick',True):
        for i in range(c.get('action_ticks',1)):calls.append(call(0x496AA0,old.ACTOR))
    calls += [snap(3),call(0x476FC0,old.CONTEXT),snap(4),call(0x476FC0,old.CONTEXT),snap(5)]
    extra_control,extra_log=0x28000000,0x29000000
    if c.get('walk_steps'):
        mem[extra_log]=bytearray([165])*(3*old.STRIDE)
        controls=[]
        def extra_snapshot(index):
            address=extra_control+256*index
            code=bytearray(b'\xc7\x05'+struct.pack('<II',extra_log+index*old.STRIDE,index)+bytes.fromhex('9c60fc'))
            for name,(off,n,at) in ofs.items():
                code+=b'\xbe'+struct.pack('<I',at)+b'\xbf'+struct.pack('<I',extra_log+index*old.STRIDE+off)+b'\xb9'+struct.pack('<I',n)+bytes.fromhex('f3a4')
            code+=bytes.fromhex('619dc3');mem[address]=code;controls.append([address,len(code)]);calls.append(call(address,0))
        def clock_at(index,value):
            address=extra_control+0x1000+256*index
            code=bytearray(b'\xc7\x05'+struct.pack('<II',session+4,value)+b'\xc3')
            mem[address]=code;controls.append([address,len(code)]);calls.append(call(address,0))
        for i in range(c['walk_steps']):
            clock_at(i,c.get('clock',40)+i+1);calls.append(call(0x488930,old.ACTOR))
        extra_snapshot(0)
        calls.append(dict(address='0x476d30',registers=dict(ECX=hex(old.CONTEXT)),stack_args=[1,0]))
        extra_snapshot(1)
        if c.get('clear_after_exit'):calls.append(call(0x4966A0,old.ACTOR+0x38D))
        clock_at(c['walk_steps'],c.get('clock',40)+c['walk_steps']+100)
        calls.append(call(0x488930,old.ACTOR));extra_snapshot(2)
        a['allowed_external_IO_code']+=controls;ch[extra_log]=bytes(mem[extra_log])
        a['observe_writes'].append([extra_log,len(mem[extra_log])]);a['observe_reads'].append([extra_log,len(mem[extra_log])])
    a['before_calls']=calls
    adapter=old.old.old.prior.readable
    mem[adapter.ADDRESS]=bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(),[region(at,b) for at,b in mem.items() if at!=adapter.ADDRESS])))
    a['allowed_external_IO_code']=[r for r in a['allowed_external_IO_code'] if r[0]!=adapter.ADDRESS]+[[adapter.ADDRESS,len(mem[adapter.ADDRESS])]]
    a['memory']=json.dumps(dict(regions=[region(at,b) for at,b in mem.items()]))
    return a,ch,ofs

def execute(c):
    a,ch,ofs=build(c);tag=sha((json.dumps(a,sort_keys=True)+sha(Path(__file__).read_bytes())).encode())[:12]
    for attempt in range(3):
        f=ROOT/'ghidra'/f'phase185-crypt-drop-recovery-{tag}{"" if not attempt else "-attempt"+str(attempt+1)}.json'
        if f.exists():s=json.loads(f.read_text(encoding='utf-8'));assert s['input']==a
        else:
            r=old.old.old.prior.old.base.cpu.run(EXE,a,list(ch.items()),p.f.OBS,old.ACTOR,len(ch[old.ACTOR]))
            s=dict(case=c,input=a,result=r);f.write_text(json.dumps(s,indent=2),encoding='utf-8')
        r=s['result']
        if r['success'] or r['emulation_error'] or r['protected_write_or_execution_failures']:break
    return a,ch,s,f,ofs

def describe(c):
    a,ch,s,f,ofs=execute(c);r=s['result'];post=post_context(ch,r);o,n,_=ofs['actor'];rows=[]
    for i in range(6):
        b=post[old.LOG][i*old.STRIDE:(i+1)*old.STRIDE];actor=b[o:o+n]
        rows.append(dict(index=i,state=u(actor,0x25E),action=u(actor,0x3A9),HP=u(actor,0x40),
            position=[u(actor,v) for v in [0x1A,0x1E,0x22]],roomtag=u(actor,0x3B9,2),graphicsDC=u(actor,0xDC),flagsF1=actor[0xF1],
            frame_progress=u(actor,0xD8),action_count=actor[0x3AE]))
    extra=[]
    if c.get('walk_steps'):
        for i in range(3):
            b=post[0x29000000][i*old.STRIDE:(i+1)*old.STRIDE];actor=b[o:o+n]
            extra.append(dict(index=i,state=u(actor,0x25E),action=u(actor,0x3A9),HP=u(actor,0x40),position=[u(actor,v) for v in [0x1A,0x1E,0x22]],roomtag=u(actor,0x3B9,2)))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],protection=r['protected_write_or_execution_failures'],
        snapshots=rows,extra=extra,tail=r['trace_tail'][-12:],file=str(f.relative_to(ROOT)))),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
