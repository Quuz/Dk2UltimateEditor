"""Phase179 discovery: actual Crypt Dancer create/attach/unlink/delete.

Original source Objects142/143/144 loading, whole room creator and Object
factories. Prepared room/Creature context from frozen178; no live scheduler.
"""
from pathlib import Path
from functools import cache
import json,struct,sys
import probe_crypt_occupant_cleanup as old
import dk2_formats as fmt
from emulate_element_colors import region
from emulate_resistance import post_context
from object_state_tables import BASE as STATE_BASE,SIZE as STATE_SIZE
import fixture_read_probe as readable
ROOT=Path(__file__).resolve().parent
EXE=old.EXE
p=old.p
sha=old.sha
DEFS,RAWS,MID=0x20900000,0x20910000,0x20920000
ARENAS=0x0F000000
FREE_HEAD,FREE_TAIL=0x20D00000,0x0E001000
SOUND_END,SOUND_LOG=0x20B00000,0x20B01000
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'
IDS=[142,143,144]
ROOMDEF,TERRAIN_TABLE=0x20930000,0x20C00000

@cache
def room_context():
    # Reuse a complete original raw->MID->Room/Terrain loader replay.
    # Object attachment reads RoomDefinition+44E and then calls the original
    # World Terrain getter. A null room definition is not an allowed fixture.
    import probe_room_resource_loader as source
    args,checks,saved,file=source.execute(dict(room_id=26,service=False,opaque='source'))
    assert saved['result']['success'] and not saved['result']['emulation_error']
    post=post_context(checks,saved['result'])
    return post[source.prior.prior.ROOMDEF],post[source.prior.TABLE],file

