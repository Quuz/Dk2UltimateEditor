"""Whole56AC80 reader -> native MyFile/Disc -> World Door callbacks -> lookup.

Prepared World/array storage with original per-slot constructors, null visual
provider and declared sound lookup0. Original Windows imports use bounded
memory-backed external adapters, unchanged IAT/vtables. No wholeWorld bootstrap.
"""
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import json,struct,sys
import dk2_formats as fmt
import native_unicorn_mapreader as cpu
import native_file_api_fixture as win
import native_door_loading as layout
from build_effect_semantics import EXE,pe_read
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
WORLD,ARRAY,REF,FILE,STREAM,CFG,DATA=[0x18000000+i*0x10000 for i in range(7)]
STATE,LOG,READCODE,SEEKCODE,SOUND,SV,SCB,SIO,SEEDRAW,SEEDMID=[0x18100000+i*0x10000 for i in range(10)]
BUFFER,MID,STOP=0x746EC0,0x747128,0x18900000
SLOTS=12
sha=cpu.sha
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')

def cases():
    return [dict(name='source'),dict(name='source-two-pass',two_pass=True,header_callback=False),dict(name='source-appended',append=True),dict(name='source-opaque',opaque_source=True),dict(name='reverse-source',reverse=True),
        dict(name='unsorted',ids=[4,1,3,2],query=3),dict(name='duplicate-low',ids=[1,1,2,3],query=1),
        dict(name='duplicate-high',ids=[4,4,1,2],query=4),dict(name='missing-id',ids=[1,3],query=2),
        dict(name='zero-id',ids=[0,2],query=0),dict(name='query-highword',ids=[1,2],query=0x101),
        dict(name='capacity-two',ids=[3,1,2,4],capacity=2,header_callback=False,query=3),
        dict(name='update-existing',ids=[3,1],existing=[1,3],update=True,header_callback=False,query=3),
        dict(name='gate1',gate=1),dict(name='short-stride',stride=600),dict(name='size-remainder',remainder=1),
        dict(name='truncated-tail',ids=[1,2],truncate=60,query=2),dict(name='wrong-header',header_tag=140),
        dict(name='wrong-data',data_tag=143),dict(name='zero-count',ids=[],expected_divzero=True),
        dict(name='zero-count-skip',ids=[],data_callback=False)]

def source_records(c):
    records=fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Doors.kwd').read_bytes())[0].records
    if 'ids' in c:
        rows=[]
        for i,ident in enumerate(c['ids']):
            raw=bytearray(next((r for r in records if r[0x244]==ident),records[0]));raw[0x244]=ident
            put(raw,0x236,0x1100+i,2);rows.append(bytes(raw))
        return rows
    rows=list(reversed(records)) if c.get('reverse') else list(records)
    if c.get('append'):
        raw=bytearray(rows[0]);raw[0x244]=9;put(raw,0x236,0xBEEF,2);rows.append(bytes(raw))
    if c.get('opaque_source'):
        raw=bytearray(rows[-1]);put(raw,0x236,0xBEEF,2);rows[-1]=bytes(raw)
    return rows

def raw_file(c):
    original=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Doors.kwd').read_bytes();head=bytearray(original[:56])
    rows=source_records(c);stride=c.get('stride',616);payload=b''.join(r[:stride] for r in rows)+bytes([0xA7])*c.get('remainder',0)
    put(head,8,56+len(payload));put(head,12,c.get('header_tag',141));put(head,20,len(rows));put(head,48,c.get('data_tag',142));put(head,52,len(payload))
    data=bytes(head)+payload
    return data[:-c['truncate']] if c.get('truncate') else data

def ctor_definition():
    # Full504D60 clears each27-byte resource block;4A7040 leaves checksum87
    # and WORD9D untouched but clears all other scalar destinations below.
    d=bytearray([165])*181;d[:0x87]=bytes(0x87);d[0x8B:0x9D]=bytes(0x12);d[0x9F:]=bytes(181-0x9F)
    return bytes(d)

