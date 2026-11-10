"""Native raw Effects -> definition -> CUBE_GEN -> registered child instances."""
from pathlib import Path
import json,struct
import probe_effect_generation_scale as p
from emulate_resistance import post_context
from native_effect_generation_scale import cube_batches,scale_from_rate_dword
from emulate_element_colors import region,pack32

ROOT=Path(__file__).resolve().parent
sha=p.cpu.sha

def cases():
    rows=[dict(name=f'scaled6-{scale}',scale=scale,count=6) for scale in [0,50,127,128,254,255]]
    rows += [dict(name='unscaled6-zero',flags=0,scale=0,count=6),dict(name='unscaled6-full',flags=0,scale=255,count=6),
        dict(name='zero-count',scale=0,count=0),dict(name='one-count',scale=0,count=1),
        dict(name='max-count-low-scale',scale=0,count=255),dict(name='count12-min-live-scale',scale=50,count=12),
        dict(name='three-physical-slots',scale=127,count=6,slots=[294,0,294,0,0,0,0,294]),
        dict(name='random-slots',scale=127,count=6,flags=0x4400,slots=[294]*8),
        dict(name='random-first-zero',scale=127,count=6,flags=0x4400,slots=[0,294,0,0,0,0,0,0]),
        dict(name='zero-lifetime',scale=255,count=6,life=0),
        dict(name='misc-pair',scale=127,count=6,misc=[0x1234,0xFEDC]),
        dict(name='effect-target-zero-count',scale=0,count=0,flags=0x4040)]
    return rows

def execute(c):
    args,checks,saved,path=p.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path

def verify(c,args,checks,r):
    post=post_context(checks,r);base=p.base
    u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    raw=checks[p.ERAW];definition=post[base.PARENT_DEF];mid=post[p.EMID]
    assert post[p.ERAW]==raw and raw[0xF3]==c['count'] and raw[0xF2]==4
    for a,n in [(p.ERAW,246),(p.EMID,426),(base.PARENT_DEF,148),(p.element.RAW,182),(p.element.MID,290),(base.DEF,79)]:assert post[a][n:]==checks[a][n:]
    assert definition[0x8A]==raw[0xF3] and definition[0x85]==4
    assert u(definition,0x5F)==u(raw,0xB4)==c.get('flags',0x4000)
    assert definition[0x4F:0x5F]==raw[0xCA:0xDA]
    assert definition[0x4B:0x4F]==raw[0xEE:0xF2]
    assert mid[0x1A2:0x1A6]==raw[0xEE:0xF2]
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==9
    batches=cube_batches(c['count'],c.get('flags',0x4000),c['scale'])
    slots=c.get('slots',[294,0,0,0,0,0,0,0]);random=bool(c.get('flags',0x4000)&0x400)
    expected=(batches if random else batches*sum(x!=0 for x in slots)) if c.get('life',100)>0 else 0
    if random and slots[0]==0:expected=0
    n=r['visited_instructions'].get('0x4352e0',0);assert n==expected,(c['name'],n,expected)
    assert r['visited_instructions'].get('0x435f20',0)==n # complete native child initializer
    assert u(post[0x69793C],0)==4094-n and u(post[base.BRIDGE],0x135F)==300*n
    pool=post[p.POOL];assert u(pool,0)==p.POOL_SIZE-300*n
    assert pool[p.POOL_SIZE:]==bytes([165])*64
    # Follow the real linked child list and tag registry, not just CALL counts.
    ptr=u(post[base.PARENT],0x113);seen=[]
    while ptr:
        assert ptr not in seen and p.POOL<=ptr<=p.POOL+p.POOL_SIZE-287
        b=pool[ptr-p.POOL:ptr-p.POOL+287]
        assert u(b,0)==0x66CBDC and u(b,0x101,2)==294 and u(b,0x103)==base.DEF
        tag=u(b,0xF2,2);assert u(post[0x6D6458],4*tag)==ptr
        assert u(b,0x111,2)==2 and u(b,0x109)==128
        seen.append(ptr);ptr=u(b,0x117)
    assert len(seen)==n
    scale_reads=[x for x in r['observed_reads'] if int(x['address'],16)==0x69752C]
    scaling=bool(c.get('flags',0x4000)&0x4000) and not c.get('flags',0x4000)&0x40 and c.get('life',100)>0
    assert len(scale_reads)==int(scaling)
    assert all(int(x['pc'],16)==0x438D0A and x['size']==1 for x in scale_reads)
    assert int(r['registers']['EAX'],16)==0
    assert post[0x69752C]==checks[0x69752C]
    return post,dict(batches=batches,successful_child_creations=n,scale_reads=len(scale_reads),
        checked_regions=len(post),checked_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def rate_controls():
    rows=[]
    for value in [0,1,7,13,14,30,0xFFFFFFFF,0x80000000,0x7FFFFFFF]:
        memory=[region(0x75CA70,pack32(value)),region(0x69752C,bytes([165])),region(0x7FFEF000,bytes(8192))]
        args=dict(address='0x43b28b',memory=json.dumps(dict(regions=memory)),registers=json.dumps({}),max_steps=200)
        checks=[(0x75CA70,pack32(value)),(0x69752C,bytes([scale_from_rate_dword(value)]))]
        tag=sha(json.dumps(args,sort_keys=True).encode())[:12];path=ROOT/'ghidra'/f'phase141-scale-rate-fragment-{tag}.json'
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            r=p.cpu.run(p.EXE,args,checks,0x43B2C9,0x69752C,1)
            saved=dict(input=args,result=r,scope='Original canonical instruction fragment43B28B..43B2C9; stop before World tick continues. hit_return is backend stop flag, not a function RET.')
            path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        r=saved['result'];assert r['success'] and not r['context_mismatches'] and int(r['registers']['EIP'],16)==0x43B2C9
        rows.append(dict(input_dword=value,scale=scale_from_rate_dword(value),file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
    return rows

def main():
    rows=[];poststates={}
    for c in cases():
        args,checks,saved,path=execute(c);post,details=verify(c,args,checks,saved['result']);poststates[c['name']]=post
        rows.append(dict(case=c,summary=details,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
        print(c['name'],details['successful_child_creations'],'children verified',flush=True)
    # Misc words are loaded, but all other prepared memory must match a baseline.
    expected={a:bytearray(b) for a,b in poststates['scaled6-127'].items()}
    expected[p.ERAW][0xEE:0xF2]=bytes.fromhex('3412dcfe');expected[p.EMID][0x1A2:0x1A6]=bytes.fromhex('3412dcfe');expected[p.base.PARENT_DEF][0x4B:0x4F]=bytes.fromhex('3412dcfe')
    assert {a:bytes(b) for a,b in expected.items()}==poststates['misc-pair']
    fragments=rate_controls()
    (ROOT/'phase141-effect-scale-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,rate_fragment_controls=fragments,
        misc_pair_compared_regions=len(expected),misc_pair_compared_bytes=sum(map(len,expected.values()))),indent=2),encoding='utf8')
    print('Native chains',len(rows),'rate fragments',len(fragments),flush=True)

if __name__=='__main__':main()
