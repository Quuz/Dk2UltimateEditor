"""Raw Terrain -> native cache -> actual Light factory -> full tile-light submission.

Prepared CBridge prefix, sparse World catalogue and runtime tile; original
vtables/constructors/getters/pools/tag registry. Sound lookup and engine E0
submission are explicit external sinks. No final rendered pixel claim.
"""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_terrain_light as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha

def cases():
    return [dict(name='dirt'),dict(name='gold',terrain=6),dict(name='encoded-high',rgb=[255,0,1],high=[1,0,1]),
        dict(name='black',rgb=[0,0,0]),dict(name='disabled',disabled=True),dict(name='cellC-blocked',cellC=0),
        dict(name='global-dirt',cell9=0,cellB=0x40),dict(name='global-gold',terrain=6,cell9=0,cellB=0x40),
        dict(name='unknowns',unknown198=0xBEEF,unknown224=0x89ABCDEF),dict(name='negative-height',light_height=0xFFFFF800),
        dict(name='raised-floor',floor=8192),dict(name='negative-floor',floor=-4096,light_height=0xFFFFF800),
        dict(name='fallback-global',fallback=True),dict(name='fallback-colour',fallback=True,fallback_gold=1),
        dict(name='fallback-disabled',fallback=True,fallback_disabled=True)]

def execute(c):return p.execute(c)

def verify(c,args,checks,raws,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==10 and all(b['returned'] for b in r['before_calls'])
    assert r['visited_instructions'].get('0x4397e0',0)==1 and r['visited_instructions'].get('0x441cd0',0)==1
    post=post_context(checks,r);u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    expected_array=bytearray(checks[p.ARRAY]);expected_defs={}
    for ident,raw in raws.items():
        mid=p.terrain.layout.intermediate(raw);d=p.terrain.definition_expected(mid,0);expected_defs[ident]=d
        expected_array[460*ident:460*(ident+1)]=d
    assert post[p.ARRAY]==expected_array
    last=p.terrain.layout.intermediate(raws[38]);assert post[p.MID]==last+checks[p.MID][736:]
    for i,ident in enumerate([c.get('terrain',3),38]):assert post[p.RAW+0x1000*i]==checks[p.RAW+0x1000*i]
    cache=bytearray(1024)
    for ident in raws:p.put(cache,4*ident,p.ARRAY+460*ident)
    assert post[0x6DA4A8]==cache and post[p.WORLD]==checks[p.WORLD]
    assert u(post[p.SIO],0)==4
    ptr=u(post[p.OUT],0);pool=post[p.fx.scaffold.POOL];offset=ptr-p.fx.scaffold.POOL
    assert 0<=offset<=p.fx.scaffold.POOL_SIZE-326
    light=pool[offset:offset+326];tag=u(light,0xF2,2)
    assert u(light,0)==0x66CC14 and tag==u(post[p.fx.base.BRIDGE],0x35B,2)==1
    assert u(post[0x6D6458],4*tag)==ptr
    assert u(post[p.fx.base.BRIDGE],0x33A)==ptr
    assert u(light,0x13C)==0 and u(light,0x140)==0
    current=c.get('terrain',3);selected=38 if c.get('fallback') else current
    d=expected_defs[selected];enabled=bool(u(d,0x17B)&0x10000000) and c.get('cellC',255)==255
    expected_rgb=[u(d,o,2) for o in [0x1AF,0x1B1,0x1B3]]
    cell9=0 if c.get('fallback') else c.get('cell9',0x20);cellB=0x40 if c.get('fallback') else c.get('cellB',0)
    override=not(cell9&0x20) and bool(cellB&0x40) and u(d,0x185,2)==0
    if override:expected_rgb=[111,222,333]
    expected_xyz=[10240,14336,struct.unpack('<i',d[0x1BF:0x1C3])[0]]
    # Actual Light initializer has zero F8; original446C70 uses the declared
    # external floor provider, and lifts this Light to that floor if below it.
    expected_xyz[2]=max(expected_xyz[2],c.get('floor',0))
    if not enabled:expected_rgb=[500]*3;expected_xyz=[2048,2048,max(0x1200,c.get('floor',0))]
    rgb=list(struct.unpack_from('<3H',light,0x111));xyz=list(struct.unpack_from('<3i',light,0xD6))
    assert rgb==expected_rgb and xyz==expected_xyz,(c['name'],rgb,expected_rgb,xyz,expected_xyz)
    assert u(light,0x117)==0x1200 and u(light,0x134)==(0x8072 if enabled else 0x20)
    event=list(struct.unpack('<6I',post[p.RIO]));assert event==([1,2,3,ptr,0,0xFF000000] if enabled else [0]*6)
    assert r['visited_instructions'].get('0x508dd0',0)==(3 if c.get('fallback') else 2)
    # The decoded object must be the actual allocated/tagged Light, and all low
    # accesses must be original FS:0 SEH, never a readable-null-page substitute.
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        pc=int(row['pc'],16);assert 0x400000<=pc<0x670000,row
        ins=next(md.disasm(p.fx.pe_read(p.fx.EXE.read_bytes(),pc,15),pc))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,row
    unknown=[]
    for row in r['observed_reads']:
        a=int(row['address'],16)
        for ident in raws:
            for off,size in [(0x183,2),(0x1B5,4)]:
                start=p.ARRAY+460*ident+off
                if a<start+size and a+row['size']>start:unknown.append(row)
    assert not unknown
    assert pool[p.fx.scaffold.POOL_SIZE:]==checks[p.fx.scaffold.POOL][p.fx.scaffold.POOL_SIZE:]
    # Light326 + alignment2 + original12-byte arena header =340.
    assert u(post[p.fx.base.BRIDGE],0x135F)==340
    return post,dict(selected_terrain=selected,submitted=enabled,global_colour_override=override and enabled,
        runtime_RGB_WORDs=rgb,position_Q12=xyz,unchanged_Light_parameter117=0x1200,Light_flags=u(light,0x134),native_Light_tag=tag,
        sink_arguments=event,unknown_definition_reads=0,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_unknown_pair(a,b):
    assert a.keys()==b.keys();allowed={p.RAW:[(0x198,2),(0x224,4)],p.ARRAY:[(460*3+0x84,4),(460*3+0x183,2),(460*3+0x1B5,4)]}
    diffs=[]
    for address,left in a.items():
        right=b[address];assert len(left)==len(right)
        indices=[i for i,(x,y) in enumerate(zip(left,right)) if x!=y]
        assert all(any(off<=i<off+n for off,n in allowed.get(address,[])) for i in indices),(hex(address),indices[:30])
        if indices:diffs.append(dict(region=hex(address),changed_byte_offsets=indices))
    assert {int(d['region'],16) for d in diffs}==set(allowed)
    return dict(cases=['dirt','unknowns'],compared_regions=len(a),compared_bytes=sum(map(len,a.values())),
        allowed_storage_differences={hex(k):[list(span) for span in v] for k,v in allowed.items()},actual_differences=diffs,
        Light_and_submission_identical=True,scope='Only prepared selected441CD0 path; no globalnonuse or gameplay name for raw198/224.')

def main():
    rows=[];posts={}
    for c in cases():
        args,ch,raws,d,file=execute(c);post,out=verify(c,args,ch,raws,d['result']);posts[c['name']]=post
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out['runtime_RGB_WORDs'],out['position_Q12'],'verified',flush=True)
    pair=compare_unknown_pair(posts['dirt'],posts['unknowns'])
    (ROOT/'phase159-terrain-light-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=[pair]),indent=2),encoding='utf8')
if __name__=='__main__':main()