def lookup_index(rows,ident):
    ident&=255;i=min(ident-1,len(rows)-1)
    while i>=0:
        found=rows[i][0xAD]
        if found==ident:return i
        if found<ident:return None
        i-=1
    return None

def build(c):
    data=raw_file(c);world=bytearray(0xA400);put(world,0,0x66E3EC);put(world,0x15D4,ARRAY)
    existing=c.get('existing',[]);capacity=c.get('capacity',len(existing) if existing else len(source_records(c)))
    put(world,0x15D8,0 if c.get('header_callback',True) or c.get('two_pass') else capacity);put(world,0x15DC,len(existing));put(world,0xA3C7,int(c.get('update',False)))
    if existing:put(world,0x15E0,0x55555555)
    stream=bytearray(32);put(stream,0,0x672FE8);put(stream,12,win.HANDLE);put(stream,16,12)
    cfg=bytearray(0x38);put(cfg,0x2C,0x51FF10 if c.get('header_callback',True) else 0);put(cfg,0x30,0x51FF30 if c.get('data_callback',True) else 0);put(cfg,0x34,WORLD)
    sv=bytearray(32);put(sv,0x1C,SCB);scb=bytes.fromhex('ff05')+win.d(SIO)+bytes.fromhex('33c0c20800')
    mem={WORLD:world,ARRAY:bytearray([165])*(181*SLOTS+64),REF:bytearray(win.d(FILE)),FILE:bytearray([165])*72,
        STREAM:stream+bytes([165])*64,CFG:cfg,DATA:bytearray(data)+bytes([165])*64,
        STATE:bytearray(struct.pack('<5I',12,0,0,0,0)),LOG:bytearray([165])*(64*20),BUFFER:bytearray([165])*(616+732+64),
        SOUND:bytearray(win.d(SV)),SV:sv,SCB:bytearray(scb),SIO:bytearray(4),0x75B468:bytearray(win.d(SOUND)),0x740C20:bytearray([165])*64,
        0x6FCDCF:bytearray(win.d(c.get('gate',0))),0:bytearray(4),0x7FFEE000:bytearray([165])*0x4000}
    adapters=win.build(EXE.read_bytes(),source=DATA,size=len(data),state=STATE,log=LOG,read_code=READCODE,seek_code=SEEKCODE,ref_address=STREAM+8)
    for a,b in adapters.items():mem[a]=bytearray(b)
    calls=[dict(address='0x4a7040',registers=dict(ECX=hex(ARRAY+181*i)),stack_args=[]) for i in range(SLOTS)]
    for i,ident in enumerate(existing):
        raw=bytearray(source_records({})[ident-1]);put(raw,0x236,0x7700+i,2);mem[SEEDRAW+4096*i]=raw+bytes([165])*64
        if SEEDMID not in mem:mem[SEEDMID]=bytearray([165])*796
        calls += [dict(address='0x56a920',registers=dict(ECX=hex(SEEDMID)),stack_args=[]),dict(address='0x56aa40',registers=dict(ECX=hex(SEEDMID)),stack_args=[SEEDRAW+4096*i]),
            dict(address='0x4a7100',registers=dict(ECX=hex(ARRAY+181*i)),stack_args=[SEEDMID,0])]
    file_calls=[dict(address='0x5b6e40',registers=dict(ECX=hex(FILE)),stack_args=[]),dict(address='0x5b6f20',registers=dict(ECX=hex(FILE)),stack_args=[STREAM])]
    if c.get('two_pass'):
        assert not existing
        first=bytearray(cfg);put(first,0x2C,0x51FF10);put(first,0x30,0);mem[CFG+4096]=first
        calls=file_calls+[dict(address='0x56ac80',registers=dict(ECX='0'),stack_args=[REF,CFG+4096]),
            dict(address='0x5b3a90',registers=dict(ECX='0'),stack_args=[FILE,12,0])]+calls
    else:calls+=file_calls
    calls.append(dict(address='0x56ac80',registers=dict(ECX='0'),stack_args=[REF,CFG]))
    args=dict(program='DKII-DX.exe',address='0x50d650',registers=json.dumps(dict(ECX=hex(WORLD))),stack_args=[c.get('query',8)],before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=1500000,
        allowed_external_IO_code=[[a,len(b)] for a,b in adapters.items()]+[[SCB,len(scb)]],
        observe_reads=[[ARRAY,181*SLOTS],[BUFFER,1348],[DATA,len(data)],[0,4096]],
        observe_writes=[[ARRAY,181*SLOTS+64],[WORLD+0x15D4,16],[BUFFER,1412],[0,4096]])
    excluded=set(adapters)|{SCB,0x7FFEE000};checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66E3EC,0x3A0),(0x671F80,0x24),(0x672FE8,0x30),(win.READ_IAT,8),(win.REF_IAT,4)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks

