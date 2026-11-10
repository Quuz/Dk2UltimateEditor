"""Phase183 discovery: generic room assignment derives274 from original table.

Reuse the frozen182 source/World/NAV/renderer scene. New declared wrapper
only passes explicit arguments to original4E8DA0 and captures its EAX.
No Crypt state number is passed by the host on the room-assignment path.
"""
from pathlib import Path
import json,struct,sys
import probe_crypt_movement as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u
ACTOR,CONTEXT,ROOM,LOG,STRIDE=old.ACTOR,old.CONTEXT,old.ROOM,old.LOG,old.STRIDE
ENTRY_RESULT,ENTRY_CODE=0x26000000,0x26001000

def build(c):
    args,checks,offsets=old.build(c)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    if 'raw_hate' in c:
        p.f.put(mem[p.target.RAW],0xE74,c['raw_hate'])
        checks[p.target.RAW]=bytes(mem[p.target.RAW])
    if c.get('blocked_status'):
        p.f.put(mem[ACTOR],0x3F2,c['blocked_status'],2);checks[ACTOR]=bytes(mem[ACTOR])
    mode=c.get('assignment_mode',0)
    code=bytearray(b'\x68'+struct.pack('<I',mode)+b'\xb9'+struct.pack('<I',ACTOR)+b'\xb8'+struct.pack('<I',0x4E8DA0)+bytes.fromhex('ffd0a3')+struct.pack('<I',ENTRY_RESULT)+b'\xc3')
    result=bytearray([165])*68
    if c.get('entry_path')=='hand_drop':
        # Explicit already-held state; the original drop removes the real
        # tag from the player's hand and restores ordinary map membership.
        # Actor+16=2 is the not-in-map sentinel, not a third linked-list head:
        # native4B4F60 skips removal completely for that sentinel.
        owner=u(mem[ACTOR],0x2A);actor=mem[ACTOR]
        p.f.put(mem[owner],0xB2,101,2);mem[owner][0x1BF]=1
        cell=((u(actor,0x1E)>>12)*32+(u(actor,0x1A)>>12))*16
        assert u(mem[p.f.MAP],cell,2)==101
        nxt=u(actor,0xC,2);p.f.put(mem[p.f.MAP],cell,nxt,2)
        if nxt:p.f.put(mem[old.old.RECIPIENT],0xA,0,2)
        p.f.put(actor,0xA,0,2);p.f.put(actor,0xC,0,2);p.f.put(actor,0x16,2)
        p.f.put(actor,0x3E6,u(actor,0x3E6)|1)
        p.f.put(result,0x10,c.get('direction',0x600),2)
        for j,off in enumerate([0x1A,0x1E,0x22]):p.f.put(result,0x20+4*j,u(actor,off))
        code=bytearray(b'\x68'+struct.pack('<I',ENTRY_RESULT+0x10)+b'\x68'+struct.pack('<I',ENTRY_RESULT+0x20)+b'\xb9'+struct.pack('<I',owner)+b'\xb8'+struct.pack('<I',0x4BC710)+bytes.fromhex('ffd0a3')+struct.pack('<I',ENTRY_RESULT)+b'\xc3')
        bound=next((a for a,b in mem.items() if a<=0x6E5054 and 0x6E5058<=a+len(b)),None)
        if bound is None:mem[0x6E5054]=bytearray(struct.pack('<I',p.f.WORLD));checks[0x6E5054]=bytes(mem[0x6E5054])
        else:p.f.put(mem[bound],0x6E5054-bound,p.f.WORLD);checks[bound]=bytes(mem[bound])
        for a in [ACTOR,owner,old.old.RECIPIENT,p.f.MAP]:checks[a]=bytes(mem[a])
    elif c.get('entry_path')=='post_recovery':
        # A separate explicit after-recovery boundary: native257 dispatch
        # chooses the room state. This does not claim the animation finished
        # during the held/drop trace or emulate the live renderer.
        code=bytearray(b'\x6a\x00\x68'+struct.pack('<I',257)+b'\xb9'+struct.pack('<I',CONTEXT)+b'\xb8'+struct.pack('<I',0x476D30)+bytes.fromhex('ffd0')+
            b'\xb9'+struct.pack('<I',CONTEXT)+b'\xb8'+struct.pack('<I',0x476FC0)+bytes.fromhex('ffd0a3')+struct.pack('<I',ENTRY_RESULT)+b'\xc3')
        # Already-initialized null-vector singleton, identical to the
        # accepted portal-exit fixture; CRT startup/shutdown is outside scope.
        mem[0x6E4144]=bytearray(b'\x01');mem[0x6E4148]=bytearray(12)
        checks[0x6E4144]=bytes(mem[0x6E4144]);checks[0x6E4148]=bytes(mem[0x6E4148])
    mem[ENTRY_CODE]=code;mem[ENTRY_RESULT]=result
    checks[ENTRY_RESULT]=bytes(mem[ENTRY_RESULT])
    calls=args['before_calls'];found=[]
    for i,x in enumerate(calls):
        if x['address']=='0x476d30' and x['stack_args']==[c.get('entry_state',274),0]:found.append(i)
    assert len(found)==1,(c,found)
    calls[found[0]]=dict(address=hex(ENTRY_CODE),registers=dict(ECX='0'),stack_args=[])
    args['allowed_external_IO_code'].append([ENTRY_CODE,len(code)])
    args['observe_writes'].append([ENTRY_RESULT,68])
    # Added buffers require updating the declared imported readability gate.
    adapter=old.old.prior.readable
    mem[adapter.ADDRESS]=bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=adapter.ADDRESS])))
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=adapter.ADDRESS]+[[adapter.ADDRESS,len(mem[adapter.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,checks,offsets

def execute(c):
    args,ch,offsets=build(c)
    deps=sha(Path(__file__).read_bytes())+sha(Path(old.__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    for attempt in range(3):
        suffix='' if attempt==0 else f'-attempt{attempt+1}'
        file=ROOT/'ghidra'/f'phase183-crypt-room-assignment-{tag}{suffix}.json'
        if file.exists():s=json.loads(file.read_text(encoding='utf-8'));assert s['input']==args
        else:
            r=old.old.prior.old.base.cpu.run(EXE,args,list(ch.items()),p.f.OBS,ACTOR,len(ch[ACTOR]))
            s=dict(case=c,input=args,result=r);file.write_text(json.dumps(s,indent=2),encoding='utf-8')
        r=s['result']
        if r['success'] or r['emulation_error'] or r['protected_write_or_execution_failures']:break
    return args,ch,s,file,offsets

def describe(c):
    args,ch,s,f,offsets=execute(c);r=s['result'];post=post_context(ch,r);rows=[]
    for i in range(6):
        b=post[LOG][i*STRIDE:(i+1)*STRIDE]
        get=lambda name:b[offsets[name][0]:offsets[name][0]+offsets[name][1]]
        a,room,owner=get('actor'),get('room'),get('owner')
        rows.append(dict(index=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),position=[u(a,o) for o in [0x1A,0x1E,0x22]],
            roomtag=u(a,0x3B9,2),registered=u(room,0x53,2),job=u(a,0x2BC),counter=u(owner,0x108A,1)))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],entry_return=u(post[ENTRY_RESULT],0),snapshots=rows,
        calls={hex(a):r['visited_instructions'].get(hex(a),0) for a in [0x4E8DA0,0x494480,0x48AD30,0x483790,0x483870,0x496700]},
        file=str(f.relative_to(ROOT)))),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else dict(species=5,owner=7,entry_state=274,register=False,activate=False,tick=False,exit=False,native_nav=True,HP=900))
