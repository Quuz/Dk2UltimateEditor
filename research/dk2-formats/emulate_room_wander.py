"""New native room-list branch cases; no old empty-list cases counted again."""
from pathlib import Path
import json,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_room_wander as probe
import native_room_wander as oracle
from native_wander_state import rand
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
base=probe.base;sha=probe.cpu.sha
def cases():
    a=dict(type=1,cells=[[3,3]]);b=dict(type=2,cells=[[20,20]]);empty=dict(type=2,cells=[])
    rows=[dict(name='two-eligible-last',seed=23,radius=1,rooms=[a,b]),
      dict(name='two-eligible-reversed',seed=23,radius=1,rooms=[b,a]),
      dict(name='last-empty-skips-earlier',seed=23,radius=1,rooms=[a,empty]),
      dict(name='trailing-excluded-selected',seed=23,radius=1,rooms=[a,empty,dict(type=3,cells=[[24,24]])]),
      dict(name='trailing-empty-then-excluded',seed=23,radius=1,rooms=[a,empty,dict(type=5,cells=[]),dict(type=8,cells=[[24,24]])]),
      dict(name='gate-bypasses-nonempty',seed=1,radius=1,rooms=[a,b])]
    for t in range(1,27):rows.append(dict(name=f'room-type-{t}',seed=23,radius=1,rooms=[dict(type=t,cells=[[20,20]])]))
    seeds={}
    for s in range(1000):
        after,g=rand(s,8)
        if g==0:seeds.setdefault(rand(after,3)[1],s)
    assert set(seeds)=={0,1,2}
    for j,s in sorted(seeds.items()):rows.append(dict(name=f'cell-choice-{j}',seed=s,radius=1,rooms=[dict(type=26,cells=[[20,20],[21,20],[22,20]])]))
    for n,extra in [('callback-room',{}),('callback-room-wander255',dict(wander=255)),('callback-room-blocked',dict(blocked=True))]:
        rows.append(dict(name=n,seed=8,callback=True,rooms=[a,b],**extra))
    return rows
def execute(c):
    args,checks,saved,p=probe.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,p
def verify(c,args,checks,r):
    post=post_context(checks,r);e=oracle.expected(c);u=lambda a,o,n=4:int.from_bytes(post[a][o:o+n],'little')
    assert r['unfinished_wander_target'] is None
    assert u(base.f.WORLD,0x15A0)==e['seed']
    assert [x['bound'] for x in r['wander_rng_calls']]==e['rng_bounds']
    assert r['room_target_calls']==[hex(probe.room_address(i)) for i in e['room_trials']]
    runs=e['helper_runs'] if c.get('callback') else [dict(radius=c.get('radius',4),**e)]
    assert len(r['wander_target_calls'])==len(runs)
    for observed,wanted in zip(r['wander_target_calls'],runs):
        for k in ['radius','target']:assert observed[k]==wanted[k],(c['name'],k,observed,wanted)
        assert observed['bounds_queries']==wanted['queried_cells']
    assert post[base.cf.ACTOR][0x418]==c.get('wander',0)
    assert post[base.cf.ACTOR][0x1A:0x26]==checks[base.cf.ACTOR][0x1A:0x26]
    assert not [x for x in r['observed_reads'] if int(x['address'],16)<=base.cf.ACTOR+0x418<int(x['address'],16)+x['size']]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=base.EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(base.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    for i in range(len(c['rooms'])):assert post[probe.room_address(i)]==checks[probe.room_address(i)]
    assert post[base.f.MAP]==checks[base.f.MAP]
    if c.get('callback'):
        accepted=not c.get('blocked',False)
        assert int(r['registers']['EAX'],16)==int(accepted)
        assert u(base.cf.ACTOR,0x25E)==(6 if accepted else 87)
        assert len(r['before_calls'])==6 and all(v['returned'] for v in r['before_calls'])
        assert post[probe.path.STATEBASE]==probe.path.initialized_states()[0]
        assert post[probe.path.CATBASE]==probe.path.initialized_categories()[0]
        assert post[probe.path.TRIANGLES][probe.path.TRIANGLE_BYTES:]==checks[probe.path.TRIANGLES][probe.path.TRIANGLE_BYTES:]
        if accepted:
            assert [u(base.cf.ACTOR,0x331),u(base.cf.ACTOR,0x335)]==e['target'][:2]
            assert u(base.cf.ACTOR,0x28E)==87
        e.update(path_accepted=accepted,state_after=u(base.cf.ACTOR,0x25E))
    else:
        assert int(r['registers']['EAX'],16)==int(e['target'] is not None)
        wanted={a:bytearray(b) for a,b in checks.items()}
        struct.pack_into('<I',wanted[base.f.WORLD],0x15A0,e['seed'])
        if e['target'] is not None:wanted[base.TARGET][:12]=struct.pack('<3I',*e['target'])
        assert {a:bytes(b) for a,b in wanted.items()}==post,c['name']
    return post,dict(**e,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
def main():
    rows=[];states={};selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name'] in sys.argv[1:]];assert selected
    for c in selected:
        args,checks,saved,p=execute(c);post,out=verify(c,args,checks,saved['result']);states[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes())))
        print(c['name'],{k:out[k] for k in ['target','room_trials','rng_bounds']},flush=True)
    if len(selected)==len(cases()):
        wanted={a:bytearray(b) for a,b in states['callback-room'].items()};wanted[base.cf.ACTOR][0x418]=255
        assert {a:bytes(b) for a,b in wanted.items()}==states['callback-room-wander255']
        (ROOT/'phase145-room-wander-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
            paired_control=dict(whole_regions=len(wanted),whole_bytes=sum(map(len,wanted.values())),only_actor418_differs=True)),indent=2),encoding='utf8')
if __name__=='__main__':main()
