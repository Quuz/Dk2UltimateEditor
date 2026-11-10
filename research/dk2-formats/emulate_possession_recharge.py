"""Raw224/31 and scalar210→native interval, readiness and progress.

Real possession setter and (secondary cases) owner neighbour producer execute.
RawCreatureSpell cases use native init/converter/loader withnullresources.
Prepared actors/world fromphase90; no game functions,vtables,tables replaced.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed as sp
import native_possession_recharge as api
from native_spell_alternatives import full_intermediate,runtime_projection
from native_spell_writer import patch_raw
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
f=sp.f;cf=sp.cf;ROOT=Path(__file__).resolve().parent
SPELL=0x10500000;RAW210=0x10510000;MID=0x10520000;RAWSPELL=0x10530000;DRIVER=0x10540000;SOURCE=0x10550000

def cases():
    base=dict(state='possessed',percent=25,scalar=100,base_ticks=20,last_use=100,tick=110,denominator=4096,slot_query=0,ability_count=1,ability_id=1,scalars=[6,6,6])
    changes=[('possessed',{}),('normal',dict(state='normal')),('secondary',dict(state='secondary')),('both-flags',dict(state='both')),
      ('equal-deadline',dict(tick=125)),('after-deadline',dict(tick=126)),('normal-equal',dict(state='normal',tick=120)),
      ('normal-before',dict(state='normal',tick=119)),('last-use-zero',dict(last_use=0,tick=0)),
      ('tick-before-last',dict(tick=99)),('percent0',dict(percent=0)),('percent100',dict(percent=100)),
      ('percent-minus1',dict(percent=0xffffffff)),('percent-highbits',dict(percent=0xabcd0019)),
      ('scalar0',dict(scalar=0)),('scalar50',dict(scalar=50)),('scalar256',dict(scalar=256)),('scalar255',dict(scalar=255)),
      ('scalar-highbits',dict(scalar=0xabcd0064)),('secondary-scalar0',dict(state='secondary',scalar=0)),
      ('denominator-double',dict(state='normal',denominator=8192,tick=111)),
      ('denominator-double-equal',dict(state='normal',denominator=8192,tick=110)),
      ('slot-low8',dict(slot_query=256)),('slot-outside',dict(slot_query=1)),('no-abilities',dict(ability_count=0)),('zero-id',dict(ability_id=0)),
      ('scalar98-113-114-high',dict(scalars=[30000,30000,30000])),('scalar98-113-114-zero',dict(scalars=[0,0,0])),
      ('current-records',dict(source_current=True)),
      ('rawspell20',dict(raw_recharge=4096,tps=20)),('rawspell25',dict(raw_recharge=4096,tps=25)),
      ('rawspell-fractional',dict(raw_recharge=6144,tps=20)),('rawspell-wrap',dict(raw_recharge=0xffffffff,tps=20)),
      ('rawspell-current',dict(raw_recharge=4096,tps=20,current_spell=True)),
      ('base-zero',dict(base_ticks=0)),('base-shift-wrap',dict(base_ticks=0x100000)),
      ('spell-id7',dict(spell_id=7,ability_id=7)),('tick-wrap',dict(last_use=0xfffffff0,tick=10))]
    return [dict(base,**v,name=n) for n,v in changes]

def spell_raw(c):
    raw=bytes(266)
    if c.get('current_spell'):
        doc=json.loads((ROOT/'decoded/CreatureSpells.kwd.json').read_text(encoding='utf8'));data=Path(doc['source']).read_bytes();assert hashlib.sha256(data).hexdigest()==doc['sha256']
        row=doc['sections'][0]['records'][0];raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+len(raw)]==raw
    else:
        b=bytearray(raw);b[0xe6]=1;raw=bytes(b)
    return patch_raw(raw,0xea,c['raw_recharge'])

def expected(c):
    base=((c['raw_recharge']*c['tps'])&0xffffffff)>>12 if 'raw_recharge' in c else c['base_ticks']
    adjusted=api.interval(base,c['percent'],c['scalar'],possessed=c['state'] in ['possessed','both'],secondary=c['state'] in ['secondary','both'])
    e=dict(base_ticks=base,interval=adjusted)
    present=(c['slot_query']&255)<c['ability_count'] and c['ability_id']!=0
    e.update(api.readiness_progress(adjusted,c['last_use'],c['tick'],c['denominator'],ability_present=present));return e

def build(c):
    e=expected(c);args,old=sp.build(dict(sp.cases()[0],possessed=False,raw_index=31,percent=c['percent'],scalars=c['scalars']))
    allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a not in [f.OBS,sp.DRIVER]}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    # Actual raw224callback is still the emulation entrypoint.
    if c.get('source_current'):
        doc=json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'));data=Path(doc['source']).read_bytes();assert hashlib.sha256(data).hexdigest()==doc['sha256']
        selected={r['words_u32'][0]:r for r in doc['sections'][0]['records'] if r['words_u32'][0]==210 or r['words_u32'][:2]==[224,31]}
        for row in selected.values():assert data[row['offset']:row['offset']+16]==bytes.fromhex(row['raw_hex'])
        memory[sp.RAW]=bytearray(bytes(16)+bytes.fromhex(selected[224]['raw_hex']));checks[sp.RAW]=bytearray(memory[sp.RAW]);scalar_raw=bytes.fromhex(selected[210]['raw_hex'])
        assert selected[224]['words_u32']==[224,31,25,10] and selected[210]['words_u32']==[210,100,0,0]
    else:scalar_raw=struct.pack('<4I',210,c['scalar'],0x11223344,0x55667788)
    memory[RAW210]=bytearray(bytes(16)+scalar_raw);checks[RAW210]=bytearray(memory[RAW210]);f.put(checks[sp.VARS],0x6f29b0-sp.VARS,c['scalar'],1)
    both(cf.ACTOR,0x41b,c['last_use']);both(cf.ACTOR,0x3af,c['denominator']);both(cf.ACTOR,0x3b3,0);both(f.SESSION,4,c['tick'])
    both(cf.DEFINITION,0x789,c['ability_count'],1);both(cf.DEFINITION,0x5e1,c['ability_id'],1)
    # Skip only lazyCRTfirstregistration,with its initialized zero fallback prepared.
    memory[0x6e4168]=bytearray(pack32(1));checks[0x6e4168]=bytearray(pack32(1));memory[0x6e4178]=bytearray(19);checks[0x6e4178]=bytearray(19)
    definition=bytearray(70);f.put(definition,0x2d,e['base_ticks']);definition[0x43]=c.get('spell_id',1)
    if 'raw_recharge' in c:
        raw=spell_raw(c);mid=full_intermediate(raw,0);definition=bytearray(runtime_projection(mid,bytes(70),c['tps'],checksum_gate=1))
        for a,actual,wanted in [(RAWSPELL,raw,raw),(MID,bytes([0xa5])*346,mid)]:memory[a]=bytearray(actual)+bytearray([0xa5])*64;checks[a]=bytearray(wanted)+bytearray([0xa5])*64
        memory[0x6fcdcf]=bytearray(pack32(1));checks[0x6fcdcf]=bytearray(pack32(1));both(sp.VARS,0x6f2590-sp.VARS,c['tps'])
        memory[SPELL]=bytearray(70)+bytearray([0xa5])*64;checks[SPELL]=definition+bytearray([0xa5])*64
        # Current firstrawspell has itsown ID;matchabilityslot without normalizingraw.
        both(cf.DEFINITION,0x5e1,raw[0xe6] if c['ability_id'] else 0,1)
    else:memory[SPELL]=definition+bytearray([0xa5])*64;checks[SPELL]=bytearray(memory[SPELL])
    both(f.WORLD,0x1614,SPELL);both(f.WORLD,0x161c,1)
    source=bytearray(memory[cf.ACTOR]);f.put(source,4,101,2);f.put(source,0x10,100,2);f.put(source,0x1a,0x5900)
    memory[SOURCE]=source;checks[SOURCE]=bytearray(source);both(cf.ACTOR,0x10,0,2);both(f.PLAYER,6,101,2)
    for b in [memory[0x6ee540],checks[0x6ee540]]:f.put(b,101*4,SOURCE)
    for b in [memory[0x6ed540],checks[0x6ed540]]:b[101]=1
    memory[f.OUT]=bytearray(16)+bytearray([0xa5])*16;checks[f.OUT]=bytearray(struct.pack('<4I',1,e['interval'],e['ready'],e['progress']))+bytearray([0xa5])*16
    driver=bytearray(b'\xa3'+pack32(f.OUT))
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(DRIVER+len(driver)+5)))
    def possess(actor):
        emit(b'\x68'+pack32(1)+b'\xb9'+pack32(actor));call(0x48dea0)
        flags=int.from_bytes(checks[actor][0x3e6:0x3ea],'little');f.put(checks[actor],0x3e6,flags|0x80);f.put(checks[actor],0x488,0);f.put(checks[actor],0x3ee,0)
    emit(b'\x68'+pack32(RAW210)+b'\xb9'+pack32(f.WORLD));call(0x51cd90)
    if 'raw_recharge' in c:
        emit(b'\xb9'+pack32(MID));call(0x56a490)
        emit(b'\x68'+pack32(RAWSPELL)+b'\xb9'+pack32(MID));call(0x56a560)
        emit(b'\x68'+pack32(0)+b'\x68'+pack32(MID)+b'\xb9'+pack32(SPELL));call(0x4f7860)
    if c['state'] in ['secondary','both']:
        possess(SOURCE);emit(b'\x68'+pack32(SOURCE)+b'\xb9'+pack32(f.PLAYER));call(0x4bfd00)
        flags=int.from_bytes(checks[cf.ACTOR][0x3ea:0x3ee],'little');f.put(checks[cf.ACTOR],0x3ea,flags|8)
    if c['state'] in ['possessed','both']:possess(cf.ACTOR)
    emit(b'\x68'+pack32(cf.ACTOR)+b'\xb9'+pack32(SPELL));call(0x4f7ab0);emit(b'\xa3'+pack32(f.OUT+4))
    for target,offset in [(0x48c800,8),(0x48c8b0,12)]:
        emit(b'\x68'+pack32(c['slot_query'])+b'\x68'+pack32(cf.DEFINITION)+b'\xb9'+pack32(cf.ACTOR));call(target);emit(b'\xa3'+pack32(f.OUT+offset))
    emit(b'\x68'+pack32(f.OBS)+b'\xc3');memory[DRIVER]=driver;f.put(memory[0x7ffee000],8192,DRIVER)
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=mem));e['checks']=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()];return args,e

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);path=ROOT/'ghidra'/f'phase96-possession-recharge-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['interval']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'interval',e['interval'],'ready',e['ready'],'progress',e['progress'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase96-possession-recharge-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
