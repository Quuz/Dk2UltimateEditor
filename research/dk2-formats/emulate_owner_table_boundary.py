"""Original46-entry owner helper; bounded lookup and whole-buffer write contract.

The caller45A0D0 is static only. Runtime buffer/record contents are prepared;
this is not a raw KWD load, complete death state or a new Crypt bonus.
"""
from pathlib import Path
import json,struct
import native_unicorn_keepertrace as cpu
from build_effect_semantics import EXE
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
OWNER,OBS=0x10000000,0x10010000
sha=cpu.sha

def cases():
    return [dict(name='none',param=34,match=[]),dict(name='first',param=34,match=[0]),dict(name='middle',param=34,match=[23]),
        dict(name='last',param=34,match=[45]),dict(name='all',param=34,match=list(range(46))),
        dict(name='alternating',param=34,match=list(range(0,46,2))),dict(name='byte0',param=0,match=[0,45]),
        dict(name='byte255',param=255,match=[0,45]),dict(name='high-arg-low34',param=0x12340022,match=list(range(46))),
        dict(name='unknown108B1',param=34,match=[],counter=1),dict(name='unknown108B255',param=34,match=[],counter=255)]

def build(c):
    b=bytearray((i*37+11)&255 for i in range(0x1125+32));b[0x108B]=c.get('counter',0)
    for i in range(46):
        b[0xB57+18*i]=c['param']&255 if i in c['match'] else (c['param']+1)&255
        struct.pack_into('<I',b,0xB65+18*i,0xD0000000+i)
    mem={OWNER:bytes(b),OBS:bytes([0xCC]),0x7FFEE000:bytes([0xA5])*0x4000}
    args=dict(program='DKII-DX.exe',address='0x4dc7e0',registers=json.dumps(dict(ECX=hex(OWNER))),before_calls=[],stack_args=[c['param']],
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=5000,
        observe_reads=[[OWNER,len(b)]],observe_writes=[[OWNER,len(b)]])
    return args,{a:b for a,b in mem.items() if a!=0x7FFEE000}

def execute(c):
    args,checks=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];p=ROOT/'ghidra'/f'phase157-owner-table-native-{tag}.json'
    if p.exists():d=json.loads(p.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),OBS,OWNER,0x1125+32));p.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,d,p

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'] and not r['before_calls']
    post=post_context(checks,r);want={a:bytearray(b) for a,b in checks.items()};want[OWNER][0xB47]=c['param']&255
    for i in c['match']:struct.pack_into('<I',want[OWNER],0xB65+18*i,0xA000)
    assert post==want
    reads=r['observed_reads'];assert [int(x['address'],16)-OWNER for x in reads]==[0xB57+18*i for i in range(46)]
    assert all(x['size']==1 for x in reads)
    writes=r['observed_writes'];assert [(int(x['address'],16)-OWNER,x['size'],x['value']) for x in writes]==[(0xB47,1,c['param']&255)]+[(0xB65+18*i,4,0xA000) for i in c['match']]
    assert int(r['registers']['EAX'],16)==OWNER+0xB65+46*18 and int(r['registers']['ECX'],16)==0
    assert all(not(0x108A<=int(x['address'],16)-OWNER<0x109C) for x in reads+writes)
    return post,dict(entry_reads=46,matching_records=c['match'],write_count=len(writes),first_read=0xB57,last_read=0xB57+45*18,
        final_pointer=OWNER+0xB65+46*18,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'],Crypt_counter_accesses=0)

def main():
    rows=[]
    for c in cases():
        args,ch,d,p=execute(c);_,out=verify(c,args,ch,d['result']);rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],'verified',flush=True)
    (ROOT/'phase157-owner-table-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
