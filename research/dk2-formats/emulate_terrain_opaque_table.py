"""Raw Terrain16WORD table through full DX/old definition loaders.

Prepared null graphics and null sound lookup at declared external boundary.
Diagnostic raw flags clear3000h resource-special branches; remaining original
ClaimedPath8 bytes retained except selected16WORD table. Exact EXE profiles.
"""
from pathlib import Path
import json,struct,sys
import dk2_formats as fmt
import native_unicorn_factorytrace as cpu
import native_terrain_catalogue as layout
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE as DX

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd'
RAW,MID,DEF,SCRATCH,SOUND,SV,SCB,IO,STOP=[0x18000000+i*0x10000 for i in range(9)]
PROFILES={'DX':dict(exe=DX,program='DKII-DX.exe',ctor=0x4F6570,init=0x56EB60,convert=0x56ED20,
    load=0x4F66E0,root=0x504D60,sound_global=0x75B468,sound_temp=0x740C20,checksum_PC=0x4F670E),
    'old':dict(exe=ROOT/'source-binaries/DKII-local13.exe',program='DKII-local13.exe',ctor=0x4EB1B0,
        init=0x564E60,convert=0x565020,load=0x4EB330,root=0x4F9800,sound_global=0x74B100,
        sound_temp=0x730A80,checksum_PC=0x4EB355)}
VALUES={'zero':[0]*16,'ascending':[0x1100+257*i for i in range(16)],'reversed':list(reversed([0x1100+257*i for i in range(16)])),
    'highbit':[0x8000+0x41*i for i in range(16)]}

def sha(b):return __import__('hashlib').sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')


def cases():
    return [dict(name=f'{profile}-{gate}-{values}',profile=profile,gate=gate,values=values)
        for profile in PROFILES for gate in ([0,1] if profile=='DX' else [0]) for values in VALUES]


def build(case):
    p=PROFILES[case['profile']]
    raw=bytearray(next(r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0x6E for r in s.records if r[0x1D6]==8))
    put(raw,0x190,int.from_bytes(raw[0x190:0x194],'little')&~0x3000)
    struct.pack_into('<16H',raw,0x1AE,*VALUES[case['values']])
    vt=bytearray(32);put(vt,0x1C,SCB)
    cb=bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('33c0c20800')
    memory={RAW:raw+bytes([165])*64,MID:bytes([165])*800,DEF:bytes([165])*524,SCRATCH:bytes(32),
        SOUND:struct.pack('<I',SV),SV:vt,SCB:cb,IO:bytes(4),p['sound_global']:struct.pack('<I',SOUND),
        p['sound_temp']:bytes(64),0x7FFEE000:bytes([165])*0x4000}
    if case['profile']=='DX':memory[0x6FCDCF]=struct.pack('<I',case['gate'])
    calls=[dict(address=hex(p['ctor']),registers={'ECX':hex(DEF)},stack_args=[]),
        dict(address=hex(p['init']),registers={'ECX':hex(MID)},stack_args=[]),
        dict(address=hex(p['convert']),registers={'ECX':hex(MID)},stack_args=[RAW]),
        dict(address=hex(p['load']),registers={'ECX':hex(DEF)},stack_args=[MID,0])]
    args=dict(program=p['program'],address=hex(p['root']),registers=json.dumps({'ECX':hex(SCRATCH)}),
        before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),max_steps=300000,
        observe_reads=[[RAW,len(memory[RAW])],[MID,800],[DEF,524],[0,4096]],
        observe_writes=[[DEF,524],[p['sound_temp'],64],[0x400000,0x3B0000]],allowed_external_IO_code=[[SCB,len(cb)]])
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,{a:bytes(b) for a,b in memory.items() if a not in [SCB,0x7FFEE000]}


