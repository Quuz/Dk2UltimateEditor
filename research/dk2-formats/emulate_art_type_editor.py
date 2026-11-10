"""Original ArtResource type-change helper, stack provenance and Room serialization.

Prepared ArtResource objects and caller stack patterns are explicit fixtures.
Full Room cases execute native converter/model registration/writer. Read/write
hooks observe stack data provenance; no original instruction is substituted.
The existing initialized MFC/CRT/Win32 boundary fixture is reused unchanged.
"""
from pathlib import Path
import hashlib,json,struct,sys
import pefile
import emulate_room_model_chain as common
from audit_targeted_spell import executable_ranges
from build_effect_semantics import pe_read
import dk2_formats as fmt

ROOT=Path(__file__).resolve().parent;EXE=common.EXE;STACK=common.STACK;STOP=common.STOP
MODEL=0x17600000
sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
import unicorn
from unicorn import x86_const as xc

def sha(b):return hashlib.sha256(b).hexdigest()
def region(a,b):return dict(address=hex(a),hex=bytes(b).hex())


def art(old):
    b=bytearray(84);b[:11]=b'Fixture#9\0\0'
    struct.pack_into('<4I',b,64,0x12345678,0x10203040,0x55667788,0x99AABBCC)
    b[80:84]=bytes([old,0x21,0x43,0x65]);return bytes(b)


def cases():
    out=[dict(name=f'type-{old}-to7-stack{seed:02x}',kind='helper',old=old,new=7,seed=seed)
        for old in [0,6,7] for seed in [0,90,165]]
    for name,old,new,value in [('new7-ID4',0,7,4),('same7-ID0',7,7,0),
        ('same7-IDffffffff',7,7,0xFFFFFFFF),('wrongtype-IDffffffff',6,6,0xFFFFFFFF),('to5',7,5,None)]:
        out.append(dict(name=name,kind='helper',old=old,new=new,seed=165,value=value))
    out.append(dict(name='constructor',kind='constructor',old=7,new=0,seed=165))
    out.append(dict(name='clear',kind='clear',old=7,new=0,seed=165))
    for value in [128,255]:out.append(dict(name=f'same-unknown-type-{value}',kind='helper',old=value,new=value,seed=90))
    for old in [0,7]:
        for seed in [90,165]:out.append(dict(name=f'Room-complete-resource-{old}-to7-stack{seed:02x}',kind='Room',old=old,new=7,seed=seed,value=4))
    return out


def build(case):
    fixture=common.build(record_ids=[1]);memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in fixture['memory']}
    stack_base=STACK-0x4000;assert stack_base in memory
    memory[stack_base][:]=bytes([case['seed']])*len(memory[stack_base])
    raw=art(case['old'])
    if case['kind']=='Room':
        record=bytearray.fromhex(fixture['raw_records_hex'][0]);record[0xC8:0x11C]=raw
        memory[common.RAW][:len(record)]=record
        fixture['raw_records_hex']=[record.hex()];fixture['raw_hex']=record.hex()
        model=common.HEAP+0x16C
        calls=fixture['calls'][:2]
    else:
        model=MODEL
        memory[MODEL]=bytearray(struct.pack('<I',0x50AC44)+raw+struct.pack('<H',9)+bytes([165])*66)
        calls=[]
    if case['kind']=='constructor':calls.append(dict(address=0x41BB40,ecx=model,stack=[]))
    elif case['kind']=='clear':calls.append(dict(address=0x41C240,ecx=model,stack=[0]))
    else:
        calls.append(dict(address=0x41BBB0,ecx=model,stack=[case['new']]))
        if case['kind']=='Room':calls.append(dict(address=0x41C240,ecx=model,stack=[1]))
        if case.get('value') is not None:calls.append(dict(address=0x41C090,ecx=model,stack=[case['value']]))
    if case['kind']=='Room':calls+=fixture['calls'][2:]
    else:calls.append(dict(address=0x41C080,ecx=model,stack=[]))
    fixture.update(case=case,model=model,input_art_hex=raw.hex(),calls=calls,
        memory=[region(a,b) for a,b in sorted(memory.items())])
    return fixture


