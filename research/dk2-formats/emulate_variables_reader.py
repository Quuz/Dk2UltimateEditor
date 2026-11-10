"""Original outer dispatch/Variables reader with original MyFile/Disc I/O.

Runtime scenes use the measured output of the constructor registration segment;
the whole World constructor and natural reference traversal are not executed.
Capture callbacks and OS imports are declared external fixture code only.
"""
from pathlib import Path
import json,struct,sys
import emulate_tick_rate as prior
import native_unicorn_mapreader as cpu
import native_variables_file_api as win
import dk2_formats as fmt
from emulate_element_colors import region
from emulate_resistance import post_context
from probe_melee_animation_flag import coalesce_world
ROOT,EXE,sha,load,dump=prior.ROOT,prior.EXE,prior.sha,prior.load,prior.dump
WORLD=prior.fixture.p.f.WORLD
REF,FILE,STREAM,CFG,DATA=0x1E600000,0x1E610000,0x1E620000,0x1E630000,0x1E640000
STATE,LOG,READCODE,SEEKCODE=0x1E650000,0x1E660000,0x1E670000,0x1E680000
CAPSTATE,CAPTURE,CALLBACK,HEADER,OUT=0x1E690000,0x1E6A0000,0x1E6B0000,0x1E6C0000,0x1E6D0000
STOP=0x1E6E0000
GLOBAL=ROOT.parents[1]/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
TABLE_BYTES=243*16


def registration():
    """Execute only 509AEF..509B3A, after actual static initializer 51AA90."""
    args=dict(program='DKII-DX.exe',address='0x509aef',registers=json.dumps(dict(ESI=hex(WORLD),EBX='0')),
        before_calls=[dict(address='0x51aa90',registers=dict(ECX='0'),stack_args=[])],max_steps=100000,
        memory=json.dumps(dict(regions=[region(WORLD,bytes([165])*0xA3D0),region(0x7FFEE000,bytes([165])*0x4000)])),
        observe_reads=[[prior.TABLE,0x1064]],observe_writes=[[WORLD+0x1F24,TABLE_BYTES]])
    checks={WORLD:bytes([165])*0xA3D0,prior.TABLE:prior.fixture.p.pe_read(EXE.read_bytes(),prior.TABLE,0x1064)}
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase197-registration-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        saved=dict(input=args,result=cpu.run(EXE,args,list(checks.items()),0x509B3A,WORLD,0xA3D0));dump(str(file.relative_to(ROOT)),saved)
    r=saved['result'];assert r['success'] and not r['protected_write_or_execution_failures']
    post=post_context(checks,r);table=post[WORLD][0x1F24:0x1F24+TABLE_BYTES]
    want=bytearray(TABLE_BYTES);rows=[]
    for off in range(0,0x1064,20):
        tag=int.from_bytes(post[prior.TABLE][off:off+4],'little')
        if tag==0:break
        assert tag<243;want[tag*16:tag*16+16]=post[prior.TABLE][off+4:off+20];rows.append(tag)
    assert table==want
    assert post[WORLD][:0x1F24]==checks[WORLD][:0x1F24] and post[WORLD][0x1F24+TABLE_BYTES:]==checks[WORLD][0x1F24+TABLE_BYTES:]
    assert int.from_bytes(table[19*16:19*16+4],'little')==0x51CD90
    return table,dict(file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),entry=0x509AEF,stop_before=0x509B3A,
        bytes=TABLE_BYTES,registered_rows=len(rows),native_steps=r['instruction_count'],scope='Isolated constructor segment, not whole constructor; measured table becomes explicit input to later scenes.')


def rec(tag,value,extra1=0x11223344,extra2=0x55667788):return [tag,value&0xFFFFFFFF,extra1,extra2]


