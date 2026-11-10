"""Discovery: original raw Creature loader followed by complete native melee attack.

Not accepted until target/definition/whole-context expectations are checked.
Full sources remain untouched. Only declared external graphics/sound providers.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import dk2_formats as fmt
import emulate_melee_attack as attack
import native_unicorn_factorytrace as cpu
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
f,cf,sp=attack.f,attack.cf,attack.sp
RAW,MID,IO,LOAD_GFX,LOAD_SOUND=0x10900000,0x10910000,0x10920000,0x10921000,0x10922000


def build(value=0,attack_type=1):
    c=dict(next(c for c in attack.cases() if c['name']=='creature-normal60'))
    args,expected=attack.build(c)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']
            if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [f.OBS,sp.DRIVER,0x7FFEE000]}
    raw=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes())
             if s.signature==0xAA for r in s.records if r[0xF4B]==8)
    raw=bytearray(raw)
    for i in range(4):raw[0xE0C+2*i:0xE0E+2*i]=bytes([attack_type,value])
    memory[RAW]=raw;memory[MID]=bytearray([0xA5])*6457
    memory[IO]=bytearray(8)
    memory[LOAD_GFX]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('8b442410c7000000000033c0c21800'))
    memory[LOAD_SOUND]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO+4)+bytes.fromhex('33c0c20800'))
    f.put(memory[f.GV],0x54,LOAD_GFX);f.put(memory[f.SV],0x1C,LOAD_SOUND)
    # Real CPCEngineInterface slot90 handles the blade-impact effect request.
    f.put(memory[f.GV],0x90,int.from_bytes(pe_read(EXE.read_bytes(),0x670574+0x90,4),'little'))
    memory[f.WORLD].extend(bytes(0xA3D0-len(memory[f.WORLD])))
    f.put(memory[f.WORLD],0xA3C7,1);f.put(memory[sp.VARS],0x40,20)
    # Raw Creature F50 -> mid1340 -> definition77A; explicitly match table type.
    f.put(memory[cf.DEFINITION],0x77A,attack_type,1)
    stack=bytearray([0xA5])*16384;memory[0x7FFEE000]=stack
    calls=[dict(address='0x491dc0',registers=dict(ECX=hex(attack.TARGET_DEF)),stack_args=[]),
           dict(address='0x568820',registers=dict(ECX=hex(MID)),stack_args=[]),
           dict(address='0x568e30',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
           dict(address='0x492320',registers=dict(ECX=hex(attack.TARGET_DEF)),stack_args=[MID,f.WORLD,0]),
           dict(address='0x51dc70',registers=dict(ECX='0'),stack_args=[sp.RAW]),
           dict(address='0x51dc40',registers=dict(ECX='0'),stack_args=[attack.RAW65])]
    args.update(address='0x498e30',registers=json.dumps(dict(ECX=hex(cf.ACTOR))),before_calls=calls,
                max_steps=500000,memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),
                observe_reads=[[attack.TARGET_DEF,2199],[MID+0x11FC,8],[MID+0x284,16],[0,4096]],
                observe_writes=[[attack.TARGET,1599],[attack.TARGET_DEF,2199]],
                allowed_external_IO_code=[[a,len(memory[a])] for a in [f.GFX_CB,f.SOUND_CB,attack.EVENT,LOAD_GFX,LOAD_SOUND]])
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,attack.EVENT,LOAD_GFX,LOAD_SOUND]}
    return args,expected,checks


def main():
    value=int(sys.argv[1]) if len(sys.argv)>1 else 0
    args,e,checks=build(value)
    r=cpu.run(EXE,args,list(checks.items()),f.OBS,attack.TARGET_DEF,2199)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10]
    p=ROOT/'ghidra'/f'phase113-resistance-discovery-{value}-{tag}.json'
    p.write_text(json.dumps(dict(input=args,expected_attack=e,result=r),indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in r.items() if k not in ['visited_instructions','captured_hex','context_mismatches','observed_reads','observed_writes','stack_at_stop_hex']}))
    if r['success']:
        definition=bytes.fromhex(r['captured_hex']);print('TABLE',definition[0x61C:0x624].hex(),'COUNT',definition[0x78A])
    print('Changed contexts',[(x['address'],len(x['differences'])) for x in r['context_mismatches']])


if __name__=='__main__':main()
