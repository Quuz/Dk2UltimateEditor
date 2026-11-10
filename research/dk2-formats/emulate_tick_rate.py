"""Actual registered Variables19 callback -> original collision/Freeze consumer."""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_target_combinations as fixture
import native_unicorn_mapreader as cpu
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE,sha=fixture.EXE,cpu.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
RAW,DISPATCH,TABLE,ROW=0x1E400000,0x1E410000,0x6A5CD0,0x6A5E38
TARGET=fixture.p.target.attack.TARGET


def cases():
    return [dict(name=name,tps=tps,duration=duration) for name,tps,duration in [
        ('zero',0,5),('one',1,5),('four',4,5),('twenty',20,5),('thirty',30,5),
        ('sixty',60,5),('u16-max',65535,5),('u16-plus1',65536,5),('all-ones',0xFFFFFFFF,5),
        ('high-bit',0x80000000,5),('duration-zero',20,0),('duration80',20,80)]]


def build(c):
    args,_=fixture.build(dict(process=11,mode=0,data1=c['duration'],data2=0,mask=4,HP=1000,root='collision'))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    mem[RAW]=bytearray(bytes(16)+struct.pack('<4I',19,c['tps'],0x11223344,0x55667788)+bytes([165])*64)
    code=b'\x68'+struct.pack('<I',RAW)+b'\xa1'+struct.pack('<I',ROW+4)+bytes.fromhex('ffd0c3')
    mem[DISPATCH]=bytearray(code)
    calls=args['before_calls'];assert calls[-1]['address']=='0x4abd60'
    calls.insert(0,dict(address='0x51aa90',registers=dict(ECX='0'),stack_args=[]))
    calls.insert(-1,dict(address=hex(DISPATCH),registers=dict(ECX='0'),stack_args=[]))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['allowed_external_IO_code'].append([DISPATCH,len(code)])
    args['observe_reads'] += [[0x6F2590,4],[0,4096]]
    args['observe_writes'] += [[0x6F2590,4],[TARGET+0x447,2],[0,4096]]
    excluded=[0x7FFEE000,fixture.p.f.GFX_CB,fixture.p.f.SOUND_CB,fixture.p.target.attack.EVENT,fixture.p.target.LOAD_GFX,fixture.p.target.LOAD_SOUND,0x28CFA4,DISPATCH]
    ch={a:bytes(b) for a,b in mem.items() if a not in excluded}
    ch[TABLE]=fixture.p.pe_read(EXE.read_bytes(),TABLE,0x1064)
    return args,ch


def execute(c):
    args,ch=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_mapreader.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase195-tps-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),fixture.p.f.OBS,TARGET,len(ch[TARGET]));saved=dict(case=c,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==len(args['before_calls'])
    post=post_context(ch,r);u=lambda a,o,n=4:int.from_bytes(post[a][o:o+n],'little')
    timer=(c['tps']*c['duration'])&65535
    assert u(TARGET,0x40)==1000 and u(TARGET,0x447,2)==timer,(c['name'],u(TARGET,0x447,2),timer)
    assert post[RAW]==ch[RAW]
    for a in [fixture.p.shot.RAW,fixture.p.target.RAW,fixture.p.RAWSPELL]:assert post[a]==ch[a]
    stores=[x for x in r['observed_writes'] if int(x['address'],16)==0x6F2590]
    assert len(stores)==1 and stores[0]['pc']=='0x51cdca' and stores[0]['value']==c['tps'],stores
    consumes=[x for x in r['observed_reads'] if x['pc']=='0x4ae238']
    assert len(consumes)==1 and int.from_bytes(bytes.fromhex(consumes[0]['value_hex']),'little')==c['tps']
    row=post[TABLE][ROW-TABLE:ROW-TABLE+20]
    assert int.from_bytes(row[:4],'little')==19 and int.from_bytes(row[4:8],'little')==0x51CD90
    for pc in [0x51AA90,0x51CD90,0x4ABD60,0x4AE1D0,0x48CB60]:assert r['visited_instructions'].get(hex(pc))==1,hex(pc)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(fixture.p.pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    return post,dict(runtime_tps_bits=c['tps'],duration=c['duration'],freeze_timer=timer,HP=1000,
        actual_registered_callback=True,native_steps=r['instruction_count'])


def main():
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for c in selected:
        args,ch,saved,file=execute(c);_,out=verify(c,args,ch,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out,flush=True)
    if len(selected)==len(cases()):dump('phase195-tps-emulations.json',dict(phase=195,replays=rows))


if __name__=='__main__':main()
