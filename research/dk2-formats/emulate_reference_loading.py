"""Original file-open/section loop and Level reference traversal discovery."""
from pathlib import Path
import json,struct,sys
import emulate_variables_reader as variables
import emulate_level_globals as level
import native_reference_file_api as win
import native_unicorn_reference as cpu
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT,EXE,sha,load,dump=variables.ROOT,variables.EXE,variables.sha,variables.load,variables.dump
WORLD=variables.WORLD
PATH,CFG,OUT=0x23300000,0x23310000,0x23320000
DATA=0x23400000


def cases():
    base=dict(profile='level',level_TPS=4,flags=0x36,missing=None,root_missing=False,extra_section=False,records=[variables.rec(19,30),variables.rec(4,101)])
    rows=[('direct-variables',dict(profile='variables')),('direct-two-sections',dict(profile='variables',extra_section=True)),
        ('common30-map60',dict(references=['common.kwd','map.kld'])),('map60-common30',dict(references=['map.kld','common.kwd'])),
        ('common4-mapTPS30',dict(references=['rate.kwd','tick.kld'])),('mapTPS30-common4',dict(references=['tick.kld','rate.kwd'])),
        ('missing-first',dict(references=['absent.kwd','rate.kwd'])),('missing-last',dict(references=['common.kwd','absent.kld'])),
        ('duplicate-reference',dict(references=['common.kwd','common.kwd'])),('missing-root',dict(references=['common.kwd'],root_missing=True)),
        ('bad-child',dict(references=['bad.kwd','rate.kwd'])),('partial-child',dict(references=['partial.kwd'])),
        ('prerequisite-bit2-clear',dict(references=['common.kwd'],flags=0x34))]
    return [dict(base,**values,name=name) for name,values in rows]


