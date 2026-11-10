"""Discovery: whole raw EffectElement loader -> native spawn -> complete tick.

Original bridge/element code and vtables retained. External connected-engine
queries and resource lookup are explicit providers. Unknown bytes remain opaque.
"""
from pathlib import Path
import json,struct,sys
import dk2_formats as fmt
import emulate_element_initialization as base
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from build_effect_semantics import EXE
import native_unicorn_factorytrace as cpu
import emulate_element_graphics as graphics

ROOT=Path(__file__).resolve().parent
RAW,MID,GFX,GV,GCB,IO,STOP=0x10100000,0x10110000,0x10120000,0x10121000,0x10122000,0x10130000,0x10140000

def build(ident=294,palette=(0,0),ticks=0,spawn=True,render=True,rgb=None):
    c=dict(base.cases()[0],id=ident,id_argument=ident)
    inherited,_=base.build(c,0)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(inherited['memory'])['regions'] if int(r['address'],16)<0x11000000 or int(r['address'],16)==0x7FFEF000}
    memory.pop(base.OBS)
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/EffectElements.kwd'
    raw=next(r for s in fmt.parse(source.read_bytes()) if s.signature==250 for r in s.records if int.from_bytes(r[0xA0:0xA2],'little')==ident)
    raw=bytearray(raw);raw[0xB1:0xB3]=bytes(palette)
    if rgb is not None:raw[0xAE:0xB1]=bytes(rgb)
    memory[RAW]=raw+bytes([165])*64;memory[MID]=bytearray([165])*(290+64);memory[base.DEF]=bytearray([165])*(79+64)
    gv=bytearray(0x58);struct.pack_into('<I',gv,0x54,GCB);memory[GFX]=bytearray(pack32(GV));memory[GV]=gv;memory[IO]=bytearray(4)
    # External resource catalogue accepts the name/type request and returns a
    # declared handle; original504D80 derives runtime descriptor from raw union.
    memory[GCB]=bytearray(bytes.fromhex('ff05')+pack32(IO)+bytes.fromhex('8b442410c70034120000b801000000c21800'))
    memory[0x6D6321]=bytearray(4)
    calls=[dict(address='0x436630',registers=dict(ECX=hex(base.DEF)),stack_args=[]),
        dict(address='0x56b460',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x56b570',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x4366a0',registers=dict(ECX=hex(base.DEF)),stack_args=[MID,GFX])]
    if spawn:calls.append(dict(address='0x4352e0',registers=dict(ECX=hex(base.PARENT)),stack_args=[ident,base.ARGS,base.ARGS+12,base.OUT+4]))
    if ticks:calls.append(dict(address='0x436430',registers=dict(ECX=hex(base.NODE)),stack_args=[ticks]))
    if spawn and render:
        ga=graphics.build(next(c for c in graphics.cases() if c['name']=='source-quad-base'))
        gm={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(ga['memory'])['regions']}
        for a in [graphics.ENGINE,graphics.HEAP,graphics.QUEUE,0x765B00,0x760B80,0x760B7C,0x759ADC,0x759ACC,0x795704]:memory[a]=gm[a]
        calls.append(dict(address='0x599c60',registers=dict(ECX=hex(graphics.ENGINE)),stack_args=[0,0,base.NODE,0]))
    args=dict(program='DKII-DX.exe',address='0x517490',registers=json.dumps(dict(ECX=hex(base.BRIDGE))),before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),max_steps=800000,
        observe_reads=[[RAW,182],[MID,290],[base.DEF,79],[0,4096]],observe_writes=[[base.BLOCK,len(memory[base.BLOCK])],[base.DEF,143],[0,4096]],
        allowed_external_IO_code=[[a,len(memory[a])] for a in [base.ALLOW,base.BLOCKED,base.TOUCH,base.FLOOR,GCB]])
    checks={a:bytes(b) for a,b in memory.items() if a not in [base.ALLOW,base.BLOCKED,base.TOUCH,base.FLOOR,GCB,0x7FFEF000]}
    return args,checks

def execute(ident=294,palette=(0,0),ticks=0,spawn=True,render=True,rgb=None,verbose=True):
    args,checks=build(ident,palette,ticks,spawn,render,rgb);key=cpu.sha(json.dumps(args,sort_keys=True).encode())[:12]
    path=ROOT/'ghidra'/f'phase140-element-discovery-{key}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),STOP,base.NODE,287)
        saved=dict(input=args,ident=ident,palette=palette,ticks=ticks,spawn=spawn,render=render,rgb=rgb,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps(dict(success=r['success'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],calls=r['before_calls'])),flush=True)
    if r['success'] and verbose:
        post=post_context(checks,r);node=bytes.fromhex(r['captured_hex'])
        print('DEF',post[base.DEF][:79].hex(),'HP',int.from_bytes(node[0x109:0x10D],'little'),'COLOR',node[0x6e:0x72].hex(),'POS',node[0xD6:0xE2].hex(),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute(ticks=1 if 'tick' in sys.argv else 0,spawn='loader' not in sys.argv)
