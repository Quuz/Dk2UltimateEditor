"""Whole Pro4937C0 Variables writer with native numeric defaults and Terrain6.

Map model and its double array are explicit inputs, not a reconstructed full
editor document. Historical Win32/IAT and CFile sink fixtures remain declared.
"""
from pathlib import Path
import json,struct,sys
import emulate_editor_global_variables as defaults
import native_editor_variables_writer_cpu as cpu
from emulate_room_model_chain import p32,region
ROOT=Path(__file__).resolve().parent
EXE=defaults.engine.EXE
sha=defaults.sha
MAP,VALUES,RESEARCH_OUT=0x17700000,0x17710000,0x17720000
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,v:(ROOT/n).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf8')


def cases():
    out=[]
    def add(name,TPS=4,overrides=None,terrain=None,tail=None,extra=False,lists=None,error=None):
        out.append(dict(name=name,TPS=TPS,seed=165,overrides=overrides or {},terrain=terrain or {},tail=tail,extra=extra,lists=lists or [],error=error))
    add('defaults-TPS4');add('defaults-TPS20',TPS=20)
    add('tail-A',tail=[0x11223344,0x55667788]);add('tail-B',tail=[0xDEADBEEF,0xA5A55A5A])
    add('fractional-positive',overrides={100:3.75,**{k:1.25 for k in [67,155,158,185,220,221]}})
    add('fractional-negative',overrides={100:-3.75,**{k:-1.25 for k in [67,155,158,185,220,221]}})
    add('signed-max',overrides={100:2147483647});add('signed-min',overrides={100:-2147483648})
    add('negative-type7',overrides={7:-300});add('positive-type7',overrides={7:300})
    add('fractional-type7',overrides={7:-300.99});add('type74-slot-ignored',overrides={7:-300,74:9999})
    add('gold-value-double',overrides={7:-300},terrain={0x19A:6000})
    add('max-health-half',overrides={7:-300},terrain={0x1FF:330})
    add('type74-TPS20',TPS=20,overrides={7:-300})
    add('type74-product-wrap',overrides={7:0x40000000},terrain={0x19A:3})
    add('type74-signed-min',overrides={7:-2147483648})
    add('unknown-array-slots',overrides={19:99,29:0x12345678,186:0x76543210})
    add('manual-ID-extension',overrides={29:0x12345678},extra=True)
    add('type66-list',lists=[0x0201,0xFEC8])
    add('zero-TPS',TPS=0,error=0x494181)
    add('zero-max-health',terrain={0x1FF:0},error=0x494164)
    return out


