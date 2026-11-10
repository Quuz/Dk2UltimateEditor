"""Raw Rooms/Terrain -> original loaders with an explicit successful asset IO.

The IO adapter logs bounded names/type/flags, writes a deterministic handle,
and returns success. Original504D80 metadata unpacker executes. No actual
asset files/GPU. Whole loader is a beforecall; final Worldgetter onlyreturns.
"""
from pathlib import Path
import json,struct,sys
import emulate_room_adjacency as prior
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
cpu=prior.cpu;EXE=prior.EXE;sha=prior.sha;fmt=prior.fmt;put=prior.put
SERVICE,VT,CB,LOG=0x1D000000,0x1D001000,0x1D002000,0x1D010000
ROW_SIZE,CAPACITY=80,128

def source_records(name,family):
    return next(s.records for s in fmt.parse((prior.prior.SOURCE.parent/name).read_bytes()) if fmt.FAMILIES.get(s.signature)==family)

def opaque_resource(kind=7):
    b=bytearray((i*17+3)&255 for i in range(84));b[:64]=bytes(64);name=b'P150_Unknown#9';b[:len(name)]=name;b[80]=kind
    return bytes(b)

def callback():
    # Sixstackargs: name,type,flags,outHandle*,1,0. Preserve allcallee-saved
    # registers; only bounded NULterminated names are admitted bythefixture.
    code=bytearray(bytes.fromhex('535657a1')+struct.pack('<I',LOG)+bytes.fromhex('ff05')+struct.pack('<I',LOG))
    code+=bytes.fromhex('6bc05005')+struct.pack('<I',LOG+0x100)+bytes.fromhex('89c389c78b742410b940000000fcacaa84c07402e2f8')
    code+=bytes.fromhex('8b4424148943408b442418894344a1')+struct.pack('<I',LOG)
    code+=bytes.fromhex('8943488b54241c6689028b44240c89434cb8010000005f5e5bc21800')
    return code

def build(c):
    args,_=prior.build(prior.cases()[0],baseline=True)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=bytearray(next(r for r in source_records('Rooms.kwd','Rooms') if r[0x392]==c.get('room_id',13)))
    if 'mode' in c:raw[0x395]=c['mode']
    for off in c.get('opaque_offsets',[0x1C4]):
        if c.get('opaque')=='filled':raw[off:off+84]=opaque_resource(c.get('opaque_kind',7))
        elif c.get('opaque')=='zero':raw[off:off+84]=bytes(84)
    if c.get('complete_control'):
        raw[0xC8:0x108]=bytes(64);name=b'P150_Control';raw[0xC8:0xC8+len(name)]=name
    mem[prior.ROOMRAW]=raw+bytes([165])*64
    terrain_id=raw[0x394]
    tr=next(r for r in source_records('Terrain.kwd','Terrain') if r[0x1D6]==terrain_id)
    mem[prior.TERRAINRAW]=bytearray(tr)+bytes([165])*64
    calls=args['before_calls'][-7:]
    assert [int(r['address'],16) for r in calls]==[0x56EB60,0x56ED20,0x4F66E0,0x4EAB70,0x56DE40,0x56DFD0,0x4EACC0]
    calls[2]['registers']['ECX']=hex(prior.TABLE+terrain_id*460)
    calls[-1]['stack_args'][-1]=SERVICE if c.get('service',True) else 0
    mem[SERVICE]=bytearray(struct.pack('<I',VT));mem[VT]=bytearray(0x58);put(mem[VT],0x54,CB)
    mem[CB]=callback();mem[LOG]=bytearray(0x100+CAPACITY*ROW_SIZE)
    put(mem[prior.prior.WORLD],0xA3C7,c.get('update_mode',0))
    if c.get('update_mode'):
        # Update skips checksum initialization/recalculation. Prepare an
        # explicit previous value rather than interpreting the A5 seed.
        put(mem[prior.prior.ROOMDEF],0xA2,0x12345678)
    args['before_calls']=calls
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['allowed_external_IO_code'].append([CB,len(mem[CB])])
    args['observe_reads'] += [[prior.ROOMMID+0x298,84],[LOG,len(mem[LOG])]]
    args['observe_writes'] += [[LOG,len(mem[LOG])]]
    excluded=[prior.prior.SCB,prior.prior.PLAY,prior.LOOKUP,prior.QUEUE,prior.VFX,CB,0x7FFEE000]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    return args,checks

def execute(c,verbose=False):
    args,checks=build(c);tag=sha(json.dumps(args,sort_keys=True).encode())[:12];p=ROOT/'ghidra'/f'phase150-room-resource-loader-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),prior.prior.STOP,prior.prior.ROOMDEF,1222);saved=dict(case=c,input=args,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    if verbose:
        r=saved['result'];post=post_context(checks,r);log=post[LOG];n=int.from_bytes(log[:4],'little')
        print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],calls=r['before_calls'],request_count=n,
            requests=[dict(name=log[0x100+80*i:0x100+80*i+64].split(b'\0',1)[0].decode('cp1252'),kind=log[0x100+80*i+64],handle=int.from_bytes(log[0x100+80*i+72:0x100+80*i+76],'little')) for i in range(min(n,8))],
            tail=r['trace_tail'])),flush=True)
    return args,checks,saved,p

if __name__=='__main__':execute(dict(room_id=13,opaque='filled' if 'filled' in sys.argv else 'source'),True)