def cases():
    base=dict(profile='capture',records=[rec(74,340),rec(19,30,0xA1B2C3D4,0x12345678)],truncate=0,declared=None,
        count=None,header_tag=231,data_tag=232,header_size=28,callback=True,reject=False,header_reject=False,initial_tps=20)
    rows=[('source-common',dict(source=str(GLOBAL))),('source-autosave',dict(source=str(GLOBAL.parent/'AutoSaveVariables.kld'))),
        ('declared-zero',dict(declared=0)),('header-size-unchecked',dict(header_size=0xA5A5A5A5)),
        ('wrong-header',dict(header_tag=230)),('wrong-data',dict(data_tag=233)),('count-zero',dict(count=0)),
        ('skip',dict(callback=False)),('skip-beyond-EOF',dict(callback=False,declared=100000)),
        ('header-reject',dict(header_reject=True)),('record-reject',dict(reject=True)),
        ('short-last-DWORD',dict(truncate=4)),('short-last-value',dict(truncate=12)),('missing-last-record',dict(truncate=16)),
        ('count-above-data',dict(count=3)),
        ('runtime-TPS-first',dict(profile='runtime',records=[rec(19,30),rec(4,101)])),
        ('runtime-TPS-last',dict(profile='runtime',records=[rec(4,101),rec(19,30)])),
        ('runtime-reapply',dict(profile='runtime',records=[rec(4,101),rec(19,30),rec(4,101)])),
        ('runtime-negative',dict(profile='runtime',records=[rec(19,20),rec(4,-101)])),
        ('runtime-174-bypass',dict(profile='runtime',records=[rec(19,0),rec(174,0xF1234567),rec(19,20)])),
        ('runtime-scaled-family',dict(profile='runtime',records=[rec(19,20)]+[rec(t,101+2*i) for i,t in enumerate(list(range(4,17))+[136,175,176])])),
        ('runtime-unknown29-186',dict(profile='runtime',records=[rec(29,0x12345678),rec(186,0x89ABCDEF)])),
        ('runtime-unregistered',dict(profile='runtime',records=[rec(0,0xDEADBEEF),rec(243,0xAABBCCDD),rec(19,30)]))]
    return [dict(base,**v,name=n) for n,v in rows]


def raw_file(c):
    if c.get('source'):return Path(c['source']).read_bytes()
    body=b''.join(struct.pack('<4I',*r) for r in c['records']);head=bytearray(GLOBAL.read_bytes()[:56])
    struct.pack_into('<I',head,8,len(body)+56);struct.pack_into('<II',head,12,c['header_tag'],c['header_size'])
    struct.pack_into('<I',head,20,len(c['records']) if c['count'] is None else c['count'])
    struct.pack_into('<II',head,48,c['data_tag'],len(body) if c['declared'] is None else c['declared'])
    return bytes(head)+body[:len(body)-c['truncate']]


