"""Reject a new falseCryptCounter01 lead by executing the original boundedloop.

4C7360 checks exactly three owner-relative bytes E97/EB0/EC9. The pointer-only
scan followed the taken loop edge without constraining its EAX counter and
invented a read at108B after20 increments. No Cryptbonus meaning is assigned.
"""
from pathlib import Path
import json,struct
import native_unicorn_factorytrace as cpu
from build_effect_semantics import EXE,pe_read
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
OWNER,STOP,STACK=0x1C000000,0x1C100000,0x7FFEE000
READ_OFFSETS=[0xE97,0xEB0,0xEC9]
sha=cpu.sha

def cases():
    return [dict(name=f'crypt108b-{n}',counter=n,slots=[0,0,0]) for n in [0,1,127,255]]+[
        dict(name=f'slot{i}-active',counter=1,slots=[int(j==i) for j in range(3)]) for i in range(3)]+[
        dict(name='slot0-highbit',counter=255,slots=[128,0,0])]

def build(c):
    owner=bytearray([165])*0x1240
    for off,v in zip(READ_OFFSETS,c['slots']):owner[off]=v
    owner[0x108B]=c['counter'];stack=bytearray([165])*16384;struct.pack_into('<I',stack,8192,STOP)
    args=dict(program='DKII-DX.exe',address='0x4c7360',registers=json.dumps(dict(ECX=hex(OWNER))),
        memory=json.dumps(dict(regions=[region(OWNER,owner),region(STACK,stack),region(STOP,b'\xcc')])),
        max_steps=1000,return_registers='EAX,ECX,EIP,ESP',observe_reads=[[OWNER,len(owner)],[0,4096]],observe_writes=[[OWNER,len(owner)]])
    return args,{OWNER:bytes(owner)}

def execute(c):
    args,checks=build(c);tag=sha(json.dumps(args,sort_keys=True).encode())[:12];p=ROOT/'ghidra'/f'phase149-crypt-counter-boundary-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),STOP,OWNER,len(checks[OWNER]));saved=dict(case=c,input=args,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,checks,saved,p

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['context_mismatches'] and not r['protected_write_or_execution_failures']
    active=next((i for i,v in enumerate(c['slots']) if v),None);count=3 if active is None else active+1
    reads=[x for x in r['observed_reads'] if x['pc']=='0x4c7368']
    assert [int(x['address'],16)-OWNER for x in reads]==READ_OFFSETS[:count] and all(x['size']==1 for x in reads)
    assert len(r['observed_reads'])==count and not r['observed_writes']
    assert int(r['registers']['EAX'],16)==int(active is not None)
    assert int(r['registers']['ECX'],16)==OWNER+0xE97+25*(3 if active is None else active)
    assert bytes.fromhex(r['captured_hex'])==checks[OWNER]
    return dict(result=int(active is not None),read_offsets=READ_OFFSETS[:count],compared_regions=1,compared_bytes=len(checks[OWNER]),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,p=execute(c);out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],out,flush=True)
    (ROOT/'phase149-crypt-counter-boundary-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':main()
