"""Discovery: raw Terrain -> original light factory -> full441CD0 -> engine sink.

Prepared CBridge/World/tile; original vtables and allocator/tag registry. Sound
lookup and engine E0 submission are declared external boundaries. No pixels.
"""
from pathlib import Path
import json,struct,sys
import dk2_formats as fmt
import probe_effect_mode18 as fx
import emulate_terrain_opaque_table as terrain
import native_unicorn_keepertrace as cpu
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
WORLD,ARRAY,RAW,MID,SOUND=0x18000000,0x18100000,0x18200000,0x18300000,0x18400000
SV,SCB,SIO=SOUND+0x1000,SOUND+0x2000,SOUND+0x3000
XYZ,OUT,SINK,RIO=0x18500000,0x18501000,0x18502000,0x18503000
TILE=0x18600000
sha=cpu.sha

def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')

def build(c):
    args,_=fx.build(dict(name='seed',count=0));mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # Only the used CBridge prefix is needed; avoid overlaying its former tail
    # on the separately prepared Session object at10002000.
    mem[fx.base.BRIDGE]=mem[fx.base.BRIDGE][:0x1380]
    if 'floor' in c:mem[fx.base.FLOOR]=bytearray(fx.base.callback(4,c['floor'],4))
    put(mem[fx.base.BRIDGE],0x35B,1,2)
    put(mem[fx.base.EV],0xE0,SINK)
    sink=bytes.fromhex('ff05')+pack32(RIO)+bytes.fromhex('56578d74240cbf')+pack32(RIO+4)+bytes.fromhex('b905000000f3a55f5ec21400')
    mem[SINK]=bytearray(sink);mem[RIO]=bytearray(24)
    world=bytearray(0x1700);put(world,0,0x66E3EC);put(world,0x15A4,ARRAY);put(world,0x15A8,41);put(world,0x15AC,2);mem[WORLD]=world
    mem[ARRAY]=bytearray(41*460)+bytearray([165])*64
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd';records=next(s.records for s in fmt.parse(source.read_bytes()) if s.signature==110)
    calls=[];raws={}
    for i,ident in enumerate([c.get('terrain',3),38]):
        raw=bytearray(next(r for r in records if r[0x1D6]==ident))
        if i==0:
            for key,off,size in [('unknown198',0x198,2),('unknown224',0x224,4),('light_height',0x18C,4),('gold',0x19A,2)]:
                if key in c:put(raw,off,c[key],size)
            if 'rgb' in c:
                raw[0x1DB:0x1DE]=bytes(c['rgb']);flags=int.from_bytes(raw[0x190:0x194],'little')&~0x380000
                for j,on in enumerate(c.get('high',[0,0,0])):
                    if on:flags|=0x80000<<j
                put(raw,0x190,flags)
            if c.get('disabled'):put(raw,0x190,int.from_bytes(raw[0x190:0x194],'little')&~0x10000000)
        else:
            flags=int.from_bytes(raw[0x190:0x194],'little')&~0x3000
            put(raw,0x190,(flags&~0x10000000) if c.get('fallback_disabled') else flags|0x10000000)
            if 'fallback_gold' in c:put(raw,0x19A,c['fallback_gold'],2)
        address=RAW+0x1000*i;mem[address]=raw+bytes([165])*64;raws[ident]=bytes(raw)
        if MID not in mem:mem[MID]=bytearray([165])*800
        dest=ARRAY+460*ident
        mem[ARRAY][460*ident:460*(ident+1)]=bytes([165])*460
        calls += [dict(address='0x4f6570',registers=dict(ECX=hex(dest)),stack_args=[]),dict(address='0x56eb60',registers=dict(ECX=hex(MID)),stack_args=[]),
            dict(address='0x56ed20',registers=dict(ECX=hex(MID)),stack_args=[address]),dict(address='0x4f66e0',registers=dict(ECX=hex(dest)),stack_args=[MID,0])]
    mem[0x6DA4A4]=bytearray(pack32(WORLD));mem[0x6DA4A8]=bytearray([165])*1024
    calls.append(dict(address='0x455d40',registers=dict(ECX='0'),stack_args=[WORLD]))
    mem[0x6FCDCF]=bytearray(4);mem[0x75B468]=bytearray(pack32(SOUND));mem[0x740C20]=bytearray(64)
    sv=bytearray(32);put(sv,0x1C,SCB);mem[SOUND]=bytearray(pack32(SV));mem[SV]=sv
    scb=bytes.fromhex('ff05')+pack32(SIO)+bytes.fromhex('33c0c20800');mem[SCB]=bytearray(scb);mem[SIO]=bytearray(4)
    mem[XYZ]=bytearray(struct.pack('<3i',2048,2048,0x1200));mem[OUT]=bytearray(pack32(0xCAFEBABE))
    tile=bytearray(16);put(tile,2,1,2);tile[9]=c.get('cell9',0x20);tile[10]=c.get('terrain',3);tile[11]=c.get('cellB',0);tile[12]=c.get('cellC',255);mem[TILE]=tile
    if c.get('fallback'):
        put(tile,2,0,2);tile[9]=0;tile[11]=0x40
    mem[0x6F29F0]=bytearray(struct.pack('<3H',111,222,333))
    calls.append(dict(address='0x4397e0',registers=dict(ECX=hex(fx.base.BRIDGE)),stack_args=[XYZ,500,500,500,128,0x1200,0,OUT]))
    args.update(address='0x441cd0',registers=json.dumps(dict(ECX=hex(fx.base.BRIDGE))),stack_args=[TILE,2,3],before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=600000,
        allowed_external_IO_code=args['allowed_external_IO_code']+[[SINK,len(sink)],[SCB,len(scb)]],
        observe_reads=[[ARRAY,len(mem[ARRAY])],[MID,800],[TILE,16],[0,4096]],observe_writes=[[fx.scaffold.POOL,fx.scaffold.POOL_SIZE+64],[ARRAY,len(mem[ARRAY])],[0,4096]])
    excluded={int(a) for a,_ in args['allowed_external_IO_code']}|{0x7FFEF000}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66CC94,0x194),(0x66CC14,0x20),(0x66E3EC,0x3A0)]:checks[a]=fx.pe_read(fx.EXE.read_bytes(),a,n)
    return args,checks,raws

def execute(c):
    args,checks,raws=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];p=ROOT/'ghidra'/f'phase159-terrain-light-probe-{tag}.json'
    if p.exists():d=json.loads(p.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(fx.EXE,args,list(checks.items()),fx.scaffold.element.STOP,OUT,4));p.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,raws,d,p

if __name__=='__main__':
    a,ch,raw,d,p=execute(dict(name='dirt'));r=d['result'];print(p.name);print(json.dumps({k:r[k] for k in ['success','emulation_error','trace_tail','before_calls','registers']}))
    if r['success']:
        post=post_context(ch,r);ptr=int.from_bytes(post[OUT],'little');b=post[fx.scaffold.POOL][ptr-fx.scaffold.POOL:ptr-fx.scaffold.POOL+326]
        print('LIGHT',hex(ptr),'tag',b[0xF2:0xF4].hex(),'RGB',struct.unpack_from('<3H',b,0x111),'XYZ',struct.unpack_from('<3i',b,0xD6),'flags',b[0x134:0x138].hex(),'RIO',post[RIO].hex())
