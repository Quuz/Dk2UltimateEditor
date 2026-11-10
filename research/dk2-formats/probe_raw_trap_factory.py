"""Whole original rawTrap load and CTrap creation in one isolated CPU.

Actual supplied raw record, constructor/initializer/converter/loader/factory.
Absent visual resources and external null sound are explicit; original game
instructions and vtables stay intact. No live World/GUI or source-file write.
"""
from pathlib import Path
import json
import hashlib
import struct
import sys
import dk2_formats as fmt
import probe_trap_factory as base
import native_trap_loading as loading
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
RAW,MID,LOAD_SOUND,IO=0x10B00000,0x10B10000,0x10B20000,0x10B30000
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Traps.kwd'
f=base.f


def ctor_definition(seed=165):
    value=bytearray([seed])*209
    for start,end in [(0,0x6C),(0x70,0x9E),(0xA0,0xD1)]:value[start:end]=bytes(end-start)
    return bytes(value)


def build(unknown=0,ident=1,gate=0,*,health=None,upkeep=None,flags=None,activation=False,tick=False,cell_owner=7):
    args,_,node=base.build()
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=bytearray(next(r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0x82
        for r in s.records if r[0x20C]==ident))
    f.put(raw,0x204,unknown,2)
    if health is not None:f.put(raw,0x1F0,health,2)
    if upkeep is not None:f.put(raw,0x202,upkeep,2)
    if flags is not None:f.put(raw,0x1EC,flags)
    memory[RAW]=raw+bytearray([0xA5])*64
    memory[MID]=bytearray([0xA5])*(747+64)
    memory[f.DEFINITION]=bytearray([0xA5])*(209+64)
    memory[LOAD_SOUND]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('33c0c20800'))
    memory[IO]=bytearray(4)
    memory[0x6F2590]=bytearray(struct.pack('<I',20))
    memory[0x6FCDCF]=bytearray(struct.pack('<I',gate))
    f.put(memory[f.WORLD],0x9EA,32)
    # CTrap::tick requires the cell's low12 owner to match the actual owner.
    px,py=struct.unpack_from('<2I',memory[f.ARGS])
    cell=((py>>12)*32+(px>>12))*16
    f.put(memory[f.MAP],cell+8,cell_owner,2)
    f.put(memory[f.SV],0x1C,LOAD_SOUND)
    # Original null effect request implementation, not a gameplay substitute.
    f.put(memory[f.GV],0x90,int.from_bytes(pe_read(EXE.read_bytes(),0x670574+0x90,4),'little'))
    calls=[dict(address='0x4aa330',registers=dict(ECX=hex(f.DEFINITION)),stack_args=[]),
        dict(address='0x570b60',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x570d10',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x4aa440',registers=dict(ECX=hex(f.DEFINITION)),stack_args=[MID,0]),
        dict(address='0x4b7c10',registers=dict(ECX=hex(f.WORLD+8)),stack_args=[ident,7,f.ARGS,f.OUT+4])]
    if activation:calls.append(dict(address='0x4a96d0',registers=dict(ECX=hex(node)),stack_args=[]))
    if tick:calls.append(dict(address='0x4a84e0',registers=dict(ECX=hex(node)),stack_args=[]))
    args.update(address='0x4b5c30',registers=json.dumps(dict(ECX=hex(node),DF='0')),
        before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),
        observe_reads=[[f.DEFINITION,209],[RAW,len(memory[RAW])],[MID,len(memory[MID])],[0,4096]],
        observe_writes=[[f.BLOCK,768],[f.WORLD,0x8000],[f.PLAYER,0x1200],[0x66D754,96]],
        allowed_external_IO_code=[[a,len(memory[a])] for a in [f.GFX_CB,f.SOUND_CB,LOAD_SOUND]],max_steps=500000)
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB,LOAD_SOUND]}
    checks[0x66D754]=pe_read(EXE.read_bytes(),0x66D754,96)
    mid=loading.intermediate(raw,bytes(747))
    definition=loading.definition(mid,ctor_definition(),20,loading.DX,gate,0)
    expected=dict(raw=raw.hex(),mid=mid.hex(),definition=definition.hex(),node=node,ident=ident,
        gate=gate,unknown=unknown,activation=activation,tick=tick,cell_owner=cell_owner)
    return args,checks,expected


if __name__=='__main__':
    value=int(sys.argv[1],0) if len(sys.argv)>1 else 0
    ident=int(sys.argv[2],0) if len(sys.argv)>2 else 1
    gate=int(sys.argv[3],0) if len(sys.argv)>3 else 0
    args,checks,expected=build(value,ident,gate)
    result=base.cpu.run(EXE,args,list(checks.items()),f.OBS,expected['node'],291)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
    p=ROOT/'ghidra'/f'phase119-raw-trap-discovery-{tag}.json'
    p.write_text(json.dumps(dict(scope=__doc__,input=args,expected=expected,result=result),ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k not in ['captured_hex','visited_instructions','context_mismatches','observed_writes','observed_reads','stack_at_stop_hex']}))
    print('file',p.name)
    print('Changed contexts',[(x['address'],len(x['differences'])) for x in result['context_mismatches']])
