"""Actual AI/predicate initializers, wrapper, dispatcher, getter and predicates.

All native code is unchanged. World/creature/definitions/level coefficients
are explicit data fixtures. The complete281-row AI table is initialized by
native47BBE0, not replaced with invented state categories. The observer also
calls478050 separately after saving the predicate result for diagnostic data.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_spell_predicates import eligible,effective_category
from spell_state_tables import initialized_states,BASE,SIZE
ROOT=Path(__file__).resolve().parent
CREATURE=0x10000000;DEFINITION=0x10010000;WORLD=0x10020000;WV=0x10030000;SPELL=0x10040000;LEVELS=0x10050000;OUT=0x10060000;OBS=0x10080000;STACK=0x7FFF0000
TABLE,STATE_ROWS=initialized_states();CATEGORIES=bytes(r['category'] for r in STATE_ROWS)
TIMERS={1:0x459,3:0x443,4:0x445,5:0x44F}

def cases():
    combat=next(r['state'] for r in STATE_ROWS if r['category']==22)
    transit=next(r['state'] for r in STATE_ROWS if r['category']==2)
    other=next(r['state'] for r in STATE_ROWS if r['category'] not in {2,22})
    base=dict(rule=1,timer=0,hp=49,base_hp=100,level=1,coefficient=100,current=combat,backup=other,id_argument=12,direct=False,second_argument=0)
    out=[]
    def add(name,**kw):out.append(dict(base,**kw,name=name))
    for value in [0,6,128,255]:add(f'rule-{value}',rule=value)
    for value in [0,1,0x100,0xFFFF]:add(f'haste-timer-{value:04X}',timer=value)
    for name,kw in [
      ('heal-below',dict(hp=49)),('heal-equal',dict(hp=50)),('heal-above',dict(hp=51)),
      ('heal-odd-base',dict(base_hp=101,hp=50)),('heal-zero-base',dict(base_hp=0,hp=0)),
      ('heal-negative-hp',dict(base_hp=0,hp=0xFFFFFFFF)),('heal-level1-ignores-coefficient',dict(coefficient=-32768)),
      ('heal-level0-scaled',dict(level=0,coefficient=150,hp=74)),
      ('heal-scaled-below',dict(level=2,coefficient=150,hp=74)),('heal-scaled-equal',dict(level=2,coefficient=150,hp=75)),
      ('heal-negative-coefficient',dict(level=2,coefficient=-100,hp=100)),('heal-zero-coefficient',dict(level=2,coefficient=0,hp=0)),
      ('heal-maximum-positive-product',dict(level=255,base_hp=65535,coefficient=32767,hp=10736926)),
      ('heal-minimum-product',dict(level=255,base_hp=65535,coefficient=-32768,hp=0x7FFFFFFF))]:add(name,rule=2,**kw)
    for rule in [3,4,5]:
        for label,kw in [('ready',{}),('timer1',dict(timer=1)),('timer-highbyte',dict(timer=0x100)),
          ('other-state',dict(current=other)),('backup-combat',dict(current=transit,backup=combat)),
          ('backup-other',dict(current=transit,backup=other)),('backup-transit-no-recursion',dict(current=transit,backup=transit))]:add(f'rule{rule}-{label}',rule=rule,**kw)
    add('id-high-bits',id_argument=0x8000000C)
    add('direct-second-argument',rule=3,direct=True,second_argument=0xDEAD1234)
    for row in STATE_ROWS:
        if row['category']==22 and row['state']!=combat:add(f'rule3-state-{row["state"]}',rule=3,current=row['state'])
    add('backup-not-read-for-direct-category',rule=3,backup=0xFFFFFFFF)
    add('haste-helper-other-state',current=other)
    add('heal-helper-other-state',rule=2,current=other)
    return out

def build(c):
    creature=bytearray((i*17+19)&255 for i in range(0x600));definition=bytearray((i*23+11)&255 for i in range(2199));world=bytearray((i*13+7)&255 for i in range(0x1624));spell=bytearray((i*31+3)&255 for i in range(70));levels=bytearray((i*29+5)&255 for i in range(0x300));wv=bytearray(0xEC)
    for a,v in [(0x370,DEFINITION),(0x40,c['hp']),(0x25E,c['current']),(0x32D,c['backup'])]:struct.pack_into('<I',creature,a,v)
    creature[0x403]=c['level']
    for a in TIMERS.values():struct.pack_into('<H',creature,a,c['timer'])
    struct.pack_into('<H',definition,0x692,c['base_hp']);struct.pack_into('<h',levels,0xE1+2*c['level'],c['coefficient'])
    struct.pack_into('<I',world,0,WV);struct.pack_into('<I',world,0x1614,SPELL);struct.pack_into('<I',world,0x161C,1)
    struct.pack_into('<I',wv,0xE8,0x50DFA0);spell[0x43]=12;spell[0x45]=c['rule']
    pred_table=bytearray()
    for i,a in enumerate([0,0x49A830,0x49A840,0x49A8C0,0x49A8F0,0x49A920]):pred_table+=struct.pack('<IIIIB',a,0,0,0xFFFFFFFF if i==0 else 0,i)
    category=effective_category(CATEGORIES,c['current'],c['backup']);result=eligible(c['rule'],hp=c['hp'],base_hp=c['base_hp'],level=c['level'],coefficient=c['coefficient'],timer=c['timer'],category=category)
    want_out=pack32(result)+pack32(category)+bytes(56)
    buffers=[(CREATURE,creature,creature),(DEFINITION,definition,definition),(WORLD,world,world),(WV,wv,wv),(SPELL,spell,spell),(LEVELS,levels,levels),
      (OUT,bytes(64),want_out),(BASE,bytes([0xA5])*SIZE,TABLE),(0x6E4AC8,bytes([0xA5])*102,pred_table),
      (0x6E5050,pack32(WORLD),pack32(WORLD)),(0x6E4154,pack32(LEVELS),pack32(LEVELS))]
    mem=[region(a,b) for a,b,_ in buffers]+[region(STACK,pack32(OBS))]
    observer=b'\xB8'+pack32(0x49A6C0)+bytes.fromhex('ffd0')
    if c['direct']:observer+=b'\x68'+pack32(c['second_argument'])
    observer+=b'\x68'+pack32(c['id_argument'])+b'\xB9'+pack32(CREATURE)+b'\xB8'+pack32(0x49A7B0 if c['direct'] else 0x49A7A0)+bytes.fromhex('ffd0a3')+pack32(OUT)
    observer+=b'\xB9'+pack32(CREATURE+0x24E)+b'\xB8'+pack32(0x478050)+bytes.fromhex('ffd0a3')+pack32(OUT+4)
    observer+=b'\xBD'+pack32(1);at=0x11000000
    for address,_,b in buffers:
        mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program='DKII-DX.exe',address='0x47bbe0',max_steps=350000,registers=json.dumps({r:'0x0' for r in ['EAX','EBX','ECX','EDX','ESI','EDI','EBP']}),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(native_predicate_return=result,diagnostic_category=category)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,expected=build(c);path=ROOT/'ghidra'/f'phase78-emulation-predicate-{c["name"]}.json'
            if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args)
                value='\n'.join(x.text for x in response.content if x.type=='text')
                try:r=json.loads(value)
                except ValueError:
                    path.with_suffix('.error.txt').write_text(value,encoding='utf8');raise
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],r,expected)
            rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase78-spell-predicate-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