def build(c):
    old=next(x for x in variables.cases() if x['name']=='runtime-TPS-first')
    if c['profile']=='variables':args,_=variables.build(old)
    else:args,_=level.build(next(x for x in level.cases() if x['name']=='level-only4'))
    observer=dict(address=args['address'],registers=args['registers'],stack_args=args.get('stack_args',[]))
    collision=next(r for r in args['before_calls'] if r['address']=='0x4abd60')
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    excluded={variables.REF,variables.FILE,variables.STREAM,variables.CFG,variables.DATA,variables.STATE,variables.LOG,variables.CAPSTATE,variables.CAPTURE,variables.OUT,
        variables.CALLBACK,variables.HEADER,variables.READCODE,variables.SEEKCODE,0x28CF2A,0x28CF42,0x28C5D4}
    for a in excluded:mem.pop(a,None)
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0] not in excluded]
    # Older read-probe body crossed other external import addresses. Relocate
    # its internal-relative-only code, retaining a five-byte external thunk.
    old_probe=bytes(mem[0x28CFA4]);mem[0x23500000]=bytearray(old_probe);mem[0x28CFA4]=bytearray(win.jump(0x28CFA4,0x23500000))
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=0x28CFA4]+[[0x28CFA4,5],[0x23500000,len(old_probe)]]
    if c['profile']=='level':
        body=bytes(mem[level.WIDE]);mem[0x23510000]=bytearray(body);mem[level.WIDE]=bytearray(win.jump(level.WIDE,0x23510000))
        args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=level.WIDE]+[[level.WIDE,5],[0x23510000,len(body)]]
    args['before_calls']=[r for r in args['before_calls'] if r['address'] not in ['0x5b6e40','0x5b6f20','0x56cbd0','0x4abd60']]
    args['before_calls']=[r for r in args['before_calls'] if r['address'] not in [hex(variables.prior.DISPATCH),'0x56c100','0x51e6d0','0x1e550000','0x1e580000']]
    mem.pop(variables.prior.DISPATCH,None);args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=variables.prior.DISPATCH]
    if c['profile']=='level':
        table,_=variables.registration();mem[WORLD][0x1F24:0x1F24+variables.TABLE_BYTES]=table;mem[WORLD][0x7239]=c['flags']
        # Both sound slots retain their own declared external calling convention.
        struct.pack_into('<I',mem[variables.prior.fixture.p.f.SV],0x1C,level.LEVEL_SOUND)
        global_extra=mem.pop(0x6F2957);mem[0x6F2550][0x6F2957-0x6F2550:0x6F2957-0x6F2550+len(global_extra)]=global_extra
    cfile=dict(old,records=c['records']);raw=variables.raw_file(cfile)
    if c['extra_section']:raw+=variables.raw_file(dict(old,records=[variables.rec(19,60),variables.rec(4,101)]))
    files=[('C:\\DK2\\fixture.kwd',DATA,len(raw))]
    mem[DATA]=bytearray(raw)+bytes([165])*64
    if c['profile']=='level':
        rawlevel=bytearray(mem[level.RAWLEVEL]);struct.pack_into('<H',rawlevel,0x1182,c['level_TPS'])
        refs=b''.join(struct.pack('<2I',230,0)+name.encode('ascii')+bytes(64-len(name)) for name in c['references'])
        head=bytearray(level.SOURCE.read_bytes()[:56]);struct.pack_into('<HH',head,20,len(c['references']),0)
        raw=bytes(head)+rawlevel+struct.pack('<II',222,len(refs))+refs
        raw=raw[:8]+struct.pack('<I',len(raw))+raw[12:];mem[DATA]=bytearray(raw)+bytes([165])*64
        files=[] if c['root_missing'] else [('C:\\DK2\\fixture.kwd',DATA,len(raw))]
        definitions=[('common.kwd',[variables.rec(19,30),variables.rec(4,101)]),('map.kld',[variables.rec(19,60),variables.rec(4,101)]),
            ('rate.kwd',[variables.rec(4,101)]),('tick.kld',[variables.rec(19,30)])]
        for i,(name,records) in enumerate(definitions):
            b=variables.raw_file(dict(old,records=records));address=DATA+(i+1)*0x10000;mem[address]=bytearray(b)+bytes([165])*64
            files.append(('C:\\DK2\\'+name,address,len(b)))
        bad_section=variables.raw_file(dict(old,header_tag=230))
        for name,address,b in [('bad.kwd',DATA+0x50000,bad_section),('partial.kwd',DATA+0x60000,variables.raw_file(dict(old,records=[variables.rec(19,60),variables.rec(4,101)]))+bad_section)]:
            mem[address]=bytearray(b)+bytes([165])*64;files.append(('C:\\DK2\\'+name,address,len(b)))
    mem[PATH]=bytearray(b'fixture.kwd\0'+bytes(244));cfg=bytearray(0x110)
    struct.pack_into('<III',cfg,0xD4,0x521770,0x521790,WORLD);mem[CFG]=cfg;mem[OUT]=bytearray(16)
    providers,allowed,proof=win.build(EXE.read_bytes(),files)
    mem.update({a:bytearray(b) for a,b in providers.items()});args['allowed_external_IO_code']+=allowed
    mem[0x6CBAD4]=bytearray(4);mem[0x7A5DA8]=bytearray(struct.pack('<I',0x1234))
    mem[0x6C9238+9*4]=bytearray(struct.pack('<I',win.CRIT));mem[0x6C9AB0]=bytearray(struct.pack('<6I',0x6C9AB0,0,0,0,0,0))
    mem[0x746EC0]=bytearray(65536)
    if c['profile']=='variables':args.update(address='0x56c960',registers=json.dumps(dict(ECX=hex(CFG))),stack_args=[PATH])
    else:
        args['before_calls'] += [dict(address='0x51ddc0',registers=dict(ECX=hex(WORLD)),stack_args=[PATH]),collision]
        args.update(observer)
    args.update(max_steps=2000000,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])))
    args['observe_reads']=[[0,4096],[0x6F2590,4]]
    args['observe_writes']=[[win.STATE,64],[win.HSTATE,0x1000],[0x6F2550,0x1000]]
    args['observe_registers']=[0x51E274,0x51EAE3,0x51DED1,0x51DDE9]
    excluded={0x7FFEE000,variables.prior.fixture.p.f.GFX_CB,variables.prior.fixture.p.f.SOUND_CB,variables.prior.fixture.p.target.attack.EVENT,
        variables.prior.fixture.p.target.LOAD_GFX,variables.prior.fixture.p.target.LOAD_SOUND,0x28CFA4}
    excluded.update(a for a,_ in args['allowed_external_IO_code'])
    ch={a:bytes(b) for a,b in mem.items() if a not in excluded}
    ch[variables.prior.TABLE]=variables.prior.fixture.p.pe_read(EXE.read_bytes(),variables.prior.TABLE,0x1064)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,ch,proof