def build(c):
    f=defaults.probe.build();engine=defaults.engine;w=engine.writer
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in f['memory']}
    # Existing Terrain fixture loads1. Select actualGold6 before its native
    # converter/callback; these bodies remain original instructions.
    terrain=next(r for s in defaults.fmt.parse((ROOT.parents[1]/'Dungeon Keeper 2/Data/editor/Terrain.kwd').read_bytes()) if s.signature==110 for r in s.records if r[0x1D6]==6)
    terrain=bytearray(terrain)
    for offset,value in c['terrain'].items():struct.pack_into('<H',terrain,int(offset),value)
    mem[engine.RAW]=terrain+bytes([165])*64
    ids,raw,_=defaults.static_defaults();values=bytearray(raw)
    for typ,val in c['overrides'].items():struct.pack_into('<d',values,8*int(typ),val)
    model=bytearray(0xA90);struct.pack_into('<4I',model,0x134,VALUES,248,248,0);struct.pack_into('<H',model,0x946,c['TPS'])
    struct.pack_into('<d',model,0xB8,1.25);struct.pack_into('<I',model,0xC0,0x11223344)
    mem[MAP]=model;mem[VALUES]=values;mem[RESEARCH_OUT]=bytearray(16)
    mem[w.STACK-0x4000]=bytearray([c['seed']])*0x5000
    # Keep the native Terrain construction plus default initializer, removing
    # the old Terrain writer and unrelated text-reader scene's setup calls.
    f['calls']=[dict(address=0x42ED90,ecx=engine.MID,stack=[engine.RAW]),dict(address=0x49F5E0,ecx=0,stack=[engine.MID]),dict(address=0x47FFB0,ecx=0x54F550,stack=[])]
    if c['extra']:
        # A declared caller adds Type29 to the scalar ID list after native
        # defaults, testing the writer's ID policy rather than supported UI.
        ids=ids+[29];CUSTOM=0x17730000;CALLER=0x17740000
        mem[CUSTOM]=bytearray(struct.pack('<'+'I'*len(ids),*ids))
        mem[CALLER]=bytearray(bytes.fromhex('c705')+p32(0x54FC04)+p32(CUSTOM)+bytes.fromhex('c705')+p32(0x54FC08)+p32(len(ids))+b'\xC3')
        f['providers'] += [[CALLER,len(mem[CALLER])]];f['calls'].append(dict(address=CALLER,ecx=0,stack=[]))
    if c['lists']:
        LIST=0x17750000;mem[LIST]=bytearray(struct.pack('<'+'H'*len(c['lists']),*c['lists']))
        struct.pack_into('<2I',mem[MAP],0xA7C,LIST,len(c['lists']))
    if c['tail'] is not None or not c['name'].startswith('defaults-'):
        # Explicit external caller supplies stack scratch before entering native
        # writer; native code gets no replacement instructions/register hooks.
        CALLER=0x17760000
        code=bytes.fromhex('bf')+p32(w.STACK-0x1000)+bytes.fromhex('b9')+p32(0x400)+bytes.fromhex('b8a5a5a5a5f3ab')
        code+=b''.join(bytes.fromhex('c705')+p32(a)+p32(v) for a,v in zip([w.STACK-0x154,w.STACK-0x150],c['tail'] or [0xA5A5A5A5]*2))+b'\xC3'
        mem[CALLER]=bytearray(code);f['providers'] += [[CALLER,len(code)]]
        f['calls'].append(dict(address=CALLER,ecx=0,stack=[]))
    f['calls'].append(dict(address=0x4937C0,ecx=MAP,stack=[w.FILE]))
    # The generic CFile sink accepts requests<=1055; Variables uses12/36/8/16.
    f['memory']=[region(a,b) for a,b in sorted(mem.items())]
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),spans
    return f


