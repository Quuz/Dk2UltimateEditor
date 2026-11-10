"""Compose prior cast fixtures with real native Shot factory/ctor/init path."""
import json,re,struct
from emulate_element_colors import region,pack32
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_shot_creation import projected_node
from emulate_keeper_cast import build_case as cast_case
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    return [dict(name=f'chain-{route:x}-{upgrade}-{primary}-{bonus}',kind='chain',entry=route,upgraded=upgrade,primary=primary,bonus=bonus) for route in [0x4BBF90,0x4BC290] for upgrade in [0,1] for primary,bonus in [(20,40),(21,40),(0,40),(20,0)]]
def build_case(c):
    from emulate_shot_creation import build_case as factory_case
    old,want=cast_case(dict(**c,c8=0xFE0C0000,rule=1,exists=1,afford=1,valid=1,create=1,flags=0));mem={int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(old['memory'])['regions']}
    fc=dict(ident=c['primary'],seed=165,sound=1,exists=int(bool(c['primary'])),gate=1,allocate=1,name='internal-chain-factory')
    factory,_=factory_case(fc);fm={int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(factory['memory'])['regions']}
    remap={a:(0x10012000 if a==0x10000000 else 0x10002000 if a==0x10001000 else 0x10007000 if a==0x10005000 else a+0x01000000) for a in fm if 0x10000000<=a<0x10020000}
    # Also rewrite pointer values that point inside fixture structures.
    remap.update({0x10000008:0x10012008})
    for a,b in fm.items():
        if 0x10000000<=a<0x10020000:
            translated=remap[a]
            remap.update({a+i:translated+i for i in range(len(b))})
    matcher=re.compile(b'|'.join(re.escape(pack32(a)) for a in sorted(remap)))
    def patch(b):return matcher.sub(lambda m:pack32(remap[int.from_bytes(m[0],'little')]),b)
    excluded={0x10005000,0x10006000,0x10009000,0x1000A000,0x1000B000,0x1000C000,0x10007000,0x7FFF0000}
    mem.pop(0x4B7CD0);mem.pop(0x75B468)
    for a,b in fm.items():
        if a in excluded:continue
        mem[remap.get(a,a)]=patch(b)
    vt=bytearray(mem[0x10002000]);struct.pack_into('<I',vt,0x10C,0x10010000);struct.pack_into('<I',vt,0x110,0x10010100);struct.pack_into('<I',vt,0x1B0,0x10010200);mem[0x10002000]=vt
    primarydef=bytearray(mem[0x11003000]);struct.pack_into('<II',primarydef,0x37,0xAABBCCDD,0xEEFF0011);primarydef[0x6C]=9;mem[0x11003000]=primarydef;bonusdef=bytearray(primarydef);bonusdef[0x6C]=0;mem[0x11016000]=bonusdef
    # The lookup fixture records every selected/initializer lookup, returns distinct definitions.
    body=bytes.fromhex('ff05')+pack32(0x10008014)+bytes.fromhex('0fb6442404a3')+pack32(0x10008018)+bytes.fromhex('3d')+pack32(c['primary'])+bytes.fromhex('7508')+return_const(0x11003000,4)+return_const(0x11016000,4);mem[0x10010200]=body
    selected=c['bonus'] if c['upgraded'] else c['primary'];reached=bool(selected);success=bool(reached and c['primary']);position=[0x1200,0xFFFFDE00,0x3300]
    if c['entry']==0x4BBF90 and c['primary']==21:position[2]+=0x2000
    shot=bytearray(projected_node(165,c['primary'],primarydef,0x11003000,orientation=0,position=position,sound_enabled=1)) if success else bytearray([165])*0x173
    player=bytearray(mem[0x10000000]);state=bytearray(mem[0x10005000]);capture=bytearray(64);fcapture=bytearray(48);world=bytearray(mem[0x10012000])
    if reached:
        struct.pack_into('<II',capture,0x14,2 if success else 1,c['primary'] if success else selected);struct.pack_into('<II',fcapture,0,1,c['primary'])
    if success:
        d1,d2=(0x99AABBCC,0xDDEEFF00) if c['upgraded'] else (0x11223344,0x55667788)
        struct.pack_into('<II',shot,0x13D,d1,d2);struct.pack_into('<H',shot,0x147,9);struct.pack_into('<I',shot,0x14D,1);struct.pack_into('<I',player,0x10F2,1345);struct.pack_into('<II',state,5,0x11223344,0x11223344)
        struct.pack_into('<II',capture,0x1C,1,(-1234)&0xFFFFFFFF);struct.pack_into('<I',capture,0x2C,1);struct.pack_into('<II',fcapture,8,1,0x173);struct.pack_into('<I',fcapture,0x18,0xABCD);struct.pack_into('<I',fcapture,0x1C,3);struct.pack_into('<II',fcapture,0x20,7,0xEF);struct.pack_into('<I',world,0x36,6)
    mem[0x1000B000]=shot;mem[0x1000C000]=player;mem[0x1000E000]=state;mem[0x1000E200]=capture;mem[0x1100B000]=fcapture;mem[0x1100A000]=world
    readout=b'\xA3'+pack32(0x10008038)+b'\xBD'+pack32(1)
    for a,b,n in [(0x10007000,0x1000B000,371),(0x10000000,0x1000C000,4352),(0x10005000,0x1000E000,17),(0x10003000,0x1000E100,122),(0x10008000,0x1000E200,64),(0x11008000,0x1100B000,48),(0x10012000,0x1100A000,80)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
    readout+=END;mem[0x1000F000]=readout
    regions=[region(a,b) for a,b in sorted(mem.items())];spans=[(int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions];assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c
    old['memory']=json.dumps(dict(regions=regions));old['max_steps']=20000;return old,want
