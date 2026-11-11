"""File-applied Type224/65 -> rawCreature/native definition -> whole speed branches.

Extends phase92 with fresh definition and file loading plus health/surface
composition. Actor/World/cells are prepared class snapshots, no live movement.
"""
import json,struct
from pathlib import Path
import probe_possession_speed_context as p
ROOT,load,dump,sha=p.ROOT,p.load,p.dump,p.sha


def cases():
    rows=[]
    def add(name,**kw):rows.append(dict(name=name,**kw))
    add('source200-clear')
    add('wounded-water-rounding',HP=199,terrain=128)
    add('at-health-boundary-water',HP=200,terrain=128)
    add('wounded-lava',HP=199,terrain=256)
    add('water-unpossessed',terrain=128,possessed=False)
    add('lava-unpossessed',terrain=256,possessed=False)
    add('water-and-lava',terrain=384,possessed=False)
    add('wounded-other-flag',HP=199,terrain=128,possessed=False,other_flag=True)
    add('wounded-both-flags',HP=199,terrain=128,other_flag=True)
    add('wounded-coeff75',HP=199,terrain=128,percent=75)
    add('wounded-coeff-zero',HP=199,terrain=128,percent=0)
    add('wounded-coeff-minus100',HP=199,terrain=128,percent=(-100)&0xffffffff)
    add('raw-high-bits',HP=199,terrain=128,percent=0xABCD00C8)
    add('run-wounded-water',HP=199,terrain=128,mode=1)
    add('shuffle-wounded-water',HP=199,terrain=128,mode=2)
    add('unknown-mode-water',HP=199,terrain=128,mode=9)
    add('level2-wounded',HP=299,terrain=128,level=2)
    add('level2-boundary',HP=300,terrain=128,level=2)
    add('level2-File-growth125-wounded',HP=249,terrain=128,level=2,growth=125)
    add('level2-File-growth125-boundary',HP=250,terrain=128,level=2,growth=125)
    add('definition-bit0-ground',HP=199,terrain=128,definition_bit0=True)
    add('definition-bit0-status100',HP=199,terrain=128,definition_bit0=True,status100=True)
    add('status100-without-definition-bit0',HP=199,terrain=128,status100=True)
    add('HP-zero-getter-only',HP=0,terrain=128)
    add('large-raw-speed-narrowing',walk=100000000,terrain=256)
    add('walk-raw-zero',walk_raw=0)
    add('walk-raw-one-minimum',walk_raw=1)
    add('walk-raw-quotient65536-minimum',walk_raw=20*65536)
    add('walk-raw-all-ones',walk_raw=0xffffffff)
    add('run-raw-all-ones',mode=1,run_raw=0xffffffff)
    add('shuffle-raw-one-minimum',mode=2,shuffle_raw=1)
    add('negative-coefficient-wrap',terrain=256,percent=0x8000)
    return rows


