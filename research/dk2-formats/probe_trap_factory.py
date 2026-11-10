"""Discovery: original CTrap factory with definition-read observation.

Uses the prior explicit World/pool/map/owner fixture. No accepted semantics
until complete return and prepared-context effects have been verified. Native
code/vtables are unchanged; only inherited external null GFX/sound IO exists.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import emulate_object_factory as f
import native_unicorn_factorytrace as cpu
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def build(unknown=0,flags=0):
    args,_=f.build(f.cases()[0],0)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']
        if (int(r['address'],16)<0x11000000 or int(r['address'],16)==0x7FFEF000) and int(r['address'],16)!=f.OBS}
    definition=bytearray(209)+bytearray([0xA5])*64
    f.put(definition,0x98,unknown,2)
    f.put(definition,0x88,flags)
    f.put(definition,0x92,0x1234,2)
    f.put(definition,0xA2,1000,2)
    definition[0xB4]=1
    memory[f.DEFINITION]=definition
    f.put(memory[f.WORLD],0x15E4,f.DEFINITION)
    f.put(memory[f.WORLD],0x15EC,1)
    # An absent external sound registration must return0, not a fake live handle.
    memory[f.SOUND_CB][7:11]=bytes(4)
    stack=memory[0x7FFEF000]
    struct.pack_into('<5I',stack,4096,f.OBS,1,7,f.ARGS,f.OUT+4)
    checks=[(a,bytes(b)) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB]]
    original=pe_read(EXE.read_bytes(),0x66D754,96)
    checks.append((0x66D754,original))
    args.update(address='0x4b7c10',registers=json.dumps(dict(ECX=hex(f.WORLD+8),DF='0')),
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),
        allowed_external_IO_code=[[a,len(memory[a])] for a in [f.GFX_CB,f.SOUND_CB]],
        observe_reads=[[f.DEFINITION,len(definition)],[0,4096]],
        observe_writes=[[0x66D754,96],[f.BLOCK,768]],max_steps=300000)
    node=f.BLOCK+(512-304)+12
    return args,checks,node


if __name__=='__main__':
    unknown=int(sys.argv[1],0) if len(sys.argv)>1 else 0
    flags=int(sys.argv[2],0) if len(sys.argv)>2 else 0
    args,checks,node=build(unknown,flags)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
    result=cpu.run(EXE,args,checks,f.OBS,node,291)
    path=ROOT/'ghidra'/f'phase118-trap-factory-discovery-{tag}.json'
    path.write_text(json.dumps(dict(scope=__doc__,unknown=unknown,flags=flags,input=args,result=result),ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k not in ['captured_hex','visited_instructions','context_mismatches','observed_writes','observed_reads','stack_at_stop_hex']}))
    print('file',path.name)
    print('DefinitionReads',[r for r in result['observed_reads'] if int(r['address'],16)>=f.DEFINITION])
