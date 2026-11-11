"""Fresh rawCreature loader + actual Variables file + native speed/context.

Extends phase92 only at missing health/terrain/composed-parser boundaries.
Prepared factory Actor/World/tile state; no actual movement/scheduler claimed.
"""
from pathlib import Path
import json,struct
import probe_creature_work_level as prior
import emulate_variables_reader as files
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT,EXE,sha=prior.ROOT,prior.EXE,prior.sha
ACTOR,DEFINITION,WORLD,VARS=prior.cf.ACTOR,prior.cf.DEFINITION,prior.f.WORLD,prior.sp.VARS
STOP=0x1F900000
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,x:(ROOT/n).write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf8')


def build(c):
    args,_=prior.build(dict(name=c['name'],increment=0,level=c.get('level',1)))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=mem[prior.RAW]
    for off,key,default in [(0xEB4,'walk',109),(0xEB8,'run',301),(0xD37,'shuffle',53)]:struct.pack_into('<I',raw,off,c.get(key+'_raw',c.get(key,default)*20)&0xffffffff)
    flags=struct.unpack_from('<I',raw,0xEFC)[0];struct.pack_into('<I',raw,0xEFC,(flags&~1)|(1 if c.get('definition_bit0') else 0))
    actor=mem[ACTOR];struct.pack_into('<i',actor,0x40,c.get('HP',1000));struct.pack_into('<I',actor,0x3E6,0x100 if c.get('status100') else 0)
    if c.get('other_flag'):actor[0x3EA]|=8
    else:actor[0x3EA]&=~8
    terrain=struct.unpack('<I',mem[0x6DA4A8])[0];struct.pack_into('<I',mem[terrain],0x17B,c.get('terrain',0))
    # Capture-profile prepares actual file/stream/parser, with no other actors.
    records=([[65,1,c['growth']&0xffffffff,c.get('level',1)]] if 'growth' in c else [])+[[224,21,c.get('percent',200)&0xffffffff,10]]
    fc=files.cases()[0].copy();fc.update(name=c['name'],profile='capture',records=records,source=None)
    fa,_=files.build(fc);layer={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(fa['memory'])['regions']}
    layer.pop(0x7FFEE000);assert not set(mem)&set(layer),[hex(a) for a in set(mem)&set(layer)]
    mem.update(layer);table,reg=files.registration();mem[WORLD][0x1F24:0x1F24+files.TABLE_BYTES]=table
    struct.pack_into('<II',mem[files.CFG],0xD8,0x521790,WORLD)
    args['before_calls']+=fa['before_calls']+[dict(address='0x56cbd0',registers=dict(ECX='0'),stack_args=[files.REF,files.CFG])]
    if c.get('possessed',True):args['before_calls'].append(dict(address='0x48dea0',registers=dict(ECX=hex(ACTOR)),stack_args=[1]))
    args.update(address='0x48abb0',registers=json.dumps(dict(ECX=hex(ACTOR))),stack_args=[c.get('mode',0)])
    args['allowed_external_IO_code']+=fa['allowed_external_IO_code']
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[VARS,4096],[terrain+0x17B,4],[ACTOR+0x3E6,8],[ACTOR+0x40,4]]
    args['observe_writes'] += [[VARS,4096],[WORLD+0x72E7,4]]
    excluded={0x7FFEE000}|{a for a,_ in args['allowed_external_IO_code']}
    ch={a:bytes(b) for a,b in mem.items() if a not in excluded}
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,ch,dict(terrain=terrain,registration=reg,records=records,raw_variable=files.raw_file(fc).hex())


def execute(c):
    args,ch,meta=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha(Path(prior.cpu.__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase201-speed-context-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:saved=dict(case=c,input=args,result=prior.cpu.run(EXE,args,list(ch.items()),STOP,ACTOR,len(ch[ACTOR])));dump(str(file.relative_to(ROOT)),saved)
    return args,ch,meta,saved,file