def capture_callback(reject):
    a=win.Asm();a.emit(bytes.fromhex('535657a1')+win.d(CAPSTATE)+bytes.fromhex('3d00040000'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('c1e0058db8')+win.d(CAPTURE)+bytes.fromhex('8b742410b908000000f3a5ff05')+win.d(CAPSTATE))
    a.emit(b'\xB8'+win.d(0 if reject else 1)+bytes.fromhex('5f5e5bc3'))
    a.label('bad');a.emit(bytes.fromhex('31c05f5e5bc3'));return a.finish()


def build(c):
    table,reg=registration()
    if c['profile']=='runtime':
        args,_=prior.build(dict(tps=20,duration=5))
        mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};coalesce_world(mem)
        args['before_calls']=[x for x in args['before_calls'] if x['address']!=hex(prior.DISPATCH)]
        collision=args['before_calls'].pop();assert collision['address']=='0x4abd60'
        # Native reader's output, not direct Type19 fixture, drives collision.
        args['allowed_external_IO_code']=[x for x in args['allowed_external_IO_code'] if x[0]!=prior.DISPATCH]
        mem.pop(prior.DISPATCH);mem[WORLD][0x1F24:0x1F24+TABLE_BYTES]=table
        struct.pack_into('<I',mem[0x6F2550],0x40,c['initial_tps'])
        excluded={0x7FFEE000,prior.fixture.p.f.GFX_CB,prior.fixture.p.f.SOUND_CB,prior.fixture.p.target.attack.EVENT,
            prior.fixture.p.target.LOAD_GFX,prior.fixture.p.target.LOAD_SOUND,0x28CFA4}
    else:
        mem={0x7FFEE000:bytearray([165])*0x4000};excluded={0x7FFEE000}
        args=dict(program='DKII-DX.exe',before_calls=[],allowed_external_IO_code=[],observe_reads=[],observe_writes=[])
    raw=raw_file(c);mem[DATA]=bytearray(raw)+bytes([165])*64
    mem[REF]=bytearray(win.d(FILE));mem[FILE]=bytearray([165])*72
    stream=bytearray(96);struct.pack_into('<I',stream,0,0x672FE8);struct.pack_into('<II',stream,12,win.HANDLE,0)
    mem[STREAM]=stream;cfg=bytearray(0x110)
    struct.pack_into('<III',cfg,0xD4,HEADER,(0x521790 if c['profile']=='runtime' else CALLBACK) if c['callback'] else 0,WORLD if c['profile']=='runtime' else CAPSTATE)
    mem[CFG]=cfg;mem[STATE]=bytearray(20);mem[LOG]=bytearray([165])*(1024*20)
    mem[CAPSTATE]=bytearray(4);mem[CAPTURE]=bytearray([165])*(1024*32+64);mem[OUT]=bytearray(32)
    adapters=win.build(EXE.read_bytes(),source=DATA,size=len(raw),state=STATE,log=LOG,read_code=READCODE,seek_code=SEEKCODE,ref_address=STREAM+8)
    # Header probe records the exact native28-byte MID and returns a selected status.
    header=bytes.fromhex('56578b74240cbf')+win.d(OUT)+bytes.fromhex('b907000000f3a5b8')+win.d(0 if c['header_reject'] else 1)+bytes.fromhex('5f5ec3')
    adapters.update({CALLBACK:capture_callback(c['reject']),HEADER:header})
    for a,b in adapters.items():mem[a]=bytearray(b);args['allowed_external_IO_code'].append([a,len(b)]);excluded.add(a)
    args['before_calls'] += [dict(address='0x5b6e40',registers=dict(ECX=hex(FILE)),stack_args=[]),dict(address='0x5b6f20',registers=dict(ECX=hex(FILE)),stack_args=[STREAM])]
    if c['profile']=='runtime':
        args['before_calls'] += [dict(address='0x56cbd0',registers=dict(ECX='0'),stack_args=[REF,CFG]),collision]
        # Preserve inherited root observation function, which returns normally.
    else:args.update(address='0x56cbd0',registers=json.dumps(dict(ECX='0')),stack_args=[REF,CFG])
    args.update(max_steps=3000000,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])))
    args['observe_reads'] += [[DATA,len(raw)],[WORLD+0x1F24,TABLE_BYTES+16],[0x6F2590,4]]
    args['observe_writes'] += [[STATE,20],[WORLD+0x72E7,4],[0x6F2550,0x1000]]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    if c['profile']=='runtime':checks[prior.TABLE]=prior.fixture.p.pe_read(EXE.read_bytes(),prior.TABLE,0x1064)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks


