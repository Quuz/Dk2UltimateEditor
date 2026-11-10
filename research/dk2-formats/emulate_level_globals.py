"""Full original rawLevel converter and Level callback, with actual Variables19.

Preserves all game code/IAT/vtables. A bounded ASCII-only Win32 conversion
provider and the existing scene's explicitly synthetic graphics provider are IO.
"""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_tick_rate as prior
import native_unicorn_mapreader as cpu
import native_level_descriptor as level
import dk2_formats as fmt
from emulate_element_colors import region
from emulate_resistance import post_context
from probe_melee_animation_flag import coalesce_world
ROOT,EXE,sha=prior.ROOT,prior.EXE,prior.sha
load,dump=prior.load,prior.dump
RAWLEVEL,MID,OUT,LEVEL_GFX,LEVEL_SOUND=0x1E500000,0x1E510000,0x1E520000,0x1E530000,0x1E540000
SOUNDOBJ,SOUNDVT,RESTORE=0x1E560000,0x1E570000,0x1E580000
SESSION=0x1E590000
WIDE=0x28C410
WORLD=prior.fixture.p.f.WORLD
SOURCE=ROOT.parents[1]/'Dungeon Keeper 2/Data/editor/AutoSave.kwd'


def cases():
    return [dict(name=n,tps=t,common=v,order=o,flags=f,messages=m,sound=s) for n,t,v,o,f,m,s in [
        ('level-only4',4,None,'level',0,False,False),
        ('common30-level4',4,30,'common-level',0,False,False),
        ('level4-common30',4,30,'level-common',0,False,False),
        ('common-high-level-max',65535,0xFFFFFFFF,'common-level',0xFFFF,False,False),
        ('level0-common-high',0,0xFFFFFFFF,'level-common',0x8000,False,False),
        ('flags-message20',20,60,'common-level',0x1554,True,False),
        ('flags-message60',60,4,'level-common',0x0AA8,True,False),
        ('sound-source4',4,30,'common-level',0xFFFF,True,True),
        ('sound-source60',60,4,'level-common',0x0003,True,True)]]


def ascii_provider():
    # stdcall WideCharToMultiByte(0,0,src,-1,dst,cap,0,0), prevalidated ASCII.
    code=bytearray(bytes.fromhex('5657538b7424188b7c24208b5c242431c0'))
    loop=len(code)
    code+=bytes.fromhex('0fb70e83c6023bc3730e880f474085c9')
    code+=b'\x75'+bytes([(loop-(len(code)+2))&255])
    code+=bytes.fromhex('5b5f5ec2200031c05b5f5ec22000')
    return bytes(code)


