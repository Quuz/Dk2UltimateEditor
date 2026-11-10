"""Discovery: actual raw Trap creation followed by manager-owned destruction.

Unmodified factory/cleanup/destructor/tag/pool code. Only the declared external
null sound provider gains its two-argument release operation; no gameplay stub.
Explicit manager invocation is not the full World deletion scheduler.
"""
from pathlib import Path
import json
import hashlib
import sys
import probe_raw_trap_factory as creation
from emulate_element_colors import region
from build_effect_semantics import EXE

ROOT=Path(__file__).resolve().parent
RELEASE=0x10B40000
ARENA=0x10B50000
MANAGER=creation.f.WORLD+0x46
f=creation.f


def build(unknown=0,ident=1,*,active=True,bad_cell=False,upkeep=None,gate=0,world_gate=0):
    args,_,expected=creation.build(unknown,ident,gate,activation=active,tick=bad_cell,
        cell_owner=0 if bad_cell else 7,upkeep=upkeep)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory[f.SV].extend(bytes(0x40-len(memory[f.SV])))
    creation.f.put(memory[f.SV],0x38,RELEASE)
    memory[creation.IO].extend(bytes(12))
    memory[RELEASE]=bytearray(bytes.fromhex('ff05')+(creation.IO+4).to_bytes(4,'little')+bytes.fromhex('33c0c20800'))
    # Use the real contiguous-pool initializer. The earlier creation-only
    # fixture had no allocator+24 low sentinel, so it cannot certify free().
    for address in [f.BLOCK,f.FH,f.FT,f.UH,f.UT]:memory.pop(address)
    memory[ARENA]=bytearray([0xA5])*(536+64)
    creation.f.put(memory[f.WORLD],0x46,1)
    creation.f.put(memory[f.WORLD],0x4A,ARENA)
    creation.f.put(memory[f.WORLD],0x4E,536)
    creation.f.put(memory[f.WORLD],0x723B,world_gate)
    node=ARENA+232
    calls=args['before_calls']
    for call in calls:
        if call['registers'].get('ECX')==hex(expected['node']):call['registers']['ECX']=hex(node)
    calls=calls[:4]+[dict(address='0x554ad0',registers=dict(ECX=hex(MANAGER)),stack_args=[])]+calls[4:]
    calls+=[dict(address='0x4b7c90',registers=dict(ECX=hex(f.WORLD+8)),stack_args=[100])]
    expected=dict(expected,node=node,arena=ARENA,arena_bytes=536,pool_initializer=0x554AD0,world_gate=world_gate)
    args.update(address='0x4aa310',registers=json.dumps(dict(ECX='0',DF='0')),before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])))
    args['allowed_external_IO_code'].append([RELEASE,len(memory[RELEASE])])
    args['observe_writes'] += [[0x6ED540,4096],[0x6EE540,4096*4],[0x6A4818,12],[ARENA,600]]
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB,creation.LOAD_SOUND,RELEASE]}
    # The original Trap vtable is checked even after instance destruction.
    from build_effect_semantics import pe_read
    checks[0x66D754]=pe_read(EXE.read_bytes(),0x66D754,96)
    return args,checks,expected


if __name__=='__main__':
    value=int(sys.argv[1],0) if len(sys.argv)>1 else 0
    args,checks,expected=build(value)
    result=creation.base.cpu.run(EXE,args,list(checks.items()),f.OBS,expected['node'],291)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
    path=ROOT/'ghidra'/f'phase120-trap-removal-discovery-{tag}.json'
    path.write_text(json.dumps(dict(scope=__doc__,input=args,expected=expected,result=result),ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k not in ['captured_hex','visited_instructions','context_mismatches','observed_writes','observed_reads','stack_at_stop_hex']}))
    print('file',path.name)