def definition_expected(mid,gate):
    out=bytearray(460);out[0x84:0x88]=bytes([165])*4;out[0x1CB]=164
    if not gate:put(out,0x84,sum(mid))
    for _,a,p,b,n in layout.SCALARS:
        if int.from_bytes(mid[p:p+4],'little'):out[b:b+min(n,2 if b==0x17F else n)]=mid[a:a+min(n,2 if b==0x17F else n)]
    # Declared sound provider returns0. All graphic providers absent.
    if int.from_bytes(mid[0xB0:0xB4],'little'):out[0x17B:0x17F]=mid[0x248:0x24C]
    flags=int.from_bytes(out[0x17B:0x17F],'little')
    for _,a,p,b,m in layout.COLOURS:
        v=mid[a] if int.from_bytes(mid[p:p+4],'little') else int.from_bytes(out[b:b+2],'little')
        put(out,b,v+(255 if flags&m else 0),2)
    if int.from_bytes(mid[0x20:0x24],'little'):
        out[0x6C:0x84]=mid[0x228:0x240]
        if any(mid[0x23C:0x23F]):put(out,0x7C,int.from_bytes(out[0x7C:0x80],'little')|1)
    bit=int(not(flags&4) and int.from_bytes(out[0x1AB:0x1AD],'little')<int.from_bytes(out[0x1AD:0x1AF],'little') and out[0x1C4]!=out[0x1C5])
    out[0x1CB]=(out[0x1CB]&0xFC)|bit
    return bytes(out)


def path_for(args):
    key=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase134-Terrain-table-native-{key}.json'


def verify(case,args,checks,result):
    p=PROFILES[case['profile']]
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures'] and result['function_or_vtable_substitutions']==0
    assert len(result['before_calls'])==4 and all(c['returned'] for c in result['before_calls'])
    post=post_context(checks,result);raw=checks[RAW][:552];mid=layout.intermediate(raw)
    assert post[RAW]==checks[RAW] and post[MID]==mid+checks[MID][736:]
    expected=definition_expected(mid,case['gate'])
    assert post[DEF]==expected+checks[DEF][460:],case['name']
    table_reads=[r for r in result['observed_reads'] if int(r['address'],16)<MID+0x286 and int(r['address'],16)+r['size']>MID+0x266]
    assert len(table_reads)==(0 if case['gate'] else 32)
    assert all(int(r['pc'],16)==p['checksum_PC'] and r['size']==1 for r in table_reads)
    presence=[r for r in result['observed_reads'] if int(r['address'],16)<MID+0xB8 and int(r['address'],16)+r['size']>MID+0xB4]
    assert len(presence)==(0 if case['gate'] else 4) and all(int(r['pc'],16)==p['checksum_PC'] for r in presence)
    assert post[IO]==(0 if case['gate'] else 2).to_bytes(4,'little')
    assert not [r for r in result['observed_reads'] if int(r['address'],16)<4096]
    return dict(profile=case['profile'],table_word_values=VALUES[case['values']],table_read_bytes=len(table_reads),
        table_read_only_in_checksum=True,presence_read_only_in_checksum=True,intermediate_table_preserved=True,
        complete_definition_bytes_verified=460,checksum=int.from_bytes(expected[0x84:0x88],'little'),
        sound_lookups=int.from_bytes(post[IO],'little'),instructions=result['instruction_count'],
        entire_definition_sha256=sha(expected),definition_without_checksum_sha256=sha(expected[:0x84]+expected[0x88:]),
        whole_prepared_regions=len(post),whole_prepared_bytes=sum(map(len,post.values())))


if __name__=='__main__':
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for case in selected:
        args,checks=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=cpu.run(PROFILES[case['profile']]['exe'],args,list(checks.items()),STOP,DEF,524)
            saved=dict(case=case,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        if not saved['result']['success']:print(case['name'],saved['result']['emulation_error'],saved['result']['trace_tail'],flush=True)
        outcome=verify(case,args,checks,saved['result'])
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified checksum',outcome['checksum'],'table reads',outcome['table_read_bytes'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase134-Terrain-table-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
