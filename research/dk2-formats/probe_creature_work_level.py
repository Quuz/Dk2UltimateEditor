"""Discovery rawCreature->native loader->whole level-up WorkModifier update.

Prepared real-class Actor snapshot/World/player. Original loader/level/HP/World
effects paths; nullresource/sound lookup and playtoken are externalproviders.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import emulate_possession_speed as sp
import emulate_creature_factory as cf
import emulate_object_factory as f
import native_unicorn_mapreader as cpu
from build_effect_semantics import EXE,pe_read
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
RAW,MID,LOOKUP,IO,FXCB,FXIO,STOP=0x18000000,0x18010000,0x18020000,0x18021000,0x18022000,0x18023000,0x18900000
sha=cpu.sha

def build(c):
    args,_=sp.build(dict(sp.cases()[0],possessed=False,base_speed=1000));mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [sp.DRIVER,f.OBS]}
    raw=bytearray(next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Creatures' for r in s.records if r[0xF4B]==8))
    mem[f.OUT]=bytearray(32)
    f.put(raw,0xCF2,c.get('increment',1024));f.put(raw,0xCEE,c.get('default',4096));f.put(raw,0xF0C,1000,2)
    mem[RAW]=raw+bytes([165])*64;mem[MID]=bytearray([165])*(6457+64);mem[cf.DEFINITION]=bytearray([165])*(2199+64)
    f.put(mem[sp.VARS],0x40,20)
    for i in range(11):f.put(mem[sp.VARS],0xE1+2*i,100+50*(i-1),2)
    f.put(mem[cf.ACTOR],0x403,c.get('level',1),1);f.put(mem[cf.ACTOR],0x3AF,c.get('modifier',4096));f.put(mem[cf.ACTOR],0x3B3,0)
    f.put(mem[cf.ACTOR],0x3E6,0);f.put(mem[cf.ACTOR],0x40,1000 if c.get('level',1)==1 else 1000*(100+50*(c.get('level',1)-1))//100)
    if c.get('dead'):f.put(mem[cf.ACTOR],0x40,0)
    # World layout prefix and separate scalar avoid overlayingSession/GFXobjects.
    mem[f.WORLD+0xA3C7]=bytearray(4)
    mem[LOOKUP]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('33c0c20800'));mem[IO]=bytearray(4)
    f.put(mem[f.SV],0x1C,LOOKUP);mem[0x740C20]=bytearray([165])*64
    # Original World38C forwards the level-up visual request to connected
    # engine90 (sixarguments). Explicit externalprovider declines it.
    mem[FXCB]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',FXIO)+bytes.fromhex('33c0c21800'));mem[FXIO]=bytearray(4);f.put(mem[f.GV],0x90,FXCB)
    mem[0x7FFEE000]=bytearray([165])*0x4000
    calls=[dict(address='0x491dc0',registers=dict(ECX=hex(cf.DEFINITION)),stack_args=[]),dict(address='0x568820',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x568e30',registers=dict(ECX=hex(MID)),stack_args=[RAW]),dict(address='0x492320',registers=dict(ECX=hex(cf.DEFINITION)),stack_args=[MID,f.WORLD,0])]
    args.update(address='0x48b120',registers=json.dumps(dict(ECX=hex(cf.ACTOR))),stack_args=[],before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=3000000,
        allowed_external_IO_code=[[a,len(mem[a])] for a in [f.GFX_CB,f.SOUND_CB,LOOKUP,FXCB]],
        observe_reads=[[cf.DEFINITION,2199],[cf.ACTOR+0x3AF,8],[0,4096]],observe_writes=[[cf.ACTOR,len(mem[cf.ACTOR])],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [sp.DRIVER,f.GFX_CB,f.SOUND_CB,LOOKUP,FXCB,0x7FFEE000]}
    for a,n in [(0x66D524,0x130),(0x66E3EC,0x3A0)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,checks

def execute(c):
    args,ch=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend+json.dumps({hex(a):sha(b) for a,b in ch.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase162-creature-work-level-probe-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(ch.items()),STOP,cf.ACTOR,1343));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,ch,saved,path

if __name__=='__main__':
    a,ch,s,p=execute(dict(name='base'));r=s['result'];print(p.name);print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','trace_tail','before_calls']}))
    if r['success']:
        post=post_context(ch,r);actor=post[cf.ACTOR];print('modifier',int.from_bytes(actor[0x3AF:0x3B3],'little'),'level',actor[0x403],'HP',int.from_bytes(actor[0x40:0x44],'little'))
