"""Discovery: raw Creature/Shot loaders, original collision and poison processing.

Unpublished until full output and gameplay boundaries have been checked.
Prepared scene uses genuine native class snapshots, no gameplay replacements.
"""
import hashlib
import json
import sys
from pathlib import Path
import probe_shot_hit as shot
import probe_creature_tick as tick
import dk2_formats as fmt
from native_spell_alternatives import full_intermediate,runtime_projection
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
f,cf,target=shot.f,shot.cf,shot.target
COLLISION=0x6E4FF0
PROCESS=0x69E860
SPELL=0x10A70000
RAWSPELL,MIDSPELL=0x10A80000,0x10A90000
ALERT_RECIPIENTS=0x10AA0000
NAVGRID,NAVBITS=0x10AB0000,0x10AC0000


def build(ticks=0,duration=50,severity=5,value=0,freeze=0,invulnerable=0,armour=0,reapply=False,defaults=None):
    c=dict(shot.cases()[0],shot=62,res_type=41,damage_type=0,damage=(defaults or [duration,severity])[0],
           radius=(defaults or [duration,severity])[1],value=value)
    args,checks=shot.build(c)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # External absent-audio provider returns no live handle, rather than the
    # phase114 diagnostic sentinel. Original sound/game code stays untouched.
    memory[f.SOUND_CB][7:11]=bytes(4)
    base,image=target.cpu.pe_image(EXE.read_bytes())
    memory[COLLISION]=bytearray(image[COLLISION-base:COLLISION-base+85])
    memory[PROCESS]=bytearray(pe_read(EXE.read_bytes(),PROCESS,39*17))
    raw=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/CreatureSpells.kwd').read_bytes())
             if s.signature==0xA0 for r in s.records if r[0xE6]==41)
    raw=bytearray(raw);f.put(raw,0xC8,duration);f.put(raw,0xCC,severity)
    memory[RAWSPELL]=raw;memory[MIDSPELL]=bytearray([0xA5])*346
    # Resource pointers begin absent; original complete loader receives null GFX.
    memory[SPELL]=bytearray(70)
    memory[0x6E4168]=bytearray(b'\x01');memory[0x6E4178]=bytearray(19)
    f.put(memory[f.WORLD],0x1614,SPELL);f.put(memory[f.WORLD],0x161C,1)
    f.put(memory[cf.DEFINITION],0x789,1,1)
    memory[cf.DEFINITION][0x5E1:0x5F4]=bytes(19)
    memory[cf.DEFINITION][0x5E1]=41
    f.put(memory[cf.ACTOR],0x399,0);f.put(memory[cf.ACTOR],0x3A5,0)
    memory[cf.ACTOR][0x1A:0x26]=memory[target.attack.TARGET][0x1A:0x26]
    memory[cf.ACTOR][0x38D:0x399]=memory[target.attack.TARGET][0x1A:0x26]
    # Two actual class instances share a cell; caster identity is set by the
    # original CreatureSpell wrapper, avoiding a synthetic target-tag setter.
    for o in range(0,len(memory[f.MAP]),16):f.put(memory[f.MAP],o,0,2)
    x=int.from_bytes(memory[cf.ACTOR][0x1A:0x1E],'little')>>12
    y=int.from_bytes(memory[cf.ACTOR][0x1E:0x22],'little')>>12
    f.put(memory[f.MAP],(32*y+x)*16,101,2)
    f.put(memory[target.attack.TARGET],0xC,100,2);f.put(memory[cf.ACTOR],0xA,101,2)
    f.put(memory[f.PLAYER],4,7,2);f.put(memory[shot.OTHEROWNER],4,8,2)
    f.put(memory[shot.OTHEROWNER],6,101,2)
    memory[ALERT_RECIPIENTS]=bytearray(2)
    for a in [f.PLAYER,shot.OTHEROWNER]:f.put(memory[a],0x10FD,ALERT_RECIPIENTS)
    memory[NAVGRID]=bytearray(24);memory[NAVBITS]=bytearray(32*32)
    f.put(memory[NAVGRID],8,32);f.put(memory[NAVGRID],16,NAVBITS)
    f.put(memory[f.WORLD],0x9F2,NAVGRID)
    for o,v in [(0x447,freeze),(0x445,invulnerable),(0x443,armour)]:f.put(memory[target.attack.TARGET],o,v,2)
    memory[0x6C1488]=bytearray(2560*4)
    f.put(memory[0x6C1488],512*4,65536)
    f.put(memory[0x6C1488],1536*4,0xFFFF0000)
    args['before_calls']=args['before_calls'][:8]+[
        dict(address='0x56a490',registers=dict(ECX=hex(MIDSPELL)),stack_args=[]),
        dict(address='0x56a560',registers=dict(ECX=hex(MIDSPELL)),stack_args=[RAWSPELL]),
        dict(address='0x4f7860',registers=dict(ECX=hex(SPELL)),stack_args=[MIDSPELL,0]),
        dict(address='0x4984d0',registers=dict(ECX=hex(cf.ACTOR)),stack_args=[]),
        dict(address='0x4abca0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4ad1b0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4abd60',registers=dict(ECX=hex(shot.NODE)),stack_args=[])]
    if ticks:
        ta,_=tick.build(True,True,True)
        tm={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(ta['memory'])['regions']}
        for a,b in tm.items():
            if a not in memory and a<0x11000000:memory[a]=b
        for off in [0x64,0x80]:f.put(memory[f.GV],off,int.from_bytes(pe_read(EXE.read_bytes(),0x670574+off,4),'little'))
        args['before_calls'] += [dict(address='0x488930',registers=dict(ECX=hex(target.attack.TARGET)),stack_args=[]) for _ in range(ticks)]
        if reapply:
            args['before_calls'] += [dict(address='0x4ad730',registers=dict(ECX=hex(shot.NODE)),stack_args=[4]),
                                    dict(address='0x488930',registers=dict(ECX=hex(target.attack.TARGET)),stack_args=[])]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()]))
    args['observe_reads'] += [[target.attack.TARGET+0x45D,6],[f.MAP,len(memory[f.MAP])],[f.WORLD+0x9DA,0x20],
                             [MIDSPELL,346],[SPELL,70]]
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,
        target.attack.EVENT,target.LOAD_GFX,target.LOAD_SOUND,0x28CFA4]}
    checks[shot.SHOT_VTABLE]=pe_read(EXE.read_bytes(),shot.SHOT_VTABLE,0x100)
    return args,checks


def main():
    ticks=int(sys.argv[1]) if len(sys.argv)>1 else 0
    args,checks=build(ticks)
    r=target.cpu.run(EXE,args,list(checks.items()),f.OBS,target.attack.TARGET,1599)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
    p=ROOT/'ghidra'/f'phase115-poison-discovery-{ticks}-{tag}.json'
    p.write_text(json.dumps(dict(input=args,result=r),indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in r.items() if k not in ['visited_instructions','captured_hex',
        'context_mismatches','observed_reads','observed_writes','stack_at_stop_hex']}))
    if r['success']:
        b=bytes.fromhex(r['captured_hex']);print('HP',int.from_bytes(b[0x40:0x44],'little'),'poison',b[0x45D:0x463].hex())
    print('Changed contexts',[(x['address'],len(x['differences'])) for x in r['context_mismatches']])


if __name__=='__main__':main()
