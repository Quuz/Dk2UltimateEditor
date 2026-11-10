"""Whole original light-to-render record paths with the real engine allocator.

Prepared CLight, not a newly executed raw Room loader or wall placement. Every
output byte is predicted, including allocator payload slack. No game function,
native vtable, instruction or external API replacement; only FS:[0] low reads.
"""
from pathlib import Path
import hashlib,json,struct
import native_unicorn_factorytrace as cpu
from build_effect_semantics import EXE,pe_read
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
LIGHT,ENGINE,HEAP,QUEUE,OBS=0x18000000,0x18010000,0x18020000,0x18030000,0x18040000
NODE=HEAP+20

def sha(b):return hashlib.sha256(b).hexdigest()
def p32(x):return struct.pack('<I',x)
def put(b,o,v,n=4):b[o:o+n]=int(v).to_bytes(n,'little')
def region(a,b):return dict(address=hex(a),hex=bytes(b).hex())
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]

def cases():
    return [dict(name=f'{kind}-{value}',kind=kind,intensity=value) for kind in ['point','other'] for value in [0,1,64,128,255]]+[
        dict(name='point-special',kind='point',intensity=128,flags=0x4000),
        dict(name='point-special-alt',kind='point',intensity=255,flags=0x4000,alternate=1),
        dict(name='other-alt',kind='other',intensity=255,alternate=1),
        dict(name='point-radius0',kind='point',intensity=255,radius=0),
        dict(name='other-radius0',kind='other',intensity=255,radius=0),
        dict(name='point-other-kind',kind='point',intensity=128,light_kind=1)]

def build(case):
    b=bytearray(326+64);b[326:]=bytes([165])*64
    put(b,0,0x66CC14);put(b,0xF4,case.get('light_kind',2))
    struct.pack_into('<3i',b,0xD6,43008,51200,8192)
    struct.pack_into('<3H',b,0x111,64,128,255)
    put(b,0x117,case.get('radius',8192));put(b,0x134,case.get('flags',0));b[0x145]=case['intensity']
    heap=bytearray([165])*192;heap[:20]=bytes(20);put(heap,12,128);put(heap,16,28,2);heap[19]=1
    allocator=bytearray(0x300);put(allocator,0xC,28);put(allocator,0xA0+4*28,HEAP)
    mem={LIGHT:b,ENGINE:bytes(0x300),HEAP:heap,QUEUE:bytes(0x80),
        0x765B00:allocator,0x760B80:p32(QUEUE),0x759AF8:p32(case.get('alternate',0)),
        0x7FFEF000:bytes(8192),0:bytes(4)}
    function=0x599960 if case['kind']=='point' else 0x599C60
    args=dict(program='DKII-DX.exe',address='0x4aa310',registers=json.dumps(dict(ECX='0',DF='0')),
        before_calls=[dict(address=hex(function),registers=dict(ECX=hex(ENGINE)),
            stack_args=[0,0,LIGHT,1,0] if case['kind']=='point' else [0,0,LIGHT,1])],
        memory=json.dumps(dict(regions=[region(a,z) for a,z in mem.items()])),max_steps=100000,
        observe_reads=[[LIGHT,len(b)],[0,4096]],observe_writes=[[HEAP,192],[QUEUE,128],[0x765B00,0x300],[0x66CC14,20]])
    expected={a:bytes(z) for a,z in mem.items() if a!=0x7FFEF000}
    active=case.get('light_kind',2)==2 and case.get('radius',8192)!=0
    if active:
        heap=bytearray(heap);heap[18]=1
        d=bytearray([165])*56
        put(d,0,0)
        kind=case['kind'];alt=case.get('alternate',0)
        mode=(0x14 if alt else 0x12) if kind=='point' and case.get('flags',0)&0x4000 else 4 if kind=='point' else 0x15 if alt else 0x11
        flags=mode
        if not flags&1:flags|=0x20
        flags|=0x80 if flags&3 else 0x40
        if not flags&4:flags|=0x100
        put(d,4,flags)
        constant=lambda a:struct.unpack('<f',pe_read(EXE.read_bytes(),a,4))[0]
        pos=[43008*constant(0x6704D0),51200*constant(0x6704D0),8192*constant(0x670504)]
        radius=case.get('radius',8192)*constant(0x6704D0)
        # The normalized color is explicitly stored asfloat before x255.
        rgb=[f32(v*constant(0x670520))*constant(0x670528) for v in [64,128,255]]
        floats=dict(zip([8,12,16],pos));floats.update(zip([20,24,28],rgb))
        floats.update({32:radius,36:radius*radius,40:1/(radius*radius),44:1,
            48:case['intensity']/128,52:case['intensity']/128})
        for off,v in floats.items():struct.pack_into('<f',d,off,v)
        heap[20:76]=d
        expected[HEAP]=bytes(heap);after=bytearray(allocator);put(after,0xA0+4*28,0);expected[0x765B00]=bytes(after)
        queue=bytearray(128);put(queue,0x34 if kind=='point' else 0x38,NODE);expected[QUEUE]=bytes(queue)
    expected[0x66CC14]=pe_read(EXE.read_bytes(),0x66CC14,20)
    return args,expected

def verify(case,args,expected,result):
    assert result['success'] and result['hit_return'] and not result['protected_write_or_execution_failures']
    assert len(result['before_calls'])==1 and result['before_calls'][0]['returned']
    assert int(result['registers']['EAX'],16)==0x36
    post=post_context(expected,result)
    assert post==expected,[(hex(a),[(i,x,y) for i,(x,y) in enumerate(zip(post[a],v)) if x!=y][:15]) for a,v in expected.items() if post[a]!=v]
    for r in result['observed_reads']:
        if int(r['address'],16)<4096:
            assert int(r['address'],16)==0 and r['size']==4
            pc=int(r['pc'],16);assert pe_read(EXE.read_bytes(),pc,1)==b'\x64'
    active=case.get('light_kind',2)==2 and case.get('radius',8192)!=0
    visits=result['visited_instructions']
    if active:assert all(hex(a) in visits for a in [0x57CEB0,0x57CD30,0x599BD0,0x571A90 if case['kind']=='point' else 0x571AB0])
    else:assert hex(0x57CEB0) not in visits
    return dict(created_render_record=active,normalized_intensity=case['intensity']/128 if active else None,
        record_bytes=56 if active else 0,queue_offset=0x34 if case['kind']=='point' else 0x38)

def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+(ROOT/'native_unicorn_factorytrace.py').read_text(encoding='utf8')).encode())[:12]
    return ROOT/'ghidra'/f'phase123-light-render-{tag}.json'

def main():
    rows=[]
    for c in cases():
        args,expected=build(c);path=path_for(args)
        result=cpu.run(EXE,args,list(expected.items()),OBS,HEAP,192)
        path.write_text(json.dumps(dict(scope=__doc__,case=c,input=args,result=result),indent=2),encoding='utf8')
        outcome=verify(c,args,expected,result)
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(c['name'],'passed',result['instruction_count'])
    (ROOT/'phase123-light-render-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':main()