def execute(c):
    args,ch=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_variables_file_api.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase197-Variables-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        stop=prior.fixture.p.f.OBS if c['profile']=='runtime' else STOP
        saved=dict(case=c,input=args,result=cpu.run(EXE,args,list(ch.items()),stop,OUT,32));dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==len(args['before_calls'])
    post=post_context(ch,r);u=lambda a,o=0,n=4:int.from_bytes(post[a][o:o+n],'little')
    state=struct.unpack('<5I',post[STATE]);assert state[3]==0,(c['name'],state)
    expected_return=0 if c['header_tag']!=231 or c['data_tag']!=232 or c['reject'] or c['header_reject'] else 1
    actual_return=int(r['registers']['EAX'],16) if c['profile']=='capture' else next(x['return_EAX'] for x in r['before_calls'] if x['address']=='0x56cbd0')
    if isinstance(actual_return,str):actual_return=int(actual_return,16)
    assert actual_return==expected_return,(c['name'],actual_return,expected_return)
    raw=raw_file(c);assert post[DATA]==ch[DATA]
    count=int.from_bytes(raw[20:24],'little');n=0
    if c['header_tag']==231:
        assert post[OUT][:28]==raw[20:48]
        if c['data_tag']==232 and not c['header_reject'] and c['callback']:
            n=min(count,1) if c['reject'] else count
    if c['profile']=='capture':
        assert u(CAPSTATE)==n,(c['name'],u(CAPSTATE),n)
        previous=bytearray([165])*16
        for i in range(n):
            part=raw[56+16*i:56+16*i+16];previous[:len(part)]=part
            expected=struct.pack('<4I',1,1,1,1)+previous
            assert post[CAPTURE][32*i:32*i+32]==expected,(c['name'],i,post[CAPTURE][32*i:32*i+32].hex(),expected.hex())
        assert post[CAPTURE][32*n:]==ch[CAPTURE][32*n:]
        out=dict(captured_records=n,read_calls=state[1],short_reads=state[2],cursor=state[0],return_value=actual_return)
    else:
        assert n==len(c['records']);vm=load('variable-runtime-map.json')['rows'];tps=c['initial_tps'];expected={}
        for tag,val,_,_ in c['records']:
            row=vm.get(str(tag))
            if not row:continue
            v=val
            if row['transform']=='signed_i32_divide_by_TPS':
                signed=val if val<0x80000000 else val-0x100000000
                signed_tps=tps if tps<0x80000000 else tps-0x100000000
                assert signed_tps;v=(abs(signed)//abs(signed_tps))*(-1 if (signed<0)!=(signed_tps<0) else 1)
            a=int(row['runtime_address'],16);width=row['runtime_bytes'];expected[a]=(v&((1<<(width*8))-1),width)
            if tag==19:tps=val
        for a,(v,width) in expected.items():assert u(0x6F2550,a-0x6F2550,width)==v,(c['name'],hex(a),v,u(0x6F2550,a-0x6F2550,width))
        assert u(prior.TARGET,0x447,2)==(5*tps)&65535 and u(prior.TARGET,0x40)==1000
        assert post[WORLD][0x1F24:0x1F24+TABLE_BYTES]==ch[WORLD][0x1F24:0x1F24+TABLE_BYTES]
        checksum=sum((b if b<128 else b-256) for row in c['records'] for b in struct.pack('<4I',1,1,1,1)+struct.pack('<4I',*row))
        assert u(WORLD,0x72E7)==(int.from_bytes(ch[WORLD][0x72E7:0x72EB],'little')+checksum)&0xFFFFFFFF
        assert r['visited_instructions'].get('0x521790')==n
        out=dict(records_applied=n,effective_TPS=tps,freeze_timer=(5*tps)&65535,runtime_values={hex(a):dict(value=v,width=w) for a,(v,w) in expected.items()},read_calls=state[1],short_reads=state[2],return_value=actual_return)
    assert r['visited_instructions'].get('0x56cbd0')==1 and r['visited_instructions'].get('0x5716e0')==1
    assert r['visited_instructions'].get('0x5b3eb0')==state[1]
    out['native_steps']=r['instruction_count'];return post,out


def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,ch,saved,file=execute(c);_,out=verify(c,args,ch,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out,flush=True)
    if len(selected)==len(cases()):dump('phase197-Variables-emulations.json',dict(phase=197,registration=registration()[1],replays=rows))


if __name__=='__main__':main()
