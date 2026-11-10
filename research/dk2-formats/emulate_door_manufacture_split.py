"""Original DX raw Door -> constructor/initializer/converter/loader after API edits.

Null visual provider and nonzero sound/checksum gate are explicit. The historical
generic setter is replayed from its frozen research copy, not a game editor.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import dk2_formats_phase141 as old
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read
import native_unicorn_factorytrace as cpu

ROOT=Path(__file__).resolve().parent
RAW,MID,DEF,BRIDGE,SESSION,STOP=0x10000000,0x10001000,0x10002000,0x10003000,0x10004000,0x10140000
sha=cpu.sha

def cases():return ['baseline','fixed-one','legacy-one','fixed-max','fixed-upper']

def raw_record(name):
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Doors.kwd'
    raw=bytearray(fmt.parse(source.read_bytes())[0].records[0]);raw[0x236:0x238]=bytes.fromhex('5aa5')
    raw=bytes(raw)
    if name=='baseline':return raw
    if name=='legacy-one':return old.set_scalar(raw,'Doors','ManufToBuild',1)
    if name=='fixed-one':return fmt.set_scalar(raw,'Doors','ManufToBuild',1)
    if name=='fixed-max':return fmt.set_scalar(raw,'Doors','ManufToBuild',65535)
    if name=='fixed-upper':return fmt.set_scalar(raw,'Doors','Unknown236',0xE7A9)
    raise ValueError(name)

def build(name):
    raw=raw_record(name);bridge=pack32(0)+pack32(SESSION)
    mem=[region(RAW,raw+bytes([165])*64),region(MID,bytes([165])*(732+64)),region(DEF,bytes([165])*(181+64)),
        region(BRIDGE,bridge),region(SESSION,bytes(24)),region(0x6FCDCF,pack32(1)),region(0,bytes(4)),region(0x7FFEF000,bytes([165])*8192)]
    calls=[dict(address='0x4a7040',registers=dict(ECX=hex(DEF)),stack_args=[]),
        dict(address='0x56a920',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x56aa40',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x4a7100',registers=dict(ECX=hex(DEF)),stack_args=[MID,0])]
    args=dict(program='DKII-DX.exe',address='0x517490',registers=json.dumps(dict(ECX=hex(BRIDGE))),before_calls=calls,
        memory=json.dumps(dict(regions=mem)),max_steps=100000,
        observe_reads=[[RAW,616],[MID,732],[DEF,181],[0,4096]],observe_writes=[[RAW,680],[MID,796],[DEF,245],[0,4096]])
    checks={int(r['address'],16):bytes.fromhex(r['hex']) for r in mem if int(r['address'],16)!=0x7FFEF000}
    return args,checks

def execute(name):
    args,checks=build(name);tag=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    path=ROOT/'ghidra'/f'phase142-door-split-native-{name}-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(EXE,args,list(checks.items()),STOP,DEF,181);saved=dict(case=name,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(name,r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path

def verify(name,args,checks,r):
    post=post_context(checks,r);raw=checks[RAW]
    for a,n in [(RAW,616),(MID,732),(DEF,181)]:assert post[a][n:]==checks[a][n:]
    assert post[RAW]==raw
    assert post[MID][0x2A8:0x2AC]==raw[0x234:0x238]
    assert post[DEF][0x95:0x97]==raw[0x234:0x236] and post[DEF][0x99:0x9B]==raw[0x236:0x238]
    assert len(r['before_calls'])==4 and all(c['returned'] for c in r['before_calls'])
    assert int(r['registers']['EAX'],16)==0
    # The actual loader has distinct native16-bit writes for the two fields.
    writes=[x for x in r['observed_writes'] if int(x['address'],16) in [DEF+0x95,DEF+0x99]]
    assert any(x['size']==2 and int(x['address'],16)==DEF+0x95 for x in writes)
    assert any(x['size']==2 and int(x['address'],16)==DEF+0x99 for x in writes)
    return post,dict(manufacture_raw=int.from_bytes(raw[0x234:0x236],'little'),unknown_raw=int.from_bytes(raw[0x236:0x238],'little'),
        manufacture_definition=int.from_bytes(post[DEF][0x95:0x97],'little'),unknown_definition=int.from_bytes(post[DEF][0x99:0x9B],'little'),
        compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[];posts={}
    for name in cases():
        args,checks,saved,path=execute(name);post,out=verify(name,args,checks,saved['result']);posts[name]=post
        rows.append(dict(case=name,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())));print(name,out,flush=True)
    for name,value in [('fixed-one',1),('fixed-max',65535)]:
        expected={a:bytearray(b) for a,b in posts['baseline'].items()}
        for a,o in [(RAW,0x234),(MID,0x2A8),(DEF,0x95)]:expected[a][o:o+2]=value.to_bytes(2,'little')
        assert {a:bytes(b) for a,b in expected.items()}==posts[name]
    expected={a:bytearray(b) for a,b in posts['baseline'].items()}
    for a,o in [(RAW,0x236),(MID,0x2AA),(DEF,0x99)]:expected[a][o:o+2]=bytes.fromhex('a9e7')
    assert {a:bytes(b) for a,b in expected.items()}==posts['fixed-upper']
    (ROOT/'phase142-door-split-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=3),indent=2),encoding='utf8')

if __name__=='__main__':main()
