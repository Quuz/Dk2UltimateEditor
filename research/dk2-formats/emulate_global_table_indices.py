"""Real Variables reader + World callback + unchecked65/224 table handlers.

Inherited native registration/definition/parser setup and explicit virtual file
providers; no installed file writes, original code replacement or IAT rebinding.
"""
from pathlib import Path
import json,struct
import emulate_variables_reader as old
ROOT,EXE,sha,load,dump=old.ROOT,old.EXE,old.sha,old.load,old.dump
BASE=0x6F2550


def cases():
    rows=[]
    def add(name,records):
        c=next(c.copy() for c in old.cases() if c['name']=='runtime-unknown29-186')
        c.update(name=name,records=records);rows.append(c)
    add('baseline',[])
    samples=[('growth-health-level2',[65,1,150,2]),('growth-last-slot',[65,31,0x12345678,10]),
        ('growth-level0',[65,0,0xABCD,0]),('growth-level1',[65,0,0xABCD,1]),
        ('growth-level11-crosses-possession',[65,31,777,11]),('growth-stat32-crosses-possession',[65,32,777,2]),
        ('growth-wrap-to-unknown29',[65,0,0xBEEF,(-47)&0xffffffff]),
        ('growth-overlaps-unknown186-edge',[65,0,0xBEEF,421]),('growth-overlaps-unknown186-middle',[65,0,0xBEEF,422]),
        ('growth-overlaps-unknown186-last',[65,0,0xBEEF,423]),
        ('possession-valid0',[224,0,777,0xDEADBEEF]),('possession-index131',[224,131,0xBEEF,0]),
        ('possession-index132',[224,132,0xBEEF,0]),('possession-index133',[224,133,0xBEEF,0]),
        ('possession-wrap-to-unknown29',[224,(-337)&0xffffffff,0xCAFE,0]),
        ('possession-highbit-alias0',[224,0x80000000,777,0])]
    for name,r in samples:add(name,[[29,0x11223344,0xA5A55A5A,0],[186,0x55667788,0xA5A55A5A,0],r])
    add('growth-then-possession',[[65,31,150,11],[224,0,200,0]])
    add('possession-then-growth',[[224,0,200,0],[65,31,150,11]])
    return rows


def build(c):return old.build(c)


def execute(c):
    args,ch=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha(Path(old.cpu.__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase200-global-index-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        saved=dict(case=c,input=args,result=old.cpu.run(EXE,args,list(ch.items()),old.prior.fixture.p.f.OBS,old.OUT,32));dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def get_baseline():
    c=cases()[0];args,ch,saved,file=execute(c);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['emulation_error']
    return old.post_context(ch,r)[BASE]


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=old.post_context(ch,r);wanted=bytearray(get_baseline());writes=[]
    for tag,a,b,d in c['records']:
        if tag in [29,186]:off=0x71 if tag==29 else 0x41A;size=4;value=a
        elif tag==65:off=(0xCF+2*((9*a+d)&0xffffffff))&0xffffffff;size=2;value=b&65535
        elif tag==224:off=(0x313+2*a)&0xffffffff;size=2;value=b&65535
        else:raise AssertionError(tag)
        assert 0<=off<=len(wanted)-size
        wanted[off:off+size]=value.to_bytes(size,'little');writes.append(dict(type=tag,offset=off,bytes=size,value=value))
    assert post[BASE]==wanted,(c['name'],[(i,x,y) for i,(x,y) in enumerate(zip(post[BASE],wanted)) if x!=y][:10])
    assert post[old.DATA]==ch[old.DATA] and post[old.WORLD][0x1F24:0x1F24+old.TABLE_BYTES]==ch[old.WORLD][0x1F24:0x1F24+old.TABLE_BYTES]
    assert int(next(x['return_EAX'] for x in r['before_calls'] if x['address']=='0x56cbd0'),0)==1
    state=struct.unpack('<5I',post[old.STATE]);assert state[2:4]==(0,0) and state[0]==56+16*len(c['records'])
    assert r['visited_instructions'].get('0x521790',0)==len(c['records'])
    for tag,pc in [(65,0x51DC40),(224,0x51DC70)]:assert r['visited_instructions'].get(hex(pc),0)==sum(x[0]==tag for x in c['records'])
    observed=[e for e in r['observed_writes'] if int(e['pc'],16) in [0x51DC59,0x51DC7A]]
    expected=[x for x in writes if x['type'] in [65,224]];assert len(observed)==len(expected)
    for got,want in zip(observed,expected):assert (int(got['address'],16),got['size'],got['value'])==(BASE+want['offset'],want['bytes'],want['value'])
    checksum=sum((x if x<128 else x-256) for record in c['records'] for x in struct.pack('<4I',1,1,1,1)+struct.pack('<4I',*record))
    assert int.from_bytes(post[old.WORLD][0x72E7:0x72EB],'little')==(int.from_bytes(ch[old.WORLD][0x72E7:0x72EB],'little')+checksum)&0xffffffff
    assert int.from_bytes(post[old.prior.TARGET][0x447:0x449],'little')==100 and int.from_bytes(post[old.prior.TARGET][0x40:0x44],'little')==1000
    return post,dict(records_applied=len(c['records']),whole_global_region_bytes=len(wanted),whole_global_region_matches=True,expected_writes=writes,
        unknown29_bits=int.from_bytes(wanted[0x71:0x75],'little'),unknown186_bits=int.from_bytes(wanted[0x41A:0x41E],'little'),first_possession_WORD=int.from_bytes(wanted[0x313:0x315],'little'),
        original_table_handler_writes=observed,return_value=1,short_reads=0,provider_errors=0,source_records_and_registration_preserved=True,
        native_steps=r['instruction_count'])


def main():
    rows=[]
    for c in cases():
        args,ch,saved,file=execute(c);_,out=verify(c,args,ch,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],hex(out['unknown29_bits']),hex(out['unknown186_bits']),out['first_possession_WORD'],'verified',flush=True)
    dump('phase200-global-index-emulations.json',dict(scope=__doc__,registration=old.registration()[1],replays=rows))


if __name__=='__main__':main()