def execute(c):
    args,checks=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend+json.dumps({hex(a):sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase160-native-door-reader-{c["name"]}-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['backend_sha256']==backend
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),STOP,WORLD+0x15D4,16));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,checks,saved,path

def verify(c,args,checks,r):
    assert not r['protected_write_or_execution_failures']
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);assert 0x400000<=pc<0x670000,x
        ins=next(md.disasm(pe_read(blob,pc,15),pc));assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    post=post_context(checks,r);divzero=c.get('expected_divzero',False)
    assert all(x['returned'] for x in r['before_calls'][:-1])
    if divzero:
        assert not r['success'] and not r['hit_return'] and 'EXCEPTION' in r['emulation_error'] and int(r['registers']['EIP'],16)==0x56ADAE and int(r['registers']['ECX'],16)==0
    else:assert r['success'] and r['hit_return'] and all(x['returned'] for x in r['before_calls']),(c['name'],r['emulation_error'],r['trace_tail'])
    result=0 if c.get('header_tag',141)!=141 or c.get('data_tag',142)!=142 else 1
    if not divzero:assert int(r['before_calls'][-1]['return_EAX'],16)==result
    records=[];initial=ctor_definition();gate=c.get('gate',0);sound_calls=0
    for i,ident in enumerate(c.get('existing',[])):
        raw=bytearray(source_records({})[ident-1]);put(raw,0x236,0x7700+i,2);mid=layout.intermediate(bytes(raw));records.append(layout.definition(mid,initial,layout.DX,gate,0));sound_calls+=int(not gate)
    expected_buffer=bytearray(checks[BUFFER]);expected_capacity=u(checks[WORLD],0x15D8);checksum=u(checks[WORLD],0x15E0)
    n=len(source_records(c));pos=48;shorts=0;requests=([36,8] if c.get('two_pass') else [])+[36];processed=0
    if c.get('two_pass'):expected_capacity+=n
    if c.get('header_tag',141)==141:
        if c.get('header_callback',True):expected_capacity+=n
        requests.append(8);pos=56
        if result and not divzero:
            if not c.get('data_callback',True):pos+=u(checks[DATA],52)
            else:
                stride=u(checks[DATA],52)//n
                for i in range(n):
                    requests.append(stride);expected_buffer[616:1348]=bytes(732)
                    if stride!=616:expected_buffer[:616]=bytes(616)
                    chunk=checks[DATA][:len(raw_file(c))][pos:pos+stride];pos+=len(chunk);shorts+=int(len(chunk)<stride)
                    expected_buffer[:len(chunk)]=chunk;raw=bytes(expected_buffer[:616]);mid=layout.intermediate(raw);expected_buffer[616:1348]=mid;processed+=1
                    if c.get('update'):
                        index=lookup_index(records,raw[0x244]);assert index is not None
                    elif len(records)>=expected_capacity:continue
                    else:
                        index=0
                        while index<len(records) and records[index][0xAD]<raw[0x244]:index+=1
                        # Whole181-byte move copies original next-slot bytes;
                        # new slot inherits the displaced definition before load.
                        previous=records[index] if index<len(records) else initial
                        records.insert(index,previous)
                    records[index]=layout.definition(mid,records[index],layout.DX,gate,0);sound_calls+=int(not gate)
                    if not c.get('update'):checksum=(checksum+u(records[index],0x87))&0xFFFFFFFF
    expected_array=bytearray(checks[ARRAY]);expected_array[:181*SLOTS]=initial*SLOTS
    for i,d in enumerate(records):expected_array[181*i:181*(i+1)]=d
    assert post[ARRAY]==expected_array,(c['name'],'array',[(i,a,b) for i,(a,b) in enumerate(zip(post[ARRAY],expected_array)) if a!=b][:20])
    expected_world=bytearray(checks[WORLD]);put(expected_world,0x15D8,expected_capacity);put(expected_world,0x15DC,len(records));put(expected_world,0x15E0,checksum)
    assert post[WORLD]==expected_world and post[BUFFER]==expected_buffer,(c['name'],'world/buffer')
    assert post[DATA]==checks[DATA] and u(post[SIO],0)==sound_calls
    seeks=2 if c.get('two_pass') else int(not c.get('data_callback',True) and result==1 and not divzero)
    state=list(struct.unpack('<5I',post[STATE]));assert state==[pos,len(requests),shorts,0,seeks],(c['name'],state,pos,requests)
    logs=[struct.unpack_from('<5I',post[LOG],20*i) for i in range(len(requests))]
    assert [x[2] for x in logs]==requests and all(x[4]==1 for x in logs)
    assert post[LOG][20*len(requests):]==checks[LOG][20*len(requests):]
    index=lookup_index(records,c.get('query',8));pointer=0 if index is None else ARRAY+181*index
    if not divzero:assert int(r['registers']['EAX'],16)==pointer,(c['name'],index,r['registers'])
    assert r['visited_instructions'].get('0x51ff30',0)==processed
    return post,dict(reader_return=None if divzero else result,expected_divide_error=bool(divzero),processed_records=processed,catalogue_records=len(records),capacity=expected_capacity,
        sorted_runtime_IDs=[d[0xAD] for d in records],runtime_unknown_WORDs=[u(d,0x99,2) for d in records],World_checksum=checksum,
        lookup_query=c.get('query',8),lookup_slot=index,lookup_pointer=pointer,stream_position=pos,read_requests=requests,short_reads=shorts,
        original_callbacks=processed,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_opaque_pair(a,b):
    assert a.keys()==b.keys();allowed={DATA:[(56+7*616+0x236,2)],BUFFER:[(0x236,2),(616+0x2AA,2)],
        ARRAY:[(7*181+0x99,2),(7*181+0x87,4)],WORLD:[(0x15E0,4)]};diffs=[]
    for address,left in a.items():
        right=b[address];assert len(left)==len(right)
        indices=[i for i,(x,y) in enumerate(zip(left,right)) if x!=y]
        assert all(any(off<=i<off+n for off,n in allowed.get(address,[])) for i in indices),(hex(address),indices[:30])
        if indices:diffs.append(dict(region=hex(address),changed_byte_offsets=indices))
    assert {int(d['region'],16) for d in diffs}==set(allowed)
    return dict(cases=['source','source-opaque'],compared_regions=len(a),compared_bytes=sum(map(len,a.values())),
        allowed_storage_differences={hex(k):[list(s) for s in v] for k,v in allowed.items()},actual_differences=diffs,
        catalogue_IDs_lookup_and_file_IO_identical=True,scope='WORD236 remains stored/checksummed; insertion may copy whole definitions. No new gameplay meaning/nonuse proof.')

def main():
    rows=[];posts={}
    for c in cases():
        args,ch,saved,path=execute(c);post,out=verify(c,args,ch,saved['result'])
        posts[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())));print(c['name'],out['sorted_runtime_IDs'],out['runtime_unknown_WORDs'],'verified',flush=True)
    pair=compare_opaque_pair(posts['source'],posts['source-opaque'])
    assert posts['source'][ARRAY]==posts['reverse-source'][ARRAY]==posts['source-two-pass'][ARRAY]
    (ROOT/'phase160-native-door-reader-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=[pair],source_reverse_runtime_array_equal=True),indent=2),encoding='utf8')
if __name__=='__main__':main()
