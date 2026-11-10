"""Native room4D8E60→owner counter→complete health handler.

Prepared room and minimal tagged member record; the damaged Creature is the
verified native phase90 actor. Empty cleanup lists guarantee finite iteration.
No original game code, vtable, callback, or table is replaced.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_health as h
import native_room_counters as api
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
f=h.f;cf=h.cf;sp=h.sp;ROOT=Path(__file__).resolve().parent
ROOM=0x10300000;DRIVER=0x10310000;MEMBER=0x10320000;OTHEROWNER=0x10330000

def cases():
    base=dict(species=19,initial=0,operation='insert',possessed=True,alive=True,kind=0,room_owner=7)
    out=[dict(base,species=ids[0],name=f'group-{slot:x}-species{ids[0]}') for slot,ids in sorted(api.GROUPS.items())]
    out += [dict(base,**v,name=n) for n,v in [
      ('monk-normal',dict(possessed=False)),('monk-two',dict(initial=1)),
      ('monk-clear',dict(initial=1,operation='clear')),('monk-replace-same',dict(initial=1,operation='replace')),
      ('monk-clear-zero',dict(operation='clear')),('monk-register-overflow127',dict(initial=127)),
      ('monk-register-negative128',dict(initial=128)),('monk-clear-negative128',dict(initial=128,operation='clear')),
      ('monk-eight-possessed',dict(initial=7)),('monk-sixteen-normal',dict(initial=15,possessed=False)),
      ('monk-dead-tag',dict(alive=False)),('monk-object-kind',dict(kind=2)),
      ('monk-other-owner',dict(room_owner=8)),
      ('species0',dict(species=0)),('species1',dict(species=1)),('species46',dict(species=46)),('species255',dict(species=255))]]
    return out

def expected(c):
    slot=api.counter_slot(c['species']);value=c['initial'];valid=c['alive'] and c['kind']==0 and slot is not None
    if valid and c['operation'] in ['clear','replace']:value=api.change_counter(value,-1)
    if valid and c['operation'] in ['insert','replace']:value=api.change_counter(value,1)
    damage_count=value if slot==0x1095 and c['room_owner']==7 else 0
    hc=dict(h.cases()[0],owner1095=damage_count,possessed=c['possessed'])
    return dict(slot=slot,counter=value,damage_count=damage_count,health=h.outcome(hc),
      room_tag=0 if c['operation']=='clear' else 101,room55=0 if c['operation'] in ['clear','replace'] else 1234)

def build(c):
    e=expected(c);hc=dict(h.cases()[0],owner1095=e['damage_count'],possessed=c['possessed']);args,old=h.build(hc)
    allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a!=f.OBS}
    room=bytearray(0xc0)+bytearray([0xa5])*64
    f.put(room,4,200,2);room[0x18]=26;f.put(room,0x1d,c['room_owner'],2)
    f.put(room,0x53,101 if c['operation'] in ['clear','replace'] else 0,2);f.put(room,0x55,1234)
    memory[ROOM]=room;checks[ROOM]=bytearray(room);f.put(checks[ROOM],0x53,e['room_tag'],2);f.put(checks[ROOM],0x55,e['room55'])
    member=bytearray(0x380)+bytearray([0xa5])*64;f.put(member,4,101,2);f.put(member,0x12,c['kind']);member[0x36f]=c['species']
    memory[MEMBER]=member;checks[MEMBER]=bytearray(member)
    if c['room_owner']==8:
        memory[OTHEROWNER]=bytearray(0x1200);checks[OTHEROWNER]=bytearray(0x1200)
    for a in [f.PLAYER]+([OTHEROWNER] if c['room_owner']==8 else []):
        memory[a][0x108a:0x109c]=bytes(18);checks[a][0x108a:0x109c]=bytes(18)
    owner=f.PLAYER if c['room_owner']==7 else OTHEROWNER
    if e['slot'] is not None:
        memory[owner][e['slot']]=c['initial'];checks[owner][e['slot']]=e['counter']
    for b in [memory[0x6ee540],checks[0x6ee540]]:
        f.put(b,101*4,MEMBER);f.put(b,200*4,ROOM)
        if c['room_owner']==8:f.put(b,8*4,OTHEROWNER)
    for b in [memory[0x6ed540],checks[0x6ed540]]:
        b[101]=int(c['alive']);b[200]=1
        if c['room_owner']==8:b[8]=1
    # Preserve the raw224 loader return value for the existing health driver.
    wrapper=bytearray(b'\x50\x68'+pack32(e['room_tag'])+b'\xb9'+pack32(ROOM))
    wrapper+=b'\xe8'+pack32(0x4d8e60-(DRIVER+len(wrapper)+5))
    wrapper+=b'\x58\x68'+pack32(sp.DRIVER)+b'\xc3';memory[DRIVER]=wrapper
    f.put(memory[0x7ffee000],8192,DRIVER)
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=mem));e['checks']=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()]
    return args,e

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);path=ROOT/'ghidra'/f'phase94-room-counter-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['health']['hp']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'count',e['counter'],'HP',e['health']['hp'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase94-room-counter-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
