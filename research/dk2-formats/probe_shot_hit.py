"""Original rawCreature/rawShot loaders -> Shot factory -> damage consumer.

Prepared scene and external resource IO only. No native code/vtable replacement.
Object targets use a native Object snapshot with explicit scene/definition data.
Full flight/collision scheduler, real resources and renderer are not executed.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import dk2_formats as fmt
import probe_creature_resistance as target
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
f,cf=target.f,target.cf
DEFINITION,ANGLE,OUTPUT=0x10A00000,0x10A10000,0x10A20000
RAW,MID,OTHEROWNER,OBJECTDEF=0x10A30000,0x10A40000,0x10A50000,0x10A60000
NODE=f.BLOCK+128+12
SHOT_VTABLE=0x66D86C
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd'


def cases():
    base=dict(value=0,res_type=18,category=8,kind='creature',owner=8,shot=11,
              damage_type=7,damage=20,radius=1,word445=0,shot_dx=0)
    variations=[('hit-res0',{}),('hit-res100',dict(value=100)),('hit-res255',dict(value=255)),
        ('hit-zero',dict(damage=0)),('hit-negative',dict(damage=0xFFFFFFFF)),
        ('hit-block445',dict(word445=1)),
        ('area-res0',dict(damage_type=1)),('area-res100',dict(damage_type=1,value=100)),
        ('area-res255',dict(damage_type=1,value=255)),
        ('area-friendly',dict(damage_type=1,owner=7)),
        ('range-res0',dict(shot=17,damage_type=2,category=6,res_type=11,radius=2)),
        ('range-res100',dict(shot=17,damage_type=2,category=6,res_type=11,radius=2,value=100)),
        ('range-res255',dict(shot=17,damage_type=2,category=6,res_type=11,radius=2,value=255)),
        ('range-half',dict(shot=17,damage_type=2,category=6,res_type=11,radius=2,shot_dx=4096)),
        ('boulder-fire',dict(kind='boulder',category=4)),
        ('boulder-lightning',dict(kind='boulder',category=6)),
        ('boulder-projectile',dict(kind='boulder',category=8)),
        ('boulder-fire-round',dict(kind='boulder',category=4,damage=23))]
    return [dict(base,**kw,name=name) for name,kw in variations]


def build(c):
    args,_,_=target.build(c['value'],c['res_type'])
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=bytearray(next(r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0x104
                       for r in s.records if r[0xBE]==c['shot']))
    f.put(raw,0x98,c['damage']);f.put(raw,0x9C,c['radius'])
    raw[0xC5]=c['damage_type'];raw[0xC8]=c['category']
    memory[RAW]=raw;memory[MID]=bytearray([0xA5])*375
    memory[DEFINITION]=bytearray([0xA5])*118
    memory[ANGLE]=bytearray(struct.pack('<HH',512,0));memory[OUTPUT]=bytearray(4)
    # Exact original null creation-effect implementation, not a game stub.
    f.put(memory[f.GV],0x98,int.from_bytes(pe_read(EXE.read_bytes(),0x670574+0x98,4),'little'))
    f.put(memory[f.WORLD],0x1624,DEFINITION);f.put(memory[f.WORLD],0x162C,1)
    f.put(memory[f.WORLD],0x9EA,32)
    memory[0x6DA490]=bytearray(struct.pack('<I',32))
    if c['kind']=='boulder':
        oc=next(x for x in target.attack.cases() if x['name']=='object-normal60')
        oa,_=target.attack.build(oc)
        om={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(oa['memory'])['regions']}
        memory[target.attack.TARGET]=om[target.attack.TARGET]+bytearray([0xA5])*(1599-len(om[target.attack.TARGET]))
        memory[OBJECTDEF]=om[target.attack.TARGET_DEF]
        f.put(memory[target.attack.TARGET],0x12D,OBJECTDEF)
        memory[target.attack.TARGET][0x12C]=74
    else:f.put(memory[target.attack.TARGET],0x445,c['word445'],2)
    memory[OTHEROWNER]=bytearray(memory[f.PLAYER])
    f.put(memory[0x6EE540],8*4,OTHEROWNER);memory[0x6ED540][8]=1
    f.put(memory[target.attack.TARGET],0x28,c['owner'],2)
    f.put(memory[target.attack.TARGET],0x2A,OTHEROWNER if c['owner']==8 else f.PLAYER)
    for o in range(0,len(memory[f.MAP]),16):f.put(memory[f.MAP],o,0,2)
    for a,tag in [(cf.ACTOR,100),(target.attack.TARGET,101)]:
        for o in [0xA,0xC]:f.put(memory[a],o,0,2)
        x=int.from_bytes(memory[a][0x1A:0x1E],'little')>>12
        y=int.from_bytes(memory[a][0x1E:0x22],'little')>>12
        f.put(memory[f.MAP],(32*y+x)*16,tag,2)
    memory[f.ARGS][:12]=memory[target.attack.TARGET][0x1A:0x26]
    f.put(memory[f.ARGS],0,int.from_bytes(memory[f.ARGS][:4],'little')-c['shot_dx'])
    calls=args['before_calls'][:4]
    calls += [dict(address='0x4acd60',registers=dict(ECX=hex(DEFINITION)),stack_args=[]),
              dict(address='0x56e540',registers=dict(ECX=hex(MID)),stack_args=[]),
              dict(address='0x56e690',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
              dict(address='0x4acdf0',registers=dict(ECX=hex(DEFINITION)),stack_args=[MID,f.GFX]),
              dict(address='0x4b7cd0',registers=dict(ECX=hex(f.WORLD+8)),stack_args=[c['shot'],7,f.ARGS,ANGLE,OUTPUT,0])]
    if c['damage_type']==7:
        calls.append(dict(address='0x4ac420',registers=dict(ECX=hex(NODE)),stack_args=[target.attack.TARGET,7]))
    else:
        calls.append(dict(address='0x4ac3c0',registers=dict(ECX=hex(NODE)),stack_args=[]))
    args.update(address='0x4b5c30',registers=json.dumps(dict(ECX=hex(target.attack.TARGET))),before_calls=calls,
                memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])))
    args['observe_reads'] += [[NODE,371],[DEFINITION,118],[MID,375]]
    args['observe_writes'] += [[f.BLOCK,len(memory[f.BLOCK])],[f.WORLD,len(memory[f.WORLD])],
                              [f.PLAYER,0x1200],[SHOT_VTABLE,0x100]]
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,
            target.attack.EVENT,target.LOAD_GFX,target.LOAD_SOUND]}
    checks[SHOT_VTABLE]=pe_read(EXE.read_bytes(),SHOT_VTABLE,0x100)
    return args,checks


def main():
    c=next(c for c in cases() if c['name']==(sys.argv[1] if len(sys.argv)>1 else 'hit-res0'))
    args,checks=build(c)
    r=target.cpu.run(EXE,args,list(checks.items()),f.OBS,target.attack.TARGET,1599)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10]
    p=ROOT/'ghidra'/f'phase114-shot-discovery-{c["name"]}-{tag}.json'
    p.write_text(json.dumps(dict(case=c,input=args,result=r),indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in r.items() if k not in ['visited_instructions','captured_hex',
        'context_mismatches','observed_reads','observed_writes','stack_at_stop_hex']}))
    if r['success']:print('TARGET HP',int.from_bytes(bytes.fromhex(r['captured_hex'])[0x40:0x44],'little'))
    print('Changed contexts',[(x['address'],len(x['differences'])) for x in r['context_mismatches']])


if __name__=='__main__':main()
