"""Discovery: actualrawCreature -> nativeconstructor/free-slotfactory/fullinitializer.

Watch ActorWorkModifier/Enhancement assignment versus rawCEE. PreparedWorld,
owner/map/pool/variables, declaredexternalproviders; no code/vtablepatching.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import emulate_creature_factory as cf
import emulate_object_factory as f
import probe_creature_work_level as old
import native_unicorn_mapreader as cpu
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
RAW,MID,LOOKUP,IO,STOP=0x1E000000,0x1E010000,0x1E020000,0x1E021000,0x1E090000
KRAW,KMID,KDEF=0x1E030000,0x1E040000,0x1E050000
sha=cpu.sha

def build(c):
    base=dict(cf.cases()[0],ident=c.get('species',8));args,_=cf.build(base,0)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [cf.DRIVER,f.OBS]}
    raw=bytearray(next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==base['ident']))
    f.put(raw,0xCEE,c.get('default',0));f.put(raw,0xCF2,c.get('increment',0));f.put(raw,0xF0C,1000,2)
    mem[RAW]=raw+bytes([165])*64;mem[MID]=bytearray([165])*(6457+64);mem[cf.DEFINITION]=bytearray([165])*(2199+64)
    mem[LOOKUP]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('33c0c20800'));mem[IO]=bytearray(4)
    f.put(mem[f.SV],0x1C,LOOKUP);mem[0x740C20]=bytearray([165])*64
    mem[0x7FFEE000]=bytearray([165])*0x4000
    keeper=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/KeeperSpells.kwd').read_bytes()) if s.signature==150 for r in s.records if r[0xEE]==4)
    mem[KRAW]=bytearray(keeper)+bytes([165])*64;mem[KMID]=bytearray([165])*(526+64);mem[KDEF]=bytearray(122)+bytes([165])*64
    f.put(mem[f.WORLD],0x1604,KDEF);f.put(mem[f.WORLD],0x1608,1);f.put(mem[f.WORLD],0x160C,1)
    mem[0x6FCDCF]=bytearray(4)
    # Fullfactory takes the same arguments as originalphase90; constructor
    # returns one realtagged unusedCreature slot toWorld24 beforefactory.
    calls=[dict(address='0x491dc0',registers=dict(ECX=hex(cf.DEFINITION)),stack_args=[]),
        dict(address='0x568820',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x568e30',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x492320',registers=dict(ECX=hex(cf.DEFINITION)),stack_args=[MID,f.WORLD,0]),
        dict(address='0x56b990',registers=dict(ECX=hex(KMID)),stack_args=[]),
        dict(address='0x56bab0',registers=dict(ECX=hex(KMID)),stack_args=[KRAW]),
        dict(address='0x4f7ce0',registers=dict(ECX=hex(KDEF)),stack_args=[KMID,0]),
        dict(address='0x4b5e10',registers=dict(ECX=hex(cf.ACTOR)),stack_args=[])]
    mem[cf.probe.ADDRESS]=bytearray(cf.probe.adapter(cf.probe.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=cf.probe.ADDRESS])))
    args.update(address='0x4b78d0',registers=json.dumps(dict(ECX=hex(f.WORLD+8))),stack_args=[base['ident'],7,f.ARGS,f.OUT+4],
        before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=3000000,
        allowed_external_IO_code=[[a,len(mem[a])] for a in [f.GFX_CB,f.SOUND_CB,LOOKUP,cf.probe.ADDRESS]],
        observe_reads=[[cf.DEFINITION,2199],[cf.ACTOR+0x3AF,8],[0,4096]],observe_writes=[[cf.ACTOR,len(mem[cf.ACTOR])],[0,4096]])
    excluded={f.GFX_CB,f.SOUND_CB,LOOKUP,cf.probe.ADDRESS,0x7FFEE000}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66E3EC,0x3A0)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,checks

def execute(c):
    args,ch=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend+json.dumps({hex(a):sha(b) for a,b in ch.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase164-creature-work-initial-probe-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(ch.items()),STOP,cf.ACTOR,1343));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,ch,saved,path

if __name__=='__main__':
    a,ch,s,p=execute(dict(name='base'));r=s['result'];print(p.name)
    print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','trace_tail','before_calls','protected_write_or_execution_failures']}))
    if r['success']:
        post=post_context(ch,r);actor=post[cf.ACTOR];print('WorkModifier',int.from_bytes(actor[0x3AF:0x3B3],'little'),'WorkEnhancement',int.from_bytes(actor[0x3B3:0x3B7],'little'))