def build(c):
    args,_,actors,owner=old.build(dict(occupants=c.get('occupants',0),replace=c.get('replace',False),old_registered=c.get('old_registered',True)))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raws={r[0x35A]:r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==240 for r in s.records}
    mem[DEFS]=bytearray(len(IDS)*285)+bytearray([165])*64
    mem[RAWS]=bytearray(b''.join(raws[i] for i in IDS))+bytearray([165])*64
    mem[MID]=bytearray([165])*(1022+64);mem[STATE_BASE]=bytearray(STATE_SIZE)
    mem[0x6ED484]=bytearray(struct.pack('<I',p.f.WORLD))
    roomdef,terrain,_=room_context()
    mem[ROOMDEF]=bytearray(roomdef);mem[TERRAIN_TABLE]=bytearray(terrain)
    p.f.put(mem[old.ROOM],0x19,ROOMDEF)
    for off,value in [(0x15A4,TERRAIN_TABLE),(0x15A8,256),(0x15AC,256)]:p.f.put(mem[p.f.WORLD],off,value)
    # Explicit external sound notification used by full Object termination.
    # It has two stack arguments (original CALL[Soundvtable+38]); its return
    # is not consumed by this native path. No game/RTTI method is replaced.
    code=bytes.fromhex('ff05')+struct.pack('<I',SOUND_LOG)+bytes.fromhex('33c0c20800')
    mem[SOUND_END]=bytearray(code);mem[SOUND_LOG]=bytearray(4)
    mem[p.f.SV].extend(bytes(max(0,0x3C-len(mem[p.f.SV]))))
    p.f.put(mem[p.f.SV],0x38,SOUND_END)
    assert int.from_bytes(mem[p.f.SV][0x38:0x3C],'little')==SOUND_END
    # The original free routine walks backward from pool+24 (World+6A).
    # Supply both free sentinels, including the tail missing in the inherited
    # allocation-only context. Otherwise freeing reads address4 and does not
    # reinsert the released block, despite decrementing the used-byte count.
    for off,value in [(0x6A,FREE_TAIL),(0x72,FREE_HEAD),(0x15F4,DEFS),(0x15F8,3),(0x15FC,3),(0x15A0,c.get('seed',1))]:p.f.put(mem[p.f.WORLD],off,value)
    p.f.put(mem[old.ROOM],0x55,c.get('initial55',0));p.f.put(mem[old.ROOM],0x4B,6,2);p.f.put(mem[old.ROOM],0x4D,6,2)
    # Native free insertion requires descending addresses from free head to
    # tail. Keep the inherited BLOCK first for the original Shot allocation;
    # Object arenas are lower, with high/low external sentinel addresses.
    mem[FREE_HEAD]=bytearray(struct.pack('<III',0,0,p.f.BLOCK))
    mem[FREE_TAIL]=bytearray(struct.pack('<III',0,0,0))
    p.f.put(mem[p.f.BLOCK],4,FREE_HEAD)
    p.f.put(mem[p.f.BLOCK],8,FREE_TAIL)
    previous=p.f.BLOCK
    for i in reversed(range(c.get('spare_blocks',16))):
        address=ARENAS+4096*i;mem[address]=bytearray([165])*768
        struct.pack_into('<III',mem[address],0,512,previous,FREE_TAIL)
        p.f.put(mem[previous],8,address);previous=address
    p.f.put(mem[FREE_TAIL],4,previous)
    calls=args['before_calls'][:-1];assert args['before_calls'][-1]['address']=='0x4d8e60'
    calls.append(dict(address='0x49e600',registers=dict(ECX='0'),stack_args=[]))
    for i,ID in enumerate(IDS):
        calls += [dict(address='0x56d4f0',registers=dict(ECX=hex(MID)),stack_args=[]),
            dict(address='0x56d620',registers=dict(ECX=hex(MID)),stack_args=[RAWS+894*i]),
            dict(address='0x49dd60',registers=dict(ECX=hex(DEFS+285*i)),stack_args=[MID,0])]
    # These extra source Objects are created and attached with original
    # methods. Their native allocation addresses follow the explicit pool;
    # no host writes an Object payload, registry pointer or room-list link.
    extra_out=MID+0x2000;mem[extra_out]=bytearray(4)
    next_block=0
    def extra(ID):
        nonlocal next_block
        assert ID in IDS and next_block<c.get('spare_blocks',16)
        node=ARENAS+4096*(c.get('spare_blocks',16)-1-next_block)+0x74;next_block+=1
        calls.append(dict(address='0x4b7a90',registers=dict(ECX=hex(p.f.WORLD+8)),stack_args=[ID,7,p.f.ARGS,extra_out]))
        calls.append(dict(address='0x4e6220',registers=dict(ECX=hex(old.ROOM)),stack_args=[node]))
    for ID in c.get('extras_before',[]):extra(ID)
    calls.append(dict(address='0x4d9410',registers=dict(ECX=hex(old.ROOM)),stack_args=[c.get('count',1)&0xFFFFFFFF]))
    next_block+=max(0,c.get('count',1))
    for ID in c.get('extras_after',[]):extra(ID)
    if c.get('cleanup'):calls.append(args['before_calls'][-1])
    if c.get('recreate'):
        assert c.get('cleanup')
        calls.append(dict(address='0x4d9410',registers=dict(ECX=hex(old.ROOM)),stack_args=[c['recreate']]))
    args['before_calls']=calls
    mem[readable.COUNTER]=bytearray(8)
    mem[readable.ADDRESS]=bytearray(readable.adapter(readable.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=readable.ADDRESS])))
    args['allowed_external_IO_code']=args.get('allowed_external_IO_code',[])+[[readable.ADDRESS,len(mem[readable.ADDRESS])],[SOUND_END,len(code)]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[DEFS,len(mem[DEFS])],[RAWS,len(mem[RAWS])],[STATE_BASE,STATE_SIZE]]
    args['observe_writes'] += [[a,len(b)] for a,b in mem.items() if ARENAS<=a<ARENAS+64*4096]
    args['observe_writes'] += [[0x6EE540,4096*4],[0x6ED540,4096],[0x6A4818,12],[p.f.WORLD,0x80]]
    args['max_steps']=3000000
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,readable.ADDRESS,SOUND_END}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D654,0x80),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks,actors,owner

def execute(c):
    args,checks,actors,owner=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase179-crypt-objects-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.base.cpu.run(EXE,args,list(checks.items()),p.f.OBS,old.ROOM,0xC0);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file,post_context(checks,saved['result']),actors,owner

def describe(c):
    args,ch,s,f,post,actors,owner=execute(c);r=s['result'];u=lambda b,o,n=2:int.from_bytes(b[o:o+n],'little');objects=[]
    for tag in range(103,u(post[0x6A4818],4,4)):
        address=u(post[0x6EE540],tag*4,4)
        if not address:continue
        b=next((b[address-a:address-a+393] for a,b in post.items() if a<=address and address+393<=a+len(b)),None)
        if b is not None and u(b,0x12,4)==2:objects.append(dict(tag=tag,address=address,ID=b[0x12C],HP=u(b,0x40,4),room=u(b,0x138),previous=u(b,0x128),next=u(b,0x12A),state=u(b,0x13A,4),marker=u(b,0x133,4)))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],room_head=u(post[old.ROOM],0xE),room55=u(post[old.ROOM],0x55,4),objects=objects,
        callbacks={hex(a):r['visited_instructions'].get(hex(a),0) for a in [0x4D9410,0x4B7A90,0x4E6220,0x4D9570,0x4E62D0,0x4B7B10,0x554BD0]},
        before=r['before_calls'][-4:],tail=r['trace_tail'][-20:])),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
