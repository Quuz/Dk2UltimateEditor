"""Phase190: full original CPlayer writer includes the20 Crypt state bytes.

Original bare CPlayer constructor, real MyFile/Disc and external OS WriteFile
memory sink. Runtime counter values/type are explicit fixture data; no live
World/player initialisation or KWD/savegame envelope is claimed.
"""
from pathlib import Path
import json, struct
import native_unicorn_mapreader as cpu
import native_file_api_fixture as win
import native_file_write_api_fixture as write
import probe_crypt_action_completion as profile
from emulate_element_colors import region
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
EXE,p,sha=profile.EXE,profile.p,profile.sha
OWNER,FILE,STREAM,REF,DATA,STATE,LOG,CONTROL=[0x31000000+i*0x10000 for i in range(8)]
# The131136-byte output region must not overlap the20-byte IO state or log.
# Older exploratory traces at31040000 fail the full guard check and are not
# accepted evidence. Keep them on disk; the new input/cache key is distinct.
DATA=0x31400000
READCODE,SEEKCODE,WRITECODE,READSTATE,READLOG=[0x31100000+i*0x10000 for i in range(5)]
ARRAY,BEFORE,STOP=0x31200000,0x31210000,0x31900000
SIZE,CAPACITY=0x1125,0x20000
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
put=lambda b,o,v,n=4:b.__setitem__(slice(o,o+n),(v&((1<<(8*n))-1)).to_bytes(n,'little'))


def cases():
    return [dict(name='zero',counters=bytes(20).hex()),
            dict(name='marker',counters=bytes(range(1,21)).hex()),
            dict(name='signed-bits',counters=bytes([0,1,127,128,255]*4).hex()),
            dict(name='one-list-entry',counters=bytes(range(20)).hex(),one_entry=True),
            dict(name='short-17',counters=bytes(range(1,21)).hex(),limit=17),
            dict(name='write-fails',counters=bytes(range(1,21)).hex(),fail=True)]


def build(c):
    blob=EXE.read_bytes(); owner=bytearray(SIZE)+bytearray([165])*64
    owner[0x108A:0x109E]=bytes.fromhex(c['counters'])
    # Type1 avoids any claim about a normally loaded player type; optional
    # controllers remain absent in this bare-constructor fixture.
    owner[0x10FC]=1
    stream=bytearray(32);put(stream,0,0x672FE8);put(stream,12,win.HANDLE)
    mem={OWNER:owner,FILE:bytearray([165])*72,STREAM:stream+bytearray([165])*64,
         REF:bytearray(win.d(FILE)),BEFORE:bytearray([165])*(SIZE+64),DATA:bytearray([165])*(CAPACITY+64),STATE:bytearray(20),LOG:bytearray([165])*(64*20),
         READSTATE:bytearray(20),READLOG:bytearray([165])*(64*20),ARRAY:bytearray([0xA6])+bytearray(struct.pack('<II',0x11223344,0x55667788)),
         0:bytearray(4),0x7FFEE000:bytearray([165])*0x4000,0x6A4818:bytearray(struct.pack('<3I',4094,1,0)),
         0x6F2540:bytearray(4),0x6EE540:bytearray(4096*4),0x6ED540:bytearray(4096)}
    adapters=win.build(blob,source=DATA,size=CAPACITY,state=READSTATE,log=READLOG,read_code=READCODE,seek_code=SEEKCODE,ref_address=STREAM+8)
    adapters.update(write.build(blob,p.pe_read,owner=OWNER,owner_size=SIZE,data=DATA,capacity=CAPACITY,state=STATE,log=LOG,
                               code_address=WRITECODE,limit=c.get('limit'),fail=c.get('fail',False),extra_sources=[(ARRAY,9)] if c.get('one_entry') else []))
    for address,code in adapters.items():mem[address]=bytearray(code)
    calls=[dict(address='0x4b8450',registers=dict(ECX=hex(OWNER)),stack_args=[]),
           dict(address='0x5b6e40',registers=dict(ECX=hex(FILE)),stack_args=[]),
           dict(address='0x5b6f20',registers=dict(ECX=hex(FILE)),stack_args=[STREAM])]
    if c.get('one_entry'):
        code=bytearray(b'\xc7\x05'+struct.pack('<II',OWNER+0x56,1)+b'\xc7\x05'+struct.pack('<II',OWNER+0x5A,ARRAY)+b'\xc3')
        mem[CONTROL]=code;adapters[CONTROL]=code
        calls.append(dict(address=hex(CONTROL),registers=dict(ECX='0'),stack_args=[]))
    # Observation copy after native constructor and explicit list condition,
    # before the writer; it cannot alter the original source object.
    snap=CONTROL+256
    code=bytes.fromhex('9c60fc')+b'\xbe'+win.d(OWNER)+b'\xbf'+win.d(BEFORE)+b'\xb9'+win.d(SIZE+64)+bytes.fromhex('f3a4619dc3')
    mem[snap]=bytearray(code);adapters[snap]=code
    calls.append(dict(address=hex(snap),registers=dict(ECX='0'),stack_args=[]))
    args=dict(program='DKII-DX.exe',address='0x4b8d40',registers=json.dumps(dict(ECX=hex(OWNER))),stack_args=[REF],before_calls=calls,
              memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=500000,
              allowed_external_IO_code=[[a,len(b)] for a,b in adapters.items()],
              observe_reads=[[OWNER,len(owner)],[ARRAY,len(mem[ARRAY])],[0,4096]],
              observe_writes=[[OWNER,len(owner)],[STATE,20],[LOG,len(mem[LOG])],[0,4096]])
    excluded=set(adapters)|{0x7FFEE000}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D99C,12),(0x66E3AC,8),(0x66E3DC,8),(0x671F80,36),(0x672FE8,48),(0x66C000,0x420)]:checks[a]=p.pe_read(blob,a,n)
    spans=sorted((a,len(b)) for a,b in mem.items())
    assert all(a+n<=b for (a,n),(b,m) in zip(spans,spans[1:])),spans
    return args,checks


def execute(c):
    args,ch=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha(Path(cpu.__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase190-crypt-state-writer-{c["name"]}-{tag}.json'
    if file.exists():s=json.loads(file.read_text(encoding='utf-8'));assert s['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),STOP,OWNER,len(ch[OWNER]))
        s=dict(case=c,input=args,result=r);file.write_text(json.dumps(s,indent=2),encoding='utf-8')
    return args,ch,s,file


def describe(c):
    args,ch,s,file=execute(c);r=s['result'];post=post_context(ch,r)
    rows=[dict(position=u(post[LOG],20*i),source=hex(u(post[LOG],20*i+4)),request=u(post[LOG],20*i+8),written=u(post[LOG],20*i+12)) for i in range(u(post[STATE],4))]
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],failures=r['protected_write_or_execution_failures'],EAX=r['registers']['EAX'],
                         position=u(post[STATE],0),unexpected=u(post[STATE],8),counter_bytes=post[DATA][0x1017:0x102B].hex(),writes=rows,
                         tail=r['trace_tail'][-12:],file=str(file.relative_to(ROOT)))),flush=True)


if __name__=='__main__':
    for c in cases():describe(c)