def build(c):
    args,_=prior.build(dict(tps=c['common'] or 20,duration=5))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    coalesce_world(mem)
    # Inherited fixture placed Session inside unused World message storage.
    # Relocate this external input before exercising all20 message writes.
    oldsession=prior.fixture.p.f.SESSION
    mem[SESSION]=bytearray(mem[WORLD][oldsession-WORLD:oldsession-WORLD+0x280])
    struct.pack_into('<I',mem[WORLD],4,SESSION)
    section=next(s for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0xDC)
    raw=bytearray(section.data[:int.from_bytes(section.header[-4:],'little')]);assert len(raw)==level.SIZE
    struct.pack_into('<H',raw,0x1182,c['tps']);struct.pack_into('<H',raw,0x638C,c['flags'])
    raw[0x63B7]=200  # byte delivered to both original bonus/runtime helpers; disabled fixture subsystem.
    # Distinguishable values for every scalar/array copied by51E6D0.
    for off,n in [(0x63AE,27),(0x63CB,40)]:raw[off:off+n]=bytes((i*13+0x83)&255 for i in range(n))
    raw[0x63B7]=200
    for i in range(20):
        text=(f'Message{i:02d}-DK2').encode('utf-16le')+b'\0\0'
        raw[0x138C+0x400*i:0x138C+0x400*i+1024]=text+bytes(1024-len(text))
    hero='Keeper\u03a9'.encode('utf-16le')+b'\0\0';raw[0x6413:0x6413+64]=hero+bytes(64-len(hero))
    raw[0x638E:0x63AE]=b'DK2'+bytes(29)
    mem[RAWLEVEL]=raw;mem[MID]=bytearray(bytes([0xA5])*level.MID_SIZE)
    struct.pack_into('<H',mem[MID],0x64C0,0)  # converter ORs flags with its seed.
    mem[OUT]=bytearray(32)
    mem[0x6F2957]=bytearray(struct.pack('<I',0xA5A5F800)+bytes([0xA5])*3)
    mem[LEVEL_GFX]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',OUT)+bytes.fromhex('8b442404a3')+struct.pack('<I',OUT+4)+bytes.fromhex('33c0c20400'))
    mem[LEVEL_SOUND]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',OUT+8)+bytes.fromhex('8b442404a3')+struct.pack('<I',OUT+12)+b'\xb8'+struct.pack('<I',0x12345678)+bytes.fromhex('c20800'))
    p=prior.fixture.p;struct.pack_into('<I',mem[p.f.GV],0x84,LEVEL_GFX)
    mem[SOUNDOBJ]=bytearray(struct.pack('<I',SOUNDVT));mem[SOUNDVT]=bytearray(0x24)
    struct.pack_into('<I',mem[SOUNDVT],0x1C,LEVEL_SOUND)
    # Actual external-import target from the unpatched PE IAT, not a game stub.
    assert p.pe_read(EXE.read_bytes(),0x66C128,4)==struct.pack('<I',WIDE)
    mem[WIDE]=bytearray(ascii_provider())
    # Native converter marks all presence slots1. A declared caller then
    # chooses message/sound-presence branches; no original instruction edits.
    patch=b''
    for off,val in [(0x6C,int(c['sound']))]+[(0x1C+4*i,int(c['messages'])) for i in range(20)]:
        patch+=bytes.fromhex('c705')+struct.pack('<II',MID+off,val)
    patch+=bytes.fromhex('c705')+struct.pack('<II',0x75B468,SOUNDOBJ)+b'\xc3';PATCH=0x1E550000;mem[PATCH]=bytearray(patch)
    mem[RESTORE]=bytearray(bytes.fromhex('c705')+struct.pack('<II',0x75B468,p.f.SOUND)+b'\xc3')
    calls=args['before_calls'];dispatch=next(x for x in calls if x['address']==hex(prior.DISPATCH))
    calls.remove(dispatch);collision=calls.pop();assert collision['address']=='0x4abd60'
    converter=dict(address='0x56c100',registers=dict(ECX=hex(MID)),stack_args=[RAWLEVEL])
    levelcall=dict(address='0x51e6d0',registers=dict(ECX='0'),stack_args=[MID,WORLD])
    restorecall=dict(address=hex(RESTORE),registers=dict(ECX='0'),stack_args=[])
    patchcall=dict(address=hex(PATCH),registers=dict(ECX='0'),stack_args=[])
    calls += [converter,patchcall]
    if c['common'] is None:calls += [levelcall,restorecall]
    elif c['order']=='common-level':calls += [dispatch,levelcall,restorecall]
    else:calls += [levelcall,restorecall,dispatch]
    calls.append(collision)
    args['before_calls']=calls
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['allowed_external_IO_code'] += [[LEVEL_GFX,len(mem[LEVEL_GFX])],[LEVEL_SOUND,len(mem[LEVEL_SOUND])],[WIDE,len(mem[WIDE])],[PATCH,len(patch)],[RESTORE,len(mem[RESTORE])]]
    args['observe_reads'] += [[MID,level.MID_SIZE],[0x66C128,4]]
    args['observe_writes'] += [[WORLD+0x72E7,4],[WORLD+0x7209,4],[0x6F2957,7],[MID,0x134]]
    excluded=[0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,0x28CFA4,prior.DISPATCH,LEVEL_GFX,LEVEL_SOUND,WIDE,PATCH,RESTORE]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[prior.TABLE]=p.pe_read(EXE.read_bytes(),prior.TABLE,0x1064)
    base,image=cpu.pe_image(EXE.read_bytes())
    for a,n in [(0x6F2590,4),(0x6F2957,7),(0x7415B0,12)]:
        initial=bytearray(image[a-base:a-base+n])
        for at,blob in mem.items():
            lo,hi=max(a,at),min(a+n,at+len(blob))
            if hi>lo:initial[lo-a:hi-a]=blob[lo-at:hi-at]
        checks[a]=bytes(initial)
    return args,checks


