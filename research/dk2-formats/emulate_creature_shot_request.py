"""Native4982B0/4984D0 to actual4B7CD0 pre-allocation refusal.

Real4943F0,clock517490,CreatureSpell exists50E080/get50DFA0 and
Shot exists50E2D0 execute. Native factory gate6A4818=0 prevents allocation.
Synthetic vtable observer wraps real Shot-exists only to capture arguments.
No original code is replaced. A prepared guard avoids atexit registration;
rotation coefficients are explicit data fixtures, not proved angle population.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_creature_shot_request import rotated_origin,waiting
from native_spell_alternatives import full_intermediate,runtime_projection
ROOT=Path(__file__).resolve().parent
CREATURE=0x10000000;DEFINITION=0x10010000;WORLD=0x10020000;WV=0x10030000;SPELL=0x10040000;SHOT=0x10050000;SESSION=0x10051000;OUT=0x10060000;OBS=0x10080000;CAPTURE=0x10090000;STACK=0x7FFF0000
import dk2_formats as fmt
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/CreatureSpells.kwd'
SOURCE_RAW=next(r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==160 for r in s.records if r[0xE6]==12)

def cases():
    base=dict(shot=17,shot_exists=True,spell_exists=True,slot=1,count=3,slot_flag=1,flags=0,old_flags=0xF00D0234,tick=100,last=0,delay=5,angle=3,
      a=0,b=65536,position=[0x1200,0xFFFFDE00,0x3300],offsets=[4096,8192,0xFFFFF000],alternative=[36,4])
    variants=[('base',{}),('shot-zero',dict(shot=0)),('shot255',dict(shot=255)),('shot-missing',dict(shot_exists=False)),('spell-missing',dict(spell_exists=False)),
      ('slot0',dict(slot=0)),('slot2',dict(slot=2)),('slot-invalid',dict(slot=0xFFFFFFFF)),('empty-slots',dict(count=0,slot=0)),
      ('slot-flag-clear',dict(slot_flag=0)),('slot-flag-high-only',dict(slot_flag=0x80)),
      ('deadline-equal',dict(tick=5)),('deadline-before',dict(tick=4)),('deadline-after',dict(tick=6)),
      ('deadline-wrap-wait',dict(last=0xFFFFFFFE,tick=2)),('deadline-wrap-ready',dict(last=0xFFFFFFFE,tick=4)),
      ('possession-bypass',dict(flags=0x80,tick=0)),('rotated-A-positive',dict(a=65536,b=0)),('rotated-A-negative',dict(a=0xFFFF0000,b=0)),
      ('rotation-product-wrap',dict(offsets=[0x80000000,0x7FFFFFFF,0xFFFFFFFF])),
      ('position-wrap',dict(position=[0xFFFFFFFF,1,0xFFFFFFFF])),
      ('slot256-is-not-slot0',dict(slot=256)),('slot-flag-clears-existing',dict(slot_flag=0,old_flags=0xF00D1234)),
      ('alternative-zero',dict(alternative=[0,0])),('alternative68',dict(alternative=[68,0])),('alternative255',dict(alternative=[255,255]))]
    return [dict(base,**kw,name=f'{a:x}-{name}',root=a) for a in [0x4982B0,0x4984D0] for name,kw in variants]

def store_eax(a):return b'\xA3'+pack32(a)

def build(c):
    creature=bytearray((i*17+19)&255 for i in range(0x600));definition=bytearray((i*23+11)&255 for i in range(2199));world=bytearray((i*13+7)&255 for i in range(0x1630));wv=bytearray(0x1B0)
    for a,v in [(0x370,DEFINITION),(0x399,c['slot']),(0x3A5,c['last']),(0x3E6,c['flags']),(0x3EA,c['old_flags'])]:struct.pack_into('<I',creature,a,v)
    struct.pack_into('<H',creature,0x28,0xABCD);struct.pack_into('<H',creature,0xF4,c['angle'])
    for i,v in enumerate(c['position']):struct.pack_into('<I',creature,0x1A+4*i,v)
    definition[0x789]=c['count']
    for i,ident in enumerate([8,12,20]):
        slot=0x5E1+19*i;definition[slot]=12 if i==c['slot'] else ident
        struct.pack_into('<I',definition,slot+2,c['delay'])
        for j,v in enumerate(c['offsets']):struct.pack_into('<I',definition,slot+6+4*j,v)
        definition[slot+18]=c['slot_flag'] if i==c['slot'] else 0
    raw=bytearray(SOURCE_RAW);raw[0xE7]=c['shot'];raw[0xE8:0xEA]=bytes(c['alternative'])
    spell=bytearray(runtime_projection(full_intermediate(raw)));spell[0x43]=12 if c['spell_exists'] else 11
    shot=bytearray((i*31+3)&255 for i in range(118));shot[0x58]=c['shot'] if c['shot_exists'] else (16 if c['shot']!=16 else 15)
    for a,v in [(0,WV),(4,SESSION),(0x1C,WORLD),(0x1614,SPELL),(0x161C,1),(0x1624,SHOT),(0x162C,1)]:struct.pack_into('<I',world,a,v)
    for slot,fn in [(0xE4,0x50E080),(0xE8,0x50DFA0),(0x1AC,CAPTURE)]:struct.pack_into('<I',wv,slot,fn)
    valid_slot=c['slot']<c['count'];current_id=12 if valid_slot else 0
    matching=any(definition[0x5E1+19*i]==current_id for i in range(c['count']))
    flags=(c['old_flags']&~0x1000)|((c['slot_flag']&1)<<12) if valid_slot and matching else c['old_flags']
    blocked=waiting(c['tick'],c['last'],c['delay'] if valid_slot else 0,c['flags'])
    call=not blocked and valid_slot and c['spell_exists']
    origin=rotated_origin(c['position'],c['offsets'],c['a'],c['b'],bool(c['flags']&0x80)) if call else None
    exists=int(c['shot_exists'] and c['shot']!=0)
    out=bytearray(128);struct.pack_into('<I',out,0,int(blocked))
    if call:
        for at,v in [(0x10,1),(0x14,c['shot']),(0x18,0xABCD),(0x28,c['angle']),(0x2C,1),(0x30,exists),(0x34,0x4B7CE5),(0x38,WORLD)]:struct.pack_into('<I',out,at,v)
        struct.pack_into('<3I',out,0x1C,*origin)
    want_creature=bytearray(creature);struct.pack_into('<I',want_creature,0x3EA,flags)
    stack=bytearray([0xA5])*8192;struct.pack_into('<I',stack,4096,OBS)
    buffers=[(CREATURE,creature,want_creature),(DEFINITION,definition,definition),(WORLD,world,world),(WV,wv,wv),(SPELL,spell,spell),(SHOT,shot,shot),(SESSION,pack32(0)+pack32(c['tick']),pack32(0)+pack32(c['tick'])),
      (OUT,bytes(128),out),(0,bytes(4),bytes(4)),(0x6E5050,pack32(WORLD),pack32(WORLD)),(0x6A4818,bytes(4),bytes(4)),(0x6E4168,b'\x01',b'\x01'),(0x6E4178,bytes(19),bytes(19)),
      (0x6C1488+4*c['angle'],pack32(c['a']),pack32(c['a'])),(0x6C1C88+4*c['angle'],pack32(c['b']),pack32(c['b']))]
    capture=bytes.fromhex('ff05')+pack32(OUT+0x10)+bytes.fromhex('890d')+pack32(OUT+0x38)
    for offset,at,op in [(4,0x14,'0fb64424'),(28,0x18,'0fb74424'),(44,0x2C,'8b4424'),(0,0x34,'8b4424')]:capture+=bytes.fromhex(op)+bytes([offset])+store_eax(OUT+at)
    capture+=bytes.fromhex('8b4c2420')
    for i in range(3):capture+=(bytes.fromhex('8b01') if i==0 else bytes.fromhex('8b41')+bytes([4*i]))+store_eax(OUT+0x1C+4*i)
    capture+=bytes.fromhex('8b4c24240fb701')+store_eax(OUT+0x28)+bytes.fromhex('8b0d')+pack32(OUT+0x38)+bytes.fromhex('ff742404b8')+pack32(0x50E2D0)+bytes.fromhex('ffd0')+store_eax(OUT+0x30)+bytes.fromhex('c20400')
    mem=[region(a,b) for a,b,_ in buffers]+[region(STACK-4096,stack),region(CAPTURE,capture)]
    observer=store_eax(OUT)+b'\xBD'+pack32(1);at=0x11000000
    for address,_,b in buffers:mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    expected=dict(native_return=int(blocked),factory_calls=int(call),shot_id=c['shot'] if call else None,origin=origin,shot_exists=exists if call else None,actor_flags3EA=flags,allocation_reached=False)
    return dict(program='DKII-DX.exe',address=hex(c['root']),max_steps=150000,registers=json.dumps(dict(ECX=hex(CREATURE))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,expected=build(c);path=ROOT/'ghidra'/f'phase79-emulation-request-{c["name"]}.json'
            if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args);value='\n'.join(x.text for x in response.content if x.type=='text')
                try:r=json.loads(value)
                except ValueError:path.with_suffix('.error.txt').write_text(value,encoding='utf8');raise
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],r,expected)
            rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase79-creature-shot-request-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
