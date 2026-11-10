"""Native state87 target selection and real initialized NAV path acceptance."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_wander_target as direct
import probe_wander_path as path
from emulate_resistance import post_context
from native_wander_target import choose,callback,nav_value
from native_wander_state import rand

ROOT=Path(__file__).resolve().parent
sha=direct.cpu.sha

def cases():
    rows=[dict(name='helper-open',kind='helper',radius=4,seed=1),
        dict(name='helper-only-before-start',kind='helper',radius=4,seed=1,blocked=True,nav=[[7,6,0]]),
        dict(name='helper-duplicated-corner',kind='helper',radius=4,seed=1,blocked=True,nav=[[6,6,0]]),
        dict(name='helper-only-at-start',kind='helper',radius=4,seed=1,blocked=True,nav=[[10,6,0]]),
        dict(name='helper-foreign',kind='helper',radius=4,seed=1,owner=2),
        dict(name='helper-foreign-exemption',kind='helper',radius=4,seed=1,owner=2,actor_flags=0x4000),
        dict(name='helper-edge',kind='helper',radius=4,seed=1,position=[0x800,0x800,0x2000]),
        dict(name='callback-open',kind='callback',seed=1),dict(name='callback-wander255',kind='callback',seed=1,wander=255),
        dict(name='callback-blocked',kind='callback',seed=1,blocked=True),
        dict(name='callback-foreign-fallback',kind='callback',seed=1,owner=2),
        dict(name='callback-foreign-exemption',kind='callback',seed=1,owner=2,actor_flags=0x4000),
        dict(name='callback-busy',kind='callback',seed=1,current_action=1)]
    found={}
    for seed in range(256):
        radius=rand(seed,4)[1]+4
        if radius not in found:found[radius]=seed
    assert set(found)=={4,5,6,7}
    for radius,seed in sorted(found.items()):
        if seed!=1:rows.append(dict(name=f'callback-radius{radius}',kind='callback',seed=seed))
    seed=next(seed for seed in range(256) if rand(rand(seed,8)[0],32)[1]==0)
    rows.append(dict(name='helper-unvisited-corner',kind='helper',radius=4,seed=seed,blocked=True,nav=[[14,14,0]]))
    return rows

def execute(c):
    module=direct if c['kind']=='helper' else path
    args,checks,saved,p=module.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,p

def verify(c,args,checks,r):
    post=post_context(checks,r);u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    assert r['unfinished_wander_target'] is None
    pos=c.get('position',[0xA800,0xA800,0x2000]);actor=post[direct.cf.ACTOR]
    assert actor[1343:]==checks[direct.cf.ACTOR][1343:]
    assert actor[0x418]==c.get('wander',0) and actor[0x1A:0x26]==struct.pack('<3I',*pos)
    assert not [x for x in r['observed_reads'] if int(x['address'],16)<=direct.cf.ACTOR+0x418<int(x['address'],16)+x['size']]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=direct.EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(direct.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    if c['kind']=='helper':
        expected=choose(c['seed'],pos,c['radius'],c);found=expected['target'] is not None
        assert int(r['registers']['EAX'],16)==int(found)
        assert post[direct.TARGET][:12]==(struct.pack('<3I',*expected['target']) if found else checks[direct.TARGET][:12])
        assert post[direct.TARGET][12:]==checks[direct.TARGET][12:]
        assert post[direct.cf.ACTOR]==checks[direct.cf.ACTOR]
        assert u(post[direct.f.WORLD],0x15A0)==expected['seed']
        assert r['visited_instructions'].get('0x4d0f60',0)==0
        assert len(r['wander_target_calls'])==1
        observed=r['wander_target_calls'][0]
        assert observed['radius']==c['radius'] and observed['bounds_queries']==expected['queried_cells'] and observed['target']==expected['target']
        assert [x['bound'] for x in r['wander_rng_calls']]==[8,c['radius']*8]
        outcome=dict(target=expected['target'],seed_after=expected['seed'],start_index=expected['start_index'],queried_cells=expected['queried_cells'])
    else:
        expected=callback(c);requested=expected['path_requested'];target=expected['target']
        accepted=requested and not c.get('blocked',False)
        assert int(r['registers']['EAX'],16)==int(accepted or not requested)
        assert len(r['before_calls'])==6 and all(x['returned'] for x in r['before_calls'])
        assert post[path.STATEBASE]==path.initialized_states()[0]
        assert post[path.CATBASE]==path.initialized_categories()[0]
        assert post[path.TRIANGLES][path.TRIANGLE_BYTES:]==checks[path.TRIANGLES][path.TRIANGLE_BYTES:]
        assert u(post[direct.f.WORLD],0x15A0)==expected['seed']
        assert r['visited_instructions'].get('0x480a00',0)==len(expected['helper_runs'])
        assert len(r['wander_target_calls'])==len(expected['helper_runs'])
        for actual,wanted in zip(r['wander_target_calls'],expected['helper_runs']):
            assert actual['radius']==wanted['radius'] and actual['bounds_queries']==wanted['queried_cells'] and actual['target']==wanted['target']
        bounds=[4]+[v for h in expected['helper_runs'] for v in [8,h['radius']*8]]+[9] if requested else []
        assert [x['bound'] for x in r['wander_rng_calls']]==bounds
        assert r['visited_instructions'].get('0x471890',0)==int(requested)
        assert r['visited_instructions'].get('0x4d0f60',0)==int(accepted)
        assert u(actor,0x25E)==(6 if accepted else 87)
        if accepted:
            assert [u(actor,0x331),u(actor,0x335)]==target[:2]
            assert u(actor,0x28E)==87
        outcome=dict(radius=expected['radius'],target=target,seed_after=expected['seed'],helper_runs=len(expected['helper_runs']),
            path_requested=requested,path_accepted=accepted,state_after=u(actor,0x25E),actor_position_unchanged=True)
    return post,dict(**outcome,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[];states={}
    for c in cases():
        args,checks,saved,p=execute(c);post,out=verify(c,args,checks,saved['result']);states[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes())));print(c['name'],out,flush=True)
    expected={a:bytearray(b) for a,b in states['callback-open'].items()};expected[direct.cf.ACTOR][0x418]=255
    assert {a:bytes(b) for a,b in expected.items()}==states['callback-wander255']
    (ROOT/'phase144-wander-target-path-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        paired_placement_control=dict(whole_regions=len(expected),whole_bytes=sum(map(len,expected.values())),only_actor_byte418_differs=True)),indent=2),encoding='utf8')

if __name__=='__main__':main()
