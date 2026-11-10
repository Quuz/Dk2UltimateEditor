"""Actual raw Terrain→World occupancy→Lean shape→generic cursor mesh path.

Mixed32x32navigation uses original World20C/220, CMap4528C0/4528E0 and
4D1A20; no internal adapters or fake virtual results. Explicit prepared map.
"""
from pathlib import Path
import json,struct,sys
import emulate_cursor_procedure as base
import native_terrain_shape_trace as cpu
import native_terrain_lean as model
from emulate_resistance import post_context
from emulate_element_colors import region

ROOT=Path(__file__).resolve().parent


def cases():
    out=[]
    for topology in ['free','east','north','isolated','corner','checker']:
        for field in ['zero','H','V']:
            out.append(dict(name=topology+'-'+field,topology=topology,nav_block=8,lean_h=[128]*3 if field=='H' else [0]*3,
                lean_v=[128]*3 if field=='V' else [0]*3))
    out += [dict(name='east-H-dynamic',topology='east',nav_block=16,lean_h=[128]*3,lean_v=[0]*3),
        dict(name='east-H-fluid',topology='east',nav_block=1,lean_h=[128]*3,lean_v=[0]*3),
        dict(name='east-H-layers',topology='east',nav_block=8,lean_h=[32,128,255],lean_v=[0]*3),
        dict(name='east-V-layers',topology='east',nav_block=8,lean_h=[0]*3,lean_v=[32,128,255])]
    return out


def occupied(case,x,y):
    return dict(free=False,east=x>=3,north=y>=4,isolated=(x==2 and y==3),
        corner=(x>=3 and y>=4),checker=bool((x+y)&1))[case['topology']]


def build(case):
    parent=dict(base.cases()[0],deform=True,cell_flags=0x2000,coords=[8192,12288,-8192],
        terrain_values=[0,*case['lean_h'],0,*case['lean_v']],name=case['name'])
    args,checks,source=base.build(parent)
    memory=json.loads(args['memory'])['regions'];nav=bytearray(1024)
    for y in range(32):
        for x in range(32):nav[y*32+x]=case['nav_block'] if occupied(case,x,y) else 0
    next(r for r in memory if int(r['address'],16)==base.NAV)['hex']=nav.hex();checks[base.NAV]=bytes(nav)
    args['memory']=json.dumps(dict(regions=memory))
    args['observe_reads'] += [[base.NAV,1024],[base.MAP,16384]]
    return args,checks,source,parent


def path_for(args):
    tag=base.sha((json.dumps(args,sort_keys=True)+base.sha(Path(__file__).read_bytes())+base.sha((ROOT/'native_terrain_shape_trace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase133-Lean-native-{tag}.json'


def occupancy_grid(case):
    # Current integer cursor location(2,3), native helper5A1070 covers x1..4/y2..5.
    return [int(bool((case['nav_block']&0xF8) and occupied(case,x,y))) for y in range(2,6) for x in range(1,5)]


def verify(case,args,checks,source,parent,result):
    result_base=base.verify(parent,args,checks,source,result)
    assert len(result['shape_calls'])==1
    shape=result['shape_calls'][0]
    assert list(struct.unpack('<16I',bytes.fromhex(shape['occupancy_hex'])))==occupancy_grid(case)
    expected,temp=model.project(bytes.fromhex(shape['input_grid_hex']),bytes.fromhex(shape['occupancy_hex']),bytes.fromhex(shape['samples_hex']))
    assert bytes.fromhex(shape['output_grid_hex'])==expected,case['name']
    assert len(result['shape_stages'])==1 and bytes.fromhex(result['shape_stages'][0]['hex'])==temp,case['name']
    visits=result['visited_instructions']
    assert visits.get('0x5092d0',0)>0 and visits.get('0x4528c0',0)>0 and visits.get('0x4528e0',0)>0 and visits.get('0x4d1a20',0)>0
    assert visits.get('0x5092d0',0)==visits.get('0x4528c0',0)
    post=post_context(checks,result)
    assert post[base.NAV]==checks[base.NAV] and post[base.MAP]==checks[base.MAP]
    return dict(**result_base,occupancy=occupancy_grid(case),original_World220_calls=visits['0x5092d0'],
        shape_output_hex=shape['output_grid_hex'],final_grid_hex=result['deform_captures'][0]['hex'],
        lean_h=case['lean_h'],lean_v=case['lean_v'],independent_Lean_shape_bytes=324,independent_temporary_bytes=324)


if __name__=='__main__':
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for case in selected:
        args,checks,source,parent=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=cpu.run(base.EXE,args,list(checks.items()),base.base.STOP,base.TERRAIN_DEF,460)
            saved=dict(case=case,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        if not saved['result']['success']:print(case['name'],saved['result']['emulation_error'],saved['result']['trace_tail'],flush=True)
        outcome=verify(case,args,checks,source,parent,saved['result'])
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=base.sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified World220 calls',outcome['original_World220_calls'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase133-Lean-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
