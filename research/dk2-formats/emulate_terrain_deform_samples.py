"""Raw Terrain loader through complete59E210, before unsupported Ghidra FSIN.

This is a separate whole-function composition, not a patched renderer or a claim
that Ghidra executed the complete deformed frame. All540 output bytes predicted.
"""
from pathlib import Path
import json,struct,sys
import emulate_cursor_procedure as cursor
from emulate_element_colors import region
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
OUTPUT=0x18400000


def cases():return [c for c in cursor.cases() if c['name'] in ['deform-Terrain-source','deform-Lean-high']]


def build(case):
    args,checks,source=cursor.build(case)
    memory=json.loads(args['memory'])['regions'];initial=bytes([165])*604
    memory.append(region(OUTPUT,initial));checks[OUTPUT]=initial
    args['memory']=json.dumps(dict(regions=memory))
    assert args['before_calls'][-1]['address']=='0x59b880'
    args['before_calls']=args['before_calls'][:-1]+[dict(address='0x59e210',registers={},stack_args=[cursor.WORLD,2,3,OUTPUT])]
    args['observe_writes'] += [[OUTPUT,len(initial)]]
    return args,checks,source


def path_for(args):
    tag=cursor.sha((json.dumps(args,sort_keys=True)+cursor.sha(Path(__file__).read_bytes())+cursor.sha((ROOT/'native_cursor_trace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase132-Terrain-samples-native-{tag}.json'


def verify(case,args,checks,source,result):
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures'] and result['function_or_vtable_substitutions']==0
    assert all(c['returned'] for c in result['before_calls']) and len(result['before_calls'])==len(args['before_calls'])
    post=post_context(checks,result);raw=checks[cursor.TERRAIN_RAW]
    expected=bytearray(checks[OUTPUT])
    for z in range(3):
        for y in range(3):
            for x in range(3):
                struct.pack_into('<2I2f',expected,(z*9+y*3+x)*20,raw[0x1CE]<<7,raw[0x1D2]<<7,
                    raw[0x1CF+z]/512,raw[0x1D3+z]/512)
    assert post[OUTPUT]==expected
    assert post[cursor.TERRAIN_RAW]==raw and post[cursor.base.HEAP]==checks[cursor.base.HEAP] and post[cursor.base.QUEUE]==checks[cursor.base.QUEUE]
    for a in [cursor.WORLD,cursor.MAP,cursor.NAV,cursor.NAVDESC]:assert post[a]==checks[a]
    for ro,do in [(0x1CE,0x1A1),(0x1D2,0x1A5)]+[(0x1CF+i,0x1A2+i) for i in range(3)]+[(0x1D3+i,0x1A6+i) for i in range(3)]:
        assert post[cursor.TERRAIN_DEF][do]==raw[ro]
    assert result['visited_instructions'].get('0x59e210')==1 and not result['visited_instructions'].get('0x59b880')
    return dict(output_bytes_compared=len(expected),samples=27,stride=20,untouched_sample_tail_bytes=108,
        guard_bytes_preserved=64,raw_to_definition_values=list(raw[0x1CE:0x1D6]),
        instruction_count=result['instruction_count'],whole_prepared_regions=len(post),whole_prepared_bytes=sum(map(len,post.values())))


if __name__=='__main__':
    rows=[]
    for case in cases():
        args,checks,source=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            r=cursor.cpu.run(cursor.EXE,args,list(checks.items()),cursor.base.STOP,OUTPUT,len(checks[OUTPUT]))
            saved=dict(case=case,input=args,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        outcome=verify(case,args,checks,source,saved['result'])
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=cursor.sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'full540-byte sample output+64guard verified',flush=True)
    (ROOT/'phase132-Terrain-samples-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