def execute(c):return p.execute(c)
u32=lambda v:v&0xffffffff
s32=lambda v:(v&0x7fffffff)-(v&0x80000000)
truncdiv=lambda a,b:(abs(a)//abs(b))*(-1 if (a<0)!=(b<0) else 1)


def loaded_speed(c,key,default):
    raw=c.get(key+'_raw',c.get(key,default)*20)&0xffffffff;q=(raw//20)&65535
    return q if q or not raw else 1


def project(c):
    mode=c.get('mode',0);base={1:loaded_speed(c,'run',301),2:loaded_speed(c,'shuffle',53)}.get(mode,loaded_speed(c,'walk',109))
    growth=c.get('growth',100+50*(c.get('level',1)-1));maxhp=1000 if c.get('level',1)==1 else truncdiv(growth*1000,100)
    threshold=(u32(maxhp*20))//100
    wounded=not c.get('definition_bit0') and c.get('HP',1000)<s32(threshold)
    health=u32(base*20)//100 if wounded else base
    terrain=c.get('terrain',0);active_terrain=not (c.get('definition_bit0') and c.get('status100'))
    surface=128 if active_terrain and terrain&128 else 256 if active_terrain and terrain&256 else 0
    terrain_value=u32(health*(50 if surface==128 else 150))//100 if surface else health
    raw=c.get('percent',200)&65535;coeff=raw if raw<32768 else raw-65536
    divisor=100 if c.get('possessed',True) else 200 if c.get('other_flag') else None
    result=u32(terrain_value*u32(100+coeff))//divisor if divisor else terrain_value
    return dict(mode=mode,loaded_base=base,level=c.get('level',1),maxHP_formula_value=maxhp,health_branch_enabled=not bool(c.get('definition_bit0')),health_threshold=threshold,wounded_branch=wounded,after_health=health,
        selected_surface_bit=surface,after_surface=terrain_value,coefficient_signed16=coeff,final_divisor=divisor,result=result)


def verify(c,args,ch,meta,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0 and r['source_exe_sha256']==sha(p.EXE.read_bytes())
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=p.post_context(ch,r);wanted=project(c);d=post[p.DEFINITION];actor=post[p.ACTOR]
    assert struct.unpack_from('<3I',d,0x5B4)==tuple(loaded_speed(c,k,d) for k,d in [('walk',109),('run',301),('shuffle',53)])
    assert d[0x686]&1==bool(c.get('definition_bit0')) and struct.unpack_from('<H',d,0x692)[0]==1000
    assert post[p.prior.RAW]==ch[p.prior.RAW] and post[p.prior.MID][6457:]==ch[p.prior.MID][6457:] and d[2199:]==ch[p.DEFINITION][2199:]
    assert struct.unpack_from('<I',d,0x5AF)[0]==sum(post[p.prior.MID][:6457])
    assert int(r['registers']['EAX'],16)==wanted['result'],(c['name'],r['registers'],wanted)
    expected_actor=bytearray(ch[p.ACTOR])
    if c.get('possessed',True):
        struct.pack_into('<I',expected_actor,0x3E6,struct.unpack_from('<I',expected_actor,0x3E6)[0]|128)
        struct.pack_into('<II',expected_actor,0x3EE,0,struct.unpack_from('<I',expected_actor,0x3F2)[0]);struct.pack_into('<I',expected_actor,0x488,0)
    assert actor==expected_actor,(c['name'],[(hex(i),x,y) for i,(x,y) in enumerate(zip(actor,expected_actor)) if x!=y][:10])
    raw=bytes.fromhex(meta['raw_variable']);assert post[p.files.DATA]==ch[p.files.DATA]
    assert struct.unpack('<5I',post[p.files.STATE])[0:4]==(len(raw),3+len(meta['records']),0,0)
    assert post[p.files.OUT][:28]==raw[20:48]
    assert int(next(x['return_EAX'] for x in r['before_calls'] if x['address']=='0x56cbd0'),0)==1
    assert post[p.WORLD][0x1F24:0x1F24+p.files.TABLE_BYTES]==ch[p.WORLD][0x1F24:0x1F24+p.files.TABLE_BYTES]
    checksum=sum((b if b<128 else b-256) for record in meta['records'] for b in struct.pack('<4I',1,1,1,1)+struct.pack('<4I',*record))
    assert int.from_bytes(post[p.WORLD][0x72E7:0x72EB],'little')==u32(int.from_bytes(ch[p.WORLD][0x72E7:0x72EB],'little')+checksum)
    var=bytearray(ch[p.VARS]);struct.pack_into('<H',var,0x313+42,c.get('percent',200)&65535)
    if 'growth' in c:struct.pack_into('<H',var,0xE1+2*c.get('level',1),c['growth']&65535)
    assert post[p.VARS]==var
    assert r['visited_instructions'].get('0x48abb0')==1 and r['visited_instructions'].get('0x508fe0')==1 and r['visited_instructions'].get('0x48ed40')==1
    assert r['visited_instructions'].get('0x495080',0)==int(bool(wanted['final_divisor']))
    assert r['visited_instructions'].get('0x48ac73',0)==int(wanted['wounded_branch'])
    assert r['visited_instructions'].get('0x48ac9d',0)==int(wanted['selected_surface_bit']==128)
    assert r['visited_instructions'].get('0x48acb3',0)==int(wanted['selected_surface_bit']==256)
    assert r['visited_instructions'].get('0x51dc70')==1 and r['visited_instructions'].get('0x51dc40',0)==int('growth' in c)
    assert r['visited_instructions'].get('0x48dea0',0)==int(c.get('possessed',True))
    for a in [meta['terrain'],0x6DA4A8,0x6E4154,0x6E5050]:assert post[a]==ch[a]
    wanted.update(native_steps=r['instruction_count'],compared_regions=len(post),compared_bytes=sum(map(len,post.values())),file_records=len(meta['records']),
        raw_definition_source_preserved=True,actor_changes_only_original_possession_setter=True,whole_global_region_matches=True)
    return post,wanted


def main():
    rows=[]
    for c in cases():
        args,ch,meta,saved,file=execute(c);_,out=verify(c,args,ch,meta,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out['result'],'verified',flush=True)
    dump('phase201-speed-context-emulations.json',dict(scope=__doc__,replays=rows))


if __name__=='__main__':main()