def execute(c):
    args,ch,proof=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_reference_file_api.py').read_bytes())+sha((ROOT/'native_unicorn_reference.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase198-reference-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),0x23330000,win.STATE,64);saved=dict(case=c,input=args,result=r,OS_providers=proof);dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    p=post_context(ch,r);u=lambda a,o=0,n=4:int.from_bytes(p[a][o:o+n],'little')
    state=struct.unpack('<16I',p[win.STATE]);heap=struct.unpack('<8I',p[win.HSTATE][:32]);assert state[4]==0 and heap[2]==0,(c['name'],state,heap)
    # Invalid handle closes are normal Win32 failures performed by native
    # temporary streams, logged separately from provider-contract errors.
    assert state[1]==state[3] and state[6]==state[9],(c['name'],state)
    assert all(p[a]==ch[a] for a in ch if DATA<=a<DATA+0x70000)
    opened=[]
    for i in range(state[0]):
        row=p[win.OPENLOG][i*272:(i+1)*272];opened.append(dict(file_index=int.from_bytes(row[:4],'little'),handle=int.from_bytes(row[4:8],'little'),path=bytes(row[8:]).split(b'\0')[0].decode('ascii')))
    if c['profile']=='variables':
        root_return=int(r['registers']['EAX'],16);tps=60 if c['extra_section'] else 30;scaled=1 if c['extra_section'] else 3
        assert root_return==1 and [x['path'] for x in opened]==['C:\\DK2\\fixture.kwd']
        assert r['visited_instructions'].get('0x521790')==(4 if c['extra_section'] else 2)
    else:
        root_return=int(next(x['return_EAX'] for x in r['before_calls'] if x['address']=='0x51ddc0'),16)
        active=not c['root_missing'] and c['flags']&6==6;assert root_return==int(active),(c['name'],root_return)
        tps=c['level_TPS'] if active else 20;scaled=0
        expected_open=[] if c['flags']&6!=6 else ['C:\\DK2\\fixture.kwd']
        known={'common.kwd':[(19,30),(4,101)],'map.kld':[(19,60),(4,101)],'rate.kwd':[(4,101)],'tick.kld':[(19,30)],'bad.kwd':[],'partial.kwd':[(19,60),(4,101)]}
        if active:
            for name in c['references']:
                expected_open.append('C:\\DK2\\'+name)
                for tag,val in known.get(name,[]):
                    if tag==19:tps=val
                    else:scaled=val//tps
        assert [x['path'] for x in opened]==expected_open,(c['name'],opened,expected_open)
        assert bool(u(WORLD,0x7239,1)&8)==active
        assert r['visited_instructions'].get('0x51ea40',0)==(len(c['references']) if active else 0)
        assert r['visited_instructions'].get('0x51e6d0',0)==int(active)
        assert u(variables.prior.TARGET,0x447,2)==(tps*5)&65535 and u(variables.prior.TARGET,0x40)==1000
        returns=[int(x['EAX'],16) for x in r['observed_registers'] if x['pc']=='0x51e274']
        expected_returns=[int(name in known and name not in ['bad.kwd','partial.kwd']) for name in c['references']] if active else []
        assert returns==expected_returns,(c['name'],returns,expected_returns)
        observed_callback_returns=[int(x['EAX'],16) for x in r['observed_registers'] if x['pc']=='0x51eae3'];assert observed_callback_returns==returns
    assert u(0x6F2550,0x40)==tps and u(0x6F2550,4)==scaled,(c['name'],u(0x6F2550,0x40),u(0x6F2550,4),tps,scaled)
    stores=[dict(pc=x['pc'],value=x['value']) for x in r['observed_writes'] if int(x['address'],16)==0x6F2590]
    out=dict(return_value=root_return,effective_TPS=tps,Type4_runtime=scaled,opened=opened,successful_opens=state[1],closed_file_handles=state[3],
        read_calls=state[2],created_semaphores=state[6],closed_semaphores=state[9],normal_invalid_CloseHandle_calls=state[11],
        heap_allocations=heap[1],heap_frees=heap[3],provider_errors=0,TPS_stores=stores,native_steps=r['instruction_count'])
    if c['profile']=='level':out['freeze_timer']=(tps*5)&65535
    out['observed_original_return_edges']=r['observed_registers']
    return p,out


def main():
    rows=[]
    for c in cases():
        if len(sys.argv)>1 and c['name'] not in sys.argv[1:]:continue
        args,ch,saved,file=execute(c);r=saved['result'];p=post_context(ch,r)
        if not r['success']:print(c['name'],file.name,r['emulation_error'],r['registers'],r['trace_tail'],flush=True)
        _,out=verify(c,args,ch,r);rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],'return',out['return_value'],'TPS',out['effective_TPS'],'Type4',out['Type4_runtime'],'opens',out['successful_opens'],'steps',out['native_steps'],flush=True)
    if len(rows)==len(cases()):dump('phase198-reference-emulations.json',dict(phase=198,replays=rows))


if __name__=='__main__':main()
