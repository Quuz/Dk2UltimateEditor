"""Raw74 + original registration/loaders -> full464360 gold predicate."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_temple_creature_entry as fixture
import native_unicorn_mapreader as cpu
import native_gold_margin as api
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXE,sha=fixture.EXE,fixture.sha
RAW74,RAW65,DISPATCH=0x1D000000,0x1D010000,0x1D020000
CALLER=DISPATCH+0x100
TABLE,TABLE_BYTES=0x6A5CD0,0x1064
ROW74=0x6A61E4
SOURCE74=fixture.SOURCE.parent/'AutoSaveVariables.kld'
RUNTIME=0x6F2550
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))


def cases():
    return [dict(name='source-below',source=True,carried=5659),dict(name='source-boundary',source=True,carried=5660),
        dict(name='source-above',source=True,carried=5661),dict(name='zero-margin-below',margin=0,carried=5999),
        dict(name='zero-margin-full',margin=0,carried=6000),dict(name='margin-equals-cap',margin=6000,carried=0),
        dict(name='margin-over-cap',margin=6001,carried=6000),dict(name='negative1-bits',margin=0xFFFFFFFF,carried=6000),
        dict(name='level2-below',level=2,coefficient=150,carried=8659),dict(name='level2-boundary',level=2,coefficient=150,carried=8660),
        dict(name='level2-negative-growth',level=2,coefficient=-1,carried=0),dict(name='mode0-empty',argument=0,carried=0),
        dict(name='mode0-one',argument=0,carried=1),dict(name='small-cap-underflow',base_gold=100,carried=100),
        dict(name='unsigned-high-carry',margin=0xFFFFFFFF,carried=0xFFFFFFFF),dict(name='nonzero-argument2',argument=2,carried=5660)]


def build(c):
    mem,calls=fixture.memory_and_calls(dict(fixture.cases()[0]))
    mem={a:bytearray(b) for a,b in mem.items()}
    actor=mem[fixture.ACTOR];actor[0x403]=c.get('level',1);fixture.put(actor,0x3FB,c['carried'])
    if 'base_gold' in c:fixture.put(mem[fixture.RAW],0xF1E,c['base_gold'],2)
    # Original65 callback writes fixed globals. Match the game's actual
    # pointer alias rather than a disconnected shadow Variables buffer.
    mem[RUNTIME]=bytearray(mem[fixture.VARS])
    mem[0x6E4154]=bytearray(struct.pack('<I',RUNTIME))
    raw=struct.pack('<4I',74,c.get('margin',340),0x11223344,0x55667788)
    if c.get('source'):
        import dk2_formats as fmt
        data=SOURCE74.read_bytes()
        raw=next(r for s in fmt.parse(data) if s.signature==230 for r in s.records if int.from_bytes(r[:4],'little')==74)
    mem[RAW74]=bytearray(bytes(16)+raw+bytes([165])*64)
    mem[RAW65]=bytearray(bytes(16)+struct.pack('<4I',65,6,c.get('coefficient',100)&0xFFFFFFFF,c.get('level',1))+bytes([165])*64)
    mem[RUNTIME][0x6F28B6-RUNTIME:0x6F28BA-RUNTIME]=bytes([165])*4
    # Dispatch through the ACTUAL registration-table callback pointer.
    code=b'\x68'+struct.pack('<I',RAW74)+b'\xa1'+struct.pack('<I',ROW74+4)+bytes.fromhex('ffd0c3')
    mem[DISPATCH]=bytearray(code)
    caller=b'\xb9'+struct.pack('<I',fixture.CONTEXT)+b'\x68'+struct.pack('<I',c.get('argument',1))+b'\xb8'+struct.pack('<I',0x464360)+bytes.fromhex('ffd0c3')
    mem[CALLER]=bytearray(caller)
    calls.insert(0,dict(address='0x51aa90',registers=dict(ECX='0'),stack_args=[]))
    calls.append(dict(address=hex(DISPATCH),registers=dict(ECX='0'),stack_args=[]))
    calls.append(dict(address='0x51dc40',registers=dict(ECX='0'),stack_args=[RAW65]))
    mem[0x7FFEE000][0x2000:0x2008]=struct.pack('<II',fixture.STOP,c.get('argument',1))
    args=dict(program='DKII-DX.exe',address=hex(CALLER),registers=json.dumps(dict(ECX='0')),before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=400000,
        allowed_external_IO_code=[[fixture.SCB,len(mem[fixture.SCB])],[fixture.PLAY,len(mem[fixture.PLAY])],[DISPATCH,len(code)],[CALLER,len(caller)]],
        observe_reads=[[0x6F28B6,4],[fixture.ACTOR+0x3FB,4],[fixture.DEFINITION+0x69C,2],[0,4096]],
        observe_writes=[[0x6F28B6,4],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [fixture.SCB,fixture.PLAY,0x7FFEE000,DISPATCH,CALLER]}
    checks[TABLE]=pe_read(EXE.read_bytes(),TABLE,TABLE_BYTES)
    for a,n in [(0x66D524,0x130),(0x66E3EC,0x3A0)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    return args,checks


def execute(c):
    args,ch=build(c);deps=sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_mapreader.py').read_bytes())
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12];file=ROOT/'ghidra'/f'phase193-gold-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),fixture.STOP,fixture.ACTOR,len(ch[fixture.ACTOR]))
        saved=dict(case=c,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c,r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(ch,r);raw=ch[RAW74][16:32];margin=api.view(raw)['value_bits']
    base=c.get('base_gold',6000);level=c.get('level',1);coefficient=c.get('coefficient',100)
    outcome=api.project(base,level,coefficient,c['carried'],margin,c.get('argument',1))
    assert int(r['registers']['EAX'],16)==int(outcome['accepted'])
    assert post[fixture.ACTOR]==ch[fixture.ACTOR] and bytes.fromhex(r['captured_hex'])==ch[fixture.ACTOR]
    assert int.from_bytes(post[fixture.DEFINITION][0x69C:0x69E],'little')==base
    assert post[fixture.WORLD]==ch[fixture.WORLD]
    assert post[RAW74]==ch[RAW74] and post[RAW65]==ch[RAW65] and post[fixture.RAW]==ch[fixture.RAW]
    assert post[RUNTIME][0x6F28B6-RUNTIME:0x6F28BA-RUNTIME]==raw[4:8]
    at=0x13B+2*level
    assert int.from_bytes(post[RUNTIME][at:at+2],'little')==(coefficient&65535)
    for kind,at in [(19,0x6A5E38),(29,0x6A5F14),(74,ROW74),(186,0x6A68B0)]:
        row=post[TABLE][at-TABLE:at-TABLE+20];assert int.from_bytes(row[:4],'little')==kind and int.from_bytes(row[4:8],'little')==0x51CD90
    reads=[x for x in r['observed_reads'] if int(x['address'],16)==0x6F28B6]
    assert len(reads)==int(bool(c.get('argument',1))) and all(x['pc']=='0x4643c7' for x in reads)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    assert r['visited_instructions'].get('0x51aa90',0)==1 and r['visited_instructions'].get('0x51cd90',0)==1
    return post,dict(outcome,margin_bits=margin,base_gold=base,level=level,coefficient=coefficient,carried=c['carried'],
                     exact_actor_and_world_unchanged=True,actual_registered_callback=True,native_steps=r['instruction_count'])


def main():
    assert sha(EXE.read_bytes())==api.DX_SHA256
    rows=[]
    for c in cases():
        args,ch,s,file=execute(c);_,out=verify(c,args,ch,s['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],'threshold',out['threshold_bits'],'accepted',out['accepted'],flush=True)
    dump('phase193-gold-emulations.json',dict(phase=193,replays=rows))


if __name__=='__main__':main()
