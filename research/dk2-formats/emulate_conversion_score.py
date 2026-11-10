"""Original raw233 callback -> complete conversion -> original World score setter.

Initial actor/world/owner context is explicit. Torture timer/RNG caller is a
separate static boundary; no natural torture cycle or renderer is claimed.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_conversion_score as probe
import native_conversion_score as api
from emulate_resistance import post_context
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha,EXE=probe.sha,probe.EXE
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))


def cases():
    return [dict(name='source40',source_record=True),dict(name='zero',value=0),dict(name='positive123',value=123),
        dict(name='negative5',value=-5),dict(name='negative-clamp',value=-40),dict(name='int-min',value=-2147483648),
        dict(name='positive-overflow',score=0x7FFFFFFF,value=1),dict(name='negative-wrap',score=0x80000000,value=-1),
        dict(name='negative-initial-stays-negative',score=0x80000000,value=0),
        dict(name='other-mode',mode=3),dict(name='mode-zero',mode=0),
        dict(name='player0',player_type=0),dict(name='player1',player_type=1),dict(name='player2',player_type=2),
        dict(name='player4',player_type=4),dict(name='player255',player_type=255)]


def execute(c):
    args,mem=probe.build(c)
    checks={a:bytes(b) for a,b in mem.items() if a not in [probe.fixture.SCB,probe.fixture.PLAY,0x7FFEE000]}
    for address,size in [(0x66D524,0x130),(0x66E3EC,0x3A0)]:checks[address]=pe_read(EXE.read_bytes(),address,size)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_conversion_score.py','native_unicorn_mapreader.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12];file=ROOT/'ghidra'/f'phase192-conversion-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=probe.cpu.run(EXE,args,list(checks.items()),probe.fixture.STOP,probe.fixture.WORLD,len(mem[probe.fixture.WORLD]))
        saved=dict(case=c,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    return args,checks,saved,file


def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c,r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);f=probe.fixture
    raw=checks[probe.RAW][16:32];bits=api.view(raw)['value_bits']
    eligible=c.get('mode',4)==4 and c.get('player_type',3)>=3
    score=api.project(c.get('score',10),bits,c.get('mode',4),c.get('player_type',3))
    world=bytearray(checks[f.WORLD]);f.put(world,0x72A2,score)
    assert post[f.WORLD]==world and bytes.fromhex(r['captured_hex'])==world
    assert post[0x6F29E9]==raw[4:8] and post[probe.RAW]==checks[probe.RAW]
    actor=post[f.ACTOR]
    assert int.from_bytes(actor[0x28:0x2A],'little')==3
    assert int.from_bytes(actor[0x2A:0x2E],'little')==f.OWNER+0x2000
    assert int.from_bytes(actor[0x40:0x44],'little')==1000
    assert actor[1343:]==checks[f.ACTOR][1343:]==bytes([165])*64
    assert int.from_bytes(post[probe.OLD_COUNTS][9:13],'little')==0 and int.from_bytes(post[probe.NEW_COUNTS][9:13],'little')==1
    for a in [f.RAW,0x6EE540,0x6ED540,0x66D524,0x66E3EC]:assert post[a]==checks[a],hex(a)
    assert int.from_bytes(post[f.OWNER+0x2000][0x16A:0x16E],'little')==1
    visits=r['visited_instructions']
    for a in [0x51CD90,0x4F2800,0x48D050]:assert visits.get(hex(a),0)==1
    assert visits.get('0x511650',0)==int(eligible)
    reads=[x for x in r['observed_reads'] if int(x['address'],16)==0x6F29E9]
    assert len(reads)==int(eligible) and all(x['pc']=='0x4f28b9' and x['size']==4 for x in reads)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    return post,dict(score_before_bits=c.get('score',10),raw_value_bits=bits,score_after_bits=score,scoring_gate=eligible,
                     new_owner_tag=3,new_owner_pointer=f.OWNER+0x2000,HP=1000,raw_and_guards_preserved=True,
                     original_conversion_count=1,native_steps=r['instruction_count'])


def main():
    assert sha(EXE.read_bytes())==api.DX_SHA256
    rows=[]
    for c in cases():
        a,ch,s,f=execute(c);_,out=verify(c,a,ch,s['result'])
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c['name'],out['score_before_bits'],'->',out['score_after_bits'],'gate',out['scoring_gate'],flush=True)
    dump('phase192-conversion-emulations.json',dict(phase=192,replays=rows))


if __name__=='__main__':main()