def execute(c):
    f=build(c);tag=sha((json.dumps(f,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_editor_variables_writer_cpu.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase199-editor-Variables-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==f
    else:
        saved=dict(case=c,input=f,result=cpu.run(f));dump(str(file.relative_to(ROOT)),saved)
    return f,saved,file


def signed(value):return (value&0x7fffffff)-(value&0x80000000)
def idiv(a,b):return (abs(a)//abs(b))*(-1 if (a<0)!=(b<0) else 1)


def verify(c,f,r):
    assert r['source_exe_sha256']==sha(EXE.read_bytes()) and r['code_substitutions']==0
    assert not r['failures'],r['failures']
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    from build_effect_semantics import pe_read
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;exe=EXE.read_bytes()
    for row in r['low_reads']+r['low_writes']:
        pc=int(row['pc'],16);ins=next(md.disasm(pe_read(exe,pc,15),pc))
        assert row['address']==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str

    inp={int(x['address'],16):bytes.fromhex(x['hex']) for x in f['memory']}
    post={int(x['address'],16):bytes.fromhex(x['hex']) for x in r['output']};e=defaults.engine;w=e.writer
    assert post[MAP]==inp[MAP] and post[VALUES]==inp[VALUES] and post[e.RAW]==inp[e.RAW]
    assert post[e.MID][-64:]==bytes(64) and post[0]==inp[0]==bytes(4096)
    h=post[e.HEAP];ptr,count=struct.unpack_from('<2I',post[0x54f960]);assert count==1
    tp=struct.unpack_from('<I',h,ptr-e.HEAP)[0];model=h[tp-e.HEAP:tp-e.HEAP+0x3d8]
    gold,maxhealth=struct.unpack_from('<H',model,0x36a)[0],struct.unpack_from('<H',model,0x34a)[0]
    assert gold==struct.unpack_from('<H',inp[e.RAW],0x19a)[0] and maxhealth==struct.unpack_from('<H',inp[e.RAW],0x1ff)[0]
    ids,_,_=defaults.static_defaults();ids=ids+([29] if c['extra'] else [])
    seed=bytes.fromhex(r['writer_entry_record_seed_hex']);tails=struct.unpack_from('<2I',seed,8)
    if c['tail'] is not None:assert tails==tuple(c['tail'])
    scalar=[]
    for typ in ids:
        value=struct.unpack_from('<d',inp[VALUES],typ*8)[0]
        scalar.append((typ,int(value*(4096 if typ in {67,155,158,185,220,221} else 1))&0xffffffff,*tails))
    out=post[w.OUTPUT];declared=len(scalar)+3+len(c['lists']);size=56+16*declared
    assert struct.unpack_from('<I',out,8)[0]==size and struct.unpack_from('<I',out,20)[0]==declared and struct.unpack_from('<I',out,52)[0]==16*declared
    events=[x for x in r['writer_record_writes'] if 0x49404c<=int(x['pc'],16)<0x4940d7]
    assert len(events)==2*len(scalar)
    assert all(int(x['address'],16) in [w.STACK-0x15c,w.STACK-0x158] and x['size']==4 for x in events)
    assert r['visited_instructions'].get('0x494055')==len(scalar)
    assert r['visited_instructions'].get('0x4940b9')==len(scalar)
    quotient=None
    if c['error']:
        assert not r['success'] and r['error']=='Unhandled CPU exception (UC_ERR_EXCEPTION)' and int(r['registers']['EIP'],16)==c['error']
        written=scalar;actual_size=56+16*len(scalar)
        assert len(r['calls'])==len(f['calls'])-1
    else:
        assert r['success'] and r['error'] is None and len(r['calls'])==len(f['calls']) and all(x['returned'] for x in r['calls'])
        assert int(r['registers']['EAX'],16)&255==1
        v=int(struct.unpack_from('<d',inp[VALUES],7*8)[0]);product=signed((abs(v)&0xffffffff)*gold)
        quotient=idiv(idiv(product,maxhealth),c['TPS'])
        written=scalar+[(74,quotient&0xffffffff,0,0)]+[(66,x&255,x>>8,0) for x in c['lists']]+[(241,5120,0,0),(242,0x11223344,0,0)]
        actual_size=size
        parts=defaults.fmt.parse(out[:size]);assert len(parts)==1 and parts[0].signature==230 and b''.join(x.pack() for x in parts)==out[:size]
    records=[struct.unpack_from('<4I',out,56+16*i) for i in range(len(written))];assert records==written,(c['name'],[(a,b) for a,b in zip(records,written) if a!=b][:5])
    assert out[actual_size:]==bytes([165])*(len(out)-actual_size)
    assert struct.unpack('<3I',post[w.META])==(3+len(written),w.OUTPUT+actual_size,0)
    for key in [19,186]:assert key not in [r[0] for r in records]
    assert (29 in [r[0] for r in records])==c['extra']
    return dict(expected_failure=bool(c['error']),fault_PC=c['error'],declared_records=declared,written_records=len(written),written_bytes=actual_size,
        scalar_records=len(scalar),all_scalar_values_match=True,scalar_tail_DWORDs=list(tails),all_scalar_tails_retain_entry_scratch=True,
        observed_scalar_record_writes=len(events),scalar_tail_writes=0,derived74=quotient,Terrain_gold_raw19A=gold,Terrain_max_health_raw1FF=maxhealth,
        Type7_model=struct.unpack_from('<d',inp[VALUES],56)[0],TPS=c['TPS'],whole_output_records_match=True,
        input_model_raw_and_guard_bytes_preserved=True,native_steps=r['instruction_count'])


def main():
    rows=[]
    for c in cases():
        if len(sys.argv)>1 and c['name'] not in sys.argv[1:]:continue
        f,saved,file=execute(c);out=verify(c,f,saved['result']);rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],'Type74',out['derived74'],'records',out['written_records'],'expected fault' if out['expected_failure'] else 'verified',flush=True)
    if len(sys.argv)==1:dump('phase199-editor-Variables-writer-emulations.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':main()
