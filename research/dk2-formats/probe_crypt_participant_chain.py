"""Phase181 discovery: actual bell-side Crypt AI activation and category exit.

Explicit initial room membership/placement, original Bell factory and full
source loaders. Observation copies/clock advancement are declared fixture
controls; no original code/vtable or source files are changed.
"""
from pathlib import Path
import json,struct,sys
import probe_crypt_active_action as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u
ACTOR=old.PARTICIPANT
CONTEXT=ACTOR+0x24E
ROOM=old.prior.old.ROOM
ARGS,OUT=0x20F60000,0x20F61000
CONTROL,LOG=0x21000000,0x22000000
TRIANGLES=0x23000000
SPELL_RAW,SPELL_MID,SPELL_TABLE=0x24000000,0x24010000,0x24020000
STRIDE=0x3000

def build(c):
    species=c.get('species',5);spare=c.get('spare_blocks',42 if species==21 else 24)
    args,_=old.build(dict(species=species,clock=0,spare_blocks=spare))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # The full original Object getter searches backward in an ID-ordered
    # array. Phase180 only requested its valid9/11 entries; this composition
    # also creates142/143, so provide allfive in proper increasing ID order.
    raw_objects={r[0x35A]:r for s in old.prior.fmt.parse(old.prior.SOURCE.read_bytes()) if s.signature==240 for r in s.records}
    mem[old.prior.RAWS]=bytearray(b''.join(raw_objects[i] for i in [9,11,142,143,144]))+bytearray([165])*64
    actor,room=mem[ACTOR],mem[ROOM];owner=u(actor,0x2A)
    p.f.put(actor,0x3A9,c.get('initial_action',0))
    entry=c.get('entry',True)
    p.f.put(actor,0x25E,1 if entry else 275)
    p.f.put(actor,0x3B9,0 if entry else (300 if c.get('member',True) else 0),2)
    p.f.put(actor,0x236,0,2);p.f.put(actor,0x238,0,2)
    p.f.put(actor,0x1A,c.get('x',0x7800));p.f.put(actor,0x1E,c.get('y',0x6800));p.f.put(actor,0x22,0)
    p.f.put(actor,0xF4,c.get('direction',0x600),2)
    p.f.put(room,0x1D,c.get('room_owner',8),2);room[0x18]=c.get('room_type',26)
    p.f.put(room,0xA,0 if entry else (101 if c.get('member',True) else 0),2)
    p.f.put(room,0x67,100,2);p.f.put(room,0x6B,0 if entry else 1,2);p.f.put(room,0x6D,0 if entry else 1,2)
    p.f.put(room,0x49,103 if c.get('bell',True) else 0,2)
    p.f.put(room,0x53,0,2);p.f.put(room,0x55,0)
    mem[ARGS]=bytearray(struct.pack('<3I',0x6800,0x6800,0));mem[OUT]=bytearray(4)
    x,y=u(actor,0x1A)>>12,u(actor,0x1E)>>12
    cell=(y*32+x)*16
    p.f.put(mem[p.f.MAP],cell+2,300,2);p.f.put(mem[p.f.MAP],cell+8,8,2)
    # No unrelated Shot firing/collision in this participant composition;
    # retain raw loaders but not the old combat instance step.
    calls=[x for x in args['before_calls'] if x['address'] not in ['0x4984d0','0x4abd60']]
    if c.get('full_tick_after_exit'):
        # Full AI reads the source Warlock's spell catalogue, unlike the
        # earlier stand-alone Crypt callback. Supply real1/2/14 plus41,
        # not a null Worldlookup followed by a low-page flags read at27.
        spells={r[0xE6]:r for s in old.prior.fmt.parse((old.SOURCE.parent/'CreatureSpells.kwd').read_bytes()) if s.signature==160 for r in s.records}
        ids=[1,2,14,41]
        mem[SPELL_RAW]=bytearray(b''.join(spells[i] for i in ids))+bytearray([165])*64
        mem[SPELL_MID]=bytearray([165])*(346+64);mem[SPELL_TABLE]=bytearray(70*len(ids))+bytearray([165])*64
        for off,v in [(0x1614,SPELL_TABLE),(0x1618,len(ids)),(0x161C,len(ids))]:p.f.put(mem[p.f.WORLD],off,v)
        for i in range(len(ids)):
            calls += [dict(address='0x56a490',registers=dict(ECX=hex(SPELL_MID)),stack_args=[]),
                dict(address='0x56a560',registers=dict(ECX=hex(SPELL_MID)),stack_args=[SPELL_RAW+266*i]),
                dict(address='0x4f7860',registers=dict(ECX=hex(SPELL_TABLE+70*i)),stack_args=[SPELL_MID,0])]
    if c.get('native_nav',c.get('full_tick_after_exit',False)):
        # Reuse the accepted original32x32 NAV bootstrap, not a fabricated
        # triangle pointer. A whole Creaturetick actually queries this mesh.
        navbase,navsize=0x6E5AC0,0x7520
        imagebase,image=old.prior.old.base.cpu.pe_image(EXE.read_bytes())
        data=bytearray(image[navbase-imagebase:navbase-imagebase+navsize])
        for address in list(mem):
            if navbase<=address<navbase+navsize:
                b=mem.pop(address);data[address-navbase:address-navbase+len(b)]=b
        mem[navbase]=data
        mem[0x6ED408]=bytearray(20)
        n=4+29*2048;mem[TRIANGLES]=bytearray(n)+bytearray([165])*64
        calls += [dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[TRIANGLES,n]),
            dict(address='0x4ce740',registers=dict(ECX='0'),stack_args=[p.NAVBITS,32,32,32]),
            dict(address='0x4c8f30',registers=dict(ECX='0'),stack_args=[]),
            dict(address='0x4cef20',registers=dict(ECX='0'),stack_args=[]),
            dict(address='0x4d27d0',registers=dict(ECX='0'),stack_args=[0x4D16B0,p.f.WORLD])]
    calls += [dict(address='0x4b7a90',registers=dict(ECX=hex(p.f.WORLD+8)),stack_args=[142,8,ARGS,OUT])]
    controls=[]
    # Read the ACTUAL factory output, then call original room attachment.
    # A previous exploratory allocation-address prediction was wrong when
    # the unrelated Shot was absent; those low-page traces are excluded.
    bind=CONTROL+0x2000
    code=bytearray(b'\xa1'+struct.pack('<I',OUT)+bytes.fromhex('500fb75004'))
    if not c.get('bell',True):code+=bytes.fromhex('31d2')
    code+=bytes.fromhex('668915')+struct.pack('<I',ROOM+0x49)
    code+=b'\xb9'+struct.pack('<I',ROOM)+b'\xb8'+struct.pack('<I',0x4E6220)+bytes.fromhex('ffd0c3')
    mem[bind]=code;controls.append([bind,len(code)])
    calls.append(dict(address=hex(bind),registers=dict(ECX='0'),stack_args=[]))
    if entry:calls.append(dict(address='0x476d30',registers=dict(ECX=hex(CONTEXT)),stack_args=[c.get('entry_state',275),0]))
    ranges=[('actor',ACTOR,len(actor)),('room',ROOM,len(room)),('owner',owner,len(mem[owner])),
        ('recipient',old.RECIPIENT,len(mem[old.RECIPIENT]))]
    offsets={};cursor=4
    for name,a,n in ranges:offsets[name]=[cursor,n,a];cursor+=n
    assert cursor<STRIDE
    mem[LOG]=bytearray([165])*(STRIDE*6)
    def snapshot(index):
        address=CONTROL+256*index
        code=bytearray(b'\xc7\x05'+struct.pack('<II',LOG+index*STRIDE,index)+bytes.fromhex('9c60fc'))
        for name,a,n in ranges:
            dst=LOG+index*STRIDE+offsets[name][0]
            code+=b'\xbe'+struct.pack('<I',a)+b'\xbf'+struct.pack('<I',dst)+b'\xb9'+struct.pack('<I',n)+bytes.fromhex('f3a4')
        code+=bytes.fromhex('619dc3');mem[address]=code;controls.append([address,len(code)])
        calls.append(dict(address=hex(address),registers=dict(ECX='0'),stack_args=[]))
    def advance_clock(index,value):
        address=CONTROL+0x1000+256*index;session=u(mem[p.f.WORLD],4)
        code=b'\xc7\x05'+struct.pack('<II',session+4,value)+b'\xc3'
        mem[address]=bytearray(code);controls.append([address,len(code)])
        calls.append(dict(address=hex(address),registers=dict(ECX='0'),stack_args=[]))
    snapshot(0)
    if c.get('registration')=='ai':calls.append(dict(address='0x483790',registers=dict(ECX=hex(CONTEXT)),stack_args=[]))
    elif c.get('register',True):calls.append(dict(address='0x4d8e60',registers=dict(ECX=hex(ROOM)),stack_args=[101]))
    snapshot(1)
    if c.get('activate',True):calls.append(dict(address='0x483870',registers=dict(ECX=hex(CONTEXT)),stack_args=[]))
    snapshot(2)
    advance_clock(0,c.get('clock',100))
    if c.get('tick',True):calls.append(dict(address='0x496aa0',registers=dict(ECX=hex(ACTOR)),stack_args=[]))
    snapshot(3)
    if c.get('exit',True):calls.append(dict(address='0x476d30',registers=dict(ECX=hex(CONTEXT)),stack_args=[1,0]))
    snapshot(4)
    if c.get('stop_action_after_exit'):calls.append(dict(address='0x4966a0',registers=dict(ECX=hex(ACTOR+0x38D)),stack_args=[]))
    if c.get('ai_after_exit'):calls.append(dict(address='0x476fc0',registers=dict(ECX=hex(CONTEXT)),stack_args=[]))
    if c.get('tick_after_exit') or c.get('full_tick_after_exit'):
        for i in range(c.get('after_exit_ticks',1)):
            assert i<8
            advance_clock(1+i,200+100*i)
            calls.append(dict(address='0x488930' if c.get('full_tick_after_exit') else '0x496aa0',registers=dict(ECX=hex(ACTOR)),stack_args=[]))
    snapshot(5)
    args['before_calls']=calls
    args['address']='0x4e52f0';args['registers']=json.dumps(dict(ECX=hex(ROOM)))
    # Recompute declared imported readability ranges for the added buffers.
    adapter=old.prior.readable
    mem[adapter.ADDRESS]=bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=adapter.ADDRESS])))
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=adapter.ADDRESS]+controls+[[adapter.ADDRESS,len(mem[adapter.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[ROOM,len(room)],[ACTOR,len(actor)],[LOG,len(mem[LOG])]]
    args['observe_writes'] += [[ROOM,len(room)],[LOG,len(mem[LOG])]]
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,adapter.ADDRESS,old.prior.SOUND_END,old.UICB}|{a for a,n in controls}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D654,0x80),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks,offsets

def execute(c):
    args,checks,offsets=build(c)
    deps=sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    for attempt in range(3):
        suffix='' if attempt==0 else f'-attempt{attempt+1}'
        file=ROOT/'ghidra'/f'phase181-crypt-chain-discovery-{tag}{suffix}.json'
        if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
        else:
            r=old.prior.old.base.cpu.run(EXE,args,list(checks.items()),p.f.OBS,ACTOR,len(checks[ACTOR]))
            saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
        r=saved['result']
        if r['success'] or r['emulation_error'] or r['protected_write_or_execution_failures']:break
    return args,checks,saved,file,offsets

def describe(c):
    args,ch,s,f,offsets=execute(c);r=s['result'];post=post_context(ch,r);snapshots=[]
    for i in range(6):
        b=post[LOG][i*STRIDE:(i+1)*STRIDE]
        at=lambda name:b[offsets[name][0]:offsets[name][0]+offsets[name][1]]
        a,room,owner=at('actor'),at('room'),at('owner')
        snapshots.append(dict(index=i,checkpoint=u(b,0),state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),recipientHP=u(at('recipient'),0x40),
            roomtag=u(a,0x3B9,2),registered=u(room,0x53,2),roomhead=u(room,0xA,2),objecthead=u(room,0xE,2),room55=u(room,0x55),counters=list(owner[0x108A:0x109C])))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],snapshots=snapshots,
        before=r['before_calls'][-15:],tail=r['trace_tail'][-25:],file=str(f.relative_to(ROOT)))),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