def run(fixture):
    data=EXE.read_bytes();pe=pefile.PE(data=data);image=pe.get_memory_mapped_image()
    cpu=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32);pages=set()
    def write(a,b):
        for p in range(a&~4095,(a+len(b)+4095)&~4095,4096):
            if p not in pages:cpu.mem_map(p,4096);pages.add(p)
        cpu.mem_write(a,bytes(b))
    write(pe.OPTIONAL_HEADER.ImageBase,image)
    for r in fixture['memory']:write(int(r['address'],16),bytes.fromhex(r['hex']))
    executable=executable_ranges(data)
    readonly=[(0x400000+s.VirtualAddress,s.Misc_VirtualSize) for s in pe.sections if s.Characteristics&0x40000000 and not s.Characteristics&0x80000000]
    visits={};failures=[];low=[];transitions=[];active=None;calls=[];current_call=None
    def get(a,n):return bytes(cpu.mem_read(a,n))
    def code(uc,a,n,_):
        nonlocal active
        visits[hex(a)]=visits.get(hex(a),0)+1
        if not any(p<=a<p+s for p,s in executable+fixture['providers']):
            failures.append(['outside_original_PE_or_declared_boundary',hex(a)]);uc.emu_stop()
        if a==0x41BBB0:
            entry_sp=uc.reg_read(xc.UC_X86_REG_ESP);obj=uc.reg_read(xc.UC_X86_REG_ECX)
            active=dict(entry_stack=entry_sp,model=obj,before_hex=get(obj,92).hex(),requested_type=get(entry_sp+4,1)[0],
                source_address=entry_sp-0x60,source_entry_hex=get(entry_sp-0x60,84).hex(),source_reads=[],source_writes=[])
            if fixture['case']['kind']=='Room':active['Room_model_before_hex']=get(common.HEAP,0x548).hex()
            transitions.append(active)
        if a==0x41BC07 and active is not None:
            active['copied_before_name_hex']=get(active['model']+4,84).hex()
        if a==0x41BC3E and active is not None:
            active['after_hex']=get(active['model'],92).hex();active=None
    def written(uc,access,a,n,value,_):
        if any(a+n>p and a<p+s for p,s in executable+readonly):
            failures.append(['write_original_code_or_readonly',hex(a)]);uc.emu_stop()
        if active is not None and a+n>active['source_address'] and a<active['source_address']+84:
            active['source_writes'].append(dict(pc=hex(uc.reg_read(xc.UC_X86_REG_EIP)),address=a,size=n,value=value))
    def read(uc,access,a,n,value,_):
        pc=uc.reg_read(xc.UC_X86_REG_EIP)
        if a<4096:low.append(dict(pc=pc,address=a,size=n))
        if active is not None and pc==0x41BC05 and active['source_address']<=a<active['source_address']+84:
            active['source_reads'].append(dict(address=a,hex=get(a,n).hex()))
    cpu.hook_add(unicorn.UC_HOOK_CODE,code);cpu.hook_add(unicorn.UC_HOOK_MEM_READ,read);cpu.hook_add(unicorn.UC_HOOK_MEM_WRITE,written)
    error=None;unassigned=None
    try:
        for call in fixture['calls']:
            current_call=call
            for r in ['EAX','EBX','ECX','EDX','ESI','EDI']:cpu.reg_write(getattr(xc,'UC_X86_REG_'+r),0)
            for r,value in [('ESP',STACK),('EBP',STACK),('EFLAGS',2),('FPCW',0x37F),('ECX',call['ecx'])]:
                cpu.reg_write(getattr(xc,'UC_X86_REG_'+r),value)
            write(STACK,b''.join(struct.pack('<I',v) for v in [STOP]+call['stack']))
            if call['address']==0x4A7AC0:unassigned=get(STACK-0x420+0x36F,1)[0]
            cpu.emu_start(call['address'],STOP,timeout=10_000_000,count=1000000)
            ok=cpu.reg_read(xc.UC_X86_REG_EIP)==STOP and not failures
            calls.append(dict(address=hex(call['address']),returned=ok,EAX=hex(cpu.reg_read(xc.UC_X86_REG_EAX))))
            if not ok:break
    except unicorn.UcError as exc:error=str(exc)
    output=[region(int(r['address'],16),get(int(r['address'],16),len(bytes.fromhex(r['hex'])))) for r in fixture['memory'] if int(r['address'],16) not in [0,STACK-0x4000]]
    return dict(success=not error and not failures and len(calls)==len(fixture['calls']) and all(c['returned'] for c in calls),
        error=error,failures=failures,low_reads=low,transitions=transitions,calls=calls,output=output,
        visited_instructions=visits,instruction_count=sum(visits.values()),source_exe_sha256=sha(data),
        code_or_vtable_substitutions=0,model_hex=get(fixture['model'],92).hex(),writer_unassigned36F=unassigned)


