"""Original isolated Lean helper and complete raw-loader→mixed World occupancy.

Four helper inputs are captured at actual59F4B0 entries of accepted full paths,
with outputs independently predicted from those complete typed buffers. Two
other compositions start at the real raw loaders and call complete5A1070.
"""
from pathlib import Path
import json,struct
import emulate_terrain_lean as full
from emulate_element_colors import region
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
OUT,OCC,SAMPLES,SCRATCH=0x18600000,0x18610000,0x18620000,0x18630000


def cases():
    return [dict(name='shape-'+n,kind='shape',source_case=n) for n in ['east-H','isolated-H','corner-H','east-V-layers']]+[
        dict(name='World-'+n,kind='World',source_case=n) for n in ['east-H-dynamic','checker-H']]


def source(case):
    c=next(c for c in full.cases() if c['name']==case['source_case']);args,checks,raw,parent=full.build(c)
    path=full.path_for(args);saved=json.loads(path.read_text(encoding='utf8'))
    full.verify(c,args,checks,raw,parent,saved['result'])
    return c,args,checks,saved,dict(file=str(path.relative_to(ROOT)),sha256=full.base.sha(path.read_bytes()))


def build(case):
    c,args,checks,saved,source_link=source(case)
    if case['kind']=='shape':
        sh=saved['result']['shape_calls'][0]
        checks={OUT:bytes.fromhex(sh['input_grid_hex'])+bytes([165])*64,OCC:bytes.fromhex(sh['occupancy_hex']),
            SAMPLES:bytes.fromhex(sh['samples_hex']),SCRATCH:bytes(32),0:bytes(4)}
        mem={**checks,0x7FFEE000:bytes([165])*0x4000}
        args=dict(program='DKII-DX.exe',address='0x504d60',registers=json.dumps({'ECX':hex(SCRATCH)}),
            memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),
            before_calls=[dict(address='0x59f4b0',registers={'ECX':'0'},stack_args=[OUT,OCC,SAMPLES])],max_steps=1000000,
            observe_reads=[[OUT,388],[OCC,64],[SAMPLES,540],[0,4096]],observe_writes=[[OUT,388]])
    else:
        memory=json.loads(args['memory'])['regions'];memory.append(region(OUT,bytes([165])*128));checks[OUT]=bytes([165])*128
        args['memory']=json.dumps(dict(regions=memory))
        assert args['before_calls'][-1]['address']=='0x59b880'
        args['before_calls']=args['before_calls'][:-1]+[dict(address='0x5a1070',registers={'ECX':'0'},stack_args=[OUT,full.base.WORLD,2,3])]
        args['observe_writes'] += [[OUT,128]]
    return args,checks,source_link


def path_for(args):
    key=full.base.sha((json.dumps(args,sort_keys=True)+full.base.sha(Path(__file__).read_bytes())+full.base.sha((ROOT/'native_terrain_shape_trace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase133-Lean-control-native-{key}.json'


def verify(case,args,checks,source_link,result):
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures'] and result['function_or_vtable_substitutions']==0
    assert len(result['before_calls'])==len(args['before_calls']) and all(c['returned'] for c in result['before_calls'])
    post=post_context(checks,result)
    if case['kind']=='shape':
        expected,_=full.model.project(checks[OUT][:324],checks[OCC],checks[SAMPLES])
        assert post[OUT]==expected+checks[OUT][324:]
        for a in [OCC,SAMPLES,SCRATCH,0]:assert post[a]==checks[a]
        assert result['visited_instructions'].get('0x59f4b0')==1 and not result['visited_instructions'].get('0x59b880')
        independent_bytes=388
    else:
        c=next(c for c in full.cases() if c['name']==case['source_case'])
        expected=struct.pack('<16I',*full.occupancy_grid(c))+checks[OUT][64:]
        assert post[OUT]==expected
        for a in [full.base.NAV,full.base.MAP,full.base.WORLD,full.base.base.HEAP,full.base.base.QUEUE]:assert post[a]==checks[a]
        assert result['visited_instructions'].get('0x5a1070')==1 and result['visited_instructions'].get('0x4528e0',0)>0
        assert not result['visited_instructions'].get('0x59b880')
        independent_bytes=128
    return dict(kind=case['kind'],source=source_link,independent_output_and_guard_bytes=independent_bytes,
        checked_regions=len(post),checked_bytes=sum(map(len,post.values())),instruction_count=result['instruction_count'])


if __name__=='__main__':
    rows=[]
    for case in cases():
        args,checks,link=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=full.cpu.run(full.base.EXE,args,list(checks.items()),full.base.base.STOP,OUT,len(checks[OUT]))
            saved=dict(case=case,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        outcome=verify(case,args,checks,link,saved['result'])
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=full.base.sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified',flush=True)
    (ROOT/'phase133-Lean-control-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
