"""Discovery: raw Trap definition -> real native factory -> optional activation.

Prepared World/pool/map/player, original allocator/constructor/registration.
Declared null graphics, sound lookup0 and soundplay token providers. No gamefile writes.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import emulate_object_factory as f
import native_unicorn_mapreader as cpu
from build_effect_semantics import EXE,pe_read
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
RAW,MID,LOOKUP,LOOKIO,STOP=0x18000000,0x18001000,0x18002000,0x18003000,0x18900000
sha=cpu.sha

def build(c):
    args,_=f.build(f.cases()[0],0)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions'] if (int(r['address'],16)<0x11000000 or int(r['address'],16)==0x7FFEF000) and int(r['address'],16)!=f.OBS}
    raw=bytearray(next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Traps.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Traps' for r in s.records if r[0x20C]==c.get('id',1)))
    if 'unknown' in c:f.put(raw,0x204,c['unknown'],2)
    if 'usage' in c:f.put(raw,0x202,c['usage'],2)
    if 'flags' in c:f.put(raw,0x1EC,c['flags'])
    mem[RAW]=raw+bytes([165])*64;mem[MID]=bytearray([165])*811;mem[f.DEFINITION]=bytearray([165])*273
    for off,value in [(0x15E4,f.DEFINITION),(0x15E8,1),(0x15EC,1),(0x9EA,32)]:f.put(mem[f.WORLD],off,value)
    f.put(mem[f.PLAYER],0x10D6,100);f.put(mem[f.PLAYER],0x10E2,1000);f.put(mem[f.PLAYER],0x1121,1)
    # Separate originalWorld scalar range beyond the prepared8000-byte prefix;
    # avoids overlapping the independently prepared Session/GFX provider objects.
    mem[f.WORLD+0xA360]=bytearray(32)
    for a,v in [(0x6F2590,20),(0x6FCDCF,0),(0x6F2548,8),(0x6E5054,f.WORLD)]:mem[a]=bytearray(struct.pack('<I',v))
    f.put(mem[f.SESSION],4,100)
    mem[0x740C20]=bytearray([165])*64;mem[LOOKIO]=bytearray(4)
    lookup=bytes.fromhex('ff05')+struct.pack('<I',LOOKIO)+bytes.fromhex('33c0c20800');mem[LOOKUP]=bytearray(lookup);f.put(mem[f.SV],0x1C,LOOKUP)
    calls=[dict(address='0x4aa330',registers=dict(ECX=hex(f.DEFINITION)),stack_args=[]),dict(address='0x570b60',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x570d10',registers=dict(ECX=hex(MID)),stack_args=[RAW]),dict(address='0x4aa440',registers=dict(ECX=hex(f.DEFINITION)),stack_args=[MID,0])]
    request=304;node=f.BLOCK+512-request+12
    factory=dict(address='0x4b7c10',registers=dict(ECX=hex(f.WORLD+8)),stack_args=[c.get('id',1),7,f.ARGS,f.OUT+4])
    if c.get('activate'):
        calls.append(factory)
        if c.get('twice'):calls.append(dict(address='0x4a96d0',registers=dict(ECX=hex(node)),stack_args=[]))
        entry='0x4a96d0';ecx=node;stack=[]
    else:entry=factory['address'];ecx=f.WORLD+8;stack=factory['stack_args']
    args.update(address=entry,registers=json.dumps(dict(ECX=hex(ecx))),stack_args=stack,before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=2000000,
        allowed_external_IO_code=[[a,len(mem[a])] for a in [f.GFX_CB,f.SOUND_CB,LOOKUP]],
        observe_reads=[[f.DEFINITION,209],[RAW,579],[MID,747],[0,4096]],
        observe_writes=[[f.BLOCK,len(mem[f.BLOCK])],[f.PLAYER,len(mem[f.PLAYER])],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [f.GFX_CB,f.SOUND_CB,LOOKUP,0x7FFEF000]}
    for a,n in [(0x66D754,0x60),(0x66D464,0x58),(0x66D4BC,0x68),(0x66E3EC,0x3A0)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks,node

def execute(c):
    args,ch,node=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend+json.dumps({hex(a):sha(b) for a,b in ch.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase161-trap-creation-probe-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(ch.items()),STOP,node,291));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,ch,node,saved,path

if __name__=='__main__':
    a,ch,node,s,p=execute(dict(name='base'));r=s['result'];print(p.name)
    print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','trace_tail','before_calls']}))
    if r['success']:
        post=post_context(ch,r);print('node',hex(node),'OUT',post[f.OUT].hex(),'trap',r['captured_hex']);print('playerdelta',[(i,post[f.PLAYER][i],ch[f.PLAYER][i]) for i in range(len(ch[f.PLAYER])) if post[f.PLAYER][i]!=ch[f.PLAYER][i]])