def execute(c):
    args,ch=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_mapreader.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase196-Level-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),prior.fixture.p.f.OBS,prior.TARGET,len(ch[prior.TARGET]));saved=dict(case=c,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(ch,r);want=bytearray(level.intermediate(ch[RAWLEVEL],ch[MID]))
    for off,val in [(0x6C,int(c['sound']))]+[(0x1C+4*i,int(c['messages'])) for i in range(20)]:struct.pack_into('<I',want,off,val)
    assert post[MID]==want and post[RAWLEVEL]==ch[RAWLEVEL]
    u=lambda blob,off,n=4:int.from_bytes(blob[off:off+n],'little')
    world=post[WORLD];assert u(world,0x7310,2)==u(want,0x12B4,2)
    hero='Keeper\u03a9'.encode('utf-16le')+b'\0\0'
    assert world[0x9B24:0x9B24+len(hero)]==hero
    for i in range(20):
        off=0x7324+512*i;text=f'Message{i:02d}-DK2'.encode()+bytes(512-len(f'Message{i:02d}-DK2'))
        assert world[off:off+512]==(text if c['messages'] else ch[WORLD][off:off+512])
    assert u(world,0x72E7)==(u(ch[WORLD],0x72E7)+sum(x if x<128 else x-256 for x in want))&0xFFFFFFFF
    assert u(post[OUT],0)==1 and u(post[OUT],4)==MID+0x6517
    assert u(post[OUT],8)==int(c['sound'])
    if c['sound']:assert u(world,0x7209)==0x12345678
    else:assert world[0x7209:0x720D]==ch[WORLD][0x7209:0x720D]
    # Preserve high21 bits while replacing the11 Level flag bits.
    expected_flags=(u(ch[0x6F2957],0)&~0x7FF)|((c['flags']>>2)&0x7FF)
    assert u(post[0x6F2957],0)==expected_flags
    for mid,glob in [(0x64E2,4),(0x6537,5),(0x653B,6)]:assert post[0x6F2957][glob]==want[mid]
    for mo,wo,n in [(0x64E3,0x9B80,4),(0x6539,0x9B84,1),(0x64E7,0x9B86,4),(0x653A,0x9B8A,1),(0x64FF,0x9B8C,8),(0x6507,0x9B94,16),(0x653D,0x9BA4,10),(0x64EB,0x720D,1),(0x64FD,0x722E,1),(0x64FE,0x722F,1)]:assert world[wo:wo+n]==want[mo:mo+n],(hex(mo),hex(wo))
    for mo,wo in [(0x64ED,0x9B64),(0x64EF,0x9B68),(0x64F3,0x9B70),(0x64F9,0x9B74),(0x64FB,0x9B78),(0x64F7,0x9B7C)]:assert u(world,wo)==u(want,mo,2)
    effective=c['common'] if c['common'] is not None and c['order']=='level-common' else c['tps']
    timer=(effective*5)&65535;assert u(post[prior.TARGET],0x447,2)==timer and u(post[prior.TARGET],0x40)==1000
    stores=[(x['pc'],x['value']) for x in r['observed_writes'] if int(x['address'],16)==0x6F2590]
    expected=[('0x51e708',c['tps'])]
    if c['common'] is not None:
        common=('0x51cdca',c['common']);expected=[common]+expected if c['order']=='common-level' else expected+[common]
    assert stores==expected,(c['name'],stores,expected)
    assert r['visited_instructions'].get('0x56c100')==r['visited_instructions'].get('0x51e6d0')==1
    assert r['visited_instructions'].get('0x5557e0',0)==20*int(c['messages'])
    assert r['visited_instructions'].get('0x553780')==r['visited_instructions'].get('0x553800')==1
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(prior.fixture.p.pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    return post,dict(effective_tps=effective,freeze_timer=timer,HP=1000,checksum=u(world,0x72E7),runtime_flag_bits=expected_flags,
        actual_messages=20*int(c['messages']),sound_provider_calls=int(c['sound']),runtime_TPS_stores=stores,regions=len(post),bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])


def main():
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for c in selected:
        args,ch,saved,file=execute(c);_,out=verify(c,args,ch,saved['result']);rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out,flush=True)
    if len(selected)==len(cases()):dump('phase196-Level-emulations.json',dict(phase=196,replays=rows))


if __name__=='__main__':main()
