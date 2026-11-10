"""Original 51DB30→50F7E0 loading and 4F1600→4F1660 history/matching.

All target code unmodified. Driver/observer use disjoint synthetic memory.
No game process, GUI, filesystem save, or reward factory is executed here.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_sacrifice import *
ROOT=Path(__file__).resolve().parent
WORLD=0x10000000;ROOM=0x10010000;RECIPE=0x10012000;RAW=0x10014000;OUT=0x10018000;RETURNS=0x10019000
DRIVER=0x10020000;OBS=0x10028000;SHADOW=0x10100000;STACK=0x7fff0000

def call(at,target):return b'\xe8'+pack32(target-at-5)
def seed_room():
    room=bytearray((i*37+13)&255 for i in range(0xc0));room[0x5b:0x5e]=bytes([255,0,0]);return bytes(room)

def cases():
    rows=[]
    load_variants=[('full',raw_recipe([(1,3),(2,7),(3,9)],4,0xfedcba98,255)),
      ('gap',raw_recipe([(1,3),(0,0xff),(2,7)],3,0x80000000,18)),
      ('all-empty',raw_recipe([(0,18),(0,19),(0,20)],0,0xffffffff,0)),
      ('high-types',raw_recipe([(255,255),(128,128),(127,127)],255,0x7fffffff,128))]
    for name,raw in load_variants:rows.append(dict(name='load-'+name,kind='load',initial_count=0,raws=[raw.hex()]))
    for count in [127,128,255]:rows.append(dict(name=f'load-capacity-{count}',kind='load',initial_count=count,raws=[load_variants[0][1].hex(),load_variants[1][1].hex()]))
    current=json.loads((ROOT/'readiness-examples-current-records.json').read_text())
    rows.append(dict(name='load-current44',kind='load',initial_count=0,raws=[r['raw_hex'] for r in current['records'] if r['words_u32'][0]==43]))
    # Native appends build the history. Probe lists contain raw records, independently converted for the native matcher.
    variants=[('one',[(1,8,7)],[(1,8)]),('ordered-two',[(1,8,2),(2,7,0)],[(1,8),(2,7)]),
      ('reversed-two',[(1,8,2),(2,7,0)],[(2,7),(1,8)]),('ordered-three',[(1,3,2),(2,7,0),(3,9,0)],[(1,3),(2,7),(3,9)]),
      ('wrong-type',[(1,8,7)],[(2,8)]),('wrong-id',[(1,8,7)],[(1,9)]),
      ('not-enough',[(1,8,7)],[(1,8),(1,8)]),('suffix-one',[(2,7,0),(1,8,5)],[(1,8)]),
      ('suffix-two',[(3,9,0),(1,8,5),(2,7,0)],[(1,8),(2,7)]),
      ('ring-wrap',[(255,255,0),(1,3,2),(2,7,0),(3,9,0)],[(1,3),(2,7),(3,9)]),
      ('oldest-dropped',[(255,255,0),(1,3,2),(2,7,0),(3,9,0)],[(255,255)]),
      ('empty-recipe',[(1,8,7)],[]),('empty-history',[],[(1,8)]),('empty-both',[],[])]
    for name,events,inputs in variants:rows.append(dict(name='match-'+name,kind='match',events=events,raw=raw_recipe(inputs,4,0x89abcdef,17).hex()))
    for levels in [[1],[10],[0],[255],[2,7],[1,2],[255,255],[255,255,255],[100,100,100],[1,2,10]]:
        rows.append(dict(name='average-'+'-'.join(map(str,levels)),kind='match',events=[(1,8,n) for n in levels],raw=raw_recipe([(1,8)]*len(levels),1,5,23).hex()))
    for value in [0x00010005,0xffff0005,0xffffffff]:
        rows.append(dict(name=f'packed-add-{value:x}',kind='match',events=[(1,8,9)],raw=raw_recipe([(1,8)],1,value,0).hex()))
    rows += [dict(name='fault-no-creature',kind='match',events=[(2,7,0)],raw=raw_recipe([(2,7)],1,5,0).hex()),
             dict(name='fault-empty-creature-reward',kind='match',events=[],raw=raw_recipe([],1,5,0).hex())]
    return rows

def build(c):
    regions=[];expected=[];driver=bytearray();returns=bytearray(256);want_returns=bytearray(returns);fault=None;steps=[]
    def emit(b):driver.extend(b)
    def native(a):emit(call(DRIVER+len(driver),a))
    def save_return(i):emit(b'\xa3'+pack32(RETURNS+4*i))
    if c['kind']=='load':
        world=bytearray((i*11+7)&255 for i in range(0x7400));world[0x731b]=c['initial_count'];want=bytearray(world)
        raws=[bytes.fromhex(v) for v in c['raws']]
        for i,raw in enumerate(raws):
            arg=bytes(16)+raw;ptr=RAW+32*i;regions.append(region(ptr,arg));expected.append((ptr,arg))
            count=want[0x731b]
            if count<128:want[0x2e70+34*count:0x2e70+34*(count+1)]=runtime_recipe(raw);want[0x731b]+=1
            if i==0:entry=0x51db30;ecx=WORLD;stack_args=pack32(ptr)
            else:emit(b'\x68'+pack32(ptr)+b'\xb9'+pack32(WORLD));native(0x51db30)
            save_return(i);struct.pack_into('<I',want_returns,4*i,1)
        regions.append(region(WORLD,world));expected.append((WORLD,bytes(want)));steps.append(dict(loaded=len(raws),final_count=want[0x731b]))
    else:
        raw=bytes.fromhex(c['raw']);recipe=runtime_recipe(raw);room=seed_room();want_room=room
        regions.extend([region(ROOM,room),region(RECIPE,recipe),region(OUT,bytes([0x66])*12)])
        expected.append((RECIPE,recipe));events=c['events']
        if events:
            entry=0x4f1600;ecx=ROOM;stack_args=struct.pack('<III',*events[0]);want_room=history_append(want_room,events[0])
            for event in events[1:]:
                kind,ident,level=event
                emit(b'\x68'+pack32(level)+b'\x68'+pack32(ident)+b'\x68'+pack32(kind)+b'\xb9'+pack32(ROOM));native(0x4f1600)
                want_room=history_append(want_room,event)
            emit(b'\x68'+pack32(OUT)+b'\x68'+pack32(RECIPE)+b'\xb9'+pack32(ROOM));native(0x4f1660)
        else:entry=0x4f1660;ecx=ROOM;stack_args=pack32(RECIPE)+pack32(OUT)
        try:result=match(want_room,recipe)
        except ZeroDivisionError:fault=dict(pc='004f1749',error='Divide by 0');result=None
        save_return(0)
        if result:
            struct.pack_into('<I',want_returns,0,int(result['matched']));expected.extend([(ROOM,want_room),(OUT,result['output'])]);steps.append(dict(matched=result['matched'],output_hex=result['output'].hex(),average_level=result.get('average_level')))
    regions.append(region(RETURNS,returns));expected.append((RETURNS,bytes(want_returns)))
    emit(b'\x68'+pack32(OBS)+b'\xc3')
    observer=bytearray(b'\xbd'+pack32(1));shadow=SHADOW
    for i,(address,data) in enumerate(expected):
        regions.append(region(shadow,data));observer.extend(compare_code(address,shadow,len(data))+bytes.fromhex('21c5'));shadow+=((len(data)+255)//256)*256
    observer.extend(b'\x68'+pack32(0xdeadbeef)+b'\xc3')
    regions.extend([region(DRIVER,driver),region(OBS,observer),region(STACK,pack32(DRIVER)+stack_args)])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args=dict(program='DKII-DX.exe',address=hex(entry),max_steps=400000,registers=json.dumps(dict(ECX=hex(ecx))),
              memory=json.dumps(dict(regions=regions)),return_registers='EAX,EBP,ECX,EIP,ESP')
    return args,fault,steps

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[];selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,fault,steps=build(c);path=ROOT/'ghidra'/f'phase88-sacrifice-emulation-{c["name"]}.json'
                if path.exists():
                    saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_fault']==fault
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_fault=fault,expected=steps,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result']
                if fault:assert not r['success'] and not r['hit_return'] and r['final_pc']==fault['pc'] and r['emulation_error']==fault['error'],(c['name'],r)
                else:assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,ECX=0).items()),(c['name'],r)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name'],fault=bool(fault)));print(c['name'],'verified',flush=True)
    (ROOT/('phase88-sacrifice-matching-emulations.json' if len(sys.argv)==1 else 'phase88-sacrifice-one-case.json')).write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
