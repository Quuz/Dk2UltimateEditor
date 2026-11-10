"""Discovery of raw Creature work-rate fields and complete contribution paths."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import probe_poison as previous
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
f,cf,target=previous.f,previous.cf,previous.target
STATE,ROOM,MEMBER=0x10B00000,0x10B10000,0x10B20000
OWNER=previous.shot.OTHEROWNER


def build(mode='manufacture',base=100,level=1,coefficient=125,wizard=0,tick=101,initial=10,tps=20):
    args,_=previous.build(ticks=1)
    m={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    for a in [previous.COLLISION,previous.PROCESS,previous.SPELL,previous.RAWSPELL,previous.MIDSPELL,
              previous.shot.RAW,previous.shot.MID,previous.shot.DEFINITION,previous.shot.ANGLE,previous.shot.OUTPUT]:m.pop(a,None)
    # Original raw target definition, with independent work-rate bytes.
    f.put(m[target.RAW],0xF08,base,2);f.put(m[target.RAW],0xF0A,base,2)
    f.put(m[target.attack.TARGET],0x403,level,1)
    f.put(m[f.SESSION],4,tick)
    f.put(m[target.sp.VARS],0x40,tps)
    if len(m[0x6E5050])>=8:f.put(m[0x6E5050],4,f.WORLD)
    else:m[0x6E5054]=bytearray(struct.pack('<I',f.WORLD))
    f.put(m[OWNER],0x217,initial,2);f.put(m[OWNER],0x20F,initial)
    f.put(m[OWNER],0x213,0)
    # No queued crate completes in this finite contribution fixture.
    m[OWNER][0x219+0x803]=0
    m[STATE]=bytearray(0xE0);f.put(m[STATE],8,target.attack.TARGET);f.put(m[STATE],0xC,f.WORLD)
    stat=18 if mode=='manufacture' else 17
    m[target.attack.RAW65]=bytearray(bytes(16)+struct.pack('<4I',65,stat,coefficient,level))
    calls=args['before_calls'][:4]+[dict(address='0x51dc40',registers=dict(ECX='0'),stack_args=[target.attack.RAW65])]
    if wizard:
        m[ROOM]=bytearray(0x100);m[MEMBER]=bytearray(0x3C0)
        for o,v,n in [(4,200,2),(0x18,26,1),(0x1D,8,2),(0x53,0,2),(0x55,1234,4)]:f.put(m[ROOM],o,v,n)
        f.put(m[MEMBER],4,110,2);f.put(m[MEMBER],0x36F,16,1)
        for tag,a in [(200,ROOM),(110,MEMBER)]:f.put(m[0x6EE540],4*tag,a);m[0x6ED540][tag]=1
        f.put(m[OWNER],0x1094,wizard-1,1)
        calls.append(dict(address='0x4d8e60',registers=dict(ECX=hex(ROOM)),stack_args=[110]))
    calls.append(dict(address='0x488930',registers=dict(ECX=hex(target.attack.TARGET)),stack_args=[]))
    args.update(address=hex(0x47B190 if mode=='manufacture' else 0x47B000),registers=json.dumps(dict(ECX=hex(STATE))),
                before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in m.items()])))
    args['observe_reads']=[[target.attack.TARGET_DEF,2199],[target.attack.TARGET+0x3B3,4],[0,4096]]
    args['observe_writes'] += [[OWNER+0x20D,16],[target.attack.TARGET+0x3B3,4]]
    checks={a:bytes(b) for a,b in m.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,target.attack.EVENT,
            target.LOAD_GFX,target.LOAD_SOUND,0x28CFA4]}
    return args,checks


def main():
    mode=sys.argv[1] if len(sys.argv)>1 else 'manufacture';args,checks=build(mode)
    r=target.cpu.run(EXE,args,list(checks.items()),f.OBS,OWNER,0x1200)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
    (ROOT/'ghidra'/f'phase116-work-discovery-{mode}-{tag}.json').write_text(json.dumps(dict(input=args,result=r),indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in r.items() if k not in ['visited_instructions','captured_hex','context_mismatches','observed_reads','observed_writes','stack_at_stop_hex']}))
    if r['success']:
        b=bytes.fromhex(r['captured_hex']);print('research',int.from_bytes(b[0x20F:0x213],'little'),'manufacture',int.from_bytes(b[0x217:0x219],'little'))


if __name__=='__main__':main()