def path_for(fixture):
    tag=sha((json.dumps(fixture,sort_keys=True)+sha(Path(__file__).read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase130-art-type-{tag}.json'


def verify(fixture,result):
    case=fixture['case'];assert result['success'] and not result['error'] and not result['failures']
    assert result['code_or_vtable_substitutions']==0
    assert all(r['address']==0 and r['size']==4 and pe_read(EXE.read_bytes(),r['pc'],2)==bytes.fromhex('64a1') for r in result['low_reads'])
    obj=bytes.fromhex(result['model_hex']);initial=bytes.fromhex(fixture['input_art_hex'])
    expected=bytearray(initial);cache=9
    if case['kind'] in ['constructor','clear']:
        expected=bytearray(84);cache=0 if case['kind']=='constructor' else 9
        assert not result['transitions']
    else:
        assert len(result['transitions'])==1;t=result['transitions'][0]
        assert t['requested_type']==case['new']
        assert bytes.fromhex(t['before_hex'])[4:88]==initial
        requested_signed=case['new'] if case['new']<128 else case['new']-256
        if case['old']==requested_signed:
            assert not t['source_reads'] and not t['source_writes'] and 'copied_before_name_hex' not in t
            assert t['before_hex']==t['after_hex']
        else:
            assert len(t['source_reads'])==21
            copied=b''.join(bytes.fromhex(r['hex']) for r in t['source_reads'])
            assert [r['address'] for r in t['source_reads']]==list(range(t['source_address'],t['source_address']+84,4))
            assert copied.hex()==t['copied_before_name_hex']
            wanted=bytearray.fromhex(t['source_entry_hex']);wanted[80]=case['new']
            assert copied==wanted
            assert t['source_writes']==[dict(pc='0x41bbe6',address=t['source_address']+80,size=1,value=case['new'])]
            expected=bytearray(copied);expected[:64]=initial[:64]
            assert bytes.fromhex(t['after_hex'])[4:88]==expected
            if case['kind']=='helper':
                assert all(v==case['seed'] for i,v in enumerate(copied) if i!=80)
        if case['kind']=='Room':struct.pack_into('<I',expected,64,struct.unpack_from('<I',expected,64)[0]|1)
        if case.get('value') is not None and case['new']==7:struct.pack_into('<I',expected,68,case['value'])
    assert obj[:4]==struct.pack('<I',0x50AC44) and obj[4:88]==expected and int.from_bytes(obj[88:90],'little')==cache
    out={int(r['address'],16):bytes.fromhex(r['hex']) for r in result['output']}
    inputs={int(r['address'],16):bytes.fromhex(r['hex']) for r in fixture['memory']}
    assert out[common.RAW]==inputs[common.RAW] and out[common.MID][-64:]==inputs[common.MID][-64:]
    record_verified=False
    if case['kind']=='Room':
        model_before=bytearray.fromhex(result['transitions'][0]['Room_model_before_hex'])
        model_before[0x170:0x1C4]=expected
        assert out[common.HEAP][:0x548]==model_before
        assert int.from_bytes(out[0x54F998][:4],'little')!=0
        model_array=int.from_bytes(out[0x54F998][:4],'little')
        assert int.from_bytes(out[common.HEAP][model_array-common.HEAP:model_array-common.HEAP+4],'little')==common.HEAP
        meta=out[common.writer.META];assert int.from_bytes(meta[8:12],'little')==0
        written=int.from_bytes(meta[4:8],'little')-common.writer.OUTPUT
        blob=out[common.writer.OUTPUT][:written];sections=fmt.parse(blob)
        assert b''.join(s.pack() for s in sections)==blob and len(sections)==1 and len(sections[0].records)==1
        want=bytearray.fromhex(fixture['raw_records_hex'][0]);want[0xC8:0x11C]=expected
        want[0x36F]=result['writer_unassigned36F']
        assert sections[0].records[0]==want and len(want)==1055
        record_verified=True
    else:
        wanted=struct.pack('<I',0x50AC44)+expected+struct.pack('<H',cache)+bytes([165])*66
        assert out[MODEL]==wanted
        value=int.from_bytes(expected[68:72],'little') if expected[80]==7 else 0
        assert int(result['calls'][-1]['EAX'],16)==value
    return dict(kind=case['kind'],type_byte_changed=case['old']!=case['new'],stack_to_raw_provenance_checked=bool(result['transitions'] and result['transitions'][0]['source_reads']),
        output_art_hex=bytes(expected).hex(),opaque_DWORDs=list(struct.unpack_from('<2I',expected,72)),
        exact_original_writer_record_bytes=1055 if record_verified else 0,instructions=result['instruction_count'])


if __name__=='__main__':
    rows=[]
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for case in selected:
        fixture=build(case);path=path_for(fixture)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==fixture
        else:
            saved=dict(input=fixture,result=run(fixture));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        outcome=verify(fixture,saved['result']);rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified',outcome['opaque_DWORDs'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase130-art-type-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
