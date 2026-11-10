"""Phase173 discovery: Guided28 timing uses original Session clock, not PRNG.

Original rawShot35/CreatureSpell wrapper/collision from frozen phase171.
Use exact initialized overlapping PE trig bytes, replacing the older explicitly
bounded cardinal fixture. Only prepared Session+4 varies; no function changes.
"""
from pathlib import Path
import json,struct,sys
import probe_shot_target_combinations as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=old.EXE
sha=old.sha
p=old.p
TRIG=0x6C1488
TRIG_SIZE=0x2800
VELOCITY=0x1F000000

def build(c):
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd'
    i=next(i for s in old.fmt.parse(source.read_bytes()) if s.signature==260 for i,r in enumerate(s.records) if r[0xBE]==35)
    args,_=old.build(dict(process=28,mode=7,source_record=dict(file=str(source),record_index=i),invulnerable=c.get('invulnerable',0)))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    mem[TRIG]=bytearray(p.pe_read(EXE.read_bytes(),TRIG,TRIG_SIZE))
    for name,off in [('mask',0xA0),('data1',0x98),('data2',0x9C)]:
        if name in c:
            p.f.put(mem[p.shot.RAW],off,c[name])
            if off in [0x98,0x9C]:p.f.put(mem[p.RAWSPELL],off+0x30,c[name])
    if 'mode' in c:mem[p.shot.RAW][0xC5]=c['mode']
    session=int.from_bytes(mem[p.f.WORLD][4:8],'little')
    container=max(a for a,b in mem.items() if a<=session<session+8<=a+len(b))
    p.f.put(mem[container],session-container+4,c.get('now',104))
    if c.get('initial_velocity') is not None:
        mem[VELOCITY]=bytearray(struct.pack('<3i',*c['initial_velocity']))
        assert args['before_calls'][-1]['address']=='0x4abd60'
        args['before_calls'].insert(-1,dict(address='0x4aba10',registers=dict(ECX=hex(p.shot.NODE)),stack_args=[VELOCITY]))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[session,12],[TRIG,TRIG_SIZE]]
    args['observe_writes'] += [[p.shot.NODE,371]]
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,0x28CFA4}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c):
    args,checks=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase173-guided-clock-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.shot.NODE,371);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post

def describe(c):
    args,checks,saved,file,post=execute(c);r=saved['result'];b=post[p.f.BLOCK][p.shot.NODE-p.f.BLOCK:p.shot.NODE-p.f.BLOCK+371]
    writes=[x for x in r['observed_writes'] if 0x4AF3A0<=int(x['pc'],16)<0x4AF610]
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],HP=int.from_bytes(post[p.target.attack.TARGET][0x40:0x44],'little'),
        velocity=list(struct.unpack_from('<3i',b,0x104)),angle=int.from_bytes(b[0xF4:0xF6],'little'),writes=writes,
        reads=[x for x in r['observed_reads'] if 0x4AF3A0<=int(x['pc'],16)<0x4AF610],tail=r['trace_tail'][-8:])),flush=True)

if __name__=='__main__':describe(dict(now=int(sys.argv[1]) if len(sys.argv)>1 else 104))
